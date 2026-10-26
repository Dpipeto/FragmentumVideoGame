using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Core
{
    /// <summary>
    /// Algo del escenario que Kael usa con Interactuar (W): una antorcha del mundo del Miedo, una
    /// memoria del de la Culpa.
    /// </summary>
    public interface IInteractuable
    {
        /// <summary>Dónde está. La distancia se mide desde los pies de Kael.</summary>
        Vector2 PuntoInteraccion { get; }

        /// <summary>A cuánto de los pies de Kael se puede usar.</summary>
        float RadioInteraccion { get; }

        /// <summary>¿Se puede usar ahora? (Una antorcha que ya ardió, no.)</summary>
        bool Disponible { get; }

        /// <summary>Lo que hace, para el aviso del HUD: «Encender».</summary>
        string Accion { get; }

        /// <summary>Lo usa <paramref name="quien"/> (Kael).</summary>
        void Interactuar(GameObject quien);

        /// <summary>
        /// ¿Se puede usar ahora mismo? Si no, el aviso dice solo <see cref="Accion"/>, sin la tecla: una
        /// memoria de la Culpa con Kael invisible («Para recordar, déjate ver»).
        /// </summary>
        bool Usable => true;
    }

    /// <summary>
    /// Registro de lo que se puede usar con Interactuar (W). Responde a una sola pregunta: ¿qué hay
    /// al alcance de este punto? Lo pregunta Kael (<c>InteraccionJugador</c>) en cada fotograma, y el
    /// HUD enseña el aviso sobre lo que haya. Funciona como <see cref="Iluminacion"/>.
    /// </summary>
    public static class Interacciones
    {
        private static readonly List<IInteractuable> Registradas = new List<IInteractuable>();

        public static int Cantidad => Registradas.Count;

        public static void Registrar(IInteractuable interactuable)
        {
            if (interactuable != null && !Registradas.Contains(interactuable))
            {
                Registradas.Add(interactuable);
            }
        }

        public static void Quitar(IInteractuable interactuable) => Registradas.Remove(interactuable);

        /// <summary>Lo más cercano a <paramref name="punto"/> que se puede usar y está a su alcance, o null.</summary>
        public static IInteractuable AlAlcance(Vector2 punto)
        {
            IInteractuable mejor = null;
            var mejorDistancia = float.MaxValue;
            for (var i = 0; i < Registradas.Count; i++)
            {
                var candidato = Registradas[i];
                if (candidato == null || !candidato.Disponible)
                {
                    continue;
                }

                var distancia = Vector2.Distance(punto, candidato.PuntoInteraccion);
                if (distancia <= candidato.RadioInteraccion && distancia < mejorDistancia)
                {
                    mejor = candidato;
                    mejorDistancia = distancia;
                }
            }

            return mejor;
        }

        /// <summary>Vacía el registro. Lo usan las pruebas y el arranque sin recarga de dominio.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reiniciar() => Registradas.Clear();
    }
}
