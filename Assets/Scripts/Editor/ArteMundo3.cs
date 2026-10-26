using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El arte del Mundo 3 (Miedo), desde el menú y desde «Regenerar todo». Las imágenes llegan
    /// limpias desde <c>Tesis/Imagenes/limpios/Mundo3</c> (con su LEEME) y se copian al proyecto con
    /// <c>Herramientas/traer_arte_mundo3.py</c>; aquí se importan con sus ajustes
    /// (<see cref="Reglas"/>) y se construyen sus clips, sus controladores y sus prefabs:
    /// - los enemigos: el Espectro (<see cref="ConstructorEspectro"/>), el Centinela Ciego
    ///   (<see cref="ConstructorCentinelaCiego"/>) y el Acechador (<see cref="ConstructorAcechador"/>);
    /// - el jefe: el Espectro del Pavor y su lluvia de hojas (<see cref="ConstructorPavor"/>);
    /// - las mecánicas: la antorcha, el cepo, las ramas y la cinta (<see cref="ConstructorMecanicasMundo3"/>);
    /// - el fondo (<see cref="ConstructorFondoMundo3"/>) y el escenario (<see cref="ConstructorEscenarioMundo3"/>);
    /// - los planos de las cinemáticas (<see cref="ConstructorCinematicasNivel3"/>). Las puertas, la verja
    ///   y los checkpoints van con los de los otros mundos (<see cref="ConstructorPuertas"/>).
    ///
    /// Si falta el arte de alguno, avisa y ese se queda con su bloque provisional.
    /// </summary>
    public static class ArteMundo3
    {
        /// <summary>Las reglas de importación de todo el arte del mundo (las aplica <see cref="TablaArte"/>).</summary>
        public static IEnumerable<TablaArte.Regla> Reglas => ConstructorEspectro.Reglas
            .Concat(ConstructorCentinelaCiego.Reglas)
            .Concat(ConstructorAcechador.Reglas)
            .Concat(ConstructorPavor.Reglas)
            .Concat(ConstructorMecanicasMundo3.Reglas)
            .Concat(ConstructorCinematicasNivel3.Reglas);

        [MenuItem("Fragmentum/Niveles/Generar el arte del Mundo 3 (Miedo)", priority = 138)]
        public static void MenuConstruir()
        {
            // Los prefabs del jefe y de las mecánicas se visten con lo que se acaba de generar.
            if (Enemigos() && Jefe() && Mecanicas() && GeneradorJefes.Construir() && GeneradorMecanicas.CrearPrefabs() &&
                ConstructorFondoMundo3.Construir() && ConstructorEscenarioMundo3.Construir())
            {
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(ConstructorAcechador.Prefab);
            }
        }

        /// <summary>El Espectro del Pavor y su lluvia de hojas. Va antes que los prefabs de los jefes, que lo visten.</summary>
        public static bool Jefe() => ConstructorPavor.Construir();

        /// <summary>Las mecánicas. Va antes que los prefabs de las mecánicas, que las llevan.</summary>
        public static bool Mecanicas() => ConstructorMecanicasMundo3.Construir();

        /// <summary>
        /// Los enemigos. Va después del Eco de la Ira (usan su material de destello y el de sus
        /// partículas) y de los prefabs sin arte, que crean sus datos.
        /// </summary>
        public static bool Enemigos()
        {
            var ok = ConstructorEspectro.Construir();
            ok &= ConstructorCentinelaCiego.Construir();
            ok &= ConstructorAcechador.Construir();
            return ok;
        }
    }
}
