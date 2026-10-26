using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// La rama podrida del mundo del Miedo: las «plataformas que ceden» del Anexo A
    /// (<c>FalsePlatform</c>). Lo que la hace ceder es una <see cref="PlataformaInestable"/>, la del
    /// Nivel 1, con otros tiempos: al pisarla cruje 0,3 s y se parte, y vuelve a los 4 s. Este
    /// componente le pone lo que tiene de Miedo:
    ///
    /// - <b>A oscuras es igual que las demás ramas.</b> Bajo la luz de una antorcha se delata: se ve
    ///   gris (y, con su arte, podrida: <see cref="spritePodrida"/>).
    /// - <b>Cruje:</b> mientras está agrietada, el dibujo tiembla (y, con arte, se ve podrida). Es el
    ///   aviso para saltar ya.
    /// - Con arte, <b>al partirse</b> se dobla por la mitad y se deshace en astillas.
    /// </summary>
    [RequireComponent(typeof(PlataformaInestable))]
    [DisallowMultipleComponent]
    public class RamaPodrida : MonoBehaviour
    {
        [Tooltip("El dibujo. Sin arte, el rectángulo de la rama.")]
        [SerializeField] private SpriteRenderer visual;

        [Tooltip("Su color a oscuras: el de las ramas sanas.")]
        [SerializeField] private Color colorSana = new Color(0.43f, 0.33f, 0.22f, 1f);

        [Tooltip("Su color bajo la luz de una antorcha.")]
        [SerializeField] private Color colorPodrida = new Color(0.45f, 0.45f, 0.47f, 1f);

        [Tooltip("Lo que tiembla el dibujo mientras cruje, en unidades.")]
        [SerializeField] private float temblor = 0.05f;

        [Header("Con arte (vacíos: el rectángulo cambia de color)")]
        [Tooltip("La rama sana: como se ve a oscuras.")]
        [SerializeField] private Sprite spriteSana;

        [Tooltip("La rama podrida: como se ve bajo una antorcha y mientras cruje.")]
        [SerializeField] private Sprite spritePodrida;

        [Tooltip("Partiéndose: la primera mitad de su caída.")]
        [SerializeField] private Sprite spriteRota;

        [Tooltip("Las astillas: el resto de su caída.")]
        [SerializeField] private Sprite spriteAstillas;

        private PlataformaInestable _plataforma;
        private BoxCollider2D _cuerpo;
        private Vector3 _posicionDibujo;
        private ESTADOS_PLATAFORMA _estadoAnterior;
        private float _rotaDesde;

        /// <summary>¿La alumbra una antorcha? Entonces se ve que está podrida.</summary>
        public bool Delatada { get; private set; }

        public PlataformaInestable Plataforma => _plataforma;

        public Color ColorSana => colorSana;

        public Color ColorPodrida => colorPodrida;

        /// <summary>¿Lleva su arte? Entonces lo que cambia es el dibujo, no el color.</summary>
        public bool ConArte => spriteSana != null && spritePodrida != null && spriteRota != null && spriteAstillas != null;

        private void Awake()
        {
            _plataforma = GetComponent<PlataformaInestable>();
            _cuerpo = GetComponent<BoxCollider2D>();
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (visual != null)
            {
                _posicionDibujo = visual.transform.localPosition;
            }
        }

        private void Update()
        {
            Delatada = Alumbrada();
            if (visual == null)
            {
                return;
            }

            var estado = _plataforma.Estado;
            if (estado == ESTADOS_PLATAFORMA.BROKEN && _estadoAnterior != ESTADOS_PLATAFORMA.BROKEN)
            {
                _rotaDesde = Time.time;
            }

            _estadoAnterior = estado;
            if (ConArte)
            {
                visual.color = Color.white;
                visual.sprite = DibujoDe(estado, Delatada, Time.time - _rotaDesde, _plataforma.duracionCaida);
            }
            else
            {
                visual.color = Delatada ? colorPodrida : colorSana;
            }

            // Cruje: el dibujo tiembla mientras está agrietada; el collider no se mueve.
            var cruje = estado == ESTADOS_PLATAFORMA.CRACKED;
            var desplazamiento = cruje ? new Vector3(Mathf.Sin(Time.time * 90f) * temblor, 0f, 0f) : Vector3.zero;
            visual.transform.localPosition = _posicionDibujo + desplazamiento;
        }

        /// <summary>
        /// El dibujo que le toca con arte: entera, la sana a oscuras y la podrida con luz; crujiendo,
        /// la podrida; partida, la rota la primera mitad de su caída y las astillas después.
        /// </summary>
        public Sprite DibujoDe(ESTADOS_PLATAFORMA estado, bool delatada, float desdeQueSeRompio, float caida)
        {
            switch (estado)
            {
                case ESTADOS_PLATAFORMA.CRACKED:
                    return spritePodrida;
                case ESTADOS_PLATAFORMA.BROKEN:
                    return desdeQueSeRompio < caida * 0.5f ? spriteRota : spriteAstillas;
                default:
                    return delatada ? spritePodrida : spriteSana;
            }
        }

        // Se mira su superficie en tres puntos: el centro y cerca de cada punta.
        private bool Alumbrada()
        {
            var centro = (Vector2)transform.position;
            var mitad = _cuerpo != null ? _cuerpo.size.x * Mathf.Abs(transform.lossyScale.x) * 0.5f - 0.1f : 0f;
            return Iluminacion.EstaIluminado(centro) ||
                   Iluminacion.EstaIluminado(centro + new Vector2(-mitad, 0f)) ||
                   Iluminacion.EstaIluminado(centro + new Vector2(mitad, 0f));
        }
    }
}
