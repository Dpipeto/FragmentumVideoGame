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
    /// El Artillero, con su arte: un soldado arrodillado con casco, un fusil largo y, bajo el casco, la
    /// cara lisa de los Ecos.
    /// 1. importa sus 13 fotogramas: 11 en el mismo lienzo (115×65), con el pivote entre la rodilla y el pie
    ///    de detrás (<see cref="Ancla"/>), y los dos que llegaron al final en el suyo (<see cref="Sueltos"/>);
    /// 2. crea un clip por lo que se le ve hacer (<see cref="POSES_ARTILLERO"/>, el entero «Estado» que
    ///    escribe <see cref="Artillero"/>) y la muerte;
    /// 3. arma su controlador: cada pose, desde cualquier estado; la muerte, con su parámetro, primero;
    /// 4. monta su prefab, con su línea de tiro y su bala dibujada (<see cref="ConstructorEfectosMundo4"/>).
    ///
    /// <b>Llegaron reordenados</b> (decidido el 2026-10-20, <c>limpiar_mundo4.py</c>): la recarga es la
    /// del fusil bajado y la del peine. El aviso del culatazo (G1: el fusil vuelto, la culata en alto) y el
    /// abrigo vacío (M3) llegaron el 2026-10-23 (<c>Prompts_Mundo4.md</c>, apartado K1), editando K1 pero en
    /// otro lienzo: cada uno lleva su lienzo y su ancla, la de los demás llevada a él al encajarlos sobre K1 y
    /// sobre M2 (<c>anclas.json</c>, «ArtilleroNuevos»). Mismo píxel de arte: los mismos 42 px/u.
    ///
    /// <b>Mide 1,5 u arrodillado</b> (decidido el 2026-10-21): a 42 px/u. La boca de su fusil, al
    /// apuntar, queda 1,55 u por delante y a 1,06 de alto: es el cañón del juego
    /// (<see cref="DisenoNivel4.CanonDelante"/>, <see cref="DisenoNivel4.AlturaCanon"/>), y sus sacos miden 0,9.
    ///
    /// <b>Está dibujado mirando a la izquierda:</b> su <see cref="SpriteRenderer"/> va volteado, como el
    /// del Centinela Ciego. Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo4/Artillero</c>. Idempotente.
    /// </summary>
    public static class ConstructorArtillero
    {
        public const string CarpetaSprites = "Assets/Sprites/Enemigos/Artillero";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Enemigos/Artillero";
        public const string NombreDatos = "Artillero";
        public const string RutaControlador = CarpetaAnimaciones + "/Artillero.controller";
        public const string RutaPrefab = GeneradorEnemigos.CarpetaPrefabs + "/" + NombreDatos + ".prefab";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        // --- Escala y pivote ---

        public static readonly Vector2Int Lienzo = new Vector2Int(115, 65);

        /// <summary>Mide 63 píxeles del suelo a lo alto del casco: a 42 por unidad, 1,5 u.</summary>
        public const float PixelesPorUnidad = 42f;

        /// <summary>Entre la rodilla (x 61) y el pie de detrás (x 111), en la fila donde se apoya (63).</summary>
        public static readonly Vector2 Ancla = new Vector2(84f, 64f);

        /// <summary>La boca del fusil en A1 (apuntando): el píxel más a la izquierda, x 19, en la fila 19.</summary>
        public static readonly Vector2 BocaDelFusil = new Vector2(19f, 19.5f);

        public static Vector2 Pivote => new Vector2(Ancla.x / Lienzo.x, (Lienzo.y - Ancla.y) / Lienzo.y);

        /// <summary>El cañón medido en el dibujo, desde el pivote y mirando a la derecha (u).</summary>
        public static Vector2 CanonDelDibujo => new Vector2((Ancla.x - BocaDelFusil.x) / PixelesPorUnidad, (Ancla.y - BocaDelFusil.y) / PixelesPorUnidad);

        /// <summary>Un fotograma con su propio lienzo y su ancla (la de los demás, llevada a él).</summary>
        public sealed class Suelto
        {
            public readonly string Archivo;
            public readonly Vector2Int Lienzo;
            public readonly Vector2 Ancla;
            public readonly string Nota;

            public Suelto(string archivo, int ancho, int alto, float anclaX, float anclaY, string nota)
            {
                Archivo = archivo;
                Lienzo = new Vector2Int(ancho, alto);
                Ancla = new Vector2(anclaX, anclaY);
                Nota = nota;
            }

            public Vector2 Pivote => new Vector2(Ancla.x / Lienzo.x, (Lienzo.y - Ancla.y) / Lienzo.y);
        }

        /// <summary>El aviso del culatazo: encajado sobre K1 de cintura para abajo, desplazado 47 píxeles a la derecha.</summary>
        public static readonly Suelto AvisoCulatazo = new Suelto("G1", 69, 63, 37f, 63f,
            "Artillero, aviso del culatazo (G1): 69×63, su propio lienzo; el ancla de los demás (x 84) cae en su x 37.");

        /// <summary>El abrigo vacío: encajado sobre el soldado tendido (M2), 15 a la derecha y 7 abajo.</summary>
        public static readonly Suelto AbrigoVacio = new Suelto("M3", 80, 58, 69f, 57f,
            "Artillero, el abrigo vacío (M3): 80×58, su propio lienzo; el ancla de los demás cae en su (69, 57).");

        public static readonly Suelto[] Sueltos = { AvisoCulatazo, AbrigoVacio };

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(CarpetaSprites + "/", PixelesPorUnidad, Pivote,
                "Artillero: 11 fotogramas de 115×65 con el pivote entre la rodilla y el pie; 1,5 u arrodillado.", Lienzo),
        }.Concat(Sueltos.Select(f => new TablaArte.Regla($"{CarpetaSprites}/{f.Archivo}", PixelesPorUnidad, f.Pivote, f.Nota, f.Lienzo)));

        // --- Animaciones ---

        public const string Muerte = "Muerte";

        /// <summary>El estado de cada pose se llama como ella: así lo elige el entero «Estado».</summary>
        public static string EstadoDe(POSES_ARTILLERO pose) => pose.ToString();

        /// <summary>
        /// - En reposo respira (K1, K2). Apunta con el fusil al hombro (A1) mientras dura el aviso.
        /// - Dispara con el fogonazo (F1, F2) en bucle mientras salen las tres balas.
        /// - Recarga con el fusil bajado y la mano en el cerrojo (R1) y metiendo el peine (R2).
        /// - Avisa del culatazo volviendo el fusil, con la culata en alto (G1). El culatazo, G2. El golpe, H1.
        /// - Muere cayendo de lado (M1), tendido (M2), y de él solo queda el abrigo vacío (M3), que se desvanece.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Artillero_Reposo", Estado = EstadoDe(POSES_ARTILLERO.REPOSO), Fotogramas = new[] { "K1", "K2" }, Duraciones = new[] { 0.6f, 0.6f }, Bucle = true },
            new Clip { Nombre = "Artillero_Apunta", Estado = EstadoDe(POSES_ARTILLERO.APUNTA), Fotogramas = new[] { "A1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Artillero_Dispara", Estado = EstadoDe(POSES_ARTILLERO.DISPARA), Fotogramas = new[] { "F1", "F2" }, Duraciones = new[] { 0.06f, 0.06f }, Bucle = true },
            new Clip { Nombre = "Artillero_Recarga", Estado = EstadoDe(POSES_ARTILLERO.RECARGA), Fotogramas = new[] { "R1", "R2" }, Duraciones = new[] { 0.5f, 0.5f }, Bucle = true },
            new Clip { Nombre = "Artillero_AlzaLaCulata", Estado = EstadoDe(POSES_ARTILLERO.ALZA_LA_CULATA), Fotogramas = new[] { "G1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Artillero_Culatazo", Estado = EstadoDe(POSES_ARTILLERO.CULATAZO), Fotogramas = new[] { "G2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Artillero_Golpeado", Estado = EstadoDe(POSES_ARTILLERO.GOLPEADO), Fotogramas = new[] { "H1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Artillero_Muerte", Estado = Muerte, Fotogramas = new[] { "M1", "M2", "M3" }, Duraciones = new[] { 0.35f, 0.5f, ClipsDeSprites.SeQueda } },
        };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        /// <summary>Segundos que se queda el abrigo vacío (M3) antes de desvanecerse.</summary>
        public const float RestosEnPantalla = 0.8f;

        // El polvo gris y pardo que suelta al caer.
        public static readonly Color ColorPolvo = new Color32(122, 114, 104, 255);
        public static readonly Color ColorAbrigo = new Color32(92, 61, 30, 255);

        // --- Geometría (u, desde el pivote y mirando a la derecha) ---

        /// <summary>Su cuerpo, arrodillado.</summary>
        public static readonly Vector2 TamanoCuerpo = GeneradorEnemigos.TamanoArtillero;

        public static readonly Vector2 TamanoContacto = new Vector2(0.7f, 1.4f);

        /// <summary>El culatazo (G2): por delante, a la altura del pecho.</summary>
        public static readonly Vector2 CentroArma = new Vector2(0.9f, 0.8f);

        public static readonly Vector2 TamanoArma = new Vector2(1.2f, 1f);

        /// <summary>De dónde sale el polvo al morir: el pecho.</summary>
        public static readonly Vector2 Pecho = new Vector2(0f, 0.8f);

        // --- Construcción ---

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El prefab del Artillero con arte, o null si todavía no se construyó.</summary>
        public static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "el Artillero", out var sprites))
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

            var ok = CrearPrefab(sprites[ClipDe(EstadoDe(POSES_ARTILLERO.REPOSO)).Fotogramas[0]], controlador) != null;
            Debug.Log($"[Fragmentum] Artillero: {sprites.Count} fotogramas, {Clips.Length} clips, {RutaControlador} y {RutaPrefab}.");
            return ok;
        }

        /// <summary>La muerte avisa cuando ya solo queda el abrigo vacío: ahí empieza a desvanecerse.</summary>
        public static IEnumerable<AnimationEvent> EventosDe(Clip clip) => clip.Estado == Muerte
            ? new[] { EnemigosConArte.Evento(nameof(EnemigoAnimacion.FinMuerte), clip.Inicio(2)) }
            : Enumerable.Empty<AnimationEvent>();

        /// <summary>Cada pose, desde cualquier estado, mientras el entero «Estado» la diga; la muerte va primero y no tiene salida.</summary>
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
            foreach (var estado in estados.Values)
            {
                ControladoresAnimator.Transiciones(estado);
            }

            var poses = (POSES_ARTILLERO[])System.Enum.GetValues(typeof(POSES_ARTILLERO));
            var no = ControladoresAnimator.Condicion(AnimatorConditionMode.IfNot, AnimatorParams.Death.Nombre);
            var desdeCualquiera = new List<Transicion>
            {
                new Transicion("Muere", estados[Muerte], null, ControladoresAnimator.Condicion(AnimatorConditionMode.If, AnimatorParams.Death.Nombre)),
            };
            desdeCualquiera.AddRange(poses.Select(p => new Transicion(EstadoDe(p), estados[EstadoDe(p)], null,
                ControladoresAnimator.Condicion(AnimatorConditionMode.Equals, AnimatorParams.EstadoMecanica.Nombre, (int)p), no)));
            ControladoresAnimator.DesdeCualquierEstado(maquina, desdeCualquiera.ToArray());

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        /// <summary>Lo que el arte cambia en sus datos: tinte blanco (los colores son del dibujo) y mira a la derecha (va volteado).</summary>
        public static bool AjustarDatos()
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            if (datos == null)
            {
                Debug.LogError($"[Fragmentum] Falta {GeneradorEnemigos.CarpetaDatos}/{NombreDatos}.asset.");
                return false;
            }

            if (datos.tinte != Color.white || !datos.spriteMiraDerecha)
            {
                datos.tinte = Color.white;
                datos.spriteMiraDerecha = true;
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
                // Tocarlo no hace daño: solo su culatazo y sus balas.
                armazon.Contacto.gameObject.SetActive(false);

                var estallido = particulas != null
                    ? ConstructorEcoIra.CrearEstallido(armazon.Pivote, armazon.Raiz.layer, particulas, Pecho,
                        ColorPolvo, ColorAbrigo, 1f / PixelesPorUnidad, 0.4f)
                    : null;
                Asignar(armazon.Animacion, "estallido", p => p.objectReferenceValue = estallido);
                Asignar(armazon.Animacion, "restosEnPantalla", p => p.floatValue = RestosEnPantalla);

                var artillero = armazon.Raiz.AddComponent<Artillero>();
                GeneradorEnemigos.AjustarArtillero(artillero, GeneradorEnemigos.CrearLineaDeTiro(armazon.Raiz));
                Asignar(artillero, "pivote", p => p.objectReferenceValue = armazon.Pivote);
                Asignar(artillero, "sprite", p => p.objectReferenceValue = armazon.Sprite);
                Asignar(artillero, "animator", p => p.objectReferenceValue = armazon.Animator);
                Asignar(artillero, "bala", p => p.objectReferenceValue = ConstructorEfectosMundo4.PrefabBala);
            });
        }
    }
}
