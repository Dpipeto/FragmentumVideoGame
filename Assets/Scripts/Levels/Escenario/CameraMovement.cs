using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Levels.Escenario
{
    /// <summary>
    /// Cámara que sigue al jugador con suavizado y sin salirse de los límites del nivel.
    ///
    /// Corrige el <c>CameraMovement</c> del prototipo, que exigía asignar el jugador a mano y
    /// no tenía límites, así que enseñaba el vacío fuera del mapa. Los límites se aplican al
    /// área visible (no solo al centro), teniendo en cuenta el tamaño ortográfico y la
    /// proporción de pantalla.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraMovement : MonoBehaviour
    {
        [Tooltip("Objetivo a seguir. Vacío = el objeto con el tag Player.")]
        [SerializeField] private Transform objetivo;

        [Tooltip("Desplazamiento respecto del objetivo. Z debe ser negativa para ver la escena.")]
        [SerializeField] private Vector3 desplazamiento = new Vector3(0f, 1.5f, -10f);

        [Tooltip("Segundos aproximados que tarda en alcanzar al objetivo.")]
        [SerializeField] private float suavizado = 0.15f;

        [Header("Límites del área visible")]
        [SerializeField] private bool limitar = true;
        [SerializeField] private Vector2 minimo = new Vector2(-20f, -8f);
        [SerializeField] private Vector2 maximo = new Vector2(100f, 30f);

        private Camera _camara;
        private Vector3 _velocidad;
        private bool _avisado;

        private void Awake()
        {
            _camara = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (objetivo == null && !BuscarObjetivo())
            {
                return;
            }

            var deseada = objetivo.position + desplazamiento;
            if (limitar)
            {
                deseada = Limitar(deseada);
            }

            transform.position = Vector3.SmoothDamp(transform.position, deseada, ref _velocidad, suavizado);
        }

        /// <summary>Coloca la cámara sobre el objetivo sin suavizado (al reaparecer o cargar).</summary>
        public void Encuadrar()
        {
            if (objetivo == null && !BuscarObjetivo())
            {
                return;
            }

            var deseada = objetivo.position + desplazamiento;
            transform.position = limitar ? Limitar(deseada) : deseada;
            _velocidad = Vector3.zero;
        }

        /// <summary>Cambia el objetivo (el LevelManager lo usa con el jugador que hace aparecer).</summary>
        public void FijarObjetivo(Transform nuevo)
        {
            objetivo = nuevo;
        }

        public void FijarLimites(Vector2 nuevoMinimo, Vector2 nuevoMaximo)
        {
            minimo = nuevoMinimo;
            maximo = nuevoMaximo;
            limitar = true;
        }

        private Vector3 Limitar(Vector3 posicion)
        {
            var medioAlto = _camara != null && _camara.orthographic ? _camara.orthographicSize : 0f;
            var medioAncho = _camara != null ? medioAlto * _camara.aspect : 0f;

            posicion.x = LimitarEje(posicion.x, minimo.x + medioAncho, maximo.x - medioAncho);
            posicion.y = LimitarEje(posicion.y, minimo.y + medioAlto, maximo.y - medioAlto);
            return posicion;
        }

        // Si el nivel es más pequeño que la pantalla en ese eje, se centra.
        private static float LimitarEje(float valor, float desde, float hasta) =>
            desde > hasta ? (desde + hasta) * 0.5f : Mathf.Clamp(valor, desde, hasta);

        private bool BuscarObjetivo()
        {
            var jugador = GameObject.FindGameObjectWithTag(GameTags.Player);
            if (jugador != null)
            {
                objetivo = jugador.transform;
                return true;
            }

            if (!_avisado)
            {
                _avisado = true;
                Debug.LogWarning(
                    "[Cámara] No hay objetivo asignado ni ningún objeto con el tag Player. La cámara queda fija.",
                    this);
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (!limitar)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            var centro = (minimo + maximo) * 0.5f;
            Gizmos.DrawWireCube(centro, maximo - minimo);
        }
    }
}
