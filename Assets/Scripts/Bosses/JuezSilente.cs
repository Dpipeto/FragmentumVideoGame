using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Data;
using Fragmentum.Levels;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Lo que hace el Juez con su arte: cada gesto es un estado de su Animator, con su disparador
    /// del mismo nombre. Andar no es un gesto: desde el reposo anda solo, si se mueve (Walk). La
    /// condena es su grito (Rugido) y la muerte, la de todos los jefes.
    /// </summary>
    public enum GESTOS_JUEZ
    {
        /// <summary>De pie, respirando, la balanza meciéndose (I1-I4). Si se mueve, anda (W1-W4).</summary>
        REPOSO,

        /// <summary>La orden, el aviso: la espada en alto y la luz del pecho encendida (O1).</summary>
        AVISO_ORDEN,

        /// <summary>La orden: señala al frente con la espada (O2) y la baja (O3).</summary>
        ORDEN,

        /// <summary>El pueblo, el aviso: la balanza en alto y la empuñadura hacia ella (C1).</summary>
        AVISO_PUEBLO,

        /// <summary>El pueblo: golpea la balanza, que suena como una campana (C2), y la baja (C3).</summary>
        PUEBLO,

        /// <summary>El compañero, el aviso: agarra el borde de la capa (P1).</summary>
        AVISO_COMPANERO,

        /// <summary>El compañero: abre la capa y salen volando las placas (P2).</summary>
        COMPANERO,

        /// <summary>Cierra la capa, cayendo las últimas placas (P3).</summary>
        CIERRA_CAPA,

        /// <summary>La sentencia, el aviso: la espada en alto con las dos manos (S1).</summary>
        AVISO_SENTENCIA,

        /// <summary>La sentencia: la baja (S2) y la clava en el suelo (S3).</summary>
        SENTENCIA,

        /// <summary>Saca la espada del suelo (S4).</summary>
        SACA_ESPADA,

        /// <summary>Kael miró su memoria: de rodillas, la balanza en el suelo (K1, K2).</summary>
        ARRODILLADO
    }

    /// <summary>
    /// El Juez Silente, jefe del mundo de la Culpa (<c>Nivel4_Diseno.md</c>, sección 6). «No dicto
    /// sentencia. Solo te muestro lo que él se negó a mirar»: ningún golpe le hace nada (es inmune fuera
    /// de su ventana) y <b>cada ataque es una memoria</b>.
    ///
    /// - Mientras dura un ataque (<see cref="Patrones.PatronMemoria"/>), el Juez proyecta su memoria en
    ///   la pared del fondo (<see cref="pantalla"/>) y su marca brilla en el suelo
    ///   (<see cref="MarcaDeMemoria"/>). Si Kael se pone encima y pulsa W (Interactuar), el ataque se corta y el Juez
    ///   se arrodilla: durante <c>duracionVulnerable</c> (3 s) se le puede golpear.
    /// - <b>La condena</b> (al empezar la segunda fase, al 50 %): clava la espada y las cuatro memorias se
    ///   proyectan a la vez, un instante.
    /// - <b>Con arte</b>, la pared enseña la imagen de cada memoria (<see cref="imagenes"/>, las de sus
    ///   cinemáticas), grande y desvaída; cuando son las cuatro a la vez, pasan una tras otra.
    /// - <b>Una real y una trampa</b>: en la segunda fase brilla también otra marca. La trampa sujeta a
    ///   Kael <see cref="sujecionTrampa"/> s, le quita <see cref="danioTrampa"/> y apaga la marca buena:
    ///   ese ataque ya no se corta. La memoria que proyecta es la verdadera.
    /// - <b>Si no se miró ninguna memoria en el camino</b> (<see cref="SistemaConsecuencias"/>), tiene
    ///   un <see cref="vidaSinMemorias"/> más de vida (28 → 36): «el jefe sabe que Kael evita la verdad».
    /// - <b>Invisible, Kael se esconde de él</b>: sus ataques van a donde lo vio por última vez
    ///   (<see cref="KaelVisto"/>), pero invisible no se activa ninguna marca.
    /// - Al caer, todas las memorias se iluminan (Anexo A).
    ///
    /// Con arte, cada patrón pide su gesto (<see cref="Gesto"/>, <see cref="GESTOS_JUEZ"/>); arrodillado
    /// en su ventana; la condena, con su grito. Las cadenas de la trampa se dibujan sobre Kael mientras
    /// lo sujetan (<see cref="cadena"/>).
    /// </summary>
    public class JuezSilente : BossController
    {
        [Header("Las memorias")]
        [Tooltip("Las cuatro marcas de la plaza, en el orden de las memorias: la orden, el pueblo, el compañero y la carta.")]
        [SerializeField] private MarcaDeMemoria[] marcas = new MarcaDeMemoria[0];

        [Tooltip("La pared del fondo, donde proyecta la memoria de cada ataque. Sin arte, un rectángulo de su color.")]
        [SerializeField] private SpriteRenderer pantalla;

        [Tooltip("Lo que se ve la proyección: grande y desvaída.")]
        [SerializeField, Range(0f, 1f)] private float opacidadProyeccion = 0.4f;

        [Tooltip("Con arte: la imagen de cada memoria (la de su cinemática), en el orden de las marcas. Vacío = la pared es un rectángulo del color de la marca.")]
        [SerializeField] private Sprite[] imagenes = new Sprite[0];

        [Tooltip("Con las cuatro a la vez (la condena, la caída), lo que dura cada imagen en la pared antes de pasar a la siguiente.")]
        [Min(0.05f)] public float segundosPorImagen = 0.375f;

        [Header("La trampa (segunda fase)")]
        [Min(0f)] public float sujecionTrampa = 1f;

        [Min(0)] public int danioTrampa = 1;

        [Tooltip("Con arte: el tramo de cadena (se repite) que se cruza sobre Kael mientras la trampa lo sujeta. Vacío = no se dibuja.")]
        [SerializeField] private Sprite cadena;

        [Tooltip("Lo que mide cada una de las dos cadenas cruzadas sobre Kael.")]
        [Min(0.1f)] public float largoCadenas = 2.2f;

        [Header("Consecuencias")]
        [Tooltip("La vida si no se miró ninguna memoria en el camino (Anexo A: un 30 % más).")]
        [Min(1f)] public float vidaSinMemorias = 1.3f;

        [Tooltip("Lo que dura la condena: las cuatro memorias a la vez.")]
        [Min(0f)] public float duracionCondena = 1.5f;

        private readonly SeguimientoJugador _kael = new SeguimientoJugador(null);
        private int _memoria = -1;
        private int _trampa = -1;
        private bool _cortable;
        private Vector2 _kaelVisto;
        private Vector2 _cabezaVista;
        private bool _vistoAlgunaVez;
        private PlayerController _sujeto;
        private float _soltarEn;
        private GameObject _cadenas;
        private Animator _animador;
        private GESTOS_JUEZ _gesto;
        private bool _gestoPendiente;
        private bool _proyectaTodas;

        /// <summary>Kael miró la memoria de un ataque: (la marca, si era la verdadera).</summary>
        public event Action<MarcaDeMemoria, bool> OnMirada;

        /// <summary>La memoria del ataque en curso (0 a 3), o -1.</summary>
        public int MemoriaProyectada => _memoria;

        /// <summary>La marca trampa encendida (0 a 3), o -1.</summary>
        public int Trampa => _trampa;

        /// <summary>¿Se puede cortar todavía el ataque en curso mirando su marca? (La trampa lo impide.)</summary>
        public bool Cortable => _cortable && _memoria >= 0;

        public IReadOnlyList<MarcaDeMemoria> Marcas => marcas;

        public SpriteRenderer Pantalla => pantalla;

        /// <summary>Las imágenes de las memorias que proyecta (vacío sin arte).</summary>
        public IReadOnlyList<Sprite> Imagenes => imagenes;

        /// <summary>¿Hay una imagen para cada marca?</summary>
        public bool TieneImagenes => imagenes != null && imagenes.Length > 0 && imagenes.Length >= marcas.Length && imagenes.All(i => i != null);

        /// <summary>¿Empezó el combate con la vida de más (no se miró ninguna memoria)?</summary>
        public bool ConVidaExtra { get; private set; }

        /// <summary>Dónde vio a Kael por última vez (su centro); si no lo ha visto, donde está.</summary>
        public Vector2 KaelVisto => _vistoAlgunaVez ? _kaelVisto : CentroJugador;

        /// <summary>Dónde vio la cabeza de Kael por última vez: a dónde vuelan sus placas.</summary>
        public Vector2 CabezaVista => _vistoAlgunaVez ? _cabezaVista : CentroJugador + Vector2.up * 0.65f;

        /// <summary>¿Kael está invisible ahora mismo?</summary>
        public bool KaelInvisible
        {
            get
            {
                var jugador = _kael.Jugador;
                return jugador != null && jugador.Habilidades != null && jugador.Habilidades.EsInvisible;
            }
        }

        /// <summary>El jugador activó la memoria que corresponde al ataque en curso: el Juez se arrodilla.</summary>
        public void ActivarMemoriaCorrecta()
        {
            Apagar();
            AbrirVentanaVulnerable();
        }

        /// <summary>
        /// El jugador activó la memoria trampa: las cadenas lo sujetan y le quitan vida, y la marca buena
        /// se apaga (ese ataque ya no se corta).
        /// </summary>
        public void ActivarMemoriaFalsa()
        {
            Avisar(0.5f);
            _cortable = false;
            if (_memoria >= 0 && _memoria < marcas.Length && marcas[_memoria] != null)
            {
                marcas[_memoria].Apagar();
            }

            if (_trampa >= 0 && _trampa < marcas.Length && marcas[_trampa] != null)
            {
                marcas[_trampa].Apagar();
            }

            _trampa = -1;
            var jugador = _kael.Jugador;
            if (jugador != null && jugador.EstaVivo)
            {
                Encadenar(jugador);
            }
        }

        /// <summary>¿Tiene a Kael sujeto con las cadenas de la trampa?</summary>
        public bool Sujetando => _sujeto != null;

        /// <summary>Las cadenas dibujadas sobre Kael mientras lo sujeta (null sin arte o si no lo sujeta).</summary>
        public GameObject CadenasDibujadas => _cadenas;

        /// <summary>El último gesto pedido.</summary>
        public GESTOS_JUEZ GestoActual => _gesto;

        /// <summary>Pide un gesto a su arte (lo llaman los patrones). Sin arte no hace nada.</summary>
        public void Gesto(GESTOS_JUEZ gesto)
        {
            _gesto = gesto;
            _gestoPendiente = true;
            AplicarGesto();
        }

        /// <summary>
        /// Empieza un ataque con la memoria <paramref name="memoria"/>: la proyecta y enciende su marca;
        /// en la segunda fase, también una trampa. Lo llama el patrón.
        /// </summary>
        public void Proyectar(int memoria)
        {
            Apagar();
            _memoria = memoria;
            _cortable = true;
            Encender(memoria);
            PintarPantalla(memoria, opacidadProyeccion);
            if (Fase >= 2 && marcas.Length > 1)
            {
                var otra = UnityEngine.Random.Range(0, marcas.Length - 1);
                _trampa = otra >= memoria ? otra + 1 : otra;
                Encender(_trampa);
            }
        }

        /// <summary>Acaba el ataque: las marcas se apagan (las fijas, no) y la pared se vacía.</summary>
        public void Apagar()
        {
            foreach (var marca in marcas)
            {
                if (marca != null && !marca.Fija)
                {
                    marca.Apagar();
                }
            }

            _memoria = -1;
            _trampa = -1;
            _cortable = false;
            PintarPantalla(-1, 0f);
        }

        /// <summary>Kael miró una marca (la llama la marca, con Interactuar).</summary>
        public void AlMirar(MarcaDeMemoria marca)
        {
            if (!EnCombate || marca == null || _memoria < 0)
            {
                return;
            }

            var indice = Array.IndexOf(marcas, marca);
            if (indice == _memoria && _cortable)
            {
                OnMirada?.Invoke(marca, true);
                ActivarMemoriaCorrecta();
            }
            else if (indice == _trampa)
            {
                OnMirada?.Invoke(marca, false);
                ActivarMemoriaFalsa();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _animador = GetComponentInChildren<Animator>();
        }

        protected override void Update()
        {
            base.Update();
            AplicarGesto();

            // Las cadenas lo sueltan pasado su tiempo (sin corrutina: cortar un ataque las para todas).
            if (_sujeto != null && Time.time >= _soltarEn)
            {
                Soltar();
            }

            // Las cuatro a la vez: pasan una tras otra por la pared.
            if (_proyectaTodas && pantalla != null && pantalla.enabled && TieneImagenes)
            {
                pantalla.sprite = imagenes[Mathf.FloorToInt(Time.time / segundosPorImagen) % imagenes.Length];
            }

            // Mientras se deja ver, el Juez sabe dónde está Kael.
            var jugador = _kael.Jugador;
            if (jugador != null && jugador.EstaVivo && !KaelInvisible)
            {
                var cuerpo = jugador.GetComponent<Collider2D>();
                _kaelVisto = cuerpo != null ? (Vector2)cuerpo.bounds.center : (Vector2)jugador.transform.position;
                _cabezaVista = cuerpo != null ? new Vector2(cuerpo.bounds.center.x, cuerpo.bounds.max.y - 0.25f) : _kaelVisto + Vector2.up * 0.65f;
                _vistoAlgunaVez = true;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnStateChanged += AlCambiarEstadoDelJuez;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnStateChanged -= AlCambiarEstadoDelJuez;
            Soltar();
            _kael.Soltar();
        }

        protected override void AlActivarse()
        {
            // Si no se miró ninguna memoria en el camino, el Juez llega más fuerte.
            var consecuencias = SistemaConsecuencias.Instancia;
            ConVidaExtra = consecuencias != null && consecuencias.NingunaRecordada;
            var vidaBase = Datos.vidaMaxima;
            if (ConVidaExtra)
            {
                Vida.Inicializar(Mathf.RoundToInt(vidaBase * vidaSinMemorias), Datos.invulnerabilidad);
            }

            Vida.VidaBase = ConVidaExtra ? vidaBase : 0;
            Vida.NotificarVida();
        }

        protected override void AlCambiarDeFase(int fase)
        {
            if (fase >= 2)
            {
                // La condena: clava la espada y las cuatro memorias se proyectan a la vez.
                Rugir();
                Avisar(duracionCondena);
                StartCoroutine(Condena());
            }
        }

        protected override float EsperaAlEmpezarFase(int fase, FaseJefe datosFase) =>
            fase >= 2 ? duracionCondena + 0.3f : base.EsperaAlEmpezarFase(fase, datosFase);

        protected override void AlSerDerrotado()
        {
            // Todas las memorias se iluminan (Anexo A).
            foreach (var marca in marcas)
            {
                if (marca != null)
                {
                    marca.Fijar();
                }
            }

            _memoria = -1;
            _trampa = -1;
            PintarPantalla(-2, opacidadProyeccion);
        }

        protected override void AlReiniciarse()
        {
            ConVidaExtra = false;
            Vida.VidaBase = 0;
            Apagar();
            Soltar();
            Gesto(GESTOS_JUEZ.REPOSO);
        }

        // Arrodillado (la ventana) o en otra fase: el ataque se cortó y sus marcas se apagan. Al caer, no:
        // entonces se encienden todas.
        private void AlCambiarEstadoDelJuez(ESTADOS_JEFE anterior, ESTADOS_JEFE nuevo)
        {
            if (anterior != nuevo && nuevo != ESTADOS_JEFE.DEFEATED)
            {
                Apagar();
            }

            // De rodillas mientras dura su ventana; al acabar, se levanta.
            if (nuevo == ESTADOS_JEFE.VULNERABLE)
            {
                Gesto(GESTOS_JUEZ.ARRODILLADO);
            }
            else if (anterior == ESTADOS_JEFE.VULNERABLE && nuevo != ESTADOS_JEFE.DEFEATED)
            {
                Gesto(GESTOS_JUEZ.REPOSO);
            }
        }

        private void AplicarGesto()
        {
            if (!_gestoPendiente || !TieneArte || _animador == null || !_animador.isInitialized || Derrotado)
            {
                return;
            }

            _gestoPendiente = false;
            PedirGesto(_animador, _gesto);
        }

        private static readonly string[] Gestos = Enum.GetNames(typeof(GESTOS_JUEZ));

        /// <summary>
        /// Lleva el Animator al estado del gesto con su disparador (los demás, olvidados). Si ya está en
        /// él, el clip vuelve a empezar: un disparador sin usar saltaría más tarde.
        /// </summary>
        internal static void PedirGesto(Animator animador, GESTOS_JUEZ gesto)
        {
            var nombre = gesto.ToString();
            if (animador.GetCurrentAnimatorStateInfo(0).IsName(nombre))
            {
                animador.Play(nombre, 0, 0f);
                return;
            }

            foreach (var otro in Gestos)
            {
                animador.ResetTrigger(otro);
            }

            animador.SetTrigger(nombre);
        }

        private IEnumerator Condena()
        {
            // Un fotograma después: el cambio de fase apaga las marcas del ataque que cortó.
            yield return null;
            Apagar();
            foreach (var marca in marcas)
            {
                if (marca != null)
                {
                    marca.Encender();
                }
            }

            PintarPantalla(-2, opacidadProyeccion);
            yield return new WaitForSeconds(duracionCondena);
            Apagar();
        }

        // Las cadenas de la trampa: le quitan vida y lo dejan quieto un momento (sin retroceso).
        private void Encadenar(PlayerController jugador)
        {
            Soltar();
            jugador.Vida.PerderVida(danioTrampa);
            if (jugador.Motor != null)
            {
                jugador.Motor.AgregarModificador(this, new ModificadorMovimiento(0f, 1f, 0f, 1f, false));
            }

            _sujeto = jugador;
            _soltarEn = Time.time + sujecionTrampa;
            DibujarCadenas(jugador);
        }

        private void Soltar()
        {
            if (_sujeto != null && _sujeto.Motor != null)
            {
                _sujeto.Motor.QuitarModificador(this);
            }

            _sujeto = null;
            if (_cadenas != null)
            {
                Destroy(_cadenas);
                _cadenas = null;
            }
        }

        // Dos cadenas cruzadas sobre Kael, que van con él mientras lo sujetan.
        private void DibujarCadenas(PlayerController jugador)
        {
            if (cadena == null)
            {
                return;
            }

            var cuerpo = jugador.GetComponent<Collider2D>();
            _cadenas = new GameObject("CadenasDeLaTrampa");
            _cadenas.transform.SetParent(jugador.transform, false);
            _cadenas.transform.position = cuerpo != null ? cuerpo.bounds.center : jugador.transform.position + Vector3.up * 0.9f;
            foreach (var angulo in new[] { 35f, -35f })
            {
                var objeto = new GameObject("Cadena");
                objeto.transform.SetParent(_cadenas.transform, false);
                objeto.transform.localRotation = Quaternion.Euler(0f, 0f, angulo);
                var dibujo = objeto.AddComponent<SpriteRenderer>();
                dibujo.sprite = cadena;
                dibujo.drawMode = SpriteDrawMode.Tiled;
                dibujo.size = new Vector2(largoCadenas, cadena.bounds.size.y);
                dibujo.sortingOrder = 12;
            }
        }

        private void Encender(int indice)
        {
            if (indice >= 0 && indice < marcas.Length && marcas[indice] != null)
            {
                marcas[indice].Encender();
            }
        }

        // La pared del fondo: la memoria proyectada (con el color de su marca, y su imagen si la hay), todas a
        // la vez (-2: con imágenes, pasan una tras otra) o nada (-1).
        private void PintarPantalla(int memoria, float opacidad)
        {
            _proyectaTodas = false;
            if (pantalla == null)
            {
                return;
            }

            if (memoria == -1 || opacidad <= 0f)
            {
                pantalla.enabled = false;
                return;
            }

            var color = memoria >= 0 && memoria < marcas.Length && marcas[memoria] != null ? marcas[memoria].Color : new Color(0.88f, 0.69f, 0.38f, 1f);
            if (TieneImagenes)
            {
                _proyectaTodas = memoria < 0;
                pantalla.sprite = imagenes[Mathf.Max(memoria, 0)];
                color = Color.white;
            }

            pantalla.enabled = true;
            pantalla.color = new Color(color.r, color.g, color.b, opacidad);
        }
    }
}
