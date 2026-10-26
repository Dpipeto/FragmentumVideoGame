namespace Fragmentum.Enemies
{
    /// <summary>
    /// Lo que se vuelve más agresivo al recordar una memoria del mundo de la Culpa
    /// (<c>SistemaConsecuencias</c>), además de sus datos: el ritmo de su propia pieza (la recarga de
    /// un Artillero, los golpes del Verdugo).
    /// </summary>
    public interface IAgravable
    {
        /// <summary>Va <paramref name="factor"/> veces más deprisa (1,2: un 20 % más).</summary>
        void Agravar(float factor);
    }
}
