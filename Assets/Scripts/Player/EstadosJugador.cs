namespace Fragmentum.Player
{
    /// <summary>
    /// Estados de la FSM principal del jugador. Sustituye a las banderas booleanas del
    /// prototipo (attack, death, takingDamage, isWallSliding, isWallJumping, canDoubleJump).
    /// </summary>
    public enum ESTADOS_JUGADOR
    {
        // Locomoción (superestado LOCOMOCION)
        IDLE,
        RUN,
        CROUCH,
        JUMP,
        DOUBLE_JUMP,
        FALL,
        WALL_SLIDE,
        WALL_JUMP,

        // Acciones
        DASH,
        ATTACK,

        // Daño y muerte
        DAMAGED,
        DEAD
    }

    /// <summary>
    /// Región ortogonal del jugador: cómo de rápido se mueve. Corre en paralelo a la FSM
    /// principal y solo sirve para el sigilo del mundo del Miedo, donde correr hace ruido.
    /// </summary>
    public enum MODOS_MOVIMIENTO
    {
        NORMAL,
        STEALTH,
        RUN
    }
}
