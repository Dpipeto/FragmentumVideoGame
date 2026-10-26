"""
Pone al día TesisIA.docx (24-10-2026) con lo construido después del 21-10-2026: el mundo de la Culpa terminado (su
arte, sus memorias, sus cinemáticas y sus puertas), la música de todo el juego, las opciones de volumen, los efectos
de sonido de Kael y el Espectro del Pavor más rápido. Trabaja sobre la versión que tiene el autor (con sus cambios) y
solo toca lo que dice; lo demás queda como estaba.

    python actualizar_n4_musica.py <entrada.docx> <salida.docx> <resultados-EditMode.xml> <resultados-PlayMode.xml>

Antes: python figuras_n4.py (las figuras de la Culpa, en fig/).
"""
import os
import sys
import xml.etree.ElementTree as ET

from docx_util import Documento, qn, texto_de
from subsistemas import contar

FIG = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'fig')


# --- cambiar texto sin perder el formato ---

def _ts_fuera_de_campos(e):
    """Los w:t de <e> en orden, con si están dentro del resultado de un campo (una referencia, un número)."""
    out, en_campo = [], 0
    for r in e.iter(qn('w:r')):
        fc = r.find(qn('w:fldChar'))
        if fc is not None:
            tipo = fc.get(qn('w:fldCharType'))
            if tipo == 'begin':
                en_campo += 1
            elif tipo == 'end':
                en_campo -= 1
            continue
        for t in r.iter(qn('w:t')):
            out.append((t, en_campo > 0))
    return out


def cambiar_en(e, viejo, nuevo):
    """
    Cambia <viejo> por <nuevo> dentro de <e> (un párrafo o una celda). El texto nuevo toma el formato de la primera
    carrera que tocaba el viejo; las demás quedan como estaban. No toca los campos (referencias, números de tabla).
    """
    ts = _ts_fuera_de_campos(e)
    todo = ''.join(t.text or '' for t, _ in ts)
    i = todo.find(viejo)
    assert i >= 0, f'No encuentro «{viejo[:60]}»'
    assert todo.find(viejo, i + 1) < 0, f'«{viejo[:60]}» aparece dos veces'
    fin = i + len(viejo)
    pos, primero = 0, True
    for t, en_campo in ts:
        texto = t.text or ''
        a, b = pos, pos + len(texto)
        pos = b
        if b <= i or a >= fin:
            continue
        assert not en_campo, f'«{viejo[:60]}» toca un campo'
        antes = texto[:max(0, i - a)]
        despues = texto[max(0, fin - a):] if fin < b else ''
        t.text = antes + (nuevo if primero else '') + despues
        t.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
        primero = False


def cambiar(d, inicio, *pares, contiene=False):
    p = d.buscar(inicio, contiene=contiene)
    for viejo, nuevo in pares:
        cambiar_en(p, viejo, nuevo)
    return p


def tabla_con(d, texto):
    """La tabla del cuerpo que contiene <texto>."""
    for t in d.body.iter(qn('w:tbl')):
        if texto in texto_de(t):
            return t
    raise KeyError(texto)


def celda(tabla, fila, col):
    return tabla.findall(qn('w:tr'))[fila].findall(qn('w:tc'))[col]


def clave(d, leyenda):
    """La clave del marcador _Ref_ de la leyenda que empieza por <leyenda> («Tabla 31.»)."""
    p = d.buscar(leyenda)
    for bs in p.iter(qn('w:bookmarkStart')):
        nombre = bs.get(qn('w:name'))
        if nombre.startswith('_Ref_'):
            return nombre[len('_Ref_'):]
    raise KeyError(leyenda)


def num(x):
    return f'{x:.1f}'.replace('.', ',')


# --- lo que cambia ---

def preliminares(d, em, pm):
    p = d.buscar('Al músico Diego Martínez')
    d.comentar(p, 'La música que suena hoy en el juego se compuso con LMMS con el asistente de IA (apartados 7.8 y 8.1.7). '
                  'Confirmen qué compuso o eligió Diego Martínez y ajusten este agradecimiento o el apartado 8.1.7 para que '
                  'digan lo mismo.')
    cambiar(d, 'Este proyecto de investigación y desarrollo tecnológico diseñó',
            ('tres mundos jugables de principio a fin (Ira, Tristeza y Miedo), cada uno con sus mecánicas, enemigos, jefe, '
             'cinemáticas y registro emocional; el cuarto (Culpa) está en desarrollo, y los seis',
             'cuatro mundos completos (Ira, Tristeza, Miedo y Culpa), cada uno con sus mecánicas, enemigos, jefe, cinemáticas, '
             'música y registro emocional, y los seis'),
            # El formato pide un resumen de 250 palabras como mucho.
            ('Su funcionamiento se verificó con', 'Se verificó con'),
            ('908 pruebas automatizadas', f'{em + pm} pruebas automatizadas'))
    cambiar(d, 'This research and technological development project',
            ('three worlds that can be played from start to finish (Anger, Sadness and Fear), each with its own mechanics, '
             'enemies, boss, cutscenes and in-game emotional self-report; the fourth (Guilt) is in development, and all six',
             'four complete worlds (Anger, Sadness, Fear and Guilt), each with its own mechanics, enemies, boss, cutscenes, '
             'music and in-game emotional self-report, and all six'),
            ('908 automated tests', f'{em + pm:,} automated tests'))


def capitulo1(d):
    cambiar(d, 'La implementación jugable de tres mundos completos',
            ('tres mundos completos, la Ira (Fortaleza Volcánica), la Tristeza (Ciudad Sumergida) y el Miedo (Bosque '
             'Tenebroso),',
             'cuatro mundos completos, la Ira (Fortaleza Volcánica), la Tristeza (Ciudad Sumergida), el Miedo (Bosque '
             'Tenebroso) y la Culpa (Campos de Batalla Arrasados),'),
            ('puertas y registro emocional. El cuarto mundo (la Culpa) está en desarrollo, y los mundos 5 y 6',
             'puertas, música y registro emocional. Los mundos 5 y 6'))


def metodologia(d):
    cambiar(d, 'En el desarrollo se usaron herramientas de inteligencia artificial generativa',
            ('en la reconstrucción de la arquitectura de la v2 y en la escritura de sus pruebas automatizadas, y un modelo',
             'en la reconstrucción de la arquitectura de la v2, en la escritura de sus pruebas automatizadas y en la '
             'composición de la música y de los efectos de sonido, que escribió como programas que generan los proyectos '
             'de LMMS a partir de lo que el documento de diseño pide para cada mundo, y un modelo'))


def diseno(d):
    cambiar(d, 'La matriz guio el resto del desarrollo',
            ('las filas de los tres primeros mundos', 'las filas de los cuatro primeros mundos'))
    cambiar(d, 'Cada uno de los seis mundos se diseñó a partir de su emoción',
            ('los tres primeros, ya construidos,', 'los cuatro primeros, ya construidos,'))
    p = d.buscar('El mundo de la Culpa, aprobado y en desarrollo')
    figura2 = clave(d, 'Figura 2.')
    d.poner(p, ['El mundo de la Culpa parte de «la culpa pesa, pero no define»: mirar lo que pasó y resistirlo sin negarlo, '
                'también en tres tiempos. Cubrirse: en el campo arrasado, los Artilleros disparan a la cabeza y solo '
                'agachado detrás de una cobertura no alcanzan; donde no queda cobertura, en la tierra de nadie, se pasa con '
                'la Invisibilidad ganada en el Miedo. Mirar o no mirar: en las ruinas del pueblo, los tejados son rápidos y '
                'sin memorias, y la calle, más lenta, tiene dos; mirar una memoria vuelve más agresivos a los enemigos de su '
                'zona, pero no mirar ninguna fortalece al jefe. Sostener: el río bajo el puente volado se cruza con el Dash '
                'Acuático, el Verdugo solo cae de rodillas con una explosión y al Juez Silente solo se le hiere mirando la '
                'memoria que representa cada ataque. El recorrido mide unas 200 u. La ', ('ref', figura2), ' muestra el '
                'esquema de los tres recorridos.'])
    cambiar(d, 'Figura 2. Esquemas de los recorridos', ('Arrasados, en desarrollo)', 'Arrasados)'))
    t9 = tabla_con(d, 'Juez Silente → Defensa Reforzada')
    cambiar_en(celda(t9, 4, 4), 'En desarrollo (unas 200 u)', 'Jugable (200 u)')

    p = d.buscar('El menú principal ofrece seis opciones')
    cambiar_en(p, 'seis opciones', 'siete opciones')
    cambiar_en(p, 'sin descargarlo.', 'sin descargarlo. La pantalla Opciones (iteración 47), con los volúmenes, se abre desde '
                                      'el menú y desde la pausa y vuelve con «Volver» a la pantalla desde la que se abrió.')
    d.comentar(p, 'La Figura 21 todavía no dibuja la pantalla Opciones ni el botón que lleva a ella desde el menú y desde la pausa.')

    t31 = d.tabla_tras('Tabla 31.')
    d.anadir_filas(t31, [[['fragmentum_ajustes.json'], ['Los tres volúmenes de las opciones: general, música y efectos'],
                          ['Al mover un deslizador de las opciones'],
                          ['Aparte de la partida: si falta o está dañado, todo suena a su volumen normal']]])


FILAS_ITERACIONES = [
    ('41', '22/10', 'Mundo 4: arte de los personajes', 'Penitente, Artillero, Verdugo y Juez Silente con su arte y sus animaciones',
     '642 / 286'),
    ('42', '22/10', 'Mundo 4: escenario', 'Tileset, coberturas, barril, mina, río, alambrada y campanario con su arte', '649 / 286'),
    ('43', '22/10', 'Mundo 4: memorias y cinemáticas', 'Las cuatro memorias con su imagen, el fondo y la presentación y la derrota '
     'del Juez', '657 / 286'),
    ('44', '23/10', 'Mundo 4: puertas', 'Puerta, verja y checkpoint del mundo, su salida, y el escudo y el daño de Kael', '664 / 288'),
    ('45', '23/10', 'Música del mundo 4', 'La marcha del nivel y el tema del Juez, compuestos con LMMS', '671 / 293'),
    ('46', '23/10', 'Música de los mundos 1 a 3', 'Los temas de los niveles y de sus jefes, las 13 cinemáticas y el golpe de la '
     'antorcha', '710 / 295'),
    ('47', '23/10', 'Menú y opciones', 'La música del menú y del selector; los volúmenes general y de la música, en el menú y en '
     'la pausa', '720 / 298'),
    ('48', '23/10', 'Efectos de sonido de Kael', 'Salto, doble salto, dash, tajo, tajo cargado y daño, y el volumen de los efectos',
     '735 / 303'),
    ('49', '24/10', 'Ajuste del Espectro del Pavor', 'Más rápido, y su embestida, también por las ramas de la arena',
     '735 / 305'),
]


def implementacion(d, em, pm):
    cambiar(d, 'La implementación partió de la primera versión del juego (v1)',
            ('en 41 iteraciones entre el 19 de septiembre y el 20 de octubre de 2026',
             f'en {FILAS_ITERACIONES[-1][0]} iteraciones entre el 19 de septiembre y el 24 de octubre de 2026'),
            ('las 22 primeras levantaron la arquitectura y el mundo de la Ira, y las siguientes construyeron los mundos de la '
             'Tristeza y del Miedo y empezaron el de la Culpa sobre esa misma base',
             'las 22 primeras levantaron la arquitectura y el mundo de la Ira; las siguientes construyeron los mundos de la '
             'Tristeza, del Miedo y de la Culpa sobre esa misma base, y las últimas añadieron la música, las opciones de '
             'volumen y los primeros efectos de sonido'),
            ('creció de 27 a 908', f'creció de 27 a {em + pm}'))
    t32 = d.tabla_tras('Tabla 32.')
    filas = t32.findall(qn('w:tr'))
    assert texto_de(filas[-1]).startswith('41'), texto_de(filas[-1])
    for col, valor in enumerate(FILAS_ITERACIONES[0]):
        d.celda(t32, len(filas) - 1, col, [valor])
    d.anadir_filas(t32, [[[v] for v in f] for f in FILAS_ITERACIONES[1:]])
    cambiar(d, 'Nota. EM: pruebas EditMode; PM: pruebas PlayMode',
            (' La iteración 41 estaba en curso al cerrar este documento.', ''))

    # El Pavor, más rápido (v0.67).
    t35 = d.tabla_tras('Tabla 35.')
    cambiar_en(celda(t35, 3, 2), 'embestida a ras de suelo', 'embestida a su altura (en el suelo o por su rama)')
    cambiar(d, 'Nota. Cada ataque avisa antes de golpear (1 s en el Pavor).',
            ('(1 s en el Pavor)', '(en el Pavor, 0,6 s el tajo, las sombras y la embestida, y 1 s las hojas)'))

    cabeza = d.buscar('El mundo de la Culpa, en desarrollo.')
    cambiar_en(cabeza, 'El mundo de la Culpa, en desarrollo.', 'El mundo de la Culpa.')
    p = d.buscar('El cuarto mundo se aprobó el 20 de octubre de 2026')
    t32c = clave(d, 'Tabla 32.')
    d.poner(p, ['El cuarto mundo se aprobó el 20 de octubre de 2026 y se construyó en cinco iteraciones (', ('ref', t32c), '): '
                'ese día se montaron, sin arte, su recorrido, sus mecánicas, sus enemigos y su jefe, y entre el 22 y el 23 de '
                'octubre, su arte, sus memorias, sus cinemáticas y sus puertas. Como en los mundos anteriores, solo se programó '
                'lo que la Culpa tiene de propio. Agacharse ya existía y aquí se vuelve necesario: los Artilleros apuntan 0,8 s '
                'a la cabeza de Kael, con una línea roja que avisa, y disparan tres balas, de modo que solo agachado detrás de '
                'una cobertura no le alcanzan. Los barriles solo se encienden con el Ataque Cargado, otra explosión o una bala, '
                'y estallan en 2,5 u, en cadena con los que tienen a menos de 3 u; la mina del río sube y baja 2 u cada 3 s. '
                'Donde no hay cobertura, en la tierra de nadie, se pasa con la Invisibilidad ganada en el Miedo, y bajo el '
                'puente volado, con el Dash Acuático de la Tristeza: el nivel necesita las tres habilidades anteriores.'])
    d.despues(p,
              d.parrafo('texto', [
                  'Las cuatro memorias (una radio, una campana, un casco sobre un fusil y una carta) son objetos del campo de '
                  'batalla que Kael activa con la tecla de interactuar; cada una abre una cinemática de 5 s con su imagen y '
                  'una línea de Kael. Mirarlas cuesta: los enemigos de su zona se vuelven un 20 % más agresivos. No mirar '
                  'ninguna cuesta más: el Juez Silente llega con un 30 % más de vida. Y para recordar hay que dejarse ver: '
                  'invisible, Kael no puede activar una memoria. El Penitente, el Eco de este mundo, arrastra sus cadenas; el '
                  'Artillero no se mueve de sus sacos, y el Verdugo, el mini-jefe, solo cae de rodillas con una explosión '
                  'cercana (', ('ref', 'personajes_culpa4'), '). Al Juez Silente no se le puede golpear: cada ataque representa '
                  'una memoria, y solo si Kael activa su marca en el suelo, el Juez se arrodilla y durante 3 s recibe daño; en '
                  'la segunda fase brillan dos marcas, y la verdadera es la de la memoria que el Juez proyecta en la pared (',
                  ('ref', 'jefe_juez'), '). La ', ('ref', 'nivel4_capturas'), ' muestra cuatro momentos del recorrido y la ',
                  ('ref', 'cinematicas_4'), ', sus cinemáticas.']),
              d.leyenda('Figura', 'personajes_culpa4', 'Enemigos y jefe del mundo de la Culpa'),
              d.figura(os.path.join(FIG, 'personajes_culpa.png'), 13.0),
              d.nota(['Selección de fotogramas, a escala distinta por fila. Arte del proyecto; montaje propio.']),
              d.leyenda('Tabla', 'jefe_juez', 'Fases de combate del Juez Silente'),
              d.tabla(['Fase', 'Ataques', 'Cuándo se le puede herir'], [
                  ['1 (100 % a 50 %)',
                   'La orden: artillería que avanza por la plaza; el pueblo: una ola de fuego a ras de suelo; el compañero: '
                   'placas a la altura de la cabeza, que se esquivan agachado tras un escombro; la carta: la espada clavada y '
                   'una onda por el suelo',
                   'Mientras dura cada ataque brilla en el suelo la marca de su memoria; si Kael la activa, el Juez se arrodilla '
                   'y recibe daño durante 3 s'],
                  ['2 (50 % a 0 %)',
                   'La condena: las cuatro memorias a la vez; después, los mismos cuatro ataques en otro orden, con dos marcas '
                   'encendidas',
                   'Solo en la marca de la memoria que proyecta en la pared; la otra es una trampa que sujeta a Kael 1 s y le '
                   'quita 1'],
              ], [1500, 4350, 3500]),
              d.nota(['Vida: 28, o 36 si no se miró ninguna memoria en el camino. Fuera de su ventana es inmune. Cada ataque avisa '
                      '1 s antes de hacer daño. Datos del asset JuezSilente.']),
              d.leyenda('Figura', 'nivel4_capturas', 'Recorrido del Nivel 4: (a) un checkpoint encendido, entre sacos terreros, '
                        '(b) los tejados, con sus chimeneas y sus barriles, (c) la memoria de la carta, a la salida del río, y '
                        '(d) la plaza del Juez Silente, con las memorias sobre sus losas'),
              d.figura(os.path.join(FIG, 'nivel4_capturas.jpg'), 15.5),
              d.nota(['Capturas del juego con el arte del mundo (22 y 23 de octubre de 2026).']),
              d.leyenda('Figura', 'cinematicas_4', 'Planos de las cinemáticas del mundo de la Culpa: presentación del Juez '
                        'Silente (a y b), la memoria de la orden (c), derrota del Juez (d y e) y salida del mundo (f)'),
              d.figura(os.path.join(FIG, 'cinematicas_4.jpg'), 15.5),
              d.nota(['Capturas de la cámara de las cinemáticas a 1920 × 1080. Arte del proyecto.']),
              *musica(d))


def musica(d):
    t31 = clave(d, 'Tabla 31.')
    t32 = clave(d, 'Tabla 32.')
    return [
        d.parrafo('t4', ['Música y efectos de sonido.']),
        d.parrafo('texto', [
            'La música se añadió en las iteraciones 45 a 47 (', ('ref', t32), '). Se compuso con LMMS 1.2.2 (LMMS Developers, '
            '2020), un secuenciador libre, usando solo los sonidos que trae: sus muestras (piano, cuerdas, coro, órgano, '
            'trompeta y percusión) y sus sintetizadores. Cada pieza se escribió como un programa en Python que genera el '
            'proyecto de LMMS (las pistas, las notas, los instrumentos y la mezcla), de modo que se puede abrir y retocar en '
            'LMMS como cualquier otro proyecto; otro programa lo exporta a OGG, lo normaliza y comprueba que el bucle no salte '
            'al repetirse. Cada mundo tiene el carácter que pide su diseño: percusión industrial en la Ira, una caja de música '
            'bajo el agua en la Tristeza, casi silencio en el Miedo y una marcha militar en la Culpa (', ('ref', 'musica'), '). '
            'El tema de Kael, en re menor, lo presenta la cinemática inicial y vuelve en el cierre de cada mundo con los '
            'instrumentos de ese mundo, y el tema de la marcha de la Culpa vuelve en el órgano del Juez Silente.']),
        d.parrafo('texto', [
            'En el juego, la música del nivel suena en bucle y en la arena cambia a la del jefe. Calla en las cinemáticas, que '
            'traen su propia pieza, con la duración exacta de sus planos; las memorias de la Culpa van en silencio. La pantalla '
            'Opciones, desde el menú y desde la pausa, ajusta el volumen general, el de la música y el de los efectos, y los '
            'guarda aparte de la partida (', ('ref', t31), '). Los primeros efectos de sonido, los de Kael (salto, doble salto, '
            'dash, tajo, tajo cargado y daño), se hicieron también con LMMS, por capas (el aire, los golpes graves y el filo), y '
            'suenan en el mismo fotograma que su acción, cada vez un poco más agudos o más graves para que no se oigan '
            'repetidos; los del resto del juego están pendientes.']),
        d.leyenda('Tabla', 'musica', 'Música del juego'),
        d.tabla(['Pieza', 'Dónde suena', 'Cómo es', 'Tonalidad y tempo', 'Duración (s)'], [
            ['Menú', 'Menú inicial y selector', 'El tema de Kael, tranquilo, en el piano y las cuerdas', 'Re menor, 60 BPM', '96'],
            ['Nivel 1', 'Fortaleza Volcánica', 'Percusión industrial y un sintetizador distorsionado, sin melodía', 'Mi frigio, 160 BPM', '96'],
            ['Centinela Ardiente', 'Su arena', 'Lo mismo, más pesado: doble bombo, un riff de quintas y un yunque', 'Mi frigio, 168 BPM', '68,6'],
            ['Nivel 2', 'Ciudad Sumergida', 'Una caja de música lenta y torcida, bajo el agua, y un piano con mucha reverberación', 'Si menor, 70 BPM', '82,3'],
            ['Doncella del Lamento', 'Su arena', 'Un vals: el lamento en las cuerdas, sobre el piano y el coro', 'Si menor, 140 BPM (3/4)', '61,7'],
            ['Nivel 3', 'Bosque Tenebroso', 'Casi silencio: un zumbido grave, viento y ruidos del bosque al azar', '—', '192'],
            ['Espectro del Pavor', 'Su arena', 'Percusión irregular, en grupos de tres y dos golpes, y cuerdas en trémolo', 'Do sostenido menor, 105 BPM', '73,1'],
            ['Nivel 4', 'Campos de Batalla Arrasados', 'Una marcha militar lenta y distorsionada: tambores, campana y trompeta', 'Re menor, 64 BPM', '120'],
            ['Juez Silente', 'Su arena', 'Órgano de iglesia y bajo profundo; el tema de la marcha, en el órgano', 'Re menor, 96 BPM', '80'],
            ['Cinemáticas (13)', 'La inicial y la presentación, la derrota y el cierre de cada mundo', 'Golpes en los cambios de plano; '
             'el tema de Kael en la inicial y en cada cierre', '—', '9 a 80'],
            ['Antorcha', 'Al encender una antorcha', 'Un golpe de cuerdas', 'Do sostenido menor', '3'],
        ], [1500, 1850, 3300, 1650, 1050]),
        d.nota(['En los bucles, la duración de una vuelta. Los proyectos de LMMS están en la carpeta Musica del código fuente '
                '(Anexo E) y el juego usa los OGG de Assets/Audio/Musica. Elaboración propia.']),
    ]


def resultados(d, em, pm):
    cambiar(d, 'Las pruebas del software se planearon desde la fase de diseño',
            ('del 20 de octubre de 2026, se aprobaron las 908 pruebas automatizadas',
             f'del 24 de octubre de 2026, se aprobaron las {em + pm} pruebas automatizadas'))
    cambiar(d, 'Los resultados muestran dos cosas distintas que conviene no confundir.',
            ('las reglas de los tres mundos jugables', 'las reglas de los cuatro mundos jugables'),
            ('enfrentar a propósito en el Miedo, e integrar', 'enfrentar a propósito en el Miedo, mirar las memorias aunque '
                                                              'cueste en la Culpa, e integrar'))
    t = tabla_con(d, 'OE3. Desarrollar el prototipo')
    filas = t.findall(qn('w:tr'))
    fila = next(i for i, tr in enumerate(filas) if texto_de(tr).startswith('OE3.'))
    d.celda(t, fila, 1, [f'Cuatro mundos jugables de principio a fin (Ira, Tristeza, Miedo y Culpa), con sus jefes, mecánicas, '
                         f'cinemáticas, música y registro emocional; {em + pm} pruebas aprobadas (apartado 8.1.7 y capítulo 9)'])
    d.celda(t, fila, 2, ['Cumplido para cuatro mundos; los dos últimos, diseñados'])


def capitulo9(d, em, pm, res, conjuntos, dur):
    cambiar(d, 'En la última ejecución, del 20 de octubre de 2026, se aprobaron las 908',
            ('del 20 de octubre de 2026, se aprobaron las 908 pruebas automatizadas: 630 EditMode y 278 PlayMode',
             f'del 24 de octubre de 2026, se aprobaron las {em + pm} pruebas automatizadas: {em} EditMode y {pm} PlayMode'),
            ('Las EditMode tardaron 13,4 s y las PlayMode 580,9 s', f'Las EditMode tardaron {num(dur[0])} s y las PlayMode '
                                                                     f'{num(dur[1])} s'),
            ('funciona en la escena real.', 'funciona en la escena real. Las de la música, los efectos y las opciones comprueban '
                                            'lo que se mide: que cada pieza está donde debe, que los bucles no saltan al '
                                            'repetirse, que cada cinemática dura lo que su música y que los volúmenes se '
                                            'guardan.'))
    t52 = d.tabla_tras('Tabla 52.')
    for fila, valores in ((1, ['EditMode', conjuntos[0], em, em, 0, 0, num(dur[0])]),
                          (2, ['PlayMode', conjuntos[1], pm, pm, 0, 0, num(dur[1])]),
                          (3, ['Total', sum(conjuntos), em + pm, em + pm, 0, 0, num(sum(dur))])):
        for col, v in enumerate(valores):
            d.celda(t52, fila, col, [str(v)])
    cambiar(d, 'Nota. Unity Test Framework 1.8.0 sobre la versión del 20 de octubre de 2026',
            ('del 20 de octubre de 2026', 'del 24 de octubre de 2026'))

    t53 = d.tabla_tras('Tabla 53.')
    grupos = list(res.items())
    filas = t53.findall(qn('w:tr'))
    viejos = len(filas) - 2   # sin el encabezado ni el total
    d.anadir_filas(t53, [[[g], [str(e)], [str(p)], [str(e + p)]] for g, (e, p) in grupos[viejos:]], despues_de=viejos)
    for i, (g, (e, p)) in enumerate(grupos, start=1):
        for col, v in enumerate([g, e, p, e + p]):
            d.celda(t53, i, col, [str(v)])
    total = len(grupos) + 1
    for col, v in enumerate(['Total', em, pm, em + pm]):
        d.celda(t53, total, col, [str(v)])

    cambiar(d, 'Las pruebas automatizadas tienen límites',
            ('el mundo 4 está en desarrollo y los mundos 5 y 6 no están construidos,',
             'la música y los efectos de sonido se comprueban por lo que se mide (duraciones, volúmenes y bucles), no por cómo '
             'suenan; los mundos 5 y 6 no están construidos,'))

    t57 = d.tabla_tras('Tabla 57.')
    assert texto_de(celda(t57, 4, 0)) == 'Pruebas automatizadas', texto_de(celda(t57, 4, 0))
    cambiar_en(celda(t57, 1, 1), 'versión del 20 de octubre de 2026', 'versión del 24 de octubre de 2026')
    cambiar_en(celda(t57, 2, 1), '20 de octubre de 2026', '24 de octubre de 2026')
    d.celda(t57, 4, 1, [f'{em + pm} ejecutadas ({em} EditMode y {pm} PlayMode), {em + pm} aprobadas, 0 fallidas, 0 omitidas'])
    cambiar_en(celda(t57, 9, 1), 'el arte y las cinemáticas del mundo 4, el sonido y',
               'los efectos de sonido del resto del juego (solo están los de Kael) y')
    cambiar_en(celda(t57, 10, 1), 'los tres primeros mundos', 'los cuatro primeros mundos')


def conclusiones(d, em, pm):
    cambiar(d, '3. Sobre el desarrollo y la verificación (OE3).',
            ('con tres mundos jugables de principio a fin y el cuarto en desarrollo.',
             'con cuatro mundos jugables de principio a fin, con su música.'),
            ('construir el segundo y el tercer mundo en dos semanas sin perder lo construido: las pruebas automatizadas pasaron '
             'de 520 a 908',
             f'construir el segundo, el tercero y el cuarto mundo en poco más de dos semanas sin perder lo construido: las '
             f'pruebas automatizadas pasaron de 520 a {em + pm}'),
            ('(el Ataque Cargado rompe el hielo de la Tristeza y el Dash Acuático cruza la ciénaga del Miedo)',
             '(el Ataque Cargado rompe el hielo de la Tristeza, el Dash Acuático cruza la ciénaga del Miedo y la Invisibilidad, '
             'la tierra de nadie de la Culpa)'),
            ('; falta además el sonido.', '; de los efectos de sonido, solo están los de Kael.'))
    cambiar(d, 'En conjunto, el trabajo diseñó un videojuego para el autoconocimiento',
            ('hasta tener tres mundos jugables', 'hasta tener cuatro mundos jugables'),
            ('su implementación en tres emociones', 'su implementación en cuatro emociones'))
    cambiar(d, 'Pedir confirmación antes de que «Nueva partida» reemplace',
            ('y añadir un menú de opciones con volumen, pantalla completa y reasignación de controles.',
             'y ampliar el menú de opciones, que hoy solo tiene los volúmenes, con pantalla completa y reasignación de '
             'controles.'))
    cambiar(d, 'Terminar el mundo de la Culpa (su arte, sus cinemáticas y sus puertas)',
            ('Terminar el mundo de la Culpa (su arte, sus cinemáticas y sus puertas) y construir los mundos 5 y 6 sobre la '
             'arquitectura existente, con la que los mundos 2 y 3 se hicieron en dos semanas; añadir la música y los efectos '
             'de sonido, que el prototipo todavía no tiene.',
             'Construir los mundos 5 y 6 sobre la arquitectura existente, con la que los mundos 2 a 4 se hicieron en poco más de '
             'dos semanas, cada uno con su música, y completar los efectos de sonido, de los que el prototipo solo tiene los de '
             'Kael.'))


def referencias_y_glosario(d):
    marin = d.buscar('Marín Eazo, J. D.')
    d.antes(marin, d.parrafo('referencia', ['LMMS Developers. (2020). ', ('i', 'LMMS'), ' (versión 1.2.2) [Software]. '
                                                                                         'https://lmms.io']))
    metroid = d.buscar('METROIDVANIA:')
    d.antes(metroid, d.parrafo('glosario', [('b', 'LMMS:'), ' secuenciador musical libre con el que se compusieron la música y '
                                                            'los efectos de sonido del juego.']))


def anexos(d):
    cambiar(d, '6. La ejecución de las pruebas automatizadas y el mundo de la Culpa en desarrollo',
            ('el mundo de la Culpa en desarrollo', 'el mundo de la Culpa'))
    cambiar(d, 'Este formato recoge la valoración de un profesional de la psicología',
            ('juega los tres mundos disponibles', 'juega los cuatro mundos disponibles'))


def leer_resultados(edit, play):
    conjuntos, dur, totales = [], [], []
    for f in (edit, play):
        r = ET.parse(f).getroot()
        assert r.get('result') == 'Passed' and r.get('failed') == '0', f
        conjuntos.append(sum(1 for s in r.iter('test-suite') if s.get('type') == 'TestFixture'))
        dur.append(float(r.get('duration').replace(',', '.')))
        totales.append(int(r.get('total')))
    return totales, conjuntos, dur


def main(entrada, salida, edit, play):
    (em, pm), conjuntos, dur = leer_resultados(edit, play)
    res, sin = contar((edit, play))
    assert not sin, sin
    d = Documento(entrada)
    preliminares(d, em, pm)
    capitulo1(d)
    metodologia(d)
    diseno(d)
    implementacion(d, em, pm)
    resultados(d, em, pm)
    capitulo9(d, em, pm, res, conjuntos, dur)
    conclusiones(d, em, pm)
    referencias_y_glosario(d)
    anexos(d)
    cuenta, faltan = d.renumerar()
    print('tablas y figuras:', cuenta, '; referencias sin destino:', faltan or 'ninguna')
    d.guardar(salida)
    print('guardado', salida)


if __name__ == '__main__':
    main(*sys.argv[1:5])
