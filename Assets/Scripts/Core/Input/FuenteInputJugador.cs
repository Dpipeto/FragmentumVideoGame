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
    /// Sprint (Shift izquierdo, aquí el dash), Crouch (C) e Interact (E).
    ///
    /// Si falta el asset o alguna acción, avisa una vez por consola y devuelve input neutro:
    /// el juego no se rompe, solo no responde a esa acción.
    /// </summary>
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

        private float _tiempoUltimoSalto = float.NegativeInfinity;
        private bool _saltoConsumido = true;

        public bool Bloqueado { get; set; }

        public float EjeHorizontal => Bloqueado || _mover == null ? 0f : _mover.ReadValue<Vector2>().x;

        public float EjeVertical => Bloqueado || _mover == null ? 0f : _mover.ReadValue<Vector2>().y;

        public bool SaltoPulsado => !Bloqueado && _saltar != null && _saltar.WasPressedThisFrame();

        public bool SaltoMantenido => !Bloqueado && _saltar != null && _saltar.IsPressed();

        public bool AtaquePulsado => !Bloqueado && _atacar != null && _atacar.WasPressedThisFrame();

        public bool AtaqueMantenido => !Bloqueado && _atacar != null && _atacar.IsPressed();

        public bool DashPulsado => !Bloqueado && _dash != null && _dash.WasPressedThisFrame();

        public bool AgacharseMantenido => !Bloqueado && _agacharse != null && _agacharse.IsPressed();

        public bool InteractuarPulsado => !Bloqueado && _interactuar != null && _interactuar.WasPerformedThisFrame();

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
