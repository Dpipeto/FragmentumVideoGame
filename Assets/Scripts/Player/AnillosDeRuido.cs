using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Los anillos de ruido (las «partículas de ruido» del Anexo A, mundo del Miedo): cada ruido de
    /// Kael (<see cref="PlayerModoMovimiento.OnRuido"/>) dibuja en el suelo, donde sonó, un anillo
    /// pálido que crece hasta donde se oye y se desvanece. Así el jugador ve que corriendo hace
    /// ruido, y hasta dónde llega: el de la carrera (6 u) y el chasquido de un cepo (8 u).
    ///
    /// Se dibujan con líneas, sin imagen, y no les afecta la luz: se ven también a oscuras. Solo
    /// hay ruido donde las reglas del nivel piden sigilo, así que fuera del Miedo no aparecen.
    /// </summary>
    [DisallowMultipleComponent]
    public class AnillosDeRuido : MonoBehaviour
    {
        private const float RadioInicial = 0.3f;

        [SerializeField] private PlayerModoMovimiento modo;

        [Tooltip("Un material que no reacciona a la luz (Sprite-Unlit-Default).")]
        [SerializeField] private Material material;

        [SerializeField] private Color color = new Color(0.85f, 0.9f, 1f, 0.5f);

        [Tooltip("Segundos que tarda un anillo en llegar a su radio y desvanecerse.")]
        [Min(0.05f)] [SerializeField] private float duracion = 0.6f;

        [Min(0.005f)] [SerializeField] private float grosor = 0.05f;

        [Range(12, 128)] [SerializeField] private int segmentos = 48;

        [SerializeField] private int ordenDibujo = 30;

        private readonly List<Anillo> _anillos = new List<Anillo>();
        private Vector3[] _puntos;
        private bool _avisadoSinMaterial;

        private sealed class Anillo
        {
            public LineRenderer Linea;
            public Vector2 Centro;
            public float Radio;
            public float Inicio;
            public bool Activo;
        }

        /// <summary>Anillos que se ven ahora mismo.</summary>
        public int Activos
        {
            get
            {
                var cuenta = 0;
                foreach (var anillo in _anillos)
                {
                    if (anillo.Activo) cuenta++;
                }

                return cuenta;
            }
        }

        /// <summary>El radio más grande de los anillos que se ven (0 si no hay).</summary>
        public float RadioMayor
        {
            get
            {
                var mayor = 0f;
                foreach (var anillo in _anillos)
                {
                    if (anillo.Activo) mayor = Mathf.Max(mayor, anillo.Radio);
                }

                return mayor;
            }
        }

        public float Duracion => duracion;

        /// <summary>Lo que mide un anillo a la fracción <paramref name="t"/> (0 a 1) de su vida: crece deprisa y frena al llegar.</summary>
        public static float RadioEn(float t, float radioFinal)
        {
            t = Mathf.Clamp01(t);
            var salida = 1f - (1f - t) * (1f - t);
            return Mathf.Lerp(Mathf.Min(RadioInicial, radioFinal), radioFinal, salida);
        }

        private void Awake()
        {
            if (modo == null)
            {
                modo = GetComponent<PlayerModoMovimiento>();
            }
        }

        private void OnEnable()
        {
            if (modo != null)
            {
                modo.OnRuido += AlOir;
            }
        }

        private void OnDisable()
        {
            if (modo != null)
            {
                modo.OnRuido -= AlOir;
            }

            foreach (var anillo in _anillos)
            {
                Apagar(anillo);
            }
        }

        private void AlOir(Vector2 posicion, float radio)
        {
            if (material == null)
            {
                if (!_avisadoSinMaterial)
                {
                    _avisadoSinMaterial = true;
                    Debug.LogWarning("[Jugador] Los anillos de ruido no tienen material: no se dibujan. Regenera el prefab del jugador.", this);
                }

                return;
            }

            var anillo = Libre();
            anillo.Centro = posicion;
            anillo.Radio = radio;
            anillo.Inicio = Time.time;
            anillo.Activo = true;
            anillo.Linea.enabled = true;
            Dibujar(anillo, 0f);
        }

        private void Update()
        {
            foreach (var anillo in _anillos)
            {
                if (!anillo.Activo)
                {
                    continue;
                }

                var t = (Time.time - anillo.Inicio) / duracion;
                if (t >= 1f)
                {
                    Apagar(anillo);
                    continue;
                }

                Dibujar(anillo, t);
            }
        }

        private Anillo Libre()
        {
            foreach (var anillo in _anillos)
            {
                if (!anillo.Activo)
                {
                    return anillo;
                }
            }

            var objeto = new GameObject($"AnilloDeRuido{_anillos.Count}");
            // Fuera de Kael (en la escena de Kael): el anillo se queda donde sonó, no le sigue.
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(objeto, gameObject.scene);
            var linea = objeto.AddComponent<LineRenderer>();
            linea.sharedMaterial = material;
            linea.useWorldSpace = true;
            linea.loop = true;
            linea.positionCount = segmentos;
            linea.widthMultiplier = grosor;
            linea.numCapVertices = 0;
            linea.sortingOrder = ordenDibujo;
            linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            linea.receiveShadows = false;
            linea.enabled = false;

            var nuevo = new Anillo { Linea = linea };
            _anillos.Add(nuevo);
            return nuevo;
        }

        private void Dibujar(Anillo anillo, float t)
        {
            if (_puntos == null || _puntos.Length != segmentos)
            {
                _puntos = new Vector3[segmentos];
            }

            var radio = RadioEn(t, anillo.Radio);
            for (var i = 0; i < segmentos; i++)
            {
                var angulo = i * Mathf.PI * 2f / segmentos;
                _puntos[i] = new Vector3(anillo.Centro.x + Mathf.Cos(angulo) * radio, anillo.Centro.y + Mathf.Sin(angulo) * radio, 0f);
            }

            anillo.Linea.SetPositions(_puntos);
            var alfa = color.a * (1f - t);
            var tinte = new Color(color.r, color.g, color.b, alfa);
            anillo.Linea.startColor = tinte;
            anillo.Linea.endColor = tinte;
        }

        private static void Apagar(Anillo anillo)
        {
            anillo.Activo = false;
            if (anillo.Linea != null)
            {
                anillo.Linea.enabled = false;
            }
        }

        private void OnDestroy()
        {
            foreach (var anillo in _anillos)
            {
                if (anillo.Linea != null)
                {
                    Destroy(anillo.Linea.gameObject);
                }
            }

            _anillos.Clear();
        }
    }
}
