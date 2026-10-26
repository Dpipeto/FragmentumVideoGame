using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El tileset y los props del Mundo 2 (<c>Assets/TileMap/Mundo2</c>). No son una rejilla de
    /// celdas, como el tileset volcánico: son dos hojas de piezas sueltas, cada una con su tamaño.
    /// Aquí se cortan (sin tocar los PNG: cada pieza es un rectángulo de la hoja) y se dan las
    /// utilidades para vestir con ellas los bloques del Nivel 2.
    ///
    /// Piezas que se usan:
    /// - <see cref="Ladrillo"/>: 32 × 32 px del muro de ladrillo, elegidos para que se repitan sin
    ///   costura (dos ladrillos de ancho, cuatro hiladas de alto). A 32 px por unidad mide 1 × 1 u,
    ///   y como todos los bloques empiezan en unidades enteras (y en cuartos de unidad en vertical,
    ///   que es lo que mide una hilada), el aparejo casa de un bloque al siguiente;
    /// - <see cref="Hielo"/>: el bloque de hielo agrietado, repetido a lo ancho sobre los suelos que
    ///   resbalan, y <see cref="HieloMacizo"/>, sus 32 filas de arriba, para el techo de hielo y
    ///   para la muralla, que es de hielo liso y no agarra;
    /// - <see cref="Viga"/>: la viga de madera nevada con sus dos patas, para las plataformas finas.
    ///   Las patas son sus bordes (9 cortes): el centro se repite y la viga vale para cualquier ancho;
    /// - <see cref="Silla"/> y <see cref="Campana"/>: la silla vacía del primer checkpoint y la
    ///   campana del campanario;
    /// - el atrezo que coloca <see cref="DisenoNivel2.Decorados"/>: la estatua, el farol, las
    ///   rocas, los estandartes y la cadena (de la hoja de tiles), y la mesa, la campana caída, la
    ///   columna rota y las algas (de la de props).
    ///
    /// Sin usar: los remates de nieve con carámbanos, el charco helado, la repisa pequeña, los
    /// postes, las escuadras, los dos puentes de madera, la ventana y los demás trozos de muro.
    ///
    /// Los tiles van a 32 px por unidad, como el tileset volcánico. Los props, a 64, la densidad de
    /// píxel de Kael: así la silla le llega a la cintura.
    /// </summary>
    public static class ConstructorEscenarioMundo2
    {
        public const string RutaTiles = "Assets/TileMap/Mundo2/TileMap2.png";
        public const string RutaProps = "Assets/TileMap/Mundo2/Props2.png";

        public const int PixelesPorUnidadTiles = 32;
        public const int PixelesPorUnidadProps = 64;

        /// <summary>El tamaño de cada hoja: las piezas son rectángulos medidos sobre ellas.</summary>
        public static readonly Vector2Int TamanoTiles = new Vector2Int(456, 267);
        public static readonly Vector2Int TamanoProps = new Vector2Int(465, 257);

        /// <summary>Por debajo de este alto, un bloque es una plataforma fina: lleva la viga.</summary>
        public const float FinoMaximo = 0.6f;

        /// <summary>Los bloques, por detrás de Kael (10) y por delante del fondo.</summary>
        public const int OrdenBloques = 0;

        /// <summary>El hielo, sobre el ladrillo.</summary>
        public const int OrdenHielo = 1;

        /// <summary>Los props, detrás de los bloques.</summary>
        public const int OrdenProps = -1;

        /// <summary>Lo que cuelga sobre el frente de un bloque: por delante de él y del hielo, y por detrás de los personajes (5 y 10).</summary>
        public const int OrdenSobreBloques = 2;

        /// <summary>
        /// Una pieza de una hoja: su rectángulo (x e y desde arriba a la izquierda, en px), su
        /// pivote (px desde arriba a la izquierda del rectángulo) y, si se estira, lo que miden sus
        /// bordes izquierdo y derecho, que no se repiten.
        /// </summary>
        public readonly struct Pieza
        {
            public Pieza(string nombre, string hoja, int x, int y, int ancho, int alto, float pivoteX, float pivoteY,
                int bordeIzquierdo = 0, int bordeDerecho = 0)
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
        }

        // Pivote abajo a la izquierda: se repite desde la esquina del bloque.
        public static readonly Pieza Ladrillo = new Pieza("Ladrillo", RutaTiles, 111, 5, 32, 32, 0f, 32f);

        // Pivote arriba a la izquierda: su borde de arriba es lo que se pisa.
        public static readonly Pieza Hielo = new Pieza("Hielo", RutaTiles, 1, 88, 39, 38, 0f, 0f);
        public static readonly Pieza HieloMacizo = new Pieza("HieloMacizo", RutaTiles, 1, 88, 39, 32, 0f, 0f);

        // La fila 185 solo lleva un copo de nieve: se pisa en la 186. Las patas y sus carámbanos
        // quedan en los bordes; el centro (100 px) es lo que se repite.
        public static readonly Pieza Viga = new Pieza("Viga", RutaTiles, 1, 185, 156, 30, 0f, 1f, bordeIzquierdo: 32, bordeDerecho: 24);

        // Pivote abajo en el centro: se apoya.
        public static readonly Pieza Silla = new Pieza("Silla", RutaProps, 103, 189, 47, 66, 23.5f, 66f);

        // Pivote arriba en el centro: cuelga.
        public static readonly Pieza Campana = new Pieza("Campana", RutaProps, 133, 0, 110, 129, 55f, 0f);

        // --- El atrezo ---

        // De la hoja de tiles. Se apoyan: el pivote, abajo en el centro y unas filas por encima del
        // borde, que es irregular (nieve): esas filas quedan detrás del suelo y la pieza no flota.
        public static readonly Pieza Estatua = new Pieza("Estatua", RutaTiles, 357, 188, 90, 75, 45f, 72f);
        public static readonly Pieza Farol = new Pieza("Farol", RutaTiles, 428, 138, 28, 41, 14f, 39f);
        public static readonly Pieza Rocas = new Pieza("Rocas", RutaTiles, 91, 97, 59, 29, 29.5f, 27f);

        // Cuelgan: el pivote, arriba en el centro, en la fila de la vara o del primer eslabón.
        public static readonly Pieza Estandartes = new Pieza("Estandartes", RutaTiles, 252, 140, 72, 62, 36f, 5f);
        public static readonly Pieza Cadena = new Pieza("Cadena", RutaTiles, 373, 153, 39, 29, 19.5f, 2f);

        // De la hoja de props. La mesa llega hasta donde empieza la silla, que está pegada a ella.
        public static readonly Pieza Mesa = new Pieza("Mesa", RutaProps, 0, 198, 103, 57, 51.5f, 57f);
        public static readonly Pieza CampanaCaida = new Pieza("CampanaCaida", RutaProps, 162, 192, 72, 64, 36f, 63f);
        public static readonly Pieza ColumnaRota = new Pieza("ColumnaRota", RutaProps, 251, 135, 135, 120, 67.5f, 120f);
        public static readonly Pieza Algas = new Pieza("Algas", RutaProps, 402, 32, 62, 224, 31f, 222f);

        public static readonly Pieza[] Piezas =
        {
            Ladrillo, Hielo, HieloMacizo, Viga, Silla, Campana,
            Estatua, Farol, Rocas, Estandartes, Cadena, Mesa, CampanaCaida, ColumnaRota, Algas,
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

        [MenuItem("Fragmentum/Niveles/Cortar el tileset y los props del Mundo 2", priority = 134)]
        public static void MenuConstruir() => Construir();

        /// <summary>
        /// Corta las dos hojas en sus piezas. Sin las hojas no falla: el Nivel 2 se queda con sus
        /// bloques de color. Devuelve false si una hoja no mide lo que se midió al elegir las piezas
        /// o si alguna pieza no sale.
        /// </summary>
        public static bool Construir()
        {
            if (!HayArte)
            {
                Debug.LogWarning($"[Fragmentum] Faltan {RutaTiles} o {RutaProps}: el Nivel 2 se queda con sus bloques de color.");
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
                Debug.Log($"[Fragmentum] Tileset y props del Mundo 2: {Piezas.Length} piezas cortadas de {RutaTiles} y {RutaProps}.");
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
                border = new Vector4(p.BordeIzquierdo, 0f, p.BordeDerecho, 0f),
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

        /// <summary>
        /// Una pieza repetida (modo Tiled) hasta ocupar <paramref name="tamano"/> unidades, con su
        /// pivote en <paramref name="posicion"/>. Devuelve null si la pieza no está.
        /// </summary>
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
            if (sprite == null)
            {
                return null;
            }

            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.position = new Vector3(posicion.x, posicion.y, 0f);
            var dibujo = objeto.AddComponent<SpriteRenderer>();
            dibujo.sprite = sprite;
            dibujo.sortingOrder = orden;
            return dibujo;
        }

        /// <summary>Una pieza de atrezo en su sitio: por detrás de los bloques o colgada sobre ellos, y en espejo si lo pide.</summary>
        public static SpriteRenderer Decorar(Transform padre, in DisenoNivel2.Decorado decorado)
        {
            var dibujo = Colocar(padre, decorado.Nombre, decorado.Pieza, decorado.Posicion, decorado.Delante ? OrdenSobreBloques : OrdenProps);
            if (dibujo != null)
            {
                dibujo.flipX = decorado.Volteado;
            }

            return dibujo;
        }

        /// <summary>
        /// La pieza que le toca a un bloque de ese alto: la viga si es una plataforma fina, el hielo
        /// macizo si es de hielo y, si no, el ladrillo.
        /// </summary>
        public static Pieza PiezaDeBloque(float alto, bool deHielo) =>
            alto <= FinoMaximo ? Viga : deHielo ? HieloMacizo : Ladrillo;

        /// <summary>
        /// Viste un bloque (de <paramref name="desde"/> a <paramref name="hasta"/> en x y de
        /// <paramref name="abajo"/> a <paramref name="arriba"/> en y) con la pieza que le toca. El
        /// ladrillo llena el bloque desde su esquina de abajo; el hielo y la viga cuelgan de su borde
        /// de arriba, que es lo que se pisa. El hielo macizo llena el bloque entero, repetido también
        /// hacia abajo (la muralla).
        /// </summary>
        public static SpriteRenderer VestirBloque(Transform padre, string nombre, float desde, float hasta, float abajo, float arriba, bool deHielo)
        {
            var ancho = hasta - desde;
            var alto = arriba - abajo;
            var pieza = PiezaDeBloque(alto, deHielo);
            if (pieza.Nombre == Ladrillo.Nombre)
            {
                return Repetir(padre, nombre, pieza, new Vector2(desde, abajo), new Vector2(ancho, alto), OrdenBloques);
            }

            // La viga, con su alto entero (las patas cuelgan por debajo del bloque); el hielo macizo,
            // con el del bloque.
            var altoDibujo = pieza.Nombre == Viga.Nombre ? pieza.Tamano.y : alto;
            return Repetir(padre, nombre, pieza, new Vector2(desde, arriba), new Vector2(ancho, altoDibujo), OrdenBloques);
        }
    }
}
