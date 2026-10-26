using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// La carta (la sentencia): el Juez camina hasta Kael, alza la espada con las dos manos
    /// (<see cref="PatronAtaque.aviso"/> s) y la clava: una onda sale por el suelo hacia los dos lados.
    /// <see cref="golpes"/> veces. Es el ataque que más quita y el que deja más cerca de su marca.
    /// Invisible, Kael se esconde del golpe: el Juez camina hasta donde lo vio y clava allí (la onda sale
    /// igual y hay que saltarla).
    ///
    /// Con <see cref="efecto"/>, la onda se ve dibujada: la tierra que se levanta y rueda por el suelo
    /// (<c>Prompts_Mundo4.md</c>, K2).
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: la sentencia", fileName = "PatronSentencia")]
    public class PatronSentencia : PatronMemoria
    {
        [Min(1)] public int golpes = 2;

        [Tooltip("Lo más que camina hacia Kael antes de clavar.")]
        [Min(0f)] public float tiempoAcercarse = 2.5f;

        [Tooltip("Se para a esta distancia de Kael (entre centros).")]
        [Min(0f)] public float distancia = 1.5f;

        [Tooltip("El golpe de la espada, delante: de ancho y de alto.")]
        public Vector2 tamanoGolpe = new Vector2(2f, 2.5f);

        [Min(0.02f)] public float duracionGolpe = 0.2f;

        [Min(0.1f)] public float velocidadOnda = 8f;

        [Tooltip("Alto de la onda: se salta.")]
        [Min(0.1f)] public float altoOnda = 0.8f;

        [Min(0.1f)] public float anchoOnda = 1f;

        [Min(0.1f)] public float alcanceOnda = 12f;

        [Tooltip("Entre un golpe y el siguiente.")]
        [Min(0f)] public float entreGolpes = 0.6f;

        [Tooltip("¿Se ve el rectángulo del golpe de la espada? Con el arte del Juez, no: la espada clavada ya se ve. " +
                 "La onda, con su dibujo si lo tiene; si no, según «dibujarZona».")]
        public bool dibujarGolpe = true;

        [Tooltip("El dibujo de la onda, mirando a la derecha con el pivote en su frente. Vacío = el rectángulo de su color.")]
        public GameObject efecto;

        protected override IEnumerator Atacar(BossController jefe, JuezSilente juez)
        {
            Gesto(juez, GESTOS_JUEZ.REPOSO);
            for (var g = 0; g < golpes; g++)
            {
                // Camina hasta donde está (o donde lo vio).
                var limite = Time.time + tiempoAcercarse;
                while (Time.time < limite && Mathf.Abs(Kael(jefe, juez).x - jefe.Centro.x) > distancia)
                {
                    jefe.Mover(Kael(jefe, juez).x >= jefe.Centro.x ? 1 : -1, jefe.VelocidadActual);
                    yield return new WaitForFixedUpdate();
                }

                jefe.Detener();
                jefe.Mirar(Kael(jefe, juez).x >= jefe.Centro.x ? 1 : -1);
                jefe.Avisar(aviso);
                Gesto(juez, GESTOS_JUEZ.AVISO_SENTENCIA);
                yield return new WaitForSeconds(aviso);
                Gesto(juez, GESTOS_JUEZ.SENTENCIA);

                // La espada clavada delante y la onda por el suelo, hacia los dos lados.
                var suelo = jefe.SueloY;
                jefe.CrearZona("Sentencia", jefe.PuntoDelante(jefe.MedioAncho + tamanoGolpe.x * 0.5f, tamanoGolpe.y * 0.5f), tamanoGolpe, danio, 0f,
                    duracionGolpe, Vector2.zero, color, dibujarZona && dibujarGolpe);
                foreach (var direccion in new[] { -1, 1 })
                {
                    var desde = jefe.Centro.x + direccion * (jefe.MedioAncho + anchoOnda * 0.5f);
                    var recorrido = RecorridoEnArena(jefe, desde, direccion, anchoOnda * 0.5f, alcanceOnda);
                    if (recorrido <= 0f)
                    {
                        continue;
                    }

                    var onda = jefe.CrearZona("Onda", new Vector2(desde, suelo + altoOnda * 0.5f), new Vector2(anchoOnda, altoOnda), danio, 0f,
                        recorrido / velocidadOnda, new Vector2(direccion * velocidadOnda, 0f), color, dibujarZona && efecto == null);
                    onda.Vestir(efecto, direccion);
                }

                yield return new WaitForSeconds(duracionGolpe);
                Gesto(juez, GESTOS_JUEZ.SACA_ESPADA);
                yield return new WaitForSeconds(entreGolpes);
                // Vuelve al reposo: si tiene que caminar hasta Kael, desde ahí anda.
                Gesto(juez, GESTOS_JUEZ.REPOSO);
            }
        }
    }
}
