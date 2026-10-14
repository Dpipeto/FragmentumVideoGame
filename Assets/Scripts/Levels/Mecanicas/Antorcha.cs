using System;
using Fragmentum.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// La antorcha del mundo del Miedo (la <c>Torch</c> del Anexo A). La luz enseña lo que hay, pero
    /// despierta lo que dormía: cada antorcha es una decisión.
    ///
    /// - <b>Apagada</b>: Kael la enciende con Interactuar (E) a <see cref="distanciaEncender"/> de su pie.
    /// - <b>Encendida</b>: alumbra <see cref="radio"/> u durante <see cref="duracion"/> s (Anexo A: 8 u y
    ///   15 s) y parpadea los <see cref="parpadeo"/> últimos. Su círculo cuenta como iluminado
    ///   (<see cref="Iluminacion"/>): ahí se ven los cepos y las ramas podridas, y los Espectros no se
    ///   acercan a Kael. Al encenderse lo anuncia (<see cref="AlEncenderse"/>), para que despierten
    ///   los Espectros que duermen en su radio (fase C).
    /// - <b>Consumida</b>: ya ardió. <b>Se enciende una vez por vida</b>: vuelve a estar lista cuando
    ///   Kael reaparece en un checkpoint. Si se pudiera volver a encender, quedarse junto a una sería
    ///   estar a salvo para siempre.
    ///
    /// Es la versión completa de <see cref="ZonaIluminada"/>, el círculo de luz fijo de la Fase 4.
    /// La luz que se ve es una luz 2D; la que cuenta para el juego es su círculo.
    /// </summary>
    [DisallowMultipleComponent]
    public class Antorcha : MonoBehaviour, IFuenteLuz, IInteractuable
    {
        /// <summary>Lo más bajo que llega la luz al parpadear, como fracción de su intensidad.</summary>
        public const float MinimoParpadeo = 0.3f;

        [Tooltip("Radio de su luz, en unidades (Anexo A: 8).")]
        [Min(0.5f)] public float radio = 8f;

        [Tooltip("Segundos que arde (Anexo A: 15).")]
        [Min(0.1f)] public float duracion = 15f;

        [Tooltip("Los últimos segundos, parpadea: avisa de que se acaba.")]
        [Min(0f)] public float parpadeo = 3f;

        [Tooltip("Hasta dónde llega Kael para encenderla, desde el pie de la antorcha.")]
        [Min(0.1f)] public float distanciaEncender = 1.5f;

        [Tooltip("Dónde está la llama, desde el pie: de ahí sale la luz.")]
        [SerializeField] private Vector2 llama = new Vector2(0f, 1.5f);

        [Tooltip("La luz 2D, apagada hasta que se enciende.")]
        [SerializeField] private Light2D luz;

        [SerializeField] private float intensidad = 1.2f;

        [Tooltip("Lo que cambia de color con su estado (la cabeza de la antorcha). Sin arte, un rectángulo.")]
        [SerializeField] private SpriteRenderer cabeza;

        [SerializeField] private Color colorApagada = new Color(0.25f, 0.22f, 0.2f, 1f);
        [SerializeField] private Color colorEncendida = new Color(1f, 0.62f, 0.25f, 1f);
        [SerializeField] private Color colorConsumida = new Color(0.12f, 0.11f, 0.1f, 1f);

        private SeguimientoJugador _kael;
        private float _apagaEn;

        /// <summary>Una antorcha se acaba de encender. Para despertar a los Espectros de su radio (fase C).</summary>
        public static event Action<Antorcha> AlEncenderse;

        /// <summary>Cambió de estado: apagada, encendida o consumida.</summary>
        public event Action<ESTADOS_ANTORCHA> OnEstado;

        public ESTADOS_ANTORCHA Estado { get; private set; }

        /// <summary>Segundos que le quedan encendida (0 si no arde).</summary>
        public float Restante => Estado == ESTADOS_ANTORCHA.LIT ? Mathf.Max(0f, _apagaEn - Time.time) : 0f;

        /// <summary>¿Está en sus últimos segundos?</summary>
        public bool Parpadea => Estado == ESTADOS_ANTORCHA.LIT && Restante <= parpadeo;

        public Light2D Luz => luz;

        // --- IFuenteLuz ---

        public Vector2 Posicion => transform.TransformPoint(llama);

        public float Radio => radio;

        public bool Encendida => Estado == ESTADOS_ANTORCHA.LIT && isActiveAndEnabled;

        // --- IInteractuable ---

        public Vector2 PuntoInteraccion => transform.position;

        public float RadioInteraccion => distanciaEncender;

        public bool Disponible => Estado == ESTADOS_ANTORCHA.UNLIT && isActiveAndEnabled;

        public string Accion => "Encender";

        public void Interactuar(GameObject quien) => Encender();

        /// <summary>
        /// Cuánto brilla la llama (fracción de su intensidad) cuando le quedan <paramref name="restante"/>
        /// segundos: entera hasta los últimos <paramref name="segundosParpadeo"/>, y después parpadea
        /// entre <see cref="MinimoParpadeo"/> y entera, cada vez más deprisa.
        /// </summary>
        public static float FactorParpadeo(float restante, float segundosParpadeo, float ahora)
        {
            if (restante > segundosParpadeo || segundosParpadeo <= 0f)
            {
                return 1f;
            }

            var urgencia = 1f - Mathf.Clamp01(restante / segundosParpadeo);
            var frecuencia = Mathf.Lerp(4f, 12f, urgencia);
            var onda = Mathf.Abs(Mathf.Sin(ahora * frecuencia * Mathf.PI));
            return Mathf.Lerp(MinimoParpadeo, 1f, onda);
        }

        private void Awake()
        {
            if (luz == null)
            {
                luz = GetComponentInChildren<Light2D>(true);
            }

            _kael = new SeguimientoJugador(Reponer);
            Pintar();
        }

        private void OnEnable()
        {
            Iluminacion.Registrar(this);
            Interacciones.Registrar(this);
        }

        private void OnDisable()
        {
            Iluminacion.Quitar(this);
            Interacciones.Quitar(this);
            _kael?.Soltar();
        }

        private void Update()
        {
            // Solo para estar enganchada a Kael y enterarse de cuándo reaparece.
            _ = _kael.Jugador;

            if (Estado != ESTADOS_ANTORCHA.LIT)
            {
                return;
            }

            if (Time.time >= _apagaEn)
            {
                Cambiar(ESTADOS_ANTORCHA.SPENT);
                return;
            }

            if (luz != null)
            {
                luz.intensity = intensidad * FactorParpadeo(Restante, parpadeo, Time.time);
            }
        }

        /// <summary>La enciende si está apagada. Devuelve si se encendió.</summary>
        public bool Encender()
        {
            if (Estado != ESTADOS_ANTORCHA.UNLIT)
            {
                return false;
            }

            _apagaEn = Time.time + duracion;
            Cambiar(ESTADOS_ANTORCHA.LIT);
            AlEncenderse?.Invoke(this);
            return true;
        }

        /// <summary>Vuelve a estar apagada y lista. Lo hace sola cuando Kael reaparece.</summary>
        public void Reponer() => Cambiar(ESTADOS_ANTORCHA.UNLIT);

        private void Cambiar(ESTADOS_ANTORCHA estado)
        {
            var cambio = Estado != estado;
            Estado = estado;
            Pintar();
            if (cambio)
            {
                OnEstado?.Invoke(estado);
            }
        }

        private void Pintar()
        {
            if (luz != null)
            {
                luz.enabled = Estado == ESTADOS_ANTORCHA.LIT;
                luz.intensity = intensidad;
                luz.pointLightOuterRadius = radio;
            }

            if (cabeza != null)
            {
                cabeza.color = Estado switch
                {
                    ESTADOS_ANTORCHA.LIT => colorEncendida,
                    ESTADOS_ANTORCHA.SPENT => colorConsumida,
                    _ => colorApagada
                };
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Encendida ? new Color(1f, 0.7f, 0.3f, 0.6f) : new Color(0.5f, 0.45f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(Posicion, radio);
            Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, distanciaEncender);
        }
    }
}
