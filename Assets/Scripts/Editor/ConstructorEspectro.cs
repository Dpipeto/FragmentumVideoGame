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
    /// El Espectro, el Eco del Miedo, con su arte: el mismo cuerpo que el Eco de la Ira, verde y
    /// deshaciéndose en polvo negro por la espalda. Cada fotograma se hizo editando el del Eco, así
    /// que entra con sus mismos estados (Idle, Run, Attack y Death) y su misma geometría, como el
    /// Lamento. Lo que cambia:
    ///
    /// - <b>Cada fotograma lleva su pivote y su escala.</b> Gemini dibujó cada uno con su propio
    ///   píxel (de 17,7 a 20,5 px en su lienzo). La limpieza encajó cada silueta sobre la del Eco
    ///   (<c>limpiar_mundo3.py</c>, <c>anclas.json</c>): de ahí el ancla de los pies y los píxeles
    ///   por unidad de cada uno (<see cref="Fotogramas"/>), para que mida lo que el Eco.
    /// - <b>R1, R2 y R3 llegaron mirando a la izquierda:</b> la limpieza los volteó (decidido el
    ///   2026-10-15). <b>D3 no se usa:</b> llegó vez y media más grande que el resto; el Espectro
    ///   muere deshaciéndose con D2.
    /// - <b>Ataca al ritmo del Eco de la Ira</b> (<see cref="VelocidadAtaque"/>, 1,6×, y
    ///   <see cref="DescansoEntreGolpes"/>), como el Lamento: es el Eco de este mundo («recuerda el
    ///   tamaño y velocidad de ataque del enemigo común», 2026-10-16). Su aviso, a1, pasa de 0,5 a
    ///   0,31 s: con su fundido (5 de opacidad por segundo) ya se le ve entero 0,1 s antes del golpe.
    /// - <b>Duerme y es casi invisible</b> (<see cref="Espectro"/>): con arte no se tiñe, solo se
    ///   funde. Al morir se deshace en polvo negro y violeta.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo3/Espectro</c> (con su LEEME) y no se
    /// retocan. Idempotente: «Regenerar todo» lo ejecuta y, si nada cambió, no cambia nada.
    /// </summary>
    public static class ConstructorEspectro
    {
        public const string CarpetaSprites = "Assets/Sprites/Enemigos/Espectro";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Enemigos/Espectro";
        public const string NombreDatos = "Espectro";
        public const string RutaControlador = CarpetaAnimaciones + "/Espectro.controller";
        public const string RutaPrefab = GeneradorEnemigos.CarpetaPrefabs + "/" + NombreDatos + ".prefab";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        // --- Escala y pivotes ---

        /// <summary>Un píxel del dibujo, en unidades, en el fotograma de reposo (I1, a 69 px/u como el Lamento).</summary>
        public const float PixelDeArte = 1f / 69f;

        /// <summary>
        /// El encaje de cada silueta sobre la del Eco (la limpieza, <c>anclas.json</c>): su lienzo,
        /// los píxeles por unidad con los que mide lo que el Eco y el ancla de los pies (en
        /// horizontal, la del Eco llevada a ese fotograma; en vertical, la fila donde pisa).
        /// </summary>
        public static readonly ConstructorLamento.Fotograma[] Fotogramas =
        {
            new ConstructorLamento.Fotograma("I1", 51, 87, 69.0f, 26.9f, 86f),
            new ConstructorLamento.Fotograma("I2", 50, 87, 76.1f, 20.8f, 86f),
            new ConstructorLamento.Fotograma("r1", 67, 82, 79.3f, 34.4f, 82f),
            new ConstructorLamento.Fotograma("r2", 51, 82, 80.1f, 18.8f, 81f),
            new ConstructorLamento.Fotograma("r3", 52, 80, 77.9f, 17.3f, 79f),
            new ConstructorLamento.Fotograma("r4", 74, 73, 69.6f, 25.4f, 72f),
            new ConstructorLamento.Fotograma("a1", 55, 82, 71.9f, 27.4f, 81f),
            new ConstructorLamento.Fotograma("a2", 90, 90, 73.9f, 6.3f, 89f),
            new ConstructorLamento.Fotograma("a3", 64, 89, 73.9f, 10.9f, 89f),
            new ConstructorLamento.Fotograma("a4", 57, 81, 70.9f, 21.9f, 80f),
            new ConstructorLamento.Fotograma("d2", 57, 72, 69.1f, 26.0f, 72f),
        };

        /// <summary>Las reglas de importación, una por fotograma (las aplica <see cref="TablaArte"/>).</summary>
        public static IEnumerable<TablaArte.Regla> Reglas => Fotogramas.Select(f => new TablaArte.Regla(
            RutaSprite(f.Nombre), f.PixelesPorUnidad, f.Pivote,
            $"Espectro, {f.Nombre}: {f.Lienzo.x}×{f.Lienzo.y}, encajado sobre ese fotograma del Eco de la Ira (mide lo que él).",
            f.Lienzo));

        // --- Animaciones ---

        // Los mismos estados que el Eco: la FSM del enemigo escribe los mismos parámetros.
        public const string Idle = ConstructorEcoIra.Idle;
        public const string Run = ConstructorEcoIra.Run;
        public const string Attack = ConstructorEcoIra.Attack;
        public const string Death = ConstructorEcoIra.Death;

        /// <summary>
        /// Los tiempos del dibujo: a1, el aviso, dura 0,5 s; a2 y a3, el golpe, 0,15 s, y el ataque
        /// entero, 0,8 s. En el prefab el ataque va a <see cref="VelocidadAtaque"/>. Corre con el paso
        /// del Eco. Muere con D2: se deshace en polvo y se desvanece.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Espectro_Idle", Estado = Idle, Fotogramas = new[] { "I1", "I2" }, Duraciones = new[] { 0.55f, 0.55f }, Bucle = true },
            new Clip { Nombre = "Espectro_Run", Estado = Run, Fotogramas = new[] { "r1", "r2", "r3", "r4" }, Duraciones = new[] { 0.12f, 0.12f, 0.12f, 0.12f }, Bucle = true },
            new Clip { Nombre = "Espectro_Attack", Estado = Attack, Fotogramas = new[] { "a1", "a2", "a3", "a4" }, Duraciones = new[] { 0.50f, 0.08f, 0.07f, 0.15f } },
            new Clip { Nombre = "Espectro_Death", Estado = Death, Fotogramas = new[] { "d2" }, Duraciones = new[] { 0.45f } },
        };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        /// <summary>
        /// Velocidad del clip de ataque en el prefab: la del Eco de la Ira
        /// (<see cref="ConstructorEcoIra.VelocidadAtaque"/>), como el Lamento, el ajuste que se le hizo
        /// tras la primera prueba del Nivel 1, donde el golpe se sintió lento. A 1,6×, a1 dura 0,31 s,
        /// el golpe 0,09 s y el ataque entero 0,5 s.
        /// </summary>
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

        // El polvo de su muerte: el negro y el violeta de su espalda.
        public static readonly Color ColorPolvo = new Color32(10, 10, 15, 255);
        public static readonly Color ColorVioleta = new Color32(45, 27, 78, 255);

        // --- Construcción ---

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El prefab del Espectro con arte, o null si todavía no se construyó.</summary>
        public static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

        /// <summary>
        /// Fija los ajustes de importación y crea los clips, el controlador y el prefab. Devuelve
        /// false solo si algo falló; si faltan fotogramas avisa y no construye nada.
        /// </summary>
        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "el Espectro", out var sprites))
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
            Debug.Log($"[Fragmentum] Espectro: {sprites.Count} fotogramas, {Clips.Length} clips, {RutaControlador} y {RutaPrefab}.");
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
        /// Lo que el arte cambia en sus datos: tinte blanco y opaco (los colores son del dibujo; lo
        /// que se le ve lo decide <see cref="Espectro"/>) y mira a la derecha, como el Eco. Los tiempos
        /// del ataque son los del clip a <see cref="VelocidadAtaque"/>, con el descanso del Eco.
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
                        ColorPolvo, ColorVioleta, PixelDeArte, 0.6f)
                    : null;

                Asignar(armazon.Animacion, "velocidadNaturalCarrera", p => p.floatValue = VelocidadNaturalCarrera);
                Asignar(armazon.Animacion, nameof(EnemigoAnimacion.velocidadAtaque), p => p.floatValue = VelocidadAtaque);
                Asignar(armazon.Animacion, "estallido", p => p.objectReferenceValue = estallido);

                // Duerme: el controlador, apagado hasta que despierta (como en su prefab sin arte).
                armazon.Controlador.enabled = false;
                var espectro = armazon.Raiz.AddComponent<Espectro>();
                Asignar(espectro, "pivote", p => p.objectReferenceValue = armazon.Pivote);
                Asignar(espectro, "sprite", p => p.objectReferenceValue = armazon.Sprite);
                // Con arte no se tiñe: dormido y atacando se le ve su dibujo; solo se funde.
                Asignar(espectro, "colorDormido", p => p.colorValue = Color.white);
                Asignar(espectro, "colorAtaque", p => p.colorValue = Color.white);
            });
        }
    }
}
