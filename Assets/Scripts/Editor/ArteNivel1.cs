using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El arte del Golem de Ceniza y de las mecánicas del Nivel 1, desde el menú y desde
    /// «Regenerar todo»: importa los fotogramas con los ajustes de <see cref="TablaArte"/> y
    /// construye los clips y los controladores de <see cref="ConstructorGolem"/> y de
    /// <see cref="ConstructorMecanicas"/>.
    /// </summary>
    public static class ArteNivel1
    {
        [MenuItem("Fragmentum/Niveles/Generar los clips y los Animators del Golem y de las mecánicas", priority = 134)]
        public static void MenuConstruir()
        {
            if (Construir())
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(ConstructorGolem.RutaControlador));
            }
        }

        /// <summary>Construye todo. Devuelve false si algo falló (si falta arte, solo avisa).</summary>
        public static bool Construir()
        {
            var ok = ConstructorGolem.Construir();
            ok &= ConstructorMecanicas.Construir();
            AssetDatabase.SaveAssets();
            return ok;
        }
    }
}
