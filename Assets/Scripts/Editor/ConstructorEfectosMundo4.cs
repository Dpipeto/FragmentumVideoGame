using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Levels.Mecanicas;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;
using Object = UnityEngine.Object;
using Transicion = Fragmentum.EditorTools.ControladoresAnimator.Transicion;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Lo que lanzan y lo que dejan los del mundo de la Culpa (<c>Prompts_Mundo4.md</c>, apartado C), con
    /// su arte. Las imágenes vienen de <c>Tesis/Imagenes/limpios/Mundo4</c> (con su LEEME y su
    /// <c>anclas.json</c>) y no se retocan:
    ///
    /// - <b>La explosión</b> (cuatro fotogramas: el estallido, la bola de fuego, el humo y el cráter) y
    ///   <b>su marca</b> (dos: la tierra quemada con el anillo de grietas que avisa). Una sola explosión
    ///   para todo el mundo: la del barril y la de la mina (<see cref="PrefabExplosion"/>, que se escala
    ///   a su alcance) y la artillería de «La orden» (<see cref="PrefabArtilleria"/>: avisa con la marca
    ///   mientras su zona avisa y estalla al quemar).
    /// - <b>La ola de fuego</b> de «El pueblo» (<see cref="PrefabOla"/>), en bucle.
    /// - <b>La onda</b> de «La sentencia» (<see cref="PrefabOnda"/>, apartado K2, 2026-10-23): la tierra que se
    ///   levanta y rueda por el suelo, en bucle, como la ola.
    /// - <b>La placa</b> que vuela en «El compañero» (<see cref="PrefabPlaca"/>), girando.
    /// - <b>La bala</b> del Artillero (<see cref="PrefabBala"/>), un trazador.
    /// - <b>El garfio y la cadena</b> del Verdugo y de la trampa del Juez: solo sus ajustes; los ponen
    ///   <see cref="ConstructorVerdugo"/> y <see cref="ConstructorJuez"/>.
    ///
    /// <b>El píxel.</b> La placa salió con el del Juez; las demás, con uno el doble de grande, todas
    /// iguales entre sí. Cada pieza lleva sus píxeles por unidad: los que dan el tamaño de su juego.
    ///
    /// Sin el arte de una pieza avisa y no la crea: se queda con su dibujo provisional. Idempotente.
    /// </summary>
    public static class ConstructorEfectosMundo4
    {
        public const string Carpeta = TablaArte.CarpetaEfectos + "/Mundo4";
        public const string CarpetaExplosion = Carpeta + "/Explosion";
        public const string CarpetaFuego = Carpeta + "/Fuego";
        public const string CarpetaPlaca = Carpeta + "/Placa";
        public const string CarpetaBala = Carpeta + "/Bala";
        public const string CarpetaCadena = Carpeta + "/Cadena";
        public const string CarpetaOnda = Carpeta + "/Onda";
        public const string CarpetaAnimaciones = ConstructorEfectos.CarpetaAnimaciones + "/Mundo4";

        public static string RutaExplosion(string archivo) => $"{CarpetaExplosion}/{archivo}.png";
        public static string RutaFuego(string archivo) => $"{CarpetaFuego}/{archivo}.png";
        public static string RutaPlaca(string archivo) => $"{CarpetaPlaca}/{archivo}.png";
        public static string RutaBala(string archivo) => $"{CarpetaBala}/{archivo}.png";
        public static string RutaCadena(string archivo) => $"{CarpetaCadena}/{archivo}.png";
        public static string RutaOnda(string archivo) => $"{CarpetaOnda}/{archivo}.png";

        // --- Lienzos y escalas ---

        public static readonly Vector2Int LienzoExplosion = new Vector2Int(95, 91);
        public static readonly Vector2Int LienzoMarca = new Vector2Int(83, 24);
        public static readonly Vector2Int LienzoOla = new Vector2Int(82, 52);
        public static readonly Vector2Int LienzoPlaca = new Vector2Int(25, 37);
        public static readonly Vector2Int LienzoBala = new Vector2Int(35, 6);
        public static readonly Vector2Int LienzoGarfio = new Vector2Int(14, 32);
        public static readonly Vector2Int LienzoOnda = new Vector2Int(94, 50);
        public static readonly Vector2Int LienzoCadena = new Vector2Int(74, 7);

        /// <summary>
        /// La bola de fuego (Explosion2) ocupa los 95 píxeles del lienzo: a 19 por unidad, 5 u, lo que
        /// alcanza un barril (2,5 de radio). La marca, en la misma rejilla, mide 4,4 u.
        /// </summary>
        public const float PixelesPorUnidadExplosion = 19f;

        /// <summary>El alcance que cubre la explosión a escala 1: el del barril.</summary>
        public const float RadioExplosion = 2.5f;

        /// <summary>
        /// La artillería de «La orden» cae en zonas de 1,8 u: a esta escala la marca mide 2 u y la bola
        /// de fuego, 2,25. Se ve un poco más ancha que el daño, como una explosión.
        /// </summary>
        public const float EscalaArtilleria = 0.45f;

        /// <summary>
        /// Lo que miden de alto las llamas de la ola (filas 11 a 50 de su lienzo; el humo negro de detrás
        /// sube más en Ola2): a <see cref="PixelesPorUnidadOla"/>, 1,2 u, lo que su zona de daño.
        /// </summary>
        public const int AltoLlamas = 40;

        public const float PixelesPorUnidadOla = 33f;

        /// <summary>La placa, con el píxel del Juez (<see cref="ConstructorJuez.PixelesPorUnidad"/>): unas 0,45 × 0,6 u.</summary>
        public const float PixelesPorUnidadPlaca = ConstructorJuez.PixelesPorUnidad;

        /// <summary>
        /// La onda de «La sentencia»: lo que se levanta sobre el suelo (filas 3 a 41 de su lienzo, 39 píxeles) mide, a
        /// 48 por unidad, 0,8 u, lo que su zona de daño. Por debajo lleva una franja de tierra con grietas rojas (filas
        /// 42 a 48), que va metida en el suelo.
        /// </summary>
        public const float PixelesPorUnidadOnda = 48f;

        /// <summary>La fila del suelo de la onda: la primera de su franja de tierra.</summary>
        public const int SueloOnda = 42;

        /// <summary>El trazador mide 35 píxeles: a 64 por unidad (la densidad de Kael), 0,55 u.</summary>
        public const float PixelesPorUnidadBala = 64f;

        /// <summary>El garfio y la cadena, con el píxel del Verdugo (<see cref="ConstructorVerdugo.PixelesPorUnidad"/>).</summary>
        public const float PixelesPorUnidadCadena = ConstructorVerdugo.PixelesPorUnidad;

        // El pivote de cada pieza, normalizado desde abajo a la izquierda.
        // La explosión y la marca, en el suelo en el centro: donde estalla.
        public static readonly Vector2 PivoteExplosion = new Vector2(0.5f, 0f);
        // La ola, en su frente (la columna 0, donde se inclinan las llamas) sobre el suelo (la fila 50).
        public static readonly Vector2 PivoteOla = new Vector2(0f, 1f / LienzoOla.y);
        // La onda, como la ola: en su frente (la columna 1, donde empieza la tierra) y en la fila del suelo.
        public static readonly Vector2 PivoteOnda = new Vector2(1f / LienzoOnda.x, (float)(LienzoOnda.y - SueloOnda) / LienzoOnda.y);
        // La bala, en la punta del trazador (el núcleo, x 1-11): la estela queda detrás.
        public static readonly Vector2 PivoteBala = new Vector2(6f / LienzoBala.x, 0.5f);
        // El garfio, en la curva del gancho (y 25 de 32): los eslabones quedan detrás.
        public static readonly Vector2 PivoteGarfio = new Vector2(0.5f, 7f / LienzoGarfio.y);

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(CarpetaExplosion + "/Explosion", PixelesPorUnidadExplosion, PivoteExplosion,
                "Explosión del mundo de la Culpa: 95×91, el pivote en el suelo en el centro. A 19 px/u, la bola de fuego mide 5 u " +
                "(el alcance de un barril); la mina y la artillería del Juez la escalan.", LienzoExplosion),
            new TablaArte.Regla(CarpetaExplosion + "/Marca", PixelesPorUnidadExplosion, PivoteExplosion,
                "La marca que avisa de la artillería: 83×24, en la rejilla de la explosión; el pivote en el suelo en el centro.", LienzoMarca),
            new TablaArte.Regla(CarpetaFuego + "/", PixelesPorUnidadOla, PivoteOla,
                "La ola de fuego de «El pueblo»: 82×52, dibujada avanzando a la izquierda; el pivote en su frente sobre el suelo. " +
                "A 33 px/u, las llamas miden 1,2 u.", LienzoOla),
            new TablaArte.Regla(CarpetaOnda + "/", PixelesPorUnidadOnda, PivoteOnda,
                "La onda de «La sentencia»: 94×50, dibujada avanzando a la izquierda; el pivote en su frente, en la fila del suelo " +
                "(42: la franja de tierra de debajo va metida en el suelo). A 48 px/u se levanta 0,8 u.", LienzoOnda),
            new TablaArte.Regla(CarpetaPlaca + "/", PixelesPorUnidadPlaca, null,
                "La placa que vuela: 25×37, con el píxel del Juez; el pivote en el centro.", LienzoPlaca),
            new TablaArte.Regla(CarpetaBala + "/", PixelesPorUnidadBala, PivoteBala,
                "La bala del Artillero: 35×6, dibujada volando a la izquierda; el pivote en la punta. 0,55 u.", LienzoBala),
            new TablaArte.Regla(CarpetaCadena + "/Garfio", PixelesPorUnidadCadena, PivoteGarfio,
                "El garfio: 14×32, colgando (los eslabones arriba); el pivote en la curva del gancho. Con el píxel del Verdugo.", LienzoGarfio),
            new TablaArte.Regla(CarpetaCadena + "/Cadena", PixelesPorUnidadCadena, null,
                "Un tramo de cadena: 74×7, que se repite a lo largo (dibujo en mosaico); el pivote en el centro.", LienzoCadena),
        };

        // --- Clips ---

        public const string Aviso = "Aviso";
        public const string Brota = "Brota";
        public const string Arde = "Arde";
        public const string Apaga = "Apaga";
        public const string Estalla = "Estalla";

        /// <summary>La explosión entera, una vez: el estallido, la bola de fuego, el humo y el cráter.</summary>
        public static readonly Clip ExplosionEntera = new Clip
        {
            Nombre = "Explosion_Entera", Estado = Estalla, Fotogramas = new[] { "Explosion1", "Explosion2", "Explosion3", "Explosion4" },
            Duraciones = new[] { 0.08f, 0.2f, 0.25f, ClipsDeSprites.SeQueda },
        };

        /// <summary>Lo que se ve la explosión suelta: hasta el cráter, y un momento más.</summary>
        public const float DuracionExplosion = 0.08f + 0.2f + 0.25f + 0.3f;

        /// <summary>La artillería, mientras su zona avisa: la marca, que late.</summary>
        public static readonly Clip ArtilleriaAviso = new Clip
        {
            Nombre = "Artilleria_Aviso", Estado = Aviso, Fotogramas = new[] { "Marca1", "Marca2" }, Duraciones = new[] { 0.1f, 0.1f }, Bucle = true,
        };

        public static readonly Clip ArtilleriaBrota = new Clip
        {
            Nombre = "Artilleria_Brota", Estado = Brota, Fotogramas = new[] { "Explosion1" }, Duraciones = new[] { 0.08f },
        };

        public static readonly Clip ArtilleriaArde = new Clip
        {
            Nombre = "Artilleria_Arde", Estado = Arde, Fotogramas = new[] { "Explosion2" }, Duraciones = new[] { ClipsDeSprites.SeQueda },
        };

        /// <summary>Al acabar la zona, ya sin daño: el humo y el cráter.</summary>
        public static readonly Clip ArtilleriaApaga = new Clip
        {
            Nombre = "Artilleria_Apaga", Estado = Apaga, Fotogramas = new[] { "Explosion3", "Explosion4" }, Duraciones = new[] { 0.2f, 0.25f },
        };

        public static readonly Clip[] ClipsArtilleria = { ArtilleriaAviso, ArtilleriaBrota, ArtilleriaArde, ArtilleriaApaga };

        public static readonly string[] FotogramasOla = { "Ola1", "Ola2" };
        public static readonly string[] FotogramasPlaca = { "Placa1", "Placa2" };
        public static readonly string[] FotogramasOnda = { "Onda1", "Onda2" };

        /// <summary>Segundos de cada fotograma de la ola y de la placa, en bucle.</summary>
        public const float RitmoOla = 0.1f;
        public const float RitmoPlaca = 0.08f;

        // --- Prefabs ---

        public const string NombreExplosion = "ExplosionDeLaCulpa";
        public const string NombreArtilleria = "ArtilleriaDeLaOrden";
        public const string NombreOla = "OlaDelPueblo";
        public const string NombrePlaca = "PlacaDelJuez";
        public const string NombreBala = "BalaDelArtillero";
        public const string NombreOnda = "OndaDeLaSentencia";

        public static string RutaPrefab(string nombre) => $"{ConstructorEfectos.CarpetaPrefabs}/{nombre}.prefab";

        public static ExplosionDibujada PrefabExplosion => Cargar(NombreExplosion)?.GetComponent<ExplosionDibujada>();
        public static GameObject PrefabArtilleria => Cargar(NombreArtilleria);
        public static GameObject PrefabOla => Cargar(NombreOla);
        public static GameObject PrefabOnda => Cargar(NombreOnda);
        public static Proyectil PrefabPlaca => Cargar(NombrePlaca)?.GetComponent<Proyectil>();
        public static Proyectil PrefabBala => Cargar(NombreBala)?.GetComponent<Proyectil>();

        public static Sprite Garfio => AssetDatabase.LoadAssetAtPath<Sprite>(RutaCadena("Garfio"));
        public static Sprite Cadena => AssetDatabase.LoadAssetAtPath<Sprite>(RutaCadena("Cadena"));

        private static GameObject Cargar(string nombre) => AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(nombre));

        /// <summary>Devuelve false solo si algo falló; si falta el arte de una pieza avisa y sigue.</summary>
        public static bool Construir()
        {
            AsegurarCarpeta(CarpetaAnimaciones);
            AsegurarCarpeta(ConstructorEfectos.CarpetaPrefabs);
            var ok = ConstruirExplosion();
            ok &= ConstruirArtilleria();
            ok &= ConstruirOla();
            ok &= ConstruirOnda();
            ok &= ConstruirPlaca();
            ok &= ConstruirBala();
            ok &= ClipsDeSprites.CargarSprites(new[] { new Clip { Fotogramas = new[] { "Garfio", "Cadena" }, Duraciones = new[] { 1f, 1f } } },
                RutaCadena, "la cadena del Verdugo", out _);
            return ok;
        }

        private static bool ConstruirExplosion()
        {
            if (!ClipsDeSprites.CargarSprites(new[] { ExplosionEntera }, RutaExplosion, "la explosión", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            var clip = ClipsDeSprites.Crear(CarpetaAnimaciones, ExplosionEntera, sprites);
            var controlador = ControladoresAnimator.CargarOCrear($"{CarpetaAnimaciones}/Explosion.controller");
            ControladoresAnimator.AsegurarParametros(controlador);
            var estados = ControladoresAnimator.Estados(controlador.layers[0].stateMachine, new[] { (Estalla, (Motion)clip) });
            ControladoresAnimator.Transiciones(estados[Estalla]);
            EditorUtility.SetDirty(controlador);
            AssetDatabase.SaveAssets();

            return Guardar(NombreExplosion, raiz =>
            {
                Dibujo(raiz, sprites[ExplosionEntera.Fotogramas[0]], controlador);
                var explosion = raiz.AddComponent<ExplosionDibujada>();
                explosion.radio = RadioExplosion;
                explosion.duracion = DuracionExplosion;
            });
        }

        /// <summary>
        /// La marca late mientras la zona avisa; al quemar, estalla (Explosion1) y se queda en la bola de
        /// fuego mientras quema; cuando la zona acaba, el humo y el cráter, ya sin daño (<see cref="DibujoDeZona"/>).
        /// </summary>
        private static bool ConstruirArtilleria()
        {
            if (!ClipsDeSprites.CargarSprites(ClipsArtilleria, RutaExplosion, "la artillería de «La orden»", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            var clips = ClipsArtilleria.Select(c => (c.Estado, (Motion)ClipsDeSprites.Crear(CarpetaAnimaciones, c, sprites))).ToList();
            var controlador = ControladoresAnimator.CargarOCrear($"{CarpetaAnimaciones}/Artilleria.controller");
            ControladoresAnimator.AsegurarParametros(controlador,
                (AnimatorParams.Quema.Nombre, AnimatorControllerParameterType.Bool, 0f),
                (AnimatorParams.Apagada.Nombre, AnimatorControllerParameterType.Bool, 0f));
            var e = ControladoresAnimator.Estados(controlador.layers[0].stateMachine, clips);
            var apagada = ControladoresAnimator.Condicion(AnimatorConditionMode.If, AnimatorParams.Apagada.Nombre);
            var quema = ControladoresAnimator.Condicion(AnimatorConditionMode.If, AnimatorParams.Quema.Nombre);
            ControladoresAnimator.Transiciones(e[Aviso],
                new Transicion("Apagada", e[Apaga], null, apagada),
                new Transicion("Quema", e[Brota], null, quema));
            ControladoresAnimator.Transiciones(e[Brota],
                new Transicion("Apagada", e[Apaga], null, apagada),
                new Transicion("Llena", e[Arde], 1f));
            ControladoresAnimator.Transiciones(e[Arde], new Transicion("Apagada", e[Apaga], null, apagada));
            ControladoresAnimator.Transiciones(e[Apaga]);
            EditorUtility.SetDirty(controlador);
            AssetDatabase.SaveAssets();

            return Guardar(NombreArtilleria, raiz =>
            {
                raiz.transform.localScale = Vector3.one * EscalaArtilleria;
                Dibujo(raiz, sprites[ArtilleriaAviso.Fotogramas[0]], controlador);
                raiz.AddComponent<DibujoDeZona>().duracionApagado = ArtilleriaApaga.Duracion;
            });
        }

        // La ola, dibujada avanzando a la izquierda, se voltea: mira a la derecha y la zona la espeja.
        private static bool ConstruirOla()
        {
            if (!CargarSueltos(FotogramasOla, RutaFuego, "la ola de fuego", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            return Guardar(NombreOla, raiz =>
            {
                var dibujo = raiz.AddComponent<SpriteRenderer>();
                dibujo.sprite = sprites[0];
                dibujo.flipX = true;
                dibujo.sortingOrder = ConstructorEfectos.OrdenDibujo;
                var bucle = raiz.AddComponent<BucleDeSprites>();
                bucle.Fijar(sprites, RitmoOla);
            });
        }

        // La onda, como la ola: dibujada avanzando a la izquierda, se voltea y la zona la espeja.
        private static bool ConstruirOnda()
        {
            if (!CargarSueltos(FotogramasOnda, RutaOnda, "la onda de la sentencia", out var sprites))
            {
                return false;
            }

            if (sprites == null)
            {
                return true;
            }

            return Guardar(NombreOnda, raiz =>
            {
                var dibujo = raiz.AddComponent<SpriteRenderer>();
                dibujo.sprite = sprites[0];
                dibujo.flipX = true;
                dibujo.sortingOrder = ConstructorEfectos.OrdenDibujo;
                var bucle = raiz.AddComponent<BucleDeSprites>();
                bucle.Fijar(sprites, RitmoOla);
            });
        }

        // Como la bola de fuego de la torreta: un proyectil con su dibujo, que gira en bucle.
        private static bool ConstruirPlaca()
        {
            if (!CargarSueltos(FotogramasPlaca, RutaPlaca, "la placa del Juez", out var sprites))
            {
                return false;
            }

            return sprites == null || Guardar(NombrePlaca, raiz => ArmarProyectil(raiz, 0.15f, sprites, RitmoPlaca, false));
        }

        // El trazador, dibujado volando a la izquierda: volteado, vuela a la derecha y el proyectil lo espeja.
        private static bool ConstruirBala()
        {
            if (!CargarSueltos(new[] { "Bala1" }, RutaBala, "la bala del Artillero", out var sprites))
            {
                return false;
            }

            return sprites == null || Guardar(NombreBala, raiz => ArmarProyectil(raiz, 0.1f, sprites, 0f, true));
        }

        private static void ArmarProyectil(GameObject raiz, float radio, Sprite[] sprites, float ritmo, bool voltear)
        {
            raiz.layer = Capa(Capas.EnemyWeapon);
            var cuerpo = raiz.AddComponent<Rigidbody2D>();
            cuerpo.bodyType = RigidbodyType2D.Kinematic;
            cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            var zona = raiz.AddComponent<CircleCollider2D>();
            zona.isTrigger = true;
            zona.radius = radio;
            var hitbox = raiz.AddComponent<Hitbox>();
            Asignar(hitbox, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
            raiz.AddComponent<Proyectil>();

            var objeto = new GameObject("Sprite") { layer = raiz.layer };
            objeto.transform.SetParent(raiz.transform, false);
            var dibujo = objeto.AddComponent<SpriteRenderer>();
            dibujo.sprite = sprites[0];
            dibujo.flipX = voltear;
            dibujo.sortingOrder = 8;
            if (sprites.Length > 1)
            {
                objeto.AddComponent<BucleDeSprites>().Fijar(sprites, ritmo);
            }
        }

        private static void Dibujo(GameObject raiz, Sprite primero, RuntimeAnimatorController controlador)
        {
            var dibujo = raiz.AddComponent<SpriteRenderer>();
            dibujo.sprite = primero;
            dibujo.sortingOrder = ConstructorEfectos.OrdenDibujo;
            var animator = raiz.AddComponent<Animator>();
            animator.runtimeAnimatorController = controlador;
            animator.applyRootMotion = false;
        }

        // Sprites sueltos (sin clip). False si no se pudieron importar; si falta alguno, true y sprites null.
        private static bool CargarSueltos(string[] fotogramas, System.Func<string, string> ruta, string quien, out Sprite[] sprites)
        {
            sprites = null;
            var clip = new Clip { Fotogramas = fotogramas, Duraciones = fotogramas.Select(_ => 1f).ToArray() };
            if (!ClipsDeSprites.CargarSprites(new[] { clip }, ruta, quien, out var cargados))
            {
                return false;
            }

            if (cargados != null)
            {
                sprites = fotogramas.Select(f => cargados[f]).ToArray();
            }

            return true;
        }

        private static bool Guardar(string nombre, System.Action<GameObject> armar)
        {
            var ruta = RutaPrefab(nombre);
            var raiz = new GameObject(nombre);
            try
            {
                armar(raiz);
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {ruta}.");
                }
                else
                {
                    Debug.Log($"[Fragmentum] Efecto {nombre}: {ruta}.");
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
