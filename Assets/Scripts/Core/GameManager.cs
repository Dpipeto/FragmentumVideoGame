using System;
using System.Collections.Generic;
using Fragmentum.Core.Save;
using Fragmentum.Data;
using Fragmentum.Narrative;
using Fragmentum.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fragmentum.Core
{
    /// <summary>
    /// Estado global y progresión del juego (Singleton persistente, el único del proyecto).
    ///
    /// Qué corrige respecto de la v1 (auditoría, sección 3.4):
    /// - No tenía <c>DontDestroyOnLoad</c>: se destruía en cada cambio de escena y no
    ///   persistía nada. Ahora vive en los sistemas que crea <see cref="Bootstrap"/>.
    /// - Mezclaba UI de Game Over, monedas y navegación. Ahora solo lleva la progresión; la
    ///   navegación está en <see cref="GameStateMachine"/>, la pantalla de Game Over en la UI
    ///   (<c>UIManager</c>) y, en lugar de monedas, hay puntaje (<c>MarcadorNivel</c>).
    /// - El progreso se guarda en JSON (<see cref="SaveSystem"/>): al completar un nivel, al
    ///   activar un checkpoint, al volver al menú y al cerrar el juego.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        [Tooltip("Fuente única de niveles y escenas.")]
        [SerializeField] private LevelDatabase baseDeDatos;

        private PlayerController _jugadorEnganchado;
        private Vector2 _inicioJugador;
        private bool _escenaInicialProcesada;

        public static GameManager Instancia { get; private set; }

        public LevelDatabase BaseDeDatos => baseDeDatos;

        public SaveSystem Guardado { get; private set; }

        /// <summary>
        /// Registro emocional (tarea G), en la misma carpeta que la partida pero en su propio
        /// archivo: «Nueva partida» no lo borra.
        /// </summary>
        public RegistroEmocional Registro { get; private set; }

        /// <summary>La partida en curso. Nunca es null.</summary>
        public DatosGuardado Datos { get; private set; } = new DatosGuardado();

        /// <summary>Nivel de la escena actual, o null si la escena no es un nivel (menú, pruebas).</summary>
        public LevelData NivelActual { get; private set; }

        /// <summary>Jugador de la escena actual, si lo hay.</summary>
        public PlayerController Jugador => _jugadorEnganchado;

        public event Action<LevelData> OnNivelIniciado;
        public event Action<LevelData> OnNivelCompletado;
        public event Action<FragmentoData> OnFragmentoObtenido;
        public event Action OnProgresoCambiado;

        /// <summary>Cambió el jugador de la escena (null si la escena no tiene). Lo escucha el HUD.</summary>
        public event Action<PlayerController> OnJugadorRegistrado;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            Instancia = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (baseDeDatos == null)
            {
                Debug.LogWarning("[GameManager] Falta la LevelDatabase: no habrá menú, selector ni progresión. Ejecuta Fragmentum → Regenerar todo.", this);
            }

            UsarCarpetaDeGuardado(null);
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        private void Start()
        {
            // Si los sistemas se crearon con la escena ya cargada (pruebas), se procesa aquí.
            if (!_escenaInicialProcesada)
            {
                AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            }
        }

        private void OnDestroy()
        {
            if (Instancia != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= AlCargarEscena;
            SoltarJugador();
            Instancia = null;
        }

        private void OnApplicationQuit() => Guardar();

        // --- Guardado ---

        /// <summary>
        /// Usa otra carpeta de guardado y recarga la partida desde ella. Con null vuelve a
        /// <c>persistentDataPath</c>. Lo usan las pruebas para no tocar la partida real.
        /// </summary>
        public void UsarCarpetaDeGuardado(string carpeta)
        {
            Guardado = new SaveSystem(carpeta);
            Registro = new RegistroEmocional(carpeta);
            Datos = Guardado.Cargar();
            OnProgresoCambiado?.Invoke();
        }

        public bool Guardar() => Guardado.Guardar(Datos);

        /// <summary>Empieza una partida desde cero. La anterior queda en la copia de seguridad.</summary>
        public void NuevaPartida()
        {
            Datos = new DatosGuardado();
            Guardar();
            AplicarProgresoAlJugador();
            OnProgresoCambiado?.Invoke();
        }

        // --- Progresión ---

        public bool EstaDesbloqueado(LevelData nivel) =>
            baseDeDatos != null && baseDeDatos.EstaDesbloqueado(nivel, Datos);

        /// <summary>
        /// Marca el nivel como completado, desbloquea el siguiente, otorga su fragmento y su
        /// habilidad, borra el checkpoint y guarda.
        /// </summary>
        public void CompletarNivel(LevelData nivel)
        {
            if (nivel == null)
            {
                Debug.LogWarning("[GameManager] CompletarNivel sin nivel: la escena actual no está en la LevelDatabase.");
                return;
            }

            DatosGuardado.Anadir(Datos.nivelesCompletados, nivel.id);

            var siguiente = baseDeDatos != null ? baseDeDatos.Siguiente(nivel) : null;
            if (siguiente != null)
            {
                Datos.nivelMaximoAlcanzado = Mathf.Max(Datos.nivelMaximoAlcanzado, siguiente.id);
            }

            Recompensar(nivel);
            Datos.checkpoint = new DatosCheckpoint();
            Guardar();
            OnNivelCompletado?.Invoke(nivel);
            OnProgresoCambiado?.Invoke();
        }

        /// <summary>
        /// Otorga ya el fragmento y la habilidad del nivel, y guarda. La llama la derrota del jefe
        /// (tarea C: «al derrotar a un jefe se otorga el fragmento y se desbloquea la habilidad»),
        /// así que el jugador puede usar la habilidad nueva antes de salir del nivel. Completar el
        /// nivel después no repite nada.
        /// </summary>
        public void OtorgarRecompensa(LevelData nivel)
        {
            if (nivel == null)
            {
                return;
            }

            Recompensar(nivel);
            Guardar();
            OnProgresoCambiado?.Invoke();
        }

        // Fragmento y habilidad sin duplicar; la habilidad se desbloquea también en el jugador vivo.
        private void Recompensar(LevelData nivel)
        {
            if (nivel.fragmento != null && DatosGuardado.Anadir(Datos.fragmentos, nivel.fragmento.id.ToString()))
            {
                OnFragmentoObtenido?.Invoke(nivel.fragmento);
            }

            var habilidad = nivel.habilidadOtorgada != null ? nivel.habilidadOtorgada
                : nivel.fragmento != null ? nivel.fragmento.habilidad : null;
            if (habilidad != null && DatosGuardado.Anadir(Datos.habilidades, habilidad.id.ToString()) &&
                _jugadorEnganchado != null && _jugadorEnganchado.Habilidades != null)
            {
                _jugadorEnganchado.Habilidades.Desbloquear(habilidad.id);
            }
        }

        /// <summary>¿Hay que reproducir esta cinemática? No, si es de «solo la primera vez» y ya se vio.</summary>
        public bool DebeVerse(CutsceneData cinematica) =>
            cinematica != null && (!cinematica.soloPrimeraVez || !Datos.cinematicasVistas.Contains(cinematica.Id));

        /// <summary>Anota que se vio una cinemática de «solo la primera vez» y guarda.</summary>
        public void MarcarVista(CutsceneData cinematica)
        {
            if (cinematica != null && cinematica.soloPrimeraVez && DatosGuardado.Anadir(Datos.cinematicasVistas, cinematica.Id))
            {
                Guardar();
            }
        }

        /// <summary>Guarda el checkpoint activado. Lo llama el CheckpointManager.</summary>
        public void RegistrarCheckpoint(string checkpointId)
        {
            Datos.checkpoint = new DatosCheckpoint { nivelId = NivelActual != null ? NivelActual.id : 0, checkpointId = checkpointId };
            Guardar();
        }

        /// <summary>Olvida el checkpoint guardado: «Reiniciar nivel» vuelve al principio.</summary>
        public void BorrarCheckpoint()
        {
            if (Datos.checkpoint.Vacio)
            {
                return;
            }

            Datos.checkpoint = new DatosCheckpoint();
            Guardar();
        }

        /// <summary>El checkpoint guardado si pertenece a ese nivel; si no, null.</summary>
        public DatosCheckpoint CheckpointDe(LevelData nivel) =>
            nivel != null && !Datos.checkpoint.Vacio && Datos.checkpoint.nivelId == nivel.id ? Datos.checkpoint : null;

        /// <summary>Habilidades guardadas, ya convertidas a su enum (las desconocidas se ignoran).</summary>
        public List<HABILIDADES> HabilidadesGuardadas()
        {
            var lista = new List<HABILIDADES>();
            foreach (var nombre in Datos.habilidades)
            {
                if (Enum.TryParse(nombre, out HABILIDADES habilidad))
                {
                    lista.Add(habilidad);
                }
            }

            return lista;
        }

        // --- Escenas y jugador ---

        /// <summary>
        /// Engancha al jugador de la escena: le aplica el progreso y las reglas del nivel y
        /// convierte su muerte en un evento global. Se llama sola al cargar cada escena (el
        /// LevelManager hace aparecer al jugador antes, en su Awake).
        /// </summary>
        public void RegistrarJugador(PlayerController jugador)
        {
            SoltarJugador();
            _jugadorEnganchado = jugador;
            if (jugador != null)
            {
                _inicioJugador = jugador.transform.position;
                if (jugador.Vida != null)
                {
                    jugador.Vida.OnMuerte += AlMorirJugador;
                }

                AplicarProgresoAlJugador();
            }

            OnJugadorRegistrado?.Invoke(jugador);
        }

        /// <summary>
        /// Red de seguridad del reintento: si tras pedirlo el jugador sigue muerto (la escena no
        /// tiene LevelManager), se le revive donde apareció. Nunca se queda el juego atascado.
        /// </summary>
        public void RevivirJugadorSiSigueMuerto()
        {
            if (_jugadorEnganchado == null || _jugadorEnganchado.EstaVivo)
            {
                return;
            }

            Debug.LogWarning("[GameManager] Nadie hizo reaparecer al jugador (¿falta el LevelManager en la escena?): se le revive donde empezó.", _jugadorEnganchado);
            _jugadorEnganchado.Revivir(_inicioJugador);
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (modo != LoadSceneMode.Single)
            {
                return;
            }

            _escenaInicialProcesada = true;
            NivelActual = baseDeDatos != null ? baseDeDatos.PorEscena(escena.name) : null;

            // Solo en la escena que entra: en una carga asíncrona aún puede quedar la anterior.
            RegistrarJugador(PlayerController.BuscarEnEscena(escena));

            if (NivelActual != null)
            {
                if (_jugadorEnganchado == null)
                {
                    Debug.LogWarning($"[GameManager] El nivel '{NivelActual.Titulo}' no tiene jugador. Prepara la escena con Fragmentum → Niveles → Preparar la escena de nivel abierta (añade el LevelManager, que lo hace aparecer).");
                }

                OnNivelIniciado?.Invoke(NivelActual);
            }
        }

        // Solo en los niveles del juego se imponen el progreso y las reglas: las escenas de
        // prueba conservan las habilidades que tienen configuradas.
        private void AplicarProgresoAlJugador()
        {
            if (_jugadorEnganchado == null || NivelActual == null)
            {
                return;
            }

            if (_jugadorEnganchado.Habilidades != null)
            {
                _jugadorEnganchado.Habilidades.FijarDesbloqueadas(HabilidadesGuardadas());
            }

            _jugadorEnganchado.PermitirAgacharse = NivelActual.reglas.agacharse;
            var modo = _jugadorEnganchado.GetComponent<PlayerModoMovimiento>();
            if (modo != null)
            {
                modo.EmitirRuido = NivelActual.reglas.sigilo;
            }
        }

        private void SoltarJugador()
        {
            if (_jugadorEnganchado != null && _jugadorEnganchado.Vida != null)
            {
                _jugadorEnganchado.Vida.OnMuerte -= AlMorirJugador;
            }

            _jugadorEnganchado = null;
        }

        private static void AlMorirJugador() => EventosJuego.NotificarMuerteJugador();
    }
}
