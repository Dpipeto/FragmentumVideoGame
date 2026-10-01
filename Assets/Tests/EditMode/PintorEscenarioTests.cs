using System.Linq;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El escenario se ve con el tileset volcánico sin que cambie el juego: cada bloque queda
    /// cubierto por tiles alineados con su colisión, el rectángulo de color se apaga y la
    /// colisión sigue igual. Comprueba también las escenas reales.
    /// </summary>
    public class PintorEscenarioTests
    {
        private Scene _escena;
        private Transform _nivel;

        [SetUp]
        public void Preparar()
        {
            _escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _nivel = new GameObject("Nivel").transform;
            Bloque("Suelo", -5f, 5f, -1f, 0f, Capas.Suelo);
            Bloque("Plataforma", 0f, 4f, 2.5f, 3f, Capas.Suelo);
            Bloque("TechoBajo", -4f, -2f, 1.2f, 2.2f, Capas.Suelo);
            Bloque("Pared", 6f, 7f, 0f, 4f, Capas.Wall);
        }

        // Igual que los bloques de las herramientas: escala = tamaño, colisión de 1×1 y rectángulo de color.
        private void Bloque(string nombre, float xMin, float xMax, float yMin, float yMax, string capa)
        {
            var objeto = new GameObject(nombre) { layer = LayerMask.NameToLayer(capa) };
            objeto.transform.SetParent(_nivel, false);
            objeto.transform.position = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
            objeto.transform.localScale = new Vector3(xMax - xMin, yMax - yMin, 1f);
            objeto.AddComponent<SpriteRenderer>().sprite = PintorEscenario.SpriteDeBloque;
            objeto.AddComponent<BoxCollider2D>();
        }

        [TearDown]
        public void Limpiar() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        private Tilemap Mapa(string nombre) =>
            _escena.GetRootGameObjects().Single(o => o.name == PintorEscenario.NombreRaiz)
                .GetComponentsInChildren<Tilemap>().Single(m => m.name == nombre);

        private static bool Hay(Tilemap mapa, int x, int y) => mapa.HasTile(new Vector3Int(x, y, 0));

        [Test]
        public void CadaBloque_QuedaCubiertoPorTilesAlineadosConSuColision()
        {
            Assert.AreEqual(4, PintorEscenario.Pintar(_escena, _nivel, false));
            var tiles = Mapa("Tiles");

            // Suelo de -5 a 5 con la cara de arriba en 0: su fila es la y = -1, y sigue hacia abajo como cimiento.
            Assert.IsTrue(Hay(tiles, -5, -1) && Hay(tiles, 4, -1), "La fila del suelo.");
            Assert.IsFalse(Hay(tiles, -6, -1) || Hay(tiles, 5, -1), "Nada fuera del bloque.");
            Assert.IsTrue(Hay(tiles, 0, -8), "Cimiento bajo el suelo.");
            Assert.IsFalse(Hay(tiles, 0, 0), "Encima del suelo no hay nada.");

            // Plataforma fina (2,5 a 3): una viga en la celda cuya cara de arriba es la de la plataforma.
            Assert.AreNotSame(tiles.GetTile(new Vector3Int(0, 2, 0)), tiles.GetTile(new Vector3Int(1, 2, 0)), "Extremo izquierdo de la viga.");
            Assert.IsTrue(Hay(tiles, 3, 2));
            Assert.IsFalse(Hay(tiles, 0, 1), "La viga es fina: una sola fila.");

            // Pared de 0 a 4.
            Assert.IsTrue(Hay(tiles, 6, 0) && Hay(tiles, 6, 3));
            Assert.IsFalse(Hay(tiles, 6, 4));

            // Techo bajo de 1,2 a 2,2: va en un Tilemap desplazado 0,2 para coincidir con su colisión.
            var desplazado = Mapa("Tiles (0, 0.2)");
            Assert.AreEqual(0.2f, desplazado.transform.position.y, 0.001f);
            Assert.IsTrue(Hay(desplazado, -4, 1) && Hay(desplazado, -3, 1));
            Assert.IsFalse(Hay(tiles, -4, 1), "No se pinta también en el Tilemap sin desplazar.");
        }

        [Test]
        public void SoloCambiaElDibujo_LaColisionSigueIgual()
        {
            var suelo = _nivel.Find("Suelo");
            var caja = suelo.GetComponent<BoxCollider2D>();
            var antes = caja.bounds;

            PintorEscenario.Pintar(_escena, _nivel, false);

            Assert.IsFalse(suelo.GetComponent<SpriteRenderer>().enabled, "El rectángulo de color se apaga.");
            Assert.IsTrue(caja.enabled);
            Assert.AreEqual(antes, caja.bounds);
            Assert.IsNull(_escena.GetRootGameObjects().Single(o => o.name == PintorEscenario.NombreRaiz).GetComponentInChildren<Collider2D>(),
                "Los tiles no tienen colisión propia.");
        }

        [Test]
        public void PintarDosVeces_DejaLoMismo()
        {
            PintorEscenario.Pintar(_escena, _nivel, false);
            var tiles = Mapa("Tiles").GetTilesBlock(Mapa("Tiles").cellBounds).Count(t => t != null);

            PintorEscenario.Pintar(_escena, _nivel, false);

            Assert.AreEqual(1, _escena.GetRootGameObjects().Count(o => o.name == PintorEscenario.NombreRaiz));
            Assert.AreEqual(tiles, Mapa("Tiles").GetTilesBlock(Mapa("Tiles").cellBounds).Count(t => t != null));
        }

        // --- Escenas reales ---

        [Test]
        public void LasEscenasDePrueba_YElNivel1_EstanPintadas()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            var rutas = new[] { ConstructorJugador.RutaEscena, GeneradorEnemigos.RutaEscena, bd.niveles[0].escena.Ruta };

            foreach (var ruta in rutas)
            {
                var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Additive);
                try
                {
                    var raiz = escena.GetRootGameObjects().FirstOrDefault(o => o.name == PintorEscenario.NombreRaiz);
                    Assert.IsNotNull(raiz, $"{ruta} no tiene el escenario pintado: ejecuta Fragmentum → Niveles → Pintar el escenario.");

                    var tiles = raiz.GetComponentsInChildren<Tilemap>().Sum(m => m.GetTilesBlock(m.cellBounds).Count(t => t != null));
                    Assert.Greater(tiles, 50, ruta);

                    var bloques = PintorEscenario.RaizDeBloques(escena).GetComponentsInChildren<SpriteRenderer>(true)
                        .Where(r => r.sprite == PintorEscenario.SpriteDeBloque && r.GetComponent<BoxCollider2D>() is var c && c != null && !c.isTrigger &&
                                    PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject) == null &&
                                    (r.gameObject.layer == LayerMask.NameToLayer(Capas.Suelo) || r.gameObject.layer == LayerMask.NameToLayer(Capas.Wall)))
                        .ToList();
                    Assert.IsNotEmpty(bloques, ruta);
                    Assert.IsTrue(bloques.All(r => !r.enabled), $"{ruta}: quedan bloques con su rectángulo de color.");

                    if (ruta == bd.niveles[0].escena.Ruta)
                    {
                        var decoracion = raiz.GetComponentsInChildren<Tilemap>().Single(m => m.name == "Decoracion");
                        Assert.Greater(decoracion.GetTilesBlock(decoracion.cellBounds).Count(t => t != null), 10,
                            "El Nivel 1 lleva braseros, la estatua, cadenas y escombros.");
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(escena, true);
                }
            }
        }
    }
}
