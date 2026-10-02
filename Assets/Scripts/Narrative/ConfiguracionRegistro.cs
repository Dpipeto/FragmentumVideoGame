using Fragmentum.Data;
using UnityEngine;

namespace Fragmentum.Narrative
{
    /// <summary>
    /// Textos del registro emocional: la pregunta, las etiquetas de la escala y qué campos
    /// opcionales se piden. Son datos para que se puedan ajustar al instrumento de la tesis sin
    /// tocar código (PENDIENTES B2: las preguntas definitivas son del diseño).
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Configuración del registro emocional", fileName = "RegistroEmocional")]
    public class ConfiguracionRegistro : ScriptableObject
    {
        [Tooltip("Desactívalo para no preguntar al terminar los niveles.")]
        public bool activo = true;

        [Tooltip("{emocion} se sustituye por la emoción del mundo, en minúsculas.")]
        [TextArea(2, 3)]
        public string pregunta = "¿Con qué intensidad sentiste {emocion} en este mundo?";

        [Tooltip("Qué significa el 1 de la escala.")]
        public string etiquetaMinima = "Nada";

        [Tooltip("Qué significa el 5 de la escala.")]
        public string etiquetaMaxima = "Muchísimo";

        public bool pedirComentario = true;

        public string ayudaComentario = "¿Algo que quieras anotar? (opcional)";

        [Tooltip("Pide un código de participante para el pilotaje, solo mientras no se haya indicado. " +
                 "Apagado por decisión del diseño (PENDIENTES N12): el registro no identifica al jugador.")]
        public bool pedirParticipante;

        public string ayudaParticipante = "Código de participante (opcional)";

        public string PreguntaPara(LevelData nivel)
        {
            var emocion = nivel != null && nivel.emocion != null ? nivel.emocion.nombreCanonico.ToLowerInvariant() : "la emoción del mundo";
            return (pregunta ?? string.Empty).Replace("{emocion}", emocion);
        }
    }
}
