using Fragmentum.Core.Input;
using UnityEngine;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Fuente de input manejada por las pruebas. Implementa el mismo contrato que el teclado
    /// (<see cref="IFuenteInput"/>), así que el jugador no distingue una prueba de una partida:
    /// es el patrón Adapter del input usado para verificar la FSM de forma automática.
    ///
    /// Una pulsación queda pendiente hasta que el jugador la lee y dura exactamente ese frame,
    /// sin depender de en qué momento del frame la haga la prueba.
    /// </summary>
    public sealed class InputSimulado : IFuenteInput
    {
        private sealed class Boton
        {
            private bool _pendiente;
            private int _frame = -1;

            public bool Mantenido { get; set; }

            public void Pulsar(bool mantener)
            {
                _pendiente = true;
                Mantenido = mantener;
            }

            public bool PulsadoEsteFrame
            {
                get
                {
                    if (_pendiente)
                    {
                        _pendiente = false;
                        _frame = Time.frameCount;
                    }

                    return _frame == Time.frameCount;
                }
            }
        }

        private readonly Boton _salto = new Boton();
        private readonly Boton _ataque = new Boton();
        private readonly Boton _dash = new Boton();
        private readonly Boton _interactuar = new Boton();
        private readonly Boton _habilidad = new Boton();

        private float _tiempoSalto = float.NegativeInfinity;
        private bool _saltoConsumido = true;

        public bool Bloqueado { get; set; }

        public float EjeHorizontal { get; set; }

        public float EjeVertical { get; set; }

        public bool AgacharseMantenido { get; set; }

        public bool SaltoPulsado => !Bloqueado && _salto.PulsadoEsteFrame;

        public bool SaltoMantenido => !Bloqueado && _salto.Mantenido;

        public bool AtaquePulsado => !Bloqueado && _ataque.PulsadoEsteFrame;

        public bool AtaqueMantenido => !Bloqueado && _ataque.Mantenido;

        public bool DashPulsado => !Bloqueado && _dash.PulsadoEsteFrame;

        public bool InteractuarPulsado => !Bloqueado && _interactuar.PulsadoEsteFrame;

        public bool HabilidadPulsado => !Bloqueado && _habilidad.PulsadoEsteFrame;

        public void PulsarSalto(bool mantener = true)
        {
            _salto.Pulsar(mantener);
            _tiempoSalto = Time.time;
            _saltoConsumido = false;
        }

        public void SoltarSalto() => _salto.Mantenido = false;

        public void PulsarAtaque(bool mantener = false) => _ataque.Pulsar(mantener);

        public void SoltarAtaque() => _ataque.Mantenido = false;

        public void PulsarDash() => _dash.Pulsar(false);

        public void PulsarInteractuar() => _interactuar.Pulsar(false);

        public void PulsarHabilidad() => _habilidad.Pulsar(false);

        public bool HaySaltoEnBuffer(float ventana) =>
            !Bloqueado && !_saltoConsumido && Time.time - _tiempoSalto <= ventana;

        public void ConsumirSalto() => _saltoConsumido = true;
    }
}
