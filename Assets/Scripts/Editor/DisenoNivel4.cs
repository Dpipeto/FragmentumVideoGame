using System.Collections.Generic;
using System.Linq;
using Fragmentum.Data;
using UnityEngine;
using Bloque = Fragmentum.EditorTools.DisenoNivel1.Bloque;
using MEDIOS = Fragmentum.EditorTools.DisenoNivel2.MEDIOS;
using Pisada = Fragmentum.EditorTools.DisenoNivel1.Pisada;
using Zona = Fragmentum.EditorTools.DisenoNivel2.Zona;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El recorrido del Nivel 4 (Culpa, Campos de Batalla Arrasados): la propuesta de
    /// <c>Docs/Diseno/Nivel4_Diseno.md</c>, aprobada el 2026-10-20 con sus dieciséis decisiones. Lo
    /// usan <see cref="PreparadorNivel"/> para montar la escena y las pruebas para comprobar que se
    /// cruza, que la cobertura para los disparos y que el río solo se pasa con el Dash Acuático.
    ///
    /// <b>La idea:</b> la culpa pesa, pero no define. En el campo arrasado se aprende a cubrirse (los
    /// Artilleros apuntan a la cabeza: solo agachado tras una cobertura no alcanzan) y que los actos
    /// tienen consecuencias (el barril estalla con el Ataque Cargado y se lleva lo que tiene cerca); en
    /// la tierra de nadie, donde no hay detrás de qué cubrirse, se cruza sin que te vean (la
    /// Invisibilidad). En las ruinas, mirar o no mirar: los tejados no tienen memorias; la calle, dos.
    /// Después, el río bajo el puente volado (el Dash Acuático), la carta y el patio del Verdugo.
    ///
    /// <b>Las medidas.</b> x e y en unidades (1 u = 1 tile). Los bordes de los bloques caen en
    /// unidades enteras, como en los otros niveles; las coberturas, no: son atrezo (escombros, sacos
    /// terreros, chimeneas) y van donde las pide la línea de tiro.
    ///
    /// Cambios frente al esquema de la propuesta (sección «Lo que cambió al montarlo» del documento):
    /// el río tiene 3,5 u de hondo y no 4, y el pilar queda a ras del agua, como en la ciénaga del
    /// Mundo 3 (con 4 u, lo que sobra del doble salto bajo el agua no sube a Kael a la orilla); las
    /// alambradas miden 2 u y no 2,5 (con el cuerpo de Kael, 2,5 u pedirían dos dashes); los ojos del
    /// puente, el pilar y los tejados, a unidades enteras; y los escombros de la bifurcación son un
    /// bloque de 2 u de alto a 1 u del arco (bajo la torre no se salta): desde lo alto se elige subir a
    /// los tejados o bajar a la calle.
    /// </summary>
    public static class DisenoNivel4
    {
        public const int IdNivel = 4;

        /// <summary>
        /// Sube al cambiar el recorrido: <see cref="PreparadorNivel"/> lo vuelve a montar entero. La 1
        /// es la fase A: los bloques, las coberturas, el agua y las alambradas del río, los enemigos,
        /// los checkpoints y la arena. La 2, la fase B: los barriles, la mina, las cuatro memorias con
        /// su sistema de consecuencias, las dos pistas y la línea del río. La 3, la fase C: los
        /// Artilleros y el Verdugo. La 4, la fase D: las cuatro marcas de la plaza y la pared en la que
        /// el Juez proyecta sus memorias. La 5 y la 6, la fase E: los sacos de los Artilleros, más bajos
        /// y pegados a ellos, y su cañón donde lo tiene el dibujo. La 7, la segunda tanda de la fase E: el
        /// escenario vestido (el tileset, las coberturas, el río y el atrezo) y el campanario como lo dibuja
        /// su arte (el balcón, el arco y lo alto de la torre). La 8 y la 9, la última de la fase E: las memorias con
        /// su dibujo, y en la plaza cada una sobre su losa, con la pared que proyecta sus imágenes, más abajo, a la vista.
        /// La 10, la fase F: la puerta de la iglesia en la salida, la verja de cadenas y los checkpoints de la bandera blanca.
        /// </summary>
        public const int Version = 10;

        /// <summary>
        /// Escala de los Penitentes: son los Ecos de este mundo y van al tamaño de los Ecos de la Ira
        /// (<see cref="DisenoNivel1.EscalaEco"/>), como los Lamentos y los Espectros.
        /// </summary>
        public static readonly Vector2 EscalaPenitente = DisenoNivel1.EscalaEco;

        /// <summary>Base de todos los bloques: por debajo de lo que enseña la cámara.</summary>
        public const float Fondo = -8f;

        /// <summary>Tope de los muros de la arena: por encima de lo que enseña la cámara.</summary>
        public const float Tope = 22f;

        /// <summary>Lo que enseña la cámara en vertical (mínimo, máximo): del fondo del río al techo de la arena.</summary>
        public static readonly Vector2 AlturaCamara = new Vector2(-7f, 20f);

        /// <summary>El encuadre de los otros niveles.</summary>
        public const float TamanoCamara = DisenoNivel2.TamanoCamara;

        /// <summary>La zona mortal: muy por debajo del fondo del río, que es lo más hondo del nivel.</summary>
        public const float ZonaMortal = -12f;

        /// <summary>Hasta dónde llega la zona mortal, a cada lado del nivel.</summary>
        public static readonly Vector2 AnchoZonaMortal = new Vector2(-20f, 210f);

        /// <summary>Alto de Kael (su cápsula), de pie y agachado (el 60 %: <c>ParametrosJugador.alturaAgachado</c>).</summary>
        public const float AltoKael = DisenoNivel2.AltoKael;

        public const float AltoKaelAgachado = AltoKael * 0.6f;

        /// <summary>El ancho de Kael (su cápsula).</summary>
        public const float AnchoKael = DisenoNivel3.AnchoKael;

        /// <summary>Lo que queda libre bajo un techo por el que se pasa: Kael y un poco más.</summary>
        public const float HuecoBajoUnTecho = 2f;

        /// <summary>El grueso de los tejados y del balcón del campanario.</summary>
        public const float GrosorTejado = 0.5f;

        // --- La cobertura y los disparos (sección 4 del documento) ---

        /// <summary>Lo que mide una cobertura: de pie, Kael asoma la cabeza; agachado, queda debajo del borde.</summary>
        public const float AltoCobertura = 1.3f;

        /// <summary>
        /// El cañón del Artillero, sobre lo que pisa: el de su dibujo (<see cref="ConstructorArtillero"/>),
        /// arrodillado y apuntando. Mide 1,5 u y la boca del fusil queda a 1,06 (era 1,5 sin arte).
        /// </summary>
        public const float AlturaCanon = 1.06f;

        /// <summary>Lo que asoma la boca del cañón por delante de él: la de su dibujo.</summary>
        public const float CanonDelante = 1.55f;

        /// <summary>
        /// Lo que apunta el Artillero por debajo de lo alto de Kael: a lo alto de la cabeza. Era 0,25; con
        /// el cañón del dibujo, más bajo, la línea de tiro sube más despacio y, de pie tras la cobertura
        /// más cercana a él, la bala daba en la cobertura. Apuntando 0,1 u bajo lo alto, de pie le da tras
        /// todas (la decisión 2: hacen falta las dos cosas).
        /// </summary>
        public const float BajoLaCabeza = 0.1f;

        /// <summary>Hasta dónde ve un Artillero, por delante.</summary>
        public const float VisionArtillero = 14f;

        /// <summary>
        /// Los sacos de cada Artillero: de su x − 1,3 a su x − 0,45, por delante de él (miran a la
        /// izquierda). El cañón asoma 0,25 u por delante de ellos.
        /// </summary>
        public static readonly Vector2 SacosDelArtillero = new Vector2(-1.3f, -0.45f);

        /// <summary>
        /// Lo que miden los sacos de cada Artillero (decidido el 2026-10-21): más bajos que las demás
        /// coberturas, para que su fusil, a 1,06 u, dispare por encima. Las demás miden <see cref="AltoCobertura"/>.
        /// </summary>
        public const float AltoSacosArtillero = 0.9f;

        /// <summary>La ráfaga: apunta 0,8 s (el aviso), dispara tres balas y recarga 2 s.</summary>
        public const float ApuntaArtillero = 0.8f;

        public const int BalasPorRafaga = 3;

        public const float EntreBalas = 0.12f;

        public const float RecargaArtillero = 2f;

        /// <summary>El segundo Artillero del campanario empieza este tiempo después que el primero: se turnan.</summary>
        public const float TurnoDelCampanario = 1.6f;

        /// <summary>La velocidad de Kael corriendo y lo que dura la Invisibilidad Parcial (Anexo A).</summary>
        public const float DuracionInvisibilidad = 3f;

        // --- Los barriles y la mina ---

        /// <summary>El barril: mecha de 0,6 s, estalla en 2,5 u (3 a los enemigos, 1 a Kael) y enciende los que tiene a menos de 3 u.</summary>
        public const float MechaBarril = 0.6f;

        public const float RadioBarril = 2.5f;

        public const int DanioBarrilEnemigos = 3;

        public const int DanioBarrilKael = 1;

        public const float CadenaBarril = 3f;

        /// <summary>La mina del río: sube y baja 2 u cada 3 s; estalla en 1,5 u (1 a Kael, 3 a los enemigos).</summary>
        public const float RecorridoMina = 2f;

        public const float PeriodoMina = 3f;

        public const float RadioMina = 1.5f;

        public const int DanioMinaKael = 1;

        public const int DanioMinaEnemigos = 3;

        /// <summary>El radio de la mina: su bola, sujeta al fondo por la cadena.</summary>
        public const float RadioBolaMina = 0.4f;

        /// <summary>Abajo, el centro de la mina queda a esto del fondo; arriba, <see cref="RecorridoMina"/> más.</summary>
        public const float MinaSobreElFondo = 0.5f;

        // --- Las memorias y sus consecuencias ---

        /// <summary>Hasta dónde llega Kael para recordar una memoria, desde su pie (con Interactuar).</summary>
        public const float DistanciaRecordar = 1.5f;

        /// <summary>Al mirar una memoria, los enemigos de su zona: un 20 % más deprisa (Anexo A).</summary>
        public const float AgresividadPorMemoria = 1.2f;

        /// <summary>Si no se miró ninguna, el Juez tiene un 30 % más de vida (Anexo A).</summary>
        public const float VidaDelJuezSinMemorias = 1.3f;

        /// <summary>
        /// Lo que queda la superficie del agua por debajo de lo que se pisa al salir, como en el Nivel 2
        /// y en la ciénaga del Nivel 3.
        /// </summary>
        public const float BajoLaSuperficie = DisenoNivel2.BajoLaSuperficie;

        /// <summary>El velo de los bordes de la pantalla bajo el agua del río: pardo y turbio.</summary>
        public static readonly Color ColorVeloAgua = new Color(0.12f, 0.09f, 0.05f, 1f);

        public enum TIPOS_ENEMIGO
        {
            PENITENTE,
            ARTILLERO,
            VERDUGO
        }

        /// <summary>
        /// Un enemigo de pie en (X, Y), con su ruta de patrulla de Desde a Hasta. Los Artilleros no
        /// patrullan (su ruta es su sitio) y miran a la izquierda, hacia el que llega; el segundo del
        /// campanario empieza su ráfaga <see cref="Turno"/> s después.
        /// </summary>
        public readonly struct Enemigo
        {
            public Enemigo(string nombre, TIPOS_ENEMIGO tipo, float x, float y, float desde, float hasta, float turno = 0f)
            {
                Nombre = nombre;
                Tipo = tipo;
                X = x;
                Y = y;
                Desde = desde;
                Hasta = hasta;
                Turno = turno;
            }

            public string Nombre { get; }
            public TIPOS_ENEMIGO Tipo { get; }
            public float X { get; }
            public float Y { get; }
            public float Desde { get; }
            public float Hasta { get; }
            public float Turno { get; }

            /// <summary>Los del balcón del campanario: no se les alcanza.</summary>
            public bool EnElCampanario => Nombre.StartsWith("ArtilleroDelCampanario");

            /// <summary>
            /// Por dónde sale la bala: la boca del cañón, que asoma por delante de sus sacos (así sus
            /// propias balas no chocan con ellos).
            /// </summary>
            public Vector2 Canon => new Vector2(X - CanonDelante, Y + AlturaCanon);
        }

        /// <summary>Una pieza de atrezo: su pieza del tileset o de los props, su pie, y cómo va.</summary>
        public readonly struct Decorado
        {
            public Decorado(string nombre, ConstructorEscenarioMundo4.Pieza pieza, float x, float y, bool volteado = false, bool delante = false,
                float escala = 1f)
            {
                Nombre = nombre;
                Pieza = pieza;
                X = x;
                Y = y;
                Volteado = volteado;
                Delante = delante;
                Escala = escala;
            }

            public string Nombre { get; }
            public ConstructorEscenarioMundo4.Pieza Pieza { get; }
            public float X { get; }
            public float Y { get; }

            /// <summary>Dibujado en espejo, para que dos iguales no se vean iguales.</summary>
            public bool Volteado { get; }

            /// <summary>Por delante de los bloques (cuelga de un tejado), no por detrás.</summary>
            public bool Delante { get; }

            /// <summary>A qué escala va (la casa y el árbol, al doble: son fondo).</summary>
            public float Escala { get; }

            public Vector2 Posicion => new Vector2(X, Y);
        }

        /// <summary>
        /// El atrezo del campo arrasado y del pueblo en ruinas. No hace nada: solo cuenta lo que pasó.
        /// - <b>la cruz</b> con su placa, detrás del inicio: aquí cayó alguien;
        /// - <b>el campo</b>: un árbol calcinado, un cráter con agua (los cráteres, por delante del suelo: son
        ///   charcos en él) y una alambrada de estacas antes del barril;
        /// - <b>el refugio</b> (el primer checkpoint, el del eco de memoria): una caja de munición;
        /// - <b>la tierra de nadie</b>: alambradas caídas, cráteres y un casco en el barro, todo decorado
        ///   (<c>Nivel4_Diseno.md</c>, sección 3);
        /// - <b>la calle</b>: dos casas en ruinas al fondo, una farola, una cortina quemada y unos cables que
        ///   cuelgan de los tejados;
        /// - <b>la convergencia y la carta</b>: un montón de ladrillos y una viga caída.
        /// La plaza no lleva nada: el combate se lee mejor limpio.
        /// </summary>
        public static readonly Decorado[] Decorados =
        {
            // El inicio y el campo.
            new Decorado("Cruz_Inicio", ConstructorEscenarioMundo4.Cruz, -2.5f, 0f),
            new Decorado("Arbol_Campo", ConstructorEscenarioMundo4.Arbol, 7f, 0f, escala: 2f),
            new Decorado("Crater_Campo", ConstructorEscenarioMundo4.Crater, 11.5f, 0f, delante: true),
            new Decorado("Alambre_Campo", ConstructorEscenarioMundo4.Alambre, 25.5f, 0f, volteado: true),

            // El refugio.
            new Decorado("Caja_Refugio", ConstructorEscenarioMundo4.Caja, 45f, 0f),

            // La tierra de nadie.
            new Decorado("Alambre_TierraDeNadie1", ConstructorEscenarioMundo4.Alambre, 52f, 0f),
            new Decorado("Crater_TierraDeNadie1", ConstructorEscenarioMundo4.Crater, 54.5f, 0f, delante: true),
            new Decorado("Casco_TierraDeNadie", ConstructorEscenarioMundo4.Casco, 56.5f, 0f),
            new Decorado("Alambre_TierraDeNadie2", ConstructorEscenarioMundo4.Alambre, 58.5f, 0f, volteado: true),
            new Decorado("Crater_TierraDeNadie2", ConstructorEscenarioMundo4.Crater, 61f, 0f, delante: true),

            // La calle, bajo los tejados.
            new Decorado("Casa_Calle1", ConstructorEscenarioMundo4.Casa, 77f, -3f, escala: 2f),
            new Decorado("Farola_Calle", ConstructorEscenarioMundo4.Farola, 84.5f, AlturaTejados - GrosorTejado, delante: true),
            new Decorado("Cortina_Calle", ConstructorEscenarioMundo4.Cortina, 86.3f, AlturaTejados - GrosorTejado, delante: true),
            new Decorado("Casa_Calle2", ConstructorEscenarioMundo4.Casa, 99.5f, -3f, volteado: true, escala: 2f),
            new Decorado("Cables_Calle", ConstructorEscenarioMundo4.Cables, 75.5f, AlturaTejados - GrosorTejado, delante: true),

            // La convergencia y la carta.
            new Decorado("Escombrera_Convergencia", ConstructorEscenarioMundo4.Escombrera, 109f, 0f),
            new Decorado("Viga_Carta", ConstructorEscenarioMundo4.Viga, 139.3f, 0f, volteado: true),
        };

        /// <summary>Un salto del recorrido, de una pisada a otra; los del río, bajo el agua.</summary>
        public readonly struct Paso
        {
            public Paso(string desde, string hasta, MEDIOS medio = MEDIOS.SECO)
            {
                Desde = desde;
                Hasta = hasta;
                Medio = medio;
            }

            public string Desde { get; }
            public string Hasta { get; }
            public MEDIOS Medio { get; }

            public override string ToString() => $"{Desde} → {Hasta}";
        }

        /// <summary>
        /// Una memoria (Anexo A): un objeto del campo de batalla que se recuerda con E, por su pie. Al
        /// mirarla, los enemigos que están en su <see cref="Zona"/> se vuelven más agresivos. El Juez
        /// proyecta la misma memoria en cada ataque: <see cref="Marca"/> es su marca en la plaza.
        /// </summary>
        public readonly struct Memoria
        {
            public Memoria(string nombre, string objeto, float x, float y, Zona zona, float marca)
            {
                Nombre = nombre;
                Objeto = objeto;
                X = x;
                Y = y;
                Zona = zona;
                Marca = marca;
            }

            public string Nombre { get; }
            public string Objeto { get; }
            public float X { get; }
            public float Y { get; }
            public Zona Zona { get; }
            public float Marca { get; }
            public Vector2 Posicion => new Vector2(X, Y);
        }

        // --- Sección 1 (−6 a 66): el campo arrasado, la tierra de nadie y el campanario ---
        // --- Sección 2 (66 a 114): las ruinas del pueblo. Los tejados o la calle, y la convergencia ---
        // --- Sección 3 (114 a 167): el río bajo el puente volado, la carta, el patio del Verdugo y la antesala ---
        // --- Arena (167 a 194): la plaza del Juez Silente, en una sala de 25 × 15 u ---

        /// <summary>
        /// Los tramos del <c>LevelData</c> (desde, hasta). De ellos sale lo que enseña la cámara, que
        /// llega 6 u más allá de cada extremo: de la pared de la izquierda (−6) a la del fondo de la
        /// arena (194).
        /// </summary>
        public static readonly Vector2[] Secciones =
        {
            new Vector2(0f, 66f),
            new Vector2(66f, 114f),
            new Vector2(114f, 167f),
            new Vector2(167f, 188f),
        };

        /// <summary>Nombre y contenido de cada tramo, en el mismo orden que <see cref="Secciones"/>.</summary>
        public static readonly (string nombre, string contenido)[] TextosDeLasSecciones =
        {
            ("Sección 1", "El campo arrasado: el primer Penitente, la radio en el cráter (la memoria sin riesgo), el tutorial del barril y el de la cobertura, el refugio, la tierra de nadie y el campanario."),
            ("Sección 2", "Las ruinas del pueblo, con dos rutas: los tejados (Artilleros y barriles en cadena) o la calle (Penitentes y dos memorias). Se juntan en la convergencia."),
            ("Sección 3", "El río bajo el puente volado, que se cruza con el Dash Acuático entre alambradas y una mina; la carta; el patio del Verdugo, y la antesala."),
            ("Arena del jefe", "La plaza: el Juez Silente y las cuatro marcas de las memorias."),
        };

        /// <summary>Lo que la cámara pasa de cada extremo de las secciones (<c>LevelManager.margenCamara</c>).</summary>
        public const float MargenCamara = 6f;

        /// <summary>Lo que se pisa y su relleno (capa del suelo).</summary>
        public static readonly Bloque[] Suelos =
        {
            new Bloque("SueloInicio", -6f, 13f, Fondo, 0f),
            new Bloque("FondoDelCrater", 13f, 19f, Fondo, -1.5f),
            new Bloque("SueloDelCampo", 19f, 48f, Fondo, 0f),
            new Bloque("Tanque", 20f, 23f, 0f, 2f),
            new Bloque("TierraDeNadie", 48f, 63f, Fondo, 0f),
            new Bloque("BajoElCampanario", 63f, 66f, Fondo, 0f),
            new Bloque("Bifurcacion", 66f, 70f, Fondo, 0f),
            new Bloque("Escombros", 67f, 69f, 0f, 2f),
            new Bloque("Calle", 70f, 104f, Fondo, -3f),
            new Bloque("EscalonDeLaCalle", 104f, 107f, Fondo, -1.5f),
            new Bloque("Convergencia", 107f, 114f, Fondo, 0f),
            new Bloque("Orilla", 114f, 116f, Fondo, 0f),
            new Bloque("FondoDelRio1", 116f, 124f, Fondo, FondoRio),
            new Bloque("Pilar", 124f, 126f, Fondo, 0f),
            new Bloque("FondoDelRio2", 126f, 137f, Fondo, FondoRio),
            new Bloque("OtraOrilla", 137f, 145f, Fondo, 0f),
            new Bloque("Patio", 145f, 162f, Fondo, 0f),
            new Bloque("Antesala", 162f, 167f, Fondo, 0f),
            new Bloque("SueloArena", 167f, 194f, Fondo, 0f),
        };

        /// <summary>
        /// Los tejados de las ruinas, a y 4,5, con huecos de 2 y 3 u que se saltan corriendo. Finos: se
        /// pisan por arriba y la calle pasa por debajo (caer de un tejado es caer a la calle).
        /// </summary>
        public static readonly Bloque[] Tejados =
        {
            Tejado("Tejado1", 69f, 80f, AlturaTejados),
            Tejado("Tejado2", 82f, 95f, AlturaTejados),
            Tejado("Tejado3", 98f, 107f, AlturaTejados),
        };

        public const float AlturaTejados = 4.5f;

        /// <summary>
        /// El balcón del campanario, donde se arrodillan sus dos Artilleros: a 4,5 u, más alto de lo
        /// que llega el doble salto (4,1). No lo pisa Kael.
        /// </summary>
        public static readonly Bloque Balcon = Tejado("BalconDelCampanario", InicioBalcon, 63f, AlturaBalcon);

        /// <summary>
        /// Donde empieza el balcón: el del dibujo del campanario (<see cref="ConstructorMecanicasMundo4"/>), que
        /// sale 1,3 u de la torre. Era 61 (2 u) sin arte.
        /// </summary>
        public const float InicioBalcon = 61.7f;

        public const float AlturaBalcon = 4.5f;

        /// <summary>
        /// El arco del campanario: bajo la torre se pasa con 2,9 u de alto, lo que mide el hueco del arco en el
        /// dibujo (era 2,5 sin arte).
        /// </summary>
        public const float AltoDelArco = 2.9f;

        /// <summary>Hasta dónde sube lo que queda de la torre: lo alto de su dibujo (era 12 sin arte).</summary>
        public const float AltoTorre = 10.5f;

        // --- El río (114 a 145) ---

        /// <summary>
        /// El fondo del río: 3,5 u bajo las orillas, como la ciénaga del Mundo 3. Con 4, lo que sobra
        /// del doble salto bajo el agua no subía a Kael a la orilla.
        /// </summary>
        public const float FondoRio = -3.5f;

        /// <summary>
        /// El techo de los dos ojos del puente: los restos del puente volado. Bajo el primero quedan
        /// 2 u (cabe Kael); bajo el segundo, 3: la mina sube y baja entre su fondo y su techo.
        /// </summary>
        public const float TechoPrimerOjo = -1.5f;

        public const float TechoSegundoOjo = -0.5f;

        /// <summary>Hasta dónde suben los restos del puente: más de lo que llega ningún salto.</summary>
        public const float AltoRestos = 7f;

        /// <summary>Los restos del puente volado: el techo de sus dos ojos.</summary>
        public static IEnumerable<Bloque> RestosDelPuente => Paredes.Where(p => p.Nombre.StartsWith("RestosDelPuente"));

        /// <summary>
        /// El agua del río: la del primer ojo (con la poza de la orilla) y la del segundo (con la que
        /// lleva a la otra orilla). El pilar queda fuera: encima se respira.
        /// </summary>
        public static readonly Zona[] Aguas =
        {
            new Zona("AguaDelPrimerOjo", 116f, 124f, FondoRio, 0f - BajoLaSuperficie),
            new Zona("AguaDelSegundoOjo", 126f, 137f, FondoRio, 0f - BajoLaSuperficie),
        };

        /// <summary>
        /// Las alambradas sumergidas: las marañas del Mundo 3, hechas alambre de espino. Llenan su ojo
        /// del fondo al techo y miden 2 u: con el cuerpo de Kael (0,8 u), un dash (2,9 u) las cruza.
        /// </summary>
        public static readonly Zona[] Alambradas =
        {
            new Zona("Alambrada1", 120f, 122f, FondoRio, TechoPrimerOjo),
            new Zona("Alambrada2", 131f, 133f, FondoRio, TechoSegundoOjo),
        };

        /// <summary>La mina, por su x y el fondo al que la sujeta su cadena. En el segundo ojo, antes de la alambrada.</summary>
        public static readonly Vector2[] Minas =
        {
            new Vector2(129f, FondoRio),
        };

        /// <summary>Delante del agua, Kael dice la línea del río.</summary>
        public static readonly Zona LineaDelRio = new Zona("LineaDelRio", 114f, 116f, 0f, 3f);

        // --- Las pistas ---

        /// <summary>Ante el primer barril, Kael nombra el Ataque Cargado.</summary>
        public static readonly Zona PistaDelBarril = new Zona("PistaDelBarril", 23f, 25f, 0f, 3f);

        /// <summary>Ante la tierra de nadie, la Invisibilidad.</summary>
        public static readonly Zona PistaDeLaTierraDeNadie = new Zona("PistaDeLaTierraDeNadie", 46.5f, 47.5f, 0f, 3f);

        /// <summary>
        /// Muros y techos (capa de las paredes). La torre del campanario, con el arco por debajo, y los
        /// restos del puente volado no agarran: con el salto de pared se subiría al balcón y el río se
        /// saltaría por arriba.
        /// </summary>
        public static readonly Bloque[] Paredes =
        {
            new Bloque("ParedIzquierda", -7f, -6f, Fondo, Tope),
            new Bloque("TorreDelCampanario", 63f, 66f, AltoDelArco, AltoTorre),
            new Bloque("RestosDelPuente1", 118f, 123f, TechoPrimerOjo, AltoRestos),
            new Bloque("RestosDelPuente2", 127f, 134f, TechoSegundoOjo, AltoRestos),
            new Bloque("ParedArena", 193f, 194f, 0f, Tope),
            new Bloque("TechoArena", 167f, 194f, 15f, Tope),
        };

        /// <summary>Las paredes en las que Kael no se agarra (<see cref="Fragmentum.Core.SinAgarre"/>).</summary>
        public static bool NoAgarra(string bloque) => bloque == "TorreDelCampanario" || bloque.StartsWith("RestosDelPuente");

        /// <summary>
        /// Las coberturas: bloques de <see cref="AltoCobertura"/> que paran los disparos (capa del suelo:
        /// las balas se paran contra ella). Los tres escombros del tutorial, los sacos de cada Artillero
        /// que se puede alcanzar, los del refugio (la última cobertura antes de la tierra de nadie), las
        /// dos chimeneas del segundo tejado y los dos escombros de la plaza.
        /// </summary>
        public static readonly Bloque[] Coberturas =
        {
            Cobertura("Escombro1", 32f, 33f, 0f),
            Cobertura("Escombro2", 35.5f, 36.5f, 0f),
            Cobertura("Escombro3", 39f, 40f, 0f),
            Cobertura("SacosDelArtillero_1", 43.5f + SacosDelArtillero.x, 43.5f + SacosDelArtillero.y, 0f, AltoSacosArtillero),
            Cobertura("SacosDelRefugio", 47.5f, 49f, 0f),
            Cobertura("Chimenea1", 85f, 86f, AlturaTejados),
            Cobertura("Chimenea2", 88f, 89f, AlturaTejados),
            Cobertura("SacosDelArtillero_2", 92.5f + SacosDelArtillero.x, 92.5f + SacosDelArtillero.y, AlturaTejados, AltoSacosArtillero),
            Cobertura("SacosDelArtillero_3", 106.2f + SacosDelArtillero.x, 106.2f + SacosDelArtillero.y, AlturaTejados, AltoSacosArtillero),
            Cobertura("EscombroArena1", 173.5f, 174.5f, 0f),
            Cobertura("EscombroArena2", 186.5f, 187.5f, 0f),
        };

        /// <summary>
        /// Los barriles, por su pie: el del tutorial (con dos Penitentes al lado), los cuatro del último
        /// tejado (a 2 u: se encadenan) y los cuatro del patio del Verdugo (a 4 u: no se encadenan).
        /// </summary>
        public static readonly Vector2[] Barriles =
        {
            new Vector2(27f, 0f),
            new Vector2(98.5f, AlturaTejados),
            new Vector2(100.5f, AlturaTejados),
            new Vector2(102.5f, AlturaTejados),
            new Vector2(104.5f, AlturaTejados),
            new Vector2(148f, 0f),
            new Vector2(152f, 0f),
            new Vector2(156f, 0f),
            new Vector2(160f, 0f),
        };

        /// <summary>Lo que mide un barril: de ancho y de alto.</summary>
        public static readonly Vector2 TamanoBarril = new Vector2(0.7f, 1f);

        /// <summary>
        /// Las cuatro memorias del Anexo A, en el orden en que se encuentran: la radio en el cráter (sin
        /// enemigos alrededor: la memoria sin riesgo), la campana y el casco en la calle (cada una, con
        /// los Penitentes que vienen detrás) y la carta a la puerta del patio (con el Verdugo). Su marca
        /// en la plaza, en el mismo orden.
        /// </summary>
        public static readonly Memoria[] Memorias =
        {
            new Memoria("LaOrden", "la radio", 16f, -1.5f, new Zona("ZonaDeLaOrden", 13f, 19f, -1.5f, 3f), 171f),
            new Memoria("ElPueblo", "la campana", 73f, -3f, new Zona("ZonaDelPueblo", 70f, 86f, -3f, 1f), 177.5f),
            new Memoria("ElCompanero", "el casco sobre el fusil", 89f, -3f, new Zona("ZonaDelCompanero", 86f, 107f, -3f, 1f), 183.5f),
            new Memoria("LaCarta", "la carta", 142f, 0f, new Zona("ZonaDeLaCarta", 145f, 162f, 0f, 6f), 190f),
        };

        /// <summary>
        /// Los enemigos. En el campo, tres Penitentes y el Artillero del tutorial de la cobertura; en el
        /// balcón del campanario, dos Artilleros que se turnan; en los tejados, dos Artilleros y tres
        /// Penitentes entre los barriles; en la calle, cinco Penitentes; en el patio, el Verdugo.
        /// </summary>
        public static readonly Enemigo[] Enemigos =
        {
            new Enemigo("Penitente_1", TIPOS_ENEMIGO.PENITENTE, 10f, 0f, 8f, 12.5f),
            new Enemigo("Penitente_2", TIPOS_ENEMIGO.PENITENTE, 28.3f, 0f, 28f, 29.5f),
            new Enemigo("Penitente_3", TIPOS_ENEMIGO.PENITENTE, 29.5f, 0f, 28.5f, 30.5f),
            new Enemigo("Artillero_1", TIPOS_ENEMIGO.ARTILLERO, 43.5f, 0f, 43.5f, 43.5f),
            new Enemigo("ArtilleroDelCampanario_1", TIPOS_ENEMIGO.ARTILLERO, 62f, AlturaBalcon, 62f, 62f),
            new Enemigo("ArtilleroDelCampanario_2", TIPOS_ENEMIGO.ARTILLERO, 62.7f, AlturaBalcon, 62.7f, 62.7f, TurnoDelCampanario),
            new Enemigo("Penitente_4", TIPOS_ENEMIGO.PENITENTE, 78f, -3f, 76f, 80f),
            new Enemigo("Penitente_5", TIPOS_ENEMIGO.PENITENTE, 81.5f, -3f, 80f, 83f),
            new Enemigo("Penitente_6", TIPOS_ENEMIGO.PENITENTE, 94f, -3f, 92f, 96f),
            new Enemigo("Penitente_7", TIPOS_ENEMIGO.PENITENTE, 97.5f, -3f, 96f, 99f),
            new Enemigo("Penitente_8", TIPOS_ENEMIGO.PENITENTE, 101f, -3f, 99.5f, 103f),
            new Enemigo("Artillero_2", TIPOS_ENEMIGO.ARTILLERO, 92.5f, AlturaTejados, 92.5f, 92.5f),
            new Enemigo("Penitente_9", TIPOS_ENEMIGO.PENITENTE, 99.5f, AlturaTejados, 99f, 100f),
            new Enemigo("Penitente_10", TIPOS_ENEMIGO.PENITENTE, 101.5f, AlturaTejados, 101f, 102f),
            new Enemigo("Penitente_11", TIPOS_ENEMIGO.PENITENTE, 103.5f, AlturaTejados, 103f, 104f),
            new Enemigo("Artillero_3", TIPOS_ENEMIGO.ARTILLERO, 106.2f, AlturaTejados, 106.2f, 106.2f),
            new Enemigo("Verdugo", TIPOS_ENEMIGO.VERDUGO, 154f, 0f, 146f, 161f),
        };

        /// <summary>El patio del Verdugo: donde lo lleva su ruta y donde esperan sus barriles.</summary>
        public static Vector2 LimitesPatio => new Vector2(PisadaDe("Patio").Desde, PisadaDe("Patio").Hasta);

        public const float Inicio = 2f;

        /// <summary>
        /// Los checkpoints (también en <c>LevelData.puntosDeControl</c>): el refugio, con el eco de
        /// memoria, antes de la tierra de nadie; la convergencia, a unos pasos del río (quien se ahoga
        /// vuelve ahí); y la antesala. Curan.
        /// </summary>
        public static readonly Vector2[] Checkpoints =
        {
            new Vector2(46f, 0f),
            new Vector2(112.5f, 0f),
            new Vector2(164.5f, 0f),
        };

        public const float SueloArena = 0f;
        public const float TechoArena = 15f;

        /// <summary>La puerta que se cierra detrás del jugador, en el borde de la arena.</summary>
        public const float PuertaDesde = 167f;
        public const float PuertaHasta = 168f;

        /// <summary>El trigger de entrada: el jugador ya ha pasado la puerta.</summary>
        public const float Entrada = 170f;

        /// <summary>El Juez, al fondo y en el centro de la plaza.</summary>
        public const float Jefe = 180.5f;

        /// <summary>La puerta de salida, a 2,5 u de la pared del fondo, como en los otros niveles.</summary>
        public const float Salida = 190.5f;

        /// <summary>Por dentro de la puerta y de la pared de la derecha.</summary>
        public static Vector2 LimitesArena => new Vector2(PuertaHasta, Paredes.Single(p => p.Nombre == "ParedArena").Desde);

        /// <summary>Las cuatro marcas de la plaza, una por memoria, en el orden de <see cref="Memorias"/>.</summary>
        public static float[] Marcas => Memorias.Select(m => m.Marca).ToArray();

        /// <summary>
        /// El color de cada memoria, mientras no llegue su arte: el de su marca y el de su proyección en la
        /// pared, para que se vea cuál es la verdadera. La radio, verde oliva; la campana, bronce; la placa,
        /// gris azulado; la carta, hueso.
        /// </summary>
        public static readonly Color[] ColoresMemorias =
        {
            new Color(0.55f, 0.65f, 0.3f, 1f),
            new Color(0.85f, 0.6f, 0.25f, 1f),
            new Color(0.6f, 0.65f, 0.75f, 1f),
            new Color(0.95f, 0.9f, 0.8f, 1f),
        };

        /// <summary>Lo que mide una marca en el suelo de la plaza.</summary>
        public static readonly Vector2 TamanoMarca = new Vector2(1.4f, 0.12f);

        /// <summary>Hasta dónde llega Kael para mirar una marca, desde su centro: encima de ella.</summary>
        public const float DistanciaMarca = 1f;

        /// <summary>
        /// La pared del fondo de la plaza en la que el Juez proyecta sus memorias: su centro y su tamaño. Detrás del Juez,
        /// de 0,3 a 5,9 u sobre el suelo: con Kael en el suelo, la cámara enseña hasta unas 6,3 u (su centro va 1,5 u
        /// por encima de él). Estaba de 6 a 12, y desde el suelo no se veía (fase E, al montar sus imágenes).
        /// </summary>
        public static readonly Rect Proyeccion = new Rect(Jefe - 5f, 0.3f, 10f, 5.6f);

        /// <summary>
        /// Los saltos del recorrido, en orden. Bajar al cráter, a la calle o al agua no es un salto: es
        /// dejarse caer.
        /// </summary>
        public static readonly Paso[] Pasos =
        {
            // El cráter, el tanque y el campo.
            new Paso("FondoDelCrater", "SueloInicio"),
            new Paso("FondoDelCrater", "SueloDelCampo"),
            new Paso("SueloDelCampo", "Tanque"),
            new Paso("SueloDelCampo", "TierraDeNadie"),
            new Paso("TierraDeNadie", "BajoElCampanario"),
            new Paso("BajoElCampanario", "Bifurcacion"),

            // La bifurcación: arriba, por los escombros a los tejados; abajo, a la calle.
            new Paso("Bifurcacion", "Escombros"),
            new Paso("Escombros", "Tejado1"),
            new Paso("Tejado1", "Tejado2"),
            new Paso("Tejado2", "Tejado3"),
            new Paso("Calle", "EscalonDeLaCalle"),
            new Paso("EscalonDeLaCalle", "Convergencia"),

            // El río: se baja al agua dejándose caer; se sale al pilar y a la otra orilla bajo el agua.
            new Paso("Convergencia", "Orilla"),
            new Paso("FondoDelRio1", "Pilar", MEDIOS.AGUA),
            new Paso("FondoDelRio2", "OtraOrilla", MEDIOS.AGUA),

            // Hasta la arena.
            new Paso("OtraOrilla", "Patio"),
            new Paso("Patio", "Antesala"),
            new Paso("Antesala", "SueloArena"),
        };

        public static bool EsDe(LevelData nivel) => nivel != null && nivel.id == IdNivel;

        /// <summary>Todo lo que se pisa: suelos, tejados y coberturas. El balcón no: Kael no llega.</summary>
        public static IEnumerable<Bloque> TodoLoQueSePisa => Suelos.Concat(Tejados).Concat(Coberturas);

        /// <summary>Lo que se pisa con ese nombre.</summary>
        public static Pisada PisadaDe(string nombre)
        {
            foreach (var bloque in TodoLoQueSePisa.Append(Balcon))
            {
                if (bloque.Nombre == nombre)
                {
                    return new Pisada(nombre, bloque.Desde, bloque.Nombre == "SueloArena" ? LimitesArena.y : bloque.Hasta, bloque.Arriba);
                }
            }

            throw new KeyNotFoundException($"No hay ninguna pisada llamada {nombre} en el Nivel 4.");
        }

        /// <summary>La altura de lo que se pisa bajo (x, desde): lo más alto que la cubre sin pasar de esa altura.</summary>
        public static float SueloBajo(float x, float desde = float.MaxValue)
        {
            var bajo = TodoLoQueSePisa.Append(Balcon).Where(b => x >= b.Desde && x < b.Hasta && b.Arriba <= desde + 1e-4f).ToList();
            return bajo.Count > 0 ? bajo.Max(b => b.Arriba) : Fondo;
        }

        private static Bloque Tejado(string nombre, float desde, float hasta, float arriba) =>
            new Bloque(nombre, desde, hasta, arriba - GrosorTejado, arriba);

        private static Bloque Cobertura(string nombre, float desde, float hasta, float suelo, float alto = AltoCobertura) =>
            new Bloque(nombre, desde, hasta, suelo, suelo + alto);

        /// <summary>Lo que mide de alto la cobertura <paramref name="nombre"/>: los sacos de los Artilleros, menos.</summary>
        public static float AltoDe(string nombre) => nombre.StartsWith("SacosDelArtillero") ? AltoSacosArtillero : AltoCobertura;
    }
}
