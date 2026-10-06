using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Torreta de fuego (Anexo A, sección 1 del mundo de la Ira), con su arte. Un bloque de piedra
    /// que cicla por temporizador, esté donde esté el jugador:
    /// - <b>reposo</b> (<see cref="tiempoReposo"/>): el hueco apagado;
    /// - <b>carga</b> (<see cref="tiempoCarga"/>): el hueco se pone blanco. Es el aviso, lo único
    ///   que le da al jugador tiempo de salir: es el tiempo que ajusta la dificultad;
    /// - <b>disparo</b> (<see cref="tiempoDisparo"/>): la llama sale y suelta una bola de fuego.
    ///   Solo aquí hace daño: la hitbox de la llama está apagada el resto del ciclo.
    ///
    /// Dispara hacia donde mira el dibujo: a la derecha, o a la izquierda con la escala X
    /// negativa. En horizontal: se esquiva saltando. El bloque es suelo (se puede pisar); no se
    /// mueve ni se puede destruir. Con <see cref="desfase"/>, dos torretas no disparan a la vez.
    ///
    /// La bola es un prefab (<c>Mecanicas/BolaDeFuego</c>) con sprite provisional hasta que llegue
    /// su arte. Sin prefab, se crea por código.
    /// </summary>
    [DisallowMultipleComponent]
    public class TorretaFuego : MonoBehaviour
    {
        [Header("Ciclo (segundos)")]
        [Tooltip("Reposo: el hueco apagado.")]
        [Min(0.1f)] public float tiempoReposo = 1.6f;

        [Tooltip("Carga: el hueco al blanco. Es el aviso, lo único que le da al jugador tiempo de salir: el tiempo que ajusta la dificultad.")]
        [Min(0.05f)] public float tiempoCarga = 0.7f;

        [Tooltip("Disparo: la llama. Es lo único que hace daño.")]
        [Min(0.05f)] public float tiempoDisparo = 0.35f;

        [Tooltip("Segundos de más en el primer reposo, para que dos torretas cercanas no disparen a la vez.")]
        [Min(0f)] public float desfase;

        [Header("Daño")]
        [Tooltip("Daño de la llama y de la bola.")]
        [Min(1)] public int danio = 1;

        [Header("Bola de fuego")]
        [Tooltip("Prefab de la bola (Mecanicas/BolaDeFuego). Vacío = una bola provisional creada por código.")]
        [SerializeField] private Proyectil bola;

        [Min(0.1f)] public float velocidadBola = 7f;

        [Tooltip("Distancia que recorre la bola antes de apagarse.")]
        [Min(0f)] public float alcanceBola = 15f;

        [Header("Referencias")]
        [SerializeField] private Animator animator;

        [Tooltip("Hitbox de la llama (capa EnemyWeapon). Solo está activa en el disparo.")]
        [SerializeField] private Hitbox llama;

        [Tooltip("De dónde sale la bola: delante del bloque, a la altura de la llama.")]
        [SerializeField] private Transform boca;

        private float _cambioEn;

        /// <summary>Cambió de estado (para audio y VFX).</summary>
        public event Action<ESTADOS_TORRETA> OnEstadoCambiado;

        /// <summary>Disparó una bola.</summary>
        public event Action<Proyectil> OnDisparo;

        public ESTADOS_TORRETA Estado { get; private set; }

        public int Disparos { get; private set; }

        /// <summary>Hacia dónde dispara: 1 a la derecha (como está dibujada), -1 a la izquierda.</summary>
        public int Mirando => transform.lossyScale.x < 0f ? -1 : 1;

        /// <summary>¿Hace daño la llama ahora mismo?</summary>
        public bool LlamaActiva => llama != null && llama.gameObject.activeSelf;

        public Hitbox Llama => llama;

        public Proyectil Bola => bola;

        private Vector2 Boca => boca != null ? (Vector2)boca.position : (Vector2)transform.position;

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
            Entrar(ESTADOS_TORRETA.IDLE);
            _cambioEn += desfase;
        }

        private void OnDisable() => FijarLlama(false);

        private void Update()
        {
            if (Time.time >= _cambioEn)
            {
                switch (Estado)
                {
                    case ESTADOS_TORRETA.IDLE:
                        Entrar(ESTADOS_TORRETA.CHARGE);
                        break;
                    case ESTADOS_TORRETA.CHARGE:
                        Entrar(ESTADOS_TORRETA.FIRE);
                        break;
                    default:
                        Entrar(ESTADOS_TORRETA.IDLE);
                        break;
                }
            }

            // Antes de que el Animator se evalúe en este mismo fotograma (y no en OnEnable, cuando
            // puede no estar listo todavía).
            if (animator != null && animator.isInitialized)
            {
                AnimatorParams.FijarEntero(animator, AnimatorParams.EstadoMecanica, (int)Estado);
            }
        }

        private void Entrar(ESTADOS_TORRETA estado)
        {
            Estado = estado;
            _cambioEn = Time.time + Duracion(estado);
            FijarLlama(estado == ESTADOS_TORRETA.FIRE);
            if (estado == ESTADOS_TORRETA.FIRE)
            {
                Disparar();
            }

            OnEstadoCambiado?.Invoke(estado);
        }

        private float Duracion(ESTADOS_TORRETA estado)
        {
            switch (estado)
            {
                case ESTADOS_TORRETA.IDLE:
                    return tiempoReposo;
                case ESTADOS_TORRETA.CHARGE:
                    return tiempoCarga;
                default:
                    return tiempoDisparo;
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

        private void Disparar()
        {
            var velocidad = new Vector2(Mirando * velocidadBola, 0f);
            var proyectil = bola != null
                ? Proyectil.Disparar(bola, Boca, velocidad, danio, Capas.MascaraJugador, alcanceBola)
                : Proyectil.Disparar($"{name}_BolaDeFuego", Boca, velocidad, 0.3f, danio, Capas.MascaraJugador, alcanceBola, new Color(1f, 0.45f, 0.05f));
            Disparos++;
            OnDisparo?.Invoke(proyectil);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.45f, 0.05f, 0.6f);
            var origen = Boca;
            Gizmos.DrawLine(origen, origen + new Vector2(Mirando * alcanceBola, 0f));
        }
    }
}
