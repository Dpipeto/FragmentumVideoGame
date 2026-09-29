using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Enemies.Deteccion
{
    /// <summary>
    /// Enemigo del Miedo (Espectro): solo va a por el jugador cuando este está a oscuras. Si
    /// el jugador se refugia en la luz de una antorcha, el enemigo no se acerca. Traduce a
    /// una regla de juego la idea de que el miedo crece en la incertidumbre.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Detección/En oscuridad", fileName = "DeteccionEnOscuridad")]
    public class DeteccionEnOscuridad : DeteccionData
    {
        public override bool Detecta(in ContextoDeteccion contexto) =>
            PorDefecto(contexto) && !Iluminacion.EstaIluminado(contexto.Objetivo);
    }
}
