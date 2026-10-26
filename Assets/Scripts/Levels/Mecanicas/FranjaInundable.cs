using System;
using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Una franja del suelo de la arena de la Doncella del Lamento: se inunda y, en su segunda
    /// fase, el agua se congela. El objeto está en el centro de la franja, a la altura del suelo.
    ///
    /// - <b>Aviso</b> (<see cref="Inundar"/>): sale agua por las grietas. Todavía no moja.
    /// - <b>Inundada:</b> el agua sube hasta <see cref="alturaAgua"/> y se queda. No hace daño: es
    ///   agua, la misma del recorrido (lenta, con el salto alto y gastando aire). Después baja.
    /// - <b>Se congela</b> (<see cref="Congelar"/>): la superficie se vuelve blanca; es el aviso.
    /// - <b>Congelada:</b> brotan púas de hielo en lo que estaba inundado (lo único que hace daño)
    ///   y arriba queda la capa de hielo, que se pisa y resbala. Después se deshace.
    ///
    /// Vive por su cuenta: quien la manda (un patrón de la Doncella) solo la pone en marcha, así
    /// que el agua sigue ahí cuando el jefe corta su ataque para llorar. <see cref="Secar"/> la
    /// devuelve al principio (el combate se reinicia o termina).
    ///
    /// Con su arte (<see cref="ArteDeFranja"/>) la dibuja <see cref="DibujoDeFranja"/>. Sin él, el
    /// agua, el aviso, las púas y la capa son rectángulos de color, como las zonas de daño de los
    /// jefes.
    /// </summary>
    [DisallowMultipleComponent]
    public class FranjaInundable : MonoBehaviour
    {
        /// <summary>El agua va por delante de Kael (10), como la del recorrido.</summary>
        public const int OrdenAgua = 20;

        private const int OrdenAviso = 8;
        private const int OrdenCapa = 2;
        private const float AltoResbala = 0.5f;

        // Lo que se encoge la capa para saber si Kael sigue metido en ella: tocarla no es estar dentro.
        private const float HolguraCapa = 0.06f;

        [Header("Medidas, desde el suelo")]
        [Min(0.5f)] public float ancho = 8f;

        [Tooltip("Hasta dónde sube el agua.")]
        [Min(0.5f)] public float alturaAgua = 3.4f;

        [Tooltip("Altura de lo que se pisa cuando el agua se congela.")]
        [Min(0.5f)] public float alturaCapa = 3.5f;

        [Min(0.1f)] public float grosorCapa = 0.5f;

        [Header("Tiempos")]
        [Tooltip("Segundos que tarda el agua en subir.")]
        [Min(0f)] public float tiempoSubida = 0.5f;

        [Tooltip("Segundos que tarda en irse.")]
        [Min(0f)] public float tiempoBajada = 0.6f;

        [Header("Aspecto provisional (sin arte)")]
        public Color colorAgua = new Color(0.29f, 0.5f, 0.65f, 0.45f);

        public Color colorHielo = new Color(0.91f, 0.93f, 0.96f, 1f);

        [Header("Arte")]
        [Tooltip("Las imágenes de cada momento. Vacío o incompleto = los rectángulos provisionales.")]
        [SerializeField] private ArteDeFranja arte;

        private readonly List<Collider2D> _atrapados = new List<Collider2D>();
        private GameObject _objetoAgua;
        private BoxCollider2D _agua;
        private SpriteRenderer _visualAgua;
        private SpriteRenderer _aviso;
        private GameObject _objetoCapa;
        private SpriteRenderer _visualCapa;
        private BoxCollider2D _capa;
        private ZonaDeDanio _puas;
        private DibujoDeFranja _dibujo;
        private float _inicioEstado;
        private float _finEstado;
        private float _duracionAgua;
        private float _duracionPuas;
        private float _duracionCapa;
        private int _danioPuas;
        private bool _construida;

        /// <summary>Cambió de estado (para audio, VFX y el arte cuando llegue).</summary>
        public event Action<ESTADOS_FRANJA> OnEstadoCambiado;

        public ESTADOS_FRANJA Estado { get; private set; }

        public float Desde => transform.position.x - ancho * 0.5f;

        public float Hasta => transform.position.x + ancho * 0.5f;

        public float CentroX => transform.position.x;

        /// <summary>La altura del suelo de la franja.</summary>
        public float Suelo => transform.position.y;

        /// <summary>Hasta dónde llega el agua cuando está llena.</summary>
        public float Superficie => Suelo + alturaAgua;

        /// <summary>La altura de lo que se pisa cuando está congelada.</summary>
        public float Techo => Suelo + alturaCapa;

        /// <summary>Lo que ha subido el agua ahora mismo, desde el suelo.</summary>
        public float Nivel { get; private set; }

        /// <summary>¿Hay agua que moje? (Mientras sube, llena, poniéndose blanca o bajando.)</summary>
        public bool TieneAgua => Nivel > 0.01f;

        /// <summary>¿Está llena de agua y sin empezar a congelarse? Es lo que se puede congelar.</summary>
        public bool Inundada => Estado == ESTADOS_FRANJA.FLOODED;

        /// <summary>¿Está la capa de hielo, que se pisa?</summary>
        public bool Congelada => Estado == ESTADOS_FRANJA.FROZEN;

        /// <summary>¿Están las púas haciendo daño ahora mismo?</summary>
        public bool PuasActivas => _puas != null;

        /// <summary>Segundos que le quedan al estado actual (0 en seco y mientras baja el agua).</summary>
        public float TiempoRestante =>
            Estado == ESTADOS_FRANJA.DRY || Estado == ESTADOS_FRANJA.DRAINING ? 0f : Mathf.Max(0f, _finEstado - Time.time);

        /// <summary>Segundos que lleva en el estado actual.</summary>
        public float TiempoEnEstado => Time.time - _inicioEstado;

        /// <summary>Lo avanzado que va el estado actual, de 0 a 1 (1 si no tiene duración).</summary>
        public float AvanceDelEstado =>
            _finEstado > _inicioEstado ? Mathf.Clamp01((Time.time - _inicioEstado) / (_finEstado - _inicioEstado)) : 1f;

        /// <summary>Segundos que las púas hacen daño al congelarse (lo que se pidió en <see cref="Congelar"/>).</summary>
        public float DuracionPuas => _duracionPuas;

        /// <summary>Las imágenes con las que se dibuja. Se fija antes de inundarla por primera vez.</summary>
        public ArteDeFranja Arte
        {
            get => arte;
            set => arte = value;
        }

        /// <summary>¿Se dibuja con su arte? (Lo sabe después de construirse: al inundarla por primera vez.)</summary>
        public bool ConArte => _dibujo != null;

        /// <summary>El dibujo con arte, o null.</summary>
        public DibujoDeFranja Dibujo => _dibujo;

        public bool Contiene(float x) => x >= Desde && x <= Hasta;

        // La capa de hielo, algo encogida: lo que la toca por fuera no está dentro.
        private Bounds InteriorDeLaCapa =>
            new Bounds(new Vector3(CentroX, Techo - grosorCapa * 0.5f, 0f),
                new Vector3(ancho - 2f * HolguraCapa, grosorCapa - 2f * HolguraCapa, 1f));

        /// <summary>
        /// Crea una franja por código, de <paramref name="desde"/> a <paramref name="hasta"/> y con
        /// el suelo a la altura <paramref name="suelo"/>. Las medidas del agua y de la capa se
        /// pueden cambiar después, antes de inundarla.
        /// </summary>
        public static FranjaInundable Crear(string nombre, float desde, float hasta, float suelo, Transform padre = null)
        {
            var objeto = new GameObject(nombre);
            if (padre != null)
            {
                objeto.transform.SetParent(padre, false);
            }

            objeto.transform.position = new Vector3((desde + hasta) * 0.5f, suelo, 0f);
            var franja = objeto.AddComponent<FranjaInundable>();
            franja.ancho = Mathf.Abs(hasta - desde);
            return franja;
        }

        private void OnDisable() => Secar();

        private void OnDestroy()
        {
            if (_puas != null)
            {
                Destroy(_puas.gameObject);
            }
        }

        private void Update()
        {
            switch (Estado)
            {
                case ESTADOS_FRANJA.WARNING:
                    if (Time.time >= _finEstado)
                    {
                        Entrar(ESTADOS_FRANJA.FLOODED, _duracionAgua);
                    }

                    break;

                case ESTADOS_FRANJA.FLOODED:
                    MoverAgua(alturaAgua, tiempoSubida);
                    if (Time.time >= _finEstado)
                    {
                        Entrar(ESTADOS_FRANJA.DRAINING, 0f);
                    }

                    break;

                case ESTADOS_FRANJA.DRAINING:
                    MoverAgua(0f, tiempoBajada);
                    if (!TieneAgua)
                    {
                        Entrar(ESTADOS_FRANJA.DRY, 0f);
                    }

                    break;

                case ESTADOS_FRANJA.FREEZING:
                    MoverAgua(alturaAgua, tiempoSubida);
                    if (Time.time >= _finEstado)
                    {
                        Entrar(ESTADOS_FRANJA.FROZEN, _duracionCapa);
                    }

                    break;

                case ESTADOS_FRANJA.FROZEN:
                    if (Time.time >= _finEstado)
                    {
                        Entrar(ESTADOS_FRANJA.DRY, 0f);
                    }

                    break;
            }

            Pintar();
        }

        // Quien estuviera metido en la capa al formarse la atraviesa hasta que sale de ella: nadie
        // se queda encajado en el hielo.
        private void FixedUpdate()
        {
            if (_atrapados.Count == 0 || _capa == null)
            {
                return;
            }

            var dentro = InteriorDeLaCapa;
            for (var i = _atrapados.Count - 1; i >= 0; i--)
            {
                var atrapado = _atrapados[i];
                if (atrapado != null && atrapado.bounds.Intersects(dentro))
                {
                    continue;
                }

                if (atrapado != null)
                {
                    Physics2D.IgnoreCollision(_capa, atrapado, false);
                }

                _atrapados.RemoveAt(i);
            }
        }

        // --- API ---

        /// <summary>
        /// Avisa durante <paramref name="aviso"/> segundos y después el agua sube y se queda
        /// <paramref name="duracion"/> segundos. Solo desde seco: devuelve false si ya estaba en marcha.
        /// </summary>
        public bool Inundar(float aviso, float duracion)
        {
            if (Estado != ESTADOS_FRANJA.DRY || !isActiveAndEnabled)
            {
                return false;
            }

            _duracionAgua = Mathf.Max(0f, duracion);
            if (aviso > 0f)
            {
                Entrar(ESTADOS_FRANJA.WARNING, aviso);
            }
            else
            {
                Entrar(ESTADOS_FRANJA.FLOODED, _duracionAgua);
            }

            return true;
        }

        /// <summary>
        /// Congela el agua: la superficie se pone blanca <paramref name="aviso"/> segundos, y
        /// después brotan las púas (<paramref name="danio"/> a quien siga dentro, durante
        /// <paramref name="duracionPuas"/> segundos) y queda la capa de hielo
        /// <paramref name="duracionCapa"/> segundos. Solo si está inundada: devuelve false si no.
        /// </summary>
        public bool Congelar(float aviso, float duracionPuas, float duracionCapa, int danio)
        {
            if (Estado != ESTADOS_FRANJA.FLOODED)
            {
                return false;
            }

            _duracionPuas = Mathf.Max(0.05f, duracionPuas);
            _duracionCapa = Mathf.Max(0f, duracionCapa);
            _danioPuas = Mathf.Max(0, danio);
            Entrar(ESTADOS_FRANJA.FREEZING, Mathf.Max(0f, aviso));
            return true;
        }

        /// <summary>La deja seca en el acto: sin agua, sin púas y sin capa.</summary>
        public void Secar()
        {
            if (_puas != null)
            {
                Destroy(_puas.gameObject);
                _puas = null;
            }

            if (Estado != ESTADOS_FRANJA.DRY || TieneAgua)
            {
                Entrar(ESTADOS_FRANJA.DRY, 0f);
            }
        }

        // --- Estados ---

        private void Entrar(ESTADOS_FRANJA estado, float duracion)
        {
            Construir();
            var anterior = Estado;
            Estado = estado;
            _inicioEstado = Time.time;
            _finEstado = Time.time + duracion;

            switch (estado)
            {
                case ESTADOS_FRANJA.DRY:
                    FijarNivel(0f);
                    FijarCapa(false);
                    break;

                case ESTADOS_FRANJA.FROZEN:
                    FijarNivel(0f);
                    BrotarPuas();
                    FijarCapa(true);
                    break;
            }

            Pintar();
            if (anterior != estado)
            {
                OnEstadoCambiado?.Invoke(estado);
            }
        }

        private void MoverAgua(float destino, float segundos)
        {
            if (Mathf.Approximately(Nivel, destino))
            {
                return;
            }

            var paso = segundos > 0f ? alturaAgua / segundos * Time.deltaTime : alturaAgua;
            FijarNivel(Mathf.MoveTowards(Nivel, destino, paso));
        }

        private void FijarNivel(float nivel)
        {
            Nivel = Mathf.Clamp(nivel, 0f, alturaAgua);
            if (_objetoAgua == null)
            {
                return;
            }

            // Sin llegar a mojar todavía no se ve ni se toca, pero el nivel se guarda: a muchas
            // imágenes por segundo cada paso es diminuto, y si se perdiera el agua no subiría nunca.
            if (!TieneAgua)
            {
                _objetoAgua.SetActive(false);
                return;
            }

            var tamano = new Vector2(ancho, Nivel);
            _objetoAgua.transform.localPosition = new Vector3(0f, Nivel * 0.5f, 0f);
            _agua.size = tamano;
            _visualAgua.size = tamano;
            _objetoAgua.SetActive(true);
        }

        // Las púas llenan lo que estaba inundado, por debajo de la capa.
        private void BrotarPuas()
        {
            var alto = Mathf.Max(0.1f, alturaCapa - grosorCapa);
            var centro = new Vector2(CentroX, Suelo + alto * 0.5f);
            _puas = ZonaDeDanio.Crear($"{name}_Puas", centro, new Vector2(ancho, alto), _danioPuas, Capas.MascaraJugador,
                0f, _duracionPuas, Vector2.zero, colorHielo, _dibujo == null);
        }

        private void FijarCapa(bool puesta)
        {
            if (_objetoCapa == null)
            {
                return;
            }

            foreach (var atrapado in _atrapados)
            {
                if (atrapado != null)
                {
                    Physics2D.IgnoreCollision(_capa, atrapado, false);
                }
            }

            _atrapados.Clear();
            _objetoCapa.SetActive(puesta);
            if (!puesta)
            {
                return;
            }

            var dentro = InteriorDeLaCapa;
            foreach (var otro in Physics2D.OverlapBoxAll(dentro.center, dentro.size, 0f, Capas.MascaraJugador))
            {
                if (!otro.isTrigger)
                {
                    Physics2D.IgnoreCollision(_capa, otro, true);
                    _atrapados.Add(otro);
                }
            }
        }

        // --- Lo que se ve y se toca ---

        private void Construir()
        {
            if (_construida)
            {
                return;
            }

            _construida = true;
            var cuadrado = SpriteProvisional.Cuadrado();

            // El aviso: todo lo que va a ocupar el agua, translúcido y latiendo.
            var objetoAviso = Hijo("Aviso", new Vector2(0f, alturaAgua * 0.5f));
            _aviso = Rectangulo(objetoAviso, cuadrado, new Vector2(ancho, alturaAgua), OrdenAviso);
            _aviso.enabled = false;

            // El agua: un trigger que cambia la física de Kael, como el del recorrido.
            _objetoAgua = Hijo("Agua", Vector2.zero);
            _objetoAgua.SetActive(false);
            _visualAgua = Rectangulo(_objetoAgua, cuadrado, new Vector2(ancho, 0.1f), OrdenAgua);
            _agua = _objetoAgua.AddComponent<BoxCollider2D>();
            _agua.isTrigger = true;
            _objetoAgua.AddComponent<ZonaModificadorMovimiento>().FijarTipo(ZonaModificadorMovimiento.TIPOS.AGUA);

            // La capa de hielo: se pisa y resbala.
            _objetoCapa = Hijo("Capa", new Vector2(0f, alturaCapa - grosorCapa * 0.5f));
            _objetoCapa.SetActive(false);
            var capaSuelo = LayerMask.NameToLayer(Capas.Suelo);
            if (capaSuelo >= 0)
            {
                _objetoCapa.layer = capaSuelo;
            }

            _visualCapa = Rectangulo(_objetoCapa, cuadrado, new Vector2(ancho, grosorCapa), OrdenCapa);
            _visualCapa.color = colorHielo;
            _capa = _objetoCapa.AddComponent<BoxCollider2D>();
            _capa.size = new Vector2(ancho, grosorCapa);

            var resbala = new GameObject("Resbala");
            resbala.transform.SetParent(_objetoCapa.transform, false);
            resbala.transform.localPosition = new Vector3(0f, (grosorCapa + AltoResbala) * 0.5f, 0f);
            var zonaResbala = resbala.AddComponent<BoxCollider2D>();
            zonaResbala.isTrigger = true;
            zonaResbala.size = new Vector2(ancho, AltoResbala);
            resbala.AddComponent<ZonaModificadorMovimiento>().FijarTipo(ZonaModificadorMovimiento.TIPOS.HIELO);

            // Con arte, los rectángulos no se ven: lo dibuja todo DibujoDeFranja.
            if (arte != null && arte.Completo)
            {
                _visualAgua.enabled = false;
                _visualCapa.enabled = false;
                _dibujo = gameObject.AddComponent<DibujoDeFranja>();
                _dibujo.Montar(this, arte);
            }
        }

        private GameObject Hijo(string nombre, Vector2 posicion)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(transform, false);
            objeto.transform.localPosition = posicion;
            return objeto;
        }

        private static SpriteRenderer Rectangulo(GameObject objeto, Sprite sprite, Vector2 tamano, int orden)
        {
            var visual = objeto.AddComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.drawMode = SpriteDrawMode.Sliced;
            visual.size = tamano;
            visual.sortingOrder = orden;
            return visual;
        }

        // El aviso late; el agua que se va a congelar se pone blanca.
        private void Pintar()
        {
            if (!_construida)
            {
                return;
            }

            _aviso.enabled = Estado == ESTADOS_FRANJA.WARNING && _dibujo == null;
            if (_aviso.enabled)
            {
                _aviso.color = new Color(colorAgua.r, colorAgua.g, colorAgua.b, 0.12f + 0.18f * Mathf.PingPong(Time.time * 4f, 1f));
            }

            _visualAgua.color = Estado == ESTADOS_FRANJA.FREEZING
                ? Color.Lerp(colorAgua, new Color(colorHielo.r, colorHielo.g, colorHielo.b, 0.85f), 0.55f + 0.45f * Mathf.PingPong(Time.time * 5f, 1f))
                : colorAgua;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(colorAgua.r, colorAgua.g, colorAgua.b, 0.35f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * alturaAgua * 0.5f, new Vector3(ancho, alturaAgua, 0f));
            Gizmos.color = colorHielo;
            Gizmos.DrawWireCube(transform.position + Vector3.up * (alturaCapa - grosorCapa * 0.5f), new Vector3(ancho, grosorCapa, 0f));
        }
    }
}
