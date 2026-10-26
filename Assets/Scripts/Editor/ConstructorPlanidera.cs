using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// La Plañidera, el mini-jefe del Mundo 2, con su arte: un espectro encapuchado, con los ojos
    /// vendados, el corazón a la vista y un arco de esquirlas a la espalda.
    /// 1. importa sus 16 fotogramas y los 3 de la esquirla (<see cref="Reglas"/>);
    /// 2. crea sus clips: flotar (F1 a F4), el barrido (A1 a A3), los cinco momentos de las
    ///    esquirlas (E1 a E5, cada uno se queda mientras dura su estado) y la muerte (M1 a M4);
    /// 3. arma su controlador: el estado de <see cref="Planidera"/> (un entero) manda mientras
    ///    maneja las esquirlas y, si no, los parámetros de la FSM del enemigo;
    /// 4. monta el prefab de la esquirla (un <see cref="Proyectil"/>) y el de la Plañidera.
    ///
    /// <b>Está dibujada mirando a la izquierda.</b> Su <see cref="SpriteRenderer"/> va volteado
    /// (<c>flipX</c>), así que en el juego mira a la derecha como los demás, y sus medidas están
    /// en ese espacio. La esquirla también: su punta va por delante.
    ///
    /// <b>A2, el barrido, llegó en otro lienzo</b> (286×170). Está dibujada al mismo tamaño que el
    /// resto (el corazón mide lo mismo), así que va a la misma escala; su pivote se coloca para que
    /// el corazón no se mueva del sitio entre A1, A2 y A3.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo2/Planidera</c>. Idempotente.
    /// </summary>
    public static class ConstructorPlanidera
    {
        public const string CarpetaSprites = "Assets/Sprites/Enemigos/Planidera";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Enemigos/Planidera";
        public const string NombreDatos = "Planidera";
        public const string RutaControlador = CarpetaAnimaciones + "/Planidera.controller";
        public const string RutaControladorEsquirla = CarpetaAnimaciones + "/Esquirla.controller";
        public const string RutaPrefab = GeneradorEnemigos.CarpetaPrefabs + "/" + NombreDatos + ".prefab";
        public const string RutaPrefabEsquirla = GeneradorEnemigos.CarpetaPrefabs + "/Planidera_Esquirla.prefab";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        // --- Escala y pivotes ---

        public static readonly Vector2Int Lienzo = new Vector2Int(203, 198);
        public static readonly Vector2Int LienzoBarrido = new Vector2Int(286, 170);
        public static readonly Vector2Int LienzoEsquirla = new Vector2Int(101, 22);
        public static readonly Vector2Int LienzoEsquirlaRota = new Vector2Int(104, 91);

        /// <summary>Mide 180 píxeles: a 52 por unidad (la escala del Nivel 1), 3,5 u, como el Golem.</summary>
        public const float PixelesPorUnidad = 52f;

        /// <summary>
        /// El ancla, en el lienzo de 203×198 y desde arriba a la izquierda: bajo el centro de la
        /// capucha (x 88) y donde acaba la cola (fila 187). El corazón queda en (77,4, 62,3).
        /// </summary>
        public static readonly Vector2 Ancla = new Vector2(88f, 188f);

        /// <summary>El centro del corazón en los fotogramas que comparten lienzo.</summary>
        public static readonly Vector2 Corazon = new Vector2(77.4f, 62.3f);

        /// <summary>El centro del corazón en A2.</summary>
        public static readonly Vector2 CorazonBarrido = new Vector2(111.1f, 76.8f);

        /// <summary>El ancla de A2: la que deja su corazón donde está el de los demás.</summary>
        public static Vector2 AnclaBarrido => CorazonBarrido + (Ancla - Corazon);

        /// <summary>
        /// La esquirla volando mide unos 42 píxeles y, en el arco de la Plañidera, unos 14: a tres
        /// veces su escala se vería como las que lleva. Va a dos (2026-10-12): vez y media más
        /// grande, porque al probar el nivel costaba verlas venir.
        /// </summary>
        public const float PixelesPorUnidadEsquirla = PixelesPorUnidad * 2f;

        /// <summary>El centro de la piedra (x 0-40, y 0-20): la estela queda detrás.</summary>
        public static readonly Vector2 AnclaEsquirla = new Vector2(20f, 11f);

        private static Vector2 Pivote(Vector2 ancla, Vector2Int lienzo) => new Vector2(ancla.x / lienzo.x, (lienzo.y - ancla.y) / lienzo.y);

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(CarpetaSprites + "/", PixelesPorUnidad, Pivote(Ancla, Lienzo),
                "Plañidera: 15 fotogramas de 203×198 con el pivote bajo la capucha, donde acaba la cola; 3,5 u de alto.", Lienzo),
            new TablaArte.Regla(RutaSprite("A2"), PixelesPorUnidad, Pivote(AnclaBarrido, LienzoBarrido),
                "Plañidera, el barrido: en otro lienzo (286×170). El pivote deja su corazón donde está el de los demás; cae fuera del lienzo.",
                LienzoBarrido),
            new TablaArte.Regla(CarpetaSprites + "/Esquirla", PixelesPorUnidadEsquirla, Pivote(AnclaEsquirla, LienzoEsquirla),
                "Esquirla de la Plañidera, volando: 101×22, el pivote en el centro de la piedra. Al doble de la escala de su dueña.",
                LienzoEsquirla),
            new TablaArte.Regla(RutaSprite("Esquirla3"), PixelesPorUnidadEsquirla, null,
                "Esquirla de la Plañidera, rota: 104×91, el pivote en el centro.", LienzoEsquirlaRota),
        };

        // --- Animaciones ---

        public const string Flotar = "Flotar";
        public const string Barrido = "Barrido";
        public const string Alza = "Alza";
        public const string Apunta = "Apunta";
        public const string Lanza = "Lanza";
        public const string Desarmada = "Desarmada";
        public const string Rehace = "Rehace";
        public const string Muerte = "Muerte";

        /// <summary>
        /// - Flota siempre, quieta o acechando: no tiene otro paso.
        /// - El barrido son los tiempos de sus datos: A1 echa el brazo atrás 0,55 s (el aviso), A2
        ///   barre 0,15 s y A3 se recoge 0,40 s.
        /// - Los momentos de las esquirlas duran lo que diga <see cref="Planidera"/>: cada clip es
        ///   un fotograma que se queda.
        /// - M4, la venda que cae, se queda en pantalla al acabar la muerte.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Planidera_Flotar", Estado = Flotar, Fotogramas = new[] { "F1", "F2", "F3", "F4" }, Duraciones = new[] { 0.18f, 0.18f, 0.18f, 0.18f }, Bucle = true },
            new Clip { Nombre = "Planidera_Barrido", Estado = Barrido, Fotogramas = new[] { "A1", "A2", "A3" }, Duraciones = new[] { 0.55f, 0.15f, 0.40f } },
            new Clip { Nombre = "Planidera_Alza", Estado = Alza, Fotogramas = new[] { "E1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Planidera_Apunta", Estado = Apunta, Fotogramas = new[] { "E2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Planidera_Lanza", Estado = Lanza, Fotogramas = new[] { "E3" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Planidera_Desarmada", Estado = Desarmada, Fotogramas = new[] { "E4" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Planidera_Rehace", Estado = Rehace, Fotogramas = new[] { "E5" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Planidera_Muerte", Estado = Muerte, Fotogramas = new[] { "M1", "M2", "M3", "M4" }, Duraciones = new[] { 0.25f, 0.30f, 0.45f, ClipsDeSprites.SeQueda } },
        };

        /// <summary>El estado del Animator de cada momento de las esquirlas.</summary>
        public static readonly (ESTADOS_PLANIDERA estado, string animacion)[] Esquirlas =
        {
            (ESTADOS_PLANIDERA.ALZA, Alza),
            (ESTADOS_PLANIDERA.APUNTA, Apunta),
            (ESTADOS_PLANIDERA.LANZA, Lanza),
            (ESTADOS_PLANIDERA.DESARMADA, Desarmada),
            (ESTADOS_PLANIDERA.REHACE, Rehace),
        };

        public const string Vuelo = "Vuelo";

        /// <summary>La esquirla en el aire: la estela parpadea. Al romperse deja <see cref="FotogramaEsquirlaRota"/>.</summary>
        public static readonly Clip EsquirlaVuelo = new Clip
        {
            Nombre = "Esquirla_Vuelo", Estado = Vuelo, Fotogramas = new[] { "Esquirla1", "Esquirla2" }, Duraciones = new[] { 0.08f, 0.08f }, Bucle = true,
        };

        public const string FotogramaEsquirlaRota = "Esquirla3";
        public const float DuracionEsquirlaRota = 0.2f;

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        /// <summary>Segundos que la venda (M4) se queda en pantalla antes de desvanecerse.</summary>
        public const float RestosEnPantalla = 0.6f;

        // --- Geometría (u, desde el ancla y mirando a la derecha) ---

        public static Vector2 TamanoCuerpo => GeneradorEnemigos.TamanoPlanidera;

        public static readonly Vector2 TamanoContacto = new Vector2(1.1f, 2.7f);

        /// <summary>
        /// El barrido de A2: el arco blanco va de 0,5 a 2,1 u por delante y de 0,7 a 2 u de alto, y
        /// la mano llega a 2,2 u y a 3 de alto. La zona empieza en su cuerpo (x 0), como en la fase
        /// C: pegarse a ella no esquiva el barrido.
        /// </summary>
        public static readonly Rect Barre = Rect.MinMaxRect(0f, 0.5f, 2.2f, 2.6f);

        /// <summary>De dónde salen las esquirlas: la punta de la fila de E2, detrás de la capucha.</summary>
        public static readonly Vector2 Arco = new Vector2(-0.2f, 2.9f);

        /// <summary>El radio de la esquirla: la piedra mide 0,4 × 0,2 u.</summary>
        public const float RadioEsquirla = 0.18f;

        // --- Construcción ---

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El prefab de la Plañidera con arte, o null si todavía no se construyó.</summary>
        public static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

        public static Proyectil PrefabEsquirla
        {
            get
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabEsquirla);
                return prefab != null ? prefab.GetComponent<Proyectil>() : null;
            }
        }

        public static bool Construir()
        {
            var todos = Clips.Append(EsquirlaVuelo).Append(new Clip { Fotogramas = new[] { FotogramaEsquirlaRota }, Duraciones = new[] { DuracionEsquirlaRota } });
            if (!ClipsDeSprites.CargarSprites(todos, RutaSprite, "la Plañidera", out var sprites))
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

            var esquirla = CrearEsquirla(sprites);
            var ok = esquirla != null && CrearPrefab(sprites[ClipDe(Flotar).Fotogramas[0]], controlador, esquirla) != null;
            Debug.Log($"[Fragmentum] Plañidera: {sprites.Count} fotogramas, {Clips.Length + 1} clips, {RutaControlador}, {RutaPrefabEsquirla} y {RutaPrefab}.");
            return ok;
        }

        /// <summary>El barrido hace daño solo en A2; la muerte se remata al llegar a la venda (M4).</summary>
        public static IEnumerable<AnimationEvent> EventosDe(Clip clip)
        {
            switch (clip.Estado)
            {
                case Barrido:
                    return new[]
                    {
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.AbrirGolpe), clip.Inicio(1)),
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.CerrarGolpe), clip.Inicio(2)),
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.FinAtaque), clip.Duracion),
                    };
                case Muerte:
                    return new[] { EnemigosConArte.Evento(nameof(EnemigoAnimacion.FinMuerte), clip.Inicio(3)) };
                default:
                    return Enumerable.Empty<AnimationEvent>();
            }
        }

        /// <summary>
        /// - Flota por defecto y barre en cuanto la FSM entra en ATTACK.
        /// - Con las esquirlas manda el entero «Estado» (<see cref="ESTADOS_PLANIDERA"/>): cada
        ///   valor lleva a su fotograma desde cualquier estado, y al volver a 0 (ACECHA) flota.
        /// - La muerte va primero e interrumpe todo; no tiene salida (se queda en M4).
        /// </summary>
        private static AnimatorController ArmarControlador(IReadOnlyDictionary<string, Motion> clips)
        {
            var controlador = ControladoresAnimator.CargarOCrear(RutaControlador);
            ControladoresAnimator.AsegurarParametros(controlador,
                (AnimatorParams.EstadoMecanica.Nombre, AnimatorControllerParameterType.Int, 0f),
                (AnimatorParams.MovEn.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Attack.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Damage.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Death.Nombre, AnimatorControllerParameterType.Bool, 0f));

            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, Clips.Select(c => (c.Estado, clips[c.Estado])).ToList());

            var estado = AnimatorParams.EstadoMecanica.Nombre;
            AnimatorCondition Es(ESTADOS_PLANIDERA valor) => ControladoresAnimator.Condicion(AnimatorConditionMode.Equals, estado, (int)valor);
            AnimatorCondition Si(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.If, p.Nombre);
            AnimatorCondition No(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.IfNot, p.Nombre);

            ControladoresAnimator.Transiciones(estados[Flotar],
                new ControladoresAnimator.Transicion("Ya a tiro", estados[Barrido], null, Si(AnimatorParams.Attack), Es(ESTADOS_PLANIDERA.ACECHA)));
            ControladoresAnimator.Transiciones(estados[Barrido],
                new ControladoresAnimator.Transicion("Termina el barrido", estados[Flotar], 1f),
                new ControladoresAnimator.Transicion("Barrido interrumpido", estados[Flotar], null, No(AnimatorParams.Attack)));
            foreach (var (_, animacion) in Esquirlas)
            {
                ControladoresAnimator.Transiciones(estados[animacion],
                    new ControladoresAnimator.Transicion("Vuelve a acechar", estados[Flotar], null, Es(ESTADOS_PLANIDERA.ACECHA)));
            }

            ControladoresAnimator.Transiciones(estados[Muerte]);

            var desdeCualquiera = new List<ControladoresAnimator.Transicion>
            {
                new ControladoresAnimator.Transicion("Muere", estados[Muerte], null, Si(AnimatorParams.Death)),
            };
            desdeCualquiera.AddRange(Esquirlas.Select(e =>
                new ControladoresAnimator.Transicion(e.animacion, estados[e.animacion], null, Es(e.estado), No(AnimatorParams.Death))));
            ControladoresAnimator.DesdeCualquierEstado(maquina, desdeCualquiera.ToArray());

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        /// <summary>
        /// Lo que el arte cambia en sus datos: tinte blanco y los tiempos del barrido como en el
        /// clip (ya eran esos). Mira a la derecha porque su sprite va volteado.
        /// </summary>
        public static bool AjustarDatos()
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            if (datos == null)
            {
                Debug.LogError($"[Fragmentum] Falta {GeneradorEnemigos.CarpetaDatos}/{NombreDatos}.asset.");
                return false;
            }

            var barrido = ClipDe(Barrido);
            var cambia = datos.tinte != Color.white || !datos.spriteMiraDerecha ||
                         !Mathf.Approximately(datos.duracionAtaque, barrido.Duracion) ||
                         !Mathf.Approximately(datos.inicioVentanaGolpe, barrido.Inicio(1)) ||
                         !Mathf.Approximately(datos.finVentanaGolpe, barrido.Inicio(2));
            if (cambia)
            {
                datos.tinte = Color.white;
                datos.spriteMiraDerecha = true;
                datos.duracionAtaque = barrido.Duracion;
                datos.inicioVentanaGolpe = barrido.Inicio(1);
                datos.finVentanaGolpe = barrido.Inicio(2);
                EditorUtility.SetDirty(datos);
            }

            return true;
        }

        // --- La esquirla ---

        private static Proyectil CrearEsquirla(IReadOnlyDictionary<string, Sprite> sprites)
        {
            var clip = ClipsDeSprites.Crear(CarpetaAnimaciones, EsquirlaVuelo, sprites);
            var controlador = ControladoresAnimator.CargarOCrear(RutaControladorEsquirla);
            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, new[] { (EsquirlaVuelo.Estado, (Motion)clip) });
            ControladoresAnimator.Transiciones(estados[EsquirlaVuelo.Estado]);
            EditorUtility.SetDirty(controlador);

            var raiz = new GameObject("Planidera_Esquirla") { layer = Capa(Capas.EnemyWeapon) };
            try
            {
                var cuerpo = raiz.AddComponent<Rigidbody2D>();
                cuerpo.bodyType = RigidbodyType2D.Kinematic;
                cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
                var zona = raiz.AddComponent<CircleCollider2D>();
                zona.isTrigger = true;
                zona.radius = RadioEsquirla;
                var hitbox = raiz.AddComponent<Hitbox>();
                Asignar(hitbox, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
                var proyectil = raiz.AddComponent<Proyectil>();
                Asignar(proyectil, "restos", p => p.objectReferenceValue = sprites[FotogramaEsquirlaRota]);
                Asignar(proyectil, "duracionRestos", p => p.floatValue = DuracionEsquirlaRota);

                // Dibujada con la punta a la izquierda: volteada, la punta va por delante.
                var dibujo = new GameObject("Sprite") { layer = raiz.layer };
                dibujo.transform.SetParent(raiz.transform, false);
                var sprite = dibujo.AddComponent<SpriteRenderer>();
                sprite.sprite = sprites[EsquirlaVuelo.Fotogramas[0]];
                sprite.flipX = true;
                sprite.sortingOrder = 8;
                var animator = dibujo.AddComponent<Animator>();
                animator.runtimeAnimatorController = controlador;
                animator.applyRootMotion = false;

                var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabEsquirla, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {RutaPrefabEsquirla}.");
                    return null;
                }

                return prefab.GetComponent<Proyectil>();
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        // --- El prefab ---

        private static GameObject CrearPrefab(Sprite reposo, AnimatorController controlador, Proyectil esquirla)
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            var medidas = new EnemigosConArte.Medidas
            {
                Cuerpo = TamanoCuerpo,
                Contacto = TamanoContacto,
                CentroArma = Barre.center,
                TamanoArma = Barre.size,
            };

            return EnemigosConArte.CrearPrefab(NombreDatos, RutaPrefab, datos, reposo, controlador, medidas, armazon =>
            {
                // Dibujada mirando a la izquierda: volteada, mira a la derecha como los demás.
                armazon.Sprite.flipX = true;
                Asignar(armazon.Animacion, "restosEnPantalla", p => p.floatValue = RestosEnPantalla);

                // Tocarla no hace daño.
                armazon.Contacto.gameObject.SetActive(false);

                var arco = new GameObject("Arco").transform;
                arco.SetParent(armazon.Pivote, false);
                arco.localPosition = Arco;

                var planidera = armazon.Raiz.AddComponent<Planidera>();
                Asignar(planidera, "arco", p => p.objectReferenceValue = arco);
                Asignar(planidera, "pivote", p => p.objectReferenceValue = armazon.Pivote);
                Asignar(planidera, "sprite", p => p.objectReferenceValue = armazon.Sprite);
                Asignar(planidera, "animator", p => p.objectReferenceValue = armazon.Animator);
                Asignar(planidera, "esquirla", p => p.objectReferenceValue = esquirla);
                // Con arte no se tiñe: el aviso y el llanto son dibujos (E2 y E4).
                Asignar(planidera, "colorAviso", p => p.colorValue = Color.white);
                Asignar(planidera, "colorDesarmada", p => p.colorValue = Color.white);
            });
        }
    }
}
