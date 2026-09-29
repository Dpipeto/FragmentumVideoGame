using Fragmentum.Core.FSM;
using UnityEngine;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// WALL_JUMP: impulso hacia fuera de la pared con el input horizontal bloqueado durante
    /// <c>wallJumpDuration</c>.
    ///
    /// Corrige dos fallos del prototipo:
    /// - <c>isWallJumping</c> se activaba pero no bloqueaba el movimiento, así que el input del
    ///   mismo frame anulaba el impulso. Aquí este estado simplemente no aplica control
    ///   horizontal mientras dura.
    /// - La dirección salía de <c>-localScale.x</c>, que dependía del sprite. Aquí sale del lado
    ///   de la pared detectada por los sensores.
    /// </summary>
    public sealed class PlayerWallJumpState : PlayerLocomotionState
    {
        public PlayerWallJumpState(PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(ESTADOS_JUGADOR.WALL_JUMP, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            Dueno.ConsumirSalto();
            Dueno.CancelarCoyotePared();

            var haciaFuera = Dueno.LadoPared != 0 ? -Dueno.LadoPared : -Dueno.Mirando;
            Dueno.Mirar(haciaFuera);

            var impulso = Dueno.P.impulsoWallJump;
            Dueno.Motor.FijarVelocidad(new Vector2(haciaFuera * impulso.x, impulso.y * Dueno.Motor.MultSalto));
        }

        // Sin ActualizarFisica: el input horizontal queda bloqueado mientras dura el estado.

        protected override ESTADOS_JUGADOR? EvaluarLocomocion() =>
            TiempoEnEstado >= Dueno.P.wallJumpDuration ? ESTADOS_JUGADOR.FALL : (ESTADOS_JUGADOR?)null;
    }
}
