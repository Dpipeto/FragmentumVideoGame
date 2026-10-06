using System.Linq;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El Golem de Ceniza, mini-jefe del Mundo 1, con su arte:
    /// 1. importa los 15 fotogramas de <see cref="CarpetaSprites"/> como pixel art, con el mismo
    ///    lienzo (90×92) y el pivote en la línea de suelo (fila 88) para todos: se puede cortar de
    ///    cualquier clip a cualquier otro sin que el sprite salte. Los ajustes están en
    ///    <see cref="TablaArte"/>;
    /// 2. crea los 4 clips desde <see cref="Clips"/>, con los tiempos del diseño.
    ///
    /// El arte llega limpio y alineado desde <c>Tesis/Imagenes/limpios</c>, que tiene su LEEME. Los
    /// PNG no se retocan. El Golem está dibujado mirando a la derecha
    /// (<c>EnemyData.spriteMiraDerecha</c>).
    ///
    /// Idempotente: «Regenerar todo» lo ejecuta y, si nada cambió, no cambia nada.
    /// </summary>
    public static class ConstructorGolem
    {
        public const string CarpetaSprites = TablaArte.CarpetaGolem;
        public const string CarpetaAnimaciones = "Assets/Animaciones/Enemigos/GolemCeniza";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        // Nombres de los estados del controlador.
        public const string Idle = "Idle";
        public const string Andar = "Andar";
        public const string Ataque = "Ataque";
        public const string Muerte = "Muerte";

        /// <summary>
        /// Tabla del diseño (sección 3 del encargo y LEEME del arte). Los tiempos del ataque no son
        /// uniformes a propósito: A1, medio segundo con los dos puños en alto, es la ventana para
        /// reaccionar; A3, el impacto, dura 0,08 s. Si se reparten por igual, el ataque deja de
        /// poder esquivarse.
        ///
        /// - No hay A2: el salto de A1 a A3 es el golpe (el A2 que llegó no tenía brazos).
        /// - El idle respira: I1 · I2 · I3 · I2.
        /// - Al andar, W2 baja el cuerpo 4 px: es el paso que carga el peso.
        /// - D4, los restos, se queda en pantalla al acabar la muerte.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Golem_Idle", Estado = Idle, Fotogramas = new[] { "Golem_I1", "Golem_I2", "Golem_I3", "Golem_I2" }, Duraciones = new[] { 0.40f, 0.40f, 0.40f, 0.40f }, Bucle = true },
            new Clip { Nombre = "Golem_Andar", Estado = Andar, Fotogramas = new[] { "Golem_W1", "Golem_W2", "Golem_W3", "Golem_W4" }, Duraciones = new[] { 0.13f, 0.13f, 0.13f, 0.13f }, Bucle = true },
            new Clip { Nombre = "Golem_Ataque", Estado = Ataque, Fotogramas = new[] { "Golem_A1", "Golem_A3", "Golem_A4", "Golem_A5" }, Duraciones = new[] { 0.50f, 0.08f, 0.14f, 0.26f } },
            new Clip { Nombre = "Golem_Muerte", Estado = Muerte, Fotogramas = new[] { "Golem_D1", "Golem_D2", "Golem_D3", "Golem_D4" }, Duraciones = new[] { 0.35f, 0.45f, 0.50f, ClipsDeSprites.SeQueda } },
        };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        // --- Construcción ---

        /// <summary>
        /// Fija los ajustes de importación y crea los clips. Devuelve false solo si algo falló; si
        /// faltan fotogramas avisa y no construye nada.
        /// </summary>
        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "el Golem de Ceniza", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            foreach (var clip in Clips)
            {
                ClipsDeSprites.Crear(CarpetaAnimaciones, clip, sprites);
            }

            Debug.Log($"[Fragmentum] Golem de Ceniza: {sprites.Count} fotogramas y {Clips.Length} clips en {CarpetaAnimaciones}.");
            return true;
        }
    }
}
