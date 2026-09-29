using Fragmentum.Core.Juego;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fragmentum.Core
{
    /// <summary>
    /// Atajos para probar el flujo de juego antes de que existan los jefes (Fase 7). Solo
    /// funcionan en el Editor y en builds de desarrollo.
    /// - F9: completa el nivel actual (otorga su fragmento y su habilidad).
    /// - F10: vuelve al menú.
    ///
    /// La pausa (Esc o Start) ya no es un atajo: la lleva el <c>UIManager</c> con la acción
    /// <c>Game/Pause</c> del asset de input.
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
