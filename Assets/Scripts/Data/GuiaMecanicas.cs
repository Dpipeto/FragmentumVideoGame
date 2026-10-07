using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Data
{
    /// <summary>Cuándo aparece un movimiento en la guía de mecánicas.</summary>
    public enum REQUISITOS_MECANICA
    {
        /// <summary>Kael lo sabe hacer desde el principio.</summary>
        SIEMPRE,

        /// <summary>Solo sirve en los mundos que lo permiten (cobertura, mundo de la Culpa): aparece al desbloquear uno.</summary>
        MUNDO_CON_AGACHARSE
    }

    /// <summary>Un movimiento de Kael tal como lo explica la guía.</summary>
    [Serializable]
    public class MecanicaGuia
    {
        public string nombre;

        [Tooltip("Teclas. Las acciones van entre llaves y se leen del asset de input: {Jump} → ESPACIO. " +
                 "Con una barra se eligen las partes de un compuesto: {Move/left,right} → A D / ← →.")]
        public string controles;

        [TextArea(2, 4)] public string descripcion;

        public REQUISITOS_MECANICA requisito;

        public MecanicaGuia()
        {
        }

        public MecanicaGuia(string nombre, string controles, string descripcion, REQUISITOS_MECANICA requisito = REQUISITOS_MECANICA.SIEMPRE)
        {
            this.nombre = nombre;
            this.controles = controles;
            this.descripcion = descripcion;
            this.requisito = requisito;
        }
    }

    /// <summary>
    /// Textos de la pantalla «Mecánicas»: los movimientos que Kael sabe hacer sin fragmentos.
    /// Las habilidades que se desbloquean no van aquí: la pantalla las lee de cada
    /// <see cref="HabilidadData"/>, en el orden de los mundos, y las va mostrando según se
    /// integran.
    ///
    /// Las teclas no se escriben a mano: cada movimiento nombra sus acciones del Input System y la
    /// pantalla escribe las teclas que tengan asignadas, así que la guía no se queda atrás si
    /// cambian.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Guía de mecánicas", fileName = "GuiaMecanicas")]
    public class GuiaMecanicas : ScriptableObject
    {
        public List<MecanicaGuia> movimientos = new List<MecanicaGuia>();

        /// <summary>
        /// Los movimientos del diseño. Los usa el generador para crear el asset y la pantalla si
        /// falta (construir para ausencia).
        /// </summary>
        public static List<MecanicaGuia> MovimientosPorDefecto() => new List<MecanicaGuia>
        {
            new MecanicaGuia("Moverse", "{Move/left,right}",
                "Corre a izquierda y derecha."),
            new MecanicaGuia("Caminar", "{Walk}",
                "Mantén mientras te mueves para ir despacio y sin ruido: en el mundo del Miedo, así no te oyen."),
            new MecanicaGuia("Saltar", "{Jump}",
                "Mantén para saltar más alto; suelta pronto para un salto corto."),
            new MecanicaGuia("Doble salto", "{Jump} ×2",
                "Un segundo salto en el aire. Se recupera al tocar el suelo o una pared."),
            new MecanicaGuia("Pared", "{Jump}",
                "Cayendo junto a una pared, empuja hacia ella para deslizarte despacio. Salta para impulsarte hacia el otro lado."),
            new MecanicaGuia("Dash", "{Sprint}",
                "Un impulso corto y rápido. En el aire, uno por salto; se recupera al tocar el suelo o una pared."),
            new MecanicaGuia("Atacar", "{Attack}",
                "Un tajo de espada hacia delante."),
            new MecanicaGuia("Agacharse", "{Crouch}",
                "Mantén para cubrirte tras las coberturas y avanzar despacio. Solo en el mundo de la Culpa.",
                REQUISITOS_MECANICA.MUNDO_CON_AGACHARSE),
        };

        /// <summary>Una guía en memoria con los movimientos del diseño (sin asset).</summary>
        public static GuiaMecanicas PorDefecto()
        {
            var guia = CreateInstance<GuiaMecanicas>();
            guia.name = "GuiaMecanicas (por defecto)";
            guia.hideFlags = HideFlags.DontSave;
            guia.movimientos = MovimientosPorDefecto();
            return guia;
        }
    }
}
