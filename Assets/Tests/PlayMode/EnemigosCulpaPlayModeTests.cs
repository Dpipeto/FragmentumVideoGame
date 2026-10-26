using System.Collections;
using System.Linq;
using Fragmentum.Core.Combate;
using Fragmentum.Core.Juego;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Los enemigos del mundo de la Culpa jugándose en el Nivel 4 (fase C): el Artillero da en la cabeza
    /// de Kael de pie tras la cobertura y no lo alcanza agachado; baja el arma si Kael se vuelve
    /// invisible; da un culatazo a quien se le acerca; el Penitente pierde a Kael invisible; y el Verdugo
    /// se arrodilla con una explosión (los golpes valen el doble), su cadena le da a Kael de pie y le
    /// pasa por encima agachado, y, si Kael se vuelve invisible, va a donde lo vio y barre a ciegas.
    /// </summary>
    public class EnemigosCulpaPlayModeTests : PruebaFlujoBase
    {
        private LevelData Nivel4 => BaseDeDatos.niveles[3];

        private IEnumerator EntrarEnNivel4(params string[] despiertos)
        {
            Gestor.Datos.nivelMaximoAlcanzado = Mathf.Max(Gestor.Datos.nivelMaximoAlcanzado, 4);
            Assert.IsTrue(Juego.JugarNivel(Nivel4));
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 8f);
            Assert.IsNotNull(Jugador);

            // Solo los que pide la prueba: los demás, fuera.
            foreach (var enemigo in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
            {
                if (!despiertos.Contains(enemigo.name))
                {
                    enemigo.gameObject.SetActive(false);
                }
            }
        }

        private static T De<T>(string nombre) where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Exclude).Single(c => c.name == nombre);

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

        private InputSimulado Mando()
        {
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);
            return entrada;
        }

        [UnityTest]
        public IEnumerator ElArtillero_LeDaDePieTrasLaCobertura_YAgachadoNo()
        {
            yield return EntrarEnNivel4("Artillero_1");
            var artillero = De<Artillero>("Artillero_1");
            var escombro = De<BoxCollider2D>("Escombro3").bounds;

            // De pie, pegado al escombro: la bala pasa por encima del borde y le da en la cabeza.
            yield return LlevarJugadorA(new Vector2(escombro.min.x - 0.5f, 0.05f));
            var entrada = Mando();
            var vida = Jugador.Vida.Vida;
            yield return Hasta(() => Jugador.Vida.Vida < vida, artillero.tiempoApuntar + 2f, "que una bala le dé de pie");

            // Agachado tras el mismo escombro (el golpe lo apartó: vuelve a él), una ráfaga entera y su recarga: nada.
            yield return Hasta(() => artillero.Estado == ESTADOS_ARTILLERO.REPOSO, 4f, "que termine la ráfaga");
            yield return LlevarJugadorA(new Vector2(escombro.min.x - 0.5f, 0.05f));
            Jugador.Vida.Curar(99);
            entrada.AgacharseMantenido = true;
            yield return new WaitForSeconds(0.6f);
            vida = Jugador.Vida.Vida;
            yield return new WaitForSeconds(artillero.tiempoApuntar + artillero.recarga + 1f);
            Assert.AreEqual(vida, Jugador.Vida.Vida, "Agachado tras la cobertura, no le alcanza.");
            entrada.AgacharseMantenido = false;
        }

        [UnityTest]
        public IEnumerator ElArtillero_BajaElArma_SiKaelSeVuelveInvisible()
        {
            yield return EntrarEnNivel4("Artillero_1");
            Jugador.Habilidades.Desbloquear(HABILIDADES.INVISIBILIDAD_PARCIAL);
            var artillero = De<Artillero>("Artillero_1");
            yield return LlevarJugadorA(new Vector2(37.5f, 0.05f));
            Mando();
            yield return Hasta(() => artillero.Estado == ESTADOS_ARTILLERO.APUNTA, 2f, "que apunte");
            Assert.IsTrue(artillero.LineaVisible, "Apunta: la línea roja hasta su cabeza.");

            Assert.IsTrue(Jugador.Habilidades.ActivarInvisibilidad());
            yield return null;
            yield return null;
            Assert.AreEqual(ESTADOS_ARTILLERO.REPOSO, artillero.Estado, "Invisible, Kael no existe para él: baja el arma.");
            Assert.IsFalse(artillero.LineaVisible, "Y se apaga la línea.");
            var vida = Jugador.Vida.Vida;
            yield return new WaitForSeconds(Jugador.Habilidades.DuracionInvisibilidad - 0.3f);
            Assert.AreEqual(vida, Jugador.Vida.Vida, "Mientras dura, no dispara.");
            Assert.AreEqual(ESTADOS_ARTILLERO.REPOSO, artillero.Estado);
        }

        [UnityTest]
        public IEnumerator ElArtillero_LeDaUnCulatazo_AQuienSeLeAcerca()
        {
            yield return EntrarEnNivel4("Artillero_1");
            var artillero = De<Artillero>("Artillero_1");
            yield return LlevarJugadorA(new Vector2(artillero.transform.position.x + 1f, 0.05f));
            Mando();
            var vida = Jugador.Vida.Vida;
            yield return Hasta(() => artillero.Estado == ESTADOS_ARTILLERO.CULATAZO, 1f, "que alce la culata");
            yield return Hasta(() => Jugador.Vida.Vida < vida, artillero.avisoCulatazo + 0.5f, "el culatazo");
            Assert.AreEqual(vida - 1, Jugador.Vida.Vida, "Quita 1.");
        }

        [UnityTest]
        public IEnumerator ElPenitente_PierdeAKaelInvisible_YVuelveASuRonda()
        {
            yield return EntrarEnNivel4("Penitente_1");
            Jugador.Habilidades.Desbloquear(HABILIDADES.INVISIBILIDAD_PARCIAL);
            var penitente = De<EnemyController>("Penitente_1");
            yield return LlevarJugadorA(new Vector2(penitente.transform.position.x - 4f, 0.05f));
            Mando();
            yield return Hasta(() => penitente.EstadoActual == ESTADOS_ENEMIGO.CHASE || penitente.EstadoActual == ESTADOS_ENEMIGO.ATTACK, 3f,
                "que el Penitente lo vea y vaya a por él");

            Assert.IsTrue(Jugador.Habilidades.ActivarInvisibilidad());
            yield return Hasta(() => penitente.EstadoActual != ESTADOS_ENEMIGO.CHASE && penitente.EstadoActual != ESTADOS_ENEMIGO.ATTACK, 2.5f,
                "que lo pierda");
        }

        [UnityTest]
        public IEnumerator ElVerdugo_SeArrodillaConUnaExplosion_YLosGolpesValenElDoble()
        {
            yield return EntrarEnNivel4("Verdugo");
            var verdugo = De<Verdugo>("Verdugo");
            var vida = verdugo.GetComponent<Health>();
            var barril = Object.FindObjectsByType<BarrilExplosivo>(FindObjectsInactive.Exclude).Single(b => Mathf.Approximately(b.transform.position.x, 156f));
            verdugo.transform.position = new Vector3(155f, 0.05f, 0f);
            yield return null;

            var antes = vida.Vida;
            Assert.IsTrue(barril.Encender());
            yield return Hasta(() => verdugo.DeRodillas, barril.mecha + 0.5f, "que la explosión lo arrodille");
            Assert.AreEqual(antes - barril.danioEnemigos, vida.Vida, "La explosión le quita 3.");

            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(verdugo.DeRodillas, "Sigue de rodillas.");
            antes = vida.Vida;
            vida.RecibirGolpe(new Golpe(1, verdugo.transform.position + Vector3.left, null));
            yield return null;
            Assert.AreEqual(antes - 2, vida.Vida, "De rodillas, un golpe de 1 le quita 2.");

            yield return Hasta(() => !verdugo.DeRodillas, verdugo.tiempoRodillas + 0.5f, "que se levante");
        }

        [UnityTest]
        public IEnumerator LaCadenaDelVerdugo_LeDaDePie()
        {
            yield return EntrarEnNivel4("Verdugo");
            var verdugo = De<Verdugo>("Verdugo");
            yield return LlevarJugadorA(new Vector2(verdugo.transform.position.x - 7f, 0.05f));
            Mando();
            var vida = Jugador.Vida.Vida;
            var enganchado = false;
            verdugo.OnEnganchado += () => enganchado = true;
            yield return Hasta(() => verdugo.Estado == ESTADOS_VERDUGO.CADENA, 2f, "que voltee el garfio");
            yield return Hasta(() => enganchado, 3f, "que la cadena le dé");
            Assert.AreEqual(vida - 1, Jugador.Vida.Vida, "Quita 1.");
        }

        [UnityTest]
        public IEnumerator LaCadenaDelVerdugo_PasaPorEncimaDeKaelAgachado()
        {
            yield return EntrarEnNivel4("Verdugo");
            var verdugo = De<Verdugo>("Verdugo");
            yield return LlevarJugadorA(new Vector2(verdugo.transform.position.x - 7f, 0.05f));
            var entrada = Mando();
            entrada.AgacharseMantenido = true;
            yield return new WaitForSeconds(0.3f);
            var vida = Jugador.Vida.Vida;
            var lanzada = false;
            var enganchado = false;
            verdugo.OnEnganchado += () => enganchado = true;
            yield return Hasta(() => verdugo.Estado == ESTADOS_VERDUGO.CADENA, 2f, "que voltee el garfio");
            yield return Hasta(() => verdugo.GarfioEnVuelo, 2f, "que lo lance");
            while (verdugo.GarfioEnVuelo)
            {
                lanzada = true;
                yield return null;
            }

            Assert.IsTrue(lanzada);
            Assert.IsFalse(enganchado, "Agachado, el garfio le pasa por encima.");
            Assert.AreEqual(vida, Jugador.Vida.Vida);
            entrada.AgacharseMantenido = false;
        }

        [UnityTest]
        public IEnumerator ElVerdugo_PierdeAKaelInvisible_YBarreDondeLoVio()
        {
            yield return EntrarEnNivel4("Verdugo");
            Jugador.Habilidades.Desbloquear(HABILIDADES.INVISIBILIDAD_PARCIAL);
            var verdugo = De<Verdugo>("Verdugo");
            var donde = verdugo.transform.position.x - 4f;
            yield return LlevarJugadorA(new Vector2(donde, 0.05f));
            Mando();
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(donde, verdugo.UltimaVista.x, 0.5f, "Lo ve.");

            Assert.IsTrue(Jugador.Habilidades.ActivarInvisibilidad());
            yield return LlevarJugadorA(new Vector2(donde - 3f, 0.05f));
            yield return Hasta(() => verdugo.ACiegas, 2f, "que vaya a donde lo vio");
            yield return Hasta(() => verdugo.Estado == ESTADOS_VERDUGO.BARRIDO, 4f, "el barrido a ciegas");
            Assert.AreEqual(donde, verdugo.transform.position.x, 1.5f, "Barre donde lo vio, no donde está.");
            Assert.IsFalse(verdugo.GarfioEnVuelo, "Sin lanzar la cadena.");
        }
    }
}
