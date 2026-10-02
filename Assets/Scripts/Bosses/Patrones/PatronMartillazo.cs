using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Martillazo directo (Centinela Ardiente). El jefe se acerca al jugador, avisa, descarga el
    /// martillo delante de él y lo deja clavado: mientras tanto queda vulnerable (Anexo A: 1,2 s).
    /// Es el momento de devolverle el golpe.
    ///
    /// Con <see cref="golpes"/> &gt; 1 es un combo: tras cada golpe vuelve a echarse encima del
    /// jugador y golpea otra vez con un aviso más corto. El martillo solo se queda clavado en el
    /// último: la Ira no deja respirar hasta que se agota.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: martillazo", fileName = "PatronMartillazo")]
    public class PatronMartillazo : PatronAtaque
    {
        [Tooltip("Distancia horizontal (entre centros) desde la que golpea.")]
        [Min(0f)] public float alcance = 3f;

        [Tooltip("Segundos máximos acercándose; después golpea igualmente.")]
        [Min(0f)] public float tiempoAcercamiento = 2.5f;

        [Tooltip("Tamaño de la zona del golpe, delante del jefe.")]
        public Vector2 tamanoGolpe = new Vector2(2.6f, 2.6f);

        [Min(0.02f)] public float duracionGolpe = 0.15f;

        [Header("Combo")]
        [Tooltip("Martillazos seguidos. Solo el último deja el martillo clavado.")]
        [Min(1)] public int golpes = 1;

        [Tooltip("Aviso de los golpes que siguen al primero: más corto, pero siempre visible.")]
        [Min(0f)] public float avisoEncadenado = 0.35f;

        [Tooltip("Segundos máximos para volver a echarse encima del jugador entre golpe y golpe.")]
        [Min(0f)] public float acercamientoEncadenado = 0.8f;

        [Tooltip("Multiplica su velocidad al echarse encima entre golpes.")]
        [Min(0.1f)] public float impulsoEncadenado = 1.6f;

        [Header("Ventana vulnerable")]
        [Tooltip("Con el martillo clavado el jefe queda vulnerable.")]
        public bool abreVentana = true;

        [Tooltip("Duración de la ventana; 0 = la del JefeData.")]
        [Min(0f)] public float duracionVentana;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            for (var golpe = 0; golpe < golpes; golpe++)
            {
                var primero = golpe == 0;
                yield return Acercarse(jefe, primero ? tiempoAcercamiento : acercamientoEncadenado, primero ? 1f : impulsoEncadenado);

                jefe.Detener();
                jefe.MirarAlJugador();
                var espera = primero ? aviso : avisoEncadenado;
                jefe.Avisar(espera);
                yield return new WaitForSeconds(espera);

                var centro = jefe.PuntoDelante(jefe.MedioAncho + tamanoGolpe.x * 0.5f - 0.3f, tamanoGolpe.y * 0.5f);
                jefe.CrearZona("Martillazo", centro, tamanoGolpe, danio, 0f, duracionGolpe, Vector2.zero, color);
                yield return new WaitForSeconds(duracionGolpe);
            }

            if (abreVentana)
            {
                jefe.AbrirVentanaVulnerable(duracionVentana);
            }

            // El jefe corta el patrón al entrar en la ventana; si no la abre, una breve recuperación.
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator Acercarse(BossController jefe, float segundos, float impulso)
        {
            var limite = Time.time + segundos;
            while (Time.time < limite && jefe.JugadorVivo && jefe.DistanciaAlJugador > alcance)
            {
                jefe.Perseguir(impulso);
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
