using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Narrative
{
    /// <summary>Una diapositiva de cinemática: imagen, texto, cuánto dura y audio opcional.</summary>
    [Serializable]
    public class Diapositiva
    {
        [Tooltip("Imagen 16:9 (PENDIENTES B1). Sin imagen, fondo negro con el texto.")]
        public Sprite imagen;

        [TextArea(2, 6)] public string texto;

        [Tooltip("Segundos en pantalla si el jugador no la pasa antes.")]
        [Min(0.5f)] public float duracion = 4f;

        [Tooltip("Opcional: voz o sonido de la diapositiva.")]
        public AudioClip audio;
    }

    /// <summary>
    /// Una cinemática como dato: una lista de diapositivas. Es el sistema de la cinemática
    /// inicial, de los cierres de cada mundo y de las memorias (plan, §6.1).
    ///
    /// Funciona sin ningún arte: cada diapositiva sin imagen se muestra en negro con su texto, y
    /// sin texto dice qué falta. Así la cinemática inicial ya se puede ver, cronometrar y saltar
    /// antes de que existan las imágenes (criterio de aceptación de la Fase 8).
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Cinemática", fileName = "Cinematica")]
    public class CutsceneData : ScriptableObject
    {
        [Tooltip("Identificador estable: se guarda en la partida para las que solo se ven una vez.")]
        public string id;

        public List<Diapositiva> diapositivas = new List<Diapositiva>();

        [Tooltip("Se puede saltar con Esc, Enter o un clic.")]
        public bool saltable = true;

        [Tooltip("Solo se reproduce la primera vez (la cinemática inicial).")]
        public bool soloPrimeraVez;

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

        /// <summary>Texto a mostrar: el de la diapositiva o, si no hay, el aviso de lo que falta.</summary>
        public string TextoDe(int indice)
        {
            var diapositiva = indice >= 0 && indice < diapositivas.Count ? diapositivas[indice] : null;
            return diapositiva != null && !string.IsNullOrWhiteSpace(diapositiva.texto)
                ? diapositiva.texto
                : $"[CINEMÁTICA «{Id}» PENDIENTE · diapositiva {indice + 1} de {Mathf.Max(1, diapositivas.Count)}]";
        }
    }
}
