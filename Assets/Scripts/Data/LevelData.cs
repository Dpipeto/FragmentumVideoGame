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

        /// <summary>«Ira — Fortaleza Volcánica», o el nombre del mundo si falta la emoción.</summary>
        public string Titulo => emocion != null ? $"{emocion.nombreCanonico} — {nombreMundo}" : nombreMundo;

        /// <summary>¿Tiene una escena que se pueda cargar?</summary>
        public bool Jugable => escena != null && escena.EnBuild;

        public Color ColorPrincipal => emocion != null ? emocion.color : paleta != null && paleta.Length > 0 ? paleta[0] : Color.gray;
    }
}
