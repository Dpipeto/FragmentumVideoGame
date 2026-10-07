using Fragmentum.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Fragmentum.UI
{
    /// <summary>
    /// La cámara de las cinemáticas en pixel art. Es una cámara propia, aparte de la del juego, con
    /// la Pixel Perfect Camera de URP:
    /// - dibuja en una textura de exactamente <see cref="AnchoReferencia"/> × <see cref="AltoReferencia"/>
    ///   píxeles (<i>Upscale Render Texture</i>) y la amplía a la pantalla por el mayor número
    ///   entero que cabe, con filtro Point: 1 píxel del plano son k × k píxeles de pantalla, sin
    ///   interpolar. En 1920 × 1080, k = 8;
    /// - centra la imagen y rellena de negro lo que sobra (<i>Windowbox</i>): en 1280 × 720, k = 5
    ///   y queda un borde negro fino;
    /// - los planos van a 1 píxel por unidad (<see cref="PixelesPorUnidad"/>), igual que la
    ///   cámara: una unidad del mundo es un píxel del plano. Todo lo que se mueva aquí (el
    ///   encuadre, el temblor, las brasas) se mueve en unidades enteras y no sale de la rejilla.
    ///
    /// Vive lejos de cualquier nivel (<see cref="Posicion"/>) y en su propia capa
    /// (<see cref="Capas.Cinematica"/>): la cámara del juego no la ve, y ella solo ve lo suyo. Se
    /// dibuja encima de la del juego y limpia la pantalla a negro, así que no hace falta apagar
    /// ni pelearse con la cámara del juego.
    ///
    /// La crea <see cref="Crear"/>, y el prefab <c>Assets/Prefabs/Cinematicas/CamaraCinematica</c>
    /// se genera con ese mismo método.
    /// </summary>
    [DisallowMultipleComponent]
    public class CamaraCinematica : MonoBehaviour
    {
        /// <summary>Resolución nativa de los planos (16:9 exacto).</summary>
        public const int AnchoReferencia = 240;

        public const int AltoReferencia = 135;

        /// <summary>Píxeles por unidad de los planos y de la cámara: un píxel del plano, una unidad.</summary>
        public const int PixelesPorUnidad = 1;

        /// <summary>Se dibuja después de la cámara del juego.</summary>
        public const float Profundidad = 100f;

        /// <summary>Lejos de cualquier nivel, en coordenadas enteras.</summary>
        public static readonly Vector3 Posicion = new Vector3(0f, -50000f, 0f);

        [SerializeField] private Camera camara;
        [SerializeField] private PixelPerfectCamera pixelPerfect;

        [Tooltip("El plano a pantalla completa.")]
        [SerializeField] private SpriteRenderer plano;

        public Camera Camara => camara;

        public PixelPerfectCamera PixelPerfect => pixelPerfect;

        public SpriteRenderer Plano => plano;

        /// <summary>
        /// Por cuánto se multiplica cada píxel del plano en pantalla. Lo calcula la Pixel Perfect
        /// Camera al dibujar: 0 si todavía no ha dibujado.
        /// </summary>
        public int Escala => pixelPerfect != null ? pixelPerfect.pixelRatio : 0;

        public bool Visible => gameObject.activeSelf;

        /// <summary>Muestra <paramref name="sprite"/> a pantalla completa (o negro, si es null).</summary>
        public void Mostrar(Sprite sprite)
        {
            plano.sprite = sprite;
            plano.enabled = sprite != null;
            gameObject.SetActive(true);
        }

        public void Ocultar() => gameObject.SetActive(false);

        /// <summary>
        /// El rectángulo que ocupa la rejilla en una pantalla de <paramref name="ancho"/> ×
        /// <paramref name="alto"/> píxeles, como lo calcula la Pixel Perfect Camera: k veces la
        /// referencia, centrado con división entera.
        /// </summary>
        public static RectInt RectEnPantalla(int ancho, int alto)
        {
            var k = Mathf.Max(1, Mathf.Min(ancho / AnchoReferencia, alto / AltoReferencia));
            var w = AnchoReferencia * k;
            var h = AltoReferencia * k;
            return new RectInt((ancho - w) / 2, (alto - h) / 2, w, h);
        }

        /// <summary>
        /// Monta la cámara y el plano por código, apagados. <paramref name="material"/> es el del
        /// plano: sin luces 2D, para que el dibujo se vea tal cual (sin él, el de sprites sin luz
        /// de URP).
        /// </summary>
        public static CamaraCinematica Crear(Transform padre, Material material = null)
        {
            var capa = LayerMask.NameToLayer(Capas.Cinematica);
            if (capa < 0)
            {
                Debug.LogWarning($"[Cinemática] Falta la capa '{Capas.Cinematica}' (Fragmentum → Configurar proyecto): la cámara de cinemáticas usa la capa por defecto.");
                capa = 0;
            }

            var raiz = new GameObject("CamaraCinematica") { layer = capa };
            raiz.SetActive(false);
            if (padre != null)
            {
                raiz.transform.SetParent(padre, false);
            }

            raiz.transform.position = Posicion;
            var componente = raiz.AddComponent<CamaraCinematica>();

            var objetoCamara = new GameObject("Camara") { layer = capa };
            objetoCamara.transform.SetParent(raiz.transform, false);
            objetoCamara.transform.localPosition = new Vector3(0f, 0f, -10f);
            var camara = objetoCamara.AddComponent<Camera>();
            camara.orthographic = true;
            camara.orthographicSize = AltoReferencia * 0.5f / PixelesPorUnidad;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Color.black;
            camara.cullingMask = 1 << capa;
            camara.depth = Profundidad;
            camara.allowHDR = false;
            camara.allowMSAA = false;
            camara.nearClipPlane = 0.3f;
            camara.farClipPlane = 100f;

            var datosUrp = objetoCamara.AddComponent<UniversalAdditionalCameraData>();
            datosUrp.renderType = CameraRenderType.Base;
            datosUrp.renderPostProcessing = false;
            datosUrp.antialiasing = AntialiasingMode.None;
            datosUrp.renderShadows = false;

            var pixelPerfect = objetoCamara.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = PixelesPorUnidad;
            pixelPerfect.refResolutionX = AnchoReferencia;
            pixelPerfect.refResolutionY = AltoReferencia;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;

            var objetoPlano = new GameObject("Plano") { layer = capa };
            objetoPlano.transform.SetParent(raiz.transform, false);
            var plano = objetoPlano.AddComponent<SpriteRenderer>();
            plano.enabled = false;
            if (material == null)
            {
                var sombreador = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                material = sombreador != null ? new Material(sombreador) : null;
            }

            if (material != null)
            {
                plano.sharedMaterial = material;
            }

            componente.camara = camara;
            componente.pixelPerfect = pixelPerfect;
            componente.plano = plano;
            return componente;
        }
    }
}
