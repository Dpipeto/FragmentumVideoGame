using System.Collections;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Onda de suelo (Centinela Ardiente). El jefe golpea el suelo y una onda baja recorre el piso
    /// hacia los lados. Hace daño a quien esté en el suelo; se esquiva saltando.
    ///
    /// El Anexo A la describe como un «raycast horizontal». Aquí es una zona de daño que viaja,
    /// para que el jugador la vea venir y pueda saltarla: un rayo instantáneo no se podría esquivar.
    ///
    /// Con <see cref="repeticiones"/> &gt; 1 golpea varias veces seguidas: hay que saltar una onda
    /// detrás de otra, sin tiempo para acercarse a devolver el golpe.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: onda de suelo", fileName = "PatronOndaSuelo")]
    public class PatronOndaSuelo : PatronAtaque
    {
        // Segundos que se ve G3 entre dos golpes al suelo seguidos.
        private const float MuestraOnda = 0.2f;

        [Min(0.1f)] public float velocidadOnda = 9f;

        [Tooltip("Distancia que recorre cada onda.")]
        [Min(0f)] public float alcance = 16f;

        [Tooltip("Tamaño de la onda: baja, para poder saltarla.")]
        public Vector2 tamanoOnda = new Vector2(0.8f, 0.9f);

        [Tooltip("El dibujo de la onda (prefab con su animación, mirando a la derecha y con el pivote en su frente). " +
                 "Con él, la zona no se pinta como rectángulo.")]
        public GameObject efecto;

        [Tooltip("Una onda a cada lado, o solo hacia el jugador.")]
        public bool haciaAmbosLados = true;

        [Tooltip("Golpes al suelo seguidos (cada uno lanza sus ondas).")]
        [Min(1)] public int repeticiones = 1;

        [Tooltip("Segundos entre un golpe al suelo y el siguiente.")]
        [Min(0.1f)] public float intervalo = 0.55f;

        [Min(0f)] public float recuperacion = 0.6f;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            jefe.Detener();
            jefe.MirarAlJugador();

            // Con arte, las ondas salen con G3, el fotograma de la onda en el suelo.
            yield return jefe.Golpear(AnimatorParams.GolpeSuelo, aviso);

            for (var i = 0; i < repeticiones; i++)
            {
                if (i > 0)
                {
                    // Con arte, G3 se ve un momento antes del siguiente golpe: es la pose que mata.
                    var muestra = jefe.TieneArte ? Mathf.Min(MuestraOnda, intervalo * 0.5f) : 0f;
                    if (muestra > 0f)
                    {
                        yield return new WaitForSeconds(muestra);
                    }

                    yield return jefe.Golpear(AnimatorParams.GolpeSuelo, intervalo - muestra);
                }

                var y = jefe.SueloY + tamanoOnda.y * 0.5f;
                var salida = jefe.MedioAncho + tamanoOnda.x * 0.5f;
                if (haciaAmbosLados)
                {
                    Lanzar(jefe, -1, salida, y);
                    Lanzar(jefe, 1, salida, y);
                }
                else
                {
                    Lanzar(jefe, jefe.Mirando, salida, y);
                }
            }

            yield return new WaitForSeconds(recuperacion);
        }

        private void Lanzar(BossController jefe, int direccion, float salida, float y)
        {
            var origen = new Vector2(jefe.Centro.x + direccion * salida, y);
            var duracion = RecorridoEnArena(jefe, origen.x, direccion, tamanoOnda.x * 0.5f, alcance) / velocidadOnda;
            var zona = jefe.CrearZona("OndaDeSuelo", origen, tamanoOnda, danio, 0f, duracion, new Vector2(direccion * velocidadOnda, 0f), color,
                dibujarZona && efecto == null);
            zona.Vestir(efecto, direccion);
        }
    }
}
