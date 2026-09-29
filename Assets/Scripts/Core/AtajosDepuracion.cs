using Fragmentum.Core.Juego;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fragmentum.Core
{
    /// <summary>
    /// Atajos para probar el flujo de juego antes de que existan la salida de nivel (Fase 6)
    /// y los jefes (Fase 7). Solo funcionan en el Editor y en builds de desarrollo.
    /// - Esc: pausa y reanuda.
    /// - F9: completa el nivel actual (otorga su fragmento y su habilidad).
    /// - F10: vuelve al menú.
    /// </summary>
    public class AtajosDepuracion : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            var teclado = Keyboard.current;
            var juego = GameStateMachine.Instancia;
            if (teclado == null || juego == null)
            {
                return;
            }

            if (teclado.escapeKey.wasPressedThisFrame &&
                (juego.EstadoActual == ESTADOS_JUEGO.PLAYING || juego.EstadoActual == ESTADOS_JUEGO.PAUSED))
            {
                juego.AlternarPausa();
            }

            if (teclado.f9Key.wasPressedThisFrame && juego.EstadoActual == ESTADOS_JUEGO.PLAYING)
            {
                juego.CompletarNivel();
            }

            if (teclado.f10Key.wasPressedThisFrame && juego.Puede(ESTADOS_JUEGO.LOADING))
            {
                juego.IrAlMenu();
            }
        }
#endif
    }
}
