using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;
using Transicion = Fragmentum.EditorTools.ControladoresAnimator.Transicion;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// La Doncella del Lamento, jefe del Mundo 2, con su arte:
    /// 1. importa sus 28 fotogramas, todos en el mismo lienzo (260×294) y con el pivote abajo,
    ///    bajo el pecho: donde acaba su cola de agua;
    /// 2. crea sus clips desde <see cref="Clips"/>: flotar, el gesto de cada ataque (ola,
    ///    inundación y congelar), el rugido, el llanto (baja, llora en bucle y se alza) y la muerte;
    /// 3. arma su controlador: los gestos salen con su disparador (<see cref="AnimacionJefe"/>) y
    ///    el llanto sigue al entero «Estado» que pone <see cref="DoncellaLamento"/>;
    /// 4. viste el prefab del jefe (<see cref="Vestir"/>, lo llama <c>GeneradorJefes</c>): los
    ///    fotogramas, el Animator, la cola fundida y el fragmento que deja al caer.
    ///
    /// <b>Está dibujada mirando a la izquierda:</b> su <see cref="SpriteRenderer"/> va volteado.
    ///
    /// <b>La cola se sale del lienzo por abajo</b> y acaba en un corte recto. Decidido el
    /// 2026-10-10: los dibujos se conservan y el juego funde las últimas filas
    /// (<see cref="ColaFundida"/> y el shader <c>Fragmentum/SpriteFundidoAbajo</c>).
    ///
    /// <b>F4 no se usa:</b> donde se pidió que el pelo se alzara, salió una masa de agua sobre la
    /// cabeza, y en el bucle de flotar parpadearía. Flota con F1, F2, F3 y F2.
    ///
    /// Los PNG vienen de <c>Tesis/Imagenes/limpios/Mundo2/Doncella</c>. Idempotente.
    /// </summary>
    public static class ConstructorDoncella
    {
        public const string CarpetaSprites = TablaArte.CarpetaJefes + "/Doncella";
        public const string CarpetaAnimaciones = "Assets/Animaciones/Jefes/Doncella";
        public const string NombreDatos = "DoncellaLamento";
        public const string RutaControlador = CarpetaAnimaciones + "/DoncellaLamento.controller";
        public const string RutaMaterial = CarpetaSprites + "/Materiales/ColaFundida.mat";
        public const string RutaMaterialHielo = CarpetaSprites + "/Materiales/DestelloDeHielo.mat";
        public const string ShaderCola = "Fragmentum/SpriteFundidoAbajo";

        /// <summary>El hielo de la paleta del mundo (#E8EEF4): la silueta de un golpe que no le hace nada.</summary>
        public static readonly Color ColorHielo = new Color32(232, 238, 244, 255);

        public static string RutaSprite(string archivo) => $"{CarpetaSprites}/{archivo}.png";

        // --- Escala y pivote ---

        public static readonly Vector2Int Lienzo = new Vector2Int(260, 294);

        /// <summary>El lienzo entero, de la capucha al corte de la cola: a 92 por unidad, las 3,2 u del diseño.</summary>
        public const float PixelesPorUnidad = 92f;

        /// <summary>
        /// Abajo del todo, bajo el pecho (la gema del pecho está en la columna 108 y la capucha se
        /// inclina hasta la 118): flotando, ahí acaba su cola; llorando, ahí se apoya.
        /// </summary>
        public static readonly Vector2 Pivote = new Vector2(115f / 260f, 0f);

        /// <summary>El fragmento suelto (17×25), a la escala a la que mediría las 0,6 u del diseño. Hoy no se usa: el que se recoge es el de M5.</summary>
        public const float PixelesPorUnidadFragmento = 42f;

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(CarpetaSprites + "/", PixelesPorUnidad, Pivote,
                "Doncella del Lamento: 28 fotogramas de 260×294 con el pivote abajo, bajo el pecho; 3,2 u de alto.", Lienzo),
            new TablaArte.Regla(CarpetaSprites + "/Fragmento", PixelesPorUnidadFragmento, null,
                "Fragmento de la Tristeza, suelto: 17×25, el pivote en el centro.", new Vector2Int(17, 25)),
        };

        // --- Animaciones ---

        public const string Flotar = "Flotar";
        public const string Bajar = "Bajar";
        public const string Llanto = "Llanto";
        public const string Alzarse = "Alzarse";
        public const string Muerte = "Muerte";

        // El estado de cada gesto se llama como su disparador: así lo encuentra AnimacionJefe.
        public static readonly string Ola = AnimatorParams.Ola.Nombre;
        public static readonly string Inundar = AnimatorParams.Inundar.Nombre;
        public static readonly string Congelar = AnimatorParams.Congelar.Nombre;
        public static readonly string Rugido = AnimatorParams.Rugido.Nombre;

        /// <summary>
        /// Los tiempos son los de sus patrones (<c>Assets/Data/Jefes/Patrones</c>): cada gesto
        /// llega a su momento justo cuando acaba el aviso.
        /// - Ola: O1 y O2 recogen el agua 0,7 s (el aviso) y O3 la lanza: ahí sale la ola.
        /// - Inundar: I1 e I2 alzan los brazos 1 s y en I3 el agua empieza a subir.
        /// - Congelar: C1 y C2 forman la escarcha 0,8 s y en C3 el agua se congela.
        /// - Rugido: 0,9 s, lo que espera al empezar la fase 2.
        /// - El llanto no tiene tiempo propio: L1 mientras baja, L2 y L3 mientras llora y L4 al alzarse.
        /// - M5, el charco con el fragmento, se queda en pantalla al acabar la muerte.
        /// </summary>
        public static readonly Clip[] Clips =
        {
            new Clip { Nombre = "Doncella_Flotar", Estado = Flotar, Fotogramas = new[] { "F1", "F2", "F3", "F2" }, Duraciones = new[] { 0.25f, 0.25f, 0.25f, 0.25f }, Bucle = true },
            new Clip { Nombre = "Doncella_Ola", Estado = Ola, Fotogramas = new[] { "O1", "O2", "O3", "O4" }, Duraciones = new[] { 0.35f, 0.35f, 0.20f, 0.30f } },
            new Clip { Nombre = "Doncella_Inundar", Estado = Inundar, Fotogramas = new[] { "I1", "I2", "I3", "I4" }, Duraciones = new[] { 0.50f, 0.50f, 0.30f, 0.30f } },
            new Clip { Nombre = "Doncella_Congelar", Estado = Congelar, Fotogramas = new[] { "C1", "C2", "C3", "C4" }, Duraciones = new[] { 0.40f, 0.40f, 0.25f, 0.30f } },
            new Clip { Nombre = "Doncella_Rugido", Estado = Rugido, Fotogramas = new[] { "R1", "R2", "R3" }, Duraciones = new[] { 0.25f, 0.40f, 0.25f } },
            new Clip { Nombre = "Doncella_Bajar", Estado = Bajar, Fotogramas = new[] { "L1" }, Duraciones = new[] { ClipsDeSprites.SeQueda } },
            new Clip { Nombre = "Doncella_Llanto", Estado = Llanto, Fotogramas = new[] { "L2", "L3" }, Duraciones = new[] { 0.30f, 0.30f }, Bucle = true },
            new Clip { Nombre = "Doncella_Alzarse", Estado = Alzarse, Fotogramas = new[] { "L4" }, Duraciones = new[] { 0.30f } },
            new Clip { Nombre = "Doncella_Muerte", Estado = Muerte, Fotogramas = new[] { "M1", "M2", "M3", "M4", "M5" }, Duraciones = new[] { 0.30f, 0.30f, 0.40f, 0.60f, ClipsDeSprites.SeQueda } },
        };

        /// <summary>El fotograma en que llega el momento de cada gesto (su evento Golpe).</summary>
        public static readonly (string estado, string fotograma)[] Momentos =
        {
            (AnimatorParams.Ola.Nombre, "O3"),
            (AnimatorParams.Inundar.Nombre, "I3"),
            (AnimatorParams.Congelar.Nombre, "C3"),
        };

        /// <summary>Los fotogramas que no acaban en la cola cortada: no se funden por abajo.</summary>
        public static readonly string[] FotogramasEnteros = { "L2", "L3", "M4", "M5" };

        /// <summary>Las 22 filas de abajo se funden: 0,24 u de cola.</summary>
        public const float FraccionFundida = 22f / 294f;

        public static Clip ClipDe(string estado) => Clips.First(c => c.Estado == estado);

        public static string RutaClip(string estado) => ClipDe(estado).Ruta(CarpetaAnimaciones);

        /// <summary>Del inicio del clip de un gesto a su momento.</summary>
        public static float Anticipacion(string estado)
        {
            var clip = ClipDe(estado);
            return clip.Inicio(System.Array.IndexOf(clip.Fotogramas, Momentos.First(m => m.estado == estado).fotograma));
        }

        /// <summary>La derrota dura lo que tarda la muerte en llegar a M5.</summary>
        public static float DuracionDerrota => ClipDe(Muerte).Inicio(ClipDe(Muerte).Fotogramas.Length - 1);

        // --- Construcción ---

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        [MenuItem("Fragmentum/Jefes/Generar los clips y el Animator de la Doncella", priority = 144)]
        public static void MenuConstruir()
        {
            if (Construir() && GeneradorJefes.CrearPrefabs())
            {
                EditorGUIUtility.PingObject(Controlador);
            }
        }

        /// <summary>
        /// Fija los ajustes de importación y crea los clips, el controlador y el material de la
        /// cola. Devuelve false solo si algo falló; si faltan fotogramas avisa y no construye nada.
        /// </summary>
        public static bool Construir()
        {
            if (!ClipsDeSprites.CargarSprites(Clips, RutaSprite, "la Doncella del Lamento", out var sprites))
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
            if (CrearMaterial() == null || CrearMaterialDeHielo() == null)
            {
                return false;
            }

            AjustarDatos();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Fragmentum] Doncella del Lamento: {sprites.Count} fotogramas, {Clips.Length} clips y su controlador en {CarpetaAnimaciones}.");
            return true;
        }

        /// <summary>Cada gesto avisa de su momento (Golpe) y la muerte, de que llegó al charco (FinMuerte).</summary>
        public static IEnumerable<AnimationEvent> EventosDe(Clip clip)
        {
            if (Momentos.Any(m => m.estado == clip.Estado))
            {
                return new[] { EnemigosConArte.Evento(nameof(AnimacionJefe.Golpe), Anticipacion(clip.Estado)) };
            }

            return clip.Estado == Muerte
                ? new[] { EnemigosConArte.Evento(nameof(AnimacionJefe.FinMuerte), DuracionDerrota) }
                : Enumerable.Empty<AnimationEvent>();
        }

        /// <summary>
        /// - Flota por defecto. Los gestos y el rugido salen desde cualquier estado con su
        ///   disparador (un gesto puede repetirse: la ola doble) y vuelven a flotar al acabar.
        /// - El llanto sigue a «Estado» (<see cref="ESTADOS_LLANTO"/>): baja con L1, llora en bucle
        ///   y, cuando se cierra la ventana, se alza y vuelve a flotar.
        /// - La muerte va primero e interrumpe todo; no tiene salida (se queda en M5).
        ///
        /// Walk y Clavar no mueven nada: están porque <see cref="AnimacionJefe"/> los escribe.
        /// </summary>
        private static AnimatorController ArmarControlador(IReadOnlyDictionary<string, Motion> clips)
        {
            var controlador = ControladoresAnimator.CargarOCrear(RutaControlador);
            ControladoresAnimator.AsegurarParametros(controlador,
                (AnimatorParams.EstadoMecanica.Nombre, AnimatorControllerParameterType.Int, 0f),
                (AnimatorParams.Walk.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Clavar.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Death.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Rugido.Nombre, AnimatorControllerParameterType.Trigger, 0f),
                (AnimatorParams.Ola.Nombre, AnimatorControllerParameterType.Trigger, 0f),
                (AnimatorParams.Inundar.Nombre, AnimatorControllerParameterType.Trigger, 0f),
                (AnimatorParams.Congelar.Nombre, AnimatorControllerParameterType.Trigger, 0f));

            var maquina = controlador.layers[0].stateMachine;
            var estados = ControladoresAnimator.Estados(maquina, Clips.Select(c => (c.Estado, clips[c.Estado])).ToList());

            var estado = AnimatorParams.EstadoMecanica.Nombre;
            AnimatorCondition Es(ESTADOS_LLANTO valor) => ControladoresAnimator.Condicion(AnimatorConditionMode.Equals, estado, (int)valor);
            AnimatorCondition Si(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.If, p.Nombre);
            AnimatorCondition No(in AnimatorParams.Parametro p) => ControladoresAnimator.Condicion(AnimatorConditionMode.IfNot, p.Nombre);

            var gestos = new[] { AnimatorParams.Ola, AnimatorParams.Inundar, AnimatorParams.Congelar };

            ControladoresAnimator.Transiciones(estados[Flotar]);
            foreach (var gesto in gestos)
            {
                ControladoresAnimator.Transiciones(estados[gesto.Nombre], new Transicion("Termina el gesto", estados[Flotar], 1f));
            }

            ControladoresAnimator.Transiciones(estados[Rugido], new Transicion("Termina el rugido", estados[Flotar], 1f));
            ControladoresAnimator.Transiciones(estados[Bajar], new Transicion("No llega a llorar", estados[Flotar], null, Es(ESTADOS_LLANTO.FLOTA)));
            ControladoresAnimator.Transiciones(estados[Llanto], new Transicion("Se cierra la ventana", estados[Alzarse], null, Es(ESTADOS_LLANTO.FLOTA)));
            ControladoresAnimator.Transiciones(estados[Alzarse], new Transicion("Vuelve a flotar", estados[Flotar], 1f));
            ControladoresAnimator.Transiciones(estados[Muerte]);

            var desdeCualquiera = new List<Transicion>
            {
                new Transicion("Muere", estados[Muerte], null, Si(AnimatorParams.Death)),
                new Transicion("Ruge", estados[Rugido], null, Si(AnimatorParams.Rugido), No(AnimatorParams.Death)),
            };
            desdeCualquiera.AddRange(gestos.Select(g => new Transicion(g.Nombre, estados[g.Nombre], null, Si(g), No(AnimatorParams.Death))));
            desdeCualquiera.Add(new Transicion("Baja a llorar", estados[Bajar], null, Es(ESTADOS_LLANTO.BAJA), No(AnimatorParams.Death)));
            desdeCualquiera.Add(new Transicion("Llora", estados[Llanto], null, Es(ESTADOS_LLANTO.LLORA), No(AnimatorParams.Death)));
            ControladoresAnimator.DesdeCualquierEstado(maquina, desdeCualquiera.ToArray());

            // Un gesto puede repetirse desde sí mismo (la segunda ola de la ola doble).
            foreach (var transicion in maquina.anyStateTransitions.Where(t => gestos.Any(g => g.Nombre == t.name)))
            {
                transicion.canTransitionToSelf = true;
            }

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        private static Material CrearMaterial()
        {
            var shader = Shader.Find(ShaderCola);
            if (shader == null)
            {
                Debug.LogError($"[Fragmentum] No se encontró el shader {ShaderCola}.");
                return null;
            }

            AsegurarCarpeta(System.IO.Path.GetDirectoryName(RutaMaterial)?.Replace('\\', '/'));
            var material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, RutaMaterial);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }

            if (!Mathf.Approximately(material.GetFloat("_Fundido"), FraccionFundida))
            {
                material.SetFloat("_Fundido", FraccionFundida);
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        // La silueta del Eco (un color liso con el alfa del sprite), en el hielo de la paleta.
        private static Material CrearMaterialDeHielo()
        {
            var shader = Shader.Find(ConstructorEcoIra.ShaderDestello);
            if (shader == null)
            {
                Debug.LogError($"[Fragmentum] No se encontró el shader {ConstructorEcoIra.ShaderDestello}.");
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialHielo);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, RutaMaterialHielo);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }

            if (material.GetColor("_Color") != ColorHielo)
            {
                material.SetColor("_Color", ColorHielo);
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        /// <summary>
        /// El arte manda sobre dos datos del jefe: sin tinte (los colores son los del dibujo) y la
        /// derrota dura lo que tarda la muerte en llegar a M5.
        /// </summary>
        private static void AjustarDatos()
        {
            var datos = GeneradorJefes.CargarDatos(NombreDatos);
            if (datos == null || (datos.tinte == Color.white && Mathf.Approximately(datos.duracionDerrota, DuracionDerrota)))
            {
                return;
            }

            datos.tinte = Color.white;
            datos.duracionDerrota = DuracionDerrota;
            EditorUtility.SetDirty(datos);
        }

        /// <summary>
        /// Viste el prefab de la Doncella con su arte; lo llama <c>GeneradorJefes</c> al crearlo.
        /// Devuelve false si el arte no está generado: entonces se queda con su cápsula provisional.
        /// </summary>
        public static bool Vestir(GameObject objetoSprite, Transform pivote, BossController jefe)
        {
            var controlador = Controlador;
            var reposo = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite(ClipDe(Flotar).Fotogramas[0]));
            var material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (controlador == null || reposo == null || material == null)
            {
                return false;
            }

            objetoSprite.transform.localPosition = Vector3.zero;
            objetoSprite.transform.localScale = Vector3.one;

            var sprite = objetoSprite.GetComponent<SpriteRenderer>();
            sprite.sprite = reposo;
            sprite.color = Color.white;
            // Dibujada mirando a la izquierda: volteada, mira a la derecha como los demás jefes.
            sprite.flipX = true;
            sprite.sharedMaterial = material;

            var animator = objetoSprite.GetComponent<Animator>();
            animator.runtimeAnimatorController = controlador;
            animator.applyRootMotion = false;
            // Los eventos marcan el combate: tienen que llegar aunque el jefe esté fuera de cámara.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var arte = objetoSprite.AddComponent<AnimacionJefe>();
            Asignar(arte, "anticipaciones", p =>
            {
                p.arraySize = Momentos.Length;
                for (var i = 0; i < Momentos.Length; i++)
                {
                    var elemento = p.GetArrayElementAtIndex(i);
                    elemento.FindPropertyRelative("estado").stringValue = Momentos[i].estado;
                    elemento.FindPropertyRelative("segundos").floatValue = Anticipacion(Momentos[i].estado);
                }
            });
            var destello = AssetDatabase.LoadAssetAtPath<Material>(ConstructorEcoIra.RutaMaterialDestello);
            if (destello == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {ConstructorEcoIra.RutaMaterialDestello}: la Doncella no destellará al recibir golpes.");
            }

            Asignar(arte, "materialDestello", p => p.objectReferenceValue = destello);
            Asignar(jefe, "arte", p => p.objectReferenceValue = arte);
            Asignar(jefe, "destelloDeHielo", p => p.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialHielo));

            var cola = objetoSprite.AddComponent<ColaFundida>();
            Asignar(cola, "fraccion", p => p.floatValue = FraccionFundida);
            Asignar(cola, "enteros", p =>
            {
                p.arraySize = FotogramasEnteros.Length;
                for (var i = 0; i < FotogramasEnteros.Length; i++)
                {
                    p.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprite(FotogramasEnteros[i]));
                }
            });

            Asignar(jefe, "fragmento", p => p.objectReferenceValue = CrearFragmento(pivote, sprite));
            return true;
        }

        /// <summary>
        /// El fragmento que flota sobre el charco en M5, medido en el dibujo (como el de la
        /// Centinela en D4). El dibujo va volteado, así que su x cambia de signo.
        /// </summary>
        public static (Vector2 centro, Vector2 tamano)? MedirFragmento()
        {
            var medida = ConstructorCentinela.MedirFragmento(RutaSprite(ClipDe(Muerte).Fotogramas.Last()), Pivote, PixelesPorUnidad);
            return medida is { } m ? (new Vector2(-m.centro.x, m.centro.y), m.tamano) : ((Vector2, Vector2)?)null;
        }

        private static FragmentoJefe CrearFragmento(Transform pivote, SpriteRenderer dibujo)
        {
            var medida = MedirFragmento();
            if (medida == null)
            {
                Debug.LogWarning("[Fragmentum] No se encontró el fragmento en M5: la Doncella no dejará fragmento que recoger.");
                return null;
            }

            return ConstructorCentinela.CrearFragmento(pivote, dibujo, medida.Value, PixelesPorUnidad);
        }
    }
}
