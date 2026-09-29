using Fragmentum.Core;
using Fragmentum.Core.FSM;

namespace Fragmentum.Enemies.States
{
    /// <summary>
    /// DEAD: estado final. Apaga colisiones y hitbox (un cadáver no hace daño ni bloquea el
    /// paso), detiene la física, dispara la animación y destruye el objeto tras
    /// <c>deathDelay</c>. La tabla no declara ninguna salida.
    /// </summary>
    public sealed class EnemyDeadState : EstadoBase<ESTADOS_ENEMIGO, EnemyController>
    {
        public EnemyDeadState(EnemyController dueno, MaquinaEstados<ESTADOS_ENEMIGO> maquina)
            : base(ESTADOS_ENEMIGO.DEAD, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.MovEn, false);
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Attack, false);
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Damage, false);
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Death, true);
            Dueno.Morir();
        }
    }
}
