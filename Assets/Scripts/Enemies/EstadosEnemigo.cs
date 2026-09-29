namespace Fragmentum.Enemies
{
    /// <summary>
    /// Estados de la FSM del enemigo. En el prototipo el <c>enum STATES</c> solo tenía IDLE y
    /// ATTACK, se asignaba una vez en Start y no volvía a cambiar.
    /// </summary>
    public enum ESTADOS_ENEMIGO
    {
        IDLE,
        PATROL,
        CHASE,
        ATTACK,
        DAMAGED,
        DEAD
    }
}
