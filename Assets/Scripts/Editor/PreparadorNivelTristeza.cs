using System.Collections.Generic;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using Fragmentum.Levels.Mecanicas;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Fragmentum.EditorTools.UtilidadesEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El montaje del Nivel 2 (Tristeza, <see cref="DisenoNivel2"/>). A diferencia del Nivel 1, se
    /// vuelve a montar entero cada vez que sube <see cref="DisenoNivel2.Version"/>: el nivel crece
    /// por fases (recorrido, mecánicas, enemigos y jefe), y así cada fase entra en la escena sin
    /// tocarla a mano. Mientras la versión no cambie, preparar otra vez no cambia nada.
    ///
    /// Sin arte todavía: los bloques son rectángulos con los colores de la paleta del mundo, el agua
    /// es un velo translúcido por delante de Kael (para que se le vea sumergido) y el hielo, un
    /// remate blanco sobre los suelos que resbalan.
    /// </summary>
    public static partial class PreparadorNivel
    {
        /// <summary>Hijo del recorrido que dice qué versión del diseño está montada.</summary>
        public static string MarcaVersionNivel2 => $"Diseño v{DisenoNivel2.Version}";

        /// <summary>El agua va por delante de Kael (10): se le ve sumergido.</summary>
        public const int OrdenAgua = 20;

        private const int OrdenRemateHielo = 1;
        private const float AltoRemateHielo = 0.25f;

        /// <summary>¿La escena tiene montado el recorrido de la versión actual de <see cref="DisenoNivel2"/>?</summary>
        public static bool RecorridoNivel2AlDia(Scene escena)
        {
            var recorrido = RaizRecorrido(escena);
            return recorrido != null && recorrido.Find(MarcaVersionNivel2) != null;
        }

        /// <summary>
        /// Monta el recorrido de <see cref="DisenoNivel2"/>: borra lo de un montaje anterior (y lo
        /// provisional, si lo hubiera), levanta los bloques, las placas, el hielo y el agua, coloca
        /// los enemigos y pone el inicio, los checkpoints, la salida, la arena, la zona mortal y la
        /// cámara del diseño.
        /// </summary>
        private static void MontarRecorridoNivel2(Scene escena, LevelData nivel, Transform raiz)
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
            Hijo(recorrido, MarcaVersionNivel2);

            // La paleta del Anexo A: azul profundo, azul hielo, gris azulado y blanco nieve.
            var colorSuelo = DePaleta(nivel, 2, new Color(0.17f, 0.24f, 0.31f));
            var colorPared = FabricaColor(colorSuelo, 0.7f);
            var colorHielo = DePaleta(nivel, 3, new Color(0.91f, 0.93f, 0.96f));
            var azul = DePaleta(nivel, 1, new Color(0.29f, 0.5f, 0.65f));
            var colorAgua = new Color(azul.r, azul.g, azul.b, 0.45f);

            var bloques = Hijo(recorrido, "Bloques");
            foreach (var b in DisenoNivel2.Suelos)
            {
                Suelo(bloques, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, colorSuelo);
            }

            foreach (var b in DisenoNivel2.Paredes)
            {
                Bloque(bloques, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, Capa(Capas.Wall), colorPared, SpriteCuadrado);
            }

            // Las placas de hielo flotantes, cada una con su oscilación. Las de la arena y el balcón
            // los atraviesa la Doncella (ArenaJefe.plataformas).
            var plataformasArena = new List<Collider2D>();
            var placas = Hijo(recorrido, "Placas");
            foreach (var placa in DisenoNivel2.Placas)
            {
                var objeto = Instanciar(escena, placas, GeneradorMecanicas.PlataformaFlotante, placa.Nombre, placa.X, placa.Y);
                var flotante = objeto != null ? objeto.GetComponent<PlataformaFlotante>() : null;
                if (flotante == null)
                {
                    continue;
                }

                Asignar(flotante, "amplitud", p => p.floatValue = placa.Amplitud);
                Asignar(flotante, "periodo", p => p.floatValue = DisenoNivel2.PeriodoPlaca);
                Asignar(flotante, "desfase", p => p.floatValue = placa.Desfase);
                if (placa.X > DisenoNivel2.LimitesArena.x && placa.X < DisenoNivel2.LimitesArena.y)
                {
                    plataformasArena.Add(objeto.GetComponent<Collider2D>());
                }
            }

            // El tapón de hielo que solo rompe el Ataque Cargado: la única salida de la superficie.
            var mecanicas = Hijo(recorrido, "Mecanicas");
            Instanciar(escena, mecanicas, GeneradorMecanicas.HieloRompible, "HieloRompible", DisenoNivel2.HieloRompible.x, DisenoNivel2.HieloRompible.y);

            // Los ecos de intentos: tras morir, la sombra repite el intento que salió mal.
            new GameObject("EcoDeIntentos", typeof(EcoDeIntentos)).transform.SetParent(recorrido, false);

            foreach (var b in DisenoNivel2.PlataformasArena)
            {
                plataformasArena.Add(Suelo(placas, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, colorSuelo).GetComponent<Collider2D>());
            }

            // El hielo: el remate blanco que se ve y la franja que resbala (ModificadorMovimiento.Hielo).
            var hielo = Hijo(recorrido, "Hielo");
            foreach (var zona in DisenoNivel2.Hielos)
            {
                Visual(hielo, $"Remate_{zona.Nombre}", new Vector2((zona.Desde + zona.Hasta) * 0.5f, zona.Abajo - AltoRemateHielo * 0.5f),
                    new Vector2(zona.Hasta - zona.Desde, AltoRemateHielo), colorHielo, OrdenRemateHielo);
                ZonaDeMovimiento(hielo, zona, ZonaModificadorMovimiento.TIPOS.HIELO, Color.clear, null);
            }

            // El agua (ModificadorMovimiento.BajoElAgua): un velo translúcido por delante de Kael.
            var agua = Hijo(recorrido, "Agua");
            foreach (var zona in DisenoNivel2.Aguas)
            {
                ZonaDeMovimiento(agua, zona, ZonaModificadorMovimiento.TIPOS.AGUA, colorAgua, SpriteCuadrado);
            }

            ColocarEnemigosNivel2(escena, recorrido);

            var gestor = Buscar<LevelManager>(escena);
            if (gestor != null)
            {
                if (new SerializedObject(gestor).FindProperty("puntoInicio").objectReferenceValue is Transform inicio)
                {
                    inicio.position = new Vector3(DisenoNivel2.Inicio, DisenoNivel2.SueloBajo(DisenoNivel2.Inicio) + AlturaPies, 0f);
                }

                // La calle está a y −11: más honda que lo que enseña la cámara por defecto (−6).
                Asignar(gestor, "alturaCamara", p => p.vector2Value = DisenoNivel2.AlturaCamara);
            }

            var camara = Buscar<Camera>(escena);
            if (camara != null)
            {
                camara.orthographicSize = DisenoNivel2.TamanoCamara;
            }

            // El primero, en la campana junto a la silla vacía, lleva el eco de memoria del guion.
            var checkpoints = Hijo(raiz, "Checkpoints");
            for (var i = 0; i < DisenoNivel2.Checkpoints.Length; i++)
            {
                var punto = DisenoNivel2.Checkpoints[i];
                var checkpoint = CrearCheckpoint(checkpoints, $"L{nivel.id}_CP{i + 1}", punto.x, nivel, punto.y);
                if (checkpoint.Id == CheckpointDelEcoDeMemoria(nivel))
                {
                    Asignar(checkpoint, "ecoDeMemoria", p => p.boolValue = true);
                }
            }

            CrearSalida(raiz, DisenoNivel2.Salida, nivel, DisenoNivel2.SueloArena);
            CrearArenaNivel2(escena, raiz, nivel, plataformasArena);
            ZonaMortal(raiz, -20f, 110f, DisenoNivel2.ZonaMortal);
        }

        private static Color DePaleta(LevelData nivel, int indice, Color porDefecto) =>
            nivel.paleta != null && nivel.paleta.Length > indice ? nivel.paleta[indice] : porDefecto;

        // Un trigger que cambia la física de Kael mientras está dentro, como las zonas de la escena de prueba.
        private static void ZonaDeMovimiento(Transform padre, DisenoNivel2.Zona zona, ZonaModificadorMovimiento.TIPOS tipo, Color color, Sprite sprite)
        {
            var objeto = Bloque(padre, zona.Nombre, zona.Desde, zona.Hasta, zona.Abajo, zona.Arriba, 0, color, sprite);
            objeto.GetComponent<BoxCollider2D>().isTrigger = true;

            var renderer2d = objeto.GetComponent<SpriteRenderer>();
            if (renderer2d != null)
            {
                renderer2d.sortingOrder = OrdenAgua;
            }

            var componente = objeto.AddComponent<ZonaModificadorMovimiento>();
            Asignar(componente, "tipo", p => p.enumValueIndex = (int)tipo);
        }

        // Los Lamentos y la Plañidera con su ruta, la Escultura dormida en su sitio (no anda) y las
        // Medusas con su seno.
        private static void ColocarEnemigosNivel2(Scene escena, Transform recorrido)
        {
            var enemigos = Hijo(recorrido, "Enemigos");
            var rutas = Hijo(recorrido, "Rutas");
            var prefabs = new Dictionary<DisenoNivel2.TIPOS_ENEMIGO, GameObject>
            {
                [DisenoNivel2.TIPOS_ENEMIGO.LAMENTO] = GeneradorEnemigos.CargarPrefab("Lamento"),
                [DisenoNivel2.TIPOS_ENEMIGO.ESCULTURA] = GeneradorEnemigos.CargarPrefab("Escultura"),
                [DisenoNivel2.TIPOS_ENEMIGO.MEDUSA] = GeneradorEnemigos.CargarPrefab(GeneradorEnemigos.Medusa),
                [DisenoNivel2.TIPOS_ENEMIGO.PLANIDERA] = GeneradorEnemigos.CargarPrefab("Planidera"),
            };

            foreach (var guardia in DisenoNivel2.Enemigos)
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

                switch (guardia.Tipo)
                {
                    case DisenoNivel2.TIPOS_ENEMIGO.LAMENTO:
                    case DisenoNivel2.TIPOS_ENEMIGO.PLANIDERA:
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
                        break;

                    case DisenoNivel2.TIPOS_ENEMIGO.ESCULTURA:
                        // Sin ruta: no anda. Mira hacia donde llega Kael.
                        enemigo.transform.position = new Vector3(guardia.X, guardia.Y + AlturaPies, 0f);
                        break;

                    case DisenoNivel2.TIPOS_ENEMIGO.MEDUSA:
                        // El centro de su seno: flota, no se apoya.
                        enemigo.transform.position = new Vector3(guardia.X, guardia.Y, 0f);
                        var medusa = enemigo.GetComponent<MedusaHielo>();
                        Asignar(medusa, "vertical", p => p.boolValue = guardia.Vertical);
                        Asignar(medusa, "amplitud", p => p.floatValue = guardia.Amplitud);
                        Asignar(medusa, "periodo", p => p.floatValue = guardia.Periodo);
                        Asignar(medusa, "desfase", p => p.floatValue = guardia.Desfase);
                        break;
                }
            }
        }

        /// <summary>
        /// La arena: la entrada pasada la puerta, la puerta en el borde de la sala (se cierra detrás
        /// del jugador) y la Doncella en el centro, con las placas y el balcón que ella atraviesa y
        /// las tres franjas del suelo que inunda y congela.
        /// </summary>
        private static void CrearArenaNivel2(Scene escena, Transform raiz, LevelData nivel, List<Collider2D> plataformas)
        {
            var arena = new GameObject("Arena").transform;
            arena.SetParent(raiz, false);
            arena.position = new Vector3(DisenoNivel2.Entrada, DisenoNivel2.SueloArena, 0f);

            var alto = DisenoNivel2.TechoArena - DisenoNivel2.SueloArena;
            var entrada = arena.gameObject.AddComponent<BoxCollider2D>();
            entrada.isTrigger = true;
            entrada.size = new Vector2(1.2f, alto);
            entrada.offset = new Vector2(0f, alto * 0.5f);

            var colorPuerta = DePaleta(nivel, 1, Color.gray);
            var puerta = Bloque(arena, "Puerta", DisenoNivel2.PuertaDesde, DisenoNivel2.PuertaHasta, DisenoNivel2.SueloArena, DisenoNivel2.TechoArena,
                Capa(Capas.Wall), colorPuerta, SpriteCuadrado);
            puerta.SetActive(false);

            GameObject jefe = null;
            var prefab = nivel.jefe != null ? GeneradorJefes.CargarPrefab(nivel.jefe) : null;
            if (prefab != null)
            {
                jefe = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                jefe.name = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(prefab));
                jefe.transform.SetParent(arena, true);
                jefe.transform.position = new Vector3(DisenoNivel2.Jefe, DisenoNivel2.SueloArena + AlturaPies, 0f);
            }
            else
            {
                Debug.LogWarning("[Fragmentum] No existe el prefab del jefe del Nivel 2: la arena se crea sin jefe. Ejecuta Fragmentum → Jefes → Generar datos y prefabs de jefes.");
            }

            // Las tres franjas del suelo que la Doncella inunda y congela. Se dibujan solas al jugar.
            var franjas = new List<FranjaInundable>();
            foreach (var zona in DisenoNivel2.Franjas)
            {
                var franja = new GameObject(zona.Nombre).AddComponent<FranjaInundable>();
                franja.transform.SetParent(arena, true);
                franja.transform.position = new Vector3((zona.Desde + zona.Hasta) * 0.5f, zona.Abajo, 0f);
                franja.ancho = zona.Hasta - zona.Desde;
                franja.alturaAgua = zona.Arriba - zona.Abajo;
                franja.alturaCapa = DisenoNivel2.CapaHelada;
                franja.grosorCapa = DisenoNivel2.GrosorCapaHelada;
                franja.colorAgua = new Color(colorPuerta.r, colorPuerta.g, colorPuerta.b, 0.45f);
                franja.colorHielo = DePaleta(nivel, 3, franja.colorHielo);
                franjas.Add(franja);
            }

            var doncella = jefe != null ? jefe.GetComponent<DoncellaLamento>() : null;
            if (doncella != null)
            {
                Asignar(doncella, "alturaDeVuelo", p => p.floatValue = DisenoNivel2.VueloDoncella);
                Asignar(doncella, "franjas", p =>
                {
                    p.arraySize = franjas.Count;
                    for (var i = 0; i < franjas.Count; i++)
                    {
                        p.GetArrayElementAtIndex(i).objectReferenceValue = franjas[i];
                    }
                });
            }

            var componente = arena.gameObject.AddComponent<ArenaJefe>();
            Asignar(componente, "jefe", p => p.objectReferenceValue = jefe != null ? jefe.GetComponent<BossController>() : null);
            Asignar(componente, "puertas", p =>
            {
                p.arraySize = 1;
                p.GetArrayElementAtIndex(0).objectReferenceValue = puerta;
            });
            Asignar(componente, "limites", p => p.vector2Value = DisenoNivel2.LimitesArena);
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
