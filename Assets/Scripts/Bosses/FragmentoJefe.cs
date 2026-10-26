using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// El fragmento que deja un jefe al caer: en la Centinela Ardiente, el blanco que flota sobre
    /// su ceniza en D4. Aparece cuando la muerte llega a ese fotograma y, cuando el jugador lo
    /// toca, el jefe lo anuncia (<see cref="BossController.OnFragmentoRecogido"/>) y la arena
    /// otorga la recompensa del nivel.
    ///
    /// El fragmento está dibujado en D4, no es un objeto aparte. Al recogerlo, una máscara del
    /// tamaño del fragmento lo borra del sprite y la ceniza se queda. No hace falta un D4 sin
    /// fragmento.
    ///
    /// El del Juez Silente sí va aparte (<see cref="gema"/>): su último fotograma es solo el montón de
    /// la capa, y la gema, el Fragmento de la Culpa suelto, flota encima. Al recogerlo, se apaga.
    ///
    /// El trigger va desde el suelo hasta el fragmento: basta con entrar en la ceniza, sin saltar.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class FragmentoJefe : MonoBehaviour
    {
        [Tooltip("Sprite del jefe, donde está dibujado el fragmento.")]
        [SerializeField] private SpriteRenderer dibujo;

        [Tooltip("Máscara del tamaño del fragmento: al recogerlo, lo borra del sprite.")]
        [SerializeField] private SpriteMask mascara;

        [Tooltip("El fragmento dibujado aparte, si el último fotograma no lo lleva (el Juez): se apaga al recogerlo.")]
        [SerializeField] private SpriteRenderer gema;

        private BossController _jefe;
        private Collider2D _zona;

        public bool Recogido { get; private set; }

        /// <summary>¿Está en la ceniza, esperando al jugador?</summary>
        public bool Disponible => gameObject.activeInHierarchy && !Recogido;

        /// <summary>Centro del fragmento dibujado, en el mundo.</summary>
        public Vector2 Centro => gema != null ? (Vector2)gema.bounds.center
            : mascara != null ? (Vector2)mascara.transform.position : (Vector2)transform.position;

        /// <summary>El fragmento dibujado aparte, o null si va en el dibujo del jefe.</summary>
        public SpriteRenderer Gema => gema;

        private void Awake()
        {
            _jefe = GetComponentInParent<BossController>();
            _zona = GetComponent<Collider2D>();
            _zona.isTrigger = true;
            if (mascara != null)
            {
                mascara.enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            if (Recogido || otro.isTrigger || otro.attachedRigidbody == null || _jefe == null)
            {
                return;
            }

            var jugador = otro.attachedRigidbody.GetComponent<PlayerController>();
            if (jugador != null && jugador.EstaVivo)
            {
                _jefe.RecogerFragmento();
            }
        }

        /// <summary>Aparece sobre la ceniza (lo llama el jefe al llegar la muerte a su último fotograma).</summary>
        internal void Aparecer()
        {
            gameObject.SetActive(true);
            if (_zona == null)
            {
                _zona = GetComponent<Collider2D>();
            }

            _zona.enabled = !Recogido;
        }

        /// <summary>El jugador lo recogió: la máscara lo borra del dibujo.</summary>
        internal void Borrar()
        {
            Recogido = true;
            if (_zona != null)
            {
                _zona.enabled = false;
            }

            if (mascara != null && dibujo != null)
            {
                mascara.enabled = true;
                dibujo.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            }

            if (gema != null)
            {
                gema.enabled = false;
            }
        }
    }
}
