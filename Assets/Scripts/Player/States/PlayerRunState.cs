using Fragmentum.Core;
using Fragmentum.Core.FSM;
using UnityEngine;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// RUN: en el suelo con input horizontal.
    ///
    /// El parámetro Movement del Animator recibe la velocidad real en u/s. En el prototipo
    /// recibía <c>velocidadX * runSpeed</c> con <c>velocidadX</c> ya multiplicada por
    /// <c>runSpeed</c>: el blend tree veía runSpeed².
    /// </summary>
    public sealed class PlayerRunState : PlayerLocomotionState
    {
        public PlayerRunState(PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(ESTADOS_JUGADOR.RUN, dueno, maquina)
        {
        }

        public override void OnEnter() => Dueno.RecargarSaltos();

        public override void Actualizar()
        {
            Dueno.Mirar(Dueno.DireccionEntrada);
            AnimatorParams.FijarFloat(Dueno.Animador, AnimatorParams.Movement, Mathf.Abs(Dueno.Motor.Velocidad.x));
        }

        public override void ActualizarFisica() => ControlHorizontal(true);

        public override void OnExit() =>
            AnimatorParams.FijarFloat(Dueno.Animador, AnimatorParams.Movement, 0f);

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

            return Dueno.DireccionEntrada == 0 ? ESTADOS_JUGADOR.IDLE : (ESTADOS_JUGADOR?)null;
        }
    }
}
