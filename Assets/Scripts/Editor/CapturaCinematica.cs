using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Fragmentum.Narrative;
using Fragmentum.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Fotografía una cinemática en pixel art en un instante, como se ve en el juego, para revisarla
    /// sin darle a Play:
    /// - los planos, su encuadre y las brasas, con la cámara de cinemáticas del prefab;
    /// - el texto encima: en la rejilla si la cinemática tiene fuente bitmap, o en la interfaz,
    ///   colocado con el mismo código que el reproductor (<see cref="PantallaCinematica.ColocarSobreRejilla"/>).
    ///   La interfaz se dibuja sobre negro y sobre blanco; de las dos sale la opacidad exacta de cada
    ///   píxel para componerla sobre la imagen.
    ///
    /// Sin temblor ni flash: duran unos fotogramas y dependen del momento exacto. Todo ocurre en una
    /// escena de vista previa: la escena abierta no se toca.
    /// </summary>
    public static class CapturaCinematica
    {
        /// <summary>Los instantes de la presentación del jefe que se capturan: la subida, el texto, el rótulo y el final.</summary>
        public static readonly float[] InstantesPresentacion = { 1.0f, 4.5f, 8.5f, 9.55f };

        [MenuItem("Fragmentum/Cinemáticas/Capturar la presentación del jefe del Nivel 1", priority = 152)]
        public static void MenuCapturarPresentacion()
        {
            var rutas = CapturarPresentacion(1920, 1080);
            if (rutas.Count > 0)
            {
                EditorUtility.RevealInFinder(rutas[0]);
            }
        }

        /// <summary><c>-executeMethod Fragmentum.EditorTools.CapturaCinematica.CapturarPresentacionEnModoBatch -quit</c></summary>
        public static void CapturarPresentacionEnModoBatch()
        {
            if (CapturarPresentacion(1920, 1080).Count == 0 && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Captura la presentación del jefe del Nivel 1 en <see cref="InstantesPresentacion"/>, y el
        /// rótulo también sin el encuadre del ojo, para compararlo. Devuelve las rutas guardadas.
        /// </summary>
        public static List<string> CapturarPresentacion(int ancho, int alto)
        {
            var rutas = new List<string>();
            var presentacion = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicasNivel1.RutaPresentacion);
            if (presentacion == null)
            {
                Debug.LogError($"[Cinemática] Falta {ConstructorCinematicasNivel1.RutaPresentacion}.");
                return rutas;
            }

            Directory.CreateDirectory(ConstructorCamaraCinematica.CarpetaCapturas);
            foreach (var t in InstantesPresentacion)
            {
                rutas.Add(Guardar(presentacion, t, ancho, alto, $"Presentacion_{Instante(t)}s"));
            }

            // El ojo sin encuadrar, para compararlo con el desplazamiento del guion.
            var sinEncuadre = Object.Instantiate(presentacion);
            try
            {
                foreach (var beat in sinEncuadre.diapositivas)
                {
                    beat.desplazamiento = Vector2Int.zero;
                }

                rutas.Add(Guardar(sinEncuadre, 8.5f, ancho, alto, "Presentacion_8.5s_sin_encuadre"));
            }
            finally
            {
                Object.DestroyImmediate(sinEncuadre);
            }

            Debug.Log($"[Cinemática] Presentación del jefe capturada en {ConstructorCamaraCinematica.CarpetaCapturas}: {rutas.Count} imágenes.");
            return rutas;
        }

        private static string Guardar(CutsceneData datos, float t, int ancho, int alto, string nombre)
        {
            var captura = Capturar(datos, t, ancho, alto);
            try
            {
                var ruta = $"{ConstructorCamaraCinematica.CarpetaCapturas}/{nombre}.png";
                File.WriteAllBytes(ruta, captura.EncodeToPNG());
                return ruta;
            }
            finally
            {
                Object.DestroyImmediate(captura);
            }
        }

        private static string Instante(float t) => t.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>La cinemática <paramref name="datos"/> en el instante <paramref name="t"/>, a <paramref name="ancho"/> × <paramref name="alto"/>.</summary>
        public static Texture2D Capturar(CutsceneData datos, float t, int ancho, int alto)
        {
            var prefab = ConstructorCamaraCinematica.Prefab != null ? ConstructorCamaraCinematica.Prefab : ConstructorCamaraCinematica.CrearPrefab();
            var escena = EditorSceneManager.NewPreviewScene();
            var objetivo = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var copia = Object.Instantiate(prefab);
            var camara = copia.GetComponent<CamaraCinematica>();
            var creados = new List<Object> { copia };
            try
            {
                SceneManager.MoveGameObjectToScene(copia, escena);
                camara.Camara.scene = escena;
                camara.Camara.targetTexture = objetivo;
                camara.Empezar();

                var linea = new LineaTiempoCinematica(datos);
                var (abajo, encima) = linea.ImagenesEn(t);
                camara.Pintar(Imagen(datos, abajo), abajo.Alfa, Encuadre(datos, abajo), Imagen(datos, encima), encima.Alfa, Encuadre(datos, encima));
                if (abajo.Indice >= 0 && datos.diapositivas[abajo.Indice].brasas)
                {
                    camara.CapaBrasas.Simular(t - linea.InicioDe(abajo.Indice), abajo.Alfa);
                }

                var textos = linea.TextosEn(t);
                var encuadreTexto = linea.IndiceEn(t) >= 0 ? datos.diapositivas[linea.IndiceEn(t)].desplazamiento : Vector2Int.zero;
                if (datos.fuente != null)
                {
                    camara.Escribir(textos, datos, encuadreTexto);
                }

                camara.Camara.Render();
                var captura = Leer(objetivo);
                if (datos.fuente == null && textos.Count > 0)
                {
                    var camaraTextos = LienzoDeTextos(escena, objetivo, textos, datos, encuadreTexto, creados);
                    camaraTextos.backgroundColor = Color.black;
                    camaraTextos.Render();
                    var sobreNegro = Leer(objetivo);
                    camaraTextos.backgroundColor = Color.white;
                    camaraTextos.Render();
                    var sobreBlanco = Leer(objetivo);
                    Componer(captura, sobreNegro, sobreBlanco);
                    Object.DestroyImmediate(sobreNegro);
                    Object.DestroyImmediate(sobreBlanco);
                }

                return captura;
            }
            finally
            {
                camara.Camara.targetTexture = null;
                foreach (var objeto in creados)
                {
                    if (objeto != null)
                    {
                        Object.DestroyImmediate(objeto);
                    }
                }

                EditorSceneManager.ClosePreviewScene(escena);
                objetivo.Release();
                Object.DestroyImmediate(objetivo);
            }
        }

        private static Sprite Imagen(CutsceneData datos, LineaTiempoCinematica.Capa capa) =>
            capa.Indice >= 0 ? datos.diapositivas[capa.Indice]?.imagen : null;

        private static Vector2Int Encuadre(CutsceneData datos, LineaTiempoCinematica.Capa capa) =>
            capa.Indice >= 0 && datos.diapositivas[capa.Indice] != null ? datos.diapositivas[capa.Indice].desplazamiento : Vector2Int.zero;

        // Un lienzo de cámara con los textos colocados como en el juego, y la cámara que lo dibuja.
        private static Camera LienzoDeTextos(Scene escena, RenderTexture objetivo, List<LineaTiempoCinematica.TextoVisible> textos,
            CutsceneData datos, Vector2Int encuadre, List<Object> creados)
        {
            var capaUi = LayerMask.NameToLayer("UI");
            var objetoCamara = new GameObject("CamaraTextos");
            creados.Add(objetoCamara);
            SceneManager.MoveGameObjectToScene(objetoCamara, escena);
            var camara = objetoCamara.AddComponent<Camera>();
            camara.scene = escena;
            camara.orthographic = true;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.cullingMask = 1 << capaUi;
            camara.targetTexture = objetivo;
            camara.allowMSAA = false;
            objetoCamara.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;

            var objetoLienzo = new GameObject("Lienzo", typeof(RectTransform)) { layer = capaUi };
            creados.Add(objetoLienzo);
            SceneManager.MoveGameObjectToScene(objetoLienzo, escena);
            var lienzo = objetoLienzo.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceCamera;
            lienzo.worldCamera = camara;
            lienzo.planeDistance = 1f;

            var rejilla = CamaraCinematica.RectEnPantalla(objetivo.width, objetivo.height);
            foreach (var visible in textos)
            {
                var texto = FabricaUI.Texto(objetoLienzo.transform, "Texto", string.Empty, 40, Color.white);
                texto.gameObject.layer = capaUi;
                texto.horizontalOverflow = HorizontalWrapMode.Overflow;
                texto.verticalOverflow = VerticalWrapMode.Overflow;
                var sombra = texto.gameObject.AddComponent<Shadow>();
                sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
                PantallaCinematica.ColocarSobreRejilla(texto, sombra, visible.Cartel, datos, rejilla, 1f, encuadre);
                texto.gameObject.AddComponent<CanvasGroup>().alpha = visible.Alfa;
            }

            Canvas.ForceUpdateCanvases();
            return camara;
        }

        private static Texture2D Leer(RenderTexture origen)
        {
            var anterior = RenderTexture.active;
            RenderTexture.active = origen;
            var textura = new Texture2D(origen.width, origen.height, TextureFormat.RGBA32, false);
            textura.ReadPixels(new Rect(0, 0, origen.width, origen.height), 0, 0);
            textura.Apply();
            RenderTexture.active = anterior;
            return textura;
        }

        // La interfaz sobre la imagen: sobre negro da su color ya multiplicado por su opacidad, y la
        // diferencia con la de sobre blanco, lo que deja pasar.
        private static void Componer(Texture2D imagen, Texture2D sobreNegro, Texture2D sobreBlanco)
        {
            var fondo = imagen.GetPixels32();
            var negro = sobreNegro.GetPixels32();
            var blanco = sobreBlanco.GetPixels32();
            for (var i = 0; i < fondo.Length; i++)
            {
                fondo[i] = new Color32(
                    Mezclar(fondo[i].r, negro[i].r, blanco[i].r),
                    Mezclar(fondo[i].g, negro[i].g, blanco[i].g),
                    Mezclar(fondo[i].b, negro[i].b, blanco[i].b),
                    255);
            }

            imagen.SetPixels32(fondo);
            imagen.Apply();
        }

        private static byte Mezclar(byte fondo, byte negro, byte blanco)
        {
            var pasa = Mathf.Clamp01((blanco - negro) / 255f);
            return (byte)Mathf.Clamp(Mathf.RoundToInt(negro + pasa * fondo), 0, 255);
        }
    }
}
