using Fragmentum.Core.FSM;
using UnityEngine;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// DASH: desplazamiento horizontal rápido, sin gravedad, durante <c>dashDuration</c>.
    ///
    /// En el prototipo el dash era un <c>AddForce</c> que peleaba con el movimiento por
    /// <c>transform.position</c>, solo funcionaba si ya te movías y su cooldown público se
    /// sobrescribía con 1 al usarlo. Aquí es un estado con duración y cooldown propios, sale
    /// hacia donde se mira aunque no haya input y, desde una pared, sale hacia fuera.
    /// </summary>
    public sealed class PlayerDashState : EstadoBase<ESTADOS_JUGADOR, PlayerController>
    {
        private int _direccion;

        public PlayerDashState(PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(ESTADOS_JUGADOR.DASH, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            if (Maquina.EstadoAnterior == ESTADOS_JUGADOR.WALL_SLIDE && Dueno.LadoPared != 0)
            {
                _direccion = -Dueno.LadoPared;
            }
            else
            {
                _direccion = Dueno.DireccionEntrada != 0 ? Dueno.DireccionEntrada : Dueno.Mirando;
            }

            Dueno.Mirar(_direccion);
            Dueno.RegistrarDash(!Dueno.Sensores.EnSuelo);
            Dueno.Motor.SuspenderGravedad(true);
            Empujar();
        }

        public override void ActualizarFisica() => Empujar();

        public override void OnExit()
        {
            Dueno.Motor.SuspenderGravedad(false);

            // Se sale con la velocidad de carrera, no con la del dash, para no deslizar.
            var salida = _direccion * Dueno.P.velocidadMaxima * Dueno.Motor.MultVelocidad;
            Dueno.Motor.FijarVelocidad(new Vector2(salida, 0f));
        }

        public override ESTADOS_JUGADOR? EvaluarTransiciones() =>
            TiempoEnEstado >= Dueno.P.dashDuration ? Dueno.EstadoTrasAccion() : (ESTADOS_JUGADOR?)null;

        private void Empujar() =>
            Dueno.Motor.FijarVelocidad(new Vector2(_direccion * Dueno.P.velocidadDash, 0f));
    }
}
