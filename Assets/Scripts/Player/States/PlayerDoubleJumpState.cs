using Fragmentum.Core.FSM;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// DOUBLE_JUMP: segundo salto en el aire. Se gasta al entrar y se recupera al tocar
    /// suelo o pared.
    /// </summary>
    public sealed class PlayerDoubleJumpState : PlayerLocomotionState
    {
        public PlayerDoubleJumpState(PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(ESTADOS_JUGADOR.DOUBLE_JUMP, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            Dueno.ConsumirSalto();
            Dueno.DobleSaltoDisponible = false;
            Dueno.Motor.FijarVelocidadVertical(Dueno.P.velocidadDobleSalto * Dueno.Motor.MultSalto);
        }

        public override void Actualizar() => Dueno.Mirar(Dueno.DireccionEntrada);

        public override void ActualizarFisica() => ControlHorizontal(false);

        protected override ESTADOS_JUGADOR? EvaluarLocomocion() =>
            Dueno.Motor.Velocidad.y <= 0f ? ESTADOS_JUGADOR.FALL : (ESTADOS_JUGADOR?)null;
    }
}
