using UnityEngine;

namespace Fragmentum.Core.Combate
{
    /// <summary>
    /// Un golpe que viaja de quien ataca a quien lo recibe. Se pasa por valor y lleva toda
    /// la información que necesita el receptor: cuánto daño, desde dónde (para el rebote),
    /// si venía cargado y quién lo lanzó.
    /// </summary>
    public readonly struct Golpe
    {
        public Golpe(int cantidad, Vector2 origen, GameObject atacante = null,
            bool cargado = false, bool ignoraInvulnerabilidad = false)
        {
            Cantidad = cantidad;
            Origen = origen;
            Atacante = atacante;
            Cargado = cargado;
            IgnoraInvulnerabilidad = ignoraInvulnerabilidad;
        }

        /// <summary>Daño en puntos de vida. Siempre entero: la vida del juego es entera.</summary>
        public int Cantidad { get; }

        /// <summary>Posición desde la que llega el golpe; define la dirección del rebote.</summary>
        public Vector2 Origen { get; }

        /// <summary>Quién golpeó. Puede ser nulo (lava, caída al vacío).</summary>
        public GameObject Atacante { get; }

        /// <summary>Golpe del ataque cargado (habilidad del mundo de la Ira).</summary>
        public bool Cargado { get; }

        /// <summary>
        /// Atraviesa los i-frames y los escudos. Lo usan las muertes instantáneas: lava,
        /// zona de caída y ahogamiento. Corrige el bug por el que caer al vacío mientras
        /// parpadeabas no mataba al jugador.
        /// </summary>
        public bool IgnoraInvulnerabilidad { get; }

        /// <summary>Golpe letal que no se puede bloquear ni esquivar.</summary>
        public static Golpe Letal(Vector2 origen, GameObject atacante = null) =>
            new Golpe(int.MaxValue / 2, origen, atacante, false, true);
    }
}
