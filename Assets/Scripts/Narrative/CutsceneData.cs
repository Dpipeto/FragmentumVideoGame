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

        [Tooltip("Segundos en pantalla. El fundido hacia la siguiente ocurre dentro de este tiempo.")]
        [Min(0.5f)] public float duracion = 4f;

        [Tooltip("Opcional: voz o sonido de la diapositiva.")]
        public AudioClip audio;

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

        [Header("Sobre el juego")]
        [Tooltip("Para cinemáticas cortas dentro del nivel (la del jefe): detrás se ve la escena congelada, oscurecida y " +
                 "con franjas de cine, en lugar de negro. Las diapositivas con imagen la tapan igualmente.")]
        public bool sobreElJuego;

        [Tooltip("Opacidad del velo negro sobre la escena cuando la cinemática va sobre el juego.")]
        [Range(0f, 1f)] public float opacidadVelo = 0.55f;

        [Header("Imagen")]
        [Tooltip("Pixel art: cada imagen a su tamaño por un número entero (el mayor que quepa), centrada y sin interpolar. " +
                 "Un plano de 240 × 135 va a ×4 en 960 × 540 y a ×8 en 1920 × 1080; en 1280 × 720, a ×5 con un borde negro fino. " +
                 "Sin marcar, la imagen se estira hasta llenar la pantalla.")]
        public bool escalaEntera;

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

        public float DuracionTotal
        {
            get
            {
                var total = 0f;
                foreach (var diapositiva in diapositivas)
                {
                    total += diapositiva != null ? diapositiva.duracion : 0f;
                }

                return total;
            }
        }

        /// <summary>¿Le falta arte? (Alguna diapositiva sin imagen.) Para avisar una vez por consola.</summary>
        public bool FaltaArte => diapositivas.Exists(d => d != null && d.imagen == null);

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

            return diapositiva != null && diapositiva.TieneContenido
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
            var duracion = diapositiva != null ? diapositiva.duracion : 0f;

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
