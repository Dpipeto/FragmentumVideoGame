using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El Golem de Ceniza, mini-jefe del Mundo 1, con su arte:
    /// 1. importa los 15 fotogramas de <see cref="CarpetaSprites"/> como pixel art, con el mismo
    ///    lienzo (90×92) y el pivote en la línea de suelo (fila 88) para todos: se puede cortar de
    ///    cualquier clip a cualquier otro sin que el sprite salte. Los ajustes están en
    ///    <see cref="TablaArte"/>;
    /// 2. crea los 4 clips desde <see cref="Clips"/>, con los tiempos del diseño y los eventos del
    ///    golpe y de la muerte (<see cref="Eventos"/>);
    /// 3. arma su controlador (<see cref="RutaControlador"/>), con un estado por clip;
    /// 4. monta el prefab <c>GolemCeniza</c>: el mismo <see cref="EnemyController"/> que los demás
    ///    enemigos, con sus datos (<c>GolemCeniza.asset</c>), y <see cref="EnemigoAnimacion"/>.
    ///
    /// No es un jefe: no tiene fases ni umbrales. Lo que el encargo pedía exponer (vida máxima,
    /// velocidades, rango de detección, distancia de ataque y tiempo entre ataques) está en sus
    /// datos. Tiene hiperarmadura: los golpes lo hacen destellar pero no cortan su ataque.
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
        public const string NombreDatos = "GolemCeniza";
        public const string RutaPrefab = GeneradorEnemigos.CarpetaPrefabs + "/" + NombreDatos + ".prefab";

        /// <summary>El Golem provisional de la Fase 4 (una cápsula), al que sustituye en las escenas.</summary>
        public const string RutaPrefabSinArte = GeneradorEnemigos.CarpetaPrefabs + "/Enemigo_" + NombreDatos + ".prefab";

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

        /// <summary>
        /// Un evento de animación: la función de <see cref="EnemigoAnimacion"/> a la que llama al
        /// empezar un fotograma (o, sin fotograma, al acabar el clip).
        /// </summary>
        public sealed class Evento
        {
            public string Estado;
            public string Fotograma;
            public string Funcion;
        }

        /// <summary>
        /// Los momentos del dibujo que marcan el combate:
        /// - AbrirGolpe en A3 y CerrarGolpe en A4: la onda del puñetazo al suelo hace daño solo los
        ///   0,08 s de A3;
        /// - FinAtaque al acabar A5: la FSM vuelve a perseguir;
        /// - FinMuerte al llegar a D4: los restos se quedan un momento y se desvanecen.
        /// </summary>
        public static readonly Evento[] Eventos =
        {
            new Evento { Estado = Ataque, Fotograma = "Golem_A3", Funcion = nameof(EnemigoAnimacion.AbrirGolpe) },
            new Evento { Estado = Ataque, Fotograma = "Golem_A4", Funcion = nameof(EnemigoAnimacion.CerrarGolpe) },
            new Evento { Estado = Ataque, Fotograma = null, Funcion = nameof(EnemigoAnimacion.FinAtaque) },
            new Evento { Estado = Muerte, Fotograma = "Golem_D4", Funcion = nameof(EnemigoAnimacion.FinMuerte) },
        };

        /// <summary>Instante del evento dentro de su clip: el inicio de su fotograma o, sin él, el final del clip.</summary>
        public static float Instante(Evento evento)
        {
            var clip = ClipDe(evento.Estado);
            return evento.Fotograma == null ? clip.Duracion : clip.Inicio(System.Array.IndexOf(clip.Fotogramas, evento.Fotograma));
        }

        // --- Andar ---

        /// <summary>
        /// Velocidad del cuerpo (u/s) a la que el andar va a los tiempos de la tabla (0,13 s por
        /// fotograma). El dibujo no tiene zancada que medir (el pie de atrás no se mueve en los
        /// cuatro fotogramas), así que la referencia es su velocidad persiguiendo: al patrullar, a
        /// 1 u/s, anda a la mitad de ritmo. La recibe <see cref="EnemigoAnimacion"/>.
        /// </summary>
        public const float VelocidadNaturalAndar = 2f;

        /// <summary>
        /// Por debajo de este ritmo de andar (velocidad real / natural) el Golem se para y respira;
        /// por encima de <see cref="RitmoEnMarcha"/> vuelve a andar. Entre los dos no cambia: así no
        /// parpadea entre Idle y Andar cuando arranca o frena.
        /// </summary>
        public const float RitmoParado = 0.05f;

        public const float RitmoEnMarcha = 0.15f;

        // --- Geometría (u, desde los pies y mirando a la derecha), medida en el arte a 24 px/u ---

        /// <summary>El cuerpo: de 63 px de alto (2,6 u) con los brazos; el tronco ocupa ~1,5 u.</summary>
        public static readonly Vector2 TamanoCuerpo = new Vector2(1.5f, 2.55f);

        public static readonly Vector2 TamanoContacto = new Vector2(1.3f, 2.3f);

        /// <summary>
        /// La onda de A3, ancha y baja: del pecho (x 50) a más allá del puño que toca el suelo
        /// (x 90), y de la línea de suelo a la fila 76. Se esquiva saltando.
        /// </summary>
        public static readonly Rect Onda = Rect.MinMaxRect(0.42f, -0.05f, 2.2f, 0.55f);

        /// <summary>Segundos que los restos (D4) se quedan en pantalla antes de desvanecerse.</summary>
        public const float RestosEnPantalla = 0.7f;

        // --- Menús ---

        [MenuItem("Fragmentum/Enemigos/Sustituir el Golem de Ceniza sin arte de la escena abierta", priority = 86)]
        public static void MenuSustituirEnAbierta()
        {
            var escena = SceneManager.GetActiveScene();
            var cuantos = SustituirGolemsSinArte(escena);
            if (cuantos > 0)
            {
                EditorSceneManager.MarkSceneDirty(escena);
            }

            Debug.Log($"[Fragmentum] {cuantos} Golem(s) de Ceniza sin arte sustituido(s) en '{escena.name}'.");
        }

        // --- Construcción ---

        public const string RutaControlador = CarpetaAnimaciones + "/GolemCeniza.controller";

        public static AnimatorController Controlador => AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);

        /// <summary>El prefab del Golem con arte, o null si todavía no se construyó.</summary>
        public static GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);

        /// <summary>
        /// Fija los ajustes de importación y crea los clips, el controlador y el prefab. Devuelve
        /// false solo si algo falló; si faltan fotogramas avisa y no construye nada.
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
            var clips = Clips.ToDictionary(c => c.Estado, c => (Motion)ClipsDeSprites.Crear(CarpetaAnimaciones, c, sprites, EventosDe(c)));
            var controlador = ArmarControlador(clips);

            GeneradorEnemigos.GenerarDatos(false);
            if (!AjustarDatos())
            {
                return false;
            }

            var ok = CrearPrefab(sprites[ClipDe(Idle).Fotogramas[0]], controlador) != null;
            Debug.Log($"[Fragmentum] Golem de Ceniza: {sprites.Count} fotogramas, {Clips.Length} clips, {RutaControlador} y {RutaPrefab}.");
            return ok;
        }

        private static IEnumerable<AnimationEvent> EventosDe(Clip clip) => Eventos
            .Where(e => e.Estado == clip.Estado)
            .Select(e => new AnimationEvent { time = Instante(e), functionName = e.Funcion });

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
        private static AnimatorController ArmarControlador(IReadOnlyDictionary<string, Motion> clips)
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
            return controlador;
        }

        // --- Datos ---

        /// <summary>
        /// Lo que el arte cambia en sus datos: tinte blanco (los colores son del dibujo), mira a la
        /// derecha, hiperarmadura, y la duración del ataque y su ventana como en el clip (solo se
        /// usan si el prefab se queda sin animación). Lo demás (vida, velocidades, rangos) no se
        /// toca: se ajusta en el asset.
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
            var cambia = datos.tinte != Color.white || !datos.spriteMiraDerecha || !datos.hiperarmadura ||
                         !Mathf.Approximately(datos.duracionAtaque, ataque.Duracion) ||
                         !Mathf.Approximately(datos.inicioVentanaGolpe, ataque.Inicio(1)) ||
                         !Mathf.Approximately(datos.finVentanaGolpe, ataque.Inicio(2));
            if (cambia)
            {
                datos.tinte = Color.white;
                datos.spriteMiraDerecha = true;
                datos.hiperarmadura = true;
                datos.duracionAtaque = ataque.Duracion;
                datos.inicioVentanaGolpe = ataque.Inicio(1);
                datos.finVentanaGolpe = ataque.Inicio(2);
                EditorUtility.SetDirty(datos);
            }

            return true;
        }

        // --- Prefab ---

        private static GameObject CrearPrefab(Sprite reposo, AnimatorController controlador)
        {
            var datos = GeneradorEnemigos.CargarDatos(NombreDatos);
            var destello = AssetDatabase.LoadAssetAtPath<Material>(ConstructorEcoIra.RutaMaterialDestello);
            if (destello == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {ConstructorEcoIra.RutaMaterialDestello}: el Golem no destellará al recibir golpes.");
            }

            var raiz = new GameObject(NombreDatos) { layer = Capa(Capas.Enemy), tag = GameTags.Enemy };
            try
            {
                var cuerpoRigido = raiz.AddComponent<Rigidbody2D>();
                cuerpoRigido.gravityScale = 3f;
                cuerpoRigido.freezeRotation = true;
                cuerpoRigido.interpolation = RigidbodyInterpolation2D.Interpolate;
                cuerpoRigido.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var cuerpo = raiz.AddComponent<CapsuleCollider2D>();
                cuerpo.size = TamanoCuerpo;
                cuerpo.offset = new Vector2(0f, TamanoCuerpo.y / 2f);

                raiz.AddComponent<Health>();
                var enemigo = raiz.AddComponent<EnemyController>();

                // Lo que se voltea al girar: el sprite y la onda del golpe.
                var pivote = new GameObject("Pivote").transform;
                pivote.SetParent(raiz.transform, false);

                var objetoSprite = new GameObject("Sprite") { layer = raiz.layer };
                objetoSprite.transform.SetParent(pivote, false);
                var sprite = objetoSprite.AddComponent<SpriteRenderer>();
                sprite.sprite = reposo;
                sprite.sortingOrder = 5;
                sprite.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ConstructorEcoIra.RutaMaterialSprite);

                var animator = objetoSprite.AddComponent<Animator>();
                animator.runtimeAnimatorController = controlador;
                animator.applyRootMotion = false;
                // Fuera de cámara también: si no, los eventos del golpe y de la muerte no llegan.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                var animacion = objetoSprite.AddComponent<EnemigoAnimacion>();
                Asignar(animacion, "velocidadNaturalCarrera", p => p.floatValue = VelocidadNaturalAndar);
                Asignar(animacion, nameof(EnemigoAnimacion.velocidadAtaque), p => p.floatValue = 1f);
                Asignar(animacion, "materialDestello", p => p.objectReferenceValue = destello);
                Asignar(animacion, "colorDestello", p => p.colorValue = Color.white);
                Asignar(animacion, "restosEnPantalla", p => p.floatValue = RestosEnPantalla);

                // Arma: la onda de A3, ancha y baja. Solo golpea a la capa Player, y solo entre los
                // eventos de A3 y A4.
                var objetoArma = new GameObject("Arma") { layer = Capa(Capas.EnemyWeapon) };
                objetoArma.transform.SetParent(pivote, false);
                objetoArma.transform.localPosition = Onda.center;
                var zonaArma = objetoArma.AddComponent<BoxCollider2D>();
                zonaArma.isTrigger = true;
                zonaArma.size = Onda.size;
                var arma = objetoArma.AddComponent<Hitbox>();
                Asignar(arma, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
                objetoArma.SetActive(false);

                // Contacto: tocar el cuerpo hace daño siempre (danioContacto).
                var objetoContacto = new GameObject("Contacto") { layer = Capa(Capas.EnemyWeapon) };
                objetoContacto.transform.SetParent(raiz.transform, false);
                var zonaContacto = objetoContacto.AddComponent<CapsuleCollider2D>();
                zonaContacto.isTrigger = true;
                zonaContacto.size = TamanoContacto;
                zonaContacto.offset = new Vector2(0f, TamanoContacto.y / 2f);
                var contacto = objetoContacto.AddComponent<Hitbox>();
                Asignar(contacto, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
                Asignar(contacto, "golpearUnaVezPorActivacion", p => p.boolValue = false);

                Asignar(enemigo, "datos", p => p.objectReferenceValue = datos);
                Asignar(enemigo, "animator", p => p.objectReferenceValue = animator);
                Asignar(enemigo, "pivote", p => p.objectReferenceValue = pivote);
                Asignar(enemigo, "sprite", p => p.objectReferenceValue = sprite);
                Asignar(enemigo, "arma", p => p.objectReferenceValue = arma);
                Asignar(enemigo, "contacto", p => p.objectReferenceValue = contacto);
                Asignar(enemigo, "animacion", p => p.objectReferenceValue = animacion);

                var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {RutaPrefab}.");
                    return null;
                }

                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        // --- Escenas ---

        /// <summary>
        /// Sustituye cada Golem sin arte (la cápsula de la Fase 4) por el que lo tiene, en la misma
        /// posición y con la misma ruta de patrulla. Devuelve cuántos sustituyó.
        /// </summary>
        public static int SustituirGolemsSinArte(Scene escena)
        {
            var golem = Prefab;
            if (golem == null)
            {
                return 0;
            }

            var viejos = escena.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<EnemyController>(true))
                .Where(e => PrefabUtility.IsOutermostPrefabInstanceRoot(e.gameObject) &&
                            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(e.gameObject) == RutaPrefabSinArte)
                .ToList();

            foreach (var viejo in viejos)
            {
                var nuevo = (GameObject)PrefabUtility.InstantiatePrefab(golem, escena);
                nuevo.transform.SetParent(viejo.transform.parent, false);
                nuevo.transform.SetSiblingIndex(viejo.transform.GetSiblingIndex());
                nuevo.transform.position = viejo.transform.position;

                var nombreViejo = viejo.name;
                nuevo.name = nombreViejo.Replace("Enemigo_" + NombreDatos, NombreDatos);

                // La misma ruta, con los puntos renombrados para que se sepa de quién son.
                var ruta = new SerializedObject(viejo).FindProperty("waypoints");
                var puntos = Enumerable.Range(0, ruta.arraySize)
                    .Select(i => ruta.GetArrayElementAtIndex(i).objectReferenceValue as Transform)
                    .ToArray();
                foreach (var punto in puntos.Where(p => p != null && p.name.StartsWith(nombreViejo)))
                {
                    punto.name = nuevo.name + punto.name.Substring(nombreViejo.Length);
                }

                Asignar(nuevo.GetComponent<EnemyController>(), "waypoints", p =>
                {
                    p.arraySize = puntos.Length;
                    for (var i = 0; i < puntos.Length; i++)
                    {
                        p.GetArrayElementAtIndex(i).objectReferenceValue = puntos[i];
                    }
                });

                Object.DestroyImmediate(viejo.gameObject);
            }

            return viejos.Count;
        }
    }
}
