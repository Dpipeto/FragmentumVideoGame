using UnityEngine;

namespace Fragmentum.Enemies.Deteccion
{
    /// <summary>
    /// Centinela Ciego (Miedo): no ve, oye. Detecta al jugador si lo oyó correr hace poco
    /// (el ruido lo emite la región de sigilo del jugador) o si lo tiene pegado. Caminar
    /// despacio es silencioso, y la invisibilidad no sirve contra él: no ve de todos modos.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Detección/Por ruido", fileName = "DeteccionPorRuido")]
    public class DeteccionPorRuido : DeteccionData
    {
        [Tooltip("Segundos que el enemigo recuerda el último ruido oído.")]
        [SerializeField] private float memoria = 2f;

        [Tooltip("A esta distancia nota al jugador aunque no haga ruido (lo roza).")]
        [SerializeField] private float radioTacto = 1.2f;

        public float Memoria => memoria;

        public override bool Detecta(in ContextoDeteccion contexto)
        {
            if (!contexto.ObjetivoVivo)
            {
                return false;
            }

            var oyoRuido = contexto.Ahora - contexto.TiempoUltimoRuido <= memoria;
            return oyoRuido || contexto.Distancia <= radioTacto;
        }
    }
}
