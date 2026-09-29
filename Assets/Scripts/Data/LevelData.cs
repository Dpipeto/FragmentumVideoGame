using System;
using System.Collections.Generic;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Data
{
    /// <summary>Reglas de juego que el nivel impone al jugador.</summary>
    [Serializable]
    public struct ReglasNivel
    {
        [Tooltip("Se puede agacharse (cobertura, mundo de la Culpa).")]
        public bool agacharse;

        [Tooltip("Correr hace ruido que oyen los enemigos (mundo del Miedo).")]
        public bool sigilo;

        [Tooltip("El nivel está a oscuras salvo la luz del jugador y las antorchas (mundo del Miedo).")]
        public bool oscuridad;

        [Tooltip("Radio de la luz propia del jugador cuando hay oscuridad (Anexo A: 3 u).")]
        public float radioLuzJugador;
    }

    /// <summary>Tramo del layout del nivel (Anexo A). Se dibuja como guía en la vista Scene.</summary>
    [Serializable]
    public struct SeccionNivel
    {
        public string nombre;
        public float desdeX;
        public float hastaX;

        [TextArea(1, 3)] public string contenido;
    }

    /// <summary>
    /// Un nivel como dato (patrón Type Object): qué escena carga, qué mundo y qué emoción
    /// representa, su paleta, su jefe, qué fragmento y habilidad otorga, su música y sus reglas.
    ///
    /// Es la traducción directa de la regla de diseño del encargo: la correspondencia
    /// emoción ↔ mecánica ↔ estética es un dato del nivel, no un valor escrito en la escena.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Nivel", fileName = "Nivel")]
    public class LevelData : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Número del nivel; define el orden y el desbloqueo secuencial.")]
        public int id = 1;

        [Tooltip("Escena del nivel. Sin escena, el selector lo muestra «en construcción».")]
        public ReferenciaEscena escena = new ReferenciaEscena();

        public string nombreMundo;
        public EmocionData emocion;

        [TextArea(2, 5)] public string sinopsis;

        [Range(1, 5)] public int dificultad = 1;

        [Header("Estética")]
        [Tooltip("Los 4 colores del Anexo A.")]
        public Color[] paleta = new Color[4];

        public Sprite icono;
        public AudioClip musica;
        public AudioClip musicaJefe;

        [Header("Progresión")]
        [Tooltip("Vacío en el Jardín Etéreo, que no tiene jefe.")]
        public JefeData jefe;

        public FragmentoData fragmento;
        public HabilidadData habilidadOtorgada;

        [Header("Reglas y layout")]
        public ReglasNivel reglas;
        public List<SeccionNivel> secciones = new List<SeccionNivel>();

        [Tooltip("Posición x de los checkpoints que prevé el diseño (Anexo A). Solo es una guía para " +
                 "preparar la escena; en el juego mandan los objetos Checkpoint de la escena.")]
        public List<float> puntosDeControl = new List<float>();

        [Header("Interfaz (maqueta de los menús de pausa)")]
        [Tooltip("Color de acento de las pantallas de pausa, Game Over y fin de nivel.")]
        public Color acento;

        [Tooltip("Frase del mundo que se muestra en la pausa.")]
        public string cita;

        /// <summary>«Ira — Fortaleza Volcánica», o el nombre del mundo si falta la emoción.</summary>
        public string Titulo => emocion != null ? $"{emocion.nombreCanonico} — {nombreMundo}" : nombreMundo;

        /// <summary>«I · FORTALEZA VOLCÁNICA», como en la maqueta de los menús.</summary>
        public string Etiqueta => $"{Romano(id)} · {(nombreMundo ?? string.Empty).ToUpperInvariant()}";

        /// <summary>¿Tiene una escena que se pueda cargar?</summary>
        public bool Jugable => escena != null && escena.EnBuild;

        public Color ColorPrincipal => emocion != null ? emocion.color : paleta != null && paleta.Length > 0 ? paleta[0] : Color.gray;

        /// <summary>El acento de la interfaz; sin él, el color de la emoción aclarado.</summary>
        public Color ColorAcento => acento.a > 0f ? acento : Color.Lerp(ColorPrincipal, Color.white, 0.35f);

        /// <summary>El color más oscuro de la paleta (fondo de la cámara y de los menús).</summary>
        public Color ColorOscuro
        {
            get
            {
                if (paleta == null || paleta.Length == 0)
                {
                    return Color.black;
                }

                var oscuro = paleta[0];
                foreach (var color in paleta)
                {
                    if (color.grayscale < oscuro.grayscale)
                    {
                        oscuro = color;
                    }
                }

                return oscuro;
            }
        }

        /// <summary>Tramo horizontal que cubren las secciones: (desde, hasta). (0, 0) si no hay secciones.</summary>
        public Vector2 Extension
        {
            get
            {
                if (secciones == null || secciones.Count == 0)
                {
                    return Vector2.zero;
                }

                var desde = float.MaxValue;
                var hasta = float.MinValue;
                foreach (var seccion in secciones)
                {
                    desde = Mathf.Min(desde, seccion.desdeX);
                    hasta = Mathf.Max(hasta, seccion.hastaX);
                }

                return new Vector2(desde, hasta);
            }
        }

        public static string Romano(int numero)
        {
            string[] romanos = { "0", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            return numero >= 0 && numero < romanos.Length ? romanos[numero] : numero.ToString();
        }
    }
}
