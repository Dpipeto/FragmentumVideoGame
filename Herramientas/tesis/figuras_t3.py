"""Figuras compuestas para TesisIA (tercer concepto) y el GDD, con el estilo de las que ya tiene."""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

AQUI = os.path.dirname(os.path.abspath(__file__))
PROY = r'C:\Users\dpipe\OneDrive\Desktop\Importante\Tesis2.0'
SPR = os.path.join(PROY, 'Assets', 'Sprites')
DIS = os.path.join(PROY, 'Docs', 'Diseno')
CIN = os.path.join(PROY, 'Docs', 'Cinematicas')
CAP = os.path.join(AQUI, 'capturas')
SAL = os.path.join(AQUI, 'fig')
os.makedirs(SAL, exist_ok=True)

FUENTES = r'C:\Windows\Fonts'


def fuente(nombre, tam):
    for n in (nombre, 'arial.ttf'):
        try:
            return ImageFont.truetype(os.path.join(FUENTES, n), tam)
        except OSError:
            continue
    return ImageFont.load_default()


ETIQ = fuente('arialbd.ttf', 34)
TIT = fuente('segoeuib.ttf', 34)
LAB = fuente('segoeui.ttf', 30)
FONDO_HOJA = (46, 49, 56)
NARANJA = (255, 196, 120)


def panel(rutas, salida, columnas=2, ancho=960, alto=540, letras=True):
    """Capturas en rejilla con (a), (b)… encima de cada una, sobre blanco, como la Figura 5."""
    filas = (len(rutas) + columnas - 1) // columnas
    margen, cab = 24, 52
    W = columnas * ancho + (columnas + 1) * margen
    H = filas * (alto + cab) + (filas + 1) * margen // 2
    lienzo = Image.new('RGB', (W, H), (255, 255, 255))
    d = ImageDraw.Draw(lienzo)
    for i, ruta in enumerate(rutas):
        im = Image.open(ruta).convert('RGB').resize((ancho, alto), Image.LANCZOS)
        c, f = i % columnas, i // columnas
        x = margen + c * (ancho + margen)
        y = margen // 2 + f * (alto + cab + margen // 2)
        if letras:
            t = f'({chr(97 + i)})'
            w = d.textlength(t, font=ETIQ)
            d.text((x + (ancho - w) / 2, y + 6), t, fill=(0, 0, 0), font=ETIQ)
        lienzo.paste(im, (x, y + cab))
    lienzo.save(salida, quality=92)
    return salida


def apilar(rutas, salida, ancho=2400):
    """Imágenes una debajo de otra, al mismo ancho, con (a), (b)… a la izquierda, encima."""
    partes = []
    for ruta in rutas:
        im = Image.open(ruta).convert('RGB')
        h = round(im.height * ancho / im.width)
        partes.append(im.resize((ancho, h), Image.LANCZOS))
    cab = 56
    H = sum(p.height + cab for p in partes) + 20
    lienzo = Image.new('RGB', (ancho, H), (255, 255, 255))
    d = ImageDraw.Draw(lienzo)
    y = 0
    for i, p in enumerate(partes):
        d.text((8, y + 10), f'({chr(97 + i)})', fill=(0, 0, 0), font=ETIQ)
        lienzo.paste(p, (0, y + cab))
        y += p.height + cab
    lienzo.save(salida)
    return salida


def recortar(im):
    caja = im.getbbox()
    return im.crop(caja) if caja else im


def hoja(filas, salida, alto_fila=230):
    """
    Una fila por personaje: su título en naranja y sus fotogramas con un rótulo debajo, sobre el
    gris oscuro de la Figura 26. <filas> = [(título, [(ruta, rótulo), …]), …].
    """
    sep, margen = 40, 30
    bloques = []
    for titulo, cuadros in filas:
        ims = [recortar(Image.open(r).convert('RGBA')) for r, _ in cuadros]
        alto_max = max(i.height for i in ims)
        k = alto_fila / alto_max
        ims = [i.resize((max(1, round(i.width * k)), max(1, round(i.height * k))), Image.NEAREST) for i in ims]
        bloques.append((titulo, ims, [rot for _, rot in cuadros]))
    anchos = []
    for titulo, ims, rots in bloques:
        anchos.append(sum(max(i.width, 140) for i in ims) + sep * (len(ims) - 1) + 2 * margen)
    W = max(anchos)
    H = sum(60 + alto_fila + 60 for _ in bloques) + margen
    lienzo = Image.new('RGBA', (W, H), FONDO_HOJA + (255,))
    d = ImageDraw.Draw(lienzo)
    y = margen // 2
    for titulo, ims, rots in bloques:
        d.text((margen, y), titulo, fill=NARANJA, font=TIT)
        y += 56
        x = margen
        for im, rot in zip(ims, rots):
            celda = max(im.width, 140)
            lienzo.alpha_composite(im, (x + (celda - im.width) // 2, y + alto_fila - im.height))
            w = d.textlength(rot, font=LAB)
            d.text((x + (celda - w) / 2, y + alto_fila + 8), rot, fill=(235, 235, 235), font=LAB)
            x += celda + sep
        y += alto_fila + 64
    lienzo.convert('RGB').save(salida)
    return salida


def S(*partes):
    return os.path.join(SPR, *partes) + '.png'


def personajes():
    hoja([
        ('Lamento (el Eco de la Tristeza)', [(S('Enemigos', 'Lamento', 'I1'), 'Quieto'), (S('Enemigos', 'Lamento', 'r2'), 'Andar'),
                                            (S('Enemigos', 'Lamento', 'a1'), 'Anticipa'), (S('Enemigos', 'Lamento', 'a3'), 'Zarpazo'),
                                            (S('Enemigos', 'Lamento', 'd3'), 'Muere')]),
        ('Escultura Viviente y Medusa de Hielo', [(S('Enemigos', 'Escultura', 'E1'), 'Dormida'), (S('Enemigos', 'Escultura', 'P2'), 'Despierta'),
                                                  (S('Enemigos', 'Escultura', 'A2'), 'Alza el golpe'), (S('Enemigos', 'Escultura', 'A4'), 'Golpe'),
                                                  (S('Enemigos', 'Medusa', 'F1'), 'Medusa')]),
        ('La Plañidera (mini-jefa)', [(S('Enemigos', 'Planidera', 'F1'), 'Flota'), (S('Enemigos', 'Planidera', 'E2'), 'Apunta'),
                                      (S('Enemigos', 'Planidera', 'E3'), 'Lanza'), (S('Enemigos', 'Planidera', 'E4'), 'Desarmada'),
                                      (S('Enemigos', 'Planidera', 'M4'), 'Restos')]),
        ('La Doncella del Lamento (jefa)', [(S('Jefes', 'Doncella', 'F1'), 'Flota'), (S('Jefes', 'Doncella', 'O3'), 'Ola'),
                                            (S('Jefes', 'Doncella', 'I3'), 'Inunda'), (S('Jefes', 'Doncella', 'L2'), 'Llora'),
                                            (S('Jefes', 'Doncella', 'M4'), 'Cae')]),
    ], os.path.join(SAL, 'personajes_tristeza.png'))
    hoja([
        ('Espectro (el Eco del Miedo)', [(S('Enemigos', 'Espectro', 'I1'), 'Quieto'), (S('Enemigos', 'Espectro', 'r2'), 'Andar'),
                                         (S('Enemigos', 'Espectro', 'a1'), 'Anticipa'), (S('Enemigos', 'Espectro', 'a3'), 'Zarpazo'),
                                         (S('Enemigos', 'Espectro', 'd2'), 'Se deshace')]),
        ('Centinela Ciego', [(S('Enemigos', 'CentinelaCiego', 'W1'), 'Ronda'), (S('Enemigos', 'CentinelaCiego', 'L2'), 'Escucha'),
                             (S('Enemigos', 'CentinelaCiego', 'A2'), 'Estocada'), (S('Enemigos', 'CentinelaCiego', 'M3'), 'Muere')]),
        ('El Acechador (mini-jefe)', [(S('Enemigos', 'Acechador', 'P1'), 'Merodea'), (S('Enemigos', 'Acechador', 'G2'), 'Se agazapa'),
                                      (S('Enemigos', 'Acechador', 'C1'), 'Embiste'), (S('Enemigos', 'Acechador', 'S1'), 'Aturdido')]),
        ('El Espectro del Pavor (jefe)', [(S('Jefes', 'Pavor', 'I1'), 'Acecha'), (S('Jefes', 'Pavor', 'T1'), 'Aviso'),
                                          (S('Jefes', 'Pavor', 'T2'), 'Tajo'), (S('Jefes', 'Pavor', 'H2'), 'Hojas'),
                                          (S('Jefes', 'Pavor', 'M3'), 'Cae')]),
    ], os.path.join(SAL, 'personajes_miedo.png'))


def esquemas():
    os.makedirs(SAL, exist_ok=True)
    apilar([os.path.join(DIS, 'Nivel2_Diseno.png'), os.path.join(DIS, 'Nivel3_Diseno.png'), os.path.join(DIS, 'Nivel4_Diseno.png')],
           os.path.join(SAL, 'esquemas_234.png'))


def capturas_n3():
    panel([os.path.join(DIS, f'Nivel3_ConArte_{n}.png') for n in ('Trinchera', 'Centinela', 'Cienaga', 'Acechador')],
          os.path.join(SAL, 'nivel3_capturas.jpg'))


def cinematicas():
    panel([os.path.join(CIN, n) for n in ('Doncella_Presentacion_6.5s.png', 'Doncella_Derrota_2s.png', 'Doncella_Derrota_7s.png',
                                          'Pavor_Presentacion_6.5s.png', 'Pavor_Derrota_2s.png', 'Miedo_Salida_5.5s.png')],
          os.path.join(SAL, 'cinematicas_23.jpg'), columnas=3, ancho=640, alto=360)


def capturas_nuevas():
    panel([os.path.join(CAP, n) for n in ('N2_Hielo_Lamento.png', 'N2_Plaza_Planidera.png', 'N2_Calle_Medusas.png', 'N2_Doncella_Llanto.png')],
          os.path.join(SAL, 'nivel2_capturas.jpg'))
    panel([os.path.join(CAP, n) for n in ('Menu_Principal.png', 'Selector.png', 'Pausa.png', 'GameOver.png')],
          os.path.join(SAL, 'navegacion.jpg'))
    panel([os.path.join(CAP, n) for n in ('N1_Hud.png', 'N2_Doncella_Ola.png', 'FinNivel_Registro.png', 'FinNivel_Puntaje.png')],
          os.path.join(SAL, 'interfaces.jpg'))


if __name__ == '__main__':
    que = sys.argv[1:] or ['personajes', 'esquemas', 'capturas_n3', 'cinematicas']
    for q in que:
        globals()[q]()
        print('hecho', q)


def personajes_culpa():
    hoja([
        ('Penitente (el Eco de la Culpa)', [(S('Enemigos', 'Penitente', 'I1'), 'Quieto'), (S('Enemigos', 'Penitente', 'r2'), 'Andar'),
                                           (S('Enemigos', 'Penitente', 'a2'), 'Embiste'), (S('Enemigos', 'Penitente', 'a3'), 'Zarpazo'),
                                           (S('Enemigos', 'Penitente', 'd3'), 'Muere')]),
        ('Artillero', [(S('Enemigos', 'Artillero', 'K1'), 'Reposo'), (S('Enemigos', 'Artillero', 'A1'), 'Apunta'),
                       (S('Enemigos', 'Artillero', 'F1'), 'Dispara'), (S('Enemigos', 'Artillero', 'G2'), 'Culatazo')]),
        ('El Verdugo (mini-jefe)', [(S('Enemigos', 'Verdugo', 'W1'), 'Anda'), (S('Enemigos', 'Verdugo', 'H1'), 'Alza el hacha'),
                                    (S('Enemigos', 'Verdugo', 'H2'), 'Hachazo'), (S('Enemigos', 'Verdugo', 'C2'), 'Cadena'),
                                    (S('Enemigos', 'Verdugo', 'K2'), 'De rodillas')]),
    ], os.path.join(SAL, 'personajes_culpa.png'))
