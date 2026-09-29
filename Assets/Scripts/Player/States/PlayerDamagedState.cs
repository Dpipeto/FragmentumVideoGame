using Fragmentum.Core;
using Fragmentum.Core.FSM;
using UnityEngine;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// DAMAGED: retroceso tras un golpe, sin control durante <c>damagedDuration</c>. Los
    /// i-frames los lleva <see cref="PlayerHealth"/> por tiempo, así que no dependen de que
    /// termine una animación.
    ///
    /// El retroceso se aleja del origen del golpe. En el prototipo era un <c>AddForce</c> que
    /// el movimiento por <c>transform.position</c> anulaba en el frame siguiente.
    /// </summary>
    public sealed class PlayerDamagedState : EstadoBase<ESTADOS_JUGADOR, PlayerController>
    {
        public PlayerDamagedState(PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(ESTADOS_JUGADOR.DAMAGED, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            var golpe = Dueno.UltimoGolpe;
            var dx = Dueno.transform.position.x - golpe.Origen.x;
            var direccion = Mathf.Abs(dx) > 0.01f ? Mathf.Sign(dx) : -Dueno.Mirando;

            var retroceso = Dueno.P.impulsoRetroceso;
            Dueno.Motor.SuspenderGravedad(false);
            Dueno.Motor.FijarVelocidad(new Vector2(direccion * retroceso.x, retroceso.y));

            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Damage, true);
        }

        public override void OnExit() =>
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Damage, false);

        public override ESTADOS_JUGADOR? EvaluarTransiciones() =>
            TiempoEnEstado >= Dueno.P.damagedDuration ? Dueno.EstadoTrasAccion() : (ESTADOS_JUGADOR?)null;
    }
}
