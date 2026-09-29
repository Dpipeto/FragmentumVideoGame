using Fragmentum.Core;
using Fragmentum.Core.FSM;
using UnityEngine;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// DEAD: sin control. El cuerpo frena y cae al suelo. Solo se sale con
    /// <see cref="PlayerController.Revivir"/> (checkpoint), que vuelve a IDLE a través de la
    /// tabla como cualquier otra transición.
    ///
    /// El Game Over ya no lo dispara el jugador: el prototipo llamaba directamente a
    /// <c>GameManager.Instance.GameOver()</c>. Ahora quien quiera enterarse escucha
    /// <c>PlayerHealth.OnMuerte</c> (la FSM global, en la Fase 5).
    /// </summary>
    public sealed class PlayerDeadState : EstadoBase<ESTADOS_JUGADOR, PlayerController>
    {
        public PlayerDeadState(PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(ESTADOS_JUGADOR.DEAD, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            Dueno.Motor.SuspenderGravedad(false);
            Dueno.Motor.FijarVelocidad(new Vector2(0f, Mathf.Min(0f, Dueno.Motor.Velocidad.y)));
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Death, true);
        }

        public override void ActualizarFisica() =>
            Dueno.Motor.MoverHorizontal(0f, 0f, Dueno.Sensores.EnSuelo);

        public override void OnExit() =>
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Death, false);
    }
}
