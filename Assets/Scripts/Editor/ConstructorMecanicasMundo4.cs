using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Las mecánicas y las piezas sueltas del Mundo 4 (Culpa) con su arte (<c>Prompts_Mundo4.md</c>, segunda
    /// tanda, apartado E):
    /// - <b>el barril</b>: intacto, con la mecha encendida (dos fotogramas, parpadea) y quemado
    ///   (<see cref="Fragmentum.Levels.Mecanicas.BarrilExplosivo"/> cambia el dibujo con su estado);
    /// - <b>la mina</b>: quieta y avisando (parpadea con Kael cerca); su cadena es la del Verdugo, girada;
    /// - <b>el agua del río</b>: la superficie, que se repite a lo ancho y en bucle, como la de la ciénaga;
    /// - <b>la alambrada sumergida</b>: se mece y se cierra cuando sujeta a Kael (la maraña del Mundo 3);
    /// - <b>las coberturas</b>: el escombro, los sacos largos del refugio, los sacos bajos de los
    ///   Artilleros, la chimenea y el tanque volcado;
    /// - <b>el campanario</b>, con su arco y su balcón;
    /// - <b>las cuatro memorias</b> (la radio, la campana, el casco sobre el fusil y la carta), apagadas y
    ///   brillando, y <b>la losa</b> de la plaza sobre la que espera cada una, apagada y encendida.
    ///
    /// Llegaron cada una con su píxel (las coberturas, en una sola imagen y con restos de la referencia
    /// alrededor: se limpiaron por separado). Cada una va a la escala que le da su tamaño en el juego: las
    /// coberturas, a su alto, para que su lomo quede donde se pisa (su ancho sobra un poco por los lados);
    /// el campanario, a la altura de su balcón. El pivote, en el pie o en la superficie.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo4</c> y no se retocan. Los prefabs del barril y de
    /// la mina los monta <see cref="GeneradorMecanicas"/>; lo demás, <c>PreparadorNivelCulpa</c>. Si falta el
    /// arte de una pieza, esa se queda con su rectángulo. Idempotente.
    /// </summary>
    public static class ConstructorMecanicasMundo4
    {
        public const string Carpeta = TablaArte.CarpetaMecanicas + "/Mundo4";

        public static string Barril(string archivo) => $"{Carpeta}/Barril/{archivo}.png";
        public static string Mina(string archivo) => $"{Carpeta}/Mina/{archivo}.png";
        public static string Agua(string archivo) => $"{Carpeta}/Agua/{archivo}.png";
        public static string Alambrada(string archivo) => $"{Carpeta}/Alambrada/{archivo}.png";
        public static string Cobertura(string archivo) => $"{Carpeta}/Coberturas/{archivo}.png";
        public static string RutaCampanario => $"{Carpeta}/Campanario/Campanario.png";
        public static string Memoria(string archivo) => $"{Carpeta}/Memorias/{archivo}.png";

        // --- Lienzos, escalas y pivotes ---

        private static readonly Vector2Int LienzoBarril = new Vector2Int(69, 100);
        private static readonly Vector2Int LienzoMina = new Vector2Int(37, 47);
        private static readonly Vector2Int LienzoAgua = new Vector2Int(180, 37);
        private static readonly Vector2Int LienzoAlambrada = new Vector2Int(86, 86);
        public static readonly Vector2Int LienzoCampanario = new Vector2Int(135, 330);

        /// <summary>El barril mide 77 píxeles del fondo a lo alto del borde (la mecha sobresale): a 78 por unidad, 1 u y 0,65 de ancho.</summary>
        public const float PixelesPorUnidadBarril = 78f;

        /// <summary>La bola de la mina mide 32 píxeles: a 40 por unidad, los 0,8 u de su choque. Con los cuernos y la argolla, 1,1 u.</summary>
        public const float PixelesPorUnidadMina = 40f;

        /// <summary>El agua del río, a la escala de la del Mundo 2: llegó en su misma rejilla (240 a lo ancho).</summary>
        public const float PixelesPorUnidadAgua = ConstructorMecanicasMundo2.PixelesPorUnidadAgua;

        /// <summary>La fila de la línea del agua: la primera entera. Por encima flotan la ceniza y la madera.</summary>
        public const int FilaDelAgua = 6;

        /// <summary>Las filas vacías bajo el dibujo del agua: ahí sigue el cuerpo liso.</summary>
        public const int FilasVaciasBajoElAgua = 1;

        /// <summary>Lo que mide la superficie del agua por debajo de su línea: ahí sigue el cuerpo liso.</summary>
        public static float FondoDeLaSuperficie => (LienzoAgua.y - FilaDelAgua - FilasVaciasBajoElAgua) / PixelesPorUnidadAgua;

        /// <summary>El color de la última fila de la superficie: el del cuerpo del agua, pardo y translúcido.</summary>
        public static readonly Color ColorAgua = new Color32(34, 24, 18, 140);

        /// <summary>La alambrada mide 86 píxeles: a 43 por unidad, los 2 × 2 u de una alambrada. La del segundo ojo (3 u) se repite hacia abajo.</summary>
        public const float PixelesPorUnidadAlambrada = 43f;

        /// <summary>
        /// El campanario: el suelo de su balcón está a 142 píxeles del pie de la torre; a 31,5 por unidad,
        /// a 4,5 u, donde se arrodillan sus Artilleros. La torre mide entonces 2,92 u de ancho (su bloque, 3),
        /// su arco 2,92 de alto y su balcón 1,3 de ancho.
        /// </summary>
        public const float PixelesPorUnidadCampanario = 31.5f;

        /// <summary>El borde izquierdo de la torre (x 42) en su lienzo: el pivote, al pie.</summary>
        public const float BordeTorre = 42f;

        /// <summary>El balcón dibujado: de x 1 a la torre, con el suelo en la fila 188.</summary>
        public const float BordeBalcon = 1f;

        public const int FilaSueloBalcon = 188;

        /// <summary>El arco: su hueco llega a 92 píxeles del pie de la torre.</summary>
        public const int AltoArco = 92;

        /// <summary>Lo que mide en el juego cada medida del campanario: del pie de la torre.</summary>
        public static float AnchoTorre => (LienzoCampanario.x - 1 - BordeTorre) / PixelesPorUnidadCampanario;

        public static float AlturaBalconDibujado => (LienzoCampanario.y - FilaSueloBalcon) / PixelesPorUnidadCampanario;

        public static float AnchoBalconDibujado => (BordeTorre - BordeBalcon) / PixelesPorUnidadCampanario;

        public static float AltoArcoDibujado => AltoArco / PixelesPorUnidadCampanario;

        public static float AltoTorreDibujada => LienzoCampanario.y / PixelesPorUnidadCampanario;

        /// <summary>Una cobertura: su lienzo, su escala (a su alto) y el ancla (el pie, en el centro de lo que se pisa).</summary>
        public sealed class Pieza
        {
            public readonly string Nombre;
            public readonly Vector2Int Lienzo;
            public readonly float PixelesPorUnidad;
            public readonly Vector2 Ancla;
            public readonly string Nota;

            public Pieza(string nombre, int ancho, int alto, float ppu, float anclaX, float anclaY, string nota)
            {
                Nombre = nombre;
                Lienzo = new Vector2Int(ancho, alto);
                PixelesPorUnidad = ppu;
                Ancla = new Vector2(anclaX, anclaY);
                Nota = nota;
            }

            public Vector2 Pivote => new Vector2(Ancla.x / Lienzo.x, (Lienzo.y - Ancla.y) / Lienzo.y);

            public string Ruta => Cobertura(Nombre);

            public Sprite Sprite => AssetDatabase.LoadAssetAtPath<Sprite>(Ruta);
        }

        /// <summary>El escombro: 78 píxeles de alto, a 60 por unidad, 1,3 u (y 1,45 de ancho).</summary>
        public static readonly Pieza Escombro = new Pieza("Escombro", 89, 79, 60f, 44.5f, 78f,
            "Escombro (cobertura de 1 × 1,3 u): 89×79, a 60 px/u mide 1,3 u de alto y 1,45 de ancho. El pivote, al pie en el centro.");

        /// <summary>Los sacos del refugio: 93 de alto, a 71,5 por unidad, 1,3 u (y 1,97 de ancho).</summary>
        public static readonly Pieza SacosLargos = new Pieza("SacosLargos", 141, 94, 71.5f, 70.5f, 93f,
            "Sacos del refugio (cobertura de 1,5 × 1,3 u): 141×94, a 71,5 px/u, 1,3 u de alto y 1,97 de ancho.");

        /// <summary>Los sacos de cada Artillero: 67 de alto, a 74,4 por unidad, 0,9 u (y 1 de ancho).</summary>
        public static readonly Pieza SacosBajos = new Pieza("SacosBajos", 75, 68, 74.4f, 37.5f, 67f,
            "Sacos de los Artilleros (cobertura de 0,85 × 0,9 u): 75×68, a 74,4 px/u, 0,9 u de alto y 1 de ancho.");

        /// <summary>
        /// La chimenea: el tiro mide 68 píxeles hasta donde empieza el trozo de tejado de su base (fila 68). A
        /// 52,3 por unidad, 1,3 u sobre el tejado; su base de tejas cuelga 0,86 u por debajo, sobre la viga.
        /// </summary>
        public static readonly Pieza Chimenea = new Pieza("Chimenea", 101, 113, 52.3f, 49f, 68f,
            "Chimenea (cobertura de 1 × 1,3 u sobre el tejado): 101×113, el pivote donde el tiro se apoya en su tejado (fila 68). " +
            "A 52,3 px/u, el tiro mide 1,3 u y 1,4 de ancho; las tejas de su base cuelgan por debajo.");

        /// <summary>El tanque volcado: 111 de alto, a 55,5 por unidad, 2 u; el casco (x 0-190), 3,4 de ancho; el cañón sobresale.</summary>
        public static readonly Pieza Tanque = new Pieza("Tanque", 260, 113, 55.5f, 95f, 111f,
            "Tanque volcado (bloque de 3 × 2 u): 260×113, a 55,5 px/u mide 2 u de alto. El pivote, al pie en el centro del casco " +
            "(x 0-190): el cañón sobresale a la derecha.");

        public static readonly Pieza[] Coberturas = { Escombro, SacosLargos, SacosBajos, Chimenea, Tanque };

        /// <summary>
        /// Una memoria con su dibujo (apartado E9): apagada (<c>…1</c>) y brillando (<c>…2</c>: el mismo objeto,
        /// píxel a píxel, con un borde ámbar y unas chispas), en el mismo lienzo. Se pidieron a 32 píxeles por
        /// unidad y salieron con más (Gemini dibujó 100 a lo ancho en lugar de 64): cada una va a la escala que le
        /// da lo que se pidió que midiera. El ancla, al pie, en el centro de la apagada.
        /// </summary>
        public sealed class DibujoMemoria
        {
            public readonly string Nombre;
            public readonly Vector2Int Lienzo;
            public readonly float PixelesPorUnidad;
            public readonly Vector2 Ancla;
            public readonly string Nota;

            public DibujoMemoria(string nombre, int ancho, int alto, float ppu, float anclaX, float anclaY, string nota)
            {
                Nombre = nombre;
                Lienzo = new Vector2Int(ancho, alto);
                PixelesPorUnidad = ppu;
                Ancla = new Vector2(anclaX, anclaY);
                Nota = nota;
            }

            public Vector2 Pivote => new Vector2(Ancla.x / Lienzo.x, (Lienzo.y - Ancla.y) / Lienzo.y);

            public string RutaApagada => Memoria($"{Nombre}1");

            public string RutaBrillando => Memoria($"{Nombre}2");

            public Sprite Apagada => AssetDatabase.LoadAssetAtPath<Sprite>(RutaApagada);

            public Sprite Brillando => AssetDatabase.LoadAssetAtPath<Sprite>(RutaBrillando);

            /// <summary>¿Están sus dos dibujos?</summary>
            public bool Completo => Apagada != null && Brillando != null;

            public TablaArte.Regla Regla => new TablaArte.Regla($"{Carpeta}/Memorias/{Nombre}", PixelesPorUnidad, Pivote, Nota, Lienzo);
        }

        /// <summary>La radio: 74 píxeles de ancho (se pidieron 32, 1 u): a 74 por unidad, 1 × 0,92 u con su antena.</summary>
        public static readonly DibujoMemoria Radio = new DibujoMemoria("Radio", 75, 74, 74f, 37f, 73f,
            "Radio (memoria «La orden»): 75×74, apagada y brillando en el mismo lienzo. A 74 px/u mide 1 u de ancho, lo que se pidió. El pivote, al pie.");

        /// <summary>La campana: 88 de ancho (se pidieron 34, 1,06 u): a 83 por unidad, 1,06 × 0,81 u con su montón de ceniza.</summary>
        public static readonly DibujoMemoria Campana = new DibujoMemoria("Campana", 88, 72, 83f, 44f, 71f,
            "Campana (memoria «El pueblo»): 88×72. A 83 px/u mide 1,06 u de ancho, lo que se pidió. El pivote, al pie.");

        /// <summary>El casco sobre el fusil: 75 de alto (se pidieron 48, 1,5 u): a 50 por unidad, 1,5 u de alto.</summary>
        public static readonly DibujoMemoria Casco = new DibujoMemoria("Casco", 37, 84, 50f, 16f, 83f,
            "Casco sobre el fusil (memoria «El compañero»): 37×84. A 50 px/u mide 1,5 u de alto, lo que se pidió. El pivote, al pie.");

        /// <summary>La carta sobre la caja: 56 de ancho (se pidieron 32, 1 u): a 56 por unidad, 1 × 0,79 u.</summary>
        public static readonly DibujoMemoria Carta = new DibujoMemoria("Carta", 57, 54, 56f, 28f, 54f,
            "Carta sobre la caja de munición (memoria «La carta»): 57×54. A 56 px/u mide 1 u de ancho, lo que se pidió. El pivote, al pie.");

        /// <summary>
        /// La losa de la plaza, apagada y encendida: 48 × 10, a 34,3 por unidad, los 1,4 u de ancho de una marca. Va
        /// metida en el suelo, a ras: el pivote, arriba en el centro.
        /// </summary>
        public static readonly DibujoMemoria Losa = new DibujoMemoria("Losa", 48, 10, 34.3f, 24f, 0f,
            "Losa de la plaza (la marca del Juez): 48×10, apagada y encendida. A 34,3 px/u mide 1,4 u de ancho, lo que una marca. " +
            "El pivote, arriba en el centro: va a ras del suelo.");

        /// <summary>Los dibujos de las cuatro memorias, en el orden de <c>DisenoNivel4.Memorias</c>.</summary>
        public static readonly DibujoMemoria[] Memorias = { Radio, Campana, Casco, Carta };

        // Píxeles desde arriba a la izquierda → pivote de Unity.
        private static Vector2 Pivote(float x, float y, Vector2Int lienzo) => new Vector2(x / lienzo.x, (lienzo.y - y) / lienzo.y);

        /// <summary>El centro de la bola de la mina (x 17,5, fila 23,5): ahí está su choque.</summary>
        public static Vector2 PivoteMina => Pivote(17.5f, 23.5f, LienzoMina);

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
            {
                new TablaArte.Regla($"{Carpeta}/Barril/", PixelesPorUnidadBarril, Pivote(34.5f, 99f, LienzoBarril),
                    "Barril: 69×100, el pivote al pie en el centro. A 78 px/u mide 1 u de alto y 0,65 de ancho; la mecha y el humo, por encima.",
                    LienzoBarril),
                new TablaArte.Regla($"{Carpeta}/Mina/", PixelesPorUnidadMina, PivoteMina,
                    "Mina: 37×47, el pivote en el centro de la bola. A 40 px/u la bola mide 0,8 u, lo que su choque.", LienzoMina),
                new TablaArte.Regla($"{Carpeta}/Agua/", PixelesPorUnidadAgua, Pivote(LienzoAgua.x * 0.5f, FilaDelAgua, LienzoAgua),
                    "Superficie del río: 180×37, se repite a lo ancho. El pivote en la línea del agua (fila 6): por encima, la ceniza.",
                    LienzoAgua),
                new TablaArte.Regla($"{Carpeta}/Alambrada/", PixelesPorUnidadAlambrada, Pivote(LienzoAlambrada.x * 0.5f, 0f, LienzoAlambrada),
                    "Alambrada sumergida: 86×86, el pivote arriba en el centro: cuelga del techo del ojo del puente. A 43 px/u mide 2 × 2 u.",
                    LienzoAlambrada),
                new TablaArte.Regla(RutaCampanario, PixelesPorUnidadCampanario, Pivote(BordeTorre, LienzoCampanario.y, LienzoCampanario),
                    "Campanario: 135×330, el pivote al pie de la torre, en su borde izquierdo (x 42). A 31,5 px/u el suelo del balcón queda " +
                    "a 4,5 u (fila 188), la torre mide 2,92 de ancho y 10,5 de alto, y su arco, 2,92.", LienzoCampanario),
            }
            .Concat(Coberturas.Select(c => new TablaArte.Regla(c.Ruta, c.PixelesPorUnidad, c.Pivote, c.Nota, c.Lienzo)))
            .Concat(Memorias.Append(Losa).Select(m => m.Regla));

        // --- Los fotogramas ---

        public static readonly string[] FotogramasBarril = { "Barril", "Mecha1", "Mecha2", "Quemado" };
        public static readonly string[] FotogramasMina = { "Mina", "Aviso" };
        public static readonly string[] FotogramasAgua = { "Agua1", "Agua2", "Agua3" };
        public static readonly string[] FotogramasAlambradaReposo = { "Alambrada1", "Alambrada2" };
        public static readonly string[] FotogramasAlambradaAtrapa = { "Atrapa1", "Atrapa2" };

        /// <summary>Lo que dura cada fotograma de la superficie del río (el agua lenta, como la de los otros mundos).</summary>
        public const float SegundosAgua = ConstructorMecanicasMundo2.SegundosAgua;

        /// <summary>Los cuatro dibujos del barril (intacto, mecha 1 y 2, quemado), o null si falta alguno.</summary>
        public static Sprite[] DibujosBarril => Cargar(FotogramasBarril, Barril);

        /// <summary>La mina quieta y avisando, o null si falta alguna.</summary>
        public static Sprite[] DibujosMina => Cargar(FotogramasMina, Mina);

        public static Sprite[] SuperficieDelAgua => Cargar(FotogramasAgua, Agua);

        public static Sprite[] AlambradaEnReposo => Cargar(FotogramasAlambradaReposo, Alambrada);

        public static Sprite[] AlambradaQueAtrapa => Cargar(FotogramasAlambradaAtrapa, Alambrada);

        public static Sprite Campanario => AssetDatabase.LoadAssetAtPath<Sprite>(RutaCampanario);

        private static Sprite[] Cargar(IEnumerable<string> nombres, System.Func<string, string> ruta)
        {
            var sprites = nombres.Select(n => AssetDatabase.LoadAssetAtPath<Sprite>(ruta(n))).ToArray();
            return sprites.All(s => s != null) ? sprites : null;
        }

        /// <summary>Todos los PNG que nombran las tablas.</summary>
        public static IEnumerable<string> Rutas =>
            FotogramasBarril.Select(Barril)
                .Concat(FotogramasMina.Select(Mina))
                .Concat(FotogramasAgua.Select(Agua))
                .Concat(FotogramasAlambradaReposo.Concat(FotogramasAlambradaAtrapa).Select(Alambrada))
                .Concat(Coberturas.Select(c => c.Ruta))
                .Append(RutaCampanario)
                .Concat(Memorias.Append(Losa).SelectMany(m => new[] { m.RutaApagada, m.RutaBrillando }));

        [MenuItem("Fragmentum/Niveles/Generar las mecánicas del Mundo 4 (Culpa)", priority = 137)]
        public static void MenuConstruir()
        {
            if (Construir() && GeneradorMecanicas.CrearPrefabs())
            {
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>Fija los ajustes de importación. Sin arte no falla: cada pieza se queda con su rectángulo.</summary>
        public static bool Construir()
        {
            var faltan = 0;
            foreach (var ruta in Rutas)
            {
                if (AssetImporter.GetAtPath(ruta) is TextureImporter importador)
                {
                    if (TablaArte.Ajustar(importador))
                    {
                        importador.SaveAndReimport();
                    }
                }
                else
                {
                    faltan++;
                }
            }

            if (faltan > 0)
            {
                Debug.LogWarning($"[Fragmentum] Faltan {faltan} imágenes de las mecánicas del Mundo 4: esas piezas se quedan con su rectángulo.");
            }
            else
            {
                Debug.Log("[Fragmentum] Mecánicas del Mundo 4: el barril, la mina, el agua del río, la alambrada, las coberturas, el campanario, " +
                          "las memorias y su losa importados.");
            }

            return true;
        }
    }
}
