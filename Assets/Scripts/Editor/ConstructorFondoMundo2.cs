using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Data;
using Fragmentum.Levels.Escenario;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static Fragmentum.EditorTools.UtilidadesEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Monta el fondo con parallax del Mundo 2 (Tristeza, Ciudad Sumergida) a partir de las siete
    /// imágenes de <see cref="CarpetaArte"/>. Como el del Mundo 1 (<see cref="ConstructorFondoMundo1"/>),
    /// con una diferencia: este mundo tiene dos fondos, uno encima del agua y otro debajo.
    ///
    /// - <b>Arriba</b> (siguen a la cámara y ocupan la vista entera, como en el Mundo 1): el cielo
    ///   helado, la ciudad lejana y los carámbanos del primer plano; y la torre del reloj, un hito.
    /// - <b>Bajo el agua</b> (ancladas en vertical: están en su sitio del mundo y solo se mueven
    ///   en horizontal): el agua con sus rayos de luz, desde la superficie; la ciudad hundida y
    ///   las algas, apoyadas en el fondo de la calle. Tapan a las de arriba: al bajar la cámara,
    ///   el fondo cambia donde cambia el mundo.
    ///
    /// Las dos ciudades no casan por sus bordes (lo dice el LEEME del arte): se repiten en espejo,
    /// una copia al derecho y otra al revés, y así no se ve el salto. Las imágenes no se tocan.
    ///
    /// <see cref="PreparadorNivel"/> lo coloca en las escenas de nivel del mundo de la Tristeza.
    /// Es idempotente: sin cambios en las imágenes, volver a ejecutarlo no cambia nada.
    /// </summary>
    public static class ConstructorFondoMundo2
    {
        public const string CarpetaArte = ConstructorFondoMundo1.CarpetaFondos + "/Mundo2Tristeza";
        public const string CarpetaMateriales = CarpetaArte + "/Materiales";
        public const string RutaPrefab = "Assets/Prefabs/Fondos/FondoMundo2.prefab";

        /// <summary>Alto de la vista para el que se monta: el de la cámara de los niveles (tamaño ortográfico 4,85).</summary>
        public const float AltoReferencia = ConstructorFondoMundo1.AltoReferencia;

        /// <summary>Distancia a la cámara (que está en z −10) de la capa más lejana, el cielo: de ella salen las velocidades.</summary>
        public const float Lejania = 50f;

        /// <summary>Donde empieza el fondo de debajo del agua: la superficie del agua del recorrido.</summary>
        public const float SuperficieDelAgua = -DisenoNivel2.BajoLaSuperficie;

        /// <summary>Donde se apoyan la ciudad hundida y las algas: medio metro bajo el suelo de la calle (y −11), que tapa su base.</summary>
        public const float FondoDelAgua = -11.5f;

        /// <summary>Hasta dónde llega el relleno liso de debajo del agua: más abajo de lo que baja la cámara.</summary>
        public const float FondoDelRelleno = -20f;

        /// <summary>La X del centro de la torre del reloj del recorrido: ahí se ve centrada la del fondo.</summary>
        public const float CentroDeLaTorre = DisenoNivel2.CentroDeLaTorre;

        /// <summary>El color de la última fila del agua con rayos: el relleno de más abajo.</summary>
        public static readonly Color ColorDelFondo = new Color32(17, 24, 42, 255);

        public static float AnchoCapaReferencia => AltoReferencia * RepartoHitos.AspectoReferencia * ParallaxMovement.MargenAncho;

        public enum TECNICAS_FONDO
        {
            /// <summary>Textura que se repite; ocupa la vista entera y sigue a la cámara.</summary>
            VISTA,

            /// <summary>Textura que se repite; fija en vertical, en su sitio del mundo.</summary>
            ANCLADA,

            /// <summary>Sprite suelto (un hito); lo mueve <see cref="ParallaxProps"/>.</summary>
            PROP,
        }

        /// <summary>Una capa del fondo: de qué imagen sale, a qué profundidad va y cómo se mueve.</summary>
        public sealed class CapaFondo
        {
            public string Nombre;
            public string Archivo;
            public TECNICAS_FONDO Tecnica;
            public float Z;

            /// <summary>La que da su Z: 1 − (Z + 10) / <see cref="Lejania"/>.</summary>
            public float Velocidad;

            /// <summary>Orden de dibujo: siempre por debajo del juego, que usa de −3 en adelante.</summary>
            public int Orden;

            /// <summary>Se repite en espejo: sus bordes no casan.</summary>
            public bool Espejo;

            /// <summary>Las ancladas: la altura del mundo donde está su borde de arriba.</summary>
            public float Arriba;

            public string Ruta => $"{CarpetaArte}/{Archivo}";
        }

        public static readonly CapaFondo Cielo = new CapaFondo
        {
            Nombre = "Cielo", Archivo = "Cielo.png", Tecnica = TECNICAS_FONDO.VISTA, Z = 40f, Velocidad = 0f, Orden = -60,
        };

        public static readonly CapaFondo CiudadLejana = new CapaFondo
        {
            Nombre = "CiudadLejana", Archivo = "CiudadLejana.png", Tecnica = TECNICAS_FONDO.VISTA, Z = 30f, Velocidad = 0.2f, Orden = -50, Espejo = true,
        };

        public static readonly CapaFondo Torre = new CapaFondo
        {
            Nombre = "Torre", Archivo = "Torre.png", Tecnica = TECNICAS_FONDO.PROP, Z = 20f, Velocidad = 0.4f, Orden = -40,
        };

        public static readonly CapaFondo Carambanos = new CapaFondo
        {
            Nombre = "Carambanos", Archivo = "PrimerPlanoArriba.png", Tecnica = TECNICAS_FONDO.VISTA, Z = 0f, Velocidad = 0.8f, Orden = -30,
        };

        public static readonly CapaFondo Submarino = new CapaFondo
        {
            Nombre = "Submarino", Archivo = "Submarino.png", Tecnica = TECNICAS_FONDO.ANCLADA, Z = 30f, Velocidad = 0.2f, Orden = -25,
            Arriba = SuperficieDelAgua,
        };

        public static readonly CapaFondo CiudadHundida = new CapaFondo
        {
            Nombre = "CiudadHundida", Archivo = "CiudadHundida.png", Tecnica = TECNICAS_FONDO.ANCLADA, Z = 15f, Velocidad = 0.5f, Orden = -20, Espejo = true,
            Arriba = FondoDelAgua + AltoReferencia,
        };

        public static readonly CapaFondo Algas = new CapaFondo
        {
            Nombre = "Algas", Archivo = "PrimerPlanoAbajo.png", Tecnica = TECNICAS_FONDO.ANCLADA, Z = 0f, Velocidad = 0.8f, Orden = -10,
            Arriba = FondoDelAgua + AltoReferencia,
        };

        /// <summary>Las siete capas, de la más lejana a la más cercana; primero las de arriba y después las de bajo el agua.</summary>
        public static readonly CapaFondo[] CapasFondo = { Cielo, CiudadLejana, Torre, Carambanos, Submarino, CiudadHundida, Algas };

        /// <summary>Orden de dibujo del relleno liso: justo detrás del agua con rayos.</summary>
        public const int OrdenRelleno = -26;

        // --- Menú ---

        [MenuItem("Fragmentum/Escenario/Importar el fondo del Mundo 2 y regenerar su prefab", priority = 141)]
        public static void MenuConstruir() => Construir();

        /// <summary>
        /// Importa las imágenes, crea los materiales y regenera el prefab. Devuelve false solo si
        /// algo falló; si faltan imágenes no es un error: avisa y no genera el prefab.
        /// </summary>
        public static bool Construir()
        {
            var faltan = CapasFondo.Where(c => AssetDatabase.LoadAssetAtPath<Texture2D>(c.Ruta) == null).Select(c => c.Ruta).ToList();
            if (faltan.Count > 0)
            {
                Debug.LogWarning($"[Fragmentum] Faltan imágenes del fondo del Mundo 2: {string.Join(", ", faltan)}. No se genera su prefab.");
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
            foreach (var capa in CapasFondo.Where(c => c.Tecnica != TECNICAS_FONDO.PROP))
            {
                materiales[capa] = CrearMaterial($"Fondo_{capa.Nombre}", AssetDatabase.LoadAssetAtPath<Texture2D>(capa.Ruta), Color.white);
                if (materiales[capa] == null)
                {
                    return false;
                }
            }

            var relleno = CrearMaterial("Fondo_Relleno", null, ColorDelFondo);
            if (relleno == null)
            {
                return false;
            }

            var prefab = CrearPrefab(materiales, relleno);
            AssetDatabase.SaveAssets();
            if (prefab == null)
            {
                Debug.LogError($"[Fragmentum] No se pudo guardar {RutaPrefab}.");
                return false;
            }

            Debug.Log($"[Fragmentum] Fondo del Mundo 2: {CapasFondo.Length} capas importadas y {RutaPrefab}.");
            return true;
        }

        // --- 1. Importación ---

        /// <summary>La capa de una imagen de este fondo, o null si no es de ninguna.</summary>
        public static CapaFondo CapaDeRuta(string ruta) => CapasFondo.FirstOrDefault(c => c.Ruta == ruta);

        /// <summary>Píxeles por unidad de una imagen de <paramref name="alto"/> píxeles que ocupa la vista de referencia.</summary>
        public static float PixelesPorUnidad(int alto) => alto / AltoReferencia;

        /// <summary>
        /// Ajustes de pixel art, <b>sin reescalar</b>, y el tipo de la capa: textura que se repite
        /// en horizontal (tal cual o en espejo) o sprite. Devuelve si cambió algo.
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

            if (capa != null && capa.Tecnica != TECNICAS_FONDO.PROP)
            {
                ajustes.textureType = TextureImporterType.Default;
                ajustes.textureShape = TextureImporterShape.Texture2D;
                // En vertical, Clamp: con Repeat y filtro Point podría asomar una línea de la fila contraria.
                ajustes.wrapModeU = capa.Espejo ? TextureWrapMode.Mirror : TextureWrapMode.Repeat;
                ajustes.wrapModeV = TextureWrapMode.Clamp;
                ajustes.wrapModeW = TextureWrapMode.Clamp;
            }
            else if (capa != null)
            {
                importador.GetSourceTextureWidthAndHeight(out _, out var alto);
                ajustes.textureType = TextureImporterType.Sprite;
                ajustes.spriteMode = (int)SpriteImportMode.Single;
                ajustes.spritePixelsPerUnit = PixelesPorUnidad(alto);
                ajustes.spriteMeshType = SpriteMeshType.FullRect;
                ajustes.spriteAlignment = (int)SpriteAlignment.Center;
                ajustes.spriteGenerateFallbackPhysicsShape = false;
                ajustes.wrapMode = TextureWrapMode.Clamp;
            }

            var cambia = !TextureImporterSettings.Equal(antes, ajustes) ||
                         importador.textureCompression != TextureImporterCompression.Uncompressed ||
                         importador.maxTextureSize != ConstructorFondoMundo1.TamanoMaximo;
            if (!cambia)
            {
                return false;
            }

            importador.SetTextureSettings(ajustes);
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.maxTextureSize = ConstructorFondoMundo1.TamanoMaximo;
            return true;
        }

        // --- 2. Materiales ---

        // URP/Unlit transparente, como los del Mundo 1: los shaders 2D de sprites ignoran el
        // desplazamiento de textura. Sin textura es un color liso (el relleno).
        private static Material CrearMaterial(string nombre, Texture2D textura, Color color)
        {
            var shader = Shader.Find(ConstructorFondoMundo1.ShaderCapas);
            if (shader == null)
            {
                Debug.LogError($"[Fragmentum] No se encontró el shader {ConstructorFondoMundo1.ShaderCapas}: ¿está instalado URP?");
                return null;
            }

            var ruta = $"{CarpetaMateriales}/{nombre}.mat";
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

            material.mainTexture = textura;
            material.SetColor("_BaseColor", color);
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

            if (textura != null)
            {
                // En el editor se ve como en el juego con la cámara en x = 0 y pantalla 16:9.
                var anchoTextura = AltoReferencia * textura.width / textura.height;
                material.mainTextureScale = new Vector2(AnchoCapaReferencia / anchoTextura, 1f);
                material.mainTextureOffset = new Vector2(CalculoParallax.OffsetTextura(0f, 0f, anchoTextura, AnchoCapaReferencia), 0f);
            }

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

        public const string NombreVista = "CapasDeLaVista";
        public const string NombreBajoElAgua = "CapasBajoElAgua";

        /// <summary>El rectángulo liso que sigue por debajo de los rayos de luz: no lleva dibujo.</summary>
        public const string NombreRelleno = "Relleno";

        private static GameObject CrearPrefab(IReadOnlyDictionary<CapaFondo, Material> materiales, Material relleno)
        {
            AsegurarCarpeta(Path.GetDirectoryName(RutaPrefab)?.Replace('\\', '/'));
            var materialSprites = AssetDatabase.LoadAssetAtPath<Material>(ConstructorFondoMundo1.RutaMaterialSprites);

            var raiz = new GameObject(Path.GetFileNameWithoutExtension(RutaPrefab));
            try
            {
                // Arriba: ocupan la vista y siguen a la cámara.
                var vista = Grupo(raiz.transform, NombreVista, true);
                foreach (var capa in CapasFondo.Where(c => c.Tecnica == TECNICAS_FONDO.VISTA))
                {
                    Quad(vista, capa.Nombre, materiales[capa], capa.Z, capa.Orden, 0f, AltoReferencia);
                }

                // La torre del reloj, un hito: se ve centrada cuando la cámara pasa por la del recorrido.
                var torre = new GameObject(Torre.Nombre);
                torre.transform.SetParent(raiz.transform, false);
                torre.transform.localPosition = new Vector3(0f, 0f, Torre.Z);
                var props = torre.AddComponent<ParallaxProps>();
                props.velocidad = Torre.Velocidad;
                props.seguirCamaraEnY = true;
                props.altoReferencia = AltoReferencia;
                var objetoSprite = new GameObject($"{Torre.Nombre}_1");
                objetoSprite.transform.SetParent(torre.transform, false);
                objetoSprite.transform.position = new Vector3(CentroDeLaTorre, 0f, Torre.Z);
                var sprite = objetoSprite.AddComponent<SpriteRenderer>();
                sprite.sprite = CargarSprite(Torre.Ruta);
                if (materialSprites != null)
                {
                    sprite.sharedMaterial = materialSprites;
                }

                sprite.sortingOrder = Torre.Orden;

                // Bajo el agua: en su sitio del mundo, por delante de las de arriba.
                var bajoElAgua = Grupo(raiz.transform, NombreBajoElAgua, false);
                var fondoDelSubmarino = Submarino.Arriba - AltoReferencia;
                Quad(bajoElAgua, NombreRelleno, relleno, Submarino.Z + 1f, OrdenRelleno, (fondoDelSubmarino + FondoDelRelleno) * 0.5f, fondoDelSubmarino - FondoDelRelleno);
                foreach (var capa in CapasFondo.Where(c => c.Tecnica == TECNICAS_FONDO.ANCLADA))
                {
                    Quad(bajoElAgua, capa.Nombre, materiales[capa], capa.Z, capa.Orden, capa.Arriba - AltoReferencia * 0.5f, AltoReferencia);
                }

                var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab, out var guardado);
                return guardado ? prefab : null;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static Transform Grupo(Transform padre, string nombre, bool sigueLaCamaraEnY)
        {
            var grupo = new GameObject(nombre).transform;
            grupo.SetParent(padre, false);
            var parallax = grupo.gameObject.AddComponent<ParallaxMovement>();
            Asignar(parallax, "seguirCamaraEnY", p => p.boolValue = sigueLaCamaraEnY);
            Asignar(parallax, "lejania", p => p.floatValue = Lejania);
            return grupo;
        }

        private static void Quad(Transform padre, string nombre, Material material, float z, int orden, float centroY, float alto)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = new Vector3(0f, centroY, z);
            objeto.transform.localScale = new Vector3(AnchoCapaReferencia, alto, 1f);
            objeto.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var renderer = objeto.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = orden;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        // --- 4. Escenas de nivel ---

        /// <summary>¿La escena ya tiene este fondo?</summary>
        public static bool EstaColocado(Scene escena) =>
            escena.GetRootGameObjects().Any(r => r.name == Path.GetFileNameWithoutExtension(RutaPrefab));

        /// <summary>Pone el fondo en una escena de nivel. Devuelve false si no hay prefab.</summary>
        public static bool Colocar(Scene escena, LevelData nivel)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            if (prefab == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {RutaPrefab}: el nivel queda sin fondo. Ejecuta Fragmentum → Escenario → Importar el fondo del Mundo 2.");
                return false;
            }

            var fondo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            fondo.transform.position = Vector3.zero;
            return true;
        }
    }
}
