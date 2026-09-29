using Fragmentum.Core.FSM;
using Fragmentum.Data;

namespace Fragmentum.Bosses.States
{
    /// <summary>
    /// PHASE_1, PHASE_2 y PHASE_3: una sola clase instanciada tres veces (plan, §3.5). Al entrar
    /// arranca en bucle los patrones de su <see cref="FaseJefe"/>; al salir los corta.
    ///
    /// Pasa a la fase siguiente cuando la vida baja de su umbral, a VULNERABLE cuando se pide la
    /// ventana y a DEFEATED cuando se cumple la condición de derrota del jefe.
    /// </summary>
    public sealed class BossPhaseState : EstadoBase<ESTADOS_JEFE, BossController>
    {
        private readonly int _fase;

        public BossPhaseState(ESTADOS_JEFE id, int fase, BossController dueno, MaquinaEstados<ESTADOS_JEFE> maquina)
            : base(id, dueno, maquina)
        {
            _fase = fase;
        }

        public override void OnEnter() => Dueno.EntrarEnFase(_fase);

        public override void OnExit() => Dueno.DetenerAtaques();

        public override ESTADOS_JEFE? EvaluarTransiciones()
        {
            if (Dueno.DebeSerDerrotado)
            {
                return ESTADOS_JEFE.DEFEATED;
            }

            if (Dueno.ConsumirVentana())
            {
                return ESTADOS_JEFE.VULNERABLE;
            }

            // De una en una: si un golpe se lleva dos umbrales, la fase 3 llega en el frame siguiente.
            if (_fase < JefeData.MaxFases - 1 && Dueno.FaseSegunVida > _fase)
            {
                return BossController.EstadoDeFase(_fase + 1);
            }

            return null;
        }
    }
}
