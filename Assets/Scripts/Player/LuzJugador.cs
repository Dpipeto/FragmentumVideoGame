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
    ///
    /// El Espectro del Pavor juega con ella: la hace parpadear con su grito (<see cref="Parpadear"/>)
    /// y la encoge un rato cuando Kael golpea una de sus sombras (<see cref="Encoger"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class LuzJugador : MonoBehaviour
    {
        [Tooltip("La luz 2D, hija de Kael y apagada en el prefab.")]
        [SerializeField] private Light2D luz;

        [Tooltip("Anexo A: 1,5.")]
        [SerializeField] private float intensidad = 1.5f;

        private float _radio;
        private float _finEncogida = float.NegativeInfinity;
        private float _finParpadeo = float.NegativeInfinity;

        /// <summary>¿Está encendida?</summary>
        public bool Encendida => luz != null && luz.enabled;

        /// <summary>Radio de la luz ahora, en unidades (encogido, si lo está).</summary>
        public float Radio => luz != null ? luz.pointLightOuterRadius : 0f;

        /// <summary>El radio con el que se encendió, sin encoger.</summary>
        public float RadioNormal => _radio;

        public float Intensidad => intensidad;

        public Light2D Luz => luz;

        /// <summary>¿Está encogida? (Kael golpeó una sombra del Espectro del Pavor.)</summary>
        public bool Encogida => Time.time < _finEncogida;

        /// <summary>¿Está parpadeando? (El grito del Espectro del Pavor.)</summary>
        public bool Parpadeando => Time.time < _finParpadeo;

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

            _radio = Mathf.Max(0.1f, radio);
            luz.pointLightOuterRadius = _radio;
            luz.pointLightInnerRadius = Mathf.Min(luz.pointLightInnerRadius, luz.pointLightOuterRadius);
            luz.intensity = intensidad;
            luz.enabled = true;
        }

        /// <summary>Encoge la luz a <paramref name="fraccion"/> de su radio durante <paramref name="segundos"/>.</summary>
        public void Encoger(float fraccion, float segundos)
        {
            if (luz == null || !luz.enabled)
            {
                return;
            }

            _finEncogida = Time.time + Mathf.Max(0f, segundos);
            luz.pointLightOuterRadius = Mathf.Max(0.1f, _radio * Mathf.Clamp01(fraccion));
            luz.pointLightInnerRadius = Mathf.Min(luz.pointLightInnerRadius, luz.pointLightOuterRadius);
        }

        /// <summary>Hace parpadear la luz durante <paramref name="segundos"/>.</summary>
        public void Parpadear(float segundos)
        {
            if (luz != null && luz.enabled)
            {
                _finParpadeo = Time.time + Mathf.Max(0f, segundos);
            }
        }

        /// <summary>Le devuelve su radio y su intensidad, sin esperar (el combate vuelve a empezar).</summary>
        public void Restablecer()
        {
            _finEncogida = float.NegativeInfinity;
            _finParpadeo = float.NegativeInfinity;
            if (luz != null && luz.enabled)
            {
                luz.pointLightOuterRadius = _radio;
                luz.intensity = intensidad;
            }
        }

        private void Update()
        {
            if (luz == null || !luz.enabled)
            {
                return;
            }

            if (!Encogida && _radio > 0f && !Mathf.Approximately(luz.pointLightOuterRadius, _radio))
            {
                luz.pointLightOuterRadius = _radio;
            }

            // Parpadea: se apaga y se enciende a tirones, y al acabar vuelve a su intensidad.
            luz.intensity = Parpadeando && Mathf.Repeat(Time.time * 11f, 1f) < 0.45f ? intensidad * 0.15f : intensidad;
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
