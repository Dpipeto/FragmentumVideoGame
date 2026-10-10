using System.Collections.Generic;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>Un instante de Kael: dónde estaba su dibujo, qué fotograma mostraba y hacia dónde miraba.</summary>
    public readonly struct MuestraEco
    {
        public MuestraEco(Vector2 posicion, Sprite sprite, bool volteado)
        {
            Posicion = posicion;
            Sprite = sprite;
            Volteado = volteado;
        }

        public Vector2 Posicion { get; }
        public Sprite Sprite { get; }
        public bool Volteado { get; }
    }

    /// <summary>
    /// Los últimos segundos de un intento, en un búfer circular: al llenarse, cada muestra nueva
    /// pisa la más vieja. Sin Unity, para probarla sin escena.
    /// </summary>
    public sealed class GrabacionIntento
    {
        private readonly MuestraEco[] _muestras;
        private int _primera;

        public GrabacionIntento(int capacidad)
        {
            _muestras = new MuestraEco[Mathf.Max(1, capacidad)];
        }

        public int Capacidad => _muestras.Length;

        public int Cantidad { get; private set; }

        public void Grabar(in MuestraEco muestra)
        {
            if (Cantidad < _muestras.Length)
            {
                _muestras[(_primera + Cantidad) % _muestras.Length] = muestra;
                Cantidad++;
                return;
            }

            _muestras[_primera] = muestra;
            _primera = (_primera + 1) % _muestras.Length;
        }

        /// <summary>La muestra <paramref name="i"/>, de la más vieja (0) a la más nueva.</summary>
        public MuestraEco Muestra(int i) => _muestras[(_primera + i) % _muestras.Length];

        public void Limpiar()
        {
            _primera = 0;
            Cantidad = 0;
        }

        /// <summary>Las muestras en orden, de la más vieja a la más nueva.</summary>
        public List<MuestraEco> Copiar()
        {
            var copia = new List<MuestraEco>(Cantidad);
            for (var i = 0; i < Cantidad; i++)
            {
                copia.Add(Muestra(i));
            }

            return copia;
        }
    }

    /// <summary>
    /// Los ecos de intentos del mundo de la Tristeza (<c>EchoRecorder</c> del Anexo A): al
    /// reaparecer tras una muerte, una sombra de Kael repite durante <see cref="duracion"/> s los
    /// últimos segundos del intento que acabó mal. No daña ni ayuda: no tiene colisiones. Es la
    /// rumiación, verse repetir lo que dolió.
    ///
    /// Va en la escena del nivel (lo coloca el preparador en los niveles de la Tristeza), busca al
    /// jugador por el <see cref="LevelManager"/> de su escena y no necesita arte: la sombra usa los
    /// mismos fotogramas que Kael, translúcidos.
    /// </summary>
    public class EcoDeIntentos : MonoBehaviour
    {
        [Tooltip("Segundos que repite la sombra: los últimos del intento.")]
        [Min(1f)] [SerializeField] private float duracion = 8f;

        [Tooltip("Muestras por segundo que se graban de Kael.")]
        [Range(10, 60)] [SerializeField] private int muestrasPorSegundo = 30;

        [Tooltip("Color de la sombra (el alfa es su opacidad).")]
        [SerializeField] private Color color = new Color(0.55f, 0.75f, 1f, 0.35f);

        [Tooltip("Segundos que tarda en desvanecerse al acabar.")]
        [Min(0f)] [SerializeField] private float desvanecer = 0.5f;

        [Tooltip("Orden de dibujo: justo por detrás de Kael (10).")]
        [SerializeField] private int ordenDibujo = 9;

        private LevelManager _nivel;
        private PlayerController _jugador;
        private SpriteRenderer _dibujoJugador;
        private GrabacionIntento _grabacion;
        private List<MuestraEco> _intento;
        private float _acumulado;

        private SpriteRenderer _sombra;
        private int _reproduciendo = -1;
        private float _tiempoSombra;

        public float Duracion => duracion;

        /// <summary>¿Se ve la sombra ahora mismo?</summary>
        public bool Reproduciendo => _sombra != null && _sombra.gameObject.activeSelf;

        /// <summary>La sombra (para las pruebas): null hasta la primera muerte.</summary>
        public SpriteRenderer Sombra => _sombra;

        /// <summary>Muestras que guarda el último intento terminado (0 si aún no hubo ninguno).</summary>
        public int MuestrasDelUltimoIntento => _intento != null ? _intento.Count : 0;

        private int Capacidad => Mathf.CeilToInt(duracion * muestrasPorSegundo) + 1;

        private void Start()
        {
            foreach (var gestor in FindObjectsByType<LevelManager>(FindObjectsInactive.Exclude))
            {
                if (gestor.gameObject.scene == gameObject.scene)
                {
                    _nivel = gestor;
                    break;
                }
            }

            if (_nivel == null)
            {
                Debug.LogWarning("[Ecos] No hay LevelManager en la escena: no hay ecos de intentos.", this);
                enabled = false;
                return;
            }

            _grabacion = new GrabacionIntento(Capacidad);
            _nivel.OnJugadorReaparecio += AlReaparecer;
            Enganchar(_nivel.Jugador);
        }

        private void OnDestroy()
        {
            if (_nivel != null)
            {
                _nivel.OnJugadorReaparecio -= AlReaparecer;
            }

            Enganchar(null);
        }

        private void Enganchar(PlayerController jugador)
        {
            if (_jugador != null && _jugador.Vida != null)
            {
                _jugador.Vida.OnMuerte -= AlMorir;
            }

            _jugador = jugador;
            _dibujoJugador = null;
            if (_jugador == null)
            {
                return;
            }

            var visual = _jugador.GetComponent<PlayerVisual>();
            _dibujoJugador = visual != null ? visual.Dibujo : _jugador.GetComponentInChildren<SpriteRenderer>();
            if (_jugador.Vida != null)
            {
                _jugador.Vida.OnMuerte += AlMorir;
            }
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f)
            {
                return;
            }

            Grabar();
            Reproducir();
        }

        private void Grabar()
        {
            if (_jugador == null || _dibujoJugador == null || !_jugador.EstaVivo)
            {
                return;
            }

            _acumulado += Time.deltaTime;
            var paso = 1f / muestrasPorSegundo;
            if (_acumulado < paso)
            {
                return;
            }

            _acumulado %= paso;
            var dibujo = _dibujoJugador.transform;
            _grabacion.Grabar(new MuestraEco(dibujo.position, _dibujoJugador.sprite, (dibujo.lossyScale.x < 0f) ^ _dibujoJugador.flipX));
        }

        // Al morir se guarda el intento; al reaparecer se repite y se empieza a grabar el nuevo.
        private void AlMorir()
        {
            _intento = _grabacion.Copiar();
            _grabacion.Limpiar();
        }

        private void AlReaparecer(PlayerController jugador)
        {
            if (jugador != _jugador)
            {
                Enganchar(jugador);
            }

            _grabacion.Limpiar();
            _acumulado = 0f;
            if (_intento == null || _intento.Count == 0)
            {
                return;
            }

            Sombra_().gameObject.SetActive(true);
            _reproduciendo = 0;
            _tiempoSombra = 0f;
            Mostrar(_intento[0], color.a);
        }

        private void Reproducir()
        {
            if (_reproduciendo < 0 || _intento == null)
            {
                return;
            }

            _tiempoSombra += Time.deltaTime;
            var indice = Mathf.FloorToInt(_tiempoSombra * muestrasPorSegundo);
            if (indice < _intento.Count)
            {
                Mostrar(_intento[indice], color.a);
                return;
            }

            // Se queda quieta en el último instante del intento y se desvanece.
            var pasado = _tiempoSombra - _intento.Count / (float)muestrasPorSegundo;
            if (pasado < desvanecer)
            {
                Mostrar(_intento[_intento.Count - 1], color.a * (1f - pasado / Mathf.Max(0.01f, desvanecer)));
                return;
            }

            _reproduciendo = -1;
            _sombra.gameObject.SetActive(false);
        }

        private void Mostrar(in MuestraEco muestra, float alfa)
        {
            _sombra.transform.position = new Vector3(muestra.Posicion.x, muestra.Posicion.y, 0f);
            _sombra.sprite = muestra.Sprite;
            _sombra.flipX = muestra.Volteado;
            _sombra.color = new Color(color.r, color.g, color.b, alfa);
        }

        private SpriteRenderer Sombra_()
        {
            if (_sombra == null)
            {
                var objeto = new GameObject("SombraDelIntento");
                objeto.transform.SetParent(transform, false);
                _sombra = objeto.AddComponent<SpriteRenderer>();
                _sombra.sortingOrder = ordenDibujo;
            }

            // El mismo material, capa y tamaño que el dibujo de Kael (el volteo va en cada muestra).
            if (_dibujoJugador != null)
            {
                _sombra.sharedMaterial = _dibujoJugador.sharedMaterial;
                _sombra.sortingLayerID = _dibujoJugador.sortingLayerID;
                var escala = _dibujoJugador.transform.lossyScale;
                _sombra.transform.localScale = new Vector3(Mathf.Abs(escala.x), Mathf.Abs(escala.y), 1f);
            }

            return _sombra;
        }
    }
}
