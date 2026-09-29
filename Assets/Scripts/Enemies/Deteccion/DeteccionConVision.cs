using UnityEngine;

namespace Fragmentum.Enemies.Deteccion
{
    /// <summary>
    /// Como la detección por distancia, pero sin ver a través de muros: traza una línea de
    /// los ojos del enemigo al jugador y, si choca con el suelo o una pared, no lo ve.
    /// En el prototipo los enemigos detectaban al jugador a través de las paredes.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Detección/Con visión", fileName = "DeteccionConVision")]
    public class DeteccionConVision : DeteccionData
    {
        public override bool Detecta(in ContextoDeteccion contexto)
        {
            if (!PorDefecto(contexto))
            {
                return false;
            }

            return !Physics2D.Linecast(contexto.Origen, contexto.Objetivo, contexto.MascaraObstaculos);
        }
    }
}
