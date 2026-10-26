using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.EditorTools;
using Fragmentum.Enemies;
using Fragmentum.Levels.Mecanicas;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El verificador del arte del Nivel 1 (Fragmentum → Niveles → Verificar el arte del Nivel 1):
    /// el proyecto lo pasa, y cada comprobación encuentra lo que se le rompe a propósito y lo
    /// nombra. Lo roto son copias o objetos en memoria: el arte del proyecto no se toca.
    /// </summary>
    public class VerificadorNivel1Tests
    {
        private const string Carpeta = "Assets/_PruebaVerificadorNivel1";

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
                AssetDatabase.CreateFolder("Assets", "_PruebaVerificadorNivel1");
            }
        }

        private static string Lista(IEnumerable<string> problemas) => string.Join("\n", problemas);

        private static void Contiene(List<string> problemas, string texto) =>
            Assert.IsTrue(problemas.Any(p => p.Contains(texto)), $"Se esperaba «{texto}» entre:\n{Lista(problemas)}");

        [Test]
        public void ElArteDelNivel1_PasaLaVerificacion()
        {
            var problemas = VerificadorNivel1.Problemas();
            CollectionAssert.IsEmpty(problemas, Lista(problemas));

            LogAssert.Expect(LogType.Log, new Regex(@"Verificación correcta: 34 sprites, 14 clips, 5 Animators, 6 prefabs"));
            Assert.IsTrue(VerificadorNivel1.Verificar());
        }

        [Test]
        public void RevisaLosTreintaSprites_LosTreceClips_LosCuatroAnimators_YLosSeisPrefabs()
        {
            var sprites = VerificadorNivel1.Sprites;
            Assert.AreEqual(34, sprites.Count, "15 fotogramas del Golem, 15 de las mecánicas (con la bola) y 4 planos.");
            Assert.AreEqual(sprites.Count, sprites.Distinct().Count());
            Assert.AreEqual(15, sprites.Count(s => s.StartsWith(TablaArte.CarpetaGolem + "/")));
            Assert.AreEqual(15, sprites.Count(s => s.StartsWith(TablaArte.CarpetaMecanicas + "/")));
            Assert.AreEqual(4, sprites.Count(s => s.StartsWith(TablaArte.CarpetaCinematicasNivel1 + "/")));

            Assert.AreEqual(14, VerificadorNivel1.Clips.Count, "4 del Golem, 9 de las mecánicas y el vuelo de la bola (la lava no tiene clip).");
            Assert.AreEqual(4, VerificadorNivel1.Clips.Sum(c => c.Eventos.Length), "Los eventos del golpe y de la muerte del Golem.");
            Assert.AreEqual(5, VerificadorNivel1.Animators.Count, "El Golem, la torreta, la plataforma inestable, la combustión y la bola.");
            Assert.AreEqual(6, VerificadorNivel1.Prefabs.Length, "El Golem y las cinco piezas de las mecánicas (con la bola).");
        }

        // --- 1. Sprites ---

        [Test]
        public void UnSpriteReimportadoSinSusAjustes_SeNombraConLoQueLeFalta()
        {
            CrearCarpeta();
            var ruta = Carpeta + "/Golem_I1.png";
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

                // Revisado contra la regla del Golem, como si fuera su primer fotograma.
                var problemas = VerificadorNivel1.ProblemasDeSprite(ConstructorGolem.RutaSprite("Golem_I1"), importador);
                Assert.IsTrue(problemas.All(p => p.StartsWith("Sprite Golem_I1:")), Lista(problemas));
                Contiene(problemas, "100 píxeles por unidad (debe ser 24)");
                Contiene(problemas, "lienzo de 100×100 px (debe ser 90×92");
                Contiene(problemas, "(debe ser Custom (0.443, 0.043))");
                Contiene(problemas, "filtro Bilinear");
                Contiene(problemas, "compresión");

                // Y contra la de la torreta disparando, con su pivote corrido 15 px.
                Contiene(VerificadorNivel1.ProblemasDeSprite(ConstructorMecanicas.RutaSprite("Torreta_3_disparo"), importador), "(debe ser Custom (0.287, 0.048))");
            }
            finally
            {
                Object.DestroyImmediate(textura);
            }

            StringAssert.Contains("Falta el sprite Combustion_2_calienta",
                VerificadorNivel1.ProblemasDeSprite(ConstructorMecanicas.RutaSprite("Combustion_2_calienta"), null).Single());
            StringAssert.Contains("no tiene regla en TablaArte", VerificadorNivel1.ProblemasDeSprite("Assets/Otra/Cosa.png", null).Single());
        }

        // --- 2. Clips ---

        private static AnimationClip ClipDePrueba(string nombre, params (float tiempo, string fotograma)[] claves)
        {
            var clip = new AnimationClip { frameRate = ClipsDeSprites.FotogramasPorSegundo, name = nombre };
            AnimationUtility.SetObjectReferenceCurve(clip, ClipsDeSprites.BindingSprite, claves
                .Select(c => new ObjectReferenceKeyframe { time = c.tiempo, value = AssetDatabase.LoadAssetAtPath<Sprite>(ConstructorGolem.RutaSprite(c.fotograma)) })
                .ToArray());
            return clip;
        }

        [Test]
        public void UnClipUniformado_SinSusEventos_OQueApagaUnCollider_SeNombra()
        {
            var ataque = VerificadorNivel1.Clips.Single(c => c.Nombre == "Golem_Ataque");

            // Los cuatro fotogramas igualados a 0,245 s, lo que no se debe hacer, y un evento que no es de la tabla.
            var clip = ClipDePrueba("Golem_Ataque", (0f, "Golem_A1"), (0.245f, "Golem_A3"), (0.49f, "Golem_A4"), (0.735f, "Golem_A5"), (0.96f, "Golem_A5"));
            try
            {
                AnimationUtility.SetAnimationEvents(clip, new[] { new AnimationEvent { time = 0.245f, functionName = nameof(EnemigoAnimacion.AbrirGolpe) } });
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Pivote/Arma", typeof(BoxCollider2D), "m_Enabled"), AnimationCurve.Constant(0f, 0.5f, 1f));

                var problemas = VerificadorNivel1.ProblemasDeClip(ataque, clip);
                Assert.IsTrue(problemas.All(p => p.StartsWith("Clip Golem_Ataque:")), Lista(problemas));
                Contiene(problemas, "la tabla suma 0.98 s");
                Contiene(problemas, "a los 0.5 s se ve nada (debe ser Golem_A3)");
                Contiene(problemas, "falta el evento «AbrirGolpe» a los 0.5 s");
                Contiene(problemas, "falta el evento «FinAtaque» a los 0.98 s");
                Contiene(problemas, "el evento «AbrirGolpe» a los 0.245 s no está en la tabla");
                Contiene(problemas, "anima BoxCollider2D.m_Enabled de «Pivote/Arma»");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }

            var muerte = VerificadorNivel1.Clips.Single(c => c.Nombre == "Golem_Muerte");
            StringAssert.Contains("Falta el clip Golem_Muerte", VerificadorNivel1.ProblemasDeClip(muerte, null).Single());
        }

        // --- 3. Animators ---

        private AnimatorController Copia(string ruta, string nombre)
        {
            CrearCarpeta();
            var copia = $"{Carpeta}/{nombre}.controller";
            Assert.IsTrue(AssetDatabase.CopyAsset(ruta, copia), $"No se pudo copiar {ruta}.");
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(copia);
        }

        [Test]
        public void UnaMecanicaCuyoEstadoNoLlevaASuDibujo_OConUnEstadoSuelto_SeNombra()
        {
            var esperado = VerificadorNivel1.Animators.Single(a => a.Nombre == "Torreta");
            var copia = Copia(esperado.Ruta, "Torreta");
            var maquina = copia.layers[0].stateMachine;

            // Disparo con la condición de Carga: el script dispararía y se seguiría viendo la carga.
            var disparo = maquina.anyStateTransitions.Single(t => t.destinationState.name == ConstructorMecanicas.Disparo);
            disparo.conditions = new[] { new AnimatorCondition { mode = AnimatorConditionMode.Equals, parameter = AnimatorParams.EstadoMecanica.Nombre, threshold = 1f } };
            maquina.AddState("Suelto");

            var problemas = VerificadorNivel1.ProblemasDelAnimator(esperado, copia);
            Contiene(problemas, "Animator Torreta: Estado = 2 (FIRE) no lleva a Disparo");
            Contiene(problemas, "Animator Torreta: el estado Suelto no está en la tabla.");
            Contiene(problemas, "Animator Torreta: nada lleva al estado Suelto (está suelto).");

            StringAssert.Contains("Falta el Animator Torreta", VerificadorNivel1.ProblemasDelAnimator(esperado, null).Single());
        }

        [Test]
        public void UnaTransicionQueComparaUnParametroDeOtroTipo_SeNombra()
        {
            var esperado = VerificadorNivel1.Animators.Single(a => a.Nombre == ConstructorGolem.NombreDatos);
            var copia = Copia(esperado.Ruta, "Golem");
            var idle = copia.layers[0].stateMachine.states.Select(s => s.state).Single(s => s.name == ConstructorGolem.Idle);

            // Attack es un bool: «mayor que» nunca se cumpliría y el Golem no atacaría.
            var aTiro = idle.transitions.Single(t => t.name == "Ya a tiro");
            aTiro.conditions = new[] { new AnimatorCondition { mode = AnimatorConditionMode.Greater, parameter = AnimatorParams.Attack.Nombre, threshold = 0.5f } };

            var problemas = VerificadorNivel1.ProblemasDelAnimator(esperado, copia);
            Contiene(problemas, "«Ya a tiro» (Idle → Ataque) compara Attack con Greater, pero es Bool");
        }

        // --- 4 y 5. Prefabs y eventos ---

        [Test]
        public void UnaZonaDeDanioEncendidaEnReposo_SeNombra_SalvoLasQueDananSiempre()
        {
            var golem = VerificadorNivel1.Prefabs.Single(p => p.Ruta == ConstructorGolem.RutaPrefab);
            var raiz = PrefabUtility.LoadPrefabContents(golem.Ruta);
            try
            {
                Assert.IsEmpty(VerificadorNivel1.ProblemasDelPrefab(golem, raiz), "El prefab, tal cual, está bien (con el contacto encendido).");

                raiz.transform.Find("Pivote/Arma").gameObject.SetActive(true);
                var problemas = VerificadorNivel1.ProblemasDelPrefab(golem, raiz);
                Assert.AreEqual("Prefab GolemCeniza: el collider de daño «Arma» está encendido en reposo.", problemas.Single(), Lista(problemas));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            var torreta = VerificadorNivel1.Prefabs.Single(p => p.Nombre == GeneradorMecanicas.TorretaFuego);
            raiz = PrefabUtility.LoadPrefabContents(torreta.Ruta);
            try
            {
                Object.DestroyImmediate(raiz.transform.Find("Llama").gameObject);
                raiz.GetComponentInChildren<Animator>().runtimeAnimatorController = null;
                var problemas = VerificadorNivel1.ProblemasDelPrefab(torreta, raiz);
                Contiene(problemas, "Prefab TorretaFuego: le falta su zona de daño «Llama».");
                Contiene(problemas, "Prefab TorretaFuego: su dibujo no usa Torreta.controller");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            StringAssert.Contains("Falta el prefab Lava", VerificadorNivel1.ProblemasDelPrefab(VerificadorNivel1.Prefabs.Single(p => p.Nombre == GeneradorMecanicas.Lava), null).Single());
        }

        [Test]
        public void UnEventoSinFuncionQueLoReciba_SeNombra()
        {
            var clip = new AnimationClip { name = "Golem_Prueba" };
            try
            {
                AnimationUtility.SetAnimationEvents(clip, new[]
                {
                    new AnimationEvent { time = 0.1f, functionName = nameof(EnemigoAnimacion.AbrirGolpe) },
                    new AnimationEvent { time = 0.2f, functionName = "DesactiveDamage" },
                });

                var golem = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorGolem.RutaPrefab).GetComponentInChildren<Animator>(true);
                CollectionAssert.AreEqual(new[]
                {
                    "Clip Golem_Prueba: el evento «DesactiveDamage» (a los 0.2 s) no llama a ninguna función de EnemigoAnimacion.",
                }, ComprobacionesArte.ProblemasDeEventos(clip, ComprobacionesArte.ReceptoresDe(golem)));

                // En una mecánica nada recibe eventos: su ciclo es del script.
                var torreta = GeneradorMecanicas.Cargar(GeneradorMecanicas.TorretaFuego).GetComponentInChildren<Animator>(true);
                var problemas = ComprobacionesArte.ProblemasDeEventos(clip, ComprobacionesArte.ReceptoresDe(torreta));
                Assert.AreEqual(2, problemas.Count, Lista(problemas));
                Contiene(problemas, "no llama a nada: el objeto del Animator no tiene scripts que reciban eventos");
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        // --- 6. Lo que sostiene ---

        [Test]
        public void LaPlataformaInestable_NoSostieneRota_YSiSeVieraRotaSosteniendo_SeNombra()
        {
            Assert.IsTrue(PlataformaInestable.SostieneEn(ESTADOS_PLATAFORMA.INTACT));
            Assert.IsTrue(PlataformaInestable.SostieneEn(ESTADOS_PLATAFORMA.CRACKED), "Agrietada es el aviso: aún se pisa.");
            Assert.IsFalse(PlataformaInestable.SostieneEn(ESTADOS_PLATAFORMA.BROKEN));

            var prefab = GeneradorMecanicas.Cargar(GeneradorMecanicas.PlataformaInestable);
            Assert.IsEmpty(VerificadorNivel1.ProblemasDeLaPlataformaInestable(ConstructorMecanicas.Plataforma, prefab));

            // La tabla, rota en el sitio de la agrietada: se vería partida con el collider activo.
            var original = ConstructorMecanicas.Plataforma;
            var rotaAntes = new ConstructorMecanicas.Mecanica
            {
                Nombre = original.Nombre,
                Estados = original.Estados,
                Clips = new[]
                {
                    original.Clips[0],
                    new ClipsDeSprites.Clip { Nombre = "Plataforma_Agrietada", Estado = ConstructorMecanicas.Agrietada, Fotogramas = new[] { VerificadorNivel1.FotogramaRota }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
                    original.Clips[2],
                },
            };

            var raiz = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
            try
            {
                raiz.GetComponent<BoxCollider2D>().enabled = false;
                var problemas = VerificadorNivel1.ProblemasDeLaPlataformaInestable(rotaAntes, raiz);
                Contiene(problemas, "en Agrietada (Estado = CRACKED) se ve Plataforma_3_rota y su collider sigue activo");
                Contiene(problemas, "Prefab PlataformaInestable: intacta no sostiene");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        [Test]
        public void LaCombustion_SostieneSiempre_YSiLaLlamaCompartieraObjetoConElSuelo_SeNombra()
        {
            var ruta = $"{GeneradorMecanicas.Carpeta}/{GeneradorMecanicas.PlataformaCombustion}.prefab";
            Assert.IsEmpty(VerificadorNivel1.ProblemasDeLaCombustion(AssetDatabase.LoadAssetAtPath<GameObject>(ruta)));

            var raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                // La llama en la raíz: al apagarla, SetActive(false) se llevaría el suelo.
                var combustion = new SerializedObject(raiz.GetComponent<PlataformaCombustion>());
                combustion.FindProperty("llama").objectReferenceValue = raiz.AddComponent<Hitbox>();
                combustion.ApplyModifiedPropertiesWithoutUndo();
                raiz.GetComponent<BoxCollider2D>().enabled = false;

                var problemas = VerificadorNivel1.ProblemasDeLaCombustion(raiz);
                Contiene(problemas, "Prefab PlataformaCombustion: su suelo (el collider sólido de la raíz) falta o está apagado.");
                Contiene(problemas, "Prefab PlataformaCombustion: la llama comparte objeto con el suelo; al apagarla dejaría de sostener.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
