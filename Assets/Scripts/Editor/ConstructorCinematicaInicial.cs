using System.Collections.Generic;
using Fragmentum.Narrative;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// La cinemática inicial: 12 planos de ~80 s con su guion. Prepara la importación de las
    /// imágenes (pixel art: filtro Point, sin compresión) y escribe el guion en
    /// <c>Cinematica_Inicial.asset</c>.
    ///
    /// Después, el asset se edita en el Inspector como cualquier cinemática. Volver a ejecutar el
    /// menú restablece este guion y descarta lo que se haya cambiado a mano en ese asset.
    /// </summary>
    public static class ConstructorCinematicaInicial
    {
        public const string CarpetaImagenes = "Assets/Sprites/Cinematicas/Inicial";
        public const string RutaAsset = GeneradorNarrativa.Carpeta + "/Cinematica_Inicial.asset";

        /// <summary>Un plano del guion: imagen, duración y textos.</summary>
        private sealed class Plano
        {
            public int Numero;
            public float Duracion;
            public string Texto;
            public Cartel[] Carteles = new Cartel[0];
        }

        // Guion de la cinemática inicial (2026-09-30). Los textos sin tiempos entran a los 0,8 s y
        // se van 0,8 s antes de acabar el plano, en el tercio inferior.
        private static readonly Plano[] Guion =
        {
            new Plano { Numero = 1, Duracion = 7f },
            new Plano { Numero = 2, Duracion = 8f, Texto = "Hubo una guerra." },
            new Plano { Numero = 3, Duracion = 6f, Texto = "Alguien volvió de ella." },
            new Plano
            {
                Numero = 4, Duracion = 7f, Carteles = new[]
                {
                    new Cartel { texto = "Le dolía.", inicio = 0.5f, duracion = 2.5f },
                    new Cartel { texto = "Así que decidió no sentirlo.", inicio = 3.5f, duracion = 3f },
                },
            },
            new Plano { Numero = 5, Duracion = 6f },
            new Plano { Numero = 6, Duracion = 7f, Texto = "Le sacaron todo lo que pesaba." },
            new Plano { Numero = 7, Duracion = 8f, Texto = "Pero lo que negó no desapareció." },
            new Plano { Numero = 8, Duracion = 7f, Texto = "Tomó forma, y se quedó esperándolo." },
            new Plano { Numero = 9, Duracion = 8f },
            new Plano { Numero = 10, Duracion = 7f, Texto = "Y con lo que sobró, hicieron a alguien nuevo." },
            // El título va arriba: la mitad superior de la imagen está vacía a propósito.
            new Plano
            {
                Numero = 11, Duracion = 5f, Carteles = new[]
                {
                    new Cartel
                    {
                        texto = "FRAGMENTUM: Ecos de la Identidad", inicio = 0.8f, duracion = 3.4f,
                        posicion = POSICIONES_CARTEL.ARRIBA, titulo = true,
                    },
                },
            },
            new Plano { Numero = 12, Duracion = 4f, Texto = "¿Qué es lo que no estás mirando?" },
        };

        public static int Planos => Guion.Length;

        public static string RutaImagen(int numero) => $"{CarpetaImagenes}/Plano_{numero:00}.png";

        [MenuItem("Fragmentum/Narrativa/Importar la cinemática inicial (imágenes y guion)", priority = 152)]
        public static void MenuConstruir()
        {
            if (EditorUtility.DisplayDialog("Fragmentum",
                    "Se configuran las imágenes de la cinemática inicial y se escribe su guion en Cinematica_Inicial. " +
                    "Lo que hayas cambiado a mano en ese asset se perderá. ¿Continuar?", "Importar", "Cancelar"))
            {
                Construir();
            }
        }

        /// <summary><c>-executeMethod Fragmentum.EditorTools.ConstructorCinematicaInicial.ConstruirEnModoBatch -quit</c></summary>
        public static void ConstruirEnModoBatch()
        {
            if (!Construir() && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        public static bool Construir()
        {
            var faltan = 0;
            for (var n = 1; n <= Guion.Length; n++)
            {
                if (!ConfigurarImagen(RutaImagen(n)))
                {
                    faltan++;
                }
            }

            if (faltan > 0)
            {
                Debug.LogWarning($"[Fragmentum] Faltan {faltan} imágenes en {CarpetaImagenes}: esos planos se verán en negro.");
            }

            var cinematica = AssetDatabase.LoadAssetAtPath<CutsceneData>(RutaAsset);
            if (cinematica == null)
            {
                // Lo crea GeneradorNarrativa (que ya usa este guion) y lo enlaza al Nivel 1.
                GeneradorNarrativa.Construir();
                cinematica = AssetDatabase.LoadAssetAtPath<CutsceneData>(RutaAsset);
            }

            if (cinematica == null)
            {
                Debug.LogError($"[Fragmentum] No se pudo crear {RutaAsset}.");
                return false;
            }

            Rellenar(cinematica);
            EditorUtility.SetDirty(cinematica);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Fragmentum] Cinemática inicial: {cinematica.diapositivas.Count} planos, {cinematica.DuracionTotal:0} s.");
            return true;
        }

        /// <summary>Escribe el guion en una cinemática. Lo usa también GeneradorNarrativa al crearla.</summary>
        public static void Rellenar(CutsceneData cinematica)
        {
            cinematica.id = "Inicial";
            cinematica.saltable = true;
            cinematica.soloPrimeraVez = true;
            cinematica.musica = MusicaNiveles.DeCinematica(cinematica.id);
            cinematica.diapositivas = new List<Diapositiva>();

            foreach (var plano in Guion)
            {
                var carteles = new List<Cartel>();
                foreach (var cartel in plano.Carteles)
                {
                    carteles.Add(new Cartel
                    {
                        texto = cartel.texto,
                        inicio = cartel.inicio,
                        duracion = cartel.duracion,
                        posicion = cartel.posicion,
                        titulo = cartel.titulo,
                    });
                }

                cinematica.diapositivas.Add(new Diapositiva
                {
                    imagen = AssetDatabase.LoadAssetAtPath<Sprite>(RutaImagen(plano.Numero)),
                    texto = plano.Texto ?? string.Empty,
                    carteles = carteles,
                    duracion = plano.Duracion,
                });
            }
        }

        // Pixel art: sin filtro ni compresión, y sin mipmaps (la UI no los usa).
        private static bool ConfigurarImagen(string ruta)
        {
            if (!(AssetImporter.GetAtPath(ruta) is TextureImporter importador))
            {
                return false;
            }

            var ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);
            ajustes.textureType = TextureImporterType.Sprite;
            ajustes.spriteMode = (int)SpriteImportMode.Single;
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            ajustes.spriteGenerateFallbackPhysicsShape = false;
            ajustes.filterMode = FilterMode.Point;
            ajustes.wrapMode = TextureWrapMode.Clamp;
            ajustes.mipmapEnabled = false;
            ajustes.alphaIsTransparency = false;
            importador.SetTextureSettings(ajustes);
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.maxTextureSize = 2048;
            importador.SaveAndReimport();
            return true;
        }
    }
}
