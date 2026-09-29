using Fragmentum.Core.FSM;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// FALL: en el aire bajando, o tras perder el suelo.
    ///
    /// Aquí se resuelven las dos «ayudas» que el prototipo no tenía y que hacían sentir el
    /// salto injusto en los bordes:
    /// - Coyote time: si se acaba de salir de un borde caminando, todavía se puede saltar.
    /// - Coyote de pared: si se acaba de soltar una pared, todavía se puede hacer wall jump.
    /// El jump buffer (saltar justo antes de aterrizar) lo resuelven IDLE y RUN al entrar.
    /// </summary>
    public sealed class PlayerFallState : PlayerLocomotionState
    {
        public PlayerFallState(PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(ESTADOS_JUGADOR.FALL, dueno, maquina)
        {
        }

        public override void Actualizar() => Dueno.Mirar(Dueno.DireccionEntrada);

        public override void ActualizarFisica() => ControlHorizontal(false);

        protected override ESTADOS_JUGADOR? EvaluarLocomocion()
        {
            if (Dueno.QuiereSaltar())
            {
                if (Dueno.EnCoyote)
                {
                    return ESTADOS_JUGADOR.JUMP;
                }

                if (Dueno.EnCoyotePared)
                {
                    return ESTADOS_JUGADOR.WALL_JUMP;
                }

                if (Dueno.DobleSaltoDisponible)
                {
                    return ESTADOS_JUGADOR.DOUBLE_JUMP;
                }
            }

            var vy = Dueno.Motor.Velocidad.y;
            if (Dueno.Sensores.EnSuelo && vy <= 0.01f)
            {
                return SegunInput();
            }

            var direccion = Dueno.DireccionEntrada;
            if (direccion != 0 && vy <= 0f && Dueno.Sensores.HayParedHacia(direccion))
            {
                Dueno.LadoPared = direccion;
                return ESTADOS_JUGADOR.WALL_SLIDE;
            }

            return null;
        }
    }
}
