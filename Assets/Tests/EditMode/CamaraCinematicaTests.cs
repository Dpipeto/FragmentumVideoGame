using Fragmentum.Core;
using Fragmentum.EditorTools;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La cámara de las cinemáticas en pixel art (fase A): la Pixel Perfect Camera de URP
    /// configurada para una rejilla de 240 × 135, los planos a 1 píxel por unidad, y capturas
    /// reales en las que cada píxel del plano es un cuadrado de k × k píxeles idénticos al
    /// original. No se supone: se dibuja y se compara píxel a píxel con el PNG.
    /// </summary>
    public class CamaraCinematicaTests
    {
        private static readonly string[] Planos = { "CinematicaJefe1", "CinematicaJefe2", "CinematicaFinalLvl1" };

        private static CamaraCinematica Prefab()
        {
            var prefab = ConstructorCamaraCinematica.Prefab;
            Assert.IsNotNull(prefab, $"Falta {ConstructorCamaraCinematica.RutaPrefab}: ejecuta Fragmentum → Regenerar todo.");
            return prefab.GetComponent<CamaraCinematica>();
        }

        [Test]
        public void LaCamara_TieneLaPixelPerfectCamera_ConLaRejillaDe240x135()
        {
            var camara = Prefab();
            var ppc = camara.PixelPerfect;
            Assert.IsNotNull(ppc, "La cámara de cinemáticas lleva la Pixel Perfect Camera de URP.");
            Assert.AreEqual(240, ppc.refResolutionX);
            Assert.AreEqual(135, ppc.refResolutionY);
            Assert.AreEqual(1, ppc.assetsPPU, "Un píxel del plano, una unidad.");
            Assert.AreEqual(PixelPerfectCamera.GridSnapping.UpscaleRenderTexture, ppc.gridSnapping, "Dibuja a 240 × 135 y amplía la textura.");
            Assert.AreEqual(PixelPerfectCamera.CropFrame.Windowbox, ppc.cropFrame, "Recorta a la rejilla: lo que sobra, negro.");
            Assert.AreEqual((int)PixelPerfectCamera.PixelPerfectFilterMode.Point,
                new SerializedObject(ppc).FindProperty("m_FilterMode").intValue, "El último paso, también sin interpolar.");

            var cam = camara.Camara;
            Assert.IsTrue(cam.orthographic);
            Assert.AreEqual(1 << LayerMask.NameToLayer(Capas.Cinematica), cam.cullingMask, "Solo ve su capa.");
            Assert.AreEqual(CameraClearFlags.SolidColor, cam.clearFlags);
            Assert.AreEqual(Color.black, cam.backgroundColor);
            Assert.Greater(cam.depth, 0f, "Se dibuja después de la cámara del juego.");
            Assert.AreEqual(CameraRenderType.Base, cam.GetComponent<UniversalAdditionalCameraData>().renderType,
                "La Pixel Perfect Camera de URP no funciona como cámara apilada.");

            Assert.IsFalse(camara.gameObject.activeSelf, "Apagada hasta que haya una cinemática.");
            Assert.AreEqual(LayerMask.NameToLayer(Capas.Cinematica), camara.Plano.gameObject.layer);
            Assert.AreEqual("Sprite-Unlit-Default", camara.Plano.sharedMaterial.name, "Sin las luces 2D del nivel.");
        }

        [Test]
        public void LosPlanos_VanA1PixelPorUnidad_ComoLaCamara()
        {
            var ppc = Prefab().PixelPerfect;
            foreach (var nombre in Planos)
            {
                var sprite = ConstructorCinematicasNivel1.Imagen(nombre);
                Assert.IsNotNull(sprite, $"Falta {nombre}.");
                Assert.AreEqual(ppc.assetsPPU, sprite.pixelsPerUnit, $"{nombre}: los píxeles por unidad de la cámara.");
                Assert.AreEqual(new Vector2(240f, 135f), sprite.rect.size, $"{nombre}: el lienzo entero.");
                Assert.AreEqual(new Vector2(240f, 135f), (Vector2)sprite.bounds.size, $"{nombre}: 240 × 135 unidades, la rejilla entera.");
                Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, nombre);
            }
        }

        [Test]
        public void RectEnPantalla_EsLaReferenciaPorUnEntero_Centrada()
        {
            Assert.AreEqual(new RectInt(0, 0, 1920, 1080), CamaraCinematica.RectEnPantalla(1920, 1080), "×8 justo.");
            Assert.AreEqual(new RectInt(0, 0, 960, 540), CamaraCinematica.RectEnPantalla(960, 540), "×4 justo.");
            Assert.AreEqual(new RectInt(40, 22, 1200, 675), CamaraCinematica.RectEnPantalla(1280, 720), "×5 con borde negro.");
            Assert.AreEqual(new RectInt(0, 60, 1920, 1080), CamaraCinematica.RectEnPantalla(1920, 1200), "16:10: ×8 con franjas arriba y abajo.");
        }

        [TestCase(1920, 1080, 8)]
        [TestCase(1280, 720, 5)]
        [TestCase(960, 540, 4)]
        public void Capturada_CadaPixelDelPlanoEsUnCuadradoDeKxK_SinInterpolar(int ancho, int alto, int k)
        {
            foreach (var nombre in Planos)
            {
                var sprite = ConstructorCinematicasNivel1.Imagen(nombre);
                var captura = ConstructorCamaraCinematica.Capturar(sprite, ancho, alto);
                var original = ConstructorCamaraCinematica.LeerPng(sprite);
                try
                {
                    Assert.AreEqual(k, CamaraCinematica.RectEnPantalla(ancho, alto).width / 240);
                    var problemas = ConstructorCamaraCinematica.ProblemasDeRejilla(captura, original);
                    CollectionAssert.IsEmpty(problemas, $"{nombre} a {ancho} × {alto}: {string.Join("\n", problemas)}");
                }
                finally
                {
                    Object.DestroyImmediate(captura);
                    Object.DestroyImmediate(original);
                }
            }
        }

        [Test]
        public void LaComprobacion_DetectaUnPlanoSuavizado()
        {
            // Lo que no debe pasar: el plano ampliado con filtro bilineal. La comprobación lo ve.
            var sprite = ConstructorCinematicasNivel1.Imagen("CinematicaJefe1");
            var original = ConstructorCamaraCinematica.LeerPng(sprite);
            var destino = new RenderTexture(1920, 1080, 0);
            var suavizada = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            try
            {
                original.filterMode = FilterMode.Bilinear;
                Graphics.Blit(original, destino);
                var anterior = RenderTexture.active;
                RenderTexture.active = destino;
                suavizada.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                suavizada.Apply();
                RenderTexture.active = anterior;

                var problemas = ConstructorCamaraCinematica.ProblemasDeRejilla(suavizada, original);
                Assert.IsNotEmpty(problemas, "Un plano interpolado no pasa.");
                StringAssert.Contains("sin interpolar", problemas[0]);
            }
            finally
            {
                Object.DestroyImmediate(original);
                Object.DestroyImmediate(suavizada);
                destino.Release();
                Object.DestroyImmediate(destino);
            }
        }
    }
}
