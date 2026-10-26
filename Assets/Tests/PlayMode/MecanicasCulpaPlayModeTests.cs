using System.Collections;
using System.Linq;
using Fragmentum.Core.Juego;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Las mecánicas del mundo de la Culpa jugándose en el Nivel 4 (fase B): el Ataque Cargado enciende
    /// el barril, que estalla y se lleva a los Penitentes que tiene al lado; la cadena del último tejado;
    /// la mina, que estalla al tocarla; la memoria, que no se recuerda invisible y que, recordada, pone
    /// su cinemática y vuelve más agresivos a los enemigos de su zona; y el indicador de la
    /// Invisibilidad en el HUD.
    /// </summary>
    public class MecanicasCulpaPlayModeTests : PruebaFlujoBase
    {
        private LevelData Nivel4 => BaseDeDatos.niveles[3];

        // Del diseño (DisenoNivel4, que es del Editor): el barril del tutorial y el primero del patio.
        private const float BarrilDelTutorial = 27f;
        private const float PrimerBarrilDelPatio = 148f;

        private IEnumerator EntrarEnNivel4()
        {
            Gestor.Datos.nivelMaximoAlcanzado = Mathf.Max(Gestor.Datos.nivelMaximoAlcanzado, 4);
            Assert.IsTrue(Juego.JugarNivel(Nivel4));
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 8f);
            Assert.IsNotNull(Jugador);
        }

        private static EnemyController Enemigo(string nombre) =>
            Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude).Single(e => e.name == nombre);

        // Quietos: así sus golpes no interrumpen la carga de Kael. Su vida sigue recibiendo golpes.
        private static EnemyController[] Quietos(params string[] nombres)
        {
            var enemigos = nombres.Select(Enemigo).ToArray();
            foreach (var enemigo in enemigos)
            {
                enemigo.enabled = false;
            }

            return enemigos;
        }

        private static BarrilExplosivo BarrilEn(float x) =>
            Object.FindObjectsByType<BarrilExplosivo>(FindObjectsInactive.Exclude).Single(b => Mathf.Approximately(b.transform.position.x, x));

        [UnityTest]
        public IEnumerator ElAtaqueCargado_EnciendeElBarril_QueSeLlevaALosDosPenitentes()
        {
            yield return EntrarEnNivel4();
            Jugador.Habilidades.Desbloquear(HABILIDADES.ATAQUE_CARGADO);
            var barril = BarrilEn(BarrilDelTutorial);
            var penitentes = Quietos("Penitente_2", "Penitente_3");

            // A la izquierda del barril, mirándolo: un golpe normal solo suena.
            yield return LlevarJugadorA(new Vector2(barril.transform.position.x - 1f, 0.05f));
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);
            var suena = 0;
            barril.OnSuena += () => suena++;
            entrada.PulsarAtaque();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(ESTADOS_BARRIL.INTACT, barril.Estado, "Un golpe normal no lo enciende.");
            Assert.Greater(suena, 0, "Suena, y nada más.");

            // El Ataque Cargado le enciende la mecha; al acabarse, estalla.
            entrada.PulsarAtaque(mantener: true);
            yield return new WaitForSeconds(0.8f);
            entrada.SoltarAtaque();
            var limite = Time.time + 0.8f;
            while (barril.Estado == ESTADOS_BARRIL.INTACT && Time.time < limite)
            {
                yield return null;
            }

            Assert.AreEqual(ESTADOS_BARRIL.FUSE, barril.Estado, "El Ataque Cargado le enciende la mecha.");
            yield return new WaitForSeconds(barril.mecha + 0.2f);
            Assert.AreEqual(ESTADOS_BARRIL.BURNT, barril.Estado, "Al acabarse la mecha, estalla.");
            Assert.IsTrue(penitentes.All(p => p == null || !p.Vida.EstaVivo), "Y se lleva a los dos Penitentes (3 de vida, 3 de daño).");
        }

        [UnityTest]
        public IEnumerator QuienSeQuedaCerca_SeLlevaSuGolpe_YAlReaparecerElBarrilVuelve()
        {
            yield return EntrarEnNivel4();
            Quietos("Penitente_2", "Penitente_3");
            var barril = BarrilEn(BarrilDelTutorial);
            yield return LlevarJugadorA(new Vector2(barril.transform.position.x - 1.2f, 0.05f));
            var vida = Jugador.Vida.Vida;
            Assert.IsTrue(barril.Encender());
            yield return new WaitForSeconds(barril.mecha + 0.2f);
            Assert.AreEqual(vida - barril.danioJugador, Jugador.Vida.Vida, "A 1,2 u, la explosión le quita 1.");

            Jugador.Vida.Matar();
            yield return EsperarEstado(ESTADOS_JUEGO.GAME_OVER, 3f);
            Assert.IsTrue(Juego.Reintentar());
            yield return null;
            Assert.IsTrue(Jugador.Vida.EstaVivo, "Reaparece.");
            Assert.AreEqual(ESTADOS_BARRIL.INTACT, barril.Estado, "Y el barril vuelve a estar intacto: uno por vida.");
        }

        [UnityTest]
        public IEnumerator EnElUltimoTejado_UnBarrilEnciendeALosOtros_EnCadena()
        {
            yield return EntrarEnNivel4();
            var tejado = Object.FindObjectsByType<BarrilExplosivo>(FindObjectsInactive.Exclude).Where(b => b.transform.position.y > 4f)
                .OrderBy(b => b.transform.position.x).ToList();
            Assert.AreEqual(4, tejado.Count);

            // Kael, lejos: en la convergencia.
            yield return LlevarJugadorA(new Vector2(112f, 0.05f));
            Assert.IsTrue(tejado[0].Encender());
            yield return new WaitForSeconds((tejado[0].mecha + 0.1f) * tejado.Count + 0.5f);
            Assert.IsTrue(tejado.All(b => b.Estado == ESTADOS_BARRIL.BURNT), "Los cuatro, uno tras otro.");
            foreach (var nombre in new[] { "Penitente_9", "Penitente_10", "Penitente_11" })
            {
                var penitente = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include).SingleOrDefault(e => e.name == nombre);
                Assert.IsTrue(penitente == null || !penitente.Vida.EstaVivo, $"{nombre}: lo alcanzó la cadena.");
            }

            var patio = BarrilEn(PrimerBarrilDelPatio);
            Assert.AreEqual(ESTADOS_BARRIL.INTACT, patio.Estado, "La cadena no salta a los barriles lejanos.");
        }

        [UnityTest]
        public IEnumerator LaMina_EstallaAlTocarla_YLeQuitaUnCorazon()
        {
            yield return EntrarEnNivel4();
            var mina = Object.FindObjectsByType<Mina>(FindObjectsInactive.Exclude).Single();
            var vida = Jugador.Vida.Vida;
            yield return LlevarJugadorA(new Vector2(mina.Centro.x - 0.2f, mina.transform.position.y + 0.05f));
            yield return new WaitForSeconds(mina.periodo + 0.2f);
            Assert.IsTrue(mina.Estallada, "Al tocarla, estalla.");
            Assert.AreEqual(vida - mina.danioJugador, Jugador.Vida.Vida, "Y le quita un corazón.");
        }

        [UnityTest]
        public IEnumerator LaMemoria_InvisibleNoSeRecuerda_YRecordadaAgravaASuZona()
        {
            yield return EntrarEnNivel4();
            Jugador.Habilidades.Desbloquear(HABILIDADES.INVISIBILIDAD_PARCIAL);
            var memoria = Object.FindObjectsByType<MemoriaInteractuable>(FindObjectsInactive.Exclude).Single(m => m.Id == "ElPueblo");
            var penitente = Enemigo("Penitente_4");
            var antes = penitente.Datos.velocidadPersecucion;
            yield return LlevarJugadorA(new Vector2(memoria.transform.position.x, memoria.transform.position.y + 0.05f));
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);

            // Invisible: el aviso dice que hay que dejarse ver, y E no hace nada.
            Assert.IsTrue(Jugador.Habilidades.ActivarInvisibilidad());
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(MemoriaInteractuable.AvisoInvisible, Interfaz.Hud.Interaccion.Texto, "Para recordar, déjate ver.");
            entrada.PulsarInteractuar();
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(memoria.Recordada, "Invisible no se recuerda.");

            // Visible: «E · Recordar», su cinemática y, al volver, los Penitentes de la calle más agresivos.
            yield return new WaitForSeconds(Jugador.Habilidades.DuracionInvisibilidad);
            Assert.IsFalse(Jugador.Habilidades.EsInvisible);
            StringAssert.EndsWith("Recordar", Interfaz.Hud.Interaccion.Texto);
            entrada.PulsarInteractuar();
            yield return null;
            yield return null;
            Assert.IsTrue(memoria.Recordada);
            Assert.AreEqual(ESTADOS_JUEGO.CUTSCENE, Juego.EstadoActual, "Recordarla pone su cinemática.");
            Assert.AreSame(memoria.Cinematica, Juego.CinematicaActual);
            yield return SaltarCinematicaSiHay();
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);

            Assert.AreEqual(antes * SistemaConsecuencias.Instancia.agresividad, penitente.Datos.velocidadPersecucion, 1e-3f, "Mirar cuesta: un 20 % más deprisa.");
            Assert.AreEqual(penitente.Vida.VidaMaxima, penitente.Vida.Vida, "Sin perder vida.");
            Assert.AreEqual(2.2f, Enemigo("Penitente_6").Datos.velocidadPersecucion, 1e-3f, "Los de la otra memoria, igual.");
            CollectionAssert.Contains(Gestor.Datos.memoriasActivadas, "ElPueblo", "La partida la apunta.");
            Assert.IsFalse(SistemaConsecuencias.Instancia.NingunaRecordada, "El Juez ya no tendrá más vida.");
        }

        [UnityTest]
        public IEnumerator ElIndicadorDeLaInvisibilidad_SeVeConLaHabilidad_SeVaciaYSeRellena()
        {
            yield return EntrarEnNivel4();
            var indicador = Interfaz.Hud.Invisibilidad;
            Assert.IsNotNull(indicador);
            yield return null;
            Assert.IsFalse(indicador.Visible, "Sin la Invisibilidad, no se ve.");

            Jugador.Habilidades.Desbloquear(HABILIDADES.INVISIBILIDAD_PARCIAL);
            yield return null;
            Assert.IsTrue(indicador.Visible);
            Assert.IsTrue(indicador.Lista);
            Assert.AreEqual(1f, indicador.Carga, 1e-3f);

            Assert.IsTrue(Jugador.Habilidades.ActivarInvisibilidad());
            yield return null;
            Assert.IsTrue(indicador.EnUso, "Mientras dura, late entero.");
            yield return new WaitForSeconds(Jugador.Habilidades.DuracionInvisibilidad + 0.2f);
            Assert.IsFalse(indicador.EnUso);
            Assert.IsFalse(indicador.Lista);
            Assert.Less(indicador.Carga, 0.2f, "Recién acabada, casi vacío.");
            yield return new WaitForSeconds(2f);
            Assert.Greater(indicador.Carga, 0.2f, "Y se va rellenando.");
        }
    }
}
