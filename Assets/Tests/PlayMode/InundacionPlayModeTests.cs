using System.Collections;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Levels.Mecanicas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// La franja que la Doncella inunda y congela, con Kael dentro: el aviso no moja, el agua es
    /// la del recorrido y no hace daño, las púas hieren a quien sigue dentro al congelarse y la
    /// capa de hielo se pisa sin dejar a nadie encajado.
    /// </summary>
    public class InundacionPlayModeTests : PruebaPlayModeBase
    {
        [UnityTearDown]
        public IEnumerator BorrarPuas()
        {
            foreach (var zona in Object.FindObjectsByType<ZonaDeDanio>(FindObjectsInactive.Include))
            {
                Object.Destroy(zona.gameObject);
            }

            yield return null;
        }

        [TearDown]
        public void SoltarElReloj() => Time.captureDeltaTime = 0f;

        [UnityTest]
        public IEnumerator ElAvisoNoMoja_ElAguaSi_YDespuesSeVa()
        {
            var franja = CrearFranja();
            yield return CrearJugadorEnSuelo(Vector2.zero);
            var vida = Jugador.Vida.Vida;

            Assert.IsTrue(franja.Inundar(0.4f, 1f));
            Assert.AreEqual(ESTADOS_FRANJA.WARNING, franja.Estado);
            Assert.IsFalse(franja.Inundar(0.4f, 1f), "Ya está en marcha: solo se inunda desde seco.");
            yield return ComprobarQueNunca(() => Jugador.Motor.BajoElAgua, 0.3f, "El aviso todavía no moja.");

            yield return EsperarHasta(() => Jugador.Motor.BajoElAgua, 1f, "que el agua suba hasta Kael");
            Assert.AreEqual(ESTADOS_FRANJA.FLOODED, franja.Estado);
            Assert.AreEqual(0.6f, Jugador.Motor.MultVelocidad, 1e-3f, "Es el agua del recorrido: velocidad × 0,6.");
            yield return EsperarHasta(() => Mathf.Approximately(franja.Nivel, franja.alturaAgua), 1f, "que el agua llegue arriba");

            yield return EsperarHasta(() => franja.Estado == ESTADOS_FRANJA.DRY, 3f, "que el agua se vaya");
            Assert.IsFalse(franja.TieneAgua);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(Jugador.Motor.BajoElAgua, "Sin agua, Kael vuelve a estar en seco.");
            Assert.AreEqual(vida, Jugador.Vida.Vida, "El agua no hace daño.");
        }

        // A muchas imágenes por segundo cada paso de la subida es diminuto: tiene que sumarse igual.
        [UnityTest]
        public IEnumerator AMilImagenesPorSegundo_ElAguaSubeIgual()
        {
            var franja = CrearFranja();
            yield return CrearJugadorEnSuelo(Vector2.zero);
            Time.captureDeltaTime = 0.001f;

            Assert.IsTrue(franja.Inundar(0f, 0.8f));
            yield return new WaitForSeconds(franja.tiempoSubida + 0.1f);
            Assert.AreEqual(franja.alturaAgua, franja.Nivel, 0.01f, "Con imágenes de una milésima de segundo, el agua llega arriba en su tiempo.");
            Assert.IsTrue(Jugador.Motor.BajoElAgua, "Y moja.");

            yield return EsperarHasta(() => franja.Estado == ESTADOS_FRANJA.DRY, 2f, "que el agua se vaya");
            Assert.IsFalse(franja.TieneAgua);
        }

        [UnityTest]
        public IEnumerator AlCongelarse_LasPuasHierenAQuienSigueDentro_YQuedaUnaCapaQueSePisaYResbala()
        {
            var franja = CrearFranja();
            yield return CrearJugadorEnSuelo(Vector2.zero);
            var vida = Jugador.Vida.Vida;
            Assert.IsFalse(franja.Congelar(0.3f, 0.4f, 2f, 1), "En seco no hay nada que congelar.");

            yield return Llenar(franja);
            Assert.IsTrue(franja.Congelar(0.3f, 0.4f, 2f, 1));
            Assert.AreEqual(ESTADOS_FRANJA.FREEZING, franja.Estado);
            yield return ComprobarQueNunca(() => !Jugador.Motor.BajoElAgua || Jugador.Vida.Vida < vida, 0.2f,
                "Mientras se pone blanca sigue siendo agua, y el aviso no hace daño.");

            yield return EsperarHasta(() => franja.Congelada, 1f, "que se congele");
            Assert.IsTrue(franja.PuasActivas, "Brotan las púas en lo que estaba inundado.");
            yield return EsperarHasta(() => Jugador.Vida.Vida < vida, 0.5f, "que las púas hieran a quien sigue dentro");
            Assert.AreEqual(vida - 1, Jugador.Vida.Vida);
            Assert.IsFalse(Jugador.Motor.BajoElAgua, "El agua ya es hielo.");
            yield return EsperarHasta(() => !franja.PuasActivas, 1f, "que las púas se retiren");

            // Encima de la capa se pisa, y es hielo.
            yield return Mover(new Vector2(0f, franja.Techo + 0.05f));
            yield return EsperarHasta(() => Jugador.Sensores.EnSuelo, 1f, "que Kael se apoye en la capa");
            Assert.AreEqual(franja.Techo, PiesJugador.y, 0.1f, "La capa de hielo se pisa.");
            Assert.Less(Jugador.Motor.MultAceleracion, 0.5f, "Y resbala.");
            Assert.AreEqual(vida - 1, Jugador.Vida.Vida, "Encima ya no hay púas.");

            yield return EsperarHasta(() => franja.Estado == ESTADOS_FRANJA.DRY, 3f, "que la capa se deshaga");
            yield return EsperarHasta(() => PiesJugador.y < 0.5f, 2f, "que Kael caiga al suelo");
        }

        [UnityTest]
        public IEnumerator QuienEstaMetidoEnLaCapaAlFormarse_NoSeQuedaEncajado()
        {
            var franja = CrearFranja();
            yield return CrearJugadorEnSuelo(Vector2.zero);
            yield return Llenar(franja);

            // Kael, a media agua, con el cuerpo cruzando la altura de la capa.
            var cuerpo = Jugador.Motor.Cuerpo;
            cuerpo.position = new Vector2(0f, franja.Techo - 1f);
            cuerpo.linearVelocity = Vector2.zero;
            Assert.IsTrue(franja.Congelar(0f, 0.1f, 3f, 0));
            yield return EsperarHasta(() => franja.Congelada, 0.5f, "que se congele");

            yield return EsperarHasta(() => Jugador.Sensores.EnSuelo && PiesJugador.y < 0.3f, 3f, "que caiga al suelo atravesando la capa");

            // Una vez fuera, la capa es sólida también para él.
            yield return Mover(new Vector2(0f, franja.Techo + 0.05f));
            yield return EsperarHasta(() => Jugador.Sensores.EnSuelo, 1f, "que Kael se apoye en la capa");
            Assert.AreEqual(franja.Techo, PiesJugador.y, 0.1f);
        }

        [UnityTest]
        public IEnumerator Secar_LoQuitaTodoEnElActo()
        {
            var franja = CrearFranja();
            yield return CrearJugadorEnSuelo(new Vector2(-10f, 0f));
            yield return Llenar(franja);
            franja.Congelar(0f, 5f, 5f, 1);
            yield return EsperarHasta(() => franja.Congelada, 0.5f, "que se congele");
            yield return new WaitForFixedUpdate();
            var enLaCapa = new Vector2(0f, franja.Techo - franja.grosorCapa * 0.5f);
            Assert.IsNotNull(Physics2D.OverlapPoint(enLaCapa, Capas.MascaraSuelo), "La capa es suelo.");
            Assert.IsTrue(franja.PuasActivas);

            franja.Secar();

            Assert.AreEqual(ESTADOS_FRANJA.DRY, franja.Estado);
            Assert.IsNull(Physics2D.OverlapPoint(enLaCapa, Capas.MascaraSuelo), "Sin capa.");
            yield return null;
            Assert.IsFalse(franja.PuasActivas, "Sin púas.");
            Assert.IsEmpty(Object.FindObjectsByType<ZonaDeDanio>(FindObjectsInactive.Include));

            // Y vuelve a empezar desde seco.
            Assert.IsTrue(franja.Inundar(0f, 1f));
        }

        // --- Utilidades ---

        private FranjaInundable CrearFranja()
        {
            Suelo(-20f, 20f);
            var franja = FranjaInundable.Crear("Franja", -4f, 4f, 0f);
            Creados.Add(franja.gameObject);
            return franja;
        }

        private static IEnumerator Llenar(FranjaInundable franja)
        {
            Assert.IsTrue(franja.Inundar(0f, 20f));
            yield return EsperarHasta(() => Mathf.Approximately(franja.Nivel, franja.alturaAgua), 2f, "que la franja se llene");
        }

        // Mueve a Kael sin revivirlo, y deja correr la física.
        private IEnumerator Mover(Vector2 pies)
        {
            var cuerpo = Jugador.Motor.Cuerpo;
            cuerpo.position = pies;
            cuerpo.linearVelocity = Vector2.zero;
            for (var i = 0; i < 6; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
