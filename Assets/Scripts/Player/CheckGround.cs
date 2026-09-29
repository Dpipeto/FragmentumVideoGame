using System;
using System.Collections.Generic;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Detección de suelo por consulta física, evaluada una vez por frame.
    ///
    /// Corrige el defecto 3.3 del prototipo, donde <c>isGrounded</c> era un bool que se
    /// encendía y apagaba con OnTriggerEnter/Exit: al salir de una de dos plataformas
    /// solapadas se marcaba <c>false</c> aunque el jugador siguiera apoyado en la otra.
    /// Aquí no hay estado que se pueda desincronizar: cada frame se pregunta al motor de
    /// física qué hay bajo los pies, con una capa (no un tag) como filtro.
    /// </summary>
    [DisallowMultipleComponent]
    public class CheckGround : MonoBehaviour
    {
        [Tooltip("Collider del cuerpo. Si se deja vacío se usa el del mismo objeto.")]
        [SerializeField] private Collider2D cuerpo;

        [Tooltip("Capas que cuentan como suelo. Si se deja vacío: 'suelo' y 'Wall'.")]
        [SerializeField] private LayerMask capasSuelo;

        [Tooltip("Grosor de la caja de detección bajo los pies, en unidades.")]
        [SerializeField] private float grosor = 0.1f;

        [Tooltip("Ancho de la caja respecto del cuerpo. Menor que 1 para no confundir una pared lateral con suelo.")]
        [SerializeField, Range(0.5f, 1f)] private float anchoRelativo = 0.9f;

        private readonly List<Collider2D> _contactos = new List<Collider2D>(8);
        private ContactFilter2D _filtro;
        private bool _preparado;

        /// <summary>¿Hay suelo bajo los pies en este frame?</summary>
        public bool EnSuelo { get; private set; }

        /// <summary>Número de colliders de suelo que se pisan a la vez (2 sobre plataformas solapadas).</summary>
        public int Contactos => _contactos.Count;

        /// <summary>Collider que se pisa. Lo usarán las plataformas que se desmoronan y el hielo.</summary>
        public Collider2D SueloActual { get; private set; }

        public event Action OnAterrizo;
        public event Action OnDespego;

        private void Awake() => Preparar();

        private void Preparar()
        {
            if (cuerpo == null)
            {
                cuerpo = GetComponent<Collider2D>();
            }

            if (cuerpo == null)
            {
                Debug.LogWarning(
                    "[CheckGround] No hay Collider2D del cuerpo asignado ni en el mismo objeto. " +
                    "El jugador nunca estará en el suelo.", this);
            }

            var mascara = capasSuelo.value != 0 ? capasSuelo.value : Capas.Mascara(Capas.Suelo, Capas.Wall);
            _filtro = new ContactFilter2D();
            _filtro.SetLayerMask(mascara);
            _filtro.useTriggers = false;
            _preparado = true;
        }

        /// <summary>Consulta la física. La llama el jugador al principio de cada frame, antes de la FSM.</summary>
        public void Evaluar()
        {
            if (!_preparado)
            {
                Preparar();
            }

            var antes = EnSuelo;
            _contactos.Clear();

            if (cuerpo != null && cuerpo.enabled)
            {
                var (centro, tamano) = CajaDeteccion(cuerpo.bounds);
                Physics2D.OverlapBox(centro, tamano, 0f, _filtro, _contactos);
                QuitarColliderPropio();
            }

            EnSuelo = _contactos.Count > 0;
            SueloActual = EnSuelo ? _contactos[0] : null;

            if (EnSuelo && !antes)
            {
                OnAterrizo?.Invoke();
            }
            else if (!EnSuelo && antes)
            {
                OnDespego?.Invoke();
            }
        }

        private (Vector2 centro, Vector2 tamano) CajaDeteccion(Bounds b)
        {
            return (new Vector2(b.center.x, b.min.y), new Vector2(b.size.x * anchoRelativo, grosor * 2f));
        }

        // Por si alguna pieza del propio jugador quedara en una capa de suelo.
        private void QuitarColliderPropio()
        {
            var propio = cuerpo.attachedRigidbody;
            if (propio == null)
            {
                return;
            }

            for (var i = _contactos.Count - 1; i >= 0; i--)
            {
                if (_contactos[i] == null || _contactos[i].attachedRigidbody == propio)
                {
                    _contactos.RemoveAt(i);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            var c = cuerpo != null ? cuerpo : GetComponent<Collider2D>();
            if (c == null)
            {
                return;
            }

            var (centro, tamano) = CajaDeteccion(c.bounds);
            Gizmos.color = EnSuelo ? Color.green : Color.red;
            Gizmos.DrawWireCube(centro, tamano);
        }
    }
}
