using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Narrative
{
    /// <summary>Cuándo suena una línea dentro del nivel (plan, §6.1).</summary>
    public enum MOMENTOS_DIALOGO
    {
        APERTURA,
        ECO_MEMORIA,
        PRE_JEFE,
        CIERRE,

        /// <summary>Lo que Kael se dice ante un obstáculo que pide una habilidad (el tapón de hielo).</summary>
        PISTA,

        /// <summary>La segunda pista de un mundo: ante la tierra de nadie de la Culpa, la Invisibilidad.</summary>
        PISTA_INVISIBILIDAD,

        /// <summary>Lo que Kael dice en la orilla de un agua que se cruza buceando: el río de la Culpa.</summary>
        ORILLA
    }

    /// <summary>Una línea de diálogo interno: quién habla, qué dice y cuándo.</summary>
    [Serializable]
    public class LineaDialogo
    {
        public string hablante = "Kael";

        [TextArea(2, 4)] public string texto;

        public MOMENTOS_DIALOGO momento;

        [Tooltip("Segundos en pantalla.")]
        [Min(1f)] public float duracion = 4f;
    }

    /// <summary>
    /// Diálogos internos de un mundo (tarea G): líneas de Kael y de los Arquitectos al abrir el
    /// mundo, al activar una memoria, antes del jefe, al cerrar y ante un obstáculo que pide una
    /// habilidad (la pista). Las líneas de apertura y de cierre deben nombrar la
    /// emoción del mundo; viven aquí y nunca en la escena.
    ///
    /// Los textos son los del guion (Docs/Diseno/Guion.md), que carga el generador de narrativa.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Diálogos", fileName = "Dialogos")]
    public class DialogueData : ScriptableObject
    {
        public List<LineaDialogo> lineas = new List<LineaDialogo>();

        /// <summary>Las líneas de un momento, en orden.</summary>
        public List<LineaDialogo> Del(MOMENTOS_DIALOGO momento) =>
            lineas.FindAll(l => l != null && l.momento == momento && !string.IsNullOrWhiteSpace(l.texto));
    }
}
