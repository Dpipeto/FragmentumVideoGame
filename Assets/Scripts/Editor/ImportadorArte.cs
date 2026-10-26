using UnityEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El único postprocessor del arte: fija los ajustes de importación de todo lo que entra en
    /// <c>Assets/Sprites</c> y tiene regla, en cada importación.
    /// - Los sprites, según <see cref="TablaArte"/>: pixel art, con los píxeles por unidad y el
    ///   pivote de su carpeta (la Centinela, el Eco, el Golem, las mecánicas y las cinemáticas
    ///   del Nivel 1).
    /// - Las capas del fondo de <c>Assets/Sprites/Fondos</c>, según la capa a la que pertenecen
    ///   (<see cref="ConstructorFondoMundo1.AjustarImportador"/>): no son sprites sueltos.
    ///
    /// Antes había uno por jefes, enemigos y fondos; ahora todo se decide en un sitio. Lo que no
    /// tiene regla (Kael, la cinemática inicial) lo ajusta su constructor al regenerar.
    ///
    /// Y la música de <c>Assets/Audio/Musica</c>, según <see cref="MusicaNiveles.AjustarImportador"/>.
    /// </summary>
    internal sealed class ImportadorArte : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (assetImporter is AudioImporter importador && assetPath.StartsWith(MusicaNiveles.Carpeta + "/"))
            {
                MusicaNiveles.AjustarImportador(importador, assetPath);
            }
            else if (assetImporter is AudioImporter efecto && assetPath.StartsWith(SonidosDelJuego.Carpeta + "/"))
            {
                SonidosDelJuego.AjustarImportador(efecto);
            }
        }

        private void OnPreprocessTexture()
        {
            if (!(assetImporter is TextureImporter importador))
            {
                return;
            }

            if (assetPath.StartsWith(ConstructorFondoMundo2.CarpetaArte + "/"))
            {
                ConstructorFondoMundo2.AjustarImportador(importador, ConstructorFondoMundo2.CapaDeRuta(assetPath));
                return;
            }

            if (assetPath.StartsWith(ConstructorFondoMundo3.CarpetaArte + "/"))
            {
                ConstructorFondoMundo2.AjustarImportador(importador, ConstructorFondoMundo3.CapaDeRuta(assetPath));
                return;
            }

            if (assetPath.StartsWith(ConstructorFondoMundo4.CarpetaArte + "/"))
            {
                ConstructorFondoMundo2.AjustarImportador(importador, ConstructorFondoMundo4.CapaDeRuta(assetPath));
                return;
            }

            if (assetPath.StartsWith(ConstructorFondoMundo1.CarpetaFondos + "/"))
            {
                ConstructorFondoMundo1.AjustarImportador(importador, ConstructorFondoMundo1.CapaDeRuta(assetPath));
                return;
            }

            TablaArte.Ajustar(importador);
        }
    }
}
