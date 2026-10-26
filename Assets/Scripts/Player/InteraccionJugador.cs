using System;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Interactuar (W): Kael usa lo que tenga al alcance de los pies (<see cref="Interacciones"/>).
    /// En el mundo del Miedo, encender una antorcha.
    ///
    /// Lo que se puede usar no lee la tecla: se registra y espera a que Kael lo use. Así la tecla
    /// la lee un solo sitio, y el HUD sabe sobre qué enseñar el aviso (<see cref="Candidato"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class InteraccionJugador : MonoBehaviour
    {
        [SerializeField] private PlayerController jugador;

        /// <summary>Lo que Kael usaría ahora si pulsara Interactuar, o null.</summary>
        public IInteractuable Candidato { get; private set; }

        /// <summary>Kael usó algo.</summary>
        public event Action<IInteractuable> OnInteraccion;

        private void Awake()
        {
            if (jugador == null)
            {
                jugador = GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            if (jugador == null)
            {
                return;
            }

            // La tecla se lee en cada fotograma, haya algo o no: una pulsación lejos de todo no se
            // guarda para cuando Kael llegue.
            var pulsado = jugador.FuenteInput.InteractuarPulsado;
            Candidato = jugador.EstaVivo ? Interacciones.AlAlcance(transform.position) : null;
            if (pulsado && Candidato != null)
            {
                var usado = Candidato;
                usado.Interactuar(gameObject);
                OnInteraccion?.Invoke(usado);
                Candidato = Interacciones.AlAlcance(transform.position);
            }
        }

        private void OnDisable() => Candidato = null;
    }
}
