"""
Pasa la música al juego: cada proyecto de LMMS de Musica/ (.mmp o .mmpz), a un OGG en
Assets/Audio/Musica. Hay dos clases:
  * Los bucles (la barra de arriba de LMMS encendida): la música de un nivel o de un jefe, que se repite
    sin costura.
  * Las piezas que suenan una vez (la barra apagada): la música de una cinemática o un golpe. Dura justo
    hasta la marca de fin, que es lo que dura su cinemática, y se apaga en sus últimos FUNDIDO_PIEZA.
Las de Musica/Cinematicas y Musica/Golpes van a esas carpetas de Assets/Audio/Musica; las demás, a ella.

    python Herramientas/exportar_musica.py                 # todos
    python Herramientas/exportar_musica.py Nivel4_Juez     # uno (el nombre del archivo, sin extensión)

Cómo se hace un bucle:
  1. El bucle va de la marca de inicio a la de fin de LMMS (la barra de arriba). Si no hay marcas, es
     el proyecto entero, redondeado a compases. Las notas que pasen del final suenan al principio, como
     en el juego.
  2. LMMS lo toca dos veces seguidas y se guarda la segunda vuelta: así su principio lleva la cola de
     la reverb y de las notas del final, como cuando el juego lo repite. Su último segundo se funde
     con el de la primera vuelta, que es el que de verdad va antes de ese principio (la reverb de
     LMMS no suena igual dos veces).
  3. Se normaliza: todos los temas suenan igual de fuerte (RMS de NIVEL_DB, sin pasar de TECHO_DB), salvo
     los que piden otra cosa en NIVELES (el Miedo es casi silencio).
  4. LMMS lo pasa a OGG (44,1 kHz, estéreo). Su codificador tira el último bloque si queda a medias:
     se codifica con un poco de más y se corta el OGG en su sitio (recortar_ogg).
  5. Se comprueba, leyéndolo otra vez con LMMS, que dura el bucle justo y que es lo que se exportó.

Lo que no es exacto: para codificarlo, LMMS lo toca como una muestra, y su reproductor lo adelanta media
muestra (un filtro sinc). Es igual en todo el bucle, así que la costura sigue siendo continua; solo las
diez primeras muestras (0,2 ms) no ven lo que sonaba antes, un error de unos -35 dB que cae en el
golpe con que empieza el compás 1.
"""
import copy
import os
import struct
import sys
import tempfile
import xml.etree.ElementTree as ET

import numpy as np
import scipy.io.wavfile as wav

import musica_lmms as m

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ORIGEN = os.path.join(RAIZ, 'Musica')
DESTINO = os.path.join(RAIZ, 'Assets', 'Audio', 'Musica')
FRECUENCIA = 44100
NIVEL_DB = -18.0   # RMS de todo un bucle
NIVEL_PIEZA_DB = -20.0  # RMS de una pieza que suena una vez (una cinemática)
NIVELES = {'Nivel3_Miedo': -30.0}  # los que suenan a otro nivel: el Miedo, «casi silencio» (Anexo A)
TECHO_DB = -1.0    # el pico más alto
RELLENO = 4096     # lo que se codifica de más tras el bucle (como mínimo), y luego se corta
DESFASE = 1        # muestras que adelanta LMMS un audio al codificarlo y volver a leerlo
FUNDIDO = FRECUENCIA  # el último segundo pasa al final de la primera vuelta (la reverb no se repite)
TCOS = ('pattern', 'sampletco', 'bbtco', 'automationpattern')
FUNDIDO_PIEZA = int(0.3 * FRECUENCIA)  # una pieza se apaga en sus últimos 0,3 s
CARPETAS_PROPIAS = ('Cinematicas', 'Golpes')


def cargar(ruta):
    if ruta.endswith('.mmpz'):
        return ET.fromstring(m.lmms('dump', ruta).stdout)
    return ET.parse(ruta).getroot()


def _pistas(raiz):
    return raiz.find('song').find('trackcontainer').findall('track')


def ticks_compas(raiz):
    return m.TICKS_NEGRA * int(raiz.find('head').get('timesig_numerator', 4))


def es_bucle(raiz):
    marcas = raiz.find('song').find('timeline')
    return marcas is None or marcas.get('lpstate', '1') == '1'


def largo_en_ticks(raiz):
    """Lo que mide: hasta la marca de fin de LMMS (con la barra encendida o no); si no hay marcas, hasta
    donde llega el último patrón, redondeado al compás."""
    compas = ticks_compas(raiz)
    marcas = raiz.find('song').find('timeline')
    if marcas is not None:
        inicio, fin = int(marcas.get('lp0pos', 0)), int(marcas.get('lp1pos', 0))
        if inicio != 0:
            raise RuntimeError('Tiene que empezar en el compás 1 (la marca de inicio está en el tick '
                               f'{inicio})')
        if fin > 0:
            return fin
    fin = 0
    for pista in _pistas(raiz):
        for tco in pista:
            if tco.tag not in TCOS:
                continue
            largo = int(tco.get('len', 0))
            notas = [int(n.get('pos')) + int(n.get('len')) for n in tco.findall('note')]
            if notas:
                largo = max(largo, -(-max(notas) // compas) * compas)
            fin = max(fin, int(tco.get('pos', 0)) + largo)
    return -(-fin // compas) * compas


def dos_vueltas(raiz, largo):
    doble = copy.deepcopy(raiz)
    for pista in _pistas(doble):
        for tco in [t for t in pista if t.tag in TCOS]:
            otra = copy.deepcopy(tco)
            otra.set('pos', str(int(tco.get('pos', 0)) + largo))
            pista.append(otra)
    return doble


def guardar_xml(raiz, ruta):
    with open(ruta, 'w', encoding='utf-8', newline='\n') as f:
        f.write('<?xml version="1.0"?>\n<!DOCTYPE lmms-project>\n')
        f.write(ET.tostring(raiz, encoding='unicode'))


def leer_wav(ruta):
    r, d = wav.read(ruta)
    if r != FRECUENCIA:
        raise RuntimeError(f'{ruta}: {r} Hz, se esperaban {FRECUENCIA}')
    if d.dtype.kind in 'iu':
        d = d.astype(np.float64) / float(np.iinfo(d.dtype).max + 1)
    d = d.astype(np.float64)
    return d if d.ndim == 2 else np.stack([d, d], axis=1)


def comprobar_audio(d, nombre):
    """LMMS, a veces, deja muestras rotas (infinitas o enormes) al empezar a exportar una pista sola: si
    pasa con un tema, se para aquí en vez de llevarlo al juego."""
    malas = np.nonzero(~np.isfinite(d).all(axis=1) | (np.abs(d).max(axis=1) > 16))[0]
    if len(malas):
        raise RuntimeError(f'{nombre}: LMMS exportó {len(malas)} muestras rotas (la primera, a los '
                           f'{malas[0] / FRECUENCIA:.3f} s): vuelve a intentarlo')


def proyecto_de_audio(archivo, bpm, largo):
    """Un proyecto que solo toca un archivo de audio, tal cual: sirve para codificarlo y para leerlo. Mide
    compases enteros (por arriba): LMMS corta en el último compás entero una pista de audio que no los mida."""
    largo = -(-largo // m.TICKS_COMPAS) * m.TICKS_COMPAS
    p = m.Proyecto(bpm, largo // m.TICKS_COMPAS)
    raiz = p.xml()
    tc = raiz.find('song').find('trackcontainer')
    pista = ET.SubElement(tc, 'track', type='2', name='Audio', muted='0', solo='0')
    ET.SubElement(pista, 'sampletrack', vol='100', pan='0', fxch='0')
    ET.SubElement(pista, 'sampletco', pos='0', len=str(largo), src=archivo.replace('\\', '/'), muted='0')
    return raiz


def db(x):
    return 20 * np.log10(max(x, 1e-12))


# --- El final exacto del OGG --------------------------------------------------------------------

def _crc_ogg(datos):
    """El CRC de una página Ogg (polinomio 0x04C11DB7, sin reflejar, desde 0)."""
    crc = 0
    for byte in datos:
        crc ^= byte << 24
        for _ in range(8):
            crc = ((crc << 1) ^ 0x04C11DB7) if crc & 0x80000000 else crc << 1
            crc &= 0xFFFFFFFF
    return crc


def _paginas_ogg(datos):
    """(inicio, fin, posición de la muestra) de cada página."""
    paginas, i = [], 0
    while i < len(datos):
        if datos[i:i + 4] != b'OggS':
            raise RuntimeError(f'OGG mal formado en el byte {i}')
        segmentos = datos[26 + i]
        cuerpo = sum(datos[i + 27:i + 27 + segmentos])
        fin = i + 27 + segmentos + cuerpo
        paginas.append((i, fin, struct.unpack_from('<q', datos, i + 6)[0]))
        i = fin
    return paginas


def recortar_ogg(ruta, muestras):
    """
    Deja el OGG en «muestras» exactas: termina en la primera página que llega hasta ahí, marcada como la
    última, con su posición de muestra en «muestras». Vorbis lo prevé así (el decodificador descarta lo
    que sobra de esa página): es como se cortan los bucles sin hueco.
    """
    datos = bytearray(open(ruta, 'rb').read())
    for inicio, fin, posicion in _paginas_ogg(datos):
        if posicion >= muestras:
            pagina = datos[inicio:fin]
            struct.pack_into('<q', pagina, 6, muestras)
            pagina[5] |= 0x04                      # fin del flujo
            struct.pack_into('<I', pagina, 22, 0)
            struct.pack_into('<I', pagina, 22, _crc_ogg(pagina))
            with open(ruta, 'wb') as f:
                f.write(bytes(datos[:inicio]) + bytes(pagina))
            return
    raise RuntimeError(f'{ruta}: el OGG no llega a las {muestras} muestras')


def destino(ruta):
    carpeta = os.path.basename(os.path.dirname(ruta))
    salida = os.path.join(DESTINO, carpeta) if carpeta in CARPETAS_PROPIAS else DESTINO
    os.makedirs(salida, exist_ok=True)
    return os.path.join(salida, os.path.splitext(os.path.basename(ruta))[0] + '.ogg')


def normalizar(audio, nivel_db):
    rms = np.sqrt(np.mean(audio ** 2))
    return min(10 ** ((nivel_db - db(rms)) / 20), 10 ** (TECHO_DB / 20) / np.abs(audio).max())


def codificar(audio, continuacion, ogg, bpm, largo, muestras, tmp):
    """
    LMMS lo pasa a OGG. Su codificador tira el último bloque si queda a medias: se codifica con lo que
    sigue detrás (la continuación) y luego se corta en su sitio (recortar_ogg). Se comprueba, leyéndolo
    otra vez con LMMS, que dura lo justo y que es lo que se exportó. Devuelve el parecido.
    """
    sobra = -(-(muestras + RELLENO) // RELLENO) * RELLENO - muestras
    con_relleno = np.concatenate([audio, continuacion[:sobra]])
    limpio = os.path.join(tmp, 'limpio.wav')
    wav.write(limpio, FRECUENCIA, np.round(con_relleno * 32767).astype(np.int16))
    proyecto = os.path.join(tmp, 'codificar.mmp')
    largo_relleno = int(np.ceil(len(con_relleno) / (muestras / largo))) + m.TICKS_COMPAS
    guardar_xml(proyecto_de_audio(limpio, bpm, largo_relleno), proyecto)
    m.renderizar(proyecto, ogg, formato='ogg')
    recortar_ogg(ogg, muestras)

    # Tiene que durar lo justo (lo dice su última página) y ser lo que se exportó. Al leerlo, LMMS se
    # deja el último bloque a medias: el parecido se mide hasta ahí.
    final = _paginas_ogg(open(ogg, 'rb').read())[-1][2]
    if final != muestras:
        raise RuntimeError(f'{ogg}: dura {final} muestras, no {muestras}')
    leer = os.path.join(tmp, 'leer.mmp')
    guardar_xml(proyecto_de_audio(ogg, bpm, largo), leer)
    m.renderizar(leer, os.path.join(tmp, 'leido.wav'))
    leido = leer_wav(os.path.join(tmp, 'leido.wav'))
    if len(leido) < muestras - 256:
        raise RuntimeError(f'{ogg}: LMMS solo lee {len(leido)} muestras, de {muestras}')
    # El reproductor de muestras de LMMS adelanta el audio media muestra, al codificarlo y otra vez al
    # leerlo: se compara con una muestra de desfase.
    n = min(len(leido), muestras) - 256
    parecido = min(np.corrcoef(leido[:n - DESFASE, c], audio[DESFASE:n, c])[0, 1] for c in (0, 1))
    ventana = min(44100, n // 2)
    final_parecido = np.corrcoef(leido[n - ventana:n - DESFASE, 0], audio[n - ventana + DESFASE:n, 0])[0, 1]
    if parecido < 0.995 or final_parecido < 0.99:
        raise RuntimeError(f'{ogg}: no se parece a lo exportado (correlación {parecido:.4f}, al final '
                           f'{final_parecido:.4f})')
    return parecido


def exportar(ruta):
    nombre = os.path.splitext(os.path.basename(ruta))[0]
    raiz = cargar(ruta)
    bpm = float(raiz.find('head').get('bpm'))
    largo = largo_en_ticks(raiz)
    muestras = largo * FRECUENCIA * 60.0 / (bpm * m.TICKS_NEGRA)
    if abs(muestras - round(muestras)) > 1e-6:
        raise RuntimeError(f'{nombre}: no cae en una muestra entera ({muestras})')
    muestras = int(round(muestras))
    if not es_bucle(raiz):
        return exportar_pieza(nombre, ruta, raiz, bpm, largo, muestras)

    with tempfile.TemporaryDirectory() as tmp:
        doble = os.path.join(tmp, 'doble.mmp')
        guardar_xml(dos_vueltas(raiz, largo), doble)
        m.renderizar(doble, os.path.join(tmp, 'doble.wav'))
        d = leer_wav(os.path.join(tmp, 'doble.wav'))
        comprobar_audio(d, nombre)
        if len(d) < 2 * muestras - 2:
            raise RuntimeError(f'{nombre}: LMMS exportó {len(d)} muestras, se esperaban al menos {2 * muestras}')
        bucle = d[muestras:2 * muestras].copy()
        if len(bucle) < muestras:
            bucle = np.concatenate([bucle, np.zeros((muestras - len(bucle), 2))])
        # La reverb de LMMS (ReverbSC) modula sus retardos al azar: su cola no sale igual en las dos
        # vueltas, y el final de la segunda no enlaza con la cola que lleva el principio. El último
        # FUNDIDO pasa, poco a poco, al final de la primera vuelta, que es lo que va antes de ese
        # principio. Lo seco es igual en las dos: solo se mezclan las colas.
        t = np.linspace(0.0, 1.0, FUNDIDO)[:, None]
        bucle[-FUNDIDO:] = (1 - t) * bucle[-FUNDIDO:] + t * d[muestras - FUNDIDO:muestras]
        # El salto del final al principio, y el mismo paso entre las dos vueltas (que es el natural).
        natural = np.abs(d[muestras] - d[muestras - 1]).max()

        ganancia = normalizar(bucle, NIVELES.get(nombre, NIVEL_DB))
        bucle = bucle * ganancia
        # Detrás del bucle va su principio: es lo que suena después en el juego.
        ogg = destino(ruta)
        parecido = codificar(bucle, bucle, ogg, bpm, largo, muestras, tmp)

    salto = np.abs(bucle[0] - bucle[-1]).max()
    natural *= ganancia
    if salto > 1.5 * natural + 0.01:
        raise RuntimeError(f'{nombre}: la costura salta {salto:.4f}, y entre las dos vueltas, {natural:.4f}')
    print(f'{nombre}: {muestras / FRECUENCIA:.2f} s ({largo // ticks_compas(raiz)} compases a {bpm:g} BPM), '
          f'RMS {db(np.sqrt(np.mean(bucle ** 2))):.1f} dB, pico {db(np.abs(bucle).max()):.1f} dB, '
          f'costura {salto:.4f} (entre las dos vueltas, {natural:.4f}), '
          f'OGG {os.path.getsize(ogg) / 1e6:.2f} MB, se parece {parecido:.4f}')
    return ogg


def exportar_pieza(nombre, ruta, raiz, bpm, largo, muestras):
    """Una pieza que suena una vez: hasta la marca de fin, apagándose al final."""
    with tempfile.TemporaryDirectory() as tmp:
        proyecto = os.path.join(tmp, 'pieza.mmp')
        guardar_xml(raiz, proyecto)
        m.renderizar(proyecto, os.path.join(tmp, 'pieza.wav'))
        d = leer_wav(os.path.join(tmp, 'pieza.wav'))
        comprobar_audio(d, nombre)
        pieza = d[:muestras].copy()
        if len(pieza) < muestras:
            pieza = np.concatenate([pieza, np.zeros((muestras - len(pieza), 2))])
        pieza[-FUNDIDO_PIEZA:] *= np.cos(np.linspace(0.0, np.pi / 2, FUNDIDO_PIEZA))[:, None] ** 2
        ganancia = normalizar(pieza, NIVELES.get(nombre, NIVEL_PIEZA_DB))
        pieza = pieza * ganancia
        ogg = destino(ruta)
        parecido = codificar(pieza, np.zeros_like(pieza), ogg, bpm, largo, muestras, tmp)

    print(f'{nombre}: {muestras / FRECUENCIA:.2f} s, una vez, RMS {db(np.sqrt(np.mean(pieza ** 2))):.1f} dB, '
          f'pico {db(np.abs(pieza).max()):.1f} dB, OGG {os.path.getsize(ogg) / 1e6:.2f} MB, se parece {parecido:.4f}')
    return ogg


def proyectos(nombres):
    for carpeta, _, archivos in sorted(os.walk(ORIGEN)):
        for archivo in sorted(archivos):
            base, ext = os.path.splitext(archivo)
            if ext in ('.mmp', '.mmpz') and (not nombres or base in nombres):
                yield os.path.join(carpeta, archivo)


if __name__ == '__main__':
    nombres = set(sys.argv[1:])
    rutas = list(proyectos(nombres))
    if not rutas:
        sys.exit(f'No hay proyectos de LMMS en {ORIGEN}' + (f' con el nombre {", ".join(sorted(nombres))}' if nombres else ''))
    for ruta in rutas:
        exportar(ruta)
