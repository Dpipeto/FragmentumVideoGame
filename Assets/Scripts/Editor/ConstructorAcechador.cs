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
using Transicion = Fragmentum.EditorTools.ControladoresAnimator.Transicion;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El Acechador, el mini-jefe del Mundo 3, con su arte: un lobo enorme de sombra, sin ojos, con
    /// el aliento verde y la grupa deshaciéndose en polvo.
    /// 1. importa sus 17 fotogramas: los 12 que comparten lienzo (220×107), el mordisco B2 (en otro
    ///    lienzo, 207×103) y los cuatro de la muerte (en otro lienzo y otra rejilla, 224×128);
    /// 2. crea sus clips: quieto (escuchando), merodear, el mordisco, agazaparse, la embestida,
    ///    aturdido y la muerte;
    /// 3. arma su controlador: el estado de <see cref="Acechador"/> (el entero «Estado») manda
    ///    mientras se agazapa, embiste, está aturdido o frena; merodeando, los parámetros de su FSM;
    /// 4. monta su prefab, con la zona de daño de la embestida.
    ///
    /// <b>Está dibujado mirando a la izquierda:</b> su <see cref="SpriteRenderer"/> va volteado.
    ///
    /// <b>Su muerte llegó con el píxel un 7 % más fino</b> (240 píxeles de arte a lo ancho de su
    /// lienzo y no 222): va a su propia escala, para que mida lo mismo que el resto. En la
    /// embestida (C1, C2) la estela de polvo acaba cortada por el borde del lienzo, como llegó.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo3/Acechador</c>. Idempotente.
    /// </summary>
    public static class ConstructorAcechador
    {
        public const string CarpetaSprites = "Assets/Sprites/Enemigos/Acechador";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Enemigos/Acechador";
        public const string NombreDatos = "Acechador";
        public const string RutaControlador = CarpetaAnimaciones + "/Acechador.controller";
        public const string RutaPrefab = GeneradorEnemigos.CarpetaPrefabs + "/" + NombreDatos + ".prefab";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        // --- Escala y pivotes ---

        public static readonly Vector2Int Lienzo = new Vector2Int(220, 107);
        public static readonly Vector2Int LienzoMordisco = new Vector2Int(207, 103);
        public static readonly Vector2Int LienzoMuerte = new Vector2Int(224, 128);

        /// <summary>Mide 104 píxeles de las patas a las orejas: a 65 por unidad, 1,6 u; y 3,2 del hocico a la cola.</summary>
        public const float PixelesPorUnidad = 65f;

        /// <summary>Su muerte, con el píxel 12,252 / 11,467 veces más fino: a esta escala mide lo mismo.</summary>
        public const float PixelesPorUnidadMuerte = PixelesPorUnidad * 12.252f / 11.467f;

        /// <summary>Entre las patas de delante (x 33) y las de atrás (x 175), en la fila donde pisa (105).</summary>
        public static readonly Vector2 Ancla = new Vector2(104f, 106f);

        /// <summary>B2: sus patas están 3 píxeles más a la izquierda y su lienzo, más bajo.</summary>
        public static readonly Vector2 AnclaMordisco = new Vector2(101f, 103f);

        /// <summary>La muerte: el ancla de los demás llevada a su lienzo (de 2720 a 2752 px de ancho).</summary>
        public static readonly Vector2 AnclaMuerte = new Vector2(110.5f, 127f);

        private static Vector2 Pivote(Vector2 ancla, Vector2Int lienzo) => new Vector2(ancla.x / lienzo.x, (lienzo.y - ancla.y) / lienzo.y);

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(CarpetaSprites + "/", PixelesPorUnidad, Pivote(Ancla, Lienzo),
                "Acechador: 12 fotogramas de 220×107 con el pivote entre las patas; 1,6 u de alto y 3,2 de largo.", Lienzo),
            new TablaArte.Regla(RutaSprite("B2"), PixelesPorUnidad, Pivote(AnclaMordisco, LienzoMordisco),
                "Acechador, el mordisco: en otro lienzo (207×103), con el mismo píxel. El pivote, entre sus patas.", LienzoMordisco),
            new TablaArte.Regla(CarpetaSprites + "/X", PixelesPorUnidadMuerte, Pivote(AnclaMuerte, LienzoMuerte),
                "Acechador, la muerte: 224×128, con el píxel un 7 % más fino; a su escala mide lo mismo que el resto.", LienzoMuerte),
        };

        // --- Animaciones ---

        public const string Quieto = "Quieto";
        public const string Merodea = "Merodea";
        public const string Mordisco = "Mordisco";
        public const string Agazapado = "Agazapado";
        public const string Embiste = "Embiste";
        public const string Aturdido = "Aturdido";
        public const string Frena = "Frena";
        public const string Muerte = "Muerte";

        /// <summary>
        /// - Quieto, escucha con la cabeza alta (E1); merodea con sus cuatro pasos al ritmo de su cuerpo.
        /// - El mordisco son los tiempos de sus datos: echa la cabeza atrás 0,5 s (B1) y muerde (B2).
        /// - Agazapado: G1 y se queda en G2, con el aliento (el aviso, 0,7 s). Embiste al galope.
        /// - Aturdido sacude la cabeza en bucle; al frenar, vuelve a escuchar (E1).
        /// - Muere en cuatro tiempos y se queda el montón de polvo (X4).
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Acechador_Quieto", Estado = Quieto, Fotogramas = new[] { "E1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Acechador_Merodea", Estado = Merodea, Fotogramas = new[] { "P1", "P2", "P3", "P4" }, Duraciones = new[] { 0.22f, 0.22f, 0.22f, 0.22f }, Bucle = true },
            new Clip { Nombre = "Acechador_Mordisco", Estado = Mordisco, Fotogramas = new[] { "B1", "B2" }, Duraciones = new[] { 0.50f, 0.40f } },
            new Clip { Nombre = "Acechador_Agazapado", Estado = Agazapado, Fotogramas = new[] { "G1", "G2" }, Duraciones = new[] { 0.35f, ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Acechador_Embiste", Estado = Embiste, Fotogramas = new[] { "C1", "C2" }, Duraciones = new[] { 0.08f, 0.08f }, Bucle = true },
            new Clip { Nombre = "Acechador_Aturdido", Estado = Aturdido, Fotogramas = new[] { "S1", "S2" }, Duraciones = new[] { 0.30f, 0.30f }, Bucle = true },
            new Clip { Nombre = "Acechador_Frena", Estado = Frena, Fotogramas = new[] { "E1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Acechador_Muerte", Estado = Muerte, Fotogramas = new[] { "X1", "X2", "X3", "X4" }, Duraciones = new[] { 0.30f, 0.45f, 0.45f, ClipsDeSprites.SeQueda } },
        };

        /// <summary>El estado del Animator de cada estado de <see cref="Acechador"/> que no es merodear.</summary>
        public static readonly (ESTADOS_ACECHADOR estado, string animacion)[] Caza =
        {
            (ESTADOS_ACECHADOR.AGAZAPADO, Agazapado),
            (ESTADOS_ACECHADOR.EMBISTE, Embiste),
            (ESTADOS_ACECHADOR.ATURDIDO, Aturdido),
            (ESTADOS_ACECHADOR.FRENA, Frena),
        };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        /// <summary>A esta velocidad (u/s, la de merodear) el paso no patina con el clip a ritmo 1.</summary>
        public const float VelocidadNaturalAndar = 1f;

        /// <summary>Segundos que el montón de polvo (X4) se queda en pantalla antes de desvanecerse.</summary>
        public const float RestosEnPantalla = 0.6f;

        // --- Geometría (u, desde el pivote y mirando a la derecha) ---

        /// <summary>De dónde sale el aliento: el hocico, por delante y bajo.</summary>
        public static readonly Vector2 Hocico = new Vector2(1.4f, 0.7f);

        public static readonly Color ColorPolvo = new Color32(10, 10, 15, 255);
        public static readonly Color ColorAliento = new Color32(127, 230, 176, 255);

        // --- Construcción ---

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El prefab del Acechador con arte, o null si todavía no se construyó.</summary>
        public static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "el Acechador", out var sprites))
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

            var ok = CrearPrefab(sprites[ClipDe(Merodea).Fotogramas[0]], controlador) != null;
            Debug.Log($"[Fragmentum] Acechador: {sprites.Count} fotogramas, {Clips.Length} clips, {RutaControlador} y {RutaPrefab}.");
            return ok;
        }

        /// <summary>
        /// El mordisco hace daño de 0,5 a 0,65 s (lo que dicen sus datos: dentro de B2) y avisa al
        /// acabar; la muerte se remata en el montón de polvo.
        /// </summary>
        public static IEnumerable<AnimationEvent> EventosDe(Clip clip)
        {
            switch (clip.Estado)
            {
                case Mordisco:
                    return new[]
                    {
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.AbrirGolpe), clip.Inicio(1)),
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.CerrarGolpe), clip.Inicio(1) + DuracionDelMordisco),
                        EnemigosConArte.Evento(nameof(EnemigoAnimacion.FinAtaque), clip.Duracion),
                    };
                case Muerte:
                    return new[] { EnemigosConArte.Evento(nameof(EnemigoAnimacion.FinMuerte), clip.Inicio(3)) };
                default:
                    return Enumerable.Empty<AnimationEvent>();
            }
        }

        /// <summary>Lo que muerde: las mandíbulas cierran al empezar B2 y dejan de hacer daño 0,15 s después.</summary>
        public const float DuracionDelMordisco = 0.15f;

        /// <summary>
        /// - Merodeando (Estado 0) manda su FSM: anda si se mueve y muerde si Kael se le pega. Tiene
        ///   hiperarmadura: los golpes no lo interrumpen.
        /// - Lo demás (agazaparse, embestir, aturdido, frenar) manda desde cualquier estado; al volver
        ///   a 0 se queda quieto, escuchando.
        /// - La muerte va primero e interrumpe todo; no tiene salida (se queda en X4).
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
                new Dictionary<string, string> { [Merodea] = AnimatorParams.VelocidadCorrer.Nombre });

            var estado = AnimatorParams.EstadoMecanica.Nombre;
            AnimatorCondition Es(ESTADOS_ACECHADOR valor) => ControladoresAnimator.Condicion(AnimatorConditionMode.Equals, estado, (int)valor);
            AnimatorCondition Si(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.If, p.Nombre);
            AnimatorCondition No(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.IfNot, p.Nombre);
            var merodea = Es(ESTADOS_ACECHADOR.MERODEA);

            ControladoresAnimator.Transiciones(estados[Quieto],
                new Transicion("Ya a tiro", estados[Mordisco], null, Si(AnimatorParams.Attack), merodea),
                new Transicion("Merodea", estados[Merodea], null, Si(AnimatorParams.MovEn), merodea));
            ControladoresAnimator.Transiciones(estados[Merodea],
                new Transicion("Ya a tiro", estados[Mordisco], null, Si(AnimatorParams.Attack), merodea),
                new Transicion("Se para", estados[Quieto], null, No(AnimatorParams.MovEn), No(AnimatorParams.Attack)));
            ControladoresAnimator.Transiciones(estados[Mordisco],
                new Transicion("Termina el mordisco", estados[Quieto], 1f, merodea),
                new Transicion("Mordisco interrumpido", estados[Quieto], null, No(AnimatorParams.Attack), merodea));
            foreach (var (_, animacion) in Caza)
            {
                ControladoresAnimator.Transiciones(estados[animacion],
                    new Transicion("Vuelve a merodear", estados[Quieto], null, merodea));
            }

            ControladoresAnimator.Transiciones(estados[Muerte]);

            var desdeCualquiera = new List<Transicion> { new Transicion("Muere", estados[Muerte], null, Si(AnimatorParams.Death)) };
            desdeCualquiera.AddRange(Caza.Select(c => new Transicion(c.animacion, estados[c.animacion], null, Es(c.estado), No(AnimatorParams.Death))));
            ControladoresAnimator.DesdeCualquierEstado(maquina, desdeCualquiera.ToArray());

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        /// <summary>
        /// Lo que el arte cambia en sus datos: tinte blanco y mira a la derecha (su sprite va
        /// volteado). Los tiempos del mordisco son los del clip, que ya eran los suyos.
        /// </summary>
        public static bool AjustarDatos()
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            if (datos == null)
            {
                Debug.LogError($"[Fragmentum] Falta {GeneradorEnemigos.CarpetaDatos}/{NombreDatos}.asset.");
                return false;
            }

            var mordisco = ClipDe(Mordisco);
            var fin = mordisco.Inicio(1) + DuracionDelMordisco;
            var cambia = datos.tinte != Color.white || !datos.spriteMiraDerecha ||
                         !Mathf.Approximately(datos.duracionAtaque, mordisco.Duracion) ||
                         !Mathf.Approximately(datos.inicioVentanaGolpe, mordisco.Inicio(1)) ||
                         !Mathf.Approximately(datos.finVentanaGolpe, fin);
            if (cambia)
            {
                datos.tinte = Color.white;
                datos.spriteMiraDerecha = true;
                datos.duracionAtaque = mordisco.Duracion;
                datos.inicioVentanaGolpe = mordisco.Inicio(1);
                datos.finVentanaGolpe = fin;
                EditorUtility.SetDirty(datos);
            }

            return true;
        }

        /// <summary>
        /// El armazón de los enemigos con arte y lo suyo, como su prefab sin arte
        /// (<c>GeneradorEnemigos.PrepararAcechador</c>): el cuerpo largo y bajo (cápsula tumbada), el
        /// mordisco delante, sin daño por contacto, y la zona de la embestida, apagada hasta que embiste.
        /// </summary>
        private static GameObject CrearPrefab(Sprite reposo, AnimatorController controlador)
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            var particulas = AssetDatabase.LoadAssetAtPath<Material>(ConstructorEcoIra.RutaMaterialParticulas);
            var tamano = GeneradorEnemigos.TamanoAcechador;
            var mitad = tamano.y * 0.5f;
            var medidas = new EnemigosConArte.Medidas
            {
                Cuerpo = tamano,
                Contacto = new Vector2(tamano.x - 0.1f, tamano.y - 0.1f),
                CentroArma = new Vector2(1.3f, mitad),
                TamanoArma = new Vector2(1.2f, 1f),
            };

            return EnemigosConArte.CrearPrefab(NombreDatos, RutaPrefab, datos, reposo, controlador, medidas, armazon =>
            {
                // Dibujado mirando a la izquierda: volteado, mira a la derecha como los demás.
                armazon.Sprite.flipX = true;
                armazon.Raiz.GetComponent<CapsuleCollider2D>().direction = CapsuleDirection2D.Horizontal;

                // Tocarlo no hace daño.
                armazon.Contacto.gameObject.SetActive(false);

                var estallido = particulas != null
                    ? ConstructorEcoIra.CrearEstallido(armazon.Pivote, armazon.Raiz.layer, particulas, Hocico,
                        ColorPolvo, ColorAliento, 1f / PixelesPorUnidad, 0.6f)
                    : null;
                Asignar(armazon.Animacion, "velocidadNaturalCarrera", p => p.floatValue = VelocidadNaturalAndar);
                Asignar(armazon.Animacion, "estallido", p => p.objectReferenceValue = estallido);
                Asignar(armazon.Animacion, "restosEnPantalla", p => p.floatValue = RestosEnPantalla);

                // La embestida: todo el cuerpo y un poco por delante. Solo encendida mientras embiste.
                var objeto = new GameObject("Embestida") { layer = Capa(Capas.EnemyWeapon) };
                objeto.transform.SetParent(armazon.Pivote, false);
                objeto.transform.localPosition = new Vector3(0.2f, mitad, 0f);
                var zona = objeto.AddComponent<BoxCollider2D>();
                zona.isTrigger = true;
                zona.size = new Vector2(tamano.x + 0.2f, tamano.y - 0.1f);
                var embestida = objeto.AddComponent<Hitbox>();
                Asignar(embestida, "danio", p => p.intValue = 2);
                Asignar(embestida, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
                objeto.SetActive(false);

                var acechador = armazon.Raiz.AddComponent<Acechador>();
                Asignar(acechador, "embestida", p => p.objectReferenceValue = embestida);
                Asignar(acechador, "pivote", p => p.objectReferenceValue = armazon.Pivote);
                Asignar(acechador, "sprite", p => p.objectReferenceValue = armazon.Sprite);
                Asignar(acechador, "animator", p => p.objectReferenceValue = armazon.Animator);
            });
        }
    }
}
