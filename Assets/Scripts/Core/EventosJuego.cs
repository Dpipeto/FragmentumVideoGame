using System;
using UnityEngine;

namespace Fragmentum.Core
{
    /// <summary>
    /// Hub de los pocos eventos globales del juego (patrón Observer). Quien publica y quien
    /// escucha no se conocen: el jugador avisa de que murió sin saber que existe una FSM
    /// global, y el input se bloquea sin que la FSM global conozca al jugador.
    ///
    /// Se usan eventos C# en lugar de canales ScriptableObject para no tener que crear y
    /// asignar un asset por evento (plan, §7).
    /// </summary>
    public static class EventosJuego
    {
        /// <summary>El jugador del nivel murió.</summary>
        public static event Action JugadorMurio;

        /// <summary>Se pidió reintentar desde el último checkpoint (tras el Game Over).</summary>
        public static event Action ReintentoSolicitado;

        /// <summary>Cambió el bloqueo global del input de juego: (bloqueado).</summary>
        public static event Action<bool> EntradaBloqueadaCambiada;

        /// <summary>
        /// ¿Está bloqueado el input de juego? Lo fija la FSM global (pausa, cinemáticas, cargas,
        /// Game Over) y lo respeta <c>FuenteInputJugador</c>.
        /// </summary>
        public static bool EntradaBloqueada { get; private set; }

        public static void NotificarMuerteJugador() => JugadorMurio?.Invoke();

        public static void SolicitarReintento() => ReintentoSolicitado?.Invoke();

        public static void FijarEntradaBloqueada(bool bloqueada)
        {
            if (EntradaBloqueada == bloqueada)
            {
                return;
            }

            EntradaBloqueada = bloqueada;
            EntradaBloqueadaCambiada?.Invoke(bloqueada);
        }

        /// <summary>Vuelve al estado inicial. Lo usan las pruebas y el arranque sin recarga de dominio.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reiniciar()
        {
            JugadorMurio = null;
            ReintentoSolicitado = null;
            EntradaBloqueadaCambiada = null;
            EntradaBloqueada = false;
        }
    }
}
