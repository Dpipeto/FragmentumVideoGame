"""TesisIA tras el tercer concepto: implementación (8.1.7), pruebas, resultados, capítulo 9, conclusiones y anexos."""
import copy
import os

from docx.oxml import OxmlElement

from docx_util import qn, texto_de, estilo_de
from subsistemas import contar

FIG = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'fig')
C3 = 'Tercer concepto (16 de octubre de 2026): '


def _cambiar_imagen(d, leyenda, ruta, ancho):
    ley = d.buscar(leyenda)
    img = ley.getnext()
    assert img.find('.//' + qn('w:drawing')) is not None, leyenda
    nueva = d.figura(ruta, ancho)
    img.addprevious(nueva)
    d.quitar(img)
    return nueva


def implementacion(d):
    p = d.buscar('La implementación partió de la primera versión del juego (v1)')
    d.poner(p, ['La implementación partió de la primera versión del juego (v1), construida antes de esta metodología. Su '
                'revisión encontró defectos que impedían crecer: la máquina de estados de los enemigos era decorativa, el '
                'movimiento del jugador estaba en un único método con banderas, la detección de suelo fallaba con '
                'plataformas solapadas, el gestor del juego no conservaba la progresión y el selector de niveles no '
                'funcionaba. Por eso la arquitectura se reconstruyó en una segunda versión (v2), en 41 iteraciones entre el '
                '19 de septiembre y el 20 de octubre de 2026 (', ('ref', 'iteraciones'), '): las 22 primeras levantaron la '
                'arquitectura y el mundo de la Ira, y las siguientes construyeron los mundos de la Tristeza y del Miedo y '
                'empezaron el de la Culpa sobre esa misma base. Cada iteración terminó con una versión jugable y con todas '
                'sus pruebas aprobadas; el número de pruebas automatizadas creció de 27 a 908.'])
    t32 = d.tabla_tras('Tabla 32.')
    d.anadir_filas(t32, [
        ['23', '08/10', 'Mundo 2: recorrido', 'Nivel 2 montado desde una tabla de medidas, con sus pruebas de alcance', '391 / 153'],
        ['24', '08/10', 'Mundo 2: mecánicas', 'Hielo, agua con oxígeno, placas flotantes, hielo rompible y ecos de intentos', '405 / 158'],
        ['25', '09/10', 'Mundo 2: enemigos', 'Lamento, Escultura Viviente y Medusa de Hielo', '416 / 164'],
        ['26', '10/10', 'Mundo 2: mini-jefa', 'La Plañidera y sus esquirlas', '420 / 168'],
        ['27', '10/10', 'Mundo 2: jefa', 'La Doncella del Lamento: olas, inundación, congelación y llanto', '434 / 185'],
        ['28', '10/10', 'Mundo 2: arte', 'Enemigos, jefa, mecánicas, tileset y fondo del mundo', '450 / 188'],
        ['29', '10/10', 'Mundo 2: cinemáticas', 'Presentación y derrota de la Doncella', '460 / 190'],
        ['30', '12/10', 'Cuarta prueba del autor', 'Ajustes del Nivel 2: oxígeno de 10 s, muralla de hielo liso y pista del tapón', '470 / 196'],
        ['31', '13/10', 'Puntaje y puertas', 'Puntaje en lugar de monedas, niveles más largos, menú con portada, puertas, verjas y checkpoints de cada mundo', '497 / 207'],
        ['32', '14/10', 'Mundo 3: recorrido', 'Nivel 3 montado: la linde, el bosque cerrado, el claro y la arena', '519 / 210'],
        ['33', '14/10', 'Mundo 3: mecánicas', 'Oscuridad por tramos, luz de Kael, antorchas, cepos, ramas que ceden y anillos de ruido', '534 / 220'],
        ['34', '14/10', 'Mundo 3: enemigos', 'Espectro, Centinela Ciego y Acechador', '547 / 234'],
        ['35', '14/10', 'Mundo 3: jefe', 'El Espectro del Pavor: tajo, embestida, hojas y tres sombras', '553 / 244'],
        ['36', '15/10', 'Mundo 3: arte', 'Enemigos, jefe, mecánicas, tileset y fondo del bosque', '564 / 251'],
        ['37', '16/10', 'Mundo 3: puertas y cinemáticas', 'Presentación y derrota del Pavor y salida del mundo', '573 / 252'],
        ['38', '17/10', 'Quinta prueba del autor', 'Brillo del fragmento y zonas de daño sin rectángulos', '573 / 252'],
        ['39', '19/10', 'La ciénaga', 'Un tramo de agua del Nivel 3 que solo se cruza con el Dash Acuático', '578 / 254'],
        ['40', '20/10', 'Mundo 4 sin arte', 'Recorrido, barriles, mina, memorias, Penitente, Artillero, Verdugo y Juez Silente', '630 / 278'],
        ['41', '21/10', 'Mundo 4: arte', 'Limpieza e importación del arte del Penitente, el Artillero y el Verdugo (en curso)', '—'],
    ])
    nota32 = d.buscar('Nota. EM: pruebas EditMode; PM: pruebas PlayMode')
    d.poner(nota32, [('i', 'Nota. '), 'EM: pruebas EditMode; PM: pruebas PlayMode, acumuladas al final de cada iteración. '
                     'Fechas de 2026. La iteración 41 estaba en curso al cerrar este documento. Fuente: historial de cambios '
                     '(CHANGELOG.md) del proyecto.'])

    # Los mundos 2 y 3, antes de «Progresión, narrativa y registro emocional».
    destino = d.buscar('Progresión, narrativa y registro emocional.', estilo='Ttulo4tesis')
    bloques = [
        d.parrafo('t4', ['El mundo de la Tristeza.']),
        d.parrafo('texto', [
            'El segundo mundo se construyó en siete iteraciones sobre los mismos sistemas que el primero: solo se '
            'programó lo que la Tristeza tiene de propio. El agua cambia la física de Kael (velocidad × 0,6, gravedad × 0,3 '
            'y un salto más alto) y le da 10 s de aire; al agotarse, pierde un corazón cada 2 s, sin morir de golpe. El '
            'tapón de hielo solo cede al Ataque Cargado, de modo que la habilidad del mundo anterior es la llave del '
            'siguiente, y Kael lo dice en una pista la primera vez que llega ante él. Los ecos de intentos graban los últimos '
            '8 s de Kael y, al reaparecer, una sombra translúcida repite el intento fallido. El Lamento, el Eco de este '
            'mundo, comparte el cuerpo y el controlador de animación del Eco de la Ira y vuelve pesado a Kael durante 2 s; '
            'la Escultura Viviente duerme como una estatua más del decorado; la Medusa de Hielo no muere, solo se aparta, y '
            'la Plañidera, la mini-jefa, lanza sus esquirlas y después llora sin defenderse (', ('ref', 'personajes_tristeza'),
            '). La Doncella del Lamento, la jefa, solo es vulnerable mientras llora (', ('ref', 'jefes_23'), '). La ',
            ('ref', 'nivel2_capturas'), ' muestra cuatro momentos del recorrido.']),
        d.leyenda('Figura', 'personajes_tristeza', 'Enemigos y jefa del mundo de la Tristeza'),
        d.figura(os.path.join(FIG, 'personajes_tristeza.png'), 10.5),
        d.nota(['Selección de fotogramas, a escala distinta por fila. Arte del proyecto; montaje propio.']),
        d.leyenda('Figura', 'nivel2_capturas', 'Recorrido del Nivel 2: (a) el primer hielo y un Lamento, (b) la plaza helada '
                  'y la Plañidera, (c) la calle hundida y sus Medusas y (d) la Doncella del Lamento, llorando'),
        d.figura(os.path.join(FIG, 'nivel2_capturas.jpg'), 15.5),
        d.nota(['Capturas del juego a 1920 × 1080 (versión del 21 de octubre de 2026). En (c), arriba a la izquierda, el '
                'indicador del oxígeno.']),
        d.parrafo('t4', ['El mundo del Miedo.']),
        d.parrafo('texto', [
            'El tercer mundo añadió la oscuridad: cada tramo del nivel tiene su luz, que cambia poco a poco según dónde está '
            'Kael, y Kael lleva su propio círculo de luz de 3 u. Las antorchas se encienden con la tecla de interactuar y '
            'alumbran 8 u durante 15 s, pero despiertan a los Espectros dormidos en su radio. Correr hace ruido cada 0,4 s, '
            'que se oye a 6 u y que el jugador ve como anillos que salen de los pies de Kael; caminar o agacharse es '
            'silencioso. Los cepos solo se ven a 1 u o bajo una antorcha, y las ramas podridas ceden al pisarlas. El '
            'Espectro, el Eco de este mundo, duerme y casi no se ve; el Centinela Ciego no ve, oye, y el Acechador, el '
            'mini-jefe, embiste hacia el ruido y queda aturdido si choca con un árbol (', ('ref', 'personajes_miedo'), '). '
            'El Espectro del Pavor es invisible e intangible salvo cuando ataca, y en su segunda fase se multiplica en tres '
            'sombras, de las que solo la verdadera lleva luz (', ('ref', 'jefes_23'), '). Por último, la ciénaga obliga a '
            'usar el Dash Acuático del mundo anterior: sus marañas sumergidas sujetan a Kael y solo un dash las cruza (',
            ('ref', 'nivel3_capturas'), ').']),
        d.leyenda('Figura', 'personajes_miedo', 'Enemigos y jefe del mundo del Miedo'),
        d.figura(os.path.join(FIG, 'personajes_miedo.png'), 14.0),
        d.nota(['Selección de fotogramas, a escala distinta por fila. Arte del proyecto; montaje propio.']),
        d.leyenda('Tabla', 'jefes_23', 'Fases de combate de la Doncella del Lamento y del Espectro del Pavor'),
        d.tabla(['Jefe', 'Fase', 'Ataques', 'Cuándo se le puede herir'], [
            ['Doncella del Lamento (26 de vida)', '1 (100 % a 50 %)', 'Ola a ras del hielo; inundación de la franja donde está '
             'Kael; otra ola', 'Baja al hielo a llorar 1,5 s: cada golpe vale el doble'],
            ['', '2 (50 % a 0 %)', 'Grito; ola doble; inundación y congelación (púas y una capa de hielo que se pisa)',
             'Llora 1,2 s sobre su capa de hielo o metida en su agua'],
            ['Espectro del Pavor (22 de vida)', '1 (100 % a 50 %)', 'Tajo a la espalda de Kael; embestida a ras de suelo; '
             'hojas en cuatro o cinco franjas', 'Tras cada ataque se queda visible y jadeando: tras el tajo, 1,5 s'],
            ['', '2 (50 % a 0 %)', 'Grito; tres sombras que lanzan su tajo una tras otra; hojas; embestida',
             'Solo la figura verdadera, la que lleva una luciérnaga dorada; golpear una sombra encoge la luz de Kael'],
        ], [2100, 1500, 3200, 2550]),
        d.nota(['Cada ataque avisa antes de golpear (1 s en el Pavor). Datos de los assets DoncellaLamento y EspectroDelPavor.']),
        d.leyenda('Figura', 'nivel3_capturas', 'Recorrido del Nivel 3: (a) la trinchera del primer checkpoint, (b) un '
                  'Centinela Ciego en el bosque cerrado, (c) la ciénaga y (d) el Acechador en el claro'),
        d.figura(os.path.join(FIG, 'nivel3_capturas.jpg'), 15.5),
        d.nota(['Renders de la escena con el arte del mundo (16 al 19 de octubre de 2026), sin la oscuridad del nivel: en el '
                'juego, fuera de la luz de Kael y de las antorchas, el bosque es mucho más oscuro.']),
    ]
    for b in bloques:
        destino.addprevious(b)
    # Las cinemáticas de los mundos 2 y 3 y el mundo 4, después de las del primero: antes de las pruebas (8.1.8).
    destino = d.buscar('8.1.8 Fase 4: pruebas de software', estilo='Ttulo3tesis')
    bloques = [
        d.parrafo('t4', ['Cinemáticas, puertas y puntaje.']),
        d.parrafo('texto', [
            'Cada jefe construido tiene su presentación al entrar en la arena y su derrota, y cada mundo, su cinemática de '
            'salida, con el mismo sistema de planos en pixel art del primer mundo (', ('ref', 'cinematicas_23'), '). La '
            'puerta de salida de cada mundo está cerrada hasta recoger el fragmento del jefe, y cada mundo tiene su verja de '
            'la arena y su checkpoint dibujados. Desde la iteración 31 el juego reemplazó las monedas por un puntaje: un '
            'enemigo común vale 100, un mini-jefe entre 500 y 800, un jefe entre 1500 y 5000, y al cruzar la salida se suma un '
            'bono de 5 puntos por cada segundo que sobre del tiempo de referencia del nivel. Así, correr y pelear compiten: '
            'quien se salta un mini-jefe gana tiempo, pero pierde sus puntos. El menú principal y el selector muestran el '
            'mejor puntaje de cada mundo.']),
        d.leyenda('Figura', 'cinematicas_23', 'Planos de las cinemáticas de los mundos 2 y 3: presentación (a) y derrota (b y '
                  'c) de la Doncella del Lamento, presentación (d) y derrota (e) del Espectro del Pavor y salida del mundo del '
                  'Miedo (f)'),
        d.figura(os.path.join(FIG, 'cinematicas_23.jpg'), 15.5),
        d.nota(['Capturas de la cámara de las cinemáticas a 1920 × 1080. Arte del proyecto.']),
        d.parrafo('t4', ['El mundo de la Culpa, en desarrollo.']),
        d.parrafo('texto', [
            'El cuarto mundo se aprobó el 20 de octubre de 2026 y ese mismo día se montaron, sin arte, su recorrido, sus '
            'mecánicas, sus enemigos y su jefe: los barriles que solo enciende el Ataque Cargado, una mina que sube y baja '
            'en el río, las cuatro memorias, cuyo recuerdo vuelve más agresivos a los enemigos de su zona, el indicador de '
            'la Invisibilidad en el HUD, el Penitente, el Artillero, el Verdugo y el Juez Silente, con 52 pruebas nuevas. Su '
            'arte se estaba limpiando e importando al cerrar este documento, y faltan sus cinemáticas y sus puertas.']),
    ]
    for b in bloques:
        destino.addprevious(b)

    p = d.buscar('Los niveles son datos: cada uno reúne su escena')
    d.poner(p, ['Los niveles son datos: cada uno reúne su escena, mundo, emoción, jefe, fragmento, cinemáticas, diálogos y '
                'reglas, y una base de datos los ordena y decide cuáles están desbloqueados. El gestor del juego lleva la '
                'progresión y la guarda en JSON con escritura atómica. Las cinemáticas son listas de planos con sus tiempos, '
                'textos, temblores y sonidos; los planos en pixel art se dibujan con una cámara propia a 240 × 135 píxeles, '
                'ampliada por un factor entero. El juego tiene la cinemática inicial (12 planos, unos 80 s), la presentación y '
                'la derrota de cada jefe construido (', ('ref', 'cinematicas'), ' y ', ('ref', 'cinematicas_23'), ') y la '
                'salida de cada mundo, y los diálogos internos de Kael aparecen en cuatro momentos de cada mundo: apertura, '
                'eco de memoria, antes del jefe y cierre.'])
    p = d.buscar('El registro emocional es el instrumento de recolección dentro del juego.')
    d.poner(p, ['El registro emocional es el instrumento de recolección dentro del juego. Al terminar cada mundo pregunta '
                '«¿Con qué intensidad sentiste [emoción] en este mundo?» con una escala de 1 a 5, un comentario opcional y un '
                'código de participante opcional (', ('ref', 'lam_juego'), 'c), y guarda las respuestas en JSON y CSV sin '
                'datos personales. Una parte del código no va en el juego: son herramientas del editor que generan los '
                'prefabs, las escenas, los datos de los niveles y las animaciones, y verificadores que comprueban que el arte '
                'de cada mundo siga montado como se diseñó. El ejecutable se construye para Windows de 64 bits con las escenas '
                'del menú, el selector y los niveles; ', ('hl', 'indiquen el tamaño del último ejecutable que construyan'),
                '.'])

    # Las capturas de la navegación y del juego, con el HUD de ahora (puntaje y tiempo).
    _cambiar_imagen(d, 'Figura 5.', os.path.join(FIG, 'navegacion.jpg'), 16.5)
    _cambiar_imagen(d, 'Figura 18.', os.path.join(FIG, 'interfaces.jpg'), 16.5)
    for nota in [e for e in d.bloques() if texto_de(e) == 'Nota. Capturas del juego a 1920 × 1080 (versión del 8 de octubre de 2026).']:
        d.poner(nota, [('i', 'Nota. '), 'Capturas del juego a 1920 × 1080 (versión del 21 de octubre de 2026).'])
    p = d.buscar('La interfaz se diseñó para no tapar el juego')
    d.poner(p, ['La interfaz se diseñó para no tapar el juego: el HUD ocupa las esquinas superiores (vida y mundo a la '
                'izquierda; puntaje y tiempo a la derecha; bajo el agua, el oxígeno) y la línea de diálogo aparece en la '
                'parte inferior; la barra del jefe solo se muestra durante el combate. Las pantallas superpuestas (pausa, '
                '«Has caído», «Mundo superado», mecánicas) comparten una misma plantilla con el color de acento de cada mundo, '
                'y toda la interfaz se construye por código, lo que mantiene la misma tipografía y los mismos márgenes en '
                'todas. La ', ('ref', 'lam_juego'), ' muestra las interfaces durante el juego.'])


def pruebas_y_resultados(d):
    p = d.buscar('Las pruebas del software se planearon desde la fase de diseño')
    d.poner(p, ['Las pruebas del software se planearon desde la fase de diseño, en concordancia con la metodología: cada '
                'iteración de la fase 3 terminó con sus pruebas automatizadas, y al final se ejecutaron los casos de caja '
                'negra y de caja blanca sobre la versión completa. En la última ejecución, del 20 de octubre de 2026, se '
                'aprobaron las 908 pruebas automatizadas, 48 de los 50 casos de caja negra (los otros 2 requieren el '
                'ejecutable y se hacen a mano) y los 14 casos de caja blanca. El capítulo 9 presenta la planeación, los casos '
                'y los resultados, separados de la investigación con usuarios.'])
    p = d.buscar('Los dos cuestionarios se montaron en Microsoft Forms en modo anónimo')
    d.comentar(p, C3 + 'los evaluadores pidieron incluir los resultados de la prueba piloto. Los del pretest están en el '
               'apartado 8.1.10; la comparación con el postest no se puede hacer hasta aplicarlo (con un código de participante '
               'para emparejar las respuestas). Si ya tienen el postest, envíenme los dos Excel y completo la Tabla 40 y la '
               'discusión con la prueba de Wilcoxon.')
    p = d.buscar('Los resultados muestran dos cosas distintas que conviene no confundir.')
    d.poner(p, ['Los resultados muestran dos cosas distintas que conviene no confundir. Las pruebas de software (capítulo 9) '
                'indican que el prototipo hace lo que se diseñó: las reglas de los tres mundos jugables (esperar el momento en '
                'la Ira, dejarse hundir en la Tristeza, avanzar despacio y enfrentar a propósito en el Miedo, e integrar cada '
                'emoción como una habilidad que el mundo siguiente exige) están implementadas y verificadas, y el registro '
                'emocional guarda los datos sin perderlos ni recoger información personal. Esto es condición necesaria para la '
                'evaluación con usuarios, pero no la sustituye: que una mecánica funcione no demuestra que produzca el efecto '
                'psicológico que se le atribuye en la matriz de estrategias.'])

    t42 = d.tabla_tras('Tabla 42.')
    d.celda(t42, 2, 1, ['Diseño de los seis mundos y de los personajes, requisitos, casos de uso y modelos de análisis, '
                        'diseño y sistema (apartados 8.1.2 a 8.1.6), recogidos en el documento de diseño del juego (Anexo D)'])
    d.celda(t42, 3, 0, ['OE3. Desarrollar el prototipo y verificar su funcionamiento'])
    d.celda(t42, 3, 1, ['Tres mundos jugables de principio a fin (Ira, Tristeza y Miedo), con sus jefes, mecánicas, '
                        'cinemáticas y registro emocional, y el cuarto en desarrollo; 908 pruebas aprobadas (apartado 8.1.7 y '
                        'capítulo 9)'])
    d.celda(t42, 3, 2, ['Cumplido para tres mundos; el cuarto, en desarrollo, y los dos últimos, diseñados'])
    d.celda(t42, 4, 0, ['OE4. Validar el prototipo con una prueba piloto'])

    p = d.buscar('Para elegir el modelo de estimación se compararon dos opciones.')
    d.poner(p, ['Para elegir el modelo de estimación se compararon dos opciones. La primera fue COCOMO II, que estima el '
                'esfuerzo a partir del tamaño del código. El código del juego, sin las herramientas del editor ni las '
                'pruebas, tiene 22 526 líneas efectivas (sin comentarios ni líneas en blanco) en 210 archivos; con los '
                'factores de escala y los multiplicadores en su valor nominal, el modelo estima unos 90 meses-persona y una '
                'duración de 15 meses, con un equipo de unas seis personas. Esa estimación no corresponde al proyecto: COCOMO '
                'II supone un desarrollo tradicional, mientras que aquí el motor aporta la física, el renderizado y la '
                'animación, y buena parte del código de la v2 se escribió con apoyo de un asistente de IA (apartado 7.8). '
                'Usarlo habría sobredimensionado el costo.'])


def capitulo9(d):
    p = d.buscar('Los casos de caja negra se agruparon por módulo')
    d.poner(p, ['Los casos de caja negra se agruparon por módulo: movimiento (', ('ref', 'cn_mov'), '), combate y mecánicas (',
                ('ref', 'cn_com'), '), jefe (', ('ref', 'cn_jef'), '), flujo, menús y registro (', ('ref', 'cn_flu'),
                ') y mundos de la Tristeza y del Miedo (', ('ref', 'cn_mundos'), '). La columna «Verificado por» indica la '
                'prueba automatizada que ejecuta el caso con la entrada simulada y comprueba el resultado; todas se aprobaron '
                'en la ejecución del 20 de octubre de 2026. Los casos de cerrar la aplicación y de abrir el ejecutable en otro '
                'equipo no se pueden automatizar dentro del editor y se ejecutan a mano.'])
    nota48 = d.tabla_tras('Tabla 48.').getnext()
    assert estilo_de(nota48) == 'Notatesis'
    filas = [
        ['CB-36', 'Hielo y agua', 'Correr sobre el hielo y caer al agua de la calle', 'Kael resbala en el hielo; bajo el agua '
         'va al 60 % y salta más alto', 'Nivel2PlayModeTests: EnElHielo_Resbala_YEnElAgua_TodoPesa', 'Aprobado'],
        ['CB-37', 'Oxígeno', 'Quedarse bajo el agua sin respirar', 'A los 10 s pierde un corazón cada 2 s; respirar lo salva',
         'SinAire_KaelPierdeUnCorazonCada2s_SinRetroceso_YRespirarLoSalva', 'Aprobado'],
        ['CB-38', 'Hielo rompible', 'Golpear el tapón con un golpe normal y con el Ataque Cargado', 'El normal lo agrieta; el '
         'cargado lo rompe y Kael cae al agua', 'ElTapon_UnGolpeLoAgrieta_YElCargadoLoRompe_YKaelCaeAlAgua', 'Aprobado'],
        ['CB-39', 'Eco de intentos', 'Morir y reaparecer', 'Una sombra repite el intento fallido y se va',
         'TrasMorir_UnaSombraRepiteElIntento_YSeVa', 'Aprobado'],
        ['CB-40', 'Lamento', 'Dejarse alcanzar por su zarpazo', 'Kael va a la mitad de velocidad durante 2 s',
         'ElLamento_TeVuelvePesado_2s', 'Aprobado'],
        ['CB-41', 'Doncella del Lamento', 'Golpearla flotando y mientras llora', 'Flotando no recibe daño; llorando, cada '
         'golpe vale el doble', 'DoncellaPlayModeTests: SoloSeLaHiereMientrasLlora_YEntoncesCadaGolpeValeElDoble', 'Aprobado'],
        ['CB-42', 'Sigilo', 'Correr y caminar con Alt en la linde', 'Correr hace ruido y lo muestran los anillos; caminar, no',
         'Nivel3PlayModeTests: AlEmpezar_KaelEstaEnLaLinde_YCorrerHaceRuido_PeroCaminarNo', 'Aprobado'],
        ['CB-43', 'Antorcha', 'Pulsar E junto a una antorcha', 'Se enciende, alumbra su tiempo y despierta a los Espectros de '
         'su luz', 'MecanicasMiedoPlayModeTests: Antorcha_SeEnciendeConE_SoloAlAlcance_AlumbraSuTiempo_YVuelveAlReaparecer',
         'Aprobado'],
        ['CB-44', 'Cepo', 'Pisar un cepo oculto', 'Quita 1, sujeta a Kael 1 s y su chasquido se oye a 8 u',
         'Cepo_AlPisarlo_QuitaUno_LoSujetaUnSegundo_SuenaAOchoUnidades_YSeRearmaAlReaparecer', 'Aprobado'],
        ['CB-45', 'Centinela Ciego', 'Hacer ruido cerca de él', 'Se detiene, ladea la cabeza, va hacia el ruido y apuñala',
         'EnemigosMiedoPlayModeTests: CentinelaCiego_AlOirSeParaYLadeaLaCabeza_VaHaciaElRuido_YApunalaAlli', 'Aprobado'],
        ['CB-46', 'Acechador', 'Hacerlo embestir contra un árbol', 'Queda aturdido y los golpes valen el doble',
         'Acechador_SeEstrellaContraUnArbol_YAturdidoLosGolpesValenElDoble', 'Aprobado'],
        ['CB-47', 'Ciénaga', 'Cruzar una maraña sin y con el Dash Acuático', 'Sin el dash no se cruza; con él, sí',
         'Nivel3PlayModeTests: SinElDashAcuatico_LaMaranaNoSeCruza; LaCienaga_LaMaranaSujetaAKael_YSoloElDashAcuaticoLaCruza',
         'Aprobado'],
        ['CB-48', 'Espectro del Pavor', 'Golpearlo fuera de sus ataques y tras el tajo', 'Fuera del ataque los golpes lo '
         'atraviesan; jadeando tras el tajo se le hiere', 'EspectroDelPavorPlayModeTests: ElTajo_ApareceALaEspaldaDeKael_'
         'AvisaUnSegundo_QuitaDos_YJadeaUnoYMedio', 'Aprobado'],
        ['CB-49', 'Puntaje', 'Derrotar enemigos y cruzar la salida', 'Suma lo que vale cada enemigo y el bono por tiempo; el '
         'fin de nivel lo muestra', 'PuntajePlayModeTests: CruzarLaSalida_AnotaElPuntajeConSuBonoPorTiempo_YLoEnseñaElFinDeNivel',
         'Aprobado'],
        ['CB-50', 'Puerta de salida', 'Recoger el fragmento del jefe', 'La puerta, cerrada hasta entonces, se abre',
         'PuertasPlayModeTests: LaPuertaDeSalida_SeVeCerrada_YSeAbreAlRecogerElFragmento', 'Aprobado'],
    ]
    d.despues(nota48,
              d.leyenda('Tabla', 'cn_mundos', 'Casos de prueba de caja negra: mundos de la Tristeza y del Miedo'),
              d.tabla(['N.º', 'Caso', 'Entrada', 'Resultado esperado', 'Verificado por', 'Estado'], filas,
                      [700, 1250, 1900, 2300, 2350, 850]),
              d.nota(['En los casos aprobados, el resultado obtenido coincide con el esperado. Las pruebas se ejecutan sobre '
                      'las escenas reales de los niveles 2 y 3.']))

    for e in d.bloques():
        if texto_de(e) == 'Nota. Todas las pruebas se aprobaron en la ejecución del 8 de octubre de 2026.':
            d.poner(e, [('i', 'Nota. '), 'Todas las pruebas se aprobaron en la ejecución del 20 de octubre de 2026.'])

    p = d.buscar('En la última ejecución, del 8 de octubre de 2026')
    d.poner(p, ['En la última ejecución, del 20 de octubre de 2026, se aprobaron las 908 pruebas automatizadas: 630 EditMode '
                'y 278 PlayMode, sin fallos, omisiones ni resultados inconclusos (', ('ref', 'res_auto'), '). Las EditMode '
                'tardaron 13,4 s y las PlayMode 580,9 s, porque estas cargan las escenas de los niveles y esperan a la '
                'física. La ', ('ref', 'res_sub'), ' las distribuye por subsistema: las de los niveles y sus mecánicas son '
                'las más numerosas, porque cada mundo comprueba que su recorrido se puede cruzar con las medidas reales de '
                'Kael, que su escena sigue a su tabla de diseño y que cada mecánica funciona en la escena real.'])
    t50 = d.tabla_tras('Tabla 50.')
    for fila, valores in ((1, ['EditMode', '60', '630', '630', '0', '0', '13,4']),
                          (2, ['PlayMode', '30', '278', '278', '0', '0', '580,9']),
                          (3, ['Total', '90', '908', '908', '0', '0', '594,3'])):
        for c, v in enumerate(valores):
            d.celda(t50, fila, c, [v])
    nota50 = t50.getnext()
    d.poner(nota50, [('i', 'Nota. '), 'Unity Test Framework 1.8.0 sobre la versión del 20 de octubre de 2026 de Tesis2.0 '
                     '(rama nivel4-recorrido). Un conjunto es una clase de pruebas. El reporte completo, caso por caso, se '
                     'entrega con el código fuente.'])
    t51 = d.tabla_tras('Tabla 51.')
    res, _ = contar()
    filas51 = [[g, str(e), str(p), str(e + p)] for g, (e, p) in res.items()]
    filas51.append(['Total', str(sum(v[0] for v in res.values())), str(sum(v[1] for v in res.values())),
                    str(sum(v[0] + v[1] for v in res.values()))])
    n_actual = len(t51.findall(qn('w:tr'))) - 1
    for i, f in enumerate(filas51):
        for c, v in enumerate(f):
            d.celda(t51, i + 1, c, [v])
    for _ in range(n_actual - len(filas51)):
        d.quitar_fila(t51, len(filas51) + 1)

    p = d.buscar('Las pruebas automatizadas tienen límites')
    d.poner(p, ['Las pruebas automatizadas tienen límites: no se midió la cobertura de código; las PlayMode usan entrada '
                'simulada y no evalúan la sensación de control ni la lectura visual, que dependen de las pruebas con '
                'jugadores; el mundo 4 está en desarrollo y los mundos 5 y 6 no están construidos, y algunas pruebas que '
                'dependen del tiempo o del azar fallaron alguna vez sin que hubiera un defecto: dos que miden tiempos (el '
                'margen tras el borde y el ciclo de la torreta), con el equipo muy cargado, y una que exigía siempre cinco '
                'franjas de hojas del Pavor cuando el diseño admite cuatro o cinco; esta última se corrigió para comprobar lo '
                'que dice el diseño.'])
    p = d.buscar('Estas pruebas observaron el comportamiento del sistema cuando aumenta el trabajo')
    d.poner(p, ['Estas pruebas observaron el comportamiento del sistema cuando aumenta el trabajo (carga), cuando se lleva al '
                'extremo (estrés) y cuando se usa de forma continua (estabilidad). Se ejecutaron el 8 de octubre de 2026 con '
                'una prueba automatizada sobre el Nivel 1 real, en el mismo equipo y en modo batch, por lo que miden el costo '
                'de la lógica, la física y la animación sin dibujar en pantalla; sirven para comparar escenarios y detectar '
                'fugas o errores, no como medida de los cuadros por segundo que verá el jugador. Los mundos 2 y 3 usan los '
                'mismos sistemas; repetir estas pruebas en ellos, con la oscuridad y el agua, queda como recomendación.'])
    t54 = d.tabla_tras('Tabla 54.')
    d.anadir_filas(t54, [
        ['Kael no podía salir del agua del río ni de la ciénaga a la orilla', 'Con 4 u de hondo, el impulso al salir del agua lo '
         'dejaba 0,22 u por debajo del borde', 'El agua bajó a 3,5 u; una prueba de diseño comprueba el margen en cada orilla',
         'Prueba EditMode de diseño (DisenoNivel3Tests)'],
        ['La gema del Espectro del Pavor apenas se veía al vencerlo', 'Una luz 2D añadida a un objeto inactivo no ilumina '
         'ninguna capa', 'Se fijan las capas que ilumina y un verificador lo comprueba al regenerar', 'Sesión de juego del '
         'autor; capturas'],
        ['El tapón de hielo del Nivel 2 no se entendía', 'Nada indicaba que hacía falta el Ataque Cargado', 'Kael lo dice en '
         'una pista la primera vez que llega ante el tapón', 'Sesión de juego del autor; prueba PlayMode nueva'],
    ])
    t55 = d.tabla_tras('Tabla 55.')
    d.celda(t55, 1, 1, ['Fragmentum: Ecos de la Identidad, v2 (Tesis2.0, rama nivel4-recorrido, versión del 20 de octubre '
                        'de 2026; ', ('hl', 'commit de la versión entregada'), ')'])
    d.celda(t55, 2, 1, ['20 de octubre de 2026'])
    d.celda(t55, 4, 1, ['908 ejecutadas (630 EditMode y 278 PlayMode), 908 aprobadas, 0 fallidas, 0 omitidas'])
    d.celda(t55, 5, 1, ['50 casos: 48 aprobados por pruebas automatizadas y 2 pendientes de ejecución manual'])
    d.celda(t55, 7, 1, ['Ejecutadas sobre el Nivel 1 (8 de octubre): hasta 100 enemigos simultáneos, 10 recargas del nivel, '
                        '3 min de sesión continua y 1000 ciclos de guardado; sin errores (apartado 9.5)'])
    d.celda(t55, 9, 1, ['Casos CB-34 y CB-35 en el ejecutable, el arte y las cinemáticas del mundo 4, el sonido y la prueba '
                        'de explotación con participantes'])
    d.celda(t55, 10, 1, ['El prototipo es estable y funcional para la prueba piloto; los tres primeros mundos se juegan de '
                         'principio a fin'])


def conclusiones(d):
    p = d.buscar('Las conclusiones se organizan según los objetivos específicos')
    d.poner(p, ['Las conclusiones se organizan según los objetivos específicos y resumen la evidencia que respalda cada una; '
                'la última recoge la valoración del experto disciplinar en psicología.'])
    d.comentar(p, C3 + 'los evaluadores pidieron conclusiones rigurosas que reflejen la validación de los resultados y la '
               'perspectiva del experto disciplinar (Psicología). Las conclusiones 1 a 3 salen de lo construido y verificado; '
               'la 4 y la 5 dependen del postest, y la 6 necesita la valoración real de la asesora en psicología (el formato '
               'del Anexo F sirve para pedírsela). No la inventé: completen lo resaltado con lo que ella diga.')
    p = d.buscar('2. Sobre el diseño (OE2).')
    d.poner(p, ['2. Sobre el diseño (OE2). Se diseñaron los seis mundos con su emoción, mecánicas, enemigos, jefe y guion, y '
                'se especificaron quince requisitos funcionales, siete no funcionales, catorce casos de uso y los modelos de '
                'análisis, diseño y sistema; todo ello se recoge en el documento de diseño del juego (Anexo D). Diseñar a '
                'partir de la emoción dio coherencia a cada recorrido: en la Ira, cada peligro enseña a contener el impulso; '
                'en la Tristeza, solo se avanza dejándose hundir, y en el Miedo, avanzando despacio y enfrentando a propósito.'])
    p = d.buscar('3. Sobre la implementación y la verificación (OE3).')
    d.poner(p, ['3. Sobre el desarrollo y la verificación (OE3). Se desarrolló un prototipo funcional con tres mundos jugables '
                'de principio a fin y el cuarto en desarrollo. La arquitectura sobre máquinas de estados y datos editables '
                'permitió construir el segundo y el tercer mundo en dos semanas sin perder lo construido: las pruebas '
                'automatizadas pasaron de 520 a 908 y todas se aprobaron al final de cada iteración. Cada mundo exige la '
                'habilidad ganada en el anterior (el Ataque Cargado rompe el hielo de la Tristeza y el Dash Acuático cruza la '
                'ciénaga del Miedo), de modo que la progresión misma expresa la idea del juego: lo que se integra se usa. La '
                'principal dificultad fue el volumen de arte que exige cada mundo; falta además el sonido.'])
    p = d.buscar('5. Sobre el análisis (OE5).')
    d.despues(p, d.parrafo('vineta', [
        '6. Sobre la valoración del experto disciplinar. ',
        ('hl', 'Resuman aquí la valoración de la psicóloga Ashley Vanessa Acevedo Cepeda, asesora del proyecto, con el formato '
               'del Anexo F: si la matriz de estrategias representa estrategias de regulación reconocidas, si cada mundo '
               'representa su emoción de forma adecuada y sin estigmatizar, qué riesgos ve para jóvenes en crisis y en qué '
               'condiciones recomienda usar el prototipo.')]))
    p = d.buscar('En conjunto, el trabajo propuso un videojuego para el autoconocimiento')
    d.poner(p, ['En conjunto, el trabajo diseñó un videojuego para el autoconocimiento, lo desarrolló con una metodología '
                'verificable hasta tener tres mundos jugables, lo validó como software y estableció la línea base de las '
                'variables psicológicas. La pregunta de investigación, sobre cómo un videojuego narrativo con simbolismo '
                'emocional puede contribuir al autoconocimiento, tiene hoy una respuesta de diseño (la matriz y su '
                'implementación en tres emociones) y queda pendiente su respuesta empírica, que depende del postest y de la '
                'valoración del experto disciplinar.'])

    p = d.buscar('Pedir el aval del comité de ética')
    d.despues(p,
              d.parrafo('vineta', ['Pedir a la asesora en psicología, y si es posible a un segundo profesional de la '
                                   'psicología, la valoración del prototipo con el formato del Anexo F, e incorporarla a las '
                                   'conclusiones.']),
              d.parrafo('vineta', ['Grabar el video del prototipo con el guion del Anexo E y dejar el código fuente en un '
                                   'repositorio o una carpeta accesible para los evaluadores.']))
    p = d.buscar('Construir los mundos 2 a 6 sobre la arquitectura existente')
    d.poner(p, ['Terminar el mundo de la Culpa (su arte, sus cinemáticas y sus puertas) y construir los mundos 5 y 6 sobre la '
                'arquitectura existente, con la que los mundos 2 y 3 se hicieron en dos semanas; añadir la música y los efectos '
                'de sonido, que el prototipo todavía no tiene.'])


def referencias(d):
    acerenza = d.buscar('Acerenza, N., Coppes, A.')
    d.despues(acerenza, d.parrafo('referencia', ['Adams, E. (2014). ', ('i', 'Fundamentals of game design'),
                                                 ' (3.ª ed.). New Riders.']))
    rodriguez = d.buscar('Rodríguez, M. R. y Padilla')
    d.despues(rodriguez, d.parrafo('referencia', ['Rogers, S. (2014). ', ('i', 'Level up! The guide to great video game design'),
                                                  ' (2.ª ed.). Wiley.']))
