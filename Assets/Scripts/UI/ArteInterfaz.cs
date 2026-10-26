using UnityEngine;

namespace Fragmentum.UI
{
    /// <summary>
    /// Las imágenes de la interfaz que se dibujan con arte. El HUD se construye por código, así que
    /// no tiene dónde arrastrarlas: están en un asset de <c>Resources</c> que el HUD carga al
    /// construirse. Lo crea <c>ConstructorMecanicasMundo2</c>. Sin él (o sin una imagen), cada
    /// pieza del HUD usa su versión provisional.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Interfaz/Arte de la interfaz", fileName = "ArteInterfaz")]
    public class ArteInterfaz : ScriptableObject
    {
        /// <summary>Nombre del asset dentro de una carpeta <c>Resources</c>.</summary>
        public const string Recurso = "ArteInterfaz";

        [Tooltip("La burbuja de aire, de llena a vacía (mundo de la Tristeza): sustituye a la barra de aire.")]
        public Sprite[] oxigeno = new Sprite[0];

        /// <summary>¿Están todos los fotogramas de la burbuja?</summary>
        public bool TieneOxigeno
        {
            get
            {
                if (oxigeno == null || oxigeno.Length == 0)
                {
                    return false;
                }

                foreach (var fotograma in oxigeno)
                {
                    if (fotograma == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>El arte de la interfaz, o null si todavía no se ha generado.</summary>
        public static ArteInterfaz Cargar() => Resources.Load<ArteInterfaz>(Recurso);

        /// <summary>
        /// El fotograma de la burbuja para lo que queda de aire (de 0 a 1): el primero, llena; el
        /// último, vacía. Con cinco: llena, tres cuartos, media, un cuarto y a punto de romperse.
        /// </summary>
        public static int FotogramaDeAire(float fraccion, int cuantos) =>
            cuantos <= 0 ? -1 : Mathf.Clamp(Mathf.RoundToInt((1f - Mathf.Clamp01(fraccion)) * (cuantos - 1)), 0, cuantos - 1);
    }
}
