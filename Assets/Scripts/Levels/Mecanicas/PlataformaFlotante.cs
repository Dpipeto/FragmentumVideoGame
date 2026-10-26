using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Placa de hielo flotante (Anexo A, mundo de la Tristeza): sube y baja en un seno vertical
    /// alrededor de donde se colocó. Las placas vecinas van desfasadas, así que cruzar es esperar
    /// el momento, y como son de hielo (llevan su franja de <see cref="ZonaModificadorMovimiento"/>)
    /// llegar corriendo hace resbalar al agua.
    ///
    /// Es un cuerpo cinemático que se mueve con <c>MovePosition</c>: al subir empuja a Kael, y al
    /// bajar despacio (como mucho 2πA/T, 1,3 u/s con los valores del diseño) Kael la sigue por su
    /// propio peso sin despegarse. El pivote está en la superficie, como el de las demás mecánicas.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlataformaFlotante : MonoBehaviour
    {
        [Tooltip("Lo que sube y baja desde donde se colocó, en unidades.")]
        [Min(0f)] public float amplitud = 0.6f;

        [Tooltip("Segundos de una oscilación completa.")]
        [Min(0.5f)] public float periodo = 3f;

        [Tooltip("Segundos de adelanto en el ciclo: con medio periodo, dos placas van a la contra.")]
        public float desfase;

        private Rigidbody2D _cuerpo;
        private Vector2 _base;
        private float _inicio;

        /// <summary>Donde se colocó: el centro de la oscilación.</summary>
        public Vector2 Base => _base;

        /// <summary>El desplazamiento vertical en el segundo <paramref name="t"/> del ciclo.</summary>
        public static float Desplazamiento(float t, float amplitud, float periodo, float desfase) =>
            amplitud * Mathf.Sin(2f * Mathf.PI * (t + desfase) / Mathf.Max(0.01f, periodo));

        /// <summary>La velocidad vertical más alta: al cruzar el centro.</summary>
        public static float VelocidadMaxima(float amplitud, float periodo) => 2f * Mathf.PI * amplitud / Mathf.Max(0.01f, periodo);

        private void Awake()
        {
            _cuerpo = GetComponent<Rigidbody2D>();
            _cuerpo.bodyType = RigidbodyType2D.Kinematic;
            _cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            _base = transform.position;
            _inicio = Time.time;
        }

        private void FixedUpdate()
        {
            var t = Time.time - _inicio;
            _cuerpo.MovePosition(new Vector2(_base.x, _base.y + Desplazamiento(t, amplitud, periodo, desfase)));
        }

        private void OnDrawGizmosSelected()
        {
            var centro = Application.isPlaying ? (Vector3)_base : transform.position;
            Gizmos.color = new Color(0.6f, 0.85f, 1f, 0.8f);
            Gizmos.DrawLine(centro + Vector3.up * amplitud, centro - Vector3.up * amplitud);
        }
    }
}
