using System;
using Fragmentum.Core.FSM;
using Fragmentum.Core.Juego;
using Fragmentum.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fragmentum.Core
{
    /// <summary>
    /// FSM global del juego (tarea E): BOOT → MAIN_MENU → LEVEL_SELECT → LOADING → PLAYING →
    /// (PAUSED | CUTSCENE | GAME_OVER | LEVEL_COMPLETE).
    ///
    /// Es la única dueña de <c>Time.timeScale</c> y del bloqueo del input, y la única que pide
    /// cargar escenas. El menú, el selector y los niveles no cambian de escena por su cuenta:
    /// llaman a la API de esta clase, y cada llamada pasa por el <c>TransitionTo</c> del núcleo
    /// y su tabla, igual que en el jugador y los enemigos.
    ///
    /// Los estados avisan de sus cambios con <see cref="OnStateChanged"/>; la UI (Fase 6)
    /// decidirá con ese evento qué lienzo mostrar.
    /// </summary>
    [DeclaraFSM("Juego")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SceneLoader))]
    public class GameStateMachine : MonoBehaviour
    {
        public const string GrupoEnNivel = "EN_NIVEL";

        [Tooltip("Sin pantalla de Game Over o de fin de nivel (llegan en la Fase 6), segundos antes de continuar solos.")]
        [SerializeField] private float esperaSinPantalla = 1.5f;

        [Header("Depuración")]
        [SerializeField] private ESTADOS_JUEGO currentState;

        private MaquinaEstados<ESTADOS_JUEGO> _maquina;
        private TablaTransiciones<ESTADOS_JUEGO> _tabla;
        private SceneLoader _cargador;
        private string _escenaDestino;

        public static GameStateMachine Instancia { get; private set; }

        /// <summary>Lo activará la pantalla de Game Over al existir (Fase 6).</summary>
        public static bool HayPantallaGameOver { get; set; }

        /// <summary>Lo activará la pantalla de fin de nivel al existir (Fase 6).</summary>
        public static bool HayPantallaFinNivel { get; set; }

        public event Action<ESTADOS_JUEGO, ESTADOS_JUEGO> OnStateChanged;

        public ESTADOS_JUEGO EstadoActual => _maquina != null && _maquina.Iniciada ? _maquina.EstadoActual : ESTADOS_JUEGO.BOOT;

        public float EsperaSinPantalla => esperaSinPantalla;

        internal bool CargaTerminada { get; private set; }

        internal ESTADOS_JUEGO DestinoDeCarga { get; private set; }

        /// <summary>Nivel que se está cargando o que se continuará tras LEVEL_COMPLETE.</summary>
        public LevelData NivelDestino { get; private set; }

        private static LevelDatabase BaseDeDatos => GameManager.Instancia != null ? GameManager.Instancia.BaseDeDatos : null;

        // --- Especificación: tabla de transiciones y efectos de cada estado ---

        public static TablaTransiciones<ESTADOS_JUEGO> CrearTabla()
        {
            const ESTADOS_JUEGO BOOT = ESTADOS_JUEGO.BOOT;
            const ESTADOS_JUEGO MAIN_MENU = ESTADOS_JUEGO.MAIN_MENU;
            const ESTADOS_JUEGO LEVEL_SELECT = ESTADOS_JUEGO.LEVEL_SELECT;
            const ESTADOS_JUEGO LOADING = ESTADOS_JUEGO.LOADING;
            const ESTADOS_JUEGO PLAYING = ESTADOS_JUEGO.PLAYING;
            const ESTADOS_JUEGO PAUSED = ESTADOS_JUEGO.PAUSED;
            const ESTADOS_JUEGO CUTSCENE = ESTADOS_JUEGO.CUTSCENE;
            const ESTADOS_JUEGO GAME_OVER = ESTADOS_JUEGO.GAME_OVER;
            const ESTADOS_JUEGO LEVEL_COMPLETE = ESTADOS_JUEGO.LEVEL_COMPLETE;

            return new TablaTransiciones<ESTADOS_JUEGO>("Juego")
                .Inicial(BOOT)
                .DefinirGrupo(GrupoEnNivel, PLAYING, PAUSED, CUTSCENE, GAME_OVER, LEVEL_COMPLETE)
                .Permitir(BOOT, MAIN_MENU, "arranca en la escena del menú")
                .Permitir(BOOT, LEVEL_SELECT, "arranca en el selector (Editor)")
                .Permitir(BOOT, PLAYING, "arranca en un nivel o escena de prueba (Editor)")
                .Permitir(MAIN_MENU, LOADING, "Continuar, Nueva partida o Seleccionar nivel")
                .Permitir(LEVEL_SELECT, LOADING, "nivel desbloqueado o Volver")
                .Permitir(LOADING, MAIN_MENU, "escena cargada: menú")
                .Permitir(LOADING, LEVEL_SELECT, "escena cargada: selector")
                .Permitir(LOADING, PLAYING, "escena cargada: nivel")
                .Permitir(LOADING, CUTSCENE, "nivel con cinemática de entrada pendiente")
                .Permitir(PLAYING, PAUSED, "pausa")
                .Permitir(PAUSED, PLAYING, "reanudar")
                .Permitir(PAUSED, LOADING, "reiniciar el nivel o volver al menú")
                .Permitir(PLAYING, CUTSCENE, "memoria, jefe o cierre del mundo")
                .Permitir(CUTSCENE, PLAYING, "termina una cinemática de entrada o de memoria")
                .Permitir(CUTSCENE, LEVEL_COMPLETE, "termina la cinemática de cierre")
                .Permitir(PLAYING, GAME_OVER, "el jugador muere en un nivel")
                .Permitir(GAME_OVER, PLAYING, "reintentar desde el checkpoint")
                .Permitir(GAME_OVER, LOADING, "reiniciar el nivel o volver al menú")
                .Permitir(PLAYING, LEVEL_COMPLETE, "victoria sin cinemática de cierre")
                .Permitir(LEVEL_COMPLETE, LOADING, "continuar: siguiente nivel, selector o menú");
        }

        /// <summary>Efectos de cada estado (plan, §3.6): escala de tiempo y bloqueo del input.</summary>
        public static (float escalaTiempo, bool bloqueaInput) Efectos(ESTADOS_JUEGO estado)
        {
            switch (estado)
            {
                case ESTADOS_JUEGO.PLAYING:
                    return (1f, false);
                case ESTADOS_JUEGO.PAUSED:
                case ESTADOS_JUEGO.CUTSCENE:
                case ESTADOS_JUEGO.GAME_OVER:
                case ESTADOS_JUEGO.LEVEL_COMPLETE:
                    return (0f, true);
                default: // BOOT, MAIN_MENU, LEVEL_SELECT, LOADING
                    return (1f, true);
            }
        }

        // --- Ciclo de vida ---

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(this);
                return;
            }

            Instancia = this;
            _cargador = GetComponent<SceneLoader>();
            _tabla = CrearTabla();

            // Reloj sin escala: los estados con el tiempo parado también cuentan sus esperas.
            _maquina = new MaquinaEstados<ESTADOS_JUEGO>(_tabla, () => Time.unscaledTime);
            foreach (ESTADOS_JUEGO id in Enum.GetValues(typeof(ESTADOS_JUEGO)))
            {
                _maquina.Registrar(CrearEstado(id));
            }

            _maquina.OnStateChanged += (anterior, nuevo) =>
            {
                currentState = nuevo;
                OnStateChanged?.Invoke(anterior, nuevo);
            };
        }

        private void OnEnable() => EventosJuego.JugadorMurio += AlMorirJugador;

        private void OnDisable() => EventosJuego.JugadorMurio -= AlMorirJugador;

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
                Time.timeScale = 1f;
                EventosJuego.FijarEntradaBloqueada(false);
            }
        }

        private void Start() => _maquina.Iniciar(ESTADOS_JUEGO.BOOT);

        private void Update() => _maquina.Actualizar();

        private IEstado<ESTADOS_JUEGO> CrearEstado(ESTADOS_JUEGO id)
        {
            switch (id)
            {
                case ESTADOS_JUEGO.BOOT:
                    return new EstadoArranque(this, _maquina);
                case ESTADOS_JUEGO.LOADING:
                    return new EstadoCargando(this, _maquina);
                case ESTADOS_JUEGO.PLAYING:
                    return new EstadoJugando(this, _maquina);
                case ESTADOS_JUEGO.GAME_OVER:
                    return new EstadoEsperaPantalla(id, ESTADOS_JUEGO.PLAYING, () => HayPantallaGameOver, this, _maquina);
                case ESTADOS_JUEGO.LEVEL_COMPLETE:
                    return new EstadoEsperaPantalla(id, ESTADOS_JUEGO.LOADING, () => HayPantallaFinNivel, this, _maquina);
                default:
                    return new EstadoJuego(id, this, _maquina);
            }
        }

        // --- API: lo que pueden pedir el menú, el selector, la UI y los niveles ---

        public bool IrAlMenu()
        {
            GuardarProgreso();
            var bd = BaseDeDatos;
            return bd != null && CargarEscena(bd.escenaMenu.Nombre, ESTADOS_JUEGO.MAIN_MENU, null);
        }

        public bool IrAlSelector()
        {
            var bd = BaseDeDatos;
            return bd != null && CargarEscena(bd.escenaSelector.Nombre, ESTADOS_JUEGO.LEVEL_SELECT, null);
        }

        /// <summary>Carga un nivel. Si no tiene escena jugable, avisa y no hace nada.</summary>
        public bool JugarNivel(LevelData nivel)
        {
            if (nivel == null || !nivel.Jugable)
            {
                Debug.LogWarning($"[Juego] '{(nivel != null ? nivel.Titulo : "(ninguno)")}' todavía no tiene escena jugable (en construcción).");
                return false;
            }

            if (GameManager.Instancia != null && !GameManager.Instancia.EstaDesbloqueado(nivel))
            {
                Debug.LogWarning($"[Juego] '{nivel.Titulo}' está bloqueado: completa antes el mundo anterior.");
                return false;
            }

            return CargarEscena(nivel.escena.Nombre, ESTADOS_JUEGO.PLAYING, nivel);
        }

        /// <summary>«Continuar» del menú: el nivel desbloqueado más avanzado con escena, o el selector.</summary>
        public bool ContinuarPartida()
        {
            var bd = BaseDeDatos;
            var nivel = bd != null && GameManager.Instancia != null ? bd.ParaContinuar(GameManager.Instancia.Datos) : null;
            return nivel != null ? JugarNivel(nivel) : IrAlSelector();
        }

        /// <summary>Reinicia el nivel actual desde el principio (pausa o Game Over).</summary>
        public bool ReiniciarNivel()
        {
            var nivel = GameManager.Instancia != null ? GameManager.Instancia.NivelActual : null;
            return CargarEscena(SceneManager.GetActiveScene().name, ESTADOS_JUEGO.PLAYING, nivel);
        }

        public bool Pausar() => Transitar(ESTADOS_JUEGO.PAUSED);

        public bool Reanudar() => Transitar(ESTADOS_JUEGO.PLAYING, desde: ESTADOS_JUEGO.PAUSED);

        public bool AlternarPausa() => EstadoActual == ESTADOS_JUEGO.PAUSED ? Reanudar() : Pausar();

        /// <summary>Tras el Game Over: vuelve a jugar desde el checkpoint.</summary>
        public bool Reintentar() => Transitar(ESTADOS_JUEGO.PLAYING, desde: ESTADOS_JUEGO.GAME_OVER);

        /// <summary>Victoria: registra el nivel como completado y pasa a LEVEL_COMPLETE.</summary>
        public bool CompletarNivel()
        {
            if (!Puede(ESTADOS_JUEGO.LEVEL_COMPLETE))
            {
                Debug.LogWarning($"[Juego] No se puede completar el nivel desde {EstadoActual}.");
                return false;
            }

            if (GameManager.Instancia != null)
            {
                GameManager.Instancia.CompletarNivel(GameManager.Instancia.NivelActual);
            }

            return _maquina.TransitionTo(ESTADOS_JUEGO.LEVEL_COMPLETE);
        }

        /// <summary>Tras LEVEL_COMPLETE: siguiente nivel si tiene escena; si no, el selector.</summary>
        public bool ContinuarTrasNivel()
        {
            if (!Puede(ESTADOS_JUEGO.LOADING) || EstadoActual != ESTADOS_JUEGO.LEVEL_COMPLETE)
            {
                return false;
            }

            PrepararContinuacion();
            return _maquina.TransitionTo(ESTADOS_JUEGO.LOADING);
        }

        public bool IniciarCinematica() => Transitar(ESTADOS_JUEGO.CUTSCENE);

        /// <summary>Fin de una cinemática: de cierre → LEVEL_COMPLETE; de entrada o memoria → PLAYING.</summary>
        public bool TerminarCinematica(bool eraDeCierre)
        {
            if (EstadoActual != ESTADOS_JUEGO.CUTSCENE)
            {
                return false;
            }

            if (eraDeCierre && GameManager.Instancia != null)
            {
                GameManager.Instancia.CompletarNivel(GameManager.Instancia.NivelActual);
            }

            return _maquina.TransitionTo(eraDeCierre ? ESTADOS_JUEGO.LEVEL_COMPLETE : ESTADOS_JUEGO.PLAYING);
        }

        public bool Puede(ESTADOS_JUEGO destino) => _maquina.Iniciada && _tabla.EsValida(_maquina.EstadoActual, destino);

        // --- Internos que usan los estados ---

        internal ESTADOS_JUEGO DestinoDeArranque()
        {
            var escena = SceneManager.GetActiveScene().name;
            var bd = BaseDeDatos;
            if (bd != null && escena == bd.escenaMenu.Nombre) return ESTADOS_JUEGO.MAIN_MENU;
            if (bd != null && escena == bd.escenaSelector.Nombre) return ESTADOS_JUEGO.LEVEL_SELECT;
            return ESTADOS_JUEGO.PLAYING;
        }

        internal void IniciarCarga()
        {
            CargaTerminada = false;
            if (!_cargador.Cargar(_escenaDestino, () => CargaTerminada = true))
            {
                // No debería pasar (se valida antes), pero nunca se deja al juego en LOADING.
                Debug.LogError("[Juego] La carga no pudo empezar; se vuelve al menú si existe.");
                DestinoDeCarga = ESTADOS_JUEGO.MAIN_MENU;
                CargaTerminada = true;
            }
        }

        /// <summary>Elige a dónde ir tras completar el nivel: el siguiente si tiene escena, si no el selector.</summary>
        internal void PrepararContinuacion()
        {
            var bd = BaseDeDatos;
            var actual = GameManager.Instancia != null ? GameManager.Instancia.NivelActual : null;
            var siguiente = bd != null && actual != null ? bd.Siguiente(actual) : null;

            if (siguiente != null && siguiente.Jugable)
            {
                PrepararCarga(siguiente.escena.Nombre, ESTADOS_JUEGO.PLAYING, siguiente);
            }
            else if (bd != null && bd.escenaSelector.EnBuild)
            {
                PrepararCarga(bd.escenaSelector.Nombre, ESTADOS_JUEGO.LEVEL_SELECT, null);
            }
            else if (bd != null)
            {
                PrepararCarga(bd.escenaMenu.Nombre, ESTADOS_JUEGO.MAIN_MENU, null);
            }
        }

        private bool CargarEscena(string escena, ESTADOS_JUEGO destino, LevelData nivel)
        {
            if (!Puede(ESTADOS_JUEGO.LOADING))
            {
                Debug.LogWarning($"[Juego] No se puede cambiar de escena desde {EstadoActual}.");
                return false;
            }

            if (!SceneLoader.PuedeCargar(escena))
            {
                Debug.LogError($"[Juego] La escena '{escena}' no se puede cargar: no está en Build Settings.");
                return false;
            }

            PrepararCarga(escena, destino, nivel);
            return _maquina.TransitionTo(ESTADOS_JUEGO.LOADING);
        }

        private void PrepararCarga(string escena, ESTADOS_JUEGO destino, LevelData nivel)
        {
            _escenaDestino = escena;
            DestinoDeCarga = destino;
            NivelDestino = nivel;
            CargaTerminada = false;
        }

        private bool Transitar(ESTADOS_JUEGO destino, ESTADOS_JUEGO? desde = null)
        {
            if ((desde.HasValue && EstadoActual != desde.Value) || !Puede(destino))
            {
                Debug.LogWarning($"[Juego] Transición no disponible ahora: {EstadoActual} -> {destino}.");
                return false;
            }

            return _maquina.TransitionTo(destino);
        }

        private void AlMorirJugador()
        {
            // Solo los niveles del juego tienen Game Over; en las escenas de prueba el jugador
            // reaparece con su propio mecanismo.
            if (EstadoActual == ESTADOS_JUEGO.PLAYING && GameManager.Instancia != null && GameManager.Instancia.NivelActual != null)
            {
                _maquina.TransitionTo(ESTADOS_JUEGO.GAME_OVER);
            }
        }

        private static void GuardarProgreso()
        {
            if (GameManager.Instancia != null)
            {
                GameManager.Instancia.Guardar();
            }
        }
    }
}
