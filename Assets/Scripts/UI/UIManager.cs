using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Core.Juego;
using Fragmentum.Data;
using Fragmentum.Narrative;
using Fragmentum.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Fragmentum.UI
{
    /// <summary>
    /// Interfaz persistente del juego (plan, §3.6: «qué canvas está activo» es un efecto de la
    /// FSM global). Escucha los cambios de estado de <see cref="GameStateMachine"/> y muestra:
    /// - PLAYING: el HUD (solo en los niveles del juego).
    /// - PAUSED: el HUD y el <see cref="MenuPausa"/>.
    /// - GAME_OVER: la <see cref="PantallaGameOver"/>.
    /// - LEVEL_COMPLETE: la <see cref="PantallaFinNivel"/>.
    ///
    /// También lee la pausa (acción <c>Game/Pause</c>: Esc o Start) y se asegura de que haya un
    /// EventSystem en las escenas que no lo traen, como los niveles. Sin él, ningún botón
    /// respondería. No toca <c>Time.timeScale</c> ni carga escenas: eso es de la FSM global.
    ///
    /// Vive en <c>Resources/Sistemas.prefab</c> y construye sus pantallas por código al
    /// arrancar. Si faltara, la FSM global hace avanzar sola el Game Over y el fin de nivel.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        [Tooltip("Asset de acciones. Por defecto, Assets/Settings/InputSystem_Actions.inputactions")]
        [SerializeField] private InputActionAsset acciones;

        [SerializeField] private string mapaJuego = "Game";
        [SerializeField] private string accionPausa = "Pause";

        [Tooltip("Orden del lienzo: por encima de la UI de las escenas y por debajo del fundido de carga (1000).")]
        [SerializeField] private int ordenLienzo = 100;

        [Tooltip("Textos del registro emocional del fin de nivel. Vacío = los valores por defecto.")]
        [SerializeField] private ConfiguracionRegistro registro;

        private Hud _hud;
        private MenuPausa _pausa;
        private PantallaGameOver _gameOver;
        private PantallaFinNivel _finNivel;
        private PantallaYoReconstruido _yo;
        private PantallaCinematica _cinematica;
        private GameObject _sistemaEventos;
        private InputAction _pausaAccion;
        private bool _accionPropia;
        private GameStateMachine _juego;
        private GameManager _gestor;
        private DialogueManager _dialogos;

        public static UIManager Instancia { get; private set; }

        public Hud Hud => _hud;

        public MenuPausa MenuPausa => _pausa;

        public PantallaGameOver PantallaGameOver => _gameOver;

        public PantallaFinNivel PantallaFinNivel => _finNivel;

        public PantallaYoReconstruido YoReconstruido => _yo;

        public PantallaCinematica Cinematica => _cinematica;

        private void Awake()
        {
            Instancia = this;
            Construir();
            PrepararPausa();
            CrearSistemaEventos();

            // Con estas pantallas, el Game Over, el fin de nivel y las cinemáticas esperan al jugador.
            GameStateMachine.HayPantallaGameOver = true;
            GameStateMachine.HayPantallaFinNivel = true;
            GameStateMachine.HayReproductorCinematicas = true;
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        private void Start()
        {
            _juego = GameStateMachine.Instancia;
            if (_juego != null)
            {
                _juego.OnStateChanged += AlCambiarEstado;
            }

            _gestor = GameManager.Instancia;
            if (_gestor != null)
            {
                _gestor.OnJugadorRegistrado += AlRegistrarJugador;
                _gestor.OnFragmentoObtenido += AlObtenerFragmento;
                AlRegistrarJugador(_gestor.Jugador);
            }

            _dialogos = DialogueManager.Instancia;
            if (_dialogos != null)
            {
                _dialogos.OnLineaMostrada += AlMostrarLinea;
                _dialogos.OnLineasTerminadas += _hud.OcultarLinea;
            }

            EventosJuego.CheckpointActivado += AlActivarCheckpoint;
            EventosJuego.JefeEnCombate += AlEmpezarCombateJefe;
            EventosJuego.JefeFueraDeCombate += AlTerminarCombateJefe;
            AjustarSistemaEventos();
            Mostrar(_juego != null ? _juego.EstadoActual : ESTADOS_JUEGO.BOOT);
        }

        /// <summary>Abre la pantalla «yo reconstruido» (desde el menú principal).</summary>
        public void MostrarYoReconstruido()
        {
            AjustarSistemaEventos();
            _yo.Mostrar(_gestor != null ? _gestor.NivelActual : null);
        }

        private void OnEnable() => _pausaAccion?.Enable();

        private void OnDisable() => _pausaAccion?.Disable();

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            EventosJuego.CheckpointActivado -= AlActivarCheckpoint;
            EventosJuego.JefeEnCombate -= AlEmpezarCombateJefe;
            EventosJuego.JefeFueraDeCombate -= AlTerminarCombateJefe;
            if (_juego != null)
            {
                _juego.OnStateChanged -= AlCambiarEstado;
            }

            if (_gestor != null)
            {
                _gestor.OnJugadorRegistrado -= AlRegistrarJugador;
                _gestor.OnFragmentoObtenido -= AlObtenerFragmento;
            }

            if (_dialogos != null)
            {
                _dialogos.OnLineaMostrada -= AlMostrarLinea;
                _dialogos.OnLineasTerminadas -= _hud.OcultarLinea;
            }

            if (_accionPropia)
            {
                _pausaAccion?.Dispose();
            }

            if (Instancia == this)
            {
                Instancia = null;
            }

            GameStateMachine.HayPantallaGameOver = false;
            GameStateMachine.HayPantallaFinNivel = false;
            GameStateMachine.HayReproductorCinematicas = false;
        }

        private void Update()
        {
            if (_pausaAccion != null && _pausaAccion.WasPressedThisFrame())
            {
                AlternarPausa();
            }
        }

        /// <summary>
        /// Esc o Start: pausa si se está jugando y reanuda si está en pausa. En una cinemática la
        /// salta, y con «yo reconstruido» abierto lo cierra.
        /// </summary>
        public void AlternarPausa()
        {
            if (_yo.Visible)
            {
                _yo.Ocultar();
                return;
            }

            if (_juego == null)
            {
                return;
            }

            if (_juego.EstadoActual == ESTADOS_JUEGO.CUTSCENE)
            {
                _cinematica.Saltar();
            }
            else if (_juego.EstadoActual == ESTADOS_JUEGO.PLAYING)
            {
                _juego.Pausar();
            }
            else if (_juego.EstadoActual == ESTADOS_JUEGO.PAUSED)
            {
                _juego.Reanudar();
            }
        }

        // --- Pantallas ---

        private void Construir()
        {
            var lienzo = FabricaUI.Lienzo(transform, "Interfaz", ordenLienzo);

            _hud = FabricaUI.Objeto("HUD", lienzo.transform).gameObject.AddComponent<Hud>();
            _hud.Construir();

            _pausa = Crear<MenuPausa>(lienzo.transform, "MenuPausa");
            _gameOver = Crear<PantallaGameOver>(lienzo.transform, "GameOver");
            _finNivel = FabricaUI.Objeto("FinNivel", lienzo.transform).gameObject.AddComponent<PantallaFinNivel>();
            _finNivel.Configuracion = registro;
            _finNivel.Construir();
            _yo = Crear<PantallaYoReconstruido>(lienzo.transform, "YoReconstruido");

            // La cinemática va encima de todo lo demás.
            _cinematica = FabricaUI.Objeto("Cinematica", lienzo.transform).gameObject.AddComponent<PantallaCinematica>();
            _cinematica.Construir();
        }

        private static T Crear<T>(Transform padre, string nombre) where T : PantallaSuperpuesta
        {
            var pantalla = FabricaUI.Objeto(nombre, padre).gameObject.AddComponent<T>();
            pantalla.Construir();
            return pantalla;
        }

        private void AlCambiarEstado(ESTADOS_JUEGO anterior, ESTADOS_JUEGO nuevo) => Mostrar(nuevo);

        private void Mostrar(ESTADOS_JUEGO estado)
        {
            var nivel = _gestor != null ? _gestor.NivelActual : null;
            if (estado != ESTADOS_JUEGO.LOADING)
            {
                AjustarSistemaEventos();
            }

            // El HUD es de los niveles del juego; las escenas de prueba traen el suyo.
            var conHud = nivel != null && (estado == ESTADOS_JUEGO.PLAYING || estado == ESTADOS_JUEGO.PAUSED);
            if (conHud)
            {
                _hud.MostrarNivel(nivel);
            }

            _hud.gameObject.SetActive(conHud);
            Alternar(_pausa, estado == ESTADOS_JUEGO.PAUSED, nivel);
            Alternar(_gameOver, estado == ESTADOS_JUEGO.GAME_OVER, nivel);
            Alternar(_finNivel, estado == ESTADOS_JUEGO.LEVEL_COMPLETE, nivel);
            _yo.Ocultar();

            if (estado == ESTADOS_JUEGO.CUTSCENE)
            {
                if (!_cinematica.Reproduciendo)
                {
                    _cinematica.Reproducir(_juego != null ? _juego.CinematicaActual : null);
                }
            }
            else if (_cinematica.Visible)
            {
                _cinematica.Ocultar();
            }
        }

        private void AlMostrarLinea(LineaDialogo linea) =>
            _hud.MostrarLinea(linea.hablante, linea.texto, _gestor != null && _gestor.NivelActual != null ? _gestor.NivelActual.ColorAcento : PantallaSuperpuesta.AcentoNeutro);

        private static void Alternar(PantallaSuperpuesta pantalla, bool visible, LevelData nivel)
        {
            if (visible && !pantalla.Visible)
            {
                pantalla.Mostrar(nivel);
            }
            else if (!visible)
            {
                pantalla.Ocultar();
            }
        }

        private void AlRegistrarJugador(PlayerController jugador) => _hud.Vincular(jugador);

        private void AlActivarCheckpoint(string id) => _hud.Avisar("Punto de control");

        private void AlEmpezarCombateJefe(string nombre, Health vida) => _hud.MostrarJefe(nombre, vida);

        private void AlTerminarCombateJefe() => _hud.OcultarJefe();

        // Al derrotar al jefe el fragmento llega en el acto: se anuncia antes de salir del nivel.
        private void AlObtenerFragmento(FragmentoData fragmento)
        {
            if (fragmento != null)
            {
                _hud.Avisar($"{fragmento.nombre} integrado");
            }
        }

        // --- Pausa ---

        private void PrepararPausa()
        {
            var mapa = acciones != null ? acciones.FindActionMap(mapaJuego, false) : null;
            _pausaAccion = mapa != null ? mapa.FindAction(accionPausa, false) : null;
            if (_pausaAccion != null)
            {
                return;
            }

            if (acciones != null)
            {
                Debug.LogWarning($"[UI] El asset de acciones no tiene '{mapaJuego}/{accionPausa}': la pausa usa Esc y Start fijos.", this);
            }

            // Sin asset (sistemas creados sin el prefab) la pausa sigue funcionando.
            _pausaAccion = new InputAction("Pausa", InputActionType.Button);
            _pausaAccion.AddBinding("<Keyboard>/escape");
            _pausaAccion.AddBinding("<Gamepad>/start");
            _accionPropia = true;
        }

        // --- EventSystem ---

        private void CrearSistemaEventos()
        {
            _sistemaEventos = new GameObject("EventSystem");
            _sistemaEventos.SetActive(false);
            _sistemaEventos.transform.SetParent(transform, false);
            _sistemaEventos.AddComponent<EventSystem>();
            _sistemaEventos.AddComponent<InputSystemUIInputModule>();
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            _hud.OcultarJefe();
            AjustarSistemaEventos();
        }

        // El propio solo se usa si la escena no trae uno: dos EventSystem a la vez dan avisos.
        private void AjustarSistemaEventos()
        {
            if (_sistemaEventos == null)
            {
                return;
            }

            _sistemaEventos.SetActive(false);
            var otro = FindAnyObjectByType<EventSystem>();
            _sistemaEventos.SetActive(otro == null);
        }
    }
}
