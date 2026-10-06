using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// La tabla de importación del arte: qué píxeles por unidad y qué pivote lleva cada carpeta (o
    /// cada grupo de archivos) de <c>Assets/Sprites</c>. La aplica <see cref="ImportadorArte"/> en
    /// cada importación, y los constructores al regenerar; es el único sitio donde se decide.
    ///
    /// <b>La escala.</b> Un píxel de arte mide lo mismo en pantalla en todo el juego. La Centinela
    /// fijó la referencia a 49 píxeles por unidad; el Golem y la torreta están dibujados con
    /// bloques el doble de grandes (24) y la lava y las plataformas con bloques del mismo tamaño
    /// (48). Por eso los píxeles por unidad cambian por carpeta: no son los mismos para todos.
    ///
    /// <b>Los pivotes</b> caen en el suelo o en la superficie que se pisa, no en el centro: así se
    /// coloca una plataforma poniendo su Transform justo donde el jugador apoya el pie.
    ///
    /// Todo es pixel art (<see cref="AjustarPixelArt"/>): sprite único, filtro Point, sin
    /// compresión ni mipmaps, Clamp y malla Full Rect. Los PNG no se recortan ni se reescalan.
    ///
    /// Un cambio hecho a mano en el inspector se pierde al pulsar Apply: para cambiar un ajuste,
    /// cámbialo aquí.
    /// </summary>
    public static class TablaArte
    {
        public const string CarpetaSprites = "Assets/Sprites";
        public const string CarpetaJefes = CarpetaSprites + "/Jefes";
        public const string CarpetaGolem = CarpetaSprites + "/Enemigos/GolemCeniza";
        public const string CarpetaMecanicas = CarpetaSprites + "/Mecanicas";
        public const string CarpetaCinematicasNivel1 = CarpetaSprites + "/Cinematicas/Nivel1";

        public const int TamanoMaximo = 2048;

        /// <summary>
        /// Ajustes de los archivos cuya ruta empieza por <see cref="Ruta"/>: una carpeta (acabada
        /// en «/»), un grupo de archivos («…/Torreta_») o un archivo. Si dos reglas valen, gana la
        /// más concreta (la de ruta más larga).
        /// </summary>
        public sealed class Regla
        {
            public readonly string Ruta;

            /// <summary>Vacío: los de Unity por defecto (100).</summary>
            public readonly float? PixelesPorUnidad;

            /// <summary>Normalizado desde abajo a la izquierda. Vacío: el centro.</summary>
            public readonly Vector2? Pivote;

            /// <summary>Qué hay en esos archivos y por qué ese pivote.</summary>
            public readonly string Nota;

            public Regla(string ruta, float? pixelesPorUnidad, Vector2? pivote, string nota)
            {
                Ruta = ruta;
                PixelesPorUnidad = pixelesPorUnidad;
                Pivote = pivote;
                Nota = nota;
            }

            public bool Vale(string ruta) => ruta.StartsWith(Ruta, StringComparison.Ordinal);

            public SpriteAlignment Alineacion => Pivote is { } pivote && pivote != new Vector2(0.5f, 0.5f)
                ? SpriteAlignment.Custom
                : SpriteAlignment.Center;

            internal void Aplicar(TextureImporterSettings ajustes)
            {
                if (PixelesPorUnidad is { } ppu)
                {
                    ajustes.spritePixelsPerUnit = ppu;
                }

                ajustes.spriteAlignment = (int)Alineacion;
                ajustes.spritePivot = Pivote ?? new Vector2(0.5f, 0.5f);
            }
        }

        // La torreta disparando está dibujada 15 px más a la izquierda que en reposo y en carga
        // (el bloque ocupa x 1-45 en vez de 16-61). Su pivote se corre lo mismo para que el
        // bloque no salte al disparar; el PNG no se toca.
        private const float AnchoTorreta = 82f;
        public const float CorrimientoTorretaDisparo = 15f;

        private static readonly Regla[] Tabla = new[]
            {
                // Mundo 1: Golem de Ceniza, mecánicas y cinemáticas (Tesis/Imagenes/limpios, con su LEEME).
                new Regla(CarpetaGolem + "/", 24f, new Vector2(0.443f, 0.043f),
                    "Golem de Ceniza: 15 fotogramas de 90×92 con la línea de suelo en la fila 88; 2,7 u de alto."),
                new Regla(CarpetaMecanicas + "/Torreta_", 24f, new Vector2(0.470f, 0.048f),
                    "Torreta: 82×42, el pivote en la base del bloque (x 16-61, y 1-40); 1,9 × 1,7 u."),
                new Regla(CarpetaMecanicas + "/Torreta_3_disparo", 24f, new Vector2(0.470f - CorrimientoTorretaDisparo / AnchoTorreta, 0.048f),
                    "Torreta disparando: el bloque está dibujado 15 px a la izquierda; el pivote se corre lo mismo."),
                new Regla(CarpetaMecanicas + "/Lava", 48f, new Vector2(0f, 1f),
                    "Lava y Lava_espejo: 151×64, el pivote en la esquina de arriba a la izquierda (la costra); 3,15 × 1,33 u."),
                new Regla(CarpetaMecanicas + "/Plataforma_", 48f, new Vector2(0.5f, 0.852f),
                    "Plataforma inestable: 184×142, el pivote en la superficie (fila 21); 3,83 u de ancho."),
                new Regla(CarpetaMecanicas + "/Combustion_", 48f, new Vector2(0.5f, 0.708f),
                    "Plataforma de combustión: 166×89, el pivote en la superficie (fila 26); 3,42 u de ancho."),
                new Regla(CarpetaCinematicasNivel1 + "/", null, null,
                    "Planos del Nivel 1: 240×135 (16:9 exacto); se muestran a escala entera."),

                // Jefes: pixel art, y la Centinela con su escala y el pivote de los pies.
                new Regla(CarpetaJefes + "/", null, null,
                    "Jefes sin tabla propia: solo los ajustes de pixel art."),
                new Regla(ConstructorCentinela.CarpetaSprites + "/", ConstructorCentinela.PixelesPorUnidad, ConstructorCentinela.Pivote,
                    "Centinela Ardiente: 262×252, el pivote en los pies; 3,4 u de alto."),

                // Enemigos.
                new Regla(ConstructorEcoIra.CarpetaSprites + "/", ConstructorEcoIra.PixelesPorUnidad, ConstructorEcoIra.Pivote,
                    "Eco de la Ira: 1229×905, el pivote en los pies."),
            }
            .Concat(ConstructorCentinela.FotogramasIdle.Select(fotograma =>
                new Regla(ConstructorCentinela.RutaSprite(fotograma), ConstructorCentinela.PixelesPorUnidadIdle, ConstructorCentinela.Pivote,
                    "Centinela, idle frontal: dibujado más grande; a 58 mide lo mismo que en combate.")))
            .ToArray();

        public static IReadOnlyList<Regla> Reglas => Tabla;

        /// <summary>La regla más concreta que vale para <paramref name="ruta"/>, o null.</summary>
        public static Regla ReglaDe(string ruta)
        {
            Regla mejor = null;
            foreach (var regla in Tabla)
            {
                if (regla.Vale(ruta) && (mejor == null || regla.Ruta.Length > mejor.Ruta.Length))
                {
                    mejor = regla;
                }
            }

            return mejor;
        }

        /// <summary>
        /// Reaplica la tabla a todo lo que tiene regla en <see cref="CarpetaSprites"/> y lo
        /// reimporta si cambió. El importador ya lo hace en cada importación; esto cubre lo que se
        /// importó antes de que existiera su regla. Lo llama «Regenerar todo». Devuelve cuántos
        /// archivos cambiaron.
        /// </summary>
        public static int AplicarATodo()
        {
            var cambiados = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CarpetaSprites }))
            {
                var ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (ReglaDe(ruta) != null && AssetImporter.GetAtPath(ruta) is TextureImporter importador && Ajustar(importador))
                {
                    importador.SaveAndReimport();
                    cambiados++;
                }
            }

            if (cambiados > 0)
            {
                Debug.Log($"[Fragmentum] Tabla de importación: {cambiados} archivos tenían otros ajustes y se reimportaron.");
            }

            return cambiados;
        }

        /// <summary>
        /// Fija los ajustes de la tabla en <paramref name="importador"/>. Devuelve si cambió algo
        /// (false también si ninguna regla vale para su ruta).
        /// </summary>
        public static bool Ajustar(TextureImporter importador)
        {
            var regla = ReglaDe(importador.assetPath);
            return regla != null && AjustarPixelArt(importador, regla.Aplicar);
        }

        /// <summary>
        /// Fija los ajustes de pixel art y, con <paramref name="propios"/>, los de su regla.
        /// Devuelve si cambió algo.
        /// </summary>
        public static bool AjustarPixelArt(TextureImporter importador, Action<TextureImporterSettings> propios = null)
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
