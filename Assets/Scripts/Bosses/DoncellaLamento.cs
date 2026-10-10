using System;
using System.Collections.Generic;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Levels.Mecanicas;
using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Doncella del Lamento, jefe del mundo de la Tristeza (<c>Docs/Diseno/Nivel2_Diseno.md</c>,
    /// sección 6). Todavía sin arte: una cápsula azul.
    ///
    /// - <b>Flota:</b> espera en el hielo y, al empezar el combate, sube y va de lado a lado sobre
    ///   la arena. Es un cuerpo cinemático: ni cae ni choca, y los muros de la arena son sus límites.
    /// - <b>Solo se la hiere mientras llora</b> (<c>inmuneFueraDeVulnerable</c> en sus datos): fuera
    ///   de esa ventana los golpes suenan a hielo y destella en blanco, sin perder vida. Tocarla no
    ///   hace daño: lo hacen sus olas y sus púas.
    /// - <b>Fase 1:</b> olas a ras del hielo e inundación de una franja del suelo. Para llorar baja
    ///   al hielo seco, cerca de Kael: se la alcanza a pie.
    /// - <b>Fase 2</b> (50 % de vida): grita, y el agua que deja se congela (púas en lo inundado y
    ///   una capa de hielo arriba). Llora flotando bajo sobre lo que acaba de congelar o, si no hay,
    ///   metida en lo que acaba de inundar: hay que subirse a su hielo o meterse en su agua.
    /// - <b>Muerte:</b> el agua se va y el hielo se deshace. La cinemática llega con la narrativa.
    ///
    /// Los ataques son datos (<c>Assets/Data/Jefes/Patrones</c>): <see cref="PatronOla"/>,
    /// <see cref="PatronInundacion"/>, <see cref="PatronCongelar"/> y <see cref="PatronLlanto"/>.
    /// Aquí está lo que es suyo: volar, saber dónde llorar y llevar las franjas de la arena.
    /// </summary>
    public class DoncellaLamento : BossController
    {
        private const float Rugido = 0.9f;
        private const float DestelloDeHielo = 0.12f;
        private const float Llegada = 0.05f;

        // Lo que guarda a cada lado al colocarse dentro de una franja, junto a una pared o al agua.
        private const float Holgura = 0.5f;

        // Las lágrimas (#00D4FF) y el hielo (#E8EEF4) de la paleta del mundo.
        private static readonly Color Lagrima = new Color(0f, 0.83f, 1f, 1f);
        private static readonly Color Hielo = new Color(0.91f, 0.93f, 0.96f, 1f);

        [Header("Flotar")]
        [Tooltip("Altura de sus pies sobre el hielo mientras flota.")]
        [SerializeField, Min(0f)] private float alturaDeVuelo = 3f;

        [Tooltip("Velocidad al subir, al bajar y al colocarse (u/s). De lado a lado va a la de sus datos.")]
        [SerializeField, Min(0.1f)] private float velocidadDeVuelo = 5f;

        [Tooltip("Distancia a las paredes de la arena que guarda al ir de lado a lado.")]
        [SerializeField, Min(0f)] private float margenPared = 2.5f;

        [Header("Llanto")]
        [Tooltip("En el hielo seco baja a esta distancia de Kael (entre centros), por el lado en el que ella estaba: a tiro de espada.")]
        [SerializeField, Min(0f)] private float distanciaAKael = 2f;

        [Tooltip("Sobre una franja inundada, lo que se hunden sus pies bajo la superficie: desde dentro del agua se llega saltando.")]
        [SerializeField, Min(0f)] private float hundimientoAlLlorar = 1f;

        [Tooltip("Sobre una franja congelada, lo que quedan sus pies por encima de la capa: de pie en ella, la espada llega.")]
        [SerializeField, Min(0f)] private float alturaSobreLaCapa = 0.2f;

        [Header("La arena")]
        [Tooltip("Las franjas del suelo que inunda y congela, de izquierda a derecha. Vacío = las que cuelgan de su arena.")]
        [SerializeField] private FranjaInundable[] franjas = new FranjaInundable[0];

        private Rigidbody2D _cuerpo;
        private SpriteRenderer _sprite;
        private float _suelo;
        private int _vaiven = 1;
        private float _finQuieta = float.NegativeInfinity;
        private float _finDestello = float.NegativeInfinity;
        private bool _hayOrden;
        private Vector2 _destino;
        private float _velocidadOrden;
        private PatronAtaque _patronDeLaOrden;
        private bool _llorando;
        private float _inicioLlanto;
        private Vector2 _lugarDeLlanto;

        /// <summary>Empieza (true) o deja (false) de llorar. Para el arte y el sonido.</summary>
        public event Action<bool> OnLlanto;

        /// <summary>Un golpe no le hizo nada: no estaba llorando. Para el sonido a hielo.</summary>
        public event Action OnGolpeEnElHielo;

        public bool Llorando => _llorando;

        /// <summary>La altura del hielo de la arena: donde espera antes del combate.</summary>
        public float Suelo => _suelo;

        public float AlturaDeVuelo => alturaDeVuelo;

        public float DistanciaAKael => distanciaAKael;

        public float HundimientoAlLlorar => hundimientoAlLlorar;

        public float AlturaSobreLaCapa => alturaSobreLaCapa;

        /// <summary>Donde tiene los pies.</summary>
        public Vector2 Pies => _cuerpo != null ? _cuerpo.position : (Vector2)transform.position;

        /// <summary>¿Ha llegado adonde la mandó su patrón? (Sin orden, sí.)</summary>
        public bool HaLlegado => !_hayOrden || Vector2.Distance(Pies, _destino) <= Llegada;

        public IReadOnlyList<FranjaInundable> Franjas => franjas;

        // --- Ciclo de vida ---

        protected override void Awake()
        {
            base.Awake();
            _cuerpo = GetComponent<Rigidbody2D>();

            // Flota: ni cae ni choca. La mueve este script.
            _cuerpo.bodyType = RigidbodyType2D.Kinematic;
            _cuerpo.gravityScale = 0f;
            _cuerpo.linearVelocity = Vector2.zero;

            _sprite = GetComponentInChildren<SpriteRenderer>();
            _suelo = transform.position.y;

            if ((franjas == null || franjas.Length == 0) && transform.parent != null)
            {
                franjas = transform.parent.GetComponentsInChildren<FranjaInundable>(true);
            }

            FijarFranjas(franjas);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Vida.OnGolpeBloqueado += AlBloquearGolpe;
            OnStateChanged += AlCambiarDeEstado;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Vida.OnGolpeBloqueado -= AlBloquearGolpe;
            OnStateChanged -= AlCambiarDeEstado;
        }

        protected override void Update()
        {
            base.Update();
            if (_sprite != null && !Derrotado && Time.time < _finDestello)
            {
                _sprite.color = Hielo;
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!EnCombate)
            {
                return;
            }

            // La orden de vuelo es del patrón que la dio: al acabar él, vuelve a flotar a su aire.
            if (_hayOrden && PatronActual != _patronDeLaOrden)
            {
                _hayOrden = false;
            }

            // Por si la ventana no llegara a abrirse: no se queda arrodillada para siempre.
            if (_llorando && !EsVulnerable && Time.time > _inicioLlanto + 1f)
            {
                DejarDeLlorar();
            }

            var pies = _cuerpo.position;
            var paso = Time.fixedDeltaTime;
            Vector2 nueva;
            if (_llorando)
            {
                nueva = Vector2.MoveTowards(pies, _lugarDeLlanto, velocidadDeVuelo * paso);
            }
            else if (_hayOrden)
            {
                nueva = Vector2.MoveTowards(pies, _destino, _velocidadOrden * paso);
            }
            else
            {
                // Mientras ataca, ruge o no hay a quién mirar, quieta en el aire; si no, de lado a lado.
                var x = pies.x;
                if (PatronActual == null && Time.time >= _finQuieta && JugadorVivo)
                {
                    var limites = Limites;
                    var izquierda = limites.x + margenPared;
                    var derecha = limites.y - margenPared;
                    if (izquierda >= derecha)
                    {
                        // Una arena más estrecha que sus márgenes: se queda en el centro.
                        izquierda = derecha = (limites.x + limites.y) * 0.5f;
                    }

                    if (pies.x >= derecha)
                    {
                        _vaiven = -1;
                    }
                    else if (pies.x <= izquierda)
                    {
                        _vaiven = 1;
                    }

                    x = Mathf.MoveTowards(pies.x, _vaiven > 0 ? derecha : izquierda, VelocidadActual * paso);
                }

                nueva = new Vector2(x, Mathf.MoveTowards(pies.y, _suelo + alturaDeVuelo, velocidadDeVuelo * paso));
            }

            if (JugadorVivo)
            {
                MirarAlJugador();
            }

            _cuerpo.MovePosition(nueva);
        }

        // --- Para sus patrones ---

        /// <summary>
        /// La manda a un punto (el de sus pies) dentro de la arena, a <paramref name="velocidad"/>
        /// u/s (por defecto, la de vuelo). La orden dura hasta que acaba el patrón que la dio.
        /// </summary>
        public void FlotarA(Vector2 pies, float velocidad = -1f)
        {
            _hayOrden = true;
            _destino = Recortar(pies);
            _velocidadOrden = velocidad > 0f ? velocidad : velocidadDeVuelo;
            _patronDeLaOrden = PatronActual;
        }

        /// <summary>
        /// Se queda donde está, llorando, y abre la ventana vulnerable <paramref name="duracion"/>
        /// segundos (por defecto, los de sus datos). Al cerrarse la ventana vuelve a flotar.
        /// </summary>
        public void Llorar(float duracion = -1f)
        {
            if (!EnCombate || EsVulnerable || _llorando)
            {
                return;
            }

            _llorando = true;
            _inicioLlanto = Time.time;
            _lugarDeLlanto = Pies;
            _hayOrden = false;
            AbrirVentanaVulnerable(duracion);
            OnLlanto?.Invoke(true);
        }

        /// <summary>
        /// Dónde le toca llorar ahora (el punto de sus pies). En la fase 2, sobre la franja que acaba
        /// de congelar (algo por encima de la capa) o, si no hay, sobre la que acaba de inundar
        /// (metida en el agua). Si no, en el hielo seco, cerca de Kael.
        /// </summary>
        public Vector2 LugarDeLlanto()
        {
            var kael = CentroJugador.x;
            if (Fase >= 2)
            {
                var helada = MasCercana(kael, f => f.Congelada);
                if (helada != null)
                {
                    return Recortar(new Vector2(DentroDe(helada, kael), helada.Techo + alturaSobreLaCapa));
                }

                var inundada = MasCercana(kael, f => f.Inundada);
                if (inundada != null)
                {
                    return Recortar(new Vector2(DentroDe(inundada, kael), inundada.Superficie - hundimientoAlLlorar));
                }
            }

            // Al lado de Kael, por donde ella está: no le cae encima.
            var lado = Pies.x >= kael ? 1 : -1;
            var limites = Limites;
            var minimo = limites.x + MedioAncho + Holgura;
            var maximo = limites.y - MedioAncho - Holgura;
            var x = kael + lado * distanciaAKael;
            if (x < minimo || x > maximo)
            {
                x = kael - lado * distanciaAKael;
            }

            return new Vector2(EnSeco(Mathf.Clamp(x, minimo, maximo), minimo, maximo), _suelo);
        }

        /// <summary>
        /// Las franjas que le toca inundar: la de Kael (o la seca más cercana a él) y, si se piden
        /// más, sus vecinas secas, la más cercana a Kael primero. El agua se extiende, no salta.
        /// </summary>
        public List<FranjaInundable> FranjasParaInundar(int cuantas)
        {
            var elegidas = new List<FranjaInundable>();
            var kael = CentroJugador.x;
            var primera = MasCercana(kael, f => f.Estado == ESTADOS_FRANJA.DRY);
            if (primera == null || cuantas <= 0)
            {
                return elegidas;
            }

            elegidas.Add(primera);
            while (elegidas.Count < cuantas)
            {
                var vecina = MasCercana(kael, f => f.Estado == ESTADOS_FRANJA.DRY && !elegidas.Contains(f) && EsVecina(f, elegidas));
                if (vecina == null)
                {
                    break;
                }

                elegidas.Add(vecina);
            }

            return elegidas;
        }

        /// <summary>Las franjas llenas de agua: lo que se puede congelar.</summary>
        public List<FranjaInundable> FranjasInundadas()
        {
            var inundadas = new List<FranjaInundable>();
            foreach (var franja in franjas)
            {
                if (franja != null && franja.Inundada)
                {
                    inundadas.Add(franja);
                }
            }

            return inundadas;
        }

        /// <summary>Cambia las franjas de su arena (herramientas y pruebas). Las ordena de izquierda a derecha.</summary>
        public void FijarFranjas(params FranjaInundable[] nuevas)
        {
            var lista = new List<FranjaInundable>();
            if (nuevas != null)
            {
                foreach (var franja in nuevas)
                {
                    if (franja != null && !lista.Contains(franja))
                    {
                        lista.Add(franja);
                    }
                }
            }

            lista.Sort((a, b) => a.CentroX.CompareTo(b.CentroX));
            franjas = lista.ToArray();
        }

        // --- Template Method ---

        protected override void AlCambiarDeFase(int fase)
        {
            _finQuieta = Time.time + Espera(fase);
            if (fase >= 2)
            {
                Avisar(Rugido);
                Rugir();
            }
        }

        // Fase 1: lo que tarda en subir desde el hielo. Después, el grito.
        protected override float EsperaAlEmpezarFase(int fase, FaseJefe datosFase) => Espera(fase);

        protected override void AlReiniciarse()
        {
            DejarDeLlorar();
            _hayOrden = false;
            _vaiven = 1;
            _finQuieta = float.NegativeInfinity;
            _finDestello = float.NegativeInfinity;
            SecarFranjas();
        }

        // El agua se va y el hielo se deshace.
        protected override void AlSerDerrotado()
        {
            DejarDeLlorar();
            SecarFranjas();
        }

        // Llora: late en el cian de sus lágrimas. Es la señal de que ahora sí se la puede herir.
        protected override Color ColorVentana(Color colorBase) =>
            Color.Lerp(colorBase, Lagrima, 0.55f + 0.25f * Mathf.Sin(Time.time * 18f));

        // --- Internos ---

        private float Espera(int fase) => fase <= 1 ? alturaDeVuelo / velocidadDeVuelo + 0.3f : Rugido;

        private void AlBloquearGolpe(Golpe golpe)
        {
            _finDestello = Time.time + DestelloDeHielo;
            OnGolpeEnElHielo?.Invoke();
        }

        private void AlCambiarDeEstado(ESTADOS_JEFE anterior, ESTADOS_JEFE nuevo)
        {
            if (anterior == ESTADOS_JEFE.VULNERABLE)
            {
                DejarDeLlorar();
            }
        }

        private void DejarDeLlorar()
        {
            if (!_llorando)
            {
                return;
            }

            _llorando = false;
            OnLlanto?.Invoke(false);
        }

        private void SecarFranjas()
        {
            foreach (var franja in franjas)
            {
                if (franja != null)
                {
                    franja.Secar();
                }
            }
        }

        // Dentro de la arena y nunca por debajo del hielo.
        private Vector2 Recortar(Vector2 pies)
        {
            var limites = Limites;
            var minimo = limites.x + MedioAncho;
            var maximo = limites.y - MedioAncho;
            return new Vector2(minimo <= maximo ? Mathf.Clamp(pies.x, minimo, maximo) : (limites.x + limites.y) * 0.5f, Mathf.Max(_suelo, pies.y));
        }

        // La x más cercana a Kael sin salirse de la franja.
        private float DentroDe(FranjaInundable franja, float x)
        {
            var minimo = franja.Desde + MedioAncho + Holgura;
            var maximo = franja.Hasta - MedioAncho - Holgura;
            return minimo <= maximo ? Mathf.Clamp(x, minimo, maximo) : franja.CentroX;
        }

        // La x más cercana a la pedida que no cae en una franja con agua: a pie no se llega a lo mojado.
        private float EnSeco(float x, float minimo, float maximo)
        {
            var mejor = x;
            var distancia = float.MaxValue;
            var margen = MedioAncho + Holgura;

            // Los huecos entre las franjas mojadas, de izquierda a derecha (están ordenadas).
            var desde = minimo;
            foreach (var franja in franjas)
            {
                if (franja == null || !Mojada(franja))
                {
                    continue;
                }

                Probar(desde, franja.Desde - margen);
                desde = Mathf.Max(desde, franja.Hasta + margen);
            }

            Probar(desde, maximo);
            return mejor;

            void Probar(float a, float b)
            {
                if (a > b)
                {
                    return;
                }

                var candidata = Mathf.Clamp(x, a, b);
                if (Mathf.Abs(candidata - x) < distancia)
                {
                    distancia = Mathf.Abs(candidata - x);
                    mejor = candidata;
                }
            }
        }

        private static bool Mojada(FranjaInundable franja) =>
            franja.TieneAgua || franja.Estado == ESTADOS_FRANJA.WARNING || franja.Estado == ESTADOS_FRANJA.FLOODED || franja.Estado == ESTADOS_FRANJA.FREEZING;

        private FranjaInundable MasCercana(float x, Predicate<FranjaInundable> vale)
        {
            FranjaInundable mejor = null;
            var distancia = float.MaxValue;
            foreach (var franja in franjas)
            {
                if (franja == null || !vale(franja))
                {
                    continue;
                }

                var d = franja.Contiene(x) ? 0f : Mathf.Min(Mathf.Abs(x - franja.Desde), Mathf.Abs(x - franja.Hasta));
                if (d < distancia)
                {
                    distancia = d;
                    mejor = franja;
                }
            }

            return mejor;
        }

        private static bool EsVecina(FranjaInundable franja, List<FranjaInundable> de)
        {
            foreach (var otra in de)
            {
                if (Mathf.Abs(franja.Desde - otra.Hasta) < 0.05f || Mathf.Abs(franja.Hasta - otra.Desde) < 0.05f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
