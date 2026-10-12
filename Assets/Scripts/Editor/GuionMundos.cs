using System.Linq;
using Fragmentum.Data;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El guion de los seis mundos (<c>Docs/Diseno/Guion.md</c>, aprobado): las líneas de Kael y de
    /// los Arquitectos en el HUD y los textos de las cinemáticas de cierre, de entrada al final y
    /// final. <see cref="GeneradorNarrativa"/> lo carga en los assets de <c>Assets/Data/Narrativa</c>.
    ///
    /// - Kael habla en frases cortas, en presente y sin certezas; los Arquitectos hablan del
    ///   soldado en tercera persona y a Kael de tú.
    /// - Cada apertura y cada cierre nombran la emoción del mundo (tarea G).
    /// - El cierre nombra la habilidad como un gesto emocional: la habilidad es lo que Kael aprendió.
    /// - Las líneas del HUD duran 4,5 s: ninguna pasa de unas 120 letras.
    ///
    /// No toca la cinemática inicial ni la presentación de la Centinela («El que ya estaba aquí»),
    /// que tienen su propio guion.
    /// </summary>
    public static class GuionMundos
    {
        public sealed class Mundo
        {
            public EMOCIONES Emocion;
            public string Apertura;

            /// <summary>Suena con las memorias interactuables (Fase 10). Vacío: el mundo no tiene.</summary>
            public string EcoMemoria;

            /// <summary>La línea del jefe antes del combate. Vacío: el mundo no tiene jefe.</summary>
            public string AntesDelJefe;

            /// <summary>
            /// Lo que Kael se dice ante el obstáculo del mundo que pide una habilidad que ya tiene
            /// (el tapón de hielo y el Ataque Cargado). Vacío: el mundo no tiene.
            /// </summary>
            public string Pista;

            public string Cierre;

            /// <summary>Los textos de la cinemática de cierre del mundo.</summary>
            public string[] CinematicaCierre;

            /// <summary>Segundos de cada texto de la cinemática de cierre.</summary>
            public float DuracionCierre = 4f;
        }

        public static readonly Mundo[] Mundos =
        {
            new Mundo
            {
                Emocion = EMOCIONES.IRA,
                Apertura = "Este calor no es mío… y aun así me empuja a correr. Si esto es la Ira, alguien la dejó encendida.",
                EcoMemoria = "Una voz grita órdenes que no entiendo. Mis manos sí las entienden: se cierran solas.",
                AntesDelJefe = "Él me dejó aquí para que nadie tocara su rabia. Tú tampoco la tocarás.",
                Cierre = "La Ira no era el fuego. Era lo que él nunca dijo. La llevo conmigo, pero ya no me lleva ella.",
                CinematicaCierre = new[]
                {
                    "El fuego se apaga sin que nadie lo sople.",
                    "En la ceniza queda una forma: un puño que, por fin, se abre.",
                    "Kael integra el Fragmento de la Ira. Ahora puede contener un golpe… y soltarlo cuando decida.",
                },
            },
            new Mundo
            {
                Emocion = EMOCIONES.TRISTEZA,
                Apertura = "Una ciudad entera bajo el agua y ni una lágrima en la superficie. Así de callada es la Tristeza.",
                EcoMemoria = "Alguien deja una silla vacía en la mesa. Nadie la quita. Nadie la nombra.",
                AntesDelJefe = "Él cerró los ojos para no llorar, y la ciudad se hundió. ¿Vienes a cerrarlos tú también?",
                Pista = "Este hielo no cede a un golpe cualquiera. Hace falta el Ataque Cargado: contener el golpe… y soltarlo.",
                Cierre = "Me dejé hundir y no me ahogué. La Tristeza pesa, pero también sostiene.",
                CinematicaCierre = new[]
                {
                    "El agua baja despacio, como quien termina de llorar.",
                    "La ciudad respira por primera vez en años.",
                    "Kael integra el Fragmento de la Tristeza. Ya puede avanzar también bajo el agua.",
                },
            },
            new Mundo
            {
                Emocion = EMOCIONES.MIEDO,
                Apertura = "No veo mis propios pasos. Algo respira entre los árboles… o soy yo. Esto es el Miedo.",
                EcoMemoria = "Una trinchera. Un silencio demasiado largo. Él se quedó quieto para que no lo encontraran.",
                AntesDelJefe = "Él aprendió a desaparecer para seguir vivo. Yo soy todo lo que dejó escondido.",
                Cierre = "El Miedo no se fue: dejó de decidir por mí. Puedo andar en la sombra sin perderme en ella.",
                CinematicaCierre = new[]
                {
                    "La oscuridad retrocede un paso. Luego otro.",
                    "El bosque no era un laberinto: era un lugar donde nadie había encendido una luz.",
                    "Kael integra el Fragmento del Miedo. Puede ocultarse sin dejar de avanzar.",
                },
            },
            new Mundo
            {
                Emocion = EMOCIONES.CULPA,
                Apertura = "Aquí nada se movió desde el último disparo. Es la Culpa de otro… y la siento mía.",
                EcoMemoria = "Una orden. Un sí. Un pueblo que ya no está. Él lo recuerda cada día con los ojos abiertos.",
                AntesDelJefe = "No dicto sentencia. Solo te muestro lo que él se negó a mirar.",
                Cierre = "Mirarlo no me destruyó. La Culpa es un peso, no un nombre. Puedo cargarlo y seguir de pie.",
                CinematicaCierre = new[]
                {
                    "Las memorias se encienden una a una sobre la plaza.",
                    "Nadie las borra. Por primera vez, alguien las sostiene.",
                    "Kael integra el Fragmento de la Culpa. Un escudo que no niega el golpe: lo resiste.",
                },
            },
            new Mundo
            {
                Emocion = EMOCIONES.ESPERANZA,
                Apertura = "Por primera vez nada quiere herirme. Me cuesta creerlo. Quizá así empieza la Esperanza.",
                EcoMemoria = "Una casa con flores en la ventana. Antes de la guerra, él también reía.",
                Cierre = "Cada fragmento encontró su sitio. La Esperanza no borra lo que pasó: lo deja crecer en otro lugar.",
                // Anexo A: ~30 s, no interactiva; Kael ve al Soldado Original a lo lejos por primera vez.
                CinematicaCierre = new[]
                {
                    "El jardín termina en un borde de niebla.",
                    "Al otro lado, alguien espera de espaldas. Lleva el mismo abrigo gastado.",
                    "Kael lo reconoce sin haberlo visto nunca.",
                    "Es él. El que se quedó con todo lo que Kael no recuerda.",
                    "El soldado no se da la vuelta. Todavía no.",
                },
                DuracionCierre = 6f,
            },
            new Mundo
            {
                Emocion = EMOCIONES.INTEGRACION,
                Apertura = "Todo lo que vine a buscar estaba aquí, esperando. La Integración no es ganarle: es dejar de huir.",
                AntesDelJefe = "Eres lo que sobró cuando me arranqué el dolor. Eres un error.",
                Cierre = "No vine a reemplazarte ni a destruirte. La Integración era esto: caminar juntos hacia la misma luz.",
                // Su cierre es la cinemática final (Final), no una de cierre de mundo.
            },
        };

        /// <summary>
        /// La entrada al Espejo del Origen (~45 s). El Anexo A pide que el soldado confronte a Kael y
        /// que Kael responda con actos, no con palabras.
        /// </summary>
        public static readonly string[] EntradaFinal =
        {
            "Un espejo del tamaño del cielo.",
            "Del otro lado, el soldado. Del mismo lado, también.",
            "Soldado: Te hice para no sentir.",
            "Soldado: Y aun así lo sentiste todo.",
            "Soldado: Eres un error.",
            "Kael no responde.",
            "Da un paso al frente.",
        };

        public const float DuracionEntradaFinal = 6f;

        public const string PreguntaFinal = "¿Eres un error… o una nueva identidad?";

        /// <summary>La cinemática final: la del Anexo A, con la pregunta sola y al final.</summary>
        public static readonly string[] Final =
        {
            "El soldado se fragmenta en luz.",
            "Kael no la esquiva: la recibe.",
            "El mundo se reconstruye, pieza a pieza.",
            PreguntaFinal,
        };

        public const float DuracionFinal = 4f;

        /// <summary>La pregunta se queda más tiempo: no tiene respuesta.</summary>
        public const float DuracionPregunta = 6f;

        public static Mundo De(EMOCIONES emocion) => Mundos.FirstOrDefault(m => m.Emocion == emocion);
    }
}
