"""
Esquema del Nivel 3 (Docs/Diseno/Nivel3_Diseno.png): vista lateral a escala, 1 u = 14 px.

Es un dibujo de diseño, no una captura del juego. Las medidas son las del recorrido montado
(Assets/Scripts/Editor/DisenoNivel3.cs y Docs/Diseno/Nivel3_Diseno.md): si cambias una, cambia
las otras (la luz de cada tramo está en DisenoNivel3.Luces; los árboles y el Acechador, en
DisenoNivel3.Arboles y DisenoNivel3.Enemigos).

Uso:  python Herramientas/esquema_nivel3.py
"""
import math
import os
from PIL import Image, ImageDraw, ImageFont

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, '..', 'Docs', 'Diseno', 'Nivel3_Diseno.png')

U = 14                      # px por unidad
X0, X1, Y0, Y1 = -7, 168, -7, 17
MARGEN_IZQ, MARGEN_SUP, MARGEN_INF = 40, 76, 120
ANCHO = (X1 - X0) * U + MARGEN_IZQ + 24
ALTO = (Y1 - Y0) * U + MARGEN_SUP + MARGEN_INF

# Paleta del Anexo A (negro profundo, violeta oscuro, verde pantanoso, gris espectral) y derivados
FONDO = (10, 10, 15)             # #0A0A0F
TIERRA = (26, 46, 26)            # #1A2E1A
BORDE_TIERRA = (70, 96, 66)
RAMA = (110, 84, 56)
PODRIDA = (150, 150, 160)
RAIZ = (58, 58, 74)              # #3A3A4A: las raíces que no agarran
LUNA = (200, 210, 230)
ANTORCHA = (255, 176, 64)
CEPO = (220, 70, 70)
ESPECTRO = (110, 190, 130)       # el verde del Eco del Miedo
CENTINELA = (150, 120, 200)
ACECHADOR = (90, 70, 140)
JEFE = (120, 255, 190)           # el brillo verde de jefe3.jpeg
TEXTO = (236, 236, 244)
TENUE = (150, 146, 172)
ACENTO = (45, 27, 78)            # #2D1B4E, el color del Miedo
AGUA = (34, 70, 58, 150)         # el agua de la ciénaga
MARANA = (96, 120, 64)           # las marañas: solo el Dash Acuático las cruza
TOCON = (88, 70, 50)

FONDO_BLOQUES = -7

# Secciones, con la luz que tiene cada una
SECCIONES = ((-7, 36, 'SECCIÓN 1 · La linde (luz de luna)', (22, 22, 34)),
             (36, 112, 'SECCIÓN 2 · El bosque cerrado y la ciénaga (oscuridad)', (12, 12, 18)),
             (112, 140, 'SECCIÓN 3 · El claro (luna)', (22, 22, 36)),
             (140, 168, 'ARENA · El Espectro del Pavor (oscuridad total)', (8, 8, 12)))


def fuente(tamano, negrita=False):
    for nombre in (('arialbd.ttf' if negrita else 'arial.ttf'), 'DejaVuSans.ttf'):
        try:
            return ImageFont.truetype(nombre, tamano)
        except OSError:
            continue
    return ImageFont.load_default()


def px(x, y):
    return MARGEN_IZQ + (x - X0) * U, MARGEN_SUP + (Y1 - y) * U


def rect(d, x0, y0, x1, y1, color, borde=None, ancho=1):
    y0, y1 = max(y0, Y0), min(y1, Y1)
    a, b = px(x0, y1)
    c, e = px(x1, y0)
    d.rectangle([a, b, c, e], fill=color, outline=borde, width=ancho)


def circulo_discontinuo(d, x, y, radio, color, trozos=48):
    cx, cy = px(x, y)
    r = radio * U
    for i in range(0, trozos, 2):
        a0, a1 = 2 * math.pi * i / trozos, 2 * math.pi * (i + 1) / trozos
        d.line([cx + r * math.cos(a0), cy + r * math.sin(a0), cx + r * math.cos(a1), cy + r * math.sin(a1)],
               fill=color, width=1)


img = Image.new('RGB', (ANCHO, ALTO), FONDO)
d = ImageDraw.Draw(img, 'RGBA')
f, fb, fp, ft = fuente(13), fuente(13, True), fuente(11), fuente(20, True)

for desde, hasta, _, color in SECCIONES:
    rect(d, desde, Y0, hasta, Y1, color)
# El claro (el único sitio del bosque sin copas) y la antesala: entra la luna desde x 117
rect(d, 117, 0, 140, Y1, (30, 32, 48))

# --- La propuesta (Nivel3_Diseno.md, sección 3) ---
SUELOS = [(-6, 17, 0), (17, 27, -2.5), (27, 38, 0), (38, 46, -2), (46, 58, 0), (58, 73, -4), (74, 86, 0),
          (86, 96, -3.5), (96, 98, 0), (98, 110, -3.5), (110, 140, 0), (140, 167, 0)]
RAMAS = [(18, 20, 0), (24, 26, 0),                    # el barranco de la linde
         (59, 61, 2), (62, 64, 4),                    # subir a las copas desde la bifurcación
         (68, 74, 4.5),                               # el puesto del vigía
         (78, 80, 3),                                 # bajar de las copas
         (145, 148, 3.5), (159, 162, 3.5)]            # las dos ramas de la arena
PODRIDAS = [(21, 23, 0), (65, 67, 4), (75, 77, 4)]
BLOQUES = [(58, 60, -4, -2)]                          # el escalón para salir de la hondonada
PAREDES = [(-7, -6, FONDO_BLOQUES, 17),               # ParedIzquierda
           (166, 167, 0, 17),                         # ParedArena
           (140, 167, 15, 17)]                        # TechoArena
RAICES = (73, 74, FONDO_BLOQUES, 4)                   # el fondo del callejón: no agarra

for x0, x1, y in SUELOS:
    rect(d, x0, FONDO_BLOQUES, x1, y, TIERRA, BORDE_TIERRA)
for x0, x1, y0, y1 in BLOQUES + PAREDES:
    rect(d, x0, y0, x1, y1, TIERRA, BORDE_TIERRA)
rect(d, RAICES[0], RAICES[2], RAICES[1], RAICES[3], RAIZ, PODRIDA, 2)

# La ciénaga: el agua (sin el tocón), las raíces del árbol hundido que hacen de techo de los túneles
# (no agarran) y las marañas, que solo cruza el Dash Acuático
for x0, x1 in ((86, 96), (98, 110)):
    rect(d, x0, -3.5, x1, -0.1, AGUA)
rect(d, 96, -3.5, 98, 0, TOCON)
for x0, x1 in ((89, 95), (100, 107)):
    rect(d, x0, -1, x1, 7, RAIZ, PODRIDA, 2)
for x0, x1 in ((91, 93), (101, 105)):
    rect(d, x0, -3.5, x1, -1, MARANA)
for x0, x1, y in RAMAS:
    rect(d, x0, y - 0.4, x1, y, RAMA)
for x0, x1, y in PODRIDAS:
    rect(d, x0, y - 0.4, x1, y, PODRIDA)
    a, b = px((x0 + x1) / 2, y + 1.0)
    d.text((a - 3, b), '×', font=fb, fill=PODRIDA)
# Las cintas blancas de la ruta buena (las copas): cuelgan de lo alto de su rama
for x, y in ((60, 2), (63, 4), (69, 4.5), (79, 3)):
    rect(d, x - 0.08, y - 0.85, x + 0.08, y + 0.05, (240, 240, 232))

# La trinchera, con su checkpoint y su eco de memoria
a, b = px(38.3, -2.4)
d.text((a, b - 2), 'la trinchera', font=fp, fill=TENUE)

# Los árboles del claro: Kael pasa por delante; el Acechador, que embiste a ciegas, se estrella
for x in (121, 126, 131):
    rect(d, x - 0.6, 0, x + 0.6, 6, (60, 50, 40), (100, 84, 64))
a, b = px(119.4, 7.8)
d.text((a, b), 'tres árboles: el Acechador se estrella', font=fp, fill=TENUE)

# Luz de las antorchas (8 u) y de Kael (3 u)
for x, y in ((7, 0), (30, 0), (55, 0), (85, 0), (114, 0)):
    circulo_discontinuo(d, x, y + 1.5, 8, (255, 176, 64, 150))
    a, b = px(x, y)
    d.rectangle([a - 2, b - 26, a + 2, b], fill=(120, 90, 60))
    d.ellipse([a - 6, b - 36, a + 6, b - 24], fill=ANTORCHA)
circulo_discontinuo(d, 2, 0.9, 3, (220, 220, 255, 170))


def punto(x, y, color, letra, r=7):
    a, b = px(x, y)
    d.ellipse([a - r, b - 2 * r, a + r, b], fill=color)
    w = d.textlength(letra, font=fp)
    d.text((a - w / 2, b - 2 * r + 1), letra, font=fp, fill=(10, 10, 16))


def cepo(x, y):
    a, b = px(x, y)
    d.polygon([(a - 7, b), (a, b - 8), (a + 7, b)], outline=CEPO, fill=(80, 20, 20))


for x, y in ((11, 0), (22, -2.5), (34, 0), (61.5, -4), (67, -4)):
    cepo(x, y)
for x, y in ((14, 0), (33, 0), (56.5, 0), (76, 0), (69, -4), (71.5, -4)):
    punto(x, y, ESPECTRO, 'E')
for x, y in ((51, 0), (71, 4.5), (63.5, -4)):
    punto(x, y, CENTINELA, 'C', r=8)
punto(128, 0, ACECHADOR, 'A', r=13)
punto(153.5, 0, JEFE, 'P', r=16)
a, b = px(153.5, 6.6)
d.text((a - 92, b), 'invisible salvo al avisar y atacar', font=fp, fill=TEXTO)

# Inicio, checkpoints, salida, verja
a, b = px(2, 0)
d.rectangle([a - 4, b - 22, a + 4, b], fill=LUNA)
d.text((a - 14, b - 40), 'Kael', font=fb, fill=TEXTO)
for x, y, nombre in ((41, -2, 'CP1 · eco de memoria'), (82.5, 0, 'CP2'), (137, 0, 'CP3')):
    a, b = px(x, y)
    d.line([a, b, a, b - 30], fill=(200, 200, 205), width=3)
    d.polygon([(a, b - 30), (a + 12, b - 25), (a, b - 20)], fill=ANTORCHA)
    d.text((a - 10, b - 46), nombre, font=fp, fill=TEXTO)
a, b = px(163.5, 0)
d.rectangle([a - 26, b - 56, a + 26, b], outline=JEFE, width=2)
d.text((a - 16, b - 72), 'salida', font=fp, fill=TEXTO)
a, b = px(141.3, 5.4)
d.text((a, b), '← verja', font=fp, fill=TEXTO)

# Rótulos de tramo
for x, y, texto in ((-5.5, 3.2, 'inicio'), (5, 10.4, 'primera antorcha: E'), (17.2, -4.6, 'el barranco: caer no mata'),
                    (27.5, 10.4, 'segunda antorcha'), (44.2, 4.4, 'salir de la trinchera sin ruido'),
                    (52.5, 10.4, 'la bifurcación'), (57.5, 7.6, 'arriba: las copas (la buena)'),
                    (58.3, -5.9, 'abajo: la hondonada (la trampa)'), (66.2, 6.4, 'el puesto del vigía'),
                    (66.2, -1.6, 'callejón'), (74.4, 6.0, 'raíces que no agarran'),
                    (83.4, 10.4, 'la ciénaga: solo el Dash Acuático'), (95.2, 1.9, 'tocón'),
                    (112.3, 10.4, 'antorcha del claro'), (132.6, 4.4, 'antesala')):
    a, b = px(x, y)
    d.text((a, b), texto, font=fp, fill=TENUE)

# Cotas
for x in range(-5, 168, 5):
    a, b = px(x, Y0)
    d.line([a, b, a, b + 6], fill=TENUE)
    d.text((a - 6, b + 8), str(x), font=fp, fill=TENUE)
for y in (-5, 0, 5, 10, 15):
    a, b = px(X0, y)
    d.text((4, b - 7), f'{y}', font=fp, fill=TENUE)
    d.line([a, b, a + 4, b], fill=TENUE)

for x, _, nombre, _ in SECCIONES:
    a, b = px(x, Y1)
    d.rectangle([a + 2, b + 2, a + 12 + d.textlength(nombre, font=fb), b + 22], fill=FONDO)
    d.text((a + 6, b + 4), nombre, font=fb, fill=TEXTO)

# Título y leyenda
d.text((MARGEN_IZQ, 14), 'Nivel 3 · Miedo · Bosque Tenebroso — el recorrido (vista lateral, 1 u = 1 tile)', font=ft, fill=TEXTO)
d.text((MARGEN_IZQ, 44), 'La luz enseña el camino pero despierta a los Espectros; correr hace ruido y los ciegos oyen. Esconderse sirve; quedarse quieto, no. En la ciénaga, quien duda queda atrapado.',
       font=f, fill=TENUE)
ly = ALTO - MARGEN_INF + 44
items = [(TIERRA, 'tierra'), (RAMA, 'rama'), (PODRIDA, 'rama podrida (cede)'), (RAIZ, 'raíces (no agarran)'),
         (AGUA[:3], 'agua (10 s de aire)'), (MARANA, 'maraña (solo el Dash Acuático)'),
         (ANTORCHA, 'antorcha (8 u, 15 s)'), (CEPO, 'cepo oculto'), (ESPECTRO, 'E · Espectro (Eco del Miedo)'),
         (CENTINELA, 'C · Centinela Ciego'), (ACECHADOR, 'A · el Acechador (mini-jefe)'),
         (JEFE, 'P · Espectro del Pavor')]
lx, fila = MARGEN_IZQ, 0
for color, nombre in items:
    ancho_item = 26 + d.textlength(nombre, font=f) + 18
    if lx + ancho_item > ANCHO - 20:
        lx, fila = MARGEN_IZQ, fila + 1
    d.rectangle([lx, ly + fila * 22, lx + 14, ly + fila * 22 + 14], fill=color)
    d.text((lx + 20, ly + fila * 22), nombre, font=f, fill=TEXTO)
    lx += ancho_item
ly += (fila + 1) * 22 + 8
d.text((MARGEN_IZQ, ly), 'Círculos: la luz de cada antorcha (8 u) y la de Kael (3 u). Diálogos: apertura al aparecer · eco de memoria en el CP1, en la trinchera · presentación del Espectro al cruzar la verja · cierre al recoger el fragmento.',
       font=f, fill=TENUE)
d.text((MARGEN_IZQ, ly + 20), 'Medidas de Kael: sube 2,4 u (4,1 con doble salto). Caminando (Alt) va a 2,1 u/s y no hace ruido; con el doble salto avanza 2,5 u, así que ningún hueco obliga a correr. Los checkpoints curan.',
       font=f, fill=TENUE)

os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
img.save(SALIDA)
print(SALIDA, img.size)
