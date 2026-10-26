using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;
using Object = UnityEngine.Object;
using Transicion = Fragmentum.EditorTools.ControladoresAnimator.Transicion;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El Espectro del Pavor, jefe del Mundo 3, con su arte (AI Studio, desde <c>jefe3.jpeg</c>): una
    /// figura alta de corteza, con una capa de hojas negras, la máscara que brilla por dentro, una
    /// gema en el pecho y una espada curva.
    /// 1. importa sus 26 fotogramas: 25 en el mismo lienzo (466×396) con el pivote entre los pies, y
    ///    E1, el aviso de la embestida, que llegó en otro lienzo y otra rejilla (305×243);
    /// 2. crea un clip por gesto (<see cref="GESTOS_PAVOR"/>, el nombre de su estado), el grito y la
    ///    muerte, que acaba en el montón de hojas con el fragmento del Miedo flotando encima;
    /// 3. arma su controlador: cada gesto, con su disparador (lo pide <see cref="EspectroDelPavor"/>);
    ///    el grito y la muerte, <see cref="AnimacionJefe"/>, como en los demás jefes;
    /// 4. viste el prefab del jefe (<see cref="Vestir"/>, lo llama <c>GeneradorJefes</c>) y crea el
    ///    dibujo de la lluvia de hojas (<see cref="PrefabHojas"/>), que lleva su patrón.
    ///
    /// <b>Está dibujado mirando a la izquierda:</b> su <see cref="SpriteRenderer"/> va volteado.
    /// <b>Su dibujo no depende de la luz</b> (lo comprueba <see cref="VerificadorPavor"/>): a oscuras,
    /// su destello se ve desde toda la arena. Las hojas y la luciérnaga, igual.
    ///
    /// <b>Lo que se arregló al limpiarlo</b> (decidido el 2026-10-15): en ocho fotogramas Gemini
    /// dibujó a su lado una copia borrosa y suelta, que se borró; y el magenta que se colaba en un
    /// resplandor (H2) y en el montón de hojas (M3-M5) se quitó. Hoja2 no se usa (salió con otro
    /// tamaño de píxel): la hoja cae y se rompe.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo3/Pavor</c>. Idempotente.
    /// </summary>
    public static class ConstructorPavor
    {
        public const string CarpetaSprites = TablaArte.CarpetaJefes + "/Pavor";
        public const string CarpetaHojas = TablaArte.CarpetaEfectos + "/Mundo3/Hojas";
        public const string CarpetaLuciernaga = TablaArte.CarpetaEfectos + "/Mundo3/Luciernaga";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Jefes/Pavor";
        public const string NombreDatos = GeneradorJefes.NombrePavor;
        public const string RutaControlador = CarpetaAnimaciones + "/EspectroDelPavor.controller";
        public const string RutaControladorHojas = ConstructorEfectos.CarpetaAnimaciones + "/HojasDelPavor.controller";
        public const string RutaPrefabHojas = ConstructorEfectos.CarpetaPrefabs + "/HojasDelPavor.prefab";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        public static string RutaHoja(string archivo) => $"{CarpetaHojas}/{archivo}.png";

        public static string RutaLuciernaga(string archivo) => $"{CarpetaLuciernaga}/{archivo}.png";

        // --- Escala y pivotes ---

        public static readonly Vector2Int Lienzo = new Vector2Int(466, 396);
        public static readonly Vector2Int LienzoEmbestida = new Vector2Int(305, 243);

        /// <summary>
        /// Mide unos 345 píxeles de los pies a lo alto de la máscara: a 100 por unidad, 3,45 u, como
        /// la Centinela. Su píxel es más fino que el de ella, como el de la Doncella (92).
        /// </summary>
        public const float PixelesPorUnidad = 100f;

        /// <summary>Entre los pies (x 230-339) y en la fila donde pisa (383).</summary>
        public static readonly Vector2 Ancla = new Vector2(285f, 384f);

        /// <summary>E1, agazapado: entre sus pies (x 23-65 y 196-219), en la fila donde pisa (241). Con el mismo píxel.</summary>
        public static readonly Vector2 AnclaEmbestida = new Vector2(121f, 242f);

        public static Vector2 Pivote => PivoteDe(Ancla, Lienzo);

        private static Vector2 PivoteDe(Vector2 ancla, Vector2Int lienzo) => new Vector2(ancla.x / lienzo.x, (lienzo.y - ancla.y) / lienzo.y);

        // Las hojas que caen (64×30) y el suelo que brilla (217×112), y la luciérnaga (18×13).
        public static readonly Vector2Int LienzoHoja = new Vector2Int(64, 30);
        public static readonly Vector2Int LienzoAviso = new Vector2Int(217, 112);
        public static readonly Vector2Int LienzoLuciernaga = new Vector2Int(18, 13);

        /// <summary>La hoja mide 23 píxeles: a 46 por unidad, media unidad; rota, se esparce 1,3 u.</summary>
        public const float PixelesPorUnidadHoja = 46f;

        /// <summary>El suelo que brilla mide 217 píxeles: a 108,5 por unidad, las 2 u de una franja.</summary>
        public const float PixelesPorUnidadAviso = 108.5f;

        /// <summary>La luciérnaga, con las alas: a 50 por unidad, 0,35 u.</summary>
        public const float PixelesPorUnidadLuciernaga = 50f;

        /// <summary>El fragmento del Miedo suelto (18×25), a la escala del de la Tristeza. Hoy no se usa: el que se recoge es el de M5.</summary>
        public const float PixelesPorUnidadFragmento = ConstructorDoncella.PixelesPorUnidadFragmento;

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(CarpetaSprites + "/", PixelesPorUnidad, Pivote,
                "Espectro del Pavor: 25 fotogramas de 466×396 con el pivote entre los pies; 3,45 u de alto.", Lienzo),
            new TablaArte.Regla(RutaSprite("E1"), PixelesPorUnidad, PivoteDe(AnclaEmbestida, LienzoEmbestida),
                "Espectro del Pavor, E1 (el aviso de la embestida): en otro lienzo (305×243) y otra rejilla, con el mismo píxel. " +
                "El pivote, entre sus pies.", LienzoEmbestida),
            new TablaArte.Regla(CarpetaSprites + "/Fragmento", PixelesPorUnidadFragmento, null,
                "Fragmento del Miedo, suelto: 18×25, el pivote en el centro.", new Vector2Int(18, 25)),
            new TablaArte.Regla(CarpetaHojas + "/Hoja", PixelesPorUnidadHoja, new Vector2(0.5f, 0f),
                "Hoja del Pavor (que cae y rota): 64×30, el pivote abajo en el centro (la rota se esparce desde ahí).", LienzoHoja),
            new TablaArte.Regla(CarpetaHojas + "/Aviso", PixelesPorUnidadAviso, PivoteDe(new Vector2(108.5f, 111f), LienzoAviso),
                "El suelo que brilla bajo las hojas: 217×112, el pivote abajo en el centro. 2 u de ancho, lo que una franja.", LienzoAviso),
            new TablaArte.Regla(CarpetaLuciernaga + "/", PixelesPorUnidadLuciernaga, null,
                "Luciérnaga del Pavor: 18×13, casi blanca (el juego la tiñe), el pivote en el centro.", LienzoLuciernaga),
        };

        // --- Animaciones ---

        public const string Rugido = "Rugido";
        public const string Muerte = "Muerte";

        /// <summary>El estado de cada gesto se llama como el gesto: así lo pide <see cref="EspectroDelPavor"/>.</summary>
        public static string EstadoDe(GESTOS_PAVOR gesto) => gesto.ToString();

        /// <summary>
        /// - Reposa respirando (I1-I4) y aparece de su remolino de hojas (A1-A4) o se deshace en él (al revés).
        /// - Los avisos se quedan en su fotograma mientras destella (T1, E1, H1): lo que dure el aviso.
        /// - El tajo (T2) acaba con la espada abajo (T3); jadea en T4. Embiste en E2 y frena en E3.
        ///   Las hojas: la columna (H2) sale disparada (H3).
        /// - El grito dura lo que espera al empezar la fase 2 (0,9 s).
        /// - La muerte acaba en M5, el montón de hojas con el fragmento, y se queda.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Pavor_Reposo", Estado = EstadoDe(GESTOS_PAVOR.REPOSO), Fotogramas = new[] { "I1", "I2", "I3", "I4" }, Duraciones = new[] { 0.3f, 0.3f, 0.3f, 0.3f }, Bucle = true },
            new Clip { Nombre = "Pavor_Aparece", Estado = EstadoDe(GESTOS_PAVOR.APARECE), Fotogramas = new[] { "A1", "A2", "A3", "A4" }, Duraciones = new[] { 0.1f, 0.1f, 0.1f, ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Pavor_Desvanece", Estado = EstadoDe(GESTOS_PAVOR.DESVANECE), Fotogramas = new[] { "A4", "A3", "A2", "A1" }, Duraciones = new[] { 0.1f, 0.1f, 0.1f, ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Pavor_AvisoTajo", Estado = EstadoDe(GESTOS_PAVOR.AVISO_TAJO), Fotogramas = new[] { "T1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Pavor_Tajo", Estado = EstadoDe(GESTOS_PAVOR.TAJO), Fotogramas = new[] { "T2", "T3" }, Duraciones = new[] { 0.1f, ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Pavor_Jadea", Estado = EstadoDe(GESTOS_PAVOR.JADEA), Fotogramas = new[] { "T4" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Pavor_AvisoEmbestida", Estado = EstadoDe(GESTOS_PAVOR.AVISO_EMBESTIDA), Fotogramas = new[] { "E1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Pavor_Embiste", Estado = EstadoDe(GESTOS_PAVOR.EMBISTE), Fotogramas = new[] { "E2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Pavor_Frena", Estado = EstadoDe(GESTOS_PAVOR.FRENA), Fotogramas = new[] { "E3" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Pavor_AvisoHojas", Estado = EstadoDe(GESTOS_PAVOR.AVISO_HOJAS), Fotogramas = new[] { "H1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Pavor_Hojas", Estado = EstadoDe(GESTOS_PAVOR.HOJAS), Fotogramas = new[] { "H2", "H3" }, Duraciones = new[] { 0.3f, ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Pavor_Rugido", Estado = Rugido, Fotogramas = new[] { "R1", "R2", "R3" }, Duraciones = new[] { 0.25f, 0.4f, 0.25f } },
            new Clip { Nombre = "Pavor_Muerte", Estado = Muerte, Fotogramas = new[] { "M1", "M2", "M3", "M4", "M5" }, Duraciones = new[] { 0.25f, 0.35f, 0.4f, 0.4f, ClipsDeSprites.SeQueda } },
        };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        /// <summary>La derrota dura lo que tarda la muerte en llegar a M5.</summary>
        public static float DuracionDerrota => ClipDe(Muerte).Inicio(ClipDe(Muerte).Fotogramas.Length - 1);

        // --- La lluvia de hojas ---

        public const string HojasAviso = "Aviso";
        public const string HojasCaen = "Caen";
        public const string HojasApaga = "Apaga";

        /// <summary>
        /// Mientras su zona avisa, el suelo brilla entre las hojas muertas (Aviso1 y Aviso2, en
        /// bucle); al hacer daño, brilla del todo (Aviso2) y caen las hojas (<see cref="LluviaDeHojas"/>);
        /// al acabar, se apaga un momento (Aviso1) mientras las últimas se rompen contra el suelo.
        /// </summary>
        public static readonly Clip[] ClipsHojas =
        {
            new Clip { Nombre = "HojasDelPavor_Aviso", Estado = HojasAviso, Fotogramas = new[] { "Aviso1", "Aviso2" }, Duraciones = new[] { 0.12f, 0.12f }, Bucle = true },
            new Clip { Nombre = "HojasDelPavor_Caen", Estado = HojasCaen, Fotogramas = new[] { "Aviso2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "HojasDelPavor_Apaga", Estado = HojasApaga, Fotogramas = new[] { "Aviso1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
        };

        public static readonly string[] FotogramasHoja = { "Hoja1", "Hoja3" };

        /// <summary>Lo que sigue viéndose la franja al acabar su zona: lo que tardan en romperse las últimas hojas.</summary>
        public const float DuracionApagado = 0.5f;

        /// <summary>Las hojas de cada franja.</summary>
        public const int HojasPorFranja = 5;

        public static readonly string[] FotogramasLuciernaga = { "Luciernaga1", "Luciernaga2" };

        // --- Construcción ---

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El dibujo de una franja de hojas, o null si no hay arte.</summary>
        public static GameObject PrefabHojas => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabHojas);

        /// <summary>Los fotogramas de la luciérnaga, o null si falta alguno.</summary>
        public static Sprite[] Luciernaga
        {
            get
            {
                var sprites = FotogramasLuciernaga.Select(f => AssetDatabase.LoadAssetAtPath<Sprite>(RutaLuciernaga(f))).ToArray();
                return sprites.All(s => s != null) ? sprites : null;
            }
        }

        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "el Espectro del Pavor", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            AsegurarCarpeta(CarpetaAnimaciones);
            var clips = Clips.ToDictionary(c => c.Estado, c => (Motion)ClipsDeSprites.Crear(CarpetaAnimaciones, c, sprites, EventosDe(c)));
            ArmarControlador(clips);

            // La luciérnaga, con sus ajustes.
            if (!ClipsDeSprites.CargarSprites(new[] { new Clip { Fotogramas = FotogramasLuciernaga, Duraciones = new[] { 1f, 1f } } },
                    RutaLuciernaga, "la luciérnaga del Pavor", out _))
            {
                return false;
            }

            var ok = CrearHojas();
            Debug.Log($"[Fragmentum] Espectro del Pavor: {sprites.Count} fotogramas, {Clips.Length} clips, {RutaControlador} y {RutaPrefabHojas}.");
            return ok;
        }

        /// <summary>La muerte avisa al llegar a M5: ahí aparece el fragmento.</summary>
        public static IEnumerable<AnimationEvent> EventosDe(Clip clip) => clip.Estado == Muerte
            ? new[] { new AnimationEvent { functionName = nameof(AnimacionJefe.FinMuerte), time = clip.Inicio(clip.Fotogramas.Length - 1) } }
            : Enumerable.Empty<AnimationEvent>();

        /// <summary>
        /// Cada gesto sale de cualquier estado con su disparador, que se llama como él (lo pide
        /// <see cref="EspectroDelPavor"/>), y se queda hasta el siguiente. El grito, igual, y vuelve al
        /// reposo; la muerte, con su parámetro, va primero, interrumpe todo y no tiene salida.
        /// </summary>
        private static AnimatorController ArmarControlador(IReadOnlyDictionary<string, Motion> clips)
        {
            var controlador = ControladoresAnimator.CargarOCrear(RutaControlador);
            // Los de AnimacionJefe, aunque no los use (andar y clavar la espada): así no avisa de que faltan.
            var gestos = (GESTOS_PAVOR[])System.Enum.GetValues(typeof(GESTOS_PAVOR));
            ControladoresAnimator.AsegurarParametros(controlador, new[]
                {
                    (AnimatorParams.Walk.Nombre, AnimatorControllerParameterType.Bool, 0f),
                    (AnimatorParams.Clavar.Nombre, AnimatorControllerParameterType.Bool, 0f),
                    (AnimatorParams.Death.Nombre, AnimatorControllerParameterType.Bool, 0f),
                    (AnimatorParams.Rugido.Nombre, AnimatorControllerParameterType.Trigger, 0f),
                }
                .Concat(gestos.Select(g => (EstadoDe(g), AnimatorControllerParameterType.Trigger, 0f))).ToArray());

            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, Clips.Select(c => (c.Estado, clips[c.Estado])).ToList());
            foreach (var estado in estados.Values.Where(e => e.name != Rugido))
            {
                ControladoresAnimator.Transiciones(estado);
            }

            ControladoresAnimator.Transiciones(estados[Rugido],
                new Transicion("Acaba el grito", estados[EstadoDe(GESTOS_PAVOR.REPOSO)], 1f));

            AnimatorCondition Si(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.If, p.Nombre);
            AnimatorCondition No(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.IfNot, p.Nombre);
            var desdeCualquiera = new List<Transicion>
            {
                new Transicion("Muere", estados[Muerte], null, Si(AnimatorParams.Death)),
                new Transicion("Grita", estados[Rugido], null, Si(AnimatorParams.Rugido), No(AnimatorParams.Death)),
            };
            desdeCualquiera.AddRange(gestos.Select(g => new Transicion(EstadoDe(g), estados[EstadoDe(g)], null,
                ControladoresAnimator.Condicion(AnimatorConditionMode.If, EstadoDe(g)), No(AnimatorParams.Death))));
            ControladoresAnimator.DesdeCualquierEstado(maquina, desdeCualquiera.ToArray());

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        /// <summary>
        /// Lo que el arte cambia en sus datos: sin tinte (los colores son del dibujo) y la derrota
        /// dura lo que la muerte hasta M5. Lo llama <c>GeneradorJefes</c> si hay arte.
        /// </summary>
        public static bool AjustarDatos(JefeData datos)
        {
            if (datos == null || Controlador == null)
            {
                return false;
            }

            var cambia = datos.tinte != Color.white || !Mathf.Approximately(datos.duracionDerrota, DuracionDerrota);
            if (cambia)
            {
                datos.tinte = Color.white;
                datos.duracionDerrota = DuracionDerrota;
                EditorUtility.SetDirty(datos);
            }

            return cambia;
        }

        // --- El prefab del jefe ---

        /// <summary>
        /// Viste el prefab del jefe con su arte: el primer fotograma de su reposo, volteado, con su
        /// material sin luz (lo pone <c>GeneradorJefes</c>), su Animator, su <see cref="AnimacionJefe"/>,
        /// la luciérnaga dibujada y el fragmento que deja al caer. Devuelve false si falta el arte (y
        /// el jefe se queda con su cápsula).
        /// </summary>
        public static bool Vestir(GameObject objetoSprite, Transform pivote, BossController jefe)
        {
            var controlador = Controlador;
            var reposo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite(ClipDe(EstadoDe(GESTOS_PAVOR.REPOSO)).Fotogramas[0]));
            if (controlador == null || reposo == null)
            {
                return false;
            }

            objetoSprite.transform.localPosition = Vector3.zero;
            objetoSprite.transform.localScale = Vector3.one;

            var sprite = objetoSprite.GetComponent<SpriteRenderer>();
            sprite.sprite = reposo;
            sprite.color = Color.white;
            // Dibujado mirando a la izquierda: volteado, mira a la derecha como los demás jefes.
            sprite.flipX = true;

            var animator = objetoSprite.GetComponent<Animator>();
            animator.runtimeAnimatorController = controlador;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var arte = objetoSprite.AddComponent<AnimacionJefe>();
            var destello = AssetDatabase.LoadAssetAtPath<Material>(ConstructorEcoIra.RutaMaterialDestello);
            if (destello == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {ConstructorEcoIra.RutaMaterialDestello}: el Pavor no destellará al recibir golpes.");
            }

            Asignar(arte, "materialDestello", p => p.objectReferenceValue = destello);
            Asignar(jefe, "arte", p => p.objectReferenceValue = arte);

            var luciernaga = Luciernaga;
            Asignar(jefe, "luciernaga", p =>
            {
                p.arraySize = luciernaga?.Length ?? 0;
                for (var i = 0; i < p.arraySize; i++)
                {
                    p.GetArrayElementAtIndex(i).objectReferenceValue = luciernaga[i];
                }
            });

            Asignar(jefe, "fragmento", p => p.objectReferenceValue = CrearFragmento(pivote, sprite));
            return true;
        }

        /// <summary>
        /// El fragmento que flota sobre el montón de hojas en M5, medido en el dibujo (como el de la
        /// Doncella). El dibujo va volteado, así que su x cambia de signo.
        /// </summary>
        public static (Vector2 centro, Vector2 tamano)? MedirFragmento()
        {
            var muerte = ClipDe(Muerte);
            var medida = ConstructorCentinela.MedirFragmento(RutaSprite(muerte.Fotogramas.Last()), Pivote, PixelesPorUnidad);
            return medida is { } m ? (new Vector2(-m.centro.x, m.centro.y), m.tamano) : ((Vector2, Vector2)?)null;
        }

        private static FragmentoJefe CrearFragmento(Transform pivote, SpriteRenderer dibujo)
        {
            var medida = MedirFragmento();
            if (medida == null)
            {
                Debug.LogWarning("[Fragmentum] No se encontró el fragmento en M5: el Pavor no dejará fragmento que recoger.");
                return null;
            }

            var fragmento = ConstructorCentinela.CrearFragmento(pivote, dibujo, medida.Value, PixelesPorUnidad);
            AnadirBrillo(fragmento, dibujo, medida.Value);
            return fragmento;
        }

        /// <summary>
        /// La luz del fragmento: casi blanca, con un toque lavanda. Una violeta solo aclaraba el azul de
        /// la gema; esta la lleva a casi blanco en lo alto del latido.
        /// </summary>
        public static readonly Color ColorBrilloFragmento = new Color32(225, 215, 255, 255);

        /// <summary>Hasta dónde alumbra la luz del fragmento: flota a unos 2 u, y llega al suelo de alrededor del montón.</summary>
        public const float RadioBrilloFragmento = 3.2f;

        /// <summary>¿Alumbra <paramref name="luz"/> la capa de orden <paramref name="capa"/>?</summary>
        public static bool AlumbraLaCapa(Light2D luz, int capa)
        {
            var capas = new SerializedObject(luz).FindProperty("m_ApplyToSortingLayers");
            for (var i = 0; capas != null && i < capas.arraySize; i++)
            {
                if (capas.GetArrayElementAtIndex(i).intValue == capa)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// El fragmento del Miedo es azul oscuro y sobre el montón de hojas apenas se veía (el autor:
        /// «no se visualiza mucho, haz que brille más», 2026-10-17). Encima va una copia de sus mismos
        /// píxeles, recortada de M5, con el material que recibe luz, y una luz violeta que late la
        /// enciende (<see cref="BrilloDeFragmento"/>). No es arte nuevo.
        /// </summary>
        private static void AnadirBrillo(FragmentoJefe fragmento, SpriteRenderer dibujo, (Vector2 centro, Vector2 tamano) medida)
        {
            var ruta = RutaSprite(ClipDe(Muerte).Fotogramas.Last());
            var fotograma = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            var caja = ConstructorCentinela.CajaDelFragmento(ruta);
            if (fragmento == null || fotograma == null || caja == null)
            {
                Debug.LogWarning("[Fragmentum] El fragmento del Pavor se queda sin su brillo: falta M5 o su medida.");
                return;
            }

            // Donde la máscara: en el centro del fragmento dibujado.
            var objeto = new GameObject("Brillo");
            objeto.transform.SetParent(fragmento.transform, false);
            objeto.transform.localPosition = new Vector3(0f, medida.centro.y, 0f);
            var copia = objeto.AddComponent<SpriteRenderer>();
            copia.sortingLayerID = dibujo.sortingLayerID;
            copia.sortingOrder = dibujo.sortingOrder + 1;

            // Su silueta, con el material del destello de los golpes: la luz 2D no aclara un dibujo
            // por encima de sus colores, y esto sí. Su opacidad late con la luz.
            var objetoDestello = new GameObject("Destello");
            objetoDestello.transform.SetParent(objeto.transform, false);
            var destello = objetoDestello.AddComponent<SpriteRenderer>();
            destello.sortingLayerID = dibujo.sortingLayerID;
            destello.sortingOrder = dibujo.sortingOrder + 2;
            destello.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ConstructorEcoIra.RutaMaterialDestello);
            if (destello.sharedMaterial == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {ConstructorEcoIra.RutaMaterialDestello}: la gema del Pavor no destellará.");
            }

            var luz = objeto.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.pointLightOuterRadius = RadioBrilloFragmento;
            luz.pointLightInnerRadius = 1.2f;
            luz.falloffIntensity = 0.3f;
            luz.color = ColorBrilloFragmento;
            luz.intensity = 2.5f;
            luz.shadowsEnabled = false;

            // El fragmento está apagado hasta que aparece, y una luz añadida a un objeto apagado no se
            // prepara sola: no alumbraba ninguna capa. Se le dicen todas, como las que tienen la
            // antorcha y la luz de Kael.
            Asignar(luz, "m_ApplyToSortingLayers", p =>
            {
                var capas = SortingLayer.layers;
                p.arraySize = capas.Length;
                for (var i = 0; i < capas.Length; i++)
                {
                    p.GetArrayElementAtIndex(i).intValue = capas[i].id;
                }
            });

            var brillo = fragmento.gameObject.AddComponent<BrilloDeFragmento>();
            Asignar(brillo, "fotograma", p => p.objectReferenceValue = fotograma);
            Asignar(brillo, "caja", p => p.rectIntValue = caja.Value.caja);
            Asignar(brillo, "dibujoDelJefe", p => p.objectReferenceValue = dibujo);
            Asignar(brillo, "copia", p => p.objectReferenceValue = copia);
            Asignar(brillo, "destello", p => p.objectReferenceValue = destello);
            Asignar(brillo, "luz", p => p.objectReferenceValue = luz);
        }

        // --- La lluvia de hojas ---

        private static bool CrearHojas()
        {
            var todos = ClipsHojas.Append(new Clip { Fotogramas = FotogramasHoja, Duraciones = new[] { 1f, 1f } });
            if (!ClipsDeSprites.CargarSprites(todos, RutaHoja, "la lluvia de hojas del Pavor", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            AsegurarCarpeta(ConstructorEfectos.CarpetaAnimaciones);
            AsegurarCarpeta(ConstructorEfectos.CarpetaPrefabs);
            var clips = ClipsHojas.ToDictionary(c => c.Estado, c => (Motion)ClipsDeSprites.Crear(ConstructorEfectos.CarpetaAnimaciones, c, sprites));

            var controlador = ControladoresAnimator.CargarOCrear(RutaControladorHojas);
            ControladoresAnimator.AsegurarParametros(controlador,
                (AnimatorParams.Quema.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Apagada.Nombre, AnimatorControllerParameterType.Bool, 0f));
            var maquina = controlador.layers[0].stateMachine;
            var e = ControladoresAnimator.Estados(maquina, ClipsHojas.Select(c => (c.Estado, clips[c.Estado])).ToList());
            var apagada = ControladoresAnimator.Condicion(AnimatorConditionMode.If, AnimatorParams.Apagada.Nombre);
            var quema = ControladoresAnimator.Condicion(AnimatorConditionMode.If, AnimatorParams.Quema.Nombre);
            ControladoresAnimator.Transiciones(e[HojasAviso],
                new Transicion("Apagada", e[HojasApaga], null, apagada),
                new Transicion("Caen", e[HojasCaen], null, quema));
            ControladoresAnimator.Transiciones(e[HojasCaen], new Transicion("Apagada", e[HojasApaga], null, apagada));
            ControladoresAnimator.Transiciones(e[HojasApaga]);
            EditorUtility.SetDirty(controlador);
            AssetDatabase.SaveAssets();

            var raiz = new GameObject("HojasDelPavor");
            try
            {
                var suelo = raiz.AddComponent<SpriteRenderer>();
                suelo.sprite = sprites[ClipsHojas[0].Fotogramas[0]];
                suelo.sortingOrder = ConstructorEfectos.OrdenDibujo;
                // Como su dueño, no depende de la luz: en la arena a oscuras se ve dónde van a caer.
                suelo.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(GeneradorJefes.RutaMaterialSinLuz);
                var animator = raiz.AddComponent<Animator>();
                animator.runtimeAnimatorController = controlador;
                animator.applyRootMotion = false;
                raiz.AddComponent<DibujoDeZona>().duracionApagado = DuracionApagado;

                var lluvia = raiz.AddComponent<LluviaDeHojas>();
                lluvia.cuantas = HojasPorFranja;
                Asignar(lluvia, "hoja", p => p.objectReferenceValue = sprites[FotogramasHoja[0]]);
                Asignar(lluvia, "rota", p => p.objectReferenceValue = sprites[FotogramasHoja[1]]);

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabHojas, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {RutaPrefabHojas}.");
                }

                return guardado;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }
    }
}
