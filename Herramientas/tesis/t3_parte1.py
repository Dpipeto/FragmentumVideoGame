"""TesisIA tras el tercer concepto: portada, preliminares, capítulos 1 y 3 (metodología) y diseño (8.1.1 a 8.1.3)."""
import copy
import os

from docx_util import qn, texto_de, estilo_de

FIG = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'fig')
DIS = r'C:\Users\dpipe\OneDrive\Desktop\Importante\Tesis2.0\Docs\Diseno'

C3 = 'Tercer concepto (16 de octubre de 2026): '


def portada(d):
    # La clasificación del documento (observación 5): proyecto de investigación y desarrollo
    # tecnológico, no monografía. En la portada, sobre la línea del título que se opta.
    linea = d.buscar('Trabajo de grado para optar el título de Ingeniero de Sistemas')
    vacio = linea.getprevious()
    assert estilo_de(vacio) == 'Portadatesis' and not texto_de(vacio)
    d.poner(vacio, ['PROYECTO DE INVESTIGACIÓN Y DESARROLLO TECNOLÓGICO'])
    d.comentar(vacio, C3 + 'los evaluadores observaron que el trabajo es un proyecto de investigación y desarrollo '
               'tecnológico (construye un prototipo en Unity y lo evalúa con un estudio empírico) y no una monografía de '
               'revisión, y pidieron ajustar la clasificación en la portada y en los metadatos. Se añadió esta línea aquí '
               'y en la contraportada, y se cambiaron las propiedades del archivo (Archivo › Información). Corrijan '
               'también la modalidad en el formulario de radicación de la Facultad.')
    # Contraportada: tras su título, en el primer párrafo vacío.
    titulos = [p for p in d.body.iterchildren() if p.tag == qn('w:p') and texto_de(p).startswith('VIDEOJUEGO COMO HERRAMIENTA')]
    contra = titulos[1]
    siguiente = contra.getnext()
    siguiente = siguiente.getnext()
    assert estilo_de(siguiente) == 'Portadatesis' and not texto_de(siguiente)
    d.poner(siguiente, ['Proyecto de investigación y desarrollo tecnológico presentado como trabajo de grado '
                        'para optar el título de Ingeniero de Sistemas'])

    cp = d.doc.core_properties
    cp.subject = 'Proyecto de investigación y desarrollo tecnológico (trabajo de grado, formato FPG-V 9.0)'
    cp.category = 'Proyecto de investigación y desarrollo tecnológico'
    cp.keywords = ('Fragmentum; videojuego serio; autoconocimiento; regulación emocional; Unity; prototipo; '
                   'prueba piloto; documento de diseño del juego (GDD)')
    cp.comments = 'Versión de trabajo actualizada tras el tercer concepto de evaluación (16 de octubre de 2026).'


RESUMEN = [
    'Este proyecto de investigación y desarrollo tecnológico diseñó, desarrolló y validó en una prueba piloto '
    'Fragmentum: Ecos de la Identidad, un videojuego narrativo de plataformas en 2D que convierte emociones como la ira, '
    'la tristeza, el miedo y la culpa en reglas de juego, con el fin de favorecer el autoconocimiento y la regulación '
    'emocional de jóvenes de 18 a 25 años. El prototipo se construyó en Unity 6 con una metodología incremental de seis '
    'fases ligadas a los objetivos específicos. La versión documentada tiene tres mundos jugables de principio a fin '
    '(Ira, Tristeza y Miedo), cada uno con sus mecánicas, enemigos, jefe, cinemáticas y registro emocional; el cuarto '
    '(Culpa) está en desarrollo, y los seis se describen en un documento de diseño del juego (GDD). Su funcionamiento se '
    'verificó con pruebas de caja negra, de caja blanca y 908 pruebas automatizadas, todas aprobadas. La investigación fue '
    'aplicada, de enfoque mixto y diseño preexperimental con pretest y postest en un solo grupo, con cuestionarios '
    'adaptados de la TMMS-24 y de la SCCS. En el pretest (n = 14 y n = 16), la atención, la claridad y la reparación '
    'emocional promediaron entre 26,2 y 27,5 puntos sobre 40, y la claridad del autoconcepto 41,8 sobre 60, con una '
    'consistencia interna entre α = 0,74 y α = 0,90. ',
    ('hl', 'El postest está pendiente.'),
    ' Se concluye que el prototipo implementa y verifica una matriz que relaciona cada emoción con una mecánica y con una '
    'dimensión medible; valorar su efecto exige completar el postest. El videojuego no sustituye la atención psicológica '
    'profesional.',
]

ABSTRACT = [
    'This research and technological development project designed, developed and piloted Fragmentum: Ecos de la '
    'Identidad, a 2D narrative platformer that turns emotions such as anger, sadness, fear and guilt into game rules, in '
    'order to support self-knowledge and emotional regulation in young people aged 18 to 25. The prototype was built in '
    'Unity 6 following an incremental methodology of six phases linked to the specific objectives. The documented version '
    'has three worlds that can be played from start to finish (Anger, Sadness and Fear), each with its own mechanics, '
    'enemies, boss, cutscenes and in-game emotional self-report; the fourth (Guilt) is in development, and all six are '
    'described in a game design document (GDD). It was verified with black-box and white-box tests and 908 automated '
    'tests, all of which passed. The research was applied, with a mixed approach and a one-group pretest-posttest '
    'pre-experimental design, using questionnaires adapted from the TMMS-24 and the SCCS. At pretest (n = 14 and n = 16), '
    'emotional attention, clarity and repair averaged between 26.2 and 27.5 points out of 40, and self-concept clarity '
    '41.8 out of 60, with internal consistency between α = 0.74 and α = 0.90. ',
    ('hl', 'The posttest is pending.'),
    ' The prototype implements and verifies a matrix that links each emotion to a mechanic and to a measurable '
    'dimension; assessing its effect requires completing the posttest. The game is not a substitute for professional '
    'psychological care.',
]


def preliminares(d):
    resumen = d.buscar('Este trabajo desarrolló y evaluó Fragmentum')
    d.poner(resumen, RESUMEN)
    d.comentar(resumen, C3 + 'resumen actualizado con el estado actual: tres mundos jugables, el cuarto en desarrollo, el '
               'GDD y 908 pruebas automatizadas (eran 520). Sigue por debajo de 250 palabras. Reescríbanlo con sus palabras.')
    abstract = d.buscar('This project developed and evaluated Fragmentum')
    d.poner(abstract, ABSTRACT)


def capitulo1(d):
    p = d.buscar('El estudio fue aplicado, de alcance exploratorio')
    d.poner(p, ['Por su naturaleza, el trabajo es un proyecto de investigación y desarrollo tecnológico: construye un '
                'artefacto, el prototipo del videojuego, y lo evalúa con un estudio empírico. El estudio fue aplicado, de '
                'alcance exploratorio, enfoque mixto y lógica deductiva. Siguió un diseño preexperimental con pretest y '
                'postest en un solo grupo, en el que se aplicaron cuestionarios basados en escalas psicométricas validadas '
                '(TMMS-24 y SCCS) para observar posibles cambios asociados al juego. La población fueron jóvenes de 18 a 25 '
                'años de la Universidad de Cundinamarca, extensión Chía, seleccionados por conveniencia.'])
    p = d.buscar('El documento sigue la estructura del formato de la Facultad.')
    d.poner(p, ['El documento sigue la estructura del formato de la Facultad. El capítulo 1 plantea el problema, los '
                'objetivos, los alcances, la justificación y las líneas de investigación; el capítulo 2 reúne el marco '
                'teórico (referencial, conceptual e ingenieril); el capítulo 3 describe la metodología, el desarrollo del '
                'proyecto fase por fase con sus resultados, el costo del proyecto y las pruebas del software, y el capítulo 4 '
                'recoge las conclusiones, las recomendaciones y las proyecciones. Los anexos contienen los instrumentos '
                'aplicados, el consentimiento informado, el cuestionario de percepción, un resumen del documento de diseño '
                'del juego (GDD), la forma de acceder al código fuente y al video del prototipo, y el formato de valoración '
                'del experto disciplinar. El GDD completo, el manual de usuario, el manual técnico y el código fuente se '
                'entregan como soportes independientes.'])

    general = d.buscar('Desarrollar y evaluar un videojuego narrativo orientado al autoconocimiento')
    d.poner(general, ['Diseñar, desarrollar y validar mediante una prueba piloto un prototipo funcional de videojuego '
                      'narrativo en 2D orientado al autoconocimiento, con el fin de explorar sus beneficios y limitaciones en '
                      'el fortalecimiento de la identidad, la autoexploración y la regulación emocional de jóvenes de 18 a '
                      '25 años.'])
    d.comentar(general, C3 + 'los evaluadores vieron una incoherencia entre un objetivo general limitado al diseño y un '
               'alcance que abarca la construcción, y pidieron que el general incorpore el desarrollo, el prototipado o la '
               'validación del artefacto. Ahora dice «diseñar, desarrollar y validar». Se ajustaron también OE3 (desarrollar '
               'el prototipo y verificarlo) y OE4 (validar con la prueba piloto). Si el director prefiere no tocar los '
               'específicos aprobados en el anteproyecto, conserven solo el cambio del general.')
    oe3 = d.buscar('3. Implementar mecánicas de juego que favorezcan')
    d.poner(oe3, ['3. Desarrollar en Unity un prototipo funcional que implemente mecánicas de juego que favorezcan la toma '
                  'de decisiones significativas, la reflexión personal y la gestión emocional, y verificar su funcionamiento '
                  'con pruebas de software.'])
    oe4 = d.buscar('4. Aplicar pruebas piloto del videojuego con usuarios seleccionados')
    d.poner(oe4, ['4. Validar el prototipo mediante una prueba piloto con usuarios seleccionados, para evaluar su '
                  'experiencia, su percepción y su nivel de impacto en el autoconocimiento y la regulación emocional.'])

    p = d.buscar('El diseño completo de los seis mundos (Ira, Tristeza, Miedo, Culpa')
    d.poner(p, ['El diseño completo de los seis mundos (Ira, Tristeza, Miedo, Culpa, Esperanza y el nivel final), con su '
                'emoción, mecánicas, enemigos, jefe, recompensa y guion (apartado 8.1.2), recogido en el documento de diseño '
                'del juego (GDD), que se resume en el Anexo D.'])
    p = d.buscar('La implementación jugable del primer mundo (Ira, Fortaleza Volcánica)')
    d.poner(p, ['La implementación jugable de tres mundos completos, la Ira (Fortaleza Volcánica), la Tristeza (Ciudad '
                'Sumergida) y el Miedo (Bosque Tenebroso), cada uno con su recorrido, mecánicas, enemigos, mini-jefe, jefe, '
                'cinemáticas, diálogos, puertas y registro emocional. El cuarto mundo (la Culpa) está en desarrollo, y los '
                'mundos 5 y 6 tienen su estructura de datos, sus jefes base y su progresión.'])
    p = d.buscar('Una prueba piloto en un entorno controlado con jóvenes de 18 a 25 años')
    d.poner(p, ['Una prueba piloto en un entorno controlado con jóvenes de 18 a 25 años, que juegan el primer mundo y '
                'responden los cuestionarios de inteligencia emocional percibida y de claridad del autoconcepto antes y '
                'después de jugar, para validar el prototipo con sus usuarios.'])
    p = d.buscar('La documentación del producto: manual de usuario y manual técnico')
    d.poner(p, ['La documentación del producto: el documento de diseño del juego (GDD), el manual de usuario y el manual '
                'técnico, entregados como soportes independientes junto con el código fuente y un video del prototipo '
                '(Anexo E).'])
    p = d.buscar('El estudio está limitado por su naturaleza exploratoria.')
    d.poner(p, ['El estudio está limitado por su naturaleza exploratoria. La autoevaluación de los participantes es '
                'subjetiva, y factores externos como el estado emocional previo o experiencias recientes pueden influir en '
                'los resultados. El diseño de un solo grupo, sin grupo de control, no permite atribuir los cambios al juego '
                'con certeza causal, y el tamaño de la muestra no permite generalizar. Además, el videojuego es un prototipo: '
                'tres de sus seis mundos son jugables, pero la sesión de la prueba piloto se limita al primero, de modo que '
                'la experiencia evaluada corresponde a una sola emoción, la ira, y dura en promedio ',
                ('hl', 'N'), ' minutos. El prototipo todavía no tiene sonido.'])


def capitulo3_metodologia(d):
    p = d.buscar('Se realizó una investigación aplicada, porque su producto principal')
    d.poner(p, ['Se realizó un proyecto de investigación y desarrollo tecnológico. La investigación fue aplicada, porque su '
                'producto principal fue un artefacto tecnológico creado por los investigadores, el prototipo del videojuego, '
                'y no una revisión bibliográfica ni el análisis de herramientas existentes, y exploratoria, porque abordó un '
                'fenómeno poco documentado en el contexto latinoamericano: el uso de videojuegos narrativos diseñados para '
                'fomentar la autoconciencia emocional de estudiantes universitarios.'])
    d.comentar(p, C3 + 'tipo de proyecto alineado con la observación de los evaluadores (proyecto de investigación y '
               'desarrollo tecnológico, no monografía) y con el perfil aprobado en el anteproyecto.')

    t5 = d.tabla_tras('Tabla 5.')
    d.celda(t5, 2, 3, ['Fichas de niveles y personajes, requisitos, casos de uso, diagramas UML y documento de diseño del '
                       'juego (GDD)'])
    d.celda(t5, 3, 1, ['OE3: desarrollar el prototipo'])
    d.celda(t5, 3, 3, ['Una versión jugable por iteración (41 iteraciones), tres mundos completos, código y escenas'])
    d.celda(t5, 4, 1, ['OE3: verificar el prototipo'])
    d.celda(t5, 5, 1, ['OE4: validar con la prueba piloto'])


def diseno(d):
    p = d.buscar('La matriz guió el resto del desarrollo')
    d.poner(p, ['La matriz guió el resto del desarrollo: las mecánicas de la fase 3 implementan sus filas y el análisis de '
                'la fase 6 contrasta las dimensiones que se esperaba modificar. La versión actual implementa las filas de '
                'los tres primeros mundos y los sistemas transversales; la prueba piloto evalúa la primera fila y esos '
                'sistemas (diálogos, registro, fragmento y «yo reconstruido»).'])
    t8 = d.tabla_tras('Tabla 8.')
    d.celda(t8, 2, 1, ['El hielo de la superficie resbala y no lleva a ningún sitio: solo se avanza rompiéndolo y '
                       'dejándose hundir. Bajo el agua todo es lento y el aire dura 10 s, pero se salta más alto. Tras '
                       'morir, una sombra repite el último intento. La Doncella solo es vulnerable mientras llora'])
    d.celda(t8, 2, 4, ['Implementado y jugable'])
    d.celda(t8, 3, 1, ['A oscuras, correr hace ruido y las trampas solo se ven de cerca; encender una antorcha despierta a '
                       'los Espectros. El Acechador se vence haciéndose oír delante de un árbol, y el Espectro del Pavor, '
                       'acercándose cuando avisa'])
    d.celda(t8, 3, 4, ['Implementado y jugable'])
    d.celda(t8, 4, 4, ['En desarrollo: recorrido, memorias, enemigos y Juez montados sin arte'])

    p = d.buscar('Cada uno de los seis mundos se diseñó a partir de su emoción')
    d.poner(p, ['Cada uno de los seis mundos se diseñó a partir de su emoción: la paleta, el recorrido, la mecánica '
                'principal, los enemigos, el jefe, la recompensa, la música y la dificultad. La ', ('ref', 'niveles'),
                ' los resume y la ', ('ref', 'mundos_detalle'), ' amplía el diseño de los mundos 4 a 6; los tres primeros, ya '
                'construidos, se describen a continuación. El diseño completo está en el documento de diseño del juego '
                '(GDD, Anexo D).'])
    t9 = d.tabla_tras('Tabla 9.')
    d.celda(t9, 1, 4, ['Jugable (126 u)'])
    d.celda(t9, 2, 2, ['Hielo que resbala, agua lenta con oxígeno, placas flotantes, hielo que solo rompe el Ataque '
                       'Cargado, ecos de intentos'])
    d.celda(t9, 2, 4, ['Jugable (139 u)'])
    d.celda(t9, 3, 2, ['Oscuridad por tramos, antorchas, sigilo por ruido, cepos y ramas que ceden; una ciénaga que '
                       'exige el Dash Acuático'])
    d.celda(t9, 3, 4, ['Jugable (172 u)'])
    d.celda(t9, 4, 2, ['Cobertura, barriles explosivos, memorias con consecuencias, Invisibilidad en la tierra de nadie, '
                       'un río con alambradas sumergidas'])
    d.celda(t9, 4, 4, ['En desarrollo (unas 200 u)'])
    nota9 = d.buscar('Nota. «Datos y jefe base»')
    d.poner(nota9, [('i', 'Nota. '), '«Datos y jefe base»: el nivel tiene sus datos, su jefe con su regla propia y su '
                     'progresión, pero no su escena ni los ataques del jefe. Entre paréntesis, la longitud del recorrido en '
                     'unidades de Unity. Elaboración propia.'])

    # Tabla 10: los mundos 2 y 3 ya están construidos y se describen en el texto: quedan los 4 a 6.
    t10_ley = d.buscar('Tabla 10.')
    for r in t10_ley.iter(qn('w:t')):
        if r.text and r.text.startswith('Diseño detallado de los mundos 2 a 6'):
            r.text = 'Diseño detallado de los mundos 4 a 6'
    t10 = d.tabla_tras('Tabla 10.')
    d.celda(t10, 3, 1, [[('b', 'Recorrido:'), ' Unas 200 u en tres secciones: el campo arrasado, la tierra de nadie bajo un '
                         'campanario, los tejados o la calle y un río bajo un puente volado'],
                        [('b', 'Mecánicas:'), ' Cobertura agachándose, barriles explosivos (Ataque Cargado), una mina, cuatro memorias '
                         'con consecuencias y la Invisibilidad para cruzar la tierra de nadie']])
    d.celda(t10, 3, 2, [[('b', 'Enemigos:'), ' Penitentes (el Eco de la Culpa), Artilleros y el Verdugo (mini-jefe)'],
                        [('b', 'Jefe:'), ' Juez Silente: cada ataque es una memoria; mirarla en su marca lo arrodilla 3 s'],
                        [('b', 'Recompensa:'), ' Fragmento de la Culpa → Defensa Reforzada']])
    d.quitar_fila(t10, 2)
    d.quitar_fila(t10, 1)

    p = d.buscar('El mundo de la Ira se diseñó con el detalle necesario para construirlo.')
    d.poner(p, ['El mundo de la Ira parte de la idea de que «la Ira empuja y el nivel enseña a contenerla»: todo invita a '
                'correr (las plataformas inestables se rompen si uno se queda, la lava mata y los enemigos atacan rápido), '
                'pero correr sin mirar castiga. Cada peligro aparece tres veces: primero sin riesgo, después sobre lava y por '
                'último junto a enemigos, de modo que el nivel enseña sin carteles. Su recorrido, horizontal y ascendente '
                'hacia la fortaleza, mide unas 126 u en cuatro tramos: el inicio, el foso y la terraza de la primera torreta; '
                'el lago de combustión, la isla y el patio del Golem de Ceniza (checkpoint en x 43); la forja, que vuelve a '
                'juntar todas las mecánicas (checkpoints en x 57,5 y 94,5), y la arena de la Centinela, de 25 × 15 u con '
                'plataformas a tres alturas (', ('ref', 'esquema_n1'), ').'])
    # La Figura 1, con el esquema actual del nivel (126 u desde el 13 de octubre).
    ley = d.buscar('Figura 1.')
    img = ley.getnext()
    nueva = d.figura(os.path.join(DIS, 'Nivel1_Diseno.png'), 16.3)
    img.addprevious(nueva)
    d.quitar(img)

    ancla = d.buscar('El diseño se comprobó con las medidas reales de Kael')
    d.poner(ancla, ['El diseño se comprobó con las medidas reales de Kael: su salto alcanza unas 2,45 unidades, el doble '
                    'salto unas 4,15 y un salto corriendo unas 4,4 de distancia. El hueco más largo del nivel mide 3 '
                    'unidades y ninguna subida pide más de 3, de modo que todo se puede cruzar con margen; una prueba '
                    'automatizada lo verifica en cada uno de los niveles.'])
    ancla = d.despues(
        ancla,
        d.parrafo('texto', [
            'El mundo de la Tristeza se diseñó con la idea de que «la Tristeza congela; el nivel enseña a sentirla», en '
            'tres tiempos. Suprimir: en la superficie helada todo resbala, los Lamentos vuelven pesado a Kael y una muralla '
            'de hielo liso cierra el paso. Sentir: la única salida es romper el hielo con el Ataque Cargado, la habilidad '
            'ganada en el mundo anterior, y dejarse hundir; abajo todo es lento y el aire dura 10 s, pero bajo el agua se '
            'salta más alto. Atravesar: en la arena, la Doncella del Lamento solo es vulnerable mientras llora, y en su '
            'segunda fase el agua que frena a Kael es la que le permite alcanzarla. El recorrido mide unas 139 u en cuatro '
            'tramos: la superficie helada y la plaza de la Plañidera; la calle hundida, la campana y la torre del reloj; '
            'el canal y el patio de las estatuas, y la arena (', ('ref', 'esquemas_234'), ').']),
        d.parrafo('texto', [
            'El mundo del Miedo parte de «el miedo paraliza; el nivel enseña a avanzar a oscuras», también en tres '
            'tiempos. Huir: en la linde, correr hace ruido y las trampas solo se ven a 1 u. Esconderse: en el bosque '
            'cerrado, a oscuras, caminar despacio es silencioso, pero los Espectros cazan en la oscuridad y la ruta fácil '
            'acaba en un callejón. Enfrentar: en el claro, el Acechador se vence haciéndose oír delante de un árbol, y el '
            'Espectro del Pavor, acercándose cuando avisa. Entre el segundo checkpoint y el claro, una ciénaga con marañas '
            'sumergidas solo se cruza con el Dash Acuático ganado en el mundo anterior, de modo que cada mundo pone en '
            'juego lo que Kael ya aprendió. El recorrido mide unas 172 u.']),
        d.parrafo('texto', [
            'El mundo de la Culpa, aprobado y en desarrollo, mide unas 200 u: un campo arrasado en el que agacharse tras '
            'la cobertura protege de los Artilleros, una tierra de nadie que solo se cruza con la Invisibilidad ganada en '
            'el Miedo, una bifurcación entre los tejados y la calle, un río bajo un puente volado y la plaza del Juez '
            'Silente, al que solo se hiere mirando la memoria que representa cada ataque. La ',
            ('ref', 'esquemas_234'), ' muestra el esquema de los tres recorridos.']),
        d.leyenda('Figura', 'esquemas_234', 'Esquemas de los recorridos de los niveles 2 (Ciudad Sumergida), 3 (Bosque '
                  'Tenebroso) y 4 (Campos de Batalla Arrasados, en desarrollo)'),
        d.figura(os.path.join(FIG, 'esquemas_234.png'), 16.0),
        d.nota(['Dibujos de diseño a escala (1 unidad = 1 celda del tileset), no capturas del juego. Elaboración propia a '
                'partir de las tablas de medidas de cada nivel (DisenoNivel2, DisenoNivel3 y DisenoNivel4).']),
    )

    # Personajes (Tabla 11).
    t11 = d.tabla_tras('Tabla 11.')
    d.celda(t11, 3, 4, ['26 de vida y dos fases: olas, inundación y congelación. Solo es vulnerable mientras llora. Da el '
                        'Dash Acuático'])
    d.celda(t11, 4, 4, ['22 de vida; invisible e intangible salvo cuando ataca: tajo, embestida, hojas y tres sombras. Da la '
                        'Invisibilidad Parcial'])
    d.celda(t11, 5, 4, ['28 de vida (36 si no se mira ninguna memoria). Cada ataque es una memoria: mirarla lo arrodilla. '
                        'Da la Defensa Reforzada'])
    d.anadir_filas(t11, [
        ['Lamento, Escultura Viviente, Medusa de Hielo y Plañidera', 'Enemigos y mini-jefa del mundo 2', 'Tristeza',
         'La tristeza que se pega y lo que no se vence: se atraviesa', 'El Lamento vuelve pesado a Kael 2 s; la Medusa no '
         'muere; la Plañidera (10 de vida) lanza esquirlas y después llora sin defenderse'],
        ['Espectro, Centinela Ciego y Acechador', 'Enemigos y mini-jefe del mundo 3', 'Miedo',
         'Lo que se acerca cuando no se mira; el silencio que protege', 'El Espectro duerme y casi no se ve; el Centinela '
         'oye pero no ve; el Acechador (10 de vida) embiste hacia el ruido y se aturde contra los árboles'],
        ['Penitente, Artillero y Verdugo', 'Enemigos y mini-jefe del mundo 4 (en desarrollo)', 'Culpa',
         'Los que cargan con lo que pasó', 'El Artillero dispara a la cabeza: solo protege agacharse tras una cobertura; '
         'el Verdugo (16 de vida) se arrodilla con una explosión cerca'],
    ])

    # Requisitos: las mecánicas de los tres mundos y el puntaje.
    t12 = d.tabla_tras('Tabla 12.')
    d.celda(t12, 6, 1, ['Mecánicas propias de cada mundo: en la Ira, lava, plataformas inestables y de combustión y '
                        'torretas; en la Tristeza, hielo, agua con oxígeno, placas flotantes, hielo rompible y ecos de '
                        'intentos; en el Miedo, oscuridad, antorchas, sigilo por ruido, cepos, ramas que ceden y marañas'])
    d.anadir_filas(t12, [['RF-15', 'Calcular un puntaje por los enemigos, los jefes y el tiempo de cada nivel, y guardar el '
                                   'mejor de cada uno', 'Media']])
    t14 = d.tabla_tras('Tabla 14.')
    d.celda(t14, 4, 3, ['RF-02, RF-03, RF-06, RF-07, RF-15'])

    # Interfaces: el HUD ya no tiene monedas sino puntaje y tiempo.
    t29 = d.tabla_tras('Tabla 29.')
    d.celda(t29, 1, 2, ['Continuar, Nueva partida, Seleccionar mundo, Yo reconstruido, Mecánicas y Salir sobre la portada '
                        'del juego; muestra los fragmentos integrados, el puntaje total y a los autores'])
    d.celda(t29, 2, 2, ['Un botón por mundo con su estado (completado, disponible, bloqueado o en construcción), su '
                        'dificultad y su mejor puntaje'])
    d.celda(t29, 3, 2, ['Vida, mundo, puntaje, tiempo, oxígeno (bajo el agua), avisos, la línea de diálogo y la barra del '
                        'jefe'])
    d.celda(t29, 8, 2, ['Registro emocional (1 a 5, comentario y código), fragmento y habilidad integrados y puntaje del '
                        'nivel con su bono por tiempo; Continuar o Menú principal'])
    t31 = d.tabla_tras('Tabla 31.')
    d.celda(t31, 1, 1, ['Progreso: niveles completados, fragmentos, habilidades, mejor puntaje por nivel, checkpoint y '
                        'cinemáticas vistas'])
