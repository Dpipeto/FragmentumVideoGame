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
    /// El Penitente, el Eco de la Culpa, con su arte: el mismo cuerpo que el Eco de la Ira, encorvado,
    /// con placas de identificación en el pecho y las cadenas de sus grilletes arrastrando. Cada
    /// fotograma se hizo editando el del Eco, así que entra con sus mismos estados (Idle, Run, Attack y
    /// Death) y su misma geometría, como el Lamento y el Espectro. Lo que cambia:
    ///
    /// - <b>Cada fotograma lleva su pivote y su escala.</b> Gemini dibujó cada uno con su propio píxel
    ///   (de 16,7 a 20,5 px en su lienzo). La limpieza llevó a cada uno el ancla del Eco
    ///   (<c>limpiar_mundo4.py</c>, <c>anclas.json</c>): de ahí el ancla de los pies y los píxeles por
    ///   unidad de cada uno (<see cref="Fotogramas"/>), para que mida lo que el Eco.
    /// - <b>Ataca al ritmo del Eco de la Ira</b> (<see cref="VelocidadAtaque"/>, 1,6×, y
    ///   <see cref="DescansoEntreGolpes"/>) desde el principio: es el Eco de este mundo («recuerda el
    ///   tamaño y velocidad de ataque del enemigo común»). Sus tiempos son los del Lamento, que ya eran
    ///   los de sus datos.
    /// - Al morir se deshace en tierra seca y ceniza: d2 y, en el suelo, d3.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo4/Penitente</c> (con su LEEME) y no se retocan.
    /// Idempotente: «Regenerar todo» lo ejecuta y, si nada cambió, no cambia nada.
    /// </summary>
    public static class ConstructorPenitente
    {
        public const string CarpetaSprites = "Assets/Sprites/Enemigos/Penitente";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Enemigos/Penitente";
        public const string NombreDatos = "Penitente";
        public const string RutaControlador = CarpetaAnimaciones + "/Penitente.controller";
        public const string RutaPrefab = GeneradorEnemigos.CarpetaPrefabs + "/" + NombreDatos + ".prefab";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        // --- Escala y pivotes ---

        /// <summary>Un píxel del dibujo, en unidades, en los fotogramas de andar (a 82,5 px/u).</summary>
        public const float PixelDeArte = 1f / 82.5f;

        /// <summary>
        /// El encaje de cada fotograma sobre el del Eco (la limpieza, <c>anclas.json</c>): su lienzo, los
        /// píxeles por unidad con los que mide lo que el Eco y el ancla de los pies (en horizontal, la del
        /// Eco llevada a ese fotograma; en vertical, la fila donde pisa).
        /// </summary>
        public static readonly ConstructorLamento.Fotograma[] Fotogramas =
        {
            new ConstructorLamento.Fotograma("I1", 71, 103, 82.4f, 41.0f, 103f),
            new ConstructorLamento.Fotograma("I2", 57, 78, 69.2f, 25.0f, 77f),
            new ConstructorLamento.Fotograma("r1", 67, 88, 69.2f, 27.0f, 87f),
            new ConstructorLamento.Fotograma("r2", 99, 100, 82.5f, 37.5f, 99f),
            new ConstructorLamento.Fotograma("r3", 86, 104, 82.5f, 42.5f, 104f),
            new ConstructorLamento.Fotograma("r4", 84, 94, 82.5f, 26.5f, 93f),
            new ConstructorLamento.Fotograma("a1", 80, 83, 82.5f, 44.5f, 83f),
            new ConstructorLamento.Fotograma("a2", 103, 86, 74.0f, 19.3f, 85f),
            new ConstructorLamento.Fotograma("a3", 71, 91, 74.0f, 17.9f, 90f),
            new ConstructorLamento.Fotograma("a4", 58, 81, 71.0f, 22.0f, 80f),
            new ConstructorLamento.Fotograma("d2", 70, 78, 71.0f, 33.0f, 77f),
            new ConstructorLamento.Fotograma("d3", 82, 44, 69.1f, 45.3f, 42f),
        };

        /// <summary>Las reglas de importación, una por fotograma (las aplica <see cref="TablaArte"/>).</summary>
        public static IEnumerable<TablaArte.Regla> Reglas => Fotogramas.Select(f => new TablaArte.Regla(
            RutaSprite(f.Nombre), f.PixelesPorUnidad, f.Pivote,
            $"Penitente, {f.Nombre}: {f.Lienzo.x}×{f.Lienzo.y}, con el ancla de los pies del Eco de la Ira (mide lo que él).",
            f.Lienzo));

        // --- Animaciones ---

        // Los mismos estados que el Eco: la FSM del enemigo escribe los mismos parámetros.
        public const string Idle = ConstructorEcoIra.Idle;
        public const string Run = ConstructorEcoIra.Run;
        public const string Attack = ConstructorEcoIra.Attack;
        public const string Death = ConstructorEcoIra.Death;

        /// <summary>
        /// Los tiempos del Lamento: a1, la anticipación, dura 0,45 s; a2 y a3, el zarpazo, 0,15 s, y el
        /// ataque entero, 0,8 s (en el prefab va a <see cref="VelocidadAtaque"/>: 0,5 s). Anda arrastrando
        /// los pies, como el Lamento. Al morir, d3 (el montón) es lo que se queda antes de desvanecerse.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Penitente_Idle", Estado = Idle, Fotogramas = new[] { "I1", "I2" }, Duraciones = new[] { 0.60f, 0.60f }, Bucle = true },
            new Clip { Nombre = "Penitente_Run", Estado = Run, Fotogramas = new[] { "r1", "r2", "r3", "r4" }, Duraciones = new[] { 0.16f, 0.16f, 0.16f, 0.16f }, Bucle = true },
            new Clip { Nombre = "Penitente_Attack", Estado = Attack, Fotogramas = new[] { "a1", "a2", "a3", "a4" }, Duraciones = new[] { 0.45f, 0.10f, 0.05f, 0.20f } },
            new Clip { Nombre = "Penitente_Death", Estado = Death, Fotogramas = new[] { "d2", "d3" }, Duraciones = new[] { 0.25f, 0.35f } },
        };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        /// <summary>La velocidad del ataque: la del Eco de la Ira, desde el principio.</summary>
        public const float VelocidadAtaque = ConstructorEcoIra.VelocidadAtaque;

        /// <summary>Segundos que descansa entre dos golpes: los del Eco de la Ira y el Lamento.</summary>
        public const float DescansoEntreGolpes = ConstructorLamento.DescansoEntreGolpes;

        /// <summary>Lo que dura el ataque en el juego.</summary>
        public static float DuracionAtaque => ClipDe(Attack).Duracion / VelocidadAtaque;

        /// <summary>Cuándo empieza a hacer daño (a2) y cuándo deja de hacerlo (a4), en el juego.</summary>
        public static float InicioDelGolpe => ClipDe(Attack).Inicio(1) / VelocidadAtaque;

        public static float FinDelGolpe => ClipDe(Attack).Inicio(3) / VelocidadAtaque;

        /// <summary>El paso es el del Eco: es su mismo cuerpo.</summary>
        public static float VelocidadNaturalCarrera => ConstructorEcoIra.UnidadesPorCiclo / ClipDe(Run).Duracion;

        // La tierra y la ceniza de su muerte.
        public static readonly Color ColorTierra = new Color32(92, 61, 30, 255);
        public static readonly Color ColorCeniza = new Color32(122, 114, 104, 255);

        // --- Construcción ---

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El prefab del Penitente con arte, o null si todavía no se construyó.</summary>
        public static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

        /// <summary>
        /// Fija los ajustes de importación y crea los clips, el controlador y el prefab. Devuelve false
        /// solo si algo falló; si faltan fotogramas avisa y no construye nada.
        /// </summary>
        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "el Penitente", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            var clips = Clips.ToDictionary(c => c.Estado, c => (Motion)ClipsDeSprites.Crear(CarpetaAnimaciones, c, sprites, ConstructorLamento.EventosDe(c)));
            var controlador = ArmarControlador(clips);

            GeneradorEnemigos.GenerarDatos(false);
            if (!AjustarDatos())
            {
                return false;
            }

            var ok = CrearPrefab(sprites[ClipDe(Idle).Fotogramas[0]], controlador) != null;
            Debug.Log($"[Fragmentum] Penitente: {sprites.Count} fotogramas, {Clips.Length} clips, {RutaControlador} y {RutaPrefab}.");
            return ok;
        }

        /// <summary>Las transiciones del Eco de la Ira (ver <c>ConstructorEcoIra</c>), como las del Lamento.</summary>
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
        /// Lo que el arte cambia en sus datos: tinte blanco (los colores son del dibujo) y mira a la
        /// derecha, como el Eco. Los tiempos del ataque son los del clip a <see cref="VelocidadAtaque"/>,
        /// con el descanso del Eco.
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
                        ColorTierra, ColorCeniza, PixelDeArte, 0.6f)
                    : null;

                Asignar(armazon.Animacion, "velocidadNaturalCarrera", p => p.floatValue = VelocidadNaturalCarrera);
                Asignar(armazon.Animacion, nameof(EnemigoAnimacion.velocidadAtaque), p => p.floatValue = VelocidadAtaque);
                Asignar(armazon.Animacion, "estallido", p => p.objectReferenceValue = estallido);
            });
        }
    }
}
