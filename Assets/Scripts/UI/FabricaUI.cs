using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Crea elementos de uGUI por código con un estilo común: textos, botones, columnas y los
    /// adornos de las pantallas superpuestas (velo, viñeta, esquinas y separador, según la
    /// maqueta de <c>Referencias/Menus/fragmentum_pause_worlds.jsx</c>). La usan el selector de
    /// niveles, las herramientas que montan las escenas de menú y el <c>UIManager</c>.
    ///
    /// Usa la fuente integrada de Unity y no TextMeshPro, porque los recursos esenciales de
    /// TMP no están importados en el proyecto (pendiente de pulido de UI, PENDIENTES B9).
    /// </summary>
    public static class FabricaUI
    {
        public static readonly Vector2 ResolucionReferencia = new Vector2(1920f, 1080f);

        private static Font _fuente;
        private static Sprite _vineta;

        public static Font Fuente => _fuente != null ? _fuente : _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform Objeto(string nombre, Transform padre)
        {
            var objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            return (RectTransform)objeto.transform;
        }

        /// <summary>Lienzo a pantalla completa que escala desde 1920×1080 y recibe clics.</summary>
        public static Canvas Lienzo(Transform padre, string nombre, int orden)
        {
            var rect = Objeto(nombre, padre);
            var lienzo = rect.gameObject.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            lienzo.sortingOrder = orden;

            var escalador = rect.gameObject.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = ResolucionReferencia;
            escalador.matchWidthOrHeight = 0.5f;

            rect.gameObject.AddComponent<GraphicRaycaster>();
            return lienzo;
        }

        public static Text Texto(Transform padre, string nombre, string contenido, int tamano, Color color,
            TextAnchor alineacion = TextAnchor.MiddleCenter)
        {
            var texto = Objeto(nombre, padre).gameObject.AddComponent<Text>();
            texto.font = Fuente;
            texto.text = contenido;
            texto.fontSize = tamano;
            // Unity sube el tamaño máximo hasta el de la fuente al cargar la escena; si no se
            // fija aquí, abrir en el Editor una escena generada la marca como modificada.
            texto.resizeTextMaxSize = Mathf.Max(texto.resizeTextMaxSize, tamano);
            texto.color = color;
            texto.alignment = alineacion;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.supportRichText = true;
            return texto;
        }

        /// <summary>Botón con fondo de color y texto centrado que ocupa todo el botón.</summary>
        public static Button Boton(Transform padre, string nombre, string etiqueta, Color fondo, float alto,
            UnityAction alPulsar = null, int tamanoTexto = 28)
        {
            var rect = Objeto(nombre, padre);
            var imagen = rect.gameObject.AddComponent<Image>();
            imagen.color = fondo;

            var boton = rect.gameObject.AddComponent<Button>();
            var colores = boton.colors;
            colores.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colores.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colores.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
            boton.colors = colores;
            if (alPulsar != null)
            {
                boton.onClick.AddListener(alPulsar);
            }

            var elemento = rect.gameObject.AddComponent<LayoutElement>();
            elemento.preferredHeight = alto;
            elemento.minHeight = alto;

            var texto = Texto(rect, "Texto", etiqueta, tamanoTexto, Color.white);
            Estirar(texto.rectTransform, 16f);
            return boton;
        }

        /// <summary>
        /// Opción de menú de solo texto, como en la maqueta: tenue en reposo y del color de
        /// acento cuando el ratón está encima o está seleccionada con el teclado o el mando.
        /// </summary>
        public static Button Opcion(Transform padre, string nombre, string etiqueta, Color acento, UnityAction alPulsar,
            float ancho = 640f, float alto = 64f, int tamanoTexto = 40)
        {
            var rect = Objeto(nombre, padre);

            // Fondo invisible para que toda la franja reciba el clic, no solo las letras.
            var zona = rect.gameObject.AddComponent<Image>();
            zona.color = new Color(1f, 1f, 1f, 0f);

            var texto = Texto(rect, "Texto", etiqueta, tamanoTexto, Color.white);
            Estirar(texto.rectTransform);

            var boton = rect.gameObject.AddComponent<Button>();
            boton.targetGraphic = texto;
            AplicarAcento(boton, acento);
            if (alPulsar != null)
            {
                boton.onClick.AddListener(alPulsar);
            }

            var elemento = rect.gameObject.AddComponent<LayoutElement>();
            elemento.preferredWidth = ancho;
            elemento.preferredHeight = alto;
            elemento.minHeight = alto;
            return boton;
        }

        /// <summary>Colores de una opción según el acento del mundo.</summary>
        public static void AplicarAcento(Selectable opcion, Color acento)
        {
            var colores = opcion.colors;
            colores.normalColor = new Color(1f, 1f, 1f, 0.55f);
            colores.highlightedColor = acento;
            colores.selectedColor = acento;
            colores.pressedColor = Color.Lerp(acento, Color.white, 0.4f);
            colores.disabledColor = new Color(1f, 1f, 1f, 0.18f);
            colores.colorMultiplier = 1f;
            colores.fadeDuration = 0.08f;
            opcion.colors = colores;
        }

        /// <summary>Columna vertical que reparte a sus hijos con separación.</summary>
        public static VerticalLayoutGroup Columna(RectTransform rect, float separacion, int margen = 0)
        {
            var columna = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            columna.spacing = separacion;
            columna.padding = new RectOffset(margen, margen, margen, margen);
            columna.childAlignment = TextAnchor.UpperCenter;
            columna.childControlWidth = true;
            columna.childControlHeight = true;
            columna.childForceExpandWidth = true;
            columna.childForceExpandHeight = false;
            return columna;
        }

        /// <summary>Columna centrada en la que cada hijo conserva su ancho preferido.</summary>
        public static VerticalLayoutGroup ColumnaCentrada(RectTransform rect, float separacion)
        {
            var columna = Columna(rect, separacion);
            columna.childAlignment = TextAnchor.MiddleCenter;
            columna.childForceExpandWidth = false;
            return columna;
        }

        /// <summary>Hueco vertical fijo dentro de una columna.</summary>
        public static void Hueco(Transform padre, float alto)
        {
            var elemento = Objeto("Hueco", padre).gameObject.AddComponent<LayoutElement>();
            elemento.preferredHeight = alto;
            elemento.minHeight = alto;
        }

        /// <summary>Rectángulo de color a pantalla completa (velo de las pantallas superpuestas).</summary>
        public static Image Panel(Transform padre, string nombre, Color color, bool recibeClics = true)
        {
            var imagen = Objeto(nombre, padre).gameObject.AddComponent<Image>();
            imagen.color = color;
            imagen.raycastTarget = recibeClics;
            Estirar(imagen.rectTransform);
            return imagen;
        }

        /// <summary>
        /// Viñeta en gradiente radial, generada por código: la maqueta la pide y no hay sprite
        /// (PENDIENTES B9). Oscurece los bordes y deja el centro limpio.
        /// </summary>
        public static Image Vineta(Transform padre, float intensidad = 0.6f)
        {
            var imagen = Panel(padre, "Vineta", new Color(0f, 0f, 0f, intensidad), false);
            imagen.sprite = SpriteVineta();
            return imagen;
        }

        /// <summary>Línea fina (separador) de tamaño fijo dentro de una columna.</summary>
        public static Image Linea(Transform padre, string nombre, Color color, float ancho, float grosor)
        {
            var imagen = Objeto(nombre, padre).gameObject.AddComponent<Image>();
            imagen.color = color;
            imagen.raycastTarget = false;
            var elemento = imagen.gameObject.AddComponent<LayoutElement>();
            elemento.preferredWidth = ancho;
            elemento.preferredHeight = grosor;
            elemento.minHeight = grosor;
            return imagen;
        }

        /// <summary>
        /// Las cuatro esquinas en «L» que enmarcan las pantallas de la maqueta. Devuelve las
        /// ocho piezas para poder teñirlas con el acento de cada mundo.
        /// </summary>
        public static Image[] Esquinas(Transform padre, Color color, float margen = 64f, float largo = 56f, float grosor = 3f)
        {
            var piezas = new Image[8];
            var anclas = new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
            for (var i = 0; i < anclas.Length; i++)
            {
                var ancla = anclas[i];
                var signo = new Vector2(ancla.x == 0f ? 1f : -1f, ancla.y == 0f ? 1f : -1f);
                var origen = new Vector2(margen * signo.x, margen * signo.y);

                piezas[i * 2] = Pieza(padre, $"Esquina{i}_H", color, ancla, origen, new Vector2(largo, grosor), signo);
                piezas[i * 2 + 1] = Pieza(padre, $"Esquina{i}_V", color, ancla, origen, new Vector2(grosor, largo), signo);
            }

            return piezas;
        }

        private static Image Pieza(Transform padre, string nombre, Color color, Vector2 ancla, Vector2 origen, Vector2 tamano, Vector2 signo)
        {
            var imagen = Objeto(nombre, padre).gameObject.AddComponent<Image>();
            imagen.color = color;
            imagen.raycastTarget = false;
            var rect = imagen.rectTransform;
            rect.anchorMin = rect.anchorMax = ancla;
            rect.pivot = new Vector2(signo.x > 0f ? 0f : 1f, signo.y > 0f ? 0f : 1f);
            rect.anchoredPosition = origen;
            rect.sizeDelta = tamano;
            return imagen;
        }

        public static void Estirar(RectTransform rect, float margen = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margen, 0f);
            rect.offsetMax = new Vector2(-margen, 0f);
        }

        /// <summary>Coloca un rectángulo por su ancla, posición y tamaño.</summary>
        public static void Colocar(RectTransform rect, Vector2 ancla, Vector2 posicion, Vector2 tamano, Vector2? pivote = null)
        {
            rect.anchorMin = rect.anchorMax = ancla;
            rect.pivot = pivote ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
        }

        /// <summary>Oscurece un color de mundo para usarlo de fondo sin perder el tono.</summary>
        public static Color Fondo(Color color, float factor = 0.55f) =>
            new Color(color.r * factor, color.g * factor, color.b * factor, 1f);

        /// <summary>
        /// Separa las letras («PAUSA» → «P A U S A»): la fuente integrada no admite espaciado
        /// entre letras, que es como la maqueta escribe los títulos.
        /// </summary>
        public static string Espaciar(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return string.Empty;
            }

            var resultado = new StringBuilder(texto.Length * 2);
            for (var i = 0; i < texto.Length; i++)
            {
                if (i > 0)
                {
                    resultado.Append(texto[i - 1] == ' ' ? "  " : " ");
                }

                if (texto[i] != ' ')
                {
                    resultado.Append(texto[i]);
                }
            }

            return resultado.ToString();
        }

        public static string ColorHtml(Color color) => ColorUtility.ToHtmlStringRGB(color);

        private static Sprite SpriteVineta()
        {
            if (_vineta != null)
            {
                return _vineta;
            }

            const int lado = 128;
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "VinetaGenerada"
            };

            var pixeles = new Color32[lado * lado];
            for (var y = 0; y < lado; y++)
            {
                for (var x = 0; x < lado; x++)
                {
                    var dx = (x + 0.5f) / lado * 2f - 1f;
                    var dy = (y + 0.5f) / lado * 2f - 1f;
                    var distancia = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Sqrt(2f);
                    var alfa = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, distancia));
                    pixeles[y * lado + x] = new Color32(255, 255, 255, (byte)(alfa * 255f));
                }
            }

            textura.SetPixels32(pixeles);
            textura.Apply(false, true);
            _vineta = Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f));
            _vineta.name = "VinetaGenerada";
            return _vineta;
        }
    }
}
