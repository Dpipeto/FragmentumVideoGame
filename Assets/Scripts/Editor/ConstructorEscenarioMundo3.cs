using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Levels.Mecanicas;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El tileset y los props del Mundo 3 (<c>Assets/TileMap/Mundo3</c>), editados por Gemini desde
    /// los del Mundo 2 con la misma distribución: el bosque de noche y la trinchera olvidada. Como
    /// en el Mundo 2 (<see cref="ConstructorEscenarioMundo2"/>), no son una rejilla: cada pieza es un
    /// rectángulo medido sobre su hoja, sin tocar los PNG. Aquí se cortan y se dan las utilidades para
    /// vestir con ellas el Nivel 3.
    ///
    /// Piezas que se usan:
    /// - <see cref="Tierra"/>: 32 × 32 px de tierra oscura con piedras y raíces, elegidos para que se
    ///   repitan sin costura. A 32 px por unidad mide 1 × 1 u: los bloques empiezan en unidades
    ///   enteras y el dibujo casa de uno al siguiente;
    /// - <see cref="Musgo"/>: el borde de hojas muertas y musgo de encima de la tierra, que se repite
    ///   a lo ancho sobre lo que se pisa (asoma 4 píxeles por encima: las briznas);
    /// - <see cref="Raices"/>: la maraña de raíces del muro del callejón, que no agarra;
    /// - las ramas, de las mecánicas (<see cref="ConstructorMecanicasMundo3"/>): la sana, alargada por
    ///   su centro, en las ramas del recorrido y de la arena;
    /// - el <see cref="Tocon"/> del árbol muerto, al doble, en los tres árboles del claro;
    /// - el atrezo que coloca <see cref="DisenoNivel3.Decorados"/>: la tumba del soldado, la lámpara
    ///   de trinchera, la lona podrida y las lianas (de la hoja de tiles), y el refugio, los sacos, la
    ///   caja de munición, el casco entre las hojas, los helechos y el tronco caído (de la de props).
    ///
    /// Sin usar: los remates de tierra con raíces colgando, el montón de ramas, el charco, las ramas
    /// con patas, los postes y la rama con el casco y la cantimplora.
    ///
    /// Los tiles van a 32 px por unidad, como en el Mundo 2; los props, a 64, la densidad de Kael.
    /// </summary>
    public static class ConstructorEscenarioMundo3
    {
        public const string RutaTiles = "Assets/TileMap/Mundo3/TileMap3.png";
        public const string RutaProps = "Assets/TileMap/Mundo3/Props3.png";

        public const int PixelesPorUnidadTiles = 32;
        public const int PixelesPorUnidadProps = 64;

        /// <summary>El tamaño de cada hoja: las piezas son rectángulos medidos sobre ellas.</summary>
        public static readonly Vector2Int TamanoTiles = new Vector2Int(456, 268);
        public static readonly Vector2Int TamanoProps = new Vector2Int(466, 266);

        /// <summary>Los bloques, por detrás de Kael (10) y por delante del fondo.</summary>
        public const int OrdenBloques = 0;

        /// <summary>El musgo, sobre la tierra.</summary>
        public const int OrdenMusgo = 1;

        /// <summary>Los props, detrás de los bloques.</summary>
        public const int OrdenProps = -1;

        /// <summary>Lo que cuelga sobre el frente de una rama: por delante de ella y por detrás de los personajes.</summary>
        public const int OrdenSobreBloques = 2;

        /// <summary>Cuánto se agranda el tocón para ser un árbol del claro: al doble, la densidad de los tiles.</summary>
        public const float EscalaArbol = 2f;

        /// <summary>
        /// Una pieza de una hoja: su rectángulo (x e y desde arriba a la izquierda, en px) y su pivote
        /// (px desde arriba a la izquierda del rectángulo).
        /// </summary>
        public readonly struct Pieza
        {
            public Pieza(string nombre, string hoja, int x, int y, int ancho, int alto, float pivoteX, float pivoteY)
            {
                Nombre = nombre;
                Hoja = hoja;
                Caja = new RectInt(x, y, ancho, alto);
                Pivote = new Vector2(pivoteX, pivoteY);
            }

            public string Nombre { get; }
            public string Hoja { get; }
            public RectInt Caja { get; }
            public Vector2 Pivote { get; }

            public int PixelesPorUnidad => Hoja == RutaTiles ? PixelesPorUnidadTiles : PixelesPorUnidadProps;

            /// <summary>Lo que mide en unidades.</summary>
            public Vector2 Tamano => new Vector2(Caja.width, Caja.height) / PixelesPorUnidad;

            /// <summary>El pivote como lo quiere Unity: de 0 a 1 desde abajo a la izquierda.</summary>
            public Vector2 PivoteNormalizado => new Vector2(Pivote.x / Caja.width, 1f - Pivote.y / Caja.height);

            /// <summary>El rectángulo como lo quiere Unity: con la y desde abajo.</summary>
            public Rect CajaEnLaHoja(int altoHoja) => new Rect(Caja.x, altoHoja - Caja.y - Caja.height, Caja.width, Caja.height);
        }

        // Pivote abajo a la izquierda: se repite desde la esquina del bloque.
        public static readonly Pieza Tierra = new Pieza("Tierra", RutaTiles, 106, 9, 32, 32, 0f, 32f);

        // Pivote en la fila 4 a la izquierda: por encima asoman las briznas; su centro (45 px) casa consigo mismo.
        public static readonly Pieza Musgo = new Pieza("Musgo", RutaTiles, 97, 1, 45, 10, 0f, 4f);

        // Pivote arriba a la izquierda: cuelga del borde de arriba del muro.
        public static readonly Pieza Raices = new Pieza("Raices", RutaTiles, 1, 89, 39, 38, 0f, 0f);

        // --- El atrezo ---

        // De la hoja de tiles. Se apoyan: abajo en el centro.
        public static readonly Pieza Tumba = new Pieza("Tumba", RutaTiles, 358, 187, 89, 77, 44.5f, 75f);

        // Cuelgan: arriba en el centro.
        public static readonly Pieza Lampara = new Pieza("Lampara", RutaTiles, 428, 135, 28, 43, 14f, 1f);
        public static readonly Pieza Lona = new Pieza("Lona", RutaTiles, 252, 142, 72, 58, 36f, 3f);
        public static readonly Pieza Lianas = new Pieza("Lianas", RutaTiles, 367, 137, 47, 57, 23.5f, 1f);

        // De la hoja de props. Se apoyan: abajo en el centro, unas filas por encima de su borde.
        public static readonly Pieza Refugio = new Pieza("Refugio", RutaProps, 291, 12, 100, 132, 50f, 130f);
        public static readonly Pieza Sacos = new Pieza("Sacos", RutaProps, 97, 190, 54, 72, 27f, 70f);
        public static readonly Pieza Caja = new Pieza("Caja", RutaProps, 0, 206, 89, 57, 44.5f, 55f);
        public static readonly Pieza Casco = new Pieza("Casco", RutaProps, 157, 212, 73, 52, 36.5f, 50f);
        public static readonly Pieza Helechos = new Pieza("Helechos", RutaProps, 388, 39, 78, 223, 39f, 221f);
        public static readonly Pieza TroncoCaido = new Pieza("TroncoCaido", RutaProps, 293, 201, 91, 62, 45.5f, 60f);

        // El tocón del árbol muerto, hasta donde empieza el tronco caído (x 292): el árbol del claro.
        public static readonly Pieza Tocon = new Pieza("Tocon", RutaProps, 234, 138, 58, 127, 32f, 125f);

        public static readonly Pieza[] Piezas =
        {
            Tierra, Musgo, Raices, Tumba, Lampara, Lona, Lianas,
            Refugio, Sacos, Caja, Casco, Helechos, TroncoCaido, Tocon,
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

        [MenuItem("Fragmentum/Niveles/Cortar el tileset y los props del Mundo 3", priority = 137)]
        public static void MenuConstruir() => Construir();

        /// <summary>
        /// Corta las dos hojas en sus piezas. Sin las hojas no falla: el Nivel 3 se queda con sus
        /// bloques de color. Devuelve false si una hoja no mide lo que se midió al elegir las piezas
        /// o si alguna pieza no sale.
        /// </summary>
        public static bool Construir()
        {
            if (!HayArte)
            {
                Debug.LogWarning($"[Fragmentum] Faltan {RutaTiles} o {RutaProps}: el Nivel 3 se queda con sus bloques de color.");
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
                Debug.Log($"[Fragmentum] Tileset y props del Mundo 3: {Piezas.Length} piezas cortadas de {RutaTiles} y {RutaProps}.");
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
                border = Vector4.zero,
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
            return sprite != null ? ColocarSprite(padre, nombre, sprite, posicion, orden) : null;
        }

        private static SpriteRenderer ColocarSprite(Transform padre, string nombre, Sprite sprite, Vector2 posicion, int orden)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.position = new Vector3(posicion.x, posicion.y, 0f);
            var dibujo = objeto.AddComponent<SpriteRenderer>();
            dibujo.sprite = sprite;
            dibujo.sortingOrder = orden;
            return dibujo;
        }

        /// <summary>Una pieza de atrezo en su sitio: por detrás de los bloques o colgada sobre ellos, y en espejo si lo pide.</summary>
        public static SpriteRenderer Decorar(Transform padre, in DisenoNivel3.Decorado decorado)
        {
            var dibujo = Colocar(padre, decorado.Nombre, decorado.Pieza, decorado.Posicion, decorado.Delante ? OrdenSobreBloques : OrdenProps);
            if (dibujo != null)
            {
                dibujo.flipX = decorado.Volteado;
            }

            return dibujo;
        }

        /// <summary>
        /// Viste un suelo (de <paramref name="desde"/> a <paramref name="hasta"/> en x y de
        /// <paramref name="abajo"/> a <paramref name="arriba"/> en y): la tierra lo llena desde su
        /// esquina de abajo y el musgo corre por lo alto, que es lo que se pisa. Devuelve la tierra.
        /// </summary>
        public static SpriteRenderer VestirSuelo(Transform padre, string nombre, float desde, float hasta, float abajo, float arriba)
        {
            var tierra = Repetir(padre, nombre, Tierra, new Vector2(desde, abajo), new Vector2(hasta - desde, arriba - abajo), OrdenBloques);
            Repetir(padre, $"{nombre}_Musgo", Musgo, new Vector2(desde, arriba), new Vector2(hasta - desde, Musgo.Tamano.y), OrdenMusgo);
            return tierra;
        }

        /// <summary>
        /// Viste un muro: de tierra, o de raíces si es el del callejón (no agarra), que cuelgan desde
        /// su borde de arriba. Devuelve su dibujo.
        /// </summary>
        public static SpriteRenderer VestirMuro(Transform padre, string nombre, float desde, float hasta, float abajo, float arriba, bool deRaices)
        {
            return deRaices
                ? Repetir(padre, nombre, Raices, new Vector2(desde, arriba), new Vector2(hasta - desde, arriba - abajo), OrdenBloques)
                : Repetir(padre, nombre, Tierra, new Vector2(desde, abajo), new Vector2(hasta - desde, arriba - abajo), OrdenBloques);
        }

        /// <summary>
        /// Viste una rama (de <paramref name="desde"/> a <paramref name="hasta"/>, con su superficie en
        /// <paramref name="arriba"/>) con la rama sana de las mecánicas, alargada por su centro. Devuelve
        /// null si no hay arte.
        /// </summary>
        public static SpriteRenderer VestirRama(Transform padre, string nombre, float desde, float hasta, float arriba)
        {
            var sana = ConstructorMecanicasMundo3.RamaSana;
            if (sana == null)
            {
                return null;
            }

            var dibujo = ColocarSprite(padre, nombre, sana, new Vector2((desde + hasta) * 0.5f, arriba), OrdenBloques);
            dibujo.drawMode = SpriteDrawMode.Tiled;
            dibujo.tileMode = SpriteTileMode.Continuous;
            dibujo.size = new Vector2(ConstructorMecanicasMundo3.AnchoDibujoRama(hasta - desde), ConstructorMecanicasMundo3.AltoDibujoRama);
            return dibujo;
        }

        /// <summary>
        /// Una cinta blanca con su nudo en <paramref name="nudo"/>, moviéndose al viento. Devuelve null
        /// si no hay arte.
        /// </summary>
        public static SpriteRenderer ColocarCinta(Transform padre, string nombre, Vector2 nudo, int orden)
        {
            var fotogramas = ConstructorMecanicasMundo3.FotogramasDeLaCinta;
            if (fotogramas == null)
            {
                return null;
            }

            var dibujo = ColocarSprite(padre, nombre, fotogramas[0], nudo, orden);
            var bucle = dibujo.gameObject.AddComponent<BucleDeSprites>();
            UtilidadesEditor.Asignar(bucle, "fotogramas", p =>
            {
                p.arraySize = fotogramas.Length;
                for (var i = 0; i < fotogramas.Length; i++)
                {
                    p.GetArrayElementAtIndex(i).objectReferenceValue = fotogramas[i];
                }
            });
            UtilidadesEditor.Asignar(bucle, "segundos", p => p.floatValue = ConstructorMecanicasMundo3.SegundosCinta);
            UtilidadesEditor.Asignar(bucle, "desfase", p => p.floatValue = nudo.x * 0.37f % ConstructorMecanicasMundo3.SegundosCinta);
            return dibujo;
        }

        /// <summary>El dibujo de un árbol del claro: el tocón del árbol muerto, al doble, con el pie en <paramref name="pie"/>.</summary>
        public static SpriteRenderer ColocarArbol(Transform padre, string nombre, Vector2 pie)
        {
            var dibujo = Colocar(padre, nombre, Tocon, pie, OrdenProps);
            if (dibujo != null)
            {
                dibujo.transform.localScale = new Vector3(EscalaArbol, EscalaArbol, 1f);
            }

            return dibujo;
        }
    }
}
