using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// La orden (la radio): el Juez señala al frente con la espada y caen <see cref="explosiones"/>
    /// explosiones de artillería una tras otra, avanzando por la plaza desde su lado hacia donde vio a
    /// Kael. Cada una marca el suelo <see cref="marca"/> s antes. Se responde quedándose entre dos marcas
    /// o saltando, y llegando a la radio.
    ///
    /// Con <see cref="efecto"/>, cada una se ve dibujada: la marca en el suelo mientras avisa y la
    /// explosión al caer (<c>Prompts_Mundo4.md</c>, C2).
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: la orden", fileName = "PatronOrden")]
    public class PatronOrden : PatronMemoria
    {
        [Min(1)] public int explosiones = 6;

        [Tooltip("Lo que avanza cada explosión respecto a la anterior.")]
        [Min(0.1f)] public float separacion = 2.2f;

        [Tooltip("Lo que tarda en caer la siguiente.")]
        [Min(0f)] public float entreExplosiones = 0.6f;

        [Tooltip("Lo que marca el suelo antes de estallar.")]
        [Min(0f)] public float marca = 0.8f;

        [Min(0.1f)] public float ancho = 1.8f;

        [Min(0.1f)] public float alto = 3f;

        [Min(0.02f)] public float duracionGolpe = 0.3f;

        [Tooltip("El dibujo de cada explosión (la marca y el estallido). Vacío = el rectángulo de su color.")]
        public GameObject efecto;

        protected override IEnumerator Atacar(BossController jefe, JuezSilente juez)
        {
            jefe.Detener();
            var kael = Kael(jefe, juez);
            var direccion = kael.x >= jefe.Centro.x ? 1 : -1;
            jefe.Mirar(direccion);
            jefe.Avisar(aviso);
            Gesto(juez, GESTOS_JUEZ.AVISO_ORDEN);
            yield return new WaitForSeconds(Mathf.Max(0f, aviso - marca));
            Gesto(juez, GESTOS_JUEZ.ORDEN);

            var limites = jefe.Limites;
            var desde = jefe.Centro.x + direccion * (jefe.MedioAncho + ancho * 0.5f);
            for (var i = 0; i < explosiones; i++)
            {
                var x = desde + direccion * i * separacion;
                if (x - ancho * 0.5f < limites.x || x + ancho * 0.5f > limites.y)
                {
                    break;
                }

                var zona = jefe.CrearZona("Orden", new Vector2(x, jefe.SueloY + alto * 0.5f), new Vector2(ancho, alto), danio, marca,
                    duracionGolpe, Vector2.zero, color, dibujarZona && efecto == null);
                zona.Vestir(efecto, 0);
                yield return new WaitForSeconds(entreExplosiones);
            }

            yield return new WaitForSeconds(marca + duracionGolpe);
        }
    }
}
