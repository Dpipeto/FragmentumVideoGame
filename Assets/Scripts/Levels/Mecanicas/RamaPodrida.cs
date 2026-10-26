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
    ///   gris (y, con su arte, agrietada).
    /// - <b>Cruje:</b> mientras está agrietada, el dibujo tiembla. Es el aviso para saltar ya.
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

        private PlataformaInestable _plataforma;
        private BoxCollider2D _cuerpo;
        private Vector3 _posicionDibujo;

        /// <summary>¿La alumbra una antorcha? Entonces se ve que está podrida.</summary>
        public bool Delatada { get; private set; }

        public PlataformaInestable Plataforma => _plataforma;

        public Color ColorSana => colorSana;

        public Color ColorPodrida => colorPodrida;

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

            visual.color = Delatada ? colorPodrida : colorSana;

            // Cruje: el dibujo tiembla mientras está agrietada; el collider no se mueve.
            var cruje = _plataforma.Estado == ESTADOS_PLATAFORMA.CRACKED;
            var desplazamiento = cruje ? new Vector3(Mathf.Sin(Time.time * 90f) * temblor, 0f, 0f) : Vector3.zero;
            visual.transform.localPosition = _posicionDibujo + desplazamiento;
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
