using Fragmentum.Data;
using Fragmentum.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// HUD de los niveles: barra de vida del jugador, monedas, el mundo actual y un aviso breve
    /// al activar un checkpoint. Vive con los sistemas persistentes (lo crea el UIManager), así
    /// que ninguna escena de nivel necesita su propio HUD, y se engancha al jugador que registra
    /// el GameManager en cada escena.
    ///
    /// Solo escucha eventos: no lee la vida ni las monedas en cada frame.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        private const float DuracionAviso = 2.2f;

        private LifeBar _barra;
        private Text _mundo;
        private Text _aviso;
        private float _ocultarAvisoEn;
        private bool _construido;

        public LifeBar Barra => _barra;

        /// <summary>Texto del aviso visible ahora mismo, o vacío.</summary>
        public string AvisoActual => _aviso != null && _aviso.gameObject.activeSelf ? _aviso.text : string.Empty;

        /// <summary>Crea los elementos del HUD. La llama el UIManager una sola vez.</summary>
        public void Construir()
        {
            if (_construido)
            {
                return;
            }

            _construido = true;
            // Se construye desactivado: la barra no debe buscar jugador hasta que se la vincule.
            gameObject.SetActive(false);
            var raiz = (RectTransform)transform;
            FabricaUI.Estirar(raiz);

            var fondo = FabricaUI.Objeto("BarraVida", raiz).gameObject.AddComponent<Image>();
            fondo.color = new Color(0f, 0f, 0f, 0.6f);
            fondo.raycastTarget = false;
            FabricaUI.Colocar(fondo.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(360f, 28f), new Vector2(0f, 1f));

            var relleno = FabricaUI.Objeto("Relleno", fondo.transform).gameObject.AddComponent<Image>();
            relleno.color = new Color(0.8f, 0.16f, 0.16f, 1f);
            relleno.raycastTarget = false;
            relleno.type = Image.Type.Filled;
            relleno.fillMethod = Image.FillMethod.Horizontal;
            relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
            relleno.sprite = SpriteBlanco();
            var rectRelleno = relleno.rectTransform;
            rectRelleno.anchorMin = Vector2.zero;
            rectRelleno.anchorMax = Vector2.one;
            rectRelleno.offsetMin = new Vector2(3f, 3f);
            rectRelleno.offsetMax = new Vector2(-3f, -3f);

            _barra = fondo.gameObject.AddComponent<LifeBar>();
            _barra.BuscarJugador = false;
            _barra.FijarRelleno(relleno);

            _mundo = FabricaUI.Texto(raiz, "Mundo", string.Empty, 22, new Color(1f, 1f, 1f, 0.55f), TextAnchor.UpperLeft);
            FabricaUI.Colocar(_mundo.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -66f), new Vector2(700f, 34f), new Vector2(0f, 1f));

            var monedas = FabricaUI.Texto(raiz, "Monedas", string.Empty, 28, new Color(0.78f, 0.64f, 0.29f, 1f), TextAnchor.UpperRight);
            FabricaUI.Colocar(monedas.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(420f, 40f), new Vector2(1f, 1f));
            monedas.gameObject.AddComponent<ContadorMonedas>();

            _aviso = FabricaUI.Texto(raiz, "Aviso", string.Empty, 30, new Color(1f, 1f, 1f, 0.9f));
            FabricaUI.Colocar(_aviso.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1000f, 50f));
            _aviso.gameObject.SetActive(false);
        }

        /// <summary>Engancha la barra de vida al jugador (null la deja vacía).</summary>
        public void Vincular(PlayerController jugador)
        {
            Construir();
            _barra.Vincular(jugador != null ? jugador.Vida : null);
        }

        /// <summary>Pone el nombre del mundo en una esquina (vacío fuera de los niveles).</summary>
        public void MostrarNivel(LevelData nivel)
        {
            Construir();
            _mundo.text = nivel != null ? nivel.Etiqueta : string.Empty;
            _mundo.color = nivel != null ? new Color(nivel.ColorAcento.r, nivel.ColorAcento.g, nivel.ColorAcento.b, 0.7f) : Color.clear;
        }

        /// <summary>Aviso breve en la parte baja de la pantalla («Punto de control»).</summary>
        public void Avisar(string texto)
        {
            Construir();
            _aviso.text = texto;
            _aviso.gameObject.SetActive(true);
            _ocultarAvisoEn = Time.unscaledTime + DuracionAviso;
        }

        private void Update()
        {
            if (_aviso != null && _aviso.gameObject.activeSelf && Time.unscaledTime >= _ocultarAvisoEn)
            {
                _aviso.gameObject.SetActive(false);
            }
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
