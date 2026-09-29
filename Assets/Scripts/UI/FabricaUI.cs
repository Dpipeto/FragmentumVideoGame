using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Crea elementos de uGUI por código con un estilo común: textos, botones y columnas. La
    /// usan el selector de niveles (que genera sus botones desde la LevelDatabase) y las
    /// herramientas que montan las escenas de menú.
    ///
    /// Usa la fuente integrada de Unity y no TextMeshPro, porque los recursos esenciales de
    /// TMP no están importados en el proyecto (pendiente de pulido de UI, PENDIENTES B9).
    /// </summary>
    public static class FabricaUI
    {
        private static Font _fuente;

        public static Font Fuente => _fuente != null ? _fuente : _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform Objeto(string nombre, Transform padre)
        {
            var objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            return (RectTransform)objeto.transform;
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

        public static void Estirar(RectTransform rect, float margen = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margen, 0f);
            rect.offsetMax = new Vector2(-margen, 0f);
        }

        /// <summary>Oscurece un color de mundo para usarlo de fondo sin perder el tono.</summary>
        public static Color Fondo(Color color, float factor = 0.55f) =>
            new Color(color.r * factor, color.g * factor, color.b * factor, 1f);
    }
}
