using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Levels.Mecanicas;
using UnityEngine;

namespace Fragmentum.Enemies
{
    public enum ESTADOS_VERDUGO
    {
        /// <summary>Manda el <see cref="EnemyController"/>: ronda por el patio y camina despacio hacia Kael.</summary>
        MANDA,

        /// <summary>Alza el hacha (el aviso) y la deja caer por delante.</summary>
        HACHAZO,

        /// <summary>Gira el hacha a su alrededor: Kael a su espalda o pegado a él.</summary>
        BARRIDO,

        /// <summary>Voltea el garfio (el aviso) y lo lanza recto a la altura del pecho.</summary>
        CADENA,

        /// <summary>Una explosión lo tiró: de rodillas, los golpes valen el doble.</summary>
        DE_RODILLAS,

        /// <summary>Kael se volvió invisible: va a donde lo vio por última vez.</summary>
        BUSCA,

        /// <summary>Tras un golpe, recupera el hacha.</summary>
        RECUPERA
    }

    /// <summary>Lo que dibuja el Verdugo con arte: el entero «Estado» de su Animator.</summary>
    public enum POSES_VERDUGO
    {
        /// <summary>Manda su controlador: quieto o andando.</summary>
        ANDA,

        /// <summary>El aviso del hachazo: el hacha en alto.</summary>
        ALZA_EL_HACHA,

        /// <summary>El hachazo: el hacha en el suelo.</summary>
        HACHAZO,

        /// <summary>Tras el hachazo: saca el hacha del suelo.</summary>
        SACA_EL_HACHA,

        /// <summary>El aviso del barrido: el hacha echada atrás.</summary>
        PREPARA_EL_BARRIDO,

        /// <summary>El barrido.</summary>
        BARRIDO,

        /// <summary>Tras el barrido: vuelve a ponerse en guardia.</summary>
        TRAS_EL_BARRIDO,

        /// <summary>El aviso de la cadena: voltea el garfio.</summary>
        VOLTEA_EL_GARFIO,

        /// <summary>Lanza la cadena: el garfio vuela.</summary>
        LANZA,

        /// <summary>Tras la cadena: tira de ella.</summary>
        TIRA,

        /// <summary>Una explosión lo tiró.</summary>
        DE_RODILLAS,

        /// <summary>Anda a ciegas hasta donde vio a Kael por última vez.</summary>
        BUSCA
    }

    /// <summary>
    /// El Verdugo, el mini-jefe del mundo de la Culpa (<c>Nivel4_Diseno.md</c>, sección 5): un Penitente
    /// enorme con capucha, un hacha de dos manos y la cadena de un garfio. Es la mano que cumplió la
    /// sentencia. Camina despacio hacia Kael (su <see cref="EnemyController"/>, con hiperarmadura) y esta
    /// pieza lleva sus golpes:
    ///
    /// - <b>Hachazo:</b> alza el hacha <see cref="avisoHachazo"/> s y la deja caer
    ///   <see cref="alcanceHachazo"/> u por delante; quita 2.
    /// - <b>Barrido:</b> si Kael está a su espalda o muy cerca, gira el hacha a su alrededor (aviso
    ///   <see cref="avisoBarrido"/> s); quita 1.
    /// - <b>Cadena:</b> si Kael se aleja más de <see cref="distanciaCadena"/> u, voltea el garfio
    ///   <see cref="avisoCadena"/> s y lo lanza recto a <see cref="alturaCadena"/> u del suelo: si le da,
    ///   le quita 1 y tira de él hacia el Verdugo. <b>Agachado, le pasa por encima.</b>
    /// - <b>De rodillas:</b> una explosión a menos de <see cref="radioRodillas"/> u lo deja de rodillas
    ///   <see cref="tiempoRodillas"/> s, y los golpes valen <see cref="multiplicadorRodillas"/> veces más.
    /// - <b>Si Kael se vuelve invisible, lo pierde:</b> termina lo que hacía, camina hasta donde lo vio por
    ///   última vez y da allí un barrido a ciegas, sin lanzar la cadena. Así se le lleva junto a un barril.
    ///
    /// Mientras golpea, está de rodillas o busca, su controlador está apagado, como el del Acechador. No
    /// toca <see cref="EnemyController"/>. Con las memorias (la carta), golpea un 20 % antes.
    ///
    /// Con arte, su Animator sigue lo que hace (<see cref="Pose"/>, el entero «Estado») y no se tiñe.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController), typeof(Health))]
    public class Verdugo : MonoBehaviour, IAgravable
    {
        [Header("Vista")]
        [Tooltip("Hasta dónde ve a Kael (y lo vigila para saber dónde lo vio por última vez).")]
        [Min(0.5f)] public float vision = 10f;

        [Header("Hachazo")]
        [Min(0f)] public float avisoHachazo = 0.8f;

        [Min(0.1f)] public float alcanceHachazo = 2.5f;

        [Header("Barrido")]
        [Min(0f)] public float avisoBarrido = 0.6f;

        [Min(0.1f)] public float radioBarrido = 2f;

        [Tooltip("Más cerca que esto, barre aunque Kael esté delante.")]
        [Min(0f)] public float pegado = 1f;

        [Header("Cadena")]
        [Min(0.1f)] public float distanciaCadena = 6f;

        [Min(0.1f)] public float alcanceCadena = 9f;

        [Min(0f)] public float avisoCadena = 0.7f;

        [Min(0.1f)] public float velocidadCadena = 12f;

        [Tooltip("A qué altura del suelo vuela el garfio: la del pecho. Agachado (1,08 u), Kael queda por debajo.")]
        [Min(0.1f)] public float alturaCadena = 1.4f;

        [Min(0.02f)] public float radioGarfio = 0.15f;

        [Min(1)] public int danioCadena = 1;

        [Header("Golpes")]
        [Tooltip("Lo que dura abierta la zona de daño de un golpe.")]
        [Min(0.05f)] public float duracionGolpe = 0.2f;

        [Tooltip("Lo que tarda en recuperar el hacha tras un golpe.")]
        [Min(0f)] public float recupera = 0.7f;

        [Header("De rodillas")]
        [Min(0f)] public float radioRodillas = 2.5f;

        [Min(0f)] public float tiempoRodillas = 2.5f;

        [Min(1f)] public float multiplicadorRodillas = 2f;

        [Header("Territorio")]
        [Tooltip("Entre qué x busca a Kael (el patio).")]
        public Vector2 limites = new Vector2(-10000f, 10000f);

        [Header("Referencias")]
        [Tooltip("La zona de daño del hachazo (capa EnemyWeapon), por delante. Apagada fuera del golpe.")]
        [SerializeField] private Hitbox hachazo;

        [Tooltip("La zona de daño del barrido, a su alrededor. Apagada fuera del golpe.")]
        [SerializeField] private Hitbox barrido;

        [Tooltip("El garfio que vuela (sin arte, un rectángulo). Con arte, dibujado colgando: se gira hacia donde vuela.")]
        [SerializeField] private Transform garfio;

        [Tooltip("Con arte: la cadena, un tramo que se repite (dibujo en mosaico) de su mano al garfio. Vacío = sin cadena.")]
        [SerializeField] private SpriteRenderer cadena;

        [Tooltip("De dónde sale la cadena: su mano, por delante de su centro (mirando a la derecha)…")]
        public float manoCadena = 0.6f;

        [Tooltip("…y sobre sus pies. El garfio vuela más bajo (alturaCadena): la cadena baja de la mano a él.")]
        public float alturaMano = 1.4f;

        [SerializeField] private Transform pivote;

        [SerializeField] private SpriteRenderer sprite;

        [Tooltip("Con arte: su Animator, que sigue lo que hace (el entero «Estado»). Vacío = se busca solo.")]
        [SerializeField] private Animator animator;

        [Header("Sin arte")]
        [SerializeField] private Color colorAviso = new Color(0.62f, 0.2f, 0.24f, 1f);

        [SerializeField] private Color colorRodillas = new Color(0.48f, 0.5f, 0.6f, 1f);

        private EnemyController _controlador;
        private Health _vida;
        private RelevoDelControlador _relevo;
        private float _finDeEstado;
        private ESTADOS_VERDUGO _ultimoGolpe = ESTADOS_VERDUGO.HACHAZO;
        private bool _golpeAbierto;
        private bool _aCiegas;
        private Vector2 _ultimaVista;
        private float _vistoEn = float.NegativeInfinity;
        private float _garfioX;
        private float _garfioDesde;
        private int _direccionGarfio;
        private bool _garfioEnVuelo;
        private int _danioPendiente;

        public event Action<ESTADOS_VERDUGO> OnCambio;

        /// <summary>La cadena le dio a Kael.</summary>
        public event Action OnEnganchado;

        public ESTADOS_VERDUGO Estado { get; private set; } = ESTADOS_VERDUGO.MANDA;

        public bool DeRodillas => Estado == ESTADOS_VERDUGO.DE_RODILLAS;

        /// <summary>¿Está buscando a Kael (o barriendo) a ciegas, porque se volvió invisible?</summary>
        public bool ACiegas => _aCiegas;

        /// <summary>Dónde vio a Kael por última vez.</summary>
        public Vector2 UltimaVista => _ultimaVista;

        /// <summary>¿El garfio está volando? ¿Por dónde va?</summary>
        public bool GarfioEnVuelo => _garfioEnVuelo;

        public Vector2 Garfio => new Vector2(_garfioX, transform.position.y + alturaCadena);

        /// <summary>¿Lleva arte? Entonces su Animator dibuja lo que hace y no se tiñe.</summary>
        public bool ConArte => animator != null && animator.runtimeAnimatorController != null;

        /// <summary>
        /// Lo que se le ve hacer: el aviso y el golpe de cada ataque por separado, y al recuperarse, lo
        /// que toca tras ese golpe (sacar el hacha del suelo, ponerse en guardia o tirar de la cadena).
        /// </summary>
        public POSES_VERDUGO Pose
        {
            get
            {
                switch (Estado)
                {
                    case ESTADOS_VERDUGO.HACHAZO:
                        return _golpeAbierto ? POSES_VERDUGO.HACHAZO : POSES_VERDUGO.ALZA_EL_HACHA;
                    case ESTADOS_VERDUGO.BARRIDO:
                        return _golpeAbierto ? POSES_VERDUGO.BARRIDO : POSES_VERDUGO.PREPARA_EL_BARRIDO;
                    case ESTADOS_VERDUGO.CADENA:
                        return _garfioEnVuelo ? POSES_VERDUGO.LANZA : POSES_VERDUGO.VOLTEA_EL_GARFIO;
                    case ESTADOS_VERDUGO.RECUPERA:
                        return _ultimoGolpe == ESTADOS_VERDUGO.BARRIDO ? POSES_VERDUGO.TRAS_EL_BARRIDO
                            : _ultimoGolpe == ESTADOS_VERDUGO.CADENA ? POSES_VERDUGO.TIRA : POSES_VERDUGO.SACA_EL_HACHA;
                    case ESTADOS_VERDUGO.DE_RODILLAS:
                        return POSES_VERDUGO.DE_RODILLAS;
                    case ESTADOS_VERDUGO.BUSCA:
                        return POSES_VERDUGO.BUSCA;
                    default:
                        return POSES_VERDUGO.ANDA;
                }
            }
        }

        /// <summary>
        /// Qué golpe da con Kael a <paramref name="dx"/> en horizontal (positivo, a la derecha) y
        /// <paramref name="dy"/> en vertical, mirando hacia <paramref name="mirando"/>: el barrido si está
        /// a su espalda o pegado; el hachazo si lo tiene delante a su alcance; la cadena si está lejos y a
        /// su altura; y si no, ninguno (<see cref="ESTADOS_VERDUGO.MANDA"/>: que camine hacia él).
        /// </summary>
        public ESTADOS_VERDUGO Elegir(float dx, float dy, int mirando)
        {
            var distancia = Mathf.Abs(dx);
            var delante = dx * mirando > 0f;
            if (Mathf.Abs(dy) > 2.5f)
            {
                return ESTADOS_VERDUGO.MANDA;
            }

            if (distancia < pegado || (!delante && distancia <= radioBarrido))
            {
                return ESTADOS_VERDUGO.BARRIDO;
            }

            if (delante && distancia <= alcanceHachazo - 0.2f)
            {
                return ESTADOS_VERDUGO.HACHAZO;
            }

            if (distancia >= distanciaCadena && distancia <= alcanceCadena && Mathf.Abs(dy) < 1.5f)
            {
                return ESTADOS_VERDUGO.CADENA;
            }

            return ESTADOS_VERDUGO.MANDA;
        }

        public void Agravar(float factor)
        {
            avisoHachazo /= factor;
            avisoBarrido /= factor;
            avisoCadena /= factor;
            recupera /= factor;
        }

        private void Awake()
        {
            _controlador = GetComponent<EnemyController>();
            _vida = GetComponent<Health>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (pivote == null && sprite != null) pivote = sprite.transform.parent != transform ? sprite.transform.parent : sprite.transform;
            _relevo = new RelevoDelControlador(_controlador, _vida, pivote);
            CerrarGolpes();
            MostrarGarfio(false);
        }

        private void OnEnable()
        {
            _vida.OnGolpeRecibido += AlRecibirGolpe;
            Explosiones.AlEstallar += AlEstallar;
        }

        private void OnDisable()
        {
            _vida.OnGolpeRecibido -= AlRecibirGolpe;
            Explosiones.AlEstallar -= AlEstallar;
            _relevo.Soltar();
        }

        private void Update()
        {
            if (!_vida.EstaVivo)
            {
                CerrarGolpes();
                MostrarGarfio(false);
                return;
            }

            var visible = VeAKael();
            switch (Estado)
            {
                case ESTADOS_VERDUGO.MANDA:
                    if (visible)
                    {
                        var dx = _relevo.CentroDeKael.x - _relevo.Centro.x;
                        var dy = _relevo.CentroDeKael.y - _relevo.Centro.y;
                        var golpe = Elegir(dx, dy, _relevo.Mirando);
                        if (golpe != ESTADOS_VERDUGO.MANDA)
                        {
                            Empezar(golpe, dx);
                        }
                    }
                    else if (KaelInvisible && Time.time - _vistoEn < 3f)
                    {
                        // Lo perdió: va a donde lo vio por última vez.
                        _relevo.Tomar();
                        _aCiegas = true;
                        Entrar(ESTADOS_VERDUGO.BUSCA, 6f);
                    }

                    break;

                case ESTADOS_VERDUGO.HACHAZO:
                case ESTADOS_VERDUGO.BARRIDO:
                    Golpear(Estado == ESTADOS_VERDUGO.HACHAZO ? hachazo : barrido);
                    break;

                case ESTADOS_VERDUGO.CADENA:
                    if (!_garfioEnVuelo && Time.time >= _finDeEstado)
                    {
                        Lanzar();
                    }

                    break;

                case ESTADOS_VERDUGO.DE_RODILLAS:
                case ESTADOS_VERDUGO.RECUPERA:
                    if (Time.time >= _finDeEstado)
                    {
                        Volver();
                    }

                    break;

                case ESTADOS_VERDUGO.BUSCA:
                    if (Time.time >= _finDeEstado)
                    {
                        Volver();
                    }

                    break;
            }

            // Antes de que el Animator se evalúe en este fotograma.
            if (ConArte && animator.isInitialized)
            {
                AnimatorParams.FijarEntero(animator, AnimatorParams.EstadoMecanica, (int)Pose);
            }
        }

        private void FixedUpdate()
        {
            if (!_vida.EstaVivo)
            {
                return;
            }

            switch (Estado)
            {
                case ESTADOS_VERDUGO.BUSCA:
                    var dx = _ultimaVista.x - _relevo.Centro.x;
                    var siguiente = _relevo.Centro.x + Mathf.Sign(dx) * Time.fixedDeltaTime;
                    if (Mathf.Abs(dx) <= 0.4f || siguiente < limites.x || siguiente > limites.y ||
                        !_relevo.Mover(dx > 0f ? 1 : -1, _controlador.Datos != null ? _controlador.Datos.velocidadPersecucion : 1.6f))
                    {
                        // Llegó (o no puede seguir): un barrido a ciegas.
                        _relevo.Detener();
                        Empezar(ESTADOS_VERDUGO.BARRIDO, 0f);
                    }

                    break;

                case ESTADOS_VERDUGO.CADENA:
                    _relevo.Detener();
                    if (_garfioEnVuelo)
                    {
                        VolarGarfio();
                    }

                    break;

                case ESTADOS_VERDUGO.MANDA:
                    break;

                default:
                    _relevo.Detener();
                    break;
            }
        }

        // De rodillas, el golpe vale el doble: lo que falta se quita después del golpe, como el Acechador.
        private void LateUpdate()
        {
            if (_danioPendiente <= 0)
            {
                return;
            }

            var extra = _danioPendiente;
            _danioPendiente = 0;
            if (!_vida.EstaVivo)
            {
                return;
            }

            if (extra >= _vida.Vida)
            {
                _relevo.Devolver();
            }

            _vida.PerderVida(extra);
        }

        /// <summary>Lo pone de rodillas, como si le hubiera estallado algo al lado (para las pruebas).</summary>
        public void Arrodillar()
        {
            if (!_vida.EstaVivo)
            {
                return;
            }

            _relevo.Tomar();
            CerrarGolpes();
            MostrarGarfio(false);
            _garfioEnVuelo = false;
            Pintar(colorRodillas);
            Entrar(ESTADOS_VERDUGO.DE_RODILLAS, tiempoRodillas);
        }

        private void AlEstallar(Vector2 centro, float radio)
        {
            var cuerpo = GetComponent<Collider2D>();
            var cerca = cuerpo != null ? (Vector2)cuerpo.ClosestPoint(centro) : _relevo.Centro;
            if (Vector2.Distance(cerca, centro) <= radioRodillas)
            {
                Arrodillar();
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

        // ¿Lo ve? Vivo, visible y a su alcance. Mientras lo ve, apunta dónde está.
        private bool VeAKael()
        {
            var kael = _relevo.Kael;
            if (kael == null || !kael.EstaVivo || KaelInvisible)
            {
                return false;
            }

            var centro = _relevo.CentroDeKael;
            if (Vector2.Distance(centro, _relevo.Centro) > vision)
            {
                return false;
            }

            _ultimaVista = centro;
            _vistoEn = Time.time;
            return true;
        }

        private void Empezar(ESTADOS_VERDUGO golpe, float dx)
        {
            _relevo.Tomar();
            if (golpe != ESTADOS_VERDUGO.BARRIDO && Mathf.Abs(dx) > 0.01f)
            {
                _relevo.Mirar(dx > 0f ? 1 : -1);
            }

            _golpeAbierto = false;
            _ultimoGolpe = golpe;
            Pintar(colorAviso);
            var aviso = golpe == ESTADOS_VERDUGO.HACHAZO ? avisoHachazo : golpe == ESTADOS_VERDUGO.BARRIDO ? avisoBarrido : avisoCadena;
            Entrar(golpe, aviso);
        }

        // Pasado el aviso, la zona de daño se abre un momento; después, recupera el hacha.
        private void Golpear(Hitbox zona)
        {
            if (Time.time < _finDeEstado)
            {
                return;
            }

            if (!_golpeAbierto)
            {
                _golpeAbierto = true;
                if (zona != null)
                {
                    zona.ReiniciarObjetivos();
                    zona.gameObject.SetActive(true);
                }

                return;
            }

            if (Time.time >= _finDeEstado + duracionGolpe)
            {
                CerrarGolpes();
                Pintar(Tinte);
                Entrar(ESTADOS_VERDUGO.RECUPERA, recupera);
            }
        }

        private void Lanzar()
        {
            _direccionGarfio = _relevo.Mirando;
            _garfioDesde = _relevo.Centro.x;
            _garfioX = _garfioDesde;
            _garfioEnVuelo = true;
            MostrarGarfio(true);
            Pintar(Tinte);
        }

        private void VolarGarfio()
        {
            _garfioX += _direccionGarfio * velocidadCadena * Time.fixedDeltaTime;
            ColocarGarfio();

            foreach (var otro in Physics2D.OverlapCircleAll(Garfio, radioGarfio, Capas.Mascara(Capas.Player)))
            {
                if (otro.isTrigger || otro.attachedRigidbody == null || !otro.attachedRigidbody.TryGetComponent<Health>(out var vidaKael))
                {
                    continue;
                }

                // El golpe viene del otro lado de Kael: su retroceso lo lleva hacia el Verdugo.
                var origen = (Vector2)otro.bounds.center + new Vector2(_direccionGarfio, 0f);
                vidaKael.RecibirGolpe(new Golpe(danioCadena, origen, gameObject));
                OnEnganchado?.Invoke();
                Recoger();
                return;
            }

            if (Mathf.Abs(_garfioX - _garfioDesde) >= alcanceCadena || Physics2D.OverlapCircle(Garfio, radioGarfio, Capas.Mascara(Capas.Wall)) != null)
            {
                Recoger();
            }
        }

        private void Recoger()
        {
            _garfioEnVuelo = false;
            MostrarGarfio(false);
            Entrar(ESTADOS_VERDUGO.RECUPERA, recupera);
        }

        private void Volver()
        {
            CerrarGolpes();
            MostrarGarfio(false);
            _garfioEnVuelo = false;
            _aCiegas = false;
            Pintar(Tinte);
            Entrar(ESTADOS_VERDUGO.MANDA, 0f);
            _relevo.Devolver();
        }

        private void AlRecibirGolpe(Golpe golpe)
        {
            _relevo.AntesDeMorir();
            if (DeRodillas && _vida.EstaVivo)
            {
                _danioPendiente += Mathf.CeilToInt(golpe.Cantidad * (multiplicadorRodillas - 1f));
            }
        }

        private void CerrarGolpes()
        {
            if (hachazo != null) hachazo.gameObject.SetActive(false);
            if (barrido != null) barrido.gameObject.SetActive(false);
        }

        private void MostrarGarfio(bool visible)
        {
            if (garfio != null)
            {
                garfio.gameObject.SetActive(visible);
            }

            if (cadena != null)
            {
                cadena.gameObject.SetActive(visible);
            }

            if (visible)
            {
                ColocarGarfio();
            }
        }

        /// <summary>Lo que ha salido la cadena de su mano, a lo ancho (0 si no vuela o si el garfio no ha pasado su mano).</summary>
        public float LargoCadena => _garfioEnVuelo ? Mathf.Max(0f, (_garfioX - (_garfioDesde + _direccionGarfio * manoCadena)) * _direccionGarfio) : 0f;

        private void ColocarGarfio()
        {
            var altura = transform.position.y + alturaCadena;
            if (garfio != null)
            {
                garfio.position = new Vector3(_garfioX, altura, 0f);
                // Con arte está dibujado colgando (los eslabones arriba): el garfio va por delante y
                // los eslabones miran a su mano.
                if (cadena != null)
                {
                    garfio.rotation = Quaternion.Euler(0f, 0f, _direccionGarfio < 0 ? -90f : 90f);
                }
            }

            if (cadena != null)
            {
                // De la mano al garfio: baja desde su mano hasta la altura a la que vuela el garfio.
                var mano = new Vector2(_garfioDesde + _direccionGarfio * manoCadena, transform.position.y + alturaMano);
                var tramo = new Vector2(_garfioX, altura) - mano;
                var largo = LargoCadena > 0.01f ? tramo.magnitude : 0f;
                cadena.transform.position = (Vector3)(mano + tramo * 0.5f);
                cadena.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tramo.y, tramo.x) * Mathf.Rad2Deg);
                cadena.size = new Vector2(largo, cadena.size.y);
                cadena.enabled = largo > 0.01f;
            }
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

        private void Entrar(ESTADOS_VERDUGO nuevo, float duracion)
        {
            Estado = nuevo;
            _finDeEstado = Time.time + duracion;
            OnCambio?.Invoke(nuevo);
        }
    }
}
