using Fragmentum.Core.FSM;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// WALL_SLIDE: pegado a una pared, empujando hacia ella y cayendo despacio. Recupera el
    /// doble salto y el dash aéreo, y mira hacia fuera de la pared (hacia donde saldrá el
    /// wall jump).
    /// </summary>
    public sealed class PlayerWallSlideState : PlayerLocomotionState
    {
        private int _lado;

        public PlayerWallSlideState(PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(ESTADOS_JUGADOR.WALL_SLIDE, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            _lado = Dueno.LadoPared;
            Dueno.RecargarSaltos();
            Dueno.CancelarCoyotePared();
            Dueno.Mirar(-_lado);
        }

        public override void ActualizarFisica()
        {
            ControlHorizontal(false);
            Dueno.Motor.LimitarCaida(Dueno.P.velocidadWallSlide);
        }

        protected override ESTADOS_JUGADOR? EvaluarLocomocion()
        {
            // El wall jump tiene prioridad sobre el doble salto.
            if (Dueno.QuiereSaltar())
            {
                Dueno.LadoPared = _lado;
                return ESTADOS_JUGADOR.WALL_JUMP;
            }

            if (Dueno.Sensores.EnSuelo)
            {
                return ESTADOS_JUGADOR.IDLE;
            }

            if (!Dueno.Sensores.HayParedHacia(_lado) || Dueno.DireccionEntrada != _lado)
            {
                Dueno.IniciarCoyotePared(_lado);
                return ESTADOS_JUGADOR.FALL;
            }

            return null;
        }
    }
}
