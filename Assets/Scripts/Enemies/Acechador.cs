using System;
using System.Collections.Generic;
using Fragmentum.Core.Combate;
using Fragmentum.Levels.Escenario;
using UnityEngine;

namespace Fragmentum.Enemies
{
    public enum ESTADOS_ACECHADOR
    {
        /// <summary>Manda el <see cref="EnemyController"/>: merodea despacio y muerde si Kael se le pega.</summary>
        MERODEA,

        /// <summary>Oyó algo: se agazapa y gruñe. Es el aviso.</summary>
        AGAZAPADO,

        /// <summary>Embiste en línea recta hacia donde oyó el ruido, hasta chocar.</summary>
        EMBISTE,

        /// <summary>Se estrelló contra un árbol: no ataca, y los golpes valen el doble.</summary>
        ATURDIDO,

        /// <summary>No chocó con nada (o arrolló a Kael): frena y vuelve a escuchar.</summary>
        FRENA
    }

    /// <summary>
    /// El Acechador, el mini-jefe del mundo del Miedo (<c>Nivel3_Diseno.md</c>, sección 5): una
    /// bestia de sombra sin ojos que merodea por el claro entre tres árboles gruesos. De cerca es un
    /// <see cref="EnemyController"/> más, con hiperarmadura: merodea olfateando y, si Kael se le
    /// pega, echa la cabeza atrás y muerde. Esta pieza lleva lo suyo:
    ///
    /// - <b>Oye más lejos que el Centinela</b>, en todas direcciones: <see cref="oido"/> veces lo
    ///   que llega cada ruido (12 u corriendo).
    /// - <b>Al oír se agazapa</b> <see cref="tiempoAgazapado"/> s (el aviso) y <b>embiste</b> en
    ///   línea recta hacia donde oyó, a <see cref="velocidadEmbestida"/> u/s:
    ///   - contra un árbol (<see cref="Arbol"/>), se queda aturdido <see cref="tiempoAturdido"/> s y
    ///     los golpes valen <see cref="multiplicadorAturdido"/> veces más;
    ///   - contra Kael, le quita <see cref="danioEmbestida"/> y frena;
    ///   - si no choca con nada, frena pasado el sitio del ruido o en el borde de su territorio
    ///     (<see cref="limites"/>, el claro) y vuelve a escuchar.
    ///
    /// Mientras se agazapa, embiste, está aturdido o frena, su controlador está apagado, como el de
    /// la Plañidera. No toca <see cref="EnemyController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController), typeof(Health))]
    public class Acechador : MonoBehaviour
    {
        [Header("Oído")]
        [Tooltip("Oye en todas direcciones esta veces lo que llega cada ruido: 12 u corriendo (6 u).")]
        [Min(0f)] public float oido = 2f;

        [Header("Embestida")]
        [Tooltip("Se agazapa y gruñe: el aviso.")]
        [Min(0f)] public float tiempoAgazapado = 0.7f;

        [Min(0.1f)] public float velocidadEmbestida = 10f;

        [Tooltip("Si no choca con nada, sigue esto más allá del sitio del ruido antes de frenar.")]
        [Min(0f)] public float pasada = 2f;

        [Tooltip("Lo más que recorre de una embestida.")]
        [Min(0.1f)] public float alcanceMaximo = 16f;

        [Tooltip("Lo que quita a Kael si lo arrolla.")]
        [Min(1)] public int danioEmbestida = 2;

        [Tooltip("Lo que tarda en frenar.")]
        [Min(0f)] public float tiempoFrenada = 0.4f;

        [Header("Aturdido")]
        [Tooltip("Lo que se queda aturdido tras estrellarse contra un árbol.")]
        [Min(0f)] public float tiempoAturdido = 2.5f;

        [Tooltip("Aturdido, cada golpe le quita esto veces más.")]
        [Min(1f)] public float multiplicadorAturdido = 2f;

        [Header("Territorio")]
        [Tooltip("Entre qué x embiste (el claro): en un extremo, frena.")]
        public Vector2 limites = new Vector2(-10000f, 10000f);

        [Header("Referencias")]
        [Tooltip("La zona de daño de la embestida (capa EnemyWeapon): solo encendida mientras embiste.")]
        [SerializeField] private Hitbox embestida;

        [Tooltip("Lo que se voltea al girar (el del EnemyController).")]
        [SerializeField] private Transform pivote;

        [SerializeField] private SpriteRenderer sprite;

        [Header("Sin arte")]
        [Tooltip("Agazapado, el bloque se tiñe de este color (el aviso).")]
        [SerializeField] private Color colorAviso = new Color(0.62f, 0.2f, 0.24f, 1f);

        [Tooltip("Aturdido, se apaga a este color: es cuando se le castiga.")]
        [SerializeField] private Color colorAturdido = new Color(0.48f, 0.5f, 0.6f, 1f);

        private EnemyController _controlador;
        private Health _vida;
        private Collider2D _cuerpo;
        private RelevoDelControlador _relevo;
        private Action<Vector2, float> _alOir;
        private readonly List<Arbol> _arbolesDeSalida = new List<Arbol>();
        private Vector3 _escalaSprite = Vector3.one;
        private float _destino;
        private float _salida;
        private int _direccion = 1;
        private float _inicioDeEstado;
        private float _finDeEstado;
        private int _danioPendiente;

        public event Action<ESTADOS_ACECHADOR> OnCambio;

        /// <summary>Se estrelló contra ese árbol.</summary>
        public event Action<Arbol> OnChoque;

        public ESTADOS_ACECHADOR Estado { get; private set; } = ESTADOS_ACECHADOR.MERODEA;

        public bool Aturdido => Estado == ESTADOS_ACECHADOR.ATURDIDO;

        /// <summary>Hacia dónde embiste (o embistió): 1 derecha, -1 izquierda.</summary>
        public int Direccion => _direccion;

        /// <summary>¿Oye un ruido de <paramref name="radio"/> que suena a <paramref name="distancia"/>?</summary>
        public static bool Oye(float distancia, float radio, float oido) => distancia <= radio * oido;

        private Color Tinte => _controlador != null && _controlador.Datos != null ? _controlador.Datos.tinte : Color.white;

        private void Awake()
        {
            _controlador = GetComponent<EnemyController>();
            _vida = GetComponent<Health>();
            _cuerpo = GetComponent<Collider2D>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (pivote == null && sprite != null) pivote = sprite.transform.parent != transform ? sprite.transform.parent : sprite.transform;
            if (sprite != null) _escalaSprite = sprite.transform.localScale;
            _relevo = new RelevoDelControlador(_controlador, _vida, pivote);
            _alOir = AlOirRuido;

            if (embestida != null)
            {
                embestida.Danio = danioEmbestida;
                embestida.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            _vida.OnGolpeRecibido += AlRecibirGolpe;
            if (embestida != null) embestida.OnImpacto += AlArrollar;
        }

        private void OnDisable()
        {
            _vida.OnGolpeRecibido -= AlRecibirGolpe;
            if (embestida != null) embestida.OnImpacto -= AlArrollar;
            _relevo.Soltar();
        }

        private void Update()
        {
            _relevo.Escuchar(_alOir);
            if (!_vida.EstaVivo)
            {
                return;
            }

            switch (Estado)
            {
                case ESTADOS_ACECHADOR.AGAZAPADO:
                    if (Time.time >= _finDeEstado)
                    {
                        Embestir();
                    }

                    break;

                case ESTADOS_ACECHADOR.ATURDIDO:
                case ESTADOS_ACECHADOR.FRENA:
                    if (Time.time >= _finDeEstado)
                    {
                        Volver();
                    }

                    break;
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
                case ESTADOS_ACECHADOR.EMBISTE:
                    Avanzar();
                    break;

                case ESTADOS_ACECHADOR.FRENA:
                    // Se desliza un poco, cada vez más despacio, sin salir de su territorio.
                    var duracion = _finDeEstado - _inicioDeEstado;
                    var queda = duracion > 0f ? 1f - Mathf.Clamp01((Time.time - _inicioDeEstado) / duracion) : 0f;
                    var velocidad = velocidadEmbestida * queda * 0.5f;
                    var siguiente = _relevo.Centro.x + _direccion * velocidad * Time.fixedDeltaTime;
                    if (siguiente < limites.x || siguiente > limites.y || !_relevo.Mover(_direccion, velocidad))
                    {
                        _relevo.Detener();
                    }

                    break;

                case ESTADOS_ACECHADOR.AGAZAPADO:
                case ESTADOS_ACECHADOR.ATURDIDO:
                    _relevo.Detener();
                    break;
            }
        }

        // Aturdido, el golpe vale el doble: lo que falta se quita después del golpe, no dentro de su
        // aviso (si no, Health avisaría dos veces de la muerte).
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
                // Lo mata: que el controlador lo oiga y muera por su camino.
                _relevo.Devolver();
            }

            _vida.PerderVida(extra);
        }

        /// <summary>Oye un ruido en ese sitio, como si lo hubiera hecho Kael (para las pruebas).</summary>
        public void OirEn(Vector2 posicion) => Agazaparse(posicion);

        private void AlOirRuido(Vector2 posicion, float radio)
        {
            if (!_vida.EstaVivo || Estado != ESTADOS_ACECHADOR.MERODEA || !_controlador.enabled)
            {
                return;
            }

            var estado = _controlador.EstadoActual;
            if (estado != ESTADOS_ENEMIGO.IDLE && estado != ESTADOS_ENEMIGO.PATROL)
            {
                return;
            }

            if (Oye(Vector2.Distance(posicion, _relevo.Centro), radio, oido))
            {
                Agazaparse(posicion);
            }
        }

        private void Agazaparse(Vector2 posicion)
        {
            if (Estado != ESTADOS_ACECHADOR.MERODEA || !_vida.EstaVivo)
            {
                return;
            }

            _relevo.Tomar();
            _destino = posicion.x;
            var centro = _relevo.Centro.x;
            _direccion = Mathf.Abs(_destino - centro) > 0.01f ? (_destino > centro ? 1 : -1) : _relevo.Mirando;
            _relevo.Mirar(_direccion);
            Pintar(colorAviso, 0.75f);
            Entrar(ESTADOS_ACECHADOR.AGAZAPADO, tiempoAgazapado);
        }

        private void Embestir()
        {
            _salida = _relevo.Centro.x;
            _arbolesDeSalida.Clear();
            _arbolesDeSalida.AddRange(Arbol.Tocando(Caja(0f)));
            Pintar(colorAviso, 1f);
            if (embestida != null)
            {
                embestida.Danio = danioEmbestida;
                embestida.ReiniciarObjetivos();
                embestida.gameObject.SetActive(true);
            }

            Entrar(ESTADOS_ACECHADOR.EMBISTE, 0f);
        }

        private void Avanzar()
        {
            var paso = velocidadEmbestida * Time.fixedDeltaTime;
            var arbol = Arbol.Choca(Caja(paso), _arbolesDeSalida);
            if (arbol != null)
            {
                Estrellarse(arbol);
                return;
            }

            var x = _relevo.Centro.x;
            var siguiente = x + _direccion * paso;
            var pasoElRuido = (x - _destino) * _direccion >= pasada;
            if (pasoElRuido || Mathf.Abs(x - _salida) >= alcanceMaximo || siguiente < limites.x || siguiente > limites.y ||
                !_relevo.Mover(_direccion, velocidadEmbestida))
            {
                Frenar();
            }
        }

        private void Estrellarse(Arbol arbol)
        {
            ApagarEmbestida();
            _relevo.Detener();
            Pintar(colorAturdido, 1f);
            Entrar(ESTADOS_ACECHADOR.ATURDIDO, tiempoAturdido);
            OnChoque?.Invoke(arbol);
        }

        private void Frenar()
        {
            ApagarEmbestida();
            Pintar(Tinte, 1f);
            Entrar(ESTADOS_ACECHADOR.FRENA, tiempoFrenada);
        }

        private void Volver()
        {
            ApagarEmbestida();
            Pintar(Tinte, 1f);
            Entrar(ESTADOS_ACECHADOR.MERODEA, 0f);
            _relevo.Devolver();
        }

        // Arrolló a Kael: le quitó lo suyo (la zona de daño) y frena.
        private void AlArrollar(IGolpeable objetivo, Golpe golpe)
        {
            if (Estado == ESTADOS_ACECHADOR.EMBISTE)
            {
                Frenar();
            }
        }

        private void AlRecibirGolpe(Golpe golpe)
        {
            if (Estado == ESTADOS_ACECHADOR.MERODEA)
            {
                return;
            }

            _relevo.AntesDeMorir();
            if (Aturdido && _vida.EstaVivo)
            {
                _danioPendiente += Mathf.CeilToInt(golpe.Cantidad * (multiplicadorAturdido - 1f));
            }
        }

        private void ApagarEmbestida()
        {
            if (embestida != null)
            {
                embestida.gameObject.SetActive(false);
            }
        }

        // La caja de su cuerpo, alargada lo que avanza en este paso hacia donde embiste.
        private Rect Caja(float adelante)
        {
            var b = _cuerpo != null ? _cuerpo.bounds : new Bounds(transform.position + Vector3.up, new Vector3(2f, 1.4f, 0f));
            var caja = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
            if (_direccion > 0)
            {
                caja.width += adelante;
            }
            else
            {
                caja.xMin -= adelante;
            }

            return caja;
        }

        private void Entrar(ESTADOS_ACECHADOR nuevo, float duracion)
        {
            Estado = nuevo;
            _inicioDeEstado = Time.time;
            _finDeEstado = Time.time + duracion;
            OnCambio?.Invoke(nuevo);
        }

        // Sin arte: agazapado, el bloque se tiñe y se encoge; aturdido, se apaga.
        private void Pintar(Color color, float alto)
        {
            if (sprite == null)
            {
                return;
            }

            sprite.color = color;
            sprite.transform.localScale = new Vector3(_escalaSprite.x, _escalaSprite.y * alto, _escalaSprite.z);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.62f, 0.2f, 0.24f, 0.6f);
            var centro = transform.position + Vector3.up * 0.7f;
            Gizmos.DrawWireSphere(centro, 6f * oido);
            if (limites.x > -1000f && limites.y < 1000f)
            {
                Gizmos.DrawLine(new Vector3(limites.x, centro.y, 0f), new Vector3(limites.y, centro.y, 0f));
            }
        }
    }
}
