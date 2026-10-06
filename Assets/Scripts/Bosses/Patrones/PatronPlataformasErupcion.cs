using System.Collections;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Plataformas en erupción (Centinela Ardiente, fase 2): varias columnas de lava brotan del
    /// suelo, la primera justo bajo el jugador y las demás alrededor. Cada una se anuncia antes
    /// (zona translúcida que no hace daño) y obliga a moverse. Nunca salen fuera de la arena.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: plataformas en erupción", fileName = "PatronPlataformasErupcion")]
    public class PatronPlataformasErupcion : PatronAtaque
    {
        [Min(1)] public int erupciones = 3;

        [Tooltip("Segundos entre una erupción y la siguiente.")]
        [Min(0f)] public float intervalo = 0.45f;

        [Tooltip("Distancia máxima, a cada lado del jugador, de las erupciones después de la primera.")]
        [Min(0f)] public float dispersion = 3.5f;

        [Tooltip("Ancho y alto de cada columna de lava.")]
        public Vector2 tamano = new Vector2(1.6f, 5f);

        [Tooltip("Segundos que quema cada columna.")]
        [Min(0.05f)] public float duracion = 0.9f;

        [Min(0f)] public float recuperacion = 0.8f;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            jefe.Detener();

            // Golpea el suelo y la lava empieza a brotar: con arte, las erupciones salen con G3.
            yield return jefe.Golpear(AnimatorParams.GolpeSuelo, aviso * 0.5f);

            var limites = jefe.Limites;
            for (var i = 0; i < erupciones; i++)
            {
                var x = jefe.CentroJugador.x + (i == 0 ? 0f : Random.Range(-dispersion, dispersion));
                x = Mathf.Clamp(x, limites.x + tamano.x * 0.5f, limites.y - tamano.x * 0.5f);
                var centro = new Vector2(x, jefe.SueloEn(x) + tamano.y * 0.5f);
                jefe.CrearZona("Erupcion", centro, tamano, danio, aviso, duracion, Vector2.zero, color);
                yield return new WaitForSeconds(intervalo);
            }

            yield return new WaitForSeconds(aviso + duracion + recuperacion);
        }
    }
}
