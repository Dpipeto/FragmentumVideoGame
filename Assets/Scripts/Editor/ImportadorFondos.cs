using UnityEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Fija los ajustes de importación de todo lo que entra en <c>Assets/Sprites/Fondos</c>: filtro
    /// Point, sin compresión, sin mipmaps y sin reescalar. A las imágenes de una capa del fondo les
    /// da además su tipo (ver <see cref="ConstructorFondoMundo1.AjustarImportador"/>).
    ///
    /// Se aplican en cada importación, así que un cambio hecho a mano en el inspector se pierde al
    /// pulsar Apply: para cambiar un ajuste, cámbialo en <see cref="ConstructorFondoMundo1"/>.
    /// </summary>
    internal sealed class ImportadorFondos : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ConstructorFondoMundo1.CarpetaFondos + "/") || !(assetImporter is TextureImporter importador))
            {
                return;
            }

            ConstructorFondoMundo1.AjustarImportador(importador, ConstructorFondoMundo1.CapaDeRuta(assetPath));
        }
    }
}
