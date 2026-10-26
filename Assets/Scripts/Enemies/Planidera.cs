using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Enemies
{
    public enum ESTADOS_PLANIDERA
    {
        /// <summary>Manda el <see cref="EnemyController"/>: flota hacia Kael y, de cerca, barre con el brazo.</summary>
        ACECHA,

        /// <summary>Alza el brazo y las esquirlas dejan el arco (E1).</summary>
        ALZA,

        /// <summary>El aviso: las esquirlas en fila, con las puntas encendidas (E2).</summary>
        APUNTA,

        /// <summary>Las lanza una tras otra hacia Kael (E3).</summary>
        LANZA,

        /// <summary>Sin esquirlas: se encoge y llora. No ataca: es cuando se la castiga (E4).</summary>
        DESARMADA,

        /// <summary>Las esquirlas vuelven al arco (E5).</summary>
        REHACE
    }

    /// <summary>
    /// La Plañidera, el mini-jefe del mundo de la Tristeza (como el Golem de Ceniza en el de la
    /// Ira). De cerca es un <see cref="EnemyController"/> más: flota despacio hacia Kael y barre
    /// con el brazo (su «abrazo»), con hiperarmadura. Lo suyo son las esquirlas del arco que lleva
    /// detrás de la cabeza: cada <see cref="esperaEntreRafagas"/> s las alza, las apunta (el
    /// aviso) y las lanza una tras otra hacia donde está Kael. Después se queda
    /// <see cref="tiempoDesarmada"/> s llorando, sin atacar ni moverse, hasta que las esquirlas
    /// vuelven. Enseña lo que pedirá la Doncella: esperar a que baje la guardia.
    ///
    /// Mientras maneja las esquirlas, su <see cref="EnemyController"/> está apagado (como el de
    /// la Escultura dormida): ni anda, ni se gira, ni barre. Si muere así, se enciende antes de
    /// que llegue el aviso de la muerte, para que muera por el camino de siempre.
    ///
    /// No toca <see cref="EnemyController"/>: solo lo apaga y lo enciende.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController), typeof(Health), typeof(Rigidbody2D))]
    public class Planidera : MonoBehaviour
    {
        [Header("Ritmo (segundos)")]
        [Tooltip("Lo que acecha a Kael (persiguiéndolo) entre una ráfaga y la siguiente.")]
        [Min(0f)] public float esperaEntreRafagas = 2.5f;

        [Tooltip("Alzar el brazo: las esquirlas dejan el arco.")]
        [Min(0f)] public float tiempoAlzar = 0.5f;

        [Tooltip("Apuntar: el aviso. Es el tiempo que ajusta la dificultad.")]
        [Min(0.05f)] public float tiempoApuntar = 0.8f;

        [Tooltip("Entre una esquirla y la siguiente.")]
        [Min(0.05f)] public float intervaloEsquirlas = 0.35f;

        [Tooltip("Desarmada: llora y no ataca. Es la ventana para castigarla.")]
        [Min(0f)] public float tiempoDesarmada = 2f;

        [Tooltip("Las esquirlas vuelven al arco.")]
        [Min(0f)] public float tiempoRehacer = 0.6f;

        [Header("Esquirlas")]
        [Min(1)] public int esquirlas = 3;

        [Min(1)] public int danioEsquirla = 1;

        [Min(0.1f)] public float velocidadEsquirla = 8f;

        [Tooltip("Distancia que recorre una esquirla antes de deshacerse.")]
        [Min(0f)] public float alcanceEsquirla = 12f;

        [Tooltip("Radio de la esquirla provisional (sin prefab).")]
        [Min(0.05f)] public float radioEsquirla = 0.2f;

        [Tooltip("Prefab de la esquirla, con su arte. Vacío = una provisional creada por código.")]
        [SerializeField] private Proyectil esquirla;

        [Header("Referencias")]
        [Tooltip("De dónde salen las esquirlas: sobre su cabeza. Cuelga del pivote, así se voltea con ella.")]
        [SerializeField] private Transform arco;

        [Tooltip("Lo que se voltea al girar (el del EnemyController).")]
        [SerializeField] private Transform pivote;

        [SerializeField] private SpriteRenderer sprite;

        [Tooltip("Sin arte se queda vacío. Con arte, los clips de las esquirlas.")]
        [SerializeField] private Animator animator;

        [Header("Sin arte")]
        [Tooltip("El color del aviso: las puntas de las esquirlas se encienden en cian.")]
        [SerializeField] private Color colorAviso = new Color(0f, 0.83f, 1f, 1f);

        [Tooltip("Desarmada, se apaga.")]
        [SerializeField] private Color colorDesarmada = new Color(0.55f, 0.6f, 0.68f, 1f);

        private EnemyController _controlador;
        private Health _vida;
        private Rigidbody2D _cuerpo;
        private Transform _jugador;
        private Collider2D _colisionadorJugador;
        private Health _vidaJugador;
        private float _proximaBusqueda;
        private float _acecho;
        private float _finDeEstado;
        private float _inicioDeEstado;
        private float _proximaEsquirla;
        private int _lanzadas;

        public event Action<ESTADOS_PLANIDERA> OnCambio;

        /// <summary>Lanzó una esquirla.</summary>
        public event Action<Proyectil> OnEsquirla;

        public ESTADOS_PLANIDERA Estado { get; private set; } = ESTADOS_PLANIDERA.ACECHA;

        /// <summary>¿Está con las esquirlas (su controlador, apagado)?</summary>
        public bool ConLasEsquirlas => Estado != ESTADOS_PLANIDERA.ACECHA;

        public bool Desarmada => Estado == ESTADOS_PLANIDERA.DESARMADA;

        /// <summary>Esquirlas lanzadas en la ráfaga en curso (o en la última).</summary>
        public int Lanzadas => _lanzadas;

        /// <summary>Lo que dura una ráfaga entera, del brazo alzado a las esquirlas de vuelta.</summary>
        public float DuracionRafaga =>
            tiempoAlzar + tiempoApuntar + Mathf.Max(0, esquirlas - 1) * intervaloEsquirlas + tiempoDesarmada + tiempoRehacer;

        private Vector2 Boca => arco != null ? (Vector2)arco.position : (Vector2)transform.position + Vector2.up * 2.8f;

        private Color Tinte => _controlador != null && _controlador.Datos != null ? _controlador.Datos.tinte : Color.white;

        private void Awake()
        {
            _controlador = GetComponent<EnemyController>();
            _vida = GetComponent<Health>();
            _cuerpo = GetComponent<Rigidbody2D>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable() => _vida.OnGolpeRecibido += AlRecibirGolpe;

        private void OnDisable() => _vida.OnGolpeRecibido -= AlRecibirGolpe;

        private void Update()
        {
            if (!_vida.EstaVivo)
            {
                return;
            }

            switch (Estado)
            {
                case ESTADOS_PLANIDERA.ACECHA:
                    Acechar();
                    break;

                case ESTADOS_PLANIDERA.ALZA:
                    MirarAKael();
                    if (Time.time >= _finDeEstado)
                    {
                        Entrar(ESTADOS_PLANIDERA.APUNTA, tiempoApuntar);
                    }

                    break;

                case ESTADOS_PLANIDERA.APUNTA:
                    MirarAKael();
                    Pintar(Color.Lerp(Tinte, colorAviso, Avance()));
                    if (Time.time >= _finDeEstado)
                    {
                        _lanzadas = 0;
                        _proximaEsquirla = Time.time;
                        Entrar(ESTADOS_PLANIDERA.LANZA, 0f);
                    }

                    break;

                case ESTADOS_PLANIDERA.LANZA:
                    MirarAKael();
                    if (Time.time >= _proximaEsquirla)
                    {
                        Lanzar();
                        _proximaEsquirla += intervaloEsquirlas;
                        if (_lanzadas >= esquirlas)
                        {
                            Entrar(ESTADOS_PLANIDERA.DESARMADA, tiempoDesarmada);
                            Pintar(colorDesarmada);
                        }
                    }

                    break;

                case ESTADOS_PLANIDERA.DESARMADA:
                    if (Time.time >= _finDeEstado)
                    {
                        Entrar(ESTADOS_PLANIDERA.REHACE, tiempoRehacer);
                    }

                    break;

                case ESTADOS_PLANIDERA.REHACE:
                    Pintar(Color.Lerp(colorDesarmada, Tinte, Avance()));
                    if (Time.time >= _finDeEstado)
                    {
                        Acabar();
                    }

                    break;
            }
        }

        /// <summary>Empieza la ráfaga ya, sin esperar (para las pruebas).</summary>
        public void EmpezarRafaga()
        {
            if (Estado != ESTADOS_PLANIDERA.ACECHA || !_vida.EstaVivo)
            {
                return;
            }

            _controlador.enabled = false;
            _cuerpo.linearVelocity = new Vector2(0f, _cuerpo.linearVelocity.y);
            Entrar(ESTADOS_PLANIDERA.ALZA, tiempoAlzar);
        }

        // Solo cuenta el tiempo en que persigue a Kael: ni de patrulla ni en mitad de su barrido.
        private void Acechar()
        {
            if (!_controlador.enabled)
            {
                return;
            }

            var estado = _controlador.EstadoActual;
            if (estado == ESTADOS_ENEMIGO.CHASE)
            {
                _acecho += Time.deltaTime;
                if (_acecho >= esperaEntreRafagas)
                {
                    EmpezarRafaga();
                }
            }
            else if (estado == ESTADOS_ENEMIGO.IDLE || estado == ESTADOS_ENEMIGO.PATROL)
            {
                _acecho = 0f;
            }
        }

        private void Lanzar()
        {
            var origen = Boca;
            var direccion = BuscarAKael() && _vidaJugador != null && _vidaJugador.EstaVivo
                ? CentroDeKael() - origen
                : new Vector2(HaciaDondeMira(), 0f);
            direccion = direccion.sqrMagnitude > 1e-6f ? direccion.normalized : new Vector2(HaciaDondeMira(), 0f);

            var velocidad = direccion * velocidadEsquirla;
            var proyectil = esquirla != null
                ? Proyectil.Disparar(esquirla, origen, velocidad, danioEsquirla, Capas.MascaraJugador, alcanceEsquirla)
                : Proyectil.Disparar($"{name}_Esquirla", origen, velocidad, radioEsquirla, danioEsquirla, Capas.MascaraJugador, alcanceEsquirla, colorAviso);

            // La punta, hacia donde vuela. El prefab ya se voltea solo si va hacia la izquierda.
            var haciaAtras = esquirla != null && direccion.x < 0f;
            var angulo = haciaAtras ? Mathf.Atan2(-direccion.y, -direccion.x) : Mathf.Atan2(direccion.y, direccion.x);
            proyectil.transform.rotation = Quaternion.Euler(0f, 0f, angulo * Mathf.Rad2Deg);

            _lanzadas++;
            OnEsquirla?.Invoke(proyectil);
        }

        private void Acabar()
        {
            _acecho = 0f;
            Pintar(Tinte);
            Entrar(ESTADOS_PLANIDERA.ACECHA, 0f);
            _controlador.enabled = true;
        }

        // Si el golpe la mata con el controlador apagado, se enciende ya: Health avisa de la muerte
        // justo después de este evento, y así el controlador la oye y muere por su camino (DEAD).
        private void AlRecibirGolpe(Golpe golpe)
        {
            if (!_vida.EstaVivo && !_controlador.enabled)
            {
                Entrar(ESTADOS_PLANIDERA.ACECHA, 0f);
                _controlador.enabled = true;
            }
        }

        private void Entrar(ESTADOS_PLANIDERA nuevo, float duracion)
        {
            Estado = nuevo;
            _inicioDeEstado = Time.time;
            _finDeEstado = Time.time + duracion;
            if (animator != null && animator.isInitialized)
            {
                AnimatorParams.FijarEntero(animator, AnimatorParams.EstadoMecanica, (int)nuevo);
            }

            OnCambio?.Invoke(nuevo);
        }

        private float Avance()
        {
            var duracion = _finDeEstado - _inicioDeEstado;
            return duracion > 0f ? Mathf.Clamp01((Time.time - _inicioDeEstado) / duracion) : 1f;
        }

        // Sin arte: el aviso y el llanto se leen en el color del bloque.
        private void Pintar(Color color)
        {
            if (sprite != null)
            {
                sprite.color = color;
            }
        }

        private int HaciaDondeMira() => _controlador.Mirando;

        // Con el controlador apagado nadie la gira: mientras apunta y lanza, sigue a Kael.
        private void MirarAKael()
        {
            if (pivote == null || !BuscarAKael())
            {
                return;
            }

            var mira = CentroDeKael().x >= transform.position.x ? 1 : -1;
            var derecha = _controlador.Datos == null || _controlador.Datos.spriteMiraDerecha;
            var escala = pivote.localScale;
            escala.x = Mathf.Abs(escala.x) * mira * (derecha ? 1 : -1);
            pivote.localScale = escala;
        }

        private bool BuscarAKael()
        {
            if (_jugador != null)
            {
                return true;
            }

            if (Time.time < _proximaBusqueda)
            {
                return false;
            }

            _proximaBusqueda = Time.time + 0.5f;
            var objeto = GameObject.FindGameObjectWithTag(GameTags.Player);
            if (objeto == null)
            {
                return false;
            }

            _jugador = objeto.transform;
            _vidaJugador = objeto.GetComponentInParent<Health>();
            var cuerpoJugador = objeto.GetComponentInParent<Rigidbody2D>();
            _colisionadorJugador = cuerpoJugador != null ? cuerpoJugador.GetComponent<Collider2D>() : objeto.GetComponent<Collider2D>();
            return true;
        }

        private Vector2 CentroDeKael() =>
            _colisionadorJugador != null ? (Vector2)_colisionadorJugador.bounds.center : (Vector2)_jugador.position + Vector2.up * 0.9f;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.83f, 1f, 0.6f);
            Gizmos.DrawWireSphere(Boca, radioEsquirla);
            Gizmos.DrawWireSphere(Boca, alcanceEsquirla);
        }
    }
}
