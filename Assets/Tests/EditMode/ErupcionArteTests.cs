using System.IO;
using System.Linq;
using Fragmentum.Bosses.Patrones;
using Fragmentum.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La erupción de la Centinela con su arte (<see cref="ConstructorEfectos"/>): los dos
    /// fotogramas de la columna en un lienzo común con el pivote en el centro de su base, su
    /// clip en bucle, el prefab, y las dos erupciones de la Centinela vestidas con él.
    /// </summary>
    public class ErupcionArteTests
    {
        private static readonly string[] Fotogramas = { "Erupcion_3_arde", "Erupcion_4_arde" };

        private static string Ruta(string fotograma) => $"Assets/Sprites/Efectos/{fotograma}.png";

        [Test]
        public void LosFotogramas_CompartenLienzo_YSeImportanComoPixelArtA24()
        {
            foreach (var fotograma in Fotogramas)
            {
                var importador = (TextureImporter)AssetImporter.GetAtPath(Ruta(fotograma));
                Assert.IsNotNull(importador, $"Falta {Ruta(fotograma)}.");
                importador.GetSourceTextureWidthAndHeight(out var ancho, out var alto);
                Assert.AreEqual(new Vector2Int(44, 150), new Vector2Int(ancho, alto), $"{fotograma}: el lienzo común de limpiar_erupcion.py.");

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Ruta(fotograma));
                Assert.AreEqual(24f, sprite.pixelsPerUnit, $"{fotograma}: el tamaño de píxel del Golem, la torreta y la bola.");
                Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, fotograma);
                Assert.AreEqual(22f, sprite.pivot.x, 0.01f, $"{fotograma}: el pivote, en el centro de la columna.");
                Assert.AreEqual(0f, sprite.pivot.y, 0.01f, $"{fotograma}: y en su base.");
            }
        }

        [Test]
        public void LaColumna_BrotaDeSuBase_YCubreSuZonaDeDanio()
        {
            var erupcion = AssetDatabase.LoadAssetAtPath<PatronPlataformasErupcion>($"{GeneradorJefes.CarpetaPatrones}/PlataformasEnErupcion.asset");
            Assert.IsNotNull(erupcion);
            foreach (var fotograma in Fotogramas)
            {
                var textura = new Texture2D(2, 2);
                try
                {
                    Assert.IsTrue(textura.LoadImage(File.ReadAllBytes(Ruta(fotograma))));

                    // Fila de abajo (y = 0 en la textura): la base, entera y sin nada debajo.
                    var baseOpaca = Enumerable.Range(0, textura.width).Count(x => textura.GetPixel(x, 0).a > 0.5f);
                    Assert.Greater(baseOpaca, textura.width * 0.6f, $"{fotograma}: la columna se apoya en la fila de abajo.");

                    // Lo que se ve quema: el dibujo es al menos tan ancho y tan alto como la zona de daño.
                    var ancho = textura.width / 24f;
                    var alto = textura.height / 24f;
                    Assert.GreaterOrEqual(ancho, erupcion.tamano.x, $"{fotograma}: más estrecha que su zona, quemaría donde no se ve.");
                    Assert.GreaterOrEqual(alto, erupcion.tamano.y, $"{fotograma}: más baja que su zona, quemaría donde no se ve.");
                }
                finally
                {
                    Object.DestroyImmediate(textura);
                }
            }
        }

        [Test]
        public void ElClip_VaEnBucle_ConLosDosFotogramas()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ConstructorEfectos.ErupcionArde.Ruta(ConstructorEfectos.CarpetaAnimaciones));
            Assert.IsNotNull(clip, "Ejecuta Fragmentum → Jefes → Generar los efectos de los ataques.");
            Assert.IsTrue(clip.isLooping);
            var claves = AnimationUtility.GetObjectReferenceCurve(clip, ClipsDeSprites.BindingSprite);
            CollectionAssert.AreEqual(Fotogramas, claves.Select(k => k.value.name).Take(2).ToArray(), "Las dos llamas alternan.");
            Assert.AreEqual(0.2f, clip.length, 0.02f);

            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorEfectos.RutaControladorErupcion);
            Assert.IsNotNull(controlador);
            Assert.AreSame(clip, controlador.layers[0].stateMachine.defaultState.motion);
            Assert.IsEmpty(controlador.parameters, "Un solo estado, sin parámetros.");
        }

        [Test]
        public void ElPrefab_LlevaElDibujoYSuAnimacion_SinColision()
        {
            var prefab = ConstructorEfectos.PrefabErupcion;
            Assert.IsNotNull(prefab, $"Falta {ConstructorEfectos.RutaPrefabErupcion}.");
            Assert.AreEqual(Fotogramas[0], prefab.GetComponent<SpriteRenderer>().sprite.name);
            Assert.AreEqual(ConstructorEfectos.OrdenDibujo, prefab.GetComponent<SpriteRenderer>().sortingOrder);
            Assert.IsNotNull(prefab.GetComponent<Animator>().runtimeAnimatorController);
            Assert.IsEmpty(prefab.GetComponentsInChildren<Collider2D>(true), "El daño es de la zona, no del dibujo.");
        }

        [Test]
        public void LasErupcionesDeLaCentinela_LlevanElDibujo()
        {
            var prefab = ConstructorEfectos.PrefabErupcion;
            foreach (var nombre in new[] { "PlataformasEnErupcion", "ErupcionEnRafaga" })
            {
                var erupcion = AssetDatabase.LoadAssetAtPath<PatronPlataformasErupcion>($"{GeneradorJefes.CarpetaPatrones}/{nombre}.asset");
                Assert.AreSame(prefab, erupcion.efecto, nombre);
            }
        }
    }
}
