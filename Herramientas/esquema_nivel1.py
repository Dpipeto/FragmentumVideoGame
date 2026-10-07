"""
Esquema del Nivel 1 propuesto (Docs/Diseno/Nivel1_Diseno.png): vista lateral a escala, 1 u = 16 px.

Es un dibujo de diseño, no el nivel: sirve para discutir el recorrido antes de montarlo en
Unity. Las medidas son las de Docs/Diseno/Nivel1_Diseno.md.

Uso:  python Herramientas/esquema_nivel1.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, '..', 'Docs', 'Diseno', 'Nivel1_Diseno.png')

U = 16                      # px por unidad
X0, X1, Y0, Y1 = -2, 82, -3, 21
MARGEN_IZQ, MARGEN_SUP, MARGEN_INF = 40, 70, 150
ANCHO = (X1 - X0) * U + MARGEN_IZQ + 20
ALTO = (Y1 - Y0) * U + MARGEN_SUP + MARGEN_INF

FONDO = (24, 14, 10)
SUELO = (74, 64, 60)
BORDE_SUELO = (120, 104, 96)
LAVA = (214, 74, 20)
INESTABLE = (150, 112, 82)
COMBUSTION = (92, 64, 51)
TORRETA = (104, 92, 120)
ECO = (200, 40, 40)
GOLEM = (150, 140, 130)
JEFE = (255, 120, 40)
TEXTO = (240, 236, 220)
TENUE = (150, 140, 130)
ACENTO = (255, 107, 53)


def fuente(tamano, negrita=False):
    for nombre in (('arialbd.ttf' if negrita else 'arial.ttf'), 'DejaVuSans.ttf'):
        try:
            return ImageFont.truetype(nombre, tamano)
        except OSError:
            continue
    return ImageFont.load_default()


def px(x, y):
    return MARGEN_IZQ + (x - X0) * U, MARGEN_SUP + (Y1 - y) * U


def rect(d, x0, y0, x1, y1, color, borde=None):
    a, b = px(x0, y1)
    c, e = px(x1, y0)
    d.rectangle([a, b, c, e], fill=color, outline=borde)


img = Image.new('RGB', (ANCHO, ALTO), FONDO)
d = ImageDraw.Draw(img)
f, fb, fp, ft = fuente(13), fuente(13, True), fuente(11), fuente(20, True)

# Secciones
for desde, hasta, nombre, color in ((0, 25, 'SECCIÓN 1 · El despertar', (40, 26, 20)),
                                    (25, 55, 'SECCIÓN 2 · La fortaleza arde', (48, 24, 16)),
                                    (55, 80, 'ARENA · La Centinela', (58, 22, 14))):
    rect(d, desde, Y0, hasta, Y1, color)
    a, b = px(desde, Y1)
    d.text((a + 6, b + 4), nombre, font=fb, fill=TEXTO)

# Suelo (basalto) por tramos: (desde, hasta, altura)
SUELOS = [(-2, 12, 0), (12, 17, -2), (17, 27.5, 2), (33.5, 36.5, 2), (40, 51, 5), (54.5, 56, 5), (56, 80, 5), (80, 82, 5)]
for x0, x1, y in SUELOS:
    rect(d, x0, Y0, x1, y, SUELO, BORDE_SUELO)
rect(d, 4.5, 0, 6, 1, SUELO, BORDE_SUELO)                       # escalón de enseñanza
# Lava (letal)
for x0, x1, y in ((27.5, 33.5, 0.5), (36.5, 40, 0.5), (51, 54.5, 3.5)):
    rect(d, x0, Y0, x1, y, LAVA)
# Paredes de la arena
rect(d, 55, 8.5, 55.6, 20, (90, 70, 66))                         # puerta (se cierra tras entrar)
rect(d, 81.4, 5, 82, 20, (90, 70, 66))
rect(d, 55, 20, 82, 20.6, (90, 70, 66))

# Plataformas
rect(d, 13.25, 0, 15.75, 0.5, INESTABLE, (210, 170, 130))       # inestable sobre un foso que no mata
for x, y in ((29.4, 2), (31.8, 2)):                              # combustión sobre lava
    rect(d, x - 0.9, y - 0.5, x + 0.9, y, COMBUSTION, (230, 120, 60))
    d.text(px(x - 0.7, y + 1.1), '≈', font=f, fill=ACENTO)
for x, y in ((37.5, 3.2), (39, 4.4)):                            # inestables escalonadas
    rect(d, x - 0.8, y - 0.5, x + 0.8, y, INESTABLE, (210, 170, 130))
for x, y, ancho in ((61, 8, 4), (75, 8, 4), (68, 11, 5), (62.5, 14, 3), (73.5, 14, 3)):   # arena, 3 alturas
    rect(d, x - ancho / 2, y - 0.5, x + ancho / 2, y, (110, 90, 84), (170, 140, 130))

# Torreta: en el suelo de la terraza, mirando a la izquierda (la bola va a la altura de Kael)
rect(d, 23.6, 2, 25.4, 3.7, TORRETA, (180, 170, 200))
a, b = px(23.2, 3.0)
d.polygon([(a, b), (a + 10, b - 5), (a + 10, b + 5)], fill=ACENTO)
d.text(px(21.2, 5.6), 'torreta ←', font=fp, fill=TEXTO)


def punto(x, y, color, letra, r=7):
    a, b = px(x, y)
    d.ellipse([a - r, b - 2 * r, a + r, b], fill=color)
    w = d.textlength(letra, font=fp)
    d.text((a - w / 2, b - 2 * r + 1), letra, font=fp, fill=(255, 255, 255))


for x, y in ((10, 0), (19, 2), (21.5, 2), (26.2, 2), (35, 2), (44, 5), (50, 5)):
    punto(x, y, ECO, 'E')
punto(47.5, 5, GOLEM, 'G', r=11)
punto(70, 5, JEFE, 'C', r=16)

# Inicio, checkpoint, salida, entradas de diálogo
a, b = px(2, 0)
d.rectangle([a - 4, b - 22, a + 4, b], fill=(240, 236, 216))
d.text((a - 14, b - 40), 'Kael', font=fb, fill=TEXTO)
a, b = px(41, 5)
d.line([a, b, a, b - 44], fill=(200, 200, 205), width=3)
d.polygon([(a, b - 44), (a + 16, b - 38), (a, b - 32)], fill=ACENTO)
d.text((a - 18, b - 62), 'checkpoint', font=fp, fill=TEXTO)
a, b = px(79.5, 5)
d.rectangle([a - 12, b - 90, a + 12, b], outline=ACENTO, width=2)
d.text((a - 18, b - 108), 'salida', font=fp, fill=TEXTO)
a, b = px(55, 5)
d.text((a - 14, b - 74), 'puerta', font=fp, fill=TEXTO)

# Cotas
for x in range(0, 81, 5):
    a, b = px(x, Y0)
    d.line([a, b, a, b + 6], fill=TENUE)
    d.text((a - 6, b + 8), str(x), font=fp, fill=TENUE)
for y in (0, 5, 10, 15, 20):
    a, b = px(X0, y)
    d.text((4, b - 7), f'{y}', font=fp, fill=TENUE)
    d.line([a, b, a + 4, b], fill=TENUE)

# Título y leyenda
d.text((MARGEN_IZQ, 14), 'Nivel 1 · Ira · Fortaleza Volcánica — propuesta de recorrido (vista lateral, 1 u = 1 tile)', font=ft, fill=TEXTO)
d.text((MARGEN_IZQ, 42), 'Cada mecánica aparece primero sin riesgo, luego sobre lava y por último junto a enemigos.', font=f, fill=TENUE)
ly = ALTO - MARGEN_INF + 42
items = [(SUELO, 'basalto'), (LAVA, 'lava (mata)'), (INESTABLE, 'plataforma inestable'), (COMBUSTION, 'plataforma de combustión'),
         (TORRETA, 'torreta'), (ECO, 'E · Eco de la Ira'), (GOLEM, 'G · Golem de Ceniza'), (JEFE, 'C · Centinela Ardiente')]
lx = MARGEN_IZQ
for color, nombre in items:
    d.rectangle([lx, ly, lx + 14, ly + 14], fill=color)
    d.text((lx + 20, ly), nombre, font=f, fill=TEXTO)
    lx += 26 + d.textlength(nombre, font=f) + 18
d.text((MARGEN_IZQ, ly + 30), 'Diálogos: apertura al aparecer (x 2) · eco de memoria en el checkpoint (x 41) · presentación del jefe al cruzar la puerta (x 55) · cierre al recoger el fragmento.',
       font=f, fill=TENUE)
d.text((MARGEN_IZQ, ly + 50), 'Alturas: el suelo sube de 0 a 5 hacia la fortaleza; la arena va de 5 a 20 (15 u de alto), como pide el Anexo A.',
       font=f, fill=TENUE)

os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
img.save(SALIDA)
print(SALIDA, img.size)
