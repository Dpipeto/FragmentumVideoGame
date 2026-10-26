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

            // La cámara de las cinemáticas en pixel art, antes que los sistemas, que la usan.
            var ok = ConstructorCamaraCinematica.CrearPrefab() != null;
            ok &= GeneradorProgresion.Construir();

            // Los prefabs se regeneran siempre: Unity conserva sus identificadores. El arte de
            // Kael va antes que el prefab del jugador, que lo usa.
            ok &= ConstructorArteKael.Construir();
            ok &= ConstructorJugador.CrearPrefab() != null;
            ok &= GeneradorEnemigos.CrearPrefabs() != null;
            ok &= ConstructorEcoIra.Construir();
            // El Golem, las mecánicas y las cinemáticas del Nivel 1: después del Eco (el Golem usa
            // su material de destello) y antes que los prefabs de las mecánicas, que usan sus clips.
            ok &= ArteNivel1.Construir();
            // Los enemigos del Mundo 2: también después del Eco, por sus materiales.
            ok &= ArteMundo2.Enemigos();
            ok &= ConstructorCinematicasNivel2.Construir();
            // Las cinemáticas de salida de cada mundo (sus planos y las frases del cierre).
            ok &= ConstructorPuertas.Construir();
            // El arte de la Centinela va antes que los prefabs de jefes, que lo usan (y usan el
            // material de destello del Eco).
            ok &= ConstructorCentinela.Construir();
            // Lo mismo con el de la Doncella del Lamento.
            ok &= ArteMundo2.Jefe();
            // Los efectos de sus ataques (la onda), antes que los patrones, que los llevan.
            ok &= ConstructorEfectos.Construir();
            // Las mecánicas y los efectos del Mundo 2: antes que los patrones de la Doncella (su
            // ola) y que los prefabs de las mecánicas (la placa y el tapón).
            ok &= ArteMundo2.Mecanicas();
            ok &= GeneradorJefes.Construir();
            // Lo que vale cada enemigo, cada jefe y el tiempo de cada nivel, ya con todos sus assets.
            ok &= TablaPuntaje.Aplicar();
            // Si algo del arte de la Centinela no cuadra (un sprite reimportado sin sus ajustes,
            // un clip o el prefab tocados a mano), la regeneración falla con su nombre.
            ok &= VerificadorCentinela.Verificar();
            // La Doncella solo se hiere mientras llora: si una fase se queda sin llanto, no se la
            // podría vencer. La regeneración falla antes de que eso llegue al juego.
            ok &= VerificadorDoncella.Verificar();
            // El Espectro del Pavor solo se hiere cuando se deja ver: si una fase se queda sin su
            // ventana, o su destello no se viera a oscuras, no se le podría vencer.
            ok &= VerificadorPavor.Verificar();
            ok &= GeneradorMecanicas.CrearPrefabs();
            // Lo mismo con el arte del Nivel 1 (el Golem, las mecánicas y los planos del jefe), ya
            // con los prefabs de las mecánicas hechos.
            ok &= VerificadorNivel1.Verificar();
            ok &= ConstructorFondoMundo1.Construir();
            ok &= ConstructorFondoMundo2.Construir();
            ok &= ConstructorEscenarioMundo2.Construir();
            ok &= VerificadorNivel2.Verificar();

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
