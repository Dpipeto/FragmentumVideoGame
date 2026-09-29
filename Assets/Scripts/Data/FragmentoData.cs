using UnityEngine;

namespace Fragmentum.Data
{
    /// <summary>
    /// Un fragmento de identidad: lo que Kael integra al superar cada mundo. Es la pieza
    /// narrativa de la progresión (la pantalla «yo reconstruido» los muestra) y la llave de
    /// una habilidad.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Fragmento", fileName = "Fragmento")]
    public class FragmentoData : ScriptableObject
    {
        [Tooltip("Emoción de la que procede; es también su identificador en el progreso.")]
        public EMOCIONES id;

        public string nombre;

        [TextArea(2, 4)] public string descripcion;

        [Tooltip("Habilidad que desbloquea. Vacío en el fragmento de la Esperanza, que integra los demás.")]
        public HabilidadData habilidad;

        public Sprite icono;
    }
}
