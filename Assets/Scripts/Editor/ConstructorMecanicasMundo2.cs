using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;
using Object = UnityEngine.Object;
using Transicion = Fragmentum.EditorTools.ControladoresAnimator.Transicion;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Las mecánicas y los efectos del Mundo 2 (Tristeza) con su arte:
    /// - <b>el tapón de hielo rompible</b>: intacto, agrietado y roto (salta en esquirlas y quedan
    ///   sus restos en el borde del agujero). Sigue el estado de <see cref="HieloRompible"/>;
    /// - <b>la placa de hielo flotante</b>: un fotograma;
    /// - <b>la columna de burbujas</b> que señala una bolsa de aire, y <b>la superficie del agua</b>,
    ///   que se repite a lo ancho: dos bucles (<see cref="BucleDeSprites"/>);
    /// - <b>la ola</b> de la Doncella: un prefab que sigue las fases de su zona de daño;
    /// - <b>el arte de las franjas</b> que inunda y congela (<see cref="ArteDeFranja"/>): el aviso,
    ///   las púas y la capa de hielo;
    /// - <b>la burbuja de aire</b> del HUD (<see cref="ArteInterfaz"/>).
    ///
    /// Cada grupo llegó en su rejilla, así que cada uno lleva su escala (<see cref="Reglas"/>). Los
    /// PNG vienen de <c>Tesis/Imagenes/limpios/Mundo2</c>, que tiene su LEEME, y no se retocan.
    /// Los prefabs de la placa y del tapón los monta <see cref="GeneradorMecanicas"/>.
    ///
    /// Si falta el arte de una pieza, avisa y esa se queda con su rectángulo. Idempotente.
    /// </summary>
    public static class ConstructorMecanicasMundo2
    {
        public const string CarpetaMecanicas = TablaArte.CarpetaMecanicas + "/Mundo2";
        public const string CarpetaEfectos = TablaArte.CarpetaEfectos + "/Mundo2";
        public const string CarpetaInterfaz = TablaArte.CarpetaSprites + "/Interfaz";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Mecanicas/Mundo2";
        public const string CarpetaDatos = "Assets/Data/Mecanicas";
        public const string RutaArteFranja = CarpetaDatos + "/ArteDeFranja.asset";
        public const string RutaArteInterfaz = "Assets/Resources/" + ArteInterfaz.Recurso + ".asset";
        public const string RutaPrefabBurbujas = GeneradorMecanicas.Carpeta + "/ColumnaDeBurbujas.prefab";
        public const string RutaPrefabOla = ConstructorEfectos.CarpetaPrefabs + "/OlaDeAgua.prefab";

        public static string Hielo(string archivo) => $"{CarpetaMecanicas}/Hielo/{archivo}.png";

        public static string Placa(string archivo) => $"{CarpetaMecanicas}/Placa/{archivo}.png";

        public static string Burbujas(string archivo) => $"{CarpetaMecanicas}/Burbujas/{archivo}.png";

        public static string Agua(string archivo) => $"{CarpetaMecanicas}/Agua/{archivo}.png";

        public static string Ola(string archivo) => $"{CarpetaEfectos}/Ola/{archivo}.png";

        public static string Puas(string archivo) => $"{CarpetaEfectos}/Puas/{archivo}.png";

        public static string Capa(string archivo) => $"{CarpetaEfectos}/Capa/{archivo}.png";

        public static string Inundacion(string archivo) => $"{CarpetaEfectos}/Inundacion/{archivo}.png";

        public static string Oxigeno(string archivo) => $"{CarpetaInterfaz}/Oxigeno/{archivo}.png";

        // --- Escalas y pivotes ---

        /// <summary>El tapón mide 60 × 65 píxeles: a 30 por unidad, las 2 u del agujero, y asoma 1,25 u.</summary>
        public const float PixelesPorUnidadHielo = 30f;

        /// <summary>La placa mide 166 píxeles: a 66 por unidad, las 2,5 u del recorrido (decidido el 2026-10-10).</summary>
        public const float PixelesPorUnidadPlaca = 66f;

        /// <summary>El agua y las burbujas, con la regla del Nivel 1 (491,5 entre su bloque de 11,47 px).</summary>
        public const float PixelesPorUnidadAgua = 43f;

        /// <summary>La ola, a la escala de la onda de la Centinela, de la que se editó: 2,4 u de largo.</summary>
        public const float PixelesPorUnidadOla = ConstructorEfectos.PixelesPorUnidadOnda;

        /// <summary>Las púas, con la regla del Nivel 1: 2,4 u de ancho y 2,8 de alto, lo que mide su zona de daño.</summary>
        public const float PixelesPorUnidadPuas = 43f;

        /// <summary>El aviso de las púas llegó en otra rejilla (10,24 px): con la regla, 48.</summary>
        public const float PixelesPorUnidadEscarcha = 48f;

        /// <summary>La losa mide 34 píxeles de grueso: a 68 por unidad, la media unidad de la capa que se pisa.</summary>
        public const float PixelesPorUnidadCapa = 68f;

        /// <summary>El aviso de la inundación, con la regla: sus burbujas suben 3,4 u, hasta donde llegará el agua.</summary>
        public const float PixelesPorUnidadAviso = 48f;

        private static readonly Vector2Int LienzoHielo = new Vector2Int(95, 79);
        private static readonly Vector2Int LienzoPlaca = new Vector2Int(168, 84);
        private static readonly Vector2Int LienzoBurbujas = new Vector2Int(35, 118);
        private static readonly Vector2Int LienzoAgua = new Vector2Int(180, 40);
        private static readonly Vector2Int LienzoOla = new Vector2Int(237, 150);
        private static readonly Vector2Int LienzoPuas = new Vector2Int(161, 122);
        private static readonly Vector2Int LienzoEscarcha = new Vector2Int(140, 117);
        private static readonly Vector2Int LienzoCapa = new Vector2Int(186, 70);
        private static readonly Vector2Int LienzoAviso = new Vector2Int(185, 193);
        private static readonly Vector2Int LienzoOxigeno = new Vector2Int(81, 77);

        /// <summary>La fila de la línea del agua en su superficie (por encima, la espuma).</summary>
        private const int FilaDelAgua = 9;

        // Píxeles desde arriba a la izquierda → pivote de Unity.
        private static Vector2 Pivote(float x, float y, Vector2Int lienzo) => new Vector2(x / lienzo.x, (lienzo.y - y) / lienzo.y);

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla($"{CarpetaMecanicas}/Hielo/", PixelesPorUnidadHielo, Pivote(47f, 9f, LienzoHielo),
                "Tapón de hielo: 95×79, el pivote arriba en el centro (la cúpula empieza en la fila 9), como su prefab. " +
                "A 30 px/u mide 2 u de ancho y asoma 1,25 u.", LienzoHielo),
            new TablaArte.Regla($"{CarpetaMecanicas}/Placa/", PixelesPorUnidadPlaca, Pivote(84f, 1f, LienzoPlaca),
                "Placa de hielo flotante: 168×84, el pivote en la superficie (fila 1). A 66 px/u mide 2,5 u.", LienzoPlaca),
            new TablaArte.Regla($"{CarpetaMecanicas}/Burbujas/", PixelesPorUnidadAgua, new Vector2(0.5f, 1f),
                "Columna de burbujas: 35×118, el pivote arriba: acaba en la superficie de la bolsa de aire. 0,8 × 2,7 u.", LienzoBurbujas),
            new TablaArte.Regla($"{CarpetaMecanicas}/Agua/", PixelesPorUnidadAgua, Pivote(90f, FilaDelAgua, LienzoAgua),
                "Superficie del agua: 180×40, se repite a lo ancho. El pivote en la línea del agua (fila 9): por encima, la espuma.", LienzoAgua),
            new TablaArte.Regla($"{CarpetaEfectos}/Ola/", PixelesPorUnidadOla, Pivote(230f, 150f, LienzoOla),
                "Ola de la Doncella: 237×150, el pivote en su frente sobre el suelo. A 98 px/u, como la onda de la Centinela: 2,4 u de largo.",
                LienzoOla),
            new TablaArte.Regla($"{CarpetaEfectos}/Puas/", PixelesPorUnidadPuas, Pivote(78f, 122f, LienzoPuas),
                "Púas de hielo: 161×122, el pivote en el centro de su base. 2,4 × 2,8 u.", LienzoPuas),
            new TablaArte.Regla(Puas("P1"), PixelesPorUnidadEscarcha, Pivote(70f, 114f, LienzoEscarcha),
                "Púas, el aviso (la escarcha): en otro lienzo (140×117) y otra rejilla. El pivote en el centro de la escarcha.", LienzoEscarcha),
            new TablaArte.Regla($"{CarpetaEfectos}/Capa/", PixelesPorUnidadCapa, Pivote(93f, 2f, LienzoCapa),
                "Capa de hielo: 186×70, el pivote arriba en el centro, en lo que se pisa (fila 2). A 68 px/u la losa mide 0,5 u de grueso.",
                LienzoCapa),
            new TablaArte.Regla($"{CarpetaEfectos}/Inundacion/", PixelesPorUnidadAviso, Pivote(92f, 165f, LienzoAviso),
                "Aviso de la inundación: 185×193, el pivote en el centro de la grieta (fila 165), sobre el suelo.", LienzoAviso),
            new TablaArte.Regla($"{CarpetaInterfaz}/Oxigeno/", 100f, null,
                "Burbuja de aire del HUD: 81×77, a su tamaño (un píxel suyo, un píxel de la interfaz).", LienzoOxigeno),
        };

        // --- El tapón de hielo ---

        public const string Intacto = "Intacto";
        public const string Agrietado = "Agrietado";
        public const string Roto = "Roto";

        /// <summary>
        /// Un clip por valor de <see cref="ESTADOS_HIELO"/>, en su orden. Al romperse salta en
        /// esquirlas un instante y quedan los restos, en el borde del agujero.
        /// </summary>
        public static readonly ConstructorMecanicas.Mecanica Tapon = new ConstructorMecanicas.Mecanica
        {
            Nombre = "Hielo",
            Estados = typeof(ESTADOS_HIELO),
            Clips = new[]
            {
                new Clip { Nombre = "Hielo_Intacto", Estado = Intacto, Fotogramas = new[] { "Hielo1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
                new Clip { Nombre = "Hielo_Agrietado", Estado = Agrietado, Fotogramas = new[] { "Hielo2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
                new Clip { Nombre = "Hielo_Roto", Estado = Roto, Fotogramas = new[] { "Esquirlas1", "Esquirlas2", "Hielo3" }, Duraciones = new[] { 0.08f, 0.10f, ClipsDeSprites.SeQueda } },
            },
        };

        /// <summary>El tapón tal como se ve antes de jugar, o null si no hay arte.</summary>
        public static Sprite TaponIntacto => AssetDatabase.LoadAssetAtPath<Sprite>(Hielo(Tapon.Clips[0].Fotogramas[0]));

        /// <summary>La placa, o null si no hay arte.</summary>
        public static Sprite PlacaFlotante => AssetDatabase.LoadAssetAtPath<Sprite>(Placa("Placa1"));

        // --- Los bucles ---

        public static readonly string[] FotogramasBurbujas = { "Burbujas1", "Burbujas2", "Burbujas3", "Burbujas4" };
        public static readonly string[] FotogramasAgua = { "Agua1", "Agua2", "Agua3" };

        public const float SegundosBurbujas = 0.18f;
        public const float SegundosAgua = 0.25f;

        /// <summary>Las burbujas, detrás del agua (20) y delante del fondo.</summary>
        public const int OrdenBurbujas = 3;

        /// <summary>La última fila del lienzo de la superficie viene vacía en los tres fotogramas.</summary>
        public const int FilasVaciasBajoElAgua = 1;

        /// <summary>
        /// Lo que mide la superficie del agua por debajo de su línea: ahí sigue el cuerpo liso. Sin
        /// descontar la fila vacía quedaba una raya de fondo sin teñir entre la banda y el cuerpo.
        /// </summary>
        public static float FondoDeLaSuperficie => (LienzoAgua.y - FilaDelAgua - FilasVaciasBajoElAgua) / PixelesPorUnidadAgua;

        /// <summary>El color de la última fila de la superficie: el del cuerpo del agua, translúcido.</summary>
        public static readonly Color ColorAgua = new Color32(28, 44, 68, 128);

        /// <summary>Los fotogramas de la superficie del agua, o null si falta alguno.</summary>
        public static Sprite[] SuperficieDelAgua => Cargar(FotogramasAgua, Agua);

        public static GameObject PrefabBurbujas => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabBurbujas);

        // --- La ola ---

        public const string OlaAviso = "Aviso";
        public const string OlaAvanza = "Avanza";
        public const string OlaRompe = "Rompe";

        /// <summary>
        /// Mientras su zona avisa, la ola se alza donde nace (Ola1); al correr, sube, se curva y
        /// rompe en bucle; y cuando la zona acaba, contra la pared, rompe una vez y desaparece.
        /// </summary>
        public static readonly Clip[] ClipsOla =
        {
            new Clip { Nombre = "Ola_Aviso", Estado = OlaAviso, Fotogramas = new[] { "Ola1" }, Duraciones = new[] { 0.1f }, Bucle = true },
            new Clip { Nombre = "Ola_Avanza", Estado = OlaAvanza, Fotogramas = new[] { "Ola1", "Ola2", "Ola3" }, Duraciones = new[] { 0.1f, 0.1f, 0.1f }, Bucle = true },
            new Clip { Nombre = "Ola_Rompe", Estado = OlaRompe, Fotogramas = new[] { "Ola3" }, Duraciones = new[] { 0.2f } },
        };

        public static string RutaControladorOla => $"{CarpetaAnimaciones}/Ola.controller";

        public static GameObject PrefabOla => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabOla);

        // --- Las franjas ---

        public static readonly string[] FotogramasAviso = { "Aviso1", "Aviso2" };
        public static readonly string[] FotogramasPuasQuietas = { "P3", "P4" };

        /// <summary>El grupo de púas mide 105 píxeles de ancho.</summary>
        public static float AnchoPuas => 105f / PixelesPorUnidadPuas;

        /// <summary>La losa mide 181 píxeles de punta a punta.</summary>
        public static float AnchoLosa => 181f / PixelesPorUnidadCapa;

        public static ArteDeFranja ArteFranja => AssetDatabase.LoadAssetAtPath<ArteDeFranja>(RutaArteFranja);

        // --- El HUD ---

        public static readonly string[] FotogramasOxigeno = { "Oxigeno1", "Oxigeno2", "Oxigeno3", "Oxigeno4", "Oxigeno5" };

        // --- Construcción ---

        /// <summary>
        /// Importa lo que haya y construye sus clips, controladores, prefabs y assets de arte.
        /// Devuelve false solo si algo falló; lo que no tiene arte todavía, avisa y se salta.
        /// </summary>
        public static bool Construir()
        {
            AsegurarCarpeta(CarpetaAnimaciones);
            AsegurarCarpeta(CarpetaDatos);

            var ok = ConstruirTapon();
            ok &= Importar(new[] { "Placa1" }, Placa, "la placa de hielo flotante") != false;
            ok &= ConstruirBurbujas();
            ok &= Importar(FotogramasAgua, Agua, "la superficie del agua") != false;
            ok &= ConstruirOla();
            ok &= ConstruirArteDeFranja();
            ok &= ConstruirArteDeInterfaz();
            AssetDatabase.SaveAssets();
            return ok;
        }

        private static bool ConstruirTapon()
        {
            if (!ClipsDeSprites.CargarSprites(Tapon.Clips, Hielo, "el tapón de hielo", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            var clips = Tapon.Clips.Select(c => (c.Estado, (Motion)ClipsDeSprites.Crear(CarpetaAnimaciones, c, sprites))).ToList();
            ConstructorMecanicas.ArmarControlador(Tapon, clips);
            Debug.Log($"[Fragmentum] Tapón de hielo: {sprites.Count} fotogramas, {Tapon.Clips.Length} clips y {Tapon.RutaControlador}.");
            return true;
        }

        private static bool ConstruirBurbujas()
        {
            switch (Importar(FotogramasBurbujas, Burbujas, "las burbujas"))
            {
                case false:
                    return false;
                case null:
                    return true;
            }

            var sprites = Cargar(FotogramasBurbujas, Burbujas);
            var raiz = new GameObject("ColumnaDeBurbujas");
            try
            {
                var dibujo = raiz.AddComponent<SpriteRenderer>();
                dibujo.sprite = sprites[0];
                dibujo.sortingOrder = OrdenBurbujas;
                var bucle = raiz.AddComponent<BucleDeSprites>();
                Asignar(bucle, "fotogramas", p => Llenar(p, sprites));
                Asignar(bucle, "segundos", p => p.floatValue = SegundosBurbujas);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBurbujas, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {RutaPrefabBurbujas}.");
                }

                return guardado;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        /// <summary>
        /// Aviso en bucle hasta que la zona hace daño (<c>Quema</c>); entonces avanza en bucle.
        /// Cuando la zona acaba (<c>Apagada</c>), desde donde esté, rompe.
        /// </summary>
        private static bool ConstruirOla()
        {
            if (!ClipsDeSprites.CargarSprites(ClipsOla, Ola, "la ola de la Doncella", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            AsegurarCarpeta(ConstructorEfectos.CarpetaPrefabs);
            var clips = ClipsOla.Select(c => (c.Estado, (Motion)ClipsDeSprites.Crear(CarpetaAnimaciones, c, sprites))).ToList();
            var controlador = ControladoresAnimator.CargarOCrear(RutaControladorOla);
            ControladoresAnimator.AsegurarParametros(controlador,
                (AnimatorParams.Quema.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Apagada.Nombre, AnimatorControllerParameterType.Bool, 0f));
            var e = ControladoresAnimator.Estados(controlador.layers[0].stateMachine, clips);

            var apagada = ControladoresAnimator.Condicion(AnimatorConditionMode.If, AnimatorParams.Apagada.Nombre);
            var quema = ControladoresAnimator.Condicion(AnimatorConditionMode.If, AnimatorParams.Quema.Nombre);
            ControladoresAnimator.Transiciones(e[OlaAviso],
                new Transicion("Apagada", e[OlaRompe], null, apagada),
                new Transicion("Corre", e[OlaAvanza], null, quema));
            ControladoresAnimator.Transiciones(e[OlaAvanza], new Transicion("Apagada", e[OlaRompe], null, apagada));
            ControladoresAnimator.Transiciones(e[OlaRompe]);
            EditorUtility.SetDirty(controlador);
            AssetDatabase.SaveAssets();

            var raiz = new GameObject("OlaDeAgua");
            try
            {
                var dibujo = raiz.AddComponent<SpriteRenderer>();
                dibujo.sprite = sprites[ClipsOla[0].Fotogramas[0]];
                dibujo.sortingOrder = ConstructorEfectos.OrdenDibujo;
                var animator = raiz.AddComponent<Animator>();
                animator.runtimeAnimatorController = controlador;
                animator.applyRootMotion = false;
                raiz.AddComponent<DibujoDeZona>().duracionApagado = ClipsOla[2].Duracion;
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabOla, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {RutaPrefabOla}.");
                    return false;
                }
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }

            Debug.Log($"[Fragmentum] Efecto OlaDeAgua: sus clips, su controlador y el prefab {RutaPrefabOla}.");
            return true;
        }

        private static bool ConstruirArteDeFranja()
        {
            var listos = new[]
            {
                Importar(FotogramasAviso, Inundacion, "el aviso de la inundación"),
                Importar(FotogramasAgua, Agua, "la superficie del agua"),
                Importar(new[] { "P1", "P2", "P3", "P4", "P5" }, Puas, "las púas de hielo"),
                Importar(new[] { "Capa1", "Capa2", "Capa3" }, Capa, "la capa de hielo"),
            };
            if (listos.Any(l => l == false))
            {
                return false;
            }

            if (listos.Any(l => l == null))
            {
                return true; // falta arte: las franjas siguen con sus rectángulos
            }

            var arte = ArteFranja;
            if (arte == null)
            {
                arte = ScriptableObject.CreateInstance<ArteDeFranja>();
                AssetDatabase.CreateAsset(arte, RutaArteFranja);
            }

            arte.aviso = Cargar(FotogramasAviso, Inundacion);
            arte.superficie = Cargar(FotogramasAgua, Agua);
            arte.segundosSuperficie = SegundosAgua;
            arte.fondoSuperficie = FondoDeLaSuperficie;
            arte.colorAgua = ColorAgua;
            arte.puasAviso = AssetDatabase.LoadAssetAtPath<Sprite>(Puas("P1"));
            arte.puasBrota = AssetDatabase.LoadAssetAtPath<Sprite>(Puas("P2"));
            arte.puasQuietas = Cargar(FotogramasPuasQuietas, Puas);
            arte.puasRompe = AssetDatabase.LoadAssetAtPath<Sprite>(Puas("P5"));
            arte.anchoPuas = AnchoPuas;
            arte.capaSeForma = AssetDatabase.LoadAssetAtPath<Sprite>(Capa("Capa1"));
            arte.capaEntera = AssetDatabase.LoadAssetAtPath<Sprite>(Capa("Capa2"));
            arte.capaSeDerrite = AssetDatabase.LoadAssetAtPath<Sprite>(Capa("Capa3"));
            arte.anchoLosa = AnchoLosa;
            EditorUtility.SetDirty(arte);
            Debug.Log($"[Fragmentum] Arte de las franjas de la Doncella: {RutaArteFranja}.");
            return arte.Completo;
        }

        private static bool ConstruirArteDeInterfaz()
        {
            switch (Importar(FotogramasOxigeno, Oxigeno, "la burbuja de aire del HUD"))
            {
                case false:
                    return false;
                case null:
                    return true;
            }

            AsegurarCarpeta(System.IO.Path.GetDirectoryName(RutaArteInterfaz)?.Replace('\\', '/'));
            var arte = AssetDatabase.LoadAssetAtPath<ArteInterfaz>(RutaArteInterfaz);
            if (arte == null)
            {
                arte = ScriptableObject.CreateInstance<ArteInterfaz>();
                AssetDatabase.CreateAsset(arte, RutaArteInterfaz);
            }

            arte.oxigeno = Cargar(FotogramasOxigeno, Oxigeno);
            EditorUtility.SetDirty(arte);
            return arte.TieneOxigeno;
        }

        // --- Utilidades ---

        /// <summary>
        /// Fija los ajustes de importación de esos archivos. Devuelve true si están todos, null si
        /// falta alguno (avisa: el arte puede no haber llegado) y false si uno no es un sprite.
        /// </summary>
        private static bool? Importar(IEnumerable<string> archivos, System.Func<string, string> ruta, string quien)
        {
            var clip = new Clip { Fotogramas = archivos.ToArray() };
            if (!ClipsDeSprites.CargarSprites(new[] { clip }, ruta, quien, out var sprites))
            {
                return false;
            }

            return sprites != null ? true : (bool?)null;
        }

        private static Sprite[] Cargar(IEnumerable<string> archivos, System.Func<string, string> ruta)
        {
            var sprites = archivos.Select(a => AssetDatabase.LoadAssetAtPath<Sprite>(ruta(a))).ToArray();
            return sprites.All(s => s != null) ? sprites : null;
        }

        private static void Llenar(SerializedProperty lista, IReadOnlyList<Sprite> sprites)
        {
            lista.arraySize = sprites.Count;
            for (var i = 0; i < sprites.Count; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }
    }
}
