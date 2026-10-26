using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Hace brillar el fragmento que deja un jefe cuando su dibujo es oscuro: el del Miedo es azul
    /// oscuro y, sobre el montón de hojas del Espectro del Pavor, apenas se veía. Encima del
    /// fragmento dibujado va una copia de sus mismos píxeles (recortada de su fotograma: no es arte
    /// nuevo) con el material que recibe luz, y una luz suya que late la enciende y alumbra el suelo
    /// alrededor. La luz 2D no aclara un dibujo por encima de sus colores, así que encima va además
    /// su silueta, blanca lavanda (el material del destello de los golpes), que aparece y se apaga
    /// con el latido: en lo alto, la gema destella. Al recogerlo, todo se apaga y la máscara de
    /// <see cref="FragmentoJefe"/> lo borra del dibujo, como siempre.
    ///
    /// Va en el objeto del fragmento, que aparece cuando la muerte llega a su último fotograma.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FragmentoJefe))]
    public class BrilloDeFragmento : MonoBehaviour
    {
        private static readonly int IdColor = Shader.PropertyToID("_Color");

        [Tooltip("El último fotograma de la muerte del jefe, donde está dibujado el fragmento.")]
        [SerializeField] private Sprite fotograma;

        [Tooltip("El fragmento en ese fotograma, en píxeles (la fila 0 es la de abajo).")]
        [SerializeField] private RectInt caja;

        [Tooltip("El dibujo del jefe: la copia se voltea con él.")]
        [SerializeField] private SpriteRenderer dibujoDelJefe;

        [Tooltip("La copia del fragmento, encima del dibujado, con el material que recibe luz.")]
        [SerializeField] private SpriteRenderer copia;

        [Tooltip("Su silueta, encima de la copia, con el material del destello (una silueta de color liso).")]
        [SerializeField] private SpriteRenderer destello;

        [SerializeField] private Light2D luz;

        [Header("El latido")]
        [Min(0f)] public float intensidadMinima = 2.5f;

        [Min(0f)] public float intensidadMaxima = 6f;

        [Tooltip("Color de la silueta en lo alto del latido.")]
        public Color colorDestello = new Color32(240, 232, 255, 255);

        [Tooltip("Opacidad de la silueta en lo alto del latido (abajo, 0: se ve la gema).")]
        [Range(0f, 1f)] public float destelloMaximo = 0.65f;

        [Tooltip("Segundos de un latido completo.")]
        [Min(0.1f)] public float periodo = 1.4f;

        private FragmentoJefe _fragmento;
        private MaterialPropertyBlock _bloque;

        /// <summary>
        /// ¿Brilla? Mientras el fragmento espera a que lo recojan: no antes de que aparezca (el objeto
        /// sigue apagado) ni después de recogerlo.
        /// </summary>
        public bool Encendido => luz != null && luz.isActiveAndEnabled && copia != null && copia.enabled && copia.gameObject.activeInHierarchy;

        public Light2D Luz => luz;

        public SpriteRenderer Copia => copia;

        public SpriteRenderer Destello => destello;

        /// <summary>De 0 a 1: dónde está el latido (1, en lo alto).</summary>
        public float Latido => (Mathf.Sin(Time.time * 2f * Mathf.PI / periodo) + 1f) * 0.5f;

        private void Awake()
        {
            _fragmento = GetComponent<FragmentoJefe>();
            if (copia != null && fotograma != null && caja.width > 0 && caja.height > 0)
            {
                var rect = fotograma.textureRect;
                copia.sprite = Sprite.Create(fotograma.texture, new Rect(rect.x + caja.x, rect.y + caja.y, caja.width, caja.height),
                    new Vector2(0.5f, 0.5f), fotograma.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                copia.sprite.name = $"{fotograma.name}_Fragmento";
                if (destello != null)
                {
                    destello.sprite = copia.sprite;
                }
            }
        }

        private void Update()
        {
            var brilla = _fragmento == null || !_fragmento.Recogido;
            var t = Latido;
            if (copia != null)
            {
                copia.enabled = brilla && copia.sprite != null;
                if (dibujoDelJefe != null)
                {
                    copia.flipX = dibujoDelJefe.flipX;
                }
            }

            if (destello != null)
            {
                destello.enabled = brilla && destello.sprite != null;
                destello.flipX = copia != null && copia.flipX;
                _bloque ??= new MaterialPropertyBlock();
                destello.GetPropertyBlock(_bloque);
                var color = colorDestello;
                color.a = t * destelloMaximo;
                _bloque.SetColor(IdColor, color);
                destello.SetPropertyBlock(_bloque);
            }

            if (luz != null)
            {
                luz.enabled = brilla;
                luz.intensity = Mathf.Lerp(intensidadMinima, intensidadMaxima, t);
            }
        }

        private void OnDestroy()
        {
            // La silueta comparte el sprite de la copia: se destruye una vez.
            if (copia != null && copia.sprite != null)
            {
                Destroy(copia.sprite);
            }
        }
    }
}
