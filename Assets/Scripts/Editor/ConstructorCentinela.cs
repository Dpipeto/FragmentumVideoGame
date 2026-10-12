using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;
using static Fragmentum.EditorTools.UtilidadesEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El arte de la Centinela Ardiente, jefe del Mundo 1:
    /// 1. importa los 29 fotogramas de <see cref="CarpetaSprites"/> como pixel art, con el mismo
    ///    lienzo y el mismo pivote en los pies para todos (si no, saltaría al cambiar de clip); los
    ///    ajustes están en <see cref="TablaArte"/>, con las constantes de aquí;
    /// 2. crea los 8 clips desde <see cref="Clips"/>, con los tiempos del diseño y el forcejeo de
    ///    la espada clavada;
    /// 3. arma su controlador (<see cref="RutaControlador"/>), con un estado por clip;
    /// 4. viste el prefab del jefe (<see cref="Vestir"/>, lo llama <c>GeneradorJefes</c>): los
    ///    fotogramas, el Animator, <c>AnimacionJefe</c> y el fragmento de la ceniza de D4.
    ///
    /// El arte llega limpio y alineado desde <c>Tesis/Jefe1 animaciones/limpios</c>, que tiene su
    /// LEEME. Los PNG no se retocan: cualquier recorte o reescalado rompe el anclaje común.
    ///
    /// Idempotente: «Regenerar todo» lo ejecuta y, si nada cambió, no cambia nada.
    /// </summary>
    public static class ConstructorCentinela
    {
        public const string CarpetaSprites = TablaArte.CarpetaJefes + "/Centinela";
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

        /// <summary>
        /// Un evento de animación: la función de <see cref="AnimacionJefe"/> a la que llama al
        /// empezar un fotograma.
        /// </summary>
        public sealed class Evento
        {
            public string Estado;
            public string Fotograma;
            public string Funcion;

            /// <summary>Lleva como parámetro lo que dura el fotograma: las ventanas duran lo que se ve.</summary>
            public bool ConDuracion;

            /// <summary>Segundos antes del inicio del fotograma (EnElAire: el último instante de J4).</summary>
            public float Antes;
        }

        /// <summary>
        /// Los momentos del dibujo que marcan el combate:
        /// - Golpe: E2 (el tajo), G3 (la onda) y J2 (el despegue). Ahí sale la zona de daño.
        /// - FinGolpe: E3 o C1, cuando acaba el tajo; su zona se apaga.
        /// - VentanaCorta (E3) y Ventana (C2): daño doble lo que dura el fotograma.
        /// - EnElAire: al acabar J4, si el cuerpo aún vuela, J4 se sostiene hasta tocar el suelo.
        /// - Aterrizaje (J5) y FinMuerte (D4).
        /// </summary>
        public static readonly Evento[] Eventos =
        {
            new Evento { Estado = Espadazo, Fotograma = "E2", Funcion = nameof(AnimacionJefe.Golpe) },
            new Evento { Estado = Espadazo, Fotograma = "E3", Funcion = nameof(AnimacionJefe.FinGolpe) },
            new Evento { Estado = Espadazo, Fotograma = "E3", Funcion = nameof(AnimacionJefe.VentanaCorta), ConDuracion = true },
            new Evento { Estado = EspadaClavada, Fotograma = "C1", Funcion = nameof(AnimacionJefe.FinGolpe) },
            new Evento { Estado = EspadaClavada, Fotograma = "C2", Funcion = nameof(AnimacionJefe.Ventana), ConDuracion = true },
            new Evento { Estado = GolpeSuelo, Fotograma = "G3", Funcion = nameof(AnimacionJefe.Golpe) },
            new Evento { Estado = Salto, Fotograma = "J2", Funcion = nameof(AnimacionJefe.Golpe) },
            new Evento { Estado = Salto, Fotograma = "J5", Funcion = nameof(AnimacionJefe.EnElAire), Antes = SeQueda },
            new Evento { Estado = Salto, Fotograma = "J5", Funcion = nameof(AnimacionJefe.Aterrizaje) },
            new Evento { Estado = Muerte, Fotograma = "D4", Funcion = nameof(AnimacionJefe.FinMuerte) },
        };

        /// <summary>Instante del evento dentro de su clip.</summary>
        public static float Instante(Evento evento)
        {
            var clip = ClipDe(evento.Estado);
            return clip.Inicio(System.Array.IndexOf(clip.Fotogramas, evento.Fotograma)) - evento.Antes;
        }

        /// <summary>Parámetro del evento: lo que dura su fotograma si es una ventana; si no, 0.</summary>
        public static float Parametro(Evento evento)
        {
            if (!evento.ConDuracion)
            {
                return 0f;
            }

            var clip = ClipDe(evento.Estado);
            return clip.Duraciones[System.Array.IndexOf(clip.Fotogramas, evento.Fotograma)];
        }

        /// <summary>
        /// Anticipación de cada ataque: del inicio de su clip a su evento Golpe. La usa
        /// <see cref="AnimacionJefe"/> para empezar el clip ya avanzado cuando el ataque va más
        /// rápido que el dibujo (los espadazos encadenados).
        /// </summary>
        public static (string estado, float segundos)[] Anticipaciones =>
            Eventos.Where(e => e.Funcion == nameof(AnimacionJefe.Golpe)).Select(e => (e.Estado, Instante(e))).ToArray();

        // --- Menús y modo batch ---

        [MenuItem("Fragmentum/Jefes/Generar los clips y el Animator de la Centinela", priority = 142)]
        public static void MenuConstruir()
        {
            // El prefab del jefe se viste con lo que se acaba de generar.
            if (Construir() && GeneradorJefes.CrearPrefabs())
            {
                EditorGUIUtility.PingObject(Controlador);
            }
        }

        // --- Construcción ---

        /// <summary>
        /// Fija los ajustes de importación y crea los clips y el controlador. Devuelve false solo si
        /// algo falló; si faltan fotogramas avisa y no construye nada.
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
                if (AssetImporter.GetAtPath(ruta) is TextureImporter importador && TablaArte.Ajustar(importador))
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
            var clips = Clips.ToDictionary(c => c.Estado, c => CrearClip(c, sprites));
            ArmarControlador(clips);
            AjustarDatos();

            AssetDatabase.SaveAssets();
            Debug.Log($"[Fragmentum] Centinela Ardiente: {sprites.Count} fotogramas, {Clips.Length} clips y su controlador en {CarpetaAnimaciones}.");
            return true;
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

            AnimationUtility.SetAnimationEvents(clip, Eventos
                .Where(e => e.Estado == definicion.Estado)
                .Select(e => new AnimationEvent { time = Instante(e), functionName = e.Funcion, floatParameter = Parametro(e) })
                .ToArray());

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

        // --- 3. Controlador ---

        public const string RutaControlador = CarpetaAnimaciones + "/CentinelaArdiente.controller";

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El fotograma en el que el espadazo se convierte en espada clavada, si toca.</summary>
        public const string FotogramaClavar = "E3";

        private static readonly Dictionary<string, Vector3> PosicionEstados = new Dictionary<string, Vector3>
        {
            [Idle] = new Vector3(300f, 120f, 0f),
            [Andar] = new Vector3(300f, 240f, 0f),
            [Espadazo] = new Vector3(580f, 0f, 0f),
            [EspadaClavada] = new Vector3(860f, 0f, 0f),
            [GolpeSuelo] = new Vector3(580f, 100f, 0f),
            [Salto] = new Vector3(580f, 200f, 0f),
            [Rugido] = new Vector3(580f, 300f, 0f),
            [Muerte] = new Vector3(860f, 300f, 0f),
        };

        /// <summary>
        /// Un estado por clip. Lo maneja el código del jefe (fase D) con estos parámetros:
        /// - Walk: Idle ↔ Andar mientras se mueve.
        /// - Espadazo, GolpeSuelo, Salto y Rugido (disparadores): empiezan su clip al momento, desde
        ///   cualquier estado. En el combate actual los ataques se encadenan (combos de espadazos,
        ///   ondas dobles) y el golpe tiene que coincidir con su zona de daño: no pueden esperar a que
        ///   acabe el clip anterior. El rugido interrumpe lo que esté haciendo.
        /// - Clavar: el espadazo acaba con la espada clavada (C1-C4) en vez de recuperarse (E3-E4).
        ///   Se fija al empezar el espadazo y el cambio se hace justo al acabar E2.
        /// - Death: muere desde cualquier estado y se queda en D4: Muerte no tiene salida.
        /// Cada ataque vuelve a Idle al acabar, o a Andar si ya camina (para no enseñar un fotograma
        /// del idle frontal). Ninguna transición mezcla (duración 0): de E4 a Idle sale sin corte.
        /// </summary>
        private static AnimatorController ArmarControlador(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            var controlador = Controlador ?? AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);

            AsegurarParametros(controlador,
                (AnimatorParams.Walk.Nombre, AnimatorControllerParameterType.Bool),
                (AnimatorParams.Espadazo.Nombre, AnimatorControllerParameterType.Trigger),
                (AnimatorParams.Clavar.Nombre, AnimatorControllerParameterType.Bool),
                (AnimatorParams.GolpeSuelo.Nombre, AnimatorControllerParameterType.Trigger),
                (AnimatorParams.Salto.Nombre, AnimatorControllerParameterType.Trigger),
                (AnimatorParams.Rugido.Nombre, AnimatorControllerParameterType.Trigger),
                (AnimatorParams.Death.Nombre, AnimatorControllerParameterType.Bool));

            var maquina = controlador.layers[0].stateMachine;
            var estados = new Dictionary<string, AnimatorState>();
            foreach (var definicion in Clips)
            {
                var nombre = definicion.Estado;
                var estado = maquina.states.Select(s => s.state).FirstOrDefault(s => s.name == nombre) ??
                             maquina.AddState(nombre, PosicionEstados[nombre]);
                estado.motion = clips[nombre];
                estado.speed = 1f;
                estado.speedParameterActive = false;
                estado.writeDefaultValues = true; // al salir de EspadaClavada el forcejeo vuelve a 0
                estados[nombre] = estado;
            }

            foreach (var sobrante in maquina.states.Select(s => s.state).Where(s => !estados.ContainsKey(s.name)).ToList())
            {
                maquina.RemoveState(sobrante);
            }

            maquina.defaultState = estados[Idle];
            maquina.anyStatePosition = new Vector3(40f, 0f, 0f);
            maquina.entryPosition = new Vector3(40f, 120f, 0f);
            maquina.exitPosition = new Vector3(1100f, 120f, 0f);

            var si = AnimatorConditionMode.If;
            var no = AnimatorConditionMode.IfNot;
            var anda = new[] { Condicion(si, AnimatorParams.Walk) };
            var clavada = ClipDe(Espadazo);
            var finE2 = clavada.Inicio(System.Array.IndexOf(clavada.Fotogramas, FotogramaClavar)) / clavada.Duracion;

            Transiciones(estados[Idle], ("Echa a andar", estados[Andar], null, anda));
            Transiciones(estados[Andar], ("Se para", estados[Idle], null, new[] { Condicion(no, AnimatorParams.Walk) }));
            Transiciones(estados[Espadazo],
                ("Clava la espada", estados[EspadaClavada], finE2, new[] { Condicion(si, AnimatorParams.Clavar) }),
                ("Acaba andando", estados[Andar], 1f, anda),
                ("Acaba", estados[Idle], 1f, new AnimatorCondition[0]));
            foreach (var ataque in new[] { EspadaClavada, GolpeSuelo, Salto, Rugido })
            {
                Transiciones(estados[ataque],
                    ("Acaba andando", estados[Andar], 1f, anda),
                    ("Acaba", estados[Idle], 1f, new AnimatorCondition[0]));
            }

            Transiciones(estados[Muerte]);

            var vivo = Condicion(no, AnimatorParams.Death);
            DesdeCualquierEstado(maquina,
                ("Muere", estados[Muerte], false, new[] { Condicion(si, AnimatorParams.Death) }),
                ("Ruge", estados[Rugido], false, new[] { Condicion(si, AnimatorParams.Rugido), vivo }),
                ("Espadazo", estados[Espadazo], true, new[] { Condicion(si, AnimatorParams.Espadazo), vivo }),
                ("Golpea el suelo", estados[GolpeSuelo], true, new[] { Condicion(si, AnimatorParams.GolpeSuelo), vivo }),
                ("Salta", estados[Salto], false, new[] { Condicion(si, AnimatorParams.Salto), vivo }));

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        // Se reutilizan por nombre: así regenerar no cambia los identificadores del archivo.
        // salida = null: sin tiempo de salida; si no, el tiempo normalizado del clip.
        private static void Transiciones(AnimatorState desde, params (string nombre, AnimatorState hasta, float? salida, AnimatorCondition[] condiciones)[] deseadas)
        {
            foreach (var sobrante in desde.transitions.Where(t => deseadas.All(d => d.nombre != t.name)).ToList())
            {
                desde.RemoveTransition(sobrante);
            }

            foreach (var (nombre, hasta, salida, condiciones) in deseadas)
            {
                var transicion = desde.transitions.FirstOrDefault(t => t.name == nombre) ?? desde.AddTransition(hasta);
                transicion.destinationState = hasta;
                Configurar(transicion, nombre, salida, condiciones);
            }

            // El orden importa: la primera que se cumple gana.
            desde.transitions = deseadas.Select(d => desde.transitions.First(t => t.name == d.nombre)).ToArray();
        }

        private static void DesdeCualquierEstado(AnimatorStateMachine maquina, params (string nombre, AnimatorState hasta, bool repetir, AnimatorCondition[] condiciones)[] deseadas)
        {
            foreach (var sobrante in maquina.anyStateTransitions.Where(t => deseadas.All(d => d.nombre != t.name)).ToList())
            {
                maquina.RemoveAnyStateTransition(sobrante);
            }

            foreach (var (nombre, hasta, repetir, condiciones) in deseadas)
            {
                var transicion = maquina.anyStateTransitions.FirstOrDefault(t => t.name == nombre) ?? maquina.AddAnyStateTransition(hasta);
                transicion.destinationState = hasta;
                Configurar(transicion, nombre, null, condiciones);
                transicion.canTransitionToSelf = repetir;
            }

            // La muerte va la primera: gana a cualquier disparador del mismo fotograma.
            maquina.anyStateTransitions = deseadas.Select(d => maquina.anyStateTransitions.First(t => t.name == d.nombre)).ToArray();
        }

        private static void Configurar(AnimatorStateTransition transicion, string nombre, float? salida, AnimatorCondition[] condiciones)
        {
            transicion.name = nombre;
            transicion.hasExitTime = salida.HasValue;
            transicion.exitTime = salida ?? 1f;
            transicion.hasFixedDuration = true;
            transicion.duration = 0f;
            transicion.offset = 0f;
            transicion.interruptionSource = TransitionInterruptionSource.None;
            transicion.conditions = condiciones;
        }

        private static AnimatorCondition Condicion(AnimatorConditionMode modo, in AnimatorParams.Parametro parametro) =>
            new AnimatorCondition { mode = modo, parameter = parametro.Nombre, threshold = 0f };

        private static void AsegurarParametros(AnimatorController controlador, params (string nombre, AnimatorControllerParameterType tipo)[] deseados)
        {
            var actuales = controlador.parameters;
            if (actuales.Length == deseados.Length && actuales.Zip(deseados, (a, d) => a.name == d.nombre && a.type == d.tipo).All(x => x))
            {
                return;
            }

            controlador.parameters = deseados.Select(d => new AnimatorControllerParameter { name = d.nombre, type = d.tipo }).ToArray();
        }

        // --- 4. Datos y prefab ---

        /// <summary>Nombre del asset de datos y del prefab del jefe.</summary>
        public const string NombreDatos = "CentinelaArdiente";

        /// <summary>Ancho del trigger del fragmento: desde el suelo hasta él, basta con entrar en la ceniza.</summary>
        public const float AnchoRecogida = 1f;

        /// <summary>
        /// El arte manda sobre dos datos del jefe: sin tinte (los colores son los del dibujo) y la
        /// derrota dura lo que tarda la muerte en llegar a D4.
        /// </summary>
        public static float DuracionDerrota => ClipDe(Muerte).Inicio(ClipDe(Muerte).Fotogramas.Length - 1);

        private static void AjustarDatos()
        {
            var datos = GeneradorJefes.CargarDatos(NombreDatos);
            if (datos == null || (datos.tinte == Color.white && Mathf.Approximately(datos.duracionDerrota, DuracionDerrota)))
            {
                return;
            }

            datos.tinte = Color.white;
            datos.duracionDerrota = DuracionDerrota;
            EditorUtility.SetDirty(datos);
        }

        /// <summary>
        /// Viste el prefab de la Centinela con su arte; lo llama <c>GeneradorJefes</c> al crearlo.
        /// El sprite va a altura 0 dentro del pivote (el forcejeo fija esa altura) y con escala 1:
        /// el tamaño lo dan los píxeles por unidad. Devuelve false si el arte no está generado:
        /// entonces el jefe se queda con su cápsula provisional.
        /// </summary>
        public static bool Vestir(GameObject objetoSprite, Transform pivote, BossController jefe)
        {
            var controlador = Controlador;
            var reposo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite(ClipDe(Idle).Fotogramas[0]));
            if (controlador == null || reposo == null)
            {
                return false;
            }

            objetoSprite.transform.localPosition = Vector3.zero;
            objetoSprite.transform.localScale = Vector3.one;

            var sprite = objetoSprite.GetComponent<SpriteRenderer>();
            sprite.sprite = reposo;
            sprite.color = Color.white;

            var animator = objetoSprite.GetComponent<Animator>();
            animator.runtimeAnimatorController = controlador;
            animator.applyRootMotion = false;
            // Los eventos marcan el combate: tienen que llegar aunque el jefe esté fuera de cámara.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var arte = objetoSprite.AddComponent<AnimacionJefe>();
            var anticipaciones = Anticipaciones;
            Asignar(arte, "anticipaciones", p =>
            {
                p.arraySize = anticipaciones.Length;
                for (var i = 0; i < anticipaciones.Length; i++)
                {
                    var elemento = p.GetArrayElementAtIndex(i);
                    elemento.FindPropertyRelative("estado").stringValue = anticipaciones[i].estado;
                    elemento.FindPropertyRelative("segundos").floatValue = anticipaciones[i].segundos;
                }
            });
            var destello = AssetDatabase.LoadAssetAtPath<Material>(ConstructorEcoIra.RutaMaterialDestello);
            if (destello == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {ConstructorEcoIra.RutaMaterialDestello}: la Centinela no destellará al recibir golpes.");
            }

            Asignar(arte, "materialDestello", p => p.objectReferenceValue = destello);
            Asignar(jefe, "arte", p => p.objectReferenceValue = arte);

            var fragmento = CrearFragmento(pivote, sprite);
            Asignar(jefe, "fragmento", p => p.objectReferenceValue = fragmento);
            return true;
        }

        /// <summary>
        /// El fragmento que flota sobre la ceniza en D4, medido en el dibujo: las filas opacas de
        /// arriba, separadas de la ceniza por al menos una fila vacía. Devuelve su centro y su
        /// tamaño en unidades, respecto del pivote (los pies), o null si no hay fragmento separado.
        /// </summary>
        public static (Vector2 centro, Vector2 tamano)? MedirFragmento() =>
            MedirFragmento(RutaSprite(ClipDe(Muerte).Fotogramas.Last()), Pivote, PixelesPorUnidad);

        /// <summary>
        /// Lo mismo en el último fotograma de la muerte de cualquier jefe: <paramref name="ruta"/> es
        /// su PNG, con el pivote y los píxeles por unidad de su regla.
        /// </summary>
        public static (Vector2 centro, Vector2 tamano)? MedirFragmento(string ruta, Vector2 pivoteNormalizado, float pixelesPorUnidad)
        {
            var textura = new Texture2D(2, 2);
            try
            {
                if (!File.Exists(ruta) || !textura.LoadImage(File.ReadAllBytes(ruta)))
                {
                    return null;
                }

                var ancho = textura.width;
                var alto = textura.height;
                var pixeles = textura.GetPixels32(); // fila 0 = la de abajo
                bool Opaca(int fila) => Enumerable.Range(0, ancho).Any(x => pixeles[fila * ancho + x].a > 0);

                // De arriba abajo: el primer bloque de filas opacas es el fragmento.
                var arriba = alto - 1;
                while (arriba >= 0 && !Opaca(arriba)) arriba--;
                var abajo = arriba;
                while (abajo > 0 && Opaca(abajo - 1)) abajo--;
                var hayCeniza = Enumerable.Range(0, Mathf.Max(0, abajo - 1)).Any(Opaca);
                if (arriba < 0 || !hayCeniza)
                {
                    return null;
                }

                int izquierda = ancho, derecha = -1;
                for (var y = abajo; y <= arriba; y++)
                {
                    for (var x = 0; x < ancho; x++)
                    {
                        if (pixeles[y * ancho + x].a > 0)
                        {
                            izquierda = Mathf.Min(izquierda, x);
                            derecha = Mathf.Max(derecha, x);
                        }
                    }
                }

                var pivote = new Vector2(pivoteNormalizado.x * ancho, pivoteNormalizado.y * alto);
                var centro = (new Vector2(izquierda + derecha + 1, abajo + arriba + 1) * 0.5f - pivote) / pixelesPorUnidad;
                var tamano = new Vector2(derecha - izquierda + 1, arriba - abajo + 1) / pixelesPorUnidad;
                return (centro, tamano);
            }
            finally
            {
                Object.DestroyImmediate(textura);
            }
        }

        // El trigger va del suelo al fragmento y la máscara (apagada) lo tapa al recogerlo.
        private static FragmentoJefe CrearFragmento(Transform pivote, SpriteRenderer dibujo)
        {
            var medida = MedirFragmento();
            if (medida == null)
            {
                Debug.LogWarning("[Fragmentum] No se encontró el fragmento en D4: la Centinela no dejará fragmento que recoger.");
                return null;
            }

            return CrearFragmento(pivote, dibujo, medida.Value, PixelesPorUnidad);
        }

        /// <summary>
        /// El fragmento de cualquier jefe, con la medida de <see cref="MedirFragmento(string, Vector2, float)"/>.
        /// </summary>
        public static FragmentoJefe CrearFragmento(Transform pivote, SpriteRenderer dibujo, (Vector2 centro, Vector2 tamano) medida, float pixelesPorUnidad)
        {
            var (centro, tamano) = medida;
            var objeto = new GameObject("Fragmento"); // capa Default: la toca el jugador
            objeto.transform.SetParent(pivote, false);
            objeto.transform.localPosition = new Vector3(centro.x, 0f, 0f);

            var cima = centro.y + tamano.y * 0.5f;
            var zona = objeto.AddComponent<BoxCollider2D>();
            zona.isTrigger = true;
            zona.size = new Vector2(AnchoRecogida, cima);
            zona.offset = new Vector2(0f, cima * 0.5f);

            var objetoMascara = new GameObject("Mascara");
            objetoMascara.transform.SetParent(objeto.transform, false);
            objetoMascara.transform.localPosition = new Vector3(0f, centro.y, 0f);
            var mascara = objetoMascara.AddComponent<SpriteMask>();
            mascara.sprite = SpriteCuadrado;
            mascara.enabled = false;
            if (mascara.sprite != null)
            {
                // Un píxel de margen alrededor del fragmento; la ceniza queda 20 px más abajo.
                var margen = 2f / pixelesPorUnidad;
                var medidaSprite = mascara.sprite.bounds.size;
                objetoMascara.transform.localScale = new Vector3((tamano.x + margen) / medidaSprite.x, (tamano.y + margen) / medidaSprite.y, 1f);
            }

            var fragmento = objeto.AddComponent<FragmentoJefe>();
            Asignar(fragmento, "dibujo", p => p.objectReferenceValue = dibujo);
            Asignar(fragmento, "mascara", p => p.objectReferenceValue = mascara);
            objeto.SetActive(false); // aparece al llegar la muerte a D4
            return fragmento;
        }
    }
}
