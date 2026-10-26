"""
La música del Nivel 2 (la Tristeza), con los sonidos que trae LMMS. Escribe dos proyectos de LMMS:

  Musica/Nivel2/Nivel2_Tristeza.mmp   El nivel. Anexo A: «caja de música lenta y distorsionada bajo el agua,
                                      piano melancólico con reverb extremo, ~70 BPM». Si menor, 70 BPM,
                                      24 compases (82 s): la nana en la caja de música, que se tuerce como una
                                      cinta vieja; la nana en el piano, con cuerdas; y la caja de música sola,
                                      cada vez con menos notas.
  Musica/Nivel2/Nivel2_Doncella.mmp   La Doncella del Lamento. Un vals: Si menor, 3/4, 140 BPM, 48 compases
                                      (62 s). El lamento en las cuerdas sobre el vals del piano y el coro; en la
                                      caja de música, con timbales; y arriba, cuerdas y coro juntos.

Todo pasa por un filtro que quita los agudos (bajo el agua) y por una reverb enorme. Para pasarlos al
juego: exportar_musica.py.

    python Herramientas/componer_nivel2.py            # no pisa un .mmp que ya exista
    python Herramientas/componer_nivel2.py --forzar   # lo vuelve a escribir (se pierde lo editado)
"""
import os
import sys

import musica_lmms as m

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARPETA = os.path.join(RAIZ, 'Musica', 'Nivel2')


def bajo_el_agua(p, nombre, volumen, reverb, envio, corte=3000, vibrato=False, rota=False):
    """Un canal sin agudos, con mucha reverb; la caja de música, además, se tuerce y se rompe."""
    efectos = []
    if vibrato:
        efectos.append(m.vibrato(segundos=0.004, hz=0.45))
    if rota:
        efectos.append(m.bitcrush(frecuencia=16000, niveles=48, entrada_db=2, recorte_db=-1))
    efectos.append(m.filtro_doble(corte1=corte, reso1=0.6, tipo1=0))
    return p.canal(nombre, volumen=volumen, envios=[(0, 1.0), (reverb, envio)], efectos=efectos)


def agua(p, reverb, compases, ticks_compas):
    """Un rumor grave de agua: ruido sin agudos que crece y se va despacio, todo el bucle."""
    canal = p.canal('Agua', volumen=1.0, envios=[(0, 1.0), (reverb, 0.3)])
    pista = p.pista('Agua', m.triple([m.oscilador(m.RUIDO, 50)]),
                    m.eldata(vol=m.env_volumen(ataque=2.0, liberacion=2.0), fcut=380, fres=0.6, ftype=0, filtro=True),
                    canal=canal, vol=130)
    for c in range(0, compases, 2):
        pista.patron(c, [(0, 2 * ticks_compas, 'A4', 90)])
    return pista


# =============================================================================================
# El nivel
# =============================================================================================

PROGRESION = ['Bm', 'G', 'D', 'F#', 'Bm', 'Em', 'G', 'F#']
ACORDES_GRAVES = {'Bm': 'B1+F#2+B2', 'G': 'G1+D2+G2', 'D': 'D2+A2+D3', 'F#': 'F#1+C#2+F#2', 'Em': 'E2+B2+E3'}
CUERDAS = {'Bm': 'B2+D3+F#3', 'G': 'B2+D3+G3', 'D': 'A2+D3+F#3', 'F#': 'A#2+C#3+F#3', 'Em': 'B2+E3+G3'}
NANA = ('F#4:q B4:q D5:q C#5:e B4:e | B4:h. A4:e G4:e | A4:q F#4:q D4:q E4:e F#4:e | E4:h. r:q | '
        'F#4:q B4:q D5:q E5:e F#5:e | G5:h E5:q B4:q | D5:q. C#5:e B4:q A4:q | A#4:h. r:q')
# La nana que se queda sin notas: solo lo que cae en la parte 1 y la 3.
NANA_SOLA = ('F#4:h D5:h | B4:w | A4:h D4:h | E4:w | F#4:h D5:h | G5:h B4:h | D5:h B4:h | A#4:w')


def tristeza():
    p = m.Proyecto(70, 24, notas_del_proyecto=(
        'Nivel 2 (la Tristeza). Si menor, 70 BPM, 24 compases en bucle. 1-8: la nana en la caja de música, '
        'torcida y bajo el agua, con el piano grave. 9-16: la nana en el piano, con cuerdas. 17-24: la caja de '
        'música sola, cada vez con menos notas. Progresión cada 8 compases: Sim, Sol, Re, Fa#, Sim, Mim, Sol, Fa#.'))
    reverb = p.canal('Reverb', efectos=[m.reverb(tamano=0.97, color=4500)])
    caja = bajo_el_agua(p, 'Caja de musica', 0.7, reverb, 0.6, corte=3500, vibrato=True, rota=True)
    piano = bajo_el_agua(p, 'Piano', 0.8, reverb, 0.85, corte=4000)
    cuerdas = bajo_el_agua(p, 'Cuerdas', 0.3, reverb, 0.7, corte=2500)

    t_caja = p.pista('Caja de musica', **m.caja_de_musica(), canal=caja, vol=80)
    t_graves = p.pista('Piano grave', **m.piano(liberacion=1.0), canal=piano, vol=18)
    t_piano = p.pista('Piano nana', **m.piano(liberacion=1.0), canal=piano, vol=49)
    t_cuerdas = p.pista('Cuerdas', **m.cuerdas(ataque=1.2, liberacion=1.5), canal=cuerdas, vol=60)
    agua(p, reverb, 24, p.ticks_compas)

    t_caja.patron(0, m.frase(NANA, vol=100, transporte=12, compases=8))
    t_piano.patron(8, m.frase(NANA, vol=95, legato=1.0, transporte=-12, compases=8))
    t_caja.patron(16, m.frase(NANA_SOLA, vol=85, transporte=12, compases=8))
    for c in range(24):
        acorde = PROGRESION[c % 8]
        t_graves.patron(c, m.frase(f'{ACORDES_GRAVES[acorde]}:w', vol=70 if c < 16 else 55))
        if 8 <= c < 16:
            t_cuerdas.patron(c, m.frase(f'{CUERDAS[acorde]}:w', vol=70, legato=0.98))
        if 8 <= c < 16 and c % 2 == 1:
            # La caja de música contesta al piano, una nota arriba
            t_caja.patron(c, [(144, 48, m.tecla({'G': 'B5', 'F#': 'C#6', 'Em': 'E6', 'Bm': 'F#6', 'D': 'A5'}[acorde]), 70)])
    return p


# =============================================================================================
# La Doncella del Lamento
# =============================================================================================

TIEMPO = 3 * m.TICKS_NEGRA  # un compás de 3/4
PROGRESION_VALS = ['Bm', 'Bm', 'G', 'G', 'Em', 'Em', 'F#', 'F#', 'Bm', 'Bm', 'D', 'D', 'Em', 'F#', 'Bm', 'Bm']
VALS_BAJO = {'Bm': 'B1', 'G': 'G1', 'Em': 'E2', 'F#': 'F#1', 'D': 'D2'}
VALS_ACORDE = {'Bm': 'D3+F#3+B3', 'G': 'D3+G3+B3', 'Em': 'E3+G3+B3', 'F#': 'C#3+F#3+A#3', 'D': 'D3+F#3+A3'}
CORO_VALS = {'Bm': 'B3+D4+F#4', 'G': 'B3+D4+G4', 'Em': 'B3+E4+G4', 'F#': 'A#3+C#4+F#4', 'D': 'A3+D4+F#4'}
LAMENTO = ('F#4:h. | B4:h D5:q | C#5:h B4:q | A4:h. | G4:h B4:q | E5:h D5:q | C#5:h. | A#4:h F#4:q | '
           'B4:h D5:q | F#5:h. | E5:h D5:q | A4:h. | G4:q B4:q E5:q | D5:h C#5:q | B4:h. | r:h.')


def doncella():
    p = m.Proyecto(140, 48, numerador=3, notas_del_proyecto=(
        'Nivel 2 (la Tristeza): la Doncella del Lamento. Un vals en Si menor, 3/4, 140 BPM, 48 compases en bucle. '
        '1-16: el lamento en las cuerdas, sobre el vals del piano y el coro. 17-32: el lamento en la caja de música, '
        'con timbales. 33-48: cuerdas y coro juntos, arriba; el timbal redobla al final.'))
    reverb = p.canal('Reverb', efectos=[m.reverb(tamano=0.95, color=5000)])
    piano = bajo_el_agua(p, 'Piano', 0.7, reverb, 0.5, corte=4500)
    cuerdas = bajo_el_agua(p, 'Cuerdas', 0.7, reverb, 0.55, corte=4000)
    coro = bajo_el_agua(p, 'Coro', 0.5, reverb, 0.6, corte=3500)
    caja = bajo_el_agua(p, 'Caja de musica', 0.6, reverb, 0.5, corte=5000, vibrato=True)
    percusion = p.canal('Percusion', volumen=0.8, envios=[(0, 1.0), (reverb, 0.35)])

    t_bajo = p.pista('Piano bajo', **m.piano(liberacion=0.6), canal=piano, vol=49)
    t_vals = p.pista('Piano vals', **m.piano(liberacion=0.3), canal=piano, vol=26)
    t_cuerdas = p.pista('Cuerdas lamento', **m.cuerdas(ataque=0.25, liberacion=0.8), canal=cuerdas, vol=123)
    t_coro = p.pista('Coro', **m.coro(ataque=0.5, liberacion=0.9), canal=coro, vol=67)
    t_coro_tema = p.pista('Coro lamento', **m.coro(ataque=0.2, liberacion=0.6), canal=coro, vol=152)
    t_caja = p.pista('Caja de musica', **m.caja_de_musica(decaimiento=1.2), canal=caja, vol=102)
    t_timbal = p.pista('Timbal', **m.timbal(), canal=percusion, vol=132)
    t_hielo = p.pista('Hielo', **m.golpe('effects/wind_chimes01.ogg', liberacion=0.8), canal=percusion, vol=71, pan=30)
    agua(p, reverb, 48, p.ticks_compas)

    def lamento(transporte=0, vol=100):
        return m.frase(LAMENTO, vol=vol, legato=0.97, transporte=transporte, compases=16, compas=TIEMPO)

    t_cuerdas.patron(0, lamento())
    t_caja.patron(16, lamento(transporte=12, vol=95))
    t_cuerdas.patron(32, lamento(transporte=12, vol=95))
    t_coro_tema.patron(32, lamento(transporte=0, vol=90))
    for c in range(48):
        acorde = PROGRESION_VALS[c % 16]
        seccion = c // 16
        # El vals: el bajo en la 1, el acorde en la 2 y la 3
        t_bajo.patron(c, [(0, 44, VALS_BAJO[acorde], 100)])
        t_vals.patron(c, [(pos, 40, k, 80) for pos in (48, 96) for _, _, k, _ in m.frase(f'{VALS_ACORDE[acorde]}:q')])
        if seccion != 2:
            t_coro.patron(c, m.frase(f'{CORO_VALS[acorde]}:h.', vol=70 if seccion == 0 else 80, legato=0.98, compas=TIEMPO))
        if seccion >= 1:
            if c % 2 == 0 or seccion == 2:
                t_timbal.patron(c, [(0, 48, m.tecla(VALS_BAJO[acorde]) + (12 if m.tecla(VALS_BAJO[acorde]) < m.tecla('C2') else 0), 100)])
            if c == 47:
                t_timbal.patron(c, [(i * 12, 12, 'F#2', 60 + 5 * i) for i in range(12)])
        if c % 8 == 0:
            t_hielo.patron(c, [(0, 2 * TIEMPO, 45, 90)])  # una octava abajo: más lento y más frío
    return p


PROYECTOS = {'Nivel2_Tristeza.mmp': tristeza, 'Nivel2_Doncella.mmp': doncella}


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
        print(f'{archivo}: {proyecto.compases:g} compases a {proyecto.bpm} BPM ({proyecto.segundos:.1f} s), '
              f'{len(proyecto.pistas)} pistas.')
