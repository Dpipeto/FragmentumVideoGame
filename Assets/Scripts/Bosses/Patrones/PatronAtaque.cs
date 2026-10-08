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

        [Tooltip("Dibujar la zona de daño como un rectángulo de color. Apagado cuando el dibujo del jefe ya " +
                 "enseña el golpe (el espadazo, la caída del salto): el aviso es la animación. Encendido mientras " +
                 "el ataque no tenga su propio arte (la onda, las erupciones), para que no haga daño invisible.")]
        public bool dibujarZona = true;

        /// <summary>Ejecuta el ataque sobre el jefe. Termina cuando el ataque acaba.</summary>
        public abstract IEnumerator Ejecutar(BossController jefe);

        /// <summary>
        /// Lo que puede recorrer, como mucho <paramref name="alcance"/>, una zona que sale con su
        /// centro en <paramref name="desdeX"/> hacia <paramref name="direccion"/>, hasta que su
        /// borde delantero llega a la pared de la arena. Las ondas se rompen contra la pared: no la
        /// atraviesan.
        /// </summary>
        protected static float RecorridoEnArena(BossController jefe, float desdeX, int direccion, float medioAncho, float alcance)
        {
            var limites = jefe.Limites;
            var pared = direccion > 0 ? limites.y : limites.x;
            return Mathf.Clamp((pared - desdeX) * direccion - medioAncho, 0f, alcance);
        }
    }
}
