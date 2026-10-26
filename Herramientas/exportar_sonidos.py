"""
Pasa los efectos de sonido al juego: cada proyecto de LMMS de Sonidos/ (.mmp o .mmpz), a un WAV mono en
Assets/Audio/Sonidos, en su misma carpeta (Sonidos/Kael/Salto.mmp -> Assets/Audio/Sonidos/Kael/Salto.wav).

    python Herramientas/exportar_sonidos.py            # todos
    python Herramientas/exportar_sonidos.py Salto      # uno (el nombre del archivo, sin extensión)

Cómo se hace:
  1. LMMS lo toca desde el principio hasta la marca de fin de la barra de arriba (un compás).
  2. Se corta donde se apaga (a SILENCIO_DB de su pico), con un fundido de FUNDIDO al final.
  3. En mono (la media de los dos lados) y a 44,1 kHz: como pide PENDIENTES, «SFX: WAV mono, 44,1 kHz, menos
     de 2 s».
  4. Se normaliza: lo más fuerte (en 50 ms) a NIVEL_DB, sin pasar de TECHO_DB de pico. Los que suenan más o
     menos que los demás lo dicen en NIVELES (un salto, que se oye a cada rato, más bajo que un golpe).
"""
import os
import sys
import tempfile

import numpy as np
import scipy.io.wavfile as wav

import musica_lmms as m
from exportar_musica import FRECUENCIA, cargar, comprobar_audio, db, guardar_xml, leer_wav

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ORIGEN = os.path.join(RAIZ, 'Sonidos')
DESTINO = os.path.join(RAIZ, 'Assets', 'Audio', 'Sonidos')
NIVEL_DB = -12.0      # lo más fuerte del efecto, en 50 ms
NIVELES = {'Salto': -17.0, 'DobleSalto': -17.0, 'Dash': -14.0, 'Ataque': -14.0, 'AtaqueCargado': -12.0,
           'Danio': -11.0}
TECHO_DB = -1.0       # el pico más alto
SILENCIO_DB = -60.0   # por debajo de esto (bajo su pico), ya se apagó
FUNDIDO = int(0.01 * FRECUENCIA)
VENTANA = int(0.05 * FRECUENCIA)
MAXIMO = 2.0          # segundos


def destino(ruta):
    relativa = os.path.relpath(os.path.dirname(ruta), ORIGEN)
    carpeta = os.path.join(DESTINO, relativa)
    os.makedirs(carpeta, exist_ok=True)
    return os.path.join(carpeta, os.path.splitext(os.path.basename(ruta))[0] + '.wav')


def lo_mas_fuerte(audio):
    """El RMS más alto del efecto, en ventanas de 50 ms."""
    energia = np.convolve(audio ** 2, np.ones(VENTANA) / VENTANA, mode='same')
    return np.sqrt(energia.max())


def exportar(ruta):
    nombre = os.path.splitext(os.path.basename(ruta))[0]
    raiz = cargar(ruta)
    with tempfile.TemporaryDirectory() as tmp:
        proyecto = os.path.join(tmp, 'efecto.mmp')
        guardar_xml(raiz, proyecto)
        m.renderizar(proyecto, os.path.join(tmp, 'efecto.wav'))
        d = leer_wav(os.path.join(tmp, 'efecto.wav'))
    comprobar_audio(d, nombre)
    audio = d.mean(axis=1)
    pico = np.abs(audio).max()
    if pico < 1e-4:
        raise RuntimeError(f'{nombre}: no suena')

    # Donde se apaga: la última muestra por encima del silencio, y un poco más para el fundido.
    suena = np.nonzero(np.abs(audio) > pico * 10 ** (SILENCIO_DB / 20))[0]
    inicio, fin = suena[0], min(len(audio), suena[-1] + FUNDIDO)
    audio = audio[inicio:fin].copy()
    audio[-FUNDIDO:] *= np.cos(np.linspace(0.0, np.pi / 2, FUNDIDO)) ** 2
    if len(audio) >= MAXIMO * FRECUENCIA:
        raise RuntimeError(f'{nombre}: dura {len(audio) / FRECUENCIA:.2f} s; un efecto tiene que durar menos de {MAXIMO:g} s')

    ganancia = min(10 ** ((NIVELES.get(nombre, NIVEL_DB) - db(lo_mas_fuerte(audio))) / 20),
                   10 ** (TECHO_DB / 20) / np.abs(audio).max())
    audio *= ganancia
    salida = destino(ruta)
    wav.write(salida, FRECUENCIA, np.round(np.clip(audio, -1.0, 1.0) * 32767).astype(np.int16))
    print(f'{nombre}: {len(audio) / FRECUENCIA:.3f} s, lo más fuerte {db(lo_mas_fuerte(audio)):.1f} dB, '
          f'pico {db(np.abs(audio).max()):.1f} dB' + (f', empieza {inicio / FRECUENCIA * 1000:.1f} ms tarde' if inicio else ''))
    return salida


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
