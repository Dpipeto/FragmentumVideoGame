using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Los efectos de sonido de Kael (patrón Observer, como <see cref="PlayerAnimacion"/>): al entrar en un estado
    /// de la FSM suena el suyo, y al recibir un golpe, el daño. No decide nada: solo escucha.
    /// - JUMP y WALL_JUMP: <see cref="salto"/>; DOUBLE_JUMP: <see cref="dobleSalto"/>.
    /// - DASH: <see cref="dash"/>.
    /// - ATTACK: <see cref="ataque"/>, o <see cref="ataqueCargado"/> si salió cargado.
    /// - Un golpe que quita vida (<see cref="Health.OnGolpeRecibido"/>): <see cref="danio"/>. El que para el
    ///   escudo no llega a quitarla, y no suena a daño.
    ///
    /// Cada vez suena un poco más agudo o más grave (<see cref="variacionTono"/>), para que repetido no suene a
    /// máquina, y pueden sonar varios a la vez (<see cref="Voces"/>). Su volumen, sobre el de «Efectos» de las
    /// opciones (<see cref="AjustesAudio.Efectos"/>). Salen de los proyectos de LMMS de <c>Sonidos/Kael</c>
    /// (<c>Herramientas/exportar_sonidos.py</c>).
    /// </summary>
    [DisallowMultipleComponent]
    public class SonidosKael : MonoBehaviour
    {
        /// <summary>Los que pueden sonar a la vez (un salto que empieza mientras acaba un tajo).</summary>
        public const int Voces = 4;

        [SerializeField] private PlayerController jugador;
        [SerializeField] private PlayerHealth vida;

        [Header("Sonidos (Sonidos/Kael)")]
        [SerializeField] private AudioClip salto;
        [SerializeField] private AudioClip dobleSalto;
        [SerializeField] private AudioClip dash;
        [SerializeField] private AudioClip ataque;
        [SerializeField] private AudioClip ataqueCargado;
        [SerializeField] private AudioClip danio;

        [Tooltip("Sobre el volumen de «Efectos» de las opciones.")]
        [Range(0f, 1f)]
        [SerializeField] private float volumen = 0.8f;

        [Tooltip("Cada vez, hasta tanto más agudo o más grave (0,05 = un 5 %).")]
        [Range(0f, 0.2f)]
        [SerializeField] private float variacionTono = 0.05f;

        private readonly AudioSource[] _fuentes = new AudioSource[Voces];

        // Un azar propio: el de Unity lo siembran algunas pruebas, y un salto no debe moverles la secuencia.
        private readonly System.Random _azar = new System.Random();
        private int _siguiente;

        public AudioClip Salto => salto;

        public AudioClip DobleSalto => dobleSalto;

        public AudioClip Dash => dash;

        public AudioClip Ataque => ataque;

        public AudioClip AtaqueCargado => ataqueCargado;

        public AudioClip Danio => danio;

        public float Volumen => volumen;

        /// <summary>El último que sonó, y por qué fuente (para las pruebas).</summary>
        public AudioClip Ultimo { get; private set; }

        public AudioSource UltimaFuente { get; private set; }

        private void Awake()
        {
            if (jugador == null) jugador = GetComponent<PlayerController>();
            if (vida == null) vida = GetComponent<PlayerHealth>();

            for (var i = 0; i < Voces; i++)
            {
                var fuente = gameObject.AddComponent<AudioSource>();
                fuente.playOnAwake = false;
                fuente.spatialBlend = 0f; // la cámara sigue a Kael: se oye siempre igual
                _fuentes[i] = fuente;
            }
        }

        private void OnEnable()
        {
            if (jugador != null)
            {
                jugador.OnStateChanged += AlCambiarEstado;
            }

            if (vida != null)
            {
                vida.OnGolpeRecibido += AlRecibirGolpe;
            }
        }

        private void OnDisable()
        {
            if (jugador != null)
            {
                jugador.OnStateChanged -= AlCambiarEstado;
            }

            if (vida != null)
            {
                vida.OnGolpeRecibido -= AlRecibirGolpe;
            }
        }

        /// <summary>Lo que suena al entrar en ese estado, o null si no suena nada.</summary>
        public AudioClip DeEstado(ESTADOS_JUGADOR estado, bool cargado = false)
        {
            switch (estado)
            {
                case ESTADOS_JUGADOR.JUMP:
                case ESTADOS_JUGADOR.WALL_JUMP:
                    return salto;
                case ESTADOS_JUGADOR.DOUBLE_JUMP:
                    return dobleSalto;
                case ESTADOS_JUGADOR.DASH:
                    return dash;
                case ESTADOS_JUGADOR.ATTACK:
                    return cargado ? ataqueCargado : ataque;
                default:
                    return null;
            }
        }

        private void AlCambiarEstado(ESTADOS_JUGADOR anterior, ESTADOS_JUGADOR nuevo) =>
            Sonar(DeEstado(nuevo, jugador.AtaqueEnCursoCargado));

        private void AlRecibirGolpe(Golpe golpe) => Sonar(danio);

        private void Sonar(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            var fuente = _fuentes[_siguiente];
            _siguiente = (_siguiente + 1) % Voces;
            fuente.clip = clip;
            fuente.volume = volumen * AjustesAudio.Efectos;
            fuente.pitch = 1f + (float)(_azar.NextDouble() * 2.0 - 1.0) * variacionTono;
            fuente.Play();
            Ultimo = clip;
            UltimaFuente = fuente;
        }
    }
}
