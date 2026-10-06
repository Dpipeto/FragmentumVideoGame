using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
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
    ///
    /// El controlador tiene un estado por clip, sin estados sueltos ni transiciones rotas: el
    /// rugido y la muerte interrumpen cualquier estado, y la muerte no tiene salida.
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

        // --- Controlador ---

        private static AnimatorController Controlador()
        {
            var controlador = ConstructorCentinela.Controlador;
            Assert.IsNotNull(controlador, $"Falta {ConstructorCentinela.RutaControlador}: ejecuta Fragmentum → Regenerar todo.");
            return controlador;
        }

        private static Dictionary<string, AnimatorState> Estados() =>
            Controlador().layers[0].stateMachine.states.Select(s => s.state).ToDictionary(s => s.name);

        private static string Descripcion(AnimatorStateTransition t) =>
            $"{t.destinationState.name}:{(t.hasExitTime ? t.exitTime.ToString("0.###", CultureInfo.InvariantCulture) : "-")}:" +
            string.Join(",", t.conditions.Select(c => $"{c.mode}{c.parameter}"));

        private static readonly string[] Ataques =
        {
            ConstructorCentinela.Espadazo, ConstructorCentinela.EspadaClavada, ConstructorCentinela.GolpeSuelo,
            ConstructorCentinela.Salto, ConstructorCentinela.Rugido,
        };

        [Test]
        public void ElControlador_TieneUnEstadoPorClip_YEmpiezaEnIdle()
        {
            var estados = Estados();
            CollectionAssert.AreEquivalent(ConstructorCentinela.Clips.Select(c => c.Estado), estados.Keys);
            Assert.AreEqual(ConstructorCentinela.Idle, Controlador().layers[0].stateMachine.defaultState.name);
            Assert.AreEqual(1, Controlador().layers.Length);

            foreach (var estado in estados.Values)
            {
                Assert.AreSame(Clip(estado.name), estado.motion, $"{estado.name} reproduce su clip.");
                Assert.AreEqual(1f, estado.speed, $"{estado.name}: los tiempos del LEEME, sin acelerar ni frenar.");
                Assert.IsFalse(estado.speedParameterActive, estado.name);
                Assert.IsTrue(estado.writeDefaultValues, $"{estado.name}: al salir de EspadaClavada el forcejeo vuelve a 0.");
            }

            CollectionAssert.AreEqual(
                new[] { "Walk:Bool", "Espadazo:Trigger", "Clavar:Bool", "GolpeSuelo:Trigger", "Salto:Trigger", "Rugido:Trigger", "Death:Bool" },
                Controlador().parameters.Select(p => $"{p.name}:{p.type}"));
        }

        [Test]
        public void LaMuerteYElRugido_InterrumpenCualquierEstado_YLaMuerteNoTieneSalida()
        {
            var maquina = Controlador().layers[0].stateMachine;
            CollectionAssert.AreEqual(
                new[]
                {
                    "Muerte:-:IfDeath",
                    "Rugido:-:IfRugido,IfNotDeath",
                    "Espadazo:-:IfEspadazo,IfNotDeath",
                    "GolpeSuelo:-:IfGolpeSuelo,IfNotDeath",
                    "Salto:-:IfSalto,IfNotDeath",
                },
                maquina.anyStateTransitions.Select(Descripcion),
                "La muerte va la primera; los demás no la interrumpen.");

            var repiten = maquina.anyStateTransitions.Where(t => t.canTransitionToSelf).Select(t => t.destinationState.name);
            CollectionAssert.AreEquivalent(new[] { ConstructorCentinela.Espadazo, ConstructorCentinela.GolpeSuelo }, repiten,
                "Solo se repiten los combos de espadazos y la onda doble; la muerte y el rugido no se reinician.");

            Assert.IsEmpty(Estados()[ConstructorCentinela.Muerte].transitions, "D4, la ceniza con el fragmento, se queda en pantalla.");
        }

        [Test]
        public void ElEspadazo_ClavaLaEspadaAlAcabarE2_OSigueHastaE4()
        {
            var espadazo = ConstructorCentinela.ClipDe(ConstructorCentinela.Espadazo);
            var finE2 = (0.55f + 0.08f) / espadazo.Duracion;
            var clavar = Estados()[ConstructorCentinela.Espadazo].transitions[0];
            Assert.AreEqual(ConstructorCentinela.EspadaClavada, clavar.destinationState.name);
            Assert.IsTrue(clavar.hasExitTime);
            Assert.AreEqual(finE2, clavar.exitTime, 1e-4f, "Justo cuando empezaría E3.");
            Assert.AreEqual("IfClavar", string.Join(",", clavar.conditions.Select(c => $"{c.mode}{c.parameter}")));
        }

        [Test]
        public void CadaAtaque_VuelveAIdleAlAcabar_OAAndarSiYaCamina()
        {
            var estados = Estados();
            CollectionAssert.AreEqual(new[] { "Andar:-:IfWalk" }, estados[ConstructorCentinela.Idle].transitions.Select(Descripcion));
            CollectionAssert.AreEqual(new[] { "Idle:-:IfNotWalk" }, estados[ConstructorCentinela.Andar].transitions.Select(Descripcion));

            foreach (var ataque in Ataques)
            {
                // Andar va antes: si ya camina, no se ve ni un fotograma del idle frontal.
                var finales = estados[ataque].transitions.Skip(ataque == ConstructorCentinela.Espadazo ? 1 : 0).Select(Descripcion);
                CollectionAssert.AreEqual(new[] { "Andar:1:IfWalk", "Idle:1:" }, finales, ataque);
            }
        }

        [Test]
        public void NingunEstadoSuelto_NiTransicionesRotas_NiMezclas()
        {
            var controlador = Controlador();
            var maquina = controlador.layers[0].stateMachine;
            var estados = Estados();
            var parametros = controlador.parameters.ToDictionary(p => p.name, p => p.type);
            var todas = estados.Values.SelectMany(e => e.transitions).Concat(maquina.anyStateTransitions).ToList();

            foreach (var t in todas)
            {
                Assert.IsNotNull(t.destinationState, $"{t.name}: sin destino.");
                Assert.IsTrue(estados.ContainsValue(t.destinationState), $"{t.name}: el destino no está en el controlador.");
                Assert.IsTrue(t.hasFixedDuration, t.name);
                Assert.AreEqual(0f, t.duration, $"{t.name}: sin mezcla entre fotogramas de pixel art.");
                Assert.AreEqual(0f, t.offset, t.name);
                Assert.IsTrue(t.hasExitTime || t.conditions.Length > 0, $"{t.name}: sin condición ni tiempo de salida se tomaría siempre.");
                foreach (var condicion in t.conditions)
                {
                    Assert.IsTrue(parametros.TryGetValue(condicion.parameter, out var tipo), $"{t.name}: falta el parámetro {condicion.parameter}.");
                    Assert.That(tipo, Is.EqualTo(AnimatorControllerParameterType.Bool).Or.EqualTo(AnimatorControllerParameterType.Trigger), condicion.parameter);
                    Assert.That(condicion.mode, Is.EqualTo(AnimatorConditionMode.If).Or.EqualTo(AnimatorConditionMode.IfNot), condicion.parameter);
                }
            }

            foreach (var estado in estados.Values.Where(e => e != maquina.defaultState))
            {
                Assert.IsTrue(todas.Any(t => t.destinationState == estado), $"Nada lleva a {estado.name}.");
            }

            // Solo los ataques esperan a acabar su clip (y el espadazo, a acabar E2 para clavar).
            foreach (var estado in estados.Values)
            {
                Assert.AreEqual(Ataques.Contains(estado.name), estado.transitions.Any(t => t.hasExitTime), estado.name);
            }

            Assert.IsFalse(maquina.anyStateTransitions.Any(t => t.hasExitTime), "Lo que interrumpe no espera.");
        }

        // --- Eventos de animación ---

        [Test]
        public void LosEventos_CaenAlEmpezarSuFotograma_YLasVentanasDuranLoQueSeVen()
        {
            // Copiados del diseño, no leídos de la tabla: Golpe en E2, G3 y J2; ventanas de E3 y C2.
            var esperados = new Dictionary<string, string[]>
            {
                [ConstructorCentinela.Idle] = new string[0],
                [ConstructorCentinela.Andar] = new string[0],
                [ConstructorCentinela.Espadazo] = new[] { "Golpe@0.55", "FinGolpe@0.63", "VentanaCorta@0.63(0.22)" },
                [ConstructorCentinela.EspadaClavada] = new[] { "FinGolpe@0", "Ventana@0.09(1.2)" },
                [ConstructorCentinela.GolpeSuelo] = new[] { "Golpe@0.57" },
                [ConstructorCentinela.Rugido] = new string[0],
                [ConstructorCentinela.Salto] = new[] { "Golpe@0.5", "EnElAire@1.033", "Aterrizaje@1.05" },
                [ConstructorCentinela.Muerte] = new[] { "FinMuerte@2.1" },
            };

            string Describir(AnimationEvent e) =>
                $"{e.functionName}@{e.time.ToString("0.###", CultureInfo.InvariantCulture)}" +
                (e.floatParameter != 0f ? $"({e.floatParameter.ToString("0.###", CultureInfo.InvariantCulture)})" : string.Empty);

            foreach (var definicion in ConstructorCentinela.Clips)
            {
                var eventos = AnimationUtility.GetAnimationEvents(Clip(definicion.Estado));
                CollectionAssert.AreEquivalent(esperados[definicion.Estado], eventos.Select(Describir), definicion.Estado);
            }
        }

        [Test]
        public void CadaEvento_LlamaAUnaFuncionQueExisteEnAnimacionJefe()
        {
            foreach (var definicion in ConstructorCentinela.Clips)
            {
                foreach (var evento in AnimationUtility.GetAnimationEvents(Clip(definicion.Estado)))
                {
                    var metodo = typeof(AnimacionJefe).GetMethod(evento.functionName);
                    Assert.IsNotNull(metodo, $"{definicion.Estado}: AnimacionJefe no tiene {evento.functionName}.");
                    Assert.IsTrue(metodo.IsPublic && metodo.ReturnType == typeof(void), evento.functionName);
                    var parametros = metodo.GetParameters();
                    Assert.That(parametros.Length == 0 || (parametros.Length == 1 && parametros[0].ParameterType == typeof(float)),
                        $"{evento.functionName}: un evento solo puede pasar un float.");
                }
            }
        }

        // --- Prefab ---

        private static GameObject PrefabCentinela()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GeneradorJefes.CarpetaPrefabs}/{ConstructorCentinela.NombreDatos}.prefab");
            Assert.IsNotNull(prefab, "Falta el prefab de la Centinela: ejecuta Fragmentum → Regenerar todo.");
            return prefab;
        }

        [Test]
        public void ElPrefab_LlevaElArte_ConElSpriteEnLosPies_YSinCapsula()
        {
            var prefab = PrefabCentinela();
            var arte = prefab.GetComponentInChildren<AnimacionJefe>(true);
            Assert.IsNotNull(arte, "El sprite lleva AnimacionJefe, que recibe los eventos.");
            Assert.AreEqual("Pivote", arte.transform.parent.name, "Se voltea con el pivote.");
            Assert.AreEqual(Vector3.zero, arte.transform.localPosition, "A altura 0: los pies en el origen del jefe y el forcejeo parte de ahí.");
            Assert.AreEqual(Vector3.one, arte.transform.localScale, "El tamaño lo dan los píxeles por unidad.");

            var sprite = arte.GetComponent<SpriteRenderer>();
            Assert.AreSame(Fotograma("I1"), sprite.sprite);
            Assert.AreEqual(Color.white, sprite.color);
            var animator = arte.GetComponent<Animator>();
            Assert.AreSame(Controlador(), animator.runtimeAnimatorController);
            Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, animator.cullingMode, "Los eventos llegan aunque esté fuera de cámara.");
            Assert.IsFalse(animator.applyRootMotion);
            Assert.IsNull(prefab.GetComponentInChildren<Core.SpriteProvisional>(true), "Con arte, sin cápsula provisional.");

            var serializado = new SerializedObject(arte);
            var anticipaciones = serializado.FindProperty("anticipaciones");
            var leidas = Enumerable.Range(0, anticipaciones.arraySize).Select(i => anticipaciones.GetArrayElementAtIndex(i))
                .Select(p => $"{p.FindPropertyRelative("estado").stringValue}@{p.FindPropertyRelative("segundos").floatValue.ToString("0.###", CultureInfo.InvariantCulture)}");
            CollectionAssert.AreEquivalent(new[] { "Espadazo@0.55", "GolpeSuelo@0.57", "Salto@0.5" }, leidas, "Del inicio de cada ataque a su golpe.");
            Assert.AreEqual(ConstructorEcoIra.RutaMaterialDestello,
                AssetDatabase.GetAssetPath(serializado.FindProperty("materialDestello").objectReferenceValue), "Destella con la silueta del Eco.");

            var jefe = new SerializedObject(prefab.GetComponent<Bosses.CentinelaArdiente>());
            Assert.AreSame(arte, jefe.FindProperty("arte").objectReferenceValue);
            Assert.IsNotNull(jefe.FindProperty("fragmento").objectReferenceValue);

            var contacto = prefab.GetComponentInChildren<Core.Combate.Hitbox>(true);
            Assert.IsFalse(contacto.gameObject.activeSelf, "En reposo no hace daño: el contacto se enciende al empezar el combate.");

            var datos = GeneradorJefes.CargarDatos(ConstructorCentinela.NombreDatos);
            Assert.AreEqual(Color.white, datos.tinte, "Con arte, sin tinte.");
            Assert.AreEqual(2.1f, datos.duracionDerrota, 1e-4f, "La muerte tarda 2,1 s en llegar a D4.");
        }

        [Test]
        public void ElFragmento_EstaEscondido_ConSuMascaraSobreElDeD4_YSinTocarLaCeniza()
        {
            var prefab = PrefabCentinela();
            var fragmento = prefab.GetComponentInChildren<FragmentoJefe>(true);
            Assert.IsNotNull(fragmento);
            Assert.IsFalse(fragmento.gameObject.activeSelf, "Aparece al llegar la muerte a D4.");
            Assert.AreEqual("Pivote", fragmento.transform.parent.name);
            Assert.AreEqual(0, fragmento.gameObject.layer, "Capa Default: la toca el jugador.");

            var zona = fragmento.GetComponent<BoxCollider2D>();
            Assert.IsTrue(zona.isTrigger);
            Assert.AreEqual(0f, fragmento.transform.localPosition.y + zona.offset.y - zona.size.y * 0.5f, 1e-4f, "Desde el suelo: se recoge sin saltar.");

            var mascara = fragmento.GetComponentInChildren<SpriteMask>(true);
            Assert.IsFalse(mascara.enabled, "La máscara solo borra el fragmento al recogerlo.");

            // La máscara en píxeles de D4 (fila 0 abajo), respecto del pivote.
            var centro = (Vector2)(fragmento.transform.localPosition + mascara.transform.localPosition) * ConstructorCentinela.PixelesPorUnidad;
            var medio = Vector2.Scale(mascara.transform.localScale, mascara.sprite.bounds.size) * (ConstructorCentinela.PixelesPorUnidad * 0.5f);
            var pivote = new Vector2(ConstructorCentinela.Pivote.x * ConstructorCentinela.AnchoLienzo, ConstructorCentinela.Pivote.y * ConstructorCentinela.AltoLienzo);
            var minimo = pivote + centro - medio;
            var maximo = pivote + centro + medio;

            var textura = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(textura.LoadImage(File.ReadAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "..", ConstructorCentinela.RutaSprite("D4"))))));
                var pixeles = textura.GetPixels32();
                int ancho = textura.width, alto = textura.height;
                bool Opaco(int x, int y) => pixeles[y * ancho + x].a > 0;
                bool Dentro(int x, int y) => x + 0.5f > minimo.x && x + 0.5f < maximo.x && y + 0.5f > minimo.y && y + 0.5f < maximo.y;

                // El fragmento: las filas opacas de arriba, hasta la primera fila vacía.
                var arriba = Enumerable.Range(0, alto).Reverse().First(y => Enumerable.Range(0, ancho).Any(x => Opaco(x, y)));
                var abajo = arriba;
                while (Enumerable.Range(0, ancho).Any(x => Opaco(x, abajo - 1))) abajo--;

                for (var y = 0; y < alto; y++)
                {
                    for (var x = 0; x < ancho; x++)
                    {
                        if (!Opaco(x, y))
                        {
                            continue;
                        }

                        var delFragmento = y >= abajo;
                        Assert.AreEqual(delFragmento, Dentro(x, y), delFragmento ? $"La máscara no tapa el píxel ({x}, {y}) del fragmento." : $"La máscara borraría ceniza en ({x}, {y}).");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(textura);
            }
        }

        [Test]
        public void ElImportadorDeJefes_DaAjustesDePixelArt_AOtroJefeSinTabla()
        {
            const string carpeta = TablaArte.CarpetaJefes + "/_PruebaImportador";
            var ruta = carpeta + "/Fotograma.png";
            var textura = new Texture2D(4, 4);
            try
            {
                Assert.IsTrue(AssetDatabase.IsValidFolder(TablaArte.CarpetaJefes), $"Falta {TablaArte.CarpetaJefes}.");
                if (!AssetDatabase.IsValidFolder(carpeta))
                {
                    AssetDatabase.CreateFolder(TablaArte.CarpetaJefes, "_PruebaImportador");
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
