using System.Collections.Generic;
using System.IO;
using Fragmentum.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// La cámara de las cinemáticas en pixel art (<see cref="CamaraCinematica"/>):
    /// - genera su prefab (<see cref="RutaPrefab"/>) con <see cref="CamaraCinematica.Crear"/>, el
    ///   material de sprites sin luz de URP y el filtro final Point;
    /// - la captura a cualquier resolución (<see cref="Capturar"/>), en una escena de vista previa
    ///   que no toca la escena abierta;
    /// - comprueba que la captura es el plano ampliado por un entero, sin interpolar
    ///   (<see cref="ProblemasDeRejilla"/>): cada píxel del plano es un cuadrado de k × k píxeles
    ///   idénticos al original, y lo que sobra es negro.
    /// </summary>
    public static class ConstructorCamaraCinematica
    {
        public const string Carpeta = "Assets/Prefabs/Cinematicas";
        public const string RutaPrefab = Carpeta + "/CamaraCinematica.prefab";

        /// <summary>Sprites sin luz de URP: el plano se ve tal cual, sin las luces 2D del nivel.</summary>
        public const string RutaMaterial = ConstructorFondoMundo1.RutaMaterialSprites;

        /// <summary>Dónde se guardan las capturas (fuera de Assets: Unity no las importa).</summary>
        public const string CarpetaCapturas = "Docs/Cinematicas";

        /// <summary>Diferencia máxima por canal entre la captura y el plano (redondeos del color).</summary>
        public const int Tolerancia = 2;

        /// <summary>Las resoluciones que se capturan y se comprueban en modo batch.</summary>
        public static readonly Vector2Int[] Resoluciones = { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(960, 540) };

        public static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

        // --- Menú y modo batch ---

        [MenuItem("Fragmentum/Cinemáticas/Crear el prefab de la cámara de cinemáticas", priority = 150)]
        public static void MenuCrearPrefab() => EditorGUIUtility.PingObject(CrearPrefab());

        [MenuItem("Fragmentum/Cinemáticas/Capturar la cámara de cinemáticas a 1920 × 1080", priority = 151)]
        public static void MenuCapturar()
        {
            var ruta = CapturarYComprobar(PlanoDePrueba, 1920, 1080);
            if (ruta != null)
            {
                EditorUtility.RevealInFinder(ruta);
            }
        }

        /// <summary>
        /// Captura el plano de prueba en <see cref="Resoluciones"/>, guarda las capturas y comprueba
        /// la rejilla. Sale con error si algo no cuadra:
        /// <c>-executeMethod Fragmentum.EditorTools.ConstructorCamaraCinematica.CapturarEnModoBatch -quit</c>
        /// </summary>
        public static void CapturarEnModoBatch()
        {
            var ok = true;
            foreach (var resolucion in Resoluciones)
            {
                ok &= CapturarYComprobar(PlanoDePrueba, resolucion.x, resolucion.y) != null;
            }

            if (!ok && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        /// <summary>El sprite de prueba: el primer plano de la presentación del jefe del Nivel 1.</summary>
        public static Sprite PlanoDePrueba => ConstructorCinematicasNivel1.Imagen(ConstructorCinematicasNivel1.Jefe1);

        // --- Prefab ---

        /// <summary>Crea (o rehace) el prefab de la cámara. Devuelve null si no se pudo guardar.</summary>
        public static GameObject CrearPrefab()
        {
            AsegurarConfiguracion();
            AsegurarCarpeta(Carpeta);
            var material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (material == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {RutaMaterial}: el plano de las cinemáticas usará el material de sprites por defecto.");
            }

            var camara = CamaraCinematica.Crear(null, material);
            try
            {
                // Filtro final Point. El de por defecto (RetroAA) solo suaviza si la imagen se estira
                // a un tamaño no entero, cosa que el recorte en ventana ya evita; así no lo hace nunca.
                var ajustes = new SerializedObject(camara.PixelPerfect);
                ajustes.FindProperty("m_FilterMode").intValue = (int)PixelPerfectCamera.PixelPerfectFilterMode.Point;
                ajustes.ApplyModifiedPropertiesWithoutUndo();

                var prefab = PrefabUtility.SaveAsPrefabAsset(camara.gameObject, RutaPrefab, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {RutaPrefab}.");
                    return null;
                }

                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(camara.gameObject);
            }
        }

        // --- Captura ---

        /// <summary>
        /// Dibuja <paramref name="sprite"/> con la cámara del prefab en una textura de
        /// <paramref name="ancho"/> × <paramref name="alto"/> píxeles y la devuelve leída. Usa una
        /// escena de vista previa: la escena abierta no se toca.
        /// </summary>
        public static Texture2D Capturar(Sprite sprite, int ancho, int alto)
        {
            var prefab = Prefab != null ? Prefab : CrearPrefab();
            var escena = EditorSceneManager.NewPreviewScene();
            var objetivo = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var copia = Object.Instantiate(prefab);
            var camara = copia.GetComponent<CamaraCinematica>();
            try
            {
                SceneManager.MoveGameObjectToScene(copia, escena);
                camara.Camara.scene = escena;
                camara.Camara.targetTexture = objetivo;
                camara.Mostrar(sprite);
                camara.Camara.Render();

                var anterior = RenderTexture.active;
                RenderTexture.active = objetivo;
                var captura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
                captura.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
                captura.Apply();
                RenderTexture.active = anterior;
                return captura;
            }
            finally
            {
                camara.Camara.targetTexture = null;
                Object.DestroyImmediate(copia);
                EditorSceneManager.ClosePreviewScene(escena);
                objetivo.Release();
                Object.DestroyImmediate(objetivo);
            }
        }

        /// <summary>
        /// Captura <paramref name="sprite"/>, la guarda en <see cref="CarpetaCapturas"/> y comprueba
        /// la rejilla. Devuelve la ruta de la captura, o null si algo no cuadra (con un error por
        /// problema en la consola).
        /// </summary>
        public static string CapturarYComprobar(Sprite sprite, int ancho, int alto)
        {
            if (sprite == null)
            {
                Debug.LogError("[Cinemática] Falta el plano de prueba (CinematicaJefe1): no hay nada que capturar.");
                return null;
            }

            var captura = Capturar(sprite, ancho, alto);
            var original = LeerPng(sprite);
            try
            {
                Directory.CreateDirectory(CarpetaCapturas);
                var ruta = $"{CarpetaCapturas}/CamaraCinematica_{ancho}x{alto}.png";
                File.WriteAllBytes(ruta, captura.EncodeToPNG());

                var problemas = ProblemasDeRejilla(captura, original);
                var k = CamaraCinematica.RectEnPantalla(ancho, alto).width / original.width;
                if (problemas.Count == 0)
                {
                    Debug.Log($"[Cinemática] {ancho} × {alto}: cada píxel de {sprite.name} es un cuadrado de {k} × {k} píxeles idénticos al original, sin interpolar. Captura en {ruta}.");
                    return ruta;
                }

                foreach (var problema in problemas)
                {
                    Debug.LogError($"[Cinemática] {ancho} × {alto}: {problema}");
                }

                return null;
            }
            finally
            {
                Object.DestroyImmediate(captura);
                Object.DestroyImmediate(original);
            }
        }

        /// <summary>
        /// Lo que no cuadra entre <paramref name="captura"/> y el plano <paramref name="original"/>
        /// ampliado por el mayor entero que cabe y centrado como lo centra la Pixel Perfect Camera:
        /// cada píxel de la captura tiene que ser el del original que le corresponde (sobre negro,
        /// si tuviera transparencia) y fuera del plano, negro. Vacía si todo cuadra.
        /// </summary>
        public static List<string> ProblemasDeRejilla(Texture2D captura, Texture2D original)
        {
            var problemas = new List<string>();
            var rect = CamaraCinematica.RectEnPantalla(captura.width, captura.height);
            var k = rect.width / CamaraCinematica.AnchoReferencia;
            if (original.width != CamaraCinematica.AnchoReferencia || original.height != CamaraCinematica.AltoReferencia)
            {
                problemas.Add($"el plano mide {original.width} × {original.height} y la rejilla {CamaraCinematica.AnchoReferencia} × {CamaraCinematica.AltoReferencia}.");
                return problemas;
            }

            var pantalla = captura.GetPixels32();
            var plano = original.GetPixels32();
            var fuera = 0;
            var distintos = 0;
            var ejemplos = new List<string>();
            for (var y = 0; y < captura.height; y++)
            {
                for (var x = 0; x < captura.width; x++)
                {
                    var visto = pantalla[y * captura.width + x];
                    if (!rect.Contains(new Vector2Int(x, y)))
                    {
                        if (visto.r > Tolerancia || visto.g > Tolerancia || visto.b > Tolerancia)
                        {
                            fuera++;
                        }

                        continue;
                    }

                    var px = plano[(y - rect.y) / k * original.width + (x - rect.x) / k];
                    var esperado = new Color32((byte)(px.r * px.a / 255), (byte)(px.g * px.a / 255), (byte)(px.b * px.a / 255), 255);
                    if (Mathf.Abs(visto.r - esperado.r) > Tolerancia || Mathf.Abs(visto.g - esperado.g) > Tolerancia || Mathf.Abs(visto.b - esperado.b) > Tolerancia)
                    {
                        distintos++;
                        if (ejemplos.Count < 5)
                        {
                            ejemplos.Add($"({x}, {y}) es {Texto(visto)} y debía ser {Texto(esperado)}");
                        }
                    }
                }
            }

            if (distintos > 0)
            {
                problemas.Add($"{distintos} píxeles no son el del plano ampliado ×{k} sin interpolar; por ejemplo, {string.Join("; ", ejemplos)}.");
            }

            if (fuera > 0)
            {
                problemas.Add($"{fuera} píxeles fuera del plano ({rect.width} × {rect.height} en ({rect.x}, {rect.y})) no son negros.");
            }

            return problemas;
        }

        /// <summary>Los píxeles del PNG de <paramref name="sprite"/>, leídos del disco (el importador no los deja leer).</summary>
        public static Texture2D LeerPng(Sprite sprite)
        {
            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            textura.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
            return textura;
        }

        private static string Texto(Color32 c) => $"({c.r}, {c.g}, {c.b})";
    }
}
