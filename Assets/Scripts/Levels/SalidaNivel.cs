using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// Final del recorrido: cuando el jugador la cruza, el <see cref="LevelManager"/> declara la
    /// victoria. En los niveles con jefe (Fase 7) la victoria llegará al derrotarlo; la salida
    /// sirve mientras tanto y en los niveles sin jefe (Jardín Etéreo).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SalidaNivel : MonoBehaviour
    {
        [Tooltip("Vacío = el LevelManager de la escena.")]
        [SerializeField] private LevelManager nivel;

        private bool _usada;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D otro)
        {
            if (_usada || otro.isTrigger || otro.attachedRigidbody == null)
            {
                return;
            }

            var jugador = otro.attachedRigidbody.GetComponent<PlayerController>();
            if (jugador == null || !jugador.EstaVivo)
            {
                return;
            }

            if (nivel == null)
            {
                nivel = FindAnyObjectByType<LevelManager>();
            }

            if (nivel == null)
            {
                Debug.LogWarning("[Salida] No hay LevelManager en la escena: la salida no hace nada.", this);
                return;
            }

            _usada = nivel.Completar();
        }

        private void OnDrawGizmos()
        {
            var zona = GetComponent<BoxCollider2D>();
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (zona != null)
            {
                Gizmos.DrawCube(zona.offset, zona.size);
            }
        }
    }
}
