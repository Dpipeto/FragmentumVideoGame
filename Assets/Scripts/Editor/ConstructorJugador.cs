using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Core.Input;
using Fragmentum.Levels;
using Fragmentum.Levels.Escenario;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Levels.Pruebas;
using Fragmentum.Player;
using Fragmentum.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Monta desde el menú lo que la Fase 3 necesita en el Editor, para que los «pasos
    /// manuales» se reduzcan a elegir una opción de menú:
    /// - el prefab del jugador con todos sus componentes y referencias;
    /// - una escena de prueba con una zona para cada criterio de aceptación del jugador;
    /// - la preparación de un tilemap como suelo (capa, collider compuesto y cuerpo estático).
    ///
    /// Todo es idempotente: volver a ejecutarlo regenera el prefab o la escena.
    /// </summary>
    public static class ConstructorJugador
    {
        public const string RutaPrefab = "Assets/Prefabs/Player.prefab";
        public const string RutaEscena = "Assets/Scenes/Pruebas/PruebaJugador.unity";

        private const string RutaPlantillaEscena = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
        private const string RutaMaterialSinFriccion = "Assets/Settings/Fisica/JugadorSinFriccion.physicsMaterial2D";
        private const string RutaAccionesInput = "Assets/Settings/InputSystem_Actions.inputactions";
        private const string CarpetaSpritesBase = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/";

        // Paleta del Anexo A (Ira) para que la escena de prueba ya hable el idioma del Nivel 1.
        private static readonly Color ColorSuelo = Hex("5C4033");
        private static readonly Color ColorSolapado = Hex("8B1A1A");
        private static readonly Color ColorPared = Hex("3A3A4A");
        private static readonly Color ColorTecho = Hex("2D1B4E");
        private static readonly Color ColorJugador = Hex("E8EEF4");
        private static readonly Color ColorPeligro = Hex("CC4400");

        // --- Menús ---

        [MenuItem("Fragmentum/Jugador/Crear prefab del jugador", priority = 40)]
        public static void MenuCrearPrefab()
        {
            var prefab = CrearPrefab();
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
        }

        [MenuItem("Fragmentum/Jugador/Crear escena de prueba del jugador", priority = 41)]
        public static void MenuCrearEscena()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                CrearEscenaPrueba(true);
            }
        }

        [MenuItem("Fragmentum/Jugador/Preparar tilemap seleccionado como suelo", priority = 60)]
        public static void MenuPrepararTilemap()
        {
            var objeto = Selection.activeGameObject;
            if (objeto == null || objeto.GetComponent<Tilemap>() == null)
            {
                EditorUtility.DisplayDialog("Fragmentum", "Selecciona en la jerarquía el objeto que tiene el Tilemap.", "Vale");
                return;
            }

            PrepararTilemapComoSuelo(objeto);
        }

        [MenuItem("Fragmentum/Jugador/Preparar tilemap seleccionado como suelo", true)]
        private static bool ValidarPrepararTilemap() =>
            Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Tilemap>() != null;

        /// <summary>
        /// Entrada para modo batch, sin diálogos: configura el proyecto, genera el prefab y la
        /// escena de prueba y exporta los diagramas FSM.
        /// <c>Unity.exe -batchmode -projectPath … -executeMethod Fragmentum.EditorTools.ConstructorJugador.ConstruirEnModoBatch -quit</c>
        /// </summary>
        public static void ConstruirEnModoBatch()
        {
            ConfiguradorProyecto.ConfigurarTodo();
            var escenaCreada = CrearEscenaPrueba(false);
            ExportadorDiagramasFSM.Exportar();
            AssetDatabase.SaveAssets();

            if (!escenaCreada)
            {
                Debug.LogError("[Fragmentum] La construcción en modo batch falló.");
                EditorApplication.Exit(1);
            }
        }

        // --- Prefab del jugador ---

        /// <summary>Crea (o regenera) <see cref="RutaPrefab"/>. Devuelve el prefab o null si falló.</summary>
        public static GameObject CrearPrefab()
        {
            AsegurarConfiguracion();
            AsegurarCarpeta("Assets/Prefabs");

            var raiz = new GameObject("Player");
            try
            {
                raiz.tag = GameTags.Player;
                raiz.layer = Capa(Capas.Player);

                var cuerpoRigido = raiz.AddComponent<Rigidbody2D>();
                cuerpoRigido.bodyType = RigidbodyType2D.Dynamic;
                cuerpoRigido.gravityScale = 3f;
                cuerpoRigido.freezeRotation = true;
                cuerpoRigido.interpolation = RigidbodyInterpolation2D.Interpolate;
                cuerpoRigido.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                // Pivote en los pies: la posición del jugador es el punto donde pisa.
                var cuerpo = raiz.AddComponent<CapsuleCollider2D>();
                cuerpo.direction = CapsuleDirection2D.Vertical;
                cuerpo.size = new Vector2(0.8f, 1.8f);
                cuerpo.offset = new Vector2(0f, 0.9f);
                cuerpo.sharedMaterial = MaterialSinFriccion();

                raiz.AddComponent<PlayerMotor>();
                raiz.AddComponent<CheckGround>();
                raiz.AddComponent<PlayerSensors>();

                var vida = raiz.AddComponent<PlayerHealth>();
                Asignar(vida, "vidaMaxima", p => p.intValue = 5);
                Asignar(vida, "invulnerabilidadTrasGolpe", p => p.floatValue = 1f);

                raiz.AddComponent<PlayerAbilities>();

                var input = raiz.AddComponent<FuenteInputJugador>();
                var acciones = AssetDatabase.LoadMainAssetAtPath(RutaAccionesInput);
                if (acciones == null)
                {
                    Debug.LogWarning($"[Fragmentum] No se encontró {RutaAccionesInput}; asigna el asset de acciones en FuenteInputJugador.");
                }

                Asignar(input, "acciones", p => p.objectReferenceValue = acciones);

                var controlador = raiz.AddComponent<PlayerController>();
                raiz.AddComponent<PlayerModoMovimiento>();
                var visual = raiz.AddComponent<PlayerVisual>();

                // Pivote: lo que se voltea al girar (sprite y espada).
                var pivote = new GameObject("Pivote").transform;
                pivote.SetParent(raiz.transform, false);

                var objetoSprite = new GameObject("Sprite");
                objetoSprite.layer = raiz.layer;
                objetoSprite.transform.SetParent(pivote, false);
                objetoSprite.transform.localPosition = new Vector3(0f, 0.9f, 0f);

                var sprite = objetoSprite.AddComponent<SpriteRenderer>();
                sprite.sprite = CargarSprite(CarpetaSpritesBase + "Capsule.png");
                sprite.color = ColorJugador;
                sprite.sortingOrder = 10;
                if (sprite.sprite != null)
                {
                    objetoSprite.transform.localScale = new Vector3(0.8f, 0.9f, 1f); // la cápsula mide 1 × 2 u
                }

                var animator = objetoSprite.AddComponent<Animator>();
                var provisional = objetoSprite.AddComponent<SpriteProvisional>();
                Asignar(provisional, "assetPendiente", p => p.stringValue = "Sprites y animaciones de Kael (PENDIENTES B3)");
                Asignar(provisional, "color", p => p.colorValue = ColorJugador);
                Asignar(provisional, "tamano", p => p.vector2Value = new Vector2(0.8f, 1.8f));

                // Espada: hitbox en la capa PlayerWeapon que solo golpea a la capa Enemy.
                var objetoEspada = new GameObject("Espada");
                objetoEspada.layer = Capa(Capas.PlayerWeapon);
                objetoEspada.transform.SetParent(pivote, false);
                objetoEspada.transform.localPosition = new Vector3(0.9f, 0.9f, 0f);

                var zonaEspada = objetoEspada.AddComponent<BoxCollider2D>();
                zonaEspada.isTrigger = true;
                zonaEspada.size = new Vector2(1.2f, 1.3f);

                var hitbox = objetoEspada.AddComponent<Hitbox>();
                Asignar(hitbox, "danio", p => p.intValue = 1);
                Asignar(hitbox, "objetivos", p => p.intValue = Capas.Mascara(Capas.Enemy));
                objetoEspada.SetActive(false);

                Asignar(controlador, "animator", p => p.objectReferenceValue = animator);
                Asignar(controlador, "hitboxAtaque", p => p.objectReferenceValue = hitbox);
                Asignar(controlador, "fuenteInput", p => p.objectReferenceValue = input);
                Asignar(visual, "pivote", p => p.objectReferenceValue = pivote);
                Asignar(visual, "sprite", p => p.objectReferenceValue = sprite);

                var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {RutaPrefab}.");
                    return null;
                }

                Debug.Log($"[Fragmentum] Prefab del jugador creado en {RutaPrefab}.", prefab);
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        // --- Escena de prueba ---

        /// <summary>
        /// Crea (o regenera) <see cref="RutaEscena"/>: un circuito con una zona para cada
        /// criterio de aceptación del jugador. Devuelve si se guardó.
        /// </summary>
        public static bool CrearEscenaPrueba(bool preguntarSiExiste)
        {
            AsegurarConfiguracion();

            var prefab = CrearPrefab();
            if (prefab == null)
            {
                return false;
            }

            AsegurarCarpeta("Assets/Scenes/Pruebas");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) != null)
            {
                if (preguntarSiExiste && !EditorUtility.DisplayDialog("Fragmentum",
                        $"{RutaEscena} ya existe. ¿Regenerarla? Se perderán los cambios hechos a mano en ella.",
                        "Regenerar", "Cancelar"))
                {
                    return false;
                }

                AssetDatabase.DeleteAsset(RutaEscena);
            }

            Scene escena;
            if (AssetDatabase.CopyAsset(RutaPlantillaEscena, RutaEscena))
            {
                escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            }
            else
            {
                Debug.LogWarning(
                    $"[Fragmentum] No se encontró la plantilla {RutaPlantillaEscena}. La escena se crea sin " +
                    "Global Light 2D: añade una si los sprites se ven oscuros.");
                escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }

            var cuadrado = CargarSprite(CarpetaSpritesBase + "Square.png");
            var suelo = Capa(Capas.Suelo);
            var pared = Capa(Capas.Wall);
            var nivel = new GameObject("Nivel").transform;

            // Zona 1: suelos solapados (criterio: dos plataformas solapadas no rompen el suelo).
            Bloque(nivel, "SueloInicio", -24f, 10f, -1f, 0f, suelo, ColorSuelo, cuadrado, GameTags.Suelo);
            Bloque(nivel, "SueloSolapado", 6f, 18f, -1.5f, 0f, suelo, ColorSolapado, cuadrado, GameTags.Suelo);

            // Zona 2: techo bajo para agacharse (cabe agachado, no de pie).
            Bloque(nivel, "TechoBajo", -8f, -4f, 1.2f, 2.2f, suelo, ColorTecho, cuadrado, GameTags.Suelo);

            // Zona 3: agua (gravedad ×0,3, velocidad ×0,6; el dash solo con Dash Acuático) y hielo.
            Zona(nivel, "Agua", -22f, -14f, 0f, 4f, ZonaModificadorMovimiento.TIPOS.AGUA, new Color(0.29f, 0.5f, 0.65f, 0.35f), cuadrado);
            Zona(nivel, "Hielo", 12f, 16f, 0f, 0.5f, ZonaModificadorMovimiento.TIPOS.HIELO, new Color(0.9f, 0.95f, 1f, 0.5f), cuadrado);

            // Zona 4: foso sobre la zona mortal (coyote time en el borde, doble salto para cruzarlo).
            Bloque(nivel, "SueloCentral", 22f, 40f, -1f, 0f, suelo, ColorSuelo, cuadrado, GameTags.Suelo);

            // Zona 5: escalones (el 2.º solo se alcanza desde el suelo con doble salto).
            Bloque(nivel, "Escalon1", 26f, 28f, 1.5f, 2f, suelo, ColorSuelo, cuadrado, GameTags.Suelo);
            Bloque(nivel, "Escalon2", 30f, 32f, 3.5f, 4f, suelo, ColorSuelo, cuadrado, GameTags.Suelo);

            // Zona 6: pinchos (DAMAGED, i-frames, escudo) y muñeco (ataque normal y cargado).
            CrearPinchos(nivel, 35f, 36.5f, cuadrado);
            CrearMuneco(nivel, new Vector2(38.5f, 0f), cuadrado);

            // Zona 7: pozo entre paredes (wall slide y wall jump) con salida por la cornisa.
            Bloque(nivel, "SueloFinal", 40f, 52f, -1f, 0f, suelo, ColorSuelo, cuadrado, GameTags.Suelo);
            Bloque(nivel, "ParedIzquierda", 43f, 44f, 0f, 12f, pared, ColorPared, cuadrado);
            Bloque(nivel, "ParedDerecha", 47f, 48f, 0f, 9f, pared, ColorPared, cuadrado);
            Bloque(nivel, "Cornisa", 48f, 52f, 8.5f, 9f, suelo, ColorSuelo, cuadrado, GameTags.Suelo);

            // Zona mortal bajo todo el nivel.
            var zonaMortal = Bloque(nivel, "ZonaMortal", -30f, 80f, -12f, -10f, 0, Color.clear, null);
            zonaMortal.GetComponent<BoxCollider2D>().isTrigger = true;
            zonaMortal.AddComponent<DeadZone>();

            // Jugador.
            var aparicion = new GameObject("PuntoDeAparicion").transform;
            aparicion.position = new Vector3(0f, 0.05f, 0f);

            var jugador = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            jugador.transform.position = aparicion.position;
            Asignar(jugador.GetComponent<PlayerAbilities>(), "desbloqueadasAlInicio", p =>
            {
                // En la escena de prueba se desbloquean todas para poder probarlas.
                var todas = (HABILIDADES[])System.Enum.GetValues(typeof(HABILIDADES));
                p.arraySize = todas.Length;
                for (var i = 0; i < todas.Length; i++)
                {
                    p.GetArrayElementAtIndex(i).enumValueIndex = (int)todas[i];
                }
            });

            // Cámara.
            var camara = Object.FindAnyObjectByType<Camera>();
            if (camara == null)
            {
                camara = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
                camara.orthographic = true;
            }

            camara.backgroundColor = Hex("1A0800");
            camara.transform.position = new Vector3(0f, 1.5f, -10f);
            var seguimiento = camara.GetComponent<CameraMovement>();
            if (seguimiento == null)
            {
                seguimiento = camara.gameObject.AddComponent<CameraMovement>();
            }

            Asignar(seguimiento, "objetivo", p => p.objectReferenceValue = jugador.transform);
            Asignar(seguimiento, "minimo", p => p.vector2Value = new Vector2(-24f, -6f));
            Asignar(seguimiento, "maximo", p => p.vector2Value = new Vector2(52f, 16f));

            // Reaparición provisional (Fase 6: CheckpointManager).
            var reaparicion = new GameObject("Reaparicion").AddComponent<ReaparicionSimple>();
            Asignar(reaparicion, "jugador", p => p.objectReferenceValue = jugador.GetComponent<PlayerController>());
            Asignar(reaparicion, "punto", p => p.objectReferenceValue = aparicion);
            Asignar(reaparicion, "camara", p => p.objectReferenceValue = seguimiento);

            // Panel de depuración con lo que hay que probar en cada zona.
            var depuracion = new GameObject("Depuracion").AddComponent<DepuracionJugador>();
            Asignar(depuracion, "jugador", p => p.objectReferenceValue = jugador.GetComponent<PlayerController>());
            Asignar(depuracion, "modo", p => p.objectReferenceValue = jugador.GetComponent<PlayerModoMovimiento>());
            Asignar(depuracion, "notas", p => p.stringValue =
                "<b>Circuito</b> (de izquierda a derecha)\n" +
                "Agua: lento y flotante; el dash solo sirve con Dash Acuático.\n" +
                "Techo bajo: agáchate (C) para pasar; no te levantas debajo.\n" +
                "x = 6 a 10: dos suelos solapados; 'Suelo' debe decir 2 contactos y no parpadear.\n" +
                "Hielo: cuesta arrancar y frenar.\n" +
                "Foso: salta tarde desde el borde (coyote time) o usa el doble salto.\n" +
                "Escalones: el alto requiere doble salto desde el suelo.\n" +
                "Pinchos: daño, retroceso y parpadeo; el escudo absorbe el primer golpe.\n" +
                "Muñeco: clic = ataque normal; mantén y suelta = ataque cargado.\n" +
                "Pozo: desliza por la pared y encadena wall jumps hasta la cornisa.");

            CrearHud(jugador.GetComponent<PlayerHealth>());

            EditorSceneManager.MarkSceneDirty(escena);
            var ok = EditorSceneManager.SaveScene(escena, RutaEscena);
            if (ok)
            {
                Debug.Log($"[Fragmentum] Escena de prueba creada en {RutaEscena}. Dale a Play.");
            }

            return ok;
        }

        // --- Tilemap como suelo ---

        /// <summary>
        /// Deja un Tilemap listo para que el jugador lo pise: capa «suelo», TilemapCollider2D
        /// fusionado en un CompositeCollider2D y cuerpo estático. Se puede deshacer (Ctrl+Z).
        /// </summary>
        public static void PrepararTilemapComoSuelo(GameObject objeto)
        {
            AsegurarConfiguracion();
            Undo.RegisterFullObjectHierarchyUndo(objeto, "Preparar tilemap como suelo");

            objeto.layer = Capa(Capas.Suelo);
            objeto.tag = GameTags.Suelo;

            var colisionTiles = objeto.GetComponent<TilemapCollider2D>();
            if (colisionTiles == null)
            {
                colisionTiles = Undo.AddComponent<TilemapCollider2D>(objeto);
            }

            var cuerpo = objeto.GetComponent<Rigidbody2D>();
            if (cuerpo == null)
            {
                cuerpo = Undo.AddComponent<Rigidbody2D>(objeto);
            }

            cuerpo.bodyType = RigidbodyType2D.Static;

            var compuesto = objeto.GetComponent<CompositeCollider2D>();
            if (compuesto == null)
            {
                compuesto = Undo.AddComponent<CompositeCollider2D>(objeto);
            }

            compuesto.geometryType = CompositeCollider2D.GeometryType.Polygons;
            colisionTiles.compositeOperation = Collider2D.CompositeOperation.Merge;

            EditorSceneManager.MarkSceneDirty(objeto.scene);
            Debug.Log($"[Fragmentum] '{objeto.name}' preparado como suelo (capa '{Capas.Suelo}', collider compuesto, cuerpo estático).", objeto);
        }

        // --- Piezas de la escena ---

        private static GameObject Bloque(Transform padre, string nombre, float xMin, float xMax, float yMin, float yMax,
            int capa, Color color, Sprite sprite, string tag = null)
        {
            var objeto = new GameObject(nombre) { layer = capa };
            if (!string.IsNullOrEmpty(tag))
            {
                objeto.tag = tag;
            }

            objeto.transform.SetParent(padre, false);
            objeto.transform.position = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
            objeto.transform.localScale = new Vector3(xMax - xMin, yMax - yMin, 1f);

            if (sprite != null)
            {
                var renderer2d = objeto.AddComponent<SpriteRenderer>();
                renderer2d.sprite = sprite;
                renderer2d.color = color;
            }

            var caja = objeto.AddComponent<BoxCollider2D>();
            caja.size = Vector2.one;
            caja.offset = Vector2.zero;
            return objeto;
        }

        private static void Zona(Transform padre, string nombre, float xMin, float xMax, float yMin, float yMax,
            ZonaModificadorMovimiento.TIPOS tipo, Color color, Sprite sprite)
        {
            var objeto = Bloque(padre, nombre, xMin, xMax, yMin, yMax, 0, color, sprite);
            objeto.GetComponent<BoxCollider2D>().isTrigger = true;

            var renderer2d = objeto.GetComponent<SpriteRenderer>();
            if (renderer2d != null)
            {
                renderer2d.sortingOrder = 20; // por delante del jugador, semitransparente
            }

            var zona = objeto.AddComponent<ZonaModificadorMovimiento>();
            Asignar(zona, "tipo", p => p.enumValueIndex = (int)tipo);
        }

        private static void CrearPinchos(Transform padre, float xMin, float xMax, Sprite sprite)
        {
            var pinchos = Bloque(padre, "Pinchos", xMin, xMax, 0f, 0.4f, Capa(Capas.EnemyWeapon), ColorPeligro, sprite);
            pinchos.GetComponent<BoxCollider2D>().isTrigger = true;

            var hitbox = pinchos.AddComponent<Hitbox>();
            Asignar(hitbox, "danio", p => p.intValue = 1);
            Asignar(hitbox, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
            Asignar(hitbox, "golpearUnaVezPorActivacion", p => p.boolValue = false);
        }

        private static void CrearMuneco(Transform padre, Vector2 pies, Sprite sprite)
        {
            var muneco = new GameObject("MunecoDePrueba") { layer = Capa(Capas.Enemy), tag = GameTags.Enemy };
            muneco.transform.SetParent(padre, false);
            muneco.transform.position = pies;

            var caja = muneco.AddComponent<BoxCollider2D>();
            caja.isTrigger = true;
            caja.size = new Vector2(1f, 2f);
            caja.offset = new Vector2(0f, 1f);

            var objetoSprite = new GameObject("Sprite");
            objetoSprite.transform.SetParent(muneco.transform, false);
            objetoSprite.transform.localPosition = new Vector3(0f, 1f, 0f);
            objetoSprite.transform.localScale = new Vector3(1f, 2f, 1f);
            var renderer2d = objetoSprite.AddComponent<SpriteRenderer>();
            renderer2d.sprite = sprite;
            renderer2d.color = Hex("C8A44A");

            var vida = muneco.AddComponent<Health>();
            Asignar(vida, "vidaMaxima", p => p.intValue = 10);
            Asignar(vida, "invulnerabilidadTrasGolpe", p => p.floatValue = 0.1f);

            var objetivo = muneco.AddComponent<ObjetivoDePrueba>();
            Asignar(objetivo, "sprite", p => p.objectReferenceValue = renderer2d);
        }

        private static void CrearHud(Health vidaJugador)
        {
            var objetoCanvas = new GameObject("HUD");
            var canvas = objetoCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var escalador = objetoCanvas.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);

            var spriteUi = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var fondo = new GameObject("BarraVida", typeof(RectTransform)).AddComponent<Image>();
            fondo.transform.SetParent(objetoCanvas.transform, false);
            fondo.sprite = spriteUi;
            fondo.type = Image.Type.Sliced;
            fondo.color = new Color(0f, 0f, 0f, 0.6f);
            var rectFondo = fondo.rectTransform;
            rectFondo.anchorMin = rectFondo.anchorMax = rectFondo.pivot = new Vector2(0f, 1f);
            rectFondo.anchoredPosition = new Vector2(20f, -20f);
            rectFondo.sizeDelta = new Vector2(320f, 26f);

            var relleno = new GameObject("Relleno", typeof(RectTransform)).AddComponent<Image>();
            relleno.transform.SetParent(fondo.transform, false);
            relleno.sprite = spriteUi;
            relleno.type = Image.Type.Filled;
            relleno.fillMethod = Image.FillMethod.Horizontal;
            relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
            relleno.color = ColorSolapado;
            var rectRelleno = relleno.rectTransform;
            rectRelleno.anchorMin = Vector2.zero;
            rectRelleno.anchorMax = Vector2.one;
            rectRelleno.offsetMin = new Vector2(3f, 3f);
            rectRelleno.offsetMax = new Vector2(-3f, -3f);

            var barra = fondo.gameObject.AddComponent<LifeBar>();
            Asignar(barra, "vida", p => p.objectReferenceValue = vidaJugador);
            Asignar(barra, "relleno", p => p.objectReferenceValue = relleno);
        }

        // --- Utilidades ---

        private static void AsegurarConfiguracion()
        {
            foreach (var capa in Capas.Propias)
            {
                if (LayerMask.NameToLayer(capa) < 0)
                {
                    Debug.Log("[Fragmentum] Faltan capas del proyecto; se ejecuta Configurar proyecto.");
                    ConfiguradorProyecto.ConfigurarTodo();
                    return;
                }
            }
        }

        private static int Capa(string nombre)
        {
            var indice = LayerMask.NameToLayer(nombre);
            if (indice < 0)
            {
                Debug.LogWarning($"[Fragmentum] La capa '{nombre}' no existe; se usa Default. Ejecuta Fragmentum → Configurar proyecto.");
                return 0;
            }

            return indice;
        }

        private static PhysicsMaterial2D MaterialSinFriccion()
        {
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(RutaMaterialSinFriccion);
            if (material != null)
            {
                return material;
            }

            AsegurarCarpeta("Assets/Settings/Fisica");
            material = new PhysicsMaterial2D("JugadorSinFriccion") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, RutaMaterialSinFriccion);
            return material;
        }

        private static Sprite CargarSprite(string ruta)
        {
            foreach (var objeto in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                if (objeto is Sprite sprite)
                {
                    return sprite;
                }
            }

            Debug.LogWarning($"[Fragmentum] No se encontró el sprite base {ruta}; se usará un placeholder en tiempo de ejecución.");
            return null;
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta))
            {
                return;
            }

            var partes = ruta.Split('/');
            var actual = partes[0];
            for (var i = 1; i < partes.Length; i++)
            {
                var siguiente = $"{actual}/{partes[i]}";
                if (!AssetDatabase.IsValidFolder(siguiente))
                {
                    AssetDatabase.CreateFolder(actual, partes[i]);
                }

                actual = siguiente;
            }
        }

        /// <summary>Asigna un campo serializado (también privado) y avisa si no existe.</summary>
        private static void Asignar(Object objeto, string campo, System.Action<SerializedProperty> asignar)
        {
            if (objeto == null)
            {
                return;
            }

            var serializado = new SerializedObject(objeto);
            var propiedad = serializado.FindProperty(campo);
            if (propiedad == null)
            {
                Debug.LogWarning($"[Fragmentum] {objeto.GetType().Name} no tiene el campo serializado '{campo}'.");
                return;
            }

            asignar(propiedad);
            serializado.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Color Hex(string rgb) =>
            ColorUtility.TryParseHtmlString("#" + rgb, out var color) ? color : Color.magenta;
    }
}
