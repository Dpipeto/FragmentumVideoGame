namespace Fragmentum.Core.Combate
{
    /// <summary>
    /// Todo lo que puede recibir daño: jugador, enemigos, jefes, barriles explosivos y cajas.
    ///
    /// Sustituye al reparto de daño por tag ("Weapon"), que hacía que el arma de un enemigo
    /// dañara a otros enemigos y rompiera cajas.
    /// </summary>
    public interface IGolpeable
    {
        /// <summary>¿Sigue vivo o intacto?</summary>
        bool EstaVivo { get; }

        /// <summary>
        /// Aplica un golpe. Devuelve <c>true</c> solo si el golpe surtió efecto, para que
        /// quien ataca sepa si debe reproducir el impacto (sonido, partículas, retroceso).
        /// </summary>
        bool RecibirGolpe(in Golpe golpe);
    }
}
