using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Las tres sombras (Espectro del Pavor, fase 2). Aparecen tres figuras iguales (a la izquierda,
    /// en el centro y a la derecha de la arena) y lanzan su tajo una tras otra, de izquierda a
    /// derecha, cada una con su destello. Después se quedan las tres <see cref="jadeo"/> s
    /// jadeando: la ventana. Solo la verdadera lleva una luciérnaga dorada; las sombras, verdes.
    ///
    /// Golpear a la verdadera cuenta y deshace a las sombras; golpear a una sombra la deshace y
    /// encoge la luz de Kael (<see cref="SombraDelPavor"/>). En el miedo, la figura verdadera es la
    /// que tiene luz.
    ///
    /// El tajo de cada figura es el de <see cref="tajo"/> (su alcance, su alto y su daño). Con otro
    /// jefe, solo él lanza su tajo.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: sombras", fileName = "PatronSombras")]
    public class PatronSombras : PatronAtaque
    {
        [Tooltip("El tajo de cada figura (su alcance, su alto, su daño y lo que dura).")]
        public PatronTajo tajo;

        [Tooltip("Lo lejos de las paredes que aparecen las figuras de los lados.")]
        [Min(0f)] public float margen = 3f;

        [Tooltip("Lo que tardan en aparecer, antes del primer aviso.")]
        [Min(0f)] public float aparicion = 0.4f;

        [Tooltip("Entre el tajo de una figura y el aviso de la siguiente.")]
        [Min(0f)] public float intervalo = 0.3f;

        [Tooltip("Lo que se quedan las tres jadeando: la ventana vulnerable.")]
        [Min(0f)] public float jadeo = 1.5f;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            jefe.Detener();
            var pavor = jefe as EspectroDelPavor;
            if (pavor == null || tajo == null)
            {
                // Sin sombras que llevar (o sin tajo): un tajo suyo, si lo hay.
                if (tajo != null)
                {
                    yield return tajo.Ejecutar(jefe);
                }

                yield break;
            }

            var limites = jefe.Limites;
            var lugares = new[] { limites.x + margen, (limites.x + limites.y) * 0.5f, limites.y - margen };
            var verdadera = Random.Range(0, lugares.Length);
            var sombras = new SombraDelPavor[lugares.Length];
            pavor.Aparecer(lugares[verdadera]);
            pavor.Mostrarse();
            pavor.LlevarLuciernaga(true);
            for (var i = 0; i < lugares.Length; i++)
            {
                if (i != verdadera)
                {
                    sombras[i] = pavor.CrearSombra(lugares[i]);
                }
            }

            yield return new WaitForSeconds(aparicion);

            for (var i = 0; i < lugares.Length; i++)
            {
                var sombra = sombras[i];
                if (i == verdadera)
                {
                    pavor.MirarAlJugador();
                    pavor.Destellar(aviso);
                    yield return new WaitForSeconds(aviso);
                    tajo.Tajo(jefe, pavor.Centro.x, pavor.Suelo, pavor.Mirando);
                }
                else
                {
                    // La deshizo un golpe (a ella o a la verdadera): no ataca.
                    if (sombra == null || sombra.Deshecha)
                    {
                        continue;
                    }

                    sombra.Mirar(jefe.CentroJugador.x >= sombra.Centro.x ? 1 : -1);
                    sombra.Destellar(aviso);
                    yield return new WaitForSeconds(aviso);
                    if (sombra == null || sombra.Deshecha)
                    {
                        continue;
                    }

                    tajo.Tajo(jefe, sombra.Centro.x, pavor.Suelo, sombra.Mirando);
                }

                yield return new WaitForSeconds(tajo.duracionGolpe + intervalo);
            }

            // Las tres jadean: la ventana corta el patrón. Las sombras se deshacen al cerrarse.
            jefe.AbrirVentanaVulnerable(jadeo);
            yield return new WaitForSeconds(0.3f);
        }
    }
}
