using System.Linq;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El arte de las mecánicas del Mundo 1 (torreta, plataforma inestable y plataforma de
    /// combustión): importa sus fotogramas de <see cref="CarpetaSprites"/> y crea sus clips desde
    /// <see cref="Mecanicas"/>.
    ///
    /// Son ciclos que gobierna el script de cada mecánica con sus tiempos públicos (cuánto avisa
    /// la torreta, cuánto aguanta la plataforma), no el Animator. Por eso casi todos los clips son
    /// un solo fotograma que se queda: el estado dura lo que el script diga. La lava no tiene
    /// clip: es un solo fotograma, que se repite alternando con su espejo.
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

        /// <summary>Una mecánica: su nombre (el de su controlador, fase C) y sus clips.</summary>
        public sealed class Mecanica
        {
            public string Nombre;
            public Clip[] Clips;

            public Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);
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
            Clips = new[]
            {
                new Clip { Nombre = "Combustion_Apagada", Estado = Apagada, Fotogramas = new[] { "Combustion_1_apagada" }, Duraciones = new[] { SeQueda } },
                new Clip { Nombre = "Combustion_Calienta", Estado = Calienta, Fotogramas = new[] { "Combustion_2_calienta" }, Duraciones = new[] { SeQueda } },
                new Clip { Nombre = "Combustion_Encendida", Estado = Encendida, Fotogramas = new[] { "Combustion_3_encendida" }, Duraciones = new[] { SeQueda } },
            },
        };

        public static readonly Mecanica[] Mecanicas = { Torreta, Plataforma, Combustion };

        /// <summary>La lava: un solo fotograma y su espejo, sin clip.</summary>
        public static readonly string[] FotogramasLava = { "Lava", "Lava_espejo" };

        public static Clip[] TodosLosClips => Mecanicas.SelectMany(m => m.Clips).ToArray();

        // --- Construcción ---

        /// <summary>
        /// Fija los ajustes de importación y crea los clips. Devuelve false solo si algo falló; si
        /// faltan fotogramas avisa y no construye nada.
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
            foreach (var clip in clips)
            {
                ClipsDeSprites.Crear(CarpetaAnimaciones, clip, sprites);
            }

            Debug.Log($"[Fragmentum] Mecánicas: {sprites.Count} fotogramas y {clips.Length} clips en {CarpetaAnimaciones}.");
            return true;
        }
    }
}
