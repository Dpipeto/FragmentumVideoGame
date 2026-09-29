using UnityEngine;

namespace Fragmentum.Data
{
    /// <summary>
    /// Datos de un jefe (Arquitecto del Alma). El nombre canónico vive aquí y no en escenas
    /// ni scripts (Anexo A: Centinela Ardiente, no Pyrokratos; Juez Silente, no Silencioso).
    ///
    /// En la Fase 5 solo lleva identidad; la Fase 7 añade vida, fases y patrones de ataque.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefe", fileName = "Jefe")]
    public class JefeData : ScriptableObject
    {
        public string nombreCanonico;

        public EmocionData emocion;

        [TextArea(2, 4)] public string descripcion;

        [Tooltip("Arte conceptual de referencia (no es el sprite de juego).")]
        public Sprite concepto;
    }
}
