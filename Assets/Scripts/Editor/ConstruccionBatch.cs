using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Regenera todo lo que las herramientas del proyecto generan: configuración, prefabs,
    /// datos, escenas de prueba y diagramas. Sirve desde el menú y en modo batch:
    /// <c>Unity.exe -batchmode -projectPath … -executeMethod Fragmentum.EditorTools.ConstruccionBatch.Todo -quit</c>
    /// </summary>
    public static class ConstruccionBatch
    {
        [MenuItem("Fragmentum/Regenerar todo (prefabs, escenas de prueba y diagramas)", priority = 100)]
        public static void MenuTodo()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Todo();
            }
        }

        public static void Todo()
        {
            ConfiguradorProyecto.ConfigurarTodo();

            var ok = GeneradorProgresion.Construir();
            ok &= ConstructorJugador.CrearEscenaPrueba(false);
            ok &= GeneradorEnemigos.CrearEscenaPrueba(false);
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
    }
}
