using UnityEngine;

namespace Fragmentum.Data
{
    /// <summary>Las emociones del juego. El identificador es estable: se guarda en el progreso.</summary>
    public enum EMOCIONES
    {
        IRA,
        TRISTEZA,
        MIEDO,
        CULPA,
        ESPERANZA,
        INTEGRACION
    }

    /// <summary>
    /// Una emoción como dato: su nombre canónico, cómo se describe y su color. El nivel, el
    /// selector, los fragmentos y la UI la leen de aquí en vez de repetir textos y colores.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Emoción", fileName = "Emocion")]
    public class EmocionData : ScriptableObject
    {
        public EMOCIONES id;

        [Tooltip("Nombre que ve el jugador, por ejemplo «Ira».")]
        public string nombreCanonico;

        [Tooltip("Cómo se vive esa emoción en el nivel, por ejemplo «impulsividad, rabia reprimida».")]
        public string descriptor;

        public Color color = Color.white;

        [Tooltip("Opcional: ícono para el selector y la pantalla «yo reconstruido».")]
        public Sprite icono;
    }
}
