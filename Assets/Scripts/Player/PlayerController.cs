using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Core.FSM;
using Fragmentum.Core.Input;
using Fragmentum.Player.States;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fragmentum.Player
{
    /// <summary>
    /// Controlador del jugador: dueño de la FSM principal (patrón State). Sustituye al
    /// <c>PlayerMovement</c> del prototipo, cuyo <c>Update()</c> monolítico se gobernaba con
    /// banderas booleanas.
    ///
    /// Reparto de responsabilidades:
    /// - Este componente lee el input (Update), mantiene los temporizadores (coyote time,
    ///   cooldown del dash, doble salto) y convierte los eventos de vida en transiciones.
    /// - Cada estado (carpeta <c>States/</c>) decide qué hacer y a dónde quiere ir.
    /// - <see cref="PlayerMotor"/> aplica la física en FixedUpdate.
    /// - <see cref="PlayerHealth"/> lleva la vida y avisa por eventos.
    ///
    /// La tabla de transiciones (<see cref="CrearTabla"/>) es la especificación de la FSM: la
    /// máquina rechaza cualquier cambio que no esté en ella y el diagrama de la tesis se
    /// genera a partir de ella (menú Fragmentum → Exportar diagramas FSM).
    /// </summary>
    [DeclaraFSM("Jugador")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor), typeof(PlayerSensors), typeof(PlayerHealth))]
    public class PlayerController : MonoBehaviour
    {
        /// <summary>Superestado con los 8 estados de locomoción; comparten las salidas a DASH y ATTACK.</summary>
        public const string GrupoLocomocion = "LOCOMOCION";

        /// <summary>Todos los estados salvo DEAD.</summary>
        public const string GrupoVivo = "VIVO";

        [SerializeField] private ParametrosJugador parametros = new ParametrosJugador();

        [Header("Componentes (vacío = se buscan en el mismo objeto)")]
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerSensors sensores;
        [SerializeField] private PlayerHealth vida;
        [SerializeField] private PlayerAbilities habilidades;

        [Tooltip("Animator del sprite. Puede no tener controlador todavía: los parámetros que falten se avisan una sola vez.")]
        [SerializeField] private Animator animator;

        [Tooltip("Hitbox de la espada: hijo en la capa PlayerWeapon, desactivado por defecto.")]
        [SerializeField] private Hitbox hitboxAtaque;

        [Tooltip("Componente que implementa IFuenteInput. Vacío = el primero que haya en el mismo objeto.")]
        [SerializeField] private MonoBehaviour fuenteInput;

        [Header("Reglas del nivel (desde la Fase 5 las fija LevelData)")]
        [Tooltip("Agacharse solo tiene sentido en los niveles con cobertura (Culpa).")]
        [SerializeField] private bool permitirAgacharse = true;

        [Header("Depuración")]
        [Tooltip("Estado actual de la FSM. Solo informativo: lo escribe la máquina.")]
        [SerializeField] private ESTADOS_JUGADOR currentState;

        [Tooltip("Escribe cada transición en la consola.")]
        [SerializeField] private bool registrarTransiciones;

        private MaquinaEstados<ESTADOS_JUGADOR> _maquina;
        private IFuenteInput _input;
        private int _danioBaseEspada = 1;
        private float _finCoyote = float.NegativeInfinity;
        private float _finCoyotePared = float.NegativeInfinity;
        private float _proximoDash = float.NegativeInfinity;

        /// <summary>Cambio de estado: (anterior, nuevo). Para audio, VFX y UI.</summary>
        public event Action<ESTADOS_JUGADOR, ESTADOS_JUGADOR> OnStateChanged;

        // --- API pública ---

        public ESTADOS_JUGADOR EstadoActual => _maquina != null && _maquina.Iniciada ? _maquina.EstadoActual : ESTADOS_JUGADOR.IDLE;

        public ESTADOS_JUGADOR EstadoAnterior => _maquina != null && _maquina.Iniciada ? _maquina.EstadoAnterior : ESTADOS_JUGADOR.IDLE;

        public ParametrosJugador Parametros => parametros;

        public PlayerMotor Motor => motor;

        public PlayerSensors Sensores => sensores;

        public PlayerHealth Vida => vida;

        public PlayerAbilities Habilidades => habilidades;

        public bool EstaVivo => vida != null && vida.EstaVivo;

        /// <summary>Hacia dónde mira: 1 derecha, -1 izquierda.</summary>
        public int Mirando { get; private set; } = 1;

        /// <summary>Fuente de input activa. Nunca es nula: sin fuente real se usa <see cref="InputNeutro"/>.</summary>
        public IFuenteInput FuenteInput => _input ?? InputNeutro.Instancia;

        public bool PermitirAgacharse
        {
            get => permitirAgacharse;
            set => permitirAgacharse = value;
        }

        // --- Contexto que consultan los estados ---

        internal ParametrosJugador P => parametros;

        internal Animator Animador => animator;

        internal Hitbox Espada => hitboxAtaque;

        internal int DanioBaseEspada => _danioBaseEspada;

        internal float Ahora => Time.time;

        /// <summary>Eje horizontal del último Update, con la zona muerta aplicada.</summary>
        internal float EjeHorizontal { get; private set; }

        /// <summary>Dirección pedida: -1, 0 o 1.</summary>
        internal int DireccionEntrada { get; private set; }

        internal bool DobleSaltoDisponible { get; set; }

        internal bool DashAereoDisponible { get; set; }

        /// <summary>Lado de la pared del último wall slide (-1 izquierda, 1 derecha).</summary>
        internal int LadoPared { get; set; }

        internal Golpe UltimoGolpe { get; private set; }

        internal bool EnCoyote => Ahora <= _finCoyote;

        internal bool EnCoyotePared => Ahora <= _finCoyotePared;

        // --- Tabla de transiciones: la especificación de la FSM ---

        /// <summary>
        /// Función de transición del jugador. Es la fuente del diagrama de estados de la tesis.
        /// </summary>
        public static TablaTransiciones<ESTADOS_JUGADOR> CrearTabla()
        {
            const ESTADOS_JUGADOR IDLE = ESTADOS_JUGADOR.IDLE;
            const ESTADOS_JUGADOR RUN = ESTADOS_JUGADOR.RUN;
            const ESTADOS_JUGADOR CROUCH = ESTADOS_JUGADOR.CROUCH;
            const ESTADOS_JUGADOR JUMP = ESTADOS_JUGADOR.JUMP;
            const ESTADOS_JUGADOR DOUBLE_JUMP = ESTADOS_JUGADOR.DOUBLE_JUMP;
            const ESTADOS_JUGADOR FALL = ESTADOS_JUGADOR.FALL;
            const ESTADOS_JUGADOR WALL_SLIDE = ESTADOS_JUGADOR.WALL_SLIDE;
            const ESTADOS_JUGADOR WALL_JUMP = ESTADOS_JUGADOR.WALL_JUMP;
            const ESTADOS_JUGADOR DASH = ESTADOS_JUGADOR.DASH;
            const ESTADOS_JUGADOR ATTACK = ESTADOS_JUGADOR.ATTACK;
            const ESTADOS_JUGADOR DAMAGED = ESTADOS_JUGADOR.DAMAGED;
            const ESTADOS_JUGADOR DEAD = ESTADOS_JUGADOR.DEAD;

            return new TablaTransiciones<ESTADOS_JUGADOR>("Jugador")
                .Inicial(IDLE)
                .DefinirGrupo(GrupoVivo, IDLE, RUN, CROUCH, JUMP, DOUBLE_JUMP, FALL, WALL_SLIDE, WALL_JUMP, DASH, ATTACK, DAMAGED)
                .DefinirGrupo(GrupoLocomocion, IDLE, RUN, CROUCH, JUMP, DOUBLE_JUMP, FALL, WALL_SLIDE, WALL_JUMP)

                // (a) Locomoción
                .Permitir(IDLE, RUN, "input horizontal")
                .Permitir(RUN, IDLE, "sin input")
                .Permitir(IDLE, CROUCH, "agacharse y el nivel lo permite")
                .Permitir(RUN, CROUCH, "agacharse y el nivel lo permite")
                .Permitir(CROUCH, IDLE, "suelta y hay espacio arriba")
                .Permitir(CROUCH, FALL, "pierde el suelo")
                .Permitir(IDLE, JUMP, "salto en buffer")
                .Permitir(RUN, JUMP, "salto en buffer")
                .Permitir(IDLE, FALL, "pierde el suelo, arranca el coyote time")
                .Permitir(RUN, FALL, "pierde el suelo, arranca el coyote time")
                .Permitir(FALL, JUMP, "salto dentro del coyote time")
                .Permitir(JUMP, FALL, "vy <= 0")
                .Permitir(JUMP, DOUBLE_JUMP, "salto y queda doble salto")
                .Permitir(FALL, DOUBLE_JUMP, "salto y queda doble salto")
                .Permitir(DOUBLE_JUMP, FALL, "vy <= 0")
                .Permitir(FALL, IDLE, "aterriza sin input")
                .Permitir(FALL, RUN, "aterriza con input")
                .Permitir(FALL, WALL_SLIDE, "toca pared y empuja hacia ella")
                .Permitir(WALL_SLIDE, WALL_JUMP, "salto (prioridad sobre el doble salto)")
                .Permitir(WALL_SLIDE, FALL, "deja la pared o de empujar")
                .Permitir(WALL_SLIDE, IDLE, "aterriza")
                .Permitir(FALL, WALL_JUMP, "salto dentro del coyote de pared")
                .Permitir(WALL_JUMP, FALL, "termina wallJumpDuration")

                // (b) Acciones: el superestado las declara una sola vez para sus 8 subestados
                .PermitirDesdeGrupo(GrupoLocomocion, DASH, "dash y cooldown listo")
                .PermitirDesdeGrupo(GrupoLocomocion, ATTACK, "ataque, o carga completa soltada")
                .Permitir(DASH, IDLE, "termina dashDuration en el suelo")
                .Permitir(DASH, FALL, "termina dashDuration en el aire")
                .Permitir(ATTACK, IDLE, "termina el ataque en el suelo")
                .Permitir(ATTACK, FALL, "termina el ataque en el aire")

                // Daño y muerte
                .PermitirDesdeGrupo(GrupoLocomocion, DAMAGED, "golpe sin i-frames ni escudo")
                .Permitir(DASH, DAMAGED, "golpe sin i-frames ni escudo")
                .Permitir(ATTACK, DAMAGED, "golpe, interrumpe el ataque")
                .Permitir(DAMAGED, IDLE, "termina damagedDuration en el suelo")
                .Permitir(DAMAGED, FALL, "termina damagedDuration en el aire")
                .PermitirDesdeGrupo(GrupoVivo, DEAD, "vida 0 o muerte instantánea")
                .Permitir(DEAD, IDLE, "Revivir() en el checkpoint");
        }

        // --- Ciclo de vida ---

        private void Awake()
        {
            ResolverComponentes();

            _maquina = new MaquinaEstados<ESTADOS_JUGADOR>(CrearTabla());
            _maquina.Registrar(new PlayerIdleState(this, _maquina));
            _maquina.Registrar(new PlayerRunState(this, _maquina));
            _maquina.Registrar(new PlayerCrouchState(this, _maquina));
            _maquina.Registrar(new PlayerJumpState(this, _maquina));
            _maquina.Registrar(new PlayerDoubleJumpState(this, _maquina));
            _maquina.Registrar(new PlayerFallState(this, _maquina));
            _maquina.Registrar(new PlayerWallSlideState(this, _maquina));
            _maquina.Registrar(new PlayerWallJumpState(this, _maquina));
            _maquina.Registrar(new PlayerDashState(this, _maquina));
            _maquina.Registrar(new PlayerAttackState(this, _maquina));
            _maquina.Registrar(new PlayerDamagedState(this, _maquina));
            _maquina.Registrar(new PlayerDeadState(this, _maquina));
            _maquina.OnStateChanged += AlCambiarEstado;
        }

        private void OnEnable()
        {
            if (vida != null)
            {
                vida.OnGolpeRecibido += AlRecibirGolpe;
                vida.OnMuerte += AlMorir;
            }
        }

        private void OnDisable()
        {
            if (vida != null)
            {
                vida.OnGolpeRecibido -= AlRecibirGolpe;
                vida.OnMuerte -= AlMorir;
            }
        }

        private void Start()
        {
            motor.Configurar(parametros);
            RecargarSaltos();

            if (hitboxAtaque != null)
            {
                _danioBaseEspada = hitboxAtaque.Danio;
                hitboxAtaque.gameObject.SetActive(false);
            }

            _maquina.Iniciar(ESTADOS_JUGADOR.IDLE);
        }

        private void Update()
        {
            if (!_maquina.Iniciada)
            {
                return;
            }

            // 1. Input: se lee una vez por frame y los estados consultan la copia.
            var entrada = FuenteInput;
            var eje = entrada.EjeHorizontal;
            EjeHorizontal = Mathf.Abs(eje) < parametros.zonaMuerta ? 0f : Mathf.Clamp(eje, -1f, 1f);
            DireccionEntrada = EjeHorizontal > 0f ? 1 : EjeHorizontal < 0f ? -1 : 0;

            if (habilidades != null && EstaVivo)
            {
                habilidades.ActualizarCarga(entrada.AtaquePulsado, entrada.AtaqueMantenido);
                if (entrada.HabilidadPulsado)
                {
                    habilidades.ActivarInvisibilidad();
                }
            }

            // 2. Sentidos: suelo y paredes, antes de decidir.
            sensores.Actualizar();

            // 3. Decisión: el estado actual actúa y pide transiciones; la máquina las aplica.
            _maquina.Actualizar();
            currentState = _maquina.EstadoActual;
        }

        private void FixedUpdate()
        {
            if (!_maquina.Iniciada)
            {
                return;
            }

            _maquina.ActualizarFisica();
            motor.PasoFisica();
        }

        // --- Acciones públicas ---

        /// <summary>
        /// Sustituye la fuente de input (patrón Adapter). La usan las pruebas automáticas con
        /// un input simulado y, más adelante, las cinemáticas o el eco del mundo de la Tristeza,
        /// que reproducen un recorrido grabado. Con <c>null</c> el jugador queda sin control.
        /// </summary>
        public void FijarFuenteInput(IFuenteInput fuente)
        {
            _input = fuente ?? InputNeutro.Instancia;
        }

        /// <summary>
        /// El jugador de una escena concreta (objeto con el tag Player), o null. Busca solo en
        /// esa escena: durante una carga asíncrona conviven la escena que entra y la que sale.
        /// </summary>
        public static PlayerController BuscarEnEscena(Scene escena)
        {
            if (!escena.IsValid() || !escena.isLoaded)
            {
                return null;
            }

            foreach (var raiz in escena.GetRootGameObjects())
            {
                foreach (var jugador in raiz.GetComponentsInChildren<PlayerController>(true))
                {
                    if (jugador.CompareTag(GameTags.Player))
                    {
                        return jugador;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Coloca al jugador y, si estaba muerto, lo devuelve a la vida. Lo usa el LevelManager
        /// al reaparecer en el checkpoint.
        /// </summary>
        public void Revivir(Vector2 posicion)
        {
            motor.Teletransportar(posicion);
            Mirando = 1;

            if (!vida.EstaVivo)
            {
                vida.Revivir();
            }

            if (_maquina.Iniciada && _maquina.EstadoActual == ESTADOS_JUGADOR.DEAD)
            {
                _maquina.TransitionTo(ESTADOS_JUGADOR.IDLE);
            }
        }

        // --- Consultas y temporizadores para los estados ---

        internal bool QuiereSaltar() => FuenteInput.HaySaltoEnBuffer(parametros.jumpBuffer);

        internal void ConsumirSalto() => FuenteInput.ConsumirSalto();

        internal bool QuiereAgacharse() => permitirAgacharse && FuenteInput.AgacharseMantenido;

        internal bool QuiereAtacar() =>
            FuenteInput.AtaquePulsado || (habilidades != null && habilidades.HayAtaqueCargadoPendiente);

        internal bool QuiereDash()
        {
            if (!FuenteInput.DashPulsado || Ahora < _proximoDash)
            {
                return false;
            }

            if (!sensores.EnSuelo && !DashAereoDisponible)
            {
                return false;
            }

            // Bajo el agua solo con el Dash Acuático (fragmento de la Tristeza).
            return !motor.BajoElAgua || (habilidades != null && habilidades.Tiene(HABILIDADES.DASH_ACUATICO));
        }

        /// <summary>Arranca el cooldown del dash. En el aire gasta el dash aéreo hasta aterrizar.</summary>
        internal void RegistrarDash(bool enElAire)
        {
            _proximoDash = Ahora + parametros.dashDuration + parametros.dashCooldown;
            if (enElAire)
            {
                DashAereoDisponible = false;
            }
        }

        /// <summary>Al tocar suelo o pared se recuperan el doble salto y el dash aéreo.</summary>
        internal void RecargarSaltos()
        {
            DobleSaltoDisponible = true;
            DashAereoDisponible = true;
        }

        internal void IniciarCoyote() => _finCoyote = Ahora + parametros.coyoteTime;

        internal void CancelarCoyote() => _finCoyote = float.NegativeInfinity;

        internal void IniciarCoyotePared(int lado)
        {
            LadoPared = lado;
            _finCoyotePared = Ahora + parametros.coyoteTime;
        }

        internal void CancelarCoyotePared() => _finCoyotePared = float.NegativeInfinity;

        internal void Mirar(int direccion)
        {
            if (direccion != 0)
            {
                Mirando = direccion > 0 ? 1 : -1;
            }
        }

        /// <summary>Destino común al terminar DASH, ATTACK o DAMAGED.</summary>
        internal ESTADOS_JUGADOR EstadoTrasAccion() =>
            sensores.EnSuelo ? ESTADOS_JUGADOR.IDLE : ESTADOS_JUGADOR.FALL;

        // --- Eventos externos: también pasan por TransitionTo y por la tabla ---

        private void AlRecibirGolpe(Golpe golpe)
        {
            UltimoGolpe = golpe;

            if (habilidades != null)
            {
                habilidades.CancelarCarga();
            }

            // La muerte la gestiona AlMorir, que llega justo después si la vida llegó a 0.
            if (_maquina == null || !_maquina.Iniciada || !vida.EstaVivo)
            {
                return;
            }

            var actual = _maquina.EstadoActual;
            if (actual != ESTADOS_JUGADOR.DAMAGED && actual != ESTADOS_JUGADOR.DEAD)
            {
                _maquina.TransitionTo(ESTADOS_JUGADOR.DAMAGED);
            }
        }

        private void AlMorir()
        {
            if (_maquina != null && _maquina.Iniciada && _maquina.EstadoActual != ESTADOS_JUGADOR.DEAD)
            {
                _maquina.TransitionTo(ESTADOS_JUGADOR.DEAD);
            }
        }

        private void AlCambiarEstado(ESTADOS_JUGADOR anterior, ESTADOS_JUGADOR nuevo)
        {
            currentState = nuevo;

            if (registrarTransiciones && anterior != nuevo)
            {
                Debug.Log($"[Jugador] {anterior} -> {nuevo} (t = {Ahora:0.00})", this);
            }

            OnStateChanged?.Invoke(anterior, nuevo);
        }

        private void ResolverComponentes()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (sensores == null) sensores = GetComponent<PlayerSensors>();
            if (vida == null) vida = GetComponent<PlayerHealth>();
            if (habilidades == null) habilidades = GetComponent<PlayerAbilities>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            _input = fuenteInput as IFuenteInput;
            if (_input == null)
            {
                if (fuenteInput != null)
                {
                    Debug.LogWarning($"[Jugador] '{fuenteInput.GetType().Name}' no implementa IFuenteInput; se ignora.", this);
                }

                _input = GetComponent<IFuenteInput>();
            }

            if (_input == null)
            {
                Debug.LogWarning(
                    "[Jugador] No hay ninguna fuente de input (FuenteInputJugador). El jugador no responderá " +
                    "al teclado; añade el componente y asígnale InputSystem_Actions.", this);
            }

            if (hitboxAtaque == null)
            {
                Debug.LogWarning(
                    "[Jugador] Falta la hitbox de la espada (hijo con Hitbox en la capa PlayerWeapon). " +
                    "El ataque se reproduce pero no hace daño.", this);
            }
        }
    }
}
