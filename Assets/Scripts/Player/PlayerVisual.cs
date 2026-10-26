using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Presentación del jugador: hacia dónde mira, parpadeo durante los i-frames,
    /// transparencia de la Invisibilidad Parcial y tinte de la carga completa.
    ///
    /// Solo lee el estado de los demás componentes; no cambia nada de la lógica. El giro se
    /// hace sobre un hijo (<see cref="pivote"/>) que contiene el sprite y la espada, así que la
    /// raíz con el Rigidbody no cambia de escala y el wall jump no depende del sprite.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerVisual : MonoBehaviour
    {
        [SerializeField] private PlayerController jugador;
        [SerializeField] private PlayerHealth vida;
        [SerializeField] private PlayerAbilities habilidades;

        [Tooltip("Hijo que se voltea al girar: contiene el sprite y la espada.")]
        [SerializeField] private Transform pivote;

        [SerializeField] private SpriteRenderer sprite;

        [Header("Efectos")]
        [Tooltip("Parpadeos por segundo mientras dura la invulnerabilidad.")]
        [SerializeField] private float frecuenciaParpadeo = 14f;

        [SerializeField, Range(0f, 1f)] private float alfaParpadeo = 0.25f;

        [Tooltip("Opacidad durante la Invisibilidad Parcial (Anexo: placeholder sin arte, alfa 0,4).")]
        [SerializeField, Range(0f, 1f)] private float alfaInvisible = 0.4f;

        [Tooltip("Tinte mientras la carga del Ataque Cargado está completa.")]
        [SerializeField] private Color tinteCarga = new Color(1f, 0.55f, 0.45f, 1f);

        private Color _colorBase = Color.white;

        /// <summary>El dibujo de Kael (lo copian, por ejemplo, los ecos de intentos de la Tristeza).</summary>
        public SpriteRenderer Dibujo => sprite != null ? sprite : sprite = GetComponentInChildren<SpriteRenderer>();

        private void Awake()
        {
            if (jugador == null) jugador = GetComponent<PlayerController>();
            if (vida == null) vida = GetComponent<PlayerHealth>();
            if (habilidades == null) habilidades = GetComponent<PlayerAbilities>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (pivote == null && sprite != null) pivote = sprite.transform;
        }

        // En Start, no en Awake: el color base puede fijarlo antes un SpriteProvisional.
        private void Start()
        {
            if (sprite != null)
            {
                _colorBase = sprite.color;
            }
        }

        private void LateUpdate()
        {
            if (jugador != null && pivote != null)
            {
                var escala = pivote.localScale;
                escala.x = Mathf.Abs(escala.x) * jugador.Mirando;
                pivote.localScale = escala;
            }

            if (sprite == null)
            {
                return;
            }

            var color = _colorBase;
            if (habilidades != null && habilidades.CargaCompleta)
            {
                color *= tinteCarga;
            }

            var alfa = _colorBase.a;
            if (habilidades != null && habilidades.EsInvisible)
            {
                alfa *= alfaInvisible;
            }

            if (vida != null && vida.EstaVivo && vida.EsInvulnerable &&
                Mathf.Repeat(Time.time * frecuenciaParpadeo, 1f) < 0.5f)
            {
                alfa *= alfaParpadeo;
            }

            color.a = alfa;
            sprite.color = color;
        }
    }
}
