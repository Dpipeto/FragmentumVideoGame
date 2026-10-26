using System;
using System.Collections.Generic;
using Fragmentum.Bosses.Patrones;
using UnityEngine;

namespace Fragmentum.Data
{
    /// <summary>
    /// Una fase de un jefe: desde qué vida empieza y qué patrones de ataque repite.
    /// </summary>
    [Serializable]
    public class FaseJefe
    {
        public string nombre = "Fase";

        [Tooltip("La fase empieza cuando la vida baja a esta fracción: 1 = desde el principio, 0,5 = a mitad de vida.")]
        [Range(0f, 1f)] public float umbralVida = 1f;

        [Tooltip("Patrones que el jefe repite en esta fase, en orden (patrón Strategy). Vacío = el jefe espera.")]
        public List<PatronAtaque> patrones = new List<PatronAtaque>();

        [Min(0f)] public float pausaEntrePatrones = 1f;

        [Tooltip("Elige los patrones al azar en lugar de en orden.")]
        public bool aleatorio;

        [Tooltip("Multiplica la velocidad del jefe al perseguir durante esta fase: la Ira acelera según pierde vida.")]
        [Min(0.1f)] public float multiplicadorVelocidad = 1f;
    }

    /// <summary>
    /// Datos de un jefe (Arquitecto del Alma), patrón Type Object: el nombre canónico, la vida,
    /// las fases con sus patrones y la ventana vulnerable. Los cinco jefes comparten el mismo
    /// <c>BossController</c>; lo que los distingue está aquí y en los pocos pasos que redefine
    /// cada uno.
    ///
    /// El nombre canónico vive aquí y no en escenas ni scripts (Anexo A: Centinela Ardiente, no
    /// Pyrokratos; Juez Silente, no Silencioso).
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefe", fileName = "Jefe")]
    public class JefeData : ScriptableObject
    {
        public const int MaxFases = 3;

        public string nombreCanonico;

        public EmocionData emocion;

        [TextArea(2, 4)] public string descripcion;

        [Tooltip("Arte conceptual de referencia (no es el sprite de juego).")]
        public Sprite concepto;

        [Header("Combate")]
        [Min(1)] public int vidaMaxima = 24;

        [Tooltip("Segundos de invulnerabilidad tras cada golpe. Cortos: el jefe no tiene estado DAMAGED.")]
        [Min(0f)] public float invulnerabilidad = 0.15f;

        [Min(0)] public int danioContacto = 1;

        [Min(0f)] public float velocidad = 2.5f;

        [Tooltip("Puntos que da derrotarlo: más que cualquier enemigo del mundo, mini-jefe incluido.")]
        [Min(0)] public int puntos = 1500;

        [Tooltip("Hasta 3 fases, de mayor a menor umbral de vida. La primera empieza siempre en 1.")]
        public List<FaseJefe> fases = new List<FaseJefe>();

        [Header("Ventana vulnerable")]
        [Tooltip("Segundos que dura la ventana si el patrón no dice otra cosa (Anexo A: 1,2 s de espada clavada).")]
        [Min(0f)] public float duracionVulnerable = 1.2f;

        [Tooltip("Multiplica el daño recibido durante la ventana.")]
        [Min(1f)] public float multiplicadorVulnerable = 2f;

        [Tooltip("Solo recibe daño durante la ventana vulnerable (Juez Silente).")]
        public bool inmuneFueraDeVulnerable;

        [Header("Derrota")]
        [Tooltip("Sin arte, segundos que dura la «explosión» antes de desaparecer. Con arte, lo que tarda la muerte en llegar a su último fotograma (la Centinela, 2,1 s hasta D4).")]
        [Min(0f)] public float duracionDerrota = 2f;

        [Header("Aspecto provisional (sin arte, PENDIENTES B5)")]
        [Tooltip("Color del sprite. Con arte, blanco: los colores son los del dibujo.")]
        public Color tinte = Color.white;

        public Vector2 tamano = new Vector2(2f, 3.2f);

        [Tooltip("Desmárcalo si el arte del jefe mira a la izquierda.")]
        public bool spriteMiraDerecha = true;

        /// <summary>Número de fases usables (entre 1 y 3).</summary>
        public int NumeroFases => Mathf.Clamp(fases != null ? fases.Count : 0, 1, MaxFases);

        public FaseJefe Fase(int indice) => fases != null && indice >= 0 && indice < fases.Count ? fases[indice] : null;

        /// <summary>
        /// Índice de la fase (0, 1 o 2) que corresponde a una fracción de vida: la más avanzada
        /// cuyo umbral ya se alcanzó. Con fases a 1 y 0,5: 0,6 → 0; 0,5 → 1.
        /// </summary>
        public int FaseParaVida(float fraccion)
        {
            var fase = 0;
            if (fases == null)
            {
                return fase;
            }

            for (var i = 1; i < fases.Count && i < MaxFases; i++)
            {
                if (fases[i] != null && fraccion <= fases[i].umbralVida)
                {
                    fase = i;
                }
            }

            return fase;
        }
    }
}
