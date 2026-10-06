using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Levels.Mecanicas;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Prefabs de las mecánicas del mundo de la Ira (Anexo A) en <c>Assets/Prefabs/Mecanicas</c>,
    /// con su arte (<see cref="ConstructorMecanicas"/>): plataforma inestable, plataforma de
    /// combustión, lava, torreta de fuego y la bola de la torreta. Son las piezas que se
    /// arrastran al pintar un nivel, y las que coloca el preparador de escenas.
    ///
    /// - <b>Pivotes donde se pisa:</b> el de cada pieza es el de su arte (<see cref="TablaArte"/>):
    ///   la superficie de las plataformas, la base de la torreta y la costra de la lava. Se colocan
    ///   poniendo el Transform donde el jugador apoya el pie.
    /// - <b>Colliders medidos en el arte</b>, en píxeles nativos (<see cref="Geometria"/>).
    /// - <b>Las zonas de daño, apagadas en reposo:</b> la llama de la torreta solo en el disparo y
    ///   la de la combustión solo encendida. La lava y la bola sí hacen daño siempre.
    /// - Se dibujan detrás de los tiles del suelo, salvo la lava: lo que se hunde en el terreno no
    ///   se ve por delante de él. La costra de la lava, sí.
    ///
    /// Sin el arte, cada pieza lleva un rectángulo de color del mismo tamaño y avisa.
    /// </summary>
    public static class GeneradorMecanicas
    {
        public const string Carpeta = "Assets/Prefabs/Mecanicas";

        public const string PlataformaInestable = "PlataformaInestable";
        public const string PlataformaCombustion = "PlataformaCombustion";
        public const string Lava = "Lava";
        public const string TorretaFuego = "TorretaFuego";
        public const string BolaDeFuego = "BolaDeFuego";

        /// <summary>Orden de dibujo de las piezas: detrás de los tiles del suelo (0) y de su decoración (-1).</summary>
        public const int OrdenDibujo = -2;

        /// <summary>La lava, por delante del suelo: su costra tiene que verse.</summary>
        public const int OrdenLava = 1;

        /// <summary>Radio de la bola de fuego (provisional hasta que llegue su arte).</summary>
        public const float RadioBola = 0.3f;

        [MenuItem("Fragmentum/Niveles/Crear prefabs de mecánicas (Ira)", priority = 133)]
        public static void MenuCrearPrefabs() => CrearPrefabs();

        public static bool CrearPrefabs()
        {
            AsegurarConfiguracion();
            AsegurarCarpeta(Carpeta);

            var ok = Guardar(BolaDeFuego, CrearBola);
            ok &= Guardar(PlataformaInestable, CrearPlataformaInestable);
            ok &= Guardar(PlataformaCombustion, CrearPlataformaCombustion);
            ok &= Guardar(Lava, CrearLava);
            ok &= Guardar(TorretaFuego, CrearTorreta);
            if (ok)
            {
                Debug.Log($"[Fragmentum] Prefabs de mecánicas creados en {Carpeta}.");
            }

            return ok;
        }

        public static GameObject Cargar(string nombre) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Carpeta}/{nombre}.prefab");

        // --- Geometría, en píxeles nativos del arte ---

        /// <summary>
        /// Las medidas del encargo, en píxeles de su lienzo (x desde la izquierda, y desde arriba,
        /// extremos incluidos), pasadas a unidades desde el pivote de su sprite.
        /// </summary>
        public static class Geometria
        {
            /// <summary>Torreta: el bloque sólido, igual en los tres fotogramas (x 16-61, y 1-40).</summary>
            public static Rect BloqueTorreta => Rectangulo("Torreta_1_reposo", 16, 61, 1, 40);

            /// <summary>Torreta: la llama del disparo, hasta x 81 (en el lienzo del disparo, que está corrido 15 px).</summary>
            public static Rect LlamaTorreta => Rectangulo("Torreta_3_disparo", 46, 81, 9, 30);

            /// <summary>Plataforma inestable: de la superficie (fila 21) al fondo del cuerpo, todo el ancho.</summary>
            public static Rect CuerpoPlataforma => Rectangulo("Plataforma_1_intacta", 0, 183, 21, 62);

            /// <summary>Plataforma de combustión: de la superficie (fila 26) a la base (fila 87), x 1-164.</summary>
            public static Rect CuerpoCombustion => Rectangulo("Combustion_1_apagada", 1, 164, 26, 87);

            /// <summary>Plataforma de combustión: la llama encendida, de la superficie hasta la fila 0.</summary>
            public static Rect LlamaCombustion => Rectangulo("Combustion_3_encendida", 1, 164, 0, 25);

            /// <summary>
            /// Un rectángulo de píxeles del lienzo de <paramref name="sprite"/> en unidades desde su
            /// pivote, con los píxeles por unidad y el pivote de <see cref="TablaArte"/>.
            /// </summary>
            public static Rect Rectangulo(string sprite, int x0, int x1, int y0, int y1)
            {
                var ruta = ConstructorMecanicas.RutaSprite(sprite);
                var regla = TablaArte.ReglaDe(ruta);
                var lienzo = Lienzo(sprite);
                var ppu = regla?.PixelesPorUnidad ?? 100f;
                var pivote = regla?.Pivote ?? new Vector2(0.5f, 0.5f);
                var pivoteX = pivote.x * lienzo.x;
                var filaPivote = lienzo.y - pivote.y * lienzo.y;
                return Rect.MinMaxRect(
                    (x0 - pivoteX) / ppu, (filaPivote - (y1 + 1)) / ppu,
                    (x1 + 1 - pivoteX) / ppu, (filaPivote - y0) / ppu);
            }

            // Lienzo de cada grupo (encargo, sección 4).
            private static Vector2 Lienzo(string sprite)
            {
                if (sprite.StartsWith("Torreta_")) return new Vector2(82f, 42f);
                if (sprite.StartsWith("Plataforma_")) return new Vector2(184f, 142f);
                if (sprite.StartsWith("Combustion_")) return new Vector2(166f, 89f);
                return new Vector2(151f, 64f);
            }
        }

        // --- Piezas ---

        private static GameObject CrearBola()
        {
            // Provisional: el círculo del proyecto, en naranja, hasta que llegue la bola (Fireball3).
            var raiz = Raiz(BolaDeFuego, Capa(Capas.EnemyWeapon), null);
            var cuerpo = raiz.AddComponent<Rigidbody2D>();
            cuerpo.bodyType = RigidbodyType2D.Kinematic;
            cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            var zona = raiz.AddComponent<CircleCollider2D>();
            zona.isTrigger = true;
            zona.radius = RadioBola;
            var hitbox = raiz.AddComponent<Hitbox>();
            Asignar(hitbox, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
            raiz.AddComponent<Proyectil>();

            var dibujo = new GameObject("Sprite") { layer = raiz.layer };
            dibujo.transform.SetParent(raiz.transform, false);
            var sprite = dibujo.AddComponent<SpriteRenderer>();
            sprite.sprite = SpriteCirculo;
            sprite.color = new Color(1f, 0.45f, 0.05f);
            sprite.sortingOrder = 8;
            if (sprite.sprite != null)
            {
                var lado = Mathf.Max(sprite.sprite.bounds.size.x, 0.01f);
                dibujo.transform.localScale = Vector3.one * (RadioBola * 2f / lado);
            }

            return raiz;
        }

        private static GameObject CrearPlataformaInestable()
        {
            var raiz = Raiz(PlataformaInestable, Capa(Capas.Suelo), GameTags.Suelo);
            var cuerpo = Geometria.CuerpoPlataforma;
            var caja = raiz.AddComponent<BoxCollider2D>();
            caja.size = cuerpo.size;
            caja.offset = cuerpo.center;

            var (dibujo, animator) = Dibujo(raiz.transform, ConstructorMecanicas.Plataforma, OrdenDibujo, cuerpo, Hex("8A6A55"));
            var componente = raiz.AddComponent<Levels.Mecanicas.PlataformaInestable>();
            Asignar(componente, "animator", p => p.objectReferenceValue = animator);
            Asignar(componente, "visual", p => p.objectReferenceValue = dibujo);
            return raiz;
        }

        private static GameObject CrearPlataformaCombustion()
        {
            var raiz = Raiz(PlataformaCombustion, Capa(Capas.Suelo), GameTags.HotPlatform);
            var cuerpo = Geometria.CuerpoCombustion;
            var caja = raiz.AddComponent<BoxCollider2D>();
            caja.size = cuerpo.size;
            caja.offset = cuerpo.center;

            var (_, animator) = Dibujo(raiz.transform, ConstructorMecanicas.Combustion, OrdenDibujo, cuerpo, Hex("5C4033"));

            // La llama baja un poco dentro de la rejilla: así toca los pies del jugador que está encima.
            var llamaArte = Geometria.LlamaCombustion;
            var llama = Llama(raiz.transform, Rect.MinMaxRect(llamaArte.xMin, llamaArte.yMin - 0.1f, llamaArte.xMax, llamaArte.yMax), false);

            var componente = raiz.AddComponent<Levels.Mecanicas.PlataformaCombustion>();
            Asignar(componente, "animator", p => p.objectReferenceValue = animator);
            Asignar(componente, "llama", p => p.objectReferenceValue = llama);
            return raiz;
        }

        private static GameObject CrearLava()
        {
            var raiz = Raiz(Lava, 0, GameTags.Lava);
            var franja = CargarSprite(ConstructorMecanicas.RutaSprite(ConstructorMecanicas.FotogramasLava[0]));
            var espejo = CargarSprite(ConstructorMecanicas.RutaSprite(ConstructorMecanicas.FotogramasLava[1]));
            var tamano = franja != null ? (Vector2)franja.bounds.size : new Vector2(3f, 1f);

            var zona = raiz.AddComponent<BoxCollider2D>();
            zona.isTrigger = true;
            zona.size = tamano;
            zona.offset = new Vector2(0f, -tamano.y / 2f);

            var componente = raiz.AddComponent<Levels.Mecanicas.Lava>();
            Asignar(componente, "franja", p => p.objectReferenceValue = franja);
            Asignar(componente, "franjaEspejo", p => p.objectReferenceValue = espejo);
            Asignar(componente, "ordenDibujo", p => p.intValue = OrdenLava);
            if (franja == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta el arte de la lava ({ConstructorMecanicas.RutaSprite("Lava")}): el prefab no se verá.");
            }

            return raiz;
        }

        private static GameObject CrearTorreta()
        {
            // El bloque es suelo (se puede pisar); dispara hacia donde mira el dibujo, a la derecha.
            var raiz = Raiz(TorretaFuego, Capa(Capas.Suelo), GameTags.Suelo);
            var bloque = Geometria.BloqueTorreta;
            var caja = raiz.AddComponent<BoxCollider2D>();
            caja.size = bloque.size;
            caja.offset = bloque.center;

            var (_, animator) = Dibujo(raiz.transform, ConstructorMecanicas.Torreta, OrdenDibujo, bloque, Hex("3A1A10"));
            var llamaArte = Geometria.LlamaTorreta;
            var llama = Llama(raiz.transform, llamaArte, true);

            // La bola sale delante del bloque (dentro de él la frenaría el propio bloque) y a la
            // altura de la llama.
            var boca = new GameObject("Boca").transform;
            boca.SetParent(raiz.transform, false);
            boca.localPosition = new Vector3(bloque.xMax + RadioBola + 0.05f, llamaArte.center.y, 0f);

            var componente = raiz.AddComponent<Levels.Mecanicas.TorretaFuego>();
            Asignar(componente, "animator", p => p.objectReferenceValue = animator);
            Asignar(componente, "llama", p => p.objectReferenceValue = llama);
            Asignar(componente, "boca", p => p.objectReferenceValue = boca);
            Asignar(componente, "bola", p => p.objectReferenceValue = Cargar(BolaDeFuego)?.GetComponent<Proyectil>());
            return raiz;
        }

        // --- Utilidades ---

        private static GameObject Raiz(string nombre, int capa, string tag)
        {
            var raiz = new GameObject(nombre) { layer = capa };
            if (!string.IsNullOrEmpty(tag))
            {
                raiz.tag = tag;
            }

            return raiz;
        }

        /// <summary>
        /// El dibujo de una mecánica: su primer fotograma y su Animator, en el pivote. Sin arte,
        /// un rectángulo de color sobre <paramref name="provisional"/> que avisa de lo que falta.
        /// </summary>
        private static (SpriteRenderer, Animator) Dibujo(Transform padre, ConstructorMecanicas.Mecanica mecanica, int orden, Rect provisional, Color color)
        {
            var objeto = new GameObject("Sprite") { layer = padre.gameObject.layer };
            objeto.transform.SetParent(padre, false);
            var dibujo = objeto.AddComponent<SpriteRenderer>();
            dibujo.sortingOrder = orden;

            var primero = CargarSprite(ConstructorMecanicas.RutaSprite(mecanica.Clips[0].Fotogramas[0]));
            var controlador = mecanica.Controlador;
            if (primero == null || controlador == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta el arte de {mecanica.Nombre}: el prefab lleva un rectángulo provisional. " +
                                 "Ejecuta Fragmentum → Niveles → Generar los clips y los Animators del Golem y de las mecánicas.");
                objeto.transform.localPosition = provisional.center;
                objeto.transform.localScale = new Vector3(provisional.width, provisional.height, 1f);
                dibujo.sprite = SpriteCuadrado;
                dibujo.color = color;
                return (dibujo, null);
            }

            dibujo.sprite = primero;
            var animator = objeto.AddComponent<Animator>();
            animator.runtimeAnimatorController = controlador;
            animator.applyRootMotion = false;
            // Fuera de cámara también: al volver a verse, el dibujo está en el estado del script.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return (dibujo, animator);
        }

        /// <summary>Una llama: hitbox trigger en la capa EnemyWeapon que solo golpea al jugador. Apagada en reposo.</summary>
        private static Hitbox Llama(Transform padre, Rect zona, bool unGolpePorActivacion)
        {
            var objeto = new GameObject("Llama") { layer = Capa(Capas.EnemyWeapon) };
            objeto.transform.SetParent(padre, false);
            var caja = objeto.AddComponent<BoxCollider2D>();
            caja.isTrigger = true;
            caja.size = zona.size;
            caja.offset = zona.center;
            var hitbox = objeto.AddComponent<Hitbox>();
            Asignar(hitbox, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
            Asignar(hitbox, "golpearUnaVezPorActivacion", p => p.boolValue = unGolpePorActivacion);
            objeto.SetActive(false);
            return hitbox;
        }

        private static bool Guardar(string nombre, Func<GameObject> crear)
        {
            var raiz = crear();
            try
            {
                PrefabUtility.SaveAsPrefabAsset(raiz, $"{Carpeta}/{nombre}.prefab", out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar el prefab {nombre}.");
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
