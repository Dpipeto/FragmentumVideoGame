using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Levels.Mecanicas;
using UnityEngine;

namespace Fragmentum.Enemies
{
    public enum ESTADOS_ARTILLERO
    {
        /// <summary>No ve a Kael: arrodillado tras sus sacos, con el arma baja.</summary>
        REPOSO,

        /// <summary>Apunta a la cabeza de Kael: el aviso (un destello y una línea roja hasta donde va a disparar).</summary>
        APUNTA,

        /// <summary>Dispara la ráfaga.</summary>
        DISPARA,

        /// <summary>Recarga: es cuando se avanza.</summary>
        RECARGA,

        /// <summary>Kael se le acercó demasiado: alza la culata (el aviso) y le golpea.</summary>
        CULATAZO
    }

    /// <summary>Lo que dibuja el Artillero con arte: el entero «Estado» de su Animator.</summary>
    public enum POSES_ARTILLERO
    {
        /// <summary>En reposo, con el arma baja.</summary>
        REPOSO,

        /// <summary>Apunta: el fusil al hombro.</summary>
        APUNTA,

        /// <summary>Dispara: el fogonazo.</summary>
        DISPARA,

        /// <summary>Recarga: el fusil bajado y la mano en el cerrojo.</summary>
        RECARGA,

        /// <summary>El aviso del culatazo.</summary>
        ALZA_LA_CULATA,

        /// <summary>El culatazo.</summary>
        CULATAZO,

        /// <summary>Le acaban de dar (en reposo o recargando: nunca tapa el aviso de la ráfaga).</summary>
        GOLPEADO
    }

    /// <summary>
    /// El Artillero del mundo de la Culpa (<c>Nivel4_Diseno.md</c>, sección 5): un soldado arrodillado
    /// tras sus sacos terreros que <b>no se mueve</b> y dispara a la cabeza. La cobertura es un problema
    /// de ritmo, no de persecución.
    ///
    /// - Ve a Kael hasta <see cref="vision"/> u por delante, si nada (suelo ni paredes) se interpone.
    /// - <b>Ráfaga:</b> apunta <see cref="tiempoApuntar"/> s (el aviso: una línea roja fina hasta donde va
    ///   a disparar), dispara <see cref="balas"/> balas seguidas <see cref="bajoLaCabeza"/> u por debajo
    ///   de lo alto de Kael y recarga <see cref="recarga"/> s. Apunta a la cabeza: ni agacharse solo ni la
    ///   cobertura sola sirven; agachado tras una cobertura, la bala da en ella (las coberturas son
    ///   suelo y las balas se paran contra él). Si Kael se esconde mientras apunta, dispara a donde lo vio.
    /// - Sus balas encienden los barriles que tocan (<see cref="BarrilExplosivo.EncenderEn"/>).
    /// - Si Kael llega a <see cref="distanciaCulatazo"/>, le da un culatazo (avisa
    ///   <see cref="avisoCulatazo"/> s, quita 1): su arma, la del <see cref="EnemyController"/>.
    /// - <b>Invisible, Kael no existe para él:</b> si estaba apuntando, baja el arma y se apaga la línea; la
    ///   ráfaga que ya salió sigue su camino.
    /// - Los dos del campanario se turnan: el segundo empieza <see cref="turno"/> s después.
    ///
    /// Lleva siempre el mando: su controlador solo despierta para morir por su camino (los puntos, el
    /// desvanecerse). Con las memorias, recarga y apunta un 20 % antes (<see cref="IAgravable"/>).
    ///
    /// Con arte, su Animator sigue lo que hace (<see cref="Pose"/>, el entero «Estado») y no se tiñe.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(EnemyController), typeof(Health))]
    public class Artillero : MonoBehaviour, IAgravable
    {
        [Header("Vista")]
        [Tooltip("Hasta dónde ve, por delante.")]
        [Min(0.5f)] public float vision = 14f;

        [Tooltip("Mira a la izquierda, hacia el que llega.")]
        public bool miraIzquierda = true;

        [Header("Ráfaga")]
        [Min(0.05f)] public float tiempoApuntar = 0.8f;

        [Min(1)] public int balas = 3;

        [Min(0.01f)] public float entreBalas = 0.12f;

        [Min(0.1f)] public float recarga = 2f;

        [Tooltip("El segundo de una pareja empieza esto después que el primero: se turnan.")]
        [Min(0f)] public float turno;

        [Min(1f)] public float velocidadBala = 14f;

        [Min(0.02f)] public float radioBala = 0.1f;

        [Min(1)] public int danioBala = 1;

        [Tooltip("Apunta esto por debajo de lo alto de Kael: a la cabeza.")]
        [Min(0f)] public float bajoLaCabeza = 0.25f;

        [Tooltip("La boca del cañón, desde su pie, mirando a la derecha (se voltea al mirar a la izquierda).")]
        public Vector2 canon = new Vector2(1.3f, 1.5f);

        [Header("Culatazo")]
        [Min(0.1f)] public float distanciaCulatazo = 1.5f;

        [Min(0f)] public float avisoCulatazo = 0.4f;

        [Min(0.05f)] public float golpeCulatazo = 0.15f;

        [Tooltip("Lo que descansa tras un culatazo.")]
        [Min(0f)] public float trasCulatazo = 0.6f;

        [Tooltip("Con arte: lo que se le ve golpeado tras recibir un golpe (en reposo o recargando).")]
        [Min(0f)] public float golpeado = 0.25f;

        [Header("Referencias")]
        [Tooltip("La línea roja del aviso: de su cañón a donde va a disparar.")]
        [SerializeField] private LineRenderer linea;

        [Tooltip("Lo que se voltea al girar (el del EnemyController).")]
        [SerializeField] private Transform pivote;

        [SerializeField] private SpriteRenderer sprite;

        [Tooltip("Con arte: su Animator, que sigue lo que hace (el entero «Estado»). Vacío = se busca solo.")]
        [SerializeField] private Animator animator;

        [Tooltip("La bala dibujada (el trazador). Vacío = un cuadrado provisional de su color.")]
        [SerializeField] private Proyectil bala;

        [Header("Sin arte")]
        [SerializeField] private Color colorApunta = new Color(0.55f, 0.2f, 0.2f, 1f);

        private EnemyController _controlador;
        private Health _vida;
        private RelevoDelControlador _relevo;
        private float _finDeEstado;
        private float _golpeadoHasta = float.NegativeInfinity;
        private float _listoParaApuntar;
        private int _disparadas;
        private Vector2 _mira;
        private bool _vioAntes;

        public event Action<ESTADOS_ARTILLERO> OnCambio;

        /// <summary>Disparó una bala.</summary>
        public event Action<Proyectil> OnDisparo;

        public ESTADOS_ARTILLERO Estado { get; private set; } = ESTADOS_ARTILLERO.REPOSO;

        /// <summary>A donde apunta (o apuntó por última vez).</summary>
        public Vector2 Mira => _mira;

        /// <summary>¿Se ve la línea roja?</summary>
        public bool LineaVisible => linea != null && linea.enabled;

        public LineRenderer Linea => linea;

        /// <summary>¿Lleva arte? Entonces su Animator dibuja lo que hace y no se tiñe.</summary>
        public bool ConArte => animator != null && animator.runtimeAnimatorController != null;

        /// <summary>Lo que se le ve hacer: su estado, con el aviso del culatazo y el golpe recibido aparte.</summary>
        public POSES_ARTILLERO Pose
        {
            get
            {
                switch (Estado)
                {
                    case ESTADOS_ARTILLERO.APUNTA:
                        return POSES_ARTILLERO.APUNTA;
                    case ESTADOS_ARTILLERO.DISPARA:
                        return POSES_ARTILLERO.DISPARA;
                    case ESTADOS_ARTILLERO.CULATAZO:
                        return Time.time >= _finDeEstado ? POSES_ARTILLERO.CULATAZO : POSES_ARTILLERO.ALZA_LA_CULATA;
                    default:
                        return Time.time < _golpeadoHasta ? POSES_ARTILLERO.GOLPEADO
                            : Estado == ESTADOS_ARTILLERO.RECARGA ? POSES_ARTILLERO.RECARGA : POSES_ARTILLERO.REPOSO;
                }
            }
        }

        /// <summary>De donde salen las balas.</summary>
        public Vector2 Canon => (Vector2)transform.position + new Vector2(canon.x * Direccion, canon.y);

        private int Direccion => miraIzquierda ? -1 : 1;

        /// <summary>A dónde apunta: <paramref name="bajo"/> u por debajo de lo alto del cuerpo de Kael.</summary>
        public static Vector2 PuntoDeMira(Bounds cuerpo, float bajo) => new Vector2(cuerpo.center.x, cuerpo.max.y - bajo);

        /// <summary>¿Está <paramref name="objetivo"/> por delante de <paramref name="origen"/>, a <paramref name="alcance"/> u como mucho?</summary>
        public static bool PorDelante(Vector2 origen, Vector2 objetivo, int direccion, float alcance)
        {
            var delante = (objetivo.x - origen.x) * direccion;
            return delante > 0f && delante <= alcance;
        }

        public void Agravar(float factor)
        {
            tiempoApuntar /= factor;
            recarga /= factor;
            entreBalas /= factor;
            avisoCulatazo /= factor;
        }

        private void Awake()
        {
            _controlador = GetComponent<EnemyController>();
            _vida = GetComponent<Health>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (pivote == null && sprite != null) pivote = sprite.transform.parent != transform ? sprite.transform.parent : sprite.transform;
            _relevo = new RelevoDelControlador(_controlador, _vida, pivote);
            ApagarLinea();
        }

        private void OnEnable() => _vida.OnGolpeRecibido += AlRecibirGolpe;

        private void OnDisable()
        {
            _vida.OnGolpeRecibido -= AlRecibirGolpe;
            _relevo.Soltar();
            ApagarLinea();
        }

        // Después del Start del controlador (orden 100): ya arrancado, puede devolverle el mando para morir.
        private void Start()
        {
            _relevo.Tomar();
            _relevo.Mirar(Direccion);
        }

        private void Update()
        {
            if (!_vida.EstaVivo)
            {
                ApagarLinea();
                return;
            }

            _relevo.Detener();
            var ve = VeAKael(out var mira);
            if (ve)
            {
                // Los dos del campanario se turnan: al verlo por primera vez, el segundo espera su turno.
                if (!_vioAntes)
                {
                    _vioAntes = true;
                    _listoParaApuntar = Mathf.Max(_listoParaApuntar, Time.time + turno);
                }
            }

            switch (Estado)
            {
                case ESTADOS_ARTILLERO.REPOSO:
                    if (Cerca())
                    {
                        // Se vuelve hacia Kael y alza la culata.
                        _relevo.Mirar(_relevo.CentroDeKael.x >= transform.position.x ? 1 : -1);
                        Entrar(ESTADOS_ARTILLERO.CULATAZO, avisoCulatazo);
                        Pintar(colorApunta);
                    }
                    else if (ve && Time.time >= _listoParaApuntar)
                    {
                        _mira = mira;
                        Entrar(ESTADOS_ARTILLERO.APUNTA, tiempoApuntar);
                        Pintar(colorApunta);
                    }

                    break;

                case ESTADOS_ARTILLERO.APUNTA:
                    if (KaelInvisible)
                    {
                        // Kael no existe para él: baja el arma.
                        ApagarLinea();
                        Pintar(Tinte);
                        Entrar(ESTADOS_ARTILLERO.REPOSO, 0f);
                        break;
                    }

                    if (ve)
                    {
                        _mira = mira;
                    }

                    DibujarLinea();
                    if (Time.time >= _finDeEstado)
                    {
                        _disparadas = 0;
                        Entrar(ESTADOS_ARTILLERO.DISPARA, 0f);
                    }

                    break;

                case ESTADOS_ARTILLERO.DISPARA:
                    if (ve)
                    {
                        _mira = mira;
                    }

                    if (Time.time >= _finDeEstado)
                    {
                        Disparar();
                        _disparadas++;
                        if (_disparadas >= balas)
                        {
                            ApagarLinea();
                            Pintar(Tinte);
                            Entrar(ESTADOS_ARTILLERO.RECARGA, recarga);
                        }
                        else
                        {
                            _finDeEstado = Time.time + entreBalas;
                        }
                    }
                    else
                    {
                        DibujarLinea();
                    }

                    break;

                case ESTADOS_ARTILLERO.RECARGA:
                    if (Time.time >= _finDeEstado)
                    {
                        Entrar(ESTADOS_ARTILLERO.REPOSO, 0f);
                    }

                    break;

                case ESTADOS_ARTILLERO.CULATAZO:
                    if (Time.time >= _finDeEstado + golpeCulatazo)
                    {
                        _relevo.FijarArma(false);
                        _relevo.Mirar(Direccion);
                        Pintar(Tinte);
                        _listoParaApuntar = Time.time + trasCulatazo;
                        Entrar(ESTADOS_ARTILLERO.REPOSO, 0f);
                    }
                    else if (Time.time >= _finDeEstado)
                    {
                        _relevo.FijarArma(true);
                    }

                    break;
            }

            // Antes de que el Animator se evalúe en este fotograma.
            if (ConArte && animator.isInitialized)
            {
                AnimatorParams.FijarEntero(animator, AnimatorParams.EstadoMecanica, (int)Pose);
            }
        }

        private bool KaelInvisible
        {
            get
            {
                var kael = _relevo.Kael;
                return kael != null && kael.Habilidades != null && kael.Habilidades.EsInvisible;
            }
        }

        // ¿Ve a Kael? Vivo, visible, por delante, a su alcance y sin suelo ni paredes en medio de su
        // cañón a la cabeza de Kael (agachado tras una cobertura, no lo ve: la cobertura lo tapa).
        private bool VeAKael(out Vector2 mira)
        {
            mira = _mira;
            var kael = _relevo.Kael;
            if (kael == null || !kael.EstaVivo || KaelInvisible)
            {
                return false;
            }

            var cuerpo = kael.GetComponent<Collider2D>();
            mira = cuerpo != null ? PuntoDeMira(cuerpo.bounds, bajoLaCabeza) : (Vector2)kael.transform.position + Vector2.up * 1.5f;
            if (!PorDelante(Canon, mira, Direccion, vision))
            {
                return false;
            }

            return !Physics2D.Linecast(Canon, mira, Capas.Mascara(Capas.Suelo, Capas.Wall));
        }

        // ¿Kael, visible y vivo, a un culatazo?
        private bool Cerca()
        {
            var kael = _relevo.Kael;
            if (kael == null || !kael.EstaVivo || KaelInvisible)
            {
                return false;
            }

            var dx = Mathf.Abs(kael.transform.position.x - transform.position.x);
            var dy = Mathf.Abs(kael.transform.position.y - transform.position.y);
            return dx <= distanciaCulatazo && dy < 1f;
        }

        private void Disparar()
        {
            var origen = Canon;
            var direccion = _mira - origen;
            if (direccion.sqrMagnitude < 0.0001f)
            {
                direccion = new Vector2(Direccion, 0f);
            }

            var velocidad = direccion.normalized * velocidadBala;
            var disparo = bala != null
                ? Proyectil.Disparar(bala, origen, velocidad, danioBala, Capas.Mascara(Capas.Player), vision + 4f)
                : Proyectil.Disparar($"Bala_{name}", origen, velocidad, radioBala, danioBala, Capas.Mascara(Capas.Player), vision + 4f,
                    new Color(1f, 0.85f, 0.55f, 1f));
            disparo.gameObject.AddComponent<BalaQueEnciendeBarriles>().radio = radioBala;
            OnDisparo?.Invoke(disparo);
        }

        private void AlRecibirGolpe(Golpe golpe)
        {
            _golpeadoHasta = Time.time + golpeado;
            _relevo.AntesDeMorir();
        }

        private Color Tinte => _controlador != null && _controlador.Datos != null ? _controlador.Datos.tinte : Color.white;

        // Sin arte, el color dice lo que hace; con arte, lo dice el dibujo.
        private void Pintar(Color color)
        {
            if (sprite != null && !ConArte)
            {
                sprite.color = color;
            }
        }

        private void DibujarLinea()
        {
            if (linea == null)
            {
                return;
            }

            linea.enabled = true;
            linea.positionCount = 2;
            linea.SetPosition(0, Canon);
            linea.SetPosition(1, _mira);
        }

        private void ApagarLinea()
        {
            if (linea != null)
            {
                linea.enabled = false;
            }
        }

        private void Entrar(ESTADOS_ARTILLERO nuevo, float duracion)
        {
            Estado = nuevo;
            _finDeEstado = Time.time + duracion;
            OnCambio?.Invoke(nuevo);
        }
    }
}
