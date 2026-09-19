using System;

namespace Fragmentum.Core.FSM
{
    /// <summary>
    /// Marca un controlador que declara una FSM, para que el exportador de diagramas la
    /// encuentre por reflexión.
    ///
    /// La clase marcada debe tener un método estático público, sin parámetros, que devuelva
    /// la tabla ya declarada (por defecto se llama <c>CrearTabla</c>):
    /// <code>
    /// [DeclaraFSM("Enemigo")]
    /// public class EnemyController : MonoBehaviour
    /// {
    ///     public static TablaTransiciones&lt;STATES&gt; CrearTabla() { ... }
    /// }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DeclaraFSMAttribute : Attribute
    {
        public DeclaraFSMAttribute(string nombre, string metodo = "CrearTabla")
        {
            Nombre = nombre;
            Metodo = metodo;
        }

        /// <summary>Nombre legible de la FSM; da nombre al archivo del diagrama.</summary>
        public string Nombre { get; }

        /// <summary>Nombre del método estático que construye la tabla.</summary>
        public string Metodo { get; }
    }
}
