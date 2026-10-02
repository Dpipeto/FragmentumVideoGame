using System;
using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Core.FSM;
using Fragmentum.Data;
using Fragmentum.Enemies.Deteccion;
using Fragmentum.Enemies.States;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Enemies
{
    /// <summary>
    /// Controlador de enemigo: dueño de una FSM real de 6 estados (patrón State). Sustituye al
    /// <c>EnemyController</c> del prototipo, cuya FSM era decorativa (el estado se fijaba en
    /// Start y nunca cambiaba) y cuya IA vivía en condicionales anidados.
    ///
    /// Qué corrige respecto de la v1 (auditoría, sección 3.1):
    /// - Ataca por los dos lados: el ataque ya no está anidado dentro de <c>direction.x &lt; 0</c>.
    /// - El volteo depende de <see cref="EnemyData.spriteMiraDerecha"/>, no de un signo invertido.
    /// - Busca al jugador por tag en tiempo de ejecución (con aviso si no lo encuentra) en vez
    ///   de exigir asignarlo a mano en cada escena.
    /// - Patrulla entre waypoints y, si no tiene, se queda en IDLE sin lanzar excepciones.
    /// - Tiene cooldown de ataque por tiempo, histéresis en la persecución, estado DAMAGED con
    ///   i-frames que interrumpe el ataque y un DEAD real que apaga colisiones y lo destruye.
    /// - La vida es entera y los parámetros viven en <see cref="EnemyData"/>.
    /// - El Animator se fija en el OnEnter de cada estado, no con cuatro SetBool por frame.
    /// - Con arte (el Eco de la Ira), los fotogramas del clip marcan la ventana del golpe y el
    ///   final de la muerte a través de <see cref="EnemigoAnimacion"/>; sin arte, todo va por
    ///   tiempo con los valores de <see cref="EnemyData"/>.
    /// </summary>
    [DeclaraFSM("Enemigo")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        public const string GrupoVivo = "VIVO";

        private static readonly List<EnemyController> ActivosInternos = new List<EnemyController>();

        [Tooltip("Parámetros del enemigo (vida, daño, velocidades, radios, detección).")]
        [SerializeField] private EnemyData datos;

        [Header("Patrulla")]
        [Tooltip("Puntos de la ruta. Sin waypoints el enemigo espera en IDLE.")]
        [SerializeField] private Transform[] waypoints = new Transform[0];

        [Header("Referencias (vacío = se buscan solas)")]
        [Tooltip("Jugador. Vacío = el objeto con el tag Player.")]
        [SerializeField] private Transform jugador;

        [SerializeField] private Animator animator;

        [Tooltip("Hijo que se voltea al girar: sprite y arma.")]
        [SerializeField] private Transform pivote;

        [SerializeField] private SpriteRenderer sprite;

        [Tooltip("Hitbox del arma (capa EnemyWeapon), activa solo durante el golpe.")]
        [SerializeField] private Hitbox arma;

        [Tooltip("Hitbox del cuerpo (capa EnemyWeapon): daño por contacto.")]
        [SerializeField] private Hitbox contacto;

        [Tooltip("Animación con eventos (Ecos). Vacío = se busca en los hijos; si no hay, el golpe y la muerte van por tiempo.")]
        [SerializeField] private EnemigoAnimacion animacion;

        [Header("Depuración")]
        [SerializeField] private ESTADOS_ENEMIGO currentState;
        [SerializeField] private bool registrarTransiciones;

        // Margen sobre la duración del clip de ataque antes de darlo por terminado sin su evento.
        private const float MargenFinAtaque = 0.25f;

        private MaquinaEstados<ESTADOS_ENEMIGO> _maquina;
        private EnemyAttackState _ataque;
        private Rigidbody2D _cuerpo;
        private Collider2D _colisionador;
        private Health _vida;
        private Collider2D _colisionadorJugador;
        private Health _vidaJugador;
        private PlayerAbilities _habilidadesJugador;
        private PlayerModoMovimiento _ruidoJugador;
        private int _indiceWaypoint;
        private float _proximoAtaque = float.NegativeInfinity;
        private float _tiempoUltimoRuido = float.NegativeInfinity;
        private float _proximaBusquedaJugador;
        private bool _avisadoSinJugador;
        private bool _iniciado;
        private ContactFilter2D _filtroEntorno;
        private readonly List<Collider2D> _consulta = new List<Collider2D>(4);

        /// <summary>Enemigos vivos en escena (para depuración y, más adelante, el LevelManager).</summary>
        public static IReadOnlyList<EnemyController> Activos => ActivosInternos;

        /// <summary>Cambio de estado: (anterior, nuevo). Para audio, VFX y UI.</summary>
        public event Action<ESTADOS_ENEMIGO, ESTADOS_ENEMIGO> OnStateChanged;

        public ESTADOS_ENEMIGO EstadoActual => _maquina != null && _maquina.Iniciada ? _maquina.EstadoActual : ESTADOS_ENEMIGO.IDLE;

        public EnemyData Datos => datos;

        public Health Vida => _vida;

        /// <summary>Hacia dónde mira: 1 derecha, -1 izquierda.</summary>
        public int Mirando { get; private set; } = 1;

        public bool TieneWaypoints
        {
            get
            {
                foreach (var w in waypoints)
                {
                    if (w != null) return true;
                }

                return false;
            }
        }

        // --- Contexto para los estados ---

        internal EnemyData D => datos;

        internal Animator Animador => animator;

        internal Hitbox Arma => arma;

        /// <summary>
        /// Hay animación con eventos: los fotogramas del clip abren y cierran el arma, el ataque
        /// acaba con el clip y la muerte la remata <see cref="EnemigoAnimacion"/>.
        /// </summary>
        internal bool AnimadoPorEventos => animacion != null && animacion.Lista;

        /// <summary>Lo más que dura ATTACK: el clip a su ritmo (con margen) o, sin animación, EnemyData.</summary>
        internal float DuracionAtaque => AnimadoPorEventos ? animacion.DuracionAtaque + MargenFinAtaque : datos.duracionAtaque;

        internal float Ahora => Time.time;

        internal bool AtaqueListo => Ahora >= _proximoAtaque;

        internal Golpe UltimoGolpe { get; private set; }

        /// <summary>Un evento de animación avisó del final del ataque (receptor de la v1).</summary>
        internal bool FinAtaquePorAnimacion { get; set; }

        /// <summary>Un evento de animación avisó del final del clip de daño (receptor de la v1).</summary>
        internal bool FinDanioPorAnimacion { get; set; }

        internal bool JugadorVivo => _vidaJugador != null && _vidaJugador.EstaVivo;

        internal Vector2 Centro => _colisionador != null ? (Vector2)_colisionador.bounds.center : (Vector2)transform.position;

        internal Vector2 CentroJugador =>
            _colisionadorJugador != null ? (Vector2)_colisionadorJugador.bounds.center
            : jugador != null ? (Vector2)jugador.position : Centro;

        internal int DireccionHaciaJugador => CentroJugador.x >= Centro.x ? 1 : -1;

        internal bool JugadorEnRangoDeAtaque
        {
            get
            {
                if (!JugadorVivo) return false;
                var delta = CentroJugador - Centro;
                return Mathf.Abs(delta.x) <= datos.attackRadius && Mathf.Abs(delta.y) <= datos.alturaAtaque;
            }
        }

        internal Vector2 WaypointActual
        {
            get
            {
                if (!TieneWaypoints) return transform.position;
                for (var i = 0; i < waypoints.Length; i++)
                {
                    var w = waypoints[(_indiceWaypoint + i) % waypoints.Length];
                    if (w != null) return w.position;
                }

                return transform.position;
            }
        }

        // --- Tabla de transiciones: la especificación de la FSM ---

        public static TablaTransiciones<ESTADOS_ENEMIGO> CrearTabla()
        {
            const ESTADOS_ENEMIGO IDLE = ESTADOS_ENEMIGO.IDLE;
            const ESTADOS_ENEMIGO PATROL = ESTADOS_ENEMIGO.PATROL;
            const ESTADOS_ENEMIGO CHASE = ESTADOS_ENEMIGO.CHASE;
            const ESTADOS_ENEMIGO ATTACK = ESTADOS_ENEMIGO.ATTACK;
            const ESTADOS_ENEMIGO DAMAGED = ESTADOS_ENEMIGO.DAMAGED;
            const ESTADOS_ENEMIGO DEAD = ESTADOS_ENEMIGO.DEAD;

            return new TablaTransiciones<ESTADOS_ENEMIGO>("Enemigo")
                .Inicial(IDLE)
                .DefinirGrupo(GrupoVivo, IDLE, PATROL, CHASE, ATTACK, DAMAGED)
                .Permitir(IDLE, PATROL, "termina la espera y hay waypoints")
                .Permitir(PATROL, IDLE, "llega a un waypoint con espera")
                .Permitir(IDLE, CHASE, "detecta al jugador")
                .Permitir(PATROL, CHASE, "detecta al jugador")
                .Permitir(CHASE, ATTACK, "en rango de ataque y cooldown listo")
                .Permitir(ATTACK, CHASE, "termina el ataque, arranca attackCooldown")
                .Permitir(CHASE, PATROL, "lo pierde (más allá de 1,5 × radio) y hay waypoints")
                .Permitir(CHASE, IDLE, "lo pierde y no hay waypoints")
                .Permitir(IDLE, DAMAGED, "golpe sin i-frames")
                .Permitir(PATROL, DAMAGED, "golpe sin i-frames")
                .Permitir(CHASE, DAMAGED, "golpe sin i-frames")
                .Permitir(ATTACK, DAMAGED, "golpe, interrumpe el ataque")
                .Permitir(DAMAGED, CHASE, "termina damagedDuration")
                .PermitirDesdeGrupo(GrupoVivo, DEAD, "vida 0");
        }

        // --- Ciclo de vida ---

        private void Awake()
        {
            _cuerpo = GetComponent<Rigidbody2D>();
            _colisionador = GetComponent<Collider2D>();
            _vida = GetComponent<Health>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (animacion == null) animacion = GetComponentInChildren<EnemigoAnimacion>();
            if (pivote == null && sprite != null) pivote = sprite.transform.parent != transform ? sprite.transform.parent : sprite.transform;

            _cuerpo.freezeRotation = true;
            _cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;

            _filtroEntorno = new ContactFilter2D();
            _filtroEntorno.SetLayerMask(Capas.Mascara(Capas.Suelo, Capas.Wall));
            _filtroEntorno.useTriggers = false;

            _maquina = new MaquinaEstados<ESTADOS_ENEMIGO>(CrearTabla());
            _maquina.Registrar(new EnemyIdleState(this, _maquina));
            _maquina.Registrar(new EnemyPatrolState(this, _maquina));
            _maquina.Registrar(new EnemyChaseState(this, _maquina));
            _ataque = new EnemyAttackState(this, _maquina);
            _maquina.Registrar(_ataque);
            _maquina.Registrar(new EnemyDamagedState(this, _maquina));
            _maquina.Registrar(new EnemyDeadState(this, _maquina));
            _maquina.OnStateChanged += AlCambiarEstado;
        }

        private void OnEnable()
        {
            ActivosInternos.Add(this);
            _vida.OnGolpeRecibido += AlRecibirGolpe;
            _vida.OnMuerte += AlMorir;
        }

        private void OnDisable()
        {
            ActivosInternos.Remove(this);
            _vida.OnGolpeRecibido -= AlRecibirGolpe;
            _vida.OnMuerte -= AlMorir;
            DesuscribirRuido();
        }

        private void Start()
        {
            AplicarDatos();
            ResolverJugador(true);
            _iniciado = true;
            _maquina.Iniciar(ESTADOS_ENEMIGO.IDLE);
        }

        private void Update()
        {
            if (!_maquina.Iniciada)
            {
                return;
            }

            if (_vidaJugador == null && Ahora >= _proximaBusquedaJugador)
            {
                ResolverJugador(false);
            }

            _maquina.Actualizar();
            currentState = _maquina.EstadoActual;
        }

        private void FixedUpdate()
        {
            if (_maquina.Iniciada)
            {
                _maquina.ActualizarFisica();
            }
        }

        // --- API pública ---

        /// <summary>Cambia los datos (antes de Start, o en caliente para pruebas).</summary>
        public void FijarDatos(EnemyData nuevos)
        {
            datos = nuevos;
            if (_iniciado)
            {
                AplicarDatos();
            }
        }

        public void FijarWaypoints(params Transform[] puntos)
        {
            waypoints = puntos ?? new Transform[0];
            _indiceWaypoint = 0;
        }

        /// <summary>Receptor del evento de animación de la v1 (EnemyAttack.anim): fin del ataque.</summary>
        public void CancelAttack() => FinAtaquePorAnimacion = true;

        /// <summary>Receptor del evento de animación de la v1 (EnemyHurt.anim): fin del daño.</summary>
        public void DesactiveDamage() => FinDanioPorAnimacion = true;

        /// <summary>Evento de animación (primer instante de a2): el arma empieza a hacer daño.</summary>
        public void AbrirGolpe()
        {
            if (EstadoActual == ESTADOS_ENEMIGO.ATTACK)
            {
                _ataque.AbrirGolpe();
            }
        }

        /// <summary>Evento de animación (primer instante de a4): el arma deja de hacer daño.</summary>
        public void CerrarGolpe()
        {
            if (EstadoActual == ESTADOS_ENEMIGO.ATTACK)
            {
                _ataque.CerrarGolpe();
            }
        }

        // --- Consultas y acciones para los estados ---

        /// <summary>
        /// ¿Detecta al jugador? Con <paramref name="persiguiendo"/> el radio se amplía por la
        /// histéresis, para que el enemigo no parpadee entre CHASE y PATROL en el borde.
        /// </summary>
        internal bool JugadorDetectado(bool persiguiendo)
        {
            if (_vidaJugador == null)
            {
                return false;
            }

            var radio = datos.detectionRadius * (persiguiendo ? datos.factorHisteresis : 1f);
            var contexto = new ContextoDeteccion(
                Centro,
                CentroJugador,
                radio,
                JugadorVivo,
                _habilidadesJugador != null && _habilidadesJugador.EsInvisible,
                Ahora,
                _tiempoUltimoRuido,
                _filtroEntorno.layerMask);

            return datos.deteccion != null ? datos.deteccion.Detecta(contexto) : DeteccionData.PorDefecto(contexto);
        }

        internal void ArrancarCooldown() => _proximoAtaque = Ahora + datos.attackCooldown;

        internal bool LlegoAlWaypoint() => Mathf.Abs(WaypointActual.x - transform.position.x) <= datos.toleranciaWaypoint;

        internal void AvanzarWaypoint()
        {
            if (waypoints.Length > 0)
            {
                _indiceWaypoint = (_indiceWaypoint + 1) % waypoints.Length;
            }
        }

        internal void Mirar(int direccion)
        {
            if (direccion != 0)
            {
                Mirando = direccion > 0 ? 1 : -1;
            }
        }

        /// <summary>
        /// Avanza en horizontal (FixedUpdate). Si delante hay un borde o una pared y el enemigo
        /// los evita, se detiene y devuelve <c>false</c>.
        /// </summary>
        internal bool Mover(int direccion, float velocidad)
        {
            Mirar(direccion);
            if (direccion == 0 || (datos.evitarBordes && Bloqueado(direccion)))
            {
                Detener();
                return false;
            }

            var v = _cuerpo.linearVelocity;
            v.x = direccion * velocidad;
            _cuerpo.linearVelocity = v;
            return true;
        }

        internal void Detener()
        {
            var v = _cuerpo.linearVelocity;
            v.x = 0f;
            _cuerpo.linearVelocity = v;
        }

        /// <summary>¿Hay un borde (sin suelo delante) o una pared en esa dirección?</summary>
        internal bool Bloqueado(int direccion)
        {
            if (_colisionador == null)
            {
                return false;
            }

            var b = _colisionador.bounds;
            var frente = direccion > 0 ? b.max.x : b.min.x;

            // Pared: una franja a media altura justo delante.
            if (HayEntorno(new Vector2(frente + direccion * 0.1f, b.center.y), new Vector2(0.1f, b.size.y * 0.5f)))
            {
                return true;
            }

            // Borde: sin suelo bajo el paso siguiente.
            return !HayEntorno(new Vector2(frente + direccion * 0.2f, b.min.y - 0.15f), new Vector2(0.2f, 0.3f));
        }

        internal void AplicarRetroceso(in Golpe golpe)
        {
            var dx = transform.position.x - golpe.Origen.x;
            var direccion = Mathf.Abs(dx) > 0.01f ? Mathf.Sign(dx) : -Mirando;
            var factor = 1f - datos.resistenciaRetroceso;
            _cuerpo.linearVelocity = new Vector2(direccion * datos.retroceso.x * factor, datos.retroceso.y * factor);
        }

        /// <summary>
        /// Muerte real: apaga todas las colisiones y hitbox, detiene la física y programa la
        /// destrucción. En el prototipo <c>death</c> solo congelaba al enemigo, que seguía
        /// bloqueando el paso y podía hacer daño.
        ///
        /// Con animación, el arma y el contacto se apagan ya (un enemigo moribundo no hace daño),
        /// pero el cuerpo sigue hasta que acaba el clip de muerte: entonces
        /// <see cref="EnemigoAnimacion"/> apaga el collider, suelta las partículas, funde el sprite
        /// y lo destruye.
        /// </summary>
        internal void Morir()
        {
            FijarArma(false);
            _cuerpo.linearVelocity = Vector2.zero;
            _cuerpo.bodyType = RigidbodyType2D.Kinematic;

            if (AnimadoPorEventos)
            {
                if (contacto != null)
                {
                    contacto.gameObject.SetActive(false);
                }

                animacion.Morir();
                return;
            }

            foreach (var c in GetComponentsInChildren<Collider2D>())
            {
                c.enabled = false;
            }

            Destroy(gameObject, Mathf.Max(0f, datos.deathDelay));
        }

        internal void FijarArma(bool activa)
        {
            if (arma != null && arma.gameObject.activeSelf != activa)
            {
                arma.gameObject.SetActive(activa);
            }
        }

        // --- Eventos externos: también pasan por TransitionTo y la tabla ---

        private void AlRecibirGolpe(Golpe golpe)
        {
            UltimoGolpe = golpe;
            if (!_maquina.Iniciada || !_vida.EstaVivo)
            {
                return;
            }

            var actual = _maquina.EstadoActual;
            if (actual != ESTADOS_ENEMIGO.DAMAGED && actual != ESTADOS_ENEMIGO.DEAD)
            {
                _maquina.TransitionTo(ESTADOS_ENEMIGO.DAMAGED);
            }
        }

        private void AlMorir()
        {
            if (_maquina.Iniciada && _maquina.EstadoActual != ESTADOS_ENEMIGO.DEAD)
            {
                _maquina.TransitionTo(ESTADOS_ENEMIGO.DEAD);
            }
        }

        private void AlOirRuido(Vector2 posicion, float radio)
        {
            if (Vector2.Distance(posicion, Centro) <= radio)
            {
                _tiempoUltimoRuido = Ahora;
            }
        }

        private void AlCambiarEstado(ESTADOS_ENEMIGO anterior, ESTADOS_ENEMIGO nuevo)
        {
            currentState = nuevo;
            if (registrarTransiciones && anterior != nuevo)
            {
                Debug.Log($"[Enemigo {name}] {anterior} -> {nuevo} (t = {Ahora:0.00})", this);
            }

            OnStateChanged?.Invoke(anterior, nuevo);
        }

        // --- Preparación ---

        private void AplicarDatos()
        {
            if (datos == null)
            {
                Debug.LogWarning($"[Enemigo] '{name}' no tiene EnemyData asignado; usa valores por defecto. Asigna uno de Assets/Data/Enemigos.", this);
                datos = ScriptableObject.CreateInstance<EnemyData>();
            }

            _vida.Inicializar(datos.vidaMaxima, datos.invulnerabilidad);

            if (arma != null)
            {
                arma.Danio = datos.danio;
                arma.gameObject.SetActive(false);
            }

            if (contacto != null)
            {
                contacto.Danio = datos.danioContacto;
                contacto.gameObject.SetActive(datos.danioContacto > 0);
            }

            if (sprite != null)
            {
                sprite.color = datos.tinte;
            }
        }

        private void ResolverJugador(bool primeraVez)
        {
            _proximaBusquedaJugador = Ahora + 1f;

            if (jugador == null)
            {
                var objeto = GameObject.FindGameObjectWithTag(GameTags.Player);
                jugador = objeto != null ? objeto.transform : null;
            }

            if (jugador == null)
            {
                if (primeraVez && !_avisadoSinJugador)
                {
                    _avisadoSinJugador = true;
                    Debug.LogWarning(
                        $"[Enemigo] '{name}' no encuentra al jugador (ningún objeto con el tag Player ni referencia asignada). " +
                        "Patrullará sin perseguir; lo reintenta cada segundo.", this);
                }

                return;
            }

            _vidaJugador = jugador.GetComponentInParent<Health>();
            var cuerpoJugador = jugador.GetComponentInParent<Rigidbody2D>();
            _colisionadorJugador = cuerpoJugador != null
                ? cuerpoJugador.GetComponent<Collider2D>()
                : jugador.GetComponent<Collider2D>();
            _habilidadesJugador = jugador.GetComponentInParent<PlayerAbilities>();

            DesuscribirRuido();
            _ruidoJugador = jugador.GetComponentInParent<PlayerModoMovimiento>();
            if (_ruidoJugador != null)
            {
                _ruidoJugador.OnRuido += AlOirRuido;
            }
        }

        private void DesuscribirRuido()
        {
            if (_ruidoJugador != null)
            {
                _ruidoJugador.OnRuido -= AlOirRuido;
                _ruidoJugador = null;
            }
        }

        private bool HayEntorno(Vector2 centro, Vector2 tamano)
        {
            _consulta.Clear();
            Physics2D.OverlapBox(centro, tamano, 0f, _filtroEntorno, _consulta);
            return _consulta.Count > 0;
        }

        private void LateUpdate()
        {
            if (pivote == null || datos == null)
            {
                return;
            }

            var escala = pivote.localScale;
            escala.x = Mathf.Abs(escala.x) * Mirando * (datos.spriteMiraDerecha ? 1 : -1);
            pivote.localScale = escala;
        }

        private void OnDrawGizmosSelected()
        {
            var d = datos;
            if (d == null)
            {
                return;
            }

            var centro = Application.isPlaying ? Centro : (Vector2)transform.position + Vector2.up;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(centro, d.detectionRadius);
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
            Gizmos.DrawWireSphere(centro, d.detectionRadius * d.factorHisteresis);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(centro, new Vector3(d.attackRadius * 2f, d.alturaAtaque * 2f, 0f));

            Gizmos.color = Color.cyan;
            Vector3? anterior = null;
            foreach (var w in waypoints)
            {
                if (w == null) continue;
                Gizmos.DrawWireSphere(w.position, 0.2f);
                if (anterior.HasValue) Gizmos.DrawLine(anterior.Value, w.position);
                anterior = w.position;
            }

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2.3f, EstadoActual.ToString());
            }
#endif
        }
    }
}
