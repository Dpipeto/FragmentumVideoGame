using Fragmentum.Core;
using Fragmentum.Core.FSM;

namespace Fragmentum.Player.States
{
    /// <summary>IDLE: en el suelo y sin input horizontal.</summary>
    public sealed class PlayerIdleState : PlayerLocomotionState
    {
        public PlayerIdleState(PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(ESTADOS_JUGADOR.IDLE, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            Dueno.RecargarSaltos();
            AnimatorParams.FijarFloat(Dueno.Animador, AnimatorParams.Movement, 0f);
        }

        public override void ActualizarFisica() => ControlHorizontal(true);

        protected override ESTADOS_JUGADOR? EvaluarLocomocion()
        {
            if (Dueno.QuiereSaltar())
            {
                return ESTADOS_JUGADOR.JUMP;
            }

            if (!Dueno.Sensores.EnSuelo)
            {
                Dueno.IniciarCoyote();
                return ESTADOS_JUGADOR.FALL;
            }

            if (Dueno.QuiereAgacharse())
            {
                return ESTADOS_JUGADOR.CROUCH;
            }

            return Dueno.DireccionEntrada != 0 ? ESTADOS_JUGADOR.RUN : (ESTADOS_JUGADOR?)null;
        }
    }
}
