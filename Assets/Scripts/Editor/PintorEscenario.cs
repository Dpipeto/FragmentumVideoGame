using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using static Fragmentum.EditorTools.UtilidadesEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Viste con el tileset volcánico (<c>Assets/TileMap/tileset_32.png</c>) los bloques de
    /// colisión de una escena: el recorrido provisional del Nivel 1 y las escenas de prueba.
    ///
    /// Solo cambia lo que se ve. Las colisiones siguen siendo las de los bloques, así que el
    /// recorrido, las mecánicas y las pruebas no cambian; se apaga el rectángulo de color de cada
    /// bloque y se pinta encima un Tilemap:
    /// - masa sólida (suelos, techos, paredes): la pieza de ladrillo de 3×3, que encaja sin
    ///   costuras. Los suelos a ras de 0 siguen hacia abajo como cimiento, y los fosos quedan como
    ///   huecos entre acantilados;
    /// - plataformas finas (0,5 u): la viga de hierro, con sus dos extremos;
    /// - en el mundo de la Ira, decoración detrás del jugador: braseros, la estatua del caballero,
    ///   cadenas y escombros, sin tapar ningún objeto del juego.
    ///
    /// Los bloques que no empiezan en una unidad entera (el techo bajo, a 1,2 u) van en un
    /// Tilemap desplazado, para que el dibujo coincida con su colisión.
    ///
    /// Es idempotente: repintar borra lo pintado y lo vuelve a generar igual.
    /// </summary>
    public static class PintorEscenario
    {
        public const string NombreRaiz = "Escenario (tiles)";
        public const string CarpetaTiles = "Assets/TileMap/tileset";

        private const int AltoTileset = 544;
        private const int Cimiento = 7;        // filas de ladrillo bajo un suelo a ras de 0
        private const float FinoMaximo = 0.6f; // por debajo de este alto, un bloque es una plataforma fina
        private const int OrdenTiles = 0;      // Kael va en el 10
        private const int OrdenDecoracion = -1;
        private const float SeparacionDecoracion = 4f; // aire entre dos piezas de decoración

        // Piezas del tileset: (columna, fila contando desde arriba), en celdas de 32 px.
        private static readonly Vector2Int Ladrillo = new Vector2Int(3, 6);       // 3×3
        private static readonly Vector2Int VigaIzquierda = new Vector2Int(20, 12);
        private static readonly Vector2Int VigaDerecha = new Vector2Int(30, 12);
        private const int VigaCentroDesde = 21;
        private const int VigaCentroHasta = 29;

        /// <summary>Un elemento decorativo: un rectángulo de celdas del tileset.</summary>
        private readonly struct Sello
        {
            public Sello(string nombre, int columna, int fila, int ancho, int alto)
            {
                Nombre = nombre;
                Origen = new Vector2Int(columna, fila);
                Tamano = new Vector2Int(ancho, alto);
            }

            public string Nombre { get; }
            public Vector2Int Origen { get; }
            public Vector2Int Tamano { get; }
        }

        private static readonly Sello Estatua = new Sello("Estatua", 3, 1, 5, 4);
        private static readonly Sello Brasero = new Sello("Brasero", 0, 7, 2, 2);
        private static readonly Sello Cadenas = new Sello("Cadenas", 20, 15, 3, 2);
        private static readonly Sello Escombros = new Sello("Escombros", 4, 14, 3, 2);
        private static readonly Sello[] Turnos = { Brasero, Escombros, Brasero, Cadenas };

        // --- Menú y modo batch ---

        [MenuItem("Fragmentum/Niveles/Pintar el escenario de la escena abierta con el tileset", priority = 132)]
        public static void MenuPintar()
        {
            var escena = SceneManager.GetActiveScene();
            var bloques = RaizDeBloques(escena);
            if (bloques == null)
            {
                EditorUtility.DisplayDialog("Fragmentum", "La escena no tiene recorrido provisional («Provisional…» o «Nivel») que vestir.", "Vale");
                return;
            }

            Pintar(escena, bloques, EsDeLaIra(escena));
            EditorSceneManager.MarkSceneDirty(escena);
        }

        /// <summary>
        /// Pinta las escenas de prueba del jugador y de los enemigos y las escenas de nivel de la
        /// Ira que tengan recorrido provisional:
        /// <c>-executeMethod Fragmentum.EditorTools.PintorEscenario.PintarEnModoBatch -quit</c>
        /// </summary>
        public static void PintarEnModoBatch()
        {
            var ok = PintarEscenasDePrueba() & PintarNivelesDeLaIra();
            AssetDatabase.SaveAssets();
            if (!ok && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        public static bool PintarEscenasDePrueba()
        {
            var ok = true;
            foreach (var ruta in new[] { ConstructorJugador.RutaEscena, GeneradorEnemigos.RutaEscena })
            {
                ok &= PintarYGuardar(ruta, false);
            }

            return ok;
        }

        public static bool PintarNivelesDeLaIra()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            if (bd == null)
            {
                return false;
            }

            var rutas = bd.niveles
                .Where(n => n != null && n.emocion != null && n.emocion.id == EMOCIONES.IRA && n.escena.Asignada)
                .Select(n => n.escena.Ruta)
                .ToList();

            var ok = true;
            foreach (var ruta in rutas)
            {
                ok &= PintarYGuardar(ruta, true);
            }

            return ok;
        }

        private static bool PintarYGuardar(string ruta, bool decorar)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) == null)
            {
                Debug.LogWarning($"[Fragmentum] No existe {ruta}: no se pinta.");
                return true;
            }

            var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            var bloques = RaizDeBloques(escena);
            if (bloques == null)
            {
                Debug.Log($"[Fragmentum] {ruta} no tiene recorrido provisional: no se pinta.");
                return true;
            }

            Pintar(escena, bloques, decorar);
            EditorSceneManager.MarkSceneDirty(escena);
            var guardada = EditorSceneManager.SaveScene(escena);
            Debug.Log(guardada ? $"[Fragmentum] Escenario pintado: {ruta}." : $"[Fragmentum] No se pudo guardar {ruta}.");
            return guardada;
        }

        // --- Consultas ---

        /// <summary>El sprite del rectángulo de color de un bloque: así se reconoce lo que hay que vestir.</summary>
        public static Sprite SpriteDeBloque => SpriteCuadrado;

        /// <summary>¿Ya tiene la escena su escenario pintado?</summary>
        public static bool EstaPintada(Scene escena) => escena.GetRootGameObjects().Any(o => o.name == NombreRaiz);

        /// <summary>
        /// El objeto que agrupa los bloques: «Recorrido» en un nivel con su diseño montado,
        /// «Provisional…» en uno sin él, «Nivel» en una escena de prueba.
        /// </summary>
        public static Transform RaizDeBloques(Scene escena)
        {
            var raices = escena.GetRootGameObjects();
            var raiz = raices.FirstOrDefault(o => o.name == PreparadorNivel.NombreRecorrido)
                       ?? raices.FirstOrDefault(o => o.name == PreparadorNivel.NombreProvisional);
            if (raiz == null)
            {
                raiz = raices.FirstOrDefault(o => o.name == "Nivel" && o.GetComponent<Grid>() == null);
            }

            return raiz != null ? raiz.transform : null;
        }

        private static bool EsDeLaIra(Scene escena)
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            return bd != null && bd.niveles.Any(n =>
                n != null && n.emocion != null && n.emocion.id == EMOCIONES.IRA && n.escena.Asignada && n.escena.Ruta == escena.path);
        }

        // --- Pintar ---

        /// <summary>
        /// Viste los bloques de <paramref name="raizBloques"/>. Si ya había un escenario pintado, lo
        /// sustituye. Devuelve cuántos bloques vistió.
        /// </summary>
        public static int Pintar(Scene escena, Transform raizBloques, bool decorar)
        {
            var tiles = CargarTiles();
            if (tiles.Count == 0)
            {
                Debug.LogWarning($"[Fragmentum] No hay tiles en {CarpetaTiles}: el escenario sigue con bloques de color.");
                return 0;
            }

            foreach (var vieja in escena.GetRootGameObjects().Where(o => o.name == NombreRaiz).ToList())
            {
                Object.DestroyImmediate(vieja);
            }

            var raiz = new GameObject(NombreRaiz);
            SceneManager.MoveGameObjectToScene(raiz, escena);
            raiz.AddComponent<Grid>().cellSize = Vector3.one;

            var mapas = new Dictionary<Vector2, Tilemap>();
            Tilemap Mapa(Vector2 desplazamiento)
            {
                if (!mapas.TryGetValue(desplazamiento, out var mapa))
                {
                    var nombre = desplazamiento == Vector2.zero ? "Tiles" : $"Tiles ({Numero(desplazamiento.x)}, {Numero(desplazamiento.y)})";
                    mapa = NuevoTilemap(raiz.transform, nombre, desplazamiento, OrdenTiles);
                    mapas[desplazamiento] = mapa;
                }

                return mapa;
            }

            var bloques = Bloques(raizBloques);
            foreach (var bloque in bloques)
            {
                var rect = bloque.Rect;
                var fino = rect.height <= FinoMaximo;

                // El dibujo se alinea con la cara de arriba (la que se pisa) y con el borde izquierdo.
                var desplazamiento = new Vector2(Fraccion(rect.xMin), Fraccion(rect.yMax));
                var mapa = Mapa(desplazamiento);
                var x0 = Mathf.RoundToInt(rect.xMin - desplazamiento.x);
                var ancho = Mathf.Max(1, Mathf.CeilToInt(rect.width - 0.01f));
                var arriba = Mathf.RoundToInt(rect.yMax - desplazamiento.y) - 1; // fila de la cara de arriba

                if (fino)
                {
                    PintarViga(mapa, tiles, x0, arriba, ancho);
                }
                else
                {
                    var filas = Mathf.Max(1, Mathf.CeilToInt(rect.height - 0.01f));
                    if (bloque.EsSueloARas)
                    {
                        filas += Cimiento;
                    }

                    PintarLadrillo(mapa, tiles, x0, arriba, ancho, filas);
                }

                bloque.Renderer.enabled = false;
            }

            if (decorar)
            {
                var decoracion = NuevoTilemap(raiz.transform, "Decoracion", Vector2.zero, OrdenDecoracion);
                Decorar(escena, decoracion, tiles, bloques, raizBloques);
            }

            foreach (var mapa in mapas.Values)
            {
                mapa.CompressBounds();
            }

            return bloques.Count;
        }

        private static void PintarLadrillo(Tilemap mapa, Dictionary<Vector2Int, TileBase> tiles, int x0, int arriba, int ancho, int filas)
        {
            for (var y = arriba; y > arriba - filas; y--)
            {
                for (var x = x0; x < x0 + ancho; x++)
                {
                    // Patrón anclado al mundo en x y a la cara de arriba en y: los suelos vecinos
                    // continúan el mismo aparejo y la primera fila es siempre la de arriba del bloque.
                    var celda = new Vector2Int(Ladrillo.x + Modulo(x, 3), Ladrillo.y + Modulo(arriba - y, 3));
                    Poner(mapa, tiles, x, y, celda);
                }
            }
        }

        private static void PintarViga(Tilemap mapa, Dictionary<Vector2Int, TileBase> tiles, int x0, int fila, int ancho)
        {
            var centro = VigaCentroHasta - VigaCentroDesde + 1;
            for (var i = 0; i < ancho; i++)
            {
                var celda = ancho == 1 ? new Vector2Int(VigaCentroDesde + 1, VigaIzquierda.y)
                    : i == 0 ? VigaIzquierda
                    : i == ancho - 1 ? VigaDerecha
                    : new Vector2Int(VigaCentroDesde + Modulo(x0 + i, centro), VigaIzquierda.y);
                Poner(mapa, tiles, x0 + i, fila, celda);
            }
        }

        // --- Decoración (mundo de la Ira) ---

        private static void Decorar(Scene escena, Tilemap mapa, Dictionary<Vector2Int, TileBase> tiles, List<Bloque> bloques, Transform raizBloques)
        {
            var ocupado = ZonasOcupadas(escena, raizBloques);
            var suelos = bloques.Where(b => b.EsSueloGrueso).OrderBy(b => b.Rect.xMin).ToList();
            var estatuaPuesta = false;
            var turno = 0;

            foreach (var suelo in suelos)
            {
                // Ni debajo de una plataforma, ni contra una pared, ni encima de un escalón: la
                // decoración no debe parecer que choca. Todo bloque que no sea este suelo estorba.
                var obstaculos = bloques.Where(b => b != suelo).Select(b => b.Rect).ToList();

                // Se busca hueco unidad a unidad y, tras cada pieza, se deja aire: entre enemigos y
                // mecánicas, un paso fijo se saltaba los pocos sitios libres.
                var x = Mathf.Ceil(suelo.Rect.xMin) + 1f;
                while (x + 2f <= suelo.Rect.xMax)
                {
                    var sello = !estatuaPuesta ? Estatua : Turnos[turno % Turnos.Length];
                    var desde = Mathf.RoundToInt(x);
                    var hasta = desde + sello.Tamano.x;
                    var hueco = new Rect(desde - 0.5f, suelo.Rect.yMax, sello.Tamano.x + 1f, sello.Tamano.y + 0.5f);
                    if (hasta > suelo.Rect.xMax - 1f || ocupado.Any(o => o.x < hasta + 1f && o.y > desde - 1f) || obstaculos.Any(r => r.Overlaps(hueco)))
                    {
                        x += 1f;
                        continue;
                    }

                    Estampar(mapa, tiles, sello, desde, Mathf.RoundToInt(suelo.Rect.yMax));
                    if (sello.Nombre == Estatua.Nombre)
                    {
                        estatuaPuesta = true;
                    }
                    else
                    {
                        turno++;
                    }

                    x = hasta + SeparacionDecoracion;
                }
            }
        }

        /// <summary>Tramos en x (desde, hasta) donde hay algo del juego que la decoración no debe tapar.</summary>
        private static List<Vector2> ZonasOcupadas(Scene escena, Transform raizBloques)
        {
            var ocupado = new List<Vector2>();
            foreach (var raiz in escena.GetRootGameObjects())
            {
                if (raiz.name == NombreRaiz)
                {
                    continue;
                }

                // Solo sprites: lo del juego lo es. Una malla suelta (el plano que quedó del proyecto
                // original junto al inicio) no es nada que tapar.
                foreach (var renderer2d in raiz.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer2d.transform.IsChildOf(raizBloques) && renderer2d.GetComponent<BoxCollider2D>() != null &&
                        PrefabUtility.GetOutermostPrefabInstanceRoot(renderer2d.gameObject) == null)
                    {
                        continue; // un bloque del recorrido: es lo que se viste
                    }

                    var limites = renderer2d.bounds;
                    if (limites.size.x < 30f)
                    {
                        ocupado.Add(new Vector2(limites.min.x, limites.max.x));
                    }
                }

                foreach (var colision in raiz.GetComponentsInChildren<Collider2D>(true))
                {
                    if (colision.isTrigger && colision.bounds.size.x < 30f)
                    {
                        ocupado.Add(new Vector2(colision.bounds.min.x, colision.bounds.max.x));
                    }
                }

                // La arena del jefe, entera: el combate se lee mejor sin nada detrás.
                foreach (var arena in raiz.GetComponentsInChildren<Levels.ArenaJefe>(true))
                {
                    ocupado.Add(arena.Limites);
                }
            }

            return ocupado;
        }

        private static void Estampar(Tilemap mapa, Dictionary<Vector2Int, TileBase> tiles, Sello sello, int x0, int suelo)
        {
            // La fila de abajo del sello se apoya en el suelo.
            for (var j = 0; j < sello.Tamano.y; j++)
            {
                for (var i = 0; i < sello.Tamano.x; i++)
                {
                    var celda = new Vector2Int(sello.Origen.x + i, sello.Origen.y + j);
                    Poner(mapa, tiles, x0 + i, suelo + sello.Tamano.y - 1 - j, celda);
                }
            }
        }

        // --- Utilidades ---

        private sealed class Bloque
        {
            public Rect Rect;
            public SpriteRenderer Renderer;
            public bool EsSuelo;

            /// <summary>
            /// Una losa de suelo cuya cara de arriba está a ras de 0 (el recorrido provisional): lleva
            /// cimiento debajo. Los bloques que ya bajan hasta el fondo no lo necesitan.
            /// </summary>
            public bool EsSueloARas => EsSuelo && Mathf.Abs(Rect.yMax) < 0.01f && Rect.height > FinoMaximo && Rect.yMin > -1.01f;

            /// <summary>Un suelo que no es una plataforma fina: se puede decorar encima.</summary>
            public bool EsSueloGrueso => EsSuelo && Rect.height > FinoMaximo;
        }

        /// <summary>
        /// Los bloques de escenario: colisión sólida en la capa del suelo o de las paredes, con el
        /// rectángulo de color de <c>Bloque()</c>. Se dejan fuera las instancias de prefab (las
        /// mecánicas tienen su propio dibujo) y los triggers (agua, hielo, pinchos).
        /// </summary>
        private static List<Bloque> Bloques(Transform raiz)
        {
            var suelo = LayerMask.NameToLayer(Capas.Suelo);
            var pared = LayerMask.NameToLayer(Capas.Wall);
            var cuadrado = SpriteCuadrado;
            var bloques = new List<Bloque>();

            foreach (var caja in raiz.GetComponentsInChildren<BoxCollider2D>(true))
            {
                var objeto = caja.gameObject;
                if (caja.isTrigger || (objeto.layer != suelo && objeto.layer != pared) ||
                    PrefabUtility.GetOutermostPrefabInstanceRoot(objeto) != null)
                {
                    continue;
                }

                var renderer2d = objeto.GetComponent<SpriteRenderer>();
                if (renderer2d == null || renderer2d.sprite != cuadrado)
                {
                    continue;
                }

                var escala = objeto.transform.lossyScale;
                var centro = (Vector2)objeto.transform.position;
                var tamano = new Vector2(Mathf.Abs(escala.x) * caja.size.x, Mathf.Abs(escala.y) * caja.size.y);
                bloques.Add(new Bloque
                {
                    Rect = new Rect(centro - tamano * 0.5f, tamano),
                    Renderer = renderer2d,
                    EsSuelo = objeto.layer == suelo,
                });
            }

            return bloques;
        }

        private static Tilemap NuevoTilemap(Transform padre, string nombre, Vector2 desplazamiento, int orden)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = desplazamiento;
            var mapa = objeto.AddComponent<Tilemap>();
            var renderer2d = objeto.AddComponent<TilemapRenderer>();
            renderer2d.sortingOrder = orden;
            return mapa;
        }

        /// <summary>Los tiles de <see cref="CarpetaTiles"/>, por su celda en el tileset (columna, fila desde arriba).</summary>
        private static Dictionary<Vector2Int, TileBase> CargarTiles()
        {
            var tiles = new Dictionary<Vector2Int, TileBase>();
            foreach (var guid in AssetDatabase.FindAssets("t:Tile", new[] { CarpetaTiles }))
            {
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(guid));
                if (tile == null || tile.sprite == null)
                {
                    continue;
                }

                var r = tile.sprite.rect;
                var celda = new Vector2Int(Mathf.RoundToInt(r.x / 32f), Mathf.RoundToInt((AltoTileset - r.yMax) / 32f));
                tiles[celda] = tile;
            }

            return tiles;
        }

        private static void Poner(Tilemap mapa, Dictionary<Vector2Int, TileBase> tiles, int x, int y, Vector2Int celda)
        {
            if (tiles.TryGetValue(celda, out var tile))
            {
                mapa.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        private static float Fraccion(float valor)
        {
            var f = valor - Mathf.Floor(valor);
            return f > 0.99f || f < 0.01f ? 0f : Mathf.Round(f * 100f) / 100f;
        }

        private static int Modulo(int a, int b) => ((a % b) + b) % b;

        // Con punto decimal en cualquier idioma del sistema: el nombre del Tilemap no debe cambiar de un PC a otro.
        private static string Numero(float valor) => valor.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}
