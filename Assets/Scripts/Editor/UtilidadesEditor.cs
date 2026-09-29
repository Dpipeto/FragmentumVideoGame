using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Levels;
using Fragmentum.Levels.Escenario;
using Fragmentum.Player;
using Fragmentum.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Piezas comunes de las herramientas que montan prefabs y escenas de prueba: asignar
    /// campos serializados, crear carpetas, bloques de escenario, HUD, cámara y reaparición.
    /// </summary>
    internal static class UtilidadesEditor
    {
        public const string RutaPlantillaEscena = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
        private const string CarpetaSpritesBase = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/";

        public static Sprite SpriteCuadrado => CargarSprite(CarpetaSpritesBase + "Square.png");

        public static Sprite SpriteCapsula => CargarSprite(CarpetaSpritesBase + "Capsule.png");

        public static Sprite SpriteCirculo => CargarSprite(CarpetaSpritesBase + "Circle.png");

        /// <summary>Ejecuta el configurador si falta alguna capa del juego.</summary>
        public static void AsegurarConfiguracion()
        {
            foreach (var capa in Capas.Propias)
            {
                if (LayerMask.NameToLayer(capa) < 0)
                {
                    Debug.Log("[Fragmentum] Faltan capas del proyecto; se ejecuta Configurar proyecto.");
                    ConfiguradorProyecto.ConfigurarTodo();
                    return;
                }
            }
        }

        public static int Capa(string nombre)
        {
            var indice = LayerMask.NameToLayer(nombre);
            if (indice < 0)
            {
                Debug.LogWarning($"[Fragmentum] La capa '{nombre}' no existe; se usa Default. Ejecuta Fragmentum → Configurar proyecto.");
                return 0;
            }

            return indice;
        }

        public static Sprite CargarSprite(string ruta)
        {
            foreach (var objeto in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                if (objeto is Sprite sprite)
                {
                    return sprite;
                }
            }

            Debug.LogWarning($"[Fragmentum] No se encontró el sprite base {ruta}; se usará un placeholder en tiempo de ejecución.");
            return null;
        }

        public static void AsegurarCarpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta))
            {
                return;
            }

            var partes = ruta.Split('/');
            var actual = partes[0];
            for (var i = 1; i < partes.Length; i++)
            {
                var siguiente = $"{actual}/{partes[i]}";
                if (!AssetDatabase.IsValidFolder(siguiente))
                {
                    AssetDatabase.CreateFolder(actual, partes[i]);
                }

                actual = siguiente;
            }
        }

        /// <summary>Asigna un campo serializado (también privado) y avisa si no existe.</summary>
        public static void Asignar(Object objeto, string campo, System.Action<SerializedProperty> asignar)
        {
            if (objeto == null)
            {
                return;
            }

            var serializado = new SerializedObject(objeto);
            var propiedad = serializado.FindProperty(campo);
            if (propiedad == null)
            {
                Debug.LogWarning($"[Fragmentum] {objeto.GetType().Name} no tiene el campo serializado '{campo}'.");
                return;
            }

            asignar(propiedad);
            serializado.ApplyModifiedPropertiesWithoutUndo();
        }

        public static Color Hex(string rgb) =>
            ColorUtility.TryParseHtmlString("#" + rgb, out var color) ? color : Color.magenta;

        /// <summary>
        /// Crea (o regenera) una escena a partir de la plantilla URP 2D, que ya trae cámara y
        /// Global Light 2D. Devuelve null si el usuario cancela.
        ///
        /// Al regenerar se sobrescribe el contenido pero se conserva el <c>.meta</c>: la escena
        /// mantiene su GUID y no se rompen las referencias a ella (Build Settings, LevelDatabase).
        /// </summary>
        public static Scene? CrearEscenaDesdePlantilla(string ruta, bool preguntarSiExiste)
        {
            AsegurarCarpeta(System.IO.Path.GetDirectoryName(ruta)?.Replace('\\', '/'));

            var existe = AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) != null;
            if (existe && preguntarSiExiste && !EditorUtility.DisplayDialog("Fragmentum",
                    $"{ruta} ya existe. ¿Regenerarla? Se perderán los cambios hechos a mano en ella.",
                    "Regenerar", "Cancelar"))
            {
                return null;
            }

            if (existe && System.IO.File.Exists(RutaPlantillaEscena))
            {
                System.IO.File.Copy(RutaPlantillaEscena, ruta, true);
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
                return EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            }

            if (existe)
            {
                AssetDatabase.DeleteAsset(ruta);
            }

            if (AssetDatabase.CopyAsset(RutaPlantillaEscena, ruta))
            {
                return EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            }

            Debug.LogWarning(
                $"[Fragmentum] No se encontró la plantilla {RutaPlantillaEscena}. La escena se crea sin " +
                "Global Light 2D: añade una si los sprites se ven oscuros.");
            return EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        /// <summary>Bloque rectangular de escenario (x e y en unidades del mundo) con BoxCollider2D.</summary>
        public static GameObject Bloque(Transform padre, string nombre, float xMin, float xMax, float yMin, float yMax,
            int capa, Color color, Sprite sprite, string tag = null)
        {
            var objeto = new GameObject(nombre) { layer = capa };
            if (!string.IsNullOrEmpty(tag))
            {
                objeto.tag = tag;
            }

            objeto.transform.SetParent(padre, false);
            objeto.transform.position = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
            objeto.transform.localScale = new Vector3(xMax - xMin, yMax - yMin, 1f);

            if (sprite != null)
            {
                var renderer2d = objeto.AddComponent<SpriteRenderer>();
                renderer2d.sprite = sprite;
                renderer2d.color = color;
            }

            var caja = objeto.AddComponent<BoxCollider2D>();
            caja.size = Vector2.one;
            caja.offset = Vector2.zero;
            return objeto;
        }

        public static GameObject Suelo(Transform padre, string nombre, float xMin, float xMax, float yMin, float yMax, Color color) =>
            Bloque(padre, nombre, xMin, xMax, yMin, yMax, Capa(Capas.Suelo), color, SpriteCuadrado, GameTags.Suelo);

        public static void ZonaMortal(Transform padre, float xMin, float xMax, float y)
        {
            var zona = Bloque(padre, "ZonaMortal", xMin, xMax, y - 2f, y, 0, Color.clear, null);
            zona.GetComponent<BoxCollider2D>().isTrigger = true;
            zona.AddComponent<DeadZone>();
        }

        /// <summary>Instancia el prefab del jugador con las habilidades indicadas desbloqueadas.</summary>
        public static GameObject InstanciarJugador(GameObject prefab, Scene escena, Vector2 pies, params HABILIDADES[] habilidades)
        {
            var jugador = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            jugador.transform.position = pies;
            Asignar(jugador.GetComponent<PlayerAbilities>(), "desbloqueadasAlInicio", p =>
            {
                p.arraySize = habilidades.Length;
                for (var i = 0; i < habilidades.Length; i++)
                {
                    p.GetArrayElementAtIndex(i).enumValueIndex = (int)habilidades[i];
                }
            });
            return jugador;
        }

        public static HABILIDADES[] TodasLasHabilidades => (HABILIDADES[])System.Enum.GetValues(typeof(HABILIDADES));

        /// <summary>
        /// Cámara con seguimiento y límites, y un LevelManager que hace reaparecer al jugador en
        /// el punto de aparición (en las escenas de prueba no hay Game Over).
        /// </summary>
        public static void CamaraYReaparicion(GameObject jugador, Transform aparicion, Vector2 minimo, Vector2 maximo)
        {
            var camara = Object.FindAnyObjectByType<Camera>();
            if (camara == null)
            {
                camara = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
                camara.orthographic = true;
            }

            camara.backgroundColor = Hex("1A0800");
            camara.transform.position = new Vector3(aparicion.position.x, aparicion.position.y + 1.5f, -10f);
            var seguimiento = camara.GetComponent<CameraMovement>();
            if (seguimiento == null)
            {
                seguimiento = camara.gameObject.AddComponent<CameraMovement>();
            }

            Asignar(seguimiento, "objetivo", p => p.objectReferenceValue = jugador.transform);
            Asignar(seguimiento, "minimo", p => p.vector2Value = minimo);
            Asignar(seguimiento, "maximo", p => p.vector2Value = maximo);

            // Sin nivel de la LevelDatabase: no toca los límites ni el fondo de la cámara.
            var gestor = new GameObject("Nivel (LevelManager)").AddComponent<LevelManager>();
            Asignar(gestor, "puntoInicio", p => p.objectReferenceValue = aparicion);
            Asignar(gestor, "limitesDesdeSecciones", p => p.boolValue = false);
            Asignar(gestor, "fondoDesdePaleta", p => p.boolValue = false);
        }

        /// <summary>HUD mínimo: barra de vida del jugador escuchando sus eventos.</summary>
        public static void CrearHud(Health vidaJugador)
        {
            var objetoCanvas = new GameObject("HUD");
            var canvas = objetoCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var escalador = objetoCanvas.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);

            var spriteUi = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var fondo = new GameObject("BarraVida", typeof(RectTransform)).AddComponent<Image>();
            fondo.transform.SetParent(objetoCanvas.transform, false);
            fondo.sprite = spriteUi;
            fondo.type = Image.Type.Sliced;
            fondo.color = new Color(0f, 0f, 0f, 0.6f);
            var rectFondo = fondo.rectTransform;
            rectFondo.anchorMin = rectFondo.anchorMax = rectFondo.pivot = new Vector2(0f, 1f);
            rectFondo.anchoredPosition = new Vector2(20f, -20f);
            rectFondo.sizeDelta = new Vector2(320f, 26f);

            var relleno = new GameObject("Relleno", typeof(RectTransform)).AddComponent<Image>();
            relleno.transform.SetParent(fondo.transform, false);
            relleno.sprite = spriteUi;
            relleno.type = Image.Type.Filled;
            relleno.fillMethod = Image.FillMethod.Horizontal;
            relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
            relleno.color = Hex("8B1A1A");
            var rectRelleno = relleno.rectTransform;
            rectRelleno.anchorMin = Vector2.zero;
            rectRelleno.anchorMax = Vector2.one;
            rectRelleno.offsetMin = new Vector2(3f, 3f);
            rectRelleno.offsetMax = new Vector2(-3f, -3f);

            var barra = fondo.gameObject.AddComponent<LifeBar>();
            Asignar(barra, "vida", p => p.objectReferenceValue = vidaJugador);
            Asignar(barra, "relleno", p => p.objectReferenceValue = relleno);
        }
    }
}
