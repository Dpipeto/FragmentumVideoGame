using UnityEngine;

namespace Fragmentum.Levels.Escenario
{
    /// <summary>
    /// Las cuentas del parallax, sin estado, para que <see cref="ParallaxMovement"/>,
    /// <see cref="ParallaxProps"/> y <see cref="RepartoHitos"/> usen las mismas y las pruebas
    /// las comprueben sin escena.
    ///
    /// Convención: la <b>velocidad</b> de una capa es lo que se desplaza en pantalla por cada
    /// unidad que avanza la cámara. 0 = viaja con la cámara (infinitamente lejos, parece no
    /// moverse); 1 = fija en el mundo, como el suelo que pisa el jugador.
    /// </summary>
    public static class CalculoParallax
    {
        /// <summary>
        /// Velocidad de una capa según su profundidad, como en el prototipo:
        /// <c>1 - (z - zCamara) / distanciaMaxima</c>. La capa más lejana (a
        /// <paramref name="distanciaMaxima"/> de la cámara) queda en 0.
        /// </summary>
        public static float VelocidadPorProfundidad(float z, float zCamara, float distanciaMaxima) =>
            distanciaMaxima <= 0f ? 1f : Mathf.Clamp01(1f - (z - zCamara) / distanciaMaxima);

        /// <summary>
        /// X en el mundo de un elemento anclado en <paramref name="ancla"/>: se ve centrado en
        /// pantalla cuando la cámara pasa por su ancla, sea cual sea el punto de partida.
        /// </summary>
        public static float PosicionX(float ancla, float camaraX, float velocidad) =>
            ancla + (camaraX - ancla) * (1f - velocidad);

        /// <summary>
        /// Desplazamiento horizontal de la textura, en repeticiones (0 a 1), de una capa repetible
        /// centrada en la cámara. Divide por el ancho de la textura en el mundo: así dos capas con
        /// texturas de distinto ancho y la misma velocidad se mueven igual en pantalla.
        /// </summary>
        public static float OffsetTextura(float camaraX, float velocidad, float anchoTextura, float anchoCapa) =>
            OffsetTextura(camaraX, velocidad, anchoTextura, anchoCapa, 1f);

        /// <summary>
        /// Lo mismo para una textura que tarda <paramref name="periodo"/> anchos en volver a empezar:
        /// 1 si se repite tal cual y 2 si se repite en espejo (una copia al derecho y otra al revés).
        /// </summary>
        public static float OffsetTextura(float camaraX, float velocidad, float anchoTextura, float anchoCapa, float periodo) =>
            anchoTextura <= 0f ? 0f : Mathf.Repeat((camaraX * velocidad - anchoCapa * 0.5f) / anchoTextura, Mathf.Max(1f, periodo));

        /// <summary>
        /// Tramo de posiciones de la cámara (desde, hasta) en el que se ve al menos una parte de un
        /// elemento de ancho <paramref name="ancho"/> anclado en <paramref name="ancla"/>.
        /// </summary>
        public static Vector2 RecorridoVisible(float ancla, float ancho, float velocidad, float anchoVista)
        {
            if (velocidad <= 0f)
            {
                return new Vector2(float.NegativeInfinity, float.PositiveInfinity);
            }

            var medio = (ancho + anchoVista) * 0.5f / velocidad;
            return new Vector2(ancla - medio, ancla + medio);
        }

        /// <summary>
        /// Ancho que necesita un tramo de la capa para tapar la pantalla entera mientras la cámara
        /// va de <paramref name="desde"/> a <paramref name="hasta"/> (anclado en el punto medio).
        /// </summary>
        public static float AnchoParaCubrir(float desde, float hasta, float velocidad, float anchoVista) =>
            anchoVista + velocidad * Mathf.Abs(hasta - desde);
    }
}
