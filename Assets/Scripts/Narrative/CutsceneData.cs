using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Narrative
{
    /// <summary>Dónde aparece un cartel, relativo a la imagen (no a la pantalla).</summary>
    public enum POSICIONES_CARTEL
    {
        ARRIBA,
        CENTRO,
        ABAJO
    }

    /// <summary>Cómo se encadenan las diapositivas de una cinemática.</summary>
    public enum MONTAJES_CINEMATICA
    {
        /// <summary>
        /// Cada imagen se funde sobre la anterior, con <see cref="CutsceneData.fundidoImagen"/>, y
        /// los fundidos van dentro de la duración de cada diapositiva (la cinemática inicial).
        /// </summary>
        FUNDIDOS_CRUZADOS,

        /// <summary>
        /// Cada diapositiva es un beat: sube desde negro, se mantiene y baja a negro con sus propios
        /// tiempos, y 0 es un corte seco. Un beat sin imagen es negro a propósito.
        /// </summary>
        BEATS
    }

    /// <summary>
    /// Un sonido de la cinemática, por su nombre en el guion. Si falta el clip, la cinemática sigue
    /// sin él y avisa por consola con ese nombre.
    /// </summary>
    [Serializable]
    public class CueAudio
    {
        [Tooltip("Nombre del cue en el guion (drone_sala, rugido_centinela…).")]
        public string nombre;

        [Tooltip("El clip. Vacío: la cinemática sigue sin él y avisa por consola.")]
        public AudioClip clip;

        [Tooltip("Segundos desde que empieza la diapositiva (fundido de entrada incluido).")]
        [Min(0f)] public float instante;

        [Tooltip("Se repite hasta que se corta.")]
        public bool bucle;

        [Tooltip("Segundos desde que empieza la diapositiva en que se corta en seco, aunque sea en otra diapositiva. " +
                 "0 = sin corte: suena entero o, en bucle, hasta el final de la cinemática.")]
        [Min(0f)] public float hasta;
    }

    /// <summary>Un texto con sus tiempos dentro de una diapositiva.</summary>
    [Serializable]
    public class Cartel
    {
        [TextArea(1, 4)] public string texto;

        [Tooltip("Segundos desde que empieza la diapositiva hasta que el texto empieza a aparecer.")]
        [Min(0f)] public float inicio;

        [Tooltip("Segundos desde que empieza a aparecer hasta que termina de irse (fundidos incluidos).")]
        [Min(0.1f)] public float duracion = 3f;

        [Tooltip("Tercio de la imagen: arriba, centro o abajo.")]
        public POSICIONES_CARTEL posicion = POSICIONES_CARTEL.ABAJO;

        [Tooltip("Tipografía de título, mayor que la normal.")]
        public bool titulo;

        [Tooltip("Aparece y se va en seco, sin fundido (un rótulo que entra con un golpe).")]
        public bool sinFundido;

        [Header("En la rejilla (cinemáticas en pixel art)")]
        [Tooltip("Colocarlo en un píxel del plano. Sin marcar, va centrado en su tercio.")]
        public bool enPunto;

        [Tooltip("Píxel de la imagen del plano donde se ancla el texto, contado desde arriba a la izquierda, como en el guion. " +
                 "Se mueve con el encuadre del beat: el texto sigue a lo que acompaña.")]
        public Vector2Int punto;

        [Tooltip("Qué punto del bloque de texto cae en ese píxel, y cómo se alinean sus líneas.")]
        public TextAnchor alineacion = TextAnchor.UpperLeft;

        public float Fin => inicio + duracion;
    }

    /// <summary>Una diapositiva de cinemática: imagen, textos, cuánto dura y audio opcional.</summary>
    [Serializable]
    public class Diapositiva
    {
        [Tooltip("Imagen de la diapositiva (PENDIENTES B1). Sin imagen, fondo negro.")]
        public Sprite imagen;

        [Tooltip("Texto sin tiempos propios: abajo, entra y sale con el margen de la cinemática.")]
        [TextArea(2, 6)] public string texto;

        [Tooltip("Textos con tiempos y posición propios, además del de arriba.")]
        public List<Cartel> carteles = new List<Cartel>();

        [Tooltip("Segundos en pantalla. Con fundidos cruzados, el fundido hacia la siguiente ocurre dentro de este tiempo; " +
                 "en un beat, es lo que se mantiene entre la subida y la bajada.")]
        [Min(0f)] public float duracion = 4f;

        [Tooltip("Opcional: voz o sonido de la diapositiva.")]
        public AudioClip audio;

        [Header("Beat (montaje por beats)")]
        [Tooltip("Segundos de subida desde negro (0 = corte seco).")]
        [Min(0f)] public float fundidoEntrada;

        [Tooltip("Segundos de bajada a negro (0 = corte seco).")]
        [Min(0f)] public float fundidoSalida;

        [Tooltip("Píxeles del plano que se desplaza la imagen para encuadrarla: x a la derecha, y arriba. Lo que queda al descubierto es negro.")]
        public Vector2Int desplazamiento;

        [Tooltip("Instantes de un temblor de cámara de píxeles enteros, en segundos desde que empieza el beat.")]
        public List<float> temblores = new List<float>();

        [Tooltip("La capa de brasas, en la misma rejilla.")]
        public bool brasas;

        [Tooltip("Fotogramas de flash blanco al terminar el beat (0 = ninguno). Se cuentan fotogramas, no segundos.")]
        [Min(0)] public int flashFinal;

        [Tooltip("Sonidos del beat, cada uno con su instante. Funcionan también con fundidos cruzados.")]
        public List<CueAudio> cues = new List<CueAudio>();

        /// <summary>Lo que dura como beat: la subida, lo que se mantiene y la bajada.</summary>
        public float DuracionBeat => fundidoEntrada + duracion + fundidoSalida;

        /// <summary>¿Tiene algo que mostrar además del fondo?</summary>
        public bool TieneContenido =>
            imagen != null || !string.IsNullOrWhiteSpace(texto) || (carteles != null && carteles.Exists(c => c != null && !string.IsNullOrWhiteSpace(c.texto)));
    }

    /// <summary>
    /// Una cinemática como dato: una lista de diapositivas. Es el sistema de la cinemática
    /// inicial, de los cierres de cada mundo y de las memorias (plan, §6.1). Una cinemática
    /// nueva se crea con <i>Create → Fragmentum → Cinemática</i>, sin escribir código.
    ///
    /// Funciona sin ningún arte: una diapositiva sin imagen se muestra en negro con su texto, y
    /// sin texto dice qué falta. Así una cinemática se puede ver, cronometrar y saltar antes de
    /// que existan las imágenes (criterio de aceptación de la Fase 8).
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Cinemática", fileName = "Cinematica")]
    public class CutsceneData : ScriptableObject
    {
        [Tooltip("Identificador estable: se guarda en la partida para las que solo se ven una vez.")]
        public string id;

        public List<Diapositiva> diapositivas = new List<Diapositiva>();

        [Tooltip("Se puede saltar con cualquier tecla, botón o clic, pasado el bloqueo inicial.")]
        public bool saltable = true;

        [Tooltip("Solo se reproduce la primera vez (la cinemática inicial).")]
        public bool soloPrimeraVez;

        [Tooltip("Presentación de un jefe que ya dice sus líneas «antes del jefe»: al terminar, el HUD no las repite. " +
                 "Sin marcar (la de la Ira, que dice Kael), suenan en el HUD al empezar el combate.")]
        public bool llevaDialogoDelJefe;

        [Tooltip("Fundidos cruzados entre imágenes (la cinemática inicial) o beats, cada uno con sus fundidos desde y hacia negro.")]
        public MONTAJES_CINEMATICA montaje = MONTAJES_CINEMATICA.FUNDIDOS_CRUZADOS;

        [Header("Sobre el juego")]
        [Tooltip("Para cinemáticas cortas dentro del nivel (la del jefe): detrás se ve la escena congelada, oscurecida y " +
                 "con franjas de cine, en lugar de negro. Las diapositivas con imagen la tapan igualmente.")]
        public bool sobreElJuego;

        [Tooltip("Opacidad del velo negro sobre la escena cuando la cinemática va sobre el juego.")]
        [Range(0f, 1f)] public float opacidadVelo = 0.55f;

        [Header("Imagen")]
        [Tooltip("Pixel art de 240 × 135: se dibuja con la cámara de cinemáticas, en su rejilla y ampliada por un número " +
                 "entero sin interpolar (×8 en 1920 × 1080; en 1280 × 720, ×5 con un borde negro fino). El texto, el " +
                 "encuadre, los temblores y el flash van en la misma rejilla. Siempre opaca. Sin marcar, la imagen se " +
                 "estira hasta llenar la pantalla.")]
        public bool escalaEntera;

        [Header("En la rejilla (cinemáticas en pixel art)")]
        [Tooltip("Fuente del texto: bitmap, con sus píxeles del tamaño de los del plano. Sin ella, la de por defecto, que no " +
                 "es bitmap (se avisa).")]
        public Font fuente;

        [Tooltip("Alto de la fuente, en píxeles del plano.")]
        [Min(1)] public int tamanoFuente = 8;

        [Tooltip("Píxeles del plano entre el principio de una línea y el de la siguiente (0 = el de la fuente).")]
        [Min(0)] public int interlineado;

        public Color colorTexto = new Color(0.94f, 0.93f, 0.85f, 1f);

        [Tooltip("Cuánto se desplaza la cámara en cada temblor, en píxeles del plano.")]
        [Min(1)] public int temblorPixeles = 1;

        [Tooltip("Cuántos fotogramas dura cada temblor.")]
        [Min(1)] public int temblorFotogramas = 4;

        [Header("Tiempos (segundos)")]
        [Tooltip("Fundido cruzado entre imágenes, y desde y hacia negro al empezar y al terminar.")]
        [Min(0f)] public float fundidoImagen = 0.6f;

        [Tooltip("Fundido de entrada y de salida de cada texto.")]
        [Min(0f)] public float fundidoTexto = 0.4f;

        [Tooltip("El texto sin tiempos propios entra este tiempo después de empezar la diapositiva y se va este tiempo antes de que termine.")]
        [Min(0f)] public float margenTexto = 0.8f;

        [Tooltip("Durante estos primeros segundos no se puede saltar: evita saltarla con la tecla que inició la partida.")]
        [Min(0f)] public float bloqueoSalto = 2f;

        [Header("Aviso para saltar")]
        public string avisoSaltar = "Pulsa cualquier tecla para saltar";

        [Tooltip("El aviso aparece al terminar el bloqueo y se ha ido del todo en este segundo.")]
        [Min(0f)] public float finAviso = 5f;

        [Tooltip("Opacidad del aviso: discreto.")]
        [Range(0f, 1f)] public float opacidadAviso = 0.45f;

        [Header("Audio")]
        [Tooltip("Opcional: música de toda la cinemática. Cada diapositiva puede tener además su propio audio.")]
        public AudioClip musica;

        public string Id => string.IsNullOrEmpty(id) ? name : id;

        /// <summary>¿Cada diapositiva es un beat con sus fundidos?</summary>
        public bool PorBeats => montaje == MONTAJES_CINEMATICA.BEATS;

        /// <summary>
        /// Lo que dura la diapositiva <paramref name="indice"/> en la cinemática: su duración con
        /// fundidos cruzados, o la subida, lo que se mantiene y la bajada si es un beat.
        /// </summary>
        public float DuracionDe(int indice)
        {
            var diapositiva = indice >= 0 && indice < diapositivas.Count ? diapositivas[indice] : null;
            if (diapositiva == null)
            {
                return 0f;
            }

            return PorBeats ? diapositiva.DuracionBeat : diapositiva.duracion;
        }

        public float DuracionTotal
        {
            get
            {
                var total = 0f;
                for (var i = 0; i < diapositivas.Count; i++)
                {
                    total += DuracionDe(i);
                }

                return total;
            }
        }

        /// <summary>
        /// ¿Le falta arte? (Alguna diapositiva sin imagen.) Para avisar una vez por consola. En un
        /// montaje por beats, un beat sin imagen es negro a propósito.
        /// </summary>
        public bool FaltaArte => !PorBeats && diapositivas.Exists(d => d != null && d.imagen == null);

        /// <summary>
        /// Texto principal de una diapositiva. Si no tiene imagen ni ningún texto, el aviso de lo
        /// que falta, para que una cinemática a medio hacer nunca se vea vacía.
        /// </summary>
        public string TextoDe(int indice)
        {
            var diapositiva = indice >= 0 && indice < diapositivas.Count ? diapositivas[indice] : null;
            if (diapositiva != null && !string.IsNullOrWhiteSpace(diapositiva.texto))
            {
                return diapositiva.texto;
            }

            // Un beat vacío es negro a propósito: no dice que falte nada.
            return (diapositiva != null && diapositiva.TieneContenido) || (diapositiva != null && PorBeats)
                ? string.Empty
                : $"[CINEMÁTICA «{Id}» PENDIENTE · diapositiva {indice + 1} de {Mathf.Max(1, diapositivas.Count)}]";
        }

        /// <summary>
        /// Todos los carteles de una diapositiva con sus tiempos: los propios y, si hay texto
        /// principal (o falta todo), uno abajo que entra y sale con <see cref="margenTexto"/>.
        /// </summary>
        public List<Cartel> CartelesDe(int indice)
        {
            var resultado = new List<Cartel>();
            var diapositiva = indice >= 0 && indice < diapositivas.Count ? diapositivas[indice] : null;
            var duracion = DuracionDe(indice);

            var principal = TextoDe(indice);
            if (!string.IsNullOrEmpty(principal))
            {
                // En diapositivas muy cortas el margen se recorta para que el texto llegue a verse.
                var margen = Mathf.Min(margenTexto, duracion * 0.25f);
                resultado.Add(new Cartel
                {
                    texto = principal,
                    inicio = margen,
                    duracion = Mathf.Max(0.1f, duracion - 2f * margen),
                    posicion = POSICIONES_CARTEL.ABAJO,
                });
            }

            if (diapositiva != null && diapositiva.carteles != null)
            {
                resultado.AddRange(diapositiva.carteles.FindAll(c => c != null && !string.IsNullOrWhiteSpace(c.texto)));
            }

            return resultado;
        }
    }
}
