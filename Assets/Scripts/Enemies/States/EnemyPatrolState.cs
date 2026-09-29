using Fragmentum.Core;
using Fragmentum.Core.FSM;
using UnityEngine;

namespace Fragmentum.Enemies.States
{
    /// <summary>
    /// PATROL: camina hacia el waypoint actual. Al llegar (con tolerancia configurable) pasa
    /// al siguiente y, si hay espera, se detiene en IDLE. Si un borde o una pared le cortan
    /// el paso, da la vuelta en lugar de quedarse empujando.
    ///
    /// En el prototipo no había patrulla: fuera del radio de detección el enemigo se quedaba
    /// inmóvil, y todos los enemigos se sentían iguales.
    /// </summary>
    public sealed class EnemyPatrolState : EstadoBase<ESTADOS_ENEMIGO, EnemyController>
    {
        private bool _bloqueado;

        public EnemyPatrolState(EnemyController dueno, MaquinaEstados<ESTADOS_ENEMIGO> maquina)
            : base(ESTADOS_ENEMIGO.PATROL, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            _bloqueado = false;
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.MovEn, true);
        }

        public override void ActualizarFisica()
        {
            var dx = Dueno.WaypointActual.x - Dueno.transform.position.x;
            if (Mathf.Abs(dx) <= Dueno.D.toleranciaWaypoint)
            {
                Dueno.Detener();
                return;
            }

            if (!Dueno.Mover(dx > 0f ? 1 : -1, Dueno.D.velocidadPatrulla))
            {
                _bloqueado = true;
            }
        }

        public override ESTADOS_ENEMIGO? EvaluarTransiciones()
        {
            if (Dueno.JugadorDetectado(false))
            {
                return ESTADOS_ENEMIGO.CHASE;
            }

            if (!Dueno.LlegoAlWaypoint() && !_bloqueado)
            {
                return null;
            }

            _bloqueado = false;
            Dueno.AvanzarWaypoint();
            return Dueno.D.esperaEnWaypoint > 0f ? ESTADOS_ENEMIGO.IDLE : (ESTADOS_ENEMIGO?)null;
        }
    }
}
