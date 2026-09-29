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
        CIERRE
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
    /// mundo, al activar una memoria, antes del jefe y al cerrar. Las líneas deben nombrar la
    /// emoción del mundo; viven aquí y nunca en la escena.
    ///
    /// Los textos son del guion (PENDIENTES B2). Mientras falten, el generador deja líneas
    /// marcadas como pendientes que ya nombran la emoción, para que el sistema se vea funcionar.
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
