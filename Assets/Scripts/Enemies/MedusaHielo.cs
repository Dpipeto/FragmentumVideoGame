using System;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Enemies
{
    /// <summary>
    /// La Medusa de Hielo (mundo de la Tristeza): flota sobre una línea con un seno, en vertical o
    /// en horizontal, y quema al tocarla. <b>No muere</b> (decidido el 2026-10-08): la espada solo
    /// la aparta y la encoge un segundo. Es la forma de cumplir «el melee es poco efectivo aquí»
    /// del Anexo A: no se vence, se atraviesa eligiendo el momento.
    ///
    /// No es un <see cref="EnemyController"/> porque no anda, no persigue ni tiene vida. Es un
    /// cuerpo cinemático en la capa Enemy (la espada la nota; Kael la atraviesa) con una hitbox de
    /// contacto hija que daña a Kael. Encogida, su contacto también encoge: es el hueco para pasar.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class MedusaHielo : MonoBehaviour, IGolpeable
    {
        [Header("Recorrido")]
        [Tooltip("Sube y baja (true) o va de lado a lado (false).")]
        public bool vertical = true;

        [Tooltip("Lo que se aleja de donde se colocó, en unidades.")]
        [Min(0f)] public float amplitud = 2.5f;

        [Tooltip("Segundos de una oscilación completa.")]
        [Min(0.5f)] public float periodo = 4f;

        [Tooltip("Segundos de adelanto en el ciclo: con medio periodo, dos medusas van a la contra.")]
        public float desfase;

        [Header("Al golpearla")]
        [Tooltip("Lo que la aparta un golpe, en unidades.")]
        [Min(0f)] public float empuje = 1.5f;

        [Tooltip("Segundos en volver a su línea.")]
        [Min(0.1f)] public float recuperacion = 1.2f;

        [Tooltip("Segundos que se queda encogida. Mientras tanto, otro golpe no la mueve.")]
        [Min(0f)] public float tiempoEncogida = 1f;

        [Range(0.3f, 1f)] public float escalaEncogida = 0.6f;

        [Tooltip("Lo que encoge: el dibujo y las zonas de golpe y de contacto.")]
        [SerializeField] private Transform cuerpo;

        private Rigidbody2D _rigido;
        private Vector2 _base;
        private float _inicio;
        private Vector2 _apartado;
        private float _finEncogida = float.NegativeInfinity;
        private Vector3 _escalaCuerpo = Vector3.one;

        /// <summary>La golpearon: se aparta y encoge.</summary>
        public event Action OnApartada;

        /// <summary>No muere nunca.</summary>
        public bool EstaVivo => true;

        public bool Encogida => Time.time < _finEncogida;

        /// <summary>Donde se colocó: el centro de su recorrido.</summary>
        public Vector2 Base => _base;

        /// <summary>Lo que la apartó el último golpe y aún no ha recuperado.</summary>
        public Vector2 Apartado => _apartado;

        public Transform Cuerpo => cuerpo;

        /// <summary>Dónde está sobre su línea en el segundo <paramref name="t"/> del ciclo.</summary>
        public static Vector2 Desplazamiento(float t, bool vertical, float amplitud, float periodo, float desfase)
        {
            var s = amplitud * Mathf.Sin(2f * Mathf.PI * (t + desfase) / Mathf.Max(0.01f, periodo));
            return vertical ? new Vector2(0f, s) : new Vector2(s, 0f);
        }

        private void Awake()
        {
            _rigido = GetComponent<Rigidbody2D>();
            _rigido.bodyType = RigidbodyType2D.Kinematic;
            _rigido.interpolation = RigidbodyInterpolation2D.Interpolate;
            _base = transform.position;
            _inicio = Time.time;
            if (cuerpo != null)
            {
                _escalaCuerpo = cuerpo.localScale;
            }
        }

        private void FixedUpdate()
        {
            var paso = empuje / recuperacion * Time.fixedDeltaTime;
            _apartado = Vector2.MoveTowards(_apartado, Vector2.zero, paso);
            var t = Time.time - _inicio;
            _rigido.MovePosition(_base + Desplazamiento(t, vertical, amplitud, periodo, desfase) + _apartado);
        }

        private void Update()
        {
            if (cuerpo == null)
            {
                return;
            }

            var objetivo = Encogida ? _escalaCuerpo * escalaEncogida : _escalaCuerpo;
            cuerpo.localScale = Vector3.MoveTowards(cuerpo.localScale, objetivo, 4f * Time.deltaTime);
        }

        public bool RecibirGolpe(in Golpe golpe)
        {
            if (Encogida)
            {
                return false;
            }

            var direccion = (Vector2)transform.position - golpe.Origen;
            if (direccion.sqrMagnitude < 1e-4f)
            {
                direccion = Vector2.right;
            }

            _apartado = Vector2.ClampMagnitude(_apartado + direccion.normalized * empuje, empuje);
            _finEncogida = Time.time + tiempoEncogida;
            OnApartada?.Invoke();
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            var centro = Application.isPlaying ? (Vector3)_base : transform.position;
            var eje = vertical ? Vector3.up : Vector3.right;
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.8f);
            Gizmos.DrawLine(centro + eje * amplitud, centro - eje * amplitud);
        }
    }
}
