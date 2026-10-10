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

    /// <summary>
    /// El hielo rompible del mundo de la Tristeza. Cada valor será el del parámetro <c>Estado</c>
    /// de su Animator cuando tenga arte (Intacto, Agrietado y Roto).
    /// </summary>
    public enum ESTADOS_HIELO
    {
        /// <summary>Intacto: tapa el paso, con la grieta naranja.</summary>
        INTACT,

        /// <summary>Agrietado: un golpe normal lo resquebraja, pero sigue tapando el paso.</summary>
        CRACKED,

        /// <summary>Roto: el Ataque Cargado lo parte y deja el hueco abierto.</summary>
        BROKEN
    }

    /// <summary>
    /// Una franja del suelo de la arena de la Doncella del Lamento (<see cref="FranjaInundable"/>):
    /// se inunda y, en la segunda fase, el agua se congela. Cada valor será el del parámetro
    /// <c>Estado</c> de su Animator cuando tenga arte.
    /// </summary>
    public enum ESTADOS_FRANJA
    {
        /// <summary>Seca: el hielo de la arena, sin más.</summary>
        DRY,

        /// <summary>Aviso: sale agua por las grietas. Todavía no moja.</summary>
        WARNING,

        /// <summary>Inundada: el agua sube y se queda. No hace daño: es agua.</summary>
        FLOODED,

        /// <summary>El agua baja. Sigue mojando hasta que se va.</summary>
        DRAINING,

        /// <summary>La superficie se pone blanca: el aviso de que se congela. Sigue siendo agua.</summary>
        FREEZING,

        /// <summary>Congelada: brotan las púas en lo que estaba inundado y arriba queda la capa de hielo, que se pisa.</summary>
        FROZEN
    }
}
