using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El Eco de la Ira, enemigo del Mundo 1, con su arte:
    /// 1. importa los 12 fotogramas de <see cref="CarpetaSprites"/> como pixel art, con el mismo
    ///    pivote en los pies para todos (si no, el Eco saltaría al cambiar de estado);
    /// 2. crea los clips Idle, Run, Attack y Death con los tiempos del diseño, los eventos de la
    ///    ventana de golpe y de la muerte, y el squash del ataque;
    /// 3. arma <c>EcoIra.controller</c> con sus transiciones;
    /// 4. monta el prefab <c>EcoIra</c>: sprite, Animator, Rigidbody2D, cuerpo, arma, contacto,
    ///    <see cref="EnemyController"/>, <see cref="EnemigoAnimacion"/> y las partículas.
    ///
    /// Idempotente: «Regenerar todo» lo ejecuta y, si nada cambió, no cambia nada.
    /// </summary>
    public static class ConstructorEcoIra
    {
        public const string CarpetaSprites = "Assets/Sprites/Enemigos/EcoIra";
        public const string CarpetaMateriales = "Assets/Sprites/Enemigos/Materiales";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Enemigos/EcoIra";
        public const string RutaControlador = CarpetaAnimaciones + "/EcoIra.controller";
        public const string RutaPrefab = GeneradorEnemigos.CarpetaPrefabs + "/" + NombreDatos + ".prefab";
        public const string NombreDatos = "EcoIra";
        public const string RutaMaterialDestello = CarpetaMateriales + "/Destello.mat";
        public const string RutaMaterialParticulas = CarpetaMateriales + "/ParticulasEco.mat";
        public const string ShaderDestello = "Fragmentum/SiluetaSprite";
        public const string ShaderParticulas = "Universal Render Pipeline/Particles/Unlit";
        public const string RutaMaterialSprite = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

        // --- Escala y pivote ---

        /// <summary>Lienzo común de los doce fotogramas (no se recorta: ahí está la alineación).</summary>
        public const int AnchoLienzo = 1229;
        public const int AltoLienzo = 905;

        /// <summary>
        /// Ancla de los pies compartida, en el píxel (424, 892) del lienzo contando desde arriba:
        /// normalizada desde abajo a la izquierda, (424 / 1229, 13 / 905). Es la misma en los doce
        /// sprites; con pivote Bottom o Center cada fotograma tendría el suyo y el Eco saltaría.
        /// </summary>
        public static readonly Vector2 Pivote = new Vector2(0.345f, 0.014f);

        /// <summary>
        /// Kael mide 113 px de arte a 64 px por unidad: 1,77 u. El Eco mide el 60 % (1,06 u) y en
        /// la pose de correr ocupa unos 750 px del lienzo: 750 / 1,06 ≈ 708 px por unidad.
        ///
        /// El arte es pixel art ampliado: cada píxel del dibujo son ~10 px de la textura. A 708
        /// PPU mide 0,014 u, casi lo mismo que un píxel de Kael (1/64 = 0,016 u), así que los dos
        /// se ven de la misma resolución.
        /// </summary>
        public const float AltoKaelPx = 113f;
        public const float FraccionDeKael = 0.6f;
        public const float AltoEcoPx = 750f;
        public const float PixelesPorUnidad = 708f;

        /// <summary>Lo que mide en el mundo un píxel del dibujo (~10 px de la textura).</summary>
        public const float PixelDeArte = 10f / PixelesPorUnidad;

        public const int TamanoMaximo = 2048;

        // --- Animaciones ---

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

            public string Ruta => $"{CarpetaAnimaciones}/{NombreDatos}_{Estado}.anim";
        }

        // Nombres de los estados del controlador.
        public const string Idle = "Idle";
        public const string Run = "Run";
        public const string Attack = "Attack";
        public const string Death = "Death";

        /// <summary>
        /// Tabla del diseño. Los tiempos del ataque no son uniformes a propósito: a1 es la
        /// anticipación larga y visible, a2 y a3 el golpe casi instantáneo. No falta nada en la
        /// muerte: D1 se descartó (estaba dibujado a otra escala) y no se rellena.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Estado = Idle, Fotogramas = new[] { "I1", "I2" }, Duraciones = new[] { 0.40f, 0.40f }, Bucle = true },
            new Clip { Estado = Run, Fotogramas = new[] { "r1", "r2", "r3", "r4" }, Duraciones = new[] { 0.12f, 0.12f, 0.12f, 0.12f }, Bucle = true },
            new Clip { Estado = Attack, Fotogramas = new[] { "a1", "a2", "a3", "a4" }, Duraciones = new[] { 0.35f, 0.08f, 0.05f, 0.25f } },
            new Clip { Estado = Death, Fotogramas = new[] { "d2", "d3" }, Duraciones = new[] { 0.15f, 0.15f } },
        };

        /// <summary>Los doce archivos, en el orden de la tabla.</summary>
        public static IEnumerable<string> Archivos => Clips.SelectMany(c => c.Fotogramas);

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        /// <summary>
        /// Squash del ataque: escala X del sprite en cada fotograma (a1 a a4). Se estira en el
        /// golpe y se encoge al recuperarse; al acabar el clip vuelve a 1.
        /// </summary>
        public static readonly float[] SquashAtaque = { 1f, 1.15f, 1f, 0.9f };

        /// <summary>
        /// Lo que avanza el cuerpo en una vuelta de r1 a r4 (un paso), medido en el arte: entre la
        /// pose con el pie de delante apoyado (r1, r2) y la de empuje (r3, r4) la cadera recorre
        /// ~300 px, y el paso entero son ~440 px (0,62 u). A ritmo 1 son 0,62 u / 0,48 s.
        /// </summary>
        public const float UnidadesPorCiclo = 0.62f;

        public static float VelocidadNaturalCarrera => UnidadesPorCiclo / ClipDe(Run).Duracion;

        // --- Geometría (u, desde los pies y mirando a la derecha), medida en el arte a 708 PPU ---

        // Cuerpo: el torso ocupa de -0,2 a 0,3 u y el Eco mide 1,06 u corriendo.
        public static readonly Vector2 TamanoCuerpo = new Vector2(0.42f, 1f);
        public static readonly Vector2 TamanoContacto = new Vector2(0.36f, 0.9f);

        // Arma: el brazo de a2 (estirado por el squash) llega a 1,29 u y el de a3 a 0,70 u, entre
        // 0,35 u y 1,1 u de altura.
        public static readonly Vector2 CentroArma = new Vector2(0.75f, 0.72f);
        public static readonly Vector2 TamanoArma = new Vector2(1f, 0.75f);

        // Cavidad del pecho tal como está dibujada en d3, el fotograma que se ve al acabar la muerte.
        public static readonly Vector2 Pecho = new Vector2(0.044f, 0.276f);

        // Partículas: brasa y ceniza, colores del propio arte.
        public static readonly Color ColorBrasa = new Color32(239, 135, 30, 255);
        public static readonly Color ColorCeniza = new Color32(73, 71, 65, 255);

        // --- Menús y modo batch ---

        [MenuItem("Fragmentum/Enemigos/Importar el arte del Eco de la Ira y regenerar su prefab", priority = 84)]
        public static void MenuConstruir()
        {
            if (Construir())
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            }
        }

        /// <summary>
        /// Solo el Eco, sin tocar escenas:
        /// <c>-executeMethod Fragmentum.EditorTools.ConstructorEcoIra.ConstruirEnModoBatch -quit</c>
        /// </summary>
        public static void ConstruirEnModoBatch()
        {
            var ok = Construir();
            AssetDatabase.SaveAssets();
            if (!ok && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Fragmentum/Enemigos/Sustituir los Soldados de lava de la escena abierta por el Eco de la Ira", priority = 85)]
        public static void MenuSustituirEnAbierta()
        {
            var escena = SceneManager.GetActiveScene();
            var cuantos = SustituirSoldadosDeLava(escena);
            if (cuantos > 0)
            {
                EditorSceneManager.MarkSceneDirty(escena);
            }

            Debug.Log($"[Fragmentum] {cuantos} Soldado(s) de lava sustituido(s) por el Eco de la Ira en '{escena.name}'.");
        }

        /// <summary>
        /// Sustituye los Soldados de lava de todas las escenas de nivel de la Ira y las guarda:
        /// <c>-executeMethod Fragmentum.EditorTools.ConstructorEcoIra.SustituirEnNivelesEnModoBatch -quit</c>
        /// </summary>
        public static void SustituirEnNivelesEnModoBatch()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            if (bd == null)
            {
                Debug.LogError("[Fragmentum] No hay LevelDatabase.");
                EditorApplication.Exit(1);
                return;
            }

            // Rutas y no objetos: abrir una escena descarga los assets cargados antes.
            var escenas = bd.niveles
                .Where(n => n != null && n.emocion != null && n.emocion.id == EMOCIONES.IRA && n.escena.Asignada)
                .Select(n => n.escena.Ruta)
                .ToList();

            foreach (var ruta in escenas)
            {
                var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                var cuantos = SustituirSoldadosDeLava(escena);
                if (cuantos > 0)
                {
                    EditorSceneManager.SaveScene(escena);
                }

                Debug.Log($"[Fragmentum] {ruta}: {cuantos} Soldado(s) de lava sustituido(s) por el Eco de la Ira.");
            }
        }

        // --- Construcción ---

        /// <summary>
        /// Importa el arte, crea clips, controlador y prefab. Devuelve false solo si algo falló;
        /// si faltan fotogramas avisa y no construye nada (el Nivel 1 sigue con lo que tenga).
        /// </summary>
        public static bool Construir()
        {
            AsegurarConfiguracion();
            var faltan = Archivos.Select(RutaSprite).Where(r => !File.Exists(r)).ToList();
            if (faltan.Count > 0)
            {
                Debug.LogWarning($"[Fragmentum] Faltan fotogramas del Eco de la Ira: {string.Join(", ", faltan)}. No se construye su prefab.");
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

            AsegurarCarpeta(CarpetaMateriales);
            AsegurarCarpeta(CarpetaAnimaciones);
            var destello = CrearMaterialDestello();
            var particulas = CrearMaterialParticulas();
            if (destello == null || particulas == null)
            {
                return false;
            }

            var clips = Clips.ToDictionary(c => c.Estado, c => CrearClip(c, sprites));
            var controlador = ArmarControlador(clips);

            GeneradorEnemigos.GenerarDatos(false);
            var ok = CrearPrefab(sprites[ClipDe(Idle).Fotogramas[0]], controlador, destello, particulas) != null;

            AssetDatabase.SaveAssets();
            if (ok)
            {
                Debug.Log($"[Fragmentum] Eco de la Ira: {sprites.Count} fotogramas, {clips.Count} clips, {RutaControlador} y {RutaPrefab}.");
            }

            return ok;
        }

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        /// <summary>El prefab del Eco, o null si todavía no se construyó.</summary>
        public static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

        // --- 1. Importación ---

        /// <summary>
        /// Ajustes de pixel art de un fotograma del Eco. Devuelve si cambió algo. Lo usan
        /// <see cref="Construir"/> y el importador de la carpeta, que los fija en cada importación.
        /// </summary>
        public static bool AjustarImportador(TextureImporter importador)
        {
            var antes = new TextureImporterSettings();
            importador.ReadTextureSettings(antes);
            var ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);

            ajustes.textureType = TextureImporterType.Sprite;
            ajustes.spriteMode = (int)SpriteImportMode.Single;
            ajustes.spritePixelsPerUnit = PixelesPorUnidad;
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
            ajustes.spritePivot = Pivote;
            ajustes.spriteGenerateFallbackPhysicsShape = false;
            ajustes.filterMode = FilterMode.Point;
            ajustes.wrapMode = TextureWrapMode.Clamp;
            ajustes.mipmapEnabled = false;
            ajustes.alphaIsTransparency = true;
            ajustes.npotScale = TextureImporterNPOTScale.None;
            ajustes.readable = false;

            var cambia = !TextureImporterSettings.Equal(antes, ajustes) ||
                         importador.textureCompression != TextureImporterCompression.Uncompressed ||
                         importador.maxTextureSize != TamanoMaximo;
            if (!cambia)
            {
                return false;
            }

            importador.SetTextureSettings(ajustes);
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.maxTextureSize = TamanoMaximo;
            return true;
        }

        // --- 2. Materiales ---

        private static Material CrearMaterialDestello()
        {
            var material = MaterialConShader(RutaMaterialDestello, ShaderDestello, out var nuevo);
            if (material == null)
            {
                return null;
            }

            material.SetColor("_Color", Color.white);
            return Guardar(material, RutaMaterialDestello, nuevo);
        }

        private static Material CrearMaterialParticulas()
        {
            var material = MaterialConShader(RutaMaterialParticulas, ShaderParticulas, out var nuevo);
            if (material == null)
            {
                return null;
            }

            // Transparente con mezcla alfa: las partículas se desvanecen al final de su vida. Sin
            // textura, cada partícula es un cuadrado liso, como un píxel del arte.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            return Guardar(material, RutaMaterialParticulas, nuevo);
        }

        private static Material MaterialConShader(string ruta, string nombreShader, out bool nuevo)
        {
            nuevo = false;
            var shader = Shader.Find(nombreShader);
            if (shader == null)
            {
                Debug.LogError($"[Fragmentum] No se encontró el shader {nombreShader}.");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            nuevo = material == null;
            if (nuevo)
            {
                material = new Material(shader);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            return material;
        }

        private static Material Guardar(Material material, string ruta, bool nuevo)
        {
            if (nuevo)
            {
                AssetDatabase.CreateAsset(material, ruta);
            }
            else
            {
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        // --- 3. Clips ---

        private static AnimationClip CrearClip(Clip definicion, IReadOnlyDictionary<string, Sprite> sprites)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(definicion.Ruta);
            var nuevo = clip == null;
            if (nuevo)
            {
                clip = new AnimationClip();
            }

            clip.frameRate = 60f;

            // Una clave por fotograma y una más al final con el último, que fija cuánto dura.
            // Unity alarga los clips de sprites un fotograma (1/60 s) tras la última clave: la de
            // cierre va ese fotograma antes para que el clip dure exactamente lo indicado.
            var n = definicion.Fotogramas.Length;
            var claves = new ObjectReferenceKeyframe[n + 1];
            for (var i = 0; i <= n; i++)
            {
                claves[i] = new ObjectReferenceKeyframe
                {
                    time = i < n ? definicion.Inicio(i) : definicion.Duracion - 1f / clip.frameRate,
                    value = sprites[definicion.Fotogramas[Mathf.Min(i, n - 1)]],
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), claves);

            var escalaX = EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalScale.x");
            AnimationUtility.SetEditorCurve(clip, escalaX, definicion.Estado == Attack ? CurvaSquash(definicion) : null);
            AnimationUtility.SetAnimationEvents(clip, Eventos(definicion));

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

        /// <summary>Escala X del sprite en escalones: cambia justo con cada fotograma.</summary>
        private static AnimationCurve CurvaSquash(Clip ataque)
        {
            var curva = new AnimationCurve();
            for (var i = 0; i < SquashAtaque.Length; i++)
            {
                curva.AddKey(ataque.Inicio(i), SquashAtaque[i]);
            }

            curva.AddKey(ataque.Duracion, 1f);
            for (var i = 0; i < curva.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curva, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curva, i, AnimationUtility.TangentMode.Constant);
            }

            return curva;
        }

        /// <summary>
        /// Eventos que recibe <see cref="EnemigoAnimacion"/>: la ventana de golpe va del primer
        /// instante de a2 al primero de a4 (solo a2 y a3 hacen daño), y cada clip sin bucle avisa
        /// al acabar.
        /// </summary>
        private static AnimationEvent[] Eventos(Clip definicion)
        {
            switch (definicion.Estado)
            {
                case Attack:
                    return new[]
                    {
                        Evento(nameof(EnemigoAnimacion.AbrirGolpe), definicion.Inicio(1)),
                        Evento(nameof(EnemigoAnimacion.CerrarGolpe), definicion.Inicio(3)),
                        Evento(nameof(EnemigoAnimacion.FinAtaque), definicion.Duracion),
                    };
                case Death:
                    return new[] { Evento(nameof(EnemigoAnimacion.FinMuerte), definicion.Duracion) };
                default:
                    return new AnimationEvent[0];
            }
        }

        private static AnimationEvent Evento(string funcion, float tiempo) =>
            new AnimationEvent { functionName = funcion, time = tiempo };

        // --- 4. Controlador ---

        /// <summary>
        /// Cuatro estados y las transiciones que piden los parámetros que ya escribe la FSM del
        /// enemigo (MovEn, Attack, Damage, Death). Todas sin Exit Time salvo Attack → Run al
        /// acabar el golpe. Además de las del diseño:
        /// - Run → Idle cuando pierde al jugador, salvo aturdido (Damage), para que los ojos no se
        ///   apaguen al recibir un golpe, y salvo atacando (Attack);
        /// - Idle → Attack si la FSM llega a ATTACK en el mismo fotograma en que detecta al jugador;
        /// - Attack → Run sin esperar si un golpe interrumpe el ataque: si no, el Eco terminaría el
        ///   golpe sin hacer daño, y eso engaña al jugador.
        /// Cualquiera → Death no puede volver a sí misma: con Death activo el clip se reiniciaría
        /// en cada fotograma y la muerte nunca acabaría.
        /// </summary>
        private static AnimatorController ArmarControlador(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador) ??
                              AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);

            AsegurarParametros(controlador,
                (AnimatorParams.MovEn.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Attack.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Damage.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Death.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.VelocidadCorrer.Nombre, AnimatorControllerParameterType.Float, 1f));

            var maquina = controlador.layers[0].stateMachine;
            var estados = new Dictionary<string, AnimatorState>();
            for (var i = 0; i < Clips.Length; i++)
            {
                var nombre = Clips[i].Estado;
                var estado = maquina.states.Select(s => s.state).FirstOrDefault(s => s.name == nombre) ??
                             maquina.AddState(nombre, new Vector3(300f + 220f * (i % 2), 80f * i, 0f));
                estado.motion = clips[nombre];
                estado.speed = 1f;
                estado.writeDefaultValues = true; // al salir de Attack el squash vuelve a 1
                estado.speedParameterActive = nombre == Run;
                estado.speedParameter = nombre == Run ? AnimatorParams.VelocidadCorrer.Nombre : string.Empty;
                estados[nombre] = estado;
            }

            foreach (var sobrante in maquina.states.Select(s => s.state).Where(s => !estados.ContainsKey(s.name)).ToList())
            {
                maquina.RemoveState(sobrante);
            }

            maquina.defaultState = estados[Idle];

            var verdadero = AnimatorConditionMode.If;
            var falso = AnimatorConditionMode.IfNot;
            // La FSM encadena transiciones en un mismo fotograma: con el jugador ya a tiro pasa de
            // IDLE a ATTACK (por CHASE) sin que el Animator llegue a ver MovEn encendido.
            Transiciones(estados[Idle],
                ("Ya a tiro", estados[Attack], false, new[] { Condicion(verdadero, AnimatorParams.Attack) }),
                ("Detecta al jugador", estados[Run], false, new[] { Condicion(verdadero, AnimatorParams.MovEn) }));
            // Atacar va primero: al entrar en ATTACK la FSM apaga MovEn, y «Lo pierde» se cumpliría
            // también si no exigiera Attack apagado.
            Transiciones(estados[Run],
                ("Entra en rango", estados[Attack], false, new[] { Condicion(verdadero, AnimatorParams.Attack) }),
                ("Lo pierde", estados[Idle], false, new[]
                {
                    Condicion(falso, AnimatorParams.MovEn), Condicion(falso, AnimatorParams.Damage), Condicion(falso, AnimatorParams.Attack),
                }));
            Transiciones(estados[Attack],
                ("Termina el golpe", estados[Run], true, new AnimatorCondition[0]),
                ("Golpe interrumpido", estados[Run], false, new[] { Condicion(falso, AnimatorParams.Attack) }));
            Transiciones(estados[Death]);

            var muerte = maquina.anyStateTransitions.FirstOrDefault(t => t.destinationState == estados[Death]) ??
                         maquina.AddAnyStateTransition(estados[Death]);
            foreach (var sobrante in maquina.anyStateTransitions.Where(t => t != muerte).ToList())
            {
                maquina.RemoveAnyStateTransition(sobrante);
            }

            Configurar(muerte, "Muere", false, new[] { Condicion(verdadero, AnimatorParams.Death) });
            muerte.canTransitionToSelf = false;

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        private static void Transiciones(AnimatorState desde, params (string nombre, AnimatorState hasta, bool alAcabar, AnimatorCondition[] condiciones)[] deseadas)
        {
            // Se reutilizan por nombre: así regenerar no cambia los identificadores del archivo.
            var actuales = desde.transitions.ToList();
            foreach (var sobrante in actuales.Where(t => deseadas.All(d => d.nombre != t.name)).ToList())
            {
                desde.RemoveTransition(sobrante);
            }

            foreach (var (nombre, hasta, alAcabar, condiciones) in deseadas)
            {
                var transicion = desde.transitions.FirstOrDefault(t => t.name == nombre) ?? desde.AddTransition(hasta);
                transicion.destinationState = hasta;
                Configurar(transicion, nombre, alAcabar, condiciones);
            }

            // El orden importa: la primera que se cumple gana.
            desde.transitions = deseadas.Select(d => desde.transitions.First(t => t.name == d.nombre)).ToArray();
        }

        private static void Configurar(AnimatorStateTransition transicion, string nombre, bool alAcabar, AnimatorCondition[] condiciones)
        {
            transicion.name = nombre;
            transicion.hasExitTime = alAcabar;
            transicion.exitTime = 1f;
            transicion.hasFixedDuration = true;
            transicion.duration = 0f;
            transicion.offset = 0f;
            transicion.interruptionSource = TransitionInterruptionSource.None;
            transicion.conditions = condiciones;
        }

        private static AnimatorCondition Condicion(AnimatorConditionMode modo, in AnimatorParams.Parametro parametro) =>
            new AnimatorCondition { mode = modo, parameter = parametro.Nombre, threshold = 0f };

        private static void AsegurarParametros(AnimatorController controlador, params (string nombre, AnimatorControllerParameterType tipo, float inicial)[] deseados)
        {
            var actuales = controlador.parameters;
            var iguales = actuales.Length == deseados.Length && actuales.Zip(deseados, (a, d) =>
                a.name == d.nombre && a.type == d.tipo && Mathf.Approximately(a.defaultFloat, d.inicial)).All(x => x);
            if (iguales)
            {
                return;
            }

            controlador.parameters = deseados.Select(d => new AnimatorControllerParameter
            {
                name = d.nombre,
                type = d.tipo,
                defaultFloat = d.inicial,
            }).ToArray();
        }

        // --- 5. Prefab ---

        private static GameObject CrearPrefab(Sprite reposo, AnimatorController controlador, Material destello, Material particulas)
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            if (datos == null)
            {
                Debug.LogError($"[Fragmentum] Falta {GeneradorEnemigos.CarpetaDatos}/{NombreDatos}.asset.");
                return null;
            }

            var raiz = new GameObject(NombreDatos) { layer = Capa(Capas.Enemy), tag = GameTags.Enemy };
            try
            {
                var cuerpoRigido = raiz.AddComponent<Rigidbody2D>();
                cuerpoRigido.gravityScale = 3f;
                cuerpoRigido.freezeRotation = true;
                cuerpoRigido.interpolation = RigidbodyInterpolation2D.Interpolate;
                cuerpoRigido.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var cuerpo = raiz.AddComponent<CapsuleCollider2D>();
                cuerpo.size = TamanoCuerpo;
                cuerpo.offset = new Vector2(0f, TamanoCuerpo.y / 2f);

                raiz.AddComponent<Health>();
                var controlador_ = raiz.AddComponent<EnemyController>();

                // Lo que se voltea al girar: el sprite, el arma y el punto del pecho.
                var pivote = new GameObject("Pivote").transform;
                pivote.SetParent(raiz.transform, false);

                var objetoSprite = new GameObject("Sprite") { layer = raiz.layer };
                objetoSprite.transform.SetParent(pivote, false);
                var sprite = objetoSprite.AddComponent<SpriteRenderer>();
                sprite.sprite = reposo;
                sprite.sortingOrder = 5;
                sprite.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialSprite);

                var animator = objetoSprite.AddComponent<Animator>();
                animator.runtimeAnimatorController = controlador;
                animator.applyRootMotion = false;
                // Fuera de cámara también: si no, los eventos del golpe y de la muerte no llegan.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                var estallido = CrearEstallido(pivote, raiz.layer, particulas);

                var animacion = objetoSprite.AddComponent<EnemigoAnimacion>();
                Asignar(animacion, "velocidadNaturalCarrera", p => p.floatValue = VelocidadNaturalCarrera);
                Asignar(animacion, "materialDestello", p => p.objectReferenceValue = destello);
                Asignar(animacion, "estallido", p => p.objectReferenceValue = estallido);

                // Arma: solo golpea a la capa Player, y solo entre los eventos de a2 y a4.
                var objetoArma = new GameObject("Arma") { layer = Capa(Capas.EnemyWeapon) };
                objetoArma.transform.SetParent(pivote, false);
                objetoArma.transform.localPosition = CentroArma;
                var zonaArma = objetoArma.AddComponent<BoxCollider2D>();
                zonaArma.isTrigger = true;
                zonaArma.size = TamanoArma;
                var arma = objetoArma.AddComponent<Hitbox>();
                Asignar(arma, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
                objetoArma.SetActive(false);

                // Contacto: el Eco no hace daño por contacto (danioContacto = 0 lo apaga al
                // arrancar), pero se deja montado para los Ecos que sí lo hagan.
                var objetoContacto = new GameObject("Contacto") { layer = Capa(Capas.EnemyWeapon) };
                objetoContacto.transform.SetParent(raiz.transform, false);
                var zonaContacto = objetoContacto.AddComponent<CapsuleCollider2D>();
                zonaContacto.isTrigger = true;
                zonaContacto.size = TamanoContacto;
                zonaContacto.offset = new Vector2(0f, TamanoContacto.y / 2f);
                var contacto = objetoContacto.AddComponent<Hitbox>();
                Asignar(contacto, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
                Asignar(contacto, "golpearUnaVezPorActivacion", p => p.boolValue = false);

                Asignar(controlador_, "datos", p => p.objectReferenceValue = datos);
                Asignar(controlador_, "animator", p => p.objectReferenceValue = animator);
                Asignar(controlador_, "pivote", p => p.objectReferenceValue = pivote);
                Asignar(controlador_, "sprite", p => p.objectReferenceValue = sprite);
                Asignar(controlador_, "arma", p => p.objectReferenceValue = arma);
                Asignar(controlador_, "contacto", p => p.objectReferenceValue = contacto);
                Asignar(controlador_, "animacion", p => p.objectReferenceValue = animacion);

                var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {RutaPrefab}.");
                    return null;
                }

                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        /// <summary>
        /// Brasas y ceniza que salen de la cavidad del pecho al acabar la muerte: cuadrados de 2 a
        /// 3 píxeles del Eco, en una sola ráfaga.
        /// </summary>
        private static ParticleSystem CrearEstallido(Transform pivote, int capa, Material material)
        {
            var objeto = new GameObject("Pecho") { layer = capa };
            objeto.transform.SetParent(pivote, false);
            objeto.transform.localPosition = Pecho;

            var sistema = objeto.AddComponent<ParticleSystem>();
            sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var principal = sistema.main;
            principal.duration = 0.5f;
            principal.loop = false;
            principal.playOnAwake = false;
            principal.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
            principal.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
            principal.startSize = new ParticleSystem.MinMaxCurve(2f * PixelDeArte, 3f * PixelDeArte);
            principal.startColor = new ParticleSystem.MinMaxGradient(ColorBrasa, ColorCeniza);
            principal.gravityModifier = 0.12f;
            principal.simulationSpace = ParticleSystemSimulationSpace.World;
            principal.maxParticles = 40;
            principal.stopAction = ParticleSystemStopAction.Destroy;

            var emision = sistema.emission;
            emision.rateOverTime = 0f;
            emision.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

            // Un abanico hacia arriba en el plano XY (de 10° a 170°): el cuerpo ya está en el
            // suelo, y las partículas no tienen colisión; hacia abajo lo atravesarían.
            var forma = sistema.shape;
            forma.shapeType = ParticleSystemShapeType.Circle;
            forma.radius = 0.06f;
            forma.arc = 160f;
            forma.rotation = new Vector3(0f, 0f, 10f);

            var color = sistema.colorOverLifetime;
            color.enabled = true;
            var desvanecer = new Gradient();
            desvanecer.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = desvanecer;

            var dibujo = objeto.GetComponent<ParticleSystemRenderer>();
            dibujo.sharedMaterial = material;
            dibujo.renderMode = ParticleSystemRenderMode.Billboard;
            dibujo.sortingOrder = 6;
            return sistema;
        }

        // --- 6. Escenas ---

        /// <summary>
        /// Sustituye cada Soldado de lava (el enemigo provisional de la Ira, sin arte) por un Eco en
        /// la misma posición, con la misma ruta de patrulla. Devuelve cuántos sustituyó.
        /// </summary>
        public static int SustituirSoldadosDeLava(Scene escena)
        {
            var eco = Prefab;
            var soldado = GeneradorEnemigos.CargarPrefab("SoldadoLava");
            if (eco == null || soldado == null)
            {
                Debug.LogWarning("[Fragmentum] Falta el prefab del Eco de la Ira o el del Soldado de lava: no se sustituye nada.");
                return 0;
            }

            var rutaSoldado = AssetDatabase.GetAssetPath(soldado);
            var soldados = escena.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<EnemyController>(true))
                .Where(e => PrefabUtility.IsOutermostPrefabInstanceRoot(e.gameObject) &&
                            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(e.gameObject) == rutaSoldado)
                .ToList();

            foreach (var viejo in soldados)
            {
                var nuevo = (GameObject)PrefabUtility.InstantiatePrefab(eco, escena);
                nuevo.transform.SetParent(viejo.transform.parent, false);
                nuevo.transform.SetSiblingIndex(viejo.transform.GetSiblingIndex());
                nuevo.transform.position = viejo.transform.position;

                var nombreViejo = viejo.name;
                nuevo.name = nombreViejo.Replace("Enemigo_SoldadoLava", NombreDatos);

                // La misma ruta, con los puntos renombrados para que se sepa de quién son.
                var ruta = new SerializedObject(viejo).FindProperty("waypoints");
                var puntos = Enumerable.Range(0, ruta.arraySize)
                    .Select(i => ruta.GetArrayElementAtIndex(i).objectReferenceValue as Transform)
                    .ToArray();
                foreach (var punto in puntos.Where(p => p != null && p.name.StartsWith(nombreViejo)))
                {
                    punto.name = nuevo.name + punto.name.Substring(nombreViejo.Length);
                }

                Asignar(nuevo.GetComponent<EnemyController>(), "waypoints", p =>
                {
                    p.arraySize = puntos.Length;
                    for (var i = 0; i < puntos.Length; i++)
                    {
                        p.GetArrayElementAtIndex(i).objectReferenceValue = puntos[i];
                    }
                });

                Object.DestroyImmediate(viejo.gameObject);
            }

            return soldados.Count;
        }
    }
}
