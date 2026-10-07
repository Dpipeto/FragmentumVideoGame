using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El arte del Golem de Ceniza, de las mecánicas y de las cinemáticas del jefe del Nivel 1,
    /// desde el menú y desde «Regenerar todo»: importa los fotogramas con los ajustes de
    /// <see cref="TablaArte"/>, construye los clips, los controladores y el prefab del Golem
    /// (<see cref="ConstructorGolem"/>), los clips y los controladores de las mecánicas
    /// (<see cref="ConstructorMecanicas"/>; sus prefabs los arma <see cref="GeneradorMecanicas"/>)
    /// y pone los planos en sus cinemáticas (<see cref="ConstructorCinematicasNivel1"/>). Desde el
    /// menú, al acabar lo revisa <see cref="VerificadorNivel1"/>.
    /// </summary>
    public static class ArteNivel1
    {
        [MenuItem("Fragmentum/Niveles/Generar el arte del Golem, las mecánicas y las cinemáticas del Nivel 1", priority = 134)]
        public static void MenuConstruir()
        {
            if (Construir() && GeneradorMecanicas.CrearPrefabs() && VerificadorNivel1.Verificar())
            {
                EditorGUIUtility.PingObject(ConstructorGolem.Prefab);
            }
        }

        /// <summary>Construye todo salvo los prefabs de las mecánicas. Devuelve false si algo falló (si falta arte, solo avisa).</summary>
        public static bool Construir()
        {
            var ok = ConstructorGolem.Construir();
            ok &= ConstructorMecanicas.Construir();
            ok &= ConstructorCinematicasNivel1.Construir();
            AssetDatabase.SaveAssets();
            return ok;
        }
    }
}
