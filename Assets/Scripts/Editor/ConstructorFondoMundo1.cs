using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Data;
using Fragmentum.Levels.Escenario;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static Fragmentum.EditorTools.UtilidadesEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Monta el fondo con parallax del Mundo 1 (Ira, Fortaleza Volcánica) a partir de las seis
    /// imágenes de <see cref="CarpetaArte"/>:
    /// 1. les aplica los ajustes de importación de pixel art (también lo hace
    ///    <see cref="ImportadorArte"/> cada vez que se importan);
    /// 2. crea un material URP/Unlit transparente por cada capa repetible, porque los shaders 2D
    ///    de sprites ignoran el desplazamiento de textura;
    /// 3. arma el prefab <see cref="RutaPrefab"/> con las seis capas, sus velocidades y su orden de
    ///    dibujo, y los hitos repartidos para el Nivel 1.
    ///
    /// <see cref="PreparadorNivel"/> lo coloca en las escenas de nivel del mundo de la Ira y reparte
    /// los hitos según la longitud de cada una.
    ///
    /// Es idempotente: sin cambios en las imágenes, volver a ejecutarlo no cambia nada.
    /// </summary>
    public static class ConstructorFondoMundo1
    {
        public const string CarpetaFondos = "Assets/Sprites/Fondos";
        public const string CarpetaArte = CarpetaFondos + "/Mundo1Ira";
        public const string CarpetaMateriales = CarpetaArte + "/Materiales";
        public const string RutaPrefab = "Assets/Prefabs/Fondos/FondoMundo1.prefab";
        public const string ShaderCapas = "Universal Render Pipeline/Unlit";
        public const string RutaMaterialSprites = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        /// <summary>
        /// Alto de la vista para el que se monta el fondo: el de la cámara del Nivel 1 (tamaño
        /// ortográfico 4,85). Con otra cámara las capas se ajustan solas.
        /// </summary>
        public const float AltoReferencia = 9.7f;

        /// <summary>Alto de las imágenes: cada una ocupa justo la altura de la pantalla.</summary>
        public const int AltoArte = 768;

        /// <summary>Unos 79,2 px por unidad: así 768 px ocupan la vista de referencia.</summary>
        public const float PixelesPorUnidad = AltoArte / AltoReferencia;

        /// <summary>Lado máximo de las texturas: el fondo lejano y el primer plano miden 2752 px.</summary>
        public const int TamanoMaximo = 4096;

        /// <summary>La extensión del Nivel 1: el prefab trae los hitos repartidos para ella.</summary>
        public static readonly Vector2 ExtensionReferencia = new Vector2(0f, 80f);

        /// <summary>Ancho de las capas repetibles en el prefab, para verlo en el editor (pantalla 16:9).</summary>
        public static float AnchoCapaReferencia => AltoReferencia * RepartoHitos.AspectoReferencia * ParallaxMovement.MargenAncho;

        public enum TECNICAS_FONDO
        {
            /// <summary>Quad con textura que se repite; la mueve <see cref="ParallaxMovement"/>.</summary>
            REPETIBLE,

            /// <summary>Sprite suelto (un hito); lo mueve <see cref="ParallaxProps"/>.</summary>
            PROP,

            /// <summary>Sprite en modo Tiled, del ancho del tramo; lo mueve <see cref="ParallaxProps"/>.</summary>
            TRAMO,
        }

        /// <summary>Una capa del fondo: de qué imagen sale, a qué profundidad va y cómo se mueve.</summary>
        public sealed class CapaFondo
        {
            public string Nombre;
            public string Archivo;
            public TECNICAS_FONDO Tecnica;
            public float Z;

            /// <summary>
            /// La de la tabla del montaje. Las repetibles la calculan desde su Z; las demás la llevan
            /// en su <see cref="ParallaxProps"/>, con el mismo valor que daría su Z.
            /// </summary>
            public float Velocidad;

            /// <summary>Orden de dibujo: siempre por debajo del juego, que usa de −2 en adelante.</summary>
            public int Orden;

            public SpriteAlignment Pivote = SpriteAlignment.Center;

            /// <summary>Y local del sprite: su altura en pantalla con la vista de referencia (0 = centro).</summary>
            public float AlturaEnPantalla;

            public string Ruta => $"{CarpetaArte}/{Archivo}";
        }

        public static readonly CapaFondo Cielo = new CapaFondo
        {
            Nombre = "Cielo", Archivo = "Skie.png", Tecnica = TECNICAS_FONDO.REPETIBLE, Z = 40f, Velocidad = 0f, Orden = -60,
        };

        public static readonly CapaFondo FondoLejano = new CapaFondo
        {
            Nombre = "FondoLejano", Archivo = "Fondo lejano tileable.png", Tecnica = TECNICAS_FONDO.REPETIBLE, Z = 30f, Velocidad = 0.2f, Orden = -50,
        };

        public static readonly CapaFondo Fortaleza = new CapaFondo
        {
            Nombre = "Fortaleza", Archivo = "Plano medio.png", Tecnica = TECNICAS_FONDO.PROP, Z = 20f, Velocidad = 0.4f, Orden = -40,
        };

        public static readonly CapaFondo Muro = new CapaFondo
        {
            Nombre = "Muro", Archivo = "Muro tileable.png", Tecnica = TECNICAS_FONDO.TRAMO, Z = 10f, Velocidad = 0.6f, Orden = -30,
        };

        // Apoyada en el borde inferior: su base queda tapada por las rocas del primer plano.
        public static readonly CapaFondo Catarata = new CapaFondo
        {
            Nombre = "Catarata", Archivo = "Prop catarata.png", Tecnica = TECNICAS_FONDO.PROP, Z = 10f, Velocidad = 0.6f, Orden = -29,
            Pivote = SpriteAlignment.BottomCenter, AlturaEnPantalla = -AltoReferencia * 0.5f,
        };

        public static readonly CapaFondo PrimerPlano = new CapaFondo
        {
            Nombre = "PrimerPlano", Archivo = "Primer plano tileable.png", Tecnica = TECNICAS_FONDO.REPETIBLE, Z = 0f, Velocidad = 0.8f, Orden = -10,
        };

        /// <summary>Las seis capas, de la más lejana a la más cercana.</summary>
        public static readonly CapaFondo[] CapasFondo = { Cielo, FondoLejano, Fortaleza, Muro, Catarata, PrimerPlano };

        // Reparto por defecto, en fracciones del nivel (ver RepartoHitos). En el Nivel 1 (0 a 80 u):
        // la fortaleza se ve centrada al final de la sección 1 y la catarata cae sobre el muro, que
        // tapa la arena del jefe (el interior de la fortaleza). La catarata necesita una pared de la
        // que caer: en campo abierto no encajaba con nada y se quitó.
        public static readonly float[] PosicionesFortaleza = { 0.3f };
        public static readonly float[] PosicionesCataratas = { 0.85f };
        public static readonly Vector2[] TramosMuro = { new Vector2(0.75f, 1f) };

        // --- Menú y modo batch ---

        [MenuItem("Fragmentum/Escenario/Importar el fondo del Mundo 1 y regenerar su prefab", priority = 140)]
        public static void MenuConstruir() => Construir();

        /// <summary>
        /// Solo el fondo, sin tocar escenas:
        /// <c>-executeMethod Fragmentum.EditorTools.ConstructorFondoMundo1.ConstruirEnModoBatch -quit</c>
        /// </summary>
        public static void ConstruirEnModoBatch()
        {
            var ok = Construir();
            AssetDatabase.SaveAssets();
            if (!ok && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Fragmentum/Niveles/Repartir los hitos del fondo en la escena abierta", priority = 132)]
        public static void MenuRepartir()
        {
            var escena = SceneManager.GetActiveScene();
            var reparto = Object.FindAnyObjectByType<RepartoHitos>();
            if (reparto == null)
            {
                EditorUtility.DisplayDialog("Fragmentum",
                    $"La escena no tiene un fondo con RepartoHitos. Arrastra {RutaPrefab} a la escena y vuelve a intentarlo.", "Entendido");
                return;
            }

            var nivel = PreparadorNivel.NivelDe(escena);
            if (nivel == null)
            {
                EditorUtility.DisplayDialog("Fragmentum",
                    $"La escena '{escena.name}' no es un nivel de la LevelDatabase: no se sabe cuánto mide.", "Entendido");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(reparto.gameObject, "Repartir los hitos del fondo");
            Repartir(reparto, escena, nivel);
            EditorSceneManager.MarkSceneDirty(escena);
            Debug.Log($"[Fragmentum] Hitos del fondo repartidos para {nivel.Titulo}. Guarda la escena para conservarlos.");
        }

        /// <summary>
        /// Importa las imágenes, crea los materiales y regenera el prefab. Devuelve false solo si
        /// algo falló; si faltan imágenes no es un error: avisa y no genera el prefab.
        /// </summary>
        public static bool Construir()
        {
            var faltan = CapasFondo.Where(c => AssetDatabase.LoadAssetAtPath<Texture2D>(c.Ruta) == null).Select(c => c.Ruta).ToList();
            if (faltan.Count > 0)
            {
                Debug.LogWarning($"[Fragmentum] Faltan imágenes del fondo del Mundo 1: {string.Join(", ", faltan)}. No se genera su prefab.");
                return true;
            }

            foreach (var capa in CapasFondo)
            {
                var importador = (TextureImporter)AssetImporter.GetAtPath(capa.Ruta);
                if (AjustarImportador(importador, capa))
                {
                    importador.SaveAndReimport();
                }
            }

            AsegurarCarpeta(CarpetaMateriales);
            var materiales = new Dictionary<CapaFondo, Material>();
            foreach (var capa in CapasFondo.Where(c => c.Tecnica == TECNICAS_FONDO.REPETIBLE))
            {
                materiales[capa] = CrearMaterial(capa);
                if (materiales[capa] == null)
                {
                    return false;
                }
            }

            var prefab = CrearPrefab(materiales);
            AssetDatabase.SaveAssets();
            if (prefab == null)
            {
                Debug.LogError($"[Fragmentum] No se pudo guardar {RutaPrefab}.");
                return false;
            }

            Debug.Log($"[Fragmentum] Fondo del Mundo 1: {CapasFondo.Length} capas importadas y {RutaPrefab}.");
            return true;
        }

        // --- 1. Importación ---

        /// <summary>La capa de una imagen de los fondos, o null si no es de ninguna.</summary>
        public static CapaFondo CapaDeRuta(string ruta) => CapasFondo.FirstOrDefault(c => c.Ruta == ruta);

        /// <summary>
        /// Ajustes de pixel art para cualquier imagen de <see cref="CarpetaFondos"/>: filtro Point,
        /// sin compresión ni mipmaps y <b>sin reescalar</b> (la opción «Non-Power of 2» viene en
        /// «To Nearest» y convertiría 2752×768 en 2048×1024). Si la imagen es de una capa, además
        /// su tipo: textura que se repite en horizontal o sprite. Devuelve si cambió algo.
        /// </summary>
        public static bool AjustarImportador(TextureImporter importador, CapaFondo capa)
        {
            var antes = new TextureImporterSettings();
            importador.ReadTextureSettings(antes);
            var ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);

            ajustes.filterMode = FilterMode.Point;
            ajustes.mipmapEnabled = false;
            ajustes.alphaIsTransparency = true;
            ajustes.npotScale = TextureImporterNPOTScale.None;
            ajustes.readable = false;

            if (capa != null && capa.Tecnica == TECNICAS_FONDO.REPETIBLE)
            {
                ajustes.textureType = TextureImporterType.Default;
                ajustes.textureShape = TextureImporterShape.Texture2D;
                // Se repite en horizontal. En vertical, Clamp: con Repeat y filtro Point podría
                // asomar una línea de la fila contraria en el borde de arriba o de abajo.
                ajustes.wrapModeU = TextureWrapMode.Repeat;
                ajustes.wrapModeV = TextureWrapMode.Clamp;
                ajustes.wrapModeW = TextureWrapMode.Clamp;
            }
            else if (capa != null)
            {
                ajustes.textureType = TextureImporterType.Sprite;
                ajustes.spriteMode = (int)SpriteImportMode.Single;
                ajustes.spritePixelsPerUnit = PixelesPorUnidad;
                ajustes.spriteMeshType = SpriteMeshType.FullRect; // el modo Tiled lo exige
                ajustes.spriteAlignment = (int)capa.Pivote;
                ajustes.spriteGenerateFallbackPhysicsShape = false;
                ajustes.wrapMode = TextureWrapMode.Clamp;
            }

            var cambia = !TextureImporterSettings.Equal(antes, ajustes) ||
                         importador.textureCompression != TextureImporterCompression.Uncompressed ||
                         importador.maxTextureSize != TamanoMaximo;
            if (!cambia)
            {
                return false;
            }

            importador.SetTextureSettings(ajustes);
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.maxTextureSize = TamanoMaximo;
            return true;
        }

        // --- 2. Materiales de las capas repetibles ---

        private static Material CrearMaterial(CapaFondo capa)
        {
            var shader = Shader.Find(ShaderCapas);
            if (shader == null)
            {
                Debug.LogError($"[Fragmentum] No se encontró el shader {ShaderCapas}: ¿está instalado URP?");
                return null;
            }

            var ruta = $"{CarpetaMateriales}/Fondo_{capa.Nombre}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            var nuevo = material == null;
            if (nuevo)
            {
                material = new Material(shader);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(capa.Ruta);
            material.mainTexture = textura;

            // Transparente con mezcla alfa normal: las capas con alfa dejan ver las de atrás.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;

            // En el editor se ve como en el juego con la cámara en x = 0 y pantalla 16:9. Jugando,
            // ParallaxMovement escribe el tiling y el desplazamiento de cada fotograma.
            var anchoTextura = AltoReferencia * textura.width / textura.height;
            material.mainTextureScale = new Vector2(AnchoCapaReferencia / anchoTextura, 1f);
            material.mainTextureOffset = new Vector2(CalculoParallax.OffsetTextura(0f, 0f, anchoTextura, AnchoCapaReferencia), 0f);

            if (nuevo)
            {
                AssetDatabase.CreateAsset(material, ruta);
            }
            else
            {
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        // --- 3. Prefab ---

        private static GameObject CrearPrefab(Dictionary<CapaFondo, Material> materiales)
        {
            AsegurarCarpeta(Path.GetDirectoryName(RutaPrefab)?.Replace('\\', '/'));
            var materialSprites = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialSprites);
            if (materialSprites == null)
            {
                Debug.LogWarning($"[Fragmentum] No se encontró {RutaMaterialSprites}: los props usarán el material por defecto.");
            }

            var raiz = new GameObject(Path.GetFileNameWithoutExtension(RutaPrefab));
            try
            {
                var repetibles = new GameObject("CapasRepetibles").transform;
                repetibles.SetParent(raiz.transform, false);
                repetibles.gameObject.AddComponent<ParallaxMovement>();
                foreach (var capa in CapasFondo.Where(c => c.Tecnica == TECNICAS_FONDO.REPETIBLE))
                {
                    CrearQuad(repetibles, capa, materiales[capa]);
                }

                var fortaleza = CrearCapaProps(raiz.transform, "Fortaleza", Fortaleza);
                CrearSprite(fortaleza, Fortaleza, materialSprites);

                var muro = CrearCapaProps(raiz.transform, "Muro", Muro);
                var tramo = CrearSprite(muro, Muro, materialSprites);
                tramo.drawMode = SpriteDrawMode.Tiled;
                tramo.tileMode = SpriteTileMode.Continuous;
                tramo.size = new Vector2(tramo.sprite.bounds.size.x, AltoReferencia);

                var cataratas = CrearCapaProps(raiz.transform, "Cataratas", Catarata);
                CrearSprite(cataratas, Catarata, materialSprites);

                var reparto = raiz.AddComponent<RepartoHitos>();
                Asignar(reparto, "hitos", p =>
                {
                    p.arraySize = 2;
                    GrupoHitos(p.GetArrayElementAtIndex(0), fortaleza, PosicionesFortaleza);
                    GrupoHitos(p.GetArrayElementAtIndex(1), cataratas, PosicionesCataratas);
                });
                Asignar(reparto, "tramos", p =>
                {
                    p.arraySize = 1;
                    var grupo = p.GetArrayElementAtIndex(0);
                    grupo.FindPropertyRelative("capa").objectReferenceValue = muro;
                    var lista = grupo.FindPropertyRelative("tramos");
                    lista.arraySize = TramosMuro.Length;
                    for (var i = 0; i < TramosMuro.Length; i++)
                    {
                        lista.GetArrayElementAtIndex(i).vector2Value = TramosMuro[i];
                    }
                });
                reparto.Repartir(ExtensionReferencia, AltoReferencia);

                var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab, out var guardado);
                return guardado ? prefab : null;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void GrupoHitos(SerializedProperty grupo, ParallaxProps capa, float[] posiciones)
        {
            grupo.FindPropertyRelative("capa").objectReferenceValue = capa;
            var lista = grupo.FindPropertyRelative("posiciones");
            lista.arraySize = posiciones.Length;
            for (var i = 0; i < posiciones.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).floatValue = posiciones[i];
            }
        }

        private static void CrearQuad(Transform padre, CapaFondo capa, Material material)
        {
            var objeto = new GameObject(capa.Nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = new Vector3(0f, 0f, capa.Z);
            objeto.transform.localScale = new Vector3(AnchoCapaReferencia, AltoReferencia, 1f);
            objeto.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var renderer = objeto.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = capa.Orden;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static ParallaxProps CrearCapaProps(Transform padre, string nombre, CapaFondo capa)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = new Vector3(0f, 0f, capa.Z);

            var props = objeto.AddComponent<ParallaxProps>();
            props.velocidad = capa.Velocidad;
            props.seguirCamaraEnY = true;
            props.altoReferencia = AltoReferencia;
            return props;
        }

        private static SpriteRenderer CrearSprite(ParallaxProps capaProps, CapaFondo capa, Material material)
        {
            var objeto = new GameObject($"{capa.Nombre}_1");
            objeto.transform.SetParent(capaProps.transform, false);
            objeto.transform.localPosition = new Vector3(0f, capa.AlturaEnPantalla, 0f);

            var sprite = objeto.AddComponent<SpriteRenderer>();
            sprite.sprite = CargarSprite(capa.Ruta);
            if (material != null)
            {
                sprite.sharedMaterial = material;
            }

            sprite.sortingOrder = capa.Orden;
            return sprite;
        }

        // --- 4. Escenas de nivel ---

        /// <summary>
        /// Pone el fondo en una escena de nivel y reparte sus hitos para la longitud del nivel.
        /// Devuelve false si no hay prefab.
        /// </summary>
        public static bool Colocar(Scene escena, LevelData nivel)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            if (prefab == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {RutaPrefab}: el nivel queda sin fondo. Ejecuta Fragmentum → Escenario → Importar el fondo del Mundo 1.");
                return false;
            }

            var fondo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            fondo.transform.position = Vector3.zero;
            Repartir(fondo.GetComponent<RepartoHitos>(), escena, nivel);
            return true;
        }

        /// <summary>
        /// Reparte otra vez los hitos del fondo que ya tenga la escena, para lo que mide ahora el
        /// nivel. Devuelve false si la escena no tiene fondo (lo pondrá <see cref="Colocar"/>).
        /// </summary>
        public static bool RepartirEn(Scene escena, LevelData nivel)
        {
            foreach (var raiz in escena.GetRootGameObjects())
            {
                var reparto = raiz.GetComponentInChildren<RepartoHitos>(true);
                if (reparto != null)
                {
                    Repartir(reparto, escena, nivel);
                    return true;
                }
            }

            return false;
        }

        private static void Repartir(RepartoHitos reparto, Scene escena, LevelData nivel)
        {
            var extension = nivel.Extension;
            if (extension.y <= extension.x)
            {
                extension = ExtensionReferencia;
            }

            var alto = AltoVista(escena);
            reparto.Repartir(extension, alto);
            foreach (var solape in reparto.Solapes(extension, alto))
            {
                Debug.LogWarning($"[Fragmentum] Fondo de '{escena.name}': {solape} Separa más sus posiciones en RepartoHitos.", reparto);
            }
        }

        /// <summary>Alto de la vista de la cámara de la escena (2 × tamaño ortográfico).</summary>
        public static float AltoVista(Scene escena)
        {
            foreach (var raiz in escena.GetRootGameObjects())
            {
                var camara = raiz.GetComponentInChildren<Camera>(true);
                if (camara != null && camara.orthographic)
                {
                    return camara.orthographicSize * 2f;
                }
            }

            return AltoReferencia;
        }
    }
}
