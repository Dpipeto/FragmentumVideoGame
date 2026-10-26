"""
El documento de diseño del juego (GDD) de Fragmentum, con los estilos de TesisIA (formato FPG-V 9.0 y APA 7).

    python gdd.py <plantilla TesisIA.docx> <salida.docx>
"""
import copy
import os
import sys

from docx.oxml import OxmlElement
from lxml import etree

from docx_util import Documento, qn, texto_de

AQUI = os.path.dirname(os.path.abspath(__file__))
FIG = os.path.join(AQUI, 'fig')
MEDIA = os.path.join(AQUI, 'x', 'word', 'media')     # las imágenes de TesisIA (el original del 13-10)
DIS = r'C:\Users\dpipe\OneDrive\Desktop\Importante\Tesis2.0\Docs\Diseno'


class GDD:
    def __init__(self, plantilla):
        self.d = Documento(plantilla)
        self.refs_originales = {texto_de(p)[:40]: copy.deepcopy(p) for p in self.d.body.iter(qn('w:p'))
                                if p.find(qn('w:pPr')) is not None and
                                (p.find(qn('w:pPr')).find(qn('w:pStyle')) is not None) and
                                p.find(qn('w:pPr')).find(qn('w:pStyle')).get(qn('w:val')) == 'Referenciatesis'}
        sect = self.d.body.find(qn('w:sectPr'))
        for e in list(self.d.body):
            if e is not sect:
                self.d.body.remove(e)
        self._vaciar_comentarios()
        self.ancla = OxmlElement('w:p')
        sect.addprevious(self.ancla)

    def _vaciar_comentarios(self):
        for rel in list(self.d.doc.part.rels.values()):
            if 'comments' in rel.reltype.lower():
                parte = rel.target_part
                if hasattr(parte, 'element'):
                    for h in list(parte.element):
                        parte.element.remove(h)
                elif hasattr(parte, '_blob'):
                    raiz = etree.fromstring(parte._blob)
                    for h in list(raiz):
                        raiz.remove(h)
                    parte._blob = etree.tostring(raiz, xml_declaration=True, encoding='UTF-8', standalone=True)

    # --- escribir ---

    def _poner(self, *elementos):
        self.ancla = self.d.despues(self.ancla, *elementos)

    def salto(self):
        p = self.d.parrafo('sinsangria')
        r = OxmlElement('w:r')
        br = OxmlElement('w:br')
        br.set(qn('w:type'), 'page')
        r.append(br)
        p.append(r)
        self._poner(p)

    def h1(self, t, salto=True):
        if salto:
            self.salto()
        self._poner(self.d.parrafo('t1', [t]))

    def h2(self, t):
        self._poner(self.d.parrafo('t2', [t]))

    def h3(self, t):
        self._poner(self.d.parrafo('t3', [t]))

    def h4(self, t):
        self._poner(self.d.parrafo('t4', [t]))

    def p(self, *piezas):
        self._poner(self.d.parrafo('texto', list(piezas)))

    def v(self, *piezas):
        self._poner(self.d.parrafo('vineta', list(piezas)))

    def portada(self, *piezas):
        self._poner(self.d.parrafo('portada', list(piezas)))

    def tabla(self, clave, titulo, cab, filas, anchos, nota):
        self._poner(self.d.leyenda('Tabla', clave, titulo), self.d.tabla(cab, filas, anchos), self.d.nota(nota))

    def figura(self, clave, titulo, ruta, ancho, nota):
        self._poner(self.d.leyenda('Figura', clave, titulo), self.d.figura(ruta, ancho), self.d.nota(nota))

    def indice(self, instruccion, texto, estilo='TDC1'):
        p = self.d.parrafo(estilo)
        for r in self.d._campo(instruccion, texto):
            p.append(r)
        self._poner(p)

    def referencia(self, inicio):
        for k, p in self.refs_originales.items():
            if k.startswith(inicio[:min(len(inicio), 40)]):
                self._poner(copy.deepcopy(p))
                return
        raise KeyError(inicio)

    def guardar(self, ruta):
        self.d.renumerar()
        # Las imágenes de la plantilla que ya no se usan, fuera del paquete.
        usados = {e.get(qn('r:embed')) for e in self.d.body.iter('{http://schemas.openxmlformats.org/drawingml/2006/main}blip')}
        parte = self.d.doc.part
        for rid, rel in list(parte.rels.items()):
            if rel.reltype.endswith('/image') and rid not in usados:
                parte.drop_rel(rid)
        cp = self.d.doc.core_properties
        cp.title = 'Documento de diseño del juego (GDD): Fragmentum: Ecos de la Identidad'
        cp.subject = 'Soporte del proyecto de investigación y desarrollo tecnológico «Videojuego como herramienta de autoconocimiento en crisis emocionales»'
        cp.keywords = 'Fragmentum; GDD; documento de diseño del juego; videojuego serio; Unity; autoconocimiento'
        cp.category = 'Documento de diseño del juego'
        cp.comments = 'Versión 1.0 del 21 de octubre de 2026.'
        self.d.guardar(ruta)


def construir(g):
    # --- Portada ---
    for _ in range(3):
        g.portada()
    g.portada('DOCUMENTO DE DISEÑO DEL JUEGO (GDD)')
    g.portada()
    g.portada('FRAGMENTUM: ECOS DE LA IDENTIDAD')
    for _ in range(4):
        g.portada()
    g.portada('Soporte del proyecto de investigación y desarrollo tecnológico «Videojuego como herramienta de '
              'autoconocimiento en crisis emocionales»')
    for _ in range(5):
        g.portada()
    g.portada('DAVID FELIPE GARZÓN BARBOSA')
    g.portada('JUAN DIEGO ROMERO JIMÉNEZ')
    for _ in range(6):
        g.portada()
    g.portada('UNIVERSIDAD DE CUNDINAMARCA EXTENSIÓN CHÍA')
    g.portada('PROGRAMA DE INGENIERÍA DE SISTEMAS')
    g.portada('FACULTAD DE INGENIERÍA')
    g.portada('2026')

    g.salto()
    g._poner(g.d.parrafo('preliminar', ['CONTROL DE VERSIONES']))
    g.tabla('versiones', 'Versiones del documento', ['Versión', 'Fecha', 'Cambios'], [
        ['1.0', '21/10/2026', 'Primera versión completa, a partir del diseño de cada nivel (Docs/Diseno), del guion y del '
                              'juego construido: tres mundos jugables y el cuarto en desarrollo'],
    ], [1300, 1700, 6350], ['Elaboración propia.'])
    g.salto()
    g._poner(g.d.parrafo('preliminar', ['TABLA DE CONTENIDO']))
    g.indice(' TOC \\o "1-3" \\h \\z \\u ', 'Al abrir el documento en Word, acepte actualizar los campos para ver el índice.')
    g.salto()
    g._poner(g.d.parrafo('preliminar', ['LISTA DE FIGURAS']))
    g.indice(' TOC \\h \\z \\c "Figura" ', 'Lista de figuras (se actualiza al abrir en Word).', 'Tabladeilustraciones')
    g._poner(g.d.parrafo('preliminar', ['LISTA DE TABLAS']))
    g.indice(' TOC \\h \\z \\c "Tabla" ', 'Lista de tablas (se actualiza al abrir en Word).', 'Tabladeilustraciones')

    # --- 1 ---
    g.h1('1. VISIÓN GENERAL')
    g.p('Este documento de diseño del juego (GDD, del inglés game design document) reúne en un solo lugar lo que define '
        'a Fragmentum: Ecos de la Identidad: su concepto, su historia, su jugabilidad, sus seis mundos, sus personajes, '
        'su interfaz, su arte y su sonido, y el estado de su producción. Sigue la estructura habitual de este tipo de '
        'documentos (Adams, 2014; Rogers, 2014) y añade un capítulo propio de un juego serio: cómo cada decisión de diseño '
        'sirve al autoconocimiento (capítulo 8). Describe el juego en su versión del 21 de octubre de 2026 y es un soporte '
        'del trabajo de grado «Videojuego como herramienta de autoconocimiento en crisis emocionales».')
    g.h2('1.1 Concepto')
    g.p('Fragmentum es un videojuego narrativo de plataformas y acción en 2D en el que cada mundo convierte una emoción en '
        'reglas de juego. El protagonista, Kael, es un clon creado para eliminar el dolor de un soldado destrozado por la '
        'guerra; para completarse debe atravesar las emociones que el soldado suprimió (la ira, la tristeza, el miedo y la '
        'culpa) e integrarlas, no eliminarlas: cada emoción enfrentada le devuelve un fragmento de identidad y una '
        'habilidad. La ', ('ref', 'ficha'), ' resume el concepto.')
    g.tabla('ficha', 'Ficha del juego', ['Elemento', 'Descripción'], [
        ['Título', 'Fragmentum: Ecos de la Identidad'],
        ['Género', 'Plataformas y acción con narrativa simbólica (metroidvania ligero): combate, exploración lateral y '
                   'habilidades que se ganan por mundo'],
        ['Propósito', 'Juego serio orientado al autoconocimiento y a la regulación emocional; no sustituye la atención '
                      'psicológica profesional'],
        ['Público', 'Jóvenes de 18 a 25 años'],
        ['Plataforma', 'PC con Windows 10 u 11 de 64 bits; teclado y ratón o mando'],
        ['Motor', 'Unity 6 (6000.6.3f1), C#, Universal Render Pipeline 2D'],
        ['Estructura', 'Seis mundos en orden, cada uno con su emoción, su recorrido, su mini-jefe y su jefe; el quinto, sin '
                       'jefe, y el sexto, el enfrentamiento final'],
        ['Duración', 'Entre 10 y 16 minutos por mundo jugado con soltura (el tiempo de referencia del puntaje)'],
        ['Estilo', 'Pixel art 2D con una paleta y una luz por mundo'],
        ['Regla central', 'La emoción no se elimina: se integra. Cada fragmento devuelve una habilidad que el mundo siguiente '
                          'exige'],
    ], [2200, 7150], ['Elaboración propia.'])
    g.h2('1.2 Pilares de diseño')
    g.p('Cinco principios guían todas las decisiones del juego y sirven para resolver las dudas de diseño:')
    g.v('1. La emoción es la regla. Cada mundo traduce su emoción en mecánicas: la ira empuja a correr, la tristeza hace '
        'todo lento, el miedo quita la luz y la culpa castiga lo que se evita.')
    g.v('2. La emoción no se elimina, se integra. Vencer a un jefe no destruye la emoción: la convierte en una habilidad.')
    g.v('3. Enseñar sin carteles. Cada peligro aparece tres veces: primero sin riesgo, después con riesgo y por último junto '
        'a enemigos. Solo hay una pista explícita por mundo, y la dice Kael.')
    g.v('4. Lo aprendido se usa. Cada mundo exige la habilidad ganada en el anterior: el Ataque Cargado rompe el hielo de la '
        'Tristeza, el Dash Acuático cruza la ciénaga del Miedo y la Invisibilidad cruza la tierra de nadie de la Culpa.')
    g.v('5. Respeto al jugador. Sin violencia gráfica, sin juicios sobre lo que siente y con un registro emocional '
        'voluntario.')
    g.h2('1.3 Referentes')
    g.p('Fragmentum toma de Celeste (Matt Makes Games, 2018) la idea de enfrentar y después aceptar a un doble, y de Gris '
        '(Nomada Studio, 2018) la de que cada etapa emocional cambie el mundo y otorgue una habilidad. De los juegos con '
        'evidencia clínica, como SPARX (Merry et al., 2012) y MindLight (Schoneveld et al., 2016), toma que la mecánica '
        'misma puede entrenar una estrategia de regulación. Su diferencia es que expresa cada emoción en una regla de juego y, '
        'a la vez, mide con un registro dentro del juego.')

    # --- 2 ---
    g.h1('2. HISTORIA Y PERSONAJES')
    g.h2('2.1 Premisa')
    g.p('Un soldado queda psicológicamente destrozado tras una guerra sangrienta. De ese colapso nace Kael, un clon creado '
        'para eliminar el dolor, la culpa y la vulnerabilidad del original. Kael despierta sin saber quién es. Cada región que '
        'recorre representa una emoción que el soldado intentó suprimir, custodiada por un Arquitecto del Alma: no son '
        'enemigos externos, sino manifestaciones de dolores enterrados que impiden al clon completarse. El conflicto culmina '
        'cuando Kael entiende que el jefe final es el propio soldado, que lo ve como un error que debe eliminarse. La '
        'resolución no es destructiva: Kael acepta su origen para construir su propia identidad, y la pregunta final del '
        'juego, «¿Eres un error… o una nueva identidad?», queda para el jugador.')
    g.h2('2.2 Reglas del guion')
    g.v('Kael habla en frases cortas, en presente y sin certezas: descubre cada emoción como algo ajeno que, poco a poco, '
        'reconoce suyo, y nunca explica la moraleja.')
    g.v('Los Arquitectos del Alma son guardianes sin rostro, no villanos: hablan del soldado en tercera persona y a Kael de tú.')
    g.v('Cada apertura y cada cierre nombran la emoción del mundo, y el cierre nombra la habilidad como un gesto emocional: '
        'contener y soltar el golpe, avanzar bajo el agua, ocultarse sin perderse, resistir sin negar.')
    g.v('Las líneas que aparecen durante el juego duran 4,5 s y no pasan de unas 120 letras.')
    g.h2('2.3 Personajes')
    g.tabla('personajes', 'Personajes principales', ['Personaje', 'Rol', 'Qué simboliza', 'En el juego'], [
        ['Kael', 'Protagonista, clon del soldado', 'La identidad fragmentada que se reconstruye', '5 de vida; gana una '
         'habilidad por cada fragmento'],
        ['Centinela Ardiente', 'Arquitecta de la Ira', 'La rabia que destruye su entorno', '30 de vida, tres fases; solo se '
         'la hiere cuando su espada queda clavada'],
        ['Doncella del Lamento', 'Arquitecta de la Tristeza', 'La pérdida que hay que sentir', '26 de vida, dos fases; solo '
         'se la hiere mientras llora'],
        ['Espectro del Pavor', 'Arquitecto del Miedo', 'La ansiedad ante lo que no se ve', '22 de vida, dos fases; invisible '
         'salvo cuando ataca'],
        ['Juez Silente', 'Arquitecto de la Culpa', 'La responsabilidad que no se enfrenta', '28 de vida (36 si no se mira '
         'ninguna memoria); se arrodilla al mirar la memoria de su ataque'],
        ['El Verdadero Soldado', 'Jefe final', 'El origen que hay que aceptar', 'Combate espejo; se vence por integración, '
         'no por violencia'],
    ], [2000, 2100, 2350, 2900], ['Elaboración propia.'])
    g.figura('conceptos', 'Arte conceptual de la Centinela Ardiente, la Doncella del Lamento y el Espectro del Pavor',
             os.path.join(MEDIA, 'image2.png'), 16.0, ['Arte conceptual del proyecto. El arte definitivo de cada jefe está '
                                                       'en el capítulo 4.'])
    g.h2('2.4 El guion de cada mundo')
    g.p('Los diálogos internos de Kael aparecen en cuatro momentos de cada mundo: al empezar, en el eco de memoria del '
        'primer checkpoint, antes del jefe (en boca del Arquitecto) y al cerrar el mundo (', ('ref', 'guion'), ').')
    g.tabla('guion', 'Líneas del guion de cada mundo', ['Mundo', 'Apertura (Kael)', 'Antes del jefe (Arquitecto)', 'Cierre (Kael)'], [
        ['Ira', 'Este calor no es mío… y aun así me empuja a correr. Si esto es la Ira, alguien la dejó encendida.',
         'Él me dejó aquí para que nadie tocara su rabia. Tú tampoco la tocarás.',
         'La Ira no era el fuego. Era lo que él nunca dijo. La llevo conmigo, pero ya no me lleva ella.'],
        ['Tristeza', 'Una ciudad entera bajo el agua y ni una lágrima en la superficie. Así de callada es la Tristeza.',
         'Él cerró los ojos para no llorar, y la ciudad se hundió. ¿Vienes a cerrarlos tú también?',
         'Me dejé hundir y no me ahogué. La Tristeza pesa, pero también sostiene.'],
        ['Miedo', 'No veo mis propios pasos. Algo respira entre los árboles… o soy yo. Esto es el Miedo.',
         'Él aprendió a desaparecer para seguir vivo. Yo soy todo lo que dejó escondido.',
         'El Miedo no se fue: dejó de decidir por mí. Puedo andar en la sombra sin perderme en ella.'],
        ['Culpa', 'Aquí nada se movió desde el último disparo. Es la Culpa de otro… y la siento mía.',
         'No dicto sentencia. Solo te muestro lo que él se negó a mirar.',
         'Mirarlo no me destruyó. La Culpa es un peso, no un nombre. Puedo cargarlo y seguir de pie.'],
        ['Esperanza', 'Por primera vez nada quiere herirme. Me cuesta creerlo. Quizá así empieza la Esperanza.', '— (sin jefe)',
         'Cada fragmento encontró su sitio. La Esperanza no borra lo que pasó: lo deja crecer en otro lugar.'],
        ['Final', 'Todo lo que vine a buscar estaba aquí, esperando. La Integración no es ganarle: es dejar de huir.',
         'Eres lo que sobró cuando me arranqué el dolor. Eres un error.',
         'No vine a reemplazarte ni a destruirte. La Integración era esto: caminar juntos hacia la misma luz.'],
    ], [1200, 2750, 2650, 2750], ['Textos del juego (GuionMundos.cs). Elaboración propia.'])

    # --- 3 ---
    g.h1('3. JUGABILIDAD')
    g.h2('3.1 Bucle de juego')
    g.p('Cada mundo sigue el mismo bucle: recorrer el nivel aprendiendo sus reglas, superar a su mini-jefe (o evitarlo), '
        'enfrentar al Arquitecto en su arena, recoger su fragmento, responder el registro emocional y pasar al mundo '
        'siguiente con una habilidad nueva. Morir no castiga más de lo necesario: Kael vuelve al último checkpoint, que '
        'además lo cura. El puntaje premia hacer el recorrido completo y hacerlo bien, sin obligar a nada.')
    g.h2('3.2 Controles')
    g.tabla('controles', 'Controles del juego', ['Acción', 'Teclado y ratón', 'Mando'], [
        ['Moverse', 'A / D o ← / →', 'Palanca izquierda'],
        ['Saltar (mantener: más alto; dos veces: doble salto)', 'Espacio', 'A / Cruz'],
        ['Dash', 'Shift izquierdo', 'Pulsar la palanca izquierda'],
        ['Atacar (mantener y soltar: Ataque Cargado)', 'Clic izquierdo o Enter', 'X / Cuadrado'],
        ['Agacharse', 'C o Ctrl izquierdo', 'B / Círculo'],
        ['Caminar (sigilo)', 'Alt izquierdo con una dirección', 'Inclinar poco la palanca'],
        ['Invisibilidad Parcial', 'Q', 'Botón superior derecho'],
        ['Interactuar (antorchas, memorias)', 'E', 'Y / Triángulo'],
        ['Pausa', 'Esc', 'Start'],
    ], [3800, 2900, 2650], ['Tomado del mapa de acciones del proyecto (InputSystem_Actions).'])
    g.h2('3.3 Movimiento y combate de Kael')
    g.p('Kael corre a 6 u/s (1 u es una celda del tileset), camina a 2,1 u/s y salta unas 2,45 u; con el doble salto llega '
        'a unas 4,15 u y corriendo salta unas 4,4 u de distancia. El dash recorre 2,88 u en 0,18 s, también bajo el agua. '
        'Puede deslizarse por las paredes y saltar desde ellas, y agacharse, lo que en el mundo de la Culpa lo protege tras '
        'una cobertura. Tiene 5 de vida y, tras recibir un golpe, un instante de invulnerabilidad. Su espada golpea en un arco '
        'por delante; mantener el ataque y soltarlo da el Ataque Cargado, que duplica el daño. Todos los niveles se diseñan con '
        'estas medidas, y una prueba automatizada comprueba que cada salto del recorrido se puede hacer con margen.')
    g.h2('3.4 Habilidades')
    g.tabla('habilidades', 'Habilidades de Kael', ['Habilidad', 'Se gana en', 'Qué hace', 'Dónde se exige'], [
        ['Ataque Cargado', 'Ira', 'Mantener y soltar el ataque: daño doble. Contener el golpe y soltarlo cuando uno decide',
         'Rompe el tapón de hielo de la Tristeza y enciende los barriles de la Culpa'],
        ['Dash Acuático', 'Tristeza', 'El dash funciona bajo el agua', 'Cruza las marañas de la ciénaga del Miedo y las '
         'alambradas del río de la Culpa'],
        ['Invisibilidad Parcial', 'Miedo', '3 s en los que los enemigos que ven pierden a Kael; 8 s de espera', 'Cruza la '
         'tierra de nadie de la Culpa; con ella no se pueden recordar las memorias'],
        ['Defensa Reforzada', 'Culpa', 'Un escudo que absorbe un golpe y se recarga a los 15 s; el golpe letal lo atraviesa',
         'El Jardín Etéreo y el combate final'],
    ], [1900, 1300, 3250, 2900], ['Datos de PlayerAbilities y PlayerHealth. Elaboración propia.'])
    g.h2('3.5 Progresión, checkpoints y muerte')
    g.p('Los mundos se desbloquean en orden; superado uno, se puede volver a jugar desde el selector. Cada nivel tiene dos o '
        'tres checkpoints que guardan la partida y curan a Kael. Al morir aparece la pantalla «Has caído», con la opción de '
        'reintentar desde el último checkpoint. La partida se guarda sola en JSON, con escritura atómica y una copia de '
        'seguridad, en los checkpoints, al recibir la recompensa y al terminar el nivel.')
    g.h2('3.6 Puntaje')
    g.p('El puntaje hace que correr y pelear compitan: quien se salta un mini-jefe gana tiempo, pero pierde sus puntos. Al '
        'cruzar la salida se suman los enemigos derrotados y un bono de 5 puntos por cada segundo que sobre del tiempo de '
        'referencia del nivel; el juego guarda el mejor puntaje de cada mundo (', ('ref', 'puntaje'), ').')
    g.tabla('puntaje', 'Valor de cada enemigo y tiempo de referencia de cada nivel', ['Elemento', 'Puntos o tiempo'], [
        ['Enemigo común (Eco de la Ira, Lamento, Penitente)', '100'],
        ['Espectro, Centinela Ciego y Artillero', '150'],
        ['Escultura Viviente', '250'],
        ['Mini-jefes: Golem de Ceniza, Plañidera, Acechador y Verdugo', '500, 600, 700 y 800'],
        ['Jefes: Centinela, Doncella, Espectro del Pavor, Juez Silente y Verdadero Soldado', '1500, 2000, 2500, 3000 y 5000'],
        ['Bono por tiempo', '5 puntos por segundo que sobre'],
        ['Tiempo de referencia de los niveles 1 a 6', '10, 13, 16, 16, 12 y 12 min'],
    ], [6000, 3350], ['Datos de TablaPuntaje. Elaboración propia.'])

    # --- 4 ---
    g.h1('4. LOS MUNDOS')
    g.p('Cada mundo se diseñó a partir de su emoción y de una frase que la resume: el recorrido enseña esa frase en tres '
        'tiempos, y su jefe la convierte en combate (', ('ref', 'mundos'), '). Los recorridos se describen como tablas de '
        'medidas en el código, y una herramienta del editor monta cada escena a partir de ellas.')
    g.tabla('mundos', 'Resumen de los seis mundos', ['Mundo', 'Emoción y mensaje', 'Mecánica principal', 'Jefe y recompensa', 'Estado'], [
        ['1. Fortaleza Volcánica', 'Ira: la ira no controlada destruye el entorno y al que la porta', 'Lava, plataformas '
         'inestables y de combustión, torretas', 'Centinela Ardiente → Ataque Cargado', 'Jugable (126 u)'],
        ['2. Ciudad Sumergida', 'Tristeza: requiere ser sentida, no suprimida', 'Hielo, agua con oxígeno, placas, hielo '
         'rompible, ecos de intentos', 'Doncella del Lamento → Dash Acuático', 'Jugable (139 u)'],
        ['3. Bosque Tenebroso', 'Miedo: paraliza cuando se enfrenta en la oscuridad', 'Oscuridad, antorchas, sigilo por '
         'ruido, cepos, ramas que ceden, ciénaga', 'Espectro del Pavor → Invisibilidad Parcial', 'Jugable (172 u)'],
        ['4. Campos de Batalla Arrasados', 'Culpa: pesa, pero no define la identidad', 'Cobertura, barriles, memorias con '
         'consecuencias, río con alambradas', 'Juez Silente → Defensa Reforzada', 'En desarrollo (unas 200 u)'],
        ['5. Jardín Etéreo', 'Esperanza: la integración da paso a la reconstrucción', 'Plataformeo que exige las cuatro '
         'habilidades', 'Sin jefe', 'Diseñado'],
        ['6. El Espejo del Origen', 'La identidad no se encuentra, se construye', 'Combate espejo y una fase final sin '
         'violencia', 'El Verdadero Soldado', 'Diseñado'],
    ], [1750, 2250, 2350, 1750, 1250], ['Entre paréntesis, la longitud del recorrido en unidades de Unity. Elaboración propia.'])

    g.h2('4.1 Mundo 1: la Ira, Fortaleza Volcánica')
    g.p('Idea: «la Ira empuja y el nivel enseña a contenerla». Todo invita a correr (las plataformas inestables se rompen si '
        'uno se queda, la lava mata y los Ecos atacan rápido), pero correr sin mirar castiga: la plataforma de combustión quema '
        'justo cuando se llega con prisa, la bola de la torreta alcanza a quien salta a destiempo y el Golem de Ceniza no se '
        'interrumpe a golpes. Ganar es esperar el momento y soltar el golpe entonces, que es lo mismo que hace la recompensa '
        'del mundo, el Ataque Cargado. El recorrido, horizontal y ascendente hacia la fortaleza, tiene cuatro tramos: el '
        'inicio, el foso y la terraza de la primera torreta; el lago de combustión, la isla y el patio del Golem; la forja, que '
        'vuelve a juntar todas las mecánicas, y la arena de la Centinela, de 25 × 15 u (', ('ref', 'esquema1'), ' y ',
        ('ref', 'recorrido1'), ').')
    g.figura('esquema1', 'Esquema del recorrido del Nivel 1', os.path.join(DIS, 'Nivel1_Diseno.png'), 16.3,
             ['Dibujo de diseño a escala (1 unidad = 1 celda del tileset), no una captura del juego. Elaboración propia.'])
    g.figura('recorrido1', 'Recorrido del Nivel 1: (a) primer Eco de la Ira, (b) la terraza con la torreta, (c) el patio '
             'del Golem de Ceniza y (d) la Centinela derrotada', os.path.join(MEDIA, 'image26.jpg'), 16.0,
             ['Capturas del juego (versión del 8 de octubre de 2026).'])
    g.p('La Centinela Ardiente pelea en tres fases: espadazos que la dejan con la espada clavada (la única ventana para '
        'herirla, con daño doble), ondas de fuego por el suelo, erupciones de lava bajo Kael y un salto aplastante; en las fases '
        '2 y 3 ruge y se vuelve más rápida. El mundo tiene dos enemigos: el Eco de la Ira, que anticipa su golpe y puede ser '
        'interrumpido, y el Golem de Ceniza, mini-jefe con hiperarmadura, inmune a la lava (', ('ref', 'enemigos_ira'), ').')
    g.figura('enemigos_ira', 'Enemigos del mundo de la Ira', os.path.join(MEDIA, 'image22.png'), 12.0,
             ['Arte del proyecto; montaje propio.'])

    g.h2('4.2 Mundo 2: la Tristeza, Ciudad Sumergida')
    g.p('Idea: «la Tristeza congela; el nivel enseña a sentirla», en tres tiempos. Suprimir: en la superficie helada todo '
        'resbala, los Lamentos vuelven pesado a Kael y la superficie termina en una muralla de hielo liso que no se puede '
        'subir. Sentir: la única salida es romper el tapón de hielo con el Ataque Cargado y dejarse hundir; abajo todo es '
        'lento y oscuro, el aire dura 10 s y se recupera en las bolsas de aire, pero bajo el agua se salta más alto, y la torre '
        'que lleva a la salida solo se sube así. Atravesar: la Doncella del Lamento inunda su arena y solo se la hiere mientras '
        'llora. Los ecos de intentos refuerzan el tema: al reaparecer, una sombra de Kael repite durante 8 s el intento que '
        'salió mal, como la rumiación (', ('ref', 'esquema2'), ').')
    g.figura('esquema2', 'Esquema del recorrido del Nivel 2', os.path.join(DIS, 'Nivel2_Diseno.png'), 16.3,
             ['Dibujo de diseño a escala. Incluye las tres franjas que inunda la Doncella durante el combate. Elaboración propia.'])
    g.tabla('tramos2', 'Tramos del Nivel 2', ['Tramo', 'Qué hay', 'Qué enseña'], [
        ['La superficie helada', 'Hielo, un Lamento, placas que suben y bajan, la plaza con la Escultura, la Plañidera y la '
         'muralla', 'Suprimir no lleva a ningún sitio'],
        ['Hundirse', 'El tapón de hielo, la calle hundida con sus Medusas, la campana con su bolsa de aire y la torre del reloj',
         'Dejarse hundir; el agua también sostiene'],
        ['El canal y el patio', 'Placas a la contra sobre el agua, las dos Esculturas entre estatuas y un Lamento',
         'Lo aprendido arriba y abajo, junto'],
        ['La arena', 'Sala de 25 × 15 u con placas y un balcón que nunca se inunda', 'La ventana es el llanto'],
    ], [2100, 4750, 2500], ['Elaboración propia a partir de DisenoNivel2.'])
    g.figura('personajes2', 'Enemigos y jefa del mundo de la Tristeza', os.path.join(FIG, 'personajes_tristeza.png'), 11.0,
             ['El Lamento (4 de vida) vuelve pesado a Kael 2 s; la Escultura (5) duerme como una estatua; la Medusa no muere, se '
              'aparta; la Plañidera (10) lanza esquirlas y después llora sin defenderse. Arte del proyecto.'])
    g.figura('capturas2', 'Recorrido del Nivel 2: (a) el primer hielo y un Lamento, (b) la plaza helada y la Plañidera, (c) '
             'la calle hundida y sus Medusas y (d) la Doncella del Lamento, llorando', os.path.join(FIG, 'nivel2_capturas.jpg'),
             16.0, ['Capturas del juego a 1920 × 1080 (21 de octubre de 2026).'])

    g.h2('4.3 Mundo 3: el Miedo, Bosque Tenebroso')
    g.p('Idea: «el miedo paraliza; el nivel enseña a avanzar a oscuras», en tres tiempos. Huir: en la linde, correr hace '
        'ruido y las trampas solo se ven a 1 u. Esconderse: el bosque cerrado está a oscuras y caminar despacio es silencioso, '
        'pero los Espectros cazan en la oscuridad, y la ruta fácil (la hondonada) acaba en un callejón de raíces; la buena, por '
        'las copas, está marcada con cintas que solo se ven con luz. Enfrentar: en el claro, el Acechador se vence haciéndose '
        'oír a propósito delante de un árbol, y el Espectro del Pavor, acercándose cuando avisa. Kael lleva su propia luz de 3 u, '
        'y cada antorcha es una decisión: ilumina 8 u durante 15 s, pero despierta a los Espectros que duermen en su luz. Entre '
        'el segundo checkpoint y el claro, una ciénaga con marañas sumergidas solo se cruza con el Dash Acuático (',
        ('ref', 'esquema3'), ').')
    g.figura('esquema3', 'Esquema del recorrido del Nivel 3', os.path.join(DIS, 'Nivel3_Diseno.png'), 16.3,
             ['Dibujo de diseño a escala. Elaboración propia.'])
    g.figura('personajes3', 'Enemigos y jefe del mundo del Miedo', os.path.join(FIG, 'personajes_miedo.png'), 15.0,
             ['El Espectro (2 de vida) duerme y casi no se ve; el Centinela Ciego (5) no ve, oye; el Acechador (10) embiste '
              'hacia el ruido y queda aturdido contra los árboles. Arte del proyecto.'])
    g.figura('capturas3', 'Recorrido del Nivel 3: (a) la trinchera del primer checkpoint, (b) un Centinela Ciego, (c) la '
             'ciénaga y (d) el Acechador en el claro', os.path.join(FIG, 'nivel3_capturas.jpg'), 16.0,
             ['Renders de la escena con el arte del mundo, sin la oscuridad del nivel.'])

    g.h2('4.4 Mundo 4: la Culpa, Campos de Batalla Arrasados (en desarrollo)')
    g.p('Idea: la culpa pesa, pero no define la identidad; evitarla sale más caro que enfrentarla. El recorrido mide unas '
        '200 u en tres secciones. En el campo arrasado, los Artilleros disparan a la cabeza: solo protege agacharse tras una '
        'cobertura, y se avanza mientras recargan. La tierra de nadie, bajo un campanario con dos Artilleros que se turnan, solo '
        'se cruza con la Invisibilidad. Después se elige entre los tejados y la calle, se cruza un río bajo un puente volado, con '
        'alambradas que exigen el Dash Acuático y una mina, y se llega al patio del Verdugo, el mini-jefe, que se arrodilla si '
        'le estalla un barril cerca. Por el camino hay cuatro memorias: recordarlas vuelve más agresivos a los enemigos de su '
        'zona; no recordar ninguna da al Juez Silente un 30 % más de vida. En la plaza, cada ataque del Juez es una de esas '
        'memorias, y mirar su marca en el suelo lo arrodilla 3 s (', ('ref', 'esquema4'), ' y ', ('ref', 'personajes4'), ').')
    g.figura('esquema4', 'Esquema del recorrido del Nivel 4', os.path.join(DIS, 'Nivel4_Diseno.png'), 16.3,
             ['Dibujo de diseño a escala, aprobado el 20 de octubre de 2026. Elaboración propia.'])
    g.figura('personajes4', 'Enemigos del mundo de la Culpa', os.path.join(FIG, 'personajes_culpa.png'), 14.0,
             ['Arte limpiado el 21 de octubre de 2026, en proceso de integración. Arte del proyecto.'])

    g.h2('4.5 Mundo 5: la Esperanza, Jardín Etéreo')
    g.p('Un recorrido lineal y suave, de unas 50 u, sin enemigos convencionales: cada tramo exige una de las cuatro '
        'habilidades, de modo que el mundo es un repaso de toda la progresión. No tiene jefe; termina con una cinemática en la '
        'que Kael ve por primera vez al soldado, de espaldas, a lo lejos. Música orquestal suave, con arpa y cuerdas.')
    g.h2('4.6 Mundo 6: el Espejo del Origen')
    g.p('Unas 40 u y una arena simétrica de 30 × 18 u. El Verdadero Soldado pelea en tres fases: un combate espejo que copia '
        'el estilo del jugador, una segunda fase que invoca zonas de cada mundo y una tercera en la que deja de atacar y ambos '
        'avanzan hacia la misma luz. Se vence por integración, no por violencia, y el juego termina con la pregunta «¿Eres un '
        'error… o una nueva identidad?».')

    # --- 5 ---
    g.h1('5. ENEMIGOS Y JEFES')
    g.p('Cada mundo tiene un enemigo común de la familia de los Ecos, del mismo cuerpo, con el aspecto de su emoción y al '
        'mismo ritmo de ataque, y uno o dos enemigos propios y un mini-jefe. Todos avisan antes de golpear, y cada uno enseña '
        'algo sobre su emoción (', ('ref', 'fichas'), ').')
    g.tabla('fichas', 'Fichas de los enemigos', ['Enemigo', 'Mundo', 'Vida y daño', 'Comportamiento', 'Qué enseña'], [
        ['Eco de la Ira', 'Ira', '3 · 1', 'Patrulla, persigue y anticipa su zarpazo; se le interrumpe a golpes', 'A pelear'],
        ['Golem de Ceniza (mini-jefe)', 'Ira', '10 · 2', 'Hiperarmadura, onda por el suelo, inmune a la lava', 'Leer el '
         'ataque antes de responder'],
        ['Lamento', 'Tristeza', '4 · 1', 'Lento; su zarpazo vuelve pesado a Kael 2 s', 'La tristeza se pega'],
        ['Escultura Viviente', 'Tristeza', '5 · 2', 'Duerme como una estatua; golpe lento y telegrafiado', 'No todo lo quieto '
         'está muerto'],
        ['Medusa de Hielo', 'Tristeza', '— · 1', 'Flota y quema al contacto; no muere', 'Hay cosas que se atraviesan'],
        ['Plañidera (mini-jefa)', 'Tristeza', '10 · 2', 'Lanza tres esquirlas y después llora sin defenderse', 'Esperar a que '
         'baje la guardia'],
        ['Espectro', 'Miedo', '2 · 1', 'Duerme; despierto, caza a oscuras y no entra en la luz', 'El miedo se acerca cuando '
         'no se mira'],
        ['Centinela Ciego', 'Miedo', '5 · 2', 'Oye en un cono; se detiene, ladea la cabeza y apuñala', 'El silencio protege'],
        ['Acechador (mini-jefe)', 'Miedo', '10 · 2', 'Embiste hacia el ruido; aturdido contra un árbol', 'Enfrentar a '
         'propósito'],
        ['Penitente', 'Culpa', '3 · 1', 'El Eco de la Culpa: arrastra cadenas; invisible, Kael lo pierde', 'A pelear con la '
         'carga'],
        ['Artillero', 'Culpa', '3 · 1', 'Apunta 0,8 s a la cabeza, dispara tres balas y recarga 2 s', 'Cubrirse y avanzar '
         'cuando recarga'],
        ['Verdugo (mini-jefe)', 'Culpa', '16 · 2', 'Hachazo, barrido y cadena; de rodillas con una explosión cerca', 'Usar el '
         'entorno'],
    ], [1900, 1000, 1150, 3200, 2100], ['Datos de GeneradorEnemigos. Elaboración propia.'])
    g.tabla('jefes', 'Fichas de los jefes', ['Jefe', 'Vida y fases', 'Cuándo se le hiere', 'Recompensa'], [
        ['Centinela Ardiente', '30; tres fases (50 % y 25 %)', 'Cuando su espada queda clavada (1,2 s, daño doble)',
         'Fragmento de la Ira · Ataque Cargado'],
        ['Doncella del Lamento', '26; dos fases (50 %)', 'Mientras llora: en el hielo, sobre su capa de hielo o en su agua',
         'Fragmento de la Tristeza · Dash Acuático'],
        ['Espectro del Pavor', '22; dos fases (50 %)', 'Visible tras cada ataque; en la fase 2, solo la figura con la '
         'luciérnaga dorada', 'Fragmento del Miedo · Invisibilidad Parcial'],
        ['Juez Silente', '28 (36 sin memorias); dos fases', 'Arrodillado 3 s al mirar la marca de la memoria de su ataque; en '
         'la fase 2, una marca es una trampa', 'Fragmento de la Culpa · Defensa Reforzada'],
        ['El Verdadero Soldado', 'Tres fases', 'La última fase se supera sin atacar', 'El final del juego'],
    ], [2100, 2050, 3400, 1800], ['Elaboración propia.'])

    # --- 6 ---
    g.h1('6. INTERFAZ')
    g.p('La interfaz no tapa el juego: el HUD ocupa las esquinas superiores (vida y mundo a la izquierda; puntaje y tiempo a '
        'la derecha; bajo el agua, el oxígeno), la línea de diálogo aparece abajo y la barra del jefe solo durante el combate. '
        'Las pantallas superpuestas comparten una plantilla con el color de acento de cada mundo (', ('ref', 'pantallas'),
        ', ', ('ref', 'navegacion'), ' y ', ('ref', 'interfaces'), ').')
    g.tabla('pantallas', 'Pantallas del juego', ['Pantalla', 'Cómo se llega', 'Qué ofrece'], [
        ['Menú principal', 'Al abrir el juego', 'Continuar, Nueva partida, Seleccionar mundo, Yo reconstruido, Mecánicas y '
         'Salir sobre la portada; fragmentos, puntaje total y autores'],
        ['Selector de mundos', 'Menú › Seleccionar mundo', 'Cada mundo con su estado, su dificultad y su mejor puntaje'],
        ['HUD', 'Durante el juego', 'Vida, mundo, puntaje, tiempo, oxígeno, avisos, diálogo y barra del jefe'],
        ['Pausa', 'Esc', 'Continuar, Reiniciar, Mecánicas y Menú principal'],
        ['Mecánicas', 'Menú o pausa', 'Movimientos con sus teclas y las cuatro habilidades, bloqueadas o integradas'],
        ['«Has caído»', 'Al morir', 'Reintentar desde el checkpoint, Reiniciar nivel o Menú principal'],
        ['Cinemática', 'Inicio, arena, derrota del jefe y salida del mundo', 'Planos en pixel art con textos; se saltan tras 2 s'],
        ['«Mundo superado»', 'Al cruzar la salida', 'Registro emocional, fragmento, habilidad y puntaje con su bono'],
        ['Yo reconstruido', 'Menú', 'Kael con los fragmentos integrados'],
    ], [2100, 2650, 4600], ['Elaboración propia.'])
    g.figura('navegacion', 'Pantallas de navegación: (a) menú principal, (b) selector de mundos, (c) pausa y (d) «Has caído»',
             os.path.join(FIG, 'navegacion.jpg'), 16.0, ['Capturas del juego a 1920 × 1080 (21 de octubre de 2026).'])
    g.figura('interfaces', 'Interfaces durante el juego: (a) HUD y línea de apertura, (b) combate con la barra del jefe, (c) '
             'registro emocional y (d) «Mundo superado»', os.path.join(FIG, 'interfaces.jpg'), 16.0,
             ['Capturas del juego a 1920 × 1080 (21 de octubre de 2026).'])
    g.figura('mapa', 'Mapa de navegación entre pantallas', os.path.join(MEDIA, 'image16.png'), 16.0,
             ['Elaboración propia a partir del código del juego.'])

    # --- 7 ---
    g.h1('7. ARTE Y SONIDO')
    g.h2('7.1 Estilo visual')
    g.p('El juego usa pixel art 2D, con el filtro de punto para que el píxel se vea nítido. Cada mundo tiene su paleta y su luz '
        '(', ('ref', 'paletas'), '): el color es parte de la emoción, y el único color cálido de los mundos fríos marca lo que '
        'importa, como la grieta naranja del tapón de hielo o las antorchas del bosque.')
    g.tabla('paletas', 'Paleta y luz de cada mundo', ['Mundo', 'Paleta', 'Luz'], [
        ['Ira', 'Rojos, naranjas y negros de ceniza', 'Cálida e intensa; la lava ilumina desde abajo'],
        ['Tristeza', 'Azules fríos, blancos de hielo y grises', 'Pálida en la superficie; azul y oscura bajo el agua'],
        ['Miedo', 'Verdes apagados, violetas y negros', 'Oscuridad por tramos; la luz de Kael y las antorchas'],
        ['Culpa', 'Pardos, verde oliva muerto y rojo desaturado; ámbar solo para lo que se recuerda', 'Gris de humo'],
        ['Esperanza', 'Claros y luminosos', 'Luz de amanecer'],
        ['Final', 'Las de todos los mundos', 'Un espejo de luz'],
    ], [1500, 4350, 3500], ['Elaboración propia.'])
    g.h2('7.2 Producción del arte')
    g.p('Kael se dibujó a mano en Pixelorama. El resto del arte se produjo con modelos de generación de imágenes (Gemini y '
        'Google AI Studio) a partir de prompts escritos para cada pieza, con reglas aprendidas en cada mundo: fondo magenta '
        'plano, la misma rejilla de píxel en todos los fotogramas de un personaje y un fotograma maestro del que se editan los '
        'demás. Un proceso propio limpia cada imagen: quita el magenta, mide el tamaño del píxel de arte, la reduce a su rejilla '
        'nativa sin reescalarla a mano, borra restos (copias borrosas, rayas de suelo) y calcula el pivote de cada fotograma '
        'encajando su silueta sobre la del modelo. En Unity, cada imagen se importa con su regla (píxeles por unidad y pivote) '
        'y un verificador comprueba, al regenerar el proyecto, que el arte de cada mundo sigue montado como se diseñó.')
    g.h2('7.3 Cinemáticas')
    g.p('Las cinemáticas son listas de planos con sus tiempos, textos, temblores y sonidos, dibujadas con una cámara propia a '
        '240 × 135 píxeles y ampliadas por un factor entero. El juego tiene la cinemática inicial (12 planos, unos 80 s), la '
        'presentación y la derrota de cada jefe construido y la salida de cada mundo (', ('ref', 'cinematicas'), ').')
    g.figura('cinematicas', 'Planos de las cinemáticas de los mundos 2 y 3', os.path.join(FIG, 'cinematicas_23.jpg'), 16.0,
             ['Presentación y derrota de la Doncella del Lamento (a–c), presentación y derrota del Espectro del Pavor (d–e) y '
              'salida del mundo del Miedo (f). Arte del proyecto.'])
    g.h2('7.4 Sonido')
    g.p('El prototipo todavía no tiene sonido. El plan es una música por mundo, en colaboración con el músico Diego Martínez, '
        'y efectos para los golpes, los avisos de los jefes y las mecánicas: en la Ira, percusión intensa; en la Tristeza, una '
        'caja de música lenta y piano con reverberación, a unos 70 BPM; en el Miedo, casi silencio con ruidos ambientales; en '
        'la Culpa, una marcha militar lenta y distorsionada, que calla en las memorias; en el Jardín, orquesta suave con arpa y '
        'cuerdas, a unos 90 BPM, y en el final, el tema principal distorsionado, con capas de todos los mundos.')

    # --- 8 ---
    g.h1('8. DISEÑO PARA EL AUTOCONOCIMIENTO')
    g.p('Fragmentum se diseñó con el marco MDA (Hunicke et al., 2004): las reglas del juego (mecánicas) producen conductas del '
        'jugador (dinámicas) que generan experiencias emocionales (estéticas). Como el diseñador actúa sobre las mecánicas y el '
        'jugador vive primero la estética, cada mundo se diseñó hacia atrás: se partió de la emoción y de la estrategia de '
        'regulación que se quería favorecer (Gross, 1998) y se eligió la regla que la produce (Schell, 2020). La ',
        ('ref', 'matriz'), ' recoge esa correspondencia; sus filas son hipótesis de diseño, no efectos demostrados.')
    g.tabla('matriz', 'Matriz de estrategias de jugabilidad para el autoconocimiento',
            ['Mundo o sistema', 'Mecánica', 'Estrategia psicológica', 'Dimensión que se mide'], [
        ['Ira', 'Todo invita a correr, pero la prisa castiga; la Centinela solo es vulnerable con la espada clavada',
         'Reconocer la urgencia y regular la impulsividad: esperar el momento', 'Atención y reparación'],
        ['Tristeza', 'La superficie helada no lleva a ningún sitio; solo se avanza dejándose hundir; la Doncella cae cuando llora',
         'Aceptar el ritmo de la pérdida y sentir en lugar de suprimir', 'Atención'],
        ['Miedo', 'A oscuras, correr hace ruido; la luz despierta lo que dormía; el Acechador se vence haciéndose oír',
         'Decidir con incertidumbre y exponerse de forma gradual', 'Claridad y reparación'],
        ['Culpa', 'Recordar las memorias endurece a los enemigos; evitarlas fortalece al Juez', 'Afrontar en lugar de evitar; '
         'asumir la responsabilidad sin autocastigo', 'Claridad'],
        ['Esperanza', 'Cada tramo exige una habilidad ganada', 'Reconocer los propios recursos (resiliencia)', 'Reparación y '
         'autoconcepto'],
        ['Final', 'La última fase se supera sin atacar', 'Aceptar el propio origen e integrar la identidad', 'Claridad del '
         'autoconcepto'],
        ['Diálogos de Kael', 'Al empezar cada mundo, Kael nombra la emoción', 'Poner nombre a lo que se siente (Lieberman et '
         'al., 2007)', 'Claridad'],
        ['Registro emocional', 'Al terminar cada mundo, se pregunta la intensidad de la emoción', 'Autoobservación y '
         'granularidad emocional', 'Atención'],
        ['Fragmento y habilidad', 'Cada emoción enfrentada devuelve una habilidad', 'La emoción se integra y se convierte en '
         'recurso', 'Reparación'],
    ], [1800, 3300, 2650, 1600], ['Las dimensiones son las de los instrumentos del estudio (TMMS-24 y SCCS). Elaboración propia.'])
    g.h2('8.1 Registro emocional')
    g.p('Al terminar cada mundo, el juego pregunta «¿Con qué intensidad sentiste [emoción] en este mundo?», con una escala de 1 '
        'a 5, un comentario opcional y un código de participante opcional. Guarda las respuestas en JSON y CSV sin datos '
        'personales. Es a la vez una mecánica de autoobservación y el instrumento de recolección del estudio.')
    g.h2('8.2 Límites y cuidado del jugador')
    g.p('Fragmentum es un juego serio (Michael y Chen, 2006), no un tratamiento: abre la conversación sobre una emoción, pero '
        'no la trabaja por sí solo. No tiene violencia gráfica ni juzga lo que siente el jugador, y en las sesiones de uso se '
        'indica siempre a quién acudir si la experiencia remueve algo difícil (Bienestar Universitario).')

    # --- 9 ---
    g.h1('9. ASPECTOS TÉCNICOS')
    g.p('El juego está hecho en Unity 6 (6000.6.3f1) con C#, la Universal Render Pipeline 2D, el Input System y el Unity Test '
        'Framework. Su arquitectura se organiza en capas (', ('ref', 'arquitectura'), '): el jugador, los enemigos, los jefes '
        'y el flujo global del juego son máquinas de estados finitos que comparten una misma implementación; los enemigos, los '
        'jefes y los niveles son datos editables (ScriptableObject), y las clases se comunican por eventos. Cada nivel se '
        'describe como una tabla de medidas en el código, y una herramienta del editor monta su escena a partir de ella; una '
        'prueba automatizada comprueba que cada salto del recorrido se puede hacer con las medidas reales de Kael. A 20 de '
        'octubre de 2026, el proyecto tenía 908 pruebas automatizadas (630 EditMode y 278 PlayMode), todas aprobadas, y el '
        'código del juego sumaba 22 526 líneas efectivas en 210 archivos.')
    g.figura('arquitectura', 'Arquitectura del sistema por capas', os.path.join(MEDIA, 'image18.png'), 16.0,
             ['Elaboración propia a partir del código del juego.'])

    # --- 10 ---
    g.h1('10. ESTADO DE LA PRODUCCIÓN')
    g.tabla('estado', 'Estado de cada mundo a 21 de octubre de 2026',
            ['Mundo', 'Recorrido', 'Mecánicas', 'Enemigos', 'Jefe', 'Arte', 'Cinemáticas', 'Sonido'], [
        ['1. Ira', 'Hecho', 'Hecho', 'Hecho', 'Hecho', 'Hecho', 'Hecho', 'Pendiente'],
        ['2. Tristeza', 'Hecho', 'Hecho', 'Hecho', 'Hecho', 'Hecho', 'Hecho', 'Pendiente'],
        ['3. Miedo', 'Hecho', 'Hecho', 'Hecho', 'Hecho', 'Hecho', 'Hecho', 'Pendiente'],
        ['4. Culpa', 'Hecho', 'Hecho', 'Hecho', 'Hecho', 'En curso', 'Pendiente', 'Pendiente'],
        ['5. Esperanza', 'Diseñado', 'Diseñado', '—', '—', 'Pendiente', 'Guion', 'Pendiente'],
        ['6. Final', 'Diseñado', 'Diseñado', '—', 'Jefe base', 'Pendiente', 'Guion', 'Pendiente'],
    ], [1400, 1150, 1150, 1150, 1100, 1150, 1200, 1050], ['«Hecho»: implementado y verificado con pruebas automatizadas. '
                                                          'Elaboración propia.'])
    g.p('Lo siguiente es terminar el mundo de la Culpa (vestir a sus enemigos y al Juez con su arte, sus cinemáticas y sus '
        'puertas), construir el Jardín Etéreo y el Espejo del Origen sobre la misma arquitectura, y añadir la música y los '
        'efectos de sonido.')

    # --- Referencias ---
    g.h1('REFERENCIAS')
    g._poner(g.d.parrafo('referencia', ['Adams, E. (2014). ', ('i', 'Fundamentals of game design'), ' (3.ª ed.). New Riders.']))
    for inicio in ('Gross, J. J. (1998)', 'Hunicke, R., LeBlanc', 'Lieberman, M. D.', 'Matt Makes Games', 'Merry, S. N.',
                   'Michael, D. R. y Chen'):
        g.referencia(inicio)
    g.referencia('Nomada Studio')
    g._poner(g.d.parrafo('referencia', ['Rogers, S. (2014). ', ('i', 'Level up! The guide to great video game design'),
                                        ' (2.ª ed.). Wiley.']))
    g.referencia('Schell, J. (2020)')
    g.referencia('Schoneveld, E. A.')


if __name__ == '__main__':
    g = GDD(sys.argv[1])
    construir(g)
    g.guardar(sys.argv[2])
    print('guardado', sys.argv[2])
