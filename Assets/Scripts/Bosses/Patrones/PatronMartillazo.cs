using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Martillazo directo (Centinela Ardiente, fase 1). El jefe se acerca al jugador, avisa,
    /// descarga el martillo delante de él y lo deja clavado: mientras tanto queda vulnerable
    /// (Anexo A: 1,2 s). Es el momento de devolverle el golpe.
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

        [Tooltip("Con el martillo clavado el jefe queda vulnerable.")]
        public bool abreVentana = true;

        [Tooltip("Duración de la ventana; 0 = la del JefeData.")]
        [Min(0f)] public float duracionVentana;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            var limite = Time.time + tiempoAcercamiento;
            while (Time.time < limite && jefe.JugadorVivo && jefe.DistanciaAlJugador > alcance)
            {
                jefe.Perseguir();
                yield return new WaitForFixedUpdate();
            }

            jefe.Detener();
            jefe.MirarAlJugador();
            jefe.Avisar(aviso);
            yield return new WaitForSeconds(aviso);

            var centro = jefe.PuntoDelante(jefe.MedioAncho + tamanoGolpe.x * 0.5f - 0.3f, tamanoGolpe.y * 0.5f);
            jefe.CrearZona("Martillazo", centro, tamanoGolpe, danio, 0f, duracionGolpe, Vector2.zero, color);
            yield return new WaitForSeconds(duracionGolpe);

            if (abreVentana)
            {
                jefe.AbrirVentanaVulnerable(duracionVentana);
            }

            // El jefe corta el patrón al entrar en la ventana; si no la abre, una breve recuperación.
            yield return new WaitForSeconds(0.3f);
        }
    }
}
