using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// El Animator de la Centinela Ardiente reproduciendo de verdad, con el controlador generado y
    /// los parámetros que usará el jefe: cada ataque pasa por sus fotogramas con los tiempos del
    /// LEEME, la espada se clava al acabar E2 y forcejea solo en C2, el rugido y la muerte
    /// interrumpen lo que haya, y la muerte se queda en D4 pase lo que pase.
    /// </summary>
    public class CentinelaAnimatorPlayModeTests : PruebaPlayModeBase
    {
        private const string RutaControlador = "Assets/Animaciones/Jefes/Centinela/CentinelaArdiente.controller";

        // Margen de los tiempos: la prueba ve el fotograma con un frame de retraso.
        private const float Margen = 0.05f;

        private Animator _animator;
        private SpriteRenderer _sprite;

        private string Fotograma => _sprite.sprite != null ? _sprite.sprite.name : string.Empty;

        private float Altura => _sprite.transform.localPosition.y;

        private IEnumerator CrearCentinela()
        {
            var raiz = new GameObject("Centinela");
            Creados.Add(raiz);
            var objetoSprite = new GameObject("Sprite");
            objetoSprite.transform.SetParent(raiz.transform, false);
            _sprite = objetoSprite.AddComponent<SpriteRenderer>();
            _animator = objetoSprite.AddComponent<Animator>();
            _animator.runtimeAnimatorController = CargarAsset<RuntimeAnimatorController>(RutaControlador);
            objetoSprite.AddComponent<ReceptorEventosJefe>();
            yield return null;
            Assert.AreEqual("I1", Fotograma, "Empieza en el idle frontal.");
        }

        /// <summary>Cada fotograma que aparece, cuándo, y las alturas del sprite mientras se ve.</summary>
        private sealed class Registro
        {
            public readonly List<(string fotograma, float inicio)> Fotogramas = new List<(string, float)>();
            public readonly List<(string fotograma, float altura)> Alturas = new List<(string, float)>();

            public string[] Secuencia => Fotogramas.Select(f => f.fotograma).ToArray();

            /// <summary>Cuánto se vio el fotograma i (hasta que apareció el siguiente).</summary>
            public float Duracion(int i) => Fotogramas[i + 1].inicio - Fotogramas[i].inicio;
        }

        private IEnumerator Observar(float segundos, Registro registro) =>
            Durante(segundos, () =>
            {
                var fotograma = Fotograma;
                if (registro.Fotogramas.Count == 0 || registro.Fotogramas[registro.Fotogramas.Count - 1].fotograma != fotograma)
                {
                    registro.Fotogramas.Add((fotograma, Time.time));
                }

                registro.Alturas.Add((fotograma, Altura));
            });

        // --- Criterio: la anticipación dura visiblemente más que el golpe ---

        [UnityTest]
        public IEnumerator CadaAtaque_PasaPorSusFotogramas_ConLosTiemposDelLeeme_YVuelveAIdle()
        {
            var ataques = new (AnimatorParams.Parametro disparador, string[] fotogramas, float[] duraciones)[]
            {
                (AnimatorParams.Espadazo, new[] { "E1", "E2", "E3", "E4" }, new[] { 0.55f, 0.08f, 0.22f, 0.30f }),
                (AnimatorParams.GolpeSuelo, new[] { "G1", "G2", "G3" }, new[] { 0.50f, 0.07f, 0.40f }),
                (AnimatorParams.Salto, new[] { "J1", "J2", "J3", "J4", "J5" }, new[] { 0.50f, 0.10f, 0.30f, 0.15f, 0.45f }),
                (AnimatorParams.Rugido, new[] { "R1", "R2", "R3" }, new[] { 0.35f, 0.35f, 0.20f }),
            };

            yield return CrearCentinela();
            foreach (var (disparador, fotogramas, duraciones) in ataques)
            {
                _animator.SetTrigger(disparador.Hash);
                yield return null; // el Animator lo procesa al final del fotograma
                var registro = new Registro();
                yield return Observar(duraciones.Sum() + 0.3f, registro);

                CollectionAssert.AreEqual(fotogramas.Append("I1"), registro.Secuencia, $"{disparador.Nombre}: sus fotogramas y, al acabar, el idle.");
                for (var i = 0; i < fotogramas.Length; i++)
                {
                    Assert.AreEqual(duraciones[i], registro.Duracion(i), Margen, $"{disparador.Nombre}: {fotogramas[i]}.");
                }

                // El rugido no es un ataque: no tiene anticipación ni golpe.
                if (disparador.Nombre != AnimatorParams.Rugido.Nombre)
                {
                    Assert.Greater(registro.Duracion(0), 3f * duraciones.Min(), $"{disparador.Nombre}: se ve venir.");
                }
            }
        }

        // --- La espada clavada y su forcejeo ---

        [UnityTest]
        public IEnumerator ConClavar_LaEspadaSeClavaAlAcabarE2_YSoloForcejeaEnC2()
        {
            yield return CrearCentinela();
            _animator.SetBool(AnimatorParams.Clavar.Hash, true);
            _animator.SetTrigger(AnimatorParams.Espadazo.Hash);
            yield return null;
            var registro = new Registro();
            yield return Observar(0.63f + 1.47f + 0.3f, registro);

            CollectionAssert.AreEqual(new[] { "E1", "E2", "C1", "C2", "C4", "I1" }, registro.Secuencia, "En vez de E3 y E4, la espada clavada.");
            Assert.AreEqual(0.08f, registro.Duracion(1), Margen, "E2 entero antes de clavar.");
            Assert.AreEqual(1.20f, registro.Duracion(3), Margen, "C2 aguanta 1,2 s.");

            var enC2 = registro.Alturas.Where(a => a.fotograma == "C2").Select(a => a.altura).ToList();
            Assert.IsTrue(enC2.Any(y => Mathf.Abs(y) < 1e-4f) && enC2.Any(y => Mathf.Abs(y - 0.02f) < 1e-4f), "En C2 sube un píxel y baja.");
            Assert.IsTrue(enC2.All(y => Mathf.Abs(y) < 1e-4f || Mathf.Abs(y - 0.02f) < 1e-4f), "Solo dos alturas: 0 y +0,02.");
            CollectionAssert.IsEmpty(registro.Alturas.Where(a => a.fotograma != "C2" && Mathf.Abs(a.altura) > 1e-4f),
                "Fuera de C2 el sprite no se mueve.");
            Assert.AreEqual(0f, Altura, 1e-4f, "Al volver al idle, el forcejeo vuelve a 0.");
        }

        // --- Combos y transiciones ---

        [UnityTest]
        public IEnumerator UnEspadazoEncadenado_VuelveAEmpezarDesdeE1()
        {
            yield return CrearCentinela();
            _animator.SetTrigger(AnimatorParams.Espadazo.Hash);
            yield return EsperarHasta(() => Fotograma == "E3", 1.5f, "la recuperación del primer espadazo (E3)");

            _animator.SetTrigger(AnimatorParams.Espadazo.Hash);
            yield return null;
            yield return null;
            Assert.AreEqual("E1", Fotograma, "El combo no espera a que acabe el espadazo anterior.");
        }

        [UnityTest]
        public IEnumerator SiYaCamina_AlAcabarElAtaqueSigueAndando_SinVerElIdle()
        {
            yield return CrearCentinela();
            _animator.SetTrigger(AnimatorParams.GolpeSuelo.Hash);
            yield return EsperarHasta(() => Fotograma == "G3", 1.5f, "la onda del golpe al suelo (G3)");

            _animator.SetBool(AnimatorParams.Walk.Hash, true);
            var registro = new Registro();
            yield return Observar(0.4f + 0.6f, registro);
            CollectionAssert.AreEqual(new[] { "G3", "A1", "A2", "A4" }, registro.Secuencia.Take(4).ToArray(), "De G3 a andar.");
            Assert.IsFalse(registro.Secuencia.Any(f => f.StartsWith("I")), "Sin el idle frontal entre medias.");
        }

        // --- La muerte ---

        [UnityTest]
        public IEnumerator ElRugidoYLaMuerte_InterrumpenElAtaque_YLaMuerteSeQuedaEnD4()
        {
            yield return CrearCentinela();
            _animator.SetTrigger(AnimatorParams.Espadazo.Hash);
            yield return EsperarHasta(() => Fotograma == "E1", 0.5f, "el espadazo");
            yield return new WaitForSeconds(0.2f);

            _animator.SetTrigger(AnimatorParams.Rugido.Hash);
            yield return null;
            yield return null;
            Assert.AreEqual("R1", Fotograma, "El rugido corta el espadazo.");

            _animator.SetBool(AnimatorParams.Death.Hash, true);
            var registro = new Registro();
            yield return Observar(2.1f + 0.3f, registro);
            CollectionAssert.AreEqual(new[] { "R1", "D1", "D2", "D3", "D4" }, registro.Secuencia, "La muerte corta el rugido.");
            Assert.AreEqual(1.20f, registro.Duracion(3), Margen, "D3.");

            // Ni un ataque ni andar la sacan de D4: la ceniza con el fragmento se queda.
            _animator.SetTrigger(AnimatorParams.Rugido.Hash);
            _animator.SetTrigger(AnimatorParams.Espadazo.Hash);
            _animator.SetTrigger(AnimatorParams.Salto.Hash);
            _animator.SetBool(AnimatorParams.Walk.Hash, true);
            yield return Durante(1f, () => Assert.AreEqual("D4", Fotograma));
        }
    }
}
