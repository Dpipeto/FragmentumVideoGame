using Fragmentum.Core;
using Fragmentum.Core.FSM;

namespace Fragmentum.Enemies.States
{
    /// <summary>
    /// IDLE: quieto. Es la pausa en un waypoint y el estado de un enemigo sin ruta: si no
    /// tiene waypoints se queda aquí sin lanzar ninguna excepción (degradación elegante).
    /// </summary>
    public sealed class EnemyIdleState : EstadoBase<ESTADOS_ENEMIGO, EnemyController>
    {
        private float _finEspera;

        public EnemyIdleState(EnemyController dueno, MaquinaEstados<ESTADOS_ENEMIGO> maquina)
            : base(ESTADOS_ENEMIGO.IDLE, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            // Solo se espera al llegar a un waypoint; al arrancar, la patrulla empieza ya.
            var espera = Maquina.EstadoAnterior == ESTADOS_ENEMIGO.PATROL ? Dueno.D.esperaEnWaypoint : 0f;
            _finEspera = Dueno.Ahora + espera;
            Dueno.Detener();
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.MovEn, false);
        }

        public override void ActualizarFisica() => Dueno.Detener();

        public override ESTADOS_ENEMIGO? EvaluarTransiciones()
        {
            if (Dueno.JugadorDetectado(false))
            {
                return ESTADOS_ENEMIGO.CHASE;
            }

            return Dueno.TieneWaypoints && Dueno.Ahora >= _finEspera ? ESTADOS_ENEMIGO.PATROL : (ESTADOS_ENEMIGO?)null;
        }
    }
}
