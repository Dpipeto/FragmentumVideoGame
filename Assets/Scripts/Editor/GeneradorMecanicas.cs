using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Levels.Mecanicas;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Prefabs de las mecánicas del mundo de la Ira (Anexo A) en <c>Assets/Prefabs/Mecanicas</c>,
    /// con su arte (<see cref="ConstructorMecanicas"/>): plataforma inestable, plataforma de
    /// combustión, lava, torreta de fuego y la bola de la torreta. Las del mundo de la Tristeza: la
    /// placa de hielo flotante y el hielo rompible. Y las del mundo del Miedo, todavía sin arte: la
    /// antorcha, el cepo y la rama podrida. Son las piezas que se arrastran al pintar un nivel, y las
    /// que coloca el preparador de escenas.
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

        // Las del mundo de la Tristeza (todavía sin arte).
        public const string PlataformaFlotante = "PlataformaFlotante";
        public const string HieloRompible = "HieloRompible";

        /// <summary>La placa flotante: 2,5 u de ancho y 0,5 de grueso, con el pivote en la superficie.</summary>
        public static readonly Vector2 TamanoPlaca = new Vector2(2.5f, 0.5f);

        /// <summary>El tapón de hielo rompible: tapa un hueco de 2 u y asoma 1,25 u sobre el suelo (pivote arriba).</summary>
        public static readonly Vector2 TamanoTapon = new Vector2(2f, 2.25f);

        // Las del mundo del Miedo (todavía sin arte).
        public const string Antorcha = "Antorcha";
        public const string Cepo = "Cepo";
        public const string RamaPodrida = "RamaPodrida";

        /// <summary>La rama podrida: 2 u de largo y el grueso de las ramas, con el pivote en la superficie.</summary>
        public static readonly Vector2 TamanoRama = new Vector2(2f, DisenoNivel3.GrosorRama);

        /// <summary>El cepo: 0,8 u de ancho y 0,3 de alto, con el pivote en el centro de su base (en el suelo).</summary>
        public static readonly Vector2 TamanoCepo = new Vector2(0.8f, 0.3f);

        /// <summary>La antorcha: un poste con la llama a esta altura sobre su pie (el pivote, en el suelo).</summary>
        public const float AlturaLlama = 1.5f;

        /// <summary>Las ramas sin arte: el marrón de las sanas, también el de las podridas a oscuras.</summary>
        public static readonly Color ColorRamaSinArte = new Color(0.43f, 0.33f, 0.22f, 1f);

        /// <summary>La luz de las antorchas: lo único cálido del mundo del Miedo.</summary>
        public static readonly Color ColorLuzAntorcha = new Color(1f, 0.7f, 0.38f, 1f);

        /// <summary>Orden de dibujo de las piezas: detrás de los tiles del suelo (0) y de su decoración (-1).</summary>
        public const int OrdenDibujo = -2;

        /// <summary>La lava, por delante del suelo: su costra tiene que verse.</summary>
        public const int OrdenLava = 1;

        /// <summary>Radio de la bola de fuego (provisional hasta que llegue su arte).</summary>
        public const float RadioBola = 0.3f;

        [MenuItem("Fragmentum/Niveles/Crear prefabs de mecánicas", priority = 133)]
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
            ok &= Guardar(PlataformaFlotante, CrearPlataformaFlotante);
            ok &= Guardar(HieloRompible, CrearHieloRompible);
            ok &= Guardar(Antorcha, CrearAntorcha);
            ok &= Guardar(Cepo, CrearCepo);
            ok &= Guardar(RamaPodrida, CrearRamaPodrida);
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
            /// pivote, con el lienzo, los píxeles por unidad y el pivote de <see cref="TablaArte"/>.
            /// </summary>
            public static Rect Rectangulo(string sprite, int x0, int x1, int y0, int y1)
            {
                var ruta = ConstructorMecanicas.RutaSprite(sprite);
                var regla = TablaArte.ReglaDe(ruta);
                var lienzo = (Vector2)(regla?.Lienzo ?? Vector2Int.zero);
                var ppu = regla?.PixelesPorUnidad ?? 100f;
                var pivote = regla?.Pivote ?? new Vector2(0.5f, 0.5f);
                var pivoteX = pivote.x * lienzo.x;
                var filaPivote = lienzo.y - pivote.y * lienzo.y;
                return Rect.MinMaxRect(
                    (x0 - pivoteX) / ppu, (filaPivote - (y1 + 1)) / ppu,
                    (x1 + 1 - pivoteX) / ppu, (filaPivote - y0) / ppu);
            }
        }

        // --- Piezas ---

        private static GameObject CrearBola()
        {
            var raiz = Raiz(BolaDeFuego, Capa(Capas.EnemyWeapon), null);
            var cuerpo = raiz.AddComponent<Rigidbody2D>();
            cuerpo.bodyType = RigidbodyType2D.Kinematic;
            cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            var zona = raiz.AddComponent<CircleCollider2D>();
            zona.isTrigger = true;
            zona.radius = RadioBola;
            var hitbox = raiz.AddComponent<Hitbox>();
            Asignar(hitbox, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
            var proyectil = raiz.AddComponent<Proyectil>();

            var dibujo = new GameObject("Sprite") { layer = raiz.layer };
            dibujo.transform.SetParent(raiz.transform, false);
            var sprite = dibujo.AddComponent<SpriteRenderer>();
            sprite.sortingOrder = 8;

            // Con su arte: vuela en bucle (dos fotogramas) y al apagarse deja sus chispas.
            var vuelo = CargarSprite(ConstructorMecanicas.RutaSprite(ConstructorMecanicas.BolaVuelo.Fotogramas[0]));
            var controlador = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ConstructorMecanicas.RutaControladorBola);
            if (vuelo != null && controlador != null)
            {
                sprite.sprite = vuelo;
                var animator = dibujo.AddComponent<Animator>();
                animator.runtimeAnimatorController = controlador;
                animator.applyRootMotion = false;
                Asignar(proyectil, "restos", p => p.objectReferenceValue = CargarSprite(ConstructorMecanicas.RutaSprite(ConstructorMecanicas.FotogramaBolaDisipa)));
                Asignar(proyectil, "duracionRestos", p => p.floatValue = ConstructorMecanicas.DuracionBolaDisipa);
                return raiz;
            }

            // Sin arte: el círculo del proyecto, en naranja.
            Debug.LogWarning("[Fragmentum] Falta el arte de la bola de fuego: el prefab lleva un círculo provisional.");
            sprite.sprite = SpriteCirculo;
            sprite.color = new Color(1f, 0.45f, 0.05f);
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

        // --- Mundo de la Tristeza (sin arte: un rectángulo del tamaño de la pieza) ---

        /// <summary>
        /// La placa de hielo que sube y baja. Lleva encima su franja de hielo (un trigger hijo con
        /// <see cref="ZonaModificadorMovimiento"/>), que se mueve con ella: si se llega corriendo, se resbala.
        /// </summary>
        private static GameObject CrearPlataformaFlotante()
        {
            var raiz = Raiz(PlataformaFlotante, Capa(Capas.Suelo), GameTags.IcePlatform);
            var cuerpo = raiz.AddComponent<Rigidbody2D>();
            cuerpo.bodyType = RigidbodyType2D.Kinematic;
            cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            var caja = raiz.AddComponent<BoxCollider2D>();
            caja.size = TamanoPlaca;
            caja.offset = new Vector2(0f, -TamanoPlaca.y * 0.5f);

            var hielo = new GameObject("Hielo");
            hielo.transform.SetParent(raiz.transform, false);
            var franja = hielo.AddComponent<BoxCollider2D>();
            franja.isTrigger = true;
            franja.size = new Vector2(TamanoPlaca.x, 0.5f);
            franja.offset = new Vector2(0f, 0.25f);
            var zona = hielo.AddComponent<ZonaModificadorMovimiento>();
            Asignar(zona, "tipo", p => p.enumValueIndex = (int)ZonaModificadorMovimiento.TIPOS.HIELO);

            // Con su arte, la placa dibujada (su pivote está en la superficie); sin él, el rectángulo.
            var placa = ConstructorMecanicasMundo2.PlacaFlotante;
            if (placa != null)
            {
                ConArte(raiz.transform, placa);
            }
            else
            {
                Provisional(raiz.transform, Rect.MinMaxRect(-TamanoPlaca.x * 0.5f, -TamanoPlaca.y, TamanoPlaca.x * 0.5f, 0f), Hex("B4D2E6"), "la placa de hielo flotante");
            }

            raiz.AddComponent<Levels.Mecanicas.PlataformaFlotante>();
            return raiz;
        }

        /// <summary>
        /// El tapón de hielo que solo rompe el Ataque Cargado. El cuerpo se pisa (capa del suelo);
        /// la zona de golpe, igual de grande, está en la capa Enemy: es la que nota la espada.
        /// </summary>
        private static GameObject CrearHieloRompible()
        {
            var raiz = Raiz(HieloRompible, Capa(Capas.Suelo), GameTags.IcePlatform);
            var centro = new Vector2(0f, -TamanoTapon.y * 0.5f);
            var caja = raiz.AddComponent<BoxCollider2D>();
            caja.size = TamanoTapon;
            caja.offset = centro;

            var golpe = new GameObject("ZonaDeGolpe") { layer = Capa(Capas.Enemy) };
            golpe.transform.SetParent(raiz.transform, false);
            var zonaGolpe = golpe.AddComponent<BoxCollider2D>();
            zonaGolpe.size = TamanoTapon;
            zonaGolpe.offset = centro;

            // Con su arte, el tapón dibujado y su Animator, que sigue el estado del script (intacto,
            // agrietado y roto); sin él, el rectángulo que cambia de color.
            SpriteRenderer dibujo;
            Animator animator = null;
            var intacto = ConstructorMecanicasMundo2.TaponIntacto;
            var controlador = ConstructorMecanicasMundo2.Tapon.Controlador;
            if (intacto != null && controlador != null)
            {
                dibujo = ConArte(raiz.transform, intacto);
                animator = dibujo.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controlador;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            else
            {
                dibujo = Provisional(raiz.transform, Rect.MinMaxRect(-TamanoTapon.x * 0.5f, -TamanoTapon.y, TamanoTapon.x * 0.5f, 0f), Hex("BFD9F2"), "el hielo rompible");
            }

            var componente = raiz.AddComponent<Levels.Mecanicas.HieloRompible>();
            Asignar(componente, "visual", p => p.objectReferenceValue = dibujo);
            Asignar(componente, "animator", p => p.objectReferenceValue = animator);
            Asignar(componente, "zonaDeGolpe", p => p.objectReferenceValue = zonaGolpe);
            return raiz;
        }

        // --- Mundo del Miedo (sin arte: rectángulos del tamaño de la pieza) ---

        /// <summary>
        /// La antorcha: un poste con la cabeza arriba, que cambia de color con su estado, y su luz
        /// 2D en la llama, apagada hasta que Kael la enciende. Sus medidas son las del Anexo A
        /// (<see cref="DisenoNivel3"/>): 8 u de luz durante 15 s, se enciende a 1,5 u.
        /// </summary>
        private static GameObject CrearAntorcha()
        {
            var raiz = Raiz(Antorcha, 0, null);
            Provisional(raiz.transform, Rect.MinMaxRect(-0.1f, 0f, 0.1f, AlturaLlama - 0.2f), Hex("2B2118"), "la antorcha", "Prompts_Mundo3.md", "Poste");
            var cabeza = Provisional(raiz.transform, Rect.MinMaxRect(-0.18f, AlturaLlama - 0.18f, 0.18f, AlturaLlama + 0.18f), new Color(0.25f, 0.22f, 0.2f, 1f), "la antorcha (la llama)", "Prompts_Mundo3.md", "Cabeza");

            var objetoLuz = new GameObject("Luz");
            objetoLuz.transform.SetParent(raiz.transform, false);
            objetoLuz.transform.localPosition = new Vector3(0f, AlturaLlama, 0f);
            var luz = objetoLuz.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.pointLightOuterRadius = DisenoNivel3.RadioAntorcha;
            luz.pointLightInnerRadius = 1f;
            luz.intensity = 1.2f;
            luz.color = ColorLuzAntorcha;
            luz.shadowsEnabled = false;
            luz.enabled = false;

            var componente = raiz.AddComponent<Levels.Mecanicas.Antorcha>();
            componente.radio = DisenoNivel3.RadioAntorcha;
            componente.duracion = DisenoNivel3.DuracionAntorcha;
            componente.parpadeo = DisenoNivel3.ParpadeoAntorcha;
            componente.distanciaEncender = DisenoNivel3.DistanciaEncender;
            Asignar(componente, "llama", p => p.vector2Value = new Vector2(0f, AlturaLlama));
            Asignar(componente, "luz", p => p.objectReferenceValue = luz);
            Asignar(componente, "cabeza", p => p.objectReferenceValue = cabeza);
            return raiz;
        }

        /// <summary>
        /// El cepo: un rectángulo en el suelo, escondido (transparente) hasta que Kael está cerca o
        /// lo alumbra una antorcha. Sujeta 1 s, quita 1 y suena a 8 u (<see cref="DisenoNivel3"/>).
        /// </summary>
        private static GameObject CrearCepo()
        {
            var raiz = Raiz(Cepo, 0, null);
            var dibujo = Provisional(raiz.transform, Rect.MinMaxRect(-TamanoCepo.x * 0.5f, 0f, TamanoCepo.x * 0.5f, TamanoCepo.y), Hex("8C8C99"), "el cepo", "Prompts_Mundo3.md");
            dibujo.sortingOrder = 1;
            var componente = raiz.AddComponent<Levels.Mecanicas.Cepo>();
            componente.danio = DisenoNivel3.DanioCepo;
            componente.sujecion = DisenoNivel3.SujecionCepo;
            componente.radioRuido = DisenoNivel3.RadioRuidoCepo;
            componente.distanciaVer = DisenoNivel3.DistanciaVerCepo;
            Asignar(componente, "tamano", p => p.vector2Value = TamanoCepo);
            Asignar(componente, "visual", p => p.objectReferenceValue = dibujo);
            return raiz;
        }

        /// <summary>
        /// La rama podrida: una <see cref="Levels.Mecanicas.PlataformaInestable"/> con los tiempos del
        /// Miedo (cruje 0,3 s, se parte y vuelve a los 4 s) y una <see cref="Levels.Mecanicas.RamaPodrida"/>,
        /// que a oscuras la pinta como las sanas y bajo una antorcha la delata. Pivote en la superficie.
        /// </summary>
        private static GameObject CrearRamaPodrida()
        {
            var raiz = Raiz(RamaPodrida, Capa(Capas.Suelo), GameTags.Suelo);
            var caja = raiz.AddComponent<BoxCollider2D>();
            caja.size = TamanoRama;
            caja.offset = new Vector2(0f, -TamanoRama.y * 0.5f);
            var dibujo = Provisional(raiz.transform, Rect.MinMaxRect(-TamanoRama.x * 0.5f, -TamanoRama.y, TamanoRama.x * 0.5f, 0f), ColorRamaSinArte, "la rama podrida", "Prompts_Mundo3.md");
            dibujo.sortingOrder = 0;

            var plataforma = raiz.AddComponent<Levels.Mecanicas.PlataformaInestable>();
            plataforma.retardoRotura = DisenoNivel3.CrujidoRama;
            plataforma.duracionCaida = DuracionCaidaRama;
            plataforma.tiempoReaparicion = DisenoNivel3.VueltaRama - DuracionCaidaRama;
            Asignar(plataforma, "visual", p => p.objectReferenceValue = dibujo);

            var rama = raiz.AddComponent<Levels.Mecanicas.RamaPodrida>();
            Asignar(rama, "visual", p => p.objectReferenceValue = dibujo);
            Asignar(rama, "colorSana", p => p.colorValue = ColorRamaSinArte);
            return raiz;
        }

        /// <summary>Lo que se ve partirse la rama antes de desaparecer (lo mínimo de la plataforma inestable).</summary>
        public const float DuracionCaidaRama = 0.25f;

        /// <summary>El rectángulo provisional de una pieza sin arte, y el aviso de lo que falta.</summary>
        // El dibujo de una pieza con arte: en el origen del prefab (el pivote de su sprite es el suyo)
        // y a escala 1 (su tamaño lo dan sus píxeles por unidad).
        private static SpriteRenderer ConArte(Transform padre, Sprite sprite)
        {
            var objeto = new GameObject("Sprite") { layer = padre.gameObject.layer };
            objeto.transform.SetParent(padre, false);
            var dibujo = objeto.AddComponent<SpriteRenderer>();
            dibujo.sprite = sprite;
            dibujo.sortingOrder = OrdenDibujo;
            return dibujo;
        }

        private static SpriteRenderer Provisional(Transform padre, Rect zona, Color color, string pieza, string prompts = "Prompts_Mundo2.md",
            string nombre = "Sprite")
        {
            Debug.LogWarning($"[Fragmentum] Todavía no hay arte para {pieza}: el prefab lleva un rectángulo provisional (Docs/Diseno/{prompts}).");
            var objeto = new GameObject(nombre) { layer = padre.gameObject.layer };
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = zona.center;
            objeto.transform.localScale = new Vector3(zona.width, zona.height, 1f);
            var dibujo = objeto.AddComponent<SpriteRenderer>();
            dibujo.sprite = SpriteCuadrado;
            dibujo.color = color;
            dibujo.sortingOrder = OrdenDibujo;
            return dibujo;
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
