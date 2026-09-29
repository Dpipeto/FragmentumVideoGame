using System.Collections;
using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Core.Juego;
using Fragmentum.Core.Save;
using Fragmentum.Player;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Pruebas de extremo a extremo de la progresión (tareas D y E): menú, selector, guardado,
    /// pausa y la red de seguridad del Game Over sin pantalla.
    /// </summary>
    public class ProgresionPlayModeTests : PruebaFlujoBase
    {
        // --- Criterios: el selector solo ofrece lo válido; el progreso persiste ---

        [UnityTest]
        public IEnumerator Flujo_MenuSelectorNivelCompletado_DesbloqueaYGuarda()
        {
            var bd = BaseDeDatos;
            Assert.IsTrue(Juego.IrAlSelector());
            yield return EsperarEstado(ESTADOS_JUEGO.LEVEL_SELECT, 5f);
            Assert.AreEqual(bd.escenaSelector.Nombre, SceneManager.GetActiveScene().name);

            var selector = Object.FindAnyObjectByType<SelectorNivel>();
            Assert.AreEqual(bd.niveles.Count, selector.Botones.Count, "Un botón por nivel de la base de datos, ni uno más.");
            Assert.IsTrue(selector.Botones[0].interactable, "El primer mundo está disponible.");
            Assert.IsFalse(selector.Botones[1].interactable, "El segundo está bloqueado en una partida nueva.");
            StringAssert.Contains(bd.niveles[0].emocion.nombreCanonico, Etiqueta(selector.Botones[0]),
                "El botón nombra la emoción, no «Level 1».");

            selector.Botones[0].onClick.Invoke();
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);
            Assert.AreEqual(bd.niveles[0].escena.Nombre, SceneManager.GetActiveScene().name);
            Assert.AreSame(bd.niveles[0], Gestor.NivelActual);

            Assert.IsTrue(Juego.CompletarNivel());
            Assert.AreEqual(ESTADOS_JUEGO.LEVEL_COMPLETE, Juego.EstadoActual);
            Assert.AreEqual(0f, Time.timeScale, "El fin de nivel detiene el tiempo.");

            // La pantalla de fin de nivel espera al jugador; el nivel 2 no tiene escena → selector.
            Assert.IsTrue(Interfaz.PantallaFinNivel.Visible);
            Interfaz.PantallaFinNivel.Opciones[0].onClick.Invoke();
            yield return EsperarEstado(ESTADOS_JUEGO.LEVEL_SELECT, 8f);

            Assert.AreEqual(2, Gestor.Datos.nivelMaximoAlcanzado);
            CollectionAssert.Contains(Gestor.Datos.fragmentos, "IRA");
            CollectionAssert.Contains(Gestor.Datos.habilidades, "ATAQUE_CARGADO");

            var enDisco = new SaveSystem(CarpetaGuardado).Cargar();
            Assert.AreEqual(2, enDisco.nivelMaximoAlcanzado, "El progreso debe sobrevivir a cerrar el juego.");
            CollectionAssert.Contains(enDisco.nivelesCompletados, 1);
            CollectionAssert.Contains(enDisco.fragmentos, "IRA");

            selector = Object.FindAnyObjectByType<SelectorNivel>();
            Assert.IsFalse(selector.Botones[1].interactable, "Desbloqueado pero sin escena: no se puede pulsar.");
            StringAssert.Contains("En construcción", Etiqueta(selector.Botones[1]));
            StringAssert.Contains("Fragmento integrado", Etiqueta(selector.Botones[0]));
        }

        [UnityTest]
        public IEnumerator DuranteLaCarga_ElFundidoSeTragaLosClics()
        {
            // En la prueba manual, un doble clic en el selector pedía una segunda carga.
            Assert.IsTrue(Juego.IrAlSelector());
            yield return null;
            yield return null;

            var datos = new PointerEventData(EventSystem.current) { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) };
            var impactos = new List<RaycastResult>();
            EventSystem.current.RaycastAll(datos, impactos);

            Assert.IsNotEmpty(impactos, "En el centro del menú hay un botón.");
            Assert.AreEqual("Negro", impactos[0].gameObject.name, "Mientras carga, el primer objeto bajo el puntero es el fundido, no un botón.");
            yield return EsperarEstado(ESTADOS_JUEGO.LEVEL_SELECT, 5f);
        }

        [UnityTest]
        public IEnumerator NivelSinEscenaOBloqueado_NoSeCargaNiRompeNada()
        {
            Assert.IsFalse(Juego.JugarNivel(BaseDeDatos.niveles[1]), "Nivel sin escena: «en construcción».");
            Assert.IsFalse(Juego.JugarNivel(BaseDeDatos.niveles[5]), "El final no se abre desde el principio.");
            Assert.IsFalse(SceneLoader.PuedeCargar("Level2"), "Los nombres de la v1 no existen.");

            yield return null;
            Assert.AreEqual(ESTADOS_JUEGO.MAIN_MENU, Juego.EstadoActual);
            Assert.AreEqual(BaseDeDatos.escenaMenu.Nombre, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator Continuar_LlevaAlNivelDesbloqueadoMasAvanzadoConEscena()
        {
            Gestor.Datos.nivelMaximoAlcanzado = 3;

            Assert.IsTrue(Juego.ContinuarPartida());
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);

            Assert.AreSame(Nivel1, Gestor.NivelActual, "Solo el Nivel 1 tiene escena todavía.");
        }

        [UnityTest]
        public IEnumerator Pausa_CongelaElTiempoYBloqueaElInput()
        {
            Assert.IsFalse(Juego.Pausar(), "En el menú no hay pausa.");

            yield return EntrarEnNivel1();
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(EventosJuego.EntradaBloqueada);

            Assert.IsTrue(Juego.Pausar());
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(EventosJuego.EntradaBloqueada);

            Assert.IsTrue(Juego.Reanudar());
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(EventosJuego.EntradaBloqueada);
            Assert.IsTrue(EventosJuego.EntradaIgnorada, "El fotograma en que se sale de la pausa no cuenta: el clic de «Continuar» no ataca.");

            yield return null;
            Assert.IsFalse(EventosJuego.EntradaIgnorada);
        }

        [UnityTest]
        public IEnumerator GameOver_SinPantalla_ReintentaSolo()
        {
            // Sin UIManager (sistemas creados sin él), la FSM no se queda esperando una pantalla.
            Object.DestroyImmediate(Interfaz);
            Assert.IsFalse(GameStateMachine.HayPantallaGameOver);

            yield return EntrarEnNivel1();
            var reintentos = 0;
            EventosJuego.ReintentoSolicitado += () => reintentos++;

            Jugador.Vida.Matar();
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual, "Se deja ver la caída antes del Game Over.");
            yield return EsperarEstado(ESTADOS_JUEGO.GAME_OVER, 3f);
            Assert.AreEqual(0f, Time.timeScale);

            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 4f);
            Assert.AreEqual(1, reintentos, "Al volver a jugar se pide reaparecer en el checkpoint.");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(Jugador.EstaVivo, "El LevelManager hizo reaparecer al jugador.");
        }

        [UnityTest]
        public IEnumerator Monedas_SeGuardanAlVolverAlMenu()
        {
            yield return EntrarEnNivel1();

            Gestor.Economia.Sumar(5);
            Assert.IsTrue(Juego.Pausar());
            Assert.IsTrue(Juego.IrAlMenu());
            yield return EsperarEstado(ESTADOS_JUEGO.MAIN_MENU, 5f);

            Assert.AreEqual(5, new SaveSystem(CarpetaGuardado).Cargar().monedas, "En la v1 las monedas no se guardaban nunca.");
        }

        [UnityTest]
        public IEnumerator EnUnNivel_SeAplicanLasHabilidadesGuardadasYLasReglas()
        {
            Gestor.Datos.habilidades.Add("DASH_ACUATICO");
            yield return EntrarEnNivel1();

            Assert.IsTrue(Jugador.Habilidades.Tiene(HABILIDADES.DASH_ACUATICO), "Las habilidades vienen del progreso guardado.");
            Assert.IsFalse(Jugador.Habilidades.Tiene(HABILIDADES.ATAQUE_CARGADO));
            Assert.IsFalse(Jugador.PermitirAgacharse, "En el mundo de la Ira no hay cobertura.");
        }

        private static string Etiqueta(Button boton) => boton.GetComponentInChildren<Text>().text;
    }
}
