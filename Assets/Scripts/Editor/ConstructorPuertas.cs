using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.Levels;
using Fragmentum.Narrative;
using UnityEditor;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El arte de lo que abre y cierra cada nivel (PENDIENTES N40, tanda del 2026-10-13): la
    /// <b>puerta de salida</b> (cuatro dibujos: cerrada, entreabierta y dos de abierta), la
    /// <b>verja</b> que cierra la arena detrás de Kael, el <b>checkpoint</b> (apagado y encendido) y
    /// los dos planos de la <b>cinemática de salida</b>, la que se ve al cruzar la puerta. Hay un
    /// juego por mundo, y de momento cuatro: Ira, Tristeza, Miedo y Culpa.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/…/limpios</c> (los limpia <c>limpiar_puertas.py</c> y
    /// los trae <c>Herramientas/traer_arte_puertas.py</c>) y no se retocan. Aquí están su escala y
    /// su pivote (<see cref="Reglas"/>), cómo se viste con ellos cada pieza del nivel
    /// (<see cref="VestirSalida"/>, <see cref="VestirCheckpoint"/>, <see cref="VestirVerja"/>, que
    /// llama <see cref="PreparadorNivel"/>) y el montaje de las dos cinemáticas
    /// (<see cref="Construir"/>). Si falta el arte de un mundo, cada pieza se queda como estaba:
    /// el rectángulo de color y las frases sobre negro.
    /// </summary>
    public static class ConstructorPuertas
    {
        public const string Carpeta = TablaArte.CarpetaMecanicas + "/Puertas";

        /// <summary>Un mundo con su juego de puertas.</summary>
        public sealed class Juego
        {
            public EMOCIONES Emocion;
            public string Clave;

            /// <summary>Tamaño del lienzo de la puerta, su escala y la fila (desde arriba) en que apoya.</summary>
            public Vector2Int LienzoPuerta;
            public float PixelesPorUnidadPuerta;
            public int SueloPuerta;

            public Vector2Int LienzoVerja;
            public float PixelesPorUnidadVerja;
            public int SueloVerja;

            /// <summary>¿La verja se repite hacia arriba hasta el techo? Si no, lleva un muro de hielo encima.</summary>
            public bool VerjaSeRepite;

            public float PixelesPorUnidadCheckpoint;

            /// <summary>De cada estado del checkpoint: el ancho de su lienzo y la columna del centro de su pie.</summary>
            public Vector2[] PieCheckpoint;

            /// <summary>Carpeta y nombres de los dos planos de la cinemática de salida.</summary>
            public string CarpetaPlanos;
            public string[] Planos;

            /// <summary>
            /// Donde se lee el texto en cada plano (arriba en el centro de la frase, en píxeles del
            /// plano): sobre una zona oscura, que la letra es clara.
            /// </summary>
            public Vector2Int[] PuntosTexto;

            public string CarpetaSprites => $"{Carpeta}/{Clave}";

            public float AltoPuerta => SueloPuerta / PixelesPorUnidadPuerta;

            public float AnchoPuerta => LienzoPuerta.x / PixelesPorUnidadPuerta;

            public float AltoVerja => SueloVerja / PixelesPorUnidadVerja;

            public float AnchoVerja => LienzoVerja.x / PixelesPorUnidadVerja;

            public string RutaCinematica => $"{GeneradorNarrativa.Carpeta}/Cinematica_Cierre_{Clave}.asset";
        }

        // Las puertas miden unas 4 u de alto, algo más del doble que Kael (1,8 u), y a una densidad
        // de píxel parecida a la suya (64 px/u). Las verjas, 4 u; los checkpoints, unas 3,3 u.
        public static readonly Juego Ira = new Juego
        {
            Emocion = EMOCIONES.IRA, Clave = "Ira",
            LienzoPuerta = new Vector2Int(225, 240), PixelesPorUnidadPuerta = 60f, SueloPuerta = 240,
            LienzoVerja = new Vector2Int(103, 256), PixelesPorUnidadVerja = 64f, SueloVerja = 256, VerjaSeRepite = true,
            PixelesPorUnidadCheckpoint = 64f,
            PieCheckpoint = new[] { new Vector2(72f, 36f), new Vector2(72f, 26f) },
            CarpetaPlanos = TablaArte.CarpetaCinematicasNivel1, Planos = new[] { "SalidaIra1", "SalidaIra2" },
            // Abajo, en el suelo de la sala; ante la puerta, arriba, en el arco: abajo cruzaba la luz.
            PuntosTexto = new[] { new Vector2Int(240, 236), new Vector2Int(240, 14) },
        };

        public static readonly Juego Tristeza = new Juego
        {
            Emocion = EMOCIONES.TRISTEZA, Clave = "Tristeza",
            LienzoPuerta = new Vector2Int(236, 253), PixelesPorUnidadPuerta = 63f, SueloPuerta = 252,
            LienzoVerja = new Vector2Int(114, 300), PixelesPorUnidadVerja = 75f, SueloVerja = 300, VerjaSeRepite = false,
            PixelesPorUnidadCheckpoint = 56f,
            PieCheckpoint = new[] { new Vector2(59f, 36f), new Vector2(58f, 42f) },
            CarpetaPlanos = ConstructorCinematicasNivel2.Carpeta, Planos = new[] { "SalidaTristeza1", "SalidaTristeza2" },
            // El plano general es casi todo hielo blanco: la frase va sobre la losa oscura que lleva a
            // la puerta. Ante la puerta, abajo, sobre la hierba oscura.
            PuntosTexto = new[] { new Vector2Int(330, 182), new Vector2Int(240, 236) },
        };

        // La del Miedo es un arco tallado en un árbol muerto, con las raíces abiertas: más ancha que
        // alta (4,7 × 4 u). La verja, un muro de raíces sobre una empalizada, se repite hasta el techo
        // como la de la Ira. El checkpoint, una lámpara de trinchera colgada de una estaca.
        public static readonly Juego Miedo = new Juego
        {
            Emocion = EMOCIONES.MIEDO, Clave = "Miedo",
            LienzoPuerta = new Vector2Int(273, 234), PixelesPorUnidadPuerta = 58.5f, SueloPuerta = 234,
            LienzoVerja = new Vector2Int(137, 436), PixelesPorUnidadVerja = 109f, SueloVerja = 436, VerjaSeRepite = true,
            PixelesPorUnidadCheckpoint = 88f,
            PieCheckpoint = new[] { new Vector2(101f, 50f), new Vector2(101f, 50f) },
            CarpetaPlanos = ConstructorCinematicasNivel3.Carpeta, Planos = new[] { "SalidaMiedo1", "SalidaMiedo2" },
            // Abajo en los dos: en el claro, sobre el camino en sombra; ante la puerta, sobre la
            // hierba oscura (más arriba, la luz de la puerta lo ocupa todo).
            PuntosTexto = new[] { new Vector2Int(240, 252), new Vector2Int(240, 255) },
        };

        // La de la Culpa es la de la iglesia quemada del pueblo, cerrada con cadenas de las que cuelgan placas:
        // 4 × 3 u. Al abrirse, las cadenas caen a su pie (en Puerta2 a Puerta4 asoman 11 píxeles bajo el suelo,
        // sobre la tierra). La verja, un muro de cadenas con placas sobre una reja, se repite hasta el techo como
        // la del Miedo, en tramos de 4 u; no casa del todo por arriba y por abajo (arriba lleva la barra de la que
        // cuelgan las cadenas): al repetirse, cada tramo cuelga de la reja del de debajo. El checkpoint, una
        // bandera blanca en un fusil roto, de 3,3 u como los de los otros mundos (se pidió de 2,8).
        public static readonly Juego Culpa = new Juego
        {
            Emocion = EMOCIONES.CULPA, Clave = "Culpa",
            LienzoPuerta = new Vector2Int(127, 181), PixelesPorUnidadPuerta = 42.5f, SueloPuerta = 170,
            LienzoVerja = new Vector2Int(41, 127), PixelesPorUnidadVerja = 31.75f, SueloVerja = 127, VerjaSeRepite = true,
            PixelesPorUnidadCheckpoint = 22f,
            PieCheckpoint = new[] { new Vector2(51f, 21.5f), new Vector2(51f, 21.5f) },
            CarpetaPlanos = ConstructorCinematicasNivel4.Carpeta, Planos = new[] { "SalidaCulpa1", "SalidaCulpa2" },
            // Abajo en los dos: en la plaza, sobre las losas en sombra; ante la puerta, sobre el suelo en
            // penumbra (más arriba, la luz de la puerta lo ocupa todo).
            PuntosTexto = new[] { new Vector2Int(240, 250), new Vector2Int(240, 254) },
        };

        public static readonly Juego[] Juegos = { Ira, Tristeza, Miedo, Culpa };

        /// <summary>La rejilla de los planos de salida: la de los de la Doncella.</summary>
        public static readonly Vector2Int Rejilla = ConstructorCinematicasNivel2.Rejilla;

        /// <summary>Por detrás de Kael y de los enemigos, por delante del fondo y de los bloques.</summary>
        public const int OrdenPuerta = 2;

        public static Juego De(LevelData nivel) =>
            nivel != null && nivel.emocion != null ? Juegos.FirstOrDefault(j => j.Emocion == nivel.emocion.id) : null;

        public static IEnumerable<TablaArte.Regla> Reglas => Juegos.SelectMany(juego => new[]
        {
            new TablaArte.Regla($"{juego.CarpetaSprites}/Puerta/", juego.PixelesPorUnidadPuerta,
                new Vector2(0.5f, 1f - (float)juego.SueloPuerta / juego.LienzoPuerta.y),
                $"Puerta de salida ({juego.Clave}): {juego.LienzoPuerta.x}×{juego.LienzoPuerta.y}, cuatro dibujos en el mismo lienzo, el pivote " +
                $"abajo en el centro. A {juego.PixelesPorUnidadPuerta:0} px/u mide {juego.AnchoPuerta:0.0} × {juego.AltoPuerta:0.0} u.", juego.LienzoPuerta),
            new TablaArte.Regla($"{juego.CarpetaSprites}/Verja/", juego.PixelesPorUnidadVerja,
                new Vector2(0.5f, 1f - (float)juego.SueloVerja / juego.LienzoVerja.y),
                $"Verja de la arena ({juego.Clave}): {juego.LienzoVerja.x}×{juego.LienzoVerja.y}, el pivote abajo en el centro. " +
                $"A {juego.PixelesPorUnidadVerja:0} px/u mide {juego.AnchoVerja:0.0} × {juego.AltoVerja:0.0} u.", juego.LienzoVerja),
            new TablaArte.Regla($"{juego.CarpetaSprites}/Checkpoint/Checkpoint1", juego.PixelesPorUnidadCheckpoint,
                new Vector2(juego.PieCheckpoint[0].y / juego.PieCheckpoint[0].x, 0f),
                $"Checkpoint apagado ({juego.Clave}): el pivote en el centro de su pie, que no está en el centro del lienzo."),
            new TablaArte.Regla($"{juego.CarpetaSprites}/Checkpoint/Checkpoint2", juego.PixelesPorUnidadCheckpoint,
                new Vector2(juego.PieCheckpoint[1].y / juego.PieCheckpoint[1].x, 0f),
                $"Checkpoint encendido ({juego.Clave}): el paño ondea y ensancha el lienzo; el pivote sigue en el pie, para que no salte al encenderse."),
        }).Concat(new[]
        {
            new TablaArte.Regla($"{Ira.CarpetaPlanos}/Salida", UI.CamaraCinematica.PixelesPorUnidad, null,
                "Planos de la salida de la Ira: 480×268, como los de la Doncella (los del jefe del Nivel 1 son de 240×135).", Rejilla),
        });

        public static string RutaSprite(Juego juego, string grupo, string nombre) => $"{juego.CarpetaSprites}/{grupo}/{nombre}.png";

        public static Sprite Cargar(Juego juego, string grupo, string nombre) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite(juego, grupo, nombre));

        public static Sprite[] Puerta(Juego juego) => Enumerable.Range(1, 4).Select(i => Cargar(juego, "Puerta", $"Puerta{i}")).ToArray();

        public static Sprite Verja(Juego juego) => Cargar(juego, "Verja", "Verja");

        public static Sprite[] Checkpoint(Juego juego) => new[] { Cargar(juego, "Checkpoint", "Checkpoint1"), Cargar(juego, "Checkpoint", "Checkpoint2") };

        public static Sprite Plano(Juego juego, int indice) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{juego.CarpetaPlanos}/{juego.Planos[indice]}.png");

        public static bool HayPuerta(Juego juego) => juego != null && Puerta(juego).All(s => s != null);

        public static bool HayVerja(Juego juego) => juego != null && Verja(juego) != null;

        public static bool HayCheckpoint(Juego juego) => juego != null && Checkpoint(juego).All(s => s != null);

        public static bool HayPlanos(Juego juego) => juego != null && Plano(juego, 0) != null && Plano(juego, 1) != null;

        // --- Vestir las piezas del nivel ---

        /// <summary>
        /// Pone la puerta dibujada en la salida y quita su rectángulo de color. Sin arte para ese
        /// mundo no hace nada. Devuelve si la vistió.
        /// </summary>
        public static bool VestirSalida(GameObject salida, LevelData nivel)
        {
            var juego = De(nivel);
            if (!HayPuerta(juego))
            {
                return false;
            }

            var portal = salida.transform.Find("Portal");
            if (portal != null)
            {
                Object.DestroyImmediate(portal.gameObject);
            }

            var dibujos = Puerta(juego);
            var objeto = new GameObject("Puerta");
            objeto.transform.SetParent(salida.transform, false);
            var sprite = objeto.AddComponent<SpriteRenderer>();
            sprite.sprite = dibujos[0];
            sprite.sortingOrder = OrdenPuerta;
            var puerta = objeto.AddComponent<PuertaDeSalida>();
            puerta.Fijar(dibujos[0], dibujos[1], dibujos[2], dibujos[3]);
            Asignar(salida.GetComponent<SalidaNivel>(), "puerta", p => p.objectReferenceValue = puerta);
            return true;
        }

        /// <summary>
        /// Cambia la bandera provisional del checkpoint (un mástil y un paño de color) por su
        /// dibujo, apagado y encendido. Sin arte para ese mundo no hace nada.
        /// </summary>
        public static bool VestirCheckpoint(Checkpoint checkpoint, LevelData nivel)
        {
            var juego = De(nivel);
            if (!HayCheckpoint(juego))
            {
                return false;
            }

            foreach (var provisional in new[] { "Mastil", "Pano" })
            {
                var viejo = checkpoint.transform.Find(provisional);
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
            }

            var dibujos = Checkpoint(juego);
            var objeto = new GameObject("Dibujo");
            objeto.transform.SetParent(checkpoint.transform, false);
            var sprite = objeto.AddComponent<SpriteRenderer>();
            sprite.sprite = dibujos[0];
            sprite.sortingOrder = OrdenPuerta;
            Asignar(checkpoint, "indicador", p => p.objectReferenceValue = sprite);
            Asignar(checkpoint, "spriteInactivo", p => p.objectReferenceValue = dibujos[0]);
            Asignar(checkpoint, "spriteActivo", p => p.objectReferenceValue = dibujos[1]);
            return true;
        }

        /// <summary>
        /// Viste la puerta que cierra la arena (un bloque de <paramref name="desde"/> a
        /// <paramref name="hasta"/> en x y del suelo al techo) con la verja del mundo: repetida hasta
        /// el techo si su dibujo se repite; si no, la verja abajo y un muro de hielo encima. El
        /// bloque de color se apaga; su colisión no cambia.
        /// </summary>
        public static bool VestirVerja(GameObject puerta, LevelData nivel, float desde, float hasta, float suelo, float techo)
        {
            var juego = De(nivel);
            if (!HayVerja(juego))
            {
                return false;
            }

            var liso = puerta.GetComponent<SpriteRenderer>();
            if (liso != null)
            {
                liso.enabled = false;
            }

            var centro = (desde + hasta) * 0.5f;
            var alto = techo - suelo;
            var objeto = new GameObject("Verja");
            objeto.transform.SetParent(puerta.transform, true);
            objeto.transform.position = new Vector3(centro, suelo, 0f);
            objeto.transform.localScale = new Vector3(1f / puerta.transform.lossyScale.x, 1f / puerta.transform.lossyScale.y, 1f);
            var sprite = objeto.AddComponent<SpriteRenderer>();
            sprite.sprite = Verja(juego);
            sprite.sortingOrder = OrdenPuerta;
            if (juego.VerjaSeRepite)
            {
                sprite.drawMode = SpriteDrawMode.Tiled;
                sprite.tileMode = SpriteTileMode.Continuous;
                sprite.size = new Vector2(juego.AnchoVerja, alto);
            }
            else if (ConstructorEscenarioMundo2.Completo && alto > juego.AltoVerja)
            {
                var muro = ConstructorEscenarioMundo2.VestirBloque(puerta.transform, "MuroSobreLaVerja", desde, hasta, suelo + juego.AltoVerja, techo, true);
                muro.transform.localScale = objeto.transform.localScale;
                muro.transform.position = new Vector3(desde, techo, 0f);
            }

            return true;
        }

        // --- Las cinemáticas de salida ---

        [MenuItem("Fragmentum/Cinemáticas/Montar las cinemáticas de salida", priority = 163)]
        public static void MenuConstruir() => Construir();

        /// <summary>
        /// Monta la cinemática de salida de cada mundo que tenga sus dos planos: el plano general
        /// con las dos primeras frases del cierre del guion y Kael ante la puerta con la tercera.
        /// Devuelve false solo si algo falló (que falten los planos no es un fallo: avisa).
        /// </summary>
        public static bool Construir()
        {
            var ok = true;
            foreach (var juego in Juegos)
            {
                if (!File.Exists($"{juego.CarpetaPlanos}/{juego.Planos[0]}.png") || !File.Exists($"{juego.CarpetaPlanos}/{juego.Planos[1]}.png"))
                {
                    Debug.LogWarning($"[Fragmentum] Faltan los planos de salida de {juego.Clave} en {juego.CarpetaPlanos}: su cierre sigue siendo de frases sobre negro.");
                    continue;
                }

                if (!HayPlanos(juego))
                {
                    Debug.LogError($"[Fragmentum] Los planos de salida de {juego.Clave} no están importados como sprites.");
                    ok = false;
                    continue;
                }

                var cinematica = AssetDatabase.LoadAssetAtPath<CutsceneData>(juego.RutaCinematica);
                var guion = GuionMundos.De(juego.Emocion);
                if (cinematica == null || guion == null || guion.CinematicaCierre == null || guion.CinematicaCierre.Length < 3)
                {
                    Debug.LogWarning($"[Fragmentum] Falta {juego.RutaCinematica} (la genera GeneradorNarrativa) o su guion: la salida de {juego.Clave} no se monta.");
                    continue;
                }

                Salida(cinematica, juego, guion.CinematicaCierre);
                EditorUtility.SetDirty(cinematica);
            }

            AssetDatabase.SaveAssets();
            return ok;
        }

        /// <summary>
        /// Escribe una cinemática de salida: negro 0,3 · el plano general 0,7 + 6,4 + 0,4, con las
        /// dos primeras frases (2,6 s cada una, o lo que pida su largo: <see cref="ParaLeer"/>) ·
        /// negro 0,2 · Kael ante la puerta 0,6 + 4,6 + 0,8, con la tercera (3,6 s). 14 s; se puede
        /// saltar.
        /// </summary>
        public static void Salida(CutsceneData cinematica, Juego juego, string[] frases)
        {
            var primera = ParaLeer(frases[0]);
            var segunda = ParaLeer(frases[1]);
            var inicioSegunda = Decimas(0.8f + primera + 0.3f);
            var general = new Diapositiva
            {
                imagen = Plano(juego, 0),
                fundidoEntrada = 0.7f,
                duracion = Mathf.Max(6.4f, Decimas(inicioSegunda + segunda)),
                fundidoSalida = 0.4f,
                carteles = { Dicho(frases[0], 0.8f, primera, juego.PuntosTexto[0]), Dicho(frases[1], inicioSegunda, segunda, juego.PuntosTexto[0]) },
            };
            var anteLaPuerta = new Diapositiva
            {
                imagen = Plano(juego, 1),
                fundidoEntrada = 0.6f,
                duracion = 4.6f,
                fundidoSalida = 0.8f,
                carteles = { Dicho(frases[2], 0.6f, 3.6f, juego.PuntosTexto[1]) },
            };

            cinematica.id = $"Cierre_{juego.Clave}";
            cinematica.montaje = MONTAJES_CINEMATICA.BEATS;
            cinematica.escalaEntera = true;
            cinematica.rejilla = Rejilla;
            cinematica.sobreElJuego = false;
            cinematica.saltable = true;
            cinematica.soloPrimeraVez = false;
            cinematica.bloqueoSalto = 0.5f;
            cinematica.fundidoTexto = 0.4f;
            cinematica.tamanoFuente = ConstructorCinematicasNivel2.TamanoFuente;
            cinematica.musica = MusicaNiveles.DeCinematica(cinematica.id);
            cinematica.diapositivas = new List<Diapositiva>
            {
                new Diapositiva { duracion = 0.3f },
                general,
                new Diapositiva { duracion = 0.2f },
                anteLaPuerta,
            };
        }

        /// <summary>
        /// Lo que dura una frase del plano general: 2,6 s, o más si es larga, para no pasar de 30
        /// letras por segundo (la del Miedo, «El bosque no era un laberinto…», tiene 80: 2,7 s).
        /// </summary>
        public static float ParaLeer(string frase) => Mathf.Max(2.6f, Mathf.Ceil((frase ?? string.Empty).Length / 30f * 10f) / 10f);

        // Redondeado a la décima: 0,8 + 2,6 + 0,3 da los 3,7 de siempre y no 3,7000003.
        private static float Decimas(float segundos) => Mathf.Round(segundos * 10f) / 10f;

        private static Cartel Dicho(string texto, float inicio, float duracion, Vector2Int punto) => new Cartel
        {
            texto = texto,
            inicio = inicio,
            duracion = duracion,
            enPunto = true,
            punto = punto,
            alineacion = TextAnchor.UpperCenter,
        };
    }
}
