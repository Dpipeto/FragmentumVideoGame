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
    ///
    /// Los 8 clips tienen los tiempos del LEEME, sin normalizar; la espada clavada forcejea sin
    /// fotograma extra y la muerte se queda en D4.
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

        // --- Clips ---

        private static AnimationClip Clip(string estado)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ConstructorCentinela.ClipDe(estado).Ruta);
            Assert.IsNotNull(clip, $"Falta el clip {estado}: ejecuta Fragmentum → Jefes → Generar los clips de la Centinela.");
            return clip;
        }

        private static readonly EditorCurveBinding Altura = EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalPosition.y");

        [Test]
        public void LaTabla_UsaLosFotogramasDelArte_YSoloC1bSeQuedaFuera()
        {
            Assert.AreEqual(8, ConstructorCentinela.Clips.Length);
            var usados = ConstructorCentinela.Clips.SelectMany(c => c.Fotogramas).Distinct().ToList();
            CollectionAssert.IsSubsetOf(usados, ConstructorCentinela.Archivos);
            CollectionAssert.AreEquivalent(new[] { "C1b" }, ConstructorCentinela.Archivos.Except(usados), "C1b es el impacto alternativo: importado, sin usar.");
        }

        [Test]
        public void LosOchoClips_TienenLosTiemposDelLeeme_SinNormalizar()
        {
            // Copiados del LEEME del arte, no de la tabla del constructor: si alguien toca un tiempo,
            // esta prueba lo nota.
            var esperados = new (string estado, string[] fotogramas, float[] duraciones, bool bucle)[]
            {
                (ConstructorCentinela.Idle, new[] { "I1", "I2", "I3", "I2" }, new[] { 0.40f, 0.40f, 0.40f, 0.40f }, true),
                (ConstructorCentinela.Andar, new[] { "A1", "A2", "A4", "A2" }, new[] { 0.15f, 0.15f, 0.15f, 0.15f }, true),
                (ConstructorCentinela.Espadazo, new[] { "E1", "E2", "E3", "E4" }, new[] { 0.55f, 0.08f, 0.22f, 0.30f }, false),
                (ConstructorCentinela.EspadaClavada, new[] { "C1", "C2", "C4" }, new[] { 0.09f, 1.20f, 0.18f }, false),
                (ConstructorCentinela.GolpeSuelo, new[] { "G1", "G2", "G3" }, new[] { 0.50f, 0.07f, 0.40f }, false),
                (ConstructorCentinela.Rugido, new[] { "R1", "R2", "R3" }, new[] { 0.35f, 0.35f, 0.20f }, false),
                (ConstructorCentinela.Salto, new[] { "J1", "J2", "J3", "J4", "J5" }, new[] { 0.50f, 0.10f, 0.30f, 0.15f, 0.45f }, false),
            };

            foreach (var (estado, fotogramas, duraciones, bucle) in esperados)
            {
                var clip = Clip(estado);
                var curva = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single(b => b.propertyName == "m_Sprite");
                var claves = AnimationUtility.GetObjectReferenceCurve(clip, curva);

                var inicio = 0f;
                for (var i = 0; i < fotogramas.Length; i++)
                {
                    Assert.AreEqual(inicio, claves[i].time, 1e-3f, $"{estado}: cuándo empieza {fotogramas[i]}.");
                    Assert.AreEqual(fotogramas[i], claves[i].value.name, $"{estado}: fotograma {i + 1}.");
                    inicio += duraciones[i];
                }

                // La clave de cierre repite el último fotograma: no hay ninguno de relleno.
                Assert.AreEqual(fotogramas.Length + 1, claves.Length, estado);
                Assert.AreEqual(fotogramas.Last(), claves.Last().value.name, estado);
                Assert.AreEqual(duraciones.Sum(), clip.length, 1e-3f, $"{estado}: duración total.");
                Assert.AreEqual(bucle, clip.isLooping, $"{estado}: bucle.");
            }
        }

        [Test]
        public void LaMuerte_AcabaEnD4_YSeQuedaAhi()
        {
            var clip = Clip(ConstructorCentinela.Muerte);
            var curva = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single(b => b.propertyName == "m_Sprite");
            var claves = AnimationUtility.GetObjectReferenceCurve(clip, curva);

            CollectionAssert.AreEqual(new[] { "D1", "D2", "D3", "D4" }, claves.Select(c => c.value.name), "Sin clave de cierre: D4 es el último.");
            var inicios = new[] { 0f, 0.40f, 0.90f, 2.10f };
            for (var i = 0; i < inicios.Length; i++)
            {
                Assert.AreEqual(inicios[i], claves[i].time, 1e-3f, $"Cuándo empieza {claves[i].value.name}.");
            }
            Assert.AreEqual(2.10f + 1f / 60f, clip.length, 1e-3f, "D3 dura 1,2 s y D4 se ve en el último fotograma.");
            Assert.IsFalse(clip.isLooping, "Sin bucle: el clip se detiene en D4, la ceniza con el fragmento.");
        }

        [Test]
        public void EnLosTresAtaques_LaAnticipacionEsLaMasLarga_YElGolpeCasiInstantaneo()
        {
            // En el salto, J2 (el despegue, 0,10 s) dura aún menos que el golpe J4 (0,15 s): manda
            // la tabla del LEEME, así que el golpe no tiene por qué ser el fotograma más corto.
            foreach (var (estado, anticipacion, golpe) in new[]
                     {
                         (ConstructorCentinela.Espadazo, "E1", "E2"),
                         (ConstructorCentinela.GolpeSuelo, "G1", "G2"),
                         (ConstructorCentinela.Salto, "J1", "J4"),
                     })
            {
                var clip = ConstructorCentinela.ClipDe(estado);
                float Duracion(string fotograma) => clip.Duraciones[Array.IndexOf(clip.Fotogramas, fotograma)];

                Assert.AreEqual(clip.Duraciones.Max(), Duracion(anticipacion), $"{estado}: {anticipacion} se ve venir.");
                Assert.LessOrEqual(Duracion(golpe), 0.15f, $"{estado}: {golpe} es casi instantáneo.");
                Assert.Greater(Duracion(anticipacion), 3f * Duracion(golpe), estado);
            }
        }

        [Test]
        public void ElForcejeo_SubeUnPixelCadaTresDecimas_SoloEnC2()
        {
            var curva = AnimationUtility.GetEditorCurve(Clip(ConstructorCentinela.EspadaClavada), Altura);
            Assert.IsNotNull(curva, "La espada clavada forcejea moviendo el Transform.");
            for (var i = 0; i < curva.length; i++)
            {
                Assert.AreEqual(AnimationUtility.TangentMode.Constant, AnimationUtility.GetKeyLeftTangentMode(curva, i), "En escalones.");
                Assert.AreEqual(AnimationUtility.TangentMode.Constant, AnimationUtility.GetKeyRightTangentMode(curva, i), "En escalones.");
                Assert.That(curva.keys[i].value, Is.EqualTo(0f).Or.EqualTo(0.02f).Within(1e-5f), "Solo dos alturas: 0 y +0,02.");
            }

            // C1 de 0 a 0,09; C2 de 0,09 a 1,29; C4 de 1,29 a 1,47.
            foreach (var (t, y) in new[]
                     {
                         (0.05f, 0f), (0.20f, 0f), (0.38f, 0f),
                         (0.40f, 0.02f), (0.68f, 0.02f),
                         (0.70f, 0f), (0.98f, 0f),
                         (1.00f, 0.02f), (1.28f, 0.02f),
                         (1.30f, 0f), (1.46f, 0f),
                     })
            {
                Assert.AreEqual(y, curva.Evaluate(t), 1e-5f, $"Altura a los {t} s.");
            }
        }

        [Test]
        public void SoloLaEspadaClavada_MueveElSprite_YElSaltoNoLoSube()
        {
            foreach (var definicion in ConstructorCentinela.Clips)
            {
                var mueve = AnimationUtility.GetCurveBindings(Clip(definicion.Estado)).Any(b => b.propertyName.StartsWith("m_LocalPosition"));
                if (definicion.Estado == ConstructorCentinela.EspadaClavada)
                {
                    Assert.IsTrue(mueve, "El forcejeo.");
                }
                else
                {
                    // El salto incluido: J3 y J4 están anclados al suelo y la altura la pone el cuerpo.
                    Assert.IsFalse(mueve, $"{definicion.Estado}: el sprite no se mueve; si no, el jefe saltaría al cambiar de clip.");
                }
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
