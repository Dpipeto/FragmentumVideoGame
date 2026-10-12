using System.Collections;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Inundación (Doncella del Lamento). Avisa en la franja del suelo donde está Kael (sale agua
    /// por las grietas) y después el agua sube y se queda unos segundos. No hace daño: es agua,
    /// lenta, con el salto alto y gastando aire. En la fase 2 es lo que después se congela.
    ///
    /// El patrón termina cuando el agua ha subido; la franja sigue inundada por su cuenta
    /// (<see cref="Fragmentum.Levels.Mecanicas.FranjaInundable"/>) mientras la Doncella sigue atacando.
    /// Si no queda ninguna franja seca, no hace nada.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: inundación", fileName = "PatronInundacion")]
    public class PatronInundacion : PatronAtaque
    {
        [Tooltip("Cuántas franjas inunda: la de Kael y, si son más, sus vecinas.")]
        [Min(1)] public int franjas = 1;

        [Tooltip("Segundos que el agua se queda, desde que empieza a subir.")]
        [Min(0.5f)] public float duracion = 5f;

        [Min(0f)] public float recuperacion = 0.5f;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            jefe.Detener();
            var elegidas = jefe is DoncellaLamento doncella ? doncella.FranjasParaInundar(franjas) : null;
            if (elegidas == null || elegidas.Count == 0)
            {
                yield return new WaitForSeconds(0.2f);
                yield break;
            }

            jefe.MirarAlJugador();
            jefe.Avisar(aviso);
            (jefe as DoncellaLamento)?.Gesto(AnimatorParams.Inundar, aviso);
            var subida = 0f;
            foreach (var franja in elegidas)
            {
                franja.Inundar(aviso, duracion);
                subida = Mathf.Max(subida, franja.tiempoSubida);
            }

            yield return new WaitForSeconds(aviso + subida + recuperacion);
        }
    }
}
