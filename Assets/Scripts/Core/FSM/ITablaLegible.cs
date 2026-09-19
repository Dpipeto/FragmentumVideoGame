using System.Collections.Generic;

namespace Fragmentum.Core.FSM
{
    /// <summary>
    /// Vista de una tabla de transiciones sin genéricos, para que herramientas como el
    /// exportador de diagramas puedan leerla por reflexión sin conocer el tipo del enum.
    /// </summary>
    public interface ITablaLegible
    {
        /// <summary>Nombre legible de la FSM (por ejemplo, "Jugador").</summary>
        string Nombre { get; }

        /// <summary>Estado inicial declarado, o cadena vacía si no se declaró.</summary>
        string EstadoInicial { get; }

        /// <summary>
        /// Transiciones declaradas. <c>grupoOrigen</c> viene relleno cuando la transición
        /// se declaró desde un superestado; en el diagrama se dibuja desde el grupo.
        /// </summary>
        IEnumerable<(string desde, string hacia, string condicion, string grupoOrigen)> TransicionesLegibles { get; }

        /// <summary>Grupos (superestados) declarados y los estados que contienen.</summary>
        IEnumerable<(string grupo, IReadOnlyList<string> estados)> GruposLegibles { get; }
    }
}
