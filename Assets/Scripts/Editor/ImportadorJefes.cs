using UnityEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Fija los ajustes de importación de todo lo que entra en <c>Assets/Sprites/Jefes</c>:
    /// - a cualquier jefe, los de pixel art (<see cref="ArteJefes.Ajustar"/>);
    /// - a la Centinela Ardiente, además, sus píxeles por unidad y el pivote común de los pies
    ///   (<see cref="ConstructorCentinela.AjustarImportador"/>).
    ///
    /// Cuando otro jefe tenga arte, se añade aquí su carpeta con los ajustes de su constructor.
    /// Hasta entonces sus sprites quedan como pixel art, con los píxeles por unidad y el pivote por
    /// defecto.
    ///
    /// Se aplican en cada importación, así que un cambio hecho a mano en el inspector se pierde al
    /// pulsar Apply: para cambiar un ajuste, cámbialo en el constructor del jefe.
    /// </summary>
    internal sealed class ImportadorJefes : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArteJefes.Carpeta + "/") || !(assetImporter is TextureImporter importador))
            {
                return;
            }

            if (assetPath.StartsWith(ConstructorCentinela.CarpetaSprites + "/"))
            {
                ConstructorCentinela.AjustarImportador(importador);
            }
            else
            {
                ArteJefes.Ajustar(importador);
            }
        }
    }
}
