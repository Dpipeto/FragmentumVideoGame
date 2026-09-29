using System;
using System.Collections;
using System.Collections.Generic;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Bosses.States;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Core.FSM;
using Fragmentum.Data;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Base de los jefes, los Arquitectos del Alma (tarea C). Es una FSM por fases sobre el mismo
    /// núcleo que el jugador y los enemigos: PHASE_1 → PHASE_2 → PHASE_3 según la vida que le
    /// queda, VULNERABLE cuando un patrón o el propio jefe abre la ventana, y DEFEATED.
    ///
    /// - <b>Template Method:</b> esta clase lleva el flujo (fases, ventana vulnerable, derrota,
    ///   reinicio) y cada jefe redefine solo sus pasos: <see cref="CondicionDerrota"/>,
    ///   <see cref="AlActivarse"/>, <see cref="AlCambiarDeFase"/>, <see cref="AlSerDerrotado"/> y
    ///   <see cref="AlReiniciarse"/>.
    /// - <b>Strategy:</b> lo que hace cada fase es un dato: la lista de <see cref="PatronAtaque"/>
    ///   de su <see cref="FaseJefe"/>.
    /// - <b>Sin estado DAMAGED:</b> un golpe solo hace parpadear al jefe y no interrumpe el ataque.
    ///   En la v1 un golpe podía dejar al jefe congelado.
    ///
    /// El jefe espera quieto hasta que la arena lo activa (<see cref="Activar"/>). Si el jugador
    /// muere durante el combate, la arena lo devuelve al principio (<see cref="Reiniciar"/>).
    /// </summary>
    [DeclaraFSM("Jefe")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(SaludJefe))]
    public abstract class BossController : MonoBehaviour
    {
        public const string GrupoCombate = "COMBATE";

        private const float DuracionParpadeo = 0.12f;

        [Tooltip("Vida, fases, patrones y ventana vulnerable del jefe.")]
        [SerializeField] protected JefeData datos;

        [Header("Referencias (vacío = se buscan solas)")]
        [Tooltip("Hijo que se voltea al girar: sprite y armas.")]
        [SerializeField] private Transform pivote;

        [SerializeField] private SpriteRenderer sprite;

        [SerializeField] private Animator animator;

        [Tooltip("Hitbox del cuerpo (capa EnemyWeapon): daño por contacto durante el combate.")]
        [SerializeField] private Hitbox contacto;

        [Header("Depuración")]
        [SerializeField] private ESTADOS_JEFE currentState;

        [SerializeField] private bool registrarTransiciones;

        private readonly List<GameObject> _efectos = new List<GameObject>();
        private readonly int[] _siguientePatron = new int[JefeData.MaxFases];
        private MaquinaEstados<ESTADOS_JEFE> _maquina;
        private Rigidbody2D _cuerpo;
        private Collider2D _colisionador;
        private SaludJefe _vida;
        private Transform _jugador;
        private Collider2D _colisionadorJugador;
        private Health _vidaJugador;
        private Vector3 _posicionInicial;
        private Color _colorBase = Color.white;
        private Vector2 _limites;
        private bool _hayLimites;
        private int _faseNotificada = -1;
        private float _ventanaPedida = -1f;
        private float _duracionVentana;
        private float _finParpadeo = float.NegativeInfinity;
        private float _finAviso = float.NegativeInfinity;
        private bool _derrotado;

        /// <summary>Cambio de estado: (anterior, nuevo). Para audio, VFX y depuración.</summary>
        public event Action<ESTADOS_JEFE, ESTADOS_JEFE> OnStateChanged;

        /// <summary>Empezó una fase (1, 2 o 3). No se repite al volver de la ventana vulnerable.</summary>
        public event Action<int> OnFaseCambiada;

        /// <summary>
        /// El jefe fue derrotado. La arena lo convierte en la recompensa del nivel: fragmento de
        /// identidad y habilidad (tarea C).
        /// </summary>
        public event Action<BossController> OnJefeDerrotado;

        public JefeData Datos => datos;

        public SaludJefe Vida => _vida;

        public ESTADOS_JEFE EstadoActual => _maquina != null && _maquina.Iniciada ? _maquina.EstadoActual : ESTADOS_JEFE.PHASE_1;

        /// <summary>¿Está combatiendo? (Activado y aún sin derrotar.)</summary>
        public bool EnCombate => _maquina != null && _maquina.Iniciada && !_derrotado;

        public bool Derrotado => _derrotado;

        /// <summary>Fase actual (1, 2 o 3); 0 si todavía no ha empezado el combate.</summary>
        public int Fase => _faseNotificada + 1;

        public bool EsVulnerable => EnCombate && _maquina.EstadoActual == ESTADOS_JEFE.VULNERABLE;

        /// <summary>Patrón que se está ejecutando ahora, o null (entre patrones o fuera de combate).</summary>
        public PatronAtaque PatronActual { get; private set; }

        /// <summary>Hacia dónde mira: 1 derecha, -1 izquierda.</summary>
        public int Mirando { get; private set; } = -1;

        /// <summary>Límites horizontales de la arena (mínimo, máximo) para moverse y atacar.</summary>
        public Vector2 Limites => _hayLimites ? _limites : new Vector2(_posicionInicial.x - 12f, _posicionInicial.x + 12f);

        // --- Contexto para los estados y los patrones ---

        public Vector2 Centro => _colisionador != null ? (Vector2)_colisionador.bounds.center : (Vector2)transform.position;

        public float MedioAncho => _colisionador != null ? _colisionador.bounds.extents.x : 1f;

        /// <summary>Altura de los pies del jefe.</summary>
        public float SueloY => _colisionador != null ? _colisionador.bounds.min.y : transform.position.y;

        public bool JugadorVivo => _vidaJugador != null && _vidaJugador.EstaVivo;

        public Vector2 CentroJugador =>
            _colisionadorJugador != null ? (Vector2)_colisionadorJugador.bounds.center
            : _jugador != null ? (Vector2)_jugador.position : Centro;

        public int DireccionHaciaJugador => CentroJugador.x >= Centro.x ? 1 : -1;

        /// <summary>Distancia horizontal entre el centro del jefe y el del jugador.</summary>
        public float DistanciaAlJugador => Mathf.Abs(CentroJugador.x - Centro.x);

        /// <summary>
        /// Color normal del jefe, sin aviso ni parpadeo. Lo usa el Espectro para hacerse casi
        /// invisible; al reiniciar el combate vuelve al tinte de sus datos.
        /// </summary>
        protected Color ColorBase
        {
            get => _colorBase;
            set => _colorBase = value;
        }

        internal float Fraccion => _vida.VidaMaxima > 0 ? (float)_vida.Vida / _vida.VidaMaxima : 0f;

        /// <summary>Fase (0, 1 o 2) que corresponde a la vida que le queda.</summary>
        internal int FaseSegunVida => Mathf.Clamp(datos.FaseParaVida(Fraccion), 0, datos.NumeroFases - 1);

        internal bool DebeSerDerrotado => CondicionDerrota();

        internal float DuracionVentana => _duracionVentana;

        // --- Especificación: tabla de transiciones ---

        /// <summary>
        /// Función de transición de los jefes: la misma para los cinco, que solo cambian sus
        /// datos y sus pasos. Es la fuente del diagrama <c>Docs/FSM/Jefe.md</c>.
        /// </summary>
        public static TablaTransiciones<ESTADOS_JEFE> CrearTabla()
        {
            const ESTADOS_JEFE PHASE_1 = ESTADOS_JEFE.PHASE_1;
            const ESTADOS_JEFE PHASE_2 = ESTADOS_JEFE.PHASE_2;
            const ESTADOS_JEFE PHASE_3 = ESTADOS_JEFE.PHASE_3;
            const ESTADOS_JEFE VULNERABLE = ESTADOS_JEFE.VULNERABLE;
            const ESTADOS_JEFE DEFEATED = ESTADOS_JEFE.DEFEATED;

            return new TablaTransiciones<ESTADOS_JEFE>("Jefe")
                .Inicial(PHASE_1)
                .DefinirGrupo(GrupoCombate, PHASE_1, PHASE_2, PHASE_3, VULNERABLE)
                .Permitir(PHASE_1, PHASE_2, "vida <= umbral de la fase 2")
                .Permitir(PHASE_2, PHASE_3, "vida <= umbral de la fase 3, si existe")
                .Permitir(PHASE_1, VULNERABLE, "AbrirVentanaVulnerable()")
                .Permitir(PHASE_2, VULNERABLE, "AbrirVentanaVulnerable()")
                .Permitir(PHASE_3, VULNERABLE, "AbrirVentanaVulnerable()")
                .Permitir(VULNERABLE, PHASE_1, "termina la ventana; vida por encima de la fase 2")
                .Permitir(VULNERABLE, PHASE_2, "termina la ventana; vida entre umbrales")
                .Permitir(VULNERABLE, PHASE_3, "termina la ventana; vida en la fase 3")
                .PermitirDesdeGrupo(GrupoCombate, DEFEATED, "CondicionDerrota()");
        }

        internal static ESTADOS_JEFE EstadoDeFase(int fase) =>
            fase <= 0 ? ESTADOS_JEFE.PHASE_1 : fase == 1 ? ESTADOS_JEFE.PHASE_2 : ESTADOS_JEFE.PHASE_3;

        // --- Template Method: los pasos que cada jefe puede redefinir ---

        /// <summary>¿Está derrotado? Por defecto, sin vida. El jefe final lo cambia por «integración completada».</summary>
        protected virtual bool CondicionDerrota() => !_vida.EstaVivo;

        /// <summary>Empieza el combate.</summary>
        protected virtual void AlActivarse()
        {
        }

        /// <summary>Empieza una fase (1, 2 o 3).</summary>
        protected virtual void AlCambiarDeFase(int fase)
        {
        }

        /// <summary>Derrotado: aquí va lo que el mundo hace al vencerlo (el Anexo A lo describe por jefe).</summary>
        protected virtual void AlSerDerrotado()
        {
        }

        /// <summary>El jugador murió en la arena y el combate vuelve a empezar.</summary>
        protected virtual void AlReiniciarse()
        {
        }

        // --- Ciclo de vida ---

        protected virtual void Awake()
        {
            _cuerpo = GetComponent<Rigidbody2D>();
            _colisionador = GetComponent<Collider2D>();
            _vida = GetComponent<SaludJefe>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (pivote == null && sprite != null) pivote = sprite.transform.parent != transform ? sprite.transform.parent : sprite.transform;

            _cuerpo.freezeRotation = true;
            _posicionInicial = transform.position;

            _maquina = new MaquinaEstados<ESTADOS_JEFE>(CrearTabla());
            _maquina.Registrar(new BossPhaseState(ESTADOS_JEFE.PHASE_1, 0, this, _maquina));
            _maquina.Registrar(new BossPhaseState(ESTADOS_JEFE.PHASE_2, 1, this, _maquina));
            _maquina.Registrar(new BossPhaseState(ESTADOS_JEFE.PHASE_3, 2, this, _maquina));
            _maquina.Registrar(new BossVulnerableState(this, _maquina));
            _maquina.Registrar(new BossDefeatedState(this, _maquina));
            _maquina.OnStateChanged += AlCambiarEstado;

            _vida.EsVulnerable = () => EsVulnerable;
            AplicarDatos();
        }

        protected virtual void OnEnable() => _vida.OnGolpeRecibido += AlRecibirGolpe;

        protected virtual void OnDisable() => _vida.OnGolpeRecibido -= AlRecibirGolpe;

        protected virtual void Update()
        {
            if (_maquina.Iniciada)
            {
                _maquina.Actualizar();
                currentState = _maquina.EstadoActual;
            }

            Pintar();
        }

        protected virtual void FixedUpdate() => _maquina.ActualizarFisica();

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

        // --- API ---

        /// <summary>
        /// Empieza el combate en la fase 1. Lo llama la arena cuando entra el jugador.
        /// <paramref name="limites"/> son los bordes horizontales de la arena.
        /// </summary>
        public void Activar(Transform jugador = null, Vector2? limites = null)
        {
            if (EnCombate || _derrotado)
            {
                return;
            }

            if (jugador != null)
            {
                FijarJugador(jugador);
            }
            else if (_jugador == null)
            {
                var encontrado = PlayerController.BuscarEnEscena(gameObject.scene);
                if (encontrado != null)
                {
                    FijarJugador(encontrado.transform);
                }
            }

            if (limites.HasValue)
            {
                _limites = limites.Value;
                _hayLimites = true;
            }

            FijarContacto(true);
            _maquina.Iniciar(ESTADOS_JEFE.PHASE_1);
            AlActivarse();
        }

        /// <summary>Vuelve al estado de antes del combate: vida llena, en su sitio y quieto.</summary>
        public void Reiniciar()
        {
            if (_derrotado)
            {
                return;
            }

            DetenerAtaques();
            if (_maquina.Iniciada)
            {
                _maquina.Detener();
            }

            _faseNotificada = -1;
            _ventanaPedida = -1f;
            Array.Clear(_siguientePatron, 0, _siguientePatron.Length);
            transform.position = _posicionInicial;
            _cuerpo.position = _posicionInicial;
            _cuerpo.linearVelocity = Vector2.zero;
            _vida.Inicializar(datos.vidaMaxima, datos.invulnerabilidad);
            _vida.NotificarVida();
            FijarContacto(false);
            Mirando = -1;
            _colorBase = datos.tinte;
            AlReiniciarse();
        }

        /// <summary>Cambia los datos antes de activarlo (herramientas y pruebas).</summary>
        public void FijarDatos(JefeData nuevos)
        {
            datos = nuevos;
            AplicarDatos();
        }

        /// <summary>
        /// Pide la ventana vulnerable: el jefe se detiene y recibe más daño durante
        /// <paramref name="duracion"/> segundos (por defecto, los de sus datos). La abre un patrón
        /// (el martillo clavado de la Centinela) o el propio jefe (la memoria correcta del Juez).
        /// </summary>
        public void AbrirVentanaVulnerable(float duracion = -1f)
        {
            if (EnCombate && _maquina.EstadoActual != ESTADOS_JEFE.VULNERABLE)
            {
                _ventanaPedida = duracion > 0f ? duracion : datos.duracionVulnerable;
            }
        }

        // --- Acciones para los patrones ---

        /// <summary>Camina hacia el jugador sin salir de la arena (en FixedUpdate).</summary>
        public void Perseguir(float factorVelocidad = 1f)
        {
            if (!JugadorVivo)
            {
                Detener();
                return;
            }

            Mover(DireccionHaciaJugador, datos.velocidad * factorVelocidad);
        }

        public void Mover(int direccion, float velocidad)
        {
            Mirar(direccion);
            var limites = Limites;
            var borde = Centro.x + direccion * MedioAncho;
            if (direccion == 0 || (direccion > 0 && borde >= limites.y) || (direccion < 0 && borde <= limites.x))
            {
                Detener();
                return;
            }

            var v = _cuerpo.linearVelocity;
            v.x = direccion * velocidad;
            _cuerpo.linearVelocity = v;
        }

        public void Detener()
        {
            if (_cuerpo == null || _cuerpo.bodyType != RigidbodyType2D.Dynamic)
            {
                return;
            }

            var v = _cuerpo.linearVelocity;
            v.x = 0f;
            _cuerpo.linearVelocity = v;
        }

        public void MirarAlJugador() => Mirar(DireccionHaciaJugador);

        public void Mirar(int direccion)
        {
            if (direccion != 0)
            {
                Mirando = direccion > 0 ? 1 : -1;
            }
        }

        /// <summary>Aviso visual previo a un ataque (telegrafiado): el jefe destella en rojo.</summary>
        public void Avisar(float segundos) => _finAviso = Mathf.Max(_finAviso, Time.time + segundos);

        /// <summary>Punto delante del jefe, a una distancia del centro y a una altura sobre sus pies.</summary>
        public Vector2 PuntoDelante(float distancia, float altura) => new Vector2(Centro.x + Mirando * distancia, SueloY + altura);

        /// <summary>Altura del suelo en una x (para colocar erupciones bajo el jugador); sin suelo, la del jefe.</summary>
        public float SueloEn(float x)
        {
            var desde = new Vector2(x, Mathf.Max(CentroJugador.y, Centro.y) + 2f);
            var impacto = Physics2D.Raycast(desde, Vector2.down, 12f, Capas.MascaraSuelo);
            return impacto.collider != null ? impacto.point.y : SueloY;
        }

        /// <summary>Crea una zona de daño del jefe (golpe, onda, erupción). Se borra si el ataque se corta.</summary>
        public ZonaDeDanio CrearZona(string nombre, Vector2 centro, Vector2 tamano, int danio, float aviso, float duracion, Vector2 velocidad, Color color)
        {
            var zona = ZonaDeDanio.Crear($"{name}_{nombre}", centro, tamano, danio, Capas.MascaraJugador, aviso, duracion, velocidad, color);
            _efectos.RemoveAll(e => e == null);
            _efectos.Add(zona.gameObject);
            return zona;
        }

        // --- Internos que usan los estados ---

        internal bool ConsumirVentana()
        {
            if (_ventanaPedida <= 0f)
            {
                return false;
            }

            _duracionVentana = _ventanaPedida;
            _ventanaPedida = -1f;
            return true;
        }

        internal void EntrarEnFase(int fase)
        {
            if (fase != _faseNotificada)
            {
                _faseNotificada = fase;
                AlCambiarDeFase(fase + 1);
                OnFaseCambiada?.Invoke(fase + 1);
            }

            StartCoroutine(BuclePatrones(fase));
        }

        internal void EntrarEnVulnerable() => Detener();

        /// <summary>Corta el patrón en curso y borra sus zonas de daño.</summary>
        internal void DetenerAtaques()
        {
            StopAllCoroutines();
            PatronActual = null;
            foreach (var efecto in _efectos)
            {
                if (efecto != null)
                {
                    Destroy(efecto);
                }
            }

            _efectos.Clear();
            Detener();
        }

        internal void Derrotar()
        {
            _derrotado = true;
            DetenerAtaques();
            FijarContacto(false);
            _cuerpo.linearVelocity = Vector2.zero;
            _cuerpo.bodyType = RigidbodyType2D.Kinematic;
            foreach (var colision in GetComponentsInChildren<Collider2D>())
            {
                colision.enabled = false;
            }

            AlSerDerrotado();
            OnJefeDerrotado?.Invoke(this);
            StartCoroutine(Desvanecer(datos.duracionDerrota));
        }

        // --- Patrones ---

        // Repite los patrones de la fase. Guarda por dónde iba: al volver de la ventana
        // vulnerable sigue con el siguiente patrón, no vuelve a empezar la lista.
        private IEnumerator BuclePatrones(int fase)
        {
            var datosFase = datos.Fase(fase);
            if (datosFase == null)
            {
                yield break;
            }

            yield return new WaitForSeconds(Mathf.Min(0.5f, datosFase.pausaEntrePatrones));
            while (true)
            {
                var patrones = datosFase.patrones;
                if (patrones == null || patrones.Count == 0 || !JugadorVivo)
                {
                    Detener();
                    yield return null;
                    continue;
                }

                var indice = datosFase.aleatorio ? UnityEngine.Random.Range(0, patrones.Count) : _siguientePatron[fase] % patrones.Count;
                _siguientePatron[fase] = indice + 1;

                var patron = patrones[indice];
                if (patron != null)
                {
                    PatronActual = patron;
                    yield return patron.Ejecutar(this);
                    PatronActual = null;
                }

                Detener();
                yield return new WaitForSeconds(datosFase.pausaEntrePatrones);
            }
        }

        // «Explosión visual» provisional hasta que haya arte (PENDIENTES B5): destello y desvanecido.
        private IEnumerator Desvanecer(float duracion)
        {
            var escala = pivote != null ? pivote.localScale : Vector3.one;
            var inicio = Time.time;
            while (Time.time - inicio < duracion)
            {
                var t = duracion > 0f ? (Time.time - inicio) / duracion : 1f;
                if (sprite != null)
                {
                    sprite.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.1f, 0f), t);
                }

                if (pivote != null)
                {
                    pivote.localScale = new Vector3(Mathf.Sign(escala.x) * Mathf.Abs(escala.x) * (1f + t * 0.4f), escala.y * (1f + t * 0.4f), escala.z);
                }

                yield return null;
            }

            gameObject.SetActive(false);
        }

        // --- Preparación y eventos ---

        private void AplicarDatos()
        {
            if (datos == null)
            {
                Debug.LogWarning($"[Jefe] '{name}' no tiene JefeData asignado; usa valores por defecto. Asigna uno de Assets/Data/Jefes.", this);
                datos = ScriptableObject.CreateInstance<JefeData>();
                datos.fases.Add(new FaseJefe());
            }

            _vida.Inicializar(datos.vidaMaxima, datos.invulnerabilidad);
            _vida.MultiplicadorVulnerable = datos.multiplicadorVulnerable;
            _vida.InmuneFueraDeVulnerable = datos.inmuneFueraDeVulnerable;

            if (contacto != null)
            {
                contacto.Danio = datos.danioContacto;
            }

            FijarContacto(EnCombate);
            _colorBase = datos.tinte;
            if (sprite != null)
            {
                sprite.color = _colorBase;
            }
        }

        private void FijarJugador(Transform jugador)
        {
            _jugador = jugador;
            var cuerpo = jugador.GetComponentInParent<Rigidbody2D>();
            _colisionadorJugador = cuerpo != null ? cuerpo.GetComponent<Collider2D>() : jugador.GetComponent<Collider2D>();
            _vidaJugador = jugador.GetComponentInParent<Health>();
        }

        private void FijarContacto(bool activo)
        {
            if (contacto != null && contacto.gameObject.activeSelf != activo)
            {
                contacto.gameObject.SetActive(activo && datos != null && datos.danioContacto > 0);
            }
        }

        private void AlRecibirGolpe(Golpe golpe) => _finParpadeo = Time.time + DuracionParpadeo;

        private void AlCambiarEstado(ESTADOS_JEFE anterior, ESTADOS_JEFE nuevo)
        {
            currentState = nuevo;
            if (registrarTransiciones && anterior != nuevo)
            {
                Debug.Log($"[Jefe {name}] {anterior} -> {nuevo} (t = {Time.time:0.00})", this);
            }

            OnStateChanged?.Invoke(anterior, nuevo);
        }

        // Color según lo que pasa: golpe recibido > aviso de ataque > ventana vulnerable > normal.
        private void Pintar()
        {
            if (sprite == null || _derrotado)
            {
                return;
            }

            var color = _colorBase;
            if (EsVulnerable)
            {
                color = Color.Lerp(_colorBase, new Color(1f, 0.95f, 0.55f), 0.55f + 0.25f * Mathf.Sin(Time.time * 18f));
            }

            if (Time.time < _finAviso && Mathf.Repeat(Time.time * 8f, 1f) < 0.5f)
            {
                color = new Color(1f, 0.3f, 0.15f, 1f);
            }

            if (Time.time < _finParpadeo)
            {
                color = Color.white;
            }

            sprite.color = color;
        }

        private void OnDrawGizmosSelected()
        {
            var limites = Limites;
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
            Gizmos.DrawLine(new Vector3(limites.x, transform.position.y, 0f), new Vector3(limites.x, transform.position.y + 6f, 0f));
            Gizmos.DrawLine(new Vector3(limites.y, transform.position.y, 0f), new Vector3(limites.y, transform.position.y + 6f, 0f));

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 4f, $"{EstadoActual} · {(_vida != null ? _vida.Vida : 0)}");
            }
#endif
        }
    }
}
