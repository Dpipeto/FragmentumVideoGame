using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Enemies.Deteccion;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Fase 4: genera los datos de los enemigos (un <see cref="EnemyData"/> por variante del
    /// Anexo A), las estrategias de detección, el prefab base con sus variantes y una escena
    /// de prueba con una zona por variante.
    ///
    /// Los datos solo se crean si faltan: si ya los has ajustado a mano, «Generar» no los
    /// toca. «Restablecer» los devuelve a los valores de diseño.
    /// </summary>
    public static class GeneradorEnemigos
    {
        public const string CarpetaDatos = "Assets/Data/Enemigos";
        public const string CarpetaDeteccion = "Assets/Data/Enemigos/Deteccion";
        public const string CarpetaPrefabs = "Assets/Prefabs/Enemigos";
        public const string RutaPrefabBase = CarpetaPrefabs + "/Enemigo.prefab";
        public const string RutaEscena = "Assets/Scenes/Pruebas/PruebaEnemigos.unity";

        /// <summary>Variantes con prefab propio (la genérica usa el prefab base).</summary>
        public static readonly string[] Variantes = { "SoldadoLava", "GolemCeniza", "Lamento", "Espectro", "CentinelaCiego" };

        // --- Menús ---

        [MenuItem("Fragmentum/Enemigos/Generar datos de enemigos (Anexo A)", priority = 80)]
        public static void MenuGenerarDatos() => GenerarDatos(false);

        [MenuItem("Fragmentum/Enemigos/Restablecer datos de enemigos al Anexo A", priority = 81)]
        public static void MenuRestablecerDatos()
        {
            if (EditorUtility.DisplayDialog("Fragmentum",
                    "Se sobrescribirán los valores de todos los EnemyData con los del diseño. ¿Continuar?",
                    "Restablecer", "Cancelar"))
            {
                GenerarDatos(true);
            }
        }

        [MenuItem("Fragmentum/Enemigos/Crear prefabs de enemigos", priority = 82)]
        public static void MenuCrearPrefabs()
        {
            var prefab = CrearPrefabs();
            if (prefab != null)
            {
                EditorGUIUtility.PingObject(prefab);
            }
        }

        [MenuItem("Fragmentum/Enemigos/Crear escena de prueba de enemigos", priority = 83)]
        public static void MenuCrearEscena()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                CrearEscenaPrueba(true);
            }
        }

        // --- Datos ---

        /// <summary>Crea las estrategias de detección y los EnemyData del Anexo A.</summary>
        public static void GenerarDatos(bool sobrescribir)
        {
            AsegurarCarpeta(CarpetaDeteccion);

            var distancia = Deteccion<DeteccionPorDistancia>("PorDistancia", sobrescribir,
                "Genérico: ve al jugador dentro de su radio.");
            var vision = Deteccion<DeteccionConVision>("ConVision", sobrescribir,
                "Ve dentro de su radio, pero no a través de suelo ni paredes.");
            var oscuridad = Deteccion<DeteccionEnOscuridad>("EnOscuridad", sobrescribir,
                "Miedo (Espectro): solo va a por el jugador cuando está a oscuras.");
            var ruido = Deteccion<DeteccionPorRuido>("PorRuido", sobrescribir,
                "Miedo (Centinela Ciego): no ve; oye al jugador cuando corre.");

            Datos("Generico", sobrescribir, d =>
            {
                d.nombreCanonico = "Enemigo genérico";
                d.descripcion = "Referencia neutra para comparar las variantes emocionales.";
                d.vidaMaxima = 3; d.danio = 1; d.danioContacto = 1;
                d.velocidadPatrulla = 1.5f; d.velocidadPersecucion = 3f; d.esperaEnWaypoint = 1f;
                d.deteccion = distancia; d.detectionRadius = 5f;
                d.attackRadius = 1.3f; d.attackCooldown = 1.2f;
                d.duracionAtaque = 0.5f; d.inicioVentanaGolpe = 0.25f; d.finVentanaGolpe = 0.4f;
                d.damagedDuration = 0.35f; d.resistenciaRetroceso = 0f;
                d.tinte = Hex("9A9A9A");
            });

            Datos("SoldadoLava", sobrescribir, d =>
            {
                d.nombreCanonico = "Soldado de lava";
                d.descripcion = "Ira: agresivo y rápido. Apenas se detiene, persige de lejos y ataca a menudo.";
                d.vidaMaxima = 3; d.danio = 1; d.danioContacto = 1;
                d.velocidadPatrulla = 2.5f; d.velocidadPersecucion = 5f; d.esperaEnWaypoint = 0.3f;
                d.deteccion = vision; d.detectionRadius = 7f;
                d.attackRadius = 1.3f; d.attackCooldown = 0.7f;
                d.duracionAtaque = 0.4f; d.inicioVentanaGolpe = 0.15f; d.finVentanaGolpe = 0.3f;
                d.damagedDuration = 0.25f; d.resistenciaRetroceso = 0f;
                d.tinte = Hex("CC4400");
            });

            Datos("GolemCeniza", sobrescribir, d =>
            {
                d.nombreCanonico = "Golem de Ceniza";
                d.descripcion = "Ira (mini-jefe, vida 10): lento, telegrafía mucho el golpe y apenas retrocede.";
                d.vidaMaxima = 10; d.danio = 2; d.danioContacto = 1;
                d.velocidadPatrulla = 1f; d.velocidadPersecucion = 2f; d.esperaEnWaypoint = 1.5f;
                d.deteccion = vision; d.detectionRadius = 6f;
                d.attackRadius = 1.8f; d.alturaAtaque = 2.2f; d.attackCooldown = 2f;
                d.duracionAtaque = 0.9f; d.inicioVentanaGolpe = 0.5f; d.finVentanaGolpe = 0.7f;
                d.damagedDuration = 0.2f; d.resistenciaRetroceso = 0.9f; d.deathDelay = 2f;
                d.tinte = Hex("5C4033");
            });

            Datos("Lamento", sobrescribir, d =>
            {
                d.nombreCanonico = "Lamento";
                d.descripcion = "Tristeza: lento y pesado. Se detiene largo rato en cada punto y persigue sin prisa.";
                d.vidaMaxima = 4; d.danio = 1; d.danioContacto = 1;
                d.velocidadPatrulla = 0.8f; d.velocidadPersecucion = 1.4f; d.esperaEnWaypoint = 2.5f;
                d.deteccion = distancia; d.detectionRadius = 5f;
                d.attackRadius = 1.2f; d.attackCooldown = 2.2f;
                d.duracionAtaque = 0.8f; d.inicioVentanaGolpe = 0.45f; d.finVentanaGolpe = 0.6f;
                d.damagedDuration = 0.5f; d.resistenciaRetroceso = 0.6f;
                d.tinte = Hex("4A7FA5");
            });

            Datos("Espectro", sobrescribir, d =>
            {
                d.nombreCanonico = "Espectro";
                d.descripcion = "Miedo: solo se acerca si el jugador está a oscuras; la luz de una antorcha lo mantiene a raya.";
                d.vidaMaxima = 2; d.danio = 1; d.danioContacto = 1;
                d.velocidadPatrulla = 1.5f; d.velocidadPersecucion = 3.8f; d.esperaEnWaypoint = 0.8f;
                d.deteccion = oscuridad; d.detectionRadius = 6f;
                d.attackRadius = 1.2f; d.attackCooldown = 1f;
                d.duracionAtaque = 0.45f; d.inicioVentanaGolpe = 0.2f; d.finVentanaGolpe = 0.35f;
                d.damagedDuration = 0.3f; d.resistenciaRetroceso = 0f;
                d.tinte = new Color(0.23f, 0.23f, 0.29f, 0.8f);
            });

            Datos("CentinelaCiego", sobrescribir, d =>
            {
                d.nombreCanonico = "Centinela Ciego";
                d.descripcion = "Miedo: no ve. Oye al jugador cuando corre; caminar despacio es seguro.";
                d.vidaMaxima = 5; d.danio = 2; d.danioContacto = 1;
                d.velocidadPatrulla = 1f; d.velocidadPersecucion = 4f; d.esperaEnWaypoint = 1f;
                d.deteccion = ruido; d.detectionRadius = 8f;
                d.attackRadius = 1.4f; d.attackCooldown = 1.5f;
                d.duracionAtaque = 0.6f; d.inicioVentanaGolpe = 0.3f; d.finVentanaGolpe = 0.45f;
                d.damagedDuration = 0.3f; d.resistenciaRetroceso = 0.4f;
                d.tinte = Hex("2D1B4E");
            });

            AssetDatabase.SaveAssets();
            Debug.Log($"[Fragmentum] Datos de enemigos {(sobrescribir ? "restablecidos" : "comprobados")} en {CarpetaDatos}.");
        }

        public static EnemyData CargarDatos(string nombre) =>
            AssetDatabase.LoadAssetAtPath<EnemyData>($"{CarpetaDatos}/{nombre}.asset");

        private static T Deteccion<T>(string nombre, bool sobrescribir, string descripcion) where T : DeteccionData
        {
            var ruta = $"{CarpetaDeteccion}/{nombre}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(ruta);
            var nuevo = asset == null;
            if (nuevo)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, ruta);
            }

            if (nuevo || sobrescribir)
            {
                Asignar(asset, "descripcion", p => p.stringValue = descripcion);
            }

            return asset;
        }

        private static void Datos(string nombre, bool sobrescribir, Action<EnemyData> rellenar)
        {
            AsegurarCarpeta(CarpetaDatos);
            var ruta = $"{CarpetaDatos}/{nombre}.asset";
            var datos = AssetDatabase.LoadAssetAtPath<EnemyData>(ruta);

            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<EnemyData>();
                rellenar(datos);
                AssetDatabase.CreateAsset(datos, ruta);
                return;
            }

            if (sobrescribir)
            {
                rellenar(datos);
                EditorUtility.SetDirty(datos);
            }
        }

        // --- Prefabs ---

        /// <summary>Crea el prefab base (enemigo genérico) y una variante por tipo de enemigo.</summary>
        public static GameObject CrearPrefabs()
        {
            AsegurarConfiguracion();
            GenerarDatos(false);
            AsegurarCarpeta(CarpetaPrefabs);

            var base_ = CrearPrefabBase();
            if (base_ == null)
            {
                return null;
            }

            foreach (var variante in Variantes)
            {
                CrearVariante(base_, variante);
            }

            Debug.Log($"[Fragmentum] Prefabs de enemigos creados en {CarpetaPrefabs} (base + {Variantes.Length} variantes).");
            return base_;
        }

        public static GameObject CargarPrefab(string variante) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(
                string.IsNullOrEmpty(variante) || variante == "Generico" ? RutaPrefabBase : $"{CarpetaPrefabs}/Enemigo_{variante}.prefab");

        private static GameObject CrearPrefabBase()
        {
            var raiz = new GameObject("Enemigo") { layer = Capa(Capas.Enemy), tag = GameTags.Enemy };
            try
            {
                var cuerpoRigido = raiz.AddComponent<Rigidbody2D>();
                cuerpoRigido.gravityScale = 3f;
                cuerpoRigido.freezeRotation = true;
                cuerpoRigido.interpolation = RigidbodyInterpolation2D.Interpolate;
                cuerpoRigido.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var cuerpo = raiz.AddComponent<CapsuleCollider2D>();
                cuerpo.size = new Vector2(0.8f, 1.6f);
                cuerpo.offset = new Vector2(0f, 0.8f);

                raiz.AddComponent<Health>();
                var controlador = raiz.AddComponent<EnemyController>();

                var pivote = new GameObject("Pivote").transform;
                pivote.SetParent(raiz.transform, false);

                var objetoSprite = new GameObject("Sprite") { layer = raiz.layer };
                objetoSprite.transform.SetParent(pivote, false);
                objetoSprite.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                var sprite = objetoSprite.AddComponent<SpriteRenderer>();
                sprite.sprite = SpriteCapsula;
                sprite.sortingOrder = 5;
                if (sprite.sprite != null)
                {
                    objetoSprite.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                }

                var animator = objetoSprite.AddComponent<Animator>();
                var provisional = objetoSprite.AddComponent<SpriteProvisional>();
                Asignar(provisional, "assetPendiente", p => p.stringValue = "Sprite y animaciones del enemigo (PENDIENTES B4)");
                Asignar(provisional, "tamano", p => p.vector2Value = new Vector2(0.8f, 1.6f));

                // Arma: solo golpea a la capa Player y solo durante la ventana del ataque.
                var objetoArma = new GameObject("Arma") { layer = Capa(Capas.EnemyWeapon) };
                objetoArma.transform.SetParent(pivote, false);
                objetoArma.transform.localPosition = new Vector3(0.8f, 0.8f, 0f);
                var zonaArma = objetoArma.AddComponent<BoxCollider2D>();
                zonaArma.isTrigger = true;
                zonaArma.size = new Vector2(1f, 1.2f);
                var arma = objetoArma.AddComponent<Hitbox>();
                Asignar(arma, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
                objetoArma.SetActive(false);

                // Contacto: tocar el cuerpo hace daño (sustituye al OnCollisionEnter2D de la v1).
                var objetoContacto = new GameObject("Contacto") { layer = Capa(Capas.EnemyWeapon) };
                objetoContacto.transform.SetParent(raiz.transform, false);
                var zonaContacto = objetoContacto.AddComponent<CapsuleCollider2D>();
                zonaContacto.isTrigger = true;
                zonaContacto.size = new Vector2(0.7f, 1.5f);
                zonaContacto.offset = new Vector2(0f, 0.8f);
                var contacto = objetoContacto.AddComponent<Hitbox>();
                Asignar(contacto, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
                Asignar(contacto, "golpearUnaVezPorActivacion", p => p.boolValue = false);

                Asignar(controlador, "datos", p => p.objectReferenceValue = CargarDatos("Generico"));
                Asignar(controlador, "animator", p => p.objectReferenceValue = animator);
                Asignar(controlador, "pivote", p => p.objectReferenceValue = pivote);
                Asignar(controlador, "sprite", p => p.objectReferenceValue = sprite);
                Asignar(controlador, "arma", p => p.objectReferenceValue = arma);
                Asignar(controlador, "contacto", p => p.objectReferenceValue = contacto);

                var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBase, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {RutaPrefabBase}.");
                    return null;
                }

                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        /// <summary>Variante de prefab: hereda la estructura del base y cambia los datos (y el tamaño del Golem).</summary>
        private static void CrearVariante(GameObject base_, string nombre)
        {
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(base_);
            try
            {
                instancia.name = $"Enemigo_{nombre}";
                var controlador = instancia.GetComponent<EnemyController>();
                Asignar(controlador, "datos", p => p.objectReferenceValue = CargarDatos(nombre));

                if (nombre == "GolemCeniza")
                {
                    AgrandarGolem(instancia, controlador);
                }

                PrefabUtility.SaveAsPrefabAsset(instancia, $"{CarpetaPrefabs}/Enemigo_{nombre}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }

        // El Golem es un mini-jefe: más grande y con más alcance.
        private static void AgrandarGolem(GameObject instancia, EnemyController controlador)
        {
            var serializado = new SerializedObject(controlador);
            var sprite = serializado.FindProperty("sprite").objectReferenceValue as SpriteRenderer;
            var arma = serializado.FindProperty("arma").objectReferenceValue as Hitbox;
            var contacto = serializado.FindProperty("contacto").objectReferenceValue as Hitbox;

            var cuerpo = instancia.GetComponent<CapsuleCollider2D>();
            cuerpo.size = new Vector2(1.4f, 2.4f);
            cuerpo.offset = new Vector2(0f, 1.2f);

            if (sprite != null)
            {
                sprite.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                sprite.transform.localScale = new Vector3(1.4f, 1.2f, 1f);
            }

            if (arma != null)
            {
                arma.transform.localPosition = new Vector3(1.3f, 1.2f, 0f);
                arma.GetComponent<BoxCollider2D>().size = new Vector2(1.6f, 2f);
            }

            if (contacto != null)
            {
                var zona = contacto.GetComponent<CapsuleCollider2D>();
                zona.size = new Vector2(1.3f, 2.3f);
                zona.offset = new Vector2(0f, 1.2f);
            }
        }

        // --- Escena de prueba ---

        /// <summary>
        /// Crea (o regenera) <see cref="RutaEscena"/>: una pista larga con una zona por variante
        /// de enemigo, para comparar su comportamiento. Devuelve si se guardó.
        /// </summary>
        public static bool CrearEscenaPrueba(bool preguntarSiExiste)
        {
            AsegurarConfiguracion();
            var jugadorPrefab = ConstructorJugador.PrefabJugador();
            if (CrearPrefabs() == null || jugadorPrefab == null)
            {
                return false;
            }

            var creada = CrearEscenaDesdePlantilla(RutaEscena, preguntarSiExiste);
            if (creada == null)
            {
                return false;
            }

            var escena = creada.Value;
            var nivel = new GameObject("Nivel").transform;
            var rutas = new GameObject("Rutas").transform;
            var colorSuelo = Hex("5C4033");

            Suelo(nivel, "Suelo", -10f, 122f, -1f, 0f, colorSuelo);
            ZonaMortal(nivel, -20f, 140f, -10f);

            // Una zona cada 16 u, con patrulla entre ±4 u de su centro.
            Zona(escena, nivel, rutas, "Generico", 14f);
            Zona(escena, nivel, rutas, "SoldadoLava", 30f);
            Zona(escena, nivel, rutas, "GolemCeniza", 46f);
            Zona(escena, nivel, rutas, "Lamento", 62f);
            Zona(escena, nivel, rutas, "Espectro", 78f);
            Zona(escena, nivel, rutas, "CentinelaCiego", 94f);

            // Refugio de luz para el Espectro: dentro del círculo, no se acerca.
            var luz = new GameObject("LuzRefugio").AddComponent<ZonaIluminada>();
            luz.transform.position = new Vector3(71f, 1f, 0f);
            var halo = Bloque(nivel, "HaloLuz", 69f, 73f, 0f, 3f, 0, new Color(1f, 0.85f, 0.35f, 0.18f), SpriteCirculo);
            Object.DestroyImmediate(halo.GetComponent<BoxCollider2D>());

            // Plataforma con un enemigo quieto (sin waypoints) que no debe caerse por el borde.
            Suelo(nivel, "Plataforma", 106f, 114f, 2.5f, 3f, colorSuelo);
            var quieto = (GameObject)PrefabUtility.InstantiatePrefab(CargarPrefab("Generico"), escena);
            quieto.name = "Generico_SinRuta";
            quieto.transform.position = new Vector3(110f, 3.05f, 0f);

            var aparicion = new GameObject("PuntoDeAparicion").transform;
            aparicion.position = new Vector3(0f, 0.05f, 0f);

            var jugador = InstanciarJugador(jugadorPrefab, escena, aparicion.position, TodasLasHabilidades);
            // En esta escena correr hace ruido, para que el Centinela Ciego pueda oír.
            Asignar(jugador.GetComponent<PlayerModoMovimiento>(), "emitirRuido", p => p.boolValue = true);
            CamaraYReaparicion(jugador, aparicion, new Vector2(-10f, -6f), new Vector2(122f, 16f));

            var depuracion = new GameObject("Depuracion");
            depuracion.AddComponent<DepuracionEnemigos>();
            var panel = depuracion.AddComponent<DepuracionJugador>();
            Asignar(panel, "jugador", p => p.objectReferenceValue = jugador.GetComponent<PlayerController>());
            Asignar(panel, "modo", p => p.objectReferenceValue = jugador.GetComponent<PlayerModoMovimiento>());
            Asignar(panel, "notas", p => p.stringValue =
                "<b>Pista de enemigos</b> (F2 muestra u oculta su estado)\n" +
                "x = 14 Genérico · 30 Soldado de lava (rápido, no ve a través de muros)\n" +
                "x = 46 Golem de Ceniza (lento, pesado, vida 10) · 62 Lamento (Tristeza, lento)\n" +
                "x = 78 Espectro: en el círculo de luz (x = 71) no se acerca\n" +
                "x = 94 Centinela Ciego: te oye si corres; con mando, camina despacio\n" +
                "x = 110 enemigo sin ruta en una plataforma: no se tira por el borde\n" +
                "Acércate por la izquierda y por la derecha: debe atacar por ambos lados.");

            CrearHud(jugador.GetComponent<PlayerHealth>());

            // Los bloques se ven con el tileset; sus colisiones no cambian.
            PintorEscenario.Pintar(escena, nivel, false);

            EditorSceneManager.MarkSceneDirty(escena);
            var ok = EditorSceneManager.SaveScene(escena, RutaEscena);
            if (ok)
            {
                Debug.Log($"[Fragmentum] Escena de prueba de enemigos creada en {RutaEscena}.");
            }

            return ok;
        }

        private static void Zona(UnityEngine.SceneManagement.Scene escena, Transform nivel, Transform rutas, string variante, float centro)
        {
            var enemigo = (GameObject)PrefabUtility.InstantiatePrefab(CargarPrefab(variante), escena);
            enemigo.transform.position = new Vector3(centro, 0.05f, 0f);

            var a = new GameObject($"{variante}_A").transform;
            var b = new GameObject($"{variante}_B").transform;
            a.SetParent(rutas, false);
            b.SetParent(rutas, false);
            a.position = new Vector3(centro - 4f, 0f, 0f);
            b.position = new Vector3(centro + 4f, 0f, 0f);

            Asignar(enemigo.GetComponent<EnemyController>(), "waypoints", p =>
            {
                p.arraySize = 2;
                p.GetArrayElementAtIndex(0).objectReferenceValue = a;
                p.GetArrayElementAtIndex(1).objectReferenceValue = b;
            });
        }
    }
}
