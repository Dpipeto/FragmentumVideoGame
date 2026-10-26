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
using CapaFondo = Fragmentum.EditorTools.ConstructorFondoMundo2.CapaFondo;
using TECNICAS_FONDO = Fragmentum.EditorTools.ConstructorFondoMundo2.TECNICAS_FONDO;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Monta el fondo con parallax del Mundo 3 (Miedo, el bosque) a partir de las siete imágenes de
    /// <see cref="CarpetaArte"/>, como los de los otros dos mundos (<see cref="ConstructorFondoMundo2"/>):
    /// el cielo de noche con la luna, el bosque lejano, el bosque cercano, la niebla y las ramas del
    /// primer plano, que ocupan la vista y siguen a la cámara; los helechos, anclados al suelo del
    /// bosque; y el árbol hueco, el hito, que se ve centrado cuando la cámara pasa por la arena del jefe.
    ///
    /// <b>Se oscurece con la luz del nivel</b> (<see cref="FondoConLaLuz"/>): en el bosque cerrado y en
    /// la arena casi no se ve, y se ve entero cuando la oscuridad retrocede.
    ///
    /// El bosque cercano y las ramas del primer plano no casan por sus bordes (lo dice el LEEME del
    /// arte): se repiten en espejo, como las ciudades del Mundo 2. Los helechos y las zarzas del
    /// primer plano de abajo llegaron con un cielo de nubes encima: se limpiaron sin él (2026-10-16).
    ///
    /// <see cref="PreparadorNivel"/> lo coloca en las escenas de nivel del mundo del Miedo.
    /// Idempotente.
    /// </summary>
    public static class ConstructorFondoMundo3
    {
        public const string CarpetaArte = ConstructorFondoMundo1.CarpetaFondos + "/Mundo3Miedo";
        public const string CarpetaMateriales = CarpetaArte + "/Materiales";
        public const string RutaPrefab = "Assets/Prefabs/Fondos/FondoMundo3.prefab";

        public const float AltoReferencia = ConstructorFondoMundo1.AltoReferencia;

        /// <summary>Distancia a la cámara de la capa más lejana, el cielo: de ella salen las velocidades.</summary>
        public const float Lejania = 50f;

        /// <summary>Donde se ve centrado el árbol hueco: en el centro de la arena, donde espera el jefe.</summary>
        public static float CentroDelArbol => (DisenoNivel3.LimitesArena.x + DisenoNivel3.LimitesArena.y) * 0.5f;

        public static float AnchoCapaReferencia => ConstructorFondoMundo2.AnchoCapaReferencia;

        public static readonly CapaFondo Cielo = new CapaFondo
        {
            Nombre = "Cielo", Archivo = "Cielo.png", Tecnica = TECNICAS_FONDO.VISTA, Z = 40f, Velocidad = 0f, Orden = -60,
        };

        public static readonly CapaFondo BosqueLejano = new CapaFondo
        {
            Nombre = "BosqueLejano", Archivo = "BosqueLejano.png", Tecnica = TECNICAS_FONDO.VISTA, Z = 30f, Velocidad = 0.2f, Orden = -50,
        };

        public static readonly CapaFondo ArbolHueco = new CapaFondo
        {
            Nombre = "ArbolHueco", Archivo = "ArbolHueco.png", Tecnica = TECNICAS_FONDO.PROP, Z = 20f, Velocidad = 0.4f, Orden = -40,
        };

        public static readonly CapaFondo BosqueCercano = new CapaFondo
        {
            Nombre = "BosqueCercano", Archivo = "BosqueCercano.png", Tecnica = TECNICAS_FONDO.VISTA, Z = 12f, Velocidad = 0.55f, Orden = -35, Espejo = true,
        };

        public static readonly CapaFondo Niebla = new CapaFondo
        {
            Nombre = "Niebla", Archivo = "Niebla.png", Tecnica = TECNICAS_FONDO.VISTA, Z = 6f, Velocidad = 0.7f, Orden = -32,
        };

        /// <summary>
        /// Los helechos y las zarzas de abajo, delante de la niebla y anclados al suelo del bosque,
        /// como las algas del Mundo 2: siguiendo a la cámara se verían bajo las copas, en el aire. Su
        /// borde de arriba va en y = 5,5: las puntas llegan a 1,5 u sobre el suelo (y = 0), a la
        /// altura de Kael, y lo más espeso queda por debajo, donde solo se ve en la trinchera, el
        /// barranco y la hondonada. Su borde de abajo (y = −4,2) no asoma: la hondonada está en −4.
        /// </summary>
        public static readonly CapaFondo Helechos = new CapaFondo
        {
            Nombre = "Helechos", Archivo = "PrimerPlanoAbajo.png", Tecnica = TECNICAS_FONDO.ANCLADA, Z = 0f, Velocidad = 0.8f, Orden = -31,
            Arriba = 5.5f,
        };

        public static readonly CapaFondo Ramas = new CapaFondo
        {
            Nombre = "Ramas", Archivo = "PrimerPlanoArriba.png", Tecnica = TECNICAS_FONDO.VISTA, Z = 0f, Velocidad = 0.8f, Orden = -30, Espejo = true,
        };

        /// <summary>Las siete capas, de la más lejana a la más cercana.</summary>
        public static readonly CapaFondo[] CapasFondo = { Cielo, BosqueLejano, ArbolHueco, BosqueCercano, Niebla, Helechos, Ramas };

        public static string RutaDe(CapaFondo capa) => $"{CarpetaArte}/{capa.Archivo}";

        /// <summary>La capa de una imagen de este fondo, o null si no es de ninguna.</summary>
        public static CapaFondo CapaDeRuta(string ruta) => CapasFondo.FirstOrDefault(c => RutaDe(c) == ruta);

        public const string NombreVista = ConstructorFondoMundo2.NombreVista;

        /// <summary>El grupo de las capas ancladas al suelo: siguen a la cámara solo en horizontal.</summary>
        public const string NombreAlSuelo = "CapasDelSuelo";

        // --- Menú ---

        [MenuItem("Fragmentum/Escenario/Importar el fondo del Mundo 3 y regenerar su prefab", priority = 142)]
        public static void MenuConstruir() => Construir();

        /// <summary>
        /// Importa las imágenes, crea los materiales y regenera el prefab. Devuelve false solo si
        /// algo falló; si faltan imágenes no es un error: avisa y no genera el prefab.
        /// </summary>
        public static bool Construir()
        {
            var faltan = CapasFondo.Where(c => AssetDatabase.LoadAssetAtPath<Texture2D>(RutaDe(c)) == null).Select(RutaDe).ToList();
            if (faltan.Count > 0)
            {
                Debug.LogWarning($"[Fragmentum] Faltan imágenes del fondo del Mundo 3: {string.Join(", ", faltan)}. No se genera su prefab.");
                return true;
            }

            foreach (var capa in CapasFondo)
            {
                var importador = (TextureImporter)AssetImporter.GetAtPath(RutaDe(capa));
                if (ConstructorFondoMundo2.AjustarImportador(importador, capa))
                {
                    importador.SaveAndReimport();
                }
            }

            AsegurarCarpeta(CarpetaMateriales);
            var materiales = new Dictionary<CapaFondo, Material>();
            foreach (var capa in CapasFondo.Where(c => c.Tecnica != TECNICAS_FONDO.PROP))
            {
                materiales[capa] = CrearMaterial($"Fondo_{capa.Nombre}", AssetDatabase.LoadAssetAtPath<Texture2D>(RutaDe(capa)));
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

            Debug.Log($"[Fragmentum] Fondo del Mundo 3: {CapasFondo.Length} capas importadas y {RutaPrefab}.");
            return true;
        }

        // URP/Unlit transparente, como los de los otros mundos: los shaders 2D de sprites ignoran el
        // desplazamiento de textura. La luz se la pone FondoConLaLuz con el color base.
        private static Material CrearMaterial(string nombre, Texture2D textura)
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
            material.SetColor("_BaseColor", Color.white);
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

            // En el editor se ve como en el juego con la cámara en x = 0 y pantalla 16:9.
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

        private static GameObject CrearPrefab(IReadOnlyDictionary<CapaFondo, Material> materiales)
        {
            AsegurarCarpeta(Path.GetDirectoryName(RutaPrefab)?.Replace('\\', '/'));
            var materialSprites = AssetDatabase.LoadAssetAtPath<Material>(ConstructorFondoMundo1.RutaMaterialSprites);

            var raiz = new GameObject(Path.GetFileNameWithoutExtension(RutaPrefab));
            try
            {
                // Ocupan la vista y siguen a la cámara.
                var vista = new GameObject(NombreVista).transform;
                vista.SetParent(raiz.transform, false);
                var parallax = vista.gameObject.AddComponent<ParallaxMovement>();
                Asignar(parallax, "seguirCamaraEnY", p => p.boolValue = true);
                Asignar(parallax, "lejania", p => p.floatValue = Lejania);
                foreach (var capa in CapasFondo.Where(c => c.Tecnica == TECNICAS_FONDO.VISTA))
                {
                    Quad(vista, capa.Nombre, materiales[capa], capa.Z, capa.Orden);
                }

                // Los helechos, en su sitio del mundo.
                var alSuelo = new GameObject(NombreAlSuelo).transform;
                alSuelo.SetParent(raiz.transform, false);
                var parallaxSuelo = alSuelo.gameObject.AddComponent<ParallaxMovement>();
                Asignar(parallaxSuelo, "seguirCamaraEnY", p => p.boolValue = false);
                Asignar(parallaxSuelo, "lejania", p => p.floatValue = Lejania);
                foreach (var capa in CapasFondo.Where(c => c.Tecnica == TECNICAS_FONDO.ANCLADA))
                {
                    Quad(alSuelo, capa.Nombre, materiales[capa], capa.Z, capa.Orden, capa.Arriba - AltoReferencia * 0.5f);
                }

                // El árbol hueco, el hito: se ve centrado cuando la cámara pasa por la arena.
                var arbol = new GameObject(ArbolHueco.Nombre);
                arbol.transform.SetParent(raiz.transform, false);
                arbol.transform.localPosition = new Vector3(0f, 0f, ArbolHueco.Z);
                var props = arbol.AddComponent<ParallaxProps>();
                props.velocidad = ArbolHueco.Velocidad;
                props.seguirCamaraEnY = true;
                props.altoReferencia = AltoReferencia;
                var objetoSprite = new GameObject($"{ArbolHueco.Nombre}_1");
                objetoSprite.transform.SetParent(arbol.transform, false);
                objetoSprite.transform.position = new Vector3(CentroDelArbol, 0f, ArbolHueco.Z);
                var sprite = objetoSprite.AddComponent<SpriteRenderer>();
                sprite.sprite = CargarSprite(RutaDe(ArbolHueco));
                if (materialSprites != null)
                {
                    sprite.sharedMaterial = materialSprites;
                }

                sprite.sortingOrder = ArbolHueco.Orden;

                // A oscuras, el fondo también.
                raiz.AddComponent<FondoConLaLuz>();

                var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab, out var guardado);
                return guardado ? prefab : null;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void Quad(Transform padre, string nombre, Material material, float z, int orden, float centroY = 0f)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = new Vector3(0f, centroY, z);
            objeto.transform.localScale = new Vector3(AnchoCapaReferencia, AltoReferencia, 1f);
            objeto.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var renderer = objeto.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = orden;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        // --- Escenas de nivel ---

        /// <summary>¿La escena ya tiene este fondo?</summary>
        public static bool EstaColocado(Scene escena) =>
            escena.GetRootGameObjects().Any(r => r.name == Path.GetFileNameWithoutExtension(RutaPrefab));

        /// <summary>Pone el fondo en una escena de nivel. Devuelve false si no hay prefab.</summary>
        public static bool Colocar(Scene escena, LevelData nivel)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            if (prefab == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {RutaPrefab}: el nivel queda sin fondo. Ejecuta Fragmentum → Escenario → Importar el fondo del Mundo 3.");
                return false;
            }

            var fondo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            fondo.transform.position = Vector3.zero;
            return true;
        }
    }
}
