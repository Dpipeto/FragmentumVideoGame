using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// El dibujo de la salida de un nivel: una puerta que se ve desde que se entra en la arena,
    /// cerrada, y que se abre cuando la <see cref="SalidaNivel"/> queda disponible (al caer el
    /// jefe y recoger su fragmento). Antes la salida era un rectángulo translúcido que aparecía
    /// de golpe.
    ///
    /// Cuatro dibujos: cerrada, entreabierta y dos de abierta, que se alternan despacio para que
    /// la luz del mundo siguiente parezca viva. No decide nada: solo enseña lo que le dice la salida.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PuertaDeSalida : MonoBehaviour
    {
        [SerializeField] private Sprite cerrada;
        [SerializeField] private Sprite entreabierta;

        [Tooltip("Los dibujos de la puerta abierta, que se alternan.")]
        [SerializeField] private Sprite[] abierta = new Sprite[0];

        [Tooltip("Segundos que se ve entreabierta antes de abrirse del todo.")]
        [Min(0f)]
        [SerializeField] private float duracionApertura = 0.45f;

        [Tooltip("Segundos que dura cada dibujo de la puerta abierta.")]
        [Min(0.05f)]
        [SerializeField] private float ritmoAbierta = 0.5f;

        private SpriteRenderer _dibujo;
        private float _abiertaDesde = float.NegativeInfinity;

        /// <summary>¿Se le ha dicho que se abra?</summary>
        public bool Abierta { get; private set; }

        /// <summary>El dibujo que enseña ahora.</summary>
        public Sprite DibujoActual => Dibujo.sprite;

        public Sprite DibujoCerrada => cerrada;

        public Sprite DibujoEntreabierta => entreabierta;

        public int DibujosAbierta => abierta != null ? abierta.Length : 0;

        private SpriteRenderer Dibujo => _dibujo != null ? _dibujo : _dibujo = GetComponent<SpriteRenderer>();

        /// <summary>Le da sus dibujos cuando se monta por código.</summary>
        public void Fijar(Sprite puertaCerrada, Sprite puertaEntreabierta, params Sprite[] puertaAbierta)
        {
            cerrada = puertaCerrada;
            entreabierta = puertaEntreabierta;
            abierta = puertaAbierta ?? new Sprite[0];
            Pintar();
        }

        /// <summary>La cierra: es como empieza un nivel con jefe.</summary>
        public void Cerrar()
        {
            Abierta = false;
            Pintar();
        }

        /// <summary>
        /// La abre. <paramref name="alInstante"/>: sin pasar por entreabierta (una salida que está
        /// disponible desde el principio).
        /// </summary>
        public void Abrir(bool alInstante = false)
        {
            if (Abierta)
            {
                return;
            }

            Abierta = true;
            _abiertaDesde = alInstante ? float.NegativeInfinity : Time.time;
            Pintar();
        }

        private void Update()
        {
            if (Abierta)
            {
                Pintar();
            }
        }

        private void Pintar()
        {
            if (!Abierta)
            {
                Dibujo.sprite = cerrada;
                return;
            }

            var tiempo = Time.time - _abiertaDesde;
            if (tiempo < duracionApertura && entreabierta != null)
            {
                Dibujo.sprite = entreabierta;
                return;
            }

            if (abierta == null || abierta.Length == 0)
            {
                Dibujo.sprite = entreabierta != null ? entreabierta : cerrada;
                return;
            }

            var paso = float.IsInfinity(tiempo) ? Time.time : tiempo - duracionApertura;
            Dibujo.sprite = abierta[Mathf.FloorToInt(Mathf.Max(0f, paso) / ritmoAbierta) % abierta.Length];
        }
    }
}
