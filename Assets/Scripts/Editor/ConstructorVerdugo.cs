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
    /// El Verdugo, el mini-jefe del mundo de la Culpa, con su arte: un Penitente enorme con capucha de
    /// verdugo, un hacha de dos manos y la cadena de un garfio.
    /// 1. importa sus 18 fotogramas, todos en el mismo lienzo (199×136), con el pivote entre los pies;
    /// 2. crea sus clips: quieto y andar (los de su controlador), un clip por lo que se le ve hacer en
    ///    sus golpes (<see cref="POSES_VERDUGO"/>, el entero «Estado» que escribe <see cref="Verdugo"/>)
    ///    y la muerte, que acaba en la capucha vacía sobre el montón de cadenas;
    /// 3. arma su controlador: con «Estado» en 0 manda su controlador (anda si se mueve); cada otra pose,
    ///    desde cualquier estado; la muerte, primero;
    /// 4. monta su prefab: sus golpes (los mismos que sin arte), y el garfio y la cadena dibujados
    ///    (<see cref="ConstructorEfectosMundo4"/>): el garfio vuela por delante y la cadena, un tramo que
    ///    se repite, baja de su mano hasta él.
    ///
    /// No tiene fotograma de daño: tiene hiperarmadura. <b>Está dibujado mirando a la izquierda:</b> su
    /// <see cref="SpriteRenderer"/> va volteado. Los PNG vienen de
    /// <c>Tesis/Imagenes/limpios/Mundo4/Verdugo</c>. Idempotente.
    /// </summary>
    public static class ConstructorVerdugo
    {
        public const string CarpetaSprites = "Assets/Sprites/Enemigos/Verdugo";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Enemigos/Verdugo";
        public const string NombreDatos = "Verdugo";
        public const string RutaControlador = CarpetaAnimaciones + "/Verdugo.controller";
        public const string RutaPrefab = GeneradorEnemigos.CarpetaPrefabs + "/" + NombreDatos + ".prefab";

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        // --- Escala y pivote ---

        public static readonly Vector2Int Lienzo = new Vector2Int(199, 136);

        /// <summary>Mide 133 píxeles de los pies a la punta de la capucha: a 44 por unidad, 3 u.</summary>
        public const float PixelesPorUnidad = 44f;

        /// <summary>Entre los pies (con los pies juntos, W2: x 96 y 181), en la fila donde pisa (134).</summary>
        public static readonly Vector2 Ancla = new Vector2(138f, 135f);

        /// <summary>Su mano al lanzar la cadena (C2): los dedos, en x 40 y la fila 47.</summary>
        public static readonly Vector2 Mano = new Vector2(40f, 47f);

        public static Vector2 Pivote => new Vector2(Ancla.x / Lienzo.x, (Lienzo.y - Ancla.y) / Lienzo.y);

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(CarpetaSprites + "/", PixelesPorUnidad, Pivote,
                "Verdugo: 18 fotogramas de 199×136 con el pivote entre los pies; 3 u de alto.", Lienzo),
        };

        // --- Animaciones ---

        public const string Quieto = "Quieto";
        public const string Andar = "Andar";
        public const string Muerte = "Muerte";

        /// <summary>El estado de cada pose se llama como ella; ANDA son dos, Quieto y Andar (los de su controlador).</summary>
        public static string EstadoDe(POSES_VERDUGO pose) => pose.ToString();

        /// <summary>Las poses que tienen su estado (todas menos ANDA, que manda el controlador).</summary>
        public static IEnumerable<POSES_VERDUGO> PosesConEstado =>
            ((POSES_VERDUGO[])System.Enum.GetValues(typeof(POSES_VERDUGO))).Where(p => p != POSES_VERDUGO.ANDA);

        /// <summary>
        /// - Anda despacio con sus cuatro pasos, al ritmo de su cuerpo (<see cref="VelocidadNaturalAndar"/>).
        /// - Los avisos se quedan en su fotograma lo que dure el aviso: el hacha en alto (H1), echada atrás
        ///   (B1) y el garfio volteando (C1). Los golpes, en el suyo: el hachazo en el suelo (H2), el
        ///   barrido (B2), la cadena lanzada (C2); y tras ellos, saca el hacha (H3), se pone en guardia (W2)
        ///   y tira de la cadena (C3).
        /// - De rodillas tras una explosión, se balancea (K1, K2). A ciegas, anda.
        /// - Muere y se queda la capucha vacía sobre el montón de cadenas (M4) antes de desvanecerse.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Verdugo_Quieto", Estado = Quieto, Fotogramas = new[] { "W2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Verdugo_Andar", Estado = Andar, Fotogramas = new[] { "W1", "W2", "W3", "W4" }, Duraciones = new[] { 0.3f, 0.3f, 0.3f, 0.3f }, Bucle = true },
            new Clip { Nombre = "Verdugo_AlzaElHacha", Estado = EstadoDe(POSES_VERDUGO.ALZA_EL_HACHA), Fotogramas = new[] { "H1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Verdugo_Hachazo", Estado = EstadoDe(POSES_VERDUGO.HACHAZO), Fotogramas = new[] { "H2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Verdugo_SacaElHacha", Estado = EstadoDe(POSES_VERDUGO.SACA_EL_HACHA), Fotogramas = new[] { "H3" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Verdugo_PreparaElBarrido", Estado = EstadoDe(POSES_VERDUGO.PREPARA_EL_BARRIDO), Fotogramas = new[] { "B1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Verdugo_Barrido", Estado = EstadoDe(POSES_VERDUGO.BARRIDO), Fotogramas = new[] { "B2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Verdugo_TrasElBarrido", Estado = EstadoDe(POSES_VERDUGO.TRAS_EL_BARRIDO), Fotogramas = new[] { "W2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Verdugo_VolteaElGarfio", Estado = EstadoDe(POSES_VERDUGO.VOLTEA_EL_GARFIO), Fotogramas = new[] { "C1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Verdugo_Lanza", Estado = EstadoDe(POSES_VERDUGO.LANZA), Fotogramas = new[] { "C2" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Verdugo_Tira", Estado = EstadoDe(POSES_VERDUGO.TIRA), Fotogramas = new[] { "C3" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Verdugo_DeRodillas", Estado = EstadoDe(POSES_VERDUGO.DE_RODILLAS), Fotogramas = new[] { "K1", "K2" }, Duraciones = new[] { 0.5f, 0.5f }, Bucle = true },
            new Clip { Nombre = "Verdugo_Busca", Estado = EstadoDe(POSES_VERDUGO.BUSCA), Fotogramas = new[] { "W1", "W2", "W3", "W4" }, Duraciones = new[] { 0.3f, 0.3f, 0.3f, 0.3f }, Bucle = true },
            new Clip { Nombre = "Verdugo_Muerte", Estado = Muerte, Fotogramas = new[] { "M1", "M2", "M3", "M4" }, Duraciones = new[] { 0.3f, 0.35f, 0.35f, ClipsDeSprites.SeQueda } },
        };

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        /// <summary>A esta velocidad (u/s, la de su ronda) el paso no patina con el clip a ritmo 1.</summary>
        public const float VelocidadNaturalAndar = 0.8f;

        /// <summary>Segundos que se queda la capucha vacía (M4) antes de desvanecerse.</summary>
        public const float RestosEnPantalla = 1.2f;

        // La tierra y la ceniza de su muerte, como la de los Penitentes.
        public static readonly Color ColorTierra = ConstructorPenitente.ColorTierra;
        public static readonly Color ColorCeniza = ConstructorPenitente.ColorCeniza;

        // --- Geometría (u, desde el pivote y mirando a la derecha) ---

        public static readonly Vector2 TamanoCuerpo = GeneradorEnemigos.TamanoVerdugo;

        public static readonly Vector2 TamanoContacto = new Vector2(1.2f, 2.8f);

        /// <summary>De dónde sale el polvo al morir: el pecho.</summary>
        public static readonly Vector2 Pecho = new Vector2(0f, 2f);

        /// <summary>Su mano al lanzar la cadena, desde el pivote y mirando a la derecha (u).</summary>
        public static Vector2 ManoDelDibujo => new Vector2((Ancla.x - Mano.x) / PixelesPorUnidad, (Ancla.y - Mano.y) / PixelesPorUnidad);

        // --- Construcción ---

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El prefab del Verdugo con arte, o null si todavía no se construyó.</summary>
        public static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "el Verdugo", out var sprites))
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
            Debug.Log($"[Fragmentum] Verdugo: {sprites.Count} fotogramas, {Clips.Length} clips, {RutaControlador} y {RutaPrefab}.");
            return ok;
        }

        /// <summary>La muerte avisa al llegar a la capucha vacía: ahí empieza a desvanecerse.</summary>
        public static IEnumerable<AnimationEvent> EventosDe(Clip clip) => clip.Estado == Muerte
            ? new[] { EnemigosConArte.Evento(nameof(EnemigoAnimacion.FinMuerte), clip.Inicio(clip.Fotogramas.Length - 1)) }
            : Enumerable.Empty<AnimationEvent>();

        /// <summary>
        /// - Con «Estado» en 0 (ANDA) manda su controlador: anda si se mueve y se para si no.
        /// - Cada otra pose, desde cualquier estado; al volver a 0, se queda quieto.
        /// - La muerte va primero e interrumpe todo; no tiene salida.
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
                });

            var estado = AnimatorParams.EstadoMecanica.Nombre;
            AnimatorCondition Es(POSES_VERDUGO pose) => ControladoresAnimator.Condicion(AnimatorConditionMode.Equals, estado, (int)pose);
            AnimatorCondition Si(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.If, p.Nombre);
            AnimatorCondition No(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.IfNot, p.Nombre);
            var manda = Es(POSES_VERDUGO.ANDA);

            ControladoresAnimator.Transiciones(estados[Quieto], new Transicion("Anda", estados[Andar], null, Si(AnimatorParams.MovEn), manda));
            ControladoresAnimator.Transiciones(estados[Andar], new Transicion("Se para", estados[Quieto], null, No(AnimatorParams.MovEn)));
            foreach (var pose in PosesConEstado)
            {
                ControladoresAnimator.Transiciones(estados[EstadoDe(pose)], new Transicion("Vuelve a andar", estados[Quieto], null, manda));
            }

            ControladoresAnimator.Transiciones(estados[Muerte]);

            var desdeCualquiera = new List<Transicion> { new Transicion("Muere", estados[Muerte], null, Si(AnimatorParams.Death)) };
            desdeCualquiera.AddRange(PosesConEstado.Select(p => new Transicion(EstadoDe(p), estados[EstadoDe(p)], null, Es(p), No(AnimatorParams.Death))));
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
                // Sus golpes los lleva Verdugo; el arma del controlador no se usa.
                CentroArma = new Vector2(1.2f, 1.5f),
                TamanoArma = new Vector2(1f, 1f),
            };

            return EnemigosConArte.CrearPrefab(NombreDatos, RutaPrefab, datos, reposo, controlador, medidas, armazon =>
            {
                // Dibujado mirando a la izquierda: volteado, mira a la derecha como los demás.
                armazon.Sprite.flipX = true;
                armazon.Contacto.gameObject.SetActive(false);

                var estallido = particulas != null
                    ? ConstructorEcoIra.CrearEstallido(armazon.Pivote, armazon.Raiz.layer, particulas, Pecho,
                        ColorTierra, ColorCeniza, 1f / PixelesPorUnidad, 0.8f)
                    : null;
                Asignar(armazon.Animacion, "velocidadNaturalCarrera", p => p.floatValue = VelocidadNaturalAndar);
                Asignar(armazon.Animacion, "estallido", p => p.objectReferenceValue = estallido);
                Asignar(armazon.Animacion, "restosEnPantalla", p => p.floatValue = RestosEnPantalla);

                var verdugo = armazon.Raiz.AddComponent<Verdugo>();
                GeneradorEnemigos.ArmarGolpesDelVerdugo(armazon.Raiz, armazon.Pivote, verdugo);
                verdugo.manoCadena = ManoDelDibujo.x;
                verdugo.alturaMano = ManoDelDibujo.y;

                var (garfio, cadena) = CrearCadena(armazon.Raiz);
                Asignar(verdugo, "garfio", p => p.objectReferenceValue = garfio);
                Asignar(verdugo, "cadena", p => p.objectReferenceValue = cadena);
                Asignar(verdugo, "pivote", p => p.objectReferenceValue = armazon.Pivote);
                Asignar(verdugo, "sprite", p => p.objectReferenceValue = armazon.Sprite);
                Asignar(verdugo, "animator", p => p.objectReferenceValue = armazon.Animator);
            });
        }

        // El garfio y la cadena dibujados, apagados hasta que la lanza. Sin su arte, el garfio es un cuadrado.
        private static (Transform garfio, SpriteRenderer cadena) CrearCadena(GameObject raiz)
        {
            var objetoGarfio = new GameObject("Garfio") { layer = raiz.layer };
            objetoGarfio.transform.SetParent(raiz.transform, false);
            var dibujoGarfio = objetoGarfio.AddComponent<SpriteRenderer>();
            dibujoGarfio.sortingOrder = 7;
            objetoGarfio.SetActive(false);

            var spriteGarfio = ConstructorEfectosMundo4.Garfio;
            var spriteCadena = ConstructorEfectosMundo4.Cadena;
            if (spriteGarfio == null || spriteCadena == null)
            {
                Debug.LogWarning("[Fragmentum] Falta el arte del garfio o de la cadena: el Verdugo lanza un cuadrado.");
                dibujoGarfio.sprite = SpriteCuadrado;
                dibujoGarfio.color = new Color(0.4f, 0.32f, 0.27f, 1f);
                objetoGarfio.transform.localScale = Vector3.one * 0.3f;
                return (objetoGarfio.transform, null);
            }

            dibujoGarfio.sprite = spriteGarfio;

            var objetoCadena = new GameObject("Cadena") { layer = raiz.layer };
            objetoCadena.transform.SetParent(raiz.transform, false);
            var cadena = objetoCadena.AddComponent<SpriteRenderer>();
            cadena.sprite = spriteCadena;
            cadena.drawMode = SpriteDrawMode.Tiled;
            cadena.size = new Vector2(1f, spriteCadena.bounds.size.y);
            cadena.sortingOrder = 6;
            objetoCadena.SetActive(false);
            return (objetoGarfio.transform, cadena);
        }
    }
}
