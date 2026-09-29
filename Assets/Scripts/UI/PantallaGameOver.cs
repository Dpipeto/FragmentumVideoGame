using Fragmentum.Core;
using Fragmentum.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Pantalla de Game Over, con la misma estética que la pausa. Ofrece reintentar desde el
    /// último checkpoint (sin recargar la escena), reiniciar el nivel desde el principio o
    /// volver al menú, y dice dónde reaparecerá el jugador.
    ///
    /// En la v1 esta UI vivía dentro del <c>GameManager</c> y se manejaba con las teclas R y M
    /// leídas en su <c>Update</c>; ahora es una pantalla que solo habla con la FSM global.
    /// </summary>
    public class PantallaGameOver : PantallaSuperpuesta
    {
        private Text _mundo;
        private Text _destino;
        private Text _ayuda;
        private Button _reiniciar;

        protected override void ConstruirContenido(RectTransform columna)
        {
            FabricaUI.Texto(columna, "Titulo", FabricaUI.Espaciar("HAS CAÍDO"), 44, new Color(1f, 1f, 1f, 0.9f));
            _mundo = TextoAcento(columna, "Mundo", 24);
            FabricaUI.Hueco(columna, 6f);
            SeparadorAcento(columna);
            FabricaUI.Hueco(columna, 10f);
            _destino = FabricaUI.Texto(columna, "Destino", string.Empty, 22, new Color(1f, 1f, 1f, 0.55f));
            FabricaUI.Hueco(columna, 24f);

            AgregarOpcion(columna, "Reintentar", () => Juego?.Reintentar());
            _reiniciar = AgregarOpcion(columna, "Reiniciar nivel", () => Juego?.ReiniciarNivel());
            AgregarOpcion(columna, "Menú principal", () => Juego?.IrAlMenu());

            FabricaUI.Hueco(columna, 30f);
            _ayuda = FabricaUI.Texto(columna, "Ayuda", string.Empty, 18, new Color(1f, 1f, 1f, 0.4f));
        }

        protected override void Rellenar(LevelData nivel)
        {
            _mundo.text = nivel != null ? nivel.Etiqueta : string.Empty;

            var gestor = GameManager.Instancia;
            var hayCheckpoint = gestor != null && gestor.CheckpointDe(nivel) != null;
            _destino.text = hayCheckpoint
                ? "Reintentar te devuelve al último punto de control."
                : "Reintentar te devuelve al inicio del nivel.";

            _ayuda.text = Ayuda(Acento, ("↑↓", "NAVEGAR"), ("ENTER", "SELECCIONAR"));
            _reiniciar.interactable = Juego != null && Juego.PuedeReiniciar();
        }
    }
}
