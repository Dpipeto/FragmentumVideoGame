using System;
using System.Collections.Generic;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Elige la animación de Kael según el estado de la FSM (patrón Observer sobre
    /// <see cref="PlayerController.OnStateChanged"/>).
    ///
    /// La FSM del código es la única que decide. El Animator no repite la lógica con
    /// transiciones propias (salvo encadenar un clip con su bucle): solo guarda los clips, y este
    /// componente reproduce el que corresponde al entrar en cada estado. Así no hay dos máquinas
    /// de estados que puedan contradecirse, que era el problema del prototipo.
    ///
    /// La tabla <see cref="clips"/> se edita en el Inspector. Un estado sin arte propio apunta
    /// al clip más parecido (por ejemplo, CROUCH reproduce el idle) hasta que exista el suyo.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerAnimacion : MonoBehaviour
    {
        /// <summary>Qué estado del Animator reproduce un estado de la FSM.</summary>
        [Serializable]
        public struct ClipDeEstado
        {
            public ESTADOS_JUGADOR estado;

            [Tooltip("Estado del Animator que se reproduce. Vacío = sigue sonando el clip anterior.")]
            public string estadoAnimator;

            [Tooltip("Vuelve al primer fotograma aunque ese clip ya esté sonando (doble salto, wall jump).")]
            public bool reiniciar;

            public ClipDeEstado(ESTADOS_JUGADOR estado, string estadoAnimator, bool reiniciar = false)
            {
                this.estado = estado;
                this.estadoAnimator = estadoAnimator;
                this.reiniciar = reiniciar;
            }
        }

        // Nombres de los estados del Kael.controller; los crea ConstructorArteKael.
        public const string Idle = "Idle";
        public const string Correr = "Correr";
        public const string Salto = "Salto";
        public const string Caida = "Caida";
        public const string CaidaBucle = "CaidaBucle";
        public const string Ataque = "Ataque";
        public const string Dash = "Dash";

        [SerializeField] private PlayerController jugador;

        [Tooltip("Animator del sprite (hijo Pivote/Sprite).")]
        [SerializeField] private Animator animator;

        [SerializeField] private List<ClipDeEstado> clips = TablaPorDefecto();

        private readonly Dictionary<ESTADOS_JUGADOR, ClipDeEstado> _porEstado = new Dictionary<ESTADOS_JUGADOR, ClipDeEstado>();
        private readonly HashSet<string> _avisados = new HashSet<string>();
        private string _ultimo;

        /// <summary>
        /// Tabla con el arte que existe hoy: idle, correr, salto (subida y caída), ataque y dash.
        /// Los estados sin clip propio usan el más parecido; están en PENDIENTES.md (apartado B3).
        /// </summary>
        public static List<ClipDeEstado> TablaPorDefecto() => new List<ClipDeEstado>
        {
            new ClipDeEstado(ESTADOS_JUGADOR.IDLE, Idle),
            new ClipDeEstado(ESTADOS_JUGADOR.RUN, Correr),
            new ClipDeEstado(ESTADOS_JUGADOR.CROUCH, Idle),
            new ClipDeEstado(ESTADOS_JUGADOR.JUMP, Salto, reiniciar: true),
            new ClipDeEstado(ESTADOS_JUGADOR.DOUBLE_JUMP, Salto, reiniciar: true),
            new ClipDeEstado(ESTADOS_JUGADOR.FALL, Caida),
            new ClipDeEstado(ESTADOS_JUGADOR.WALL_SLIDE, Caida),
            new ClipDeEstado(ESTADOS_JUGADOR.WALL_JUMP, Salto, reiniciar: true),
            new ClipDeEstado(ESTADOS_JUGADOR.DASH, Dash, reiniciar: true),
            new ClipDeEstado(ESTADOS_JUGADOR.ATTACK, Ataque, reiniciar: true),
            new ClipDeEstado(ESTADOS_JUGADOR.DAMAGED, string.Empty),
            new ClipDeEstado(ESTADOS_JUGADOR.DEAD, Idle),
        };

        /// <summary>Estado del Animator que reproduce <paramref name="estado"/>; vacío si no cambia de clip.</summary>
        public string EstadoAnimatorDe(ESTADOS_JUGADOR estado) =>
            _porEstado.TryGetValue(estado, out var clip) ? clip.estadoAnimator : string.Empty;

        /// <summary>Último estado del Animator pedido (vacío si todavía ninguno).</summary>
        public string Ultimo => _ultimo ?? string.Empty;

        private void Awake()
        {
            if (jugador == null) jugador = GetComponent<PlayerController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            _porEstado.Clear();
            foreach (var clip in clips)
            {
                _porEstado[clip.estado] = clip;
            }
        }

        // En OnEnable, antes del Start del controlador: así llega el primer IDLE de la FSM.
        private void OnEnable()
        {
            _ultimo = null; // al reactivarse, el Animator vuelve a su estado por defecto

            if (jugador != null)
            {
                jugador.OnStateChanged += AlCambiarEstado;
            }
        }

        private void OnDisable()
        {
            if (jugador != null)
            {
                jugador.OnStateChanged -= AlCambiarEstado;
            }
        }

        private void AlCambiarEstado(ESTADOS_JUGADOR anterior, ESTADOS_JUGADOR nuevo)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return; // sin arte: el SpriteProvisional ya avisó
            }

            if (nuevo == ESTADOS_JUGADOR.ATTACK)
            {
                // El clip de ataque dura 1 s: se acelera para que ocupe el ataque entero, y el
                // cargado, que dura más, se ve más lento sin necesitar un clip propio.
                var duracion = Mathf.Max(0.05f, jugador.DuracionAtaqueEnCurso);
                AnimatorParams.FijarFloat(animator, AnimatorParams.VelocidadAtaque, 1f / duracion);
            }
            else if (nuevo == ESTADOS_JUGADOR.DASH)
            {
                // Igual con el dash: su clip dura 1 s y ocupa el dash entero, dure lo que dure.
                var duracion = Mathf.Max(0.05f, jugador.Parametros.dashDuration);
                AnimatorParams.FijarFloat(animator, AnimatorParams.VelocidadDash, 1f / duracion);
            }

            if (_porEstado.TryGetValue(nuevo, out var clip))
            {
                Reproducir(clip);
            }
        }

        private void Reproducir(ClipDeEstado clip)
        {
            var nombre = clip.estadoAnimator;
            if (string.IsNullOrEmpty(nombre) || (nombre == _ultimo && !clip.reiniciar))
            {
                return;
            }

            var hash = Animator.StringToHash(nombre);
            if (!animator.HasState(0, hash))
            {
                if (_avisados.Add(nombre))
                {
                    Debug.LogWarning(
                        $"[Animación] El Animator de '{name}' no tiene el estado '{nombre}' (lo pide " +
                        $"{clip.estado}). Se mantiene el clip anterior. Ver PENDIENTES.md, B3.", this);
                }

                return;
            }

            animator.Play(hash, 0, 0f);
            _ultimo = nombre;
        }
    }
}
