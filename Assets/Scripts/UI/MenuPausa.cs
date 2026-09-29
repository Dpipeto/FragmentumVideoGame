using Fragmentum.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Menú de pausa, según la maqueta: icono de pausa, «PAUSA», el mundo («I · FORTALEZA
    /// VOLCÁNICA»), su jefe, las opciones Continuar / Reiniciar / Menú principal, la frase del
    /// mundo y la ayuda de controles.
    ///
    /// Sustituye al <c>Pause</c> de la v1, que escribía <c>Time.timeScale</c> por su cuenta y
    /// cargaba el menú por nombre: ahora cada opción es una petición a la FSM global, que es
    /// la única dueña del tiempo y de las escenas.
    /// </summary>
    public class MenuPausa : PantallaSuperpuesta
    {
        private Text _mundo;
        private Text _jefe;
        private Text _cita;
        private Text _ayuda;
        private Button _reiniciar;

        protected override void ConstruirContenido(RectTransform columna)
        {
            IconoPausa(columna);
            FabricaUI.Hueco(columna, 8f);
            FabricaUI.Texto(columna, "Titulo", FabricaUI.Espaciar("PAUSA"), 30, new Color(1f, 1f, 1f, 0.8f));
            _mundo = TextoAcento(columna, "Mundo", 24);
            _jefe = FabricaUI.Texto(columna, "Jefe", string.Empty, 18, new Color(1f, 1f, 1f, 0.4f));
            FabricaUI.Hueco(columna, 6f);
            SeparadorAcento(columna);
            FabricaUI.Hueco(columna, 24f);

            AgregarOpcion(columna, "Continuar", () => Juego?.Reanudar());
            _reiniciar = AgregarOpcion(columna, "Reiniciar", () => Juego?.ReiniciarNivel());
            AgregarOpcion(columna, "Menú principal", () => Juego?.IrAlMenu());

            FabricaUI.Hueco(columna, 30f);
            _cita = TextoAcento(columna, "Cita", 22, 0.65f);
            _cita.fontStyle = FontStyle.Italic;
            FabricaUI.Hueco(columna, 18f);
            _ayuda = FabricaUI.Texto(columna, "Ayuda", string.Empty, 18, new Color(1f, 1f, 1f, 0.4f));
        }

        protected override void Rellenar(LevelData nivel)
        {
            _mundo.text = nivel != null ? nivel.Etiqueta : FabricaUI.Espaciar("PRUEBA");
            _jefe.text = nivel == null ? string.Empty
                : nivel.jefe != null ? $"JEFE: {nivel.jefe.nombreCanonico.ToUpperInvariant()}"
                : "JEFE: —  ANTESALA";
            _cita.text = nivel != null && !string.IsNullOrEmpty(nivel.cita) ? $"«{nivel.cita}»" : string.Empty;
            _ayuda.text = Ayuda(Acento, ("↑↓", "NAVEGAR"), ("ENTER", "SELECCIONAR"), ("ESC", "CONTINUAR"));

            // Reiniciar vuelve a cargar la escena: solo si está en Build Settings.
            _reiniciar.interactable = Juego != null && Juego.PuedeReiniciar();
        }

        private static void IconoPausa(Transform columna)
        {
            var icono = FabricaUI.Objeto("IconoPausa", columna);
            var elemento = icono.gameObject.AddComponent<LayoutElement>();
            elemento.preferredWidth = 34f;
            elemento.preferredHeight = 40f;

            for (var i = 0; i < 2; i++)
            {
                var barra = FabricaUI.Objeto($"Barra{i}", icono).gameObject.AddComponent<Image>();
                barra.color = new Color(1f, 1f, 1f, 0.95f);
                barra.raycastTarget = false;
                var rect = barra.rectTransform;
                rect.anchorMin = new Vector2(i == 0 ? 0f : 0.65f, 0f);
                rect.anchorMax = new Vector2(i == 0 ? 0.35f : 1f, 1f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
        }
    }
}
