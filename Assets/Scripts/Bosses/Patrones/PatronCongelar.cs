using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Congelar (Doncella del Lamento, fase 2). El agua que dejó la inundación se congela: la
    /// superficie se pone blanca (el aviso) y después brotan púas de hielo en todo lo que estaba
    /// inundado. Arriba queda una capa de hielo que se pisa durante unos segundos.
    ///
    /// Se responde saliendo del agua a tiempo y subiéndose después a la capa: sobre ella llora la
    /// Doncella. Si no hay ninguna franja inundada, no hace nada.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: congelar", fileName = "PatronCongelar")]
    public class PatronCongelar : PatronAtaque
    {
        [Tooltip("Segundos que hacen daño las púas.")]
        [Min(0.05f)] public float duracionPuas = 0.6f;

        [Tooltip("Segundos que se queda la capa de hielo.")]
        [Min(0.5f)] public float duracionCapa = 6f;

        [Min(0f)] public float recuperacion = 0.4f;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            jefe.Detener();
            var inundadas = jefe is DoncellaLamento doncella ? doncella.FranjasInundadas() : null;
            if (inundadas == null || inundadas.Count == 0)
            {
                yield return new WaitForSeconds(0.2f);
                yield break;
            }

            jefe.MirarAlJugador();
            jefe.Avisar(aviso);
            foreach (var franja in inundadas)
            {
                franja.Congelar(aviso, duracionPuas, duracionCapa, danio);
            }

            yield return new WaitForSeconds(aviso + duracionPuas + recuperacion);
        }
    }
}
