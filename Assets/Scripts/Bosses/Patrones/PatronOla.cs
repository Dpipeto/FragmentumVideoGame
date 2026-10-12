using System.Collections;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Ola (Doncella del Lamento). Barre el brazo hacia Kael y una ola a ras del hielo cruza la
    /// arena entera: nace en la pared que la Doncella tiene a la espalda, pasa bajo ella y se
    /// rompe contra la pared de enfrente. Se esquiva saltándola o esperándola sobre una placa.
    ///
    /// Es la onda de suelo de la Centinela con otro origen: aquella sale de los pies del jefe, y
    /// este jefe flota. La ola avisa primero donde nace (translúcida, sin daño) y luego corre.
    ///
    /// Con <see cref="olas"/> &gt; 1 lanza varias seguidas: hay que saltarlas con ritmo.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: ola", fileName = "PatronOla")]
    public class PatronOla : PatronAtaque
    {
        [Min(0.1f)] public float velocidadOla = 8f;

        [Tooltip("Tamaño de la ola: baja, para poder saltarla.")]
        public Vector2 tamanoOla = new Vector2(1.2f, 1f);

        [Tooltip("Olas seguidas.")]
        [Min(1)] public int olas = 1;

        [Tooltip("Segundos entre una ola y la siguiente.")]
        [Min(0.1f)] public float intervalo = 0.8f;

        [Min(0f)] public float recuperacion = 0.6f;

        [Tooltip("El dibujo de la ola (prefab con su animación, mirando a la derecha y con el pivote en su frente). " +
                 "Con él, la zona no se pinta como rectángulo.")]
        public GameObject efecto;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            jefe.Detener();
            for (var i = 0; i < olas; i++)
            {
                if (i > 0)
                {
                    yield return new WaitForSeconds(intervalo);
                }

                jefe.MirarAlJugador();
                jefe.Avisar(aviso);
                (jefe as DoncellaLamento)?.Gesto(AnimatorParams.Ola, aviso);
                Lanzar(jefe, jefe.Mirando);
            }

            // La última todavía avisa: el patrón dura hasta que sale.
            yield return new WaitForSeconds(aviso + recuperacion);
        }

        private void Lanzar(BossController jefe, int direccion)
        {
            var limites = jefe.Limites;
            var medioAncho = tamanoOla.x * 0.5f;
            var x = (direccion > 0 ? limites.x : limites.y) + direccion * medioAncho;
            var suelo = jefe is DoncellaLamento doncella ? doncella.Suelo : jefe.SueloPisableEn(x);
            var origen = new Vector2(x, suelo + tamanoOla.y * 0.5f);
            var duracion = RecorridoEnArena(jefe, x, direccion, medioAncho, float.MaxValue) / velocidadOla;
            var zona = jefe.CrearZona("Ola", origen, tamanoOla, danio, aviso, duracion, new Vector2(direccion * velocidadOla, 0f), color,
                dibujarZona && efecto == null);
            zona.Vestir(efecto, direccion);
        }
    }
}
