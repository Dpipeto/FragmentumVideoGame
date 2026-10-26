using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// El arte de una <see cref="FranjaInundable"/> (las franjas del suelo que inunda y congela la
    /// Doncella del Lamento): con qué se dibuja cada momento. Es un asset de datos: lo crea
    /// <c>ConstructorMecanicasMundo2</c> con las imágenes y lo llevan las franjas de la arena.
    /// Sin él, la franja se dibuja con rectángulos de color.
    ///
    /// Todo se repite a lo ancho de la franja: las grietas del aviso, los grupos de púas y las
    /// losas de la capa de hielo, repartidos; la superficie del agua, como una cinta.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Mecánicas/Arte de la franja inundable", fileName = "ArteDeFranja")]
    public class ArteDeFranja : ScriptableObject
    {
        [Header("El aviso de la inundación: sale agua por las grietas")]
        [Tooltip("En orden: el último se ve cuando el agua ya va a subir.")]
        public Sprite[] aviso = new Sprite[0];

        [Tooltip("Cada cuántas unidades hay una grieta.")]
        [Min(0.5f)] public float separacionAviso = 3f;

        [Header("El agua")]
        [Tooltip("La superficie, en bucle. Se repite a lo ancho; su pivote está en la línea del agua.")]
        public Sprite[] superficie = new Sprite[0];

        [Min(0.02f)] public float segundosSuperficie = 0.25f;

        [Tooltip("Lo que mide la superficie por debajo de la línea del agua: ahí empieza el cuerpo liso.")]
        [Min(0f)] public float fondoSuperficie = 0.72f;

        [Tooltip("El cuerpo del agua: el color de la última fila de la superficie, translúcido.")]
        public Color colorAgua = new Color(0.11f, 0.17f, 0.27f, 0.5f);

        [Header("Las púas de hielo")]
        [Tooltip("La escarcha del suelo mientras el agua se pone blanca.")]
        public Sprite puasAviso;

        public Sprite puasBrota;

        [Tooltip("Mientras hacen daño, alternan.")]
        public Sprite[] puasQuietas = new Sprite[0];

        public Sprite puasRompe;

        [Tooltip("Ancho de un grupo de púas: cada cuántas unidades se repite.")]
        [Min(0.5f)] public float anchoPuas = 2.45f;

        [Min(0.02f)] public float segundosBrota = 0.08f;

        [Min(0.02f)] public float segundosQuieta = 0.12f;

        [Tooltip("Lo que se ven rompiéndose cuando ya no hacen daño.")]
        [Min(0f)] public float segundosRompe = 0.25f;

        [Header("La capa de hielo")]
        public Sprite capaSeForma;

        public Sprite capaEntera;

        [Tooltip("Los últimos instantes: avisa de que se va a deshacer.")]
        public Sprite capaSeDerrite;

        [Tooltip("Ancho de una losa: se reparten para cubrir la franja de punta a punta.")]
        [Min(0.5f)] public float anchoLosa = 2.66f;

        [Min(0f)] public float segundosFormarse = 0.15f;

        [Min(0f)] public float segundosDerretirse = 0.6f;

        /// <summary>¿Están todas las imágenes? Si falta alguna, la franja usa sus rectángulos.</summary>
        public bool Completo =>
            Llena(aviso) && Llena(superficie) && Llena(puasQuietas) &&
            puasAviso != null && puasBrota != null && puasRompe != null &&
            capaSeForma != null && capaEntera != null && capaSeDerrite != null;

        private static bool Llena(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return false;
            }

            foreach (var sprite in sprites)
            {
                if (sprite == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
