using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El arte del Mundo 2 (Tristeza), desde el menú y desde «Regenerar todo». Las imágenes llegan
    /// limpias desde <c>Tesis/Imagenes/limpios/Mundo2</c> (con su LEEME) y se copian al proyecto con
    /// <c>Herramientas/traer_arte_mundo2.py</c>; aquí se importan con sus ajustes
    /// (<see cref="Reglas"/>) y se construyen sus clips, sus controladores y sus prefabs:
    /// - los enemigos: el Lamento (<see cref="ConstructorLamento"/>), la Escultura
    ///   (<see cref="ConstructorEscultura"/>), la Medusa (<see cref="ConstructorMedusa"/>) y la
    ///   Plañidera (<see cref="ConstructorPlanidera"/>).
    ///
    /// Si falta el arte de alguno, avisa y ese se queda con su bloque provisional.
    /// </summary>
    public static class ArteMundo2
    {
        /// <summary>Las reglas de importación de todo el arte del mundo (las aplica <see cref="TablaArte"/>).</summary>
        public static IEnumerable<TablaArte.Regla> Reglas => ConstructorLamento.Reglas
            .Concat(ConstructorEscultura.Reglas)
            .Concat(ConstructorMedusa.Reglas)
            .Concat(ConstructorPlanidera.Reglas)
            .Concat(ConstructorDoncella.Reglas)
            .Concat(ConstructorMecanicasMundo2.Reglas)
            .Concat(ConstructorCinematicasNivel2.Reglas);

        [MenuItem("Fragmentum/Niveles/Generar el arte del Mundo 2 (Tristeza)", priority = 135)]
        public static void MenuConstruir()
        {
            // Los prefabs de los jefes se visten con lo que se acaba de generar.
            if (Enemigos() && Jefe() && Mecanicas() && GeneradorJefes.Construir() && GeneradorMecanicas.CrearPrefabs())
            {
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(ConstructorPlanidera.Prefab);
            }
        }

        /// <summary>La Doncella del Lamento. Va antes que los prefabs de los jefes, que la visten.</summary>
        public static bool Jefe() => ConstructorDoncella.Construir();

        /// <summary>
        /// Las mecánicas y los efectos. Va antes que los datos de los jefes (la ola de la Doncella) y
        /// que los prefabs de las mecánicas (la placa y el tapón), que los llevan.
        /// </summary>
        public static bool Mecanicas() => ConstructorMecanicasMundo2.Construir();

        /// <summary>
        /// Los enemigos. Va después del Eco de la Ira (usan su material de destello y el de sus
        /// partículas) y de los prefabs sin arte, que crean sus datos.
        /// </summary>
        public static bool Enemigos()
        {
            var ok = ConstructorLamento.Construir();
            ok &= ConstructorEscultura.Construir();
            ok &= ConstructorMedusa.Construir();
            ok &= ConstructorPlanidera.Construir();
            return ok;
        }
    }
}
