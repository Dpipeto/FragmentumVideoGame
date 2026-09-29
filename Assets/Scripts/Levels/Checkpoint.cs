using System;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// Punto de control. Cuando el jugador lo toca, el <see cref="CheckpointManager"/> lo
    /// activa y guarda la partida: tras un Game Over, y también al continuar otro día, el
    /// jugador reaparece aquí en lugar de al principio del nivel (tarea D8: el juego es de
    /// plataformas con daño por contacto y no se puede reiniciar desde cero en cada muerte).
    ///
    /// El id se guarda en la partida, así que debe ser estable: si se deja vacío se deriva de
    /// la posición, y mover el checkpoint invalida los guardados que lo usaban (el jugador
    /// empezaría desde el inicio, con un aviso por consola; nunca un error).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("Identificador estable dentro del nivel (se guarda en la partida). Vacío = se deriva de la posición.")]
        [SerializeField] private string id;

        [Tooltip("Dónde reaparece el jugador. Vacío = en la base del propio checkpoint.")]
        [SerializeField] private Transform puntoReaparicion;

        [Tooltip("Opcional: se tiñe al activarse (bandera provisional hasta que haya arte, PENDIENTES B10).")]
        [SerializeField] private SpriteRenderer indicador;

        [SerializeField] private Color colorInactivo = new Color(0.55f, 0.55f, 0.6f, 1f);
        [SerializeField] private Color colorActivo = new Color(1f, 0.75f, 0.3f, 1f);

        private CheckpointManager _gestor;
        private bool _avisadoSinGestor;

        public event Action<Checkpoint> OnActivado;

        public string Id => string.IsNullOrEmpty(id)
            ? $"CP_{Mathf.RoundToInt(transform.position.x)}_{Mathf.RoundToInt(transform.position.y)}"
            : id;

        public Vector2 PuntoReaparicion => puntoReaparicion != null ? (Vector2)puntoReaparicion.position : (Vector2)transform.position;

        public bool Activo { get; private set; }

        internal void Vincular(CheckpointManager gestor) => _gestor = gestor;

        internal void MarcarActivo(bool activo)
        {
            var estaba = Activo;
            Activo = activo;
            if (indicador != null)
            {
                indicador.color = activo ? colorActivo : colorInactivo;
            }

            if (activo && !estaba)
            {
                OnActivado?.Invoke(this);
            }
        }

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void Awake()
        {
            if (indicador != null && !Activo)
            {
                indicador.color = colorInactivo;
            }
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            // Solo el cuerpo del jugador (no su espada, que también es un trigger) y vivo.
            if (Activo || otro.isTrigger || otro.attachedRigidbody == null)
            {
                return;
            }

            var jugador = otro.attachedRigidbody.GetComponent<PlayerController>();
            if (jugador == null || !jugador.EstaVivo)
            {
                return;
            }

            if (_gestor == null)
            {
                _gestor = FindAnyObjectByType<CheckpointManager>();
                if (_gestor == null)
                {
                    if (!_avisadoSinGestor)
                    {
                        _avisadoSinGestor = true;
                        Debug.LogWarning("[Checkpoint] No hay CheckpointManager en la escena (va con el LevelManager): el checkpoint no se activa.", this);
                    }

                    return;
                }

                _gestor.Registrar(this);
            }

            _gestor.Activar(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Activo ? colorActivo : new Color(1f, 0.75f, 0.3f, 0.6f);
            var baseBandera = (Vector3)PuntoReaparicion;
            Gizmos.DrawLine(baseBandera, baseBandera + Vector3.up * 3f);
            Gizmos.DrawLine(baseBandera + Vector3.up * 3f, baseBandera + new Vector3(1f, 2.6f, 0f));
            Gizmos.DrawLine(baseBandera + new Vector3(1f, 2.6f, 0f), baseBandera + Vector3.up * 2.2f);
        }
    }
}
