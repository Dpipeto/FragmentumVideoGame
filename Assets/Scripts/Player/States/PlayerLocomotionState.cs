using Fragmentum.Core.FSM;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// Superestado LOCOMOCION (máquina de estados jerárquica). Agrupa los 8 estados de
    /// movimiento e implementa <b>una sola vez</b> las salidas comunes a DASH y ATTACK, en
    /// lugar de repetirlas en cada subestado.
    ///
    /// Patrón Template Method: <see cref="EvaluarTransiciones"/> fija el orden (primero las
    /// acciones, luego la locomoción propia) y cada subestado solo redefine
    /// <see cref="EvaluarLocomocion"/> y, si hace falta, <see cref="PuedeAccionar"/>.
    /// </summary>
    public abstract class PlayerLocomotionState : EstadoBase<ESTADOS_JUGADOR, PlayerController>
    {
        protected PlayerLocomotionState(ESTADOS_JUGADOR id, PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(id, dueno, maquina)
        {
        }

        public sealed override ESTADOS_JUGADOR? EvaluarTransiciones()
        {
            if (PuedeAccionar())
            {
                if (Dueno.QuiereDash())
                {
                    return ESTADOS_JUGADOR.DASH;
                }

                if (Dueno.QuiereAtacar())
                {
                    return ESTADOS_JUGADOR.ATTACK;
                }
            }

            return EvaluarLocomocion();
        }

        /// <summary>Transiciones propias del subestado, o <c>null</c> para quedarse.</summary>
        protected abstract ESTADOS_JUGADOR? EvaluarLocomocion();

        /// <summary>¿Puede salir a DASH o ATTACK ahora? Agachado solo si cabe de pie.</summary>
        protected virtual bool PuedeAccionar() => true;

        /// <summary>Control horizontal libre, común a casi todos los subestados.</summary>
        protected void ControlHorizontal(bool enSuelo) =>
            Dueno.Motor.MoverHorizontal(Dueno.EjeHorizontal, Dueno.P.velocidadMaxima, enSuelo);

        /// <summary>Al tocar suelo: RUN si hay input, IDLE si no.</summary>
        protected ESTADOS_JUGADOR SegunInput() =>
            Dueno.DireccionEntrada != 0 ? ESTADOS_JUGADOR.RUN : ESTADOS_JUGADOR.IDLE;
    }
}
