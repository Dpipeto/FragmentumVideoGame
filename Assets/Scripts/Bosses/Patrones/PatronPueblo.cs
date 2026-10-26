using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// El pueblo (la campana): el Juez golpea la balanza con la empuñadura, suena como una campana y una
    /// ola de fuego de <see cref="alto"/> u barre la plaza a ras de suelo, primero desde un lado y después
    /// desde el otro. Se salta.
    ///
    /// Con <see cref="efecto"/>, la ola se ve dibujada: un muro de fuego bajo que avanza
    /// (<c>Prompts_Mundo4.md</c>, C3).
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: el pueblo", fileName = "PatronPueblo")]
    public class PatronPueblo : PatronMemoria
    {
        [Min(1)] public int olas = 2;

        [Min(0.1f)] public float velocidad = 8f;

        [Tooltip("Alto de la ola: se salta con un salto.")]
        [Min(0.1f)] public float alto = 1.2f;

        [Min(0.1f)] public float ancho = 1f;

        [Tooltip("Lo que espera tras una ola antes de soltar la siguiente.")]
        [Min(0f)] public float entreOlas = 0.4f;

        [Tooltip("El dibujo de la ola, mirando a la derecha con el pivote en su frente. Vacío = el rectángulo de su color.")]
        public GameObject efecto;

        protected override IEnumerator Atacar(BossController jefe, JuezSilente juez)
        {
            jefe.Detener();
            jefe.Avisar(aviso);
            Gesto(juez, GESTOS_JUEZ.AVISO_PUEBLO);
            yield return new WaitForSeconds(aviso);
            Gesto(juez, GESTOS_JUEZ.PUEBLO);

            var limites = jefe.Limites;
            var recorrido = Mathf.Max(0f, limites.y - limites.x - ancho);
            var duracion = recorrido / velocidad;
            for (var i = 0; i < olas; i++)
            {
                // La primera, desde la pared de la izquierda; la segunda, desde la de la derecha.
                var direccion = i % 2 == 0 ? 1 : -1;
                var desde = direccion > 0 ? limites.x + ancho * 0.5f : limites.y - ancho * 0.5f;
                var zona = jefe.CrearZona("Pueblo", new Vector2(desde, jefe.SueloY + alto * 0.5f), new Vector2(ancho, alto), danio, 0f,
                    duracion, new Vector2(direccion * velocidad, 0f), color, dibujarZona && efecto == null);
                zona.Vestir(efecto, direccion);
                yield return new WaitForSeconds(duracion + entreOlas);
            }
        }
    }
}
