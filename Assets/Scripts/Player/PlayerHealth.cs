using System;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Vida del jugador. Hereda de <see cref="Health"/> los eventos que el encargo pedía como
    /// OnDamaged, OnHealed y OnDeath; aquí se llaman <c>OnGolpeRecibido</c>, <c>OnCurado</c> y
    /// <c>OnMuerte</c>, en español como el resto de la API pública.
    ///
    /// Saca la vida de <c>PlayerMovement</c>: en el prototipo el mismo script movía al
    /// jugador, restaba vida y llamaba directamente a <c>GameManager.Instance.GameOver()</c>.
    /// Ahora la barra de vida, la FSM del jugador y, más adelante, la FSM global se enteran
    /// por eventos, sin conocerse entre sí.
    ///
    /// Añade la <b>Defensa Reforzada</b> (fragmento de la Culpa): un escudo que absorbe un
    /// golpe cada 15 s, antes de que el golpe llegue a quitar vida o a provocar DAMAGED.
    /// </summary>
    public class PlayerHealth : Health
    {
        [Header("Defensa Reforzada")]
        [Tooltip("Segundos entre dos golpes absorbidos por el escudo.")]
        [SerializeField] private float cooldownEscudo = 15f;

        [Tooltip("Invulnerabilidad tras absorber un golpe, para que no se repita en el frame siguiente.")]
        [SerializeField] private float invulnerabilidadTrasEscudo = 0.5f;

        private float _ultimoEscudo = float.NegativeInfinity;

        /// <summary>El escudo absorbió un golpe. Lo escuchan el audio y los VFX.</summary>
        public event Action<Golpe> OnEscudoAbsorbio;

        /// <summary>El escudo vuelve a estar disponible (para la UI).</summary>
        public event Action OnEscudoRecargado;

        /// <summary>
        /// ¿Tiene el jugador la Defensa Reforzada? La activa <see cref="PlayerAbilities"/> al
        /// desbloquear la habilidad.
        /// </summary>
        public bool EscudoDesbloqueado { get; set; }

        public float CooldownEscudo => cooldownEscudo;

        /// <summary>¿Absorbería el siguiente golpe?</summary>
        public bool EscudoListo => EscudoDesbloqueado && Reloj() - _ultimoEscudo >= cooldownEscudo;

        private bool _avisadoRecarga = true;

        private void Update()
        {
            // Aviso de recarga para la UI, una vez por ciclo.
            if (!_avisadoRecarga && EscudoListo)
            {
                _avisadoRecarga = true;
                OnEscudoRecargado?.Invoke();
            }
        }

        protected override bool PuedeRecibir(in Golpe golpe)
        {
            if (!EscudoListo)
            {
                return true;
            }

            _ultimoEscudo = Reloj();
            _avisadoRecarga = false;
            ConcederInvulnerabilidad(invulnerabilidadTrasEscudo);
            OnEscudoAbsorbio?.Invoke(golpe);
            return false;
        }
    }
}
