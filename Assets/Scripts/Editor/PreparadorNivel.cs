using System.Collections.Generic;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using Fragmentum.Levels.Escenario;
using Fragmentum.Levels.Mecanicas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Fragmentum.EditorTools.UtilidadesEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Fase 6: deja una escena de nivel jugable de principio a fin con lo que dice su
    /// <see cref="LevelData"/>. Solo <b>añade lo que falta</b> y nunca toca lo que ya hay:
    /// - el <see cref="LevelManager"/> (con el prefab del jugador) y el punto de inicio;
    /// - los checkpoints previstos por el diseño (o uno por cada cambio de sección);
    /// - la salida al final de la última sección;
    /// - la zona mortal bajo el nivel y el seguimiento de la cámara;
    /// - si la escena todavía no tiene suelo que colisione, un <b>suelo provisional</b> con fosos
    ///   y algunos enemigos del mundo, dentro de un objeto que se borra al pintar el nivel;
    /// - si el nivel tiene jefe, la <b>arena</b> (Fase 7): entrada, puerta y el prefab del jefe;
    /// - en el mundo de la Ira, el <b>fondo con parallax</b> con sus hitos repartidos;
    /// - el Golem de Ceniza con su arte en lugar del provisional;
    /// - en el Nivel 1, el <b>recorrido de diseño</b> (<see cref="DisenoNivel1"/>) en lugar del
    ///   provisional: es lo único que borra, y solo la primera vez (lo provisional y lo que se colocó
    ///   para él: su escenario pintado, el checkpoint, la salida y la arena).
    ///
    /// Es idempotente: ejecutarla otra vez sobre una escena preparada no cambia nada, y la
    /// escena solo se guarda si cambió.
    /// </summary>
    public static class PreparadorNivel
    {
        public const string NombreRaiz = "Nivel";
        public const string NombreProvisional = "Provisional (bórralo al pintar el nivel)";

        /// <summary>El recorrido de diseño de un nivel: sus bloques, mecánicas y enemigos.</summary>
        public const string NombreRecorrido = "Recorrido";

        private const float AlturaSuelo = 0f;
        private const float AlturaPies = 0.05f;

        [MenuItem("Fragmentum/Niveles/Preparar la escena de nivel abierta", priority = 130)]
        public static void MenuPrepararAbierta()
        {
            var escena = SceneManager.GetActiveScene();
            var nivel = NivelDe(escena);
            if (nivel == null)
            {
                EditorUtility.DisplayDialog("Fragmentum",
                    $"La escena '{escena.name}' no es un nivel de la LevelDatabase. Asígnala al campo «escena» de su LevelData (Assets/Data/Niveles) y vuelve a intentarlo.",
                    "Entendido");
                return;
            }

            if (Preparar(escena, nivel))
            {
                EditorSceneManager.MarkSceneDirty(escena);
                Debug.Log($"[Fragmentum] Escena '{escena.name}' preparada para {nivel.Titulo}. Guárdala para conservar los cambios.");
            }
            else
            {
                Debug.Log($"[Fragmentum] La escena '{escena.name}' ya estaba preparada: no se cambió nada.");
            }
        }

        [MenuItem("Fragmentum/Niveles/Preparar todas las escenas de nivel", priority = 131)]
        public static void MenuPrepararTodas()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                PrepararTodas();
            }
        }

        /// <summary>
        /// Abre cada escena de nivel de la LevelDatabase, la prepara y la guarda si cambió. Sirve
        /// en modo batch (lo llama <c>ConstruccionBatch</c>).
        /// </summary>
        public static bool PrepararTodas()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            if (bd == null)
            {
                Debug.LogWarning("[Fragmentum] No hay LevelDatabase: no se preparan escenas de nivel.");
                return false;
            }

            // Se guardan las rutas de los assets y no los objetos: abrir una escena en modo Single
            // descarga los assets sin uso, y una referencia cargada antes (por ejemplo
            // nivel.emocion) pasaría a valer null. Así se colaban enemigos genéricos en el Nivel 1.
            var rutasNiveles = new List<string>();
            foreach (var nivel in bd.niveles)
            {
                if (nivel != null)
                {
                    rutasNiveles.Add(AssetDatabase.GetAssetPath(nivel));
                }
            }

            var ok = true;
            foreach (var rutaNivel in rutasNiveles)
            {
                var nivel = AssetDatabase.LoadAssetAtPath<LevelData>(rutaNivel);
                if (nivel == null || !nivel.escena.Asignada || AssetDatabase.LoadAssetAtPath<SceneAsset>(nivel.escena.Ruta) == null)
                {
                    continue;
                }

                var rutaEscena = nivel.escena.Ruta;
                var escena = EditorSceneManager.OpenScene(rutaEscena, OpenSceneMode.Single);
                nivel = AssetDatabase.LoadAssetAtPath<LevelData>(rutaNivel);
                if (!Preparar(escena, nivel))
                {
                    continue;
                }

                EditorSceneManager.MarkSceneDirty(escena);
                var guardada = EditorSceneManager.SaveScene(escena);
                ok &= guardada;
                Debug.Log(guardada
                    ? $"[Fragmentum] Escena de nivel preparada: {rutaEscena}."
                    : $"[Fragmentum] No se pudo guardar {rutaEscena}.");
            }

            return ok;
        }

        /// <summary>Añade a la escena lo que le falte para jugarse. Devuelve si cambió algo.</summary>
        public static bool Preparar(Scene escena, LevelData nivel)
        {
            AsegurarConfiguracion();
            var tramo = nivel.Extension;
            if (tramo.y <= tramo.x)
            {
                tramo = new Vector2(0f, 40f);
            }

            var cambios = false;
            var raiz = RaizNivel(escena, ref cambios);

            // 0. El recorrido de diseño (DisenoNivel1), en lugar del provisional. Pone también el
            //    checkpoint, la salida y la arena en su sitio: los pasos de abajo ya no los crean.
            if (DisenoNivel1.EsDe(nivel) && RaizRecorrido(escena) == null)
            {
                MontarRecorridoNivel1(escena, nivel, raiz);
                cambios = true;
            }

            // 1. LevelManager, con el prefab del jugador y el punto de inicio.
            var gestor = Buscar<LevelManager>(escena);
            if (gestor == null)
            {
                gestor = raiz.gameObject.AddComponent<LevelManager>();
                Asignar(gestor, "prefabJugador", p => p.objectReferenceValue = ConstructorJugador.PrefabJugador());
                cambios = true;
            }

            var serializado = new SerializedObject(gestor);
            if (serializado.FindProperty("puntoInicio").objectReferenceValue == null)
            {
                var inicio = Hijo(raiz, "Inicio");
                inicio.position = new Vector3(tramo.x + 2f, AlturaPies, 0f);
                Asignar(gestor, "puntoInicio", p => p.objectReferenceValue = inicio);
                cambios = true;
            }

            // 2. Checkpoints.
            if (Buscar<Checkpoint>(escena) == null)
            {
                var contenedor = Hijo(raiz, "Checkpoints");
                var posiciones = PosicionesCheckpoint(nivel);
                for (var i = 0; i < posiciones.Count; i++)
                {
                    CrearCheckpoint(contenedor, $"L{nivel.id}_CP{i + 1}", posiciones[i], nivel);
                }

                cambios |= posiciones.Count > 0;
            }

            // 3. Salida al final de la última sección.
            if (Buscar<SalidaNivel>(escena) == null)
            {
                CrearSalida(raiz, tramo.y - 2f, nivel);
                cambios = true;
            }

            // 4. Cámara que sigue al jugador (el LevelManager le pone los límites al arrancar).
            var camara = Buscar<Camera>(escena);
            if (camara != null && camara.GetComponent<CameraMovement>() == null)
            {
                camara.gameObject.AddComponent<CameraMovement>();
                cambios = true;
            }

            // 5. Suelo: el provisional solo si la escena no tiene ningún suelo que colisione.
            if (!HaySuelo(escena))
            {
                CrearProvisional(escena, nivel, tramo);
                cambios = true;
            }

            if (Buscar<DeadZone>(escena) == null)
            {
                ZonaMortal(raiz, tramo.x - 14f, tramo.y + 14f, -8f);
                cambios = true;
            }

            // 6. Arena del jefe (Fase 7), en la última sección, si el nivel tiene jefe.
            if (nivel.jefe != null && Buscar<ArenaJefe>(escena) == null && nivel.secciones != null && nivel.secciones.Count > 0)
            {
                CrearArena(escena, raiz, nivel);
                cambios = true;
            }

            // 7. Mecánicas del mundo (Fase 9), solo sobre el recorrido provisional: en un nivel
            //    pintado a mano se colocan arrastrando los prefabs de Assets/Prefabs/Mecanicas.
            var provisional = RaizProvisional(escena);
            if (provisional != null && TieneMecanicasDeMundo(nivel) && Buscar<TorretaFuego>(escena) == null &&
                Buscar<PlataformaInestable>(escena) == null && Buscar<PlataformaCombustion>(escena) == null)
            {
                cambios |= CrearMecanicasIra(escena, provisional, nivel);
            }

            // 8. El recorrido provisional del mundo de la Ira, vestido con su tileset volcánico.
            //    Solo cambia el dibujo: las colisiones siguen siendo las de los bloques.
            if (provisional != null && nivel.emocion != null && nivel.emocion.id == EMOCIONES.IRA &&
                !PintorEscenario.EstaPintada(escena))
            {
                cambios |= PintorEscenario.Pintar(escena, provisional, true) > 0;
            }

            // 9. El fondo con parallax del mundo, con los hitos repartidos según la longitud del
            //    nivel. Por ahora solo existe el de la Ira.
            if (nivel.emocion != null && nivel.emocion.id == EMOCIONES.IRA && Buscar<ParallaxMovement>(escena) == null)
            {
                cambios |= ConstructorFondoMundo1.Colocar(escena, nivel);
            }

            // 10. El Golem de Ceniza sin arte (la cápsula de la Fase 4) se sustituye por el que lo
            //     tiene, en su sitio y con su ruta de patrulla.
            cambios |= ConstructorGolem.SustituirGolemsSinArte(escena) > 0;

            return cambios;
        }

        private static bool TieneMecanicasDeMundo(LevelData nivel) =>
            nivel.emocion != null && nivel.emocion.id == EMOCIONES.IRA && nivel.secciones != null && nivel.secciones.Count >= 2;

        /// <summary>
        /// Mecánicas del mundo de la Ira sobre el recorrido provisional, según el Anexo A:
        /// - sección 1: plataforma inestable en el primer foso y la torreta de fuego al final;
        /// - sección 2: plataformas de combustión antes del checkpoint y un río de lava antes del
        ///   final, y lava en el fondo del segundo foso.
        /// La arena queda sin lava en el suelo hasta que se equilibre el combate (PENDIENTES).
        /// </summary>
        private static bool CrearMecanicasIra(Scene escena, Transform provisional, LevelData nivel)
        {
            var mecanicas = Hijo(provisional, "Mecanicas");
            var s1 = nivel.secciones[0];
            var s2 = nivel.secciones[1];
            var fosos = PosicionesFosos(nivel);
            var creadas = 0;

            if (fosos.Count > 0)
            {
                creadas += Colocar(escena, mecanicas, GeneradorMecanicas.PlataformaInestable, (fosos[0].x + fosos[0].y) * 0.5f, AlturaSuelo);
            }

            creadas += Colocar(escena, mecanicas, GeneradorMecanicas.TorretaFuego, s1.hastaX - 1f, AlturaSuelo);
            creadas += Colocar(escena, mecanicas, GeneradorMecanicas.PlataformaCombustion, s2.desdeX + 8.5f, AlturaSuelo + 0.3f);
            creadas += Colocar(escena, mecanicas, GeneradorMecanicas.PlataformaCombustion, s2.desdeX + 12f, AlturaSuelo + 0.3f);
            creadas += Colocar(escena, mecanicas, GeneradorMecanicas.Lava, s2.hastaX - 3.5f, AlturaSuelo + 0.05f);

            if (fosos.Count > 1)
            {
                creadas += Colocar(escena, mecanicas, GeneradorMecanicas.Lava, (fosos[1].x + fosos[1].y) * 0.5f, AlturaSuelo - 1f);
            }

            return creadas > 0;
        }

        private static int Colocar(Scene escena, Transform padre, string prefab, float x, float y)
        {
            var origen = GeneradorMecanicas.Cargar(prefab);
            if (origen == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta el prefab {prefab}: ejecuta Fragmentum → Niveles → Crear prefabs de mecánicas.");
                return 0;
            }

            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(origen, escena);
            instancia.transform.SetParent(padre, true);
            instancia.transform.position = new Vector3(x, y, 0f);
            return 1;
        }

        // --- El recorrido de diseño del Nivel 1 ---

        /// <summary>La lava de los huecos va detrás del suelo, la decoración y las mecánicas: así puede
        /// meterse bajo las orillas sin taparlas, y las plataformas de combustión se ven en pie dentro de ella.</summary>
        public const int OrdenLavaEnHueco = -3;

        /// <summary>El recorrido de diseño de la escena (<see cref="NombreRecorrido"/>), o null.</summary>
        public static Transform RaizRecorrido(Scene escena)
        {
            foreach (var objeto in escena.GetRootGameObjects())
            {
                if (objeto.name == NombreRecorrido)
                {
                    return objeto.transform;
                }
            }

            return null;
        }

        /// <summary>
        /// Monta el recorrido de <see cref="DisenoNivel1"/>. Borra lo provisional y lo que se colocó
        /// para él (su escenario pintado, el checkpoint, la salida y la arena); levanta los bloques,
        /// la lava, las mecánicas y los enemigos; pone el inicio, el checkpoint, la salida y la arena
        /// del diseño, y lo viste con el tileset.
        /// </summary>
        private static void MontarRecorridoNivel1(Scene escena, LevelData nivel, Transform raiz)
        {
            foreach (var objeto in escena.GetRootGameObjects())
            {
                if (objeto.name == NombreProvisional || objeto.name == PintorEscenario.NombreRaiz)
                {
                    Object.DestroyImmediate(objeto);
                }
            }

            foreach (var nombre in new[] { "Checkpoints", "Salida", "Arena" })
            {
                var viejo = raiz.Find(nombre);
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
            }

            var recorrido = new GameObject(NombreRecorrido).transform;
            SceneManager.MoveGameObjectToScene(recorrido.gameObject, escena);

            var colorSuelo = nivel.paleta != null && nivel.paleta.Length > 3 ? nivel.paleta[3] : new Color(0.36f, 0.25f, 0.2f);
            var colorPared = FabricaColor(colorSuelo, 0.6f);
            var bloques = Hijo(recorrido, "Bloques");
            foreach (var b in DisenoNivel1.Suelos)
            {
                Suelo(bloques, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, colorSuelo);
            }

            foreach (var b in DisenoNivel1.Paredes)
            {
                Bloque(bloques, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, Capa(Capas.Wall), colorPared, SpriteCuadrado);
            }

            var plataformas = new List<Collider2D>();
            var contenedorPlataformas = Hijo(recorrido, "PlataformasArena");
            foreach (var b in DisenoNivel1.PlataformasArena)
            {
                plataformas.Add(Suelo(contenedorPlataformas, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, colorSuelo).GetComponent<Collider2D>());
            }

            var mecanicas = Hijo(recorrido, "Mecanicas");
            foreach (var lago in DisenoNivel1.Lavas)
            {
                ColocarLava(escena, mecanicas, lago);
            }

            foreach (var pieza in DisenoNivel1.Mecanicas)
            {
                ColocarPieza(escena, mecanicas, pieza);
            }

            ColocarEnemigos(escena, recorrido);

            var gestor = Buscar<LevelManager>(escena);
            if (gestor != null && new SerializedObject(gestor).FindProperty("puntoInicio").objectReferenceValue is Transform inicio)
            {
                inicio.position = new Vector3(DisenoNivel1.Inicio, DisenoNivel1.SueloEn(DisenoNivel1.Inicio) + AlturaPies, 0f);
            }

            var checkpoints = Hijo(raiz, "Checkpoints");
            var posiciones = PosicionesCheckpoint(nivel);
            for (var i = 0; i < posiciones.Count; i++)
            {
                CrearCheckpoint(checkpoints, $"L{nivel.id}_CP{i + 1}", posiciones[i], nivel, DisenoNivel1.SueloEn(posiciones[i]));
            }

            CrearSalida(raiz, DisenoNivel1.Salida, nivel, DisenoNivel1.SueloArena);
            CrearArenaNivel1(escena, raiz, nivel, plataformas);
            PintorEscenario.Pintar(escena, recorrido, true);
        }

        private static GameObject Instanciar(Scene escena, Transform padre, string prefab, string nombre, float x, float y)
        {
            var origen = GeneradorMecanicas.Cargar(prefab);
            if (origen == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta el prefab {prefab}: ejecuta Fragmentum → Niveles → Crear prefabs de mecánicas.");
                return null;
            }

            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(origen, escena);
            instancia.name = nombre;
            instancia.transform.SetParent(padre, true);
            instancia.transform.position = new Vector3(x, y, 0f);
            return instancia;
        }

        // La lava cubre su hueco con franjas enteras, centradas: lo que sobra queda detrás de las orillas.
        private static void ColocarLava(Scene escena, Transform padre, DisenoNivel1.Lago lago)
        {
            var objeto = Instanciar(escena, padre, GeneradorMecanicas.Lava, lago.Nombre, lago.Centro, lago.Costra);
            var lava = objeto != null ? objeto.GetComponent<Lava>() : null;
            if (lava == null)
            {
                return;
            }

            Asignar(lava, "tramos", p => p.intValue = lago.Franjas);
            Asignar(lava, "ordenDibujo", p => p.intValue = OrdenLavaEnHueco);
            lava.FijarTramos(lago.Franjas);
            PrefabUtility.RecordPrefabInstancePropertyModifications(lava.GetComponent<BoxCollider2D>());
        }

        private static void ColocarPieza(Scene escena, Transform padre, DisenoNivel1.Pieza pieza)
        {
            var objeto = Instanciar(escena, padre, pieza.Prefab, pieza.Nombre, pieza.X, pieza.Y);
            if (objeto == null)
            {
                return;
            }

            // La torreta dispara hacia donde mira su dibujo: espejada, hacia el jugador que llega.
            if (pieza.MiraIzquierda)
            {
                objeto.transform.localScale = new Vector3(-1f, 1f, 1f);
            }

            if (pieza.Desfase > 0f)
            {
                Asignar(objeto.GetComponent<PlataformaCombustion>(), "desfase", p => p.floatValue = pieza.Desfase);
            }
        }

        // Los Ecos a la escala que les dio el diseñador; cada enemigo con su propia ruta de patrulla.
        private static void ColocarEnemigos(Scene escena, Transform recorrido)
        {
            var enemigos = Hijo(recorrido, "Enemigos");
            var rutas = Hijo(recorrido, "Rutas");
            foreach (var guardia in DisenoNivel1.Enemigos)
            {
                var ruta = guardia.Golem ? ConstructorGolem.RutaPrefab : ConstructorEcoIra.RutaPrefab;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Fragmentum] Falta {ruta}: {guardia.Nombre} no se coloca.");
                    continue;
                }

                var enemigo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                enemigo.name = guardia.Nombre;
                enemigo.transform.SetParent(enemigos, true);
                enemigo.transform.position = new Vector3(guardia.X, guardia.Y + AlturaPies, 0f);
                if (!guardia.Golem)
                {
                    enemigo.transform.localScale = new Vector3(DisenoNivel1.EscalaEco.x, DisenoNivel1.EscalaEco.y, 1f);
                }

                var a = Hijo(rutas, $"{guardia.Nombre}_A");
                var b = Hijo(rutas, $"{guardia.Nombre}_B");
                a.position = new Vector3(guardia.Desde, guardia.Y, 0f);
                b.position = new Vector3(guardia.Hasta, guardia.Y, 0f);
                Asignar(enemigo.GetComponent<EnemyController>(), "waypoints", p =>
                {
                    p.arraySize = 2;
                    p.GetArrayElementAtIndex(0).objectReferenceValue = a;
                    p.GetArrayElementAtIndex(1).objectReferenceValue = b;
                });
            }
        }

        /// <summary>
        /// La arena del diseño: la entrada justo después de la puerta, la puerta en el borde de la sala
        /// (se cierra detrás del jugador), la Centinela en x 70 y las plataformas que ella atraviesa.
        /// </summary>
        private static void CrearArenaNivel1(Scene escena, Transform raiz, LevelData nivel, List<Collider2D> plataformas)
        {
            var arena = new GameObject("Arena").transform;
            arena.SetParent(raiz, false);
            arena.position = new Vector3(DisenoNivel1.Entrada, DisenoNivel1.SueloArena, 0f);

            var alto = DisenoNivel1.TechoArena - DisenoNivel1.SueloArena;
            var entrada = arena.gameObject.AddComponent<BoxCollider2D>();
            entrada.isTrigger = true;
            entrada.size = new Vector2(1.2f, alto);
            entrada.offset = new Vector2(0f, alto * 0.5f);

            var colorPuerta = nivel.paleta != null && nivel.paleta.Length > 1 ? nivel.paleta[1] : Color.gray;
            var puerta = Bloque(arena, "Puerta", DisenoNivel1.PuertaDesde, DisenoNivel1.PuertaHasta, DisenoNivel1.SueloArena, DisenoNivel1.TechoArena,
                Capa(Capas.Wall), colorPuerta, SpriteCuadrado);
            puerta.SetActive(false);

            GameObject jefe = null;
            var prefab = nivel.jefe != null ? GeneradorJefes.CargarPrefab(nivel.jefe) : null;
            if (prefab != null)
            {
                jefe = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                jefe.name = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(prefab));
                jefe.transform.SetParent(arena, true);
                jefe.transform.position = new Vector3(DisenoNivel1.Jefe, DisenoNivel1.SueloArena + AlturaPies, 0f);
            }
            else
            {
                Debug.LogWarning("[Fragmentum] No existe el prefab del jefe del Nivel 1: la arena se crea sin jefe. Ejecuta Fragmentum → Jefes → Generar datos y prefabs de jefes.");
            }

            var componente = arena.gameObject.AddComponent<ArenaJefe>();
            Asignar(componente, "jefe", p => p.objectReferenceValue = jefe != null ? jefe.GetComponent<BossController>() : null);
            Asignar(componente, "puertas", p =>
            {
                p.arraySize = 1;
                p.GetArrayElementAtIndex(0).objectReferenceValue = puerta;
            });
            Asignar(componente, "limites", p => p.vector2Value = DisenoNivel1.LimitesArena);
            Asignar(componente, "plataformas", p =>
            {
                p.arraySize = plataformas.Count;
                for (var i = 0; i < plataformas.Count; i++)
                {
                    p.GetArrayElementAtIndex(i).objectReferenceValue = plataformas[i];
                }
            });
        }

        private static Transform RaizProvisional(Scene escena)
        {
            foreach (var objeto in escena.GetRootGameObjects())
            {
                if (objeto.name == NombreProvisional)
                {
                    return objeto.transform;
                }
            }

            return null;
        }

        /// <summary>
        /// Arena en la última sección: la entrada a 6 u de su comienzo, una puerta que se cierra
        /// detrás del jugador y el jefe algo más allá del centro. Si todavía no existe el prefab del
        /// jefe, la arena se crea igual (sin combate) y se avisa.
        /// </summary>
        private static void CrearArena(Scene escena, Transform raiz, LevelData nivel)
        {
            var seccion = nivel.secciones[nivel.secciones.Count - 1];
            var desde = seccion.desdeX;
            var hasta = seccion.hastaX;

            var arena = new GameObject("Arena").transform;
            arena.SetParent(raiz, false);
            arena.position = new Vector3(desde + 6f, AlturaSuelo, 0f);

            var entrada = arena.gameObject.AddComponent<BoxCollider2D>();
            entrada.isTrigger = true;
            entrada.size = new Vector2(1.2f, 20f);
            entrada.offset = new Vector2(0f, 10f);

            var colorPuerta = nivel.paleta != null && nivel.paleta.Length > 1 ? nivel.paleta[1] : Color.gray;
            var puerta = Bloque(arena, "Puerta", desde + 4f, desde + 5f, AlturaSuelo, 20f, Capa(Capas.Wall), colorPuerta, SpriteCuadrado);
            puerta.SetActive(false);

            GameObject jefe = null;
            var prefab = GeneradorJefes.CargarPrefab(nivel.jefe);
            if (prefab != null)
            {
                jefe = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                jefe.name = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(prefab));
                jefe.transform.SetParent(arena, true);
                jefe.transform.position = new Vector3((desde + hasta) * 0.5f + 4f, AlturaPies, 0f);
            }
            else
            {
                Debug.LogWarning($"[Fragmentum] No existe el prefab de {nivel.jefe.nombreCanonico}: la arena se crea sin jefe. Ejecuta Fragmentum → Jefes → Generar datos y prefabs de jefes.");
            }

            var componente = arena.gameObject.AddComponent<ArenaJefe>();
            Asignar(componente, "jefe", p => p.objectReferenceValue = jefe != null ? jefe.GetComponent<BossController>() : null);
            Asignar(componente, "puertas", p =>
            {
                p.arraySize = 1;
                p.GetArrayElementAtIndex(0).objectReferenceValue = puerta;
            });
            Asignar(componente, "limites", p => p.vector2Value = new Vector2(desde + 5f, hasta + 5f));
        }

        /// <summary>El nivel de la LevelDatabase que usa esa escena, o null.</summary>
        public static LevelData NivelDe(Scene escena)
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            return bd != null ? bd.PorEscena(escena.name) : null;
        }

        /// <summary>Los checkpoints del diseño; sin ellos, uno al empezar cada sección tras la primera.</summary>
        public static List<float> PosicionesCheckpoint(LevelData nivel)
        {
            if (nivel.puntosDeControl != null && nivel.puntosDeControl.Count > 0)
            {
                return new List<float>(nivel.puntosDeControl);
            }

            var posiciones = new List<float>();
            if (nivel.secciones != null)
            {
                for (var i = 1; i < nivel.secciones.Count; i++)
                {
                    posiciones.Add(nivel.secciones[i].desdeX + 1f);
                }
            }

            return posiciones;
        }

        // --- Piezas ---

        private static void CrearCheckpoint(Transform padre, string id, float x, LevelData nivel, float suelo = AlturaSuelo)
        {
            var objeto = new GameObject(id) { tag = GameTags.Checkpoint };
            objeto.transform.SetParent(padre, false);
            objeto.transform.position = new Vector3(x, suelo + AlturaPies, 0f);

            var zona = objeto.AddComponent<BoxCollider2D>();
            zona.isTrigger = true;
            zona.size = new Vector2(1.6f, 3.2f);
            zona.offset = new Vector2(0f, 1.6f);

            // Bandera provisional: un mástil y un paño que se tiñe al activarse.
            Visual(objeto.transform, "Mastil", new Vector2(0f, 1.4f), new Vector2(0.12f, 2.8f), new Color(0.75f, 0.75f, 0.78f), -1);
            var pano = Visual(objeto.transform, "Pano", new Vector2(0.45f, 2.45f), new Vector2(0.8f, 0.55f), Color.gray, -1);

            var checkpoint = objeto.AddComponent<Checkpoint>();
            Asignar(checkpoint, "id", p => p.stringValue = id);
            Asignar(checkpoint, "indicador", p => p.objectReferenceValue = pano.GetComponent<SpriteRenderer>());
            Asignar(checkpoint, "colorActivo", p => p.colorValue = nivel.ColorAcento);
        }

        private static void CrearSalida(Transform padre, float x, LevelData nivel, float suelo = AlturaSuelo)
        {
            var objeto = new GameObject("Salida");
            objeto.transform.SetParent(padre, false);
            objeto.transform.position = new Vector3(x, suelo, 0f);

            var zona = objeto.AddComponent<BoxCollider2D>();
            zona.isTrigger = true;
            zona.size = new Vector2(2f, 6f);
            zona.offset = new Vector2(0f, 3f);

            var acento = nivel.ColorAcento;
            Visual(objeto.transform, "Portal", new Vector2(0f, 3f), new Vector2(2f, 6f), new Color(acento.r, acento.g, acento.b, 0.35f), -2);
            objeto.AddComponent<SalidaNivel>();
        }

        /// <summary>
        /// Fosos del recorrido provisional: 3 u justo después de cada cambio de sección, salvo
        /// cerca de un checkpoint. (desde, hasta) en x.
        /// </summary>
        public static List<Vector2> PosicionesFosos(LevelData nivel)
        {
            var checkpoints = PosicionesCheckpoint(nivel);
            var fosos = new List<Vector2>();
            if (nivel.secciones != null)
            {
                for (var i = 1; i < nivel.secciones.Count; i++)
                {
                    var desde = nivel.secciones[i].desdeX + 2f;
                    if (!checkpoints.Exists(x => x > desde - 3f && x < desde + 6f))
                    {
                        fosos.Add(new Vector2(desde, desde + 3f));
                    }
                }
            }

            return fosos;
        }

        /// <summary>
        /// Recorrido provisional sobre el tramo del nivel: suelo con un foso tras cada cambio de
        /// sección, una plataforma por sección, paredes en los extremos y un enemigo del mundo
        /// por sección (salvo la última, que es la arena del jefe).
        /// </summary>
        private static void CrearProvisional(Scene escena, LevelData nivel, Vector2 tramo)
        {
            var provisional = new GameObject(NombreProvisional).transform;
            SceneManager.MoveGameObjectToScene(provisional.gameObject, escena);

            var colorSuelo = nivel.paleta != null && nivel.paleta.Length > 3 ? nivel.paleta[3] : new Color(0.36f, 0.25f, 0.2f);
            var colorPlataforma = nivel.paleta != null && nivel.paleta.Length > 0 ? nivel.paleta[0] : Color.gray;
            var colorPared = FabricaColor(colorSuelo, 0.6f);
            var cuadrado = SpriteCuadrado;
            var checkpoints = PosicionesCheckpoint(nivel);
            var fosos = PosicionesFosos(nivel);

            var inicioTramo = tramo.x - 6f;
            var n = 0;
            foreach (var foso in fosos)
            {
                Suelo(provisional, $"Suelo{n++}", inicioTramo, foso.x, AlturaSuelo - 1f, AlturaSuelo, colorSuelo);
                inicioTramo = foso.y;
            }

            Suelo(provisional, $"Suelo{n}", inicioTramo, tramo.y + 6f, AlturaSuelo - 1f, AlturaSuelo, colorSuelo);

            var pared = Capa(Capas.Wall);
            Bloque(provisional, "ParedIzquierda", tramo.x - 7f, tramo.x - 6f, AlturaSuelo, 20f, pared, colorPared, cuadrado);
            Bloque(provisional, "ParedDerecha", tramo.y + 6f, tramo.y + 7f, AlturaSuelo, 20f, pared, colorPared, cuadrado);

            if (nivel.secciones != null)
            {
                foreach (var seccion in nivel.secciones)
                {
                    // Sin tapar la bandera de ningún checkpoint.
                    var centro = (seccion.desdeX + seccion.hastaX) * 0.5f;
                    if (checkpoints.Exists(x => Mathf.Abs(x - centro) < 3.5f))
                    {
                        centro -= 6f;
                    }

                    Suelo(provisional, $"Plataforma_{seccion.nombre}", centro - 2f, centro + 2f, 2.5f, 3f, colorPlataforma);
                }
            }

            CrearEnemigosProvisionales(escena, provisional, nivel, checkpoints, fosos);
        }

        private static void CrearEnemigosProvisionales(Scene escena, Transform provisional, LevelData nivel, List<float> checkpoints, List<Vector2> fosos)
        {
            if (Buscar<EnemyController>(escena) != null || nivel.secciones == null || nivel.secciones.Count < 2)
            {
                return;
            }

            var variantes = VariantesDe(nivel);
            var rutas = Hijo(provisional, "Rutas");

            // Todas las secciones menos la última (la arena del jefe llega en la Fase 7).
            for (var i = 0; i < nivel.secciones.Count - 1; i++)
            {
                var seccion = nivel.secciones[i];
                var x = (seccion.desdeX + seccion.hastaX) * 0.5f;
                if (checkpoints.Exists(c => Mathf.Abs(c - x) < 5f))
                {
                    x += 6f;
                }

                // Que la patrulla (±3 u) no cruce ningún foso.
                if (fosos.Exists(f => x + 3f > f.x - 0.5f && x - 3f < f.y + 0.5f))
                {
                    continue;
                }

                var prefab = GeneradorEnemigos.CargarPrefab(variantes[Mathf.Min(i, variantes.Length - 1)]);
                if (prefab == null)
                {
                    continue;
                }

                // Con el nombre del archivo del prefab: la raíz de todas las variantes se llama
                // «Enemigo», y con nombres repetidos dos enemigos compartirían la misma ruta.
                var nombre = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(prefab));
                var enemigo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                enemigo.name = nombre;
                enemigo.transform.SetParent(provisional, true);
                enemigo.transform.position = new Vector3(x, AlturaPies, 0f);

                var a = Hijo(rutas, $"{nombre}_A");
                var b = Hijo(rutas, $"{nombre}_B");
                a.position = new Vector3(x - 3f, AlturaSuelo, 0f);
                b.position = new Vector3(x + 3f, AlturaSuelo, 0f);
                Asignar(enemigo.GetComponent<EnemyController>(), "waypoints", p =>
                {
                    p.arraySize = 2;
                    p.GetArrayElementAtIndex(0).objectReferenceValue = a;
                    p.GetArrayElementAtIndex(1).objectReferenceValue = b;
                });
            }
        }

        // Enemigos de cada mundo según el Anexo A (los que ya existen desde la Fase 4). En la Ira,
        // el Eco con arte ocupa el sitio del Soldado de lava.
        private static string[] VariantesDe(LevelData nivel)
        {
            switch (nivel.emocion != null ? nivel.emocion.id : EMOCIONES.INTEGRACION)
            {
                case EMOCIONES.IRA:
                    return new[] { ConstructorEcoIra.NombreDatos, "GolemCeniza" };
                case EMOCIONES.TRISTEZA:
                    return new[] { "Lamento" };
                case EMOCIONES.MIEDO:
                    return new[] { "Espectro", "CentinelaCiego" };
                default:
                    return new[] { "Generico" };
            }
        }

        // --- Utilidades ---

        private static Transform RaizNivel(Scene escena, ref bool cambios)
        {
            foreach (var objeto in escena.GetRootGameObjects())
            {
                if (objeto.name == NombreRaiz)
                {
                    return objeto.transform;
                }
            }

            var gestor = Buscar<LevelManager>(escena);
            if (gestor != null)
            {
                return gestor.transform;
            }

            var raiz = new GameObject(NombreRaiz);
            SceneManager.MoveGameObjectToScene(raiz, escena);
            cambios = true;
            return raiz.transform;
        }

        private static Transform Hijo(Transform padre, string nombre)
        {
            var existente = padre.Find(nombre);
            if (existente != null)
            {
                return existente;
            }

            var hijo = new GameObject(nombre).transform;
            hijo.SetParent(padre, false);
            return hijo;
        }

        private static GameObject Visual(Transform padre, string nombre, Vector2 posicion, Vector2 tamano, Color color, int orden)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = posicion;
            objeto.transform.localScale = new Vector3(tamano.x, tamano.y, 1f);
            var renderer2d = objeto.AddComponent<SpriteRenderer>();
            renderer2d.sprite = SpriteCuadrado;
            renderer2d.color = color;
            renderer2d.sortingOrder = orden;
            return objeto;
        }

        private static T Buscar<T>(Scene escena) where T : Component
        {
            foreach (var raiz in escena.GetRootGameObjects())
            {
                var componente = raiz.GetComponentInChildren<T>(true);
                if (componente != null)
                {
                    return componente;
                }
            }

            return null;
        }

        private static bool HaySuelo(Scene escena)
        {
            var capaSuelo = LayerMask.NameToLayer(Capas.Suelo);
            foreach (var raiz in escena.GetRootGameObjects())
            {
                foreach (var colision in raiz.GetComponentsInChildren<Collider2D>(true))
                {
                    if (!colision.isTrigger && colision.gameObject.layer == capaSuelo)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Color FabricaColor(Color color, float factor) =>
            new Color(color.r * factor, color.g * factor, color.b * factor, 1f);
    }
}
