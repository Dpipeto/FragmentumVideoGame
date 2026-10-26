namespace Fragmentum.Core.Input
{
    /// <summary>
    /// Fuente de input del jugador (patrón Adapter). La FSM del jugador solo conoce esta
    /// interfaz, así que cambiar de sistema de entrada, añadir mando o rebindear teclas no
    /// toca la lógica de juego.
    ///
    /// La implementación de este proyecto es <c>FuenteInputJugador</c>, sobre el Input System,
    /// que es el único sistema de entrada activo en Tesis2.0.
    /// </summary>
    public interface IFuenteInput
    {
        /// <summary>
        /// Mientras esté bloqueado, todo devuelve neutro. Lo activa la FSM global en PAUSED,
        /// CUTSCENE, GAME_OVER y durante las cargas.
        /// </summary>
        bool Bloqueado { get; set; }

        /// <summary>Movimiento horizontal, de -1 a 1.</summary>
        float EjeHorizontal { get; }

        /// <summary>Movimiento vertical, de -1 a 1 (nadar y navegar menús).</summary>
        float EjeVertical { get; }

        /// <summary>Se pulsó saltar en este frame.</summary>
        bool SaltoPulsado { get; }

        /// <summary>Se mantiene saltar (salto de altura variable).</summary>
        bool SaltoMantenido { get; }

        /// <summary>Se pulsó atacar en este frame.</summary>
        bool AtaquePulsado { get; }

        /// <summary>Se mantiene atacar (carga del Ataque Cargado).</summary>
        bool AtaqueMantenido { get; }

        /// <summary>Se pulsó el dash en este frame (Shift izquierdo).</summary>
        bool DashPulsado { get; }

        /// <summary>Se mantiene agacharse (cobertura del mundo de la Culpa).</summary>
        bool AgacharseMantenido { get; }

        /// <summary>Se pulsó interactuar (antorchas y memorias, tecla W).</summary>
        bool InteractuarPulsado { get; }

        /// <summary>Se pulsó la habilidad activa (Invisibilidad Parcial, tecla Q).</summary>
        bool HabilidadPulsado { get; }

        /// <summary>
        /// ¿Hay un salto pulsado hace menos de <paramref name="ventana"/> segundos y sin consumir?
        /// Es el jump buffer: pulsar justo antes de aterrizar también salta.
        /// </summary>
        bool HaySaltoEnBuffer(float ventana);

        /// <summary>Marca el salto del buffer como usado, para que no se gaste dos veces.</summary>
        void ConsumirSalto();
    }
}
