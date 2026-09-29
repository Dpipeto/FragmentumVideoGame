using System;
using System.Collections;
using System.IO;
using Fragmentum.Core;
using Fragmentum.Core.Juego;
using Fragmentum.Core.Save;
using Fragmentum.Data;
using Fragmentum.Player;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Pruebas de extremo a extremo de la progresión (tareas D y E): sistemas persistentes
    /// reales, escenas reales de menú, selector y Nivel 1, y un guardado en una carpeta
    /// temporal para no tocar la partida del usuario.
    /// </summary>
    public class ProgresionPlayModeTests
    {
        private const string RutaBaseDeDatos = "Assets/Data/Niveles/LevelDatabase.asset";
        private const string RutaPrefabJugador = "Assets/Prefabs/Player.prefab";

        private string _carpeta;
        private GameObject _sistemas;
        private LevelDatabase _bd;

        private static GameManager Gestor => GameManager.Instancia;

        private static GameStateMachine Juego => GameStateMachine.Instancia;

        [UnitySetUp]
        public IEnumerator Preparar()
        {
            _carpeta = Path.Combine(Path.GetTempPath(), "FragmentumPruebas", Guid.NewGuid().ToString("N"));
            Time.timeScale = 1f;
            EventosJuego.Reiniciar();
#if UNITY_EDITOR
            _bd = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDatabase>(RutaBaseDeDatos);
#endif
            Assert.IsNotNull(_bd, "Falta la LevelDatabase: ejecuta Fragmentum → Regenerar todo.");

            SceneManager.LoadScene(_bd.escenaMenu.Nombre);
            yield return null;
            yield return null;

            _sistemas = Bootstrap.CrearSistemas();
            Gestor.UsarCarpetaDeGuardado(_carpeta);
            yield return EsperarEstado(ESTADOS_JUEGO.MAIN_MENU, 3f);
        }

        [UnityTearDown]
        public IEnumerator Limpiar()
        {
            if (_sistemas != null)
            {
                Object.Destroy(_sistemas);
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

        // --- Criterios: el selector solo ofrece lo válido; el progreso persiste ---

        [UnityTest]
        public IEnumerator Flujo_MenuSelectorNivelCompletado_DesbloqueaYGuarda()
        {
            Assert.IsTrue(Juego.IrAlSelector());
            yield return EsperarEstado(ESTADOS_JUEGO.LEVEL_SELECT, 5f);
            Assert.AreEqual(_bd.escenaSelector.Nombre, SceneManager.GetActiveScene().name);

            var selector = Object.FindAnyObjectByType<SelectorNivel>();
            Assert.AreEqual(_bd.niveles.Count, selector.Botones.Count, "Un botón por nivel de la base de datos, ni uno más.");
            Assert.IsTrue(selector.Botones[0].interactable, "El primer mundo está disponible.");
            Assert.IsFalse(selector.Botones[1].interactable, "El segundo está bloqueado en una partida nueva.");
            StringAssert.Contains(_bd.niveles[0].emocion.nombreCanonico, Etiqueta(selector.Botones[0]),
                "El botón nombra la emoción, no «Level 1».");

            selector.Botones[0].onClick.Invoke();
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);
            Assert.AreEqual(_bd.niveles[0].escena.Nombre, SceneManager.GetActiveScene().name);
            Assert.AreSame(_bd.niveles[0], Gestor.NivelActual);

            Assert.IsTrue(Juego.CompletarNivel());
            Assert.AreEqual(ESTADOS_JUEGO.LEVEL_COMPLETE, Juego.EstadoActual);
            Assert.AreEqual(0f, Time.timeScale, "El fin de nivel detiene el tiempo.");

            // Sin pantalla de fin de nivel (Fase 6), continúa solo; el nivel 2 no tiene escena → selector.
            yield return EsperarEstado(ESTADOS_JUEGO.LEVEL_SELECT, 8f);

            Assert.AreEqual(2, Gestor.Datos.nivelMaximoAlcanzado);
            CollectionAssert.Contains(Gestor.Datos.fragmentos, "IRA");
            CollectionAssert.Contains(Gestor.Datos.habilidades, "ATAQUE_CARGADO");

            var enDisco = new SaveSystem(_carpeta).Cargar();
            Assert.AreEqual(2, enDisco.nivelMaximoAlcanzado, "El progreso debe sobrevivir a cerrar el juego.");
            CollectionAssert.Contains(enDisco.nivelesCompletados, 1);
            CollectionAssert.Contains(enDisco.fragmentos, "IRA");

            selector = Object.FindAnyObjectByType<SelectorNivel>();
            Assert.IsFalse(selector.Botones[1].interactable, "Desbloqueado pero sin escena: no se puede pulsar.");
            StringAssert.Contains("En construcción", Etiqueta(selector.Botones[1]));
            StringAssert.Contains("Fragmento integrado", Etiqueta(selector.Botones[0]));
        }

        [UnityTest]
        public IEnumerator NivelSinEscenaOBloqueado_NoSeCargaNiRompeNada()
        {
            Assert.IsFalse(Juego.JugarNivel(_bd.niveles[1]), "Nivel sin escena: «en construcción».");
            Assert.IsFalse(Juego.JugarNivel(_bd.niveles[5]), "El final no se abre desde el principio.");
            Assert.IsFalse(SceneLoader.PuedeCargar("Level2"), "Los nombres de la v1 no existen.");

            yield return null;
            Assert.AreEqual(ESTADOS_JUEGO.MAIN_MENU, Juego.EstadoActual);
            Assert.AreEqual(_bd.escenaMenu.Nombre, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator Continuar_LlevaAlNivelDesbloqueadoMasAvanzadoConEscena()
        {
            Gestor.Datos.nivelMaximoAlcanzado = 3;

            Assert.IsTrue(Juego.ContinuarPartida());
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);

            Assert.AreSame(_bd.niveles[0], Gestor.NivelActual, "Solo el Nivel 1 tiene escena todavía.");
        }

        [UnityTest]
        public IEnumerator Pausa_CongelaElTiempoYBloqueaElInput()
        {
            Assert.IsFalse(Juego.Pausar(), "En el menú no hay pausa.");

            Assert.IsTrue(Juego.JugarNivel(_bd.niveles[0]));
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(EventosJuego.EntradaBloqueada);

            Assert.IsTrue(Juego.Pausar());
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(EventosJuego.EntradaBloqueada);

            Assert.IsTrue(Juego.Reanudar());
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(EventosJuego.EntradaBloqueada);
        }

        [UnityTest]
        public IEnumerator GameOver_SinPantalla_ReintentaSolo()
        {
            Assert.IsTrue(Juego.JugarNivel(_bd.niveles[0]));
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);
            var jugador = CrearJugador();

            var reintentos = 0;
            EventosJuego.ReintentoSolicitado += () => reintentos++;

            jugador.Vida.Matar();
            Assert.AreEqual(ESTADOS_JUEGO.GAME_OVER, Juego.EstadoActual, "La muerte del jugador en un nivel lleva al Game Over.");
            Assert.AreEqual(0f, Time.timeScale);

            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 4f);
            Assert.AreEqual(1, reintentos, "Al volver a jugar se pide reaparecer en el checkpoint.");
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Monedas_SeGuardanAlVolverAlMenu()
        {
            Assert.IsTrue(Juego.JugarNivel(_bd.niveles[0]));
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);

            Gestor.Economia.Sumar(5);
            Assert.IsTrue(Juego.Pausar());
            Assert.IsTrue(Juego.IrAlMenu());
            yield return EsperarEstado(ESTADOS_JUEGO.MAIN_MENU, 5f);

            Assert.AreEqual(5, new SaveSystem(_carpeta).Cargar().monedas, "En la v1 las monedas no se guardaban nunca.");
        }

        [UnityTest]
        public IEnumerator EnUnNivel_SeAplicanLasHabilidadesGuardadasYLasReglas()
        {
            Gestor.Datos.habilidades.Add("DASH_ACUATICO");
            Assert.IsTrue(Juego.JugarNivel(_bd.niveles[0]));
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);

            var jugador = CrearJugador();

            Assert.IsTrue(jugador.Habilidades.Tiene(HABILIDADES.DASH_ACUATICO), "Las habilidades vienen del progreso guardado.");
            Assert.IsFalse(jugador.Habilidades.Tiene(HABILIDADES.ATAQUE_CARGADO));
            Assert.IsFalse(jugador.PermitirAgacharse, "En el mundo de la Ira no hay cobertura.");
        }

        // --- Utilidades ---

        private PlayerController CrearJugador()
        {
            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador);
#endif
            Assert.IsNotNull(prefab);
            var jugador = Object.Instantiate(prefab, new Vector3(0f, 50f, 0f), Quaternion.identity).GetComponent<PlayerController>();
            Gestor.RegistrarJugador(jugador);
            return jugador;
        }

        private static string Etiqueta(Button boton) => boton.GetComponentInChildren<Text>().text;

        // Con tiempo sin escala: varios estados detienen Time.timeScale.
        private static IEnumerator EsperarEstado(ESTADOS_JUEGO estado, float maximo)
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
    }
}
