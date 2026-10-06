using System;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Ajustes de importación comunes al arte de todos los jefes, que vive en
    /// <see cref="Carpeta"/> con una subcarpeta por jefe. Los aplica <see cref="ImportadorJefes"/>.
    ///
    /// Todos son pixel art: sprite único, filtro Point, sin compresión ni mipmaps, Clamp y malla
    /// Full Rect. Los píxeles por unidad y el pivote son de cada jefe y los pone su constructor
    /// (por ejemplo, <see cref="ConstructorCentinela.AjustarImportador"/>).
    /// </summary>
    public static class ArteJefes
    {
        public const string Carpeta = "Assets/Sprites/Jefes";

        public const int TamanoMaximo = 2048;

        /// <summary>
        /// Fija los ajustes de pixel art y, con <paramref name="propios"/>, los del jefe.
        /// Devuelve si cambió algo.
        /// </summary>
        public static bool Ajustar(TextureImporter importador, Action<TextureImporterSettings> propios = null)
        {
            var antes = new TextureImporterSettings();
            importador.ReadTextureSettings(antes);
            var ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);

            ajustes.textureType = TextureImporterType.Sprite;
            ajustes.spriteMode = (int)SpriteImportMode.Single;
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            ajustes.spriteGenerateFallbackPhysicsShape = false;
            ajustes.filterMode = FilterMode.Point;
            ajustes.wrapMode = TextureWrapMode.Clamp;
            ajustes.mipmapEnabled = false;
            ajustes.alphaIsTransparency = true;
            ajustes.npotScale = TextureImporterNPOTScale.None;
            ajustes.readable = false;
            propios?.Invoke(ajustes);

            var cambia = !TextureImporterSettings.Equal(antes, ajustes) ||
                         importador.textureCompression != TextureImporterCompression.Uncompressed ||
                         importador.maxTextureSize != TamanoMaximo;
            if (!cambia)
            {
                return false;
            }

            importador.SetTextureSettings(ajustes);
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.maxTextureSize = TamanoMaximo;
            return true;
        }
    }
}
