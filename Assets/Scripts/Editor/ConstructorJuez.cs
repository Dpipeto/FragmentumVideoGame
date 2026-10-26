using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;
using Transicion = Fragmentum.EditorTools.ControladoresAnimator.Transicion;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El Juez Silente, jefe del Mundo 4, con su arte (AI Studio, desde <c>Jefe4.jpeg</c>): una figura alta
    /// con armadura oscura y un yelmo liso sin rostro, una capa cubierta de placas de identificación, una
    /// luz ámbar en el pecho, una balanza y una espada larga.
    /// 1. importa sus 31 fotogramas: 30 en el mismo lienzo (195×196) y la maestra, I1, en el suyo
    ///    (150×185), los dos con el mismo píxel y el pivote en el mismo punto de apoyo;
    /// 2. crea un clip por gesto (<see cref="GESTOS_JUEZ"/>, el nombre de su estado), andar, la condena
    ///    (su grito) y la muerte, que acaba en el montón de la capa;
    /// 3. arma su controlador: cada gesto, con su disparador (lo piden los patrones a
    ///    <see cref="JuezSilente.Gesto"/>); andar, desde el reposo si se mueve; la condena y la muerte, las
    ///    de <see cref="AnimacionJefe"/>, como en los demás jefes;
    /// 4. viste el prefab del jefe (<see cref="Vestir"/>, lo llama <c>GeneradorJefes</c>): su dibujo, el
    ///    fragmento que deja al caer y las cadenas de su trampa.
    ///
    /// <b>El fragmento va aparte.</b> M5 se pidió dos veces y las dos salió con el Juez de pie encima del
    /// montón (decidido el 2026-10-22): se limpió quedándose solo con el montón, y la gema que flota
    /// encima es el Fragmento de la Culpa suelto (<c>Fragmento1</c>), que se apaga al recogerlo.
    ///
    /// <b>Está dibujado mirando a la izquierda:</b> su <see cref="SpriteRenderer"/> va volteado. Los PNG
    /// vienen de <c>Tesis/Imagenes/limpios/Mundo4/Juez</c>. Idempotente.
    /// </summary>
    public static class ConstructorJuez
    {
        public const string CarpetaSprites = TablaArte.CarpetaJefes + "/Juez";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Jefes/Juez";
        public const string NombreDatos = "JuezSilente";
        public const string RutaControlador = CarpetaAnimaciones + "/JuezSilente.controller";
        public const string RutaFragmento = CarpetaSprites + "/Fragmento1.png";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        // --- Escala y pivotes ---

        public static readonly Vector2Int Lienzo = new Vector2Int(195, 196);
        public static readonly Vector2Int LienzoMaestra = new Vector2Int(150, 185);

        /// <summary>
        /// Mide 184 píxeles de las suelas a lo alto del yelmo: a 54 por unidad, 3,4 u, como la Centinela
        /// y el Pavor.
        /// </summary>
        public const float PixelesPorUnidad = 54f;

        /// <summary>El punto de apoyo de la maestra (entre los pies, bajo la suela más baja), en cada lienzo (<c>anclas.json</c>).</summary>
        public static readonly Vector2 Ancla = new Vector2(104f, 193.8f);
        public static readonly Vector2 AnclaMaestra = new Vector2(88f, 183.9f);

        public static Vector2 Pivote => PivoteDe(Ancla, Lienzo);

        private static Vector2 PivoteDe(Vector2 ancla, Vector2Int lienzo) => new Vector2(ancla.x / lienzo.x, (lienzo.y - ancla.y) / lienzo.y);

        /// <summary>El Fragmento de la Culpa suelto: 17×25, a la escala de los de la Tristeza y el Miedo (0,6 u).</summary>
        public static readonly Vector2Int LienzoFragmento = new Vector2Int(17, 25);

        public const float PixelesPorUnidadFragmento = ConstructorDoncella.PixelesPorUnidadFragmento;

        /// <summary>Lo alto del montón de M5 (la fila 141, en x 92-101): la gema flota encima.</summary>
        public static readonly Vector2 CimaDelMonton = new Vector2(96.5f, 141f);

        /// <summary>Lo que flota la gema sobre el montón, de su borde de abajo a la cima.</summary>
        public const float SobreElMonton = 0.25f;

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(CarpetaSprites + "/", PixelesPorUnidad, Pivote,
                "Juez Silente: 30 fotogramas de 195×196 con el pivote en su punto de apoyo; 3,4 u de alto.", Lienzo),
            new TablaArte.Regla(RutaSprite("I1"), PixelesPorUnidad, PivoteDe(AnclaMaestra, LienzoMaestra),
                "Juez Silente, I1 (la maestra): en su propio lienzo (150×185) y con el mismo píxel; el pivote, en el mismo punto de apoyo.",
                LienzoMaestra),
            new TablaArte.Regla(RutaFragmento, PixelesPorUnidadFragmento, null,
                "Fragmento de la Culpa, suelto: 17×25, el pivote en el centro. Flota sobre el montón de M5.", LienzoFragmento),
        };

        // --- Animaciones ---

        public const string Andar = "Andar";
        public const string Rugido = "Rugido";
        public const string Muerte = "Muerte";

        /// <summary>El estado de cada gesto se llama como el gesto: así lo pide <see cref="JuezSilente"/>.</summary>
        public static string EstadoDe(GESTOS_JUEZ gesto) => gesto.ToString();

        /// <summary>
        /// - Reposa respirando (I1-I4) y anda despacio (W1-W4).
        /// - Los avisos se quedan en su fotograma lo que dure el aviso (O1, C1, P1, S1).
        /// - La orden: señala (O2) y baja la espada (O3). El pueblo: la balanza suena (C2, C3) mientras pasa la
        ///   ola. El compañero: la capa abierta (P2) mientras vuelan las placas, y la cierra (P3). La
        ///   sentencia: la baja (S2) y la clava (S3); después la saca (S4).
        /// - Arrodillado mientras dura su ventana (K1, K2), la luz del pecho titilando.
        /// - La condena (su grito) dura lo que espera al empezar la fase 2 (1,5 s): F1-F3.
        /// - La muerte acaba en M5, el montón de la capa, y se queda.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Juez_Reposo", Estado = EstadoDe(GESTOS_JUEZ.REPOSO), Fotogramas = new[] { "I1", "I2", "I3", "I4" }, Duraciones = new[] { 0.35f, 0.35f, 0.35f, 0.35f }, Bucle = true },
            new Clip { Nombre = "Juez_Andar", Estado = Andar, Fotogramas = new[] { "W1", "W2", "W3", "W4" }, Duraciones = new[] { 0.25f, 0.25f, 0.25f, 0.25f }, Bucle = true },
            new Clip { Nombre = "Juez_AvisoOrden", Estado = EstadoDe(GESTOS_JUEZ.AVISO_ORDEN), Fotogramas = new[] { "O1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Juez_Orden", Estado = EstadoDe(GESTOS_JUEZ.ORDEN), Fotogramas = new[] { "O2", "O3" }, Duraciones = new[] { 1.2f, ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Juez_AvisoPueblo", Estado = EstadoDe(GESTOS_JUEZ.AVISO_PUEBLO), Fotogramas = new[] { "C1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Juez_Pueblo", Estado = EstadoDe(GESTOS_JUEZ.PUEBLO), Fotogramas = new[] { "C2", "C3" }, Duraciones = new[] { 0.25f, 0.25f }, Bucle = true },
            new Clip { Nombre = "Juez_AvisoCompanero", Estado = EstadoDe(GESTOS_JUEZ.AVISO_COMPANERO), Fotogramas = new[] { "P1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Juez_Companero", Estado = EstadoDe(GESTOS_JUEZ.COMPANERO), Fotogramas = new[] { "P2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Juez_CierraCapa", Estado = EstadoDe(GESTOS_JUEZ.CIERRA_CAPA), Fotogramas = new[] { "P3" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Juez_AvisoSentencia", Estado = EstadoDe(GESTOS_JUEZ.AVISO_SENTENCIA), Fotogramas = new[] { "S1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Juez_Sentencia", Estado = EstadoDe(GESTOS_JUEZ.SENTENCIA), Fotogramas = new[] { "S2", "S3" }, Duraciones = new[] { 0.1f, ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Juez_SacaEspada", Estado = EstadoDe(GESTOS_JUEZ.SACA_ESPADA), Fotogramas = new[] { "S4" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Juez_Arrodillado", Estado = EstadoDe(GESTOS_JUEZ.ARRODILLADO), Fotogramas = new[] { "K1", "K2" }, Duraciones = new[] { 0.5f, 0.5f }, Bucle = true },
            new Clip { Nombre = "Juez_Condena", Estado = Rugido, Fotogramas = new[] { "F1", "F2", "F3" }, Duraciones = new[] { 0.4f, 0.6f, 0.5f } },
            new Clip { Nombre = "Juez_Muerte", Estado = Muerte, Fotogramas = new[] { "M1", "M2", "M3", "M4", "M5" }, Duraciones = new[] { 0.3f, 0.4f, 0.4f, 0.5f, ClipsDeSprites.SeQueda } },
        };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        /// <summary>La derrota dura lo que tarda la muerte en llegar a M5.</summary>
        public static float DuracionDerrota => ClipDe(Muerte).Inicio(ClipDe(Muerte).Fotogramas.Length - 1);

        // --- Construcción ---

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "el Juez Silente", out var sprites))
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

            // El fragmento suelto, con sus ajustes.
            if (!ClipsDeSprites.CargarSprites(new[] { new Clip { Fotogramas = new[] { "Fragmento1" }, Duraciones = new[] { 1f } } },
                    RutaSprite, "el Fragmento de la Culpa", out _))
            {
                return false;
            }

            Debug.Log($"[Fragmentum] Juez Silente: {sprites.Count} fotogramas, {Clips.Length} clips y {RutaControlador}.");
            return true;
        }

        /// <summary>La muerte avisa al llegar a M5: ahí aparece el fragmento.</summary>
        public static IEnumerable<AnimationEvent> EventosDe(Clip clip) => clip.Estado == Muerte
            ? new[] { new AnimationEvent { functionName = nameof(AnimacionJefe.FinMuerte), time = clip.Inicio(clip.Fotogramas.Length - 1) } }
            : Enumerable.Empty<AnimationEvent>();

        /// <summary>
        /// Cada gesto sale de cualquier estado con su disparador, que se llama como él, y se queda hasta el
        /// siguiente. Del reposo anda si se mueve (Walk) y al pararse vuelve. La condena (Rugido), igual que
        /// los gestos, y vuelve al reposo; la muerte, con su parámetro, va primero, interrumpe todo y no tiene salida.
        /// </summary>
        private static AnimatorController ArmarControlador(IReadOnlyDictionary<string, Motion> clips)
        {
            var controlador = ControladoresAnimator.CargarOCrear(RutaControlador);
            var gestos = (GESTOS_JUEZ[])System.Enum.GetValues(typeof(GESTOS_JUEZ));
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
            var reposo = estados[EstadoDe(GESTOS_JUEZ.REPOSO)];

            AnimatorCondition Si(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.If, p.Nombre);
            AnimatorCondition No(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.IfNot, p.Nombre);
            foreach (var estado in estados.Values.Where(e => e != reposo && e.name != Andar && e.name != Rugido))
            {
                ControladoresAnimator.Transiciones(estado);
            }

            ControladoresAnimator.Transiciones(reposo, new Transicion("Anda", estados[Andar], null, Si(AnimatorParams.Walk)));
            ControladoresAnimator.Transiciones(estados[Andar], new Transicion("Se para", reposo, null, No(AnimatorParams.Walk)));
            ControladoresAnimator.Transiciones(estados[Rugido], new Transicion("Acaba la condena", reposo, 1f));

            var desdeCualquiera = new List<Transicion>
            {
                new Transicion("Muere", estados[Muerte], null, Si(AnimatorParams.Death)),
                new Transicion("Condena", estados[Rugido], null, Si(AnimatorParams.Rugido), No(AnimatorParams.Death)),
            };
            desdeCualquiera.AddRange(gestos.Select(g => new Transicion(EstadoDe(g), estados[EstadoDe(g)], null,
                ControladoresAnimator.Condicion(AnimatorConditionMode.If, EstadoDe(g)), No(AnimatorParams.Death))));
            ControladoresAnimator.DesdeCualquierEstado(maquina, desdeCualquiera.ToArray());

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        /// <summary>
        /// Lo que el arte cambia en sus datos: sin tinte (los colores son del dibujo) y la derrota dura lo
        /// que la muerte hasta M5. Lo llama <c>GeneradorJefes</c> si hay arte. Devuelve si cambió algo.
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
        /// Viste el prefab del jefe con su arte: el primer fotograma de su reposo, volteado, con su Animator,
        /// su <see cref="AnimacionJefe"/>, el fragmento que deja al caer y la cadena de su trampa. Devuelve
        /// false si falta el arte (y el jefe se queda con su cápsula).
        /// </summary>
        public static bool Vestir(GameObject objetoSprite, Transform pivote, BossController jefe)
        {
            var controlador = Controlador;
            var reposo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite(ClipDe(EstadoDe(GESTOS_JUEZ.REPOSO)).Fotogramas[0]));
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
                Debug.LogWarning($"[Fragmentum] Falta {ConstructorEcoIra.RutaMaterialDestello}: el Juez no destellará al recibir golpes.");
            }

            Asignar(arte, "materialDestello", p => p.objectReferenceValue = destello);
            Asignar(jefe, "arte", p => p.objectReferenceValue = arte);
            Asignar(jefe, "cadena", p => p.objectReferenceValue = ConstructorEfectosMundo4.Cadena);
            Asignar(jefe, "fragmento", p => p.objectReferenceValue = CrearFragmento(pivote, sprite));
            return true;
        }

        /// <summary>
        /// Dónde flota la gema, desde el pivote y en el dibujo volteado (u): sobre la cima del montón de M5,
        /// <see cref="SobreElMonton"/> por encima.
        /// </summary>
        public static Vector2 CentroDeLaGema(Sprite gema)
        {
            var alto = gema != null ? gema.bounds.size.y : 0.6f;
            var cima = (Ancla.y - CimaDelMonton.y) / PixelesPorUnidad;
            return new Vector2((Ancla.x - CimaDelMonton.x) / PixelesPorUnidad, cima + SobreElMonton + alto * 0.5f);
        }

        // El trigger va del suelo a la gema; la gema, apagada con él hasta que la muerte llega a M5.
        private static FragmentoJefe CrearFragmento(Transform pivote, SpriteRenderer dibujo)
        {
            var gema = AssetDatabase.LoadAssetAtPath<Sprite>(RutaFragmento);
            if (gema == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {RutaFragmento}: el Juez no dejará fragmento que recoger.");
                return null;
            }

            var centro = CentroDeLaGema(gema);
            var objeto = new GameObject("Fragmento"); // capa Default: la toca el jugador
            objeto.transform.SetParent(pivote, false);
            objeto.transform.localPosition = new Vector3(centro.x, 0f, 0f);

            var cima = centro.y + gema.bounds.size.y * 0.5f;
            var zona = objeto.AddComponent<BoxCollider2D>();
            zona.isTrigger = true;
            zona.size = new Vector2(ConstructorCentinela.AnchoRecogida, cima);
            zona.offset = new Vector2(0f, cima * 0.5f);

            var objetoGema = new GameObject("Gema");
            objetoGema.transform.SetParent(objeto.transform, false);
            objetoGema.transform.localPosition = new Vector3(0f, centro.y, 0f);
            var dibujoGema = objetoGema.AddComponent<SpriteRenderer>();
            dibujoGema.sprite = gema;
            dibujoGema.sortingLayerID = dibujo.sortingLayerID;
            dibujoGema.sortingOrder = dibujo.sortingOrder + 1;

            var fragmento = objeto.AddComponent<FragmentoJefe>();
            Asignar(fragmento, "dibujo", p => p.objectReferenceValue = dibujo);
            Asignar(fragmento, "gema", p => p.objectReferenceValue = dibujoGema);
            objeto.SetActive(false); // aparece al llegar la muerte a M5
            return fragmento;
        }
    }
}
