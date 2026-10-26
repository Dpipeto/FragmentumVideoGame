"""Trae al proyecto las imágenes limpias del Mundo 4 (Culpa).

Copia, byte a byte, los PNG de `Tesis/Imagenes/limpios/Mundo4` (el repositorio de al lado, que
tiene su LEEME) a su carpeta de `Assets`. No reescala, no recorta y no cambia nombres: lo único
que decide es en qué carpeta va cada grupo. Los ajustes de importación (píxeles por unidad y
pivote) los pone Unity al importarlos, según `Assets/Scripts/Editor/ArteMundo4.cs`.

Uso, desde la raíz del proyecto:

    python Herramientas/traer_arte_mundo4.py            # copia lo que falte o haya cambiado
    python Herramientas/traer_arte_mundo4.py --comprobar # solo dice qué cambiaría

Después, en Unity: Fragmentum → Niveles → Generar el arte del Mundo 4.
"""
import filecmp
import shutil
import sys
from pathlib import Path

PROYECTO = Path(__file__).resolve().parent.parent
ORIGEN = PROYECTO.parent / "Tesis" / "Imagenes" / "limpios" / "Mundo4"

# Carpeta del set limpio -> carpeta del proyecto. Todos los PNG de la carpeta van juntos.
DESTINOS = {
    "Penitente": "Assets/Sprites/Enemigos/Penitente",
    "Artillero": "Assets/Sprites/Enemigos/Artillero",
    "Verdugo": "Assets/Sprites/Enemigos/Verdugo",
    "Juez": "Assets/Sprites/Jefes/Juez",
    # El Fragmento de la Culpa va con el Juez: flota sobre el montón de su M5.
    "Fragmento": "Assets/Sprites/Jefes/Juez",
    # Lo que lanzan y lo que dejan (Prompts_Mundo4.md, apartado C).
    "Explosion": "Assets/Sprites/Efectos/Mundo4/Explosion",
    "Fuego": "Assets/Sprites/Efectos/Mundo4/Fuego",
    "Placa": "Assets/Sprites/Efectos/Mundo4/Placa",
    "Bala": "Assets/Sprites/Efectos/Mundo4/Bala",
    "Cadena": "Assets/Sprites/Efectos/Mundo4/Cadena",
    # La segunda tanda (2026-10-22): el escenario y sus mecánicas.
    "TileMap": "Assets/TileMap/Mundo4",
    "Props": "Assets/TileMap/Mundo4",
    "Coberturas": "Assets/Sprites/Mecanicas/Mundo4/Coberturas",
    "Barril": "Assets/Sprites/Mecanicas/Mundo4/Barril",
    "Mina": "Assets/Sprites/Mecanicas/Mundo4/Mina",
    "Agua": "Assets/Sprites/Mecanicas/Mundo4/Agua",
    "Alambrada": "Assets/Sprites/Mecanicas/Mundo4/Alambrada",
    "Campanario": "Assets/Sprites/Mecanicas/Mundo4/Campanario",
    # Las memorias y su losa, el fondo y las cinemáticas (las del Juez y las cuatro de las memorias).
    "Memorias": "Assets/Sprites/Mecanicas/Mundo4/Memorias",
    "Fondos": "Assets/Sprites/Fondos/Mundo4Culpa",
    "Cinematicas": "Assets/Sprites/Cinematicas/Nivel4",
    # Lo que llegó al final (2026-10-23): la onda de «La sentencia» y el escudo de Kael (la Defensa Reforzada).
    "Onda": "Assets/Sprites/Efectos/Mundo4/Onda",
    "Escudo": "Assets/Sprites/Kael/Escudo",
}

# Las trae otro script: la puerta, la verja y el checkpoint, con los de los otros mundos
# (traer_arte_puertas.py), cuando estén completos.
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

    sobran = [d for d in ORIGEN.iterdir()
              if d.is_dir() and not d.name.startswith("_") and d.name not in DESTINOS and d.name not in DE_OTRO]
    for carpeta in sobran:
        print(f"  sin destino: {carpeta.name}")

    verbo = "se copiarían" if comprobar else "copiados"
    print(f"{nuevos} nuevos y {cambiados} cambiados {verbo}; {iguales} ya estaban al día.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
