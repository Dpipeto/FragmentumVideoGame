using Fragmentum.Core;
using Fragmentum.Core.FSM;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// ATTACK: golpe de espada. La hitbox se abre y se cierra por tiempo, dentro de la
    /// duración del ataque, así que un evento de animación perdido nunca deja al jugador
    /// bloqueado (en el prototipo, <c>attack</c> solo se apagaba con un evento de animación).
    ///
    /// Si al entrar hay una carga completa soltada (habilidad Ataque Cargado, mundo de la
    /// Ira), el golpe sale cargado: más daño, más lento y marcado como cargado para que los
    /// barriles explosivos sepan que deben detonar. No es un estado aparte: es este mismo
    /// estado con un modificador.
    /// </summary>
    public sealed class PlayerAttackState : EstadoBase<ESTADOS_JUGADOR, PlayerController>
    {
        private bool _cargado;
        private float _duracion;
        private float _escalaVentana;
        private bool _golpeLanzado;
        private int _pasosFisicaConHitbox;

        public PlayerAttackState(PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(ESTADOS_JUGADOR.ATTACK, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            var habilidades = Dueno.Habilidades;
            _cargado = habilidades != null && habilidades.ConsumirAtaqueCargado();
            _duracion = _cargado ? Dueno.P.duracionAtaqueCargado : Dueno.P.duracionAtaque;
            _escalaVentana = Dueno.P.duracionAtaque > 0f ? _duracion / Dueno.P.duracionAtaque : 1f;
            _golpeLanzado = false;
            _pasosFisicaConHitbox = 0;

            var espada = Dueno.Espada;
            if (espada != null)
            {
                var multiplicador = _cargado ? habilidades.MultiplicadorDanioCargado : 1;
                espada.Danio = Dueno.DanioBaseEspada * multiplicador;
                espada.Cargado = _cargado;
            }

            FijarHitbox(false);
            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Attack, true);
        }

        public override void Actualizar()
        {
            var t = TiempoEnEstado;

            if (!_golpeLanzado && t >= Dueno.P.inicioVentanaGolpe * _escalaVentana)
            {
                _golpeLanzado = true;
                FijarHitbox(true);
            }
            else if (_golpeLanzado && _pasosFisicaConHitbox > 0 && t >= Dueno.P.finVentanaGolpe * _escalaVentana)
            {
                // Se exige al menos un paso de física con la hitbox abierta: con un frame muy
                // largo el golpe podría abrirse y cerrarse sin que la física lo viera.
                FijarHitbox(false);
            }
        }

        public override void ActualizarFisica()
        {
            if (Dueno.Espada != null && Dueno.Espada.gameObject.activeSelf)
            {
                _pasosFisicaConHitbox++;
            }

            // En el suelo el ataque planta al jugador; en el aire conserva el control.
            var enSuelo = Dueno.Sensores.EnSuelo;
            Dueno.Motor.MoverHorizontal(enSuelo ? 0f : Dueno.EjeHorizontal, Dueno.P.velocidadMaxima, enSuelo);
        }

        public override void OnExit()
        {
            FijarHitbox(false);

            var espada = Dueno.Espada;
            if (espada != null)
            {
                espada.Danio = Dueno.DanioBaseEspada;
                espada.Cargado = false;
            }

            AnimatorParams.FijarBool(Dueno.Animador, AnimatorParams.Attack, false);
        }

        public override ESTADOS_JUGADOR? EvaluarTransiciones() =>
            TiempoEnEstado >= _duracion ? Dueno.EstadoTrasAccion() : (ESTADOS_JUGADOR?)null;

        /// <summary>¿Salió cargado el ataque en curso? (para VFX y depuración)</summary>
        public bool Cargado => _cargado;

        /// <summary>Duración del ataque en curso: la del cargado si salió cargado.</summary>
        public float Duracion => _duracion;

        private void FijarHitbox(bool activa)
        {
            var espada = Dueno.Espada;
            if (espada != null && espada.gameObject.activeSelf != activa)
            {
                espada.gameObject.SetActive(activa);
            }
        }
    }
}
