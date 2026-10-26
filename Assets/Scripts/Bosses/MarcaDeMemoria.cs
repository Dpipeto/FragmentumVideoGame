using Fragmentum.Core;
using Fragmentum.Levels;
using Fragmentum.Levels.Mecanicas;
using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Una de las cuatro marcas del suelo de la plaza del Juez Silente (<c>Nivel4_Diseno.md</c>,
    /// sección 6): la de una memoria (la radio, la campana, la placa o la carta). Mientras el Juez
    /// ataca con esa memoria, su marca brilla; si Kael se pone encima y pulsa Interactuar (W), mira lo
    /// que el Juez le enseña y el Juez se arrodilla (<see cref="JuezSilente.AlMirar"/>). En la segunda
    /// fase brilla también una trampa.
    ///
    /// <b>Para recordar hay que dejarse ver</b>: invisible, el aviso dice
    /// «<see cref="MemoriaInteractuable.AvisoInvisible"/>» y la marca no se activa.
    /// </summary>
    [DisallowMultipleComponent]
    public class MarcaDeMemoria : MonoBehaviour, IInteractuable
    {
        [Tooltip("El Juez al que se le muestra lo que se mira.")]
        [SerializeField] private JuezSilente juez;

        [Tooltip("Qué memoria es (0 la orden, 1 el pueblo, 2 el compañero, 3 la carta).")]
        [Range(0, 3)] public int memoria;

        [Tooltip("Hasta dónde llega Kael para mirarla, desde el centro de la marca.")]
        [Min(0.1f)] public float distancia = 1f;

        [Tooltip("La marca en el suelo. Sin arte, un rectángulo del color de su memoria.")]
        [SerializeField] private SpriteRenderer dibujo;

        [SerializeField] private Color color = new Color(0.88f, 0.69f, 0.38f, 1f);

        [Tooltip("Apagada, la marca casi no se ve.")]
        [SerializeField, Range(0f, 1f)] private float opacidadApagada = 0.2f;

        private SeguimientoJugador _kael;

        /// <summary>¿Brilla ahora? (Su memoria es la del ataque, o es la trampa.)</summary>
        public bool Brilla { get; private set; }

        /// <summary>¿Se queda encendida? (El Juez cayó: todas las memorias se iluminan.)</summary>
        public bool Fija { get; private set; }

        public JuezSilente Juez => juez;

        public SpriteRenderer Dibujo => dibujo;

        public Color Color => color;

        private bool KaelInvisible
        {
            get
            {
                var jugador = _kael != null ? _kael.Jugador : null;
                return jugador != null && jugador.Habilidades != null && jugador.Habilidades.EsInvisible;
            }
        }

        // --- IInteractuable ---

        public Vector2 PuntoInteraccion => transform.position;

        public float RadioInteraccion => distancia;

        public bool Disponible => Brilla && !Fija && enabled && gameObject.activeInHierarchy;

        public string Accion => KaelInvisible ? MemoriaInteractuable.AvisoInvisible : "Mirar";

        public bool Usable => !KaelInvisible;

        public void Interactuar(GameObject quien)
        {
            if (KaelInvisible || juez == null)
            {
                return;
            }

            juez.AlMirar(this);
        }

        /// <summary>Se enciende: su memoria es la del ataque (o la trampa).</summary>
        public void Encender()
        {
            Brilla = true;
            Pintar();
        }

        public void Apagar()
        {
            Brilla = false;
            Pintar();
        }

        /// <summary>Se queda encendida para siempre (el Juez cayó).</summary>
        public void Fijar()
        {
            Fija = true;
            Brilla = true;
            Pintar();
        }

        private void Awake()
        {
            _kael = new SeguimientoJugador(null);
            Pintar();
        }

        private void OnEnable() => Interacciones.Registrar(this);

        private void OnDisable()
        {
            Interacciones.Quitar(this);
            _kael?.Soltar();
        }

        private void Update()
        {
            if (Brilla && !Fija && dibujo != null)
            {
                // Late, como las memorias del camino: es lo que hay que mirar.
                var latido = 0.75f + 0.25f * Mathf.Sin(Time.time * 6f);
                dibujo.color = new Color(color.r, color.g, color.b, latido);
            }
        }

        private void Pintar()
        {
            if (dibujo != null)
            {
                dibujo.color = new Color(color.r, color.g, color.b, Brilla ? 1f : opacidadApagada);
            }
        }
    }
}
