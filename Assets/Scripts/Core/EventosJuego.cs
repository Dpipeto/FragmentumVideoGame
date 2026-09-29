using System;
using Fragmentum.Core.Combate;
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

        /// <summary>Se activó un checkpoint del nivel: (id). Lo escucha el HUD para avisar.</summary>
        public static event Action<string> CheckpointActivado;

        /// <summary>Empezó un combate de jefe: (nombre canónico, vida). El HUD muestra su barra.</summary>
        public static event Action<string, Health> JefeEnCombate;

        /// <summary>Terminó el combate de jefe (derrota o reinicio). El HUD oculta su barra.</summary>
        public static event Action JefeFueraDeCombate;

        /// <summary>
        /// ¿Está bloqueado el input de juego? Lo fija la FSM global (pausa, cinemáticas, cargas,
        /// Game Over) y lo respeta <c>FuenteInputJugador</c>.
        /// </summary>
        public static bool EntradaBloqueada { get; private set; }

        /// <summary>
        /// Fotograma en que se desbloqueó el input. En ese fotograma se ignora: el clic o el Enter
        /// que pulsan «Continuar» en un menú no deben convertirse además en un ataque.
        /// </summary>
        public static int FotogramaDesbloqueo { get; private set; } = -1;

        /// <summary>¿Bloqueado, o recién desbloqueado en este mismo fotograma?</summary>
        public static bool EntradaIgnorada => EntradaBloqueada || Time.frameCount <= FotogramaDesbloqueo;

        public static void NotificarMuerteJugador() => JugadorMurio?.Invoke();

        public static void SolicitarReintento() => ReintentoSolicitado?.Invoke();

        public static void NotificarCheckpoint(string id) => CheckpointActivado?.Invoke(id);

        public static void NotificarJefeEnCombate(string nombre, Health vida) => JefeEnCombate?.Invoke(nombre, vida);

        public static void NotificarJefeFueraDeCombate() => JefeFueraDeCombate?.Invoke();

        public static void FijarEntradaBloqueada(bool bloqueada)
        {
            if (EntradaBloqueada == bloqueada)
            {
                return;
            }

            EntradaBloqueada = bloqueada;
            if (!bloqueada)
            {
                FotogramaDesbloqueo = Time.frameCount;
            }

            EntradaBloqueadaCambiada?.Invoke(bloqueada);
        }

        /// <summary>Vuelve al estado inicial. Lo usan las pruebas y el arranque sin recarga de dominio.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reiniciar()
        {
            JugadorMurio = null;
            ReintentoSolicitado = null;
            EntradaBloqueadaCambiada = null;
            CheckpointActivado = null;
            JefeEnCombate = null;
            JefeFueraDeCombate = null;
            EntradaBloqueada = false;
            FotogramaDesbloqueo = -1;
        }
    }
}
