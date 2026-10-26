using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Llanto (Doncella del Lamento). Cierra cada ronda: baja adonde le toca llorar
    /// (<see cref="DoncellaLamento.LugarDeLlanto"/>: el hielo seco en la fase 1; sobre su hielo o su
    /// agua en la fase 2), se queda quieta y llora. Es la ventana vulnerable, y la única: fuera de
    /// ella es inmune. La bajada es el aviso.
    ///
    /// No hace daño. Al abrirse la ventana, el jefe corta el patrón.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: llanto", fileName = "PatronLlanto")]
    public class PatronLlanto : PatronAtaque
    {
        [Tooltip("Segundos que llora (la ventana vulnerable); 0 = los de sus datos.")]
        [Min(0f)] public float duracion = 1.5f;

        [Tooltip("Velocidad a la que baja (u/s).")]
        [Min(0.1f)] public float velocidadBajada = 4f;

        [Tooltip("Lo que tarda como mucho en llegar: si está lejos, va más rápido.")]
        [Min(0.1f)] public float tiempoMaximoBajando = 1.6f;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            jefe.Detener();
            if (!(jefe is DoncellaLamento doncella))
            {
                jefe.AbrirVentanaVulnerable(duracion);
                yield return new WaitForSeconds(0.3f);
                yield break;
            }

            var lugar = doncella.LugarDeLlanto();
            var velocidad = Mathf.Max(velocidadBajada, Vector2.Distance(doncella.Pies, lugar) / tiempoMaximoBajando);
            doncella.FlotarA(lugar, velocidad);

            var limite = Time.time + tiempoMaximoBajando + 0.5f;
            while (!doncella.HaLlegado && Time.time < limite)
            {
                yield return new WaitForFixedUpdate();
            }

            doncella.Llorar(duracion);

            // La ventana corta el patrón en el fotograma siguiente.
            yield return new WaitForSeconds(0.3f);
        }
    }
}
