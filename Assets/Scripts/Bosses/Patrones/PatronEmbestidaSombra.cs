using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Embestida de sombra (Espectro del Pavor). Aparece junto a la pared más lejana a Kael, a su
    /// altura (a ras de suelo o, si Kael está en una rama, a la de esa rama:
    /// <see cref="EspectroDelPavor.AlturaDeKael"/>), avisa con su destello y cruza la arena entera
    /// (<see cref="alto"/> u, a <see cref="velocidad"/> u/s) hasta la pared de enfrente. Al otro lado
    /// se queda <see cref="visibleAlFinal"/> s a la vista: se le puede golpear. Se esquiva saltándola,
    /// o bajándose de la rama si viene por ella.
    ///
    /// Con otro jefe, la embestida sale de donde esté hacia la pared que tiene delante.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: embestida de sombra", fileName = "PatronEmbestidaSombra")]
    public class PatronEmbestidaSombra : PatronAtaque
    {
        [Min(0.1f)] public float velocidad = 16f;

        [Tooltip("Alto de la embestida, desde donde pisa Kael: bajo, para poder saltarla.")]
        [Min(0.1f)] public float alto = 1.6f;

        [Tooltip("Al otro lado se queda a la vista estos segundos.")]
        [Min(0f)] public float visibleAlFinal = 1f;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            jefe.Detener();
            var pavor = jefe as EspectroDelPavor;
            float desde, hasta, suelo;
            if (pavor != null)
            {
                // Busca a Kael: si está en una rama, embiste por ella.
                suelo = pavor.AlturaDeKael();
                desde = pavor.ParedParaLaEmbestida();
                pavor.Aparecer(desde, suelo);
                hasta = pavor.ParedDeEnfrente(desde);
                pavor.Mirar(hasta >= desde ? 1 : -1);
                pavor.Destellar(aviso, GESTOS_PAVOR.AVISO_EMBESTIDA);
            }
            else
            {
                desde = jefe.Centro.x;
                jefe.MirarAlJugador();
                var limites = jefe.Limites;
                hasta = jefe.Mirando > 0 ? limites.y - jefe.MedioAncho : limites.x + jefe.MedioAncho;
                jefe.Avisar(aviso);
                suelo = jefe.SueloY;
            }

            yield return new WaitForSeconds(aviso);

            var direccion = hasta >= desde ? 1 : -1;
            var duracion = Mathf.Abs(hasta - desde) / velocidad;
            var tamano = new Vector2(jefe.MedioAncho * 2f, alto);
            var zona = jefe.CrearZona("Embestida", new Vector2(desde, suelo + alto * 0.5f), tamano, danio, 0f, duracion,
                new Vector2(direccion * velocidad, 0f), color, dibujarZona);
            if (pavor != null)
            {
                pavor.VerEnLaOscuridad(zona);
                pavor.Cruzar(hasta, velocidad);
            }

            yield return new WaitForSeconds(duracion + visibleAlFinal);
            if (pavor != null)
            {
                pavor.Ocultarse();
            }
        }
    }
}
