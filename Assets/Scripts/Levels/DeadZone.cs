using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// Zona de caída: mata al instante a lo que entre (jugador o enemigo).
    ///
    /// En el prototipo buscaba el tag "Player" y llamaba a <c>PlayerMovement.takeDamage</c>
    /// con 99 de daño; si el jugador estaba parpadeando tras un golpe, el daño se ignoraba y
    /// caía para siempre. Aquí envía un <see cref="Golpe.Letal"/>, que atraviesa los i-frames
    /// y los escudos, a cualquier <see cref="IGolpeable"/>.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DeadZone : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var raiz = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            var objetivo = raiz.GetComponentInParent<IGolpeable>();

            if (objetivo != null && objetivo.EstaVivo)
            {
                objetivo.RecibirGolpe(Golpe.Letal(transform.position, gameObject));
            }
        }

        private void OnDrawGizmos()
        {
            var zona = GetComponent<BoxCollider2D>();
            if (zona == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(zona.offset, zona.size);
        }
    }
}
