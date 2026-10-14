using System;
using System.Collections;
using Fragmentum.Core.Juego;
using Fragmentum.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// La puerta de salida y el checkpoint con su arte, jugando el Nivel 1: el checkpoint se
    /// enciende al tocarlo y la puerta se ve cerrada desde el principio y se abre al recoger el
    /// fragmento de la Centinela.
    /// </summary>
    public class PuertasPlayModeTests : PruebaFlujoBase
    {
        private static LevelManager Nivel => Object.FindAnyObjectByType<LevelManager>();

        [UnityTest]
        public IEnumerator TocarUnCheckpoint_LoEnciende_YNoSeMueveDeSuSitio()
        {
            yield return EntrarEnNivel1();
            var checkpoint = Nivel.Checkpoints.Buscar("L1_CP1");
            Assert.IsTrue(checkpoint.TieneDibujos, "El checkpoint lleva su brasero.");
            var apagado = checkpoint.DibujoActual;
            var dibujo = checkpoint.GetComponentInChildren<SpriteRenderer>();
            var pie = dibujo.bounds.min.y;

            yield return LlevarJugadorA(EnElSuelo(checkpoint.transform.position));

            Assert.IsTrue(checkpoint.Activo);
            Assert.IsNotNull(checkpoint.DibujoActual);
            Assert.AreNotSame(apagado, checkpoint.DibujoActual, "Encendido, con otro dibujo.");
            Assert.AreEqual(Color.white, dibujo.color, "Con arte no se tiñe.");
            Assert.AreEqual(pie, dibujo.bounds.min.y, 0.02f, "Al encenderse no salta: los dos dibujos apoyan en el mismo pie.");
        }

        [UnityTest]
        public IEnumerator LaPuertaDeSalida_SeVeCerrada_YSeAbreAlRecogerElFragmento()
        {
            yield return EntrarEnNivel1();
            var arena = Object.FindAnyObjectByType<ArenaJefe>();
            var salida = Object.FindAnyObjectByType<SalidaNivel>();
            var puerta = salida.Puerta;
            Assert.IsNotNull(puerta);
            var dibujo = puerta.GetComponent<SpriteRenderer>();
            Assert.IsTrue(dibujo.enabled, "La puerta se ve desde que se entra en la arena…");
            Assert.IsFalse(puerta.Abierta, "…cerrada.");
            Assert.AreSame(puerta.DibujoCerrada, puerta.DibujoActual);
            Assert.IsFalse(salida.Abierta, "Y no se puede usar.");

            yield return LlevarJugadorA(EnElSuelo(arena.transform.position));
            yield return SaltarCinematicaSiHay();
            arena.Jefe.Vida.Matar();
            yield return Hasta(() => arena.Jefe.Fragmento.Disponible, 4f, "que el fragmento aparezca en la ceniza");
            yield return SaltarCinematicaSiHay();
            Assert.IsFalse(puerta.Abierta, "Sin el fragmento sigue cerrada.");

            yield return LlevarJugadorA(new Vector2(arena.Jefe.Fragmento.Centro.x, arena.transform.position.y + 0.05f));
            yield return Hasta(() => salida.Abierta, 4f, "que la arena se abra tras recoger el fragmento");
            Assert.IsTrue(puerta.Abierta, "Al recoger el fragmento la puerta se abre.");
            Assert.AreSame(puerta.DibujoEntreabierta, puerta.DibujoActual, "Primero, entreabierta.");

            yield return new WaitForSeconds(0.7f);
            Assert.AreNotSame(puerta.DibujoCerrada, puerta.DibujoActual);
            Assert.AreNotSame(puerta.DibujoEntreabierta, puerta.DibujoActual, "Después, abierta del todo.");
        }

        [UnityTest]
        public IEnumerator UnaPuertaAbierta_AlternaSusDosDibujos()
        {
            var objeto = new GameObject("Puerta", typeof(SpriteRenderer));
            try
            {
                var puerta = objeto.AddComponent<PuertaDeSalida>();
                var dibujos = new Sprite[4];
                for (var i = 0; i < dibujos.Length; i++)
                {
                    dibujos[i] = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), Vector2.zero);
                    dibujos[i].name = $"Puerta{i + 1}";
                }

                puerta.Fijar(dibujos[0], dibujos[1], dibujos[2], dibujos[3]);
                Assert.AreSame(dibujos[0], puerta.DibujoActual);

                puerta.Abrir();
                yield return null;
                Assert.AreSame(dibujos[1], puerta.DibujoActual);

                var vistos = new System.Collections.Generic.HashSet<Sprite>();
                var fin = Time.time + 2f;
                while (Time.time < fin)
                {
                    vistos.Add(puerta.DibujoActual);
                    yield return null;
                }

                CollectionAssert.IsSupersetOf(vistos, new[] { dibujos[2], dibujos[3] }, "Abierta, alterna sus dos dibujos.");

                puerta.Cerrar();
                Assert.AreSame(dibujos[0], puerta.DibujoActual);
            }
            finally
            {
                Object.Destroy(objeto);
            }
        }

        private static IEnumerator Hasta(Func<bool> condicion, float maximo, string que)
        {
            var limite = Time.unscaledTime + maximo;
            while (!condicion())
            {
                if (Time.unscaledTime > limite)
                {
                    Assert.Fail($"Tiempo agotado esperando {que}.");
                }

                yield return null;
            }
        }
    }
}
