using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Un ataque de jefe como dato (patrón Strategy): cada fase de un <c>JefeData</c> lista qué
    /// patrones repite el jefe, y un mismo patrón sirve a cualquier jefe. Cambiar lo que hace
    /// una fase es cambiar un asset, no el código del jefe.
    ///
    /// Los patrones no guardan estado entre ejecuciones: lo que cambia durante un ataque vive en
    /// la corrutina. El jefe la corta si cambia de fase, abre la ventana vulnerable o es
    /// derrotado, y entonces borra las zonas de daño que el patrón hubiera creado.
    /// </summary>
    public abstract class PatronAtaque : ScriptableObject
    {
        [Tooltip("Nombre para la depuración y la documentación.")]
        public string nombre;

        [TextArea(1, 3)] public string descripcion;

        [Tooltip("Aviso previo (telegrafiado), en segundos: el jugador debe poder reaccionar.")]
        [Min(0f)] public float aviso = 0.6f;

        [Min(0)] public int danio = 1;

        [Tooltip("Color provisional de las zonas de daño (sin VFX, PENDIENTES B5).")]
        public Color color = new Color(1f, 0.45f, 0.1f, 1f);

        /// <summary>Ejecuta el ataque sobre el jefe. Termina cuando el ataque acaba.</summary>
        public abstract IEnumerator Ejecutar(BossController jefe);
    }
}
