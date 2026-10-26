using System;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// La maraña de raíces de la ciénaga (mundo del Miedo): raíces sumergidas que llenan un túnel
    /// del fondo al techo y sujetan a quien avanza despacio. «El miedo paraliza»: dentro, Kael
    /// casi no se mueve y no salta, y el aire se le acaba. Solo se cruza de golpe, con el Dash
    /// Acuático, que fija su propia velocidad sin que nada la frene.
    ///
    /// Lo que frena es su <see cref="ZonaModificadorMovimiento"/> (del tipo maraña,
    /// <see cref="ModificadorMovimiento.Marana"/>). Esta pieza la dibuja: se mece en reposo y se
    /// cierra mientras sujeta a Kael. Y avisa (<see cref="OnSujetaDemasiado"/>) cuando lo tiene
    /// sujeto <see cref="avisoTrasSujetar"/> s seguidos, para que la pista vuelva a sonar.
    ///
    /// No quita vida: quita aire.
    /// </summary>
    [RequireComponent(typeof(ZonaModificadorMovimiento))]
    public class Marana : MonoBehaviour
    {
        [Tooltip("Sus dibujos: uno por cada tramo de 2,5 u (la de 5 u lleva dos, el segundo en espejo).")]
        [SerializeField] private SpriteRenderer[] dibujos = new SpriteRenderer[0];

        [Tooltip("En reposo se mece: estos fotogramas, en bucle.")]
        [SerializeField] private Sprite[] reposo = new Sprite[0];

        [Tooltip("Mientras sujeta a Kael se cierra: estos fotogramas, y el último se queda.")]
        [SerializeField] private Sprite[] atrapa = new Sprite[0];

        [SerializeField, Min(0.02f)] private float segundosReposo = 0.6f;

        [SerializeField, Min(0.02f)] private float segundosAtrapa = 0.12f;

        [Tooltip("Segundos seguidos sujetando a Kael tras los que avisa (la pista vuelve a sonar).")]
        [SerializeField, Min(0f)] private float avisoTrasSujetar = 2f;

        private PlayerMotor _dentro;
        private float _desde;
        private bool _avisado;

        /// <summary>Ha sujetado a Kael <see cref="avisoTrasSujetar"/> s seguidos (una vez por cada vez que entra).</summary>
        public event Action OnSujetaDemasiado;

        /// <summary>¿Tiene a Kael dentro?</summary>
        public bool Sujetando => _dentro != null;

        /// <summary>Segundos seguidos que lleva sujetándolo (0 si no lo sujeta).</summary>
        public float SegundosSujetando => Sujetando ? Time.time - _desde : 0f;

        public SpriteRenderer[] Dibujos => dibujos;

        public float AvisoTrasSujetar => avisoTrasSujetar;

        private void OnTriggerEnter2D(Collider2D otro)
        {
            var motor = MotorDe(otro);
            if (motor == null || _dentro != null)
            {
                return;
            }

            _dentro = motor;
            _desde = Time.time;
            _avisado = false;
        }

        private void OnTriggerExit2D(Collider2D otro)
        {
            if (_dentro != null && MotorDe(otro) == _dentro)
            {
                _dentro = null;
            }
        }

        private void OnDisable() => _dentro = null;

        private void Update()
        {
            if (Sujetando && !_avisado && SegundosSujetando >= avisoTrasSujetar)
            {
                _avisado = true;
                OnSujetaDemasiado?.Invoke();
            }

            var fotograma = Fotograma();
            if (fotograma == null)
            {
                return;
            }

            foreach (var dibujo in dibujos)
            {
                if (dibujo != null && dibujo.sprite != fotograma)
                {
                    dibujo.sprite = fotograma;
                }
            }
        }

        // Sujetando, se cierra y se queda cerrada; si no, se mece.
        private Sprite Fotograma()
        {
            if (Sujetando && atrapa.Length > 0)
            {
                return atrapa[Mathf.Min((int)(SegundosSujetando / segundosAtrapa), atrapa.Length - 1)];
            }

            return reposo.Length > 0 ? reposo[(int)(Time.time / segundosReposo) % reposo.Length] : null;
        }

        // Solo el cuerpo del jugador, como el agua: la espada (trigger hijo) no cuenta.
        private static PlayerMotor MotorDe(Collider2D otro)
        {
            if (otro.isTrigger || otro.attachedRigidbody == null)
            {
                return null;
            }

            return otro.attachedRigidbody.GetComponent<PlayerMotor>();
        }
    }
}
