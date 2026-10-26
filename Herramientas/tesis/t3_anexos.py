"""TesisIA tras el tercer concepto: anexos D (GDD), E (código fuente y video) y F (valoración del experto)."""
import copy

from docx.oxml import OxmlElement

from docx_util import qn, texto_de, estilo_de

C3 = 'Tercer concepto (16 de octubre de 2026): '


def _salto_de_pagina(d):
    p = d.parrafo('sinsangria')
    r = OxmlElement('w:r')
    br = OxmlElement('w:br')
    br.set(qn('w:type'), 'page')
    r.append(br)
    p.append(r)
    return p


def _titulo_anexo(d, letra, texto):
    p = d.parrafo('t1', [texto])
    r = p.find(qn('w:r'))
    d._marcadores(p, f'_Anexo_{letra}', r, r)
    return p


def _leyenda_anexo(d, letra, titulo):
    """«Tabla D1. Título», con su campo SEQ TablaD (como las de los anexos A a C)."""
    p = d.parrafo('leyenda_tabla')
    r1 = d._run(f'Tabla {letra}', negrita=True)
    p.append(r1)
    for r in d._campo(f' SEQ Tabla{letra} \\* ARABIC ', '1', negrita=True):
        p.append(r)
    p.append(d._run('. ', negrita=True))
    rt = d._run(titulo, cursiva=True)
    p.append(rt)
    d._toc += 1
    d._marcadores(p, f'_Toc{d._toc}', r1, rt)
    return p


def anexo_d(d):
    filas = [
        ['1. Visión general', 'Concepto, género, plataforma, público, propósito serio, pilares de diseño y referentes'],
        ['2. Historia y personajes', 'Premisa, mundo, protagonista, los Arquitectos del Alma, el Verdadero Soldado y el guion '
                                     'de cada mundo'],
        ['3. Jugabilidad', 'Bucle de juego, controles, movimiento, combate, habilidades, progresión, puntaje, checkpoints y '
                           'muerte'],
        ['4. Los mundos', 'Para cada uno de los seis: emoción y mensaje, idea de diseño, recorrido, mecánicas, enemigos, '
                          'mini-jefe, jefe, recompensa, música y estado de producción'],
        ['5. Enemigos y jefes', 'Fichas de comportamiento: vida, daño, cómo avisa, cuándo se le hiere y qué enseña'],
        ['6. Interfaz', 'Pantallas, HUD, mapa de navegación y ayudas'],
        ['7. Arte y sonido', 'Estilo, paleta por mundo, proceso de las imágenes y plan de sonido'],
        ['8. Diseño para el autoconocimiento', 'Matriz de estrategias, registro emocional y límites del juego'],
        ['9. Aspectos técnicos', 'Motor, arquitectura, datos y pruebas'],
        ['10. Estado de la producción', 'Qué está jugable, qué está en desarrollo y qué falta'],
    ]
    return [
        _salto_de_pagina(d),
        _titulo_anexo(d, 'D', 'ANEXO D. DOCUMENTO DE DISEÑO DEL JUEGO (GDD)'),
        d.parrafo('texto', [
            'El documento de diseño del juego (GDD) reúne en un solo lugar lo que define a Fragmentum: su concepto, su '
            'historia, su jugabilidad, sus seis mundos, sus personajes, su interfaz, su arte y su sonido, y el estado de su '
            'producción. Sigue la estructura habitual de este tipo de documentos (Adams, 2014; Rogers, 2014) y añade un '
            'capítulo propio de un juego serio: cómo cada decisión de diseño sirve al autoconocimiento. Se entrega como '
            'soporte independiente (GDD_Fragmentum.docx), con el formato de este documento. La Tabla D1 resume su contenido.']),
        _leyenda_anexo(d, 'D', 'Contenido del documento de diseño del juego (GDD)'),
        d.tabla(['Capítulo', 'Contenido'], filas, [2800, 6550]),
        d.nota(['El GDD describe el juego en su versión del 21 de octubre de 2026. Elaboración propia.']),
    ]


def anexo_e(d):
    filas = [
        ['Assets/Scripts/Core', 'Máquina de estados genérica, combate, entrada, guardado, puntaje y flujo global', '34'],
        ['Assets/Scripts/Player', 'Kael: estados, movimiento, habilidades, luz y ruido', '32'],
        ['Assets/Scripts/Enemies', 'Controlador de enemigos, detección y los comportamientos de cada mundo', '27'],
        ['Assets/Scripts/Bosses', 'Clase base de los jefes, sus fases y los patrones de ataque de cada uno', '36'],
        ['Assets/Scripts/Levels', 'Flujo del nivel, checkpoints, arenas, mecánicas, escenario y memorias', '44'],
        ['Assets/Scripts/Narrative, UI y Data', 'Cinemáticas, diálogos, registro emocional, interfaz y datos del juego', '37'],
        ['Assets/Scripts/Editor', 'Herramientas que generan prefabs, escenas, animaciones y datos, y verificadores del arte', '68'],
        ['Assets/Tests', 'Pruebas automatizadas EditMode y PlayMode', '96'],
        ['Docs y Herramientas', 'Diseño de cada nivel, guion, prompts del arte, diagramas y scripts de verificación', '—'],
    ]
    guion = [
        '1. El menú principal y el selector de mundos (30 s).',
        '2. El mundo de la Ira: el primer Eco, la plataforma de combustión y la Centinela Ardiente con su espada clavada (2 min).',
        '3. El mundo de la Tristeza: el hielo, el tapón que rompe el Ataque Cargado, la calle hundida y la Doncella llorando (2 min).',
        '4. El mundo del Miedo: la oscuridad, una antorcha, un cepo, la ciénaga y el Espectro del Pavor (2 min).',
        '5. El registro emocional y la pantalla «Mundo superado» con el fragmento y el puntaje (30 s).',
        '6. La ejecución de las pruebas automatizadas y el mundo de la Culpa en desarrollo (1 min).',
    ]
    bloques = [
        _salto_de_pagina(d),
        _titulo_anexo(d, 'E', 'ANEXO E. CÓDIGO FUENTE Y VIDEO DEL PROTOTIPO'),
        d.parrafo('texto', [
            'El código fuente del prototipo se entrega como soporte digital: el proyecto de Unity completo (Tesis2.0), con sus '
            'escenas, sus datos, sus pruebas automatizadas y las herramientas del editor que generan buena parte del '
            'contenido, y el ejecutable para Windows. Se abre con Unity 6000.6.3f1; el manual técnico explica cómo compilarlo '
            'y cómo ejecutar las pruebas. La Tabla E1 resume su estructura. Acceso al código: ',
            ('hl', 'enlace al repositorio o a la carpeta compartida'), '.']),
        _leyenda_anexo(d, 'E', 'Estructura del código fuente entregado'),
        d.tabla(['Carpeta', 'Contenido', 'Archivos .cs'], filas, [3000, 5450, 900]),
        d.nota(['El código del juego, sin el editor ni las pruebas, suma 22 526 líneas efectivas en 210 archivos (apartado '
                '8.2). Versión del 21 de octubre de 2026. Elaboración propia.']),
        d.parrafo('texto', ['Video del prototipo: ', ('hl', 'enlace al video'), '. Se propone un video de unos 8 minutos, '
                            'grabado con el ejecutable, que siga este guion:']),
    ]
    bloques += [d.parrafo('vineta', [g]) for g in guion]
    return bloques


def anexo_f(d):
    criterios = [
        ['1', 'La matriz de estrategias de jugabilidad (Tabla 8) relaciona cada mundo con una estrategia de regulación '
              'emocional reconocida'],
        ['2', 'El mundo de la Ira representa la impulsividad y su contención (esperar el momento) de forma adecuada'],
        ['3', 'El mundo de la Tristeza representa la tristeza como algo que se siente y se atraviesa, no que se suprime'],
        ['4', 'El mundo del Miedo representa la exposición gradual y la decisión con incertidumbre'],
        ['5', 'El lenguaje de los diálogos y de las cinemáticas es adecuado para jóvenes de 18 a 25 años y no estigmatiza '
              'ninguna emoción'],
        ['6', 'La pregunta y la escala del registro emocional son claras y no inducen la respuesta'],
        ['7', 'Los riesgos para jóvenes en crisis están previstos (protocolo de la sesión y remisión a Bienestar Universitario)'],
        ['8', 'Los cuestionarios adaptados de la TMMS-24 y de la SCCS son pertinentes, incluidos los ítems con baja correlación'],
        ['9', 'El prototipo es útil como apoyo, no como sustituto, en procesos de orientación psicológica'],
    ]
    filas = [[n, c, '', ''] for n, c in criterios]
    filas += [['—', 'Concepto general: favorable / favorable con ajustes / no favorable', '', ''],
              ['—', 'Nombre, profesión y tarjeta profesional del evaluador', '', ''],
              ['—', 'Firma y fecha', '', '']]
    return [
        _salto_de_pagina(d),
        _titulo_anexo(d, 'F', 'ANEXO F. FORMATO DE VALORACIÓN DEL EXPERTO DISCIPLINAR'),
        d.parrafo('texto', [
            'Este formato recoge la valoración de un profesional de la psicología sobre el prototipo, como pidieron los '
            'evaluadores del trabajo. El evaluador juega los tres mundos disponibles o revisa su video (Anexo E), lee la matriz '
            'de estrategias y el guion, y califica cada criterio de 1 (totalmente en desacuerdo) a 5 (totalmente de acuerdo), '
            'con las observaciones que considere. Su valoración se resume en la conclusión 6 (capítulo 10).']),
        _leyenda_anexo(d, 'F', 'Criterios de la valoración del experto disciplinar'),
        d.tabla(['N.º', 'Criterio', 'Valoración (1 a 5)', 'Observaciones'], filas, [600, 5050, 1300, 2400]),
        d.nota(['Elaboración propia. Propuesta: ajústenla con la asesora en psicología.']),
    ]


def anexos(d):
    ultimo = list(d.body.iterchildren())[-2]   # el párrafo vacío tras la Tabla C1 (antes del sectPr)
    nuevos = anexo_d(d) + anexo_e(d) + anexo_f(d)
    d.despues(ultimo, *nuevos)
    titulo_d = d.buscar('ANEXO D. DOCUMENTO DE DISEÑO DEL JUEGO (GDD)', estilo='Ttulo1')
    d.comentar(titulo_d, C3 + 'los evaluadores pidieron incluir el GDD y el código fuente o un video del prototipo. El GDD '
               'completo va aparte (GDD_Fragmentum.docx) porque el formato limita los anexos al 10 % del documento; aquí, '
               'su resumen. El Anexo E dice cómo acceder al código y propone un guion para el video; el Anexo F es el formato '
               'para la valoración de la asesora en psicología, que pedían las conclusiones.')

    # La lista de anexos: una entrada por anexo, con el número de página como campo.
    c = d.buscar('Anexo C. Cuestionario de percepción de la experiencia (propuesta)', estilo='Listaanexos')
    ancla = c
    for letra, nombre in (('D', 'Documento de diseño del juego (GDD)'), ('E', 'Código fuente y video del prototipo'),
                          ('F', 'Formato de valoración del experto disciplinar')):
        e = copy.deepcopy(c)
        ts = list(e.iter(qn('w:t')))
        ts[0].text = f'Anexo {letra}. {nombre}'
        for it in e.iter(qn('w:instrText')):
            it.text = f' PAGEREF _Anexo_{letra} \\h '
        ts[-1].text = '¿?'
        ancla.addnext(e)
        ancla = e

    # La lista de tablas: un índice por anexo, como los de las tablas A, B y C.
    tabla_c = None
    for e in d.body.iterchildren():
        if e.tag == qn('w:p') and any((it.text or '').strip().startswith('TOC \\h \\z \\c "TablaC"') for it in e.iter(qn('w:instrText'))):
            tabla_c = e
    assert tabla_c is not None
    ancla = tabla_c
    for letra, titulo in (('D', 'Contenido del documento de diseño del juego (GDD)'),
                          ('E', 'Estructura del código fuente entregado'),
                          ('F', 'Criterios de la valoración del experto disciplinar')):
        p = OxmlElement('w:p')
        ppr = copy.deepcopy(tabla_c.find(qn('w:pPr')))
        p.append(ppr)
        for r in d._campo(f' TOC \\h \\z \\c "Tabla{letra}" ', f'Tabla {letra}1. {titulo}'):
            p.append(r)
        ancla.addnext(p)
        ancla = p
