using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El Lamento, el Eco de la Tristeza, con su arte: el mismo cuerpo que el Eco de la Ira,
    /// empapado. Cada fotograma se hizo editando el del Eco, así que entra con sus mismos estados
    /// (Idle, Run, Attack y Death) y su misma geometría. Lo que cambia:
    ///
    /// - <b>Cada fotograma lleva su pivote.</b> Los del Eco comparten lienzo y ancla; los del
    ///   Lamento llegaron cada uno colocado a su manera, y los cuatro del ataque, además, en otro
    ///   lienzo y con otro tamaño de píxel. El ancla de cada uno (<see cref="Fotogramas"/>) se midió
    ///   encajando su silueta sobre la del fotograma del Eco: así los pies caen donde caían.
    /// - <b>Es más lento:</b> la anticipación del zarpazo dura 0,45 s (la del Eco, 0,22 s) y el
    ///   paso, 0,64 s. Son los tiempos de sus datos (<c>Lamento.asset</c>).
    /// - <b>Pesa:</b> lleva <see cref="Pesar"/> en el arma y en el cuerpo.
    /// - Al morir se deshace en agua: las partículas son gotas, no brasas.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo2/Lamento</c> (con su LEEME) y no se
    /// retocan. Idempotente: «Regenerar todo» lo ejecuta y, si nada cambió, no cambia nada.
    /// </summary>
    public static class ConstructorLamento
    {
        public const string CarpetaSprites = "Assets/Sprites/Enemigos/Lamento";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Enemigos/Lamento";
        public const string NombreDatos = "Lamento";
        public const string RutaControlador = CarpetaAnimaciones + "/Lamento.controller";
        public const string RutaPrefab = GeneradorEnemigos.CarpetaPrefabs + "/" + NombreDatos + ".prefab";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        // --- Escala y pivotes ---

        /// <summary>
        /// Los ocho fotogramas que comparten lienzo (82×91) encajan sobre los del Eco a 69 píxeles
        /// por unidad: el Lamento mide lo mismo que él, 1,24 u de pie.
        /// </summary>
        public const float PixelesPorUnidad = 69f;

        /// <summary>Un píxel del dibujo, en unidades.</summary>
        public const float PixelDeArte = 1f / PixelesPorUnidad;

        /// <summary>Un fotograma: su lienzo, su escala y dónde tiene el ancla de los pies.</summary>
        public sealed class Fotograma
        {
            public readonly string Nombre;
            public readonly Vector2Int Lienzo;
            public readonly float PixelesPorUnidad;

            /// <summary>El ancla, en píxeles desde la esquina de arriba a la izquierda.</summary>
            public readonly Vector2 Ancla;

            public Fotograma(string nombre, int ancho, int alto, float ppu, float anclaX, float anclaY)
            {
                Nombre = nombre;
                Lienzo = new Vector2Int(ancho, alto);
                PixelesPorUnidad = ppu;
                Ancla = new Vector2(anclaX, anclaY);
            }

            /// <summary>El pivote de Unity: normalizado desde abajo a la izquierda.</summary>
            public Vector2 Pivote => new Vector2(Ancla.x / Lienzo.x, (Lienzo.y - Ancla.y) / Lienzo.y);
        }

        /// <summary>
        /// Medido encajando cada silueta sobre la del Eco (el ancla del Eco está en el píxel
        /// (424, 892) de su lienzo de 1229×905). En vertical no se usa el encaje sino el suelo: la
        /// fila 86 en los ocho que comparten lienzo (la limpieza los ancló por los pies) y el borde
        /// de abajo del dibujo en los cuatro del ataque.
        ///
        /// Los del ataque llegaron con otro tamaño de píxel: a su escala miden lo mismo que el
        /// resto, pero su píxel se ve hasta un 15 % más fino.
        /// </summary>
        public static readonly Fotograma[] Fotogramas =
        {
            new Fotograma("I1", 82, 91, PixelesPorUnidad, 41.1f, 86f),
            new Fotograma("I2", 82, 91, PixelesPorUnidad, 38.1f, 86f),
            new Fotograma("r1", 82, 91, PixelesPorUnidad, 35.9f, 86f),
            new Fotograma("r2", 82, 91, PixelesPorUnidad, 35.6f, 86f),
            new Fotograma("r3", 82, 91, PixelesPorUnidad, 30.9f, 86f),
            new Fotograma("r4", 82, 91, PixelesPorUnidad, 30.9f, 86f),
            new Fotograma("a1", 64, 92, 81f, 31.8f, 91f),
            new Fotograma("a2", 93, 88, 77f, 7.0f, 88f),
            new Fotograma("a3", 76, 90, 76f, 13.7f, 89f),
            new Fotograma("a4", 62, 81, 70f, 26.2f, 80f),
            new Fotograma("d2", 82, 91, PixelesPorUnidad, 38.1f, 86f),
            new Fotograma("d3", 82, 91, PixelesPorUnidad, 45.4f, 86f),
        };

        /// <summary>Las reglas de importación, una por fotograma (las aplica <see cref="TablaArte"/>).</summary>
        public static IEnumerable<TablaArte.Regla> Reglas => Fotogramas.Select(f => new TablaArte.Regla(
            RutaSprite(f.Nombre), f.PixelesPorUnidad, f.Pivote,
            $"Lamento, {f.Nombre}: {f.Lienzo.x}×{f.Lienzo.y}, con el ancla de los pies donde la tiene ese fotograma del Eco de la Ira.",
            f.Lienzo));

        // --- Animaciones ---

        // Los mismos estados que el Eco: la FSM del enemigo escribe los mismos parámetros.
        public const string Idle = ConstructorEcoIra.Idle;
        public const string Run = ConstructorEcoIra.Run;
        public const string Attack = ConstructorEcoIra.Attack;
        public const string Death = ConstructorEcoIra.Death;

        /// <summary>
        /// Los tiempos del dibujo: a1, la anticipación, dura 0,45 s; a2 y a3, el zarpazo, 0,15 s, y
        /// el ataque entero, 0,8 s. En el prefab el ataque va a <see cref="VelocidadAtaque"/>. Al
        /// morir, d3 (el charco) es lo que se queda antes de desvanecerse.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Lamento_Idle", Estado = Idle, Fotogramas = new[] { "I1", "I2" }, Duraciones = new[] { 0.60f, 0.60f }, Bucle = true },
            new Clip { Nombre = "Lamento_Run", Estado = Run, Fotogramas = new[] { "r1", "r2", "r3", "r4" }, Duraciones = new[] { 0.16f, 0.16f, 0.16f, 0.16f }, Bucle = true },
            new Clip { Nombre = "Lamento_Attack", Estado = Attack, Fotogramas = new[] { "a1", "a2", "a3", "a4" }, Duraciones = new[] { 0.45f, 0.10f, 0.05f, 0.20f } },
            new Clip { Nombre = "Lamento_Death", Estado = Death, Fotogramas = new[] { "d2", "d3" }, Duraciones = new[] { 0.25f, 0.35f } },
        };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        /// <summary>
        /// Velocidad del clip de ataque en el prefab: la del Eco de la Ira
        /// (<see cref="ConstructorEcoIra.VelocidadAtaque"/>). El Lamento es el Eco de este mundo y
        /// lleva el ajuste que se le hizo a aquel tras la primera prueba del Nivel 1, donde el golpe
        /// se sintió lento. A 1,6×, a1 dura 0,28 s, el zarpazo 0,09 s y el ataque entero 0,5 s.
        /// </summary>
        public const float VelocidadAtaque = ConstructorEcoIra.VelocidadAtaque;

        /// <summary>
        /// Segundos que descansa entre dos golpes: los del Eco de la Ira tras aquella prueba. Sigue
        /// siendo lento y pesado en lo demás: anda, persigue y espera como antes.
        /// </summary>
        public const float DescansoEntreGolpes = 0.45f;

        /// <summary>Lo que dura el ataque en el juego, con el clip a <see cref="VelocidadAtaque"/>.</summary>
        public static float DuracionAtaque => ClipDe(Attack).Duracion / VelocidadAtaque;

        /// <summary>Cuándo empieza a hacer daño (a2) y cuándo deja de hacerlo (a4), en el juego.</summary>
        public static float InicioDelGolpe => ClipDe(Attack).Inicio(1) / VelocidadAtaque;

        public static float FinDelGolpe => ClipDe(Attack).Inicio(3) / VelocidadAtaque;

        /// <summary>
        /// El paso es el del Eco (0,62 u por vuelta de r1 a r4: es su mismo cuerpo), más lento: a
        /// ritmo 1 avanza 0,97 u/s. Persiguiendo (1,4 u/s) el clip se acelera con el cuerpo.
        /// </summary>
        public static float VelocidadNaturalCarrera => ConstructorEcoIra.UnidadesPorCiclo / ClipDe(Run).Duracion;

        /// <summary>Segundos que el charco de d3 se queda en pantalla antes de desvanecerse.</summary>
        public const float RestosEnPantalla = 0.5f;

        // Las gotas de la muerte: el cian de sus lágrimas y el azul de su ropa, los del dibujo.
        public static readonly Color ColorLagrima = new Color32(0, 212, 255, 255);
        public static readonly Color ColorRopa = new Color32(38, 58, 84, 255);

        // --- Construcción ---

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El prefab del Lamento con arte, o null si todavía no se construyó.</summary>
        public static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

        /// <summary>
        /// Fija los ajustes de importación y crea los clips, el controlador y el prefab. Devuelve
        /// false solo si algo falló; si faltan fotogramas avisa y no construye nada.
        /// </summary>
        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "el Lamento", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            var clips = Clips.ToDictionary(c => c.Estado, c => (Motion)ClipsDeSprites.Crear(CarpetaAnimaciones, c, sprites, EventosDe(c)));
            var controlador = ArmarControlador(clips);

            GeneradorEnemigos.GenerarDatos(false);
            if (!AjustarDatos())
            {
                return false;
            }

            var ok = CrearPrefab(sprites[ClipDe(Idle).Fotogramas[0]], controlador) != null;
            Debug.Log($"[Fragmentum] Lamento: {sprites.Count} fotogramas, {Clips.Length} clips, {RutaControlador} y {RutaPrefab}.");
            return ok;
        }

        /// <summary>
        /// Los eventos que recibe <see cref="EnemigoAnimacion"/>, como en el Eco: el zarpazo hace
        /// daño de a2 a a4, y cada clip sin bucle avisa al acabar.
        /// </summary>
        public static IEnumerable<AnimationEvent> EventosDe(Clip clip)
        {
            switch (clip.Estado)
            {
                case Attack:
                    return new[]
                    {
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.AbrirGolpe), clip.Inicio(1)),
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.CerrarGolpe), clip.Inicio(3)),
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.FinAtaque), clip.Duracion),
                    };
                case Death:
                    return new[] { EnemigosConArte.Evento(nameof(EnemigoAnimacion.FinMuerte), clip.Duracion) };
                default:
                    return Enumerable.Empty<AnimationEvent>();
            }
        }

        /// <summary>
        /// Las transiciones del Eco de la Ira (ver <c>ConstructorEcoIra</c>): ataca desde Idle o
        /// corriendo en cuanto la FSM entra en ATTACK, vuelve a correr al acabar el zarpazo (o si
        /// un golpe lo corta) y la muerte interrumpe cualquier estado y no tiene salida.
        /// </summary>
        private static AnimatorController ArmarControlador(IReadOnlyDictionary<string, Motion> clips)
        {
            var controlador = ControladoresAnimator.CargarOCrear(RutaControlador);
            ControladoresAnimator.AsegurarParametros(controlador,
                (AnimatorParams.MovEn.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Attack.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Damage.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Death.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.VelocidadCorrer.Nombre, AnimatorControllerParameterType.Float, 1f),
                (AnimatorParams.VelocidadAtaque.Nombre, AnimatorControllerParameterType.Float, 1f));

            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, Clips.Select(c => (c.Estado, clips[c.Estado])).ToList(),
                new Dictionary<string, string>
                {
                    [Run] = AnimatorParams.VelocidadCorrer.Nombre,
                    [Attack] = AnimatorParams.VelocidadAtaque.Nombre,
                });

            AnimatorCondition Si(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.If, p.Nombre);
            AnimatorCondition No(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.IfNot, p.Nombre);

            ControladoresAnimator.Transiciones(estados[Idle],
                new ControladoresAnimator.Transicion("Ya a tiro", estados[Attack], null, Si(AnimatorParams.Attack)),
                new ControladoresAnimator.Transicion("Detecta al jugador", estados[Run], null, Si(AnimatorParams.MovEn)));
            ControladoresAnimator.Transiciones(estados[Run],
                new ControladoresAnimator.Transicion("Entra en rango", estados[Attack], null, Si(AnimatorParams.Attack)),
                new ControladoresAnimator.Transicion("Lo pierde", estados[Idle], null,
                    No(AnimatorParams.MovEn), No(AnimatorParams.Damage), No(AnimatorParams.Attack)));
            ControladoresAnimator.Transiciones(estados[Attack],
                new ControladoresAnimator.Transicion("Termina el golpe", estados[Run], 1f),
                new ControladoresAnimator.Transicion("Golpe interrumpido", estados[Run], null, No(AnimatorParams.Attack)));
            ControladoresAnimator.Transiciones(estados[Death]);

            ControladoresAnimator.DesdeCualquierEstado(maquina,
                new ControladoresAnimator.Transicion("Muere", estados[Death], null, Si(AnimatorParams.Death)));

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        /// <summary>
        /// Lo que el arte cambia en sus datos: tinte blanco (los colores son del dibujo) y mira a
        /// la derecha, y los tiempos del ataque, que son los del clip a <see cref="VelocidadAtaque"/>
        /// con el descanso del Eco de la Ira.
        /// </summary>
        public static bool AjustarDatos()
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            if (datos == null)
            {
                Debug.LogError($"[Fragmentum] Falta {GeneradorEnemigos.CarpetaDatos}/{NombreDatos}.asset.");
                return false;
            }

            var cambia = datos.tinte != Color.white || !datos.spriteMiraDerecha ||
                         !Mathf.Approximately(datos.attackCooldown, DescansoEntreGolpes) ||
                         !Mathf.Approximately(datos.duracionAtaque, DuracionAtaque) ||
                         !Mathf.Approximately(datos.inicioVentanaGolpe, InicioDelGolpe) ||
                         !Mathf.Approximately(datos.finVentanaGolpe, FinDelGolpe);
            if (cambia)
            {
                datos.tinte = Color.white;
                datos.spriteMiraDerecha = true;
                datos.attackCooldown = DescansoEntreGolpes;
                datos.duracionAtaque = DuracionAtaque;
                datos.inicioVentanaGolpe = InicioDelGolpe;
                datos.finVentanaGolpe = FinDelGolpe;
                EditorUtility.SetDirty(datos);
            }

            return true;
        }

        // El cuerpo, el arma y el pecho son los del Eco: es su misma silueta, a su misma escala.
        private static GameObject CrearPrefab(Sprite reposo, AnimatorController controlador)
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            var particulas = AssetDatabase.LoadAssetAtPath<Material>(ConstructorEcoIra.RutaMaterialParticulas);
            var medidas = new EnemigosConArte.Medidas
            {
                Cuerpo = ConstructorEcoIra.TamanoCuerpo,
                Contacto = ConstructorEcoIra.TamanoContacto,
                CentroArma = ConstructorEcoIra.CentroArma,
                TamanoArma = ConstructorEcoIra.TamanoArma,
            };

            return EnemigosConArte.CrearPrefab(NombreDatos, RutaPrefab, datos, reposo, controlador, medidas, armazon =>
            {
                var estallido = particulas != null
                    ? ConstructorEcoIra.CrearEstallido(armazon.Pivote, armazon.Raiz.layer, particulas, ConstructorEcoIra.Pecho,
                        ColorLagrima, ColorRopa, PixelDeArte, 0.6f)
                    : null;

                Asignar(armazon.Animacion, "velocidadNaturalCarrera", p => p.floatValue = VelocidadNaturalCarrera);
                Asignar(armazon.Animacion, nameof(EnemigoAnimacion.velocidadAtaque), p => p.floatValue = VelocidadAtaque);
                Asignar(armazon.Animacion, "estallido", p => p.objectReferenceValue = estallido);
                Asignar(armazon.Animacion, "restosEnPantalla", p => p.floatValue = RestosEnPantalla);

                // Pesa: su zarpazo y su cuerpo frenan a Kael.
                var pesar = armazon.Raiz.AddComponent<Pesar>();
                Asignar(pesar, "golpes", p =>
                {
                    p.arraySize = 2;
                    p.GetArrayElementAtIndex(0).objectReferenceValue = armazon.Arma;
                    p.GetArrayElementAtIndex(1).objectReferenceValue = armazon.Contacto;
                });
            });
        }
    }
}
