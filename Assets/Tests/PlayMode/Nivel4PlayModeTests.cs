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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// El Nivel 4 montado, jugándose: Kael se agacha (solo en este mundo), el refugio guarda la
    /// partida, bajo el arco del campanario se pasa y su torre no agarra, y en el río la alambrada
    /// sujeta a Kael y solo el Dash Acuático la cruza (fase A). Y su música: la marcha calla mientras
    /// se recuerda y en la plaza suena el órgano del Juez. Las medidas las comprueban
    /// <c>DisenoNivel4Tests</c> y <c>EscenaNivel4Tests</c>; aquí, que funcionan en el juego.
    /// </summary>
    public class Nivel4PlayModeTests : PruebaFlujoBase
    {
        private LevelData Nivel4 => BaseDeDatos.niveles[3];

        private IEnumerator EntrarEnNivel4()
        {
            Gestor.Datos.nivelMaximoAlcanzado = Mathf.Max(Gestor.Datos.nivelMaximoAlcanzado, 4);
            Assert.IsTrue(Juego.JugarNivel(Nivel4), "El Nivel 4 tiene escena y está desbloqueado.");
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 8f);
            Assert.AreEqual(Nivel4.escena.Nombre, SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Jugador, "El LevelManager del Nivel 4 hace aparecer al jugador.");
        }

        private static void ApagarEnemigos()
        {
            foreach (var enemigo in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
            {
                enemigo.gameObject.SetActive(false);
            }
        }

        [UnityTest]
        public IEnumerator AlEmpezar_KaelEstaEnElCampo_YSeAgachaTrasLaCobertura()
        {
            yield return EntrarEnNivel4();
            ApagarEnemigos();
            Assert.AreEqual(2f, Jugador.transform.position.x, 0.1f, "Kael aparece en el inicio.");
            Assert.IsTrue(Jugador.PermitirAgacharse, "En el mundo de la Culpa, Kael se agacha.");

            // Pegado a la izquierda del tercer escombro, agachado: queda por debajo de su borde.
            var escombro = Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Exclude).Single(c => c.name == "Escombro3").bounds;
            yield return LlevarJugadorA(new Vector2(escombro.min.x - 0.5f, 0.05f));
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);
            entrada.AgacharseMantenido = true;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(ESTADOS_JUGADOR.CROUCH, Jugador.EstadoActual);
            var cuerpo = Jugador.GetComponent<CapsuleCollider2D>().bounds;
            Assert.Less(cuerpo.max.y, escombro.max.y, "Agachado, Kael no asoma por encima del escombro.");

            entrada.AgacharseMantenido = false;
            yield return new WaitForSeconds(0.3f);
            cuerpo = Jugador.GetComponent<CapsuleCollider2D>().bounds;
            Assert.Greater(cuerpo.max.y, escombro.max.y, "De pie, asoma la cabeza.");
        }

        [UnityTest]
        public IEnumerator LaMarcha_SuenaEnElCampo_CallaAlRecordar_YEnLaPlazaSuenaElJuez()
        {
            yield return EntrarEnNivel4();
            ApagarEnemigos();
            var nivel = Object.FindAnyObjectByType<LevelManager>();
            Assert.IsNotNull(nivel.Musica, "El Nivel 4 tiene música.");
            var fuente = nivel.Musica.Fuente;
            Assert.AreSame(Nivel4.musica, fuente.clip, "En el campo, la marcha.");
            Assert.AreEqual(0.6f, fuente.volume, 1e-3f);

            // Anexo A: silencio en las memorias.
            var carta = Object.FindObjectsByType<MemoriaInteractuable>(FindObjectsInactive.Exclude).Single(m => m.Id == "LaCarta");
            Assert.IsTrue(carta.Recordar());
            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(ESTADOS_JUEGO.CUTSCENE, Juego.EstadoActual);
            Assert.AreEqual(0f, fuente.volume, 1e-3f, "Mientras se recuerda, la marcha calla.");
            Assert.IsTrue(nivel.Musica.EnPausa);
            yield return SaltarCinematicaSiHay();
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsFalse(nivel.Musica.EnPausa, "Al volver, sigue.");
            Assert.AreSame(Nivel4.musica, fuente.clip);
            Assert.AreEqual(0.6f, fuente.volume, 1e-3f);

            var arena = Object.FindAnyObjectByType<ArenaJefe>();
            yield return LlevarJugadorA(EnElSuelo(arena.transform.position));
            Assert.AreEqual(ESTADOS_JUEGO.CUTSCENE, Juego.EstadoActual, "Al cruzar la verja, su presentación.");
            Assert.IsNotNull(Juego.CinematicaActual.musica, "La presentación del Juez lleva su música.");
            Assert.AreSame(Juego.CinematicaActual.musica, Interfaz.Cinematica.Musica.clip);
            // Antes de 0,5 s no se puede saltar: se salta cuando lo haría un jugador con prisa.
            yield return new WaitForSecondsRealtime(0.6f);
            yield return SaltarCinematicaSiHay();
            yield return null;
            yield return null;
            Assert.AreSame(Nivel4.musicaJefe, fuente.clip, "En la plaza, el órgano del Juez.");
            Assert.AreEqual(0.6f, fuente.volume, 1e-3f, "Entra de golpe, tras su presentación.");
        }

        [UnityTest]
        public IEnumerator ElRefugio_GuardaLaPartida()
        {
            yield return EntrarEnNivel4();
            ApagarEnemigos();
            var checkpoint = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Exclude).Single(c => c.Id == "L4_CP1");
            Assert.IsFalse(checkpoint.Activo);

            yield return LlevarJugadorA(EnElSuelo(checkpoint.transform.position));
            Assert.IsTrue(checkpoint.Activo, "Al tocarlo, el refugio es el punto de reaparición.");
            Assert.IsTrue(checkpoint.EcoDeMemoria, "Y suena el eco de memoria.");
        }

        [UnityTest]
        public IEnumerator BajoElArcoDelCampanario_SePasaCorriendo()
        {
            yield return EntrarEnNivel4();
            ApagarEnemigos();
            yield return LlevarJugadorA(new Vector2(60f, 0.05f));
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);
            entrada.EjeHorizontal = 1f;
            yield return new WaitForSeconds(1.2f);
            entrada.EjeHorizontal = 0f;
            Assert.Greater(Jugador.transform.position.x, 66f, "Bajo la torre cabe Kael: pasa al otro lado.");
            Assert.Less(Jugador.transform.position.y, 0.5f, "Por el suelo, sin saltar.");
        }

        [UnityTest]
        public IEnumerator ElRio_LaAlambradaSujetaAKael_YUnDashAcuaticoLaCruza()
        {
            yield return EntrarEnNivel4();
            ApagarEnemigos();
            Jugador.Habilidades.Desbloquear(HABILIDADES.DASH_ACUATICO);

            var alambrada = Object.FindObjectsByType<Marana>(FindObjectsInactive.Exclude).Single(m => m.name == "Alambrada1");
            var caja = alambrada.GetComponent<BoxCollider2D>().bounds;
            yield return LlevarJugadorA(new Vector2(caja.min.x - 1f, caja.min.y + 0.05f));
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);
            entrada.EjeHorizontal = 1f;
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(Jugador.Motor.BajoElAgua, "Bajo el agua del río.");
            Assert.IsTrue(alambrada.Sujetando, "Nadando, la alambrada lo sujeta.");
            Assert.Less(Jugador.transform.position.x, caja.center.x, "Y casi no avanza.");

            entrada.PulsarDash();
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(alambrada.Sujetando, "Ya no lo sujeta.");
            Assert.Greater(Jugador.transform.position.x, caja.max.x, "Está al otro lado.");
            Assert.IsTrue(Jugador.Motor.BajoElAgua, "Todavía bajo el agua: el ojo del puente sigue.");
            entrada.EjeHorizontal = 0f;
        }
    }
}
