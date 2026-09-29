using System;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Plataforma que se desmorona (<c>PlatformCrumble</c> del Anexo A, mundo de la Ira): cuando
    /// el jugador se apoya encima, tiembla un momento, cae y, al rato, vuelve a su sitio.
    ///
    /// Detecta al jugador con una caja fina justo encima de su superficie, no con colisiones: así
    /// no depende del orden de los contactos ni de si el jugador llega saltando o caminando.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlataformaInestable : MonoBehaviour
    {
        [Tooltip("Segundos que aguanta con el jugador encima antes de caer.")]
        [Min(0f)]
        [SerializeField] private float aguante = 0.6f;

        [Tooltip("Segundos que tarda en volver a su sitio tras caer.")]
        [Min(0.1f)]
        [SerializeField] private float reaparicion = 3f;

        [Tooltip("Amplitud del temblor de aviso, en unidades.")]
        [SerializeField] private float temblor = 0.06f;

        [Tooltip("Opcional: el dibujo que tiembla y se oculta.")]
        [SerializeField] private SpriteRenderer visual;

        private BoxCollider2D _cuerpo;
        private Bounds _limites;
        private Vector3 _posicionVisual;
        private readonly Collider2D[] _encima = new Collider2D[2];
        private ContactFilter2D _filtroJugador;
        private float _pisadaDesde = -1f;
        private float _vuelveEn = -1f;

        /// <summary>Cambió de estado: true = se cayó, false = volvió.</summary>
        public event Action<bool> OnCaida;

        public bool Caida { get; private set; }

        /// <summary>¿Tiene al jugador encima ahora mismo?</summary>
        public bool Pisada => _pisadaDesde >= 0f;

        private void Awake()
        {
            _cuerpo = GetComponent<BoxCollider2D>();
            // Se calculan ahora y a mano: los límites de un collider desactivado valen cero, y si la
            // plataforma se acaba de crear la física aún no los ha actualizado.
            var escala = transform.lossyScale;
            _limites = new Bounds(transform.TransformPoint(_cuerpo.offset), new Vector3(_cuerpo.size.x * Mathf.Abs(escala.x), _cuerpo.size.y * Mathf.Abs(escala.y), 1f));
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            if (visual != null)
            {
                _posicionVisual = visual.transform.localPosition;
            }

            _filtroJugador = new ContactFilter2D();
            _filtroJugador.SetLayerMask(Capas.MascaraJugador);
            _filtroJugador.useTriggers = false;
        }

        private void FixedUpdate()
        {
            if (Caida)
            {
                // No vuelve mientras el jugador esté justo donde reaparecería: lo empujaría.
                if (Time.time >= _vuelveEn && !Hay(_limites.center, _limites.size))
                {
                    Recomponer();
                }

                return;
            }

            if (JugadorEncima())
            {
                if (_pisadaDesde < 0f)
                {
                    _pisadaDesde = Time.time;
                }

                if (Time.time - _pisadaDesde >= aguante)
                {
                    Caer();
                }
            }
            else
            {
                _pisadaDesde = -1f;
            }
        }

        private void Update()
        {
            if (visual == null || Caida)
            {
                return;
            }

            // Tiembla mientras aguanta al jugador: es el aviso de que va a caer.
            visual.transform.localPosition = Pisada
                ? _posicionVisual + new Vector3(Mathf.Sin(Time.time * 60f) * temblor, 0f, 0f)
                : _posicionVisual;
        }

        private bool JugadorEncima() =>
            Hay(new Vector2(_limites.center.x, _limites.max.y + 0.06f), new Vector2(_limites.size.x * 0.95f, 0.1f));

        private bool Hay(Vector2 centro, Vector2 tamano) => Physics2D.OverlapBox(centro, tamano, 0f, _filtroJugador, _encima) > 0;

        private void Caer()
        {
            Caida = true;
            _pisadaDesde = -1f;
            _vuelveEn = Time.time + reaparicion;
            _cuerpo.enabled = false;
            if (visual != null)
            {
                visual.enabled = false;
                visual.transform.localPosition = _posicionVisual;
            }

            OnCaida?.Invoke(true);
        }

        private void Recomponer()
        {
            Caida = false;
            _cuerpo.enabled = true;
            if (visual != null)
            {
                visual.enabled = true;
            }

            OnCaida?.Invoke(false);
        }
    }
}
