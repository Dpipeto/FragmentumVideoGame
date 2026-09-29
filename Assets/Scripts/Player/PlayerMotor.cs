using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Única pieza que toca el Rigidbody2D del jugador. Los estados le piden movimiento y el
    /// motor lo aplica por velocidad (<c>linearVelocity</c>), nunca moviendo el Transform.
    ///
    /// Corrige dos defectos del prototipo (3.2):
    /// - El movimiento teletransportaba al jugador con <c>transform.position</c> y peleaba con
    ///   el dash, el rebote del daño y las colisiones.
    /// - La física se aplicaba en Update. Aquí el control continuo se aplica en FixedUpdate
    ///   (<see cref="MoverHorizontal"/>, <see cref="PasoFisica"/>). Los impulsos instantáneos
    ///   (salto, dash, retroceso) fijan la velocidad una sola vez al entrar al estado.
    ///
    /// También reúne los modificadores del entorno (agua, hielo), que multiplican velocidad,
    /// gravedad, salto y aceleración sin que los estados tengan que saber de dónde vienen.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D cuerpoRigido;

        [Tooltip("Collider del cuerpo. Se encoge al agacharse.")]
        [SerializeField] private CapsuleCollider2D colisionador;

        private readonly List<KeyValuePair<object, ModificadorMovimiento>> _modificadores =
            new List<KeyValuePair<object, ModificadorMovimiento>>();

        private ParametrosJugador _p = new ParametrosJugador();
        private Vector2 _tamanoDePie;
        private Vector2 _offsetDePie;
        private CapsuleDirection2D _direccionDePie;
        private bool _gravedadSuspendida;

        /// <summary>Se emite al entrar o salir del agua: (bajo el agua).</summary>
        public event Action<bool> OnMedioAcuaticoCambiado;

        public Rigidbody2D Cuerpo => cuerpoRigido;

        public Vector2 Velocidad => cuerpoRigido != null ? cuerpoRigido.linearVelocity : Vector2.zero;

        public bool Agachado { get; private set; }

        /// <summary>Altura del cuerpo de pie, para comprobar si cabe al levantarse.</summary>
        public float AlturaDePie => _tamanoDePie.y;

        public bool BajoElAgua { get; private set; }

        public float MultVelocidad { get; private set; } = 1f;

        public float MultGravedad { get; private set; } = 1f;

        public float MultSalto { get; private set; } = 1f;

        public float MultAceleracion { get; private set; } = 1f;

        private void Awake()
        {
            if (cuerpoRigido == null)
            {
                cuerpoRigido = GetComponent<Rigidbody2D>();
            }

            if (colisionador == null)
            {
                colisionador = GetComponent<CapsuleCollider2D>();
            }

            cuerpoRigido.freezeRotation = true;
            cuerpoRigido.interpolation = RigidbodyInterpolation2D.Interpolate;
            cuerpoRigido.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (colisionador != null)
            {
                _tamanoDePie = colisionador.size;
                _offsetDePie = colisionador.offset;
                _direccionDePie = colisionador.direction;

                // Sin fricción: el jugador no se queda pegado a una pared al empujarla en el
                // aire. El frenado en el suelo lo hace el código, no el material.
                if (colisionador.sharedMaterial == null)
                {
                    colisionador.sharedMaterial = new PhysicsMaterial2D("JugadorSinFriccion")
                    {
                        friction = 0f,
                        bounciness = 0f
                    };
                }
            }
            else
            {
                Debug.LogWarning(
                    "[PlayerMotor] El jugador no tiene CapsuleCollider2D: no podrá agacharse.", this);
            }

            Recalcular();
        }

        /// <summary>Recibe los parámetros del controlador. Se llama una vez, al iniciar.</summary>
        public void Configurar(ParametrosJugador parametros)
        {
            _p = parametros ?? new ParametrosJugador();
            ActualizarEscalaGravedad();
        }

        // --- Control continuo: se llama desde ActualizarFisica (FixedUpdate) ---

        /// <summary>
        /// Acelera la velocidad horizontal hacia <paramref name="direccion"/> × velocidad
        /// máxima. Con dirección 0 frena. La aceleración depende de si se está en el suelo
        /// y de los modificadores (el hielo la reduce).
        /// </summary>
        public void MoverHorizontal(float direccion, float velocidadMaxima, bool enSuelo)
        {
            if (cuerpoRigido == null)
            {
                return;
            }

            var objetivo = Mathf.Clamp(direccion, -1f, 1f) * velocidadMaxima * MultVelocidad;
            var acelera = Mathf.Abs(objetivo) > 0.01f;
            var aceleracion = enSuelo
                ? (acelera ? _p.aceleracionSuelo : _p.desaceleracionSuelo)
                : (acelera ? _p.aceleracionAire : _p.desaceleracionAire);

            var v = cuerpoRigido.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, objetivo, aceleracion * MultAceleracion * Time.fixedDeltaTime);
            cuerpoRigido.linearVelocity = v;
        }

        /// <summary>Limita la velocidad de caída (wall slide).</summary>
        public void LimitarCaida(float maximo)
        {
            var v = cuerpoRigido.linearVelocity;
            if (v.y < -maximo)
            {
                v.y = -maximo;
                cuerpoRigido.linearVelocity = v;
            }
        }

        /// <summary>
        /// Cierre de cada paso de física: ajusta la gravedad (más fuerte al caer, nula durante
        /// el dash) y limita la velocidad de caída. Lo llama el controlador tras el estado.
        /// </summary>
        public void PasoFisica()
        {
            if (cuerpoRigido == null)
            {
                return;
            }

            ActualizarEscalaGravedad();
            LimitarCaida(_p.velocidadCaidaMaxima * Mathf.Max(0.2f, MultGravedad));
        }

        // --- Impulsos instantáneos: se llaman una vez, al entrar en un estado ---

        public void FijarVelocidad(Vector2 velocidad)
        {
            if (cuerpoRigido != null)
            {
                cuerpoRigido.linearVelocity = velocidad;
            }
        }

        public void FijarVelocidadVertical(float velocidadY)
        {
            if (cuerpoRigido == null)
            {
                return;
            }

            var v = cuerpoRigido.linearVelocity;
            v.y = velocidadY;
            cuerpoRigido.linearVelocity = v;
        }

        /// <summary>Recorta la subida al soltar el salto (salto de altura variable).</summary>
        public void CortarSubida(float factor)
        {
            var v = cuerpoRigido.linearVelocity;
            if (v.y > 0f)
            {
                v.y *= Mathf.Clamp01(factor);
                cuerpoRigido.linearVelocity = v;
            }
        }

        public void Detener() => FijarVelocidad(Vector2.zero);

        /// <summary>El dash anula la gravedad mientras dura.</summary>
        public void SuspenderGravedad(bool suspender)
        {
            _gravedadSuspendida = suspender;
            ActualizarEscalaGravedad();
        }

        /// <summary>Coloca al jugador sin arrastrar velocidad (reaparición en un checkpoint).</summary>
        public void Teletransportar(Vector2 posicion)
        {
            if (cuerpoRigido != null)
            {
                cuerpoRigido.position = posicion;
                cuerpoRigido.linearVelocity = Vector2.zero;
            }

            transform.position = posicion;
        }

        // --- Agacharse ---

        /// <summary>Encoge o restaura el collider manteniendo los pies en el mismo sitio.</summary>
        public void FijarAgachado(bool agachado)
        {
            if (colisionador == null || Agachado == agachado)
            {
                return;
            }

            Agachado = agachado;

            if (!agachado)
            {
                colisionador.size = _tamanoDePie;
                colisionador.offset = _offsetDePie;
                colisionador.direction = _direccionDePie;
                return;
            }

            var alto = _tamanoDePie.y * _p.alturaAgachado;
            colisionador.size = new Vector2(_tamanoDePie.x, alto);
            colisionador.offset = new Vector2(_offsetDePie.x, _offsetDePie.y - (_tamanoDePie.y - alto) * 0.5f);

            // Una cápsula vertical más baja que ancha deja de ser una cápsula válida.
            colisionador.direction = alto < _tamanoDePie.x ? CapsuleDirection2D.Horizontal : _direccionDePie;
        }

        // --- Modificadores del entorno ---

        /// <summary>
        /// Añade (o sustituye) el modificador de una fuente: una zona de agua, un suelo de
        /// hielo… La fuente es quien lo quita al salir.
        /// </summary>
        public void AgregarModificador(object fuente, ModificadorMovimiento modificador)
        {
            if (fuente == null)
            {
                return;
            }

            QuitarSinRecalcular(fuente);
            _modificadores.Add(new KeyValuePair<object, ModificadorMovimiento>(fuente, modificador));
            Recalcular();
        }

        public void QuitarModificador(object fuente)
        {
            if (QuitarSinRecalcular(fuente))
            {
                Recalcular();
            }
        }

        private bool QuitarSinRecalcular(object fuente)
        {
            for (var i = _modificadores.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_modificadores[i].Key, fuente))
                {
                    _modificadores.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private void Recalcular()
        {
            float velocidad = 1f, gravedad = 1f, salto = 1f, aceleracion = 1f;
            var agua = false;

            foreach (var par in _modificadores)
            {
                var m = par.Value;
                velocidad *= m.velocidad;
                gravedad *= m.gravedad;
                salto *= m.salto;
                aceleracion *= m.aceleracion;
                agua |= m.esAgua;
            }

            MultVelocidad = velocidad;
            MultGravedad = gravedad;
            MultSalto = salto;
            MultAceleracion = aceleracion;

            if (agua != BajoElAgua)
            {
                BajoElAgua = agua;
                OnMedioAcuaticoCambiado?.Invoke(agua);
            }

            if (cuerpoRigido != null)
            {
                ActualizarEscalaGravedad();
            }
        }

        private void ActualizarEscalaGravedad()
        {
            if (cuerpoRigido == null)
            {
                return;
            }

            if (_gravedadSuspendida)
            {
                cuerpoRigido.gravityScale = 0f;
                return;
            }

            var caida = cuerpoRigido.linearVelocity.y < 0f ? _p.multiplicadorGravedadCaida : 1f;
            cuerpoRigido.gravityScale = _p.escalaGravedad * MultGravedad * caida;
        }
    }
}
