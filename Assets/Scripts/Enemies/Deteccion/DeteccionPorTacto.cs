using UnityEngine;

namespace Fragmentum.Enemies.Deteccion
{
    /// <summary>
    /// Nota al jugador solo cuando lo tiene encima: dentro de su radio, que es corto. No ve, así
    /// que la invisibilidad no sirve contra él. Es lo que le queda al controlador del Centinela
    /// Ciego y del Acechador (Miedo), cuyo oído lleva su propia pieza (<c>CentinelaCiego</c>,
    /// <c>Acechador</c>): el roce, y el mordisco o la estocada de cerca.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Detección/Por tacto", fileName = "DeteccionPorTacto")]
    public class DeteccionPorTacto : DeteccionData
    {
        public override bool Detecta(in ContextoDeteccion contexto) => contexto.EnRadio;
    }
}
