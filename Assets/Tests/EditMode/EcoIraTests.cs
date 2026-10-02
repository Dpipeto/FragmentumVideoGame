using System;
using System.IO;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El Eco de la Ira con su arte: los fotogramas están importados como pixel art y alineados
    /// en los pies, los clips tienen los tiempos del diseño (anticipación larga, golpe corto), la
    /// ventana de daño va de a2 a a4 y el prefab está completo.
    /// </summary>
    public class EcoIraTests
    {
        private static Sprite Fotograma(string archivo)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ConstructorEcoIra.RutaSprite(archivo));
            Assert.IsNotNull(sprite, $"Falta {ConstructorEcoIra.RutaSprite(archivo)}: ejecuta Fragmentum → Regenerar todo.");
            return sprite;
        }

        private static AnimationClip Clip(string estado)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ConstructorEcoIra.ClipDe(estado).Ruta);
            Assert.IsNotNull(clip, $"Falta el clip {estado} del Eco.");
            return clip;
        }

        private static AnimatorController Controlador()
        {
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorEcoIra.RutaControlador);
            Assert.IsNotNull(controlador, $"Falta {ConstructorEcoIra.RutaControlador}.");
            return controlador;
        }

        private static GameObject Prefab()
        {
            var prefab = ConstructorEcoIra.Prefab;
            Assert.IsNotNull(prefab, $"Falta {ConstructorEcoIra.RutaPrefab}.");
            return prefab;
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

        // --- Importación ---

        [Test]
        public void LosDoceFotogramas_SeImportanComoPixelArt_ConElMismoPivote()
        {
            Assert.AreEqual(12, ConstructorEcoIra.Archivos.Count(), "I1-I2, r1-r4, a1-a4, d2-d3: D1 se descartó.");
            foreach (var archivo in ConstructorEcoIra.Archivos)
            {
                var ruta = ConstructorEcoIra.RutaSprite(archivo);
                var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
                Assert.AreEqual(TextureImporterType.Sprite, importador.textureType, ruta);
                Assert.AreEqual(SpriteImportMode.Single, importador.spriteImportMode, ruta);
                Assert.AreEqual(FilterMode.Point, importador.filterMode, $"{ruta}: sin suavizado.");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importador.textureCompression, ruta);
                Assert.IsFalse(importador.mipmapEnabled, ruta);
                Assert.IsTrue(importador.alphaIsTransparency, ruta);
                Assert.AreEqual(ConstructorEcoIra.PixelesPorUnidad, importador.spritePixelsPerUnit, ruta);

                var ajustes = new TextureImporterSettings();
                importador.ReadTextureSettings(ajustes);
                Assert.AreEqual(SpriteMeshType.FullRect, ajustes.spriteMeshType, ruta);
                Assert.AreEqual((int)SpriteAlignment.Custom, ajustes.spriteAlignment, ruta);

                // Sin recortar ni reescalar: el lienzo entero, y el pivote en el ancla de los pies.
                var sprite = Fotograma(archivo);
                Assert.AreEqual(new Rect(0, 0, ConstructorEcoIra.AnchoLienzo, ConstructorEcoIra.AltoLienzo), sprite.rect, ruta);
                Assert.AreEqual(0.345f * ConstructorEcoIra.AnchoLienzo, sprite.pivot.x, 0.01f, ruta);
                Assert.AreEqual(0.014f * ConstructorEcoIra.AltoLienzo, sprite.pivot.y, 0.01f, ruta);
            }
        }

        [Test]
        public void LosPies_CaenEnLaFilaDelPivote_EnLosDoce()
        {
            // El ancla (424, 892) contando desde arriba es la fila 12 contando desde abajo. Con el
            // mismo pivote en todos, la planta de los pies queda siempre a la misma altura: el Eco
            // no salta al cambiar de estado.
            var filaPivote = 0.014f * ConstructorEcoIra.AltoLienzo;
            foreach (var archivo in ConstructorEcoIra.Archivos)
            {
                var (abajo, _) = FilasOpacas(ConstructorEcoIra.RutaSprite(archivo));
                Assert.AreEqual(ConstructorEcoIra.AltoLienzo - 1 - 892, abajo, $"{archivo}: la fila de los pies.");
                Assert.That(filaPivote, Is.InRange(abajo, abajo + 1f), $"{archivo}: el pivote está en la fila de los pies.");
            }
        }

        [Test]
        public void ElEco_MideEl60PorCientoDeKael()
        {
            var hoja = ConstructorArteKael.HojaIdle;
            var (kaelAbajo, kaelArriba) = FilasOpacas(hoja.Ruta, hoja.Fotogramas);
            var altoKael = (kaelArriba - kaelAbajo + 1f) / ConstructorArteKael.PixelesPorUnidad;

            foreach (var archivo in ConstructorEcoIra.ClipDe(ConstructorEcoIra.Run).Fotogramas)
            {
                var (abajo, arriba) = FilasOpacas(ConstructorEcoIra.RutaSprite(archivo));
                var altoEco = (arriba - abajo + 1f) / ConstructorEcoIra.PixelesPorUnidad;
                Assert.AreEqual(0.6f, altoEco / altoKael, 0.02f, $"{archivo}: corriendo, el Eco mide el 60 % de Kael.");
            }

            // Y el píxel del dibujo (~10 px de textura) mide casi lo mismo que uno de Kael.
            Assert.AreEqual(1f / ConstructorArteKael.PixelesPorUnidad, ConstructorEcoIra.PixelDeArte, 0.003f);
        }

        // --- Clips ---

        [Test]
        public void LosClips_TienenLosTiemposDelDiseno_SinNormalizar()
        {
            var esperados = new (string estado, string[] fotogramas, float[] inicios, float duracion, bool bucle)[]
            {
                (ConstructorEcoIra.Idle, new[] { "I1", "I2" }, new[] { 0f, 0.40f }, 0.80f, true),
                (ConstructorEcoIra.Run, new[] { "r1", "r2", "r3", "r4" }, new[] { 0f, 0.12f, 0.24f, 0.36f }, 0.48f, true),
                (ConstructorEcoIra.Attack, new[] { "a1", "a2", "a3", "a4" }, new[] { 0f, 0.35f, 0.43f, 0.48f }, 0.73f, false),
                (ConstructorEcoIra.Death, new[] { "d2", "d3" }, new[] { 0f, 0.15f }, 0.30f, false),
            };

            foreach (var (estado, fotogramas, inicios, duracion, bucle) in esperados)
            {
                var clip = Clip(estado);
                var curva = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single(b => b.propertyName == "m_Sprite");
                var claves = AnimationUtility.GetObjectReferenceCurve(clip, curva);

                for (var i = 0; i < fotogramas.Length; i++)
                {
                    Assert.AreEqual(inicios[i], claves[i].time, 1e-4f, $"{estado}: cuándo empieza {fotogramas[i]}.");
                    Assert.AreEqual(fotogramas[i], claves[i].value.name, $"{estado}: fotograma {i + 1}.");
                }

                // La clave de cierre repite el último fotograma: no hay ninguno de relleno.
                Assert.AreEqual(fotogramas.Length + 1, claves.Length, estado);
                Assert.AreEqual(fotogramas.Last(), claves.Last().value.name, estado);
                Assert.AreEqual(duracion, clip.length, 1e-3f, $"{estado}: duración total.");
                Assert.AreEqual(bucle, clip.isLooping, $"{estado}: bucle.");
            }
        }

        [Test]
        public void LaAnticipacionDelAtaque_DuraMuchoMasQueElGolpe()
        {
            var ataque = ConstructorEcoIra.ClipDe(ConstructorEcoIra.Attack);
            var anticipacion = ataque.Duraciones[0];
            var golpe = ataque.Duraciones[1] + ataque.Duraciones[2];
            Assert.AreEqual(0.35f, anticipacion, 1e-4f);
            Assert.Greater(anticipacion, 2.5f * golpe, "a1 tiene que verse venir; a2 y a3 son casi instantáneos.");
        }

        [Test]
        public void LaVentanaDeGolpe_VaDeA2AA4_YLosClipsSinBucleAvisanAlAcabar()
        {
            var ataque = Clip(ConstructorEcoIra.Attack).events;
            Assert.AreEqual(3, ataque.Length);
            Assert.AreEqual(nameof(EnemigoAnimacion.AbrirGolpe), ataque[0].functionName);
            Assert.AreEqual(0.35f, ataque[0].time, 1e-4f, "Se abre con a2.");
            Assert.AreEqual(nameof(EnemigoAnimacion.CerrarGolpe), ataque[1].functionName);
            Assert.AreEqual(0.48f, ataque[1].time, 1e-4f, "Se cierra con a4: solo a2 y a3 hacen daño.");
            Assert.AreEqual(nameof(EnemigoAnimacion.FinAtaque), ataque[2].functionName);
            Assert.AreEqual(0.73f, ataque[2].time, 1e-4f);

            var muerte = Clip(ConstructorEcoIra.Death).events;
            Assert.AreEqual(1, muerte.Length);
            Assert.AreEqual(nameof(EnemigoAnimacion.FinMuerte), muerte[0].functionName);
            Assert.AreEqual(0.30f, muerte[0].time, 1e-4f, "La muerte se remata al acabar d3.");

            Assert.IsEmpty(Clip(ConstructorEcoIra.Idle).events);
            Assert.IsEmpty(Clip(ConstructorEcoIra.Run).events);
        }

        [Test]
        public void ElSquash_EstiraEnA2_EncogeEnA4_YVuelveA1()
        {
            var clip = Clip(ConstructorEcoIra.Attack);
            var curva = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalScale.x"));
            Assert.IsNotNull(curva, "El ataque anima la escala X del sprite.");

            Assert.AreEqual(1f, curva.Evaluate(0.2f), 1e-4f, "a1");
            Assert.AreEqual(1.15f, curva.Evaluate(0.39f), 1e-4f, "a2");
            Assert.AreEqual(1f, curva.Evaluate(0.45f), 1e-4f, "a3");
            Assert.AreEqual(0.9f, curva.Evaluate(0.6f), 1e-4f, "a4");
            Assert.AreEqual(1f, curva.Evaluate(0.73f), 1e-4f, "al acabar");

            foreach (var otro in new[] { ConstructorEcoIra.Idle, ConstructorEcoIra.Run, ConstructorEcoIra.Death })
            {
                Assert.IsFalse(AnimationUtility.GetCurveBindings(Clip(otro)).Any(b => b.type == typeof(Transform)), $"{otro} no toca la escala.");
            }
        }

        // --- Controlador ---

        [Test]
        public void ElControlador_TieneCuatroEstados_YSoloAttackARunEsperaAlFinal()
        {
            var maquina = Controlador().layers[0].stateMachine;
            var estados = maquina.states.Select(s => s.state).ToDictionary(s => s.name);
            CollectionAssert.AreEquivalent(new[] { "Idle", "Run", "Attack", "Death" }, estados.Keys);
            Assert.AreEqual("Idle", maquina.defaultState.name);

            foreach (var estado in estados.Values)
            {
                Assert.AreSame(Clip(estado.name), estado.motion, $"{estado.name} reproduce su clip.");
                Assert.IsTrue(estado.writeDefaultValues, $"{estado.name}: al salir de Attack el squash vuelve a 1.");
            }

            Assert.IsTrue(estados["Run"].speedParameterActive, "Las piernas van al ritmo del cuerpo.");
            Assert.AreEqual(AnimatorParams.VelocidadCorrer.Nombre, estados["Run"].speedParameter);

            string Descripcion(AnimatorStateTransition t) =>
                $"{t.destinationState.name}:{t.hasExitTime}:" + string.Join(",", t.conditions.Select(c => $"{c.mode}{c.parameter}"));

            // Con el jugador ya a tiro, la FSM pasa de IDLE a ATTACK en un solo fotograma.
            CollectionAssert.AreEqual(new[] { "Attack:False:IfAttack", "Run:False:IfMovEn" }, estados["Idle"].transitions.Select(Descripcion));
            // Atacar va antes que perderlo: al entrar en ATTACK la FSM apaga MovEn.
            CollectionAssert.AreEqual(new[] { "Attack:False:IfAttack", "Idle:False:IfNotMovEn,IfNotDamage,IfNotAttack" }, estados["Run"].transitions.Select(Descripcion));
            CollectionAssert.AreEqual(new[] { "Run:True:", "Run:False:IfNotAttack" }, estados["Attack"].transitions.Select(Descripcion));
            Assert.IsEmpty(estados["Death"].transitions);

            var atacar = estados["Attack"].transitions[0];
            Assert.AreEqual(1f, atacar.exitTime, 1e-4f, "Attack → Run al terminar el clip.");

            var muerte = maquina.anyStateTransitions.Single();
            Assert.AreEqual("Death", muerte.destinationState.name);
            Assert.IsFalse(muerte.hasExitTime);
            Assert.IsFalse(muerte.canTransitionToSelf, "Si no, el clip de muerte se reiniciaría en cada fotograma.");

            foreach (var t in estados.Values.SelectMany(e => e.transitions).Append(muerte))
            {
                Assert.AreEqual(0f, t.duration, $"{t.name}: sin mezcla entre fotogramas de pixel art.");
            }
        }

        // --- Prefab ---

        [Test]
        public void ElPrefab_EstaCompleto()
        {
            var prefab = Prefab();
            Assert.AreEqual(LayerMask.NameToLayer(Capas.Enemy), prefab.layer);
            Assert.IsNotNull(prefab.GetComponent<Rigidbody2D>());
            Assert.IsNotNull(prefab.GetComponent<Health>());
            var cuerpo = prefab.GetComponent<CapsuleCollider2D>();
            Assert.IsNotNull(cuerpo);
            Assert.IsFalse(cuerpo.isTrigger, "El cuerpo se apoya en el suelo.");

            var controlador = prefab.GetComponent<EnemyController>();
            Assert.IsNotNull(controlador);
            var serializado = new SerializedObject(controlador);
            var datos = serializado.FindProperty("datos").objectReferenceValue as EnemyData;
            Assert.AreSame(GeneradorEnemigos.CargarDatos(ConstructorEcoIra.NombreDatos), datos);
            Assert.AreEqual(0, datos.danioContacto, "Fuera de a2 y a3 se le puede pasar por encima sin daño.");
            Assert.AreEqual(Color.white, datos.tinte, "Con arte final, sin tinte.");
            Assert.IsTrue(datos.spriteMiraDerecha, "Los doce fotogramas miran a la derecha.");

            var sprite = prefab.transform.Find("Pivote/Sprite");
            Assert.IsNotNull(sprite);
            Assert.AreEqual(Vector3.one, sprite.localScale, "El squash parte de 1.");
            var animator = sprite.GetComponent<Animator>();
            Assert.AreSame(Controlador(), animator.runtimeAnimatorController);
            Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, animator.cullingMode, "Fuera de cámara también llegan los eventos.");
            Assert.AreSame(animator, serializado.FindProperty("animator").objectReferenceValue);

            var animacion = sprite.GetComponent<EnemigoAnimacion>();
            Assert.IsNotNull(animacion, "Los eventos llegan a los scripts del objeto del Animator.");
            Assert.AreSame(animacion, serializado.FindProperty("animacion").objectReferenceValue);
            var animacionSerializada = new SerializedObject(animacion);
            var destello = animacionSerializada.FindProperty("materialDestello").objectReferenceValue as Material;
            Assert.IsNotNull(destello);
            Assert.AreEqual(ConstructorEcoIra.ShaderDestello, destello.shader.name);
            Assert.AreEqual(ConstructorEcoIra.VelocidadNaturalCarrera, animacionSerializada.FindProperty("velocidadNaturalCarrera").floatValue, 1e-4f);
            Assert.AreEqual(1f, animacion.velocidadAnimacion);

            var arma = prefab.transform.Find("Pivote/Arma");
            Assert.IsNotNull(arma);
            Assert.IsFalse(arma.gameObject.activeSelf, "El arma solo se abre entre a2 y a4.");
            Assert.AreEqual(LayerMask.NameToLayer(Capas.EnemyWeapon), arma.gameObject.layer);
            Assert.IsTrue(arma.GetComponent<BoxCollider2D>().isTrigger);
            Assert.AreEqual(Capas.Mascara(Capas.Player), arma.GetComponent<Hitbox>().Objetivos.value);

            var pecho = prefab.transform.Find("Pivote/Pecho");
            Assert.IsNotNull(pecho, "Las partículas salen de la cavidad del pecho, no del centro del sprite.");
            Assert.AreEqual((Vector3)ConstructorEcoIra.Pecho, pecho.localPosition);
            var estallido = pecho.GetComponent<ParticleSystem>();
            Assert.AreSame(estallido, animacionSerializada.FindProperty("estallido").objectReferenceValue);
            Assert.IsFalse(estallido.main.playOnAwake);
            Assert.AreEqual(ConstructorEcoIra.ShaderParticulas, pecho.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name);
        }

        [Test]
        public void ElArma_CubreElBrazoDeA2YA3()
        {
            // Alcance medido en el arte: el puño de a2 (estirado 1,15) llega a 1,29 u y el de a3 a 0,70 u.
            var arma = Prefab().transform.Find("Pivote/Arma");
            var caja = arma.GetComponent<BoxCollider2D>();
            var minX = arma.localPosition.x - caja.size.x / 2f;
            var maxX = arma.localPosition.x + caja.size.x / 2f;
            Assert.LessOrEqual(minX, 0.3f);
            Assert.GreaterOrEqual(maxX, 1.2f);
        }
    }
}
