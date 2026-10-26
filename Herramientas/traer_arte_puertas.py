"""Trae al proyecto las puertas de salida, las verjas, los checkpoints, los planos de salida y la
portada del menú.

Copia, byte a byte, los PNG que limpian `limpiar_puertas.py` y `limpiar_portada.py` en
`Tesis/Imagenes/.../limpios` (el repositorio de al lado) a su carpeta de `Assets`. No reescala, no
recorta y no cambia nombres: lo único que decide es en qué carpeta va cada grupo. Los ajustes de
importación (píxeles por unidad y pivote) los pone Unity al importarlos, según
`ConstructorPuertas.Reglas` y `TablaArte` (la portada).

La carpeta `limpios` está en `Tesis/Imagenes`. Pasó un día dentro de `Tesis/Imagenes/Mundo1`
(2026-10-13), así que también se busca ahí.

Uso, desde la raíz del proyecto:

    python Herramientas/traer_arte_puertas.py             # copia lo que falte o haya cambiado
    python Herramientas/traer_arte_puertas.py --comprobar # solo dice qué cambiaría

Después, en Unity: Fragmentum → Regenerar todo.
"""
import filecmp
import shutil
import sys
from pathlib import Path

PROYECTO = Path(__file__).resolve().parent.parent
IMAGENES = PROYECTO.parent / "Tesis" / "Imagenes"


def limpios():
    for candidata in (IMAGENES / "limpios", IMAGENES / "Mundo1" / "limpios"):
        if candidata.is_dir():
            return candidata
    return None


# (carpeta del set limpio, archivos o "*") -> carpeta del proyecto
COPIAS = [
    ("Puerta", "*", "Assets/Sprites/Mecanicas/Puertas/Ira/Puerta"),
    ("Verja", "*", "Assets/Sprites/Mecanicas/Puertas/Ira/Verja"),
    ("Checkpoint", "*", "Assets/Sprites/Mecanicas/Puertas/Ira/Checkpoint"),
    ("Cinematicas", ["SalidaIra1.png", "SalidaIra2.png"], "Assets/Sprites/Cinematicas/Nivel1"),
    ("Mundo2/Puerta", "*", "Assets/Sprites/Mecanicas/Puertas/Tristeza/Puerta"),
    ("Mundo2/Verja", "*", "Assets/Sprites/Mecanicas/Puertas/Tristeza/Verja"),
    ("Mundo2/Checkpoint", "*", "Assets/Sprites/Mecanicas/Puertas/Tristeza/Checkpoint"),
    ("Mundo2/Cinematicas", ["SalidaTristeza1.png", "SalidaTristeza2.png"], "Assets/Sprites/Cinematicas/Nivel2"),
    # La portada del menú (N41). Después, en Unity: Fragmentum → Progresión → Crear escenas de menú y selector.
    ("Portada", ["FondoMenu.png"], "Assets/Sprites/Interfaz"),
]


def main():
    comprobar = "--comprobar" in sys.argv[1:]
    origen = limpios()
    if origen is None:
        print(f"No encuentro la carpeta limpios en {IMAGENES}")
        return 1

    nuevos = cambiados = iguales = 0
    for grupo, archivos, destino in COPIAS:
        carpeta = origen / grupo
        if not carpeta.is_dir():
            print(f"  falta la carpeta {grupo}")
            continue

        salida = PROYECTO / destino
        lista = sorted(carpeta.glob("*.png")) if archivos == "*" else [carpeta / a for a in archivos]
        for png in lista:
            if not png.exists():
                print(f"  falta {grupo}/{png.name}")
                continue

            final = salida / png.name
            if final.exists() and filecmp.cmp(png, final, shallow=False):
                iguales += 1
                continue

            if final.exists():
                cambiados += 1
                estado = "cambia"
            else:
                nuevos += 1
                estado = "nuevo"
            print(f"  {estado:7s} {destino}/{png.name}")
            if not comprobar:
                salida.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(png, final)

    print(f"Nuevos: {nuevos}  Cambiados: {cambiados}  Iguales: {iguales}" + ("  (solo comprobado)" if comprobar else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
