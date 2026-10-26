"""
La música del menú inicial y del selector de niveles (la misma en los dos), con los sonidos que trae LMMS.
Escribe Musica/Menu/Menu.mmp: el tema de Kael, tranquilo, en bucle. Re menor, 60 BPM, 24 compases (96 s):

   1-2   Un colchón grave y unas notas sueltas de piano, y la campana lejos.
   3-10  El tema de Kael en el piano.
  11-18  El tema en las cuerdas, con el piano en arpegios.
  19-22  Un puente (Sib, Fa, Solm, La).
  23-24  La cabeza del tema, que se apaga hacia el principio.

Para pasarlo al juego: exportar_musica.py.

    python Herramientas/componer_menu.py            # no pisa un .mmp que ya exista
    python Herramientas/componer_menu.py --forzar   # lo vuelve a escribir (se pierde lo editado)
"""
import os
import sys

import musica_lmms as m
from componer_cinematicas import ACORDES_KAEL, ACOMPANAMIENTO, sinte_calido, tema

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARPETA = os.path.join(RAIZ, 'Musica', 'Menu')

# El piano en corcheas, acorde a acorde (subiendo y bajando).
ARPEGIOS = {'Dm': 'D2 A2 D3 F3 A3 F3 D3 A2', 'Bb': 'Bb1 F2 Bb2 D3 F3 D3 Bb2 F2', 'A': 'A1 E2 A2 C#3 E3 C#3 A2 E2',
            'Gm': 'G1 D2 G2 Bb2 D3 Bb2 G2 D2', 'A7': 'A1 E2 G2 C#3 E3 C#3 G2 E2'}
PUENTE = 'D4:h F4:h | C4:h A3:h | Bb3:q D4:q G4:h | E4:w'
ACORDES_PUENTE = ['D3+F3+Bb3', 'C3+F3+A3', 'D3+G3+Bb3', 'C#3+E3+A3']
BAJOS_PUENTE = ['Bb1+F2', 'F1+C2', 'G1+D2', 'A1+E2']


def menu():
    p = m.Proyecto(60, 24, notas_del_proyecto=(
        'El menú inicial y el selector de niveles. El tema de Kael, tranquilo: Re menor, 60 BPM, 24 compases en bucle. '
        '1-2: colchón y notas sueltas. 3-10: el tema en el piano. 11-18: en las cuerdas, con el piano en arpegios. '
        '19-22: un puente. 23-24: la cabeza del tema, hacia el principio.'))
    reverb = p.canal('Reverb', efectos=[m.reverb(tamano=0.92, color=6000)])
    teclas = p.canal('Piano', envios=[(0, 1.0), (reverb, 0.5)])
    cuerdas = p.canal('Cuerdas', volumen=0.6, envios=[(0, 1.0), (reverb, 0.55)])
    fondo = p.canal('Fondo', volumen=0.5, envios=[(0, 1.0), (reverb, 0.4)])
    lejos = p.canal('Campana', volumen=0.8, envios=[(0, 1.0), (reverb, 0.8)])

    piano = p.pista('Piano', **m.piano(liberacion=1.5), canal=teclas, vol=75)
    piano_bajo = p.pista('Piano bajo', **m.piano(liberacion=1.5), canal=teclas, vol=32)
    agudo = p.pista('Piano agudo', **m.piano_corto(), canal=teclas, vol=110)
    cuerda = p.pista('Cuerdas tema', **m.cuerdas(ataque=0.4, liberacion=1.2), canal=cuerdas, vol=108)
    acordes = p.pista('Cuerdas acordes', **m.cuerdas(ataque=1.2, liberacion=1.5), canal=cuerdas, vol=55)
    colchon = p.pista('Colchon', **sinte_calido(), canal=fondo, vol=39)
    campana = p.pista('Campana', **m.campana(), canal=lejos, vol=200)

    # 1-2: el colchón y notas sueltas
    colchon.patron(0, m.frase('D2+A2:w | D2+A2:w', vol=70))
    piano_bajo.patron(0, m.frase('D2:w | r:w', vol=70))
    agudo.patron(0, m.frase('r:h A5:h | F5:h D5:h', vol=60))
    campana.patron(0, [(0, 2 * 192, 'D2', 90)])
    # 3-10: el tema en el piano, con su bajo
    piano.patron(2, m.frase(tema(1, 8), vol=95, legato=1.0, compases=8))
    for c, acorde in enumerate(ACORDES_KAEL):
        bajo = '+'.join(ACOMPANAMIENTO[acorde].split('+')[:2])
        piano_bajo.patron(2 + c, m.frase(f'{bajo}:w', vol=65))
        colchon.patron(2 + c, m.frase(f'{ACOMPANAMIENTO[acorde]}:w', vol=55))
    # 11-18: el tema en las cuerdas; el piano, en arpegios
    cuerda.patron(10, m.frase(tema(1, 8), vol=95, legato=0.98, transporte=12, compases=8))
    campana.patron(10, [(0, 2 * 192, 'D2', 75)])
    for c, acorde in enumerate(ACORDES_KAEL):
        arpegio = ' '.join(f'{n}:e' for n in ARPEGIOS[acorde].split())
        piano_bajo.patron(10 + c, m.frase(arpegio, vol=70, legato=1.0))
    # 19-22: el puente
    piano.patron(18, m.frase(PUENTE, vol=90, legato=1.0, compases=4))
    for c in range(4):
        acordes.patron(18 + c, m.frase(f'{ACORDES_PUENTE[c]}:w', vol=70, legato=0.98))
        piano_bajo.patron(18 + c, m.frase(f'{BAJOS_PUENTE[c]}:w', vol=60))
    # 23-24: la cabeza del tema, que se apaga
    piano.patron(22, m.frase('D3:q A3:q G3:h | F3:q E3:q D3:h', vol=75, legato=1.0, compases=2))
    colchon.patron(22, m.frase('D2+A2+F3:w | D2+A2:w', vol=55))
    return p


PROYECTOS = {'Menu.mmp': menu}


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
