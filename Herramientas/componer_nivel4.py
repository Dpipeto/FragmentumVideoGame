"""
La música del Nivel 4 (la Culpa), con los sonidos que trae LMMS. Escribe dos proyectos que se abren en
LMMS y se pueden editar a mano:

  Musica/Nivel4/Nivel4_Marcha.mmp  El nivel. Anexo A: «marcha militar lenta y distorsionada».
                                   Re menor, 64 BPM, 32 compases (2 min): tambores y campana; el
                                   tema en la trompeta; el tema con una segunda trompeta; y un eco
                                   lejano del tema, con la campana, que vuelve a crecer hasta el
                                   principio.
  Musica/Nivel4/Nivel4_Juez.mmp    El Juez Silente. Anexo A: «órgano de iglesia + bajo profundo».
                                   Re menor, 96 BPM, 32 compases (80 s) sobre un bajo que baja
                                   (Re, Do, Si bemol, La, Sol, Fa, Mi, La): el órgano a golpes; el
                                   tema de la marcha en el órgano; el tema arriba con el coro; y
                                   el pedal solo, que vuelve a subir.

Los dos son bucles: el último compás lleva al primero. Para pasarlos al juego: exportar_musica.py.

    python Herramientas/componer_nivel4.py            # no pisa un .mmp que ya exista
    python Herramientas/componer_nivel4.py --forzar   # lo vuelve a escribir (se pierde lo editado)
"""
import os
import sys

import musica_lmms as m

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARPETA = os.path.join(RAIZ, 'Musica', 'Nivel4')

FUSA = 6       # una fusa (32 por compás)
SEMIFUSA = 3   # una semifusa: lo que va de golpe a golpe en un redoble


# --- Instrumentos comunes -----------------------------------------------------------------------

def caja():
    return m.muestra('drums/snare_acoustic01.ogg'), m.eldata(vol=m.env_volumen(liberacion=0.05))


def bombo():
    return m.muestra('drums/bassdrum_acoustic02.ogg'), m.eldata(vol=m.env_volumen(liberacion=0.3))


def campana():
    # Una campana en Re 3 (misc/dong02); se toca una octava más grave, para el campanario.
    return m.muestra('misc/dong02.ogg'), m.eldata(vol=m.env_volumen(liberacion=1.0))


def golpes(pasos, compas_ticks=0):
    """(paso de fusa, volumen) -> notas de percusión."""
    return [(compas_ticks + paso * FUSA, 24, 57, vol) for paso, vol in pasos]


def redoble(desde, hasta, vol_desde, vol_hasta, tecla=57):
    """Un redoble en semifusas, del tick «desde» al «hasta» (sin él), creciendo."""
    n = (hasta - desde) // SEMIFUSA
    return [(desde + i * SEMIFUSA, 12, tecla, round(vol_desde + (vol_hasta - vol_desde) * i / max(1, n - 1)))
            for i in range(n)]


# =============================================================================================
# La marcha (el nivel)
# =============================================================================================

BPM_MARCHA = 64
PROGRESION_MARCHA = ['Dm', 'Gm', 'A', 'Dm', 'Bb', 'Gm', 'A', 'A']

# La tuba toca la fundamental en la parte 1 y la quinta en la 3.
TUBA = {'Dm': ('D2', 'A1'), 'Gm': ('G1', 'D2'), 'A': ('A1', 'E2'), 'Bb': ('Bb1', 'F2')}
CUERDAS = {'Dm': 'D3+F3+A3', 'Gm': 'D3+G3+Bb3', 'A': 'C#3+E3+A3', 'Bb': 'D3+F3+Bb3'}

# El tema: ocho compases sobre la progresión. Es también el del Juez.
TEMA = ('D4:q. A3:e D4:q F4:q | G4:h F4:e. E4:s D4:q | E4:q. C#4:e A3:h | D4:h. r:q | '
        'F4:q. F4:e D4:q F4:q | G4:q. A4:e Bb4:q A4:e. G4:s | A4:h E4:q C#4:q | E4:h. r:q')
# La segunda trompeta, por debajo, en blancas.
SEGUNDA = ('A3:h F3:h | Bb3:h G3:h | A3:h E3:h | F3:h. r:q | '
           'D4:h Bb3:h | D4:h Bb3:h | C#4:h A3:h | C#4:h. r:q')
# El eco lejano: la cabeza del tema, una octava arriba, que se apaga.
ECO = 'D5:q. A4:e D5:q F5:q | G5:h. r:q | E5:q. C#5:e A4:h | D5:h. r:q'

# La caja (en fusas): la marcha, el redoble que cierra cada frase y la marcha callada del eco.
CAJA_MARCHA = [(0, 110), (6, 50), (7, 62), (8, 95), (16, 105), (22, 50), (23, 62), (24, 88),
               (28, 42), (29, 50), (30, 60), (31, 74)]
CAJA_FIN_DE_FRASE = [(0, 110), (8, 92), (12, 60), (14, 68)]
CAJA_CALLADA = [(0, 70), (16, 58), (28, 34), (29, 40), (30, 46), (31, 54)]


def marcha():
    p = m.Proyecto(BPM_MARCHA, 32, notas_del_proyecto=(
        'Nivel 4 (la Culpa): la marcha. Re menor, 64 BPM, 32 compases en bucle. '
        '1-8: tambores y campana. 9-16: el tema (trompeta). 17-24: el tema con la segunda trompeta. '
        '25-32: el eco lejano y la campana; crece hasta volver al principio. '
        'La progresión, cada 8 compases: Rem, Solm, La, Rem, Sib, Solm, La, La.'))

    reverb = p.canal('Reverb', volumen=1.0, efectos=[m.reverb(tamano=0.86, color=6500)])
    tambores = p.canal('Tambores', volumen=1.0, envios=[(0, 1.0), (reverb, 0.3)],
                       efectos=[m.bitcrush(frecuencia=22050, niveles=64, entrada_db=3, recorte_db=-1)])
    # La trompeta, como en un disco viejo: sin agudos y algo rota.
    metales = p.canal('Metales', volumen=0.3, envios=[(0, 1.0), (reverb, 0.35)],
                      efectos=[m.filtro_doble(corte1=4500, reso1=0.7, tipo1=0),
                               m.bitcrush(frecuencia=22050, niveles=48, entrada_db=4, recorte_db=-1.5)])
    graves = p.canal('Graves', volumen=0.74, envios=[(0, 1.0), (reverb, 0.12)])
    cuerdas = p.canal('Cuerdas', volumen=0.37, envios=[(0, 1.0), (reverb, 0.45)],
                      efectos=[m.filtro_doble(corte1=3200, reso1=0.5, tipo1=0)])
    campanario = p.canal('Campana', volumen=1.0, envios=[(0, 1.0), (reverb, 0.8)])
    lejos = p.canal('Corneta lejana', volumen=0.42, envios=[(0, 1.0), (reverb, 0.9)],
                    efectos=[m.filtro_doble(corte1=2800, reso1=0.6, tipo1=0),
                             m.bitcrush(frecuencia=11025, niveles=24, entrada_db=3, recorte_db=-2),
                             m.retardo(segundos=0.47, realimentacion=0.35, mezcla=0.45)])

    # --- Percusión
    ins, env = caja()
    t_caja = p.pista('Caja', ins, env, canal=tambores, vol=71)
    ins, env = bombo()
    t_bombo = p.pista('Bombo', ins, env, canal=tambores, vol=68)
    t_platillo = p.pista('Platillo', m.muestra('drums/crash01.ogg'), m.eldata(vol=m.env_volumen(liberacion=0.6)),
                         canal=tambores, vol=77)
    for compas in range(32):
        seccion = compas // 8
        if seccion == 3 and compas < 31:
            t_caja.patron(compas, golpes(CAJA_CALLADA))
        elif compas % 8 == 7:
            # Cierra la frase con un redoble que crece hasta el compás siguiente.
            t_caja.patron(compas, golpes(CAJA_FIN_DE_FRASE) + redoble(96, 192, 45, 112))
        else:
            t_caja.patron(compas, golpes(CAJA_MARCHA))
        if seccion == 3 and compas < 28:
            t_bombo.patron(compas, [(0, 192, 57, 80)])
        else:
            t_bombo.patron(compas, [(0, 96, 57, 105), (96, 96, 57, 85)])
    for compas in (8, 16):
        t_platillo.patron(compas, [(0, 192, 54, 100)])  # tres semitonos abajo: más oscuro

    # --- La tuba (TripleOscillator: sierra y cuadrada una octava abajo, con un filtro que se abre al soplar)
    tuba = m.triple([m.oscilador(m.SIERRA, 45), m.oscilador(m.CUADRADA, 25, grueso=-12), m.oscilador(m.SENO, 45)])
    env_tuba = m.eldata(vol=m.env_volumen(ataque=0.03, decaimiento=0.35, nivel=0.65, liberacion=0.25),
                        cut=m.envolvente('elcut', att=0, dec=m.segundos(0.25), sustain=0.3, rel=m.segundos(0.2), amt=0.35),
                        fcut=420, fres=0.7, ftype=0, filtro=True)
    t_tuba = p.pista('Tuba', tuba, env_tuba, canal=graves, vol=85)
    for compas in range(32):
        seccion = compas // 8
        if seccion == 3 and compas < 28:
            continue
        raiz, quinta = TUBA[PROGRESION_MARCHA[compas % 8]]
        crece = 70 + 10 * (compas - 28) if seccion == 3 else 100
        notas = [(0, 44, raiz, crece), (96, 44, quinta, crece * 0.85)]
        if seccion == 2 and compas % 8 != 7:
            # En la segunda vuelta del tema, una anacrusa hacia el compás siguiente.
            siguiente = TUBA[PROGRESION_MARCHA[(compas + 1) % 8]][0]
            notas.append((180, 12, siguiente, 70))
        t_tuba.patron(compas, notas)

    # --- Las cuerdas (stringsnpads/strings01, en Do 4): un acorde por compás, que entra despacio
    env_cuerdas = m.eldata(vol=m.env_volumen(ataque=0.45, nivel=1.0, liberacion=0.8))
    t_cuerdas = p.pista('Cuerdas', m.muestra('stringsnpads/strings01.ogg'), env_cuerdas, canal=cuerdas,
                        vol=70, basenote='C4', cents=8)
    for compas in range(32):
        acorde = CUERDAS[PROGRESION_MARCHA[compas % 8]]
        vol = 55 if compas < 8 else 75 if compas < 24 else 60
        t_cuerdas.patron(compas, m.frase(f'{acorde}:w', vol=vol, legato=0.98))

    # --- La trompeta (instruments/trumpet01, en La 4; se le quita el zumbido de 50 Hz que trae)
    def trompeta():
        return m.muestra('instruments/trumpet01.ogg'), m.eldata(
            vol=m.env_volumen(ataque=0.01, liberacion=0.18), fcut=160, fres=0.5, ftype=1, filtro=True)

    ins, env = trompeta()
    t_trompeta = p.pista('Trompeta', ins, env, canal=metales, vol=95, basenote='A4', cents=6)
    t_trompeta.patron(8, m.frase(TEMA, vol=100, legato=0.9, compases=8))
    t_trompeta.patron(16, m.frase(TEMA, vol=108, legato=0.9, compases=8))
    ins, env = trompeta()
    t_segunda = p.pista('Trompeta 2', ins, env, canal=metales, vol=62, basenote='A4', cents=6, pan=-25)
    t_segunda.patron(16, m.frase(SEGUNDA, vol=90, legato=0.95, compases=8))
    ins, env = trompeta()
    t_eco = p.pista('Corneta lejana', ins, env, canal=lejos, vol=55, basenote='A4', cents=6, pan=30)
    t_eco.patron(24, m.frase(ECO, vol=90, legato=0.9, compases=4))

    # --- La campana del campanario: cada dos compases al principio y en el eco
    ins, env = campana()
    t_campana = p.pista('Campana', ins, env, canal=campanario, vol=120, basenote='D3')
    for compas in (0, 2, 4, 6, 24, 26, 28, 30):
        t_campana.patron(compas, [(0, 192, m.tecla('D2'), 110)])
    return p


# =============================================================================================
# El Juez Silente (el jefe)
# =============================================================================================

BPM_JUEZ = 96
PROGRESION_JUEZ = ['Dm', 'C', 'Bb', 'A', 'Gm', 'F', 'Edim', 'A7']
# El bajo que baja: la fundamental de cada compás, en la octava 1-2.
BAJO_JUEZ = {'Dm': 'D2', 'C': 'C2', 'Bb': 'Bb1', 'A': 'A1', 'Gm': 'G1', 'F': 'F1', 'Edim': 'E1', 'A7': 'A1'}
# El órgano a golpes, arriba (primera sección) y en medio (tercera, bajo el tema).
ORGANO_ARRIBA = {'Dm': 'D4+F4+A4', 'C': 'C4+E4+G4', 'Bb': 'Bb3+D4+F4', 'A': 'A3+C#4+E4',
                 'Gm': 'G3+Bb3+D4', 'F': 'F3+A3+C4', 'Edim': 'E3+G3+Bb3', 'A7': 'E3+G3+C#4'}
ORGANO_MEDIO = {'Dm': 'D3+F3+A3', 'C': 'C3+E3+G3', 'Bb': 'Bb2+D3+F3', 'A': 'A2+C#3+E3',
                'Gm': 'G2+Bb2+D3', 'F': 'F2+A2+C3', 'Edim': 'E2+G2+Bb2', 'A7': 'E2+G2+C#3'}
CORO = {'Dm': 'A3+D4+F4', 'C': 'G3+C4+E4', 'Bb': 'F3+Bb3+D4', 'A': 'E3+A3+C#4',
        'Gm': 'G3+Bb3+D4', 'F': 'F3+A3+C4', 'Edim': 'E3+G3+Bb3', 'A7': 'E3+G3+C#4'}
# El tema de la marcha, llevado a este bajo.
TEMA_JUEZ = ('D4:q. A3:e D4:q F4:q | E4:h D4:e. C4:s G3:q | F4:q. D4:e Bb3:h | C#4:h E4:h | '
             'D4:q. Bb3:e G3:q Bb3:q | C4:h A3:e. G3:s F3:q | Bb3:q. G3:e E3:h | C#4:h E4:q G4:q')
# Los golpes del órgano: 3 + 3 + 2 corcheas.
GOLPES = [(0, 64), (72, 64), (144, 40)]


def a_golpes(acorde, vol_primero, vol_resto):
    """El acorde, tocado en los tres golpes del compás."""
    return [(pos, dur, k, vol_primero if pos == 0 else vol_resto)
            for pos, dur in GOLPES for _, _, k, _ in m.frase(f'{acorde}:w')]


def juez():
    p = m.Proyecto(BPM_JUEZ, 32, notas_del_proyecto=(
        'Nivel 4 (la Culpa): el Juez Silente. Re menor, 96 BPM, 32 compases en bucle, sobre un bajo que baja: '
        'Rem, Do, Sib, La, Solm, Fa, Mi disminuido, La7. 1-8: el órgano a golpes. 9-16: el tema de la marcha '
        'en el órgano. 17-24: el tema arriba (lengüeta), con el coro y la caja de la marcha. 25-32: el pedal '
        'solo con los timbales; el órgano vuelve a crecer hasta el principio.'))

    reverb = p.canal('Reverb', volumen=1.0, efectos=[m.reverb(tamano=0.92, color=7000)])
    organo = p.canal('Organo', volumen=1.0, envios=[(0, 1.0), (reverb, 0.45)],
                     efectos=[m.filtro_doble(corte1=90, reso1=0.5, tipo1=1)])
    graves = p.canal('Graves', volumen=1.0, envios=[(0, 1.0), (reverb, 0.15)],
                     efectos=[m.filtro_doble(corte1=900, reso1=0.5, tipo1=0)])
    coro = p.canal('Coro', volumen=1.0, envios=[(0, 1.0), (reverb, 0.6)])
    percusion = p.canal('Percusion', volumen=1.0, envios=[(0, 1.0), (reverb, 0.3)],
                        efectos=[m.bitcrush(frecuencia=22050, niveles=64, entrada_db=3, recorte_db=-1)])
    campanario = p.canal('Campana', volumen=1.0, envios=[(0, 1.0), (reverb, 0.75)])

    def organo_muestra():
        # instruments/church_organ01: un La 3 con los registros de 4' y 2' abiertos; dura 8 s.
        return m.muestra('instruments/church_organ01.ogg'), m.eldata(vol=m.env_volumen(ataque=0.02, liberacion=0.35))

    # --- El órgano a golpes y en acordes
    ins, env = organo_muestra()
    t_acordes = p.pista('Organo acordes', ins, env, canal=organo, vol=48, basenote='A3', cents=2)
    for compas in range(32):
        acorde = PROGRESION_JUEZ[compas % 8]
        seccion = compas // 8
        if seccion == 0:
            notas = a_golpes(ORGANO_ARRIBA[acorde], 100, 85)
        elif seccion == 1:
            notas = m.frase(f'{ORGANO_MEDIO[acorde]}:w', vol=70, legato=0.97)
        elif seccion == 2:
            notas = a_golpes(ORGANO_MEDIO[acorde], 95, 80)
        elif compas >= 28:
            notas = m.frase(f'{ORGANO_ARRIBA[acorde]}:w', vol=60 + 16 * (compas - 28), legato=0.97)
        else:
            continue
        t_acordes.patron(compas, notas)

    # --- El tema en el órgano (segunda sección)
    ins, env = organo_muestra()
    t_melodia = p.pista('Organo tema', ins, env, canal=organo, vol=77, basenote='A3', cents=2)
    t_melodia.patron(8, m.frase(TEMA_JUEZ, vol=105, legato=0.93, compases=8))

    # --- El tema arriba, en una lengüeta de Organic (16', 8', 4', 2 2/3', 2', 1'), en la tercera
    lengueta = m.organic([(0.5, 25, 0, m.SENO), (1.0, 70, 0, m.SENO), (1.0, 30, 4, m.TRIANGULO), (2.0, 50, -3, m.SENO),
                          (3.0, 30, 0, m.SENO), (4.0, 28, 2, m.SENO), (8.0, 12, 0, m.SENO)])
    env_lengueta = m.eldata(vol=m.env_volumen(ataque=0.03, liberacion=0.3))
    t_lengueta = p.pista('Organo lengueta', lengueta, env_lengueta, canal=organo, vol=185)
    t_lengueta.patron(16, m.frase(TEMA_JUEZ, vol=110, legato=0.93, transporte=12, compases=8))

    # --- El pedal (instruments/church_organ02, un La 2 con sus octavas): la fundamental, todo el bucle
    t_pedal = p.pista('Pedal', m.muestra('instruments/church_organ02.ogg'),
                      m.eldata(vol=m.env_volumen(ataque=0.05, liberacion=0.4)), canal=graves, vol=56, basenote='A2', cents=5)
    for compas in range(32):
        raiz = BAJO_JUEZ[PROGRESION_JUEZ[compas % 8]]
        t_pedal.patron(compas, [(0, 188, raiz, 100 if compas // 8 != 3 else 90)])

    # --- El bajo profundo (TripleOscillator): corcheas que saltan de octava, con los acentos del órgano
    bajo = m.triple([m.oscilador(m.SIERRA, 40), m.oscilador(m.SENO, 60, grueso=-12), m.oscilador(m.CUADRADA, 12)])
    env_bajo = m.eldata(vol=m.env_volumen(ataque=0.005, decaimiento=0.15, nivel=0.6, liberacion=0.08),
                        cut=m.envolvente('elcut', att=0, dec=m.segundos(0.12), sustain=0.2, rel=m.segundos(0.1), amt=0.25),
                        fcut=380, fres=0.8, ftype=0, filtro=True)
    t_bajo = p.pista('Bajo', bajo, env_bajo, canal=graves, vol=42)
    for compas in range(32):
        raiz = m.tecla(BAJO_JUEZ[PROGRESION_JUEZ[compas % 8]]) + 12
        seccion = compas // 8
        base = 75 if seccion == 1 else 60 + 6 * (compas - 24) if seccion == 3 else 95
        notas = []
        for i in range(8):
            acento = i in (0, 3, 6)
            notas.append((i * 24, 20, raiz + (12 if i % 2 else 0), base + (15 if acento else 0)))
        t_bajo.patron(compas, notas)

    # --- El coro (stringsnpads/chorus02, un La 3): el acorde entero, en la tercera sección
    t_coro = p.pista('Coro', m.muestra('stringsnpads/chorus02.ogg'),
                     m.eldata(vol=m.env_volumen(ataque=0.3, liberacion=0.7)), canal=coro, vol=35, basenote='A3', cents=8)
    for compas in range(16, 24):
        t_coro.patron(compas, m.frase(f'{CORO[PROGRESION_JUEZ[compas % 8]]}:w', vol=90, legato=0.98))

    # --- Percusión: timbal (drums/tom_low01, un Sol 2 algo alto), gran casa y la caja de la marcha
    t_timbal = p.pista('Timbal', m.muestra('drums/tom_low01.ogg'), m.eldata(vol=m.env_volumen(liberacion=0.3)),
                       canal=percusion, vol=72, basenote='G2', cents=-28)
    ins, env = bombo()
    t_casa = p.pista('Gran casa', ins, env, canal=percusion, vol=64)
    ins, env = caja()
    t_caja = p.pista('Caja', ins, env, canal=percusion, vol=44)
    for compas in range(32):
        acorde = PROGRESION_JUEZ[compas % 8]
        raiz = m.tecla(BAJO_JUEZ[acorde])
        raiz = raiz + 12 if raiz < m.tecla('C2') else raiz  # el timbal no baja del Do 2
        seccion = compas // 8
        if seccion in (0, 2):
            t_timbal.patron(compas, [(0, 48, raiz, 110), (72, 48, raiz, 85), (144, 48, raiz - 5 if raiz - 5 >= m.tecla('A1') else raiz + 7, 95)])
            t_casa.patron(compas, [(0, 192, 57, 100)] + ([(72, 120, 57, 70)] if seccion == 2 else []))
        elif seccion == 1:
            t_timbal.patron(compas, [(0, 48, raiz, 90)])
        else:
            if compas < 30:
                t_timbal.patron(compas, [(0, 48, raiz, 80 + 5 * (compas - 24))])
            else:
                # Los dos últimos compases, en corcheas que crecen.
                t_timbal.patron(compas, [(i * 24, 20, raiz, 70 + 4 * i + 20 * (compas - 30)) for i in range(8)])
            if compas >= 28:
                t_casa.patron(compas, [(0, 192, 57, 70 + 8 * (compas - 28))])
        # La caja: la marcha, callada, bajo el tema de arriba; y un redoble al cerrar cada sección.
        if seccion == 2 and compas % 8 != 7:
            t_caja.patron(compas, golpes(CAJA_MARCHA))
        if compas % 8 == 7:
            t_caja.patron(compas, redoble(96, 192, 40, 110 if compas == 31 else 90))

    # --- La campana del juicio: al empezar cada sección, y cada dos compases en la última
    ins, env = campana()
    t_campana = p.pista('Campana', ins, env, canal=campanario, vol=148, basenote='D3')
    for compas in (0, 8, 16, 24, 26, 28, 30):
        t_campana.patron(compas, [(0, 192, m.tecla('D2'), 110)])
    return p


PROYECTOS = {'Nivel4_Marcha.mmp': marcha, 'Nivel4_Juez.mmp': juez}


if __name__ == '__main__':
    forzar = '--forzar' in sys.argv
    os.makedirs(CARPETA, exist_ok=True)
    for archivo, componer in PROYECTOS.items():
        ruta = os.path.join(CARPETA, archivo)
        if os.path.exists(ruta) and not forzar:
            print(f'{archivo}: ya existe, no se toca (--forzar para volver a escribirlo).')
            continue
        proyecto = componer()
        proyecto.guardar(ruta)
        print(f'{archivo}: {proyecto.compases} compases a {proyecto.bpm} BPM ({proyecto.segundos:.1f} s), '
              f'{len(proyecto.pistas)} pistas.')
