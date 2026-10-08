using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Comprueba el arte de Kael tal como lo deja <see cref="ConstructorArteKael"/>: escala,
    /// pivotes, clips, controlador y prefab. Si alguien vuelve a exportar una hoja a 5× o cambia
    /// el lienzo en Pixelorama, estas pruebas lo señalan antes de que se vea en el juego.
    /// </summary>
    public class ArteKaelTests
    {
        private static readonly EditorCurveBinding CurvaSprite =
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");

        private static AnimatorController Controlador()
        {
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorArteKael.RutaControlador);
            Assert.IsNotNull(controlador, $"Falta {ConstructorArteKael.RutaControlador}: ejecuta Fragmentum → Regenerar todo.");
            return controlador;
        }

        private static AnimatorState Estado(string nombre)
        {
            var estado = Controlador().layers[0].stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == nombre);
            Assert.IsNotNull(estado, $"Kael.controller no tiene el estado '{nombre}'.");
            return estado;
        }

        /// <summary>Filas opacas (de abajo arriba) de un fotograma de una hoja, leídas del PNG.</summary>
        private static (int abajo, int arriba) FilasOpacas(ConstructorArteKael.Hoja hoja, int fotograma = 0)
        {
            var textura = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(textura.LoadImage(File.ReadAllBytes(ConstructorArteKael.RutaEnDisco(hoja))), hoja.Ruta);
                var ancho = textura.width / hoja.Fotogramas;
                var pixeles = textura.GetPixels32(); // fila 0 = la de abajo
                int abajo = int.MaxValue, arriba = -1;
                for (var y = 0; y < textura.height; y++)
                {
                    for (var x = fotograma * ancho; x < (fotograma + 1) * ancho; x++)
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

        // --- Hojas ---

        [Test]
        public void CadaHoja_SeRecortaEnSusFotogramas_A64PPU_SinFiltrar()
        {
            foreach (var hoja in ConstructorArteKael.Hojas)
            {
                var importador = AssetImporter.GetAtPath(hoja.Ruta) as TextureImporter;
                Assert.IsNotNull(importador, $"Falta {hoja.Ruta}.");
                Assert.AreEqual(SpriteImportMode.Multiple, importador.spriteImportMode, hoja.Ruta);
                Assert.AreEqual(ConstructorArteKael.PixelesPorUnidad, importador.spritePixelsPerUnit, hoja.Ruta);
                Assert.AreEqual(FilterMode.Point, importador.filterMode, $"{hoja.Ruta}: el pixel art se emborrona con otro filtro.");
                Assert.AreEqual(hoja.Fotogramas, ConstructorArteKael.Fotogramas(hoja).Length, hoja.Ruta);
            }
        }

        [Test]
        public void Kael_MideLoMismoQueSuCollider_EnTodasLasHojas()
        {
            foreach (var hoja in ConstructorArteKael.Hojas)
            {
                var (abajo, arriba) = FilasOpacas(hoja);
                var alto = (arriba - abajo + 1) / (float)ConstructorArteKael.PixelesPorUnidad;

                // El collider mide 1,8 u. Una hoja exportada a 5× daría unos 9 u.
                Assert.That(alto, Is.InRange(1.5f, 2.1f),
                    $"{hoja.Ruta}: Kael mide {alto:0.00} u. ¿Se exportó a otra escala? Redúcela con Herramientas/reducir_hoja.py.");
            }
        }

        [Test]
        public void ElPivoteDeCadaHoja_EstaEnLaPlantaDeLosPies()
        {
            foreach (var hoja in ConstructorArteKael.Hojas)
            {
                var (abajo, _) = FilasOpacas(hoja);
                Assert.AreEqual(hoja.Pivote.y, abajo,
                    $"{hoja.Ruta}: los pies están en la fila {abajo} y el pivote en la {hoja.Pivote.y}. " +
                    "Si cambiaste el lienzo, ajusta el pivote en ConstructorArteKael.");
            }
        }

        // --- Clips y controlador ---

        [Test]
        public void CadaEstadoDeLaFSM_TieneUnClipQueExisteEnElControlador()
        {
            var tabla = PlayerAnimacion.TablaPorDefecto();
            foreach (ESTADOS_JUGADOR estado in Enum.GetValues(typeof(ESTADOS_JUGADOR)))
            {
                Assert.IsTrue(tabla.Any(e => e.estado == estado), $"{estado} no está en la tabla de PlayerAnimacion.");
            }

            var nombres = Controlador().layers[0].stateMachine.states.Select(s => s.state.name).ToList();
            foreach (var entrada in tabla.Where(e => !string.IsNullOrEmpty(e.estadoAnimator)))
            {
                Assert.Contains(entrada.estadoAnimator, nombres, $"{entrada.estado} pide '{entrada.estadoAnimator}'.");
            }
        }

        [Test]
        public void LosClips_TienenSusFotogramasYSuBucle()
        {
            foreach (var definicion in ConstructorArteKael.Clips)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(definicion.Ruta);
                Assert.IsNotNull(clip, $"Falta {definicion.Ruta}.");
                Assert.AreSame(clip, Estado(definicion.Estado).motion, $"El estado {definicion.Estado} no usa su clip.");
                Assert.AreEqual(definicion.Bucle, AnimationUtility.GetAnimationClipSettings(clip).loopTime, definicion.Ruta);
                Assert.AreEqual(definicion.Tiempos[definicion.Tiempos.Length - 1], clip.length, 0.001f,
                    $"{definicion.Ruta}: duración. Unity añade 1/60 s tras la última clave; la de cierre lo compensa.");

                var claves = AnimationUtility.GetObjectReferenceCurve(clip, CurvaSprite);
                Assert.AreEqual(definicion.Fotogramas.Length + 1, claves.Length, $"{definicion.Ruta}: una clave por fotograma y la de cierre.");
                for (var i = 0; i < definicion.Fotogramas.Length; i++)
                {
                    Assert.AreEqual($"{definicion.Hoja.Archivo}_{definicion.Fotogramas[i]}", claves[i].value.name, definicion.Ruta);
                }
            }
        }

        [Test]
        public void ElControlador_TieneLosParametrosQueEscribeElCodigo()
        {
            var parametros = Controlador().parameters;
            AnimatorControllerParameter Parametro(string nombre)
            {
                var p = parametros.FirstOrDefault(x => x.name == nombre);
                Assert.IsNotNull(p, $"Falta el parámetro {nombre}: el código avisaría por consola.");
                return p;
            }

            Assert.AreEqual(AnimatorControllerParameterType.Float, Parametro(AnimatorParams.Movement.Nombre).type);
            Assert.AreEqual(AnimatorControllerParameterType.Bool, Parametro(AnimatorParams.Attack.Nombre).type);
            Assert.AreEqual(AnimatorControllerParameterType.Bool, Parametro(AnimatorParams.Damage.Nombre).type);
            Assert.AreEqual(AnimatorControllerParameterType.Bool, Parametro(AnimatorParams.Death.Nombre).type);
            Assert.AreEqual(1f, Parametro(AnimatorParams.VelocidadAtaque.Nombre).defaultFloat,
                "Con 0 el ataque se quedaría congelado si nadie fijara la velocidad.");
            Assert.AreEqual(1f, Parametro(AnimatorParams.VelocidadDash.Nombre).defaultFloat,
                "Con 0 el dash se quedaría congelado si nadie fijara la velocidad.");

            Assert.AreEqual(AnimatorParams.Movement.Nombre, Estado(PlayerAnimacion.Correr).speedParameter,
                "Las piernas siguen la velocidad real: no patinan bajo el agua ni sobre hielo.");
            Assert.AreEqual(AnimatorParams.VelocidadAtaque.Nombre, Estado(PlayerAnimacion.Ataque).speedParameter);
            Assert.AreEqual(AnimatorParams.VelocidadDash.Nombre, Estado(PlayerAnimacion.Dash).speedParameter);
        }

        [Test]
        public void SoloSeEncadenanLaCaidaAgacharseYLevantarse_YNadaMasTieneTransicionesPropias()
        {
            var cadenas = new Dictionary<string, string>
            {
                { PlayerAnimacion.Caida, PlayerAnimacion.CaidaBucle },
                { PlayerAnimacion.Agacharse, PlayerAnimacion.AgachadoBucle },
                { PlayerAnimacion.Levantarse, PlayerAnimacion.Idle },
            };

            var maquina = Controlador().layers[0].stateMachine;
            Assert.AreEqual(PlayerAnimacion.Idle, maquina.defaultState.name);
            Assert.IsEmpty(maquina.anyStateTransitions, "La FSM del código decide: el Animator no tiene transiciones desde Any State.");
            foreach (var hijo in maquina.states)
            {
                if (cadenas.TryGetValue(hijo.state.name, out var siguiente))
                {
                    Assert.AreEqual(1, hijo.state.transitions.Length, hijo.state.name);
                    Assert.AreEqual(siguiente, hijo.state.transitions[0].destinationState.name, $"{hijo.state.name} encadena con {siguiente}.");
                    Assert.AreEqual(1f, hijo.state.transitions[0].exitTime, $"{hijo.state.name}: al terminar el clip.");
                }
                else
                {
                    Assert.IsEmpty(hijo.state.transitions, $"{hijo.state.name} no debe tener transiciones propias.");
                }
            }
        }

        [Test]
        public void ElTajoDelAtaque_SeVeMientrasLaHitboxEstaAbierta()
        {
            var p = new ParametrosJugador();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ConstructorArteKael.CarpetaAnimaciones}/Kael_{PlayerAnimacion.Ataque}.anim");
            Assert.IsNotNull(clip);
            Assert.AreEqual(1f, clip.length, 0.001f, "El clip dura 1 s; PlayerAnimacion lo estira a la duración del golpe.");

            // En tiempo normalizado del clip (0 a 1), igual para el ataque normal y el cargado.
            var abre = p.inicioVentanaGolpe / p.duracionAtaque;
            var cierra = p.finVentanaGolpe / p.duracionAtaque;
            var claves = AnimationUtility.GetObjectReferenceCurve(clip, CurvaSprite);

            // Fotograma 4: la espada sale por arriba. Fotograma 5: el tajo hacia delante.
            foreach (var fotograma in new[] { 4, 5 })
            {
                Assert.That(claves[fotograma].time, Is.InRange(abre, cierra),
                    $"El fotograma {fotograma} del ataque debe verse con la hitbox abierta ({abre:0.00} a {cierra:0.00}).");
            }
        }

        [Test]
        public void ElDash_TieneSusCuatroFotogramasPorIgual_YDuraUnSegundo()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ConstructorArteKael.CarpetaAnimaciones}/Kael_{PlayerAnimacion.Dash}.anim");
            Assert.IsNotNull(clip, "Falta el clip del dash.");
            Assert.AreEqual(1f, clip.length, 0.001f, "El clip dura 1 s; PlayerAnimacion lo estira a la duración del dash.");
            Assert.IsFalse(clip.isLooping, "Un dash, una vez: si durara más, se queda en la frenada.");

            // Como en el proyecto de Pixelorama (Dash.pxo): los cuatro fotogramas duran lo mismo.
            var claves = AnimationUtility.GetObjectReferenceCurve(clip, CurvaSprite);
            for (var i = 0; i < 4; i++)
            {
                Assert.AreEqual(i * 0.25f, claves[i].time, 0.001f, $"Kael_Dash_{i}");
                Assert.AreEqual($"Kael_Dash_{i}", claves[i].value.name);
            }

            Assert.AreEqual(PlayerAnimacion.Dash, PlayerAnimacion.TablaPorDefecto().Single(e => e.estado == ESTADOS_JUGADOR.DASH).estadoAnimator,
                "El estado DASH ya tiene su clip: no usa el de correr.");
        }

        [Test]
        public void ElDash_EstaA1x_ConElPivoteEnLosPies()
        {
            // Un fotograma del dash mide 120 px: la hoja de 2400 que exporta Pixelorama, a 5×, no.
            var dash = ConstructorArteKael.Fotogramas(ConstructorArteKael.HojaDash);
            Assert.AreEqual(4, dash.Length);
            Assert.AreEqual(new Vector2(120f, 120f), dash[0].rect.size, "Lienzo de 120×120 a 1×.");

            // Cambiar de idle o de correr al dash no desplaza a Kael: los pies, en la fila del
            // pivote como en el idle, y la x en el torso de los dos primeros fotogramas (medido).
            var idle = ConstructorArteKael.Fotogramas(ConstructorArteKael.HojaIdle)[0];
            Assert.AreEqual(idle.pivot.y, dash[0].pivot.y, "Los pies, en la planta.");
            Assert.AreEqual(42f, dash[0].pivot.x, 0.01f);
        }

        [Test]
        public void Agacharse_BajaSeQuedaAgachado_YAlSoltarSeLevanta()
        {
            var tabla = PlayerAnimacion.TablaPorDefecto();
            Assert.AreEqual(PlayerAnimacion.Agacharse, tabla.Single(e => e.estado == ESTADOS_JUGADOR.CROUCH).estadoAnimator,
                "CROUCH ya tiene su clip: no usa el idle.");
            Assert.AreEqual(PlayerAnimacion.Levantarse, PlayerAnimacion.ClipDePaso(ESTADOS_JUGADOR.CROUCH, ESTADOS_JUGADOR.IDLE));
            Assert.IsNull(PlayerAnimacion.ClipDePaso(ESTADOS_JUGADOR.CROUCH, ESTADOS_JUGADOR.FALL), "Si cae, cae: sin levantarse.");
            Assert.IsNull(PlayerAnimacion.ClipDePaso(ESTADOS_JUGADOR.RUN, ESTADOS_JUGADOR.IDLE));

            var baja = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ConstructorArteKael.CarpetaAnimaciones}/Kael_{PlayerAnimacion.Agacharse}.anim");
            Assert.IsNotNull(baja);
            Assert.LessOrEqual(baja.length, 0.2f, "Agacharse responde enseguida: el collider ya se encogió.");
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ConstructorArteKael.CarpetaAnimaciones}/Kael_{PlayerAnimacion.AgachadoBucle}.anim").isLooping);

            // Agachado (fotogramas 3 y 4) cabe bajo el techo bajo de PruebaJugador, a 1,2 u del suelo.
            foreach (var fotograma in new[] { 3, 4 })
            {
                var (abajo, arriba) = FilasOpacas(ConstructorArteKael.HojaAgachado, fotograma);
                Assert.AreEqual(0, abajo, $"Kael_Agachado_{fotograma}: los pies, en la planta.");
                Assert.Less((arriba - abajo + 1) / (float)ConstructorArteKael.PixelesPorUnidad, 1.2f,
                    $"Kael_Agachado_{fotograma}: agachado debe caber bajo un techo bajo.");
            }

            Assert.AreEqual(39f, ConstructorArteKael.Fotogramas(ConstructorArteKael.HojaAgachado)[0].pivot.x, 0.01f,
                "El torso de pie, donde el del idle: agacharse no desplaza a Kael.");
        }

        [Test]
        public void ElColliderAgachado_MideLoQueElDibujoAgachado()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorJugador.RutaPrefab);
            Assert.IsNotNull(prefab);
            var cuerpo = prefab.GetComponent<CapsuleCollider2D>();
            var parametros = prefab.GetComponent<PlayerController>().Parametros;
            var agachado = cuerpo.size.y * parametros.alturaAgachado;

            foreach (var fotograma in new[] { 3, 4 })
            {
                var (abajo, arriba) = FilasOpacas(ConstructorArteKael.HojaAgachado, fotograma);
                var dibujo = (arriba - abajo + 1) / (float)ConstructorArteKael.PixelesPorUnidad;
                Assert.AreEqual(dibujo, agachado, 0.03f,
                    $"Kael_Agachado_{fotograma}: el collider agachado ({agachado:0.00} u) debe medir lo que el dibujo ({dibujo:0.00} u).");
            }
        }

        [Test]
        public void Morir_TieneSusTresFotogramasA5fps_YSeQuedaDeRodillas()
        {
            Assert.AreEqual(PlayerAnimacion.Morir, PlayerAnimacion.TablaPorDefecto().Single(e => e.estado == ESTADOS_JUGADOR.DEAD).estadoAnimator,
                "DEAD ya tiene su clip: no usa el idle.");

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ConstructorArteKael.CarpetaAnimaciones}/Kael_{PlayerAnimacion.Morir}.anim");
            Assert.IsNotNull(clip, "Falta el clip de morir.");
            Assert.AreEqual(0.6f, clip.length, 0.001f, "Como en Morir.pxo: tres fotogramas a 5 fps.");
            Assert.IsFalse(clip.isLooping, "Cae una vez y se queda de rodillas hasta reaparecer.");
            Assert.IsEmpty(Estado(PlayerAnimacion.Morir).transitions);

            var claves = AnimationUtility.GetObjectReferenceCurve(clip, CurvaSprite);
            for (var i = 0; i < 3; i++)
            {
                Assert.AreEqual(i * 0.2f, claves[i].time, 0.001f, $"Kael_Morir_{i}");
                Assert.AreEqual($"Kael_Morir_{i}", claves[i].value.name);
            }
        }

        // --- Prefab ---

        [Test]
        public void ElPrefabDelJugador_UsaElArteDeKael_SinEscalar()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorJugador.RutaPrefab);
            Assert.IsNotNull(prefab, $"Falta {ConstructorJugador.RutaPrefab}.");

            var objetoSprite = prefab.transform.Find("Pivote/Sprite");
            Assert.IsNotNull(objetoSprite);
            var renderer2d = objetoSprite.GetComponent<SpriteRenderer>();
            Assert.AreEqual($"{ConstructorArteKael.HojaIdle.Archivo}_0", renderer2d.sprite.name);
            Assert.AreEqual(Color.white, renderer2d.color, "El arte no se tiñe: PlayerVisual parte de este color.");

            // El pivote del sprite ya está en los pies: nada de localScale (en la v1 era 0,215 y rompía el wall jump).
            Assert.AreEqual(Vector3.zero, objetoSprite.localPosition);
            Assert.AreEqual(Vector3.one, objetoSprite.localScale);

            Assert.AreSame(ConstructorArteKael.Controlador, objetoSprite.GetComponent<Animator>().runtimeAnimatorController);
            Assert.IsNotNull(prefab.GetComponent<PlayerAnimacion>());
        }
    }
}
