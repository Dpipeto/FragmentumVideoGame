using System.Collections.Generic;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using Fragmentum.Levels.Escenario;
using Fragmentum.Levels.Mecanicas;
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
    ///
    /// Fase E: con el tileset del bosque cortado (<see cref="ConstructorEscenarioMundo3"/>), cada
    /// bloque apaga su rectángulo de color y lleva su dibujo: la tierra con su musgo, las raíces del
    /// callejón, las ramas con la rama sana; las cintas, los árboles y el atrezo
    /// (<see cref="DisenoNivel3.Decorados"/>), los suyos.
    ///
    /// La ciénaga (2026-10-19): su agua, con la superficie dibujada; las marañas que sujetan, con su
    /// dibujo (<see cref="Marana"/>); y la pista en la orilla. Las raíces del árbol hundido son muros
    /// que no agarran, como las del callejón.
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

            // Con el tileset cortado, cada bloque apaga su rectángulo de color y lleva su dibujo.
            var bloques = Hijo(recorrido, "Bloques");
            var escenario = ConstructorEscenarioMundo3.Completo ? Hijo(recorrido, NombreEscenario) : null;
            foreach (var b in DisenoNivel3.Suelos)
            {
                var suelo = Suelo(bloques, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, colorSuelo);
                if (escenario != null)
                {
                    Apagar(suelo, ConstructorEscenarioMundo3.VestirSuelo(escenario, NombreDibujo(b.Nombre), b.Desde, b.Hasta, b.Abajo, b.Arriba));
                }
            }

            foreach (var b in DisenoNivel3.Paredes)
            {
                var pared = Bloque(bloques, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, Capa(Capas.Wall), colorPared, SpriteCuadrado);
                if (escenario != null)
                {
                    Apagar(pared, ConstructorEscenarioMundo3.VestirMuro(escenario, NombreDibujo(b.Nombre), b.Desde, b.Hasta, b.Abajo, b.Arriba,
                        DisenoNivel3.NoAgarra(b.Nombre)));
                }

                // Las raíces del callejón: se choca con ellas, pero no se sube con el salto de pared.
                if (DisenoNivel3.NoAgarra(b.Nombre))
                {
                    pared.AddComponent<SinAgarre>();
                }
            }

            var ramas = Hijo(recorrido, "Ramas");
            foreach (var b in DisenoNivel3.Ramas)
            {
                var rama = Suelo(ramas, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, colorRama);
                if (escenario != null)
                {
                    Apagar(rama, ConstructorEscenarioMundo3.VestirRama(escenario, NombreDibujo(b.Nombre), b.Desde, b.Hasta, b.Arriba));
                }
            }

            if (escenario != null)
            {
                foreach (var decorado in DisenoNivel3.Decorados)
                {
                    ConstructorEscenarioMundo3.Decorar(escenario, decorado);
                }
            }

            // Las podridas ceden: el prefab de la rama podrida, con el pivote en su superficie. A
            // oscuras son del color de las sanas.
            var podridas = Hijo(recorrido, "RamasPodridas");
            foreach (var b in DisenoNivel3.RamasPodridas)
            {
                Instanciar(escena, podridas, GeneradorMecanicas.RamaPodrida, b.Nombre, (b.Desde + b.Hasta) * 0.5f, b.Arriba);
            }

            ColocarMecanicasNivel3(escena, recorrido, escenario != null);
            ColocarCienagaNivel3(recorrido, escenario);
            ColocarOscuridadNivel3(escena);

            // Las ramas de la arena las atraviesa el jefe (ArenaJefe.plataformas).
            var plataformasArena = new List<Collider2D>();
            var contenedorArena = Hijo(recorrido, "PlataformasArena");
            foreach (var b in DisenoNivel3.PlataformasArena)
            {
                var rama = Suelo(contenedorArena, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, colorRama);
                plataformasArena.Add(rama.GetComponent<Collider2D>());
                if (escenario != null)
                {
                    Apagar(rama, ConstructorEscenarioMundo3.VestirRama(escenario, NombreDibujo(b.Nombre), b.Desde, b.Hasta, b.Arriba));
                }
            }

            ColocarArbolesNivel3(recorrido, escenario != null);
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
        // cuelgan de lo alto de sus ramas, por delante de ellas. Con arte, la cinta va atada por el
        // nudo en el centro de su rama y se mueve al viento.
        private static void ColocarMecanicasNivel3(Scene escena, Transform recorrido, bool conArte)
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
                var conDibujo = conArte && ConstructorEscenarioMundo3.ColocarCinta(cintas, $"Cinta_{i + 1}",
                    new Vector2(nudo.x, nudo.y - DisenoNivel3.GrosorRama * 0.5f), ConstructorEscenarioMundo3.OrdenSobreBloques) != null;
                if (!conDibujo)
                {
                    Visual(cintas, $"Cinta_{i + 1}", new Vector2(nudo.x, nudo.y + 0.05f - tamano.y * 0.5f), tamano, new Color(0.95f, 0.95f, 0.92f, 1f), 1);
                }
            }
        }

        /// <summary>Nombre de la zona de la pista de la ciénaga.</summary>
        public const string NombrePistaDeLaCienaga = "PistaDeLaCienaga";

        /// <summary>Las marañas, por delante de Kael (10), que queda dentro de ellas, y por detrás del agua.</summary>
        public const int OrdenMarana = 15;

        /// <summary>El interior del árbol hundido, detrás de los túneles: por detrás de Kael y del atrezo.</summary>
        public const int OrdenFondoTunel = -2;

        /// <summary>Las raíces del interior de los túneles, oscurecidas: la maraña se lee contra ellas.</summary>
        public static readonly Color ColorFondoTunel = new Color(0.3f, 0.3f, 0.35f, 1f);

        /// <summary>Lo que se ve del agua de la ciénaga sin su arte: un velo verde oscuro.</summary>
        public static readonly Color ColorAguaSinArteNivel3 = new Color(0.1f, 0.2f, 0.12f, 0.45f);

        /// <summary>Lo que se ve de una maraña sin su arte: un rectángulo de raíces oscuras.</summary>
        public static readonly Color ColorMaranaSinArte = new Color(0.2f, 0.12f, 0.25f, 0.85f);

        /// <summary>
        /// La ciénaga: el agua (su superficie dibujada sobre un cuerpo liso o, sin arte, un velo), el
        /// interior oscuro de los túneles, las marañas que sujetan (zonas del tipo maraña, con su
        /// dibujo) y, en la orilla, la pista, que vuelve a sonar si una maraña tiene a Kael sujeto.
        /// </summary>
        private static void ColocarCienagaNivel3(Transform recorrido, Transform escenario)
        {
            var cienaga = Hijo(recorrido, "Cienaga");

            // Dentro de los túneles se ve el interior del árbol hundido, no el bosque del fondo: sus
            // raíces, oscurecidas. Contra ellas se distingue la maraña.
            var tunel = 0;
            foreach (var techo in DisenoNivel3.RaicesDelArbolHundido)
            {
                tunel++;
                var nombre = $"FondoDelTunel{tunel}";
                var interior = escenario != null
                    ? ConstructorEscenarioMundo3.VestirMuro(escenario, nombre, techo.Desde, techo.Hasta, DisenoNivel3.FondoCienaga, techo.Abajo, true)
                    : null;
                if (interior == null)
                {
                    Visual(cienaga, nombre, new Vector2((techo.Desde + techo.Hasta) * 0.5f, (DisenoNivel3.FondoCienaga + techo.Abajo) * 0.5f),
                        new Vector2(techo.Hasta - techo.Desde, techo.Abajo - DisenoNivel3.FondoCienaga), ColorFondoTunel * 0.5f, OrdenFondoTunel);
                    continue;
                }

                interior.color = ColorFondoTunel;
                interior.sortingOrder = OrdenFondoTunel;
            }

            var superficie = ConstructorMecanicasMundo3.SuperficieDelAgua;
            foreach (var zona in DisenoNivel3.Aguas)
            {
                if (superficie == null)
                {
                    ZonaDeMovimiento(cienaga, zona, ZonaModificadorMovimiento.TIPOS.AGUA, ColorAguaSinArteNivel3, SpriteCuadrado);
                    continue;
                }

                ZonaDeMovimiento(cienaga, zona, ZonaModificadorMovimiento.TIPOS.AGUA, Color.clear, null);
                DibujarAgua(cienaga, zona, superficie, ConstructorMecanicasMundo3.ColorAgua, ConstructorMecanicasMundo3.FondoDeLaSuperficie,
                    ConstructorMecanicasMundo3.SegundosAgua);
            }

            var maranas = new List<Marana>();
            foreach (var zona in DisenoNivel3.Maranas)
            {
                maranas.Add(CrearMarana(cienaga, zona));
            }

            var pista = DisenoNivel3.PistaDeLaCienaga;
            var zonaPista = Bloque(cienaga, NombrePistaDeLaCienaga, pista.Desde, pista.Hasta, pista.Abajo, pista.Arriba, 0, Color.clear, null);
            zonaPista.GetComponent<BoxCollider2D>().isTrigger = true;
            var dialogo = zonaPista.AddComponent<ZonaDeDialogo>();
            Asignar(dialogo, "momento", p => p.enumValueIndex = (int)Fragmentum.Narrative.MOMENTOS_DIALOGO.PISTA);
            Asignar(dialogo, "maranas", p =>
            {
                p.arraySize = maranas.Count;
                for (var i = 0; i < maranas.Count; i++)
                {
                    p.GetArrayElementAtIndex(i).objectReferenceValue = maranas[i];
                }
            });
        }

        // Una maraña: un trigger del tipo maraña, sin escala (sus dibujos son hijos), con un dibujo
        // por cada 2 u, colgado del techo del túnel; el segundo, en espejo, para que el corte case
        // consigo mismo. Sin arte, un rectángulo oscuro.
        private static Marana CrearMarana(Transform padre, DisenoNivel2.Zona zona)
        {
            var objeto = new GameObject(zona.Nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.position = new Vector3((zona.Desde + zona.Hasta) * 0.5f, (zona.Abajo + zona.Arriba) * 0.5f, 0f);
            var caja = objeto.AddComponent<BoxCollider2D>();
            caja.size = new Vector2(zona.Hasta - zona.Desde, zona.Arriba - zona.Abajo);
            caja.isTrigger = true;

            var tipo = objeto.AddComponent<ZonaModificadorMovimiento>();
            Asignar(tipo, "tipo", p => p.enumValueIndex = (int)ZonaModificadorMovimiento.TIPOS.MARANA);
            var marana = objeto.AddComponent<Marana>();

            var reposo = ConstructorMecanicasMundo3.MaranaEnReposo;
            var atrapa = ConstructorMecanicasMundo3.MaranaQueAtrapa;
            if (reposo == null || atrapa == null)
            {
                Visual(objeto.transform, "SinArte", Vector2.zero, caja.size, ColorMaranaSinArte, OrdenMarana);
                return marana;
            }

            var dibujos = new List<SpriteRenderer>();
            var piezas = Mathf.Max(1, Mathf.RoundToInt((zona.Hasta - zona.Desde) / DisenoNivel3.AnchoDibujoMarana));
            for (var i = 0; i < piezas; i++)
            {
                var pieza = new GameObject($"Dibujo{i + 1}");
                pieza.transform.SetParent(objeto.transform, false);
                pieza.transform.position = new Vector3(zona.Desde + (i + 0.5f) * DisenoNivel3.AnchoDibujoMarana, zona.Arriba, 0f);
                var dibujo = pieza.AddComponent<SpriteRenderer>();
                dibujo.sprite = reposo[0];
                dibujo.flipX = i % 2 == 1;
                dibujo.sortingOrder = OrdenMarana;
                dibujos.Add(dibujo);
            }

            Asignar(marana, "dibujos", p => ListaDeObjetos(p, dibujos.ToArray()));
            Asignar(marana, "reposo", p => ListaDeObjetos(p, reposo));
            Asignar(marana, "atrapa", p => ListaDeObjetos(p, atrapa));
            return marana;
        }

        private static void ListaDeObjetos(SerializedProperty propiedad, Object[] objetos)
        {
            propiedad.arraySize = objetos.Length;
            for (var i = 0; i < objetos.Length; i++)
            {
                propiedad.GetArrayElementAtIndex(i).objectReferenceValue = objetos[i];
            }
        }

        /// <summary>Con su dibujo, el rectángulo de color del bloque se apaga (la colisión sigue siendo la suya).</summary>
        private static void Apagar(GameObject bloque, SpriteRenderer dibujo)
        {
            if (dibujo != null && bloque.TryGetComponent<SpriteRenderer>(out var liso))
            {
                liso.enabled = false;
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

        // Los árboles del claro: de pie en su suelo, sin collider (Kael pasa por delante). Con arte,
        // el tocón del árbol muerto, al doble.
        private static void ColocarArbolesNivel3(Transform recorrido, bool conArte)
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
                if (!conArte || ConstructorEscenarioMundo3.ColocarArbol(arbol, "Tronco", arbol.position) == null)
                {
                    Visual(arbol, "Tronco", new Vector2(0f, tamano.y * 0.5f), tamano, ColorArbolSinArte, -1);
                }
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
                if (guardia.Tipo == DisenoNivel3.TIPOS_ENEMIGO.ESPECTRO)
                {
                    // Los Ecos de este mundo, a la escala de los del Nivel 1.
                    enemigo.transform.localScale = new Vector3(DisenoNivel3.EscalaEspectro.x, DisenoNivel3.EscalaEspectro.y, 1f);
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
