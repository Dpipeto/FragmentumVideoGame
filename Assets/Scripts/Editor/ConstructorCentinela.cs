using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El arte de la Centinela Ardiente, jefe del Mundo 1:
    /// 1. importa los 29 fotogramas de <see cref="CarpetaSprites"/> como pixel art, con el mismo
    ///    lienzo y el mismo pivote en los pies para todos (si no, saltaría al cambiar de clip);
    /// 2. crea los 8 clips desde <see cref="Clips"/>, con los tiempos del diseño y el forcejeo de
    ///    la espada clavada.
    /// El controlador y el prefab llegan en las siguientes fases.
    ///
    /// El arte llega limpio y alineado desde <c>Tesis/Jefe1 animaciones/limpios</c>, que tiene su
    /// LEEME. Los PNG no se retocan: cualquier recorte o reescalado rompe el anclaje común.
    ///
    /// Idempotente: «Regenerar todo» lo ejecuta y, si nada cambió, no cambia nada.
    /// </summary>
    public static class ConstructorCentinela
    {
        public const string CarpetaSprites = ArteJefes.Carpeta + "/Centinela";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Jefes/Centinela";

        // --- Lienzo, escala y pivote (del LEEME del arte) ---

        /// <summary>Lienzo común de los 29 fotogramas. No se recorta: ahí está la alineación.</summary>
        public const int AnchoLienzo = 262;
        public const int AltoLienzo = 252;

        /// <summary>Línea de suelo: la fila 237 contando desde arriba, la misma en los 29.</summary>
        public const int FilaSuelo = 237;

        /// <summary>
        /// Centroide de los pies, normalizado desde abajo a la izquierda. En vertical cae en el
        /// borde de arriba de la línea de suelo (0,060 × 252 = 15,1 px desde abajo), así que se
        /// puede cortar de cualquier clip a cualquier otro sin que el sprite salte.
        /// </summary>
        public static readonly Vector2 Pivote = new Vector2(0.534f, 0.060f);

        /// <summary>
        /// Andando ocupa 168 px: a 49 px por unidad mide 3,4 u, casi el doble que Kael (1,77 u) y
        /// lo mismo que su caja de jefe (<c>JefeData.tamano</c>).
        /// </summary>
        public const float PixelesPorUnidad = 49f;

        /// <summary>
        /// El idle frontal (I1-I3) está dibujado más grande, 199 px. A 58 px por unidad mide 3,4 u,
        /// como el resto en combate. A 49 solo valdría para un plano de presentación.
        /// </summary>
        public const float PixelesPorUnidadIdle = 58f;

        public static readonly string[] FotogramasIdle = { "I1", "I2", "I3" };

        /// <summary>
        /// Los 29 fotogramas. C1b es un impacto alternativo a C1: se importa, pero ningún clip lo
        /// usa. Faltan A3 y C3 porque se descartaron al limpiar el arte, y no se rellenan.
        /// </summary>
        public static readonly string[] Archivos =
        {
            "I1", "I2", "I3",
            "A1", "A2", "A4",
            "E1", "E2", "E3", "E4",
            "C1", "C1b", "C2", "C4",
            "G1", "G2", "G3",
            "R1", "R2", "R3",
            "J1", "J2", "J3", "J4", "J5",
            "D1", "D2", "D3", "D4",
        };

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        public static float PixelesPorUnidadDe(string archivo) =>
            FotogramasIdle.Contains(archivo) ? PixelesPorUnidadIdle : PixelesPorUnidad;

        // --- Animaciones ---

        /// <summary>Muestras por segundo de los clips: los tiempos de la tabla caen en ellas.</summary>
        public const float FotogramasPorSegundo = 60f;

        /// <summary>
        /// Duración de un fotograma que se queda en pantalla al acabar el clip (D4): se muestra un
        /// fotograma y el clip, sin bucle, se detiene en él.
        /// </summary>
        public const float SeQueda = 1f / FotogramasPorSegundo;

        /// <summary>Un clip: sus fotogramas (por nombre de archivo) y cuánto dura cada uno.</summary>
        public sealed class Clip
        {
            public string Estado;
            public string[] Fotogramas;
            public float[] Duraciones;
            public bool Bucle;

            public float Duracion => Duraciones.Sum();

            /// <summary>Instante en que empieza el fotograma i.</summary>
            public float Inicio(int i) => Duraciones.Take(i).Sum();

            public string Ruta => $"{CarpetaAnimaciones}/Centinela_{Estado}.anim";
        }

        // Nombres de los estados del controlador.
        public const string Idle = "Idle";
        public const string Andar = "Andar";
        public const string Espadazo = "Espadazo";
        public const string EspadaClavada = "EspadaClavada";
        public const string GolpeSuelo = "GolpeSuelo";
        public const string Rugido = "Rugido";
        public const string Salto = "Salto";
        public const string Muerte = "Muerte";

        /// <summary>
        /// Tabla del diseño (LEEME del arte). Los tiempos no son uniformes a propósito: en los
        /// tres ataques la anticipación (E1, G1, J1) es larga y visible y el golpe (E2, G2, J4),
        /// casi instantáneo. Si se igualan, el jugador no llega a reaccionar.
        ///
        /// - E4 cierra el espadazo y es también la pose de guardia: de ahí sale a Idle sin corte.
        /// - C2 aguanta 1,2 s, lo que dura la ventana vulnerable; el forcejeo lo pone
        ///   <see cref="CurvaForcejeo"/>, sin fotograma extra.
        /// - J3 y J4 están anclados al suelo como los demás: la altura del salto la pone el cuerpo
        ///   del jefe al moverse (<c>BossController.Saltar</c>), no el sprite.
        /// - D4 (la ceniza con el fragmento) se queda en pantalla al acabar la muerte.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Estado = Idle, Fotogramas = new[] { "I1", "I2", "I3", "I2" }, Duraciones = new[] { 0.40f, 0.40f, 0.40f, 0.40f }, Bucle = true },
            new Clip { Estado = Andar, Fotogramas = new[] { "A1", "A2", "A4", "A2" }, Duraciones = new[] { 0.15f, 0.15f, 0.15f, 0.15f }, Bucle = true },
            new Clip { Estado = Espadazo, Fotogramas = new[] { "E1", "E2", "E3", "E4" }, Duraciones = new[] { 0.55f, 0.08f, 0.22f, 0.30f } },
            new Clip { Estado = EspadaClavada, Fotogramas = new[] { "C1", "C2", "C4" }, Duraciones = new[] { 0.09f, 1.20f, 0.18f } },
            new Clip { Estado = GolpeSuelo, Fotogramas = new[] { "G1", "G2", "G3" }, Duraciones = new[] { 0.50f, 0.07f, 0.40f } },
            new Clip { Estado = Rugido, Fotogramas = new[] { "R1", "R2", "R3" }, Duraciones = new[] { 0.35f, 0.35f, 0.20f } },
            new Clip { Estado = Salto, Fotogramas = new[] { "J1", "J2", "J3", "J4", "J5" }, Duraciones = new[] { 0.50f, 0.10f, 0.30f, 0.15f, 0.45f } },
            new Clip { Estado = Muerte, Fotogramas = new[] { "D1", "D2", "D3", "D4" }, Duraciones = new[] { 0.40f, 0.50f, 1.20f, SeQueda } },
        };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        /// <summary>El forcejeo de C2: sube 0,02 u (un píxel del dibujo) y baja cada 0,3 s.</summary>
        public const float AlturaForcejeo = 0.02f;
        public const float PeriodoForcejeo = 0.3f;

        /// <summary>El fotograma de la espada clavada, el que forcejea.</summary>
        public const string FotogramaForcejeo = "C2";

        // --- Menús y modo batch ---

        [MenuItem("Fragmentum/Jefes/Generar los clips de la Centinela", priority = 142)]
        public static void MenuConstruir()
        {
            if (Construir())
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipDe(Idle).Ruta));
            }
        }

        // --- Construcción ---

        /// <summary>
        /// Fija los ajustes de importación y crea los clips. Devuelve false solo si algo falló; si
        /// faltan fotogramas avisa y no construye nada.
        /// </summary>
        public static bool Construir()
        {
            var faltan = Archivos.Select(RutaSprite).Where(r => !File.Exists(r)).ToList();
            if (faltan.Count > 0)
            {
                Debug.LogWarning($"[Fragmentum] Faltan fotogramas de la Centinela: {string.Join(", ", faltan)}. No se crean sus clips.");
                return true;
            }

            var sprites = new Dictionary<string, Sprite>();
            foreach (var archivo in Archivos)
            {
                var ruta = RutaSprite(archivo);
                if (AssetImporter.GetAtPath(ruta) is TextureImporter importador && AjustarImportador(importador))
                {
                    importador.SaveAndReimport();
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
                if (sprite == null)
                {
                    Debug.LogError($"[Fragmentum] {ruta} no se importó como sprite.");
                    return false;
                }

                sprites[archivo] = sprite;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            foreach (var definicion in Clips)
            {
                CrearClip(definicion, sprites);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Fragmentum] Centinela Ardiente: {sprites.Count} fotogramas y {Clips.Length} clips en {CarpetaAnimaciones}.");
            return true;
        }

        // --- 1. Importación ---

        /// <summary>
        /// Ajustes de un fotograma de la Centinela: los de pixel art de todos los jefes, sus
        /// píxeles por unidad y el pivote de los pies. Devuelve si cambió algo. Lo usan
        /// <see cref="Construir"/> y <see cref="ImportadorJefes"/>, que los fija en cada importación.
        /// </summary>
        public static bool AjustarImportador(TextureImporter importador)
        {
            var ppu = PixelesPorUnidadDe(Path.GetFileNameWithoutExtension(importador.assetPath));
            return ArteJefes.Ajustar(importador, ajustes =>
            {
                ajustes.spritePixelsPerUnit = ppu;
                ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
                ajustes.spritePivot = Pivote;
            });
        }

        // --- 2. Clips ---

        private static AnimationClip CrearClip(Clip definicion, IReadOnlyDictionary<string, Sprite> sprites)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(definicion.Ruta);
            var nuevo = clip == null;
            if (nuevo)
            {
                clip = new AnimationClip();
            }

            clip.frameRate = FotogramasPorSegundo;

            // Una clave por fotograma y una más al final con el último, que fija cuánto dura.
            // Unity alarga los clips de sprites un fotograma (1/60 s) tras la última clave: la de
            // cierre va ese fotograma antes para que el clip dure exactamente lo indicado. Si el
            // último fotograma solo dura ese fotograma (D4, que se queda), sobra la de cierre.
            var n = definicion.Fotogramas.Length;
            var cierre = definicion.Duraciones[n - 1] > SeQueda + 1e-5f;
            var claves = new List<ObjectReferenceKeyframe>();
            for (var i = 0; i < n; i++)
            {
                claves.Add(new ObjectReferenceKeyframe { time = definicion.Inicio(i), value = sprites[definicion.Fotogramas[i]] });
            }

            if (cierre)
            {
                claves.Add(new ObjectReferenceKeyframe { time = definicion.Duracion - SeQueda, value = sprites[definicion.Fotogramas[n - 1]] });
            }

            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), claves.ToArray());

            var altura = EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalPosition.y");
            AnimationUtility.SetEditorCurve(clip, altura, definicion.Estado == EspadaClavada ? CurvaForcejeo(definicion) : null);

            var ajustes = AnimationUtility.GetAnimationClipSettings(clip);
            ajustes.loopTime = definicion.Bucle;
            AnimationUtility.SetAnimationClipSettings(clip, ajustes);

            if (nuevo)
            {
                AssetDatabase.CreateAsset(clip, definicion.Ruta);
            }
            else
            {
                EditorUtility.SetDirty(clip);
            }

            return clip;
        }

        /// <summary>
        /// El forcejeo de la espada clavada, sin fotograma extra: durante C2 el sprite sube
        /// <see cref="AlturaForcejeo"/> y vuelve a 0 cada <see cref="PeriodoForcejeo"/>, en escalones
        /// (interpolación Constant). La espada no se mueve ni un píxel respecto del cuerpo. Fuera de
        /// C2 se queda en 0.
        /// </summary>
        public static AnimationCurve CurvaForcejeo(Clip clavada)
        {
            var c2 = System.Array.IndexOf(clavada.Fotogramas, FotogramaForcejeo);
            var inicio = clavada.Inicio(c2);
            var fin = inicio + clavada.Duraciones[c2];

            var curva = new AnimationCurve();
            curva.AddKey(0f, 0f);
            var tramos = Mathf.RoundToInt(clavada.Duraciones[c2] / PeriodoForcejeo);
            for (var k = 1; k < tramos; k++)
            {
                curva.AddKey(inicio + k * PeriodoForcejeo, k % 2 == 1 ? AlturaForcejeo : 0f);
            }

            curva.AddKey(fin, 0f);
            for (var i = 0; i < curva.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curva, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curva, i, AnimationUtility.TangentMode.Constant);
            }

            return curva;
        }
    }
}
