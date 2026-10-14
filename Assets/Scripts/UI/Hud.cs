using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// HUD de los niveles: barra de vida del jugador, el puntaje y el reloj, el mundo actual, un aviso breve
    /// (checkpoint, fragmento obtenido), la tecla de Interactuar sobre lo que Kael tiene al alcance y la barra del jefe durante su combate. Vive con los sistemas persistentes (lo crea el UIManager), así
    /// que ninguna escena de nivel necesita su propio HUD, y se engancha al jugador que registra
    /// el GameManager en cada escena.
    ///
    /// Solo escucha eventos: no lee la vida ni el puntaje en cada frame.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        private const float DuracionAviso = 2.2f;

        private LifeBar _barra;
        private Text _mundo;
        private Text _aviso;
        private GameObject _panelJefe;
        private Text _nombreJefe;
        private LifeBar _barraJefe;
        private GameObject _panelDialogo;
        private Text _hablante;
        private Text _linea;
        private float _ocultarAvisoEn;
        private bool _construido;
        private IndicadorAgua _agua;
        private ContadorPuntaje _puntaje;
        private AvisoInteraccion _interaccion;

        public LifeBar Barra => _barra;

        /// <summary>El aviso de Interactuar sobre lo que Kael tiene al alcance (una antorcha).</summary>
        public AvisoInteraccion Interaccion => _interaccion;

        /// <summary>El puntaje y el reloj del nivel, arriba a la derecha.</summary>
        public ContadorPuntaje Puntaje => _puntaje;

        /// <summary>La barra de aire y el velo submarino (mundo de la Tristeza).</summary>
        public IndicadorAgua Agua => _agua;

        /// <summary>¿Se ve la barra del jefe? (Solo durante un combate de jefe.)</summary>
        public bool JefeVisible => _panelJefe != null && _panelJefe.activeSelf;

        public string NombreJefe => _nombreJefe != null ? _nombreJefe.text : string.Empty;

        /// <summary>Línea de diálogo visible ahora mismo, o vacío.</summary>
        public string LineaActual => _panelDialogo != null && _panelDialogo.activeSelf ? _linea.text : string.Empty;

        public string HablanteActual => _panelDialogo != null && _panelDialogo.activeSelf ? _hablante.text : string.Empty;

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

            // Lo primero: su velo submarino queda por detrás de todo lo demás.
            _agua = gameObject.AddComponent<IndicadorAgua>();
            _agua.Construir(raiz);

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

            _mundo = FabricaUI.Titulo(raiz, "Mundo", string.Empty, 22, new Color(1f, 1f, 1f, 0.55f), TextAnchor.UpperLeft);
            FabricaUI.Colocar(_mundo.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -66f), new Vector2(700f, 34f), new Vector2(0f, 1f));

            // Arriba a la derecha, donde la v1 llevaba las monedas: el puntaje, el reloj y, un
            // momento, lo que se acaba de ganar.
            var puntos = FabricaUI.Titulo(raiz, "Puntaje", string.Empty, 28, new Color(0.78f, 0.64f, 0.29f, 1f), TextAnchor.UpperRight);
            FabricaUI.Colocar(puntos.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(520f, 40f), new Vector2(1f, 1f));
            var tiempo = FabricaUI.Titulo(raiz, "Tiempo", string.Empty, 24, new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperRight);
            FabricaUI.Colocar(tiempo.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, -62f), new Vector2(520f, 34f), new Vector2(1f, 1f));
            var ganancia = FabricaUI.Titulo(raiz, "Ganancia", string.Empty, 26, new Color(1f, 0.9f, 0.55f, 1f), TextAnchor.UpperRight);
            FabricaUI.Colocar(ganancia.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, -98f), new Vector2(520f, 36f), new Vector2(1f, 1f));
            _puntaje = puntos.gameObject.AddComponent<ContadorPuntaje>();
            _puntaje.Fijar(puntos, tiempo, ganancia);

            _aviso = FabricaUI.Texto(raiz, "Aviso", string.Empty, 30, new Color(1f, 1f, 1f, 0.9f));
            FabricaUI.Colocar(_aviso.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1000f, 50f));
            _aviso.gameObject.SetActive(false);

            _interaccion = gameObject.AddComponent<AvisoInteraccion>();
            _interaccion.Construir(raiz);

            ConstruirBarraJefe(raiz);
            ConstruirDialogo(raiz);
        }

        // Diálogos internos de Kael: una banda abajo, sobre el aviso, con quien habla y la línea.
        private void ConstruirDialogo(RectTransform raiz)
        {
            var panel = FabricaUI.Objeto("Dialogo", raiz);
            FabricaUI.Colocar(panel, new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(1200f, 134f), new Vector2(0.5f, 0f));
            var fondo = panel.gameObject.AddComponent<Image>();
            fondo.color = new Color(0f, 0f, 0f, 0.6f);
            fondo.raycastTarget = false;
            _panelDialogo = panel.gameObject;

            // Quien habla, en la letra de los títulos; lo que dice, en cursiva de Alegreya, algo
            // más grande que con la fuente integrada (su ojo es más pequeño).
            _hablante = FabricaUI.Titulo(panel, "Hablante", string.Empty, 24, Color.white, TextAnchor.UpperLeft);
            FabricaUI.Colocar(_hablante.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -12f), new Vector2(600f, 32f), new Vector2(0f, 1f));
            _hablante.fontStyle = FontStyle.Bold;

            _linea = FabricaUI.Texto(panel, "Linea", string.Empty, 30, new Color(0.94f, 0.93f, 0.85f, 1f), TextAnchor.UpperLeft);
            FabricaUI.Colocar(_linea.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -48f), new Vector2(1144f, 80f), new Vector2(0f, 1f));
            _linea.fontStyle = FontStyle.Italic;

            _panelDialogo.SetActive(false);
        }

        /// <summary>Muestra una línea de diálogo interno; el nombre, en el color del mundo.</summary>
        public void MostrarLinea(string hablante, string texto, Color acento)
        {
            Construir();
            _hablante.text = (hablante ?? string.Empty).ToUpperInvariant();
            _hablante.color = acento;
            _linea.text = texto ?? string.Empty;
            _panelDialogo.SetActive(true);
        }

        public void OcultarLinea()
        {
            if (_construido)
            {
                _panelDialogo.SetActive(false);
            }
        }

        // Barra del jefe arriba en el centro, con su nombre canónico; oculta fuera del combate.
        private void ConstruirBarraJefe(RectTransform raiz)
        {
            var panel = FabricaUI.Objeto("Jefe", raiz);
            FabricaUI.Colocar(panel, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(760f, 70f), new Vector2(0.5f, 1f));
            _panelJefe = panel.gameObject;

            _nombreJefe = FabricaUI.Titulo(panel, "Nombre", string.Empty, 24, new Color(1f, 0.85f, 0.7f, 0.95f));
            FabricaUI.Colocar(_nombreJefe.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(760f, 32f), new Vector2(0.5f, 1f));

            var fondo = FabricaUI.Objeto("Barra", panel).gameObject.AddComponent<Image>();
            fondo.color = new Color(0f, 0f, 0f, 0.65f);
            fondo.raycastTarget = false;
            FabricaUI.Colocar(fondo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(760f, 22f), new Vector2(0.5f, 0f));

            var relleno = FabricaUI.Objeto("Relleno", fondo.transform).gameObject.AddComponent<Image>();
            relleno.color = new Color(1f, 0.42f, 0.2f, 1f);
            relleno.raycastTarget = false;
            relleno.type = Image.Type.Filled;
            relleno.fillMethod = Image.FillMethod.Horizontal;
            relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
            relleno.sprite = SpriteBlanco();
            relleno.rectTransform.anchorMin = Vector2.zero;
            relleno.rectTransform.anchorMax = Vector2.one;
            relleno.rectTransform.offsetMin = new Vector2(3f, 3f);
            relleno.rectTransform.offsetMax = new Vector2(-3f, -3f);

            _barraJefe = fondo.gameObject.AddComponent<LifeBar>();
            _barraJefe.BuscarJugador = false;
            _barraJefe.FijarRelleno(relleno);
            _panelJefe.SetActive(false);
        }

        /// <summary>Muestra la barra de vida del jefe durante su combate.</summary>
        public void MostrarJefe(string nombre, Health vida)
        {
            Construir();
            _nombreJefe.text = (nombre ?? string.Empty).ToUpperInvariant();
            _panelJefe.SetActive(true);
            _barraJefe.Vincular(vida);
        }

        public void OcultarJefe()
        {
            if (!_construido)
            {
                return;
            }

            _barraJefe.Vincular(null);
            _panelJefe.SetActive(false);
        }

        /// <summary>Engancha la barra de vida y la de aire al jugador (null las deja vacías).</summary>
        public void Vincular(PlayerController jugador)
        {
            Construir();
            _barra.Vincular(jugador != null ? jugador.Vida : null);
            _agua.Vincular(jugador != null ? jugador.GetComponent<OxigenoJugador>() : null);
            _interaccion.Vincular(jugador);
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
