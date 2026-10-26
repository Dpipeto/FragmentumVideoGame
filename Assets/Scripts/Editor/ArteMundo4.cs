using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El arte del Mundo 4 (Culpa), desde el menú y desde «Regenerar todo». Las imágenes llegan limpias
    /// desde <c>Tesis/Imagenes/limpios/Mundo4</c> (con su LEEME) y se copian al proyecto con
    /// <c>Herramientas/traer_arte_mundo4.py</c>; aquí se importan con sus ajustes (<see cref="Reglas"/>)
    /// y se construyen sus clips, sus controladores y sus prefabs:
    /// - los enemigos: el Penitente (<see cref="ConstructorPenitente"/>), el Artillero
    ///   (<see cref="ConstructorArtillero"/>) y el Verdugo (<see cref="ConstructorVerdugo"/>);
    /// - el jefe: el Juez Silente (<see cref="ConstructorJuez"/>);
    /// - lo que lanzan y lo que dejan: la explosión, la artillería, la ola de fuego, la placa, la bala, el
    ///   garfio y la cadena (<see cref="ConstructorEfectosMundo4"/>);
    /// - de la segunda tanda (2026-10-22): las mecánicas y las piezas sueltas (el barril, la mina, el agua del
    ///   río, la alambrada, las coberturas, el campanario, las memorias y su losa: <see cref="ConstructorMecanicasMundo4"/>)
    ///   y el tileset y los props (<see cref="ConstructorEscenarioMundo4"/>), con los que <c>PreparadorNivelCulpa</c>
    ///   viste la escena;
    /// - el fondo (<see cref="ConstructorFondoMundo4"/>) y las cinemáticas del Juez y de las cuatro memorias
    ///   (<see cref="ConstructorCinematicasNivel4"/>).
    ///
    /// Lo que falta (las puertas, la verja, el checkpoint y la salida) llegará con sus prompts
    /// (<c>Prompts_Mundo4.md</c>, apartado I). Si falta el arte de alguno, avisa y ese se queda con su bloque
    /// provisional.
    /// </summary>
    public static class ArteMundo4
    {
        /// <summary>Las reglas de importación de todo el arte del mundo (las aplica <see cref="TablaArte"/>).</summary>
        public static IEnumerable<TablaArte.Regla> Reglas => ConstructorPenitente.Reglas
            .Concat(ConstructorArtillero.Reglas)
            .Concat(ConstructorVerdugo.Reglas)
            .Concat(ConstructorJuez.Reglas)
            .Concat(ConstructorEfectosMundo4.Reglas)
            .Concat(ConstructorMecanicasMundo4.Reglas)
            .Concat(ConstructorCinematicasNivel4.Reglas);

        [MenuItem("Fragmentum/Niveles/Generar el arte del Mundo 4 (Culpa)", priority = 139)]
        public static void MenuConstruir()
        {
            // Los efectos van antes que los enemigos (la bala, el garfio) y que el jefe y las mecánicas,
            // que los llevan; los prefabs del jefe y de las mecánicas se visten con lo que se acaba de generar.
            if (Efectos() && Enemigos() && Jefe() && Mecanicas() && GeneradorJefes.Construir() && GeneradorMecanicas.CrearPrefabs() &&
                ConstructorEscenarioMundo4.Construir() && ConstructorFondoMundo4.Construir() && ConstructorCinematicasNivel4.Construir())
            {
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(ConstructorVerdugo.Prefab);
            }
        }

        /// <summary>Lo que lanzan y lo que dejan. Va antes que los enemigos, el jefe y las mecánicas, que los llevan.</summary>
        public static bool Efectos() => ConstructorEfectosMundo4.Construir();

        /// <summary>Las mecánicas y las piezas sueltas. Va antes que los prefabs de las mecánicas (el barril y la mina).</summary>
        public static bool Mecanicas() => ConstructorMecanicasMundo4.Construir();

        /// <summary>El Juez Silente. Va antes que los prefabs de los jefes, que lo visten.</summary>
        public static bool Jefe() => ConstructorJuez.Construir();

        /// <summary>
        /// Los enemigos. Va después del Eco de la Ira (usan su material de destello y el de sus
        /// partículas), de los prefabs sin arte, que crean sus datos, y de los efectos.
        /// </summary>
        public static bool Enemigos()
        {
            var ok = ConstructorPenitente.Construir();
            ok &= ConstructorArtillero.Construir();
            ok &= ConstructorVerdugo.Construir();
            return ok;
        }
    }
}
