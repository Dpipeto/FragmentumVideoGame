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
    ///
    /// Se comprueba <see cref="Debug.isDebugBuild"/> en lugar de la directiva
    /// <c>DEVELOPMENT_BUILD</c>, que Unity 6000.6 marca como obsoleta (aviso UAC0009).
    /// </summary>
    public class AtajosDepuracion : MonoBehaviour
    {
        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                enabled = false;
            }
        }

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
    }
}
