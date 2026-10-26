using System.Collections.Generic;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using Fragmentum.Levels.Escenario;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static Fragmentum.EditorTools.UtilidadesEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El montaje del Nivel 3 (Miedo, <see cref="DisenoNivel3"/>). Como el del Nivel 2, se vuelve
    /// a montar entero cada vez que sube <see cref="DisenoNivel3.Version"/>: el nivel crece por fases
    /// (recorrido, mecánicas, enemigos y jefe) y así cada fase entra en la escena sin tocarla a mano.
    /// Mientras la versión no cambie, preparar otra vez no cambia nada.
    ///
    /// Fase A: los bloques con los colores de la paleta (el tileset del bosque llegará con el
    /// arte), las ramas, las raíces del callejón que no agarran (<see cref="SinAgarre"/>), los
    /// Espectros y los Centinelas Ciegos con su ruta, los checkpoints, la salida y la arena del
    /// Espectro del Pavor.
    ///
    /// Fase B: la oscuridad (<see cref="LuzDelNivel"/> en la luz global de la escena, con la luz de
    /// cada tramo), las antorchas, los cepos, las ramas podridas que ceden (prefabs de
    /// <see cref="GeneradorMecanicas"/>) y las cintas blancas de la ruta buena.
    ///
    /// Fase C: el Acechador, con el claro como territorio, y los tres árboles contra los que se
    /// estrella (<see cref="Arbol"/>). El sueño de los Espectros y el oído de los Centinelas Ciegos
    /// van en sus prefabs (<see cref="GeneradorEnemigos"/>).
    /// </summary>
    public static partial class PreparadorNivel
    {
        /// <summary>Hijo del recorrido que dice qué versión del diseño está montada.</summary>
        public static string MarcaVersionNivel3 => $"Diseño v{DisenoNivel3.Version}";

        /// <summary>¿La escena tiene montado el recorrido de la versión actual de <see cref="DisenoNivel3"/>?</summary>
        public static bool RecorridoNivel3AlDia(Scene escena)
        {
            var recorrido = RaizRecorrido(escena);
            return recorrido != null && recorrido.Find(MarcaVersionNivel3) != null;
        }

        /// <summary>
        /// Monta el recorrido de <see cref="DisenoNivel3"/>: borra lo de un montaje anterior (y lo
        /// provisional, si lo hubiera), levanta los bloques y las ramas, coloca los enemigos y pone
        /// el inicio, los checkpoints, la salida, la arena, la zona mortal y la cámara del diseño.
        /// </summary>
        private static void MontarRecorridoNivel3(Scene escena, LevelData nivel, Transform raiz)
        {
            foreach (var objeto in escena.GetRootGameObjects())
            {
                if (objeto.name == NombreRecorrido || objeto.name == NombreProvisional || objeto.name == PintorEscenario.NombreRaiz)
                {
                    Object.DestroyImmediate(objeto);
                }
            }

            foreach (var nombre in new[] { "Checkpoints", "Salida", "Arena", "ZonaMortal" })
            {
                var viejo = raiz.Find(nombre);
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
            }

            var recorrido = new GameObject(NombreRecorrido).transform;
            SceneManager.MoveGameObjectToScene(recorrido.gameObject, escena);
            Hijo(recorrido, MarcaVersionNivel3);

            // La paleta del Anexo A (negro, violeta, verde pantanoso y gris espectral) es tan oscura
            // que, bajo la luz de la luna y la de Kael, los bloques se aclaran para que el recorrido se lea.
            var colorSuelo = FabricaColor(DePaleta(nivel, 2, new Color(0.1f, 0.18f, 0.1f)), 1.8f);
            var colorPared = FabricaColor(colorSuelo, 0.7f);
            var colorRama = GeneradorMecanicas.ColorRamaSinArte;

            var bloques = Hijo(recorrido, "Bloques");
            foreach (var b in DisenoNivel3.Suelos)
            {
                Suelo(bloques, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, colorSuelo);
            }

            foreach (var b in DisenoNivel3.Paredes)
            {
                var pared = Bloque(bloques, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, Capa(Capas.Wall), colorPared, SpriteCuadrado);

                // Las raíces del callejón: se choca con ellas, pero no se sube con el salto de pared.
                if (DisenoNivel3.NoAgarra(b.Nombre))
                {
                    pared.AddComponent<SinAgarre>();
                }
            }

            var ramas = Hijo(recorrido, "Ramas");
            foreach (var b in DisenoNivel3.Ramas)
            {
                Suelo(ramas, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, colorRama);
            }

            // Las podridas ceden: el prefab de la rama podrida, con el pivote en su superficie. A
            // oscuras son del color de las sanas.
            var podridas = Hijo(recorrido, "RamasPodridas");
            foreach (var b in DisenoNivel3.RamasPodridas)
            {
                Instanciar(escena, podridas, GeneradorMecanicas.RamaPodrida, b.Nombre, (b.Desde + b.Hasta) * 0.5f, b.Arriba);
            }

            ColocarMecanicasNivel3(escena, recorrido);
            ColocarOscuridadNivel3(escena);

            // Las ramas de la arena las atraviesa el jefe (ArenaJefe.plataformas).
            var plataformasArena = new List<Collider2D>();
            var contenedorArena = Hijo(recorrido, "PlataformasArena");
            foreach (var b in DisenoNivel3.PlataformasArena)
            {
                plataformasArena.Add(Suelo(contenedorArena, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, colorRama).GetComponent<Collider2D>());
            }

            ColocarArbolesNivel3(recorrido);
            ColocarEnemigosNivel3(escena, recorrido);

            var gestor = Buscar<LevelManager>(escena);
            if (gestor != null)
            {
                if (new SerializedObject(gestor).FindProperty("puntoInicio").objectReferenceValue is Transform inicio)
                {
                    inicio.position = new Vector3(DisenoNivel3.Inicio, DisenoNivel3.SueloBajo(DisenoNivel3.Inicio) + AlturaPies, 0f);
                }

                // La hondonada está a y −4: más honda que lo que enseña la cámara por defecto (−6) con su suelo.
                Asignar(gestor, "alturaCamara", p => p.vector2Value = DisenoNivel3.AlturaCamara);
            }

            var camara = Buscar<Camera>(escena);
            if (camara != null)
            {
                camara.orthographicSize = DisenoNivel3.TamanoCamara;
            }

            // El primero, en el fondo de la trinchera, lleva el eco de memoria del guion.
            var checkpoints = Hijo(raiz, "Checkpoints");
            for (var i = 0; i < DisenoNivel3.Checkpoints.Length; i++)
            {
                var punto = DisenoNivel3.Checkpoints[i];
                var checkpoint = CrearCheckpoint(checkpoints, $"L{nivel.id}_CP{i + 1}", punto.x, nivel, punto.y);
                if (checkpoint.Id == CheckpointDelEcoDeMemoria(nivel))
                {
                    Asignar(checkpoint, "ecoDeMemoria", p => p.boolValue = true);
                }
            }

            CrearSalida(raiz, DisenoNivel3.Salida, nivel, DisenoNivel3.SueloArena);
            CrearArenaNivel3(escena, raiz, nivel, plataformasArena);
            ZonaMortal(raiz, DisenoNivel3.AnchoZonaMortal.x, DisenoNivel3.AnchoZonaMortal.y, DisenoNivel3.ZonaMortal);
        }

        /// <summary>Nombre del objeto de la luz global cuando la escena no la trae.</summary>
        public const string NombreLuzGlobal = "Global Light 2D";

        // Las antorchas y los cepos (prefabs, por su pie) y las cintas que marcan la ruta buena, que
        // cuelgan de lo alto de sus ramas, por delante de ellas.
        private static void ColocarMecanicasNivel3(Scene escena, Transform recorrido)
        {
            var antorchas = Hijo(recorrido, "Antorchas");
            for (var i = 0; i < DisenoNivel3.Antorchas.Length; i++)
            {
                var pie = DisenoNivel3.Antorchas[i];
                Instanciar(escena, antorchas, GeneradorMecanicas.Antorcha, $"Antorcha_{i + 1}", pie.x, pie.y);
            }

            var cepos = Hijo(recorrido, "Cepos");
            for (var i = 0; i < DisenoNivel3.Cepos.Length; i++)
            {
                var centro = DisenoNivel3.Cepos[i];
                Instanciar(escena, cepos, GeneradorMecanicas.Cepo, $"Cepo_{i + 1}", centro.x, centro.y);
            }

            var cintas = Hijo(recorrido, "Cintas");
            var tamano = DisenoNivel3.TamanoCinta;
            for (var i = 0; i < DisenoNivel3.Cintas.Length; i++)
            {
                var nudo = DisenoNivel3.Cintas[i];
                Visual(cintas, $"Cinta_{i + 1}", new Vector2(nudo.x, nudo.y + 0.05f - tamano.y * 0.5f), tamano, new Color(0.95f, 0.95f, 0.92f, 1f), 1);
            }
        }

        /// <summary>
        /// La oscuridad: la luz global de la escena (la de la plantilla, o una nueva si no la hay) con
        /// la <see cref="LuzDelNivel"/> de cada tramo del diseño. La activa el LevelManager al jugar,
        /// así que en el Editor la escena sigue viéndose con luz.
        /// </summary>
        private static void ColocarOscuridadNivel3(Scene escena)
        {
            Light2D global = null;
            foreach (var raiz in escena.GetRootGameObjects())
            {
                foreach (var luz in raiz.GetComponentsInChildren<Light2D>(true))
                {
                    if (luz.lightType == Light2D.LightType.Global)
                    {
                        global = luz;
                        break;
                    }
                }

                if (global != null)
                {
                    break;
                }
            }

            if (global == null)
            {
                var objeto = new GameObject(NombreLuzGlobal);
                SceneManager.MoveGameObjectToScene(objeto, escena);
                global = objeto.AddComponent<Light2D>();
                global.lightType = Light2D.LightType.Global;
                global.intensity = 1f;
                global.color = Color.white;
            }

            var oscuridad = global.GetComponent<LuzDelNivel>();
            if (oscuridad == null)
            {
                oscuridad = global.gameObject.AddComponent<LuzDelNivel>();
            }

            Asignar(oscuridad, "luzGlobal", p => p.objectReferenceValue = global);
            Asignar(oscuridad, "tramos", p =>
            {
                p.arraySize = DisenoNivel3.Luces.Length;
                for (var i = 0; i < DisenoNivel3.Luces.Length; i++)
                {
                    var luz = DisenoNivel3.Luces[i];
                    var tramo = p.GetArrayElementAtIndex(i);
                    tramo.FindPropertyRelative(nameof(TramoDeLuz.nombre)).stringValue = luz.Nombre;
                    tramo.FindPropertyRelative(nameof(TramoDeLuz.desdeX)).floatValue = luz.Desde;
                    tramo.FindPropertyRelative(nameof(TramoDeLuz.hastaX)).floatValue = luz.Hasta;
                    tramo.FindPropertyRelative(nameof(TramoDeLuz.color)).colorValue = luz.Color;
                    tramo.FindPropertyRelative(nameof(TramoDeLuz.intensidad)).floatValue = luz.Intensidad;
                }
            });
        }

        /// <summary>Lo que se ve de un árbol del claro mientras no llega su arte: un tronco oscuro, por detrás de todo.</summary>
        public static readonly Color ColorArbolSinArte = new Color(0.22f, 0.18f, 0.16f, 1f);

        // Los árboles del claro: de pie en su suelo, sin collider (Kael pasa por delante).
        private static void ColocarArbolesNivel3(Transform recorrido)
        {
            var arboles = Hijo(recorrido, "Arboles");
            var tamano = DisenoNivel3.TamanoArbol;
            for (var i = 0; i < DisenoNivel3.Arboles.Length; i++)
            {
                var x = DisenoNivel3.Arboles[i];
                var arbol = new GameObject($"Arbol_{i + 1}").transform;
                arbol.SetParent(arboles, false);
                arbol.position = new Vector3(x, DisenoNivel3.SueloBajo(x), 0f);
                var componente = arbol.gameObject.AddComponent<Arbol>();
                Asignar(componente, "tamano", p => p.vector2Value = tamano);
                Visual(arbol, "Tronco", new Vector2(0f, tamano.y * 0.5f), tamano, ColorArbolSinArte, -1);
            }
        }

        // Los Espectros, los Centinelas Ciegos y el Acechador, cada uno con su ruta de patrulla.
        private static void ColocarEnemigosNivel3(Scene escena, Transform recorrido)
        {
            var enemigos = Hijo(recorrido, "Enemigos");
            var rutas = Hijo(recorrido, "Rutas");
            var prefabs = new Dictionary<DisenoNivel3.TIPOS_ENEMIGO, GameObject>
            {
                [DisenoNivel3.TIPOS_ENEMIGO.ESPECTRO] = GeneradorEnemigos.CargarPrefab("Espectro"),
                [DisenoNivel3.TIPOS_ENEMIGO.CENTINELA_CIEGO] = GeneradorEnemigos.CargarPrefab("CentinelaCiego"),
                [DisenoNivel3.TIPOS_ENEMIGO.ACECHADOR] = GeneradorEnemigos.CargarPrefab("Acechador"),
            };

            foreach (var guardia in DisenoNivel3.Enemigos)
            {
                var prefab = prefabs[guardia.Tipo];
                if (prefab == null)
                {
                    Debug.LogWarning($"[Fragmentum] Falta el prefab de {guardia.Tipo}: {guardia.Nombre} no se coloca. Ejecuta Fragmentum → Enemigos → Crear prefabs.");
                    continue;
                }

                var enemigo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                enemigo.name = guardia.Nombre;
                enemigo.transform.SetParent(enemigos, true);
                enemigo.transform.position = new Vector3(guardia.X, guardia.Y + AlturaPies, 0f);

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

                // El Acechador embiste dentro del claro: en un extremo, frena.
                var acechador = enemigo.GetComponent<Acechador>();
                if (acechador != null)
                {
                    Asignar(acechador, "limites", p => p.vector2Value = DisenoNivel3.LimitesAcechador);
                }
            }
        }

        /// <summary>
        /// La arena: la entrada pasada la puerta, la puerta en el borde de la sala (se cierra detrás
        /// del jugador) y el Espectro del Pavor en el centro, con las dos ramas que atraviesa.
        /// </summary>
        private static void CrearArenaNivel3(Scene escena, Transform raiz, LevelData nivel, List<Collider2D> plataformas)
        {
            var arena = new GameObject("Arena").transform;
            arena.SetParent(raiz, false);
            arena.position = new Vector3(DisenoNivel3.Entrada, DisenoNivel3.SueloArena, 0f);

            var alto = DisenoNivel3.TechoArena - DisenoNivel3.SueloArena;
            var entrada = arena.gameObject.AddComponent<BoxCollider2D>();
            entrada.isTrigger = true;
            entrada.size = new Vector2(1.2f, alto);
            entrada.offset = new Vector2(0f, alto * 0.5f);

            var colorPuerta = DePaleta(nivel, 1, Color.gray);
            var puerta = Bloque(arena, "Puerta", DisenoNivel3.PuertaDesde, DisenoNivel3.PuertaHasta, DisenoNivel3.SueloArena, DisenoNivel3.TechoArena,
                Capa(Capas.Wall), FabricaColor(colorPuerta, 2f), SpriteCuadrado);
            ConstructorPuertas.VestirVerja(puerta, nivel, DisenoNivel3.PuertaDesde, DisenoNivel3.PuertaHasta, DisenoNivel3.SueloArena, DisenoNivel3.TechoArena);
            puerta.SetActive(false);

            GameObject jefe = null;
            var prefab = nivel.jefe != null ? GeneradorJefes.CargarPrefab(nivel.jefe) : null;
            if (prefab != null)
            {
                jefe = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                jefe.name = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(prefab));
                jefe.transform.SetParent(arena, true);
                jefe.transform.position = new Vector3(DisenoNivel3.Jefe, DisenoNivel3.SueloArena + AlturaPies, 0f);
            }
            else
            {
                Debug.LogWarning("[Fragmentum] No existe el prefab del jefe del Nivel 3: la arena se crea sin jefe. Ejecuta Fragmentum → Jefes → Generar datos y prefabs de jefes.");
            }

            var componente = arena.gameObject.AddComponent<ArenaJefe>();
            Asignar(componente, "jefe", p => p.objectReferenceValue = jefe != null ? jefe.GetComponent<BossController>() : null);
            Asignar(componente, "puertas", p =>
            {
                p.arraySize = 1;
                p.GetArrayElementAtIndex(0).objectReferenceValue = puerta;
            });
            Asignar(componente, "limites", p => p.vector2Value = DisenoNivel3.LimitesArena);
            Asignar(componente, "plataformas", p =>
            {
                p.arraySize = plataformas.Count;
                for (var i = 0; i < plataformas.Count; i++)
                {
                    p.GetArrayElementAtIndex(i).objectReferenceValue = plataformas[i];
                }
            });
        }
    }
}
