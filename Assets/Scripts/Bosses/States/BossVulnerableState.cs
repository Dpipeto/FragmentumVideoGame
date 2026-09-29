using Fragmentum.Core.FSM;

namespace Fragmentum.Bosses.States
{
    /// <summary>
    /// VULNERABLE: el jefe se detiene y recibe más daño (o, si es inmune fuera de la ventana,
    /// por fin puede recibirlo). Al terminar vuelve a la fase que le toque según la vida: si
    /// durante la ventana bajó de un umbral, sale directamente a la fase siguiente.
    /// </summary>
    public sealed class BossVulnerableState : EstadoBase<ESTADOS_JEFE, BossController>
    {
        public BossVulnerableState(BossController dueno, MaquinaEstados<ESTADOS_JEFE> maquina)
            : base(ESTADOS_JEFE.VULNERABLE, dueno, maquina)
        {
        }

        public override void OnEnter() => Dueno.EntrarEnVulnerable();

        public override ESTADOS_JEFE? EvaluarTransiciones()
        {
            if (Dueno.DebeSerDerrotado)
            {
                return ESTADOS_JEFE.DEFEATED;
            }

            return TiempoEnEstado >= Dueno.DuracionVentana
                ? BossController.EstadoDeFase(Dueno.FaseSegunVida)
                : (ESTADOS_JEFE?)null;
        }
    }
}
