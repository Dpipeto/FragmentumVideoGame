using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Fragmentum.Player
{
    /// <summary>
    /// La luz propia de Kael en los niveles a oscuras (Anexo A, mundo del Miedo: un círculo de 3 u e
    /// intensidad 1,5). Es una luz 2D hija de Kael, apagada salvo en los niveles cuyas reglas piden
    /// oscuridad: la enciende el <c>LevelManager</c> con el radio de <c>LevelData.reglas</c>.
    ///
    /// No se registra en <see cref="Fragmentum.Core.Iluminacion"/>: con ella Kael ve dónde pisa, pero
    /// no está a salvo de los Espectros, que solo respetan la luz de las antorchas.
    /// </summary>
    [DisallowMultipleComponent]
    public class LuzJugador : MonoBehaviour
    {
        [Tooltip("La luz 2D, hija de Kael y apagada en el prefab.")]
        [SerializeField] private Light2D luz;

        [Tooltip("Anexo A: 1,5.")]
        [SerializeField] private float intensidad = 1.5f;

        /// <summary>¿Está encendida?</summary>
        public bool Encendida => luz != null && luz.enabled;

        /// <summary>Radio de la luz, en unidades.</summary>
        public float Radio => luz != null ? luz.pointLightOuterRadius : 0f;

        public float Intensidad => intensidad;

        public Light2D Luz => luz;

        private void Awake()
        {
            if (luz == null)
            {
                luz = GetComponentInChildren<Light2D>(true);
            }

            if (luz == null)
            {
                Debug.LogWarning("[Jugador] Kael no tiene su luz 2D: en los niveles a oscuras no verá nada. Regenera el prefab del jugador.", this);
            }
        }

        /// <summary>Enciende la luz de Kael con ese radio.</summary>
        public void Encender(float radio)
        {
            if (luz == null)
            {
                return;
            }

            luz.pointLightOuterRadius = Mathf.Max(0.1f, radio);
            luz.pointLightInnerRadius = Mathf.Min(luz.pointLightInnerRadius, luz.pointLightOuterRadius);
            luz.intensity = intensidad;
            luz.enabled = true;
        }

        public void Apagar()
        {
            if (luz != null)
            {
                luz.enabled = false;
            }
        }
    }
}
