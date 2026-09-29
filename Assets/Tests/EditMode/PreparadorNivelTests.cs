using System.Collections.Generic;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La herramienta que prepara las escenas de nivel deja jugable una escena vacía y, al
    /// repetirla, no duplica nada.
    /// </summary>
    public class PreparadorNivelTests
    {
        private Scene _escena;
        private LevelData _nivel;

        [SetUp]
        public void Preparar()
        {
            _escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _nivel = ScriptableObject.CreateInstance<LevelData>();
            _nivel.id = 9;
            _nivel.nombreMundo = "Prueba";
            _nivel.paleta = new[] { Color.red, Color.gray, Color.black, Color.white };
            _nivel.secciones = new List<SeccionNivel>
            {
                new SeccionNivel { nombre = "A", desdeX = 0f, hastaX = 20f },
                new SeccionNivel { nombre = "B", desdeX = 20f, hastaX = 40f },
            };
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(_nivel);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void PrepararUnaEscenaVacia_LaDejaJugable()
        {
            Assert.IsTrue(PreparadorNivel.Preparar(_escena, _nivel));

            Assert.AreEqual(1, EscenasNivelTests.TodosEn<LevelManager>(_escena).Count);
            var checkpoints = EscenasNivelTests.TodosEn<Checkpoint>(_escena);
            Assert.AreEqual(1, checkpoints.Count, "Sin checkpoints de diseño, uno al empezar la segunda sección.");
            Assert.AreEqual("L9_CP1", checkpoints[0].Id);
            Assert.AreEqual(21f, checkpoints[0].transform.position.x, 0.01f);
            Assert.AreEqual(38f, EscenasNivelTests.TodosEn<SalidaNivel>(_escena)[0].transform.position.x, 0.01f);
            Assert.IsNotEmpty(EscenasNivelTests.TodosEn<DeadZone>(_escena));
            Assert.AreEqual(1, EscenasNivelTests.TodosEn<EnemyController>(_escena).Count, "Un enemigo por sección salvo la última (la arena).");
        }

        [Test]
        public void PrepararDosVeces_NoDuplicaNada()
        {
            PreparadorNivel.Preparar(_escena, _nivel);
            var raices = _escena.rootCount;

            Assert.IsFalse(PreparadorNivel.Preparar(_escena, _nivel), "La segunda vez no añade nada.");
            Assert.AreEqual(raices, _escena.rootCount);
            Assert.AreEqual(1, EscenasNivelTests.TodosEn<LevelManager>(_escena).Count);
            Assert.AreEqual(1, EscenasNivelTests.TodosEn<Checkpoint>(_escena).Count);
        }

        [Test]
        public void ConSueloPropio_NoAnadeElRecorridoProvisional()
        {
            var suelo = new GameObject("SueloPintado") { layer = LayerMask.NameToLayer(Fragmentum.Core.Capas.Suelo) };
            suelo.AddComponent<BoxCollider2D>();

            PreparadorNivel.Preparar(_escena, _nivel);

            foreach (var raiz in _escena.GetRootGameObjects())
            {
                Assert.AreNotEqual(PreparadorNivel.NombreProvisional, raiz.name, "Si ya hay suelo, no se añade el provisional.");
            }

            Assert.IsEmpty(EscenasNivelTests.TodosEn<EnemyController>(_escena), "Los enemigos provisionales van con el recorrido provisional.");
        }
    }
}
