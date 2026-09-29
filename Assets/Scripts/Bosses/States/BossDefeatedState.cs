using Fragmentum.Core.FSM;

namespace Fragmentum.Bosses.States
{
    /// <summary>
    /// DEFEATED: estado final. Corta los ataques, apaga el daño por contacto y las colisiones,
    /// emite <c>OnJefeDerrotado</c> (la arena lo convierte en fragmento y habilidad) y
    /// desvanece al jefe.
    /// </summary>
    public sealed class BossDefeatedState : EstadoBase<ESTADOS_JEFE, BossController>
    {
        public BossDefeatedState(BossController dueno, MaquinaEstados<ESTADOS_JEFE> maquina)
            : base(ESTADOS_JEFE.DEFEATED, dueno, maquina)
        {
        }

        public override void OnEnter() => Dueno.Derrotar();
    }
}
