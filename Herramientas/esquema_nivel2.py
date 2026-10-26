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
X0, X1, Y0, Y1 = -7, 140, -13, 17
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
SECCIONES = ((-7, 46, 'SECCIÓN 1 · La superficie helada', (20, 30, 44)),
             (46, 74, 'SECCIÓN 2 · Hundirse', (14, 26, 46)),
             (74, 104, 'SECCIÓN 3 · El canal', (18, 32, 48)),
             (104, 140, 'ARENA · La Doncella del Lamento', (22, 30, 50)))
for desde, hasta, _, color in SECCIONES:
    rect(d, desde, Y0, hasta, Y1, color)

# --- Lo mismo que DisenoNivel2 ---
BAJO = 0.1                       # DisenoNivel2.BajoLaSuperficie
AGUAS = [(13, 21, -5, 0 - BAJO),         # AguaDeLasPlacas: caer no mata
         (46, 56, -11, -1),              # AguaDeLaCalle
         (56, 66, -11, -7.5 - BAJO),     # AguaBajoLaCampana
         (66, 67, -11, -4),              # AguaAntesDeLaTorre
         (67, 73, -11, 1.25 - BAJO),     # AguaDeLaTorre
         (80, 92, -5, 0 - BAJO)]         # AguaDelCanal: caer no mata
BOLSAS = [(57, 65, -7.5 - BAJO, -1),     # BolsaDeLaCampana
          (67, 73, 1.25 - BAJO, 7.5)]    # Campanario
SUELOS = [(-6, 5, 0), (5, 13, 0), (13, 21, -5), (21, 46, 0), (46, 73, -11), (74, 80, 0), (80, 92, -5), (92, 104, 0),
          (104, 112, 0), (112, 139, 0)]
BLOQUES = [(20, 21, -5, -3),          # EscalonDeLasPlacas
           (25, 30, 0, 2),            # Puente
           (59, 63, -8, -7.5),        # RepisaDeLaCampana
           (70, 73, -7.25, -6.75),    # Rellano1
           (67, 70, -3, -2.5),        # Rellano2
           (70, 73, 0.75, 1.25),      # Rellano3
           (91, 92, -5, -3),          # EscalonDelCanal
           (123, 128, 6.5, 7)]        # Balcon
MURALLA = (48, 50, 0, 9)                  # de hielo liso: no agarra (DisenoNivel2.NoAgarra)
PAREDES = [(-7, -6, FONDO_BLOQUES, 17),   # ParedIzquierda
           (48, 66, -1, 0),               # TechoDeHielo
           (56, 57, -8.5, -1),            # CampanaOeste
           (65, 66, -8.5, -1),            # CampanaEste
           (66, 67, -4, 7.5),             # TorreOeste
           (73, 74, FONDO_BLOQUES, 1.25), # TorreEsteBaja
           (73, 74, 4.5, 7.5),            # TorreEsteAlta
           (66, 74, 7.5, 8.5),            # TejadoDeLaTorre
           (138, 139, 0, 17),             # ParedArena
           (112, 139, 15, 17)]            # TechoArena
HIELOS = [(5, 13), (21, 25), (30, 46), (92, 104), (113, 138)]
PLACAS = [(15.25, 0.5), (18.75, 0.5), (119, 3.5), (132, 3.5)]   # centro de su oscilación
PLACAS_CANAL = [(82, 1), (86, 1), (90, 1)]                        # suben 2 u, a la contra

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

# La muralla, de hielo liso: no se sube con el salto de pared
rect(d, MURALLA[0], MURALLA[2], MURALLA[1], MURALLA[3], FLOTANTE, HIELO, 2)

# Hielo rompible: un tapón que llena el agujero y asoma 1,25 u; solo cede al Ataque Cargado
rect(d, 46, -1, 48, 1.25, (60, 40, 30), ROMPIBLE, 2)
a, b = px(36.6, -1.4)
d.text((a, b), 'hielo rompible (Ataque Cargado) →', font=fp, fill=ROMPIBLE)

# La pista: delante del tapón, Kael dice cómo se rompe (DisenoNivel2.PistaDelTapon)
discontinuo(d, 42, 0, 46, 3, ROMPIBLE, paso=4)
a, b = px(40.2, 4.4)
d.text((a, b), 'pista de Kael', font=fp, fill=ROMPIBLE)


def flotante(x, y, ancho=2.5, amplitud=0.6):
    rect(d, x - ancho / 2, y - 0.5, x + ancho / 2, y, FLOTANTE, HIELO)
    a, b = px(x, y + amplitud + 0.9)
    d.text((a - 4, b), '↕', font=fb, fill=TEXTO)


for x, y in PLACAS:
    flotante(x, y, amplitud=0.5)
for x, y in PLACAS_CANAL:
    flotante(x, y, amplitud=1.0)

# Inundación de la arena (fase D): tres franjas (DisenoNivel2.Franjas), sube hasta y 3,5
for i, (x0, x1) in enumerate(((113, 121), (121, 130), (130, 138))):
    discontinuo(d, x0, 0, x1, 3.5, (120, 180, 230), paso=4)
    a, b = px(x0 + 0.4, 3.3)
    d.text((a, b), 'ABC'[i], font=fb, fill=(120, 180, 230))
a, b = px(113.5, -1.2)
d.text((a, b), 'franjas A, B y C: agua hasta y 3,5 (rayado); congelada, se pisa', font=fp, fill=(120, 180, 230))


def punto(x, y, color, letra, r=7):
    a, b = px(x, y)
    d.ellipse([a - r, b - 2 * r, a + r, b], fill=color)
    w = d.textlength(letra, font=fp)
    d.text((a - w / 2, b - 2 * r + 1), letra, font=fp, fill=(10, 16, 24))


for x, y in ((9, 0), (27.5, 2), (44.5, 0), (77.5, 0), (99.3, 0)):
    punto(x, y, LAMENTO, 'L')
for x in (23.5, 96.5, 102.4):
    punto(x, 0, ESCULTURA, 'S', r=9)
for x in (93.6, 99.5):                       # estatuas de piedra (atrezo): no despiertan
    a, b = px(x, 0)
    d.rectangle([a - 5, b - 22, a + 5, b], outline=ESCULTURA, width=1)
# El mini-jefe, en mitad de la plaza este (16 u): guarda el tapón de hielo.
punto(38, 0, PLANIDERA, 'P', r=13)
for x, y, flecha in ((50.5, -8.5, '↕'), (53.5, -6.5, '↕'), (70, -4.75, '↔'), (70, -0.7, '↔'), (84, -3.2, '↕'), (88, -2.2, '↕')):
    punto(x, y, MEDUSA, 'M')
    a, b = px(x + 0.7, y + 0.9)
    d.text((a, b), flecha, font=fb, fill=TEXTO)
# Flota con los pies a y 3: entera bajo el balcón y dentro de lo que enseña la cámara.
punto(125.5, 3, JEFE, 'D', r=16)
a, b = px(125.5, 10.4)
d.text((a - 76, b), 'flota a y 3; baja a llorar', font=fp, fill=TEXTO)

# Inicio, checkpoints, salida, puerta
a, b = px(2, 0)
d.rectangle([a - 4, b - 22, a + 4, b], fill=HIELO)
d.text((a - 14, b - 40), 'Kael', font=fb, fill=TEXTO)
for x, y, nombre in ((62.4, -7.5, 'CP1 · la silla vacía y la mesa'), (108, 0, 'CP2')):
    a, b = px(x, y)
    d.line([a, b, a, b - 30], fill=(200, 200, 205), width=3)
    d.polygon([(a, b - 30), (a + 12, b - 25), (a, b - 20)], fill=ACENTO)
    if x < 70:
        a2, b2 = px(57.3, -9.3)
        d.text((a2, b2), nombre, font=fp, fill=TEXTO)
    else:
        d.text((a - 10, b - 46), nombre, font=fp, fill=TEXTO)
a, b = px(136, 0)
d.rectangle([a - 26, b - 56, a + 26, b], outline=ACENTO, width=2)
d.text((a - 16, b - 72), 'salida', font=fp, fill=TEXTO)
a, b = px(109.5, 5.4)
d.text((a, b), 'verja →', font=fp, fill=TEXTO)
a, b = px(50.6, 1.4)
d.text((a, b), 'techo de hielo: no se pisa', font=fp, fill=TENUE)

# Rótulos de tramo
for x, y, texto in ((-5.5, 3.2, 'piedra: no resbala'), (6, 2.0, 'primer hielo'), (13.2, 2.6, 'placas flotantes'),
                    (13.2, -6.6, 'caer al agua no mata'), (25, 4.0, 'puente de piedra'), (31, 6.4, 'plaza este: 16 u'),
                    (44.2, 10.6, 'muralla de hielo: no agarra'),
                    (46.5, -2.6, 'la calle hundida'), (57.3, 0.2, 'la campana'), (66.6, 10, 'la torre del reloj'),
                    (67.3, 5.6, 'campanario (aire)'), (67.2, -10, 'escalones de 4,25 u'), (74.3, 2.4, 'azotea'),
                    (80.3, 3.6, 'el canal: placas que suben 2 u'), (92.3, 3.4, 'patio de las estatuas (hielo)'),
                    (104.5, 2.4, 'antesala')):
    a, b = px(x, y)
    d.text((a, b), texto, font=fp, fill=TENUE)

# Cotas
for x in range(-5, 140, 5):
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
d.text((MARGEN_IZQ, ly), 'Diálogos: apertura al aparecer (x 2) · la pista ante el tapón (x 42 a 46) · eco de memoria en CP1, junto a la silla vacía · presentación de la Doncella al cruzar la entrada (x 115) · cierre al recoger el fragmento.',
       font=f, fill=TENUE)
d.text((MARGEN_IZQ, ly + 20), 'Medidas de Kael: en seco sube 2,4 u (4,1 con doble salto); bajo el agua, 2,9 u (5,0). Los dos primeros escalones de la torre (4,25 u) solo se suben bajo el agua. Aire: 10 s. Los checkpoints curan.',
       font=f, fill=TENUE)

os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
img.save(SALIDA)
print(SALIDA, img.size)
