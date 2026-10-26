using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using UnityEngine;

namespace Fragmentum.Enemies
{
    public enum ESTADOS_CENTINELA_CIEGO
    {
        /// <summary>Manda el <see cref="EnemyController"/>: patrulla despacio y solo nota el roce.</summary>
        RONDA,

        /// <summary>Oyó algo: se para y ladea la cabeza. Es el aviso.</summary>
        ESCUCHA,

        /// <summary>Va hacia donde oyó el ruido (no hacia Kael: no lo ve).</summary>
        VA,

        /// <summary>La estocada con la bayoneta, en el sitio.</summary>
        ESTOCADA,

        /// <summary>Escucha un poco más; si no oye nada, vuelve a su ronda.</summary>
        BUSCA
    }

    /// <summary>
    /// El oído del Centinela Ciego (<c>Nivel3_Diseno.md</c>, sección 5). No ve: oye. Su
    /// <see cref="EnemyController"/> patrulla y, con la detección por tacto, solo nota a Kael si lo
    /// roza. Esta pieza lleva lo demás:
    ///
    /// - <b>El cono.</b> Oye los ruidos de Kael (correr, el chasquido de un cepo) que le llegan:
    ///   delante, hasta donde llega cada ruido, que es lo que enseñan los anillos de Kael
    ///   (<see cref="oidoDelante"/>); detrás, solo la mitad (<see cref="oidoDetras"/>). Por la
    ///   espalda se le puede sorprender.
    /// - <b>El aviso.</b> Al oír se para y ladea la cabeza <see cref="tiempoEscucha"/> s.
    /// - <b>Va hacia donde oyó</b> a <see cref="velocidadIr"/> u/s, no hacia Kael, y al llegar lanza
    ///   la estocada en el sitio. Si Kael sigue allí, o se le pone delante por el camino, le alcanza.
    /// - <b>Busca</b> <see cref="tiempoBusqueda"/> s; si no oye nada más, vuelve a su ronda.
    /// - Si Kael lo roza o le pega mientras escucha, va o busca, se gira y lanza la estocada.
    ///
    /// La estocada usa el arma y los tiempos del ataque de sus datos (2 de daño; se abre a los
    /// 0,3 s). Mientras oye, va y apuñala, su controlador está apagado, como el de la Plañidera.
    /// Con arte, su Animator sigue el estado de esta pieza (el entero «Estado»): escucha con la mano
    /// en la máscara y apuñala con su dibujo; sin arte, el bloque se ladea y se aclara.
    /// No toca <see cref="EnemyController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController), typeof(Health))]
    public class CentinelaCiego : MonoBehaviour
    {
        [Header("Oído")]
        [Tooltip("Delante oye hasta donde llega cada ruido (6 u corriendo, 8 u un cepo), por esto.")]
        [Min(0f)] public float oidoDelante = 1f;

        [Tooltip("Detrás oye menos: esta fracción de lo que llega el ruido.")]
        [Min(0f)] public float oidoDetras = 0.5f;

        [Header("Ritmo")]
        [Tooltip("Al oír se para y ladea la cabeza: el aviso.")]
        [Min(0f)] public float tiempoEscucha = 0.6f;

        [Tooltip("Va hacia donde oyó a esta velocidad.")]
        [Min(0.1f)] public float velocidadIr = 4f;

        [Tooltip("Lanza la estocada a esta distancia del sitio donde oyó.")]
        [Min(0f)] public float distanciaEstocada = 1f;

        [Tooltip("Lo más que va hacia el ruido; después apuñala donde esté.")]
        [Min(0.1f)] public float tiempoMaximoIr = 4f;

        [Tooltip("Tras la estocada escucha esto; si no oye nada, vuelve a su ronda.")]
        [Min(0f)] public float tiempoBusqueda = 2f;

        [Header("Referencias")]
        [Tooltip("Lo que se voltea al girar (el del EnemyController).")]
        [SerializeField] private Transform pivote;

        [SerializeField] private SpriteRenderer sprite;

        [Tooltip("Con arte: su Animator, que sigue el estado de su oído (el entero «Estado»). Vacío = se busca solo.")]
        [SerializeField] private Animator animator;

        [Header("Sin arte")]
        [Tooltip("Cuánto ladea la cabeza al escuchar (grados).")]
        [SerializeField] private float ladeo = 14f;

        [Tooltip("Mientras escucha, el bloque se aclara a este color.")]
        [SerializeField] private Color colorAviso = new Color(0.72f, 0.66f, 0.86f, 1f);

        private EnemyController _controlador;
        private Health _vida;
        private RelevoDelControlador _relevo;
        private Action<Vector2, float> _alOir;
        private float _destino;
        private float _inicioDeEstado;
        private float _finDeEstado;
        private bool _golpeLanzado;
        private int _pasosConArma;

        public event Action<ESTADOS_CENTINELA_CIEGO> OnCambio;

        public ESTADOS_CENTINELA_CIEGO Estado { get; private set; } = ESTADOS_CENTINELA_CIEGO.RONDA;

        /// <summary>¿Lleva el mando el oído (el controlador, apagado)?</summary>
        public bool Alerta => Estado != ESTADOS_CENTINELA_CIEGO.RONDA;

        /// <summary>Donde oyó el último ruido (x).</summary>
        public float Destino => _destino;

        /// <summary>
        /// ¿Oye un ruido de <paramref name="radio"/> que suena en <paramref name="ruido"/>? Delante,
        /// hasta donde llega por <paramref name="delante"/>; detrás, por <paramref name="detras"/>.
        /// </summary>
        public static bool Oye(Vector2 oreja, int mirando, Vector2 ruido, float radio, float delante, float detras)
        {
            var delta = ruido - oreja;
            var alcance = radio * (delta.x * mirando >= 0f ? delante : detras);
            return delta.sqrMagnitude <= alcance * alcance;
        }

        private EnemyData Datos => _controlador.Datos;

        private Color Tinte => Datos != null ? Datos.tinte : Color.white;

        /// <summary>¿Lleva su arte? Entonces el aviso y la estocada son su dibujo.</summary>
        public bool ConArte => animator != null && animator.runtimeAnimatorController != null;

        private void Awake()
        {
            _controlador = GetComponent<EnemyController>();
            _vida = GetComponent<Health>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (pivote == null && sprite != null) pivote = sprite.transform.parent != transform ? sprite.transform.parent : sprite.transform;
            _relevo = new RelevoDelControlador(_controlador, _vida, pivote);
            _alOir = AlOirRuido;
        }

        private void OnEnable() => _vida.OnGolpeRecibido += AlRecibirGolpe;

        private void OnDisable()
        {
            _vida.OnGolpeRecibido -= AlRecibirGolpe;
            _relevo.Soltar();
        }

        private void Update()
        {
            _relevo.Escuchar(_alOir);

            // Antes de que el Animator se evalúe en este fotograma.
            if (ConArte && animator.isInitialized)
            {
                AnimatorParams.FijarEntero(animator, AnimatorParams.EstadoMecanica, (int)Estado);
            }

            if (!_vida.EstaVivo || !Alerta)
            {
                return;
            }

            var ahora = Time.time;
            switch (Estado)
            {
                case ESTADOS_CENTINELA_CIEGO.ESCUCHA:
                    if (KaelLoRoza())
                    {
                        Apunalar(_relevo.CentroDeKael.x);
                    }
                    else if (ahora >= _finDeEstado)
                    {
                        Ladear(false);
                        Entrar(ESTADOS_CENTINELA_CIEGO.VA, tiempoMaximoIr);
                    }

                    break;

                case ESTADOS_CENTINELA_CIEGO.ESTOCADA:
                    var t = ahora - _inicioDeEstado;
                    if (!_golpeLanzado && t >= Datos.inicioVentanaGolpe)
                    {
                        _golpeLanzado = true;
                        _relevo.FijarArma(true);
                    }
                    else if (_golpeLanzado && _pasosConArma > 0 && t >= Datos.finVentanaGolpe)
                    {
                        _relevo.FijarArma(false);
                    }

                    // Sin un paso de física con el arma abierta, la estocada no podría tocar a nadie.
                    var armaUsada = _pasosConArma > 0 || _controlador.Arma == null;
                    if (t >= Mathf.Max(Datos.duracionAtaque, Datos.finVentanaGolpe) && armaUsada)
                    {
                        _relevo.FijarArma(false);
                        Entrar(ESTADOS_CENTINELA_CIEGO.BUSCA, tiempoBusqueda);
                    }

                    break;

                case ESTADOS_CENTINELA_CIEGO.BUSCA:
                    if (KaelLoRoza())
                    {
                        Apunalar(_relevo.CentroDeKael.x);
                    }
                    else if (ahora >= _finDeEstado)
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
                case ESTADOS_CENTINELA_CIEGO.VA:
                    var centro = _relevo.Centro;
                    var direccion = _destino >= centro.x ? 1 : -1;
                    if (KaelLoRoza() || KaelAlAlcance(direccion))
                    {
                        Apunalar(_relevo.CentroDeKael.x);
                    }
                    else if (Mathf.Abs(_destino - centro.x) <= distanciaEstocada || Time.time >= _finDeEstado ||
                             !_relevo.Mover(direccion, velocidadIr))
                    {
                        Apunalar(_destino);
                    }

                    break;

                case ESTADOS_CENTINELA_CIEGO.ESTOCADA:
                    _relevo.Detener();
                    if (_golpeLanzado && _controlador.Arma != null && _controlador.Arma.gameObject.activeSelf)
                    {
                        _pasosConArma++;
                    }

                    break;

                case ESTADOS_CENTINELA_CIEGO.ESCUCHA:
                case ESTADOS_CENTINELA_CIEGO.BUSCA:
                    _relevo.Detener();
                    break;
            }
        }

        /// <summary>Oye un ruido en ese sitio, como si lo hubiera hecho Kael (para las pruebas).</summary>
        public void OirEn(Vector2 posicion) => Escuchar(posicion);

        private void AlOirRuido(Vector2 posicion, float radio)
        {
            if (!_vida.EstaVivo)
            {
                return;
            }

            // De ronda, solo si su controlador no está ya con Kael (lo rozó y lo persigue o lo apuñala).
            if (Estado == ESTADOS_CENTINELA_CIEGO.RONDA)
            {
                var estado = _controlador.EstadoActual;
                if (!_controlador.enabled || (estado != ESTADOS_ENEMIGO.IDLE && estado != ESTADOS_ENEMIGO.PATROL))
                {
                    return;
                }
            }
            else if (Estado != ESTADOS_CENTINELA_CIEGO.BUSCA)
            {
                return;
            }

            if (Oye(_relevo.Centro, _relevo.Mirando, posicion, radio, oidoDelante, oidoDetras))
            {
                Escuchar(posicion);
            }
        }

        private void Escuchar(Vector2 posicion)
        {
            if (!Alerta)
            {
                _relevo.Tomar();
            }

            _destino = posicion.x;
            _relevo.Mirar(posicion.x >= _relevo.Centro.x ? 1 : -1);
            Ladear(true);
            Entrar(ESTADOS_CENTINELA_CIEGO.ESCUCHA, tiempoEscucha);
        }

        private void Apunalar(float haciaX)
        {
            Ladear(false);
            _relevo.Detener();
            _relevo.Mirar(haciaX >= _relevo.Centro.x ? 1 : -1);
            _golpeLanzado = false;
            _pasosConArma = 0;
            Entrar(ESTADOS_CENTINELA_CIEGO.ESTOCADA, Datos != null ? Datos.duracionAtaque : 0.6f);
        }

        private void Volver()
        {
            Ladear(false);
            _relevo.FijarArma(false);
            Entrar(ESTADOS_CENTINELA_CIEGO.RONDA, 0f);
            _relevo.Devolver();
        }

        // Si le pegan mientras oye, va o busca, sabe dónde está Kael: se gira y apuñala.
        private void AlRecibirGolpe(Golpe golpe)
        {
            if (!Alerta)
            {
                return;
            }

            _relevo.AntesDeMorir();
            if (_vida.EstaVivo && Estado != ESTADOS_CENTINELA_CIEGO.ESTOCADA)
            {
                Apunalar(golpe.Origen.x);
            }
        }

        private bool KaelLoRoza()
        {
            var radio = Datos != null ? Datos.detectionRadius : 1.2f;
            return _relevo.KaelVivo && Vector2.Distance(_relevo.CentroDeKael, _relevo.Centro) <= radio;
        }

        // Kael, delante y al alcance de la bayoneta: se lo encuentra por el camino.
        private bool KaelAlAlcance(int direccion)
        {
            if (!_relevo.KaelVivo || Datos == null)
            {
                return false;
            }

            var delta = _relevo.CentroDeKael - _relevo.Centro;
            return delta.x * direccion >= 0f && Mathf.Abs(delta.x) <= Datos.attackRadius && Mathf.Abs(delta.y) <= Datos.alturaAtaque;
        }

        private void Entrar(ESTADOS_CENTINELA_CIEGO nuevo, float duracion)
        {
            Estado = nuevo;
            _inicioDeEstado = Time.time;
            _finDeEstado = Time.time + duracion;
            OnCambio?.Invoke(nuevo);
        }

        // Sin arte: ladea el bloque y lo aclara mientras escucha. Con arte, escucha su dibujo.
        private void Ladear(bool escuchando)
        {
            if (sprite == null || ConArte)
            {
                return;
            }

            sprite.transform.localRotation = Quaternion.Euler(0f, 0f, escuchando ? ladeo : 0f);
            sprite.color = escuchando ? colorAviso : Tinte;
        }

        private void OnDrawGizmosSelected()
        {
            // El alcance de lo que oye corriendo (6 u): delante y detrás.
            var centro = transform.position + Vector3.up * 0.8f;
            var mirando = Application.isPlaying && _controlador != null ? _controlador.Mirando : 1;
            Gizmos.color = new Color(0.72f, 0.66f, 0.86f, 0.7f);
            Gizmos.DrawLine(centro, centro + Vector3.right * mirando * 6f * oidoDelante);
            Gizmos.DrawLine(centro, centro - Vector3.right * mirando * 6f * oidoDetras);
        }
    }
}
