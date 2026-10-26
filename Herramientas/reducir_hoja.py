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

--escala E fija la escala cuando no es entera (el daño de Kael llegó a 7,5×: cada píxel de arte mide
7 u 8 píxeles, alternos). Se toma el píxel del centro de cada bloque y se comprueba que, ampliando la
reducción otra vez a E×, sale la imagen exportada píxel a píxel: si no, no se guarda nada.

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
    parser.add_argument("--escala", type=float, default=None, help="la escala de la exportación, si no es entera")
    args = parser.parse_args()

    imagen = np.array(Image.open(args.entrada).convert("RGBA"))
    alto, ancho, _ = imagen.shape
    if ancho % args.fotogramas:
        print(f"El ancho ({ancho}) no se divide en {args.fotogramas} fotogramas iguales.", file=sys.stderr)
        return 1

    ancho_fotograma = ancho // args.fotogramas
    if args.escala:
        n = args.escala
        nativa = reducir_no_entera(imagen, n)
        if nativa is None:
            print(f"A {n}x no sale exacta: la exportación no es una ampliación por vecino más cercano.", file=sys.stderr)
            return 1
    else:
        n = escala_de(imagen, ancho_fotograma)
        nativa = imagen[::n, ::n]

    ancho_nativo = int(round(ancho_fotograma / n))
    conservar = args.conservar or args.fotogramas
    nativa = nativa[:, : conservar * ancho_nativo]

    Image.fromarray(nativa).save(args.salida)
    # Sin "×": la consola de Windows no siempre lo muestra.
    print(f"{args.entrada}: escala {n:g}x, {conservar} fotogramas de "
          f"{ancho_nativo}x{nativa.shape[0]} px -> {args.salida}")
    return 0


def reducir_no_entera(imagen: np.ndarray, escala: float):
    """Reduce una exportación a escala no entera tomando el centro de cada bloque; None si no es exacta."""
    alto, ancho, _ = imagen.shape
    nx, ny = int(round(ancho / escala)), int(round(alto / escala))
    xs = np.floor((np.arange(nx) + 0.5) * escala).astype(int)
    ys = np.floor((np.arange(ny) + 0.5) * escala).astype(int)
    nativa = imagen[ys][:, xs]
    cx = np.minimum((np.arange(ancho) / escala).astype(int), nx - 1)
    cy = np.minimum((np.arange(alto) / escala).astype(int), ny - 1)
    return nativa if (nativa[cy][:, cx] == imagen).all() else None


if __name__ == "__main__":
    sys.exit(main())
