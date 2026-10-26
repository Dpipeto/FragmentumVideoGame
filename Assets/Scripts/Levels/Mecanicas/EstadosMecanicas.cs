namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Ciclo de la torreta de fuego. Cada valor es el del parámetro <c>Estado</c> de su Animator
    /// (<c>Torreta.controller</c>), con un estado y un fotograma por valor: Reposo, Carga y
    /// Disparo. El tiempo de cada uno lo pone su script.
    /// </summary>
    public enum ESTADOS_TORRETA
    {
        /// <summary>Reposo: el hueco apagado.</summary>
        IDLE,

        /// <summary>Carga: el hueco se pone blanco. Es el aviso.</summary>
        CHARGE,

        /// <summary>Disparo: la llama sale. Es el único que hace daño.</summary>
        FIRE
    }

    /// <summary>
    /// La plataforma inestable. Cada valor es el del parámetro <c>Estado</c> de su Animator
    /// (<c>Plataforma.controller</c>): Intacta, Agrietada y Rompe.
    /// </summary>
    public enum ESTADOS_PLATAFORMA
    {
        /// <summary>Intacta: se puede pisar.</summary>
        INTACT,

        /// <summary>Agrietada: el aviso. Se sigue pudiendo pisar.</summary>
        CRACKED,

        /// <summary>Rompe: partida en tres trozos y después el polvo. Ya no se puede pisar.</summary>
        BROKEN
    }

    /// <summary>
    /// La plataforma de combustión. Cada valor es el del parámetro <c>Estado</c> de su Animator
    /// (<c>Combustion.controller</c>): Apagada, Calienta y Encendida. Nunca deja de poder pisarse.
    /// </summary>
    public enum ESTADOS_COMBUSTION
    {
        /// <summary>Apagada: se pisa sin más.</summary>
        OFF,

        /// <summary>Calienta: las brasas suben entre las rejas. Es el aviso.</summary>
        HEATING,

        /// <summary>Encendida: la llama. Es la única que hace daño.</summary>
        BURNING
    }
}
