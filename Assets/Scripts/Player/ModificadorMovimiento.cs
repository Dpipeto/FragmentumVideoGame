using System;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Cambio temporal de la física del jugador que impone el entorno: el agua del mundo de la
    /// Tristeza, el hielo, etc. Las zonas lo añaden al entrar y lo quitan al salir; el motor
    /// multiplica todos los activos.
    ///
    /// Así «estar bajo el agua» es un modificador del movimiento y no una bandera nueva que
    /// cada estado tenga que consultar.
    /// </summary>
    [Serializable]
    public struct ModificadorMovimiento
    {
        [Tooltip("Multiplica la velocidad horizontal máxima.")]
        public float velocidad;

        [Tooltip("Multiplica la gravedad.")]
        public float gravedad;

        [Tooltip("Multiplica la velocidad de los saltos.")]
        public float salto;

        [Tooltip("Multiplica la aceleración y el frenado. Bajo = resbaladizo (hielo).")]
        public float aceleracion;

        [Tooltip("Marca el medio acuático: habilita el Dash Acuático y el oxígeno.")]
        public bool esAgua;

        public ModificadorMovimiento(float velocidad, float gravedad, float salto, float aceleracion, bool esAgua)
        {
            this.velocidad = velocidad;
            this.gravedad = gravedad;
            this.salto = salto;
            this.aceleracion = aceleracion;
            this.esAgua = esAgua;
        }

        /// <summary>Sin efecto.</summary>
        public static ModificadorMovimiento Neutro => new ModificadorMovimiento(1f, 1f, 1f, 1f, false);

        /// <summary>Bajo el agua (Anexo A, Nivel 2): gravedad ×0,3 y velocidad ×0,6.</summary>
        public static ModificadorMovimiento BajoElAgua => new ModificadorMovimiento(0.6f, 0.3f, 0.6f, 0.6f, true);

        /// <summary>Suelo de hielo (Nivel 2): misma velocidad, pero cuesta arrancar y frenar.</summary>
        public static ModificadorMovimiento Hielo => new ModificadorMovimiento(1f, 1f, 1f, 0.15f, false);

        /// <summary>
        /// Una maraña de raíces bajo el agua (la ciénaga del Nivel 3): sujeta. Kael va al 5 % de su
        /// velocidad y no salta; la gravedad es la del agua, que se suma. El dash fija su propia
        /// velocidad: es lo único que la cruza.
        /// </summary>
        public static ModificadorMovimiento Marana => new ModificadorMovimiento(0.05f, 1f, 0f, 1f, false);
    }
}
