using System.Collections.Generic;
using System.Text;
using Fragmentum.Core;
using Fragmentum.Narrative;
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
    ///   cámara: una unidad del mundo es un píxel del plano.
    ///
    /// Todo lo que se mueve aquí sale de cuatro sitios, y todo en la rejilla:
    /// - los fundidos (la opacidad de los planos; ninguno se mueve ni se escala);
    /// - el temblor (<see cref="Temblar"/>): la cámara se desplaza píxeles enteros;
    /// - la capa de brasas (llega con la cinemática del jefe);
    /// - el sonido, que lleva el reproductor.
    ///
    /// El encuadre de cada plano (<see cref="Pintar"/>), los textos (<see cref="TextoEnRejilla"/>)
    /// y el flash también caen en píxeles enteros.
    ///
    /// Vive lejos de cualquier nivel (<see cref="Posicion"/>) y en su propia capa
    /// (<see cref="Capas.Cinematica"/>): la cámara del juego no la ve, y ella solo ve lo suyo. Se
    /// dibuja encima de la del juego y limpia la pantalla a negro, así que no hace falta apagar
    /// ni pelearse con la cámara del juego.
    ///
    /// La crea <see cref="Crear"/>, y el prefab <c>Assets/Prefabs/Cinematicas/CamaraCinematica</c>
    /// se genera con ese mismo método. La maneja <see cref="PantallaCinematica"/>.
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

        // Orden de dibujo dentro de la cámara.
        private const int OrdenAbajo = 0;
        private const int OrdenEncima = 1;
        private const int OrdenTexto = 10;
        private const int OrdenFlash = 20;

        // El temblor recorre estas direcciones, un fotograma cada una: nunca se queda desplazado.
        private static readonly Vector2Int[] Direcciones = { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };

        // Tercios del plano para los textos sin punto propio (arriba, centro, abajo), desde arriba.
        private static readonly int[] FilasTercio = { AltoReferencia / 6, AltoReferencia / 2, AltoReferencia - AltoReferencia / 6 };

        private static readonly HashSet<string> AvisadasSinFuente = new HashSet<string>();
        private static Sprite _blanco;

        [SerializeField] private Camera camara;
        [SerializeField] private PixelPerfectCamera pixelPerfect;

        [Tooltip("El plano a pantalla completa (y el de debajo, en un fundido cruzado).")]
        [SerializeField] private SpriteRenderer plano;

        [Tooltip("El plano que entra encima en un fundido cruzado.")]
        [SerializeField] private SpriteRenderer planoEncima;

        [Tooltip("Rectángulo blanco que tapa la rejilla durante el flash.")]
        [SerializeField] private SpriteRenderer flash;

        [Tooltip("Donde se crean los textos.")]
        [SerializeField] private Transform textos;

        private readonly List<TextoEnRejilla> _textos = new List<TextoEnRejilla>();
        private int _temblorPixeles;
        private int _temblorRestante;
        private int _paso;

        public Camera Camara => camara;

        public PixelPerfectCamera PixelPerfect => pixelPerfect;

        public SpriteRenderer Plano => plano;

        public SpriteRenderer PlanoEncima => planoEncima;

        /// <summary>
        /// Por cuánto se multiplica cada píxel del plano en pantalla. Lo calcula la Pixel Perfect
        /// Camera al encenderse y al dibujar: 0 si todavía no se ha encendido.
        /// </summary>
        public int Escala => pixelPerfect != null && gameObject.activeInHierarchy ? pixelPerfect.pixelRatio : 0;

        public bool Visible => gameObject.activeSelf;

        /// <summary>¿Se ve el flash blanco en este fotograma?</summary>
        public bool FlashVisible => flash != null && flash.enabled;

        /// <summary>Desplazamiento actual de la cámara por el temblor, en píxeles del plano.</summary>
        public Vector2Int Temblor { get; private set; }

        /// <summary>¿Hay un temblor en curso?</summary>
        public bool Temblando => _temblorRestante > 0 || Temblor != Vector2Int.zero;

        /// <summary>Los textos que se ven ahora, uno por línea.</summary>
        public string TextoVisible
        {
            get
            {
                var texto = new StringBuilder();
                foreach (var t in _textos)
                {
                    if (t.Visible)
                    {
                        texto.AppendLine(t.Texto);
                    }
                }

                return texto.ToString().TrimEnd();
            }
        }

        /// <summary>Los textos de la cámara (para las pruebas).</summary>
        public IReadOnlyList<TextoEnRejilla> Textos => _textos;

        /// <summary>Muestra <paramref name="sprite"/> a pantalla completa (o negro, si es null).</summary>
        public void Mostrar(Sprite sprite)
        {
            Empezar();
            Pintar(sprite, 1f, Vector2Int.zero, null, 0f, Vector2Int.zero);
        }

        /// <summary>Se enciende en negro, sin textos, sin temblor y sin flash.</summary>
        public void Empezar()
        {
            gameObject.SetActive(true);
            Pintar(null, 0f, Vector2Int.zero, null, 0f, Vector2Int.zero);
            foreach (var t in _textos)
            {
                t.gameObject.SetActive(false);
            }

            _temblorRestante = 0;
            Temblor = Vector2Int.zero;
            AplicarTemblor();
            Flash(false);
        }

        public void Ocultar()
        {
            _temblorRestante = 0;
            Temblor = Vector2Int.zero;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Los dos planos: el de abajo y el que entra encima en un fundido cruzado, cada uno con su
        /// opacidad y su encuadre en píxeles del plano (x a la derecha, y arriba). Lo que queda al
        /// descubierto es el negro de la cámara.
        /// </summary>
        public void Pintar(Sprite abajo, float alfaAbajo, Vector2Int desplazamientoAbajo, Sprite encima, float alfaEncima, Vector2Int desplazamientoEncima)
        {
            Pintar(plano, abajo, alfaAbajo, desplazamientoAbajo);
            Pintar(planoEncima, encima, alfaEncima, desplazamientoEncima);
        }

        private static void Pintar(SpriteRenderer capa, Sprite sprite, float alfa, Vector2Int desplazamiento)
        {
            if (capa == null)
            {
                return;
            }

            capa.sprite = sprite;
            capa.enabled = sprite != null && alfa > 0f;
            capa.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alfa));
            capa.transform.localPosition = new Vector3(desplazamiento.x, desplazamiento.y, 0f);
        }

        /// <summary>
        /// Un temblor: durante <paramref name="fotogramas"/> fotogramas la cámara se desplaza
        /// <paramref name="pixeles"/> píxeles enteros, cada fotograma en una dirección, y vuelve a su
        /// sitio. Se cuentan fotogramas, no segundos.
        /// </summary>
        public void Temblar(int pixeles, int fotogramas)
        {
            _temblorPixeles = Mathf.Max(1, pixeles);
            _temblorRestante = Mathf.Max(_temblorRestante, fotogramas);
        }

        /// <summary>Enciende o apaga el flash blanco. Cuántos fotogramas dura lo cuenta el reproductor.</summary>
        public void Flash(bool visible)
        {
            if (flash == null)
            {
                return;
            }

            if (flash.sprite == null)
            {
                flash.sprite = Blanco;
            }

            flash.enabled = visible;
        }

        /// <summary>
        /// Los textos que se ven, con su opacidad, escritos con la fuente de
        /// <paramref name="cinematica"/>. Si no tiene fuente, se usa la de por defecto y se avisa.
        /// </summary>
        public void Escribir(IReadOnlyList<LineaTiempoCinematica.TextoVisible> visibles, CutsceneData cinematica)
        {
            var fuente = cinematica != null && cinematica.fuente != null ? cinematica.fuente : null;
            if (fuente == null && visibles.Count > 0)
            {
                fuente = FabricaUI.Fuente;
                if (cinematica != null && AvisadasSinFuente.Add(cinematica.Id))
                {
                    Debug.LogWarning($"[Cinemática] '{cinematica.Id}' no tiene fuente bitmap (PENDIENTES N21): su texto se ve con la fuente por defecto, que no es pixel art.");
                }
            }

            for (var i = 0; i < visibles.Count; i++)
            {
                var cartel = visibles[i].Cartel;
                var texto = Texto(i);
                var tamano = cinematica != null ? cinematica.tamanoFuente : 8;
                if (cartel.titulo)
                {
                    tamano *= 2; // el título, al doble: un entero, sigue en la rejilla
                }

                var punto = cartel.enPunto ? cartel.punto : new Vector2Int(AnchoReferencia / 2, FilasTercio[(int)cartel.posicion]);
                var alineacion = cartel.enPunto ? cartel.alineacion : TextAnchor.MiddleCenter;
                texto.gameObject.SetActive(true);
                texto.Fijar(cartel.texto, fuente, tamano, cinematica != null ? cinematica.interlineado : 0, punto, alineacion,
                    cinematica != null ? cinematica.colorTexto : Color.white);
                texto.FijarAlfa(visibles[i].Alfa);
            }

            for (var i = visibles.Count; i < _textos.Count; i++)
            {
                _textos[i].gameObject.SetActive(false);
            }
        }

        private TextoEnRejilla Texto(int indice)
        {
            while (_textos.Count <= indice)
            {
                var objeto = new GameObject($"Texto{_textos.Count + 1}") { layer = gameObject.layer };
                objeto.transform.SetParent(textos != null ? textos : transform, false);
                objeto.AddComponent<MeshFilter>();
                objeto.AddComponent<MeshRenderer>().sortingOrder = OrdenTexto;
                _textos.Add(objeto.AddComponent<TextoEnRejilla>());
            }

            return _textos[indice];
        }

        private void LateUpdate()
        {
            if (_temblorRestante > 0)
            {
                Temblor = Direcciones[_paso++ % Direcciones.Length] * _temblorPixeles;
                _temblorRestante--;
            }
            else
            {
                Temblor = Vector2Int.zero;
                _paso = 0;
            }

            AplicarTemblor();
        }

        private void AplicarTemblor()
        {
            if (camara != null)
            {
                camara.transform.localPosition = new Vector3(Temblor.x, Temblor.y, -10f);
            }
        }

        // Un píxel blanco a 1 píxel por unidad, escalado a la rejilla entera.
        private static Sprite Blanco
        {
            get
            {
                if (_blanco == null)
                {
                    var textura = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "FlashCinematica", filterMode = FilterMode.Point };
                    textura.SetPixel(0, 0, Color.white);
                    textura.Apply();
                    _blanco = Sprite.Create(textura, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), PixelesPorUnidad);
                }

                return _blanco;
            }
        }

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
        /// Monta la cámara, los planos, el flash y el sitio de los textos por código, apagados.
        /// <paramref name="material"/> es el de los planos: sin luces 2D, para que el dibujo se vea
        /// tal cual (sin él, el de sprites sin luz de URP).
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

            if (material == null)
            {
                var sombreador = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                material = sombreador != null ? new Material(sombreador) : null;
            }

            componente.camara = camara;
            componente.pixelPerfect = pixelPerfect;
            componente.plano = Capa(raiz.transform, "Plano", capa, OrdenAbajo, material);
            componente.planoEncima = Capa(raiz.transform, "PlanoEncima", capa, OrdenEncima, material);
            componente.flash = Capa(raiz.transform, "Flash", capa, OrdenFlash, material);
            componente.flash.transform.localScale = new Vector3(AnchoReferencia, AltoReferencia, 1f);

            var objetoTextos = new GameObject("Textos") { layer = capa };
            objetoTextos.transform.SetParent(raiz.transform, false);
            componente.textos = objetoTextos.transform;
            return componente;
        }

        private static SpriteRenderer Capa(Transform raiz, string nombre, int capa, int orden, Material material)
        {
            var objeto = new GameObject(nombre) { layer = capa };
            objeto.transform.SetParent(raiz, false);
            var sprite = objeto.AddComponent<SpriteRenderer>();
            sprite.enabled = false;
            sprite.sortingOrder = orden;
            if (material != null)
            {
                sprite.sharedMaterial = material;
            }

            return sprite;
        }
    }
}
