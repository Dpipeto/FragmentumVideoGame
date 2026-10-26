using System;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Plataforma inestable (<c>PlatformCrumble</c> del Anexo A, mundo de la Ira), con su arte. El
    /// suelo te falla: se resuelve moviéndose rápido.
    ///
    /// - <b>Intacta</b> hasta que el jugador la pisa.
    /// - <b>Agrietada</b> (<see cref="retardoRotura"/>): las grietas se encienden. Es el aviso y se
    ///   sigue pudiendo pisar. Una vez pisada, se rompe aunque el jugador se baje: las grietas no
    ///   se cierran.
    /// - <b>Se rompe:</b> el collider se apaga en el acto. Se ve partida en tres trozos y después
    ///   el polvo, durante <see cref="duracionCaida"/>, y desaparece.
    /// - A los <see cref="tiempoReaparicion"/> s vuelve intacta, salvo si el jugador está justo
    ///   donde reaparecería (lo empujaría): entonces espera a que se aparte.
    ///
    /// Detecta al jugador con una caja fina justo encima de su superficie, no con colisiones: así
    /// no depende del orden de los contactos ni de si el jugador llega saltando o caminando. El
    /// pivote está en la superficie: el Transform va donde se apoya el pie.
    ///
    /// La plataforma de combustión es otra cosa a propósito, con su propio script: allí el suelo
    /// no falla, lo que mata es el momento.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlataformaInestable : MonoBehaviour
    {
        [Tooltip("Segundos agrietada desde que el jugador la pisa hasta que se rompe. Es el aviso.")]
        [Min(0f)] public float retardoRotura = 0.6f;

        [Tooltip("Segundos que se ve rompiéndose (partida en tres trozos y el polvo) antes de desaparecer.")]
        [Min(0.25f)] public float duracionCaida = 0.6f;

        [Tooltip("Segundos desde que desaparece hasta que vuelve, intacta.")]
        [Min(0.1f)] public float tiempoReaparicion = 3f;

        [SerializeField] private Animator animator;

        [Tooltip("El dibujo: se esconde al desaparecer.")]
        [SerializeField] private SpriteRenderer visual;

        private BoxCollider2D _cuerpo;
        private Bounds _limites;
        private readonly Collider2D[] _encima = new Collider2D[2];
        private ContactFilter2D _filtroJugador;
        private float _rompeEn;
        private float _desapareceEn;
        private float _vuelveEn;

        /// <summary>Cambió de estado: true = se rompió, false = volvió.</summary>
        public event Action<bool> OnCaida;

        public ESTADOS_PLATAFORMA Estado { get; private set; }

        /// <summary>Rota: ya no sostiene (aunque todavía se vean los trozos o el polvo).</summary>
        public bool Caida => Estado == ESTADOS_PLATAFORMA.BROKEN;

        /// <summary>Se ve (intacta, agrietada o rompiéndose); false cuando ya desapareció.</summary>
        public bool Visible => visual == null || visual.enabled;

        /// <summary>¿Sostiene al jugador? (Su collider está activo.)</summary>
        public bool Sostiene => _cuerpo != null && _cuerpo.enabled;

        /// <summary>
        /// La regla del collider: sostiene intacta y agrietada, y deja de sostener en cuanto se
        /// rompe (mientras se ve partida en tres trozos, Plataforma_3_rota, y el polvo). La aplica
        /// este script y la comprueba el verificador del Nivel 1 contra el arte de cada estado.
        /// </summary>
        public static bool SostieneEn(ESTADOS_PLATAFORMA estado) => estado != ESTADOS_PLATAFORMA.BROKEN;

        private void Awake()
        {
            _cuerpo = GetComponent<BoxCollider2D>();
            // Se calculan ahora y a mano: los límites de un collider desactivado valen cero, y si la
            // plataforma se acaba de crear la física aún no los ha actualizado.
            var escala = transform.lossyScale;
            _limites = new Bounds(transform.TransformPoint(_cuerpo.offset), new Vector3(_cuerpo.size.x * Mathf.Abs(escala.x), _cuerpo.size.y * Mathf.Abs(escala.y), 1f));
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            _filtroJugador = new ContactFilter2D();
            _filtroJugador.SetLayerMask(Capas.MascaraJugador);
            _filtroJugador.useTriggers = false;
        }

        private void FixedUpdate()
        {
            var ahora = Time.time;
            switch (Estado)
            {
                case ESTADOS_PLATAFORMA.INTACT:
                    if (JugadorEncima())
                    {
                        Estado = ESTADOS_PLATAFORMA.CRACKED;
                        _rompeEn = ahora + retardoRotura;
                    }

                    break;

                case ESTADOS_PLATAFORMA.CRACKED:
                    if (ahora >= _rompeEn)
                    {
                        Romper(ahora);
                    }

                    break;

                default:
                    if (Visible && ahora >= _desapareceEn && visual != null)
                    {
                        visual.enabled = false;
                    }

                    // No vuelve mientras el jugador esté justo donde reaparecería: lo empujaría.
                    if (ahora >= _vuelveEn && !Hay(_limites.center, _limites.size))
                    {
                        Recomponer();
                    }

                    break;
            }
        }

        private void Update()
        {
            // Antes de que el Animator se evalúe en este fotograma.
            if (animator != null && animator.isInitialized)
            {
                AnimatorParams.FijarEntero(animator, AnimatorParams.EstadoMecanica, (int)Estado);
            }
        }

        private bool JugadorEncima() =>
            Hay(new Vector2(_limites.center.x, _limites.max.y + 0.06f), new Vector2(_limites.size.x * 0.95f, 0.1f));

        private bool Hay(Vector2 centro, Vector2 tamano) => Physics2D.OverlapBox(centro, tamano, 0f, _filtroJugador, _encima) > 0;

        private void Romper(float ahora)
        {
            Estado = ESTADOS_PLATAFORMA.BROKEN;
            _cuerpo.enabled = SostieneEn(Estado);
            _desapareceEn = ahora + duracionCaida;
            _vuelveEn = _desapareceEn + tiempoReaparicion;
            OnCaida?.Invoke(true);
        }

        private void Recomponer()
        {
            Estado = ESTADOS_PLATAFORMA.INTACT;
            _cuerpo.enabled = SostieneEn(Estado);
            if (visual != null)
            {
                visual.enabled = true;
            }

            OnCaida?.Invoke(false);
        }
    }
}
