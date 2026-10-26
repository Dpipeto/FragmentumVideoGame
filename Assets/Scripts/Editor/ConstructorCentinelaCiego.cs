using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;
using Transicion = Fragmentum.EditorTools.ControladoresAnimator.Transicion;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El Centinela Ciego, con su arte: un centinela de trinchera con máscara de gas y los cristales
    /// pintados, que patrulla con la bayoneta por delante.
    /// 1. importa sus 13 fotogramas, todos en el mismo lienzo (140×118) y con el pivote entre los pies;
    /// 2. crea sus clips: quieto, andar, escuchar (con la mano en la máscara), ir hacia el ruido,
    ///    la estocada, buscar, el golpe recibido y la muerte (se queda el abrigo vacío);
    /// 3. arma su controlador: el estado de su oído (<see cref="CentinelaCiego"/>, el entero
    ///    «Estado») manda mientras oye, va, apuñala y busca; de ronda, los parámetros de su FSM;
    /// 4. monta su prefab.
    ///
    /// <b>Está dibujado mirando a la izquierda:</b> su <see cref="SpriteRenderer"/> va volteado, como
    /// el de la Plañidera, y sus medidas están mirando a la derecha.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo3/Centinela</c>. Idempotente.
    /// </summary>
    public static class ConstructorCentinelaCiego
    {
        public const string CarpetaSprites = "Assets/Sprites/Enemigos/CentinelaCiego";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Enemigos/CentinelaCiego";
        public const string NombreDatos = "CentinelaCiego";
        public const string RutaControlador = CarpetaAnimaciones + "/CentinelaCiego.controller";
        public const string RutaPrefab = GeneradorEnemigos.CarpetaPrefabs + "/" + NombreDatos + ".prefab";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        // --- Escala y pivote ---

        public static readonly Vector2Int Lienzo = new Vector2Int(140, 118);

        /// <summary>Mide 117 píxeles del suelo al casco: a 58 por unidad, 2 u, algo más alto que Kael (1,8).</summary>
        public const float PixelesPorUnidad = 58f;

        /// <summary>Entre los pies (andando, x 56-71 y 110-120) y en la fila donde pisa (116).</summary>
        public static readonly Vector2 Ancla = new Vector2(88f, 117f);

        public static Vector2 Pivote => new Vector2(Ancla.x / Lienzo.x, (Lienzo.y - Ancla.y) / Lienzo.y);

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(CarpetaSprites + "/", PixelesPorUnidad, Pivote,
                "Centinela Ciego: 13 fotogramas de 140×118 con el pivote entre los pies; 2 u de alto.", Lienzo),
        };

        // --- Animaciones ---

        public const string Quieto = "Quieto";
        public const string Andar = "Andar";
        public const string Escucha = "Escucha";
        public const string Va = "Va";
        public const string Estocada = "Estocada";
        public const string Busca = "Busca";
        public const string Dano = "Dano";
        public const string Muerte = "Muerte";

        /// <summary>
        /// - Anda con sus cuatro pasos, al ritmo de su cuerpo (<see cref="VelocidadNaturalAndar"/>):
        ///   de ronda despacio y, hacia el ruido, cuatro veces más deprisa.
        /// - Escucha: L1 y se queda en L2 (el aviso, 0,6 s). Busca: L2 y L1, en bucle.
        /// - La estocada son los tiempos de sus datos: A1 echa el fusil atrás 0,3 s, A2 apuñala
        ///   0,15 s (la ventana del daño) y A3 se recoge.
        /// - Muere de rodillas y se queda el abrigo vacío (M3).
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "CentinelaCiego_Quieto", Estado = Quieto, Fotogramas = new[] { "W2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "CentinelaCiego_Andar", Estado = Andar, Fotogramas = new[] { "W1", "W2", "W3", "W4" }, Duraciones = new[] { 0.2f, 0.2f, 0.2f, 0.2f }, Bucle = true },
            new Clip { Nombre = "CentinelaCiego_Escucha", Estado = Escucha, Fotogramas = new[] { "L1", "L2" }, Duraciones = new[] { 0.3f, ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "CentinelaCiego_Va", Estado = Va, Fotogramas = new[] { "W1", "W2", "W3", "W4" }, Duraciones = new[] { 0.2f, 0.2f, 0.2f, 0.2f }, Bucle = true },
            new Clip { Nombre = "CentinelaCiego_Estocada", Estado = Estocada, Fotogramas = new[] { "A1", "A2", "A3" }, Duraciones = new[] { 0.30f, 0.15f, 0.15f } },
            new Clip { Nombre = "CentinelaCiego_Busca", Estado = Busca, Fotogramas = new[] { "L2", "L1" }, Duraciones = new[] { 0.45f, 0.45f }, Bucle = true },
            new Clip { Nombre = "CentinelaCiego_Dano", Estado = Dano, Fotogramas = new[] { "H1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "CentinelaCiego_Muerte", Estado = Muerte, Fotogramas = new[] { "M1", "M2", "M3" }, Duraciones = new[] { 0.30f, 0.35f, ClipsDeSprites.SeQueda } },
        };

        /// <summary>El estado del Animator de cada estado de su oído.</summary>
        public static readonly (ESTADOS_CENTINELA_CIEGO estado, string animacion)[] Oido =
        {
            (ESTADOS_CENTINELA_CIEGO.ESCUCHA, Escucha),
            (ESTADOS_CENTINELA_CIEGO.VA, Va),
            (ESTADOS_CENTINELA_CIEGO.ESTOCADA, Estocada),
            (ESTADOS_CENTINELA_CIEGO.BUSCA, Busca),
        };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        /// <summary>A esta velocidad (u/s, la de su ronda) el paso no patina con el clip a ritmo 1.</summary>
        public const float VelocidadNaturalAndar = 1f;

        /// <summary>Segundos que el abrigo vacío (M3) se queda en pantalla antes de desvanecerse.</summary>
        public const float RestosEnPantalla = 0.8f;

        // El polvo negro que sale de la máscara al caer.
        public static readonly Color ColorPolvo = new Color32(10, 10, 15, 255);
        public static readonly Color ColorAbrigo = new Color32(58, 58, 74, 255);

        // --- Geometría (u, desde el pivote y mirando a la derecha) ---

        /// <summary>Su cuerpo: delgado y algo más alto que Kael.</summary>
        public static readonly Vector2 TamanoCuerpo = new Vector2(0.8f, 1.9f);

        public static readonly Vector2 TamanoContacto = new Vector2(0.7f, 1.8f);

        /// <summary>La bayoneta (A2): de 0,35 a 1,45 u por delante, a la altura de la cadera.</summary>
        public static readonly Vector2 CentroArma = new Vector2(0.9f, 0.9f);

        public static readonly Vector2 TamanoArma = new Vector2(1.1f, 0.6f);

        /// <summary>De dónde sale el polvo al morir: la máscara.</summary>
        public static readonly Vector2 Mascara = new Vector2(0.15f, 1.6f);

        // --- Construcción ---

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El prefab del Centinela Ciego con arte, o null si todavía no se construyó.</summary>
        public static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "el Centinela Ciego", out var sprites))
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

            var ok = CrearPrefab(sprites[ClipDe(Quieto).Fotogramas[0]], controlador) != null;
            Debug.Log($"[Fragmentum] Centinela Ciego: {sprites.Count} fotogramas, {Clips.Length} clips, {RutaControlador} y {RutaPrefab}.");
            return ok;
        }

        /// <summary>
        /// La estocada avisa al controlador cuando la lanza él (lo rozan de ronda): hace daño en A2 y
        /// avisa al acabar. Cuando apuñala su oído, los tiempos los lleva <see cref="CentinelaCiego"/>
        /// y el controlador no los escucha (no está en ATTACK). La muerte se remata en el abrigo vacío.
        /// </summary>
        public static IEnumerable<AnimationEvent> EventosDe(Clip clip)
        {
            switch (clip.Estado)
            {
                case Estocada:
                    return new[]
                    {
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.AbrirGolpe), clip.Inicio(1)),
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.CerrarGolpe), clip.Inicio(2)),
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.FinAtaque), clip.Duracion),
                    };
                case Muerte:
                    return new[] { EnemigosConArte.Evento(nameof(EnemigoAnimacion.FinMuerte), clip.Inicio(2)) };
                default:
                    return Enumerable.Empty<AnimationEvent>();
            }
        }

        /// <summary>
        /// - De ronda (Estado 0) manda su FSM: anda si se mueve, apuñala si lo rozan y se encoge si le
        ///   pegan.
        /// - Su oído manda desde cualquier estado: cada valor del entero lleva a su animación, y al
        ///   volver a 0 se queda quieto. La estocada de su oído se queda en A3 hasta que pasa a buscar.
        /// - La muerte va primero e interrumpe todo; no tiene salida (se queda en M3).
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
                (AnimatorParams.VelocidadCorrer.Nombre, AnimatorControllerParameterType.Float, 1f));

            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, Clips.Select(c => (c.Estado, clips[c.Estado])).ToList(),
                new Dictionary<string, string>
                {
                    [Andar] = AnimatorParams.VelocidadCorrer.Nombre,
                    [Va] = AnimatorParams.VelocidadCorrer.Nombre,
                });

            var estado = AnimatorParams.EstadoMecanica.Nombre;
            AnimatorCondition Es(ESTADOS_CENTINELA_CIEGO valor) => ControladoresAnimator.Condicion(AnimatorConditionMode.Equals, estado, (int)valor);
            AnimatorCondition Si(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.If, p.Nombre);
            AnimatorCondition No(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.IfNot, p.Nombre);
            var ronda = Es(ESTADOS_CENTINELA_CIEGO.RONDA);

            ControladoresAnimator.Transiciones(estados[Quieto],
                new Transicion("Ya a tiro", estados[Estocada], null, Si(AnimatorParams.Attack), ronda),
                new Transicion("Le pegan", estados[Dano], null, Si(AnimatorParams.Damage), ronda),
                new Transicion("Anda", estados[Andar], null, Si(AnimatorParams.MovEn), ronda));
            ControladoresAnimator.Transiciones(estados[Andar],
                new Transicion("Ya a tiro", estados[Estocada], null, Si(AnimatorParams.Attack), ronda),
                new Transicion("Le pegan", estados[Dano], null, Si(AnimatorParams.Damage), ronda),
                new Transicion("Se para", estados[Quieto], null, No(AnimatorParams.MovEn), No(AnimatorParams.Attack)));
            ControladoresAnimator.Transiciones(estados[Dano],
                new Transicion("Se recupera", estados[Quieto], null, No(AnimatorParams.Damage)));
            ControladoresAnimator.Transiciones(estados[Estocada],
                new Transicion("Termina la estocada", estados[Quieto], 1f, ronda),
                new Transicion("Estocada interrumpida", estados[Quieto], null, No(AnimatorParams.Attack), ronda));
            foreach (var (_, animacion) in Oido.Where(o => o.animacion != Estocada))
            {
                ControladoresAnimator.Transiciones(estados[animacion],
                    new Transicion("Vuelve a su ronda", estados[Quieto], null, ronda));
            }

            ControladoresAnimator.Transiciones(estados[Muerte]);

            var desdeCualquiera = new List<Transicion> { new Transicion("Muere", estados[Muerte], null, Si(AnimatorParams.Death)) };
            desdeCualquiera.AddRange(Oido.Select(o => new Transicion(o.animacion, estados[o.animacion], null, Es(o.estado), No(AnimatorParams.Death))));
            ControladoresAnimator.DesdeCualquierEstado(maquina, desdeCualquiera.ToArray());

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        /// <summary>
        /// Lo que el arte cambia en sus datos: tinte blanco (los colores son del dibujo) y mira a la
        /// derecha (su sprite va volteado). Los tiempos de la estocada son los del clip, que ya eran
        /// los suyos.
        /// </summary>
        public static bool AjustarDatos()
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            if (datos == null)
            {
                Debug.LogError($"[Fragmentum] Falta {GeneradorEnemigos.CarpetaDatos}/{NombreDatos}.asset.");
                return false;
            }

            var estocada = ClipDe(Estocada);
            var cambia = datos.tinte != Color.white || !datos.spriteMiraDerecha ||
                         !Mathf.Approximately(datos.duracionAtaque, estocada.Duracion) ||
                         !Mathf.Approximately(datos.inicioVentanaGolpe, estocada.Inicio(1)) ||
                         !Mathf.Approximately(datos.finVentanaGolpe, estocada.Inicio(2));
            if (cambia)
            {
                datos.tinte = Color.white;
                datos.spriteMiraDerecha = true;
                datos.duracionAtaque = estocada.Duracion;
                datos.inicioVentanaGolpe = estocada.Inicio(1);
                datos.finVentanaGolpe = estocada.Inicio(2);
                EditorUtility.SetDirty(datos);
            }

            return true;
        }

        private static GameObject CrearPrefab(Sprite reposo, AnimatorController controlador)
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            var particulas = AssetDatabase.LoadAssetAtPath<Material>(ConstructorEcoIra.RutaMaterialParticulas);
            var medidas = new EnemigosConArte.Medidas
            {
                Cuerpo = TamanoCuerpo,
                Contacto = TamanoContacto,
                CentroArma = CentroArma,
                TamanoArma = TamanoArma,
            };

            return EnemigosConArte.CrearPrefab(NombreDatos, RutaPrefab, datos, reposo, controlador, medidas, armazon =>
            {
                // Dibujado mirando a la izquierda: volteado, mira a la derecha como los demás.
                armazon.Sprite.flipX = true;

                var estallido = particulas != null
                    ? ConstructorEcoIra.CrearEstallido(armazon.Pivote, armazon.Raiz.layer, particulas, Mascara,
                        ColorPolvo, ColorAbrigo, 1f / PixelesPorUnidad, 0.4f)
                    : null;
                Asignar(armazon.Animacion, "velocidadNaturalCarrera", p => p.floatValue = VelocidadNaturalAndar);
                Asignar(armazon.Animacion, "estallido", p => p.objectReferenceValue = estallido);
                Asignar(armazon.Animacion, "restosEnPantalla", p => p.floatValue = RestosEnPantalla);

                var oido = armazon.Raiz.AddComponent<CentinelaCiego>();
                Asignar(oido, "pivote", p => p.objectReferenceValue = armazon.Pivote);
                Asignar(oido, "sprite", p => p.objectReferenceValue = armazon.Sprite);
                Asignar(oido, "animator", p => p.objectReferenceValue = armazon.Animator);
            });
        }
    }
}
