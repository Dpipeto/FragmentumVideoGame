using System.Collections;
using Fragmentum.Core;
using Fragmentum.Core.Juego;
using Fragmentum.Core.Save;
using Fragmentum.Levels;
using Fragmentum.Levels.Escenario;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Criterio de aceptación de la Fase 6 (tareas D5 y D8): el Nivel 1 se juega de principio a
    /// fin con checkpoints. Usa la escena real del Nivel 1, preparada por
    /// <c>Fragmentum → Niveles → Preparar</c>, y los sistemas reales con su interfaz.
    /// </summary>
    public class FlujoNivelPlayModeTests : PruebaFlujoBase
    {
        private static LevelManager Nivel => Object.FindAnyObjectByType<LevelManager>();

        private static Checkpoint PrimerCheckpoint => Nivel.Checkpoints.Checkpoints[0];

        [UnityTest]
        public IEnumerator EntrarEnElNivel_ApareceUnSoloJugadorEnElInicio_ConHudYCamara()
        {
            yield return EntrarEnNivel1();

            Assert.AreEqual(1, Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include).Length, "Ni cero jugadores ni dos.");
            Assert.AreSame(Nivel.Jugador, Jugador, "El GameManager engancha al jugador que hizo aparecer el LevelManager.");
            Assert.AreEqual(Nivel.Checkpoints.PuntoInicio.x, Jugador.transform.position.x, 0.5f, "Partida nueva: empieza en el inicio.");

            var camara = Camera.main.GetComponent<CameraMovement>();
            Assert.IsNotNull(camara, "La cámara sigue al jugador.");
            Assert.AreEqual(Nivel1.ColorOscuro, Camera.main.backgroundColor, "El fondo sale de la paleta del nivel.");

            Assert.IsTrue(Interfaz.Hud.gameObject.activeSelf, "En un nivel se ve el HUD.");
            Assert.IsFalse(Interfaz.MenuPausa.Visible);
        }

        [UnityTest]
        public IEnumerator TocarElCheckpoint_LoActivaYGuardaLaPartida()
        {
            yield return EntrarEnNivel1();
            var checkpoint = PrimerCheckpoint;
            Assert.AreEqual("L1_CP1", checkpoint.Id);
            Assert.AreEqual(40f, checkpoint.transform.position.x, 0.01f, "Anexo A: checkpoint en x = 40.");

            yield return LlevarJugadorA(checkpoint.PuntoReaparicion);

            Assert.AreSame(checkpoint, Nivel.Checkpoints.Activo);
            Assert.AreEqual("L1_CP1", Gestor.Datos.checkpoint.checkpointId);
            Assert.AreEqual(1, Gestor.Datos.checkpoint.nivelId);

            var enDisco = new SaveSystem(CarpetaGuardado).Cargar();
            Assert.AreEqual("L1_CP1", enDisco.checkpoint.checkpointId, "El checkpoint se guarda en cuanto se activa.");
            Assert.AreEqual("Punto de control", Interfaz.Hud.AvisoActual, "El HUD avisa al activarlo.");
        }

        [UnityTest]
        public IEnumerator Morir_MuestraElGameOver_YReintentarVuelveAlCheckpoint()
        {
            yield return EntrarEnNivel1();
            yield return LlevarJugadorA(PrimerCheckpoint.PuntoReaparicion);
            yield return LlevarJugadorA(new Vector2(52f, 0.05f));

            Jugador.Vida.Matar();
            yield return EsperarEstado(ESTADOS_JUEGO.GAME_OVER, 3f);
            Assert.IsTrue(Interfaz.PantallaGameOver.Visible);
            Assert.AreEqual(0f, Time.timeScale);

            // Con pantalla, el Game Over espera al jugador: no avanza solo.
            yield return EsperarReal(2f);
            Assert.AreEqual(ESTADOS_JUEGO.GAME_OVER, Juego.EstadoActual);

            Interfaz.PantallaGameOver.Opciones[0].onClick.Invoke(); // Reintentar
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);
            Assert.IsFalse(Interfaz.PantallaGameOver.Visible);
            Assert.IsTrue(Jugador.EstaVivo);
            Assert.AreEqual(40f, Jugador.transform.position.x, 0.5f, "Reaparece en el checkpoint, no al principio.");
            Assert.AreEqual(ESTADOS_JUGADOR.IDLE, Jugador.EstadoActual);
        }

        [UnityTest]
        public IEnumerator SinCheckpoint_ReintentarVuelveAlInicio()
        {
            yield return EntrarEnNivel1();
            var inicio = Nivel.Checkpoints.PuntoInicio;
            yield return LlevarJugadorA(new Vector2(20f, 0.05f));

            Jugador.Vida.Matar();
            yield return EsperarEstado(ESTADOS_JUEGO.GAME_OVER, 3f);
            Assert.IsTrue(Juego.Reintentar());

            Assert.IsTrue(Jugador.EstaVivo);
            Assert.AreEqual(inicio.x, Jugador.transform.position.x, 0.5f);
        }

        [UnityTest]
        public IEnumerator Pausa_MuestraElMenu_YContinuarReanuda()
        {
            yield return EntrarEnNivel1();

            Interfaz.AlternarPausa(); // Esc
            Assert.AreEqual(ESTADOS_JUEGO.PAUSED, Juego.EstadoActual);
            Assert.IsTrue(Interfaz.MenuPausa.Visible);
            Assert.IsTrue(Interfaz.Hud.gameObject.activeSelf, "El HUD sigue debajo del menú.");
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(Interfaz.MenuPausa.Opciones[1].interactable, "El Nivel 1 está en Build Settings: se puede reiniciar.");

            Interfaz.MenuPausa.Opciones[0].onClick.Invoke(); // Continuar
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);
            Assert.IsFalse(Interfaz.MenuPausa.Visible);
            Assert.AreEqual(1f, Time.timeScale);

            Interfaz.AlternarPausa();
            Interfaz.AlternarPausa(); // Esc otra vez reanuda
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);
        }

        [UnityTest]
        public IEnumerator ReiniciarNivel_OlvidaElCheckpoint_YEmpiezaDesdeElInicio()
        {
            yield return EntrarEnNivel1();
            yield return LlevarJugadorA(PrimerCheckpoint.PuntoReaparicion);
            Assert.IsFalse(Gestor.Datos.checkpoint.Vacio);

            Interfaz.AlternarPausa();
            Interfaz.MenuPausa.Opciones[1].onClick.Invoke(); // Reiniciar
            Assert.IsTrue(Gestor.Datos.checkpoint.Vacio, "Reiniciar vuelve al principio: el checkpoint se olvida.");
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 6f);

            Assert.AreEqual(1, Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include).Length);
            Assert.IsNull(Nivel.Checkpoints.Activo);
            Assert.AreEqual(Nivel.Checkpoints.PuntoInicio.x, Jugador.transform.position.x, 0.5f);
        }

        [UnityTest]
        public IEnumerator Continuar_EmpiezaEnElCheckpointGuardado()
        {
            yield return EntrarEnNivel1();
            yield return LlevarJugadorA(PrimerCheckpoint.PuntoReaparicion);

            Assert.IsTrue(Juego.Pausar());
            Assert.IsTrue(Juego.IrAlMenu());
            yield return EsperarEstado(ESTADOS_JUEGO.MAIN_MENU, 6f);

            Assert.IsTrue(Juego.ContinuarPartida());
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 6f);

            Assert.AreEqual("L1_CP1", Nivel.Checkpoints.Activo != null ? Nivel.Checkpoints.Activo.Id : null);
            Assert.AreEqual(40f, Jugador.transform.position.x, 0.5f, "Se continúa desde el checkpoint, no desde el principio.");
        }

        [UnityTest]
        public IEnumerator EntrarEnLaArena_CierraLaPuerta_YActivaALaCentinela()
        {
            yield return EntrarEnNivel1();
            var arena = Object.FindAnyObjectByType<ArenaJefe>();
            var salida = Object.FindAnyObjectByType<SalidaNivel>();
            Assert.IsFalse(salida.Abierta, "Con el jefe vivo, la salida está cerrada: no se puede esquivar el combate.");
            Assert.IsFalse(arena.PuertasCerradas);

            yield return LlevarJugadorA(new Vector2(arena.transform.position.x, 0.05f));

            Assert.IsTrue(arena.EnCombate);
            Assert.IsTrue(arena.Jefe.EnCombate);
            Assert.IsTrue(arena.PuertasCerradas, "La puerta se cierra detrás del jugador.");
            Assert.IsTrue(Interfaz.Hud.JefeVisible, "El HUD muestra la vida del jefe.");
            StringAssert.Contains("CENTINELA ARDIENTE", Interfaz.Hud.NombreJefe);
        }

        [UnityTest]
        public IEnumerator DerrotarALaCentinela_OtorgaFragmentoYHabilidad_YAbreLaSalida()
        {
            yield return EntrarEnNivel1();
            var arena = Object.FindAnyObjectByType<ArenaJefe>();
            var salida = Object.FindAnyObjectByType<SalidaNivel>();
            yield return LlevarJugadorA(new Vector2(arena.transform.position.x, 0.05f));
            Assert.IsFalse(Jugador.Habilidades.Tiene(HABILIDADES.ATAQUE_CARGADO));

            arena.Jefe.Vida.Matar();
            yield return null;
            yield return null;

            // Tarea C: al derrotar al jefe se otorgan el fragmento y la habilidad, en el acto.
            Assert.IsTrue(arena.JefeDerrotado);
            CollectionAssert.Contains(Gestor.Datos.fragmentos, "IRA");
            Assert.IsTrue(Jugador.Habilidades.Tiene(HABILIDADES.ATAQUE_CARGADO), "El Ataque Cargado se puede usar ya, antes de salir.");
            CollectionAssert.Contains(new SaveSystem(CarpetaGuardado).Cargar().fragmentos, "IRA", "Y queda guardado.");
            Assert.AreEqual("Fragmento de la Ira integrado", Interfaz.Hud.AvisoActual);
            Assert.IsFalse(Interfaz.Hud.JefeVisible);
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual, "Derrotar al jefe no termina el nivel: hay que salir.");

            yield return EsperarHastaReal(() => arena.Superada, 4f, "que se abra la arena");
            Assert.IsFalse(arena.PuertasCerradas);
            Assert.IsTrue(salida.Abierta, "Tras la derrota aparece la salida.");

            yield return LlevarJugadorA(new Vector2(salida.transform.position.x, 0.05f));

            Assert.AreEqual(ESTADOS_JUEGO.CUTSCENE, Juego.EstadoActual, "Primero, la cinemática de cierre del mundo.");
            yield return SaltarCinematicaSiHay();
            Assert.AreEqual(ESTADOS_JUEGO.LEVEL_COMPLETE, Juego.EstadoActual);
            Assert.IsTrue(Nivel.Completado);
            Assert.IsTrue(Interfaz.PantallaFinNivel.Visible);
            CollectionAssert.Contains(Gestor.Datos.nivelesCompletados, 1);
            Assert.IsTrue(Gestor.Datos.checkpoint.Vacio, "Al superar el nivel se olvida su checkpoint.");

            yield return EsperarReal(2f);
            Assert.AreEqual(ESTADOS_JUEGO.LEVEL_COMPLETE, Juego.EstadoActual, "Con pantalla, el fin de nivel espera al jugador.");

            ContinuarDesdeFinDeNivel();
            yield return EsperarEstado(ESTADOS_JUEGO.LEVEL_SELECT, 6f);
            Assert.IsFalse(Interfaz.Hud.gameObject.activeSelf, "Fuera de los niveles no hay HUD.");
        }

        [UnityTest]
        public IEnumerator MorirEnLaArena_ReiniciaElCombate()
        {
            yield return EntrarEnNivel1();
            var arena = Object.FindAnyObjectByType<ArenaJefe>();
            yield return LlevarJugadorA(new Vector2(arena.transform.position.x, 0.05f));
            arena.Jefe.Vida.RecibirGolpe(new Fragmentum.Core.Combate.Golpe(10, arena.Jefe.Centro));

            Jugador.Vida.Matar();
            yield return EsperarEstado(ESTADOS_JUEGO.GAME_OVER, 3f);
            Interfaz.PantallaGameOver.Opciones[0].onClick.Invoke(); // Reintentar

            Assert.IsFalse(arena.EnCombate);
            Assert.IsFalse(arena.Jefe.EnCombate, "El jefe espera otra vez a que entre el jugador.");
            Assert.AreEqual(arena.Jefe.Vida.VidaMaxima, arena.Jefe.Vida.Vida, "Y recupera toda la vida.");
            Assert.IsFalse(arena.PuertasCerradas);
            Assert.IsFalse(Interfaz.Hud.JefeVisible);
            Assert.Less(Jugador.transform.position.x, arena.Limites.x, "El jugador reaparece fuera de la arena.");

            // Unos pasos de física fuera de la entrada: sin ellos, Box2D no registra la salida del
            // trigger y no volvería a avisar de la entrada (un jugador real vuelve caminando).
            for (var i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            yield return LlevarJugadorA(new Vector2(arena.transform.position.x, 0.05f));
            Assert.IsTrue(arena.EnCombate, "Se puede volver a entrar.");
        }

        private static IEnumerator EsperarHastaReal(System.Func<bool> condicion, float maximo, string descripcion)
        {
            var limite = Time.unscaledTime + maximo;
            while (!condicion())
            {
                if (Time.unscaledTime > limite)
                {
                    Assert.Fail($"Tiempo agotado ({maximo:0.0} s) esperando {descripcion}.");
                }

                yield return null;
            }
        }
    }
}
