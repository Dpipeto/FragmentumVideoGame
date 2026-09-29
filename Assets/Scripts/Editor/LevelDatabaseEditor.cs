using Fragmentum.Data;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Inspector de la <see cref="LevelDatabase"/>: además de los campos, muestra los problemas
    /// de configuración (escenas fuera de Build Settings, ids repetidos o desordenados, niveles
    /// sin emoción) y permite generar Build Settings a partir de la base de datos.
    /// </summary>
    [CustomEditor(typeof(LevelDatabase))]
    public class LevelDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var bd = (LevelDatabase)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validación", EditorStyles.boldLabel);

            var problemas = bd.Validar();
            if (problemas.Count == 0)
            {
                EditorGUILayout.HelpBox("Todo correcto: el menú, el selector y los niveles con escena se pueden cargar.", MessageType.Info);
            }
            else
            {
                foreach (var problema in problemas)
                {
                    EditorGUILayout.HelpBox(problema, MessageType.Warning);
                }
            }

            var enConstruccion = 0;
            foreach (var nivel in bd.niveles)
            {
                if (nivel != null && !nivel.escena.Asignada) enConstruccion++;
            }

            if (enConstruccion > 0)
            {
                EditorGUILayout.HelpBox($"{enConstruccion} nivel(es) sin escena: el selector los muestra «en construcción».", MessageType.None);
            }

            if (GUILayout.Button("Configurar Build Settings desde esta base de datos"))
            {
                GeneradorProgresion.ConfigurarBuildSettings(bd);
            }
        }
    }
}
