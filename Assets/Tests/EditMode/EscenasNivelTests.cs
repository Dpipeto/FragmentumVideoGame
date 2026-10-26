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
        public void ElNivel1_TieneElCheckpointDelDiseno_SobreElSueloDelPatio()
        {
            var checkpoints = Todos<Checkpoint>(_nivel1);
            Assert.AreEqual(1, checkpoints.Count);
            Assert.AreEqual("L1_CP1", checkpoints[0].Id);
            Assert.AreEqual(DisenoNivel1.Checkpoint, checkpoints[0].transform.position.x, 0.01f, "Tras la subida y antes del Golem.");
            Assert.AreEqual(DisenoNivel1.SueloEn(DisenoNivel1.Checkpoint) + 0.05f, checkpoints[0].transform.position.y, 0.01f, "De pie en el suelo, no en el aire.");
            Assert.IsTrue(checkpoints[0].CompareTag(GameTags.Checkpoint));
            Assert.IsTrue(checkpoints[0].GetComponent<Collider2D>().isTrigger);
        }

        [Test]
        public void ElNivel1_TieneSalida_ZonaMortal_Suelo_YCamaraQueSigue()
        {
            var salidas = Todos<SalidaNivel>(_nivel1);
            Assert.AreEqual(1, salidas.Count);
            Assert.AreEqual(new Vector2(DisenoNivel1.Salida, DisenoNivel1.SueloArena), (Vector2)salidas[0].transform.position, "Al fondo de la arena, en su suelo.");

            Assert.IsNotEmpty(Todos<DeadZone>(_nivel1), "Caer por un foso mata al jugador.");
            var capaSuelo = LayerMask.NameToLayer(Capas.Suelo);
            Assert.IsTrue(Todos<Collider2D>(_nivel1).Any(c => !c.isTrigger && c.gameObject.layer == capaSuelo), "Hay suelo que colisiona.");
            Assert.IsTrue(Todos<Camera>(_nivel1).Any(c => c.GetComponent<CameraMovement>() != null));
        }

        [Test]
        public void ElNivel1_TieneElRecorridoDeDiseno_EnLugarDelProvisional()
        {
            var raices = _nivel1.GetRootGameObjects();
            Assert.IsFalse(raices.Any(r => r.name == PreparadorNivel.NombreProvisional), "El provisional ya no está.");
            Assert.AreEqual(1, raices.Count(r => r.name == PreparadorNivel.NombreRecorrido));

            var bloques = PreparadorNivel.RaizRecorrido(_nivel1).GetComponentsInChildren<BoxCollider2D>(true)
                .Where(c => PrefabUtility.GetOutermostPrefabInstanceRoot(c.gameObject) == null).ToList();
            foreach (var esperado in DisenoNivel1.Suelos.Concat(DisenoNivel1.Paredes).Concat(DisenoNivel1.PlataformasArena))
            {
                var bloque = bloques.SingleOrDefault(b => b.name == esperado.Nombre);
                Assert.IsNotNull(bloque, $"Falta el bloque {esperado.Nombre}.");
                var rect = Rectangulo(bloque);
                Assert.AreEqual(esperado.Desde, rect.xMin, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Hasta, rect.xMax, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Arriba, rect.yMax, 0.01f, esperado.Nombre);
            }
        }

        [Test]
        public void ElNivel1_TieneLosEnemigosDelAnexo_ConLosEcosALaEscalaDelDisenador()
        {
            var enemigos = Todos<EnemyController>(_nivel1);
            Assert.AreEqual(DisenoNivel1.Enemigos.Length, enemigos.Count);

            // Anexo A: 3 soldados en la sección 1 (los Ecos ocupan su sitio) y 4 más el Golem en la 2.
            var ecos = enemigos.Where(e => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(e.gameObject) == ConstructorEcoIra.RutaPrefab).ToList();
            var golems = enemigos.Where(e => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(e.gameObject) == ConstructorGolem.RutaPrefab).ToList();
            Assert.AreEqual(3, ecos.Count(e => e.transform.position.x < 25f), "Tres Ecos en la sección 1.");
            Assert.AreEqual(4, ecos.Count(e => e.transform.position.x >= 25f && e.transform.position.x < 55f), "Cuatro Ecos en la sección 2.");
            Assert.AreEqual(1, golems.Count);
            Assert.Greater(golems[0].transform.position.x, DisenoNivel1.Checkpoint, "El checkpoint va antes del Golem.");

            foreach (var eco in ecos)
            {
                Assert.AreEqual(DisenoNivel1.EscalaEco.x, eco.transform.localScale.x, 1e-4f, $"{eco.name}: la escala que dejó el diseñador (PENDIENTES N18).");
                Assert.AreEqual(DisenoNivel1.EscalaEco.y, eco.transform.localScale.y, 1e-4f, eco.name);
            }

            foreach (var enemigo in enemigos)
            {
                var pies = enemigo.transform.position;
                Assert.AreEqual(DisenoNivel1.SueloEn(pies.x) + 0.05f, pies.y, 0.01f, $"{enemigo.name} está de pie sobre su suelo.");

                var ruta = new SerializedObject(enemigo).FindProperty("waypoints");
                Assert.AreEqual(2, ruta.arraySize, enemigo.name);
                var a = (ruta.GetArrayElementAtIndex(0).objectReferenceValue as Transform).position.x;
                var b = (ruta.GetArrayElementAtIndex(1).objectReferenceValue as Transform).position.x;
                Assert.That(pies.x, Is.InRange(a, b), $"{enemigo.name} empieza dentro de su ruta.");
                Assert.AreEqual(DisenoNivel1.SueloEn(a), DisenoNivel1.SueloEn(b), $"La ruta de {enemigo.name} no cambia de suelo.");
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
            Assert.AreEqual(DisenoNivel1.LimitesArena, arena.Limites, "Por dentro de la puerta y de la pared: 25 u de ancho.");
            Assert.AreEqual(DisenoNivel1.Jefe, arena.Jefe.transform.position.x, 0.01f);
            Assert.AreEqual(DisenoNivel1.SueloArena + 0.05f, arena.Jefe.transform.position.y, 0.01f, "En el suelo de la arena.");
            Assert.IsTrue(arena.GetComponent<Collider2D>().isTrigger, "La entrada es un trigger.");

            var puertas = new SerializedObject(arena).FindProperty("puertas");
            Assert.AreEqual(1, puertas.arraySize);
            var puerta = puertas.GetArrayElementAtIndex(0).objectReferenceValue as GameObject;
            Assert.IsNotNull(puerta);
            Assert.IsFalse(puerta.activeSelf, "La puerta está abierta hasta que empieza el combate.");
            Assert.Less(puerta.transform.position.x, arena.transform.position.x, "La puerta queda detrás del jugador al entrar.");

            // Anexo A: sala de 15 u de alto con plataformas a tres alturas, que el jefe atraviesa.
            Assert.AreEqual(15f, DisenoNivel1.TechoArena - DisenoNivel1.SueloArena);
            Assert.AreEqual(DisenoNivel1.PlataformasArena.Length, arena.Plataformas.Count);
            Assert.IsTrue(arena.Plataformas.All(p => p != null && !p.isTrigger && p.gameObject.layer == LayerMask.NameToLayer(Capas.Suelo)));
            Assert.AreEqual(3, arena.Plataformas.Select(p => Mathf.Round(Rectangulo((BoxCollider2D)p).yMax)).Distinct().Count(), "Tres alturas.");
        }

        [Test]
        public void ElNivel1_TieneLasMecanicasDeLaIra_DondeDiceElDiseno()
        {
            foreach (var pieza in DisenoNivel1.Mecanicas)
            {
                var instancia = Todos<Transform>(_nivel1).SingleOrDefault(t => t.name == pieza.Nombre);
                Assert.IsNotNull(instancia, $"Falta {pieza.Nombre}.");
                Assert.AreEqual(GeneradorMecanicas.Carpeta + $"/{pieza.Prefab}.prefab", PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instancia.gameObject));
                Assert.AreEqual(new Vector2(pieza.X, pieza.Y), (Vector2)instancia.position, pieza.Nombre);
            }

            // Anexo A: plataformas inestables y torreta en la sección 1; plataformas de combustión y lava en la 2.
            var torretas = Todos<TorretaFuego>(_nivel1);
            Assert.AreEqual(1, torretas.Count);
            Assert.That(torretas[0].transform.position.x, Is.InRange(0f, 25f), "La torreta es de la sección 1.");
            Assert.AreEqual(-1, torretas[0].Mirando, "Dispara hacia el jugador que llega.");

            var combustion = Todos<PlataformaCombustion>(_nivel1).OrderBy(c => c.transform.position.x).ToList();
            Assert.AreEqual(2, combustion.Count);
            Assert.IsTrue(combustion.All(c => c.transform.position.x > 25f && c.transform.position.x < DisenoNivel1.Checkpoint), "Antes del checkpoint.");
            Assert.AreNotEqual(combustion[0].desfase, combustion[1].desfase, "Desfasadas: hay que esperar el momento de cada una.");

            Assert.AreEqual(2, Todos<PlataformaInestable>(_nivel1).Count, "Una sobre el foso que no mata y otra sobre la lava.");

            var lavas = Todos<Lava>(_nivel1);
            Assert.AreEqual(DisenoNivel1.Lavas.Length, lavas.Count);
            Assert.IsTrue(lavas.All(l => l.GetComponent<Collider2D>().isTrigger));
            Assert.IsTrue(lavas.All(l => l.CompareTag(GameTags.Lava)));
            foreach (var lago in DisenoNivel1.Lavas)
            {
                var lava = lavas.Single(l => l.name == lago.Nombre);
                Assert.GreaterOrEqual(lava.Ancho, lago.Hasta - lago.Desde, $"{lago.Nombre} cubre su hueco.");
                Assert.AreEqual(lava.Ancho, ((BoxCollider2D)lava.GetComponent<Collider2D>()).size.x, 0.01f, $"{lago.Nombre}: el trigger mide lo que sus franjas.");
            }
        }

        [Test]
        public void LosPrefabsDeMecanicas_Existen()
        {
            foreach (var nombre in new[] { GeneradorMecanicas.PlataformaInestable, GeneradorMecanicas.PlataformaCombustion, GeneradorMecanicas.Lava, GeneradorMecanicas.TorretaFuego, GeneradorMecanicas.BolaDeFuego })
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

        // El rectángulo de una caja por su Transform (en el editor, sin depender de la física).
        private static Rect Rectangulo(BoxCollider2D caja)
        {
            var escala = caja.transform.lossyScale;
            var tamano = new Vector2(Mathf.Abs(escala.x) * caja.size.x, Mathf.Abs(escala.y) * caja.size.y);
            var centro = (Vector2)caja.transform.position + Vector2.Scale(caja.offset, escala);
            return new Rect(centro - tamano * 0.5f, tamano);
        }
    }
}
