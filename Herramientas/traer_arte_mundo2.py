"""Trae al proyecto las imágenes limpias del Mundo 2 (Tristeza).

Copia, byte a byte, los PNG de `Tesis/Imagenes/limpios/Mundo2` (el repositorio de al lado, que
tiene su LEEME) a su carpeta de `Assets`. No reescala, no recorta y no cambia nombres: lo único
que decide es en qué carpeta va cada grupo. Los ajustes de importación (píxeles por unidad y
pivote) los pone Unity al importarlos, según `Assets/Scripts/Editor/TablaArteMundo2.cs`.

Uso, desde la raíz del proyecto:

    python Herramientas/traer_arte_mundo2.py            # copia lo que falte o haya cambiado
    python Herramientas/traer_arte_mundo2.py --comprobar # solo dice qué cambiaría

Después, en Unity: Fragmentum → Niveles → Generar el arte del Mundo 2.
"""
import filecmp
import shutil
import sys
from pathlib import Path

PROYECTO = Path(__file__).resolve().parent.parent
ORIGEN = PROYECTO.parent / "Tesis" / "Imagenes" / "limpios" / "Mundo2"

# Carpeta del set limpio -> carpeta del proyecto. Todos los PNG de la carpeta van juntos.
DESTINOS = {
    "Lamento": "Assets/Sprites/Enemigos/Lamento",
    "Escultura": "Assets/Sprites/Enemigos/Escultura",
    "Medusa": "Assets/Sprites/Enemigos/Medusa",
    "Planidera": "Assets/Sprites/Enemigos/Planidera",
    "Doncella": "Assets/Sprites/Jefes/Doncella",
    "Fragmento": "Assets/Sprites/Jefes/Doncella",
    "Ola": "Assets/Sprites/Efectos/Mundo2/Ola",
    "Puas": "Assets/Sprites/Efectos/Mundo2/Puas",
    "Capa": "Assets/Sprites/Efectos/Mundo2/Capa",
    "Inundacion": "Assets/Sprites/Efectos/Mundo2/Inundacion",
    "Agua": "Assets/Sprites/Mecanicas/Mundo2/Agua",
    "Burbujas": "Assets/Sprites/Mecanicas/Mundo2/Burbujas",
    "Placa": "Assets/Sprites/Mecanicas/Mundo2/Placa",
    "Hielo": "Assets/Sprites/Mecanicas/Mundo2/Hielo",
    "Oxigeno": "Assets/Sprites/Interfaz/Oxigeno",
    "Fondos": "Assets/Sprites/Fondos/Mundo2Tristeza",
    "Cinematicas": "Assets/Sprites/Cinematicas/Nivel2",
    "TileMap": "Assets/TileMap/Mundo2",
    "Props": "Assets/TileMap/Mundo2",
}


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

    sobran = [d for d in ORIGEN.iterdir() if d.is_dir() and not d.name.startswith("_") and d.name not in DESTINOS]
    for carpeta in sobran:
        print(f"  sin destino: {carpeta.name}")

    verbo = "se copiarían" if comprobar else "copiados"
    print(f"{nuevos} nuevos y {cambiados} cambiados {verbo}; {iguales} ya estaban al día.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
