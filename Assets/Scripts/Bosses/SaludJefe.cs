using System;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Vida de un jefe. Añade a <see cref="Health"/> las reglas de la ventana vulnerable:
    /// - Durante la ventana, el daño se multiplica (<c>JefeData.multiplicadorVulnerable</c>).
    /// - Con <c>inmuneFueraDeVulnerable</c> (Juez Silente), fuera de la ventana no recibe daño.
    /// - Mientras es <see cref="Intangible"/> (el Espectro del Pavor, invisible fuera de sus
    ///   ataques), los golpes lo atraviesan.
    /// - Los golpes letales (lava, vacío) atraviesan las tres reglas.
    ///
    /// El jefe le dice cuándo es vulnerable; esta clase no conoce la FSM.
    /// </summary>
    public class SaludJefe : Health
    {
        /// <summary>¿Está abierta la ventana vulnerable? La fija el BossController.</summary>
        public Func<bool> EsVulnerable { get; set; }

        public float MultiplicadorVulnerable { get; set; } = 1f;

        public bool InmuneFueraDeVulnerable { get; set; }

        /// <summary>¿Lo atraviesan ahora los golpes? La fija el jefe (el Espectro del Pavor). Vacía = nunca.</summary>
        public Func<bool> Intangible { get; set; }

        /// <summary>
        /// La vida que tendría sin la de más (el Juez Silente, si no se miró ninguna memoria). 0 = no
        /// tiene de más. La barra del jefe enseña ese tramo en otro color.
        /// </summary>
        public int VidaBase { get; set; }

        /// <summary>Un golpe no hizo nada porque el jefe era inmune o intangible (para un sonido, por ejemplo).</summary>
        public event Action<Golpe> OnGolpeBloqueado;

        public override bool RecibirGolpe(in Golpe golpe)
        {
            if (golpe.IgnoraInvulnerabilidad || !EstaVivo)
            {
                return base.RecibirGolpe(golpe);
            }

            var vulnerable = EsVulnerable != null && EsVulnerable();
            if ((InmuneFueraDeVulnerable && !vulnerable) || (Intangible != null && Intangible()))
            {
                OnGolpeBloqueado?.Invoke(golpe);
                return false;
            }

            var cantidad = vulnerable ? Mathf.CeilToInt(golpe.Cantidad * Mathf.Max(1f, MultiplicadorVulnerable)) : golpe.Cantidad;
            return base.RecibirGolpe(new Golpe(cantidad, golpe.Origen, golpe.Atacante, golpe.Cargado));
        }
    }
}
