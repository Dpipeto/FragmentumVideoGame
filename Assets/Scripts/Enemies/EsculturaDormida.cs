using System;
using System.Collections;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Enemies
{
    public enum ESTADOS_ESCULTURA
    {
        DORMIDA,
        DESPERTANDO,
        DESPIERTA
    }

    /// <summary>
    /// La Escultura Viviente (mundo de la Tristeza) parece una estatua más del decorado. Mientras
    /// duerme, su <see cref="EnemyController"/> está apagado: ni ve, ni se gira, ni ataca, y
    /// tocarla no hace daño. Cuando Kael pasa cerca tarda <see cref="tiempoDespertar"/> en
    /// desperezarse (su clip de despertar, P1 a P3) y entonces se enciende el controlador. Con sus
    /// datos (velocidades 0 e hiperarmadura) ya no anda: se gira y descarga su golpe lento.
    ///
    /// Si la golpean dormida, despierta del todo en el acto. El controlador deja la vida al máximo
    /// al arrancar, así que lo que le quitaron dormida se le vuelve a quitar justo después: un
    /// golpe a la estatua cuenta.
    ///
    /// No toca <see cref="EnemyController"/>: solo lo enciende.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController), typeof(Health))]
    public class EsculturaDormida : MonoBehaviour
    {
        [Tooltip("Despierta cuando Kael está a esta distancia en horizontal...")]
        [Min(0f)] public float radioDespertar = 3.5f;

        [Tooltip("...y a esta en vertical.")]
        [Min(0f)] public float alturaDespertar = 2.5f;

        [Tooltip("Segundos que tarda en despertar (su clip). Mientras tanto no ataca.")]
        [Min(0f)] public float tiempoDespertar = 1f;

        [Tooltip("Sin arte: el color de la piedra dormida. Despierta, el tinte de sus datos.")]
        [SerializeField] private Color colorDormida = new Color(0.43f, 0.5f, 0.56f, 1f);

        [SerializeField] private SpriteRenderer sprite;

        [Tooltip("Sin arte se queda vacío. Con arte, el clip de despertar.")]
        [SerializeField] private Animator animator;

        private EnemyController _controlador;
        private Health _vida;
        private Transform _jugador;
        private float _proximaBusqueda;
        private float _finDespertar;

        public event Action<ESTADOS_ESCULTURA> OnCambio;

        public ESTADOS_ESCULTURA Estado { get; private set; } = ESTADOS_ESCULTURA.DORMIDA;

        public bool Despierta => Estado == ESTADOS_ESCULTURA.DESPIERTA;

        private void Awake()
        {
            _controlador = GetComponent<EnemyController>();
            _vida = GetComponent<Health>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            // Dormida no ve ni ataca. Su vida ya es la de sus datos, por si la golpean dormida.
            _controlador.enabled = false;
            var datos = _controlador.Datos;
            if (datos != null)
            {
                _vida.Inicializar(datos.vidaMaxima, datos.invulnerabilidad);
            }

            Pintar(0f);
        }

        private void OnEnable() => _vida.OnGolpeRecibido += AlRecibirGolpe;

        private void OnDisable() => _vida.OnGolpeRecibido -= AlRecibirGolpe;

        private void Update()
        {
            switch (Estado)
            {
                case ESTADOS_ESCULTURA.DORMIDA:
                    if (KaelCerca())
                    {
                        EmpezarADespertar();
                    }

                    break;

                case ESTADOS_ESCULTURA.DESPERTANDO:
                    var t = tiempoDespertar > 0f ? 1f - (_finDespertar - Time.time) / tiempoDespertar : 1f;
                    Pintar(Mathf.Clamp01(t));
                    if (Time.time >= _finDespertar)
                    {
                        Encender();
                    }

                    break;
            }
        }

        /// <summary>Despierta ya, sin desperezarse (la golpearon, o para las pruebas).</summary>
        public void DespertarYa() => Encender();

        private bool KaelCerca()
        {
            if (_jugador == null)
            {
                if (Time.time < _proximaBusqueda)
                {
                    return false;
                }

                _proximaBusqueda = Time.time + 0.5f;
                var objeto = GameObject.FindGameObjectWithTag(GameTags.Player);
                _jugador = objeto != null ? objeto.transform : null;
                if (_jugador == null)
                {
                    return false;
                }
            }

            var delta = (Vector2)(_jugador.position - transform.position);
            return Mathf.Abs(delta.x) <= radioDespertar && Mathf.Abs(delta.y) <= alturaDespertar;
        }

        private void EmpezarADespertar()
        {
            _finDespertar = Time.time + tiempoDespertar;
            Cambiar(ESTADOS_ESCULTURA.DESPERTANDO);
        }

        private void AlRecibirGolpe(Golpe golpe)
        {
            if (!Despierta)
            {
                Encender();
            }
        }

        private void Encender()
        {
            if (Despierta)
            {
                return;
            }

            Cambiar(ESTADOS_ESCULTURA.DESPIERTA);
            if (!_vida.EstaVivo)
            {
                // Rota antes de despertar: no se enciende (el controlador le devolvería la vida).
                foreach (var c in GetComponentsInChildren<Collider2D>())
                {
                    c.enabled = false;
                }

                Destroy(gameObject, 1f);
                return;
            }

            var perdida = _vida.VidaMaxima - _vida.Vida;
            _controlador.enabled = true;
            if (perdida > 0)
            {
                StartCoroutine(Reaplicar(perdida));
            }
        }

        // El Start del controlador (en el fotograma siguiente) deja la vida al máximo.
        private IEnumerator Reaplicar(int perdida)
        {
            yield return null;
            yield return null;
            _vida.PerderVida(perdida);
        }

        private void Cambiar(ESTADOS_ESCULTURA nuevo)
        {
            Estado = nuevo;
            if (animator != null && animator.isInitialized)
            {
                AnimatorParams.FijarEntero(animator, AnimatorParams.EstadoMecanica, (int)nuevo);
            }

            OnCambio?.Invoke(nuevo);
        }

        // Sin arte: de piedra gris al tinte de sus datos. Despierta, lo pinta el controlador.
        private void Pintar(float despertar)
        {
            if (sprite == null)
            {
                return;
            }

            var tinte = _controlador != null && _controlador.Datos != null ? _controlador.Datos.tinte : Color.white;
            sprite.color = Color.Lerp(colorDormida, tinte, despertar);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.75f, 0.9f, 0.6f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1.2f, new Vector3(radioDespertar * 2f, alturaDespertar * 2f, 0f));
        }
    }
}
