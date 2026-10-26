namespace Fragmentum.Bosses
{
    /// <summary>
    /// Estados de la FSM de los jefes (plan, §3.5). Las tres fases y la ventana vulnerable
    /// forman el superestado COMBATE.
    ///
    /// No hay estado DAMAGED a propósito: recibir un golpe solo hace parpadear al jefe y nunca
    /// interrumpe el ataque en curso, así que ningún jefe puede quedar congelado por un golpe.
    /// </summary>
    public enum ESTADOS_JEFE
    {
        PHASE_1,
        PHASE_2,
        PHASE_3,
        VULNERABLE,
        DEFEATED
    }

    /// <summary>
    /// Momentos del dibujo de un jefe con arte que marcan el combate. Los avisan los eventos de
    /// animación (<see cref="AnimacionJefe"/>) y los esperan los patrones: así el golpe sale
    /// cuando el dibujo golpea, no por un temporizador aparte.
    /// </summary>
    public enum MOMENTOS_JEFE
    {
        /// <summary>El fotograma del golpe (E2, la onda de G3) o el despegue del salto (J2).</summary>
        GOLPE,

        /// <summary>Acaba el fotograma del golpe: su zona de daño se apaga.</summary>
        FIN_GOLPE,

        /// <summary>Toca el suelo tras el salto (J5).</summary>
        ATERRIZAJE,

        /// <summary>Último fotograma de la muerte (D4), el que se queda.</summary>
        FIN_MUERTE
    }
}
