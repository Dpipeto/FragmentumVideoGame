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
    /// Además <b>cura</b>: cada vez que Kael lo toca, también cuando ya está activo, recupera toda
    /// la vida (decisión del 2026-10-12, para todos los niveles).
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

        [Tooltip("Opcional: se tiñe al activarse o, con sus dos dibujos, cambia de dibujo.")]
        [SerializeField] private SpriteRenderer indicador;

        [SerializeField] private Color colorInactivo = new Color(0.55f, 0.55f, 0.6f, 1f);
        [SerializeField] private Color colorActivo = new Color(1f, 0.75f, 0.3f, 1f);

        [Tooltip("Con arte: el checkpoint apagado y encendido (el brasero, el farol). Con los dos, el indicador no se tiñe.")]
        [SerializeField] private Sprite spriteInactivo;
        [SerializeField] private Sprite spriteActivo;

        [Tooltip("Al tocarlo por primera vez, Kael dice la línea «eco de memoria» de los diálogos del nivel.")]
        [SerializeField] private bool ecoDeMemoria;

        [Tooltip("Cada vez que Kael lo toca recupera toda la vida, también si ya estaba activo.")]
        [SerializeField] private bool cura = true;

        private CheckpointManager _gestor;
        private bool _avisadoSinGestor;

        public event Action<Checkpoint> OnActivado;

        /// <summary>Curó a Kael: cuántos corazones recuperó.</summary>
        public event Action<Checkpoint, int> OnCuro;

        public string Id => string.IsNullOrEmpty(id)
            ? $"CP_{Mathf.RoundToInt(transform.position.x)}_{Mathf.RoundToInt(transform.position.y)}"
            : id;

        public Vector2 PuntoReaparicion => puntoReaparicion != null ? (Vector2)puntoReaparicion.position : (Vector2)transform.position;

        public bool Activo { get; private set; }

        /// <summary>¿Suena aquí el eco de memoria del nivel? (Lo dice el <see cref="LevelManager"/>.)</summary>
        public bool EcoDeMemoria => ecoDeMemoria;

        /// <summary>¿Cura a Kael al tocarlo?</summary>
        public bool Cura => cura;

        /// <summary>¿Tiene sus dos dibujos (apagado y encendido)?</summary>
        public bool TieneDibujos => spriteInactivo != null && spriteActivo != null;

        /// <summary>El dibujo que enseña ahora el indicador, o null.</summary>
        public Sprite DibujoActual => indicador != null ? indicador.sprite : null;

        internal void Vincular(CheckpointManager gestor) => _gestor = gestor;

        internal void MarcarActivo(bool activo)
        {
            var estaba = Activo;
            Activo = activo;
            Pintar();

            if (activo && !estaba)
            {
                OnActivado?.Invoke(this);
            }
        }

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void Awake()
        {
            if (!Activo)
            {
                Pintar();
            }
        }

        // Con sus dos dibujos cambia de dibujo; sin ellos, el paño provisional cambia de color.
        private void Pintar()
        {
            if (indicador == null)
            {
                return;
            }

            if (TieneDibujos)
            {
                indicador.sprite = Activo ? spriteActivo : spriteInactivo;
                indicador.color = Color.white;
            }
            else
            {
                indicador.color = Activo ? colorActivo : colorInactivo;
            }
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            // Solo el cuerpo del jugador (no su espada, que también es un trigger) y vivo.
            if (otro.isTrigger || otro.attachedRigidbody == null)
            {
                return;
            }

            var jugador = otro.attachedRigidbody.GetComponent<PlayerController>();
            if (jugador == null || !jugador.EstaVivo)
            {
                return;
            }

            // Cura siempre, también el que ya está activo: volver a él es volver a respirar.
            if (cura && jugador.Vida != null)
            {
                var curado = jugador.Vida.Curar(jugador.Vida.VidaMaxima);
                if (curado > 0)
                {
                    OnCuro?.Invoke(this, curado);
                }
            }

            if (Activo)
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
