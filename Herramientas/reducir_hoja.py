"""Reduce una hoja de sprites exportada a escala N× a su resolución nativa (1 px de arte = 1 px).

Pixelorama puede exportar a 5× o 10×, pero Unity necesita la hoja a 1×: el arte de Kael se
importa a 64 px por unidad, y una tira de 7 fotogramas de 1900 px supera el máximo de textura
de Unity (8192 px), que la reduciría con filtro y emborronaría el pixel art.

La escala se detecta sola: es el mayor N que divide el fotograma en bloques de N×N píxeles
idénticos. La reducción es exacta (se toma un píxel de cada bloque), así que no se pierde nada.

Uso:
    python reducir_hoja.py entrada.png FOTOGRAMAS salida.png [--conservar N]

FOTOGRAMAS es cuántos fotogramas tiene la tira exportada (en horizontal). --conservar deja solo
los N primeros: sirve para quitar la vuelta de una exportación en ping-pong.

Requiere Pillow y numpy (pip install pillow numpy).
"""

import argparse
import sys
from math import gcd

import numpy as np
from PIL import Image


def escala_de(imagen: np.ndarray, ancho_fotograma: int) -> int:
    alto = imagen.shape[0]
    maximo = gcd(ancho_fotograma, alto)
    for n in range(maximo, 0, -1):
        if maximo % n:
            continue
        bloques = imagen.reshape(alto // n, n, imagen.shape[1] // n, n, 4)
        if (bloques == bloques[:, :1, :, :1, :]).all():
            return n
    return 1


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("entrada")
    parser.add_argument("fotogramas", type=int)
    parser.add_argument("salida")
    parser.add_argument("--conservar", type=int, default=None, help="deja solo los N primeros fotogramas")
    args = parser.parse_args()

    imagen = np.array(Image.open(args.entrada).convert("RGBA"))
    alto, ancho, _ = imagen.shape
    if ancho % args.fotogramas:
        print(f"El ancho ({ancho}) no se divide en {args.fotogramas} fotogramas iguales.", file=sys.stderr)
        return 1

    ancho_fotograma = ancho // args.fotogramas
    n = escala_de(imagen, ancho_fotograma)
    nativa = imagen[::n, ::n]

    conservar = args.conservar or args.fotogramas
    nativa = nativa[:, : conservar * (ancho_fotograma // n)]

    Image.fromarray(nativa).save(args.salida)
    # Sin "×": la consola de Windows no siempre lo muestra.
    print(f"{args.entrada}: escala {n}x, {conservar} fotogramas de "
          f"{ancho_fotograma // n}x{alto // n} px -> {args.salida}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
