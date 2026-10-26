using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Levels.Mecanicas;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;
using Transicion = Fragmentum.EditorTools.ControladoresAnimator.Transicion;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Las mecánicas del Mundo 3 (Miedo) con su arte:
    /// - <b>la antorcha</b>: apagada, encendida (su llama, en bucle) y consumida. Sigue el estado de
    ///   <see cref="Antorcha"/> (el entero «Estado»);
    /// - <b>el cepo</b>: abierto y cerrado (<see cref="Cepo"/> cambia el dibujo);
    /// - <b>las ramas</b>: la sana, que se repite por el centro para alargarla (las ramas del
    ///   recorrido y de la arena), y la podrida, que a oscuras es la sana, bajo una antorcha se ve
    ///   podrida y al partirse se rompe y se deshace en astillas (<see cref="RamaPodrida"/>);
    /// - <b>la cinta blanca</b> que marca el camino bueno: dos fotogramas al viento;
    /// - <b>la ciénaga</b> (2026-10-19): la superficie de su agua, que se repite a lo ancho y en bucle
    ///   (como la del Mundo 2, pedida editándola: el mismo píxel), y <b>la maraña</b> de raíces, que se
    ///   mece y se cierra cuando sujeta a Kael (<see cref="Fragmentum.Levels.Mecanicas.Marana"/>).
    ///
    /// Llegaron todas en la rejilla del Golem (100 píxeles de arte a lo ancho de 2048), dibujadas
    /// más grandes de lo pedido. Cada una va a la escala que le da su tamaño en el juego (como la
    /// placa del Mundo 2, decidido el 2026-10-10): el pivote, en el pie o en la superficie.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo3</c> y no se retocan. Los prefabs de la
    /// antorcha, el cepo y la rama podrida los monta <see cref="GeneradorMecanicas"/>; las ramas
    /// sanas y las cintas, <c>PreparadorNivelMiedo</c>. Si falta el arte de una pieza, avisa y
    /// esa se queda con su rectángulo. Idempotente.
    /// </summary>
    public static class ConstructorMecanicasMundo3
    {
        public const string CarpetaMecanicas = TablaArte.CarpetaMecanicas + "/Mundo3";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Mecanicas/Mundo3";

        public static string Antorcha(string archivo) => $"{CarpetaMecanicas}/Antorcha/{archivo}.png";

        public static string Cepo(string archivo) => $"{CarpetaMecanicas}/Cepo/{archivo}.png";

        public static string Rama(string archivo) => $"{CarpetaMecanicas}/Rama/{archivo}.png";

        public static string Cinta(string archivo) => $"{CarpetaMecanicas}/Cinta/{archivo}.png";

        public static string Agua(string archivo) => $"{CarpetaMecanicas}/Agua/{archivo}.png";

        public static string Marana(string archivo) => $"{CarpetaMecanicas}/Marana/{archivo}.png";

        // --- Escalas y pivotes ---

        private static readonly Vector2Int LienzoAntorcha = new Vector2Int(43, 86);
        private static readonly Vector2Int LienzoCepo = new Vector2Int(79, 55);
        private static readonly Vector2Int LienzoRama = new Vector2Int(91, 70);
        private static readonly Vector2Int LienzoCinta = new Vector2Int(58, 57);
        private static readonly Vector2Int LienzoAgua = new Vector2Int(179, 34);
        private static readonly Vector2Int LienzoMarana = new Vector2Int(77, 76);

        /// <summary>
        /// La antorcha mide 68 píxeles del pie a lo alto de los trapos y su cabeza está a 58 del
        /// pie: a 40 por unidad, 1,7 u de alto y la cabeza a 1,45, donde su luz
        /// (<see cref="GeneradorMecanicas.AlturaLlama"/>). La llama sube por encima.
        /// </summary>
        public const float PixelesPorUnidadAntorcha = 40f;

        /// <summary>El cepo mide 76 píxeles: a 80 por unidad, 0,95 u, lo que su zona (0,8) y algo de hojas.</summary>
        public const float PixelesPorUnidadCepo = 80f;

        /// <summary>La rama mide 85 píxeles: a 42,5 por unidad, las 2 u de la rama podrida; de grueso, 0,54 u.</summary>
        public const float PixelesPorUnidadRama = 42.5f;

        /// <summary>La cinta cuelga 54 píxeles: a 60 por unidad, 0,9 u, lo que la provisional.</summary>
        public const float PixelesPorUnidadCinta = 60f;

        /// <summary>
        /// El agua de la ciénaga, a la escala de la del Mundo 2: llegó en su misma rejilla (240 píxeles
        /// de arte a lo ancho del mismo lienzo), así que su píxel es el mismo.
        /// </summary>
        public const float PixelesPorUnidadAgua = ConstructorMecanicasMundo2.PixelesPorUnidadAgua;

        /// <summary>La fila de la línea del agua en su superficie: por encima, las lentejas de agua.</summary>
        public const int FilaDelAgua = 9;

        /// <summary>Las filas vacías bajo el dibujo del agua: ahí sigue el cuerpo liso.</summary>
        public const int FilasVaciasBajoElAgua = 1;

        /// <summary>Lo que mide la superficie del agua por debajo de su línea: ahí sigue el cuerpo liso.</summary>
        public static float FondoDeLaSuperficie => (LienzoAgua.y - FilaDelAgua - FilasVaciasBajoElAgua) / PixelesPorUnidadAgua;

        /// <summary>El color de la última fila de la superficie: el del cuerpo del agua, translúcido.</summary>
        public static readonly Color ColorAgua = new Color32(16, 25, 19, 128);

        /// <summary>
        /// La maraña mide 75 píxeles de arte de ancho: a 37,5 por unidad, los 2 u de su zona (y 2 de
        /// alto: cuelga del techo del túnel, con las puntas de las raíces abajo). Su píxel es un poco más
        /// fino que el de la rama (42,5).
        /// </summary>
        public const float PixelesPorUnidadMarana = 37.5f;

        /// <summary>La fila de la superficie de la rama (la de su musgo): el pivote.</summary>
        public const int FilaSuperficieRama = 22;

        /// <summary>Lo que no se repite de la rama, a cada lado: sus puntas rotas. Su centro (x 22-36) casa consigo mismo.</summary>
        public const int BordeIzquierdoRama = 22;

        public const int BordeDerechoRama = 54;

        /// <summary>Las columnas vacías del lienzo de la rama a cada lado del dibujo.</summary>
        public const int MargenRama = 3;

        // Píxeles desde arriba a la izquierda → pivote de Unity.
        private static Vector2 Pivote(float x, float y, Vector2Int lienzo) => new Vector2(x / lienzo.x, (lienzo.y - y) / lienzo.y);

        public static Vector2 PivoteRama => Pivote(LienzoRama.x * 0.5f, FilaSuperficieRama, LienzoRama);

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla($"{CarpetaMecanicas}/Antorcha/", PixelesPorUnidadAntorcha, Pivote(24.5f, 85f, LienzoAntorcha),
                "Antorcha: 43×86, el pivote al pie del poste. A 40 px/u mide 1,7 u y su cabeza queda a 1,45 u, donde su luz.",
                LienzoAntorcha),
            new TablaArte.Regla($"{CarpetaMecanicas}/Cepo/", PixelesPorUnidadCepo, Pivote(40f, 54f, LienzoCepo),
                "Cepo: 79×55, el pivote en el centro de su base, en el suelo. A 80 px/u mide 0,95 u.", LienzoCepo),
            new TablaArte.Regla($"{CarpetaMecanicas}/Rama/", PixelesPorUnidadRama, PivoteRama,
                "Ramas: 91×70, el pivote en el centro de su superficie (fila 22). A 42,5 px/u miden 2 u y 0,54 de grueso.",
                LienzoRama),
            new TablaArte.Regla($"{CarpetaMecanicas}/Cinta/", PixelesPorUnidadCinta, Pivote(33.5f, 10.5f, LienzoCinta),
                "Cinta blanca: 58×57, el pivote en el nudo (va en el centro de su rama). Cuelga 0,9 u.", LienzoCinta),
            new TablaArte.Regla($"{CarpetaMecanicas}/Agua/", PixelesPorUnidadAgua, Pivote(LienzoAgua.x * 0.5f, FilaDelAgua, LienzoAgua),
                "Superficie de la ciénaga: 179×34, se repite a lo ancho. El pivote en la línea del agua (fila 9): por encima, las lentejas.",
                LienzoAgua),
            new TablaArte.Regla($"{CarpetaMecanicas}/Marana/", PixelesPorUnidadMarana, Pivote(LienzoMarana.x * 0.5f, 0f, LienzoMarana),
                "Maraña: 77×76, el pivote arriba en el centro: cuelga del techo del túnel. A 37,5 px/u mide 2 × 2 u.",
                LienzoMarana),
        };

        // --- La antorcha ---

        public const string Apagada = "Apagada";
        public const string Encendida = "Encendida";
        public const string Consumida = "Consumida";

        /// <summary>Un clip por valor de <see cref="ESTADOS_ANTORCHA"/>, en su orden. Encendida, la llama se mueve en bucle.</summary>
        public static readonly ConstructorMecanicas.Mecanica MecanicaAntorcha = new ConstructorMecanicas.Mecanica
        {
            Nombre = "Antorcha",
            Estados = typeof(ESTADOS_ANTORCHA),
            Clips = new[]
            {
                new Clip { Nombre = "Antorcha_Apagada", Estado = Apagada, Fotogramas = new[] { "Apagada" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
                new Clip { Nombre = "Antorcha_Encendida", Estado = Encendida, Fotogramas = new[] { "Encendida1", "Encendida2", "Encendida3", "Encendida2" }, Duraciones = new[] { 0.12f, 0.12f, 0.12f, 0.12f }, Bucle = true },
                new Clip { Nombre = "Antorcha_Consumida", Estado = Consumida, Fotogramas = new[] { "Consumida" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            },
        };

        public const string RutaControladorAntorcha = CarpetaAnimaciones + "/Antorcha.controller";

        public static AnimatorController ControladorAntorcha => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControladorAntorcha);

        /// <summary>La antorcha apagada, o null si no hay arte.</summary>
        public static Sprite AntorchaApagada => AssetDatabase.LoadAssetAtPath<Sprite>(Antorcha("Apagada"));

        // --- El cepo, las ramas y la cinta ---

        public static Sprite CepoAbierto => AssetDatabase.LoadAssetAtPath<Sprite>(Cepo("Abierto"));

        public static Sprite CepoCerrado => AssetDatabase.LoadAssetAtPath<Sprite>(Cepo("Cerrado"));

        public static Sprite RamaSana => AssetDatabase.LoadAssetAtPath<Sprite>(Rama("Rama"));

        public static Sprite RamaPodrida => AssetDatabase.LoadAssetAtPath<Sprite>(Rama("RamaPodrida"));

        public static Sprite RamaRota => AssetDatabase.LoadAssetAtPath<Sprite>(Rama("RamaRota"));

        public static Sprite Astillas => AssetDatabase.LoadAssetAtPath<Sprite>(Rama("Astillas"));

        public static readonly string[] FotogramasCinta = { "Cinta1", "Cinta2" };

        /// <summary>Lo que dura cada fotograma de la cinta al viento.</summary>
        public const float SegundosCinta = 0.6f;

        /// <summary>Los fotogramas de la cinta, o null si falta alguno.</summary>
        public static Sprite[] FotogramasDeLaCinta
        {
            get
            {
                var sprites = FotogramasCinta.Select(f => AssetDatabase.LoadAssetAtPath<Sprite>(Cinta(f))).ToArray();
                return sprites.All(s => s != null) ? sprites : null;
            }
        }

        // --- La ciénaga ---

        public static readonly string[] FotogramasAgua = { "Agua1", "Agua2", "Agua3" };

        /// <summary>Lo que dura cada fotograma de la superficie del agua (el agua quieta de la ciénaga, como la del Mundo 2).</summary>
        public const float SegundosAgua = ConstructorMecanicasMundo2.SegundosAgua;

        /// <summary>La maraña quieta: se mece.</summary>
        public static readonly string[] FotogramasMaranaReposo = { "Marana1", "Marana2" };

        /// <summary>La maraña cerrándose sobre Kael: el último se queda mientras lo sujeta.</summary>
        public static readonly string[] FotogramasMaranaAtrapa = { "Atrapa1", "Atrapa2" };

        /// <summary>La superficie del agua de la ciénaga, o null si falta algún fotograma.</summary>
        public static Sprite[] SuperficieDelAgua => Cargar(FotogramasAgua, Agua);

        /// <summary>La maraña en reposo, o null si falta algún fotograma.</summary>
        public static Sprite[] MaranaEnReposo => Cargar(FotogramasMaranaReposo, Marana);

        /// <summary>La maraña cerrándose, o null si falta algún fotograma.</summary>
        public static Sprite[] MaranaQueAtrapa => Cargar(FotogramasMaranaAtrapa, Marana);

        private static Sprite[] Cargar(IEnumerable<string> nombres, System.Func<string, string> ruta)
        {
            var sprites = nombres.Select(n => AssetDatabase.LoadAssetAtPath<Sprite>(ruta(n))).ToArray();
            return sprites.All(s => s != null) ? sprites : null;
        }

        /// <summary>¿Están las cuatro ramas? (La sana viste las del recorrido; las otras, la podrida.)</summary>
        public static bool HayRamas => RamaSana != null && RamaPodrida != null && RamaRota != null && Astillas != null;

        /// <summary>Lo que mide de ancho el dibujo de una rama de <paramref name="largo"/> unidades: su largo y sus márgenes vacíos.</summary>
        public static float AnchoDibujoRama(float largo) => largo + 2f * MargenRama / PixelesPorUnidadRama;

        /// <summary>El alto del lienzo de la rama, en unidades.</summary>
        public static float AltoDibujoRama => LienzoRama.y / PixelesPorUnidadRama;

        [MenuItem("Fragmentum/Niveles/Generar las mecánicas del Mundo 3 (Miedo)", priority = 136)]
        public static void MenuConstruir()
        {
            if (Construir() && GeneradorMecanicas.CrearPrefabs())
            {
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Fija los ajustes de importación, los bordes de las ramas (para alargarlas) y el
        /// controlador de la antorcha. Sin arte no falla: cada pieza se queda con su rectángulo.
        /// </summary>
        public static bool Construir()
        {
            var ok = true;
            var piezas = new[]
            {
                MecanicaAntorcha.Clips.SelectMany(c => c.Fotogramas).Distinct().Select(Antorcha),
                new[] { Cepo("Abierto"), Cepo("Cerrado") },
                new[] { Rama("Rama"), Rama("RamaPodrida"), Rama("RamaRota"), Rama("Astillas") },
                FotogramasCinta.Select(Cinta),
                FotogramasAgua.Select(Agua),
                FotogramasMaranaReposo.Concat(FotogramasMaranaAtrapa).Select(Marana),
            }.SelectMany(r => r);
            foreach (var ruta in piezas)
            {
                if (AssetImporter.GetAtPath(ruta) is TextureImporter importador)
                {
                    var cambia = TablaArte.Ajustar(importador);
                    cambia |= AjustarBordes(importador);
                    if (cambia)
                    {
                        importador.SaveAndReimport();
                    }
                }
                else
                {
                    Debug.LogWarning($"[Fragmentum] Falta {ruta}: esa pieza del Mundo 3 se queda con su rectángulo.");
                }
            }

            if (AntorchaApagada != null)
            {
                ok &= ArmarAntorcha();
            }

            Debug.Log("[Fragmentum] Mecánicas del Mundo 3: la antorcha, el cepo, las ramas, la cinta, el agua de la ciénaga y la maraña importados.");
            return ok;
        }

        // Las ramas: sus puntas no se repiten. El centro sí, para alargarlas (modo Tiled).
        private static bool AjustarBordes(TextureImporter importador)
        {
            var esRama = importador.assetPath.StartsWith($"{CarpetaMecanicas}/Rama/");
            var bordes = esRama ? new Vector4(BordeIzquierdoRama, 0f, BordeDerechoRama, 0f) : Vector4.zero;
            if (importador.spriteBorder == bordes)
            {
                return false;
            }

            importador.spriteBorder = bordes;
            return true;
        }

        private static bool ArmarAntorcha()
        {
            if (!ClipsDeSprites.CargarSprites(MecanicaAntorcha.Clips, Antorcha, "la antorcha", out var sprites) || sprites == null)
            {
                return false;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            var clips = MecanicaAntorcha.Clips.Select(c => (c.Estado, (Motion)ClipsDeSprites.Crear(CarpetaAnimaciones, c, sprites))).ToList();
            var controlador = ControladoresAnimator.CargarOCrear(RutaControladorAntorcha);
            ControladoresAnimator.AsegurarParametros(controlador,
                (AnimatorParams.EstadoMecanica.Nombre, AnimatorControllerParameterType.Int, 0f));
            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, clips);
            foreach (var estado in estados.Values)
            {
                ControladoresAnimator.Transiciones(estado);
            }

            // Desde cualquier estado, al suyo: el entero es el estado de la antorcha.
            var valores = (ESTADOS_ANTORCHA[])System.Enum.GetValues(typeof(ESTADOS_ANTORCHA));
            ControladoresAnimator.DesdeCualquierEstado(maquina, valores.Select((valor, i) => new Transicion(
                MecanicaAntorcha.Clips[i].Estado, estados[MecanicaAntorcha.Clips[i].Estado], null,
                ControladoresAnimator.Condicion(AnimatorConditionMode.Equals, AnimatorParams.EstadoMecanica.Nombre, (int)valor))).ToArray());

            EditorUtility.SetDirty(controlador);
            return true;
        }
    }
}
