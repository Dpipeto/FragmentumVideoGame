using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Core.Combate;
using Fragmentum.Core.Juego;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// El Juez Silente jugándose en su plaza del Nivel 4 (fase D): cada ataque enciende la marca de su
    /// memoria y mirarla (E) lo arrodilla; invisible, no se mira; sin memorias en el camino tiene más
    /// vida (y su barra lo enseña), con una, la de siempre; en la segunda fase brilla también una trampa,
    /// que sujeta a Kael y deja el ataque sin cortar; y al caer, todas las memorias se iluminan.
    /// </summary>
    public class JuezPlayModeTests : PruebaFlujoBase
    {
        private LevelData Nivel4 => BaseDeDatos.niveles[3];

        private IEnumerator EntrarEnLaPlaza(bool recordarUnaMemoria = false)
        {
            Gestor.Datos.nivelMaximoAlcanzado = Mathf.Max(Gestor.Datos.nivelMaximoAlcanzado, 4);
            Assert.IsTrue(Juego.JugarNivel(Nivel4));
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 8f);
            foreach (var enemigo in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
            {
                enemigo.gameObject.SetActive(false);
            }

            if (recordarUnaMemoria)
            {
                var carta = Object.FindObjectsByType<MemoriaInteractuable>(FindObjectsInactive.Exclude).Single(m => m.Id == "LaCarta");
                Assert.IsTrue(carta.Recordar());
                yield return SaltarCinematicaSiHay();
            }

            var arena = Object.FindAnyObjectByType<ArenaJefe>();
            yield return LlevarJugadorA(EnElSuelo(arena.transform.position));
            yield return SaltarCinematicaSiHay();
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);
            Assert.IsTrue(Juez.EnCombate, "Al cruzar la verja, empieza el combate.");
        }

        private static JuezSilente Juez => Object.FindAnyObjectByType<JuezSilente>();

        private static IEnumerator Hasta(System.Func<bool> condicion, float maximo, string que)
        {
            var limite = Time.time + maximo;
            while (!condicion())
            {
                if (Time.time > limite)
                {
                    Assert.Fail($"Tiempo agotado esperando {que}.");
                }

                yield return null;
            }
        }

        // Kael sobre una marca, sin que le hagan daño mientras mira.
        private IEnumerator SobreLaMarca(MarcaDeMemoria marca)
        {
            yield return LlevarJugadorA(new Vector2(marca.transform.position.x, marca.transform.position.y + 0.05f));
            Jugador.Vida.Curar(99);
        }

        [UnityTest]
        public IEnumerator CadaAtaqueEnciendeSuMemoria_YMirarlaArrodillaAlJuez()
        {
            yield return EntrarEnLaPlaza();
            var juez = Juez;
            Assert.AreEqual(4, juez.Marcas.Count);
            yield return Hasta(() => juez.MemoriaProyectada >= 0, 3f, "el primer ataque");
            var memoria = juez.MemoriaProyectada;
            var marca = juez.Marcas[memoria];
            Assert.IsTrue(marca.Brilla, "Su marca brilla.");
            Assert.IsTrue(juez.Pantalla.enabled, "Y la proyecta en la pared del fondo.");
            if (juez.TieneImagenes)
            {
                Assert.AreSame(juez.Imagenes[memoria], juez.Pantalla.sprite, "Con arte, la imagen de su memoria.");
            }

            Assert.IsTrue(juez.Marcas.Where((m, i) => i != memoria).All(m => !m.Brilla), "En la primera fase, solo la suya.");

            juez.Vida.RecibirGolpe(new Golpe(3, juez.Centro));
            Assert.AreEqual(juez.Vida.VidaMaxima, juez.Vida.Vida, "Ningún golpe le hace nada.");

            yield return SobreLaMarca(marca);
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);
            entrada.PulsarInteractuar();
            yield return Hasta(() => juez.EsVulnerable, 1f, "que se arrodille");
            yield return null;
            Assert.IsFalse(marca.Brilla, "El ataque se cortó: la marca se apaga.");
            Assert.IsEmpty(Object.FindObjectsByType<ZonaDeDanio>(FindObjectsInactive.Exclude), "Y sus golpes desaparecen.");

            var vida = juez.Vida.Vida;
            juez.Vida.RecibirGolpe(new Golpe(1, juez.Centro));
            Assert.AreEqual(vida - 1, juez.Vida.Vida, "Arrodillado, se le golpea.");
        }

        [UnityTest]
        public IEnumerator Invisible_NoSeMiraLaMarca()
        {
            yield return EntrarEnLaPlaza();
            Jugador.Habilidades.Desbloquear(HABILIDADES.INVISIBILIDAD_PARCIAL);
            var juez = Juez;
            yield return Hasta(() => juez.MemoriaProyectada >= 0, 3f, "el primer ataque");
            var marca = juez.Marcas[juez.MemoriaProyectada];
            yield return SobreLaMarca(marca);
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);
            Assert.IsTrue(Jugador.Habilidades.ActivarInvisibilidad());
            yield return null;
            yield return null;
            Assert.AreEqual(MemoriaInteractuable.AvisoInvisible, Interfaz.Hud.Interaccion.Texto, "Para recordar, déjate ver.");
            entrada.PulsarInteractuar();
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(juez.EsVulnerable, "Invisible, mirar no lo arrodilla.");
        }

        [UnityTest]
        public IEnumerator SinMemoriasEnElCamino_ElJuezTieneMasVida_YSuBarraLoEnsena()
        {
            yield return EntrarEnLaPlaza();
            var juez = Juez;
            Assert.IsTrue(juez.ConVidaExtra, "No se miró ninguna memoria: el Juez llega más fuerte.");
            Assert.AreEqual(36, juez.Vida.VidaMaxima, "28 → 36.");
            Assert.AreEqual(36, juez.Vida.Vida);
            Assert.IsTrue(Interfaz.Hud.TramoExtraVisible, "Su barra enseña ese tramo en otro color.");
        }

        [UnityTest]
        public IEnumerator ConUnaMemoriaEnElCamino_LaVidaDeSiempre()
        {
            yield return EntrarEnLaPlaza(recordarUnaMemoria: true);
            var juez = Juez;
            Assert.IsFalse(juez.ConVidaExtra);
            Assert.AreEqual(juez.Datos.vidaMaxima, juez.Vida.VidaMaxima, "Se miró una memoria: 28.");
            Assert.IsFalse(Interfaz.Hud.TramoExtraVisible);
        }

        [UnityTest]
        public IEnumerator EnLaSegundaFase_BrillaUnaTrampa_QueSujetaAKael_YDejaElAtaqueSinCortar()
        {
            yield return EntrarEnLaPlaza();
            var juez = Juez;
            juez.Vida.RecibirGolpe(new Golpe(juez.Vida.VidaMaxima / 2 + 1, juez.Centro, null, false, true));
            yield return Hasta(() => juez.Fase == 2, 2f, "la segunda fase");
            yield return Hasta(() => juez.Trampa >= 0, 6f, "un ataque con su trampa");
            Assert.AreNotEqual(juez.MemoriaProyectada, juez.Trampa, "La trampa es otra memoria.");
            Assert.IsTrue(juez.Marcas[juez.MemoriaProyectada].Brilla && juez.Marcas[juez.Trampa].Brilla, "Brillan dos marcas.");

            var verdadera = juez.Marcas[juez.MemoriaProyectada];
            yield return SobreLaMarca(juez.Marcas[juez.Trampa]);
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);
            var vida = Jugador.Vida.Vida;
            entrada.PulsarInteractuar();
            yield return null;
            yield return null;
            Assert.AreEqual(vida - juez.danioTrampa, Jugador.Vida.Vida, "Las cadenas le quitan 1…");
            Assert.IsFalse(juez.EsVulnerable, "…el Juez no se arrodilla…");
            Assert.IsFalse(juez.Cortable, "…y ese ataque ya no se corta.");
            Assert.IsFalse(verdadera.Brilla, "La marca buena se apaga.");

            // Sujeto: no se mueve hasta que las cadenas lo sueltan.
            Assert.IsTrue(juez.Sujetando);
            Assert.AreEqual(0f, Jugador.Motor.MultVelocidad, 1e-4f, "Las cadenas lo sujetan.");
            yield return new WaitForSeconds(juez.sujecionTrampa + 0.2f);
            Assert.IsFalse(juez.Sujetando);
            Assert.AreEqual(1f, Jugador.Motor.MultVelocidad, 1e-4f, "Y lo sueltan.");
        }

        [UnityTest]
        public IEnumerator AlCaer_TodasLasMemoriasSeIluminan()
        {
            yield return EntrarEnLaPlaza();
            var juez = Juez;
            juez.Vida.Matar();
            yield return null;
            Assert.IsTrue(juez.Derrotado);
            Assert.IsTrue(juez.Marcas.All(m => m.Brilla && m.Fija), "Las cuatro marcas, encendidas para siempre.");
            Assert.IsTrue(juez.Pantalla.enabled, "Y la pared, con todas.");
            if (juez.TieneImagenes)
            {
                // Con arte, pasan una tras otra (antes de que empiece la cinemática de su caída, que para el tiempo).
                var vistas = new HashSet<Sprite>();
                var hasta = Time.unscaledTime + juez.segundosPorImagen * 2.5f;
                while (Time.unscaledTime < hasta)
                {
                    vistas.Add(juez.Pantalla.sprite);
                    yield return null;
                }

                Assert.GreaterOrEqual(vistas.Count, 2, "Pasan una tras otra.");
                Assert.IsTrue(vistas.All(v => juez.Imagenes.Contains(v)), "Y son las de las memorias.");
            }
        }
    }
}
