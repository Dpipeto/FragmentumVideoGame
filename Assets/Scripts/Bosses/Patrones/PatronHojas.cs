using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Hojas (Espectro del Pavor). Aparece en el lado de la arena contrario a Kael, alza el brazo y
    /// avisa con su destello; en
    /// cuatro o cinco franjas el suelo brilla en verde (el aviso de las zonas, que aún no hacen
    /// daño) y en ellas caen hojas negras afiladas. La primera franja cae bajo Kael. Se responde
    /// saliendo de lo que brilla, como las erupciones de la Centinela.
    ///
    /// Con <see cref="efecto"/>, cada franja se ve dibujada (el suelo que brilla entre las hojas
    /// muertas y las hojas que caen, <see cref="LluviaDeHojas"/>) en vez del rectángulo.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: hojas", fileName = "PatronHojas")]
    public class PatronHojas : PatronAtaque
    {
        [Tooltip("Franjas, como poco…")]
        [Min(1)] public int franjasMinimo = 4;

        [Tooltip("…y como mucho.")]
        [Min(1)] public int franjasMaximo = 5;

        [Tooltip("Ancho de cada franja que brilla.")]
        [Min(0.2f)] public float ancho = 2f;

        [Tooltip("Hueco mínimo entre dos franjas: siempre queda dónde ponerse.")]
        [Min(0f)] public float hueco = 1f;

        [Tooltip("Alto de la lluvia de hojas, desde el suelo.")]
        [Min(0.2f)] public float alto = 5f;

        [Tooltip("Lo que caen las hojas, tras el aviso.")]
        [Min(0.05f)] public float duracion = 0.6f;

        [Min(0f)] public float recuperacion = 0.6f;

        [Tooltip("Lo lejos de la pared que aparece para alzar el brazo, en el lado contrario a Kael.")]
        [Min(0f)] public float margen = 3f;

        [Tooltip("El dibujo de cada franja (un prefab que sigue las fases de su zona). Vacío = el rectángulo.")]
        public GameObject efecto;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            jefe.Detener();
            var pavor = jefe as EspectroDelPavor;
            var limites = jefe.Limites;
            var kael = jefe.CentroJugador.x;
            if (pavor != null)
            {
                var centro = (limites.x + limites.y) * 0.5f;
                pavor.Aparecer(kael <= centro ? limites.y - margen : limites.x + margen);
                pavor.Destellar(aviso, GESTOS_PAVOR.AVISO_HOJAS);
            }
            else
            {
                jefe.Avisar(aviso);
            }

            var suelo = pavor != null ? pavor.Suelo : jefe.SueloY;
            var cuantas = Random.Range(franjasMinimo, Mathf.Max(franjasMinimo, franjasMaximo) + 1);
            foreach (var x in Franjas(limites, kael, cuantas, ancho, hueco))
            {
                var zona = jefe.CrearZona("Hojas", new Vector2(x, suelo + alto * 0.5f), new Vector2(ancho, alto), danio, aviso, duracion,
                    Vector2.zero, color, dibujarZona && efecto == null);
                zona.Vestir(efecto, 0);
                if (pavor != null)
                {
                    pavor.VerEnLaOscuridad(zona);
                }
            }

            // Al acabar el aviso, lanza las hojas hacia arriba.
            yield return new WaitForSeconds(aviso);
            if (pavor != null)
            {
                pavor.Gesto(GESTOS_PAVOR.HOJAS);
            }

            yield return new WaitForSeconds(duracion + recuperacion);
            if (pavor != null)
            {
                pavor.Ocultarse();
            }
        }

        /// <summary>
        /// Los centros de las franjas: la primera bajo Kael y las demás repartidas al azar por la
        /// arena, sin pisarse y con <paramref name="hueco"/> entre ellas. Si no caben todas, las que
        /// quepan.
        /// </summary>
        public static List<float> Franjas(Vector2 limites, float kael, int cuantas, float ancho, float hueco)
        {
            var mitad = ancho * 0.5f;
            var minimo = limites.x + mitad;
            var maximo = limites.y - mitad;
            var franjas = new List<float>();
            if (minimo > maximo || cuantas <= 0)
            {
                return franjas;
            }

            franjas.Add(Mathf.Clamp(kael, minimo, maximo));
            for (var intento = 0; franjas.Count < cuantas && intento < 200; intento++)
            {
                var x = Random.Range(minimo, maximo);
                var libre = true;
                foreach (var otra in franjas)
                {
                    if (Mathf.Abs(otra - x) < ancho + hueco)
                    {
                        libre = false;
                        break;
                    }
                }

                if (libre)
                {
                    franjas.Add(x);
                }
            }

            return franjas;
        }
    }
}
