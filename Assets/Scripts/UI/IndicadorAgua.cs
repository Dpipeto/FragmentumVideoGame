using Fragmentum.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Lo que el HUD enseña del agua (mundo de la Tristeza):
    /// - <b>El aire</b>, bajo la vida: una burbuja que se vacía (<see cref="ArteInterfaz"/>) o, sin
    ///   su arte, una barra. Aparece al meterse en el agua y se va cuando el aire vuelve a estar
    ///   lleno. Sin aire parpadea en rojo: Kael está perdiendo corazones.
    /// - <b>El velo submarino:</b> los bordes de la pantalla se oscurecen en azul mientras Kael está
    ///   bajo el agua. Es la «visibilidad reducida» del Anexo A, hecha con la viñeta de la interfaz
    ///   en lugar de con un volumen de post-procesado.
    ///
    /// El velo es la viñeta generada por código. La burbuja va a su tamaño: un píxel suyo, un
    /// píxel de la interfaz.
    /// Solo escucha a <see cref="OxigenoJugador"/>: no pregunta nada en cada fotograma.
    /// </summary>
    public class IndicadorAgua : MonoBehaviour
    {
        private static readonly Color ColorAire = new Color(0.45f, 0.85f, 1f, 1f);
        private static readonly Color ColorAsfixia = new Color(0.9f, 0.2f, 0.2f, 1f);
        private static readonly Color ColorVelo = new Color(0.02f, 0.08f, 0.18f, 1f);

        private const float OpacidadVelo = 0.7f;
        private const float VelocidadVelo = 3f;
        private const float ParpadeosPorSegundo = 3f;

        private Image _velo;
        private GameObject _barra;
        private Image _relleno;
        private Image _burbuja;
        private Sprite[] _fotogramas;
        private float _aire = 1f;
        private OxigenoJugador _oxigeno;
        private float _alfaVelo;
        private float _alfaVeloObjetivo;
        private bool _asfixia;

        /// <summary>¿Se ve el aire (la burbuja o la barra)?</summary>
        public bool BarraVisible => _barra != null && _barra.activeSelf;

        /// <summary>El aire que marca (0 a 1).</summary>
        public float Aire => _aire;

        /// <summary>¿El aire es la burbuja dibujada? (Si no, la barra provisional.)</summary>
        public bool ConBurbuja => _burbuja != null;

        /// <summary>El fotograma de la burbuja que se ve ahora, o null sin arte.</summary>
        public Sprite Burbuja => _burbuja != null ? _burbuja.sprite : null;

        /// <summary>Opacidad del velo submarino ahora mismo (0 fuera del agua).</summary>
        public float Velo => _alfaVelo;

        /// <summary>¿Parpadea en rojo? (Sin aire.)</summary>
        public bool Asfixia => _asfixia;

        /// <summary>
        /// Crea el velo y la barra. El velo va el primero, para quedar por detrás del resto del
        /// HUD: la llama el <see cref="Hud"/> antes de construir lo demás.
        /// </summary>
        public void Construir(RectTransform raiz)
        {
            _velo = FabricaUI.Vineta(raiz, 0f);
            _velo.name = "VeloSubmarino";
            _velo.color = new Color(ColorVelo.r, ColorVelo.g, ColorVelo.b, 0f);
            _velo.gameObject.SetActive(false);

            var arte = ArteInterfaz.Cargar();
            if (arte != null && arte.TieneOxigeno)
            {
                _fotogramas = arte.oxigeno;
                _burbuja = FabricaUI.Objeto("BurbujaAire", raiz).gameObject.AddComponent<Image>();
                _burbuja.sprite = _fotogramas[0];
                _burbuja.raycastTarget = false;
                var tamano = _fotogramas[0].rect.size;
                FabricaUI.Colocar(_burbuja.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -100f), tamano, new Vector2(0f, 1f));
                _barra = _burbuja.gameObject;
                _barra.SetActive(false);
                return;
            }

            var fondo = FabricaUI.Objeto("BarraAire", raiz).gameObject.AddComponent<Image>();
            fondo.color = new Color(0f, 0f, 0f, 0.6f);
            fondo.raycastTarget = false;
            FabricaUI.Colocar(fondo.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -108f), new Vector2(240f, 18f), new Vector2(0f, 1f));
            _barra = fondo.gameObject;

            _relleno = FabricaUI.Objeto("Relleno", fondo.transform).gameObject.AddComponent<Image>();
            _relleno.color = ColorAire;
            _relleno.raycastTarget = false;
            _relleno.type = Image.Type.Filled;
            _relleno.fillMethod = Image.FillMethod.Horizontal;
            _relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
            _relleno.sprite = SpriteBlanco();
            _relleno.rectTransform.anchorMin = Vector2.zero;
            _relleno.rectTransform.anchorMax = Vector2.one;
            _relleno.rectTransform.offsetMin = new Vector2(2f, 2f);
            _relleno.rectTransform.offsetMax = new Vector2(-2f, -2f);

            var etiqueta = FabricaUI.Titulo(fondo.transform, "Etiqueta", "AIRE", 16, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleLeft);
            FabricaUI.Colocar(etiqueta.rectTransform, new Vector2(1f, 0.5f), new Vector2(10f, 0f), new Vector2(80f, 20f), new Vector2(0f, 0.5f));
            _barra.SetActive(false);
        }

        /// <summary>Se engancha al oxígeno del jugador (null lo suelta y lo esconde todo).</summary>
        public void Vincular(OxigenoJugador oxigeno)
        {
            if (_oxigeno != null)
            {
                _oxigeno.OnCambio -= AlCambiar;
            }

            _oxigeno = oxigeno;
            if (_oxigeno == null)
            {
                AlCambiar(1f, false, false);
                _alfaVelo = 0f;
                PintarVelo();
                return;
            }

            _oxigeno.OnCambio += AlCambiar;
            _oxigeno.Notificar();
        }

        private void OnDestroy()
        {
            if (_oxigeno != null)
            {
                _oxigeno.OnCambio -= AlCambiar;
            }
        }

        private void AlCambiar(float fraccion, bool bajoElAgua, bool asfixia)
        {
            if (_barra == null)
            {
                return;
            }

            _barra.SetActive(bajoElAgua || fraccion < 0.999f);
            _aire = Mathf.Clamp01(fraccion);
            _asfixia = asfixia;
            if (_burbuja != null)
            {
                _burbuja.sprite = _fotogramas[ArteInterfaz.FotogramaDeAire(_aire, _fotogramas.Length)];
                if (!asfixia)
                {
                    _burbuja.color = Color.white;
                }
            }
            else
            {
                _relleno.fillAmount = _aire;
                if (!asfixia)
                {
                    _relleno.color = ColorAire;
                }
            }

            _alfaVeloObjetivo = bajoElAgua ? OpacidadVelo : 0f;
        }

        private void Update()
        {
            if (_velo == null)
            {
                return;
            }

            if (!Mathf.Approximately(_alfaVelo, _alfaVeloObjetivo))
            {
                _alfaVelo = Mathf.MoveTowards(_alfaVelo, _alfaVeloObjetivo, VelocidadVelo * Time.unscaledDeltaTime);
                PintarVelo();
            }

            if (!_asfixia)
            {
                return;
            }

            var encendido = Mathf.Repeat(Time.unscaledTime * ParpadeosPorSegundo, 1f) < 0.5f;
            if (_burbuja != null)
            {
                // La burbuja rota, que late en rojo.
                _burbuja.color = encendido ? ColorAsfixia : Color.white;
            }
            else if (_relleno != null)
            {
                _relleno.color = encendido ? ColorAsfixia : ColorAire;
                _relleno.fillAmount = encendido ? 1f : 0f;
            }
        }

        private void PintarVelo()
        {
            _velo.color = new Color(ColorVelo.r, ColorVelo.g, ColorVelo.b, _alfaVelo);
            _velo.gameObject.SetActive(_alfaVelo > 0.001f);
        }

        // Una imagen «Filled» necesita sprite para recortarse.
        private static Sprite _blanco;

        private static Sprite SpriteBlanco()
        {
            if (_blanco == null)
            {
                var textura = Texture2D.whiteTexture;
                _blanco = Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f));
                _blanco.name = "BlancoGenerado";
            }

            return _blanco;
        }
    }
}
