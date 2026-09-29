using System;
using System.Collections;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// Arena del jefe (Anexo A: «sala cerrada»). Su collider es la entrada: cuando el jugador la
    /// cruza, cierra las puertas, activa al jefe con los límites de la arena, pone la música del
    /// jefe y el HUD muestra su barra de vida.
    ///
    /// - <b>Derrota del jefe:</b> el <see cref="LevelManager"/> otorga ya el fragmento y la
    ///   habilidad; tras una breve pausa se abren las puertas y aparece la salida del nivel.
    /// - <b>Muerte del jugador:</b> al reaparecer en el checkpoint, el combate vuelve a empezar: el
    ///   jefe recupera la vida y su sitio, y las puertas se abren.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ArenaJefe : MonoBehaviour
    {
        [Tooltip("Vacío = el jefe que cuelga de la arena.")]
        [SerializeField] private BossController jefe;

        [Tooltip("Muros que cierran la arena durante el combate. Fuera del combate están desactivados.")]
        [SerializeField] private GameObject[] puertas = new GameObject[0];

        [Tooltip("Bordes horizontales de la arena (mínimo, máximo): el jefe no los cruza y sus ataques tampoco.")]
        [SerializeField] private Vector2 limites = new Vector2(55f, 80f);

        [Tooltip("Segundos entre la derrota del jefe y la apertura de la arena.")]
        [SerializeField] private float esperaTrasDerrota = 2f;

        private LevelManager _nivel;

        /// <summary>Empezó el combate.</summary>
        public event Action OnCombateIniciado;

        /// <summary>El jefe está derrotado y la arena, abierta.</summary>
        public event Action OnArenaSuperada;

        public BossController Jefe => jefe;

        public bool EnCombate { get; private set; }

        /// <summary>El jefe ya cayó (aunque las puertas se abran un poco después).</summary>
        public bool JefeDerrotado { get; private set; }

        /// <summary>La arena está abierta tras la victoria.</summary>
        public bool Superada { get; private set; }

        public Vector2 Limites => limites;

        /// <summary>¿Están cerradas las puertas? (Solo durante el combate.)</summary>
        public bool PuertasCerradas
        {
            get
            {
                foreach (var puerta in puertas)
                {
                    if (puerta != null && puerta.activeSelf)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void Awake()
        {
            if (jefe == null)
            {
                jefe = GetComponentInChildren<BossController>(true);
            }

            FijarPuertas(false);
        }

        private void Start()
        {
            _nivel = FindAnyObjectByType<LevelManager>();
            if (_nivel != null)
            {
                _nivel.OnJugadorReaparecio += AlReaparecerJugador;
            }

            if (jefe != null)
            {
                jefe.OnJefeDerrotado += AlDerrotarJefe;
            }
            else
            {
                Debug.LogWarning("[Arena] No hay jefe en la arena: no habrá combate. Añade el prefab del jefe como hijo de la arena.", this);
            }
        }

        private void OnDestroy()
        {
            if (_nivel != null)
            {
                _nivel.OnJugadorReaparecio -= AlReaparecerJugador;
            }

            if (jefe != null)
            {
                jefe.OnJefeDerrotado -= AlDerrotarJefe;
            }

            if (EnCombate)
            {
                EventosJuego.NotificarJefeFueraDeCombate();
            }
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            if (EnCombate || JefeDerrotado || jefe == null || otro.isTrigger || otro.attachedRigidbody == null)
            {
                return;
            }

            var jugador = otro.attachedRigidbody.GetComponent<PlayerController>();
            if (jugador != null && jugador.EstaVivo)
            {
                IniciarCombate(jugador);
            }
        }

        /// <summary>Cierra la arena y activa al jefe (también lo usan las pruebas).</summary>
        public void IniciarCombate(PlayerController jugador)
        {
            if (EnCombate || JefeDerrotado || jefe == null)
            {
                return;
            }

            EnCombate = true;
            FijarPuertas(true);
            jefe.Activar(jugador != null ? jugador.transform : null, limites);
            if (_nivel != null)
            {
                _nivel.MusicaDeJefe(true);
            }

            EventosJuego.NotificarJefeEnCombate(jefe.Datos != null ? jefe.Datos.nombreCanonico : jefe.name, jefe.Vida);
            OnCombateIniciado?.Invoke();
        }

        private void AlDerrotarJefe(BossController derrotado)
        {
            EnCombate = false;
            JefeDerrotado = true;
            EventosJuego.NotificarJefeFueraDeCombate();
            if (_nivel != null)
            {
                _nivel.MusicaDeJefe(false);
                _nivel.JefeDerrotado(derrotado);
            }

            StartCoroutine(AbrirTrasDerrota());
        }

        private IEnumerator AbrirTrasDerrota()
        {
            yield return new WaitForSeconds(esperaTrasDerrota);
            FijarPuertas(false);
            Superada = true;
            OnArenaSuperada?.Invoke();
        }

        // El jugador murió en la arena y reaparece en el checkpoint: el combate vuelve a empezar.
        private void AlReaparecerJugador(PlayerController jugador)
        {
            if (!EnCombate)
            {
                return;
            }

            EnCombate = false;
            jefe.Reiniciar();
            FijarPuertas(false);
            if (_nivel != null)
            {
                _nivel.MusicaDeJefe(false);
            }

            EventosJuego.NotificarJefeFueraDeCombate();
        }

        private void FijarPuertas(bool cerradas)
        {
            foreach (var puerta in puertas)
            {
                if (puerta != null)
                {
                    puerta.SetActive(cerradas);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.5f);
            Gizmos.DrawWireCube(new Vector3((limites.x + limites.y) * 0.5f, 7.5f, 0f), new Vector3(limites.y - limites.x, 15f, 0f));
        }
    }
}
