using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Core.FSM
{
    /// <summary>
    /// Máquina de estados finitos genérica (patrón State). La comparten el jugador, los
    /// enemigos, los jefes y el juego global.
    ///
    /// Garantías de diseño:
    /// - <see cref="TransitionTo"/> es el ÚNICO método que cambia de estado.
    /// - Toda transición se valida contra la <see cref="TablaTransiciones{TEstado}"/>;
    ///   una transición no declarada se rechaza y se avisa por consola.
    /// - Cada cambio ejecuta <c>OnExit</c> del estado que se deja y <c>OnEnter</c> del que entra,
    ///   en ese orden, y emite <see cref="OnStateChanged"/> para audio, VFX y UI.
    /// </summary>
    public sealed class MaquinaEstados<TEstado> where TEstado : struct, Enum
    {
        /// <summary>Tope de transiciones encadenadas en un mismo frame; evita bucles infinitos.</summary>
        public const int MaxTransicionesPorFrame = 8;

        private readonly Dictionary<TEstado, IEstado<TEstado>> _estados =
            new Dictionary<TEstado, IEstado<TEstado>>();

        private readonly TablaTransiciones<TEstado> _tabla;
        private readonly Func<float> _reloj;
        private float _tiempoEntrada;

        /// <summary>
        /// Crea la máquina. El reloj se puede sustituir en las pruebas para no depender de
        /// <see cref="Time.time"/>.
        /// </summary>
        public MaquinaEstados(TablaTransiciones<TEstado> tabla, Func<float> reloj = null)
        {
            _tabla = tabla ?? throw new ArgumentNullException(nameof(tabla));
            _reloj = reloj ?? (() => Time.time);
        }

        /// <summary>Se emite tras cada cambio de estado: (estado anterior, estado nuevo).</summary>
        public event Action<TEstado, TEstado> OnStateChanged;

        /// <summary>Nombre legible de la máquina, usado en los mensajes de consola.</summary>
        public string Nombre => _tabla.Nombre;

        public TEstado EstadoActual { get; private set; }

        public TEstado EstadoAnterior { get; private set; }

        /// <summary>¿Ya se llamó a <see cref="Iniciar"/>?</summary>
        public bool Iniciada { get; private set; }

        /// <summary>Segundos desde que se entró en el estado actual.</summary>
        public float TiempoEnEstado => _reloj() - _tiempoEntrada;

        /// <summary>Instancia del estado actual.</summary>
        public IEstado<TEstado> Estado =>
            _estados.TryGetValue(EstadoActual, out var estado) ? estado : null;

        /// <summary>Registra la instancia que implementa un estado. Se llama una vez, al construir.</summary>
        public void Registrar(IEstado<TEstado> estado)
        {
            if (estado == null)
            {
                throw new ArgumentNullException(nameof(estado));
            }

            _estados[estado.Id] = estado;
        }

        /// <summary>Arranca la máquina en el estado indicado y ejecuta su <c>OnEnter</c>.</summary>
        public void Iniciar(TEstado inicial)
        {
            if (!_estados.TryGetValue(inicial, out var estado))
            {
                Debug.LogError($"[FSM {Nombre}] No se puede iniciar en {inicial}: el estado no está registrado.");
                return;
            }

            EstadoActual = inicial;
            EstadoAnterior = inicial;
            _tiempoEntrada = _reloj();
            Iniciada = true;
            estado.OnEnter();
            OnStateChanged?.Invoke(inicial, inicial);
        }

        /// <summary>
        /// Para la máquina: ejecuta el <c>OnExit</c> del estado actual y la deja sin iniciar, lista
        /// para volver a arrancar con <see cref="Iniciar"/>. La usa un jefe cuando el jugador
        /// muere en la arena y el combate vuelve a empezar. No es una transición: no emite
        /// <see cref="OnStateChanged"/>.
        /// </summary>
        public void Detener()
        {
            if (!Iniciada)
            {
                return;
            }

            _estados[EstadoActual].OnExit();
            Iniciada = false;
        }

        /// <summary>
        /// ÚNICO punto de cambio de estado de todo el juego. Devuelve <c>true</c> si el cambio
        /// se aplicó; <c>false</c> si la transición no estaba declarada o el estado no existe.
        /// </summary>
        public bool TransitionTo(TEstado nuevo)
        {
            if (!Iniciada)
            {
                Debug.LogError($"[FSM {Nombre}] TransitionTo({nuevo}) antes de Iniciar().");
                return false;
            }

            if (!_estados.TryGetValue(nuevo, out var siguiente))
            {
                Debug.LogError($"[FSM {Nombre}] El estado {nuevo} no está registrado.");
                return false;
            }

            if (!_tabla.EsValida(EstadoActual, nuevo))
            {
                Debug.LogError(
                    $"[FSM {Nombre}] Transición no declarada: {EstadoActual} -> {nuevo}. " +
                    "Añádela a la tabla de transiciones o corrige la condición que la pidió.");
                return false;
            }

            var anterior = EstadoActual;
            _estados[anterior].OnExit();

            EstadoAnterior = anterior;
            EstadoActual = nuevo;
            _tiempoEntrada = _reloj();

            siguiente.OnEnter();
            OnStateChanged?.Invoke(anterior, nuevo);
            return true;
        }

        /// <summary>Lógica por frame: ejecuta el estado actual y aplica la transición que pida.</summary>
        public void Actualizar()
        {
            if (!Iniciada)
            {
                return;
            }

            // Solo se ejecuta el Actualizar del estado con el que empieza el frame. Después se
            // encadenan las transiciones que pidan los estados por los que se vaya pasando, para
            // que un cambio como DASH -> LOCOMOCION -> IDLE se resuelva en el mismo frame.
            _estados[EstadoActual].Actualizar();

            for (var i = 0; i < MaxTransicionesPorFrame; i++)
            {
                var deseado = _estados[EstadoActual].EvaluarTransiciones();
                if (!deseado.HasValue || EqualityComparer<TEstado>.Default.Equals(deseado.Value, EstadoActual))
                {
                    return;
                }

                if (!TransitionTo(deseado.Value))
                {
                    return;
                }
            }

            Debug.LogWarning(
                $"[FSM {Nombre}] Se alcanzó el tope de {MaxTransicionesPorFrame} transiciones en un frame " +
                $"(estado actual: {EstadoActual}). Puede haber un ciclo entre condiciones.");
        }

        /// <summary>Lógica por paso de física del estado actual. Nunca transita.</summary>
        public void ActualizarFisica()
        {
            if (Iniciada)
            {
                _estados[EstadoActual].ActualizarFisica();
            }
        }
    }
}
