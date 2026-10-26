"""
Esquema de la ciénaga del Nivel 3 (Docs/Diseno/Nivel3_Cienaga.png): la sección 10 de
Docs/Diseno/Nivel3_Diseno.md, de cerca y a escala (1 u = 22 px).

Es un dibujo de diseño, no una captura del juego. Las medidas son las de
Assets/Scripts/Editor/DisenoNivel3.cs (versión 8): si cambias una, cambia las otras.

Uso:  python Herramientas/esquema_cienaga.py
"""
import math
import os
from PIL import Image, ImageDraw, ImageFont

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, '..', 'Docs', 'Diseno', 'Nivel3_Cienaga.png')

U = 22                      # px por unidad
X0, X1, Y0, Y1 = 72, 119, -6, 10
MARGEN_IZQ, MARGEN_SUP, MARGEN_INF = 40, 76, 112
ANCHO = (X1 - X0) * U + MARGEN_IZQ + 24
ALTO = (Y1 - Y0) * U + MARGEN_SUP + MARGEN_INF

# La paleta de esquema_nivel3.py (Anexo A: negro profundo, violeta oscuro, verde pantanoso, gris espectral)
FONDO = (10, 10, 15)
OSCURO = (12, 12, 18)            # el bosque cerrado
CLARO = (30, 32, 48)             # el claro: entra la luna
TIERRA = (26, 46, 26)
BORDE_TIERRA = (70, 96, 66)
RAMA = (110, 84, 56)
PODRIDA = (150, 150, 160)
RAIZ = (58, 58, 74)              # las raíces del árbol hundido: no agarran
AGUA = (34, 70, 58, 150)         # el agua de la ciénaga, translúcida
SUPERFICIE = (120, 170, 120)
MARANA = (96, 120, 64)
TOCON = (88, 70, 50)
ANTORCHA = (255, 176, 64)
DASH = (127, 230, 176)           # #7FE6B0, el verde pálido de lo que brilla
TEXTO = (236, 236, 244)
TENUE = (150, 146, 172)

FONDO_BLOQUES = -6


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
    x0, x1 = max(x0, X0), min(x1, X1)
    y0, y1 = max(y0, Y0), min(y1, Y1)
    a, b = px(x0, y1)
    c, e = px(x1, y0)
    d.rectangle([a, b, c, e], fill=color, outline=borde, width=ancho)


def circulo_discontinuo(d, x, y, radio, color, trozos=64):
    cx, cy = px(x, y)
    r = radio * U
    for i in range(0, trozos, 2):
        a0, a1 = 2 * math.pi * i / trozos, 2 * math.pi * (i + 1) / trozos
        d.line([cx + r * math.cos(a0), cy + r * math.sin(a0), cx + r * math.cos(a1), cy + r * math.sin(a1)],
               fill=color, width=1)


def flecha(d, x0, x1, y, color, texto=None):
    a, b = px(x0, y)
    c, _ = px(x1, y)
    d.line([a, b, c, b], fill=color, width=3)
    d.polygon([(c, b), (c - 9, b - 5), (c - 9, b + 5)], fill=color)
    if texto:
        w = d.textlength(texto, font=fp)
        d.text(((a + c) / 2 - w / 2, b - 18), texto, font=fp, fill=color)


img = Image.new('RGB', (ANCHO, ALTO), FONDO)
d = ImageDraw.Draw(img, 'RGBA')
f, fb, fp, ft = fuente(13), fuente(13, True), fuente(12), fuente(20, True)

rect(d, X0, Y0, 112, Y1, OSCURO)
rect(d, 112, Y0, X1, Y1, (22, 22, 36))
rect(d, 117, 0, X1, Y1, CLARO)       # la luna del claro empieza en x 117

# --- Lo que ya está montado, a la izquierda: la bajada de las copas y el CP2 ---
rect(d, 72, 4, 74, 4.5, RAMA)                          # el final del puesto del vigía (68 a 74)
rect(d, 73, FONDO_BLOQUES, 74, 4, RAIZ, PODRIDA, 2)    # las raíces del callejón
rect(d, 75, 3.6, 77, 4, PODRIDA)                       # la rama podrida de la bajada
rect(d, 78, 2.6, 80, 3, RAMA)                          # la rama de la bajada

# --- La ciénaga (DisenoNivel3, versión 8) ---
# El suelo: la orilla de las copas (74 a 86) y la otra orilla, que sigue hacia el claro (110 a 119)
rect(d, 74, FONDO_BLOQUES, 86, 0, TIERRA, BORDE_TIERRA)
rect(d, 110, FONDO_BLOQUES, X1, 0, TIERRA, BORDE_TIERRA)
# El fondo de la ciénaga (y −3,5)
rect(d, 86, FONDO_BLOQUES, 110, -3.5, TIERRA, BORDE_TIERRA)
# El agua (superficie a y −0,1), sin el tocón
for x0, x1 in ((86, 96), (98, 110)):
    rect(d, x0, -3.5, x1, -0.1, AGUA)
    a, b = px(x0, -0.1)
    c, _ = px(x1, -0.1)
    d.line([a, b, c, b], fill=SUPERFICIE, width=2)
# El tocón, a ras del agua: encima se respira
rect(d, 96, -3.5, 98, 0, TOCON, (140, 112, 80), 2)
# Las raíces del árbol hundido: el techo de los dos túneles (y −1), hasta donde no llega ningún salto
for x0, x1 in ((89, 95), (100, 107)):
    rect(d, x0, -1, x1, 7, RAIZ, PODRIDA, 2)
a, b = px(89.4, 6.4)
d.text((a, b), 'raíces del árbol hundido (no agarran)', font=fp, fill=TENUE)
# Las marañas: llenan el túnel del fondo al techo; su dibujo cuelga del techo
for x0, x1 in ((91, 93), (101, 103), (103, 105)):
    rect(d, x0, -3.5, x1, -1, (60, 80, 40, 210), MARANA, 2)
    a, b = px(x0, -1)
    c, e = px(x1, -3.5)
    for i in range(0, int(c - a) + int(e - b), 9):
        d.line([max(a, a + i - (e - b)), min(e, b + i), min(c, a + i), max(b, b + i - (c - a))], fill=MARANA, width=1)

# Los dashes: 2,9 u cada uno (16 u/s durante 0,18 s); el agua no los frena. Para salir hay que sacar
# el cuerpo (0,8 u): la primera, de un dash; la segunda, de dos.
flecha(d, 90.6, 93.5, -2.2, DASH, 'dash · 2,9 u')
flecha(d, 100.6, 103.5, -2.2, DASH, '1.º dash')
flecha(d, 103.5, 106.4, -2.8, DASH, '2.º dash')
a, b = px(99.7, -4.75)
d.text((a, b), 'entre los dos dashes, 0,6 s sujeto', font=fp, fill=DASH)

# La antorcha de la orilla (8 u de luz) y la antorcha del claro, ya corrida (86 → 114)
for x in (85, 114):
    circulo_discontinuo(d, x, 1.5, 8, (255, 176, 64, 150))
    a, b = px(x, 0)
    d.rectangle([a - 2, b - 30, a + 2, b], fill=(120, 90, 60))
    d.ellipse([a - 7, b - 42, a + 7, b - 28], fill=ANTORCHA)

# El Espectro de la bajada, en x 76: la antorcha de la orilla no lo alcanza
a, b = px(76, 0)
d.ellipse([a - 7, b - 14, a + 7, b], fill=(110, 190, 130))
d.text((a - 3, b - 13), 'E', font=fp, fill=(10, 10, 16))

# El CP2, la pista y Kael con su luz (3 u)
a, b = px(82.5, 0)
d.line([a, b, a, b - 34], fill=(200, 200, 205), width=3)
d.polygon([(a, b - 34), (a + 13, b - 28), (a, b - 22)], fill=ANTORCHA)
d.text((a - 10, b - 52), 'CP2', font=fb, fill=TEXTO)
a, b = px(84, 0)
d.ellipse([a - 9, b - 66, a + 9, b - 48], outline=TEXTO, width=2)
d.text((a - 3, b - 65), '!', font=fb, fill=TEXTO)
d.text((a - 14, b - 84), 'pista', font=fp, fill=TEXTO)
a, b = px(88, -3.5)
d.rectangle([a - 5, b - 40, a + 5, b], fill=(200, 210, 230))
circulo_discontinuo(d, 88, -2.6, 3, (220, 220, 255, 170))

# El claro, que empieza en x 117 (antes 89)
a, b = px(116.4, 7.6)
d.text((a, b), 'el claro →', font=fp, fill=TENUE)

# Rótulos de tramo
for x, y, texto in ((83.4, -0.5, 'la orilla'), (86.2, -4.1, 'la poza'), (89.2, -4.75, 'primer túnel · maraña de 2 u'),
                    (95.6, 1.9, 'el tocón: respirar'), (99.7, -4.1, 'segundo túnel · maraña de 4 u'),
                    (107.2, 1.0, 'la salida'), (109.3, 2.4, 'la otra orilla'), (74.4, 8.0, 'la bajada de las copas')):
    a, b = px(x, y)
    d.text((a, b), texto, font=fp, fill=TENUE)

# Cotas
for x in range(X0, X1 + 1, 2):
    a, b = px(x, Y0)
    d.line([a, b, a, b + 6], fill=TENUE)
    d.text((a - 8, b + 8), str(x), font=fp, fill=TENUE)
for y in (-4, 0, 4, 8):
    a, b = px(X0, y)
    d.text((4, b - 7), f'{y}', font=fp, fill=TENUE)
    d.line([a, b, a + 4, b], fill=TENUE)

# Título y leyenda
d.text((MARGEN_IZQ, 14), 'Nivel 3 · la ciénaga (vista lateral, 1 u = 1 tile). Todo lo que sigue se corrió 28 u.',
       font=ft, fill=TEXTO)
d.text((MARGEN_IZQ, 44), 'Bajo el agua, las marañas sujetan a quien duda (5 % de su velocidad) y el aire se acaba. Solo el Dash Acuático las cruza.',
       font=f, fill=TENUE)
ly = ALTO - MARGEN_INF + 46
items = [(TIERRA, 'tierra'), (AGUA[:3], 'agua (10 s de aire)'), (RAIZ, 'raíces (no agarran)'), (MARANA, 'maraña'),
         (TOCON, 'tocón (se respira)'), (ANTORCHA, 'antorcha (8 u, 15 s)'), (DASH, 'dash (2,9 u)')]
lx = MARGEN_IZQ
for color, nombre in items:
    d.rectangle([lx, ly, lx + 14, ly + 14], fill=color)
    d.text((lx + 20, ly), nombre, font=f, fill=TEXTO)
    lx += 26 + d.textlength(nombre, font=f) + 18
d.text((MARGEN_IZQ, ly + 26), 'Bajo el agua Kael va a 3,6 u/s y sube 2,9 u desde el fondo (5 con el doble salto): así sale al tocón y a las orillas (3,5 u).',
       font=f, fill=TENUE)
d.text((MARGEN_IZQ, ly + 46), 'Cada tramo bajo el agua cuesta unos 3 a 4 s de los 10 de aire. Sin el dash, la primera maraña cuesta 16 s y la segunda, 27.',
       font=f, fill=TENUE)

os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
img.save(SALIDA)
print(SALIDA, img.size)
