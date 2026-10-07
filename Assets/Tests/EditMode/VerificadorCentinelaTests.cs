using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fragmentum.Bosses;
using Fragmentum.Core.Combate;
using Fragmentum.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El verificador de la Centinela (Fragmentum → Jefes → Verificar Centinela): el proyecto lo
    /// pasa, y cada comprobación encuentra lo que se le rompe a propósito y lo nombra. Lo roto
    /// son copias o objetos en memoria: el arte del proyecto no se toca.
    /// </summary>
    public class VerificadorCentinelaTests
    {
        private const string Carpeta = "Assets/_PruebaVerificador";

        [TearDown]
        public void BorrarCarpeta()
        {
            if (AssetDatabase.IsValidFolder(Carpeta))
            {
                AssetDatabase.DeleteAsset(Carpeta);
            }
        }

        private static void CrearCarpeta()
        {
            if (!AssetDatabase.IsValidFolder(Carpeta))
            {
                AssetDatabase.CreateFolder("Assets", "_PruebaVerificador");
            }
        }

        private static string Lista(System.Collections.Generic.IEnumerable<string> problemas) => string.Join("\n", problemas);

        [Test]
        public void ElArteDeLaCentinela_PasaLaVerificacion()
        {
            var problemas = VerificadorCentinela.Problemas();
            CollectionAssert.IsEmpty(problemas, Lista(problemas));

            LogAssert.Expect(LogType.Log, new Regex(@"Verificación correcta: 29 sprites, 8 clips"));
            Assert.IsTrue(VerificadorCentinela.Verificar());
        }

        // --- 1. Sprites ---

        [Test]
        public void UnSpriteReimportadoSinSusAjustes_SeNombraConLoQueLeFalta()
        {
            CrearCarpeta();
            var ruta = Carpeta + "/D4.png";
            var textura = new Texture2D(100, 100);
            try
            {
                File.WriteAllBytes(ruta, textura.EncodeToPNG());
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
                var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
                importador.textureType = TextureImporterType.Sprite;
                importador.spritePixelsPerUnit = 100f;
                importador.filterMode = FilterMode.Bilinear;
                importador.textureCompression = TextureImporterCompression.Compressed;
                importador.SaveAndReimport();

                var problemas = VerificadorCentinela.ProblemasDeSprite("D4", importador);
                Assert.IsTrue(problemas.All(p => p.StartsWith("Sprite D4:")), Lista(problemas));
                Assert.IsTrue(problemas.Any(p => p.Contains("100 píxeles por unidad (debe ser 49)")), Lista(problemas));
                Assert.IsTrue(problemas.Any(p => p.Contains("lienzo de 100×100")), Lista(problemas));
                Assert.IsTrue(problemas.Any(p => p.Contains("pivote")), Lista(problemas));
                Assert.IsTrue(problemas.Any(p => p.Contains("filtro Bilinear")), Lista(problemas));
                Assert.IsTrue(problemas.Any(p => p.Contains("compresión")), Lista(problemas));

                var idle = VerificadorCentinela.ProblemasDeSprite("I1", importador);
                Assert.IsTrue(idle.Any(p => p.Contains("(debe ser 58)")), "El idle frontal va a 58.");
            }
            finally
            {
                Object.DestroyImmediate(textura);
            }

            StringAssert.Contains("Falta el sprite G3", VerificadorCentinela.ProblemasDeSprite("G3", null).Single());
        }

        // --- 2. Clips ---

        [Test]
        public void UnClipConOtraDuracionOConOtroFotograma_SeNombra()
        {
            var espadazo = ConstructorCentinela.ClipDe(ConstructorCentinela.Espadazo);
            var clip = new AnimationClip { frameRate = ConstructorCentinela.FotogramasPorSegundo, name = "Centinela_Espadazo" };
            try
            {
                Sprite Fotograma(string nombre) => AssetDatabase.LoadAssetAtPath<Sprite>(ConstructorCentinela.RutaSprite(nombre));

                // E3 donde va E2, y todo igualado a 0,25 s: lo que no se debe hacer.
                AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), new[]
                {
                    new ObjectReferenceKeyframe { time = 0f, value = Fotograma("E1") },
                    new ObjectReferenceKeyframe { time = 0.55f, value = Fotograma("E3") },
                    new ObjectReferenceKeyframe { time = 0.75f, value = Fotograma("E4") },
                });

                var problemas = VerificadorCentinela.ProblemasDeClip(espadazo, clip);
                Assert.IsTrue(problemas.Any(p => p.StartsWith("Clip Centinela_Espadazo: dura") && p.Contains("la tabla suma 1.15 s")), Lista(problemas));
                Assert.IsTrue(problemas.Any(p => p.Contains("a los 0.55 s se ve E3 (debe ser E2)")), Lista(problemas));
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }

            StringAssert.Contains("Falta el clip Centinela_Muerte", VerificadorCentinela.ProblemasDeClip(ConstructorCentinela.ClipDe(ConstructorCentinela.Muerte), null).Single());
        }

        // --- 3. Animator ---

        [Test]
        public void UnAnimatorConUnEstadoSuelto_OUnaTransicionRota_SeNombra()
        {
            CrearCarpeta();
            var ruta = Carpeta + "/Copia.controller";
            Assert.IsTrue(AssetDatabase.CopyAsset(ConstructorCentinela.RutaControlador, ruta));
            var copia = AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta);
            var maquina = copia.layers[0].stateMachine;
            var estados = maquina.states.Select(s => s.state).ToDictionary(s => s.name);

            maquina.RemoveState(estados[ConstructorCentinela.Salto]);
            maquina.AddState("Suelto");
            var rota = estados[ConstructorCentinela.Idle].AddTransition(estados[ConstructorCentinela.Rugido]);
            rota.name = "Rota";
            rota.AddCondition(AnimatorConditionMode.If, 0f, "NoExiste");
            var siempre = estados[ConstructorCentinela.Andar].AddTransition(estados[ConstructorCentinela.Idle]);
            siempre.name = "Siempre";
            siempre.hasExitTime = false;

            var problemas = VerificadorCentinela.ProblemasDelControlador(copia);
            Assert.IsTrue(problemas.Contains("Animator: falta el estado Salto."), Lista(problemas));
            Assert.IsTrue(problemas.Contains("Animator: el estado Suelto no está en la tabla."), Lista(problemas));
            Assert.IsTrue(problemas.Contains("Animator: nada lleva al estado Suelto (está suelto)."), Lista(problemas));
            Assert.IsTrue(problemas.Any(p => p.Contains("«Rota» (Idle → Rugido) usa el parámetro NoExiste")), Lista(problemas));
            Assert.IsTrue(problemas.Any(p => p.Contains("«Siempre» (Andar → Idle) no tiene condición ni tiempo de salida")), Lista(problemas));

            StringAssert.Contains("Falta el Animator", VerificadorCentinela.ProblemasDelControlador(null).Single());
        }

        // --- 4. Prefab ---

        [Test]
        public void UnColliderDeAtaqueEncendido_OElFragmentoVisible_SeNombran()
        {
            var raiz = PrefabUtility.LoadPrefabContents(VerificadorCentinela.RutaPrefab);
            try
            {
                Assert.IsEmpty(VerificadorCentinela.ProblemasDelPrefab(raiz), "El prefab, tal cual, está bien.");

                raiz.GetComponentInChildren<Hitbox>(true).gameObject.SetActive(true);
                raiz.GetComponentInChildren<FragmentoJefe>(true).gameObject.SetActive(true);
                raiz.GetComponentInChildren<AnimacionJefe>(true).transform.localPosition = new Vector3(0f, 1f, 0f);

                var problemas = VerificadorCentinela.ProblemasDelPrefab(raiz);
                Assert.IsTrue(problemas.Any(p => p.Contains("el collider de daño «Contacto» está encendido en reposo")), Lista(problemas));
                Assert.IsTrue(problemas.Any(p => p.Contains("el fragmento está visible en reposo")), Lista(problemas));
                Assert.IsTrue(problemas.Any(p => p.Contains("el sprite no está en los pies")), Lista(problemas));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            StringAssert.Contains("Falta el prefab", VerificadorCentinela.ProblemasDelPrefab(null).Single());
        }

        // --- 5. Eventos ---

        [Test]
        public void UnEventoQueNoLlamaAUnaFuncionDeAnimacionJefe_SeNombra()
        {
            var clip = new AnimationClip { name = "Centinela_Prueba" };
            try
            {
                AnimationUtility.SetAnimationEvents(clip, new[]
                {
                    new AnimationEvent { time = 0.1f, functionName = nameof(AnimacionJefe.Golpe) },
                    new AnimationEvent { time = 0.2f, functionName = "NoExiste" },
                    new AnimationEvent { time = 0.3f, functionName = nameof(AnimacionJefe.AnticipacionDe) }, // devuelve un float
                });

                var problemas = VerificadorCentinela.ProblemasDeEventos(clip);
                CollectionAssert.AreEquivalent(new[]
                {
                    "Clip Centinela_Prueba: el evento «NoExiste» (a los 0.2 s) no llama a ninguna función de AnimacionJefe.",
                    "Clip Centinela_Prueba: el evento «AnticipacionDe» (a los 0.3 s) no llama a ninguna función de AnimacionJefe.",
                }, problemas);
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }
    }
}
