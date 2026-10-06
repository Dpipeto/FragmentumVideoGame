using System;
using System.IO;
using System.Linq;
using Fragmentum.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El arte de la Centinela Ardiente: los 29 fotogramas están importados como pixel art, con el
    /// lienzo entero, el pivote común en los pies y la escala del LEEME (49 px por unidad; 58 el
    /// idle frontal). Si alguien los reimporta y se pierde un ajuste, falla aquí.
    /// </summary>
    public class CentinelaArteTests
    {
        private static Sprite Fotograma(string archivo)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ConstructorCentinela.RutaSprite(archivo));
            Assert.IsNotNull(sprite, $"Falta {ConstructorCentinela.RutaSprite(archivo)} o no se importó como sprite.");
            return sprite;
        }

        /// <summary>Filas opacas (de abajo arriba) del primer fotograma de un PNG, leído del disco.</summary>
        private static (int abajo, int arriba) FilasOpacas(string rutaAsset, int fotogramas = 1)
        {
            var textura = new Texture2D(2, 2);
            try
            {
                var ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", rutaAsset));
                Assert.IsTrue(textura.LoadImage(File.ReadAllBytes(ruta)), rutaAsset);
                var ancho = textura.width / fotogramas;
                var pixeles = textura.GetPixels32(); // fila 0 = la de abajo
                int abajo = int.MaxValue, arriba = -1;
                for (var y = 0; y < textura.height; y++)
                {
                    for (var x = 0; x < ancho; x++)
                    {
                        if (pixeles[y * textura.width + x].a > 0)
                        {
                            abajo = Math.Min(abajo, y);
                            arriba = Math.Max(arriba, y);
                        }
                    }
                }

                return (abajo, arriba);
            }
            finally
            {
                Object.DestroyImmediate(textura);
            }
        }

        /// <summary>Fila contando desde abajo de una fila contada desde arriba, como en el LEEME.</summary>
        private static int DesdeAbajo(int filaDesdeArriba) => ConstructorCentinela.AltoLienzo - 1 - filaDesdeArriba;

        // --- Importación ---

        [Test]
        public void LosVeintinueveFotogramas_SeImportanComoPixelArt_ConElMismoPivote()
        {
            Assert.AreEqual(29, ConstructorCentinela.Archivos.Length);
            Assert.AreEqual(29, ConstructorCentinela.Archivos.Distinct().Count(), "Sin repetidos.");
            foreach (var archivo in ConstructorCentinela.Archivos)
            {
                var ruta = ConstructorCentinela.RutaSprite(archivo);
                var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
                Assert.IsNotNull(importador, $"Falta {ruta}.");
                Assert.AreEqual(TextureImporterType.Sprite, importador.textureType, ruta);
                Assert.AreEqual(SpriteImportMode.Single, importador.spriteImportMode, ruta);
                Assert.AreEqual(FilterMode.Point, importador.filterMode, $"{ruta}: sin suavizado.");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importador.textureCompression, ruta);
                Assert.IsFalse(importador.mipmapEnabled, ruta);
                Assert.AreEqual(TextureWrapMode.Clamp, importador.wrapMode, ruta);
                Assert.IsTrue(importador.alphaIsTransparency, ruta);

                var ajustes = new TextureImporterSettings();
                importador.ReadTextureSettings(ajustes);
                Assert.AreEqual(SpriteMeshType.FullRect, ajustes.spriteMeshType, ruta);
                Assert.AreEqual((int)SpriteAlignment.Custom, ajustes.spriteAlignment, ruta);

                // Sin recortar ni reescalar: el lienzo entero, y el pivote en el centroide de los pies.
                var sprite = Fotograma(archivo);
                Assert.AreEqual(new Rect(0, 0, ConstructorCentinela.AnchoLienzo, ConstructorCentinela.AltoLienzo), sprite.rect, ruta);
                Assert.AreEqual(0.534f * ConstructorCentinela.AnchoLienzo, sprite.pivot.x, 0.01f, ruta);
                Assert.AreEqual(0.060f * ConstructorCentinela.AltoLienzo, sprite.pivot.y, 0.01f, ruta);
            }
        }

        [Test]
        public void ElIdleFrontal_VaA58PixelesPorUnidad_YElResto_A49()
        {
            foreach (var archivo in ConstructorCentinela.Archivos)
            {
                var esperado = archivo is "I1" or "I2" or "I3" ? 58f : 49f;
                Assert.AreEqual(esperado, Fotograma(archivo).pixelsPerUnit, archivo);
            }
        }

        [Test]
        public void LosPies_ApoyanEnLaLineaDeSuelo_YSoloLaOndaDeG3LaCruza()
        {
            // El pivote cae en el borde de arriba de la fila 237: los pies, sobre el suelo.
            Assert.AreEqual(DesdeAbajo(ConstructorCentinela.FilaSuelo) + 1f, ConstructorCentinela.Pivote.y * ConstructorCentinela.AltoLienzo, 0.2f);

            foreach (var archivo in ConstructorCentinela.Archivos)
            {
                var (abajo, _) = FilasOpacas(ConstructorCentinela.RutaSprite(archivo));
                if (archivo == "G3")
                {
                    // El golpe al suelo levanta la onda por debajo de la línea.
                    Assert.Less(abajo, DesdeAbajo(ConstructorCentinela.FilaSuelo), "G3: la onda baja del suelo.");
                    continue;
                }

                // A4 es el paso en el aire: la planta queda 3 px por encima.
                Assert.That(abajo, Is.InRange(DesdeAbajo(ConstructorCentinela.FilaSuelo), DesdeAbajo(ConstructorCentinela.FilaSuelo - 3)),
                    $"{archivo}: nada por debajo del suelo y los pies como mucho 3 px por encima.");
            }
        }

        [Test]
        public void LaCentinela_MideCasiElDobleQueKael_YElIdleLoMismoQueAndando()
        {
            var hoja = ConstructorArteKael.HojaIdle;
            var (kaelAbajo, kaelArriba) = FilasOpacas(hoja.Ruta, hoja.Fotogramas);
            var altoKael = (kaelArriba - kaelAbajo + 1f) / ConstructorArteKael.PixelesPorUnidad;

            float Alto(string archivo)
            {
                var (abajo, arriba) = FilasOpacas(ConstructorCentinela.RutaSprite(archivo));
                return (arriba - abajo + 1f) / ConstructorCentinela.PixelesPorUnidadDe(archivo);
            }

            var andando = Alto("A1");
            Assert.AreEqual(3.4f, andando, 0.1f, "Andando mide 3,4 u.");
            Assert.That(andando / altoKael, Is.InRange(1.8f, 2f), "Casi el doble que Kael.");
            foreach (var idle in ConstructorCentinela.FotogramasIdle)
            {
                Assert.AreEqual(andando, Alto(idle), 0.05f, $"{idle}: a 58 px por unidad, el idle frontal mide lo mismo que andando.");
            }
        }

        [Test]
        public void ElImportadorDeJefes_DaAjustesDePixelArt_AOtroJefeSinTabla()
        {
            const string carpeta = ArteJefes.Carpeta + "/_PruebaImportador";
            var ruta = carpeta + "/Fotograma.png";
            var textura = new Texture2D(4, 4);
            try
            {
                Assert.IsTrue(AssetDatabase.IsValidFolder(ArteJefes.Carpeta), $"Falta {ArteJefes.Carpeta}.");
                if (!AssetDatabase.IsValidFolder(carpeta))
                {
                    AssetDatabase.CreateFolder(ArteJefes.Carpeta, "_PruebaImportador");
                }

                File.WriteAllBytes(ruta, textura.EncodeToPNG());
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);

                var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
                Assert.AreEqual(TextureImporterType.Sprite, importador.textureType);
                Assert.AreEqual(SpriteImportMode.Single, importador.spriteImportMode);
                Assert.AreEqual(FilterMode.Point, importador.filterMode);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importador.textureCompression);
                Assert.IsFalse(importador.mipmapEnabled);
                Assert.AreEqual(TextureWrapMode.Clamp, importador.wrapMode);

                var ajustes = new TextureImporterSettings();
                importador.ReadTextureSettings(ajustes);
                Assert.AreEqual(SpriteMeshType.FullRect, ajustes.spriteMeshType);
            }
            finally
            {
                Object.DestroyImmediate(textura);
                AssetDatabase.DeleteAsset(carpeta);
            }
        }
    }
}
