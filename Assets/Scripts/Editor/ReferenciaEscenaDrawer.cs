using Fragmentum.Core;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Inspector de <see cref="ReferenciaEscena"/>: un campo donde se arrastra la escena y, debajo,
    /// un aviso si no está en Build Settings (con un botón para añadirla).
    /// </summary>
    [CustomPropertyDrawer(typeof(ReferenciaEscena))]
    public class ReferenciaEscenaDrawer : PropertyDrawer
    {
        private const float AltoAviso = 20f;

        public override float GetPropertyHeight(SerializedProperty propiedad, GUIContent etiqueta)
        {
            var linea = EditorGUIUtility.singleLineHeight;
            return NecesitaAviso(propiedad) ? linea + AltoAviso + 2f : linea;
        }

        public override void OnGUI(Rect zona, SerializedProperty propiedad, GUIContent etiqueta)
        {
            var escena = propiedad.FindPropertyRelative("escena");
            var ruta = propiedad.FindPropertyRelative("ruta");

            EditorGUI.BeginProperty(zona, etiqueta, propiedad);
            var linea = new Rect(zona.x, zona.y, zona.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();
            var nueva = EditorGUI.ObjectField(linea, etiqueta, escena.objectReferenceValue, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                escena.objectReferenceValue = nueva;
                ruta.stringValue = nueva != null ? AssetDatabase.GetAssetPath(nueva) : string.Empty;
            }
            else if (escena.objectReferenceValue != null)
            {
                // La escena se movió o se renombró: se corrige la ruta guardada.
                var actual = AssetDatabase.GetAssetPath(escena.objectReferenceValue);
                if (actual != ruta.stringValue)
                {
                    ruta.stringValue = actual;
                }
            }

            if (NecesitaAviso(propiedad))
            {
                var aviso = new Rect(zona.x + EditorGUIUtility.labelWidth, linea.yMax + 2f,
                    zona.width - EditorGUIUtility.labelWidth - 70f, AltoAviso);
                EditorGUI.HelpBox(aviso, "No está en Build Settings: no se podrá cargar.", MessageType.Warning);

                var boton = new Rect(aviso.xMax + 4f, aviso.y, 66f, AltoAviso);
                if (GUI.Button(boton, "Añadir"))
                {
                    GeneradorProgresion.AnadirABuild(ruta.stringValue);
                }
            }

            EditorGUI.EndProperty();
        }

        private static bool NecesitaAviso(SerializedProperty propiedad)
        {
            var ruta = propiedad.FindPropertyRelative("ruta").stringValue;
            if (string.IsNullOrEmpty(ruta))
            {
                return false;
            }

            foreach (var escena in EditorBuildSettings.scenes)
            {
                if (escena.path == ruta && escena.enabled)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
