"""
Esquema del Nivel 1 propuesto (Docs/Diseno/Nivel1_Diseno.png): vista lateral a escala, 1 u = 16 px.

Es un dibujo de diseño, no una captura del juego. Las medidas son las del recorrido montado
(Assets/Scripts/Editor/DisenoNivel1.cs y Docs/Diseno/Nivel1_Diseno.md): si cambias una, cambia
la otra.

Uso:  python Herramientas/esquema_nivel1.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, '..', 'Docs', 'Diseno', 'Nivel1_Diseno.png')

U = 16                      # px por unidad
X0, X1, Y0, Y1 = -2, 127, -3, 21
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
                                    (55, 100, 'SECCIÓN 3 · La forja', (52, 28, 16)),
                                    (100, 125, 'ARENA · La Centinela', (58, 22, 14))):
    rect(d, desde, Y0, hasta, Y1, color)
    a, b = px(desde, Y1)
    d.text((a + 6, b + 4), nombre, font=fb, fill=TEXTO)

# Suelo (basalto) por tramos: (desde, hasta, altura)
SUELOS = [(-2, 12, 0), (12, 17, -2), (17, 27, 2), (27, 36, 0), (36, 39, 2), (39, 42, 0), (42, 52, 5), (52, 55, 3),
          (55, 60, 5), (60, 64, 3), (64, 75, 7), (75, 87, 5), (87, 92, 7), (92, 97, 5), (97, 100, 3), (100, 127, 5)]
for x0, x1, y in SUELOS:
    rect(d, x0, Y0, x1, y, SUELO, BORDE_SUELO)
rect(d, 4, 0, 6, 1, SUELO, BORDE_SUELO)                         # escalón de enseñanza
# Lava (letal), apoyada en su cuenca
for x0, x1, y in ((27, 36, 4 / 3), (39, 42, 4 / 3), (52, 55, 13 / 3), (60, 64, 13 / 3), (75, 87, 19 / 3), (97, 100, 13 / 3)):
    rect(d, x0, y - 4 / 3, x1, y, LAVA)
# Paredes de la arena
rect(d, 100, 5, 101, 20, (90, 70, 66))                           # verja (se cierra tras entrar)
rect(d, 126, 5, 127, 20.6, (90, 70, 66))
rect(d, 100, 20, 127, 20.6, (90, 70, 66))

# Plataformas
rect(d, 14.5 - 1.92, -0.875, 14.5 + 1.92, 0, INESTABLE, (210, 170, 130))   # inestable sobre un foso que no mata
for x, y in ((29.43, 2), (33.57, 2)):                            # combustión sobre lava, desfasadas
    rect(d, x - 1.71, y - 1.29, x + 1.71, y, COMBUSTION, (230, 120, 60))
    d.text(px(x - 0.3, y + 1.1), '≈', font=f, fill=ACENTO)
rect(d, 40.5 - 1.92, 3.5 - 0.875, 40.5 + 1.92, 3.5, INESTABLE, (210, 170, 130))   # inestable sobre la lava
# La forja: la inestable de la escalera y, en el lago, una inestable entre dos de combustión.
rect(d, 62 - 1.92, 6 - 0.875, 62 + 1.92, 6, INESTABLE, (210, 170, 130))
rect(d, 81 - 1.92, 7 - 0.875, 81 + 1.92, 7, INESTABLE, (210, 170, 130))
for x, y in ((77.2, 7), (84.8, 7)):
    rect(d, x - 1.71, y - 1.29, x + 1.71, y, COMBUSTION, (230, 120, 60))
    d.text(px(x - 0.3, y + 1.1), '≈', font=f, fill=ACENTO)
for x0, x1, y in ((104, 108, 8), (118, 122, 8), (110, 115, 11), (106, 109, 14), (117, 120, 14)):   # arena, 3 alturas
    rect(d, x0, y - 0.5, x1, y, (110, 90, 84), (170, 140, 130))

# Torreta: en el suelo de la terraza, mirando a la izquierda (la bola va a la altura de Kael)
rect(d, 22.42, 2, 24.34, 3.62, TORRETA, (180, 170, 200))
a, b = px(22.1, 2.9)
d.polygon([(a, b), (a + 10, b - 5), (a + 10, b + 5)], fill=ACENTO)
d.text(px(21.4, 5.6), 'torreta ←', font=fp, fill=TEXTO)
rect(d, 71.42, 7, 73.34, 8.62, TORRETA, (180, 170, 200))          # la segunda, en la almena, hacia los dos Ecos
a, b = px(71.1, 7.9)
d.polygon([(a, b), (a + 10, b - 5), (a + 10, b + 5)], fill=ACENTO)
d.text(px(70.4, 10.6), 'torreta ←', font=fp, fill=TEXTO)


def punto(x, y, color, letra, r=7):
    a, b = px(x, y)
    d.ellipse([a - r, b - 2 * r, a + r, b], fill=color)
    w = d.textlength(letra, font=fp)
    d.text((a - w / 2, b - 2 * r + 1), letra, font=fp, fill=(255, 255, 255))


for x, y in ((10, 0), (18.8, 2), (21, 2), (26, 2), (37.5, 2), (45.5, 5), (51, 5), (66.5, 7), (69.5, 7), (89.5, 7)):
    punto(x, y, ECO, 'E')
punto(48.5, 5, GOLEM, 'G', r=11)
punto(115, 5, JEFE, 'C', r=16)

# Inicio, checkpoint, salida, entradas de diálogo
a, b = px(2, 0)
d.rectangle([a - 4, b - 22, a + 4, b], fill=(240, 236, 216))
d.text((a - 14, b - 40), 'Kael', font=fb, fill=TEXTO)
for x, y in ((43, 5), (57.5, 5), (94.5, 5)):                  # checkpoints: antes del Golem, tras él y ante la arena
    a, b = px(x, y)
    d.line([a, b, a, b - 44], fill=(200, 200, 205), width=3)
    d.polygon([(a, b - 44), (a + 16, b - 38), (a, b - 32)], fill=ACENTO)
    d.text((a - 18, b - 62), 'checkpoint', font=fp, fill=TEXTO)
a, b = px(124, 5)
d.rectangle([a - 30, b - 64, a + 30, b], outline=ACENTO, width=2)
d.text((a - 18, b - 82), 'salida', font=fp, fill=TEXTO)
a, b = px(100.5, 5)
d.text((a + 8, b - 74), 'verja', font=fp, fill=TEXTO)

# Cotas
for x in range(0, 126, 5):
    a, b = px(x, Y0)
    d.line([a, b, a, b + 6], fill=TENUE)
    d.text((a - 6, b + 8), str(x), font=fp, fill=TENUE)
for y in (0, 5, 10, 15, 20):
    a, b = px(X0, y)
    d.text((4, b - 7), f'{y}', font=fp, fill=TENUE)
    d.line([a, b, a + 4, b], fill=TENUE)

# Título y leyenda
d.text((MARGEN_IZQ, 14), 'Nivel 1 · Ira · Fortaleza Volcánica — el recorrido montado (vista lateral, 1 u = 1 tile)', font=ft, fill=TEXTO)
d.text((MARGEN_IZQ, 42), 'Cada mecánica aparece primero sin riesgo, luego sobre lava y por último junto a enemigos.', font=f, fill=TENUE)
ly = ALTO - MARGEN_INF + 42
items = [(SUELO, 'basalto'), (LAVA, 'lava (mata)'), (INESTABLE, 'plataforma inestable'), (COMBUSTION, 'plataforma de combustión'),
         (TORRETA, 'torreta'), (ECO, 'E · Eco de la Ira'), (GOLEM, 'G · Golem de Ceniza'), (JEFE, 'C · Centinela Ardiente')]
lx = MARGEN_IZQ
for color, nombre in items:
    d.rectangle([lx, ly, lx + 14, ly + 14], fill=color)
    d.text((lx + 20, ly), nombre, font=f, fill=TEXTO)
    lx += 26 + d.textlength(nombre, font=f) + 18
d.text((MARGEN_IZQ, ly + 30), 'Diálogos: apertura al aparecer (x 2) · eco de memoria en el primer checkpoint (x 43) · presentación del jefe al cruzar la entrada (x 103) · cierre al recoger el fragmento.',
       font=f, fill=TENUE)
d.text((MARGEN_IZQ, ly + 50), 'Alturas: el suelo sube de 0 a 5 hacia la fortaleza y a 7 en la forja; la arena va de 5 a 20 (15 u de alto), como pide el Anexo A. La forja (55-100) se añadió el 2026-10-13.',
       font=f, fill=TENUE)

os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
img.save(SALIDA)
print(SALIDA, img.size)
