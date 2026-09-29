namespace Fragmentum.Bosses
{
    /// <summary>
    /// El Verdadero Soldado, jefe final. <b>Esqueleto documentado</b>: tres fases (100-60 %,
    /// 60-30 % y 30-0 %) con el flujo completo de <see cref="BossController"/>.
    ///
    /// Ya implementado: no se le derrota quitándole la vida, sino con la integración
    /// (<see cref="CondicionDerrota"/>). Es la tesis hecha mecánica: el combate termina sin
    /// violencia. Para ello su vida nunca baja de 1 mientras no se complete la integración.
    ///
    /// Pendiente según el Anexo A (Fase 10):
    /// - Fase 1: combate espejo, que copia el estilo del jugador. Patrón propuesto:
    ///   <c>PatronEspejo</c>, que lee el historial de estados del jugador.
    /// - Fase 2: invoca secciones de cada mundo (lava, hielo, oscuridad, memorias).
    /// - Fase 3: deja de atacar (su fase no tiene patrones) y empieza la secuencia de plataformeo
    ///   hacia el fragmento de luz, que al alcanzarse llama a <see cref="CompletarIntegracion"/>.
    /// </summary>
    public class VerdaderoSoldado : BossController
    {
        private bool _integracionCompletada;

        public bool IntegracionCompletada => _integracionCompletada;

        /// <summary>Kael alcanzó el fragmento de luz: el combate termina.</summary>
        public void CompletarIntegracion() => _integracionCompletada = true;

        protected override bool CondicionDerrota() => _integracionCompletada;

        protected override void AlActivarse()
        {
            Vida.OnVidaCambiada += MantenerConVida;
        }

        protected override void AlReiniciarse()
        {
            Vida.OnVidaCambiada -= MantenerConVida;
            _integracionCompletada = false;
        }

        // La vida baja hasta 1 y ahí se queda: marca el paso a la fase 3, no la derrota.
        private void MantenerConVida(int actual, int maxima)
        {
            if (actual <= 0 && !_integracionCompletada)
            {
                Vida.Revivir(1);
            }
        }
    }
}
