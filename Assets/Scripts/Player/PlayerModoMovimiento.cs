using System;
using Fragmentum.Core.FSM;
using Fragmentum.Player.States;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Región ortogonal del jugador: una segunda FSM, independiente de la principal, que
    /// clasifica cómo de rápido se mueve (STEALTH / NORMAL / RUN).
    ///
    /// Existe para el sigilo del mundo del Miedo (Anexo A, Nivel 3): correr por encima del
    /// 60 % de la velocidad máxima hace ruido que oyen los Centinelas Ciegos; caminar por
    /// debajo del 40 % es silencioso. Es otra instancia del mismo núcleo de FSM, así que el
    /// sigilo no añade ninguna bandera a los estados de movimiento.
    ///
    /// Solo publica <see cref="OnRuido"/>, y solo si el nivel activa el sigilo.
    /// </summary>
    [DeclaraFSM("JugadorModoMovimiento")]
    [DisallowMultipleComponent]
    public class PlayerModoMovimiento : MonoBehaviour
    {
        [SerializeField] private PlayerController jugador;

        [Header("Umbrales (fracción de la velocidad máxima)")]
        [SerializeField, Range(0f, 1f)] private float umbralSigilo = 0.4f;
        [SerializeField, Range(0f, 1f)] private float umbralCarrera = 0.6f;

        [Header("Ruido")]
        [Tooltip("Solo se emite ruido si el nivel activa el sigilo. Desde la Fase 5 lo fija LevelData.")]
        [SerializeField] private bool emitirRuido;

        [Tooltip("Radio en unidades en el que se oye el ruido de la carrera.")]
        [SerializeField] private float radioRuido = 6f;

        [Tooltip("Segundos entre dos ruidos mientras se corre.")]
        [SerializeField] private float intervaloRuido = 0.4f;

        [Header("Depuración")]
        [SerializeField] private MODOS_MOVIMIENTO currentState;

        private MaquinaEstados<MODOS_MOVIMIENTO> _maquina;
        private float _proximoRuido;

        /// <summary>Cambio de modo: (anterior, nuevo).</summary>
        public event Action<MODOS_MOVIMIENTO, MODOS_MOVIMIENTO> OnStateChanged;

        /// <summary>
        /// Ruido de Kael: (posición, radio). El de la carrera y, con <see cref="HacerRuido"/>, el de un
        /// cepo. Lo escuchan los enemigos que oyen y lo dibujan los anillos de ruido.
        /// </summary>
        public event Action<Vector2, float> OnRuido;

        public MODOS_MOVIMIENTO ModoActual => _maquina != null && _maquina.Iniciada ? _maquina.EstadoActual : MODOS_MOVIMIENTO.NORMAL;

        public bool EmitirRuido
        {
            get => emitirRuido;
            set => emitirRuido = value;
        }

        /// <summary>Modo que corresponde a la velocidad de este frame.</summary>
        internal MODOS_MOVIMIENTO ModoDeseado { get; private set; } = MODOS_MOVIMIENTO.NORMAL;

        public static TablaTransiciones<MODOS_MOVIMIENTO> CrearTabla()
        {
            return new TablaTransiciones<MODOS_MOVIMIENTO>("JugadorModoMovimiento")
                .Inicial(MODOS_MOVIMIENTO.NORMAL)
                .Permitir(MODOS_MOVIMIENTO.NORMAL, MODOS_MOVIMIENTO.STEALTH, "velocidad < 40 % de la máxima")
                .Permitir(MODOS_MOVIMIENTO.STEALTH, MODOS_MOVIMIENTO.NORMAL, "velocidad entre 40 % y 60 %")
                .Permitir(MODOS_MOVIMIENTO.NORMAL, MODOS_MOVIMIENTO.RUN, "velocidad > 60 %")
                .Permitir(MODOS_MOVIMIENTO.RUN, MODOS_MOVIMIENTO.NORMAL, "velocidad entre 40 % y 60 %")
                .Permitir(MODOS_MOVIMIENTO.STEALTH, MODOS_MOVIMIENTO.RUN, "velocidad > 60 %")
                .Permitir(MODOS_MOVIMIENTO.RUN, MODOS_MOVIMIENTO.STEALTH, "velocidad < 40 %");
        }

        /// <summary>Modo que corresponde a una fracción de la velocidad máxima (0 a 1).</summary>
        public static MODOS_MOVIMIENTO Clasificar(float fraccion, float umbralSigilo, float umbralCarrera)
        {
            if (fraccion < umbralSigilo)
            {
                return MODOS_MOVIMIENTO.STEALTH;
            }

            return fraccion > umbralCarrera ? MODOS_MOVIMIENTO.RUN : MODOS_MOVIMIENTO.NORMAL;
        }

        private void Awake()
        {
            if (jugador == null)
            {
                jugador = GetComponent<PlayerController>();
            }

            _maquina = new MaquinaEstados<MODOS_MOVIMIENTO>(CrearTabla());
            _maquina.Registrar(new EstadoModoMovimiento(MODOS_MOVIMIENTO.NORMAL, this, _maquina));
            _maquina.Registrar(new EstadoModoMovimiento(MODOS_MOVIMIENTO.STEALTH, this, _maquina));
            _maquina.Registrar(new EstadoModoMovimiento(MODOS_MOVIMIENTO.RUN, this, _maquina));
            _maquina.OnStateChanged += (anterior, nuevo) =>
            {
                currentState = nuevo;
                OnStateChanged?.Invoke(anterior, nuevo);
            };
        }

        private void Start() => _maquina.Iniciar(MODOS_MOVIMIENTO.NORMAL);

        private void Update()
        {
            if (jugador == null || jugador.Motor == null)
            {
                return;
            }

            var maxima = jugador.Parametros.velocidadMaxima * jugador.Motor.MultVelocidad;
            var fraccion = maxima > 0f ? Mathf.Abs(jugador.Motor.Velocidad.x) / maxima : 0f;
            ModoDeseado = Clasificar(fraccion, umbralSigilo, umbralCarrera);
            _maquina.Actualizar();
        }

        /// <summary>Lo llama el estado RUN en cada frame; respeta el intervalo entre ruidos.</summary>
        internal void EmitirRuidoSiToca()
        {
            if (!emitirRuido || Time.time < _proximoRuido)
            {
                return;
            }

            _proximoRuido = Time.time + intervaloRuido;
            OnRuido?.Invoke(transform.position, radioRuido);
        }

        /// <summary>
        /// Un ruido que hace Kael sin correr: el chasquido de un cepo al pisarlo (mundo del Miedo).
        /// Lo oyen los mismos enemigos que la carrera, en su propio radio. No depende de
        /// <see cref="EmitirRuido"/>: no es la carrera, es algo que suena.
        /// </summary>
        public void HacerRuido(Vector2 posicion, float radio)
        {
            if (radio > 0f)
            {
                OnRuido?.Invoke(posicion, radio);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (emitirRuido)
            {
                Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, radioRuido);
            }
        }
    }
}
