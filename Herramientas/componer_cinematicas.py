"""
La música de las cinemáticas, con los sonidos que trae LMMS. Cada pieza suena una vez y dura lo que su
cinemática; sus golpes caen donde cambia el plano. Escribe en Musica/Cinematicas un proyecto por cinemática:

  Inicial            La cinemática inicial (80 s). Presenta el tema de Kael (Re menor), en el piano: «Hubo una
                     guerra» con tambores lejanos; el tema; se corta en seco con «Le sacaron todo lo que
                     pesaba»; un latido vuelve («lo que negó no desapareció»); las cuatro emociones toman forma,
                     una a una (un golpe de metal, la caja de música, un aliento, una campana); crece; y el tema,
                     frágil y agudo, para «alguien nuevo», hasta el título. Acaba en una pregunta: la cabeza del
                     tema, sin resolver.
  Jefe_<mundo>       La presentación del jefe (~9,8 s): crece sobre el primer plano y golpea con el primer plano
                     del jefe; acaba subiendo, porque detrás entra el combate.
  Derrota_<mundo>    La derrota (9 s): el golpe, que se apaga; y, en el segundo plano, la cabeza del tema de Kael.
  Cierre_<mundo>     El cierre del mundo (14 s): el tema de Kael con los instrumentos de ese mundo; cuando
                     Kael integra el fragmento, resuelve en Re mayor.

Los mundos: Ira, Tristeza, Miedo y Culpa. Las memorias de la Culpa no llevan música: el Anexo A pide
silencio. Para pasarlas al juego: exportar_musica.py (las deja en Assets/Audio/Musica/Cinematicas).

    python Herramientas/componer_cinematicas.py            # no pisa un .mmp que ya exista
    python Herramientas/componer_cinematicas.py --forzar   # lo vuelve a escribir (se pierde lo editado)
"""
import os
import sys

import musica_lmms as m

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARPETA = os.path.join(RAIZ, 'Musica', 'Cinematicas')

# El tema de Kael: ocho compases en Re menor. Lo presenta la cinemática inicial, vuelve en cada cierre
# y es el que el Anexo A pide para el final («versión distorsionada del tema principal»).
TEMA_KAEL = ['D3:q A3:q G3:h', 'F3:q. E3:e D3:h', 'Bb3:q A3:q F3:q G3:q', 'A3:w',
             'D3:q A3:q C4:h', 'Bb3:q. A3:e G3:h', 'F3:q G3:q E3:q C#3:q', 'D3:w']
ACORDES_KAEL = ['Dm', 'Dm', 'Bb', 'A', 'Dm', 'Gm', 'A7', 'Dm']
ACOMPANAMIENTO = {'Dm': 'D2+A2+F3', 'Bb': 'Bb1+F2+D3', 'A': 'A1+E2+C#3', 'Gm': 'G1+D2+Bb2', 'A7': 'A1+E2+G2+C#3',
                  'D': 'D2+A2+F#3+A3'}
# Lo que dura un plano (desde, hasta), en segundos, en las cinemáticas de jefe, derrota y cierre.
PLANOS_JEFE = {'Ira': ((0.5, 6.4), (6.9, 9.6)), 'Tristeza': ((0.3, 5.1), (5.3, 9.8)),
               'Miedo': ((0.3, 5.1), (5.3, 9.8)), 'Culpa': ((0.3, 4.8), (5.0, 9.8))}
DURACION_DERROTA = 9.0
DURACION_CIERRE = 14.0
INTEGRA = 8.0  # el plano de «Kael integra el Fragmento...»


def tema(desde, hasta=None):
    """Los compases del tema de Kael (desde 1), como una frase."""
    hasta = hasta or desde
    return ' | '.join(TEMA_KAEL[desde - 1:hasta])


class Partitura:
    """Las notas de cada pista de una cinemática, escritas en segundos; al final, un patrón por pista."""

    def __init__(self, proyecto):
        self.p = proyecto
        self.notas = {}

    def tick(self, segundos):
        """El tick más cercano (a 75 BPM, 1/60 s)."""
        return int(round(segundos * self.p.bpm * m.TICKS_NEGRA / 60.0))

    def toca(self, pista, segundo, duracion, nota, vol=100):
        n = nota if isinstance(nota, int) else m.tecla(nota)
        self.notas.setdefault(pista, []).append((self.tick(segundo), max(1, self.tick(duracion)), n, vol))

    def acorde(self, pista, segundo, duracion, acorde, vol=100):
        for altura in acorde.split('+'):
            self.toca(pista, segundo, duracion, altura, vol)

    def frase(self, pista, segundo, texto, vol=100, transporte=0, legato=0.95):
        base = self.tick(segundo)
        for pos, dur, k, v in m.frase(texto, vol=vol, transporte=transporte, legato=legato):
            self.notas.setdefault(pista, []).append((base + pos, dur, k, v))

    def cerrar(self):
        for pista, notas in self.notas.items():
            pista.en(0, notas)
        return self.p


def proyecto(segundos, notas, bpm=75):
    """Una pieza que suena una vez. A 75 BPM un tick es 1/60 s: cualquier décima de segundo cae justa."""
    p = m.Proyecto(bpm, segundos=segundos, bucle=False, notas_del_proyecto=notas)
    reverb = p.canal('Reverb', efectos=[m.reverb(tamano=0.92, color=6000)])
    return p, reverb


def canal(p, reverb, nombre, envio=0.4, volumen=1.0, efectos=None):
    return p.canal(nombre, volumen=volumen, envios=[(0, 1.0), (reverb, envio)], efectos=efectos)


# --- Los instrumentos de cada mundo -------------------------------------------------------------

def sinte_calido():
    """Un colchón de sierras sin agudos, que entra despacio (el fuego que se apaga)."""
    return dict(instrumento=m.triple([m.oscilador(m.SIERRA, 35, fino_izq=-6, fino_der=6), m.oscilador(m.SIERRA, 35, fino_izq=6, fino_der=-6),
                                      m.oscilador(m.SENO, 40, grueso=-12)]),
                envolventes=m.eldata(vol=m.env_volumen(ataque=1.0, liberacion=1.2), fcut=900, fres=0.5, ftype=0, filtro=True))


def sinte_roto():
    """Las sierras del Mundo 1, por un bitcrush (se pone en su canal)."""
    return dict(instrumento=m.triple([m.oscilador(m.SIERRA, 40, fino_izq=-8, fino_der=8), m.oscilador(m.SIERRA, 40, fino_izq=7, fino_der=-7),
                                      m.oscilador(m.SIERRA, 30, grueso=-12)]),
                envolventes=m.eldata(vol=m.env_volumen(ataque=0.005, decaimiento=1.5, nivel=0.4, liberacion=0.8)))


def zumbido(corte=400, ataque=1.5):
    return dict(instrumento=m.triple([m.oscilador(m.SIERRA, 40), m.oscilador(m.SENO, 60, grueso=-12)]),
                envolventes=m.eldata(vol=m.env_volumen(ataque=ataque, liberacion=0.8), fcut=corte, fres=0.7, ftype=0, filtro=True))


def ruido_que_sube(segundos):
    return dict(instrumento=m.triple([m.oscilador(m.RUIDO, 80)]),
                envolventes=m.eldata(vol=m.env_volumen(ataque=segundos, liberacion=0.1),
                                     cut=m.envolvente('elcut', att=m.segundos(segundos), sustain=1.0, rel=0.1, amt=1.0),
                                     fcut=200, fres=0.9, ftype=0, filtro=True))


def bitcrush_roto():
    return [m.bitcrush(frecuencia=11025, niveles=24, entrada_db=12, recorte_db=-6), m.filtro_doble(corte1=3500, reso1=0.6, tipo1=0)]


def bajo_el_agua(corte=3000, vibrato=False):
    return ([m.vibrato(segundos=0.004, hz=0.45)] if vibrato else []) + [m.filtro_doble(corte1=corte, reso1=0.6, tipo1=0)]


# =============================================================================================
# La cinemática inicial
# =============================================================================================

def inicial():
    p, reverb = proyecto(80.0, bpm=60, notas=(
        'La cinemática inicial (80 s, 60 BPM: una negra es un segundo). Presenta el tema de Kael, en Re menor. '
        '0-7: un colchón grave. 7-15 («Hubo una guerra»): tambores lejanos. 15-34: el tema en el piano, con cuerdas. '
        '34 («Le sacaron todo lo que pesaba»): se corta en seco. 41: vuelve un latido. 49-56: las cuatro emociones '
        'toman forma (Ira, Tristeza, Miedo, Culpa). 56-64: crece. 64: el tema, agudo y frágil. 71: el título. '
        '76: la pregunta, sin resolver.'))
    t = Partitura(p)
    teclas = canal(p, reverb, 'Piano', envio=0.5)
    cuerdas = canal(p, reverb, 'Cuerdas', envio=0.5, volumen=0.6)
    fondo = canal(p, reverb, 'Fondo', envio=0.3, volumen=0.6)
    golpes = canal(p, reverb, 'Golpes', envio=0.45, volumen=0.8)
    emociones = canal(p, reverb, 'Emociones', envio=0.6, volumen=1.0)

    piano = p.pista('Piano', **m.piano(liberacion=1.5), canal=teclas, vol=70)
    piano_agudo = p.pista('Piano agudo', **m.piano_corto(), canal=teclas, vol=60)
    colchon = p.pista('Colchon', **sinte_calido(), canal=fondo, vol=70)
    cuerda = p.pista('Cuerdas', **m.cuerdas(ataque=0.8, liberacion=1.5), canal=cuerdas, vol=70)
    racimo = p.pista('Racimo', **m.cuerdas(ataque=2.5, liberacion=1.0), canal=cuerdas, vol=60)
    bombo = p.pista('Gran casa', **m.bombo(), canal=golpes, vol=70)
    caja = p.pista('Caja', **m.caja(), canal=golpes, vol=45)
    latido = p.pista('Latido', **m.timbal(), canal=golpes, vol=70)
    ira = p.pista('Ira', **m.golpe('misc/hit01.ogg', liberacion=0.5), canal=emociones, vol=60)
    metal = p.pista('Ira metal', **m.golpe('misc/metalish_dong01.ogg', 'C4', cents=5), canal=emociones, vol=70)
    tristeza = p.pista('Tristeza', **m.caja_de_musica(), canal=emociones, vol=70)
    miedo = p.pista('Miedo', **m.golpe('misc/breath01.ogg', liberacion=0.6), canal=emociones, vol=90)
    culpa = p.pista('Culpa', **m.campana(), canal=emociones, vol=90)

    # 0-7: el colchón grave; el piano, una nota en la oscuridad
    t.acorde(colchon, 0.0, 14.5, 'D2+A2', 60)
    t.toca(piano, 1.0, 4.0, 'D2', 55)
    t.toca(piano_agudo, 3.0, 2.0, 'A5', 45)
    t.toca(piano, 5.0, 2.0, 'D2', 50)
    # 7-15: «Hubo una guerra»: la gran casa lejos, y una caja que redobla
    for segundo, vol in ((7.0, 110), (9.0, 90), (11.0, 95), (13.0, 80)):
        t.toca(bombo, segundo, 2.0, 50, vol)
    for i in range(32):
        t.toca(caja, 11.0 + i * 0.125, 0.1, 57, 30 + i * 2)
    # 15-34: el tema en el piano; las cuerdas entran con «Le dolía»
    t.frase(piano, 15.0, tema(1, 4), vol=95, legato=1.0)
    for c, acorde in enumerate(ACORDES_KAEL[:4]):
        t.acorde(piano, 15.0 + 4 * c, 4.0, ACOMPANAMIENTO[acorde].split('+')[0], 60)
    t.acorde(cuerda, 23.0, 4.0, 'D3+F3+Bb3', 70)
    t.acorde(cuerda, 27.0, 4.0, 'C#3+E3+A3', 75)
    t.acorde(cuerda, 31.0, 3.0, 'D3+F3+A3', 70)
    t.frase(piano, 31.0, 'D3:q A3:q C4:q', vol=95, legato=1.0)  # el quinto compás... y se corta
    t.toca(piano, 31.0, 3.0, 'D2', 60)
    # 34-41: «Le sacaron todo lo que pesaba»: casi nada; notas sueltas, muy agudas
    t.toca(piano_agudo, 36.0, 1.0, 'D6', 40)
    t.toca(piano_agudo, 38.5, 1.0, 'A5', 35)
    # 41-56: «Pero lo que negó no desapareció»: un latido, y racimos graves que crecen
    for i in range(15):
        segundo = 41.0 + i
        t.toca(latido, segundo, 0.5, 'D2', 50 + 3 * i)
        t.toca(latido, segundo + 0.25, 0.5, 'D2', 35 + 2 * i)
    t.acorde(racimo, 41.0, 7.5, 'C#3+D3+A3', 60)
    t.acorde(colchon, 41.0, 23.0, 'D1+A1', 70)
    # 49-56: «Tomó forma»: las cuatro emociones, una a una
    t.toca(ira, 49.0, 2.0, 57, 100)
    t.toca(metal, 49.0, 0.5, 'E5', 90)
    t.toca(tristeza, 50.75, 1.5, 'B5', 95)
    t.toca(tristeza, 51.25, 1.5, 'F#5', 80)
    t.toca(miedo, 52.5, 2.5, 52, 100)
    t.toca(culpa, 54.25, 3.0, 'D2', 110)
    t.acorde(racimo, 49.0, 7.0, 'D3+Eb3+A3', 65)
    # 56-64: crece hasta el corte
    t.acorde(racimo, 56.0, 8.0, 'C#3+D3+G#3+A3', 85)
    for i in range(32):
        t.toca(latido, 56.0 + i * 0.25, 0.25, 'D2', 50 + i * 2)
    for segundo in (56.0, 60.0, 62.0, 63.0):
        t.toca(bombo, segundo, 1.0, 50, 100)
    # 64-71: «hicieron a alguien nuevo»: el tema, agudo y frágil
    t.frase(piano_agudo, 64.0, 'D4:q A4:q G4:h | F4:q E4:q D4:q', vol=80, legato=1.0)
    t.toca(piano, 64.0, 7.0, 'D3', 45)
    # 71-76: el título: la cadencia del tema, fuerte
    t.frase(piano, 71.0, 'F3:q G3:q E3:q C#3:q D3:q', vol=110, legato=1.0)
    t.acorde(piano, 71.0, 4.0, 'A1+E2', 90)
    t.acorde(piano, 75.0, 1.0, 'D2+A2', 95)
    t.acorde(cuerda, 71.0, 4.0, 'E3+G3+C#4', 90)
    t.acorde(cuerda, 75.0, 1.0, 'D3+F3+A3+D4', 95)
    t.toca(bombo, 71.0, 2.0, 50, 110)
    t.toca(bombo, 75.0, 1.0, 50, 100)
    t.acorde(colchon, 71.0, 5.0, 'D2+A2', 80)
    # 76-80: «¿Qué es lo que no estás mirando?»: la cabeza del tema, sin resolver
    t.toca(piano_agudo, 76.0, 1.0, 'D5', 70)
    t.toca(piano_agudo, 77.0, 1.0, 'A4', 60)
    t.toca(piano_agudo, 78.0, 2.0, 'G4', 55)
    t.acorde(cuerda, 76.0, 4.0, 'G3+Bb3+D4', 45)
    return t.cerrar()


# =============================================================================================
# Las presentaciones de los jefes
# =============================================================================================

def jefe_ira():
    (a0, a1), (b0, b1) = PLANOS_JEFE['Ira']
    p, reverb = proyecto(b1, notas='La presentación de la Centinela Ardiente (9,6 s). La forja respira; golpe '
                                   'con su primer plano; redoble hasta el flash final.')
    t = Partitura(p)
    golpes = canal(p, reverb, 'Golpes', envio=0.3, efectos=[m.bitcrush(frecuencia=22050, niveles=64, entrada_db=6, recorte_db=-2)])
    sinte = canal(p, reverb, 'Sinte', envio=0.2, volumen=0.5, efectos=bitcrush_roto())
    fondo = canal(p, reverb, 'Fondo', envio=0.2)
    forja = p.pista('Forja', **zumbido(corte=350), canal=fondo, vol=80)
    latido = p.pista('Latido', **m.golpe('drums/kick_distorted01.ogg'), canal=golpes, vol=45)
    raspa = p.pista('Raspa', **m.golpe('effects/filter_sweep01.ogg', liberacion=0.4), canal=golpes, vol=70)
    golpe = p.pista('Golpe', **m.golpe('misc/hit01.ogg', liberacion=0.5), canal=golpes, vol=90)
    estallido = p.pista('Estallido', **m.golpe('effects/explode01.ogg', liberacion=0.5), canal=golpes, vol=70)
    platillo = p.pista('Platillo', **m.golpe('drums/crash02.ogg', liberacion=0.6), canal=golpes, vol=60)
    caja = p.pista('Caja', **m.golpe('drums/snare_harsh01.ogg', liberacion=0.1), canal=golpes, vol=55)
    acorde = p.pista('Sinte', **sinte_roto(), canal=sinte, vol=85)
    sube = p.pista('Ruido', **ruido_que_sube(b1 - b0 - 0.6), canal=fondo, vol=70)

    t.acorde(forja, 0.0, a1, 'E1+B1', 90)
    for segundo, vol in ((1.2, 70), (2.4, 75), (3.4, 80), (4.3, 85), (5.1, 90), (5.8, 95), (6.3, 100)):
        t.toca(latido, segundo, 0.4, 57, vol)
    t.toca(raspa, 2.0, 3.0, 45, 80)
    # El primer plano de la Centinela
    t.toca(golpe, b0, 2.0, 57, 115)
    t.toca(estallido, b0, 2.5, 50, 100)
    t.toca(platillo, b0, 2.5, 57, 90)
    t.acorde(acorde, b0, b1 - b0, 'E2+B2+E3', 100)
    t.acorde(forja, b0, b1 - b0, 'E1+B1', 100)
    # Redoble hasta el flash
    n = int((b1 - (b0 + 0.7)) / 0.1)
    for i in range(n):
        t.toca(caja, b0 + 0.7 + i * 0.1, 0.1, 57, 45 + i * 3)
    t.toca(sube, b0 + 0.6, b1 - b0 - 0.6, 'A4', 90)
    return t.cerrar()


def jefe_tristeza():
    (a0, a1), (b0, b1) = PLANOS_JEFE['Tristeza']
    p, reverb = proyecto(b1, notas='La presentación de la Doncella del Lamento (9,8 s). La caja de música, bajo el '
                                   'agua; con su primer plano, el coro; las cuerdas suben hasta el combate.')
    t = Partitura(p)
    caja_musica = canal(p, reverb, 'Caja de musica', envio=0.6, volumen=0.7, efectos=bajo_el_agua(3500, vibrato=True))
    piano_c = canal(p, reverb, 'Piano', envio=0.7, volumen=0.7, efectos=bajo_el_agua(4000))
    coro_c = canal(p, reverb, 'Coro', envio=0.6, volumen=0.6, efectos=bajo_el_agua(3500))
    cuerdas_c = canal(p, reverb, 'Cuerdas', envio=0.5, volumen=0.6, efectos=bajo_el_agua(4000))
    golpes = canal(p, reverb, 'Golpes', envio=0.4, volumen=0.8)
    caja = p.pista('Caja de musica', **m.caja_de_musica(), canal=caja_musica, vol=85)
    piano = p.pista('Piano', **m.piano(liberacion=1.0), canal=piano_c, vol=60)
    coro = p.pista('Coro', **m.coro(ataque=0.8, liberacion=0.8), canal=coro_c, vol=85)
    cuerdas = p.pista('Cuerdas', **m.cuerdas(ataque=0.3, liberacion=0.5), canal=cuerdas_c, vol=80)
    timbal = p.pista('Timbal', **m.timbal(), canal=golpes, vol=90)
    campana = p.pista('Campana', **m.campana(), canal=golpes, vol=80)

    arpegio = ['B4', 'D5', 'F#5', 'B5', 'F#5', 'D5', 'A#4', 'C#5', 'F#5', 'C#5', 'B4']
    for i, nota in enumerate(arpegio):
        t.toca(caja, a0 + 0.4 * i, 1.0, nota, 90 - 3 * i)
    t.toca(piano, a0, a1 - a0, 'B1', 70)
    # Su primer plano: el coro, el piano grave y una campana hundida
    t.acorde(coro, b0, b1 - b0, 'B3+D4+F#4', 95)
    t.acorde(piano, b0, b1 - b0, 'B1+F#2', 85)
    t.toca(campana, b0, 3.0, 'B2', 100)
    t.toca(timbal, b0, 1.0, 'B2', 110)
    # Las cuerdas suben; el timbal, en corcheas que crecen
    for i, nota in enumerate(['B3', 'C#4', 'D4', 'E4', 'F#4']):
        t.toca(cuerdas, b0 + 2.2 + 0.46 * i, 0.46, nota, 80 + 6 * i)
    for i in range(10):
        t.toca(timbal, b0 + 2.2 + 0.23 * i, 0.23, 'F#2', 60 + 5 * i)
    return t.cerrar()


def jefe_miedo():
    (a0, a1), (b0, b1) = PLANOS_JEFE['Miedo']
    p, reverb = proyecto(b1, notas='La presentación del Espectro del Pavor (9,8 s). Casi nada: un zumbido, un aliento, '
                                   'un latido que se acelera; con su primer plano, un racimo de cuerdas y toms irregulares.')
    t = Partitura(p)
    fondo = canal(p, reverb, 'Fondo', envio=0.3)
    bosque = canal(p, reverb, 'Bosque', envio=0.7)
    cuerdas_c = canal(p, reverb, 'Cuerdas', envio=0.5, volumen=0.7, efectos=[m.filtro_doble(corte1=3000, reso1=0.5, tipo1=0)])
    golpes = canal(p, reverb, 'Golpes', envio=0.3)
    drone = p.pista('Zumbido', **zumbido(corte=250, ataque=2.0), canal=fondo, vol=70)
    aliento = p.pista('Aliento', **m.golpe('misc/breath01.ogg', liberacion=0.6), canal=bosque, vol=110)
    rama = p.pista('Rama', **m.golpe('drums/wood01.ogg'), canal=bosque, vol=110)
    latido = p.pista('Latido', **m.timbal(), canal=golpes, vol=95)
    tom = p.pista('Tom medio', **m.golpe('drums/tom_mid01.ogg', 'A2', cents=-20, liberacion=0.2), canal=golpes, vol=100)
    boom = p.pista('Golpe grave', **m.bombo(), canal=golpes, vol=80)
    racimo = p.pista('Racimo', **m.cuerdas(ataque=0.02, liberacion=1.0), canal=cuerdas_c, vol=100)

    t.acorde(drone, a0, b1 - a0, 'C#1', 90)
    t.toca(aliento, 0.8, 3.0, 52, 100)
    t.toca(rama, 2.5, 0.5, 55, 100)
    for segundo in (1.6, 2.9, 4.0, 4.8):
        t.toca(latido, segundo, 0.4, 'C#2', 100)
        t.toca(latido, segundo + 0.22, 0.4, 'C#2', 70)
    # Su primer plano
    t.acorde(racimo, b0, 1.6, 'C#3+D3+G3+G#3', 115)
    t.toca(boom, b0, 3.0, 50, 115)
    # Toms en grupos de 3 y 2 que se aprietan hasta el combate
    pos, i = b0 + 0.6, 0
    grupos = [3, 2, 3, 3, 2, 3, 2, 2, 2, 2, 1, 1, 1, 1]
    paso = 0.2
    for g in grupos:
        if pos >= b1 - 0.05:
            break
        t.toca(latido if i % 2 == 0 else tom, pos, 0.3, 'C#2' if i % 2 == 0 else 'E2', 85 + 2 * i)
        pos += g * paso
        paso = max(0.08, paso * 0.93)
        i += 1
    t.acorde(racimo, b1 - 2.0, 2.0, 'C#3+G3', 80)
    return t.cerrar()


def jefe_culpa():
    (a0, a1), (b0, b1) = PLANOS_JEFE['Culpa']
    p, reverb = proyecto(b1, notas='La presentación del Juez Silente (9,8 s). El pedal del órgano y la campana sobre la '
                                   'plaza; con su primer plano, el órgano entero en Re menor, y el bajo que baja (Re, Do, '
                                   'Si bemol, La) con un redoble de caja, hasta el primer compás de su tema.')
    t = Partitura(p)
    organo_c = canal(p, reverb, 'Organo', envio=0.5, efectos=[m.filtro_doble(corte1=90, reso1=0.5, tipo1=1)])
    golpes = canal(p, reverb, 'Golpes', envio=0.35, volumen=0.8)
    organo = p.pista('Organo', **m.organo(), canal=organo_c, vol=60)
    pedal = p.pista('Pedal', **m.pedal(), canal=organo_c, vol=65)
    campana = p.pista('Campana', **m.campana(), canal=golpes, vol=100)
    bombo = p.pista('Gran casa', **m.bombo(), canal=golpes, vol=80)
    caja = p.pista('Caja', **m.caja(), canal=golpes, vol=60)

    t.toca(pedal, a0, b0 - a0, 'D2', 80)
    t.toca(campana, a0, 2.4, 'D2', 110)
    t.toca(campana, a0 + 2.4, 2.4, 'D2', 90)
    # Su primer plano: el órgano entero
    t.acorde(organo, b0, 1.6, 'D3+F3+A3+D4', 110)
    t.toca(pedal, b0, 1.6, 'D2', 100)
    t.toca(bombo, b0, 2.0, 57, 115)
    t.toca(campana, b0, 3.0, 'D2', 110)
    # El bajo que baja, cada vez más deprisa, y el redoble
    for (segundo, dur, acorde, bajo) in ((b0 + 1.6, 1.2, 'C3+E3+G3+C4', 'C2'), (b0 + 2.8, 0.8, 'Bb2+D3+F3+Bb3', 'Bb1'),
                                         (b0 + 3.6, b1 - b0 - 3.6, 'A2+C#3+E3+A3', 'A1')):
        t.acorde(organo, segundo, dur, acorde, 105)
        t.toca(pedal, segundo, dur, bajo, 100)
    n = int((b1 - (b0 + 3.0)) / 0.1)
    for i in range(n):
        t.toca(caja, b0 + 3.0 + i * 0.1, 0.1, 57, 40 + i * 4)
    return t.cerrar()


# =============================================================================================
# Las derrotas
# =============================================================================================

def cabeza_del_tema(t, pista, segundo, transporte=0, vol=90):
    """Re, La, Sol: la cabeza del tema de Kael, que asoma."""
    t.frase(pista, segundo, 'D3:q A3:q G3:h', vol=vol, transporte=transporte, legato=1.0)


def derrota_ira():
    p, reverb = proyecto(DURACION_DERROTA, notas='La derrota de la Centinela (9 s). El golpe, y la forja que se para; en '
                                                 'el segundo plano, la cabeza del tema de Kael en el piano.')
    t = Partitura(p)
    golpes = canal(p, reverb, 'Golpes', envio=0.4)
    sinte = canal(p, reverb, 'Sinte', envio=0.3, volumen=0.5, efectos=bitcrush_roto())
    fondo = canal(p, reverb, 'Fondo', envio=0.4)
    teclas = canal(p, reverb, 'Piano', envio=0.6)
    golpe = p.pista('Golpe', **m.golpe('misc/hit01.ogg', liberacion=0.5), canal=golpes, vol=90)
    estallido = p.pista('Estallido', **m.golpe('effects/explode01.ogg', liberacion=0.5), canal=golpes, vol=80)
    platillo = p.pista('Platillo', **m.golpe('drums/crash02.ogg', liberacion=0.6), canal=golpes, vol=60)
    metal = p.pista('Metal', **m.golpe('misc/metalish_dong01.ogg', 'C4', cents=5), canal=golpes, vol=90)
    acorde = p.pista('Sinte', **sinte_roto(), canal=sinte, vol=80)
    bajo = p.pista('Bajo', **zumbido(corte=500, ataque=0.01), canal=fondo, vol=70)
    calido = p.pista('Colchon', **sinte_calido(), canal=fondo, vol=55)
    piano = p.pista('Piano', **m.piano(liberacion=1.5), canal=teclas, vol=70)

    t.toca(golpe, 0.0, 2.0, 57, 115)
    t.toca(estallido, 0.0, 2.5, 47, 100)
    t.toca(platillo, 0.0, 2.5, 57, 90)
    t.acorde(acorde, 0.0, 3.5, 'E2+B2+E3', 100)
    for i, nota in enumerate(['E2', 'D2', 'C2', 'B1']):
        t.toca(bajo, 0.8 * i, 0.8, nota, 90 - 8 * i)
    for segundo, vol in ((1.5, 90), (2.3, 75), (3.3, 60), (4.4, 45)):
        t.toca(metal, segundo, 0.4, 'E5', vol)
    cabeza_del_tema(t, piano, 4.5, vol=85)
    t.acorde(calido, 4.5, 4.5, 'D2+A2+F3', 70)
    return t.cerrar()


def derrota_tristeza():
    p, reverb = proyecto(DURACION_DERROTA, notas='La derrota de la Doncella (9 s). El coro cae y la caja de música se queda '
                                                 'sin cuerda; en el segundo plano, la cabeza del tema de Kael y un Re mayor.')
    t = Partitura(p)
    caja_musica = canal(p, reverb, 'Caja de musica', envio=0.6, volumen=0.7, efectos=bajo_el_agua(3500, vibrato=True))
    coro_c = canal(p, reverb, 'Coro', envio=0.6, volumen=0.6, efectos=bajo_el_agua(3500))
    piano_c = canal(p, reverb, 'Piano', envio=0.8, volumen=0.8, efectos=bajo_el_agua(4500))
    cuerdas_c = canal(p, reverb, 'Cuerdas', envio=0.6, volumen=0.5)
    golpes = canal(p, reverb, 'Golpes', envio=0.4, volumen=0.8)
    caja = p.pista('Caja de musica', **m.caja_de_musica(), canal=caja_musica, vol=85)
    coro = p.pista('Coro', **m.coro(ataque=0.05, liberacion=2.0), canal=coro_c, vol=90)
    piano = p.pista('Piano', **m.piano(liberacion=1.5), canal=piano_c, vol=70)
    cuerdas = p.pista('Cuerdas', **m.cuerdas(ataque=1.2, liberacion=1.5), canal=cuerdas_c, vol=70)
    timbal = p.pista('Timbal', **m.timbal(), canal=golpes, vol=90)
    campana = p.pista('Campana', **m.campana(), canal=golpes, vol=80)

    t.acorde(coro, 0.0, 2.0, 'B3+D4+F#4', 100)
    t.acorde(coro, 2.0, 2.5, 'G3+B3+D4', 80)
    t.toca(timbal, 0.0, 1.0, 'B2', 110)
    t.toca(campana, 0.0, 3.0, 'B2', 90)
    for segundo, nota in ((0.2, 'B5'), (0.8, 'F#5'), (1.6, 'D5'), (2.6, 'B4'), (3.8, 'F#4')):
        t.toca(caja, segundo, 1.2, nota, 85)
    cabeza_del_tema(t, piano, 4.5, vol=80)
    t.acorde(cuerdas, 4.5, 4.5, 'D3+F#3+A3', 75)
    return t.cerrar()


def derrota_miedo():
    p, reverb = proyecto(DURACION_DERROTA, notas='La derrota del Espectro (9 s). Un golpe grave y el racimo que se apaga; '
                                                 'el latido se calma; en el segundo plano, luz: cuerdas en Re mayor y la '
                                                 'cabeza del tema de Kael.')
    t = Partitura(p)
    fondo = canal(p, reverb, 'Fondo', envio=0.3)
    cuerdas_c = canal(p, reverb, 'Cuerdas', envio=0.6, volumen=0.7)
    golpes = canal(p, reverb, 'Golpes', envio=0.35)
    teclas = canal(p, reverb, 'Piano', envio=0.6)
    boom = p.pista('Golpe grave', **m.bombo(), canal=golpes, vol=85)
    latido = p.pista('Latido', **m.timbal(), canal=golpes, vol=85)
    drone = p.pista('Zumbido', **zumbido(corte=250, ataque=0.01), canal=fondo, vol=70)
    racimo = p.pista('Racimo', **m.cuerdas(ataque=0.02, liberacion=2.0), canal=cuerdas_c, vol=95)
    luz = p.pista('Luz', **m.cuerdas(ataque=1.0, liberacion=1.5), canal=cuerdas_c, vol=80)
    piano = p.pista('Piano', **m.piano(liberacion=1.5), canal=teclas, vol=65)

    t.toca(boom, 0.0, 3.0, 45, 115)
    t.acorde(racimo, 0.0, 1.5, 'C#3+D3+G3+G#3', 110)
    t.toca(drone, 0.0, 4.5, 'C#1', 90)
    for segundo, vol in ((1.0, 90), (2.2, 75), (3.6, 60)):
        t.toca(latido, segundo, 0.4, 'C#2', vol)
        t.toca(latido, segundo + 0.25, 0.4, 'C#2', vol - 25)
    t.acorde(luz, 4.5, 4.5, 'D4+F#4+A4', 85)
    cabeza_del_tema(t, piano, 4.8, vol=75)
    return t.cerrar()


def derrota_culpa():
    p, reverb = proyecto(DURACION_DERROTA, notas='La derrota del Juez (9 s). El último acorde del órgano y la campana; en el '
                                                 'segundo plano, el órgano en Re mayor y la trompeta, lejos, con la cabeza '
                                                 'del tema de Kael.')
    t = Partitura(p)
    organo_c = canal(p, reverb, 'Organo', envio=0.55, efectos=[m.filtro_doble(corte1=90, reso1=0.5, tipo1=1)])
    golpes = canal(p, reverb, 'Golpes', envio=0.4, volumen=0.8)
    metales = canal(p, reverb, 'Trompeta', envio=0.8, volumen=0.5, efectos=[m.filtro_doble(corte1=4500, reso1=0.6, tipo1=0)])
    organo = p.pista('Organo', **m.organo(liberacion=1.5), canal=organo_c, vol=60)
    pedal = p.pista('Pedal', **m.pedal(liberacion=1.5), canal=organo_c, vol=60)
    campana = p.pista('Campana', **m.campana(), canal=golpes, vol=100)
    bombo = p.pista('Gran casa', **m.bombo(), canal=golpes, vol=80)
    trompeta = p.pista('Trompeta', **m.trompeta(), canal=metales, vol=80)

    t.acorde(organo, 0.0, 2.5, 'D3+F3+A3+D4', 110)
    t.toca(pedal, 0.0, 2.5, 'D2', 100)
    t.toca(bombo, 0.0, 2.0, 57, 110)
    for segundo, vol in ((0.0, 110), (1.5, 90), (3.0, 75)):
        t.toca(campana, segundo, 3.0, 'D2', vol)
    t.acorde(organo, 4.5, 4.5, 'D3+F#3+A3', 70)
    t.toca(pedal, 4.5, 4.5, 'D2', 70)
    cabeza_del_tema(t, trompeta, 4.8, transporte=12, vol=85)
    return t.cerrar()


# =============================================================================================
# Los cierres de los mundos: el tema de Kael, y Re mayor al integrar el fragmento
# =============================================================================================

def cierre(p, t, melodia, acompanamiento, transporte):
    """
    El tema de Kael sobre el primer plano (sus dos primeros compases) y su cadencia sobre el plano de «Kael
    integra el Fragmento...», que resuelve en Re mayor. transporte: semitonos sobre el tema escrito (en Re 3).
    """
    t.frase(melodia, 0.8, tema(1, 2), vol=95, transporte=transporte, legato=1.0)
    t.acorde(acompanamiento, 0.8, 6.4, ACOMPANAMIENTO['Dm'], 70)
    t.frase(melodia, INTEGRA, 'F3:q G3:q E3:q C#3:q', vol=95, transporte=transporte, legato=1.0)
    final = DURACION_CIERRE - INTEGRA - 3.2
    t.toca(melodia, INTEGRA + 3.2, final, m.tecla('D3') + transporte, 95)
    t.toca(melodia, INTEGRA + 3.2, final, m.tecla('F#3') + transporte, 70)
    t.acorde(acompanamiento, INTEGRA, 3.2, ACOMPANAMIENTO['A7'], 75)
    t.acorde(acompanamiento, INTEGRA + 3.2, final, ACOMPANAMIENTO['D'], 80)
    return t.cerrar()


def _cierre(notas):
    p, reverb = proyecto(DURACION_CIERRE, notas=notas)
    return p, reverb, Partitura(p)


def cierre_ira():
    p, reverb, t = _cierre('El cierre del Mundo 1 (14 s): un último golpe de metal, y el tema de Kael en el piano '
                                  'sobre un colchón cálido; resuelve en Re mayor al integrar el fragmento.')
    teclas = canal(p, reverb, 'Piano', envio=0.55)
    fondo = canal(p, reverb, 'Fondo', envio=0.4, volumen=0.6)
    golpes = canal(p, reverb, 'Golpes', envio=0.6, volumen=0.6)
    piano = p.pista('Piano', **m.piano(liberacion=1.5), canal=teclas, vol=75)
    calido = p.pista('Colchon', **sinte_calido(), canal=fondo, vol=70)
    metal = p.pista('Metal', **m.golpe('misc/dong03.ogg', 'D5', liberacion=1.0), canal=golpes, vol=70)

    t.toca(metal, 0.3, 2.5, 'E4', 90)  # el fuego que se apaga: el último golpe, grave y lejos
    return cierre(p, t, piano, calido, 0)


def cierre_tristeza():
    p, reverb, t = _cierre('El cierre del Mundo 2 (14 s): el tema de Kael en la caja de música, con el piano '
                                       'grave y mucha reverb; resuelve en Re mayor al integrar el fragmento.')
    caja_musica = canal(p, reverb, 'Caja de musica', envio=0.7, volumen=0.7, efectos=bajo_el_agua(4500, vibrato=True))
    piano_c = canal(p, reverb, 'Piano', envio=0.8, volumen=0.7)
    caja = p.pista('Caja de musica', **m.caja_de_musica(decaimiento=2.0), canal=caja_musica, vol=90)
    piano = p.pista('Piano', **m.piano(liberacion=1.5), canal=piano_c, vol=60)
    return cierre(p, t, caja, piano, 24)


def cierre_miedo():
    p, reverb, t = _cierre('El cierre del Mundo 3 (14 s): el tema de Kael en las cuerdas, que se aclaran; resuelve '
                                    'en Re mayor al integrar el fragmento.')
    cuerdas_c = canal(p, reverb, 'Cuerdas', envio=0.6, volumen=0.7)
    graves = canal(p, reverb, 'Graves', envio=0.5, volumen=0.5)
    cuerdas = p.pista('Cuerdas tema', **m.cuerdas(ataque=0.25, liberacion=1.0), canal=cuerdas_c, vol=90)
    fondo = p.pista('Cuerdas graves', **m.cuerdas(ataque=1.0, liberacion=1.5), canal=graves, vol=70)
    return cierre(p, t, cuerdas, fondo, 12)


def cierre_culpa():
    p, reverb, t = _cierre('El cierre del Mundo 4 (14 s): la campana; el tema de Kael en la trompeta de la marcha, '
                                    'ya limpia, sobre el órgano suave; resuelve en Re mayor al integrar el fragmento.')
    metales = canal(p, reverb, 'Trompeta', envio=0.6, volumen=0.6, efectos=[m.filtro_doble(corte1=6000, reso1=0.5, tipo1=0)])
    organo_c = canal(p, reverb, 'Organo', envio=0.5, volumen=0.6, efectos=[m.filtro_doble(corte1=90, reso1=0.5, tipo1=1)])
    golpes = canal(p, reverb, 'Campana', envio=0.7, volumen=1.0)
    trompeta = p.pista('Trompeta', **m.trompeta(), canal=metales, vol=85)
    organo = p.pista('Organo', **m.organo(liberacion=1.0), canal=organo_c, vol=50)
    campana = p.pista('Campana', **m.campana(), canal=golpes, vol=90)

    t.toca(campana, 0.3, 3.0, 'D2', 100)
    t.toca(campana, INTEGRA + 3.2, 2.8, 'D2', 90)
    return cierre(p, t, trompeta, organo, 12)


# La mezcla: lo que se subió o bajó (dB) el volumen de cada pista tras medirlas exportadas por separado.
MEZCLA = {
    'Inicial': {'Colchon': -4.6, 'Gran casa': 5.6, 'Caja': 5.3, 'Latido': 2.5, 'Ira': 8.5, 'Ira metal': 16, 'Tristeza': 0.4,
                'Miedo': 6.6, 'Culpa': 9.8, 'Piano agudo': 8.3},
    'Jefe_Ira': {'Forja': -11.2, 'Latido': -2, 'Raspa': 1.8, 'Golpe': 5.5, 'Estallido': 1.2, 'Platillo': 2.4, 'Caja': -2.3, 'Ruido': 6},
    'Jefe_Tristeza': {'Piano': -1.2, 'Coro': 1.6, 'Cuerdas': 7.2, 'Timbal': 4.9, 'Campana': 11.5},
    'Jefe_Miedo': {'Zumbido': -11.3, 'Racimo': -2.4, 'Latido': 2, 'Tom medio': 3.7, 'Golpe grave': 7.3, 'Rama': 5.3},
    'Jefe_Culpa': {'Campana': 9.9, 'Gran casa': 8.8, 'Caja': 8.6},
    'Derrota_Ira': {'Golpe': 10.5, 'Estallido': 5.2, 'Platillo': 5.9, 'Metal': 4, 'Sinte': -1, 'Bajo': -8.4, 'Colchon': -7},
    'Derrota_Tristeza': {'Piano': 2.4, 'Cuerdas': 3, 'Timbal': 9.7, 'Campana': 10},
    'Derrota_Miedo': {'Golpe grave': 5.9, 'Latido': 5.2, 'Zumbido': -12, 'Racimo': -3.8, 'Piano': 2},
    'Derrota_Culpa': {'Campana': 8.5, 'Gran casa': 8},
    'Cierre_Ira': {'Colchon': -8, 'Metal': 10},
    'Cierre_Tristeza': {'Piano': -5.2},
    'Cierre_Miedo': {'Cuerdas graves': -2},
    'Cierre_Culpa': {'Organo': 2.5, 'Campana': 8},
}


def mezclar(nombre, proyecto_):
    for pista in proyecto_.pistas:
        cambio = MEZCLA.get(nombre, {}).get(pista.nombre)
        if cambio:
            pista.vol = min(200, round(pista.vol * 10 ** (cambio / 20)))
    return proyecto_


PROYECTOS = {
    'Inicial': inicial,
    'Jefe_Ira': jefe_ira, 'Jefe_Tristeza': jefe_tristeza, 'Jefe_Miedo': jefe_miedo, 'Jefe_Culpa': jefe_culpa,
    'Derrota_Ira': derrota_ira, 'Derrota_Tristeza': derrota_tristeza, 'Derrota_Miedo': derrota_miedo, 'Derrota_Culpa': derrota_culpa,
    'Cierre_Ira': cierre_ira, 'Cierre_Tristeza': cierre_tristeza, 'Cierre_Miedo': cierre_miedo, 'Cierre_Culpa': cierre_culpa,
}


if __name__ == '__main__':
    forzar = '--forzar' in sys.argv
    os.makedirs(CARPETA, exist_ok=True)
    for nombre, componer in PROYECTOS.items():
        ruta = os.path.join(CARPETA, nombre + '.mmp')
        if os.path.exists(ruta) and not forzar:
            print(f'{nombre}: ya existe, no se toca (--forzar para volver a escribirlo).')
            continue
        proyecto_ = mezclar(nombre, componer())
        proyecto_.guardar(ruta)
        print(f'{nombre}: {proyecto_.segundos:.1f} s, {len(proyecto_.pistas)} pistas.')
