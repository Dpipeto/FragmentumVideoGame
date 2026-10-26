using System;
using System.Collections;
using System.IO;
using Fragmentum.Core;
using Fragmentum.Core.Juego;
using Fragmentum.Data;
using Fragmentum.Player;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Base de las pruebas de extremo a extremo del flujo de juego: sistemas persistentes
    /// reales (el mismo prefab que usa el juego), escenas reales de menú, selector y niveles, y
    /// un guardado en una carpeta temporal para no tocar la partida del usuario.
    /// </summary>
    public abstract class PruebaFlujoBase
    {
        private const string RutaBaseDeDatos = "Assets/Data/Niveles/LevelDatabase.asset";

        private string _carpeta;

        protected GameObject Sistemas { get; private set; }

        protected LevelDatabase BaseDeDatos { get; private set; }

        protected string CarpetaGuardado => _carpeta;

        protected static GameManager Gestor => GameManager.Instancia;

        protected static GameStateMachine Juego => GameStateMachine.Instancia;

        protected UIManager Interfaz => Sistemas != null ? Sistemas.GetComponent<UIManager>() : null;

        protected static PlayerController Jugador => Gestor != null ? Gestor.Jugador : null;

        protected LevelData Nivel1 => BaseDeDatos.niveles[0];

        [UnitySetUp]
        public IEnumerator PrepararSistemas()
        {
            _carpeta = Path.Combine(Path.GetTempPath(), "FragmentumPruebas", Guid.NewGuid().ToString("N"));
            Time.timeScale = 1f;
            EventosJuego.Reiniciar();
#if UNITY_EDITOR
            BaseDeDatos = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDatabase>(RutaBaseDeDatos);
#endif
            Assert.IsNotNull(BaseDeDatos, "Falta la LevelDatabase: ejecuta Fragmentum → Regenerar todo.");

            SceneManager.LoadScene(BaseDeDatos.escenaMenu.Nombre);
            yield return null;
            yield return null;

            Sistemas = Bootstrap.CrearSistemas();
            Gestor.UsarCarpetaDeGuardado(_carpeta);
            AjustesAudio.UsarCarpeta(_carpeta); // el volumen, también aparte del del jugador

            // Las pruebas que no son de narrativa no deben esperar a la cinemática inicial.
            if (CinematicaInicialVista)
            {
                foreach (var nivel in BaseDeDatos.niveles)
                {
                    if (nivel != null && nivel.cinematicaEntrada != null && nivel.cinematicaEntrada.soloPrimeraVez)
                    {
                        Gestor.Datos.cinematicasVistas.Add(nivel.cinematicaEntrada.Id);
                    }
                }
            }

            yield return EsperarEstado(ESTADOS_JUEGO.MAIN_MENU, 3f);
        }

        /// <summary>Partir de una partida en la que ya se vio la cinemática inicial.</summary>
        protected virtual bool CinematicaInicialVista => true;

        /// <summary>Si hay una cinemática en curso (por ejemplo, la de cierre), la salta.</summary>
        protected static IEnumerator SaltarCinematicaSiHay()
        {
            if (Juego.EstadoActual == ESTADOS_JUEGO.CUTSCENE)
            {
                Assert.IsTrue(Juego.TerminarCinematica());
            }

            yield return null;
        }

        /// <summary>En el fin de nivel: omite el registro emocional y pulsa «Continuar».</summary>
        protected void ContinuarDesdeFinDeNivel()
        {
            var pantalla = Interfaz.PantallaFinNivel;
            if (pantalla.MostrandoRegistro)
            {
                pantalla.OmitirRegistro();
            }

            pantalla.BotonContinuar.onClick.Invoke();
        }

        [UnityTearDown]
        public IEnumerator LimpiarSistemas()
        {
            if (Sistemas != null)
            {
                Object.Destroy(Sistemas);
            }

            yield return null;
            Time.timeScale = 1f;
            EventosJuego.Reiniciar();

            // Se deja una escena vacía para que las siguientes pruebas no hereden la del nivel.
            var vacia = SceneManager.CreateScene($"Vacia_{Guid.NewGuid():N}");
            SceneManager.SetActiveScene(vacia);
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var escena = SceneManager.GetSceneAt(i);
                if (escena != vacia)
                {
                    yield return SceneManager.UnloadSceneAsync(escena);
                }
            }

            if (Directory.Exists(_carpeta))
            {
                Directory.Delete(_carpeta, true);
            }
        }

        /// <summary>Entra en el Nivel 1 desde el menú y espera a estar jugando.</summary>
        protected IEnumerator EntrarEnNivel1()
        {
            Assert.IsTrue(Juego.JugarNivel(Nivel1));
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);
            Assert.IsNotNull(Jugador, "El LevelManager del Nivel 1 hace aparecer al jugador.");
        }

        /// <summary>
        /// Coloca al jugador en un punto y deja correr la física para que se disparen los
        /// triggers que haya ahí (checkpoints, salida).
        /// </summary>
        protected static IEnumerator LlevarJugadorA(Vector2 posicion)
        {
            Jugador.Revivir(posicion);

            // Si un trigger para el tiempo (la salida lleva al fin de nivel), no habrá más pasos
            // de física que esperar.
            for (var i = 0; i < 6 && Time.timeScale > 0f; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        /// <summary>
        /// Los pies del jugador sobre un objeto colocado en el suelo (la entrada de la arena, la
        /// salida): el Nivel 1 no tiene el suelo a la misma altura en todo el recorrido.
        /// </summary>
        protected static Vector2 EnElSuelo(Vector3 objeto) => new Vector2(objeto.x, objeto.y + 0.05f);

        /// <summary>Espera un estado de la FSM global, con tiempo sin escala (varios estados paran el tiempo).</summary>
        protected static IEnumerator EsperarEstado(ESTADOS_JUEGO estado, float maximo)
        {
            var limite = Time.unscaledTime + maximo;
            while (Juego == null || Juego.EstadoActual != estado)
            {
                if (Time.unscaledTime > limite)
                {
                    Assert.Fail($"Tiempo agotado ({maximo:0.0} s) esperando {estado}; estado actual: {(Juego != null ? Juego.EstadoActual.ToString() : "sin FSM")}.");
                }

                yield return null;
            }
        }

        /// <summary>Espera segundos reales (sin escala: funciona con el juego en pausa).</summary>
        protected static IEnumerator EsperarReal(float segundos)
        {
            var fin = Time.unscaledTime + segundos;
            while (Time.unscaledTime < fin)
            {
                yield return null;
            }
        }
    }
}
