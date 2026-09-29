using Fragmentum.Core;
using Fragmentum.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Fin de nivel: el mundo superado, el fragmento de identidad integrado y la habilidad que
    /// desbloquea, y a dónde lleva «Continuar» (el siguiente mundo, o el selector si todavía
    /// está en construcción).
    ///
    /// Aquí se enganchará en la Fase 8 el registro emocional omitible (plan, §3.6: el lienzo
    /// de LEVEL_COMPLETE es «registro emocional y fragmento»).
    /// </summary>
    public class PantallaFinNivel : PantallaSuperpuesta
    {
        private Text _mundo;
        private Text _fragmento;
        private Text _descripcionFragmento;
        private Text _habilidad;
        private Text _siguiente;
        private Text _ayuda;

        protected override void ConstruirContenido(RectTransform columna)
        {
            FabricaUI.Texto(columna, "Titulo", FabricaUI.Espaciar("MUNDO SUPERADO"), 34, new Color(1f, 1f, 1f, 0.85f));
            _mundo = TextoAcento(columna, "Mundo", 24);
            FabricaUI.Hueco(columna, 6f);
            SeparadorAcento(columna);
            FabricaUI.Hueco(columna, 16f);

            FabricaUI.Texto(columna, "Integrado", "FRAGMENTO INTEGRADO", 18, new Color(1f, 1f, 1f, 0.45f));
            _fragmento = TextoAcento(columna, "Fragmento", 40);
            _descripcionFragmento = FabricaUI.Texto(columna, "DescripcionFragmento", string.Empty, 22, new Color(1f, 1f, 1f, 0.6f));
            FabricaUI.Hueco(columna, 12f);
            _habilidad = FabricaUI.Texto(columna, "Habilidad", string.Empty, 22, new Color(1f, 1f, 1f, 0.85f));
            FabricaUI.Hueco(columna, 24f);

            AgregarOpcion(columna, "Continuar", () => Juego?.ContinuarTrasNivel());
            _siguiente = FabricaUI.Texto(columna, "Siguiente", string.Empty, 18, new Color(1f, 1f, 1f, 0.45f));
            FabricaUI.Hueco(columna, 6f);
            AgregarOpcion(columna, "Menú principal", () => Juego?.IrAlMenu());

            FabricaUI.Hueco(columna, 26f);
            _ayuda = FabricaUI.Texto(columna, "Ayuda", string.Empty, 18, new Color(1f, 1f, 1f, 0.4f));

            foreach (var texto in new[] { _descripcionFragmento, _habilidad })
            {
                var elemento = texto.gameObject.AddComponent<LayoutElement>();
                elemento.preferredWidth = 900f;
            }
        }

        protected override void Rellenar(LevelData nivel)
        {
            _mundo.text = nivel != null ? nivel.Etiqueta : string.Empty;

            var fragmento = nivel != null ? nivel.fragmento : null;
            _fragmento.text = fragmento != null ? fragmento.nombre : "—";
            _descripcionFragmento.text = fragmento != null ? fragmento.descripcion : string.Empty;

            var habilidad = nivel == null ? null
                : nivel.habilidadOtorgada != null ? nivel.habilidadOtorgada
                : fragmento != null ? fragmento.habilidad : null;
            _habilidad.text = habilidad != null
                ? $"<color=#{FabricaUI.ColorHtml(Acento)}>Nueva habilidad: {habilidad.nombre}</color>\n<size=20>{habilidad.descripcion}</size>"
                : string.Empty;

            _siguiente.text = DescribirSiguiente(nivel);
            _ayuda.text = Ayuda(Acento, ("↑↓", "NAVEGAR"), ("ENTER", "SELECCIONAR"));
        }

        private static string DescribirSiguiente(LevelData nivel)
        {
            var bd = GameManager.Instancia != null ? GameManager.Instancia.BaseDeDatos : null;
            var siguiente = bd != null && nivel != null ? bd.Siguiente(nivel) : null;
            if (siguiente == null)
            {
                return "Vuelves al selector de mundos.";
            }

            return siguiente.Jugable
                ? $"Siguiente: {siguiente.Etiqueta}"
                : $"{siguiente.Etiqueta} está en construcción: vuelves al selector.";
        }
    }
}
