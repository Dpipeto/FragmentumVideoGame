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
    ///
    /// Con arte (<see cref="EnemyController.AnimadoPorEventos"/>) la ventana no va por tiempo:
    /// la abren y la cierran los eventos de a2 y a4, y el ataque acaba con el clip.
    /// </summary>
    public sealed class EnemyAttackState : EstadoBase<ESTADOS_ENEMIGO, EnemyController>
    {
        private bool _golpeLanzado;
        private bool _cierrePendiente;
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
            _cierrePendiente = false;
            _pasosFisicaConArma = 0;

            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.MovEn, false);
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Attack, true);
        }

        public override void Actualizar()
        {
            if (Dueno.AnimadoPorEventos)
            {
                return; // la ventana la marcan AbrirGolpe y CerrarGolpe
            }

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

        /// <summary>Evento de a2: abre el arma, una sola vez por ataque.</summary>
        public void AbrirGolpe()
        {
            if (_golpeLanzado)
            {
                return;
            }

            _golpeLanzado = true;
            Dueno.FijarArma(true);
        }

        /// <summary>
        /// Evento de a4: cierra el arma. Si todavía no ha habido ningún paso de física con ella
        /// abierta (un tirón juntó a2, a3 y a4 en un fotograma), espera a ese paso: si no, el
        /// golpe no podría tocar a nadie.
        /// </summary>
        public void CerrarGolpe()
        {
            if (!_golpeLanzado)
            {
                return;
            }

            if (_pasosFisicaConArma > 0)
            {
                Dueno.FijarArma(false);
            }
            else
            {
                _cierrePendiente = true;
            }
        }

        public override void ActualizarFisica()
        {
            Dueno.Detener();
            if (_cierrePendiente && _pasosFisicaConArma > 0)
            {
                _cierrePendiente = false;
                Dueno.FijarArma(false);
            }

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
            var terminado = TiempoEnEstado >= Dueno.DuracionAtaque ||
                            (Dueno.FinAtaquePorAnimacion && _golpeLanzado && _pasosFisicaConArma > 0);
            return terminado ? ESTADOS_ENEMIGO.CHASE : (ESTADOS_ENEMIGO?)null;
        }
    }
}
