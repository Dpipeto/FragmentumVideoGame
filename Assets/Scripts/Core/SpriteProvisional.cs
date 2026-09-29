using UnityEngine;

namespace Fragmentum.Core
{
    /// <summary>
    /// Placeholder para arte que todavía no existe («construir para ausencia»). Si el
    /// SpriteRenderer llega sin sprite, pone un rectángulo de color sólido del tamaño indicado
    /// y avisa por consola nombrando el asset que falta. Nunca se confunde con arte final.
    ///
    /// Si el sprite está asignado, no hace nada: se puede dejar puesto sin coste.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteProvisional : MonoBehaviour
    {
        [Tooltip("Qué arte falta. Aparece en el aviso de consola; debe coincidir con PENDIENTES.md.")]
        [SerializeField] private string assetPendiente = "Sprite pendiente";

        [SerializeField] private Color color = new Color(1f, 0f, 1f, 1f);

        [Tooltip("Tamaño del rectángulo en unidades.")]
        [SerializeField] private Vector2 tamano = Vector2.one;

        private static Sprite _cuadrado;

        private void Awake()
        {
            var renderer2d = GetComponent<SpriteRenderer>();
            if (renderer2d.sprite != null)
            {
                return;
            }

            renderer2d.sprite = Cuadrado();
            renderer2d.drawMode = SpriteDrawMode.Sliced;
            renderer2d.size = tamano;
            renderer2d.color = color;

            Debug.LogWarning(
                $"[Placeholder] '{name}': falta el arte «{assetPendiente}». Se muestra un rectángulo de color " +
                "en su lugar. Ver PENDIENTES.md.", this);
        }

        /// <summary>
        /// Sprite blanco de 1×1 unidad, generado una vez en memoria (no es un asset). Lo usan
        /// también las zonas de daño de los jefes, que aún no tienen VFX.
        /// </summary>
        public static Sprite Cuadrado()
        {
            if (_cuadrado != null)
            {
                return _cuadrado;
            }

            var textura = Texture2D.whiteTexture;
            _cuadrado = Sprite.Create(
                textura,
                new Rect(0f, 0f, textura.width, textura.height),
                new Vector2(0.5f, 0.5f),
                textura.width,
                0,
                SpriteMeshType.FullRect,
                Vector4.zero);
            _cuadrado.name = "Placeholder";
            return _cuadrado;
        }

        private void OnDrawGizmos()
        {
            var renderer2d = GetComponent<SpriteRenderer>();
            if (renderer2d != null && renderer2d.sprite == null)
            {
                Gizmos.color = color;
                Gizmos.DrawWireCube(transform.position, tamano);
            }
        }
    }
}
