using UnityEditor;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Los efectos de los ataques de los jefes, con su arte. Por ahora, la onda de la Centinela
    /// (<c>Tesis/Imagenes/Onda</c>, limpiada con <c>Imagenes/limpios/script/limpiar_onda.py</c>):
    /// - sus tres fotogramas en <see cref="CarpetaSprites"/> (sube, cresta, rompe), con la regla de
    ///   <see cref="TablaArte"/>: 98 px/u y el pivote en su frente sobre el suelo;
    /// - el clip en bucle y un controlador de un solo estado, sin parámetros, en
    ///   <see cref="CarpetaAnimaciones"/>;
    /// - el prefab <see cref="RutaPrefabOnda"/>, que los patrones de la onda ponen dentro de su
    ///   zona de daño (<c>ZonaDeDanio.Vestir</c>). Mira a la derecha; la zona lo espeja.
    ///
    /// Sin el arte avisa y no crea nada: las ondas siguen siendo rectángulos. Es idempotente: el
    /// clip, el controlador y el prefab se rehacen sobre los mismos assets.
    /// </summary>
    public static class ConstructorEfectos
    {
        public const string CarpetaSprites = TablaArte.CarpetaEfectos;
        public const string CarpetaAnimaciones = "Assets/Animaciones/Efectos";
        public const string CarpetaPrefabs = "Assets/Prefabs/Efectos";

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

        /// <summary>Como las zonas de daño: delante del suelo (0) y detrás de Kael (10).</summary>
        public const int OrdenDibujo = 8;

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        public static string RutaControladorOnda => $"{CarpetaAnimaciones}/Onda.controller";

        public static string RutaPrefabOnda => $"{CarpetaPrefabs}/{NombreOnda}.prefab";

        public static GameObject PrefabOnda => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabOnda);

        [MenuItem("Fragmentum/Jefes/Generar los efectos de los ataques (la onda)", priority = 143)]
        public static void MenuConstruir() => Construir();

        /// <summary>Devuelve false solo si algo falló; si falta el arte avisa y no construye nada.</summary>
        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(new[] { OndaAvanza }, RutaSprite, "la onda de la Centinela", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            AsegurarCarpeta(CarpetaPrefabs);
            var clip = ClipsDeSprites.Crear(CarpetaAnimaciones, OndaAvanza, sprites);
            var controlador = ControladoresAnimator.CargarOCrear(RutaControladorOnda);
            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, new[] { (OndaAvanza.Estado, (Motion)clip) });
            ControladoresAnimator.Transiciones(estados[OndaAvanza.Estado]);
            maquina.defaultState = estados[OndaAvanza.Estado];
            EditorUtility.SetDirty(controlador);
            AssetDatabase.SaveAssets();

            var ok = GuardarPrefabOnda(sprites[OndaAvanza.Fotogramas[0]], controlador);
            if (ok)
            {
                Debug.Log($"[Fragmentum] Onda de la Centinela: {OndaAvanza.Fotogramas.Length} fotogramas, su clip y el prefab {RutaPrefabOnda}.");
            }

            return ok;
        }

        private static bool GuardarPrefabOnda(Sprite primero, RuntimeAnimatorController controlador)
        {
            var raiz = new GameObject(NombreOnda);
            try
            {
                var dibujo = raiz.AddComponent<SpriteRenderer>();
                dibujo.sprite = primero;
                dibujo.sortingOrder = OrdenDibujo;
                var animator = raiz.AddComponent<Animator>();
                animator.runtimeAnimatorController = controlador;
                animator.applyRootMotion = false;

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabOnda, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {RutaPrefabOnda}.");
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
