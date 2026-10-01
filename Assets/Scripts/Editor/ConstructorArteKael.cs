using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Convierte las hojas de sprites de Kael en lo que usa el prefab del jugador:
    /// 1. recorta cada hoja en fotogramas, con el pivote en la planta de los pies;
    /// 2. crea un clip por animación (<c>Assets/Animaciones/Kael</c>);
    /// 3. arma <c>Kael.controller</c> con un estado por clip.
    ///
    /// Las hojas van en <see cref="CarpetaSprites"/> a escala 1× (1 píxel de arte = 1 píxel de
    /// la imagen). Si Pixelorama las exportó a 5× o 10×, <c>Herramientas/reducir_hoja.py</c>
    /// las reduce sin pérdida.
    ///
    /// Es idempotente: reutiliza los identificadores de los sprites y los estados que ya
    /// existen, así que volver a ejecutarlo sin cambios no modifica ningún archivo.
    /// </summary>
    public static class ConstructorArteKael
    {
        public const string CarpetaSprites = "Assets/Sprites/Kael";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Kael";
        public const string RutaControlador = CarpetaAnimaciones + "/Kael.controller";

        /// <summary>
        /// Kael mide unos 113 px de arte: a 64 px por unidad son 1,77 u, la altura de su
        /// collider (1,8 u). Los tiles van a 32 PPU: Kael tiene el doble de detalle que el
        /// escenario, como en el arte original.
        /// </summary>
        public const int PixelesPorUnidad = 64;

        /// <summary>
        /// Distancia que recorre Kael en una zancada completa (los 8 fotogramas de correr). El
        /// estado Correr se acelera con el parámetro Movement (u/s): a la velocidad máxima por
        /// defecto (6 u/s) la vuelta dura 0,5 s, y bajo el agua o sobre hielo las piernas van al
        /// ritmo real del cuerpo, sin patinar.
        /// </summary>
        public const float UnidadesPorZancada = 3f;

        /// <summary>Una tira horizontal de fotogramas del mismo tamaño.</summary>
        public sealed class Hoja
        {
            public string Archivo;
            public int Fotogramas;

            /// <summary>
            /// Pivote en píxeles desde la esquina inferior izquierda del fotograma: x en el centro
            /// del torso, y en la planta de los pies. Así cambiar de clip no desplaza a Kael.
            /// </summary>
            public Vector2Int Pivote;

            public string Ruta => $"{CarpetaSprites}/{Archivo}.png";
        }

        public static readonly Hoja HojaIdle = new Hoja { Archivo = "Kael_Idle", Fotogramas = 2, Pivote = new Vector2Int(40, 0) };
        public static readonly Hoja HojaCorrer = new Hoja { Archivo = "Kael_Correr", Fotogramas = 8, Pivote = new Vector2Int(45, 0) };
        public static readonly Hoja HojaSalto = new Hoja { Archivo = "Kael_Salto", Fotogramas = 9, Pivote = new Vector2Int(40, 2) };
        public static readonly Hoja HojaAtaque = new Hoja { Archivo = "Kael_Ataque", Fotogramas = 7, Pivote = new Vector2Int(65, 5) };

        public static readonly Hoja[] Hojas = { HojaIdle, HojaCorrer, HojaSalto, HojaAtaque };

        /// <summary>Un clip: qué fotogramas de qué hoja y en qué momento empieza cada uno.</summary>
        public sealed class Clip
        {
            /// <summary>Nombre del estado en el Animator (constantes de <see cref="PlayerAnimacion"/>).</summary>
            public string Estado;
            public Hoja Hoja;
            public int[] Fotogramas;

            /// <summary>Inicio de cada fotograma en segundos; el último valor es el final del clip.</summary>
            public float[] Tiempos;

            public bool Bucle;

            /// <summary>Parámetro float que multiplica la velocidad del estado (vacío = ninguno).</summary>
            public string ParametroVelocidad = string.Empty;

            /// <summary>Estado al que pasa solo al terminar (vacío = se queda en el último fotograma).</summary>
            public string Siguiente = string.Empty;

            public string Ruta => $"{CarpetaAnimaciones}/Kael_{Estado}.anim";
        }

        public static readonly Clip[] Clips =
        {
            new Clip
            {
                Estado = PlayerAnimacion.Idle, Hoja = HojaIdle, Fotogramas = new[] { 0, 1 },
                Tiempos = Uniformes(2, 2f), Bucle = true,
            },
            new Clip
            {
                Estado = PlayerAnimacion.Correr, Hoja = HojaCorrer, Fotogramas = Rango(0, 8),
                Tiempos = Uniformes(8, 8f / UnidadesPorZancada), Bucle = true,
                ParametroVelocidad = AnimatorParams.Movement.Nombre,
            },
            // El salto sube unos 0,4 s: los 6 primeros fotogramas lo cubren y se queda en el último.
            new Clip
            {
                Estado = PlayerAnimacion.Salto, Hoja = HojaSalto, Fotogramas = Rango(0, 6),
                Tiempos = Uniformes(6, 14f),
            },
            // Al empezar a caer, el pelo sube (fotogramas 6 a 8) y luego ondea en bucle.
            new Clip
            {
                Estado = PlayerAnimacion.Caida, Hoja = HojaSalto, Fotogramas = new[] { 6, 7, 8 },
                Tiempos = Uniformes(3, 10f), Siguiente = PlayerAnimacion.CaidaBucle,
            },
            new Clip
            {
                Estado = PlayerAnimacion.CaidaBucle, Hoja = HojaSalto, Fotogramas = new[] { 7, 8 },
                Tiempos = Uniformes(2, 6f), Bucle = true,
            },
            // Dura 1 s y PlayerAnimacion lo acelera a la duración del golpe (0,35 s el normal,
            // 0,5 s el cargado). La preparación es rápida para que el tajo (fotogramas 4 y 5)
            // se vea mientras la hitbox está abierta (0,08 a 0,22 s de 0,35 s).
            new Clip
            {
                Estado = PlayerAnimacion.Ataque, Hoja = HojaAtaque, Fotogramas = Rango(0, 7),
                Tiempos = new[] { 0f, 0.07f, 0.14f, 0.21f, 0.31f, 0.46f, 0.69f, 1f },
                ParametroVelocidad = AnimatorParams.VelocidadAtaque.Nombre,
            },
        };

        // --- Menú y modo batch ---

        [MenuItem("Fragmentum/Jugador/Importar arte de Kael y regenerar el prefab", priority = 39)]
        public static void MenuConstruir()
        {
            if (Construir())
            {
                ConstructorJugador.MenuCrearPrefab();
            }
        }

        /// <summary>
        /// Solo el arte de Kael y el prefab del jugador, sin tocar escenas:
        /// <c>-executeMethod Fragmentum.EditorTools.ConstructorArteKael.ConstruirEnModoBatch -quit</c>
        /// </summary>
        public static void ConstruirEnModoBatch()
        {
            var ok = Construir() && ConstructorJugador.CrearPrefab() != null;
            AssetDatabase.SaveAssets();
            if (!ok && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Recorta las hojas, crea los clips y arma el controlador. Devuelve false solo si algo
        /// falló; si faltan hojas no es un error: avisa y el jugador sigue con el placeholder.
        /// </summary>
        public static bool Construir()
        {
            var faltan = Hojas.Where(h => AssetDatabase.LoadAssetAtPath<Texture2D>(h.Ruta) == null).Select(h => h.Ruta).ToList();
            if (faltan.Count > 0)
            {
                Debug.LogWarning($"[Fragmentum] Faltan hojas de Kael: {string.Join(", ", faltan)}. El jugador sigue con el placeholder.");
                return true;
            }

            var sprites = new Dictionary<Hoja, Sprite[]>();
            foreach (var hoja in Hojas)
            {
                var recortes = Recortar(hoja);
                if (recortes == null)
                {
                    return false;
                }

                sprites[hoja] = recortes;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            var clips = Clips.ToDictionary(c => c.Estado, c => CrearClip(c, sprites[c.Hoja]));
            ArmarControlador(clips);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Fragmentum] Arte de Kael importado: {Hojas.Length} hojas, {Clips.Length} clips y {RutaControlador}.");
            return true;
        }

        // --- Lo que lee el constructor del prefab ---

        /// <summary>El controlador de Kael, o null si el arte todavía no se importó.</summary>
        public static RuntimeAnimatorController Controlador =>
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(RutaControlador);

        /// <summary>Fotogramas de una hoja ya recortada, en orden; vacío si no se recortó.</summary>
        public static Sprite[] Fotogramas(Hoja hoja) =>
            AssetDatabase.LoadAllAssetRepresentationsAtPath(hoja.Ruta)
                .OfType<Sprite>()
                .OrderBy(s => Indice(s.name))
                .ToArray();

        // --- 1. Recorte ---

        private static Sprite[] Recortar(Hoja hoja)
        {
            if (!(AssetImporter.GetAtPath(hoja.Ruta) is TextureImporter importador))
            {
                Debug.LogError($"[Fragmentum] {hoja.Ruta} no es una textura.");
                return null;
            }

            importador.GetSourceTextureWidthAndHeight(out var ancho, out var alto);
            if (ancho % hoja.Fotogramas != 0)
            {
                Debug.LogError($"[Fragmentum] {hoja.Ruta} mide {ancho} px de ancho: no se divide en {hoja.Fotogramas} fotogramas iguales.");
                return null;
            }

            var anchoFotograma = ancho / hoja.Fotogramas;

            // Pixel art: filtro Point, sin compresión ni mipmaps.
            var ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);
            ajustes.textureType = TextureImporterType.Sprite;
            ajustes.spriteMode = (int)SpriteImportMode.Multiple;
            ajustes.spritePixelsPerUnit = PixelesPorUnidad;
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            ajustes.spriteGenerateFallbackPhysicsShape = false;
            ajustes.filterMode = FilterMode.Point;
            ajustes.wrapMode = TextureWrapMode.Clamp;
            ajustes.mipmapEnabled = false;
            ajustes.alphaIsTransparency = true;
            importador.SetTextureSettings(ajustes);
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.maxTextureSize = Mathf.Max(2048, Mathf.NextPowerOfTwo(Mathf.Max(ancho, alto)));

            var fabrica = new SpriteDataProviderFactories();
            fabrica.Init();
            var datos = fabrica.GetSpriteEditorDataProviderFromObject(importador);
            datos.InitSpriteEditorDataProvider();

            // Se reutiliza el identificador de cada fotograma: los clips y el prefab lo referencian.
            var existentes = new Dictionary<string, GUID>();
            foreach (var r in datos.GetSpriteRects())
            {
                existentes[r.name] = r.spriteID;
            }

            var pivote = new Vector2((float)hoja.Pivote.x / anchoFotograma, (float)hoja.Pivote.y / alto);
            var rects = new SpriteRect[hoja.Fotogramas];
            for (var i = 0; i < hoja.Fotogramas; i++)
            {
                var nombre = $"{hoja.Archivo}_{i}";
                rects[i] = new SpriteRect
                {
                    name = nombre,
                    rect = new Rect(i * anchoFotograma, 0, anchoFotograma, alto),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivote,
                    spriteID = existentes.TryGetValue(nombre, out var id) ? id : GUID.Generate(),
                };
            }

            datos.SetSpriteRects(rects);
            datos.GetDataProvider<ISpriteNameFileIdDataProvider>()?
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            datos.Apply();
            importador.SaveAndReimport();

            var sprites = Fotogramas(hoja);
            if (sprites.Length != hoja.Fotogramas)
            {
                Debug.LogError($"[Fragmentum] {hoja.Ruta}: se esperaban {hoja.Fotogramas} fotogramas y hay {sprites.Length}.");
                return null;
            }

            return sprites;
        }

        // --- 2. Clips ---

        private static AnimationClip CrearClip(Clip definicion, Sprite[] fotogramas)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(definicion.Ruta);
            var nuevo = clip == null;
            if (nuevo)
            {
                clip = new AnimationClip();
            }

            clip.frameRate = 60f;

            // Una clave por fotograma y una más al final con el último, que fija cuánto dura.
            // Unity alarga los clips de sprites un fotograma (1/60 s) tras la última clave: la
            // de cierre va ese fotograma antes para que el clip dure exactamente lo indicado.
            var n = definicion.Fotogramas.Length;
            var claves = new ObjectReferenceKeyframe[n + 1];
            for (var i = 0; i <= n; i++)
            {
                claves[i] = new ObjectReferenceKeyframe
                {
                    time = i < n ? definicion.Tiempos[i] : definicion.Tiempos[n] - 1f / clip.frameRate,
                    value = fotogramas[definicion.Fotogramas[Mathf.Min(i, n - 1)]],
                };
            }

            var curva = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, curva, claves);

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

        // --- 3. Controlador ---

        private static void ArmarControlador(Dictionary<string, AnimationClip> clips)
        {
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);
            if (controlador == null)
            {
                controlador = AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);
            }

            // Los parámetros que escribe el código. Los del prototipo se conservan aunque el clip
            // ya no dependa de ellos: así el código no avisa de que faltan.
            AsegurarParametro(controlador, AnimatorParams.Movement.Nombre, AnimatorControllerParameterType.Float, 0f);
            AsegurarParametro(controlador, AnimatorParams.Attack.Nombre, AnimatorControllerParameterType.Bool, 0f);
            AsegurarParametro(controlador, AnimatorParams.Damage.Nombre, AnimatorControllerParameterType.Bool, 0f);
            AsegurarParametro(controlador, AnimatorParams.Death.Nombre, AnimatorControllerParameterType.Bool, 0f);
            AsegurarParametro(controlador, AnimatorParams.VelocidadAtaque.Nombre, AnimatorControllerParameterType.Float, 1f);

            var maquina = controlador.layers[0].stateMachine;
            var estados = new Dictionary<string, AnimatorState>();
            for (var i = 0; i < Clips.Length; i++)
            {
                var definicion = Clips[i];
                var estado = maquina.states.Select(s => s.state).FirstOrDefault(s => s.name == definicion.Estado);
                if (estado == null)
                {
                    estado = maquina.AddState(definicion.Estado, new Vector3(300f, 60f * i, 0f));
                }

                estado.motion = clips[definicion.Estado];
                estado.speedParameterActive = !string.IsNullOrEmpty(definicion.ParametroVelocidad);
                estado.speedParameter = definicion.ParametroVelocidad;
                estado.writeDefaultValues = false;
                estados[definicion.Estado] = estado;
            }

            // Fuera los estados que ya no están en la tabla.
            foreach (var sobrante in maquina.states.Select(s => s.state).Where(s => !estados.ContainsKey(s.name)).ToList())
            {
                maquina.RemoveState(sobrante);
            }

            // La única transición propia: encadenar un clip con su bucle. El resto lo decide la FSM.
            foreach (var definicion in Clips)
            {
                var estado = estados[definicion.Estado];
                var destino = string.IsNullOrEmpty(definicion.Siguiente) ? null : estados[definicion.Siguiente];
                var transiciones = estado.transitions;

                for (var i = transiciones.Length - 1; i >= 0; i--)
                {
                    if (destino == null || i > 0 || transiciones[i].destinationState != destino)
                    {
                        estado.RemoveTransition(transiciones[i]);
                    }
                }

                if (destino == null)
                {
                    continue;
                }

                var transicion = estado.transitions.Length > 0 ? estado.transitions[0] : estado.AddTransition(destino);
                transicion.hasExitTime = true;
                transicion.exitTime = 1f;
                transicion.hasFixedDuration = true;
                transicion.duration = 0f;
            }

            maquina.defaultState = estados[PlayerAnimacion.Idle];
            EditorUtility.SetDirty(controlador);
        }

        private static void AsegurarParametro(AnimatorController controlador, string nombre, AnimatorControllerParameterType tipo, float valorInicial)
        {
            var parametros = controlador.parameters;
            for (var i = 0; i < parametros.Length; i++)
            {
                if (parametros[i].name != nombre)
                {
                    continue;
                }

                if (parametros[i].type == tipo && Mathf.Approximately(parametros[i].defaultFloat, valorInicial))
                {
                    return;
                }

                controlador.RemoveParameter(i);
                break;
            }

            controlador.AddParameter(new AnimatorControllerParameter
            {
                name = nombre,
                type = tipo,
                defaultFloat = valorInicial,
            });
        }

        // --- Utilidades ---

        private static float[] Uniformes(int fotogramas, float porSegundo) =>
            Enumerable.Range(0, fotogramas + 1).Select(i => i / porSegundo).ToArray();

        private static int[] Rango(int desde, int cuantos) => Enumerable.Range(desde, cuantos).ToArray();

        private static int Indice(string nombreSprite)
        {
            var guion = nombreSprite.LastIndexOf('_');
            return guion >= 0 && int.TryParse(nombreSprite.Substring(guion + 1), out var i) ? i : 0;
        }

        /// <summary>Ruta absoluta de una hoja (para las pruebas, que leen sus píxeles).</summary>
        public static string RutaEnDisco(Hoja hoja) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", hoja.Ruta));
    }
}
