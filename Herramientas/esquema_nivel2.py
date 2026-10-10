"""
Esquema del Nivel 2 (Docs/Diseno/Nivel2_Diseno.png): vista lateral a escala, 1 u = 14 px.

Es un dibujo de diseño, no una captura del juego. Las medidas son las del recorrido montado
(Assets/Scripts/Editor/DisenoNivel2.cs y Docs/Diseno/Nivel2_Diseno.md): si cambias una, cambia
las otras. Lo que todavía no está en la escena (el hielo rompible, las placas que flotan, las
Esculturas, las Medusas y la inundación de la Doncella) se dibuja igual, porque es el diseño.

Uso:  python Herramientas/esquema_nivel2.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, '..', 'Docs', 'Diseno', 'Nivel2_Diseno.png')

U = 14                      # px por unidad
X0, X1, Y0, Y1 = -7, 97, -13, 17
MARGEN_IZQ, MARGEN_SUP, MARGEN_INF = 40, 76, 170
ANCHO = (X1 - X0) * U + MARGEN_IZQ + 24
ALTO = (Y1 - Y0) * U + MARGEN_SUP + MARGEN_INF

# Paleta del Anexo A (azul profundo, azul hielo, gris azulado, blanco nieve) y sus derivados
FONDO = (12, 20, 32)
PIEDRA = (44, 62, 80)            # #2C3E50
BORDE_PIEDRA = (84, 104, 124)
HIELO = (232, 238, 244)          # #E8EEF4, remate de la superficie que resbala
AGUA = (30, 58, 95)              # #1E3A5F
AGUA_BORDE = (74, 127, 165)      # #4A7FA5
AIRE = (40, 52, 46)
FLOTANTE = (150, 200, 230)
ROMPIBLE = (255, 120, 50)        # el naranja de la Ira: lo único cálido del mundo
LAMENTO = (74, 127, 165)
ESCULTURA = (150, 160, 170)
PLANIDERA = (61, 90, 138)        # el tinte de sus datos
MEDUSA = (90, 220, 240)
JEFE = (120, 200, 255)
TEXTO = (236, 240, 246)
TENUE = (140, 156, 176)
ACENTO = (0, 212, 255)           # LevelData.acento del L2

FONDO_BLOQUES = -14              # DisenoNivel2.Fondo


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


def discontinuo(d, x0, y0, x1, y1, color, paso=6):
    a, b = px(x0, y1)
    c, e = px(x1, y0)
    for x in range(int(a), int(c), paso * 2):
        d.line([x, b, min(x + paso, c), b], fill=color)
        d.line([x, e, min(x + paso, c), e], fill=color)
    for y in range(int(b), int(e), paso * 2):
        d.line([a, y, a, min(y + paso, e)], fill=color)
        d.line([c, y, c, min(y + paso, e)], fill=color)


img = Image.new('RGB', (ANCHO, ALTO), FONDO)
d = ImageDraw.Draw(img, 'RGBA')
f, fb, fp, ft = fuente(13), fuente(13, True), fuente(11), fuente(20, True)

# Secciones
SECCIONES = ((-7, 36, 'SECCIÓN 1 · La superficie helada', (20, 30, 44)),
             (36, 61, 'SECCIÓN 2 · Hundirse', (14, 26, 46)),
             (61, 97, 'ARENA · La Doncella del Lamento', (22, 30, 50)))
for desde, hasta, _, color in SECCIONES:
    rect(d, desde, Y0, hasta, Y1, color)

# --- Lo mismo que DisenoNivel2 ---
BAJO = 0.1                       # DisenoNivel2.BajoLaSuperficie
AGUAS = [(13, 21, -5, 0 - BAJO),         # AguaDeLasPlacas: caer no mata
         (36, 46, -11, -1),              # AguaDeLaCalle
         (46, 53, -11, -7.5 - BAJO),     # AguaBajoLaCampana
         (53, 54, -11, -4),              # AguaAntesDeLaTorre
         (54, 60, -11, 1.25 - BAJO)]     # AguaDeLaTorre
BOLSAS = [(47, 52, -7.5 - BAJO, -1),     # BolsaDeLaCampana
          (54, 60, 1.25 - BAJO, 7.5)]    # Campanario
SUELOS = [(-6, 5, 0), (5, 13, 0), (13, 21, -5), (21, 36, 0), (36, 60, -11), (61, 69, 0), (69, 96, 0)]
BLOQUES = [(20, 21, -5, -3),          # EscalonDeLasPlacas
           (25, 30, 0, 2),            # Puente
           (49, 51, -8, -7.5),        # RepisaDeLaCampana
           (57, 60, -7.25, -6.75),    # Rellano1
           (54, 57, -3, -2.5),        # Rellano2
           (57, 60, 0.75, 1.25),      # Rellano3
           (80, 85, 6.5, 7)]          # Balcon
PAREDES = [(-7, -6, FONDO_BLOQUES, 17),   # ParedIzquierda
           (38, 53, -1, 0),               # TechoDeHielo
           (38, 40, 0, 9),                # Muralla
           (46, 47, -8.5, -1),            # CampanaOeste
           (52, 53, -8.5, -1),            # CampanaEste
           (53, 54, -4, 7.5),             # TorreOeste
           (60, 61, FONDO_BLOQUES, 1.25), # TorreEsteBaja
           (60, 61, 4.5, 7.5),            # TorreEsteAlta
           (53, 61, 7.5, 8.5),            # TejadoDeLaTorre
           (95, 96, 0, 17),               # ParedArena
           (69, 96, 15, 17)]              # TechoArena
HIELOS = [(5, 13), (21, 25), (30, 36), (70, 95)]
PLACAS = [(15.25, 0.5), (18.75, 0.5), (76, 3.5), (89, 3.5)]   # centro de su oscilación

for x0, x1, y0, y1 in AGUAS:
    rect(d, x0, y0, x1, y1, AGUA)
    a, b = px(x0, y1)
    c, _ = px(x1, y1)
    d.line([a, b, c, b], fill=AGUA_BORDE, width=2)
for x0, x1, y0, y1 in BOLSAS:
    rect(d, x0, y0, x1, y1, AIRE)
    discontinuo(d, x0, y0, x1, y1, (170, 210, 170))

for x0, x1, y in SUELOS:
    rect(d, x0, FONDO_BLOQUES, x1, y, PIEDRA, BORDE_PIEDRA)
for x0, x1, y0, y1 in BLOQUES + PAREDES:
    rect(d, x0, y0, x1, y1, PIEDRA, BORDE_PIEDRA)
for x0, x1 in HIELOS:
    rect(d, x0, -0.35, x1, 0, HIELO)

# Hielo rompible: un tapón que llena el agujero y asoma 1,25 u; solo cede al Ataque Cargado
rect(d, 36, -1, 38, 1.25, (60, 40, 30), ROMPIBLE, 2)
a, b = px(26.6, -1.4)
d.text((a, b), 'hielo rompible (Ataque Cargado) →', font=fp, fill=ROMPIBLE)


def flotante(x, y, ancho=2.5, amplitud=0.6):
    rect(d, x - ancho / 2, y - 0.5, x + ancho / 2, y, FLOTANTE, HIELO)
    a, b = px(x, y + amplitud + 0.9)
    d.text((a - 4, b), '↕', font=fb, fill=TEXTO)


for x, y in PLACAS:
    flotante(x, y, amplitud=0.5)

# Inundación de la arena (fase D): tres franjas, sube hasta y 4
for i, (x0, x1) in enumerate(((70, 78.33), (78.33, 86.67), (86.67, 95))):
    discontinuo(d, x0, 0, x1, 4, (120, 180, 230), paso=4)
    a, b = px(x0 + 0.4, 3.8)
    d.text((a, b), 'ABC'[i], font=fb, fill=(120, 180, 230))
a, b = px(70.5, -1.2)
d.text((a, b), 'franjas de inundación A, B y C: el agua sube hasta y 4 (rayado)', font=fp, fill=(120, 180, 230))


def punto(x, y, color, letra, r=7):
    a, b = px(x, y)
    d.ellipse([a - r, b - 2 * r, a + r, b], fill=color)
    w = d.textlength(letra, font=fp)
    d.text((a - w / 2, b - 2 * r + 1), letra, font=fp, fill=(10, 16, 24))


for x, y in ((9, 0), (27.5, 2), (34.5, 0)):
    punto(x, y, LAMENTO, 'L')
punto(23.5, 0, ESCULTURA, 'S', r=9)
# El mini-jefe, donde estaba la segunda Escultura: guarda el tapón de hielo.
punto(32, 0, PLANIDERA, 'P', r=13)
for x, y, flecha in ((40.5, -8.5, '↕'), (43.5, -6.5, '↕'), (57, -4.75, '↔'), (57, -0.7, '↔')):
    punto(x, y, MEDUSA, 'M')
    a, b = px(x + 0.7, y + 0.9)
    d.text((a, b), flecha, font=fb, fill=TEXTO)
punto(82.5, 9, JEFE, 'D', r=16)
a, b = px(82.5, 13.4)
d.text((a - 46, b), 'flota; baja a llorar', font=fp, fill=TEXTO)

# Inicio, checkpoints, salida, puerta
a, b = px(2, 0)
d.rectangle([a - 4, b - 22, a + 4, b], fill=HIELO)
d.text((a - 14, b - 40), 'Kael', font=fb, fill=TEXTO)
for x, y, nombre in ((50, -7.5, 'CP1 · la silla vacía'), (65, 0, 'CP2')):
    a, b = px(x, y)
    d.line([a, b, a, b - 30], fill=(200, 200, 205), width=3)
    d.polygon([(a, b - 30), (a + 12, b - 25), (a, b - 20)], fill=ACENTO)
    if x < 60:
        a2, b2 = px(47.3, -9.3)
        d.text((a2, b2), nombre, font=fp, fill=TEXTO)
    else:
        d.text((a - 10, b - 46), nombre, font=fp, fill=TEXTO)
a, b = px(93, 0)
d.rectangle([a - 10, b - 80, a + 10, b], outline=ACENTO, width=2)
d.text((a - 16, b - 96), 'salida', font=fp, fill=TEXTO)
a, b = px(66, 5.4)
d.text((a, b), 'puerta →', font=fp, fill=TEXTO)
a, b = px(40.3, 1.4)
d.text((a, b), 'techo de hielo: no se pisa', font=fp, fill=TENUE)

# Rótulos de tramo
for x, y, texto in ((-5.5, 3.2, 'piedra: no resbala'), (6, 2.0, 'primer hielo'), (13.2, 2.6, 'placas flotantes'),
                    (13.2, -6.6, 'caer al agua no mata'), (25, 4.0, 'puente de piedra'), (38.2, 10.6, 'muralla'),
                    (36.5, -2.6, 'la calle hundida'), (46.3, 0.2, 'la campana'), (53.6, 10, 'la torre del reloj'),
                    (54.3, 5.6, 'campanario (aire)'), (54.2, -10, 'escalones de 4,25 u'), (61.5, 2.4, 'antesala')):
    a, b = px(x, y)
    d.text((a, b), texto, font=fp, fill=TENUE)

# Cotas
for x in range(-5, 96, 5):
    a, b = px(x, Y0)
    d.line([a, b, a, b + 6], fill=TENUE)
    d.text((a - 6, b + 8), str(x), font=fp, fill=TENUE)
for y in (-10, -5, 0, 5, 10, 15):
    a, b = px(X0, y)
    d.text((4, b - 7), f'{y}', font=fp, fill=TENUE)
    d.line([a, b, a + 4, b], fill=TENUE)

for x, _, nombre, _ in SECCIONES:
    a, b = px(x, Y1)
    d.rectangle([a + 2, b + 2, a + 12 + d.textlength(nombre, font=fb), b + 22], fill=FONDO)
    d.text((a + 6, b + 4), nombre, font=fb, fill=TEXTO)

# Título y leyenda
d.text((MARGEN_IZQ, 14), 'Nivel 2 · Tristeza · Ciudad Sumergida — el recorrido (vista lateral, 1 u = 1 tile)', font=ft, fill=TEXTO)
d.text((MARGEN_IZQ, 44), 'Arriba todo resbala y nada agarra; la única salida es hundirse. Bajo el agua todo pesa, pero Kael salta más alto.',
       font=f, fill=TENUE)
ly = ALTO - MARGEN_INF + 44
items = [(PIEDRA, 'piedra'), (HIELO, 'hielo (resbala)'), (AGUA, 'agua (lenta, oxígeno)'), (AIRE, 'bolsa de aire'),
         (FLOTANTE, 'placa flotante'), (ROMPIBLE, 'hielo rompible'), (LAMENTO, 'L · Lamento'),
         (ESCULTURA, 'S · Escultura Viviente'), (PLANIDERA, 'P · Plañidera (mini-jefe)'),
         (MEDUSA, 'M · Medusa de Hielo'), (JEFE, 'D · Doncella del Lamento')]
lx, fila = MARGEN_IZQ, 0
for color, nombre in items:
    ancho_item = 26 + d.textlength(nombre, font=f) + 18
    if lx + ancho_item > ANCHO - 20:
        lx, fila = MARGEN_IZQ, fila + 1
    d.rectangle([lx, ly + fila * 22, lx + 14, ly + fila * 22 + 14], fill=color)
    d.text((lx + 20, ly + fila * 22), nombre, font=f, fill=TEXTO)
    lx += ancho_item
ly += (fila + 1) * 22 + 8
d.text((MARGEN_IZQ, ly), 'Diálogos: apertura al aparecer (x 2) · eco de memoria en CP1, junto a la silla vacía · presentación de la Doncella al cruzar la entrada (x 72) · cierre al recoger el fragmento.',
       font=f, fill=TENUE)
d.text((MARGEN_IZQ, ly + 20), 'Medidas de Kael: en seco sube 2,4 u (4,1 con doble salto); bajo el agua, 2,9 u (5,0). Los dos primeros escalones de la torre (4,25 u) solo se suben bajo el agua.',
       font=f, fill=TENUE)

os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
img.save(SALIDA)
print(SALIDA, img.size)
