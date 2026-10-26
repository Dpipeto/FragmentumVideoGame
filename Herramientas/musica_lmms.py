"""
Escribe proyectos de LMMS 1.2 (.mmp) desde Python y los exporta a un bucle limpio.

Lo usan componer_*.py (que escriben la partitura) y exportar_musica.py (que la convierte en el OGG
del juego). El .mmp que sale se abre en LMMS y se puede editar a mano.

Convenciones de LMMS que hay que saber:
  * El compás de 4/4 tiene 192 ticks (una negra, 48).
  * Las notas van por número: el La 4 (440 Hz) es el 57, así que el Do 4 es el 48 (MIDI - 12).
  * El volumen de una nota va de 0 a 200 (100, el normal); el de una pista, también.
  * Una muestra (AudioFileProcessor) suena en su tono en su «basenote» (57 si no se dice).
"""
import os
import re
import subprocess
import tempfile
import xml.etree.ElementTree as ET

LMMS = r'C:\Program Files\LMMS\lmms.exe'
MUESTRAS = r'C:\Program Files\LMMS\data\samples'
PRESETS = r'C:\Program Files\LMMS\data\presets'

TICKS_NEGRA = 48
TICKS_COMPAS = 4 * TICKS_NEGRA

_SEMITONOS = {'C': 0, 'D': 2, 'E': 4, 'F': 5, 'G': 7, 'A': 9, 'B': 11}


def tecla(nombre):
    """'D4' -> 50, 'Bb2' -> 34, 'C#5' -> 61 (en la numeración de LMMS: el La 4 es el 57)."""
    m = re.fullmatch(r'([A-G])([#b]?)(-?\d)', nombre)
    if not m:
        raise ValueError(f'Nota sin entender: {nombre!r}')
    letra, alteracion, octava = m.groups()
    semitono = _SEMITONOS[letra] + (1 if alteracion == '#' else -1 if alteracion == 'b' else 0)
    return int(octava) * 12 + semitono


_FIGURAS = {'w': 192, 'h': 96, 'q': 48, 'e': 24, 's': 12, 't': 6, 'x': 3}


def frase(texto, vol=100, legato=1.0, transporte=0, compases=None, compas=TICKS_COMPAS):
    """
    Una frase escrita como en papel: «D4:q. A3:e D4:q F4:q | G4:h r:h». Figuras: w redonda, h blanca,
    q negra, e corchea, s semicorchea, t fusa, x semifusa; un punto la alarga la mitad. «r» es un
    silencio; «D3+F3+A3» un acorde; «@120» cambia el volumen de esa nota. «|» separa compases y
    comprueba que cada uno dure lo que debe. legato acorta las notas (0,9 = las separa un poco).
    Devuelve las notas (pos, dur, tecla, vol), desde el tick 0.
    """
    notas = []
    pos = 0
    inicio_compas = 0
    for token in texto.split():
        if token == '|':
            if (pos - inicio_compas) != compas:
                raise ValueError(f'Un compás de «{texto[:40]}...» dura {pos - inicio_compas} ticks, no {compas}')
            inicio_compas = pos
            continue
        cuerpo, _, volumen = token.partition('@')
        alturas, _, figura = cuerpo.partition(':')
        puntos = figura.count('.')
        dur = _FIGURAS[figura.rstrip('.')]
        dur = int(dur * (1.5 if puntos == 1 else 1.75 if puntos == 2 else 1))
        if alturas != 'r':
            v = int(volumen) if volumen else vol
            for altura in alturas.split('+'):
                notas.append((pos, max(1, int(round(dur * legato))), tecla(altura) + transporte, v))
        pos += dur
    if compases is not None and pos != compases * compas:
        raise ValueError(f'La frase «{texto[:40]}...» dura {pos} ticks, no {compases} compases')
    return notas


def segundos(t):
    """El valor de una perilla de tiempo de LMMS para que dure t segundos (LMMS: 5 × valor²)."""
    return (max(t, 0.0) / 5.0) ** 0.5


def env_volumen(ataque=0.0, decaimiento=0.0, nivel=1.0, liberacion=0.05, mantener=0.0):
    """La envolvente de volumen, en segundos. nivel: la amplitud a la que se queda (LMMS la eleva al
    cuadrado, así que se le pasa la raíz)."""
    return envolvente('elvol', att=segundos(ataque), hold=segundos(mantener), dec=segundos(decaimiento),
                      sustain=max(nivel, 0.0) ** 0.5, rel=segundos(liberacion), amt=1)


def _attrs(el, **valores):
    for k, v in valores.items():
        if v is None:
            continue
        if isinstance(v, bool):
            v = int(v)
        if isinstance(v, float):
            v = f'{v:.6g}'
        el.set(k.replace('__', '-'), str(v))
    return el


# --- Envolventes --------------------------------------------------------------------------------

def envolvente(etiqueta, att=0.0, hold=0.0, dec=0.5, sustain=0.5, rel=0.1, amt=0.0, pdel=0.0):
    """Una envolvente de LMMS (elvol, elcut o elres). Los tiempos van de 0 a 2 (LMMS los eleva al
    cuadrado: 0,2 es unos 0,2 s y 1, unos 5 s); amt = 0 la apaga."""
    el = ET.Element(etiqueta)
    return _attrs(el, pdel=pdel, att=att, hold=hold, dec=dec, sustain=sustain, rel=rel, amt=amt,
                  lpdel=0, latt=0, lspd=0.1, lshp=0, lamt=0, x100=0, ctlenvamt=0, userwavefile='',
                  syncmode=0, lspd_numerator=4, lspd_denominator=4)


def eldata(vol=None, cut=None, res=None, fcut=14000, fres=0.5, ftype=0, filtro=False):
    """Envolventes y filtro de la pista. ftype: 0 paso bajo, 1 paso alto, 2 y 3 paso banda, 6 Moog,
    7 paso bajo doble."""
    el = _attrs(ET.Element('eldata'), fcut=fcut, fres=fres, ftype=ftype, fwet=filtro)
    el.append(vol if vol is not None else envolvente('elvol'))
    el.append(cut if cut is not None else envolvente('elcut'))
    el.append(res if res is not None else envolvente('elres'))
    return el


# --- Instrumentos -------------------------------------------------------------------------------

SENO, TRIANGULO, SIERRA, CUADRADA, SIERRA_MOOG, EXPONENCIAL, RUIDO = range(7)


def oscilador(onda=SENO, vol=33, grueso=0, fino_izq=0, fino_der=0, pan=0, fase=0, fase_estereo=0):
    return dict(onda=onda, vol=vol, grueso=grueso, fino_izq=fino_izq, fino_der=fino_der, pan=pan,
                fase=fase, fase_estereo=fase_estereo)


def triple(osciladores, modulacion=(2, 2)):
    """TripleOscillator. osciladores: tres de oscilador(); modulacion: cómo se mezcla el 1 con el 2 y
    el 2 con el 3 (2 = suma)."""
    ins = ET.Element('instrument', name='tripleoscillator')
    el = ET.SubElement(ins, 'tripleoscillator')
    oscs = list(osciladores) + [oscilador(vol=0)] * (3 - len(osciladores))
    for i, o in enumerate(oscs):
        _attrs(el, **{f'wavetype{i}': o['onda'], f'vol{i}': o['vol'], f'coarse{i}': o['grueso'],
                      f'finel{i}': o['fino_izq'], f'finer{i}': o['fino_der'], f'pan{i}': o['pan'],
                      f'phoffset{i}': o['fase'], f'stphdetun{i}': o['fase_estereo'],
                      f'userwavefile{i}': ''})
    _attrs(el, modalgo1=modulacion[0], modalgo2=modulacion[1], modalgo3=2)
    return ins


def organic(armonicos, vol=100, distorsion=0.0):
    """Organic: hasta 8 osciladores. armonicos: (múltiplo de la fundamental, volumen 0-100,
    desafinación en centésimas, onda)."""
    ins = ET.Element('instrument', name='organic')
    el = ET.SubElement(ins, 'organic')
    _attrs(el, num_osc=8, vol=vol, foldback=distorsion)
    lista = list(armonicos) + [(1.0, 0, 0, SENO)] * (8 - len(armonicos))
    for i, (armonico, volumen, desafinado, onda) in enumerate(lista):
        # Organic guarda el armónico como un índice: 0 = ×0,5, 1 = ×0,75, 2 = ×1, 3 = ×2, 4 = ×3...
        indice = {0.5: 0, 0.75: 1, 1.0: 2, 2.0: 3, 3.0: 4, 4.0: 5, 5.0: 6, 6.0: 7, 7.0: 8, 8.0: 9,
                  9.0: 10, 10.0: 11, 12.0: 13, 16.0: 17}[float(armonico)]
        _attrs(el, **{f'newharmonic{i}': indice, f'vol{i}': volumen, f'newdetune{i}': desafinado,
                      f'wavetype{i}': onda, f'pan{i}': 0})
    return ins


def muestra(ruta, amp=100, bucle=False, inicio=0.0, fin=1.0, bucle_desde=0.0, al_reves=False):
    """AudioFileProcessor con una muestra de LMMS (ruta relativa a data/samples) o un archivo."""
    ins = ET.Element('instrument', name='audiofileprocessor')
    ET.SubElement(ins, 'audiofileprocessor')
    _attrs(ins[0], src=ruta.replace('\\', '/'), amp=amp, looped=1 if bucle else 0, sframe=inicio,
           eframe=fin, lframe=bucle_desde, reversed=al_reves, stutter=0, interp=1)
    return ins


def kicker(**valores):
    """Kicker (bombos y cajas sintetizados). Los valores, como en sus presets."""
    ins = ET.Element('instrument', name='kicker')
    base = dict(startfreq=150, endfreq=40, decay=440, dist=0.8, distend=0.8, gain=1, env=0.163,
                noise=0, click=0.4, slope=0.06, startnote=0, endnote=0, version=1, syncmode=0,
                decay_numerator=4, decay_denominator=4)
    base.update(valores)
    _attrs(ET.SubElement(ins, 'kicker'), **base)
    return ins


SFXR_CUADRADA, SFXR_SIERRA, SFXR_SENO, SFXR_RUIDO = range(4)


def sfxr(onda=SFXR_CUADRADA, **valores):
    """
    sfxr: el generador de efectos de los juegos retro. Suena entero mientras dure la nota (una nota más
    corta lo corta), y en la base (el 57) suena tal cual; cada semitono, más agudo o más grave. Sus
    perillas van de 0 a 1 (las que suben o bajan, de -1 a 1); para dárselas en segundos y hercios:
    sfxr_segundos() y sfxr_hz().
      att, hold, dec: la envolvente; sus: el golpe al empezar el sostenido.
      startFreq: la nota (en el ruido, lo grave o agudo que es); slide, dSlide: cómo sube (+) o baja (-).
      lpFilCut, lpFilCutSweep, lpFilReso: el paso bajo (1 = abierto), cómo se abre y su resonancia.
      hpFilCut, hpFilCutSweep: el paso alto. vibDepth, vibSpeed: el vibrato. sqrDuty, sqrSweep: el ancho
      de la cuadrada. repeatSpeed, changeAmt, changeSpeed, phaserOffset, phaserSweep: lo demás de sfxr.
    """
    ins = ET.Element('instrument', name='sfxr')
    base = dict(version=1, att=0.0, hold=0.3, sus=0.0, dec=0.4, startFreq=0.3, minFreq=0.0, slide=0.0, dSlide=0.0,
                vibDepth=0.0, vibSpeed=0.0, changeAmt=0.0, changeSpeed=0.0, sqrDuty=0.0, sqrSweep=0.0,
                repeatSpeed=0.0, phaserOffset=0.0, phaserSweep=0.0, lpFilCut=1.0, lpFilCutSweep=0.0,
                lpFilReso=0.0, hpFilCut=0.0, hpFilCutSweep=0.0)
    base.update(valores)
    _attrs(ET.SubElement(ins, 'sfxr', waveForm=str(onda)), **base)
    return ins


def sfxr_segundos(t):
    """El valor de una perilla de tiempo de sfxr para t segundos (sfxr: valor² × 100 000 muestras)."""
    return (max(t, 0.0) * 44100 / 100000) ** 0.5


def sfxr_hz(hz):
    """El startFreq de sfxr para esa frecuencia (sfxr: 3528 × (valor² + 0,001) Hz; medido: 0,3 suena a 324 Hz)."""
    return max(hz / 3528 - 0.001, 0.0) ** 0.5


def preset(ruta):
    """Lee un preset de LMMS (.xpf, relativo a data/presets): devuelve (instrumento, eldata, efectos)."""
    raiz = ET.parse(os.path.join(PRESETS, ruta)).getroot()
    pista = raiz.find('.//instrumenttrack')
    instrumento = pista.find('instrument')
    if instrumento is None:  # los presets antiguos de Organic guardan el instrumento suelto
        suelto = next(hijo for hijo in pista if hijo.tag not in ('eldata', 'arpeggiator', 'midiport', 'chordcreator', 'fxchain'))
        instrumento = ET.Element('instrument', name=suelto.tag)
        instrumento.append(suelto)
    return instrumento, pista.find('eldata'), pista.find('fxchain'), int(pista.get('basenote', 57))


# --- Efectos ------------------------------------------------------------------------------------

def _efecto(nombre, controles, mezcla=1.0, encendido=True, autoquit=1):
    ef = _attrs(ET.Element('effect'), name=nombre, on=encendido, wet=mezcla, gate=0, autoquit=autoquit,
                autoquit_numerator=4, autoquit_denominator=4, syncmode=0)
    ef.append(controles)
    ET.SubElement(ef, 'key')
    return ef


def reverb(tamano=0.89, color=10000, entrada_db=0.0, salida_db=0.0, mezcla=1.0):
    """ReverbSC. tamano 0-1 (lo larga que es la cola); color, la frecuencia de corte de la cola."""
    return _efecto('reverbsc', _attrs(ET.Element('ReverbSCControls'), input_gain=entrada_db, size=tamano,
                                      color=color, output_gain=salida_db), mezcla, autoquit=8000)


def bitcrush(frecuencia=44100, niveles=256, ruido=0.0, entrada_db=0.0, salida_db=0.0, recorte_db=0.0,
             diferencia_estereo=0.0, mezcla=1.0):
    """Bitcrush: baja la frecuencia de muestreo y los niveles (la «profundidad»); ruido en %."""
    return _efecto('bitcrush', _attrs(ET.Element('bitcrushcontrols'), ingain=entrada_db, innoise=ruido,
                                      outgain=salida_db, outclip=recorte_db, rate=frecuencia,
                                      stereodiff=diferencia_estereo, levels=niveles, rateon=1, depthon=1), mezcla)


def filtro_doble(corte1=14000, reso1=0.5, tipo1=0, corte2=14000, reso2=0.5, tipo2=0, mezcla_filtros=-1.0,
                 activo2=False, mezcla=1.0):
    """Dual Filter. tipo: 0 paso bajo, 1 paso alto, 2 paso banda... mezcla_filtros -1 = solo el primero."""
    return _efecto('dualfilter', _attrs(ET.Element('DualFilterControls'), enabled1=1, filter1=tipo1, cut1=corte1,
                                        res1=reso1, gain1=100, enabled2=activo2, filter2=tipo2, cut2=corte2,
                                        res2=reso2, gain2=100, mix=mezcla_filtros), mezcla)


def retardo(segundos=0.5, realimentacion=0.3, salida_db=0.0, mezcla=0.3):
    """Delay: el eco (segundos), cuánto se repite (0-1)."""
    return _efecto('delay', _attrs(ET.Element('Delay'), DelayTimeSamples=segundos, FeebackAmount=realimentacion,
                                   LfoFrequency=2, LfoAmount=0, OutGain=salida_db, syncmode=0,
                                   DelayTimeSamples_numerator=4, DelayTimeSamples_denominator=4,
                                   LfoFrequency_numerator=4, LfoFrequency_denominator=4,
                                   LfoAmount_numerator=4, LfoAmount_denominator=4), mezcla, autoquit=8000)


def amplificador(volumen=100, pan=0):
    return _efecto('amplifier', _attrs(ET.Element('AmplifierControls'), volume=volumen, pan=pan, left=100, right=100))


def realce_graves(frecuencia=100, ganancia=1.0, razon=2.0):
    return _efecto('bassbooster', _attrs(ET.Element('bassboostercontrols'), freq=frecuencia, gain=ganancia, ratio=razon))


def anchura_estereo(anchura=40):
    return _efecto('stereoenhancer', _attrs(ET.Element('stereoenhancercontrols'), width=anchura))


def vibrato(segundos=0.004, hz=0.5):
    """Un eco muy corto que oscila, sin el sonido limpio: la nota se tuerce arriba y abajo (bajo el agua,
    una cinta vieja). segundos: cuánto se mueve; hz: cada cuánto."""
    return _efecto('delay', _attrs(ET.Element('Delay'), DelayTimeSamples=0.01, FeebackAmount=0, LfoFrequency=hz,
                                   LfoAmount=segundos, OutGain=0, syncmode=0,
                                   DelayTimeSamples_numerator=4, DelayTimeSamples_denominator=4,
                                   LfoFrequency_numerator=4, LfoFrequency_denominator=4,
                                   LfoAmount_numerator=4, LfoAmount_denominator=4), 1.0, autoquit=8000)


def cadena(efectos):
    el = _attrs(ET.Element('fxchain'), numofeffects=len(efectos), enabled=bool(efectos))
    for ef in efectos:
        el.append(ef)
    return el


# --- Instrumentos medidos -----------------------------------------------------------------------
# La nota en que está grabada cada muestra se midió en su espectro (Musica/LEEME.md): así suenan en
# la nota escrita. Cada uno devuelve lo que necesita una pista: p.pista('Piano', **piano(), ...).

def _muestra(ruta, raiz, cents=0, ataque=0.0, liberacion=0.1, nivel=1.0, decaimiento=0.0, **filtro):
    return dict(instrumento=muestra(ruta), basenote=raiz, cents=cents,
                envolventes=eldata(vol=env_volumen(ataque=ataque, decaimiento=decaimiento, nivel=nivel,
                                                   liberacion=liberacion), **filtro))


def piano(liberacion=0.4):
    """instruments/piano02: un piano de verdad, en Do 3, que suena 8 s."""
    return _muestra('instruments/piano02.ogg', 'C3', liberacion=liberacion)


def piano_corto():
    """instruments/piano01: un piano en Do 4 que se apaga en menos de un segundo."""
    return _muestra('instruments/piano01.ogg', 'C4', liberacion=0.2)


def cuerdas(ataque=0.45, liberacion=0.8):
    """stringsnpads/strings01: cuerdas en Do 4 (un poco bajas), 6 s."""
    return _muestra('stringsnpads/strings01.ogg', 'C4', cents=8, ataque=ataque, liberacion=liberacion)


def coro(ataque=0.3, liberacion=0.7):
    """stringsnpads/chorus02: un coro («aah») en La 3, que crece en 8 s."""
    return _muestra('stringsnpads/chorus02.ogg', 'A3', cents=8, ataque=ataque, liberacion=liberacion)


def trompeta(ataque=0.01, liberacion=0.18):
    """instruments/trumpet01: una trompeta en La 4, 3 s. Trae un zumbido de 50 Hz: se le quita."""
    return _muestra('instruments/trumpet01.ogg', 'A4', cents=6, ataque=ataque, liberacion=liberacion,
                    fcut=160, fres=0.5, ftype=1, filtro=True)


def organo(ataque=0.02, liberacion=0.35):
    """instruments/church_organ01: un órgano de iglesia en La 3 (con 4' y 2'), 8 s."""
    return _muestra('instruments/church_organ01.ogg', 'A3', cents=2, ataque=ataque, liberacion=liberacion)


def pedal(ataque=0.05, liberacion=0.4):
    """instruments/church_organ02: un órgano en La 2 con sus octavas, 8 s: el pedal."""
    return _muestra('instruments/church_organ02.ogg', 'A2', cents=5, ataque=ataque, liberacion=liberacion)


def campana(liberacion=1.0):
    """misc/dong02: una campana en Re 3 (se toca una octava más grave para que sea de torre)."""
    return _muestra('misc/dong02.ogg', 'D3', liberacion=liberacion)


def caja():
    return _muestra('drums/snare_acoustic01.ogg', 57, liberacion=0.05)


def bombo():
    """drums/bassdrum_acoustic02: un bombo grande de orquesta, que retumba 2 s."""
    return _muestra('drums/bassdrum_acoustic02.ogg', 57, liberacion=0.3)


def timbal():
    """drums/tom_low01: un tom en Sol 2 algo alto; más grave, hace de timbal."""
    return _muestra('drums/tom_low01.ogg', 'G2', cents=-28, liberacion=0.3)


def platillo():
    return _muestra('drums/crash01.ogg', 57, liberacion=0.6)


def golpe(ruta, raiz=57, liberacion=0.1, **kwargs):
    """Cualquier muestra de percusión o efecto, tocada en su tono (o transportada desde «raiz»)."""
    return _muestra(ruta, raiz, liberacion=liberacion, **kwargs)


def caja_de_musica(decaimiento=1.6):
    """Una caja de música sintetizada: la fundamental y dos parciales agudos y algo desafinados, que se
    apagan solos."""
    return dict(instrumento=triple([oscilador(SENO, 60), oscilador(SENO, 18, grueso=24, fino_izq=4, fino_der=4),
                                    oscilador(SENO, 8, grueso=29, fino_izq=-7, fino_der=-7)]),
                envolventes=eldata(vol=env_volumen(decaimiento=decaimiento, nivel=0.0, liberacion=0.6)))


def campanas_tubulares():
    """Mallets (STK): las campanas tubulares, 4 s."""
    ins = ET.Element('instrument', name='malletsstk')
    ET.SubElement(ins, 'malletsstk', preset='9', spread='0', version='1')
    return dict(instrumento=ins, envolventes=eldata())


def lb302(corte=0.4, resonancia=0.8, envolvente=0.5, caida=0.2, distorsion=0.5, forma=0, deslizar=0.0):
    """LB302: el bajo ácido de LMMS, con su distorsión (forma 0 = sierra). Es monofónico."""
    ins = ET.Element('instrument', name='lb302')
    _attrs(ET.SubElement(ins, 'lb302'), vcf_cut=corte, vcf_res=resonancia, vcf_mod=envolvente, vcf_dec=caida,
           dist=distorsion, shape=forma, slide=deslizar, slide_dec=0.6, db24=0, dead=0)
    return dict(instrumento=ins, envolventes=eldata())


# --- Proyecto -----------------------------------------------------------------------------------

class Pista:
    def __init__(self, nombre, instrumento, envolventes=None, canal=0, vol=100, pan=0, basenote=57,
                 efectos=None, efectos_xml=None, cents=0, ticks_compas=TICKS_COMPAS):
        self.nombre = nombre
        self.ticks_compas = ticks_compas
        self.instrumento = instrumento
        self.envolventes = envolventes if envolventes is not None else eldata()
        self.canal = canal
        self.vol = vol
        self.pan = pan
        self.basenote = basenote if isinstance(basenote, int) else tecla(basenote)
        self.efectos = efectos or []
        self.efectos_xml = efectos_xml
        self.cents = cents  # afinación fina (las muestras no siempre están en su nota justa)
        self.patrones = []  # (posición en ticks, [(pos, len, tecla, vol, pan)])

    def patron(self, compas, notas):
        """Un patrón que empieza en el compás (desde 0). notas: (tick dentro del patrón, duración en
        ticks, nota 'D4' o número, volumen 0-200[, pan])."""
        return self.en(compas * self.ticks_compas, notas)

    def en(self, tick, notas):
        """Un patrón que empieza en ese tick (para las piezas que se escriben en segundos)."""
        lista = []
        for n in notas:
            pos, dur, nota, vol = n[:4]
            pan = n[4] if len(n) > 4 else 0
            lista.append((int(round(pos)), int(round(dur)), nota if isinstance(nota, int) else tecla(nota), int(round(vol)), pan))
        if lista:
            self.patrones.append((int(round(tick)), lista))
        return self

    def xml(self):
        track = _attrs(ET.Element('track'), name=self.nombre, type=0, muted=0, solo=0)
        it = _attrs(ET.SubElement(track, 'instrumenttrack'), vol=self.vol, pan=self.pan, fxch=self.canal,
                    basenote=self.basenote, pitch=self.cents, pitchrange=1, usemasterpitch=1)
        it.append(self.instrumento)
        it.append(self.envolventes)
        ET.SubElement(it, 'chordcreator', chord='0', chordrange='1').set('chord-enabled', '0')
        _attrs(ET.SubElement(it, 'arpeggiator'), arp=0, arprange=1, arptime=100, arpgate=100, arpdir=0, arpmode=0,
               arptime_numerator=4, arptime_denominator=4, syncmode=0, arp__enabled=0)
        it.append(self.efectos_xml if self.efectos_xml is not None else cadena(self.efectos))
        for inicio, notas in self.patrones:
            fin = max(p + d for p, d, *_ in notas)
            largo = -(-fin // self.ticks_compas) * self.ticks_compas
            pat = _attrs(ET.SubElement(track, 'pattern'), name=self.nombre, pos=inicio, len=largo, steps=16, muted=0, type=1)
            for p, d, k, v, pan in sorted(notas):
                _attrs(ET.SubElement(pat, 'note'), pos=p, len=d, key=k, vol=v, pan=pan)
        return track


class Canal:
    def __init__(self, num, nombre, volumen=1.0, efectos=None, envios=None):
        self.num = num
        self.nombre = nombre
        self.volumen = volumen
        self.efectos = efectos or []
        # Si no se dice, va al máster entero.
        self.envios = envios if envios is not None else ([] if num == 0 else [(0, 1.0)])

    def xml(self):
        el = _attrs(ET.Element('fxchannel'), num=self.num, name=self.nombre, volume=self.volumen, muted=0, soloed=0)
        el.append(cadena(self.efectos))
        for canal, cantidad in self.envios:
            _attrs(ET.SubElement(el, 'send'), channel=canal, amount=cantidad)
        return el


class Proyecto:
    """
    Un proyecto de LMMS. Un bucle (lo que suena en un nivel) mide sus compases; una pieza que suena una
    vez (bucle=False, la música de una cinemática) mide sus segundos justos. numerador: los tiempos del
    compás (4 = 4/4, 3 = 3/4).
    """

    def __init__(self, bpm, compases=None, notas_del_proyecto='', numerador=4, segundos=None, bucle=True):
        self.bpm = bpm
        self.numerador = numerador
        self.ticks_compas = numerador * TICKS_NEGRA
        if segundos is not None:
            self.largo = self.t(segundos)
        else:
            self.largo = compases * self.ticks_compas
        self.compases = self.largo / self.ticks_compas
        self.bucle = bucle
        self.pistas = []
        self.canales = [Canal(0, 'Master')]
        self.notas_del_proyecto = notas_del_proyecto

    def t(self, segundos):
        """Los ticks de esos segundos. Tienen que caer en un tick justo: se elige el tempo para ello
        (a 75 BPM, un tick es 1/60 s)."""
        ticks = segundos * self.bpm * TICKS_NEGRA / 60.0
        if abs(ticks - round(ticks)) > 1e-6:
            raise ValueError(f'{segundos} s no caen en un tick a {self.bpm} BPM')
        return int(round(ticks))

    def canal(self, nombre, volumen=1.0, efectos=None, envios=None):
        c = Canal(len(self.canales), nombre, volumen, efectos, envios)
        self.canales.append(c)
        return c.num

    def pista(self, *args, **kwargs):
        p = Pista(*args, ticks_compas=self.ticks_compas, **kwargs)
        self.pistas.append(p)
        return p

    @property
    def segundos(self):
        return self.largo * 60.0 / (self.bpm * TICKS_NEGRA)

    def xml(self):
        raiz = _attrs(ET.Element('lmms-project'), version='1.0', creator='LMMS', creatorversion='1.2.2', type='song')
        _attrs(ET.SubElement(raiz, 'head'), bpm=self.bpm, timesig_numerator=self.numerador, timesig_denominator=4,
               mastervol=100, masterpitch=0)
        song = ET.SubElement(raiz, 'song')
        tc = ET.SubElement(song, 'trackcontainer', type='song')
        for p in self.pistas:
            tc.append(p.xml())
        mezclador = ET.SubElement(song, 'fxmixer')
        for c in self.canales:
            mezclador.append(c.xml())
        # La barra de arriba de LMMS: hasta dónde llega la pieza; encendida, es un bucle.
        _attrs(ET.SubElement(song, 'timeline'), lp0pos=0, lp1pos=self.largo, lpstate=self.bucle)
        ET.SubElement(song, 'controllers')
        if self.notas_del_proyecto:
            ET.SubElement(song, 'projectnotes').text = self.notas_del_proyecto
        return raiz

    def guardar(self, ruta):
        texto = ET.tostring(self.xml(), encoding='unicode')
        with open(ruta, 'w', encoding='utf-8', newline='\n') as f:
            f.write('<?xml version="1.0"?>\n<!DOCTYPE lmms-project>\n')
            f.write(_sangrar(texto))
            f.write('\n')


def _sangrar(texto):
    el = ET.fromstring(texto)
    ET.indent(el, space='  ')
    return ET.tostring(el, encoding='unicode')


# --- Exportar -----------------------------------------------------------------------------------

def configuracion():
    """
    La configuración con la que se llama a LMMS: una copia de la del usuario (~/.lmmsrc.xml) sin
    proyectos recientes. LMMS apunta en su configuración cada proyecto que exporta: con la del
    usuario, su lista de recientes se llenaría de proyectos temporales.
    """
    ruta = os.path.join(tempfile.gettempdir(), 'fragmentum_lmmsrc.xml')
    usuario = os.path.join(os.path.expanduser('~'), '.lmmsrc.xml')
    raiz = ET.parse(usuario).getroot() if os.path.exists(usuario) else ET.Element('lmms', version='1.2.2')
    for recientes in raiz.findall('recentfiles'):
        raiz.remove(recientes)
    with open(ruta, 'w', encoding='utf-8') as f:
        f.write('<?xml version="1.0"?>\n<!DOCTYPE lmms-config-file>\n')
        f.write(ET.tostring(raiz, encoding='unicode'))
    return ruta


def lmms(*argumentos):
    """Llama a LMMS sin ventana, con configuracion()."""
    return subprocess.run([LMMS, '-c', configuracion(), *argumentos], capture_output=True, text=True,
                          encoding='utf-8', errors='replace')


def renderizar(mmp, salida, formato='wav'):
    """LMMS sin ventana: el proyecto entero, sin la cola del final (como bucle). El WAV sale en coma
    flotante, para que lo que pase de 0 dB no se recorte antes de normalizarlo."""
    argumentos = ['render', mmp, '-f', formato, '-l', '-o', salida, '-s', '44100', '-b', '192']
    if formato == 'wav':
        argumentos.append('-a')
    r = lmms(*argumentos)
    if r.returncode != 0 or not os.path.exists(salida):
        raise RuntimeError(f'LMMS no pudo exportar {mmp}:\n{r.stdout}\n{r.stderr}')
    return salida
