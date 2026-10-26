using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Plataforma de combustión (<c>HotPlatform</c>, mundo de la Ira), con su arte. El suelo sigue
    /// ahí; lo que mata es el momento: se resuelve esperando.
    ///
    /// Cicla por temporizador, esté donde esté el jugador:
    /// - <b>apagada</b> (<see cref="tiempoApagada"/>): se pisa sin más;
    /// - <b>calienta</b> (<see cref="tiempoCalienta"/>): las brasas suben entre las rejas. Es el aviso;
    /// - <b>encendida</b> (<see cref="tiempoEncendida"/>): la llama sube de la rejilla. Es lo único
    ///   que hace daño: la hitbox de la llama está apagada el resto del ciclo.
    ///
    /// Nunca desaparece: su collider está siempre activo. Con <see cref="desfase"/>, dos plataformas
    /// seguidas no se encienden a la vez.
    ///
    /// Antes (Fase 9, Anexo A) quemaba si el jugador se quedaba encima más de 2 s. El nuevo encargo
    /// del arte la cambió por este ciclo para que se distinga de la plataforma inestable, que tiene
    /// su propio script: allí el suelo falla y aquí no.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlataformaCombustion : MonoBehaviour
    {
        [Header("Ciclo (segundos)")]
        [Tooltip("Apagada: se pisa sin más.")]
        [Min(0.1f)] public float tiempoApagada = 2f;

        [Tooltip("Calienta: las brasas suben entre las rejas. Es el aviso.")]
        [Min(0.05f)] public float tiempoCalienta = 0.8f;

        [Tooltip("Encendida: la llama. Es lo único que hace daño.")]
        [Min(0.05f)] public float tiempoEncendida = 1f;

        [Tooltip("Segundos de más en el primer ciclo apagada, para que dos plataformas seguidas no se enciendan a la vez.")]
        [Min(0f)] public float desfase;

        [Header("Daño")]
        [Tooltip("Daño de la llama. Mientras se siga encima, vuelve a quemar al pasar los i-frames.")]
        [Min(1)] public int danio = 1;

        [Header("Referencias")]
        [SerializeField] private Animator animator;

        [Tooltip("Hitbox de la llama, encima de la rejilla (capa EnemyWeapon). Solo está activa encendida.")]
        [SerializeField] private Hitbox llama;

        private float _cambioEn;

        /// <summary>Cambió de estado (para audio y VFX).</summary>
        public event Action<ESTADOS_COMBUSTION> OnEstadoCambiado;

        public ESTADOS_COMBUSTION Estado { get; private set; }

        /// <summary>¿Hace daño la llama ahora mismo?</summary>
        public bool LlamaActiva => llama != null && llama.gameObject.activeSelf;

        public Hitbox Llama => llama;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (llama != null)
            {
                llama.Danio = danio;
                llama.Objetivos = Capas.MascaraJugador;
            }
        }

        private void OnEnable()
        {
            Entrar(ESTADOS_COMBUSTION.OFF);
            _cambioEn += desfase;
        }

        private void OnDisable() => FijarLlama(false);

        private void Update()
        {
            if (Time.time >= _cambioEn)
            {
                switch (Estado)
                {
                    case ESTADOS_COMBUSTION.OFF:
                        Entrar(ESTADOS_COMBUSTION.HEATING);
                        break;
                    case ESTADOS_COMBUSTION.HEATING:
                        Entrar(ESTADOS_COMBUSTION.BURNING);
                        break;
                    default:
                        Entrar(ESTADOS_COMBUSTION.OFF);
                        break;
                }
            }

            // Antes de que el Animator se evalúe en este mismo fotograma.
            if (animator != null && animator.isInitialized)
            {
                AnimatorParams.FijarEntero(animator, AnimatorParams.EstadoMecanica, (int)Estado);
            }
        }

        private void Entrar(ESTADOS_COMBUSTION estado)
        {
            Estado = estado;
            _cambioEn = Time.time + Duracion(estado);
            FijarLlama(estado == ESTADOS_COMBUSTION.BURNING);
            OnEstadoCambiado?.Invoke(estado);
        }

        private float Duracion(ESTADOS_COMBUSTION estado)
        {
            switch (estado)
            {
                case ESTADOS_COMBUSTION.OFF:
                    return tiempoApagada;
                case ESTADOS_COMBUSTION.HEATING:
                    return tiempoCalienta;
                default:
                    return tiempoEncendida;
            }
        }

        private void FijarLlama(bool activa)
        {
            if (llama != null && llama.gameObject.activeSelf != activa)
            {
                llama.Danio = danio;
                llama.gameObject.SetActive(activa);
            }
        }
    }
}
