using System;
using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.Narrative;
using Fragmentum.Player;
using Fragmentum.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Fase 5: genera la progresión desde el Anexo A: emociones, habilidades, fragmentos, jefes,
    /// los 6 LevelData y la LevelDatabase; las escenas de menú y selector; el prefab de
    /// sistemas persistentes; y Build Settings a partir de la base de datos.
    ///
    /// Los datos y las escenas de navegación solo se crean si faltan: si los has retocado, no se
    /// pisan. «Restablecer» y «Crear escenas» los regeneran a propósito.
    /// </summary>
    public static class GeneradorProgresion
    {
        public const string RutaBaseDeDatos = "Assets/Data/Niveles/LevelDatabase.asset";
        public const string RutaEscenaMenu = "Assets/Scenes/Menu_Inicial.unity";
        public const string RutaEscenaSelector = "Assets/Scenes/Niveles.unity";
        public const string RutaEscenaNivel1 = "Assets/Scenes/Nivel1.unity";
        public const string RutaSistemas = "Assets/Resources/Sistemas.prefab";

        // --- Menús ---

        [MenuItem("Fragmentum/Progresión/Generar datos de niveles (Anexo A)", priority = 120)]
        public static void MenuGenerarDatos() => GenerarDatos(false);

        [MenuItem("Fragmentum/Progresión/Restablecer datos de niveles al Anexo A", priority = 121)]
        public static void MenuRestablecer()
        {
            if (EditorUtility.DisplayDialog("Fragmentum", "Se sobrescribirán emociones, habilidades, fragmentos, jefes y niveles con los valores del diseño. ¿Continuar?", "Restablecer", "Cancelar"))
            {
                GenerarDatos(true);
            }
        }

        [MenuItem("Fragmentum/Progresión/Crear escenas de menú y selector", priority = 122)]
        public static void MenuCrearEscenas()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                CrearEscenaMenu(true);
                CrearEscenaSelector(true);
                ConfigurarBuildSettings(GenerarDatos(false));
            }
        }

        /// <summary>
        /// Regenera sin preguntar las escenas de menú y selector (conservan su GUID). Para el
        /// modo batch: <c>-executeMethod Fragmentum.EditorTools.GeneradorProgresion.RegenerarEscenasNavegacion</c>.
        /// </summary>
        public static void RegenerarEscenasNavegacion()
        {
            CrearEscenaMenu(false);
            CrearEscenaSelector(false);
            ConfigurarBuildSettings(GenerarDatos(false));
        }

        [MenuItem("Fragmentum/Progresión/Configurar Build Settings desde la LevelDatabase", priority = 123)]
        public static void MenuBuildSettings() => ConfigurarBuildSettings(AssetDatabase.LoadAssetAtPath<LevelDatabase>(RutaBaseDeDatos));

        [MenuItem("Fragmentum/Progresión/Validar LevelDatabase", priority = 124)]
        public static void MenuValidar()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(RutaBaseDeDatos);
            if (bd == null)
            {
                Debug.LogWarning($"[Fragmentum] No existe {RutaBaseDeDatos}.");
                return;
            }

            var problemas = bd.Validar();
            Debug.Log(problemas.Count == 0
                ? "[Fragmentum] LevelDatabase correcta."
                : "[Fragmentum] Problemas en la LevelDatabase:\n- " + string.Join("\n- ", problemas));
            Selection.activeObject = bd;
        }

        /// <summary>
        /// Todo lo de la progresión, para la regeneración completa: datos y escenas de navegación
        /// (solo si faltan), prefab de sistemas y Build Settings. Devuelve si quedó válido.
        /// </summary>
        public static bool Construir()
        {
            // Primero las escenas: la base de datos las referencia.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscenaMenu) == null) CrearEscenaMenu(false);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscenaSelector) == null) CrearEscenaSelector(false);
            var bd = GenerarDatos(false);

            // Cinemáticas, diálogos y registro emocional (Fase 8): antes del prefab, que los usa.
            GeneradorNarrativa.GenerarDatos(false);
            ConfigurarBuildSettings(bd);
            CrearPrefabSistemas(bd);

            var problemas = bd.Validar();
            foreach (var problema in problemas)
            {
                Debug.LogError($"[Fragmentum] LevelDatabase: {problema}");
            }

            return problemas.Count == 0;
        }

        // --- Datos del Anexo A ---

        public static LevelDatabase GenerarDatos(bool sobrescribir)
        {
            var ira = Asset<EmocionData>("Emociones/Ira", sobrescribir, e => Emocion(e, EMOCIONES.IRA, "Ira", "Impulsividad, rabia reprimida", "8B1A1A"));
            var tristeza = Asset<EmocionData>("Emociones/Tristeza", sobrescribir, e => Emocion(e, EMOCIONES.TRISTEZA, "Tristeza", "Pérdida, melancolía", "4A7FA5"));
            var miedo = Asset<EmocionData>("Emociones/Miedo", sobrescribir, e => Emocion(e, EMOCIONES.MIEDO, "Miedo", "Ansiedad, inseguridad", "2D1B4E"));
            var culpa = Asset<EmocionData>("Emociones/Culpa", sobrescribir, e => Emocion(e, EMOCIONES.CULPA, "Culpa", "Responsabilidad, remordimiento", "8B3333"));
            var esperanza = Asset<EmocionData>("Emociones/Esperanza", sobrescribir, e => Emocion(e, EMOCIONES.ESPERANZA, "Esperanza", "Aceptación, reconciliación", "C8A44A"));
            var integracion = Asset<EmocionData>("Emociones/Integracion", sobrescribir, e => Emocion(e, EMOCIONES.INTEGRACION, "Integración", "Dualidad que se resuelve", "F0EED8"));

            var cargado = Asset<HabilidadData>("Habilidades/AtaqueCargado", sobrescribir, h => Habilidad(h, HABILIDADES.ATAQUE_CARGADO, "Ataque Cargado",
                "Mantén el ataque y suéltalo con la carga completa: doble daño. Detona los barriles explosivos.", 0f, 0f));
            var dash = Asset<HabilidadData>("Habilidades/DashAcuatico", sobrescribir, h => Habilidad(h, HABILIDADES.DASH_ACUATICO, "Dash Acuático",
                "El dash también funciona bajo el agua.", 0.18f, 0.6f));
            var invisibilidad = Asset<HabilidadData>("Habilidades/InvisibilidadParcial", sobrescribir, h => Habilidad(h, HABILIDADES.INVISIBILIDAD_PARCIAL, "Invisibilidad Parcial",
                "Durante 3 s, los enemigos que ven no te detectan (los que oyen, sí).", 3f, 8f));
            var defensa = Asset<HabilidadData>("Habilidades/DefensaReforzada", sobrescribir, h => Habilidad(h, HABILIDADES.DEFENSA_REFORZADA, "Defensa Reforzada",
                "Un escudo absorbe un golpe cada 15 s.", 0f, 15f));

            var fIra = Asset<FragmentoData>("Fragmentos/Ira", sobrescribir, f => Fragmento(f, EMOCIONES.IRA, "Fragmento de la Ira", cargado));
            var fTristeza = Asset<FragmentoData>("Fragmentos/Tristeza", sobrescribir, f => Fragmento(f, EMOCIONES.TRISTEZA, "Fragmento de la Tristeza", dash));
            var fMiedo = Asset<FragmentoData>("Fragmentos/Miedo", sobrescribir, f => Fragmento(f, EMOCIONES.MIEDO, "Fragmento del Miedo", invisibilidad));
            var fCulpa = Asset<FragmentoData>("Fragmentos/Culpa", sobrescribir, f => Fragmento(f, EMOCIONES.CULPA, "Fragmento de la Culpa", defensa));
            var fEsperanza = Asset<FragmentoData>("Fragmentos/Esperanza", sobrescribir, f => Fragmento(f, EMOCIONES.ESPERANZA, "Fragmento de la Esperanza", null));

            var centinela = Asset<JefeData>("Jefes/CentinelaArdiente", sobrescribir, j => Jefe(j, "Centinela Ardiente", ira));
            var doncella = Asset<JefeData>("Jefes/DoncellaLamento", sobrescribir, j => Jefe(j, "Doncella del Lamento", tristeza));
            var espectro = Asset<JefeData>("Jefes/EspectroDelPavor", sobrescribir, j => Jefe(j, "Espectro del Pavor", miedo));
            var juez = Asset<JefeData>("Jefes/JuezSilente", sobrescribir, j => Jefe(j, "Juez Silente", culpa));
            var soldado = Asset<JefeData>("Jefes/VerdaderoSoldado", sobrescribir, j => Jefe(j, "El Verdadero Soldado", integracion));

            var niveles = new List<LevelData>
            {
                Asset<LevelData>("Niveles/L1_Ira", sobrescribir, n => Nivel(n, 1, RutaEscenaNivel1, "Fortaleza Volcánica", ira, 2,
                    "Impulsividad, rabia reprimida. El jugador siente urgencia y presión constante.",
                    new[] { "8B1A1A", "CC4400", "1A0800", "5C4033" }, centinela, fIra, cargado, new ReglasNivel(),
                    Seccion("Sección 1", 0, 25, "Terreno abierto. Plataformas inestables. 3 Soldados de lava y 1 torreta de fuego fija."),
                    Seccion("Sección 2", 25, 55, "Plataformeo sobre lava y plataformas de combustión. 4 Soldados de lava y el Golem de Ceniza. Checkpoint en x = 40."),
                    Seccion("Arena del jefe", 55, 80, "Sala cerrada de 60 × 15 u, plataformas a 3 alturas y lava en el suelo."))),

                Asset<LevelData>("Niveles/L2_Tristeza", sobrescribir, n => Nivel(n, 2, null, "Ciudad Sumergida", tristeza, 3,
                    "Pérdida, melancolía. El nivel es deliberadamente lento y pesado.",
                    new[] { "1E3A5F", "4A7FA5", "2C3E50", "E8EEF4" }, doncella, fTristeza, dash, new ReglasNivel(),
                    Seccion("Sección 1", 0, 30, "Sobre el hielo: suelo resbaladizo y plataformas flotantes. 3 Lamentos y 2 Esculturas Vivientes."),
                    Seccion("Sección 2", 30, 60, "Bajo el agua, con oxígeno limitado. 4 Medusas de Hielo."),
                    Seccion("Arena del jefe", 60, 90, "Superficie helada sobre agua."))),

                Asset<LevelData>("Niveles/L3_Miedo", sobrescribir, n => Nivel(n, 3, null, "Bosque Tenebroso", miedo, 4,
                    "Ansiedad, inseguridad. Explota la desorientación y la incertidumbre.",
                    new[] { "0A0A0F", "2D1B4E", "1A2E1A", "3A3A4A" }, espectro, fMiedo, invisibilidad,
                    new ReglasNivel { sigilo = true, oscuridad = true, radioLuzJugador = 3f },
                    Seccion("Sección 1", 0, 20, "Luz moderada y tutorial de antorchas. 2 Espectros y 3 trampas ocultas."),
                    Seccion("Sección 2", 20, 50, "Oscuridad completa: laberinto con 2 rutas. 3 Centinelas Ciegos y 4 Espectros."),
                    Seccion("Arena del jefe", 50, 70, "Oscuridad total; solo la luz del jugador."))),

                Asset<LevelData>("Niveles/L4_Culpa", sobrescribir, n => Nivel(n, 4, null, "Campos de Batalla Arrasados", culpa, 3,
                    "Responsabilidad, remordimiento. Confronta al jugador con consecuencias.",
                    new[] { "3C2E1E", "5C3D1E", "4A5C2E", "8B3333" }, juez, fCulpa, defensa, new ReglasNivel { agacharse = true },
                    Seccion("Sección 1", 0, 25, "Campo abierto: Penitentes y cobertura. 1 memoria sin riesgo."),
                    Seccion("Sección 2", 25, 60, "Ruinas con 2 rutas: alta (Artilleros y barriles) o baja (Penitentes y 2 memorias)."),
                    Seccion("Arena del jefe", 60, 85, "Plaza central con las memorias proyectadas en el suelo."))),

                Asset<LevelData>("Niveles/L5_Esperanza", sobrescribir, n => Nivel(n, 5, null, "Jardín Etéreo", esperanza, 1,
                    "Aceptación, reconciliación. Por primera vez el entorno no es hostil.",
                    new[] { "1A3A2E", "C8A44A", "F0EED8", "7AABCC" }, null, fEsperanza, null, new ReglasNivel(),
                    Seccion("Jardín", 0, 50, "Plataformeo puro en 4 secciones, una por habilidad previa. 3 Guardianes del Jardín."))),

                // La paleta del final no está en el Anexo A (solo «dualidad oscuro/luminoso»): valores provisionales.
                Asset<LevelData>("Niveles/L6_Final", sobrescribir, n => Nivel(n, 6, null, "El Espejo del Origen", integracion, 5,
                    "Conclusión. Combina todas las mecánicas y resuelve el arco de Kael.",
                    new[] { "1A1A1A", "5A5A5A", "F0EED8", "C8A44A" }, soldado, null, null, new ReglasNivel(),
                    Seccion("Entrada", 0, 10, "Cinemática de confrontación (~45 s)."),
                    Seccion("Arena final", 10, 40, "30 × 18 u con plataformas simétricas que se transforman entre fases."))),
            };

            // Interfaz de cada mundo según la maqueta de los menús (Referencias/Menus) y
            // checkpoints previstos por el Anexo A. Son campos de la Fase 6: en los assets que ya
            // existían se completan sin pisar lo que se haya editado a mano.
            Completar(niveles[0], "FF6B35", "la rabia consume al que la niega", 40f);
            Completar(niveles[1], "00D4FF", "el dolor también es parte del yo");
            Completar(niveles[2], "7FE0B0", "lo que ocultas en la sombra crece");
            Completar(niveles[3], "C9A567", "el peso del ayer no define el mañana");
            Completar(niveles[4], "FFE4A0", "respira. el camino continúa");
            Completar(niveles[5], "B06EFF", "no destruir. integrar.");

            var bd = Asset<LevelDatabase>("Niveles/LevelDatabase", sobrescribir, b =>
            {
                b.escenaMenu = Escena(RutaEscenaMenu);
                b.escenaSelector = Escena(RutaEscenaSelector);
                b.niveles = niveles;
            });

            // Las escenas de navegación pueden no existir aún cuando se crea la base de datos.
            if (!bd.escenaMenu.Asignada || !bd.escenaSelector.Asignada)
            {
                bd.escenaMenu = Escena(RutaEscenaMenu);
                bd.escenaSelector = Escena(RutaEscenaSelector);
                EditorUtility.SetDirty(bd);
            }

            AssetDatabase.SaveAssets();
            return bd;
        }

        private static T Asset<T>(string nombre, bool sobrescribir, Action<T> rellenar) where T : ScriptableObject
        {
            var ruta = $"Assets/Data/{nombre}.asset";
            AsegurarCarpeta(System.IO.Path.GetDirectoryName(ruta)?.Replace('\\', '/'));

            var asset = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                rellenar(asset);
                AssetDatabase.CreateAsset(asset, ruta);
            }
            else if (sobrescribir)
            {
                rellenar(asset);
                EditorUtility.SetDirty(asset);
            }

            return asset;
        }

        private static void Emocion(EmocionData e, EMOCIONES id, string nombre, string descriptor, string color)
        {
            e.id = id;
            e.nombreCanonico = nombre;
            e.descriptor = descriptor;
            e.color = Hex(color);
        }

        private static void Habilidad(HabilidadData h, HABILIDADES id, string nombre, string descripcion, float duracion, float cooldown)
        {
            h.id = id;
            h.nombre = nombre;
            h.descripcion = descripcion;
            h.duracion = duracion;
            h.cooldown = cooldown;
        }

        // La descripción es neutra a propósito: los textos narrativos son del documento de diseño.
        private static void Fragmento(FragmentoData f, EMOCIONES id, string nombre, HabilidadData habilidad)
        {
            f.id = id;
            f.nombre = nombre;
            f.descripcion = habilidad != null
                ? $"Se integra al superar el mundo. Desbloquea {habilidad.nombre}."
                : "Se integra al superar el Jardín: reúne los fragmentos anteriores antes del final.";
            f.habilidad = habilidad;
        }

        private static void Jefe(JefeData j, string nombre, EmocionData emocion)
        {
            j.nombreCanonico = nombre;
            j.emocion = emocion;
            j.descripcion = $"Arquitecto del Alma del mundo de la {emocion.nombreCanonico}.";
        }

        private static SeccionNivel Seccion(string nombre, float desde, float hasta, string contenido) =>
            new SeccionNivel { nombre = nombre, desdeX = desde, hastaX = hasta, contenido = contenido };

        private static void Nivel(LevelData n, int id, string rutaEscena, string mundo, EmocionData emocion, int dificultad,
            string sinopsis, string[] paleta, JefeData jefe, FragmentoData fragmento, HabilidadData habilidad,
            ReglasNivel reglas, params SeccionNivel[] secciones)
        {
            n.id = id;
            n.escena = rutaEscena != null ? Escena(rutaEscena) : new ReferenciaEscena();
            n.nombreMundo = mundo;
            n.emocion = emocion;
            n.dificultad = dificultad;
            n.sinopsis = sinopsis;
            n.paleta = Array.ConvertAll(paleta, Hex);
            n.jefe = jefe;
            n.fragmento = fragmento;
            n.habilidadOtorgada = habilidad;
            n.reglas = reglas;
            n.secciones = new List<SeccionNivel>(secciones);

            // Los rellena Completar con los valores de diseño.
            n.acento = default;
            n.cita = null;
            n.puntosDeControl = new List<float>();
        }

        private static void Completar(LevelData n, string acento, string cita, params float[] puntosDeControl)
        {
            var cambio = false;
            if (n.acento.a <= 0f)
            {
                n.acento = Hex(acento);
                cambio = true;
            }

            if (string.IsNullOrEmpty(n.cita))
            {
                n.cita = cita;
                cambio = true;
            }

            if ((n.puntosDeControl == null || n.puntosDeControl.Count == 0) && puntosDeControl.Length > 0)
            {
                n.puntosDeControl = new List<float>(puntosDeControl);
                cambio = true;
            }

            if (cambio)
            {
                EditorUtility.SetDirty(n);
            }
        }

        private static ReferenciaEscena Escena(string ruta)
        {
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta);
            return asset != null ? ReferenciaEscena.Desde(asset) : new ReferenciaEscena();
        }

        // --- Build Settings ---

        /// <summary>Build Settings = menú, selector y los niveles con escena, en ese orden.</summary>
        public static void ConfigurarBuildSettings(LevelDatabase bd)
        {
            if (bd == null)
            {
                Debug.LogWarning("[Fragmentum] No hay LevelDatabase para configurar Build Settings.");
                return;
            }

            var rutas = new List<string>();
            void Agregar(ReferenciaEscena referencia)
            {
                if (referencia != null)
                {
                    referencia.Refrescar();
                    if (referencia.Asignada && !rutas.Contains(referencia.Ruta)) rutas.Add(referencia.Ruta);
                }
            }

            Agregar(bd.escenaMenu);
            Agregar(bd.escenaSelector);
            foreach (var nivel in bd.niveles)
            {
                if (nivel != null) Agregar(nivel.escena);
            }

            EditorBuildSettings.scenes = rutas.ConvertAll(r => new EditorBuildSettingsScene(r, true)).ToArray();
            EditorUtility.SetDirty(bd);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Fragmentum] Build Settings: {string.Join(", ", rutas.ConvertAll(System.IO.Path.GetFileNameWithoutExtension))}.");
        }

        /// <summary>Añade una escena a Build Settings sin tocar las demás.</summary>
        public static void AnadirABuild(string ruta)
        {
            if (string.IsNullOrEmpty(ruta)) return;
            var escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            escenas.RemoveAll(e => e.path == ruta);
            escenas.Add(new EditorBuildSettingsScene(ruta, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }

        // --- Sistemas persistentes ---

        public static GameObject CrearPrefabSistemas(LevelDatabase bd)
        {
            AsegurarCarpeta("Assets/Resources");
            var raiz = new GameObject(Bootstrap.RutaRecurso);
            try
            {
                raiz.AddComponent<EconomyManager>();
                var gestor = raiz.AddComponent<GameManager>();
                Asignar(gestor, "baseDeDatos", p => p.objectReferenceValue = bd);
                raiz.AddComponent<SceneLoader>();
                raiz.AddComponent<GameStateMachine>();
                raiz.AddComponent<DialogueManager>();
                var interfaz = raiz.AddComponent<UIManager>();
                Asignar(interfaz, "acciones", p => p.objectReferenceValue = AssetDatabase.LoadMainAssetAtPath(ConstructorJugador.RutaAccionesInput));
                Asignar(interfaz, "registro", p => p.objectReferenceValue = GeneradorNarrativa.CargarConfiguracionRegistro());
                var camara = ConstructorCamaraCinematica.Prefab != null ? ConstructorCamaraCinematica.Prefab : ConstructorCamaraCinematica.CrearPrefab();
                Asignar(interfaz, "camaraCinematica", p => p.objectReferenceValue = camara != null ? camara.GetComponent<CamaraCinematica>() : null);
                raiz.AddComponent<AtajosDepuracion>();
                return PrefabUtility.SaveAsPrefabAsset(raiz, RutaSistemas);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        // --- Escenas de navegación ---

        public static void CrearEscenaMenu(bool preguntar)
        {
            var creada = CrearEscenaDesdePlantilla(RutaEscenaMenu, preguntar);
            if (creada == null) return;

            var lienzo = Lienzo("MenuPrincipal");
            var titulo = FabricaUI.Texto(lienzo, "Titulo", "FRAGMENTUM", 110, Hex("F0EED8"));
            Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1200f, 140f));
            var subtitulo = FabricaUI.Texto(lienzo, "Subtitulo", "Ecos de la Identidad", 44, Hex("C8A44A"));
            Colocar(subtitulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(1200f, 60f));

            var columna = FabricaUI.Objeto("Botones", lienzo);
            Colocar(columna, new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(520f, 500f));
            FabricaUI.Columna(columna, 18f);
            var continuar = FabricaUI.Boton(columna, "Continuar", "Continuar", Hex("8B1A1A"), 78f);
            var nueva = FabricaUI.Boton(columna, "NuevaPartida", "Nueva partida", Hex("5C4033"), 78f);
            var selector = FabricaUI.Boton(columna, "Selector", "Seleccionar mundo", Hex("2C3E50"), 78f);
            var yo = FabricaUI.Boton(columna, "YoReconstruido", "Yo reconstruido", Hex("2D1B4E"), 78f);
            var salir = FabricaUI.Boton(columna, "Salir", "Salir", Hex("3A3A4A"), 78f);

            var progreso = FabricaUI.Texto(lienzo, "Progreso", string.Empty, 26, new Color(1f, 1f, 1f, 0.7f));
            Colocar(progreso.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1200f, 40f));

            var menu = lienzo.gameObject.AddComponent<MainMenu>();
            Asignar(menu, "botonContinuar", p => p.objectReferenceValue = continuar);
            Asignar(menu, "botonNuevaPartida", p => p.objectReferenceValue = nueva);
            Asignar(menu, "botonSelector", p => p.objectReferenceValue = selector);
            Asignar(menu, "botonYoReconstruido", p => p.objectReferenceValue = yo);
            Asignar(menu, "botonSalir", p => p.objectReferenceValue = salir);
            Asignar(menu, "textoProgreso", p => p.objectReferenceValue = progreso);

            Guardar(creada.Value, RutaEscenaMenu);
        }

        public static void CrearEscenaSelector(bool preguntar)
        {
            var creada = CrearEscenaDesdePlantilla(RutaEscenaSelector, preguntar);
            if (creada == null) return;

            var lienzo = Lienzo("Selector");
            var titulo = FabricaUI.Texto(lienzo, "Titulo", "Selecciona un mundo", 64, Hex("F0EED8"));
            Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1200f, 90f));

            var contenedor = FabricaUI.Objeto("Niveles", lienzo);
            Colocar(contenedor, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(900f, 640f));
            FabricaUI.Columna(contenedor, 14f);

            var volver = FabricaUI.Boton(lienzo, "Volver", "Volver", Hex("3A3A4A"), 70f);
            Colocar((RectTransform)volver.transform, new Vector2(0f, 0f), new Vector2(170f, 70f), new Vector2(260f, 70f));

            var monedas = FabricaUI.Texto(lienzo, "Monedas", string.Empty, 30, Hex("C8A44A"), TextAnchor.MiddleRight);
            Colocar(monedas.rectTransform, new Vector2(1f, 1f), new Vector2(-220f, -60f), new Vector2(380f, 50f));
            monedas.gameObject.AddComponent<ContadorMonedas>();

            var selector = lienzo.gameObject.AddComponent<SelectorNivel>();
            Asignar(selector, "contenedor", p => p.objectReferenceValue = contenedor);
            Asignar(selector, "botonVolver", p => p.objectReferenceValue = volver);

            Guardar(creada.Value, RutaEscenaSelector);
        }

        private static RectTransform Lienzo(string nombre)
        {
            var camara = Object.FindAnyObjectByType<Camera>();
            if (camara != null)
            {
                camara.clearFlags = CameraClearFlags.SolidColor;
                camara.backgroundColor = Hex("140A06");
            }

            var sistemaEventos = new GameObject("EventSystem");
            sistemaEventos.AddComponent<EventSystem>();
            sistemaEventos.AddComponent<InputSystemUIInputModule>();

            var objeto = new GameObject(nombre, typeof(RectTransform));
            var canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var escalador = objeto.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;
            objeto.AddComponent<GraphicRaycaster>();
            return (RectTransform)objeto.transform;
        }

        private static void Colocar(RectTransform rect, Vector2 ancla, Vector2 posicion, Vector2 tamano)
        {
            rect.anchorMin = rect.anchorMax = ancla;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
        }

        private static void Guardar(UnityEngine.SceneManagement.Scene escena, string ruta)
        {
            CalcularLayout(escena);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena, ruta);
            Debug.Log($"[Fragmentum] Escena creada: {ruta}.");
        }

        /// <summary>
        /// Calcula los layouts de uGUI antes de guardar. En modo batch no hay repintado y los
        /// hijos de las columnas se guardaban con valores por defecto; al abrir la escena en el
        /// Editor, el layout los recalculaba y la escena aparecía modificada sin haberla tocado.
        /// </summary>
        public static void CalcularLayout(UnityEngine.SceneManagement.Scene escena)
        {
            // Sobre cada grupo y no sobre el lienzo: el recálculo se salta los subárboles cuya
            // raíz no controla ningún layout, y el lienzo no lo hace.
            foreach (var raiz in escena.GetRootGameObjects())
            {
                foreach (var grupo in raiz.GetComponentsInChildren<LayoutGroup>(true))
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)grupo.transform);
                }
            }
        }
    }
}
