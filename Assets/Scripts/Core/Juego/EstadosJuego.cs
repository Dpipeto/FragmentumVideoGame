using Fragmentum.Core.FSM;
using UnityEngine;

namespace Fragmentum.Core.Juego
{
    /// <summary>Estados de la FSM global del juego (plan, §3.6).</summary>
    public enum ESTADOS_JUEGO
    {
        BOOT,
        MAIN_MENU,
        LEVEL_SELECT,
        LOADING,
        PLAYING,
        PAUSED,
        CUTSCENE,
        GAME_OVER,
        LEVEL_COMPLETE
    }

    /// <summary>
    /// Estado genérico: al entrar fija la escala de tiempo y el bloqueo del input según la
    /// tabla de efectos de <see cref="GameStateMachine.Efectos"/>. Es el único sitio del
    /// juego que escribe <c>Time.timeScale</c>; en la v1 había cinco escrituras dispersas.
    /// </summary>
    public class EstadoJuego : EstadoBase<ESTADOS_JUEGO, GameStateMachine>
    {
        public EstadoJuego(ESTADOS_JUEGO id, GameStateMachine dueno, MaquinaEstados<ESTADOS_JUEGO> maquina)
            : base(id, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            var (escalaTiempo, bloqueaInput) = GameStateMachine.Efectos(Id);
            Time.timeScale = escalaTiempo;
            EventosJuego.FijarEntradaBloqueada(bloqueaInput);
        }
    }

    /// <summary>BOOT: decide dónde arranca según la escena abierta (menú, selector o juego).</summary>
    public sealed class EstadoArranque : EstadoJuego
    {
        public EstadoArranque(GameStateMachine dueno, MaquinaEstados<ESTADOS_JUEGO> maquina)
            : base(ESTADOS_JUEGO.BOOT, dueno, maquina)
        {
        }

        public override ESTADOS_JUEGO? EvaluarTransiciones() => Dueno.DestinoDeArranque();
    }

    /// <summary>LOADING: pide la escena al cargador y, cuando termina, va al destino elegido.</summary>
    public sealed class EstadoCargando : EstadoJuego
    {
        public EstadoCargando(GameStateMachine dueno, MaquinaEstados<ESTADOS_JUEGO> maquina)
            : base(ESTADOS_JUEGO.LOADING, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Dueno.IniciarCarga();
        }

        public override ESTADOS_JUEGO? EvaluarTransiciones() =>
            Dueno.CargaTerminada ? Dueno.DestinoDeCarga : (ESTADOS_JUEGO?)null;
    }

    /// <summary>
    /// PLAYING: al volver de un Game Over pide reaparecer en el checkpoint. Cuando el jugador
    /// muere, pasa al Game Over tras una breve espera para que se vea la caída.
    /// </summary>
    public sealed class EstadoJugando : EstadoJuego
    {
        public EstadoJugando(GameStateMachine dueno, MaquinaEstados<ESTADOS_JUEGO> maquina)
            : base(ESTADOS_JUEGO.PLAYING, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            if (Maquina.EstadoAnterior == ESTADOS_JUEGO.GAME_OVER)
            {
                EventosJuego.SolicitarReintento();
                Dueno.AsegurarJugadorVivo();
            }
        }

        public override ESTADOS_JUEGO? EvaluarTransiciones() =>
            Dueno.GameOverPendiente ? ESTADOS_JUEGO.GAME_OVER : (ESTADOS_JUEGO?)null;
    }

    /// <summary>
    /// GAME_OVER y LEVEL_COMPLETE: esperan a que su pantalla del UIManager decida. Si la
    /// pantalla no existe, avanzan solos tras una espera («construir para ausencia»): el
    /// Game Over reintenta y el fin de nivel continúa. El juego nunca se queda atascado.
    /// </summary>
    public sealed class EstadoEsperaPantalla : EstadoJuego
    {
        private readonly ESTADOS_JUEGO _siNoHayPantalla;
        private readonly System.Func<bool> _hayPantalla;
        private bool _avisado;

        public EstadoEsperaPantalla(ESTADOS_JUEGO id, ESTADOS_JUEGO siNoHayPantalla, System.Func<bool> hayPantalla,
            GameStateMachine dueno, MaquinaEstados<ESTADOS_JUEGO> maquina)
            : base(id, dueno, maquina)
        {
            _siNoHayPantalla = siNoHayPantalla;
            _hayPantalla = hayPantalla;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            if (Id == ESTADOS_JUEGO.LEVEL_COMPLETE)
            {
                Dueno.PrepararContinuacion();
            }
        }

        public override ESTADOS_JUEGO? EvaluarTransiciones()
        {
            if (_hayPantalla())
            {
                return null;
            }

            if (!_avisado)
            {
                _avisado = true;
                Debug.LogWarning($"[Juego] No hay pantalla para {Id} (falta el UIManager en los sistemas): se pasa sola a {_siNoHayPantalla}.");
            }

            return TiempoEnEstado >= Dueno.EsperaSinPantalla ? _siNoHayPantalla : (ESTADOS_JUEGO?)null;
        }
    }
}
