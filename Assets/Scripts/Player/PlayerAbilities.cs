using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Habilidades que se ganan al integrar cada fragmento de identidad (una por mundo).
    /// El identificador es estable porque se guardará en el archivo de progreso (Fase 5).
    /// </summary>
    public enum HABILIDADES
    {
        /// <summary>Ira: mantener el ataque y soltarlo cargado. Detona barriles explosivos.</summary>
        ATAQUE_CARGADO,

        /// <summary>Tristeza: el dash funciona bajo el agua.</summary>
        DASH_ACUATICO,

        /// <summary>Miedo: 3 s de invisibilidad parcial, con 8 s de espera.</summary>
        INVISIBILIDAD_PARCIAL,

        /// <summary>Culpa: escudo que absorbe un golpe cada 15 s.</summary>
        DEFENSA_REFORZADA
    }

    /// <summary>
    /// Habilidades desbloqueables del jugador. No son scripts separados ni estados nuevos:
    /// son <b>modificadores</b> que los estados existentes consultan.
    /// - Ataque Cargado: ATTACK sale cargado si se soltó una carga completa.
    /// - Dash Acuático: DASH se permite dentro del agua.
    /// - Invisibilidad Parcial: temporizador propio (este componente).
    /// - Defensa Reforzada: escudo en <see cref="PlayerHealth"/>.
    ///
    /// Hasta la Fase 5 las habilidades de partida se eligen en el Inspector; después las
    /// fijará el progreso guardado con <see cref="FijarDesbloqueadas"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerAbilities : MonoBehaviour
    {
        [Tooltip("Habilidades disponibles al empezar. Sirve para probar; el progreso guardado las sustituye desde la Fase 5.")]
        [SerializeField] private List<HABILIDADES> desbloqueadasAlInicio = new List<HABILIDADES>();

        [SerializeField] private PlayerHealth vida;

        [Header("Ataque Cargado (Ira)")]
        [Tooltip("Segundos que hay que mantener el ataque para que la carga esté completa.")]
        [SerializeField] private float tiempoCarga = 0.6f;

        [Tooltip("Multiplicador de daño del ataque cargado.")]
        [SerializeField] private int multiplicadorDanioCargado = 2;

        [Tooltip("Segundos durante los que una carga soltada espera a que el jugador pueda atacar.")]
        [SerializeField] private float ventanaCargaSoltada = 0.25f;

        [Header("Invisibilidad Parcial (Miedo)")]
        [SerializeField] private float duracionInvisibilidad = 3f;
        [SerializeField] private float cooldownInvisibilidad = 8f;

        private readonly HashSet<HABILIDADES> _desbloqueadas = new HashSet<HABILIDADES>();
        private Func<float> _reloj;

        private bool _cargando;
        private float _inicioCarga;
        private float _cargaSoltadaHasta = float.NegativeInfinity;
        private bool _cargaCompletaAvisada;

        private float _finInvisibilidad = float.NegativeInfinity;
        private float _proximaInvisibilidad = float.NegativeInfinity;
        private bool _eraInvisible;

        public event Action<HABILIDADES> OnHabilidadDesbloqueada;

        /// <summary>Empieza o termina la invisibilidad: (invisible).</summary>
        public event Action<bool> OnInvisibilidadCambiada;

        /// <summary>La carga llegó al máximo (para el tinte, el sonido y la UI).</summary>
        public event Action OnCargaCompleta;

        /// <summary>Fuente de tiempo; las pruebas la sustituyen.</summary>
        public Func<float> Reloj
        {
            get => _reloj ?? (_reloj = () => Time.time);
            set => _reloj = value ?? (() => Time.time);
        }

        public int MultiplicadorDanioCargado => Mathf.Max(1, multiplicadorDanioCargado);

        public IReadOnlyCollection<HABILIDADES> Desbloqueadas => _desbloqueadas;

        // Perezosa: en las pruebas EditMode Unity no llama a Awake.
        private PlayerHealth Vida => vida != null ? vida : (vida = GetComponent<PlayerHealth>());

        private void Awake()
        {
            FijarDesbloqueadas(desbloqueadasAlInicio);
        }

        private void Update()
        {
            var invisible = EsInvisible;
            if (invisible != _eraInvisible)
            {
                _eraInvisible = invisible;
                OnInvisibilidadCambiada?.Invoke(invisible);
            }
        }

        // --- Desbloqueo ---

        public bool Tiene(HABILIDADES habilidad) => _desbloqueadas.Contains(habilidad);

        public void Desbloquear(HABILIDADES habilidad)
        {
            if (!_desbloqueadas.Add(habilidad))
            {
                return;
            }

            AplicarEfectosPermanentes();
            OnHabilidadDesbloqueada?.Invoke(habilidad);
        }

        /// <summary>Sustituye el conjunto completo (al cargar una partida).</summary>
        public void FijarDesbloqueadas(IEnumerable<HABILIDADES> habilidades)
        {
            _desbloqueadas.Clear();
            if (habilidades != null)
            {
                foreach (var h in habilidades)
                {
                    _desbloqueadas.Add(h);
                }
            }

            AplicarEfectosPermanentes();
        }

        private void AplicarEfectosPermanentes()
        {
            var salud = Vida;
            if (salud != null)
            {
                salud.EscudoDesbloqueado = Tiene(HABILIDADES.DEFENSA_REFORZADA);
            }
        }

        // --- Ataque Cargado ---

        /// <summary>¿Se está manteniendo el ataque con la carga ya completa?</summary>
        public bool CargaCompleta => _cargando && Reloj() - _inicioCarga >= tiempoCarga;

        /// <summary>Hay una carga completa soltada que espera convertirse en ataque.</summary>
        public bool HayAtaqueCargadoPendiente => Reloj() <= _cargaSoltadaHasta;

        /// <summary>
        /// Sigue la pulsación del ataque. La llama el jugador cada frame con el estado del botón.
        /// Pulsar ataca al instante (ataque normal); mantener carga; soltar con la carga
        /// completa deja pendiente un ataque cargado.
        /// </summary>
        public void ActualizarCarga(bool pulsado, bool mantenido)
        {
            if (!Tiene(HABILIDADES.ATAQUE_CARGADO))
            {
                _cargando = false;
                return;
            }

            if (pulsado)
            {
                _cargando = true;
                _inicioCarga = Reloj();
                _cargaCompletaAvisada = false;
                return;
            }

            if (!_cargando)
            {
                return;
            }

            if (mantenido)
            {
                if (!_cargaCompletaAvisada && CargaCompleta)
                {
                    _cargaCompletaAvisada = true;
                    OnCargaCompleta?.Invoke();
                }

                return;
            }

            // Se soltó el botón.
            if (Reloj() - _inicioCarga >= tiempoCarga)
            {
                _cargaSoltadaHasta = Reloj() + ventanaCargaSoltada;
            }

            _cargando = false;
        }

        /// <summary>Devuelve si hay un ataque cargado pendiente y lo gasta.</summary>
        public bool ConsumirAtaqueCargado()
        {
            if (!HayAtaqueCargadoPendiente)
            {
                return false;
            }

            _cargaSoltadaHasta = float.NegativeInfinity;
            return true;
        }

        /// <summary>Cancela la carga en curso (al recibir daño o al morir).</summary>
        public void CancelarCarga()
        {
            _cargando = false;
            _cargaSoltadaHasta = float.NegativeInfinity;
        }

        // --- Invisibilidad Parcial ---

        public bool EsInvisible => Reloj() < _finInvisibilidad;

        public bool InvisibilidadLista => Tiene(HABILIDADES.INVISIBILIDAD_PARCIAL) && Reloj() >= _proximaInvisibilidad;

        /// <summary>Segundos de espera restantes (0 si está lista), para la UI.</summary>
        public float EsperaInvisibilidad => Mathf.Max(0f, _proximaInvisibilidad - Reloj());

        /// <summary>Activa la invisibilidad si está desbloqueada y lista. Devuelve si se activó.</summary>
        public bool ActivarInvisibilidad()
        {
            if (!InvisibilidadLista)
            {
                return false;
            }

            var ahora = Reloj();
            _finInvisibilidad = ahora + duracionInvisibilidad;
            _proximaInvisibilidad = ahora + duracionInvisibilidad + cooldownInvisibilidad;
            return true;
        }
    }
}
