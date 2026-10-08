using System.IO;
using System.Linq;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La erupción de la Centinela con su arte (<see cref="ConstructorEfectos"/>): los cinco
    /// fotogramas en un lienzo común con el pivote en el centro de la base de la columna, sus
    /// clips, el Animator que sigue las fases de la zona, el prefab y las dos erupciones de la
    /// Centinela vestidas con él.
    /// </summary>
    public class ErupcionArteTests
    {
        private static readonly string[] Fotogramas =
            { "Erupcion_1_aviso", "Erupcion_2_brota", "Erupcion_3_arde", "Erupcion_4_arde", "Erupcion_5_colapsa" };

        private static readonly string[] Columna = { "Erupcion_3_arde", "Erupcion_4_arde" };

        private static string Ruta(string fotograma) => $"Assets/Sprites/Efectos/{fotograma}.png";

        private static AnimatorController Controlador()
        {
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorEfectos.RutaControladorErupcion);
            Assert.IsNotNull(controlador, "Ejecuta Fragmentum → Jefes → Generar los efectos de los ataques.");
            return controlador;
        }

        private static AnimatorState Estado(string nombre)
        {
            var estado = Controlador().layers[0].stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == nombre);
            Assert.IsNotNull(estado, $"Erupcion.controller no tiene el estado {nombre}.");
            return estado;
        }

        private static Texture2D Leer(string fotograma)
        {
            var textura = new Texture2D(2, 2);
            Assert.IsTrue(textura.LoadImage(File.ReadAllBytes(Ruta(fotograma))), fotograma);
            return textura;
        }

        [Test]
        public void LosCincoFotogramas_CompartenLienzo_YSeImportanComoPixelArtA24()
        {
            foreach (var fotograma in Fotogramas)
            {
                var importador = (TextureImporter)AssetImporter.GetAtPath(Ruta(fotograma));
                Assert.IsNotNull(importador, $"Falta {Ruta(fotograma)}.");
                importador.GetSourceTextureWidthAndHeight(out var ancho, out var alto);
                Assert.AreEqual(new Vector2Int(74, 150), new Vector2Int(ancho, alto), $"{fotograma}: el lienzo común de limpiar_erupcion.py.");

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Ruta(fotograma));
                Assert.AreEqual(24f, sprite.pixelsPerUnit, $"{fotograma}: el tamaño de píxel del Golem, la torreta y la bola.");
                Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, fotograma);
                Assert.AreEqual(37f, sprite.pivot.x, 0.01f, $"{fotograma}: el pivote, en el centro de la columna.");
                Assert.AreEqual(0f, sprite.pivot.y, 0.01f, $"{fotograma}: y en su base.");
            }
        }

        [Test]
        public void TodosSeApoyanEnLaFilaDeAbajo_YLaColumnaCubreSuZonaDeDanio()
        {
            var erupcion = AssetDatabase.LoadAssetAtPath<PatronPlataformasErupcion>($"{GeneradorJefes.CarpetaPatrones}/PlataformasEnErupcion.asset");
            Assert.IsNotNull(erupcion);
            foreach (var fotograma in Fotogramas)
            {
                var textura = Leer(fotograma);
                try
                {
                    // Fila de abajo (y = 0 en la textura): la base, sin nada debajo, a ambos lados del pivote.
                    var base0 = Enumerable.Range(0, textura.width).Where(x => textura.GetPixel(x, 0).a > 0.5f).ToList();
                    Assert.GreaterOrEqual(base0.Count, 40, $"{fotograma}: se apoya en la fila de abajo.");
                    Assert.Less(base0.Min(), 37, fotograma);
                    Assert.Greater(base0.Max(), 37, fotograma);

                    if (!Columna.Contains(fotograma))
                    {
                        continue;
                    }

                    // Lo que se ve quema: la columna llena es al menos tan ancha y tan alta como su zona.
                    var ancho = (base0.Max() - base0.Min() + 1) / 24f;
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
        public void LosClips_AvisaBrotaArdeEnBucleYSeApaga()
        {
            foreach (var definicion in ConstructorEfectos.ClipsErupcion)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(definicion.Ruta(ConstructorEfectos.CarpetaAnimaciones));
                Assert.IsNotNull(clip, $"Falta {definicion.Nombre}.");
                Assert.AreSame(clip, Estado(definicion.Estado).motion, definicion.Estado);
                Assert.AreEqual(definicion.Bucle, clip.isLooping, definicion.Nombre);
                Assert.AreEqual(definicion.Duracion, clip.length, 0.02f, definicion.Nombre);
            }

            var arde = AnimationUtility.GetObjectReferenceCurve(
                AssetDatabase.LoadAssetAtPath<AnimationClip>(ConstructorEfectos.ErupcionArde.Ruta(ConstructorEfectos.CarpetaAnimaciones)),
                ClipsDeSprites.BindingSprite);
            CollectionAssert.AreEqual(Columna, arde.Select(k => k.value.name).Take(2).ToArray(), "Las dos llamas alternan.");
        }

        [Test]
        public void ElAnimator_SigueLasFasesDeLaZona()
        {
            var maquina = Controlador().layers[0].stateMachine;
            Assert.AreEqual(ConstructorEfectos.Aviso, maquina.defaultState.name, "Empieza avisando.");
            CollectionAssert.AreEquivalent(new[] { AnimatorParams.Quema.Nombre, AnimatorParams.Apagada.Nombre },
                Controlador().parameters.Select(p => p.name).ToArray());
            Assert.IsEmpty(maquina.anyStateTransitions);

            string Destino(string desde, string parametro) =>
                Estado(desde).transitions.Single(t => t.conditions.Any(c => c.parameter == parametro)).destinationState.name;

            Assert.AreEqual(ConstructorEfectos.Brota, Destino(ConstructorEfectos.Aviso, AnimatorParams.Quema.Nombre), "Al quemar, brota.");
            var llena = Estado(ConstructorEfectos.Brota).transitions.Single(t => t.conditions.Length == 0);
            Assert.AreEqual(ConstructorEfectos.Arde, llena.destinationState.name, "Y pasa sola a arder.");
            Assert.IsTrue(llena.hasExitTime);
            foreach (var desde in new[] { ConstructorEfectos.Aviso, ConstructorEfectos.Brota, ConstructorEfectos.Arde })
            {
                Assert.AreEqual(ConstructorEfectos.Apaga, Destino(desde, AnimatorParams.Apagada.Nombre), $"{desde}: al acabar la zona, se derrumba.");
            }

            Assert.IsEmpty(Estado(ConstructorEfectos.Apaga).transitions, "Derrumbada, se queda hasta desaparecer.");
        }

        [Test]
        public void ElPrefab_LlevaElDibujoQueSigueLaZona_SinColision()
        {
            var prefab = ConstructorEfectos.PrefabErupcion;
            Assert.IsNotNull(prefab, $"Falta {ConstructorEfectos.RutaPrefabErupcion}.");
            Assert.AreEqual(Fotogramas[0], prefab.GetComponent<SpriteRenderer>().sprite.name, "Aparece avisando.");
            Assert.AreEqual(ConstructorEfectos.OrdenDibujo, prefab.GetComponent<SpriteRenderer>().sortingOrder);
            Assert.AreSame(Controlador(), prefab.GetComponent<Animator>().runtimeAnimatorController);
            Assert.AreEqual(ConstructorEfectos.ErupcionApaga.Duracion, prefab.GetComponent<DibujoDeZona>().duracionApagado, 1e-4f,
                "Sigue lo que dura su derrumbe tras acabar la zona.");
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
