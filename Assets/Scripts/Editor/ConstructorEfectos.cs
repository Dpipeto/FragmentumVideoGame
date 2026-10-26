using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;
using Transicion = Fragmentum.EditorTools.ControladoresAnimator.Transicion;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Los efectos de los ataques de los jefes, con su arte. Por ahora, dos de la Centinela:
    /// - <b>la onda</b> (<c>Tesis/Imagenes/Onda</c>, limpiada con <c>limpiar_onda.py</c>): tres
    ///   fotogramas (sube, cresta, rompe) a 98 px/u con el pivote en su frente sobre el suelo.
    ///   Mira a la derecha; la zona la espeja;
    /// - <b>la erupción</b> (<c>Tesis/Imagenes/Erupcion</c>, limpiada con <c>limpiar_erupcion.py</c>):
    ///   cinco fotogramas a 24 px/u con el pivote en el centro de la base de la columna. Su
    ///   Animator sigue las fases de la zona (<see cref="DibujoDeZona"/>): avisa, brota, arde en
    ///   bucle y, cuando la zona acaba, se derrumba.
    ///
    /// Cada efecto deja sus sprites en <see cref="CarpetaSprites"/> (las reglas, en
    /// <see cref="TablaArte"/>), sus clips y su controlador en <see cref="CarpetaAnimaciones"/>, y
    /// un prefab en <see cref="CarpetaPrefabs"/> que el patrón pone dentro de su zona de daño
    /// (<c>ZonaDeDanio.Vestir</c>).
    ///
    /// Sin el arte de un efecto avisa y no lo crea: su ataque sigue siendo un rectángulo. Es
    /// idempotente: el clip, el controlador y el prefab se rehacen sobre los mismos assets.
    /// </summary>
    public static class ConstructorEfectos
    {
        public const string CarpetaSprites = TablaArte.CarpetaEfectos;
        public const string CarpetaAnimaciones = "Assets/Animaciones/Efectos";
        public const string CarpetaPrefabs = "Assets/Prefabs/Efectos";

        /// <summary>Como las zonas de daño: delante del suelo (0) y detrás de Kael (10).</summary>
        public const int OrdenDibujo = 8;

        // --- La onda ---

        /// <summary>
        /// La onda llegó dibujada a unos 225 píxeles de ancho (se pidieron 60). A 49 px/u, la escala
        /// de la Centinela, mediría 4,7 × 2,4 u: más alta que el salto de Kael. A 98 (píxeles de la
        /// mitad) mide 2,4 × 1,2 u, y la roca de su cresta, lo que su zona de daño (0,9 u).
        /// </summary>
        public const float PixelesPorUnidadOnda = 98f;

        /// <summary>El lienzo común de los tres fotogramas: suelo en la última fila, frente en la columna 209.</summary>
        public static readonly Vector2Int LienzoOnda = new Vector2Int(232, 119);

        /// <summary>El frente de la onda sobre el suelo (borde derecho de la columna 209, fila de abajo).</summary>
        public static readonly Vector2 PivoteOnda = new Vector2(210f / 232f, 0f);

        public const string Avanza = "Avanza";

        /// <summary>La onda avanza en bucle: la cresta sube, se curva y rompe, y vuelve a subir.</summary>
        public static readonly Clip OndaAvanza = new Clip
        {
            Nombre = "Onda_Avanza", Estado = Avanza, Fotogramas = new[] { "Onda_1_sube", "Onda_2_cresta", "Onda_3_rompe" },
            Duraciones = new[] { 0.1f, 0.1f, 0.1f }, Bucle = true,
        };

        public const string NombreOnda = "OndaDeFuego";

        public static string RutaControladorOnda => $"{CarpetaAnimaciones}/Onda.controller";

        public static string RutaPrefabOnda => $"{CarpetaPrefabs}/{NombreOnda}.prefab";

        public static GameObject PrefabOnda => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabOnda);

        // --- La erupción ---

        /// <summary>
        /// La columna mide 44 × 136 píxeles (150 con las gotas de arriba). A 24 px/u, el tamaño de
        /// píxel del Golem, la torreta y la bola de fuego, mide 1,83 × 5,7 u: cubre su zona de daño
        /// (1,6 × 5) con un poco de margen, así que no quema donde no se ve.
        /// </summary>
        public const float PixelesPorUnidadErupcion = 24f;

        /// <summary>
        /// El lienzo común de los cinco: la base en la última fila y su centro en la mitad. Es más
        /// ancho que la columna porque el aviso y el derrumbe se extienden por el suelo (66 y 73 px).
        /// </summary>
        public static readonly Vector2Int LienzoErupcion = new Vector2Int(74, 150);

        /// <summary>El centro de la base de la columna: donde brota.</summary>
        public static readonly Vector2 PivoteErupcion = new Vector2(0.5f, 0f);

        public const string Aviso = "Aviso";
        public const string Brota = "Brota";
        public const string Arde = "Arde";
        public const string Apaga = "Apaga";

        /// <summary>Mientras la zona avisa: la grieta que brilla, quieta.</summary>
        public static readonly Clip ErupcionAviso = new Clip
        {
            Nombre = "Erupcion_Aviso", Estado = Aviso, Fotogramas = new[] { "Erupcion_1_aviso" },
            Duraciones = new[] { 0.1f }, Bucle = true,
        };

        /// <summary>Al empezar a quemar, un instante a media altura antes de la columna llena.</summary>
        public static readonly Clip ErupcionBrota = new Clip
        {
            Nombre = "Erupcion_Brota", Estado = Brota, Fotogramas = new[] { "Erupcion_2_brota" },
            Duraciones = new[] { 0.1f },
        };

        /// <summary>Mientras quema, las dos variantes de la columna llena alternan (3 y 4 de la tanda).</summary>
        public static readonly Clip ErupcionArde = new Clip
        {
            Nombre = "Erupcion_Arde", Estado = Arde, Fotogramas = new[] { "Erupcion_3_arde", "Erupcion_4_arde" },
            Duraciones = new[] { 0.1f, 0.1f }, Bucle = true,
        };

        /// <summary>Al acabar la zona, ya sin daño, la columna se derrumba y desaparece.</summary>
        public static readonly Clip ErupcionApaga = new Clip
        {
            Nombre = "Erupcion_Apaga", Estado = Apaga, Fotogramas = new[] { "Erupcion_5_colapsa" },
            Duraciones = new[] { 0.25f },
        };

        public static readonly Clip[] ClipsErupcion = { ErupcionAviso, ErupcionBrota, ErupcionArde, ErupcionApaga };

        public const string NombreErupcion = "ErupcionDeLava";

        public static string RutaControladorErupcion => $"{CarpetaAnimaciones}/Erupcion.controller";

        public static string RutaPrefabErupcion => $"{CarpetaPrefabs}/{NombreErupcion}.prefab";

        public static GameObject PrefabErupcion => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabErupcion);

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        [MenuItem("Fragmentum/Jefes/Generar los efectos de los ataques (onda y erupción)", priority = 143)]
        public static void MenuConstruir() => Construir();

        /// <summary>Devuelve false solo si algo falló; si falta el arte de un efecto avisa y sigue.</summary>
        public static bool Construir()
        {
            var onda = ConstruirOnda();
            var erupcion = ConstruirErupcion();
            return onda && erupcion;
        }

        private static bool ConstruirOnda()
        {
            if (!CargarSprites(new[] { OndaAvanza }, "la onda de la Centinela", out var sprites, out var ok))
            {
                return ok;
            }

            var clip = ClipsDeSprites.Crear(CarpetaAnimaciones, OndaAvanza, sprites);
            var controlador = ControladoresAnimator.CargarOCrear(RutaControladorOnda);
            ControladoresAnimator.AsegurarParametros(controlador);
            var estados = ControladoresAnimator.Estados(controlador.layers[0].stateMachine, new[] { (OndaAvanza.Estado, (Motion)clip) });
            ControladoresAnimator.Transiciones(estados[OndaAvanza.Estado]);
            return Terminar(NombreOnda, RutaPrefabOnda, sprites[OndaAvanza.Fotogramas[0]], controlador, 0f);
        }

        /// <summary>
        /// Aviso en bucle hasta que la zona quema (<c>Quema</c>); entonces brota y pasa sola a arder
        /// en bucle. Cuando la zona acaba (<c>Apagada</c>), desde donde esté, se derrumba.
        /// </summary>
        private static bool ConstruirErupcion()
        {
            if (!CargarSprites(ClipsErupcion, "la erupción de la Centinela", out var sprites, out var ok))
            {
                return ok;
            }

            var clips = new List<(string, Motion)>();
            foreach (var definicion in ClipsErupcion)
            {
                clips.Add((definicion.Estado, ClipsDeSprites.Crear(CarpetaAnimaciones, definicion, sprites)));
            }

            var controlador = ControladoresAnimator.CargarOCrear(RutaControladorErupcion);
            ControladoresAnimator.AsegurarParametros(controlador,
                (AnimatorParams.Quema.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Apagada.Nombre, AnimatorControllerParameterType.Bool, 0f));
            var e = ControladoresAnimator.Estados(controlador.layers[0].stateMachine, clips);

            var apagada = ControladoresAnimator.Condicion(AnimatorConditionMode.If, AnimatorParams.Apagada.Nombre);
            var quema = ControladoresAnimator.Condicion(AnimatorConditionMode.If, AnimatorParams.Quema.Nombre);
            ControladoresAnimator.Transiciones(e[Aviso],
                new Transicion("Apagada", e[Apaga], null, apagada),
                new Transicion("Quema", e[Brota], null, quema));
            ControladoresAnimator.Transiciones(e[Brota],
                new Transicion("Apagada", e[Apaga], null, apagada),
                new Transicion("Llena", e[Arde], 1f));
            ControladoresAnimator.Transiciones(e[Arde], new Transicion("Apagada", e[Apaga], null, apagada));
            ControladoresAnimator.Transiciones(e[Apaga]);

            return Terminar(NombreErupcion, RutaPrefabErupcion, sprites[ErupcionAviso.Fotogramas[0]], controlador, ErupcionApaga.Duracion);
        }

        // Carga los sprites. False si no hay que seguir: falta el arte (ok) o no se pudo importar (no ok).
        private static bool CargarSprites(Clip[] clips, string quien, out Dictionary<string, Sprite> sprites, out bool ok)
        {
            ok = ClipsDeSprites.CargarSprites(clips, RutaSprite, quien, out sprites);
            if (!ok || sprites == null)
            {
                return false;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            AsegurarCarpeta(CarpetaPrefabs);
            return true;
        }

        private static bool Terminar(string nombre, string rutaPrefab, Sprite primero, AnimatorController controlador, float duracionApagado)
        {
            EditorUtility.SetDirty(controlador);
            AssetDatabase.SaveAssets();

            var ok = GuardarPrefab(nombre, rutaPrefab, primero, controlador, duracionApagado);
            if (ok)
            {
                Debug.Log($"[Fragmentum] Efecto {nombre}: sus clips, su controlador y el prefab {rutaPrefab}.");
            }

            return ok;
        }

        // Con duracionApagado > 0, el dibujo sigue las fases de su zona (DibujoDeZona).
        private static bool GuardarPrefab(string nombre, string ruta, Sprite primero, RuntimeAnimatorController controlador, float duracionApagado)
        {
            var raiz = new GameObject(nombre);
            try
            {
                var dibujo = raiz.AddComponent<SpriteRenderer>();
                dibujo.sprite = primero;
                dibujo.sortingOrder = OrdenDibujo;
                var animator = raiz.AddComponent<Animator>();
                animator.runtimeAnimatorController = controlador;
                animator.applyRootMotion = false;
                if (duracionApagado > 0f)
                {
                    raiz.AddComponent<DibujoDeZona>().duracionApagado = duracionApagado;
                }

                PrefabUtility.SaveAsPrefabAsset(raiz, ruta, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {ruta}.");
                }

                return guardado;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }
    }
}
