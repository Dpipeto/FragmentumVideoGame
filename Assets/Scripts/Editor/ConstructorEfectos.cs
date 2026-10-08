using UnityEditor;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Los efectos de los ataques de los jefes, con su arte. Por ahora, dos de la Centinela:
    /// - <b>la onda</b> (<c>Tesis/Imagenes/Onda</c>, limpiada con <c>limpiar_onda.py</c>): tres
    ///   fotogramas (sube, cresta, rompe) a 98 px/u con el pivote en su frente sobre el suelo.
    ///   Mira a la derecha; la zona la espeja;
    /// - <b>la erupción</b> (<c>Tesis/Imagenes/Erupcion</c>, limpiada con <c>limpiar_erupcion.py</c>):
    ///   la columna llena en dos variantes, a 24 px/u con el pivote en el centro de su base. Solo
    ///   esas dos entraron de la segunda tanda; el aviso, el brote y el colapso están pedidos.
    ///
    /// Cada efecto deja sus sprites en <see cref="CarpetaSprites"/> (las reglas, en
    /// <see cref="TablaArte"/>), un clip en bucle y un controlador de un solo estado, sin
    /// parámetros, en <see cref="CarpetaAnimaciones"/>, y un prefab en <see cref="CarpetaPrefabs"/>
    /// que el patrón pone dentro de su zona de daño (<c>ZonaDeDanio.Vestir</c>).
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
        /// La columna llegó a 44 × 136 píxeles (150 con las gotas de arriba). A 24 px/u, el tamaño de
        /// píxel del Golem, la torreta y la bola de fuego, mide 1,83 × 5,7 u: cubre su zona de daño
        /// (1,6 × 5) con un poco de margen, así que no quema donde no se ve.
        /// </summary>
        public const float PixelesPorUnidadErupcion = 24f;

        /// <summary>El lienzo común de las dos variantes: la base en la última fila, centrada.</summary>
        public static readonly Vector2Int LienzoErupcion = new Vector2Int(44, 150);

        /// <summary>El centro de la base de la columna: donde brota.</summary>
        public static readonly Vector2 PivoteErupcion = new Vector2(0.5f, 0f);

        public const string Arde = "Arde";

        /// <summary>Mientras quema, las dos variantes de la columna llena alternan (3 y 4 de la tanda).</summary>
        public static readonly Clip ErupcionArde = new Clip
        {
            Nombre = "Erupcion_Arde", Estado = Arde, Fotogramas = new[] { "Erupcion_3_arde", "Erupcion_4_arde" },
            Duraciones = new[] { 0.1f, 0.1f }, Bucle = true,
        };

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
            var onda = ConstruirEfecto(OndaAvanza, "la onda de la Centinela", RutaControladorOnda, NombreOnda, RutaPrefabOnda);
            var erupcion = ConstruirEfecto(ErupcionArde, "la erupción de la Centinela", RutaControladorErupcion, NombreErupcion, RutaPrefabErupcion);
            return onda && erupcion;
        }

        private static bool ConstruirEfecto(Clip definicion, string quien, string rutaControlador, string nombrePrefab, string rutaPrefab)
        {
            if (!ClipsDeSprites.CargarSprites(new[] { definicion }, RutaSprite, quien, out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            AsegurarCarpeta(CarpetaPrefabs);
            var clip = ClipsDeSprites.Crear(CarpetaAnimaciones, definicion, sprites);
            var controlador = ControladoresAnimator.CargarOCrear(rutaControlador);
            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, new[] { (definicion.Estado, (Motion)clip) });
            ControladoresAnimator.Transiciones(estados[definicion.Estado]);
            maquina.defaultState = estados[definicion.Estado];
            EditorUtility.SetDirty(controlador);
            AssetDatabase.SaveAssets();

            var ok = GuardarPrefab(nombrePrefab, rutaPrefab, sprites[definicion.Fotogramas[0]], controlador);
            if (ok)
            {
                Debug.Log($"[Fragmentum] Efecto {nombrePrefab} ({quien}): {definicion.Fotogramas.Length} fotogramas, su clip y el prefab {rutaPrefab}.");
            }

            return ok;
        }

        private static bool GuardarPrefab(string nombre, string ruta, Sprite primero, RuntimeAnimatorController controlador)
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
