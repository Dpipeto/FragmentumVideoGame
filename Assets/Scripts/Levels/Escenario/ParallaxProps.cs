using UnityEngine;

namespace Fragmentum.Levels.Escenario
{
    /// <summary>
    /// Parallax para capas compuestas de sprites sueltos (props), no de texturas repetibles.
    /// Mueve los Transforms en lugar de desplazar el offset del material, así que sirve para hitos
    /// únicos, como la fortaleza o la catarata, que no se pueden repetir sin que se note el patrón,
    /// y para tramos de una capa (un sprite en modo Tiled, como el muro).
    ///
    /// Convive con <see cref="ParallaxMovement"/>: ese se encarga de las capas repetibles y este
    /// de las de props. Las dos usan las cuentas de <see cref="CalculoParallax"/>.
    ///
    /// Cada hija se <b>ancla</b> en la X donde está colocada: se ve centrada en pantalla cuando la
    /// cámara pasa por ahí. Así se coloca en el editor donde se quiere ver, y sale en el mismo
    /// sitio aunque la partida empiece en un checkpoint. Para repartir los hitos según la longitud
    /// del nivel está <see cref="RepartoHitos"/>.
    ///
    /// USO: cuelga este componente de un GameObject vacío, mete dentro todos los sprites de esa
    /// capa de profundidad y ajusta <see cref="velocidad"/>.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ParallaxProps : MonoBehaviour
    {
        [Header("Profundidad")]
        [Tooltip("0 = infinitamente lejos (la capa viaja con la cámara y parece no moverse). " +
                 "1 = al mismo nivel que el jugador (la capa queda fija en el mundo). " +
                 "Usa los mismos valores que las capas repetibles a la misma Z: " +
                 "cielo 0.0 / fondo lejano 0.2 / fortaleza 0.4 / muro y catarata 0.6 / primer plano 0.8")]
        [Range(0f, 1f)]
        public float velocidad = 0.4f;

        [Tooltip("Seguir a la cámara en vertical, como las capas repetibles: la Y local de cada hija es " +
                 "su altura en la pantalla (0 = centro). Desactívalo para dejar la capa fija en Y.")]
        public bool seguirCamaraEnY = true;

        [Tooltip("Alto de la vista (2 × tamaño ortográfico) para el que se colocó la capa. Con otra cámara " +
                 "la capa se escala para encajar con las repetibles, que siempre ocupan la vista. 0 = no escalar.")]
        [Min(0f)]
        public float altoReferencia = 9.7f;

        private Camera _camara;
        private Transform[] _hijas = new Transform[0];
        private float[] _anclas = new float[0];
        private Vector3 _escalaAutor = Vector3.one;
        private bool _anclada;

        /// <summary>Número de hijas que mueve.</summary>
        public int Hitos => _hijas.Length;

        /// <summary>X de la cámara a la que esa hija se ve centrada en pantalla.</summary>
        public float Ancla(int hija) => _anclas[hija];

        private void Awake() => Anclar();

        private void Start()
        {
            _camara = Camera.main;
            if (_camara == null)
            {
                Debug.LogError("[Parallax] No hay ninguna cámara con el tag MainCamera: la capa no se moverá.", this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (_camara != null)
            {
                Aplicar(_camara);
            }
        }

        /// <summary>
        /// Toma como ancla la X actual de cada hija. Lo llama <c>Awake</c>, antes de que nada se
        /// mueva; las herramientas del editor lo llaman a mano.
        /// </summary>
        public void Anclar()
        {
            _escalaAutor = transform.localScale;
            _hijas = new Transform[transform.childCount];
            _anclas = new float[_hijas.Length];
            for (var i = 0; i < _hijas.Length; i++)
            {
                _hijas[i] = transform.GetChild(i);
                _anclas[i] = _hijas[i].position.x;
            }

            _anclada = true;
        }

        /// <summary>Coloca la capa según la cámara. Lo llama <c>LateUpdate</c> cada fotograma.</summary>
        public void Aplicar(Camera camara)
        {
            if (!_anclada)
            {
                Anclar();
            }

            var posicion = camara.transform.position;
            var escala = altoReferencia > 0f && camara.orthographic ? camara.orthographicSize * 2f / altoReferencia : 1f;
            transform.localScale = _escalaAutor * escala;
            if (seguirCamaraEnY)
            {
                transform.position = new Vector3(transform.position.x, posicion.y, transform.position.z);
            }

            for (var i = 0; i < _hijas.Length; i++)
            {
                var hija = _hijas[i];
                if (hija == null)
                {
                    continue;
                }

                var actual = hija.position;
                hija.position = new Vector3(CalculoParallax.PosicionX(_anclas[i], posicion.x, velocidad), actual.y, actual.z);
            }
        }
    }
}
