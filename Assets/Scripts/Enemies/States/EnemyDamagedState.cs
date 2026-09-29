using Fragmentum.Core;
using Fragmentum.Core.FSM;

namespace Fragmentum.Enemies.States
{
    /// <summary>
    /// DAMAGED: aturdimiento breve con retroceso (menor en los enemigos pesados). Interrumpe
    /// el ataque y cierra el arma. Los i-frames los lleva <c>Health</c> por tiempo. Al terminar
    /// vuelve a CHASE: recibir un golpe revela dónde está el jugador.
    /// </summary>
    public sealed class EnemyDamagedState : EstadoBase<ESTADOS_ENEMIGO, EnemyController>
    {
        public EnemyDamagedState(EnemyController dueno, MaquinaEstados<ESTADOS_ENEMIGO> maquina)
            : base(ESTADOS_ENEMIGO.DAMAGED, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            Dueno.FijarArma(false);
            Dueno.FinDanioPorAnimacion = false;
            Dueno.AplicarRetroceso(Dueno.UltimoGolpe);

            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.MovEn, false);
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Damage, true);
        }

        public override void OnExit() =>
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Damage, false);

        public override ESTADOS_ENEMIGO? EvaluarTransiciones()
        {
            // El evento de animación de la v1 (DesactiveDamage) puede acortarlo, nunca alargarlo.
            var terminado = TiempoEnEstado >= Dueno.D.damagedDuration ||
                            (Dueno.FinDanioPorAnimacion && TiempoEnEstado >= 0.1f);
            return terminado ? ESTADOS_ENEMIGO.CHASE : (ESTADOS_ENEMIGO?)null;
        }
    }
}
