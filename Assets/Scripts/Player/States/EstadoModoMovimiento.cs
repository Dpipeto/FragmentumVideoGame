using Fragmentum.Core.FSM;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// Los tres modos de movimiento (STEALTH, NORMAL, RUN) comparten una sola clase: no
    /// tienen comportamiento propio salvo que RUN hace ruido. Se crea una instancia por modo.
    /// </summary>
    public sealed class EstadoModoMovimiento : EstadoBase<MODOS_MOVIMIENTO, PlayerModoMovimiento>
    {
        public EstadoModoMovimiento(MODOS_MOVIMIENTO id, PlayerModoMovimiento dueno, MaquinaEstados<MODOS_MOVIMIENTO> maquina)
            : base(id, dueno, maquina)
        {
        }

        public override void Actualizar()
        {
            if (Id == MODOS_MOVIMIENTO.RUN)
            {
                Dueno.EmitirRuidoSiToca();
            }
        }

        public override MODOS_MOVIMIENTO? EvaluarTransiciones() =>
            Dueno.ModoDeseado != Id ? Dueno.ModoDeseado : (MODOS_MOVIMIENTO?)null;
    }
}
