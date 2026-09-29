using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Onda de suelo (Centinela Ardiente, fase 1). El jefe golpea el suelo y una onda baja
    /// recorre el piso hacia los lados. Hace daño a quien esté en el suelo; se esquiva saltando.
    ///
    /// El Anexo A la describe como un «raycast horizontal». Aquí es una zona de daño que viaja,
    /// para que el jugador la vea venir y pueda saltarla: un rayo instantáneo no se podría esquivar.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: onda de suelo", fileName = "PatronOndaSuelo")]
    public class PatronOndaSuelo : PatronAtaque
    {
        [Min(0.1f)] public float velocidadOnda = 9f;

        [Tooltip("Distancia que recorre cada onda.")]
        [Min(0f)] public float alcance = 16f;

        [Tooltip("Tamaño de la onda: baja, para poder saltarla.")]
        public Vector2 tamanoOnda = new Vector2(0.8f, 0.9f);

        [Tooltip("Una onda a cada lado, o solo hacia el jugador.")]
        public bool haciaAmbosLados = true;

        [Min(0f)] public float recuperacion = 0.6f;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            jefe.Detener();
            jefe.MirarAlJugador();
            jefe.Avisar(aviso);
            yield return new WaitForSeconds(aviso);

            var duracion = alcance / velocidadOnda;
            var y = jefe.SueloY + tamanoOnda.y * 0.5f;
            var salida = jefe.MedioAncho + tamanoOnda.x * 0.5f;

            if (haciaAmbosLados)
            {
                Lanzar(jefe, -1, salida, y, duracion);
                Lanzar(jefe, 1, salida, y, duracion);
            }
            else
            {
                Lanzar(jefe, jefe.Mirando, salida, y, duracion);
            }

            yield return new WaitForSeconds(recuperacion);
        }

        private void Lanzar(BossController jefe, int direccion, float salida, float y, float duracion)
        {
            var origen = new Vector2(jefe.Centro.x + direccion * salida, y);
            jefe.CrearZona("OndaDeSuelo", origen, tamanoOnda, danio, 0f, duracion, new Vector2(direccion * velocidadOnda, 0f), color);
        }
    }
}
