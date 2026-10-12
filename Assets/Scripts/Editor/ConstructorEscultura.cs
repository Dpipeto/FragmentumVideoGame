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
    /// La Escultura Viviente (mundo de la Tristeza) con su arte: un ciudadano de piedra cubierto de
    /// hielo, sobre su pedestal.
    /// 1. importa los 14 fotogramas de <see cref="CarpetaSprites"/>, todos en el mismo lienzo
    ///    (90×90) y con el pivote en el centro de la base del pedestal;
    /// 2. crea sus clips desde <see cref="Clips"/>: dormida (E1), el despertar (P1 a P3), despierta
    ///    (P3), el golpe (A1 a A5), el golpe recibido (H1) y la muerte (M1 a M4);
    /// 3. arma su controlador: el estado de <see cref="EsculturaDormida"/> (un entero) la despierta
    ///    y, ya despierta, mandan los parámetros de la FSM del enemigo;
    /// 4. monta el prefab <c>Escultura</c>, que nace dormida, con el controlador apagado.
    ///
    /// <b>A3, A4 y A5 están dibujados mirando al otro lado</b> que los otros once: descargan el
    /// golpe hacia la izquierda, y la estatua mira a la derecha. El clip del golpe voltea esos tres
    /// fotogramas (<c>SpriteRenderer.flipX</c>); los PNG no se tocan.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo2/Escultura</c>. Idempotente.
    /// </summary>
    public static class ConstructorEscultura
    {
        public const string CarpetaSprites = "Assets/Sprites/Enemigos/Escultura";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Enemigos/Escultura";
        public const string NombreDatos = "Escultura";
        public const string RutaControlador = CarpetaAnimaciones + "/Escultura.controller";
        public const string RutaPrefab = GeneradorEnemigos.CarpetaPrefabs + "/" + NombreDatos + ".prefab";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        // --- Escala y pivote ---

        public static readonly Vector2Int Lienzo = new Vector2Int(90, 90);

        /// <summary>Mide 74 píxeles con el pedestal: a 31 por unidad, las 2,4 u del diseño.</summary>
        public const float PixelesPorUnidad = 31f;

        /// <summary>
        /// El centro de la base del pedestal: el pedestal ocupa x 27-62 y su base está en la fila
        /// 85, la misma en los 14 fotogramas.
        /// </summary>
        public static readonly Vector2 Pivote = new Vector2(45f / 90f, 4f / 90f);

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(CarpetaSprites + "/", PixelesPorUnidad, Pivote,
                "Escultura Viviente: 14 fotogramas de 90×90 con el pivote en el centro de la base del pedestal (fila 85); 2,4 u de alto.",
                Lienzo),
        };

        // --- Animaciones ---

        public const string Dormida = "Dormida";
        public const string Despertar = "Despertar";
        public const string Idle = "Idle";
        public const string Ataque = "Ataque";
        public const string Golpeada = "Golpeada";
        public const string Muerte = "Muerte";

        /// <summary>
        /// Los tiempos son los de sus datos y los de <see cref="EsculturaDormida"/>:
        /// - despierta en 1 s (P1, P2 y P3, que es como se queda ya despierta);
        /// - el golpe avisa 1 s (A1 alza los puños y A2 los echa atrás, con los ojos encendidos),
        ///   golpea 0,15 s (A3) y tarda 0,65 s en soltarlos (A4 y A5): 1,8 s en total;
        /// - H1 es un instante: acusa el golpe sin aturdirse;
        /// - M4, los restos, se queda en pantalla al acabar la muerte.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Escultura_Dormida", Estado = Dormida, Fotogramas = new[] { "E1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Escultura_Despertar", Estado = Despertar, Fotogramas = new[] { "P1", "P2", "P3" }, Duraciones = new[] { 0.35f, 0.35f, 0.30f } },
            new Clip { Nombre = "Escultura_Idle", Estado = Idle, Fotogramas = new[] { "P3" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Escultura_Ataque", Estado = Ataque, Fotogramas = new[] { "A1", "A2", "A3", "A4", "A5" }, Duraciones = new[] { 0.50f, 0.50f, 0.15f, 0.35f, 0.30f } },
            new Clip { Nombre = "Escultura_Golpeada", Estado = Golpeada, Fotogramas = new[] { "H1" }, Duraciones = new[] { 0.15f } },
            new Clip { Nombre = "Escultura_Muerte", Estado = Muerte, Fotogramas = new[] { "M1", "M2", "M3", "M4" }, Duraciones = new[] { 0.20f, 0.25f, 0.35f, ClipsDeSprites.SeQueda } },
        };

        /// <summary>Los fotogramas dibujados mirando a la izquierda: el clip del golpe los voltea.</summary>
        public static readonly string[] FotogramasVolteados = { "A3", "A4", "A5" };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        /// <summary>Segundos que los restos (M4) se quedan en pantalla antes de desvanecerse.</summary>
        public const float RestosEnPantalla = 0.8f;

        // --- Geometría (u, desde la base del pedestal y mirando a la derecha) ---

        /// <summary>El cuerpo: el ancho del pedestal (36 px) y el alto de la estatua (74 px).</summary>
        public static readonly Vector2 TamanoCuerpo = new Vector2(1.1f, 2.4f);

        public static readonly Vector2 TamanoContacto = new Vector2(1f, 2.3f);

        /// <summary>
        /// El golpe de A3 (ya volteado): de los puños, que caen a 0,2 u del centro, al final del
        /// hielo que salta, a 1,45 u; hasta 1,1 u de alto. Se esquiva apartándose o saltando.
        /// </summary>
        public static readonly Rect Golpe = Rect.MinMaxRect(0.1f, 0f, 1.5f, 1.1f);

        // --- Construcción ---

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El prefab de la Escultura con arte, o null si todavía no se construyó.</summary>
        public static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "la Escultura Viviente", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            var clips = new Dictionary<string, Motion>();
            foreach (var definicion in Clips)
            {
                var clip = ClipsDeSprites.Crear(CarpetaAnimaciones, definicion, sprites, EventosDe(definicion));
                AnimationUtility.SetEditorCurve(clip, CurvaVolteo, definicion.Estado == Ataque ? Volteo(definicion) : null);
                clips[definicion.Estado] = clip;
            }

            var controlador = ArmarControlador(clips);

            GeneradorEnemigos.GenerarDatos(false);
            if (!AjustarDatos())
            {
                return false;
            }

            var ok = CrearPrefab(sprites[ClipDe(Dormida).Fotogramas[0]], controlador) != null;
            Debug.Log($"[Fragmentum] Escultura Viviente: {sprites.Count} fotogramas, {Clips.Length} clips, {RutaControlador} y {RutaPrefab}.");
            return ok;
        }

        /// <summary>La curva que voltea el sprite dentro de un clip.</summary>
        public static EditorCurveBinding CurvaVolteo => EditorCurveBinding.FloatCurve(string.Empty, typeof(SpriteRenderer), "m_FlipX");

        // Un escalón por fotograma: 1 en los que están dibujados al revés.
        private static AnimationCurve Volteo(Clip clip)
        {
            var curva = new AnimationCurve();
            for (var i = 0; i < clip.Fotogramas.Length; i++)
            {
                curva.AddKey(clip.Inicio(i), FotogramasVolteados.Contains(clip.Fotogramas[i]) ? 1f : 0f);
            }

            for (var i = 0; i < curva.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curva, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curva, i, AnimationUtility.TangentMode.Constant);
            }

            return curva;
        }

        /// <summary>
        /// El golpe hace daño solo en A3; el ataque acaba con el clip, y la muerte se remata al
        /// llegar a los restos (M4).
        /// </summary>
        public static IEnumerable<AnimationEvent> EventosDe(Clip clip)
        {
            switch (clip.Estado)
            {
                case Ataque:
                    return new[]
                    {
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.AbrirGolpe), clip.Inicio(2)),
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.CerrarGolpe), clip.Inicio(3)),
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.FinAtaque), clip.Duracion),
                    };
                case Muerte:
                    return new[] { EnemigosConArte.Evento(nameof(EnemigoAnimacion.FinMuerte), clip.Inicio(3)) };
                default:
                    return Enumerable.Empty<AnimationEvent>();
            }
        }

        /// <summary>
        /// - Dormida hasta que <see cref="EsculturaDormida"/> cambia de estado: despertando, el clip
        ///   de despertar; despierta de golpe (la golpearon dormida), directa a Idle.
        /// - Despierta, la FSM del enemigo: ataca en cuanto entra en ATTACK y vuelve a Idle al
        ///   acabar. No anda: no hay estado de andar.
        /// - El golpe recibido (H1) solo sale desde Idle: nunca tapa el aviso de su ataque.
        /// - La muerte interrumpe cualquier estado y no tiene salida (se queda en M4).
        /// </summary>
        private static AnimatorController ArmarControlador(IReadOnlyDictionary<string, Motion> clips)
        {
            var controlador = ControladoresAnimator.CargarOCrear(RutaControlador);
            ControladoresAnimator.AsegurarParametros(controlador,
                (AnimatorParams.EstadoMecanica.Nombre, AnimatorControllerParameterType.Int, 0f),
                (AnimatorParams.MovEn.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Attack.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Damage.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Death.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Golpeada.Nombre, AnimatorControllerParameterType.Trigger, 0f));

            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, Clips.Select(c => (c.Estado, clips[c.Estado])).ToList());

            var estado = AnimatorParams.EstadoMecanica.Nombre;
            AnimatorCondition Es(ESTADOS_ESCULTURA valor) => ControladoresAnimator.Condicion(AnimatorConditionMode.Equals, estado, (int)valor);
            AnimatorCondition Si(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.If, p.Nombre);
            AnimatorCondition No(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.IfNot, p.Nombre);

            ControladoresAnimator.Transiciones(estados[Dormida],
                new ControladoresAnimator.Transicion("Kael pasa cerca", estados[Despertar], null, Es(ESTADOS_ESCULTURA.DESPERTANDO)),
                new ControladoresAnimator.Transicion("La golpean dormida", estados[Idle], null, Es(ESTADOS_ESCULTURA.DESPIERTA)));
            ControladoresAnimator.Transiciones(estados[Despertar],
                new ControladoresAnimator.Transicion("Ya despierta", estados[Idle], null, Es(ESTADOS_ESCULTURA.DESPIERTA)));
            // Atacar va primero: si la golpean justo al empezar su golpe, manda el aviso.
            ControladoresAnimator.Transiciones(estados[Idle],
                new ControladoresAnimator.Transicion("Ya a tiro", estados[Ataque], null, Si(AnimatorParams.Attack)),
                new ControladoresAnimator.Transicion("Acusa el golpe", estados[Golpeada], null, Si(AnimatorParams.Golpeada)));
            ControladoresAnimator.Transiciones(estados[Ataque],
                new ControladoresAnimator.Transicion("Termina el golpe", estados[Idle], 1f),
                new ControladoresAnimator.Transicion("Golpe interrumpido", estados[Idle], null, No(AnimatorParams.Attack)));
            ControladoresAnimator.Transiciones(estados[Golpeada],
                new ControladoresAnimator.Transicion("Se rehace", estados[Idle], 1f));
            ControladoresAnimator.Transiciones(estados[Muerte]);

            ControladoresAnimator.DesdeCualquierEstado(maquina,
                new ControladoresAnimator.Transicion("Muere", estados[Muerte], null, Si(AnimatorParams.Death)));

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        /// <summary>
        /// Lo que el arte cambia en sus datos: tinte blanco, mira a la derecha y los tiempos del
        /// golpe como en el clip (ya eran esos). El resto no se toca.
        /// </summary>
        public static bool AjustarDatos()
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            if (datos == null)
            {
                Debug.LogError($"[Fragmentum] Falta {GeneradorEnemigos.CarpetaDatos}/{NombreDatos}.asset.");
                return false;
            }

            var ataque = ClipDe(Ataque);
            var cambia = datos.tinte != Color.white || !datos.spriteMiraDerecha ||
                         !Mathf.Approximately(datos.duracionAtaque, ataque.Duracion) ||
                         !Mathf.Approximately(datos.inicioVentanaGolpe, ataque.Inicio(2)) ||
                         !Mathf.Approximately(datos.finVentanaGolpe, ataque.Inicio(3));
            if (cambia)
            {
                datos.tinte = Color.white;
                datos.spriteMiraDerecha = true;
                datos.duracionAtaque = ataque.Duracion;
                datos.inicioVentanaGolpe = ataque.Inicio(2);
                datos.finVentanaGolpe = ataque.Inicio(3);
                EditorUtility.SetDirty(datos);
            }

            return true;
        }

        private static GameObject CrearPrefab(Sprite dormida, AnimatorController controlador)
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            var medidas = new EnemigosConArte.Medidas
            {
                Cuerpo = TamanoCuerpo,
                Contacto = TamanoContacto,
                CentroArma = Golpe.center,
                TamanoArma = Golpe.size,
            };

            return EnemigosConArte.CrearPrefab(NombreDatos, RutaPrefab, datos, dormida, controlador, medidas, armazon =>
            {
                Asignar(armazon.Animacion, "restosEnPantalla", p => p.floatValue = RestosEnPantalla);

                // Tocarla no hace daño, y nace dormida: el controlador lo enciende EsculturaDormida.
                armazon.Contacto.gameObject.SetActive(false);
                armazon.Controlador.enabled = false;
                var escultura = armazon.Raiz.AddComponent<EsculturaDormida>();
                Asignar(escultura, "sprite", p => p.objectReferenceValue = armazon.Sprite);
                Asignar(escultura, "animator", p => p.objectReferenceValue = armazon.Animator);
                // Con arte no se tiñe: dormida y despierta son dibujos distintos.
                Asignar(escultura, "colorDormida", p => p.colorValue = Color.white);
            });
        }
    }
}
