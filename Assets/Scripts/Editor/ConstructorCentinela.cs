using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El arte de la Centinela Ardiente, jefe del Mundo 1. Por ahora, la importación: los 29
    /// fotogramas de <see cref="CarpetaSprites"/> como pixel art, con el mismo lienzo y el mismo
    /// pivote en los pies para todos. Si no, la Centinela saltaría al cambiar de clip.
    ///
    /// El arte llega limpio y alineado desde <c>Tesis/Jefe1 animaciones/limpios</c>, que tiene su
    /// LEEME. Los PNG no se retocan: cualquier recorte o reescalado rompe el anclaje común.
    /// </summary>
    public static class ConstructorCentinela
    {
        public const string CarpetaSprites = ArteJefes.Carpeta + "/Centinela";

        // --- Lienzo, escala y pivote (del LEEME del arte) ---

        /// <summary>Lienzo común de los 29 fotogramas. No se recorta: ahí está la alineación.</summary>
        public const int AnchoLienzo = 262;
        public const int AltoLienzo = 252;

        /// <summary>Línea de suelo: la fila 237 contando desde arriba, la misma en los 29.</summary>
        public const int FilaSuelo = 237;

        /// <summary>
        /// Centroide de los pies, normalizado desde abajo a la izquierda. En vertical cae en el
        /// borde de arriba de la línea de suelo (0,060 × 252 = 15,1 px desde abajo), así que se
        /// puede cortar de cualquier clip a cualquier otro sin que el sprite salte.
        /// </summary>
        public static readonly Vector2 Pivote = new Vector2(0.534f, 0.060f);

        /// <summary>
        /// Andando ocupa 168 px: a 49 px por unidad mide 3,4 u, casi el doble que Kael (1,77 u) y
        /// lo mismo que su caja de jefe (<c>JefeData.tamano</c>).
        /// </summary>
        public const float PixelesPorUnidad = 49f;

        /// <summary>
        /// El idle frontal (I1-I3) está dibujado más grande, 199 px. A 58 px por unidad mide 3,4 u,
        /// como el resto en combate. A 49 solo valdría para un plano de presentación.
        /// </summary>
        public const float PixelesPorUnidadIdle = 58f;

        public static readonly string[] FotogramasIdle = { "I1", "I2", "I3" };

        /// <summary>
        /// Los 29 fotogramas. C1b es un impacto alternativo a C1: se importa, pero ningún clip lo
        /// usa. Faltan A3 y C3 porque se descartaron al limpiar el arte, y no se rellenan.
        /// </summary>
        public static readonly string[] Archivos =
        {
            "I1", "I2", "I3",
            "A1", "A2", "A4",
            "E1", "E2", "E3", "E4",
            "C1", "C1b", "C2", "C4",
            "G1", "G2", "G3",
            "R1", "R2", "R3",
            "J1", "J2", "J3", "J4", "J5",
            "D1", "D2", "D3", "D4",
        };

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        public static float PixelesPorUnidadDe(string archivo) =>
            FotogramasIdle.Contains(archivo) ? PixelesPorUnidadIdle : PixelesPorUnidad;

        // --- Importación ---

        /// <summary>
        /// Ajustes de un fotograma de la Centinela: los de pixel art de todos los jefes, sus
        /// píxeles por unidad y el pivote de los pies. Devuelve si cambió algo. Lo usa
        /// <see cref="ImportadorJefes"/> en cada importación.
        /// </summary>
        public static bool AjustarImportador(TextureImporter importador)
        {
            var ppu = PixelesPorUnidadDe(Path.GetFileNameWithoutExtension(importador.assetPath));
            return ArteJefes.Ajustar(importador, ajustes =>
            {
                ajustes.spritePixelsPerUnit = ppu;
                ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
                ajustes.spritePivot = Pivote;
            });
        }
    }
}
