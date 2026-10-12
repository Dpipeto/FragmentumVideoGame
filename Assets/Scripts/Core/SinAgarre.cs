using UnityEngine;

namespace Fragmentum.Core
{
    /// <summary>
    /// Marca una pared en la que Kael no se agarra: ni se desliza por ella ni salta desde ella
    /// (<c>PlayerSensors</c> no la cuenta como pared). Sigue siendo sólida, y su parte de arriba se
    /// pisa como cualquier otra.
    ///
    /// Es el hielo liso del mundo de la Tristeza: la muralla que cierra la plaza no se escala con
    /// el salto de pared, porque por arriba no hay salida.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class SinAgarre : MonoBehaviour
    {
    }
}
