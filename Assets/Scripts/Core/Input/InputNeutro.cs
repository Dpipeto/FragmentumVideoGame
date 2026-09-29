namespace Fragmentum.Core.Input
{
    /// <summary>
    /// Fuente de input que nunca pulsa nada (patrón Null Object). La usa el jugador cuando no
    /// encuentra ninguna fuente real, en lugar de comprobar <c>null</c> en cada estado: el
    /// juego sigue funcionando y el jugador simplemente se queda quieto.
    /// </summary>
    public sealed class InputNeutro : IFuenteInput
    {
        public static readonly InputNeutro Instancia = new InputNeutro();

        public bool Bloqueado { get; set; }

        public float EjeHorizontal => 0f;

        public float EjeVertical => 0f;

        public bool SaltoPulsado => false;

        public bool SaltoMantenido => false;

        public bool AtaquePulsado => false;

        public bool AtaqueMantenido => false;

        public bool DashPulsado => false;

        public bool AgacharseMantenido => false;

        public bool InteractuarPulsado => false;

        public bool HabilidadPulsado => false;

        public bool HaySaltoEnBuffer(float ventana) => false;

        public void ConsumirSalto()
        {
        }
    }
}
