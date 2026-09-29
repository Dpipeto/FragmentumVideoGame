using UnityEngine;

namespace Fragmentum.Enemies.Deteccion
{
    /// <summary>
    /// Detecta al jugador si está dentro del radio y no es invisible. Es la detección genérica
    /// (la del prototipo, pero sin parpadeo en el borde gracias a la histéresis del controlador).
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Detección/Por distancia", fileName = "DeteccionPorDistancia")]
    public class DeteccionPorDistancia : DeteccionData
    {
        public override bool Detecta(in ContextoDeteccion contexto) => PorDefecto(contexto);
    }
}
