using System;
using Fragmentum.Core;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Narrative;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// Una memoria del mundo de la Culpa (Anexo A): un objeto del campo de batalla que brilla en ámbar
    /// (la radio, la campana, el casco sobre el fusil o la carta). Con Interactuar (W), a
    /// <see cref="distancia"/> de su pie, Kael la recuerda: una cinemática de unos 5 s
    /// (<see cref="cinematica"/>, su imagen y una línea suya) y el brillo se apaga.
    ///
    /// <b>Para recordar hay que dejarse ver</b> (decisión 12): invisible, el aviso dice
    /// «<see cref="AvisoInvisible"/>» y la memoria no se activa.
    ///
    /// Recordarla cuesta: lo anuncia (<see cref="AlRecordar"/>) y el <see cref="SistemaConsecuencias"/>
    /// vuelve más agresivos a los enemigos de su <see cref="Zona"/>. Se recuerda aunque Kael muera;
    /// al empezar el nivel otra vez, se olvida (la escena vuelve a cargarse). La partida guardada
    /// apunta las que se han mirado alguna vez.
    /// </summary>
    [DisallowMultipleComponent]
    public class MemoriaInteractuable : MonoBehaviour, IInteractuable
    {
        /// <summary>Lo que dice el aviso cuando Kael está invisible.</summary>
        public const string AvisoInvisible = "Para recordar, déjate ver";

        [Tooltip("Qué memoria es (LaOrden, ElPueblo, ElCompanero, LaCarta). Es lo que guarda la partida.")]
        [SerializeField] private string id;

        [Tooltip("La cinemática que se ve al recordarla.")]
        [SerializeField] private CutsceneData cinematica;

        [Tooltip("Los enemigos que haya aquí (en coordenadas del mundo) se vuelven más agresivos al recordarla.")]
        [SerializeField] private Rect zona;

        [Tooltip("Hasta dónde llega Kael para recordarla, desde el pie de la memoria.")]
        [Min(0.1f)] public float distancia = 1.5f;

        [Tooltip("El brillo ámbar: lo que hay que mirar.")]
        [SerializeField] private SpriteRenderer brillo;

        [SerializeField] private Color colorBrillo = new Color(0.88f, 0.69f, 0.38f, 1f);
        [SerializeField] private Color colorApagada = new Color(0.35f, 0.3f, 0.25f, 1f);

        private SeguimientoJugador _kael;

        /// <summary>Alguien recordó una memoria.</summary>
        public static event Action<MemoriaInteractuable> AlRecordar;

        /// <summary>Se recordó esta.</summary>
        public event Action OnRecordada;

        public string Id => id;

        public CutsceneData Cinematica => cinematica;

        /// <summary>Donde sus enemigos se vuelven más agresivos al recordarla.</summary>
        public Rect Zona => zona;

        public bool Recordada { get; private set; }

        public SpriteRenderer Brillo => brillo;

        /// <summary>¿Kael está invisible ahora mismo?</summary>
        public bool KaelInvisible
        {
            get
            {
                var jugador = _kael != null ? _kael.Jugador : null;
                return jugador != null && jugador.Habilidades != null && jugador.Habilidades.EsInvisible;
            }
        }

        // --- IInteractuable ---

        public Vector2 PuntoInteraccion => transform.position;

        public float RadioInteraccion => distancia;

        public bool Disponible => !Recordada && enabled && gameObject.activeInHierarchy;

        public string Accion => KaelInvisible ? AvisoInvisible : "Recordar";

        public bool Usable => !KaelInvisible;

        public void Interactuar(GameObject quien)
        {
            var jugador = quien != null ? quien.GetComponentInParent<PlayerController>() : null;
            if (jugador != null && jugador.Habilidades != null && jugador.Habilidades.EsInvisible)
            {
                return;
            }

            Recordar();
        }

        /// <summary>
        /// La recuerda: apaga su brillo, lo anuncia, lo apunta en la partida y pone su cinemática.
        /// Devuelve si la recordó (una vez por partida del nivel).
        /// </summary>
        public bool Recordar()
        {
            if (Recordada)
            {
                return false;
            }

            Recordada = true;
            Pintar();
            OnRecordada?.Invoke();
            AlRecordar?.Invoke(this);

            var datos = GameManager.Instancia != null ? GameManager.Instancia.Datos : null;
            if (datos != null && !string.IsNullOrEmpty(id) && !datos.memoriasActivadas.Contains(id))
            {
                datos.memoriasActivadas.Add(id);
            }

            if (cinematica != null && GameStateMachine.Instancia != null)
            {
                GameStateMachine.Instancia.IniciarCinematica(cinematica);
            }

            return true;
        }

        private void Awake()
        {
            _kael = new SeguimientoJugador(null);
            Pintar();
        }

        private void OnEnable() => Interacciones.Registrar(this);

        private void OnDisable()
        {
            Interacciones.Quitar(this);
            _kael?.Soltar();
        }

        private void Update()
        {
            if (Recordada || brillo == null)
            {
                return;
            }

            // Late despacio: es lo único cálido del campo.
            var latido = 0.75f + 0.25f * Mathf.Sin(Time.time * 2.5f);
            brillo.color = new Color(colorBrillo.r, colorBrillo.g, colorBrillo.b, colorBrillo.a * latido);
        }

        private void Pintar()
        {
            if (brillo != null)
            {
                brillo.color = Recordada ? colorApagada : colorBrillo;
            }
        }

        /// <summary>Olvida a quien escuchaba. Lo usan las pruebas y el arranque sin recarga de dominio.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reiniciar() => AlRecordar = null;
    }
}
