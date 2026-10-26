using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Narrative;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Fragmentum.EditorTools.UtilidadesEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El montaje del Nivel 4 (Culpa, <see cref="DisenoNivel4"/>). Como los de los niveles 2 y 3, se
    /// vuelve a montar entero cada vez que sube <see cref="DisenoNivel4.Version"/>: el nivel crece por
    /// fases (recorrido, mecánicas, enemigos y jefe). Mientras la versión no cambie, preparar otra vez
    /// no cambia nada.
    ///
    /// Fase A: los bloques con los colores de la paleta (el tileset del campo llegará con el arte); los
    /// tejados; la torre del campanario y los restos del puente, que no agarran
    /// (<see cref="SinAgarre"/>); el balcón de los Artilleros; las coberturas; el agua del río y sus
    /// alambradas sumergidas (las marañas del Mundo 3, sin dibujo); los enemigos con su ruta; los
    /// checkpoints, la salida y la plaza del Juez Silente.
    ///
    /// Fase B: los barriles explosivos y la mina del río (prefabs de <see cref="GeneradorMecanicas"/>),
    /// las cuatro memorias con su cinemática y su zona, el <see cref="SistemaConsecuencias"/> que las
    /// escucha, las dos pistas y la línea del río.
    ///
    /// Fase C: los Artilleros (los del campanario, con su turno) y el Verdugo, con el patio como
    /// territorio, en sus prefabs de <see cref="GeneradorEnemigos"/>.
    ///
    /// Fase D: en la plaza, las cuatro marcas de las memorias (<see cref="MarcaDeMemoria"/>) y la pared en
    /// la que el Juez Silente las proyecta, asignadas al Juez.
    /// </summary>
    public static partial class PreparadorNivel
    {
        /// <summary>Hijo del recorrido que dice qué versión del diseño está montada.</summary>
        public static string MarcaVersionNivel4 => $"Diseño v{DisenoNivel4.Version}";

        /// <summary>Lo que se ve de una cobertura sin su arte: escombros y sacos, más claros que el suelo.</summary>
        public static readonly Color ColorCoberturaSinArte = new Color(0.55f, 0.48f, 0.38f, 1f);

        /// <summary>Lo que se ve del agua del río sin su arte: un velo pardo.</summary>
        public static readonly Color ColorAguaSinArteNivel4 = new Color(0.25f, 0.2f, 0.12f, 0.5f);

        /// <summary>Lo que se ve de una alambrada sumergida sin su arte: un rectángulo gris de alambre.</summary>
        public static readonly Color ColorAlambradaSinArte = new Color(0.4f, 0.4f, 0.42f, 0.75f);

        /// <summary>Lo que se ve de una memoria sin su arte: un cuadrado ámbar que late (lo que hay que mirar).</summary>
        public static readonly Color ColorMemoriaSinArte = new Color(0.88f, 0.69f, 0.38f, 1f);

        /// <summary>Lo que mide una memoria sin su arte.</summary>
        public static readonly Vector2 TamanoMemoria = new Vector2(0.8f, 0.8f);

        /// <summary>El objeto de la escena que lleva el <see cref="SistemaConsecuencias"/>.</summary>
        public const string NombreConsecuencias = "Consecuencias";

        /// <summary>¿La escena tiene montado el recorrido de la versión actual de <see cref="DisenoNivel4"/>?</summary>
        public static bool RecorridoNivel4AlDia(Scene escena)
        {
            var recorrido = RaizRecorrido(escena);
            return recorrido != null && recorrido.Find(MarcaVersionNivel4) != null;
        }

        /// <summary>
        /// Monta el recorrido de <see cref="DisenoNivel4"/>: borra lo de un montaje anterior (y lo
        /// provisional, si lo hubiera), levanta los bloques, los tejados y las coberturas, el río, coloca
        /// los enemigos y pone el inicio, los checkpoints, la salida, la arena, la zona mortal y la
        /// cámara del diseño.
        /// </summary>
        private static void MontarRecorridoNivel4(Scene escena, LevelData nivel, Transform raiz)
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
            Hijo(recorrido, MarcaVersionNivel4);

            // La paleta del Anexo A: gris parduzco, marrón quemado, verde oliva muerto y rojo desaturado.
            var colorSuelo = FabricaColor(DePaleta(nivel, 1, new Color(0.36f, 0.24f, 0.12f)), 1.2f);
            var colorPared = FabricaColor(DePaleta(nivel, 0, new Color(0.24f, 0.18f, 0.12f)), 1.4f);
            var colorTejado = FabricaColor(DePaleta(nivel, 2, new Color(0.29f, 0.36f, 0.18f)), 1.3f);

            // Con el tileset cortado (fase E, segunda tanda), cada bloque apaga su rectángulo de color y lleva su
            // dibujo: la tierra quemada, la piedra del puente, las vigas de los tejados, el campanario, las
            // coberturas y el tanque.
            var bloques = Hijo(recorrido, "Bloques");
            var escenario = ConstructorEscenarioMundo4.Completo ? Hijo(recorrido, NombreEscenario) : null;
            var campanario = escenario != null ? VestirCampanario(escenario) : null;
            foreach (var b in DisenoNivel4.Suelos)
            {
                var suelo = Suelo(bloques, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, colorSuelo);
                if (escenario != null)
                {
                    Apagar(suelo, VestirSueloNivel4(escenario, b));
                }
            }

            foreach (var b in DisenoNivel4.Paredes)
            {
                var pared = Bloque(bloques, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, Capa(Capas.Wall), colorPared, SpriteCuadrado);
                if (escenario != null)
                {
                    Apagar(pared, b.Nombre == "TorreDelCampanario"
                        ? campanario
                        : ConstructorEscenarioMundo4.VestirMuro(escenario, NombreDibujo(b.Nombre), b.Desde, b.Hasta, b.Abajo, b.Arriba));
                }

                // La torre y los restos del puente: se choca con ellos, pero no se sube con el salto de pared.
                if (DisenoNivel4.NoAgarra(b.Nombre))
                {
                    pared.AddComponent<SinAgarre>();
                }
            }

            var tejados = Hijo(recorrido, "Tejados");
            foreach (var b in DisenoNivel4.Tejados)
            {
                var tejado = Suelo(tejados, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, colorTejado);
                if (escenario != null)
                {
                    Apagar(tejado, ConstructorEscenarioMundo4.VestirTejado(escenario, NombreDibujo(b.Nombre), b.Desde, b.Hasta, b.Arriba));
                }
            }

            // El balcón lo dibuja el campanario.
            var balcon = DisenoNivel4.Balcon;
            Apagar(Suelo(tejados, balcon.Nombre, balcon.Desde, balcon.Hasta, balcon.Abajo, balcon.Arriba, colorPared), campanario);

            // Las coberturas son suelo: se pisan y las balas se paran contra ellas. Con arte, cada una con su
            // dibujo, por delante de los bloques: Kael se agacha delante de ellas.
            var coberturas = Hijo(recorrido, "Coberturas");
            foreach (var b in DisenoNivel4.Coberturas)
            {
                var cobertura = Suelo(coberturas, b.Nombre, b.Desde, b.Hasta, b.Abajo, b.Arriba, ColorCoberturaSinArte);
                var pieza = CoberturaDe(b.Nombre);
                if (pieza?.Sprite != null)
                {
                    Apagar(cobertura, ConstructorEscenarioMundo4.ColocarSprite(escenario != null ? escenario : coberturas, NombreDibujo(b.Nombre),
                        pieza.Sprite, new Vector2((b.Desde + b.Hasta) * 0.5f, b.Abajo), ConstructorEscenarioMundo4.OrdenCoberturas));
                }
            }

            if (escenario != null)
            {
                foreach (var decorado in DisenoNivel4.Decorados)
                {
                    ConstructorEscenarioMundo4.Decorar(escenario, decorado);
                }
            }

            ColocarRioNivel4(recorrido);
            ColocarMecanicasNivel4(escena, recorrido);
            ColocarEnemigosNivel4(escena, recorrido);

            var gestor = Buscar<LevelManager>(escena);
            if (gestor != null)
            {
                if (new SerializedObject(gestor).FindProperty("puntoInicio").objectReferenceValue is Transform inicio)
                {
                    inicio.position = new Vector3(DisenoNivel4.Inicio, DisenoNivel4.SueloBajo(DisenoNivel4.Inicio) + AlturaPies, 0f);
                }

                // El río está a y −3,5: más hondo que lo que enseña la cámara por defecto con su suelo.
                Asignar(gestor, "alturaCamara", p => p.vector2Value = DisenoNivel4.AlturaCamara);
            }

            var camara = Buscar<Camera>(escena);
            if (camara != null)
            {
                camara.orthographicSize = DisenoNivel4.TamanoCamara;
            }

            // El primero, en el refugio, lleva el eco de memoria del guion.
            var checkpoints = Hijo(raiz, "Checkpoints");
            for (var i = 0; i < DisenoNivel4.Checkpoints.Length; i++)
            {
                var punto = DisenoNivel4.Checkpoints[i];
                var checkpoint = CrearCheckpoint(checkpoints, $"L{nivel.id}_CP{i + 1}", punto.x, nivel, punto.y);
                if (checkpoint.Id == CheckpointDelEcoDeMemoria(nivel))
                {
                    Asignar(checkpoint, "ecoDeMemoria", p => p.boolValue = true);
                }
            }

            CrearSalida(raiz, DisenoNivel4.Salida, nivel, DisenoNivel4.SueloArena);
            CrearArenaNivel4(escena, raiz, nivel);
            ZonaMortal(raiz, DisenoNivel4.AnchoZonaMortal.x, DisenoNivel4.AnchoZonaMortal.y, DisenoNivel4.ZonaMortal);
        }

        /// <summary>La pieza de las coberturas que dibuja el bloque <paramref name="nombre"/>, o null.</summary>
        public static ConstructorMecanicasMundo4.Pieza CoberturaDe(string nombre) =>
            nombre.StartsWith("Escombro") ? ConstructorMecanicasMundo4.Escombro
            : nombre == "SacosDelRefugio" ? ConstructorMecanicasMundo4.SacosLargos
            : nombre.StartsWith("SacosDelArtillero") ? ConstructorMecanicasMundo4.SacosBajos
            : nombre.StartsWith("Chimenea") ? ConstructorMecanicasMundo4.Chimenea
            : nombre == "Tanque" ? ConstructorMecanicasMundo4.Tanque
            : null;

        // Un suelo: el tanque volcado, con su dibujo; el pilar del puente, de piedra; lo demás, de tierra
        // quemada con su ceniza (el escombro de la bifurcación también: es un montón de tierra).
        private static SpriteRenderer VestirSueloNivel4(Transform escenario, DisenoNivel1.Bloque b)
        {
            var tanque = b.Nombre == "Tanque" ? ConstructorMecanicasMundo4.Tanque.Sprite : null;
            if (tanque != null)
            {
                return ConstructorEscenarioMundo4.ColocarSprite(escenario, NombreDibujo(b.Nombre), tanque, new Vector2((b.Desde + b.Hasta) * 0.5f, b.Abajo),
                    ConstructorEscenarioMundo4.OrdenCoberturas);
            }

            return b.Nombre == "Pilar"
                ? ConstructorEscenarioMundo4.VestirMuro(escenario, NombreDibujo(b.Nombre), b.Desde, b.Hasta, b.Abajo, b.Arriba)
                : ConstructorEscenarioMundo4.VestirSuelo(escenario, NombreDibujo(b.Nombre), b.Desde, b.Hasta, b.Abajo, b.Arriba);
        }

        // El campanario, con el pie de su torre en el de la torre del diseño: dibuja la torre, su arco y el balcón.
        private static SpriteRenderer VestirCampanario(Transform escenario)
        {
            var sprite = ConstructorMecanicasMundo4.Campanario;
            var torre = DisenoNivel4.Paredes.Single(p => p.Nombre == "TorreDelCampanario");
            return sprite != null
                ? ConstructorEscenarioMundo4.ColocarSprite(escenario, NombreDibujo(torre.Nombre), sprite,
                    new Vector2(torre.Desde, DisenoNivel4.PisadaDe("BajoElCampanario").Y), ConstructorEscenarioMundo4.OrdenBloques)
                : null;
        }

        /// <summary>
        /// El río: su agua (con arte, su superficie parda y su cuerpo; sin arte, un velo pardo) y las alambradas
        /// sumergidas, que sujetan como las marañas de la ciénaga del Mundo 3 (una zona del tipo maraña).
        /// </summary>
        private static void ColocarRioNivel4(Transform recorrido)
        {
            var rio = Hijo(recorrido, "Rio");
            var superficie = ConstructorMecanicasMundo4.SuperficieDelAgua;
            foreach (var zona in DisenoNivel4.Aguas)
            {
                if (superficie == null)
                {
                    ZonaDeMovimiento(rio, zona, ZonaModificadorMovimiento.TIPOS.AGUA, ColorAguaSinArteNivel4, SpriteCuadrado);
                    continue;
                }

                ZonaDeMovimiento(rio, zona, ZonaModificadorMovimiento.TIPOS.AGUA, Color.clear, null);
                DibujarAgua(rio, zona, superficie, ConstructorMecanicasMundo4.ColorAgua, ConstructorMecanicasMundo4.FondoDeLaSuperficie,
                    ConstructorMecanicasMundo4.SegundosAgua);
            }

            foreach (var zona in DisenoNivel4.Alambradas)
            {
                CrearAlambrada(rio, zona);
            }
        }

        // Los barriles, la mina, las memorias con el sistema que las escucha, las pistas y la línea del río.
        private static void ColocarMecanicasNivel4(Scene escena, Transform recorrido)
        {
            var barriles = Hijo(recorrido, "Barriles");
            for (var i = 0; i < DisenoNivel4.Barriles.Length; i++)
            {
                var pie = DisenoNivel4.Barriles[i];
                Instanciar(escena, barriles, GeneradorMecanicas.BarrilExplosivo, $"Barril_{i + 1}", pie.x, pie.y);
            }

            var minas = Hijo(recorrido, "Minas");
            for (var i = 0; i < DisenoNivel4.Minas.Length; i++)
            {
                var ancla = DisenoNivel4.Minas[i];
                Instanciar(escena, minas, GeneradorMecanicas.Mina, $"Mina_{i + 1}", ancla.x, ancla.y);
            }

            var memorias = Hijo(recorrido, "Memorias");
            for (var i = 0; i < DisenoNivel4.Memorias.Length; i++)
            {
                CrearMemoria(memorias, DisenoNivel4.Memorias[i], ConstructorMecanicasMundo4.Memorias[i]);
            }

            var consecuencias = Hijo(recorrido, NombreConsecuencias).gameObject.AddComponent<SistemaConsecuencias>();
            consecuencias.agresividad = DisenoNivel4.AgresividadPorMemoria;
            consecuencias.vidaDelJuezSinMemorias = DisenoNivel4.VidaDelJuezSinMemorias;

            var pistas = Hijo(recorrido, "Pistas");
            CrearPista(pistas, DisenoNivel4.PistaDelBarril, MOMENTOS_DIALOGO.PISTA);
            CrearPista(pistas, DisenoNivel4.PistaDeLaTierraDeNadie, MOMENTOS_DIALOGO.PISTA_INVISIBILIDAD);
            CrearPista(pistas, DisenoNivel4.LineaDelRio, MOMENTOS_DIALOGO.ORILLA);
        }

        /// <summary>Las memorias, por delante de las coberturas y por detrás de Kael; su brillo, encima.</summary>
        public const int OrdenMemoria = ConstructorEscenarioMundo4.OrdenCoberturas + 1;

        /// <summary>Las losas de la plaza, metidas en el suelo: por delante de su tierra y de su ceniza.</summary>
        public const int OrdenLosa = ConstructorEscenarioMundo4.OrdenSobreBloques;

        // Una memoria por su pie: su dibujo (sin arte, un cuadrado ámbar), su cinemática y la zona de los
        // enemigos que se vuelven más agresivos al recordarla. Con arte, el objeto apagado y, encima, el mismo
        // brillando: es el brillo el que late (su borde ámbar y sus chispas) y el que se apaga al recordarla.
        private static void CrearMemoria(Transform padre, DisenoNivel4.Memoria memoria, ConstructorMecanicasMundo4.DibujoMemoria dibujo)
        {
            var objeto = new GameObject($"Memoria_{memoria.Nombre}");
            objeto.transform.SetParent(padre, false);
            objeto.transform.position = new Vector3(memoria.X, memoria.Y, 0f);
            var conArte = dibujo != null && dibujo.Completo;
            SpriteRenderer brillo;
            if (conArte)
            {
                ConstructorEscenarioMundo4.ColocarSprite(objeto.transform, "Dibujo", dibujo.Apagada, memoria.Posicion, OrdenMemoria);
                brillo = ConstructorEscenarioMundo4.ColocarSprite(objeto.transform, "Brillo", dibujo.Brillando, memoria.Posicion, OrdenMemoria + 1);
            }
            else
            {
                brillo = Visual(objeto.transform, "Brillo", new Vector2(0f, TamanoMemoria.y * 0.5f), TamanoMemoria, ColorMemoriaSinArte, 1)
                    .GetComponent<SpriteRenderer>();
            }

            var componente = objeto.AddComponent<MemoriaInteractuable>();
            componente.distancia = DisenoNivel4.DistanciaRecordar;
            var cinematica = GeneradorNarrativa.CargarCinematicaMemoria(memoria.Nombre);
            if (cinematica == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta la cinemática de la memoria {memoria.Nombre}: ejecuta Fragmentum → Narrativa → Generar cinemáticas, diálogos y registro.");
            }

            Asignar(componente, "id", p => p.stringValue = memoria.Nombre);
            Asignar(componente, "cinematica", p => p.objectReferenceValue = cinematica);
            Asignar(componente, "zona", p => p.rectValue = memoria.Zona.Rect);
            Asignar(componente, "brillo", p => p.objectReferenceValue = brillo);
            if (conArte)
            {
                Asignar(componente, "colorBrillo", p => p.colorValue = Color.white);
                Asignar(componente, "colorApagada", p => p.colorValue = Color.clear);
            }
        }

        // Una zona en la que Kael dice una línea al entrar (una pista o la de la orilla del río).
        private static void CrearPista(Transform padre, DisenoNivel2.Zona zona, MOMENTOS_DIALOGO momento)
        {
            var objeto = Bloque(padre, zona.Nombre, zona.Desde, zona.Hasta, zona.Abajo, zona.Arriba, 0, Color.clear, null);
            objeto.GetComponent<BoxCollider2D>().isTrigger = true;
            var dialogo = objeto.AddComponent<ZonaDeDialogo>();
            Asignar(dialogo, "momento", p => p.enumValueIndex = (int)momento);
        }

        // Una alambrada: un trigger del tipo maraña, sin escala (su dibujo es hijo), con la pieza que la
        // dibuja (sin fotogramas mientras no llegue su arte).
        private static Marana CrearAlambrada(Transform padre, DisenoNivel2.Zona zona)
        {
            var objeto = new GameObject(zona.Nombre);
            objeto.transform.SetParent(padre, false);
            objeto.transform.position = new Vector3((zona.Desde + zona.Hasta) * 0.5f, (zona.Abajo + zona.Arriba) * 0.5f, 0f);
            var caja = objeto.AddComponent<BoxCollider2D>();
            caja.size = new Vector2(zona.Hasta - zona.Desde, zona.Arriba - zona.Abajo);
            caja.isTrigger = true;

            var tipo = objeto.AddComponent<ZonaModificadorMovimiento>();
            Asignar(tipo, "tipo", p => p.enumValueIndex = (int)ZonaModificadorMovimiento.TIPOS.MARANA);
            var alambrada = objeto.AddComponent<Marana>();

            var reposo = ConstructorMecanicasMundo4.AlambradaEnReposo;
            var atrapa = ConstructorMecanicasMundo4.AlambradaQueAtrapa;
            if (reposo == null || atrapa == null)
            {
                Visual(objeto.transform, "SinArte", Vector2.zero, caja.size, ColorAlambradaSinArte, OrdenMarana);
                return alambrada;
            }

            // Cuelga del techo del ojo y llena su alto: la de 3 u repite su dibujo hacia abajo.
            var pieza = new GameObject("Dibujo1");
            pieza.transform.SetParent(objeto.transform, false);
            pieza.transform.position = new Vector3((zona.Desde + zona.Hasta) * 0.5f, zona.Arriba, 0f);
            var dibujo = pieza.AddComponent<SpriteRenderer>();
            dibujo.sprite = reposo[0];
            dibujo.drawMode = SpriteDrawMode.Tiled;
            dibujo.tileMode = SpriteTileMode.Continuous;
            dibujo.size = new Vector2(zona.Hasta - zona.Desde, zona.Arriba - zona.Abajo);
            dibujo.sortingOrder = OrdenMarana;

            Asignar(alambrada, "dibujos", p => ListaDeObjetos(p, new Object[] { dibujo }));
            Asignar(alambrada, "reposo", p => ListaDeObjetos(p, reposo));
            Asignar(alambrada, "atrapa", p => ListaDeObjetos(p, atrapa));
            return alambrada;
        }

        // Los Penitentes, los Artilleros y el Verdugo, cada uno con su ruta (la de los Artilleros es su sitio).
        private static void ColocarEnemigosNivel4(Scene escena, Transform recorrido)
        {
            var enemigos = Hijo(recorrido, "Enemigos");
            var rutas = Hijo(recorrido, "Rutas");
            var prefabs = new Dictionary<DisenoNivel4.TIPOS_ENEMIGO, GameObject>
            {
                [DisenoNivel4.TIPOS_ENEMIGO.PENITENTE] = GeneradorEnemigos.CargarPrefab("Penitente"),
                [DisenoNivel4.TIPOS_ENEMIGO.ARTILLERO] = GeneradorEnemigos.CargarPrefab("Artillero"),
                [DisenoNivel4.TIPOS_ENEMIGO.VERDUGO] = GeneradorEnemigos.CargarPrefab("Verdugo"),
            };

            foreach (var guardia in DisenoNivel4.Enemigos)
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
                if (guardia.Tipo == DisenoNivel4.TIPOS_ENEMIGO.PENITENTE)
                {
                    // Los Ecos de este mundo, a la escala de los del Nivel 1.
                    enemigo.transform.localScale = new Vector3(DisenoNivel4.EscalaPenitente.x, DisenoNivel4.EscalaPenitente.y, 1f);
                }

                // Los del campanario se turnan: el segundo empieza su ráfaga más tarde.
                var artillero = enemigo.GetComponent<Artillero>();
                if (artillero != null && guardia.Turno > 0f)
                {
                    Asignar(artillero, "turno", p => p.floatValue = guardia.Turno);
                }

                // El Verdugo busca a Kael dentro de su patio.
                var verdugo = enemigo.GetComponent<Verdugo>();
                if (verdugo != null)
                {
                    Asignar(verdugo, "limites", p => p.vector2Value = DisenoNivel4.LimitesPatio);
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
        /// La plaza: la entrada pasada la puerta, la puerta en el borde de la sala (se cierra detrás del
        /// jugador) y el Juez Silente al fondo, en el centro. La plaza es llana: no tiene plataformas.
        /// </summary>
        private static void CrearArenaNivel4(Scene escena, Transform raiz, LevelData nivel)
        {
            var arena = new GameObject("Arena").transform;
            arena.SetParent(raiz, false);
            arena.position = new Vector3(DisenoNivel4.Entrada, DisenoNivel4.SueloArena, 0f);

            var alto = DisenoNivel4.TechoArena - DisenoNivel4.SueloArena;
            var entrada = arena.gameObject.AddComponent<BoxCollider2D>();
            entrada.isTrigger = true;
            entrada.size = new Vector2(1.2f, alto);
            entrada.offset = new Vector2(0f, alto * 0.5f);

            var colorPuerta = DePaleta(nivel, 3, Color.gray);
            var puerta = Bloque(arena, "Puerta", DisenoNivel4.PuertaDesde, DisenoNivel4.PuertaHasta, DisenoNivel4.SueloArena, DisenoNivel4.TechoArena,
                Capa(Capas.Wall), FabricaColor(colorPuerta, 1.2f), SpriteCuadrado);
            ConstructorPuertas.VestirVerja(puerta, nivel, DisenoNivel4.PuertaDesde, DisenoNivel4.PuertaHasta, DisenoNivel4.SueloArena, DisenoNivel4.TechoArena);
            puerta.SetActive(false);

            GameObject jefe = null;
            var prefab = nivel.jefe != null ? GeneradorJefes.CargarPrefab(nivel.jefe) : null;
            if (prefab != null)
            {
                jefe = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                jefe.name = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(prefab));
                jefe.transform.SetParent(arena, true);
                jefe.transform.position = new Vector3(DisenoNivel4.Jefe, DisenoNivel4.SueloArena + AlturaPies, 0f);
            }
            else
            {
                Debug.LogWarning("[Fragmentum] No existe el prefab del jefe del Nivel 4: la arena se crea sin jefe. Ejecuta Fragmentum → Jefes → Generar datos y prefabs de jefes.");
            }

            var componente = arena.gameObject.AddComponent<ArenaJefe>();
            Asignar(componente, "jefe", p => p.objectReferenceValue = jefe != null ? jefe.GetComponent<BossController>() : null);
            Asignar(componente, "puertas", p =>
            {
                p.arraySize = 1;
                p.GetArrayElementAtIndex(0).objectReferenceValue = puerta;
            });
            Asignar(componente, "limites", p => p.vector2Value = DisenoNivel4.LimitesArena);

            ColocarMemoriasDeLaPlaza(arena, jefe != null ? jefe.GetComponent<JuezSilente>() : null);
        }

        /// <summary>El orden de dibujo de la pared de las memorias: detrás del Juez y de Kael.</summary>
        public const int OrdenProyeccion = -3;

        /// <summary>¿Hay arte para las marcas de la plaza (la losa y los cuatro objetos)?</summary>
        public static bool HayArteDeLasMarcas =>
            ConstructorMecanicasMundo4.Losa.Completo && ConstructorMecanicasMundo4.Memorias.All(m => m.Apagada != null);

        /// <summary>
        /// El color de la marca <paramref name="i"/>: el de su memoria mientras no haya arte; con arte, blanco (la losa
        /// encendida, tal como se dibujó). La proyección lleva el color de su marca.
        /// </summary>
        public static Color ColorDeMarca(int i) => HayArteDeLasMarcas ? Color.white : DisenoNivel4.ColoresMemorias[i];

        /// <summary>
        /// Las cuatro marcas del suelo de la plaza, una por memoria, y la pared del fondo en la que el Juez las
        /// proyecta. Todo se le asigna al Juez. Sin arte, cada marca es un rectángulo del color de su memoria, y la
        /// pared, un rectángulo de ese color. Con arte, cada memoria espera sobre su losa (<c>Prompts_Mundo4.md</c>,
        /// E9): la losa apagada, metida en el suelo, y encima la encendida, que solo se ve mientras la marca brilla;
        /// sobre ella, el objeto apagado, para que se sepa de quién es cada una. Y la pared proyecta la imagen de
        /// la memoria (la de su cinemática).
        /// </summary>
        private static void ColocarMemoriasDeLaPlaza(Transform arena, JuezSilente juez)
        {
            var contenedor = Hijo(arena, "Memorias");
            var marcas = new List<MarcaDeMemoria>();
            var conArte = HayArteDeLasMarcas;
            for (var i = 0; i < DisenoNivel4.Memorias.Length; i++)
            {
                var memoria = DisenoNivel4.Memorias[i];
                var objeto = new GameObject($"Marca_{memoria.Nombre}");
                objeto.transform.SetParent(contenedor, false);
                var pie = new Vector2(memoria.Marca, DisenoNivel4.SueloArena);
                objeto.transform.position = pie;
                SpriteRenderer dibujo;
                if (conArte)
                {
                    var losa = ConstructorMecanicasMundo4.Losa;
                    ConstructorEscenarioMundo4.ColocarSprite(objeto.transform, "Losa", losa.Apagada, pie, OrdenLosa);
                    dibujo = ConstructorEscenarioMundo4.ColocarSprite(objeto.transform, "Dibujo", losa.Brillando, pie, OrdenLosa + 1);
                    ConstructorEscenarioMundo4.ColocarSprite(objeto.transform, "Memoria", ConstructorMecanicasMundo4.Memorias[i].Apagada, pie, OrdenMemoria);
                }
                else
                {
                    var tamano = DisenoNivel4.TamanoMarca;
                    dibujo = Visual(objeto.transform, "Dibujo", new Vector2(0f, tamano.y * 0.5f), tamano, DisenoNivel4.ColoresMemorias[i], 1)
                        .GetComponent<SpriteRenderer>();
                }

                var marca = objeto.AddComponent<MarcaDeMemoria>();
                marca.memoria = i;
                marca.distancia = DisenoNivel4.DistanciaMarca;
                var color = ColorDeMarca(i);
                Asignar(marca, "juez", p => p.objectReferenceValue = juez);
                Asignar(marca, "dibujo", p => p.objectReferenceValue = dibujo);
                Asignar(marca, "color", p => p.colorValue = color);
                if (conArte)
                {
                    Asignar(marca, "opacidadApagada", p => p.floatValue = 0f);
                }

                marcas.Add(marca);
            }

            var pared = DisenoNivel4.Proyeccion;
            var proyeccion = Visual(contenedor, "Proyeccion", pared.center - (Vector2)contenedor.position, pared.size, Color.clear, OrdenProyeccion);
            var dibujoPared = proyeccion.GetComponent<SpriteRenderer>();
            dibujoPared.enabled = false;

            // Con las imágenes de las memorias, la pared es una de ellas, a lo ancho de la pared y sin deformarla.
            var imagenes = ConstructorCinematicasNivel4.ImagenesDeLasMemorias;
            if (imagenes != null)
            {
                dibujoPared.sprite = imagenes[0];
                var escala = pared.width / imagenes[0].bounds.size.x;
                proyeccion.transform.localScale = new Vector3(escala, escala, 1f);
            }

            if (juez == null)
            {
                return;
            }

            Asignar(juez, "marcas", p => ListaDeObjetos(p, marcas.ToArray()));
            Asignar(juez, "pantalla", p => p.objectReferenceValue = dibujoPared);
            Asignar(juez, "imagenes", p => ListaDeObjetos(p, imagenes ?? new Sprite[0]));
        }
    }
}
