using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Une el Animator de un jefe con arte (la Centinela Ardiente) con su
    /// <see cref="BossController"/>. Va en el mismo objeto que el Animator porque los eventos de
    /// animación solo llegan a los scripts de ese objeto.
    ///
    /// - <b>Ataques:</b> los patrones piden un ataque diciendo cuánto debe tardar su golpe
    ///   (<see cref="Atacar"/>). Si es lo que dura la anticipación del dibujo, el clip empieza por
    ///   su disparador; si es menos (los espadazos encadenados), empieza ya avanzado, para que el
    ///   golpe llegue a tiempo sin tocar los demás fotogramas.
    /// - <b>Eventos:</b> reenvía al jefe los de los clips. <see cref="Golpe"/>,
    ///   <see cref="FinGolpe"/>, <see cref="Aterrizaje"/> y <see cref="FinMuerte"/> marcan los
    ///   momentos del combate; <see cref="Ventana"/> (C2) y <see cref="VentanaCorta"/> (E3) abren
    ///   las ventanas de daño doble.
    /// - <b>Salto:</b> <see cref="EnElAire"/> sostiene J4 hasta que el cuerpo toca el suelo. La
    ///   altura la pone el cuerpo y su vuelo dura más que J2-J4: así J5 llega con el aterrizaje.
    /// - <b>Andar:</b> el parámetro Walk sigue al cuerpo.
    /// - <b>Golpe recibido:</b> el sprite se vuelve una silueta blanca 0,1 s, como el Eco. Un tinte
    ///   blanco no se vería sobre el arte.
    ///
    /// La FSM del jefe sigue siendo la que decide: este componente no cambia de estado por su
    /// cuenta, solo le avisa de lo que pasa en el dibujo.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class AnimacionJefe : MonoBehaviour
    {
        /// <summary>Segundos desde que empieza el clip de un ataque hasta su evento Golpe.</summary>
        [Serializable]
        public struct Anticipacion
        {
            public string estado;

            [Min(0f)] public float segundos;
        }

        // Velocidad horizontal a partir de la cual anda.
        private const float UmbralAndar = 0.05f;

        [Tooltip("Anticipación de cada ataque (estado del Animator → segundos hasta su evento Golpe). La pone el constructor del arte.")]
        [SerializeField] private Anticipacion[] anticipaciones = new Anticipacion[0];

        [Header("Destello al recibir un golpe")]
        [Tooltip("Material de silueta (shader Fragmentum/SiluetaSprite). Vacío = sin destello.")]
        [SerializeField] private Material materialDestello;

        [SerializeField, Min(0f)] private float duracionDestello = 0.1f;

        private Animator _animator;
        private SpriteRenderer _sprite;
        private BossController _jefe;
        private Rigidbody2D _cuerpo;
        private Health _vida;
        private Material _materialNormal;
        private float _finDestello = float.NegativeInfinity;
        private bool _sosteniendo;
        private bool _muerto;

        /// <summary>Hay Animator con controlador: los eventos de los clips van a llegar.</summary>
        public bool Lista => isActiveAndEnabled && _animator != null && _animator.runtimeAnimatorController != null;

        /// <summary>El sprite se ve ahora como silueta blanca.</summary>
        public bool DestelloActivo => materialDestello != null && _sprite != null && _sprite.sharedMaterial == materialDestello;

        /// <summary>El sprite está arriba en el forcejeo de la espada clavada (C2).</summary>
        public bool Elevado => transform.localPosition.y > 0.001f;

        /// <summary>J4 se sostiene hasta que el cuerpo toque el suelo.</summary>
        public bool SosteniendoCaida => _sosteniendo;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _sprite = GetComponent<SpriteRenderer>();
            _jefe = GetComponentInParent<BossController>();
            _cuerpo = GetComponentInParent<Rigidbody2D>();
            _vida = GetComponentInParent<Health>();
            _materialNormal = _sprite.sharedMaterial;
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

        private void Update()
        {
            if (!Lista)
            {
                return;
            }

            if (_sosteniendo && (_jefe == null || _jefe.EnSuelo))
            {
                Soltar();
            }

            var anda = !_muerto && _cuerpo != null && Mathf.Abs(_cuerpo.linearVelocity.x) > UmbralAndar;
            _animator.SetBool(AnimatorParams.Walk.Hash, anda);
        }

        private void LateUpdate()
        {
            if (DestelloActivo && Time.time >= _finDestello)
            {
                _sprite.sharedMaterial = _materialNormal;
            }
        }

        // --- API para el jefe ---

        /// <summary>Segundos hasta el golpe del clip de un ataque, o -1 si este Animator no lo tiene.</summary>
        public float AnticipacionDe(in AnimatorParams.Parametro ataque)
        {
            foreach (var anticipacion in anticipaciones)
            {
                if (anticipacion.estado == ataque.Nombre)
                {
                    return anticipacion.segundos;
                }
            }

            return -1f;
        }

        /// <summary>
        /// Empieza un ataque para que su golpe llegue dentro de <paramref name="anticipacion"/>
        /// segundos. Con <paramref name="clavar"/>, el espadazo acaba con la espada clavada.
        /// Devuelve false si no hay clip para ese ataque.
        /// </summary>
        public bool Atacar(in AnimatorParams.Parametro ataque, float anticipacion, bool clavar)
        {
            var propia = AnticipacionDe(ataque);
            if (!Lista || _muerto || propia < 0f)
            {
                return false;
            }

            Soltar();
            _animator.SetBool(AnimatorParams.Clavar.Hash, clavar);
            var adelanto = propia - anticipacion;
            if (adelanto > 0.001f)
            {
                // Más rápido que el dibujo: el clip empieza ya avanzado y la anticipación se acorta.
                _animator.ResetTrigger(ataque.Hash);
                _animator.PlayInFixedTime(ataque.Nombre, 0, adelanto);
            }
            else
            {
                _animator.SetTrigger(ataque.Hash);
            }

            return true;
        }

        /// <summary>Ruge: interrumpe lo que esté haciendo (cambio de fase).</summary>
        public void Rugir()
        {
            if (!Lista || _muerto)
            {
                return;
            }

            Soltar();
            _animator.SetBool(AnimatorParams.Clavar.Hash, false);
            _animator.SetTrigger(AnimatorParams.Rugido.Hash);
        }

        /// <summary>Muere: D1-D3 y se queda en D4. Nada lo saca de ahí.</summary>
        public void Morir()
        {
            if (!Lista || _muerto)
            {
                return;
            }

            _muerto = true;
            Soltar();
            QuitarDestello();
            _animator.SetBool(AnimatorParams.Walk.Hash, false);
            _animator.SetBool(AnimatorParams.Death.Hash, true);
        }

        /// <summary>Vuelve al idle del principio (el jugador murió y el combate se reinicia).</summary>
        public void Reiniciar()
        {
            _muerto = false;
            Soltar();
            QuitarDestello();
            if (Lista)
            {
                _animator.Rebind();
                _animator.Update(0f);
            }
        }

        // --- Eventos de animación: los pone ConstructorCentinela en los clips ---

        /// <summary>El fotograma del golpe (E2, la onda de G3) o el despegue del salto (J2).</summary>
        public void Golpe() => Avisar(MOMENTOS_JEFE.GOLPE);

        /// <summary>Acaba el fotograma del golpe (empieza E3 o C1).</summary>
        public void FinGolpe() => Avisar(MOMENTOS_JEFE.FIN_GOLPE);

        /// <summary>E3, la recuperación del espadazo: ventana corta de daño doble.</summary>
        public void VentanaCorta(float segundos)
        {
            if (_jefe != null)
            {
                _jefe.AbrirVentanaCorta(segundos);
            }
        }

        /// <summary>C2, la espada clavada: la ventana vulnerable, lo que dura el fotograma.</summary>
        public void Ventana(float segundos)
        {
            if (_jefe != null)
            {
                _jefe.AbrirVentanaVulnerable(segundos);
            }
        }

        /// <summary>Final de J4: si el cuerpo aún vuela, J4 se sostiene hasta tocar el suelo.</summary>
        public void EnElAire()
        {
            if (_jefe == null || _jefe.EnSuelo)
            {
                return;
            }

            _sosteniendo = true;
            _animator.speed = 0f;
        }

        /// <summary>J5: toca el suelo.</summary>
        public void Aterrizaje() => Avisar(MOMENTOS_JEFE.ATERRIZAJE);

        /// <summary>D4, la ceniza con el fragmento: se queda.</summary>
        public void FinMuerte() => Avisar(MOMENTOS_JEFE.FIN_MUERTE);

        // --- Interno ---

        private void Avisar(MOMENTOS_JEFE momento)
        {
            if (_jefe != null)
            {
                _jefe.NotificarMomento(momento);
            }
        }

        private void Soltar()
        {
            if (!_sosteniendo)
            {
                return;
            }

            _sosteniendo = false;
            _animator.speed = 1f;
        }

        private void AlRecibirGolpe(Golpe golpe)
        {
            if (materialDestello == null || _muerto)
            {
                return;
            }

            _sprite.sharedMaterial = materialDestello;
            _finDestello = Time.time + duracionDestello;
        }

        private void QuitarDestello()
        {
            if (DestelloActivo)
            {
                _sprite.sharedMaterial = _materialNormal;
            }
        }
    }
}
