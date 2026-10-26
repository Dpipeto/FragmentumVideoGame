using System;
using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Levels.Escenario;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Espectro del Pavor, jefe del mundo del Miedo (<c>Docs/Diseno/Nivel3_Diseno.md</c>, sección 6).
    /// Sin arte: una cápsula violeta que, cuando se deja ver, se ve desde cualquier sitio (su dibujo
    /// no depende de la luz, como sus vetas y su máscara en el concepto).
    ///
    /// - <b>Invisible e intangible.</b> Casi todo el combate no se ve y los golpes lo atraviesan
    ///   (<see cref="SaludJefe.Intangible"/>). Se le ve, y se le puede golpear, desde que avisa con
    ///   su destello hasta que se recupera de su ataque; mientras jadea (la ventana vulnerable), los
    ///   golpes valen el doble. Antes del combate espera visible en el centro de la arena: es su
    ///   presentación. Tocarlo no hace daño: lo hacen sus ataques.
    /// - <b>Aparece donde le toca:</b> es un cuerpo cinemático que sus patrones colocan
    ///   (<see cref="Aparecer"/>) y mueven (<see cref="Cruzar"/>). Ni cae ni choca.
    /// - <b>Fase 1:</b> tajo, embestida, tajo y hojas. <b>Fase 2</b> (50 %): grita (la oscuridad late
    ///   y la luz de Kael parpadea), y sus sombras, hojas, sombras y embestida. Solo la figura
    ///   verdadera lleva una luciérnaga dorada; golpear a una sombra la deshace y encoge la luz de
    ///   Kael (<see cref="SombraDelPavor"/>).
    /// - <b>Muerte:</b> la oscuridad retrocede (<see cref="LuzDelNivel.Retroceder"/>): la luz sube y
    ///   el bosque se ve entero.
    ///
    /// Los ataques son datos (<c>Assets/Data/Jefes/Patrones</c>): <c>PatronTajo</c>,
    /// <c>PatronEmbestidaSombra</c>, <c>PatronHojas</c> y <c>PatronSombras</c>. Aquí está lo que es
    /// suyo: verse o no, ser tangible o no, aparecer y llevar sus sombras.
    /// </summary>
    public class EspectroDelPavor : BossController
    {
        private const float Llegada = 0.05f;
        private const float DestelloDeGolpe = 0.12f;

        [Header("Invisible")]
        [Tooltip("Lo rápido que aparece y se desvanece (opacidad por segundo).")]
        [SerializeField, Min(0.1f)] private float velocidadFundido = 6f;

        [Tooltip("Al empezar el combate, lo que tarda en desvanecerse antes de su primer ataque.")]
        [SerializeField, Min(0f)] private float esperaAlEmpezar = 1.2f;

        [Header("El grito (fase 2)")]
        [Tooltip("Al empezar la fase 2, la oscuridad late y la luz de Kael parpadea estos segundos.")]
        [SerializeField, Min(0f)] private float grito = 0.9f;

        [Header("Golpear a una sombra")]
        [Tooltip("La luz de Kael se encoge a esta fracción de su radio...")]
        [SerializeField, Range(0.1f, 1f)] private float luzTrasLaSombra = 0.5f;

        [Tooltip("...durante estos segundos.")]
        [SerializeField, Min(0f)] private float segundosLuzEncogida = 3f;

        [Header("Al morir: la oscuridad retrocede")]
        [Tooltip("La luz del nivel sube hasta esta intensidad...")]
        [SerializeField, Range(0f, 2f)] private float luzAlMorir = 0.85f;

        [Tooltip("...en estos segundos.")]
        [SerializeField, Min(0f)] private float subidaAlMorir = 3f;

        [Tooltip("El color de esa luz: el de la luna.")]
        [SerializeField] private Color colorAlMorir = new Color(0.7f, 0.75f, 1f, 1f);

        [Header("Sin arte")]
        [Tooltip("El destello del aviso: la máscara, las vetas y el filo encendidos en verde pálido.")]
        [SerializeField] private Color colorDestello = new Color(0.72f, 1f, 0.72f, 1f);

        [Tooltip("La luciérnaga de la figura verdadera, en la fase de las sombras.")]
        [SerializeField] private Color colorLuciernaga = new Color(1f, 0.82f, 0.3f, 1f);

        [Tooltip("La de sus sombras.")]
        [SerializeField] private Color colorLuciernagaSombra = new Color(0.45f, 0.95f, 0.5f, 1f);

        private readonly List<SombraDelPavor> _sombras = new List<SombraDelPavor>();
        private Rigidbody2D _cuerpo;
        private SpriteRenderer _sprite;
        private Transform _luciernaga;
        private PlayerController _kael;
        private float _suelo;
        private bool _visible;
        private float _opacidad = 1f;
        private float _finDestello = float.NegativeInfinity;
        private float _finGolpe = float.NegativeInfinity;
        private bool _cruzando;
        private float _destinoCruce;
        private float _velocidadCruce;

        /// <summary>Golpearon a una de sus sombras: se deshizo y la luz de Kael se encogió.</summary>
        public event Action<SombraDelPavor> OnSombraGolpeada;

        /// <summary>
        /// ¿Se le ve? Antes del combate, sí (espera en su arena); en combate, solo desde que avisa
        /// hasta que se recupera, y mientras jadea.
        /// </summary>
        public bool Visible => !EnCombate || _visible || EsVulnerable;

        /// <summary>¿Le alcanzan los golpes? Lo mismo que verse: cuando no se le ve, lo atraviesan.</summary>
        public bool Tangible => Visible;

        /// <summary>Lo que se le ve ahora, de 0 (nada) a 1 (entero).</summary>
        public float Opacidad => _opacidad;

        /// <summary>¿Está destellando? (El aviso de un ataque.)</summary>
        public bool Destellando => Time.time < _finDestello;

        /// <summary>La altura del suelo de su arena: donde aparece.</summary>
        public float Suelo => _suelo;

        /// <summary>Donde tiene los pies.</summary>
        public Vector2 Pies => _cuerpo != null ? _cuerpo.position : (Vector2)transform.position;

        public bool Cruzando => _cruzando;

        /// <summary>Sus sombras de ahora (fase 2).</summary>
        public IReadOnlyList<SombraDelPavor> Sombras => _sombras;

        /// <summary>¿Lleva la luciérnaga dorada? (Mientras están sus sombras.)</summary>
        public bool ConLuciernaga => _luciernaga != null && _luciernaga.gameObject.activeSelf;

        public Color ColorLuciernagaSombra => colorLuciernagaSombra;

        public float LuzTrasLaSombra => luzTrasLaSombra;

        public float SegundosLuzEncogida => segundosLuzEncogida;

        public float LuzAlMorir => luzAlMorir;

        public float SubidaAlMorir => subidaAlMorir;

        public float Grito => grito;

        // --- Ciclo de vida ---

        protected override void Awake()
        {
            base.Awake();
            _cuerpo = GetComponent<Rigidbody2D>();

            // Aparece y se desvanece: ni cae ni choca. Lo colocan sus patrones.
            _cuerpo.bodyType = RigidbodyType2D.Kinematic;
            _cuerpo.gravityScale = 0f;
            _cuerpo.linearVelocity = Vector2.zero;

            _sprite = GetComponentInChildren<SpriteRenderer>();
            _suelo = transform.position.y;
            Vida.Intangible = () => EnCombate && !Tangible;
            _luciernaga = CrearLuciernaga(transform, colorLuciernaga, _sprite);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Vida.OnGolpeRecibido += AlRecibirGolpe;
            OnStateChanged += AlCambiarDeEstado;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Vida.OnGolpeRecibido -= AlRecibirGolpe;
            OnStateChanged -= AlCambiarDeEstado;
        }

        protected override void Update()
        {
            base.Update();
            if (_sprite == null || Derrotado)
            {
                return;
            }

            _opacidad = Destellando ? 1f : Mathf.MoveTowards(_opacidad, Visible ? 1f : 0f, velocidadFundido * Time.deltaTime);

            var tinte = Datos != null ? Datos.tinte : Color.white;
            Color color;
            if (Time.time < _finGolpe)
            {
                color = Color.white;
            }
            else if (Destellando)
            {
                // El destello del aviso: se enciende y se apaga, y siempre se ve.
                color = Mathf.Repeat(Time.time * 8f, 1f) < 0.5f ? colorDestello : Color.Lerp(tinte, colorDestello, 0.4f);
            }
            else if (RecibeDanioDoble)
            {
                color = ColorVentana(tinte);
            }
            else
            {
                color = tinte;
            }

            color.a = _opacidad;
            _sprite.color = color;
            Orbitar(_luciernaga, Centro + Vector2.up * 1.2f, 0f);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!_cruzando || _cuerpo == null)
            {
                return;
            }

            var pies = _cuerpo.position;
            var x = Mathf.MoveTowards(pies.x, _destinoCruce, _velocidadCruce * Time.fixedDeltaTime);
            _cuerpo.MovePosition(new Vector2(x, _suelo));
            if (Mathf.Abs(x - _destinoCruce) <= Llegada)
            {
                _cruzando = false;
            }
        }

        // --- Para sus patrones ---

        /// <summary>Se pone de pie en <paramref name="x"/> (dentro de la arena), de cara a Kael.</summary>
        public void Aparecer(float x)
        {
            _cruzando = false;
            Posar(new Vector2(DentroDeLaArena(x), _suelo));
            MirarAlJugador();
        }

        /// <summary>Se deja ver (y golpear) hasta que se oculte.</summary>
        public void Mostrarse() => _visible = true;

        /// <summary>Vuelve a ser invisible e intangible.</summary>
        public void Ocultarse() => _visible = false;

        /// <summary>
        /// El aviso de un ataque: se deja ver y destella <paramref name="segundos"/>. El destello se
        /// ve desde cualquier sitio de la arena.
        /// </summary>
        public void Destellar(float segundos)
        {
            Mostrarse();
            _opacidad = 1f;
            _finDestello = Time.time + Mathf.Max(0f, segundos);
            Avisar(segundos);
        }

        /// <summary>Cruza a ras de suelo hasta <paramref name="x"/> a <paramref name="velocidad"/> u/s (la embestida).</summary>
        public void Cruzar(float x, float velocidad)
        {
            _destinoCruce = DentroDeLaArena(x);
            _velocidadCruce = Mathf.Max(0.1f, velocidad);
            _cruzando = true;
            Mirar(_destinoCruce >= Pies.x ? 1 : -1);
        }

        /// <summary>
        /// Dónde aparecer para el tajo: a <paramref name="distancia"/> de Kael, a su espalda; si a su
        /// espalda no cabe (Kael está contra una pared), delante de él.
        /// </summary>
        public float LugarParaElTajo(float distancia)
        {
            var kael = CentroJugador.x;
            var mirando = KaelMirando;
            var limites = Limites;
            var minimo = limites.x + MedioAncho;
            var maximo = limites.y - MedioAncho;
            var espalda = kael - mirando * distancia;
            if (espalda >= minimo && espalda <= maximo)
            {
                return espalda;
            }

            return Mathf.Clamp(kael + mirando * distancia, minimo, maximo);
        }

        /// <summary>Junto a qué pared aparecer para la embestida: la más lejana a Kael, para cruzar por donde está.</summary>
        public float ParedParaLaEmbestida()
        {
            var limites = Limites;
            var kael = CentroJugador.x;
            var izquierda = limites.x + MedioAncho + 0.2f;
            var derecha = limites.y - MedioAncho - 0.2f;
            return kael - izquierda >= derecha - kael ? izquierda : derecha;
        }

        /// <summary>La otra pared: hasta donde cruza la embestida.</summary>
        public float ParedDeEnfrente(float desde)
        {
            var limites = Limites;
            var centro = (limites.x + limites.y) * 0.5f;
            return desde <= centro ? limites.y - MedioAncho - 0.2f : limites.x + MedioAncho + 0.2f;
        }

        /// <summary>
        /// Que una zona de sus ataques se vea a oscuras: el suelo que brilla bajo las hojas, el filo del
        /// tajo. Lleva el material de su dibujo, que no depende de la luz.
        /// </summary>
        public void VerEnLaOscuridad(ZonaDeDanio zona)
        {
            var dibujo = zona != null ? zona.GetComponent<SpriteRenderer>() : null;
            if (dibujo != null && _sprite != null)
            {
                dibujo.sharedMaterial = _sprite.sharedMaterial;
            }
        }

        /// <summary>Una sombra suya en <paramref name="x"/>: igual que él, con una luciérnaga verde.</summary>
        public SombraDelPavor CrearSombra(float x)
        {
            var sombra = SombraDelPavor.Crear(this, new Vector2(DentroDeLaArena(x), _suelo), _sprite);
            _sombras.Add(sombra);
            return sombra;
        }

        /// <summary>Lleva (o deja de llevar) su luciérnaga dorada: la señal de la figura verdadera.</summary>
        public void LlevarLuciernaga(bool lleva)
        {
            if (_luciernaga != null)
            {
                _luciernaga.gameObject.SetActive(lleva);
            }
        }

        /// <summary>Deshace todas sus sombras (le golpearon a él, acabó la fase o el combate).</summary>
        public void DeshacerSombras()
        {
            foreach (var sombra in _sombras)
            {
                if (sombra != null)
                {
                    sombra.Deshacer();
                }
            }

            _sombras.Clear();
            LlevarLuciernaga(false);
        }

        /// <summary>Kael golpeó una sombra: se deshace en hojas y la luz de Kael se encoge un rato.</summary>
        internal void SombraGolpeada(SombraDelPavor sombra)
        {
            _sombras.Remove(sombra);
            var luz = LuzDeKael;
            if (luz != null)
            {
                luz.Encoger(luzTrasLaSombra, segundosLuzEncogida);
            }

            OnSombraGolpeada?.Invoke(sombra);
        }

        /// <summary>
        /// Coloca una luciérnaga dando vueltas alrededor de <paramref name="centro"/> (la cabeza de
        /// una figura). La usan él y sus sombras.
        /// </summary>
        internal static void Orbitar(Transform luciernaga, Vector2 centro, float desfase)
        {
            if (luciernaga == null || !luciernaga.gameObject.activeSelf)
            {
                return;
            }

            var angulo = Time.time * 4f + desfase;
            luciernaga.position = centro + new Vector2(Mathf.Cos(angulo) * 0.8f, Mathf.Sin(angulo) * 0.35f);
        }

        /// <summary>Una luciérnaga provisional (un punto de luz que no depende de la oscuridad), apagada.</summary>
        internal static Transform CrearLuciernaga(Transform padre, Color color, SpriteRenderer como)
        {
            var objeto = new GameObject("Luciernaga");
            objeto.transform.SetParent(padre, false);
            objeto.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            var dibujo = objeto.AddComponent<SpriteRenderer>();
            dibujo.sprite = SpriteProvisional.Cuadrado();
            dibujo.color = color;
            if (como != null)
            {
                dibujo.sharedMaterial = como.sharedMaterial;
                dibujo.sortingOrder = como.sortingOrder + 1;
            }

            objeto.SetActive(false);
            return objeto.transform;
        }

        // --- Template Method ---

        // Empieza el combate: deja de verse y, mientras se desvanece, no ataca.
        protected override void AlActivarse()
        {
            _kael = PlayerController.BuscarEnEscena(gameObject.scene);
            Ocultarse();
        }

        protected override float EsperaAlEmpezarFase(int fase, FaseJefe datosFase) => fase <= 1 ? esperaAlEmpezar : grito + 0.3f;

        // La fase 2 empieza con su grito: la oscuridad late y la luz de Kael parpadea.
        protected override void AlCambiarDeFase(int fase)
        {
            DeshacerSombras();
            Ocultarse();
            _cruzando = false;
            if (fase < 2)
            {
                return;
            }

            var oscuridad = Oscuridad;
            if (oscuridad != null)
            {
                oscuridad.Latir(grito);
            }

            var luz = LuzDeKael;
            if (luz != null)
            {
                luz.Parpadear(grito);
            }
        }

        // Kael murió: vuelve a esperar visible en su arena, sin sombras, y la luz de Kael, entera.
        protected override void AlReiniciarse()
        {
            DeshacerSombras();
            _visible = false;
            _cruzando = false;
            _opacidad = 1f;
            _finDestello = float.NegativeInfinity;
            var luz = LuzDeKael;
            if (luz != null)
            {
                luz.Restablecer();
            }
        }

        // La oscuridad retrocede: la luz sube y el bosque se ve entero por primera vez.
        protected override void AlSerDerrotado()
        {
            DeshacerSombras();
            _cruzando = false;
            _visible = true;
            var oscuridad = Oscuridad;
            if (oscuridad != null)
            {
                oscuridad.Retroceder(luzAlMorir, colorAlMorir, subidaAlMorir);
            }

            var luz = LuzDeKael;
            if (luz != null)
            {
                luz.Restablecer();
            }
        }

        // Jadea en oro: es la señal de que ahora los golpes valen el doble.
        protected override Color ColorVentana(Color colorBase) =>
            Color.Lerp(colorBase, new Color(1f, 0.85f, 0.45f, 1f), 0.45f + 0.25f * Mathf.Sin(Time.time * 18f));

        // --- Internos ---

        private int KaelMirando
        {
            get
            {
                if (_kael == null)
                {
                    _kael = PlayerController.BuscarEnEscena(gameObject.scene);
                }

                return _kael != null ? _kael.Mirando : -DireccionHaciaJugador;
            }
        }

        private LuzJugador LuzDeKael
        {
            get
            {
                if (_kael == null)
                {
                    _kael = PlayerController.BuscarEnEscena(gameObject.scene);
                }

                return _kael != null ? _kael.GetComponent<LuzJugador>() : null;
            }
        }

        private static LuzDelNivel Oscuridad => FindAnyObjectByType<LuzDelNivel>();

        private void AlRecibirGolpe(Golpe golpe)
        {
            _finGolpe = Time.time + DestelloDeGolpe;

            // Golpear a la figura verdadera deshace a las sombras.
            if (_sombras.Count > 0)
            {
                DeshacerSombras();
            }
        }

        // Al acabar de jadear (o al cortar el ataque un cambio de fase) vuelve a no verse.
        private void AlCambiarDeEstado(ESTADOS_JEFE anterior, ESTADOS_JEFE nuevo)
        {
            if (anterior == ESTADOS_JEFE.VULNERABLE && nuevo != ESTADOS_JEFE.DEFEATED)
            {
                DeshacerSombras();
                Ocultarse();
            }
        }

        private float DentroDeLaArena(float x)
        {
            var limites = Limites;
            var minimo = limites.x + MedioAncho;
            var maximo = limites.y - MedioAncho;
            return minimo <= maximo ? Mathf.Clamp(x, minimo, maximo) : (limites.x + limites.y) * 0.5f;
        }

        private void Posar(Vector2 pies)
        {
            _cuerpo.position = pies;
            transform.position = new Vector3(pies.x, pies.y, transform.position.z);
        }
    }
}
