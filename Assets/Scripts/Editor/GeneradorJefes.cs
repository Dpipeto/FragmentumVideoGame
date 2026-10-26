using System;
using System.Collections.Generic;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using UnityEditor;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Fase 7: crea los patrones de ataque, completa los datos de combate de los cinco jefes
    /// según el Anexo A y genera un prefab por jefe en <c>Assets/Prefabs/Jefes</c>.
    ///
    /// Los <c>JefeData</c> los crea la progresión (Fase 5) con su identidad; aquí solo se
    /// completan los que aún no tienen fases, para no pisar lo que se haya ajustado a mano.
    /// «Restablecer» los vuelve a poner con los valores del diseño.
    /// </summary>
    public static class GeneradorJefes
    {
        public const string CarpetaDatos = "Assets/Data/Jefes";
        public const string CarpetaPatrones = "Assets/Data/Jefes/Patrones";
        public const string CarpetaPrefabs = "Assets/Prefabs/Jefes";

        public const string NombrePavor = "EspectroDelPavor";

        /// <summary>
        /// El material de los sprites que no dependen de la luz: el Espectro del Pavor, en una arena a
        /// oscuras, tiene que verse desde cualquier sitio cuando destella.
        /// </summary>
        public const string RutaMaterialSinLuz = ConstructorFondoMundo1.RutaMaterialSprites;

        /// <summary>Cada jefe: nombre de su asset de datos y su controlador.</summary>
        public static readonly (string asset, Type controlador)[] Jefes =
        {
            ("CentinelaArdiente", typeof(CentinelaArdiente)),
            ("DoncellaLamento", typeof(DoncellaLamento)),
            ("EspectroDelPavor", typeof(EspectroDelPavor)),
            ("JuezSilente", typeof(JuezSilente)),
            ("VerdaderoSoldado", typeof(VerdaderoSoldado)),
        };

        [MenuItem("Fragmentum/Jefes/Generar datos y prefabs de jefes", priority = 140)]
        public static void MenuConstruir() => Construir();

        [MenuItem("Fragmentum/Jefes/Restablecer datos de jefes al Anexo A", priority = 141)]
        public static void MenuRestablecer()
        {
            if (EditorUtility.DisplayDialog("Fragmentum", "Se sobrescribirán las fases, la vida y los patrones de los cinco jefes con los valores del diseño. ¿Continuar?", "Restablecer", "Cancelar"))
            {
                GenerarDatos(true);
                CrearPrefabs();
            }
        }

        /// <summary>Datos y prefabs. Devuelve si todos los prefabs se guardaron.</summary>
        public static bool Construir()
        {
            AsegurarConfiguracion();
            GenerarDatos(false);
            return CrearPrefabs();
        }

        // --- Datos ---

        public static void GenerarDatos(bool sobrescribir)
        {
            AsegurarCarpeta(CarpetaPatrones);

            // Centinela Ardiente. Tras la primera prueba se sentía fácil: ahora encadena golpes,
            // salta sobre el jugador y tiene una tercera fase de ira desatada (CHANGELOG). Los
            // avisos nunca bajan de 0,3 s: más rápida, pero siempre legible.
            var espadazo = Patron<PatronEspadazo>("Espadazo", sobrescribir, p =>
            {
                p.nombre = "Espadazo";
                p.descripcion = "Se acerca, avisa y golpea delante dos veces seguidas; tras el segundo la espada queda clavada 1,2 s (ventana vulnerable).";
                p.aviso = 0.55f;
                p.danio = 1;
                p.golpes = 2;
                p.avisoEncadenado = 0.35f;
                p.color = Hex("FF6B35");
                p.dibujarZona = false; // el golpe lo enseña E2
            });

            var espadazoFuria = Patron<PatronEspadazo>("EspadazoFuria", sobrescribir, p =>
            {
                p.nombre = "Espadazo de furia";
                p.descripcion = "Combo de tres espadazos, echándose encima del jugador entre uno y otro. La ventana dura menos (0,9 s).";
                p.aviso = 0.45f;
                p.danio = 1;
                p.golpes = 3;
                p.avisoEncadenado = 0.3f;
                p.impulsoEncadenado = 1.8f;
                p.duracionVentana = 0.9f;
                p.color = Hex("FF6B35");
                p.dibujarZona = false; // el golpe lo enseña E2
            });

            var onda = Patron<PatronOndaSuelo>("OndaDeSuelo", sobrescribir, p =>
            {
                p.nombre = "Onda de suelo";
                p.descripcion = "Golpe al suelo: una onda baja recorre el piso hacia los dos lados. Se esquiva saltando.";
                p.aviso = 0.6f;
                p.danio = 1;
                p.color = Hex("CC4400");
            });

            var ondaDoble = Patron<PatronOndaSuelo>("OndaDoble", sobrescribir, p =>
            {
                p.nombre = "Onda doble";
                p.descripcion = "Dos golpes al suelo seguidos: hay que saltar una onda detrás de otra.";
                p.aviso = 0.5f;
                p.danio = 1;
                p.repeticiones = 2;
                p.intervalo = 0.55f;
                p.velocidadOnda = 10f;
                p.recuperacion = 0.5f;
                p.color = Hex("CC4400");
            });

            var erupcion = Patron<PatronPlataformasErupcion>("PlataformasEnErupcion", sobrescribir, p =>
            {
                p.nombre = "Plataformas en erupción";
                p.descripcion = "Columnas de lava bajo el jugador y a su alrededor, anunciadas antes. Obligan a moverse.";
                p.aviso = 0.75f;
                p.danio = 1;
                p.erupciones = 4;
                p.intervalo = 0.4f;
                p.color = Hex("FF3D00");
            });

            var erupcionRafaga = Patron<PatronPlataformasErupcion>("ErupcionEnRafaga", sobrescribir, p =>
            {
                p.nombre = "Erupción en ráfaga";
                p.descripcion = "Seis columnas de lava muy seguidas por toda la arena: no hay sitio en el que quedarse quieto.";
                p.aviso = 0.6f;
                p.danio = 1;
                p.erupciones = 6;
                p.intervalo = 0.28f;
                p.dispersion = 4.5f;
                p.duracion = 0.8f;
                p.recuperacion = 0.5f;
                p.color = Hex("FF3D00");
            });

            var salto = Patron<PatronSaltoAplastante>("SaltoAplastante", sobrescribir, p =>
            {
                p.nombre = "Salto aplastante";
                p.descripcion = "Salta hacia el jugador y quema al aterrizar (2 de daño), con una onda a cada lado.";
                p.aviso = 0.5f;
                p.danio = 2;
                p.tiempoVuelo = 0.8f;
                p.danioOnda = 1;
                p.color = Hex("E85D04");
                p.dibujarZona = false; // la caída la enseña J4-J5
            });

            VestirOndas(ConstructorEfectos.PrefabOnda, salto, onda, ondaDoble);
            VestirErupciones(ConstructorEfectos.PrefabErupcion, erupcion, erupcionRafaga);

            Completar("CentinelaArdiente", sobrescribir, j =>
            {
                j.vidaMaxima = 30;
                j.velocidad = 2.4f;
                j.danioContacto = 1;
                j.duracionVulnerable = 1.2f;
                j.multiplicadorVulnerable = 2f;
                j.inmuneFueraDeVulnerable = false;
                // Con arte: sin tinte, y la derrota dura lo que la muerte hasta D4 (ConstructorCentinela).
                j.tinte = Color.white;
                j.duracionDerrota = ConstructorCentinela.DuracionDerrota;
                j.tamano = new Vector2(2.2f, 3.4f);
                j.fases = new List<FaseJefe>
                {
                    Fase("Espadazo y onda de suelo", 1f, 0.7f, 1f, espadazo, onda, salto, espadazo, onda),
                    Fase("Furia: plataformas en erupción (50 %)", 0.5f, 0.5f, 1.3f, ondaDoble, espadazoFuria, erupcion, salto, espadazoFuria, erupcion),
                    Fase("Ira desatada (25 %)", 0.25f, 0.3f, 1.6f, ondaDoble, erupcionRafaga, salto, espadazoFuria, salto, ondaDoble, espadazoFuria),
                };
            });

            GenerarDoncella(sobrescribir);
            GenerarPavor(sobrescribir);
            GenerarJuez(sobrescribir);

            // Esqueleto: vida y fases del Anexo A, sin patrones todavía (Fase 10).
            Completar("VerdaderoSoldado", sobrescribir, j =>
                Esqueleto(j, 40, "5A5A5A", Fase("Combate espejo", 1f), Fase("Secciones de cada mundo (60 %)", 0.6f), Fase("Integración (30 %)", 0.3f)));

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// El Juez Silente (<c>Docs/Diseno/Nivel4_Diseno.md</c>, sección 6): cada ataque es una memoria, y
        /// solo se le hiere cuando Kael mira la suya (su marca en la plaza). Inmune fuera de esa ventana,
        /// de 3 s, en la que los golpes valen lo de siempre. Sus datos nacieron como esqueleto: se
        /// completan aquí la primera vez, y después se respeta lo que se haya ajustado a mano.
        /// </summary>
        private static void GenerarJuez(bool sobrescribir)
        {
            var rojoApagado = Hex("8B3333");

            var orden = Patron<PatronOrden>("Orden", sobrescribir, p =>
            {
                p.nombre = "La orden (la radio)";
                p.descripcion = "Señala al frente con la espada: seis explosiones de artillería caen una tras otra, avanzando por la " +
                                "plaza desde su lado. Cada una marca el suelo 0,8 s antes. Quedarse entre dos marcas o saltar.";
                p.memoria = 0;
                p.aviso = 1f;
                p.danio = 1;
                p.color = rojoApagado;
            });

            var pueblo = Patron<PatronPueblo>("Pueblo", sobrescribir, p =>
            {
                p.nombre = "El pueblo (la campana)";
                p.descripcion = "Golpea la balanza con la empuñadura: suena una campana y una ola de fuego de 1,2 u barre la plaza a " +
                                "ras de suelo, primero desde un lado y después desde el otro. Se salta.";
                p.memoria = 1;
                p.aviso = 1f;
                p.danio = 1;
                p.color = new Color(1f, 0.5f, 0.2f, 1f);
            });

            var companero = Patron<PatronCompanero>("Companero", sobrescribir, p =>
            {
                p.nombre = "El compañero (la placa)";
                p.descripcion = "Abre la capa: las placas salen volando en tres ráfagas, rectas a la cabeza de Kael, como los disparos " +
                                "de los Artilleros. Agacharse tras uno de los dos escombros.";
                p.memoria = 2;
                p.aviso = 1f;
                p.danio = 1;
                p.color = new Color(0.66f, 0.62f, 0.55f, 1f);
            });

            var sentencia = Patron<PatronSentencia>("Sentencia", sobrescribir, p =>
            {
                p.nombre = "La carta (la sentencia)";
                p.descripcion = "Camina hasta Kael, alza la espada con las dos manos (1 s) y la clava: una onda sale por el suelo hacia " +
                                "los dos lados. Dos veces. Saltar la onda.";
                p.memoria = 3;
                p.aviso = 1f;
                p.danio = 2;
                p.color = rojoApagado;
            });

            Completar("JuezSilente", sobrescribir, j =>
            {
                j.vidaMaxima = 28;
                j.velocidad = 1.6f;
                // Tocarlo no hace daño: lo hacen sus ataques.
                j.danioContacto = 0;
                j.duracionVulnerable = 3f;
                j.multiplicadorVulnerable = 1f;
                j.inmuneFueraDeVulnerable = true;
                j.tinte = Hex("5A5A66");
                j.tamano = new Vector2(2f, 3.4f);
                j.fases = new List<FaseJefe>
                {
                    Fase("Lo que no miró: la orden, el pueblo, el compañero y la carta", 1f, 1f, 1f, orden, pueblo, companero, sentencia),
                    Fase("Lo que se negó a mirar (50 %): una real y una trampa", 0.5f, 0.8f, 1.15f, companero, orden, sentencia, pueblo),
                };
            }, completarEsqueleto: true);

            // Con su arte (ConstructorJuez): sin tinte y la derrota dura su muerte hasta M5. Cada ataque
            // lleva su dibujo (ConstructorEfectosMundo4): la artillería, la ola, la placa y la onda. Un dibujo
            // puesto a mano en el patrón se respeta. La espada clavada de la sentencia ya se ve en su dibujo: su
            // rectángulo no se pinta.
            ConstructorJuez.AjustarDatos(CargarDatos("JuezSilente"));
            Vestir(orden, ConstructorEfectosMundo4.PrefabArtilleria, p => p.efecto, (p, d) => p.efecto = d);
            Vestir(pueblo, ConstructorEfectosMundo4.PrefabOla, p => p.efecto, (p, d) => p.efecto = d);
            Vestir(companero, ConstructorEfectosMundo4.PrefabPlaca, p => p.placa, (p, d) => p.placa = d);
            Vestir(sentencia, ConstructorEfectosMundo4.PrefabOnda, p => p.efecto, (p, d) => p.efecto = d);
            if (ConstructorJuez.Controlador != null && sentencia.dibujarGolpe)
            {
                sentencia.dibujarGolpe = false;
                EditorUtility.SetDirty(sentencia);
            }
        }

        // Rellena el dibujo del patrón si está vacío (un dibujo puesto a mano se respeta). Sin el prefab, nada.
        private static void Vestir<TPatron, TDibujo>(TPatron patron, TDibujo dibujo, Func<TPatron, TDibujo> actual, Action<TPatron, TDibujo> fijar)
            where TPatron : PatronAtaque where TDibujo : Object
        {
            if (dibujo != null && actual(patron) == null)
            {
                fijar(patron, dibujo);
                EditorUtility.SetDirty(patron);
            }
        }

        /// <summary>
        /// Doncella del Lamento (<c>Docs/Diseno/Nivel2_Diseno.md</c>, sección 6): solo se la hiere
        /// mientras llora, así que cada fase termina su ronda con un llanto. Sus datos nacieron como
        /// esqueleto (dos fases sin patrones): se completan aquí la primera vez, y después se respeta
        /// lo que se haya ajustado a mano.
        /// </summary>
        private static void GenerarDoncella(bool sobrescribir)
        {
            var azulHielo = Hex("4A7FA5");

            var ola = Patron<PatronOla>("Ola", sobrescribir, p =>
            {
                p.nombre = "Ola";
                p.descripcion = "Una ola a ras del hielo cruza la arena desde la pared que tiene a la espalda. Se salta o se espera sobre una placa.";
                p.aviso = 0.7f;
                p.danio = 1;
                p.color = azulHielo;
            });

            var olaDoble = Patron<PatronOla>("OlaDoble", sobrescribir, p =>
            {
                p.nombre = "Ola doble";
                p.descripcion = "Dos olas seguidas: dos saltos con ritmo.";
                p.aviso = 0.6f;
                p.danio = 1;
                p.olas = 2;
                p.intervalo = 0.8f;
                p.recuperacion = 0.5f;
                p.color = azulHielo;
            });

            var inundacion = Patron<PatronInundacion>("Inundacion", sobrescribir, p =>
            {
                p.nombre = "Inundación";
                p.descripcion = "Sale agua por las grietas de la franja de Kael y después sube y se queda 5 s. No hace daño: es agua.";
                p.aviso = 1f;
                p.danio = 0;
                p.franjas = 1;
                p.duracion = 5f;
                p.color = azulHielo;
            });

            var congelar = Patron<PatronCongelar>("Congelar", sobrescribir, p =>
            {
                p.nombre = "Congelar";
                p.descripcion = "El agua de la inundación se pone blanca y se congela: púas en lo que estaba inundado y, arriba, una capa de hielo que se pisa 6 s.";
                p.aviso = 0.8f;
                p.danio = 1;
                p.duracionPuas = 0.6f;
                p.duracionCapa = 6f;
                p.color = Hex("E8EEF4");
            });

            var llanto = Patron<PatronLlanto>("Llanto", sobrescribir, p =>
            {
                p.nombre = "Llanto";
                p.descripcion = "Baja al hielo, cerca de Kael, y llora 1,5 s. Es la única ventana para herirla.";
                p.aviso = 0f;
                p.danio = 0;
                p.duracion = 1.5f;
                p.color = Hex("00D4FF");
            });

            var llantoSobreElAgua = Patron<PatronLlanto>("LlantoSobreElAgua", sobrescribir, p =>
            {
                p.nombre = "Llanto sobre el agua";
                p.descripcion = "Llora 1,2 s flotando bajo sobre la franja que acaba de congelar o de inundar: hay que subirse a su hielo o meterse en su agua.";
                p.aviso = 0f;
                p.danio = 0;
                p.duracion = 1.2f;
                p.color = Hex("00D4FF");
            });

            // Las olas llevan su dibujo (ConstructorMecanicasMundo2) en vez del rectángulo. Como con las
            // ondas de la Centinela, solo se rellena el campo vacío.
            var dibujoOla = ConstructorMecanicasMundo2.PrefabOla;
            foreach (var patron in new[] { ola, olaDoble })
            {
                if (dibujoOla != null && patron.efecto == null)
                {
                    patron.efecto = dibujoOla;
                    EditorUtility.SetDirty(patron);
                }
            }

            Completar("DoncellaLamento", sobrescribir, j =>
            {
                j.vidaMaxima = 26;
                j.velocidad = 2.5f;
                // Tocarla no hace daño: las placas de la arena, que ella atraviesa, son refugio.
                j.danioContacto = 0;
                j.duracionVulnerable = 1.5f;
                j.multiplicadorVulnerable = 2f;
                j.inmuneFueraDeVulnerable = true;
                j.tinte = azulHielo;
                j.tamano = new Vector2(2f, 3.2f);
                j.fases = new List<FaseJefe>
                {
                    Fase("Olas e inundación", 1f, 0.8f, 1f, ola, inundacion, ola, llanto),
                    // El llanto va justo después de congelar: la capa de hielo sigue ahí para subirse.
                    Fase("Congelamiento (50 %)", 0.5f, 0.6f, 1.2f, olaDoble, inundacion, congelar, llantoSobreElAgua),
                };
            }, completarEsqueleto: true);
        }

        /// <summary>
        /// Espectro del Pavor (<c>Docs/Diseno/Nivel3_Diseno.md</c>, sección 6): invisible salvo cuando
        /// ataca, y cada ataque avisa con su destello: 1 s las hojas y 0,6 s el tajo, las sombras y la
        /// embestida (v0.67: más rápido, para que dé miedo). La ventana es el jadeo tras el tajo (fase 1)
        /// y tras las sombras (fase 2). Sus datos nacieron como esqueleto: se completan aquí la primera
        /// vez, y después se respeta lo que se haya ajustado a mano.
        /// </summary>
        private static void GenerarPavor(bool sobrescribir)
        {
            var verdePalido = new Color(0.55f, 0.9f, 0.55f, 1f);

            var tajo = Patron<PatronTajo>("Tajo", sobrescribir, p =>
            {
                p.nombre = "Tajo";
                p.descripcion = "Aparece a 2,5 u de Kael, a su espalda (o delante, contra una pared), destella 0,6 s y lanza un tajo " +
                                "de 3 u. Después jadea 1,5 s, visible: la ventana.";
                p.aviso = 0.6f;
                p.danio = 2;
                p.distancia = 2.5f;
                p.alcance = 3f;
                p.alto = 2.6f;
                p.duracionGolpe = 0.2f;
                p.jadeo = 1.5f;
                p.color = verdePalido;
            });

            var embestida = Patron<PatronEmbestidaSombra>("EmbestidaSombra", sobrescribir, p =>
            {
                p.nombre = "Embestida de sombra";
                p.descripcion = "Aparece junto a la pared más lejana a Kael, a su altura (a ras de suelo o en la rama donde esté), " +
                                "destella 0,6 s y cruza la arena (1,6 u de alto, a 16 u/s). Al otro lado se queda 1 s a la vista. " +
                                "Se salta, o se baja de la rama.";
                p.aviso = 0.6f;
                p.danio = 1;
                p.velocidad = 16f;
                p.alto = 1.6f;
                p.visibleAlFinal = 1f;
                p.color = verdePalido;
            });

            var hojas = Patron<PatronHojas>("Hojas", sobrescribir, p =>
            {
                p.nombre = "Hojas";
                p.descripcion = "Alza el brazo y destella 1 s; en cuatro o cinco franjas el suelo brilla en verde y caen hojas " +
                                "negras afiladas. La primera, bajo Kael. Se sale de lo que brilla.";
                p.aviso = 1f;
                p.danio = 1;
                p.franjasMinimo = 4;
                p.franjasMaximo = 5;
                p.ancho = 2f;
                p.hueco = 1f;
                p.alto = 5f;
                p.duracion = 0.6f;
                p.recuperacion = 0.6f;
                p.color = new Color(0.35f, 0.85f, 0.4f, 1f);
            });

            var sombras = Patron<PatronSombras>("Sombras", sobrescribir, p =>
            {
                p.nombre = "Las tres sombras";
                p.descripcion = "Tres figuras (izquierda, centro y derecha) lanzan su tajo una tras otra, cada una con su destello; " +
                                "después jadean las tres 1,5 s. Solo la verdadera lleva una luciérnaga dorada. Golpear a una " +
                                "sombra la deshace y encoge la luz de Kael 3 s.";
                p.aviso = 0.6f;
                p.danio = 2;
                p.margen = 3f;
                p.aparicion = 0.4f;
                p.intervalo = 0.3f;
                p.jadeo = 1.5f;
                p.color = verdePalido;
            });

            // Las sombras lanzan el mismo tajo que él: solo se rellena si está vacío.
            if (sombras.tajo == null)
            {
                sombras.tajo = tajo;
                EditorUtility.SetDirty(sombras);
            }

            Completar("EspectroDelPavor", sobrescribir, j =>
            {
                j.vidaMaxima = 22;
                j.velocidad = 2f;
                // Tocarlo no hace daño: es invisible y lo atravesaría; lo hacen sus ataques.
                j.danioContacto = 0;
                j.duracionVulnerable = 1.5f;
                j.multiplicadorVulnerable = 2f;
                j.inmuneFueraDeVulnerable = false;
                j.tinte = Hex("6B4FA0");
                j.tamano = new Vector2(2f, 3.2f);
                j.fases = new List<FaseJefe>
                {
                    Fase("El acecho: tajo, embestida, tajo y hojas", 1f, 1f, 1f, tajo, embestida, tajo, hojas),
                    Fase("Las tres sombras (50 %)", 0.5f, 0.9f, 1f, sombras, hojas, sombras, embestida),
                };
            }, completarEsqueleto: true);

            // Con su arte (ConstructorPavor): sin tinte, la derrota dura su muerte hasta M5 y cada
            // franja de hojas lleva su dibujo. Un dibujo puesto a mano en el patrón se respeta.
            ConstructorPavor.AjustarDatos(CargarDatos("EspectroDelPavor"));
            var dibujoHojas = ConstructorPavor.PrefabHojas;
            if (dibujoHojas != null && hojas.efecto == null)
            {
                hojas.efecto = dibujoHojas;
                EditorUtility.SetDirty(hojas);
            }

            // Con su arte, cada ataque lo enseña su dibujo: el tajo y la embestida, sus gestos (las
            // sombras lanzan su mismo tajo), y las hojas, el suelo que brilla. El rectángulo verde de
            // la zona, que sin arte (fase D) era el aviso, ya no se pinta ni en el aviso ni en el golpe.
            if (dibujoHojas != null)
            {
                foreach (var patron in new PatronAtaque[] { tajo, embestida, hojas, sombras })
                {
                    if (patron.dibujarZona)
                    {
                        patron.dibujarZona = false;
                        EditorUtility.SetDirty(patron);
                    }
                }
            }
        }

        private static T Patron<T>(string nombre, bool sobrescribir, Action<T> rellenar) where T : PatronAtaque
        {
            var ruta = $"{CarpetaPatrones}/{nombre}.asset";
            var patron = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (patron == null)
            {
                patron = ScriptableObject.CreateInstance<T>();
                rellenar(patron);
                AssetDatabase.CreateAsset(patron, ruta);
            }
            else if (sobrescribir)
            {
                rellenar(patron);
                EditorUtility.SetDirty(patron);
            }

            return patron;
        }

        /// <summary>
        /// Las ondas de la Centinela llevan su dibujo (<see cref="ConstructorEfectos"/>) en vez del
        /// rectángulo. Solo se rellena el campo vacío: un dibujo puesto a mano se respeta. Sin el
        /// prefab (sin arte), se quedan como estaban.
        /// </summary>
        private static void VestirOndas(GameObject dibujo, PatronSaltoAplastante salto, params PatronOndaSuelo[] ondas)
        {
            if (dibujo == null)
            {
                return;
            }

            foreach (var onda in ondas)
            {
                if (onda.efecto == null)
                {
                    onda.efecto = dibujo;
                    EditorUtility.SetDirty(onda);
                }
            }

            if (salto.efectoOndas == null)
            {
                salto.efectoOndas = dibujo;
                EditorUtility.SetDirty(salto);
            }
        }

        /// <summary>
        /// Las erupciones llevan la columna dibujada mientras queman; el aviso sigue siendo el
        /// rectángulo. Como con las ondas, solo se rellena el campo vacío.
        /// </summary>
        private static void VestirErupciones(GameObject dibujo, params PatronPlataformasErupcion[] erupciones)
        {
            if (dibujo == null)
            {
                return;
            }

            foreach (var erupcion in erupciones)
            {
                if (erupcion.efecto == null)
                {
                    erupcion.efecto = dibujo;
                    EditorUtility.SetDirty(erupcion);
                }
            }
        }

        // Con completarEsqueleto, también rellena al jefe que todavía es un esqueleto (fases sin
        // ningún patrón): así pasa de esqueleto a completo sin restablecer a los demás.
        private static void Completar(string asset, bool sobrescribir, Action<JefeData> rellenar, bool completarEsqueleto = false)
        {
            var jefe = CargarDatos(asset);
            if (jefe == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {CarpetaDatos}/{asset}.asset: ejecuta Fragmentum → Progresión → Generar datos de niveles.");
                return;
            }

            if (sobrescribir || jefe.fases == null || jefe.fases.Count == 0 || (completarEsqueleto && EsEsqueleto(jefe)))
            {
                rellenar(jefe);
                EditorUtility.SetDirty(jefe);
            }
        }

        /// <summary>¿Tiene fases pero ningún patrón? Entonces espera sin atacar.</summary>
        public static bool EsEsqueleto(JefeData jefe)
        {
            if (jefe == null || jefe.fases == null)
            {
                return true;
            }

            foreach (var fase in jefe.fases)
            {
                if (fase != null && fase.patrones != null && fase.patrones.Count > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Esqueleto(JefeData j, int vida, string tinte, params FaseJefe[] fases)
        {
            j.vidaMaxima = vida;
            j.velocidad = 2f;
            j.danioContacto = 1;
            j.duracionVulnerable = 1.2f;
            j.multiplicadorVulnerable = 2f;
            j.inmuneFueraDeVulnerable = false;
            j.tinte = Hex(tinte);
            j.tamano = new Vector2(2f, 3.2f);
            j.fases = new List<FaseJefe>(fases);
        }

        private static FaseJefe Fase(string nombre, float umbral) => Fase(nombre, umbral, 1f, 1f);

        private static FaseJefe Fase(string nombre, float umbral, float pausa, float multiplicadorVelocidad, params PatronAtaque[] patrones) =>
            new FaseJefe
            {
                nombre = nombre,
                umbralVida = umbral,
                pausaEntrePatrones = pausa,
                multiplicadorVelocidad = multiplicadorVelocidad,
                patrones = new List<PatronAtaque>(patrones),
            };

        public static JefeData CargarDatos(string asset) => AssetDatabase.LoadAssetAtPath<JefeData>($"{CarpetaDatos}/{asset}.asset");

        // --- Prefabs ---

        public static bool CrearPrefabs()
        {
            AsegurarCarpeta(CarpetaPrefabs);
            var ok = true;
            foreach (var (asset, controlador) in Jefes)
            {
                var datos = CargarDatos(asset);
                if (datos == null)
                {
                    ok = false;
                    continue;
                }

                ok &= CrearPrefab(asset, controlador, datos) != null;
            }

            if (ok)
            {
                Debug.Log($"[Fragmentum] Prefabs de jefes creados en {CarpetaPrefabs} ({Jefes.Length}).");
            }

            return ok;
        }

        /// <summary>El prefab del jefe de esos datos, o null si todavía no existe.</summary>
        public static GameObject CargarPrefab(JefeData datos) =>
            datos != null ? AssetDatabase.LoadAssetAtPath<GameObject>($"{CarpetaPrefabs}/{datos.name}.prefab") : null;

        private static GameObject CrearPrefab(string nombre, Type tipoControlador, JefeData datos)
        {
            var raiz = new GameObject(nombre) { layer = Capa(Capas.Enemy), tag = GameTags.Boss };
            try
            {
                var tamano = datos.tamano;

                var cuerpoRigido = raiz.AddComponent<Rigidbody2D>();
                cuerpoRigido.gravityScale = 3f;
                cuerpoRigido.mass = 20f;
                cuerpoRigido.freezeRotation = true;
                cuerpoRigido.interpolation = RigidbodyInterpolation2D.Interpolate;
                cuerpoRigido.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var cuerpo = raiz.AddComponent<CapsuleCollider2D>();
                cuerpo.size = tamano;
                cuerpo.offset = new Vector2(0f, tamano.y * 0.5f);

                raiz.AddComponent<SaludJefe>();
                var controlador = (BossController)raiz.AddComponent(tipoControlador);

                var pivote = new GameObject("Pivote").transform;
                pivote.SetParent(raiz.transform, false);

                var objetoSprite = new GameObject("Sprite") { layer = raiz.layer };
                objetoSprite.transform.SetParent(pivote, false);
                var sprite = objetoSprite.AddComponent<SpriteRenderer>();
                sprite.sortingOrder = 5;
                var animator = objetoSprite.AddComponent<Animator>();

                // El Espectro del Pavor se deja ver a oscuras: su dibujo no depende de la luz.
                if (nombre == NombrePavor)
                {
                    sprite.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialSinLuz);
                }

                // Los jefes con arte (la Centinela, la Doncella y el Pavor) llevan sus fotogramas, su
                // Animator y su fragmento; los demás, la cápsula provisional.
                var conArte = (nombre == ConstructorCentinela.NombreDatos && ConstructorCentinela.Vestir(objetoSprite, pivote, controlador)) ||
                              (nombre == ConstructorDoncella.NombreDatos && ConstructorDoncella.Vestir(objetoSprite, pivote, controlador)) ||
                              (nombre == ConstructorPavor.NombreDatos && ConstructorPavor.Vestir(objetoSprite, pivote, controlador)) ||
                              (nombre == ConstructorJuez.NombreDatos && ConstructorJuez.Vestir(objetoSprite, pivote, controlador));
                if (!conArte)
                {
                    objetoSprite.transform.localPosition = new Vector3(0f, tamano.y * 0.5f, 0f);
                    sprite.sprite = SpriteCapsula;
                    sprite.color = datos.tinte;
                    if (sprite.sprite != null)
                    {
                        // La cápsula base mide 1 × 2 u.
                        objetoSprite.transform.localScale = new Vector3(tamano.x, tamano.y * 0.5f, 1f);
                    }

                    var provisional = objetoSprite.AddComponent<SpriteProvisional>();
                    Asignar(provisional, "assetPendiente", p => p.stringValue = $"Sprite y animaciones de {datos.nombreCanonico} (PENDIENTES B5)");
                    Asignar(provisional, "color", p => p.colorValue = datos.tinte);
                    Asignar(provisional, "tamano", p => p.vector2Value = tamano);
                }

                // Contacto: tocar al jefe hace daño mientras combate.
                var objetoContacto = new GameObject("Contacto") { layer = Capa(Capas.EnemyWeapon) };
                objetoContacto.transform.SetParent(raiz.transform, false);
                var zonaContacto = objetoContacto.AddComponent<CapsuleCollider2D>();
                zonaContacto.isTrigger = true;
                zonaContacto.size = tamano * 0.9f;
                zonaContacto.offset = new Vector2(0f, tamano.y * 0.5f);
                var contacto = objetoContacto.AddComponent<Hitbox>();
                Asignar(contacto, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
                Asignar(contacto, "golpearUnaVezPorActivacion", p => p.boolValue = false);
                objetoContacto.SetActive(false);

                Asignar(controlador, "datos", p => p.objectReferenceValue = datos);
                Asignar(controlador, "pivote", p => p.objectReferenceValue = pivote);
                Asignar(controlador, "sprite", p => p.objectReferenceValue = sprite);
                Asignar(controlador, "animator", p => p.objectReferenceValue = animator);
                Asignar(controlador, "contacto", p => p.objectReferenceValue = contacto);

                var ruta = $"{CarpetaPrefabs}/{nombre}.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {ruta}.");
                    return null;
                }

                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }
    }
}
