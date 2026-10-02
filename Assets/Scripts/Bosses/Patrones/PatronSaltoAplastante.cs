using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Salto aplastante (Centinela Ardiente, ataque de furia). El jefe se agacha, salta hacia donde
    /// está el jugador y cae con todo su peso: el sitio de la caída se marca en el suelo desde que
    /// despega, y al aterrizar quema ese tramo y suelta una onda baja hacia cada lado.
    ///
    /// Se esquiva en dos tiempos: apartarse de la marca mientras el jefe está en el aire y saltar
    /// la onda al caer. Lo que hace daño es la zona, no el cuerpo del jefe en el aire.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: salto aplastante", fileName = "PatronSaltoAplastante")]
    public class PatronSaltoAplastante : PatronAtaque
    {
        [Tooltip("Segundos en el aire: es lo que tiene el jugador para apartarse de la marca.")]
        [Min(0.2f)] public float tiempoVuelo = 0.8f;

        [Tooltip("Ancho y alto del tramo que quema al caer.")]
        public Vector2 tamanoImpacto = new Vector2(3f, 1.4f);

        [Min(0.02f)] public float duracionImpacto = 0.15f;

        [Header("Ondas al caer")]
        [Tooltip("Al aterrizar suelta una onda baja hacia cada lado (se esquiva saltando).")]
        public bool ondas = true;

        [Min(0)] public int danioOnda = 1;

        [Min(0.1f)] public float velocidadOnda = 8f;

        [Tooltip("Distancia que recorre cada onda.")]
        [Min(0f)] public float alcanceOnda = 7f;

        public Vector2 tamanoOnda = new Vector2(0.8f, 0.8f);

        [Min(0f)] public float recuperacion = 0.7f;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            // Se agacha: el aviso de siempre.
            jefe.Detener();
            jefe.MirarAlJugador();
            jefe.Avisar(aviso);
            yield return new WaitForSeconds(aviso);

            // La marca aparece al despegar y empieza a quemar justo al caer.
            var destino = jefe.Saltar(jefe.CentroJugador.x, tiempoVuelo);
            var suelo = jefe.SueloEn(destino);
            jefe.CrearZona("Aterrizaje", new Vector2(destino, suelo + tamanoImpacto.y * 0.5f), tamanoImpacto, danio,
                tiempoVuelo, duracionImpacto, Vector2.zero, color);

            // Espera a tocar suelo (con un límite, por si algo lo frena en el aire).
            yield return new WaitForSeconds(tiempoVuelo * 0.5f);
            var limite = Time.time + tiempoVuelo * 1.5f;
            while (Time.time < limite && !jefe.EnSuelo)
            {
                yield return new WaitForFixedUpdate();
            }

            jefe.Detener();
            if (ondas)
            {
                var y = jefe.SueloY + tamanoOnda.y * 0.5f;
                var salida = jefe.MedioAncho + tamanoOnda.x * 0.5f;
                var duracion = alcanceOnda / velocidadOnda;
                foreach (var direccion in new[] { -1, 1 })
                {
                    var origen = new Vector2(jefe.Centro.x + direccion * salida, y);
                    jefe.CrearZona("OndaDelSalto", origen, tamanoOnda, danioOnda, 0f, duracion, new Vector2(direccion * velocidadOnda, 0f), color);
                }
            }

            yield return new WaitForSeconds(recuperacion);
        }
    }
}
