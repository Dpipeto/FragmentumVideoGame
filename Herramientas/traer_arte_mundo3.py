"""Trae al proyecto las imágenes limpias del Mundo 3 (Miedo).

Copia, byte a byte, los PNG de `Tesis/Imagenes/limpios/Mundo3` (el repositorio de al lado, que
tiene su LEEME) a su carpeta de `Assets`. No reescala, no recorta y no cambia nombres: lo único
que decide es en qué carpeta va cada grupo. Los ajustes de importación (píxeles por unidad y
pivote) los pone Unity al importarlos, según `Assets/Scripts/Editor/ArteMundo3.cs`.

Uso, desde la raíz del proyecto:

    python Herramientas/traer_arte_mundo3.py            # copia lo que falte o haya cambiado
    python Herramientas/traer_arte_mundo3.py --comprobar # solo dice qué cambiaría

Después, en Unity: Fragmentum → Niveles → Generar el arte del Mundo 3.
"""
import filecmp
import shutil
import sys
from pathlib import Path

PROYECTO = Path(__file__).resolve().parent.parent
ORIGEN = PROYECTO.parent / "Tesis" / "Imagenes" / "limpios" / "Mundo3"

# Carpeta del set limpio -> carpeta del proyecto. Todos los PNG de la carpeta van juntos.
DESTINOS = {
    "Espectro": "Assets/Sprites/Enemigos/Espectro",
    "Centinela": "Assets/Sprites/Enemigos/CentinelaCiego",
    "Acechador": "Assets/Sprites/Enemigos/Acechador",
    "Pavor": "Assets/Sprites/Jefes/Pavor",
    "Fragmento": "Assets/Sprites/Jefes/Pavor",
    "Hojas": "Assets/Sprites/Efectos/Mundo3/Hojas",
    "Luciernaga": "Assets/Sprites/Efectos/Mundo3/Luciernaga",
    "Antorcha": "Assets/Sprites/Mecanicas/Mundo3/Antorcha",
    "Cepo": "Assets/Sprites/Mecanicas/Mundo3/Cepo",
    "Rama": "Assets/Sprites/Mecanicas/Mundo3/Rama",
    "Cinta": "Assets/Sprites/Mecanicas/Mundo3/Cinta",
    # La ciénaga (2026-10-19).
    "Agua": "Assets/Sprites/Mecanicas/Mundo3/Agua",
    "Marana": "Assets/Sprites/Mecanicas/Mundo3/Marana",
    "Fondos": "Assets/Sprites/Fondos/Mundo3Miedo",
    "TileMap": "Assets/TileMap/Mundo3",
    "Props": "Assets/TileMap/Mundo3",
    # Los planos del Pavor y los de la salida (estos los trae también traer_arte_puertas.py).
    "Cinematicas": "Assets/Sprites/Cinematicas/Nivel3",
}

# Las trae traer_arte_puertas.py, con las de los otros mundos.
DE_OTRO = {"Puerta", "Verja", "Checkpoint"}


def main():
    comprobar = "--comprobar" in sys.argv[1:]
    if not ORIGEN.is_dir():
        print(f"No encuentro {ORIGEN}")
        return 1

    nuevos = cambiados = iguales = 0
    for grupo, destino in DESTINOS.items():
        carpeta = ORIGEN / grupo
        if not carpeta.is_dir():
            print(f"  falta la carpeta {grupo}")
            continue

        salida = PROYECTO / destino
        for png in sorted(carpeta.glob("*.png")):
            copia = salida / png.name
            if copia.exists() and filecmp.cmp(png, copia, shallow=False):
                iguales += 1
                continue

            print(f"  {'nuevo  ' if not copia.exists() else 'cambia '} {destino}/{png.name}")
            if copia.exists():
                cambiados += 1
            else:
                nuevos += 1

            if not comprobar:
                salida.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(png, copia)

    sobran = [d for d in ORIGEN.iterdir() if d.is_dir() and not d.name.startswith("_") and d.name not in DESTINOS and d.name not in DE_OTRO]
    for carpeta in sobran:
        print(f"  sin destino: {carpeta.name}")

    verbo = "se copiarían" if comprobar else "copiados"
    print(f"{nuevos} nuevos y {cambiados} cambiados {verbo}; {iguales} ya estaban al día.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
