using System;
using Fragmentum.Core;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// El cepo del mundo del Miedo: la trampa oculta del Anexo A («invisibles hasta 1 u»), hecha cepo
    /// de caza escondido entre las hojas. En la oscuridad, quien corre cae.
    ///
    /// - <b>Solo se ve</b> a <see cref="distanciaVer"/> de Kael (Anexo A: 1 u) o bajo la luz de una
    ///   antorcha. Caminando aparece medio segundo antes de pisarlo; corriendo, no da tiempo.
    /// - <b>Al pisarlo</b> quita <see cref="danio"/> (sin retroceso: lo sujeta, no lo empuja),
    ///   <b>sujeta a Kael</b> <see cref="sujecion"/> s (puede atacar, pero no andar, saltar ni hacer el
    ///   dash) y <b>suena</b>: el chasquido se oye a <see cref="radioRuido"/> u, y lo oyen los mismos
    ///   enemigos que oyen correr.
    /// - Después se queda <b>cerrado</b> e inofensivo, a la vista, hasta que Kael reaparece.
    ///
    /// Nota a Kael con una caja justo encima de su pivote (en el suelo), como la plataforma
    /// inestable: no depende de las capas de colisión. Va después de Kael en el orden de ejecución
    /// para sujetarlo después de que su motor se mueva. Con arte, abierto y cerrado son dos dibujos.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public class Cepo : MonoBehaviour
    {
        [Tooltip("Vida que quita al cerrarse.")]
        [Min(0)] public int danio = 1;

        [Tooltip("Segundos que sujeta a Kael.")]
        [Min(0f)] public float sujecion = 1f;

        [Tooltip("Hasta dónde se oye el chasquido, en unidades.")]
        [Min(0f)] public float radioRuido = 8f;

        [Tooltip("A esta distancia de Kael se ve a oscuras (Anexo A: 1 u).")]
        [Min(0f)] public float distanciaVer = 1f;

        [Tooltip("Lo que ocupa el cepo sobre el suelo (ancho, alto), con el pivote en el centro de su base.")]
        [SerializeField] private Vector2 tamano = new Vector2(0.8f, 0.3f);

        [Tooltip("La parte que cierra: si Kael la pisa, salta. Más estrecha que el cepo.")]
        [SerializeField] private float anchoMandibulas = 0.6f;

        [SerializeField] private SpriteRenderer visual;

        [SerializeField] private Color colorAbierto = new Color(0.55f, 0.55f, 0.6f, 1f);
        [SerializeField] private Color colorCerrado = new Color(0.32f, 0.3f, 0.32f, 1f);

        [Tooltip("Con arte: el cepo abierto y el cepo cerrado. Vacíos, el rectángulo cambia de color.")]
        [SerializeField] private Sprite spriteAbierto;

        [SerializeField] private Sprite spriteCerrado;

        [Tooltip("Lo deprisa que aparece y desaparece (opacidad por segundo).")]
        [SerializeField] private float velocidadFundido = 8f;

        private static readonly ModificadorMovimiento Sujeto = new ModificadorMovimiento(0f, 1f, 0f, 1f, false);

        private readonly Collider2D[] _encima = new Collider2D[2];
        private ContactFilter2D _filtroJugador;
        private SeguimientoJugador _kael;
        private PlayerController _presa;
        private float _sueltaEn;
        private float _xSujeto;
        private float _opacidad;

        /// <summary>Se cerró sobre Kael.</summary>
        public event Action OnCierra;

        public ESTADOS_CEPO Estado { get; private set; }

        /// <summary>¿Tiene a Kael sujeto ahora mismo?</summary>
        public bool Sujetando => _presa != null;

        /// <summary>¿Debería verse ahora? (Cerrado, con luz o con Kael cerca.)</summary>
        public bool SeVe { get; private set; }

        /// <summary>Opacidad de su dibujo ahora mismo (0 a 1).</summary>
        public float Opacidad => _opacidad;

        /// <summary>Lo que ocupa, en el mundo.</summary>
        public Rect Zona => new Rect((Vector2)transform.position + new Vector2(-tamano.x * 0.5f, 0f), tamano);

        /// <summary>La regla de cuándo se ve: cerrado, siempre; abierto, con luz o a <paramref name="distanciaVer"/> de Kael.</summary>
        public static bool SeVeCon(ESTADOS_CEPO estado, bool iluminado, float distanciaAKael, float distanciaVer) =>
            estado == ESTADOS_CEPO.CLOSED || iluminado || distanciaAKael <= distanciaVer;

        /// <summary>Distancia entre dos rectángulos (0 si se tocan).</summary>
        public static float Separacion(Rect a, Rect b)
        {
            var dx = Mathf.Max(0f, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax));
            var dy = Mathf.Max(0f, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private void Awake()
        {
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>(true);
            }

            _filtroJugador = new ContactFilter2D();
            _filtroJugador.SetLayerMask(Capas.MascaraJugador);
            _filtroJugador.useTriggers = false;
            _kael = new SeguimientoJugador(Rearmar);
            Pintar(true);
        }

        private void OnDisable()
        {
            Soltar();
            _kael?.Soltar();
        }

        private void FixedUpdate()
        {
            var kael = _kael.Jugador;
            if (Estado == ESTADOS_CEPO.ARMED)
            {
                if (kael != null && kael.EstaVivo && KaelEncima())
                {
                    Cerrar(kael);
                }

                return;
            }

            if (_presa == null)
            {
                return;
            }

            if (Time.time >= _sueltaEn || !_presa.EstaVivo)
            {
                Soltar();
                return;
            }

            // Sujeto: ni anda ni sale con el dash (el modificador ya le quita andar y saltar).
            var cuerpo = _presa.Motor.Cuerpo;
            if (Mathf.Abs(cuerpo.position.x - _xSujeto) > 0.001f)
            {
                cuerpo.position = new Vector2(_xSujeto, cuerpo.position.y);
            }

            cuerpo.linearVelocity = new Vector2(0f, Mathf.Min(0f, cuerpo.linearVelocity.y));
        }

        private void Update()
        {
            var kael = _kael.Jugador;
            SeVe = SeVeCon(Estado, Iluminacion.EstaIluminado(Zona.center), DistanciaA(kael), distanciaVer);
            _opacidad = Mathf.MoveTowards(_opacidad, SeVe ? 1f : 0f, velocidadFundido * Time.deltaTime);
            Pintar(false);
        }

        /// <summary>Se cierra sobre Kael: le quita vida, lo sujeta y suena.</summary>
        public void Cerrar(PlayerController kael)
        {
            if (Estado != ESTADOS_CEPO.ARMED || kael == null)
            {
                return;
            }

            Estado = ESTADOS_CEPO.CLOSED;
            SeVe = true;
            _opacidad = 1f;
            Pintar(false);

            _presa = kael;
            _sueltaEn = Time.time + sujecion;
            _xSujeto = kael.Motor.Cuerpo.position.x;
            kael.Motor.AgregarModificador(this, Sujeto);

            var modo = kael.GetComponent<PlayerModoMovimiento>();
            if (modo != null)
            {
                modo.HacerRuido(transform.position, radioRuido);
            }

            OnCierra?.Invoke();

            // Lo último: si este era su último corazón, Kael muere aquí y se suelta.
            if (danio > 0 && kael.Vida != null)
            {
                kael.Vida.PerderVida(danio);
            }
        }

        /// <summary>Vuelve a estar abierto y escondido. Lo hace solo cuando Kael reaparece.</summary>
        public void Rearmar()
        {
            Soltar();
            Estado = ESTADOS_CEPO.ARMED;
            SeVe = false;
            _opacidad = 0f;
            Pintar(false);
        }

        private void Soltar()
        {
            if (_presa != null && _presa.Motor != null)
            {
                _presa.Motor.QuitarModificador(this);
            }

            _presa = null;
        }

        private bool KaelEncima()
        {
            var centro = (Vector2)transform.position + new Vector2(0f, tamano.y * 0.5f);
            return Physics2D.OverlapBox(centro, new Vector2(anchoMandibulas, tamano.y), 0f, _filtroJugador, _encima) > 0;
        }

        private float DistanciaA(PlayerController kael)
        {
            if (kael == null)
            {
                return float.MaxValue;
            }

            var cuerpo = kael.GetComponent<Collider2D>();
            if (cuerpo == null)
            {
                return Vector2.Distance(kael.transform.position, transform.position);
            }

            var limites = cuerpo.bounds;
            return Separacion(Zona, new Rect(limites.min, limites.size));
        }

        private void Pintar(bool inicio)
        {
            if (visual == null)
            {
                return;
            }

            var cerrado = Estado == ESTADOS_CEPO.CLOSED;
            if (spriteAbierto != null && spriteCerrado != null)
            {
                visual.sprite = cerrado ? spriteCerrado : spriteAbierto;
            }

            var color = cerrado ? colorCerrado : colorAbierto;
            color.a *= inicio ? 0f : _opacidad;
            visual.color = color;
        }

        private void OnDrawGizmos()
        {
            var zona = Zona;
            Gizmos.color = Estado == ESTADOS_CEPO.CLOSED ? Color.gray : new Color(0.9f, 0.3f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(zona.center, zona.size);
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
            Gizmos.DrawWireCube(zona.center, zona.size + Vector2.one * (2f * distanciaVer));
        }
    }
}
