using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Espectro del Pavor, jefe del mundo del Miedo. <b>Esqueleto documentado</b>: compila, tiene
    /// datos y el flujo completo de <see cref="BossController"/>; sus fases aún no tienen patrones.
    ///
    /// Ya implementado: es casi invisible durante el combate (<see cref="AlActivarse"/>), como pide
    /// el Anexo A. El aviso previo a cada ataque (<c>Avisar</c>) sí se ve con claridad.
    ///
    /// Pendiente (Fase 10):
    /// - Fase 1: ataques con 1 s de destello de aviso. El aviso ya existe; falta un patrón que lo
    ///   use con un golpe.
    /// - Fase 2 (50 %): 3 clones, uno real, que se distingue por una partícula de luz. Patrón
    ///   propuesto: <c>PatronClones</c>, con clones sin vida que desaparecen al golpearlos.
    /// - Muerte: la oscuridad retrocede y el bosque se ilumina (<see cref="BossController.AlSerDerrotado"/>).
    /// </summary>
    public class EspectroDelPavor : BossController
    {
        [Tooltip("Opacidad del espectro mientras no ataca.")]
        [Range(0f, 1f)]
        [SerializeField] private float opacidadOculto = 0.25f;

        protected override void AlActivarse()
        {
            var color = ColorBase;
            ColorBase = new Color(color.r, color.g, color.b, opacidadOculto);
        }
    }
}
