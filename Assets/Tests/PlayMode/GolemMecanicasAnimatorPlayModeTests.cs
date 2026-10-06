using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Levels.Mecanicas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Los Animators del Golem de Ceniza y de las mecánicas reproduciendo de verdad, con los
    /// controladores generados y los parámetros que pondrán la FSM del enemigo y los scripts de
    /// las mecánicas:
    /// - el Golem respira en Idle, anda al ritmo de su velocidad y se para a respirar, ataca con
    ///   medio segundo de anticipación y un impacto de 0,08 s, y la muerte se queda en D4;
    /// - cada mecánica muestra el fotograma del estado que pide su script y no cambia sola.
    /// </summary>
    public class GolemMecanicasAnimatorPlayModeTests : PruebaPlayModeBase
    {
        private const string RutaGolem = "Assets/Animaciones/Enemigos/GolemCeniza/GolemCeniza.controller";
        private const string CarpetaMecanicas = "Assets/Animaciones/Mecanicas";

        // Margen de los tiempos: la prueba ve el fotograma con un frame de retraso.
        private const float Margen = 0.05f;

        private Animator _animator;
        private SpriteRenderer _sprite;

        private string Fotograma => _sprite.sprite != null ? _sprite.sprite.name : string.Empty;

        private IEnumerator Crear(string nombre, string rutaControlador)
        {
            var objeto = new GameObject(nombre);
            Creados.Add(objeto);
            _sprite = objeto.AddComponent<SpriteRenderer>();
            _animator = objeto.AddComponent<Animator>();
            _animator.runtimeAnimatorController = CargarAsset<RuntimeAnimatorController>(rutaControlador);
            yield return null;
        }

        /// <summary>Cada fotograma que aparece y cuándo.</summary>
        private sealed class Registro
        {
            public readonly List<(string fotograma, float inicio)> Fotogramas = new List<(string, float)>();

            public string[] Secuencia => Fotogramas.Select(f => f.fotograma).ToArray();

            /// <summary>Cuánto se vio el fotograma i (hasta que apareció el siguiente).</summary>
            public float Duracion(int i) => Fotogramas[i + 1].inicio - Fotogramas[i].inicio;
        }

        private IEnumerator Observar(float segundos, Registro registro, System.Action<float> cadaFrame = null)
        {
            var inicio = Time.time;
            return Durante(segundos, () =>
            {
                cadaFrame?.Invoke(Time.time - inicio);
                var fotograma = Fotograma;
                if (registro.Fotogramas.Count == 0 || registro.Fotogramas[registro.Fotogramas.Count - 1].fotograma != fotograma)
                {
                    registro.Fotogramas.Add((fotograma, Time.time));
                }
            });
        }

        private void Fijar(in AnimatorParams.Parametro parametro, bool valor) => _animator.SetBool(parametro.Hash, valor);

        // --- Golem ---

        [UnityTest]
        public IEnumerator ElGolem_RespiraEnIdle_YAndaAlRitmoDeSuVelocidad()
        {
            yield return Crear("Golem", RutaGolem);
            Assert.AreEqual("Golem_I1", Fotograma, "Empieza en Idle.");

            var respira = new Registro();
            yield return Observar(1.75f, respira);
            CollectionAssert.AreEqual(new[] { "Golem_I1", "Golem_I2", "Golem_I3", "Golem_I2", "Golem_I1" }, respira.Secuencia.Take(5), "I1 · I2 · I3 · I2.");
            for (var i = 0; i < 4; i++)
            {
                Assert.AreEqual(0.40f, respira.Duracion(i), Margen, $"{respira.Fotogramas[i].fotograma} del idle.");
            }

            // A su velocidad natural (persiguiendo), el andar va a los tiempos de la tabla.
            _animator.SetFloat(AnimatorParams.VelocidadCorrer.Hash, 1f);
            Fijar(AnimatorParams.MovEn, true);
            yield return null;
            yield return EsperarHasta(() => Fotograma == "Golem_W1", 0.2f, "echar a andar");
            var anda = new Registro();
            yield return Observar(0.6f, anda);
            CollectionAssert.AreEqual(new[] { "Golem_W1", "Golem_W2", "Golem_W3", "Golem_W4", "Golem_W1" }, anda.Secuencia.Take(5));
            for (var i = 1; i < 4; i++)
            {
                Assert.AreEqual(0.13f, anda.Duracion(i), Margen, $"{anda.Fotogramas[i].fotograma} a velocidad natural.");
            }

            // A la mitad de velocidad (patrullando), a la mitad de ritmo.
            _animator.SetFloat(AnimatorParams.VelocidadCorrer.Hash, 0.5f);
            var despacio = new Registro();
            yield return Observar(1.2f, despacio);
            var medidas = Enumerable.Range(1, despacio.Fotogramas.Count - 2).Select(despacio.Duracion).ToList();
            Assert.AreEqual(0.26f, medidas.Average(), Margen, "A media velocidad, cada fotograma dura el doble.");

            // Parado (esperando a poder volver a golpear), respira.
            _animator.SetFloat(AnimatorParams.VelocidadCorrer.Hash, 0f);
            yield return EsperarHasta(() => Fotograma == "Golem_I1", 0.2f, "pararse a respirar");
        }

        [UnityTest]
        public IEnumerator ElAtaqueDelGolem_AnticipaMedioSegundo_GolpeaEn0_08_YVuelveAIdle()
        {
            yield return Crear("Golem", RutaGolem);

            // Como la FSM: Attack se enciende al entrar en ATTACK y se apaga al acabar el clip.
            Fijar(AnimatorParams.Attack, true);
            var registro = new Registro();
            yield return Observar(1.3f, registro, t =>
            {
                if (t >= 0.98f)
                {
                    Fijar(AnimatorParams.Attack, false);
                }
            });

            CollectionAssert.AreEqual(new[] { "Golem_I1", "Golem_A1", "Golem_A3", "Golem_A4", "Golem_A5", "Golem_I1" }, registro.Secuencia,
                "De A1 a A3 sin fotograma intermedio y, al acabar, el idle.");
            var duraciones = new[] { 0.50f, 0.08f, 0.14f, 0.26f };
            for (var i = 0; i < duraciones.Length; i++)
            {
                Assert.AreEqual(duraciones[i], registro.Duracion(i + 1), Margen, registro.Fotogramas[i + 1].fotograma);
            }

            Assert.Greater(registro.Duracion(1), 4f * registro.Duracion(2), "La anticipación se ve venir; el impacto, no.");
        }

        [UnityTest]
        public IEnumerator ElGolem_AtacaAndando_YSiLaFsmCortaElAtaqueVuelveAIdle()
        {
            yield return Crear("Golem", RutaGolem);
            _animator.SetFloat(AnimatorParams.VelocidadCorrer.Hash, 1f);
            Fijar(AnimatorParams.MovEn, true);
            yield return EsperarHasta(() => Fotograma.StartsWith("Golem_W"), 0.3f, "echar a andar");

            // Al entrar en ATTACK la FSM apaga MovEn y enciende Attack en el mismo fotograma.
            Fijar(AnimatorParams.MovEn, false);
            Fijar(AnimatorParams.Attack, true);
            yield return EsperarHasta(() => Fotograma == "Golem_A1", 0.1f, "atacar andando");

            yield return Durante(0.2f, () => { });
            Fijar(AnimatorParams.Attack, false);
            yield return EsperarHasta(() => Fotograma == "Golem_I1", 0.1f, "volver a Idle si el ataque se corta");
        }

        [UnityTest]
        public IEnumerator LaMuerte_InterrumpeElAtaque_YSeQuedaEnD4()
        {
            yield return Crear("Golem", RutaGolem);
            Fijar(AnimatorParams.Attack, true);
            yield return Durante(0.2f, () => { });
            Assert.AreEqual("Golem_A1", Fotograma);

            Fijar(AnimatorParams.Death, true);
            var registro = new Registro();
            yield return Observar(2f, registro, t =>
            {
                // Lo que pase después no la saca de la muerte.
                if (t > 1.5f)
                {
                    Fijar(AnimatorParams.Attack, false);
                    Fijar(AnimatorParams.MovEn, true);
                }
            });

            CollectionAssert.AreEqual(new[] { "Golem_A1", "Golem_D1", "Golem_D2", "Golem_D3", "Golem_D4" }, registro.Secuencia);
            var duraciones = new[] { 0.35f, 0.45f, 0.50f };
            for (var i = 0; i < duraciones.Length; i++)
            {
                Assert.AreEqual(duraciones[i], registro.Duracion(i + 1), Margen, registro.Fotogramas[i + 1].fotograma);
            }

            Assert.AreEqual("Golem_D4", Fotograma, "Los restos se quedan.");
        }

        // --- Mecánicas ---

        [UnityTest]
        public IEnumerator CadaMecanica_MuestraElEstadoQuePideSuScript_YNoCambiaSola()
        {
            var mecanicas = new (string controlador, string[] fotogramas)[]
            {
                ("Torreta", new[] { "Torreta_1_reposo", "Torreta_2_carga", "Torreta_3_disparo" }),
                ("Plataforma", new[] { "Plataforma_1_intacta", "Plataforma_2_agrietada", "Plataforma_3_rota" }),
                ("Combustion", new[] { "Combustion_1_apagada", "Combustion_2_calienta", "Combustion_3_encendida" }),
            };

            foreach (var (nombre, fotogramas) in mecanicas)
            {
                yield return Crear(nombre, $"{CarpetaMecanicas}/{nombre}.controller");
                Assert.AreEqual(fotogramas[0], Fotograma, $"{nombre}: empieza en el primer estado.");

                for (var estado = 1; estado < fotogramas.Length; estado++)
                {
                    _animator.SetInteger(AnimatorParams.EstadoMecanica.Hash, estado);
                    var esperado = fotogramas[estado];
                    yield return EsperarHasta(() => Fotograma == esperado, 0.1f, $"{nombre}: Estado = {estado}");
                    if (nombre != "Plataforma" || estado < 2)
                    {
                        yield return ComprobarQueNunca(() => Fotograma != esperado, 0.5f, $"{nombre}: {esperado} se queda mientras el script no cambie el estado");
                    }
                }

                _animator.SetInteger(AnimatorParams.EstadoMecanica.Hash, 0);
                var primero = fotogramas[0];
                yield return EsperarHasta(() => Fotograma == primero, 0.1f, $"{nombre}: vuelve al primer estado");
            }
        }

        [UnityTest]
        public IEnumerator LaPlataforma_SeVePartida0_25s_YDespuesSoloElPolvo()
        {
            yield return Crear("Plataforma", $"{CarpetaMecanicas}/Plataforma.controller");
            _animator.SetInteger(AnimatorParams.EstadoMecanica.Hash, (int)ESTADOS_PLATAFORMA.BROKEN);
            var registro = new Registro();
            yield return Observar(1f, registro);

            CollectionAssert.AreEqual(new[] { "Plataforma_1_intacta", "Plataforma_3_rota", "Plataforma_4_polvo" }, registro.Secuencia);
            Assert.AreEqual(0.25f, registro.Duracion(1), Margen, "Partida en tres trozos, un momento.");
            Assert.AreEqual("Plataforma_4_polvo", Fotograma, "El polvo se queda hasta que el script la esconde.");
        }
    }
}
