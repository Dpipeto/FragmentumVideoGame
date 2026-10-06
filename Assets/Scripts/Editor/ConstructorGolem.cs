using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using UnityEditor;
using UnityEditor.Animations;
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
    /// 2. crea los 4 clips desde <see cref="Clips"/>, con los tiempos del diseño;
    /// 3. arma su controlador (<see cref="RutaControlador"/>), con un estado por clip.
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

        // --- Andar ---

        /// <summary>
        /// Velocidad del cuerpo (u/s) a la que el andar va a los tiempos de la tabla (0,13 s por
        /// fotograma). El dibujo no tiene zancada que medir (el pie de atrás no se mueve en los
        /// cuatro fotogramas), así que la referencia es su velocidad persiguiendo: al patrullar, a
        /// 1 u/s, anda a la mitad de ritmo. La usa <c>EnemigoAnimacion</c> (fase D).
        /// </summary>
        public const float VelocidadNaturalAndar = 2f;

        /// <summary>
        /// Por debajo de este ritmo de andar (velocidad real / natural) el Golem se para y respira;
        /// por encima de <see cref="RitmoEnMarcha"/> vuelve a andar. Entre los dos no cambia: así no
        /// parpadea entre Idle y Andar cuando arranca o frena.
        /// </summary>
        public const float RitmoParado = 0.05f;

        public const float RitmoEnMarcha = 0.15f;

        // --- Construcción ---

        public const string RutaControlador = CarpetaAnimaciones + "/GolemCeniza.controller";

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>
        /// Fija los ajustes de importación y crea los clips y el controlador. Devuelve false solo
        /// si algo falló; si faltan fotogramas avisa y no construye nada.
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
            var clips = Clips.ToDictionary(c => c.Estado, c => (Motion)ClipsDeSprites.Crear(CarpetaAnimaciones, c, sprites));
            ArmarControlador(clips);

            Debug.Log($"[Fragmentum] Golem de Ceniza: {sprites.Count} fotogramas, {Clips.Length} clips y {RutaControlador}.");
            return true;
        }

        /// <summary>
        /// Cuatro estados movidos por los parámetros que ya escribe la FSM del enemigo (MovEn,
        /// Attack, Death) y por el ritmo de andar que escribe <c>EnemigoAnimacion</c>:
        /// - ataca desde Idle o andando en cuanto la FSM entra en ATTACK (la FSM encadena
        ///   transiciones en un mismo fotograma: puede llegar a ATTACK sin que se vea MovEn);
        /// - al acabar el golpe vuelve a Idle y, si se mueve, a Andar: parado a distancia de ataque
        ///   mientras espera, respira en vez de quedarse congelado a media zancada;
        /// - la muerte interrumpe cualquier estado y no tiene salida (se queda en D4).
        ///
        /// Sin estado de daño: el Golem no tiene animación de daño ni se aturde; al recibir un
        /// golpe destella sin dejar su ataque. Damage se declara porque la FSM lo apaga al morir.
        /// </summary>
        private static void ArmarControlador(IReadOnlyDictionary<string, Motion> clips)
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
                    [Andar] = AnimatorParams.VelocidadCorrer.Nombre,
                    [Ataque] = AnimatorParams.VelocidadAtaque.Nombre,
                });

            var si = AnimatorConditionMode.If;
            var no = AnimatorConditionMode.IfNot;
            AnimatorCondition Si(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(si, p.Nombre);
            AnimatorCondition No(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(no, p.Nombre);
            var ritmo = AnimatorParams.VelocidadCorrer.Nombre;

            // Atacar va primero: al entrar en ATTACK la FSM apaga MovEn, y pararse se cumpliría también.
            ControladoresAnimator.Transiciones(estados[Idle],
                new ControladoresAnimator.Transicion("Ya a tiro", estados[Ataque], null, Si(AnimatorParams.Attack)),
                new ControladoresAnimator.Transicion("Echa a andar", estados[Andar], null,
                    Si(AnimatorParams.MovEn), ControladoresAnimator.Condicion(AnimatorConditionMode.Greater, ritmo, RitmoEnMarcha)));
            ControladoresAnimator.Transiciones(estados[Andar],
                new ControladoresAnimator.Transicion("Entra en rango", estados[Ataque], null, Si(AnimatorParams.Attack)),
                new ControladoresAnimator.Transicion("Lo pierde", estados[Idle], null, No(AnimatorParams.MovEn), No(AnimatorParams.Attack)),
                new ControladoresAnimator.Transicion("Se para", estados[Idle], null,
                    ControladoresAnimator.Condicion(AnimatorConditionMode.Less, ritmo, RitmoParado), No(AnimatorParams.Attack)));
            ControladoresAnimator.Transiciones(estados[Ataque],
                new ControladoresAnimator.Transicion("Termina el golpe", estados[Idle], 1f),
                new ControladoresAnimator.Transicion("Golpe interrumpido", estados[Idle], null, No(AnimatorParams.Attack)));
            ControladoresAnimator.Transiciones(estados[Muerte]);

            ControladoresAnimator.DesdeCualquierEstado(maquina,
                new ControladoresAnimator.Transicion("Muere", estados[Muerte], null, Si(AnimatorParams.Death)));

            EditorUtility.SetDirty(controlador);
        }
    }
}
