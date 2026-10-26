"""
La música del Nivel 3 (el Miedo), con los sonidos que trae LMMS. Escribe tres proyectos de LMMS:

  Musica/Nivel3/Nivel3_Miedo.mmp   El nivel. Anexo A: «casi silencio, interrumpido por ruidos ambientales
                                   aleatorios». 192 s en bucle (48 compases a 60 BPM, sin pulso): un zumbido
                                   grave que va y viene, viento a ratos y, cada 8 a 16 s, algo: una rama que
                                   cruje, un aliento, una campana muy lejos, un carillón torcido, un golpe
                                   sordo. Al azar, pero siempre el mismo azar (SEMILLA), para que no se
                                   repita en 3 minutos.
  Musica/Nivel3/Nivel3_Pavor.mmp   El Espectro del Pavor. Anexo A: «percusión irregular + frecuencias bajas».
                                   Do# menor, 105 BPM, 32 compases (73 s): un latido; toms en grupos de 3 y 2
                                   que no caen donde se espera; el chelo en trémolo; racimos de cuerdas graves;
                                   todo junto, y los toms se aceleran hasta el principio.
  Musica/Golpes/Antorcha.mmp       Al encender una antorcha (Anexo A: «stinger de cuerdas»): un golpe de
                                   cuerdas en Do# menor que se queda en la reverb, 3 s.

Para pasarlos al juego: exportar_musica.py.

    python Herramientas/componer_nivel3.py            # no pisa un .mmp que ya exista
    python Herramientas/componer_nivel3.py --forzar   # lo vuelve a escribir (se pierde lo editado)
"""
import os
import random
import sys

import musica_lmms as m

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARPETA = os.path.join(RAIZ, 'Musica', 'Nivel3')
CARPETA_GOLPES = os.path.join(RAIZ, 'Musica', 'Golpes')
SEMILLA = 1983

# Lo que se oye en el bosque: (nombre, muestra, su nota, desde, hasta (semitonos que se puede bajar o subir), volumen)
RUIDOS = [
    ('Rama', 'drums/wood01.ogg', 57, -5, 3, 157),
    ('Crujido', 'drums/rim01.ogg', 57, -14, -10, 108),
    ('Aliento', 'misc/breath01.ogg', 57, -8, -4, 158),
    ('Campana lejana', 'misc/dong02.ogg', 'D3', -14, -13, 70),
    ('Carillon', 'effects/wind_chimes01.ogg', 57, -14, -10, 77),
    ('Golpe sordo', 'drums/bassdrum_acoustic02.ogg', 57, -8, -5, 67),
    ('Zumbido', 'effects/warp01.ogg', 57, -7, -3, 190),
]


# =============================================================================================
# El nivel
# =============================================================================================

def miedo():
    azar = random.Random(SEMILLA)
    p = m.Proyecto(60, 48, notas_del_proyecto=(
        'Nivel 3 (el Miedo). Casi silencio: 48 compases a 60 BPM (192 s), sin pulso. Un zumbido grave que va y '
        'viene, viento a ratos y, cada 8 a 16 s, un ruido del bosque, al azar (siempre el mismo: componer_nivel3.py, '
        'SEMILLA). Suena muy bajo: lo normaliza a -30 dB exportar_musica.py.'))
    reverb = p.canal('Reverb', efectos=[m.reverb(tamano=0.93, color=3500)])
    fondo = p.canal('Fondo', envios=[(0, 1.0), (reverb, 0.4)])
    bosque = p.canal('Bosque', envios=[(0, 1.0), (reverb, 0.7)],
                     efectos=[m.filtro_doble(corte1=5000, reso1=0.5, tipo1=0)])

    zumbido = p.pista('Zumbido grave', m.triple([m.oscilador(m.SENO, 60), m.oscilador(m.SENO, 25, grueso=12, fino_izq=-4, fino_der=4)]),
                      m.eldata(vol=m.env_volumen(ataque=3.0, liberacion=3.0)), canal=fondo, vol=11)
    viento = p.pista('Viento', m.triple([m.oscilador(m.RUIDO, 60)]),
                     m.eldata(vol=m.env_volumen(ataque=3.5, liberacion=3.5),
                              cut=m.envolvente('elcut', att=m.segundos(4.0), sustain=0.6, rel=m.segundos(3.0), amt=0.12),
                              fcut=350, fres=0.8, ftype=0, filtro=True), canal=fondo, vol=88)

    # El zumbido, a tramos de 4 a 7 compases, con silencios de 2 a 4 entre ellos
    c = 0
    while c < 48:
        largo = min(azar.randint(4, 7), 48 - c)
        zumbido.patron(c, [(0, largo * 192 - 48, 'C#1', 90)])
        c += largo + azar.randint(2, 4)
    # El viento, a ratos
    c = azar.randint(1, 4)
    while c < 47:
        largo = min(azar.randint(2, 3), 48 - c)
        viento.patron(c, [(0, largo * 192 - 24, 'A4', azar.randint(70, 100))])
        c += largo + azar.randint(3, 7)

    pistas = {nombre: p.pista(nombre, **m.golpe(ruta, raiz, liberacion=0.6), canal=bosque, vol=vol, pan=azar.randint(-40, 40))
              for nombre, ruta, raiz, _, _, vol in RUIDOS}
    # Los ruidos: cada 8 a 16 s, uno distinto del anterior, en cualquier punto del compás
    t, anterior = azar.uniform(3, 6), None
    while t < 190:
        nombre, _, raiz, abajo, arriba, _ = azar.choice([r for r in RUIDOS if r[0] != anterior])
        anterior = nombre
        base = m.tecla(raiz) if isinstance(raiz, str) else raiz
        nota = base + azar.randint(abajo, arriba)
        tick = int(round(t * 48))
        notas = [(tick % 192, 192, nota, azar.randint(70, 110))]
        if nombre == 'Rama' and azar.random() < 0.5:
            notas.append((tick % 192 + azar.randint(10, 30), 192, nota - 2, 60))  # se parte en dos
        pistas[nombre].patron(tick // 192, notas)
        t += azar.uniform(8, 16)
    return p


# =============================================================================================
# El Espectro del Pavor
# =============================================================================================

S = 12  # una semicorchea
# Dos compases de semicorcheas en grupos de 3 y 2 (3+3+2+3+3+2+3+2+3+3+2+3): el tom cambia en cada grupo.
GRUPOS = [3, 3, 2, 3, 3, 2, 3, 2, 3, 3, 2, 3]
TOMS = ['tom_low', 'tom_mid', 'tom_low', 'tom_hi', 'tom_low', 'tom_mid']
# Los golpes graves, en sitios distintos cada compás de los cuatro
GRAVES = [[0, 11], [5, 14], [0, 3, 9], [7, 13]]


def pavor():
    azar = random.Random(SEMILLA + 1)
    p = m.Proyecto(105, 32, notas_del_proyecto=(
        'Nivel 3 (el Miedo): el Espectro del Pavor. Do# menor, 105 BPM, 32 compases en bucle. 1-8: un latido, golpes '
        'graves y un zumbido. 9-16: los toms en grupos de 3 y 2, y el chelo en trémolo. 17-24: racimos de cuerdas '
        'graves. 25-32: todo junto; los toms se aceleran hasta el principio.'))
    reverb = p.canal('Reverb', efectos=[m.reverb(tamano=0.9, color=4000)])
    golpes = p.canal('Golpes', envios=[(0, 1.0), (reverb, 0.3)],
                     efectos=[m.bitcrush(frecuencia=22050, niveles=96, entrada_db=3, recorte_db=-1)])
    cuerdas = p.canal('Cuerdas', volumen=0.8, envios=[(0, 1.0), (reverb, 0.5)],
                      efectos=[m.filtro_doble(corte1=2500, reso1=0.5, tipo1=0)])
    graves = p.canal('Graves', volumen=0.8, envios=[(0, 1.0), (reverb, 0.15)])

    toms = {
        'tom_low': p.pista('Tom grave', **m.timbal(), canal=golpes, vol=81),
        'tom_mid': p.pista('Tom medio', **m.golpe('drums/tom_mid01.ogg', 'A2', cents=-20, liberacion=0.2), canal=golpes, vol=131, pan=-20),
        'tom_hi': p.pista('Tom agudo', **m.golpe('drums/tom_hi01.ogg', 'D#3', cents=10, liberacion=0.2), canal=golpes, vol=123, pan=20),
    }
    t_boom = p.pista('Golpe grave', **m.bombo(), canal=golpes, vol=61)
    t_click = p.pista('Chasquido', **m.golpe('drums/wood01.ogg'), canal=golpes, vol=153, pan=35)
    t_tremolo = p.pista('Cuerdas tremolo', **m.cuerdas(ataque=0.0, liberacion=0.05), canal=cuerdas, vol=80)
    t_racimo = p.pista('Racimo', **m.cuerdas(ataque=1.5, liberacion=1.5), canal=cuerdas, vol=48)
    # Un aliento que se toma: ruido sin agudos que crece hasta el principio de cada sección
    t_aliento = p.pista('Aliento', m.triple([m.oscilador(m.RUIDO, 100)]),
                        m.eldata(vol=m.env_volumen(ataque=2.8, liberacion=0.15), fcut=1400, fres=0.8, ftype=0, filtro=True),
                        canal=cuerdas, vol=180)
    t_zumbido = p.pista('Zumbido', m.triple([m.oscilador(m.SENO, 70), m.oscilador(m.SIERRA, 12, grueso=12)]),
                        m.eldata(vol=m.env_volumen(ataque=1.5, liberacion=1.5), fcut=300, fres=0.7, ftype=0, filtro=True),
                        canal=graves, vol=25)

    for c in range(32):
        seccion = c // 8
        # El latido: dos golpes (lub-dub) en la 1 y en la 3, solo al principio
        if seccion == 0:
            toms['tom_low'].patron(c, [(0, 24, 'C#2', 110), (S * 2, 24, 'C#2', 75), (S * 8, 24, 'C#2', 100), (S * 10, 24, 'C#2', 70)])
        else:
            # Los toms en grupos irregulares, sobre dos compases
            inicio = 0 if c % 2 == 0 else 16
            pos, notas = 0, []
            for i, g in enumerate(GRUPOS):
                if inicio <= pos < inicio + 16:
                    tom = TOMS[i % len(TOMS)]
                    nota = {'tom_low': 'C#2', 'tom_mid': 'E2', 'tom_hi': 'G2'}[tom]
                    notas.append((tom, (pos - inicio) * S, nota, 115 if i % 3 == 0 else 85))
                pos += g
            if seccion == 3 and c >= 30:
                # Al final se aceleran: fusas en el tom grave que crecen
                notas = [('tom_low', i * 6, 'C#2', 60 + (i + 32 * (c - 30)) * 0.8) for i in range(32)]
            for tom in toms:
                propias = [(pp, 24, n, v) for t_, pp, n, v in notas if t_ == tom]
                if propias:
                    toms[tom].patron(c, propias)
        # Los golpes graves, una cuarta abajo del Do# (un Sol# grave)
        t_boom.patron(c, [(pp * S, 192, 52, 110 if pp == 0 else 90) for pp in GRAVES[c % 4]])
        # Chasquidos sueltos, donde menos se esperan
        if azar.random() < 0.5:
            t_click.patron(c, [(azar.choice([1, 5, 7, 10, 13, 15]) * S, 24, 57 + azar.randint(-4, 4), azar.randint(50, 90))])
        # Las cuerdas en trémolo (semicorcheas repetidas, Do# y Sol#), creciendo cada cuatro compases
        if seccion >= 1:
            t_tremolo.patron(c, [(i * S, 10, k, 50 + 12 * (c % 4) + (i % 2) * 8) for i in range(16) for k in ('C#3', 'G#3')])
        # Racimos: Do#, Re y Sol, tres notas que no se llevan
        if seccion >= 2 and c % 2 == 0:
            t_racimo.patron(c, m.frase('C#3+D3+G3:w', vol=85))
            t_racimo.patron(c + 1, m.frase('C3+C#3+F#3:w', vol=90))
        # El aliento, que crece hasta el principio de cada sección
        if c % 8 == 7:
            t_aliento.patron(c, [(0, 192, 'A4', 100)])
        if c % 4 == 0:
            t_zumbido.patron(c, [(0, 4 * 192 - 24, 'C#1', 100), (0, 4 * 192 - 24, 'G#1', 60 if seccion >= 2 else 0)])
    return p


# =============================================================================================
# El golpe de cuerdas de la antorcha
# =============================================================================================

def antorcha():
    p = m.Proyecto(75, segundos=3.0, bucle=False, notas_del_proyecto=(
        'Nivel 3 (el Miedo): al encender una antorcha. Un golpe de cuerdas en Do# menor que se queda en la reverb, '
        '3 s. Suena una vez (la barra de arriba, apagada).'))
    reverb = p.canal('Reverb', efectos=[m.reverb(tamano=0.92, color=6000)])
    cuerdas = p.canal('Cuerdas', envios=[(0, 1.0), (reverb, 0.6)])
    golpe = p.canal('Golpe', volumen=0.6, envios=[(0, 1.0), (reverb, 0.4)])
    t_cuerdas = p.pista('Cuerdas', **m.cuerdas(ataque=0.02, liberacion=1.2), canal=cuerdas, vol=100)
    t_graves = p.pista('Cuerdas graves', **m.cuerdas(ataque=0.05, liberacion=1.4), canal=cuerdas, vol=70)
    t_golpe = p.pista('Golpe', **m.bombo(), canal=golpe, vol=90)
    # El acorde suena 1,2 s y se apaga en la reverb
    t_cuerdas.en(0, [(0, p.t(1.2), k, 110) for k in (m.tecla('G#3'), m.tecla('C#4'), m.tecla('E4'), m.tecla('G#4'))])
    t_graves.en(0, [(0, p.t(1.4), k, 100) for k in (m.tecla('C#2'), m.tecla('C#3'))])
    t_golpe.en(0, [(0, p.t(2.0), 50, 110)])
    return p


PROYECTOS = {os.path.join(CARPETA, 'Nivel3_Miedo.mmp'): miedo, os.path.join(CARPETA, 'Nivel3_Pavor.mmp'): pavor,
             os.path.join(CARPETA_GOLPES, 'Antorcha.mmp'): antorcha}


if __name__ == '__main__':
    forzar = '--forzar' in sys.argv
    for ruta, componer in PROYECTOS.items():
        archivo = os.path.basename(ruta)
        os.makedirs(os.path.dirname(ruta), exist_ok=True)
        if os.path.exists(ruta) and not forzar:
            print(f'{archivo}: ya existe, no se toca (--forzar para volver a escribirlo).')
            continue
        proyecto = componer()
        proyecto.guardar(ruta)
        print(f'{archivo}: {proyecto.segundos:.1f} s a {proyecto.bpm} BPM, {len(proyecto.pistas)} pistas'
              f'{"" if proyecto.bucle else ", suena una vez"}.')
