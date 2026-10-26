using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Core.Combate
{
    /// <summary>
    /// Zona de daño reutilizable: la espada del jugador, el arma de un enemigo, el mandoble
    /// del jefe o una explosión. Reparte <see cref="Golpe"/> a lo que toque, filtrando por
    /// capa de física en vez de por tag.
    ///
    /// Con esto desaparece el fuego amigo del prototipo, donde cualquier objeto con el tag
    /// "Weapon" dañaba a todo: un enemigo hería a otro y rompía las cajas.
    ///
    /// El collider debe ser trigger. Se activa y desactiva desde la animación de ataque o
    /// desde el estado ATTACK de la FSM.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hitbox : MonoBehaviour
    {
        [Header("Daño")]
        [Tooltip("Puntos de vida que quita cada impacto.")]
        [SerializeField] private int danio = 1;

        [Tooltip("Capas a las que puede golpear. El jugador golpea a Enemy; los enemigos, a Player.")]
        [SerializeField] private LayerMask objetivos;

        [Tooltip("Atraviesa i-frames y escudos. Solo para lava, pinchos y muerte instantánea.")]
        [SerializeField] private bool ignorarInvulnerabilidad;

        [Header("Comportamiento")]
        [Tooltip("Golpear a cada objetivo una sola vez mientras la hitbox esté activa.")]
        [SerializeField] private bool golpearUnaVezPorActivacion = true;

        private readonly HashSet<GameObject> _yaGolpeados = new HashSet<GameObject>();

        /// <summary>El golpe salió cargado (habilidad Ataque Cargado).</summary>
        public bool Cargado { get; set; }

        /// <summary>Daño base, para que las habilidades lo modifiquen en tiempo de ejecución.</summary>
        public int Danio
        {
            get => danio;
            set => danio = Mathf.Max(0, value);
        }

        /// <summary>Capas a las que golpea. Para hitbox creadas por código (proyectiles, pruebas).</summary>
        public LayerMask Objetivos
        {
            get => objetivos;
            set => objetivos = value;
        }

        /// <summary>
        /// Con <c>true</c> golpea a cada objetivo una vez por activación (espadas); con
        /// <c>false</c> golpea mientras haya contacto y los i-frames del objetivo marcan el ritmo
        /// (pinchos, daño por contacto).
        /// </summary>
        public bool GolpearUnaVezPorActivacion
        {
            get => golpearUnaVezPorActivacion;
            set => golpearUnaVezPorActivacion = value;
        }

        /// <summary>Impacto aceptado por el objetivo. Lo escuchan el audio y los VFX.</summary>
        public event Action<IGolpeable, Golpe> OnImpacto;

        private void Reset()
        {
            var collider2d = GetComponent<Collider2D>();
            if (collider2d != null)
            {
                collider2d.isTrigger = true;
            }
        }

        private void OnEnable() => _yaGolpeados.Clear();

        private void OnDisable() => _yaGolpeados.Clear();

        private void OnTriggerEnter2D(Collider2D other) => Intentar(other);

        // También en Stay: si el objetivo ya estaba dentro cuando se activó la hitbox,
        // Enter no vuelve a dispararse y el golpe se perdería.
        private void OnTriggerStay2D(Collider2D other) => Intentar(other);

        private void Intentar(Collider2D other)
        {
            if (other == null || !Capas.EnMascara(other.gameObject, objetivos))
            {
                return;
            }

            // El collider puede ser un hijo (los pies, una zona de detección): el componente
            // de vida vive en la raíz del Rigidbody.
            var raiz = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

            if (golpearUnaVezPorActivacion && !_yaGolpeados.Add(raiz))
            {
                return;
            }

            var objetivo = raiz.GetComponentInParent<IGolpeable>();
            if (objetivo == null || !objetivo.EstaVivo)
            {
                return;
            }

            var golpe = new Golpe(danio, transform.position, gameObject, Cargado, ignorarInvulnerabilidad);
            if (objetivo.RecibirGolpe(golpe))
            {
                OnImpacto?.Invoke(objetivo, golpe);
            }
        }

        /// <summary>Permite volver a golpear a los mismos objetivos (nuevo barrido del ataque).</summary>
        public void ReiniciarObjetivos() => _yaGolpeados.Clear();
    }
}
