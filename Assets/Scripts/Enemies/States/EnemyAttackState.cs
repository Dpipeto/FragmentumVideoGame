using Fragmentum.Core;
using Fragmentum.Core.FSM;

namespace Fragmentum.Enemies.States
{
    /// <summary>
    /// ATTACK: se planta de cara al jugador, telegrafía el golpe y abre la hitbox del arma
    /// durante su ventana. Al salir (por tiempo, por el evento de animación o por un golpe
    /// recibido) arranca el cooldown.
    ///
    /// En el prototipo no había cooldown: <c>attack</c> solo se apagaba por un evento de
    /// animación o al recibir daño.
    /// </summary>
    public sealed class EnemyAttackState : EstadoBase<ESTADOS_ENEMIGO, EnemyController>
    {
        private bool _golpeLanzado;
        private int _pasosFisicaConArma;

        public EnemyAttackState(EnemyController dueno, MaquinaEstados<ESTADOS_ENEMIGO> maquina)
            : base(ESTADOS_ENEMIGO.ATTACK, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            Dueno.Mirar(Dueno.DireccionHaciaJugador);
            Dueno.Detener();
            Dueno.FinAtaquePorAnimacion = false;
            Dueno.FijarArma(false);
            _golpeLanzado = false;
            _pasosFisicaConArma = 0;

            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.MovEn, false);
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Attack, true);
        }

        public override void Actualizar()
        {
            var t = TiempoEnEstado;
            if (!_golpeLanzado && t >= Dueno.D.inicioVentanaGolpe)
            {
                _golpeLanzado = true;
                Dueno.FijarArma(true);
            }
            else if (_golpeLanzado && _pasosFisicaConArma > 0 && t >= Dueno.D.finVentanaGolpe)
            {
                Dueno.FijarArma(false);
            }
        }

        public override void ActualizarFisica()
        {
            Dueno.Detener();
            if (Dueno.Arma != null && Dueno.Arma.gameObject.activeSelf)
            {
                _pasosFisicaConArma++;
            }
        }

        public override void OnExit()
        {
            Dueno.FijarArma(false);
            Dueno.ArrancarCooldown();
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Attack, false);
        }

        public override ESTADOS_ENEMIGO? EvaluarTransiciones()
        {
            var terminado = TiempoEnEstado >= Dueno.D.duracionAtaque ||
                            (Dueno.FinAtaquePorAnimacion && _golpeLanzado && _pasosFisicaConArma > 0);
            return terminado ? ESTADOS_ENEMIGO.CHASE : (ESTADOS_ENEMIGO?)null;
        }
    }
}
