using Fragmentum.Core;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels.Objetos
{
    /// <summary>
    /// Moneda que se recoge al tocarla. Suma en <see cref="EconomyManager"/>; las monedas se
    /// guardan con el resto del progreso. En la v1 se buscaba el tag «Player» y se llamaba a
    /// <c>GameManager.Instance</c> directamente, sin comprobar que existiera.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Moneda : MonoBehaviour
    {
        [Min(1)]
        [SerializeField] private int valor = 1;

        private bool _recogida;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_recogida || other.isTrigger || other.attachedRigidbody == null ||
                other.attachedRigidbody.GetComponent<PlayerController>() == null)
            {
                return;
            }

            _recogida = true;
            var economia = GameManager.Instancia != null ? GameManager.Instancia.Economia : null;
            if (economia != null)
            {
                economia.Sumar(valor);
            }
            else
            {
                Debug.LogWarning("[Moneda] No hay sistemas del juego: la moneda se recoge pero no cuenta.", this);
            }

            Destroy(gameObject);
        }
    }
}
