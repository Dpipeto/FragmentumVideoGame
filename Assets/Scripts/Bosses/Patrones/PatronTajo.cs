using System.Collections;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Tajo (Espectro del Pavor, fase 1). Aparece a <see cref="distancia"/> de Kael, a su espalda
    /// (o delante, si Kael está contra una pared), avisa con su destello y lanza un tajo de
    /// <see cref="alcance"/> u. Después se queda <see cref="jadeo"/> s jadeando, visible: es la
    /// ventana. Se responde alejándose del filo durante el aviso y volviendo a por él.
    ///
    /// Con otro jefe, el tajo sale donde esté, de cara a Kael.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: tajo", fileName = "PatronTajo")]
    public class PatronTajo : PatronAtaque
    {
        [Tooltip("A qué distancia de Kael aparece (entre centros), a su espalda.")]
        [Min(0f)] public float distancia = 2.5f;

        [Tooltip("Hasta dónde llega el tajo, desde su centro.")]
        [Min(0.1f)] public float alcance = 3f;

        [Tooltip("Alto del tajo, desde el suelo.")]
        [Min(0.1f)] public float alto = 2.6f;

        [Min(0.02f)] public float duracionGolpe = 0.2f;

        [Tooltip("Lo que se queda jadeando, visible: la ventana vulnerable. 0 = sin ventana.")]
        [Min(0f)] public float jadeo = 1.5f;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            jefe.Detener();
            var pavor = jefe as EspectroDelPavor;
            if (pavor != null)
            {
                pavor.Aparecer(pavor.LugarParaElTajo(distancia));
                pavor.Destellar(aviso);
            }
            else
            {
                jefe.MirarAlJugador();
                jefe.Avisar(aviso);
            }

            yield return new WaitForSeconds(aviso);
            Tajo(jefe, jefe.Centro.x, pavor != null ? pavor.Suelo : jefe.SueloY, jefe.Mirando);
            yield return new WaitForSeconds(duracionGolpe);

            if (jadeo > 0f)
            {
                // La ventana corta el patrón en el fotograma siguiente; al acabar, vuelve a no verse.
                jefe.AbrirVentanaVulnerable(jadeo);
                yield return new WaitForSeconds(0.3f);
            }
            else if (pavor != null)
            {
                pavor.Ocultarse();
            }
        }

        /// <summary>
        /// La zona del tajo de una figura cuyo centro está en <paramref name="x"/>: de su centro a
        /// <see cref="alcance"/> hacia <paramref name="mirando"/>, desde el suelo. La usan también
        /// las sombras.
        /// </summary>
        public ZonaDeDanio Tajo(BossController jefe, float x, float suelo, int mirando)
        {
            var tamano = new Vector2(alcance, alto);
            var centro = new Vector2(x + mirando * alcance * 0.5f, suelo + alto * 0.5f);
            var zona = jefe.CrearZona("Tajo", centro, tamano, danio, 0f, duracionGolpe, Vector2.zero, color, dibujarZona);
            (jefe as EspectroDelPavor)?.VerEnLaOscuridad(zona);
            return zona;
        }
    }
}
