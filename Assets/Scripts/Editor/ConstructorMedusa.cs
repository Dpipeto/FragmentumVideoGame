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
    /// La Medusa de Hielo (mundo de la Tristeza) con su arte:
    /// 1. importa sus 6 fotogramas, en el mismo lienzo (133×100) y con el pivote en el centro, que
    ///    es el centro de su cuerpo;
    /// 2. crea sus clips: flotar (F1 a F4, la campana se contrae y se abre), el golpe que la aparta
    ///    (A1) y el segundo que pasa con el núcleo apagado (A2);
    /// 3. arma su controlador, que mueve <see cref="MedusaHielo"/> con un disparador y un bool;
    /// 4. vuelve a montar su prefab (<see cref="GeneradorEnemigos.CrearMedusa"/>), ya vestido.
    ///
    /// No muere: no tiene muerte. Encoger, la encoge su script; A2 solo apaga el núcleo.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo2/Medusa</c>. Idempotente.
    /// </summary>
    public static class ConstructorMedusa
    {
        public const string CarpetaSprites = "Assets/Sprites/Enemigos/Medusa";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Enemigos/Medusa";
        public const string RutaControlador = CarpetaAnimaciones + "/Medusa.controller";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        public static readonly Vector2Int Lienzo = new Vector2Int(133, 100);

        /// <summary>
        /// El cuerpo de F1 (campana y tentáculos) mide 64 × 79 píxeles: a 78 por unidad, 0,8 × 1 u,
        /// el «~1 u» del diseño. Así cabe en su zona de golpe (un círculo de 0,45 u de radio), que
        /// es con lo que se midieron los huecos del recorrido.
        /// </summary>
        public const float PixelesPorUnidad = 78f;

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(CarpetaSprites + "/", PixelesPorUnidad, null,
                "Medusa de Hielo: 6 fotogramas de 133×100 con el pivote en el centro del lienzo, que es el centro de su cuerpo; 1 u de alto.",
                Lienzo),
        };

        public const string Flotar = "Flotar";
        public const string Apartada = "Apartada";
        public const string Encogida = "Encogida";

        /// <summary>
        /// Flota contrayendo la campana (F1 a F3) y abriéndola de golpe (F4). Al golpearla, A1 dura
        /// un instante y después se queda en A2 mientras siga encogida.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Medusa_Flotar", Estado = Flotar, Fotogramas = new[] { "F1", "F2", "F3", "F4" }, Duraciones = new[] { 0.22f, 0.22f, 0.22f, 0.22f }, Bucle = true },
            new Clip { Nombre = "Medusa_Apartada", Estado = Apartada, Fotogramas = new[] { "A1" }, Duraciones = new[] { 0.15f } },
            new Clip { Nombre = "Medusa_Encogida", Estado = Encogida, Fotogramas = new[] { "A2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
        };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El fotograma con el que se la ve antes de jugar, o null si no hay arte.</summary>
        public static Sprite Reposo => AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite(ClipDe(Flotar).Fotogramas[0]));

        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "la Medusa de Hielo", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            var clips = Clips.ToDictionary(c => c.Estado, c => (Motion)ClipsDeSprites.Crear(CarpetaAnimaciones, c, sprites));

            var controlador = ControladoresAnimator.CargarOCrear(RutaControlador);
            ControladoresAnimator.AsegurarParametros(controlador,
                (AnimatorParams.Apartada.Nombre, AnimatorControllerParameterType.Trigger, 0f),
                (AnimatorParams.Encogida.Nombre, AnimatorControllerParameterType.Bool, 0f));

            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, Clips.Select(c => (c.Estado, clips[c.Estado])).ToList());
            ControladoresAnimator.Transiciones(estados[Flotar]);
            ControladoresAnimator.Transiciones(estados[Apartada],
                new ControladoresAnimator.Transicion("Se encoge", estados[Encogida], 1f));
            ControladoresAnimator.Transiciones(estados[Encogida],
                new ControladoresAnimator.Transicion("Vuelve a abrirse", estados[Flotar], null,
                    ControladoresAnimator.Condicion(AnimatorConditionMode.IfNot, AnimatorParams.Encogida.Nombre)));
            ControladoresAnimator.DesdeCualquierEstado(maquina,
                new ControladoresAnimator.Transicion("La golpean", estados[Apartada], null,
                    ControladoresAnimator.Condicion(AnimatorConditionMode.If, AnimatorParams.Apartada.Nombre)));
            EditorUtility.SetDirty(controlador);

            var ok = GeneradorEnemigos.CrearMedusa() != null;
            Debug.Log($"[Fragmentum] Medusa de Hielo: {sprites.Count} fotogramas, {Clips.Length} clips, {RutaControlador} y {GeneradorEnemigos.RutaPrefabMedusa}.");
            return ok;
        }
    }
}
