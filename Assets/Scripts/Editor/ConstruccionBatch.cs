using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Regenera todo lo que las herramientas del proyecto generan: configuración, prefabs,
    /// datos, escenas y diagramas. Sirve desde el menú y en modo batch:
    /// <c>Unity.exe -batchmode -projectPath … -executeMethod Fragmentum.EditorTools.ConstruccionBatch.Todo -quit</c>
    ///
    /// <see cref="Todo"/> no vuelve a crear las escenas de prueba que ya existen: al regenerarlas,
    /// Unity les da identificadores internos nuevos y git las veía cambiadas por completo sin
    /// que cambiara nada. <see cref="TodoForzado"/> (y el menú) sí las regeneran.
    /// </summary>
    public static class ConstruccionBatch
    {
        [MenuItem("Fragmentum/Regenerar todo (prefabs, escenas de prueba y diagramas)", priority = 100)]
        public static void MenuTodo()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Construir(true);
            }
        }

        /// <summary>Regenera todo salvo las escenas de prueba existentes (las crea si faltan).</summary>
        public static void Todo() => Construir(false);

        /// <summary>Regenera todo, también las escenas de prueba existentes.</summary>
        public static void TodoForzado() => Construir(true);

        private static void Construir(bool regenerarEscenasDePrueba)
        {
            ConfiguradorProyecto.ConfigurarTodo();

            // Los ajustes de importación del arte (píxeles por unidad y pivote por carpeta), por
            // si algo se importó antes de que existiera su regla.
            TablaArte.AplicarATodo();

            var ok = GeneradorProgresion.Construir();

            // Los prefabs se regeneran siempre: Unity conserva sus identificadores. El arte de
            // Kael va antes que el prefab del jugador, que lo usa.
            ok &= ConstructorArteKael.Construir();
            ok &= ConstructorJugador.CrearPrefab() != null;
            ok &= GeneradorEnemigos.CrearPrefabs() != null;
            ok &= ConstructorEcoIra.Construir();
            // El Golem, las mecánicas y las cinemáticas del Nivel 1: después del Eco (el Golem usa
            // su material de destello) y antes que los prefabs de las mecánicas, que usan sus clips.
            ok &= ArteNivel1.Construir();
            // El arte de la Centinela va antes que los prefabs de jefes, que lo usan (y usan el
            // material de destello del Eco).
            ok &= ConstructorCentinela.Construir();
            ok &= GeneradorJefes.Construir();
            // Si algo del arte de la Centinela no cuadra (un sprite reimportado sin sus ajustes,
            // un clip o el prefab tocados a mano), la regeneración falla con su nombre.
            ok &= VerificadorCentinela.Verificar();
            ok &= GeneradorMecanicas.CrearPrefabs();
            ok &= ConstructorFondoMundo1.Construir();

            if (regenerarEscenasDePrueba || !Existe(ConstructorJugador.RutaEscena))
            {
                ok &= ConstructorJugador.CrearEscenaPrueba(false);
            }

            if (regenerarEscenasDePrueba || !Existe(GeneradorEnemigos.RutaEscena))
            {
                ok &= GeneradorEnemigos.CrearEscenaPrueba(false);
            }

            ok &= PreparadorNivel.PrepararTodas();
            ExportadorDiagramasFSM.Exportar();
            AssetDatabase.SaveAssets();

            if (!ok)
            {
                Debug.LogError("[Fragmentum] La regeneración falló; revisa los mensajes anteriores.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        private static bool Existe(string rutaEscena) => AssetDatabase.LoadAssetAtPath<SceneAsset>(rutaEscena) != null;
    }
}
