using System;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Levels.Mecanicas;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El arte de las mecánicas del Mundo 1 (torreta, plataforma inestable y plataforma de
    /// combustión): importa sus fotogramas de <see cref="CarpetaSprites"/>, crea sus clips desde
    /// <see cref="Mecanicas"/> y arma un controlador por mecánica.
    ///
    /// Son ciclos que gobierna el script de cada mecánica con sus tiempos públicos (cuánto avisa
    /// la torreta, cuánto aguanta la plataforma), no el Animator ni el jugador. Por eso casi todos
    /// los clips son un solo fotograma que se queda: el estado dura lo que el script diga. El
    /// script pone el parámetro entero <c>Estado</c> con el valor de su enum
    /// (<see cref="ESTADOS_TORRETA"/>, …) y el Animator salta a ese estado desde cualquier otro.
    /// La lava no tiene clip ni controlador: es un solo fotograma, que se repite alternando con su
    /// espejo.
    ///
    /// Dentro de cada mecánica los fotogramas comparten lienzo y encuadre: se cambia de uno a otro
    /// sin mover el Transform (la torreta disparando, con su propio pivote; ver
    /// <see cref="TablaArte"/>).
    ///
    /// Idempotente: «Regenerar todo» lo ejecuta y, si nada cambió, no cambia nada.
    /// </summary>
    public static class ConstructorMecanicas
    {
        public const string CarpetaSprites = TablaArte.CarpetaMecanicas;
        public const string CarpetaAnimaciones = "Assets/Animaciones/Mecanicas";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        /// <summary>
        /// Una mecánica: su nombre (el de su controlador), el enum de su script y sus clips, uno por
        /// valor del enum y en su orden: el clip i es el estado <c>Estado = i</c>.
        /// </summary>
        public sealed class Mecanica
        {
            public string Nombre;
            public Type Estados;
            public Clip[] Clips;

            public Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

            public string RutaControlador => $"{CarpetaAnimaciones}/{Nombre}.controller";

            public AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

            /// <summary>El valor de <c>Estado</c> que lleva al estado del Animator <paramref name="estado"/>.</summary>
            public int ValorDe(string estado) => Array.FindIndex(Clips, c => c.Estado == estado);
        }

        // Nombres de los estados de cada controlador.
        public const string Reposo = "Reposo";
        public const string Carga = "Carga";
        public const string Disparo = "Disparo";
        public const string Intacta = "Intacta";
        public const string Agrietada = "Agrietada";
        public const string Rompe = "Rompe";
        public const string Apagada = "Apagada";
        public const string Calienta = "Calienta";
        public const string Encendida = "Encendida";

        /// <summary>Lo que se ve «rota» antes del polvo: el instante en que se parte en tres trozos.</summary>
        public const float DuracionRota = 0.25f;

        private const float SeQueda = ClipsDeSprites.SeQueda;

        /// <summary>
        /// Torreta: reposo → carga → disparo → reposo. La carga (el hueco al blanco) es el aviso:
        /// lo único que le da al jugador tiempo de salir. Solo el disparo hace daño.
        /// </summary>
        public static readonly Mecanica Torreta = new Mecanica
        {
            Nombre = "Torreta",
            Estados = typeof(ESTADOS_TORRETA),
            Clips = new[]
            {
                new Clip { Nombre = "Torreta_Reposo", Estado = Reposo, Fotogramas = new[] { "Torreta_1_reposo" }, Duraciones = new[] { SeQueda } },
                new Clip { Nombre = "Torreta_Carga", Estado = Carga, Fotogramas = new[] { "Torreta_2_carga" }, Duraciones = new[] { SeQueda } },
                new Clip { Nombre = "Torreta_Disparo", Estado = Disparo, Fotogramas = new[] { "Torreta_3_disparo" }, Duraciones = new[] { SeQueda } },
            },
        };

        /// <summary>
        /// Plataforma inestable: intacta → agrietada (el aviso; aún se pisa) → se rompe. Al
        /// romperse se ve partida en tres trozos un momento y después solo el polvo, que se queda
        /// hasta que el script la esconde.
        /// </summary>
        public static readonly Mecanica Plataforma = new Mecanica
        {
            Nombre = "Plataforma",
            Estados = typeof(ESTADOS_PLATAFORMA),
            Clips = new[]
            {
                new Clip { Nombre = "Plataforma_Intacta", Estado = Intacta, Fotogramas = new[] { "Plataforma_1_intacta" }, Duraciones = new[] { SeQueda } },
                new Clip { Nombre = "Plataforma_Agrietada", Estado = Agrietada, Fotogramas = new[] { "Plataforma_2_agrietada" }, Duraciones = new[] { SeQueda } },
                new Clip { Nombre = "Plataforma_Rompe", Estado = Rompe, Fotogramas = new[] { "Plataforma_3_rota", "Plataforma_4_polvo" }, Duraciones = new[] { DuracionRota, SeQueda } },
            },
        };

        /// <summary>
        /// Plataforma de combustión: apagada → calienta (las brasas suben entre las rejas: el
        /// aviso) → encendida (la llama, la única que hace daño). Nunca desaparece.
        /// </summary>
        public static readonly Mecanica Combustion = new Mecanica
        {
            Nombre = "Combustion",
            Estados = typeof(ESTADOS_COMBUSTION),
            Clips = new[]
            {
                new Clip { Nombre = "Combustion_Apagada", Estado = Apagada, Fotogramas = new[] { "Combustion_1_apagada" }, Duraciones = new[] { SeQueda } },
                new Clip { Nombre = "Combustion_Calienta", Estado = Calienta, Fotogramas = new[] { "Combustion_2_calienta" }, Duraciones = new[] { SeQueda } },
                new Clip { Nombre = "Combustion_Encendida", Estado = Encendida, Fotogramas = new[] { "Combustion_3_encendida" }, Duraciones = new[] { SeQueda } },
            },
        };

        public static readonly Mecanica[] Mecanicas = { Torreta, Plataforma, Combustion };

        public const string Vuelo = "Vuelo";

        /// <summary>
        /// La bola de fuego de la torreta: vuela con dos fotogramas en bucle. No es un ciclo de su
        /// script, así que su controlador tiene un solo estado y ningún parámetro. Al apagarse deja
        /// sus chispas (<see cref="FotogramaBolaDisipa"/>) un momento: eso lo pone el proyectil.
        /// </summary>
        public static readonly Clip BolaVuelo = new Clip
        {
            Nombre = "Bola_Vuelo", Estado = Vuelo, Fotogramas = new[] { "Bola_1_vuelo", "Bola_2_vuelo" },
            Duraciones = new[] { 0.1f, 0.1f }, Bucle = true,
        };

        /// <summary>Las chispas que deja la bola al apagarse (sin clip: un solo fotograma).</summary>
        public const string FotogramaBolaDisipa = "Bola_3_disipa";

        /// <summary>Segundos que se ven las chispas.</summary>
        public const float DuracionBolaDisipa = 0.2f;

        public static string RutaControladorBola => $"{CarpetaAnimaciones}/Bola.controller";

        /// <summary>La lava: un solo fotograma y su espejo, sin clip.</summary>
        public static readonly string[] FotogramasLava = { "Lava", "Lava_espejo" };

        public static Clip[] TodosLosClips => Mecanicas.SelectMany(m => m.Clips).ToArray();

        // --- Construcción ---

        /// <summary>
        /// Fija los ajustes de importación y crea los clips y los controladores. Devuelve false
        /// solo si algo falló; si faltan fotogramas avisa y no construye nada.
        /// </summary>
        public static bool Construir()
        {
            var clips = TodosLosClips;
            if (!ClipsDeSprites.CargarSprites(clips, RutaSprite, "las mecánicas", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            foreach (var mecanica in Mecanicas)
            {
                var estados = Enum.GetNames(mecanica.Estados);
                if (estados.Length != mecanica.Clips.Length)
                {
                    Debug.LogError($"[Fragmentum] {mecanica.Nombre}: {mecanica.Clips.Length} clips para {estados.Length} valores de {mecanica.Estados.Name}. Tiene que haber uno por valor.");
                    return false;
                }

                var creados = mecanica.Clips.Select(c => (c.Estado, (Motion)ClipsDeSprites.Crear(CarpetaAnimaciones, c, sprites))).ToList();
                ArmarControlador(mecanica, creados);
            }

            Debug.Log($"[Fragmentum] Mecánicas: {sprites.Count} fotogramas, {clips.Length} clips y {Mecanicas.Length} controladores en {CarpetaAnimaciones}.");
            return ConstruirBola();
        }

        // La bola, aparte: su clip en bucle y un controlador de un solo estado, sin parámetros.
        private static bool ConstruirBola()
        {
            if (!ClipsDeSprites.CargarSprites(new[] { BolaVuelo }, RutaSprite, "la bola de fuego", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            var clip = ClipsDeSprites.Crear(CarpetaAnimaciones, BolaVuelo, sprites);
            var controlador = ControladoresAnimator.CargarOCrear(RutaControladorBola);
            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, new[] { (BolaVuelo.Estado, (Motion)clip) });
            ControladoresAnimator.Transiciones(estados[BolaVuelo.Estado]);
            maquina.defaultState = estados[BolaVuelo.Estado];
            EditorUtility.SetDirty(controlador);
            return true;
        }

        /// <summary>
        /// Un estado por clip y un solo parámetro, el entero <c>Estado</c>, que pone el script de la
        /// mecánica. Desde cualquier estado se salta al del valor pedido, sin tiempo de salida: el
        /// ciclo y sus tiempos son del script. Empieza en el primero (reposo, intacta, apagada).
        /// </summary>
        private static void ArmarControlador(Mecanica mecanica, IReadOnlyList<(string nombre, Motion clip)> clips)
        {
            var controlador = ControladoresAnimator.CargarOCrear(mecanica.RutaControlador);
            var parametro = AnimatorParams.EstadoMecanica.Nombre;
            ControladoresAnimator.AsegurarParametros(controlador, (parametro, AnimatorControllerParameterType.Int, 0f));

            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, clips);
            foreach (var estado in estados.Values)
            {
                ControladoresAnimator.Transiciones(estado);
            }

            ControladoresAnimator.DesdeCualquierEstado(maquina, clips
                .Select((c, i) => new ControladoresAnimator.Transicion(c.nombre, estados[c.nombre], null,
                    ControladoresAnimator.Condicion(AnimatorConditionMode.Equals, parametro, i)))
                .ToArray());

            EditorUtility.SetDirty(controlador);
        }
    }
}
