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
}
