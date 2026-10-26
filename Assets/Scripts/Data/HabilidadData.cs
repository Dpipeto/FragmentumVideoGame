using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Data
{
    /// <summary>
    /// Descripción de una habilidad desbloqueable. La lógica vive en <c>PlayerAbilities</c>;
    /// este asset aporta lo que ve el jugador (nombre, texto, ícono) y el identificador que
    /// se guarda en el progreso.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Habilidad", fileName = "Habilidad")]
    public class HabilidadData : ScriptableObject
    {
        public HABILIDADES id;
        public string nombre;

        [TextArea(2, 4)] public string descripcion;

        [Tooltip("Teclas, para la pantalla «Mecánicas». Las acciones van entre llaves y se leen del asset de input: " +
                 "«Mantén {Attack}» → «Mantén CLIC / ENTER». Sin llaves, el texto tal cual («Pasiva»).")]
        public string controles;

        [Tooltip("Segundos de efecto (0 = permanente o instantánea).")]
        public float duracion;

        [Tooltip("Segundos de espera entre usos (0 = sin espera).")]
        public float cooldown;

        public Sprite icono;
    }
}
