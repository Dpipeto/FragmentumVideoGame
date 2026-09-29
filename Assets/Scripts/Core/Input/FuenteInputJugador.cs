using UnityEngine;
using UnityEngine.InputSystem;

namespace Fragmentum.Core.Input
{
    /// <summary>
    /// Implementación de <see cref="IFuenteInput"/> sobre el Input System, que es el único
    /// sistema de entrada activo del proyecto.
    ///
    /// Usa el mapa "Player" del asset <c>InputSystem_Actions</c>, que ya trae las acciones
    /// que necesita el juego:
    /// Move (WASD y flechas), Jump (espacio), Attack (clic izquierdo o Enter),
    /// Sprint (Shift izquierdo, aquí el dash), Crouch (C o Ctrl izquierdo), Interact (E)
    /// y Ability (Q, añadida en la Fase 3 para la Invisibilidad Parcial).
    ///
    /// Si falta el asset o alguna acción, avisa una vez por consola y devuelve input neutro:
    /// el juego no se rompe, solo no responde a esa acción.
    ///
    /// Se ejecuta antes que el resto de scripts para que el jump buffer registre la pulsación
    /// en el mismo frame en que la FSM del jugador la consulta.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class FuenteInputJugador : MonoBehaviour, IFuenteInput
    {
        [Header("Input System")]
        [Tooltip("Asset de acciones. Por defecto, Assets/Settings/InputSystem_Actions.inputactions")]
        [SerializeField] private InputActionAsset acciones;

        [Tooltip("Mapa de acciones del jugador dentro del asset.")]
        [SerializeField] private string mapaJugador = "Player";

        private InputActionMap _mapa;
        private InputAction _mover;
        private InputAction _saltar;
        private InputAction _atacar;
        private InputAction _dash;
        private InputAction _agacharse;
        private InputAction _interactuar;
        private InputAction _habilidad;

        private float _tiempoUltimoSalto = float.NegativeInfinity;
        private bool _saltoConsumido = true;
        private bool _bloqueado;

        /// <summary>
        /// Bloqueo propio de esta fuente, o el global que fija la FSM del juego (pausa,
        /// cinemáticas, cargas, Game Over). También cuenta como bloqueado el fotograma en que se
        /// sale de un menú, para que el clic de «Continuar» no sea además un ataque.
        /// </summary>
        public bool Bloqueado
        {
            get => _bloqueado || EventosJuego.EntradaIgnorada;
            set => _bloqueado = value;
        }

        public float EjeHorizontal => Bloqueado || _mover == null ? 0f : _mover.ReadValue<Vector2>().x;

        public float EjeVertical => Bloqueado || _mover == null ? 0f : _mover.ReadValue<Vector2>().y;

        public bool SaltoPulsado => !Bloqueado && _saltar != null && _saltar.WasPressedThisFrame();

        public bool SaltoMantenido => !Bloqueado && _saltar != null && _saltar.IsPressed();

        public bool AtaquePulsado => !Bloqueado && _atacar != null && _atacar.WasPressedThisFrame();

        public bool AtaqueMantenido => !Bloqueado && _atacar != null && _atacar.IsPressed();

        public bool DashPulsado => !Bloqueado && _dash != null && _dash.WasPressedThisFrame();

        public bool AgacharseMantenido => !Bloqueado && _agacharse != null && _agacharse.IsPressed();

        public bool InteractuarPulsado => !Bloqueado && _interactuar != null && _interactuar.WasPerformedThisFrame();

        public bool HabilidadPulsado => !Bloqueado && _habilidad != null && _habilidad.WasPressedThisFrame();

        private void Awake()
        {
            if (acciones == null)
            {
                Debug.LogWarning(
                    "[Input] Falta asignar el asset de acciones en FuenteInputJugador. " +
                    "Asigna Assets/Settings/InputSystem_Actions. Hasta entonces el jugador no responde.",
                    this);
                return;
            }

            _mapa = acciones.FindActionMap(mapaJugador, false);
            if (_mapa == null)
            {
                Debug.LogWarning($"[Input] El asset no tiene el mapa '{mapaJugador}'.", this);
                return;
            }

            _mover = Buscar("Move");
            _saltar = Buscar("Jump");
            _atacar = Buscar("Attack");
            _dash = Buscar("Sprint");
            _agacharse = Buscar("Crouch");
            _interactuar = Buscar("Interact");
            _habilidad = Buscar("Ability");
        }

        private void OnEnable() => _mapa?.Enable();

        private void OnDisable() => _mapa?.Disable();

        private void Update()
        {
            if (SaltoPulsado)
            {
                _tiempoUltimoSalto = Time.time;
                _saltoConsumido = false;
            }
        }

        public bool HaySaltoEnBuffer(float ventana)
        {
            return !Bloqueado && !_saltoConsumido && Time.time - _tiempoUltimoSalto <= ventana;
        }

        public void ConsumirSalto() => _saltoConsumido = true;

        private InputAction Buscar(string nombre)
        {
            var accion = _mapa.FindAction(nombre, false);
            if (accion == null)
            {
                Debug.LogWarning(
                    $"[Input] Falta la acción '{mapaJugador}/{nombre}' en el asset de acciones. " +
                    "Esa entrada queda sin responder; añádela en el Input Actions.",
                    this);
            }

            return accion;
        }
    }
}
