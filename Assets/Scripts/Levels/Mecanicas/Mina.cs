using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// La mina del río del mundo de la Culpa: una mina de contacto sujeta al fondo por su cadena, que
    /// sube y baja. Lo que dejó la guerra sigue ahí: una consecuencia que espera.
    ///
    /// Sube y baja <see cref="recorrido"/> u cada <see cref="periodo"/> s: abajo un tercio del ciclo,
    /// sube, arriba otro tercio y baja (<see cref="Altura"/>). Arriba se pasa por debajo; abajo, por
    /// encima. Si Kael la toca, o le da un golpe de espada, estalla (<see cref="Explosiones"/>):
    /// <see cref="danioJugador"/> a Kael y <see cref="danioEnemigos"/> a los enemigos en
    /// <see cref="radio"/> u. No vuelve hasta que Kael reaparece en un checkpoint: se puede pagar un
    /// corazón para quitarla de en medio.
    ///
    /// El objeto está en el fondo, donde la sujeta la cadena; la bola es su hijo, en la capa de los
    /// enemigos (la espada la golpea), y nota a Kael con una consulta.
    /// </summary>
    [DisallowMultipleComponent]
    public class Mina : MonoBehaviour, IGolpeable
    {
        [Tooltip("Lo que sube y baja la bola.")]
        [Min(0.1f)] public float recorrido = 2f;

        [Tooltip("Segundos de un ciclo entero: abajo, sube, arriba y baja.")]
        [Min(0.3f)] public float periodo = 3f;

        [Tooltip("Abajo, el centro de la bola queda a esto del fondo.")]
        [Min(0f)] public float sobreElFondo = 0.5f;

        [Tooltip("En qué punto del ciclo empieza (segundos).")]
        [Min(0f)] public float desfase;

        [Min(0.1f)] public float radio = 1.5f;

        [Min(0)] public int danioJugador = 1;

        [Min(0)] public int danioEnemigos = 3;

        [Tooltip("La bola: se mueve y es la que choca.")]
        [SerializeField] private Transform bola;

        [Tooltip("La cadena: un rectángulo que va del fondo a la bola.")]
        [SerializeField] private Transform cadena;

        [Tooltip("La explosión dibujada, que nace bajo la bola. Vacío = un destello provisional.")]
        [SerializeField] private ExplosionDibujada dibujoExplosion;

        [Header("Con arte")]
        [Tooltip("El dibujo de la bola. Con Kael cerca, parpadea entre su reposo y su aviso.")]
        [SerializeField] private SpriteRenderer dibujoBola;

        [SerializeField] private Sprite spriteReposo;

        [Tooltip("A punto de estallar: las puntas en rojo.")]
        [SerializeField] private Sprite spriteAviso;

        [Tooltip("Con el cuerpo de Kael a esta distancia del centro de la bola (u), avisa.")]
        [Min(0f)] public float distanciaAviso = 2f;

        [SerializeField, Min(1f)] private float parpadeosPorSegundo = 6f;

        private SeguimientoJugador _kael;
        private float _inicio;

        /// <summary>¿Ya estalló?</summary>
        public bool Estallada { get; private set; }

        /// <summary>Estalló.</summary>
        public event Action OnEstalla;

        public Transform Bola => bola;

        /// <summary>El centro de la bola.</summary>
        public Vector2 Centro => bola != null ? (Vector2)bola.position : (Vector2)transform.position;

        /// <summary>El radio de la bola, en el mundo: la explosión dibujada nace en su parte de abajo.</summary>
        public float RadioBola => bola != null && bola.TryGetComponent<CircleCollider2D>(out var zona) ? zona.radius * Mathf.Abs(bola.lossyScale.x) : 0f;

        /// <summary>
        /// Lo que la bola está por encima de su punto más bajo en el instante <paramref name="t"/> del
        /// ciclo: abajo el primer tercio, sube el sexto siguiente, arriba otro tercio y baja el último.
        /// </summary>
        public static float Altura(float t, float periodo, float recorrido)
        {
            var fase = Mathf.Repeat(t, periodo) / periodo;
            if (fase < 1f / 3f)
            {
                return 0f;
            }

            if (fase < 0.5f)
            {
                return recorrido * Mathf.SmoothStep(0f, 1f, (fase - 1f / 3f) * 6f);
            }

            if (fase < 5f / 6f)
            {
                return recorrido;
            }

            return recorrido * Mathf.SmoothStep(1f, 0f, (fase - 5f / 6f) * 6f);
        }

        // --- IGolpeable: un golpe de espada la hace estallar ---

        public bool EstaVivo => !Estallada && enabled && gameObject.activeInHierarchy;

        public bool RecibirGolpe(in Golpe golpe)
        {
            if (!EstaVivo)
            {
                return false;
            }

            Estallar();
            return true;
        }

        /// <summary>Estalla ya.</summary>
        public void Estallar()
        {
            if (Estallada)
            {
                return;
            }

            var centro = Centro;
            Estallada = true;
            Mostrar(false);
            Explosiones.Estallar(centro, radio, danioEnemigos, danioJugador, gameObject, dibujo: dibujoExplosion,
                pie: centro + Vector2.down * RadioBola);
            OnEstalla?.Invoke();
        }

        /// <summary>Vuelve a su sitio. Lo hace sola cuando Kael reaparece.</summary>
        public void Reponer()
        {
            Estallada = false;
            Mostrar(true);
        }

        private void Awake()
        {
            _kael = new SeguimientoJugador(Reponer);
            _inicio = Time.time;
            Colocar();
        }

        private void OnDisable() => _kael?.Soltar();

        private void Update()
        {
            // Para estar enganchada a Kael (y enterarse de cuándo reaparece) y saber si lo tiene cerca.
            var kael = _kael.Jugador;
            if (!Estallada)
            {
                Colocar();
                Avisar(kael);
            }
        }

        /// <summary>¿Avisa ahora? Con su dibujo, parpadea si Kael está a <see cref="distanciaAviso"/> o menos.</summary>
        public bool Avisando { get; private set; }

        // Con Kael cerca, la bola parpadea entre su reposo y su aviso; si no, reposa.
        private void Avisar(PlayerController kael)
        {
            if (dibujoBola == null || spriteReposo == null || spriteAviso == null)
            {
                return;
            }

            var cuerpo = kael != null ? kael.GetComponent<Collider2D>() : null;
            var cerca = cuerpo != null ? (Vector2)cuerpo.bounds.ClosestPoint(Centro) : kael != null ? (Vector2)kael.transform.position : Centro;
            Avisando = kael != null && kael.EstaVivo && Vector2.Distance(cerca, Centro) <= distanciaAviso;
            var encendido = Avisando && Mathf.Repeat(Time.time * parpadeosPorSegundo, 1f) < 0.5f;
            dibujoBola.sprite = encendido ? spriteAviso : spriteReposo;
        }

        // La bola a su altura y la cadena del fondo hasta ella.
        private void Colocar()
        {
            var altura = sobreElFondo + Altura(Time.time - _inicio + desfase, periodo, recorrido);
            if (bola != null)
            {
                bola.localPosition = new Vector3(0f, altura, 0f);
            }

            if (cadena != null)
            {
                cadena.localPosition = new Vector3(0f, altura * 0.5f, 0f);
                if (cadena.TryGetComponent<SpriteRenderer>(out var dibujo) && dibujo.drawMode == SpriteDrawMode.Tiled)
                {
                    // Con arte, el tramo de cadena repetido a lo largo (girado: su largo es el alto).
                    dibujo.size = new Vector2(altura, dibujo.size.y);
                }
                else
                {
                    var escala = cadena.localScale;
                    cadena.localScale = new Vector3(escala.x, altura, 1f);
                }
            }
        }

        private void Mostrar(bool visible)
        {
            foreach (var dibujo in GetComponentsInChildren<SpriteRenderer>(true))
            {
                dibujo.enabled = visible;
            }

            foreach (var zona in GetComponentsInChildren<Collider2D>(true))
            {
                zona.enabled = visible;
            }
        }

        // Tocarla con el cuerpo la hace estallar. Es una consulta y no un trigger: Kael y la capa de
        // los enemigos, en la que está la bola para que la espada la golpee, no chocan entre sí.
        private void FixedUpdate()
        {
            if (Estallada || bola == null || !bola.TryGetComponent<CircleCollider2D>(out var zona))
            {
                return;
            }

            foreach (var otro in Physics2D.OverlapCircleAll(Centro, RadioBola, Capas.Mascara(Capas.Player)))
            {
                if (!otro.isTrigger && otro.attachedRigidbody != null && otro.attachedRigidbody.GetComponent<PlayerController>() != null)
                {
                    Estallar();
                    return;
                }
            }
        }
    }
}
