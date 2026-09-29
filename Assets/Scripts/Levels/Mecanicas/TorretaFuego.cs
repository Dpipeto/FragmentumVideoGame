using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Torreta de fuego fija (Anexo A, sección 1 del mundo de la Ira): cuando el jugador está a
    /// su alcance, avisa con un destello y le dispara una bola de fuego cada pocos segundos. No se
    /// mueve ni se puede destruir; se esquiva saltando o agachándose.
    ///
    /// Dispara en horizontal hacia el lado del jugador (por defecto) o directamente hacia él.
    /// </summary>
    public class TorretaFuego : MonoBehaviour
    {
        [Tooltip("Distancia a la que empieza a disparar.")]
        [Min(0f)]
        [SerializeField] private float alcance = 10f;

        [Tooltip("Segundos entre disparos.")]
        [Min(0.2f)]
        [SerializeField] private float intervalo = 2.2f;

        [Tooltip("Destello de aviso antes de cada disparo.")]
        [Min(0f)]
        [SerializeField] private float aviso = 0.5f;

        [Min(0.1f)]
        [SerializeField] private float velocidadProyectil = 7f;

        [Min(1)]
        [SerializeField] private int danio = 1;

        [Tooltip("Solo en horizontal (se esquiva saltando); si no, apunta directamente al jugador.")]
        [SerializeField] private bool soloHorizontal = true;

        [Tooltip("De dónde salen los disparos. Vacío = el centro de la torreta.")]
        [SerializeField] private Transform boca;

        [SerializeField] private SpriteRenderer visual;

        [SerializeField] private Color colorAviso = new Color(1f, 0.85f, 0.3f);

        private Transform _jugador;
        private Health _vidaJugador;
        private Color _colorBase = Color.white;
        private float _proximoDisparo = -1f;
        private float _proximaBusqueda;

        /// <summary>Disparó un proyectil.</summary>
        public event Action<Proyectil> OnDisparo;

        public int Disparos { get; private set; }

        public float Alcance => alcance;

        private Vector2 Boca => boca != null ? (Vector2)boca.position : (Vector2)transform.position;

        private void Awake()
        {
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            if (visual != null)
            {
                _colorBase = visual.color;
            }
        }

        private void Update()
        {
            if (_vidaJugador == null && Time.time >= _proximaBusqueda)
            {
                BuscarJugador();
            }

            var enAlcance = _vidaJugador != null && _vidaJugador.EstaVivo &&
                            Vector2.Distance(_jugador.position, Boca) <= alcance;
            if (!enAlcance)
            {
                _proximoDisparo = -1f;
                Pintar(false);
                return;
            }

            // Al entrar en su alcance, el primer disparo llega tras el aviso, no al instante.
            if (_proximoDisparo < 0f)
            {
                _proximoDisparo = Time.time + aviso;
            }

            Pintar(Time.time >= _proximoDisparo - aviso && Mathf.Repeat(Time.time * 10f, 1f) < 0.5f);
            if (Time.time >= _proximoDisparo)
            {
                Disparar();
                _proximoDisparo = Time.time + intervalo;
            }
        }

        private void Disparar()
        {
            var objetivo = (Vector2)_jugador.position + Vector2.up * 0.9f;
            var direccion = soloHorizontal
                ? new Vector2(Mathf.Sign(objetivo.x - Boca.x), 0f)
                : (objetivo - Boca).normalized;

            var proyectil = Proyectil.Disparar($"{name}_BolaDeFuego", Boca, direccion * velocidadProyectil, 0.3f, danio,
                Capas.MascaraJugador, alcance * 1.5f, new Color(1f, 0.45f, 0.05f));
            Disparos++;
            OnDisparo?.Invoke(proyectil);
        }

        private void BuscarJugador()
        {
            _proximaBusqueda = Time.time + 1f;
            var jugador = PlayerController.BuscarEnEscena(gameObject.scene);
            if (jugador != null)
            {
                _jugador = jugador.transform;
                _vidaJugador = jugador.Vida;
            }
        }

        private void Pintar(bool avisando)
        {
            if (visual != null)
            {
                visual.color = avisando ? colorAviso : _colorBase;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.45f, 0.05f, 0.5f);
            Gizmos.DrawWireSphere(Boca, alcance);
        }
    }
}
