using Fragmentum.Core;
using Fragmentum.Core.FSM;
using UnityEngine;

namespace Fragmentum.Enemies.States
{
    /// <summary>
    /// CHASE: persigue al jugador hacia <b>cualquier</b> lado y ataca cuando lo tiene a tiro y
    /// el cooldown está listo. Solo lo suelta cuando se aleja más de detectionRadius × 1,5
    /// (histéresis), así que no parpadea entre perseguir y patrullar en el borde del radio.
    /// </summary>
    public sealed class EnemyChaseState : EstadoBase<ESTADOS_ENEMIGO, EnemyController>
    {
        public EnemyChaseState(EnemyController dueno, MaquinaEstados<ESTADOS_ENEMIGO> maquina)
            : base(ESTADOS_ENEMIGO.CHASE, dueno, maquina)
        {
        }

        public override void OnEnter() =>
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.MovEn, true);

        public override void ActualizarFisica()
        {
            var direccion = Dueno.DireccionHaciaJugador;
            var distancia = Mathf.Abs(Dueno.CentroJugador.x - Dueno.Centro.x);

            // Se acerca hasta quedar a tiro; ahí espera de cara al jugador.
            if (distancia > Dueno.D.attackRadius * 0.8f)
            {
                Dueno.Mover(direccion, Dueno.D.velocidadPersecucion);
            }
            else
            {
                Dueno.Mirar(direccion);
                Dueno.Detener();
            }
        }

        public override ESTADOS_ENEMIGO? EvaluarTransiciones()
        {
            if (!Dueno.JugadorDetectado(true))
            {
                return Dueno.TieneWaypoints ? ESTADOS_ENEMIGO.PATROL : ESTADOS_ENEMIGO.IDLE;
            }

            return Dueno.JugadorEnRangoDeAtaque && Dueno.AtaqueListo ? ESTADOS_ENEMIGO.ATTACK : (ESTADOS_ENEMIGO?)null;
        }
    }
}
