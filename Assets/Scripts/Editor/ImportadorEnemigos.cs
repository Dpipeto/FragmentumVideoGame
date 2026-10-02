using UnityEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Fija los ajustes de importación de los fotogramas del Eco de la Ira
    /// (<c>Assets/Sprites/Enemigos/EcoIra</c>): sprite único, filtro Point, sin compresión ni
    /// mipmaps, 708 px por unidad y el pivote común de los pies. Ver
    /// <see cref="ConstructorEcoIra.AjustarImportador"/>.
    ///
    /// Se aplican en cada importación, así que un cambio hecho a mano en el inspector se pierde al
    /// pulsar Apply: para cambiar un ajuste, cámbialo en <see cref="ConstructorEcoIra"/>.
    /// </summary>
    internal sealed class ImportadorEnemigos : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(ConstructorEcoIra.CarpetaSprites + "/") && assetImporter is TextureImporter importador)
            {
                ConstructorEcoIra.AjustarImportador(importador);
            }
        }
    }
}
