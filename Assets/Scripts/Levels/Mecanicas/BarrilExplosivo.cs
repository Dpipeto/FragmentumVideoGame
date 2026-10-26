using System;
using System.Collections.Generic;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// El barril explosivo del mundo de la Culpa (el <c>Explodable</c> del Anexo A). Los actos tienen
    /// consecuencias: lo que se enciende se lleva todo lo que tiene alrededor, también a Kael si se
    /// queda cerca.
    ///
    /// - <b>Intacto</b>: solo lo enciende el Ataque Cargado (<see cref="Golpe.Cargado"/>), otra
    ///   explosión o un disparo de Artillero (<see cref="EncenderEn"/>). Golpeado sin cargar, solo
    ///   suena (<see cref="OnSuena"/>). No es cobertura: las balas lo atraviesan… y lo encienden.
    /// - <b>Mecha</b>: <see cref="mecha"/> s parpadeando en rojo apagado. Da tiempo a alejarse.
    /// - <b>Quemado</b>: estalló (<see cref="Explosiones"/>): <see cref="danioEnemigos"/> a los enemigos
    ///   y <see cref="danioJugador"/> a Kael en <see cref="radio"/> u, y enciende los barriles que
    ///   tiene a menos de <see cref="cadena"/> u, uno tras otro. Vuelve a estar intacto cuando Kael
    ///   reaparece en un checkpoint, como las antorchas del Mundo 3: uno por vida.
    /// </summary>
    [DisallowMultipleComponent]
    public class BarrilExplosivo : MonoBehaviour, IGolpeable
    {
        private static readonly List<BarrilExplosivo> Registrados = new List<BarrilExplosivo>();

        [Tooltip("Segundos de mecha desde que se enciende hasta que estalla (Nivel4_Diseno.md: 0,6).")]
        [Min(0.05f)] public float mecha = 0.6f;

        [Tooltip("Alcance de la explosión, desde su centro.")]
        [Min(0.1f)] public float radio = 2.5f;

        [Min(0)] public int danioEnemigos = 3;

        [Min(0)] public int danioJugador = 1;

        [Tooltip("Enciende los barriles que tiene a menos de esto: la cadena.")]
        [Min(0f)] public float cadena = 3f;

        [Tooltip("Su centro, desde el pie: de ahí sale la explosión.")]
        [SerializeField] private Vector2 centro = new Vector2(0f, 0.5f);

        [Tooltip("Lo que cambia de color con su estado. Sin arte, un rectángulo.")]
        [SerializeField] private SpriteRenderer visual;

        [Tooltip("La explosión dibujada, que nace en su pie. Vacío = un destello provisional.")]
        [SerializeField] private ExplosionDibujada dibujoExplosion;

        [Header("Con arte")]
        [Tooltip("Intacto. Vacío = sin arte: el visual cambia de color.")]
        [SerializeField] private Sprite spriteIntacto;

        [Tooltip("Con la mecha encendida: los dos fotogramas con los que parpadea.")]
        [SerializeField] private Sprite spriteMecha1;

        [SerializeField] private Sprite spriteMecha2;

        [SerializeField] private Sprite spriteQuemado;

        [Header("Sin arte")]
        [SerializeField] private Color colorIntacto = new Color(0.45f, 0.26f, 0.15f, 1f);
        [SerializeField] private Color colorMecha = new Color(0.55f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color colorQuemado = new Color(0.12f, 0.1f, 0.09f, 1f);
        [SerializeField, Min(1f)] private float parpadeosPorSegundo = 8f;

        private SeguimientoJugador _kael;
        private float _estallaEn;

        /// <summary>Los barriles de la escena, para las cadenas y los disparos.</summary>
        public static IReadOnlyList<BarrilExplosivo> Todos => Registrados;

        public ESTADOS_BARRIL Estado { get; private set; }

        /// <summary>Cambió de estado: intacto, con la mecha encendida o quemado.</summary>
        public event Action<ESTADOS_BARRIL> OnEstado;

        /// <summary>Un golpe sin cargar: suena, y nada más.</summary>
        public event Action OnSuena;

        /// <summary>De donde sale la explosión.</summary>
        public Vector2 Centro => transform.TransformPoint(centro);

        /// <summary>Segundos que le quedan de mecha (0 si no está encendido).</summary>
        public float RestanteMecha => Estado == ESTADOS_BARRIL.FUSE ? Mathf.Max(0f, _estallaEn - Time.time) : 0f;

        public SpriteRenderer Visual => visual;

        // --- IGolpeable ---

        public bool EstaVivo => Estado == ESTADOS_BARRIL.INTACT && enabled && gameObject.activeInHierarchy;

        public bool RecibirGolpe(in Golpe golpe)
        {
            if (!EstaVivo)
            {
                return false;
            }

            if (!golpe.Cargado)
            {
                OnSuena?.Invoke();
                return false;
            }

            return Encender();
        }

        /// <summary>Le enciende la mecha si está intacto. Devuelve si se encendió.</summary>
        public bool Encender()
        {
            if (Estado != ESTADOS_BARRIL.INTACT)
            {
                return false;
            }

            _estallaEn = Time.time + mecha;
            Cambiar(ESTADOS_BARRIL.FUSE);
            return true;
        }

        /// <summary>Estalla ya (al acabarse la mecha).</summary>
        public void Estallar()
        {
            if (Estado == ESTADOS_BARRIL.BURNT)
            {
                return;
            }

            Cambiar(ESTADOS_BARRIL.BURNT);
            Explosiones.Estallar(Centro, radio, danioEnemigos, danioJugador, gameObject, cadena, this, dibujoExplosion, transform.position);
        }

        /// <summary>Vuelve a estar intacto. Lo hace solo cuando Kael reaparece.</summary>
        public void Reponer() => Cambiar(ESTADOS_BARRIL.INTACT);

        /// <summary>Enciende los barriles intactos cuyo centro está a menos de <paramref name="distancia"/> de <paramref name="punto"/>.</summary>
        public static int EncenderCercanos(Vector2 punto, float distancia, BarrilExplosivo excepto = null)
        {
            var encendidos = 0;
            foreach (var barril in Registrados.ToArray())
            {
                if (barril != excepto && barril != null && Vector2.Distance(barril.Centro, punto) < distancia && barril.Encender())
                {
                    encendidos++;
                }
            }

            return encendidos;
        }

        /// <summary>
        /// Un disparo en <paramref name="punto"/> (de <paramref name="radioBala"/> de radio) enciende el
        /// barril intacto que toque. Devuelve si encendió alguno: la bala se apaga en él.
        /// </summary>
        public static bool EncenderEn(Vector2 punto, float radioBala)
        {
            foreach (var barril in Registrados.ToArray())
            {
                if (barril == null || !barril.EstaVivo || !barril.TryGetComponent<Collider2D>(out var zona))
                {
                    continue;
                }

                if (Vector2.Distance(zona.ClosestPoint(punto), punto) <= radioBala && barril.Encender())
                {
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            _kael = new SeguimientoJugador(Reponer);
            Pintar();
        }

        private void OnEnable() => Registrados.Add(this);

        private void OnDisable()
        {
            Registrados.Remove(this);
            _kael?.Soltar();
        }

        private void Update()
        {
            // Solo para estar enganchado a Kael y enterarse de cuándo reaparece.
            _ = _kael.Jugador;

            if (Estado != ESTADOS_BARRIL.FUSE)
            {
                return;
            }

            if (Time.time >= _estallaEn)
            {
                Estallar();
                return;
            }

            if (visual != null)
            {
                var encendido = Mathf.Repeat(Time.time * parpadeosPorSegundo, 1f) < 0.5f;
                if (ConArte)
                {
                    visual.sprite = encendido ? spriteMecha1 : spriteMecha2;
                }
                else
                {
                    visual.color = encendido ? colorMecha : colorIntacto;
                }
            }
        }

        private void Cambiar(ESTADOS_BARRIL estado)
        {
            var cambio = Estado != estado;
            Estado = estado;
            Pintar();
            if (cambio)
            {
                OnEstado?.Invoke(estado);
            }
        }

        /// <summary>¿Lleva su dibujo? Entonces cambia de sprite con su estado, no de color.</summary>
        public bool ConArte => spriteIntacto != null && spriteMecha1 != null && spriteMecha2 != null && spriteQuemado != null;

        private void Pintar()
        {
            if (visual == null)
            {
                return;
            }

            if (ConArte)
            {
                visual.color = Color.white;
                visual.sprite = Estado switch
                {
                    ESTADOS_BARRIL.FUSE => spriteMecha1,
                    ESTADOS_BARRIL.BURNT => spriteQuemado,
                    _ => spriteIntacto
                };
                return;
            }

            visual.color = Estado switch
            {
                ESTADOS_BARRIL.FUSE => colorMecha,
                ESTADOS_BARRIL.BURNT => colorQuemado,
                _ => colorIntacto
            };
        }

        /// <summary>Vacía el registro. Lo usan las pruebas y el arranque sin recarga de dominio.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reiniciar() => Registrados.Clear();
    }
}
