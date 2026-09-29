using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Plataforma de combustión (<c>HotPlatform</c> del Anexo A, mundo de la Ira): se puede pisar,
    /// pero si el jugador se queda encima más de 2 s, quema. Se va poniendo al rojo mientras tanto,
    /// para que se vea venir. Al bajarse, se enfría.
    ///
    /// Es la mecánica de la emoción del mundo: la Ira no deja quedarse quieto.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlataformaCaliente : MonoBehaviour
    {
        [Tooltip("Segundos encima antes de quemar (Anexo A: 2 s).")]
        [Min(0.1f)]
        [SerializeField] private float aguante = 2f;

        [Tooltip("Daño de cada quemadura. Mientras se siga encima, vuelve a quemar al pasar los i-frames.")]
        [Min(1)]
        [SerializeField] private int danio = 1;

        [SerializeField] private SpriteRenderer visual;

        [SerializeField] private Color colorFrio = new Color(0.36f, 0.25f, 0.2f);
        [SerializeField] private Color colorAlRojo = new Color(1f, 0.35f, 0.05f);

        private BoxCollider2D _cuerpo;
        private Bounds _limites;
        private readonly Collider2D[] _encima = new Collider2D[2];
        private ContactFilter2D _filtroJugador;
        private float _calor;

        /// <summary>De 0 (fría) a 1 (quema).</summary>
        public float Calor => _calor;

        private void Awake()
        {
            _cuerpo = GetComponent<BoxCollider2D>();
            var escala = transform.lossyScale;
            _limites = new Bounds(transform.TransformPoint(_cuerpo.offset), new Vector3(_cuerpo.size.x * Mathf.Abs(escala.x), _cuerpo.size.y * Mathf.Abs(escala.y), 1f));
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            _filtroJugador = new ContactFilter2D();
            _filtroJugador.SetLayerMask(Capas.MascaraJugador);
            _filtroJugador.useTriggers = false;
        }

        private void FixedUpdate()
        {
            var jugador = JugadorEncima();
            _calor = jugador != null
                ? Mathf.Min(1f, _calor + Time.fixedDeltaTime / aguante)
                : Mathf.Max(0f, _calor - Time.fixedDeltaTime / aguante * 2f);

            if (jugador != null && _calor >= 1f)
            {
                var vida = jugador.attachedRigidbody != null ? jugador.attachedRigidbody.GetComponent<IGolpeable>() : null;
                vida?.RecibirGolpe(new Golpe(danio, new Vector2(transform.position.x, _limites.min.y), gameObject));
            }

            if (visual != null)
            {
                visual.color = Color.Lerp(colorFrio, colorAlRojo, _calor);
            }
        }

        private Collider2D JugadorEncima()
        {
            var centro = new Vector2(_limites.center.x, _limites.max.y + 0.06f);
            var tamano = new Vector2(_limites.size.x * 0.95f, 0.1f);
            return Physics2D.OverlapBox(centro, tamano, 0f, _filtroJugador, _encima) > 0 ? _encima[0] : null;
        }
    }
}
