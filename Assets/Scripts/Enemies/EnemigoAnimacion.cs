using System.Collections;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Enemies
{
    /// <summary>
    /// Une el Animator de un enemigo con arte (el Eco de la Ira y, con los mismos clips, los
    /// demás Ecos) con su <see cref="EnemyController"/>. Va en el mismo objeto que el Animator
    /// porque los eventos de animación solo llegan a los scripts de ese objeto.
    ///
    /// - Reenvía los eventos de los clips: <see cref="AbrirGolpe"/> (a2) y
    ///   <see cref="CerrarGolpe"/> (a4) marcan la ventana en la que el arma hace daño,
    ///   <see cref="FinAtaque"/> termina el ataque y <see cref="FinMuerte"/> remata la muerte.
    /// - <see cref="velocidadAnimacion"/> marca el ritmo de todo el Animator: otro Eco con los
    ///   mismos clips va más lento o más rápido sin duplicar animaciones.
    /// - <see cref="velocidadAtaque"/> acelera además solo el clip de ataque, sin cambiar sus
    ///   proporciones: la anticipación sigue siendo más larga que el golpe.
    /// - El estado Run va al ritmo de la velocidad real del cuerpo: las piernas no patinan y,
    ///   parado a tiro durante el cooldown, el Eco no corre en el sitio.
    /// - Al recibir un golpe, el sprite se vuelve una silueta (blanca por defecto,
    ///   <see cref="colorDestello"/>) durante 0,1 s. No hay fotogramas de daño: es todo código.
    /// - Al acabar el clip de muerte apaga los colliders, suelta las partículas (si las hay),
    ///   deja los restos en pantalla <see cref="restosEnPantalla"/> s, funde el sprite y destruye
    ///   al enemigo.
    ///
    /// La FSM del <see cref="EnemyController"/> sigue siendo la que decide: este componente no
    /// cambia de estado por su cuenta, solo le avisa de lo que pasa en el clip.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class EnemigoAnimacion : MonoBehaviour
    {
        // Margen sobre la duración del clip de muerte antes de rematarla sin su evento.
        private const float MargenSeguridadMuerte = 1f;

        private static readonly int ColorSilueta = Shader.PropertyToID("_Color");

        [Tooltip("Ritmo de todas las animaciones (Animator.speed). Los Ecos comparten clips y se distinguen por su ritmo. " +
                 "También acorta la anticipación del ataque: a1 dura 0,35 s / (este valor × velocidadAtaque).")]
        [Range(0.4f, 2f)] public float velocidadAnimacion = 1f;

        [Tooltip("Multiplica solo el clip de ataque, encima del ritmo general: a 1,6 la anticipación (a1) dura 0,35 / 1,6 ≈ 0,22 s. " +
                 "Los eventos del golpe se mueven con el clip, así que la ventana de daño sigue siendo a2 y a3.")]
        [Range(0.5f, 3f)] public float velocidadAtaque = 1f;

        [Tooltip("Velocidad del cuerpo (u/s) a la que el ciclo de correr, a ritmo 1, avanza sin patinar. La mide ConstructorEcoIra en el arte.")]
        [SerializeField, Min(0.01f)] private float velocidadNaturalCarrera = 1.3f;

        [Header("Destello al recibir un golpe")]
        [Tooltip("Material de silueta (shader Fragmentum/SiluetaSprite). Vacío = sin destello.")]
        [SerializeField] private Material materialDestello;

        [SerializeField, Min(0f)] private float duracionDestello = 0.1f;

        [Tooltip("Color de la silueta. El color del SpriteRenderer solo multiplica y no puede aclarar el sprite; la silueta lo sustituye entero.")]
        [SerializeField] private Color colorDestello = Color.white;

        [Header("Muerte")]
        [Tooltip("Partículas que salen de la cavidad del pecho al acabar el clip de muerte.")]
        [SerializeField] private ParticleSystem estallido;

        [Tooltip("Segundos que el último fotograma de la muerte (los restos) se queda en pantalla antes de desvanecerse.")]
        [SerializeField, Min(0f)] private float restosEnPantalla;

        [Tooltip("Segundos que tarda el sprite en desvanecerse tras el clip de muerte.")]
        [SerializeField, Min(0f)] private float duracionFundido = 0.4f;

        private Animator _animator;
        private SpriteRenderer _sprite;
        private EnemyController _enemigo;
        private Rigidbody2D _cuerpo;
        private Health _vida;
        private Material _materialNormal;
        private Material _destello;
        private bool _tieneVelocidadCorrer;
        private bool _tieneVelocidadAtaque;
        private float _finDestello = float.NegativeInfinity;
        private float _duracionAtaque;
        private float _duracionMuerte;
        private bool _muriendo;
        private bool _muerteRematada;
        private Coroutine _seguridadMuerte;

        /// <summary>Hay Animator con controlador: los eventos de los clips van a llegar.</summary>
        public bool Lista => isActiveAndEnabled && _animator != null && _animator.runtimeAnimatorController != null;

        /// <summary>Velocidad del cuerpo (u/s) a la que el ciclo de correr, a ritmo 1, no patina.</summary>
        public float VelocidadNaturalCarrera => velocidadNaturalCarrera;

        /// <summary>Lo que dura el clip de ataque (hasta su evento FinAtaque) al ritmo actual.</summary>
        public float DuracionAtaque => _duracionAtaque / (Ritmo * RitmoAtaque);

        /// <summary>Lo que dura el clip de muerte (hasta su evento FinMuerte) al ritmo actual.</summary>
        public float DuracionMuerte => _duracionMuerte / Ritmo;

        // velocidadAnimacion, a salvo de un 0 puesto por código (el inspector no baja de 0,4).
        private float Ritmo => Mathf.Max(0.01f, velocidadAnimacion);

        // velocidadAtaque, igual de a salvo. Si el controlador no tiene el parámetro, no se aplica.
        private float RitmoAtaque => _tieneVelocidadAtaque ? Mathf.Max(0.01f, velocidadAtaque) : 1f;

        /// <summary>El sprite se ve ahora como silueta.</summary>
        public bool DestelloActivo => _destello != null && _sprite != null && _sprite.sharedMaterial == _destello;

        /// <summary>Color de la silueta del destello.</summary>
        public Color ColorDestello => colorDestello;

        /// <summary>Segundos que los restos se quedan en pantalla antes de desvanecerse.</summary>
        public float RestosEnPantalla => restosEnPantalla;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _sprite = GetComponent<SpriteRenderer>();
            _enemigo = GetComponentInParent<EnemyController>();
            _cuerpo = GetComponentInParent<Rigidbody2D>();
            _vida = GetComponentInParent<Health>();
            _materialNormal = _sprite.sharedMaterial;
            _destello = MaterialDestello();
            LeerDuraciones();
        }

        private void OnDestroy()
        {
            if (_destello != null && _destello != materialDestello)
            {
                Destroy(_destello);
            }
        }

        // El material de silueta tal cual o, si el color pedido es otro, una copia propia con él.
        private Material MaterialDestello()
        {
            if (materialDestello == null)
            {
                return null;
            }

            if (!materialDestello.HasProperty(ColorSilueta) || materialDestello.GetColor(ColorSilueta) == colorDestello)
            {
                return materialDestello;
            }

            var copia = new Material(materialDestello) { name = $"{materialDestello.name} ({name})" };
            copia.SetColor(ColorSilueta, colorDestello);
            return copia;
        }

        private void OnEnable()
        {
            if (_vida != null)
            {
                _vida.OnGolpeRecibido += AlRecibirGolpe;
            }
        }

        private void OnDisable()
        {
            if (_vida != null)
            {
                _vida.OnGolpeRecibido -= AlRecibirGolpe;
            }
        }

        private void Start()
        {
            // Los parámetros solo se pueden leer con el Animator ya en marcha.
            foreach (var parametro in _animator.parameters)
            {
                _tieneVelocidadCorrer |= parametro.nameHash == AnimatorParams.VelocidadCorrer.Hash;
                _tieneVelocidadAtaque |= parametro.nameHash == AnimatorParams.VelocidadAtaque.Hash;
            }
        }

        private void Update()
        {
            _animator.speed = Ritmo;
            if (_tieneVelocidadAtaque)
            {
                _animator.SetFloat(AnimatorParams.VelocidadAtaque.Hash, RitmoAtaque);
            }

            if (!_tieneVelocidadCorrer || _cuerpo == null)
            {
                return;
            }

            // velocidadAnimacion ya acelera todo el Animator: se descuenta aquí para que las
            // piernas sigan exactamente al cuerpo. Aturdido sale despedido, pero no corre.
            var aturdido = _enemigo != null && _enemigo.EstadoActual == ESTADOS_ENEMIGO.DAMAGED;
            var ritmo = aturdido ? 0f : Mathf.Abs(_cuerpo.linearVelocity.x) / (velocidadNaturalCarrera * Ritmo);
            _animator.SetFloat(AnimatorParams.VelocidadCorrer.Hash, ritmo);
        }

        private void LateUpdate()
        {
            if (DestelloActivo && Time.time >= _finDestello)
            {
                _sprite.sharedMaterial = _materialNormal;
            }
        }

        // --- Eventos de animación: los pone ConstructorEcoIra en los clips ---

        /// <summary>Primer instante de a2: el golpe empieza a hacer daño.</summary>
        public void AbrirGolpe()
        {
            if (_enemigo != null)
            {
                _enemigo.AbrirGolpe();
            }
        }

        /// <summary>Primer instante de a4: el golpe deja de hacer daño.</summary>
        public void CerrarGolpe()
        {
            if (_enemigo != null)
            {
                _enemigo.CerrarGolpe();
            }
        }

        /// <summary>Final del clip de ataque: la FSM vuelve a perseguir.</summary>
        public void FinAtaque()
        {
            if (_enemigo != null)
            {
                _enemigo.CancelAttack();
            }
        }

        /// <summary>
        /// Final del clip de muerte: apaga los colliders, suelta las partículas desde el pecho,
        /// funde el sprite y destruye al enemigo.
        /// </summary>
        public void FinMuerte()
        {
            if (!_muriendo || _muerteRematada)
            {
                return;
            }

            _muerteRematada = true;
            if (_seguridadMuerte != null)
            {
                StopCoroutine(_seguridadMuerte);
                _seguridadMuerte = null;
            }

            var raiz = _enemigo != null ? _enemigo.gameObject : gameObject;
            foreach (var colisionador in raiz.GetComponentsInChildren<Collider2D>())
            {
                colisionador.enabled = false;
            }

            _sprite.sharedMaterial = _materialNormal;
            SoltarEstallido();
            StartCoroutine(FundirYDestruir(raiz));
        }

        /// <summary>
        /// Lo llama <see cref="EnemyController"/> al entrar en DEAD, con el clip de muerte ya
        /// pedido al Animator. Si su evento no llegara, la muerte se remata igual.
        /// </summary>
        internal void Morir()
        {
            if (_muriendo)
            {
                return;
            }

            _muriendo = true;
            _seguridadMuerte = StartCoroutine(RematarSinEvento(DuracionMuerte + MargenSeguridadMuerte));
        }

        // --- Interno ---

        private void LeerDuraciones()
        {
            // La duración de cada clip es el instante de su evento final: así no depende de cómo
            // se llamen los clips, y un AnimatorOverrideController de otro Eco también vale.
            var controlador = _animator.runtimeAnimatorController;
            if (controlador == null)
            {
                return;
            }

            foreach (var clip in controlador.animationClips)
            {
                foreach (var evento in clip.events)
                {
                    if (evento.functionName == nameof(FinAtaque))
                    {
                        _duracionAtaque = evento.time;
                    }
                    else if (evento.functionName == nameof(FinMuerte))
                    {
                        _duracionMuerte = evento.time;
                    }
                }
            }
        }

        private void AlRecibirGolpe(Golpe golpe)
        {
            if (_destello == null || _muerteRematada)
            {
                return;
            }

            _sprite.sharedMaterial = _destello;
            _finDestello = Time.time + duracionDestello;
        }

        private void SoltarEstallido()
        {
            if (estallido == null)
            {
                return;
            }

            // Se suelta del enemigo para que destruirlo no corte las partículas en el aire.
            var transformada = estallido.transform;
            transformada.SetParent(null, true);
            transformada.localScale = Vector3.one;
            var principal = estallido.main;
            principal.stopAction = ParticleSystemStopAction.Destroy;
            estallido.Play();
        }

        private IEnumerator FundirYDestruir(GameObject raiz)
        {
            if (restosEnPantalla > 0f)
            {
                yield return new WaitForSeconds(restosEnPantalla);
            }

            var color = _sprite.color;
            var alfaInicial = color.a;
            for (var t = 0f; t < duracionFundido; t += Time.deltaTime)
            {
                color.a = alfaInicial * (1f - t / duracionFundido);
                _sprite.color = color;
                yield return null;
            }

            color.a = 0f;
            _sprite.color = color;
            Destroy(raiz);
        }

        private IEnumerator RematarSinEvento(float espera)
        {
            yield return new WaitForSeconds(espera);
            _seguridadMuerte = null;
            FinMuerte();
        }
    }
}
