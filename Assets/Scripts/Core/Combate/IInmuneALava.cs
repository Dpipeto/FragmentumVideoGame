namespace Fragmentum.Core.Combate
{
    /// <summary>
    /// Algo que la lava no quema (el Golem de Ceniza, hecho de ceniza y magma). La lava lo
    /// consulta antes de enviar su golpe letal; caer al vacío sigue matándolo.
    /// </summary>
    public interface IInmuneALava
    {
        bool InmuneALava { get; }
    }
}
