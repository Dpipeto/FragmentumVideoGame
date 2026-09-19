using System;

namespace Fragmentum.Core.FSM
{
    /// <summary>
    /// Contrato de un estado de una máquina de estados finitos.
    ///
    /// Regla fundamental del diseño: un estado NUNCA cambia el estado actual.
    /// Solo puede *pedir* el cambio devolviendo el estado deseado en
    /// <see cref="EvaluarTransiciones"/>. El único punto que cambia de estado es
    /// <see cref="MaquinaEstados{TEstado}.TransitionTo"/>.
    /// </summary>
    /// <typeparam name="TEstado">Enumeración de estados, en MAYÚSCULAS (IDLE, RUN…).</typeparam>
    public interface IEstado<TEstado> where TEstado : struct, Enum
    {
        /// <summary>Estado de la enumeración al que corresponde esta clase.</summary>
        TEstado Id { get; }

        /// <summary>Se ejecuta al entrar. Aquí se configura el Animator, la física y los temporizadores.</summary>
        void OnEnter();

        /// <summary>Se ejecuta al salir. Aquí se deshace lo que hizo <see cref="OnEnter"/>.</summary>
        void OnExit();

        /// <summary>Lógica por frame (equivale a Update): lectura de input y presentación.</summary>
        void Actualizar();

        /// <summary>Lógica por paso de física (equivale a FixedUpdate): movimiento del Rigidbody.</summary>
        void ActualizarFisica();

        /// <summary>
        /// Devuelve el estado al que se quiere transitar, o <c>null</c> para seguir en este.
        /// La máquina valida la transición contra la tabla antes de aplicarla.
        /// </summary>
        TEstado? EvaluarTransiciones();
    }
}
