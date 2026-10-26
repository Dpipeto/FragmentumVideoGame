"""
Esquema del Nivel 4 (Docs/Diseno/Nivel4_Diseno.png): vista lateral a escala, 1 u = 14 px.

Es un dibujo de diseño, no una captura del juego. Las medidas son las de la propuesta
(Docs/Diseno/Nivel4_Diseno.md, sección 3). Cuando se monte, saldrán de
Assets/Scripts/Editor/DisenoNivel4.cs: si cambias una, cambia las otras.

Uso:  python Herramientas/esquema_nivel4.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, '..', 'Docs', 'Diseno', 'Nivel4_Diseno.png')

U = 14                      # px por unidad
X0, X1, Y0, Y1 = -7, 195, -6, 17
MARGEN_IZQ, MARGEN_SUP, MARGEN_INF = 40, 76, 120
ANCHO = (X1 - X0) * U + MARGEN_IZQ + 24
ALTO = (Y1 - Y0) * U + MARGEN_SUP + MARGEN_INF

# Paleta del Anexo A (gris parduzco, marrón quemado, verde oliva muerto, rojo desaturado) y derivados
FONDO = (24, 19, 14)
TIERRA = (60, 46, 30)            # #3C2E1E
BORDE_TIERRA = (110, 84, 52)
ESCOMBRO = (92, 61, 30)          # #5C3D1E
BORDE_ESCOMBRO = (150, 110, 70)
TEJADO = (74, 92, 46)            # #4A5C2E
SACOS = (140, 124, 92)
ROJO = (139, 51, 51)             # #8B3333: lo que hace daño
AMBAR = (224, 176, 96)           # lo que se recuerda: el pecho del Juez
PENITENTE = (150, 130, 96)
ARTILLERO = (190, 160, 120)
VERDUGO = (120, 90, 70)
JEFE = (224, 176, 96)
INVISIBLE = (176, 196, 230)      # la Invisibilidad: cruzar sin que te vean
TORRE = (84, 72, 60)
AGUA = (70, 84, 72, 150)          # el agua parda del río, translúcida
RUINA = (96, 86, 76)             # los restos del puente: no agarran
ALAMBRE = (150, 140, 120)
MINA = (90, 96, 88)
TEXTO = (240, 234, 224)
TENUE = (170, 156, 136)

FONDO_BLOQUES = -6

# Secciones
SECCIONES = ((-7, 66, 'SECCIÓN 1 · El campo arrasado', (34, 28, 22)),
             (66, 110, 'SECCIÓN 2 · Las ruinas del pueblo', (30, 25, 20)),
             (110, 167, 'SECCIÓN 3 · El río y el patio', (34, 27, 22)),
             (167, 195, 'ARENA · La plaza del Juez Silente', (26, 21, 17)))


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


img = Image.new('RGB', (ANCHO, ALTO), FONDO)
d = ImageDraw.Draw(img, 'RGBA')
f, fb, fp, ft = fuente(13), fuente(13, True), fuente(11), fuente(20, True)

for desde, hasta, _, color in SECCIONES:
    rect(d, desde, Y0, hasta, Y1, color)

# --- La propuesta (Nivel4_Diseno.md, sección 3) ---
SUELOS = [(-6, 13, 0), (13, 19, -1.5), (19, 70, 0), (70, 107, -3), (107, 116, 0), (116, 138, -4), (138, 167, 0),
          (167, 194, 0)]
BLOQUES = [(19.5, 23, 0, 2),                          # el tanque volcado
           (67, 69, 0, 2),                            # los escombros para subir a los tejados
           (104, 107, -3, -1.5)]                      # el escalón para salir de la calle
PAREDES = [(-7, -6, FONDO_BLOQUES, 17),               # ParedIzquierda
           (193, 194, 0, 17),                         # ParedArena
           (167, 194, 15, 17)]                        # TechoArena
TEJADOS = [(69, 79.5, 4.5), (82, 92.5, 4.5), (95.5, 107, 4.5)]
COBERTURAS = [(32.5, 0), (36, 0), (39.5, 0),           # el campo: la primera cobertura
              (85.5, 4.5), (89, 4.5),                 # las chimeneas del tejado del medio
              (174, 0), (187, 0)]                     # la plaza: contra las placas del Juez
SACOS_ARTILLERO = [(42.3, 0), (91.1, 4.5)]
BARRILES = [(27, 0), (98.5, 4.5), (100.5, 4.5), (102.5, 4.5), (104.5, 4.5),
            (148, 0), (152, 0), (156, 0), (160, 0)]

for x0, x1, y in SUELOS:
    rect(d, x0, FONDO_BLOQUES, x1, y, TIERRA, BORDE_TIERRA)
for x0, x1, y0, y1 in BLOQUES + PAREDES:
    rect(d, x0, y0, x1, y1, TIERRA, BORDE_TIERRA)
rect(d, 19.5, 0, 23, 2, ESCOMBRO, BORDE_ESCOMBRO, 2)  # el tanque, que es cobertura y plataforma
for x0, x1, y in TEJADOS:
    rect(d, x0, y - 0.5, x1, y, TEJADO, (120, 140, 80))
    for x in range(int(x0) + 1, int(x1), 3):           # los muros que lo sostienen, de decorado
        rect(d, x, -3 if x > 70 else 0, x + 0.25, y - 0.5, (60, 50, 40, 90))
for x, y in COBERTURAS:
    rect(d, x - 0.75, y, x + 0.75, y + 1.3, ESCOMBRO, BORDE_ESCOMBRO)
for x, y in SACOS_ARTILLERO:
    rect(d, x - 0.5, y, x + 0.5, y + 1.3, SACOS, (180, 164, 130))
for x, y in BARRILES:
    a, b = px(x, y)
    d.rectangle([a - 6, b - 14, a + 6, b], fill=ROJO, outline=(200, 120, 90))
    d.line([a - 6, b - 7, a + 6, b - 7], fill=(60, 20, 20))

# El río: se cruza por debajo, por los dos ojos del puente volado
rect(d, 116, -4, 138, -0.1, AGUA)
a, b = px(116, -0.1)
c, _ = px(138, -0.1)
d.line([a, b, c, b], fill=(150, 160, 130), width=2)
for x0, x1, techo in ((118, 124, -1.5), (126.5, 133, -0.5)):
    rect(d, x0, techo, x1, 7, RUINA, (140, 128, 112), 2)
rect(d, 124.5, -4, 126, 0.25, RUINA, (140, 128, 112), 2)          # el pilar: se respira
for x0, x1, techo in ((120, 122.5, -1.5), (130, 132.5, -0.5)):    # las alambradas sumergidas
    a, b = px(x0, techo)
    c, e = px(x1, -4)
    d.rectangle([a, b, c, e], outline=ALAMBRE, width=1)
    for i in range(0, int(c - a), 6):
        d.line([a + i, b, a + i + 6, e], fill=ALAMBRE, width=1)
        d.line([a + i + 6, b, a + i, e], fill=ALAMBRE, width=1)
a, b = px(128.5, -2.5)                                             # la mina, que sube y baja
d.line([a, b, a, px(0, -4)[1]], fill=(110, 110, 100), width=1)
d.ellipse([a - 6, b - 6, a + 6, b + 6], fill=MINA, outline=ROJO, width=2)
d.text((a + 9, b - 16), '↕', font=fb, fill=TEXTO)
a, b = px(118.2, 6.2)
d.text((a, b), 'restos del puente (no agarran)', font=fp, fill=TENUE)
a, b = px(114.5, 0)
d.ellipse([a - 7, b - 50, a + 7, b - 36], outline=TEXTO, width=2)

# La tierra de nadie: los sacos del refugio son la última cobertura; después, 13 u al descubierto
rect(d, 48, 0, 49.5, 1.3, SACOS, (180, 164, 130))
for x in (50.5, 53, 55.5, 58, 60.5):                  # alambradas caídas: decorado
    a, b = px(x, 0)
    d.line([a - 8, b - 1, a + 8, b - 9], fill=(120, 110, 96), width=1)
    d.line([a - 8, b - 9, a + 8, b - 1], fill=(120, 110, 96), width=1)
a, b = px(49.5, -0.9)
c, _ = px(62.5, -0.9)
d.line([a, b, c, b], fill=INVISIBLE, width=2)
d.line([a, b - 6, a, b + 6], fill=INVISIBLE, width=2)
d.line([c, b - 6, c, b + 6], fill=INVISIBLE, width=2)
d.text((a + 6, b + 6), '13 u al descubierto: corriendo, 2,2 s', font=fp, fill=INVISIBLE)
d.text((a + 6, b + 20), 'Q · Invisibilidad: 3 s sin que te vean', font=fp, fill=INVISIBLE)

# El campanario: se pasa por debajo, por el arco; en su balcón, dos Artilleros que se turnan
rect(d, 62.5, 2.5, 66, 4.5, TORRE, (130, 112, 92), 2)            # el suelo del balcón
rect(d, 62.5, 0, 62.9, 2.5, (84, 72, 60, 120))                  # el arco, de decorado
rect(d, 65.6, 0, 66, 2.5, (84, 72, 60, 120))
rect(d, 62.5, 4.5, 63, 5.3, SACOS, (180, 164, 130))              # el parapeto del balcón
rect(d, 64.6, 7, 66, 13, (84, 72, 60, 150), (130, 112, 92))      # lo alto, roto y sin campana
rect(d, 63.2, 9.5, 64.6, 13, (84, 72, 60, 150), (130, 112, 92))
a, b = px(62.4, 14.4)
d.text((a - 30, b), 'el campanario, sin campana', font=fp, fill=TENUE)

# Las cadenas de barriles: un barril enciende a los que tiene a menos de 3 u
for x0, x1, y in ((98.5, 100.5, 4.5), (100.5, 102.5, 4.5), (102.5, 104.5, 4.5), (104.5, 106.2, 4.5)):
    a, b = px(x0, y + 1.6)
    c, e = px(x1, y + 1.6)
    d.line([a, b, c, e], fill=(220, 110, 80, 200), width=2)

# Las líneas de tiro de los Artilleros: apuntan a la cabeza de Kael
for xa, ya, hasta, hy in ((43.5, 0, 31, 1.55), (92, 4.5, 82.5, 6.05), (106.2, 4.5, 96, 6.05),
                          (63.3, 4.5, 50.5, 1.55), (65, 4.5, 56, 1.55)):
    a, b = px(xa - 0.4, ya + 1.5)
    c, e = px(hasta, hy)
    largo = abs(c - a)
    for i in range(0, int(largo), 10):
        d.line([a - i, b + (e - b) * i / largo, a - i - 5, b + (e - b) * (i + 5) / largo], fill=(200, 80, 80, 170))


def punto(x, y, color, letra, r=7):
    a, b = px(x, y)
    d.ellipse([a - r, b - 2 * r, a + r, b], fill=color)
    w = d.textlength(letra, font=fp)
    d.text((a - w / 2, b - 2 * r + 1), letra, font=fp, fill=(20, 14, 10))


def memoria(x, y, numero):
    a, b = px(x, y)
    d.polygon([(a, b - 30), (a + 9, b - 19), (a, b - 8), (a - 9, b - 19)], fill=AMBAR, outline=(255, 230, 170))
    w = d.textlength(str(numero), font=fb)
    d.text((a - w / 2, b - 27), str(numero), font=fb, fill=(40, 26, 10))


for x, y, n in ((16, -1.5, 1), (73, -3, 2), (89, -3, 3), (142, 0, 4)):
    memoria(x, y, n)
for x, y in ((10, 0), (28.5, 0), (30, 0), (78, -3), (81.5, -3), (94, -3), (97.5, -3), (101, -3),
             (99.5, 4.5), (101.5, 4.5), (103.5, 4.5)):
    punto(x, y, PENITENTE, 'P')
for x, y in ((43.5, 0), (63.3, 4.5), (65, 4.5), (92, 4.5), (106.2, 4.5)):
    punto(x, y, ARTILLERO, 'A', r=8)
punto(154, 0, VERDUGO, 'V', r=14)
punto(180.5, 0, JEFE, 'J', r=17)

# Las cuatro marcas de la plaza, una por memoria
for x, n in ((171, 1), (177.5, 2), (183.5, 3), (190, 4)):
    a, b = px(x, 0)
    d.rectangle([a - 12, b - 4, a + 12, b + 2], fill=AMBAR)
    d.text((a - 4, b + 4), str(n), font=fb, fill=AMBAR)
a, b = px(170.5, 9.6)
d.text((a, b), 'al fondo, el Juez proyecta la memoria de su ataque', font=fp, fill=TEXTO)
a, b = px(170.5, 8.6)
d.text((a, b), 'E sobre su marca, a la vista: el Juez se arrodilla (3 s)', font=fp, fill=TEXTO)

# Inicio, checkpoints, salida, verja
a, b = px(2, 0)
d.rectangle([a - 4, b - 22, a + 4, b], fill=(220, 214, 200))
d.text((a - 14, b - 40), 'Kael', font=fb, fill=TEXTO)
for x, y, nombre in ((46, 0, 'CP1 · eco de memoria'), (110.5, 0, 'CP2'), (164.5, 0, 'CP3')):
    a, b = px(x, y)
    d.line([a, b, a, b - 30], fill=(200, 196, 186), width=3)
    d.polygon([(a, b - 30), (a + 12, b - 25), (a, b - 20)], fill=ROJO)
    d.text((a - 10, b - 46), nombre, font=fp, fill=TEXTO)
a, b = px(190.5, 0)
d.rectangle([a - 26, b - 56, a + 26, b], outline=JEFE, width=2)
d.text((a - 16, b - 72), 'salida', font=fp, fill=TEXTO)
a, b = px(168.3, 5.4)
d.text((a, b), '← verja', font=fp, fill=TEXTO)

# Rótulos de tramo
for x, y, texto in ((-5.5, 3.2, 'inicio'), (12.6, -3.6, 'el cráter: memoria sin riesgo'), (18.4, 2.8, 'tanque volcado'),
                    (23.6, 4.8, 'pista: Ataque Cargado'), (31, 3.2, 'la primera cobertura'),
                    (44.6, 6.6, 'pista: Invisibilidad'), (49.6, 8.4, 'la tierra de nadie'),
                    (66.2, 9.4, 'la bifurcación'), (70.5, 7.4, 'arriba: los tejados (Artilleros y barriles)'),
                    (71.5, -5.3, 'abajo: la calle (Penitentes y dos memorias)'), (96, 7.4, 'cadena de barriles'),
                    (107.6, 4.6, 'convergen'), (113.8, 8.6, 'el río: por los ojos del puente volado'),
                    (116.2, -5.3, 'alambradas: solo el dash · el pilar: se respira · la mina sube y baja'),
                    (113.2, 1.6, 'línea'), (140, 3.4, 'la carta'), (145, 4.4, 'el patio del Verdugo'),
                    (160.6, 4.4, 'antesala')):
    a, b = px(x, y)
    d.text((a, b), texto, font=fp, fill=TENUE)

# Cotas
for x in range(-5, 195, 5):
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
d.text((MARGEN_IZQ, 14), 'Nivel 4 · Culpa · Campos de Batalla Arrasados — propuesta (vista lateral, 1 u = 1 tile)', font=ft, fill=TEXTO)
d.text((MARGEN_IZQ, 44), 'Cubrirse agachado tras los escombros; donde no hay escombros, que no te vean (la Invisibilidad). Los barriles estallan con el Ataque Cargado. El río se cruza por debajo, con el Dash Acuático. Mirar cada memoria cuesta; no mirar ninguna cuesta más; y para mirar hay que dejarse ver.',
       font=f, fill=TENUE)
ly = ALTO - MARGEN_INF + 44
items = [(TIERRA, 'tierra quemada'), (TEJADO, 'tejado'), (ESCOMBRO, 'cobertura (1,3 u)'), (SACOS, 'sacos'),
         (TORRE, 'el campanario'), (AGUA[:3], 'agua (10 s de aire)'), (ALAMBRE, 'alambrada sumergida'), (MINA, 'mina'), (ROJO, 'barril explosivo'), (AMBAR, 'memoria (1-4)'), (INVISIBLE, 'Invisibilidad (Q, 3 s)'),
         (PENITENTE, 'P · Penitente (Eco de la Culpa)'), (ARTILLERO, 'A · Artillero'), (VERDUGO, 'V · el Verdugo (mini-jefe)'),
         (JEFE, 'J · Juez Silente')]
lx, fila = MARGEN_IZQ, 0
for color, nombre in items:
    ancho_item = 26 + d.textlength(nombre, font=f) + 18
    if lx + ancho_item > ANCHO - 20:
        lx, fila = MARGEN_IZQ, fila + 1
    d.rectangle([lx, ly + fila * 22, lx + 14, ly + fila * 22 + 14], fill=color)
    d.text((lx + 20, ly + fila * 22), nombre, font=f, fill=TEXTO)
    lx += ancho_item
ly += (fila + 1) * 22 + 8
d.text((MARGEN_IZQ, ly), 'Rayas rojas: la línea de tiro de cada Artillero, a la cabeza de Kael; los dos del campanario se turnan y no se alcanzan. Diálogos: apertura al aparecer · pista ante el primer barril · eco de memoria en el CP1 · pista ante la tierra de nadie · presentación del Juez al cruzar la verja · cierre al recoger el fragmento.',
       font=f, fill=TENUE)
d.text((MARGEN_IZQ, ly + 20), 'Medidas de Kael: sube 2,4 u (4,1 con doble salto); de pie mide 1,8 u y agachado 1,08 u (avanza a 2 u/s). La cobertura mide 1,3 u: solo agachado detrás de ella no le alcanzan. Invisible, ni lo ven ni puede recordar. Los checkpoints curan.',
       font=f, fill=TENUE)

os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
img.save(SALIDA)
print(SALIDA, img.size)
