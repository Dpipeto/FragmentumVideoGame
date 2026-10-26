using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El tileset y los props del Mundo 4 (<c>Assets/TileMap/Mundo4</c>), editados por Gemini desde los del
    /// Mundo 3 con la misma distribución: el campo de batalla arrasado y el pueblo en ruinas. Como en el
    /// Mundo 3 (<see cref="ConstructorEscenarioMundo3"/>), no son una rejilla: cada pieza es un rectángulo
    /// medido sobre su hoja, sin tocar los PNG. Aquí se cortan y se dan las utilidades para vestir el Nivel 4.
    ///
    /// Piezas que se usan:
    /// - <see cref="Tierra"/>: 32 × 28 px de tierra quemada con piedras, ladrillos y casquillos, del centro
    ///   del segundo bloque (sin su borde oscuro): llena lo que se pisa;
    /// - <see cref="Ceniza"/>: la capa de ceniza y hierba seca de encima, que corre por lo que se pisa;
    /// - <see cref="Mamposteria"/>: la piedra agrietada del puente volado (sus restos y su pilar) y los
    ///   muros de la plaza;
    /// - <see cref="Tejado"/>: la viga quemada con sus tejas rotas, alargada por su centro (los extremos, con
    ///   sus patas, no se repiten): los tejados de las ruinas;
    /// - el atrezo que coloca <see cref="DisenoNivel4.Decorados"/>: de la hoja de tiles, la cruz con su placa,
    ///   el montón de ladrillos, el cráter con agua, la farola rota, la cortina quemada y los cables caídos; de
    ///   la de props, la casa en ruinas, el árbol calcinado, la viga caída, la alambrada de estacas, la caja,
    ///   los sacos y el casco en el barro.
    ///
    /// Sin usar: los otros bloques de tierra, las losas del puente con varillas, los postes y la cruceta con la
    /// máscara de gas.
    ///
    /// Los tiles van a 32 px por unidad, como en los otros mundos; los props, a 64, la densidad de Kael.
    /// </summary>
    public static class ConstructorEscenarioMundo4
    {
        public const string RutaTiles = "Assets/TileMap/Mundo4/TileMap4.png";
        public const string RutaProps = "Assets/TileMap/Mundo4/Props4.png";

        public const int PixelesPorUnidadTiles = 32;
        public const int PixelesPorUnidadProps = 64;

        /// <summary>El tamaño de cada hoja: las piezas son rectángulos medidos sobre ellas.</summary>
        public static readonly Vector2Int TamanoTiles = new Vector2Int(462, 268);
        public static readonly Vector2Int TamanoProps = new Vector2Int(469, 265);

        /// <summary>Los bloques, por detrás de Kael (10) y por delante del fondo.</summary>
        public const int OrdenBloques = 0;

        /// <summary>La ceniza, sobre la tierra.</summary>
        public const int OrdenCeniza = 1;

        /// <summary>El atrezo, detrás de los bloques.</summary>
        public const int OrdenProps = -1;

        /// <summary>Lo que cuelga sobre un tejado o tapa un muro: por delante de él y por detrás de los personajes.</summary>
        public const int OrdenSobreBloques = 2;

        /// <summary>Las coberturas, delante de los bloques y detrás de los personajes (Kael se agacha delante de ellas).</summary>
        public const int OrdenCoberturas = 3;

        /// <summary>Una pieza de una hoja: su rectángulo (x e y desde arriba a la izquierda, en px), su pivote (px desde arriba a la izquierda del rectángulo) y, si se alarga por el centro, lo que no se repite a cada lado.</summary>
        public readonly struct Pieza
        {
            public Pieza(string nombre, string hoja, int x, int y, int ancho, int alto, float pivoteX, float pivoteY, int bordeIzquierdo = 0,
                int bordeDerecho = 0)
            {
                Nombre = nombre;
                Hoja = hoja;
                Caja = new RectInt(x, y, ancho, alto);
                Pivote = new Vector2(pivoteX, pivoteY);
                BordeIzquierdo = bordeIzquierdo;
                BordeDerecho = bordeDerecho;
            }

            public string Nombre { get; }
            public string Hoja { get; }
            public RectInt Caja { get; }
            public Vector2 Pivote { get; }
            public int BordeIzquierdo { get; }
            public int BordeDerecho { get; }

            public int PixelesPorUnidad => Hoja == RutaTiles ? PixelesPorUnidadTiles : PixelesPorUnidadProps;

            /// <summary>Lo que mide en unidades.</summary>
            public Vector2 Tamano => new Vector2(Caja.width, Caja.height) / PixelesPorUnidad;

            /// <summary>El pivote como lo quiere Unity: de 0 a 1 desde abajo a la izquierda.</summary>
            public Vector2 PivoteNormalizado => new Vector2(Pivote.x / Caja.width, 1f - Pivote.y / Caja.height);

            /// <summary>El rectángulo como lo quiere Unity: con la y desde abajo.</summary>
            public Rect CajaEnLaHoja(int altoHoja) => new Rect(Caja.x, altoHoja - Caja.y - Caja.height, Caja.width, Caja.height);

            /// <summary>Los bordes que no se repiten (izquierda, abajo, derecha, arriba), en px.</summary>
            public Vector4 Bordes => new Vector4(BordeIzquierdo, 0f, BordeDerecho, 0f);
        }

        // --- El suelo, los muros y los tejados ---

        // Pivote abajo a la izquierda: se repite desde la esquina del bloque. Del centro del segundo bloque de
        // tierra, entre su capa de ceniza (filas 1-6), la sombra que deja debajo (7-8) y su borde oscuro
        // (filas 35-41): con la sombra dentro, al repetirse salían rayas cada 0,875 u.
        public static readonly Pieza Tierra = new Pieza("Tierra", RutaTiles, 104, 9, 32, 26, 0f, 26f);

        // Pivote en la fila 5 a la izquierda: por encima asoman la ceniza y la hierba seca.
        public static readonly Pieza Ceniza = new Pieza("Ceniza", RutaTiles, 97, 1, 48, 8, 0f, 5f);

        // Pivote abajo a la izquierda, como la tierra: la piedra agrietada del puente y de la torre.
        public static readonly Pieza Mamposteria = new Pieza("Mamposteria", RutaTiles, 1, 89, 39, 38, 0f, 38f);

        // La viga quemada con sus tejas: pivote en su superficie (fila 190 de la hoja), en el centro. Sus
        // extremos, con las patas (x 0-24 y 128-159), no se repiten; su centro, sí.
        public static readonly Pieza Tejado = new Pieza("Tejado", RutaTiles, 0, 184, 159, 33, 79.5f, 6f, 24, 31);

        // --- El atrezo de la hoja de tiles ---

        // Se apoyan: abajo en el centro.
        public static readonly Pieza Cruz = new Pieza("Cruz", RutaTiles, 362, 187, 90, 78, 45f, 76f);
        public static readonly Pieza Escombrera = new Pieza("Escombrera", RutaTiles, 89, 93, 65, 34, 32.5f, 32f);

        // El cráter con agua: medio hundido en el suelo.
        public static readonly Pieza Crater = new Pieza("Crater", RutaTiles, 200, 102, 43, 19, 21.5f, 12f);

        // Cuelgan: arriba en el centro.
        public static readonly Pieza Farola = new Pieza("Farola", RutaTiles, 433, 136, 28, 43, 14f, 1f);
        public static readonly Pieza Cortina = new Pieza("Cortina", RutaTiles, 255, 142, 39, 59, 19.5f, 2f);
        public static readonly Pieza Cables = new Pieza("Cables", RutaTiles, 358, 135, 63, 30, 31.5f, 2f);

        // --- El atrezo de la hoja de props: se apoyan, abajo en el centro, unas filas por encima de su borde ---

        public static readonly Pieza Casa = new Pieza("Casa", RutaProps, 265, 7, 137, 132, 68.5f, 130f);
        public static readonly Pieza Arbol = new Pieza("Arbol", RutaProps, 234, 137, 56, 128, 28f, 126f);
        public static readonly Pieza Viga = new Pieza("Viga", RutaProps, 290, 196, 94, 69, 47f, 67f);
        public static readonly Pieza Alambre = new Pieza("Alambre", RutaProps, 334, 139, 134, 94, 67f, 92f);
        public static readonly Pieza Caja = new Pieza("Caja", RutaProps, 0, 205, 89, 57, 44.5f, 55f);
        public static readonly Pieza Sacos = new Pieza("Sacos", RutaProps, 97, 189, 54, 72, 27f, 70f);
        public static readonly Pieza Casco = new Pieza("Casco", RutaProps, 157, 211, 74, 52, 37f, 50f);

        public static readonly Pieza[] Piezas =
        {
            Tierra, Ceniza, Mamposteria, Tejado, Cruz, Escombrera, Crater, Farola, Cortina, Cables,
            Casa, Arbol, Viga, Alambre, Caja, Sacos, Casco,
        };

        /// <summary>¿Están las dos hojas en el proyecto?</summary>
        public static bool HayArte => File.Exists(RutaTiles) && File.Exists(RutaProps);

        /// <summary>¿Están las hojas cortadas, con todas sus piezas?</summary>
        public static bool Completo => HayArte && Piezas.All(p => Cargar(p) != null);

        /// <summary>El sprite de una pieza, o null si su hoja falta o no está cortada.</summary>
        public static Sprite Cargar(in Pieza pieza)
        {
            var nombre = pieza.Nombre;
            return AssetDatabase.LoadAllAssetRepresentationsAtPath(pieza.Hoja).OfType<Sprite>().FirstOrDefault(s => s.name == nombre);
        }

        [MenuItem("Fragmentum/Niveles/Cortar el tileset y los props del Mundo 4", priority = 138)]
        public static void MenuConstruir() => Construir();

        /// <summary>
        /// Corta las dos hojas en sus piezas. Sin las hojas no falla: el Nivel 4 se queda con sus bloques de
        /// color. Devuelve false si una hoja no mide lo que se midió al elegir las piezas o si alguna no sale.
        /// </summary>
        public static bool Construir()
        {
            if (!HayArte)
            {
                Debug.LogWarning($"[Fragmentum] Faltan {RutaTiles} o {RutaProps}: el Nivel 4 se queda con sus bloques de color.");
                return true;
            }

            var ok = Cortar(RutaTiles, TamanoTiles, PixelesPorUnidadTiles) & Cortar(RutaProps, TamanoProps, PixelesPorUnidadProps);
            foreach (var pieza in Piezas)
            {
                if (Cargar(pieza) == null)
                {
                    Debug.LogError($"[Fragmentum] No sale la pieza {pieza.Nombre} de {pieza.Hoja}.");
                    ok = false;
                }
            }

            if (ok)
            {
                Debug.Log($"[Fragmentum] Tileset y props del Mundo 4: {Piezas.Length} piezas cortadas de {RutaTiles} y {RutaProps}.");
            }

            return ok;
        }

        private static bool Cortar(string ruta, Vector2Int tamano, int pixelesPorUnidad)
        {
            if (!(AssetImporter.GetAtPath(ruta) is TextureImporter importador))
            {
                Debug.LogError($"[Fragmentum] {ruta} no es una textura.");
                return false;
            }

            importador.GetSourceTextureWidthAndHeight(out var ancho, out var alto);
            if (ancho != tamano.x || alto != tamano.y)
            {
                Debug.LogError($"[Fragmentum] {ruta} mide {ancho} × {alto} px y sus piezas se midieron sobre {tamano.x} × {tamano.y}: hay que volver a medirlas.");
                return false;
            }

            // Pixel art: filtro Point, sin compresión ni mipmaps. Malla Full Rect: el modo Tiled la exige.
            var ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);
            ajustes.textureType = TextureImporterType.Sprite;
            ajustes.spriteMode = (int)SpriteImportMode.Multiple;
            ajustes.spritePixelsPerUnit = pixelesPorUnidad;
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            ajustes.spriteGenerateFallbackPhysicsShape = false;
            ajustes.filterMode = FilterMode.Point;
            ajustes.wrapMode = TextureWrapMode.Clamp;
            ajustes.mipmapEnabled = false;
            ajustes.alphaIsTransparency = true;
            importador.SetTextureSettings(ajustes);
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.maxTextureSize = 2048;

            var fabrica = new SpriteDataProviderFactories();
            fabrica.Init();
            var datos = fabrica.GetSpriteEditorDataProviderFromObject(importador);
            datos.InitSpriteEditorDataProvider();

            // Se reutiliza el identificador de cada pieza: la escena las referencia.
            var existentes = new Dictionary<string, GUID>();
            foreach (var r in datos.GetSpriteRects())
            {
                existentes[r.name] = r.spriteID;
            }

            var rects = Piezas.Where(p => p.Hoja == ruta).Select(p => new SpriteRect
            {
                name = p.Nombre,
                rect = p.CajaEnLaHoja(alto),
                alignment = SpriteAlignment.Custom,
                pivot = p.PivoteNormalizado,
                border = p.Bordes,
                spriteID = existentes.TryGetValue(p.Nombre, out var id) ? id : GUID.Generate(),
            }).ToArray();

            datos.SetSpriteRects(rects);
            datos.GetDataProvider<ISpriteNameFileIdDataProvider>()?
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            datos.Apply();
            importador.SaveAndReimport();
            return true;
        }

        // --- Vestir ---

        /// <summary>Una pieza repetida (modo Tiled) hasta ocupar <paramref name="tamano"/> unidades, con su pivote en <paramref name="posicion"/>.</summary>
        public static SpriteRenderer Repetir(Transform padre, string nombre, in Pieza pieza, Vector2 posicion, Vector2 tamano, int orden)
        {
            var dibujo = Colocar(padre, nombre, pieza, posicion, orden);
            if (dibujo != null)
            {
                dibujo.drawMode = SpriteDrawMode.Tiled;
                dibujo.tileMode = SpriteTileMode.Continuous;
                dibujo.size = tamano;
            }

            return dibujo;
        }

        /// <summary>Una pieza tal cual, con su pivote en <paramref name="posicion"/>. Devuelve null si no está.</summary>
        public static SpriteRenderer Colocar(Transform padre, string nombre, in Pieza pieza, Vector2 posicion, int orden)
        {
            var sprite = Cargar(pieza);
            return sprite != null ? ColocarSprite(padre, nombre, sprite, posicion, orden) : null;
        }

        /// <summary>Un sprite suelto (una cobertura, el campanario) con su pivote en <paramref name="posicion"/>.</summary>
        public static SpriteRenderer ColocarSprite(Transform padre, string nombre, Sprite sprite, Vector2 posicion, int orden)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.position = new Vector3(posicion.x, posicion.y, 0f);
            var dibujo = objeto.AddComponent<SpriteRenderer>();
            dibujo.sprite = sprite;
            dibujo.sortingOrder = orden;
            return dibujo;
        }

        /// <summary>Una pieza de atrezo en su sitio: por detrás de los bloques o colgada sobre ellos, en espejo y a su escala si lo pide.</summary>
        public static SpriteRenderer Decorar(Transform padre, in DisenoNivel4.Decorado decorado)
        {
            var dibujo = Colocar(padre, decorado.Nombre, decorado.Pieza, decorado.Posicion, decorado.Delante ? OrdenSobreBloques : OrdenProps);
            if (dibujo != null)
            {
                dibujo.flipX = decorado.Volteado;
                dibujo.transform.localScale = new Vector3(decorado.Escala, decorado.Escala, 1f);
            }

            return dibujo;
        }

        /// <summary>
        /// Viste un suelo: la tierra lo llena desde su esquina de abajo y la ceniza corre por lo alto, que es
        /// lo que se pisa. Devuelve la tierra.
        /// </summary>
        public static SpriteRenderer VestirSuelo(Transform padre, string nombre, float desde, float hasta, float abajo, float arriba)
        {
            var tierra = Repetir(padre, nombre, Tierra, new Vector2(desde, abajo), new Vector2(hasta - desde, arriba - abajo), OrdenBloques);
            Repetir(padre, $"{nombre}_Ceniza", Ceniza, new Vector2(desde, arriba), new Vector2(hasta - desde, Ceniza.Tamano.y), OrdenCeniza);
            return tierra;
        }

        /// <summary>Viste un muro, o el pilar del puente, de piedra agrietada desde su esquina de abajo. Devuelve su dibujo.</summary>
        public static SpriteRenderer VestirMuro(Transform padre, string nombre, float desde, float hasta, float abajo, float arriba) =>
            Repetir(padre, nombre, Mamposteria, new Vector2(desde, abajo), new Vector2(hasta - desde, arriba - abajo), OrdenBloques);

        /// <summary>
        /// Viste un tejado (de <paramref name="desde"/> a <paramref name="hasta"/>, con su superficie en
        /// <paramref name="arriba"/>) con la viga y sus tejas, alargada por su centro. Devuelve null si no hay arte.
        /// </summary>
        public static SpriteRenderer VestirTejado(Transform padre, string nombre, float desde, float hasta, float arriba)
        {
            var dibujo = Colocar(padre, nombre, Tejado, new Vector2((desde + hasta) * 0.5f, arriba), OrdenBloques);
            if (dibujo != null)
            {
                dibujo.drawMode = SpriteDrawMode.Tiled;
                dibujo.tileMode = SpriteTileMode.Continuous;
                dibujo.size = new Vector2(hasta - desde, Tejado.Tamano.y);
            }

            return dibujo;
        }
    }
}
