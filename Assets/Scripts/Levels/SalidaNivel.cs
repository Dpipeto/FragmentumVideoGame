using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// Final del recorrido: cuando el jugador la cruza, el <see cref="LevelManager"/> declara la
    /// victoria.
    ///
    /// En los niveles con jefe, la salida no hace nada hasta que la <see cref="ArenaJefe"/> se
    /// abre tras la derrota del jefe: no se puede terminar el nivel esquivando el combate. En los
    /// niveles sin jefe (Jardín Etéreo) está disponible desde el principio.
    ///
    /// Con su <see cref="PuertaDeSalida"/> (el arte de la puerta de cada mundo) se ve siempre:
    /// cerrada mientras no se puede usar y abriéndose al quedar disponible. Sin ella, como antes:
    /// oculta hasta que se abre.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SalidaNivel : MonoBehaviour
    {
        [Tooltip("Vacío = el LevelManager de la escena.")]
        [SerializeField] private LevelManager nivel;

        [Tooltip("Vacío = la arena de jefe de la escena, si la hay.")]
        [SerializeField] private ArenaJefe arena;

        [Tooltip("Opcional: la puerta dibujada. Con ella la salida se ve cerrada hasta que se abre.")]
        [SerializeField] private PuertaDeSalida puerta;

        private bool _usada;

        /// <summary>¿Se puede usar ya? (No, mientras haya un jefe sin derrotar.)</summary>
        public bool Abierta { get; private set; } = true;

        /// <summary>La puerta dibujada, si la tiene.</summary>
        public PuertaDeSalida Puerta => puerta;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void Start()
        {
            if (arena == null)
            {
                arena = FindAnyObjectByType<ArenaJefe>();
            }

            if (arena != null && !arena.Superada)
            {
                Mostrar(false);
                arena.OnArenaSuperada += AlSuperarArena;
            }
            else if (puerta != null)
            {
                puerta.Abrir(alInstante: true);
            }
        }

        private void OnDestroy()
        {
            if (arena != null)
            {
                arena.OnArenaSuperada -= AlSuperarArena;
            }
        }

        private void AlSuperarArena() => Mostrar(true);

        private void Mostrar(bool abierta)
        {
            Abierta = abierta;
            GetComponent<Collider2D>().enabled = abierta;
            if (puerta != null)
            {
                if (abierta)
                {
                    puerta.Abrir();
                }
                else
                {
                    puerta.Cerrar();
                }

                return;
            }

            foreach (var renderer2d in GetComponentsInChildren<Renderer>(true))
            {
                renderer2d.enabled = abierta;
            }
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            if (_usada || !Abierta || otro.isTrigger || otro.attachedRigidbody == null)
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
