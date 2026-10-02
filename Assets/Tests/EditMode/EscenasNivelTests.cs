using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using Fragmentum.Levels.Escenario;
using Fragmentum.Levels.Mecanicas;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La escena del Nivel 1 está preparada para jugarse de principio a fin (Fase 6), y la
    /// herramienta que la prepara solo añade lo que falta.
    /// </summary>
    public class EscenasNivelTests
    {
        private Scene _nivel1;
        private LevelData _datos;

        [OneTimeSetUp]
        public void AbrirNivel1()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            Assert.IsNotNull(bd, "Falta la LevelDatabase: ejecuta Fragmentum → Regenerar todo.");
            _datos = bd.niveles[0];
            _nivel1 = EditorSceneManager.OpenScene(_datos.escena.Ruta, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void CerrarNivel1()
        {
            if (_nivel1.IsValid() && _nivel1.isLoaded)
            {
                EditorSceneManager.CloseScene(_nivel1, true);
            }
        }

        [Test]
        public void ElNivel1_TieneLevelManager_ConPrefabDelJugadorEInicio()
        {
            var gestores = Todos<LevelManager>(_nivel1);
            Assert.AreEqual(1, gestores.Count, "Un LevelManager por escena de nivel.");

            var serializado = new SerializedObject(gestores[0]);
            Assert.IsNotNull(serializado.FindProperty("prefabJugador").objectReferenceValue, "Sin prefab no aparece el jugador.");
            var inicio = serializado.FindProperty("puntoInicio").objectReferenceValue as Transform;
            Assert.IsNotNull(inicio);
            Assert.AreEqual(2f, inicio.position.x, 0.01f, "El jugador empieza al principio de la primera sección.");
            Assert.IsNotNull(gestores[0].GetComponent<CheckpointManager>());
        }

        [Test]
        public void ElNivel1_TieneElCheckpointDelAnexoEnX40()
        {
            var checkpoints = Todos<Checkpoint>(_nivel1);
            Assert.AreEqual(1, checkpoints.Count);
            Assert.AreEqual("L1_CP1", checkpoints[0].Id);
            Assert.AreEqual(40f, checkpoints[0].transform.position.x, 0.01f);
            Assert.IsTrue(checkpoints[0].CompareTag(GameTags.Checkpoint));
            Assert.IsTrue(checkpoints[0].GetComponent<Collider2D>().isTrigger);
        }

        [Test]
        public void ElNivel1_TieneSalida_ZonaMortal_Suelo_YCamaraQueSigue()
        {
            var salidas = Todos<SalidaNivel>(_nivel1);
            Assert.AreEqual(1, salidas.Count);
            Assert.AreEqual(78f, salidas[0].transform.position.x, 0.01f, "Al final de la arena (80 u).");

            Assert.IsNotEmpty(Todos<DeadZone>(_nivel1), "Caer por un foso mata al jugador.");
            var capaSuelo = LayerMask.NameToLayer(Capas.Suelo);
            Assert.IsTrue(Todos<Collider2D>(_nivel1).Any(c => !c.isTrigger && c.gameObject.layer == capaSuelo), "Hay suelo que colisiona.");
            Assert.IsTrue(Todos<Camera>(_nivel1).Any(c => c.GetComponent<CameraMovement>() != null));
        }

        [Test]
        public void ElSueloProvisional_TieneEnemigosDelMundoDeLaIra()
        {
            var enemigos = Todos<EnemyController>(_nivel1);
            Assert.AreEqual(2, enemigos.Count, "Un Eco de la Ira en la sección 1 y el Golem de Ceniza tras el checkpoint.");
            Assert.IsTrue(enemigos.Any(e => e.transform.position.x < 25f && e.name.Contains(ConstructorEcoIra.NombreDatos) &&
                                            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(e.gameObject) == ConstructorEcoIra.RutaPrefab),
                "El Eco de la Ira (con arte) patrulla la sección 1, donde estaba el Soldado de lava provisional.");
            Assert.IsTrue(enemigos.Any(e => e.transform.position.x > 40f && e.name.Contains("GolemCeniza")),
                "Anexo A: el checkpoint de x = 40 va antes del Golem.");

            var rutas = enemigos
                .Select(e => new SerializedObject(e).FindProperty("waypoints").GetArrayElementAtIndex(0).objectReferenceValue as Transform)
                .ToList();
            Assert.AreNotSame(rutas[0], rutas[1], "Cada enemigo tiene su propia ruta.");
            foreach (var enemigo in enemigos)
            {
                var inicio = (new SerializedObject(enemigo).FindProperty("waypoints").GetArrayElementAtIndex(0).objectReferenceValue as Transform).position.x;
                Assert.AreEqual(enemigo.transform.position.x - 3f, inicio, 0.01f, $"La ruta de {enemigo.name} empieza a 3 u de él.");
            }
        }

        internal static List<T> TodosEn<T>(Scene escena) where T : Component => Todos<T>(escena);

        [Test]
        public void ElNivel1_TieneLaArenaConLaCentinelaArdiente()
        {
            var arenas = Todos<ArenaJefe>(_nivel1);
            Assert.AreEqual(1, arenas.Count);
            var arena = arenas[0];

            Assert.IsInstanceOf<CentinelaArdiente>(arena.Jefe, "El jefe de la Ira es la Centinela Ardiente.");
            Assert.AreEqual(new Vector2(60f, 85f), arena.Limites, "La arena ocupa la última sección (55-80 u).");
            Assert.That(arena.Jefe.transform.position.x, Is.InRange(arena.Limites.x, arena.Limites.y));
            Assert.IsTrue(arena.GetComponent<Collider2D>().isTrigger, "La entrada es un trigger.");

            var puertas = new SerializedObject(arena).FindProperty("puertas");
            Assert.AreEqual(1, puertas.arraySize);
            var puerta = puertas.GetArrayElementAtIndex(0).objectReferenceValue as GameObject;
            Assert.IsNotNull(puerta);
            Assert.IsFalse(puerta.activeSelf, "La puerta está abierta hasta que empieza el combate.");
            Assert.Less(puerta.transform.position.x, arena.transform.position.x, "La puerta queda detrás del jugador al entrar.");
        }

        [Test]
        public void ElNivel1_TieneLasMecanicasDeLaIra()
        {
            // Anexo A: plataformas inestables y torreta en la sección 1; plataformas de combustión y lava en la 2.
            var inestables = Todos<PlataformaInestable>(_nivel1);
            Assert.AreEqual(1, inestables.Count);
            Assert.AreEqual(28.5f, inestables[0].transform.position.x, 0.01f, "En el primer foso.");

            var torretas = Todos<TorretaFuego>(_nivel1);
            Assert.AreEqual(1, torretas.Count);
            Assert.That(torretas[0].transform.position.x, Is.InRange(0f, 25f), "La torreta es de la sección 1.");

            var calientes = Todos<PlataformaCaliente>(_nivel1);
            Assert.AreEqual(2, calientes.Count);
            Assert.IsTrue(calientes.All(c => c.transform.position.x > 25f && c.transform.position.x < 40f), "Antes del checkpoint.");

            var lavas = Todos<Lava>(_nivel1);
            Assert.AreEqual(2, lavas.Count, "Un río de lava en la sección 2 y lava en el fondo del segundo foso.");
            Assert.IsTrue(lavas.All(l => l.GetComponent<Collider2D>().isTrigger));
            Assert.IsTrue(lavas.All(l => l.CompareTag(GameTags.Lava)));
        }

        [Test]
        public void LosPrefabsDeMecanicas_Existen()
        {
            foreach (var nombre in new[] { GeneradorMecanicas.PlataformaInestable, GeneradorMecanicas.PlataformaCaliente, GeneradorMecanicas.Lava, GeneradorMecanicas.TorretaFuego })
            {
                Assert.IsNotNull(GeneradorMecanicas.Cargar(nombre), $"Falta el prefab {nombre}.");
            }
        }

        [Test]
        public void PrepararOtraVez_NoCambiaNada()
        {
            Assert.IsFalse(PreparadorNivel.Preparar(_nivel1, _datos), "La herramienta es idempotente.");
            Assert.IsFalse(_nivel1.isDirty);
        }

        [Test]
        public void LosCheckpointsDeDiseno_MandanSobreLasSecciones()
        {
            var nivel = ScriptableObject.CreateInstance<LevelData>();
            try
            {
                nivel.secciones = new List<SeccionNivel>
                {
                    new SeccionNivel { desdeX = 0f, hastaX = 25f },
                    new SeccionNivel { desdeX = 25f, hastaX = 55f },
                    new SeccionNivel { desdeX = 55f, hastaX = 80f },
                };
                CollectionAssert.AreEqual(new[] { 26f, 56f }, PreparadorNivel.PosicionesCheckpoint(nivel));

                nivel.puntosDeControl = new List<float> { 40f };
                CollectionAssert.AreEqual(new[] { 40f }, PreparadorNivel.PosicionesCheckpoint(nivel));
            }
            finally
            {
                Object.DestroyImmediate(nivel);
            }
        }

        private static List<T> Todos<T>(Scene escena) where T : Component =>
            escena.GetRootGameObjects().SelectMany(raiz => raiz.GetComponentsInChildren<T>(true)).ToList();
    }
}
