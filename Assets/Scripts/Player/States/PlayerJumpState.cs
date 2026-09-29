using Fragmentum.Core.FSM;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// JUMP: subida del salto desde el suelo (o dentro del coyote time).
    ///
    /// Corrige el salto flotante del prototipo: allí <c>Input.GetKey("space")</c> reasignaba la
    /// velocidad de salto en cada frame mientras se mantenía la tecla. Aquí el impulso se
    /// aplica una sola vez, en <see cref="OnEnter"/>, y la pulsación se consume del buffer.
    /// Mantener el botón solo alarga el salto hasta su altura máxima; soltarlo antes lo corta.
    /// </summary>
    public sealed class PlayerJumpState : PlayerLocomotionState
    {
        private bool _subidaCortada;

        public PlayerJumpState(PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(ESTADOS_JUGADOR.JUMP, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            Dueno.ConsumirSalto();
            Dueno.CancelarCoyote();
            Dueno.Motor.FijarVelocidadVertical(Dueno.P.velocidadSalto * Dueno.Motor.MultSalto);
            _subidaCortada = false;
        }

        public override void Actualizar()
        {
            Dueno.Mirar(Dueno.DireccionEntrada);

            // Salto de altura variable: soltar el botón mientras se sube recorta la subida.
            if (!_subidaCortada && !Dueno.FuenteInput.SaltoMantenido)
            {
                Dueno.Motor.CortarSubida(Dueno.P.cortePorSoltarSalto);
                _subidaCortada = true;
            }
        }

        public override void ActualizarFisica() => ControlHorizontal(false);

        protected override ESTADOS_JUGADOR? EvaluarLocomocion()
        {
            if (Dueno.QuiereSaltar() && Dueno.DobleSaltoDisponible)
            {
                return ESTADOS_JUGADOR.DOUBLE_JUMP;
            }

            return Dueno.Motor.Velocidad.y <= 0f ? ESTADOS_JUGADOR.FALL : (ESTADOS_JUGADOR?)null;
        }
    }
}
