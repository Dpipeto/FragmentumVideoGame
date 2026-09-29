namespace Fragmentum.Bosses
{
    /// <summary>
    /// Doncella del Lamento, jefe del mundo de la Tristeza. <b>Esqueleto documentado</b>: compila,
    /// tiene datos (vida y dos fases) y usa todo el flujo de <see cref="BossController"/>, pero sus
    /// fases aún no tienen patrones, así que espera sin atacar.
    ///
    /// Pendiente según el Anexo A (Fase 10):
    /// - Fase 1: olas de agua (proyectiles horizontales) e inundación temporal de zonas del suelo.
    ///   Patrones propuestos: <c>PatronOlas</c> y <c>PatronInundacion</c>.
    /// - Fase 2 (50 %): congela secciones, que crean zonas de daño y plataformas nuevas.
    ///   Patrón propuesto: <c>PatronCongelar</c>.
    /// - Muerte: el hielo se derrite y la ciudad emerge un instante (<see cref="AlSerDerrotado"/>).
    /// - La arena es resbaladiza: se resuelve con la zona de hielo que ya existe.
    /// </summary>
    public class DoncellaLamento : BossController
    {
    }
}
