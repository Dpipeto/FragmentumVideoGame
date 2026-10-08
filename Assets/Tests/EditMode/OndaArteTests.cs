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
    /// La onda de la Centinela con su arte (<see cref="ConstructorEfectos"/>): tres fotogramas en
    /// un lienzo común con el pivote en el frente sobre el suelo, su clip en bucle, el prefab, y
    /// las tres ondas de la Centinela (la de suelo, la doble y las del salto) vestidas con él.
    /// </summary>
    public class OndaArteTests
    {
        private static readonly string[] Fotogramas = { "Onda_1_sube", "Onda_2_cresta", "Onda_3_rompe" };

        private static string Ruta(string fotograma) => $"Assets/Sprites/Efectos/{fotograma}.png";

        [Test]
        public void LosFotogramas_CompartenLienzo_YSeImportanComoPixelArtA98()
        {
            foreach (var fotograma in Fotogramas)
            {
                var importador = (TextureImporter)AssetImporter.GetAtPath(Ruta(fotograma));
                Assert.IsNotNull(importador, $"Falta {Ruta(fotograma)}.");
                importador.GetSourceTextureWidthAndHeight(out var ancho, out var alto);
                Assert.AreEqual(new Vector2Int(232, 119), new Vector2Int(ancho, alto), $"{fotograma}: el lienzo común de limpiar_onda.py.");

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Ruta(fotograma));
                Assert.AreEqual(98f, sprite.pixelsPerUnit, fotograma);
                Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, fotograma);
                Assert.AreEqual(210f, sprite.pivot.x, 0.01f, $"{fotograma}: el pivote, en el frente de la onda.");
                Assert.AreEqual(0f, sprite.pivot.y, 0.01f, $"{fotograma}: y en el suelo.");
            }
        }

        [Test]
        public void ElPivote_CaeEnElFrenteDeLaRoca_SobreLaLineaDeSuelo()
        {
            foreach (var fotograma in Fotogramas)
            {
                var textura = new Texture2D(2, 2);
                try
                {
                    Assert.IsTrue(textura.LoadImage(File.ReadAllBytes(Ruta(fotograma))));
                    // Fila de abajo (y = 0 en la textura): el suelo. La roca llega hasta la columna del
                    // pivote y ahí acaba (por delante solo puede haber piedras sueltas).
                    Assert.Greater(textura.GetPixel(209, 0).a, 0.5f, $"{fotograma}: la roca toca el suelo en la columna del pivote.");
                    Assert.Less(textura.GetPixel(210, 0).a, 0.5f, $"{fotograma}: y su frente acaba ahí.");
                }
                finally
                {
                    Object.DestroyImmediate(textura);
                }
            }
        }

        [Test]
        public void ElClip_VaEnBucle_ConLosTresFotogramas()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ConstructorEfectos.OndaAvanza.Ruta(ConstructorEfectos.CarpetaAnimaciones));
            Assert.IsNotNull(clip, "Ejecuta Fragmentum → Jefes → Generar los efectos de los ataques.");
            Assert.IsTrue(clip.isLooping);
            var claves = AnimationUtility.GetObjectReferenceCurve(clip, ClipsDeSprites.BindingSprite);
            CollectionAssert.AreEqual(Fotogramas, claves.Select(k => k.value.name).Take(3).ToArray(), "Sube, se curva y rompe.");
            Assert.AreEqual(0.3f, clip.length, 0.02f);

            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorEfectos.RutaControladorOnda);
            Assert.IsNotNull(controlador);
            Assert.AreSame(clip, controlador.layers[0].stateMachine.defaultState.motion);
            Assert.IsEmpty(controlador.parameters, "Un solo estado, sin parámetros.");
        }

        [Test]
        public void ElPrefab_LlevaElDibujoYSuAnimacion_SinColision()
        {
            var prefab = ConstructorEfectos.PrefabOnda;
            Assert.IsNotNull(prefab, $"Falta {ConstructorEfectos.RutaPrefabOnda}.");
            Assert.AreEqual(Fotogramas[0], prefab.GetComponent<SpriteRenderer>().sprite.name);
            Assert.AreEqual(ConstructorEfectos.OrdenDibujo, prefab.GetComponent<SpriteRenderer>().sortingOrder);
            Assert.IsNotNull(prefab.GetComponent<Animator>().runtimeAnimatorController);
            Assert.IsEmpty(prefab.GetComponentsInChildren<Collider2D>(true), "El daño es de la zona, no del dibujo.");
        }

        [Test]
        public void LasOndasDeLaCentinela_LlevanElDibujo()
        {
            var prefab = ConstructorEfectos.PrefabOnda;
            foreach (var nombre in new[] { "OndaDeSuelo", "OndaDoble" })
            {
                var onda = AssetDatabase.LoadAssetAtPath<PatronOndaSuelo>($"{GeneradorJefes.CarpetaPatrones}/{nombre}.asset");
                Assert.AreSame(prefab, onda.efecto, nombre);
            }

            var salto = AssetDatabase.LoadAssetAtPath<PatronSaltoAplastante>($"{GeneradorJefes.CarpetaPatrones}/SaltoAplastante.asset");
            Assert.AreSame(prefab, salto.efectoOndas, "Las ondas del salto también.");
        }
    }
}
