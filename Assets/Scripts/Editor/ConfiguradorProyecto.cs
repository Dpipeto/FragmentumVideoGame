using System.Collections.Generic;
using System.Text;
using Fragmentum.Core;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Deja el proyecto configurado como pide el Anexo A: crea los tags y las capas del juego
    /// y ajusta la matriz de colisión 2D.
    ///
    /// Es idempotente: se puede ejecutar las veces que haga falta y solo añade lo que falte.
    /// </summary>
    public static class ConfiguradorProyecto
    {
        private const string RutaTagManager = "ProjectSettings/TagManager.asset";
        private const string RutaFisica2D = "ProjectSettings/Physics2DSettings.asset";

        /// <summary>Primer índice de capa editable por el usuario en Unity.</summary>
        private const int PrimeraCapaLibre = 6;

        /// <summary>
        /// Parejas de capas que NO deben colisionar.
        /// Las tres primeras vienen del Anexo A; las otras evitan que las zonas de daño
        /// choquen con el escenario o entre ellas.
        /// </summary>
        private static readonly (string a, string b)[] SinColision =
        {
            (Capas.PlayerWeapon, Capas.Player),
            (Capas.EnemyWeapon, Capas.Enemy),
            (Capas.Player, Capas.Player),
            (Capas.PlayerWeapon, Capas.EnemyWeapon),
            (Capas.PlayerWeapon, Capas.Suelo),
            (Capas.PlayerWeapon, Capas.Wall),
            (Capas.EnemyWeapon, Capas.Suelo),
            (Capas.EnemyWeapon, Capas.Wall)
        };

        [MenuItem("Fragmentum/Configurar proyecto/Todo", priority = 0)]
        public static void ConfigurarTodo()
        {
            var informe = new StringBuilder();
            informe.AppendLine(ConfigurarTagsYCapas());
            informe.AppendLine(ConfigurarMatrizDeColision());
            Debug.Log($"[Fragmentum] Configuración del proyecto:\n{informe}");
        }

        [MenuItem("Fragmentum/Configurar proyecto/Tags y capas", priority = 1)]
        public static void MenuTagsYCapas() => Debug.Log($"[Fragmentum] {ConfigurarTagsYCapas()}");

        [MenuItem("Fragmentum/Configurar proyecto/Matriz de colisión 2D", priority = 2)]
        public static void MenuMatriz() => Debug.Log($"[Fragmentum] {ConfigurarMatrizDeColision()}");

        /// <summary>Crea los tags y las capas que falten. Devuelve un informe legible.</summary>
        public static string ConfigurarTagsYCapas()
        {
            var activos = AssetDatabase.LoadAllAssetsAtPath(RutaTagManager);
            if (activos == null || activos.Length == 0)
            {
                return "No se pudo abrir TagManager.asset; crea los tags y las capas a mano.";
            }

            var tagManager = new SerializedObject(activos[0]);
            var informe = new StringBuilder();

            var tagsAnadidos = AnadirTags(tagManager, GameTags.Propios);
            var capasAnadidas = AnadirCapas(tagManager, Capas.Propias);

            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Capas.LimpiarCache();

            informe.Append("Tags: ");
            informe.Append(tagsAnadidos.Count == 0 ? "sin cambios." : string.Join(", ", tagsAnadidos) + " añadidos.");
            informe.Append(" Capas: ");
            informe.Append(capasAnadidas.Count == 0 ? "sin cambios." : string.Join(", ", capasAnadidas) + " añadidas.");
            return informe.ToString();
        }

        /// <summary>Aplica la matriz de colisión 2D del Anexo A. Devuelve un informe legible.</summary>
        public static string ConfigurarMatrizDeColision()
        {
            var faltantes = new List<string>();
            foreach (var nombre in Capas.Propias)
            {
                if (LayerMask.NameToLayer(nombre) < 0)
                {
                    faltantes.Add(nombre);
                }
            }

            if (faltantes.Count > 0)
            {
                return $"Faltan las capas {string.Join(", ", faltantes)}. Ejecuta antes 'Tags y capas'.";
            }

            // Se aplica sobre la configuración viva y se persiste en Physics2DSettings.asset.
            foreach (var (a, b) in SinColision)
            {
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer(a), LayerMask.NameToLayer(b), true);
            }

            var activos = AssetDatabase.LoadAllAssetsAtPath(RutaFisica2D);
            if (activos != null && activos.Length > 0)
            {
                EditorUtility.SetDirty(activos[0]);
                AssetDatabase.SaveAssets();
            }

            var comprobadas = 0;
            var sinAplicar = new List<string>();
            foreach (var (a, b) in SinColision)
            {
                comprobadas++;
                if (!Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer(a), LayerMask.NameToLayer(b)))
                {
                    sinAplicar.Add($"{a} x {b}");
                }
            }

            if (sinAplicar.Count > 0)
            {
                return $"Matriz de colisión: {sinAplicar.Count} de {comprobadas} parejas no se pudieron aplicar " +
                       $"({string.Join("; ", sinAplicar)}). Desmárcalas a mano en Project Settings > Physics 2D.";
            }

            return $"Matriz de colisión: {comprobadas} parejas desactivadas correctamente.";
        }

        private static List<string> AnadirTags(SerializedObject tagManager, IEnumerable<string> tags)
        {
            var anadidos = new List<string>();
            var propiedad = tagManager.FindProperty("tags");
            if (propiedad == null)
            {
                return anadidos;
            }

            foreach (var tag in tags)
            {
                var existe = false;
                for (var i = 0; i < propiedad.arraySize; i++)
                {
                    if (propiedad.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        existe = true;
                        break;
                    }
                }

                if (existe)
                {
                    continue;
                }

                propiedad.InsertArrayElementAtIndex(propiedad.arraySize);
                propiedad.GetArrayElementAtIndex(propiedad.arraySize - 1).stringValue = tag;
                anadidos.Add(tag);
            }

            return anadidos;
        }

        private static List<string> AnadirCapas(SerializedObject tagManager, IEnumerable<string> capas)
        {
            var anadidas = new List<string>();
            var propiedad = tagManager.FindProperty("layers");
            if (propiedad == null)
            {
                return anadidas;
            }

            foreach (var capa in capas)
            {
                var existe = false;
                for (var i = 0; i < propiedad.arraySize; i++)
                {
                    if (propiedad.GetArrayElementAtIndex(i).stringValue == capa)
                    {
                        existe = true;
                        break;
                    }
                }

                if (existe)
                {
                    continue;
                }

                var colocada = false;
                for (var i = PrimeraCapaLibre; i < propiedad.arraySize; i++)
                {
                    var elemento = propiedad.GetArrayElementAtIndex(i);
                    if (!string.IsNullOrEmpty(elemento.stringValue))
                    {
                        continue;
                    }

                    elemento.stringValue = capa;
                    anadidas.Add($"{capa} (capa {i})");
                    colocada = true;
                    break;
                }

                if (!colocada)
                {
                    Debug.LogWarning($"[Fragmentum] No queda hueco libre para la capa '{capa}'.");
                }
            }

            return anadidas;
        }
    }
}
