using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// El fondo de las escenas de menú: una imagen del juego en lugar del color plano de antes.
    /// La imagen y su velo están en la escena (los pone la herramienta que la monta); este
    /// componente añade al arrancar las dos sombras que se generan por código y no se pueden
    /// guardar en una escena: la viñeta y, si se pide, un degradado que oscurece la izquierda
    /// para que el título y las opciones se lean sobre la imagen.
    /// </summary>
    public class FondoMenu : MonoBehaviour
    {
        [Tooltip("Opacidad de la viñeta de los bordes. 0 = sin viñeta.")]
        [Range(0f, 1f)]
        [SerializeField] private float vineta = 0.7f;

        [Tooltip("Opacidad del degradado que oscurece la izquierda, donde va la columna del menú. 0 = sin él.")]
        [Range(0f, 1f)]
        [SerializeField] private float sombraIzquierda = 0.85f;

        [Tooltip("Hasta qué fracción del ancho llega el degradado.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float anchoSombra = 0.62f;

        private static Sprite _degradado;
        private bool _construido;

        /// <summary>¿Están ya puestas las sombras?</summary>
        public bool Construido => _construido;

        /// <summary>Lo ajusta la herramienta que monta la escena.</summary>
        public void Fijar(float opacidadVineta, float opacidadSombra)
        {
            vineta = Mathf.Clamp01(opacidadVineta);
            sombraIzquierda = Mathf.Clamp01(opacidadSombra);
        }

        private void Awake() => Construir();

        /// <summary>Añade las sombras. Solo la primera vez.</summary>
        public void Construir()
        {
            if (_construido)
            {
                return;
            }

            _construido = true;
            if (sombraIzquierda > 0f)
            {
                var sombra = FabricaUI.Panel(transform, "SombraIzquierda", new Color(0f, 0f, 0f, sombraIzquierda), false);
                sombra.sprite = SpriteDegradado();
                sombra.rectTransform.anchorMax = new Vector2(anchoSombra, 1f);
            }

            if (vineta > 0f)
            {
                FabricaUI.Vineta(transform, vineta);
            }
        }

        // Negro opaco a la izquierda que se desvanece hacia la derecha.
        private static Sprite SpriteDegradado()
        {
            if (_degradado != null)
            {
                return _degradado;
            }

            const int ancho = 128;
            var textura = new Texture2D(ancho, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "DegradadoGenerado"
            };

            var pixeles = new Color32[ancho];
            for (var x = 0; x < ancho; x++)
            {
                var alfa = 1f - Mathf.SmoothStep(0f, 1f, x / (ancho - 1f));
                pixeles[x] = new Color32(255, 255, 255, (byte)(alfa * 255f));
            }

            textura.SetPixels32(pixeles);
            textura.Apply(false, true);
            _degradado = Sprite.Create(textura, new Rect(0f, 0f, ancho, 1f), new Vector2(0.5f, 0.5f));
            _degradado.name = "DegradadoGenerado";
            return _degradado;
        }
    }
}
