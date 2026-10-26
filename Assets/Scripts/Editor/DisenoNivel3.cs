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
    /// El recorrido del Nivel 3 (Miedo, Bosque Tenebroso): la propuesta de
    /// <c>Docs/Diseno/Nivel3_Diseno.md</c>, aprobada el 2026-10-14 con todas sus decisiones. Lo usan
    /// <see cref="PreparadorNivel"/> para montar la escena y las pruebas para comprobar que todo se
    /// puede cruzar <b>caminando</b>, sin hacer ruido.
    ///
    /// <b>La idea:</b> el miedo pide correr y el nivel enseña a avanzar a oscuras sin perderse. En
    /// la linde, la luz de las antorchas enseña las trampas pero despierta a los Espectros; en el
    /// bosque cerrado, los Centinelas Ciegos oyen correr y el camino que parece fácil (la hondonada)
    /// acaba en un callejón; en el claro, el Acechador solo se vence haciéndose oír a propósito.
    ///
    /// <b>Las medidas.</b> x e y en unidades (1 u = 1 tile). Los bordes horizontales de los bloques
    /// caen en unidades enteras, como en los otros dos niveles. Las ramas miden media unidad de
    /// grueso y, donde se pasa andando por debajo, dejan al menos <see cref="HuecoBajoUnaRama"/>
    /// libre: Kael mide 1,8 u.
    ///
    /// Cambios frente al esquema de la propuesta: el barranco tiene 2,5 u de fondo y no 3, para
    /// que bajo sus ramas quepa Kael y se salga de él con el doble salto sin escalón (un escalón
    /// bajo una rama le cortaba el salto); las copas llevan sus ramas a una unidad entera unas de
    /// otras; y el callejón no tiene techo de raíces: la pared de raíces, que no agarra y sube hasta
    /// el puesto del vigía, ya lo cierra.
    ///
    /// <b>La ciénaga</b> (2026-10-19, sección 10 del documento): entre el CP2 y el claro, dos
    /// túneles bajo las raíces de un árbol hundido, con marañas que solo cruza el Dash Acuático
    /// (<see cref="Maranas"/>) y un tocón para respirar entre ellos. Todo lo que había desde x 84 se
    /// corrió 28 u.
    /// </summary>
    public static class DisenoNivel3
    {
        public const int IdNivel = 3;

        /// <summary>
        /// Sube al cambiar el recorrido: <see cref="PreparadorNivel"/> lo vuelve a montar entero. La 3
        /// es la fase B: la oscuridad, las antorchas, los cepos, las ramas que ceden y las cintas. La 4,
        /// la fase C: el Acechador y los árboles del claro (el sueño de los Espectros y el oído de los
        /// Centinelas van en sus prefabs). La 5, la fase E: el arte (el tileset del bosque, las ramas,
        /// las cintas, los árboles y el atrezo, <see cref="Decorados"/>). La 6, sus puertas: la verja,
        /// los checkpoints y la puerta de salida dibujados (la salida, media unidad a la izquierda). La
        /// 7, el tamaño de los Espectros (<see cref="EscalaEspectro"/>). La 8, la ciénaga: su agua, sus
        /// túneles, sus marañas, su tocón, su antorcha y su pista; lo de después, 28 u más allá. La 9,
        /// el interior oscuro de sus túneles: a través del agua se veía el bosque del fondo, y la
        /// maraña se confundía con él.
        /// </summary>
        public const int Version = 9;

        /// <summary>
        /// Escala de los Espectros: son los Ecos de este mundo y van al tamaño que el diseñador dio a
        /// los Ecos de la Ira en el Nivel 1 (<see cref="DisenoNivel1.EscalaEco"/>, PENDIENTES N18),
        /// como los Lamentos del Nivel 2.
        /// </summary>
        public static readonly Vector2 EscalaEspectro = DisenoNivel1.EscalaEco;

        /// <summary>Base de todos los bloques: por debajo de lo que enseña la cámara (−7).</summary>
        public const float Fondo = -8f;

        /// <summary>Tope de los muros de la arena: por encima de lo que enseña la cámara (20).</summary>
        public const float Tope = 22f;

        /// <summary>Lo que enseña la cámara en vertical (mínimo, máximo): del fondo de la hondonada al techo de la arena.</summary>
        public static readonly Vector2 AlturaCamara = new Vector2(-7f, 20f);

        /// <summary>El encuadre de los otros dos niveles.</summary>
        public const float TamanoCamara = DisenoNivel2.TamanoCamara;

        /// <summary>La zona mortal: muy por debajo de la hondonada, que es lo más hondo del nivel.</summary>
        public const float ZonaMortal = -12f;

        /// <summary>Hasta dónde llega la zona mortal, a cada lado del nivel.</summary>
        public static readonly Vector2 AnchoZonaMortal = new Vector2(-20f, 183f);

        /// <summary>Alto de Kael (su cápsula).</summary>
        public const float AltoKael = DisenoNivel2.AltoKael;

        /// <summary>Lo que queda libre bajo una rama por la que se pasa andando: Kael y un poco más.</summary>
        public const float HuecoBajoUnaRama = 2f;

        /// <summary>El grueso de las ramas y del puesto del vigía.</summary>
        public const float GrosorRama = 0.5f;

        /// <summary>
        /// Lo que se mueve Kael caminando con Alt, como fracción de su velocidad máxima: el
        /// <c>factorCaminar</c> de <c>FuenteInputJugador</c>. Por debajo del umbral del sigilo (0,4)
        /// de <c>PlayerModoMovimiento</c>, así que caminando no hace ruido.
        /// </summary>
        public const float FactorCaminar = 0.35f;

        /// <summary>La luz de una antorcha (Anexo A): 8 u de radio durante 15 s.</summary>
        public const float RadioAntorcha = 8f;

        public const float DuracionAntorcha = 15f;

        /// <summary>Los últimos segundos de una antorcha: parpadea, avisando de que se acaba.</summary>
        public const float ParpadeoAntorcha = 3f;

        /// <summary>Hasta dónde llega Kael para encender una antorcha, desde su pie (Interactuar, E).</summary>
        public const float DistanciaEncender = 1.5f;

        /// <summary>A esta distancia de Kael se ve un cepo a oscuras (Anexo A: «trampas invisibles hasta 1 u»).</summary>
        public const float DistanciaVerCepo = 1f;

        /// <summary>El cepo: quita 1, sujeta 1 s y su chasquido se oye a 8 u.</summary>
        public const int DanioCepo = 1;

        public const float SujecionCepo = 1f;

        public const float RadioRuidoCepo = 8f;

        /// <summary>La rama podrida: al pisarla cruje 0,3 s, se parte y vuelve a los 4 s.</summary>
        public const float CrujidoRama = 0.3f;

        public const float VueltaRama = 4f;

        /// <summary>La luz propia de Kael (Anexo A).</summary>
        public const float RadioLuzKael = 3f;

        /// <summary>
        /// Lo que queda la superficie del agua por debajo de lo que se pisa al salir, como en el Nivel 2
        /// (<see cref="DisenoNivel2.BajoLaSuperficie"/>): a ras, Kael de pie en la orilla seguiría
        /// rozando el agua.
        /// </summary>
        public const float BajoLaSuperficie = DisenoNivel2.BajoLaSuperficie;

        /// <summary>El velo de los bordes de la pantalla bajo el agua de la ciénaga: verde pantanoso (#1A2E1A, más oscuro).</summary>
        public static readonly Color ColorVeloAgua = new Color(0.03f, 0.1f, 0.05f, 1f);

        public enum TIPOS_ENEMIGO
        {
            ESPECTRO,
            CENTINELA_CIEGO,
            ACECHADOR
        }

        /// <summary>Un enemigo de pie en (X, Y), con su ruta de patrulla de Desde a Hasta.</summary>
        public readonly struct Enemigo
        {
            public Enemigo(string nombre, TIPOS_ENEMIGO tipo, float x, float y, float desde, float hasta)
            {
                Nombre = nombre;
                Tipo = tipo;
                X = x;
                Y = y;
                Desde = desde;
                Hasta = hasta;
            }

            public string Nombre { get; }
            public TIPOS_ENEMIGO Tipo { get; }
            public float X { get; }
            public float Y { get; }
            public float Desde { get; }
            public float Hasta { get; }
        }

        /// <summary>
        /// Un salto del recorrido, de una pisada a otra. Los de fuera del agua se dan caminando; los
        /// de la ciénaga, bajo el agua (<see cref="MEDIOS.AGUA"/>).
        /// </summary>
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

        /// <summary>La luz de un tramo del nivel: de Desde a Hasta en x, con el color y la intensidad de la luz global.</summary>
        public readonly struct Luz
        {
            public Luz(string nombre, float desde, float hasta, Color color, float intensidad)
            {
                Nombre = nombre;
                Desde = desde;
                Hasta = hasta;
                Color = color;
                Intensidad = intensidad;
            }

            public string Nombre { get; }
            public float Desde { get; }
            public float Hasta { get; }
            public Color Color { get; }
            public float Intensidad { get; }
        }

        // --- Sección 1 (−6 a 36): la linde. Luz de luna, dos antorchas, el barranco, dos Espectros y tres cepos ---
        // --- Sección 2 (36 a 112): el bosque cerrado. La trinchera, la bifurcación, las copas, la hondonada y la ciénaga ---
        // --- Sección 3 (112 a 140): el claro, con el Acechador, y la antesala ---
        // --- Arena (140 a 167): el Espectro del Pavor, en una sala de 25 × 15 u a oscuras ---

        /// <summary>
        /// Los tramos del <c>LevelData</c> (desde, hasta). De ellos sale lo que enseña la cámara,
        /// que llega 6 u más allá de cada extremo: de la pared de la izquierda (−6) a la del fondo
        /// de la arena (167). Los pone <see cref="GeneradorProgresion"/>.
        /// </summary>
        public static readonly Vector2[] Secciones =
        {
            new Vector2(0f, 36f),
            new Vector2(36f, 112f),
            new Vector2(112f, 140f),
            new Vector2(140f, 161f),
        };

        /// <summary>Nombre y contenido de cada tramo, en el mismo orden que <see cref="Secciones"/>.</summary>
        public static readonly (string nombre, string contenido)[] TextosDeLasSecciones =
        {
            ("Sección 1", "La linde, con luz de luna: el tutorial de las antorchas, un barranco con una rama podrida, 2 Espectros y 3 cepos."),
            ("Sección 2", "El bosque cerrado, a oscuras: la trinchera, la bifurcación (las copas o la hondonada, que acaba en un callejón), 3 Centinelas Ciegos, 4 Espectros y la ciénaga, que se cruza con el Dash Acuático."),
            ("Sección 3", "El claro, con luz de luna: el Acechador entre tres árboles. Y la antesala."),
            ("Arena del jefe", "Oscuridad total: solo la luz de Kael."),
        };

        /// <summary>Lo que la cámara pasa de cada extremo de las secciones (<c>LevelManager.margenCamara</c>).</summary>
        public const float MargenCamara = 6f;

        /// <summary>Lo que se pisa y su relleno (capa del suelo).</summary>
        public static readonly Bloque[] Suelos =
        {
            new Bloque("SueloInicio", -6f, 17f, Fondo, 0f),
            new Bloque("FondoDelBarranco", 17f, 27f, Fondo, -2.5f),
            new Bloque("SueloDeLaLinde", 27f, 38f, Fondo, 0f),
            new Bloque("FondoDeLaTrinchera", 38f, 46f, Fondo, -2f),
            new Bloque("SueloDelBosque", 46f, 58f, Fondo, 0f),
            new Bloque("FondoDeLaHondonada", 58f, 73f, Fondo, -4f),
            new Bloque("EscalonDeLaHondonada", 58f, 60f, -4f, -2f),
            new Bloque("SueloTrasLasCopas", 74f, 86f, Fondo, 0f),
            new Bloque("FondoDeLaCienaga1", 86f, 96f, Fondo, FondoCienaga),
            new Bloque("Tocon", 96f, 98f, Fondo, 0f),
            new Bloque("FondoDeLaCienaga2", 98f, 110f, Fondo, FondoCienaga),
            new Bloque("SueloTrasLaCienaga", 110f, 117f, Fondo, 0f),
            new Bloque("Claro", 117f, 134f, Fondo, 0f),
            new Bloque("Antesala", 134f, 140f, Fondo, 0f),
            new Bloque("SueloArena", 140f, 167f, Fondo, 0f),
        };

        // --- La ciénaga (84 a 112) ---

        /// <summary>
        /// El fondo de la ciénaga: 3,5 u bajo la orilla. Más hondo, lo que sobra del doble salto bajo
        /// el agua no sube a Kael a la orilla (lo mide <c>DisenoNivel3Tests</c>, como en el Nivel 2).
        /// </summary>
        public const float FondoCienaga = -3.5f;

        /// <summary>El techo de los dos túneles: las raíces del árbol hundido. Debajo quedan 2,5 u.</summary>
        public const float TechoTuneles = -1f;

        /// <summary>Hasta dónde suben las raíces del árbol hundido: más de lo que llega ningún salto.</summary>
        public const float AltoRaicesHundidas = 7f;

        /// <summary>Las raíces del árbol hundido: el techo de los dos túneles de la ciénaga.</summary>
        public static IEnumerable<Bloque> RaicesDelArbolHundido => Paredes.Where(p => p.Nombre.StartsWith("RaicesDelArbolHundido"));

        /// <summary>
        /// El agua de la ciénaga: la del primer túnel (con su poza) y la del segundo (con su poza y su
        /// salida). El tocón queda fuera: encima se respira. La superficie, a
        /// <see cref="BajoLaSuperficie"/> de las orillas y del tocón.
        /// </summary>
        public static readonly Zona[] Aguas =
        {
            new Zona("AguaDelPrimerTunel", 86f, 96f, FondoCienaga, 0f - BajoLaSuperficie),
            new Zona("AguaDelSegundoTunel", 98f, 110f, FondoCienaga, 0f - BajoLaSuperficie),
        };

        /// <summary>
        /// Las marañas de raíces: llenan su túnel del fondo al techo. Para salir de una, Kael (0,8 u de
        /// ancho) tiene que cruzarla y sacar el cuerpo: la primera (2 u) se cruza con un dash (2,9 u) y
        /// la segunda (4 u), con dos. Más anchas, pedirían un dash más. Su dibujo mide 2 u: la segunda
        /// lleva dos.
        /// </summary>
        public static readonly Zona[] Maranas =
        {
            new Zona("Marana1", 91f, 93f, FondoCienaga, TechoTuneles),
            new Zona("Marana2", 101f, 105f, FondoCienaga, TechoTuneles),
        };

        /// <summary>Lo que mide un dibujo de la maraña: cuelga del techo del túnel. La de 4 u lleva dos, el segundo en espejo.</summary>
        public const float AnchoDibujoMarana = 2f;

        /// <summary>El ancho de Kael (su cápsula): para salir de una maraña hay que sacar el cuerpo entero.</summary>
        public const float AnchoKael = 0.8f;

        /// <summary>Delante del agua, Kael dice que las raíces se cruzan con el Dash Acuático.</summary>
        public static readonly Zona PistaDeLaCienaga = new Zona("PistaDeLaCienaga", 84f, 86f, 0f, 3f);

        /// <summary>
        /// Las ramas sanas (capa del suelo): las del barranco, las de las copas, el puesto del vigía
        /// y la de la bajada. Finas: se pisan por arriba.
        /// </summary>
        public static readonly Bloque[] Ramas =
        {
            Rama("RamaDelBarranco1", 18f, 20f, 0f),
            Rama("RamaDelBarranco2", 24f, 26f, 0f),
            Rama("RamaDeLaBifurcacion", 59f, 61f, 2f),
            Rama("RamaDeLasCopas", 62f, 64f, 4f),
            Rama("PuestoDelVigia", 68f, 74f, 4.5f),
            Rama("RamaDeLaBajada", 78f, 80f, 3f),
        };

        /// <summary>
        /// Las ramas podridas: las «plataformas que ceden» del Anexo A. La del barranco y la de las
        /// copas caen sobre la trampa (el fondo del barranco y la hondonada); la de la bajada, sobre
        /// suelo firme: solo asusta. Miden lo que la rama podrida de las mecánicas (2 u): al pisarlas
        /// crujen <see cref="CrujidoRama"/> s y se parten. Caminando, hay que saltar de ellas en cuanto
        /// se pisan.
        /// </summary>
        public static readonly Bloque[] RamasPodridas =
        {
            Rama("RamaPodridaDelBarranco", 21f, 23f, 0f),
            Rama("RamaPodridaDeLasCopas", 65f, 67f, 4f),
            Rama("RamaPodridaDeLaBajada", 75f, 77f, 4f),
        };

        /// <summary>
        /// Muros y techos (capa de las paredes). Las raíces cierran la hondonada por la derecha y
        /// sostienen el puesto del vigía: desde abajo no se sube por ellas.
        /// </summary>
        public static readonly Bloque[] Paredes =
        {
            new Bloque("ParedIzquierda", -7f, -6f, Fondo, Tope),
            new Bloque("RaicesDelCallejon", 73f, 74f, Fondo, 4f),
            new Bloque("RaicesDelArbolHundido1", 89f, 95f, TechoTuneles, AltoRaicesHundidas),
            new Bloque("RaicesDelArbolHundido2", 100f, 107f, TechoTuneles, AltoRaicesHundidas),
            new Bloque("ParedArena", 166f, 167f, 0f, Tope),
            new Bloque("TechoArena", 140f, 167f, 15f, Tope),
        };

        /// <summary>
        /// Las paredes en las que Kael no se agarra (<see cref="Fragmentum.Core.SinAgarre"/>): las
        /// raíces del callejón y las del árbol hundido de la ciénaga, húmedas y podridas. Con el salto
        /// de pared, la trampa tendría salida y la ciénaga se saltaría por arriba.
        /// </summary>
        public static bool NoAgarra(string bloque) => bloque == "RaicesDelCallejon" || bloque.StartsWith("RaicesDelArbolHundido");

        /// <summary>
        /// Las dos ramas de la arena: se alcanzan con el doble salto. Ya no libran la embestida: si Kael
        /// está en una, el Pavor embiste por ella (v0.67; <c>EspectroDelPavor.AlturaDeKael</c>). A 7 u del
        /// jefe por cada lado; la de la derecha acaba antes de la puerta de salida (v2: en la v1
        /// llegaba hasta x 135 y se montaba sobre ella).
        /// </summary>
        public static readonly Bloque[] PlataformasArena =
        {
            Rama("RamaArenaIzquierda", 145f, 148f, 3.5f),
            Rama("RamaArenaDerecha", 159f, 162f, 3.5f),
        };

        /// <summary>
        /// Las antorchas, por el punto de su pie. La primera, en la linde, es el tutorial: su luz
        /// alcanza el primer cepo y el primer Espectro. La de la bifurcación enseña las cintas de las
        /// copas; la de la orilla, la primera maraña de la ciénaga; la del claro, al Acechador.
        /// </summary>
        public static readonly Vector2[] Antorchas =
        {
            new Vector2(7f, 0f),
            new Vector2(30f, 0f),
            new Vector2(55f, 0f),
            new Vector2(85f, 0f),
            new Vector2(114f, 0f),
        };

        /// <summary>La antorcha de la orilla de la ciénaga y la del claro, por su sitio en <see cref="Antorchas"/>.</summary>
        public const int AntorchaDeLaOrilla = 3;

        public const int AntorchaDelClaro = 4;

        /// <summary>
        /// Los cepos, por su centro en el suelo: tres en la linde (Anexo A: «3 trampas ocultas
        /// básicas»), uno de ellos en el fondo del barranco, bajo la rama podrida; y dos en la
        /// hondonada.
        /// </summary>
        public static readonly Vector2[] Cepos =
        {
            new Vector2(11f, 0f),
            new Vector2(22f, -2.5f),
            new Vector2(34f, 0f),
            new Vector2(61.5f, -4f),
            new Vector2(67f, -4f),
        };

        /// <summary>
        /// Las cintas blancas que marcan la ruta buena del laberinto (las copas), por su x y la altura
        /// de la rama de la que cuelgan: la de la bifurcación, la de las copas, el puesto del vigía y
        /// la de la bajada. Ninguna en la rama podrida ni en la hondonada. Solo se ven con luz: la de
        /// la antorcha de la bifurcación alcanza la primera; la de Kael, las que tiene a 3 u.
        /// </summary>
        public static readonly Vector2[] Cintas =
        {
            new Vector2(60f, 2f),
            new Vector2(63f, 4f),
            new Vector2(69f, 4.5f),
            new Vector2(79f, 3f),
        };

        /// <summary>Lo que mide una cinta: cuelga de lo alto de su rama.</summary>
        public static readonly Vector2 TamanoCinta = new Vector2(0.12f, 0.9f);

        /// <summary>La luz de la luna: fría y tenue.</summary>
        public static readonly Color LuzDeLuna = new Color(0.7f, 0.75f, 1f, 1f);

        /// <summary>Lo poco que se ve a oscuras tira a violeta, como el cielo del fondo.</summary>
        public static readonly Color Penumbra = new Color(0.45f, 0.35f, 0.7f, 1f);

        /// <summary>
        /// La luz de cada tramo (la luz global de la escena, que cambia según dónde esté Kael). La
        /// linde, con luna (Anexo A: «luz moderada»); el bosque cerrado, a oscuras («oscuridad
        /// completa»); el claro y la antesala, con luna otra vez, porque el claro no tiene copas; y la
        /// arena, con la oscuridad total: solo la luz de Kael. La ciénaga es parte del bosque cerrado:
        /// a oscuras. La luna del claro empieza donde se abre el claro (x 117), no al empezar la
        /// sección 3 (x 112): la antorcha del claro, en x 114, queda a oscuras, y por eso sirve para
        /// ver al Acechador antes de entrar.
        /// </summary>
        public static readonly Luz[] Luces =
        {
            new Luz("La linde: luz de luna", -7f, 36f, LuzDeLuna, 0.55f),
            new Luz("El bosque cerrado y la ciénaga: oscuridad completa", 36f, 117f, Penumbra, 0.08f),
            new Luz("El claro y la antesala: luz de luna", 117f, 140f, LuzDeLuna, 0.45f),
            new Luz("La arena: oscuridad total", 140f, 167f, Penumbra, 0.03f),
        };

        /// <summary>
        /// Los enemigos. En la linde, dos Espectros (Anexo A); en el bosque cerrado, tres Centinelas
        /// Ciegos y cuatro Espectros (Anexo A): uno de cada en la salida de la trinchera, un
        /// Centinela en el puesto del vigía, un Espectro en la bajada y, en la hondonada, la
        /// trampa, un Centinela y dos Espectros. En el claro, el Acechador, el mini-jefe, que merodea
        /// entre sus tres árboles. El de la bajada duerme en x 76 (antes, 78): la antorcha de la orilla
        /// de la ciénaga no lo alcanza. En la ciénaga no hay enemigos.
        /// </summary>
        public static readonly Enemigo[] Enemigos =
        {
            new Enemigo("Espectro_1", TIPOS_ENEMIGO.ESPECTRO, 14f, 0f, 12.5f, 15.5f),
            new Enemigo("Espectro_2", TIPOS_ENEMIGO.ESPECTRO, 33f, 0f, 31.5f, 35.5f),
            new Enemigo("CentinelaCiego_1", TIPOS_ENEMIGO.CENTINELA_CIEGO, 51f, 0f, 47.5f, 54.5f),
            new Enemigo("Espectro_3", TIPOS_ENEMIGO.ESPECTRO, 56.5f, 0f, 55.5f, 57.5f),
            new Enemigo("CentinelaCiego_2", TIPOS_ENEMIGO.CENTINELA_CIEGO, 71f, 4.5f, 69f, 73f),
            new Enemigo("Espectro_4", TIPOS_ENEMIGO.ESPECTRO, 76f, 0f, 75f, 77.5f),
            new Enemigo("CentinelaCiego_3", TIPOS_ENEMIGO.CENTINELA_CIEGO, 63.5f, -4f, 61.5f, 66f),
            new Enemigo("Espectro_5", TIPOS_ENEMIGO.ESPECTRO, 69f, -4f, 68f, 70f),
            new Enemigo("Espectro_6", TIPOS_ENEMIGO.ESPECTRO, 71.5f, -4f, 70.5f, 72.5f),
            new Enemigo("Acechador", TIPOS_ENEMIGO.ACECHADOR, 128f, 0f, 119f, 133f),
        };

        /// <summary>
        /// Los tres árboles gruesos del claro, por la x de su pie. Kael pasa por delante; el Acechador,
        /// que embiste a ciegas hacia el ruido, se estrella contra ellos. Entre uno y otro caben él
        /// (2 u de largo) y la carrera para embestir.
        /// </summary>
        public static readonly float[] Arboles = { 121f, 126f, 131f };

        /// <summary>Lo que mide el tronco de un árbol del claro: de ancho y de alto.</summary>
        public static readonly Vector2 TamanoArbol = new Vector2(1.2f, 6f);

        /// <summary>
        /// Una pieza del tileset o de los props colocada de adorno: su pivote en (X, Y). No se pisa
        /// ni choca. Las que cuelgan delante de una rama o de un muro van por delante de los bloques.
        /// </summary>
        public readonly struct Decorado
        {
            public Decorado(string nombre, ConstructorEscenarioMundo3.Pieza pieza, float x, float y, bool volteado = false, bool delante = false)
            {
                Nombre = nombre;
                Pieza = pieza;
                X = x;
                Y = y;
                Volteado = volteado;
                Delante = delante;
            }

            public string Nombre { get; }
            public ConstructorEscenarioMundo3.Pieza Pieza { get; }
            public float X { get; }
            public float Y { get; }

            /// <summary>Dibujado en espejo, para que dos iguales no se vean iguales.</summary>
            public bool Volteado { get; }

            /// <summary>Por delante de los bloques (cuelga de una rama o tapa un muro), no por detrás.</summary>
            public bool Delante { get; }

            public Vector2 Posicion => new Vector2(X, Y);
        }

        /// <summary>
        /// El atrezo: el bosque de noche y la trinchera olvidada del soldado. La idea de cada cosa:
        /// - <b>la tumba</b> del soldado (el fusil clavado y el casco) es lo primero que se ve, detrás
        ///   del inicio: el mundo del Miedo empieza en una guerra que no acabó;
        /// - <b>la trinchera</b> (el primer checkpoint, el del eco de memoria) tiene su refugio, sus
        ///   sacos y una caja de munición: ahí se escondía;
        /// - <b>el bosque cerrado</b>: un casco entre las hojas y helechos junto a la antorcha de la
        ///   bifurcación; las lianas cuelgan sobre el barranco;
        /// - <b>el puesto del vigía</b>, sobre las copas: su lámpara apagada y una lona podrida;
        /// - <b>el claro</b>: helechos a la entrada y un tronco caído entre los árboles del Acechador;
        /// - <b>la antesala</b>: el último casco antes de la arena, que queda vacía y a oscuras.
        /// </summary>
        public static readonly Decorado[] Decorados =
        {
            // La linde: la tumba, detrás del inicio; las lianas, bajo la segunda rama del barranco.
            new Decorado("Tumba", ConstructorEscenarioMundo3.Tumba, -2.5f, 0f),
            new Decorado("Lianas_Barranco", ConstructorEscenarioMundo3.Lianas, 25.3f, -GrosorRama, delante: true),

            // La trinchera.
            new Decorado("Sacos_Trinchera", ConstructorEscenarioMundo3.Sacos, 39.3f, -2f),
            new Decorado("Refugio_Trinchera", ConstructorEscenarioMundo3.Refugio, 43.5f, -2f),
            new Decorado("Caja_Trinchera", ConstructorEscenarioMundo3.Caja, 45.3f, -2f),

            // El bosque cerrado.
            new Decorado("Casco_Bosque", ConstructorEscenarioMundo3.Casco, 50.5f, 0f),
            new Decorado("Helechos_Bosque", ConstructorEscenarioMundo3.Helechos, 53f, 0f),

            // El puesto del vigía: cuelgan de su rama, por delante.
            new Decorado("Lampara_Vigia", ConstructorEscenarioMundo3.Lampara, 70.6f, 4.5f - GrosorRama, delante: true),
            new Decorado("Lona_Vigia", ConstructorEscenarioMundo3.Lona, 72.7f, 4.5f - GrosorRama, delante: true),

            // El claro y la antesala.
            new Decorado("Helechos_Claro", ConstructorEscenarioMundo3.Helechos, 118.5f, 0f, volteado: true),
            new Decorado("TroncoCaido_Claro", ConstructorEscenarioMundo3.TroncoCaido, 128.6f, 0f),
            new Decorado("Casco_Antesala", ConstructorEscenarioMundo3.Casco, 135.3f, 0f, volteado: true),
        };

        /// <summary>El territorio del Acechador: el claro. Embistiendo, frena al llegar a un extremo.</summary>
        public static Vector2 LimitesAcechador => new Vector2(PisadaDe("Claro").Desde, PisadaDe("Claro").Hasta);

        public const float Inicio = 2f;

        /// <summary>
        /// Los checkpoints (también en <c>LevelData.puntosDeControl</c>): en el fondo de la
        /// trinchera, con el eco de memoria; tras las copas, a 3,5 u del agua de la ciénaga (quien se
        /// ahoga vuelve ahí); y en la antesala. Curan.
        /// </summary>
        public static readonly Vector2[] Checkpoints =
        {
            new Vector2(41f, -2f),
            new Vector2(82.5f, 0f),
            new Vector2(137f, 0f),
        };

        public const float SueloArena = 0f;
        public const float TechoArena = 15f;

        /// <summary>La puerta que se cierra detrás del jugador, en el borde de la arena.</summary>
        public const float PuertaDesde = 140f;
        public const float PuertaHasta = 141f;

        /// <summary>El trigger de entrada: el jugador ya ha pasado la puerta.</summary>
        public const float Entrada = 143f;

        public const float Jefe = 153.5f;
        /// <summary>
        /// La puerta de salida: un árbol de 4,7 u de ancho con las raíces abiertas. En 163,5 cabe
        /// entera entre la rama de la derecha de la arena (acaba en 162) y la pared (166).
        /// </summary>
        public const float Salida = 163.5f;

        /// <summary>Por dentro de la puerta y de la pared de la derecha.</summary>
        public static Vector2 LimitesArena => new Vector2(PuertaHasta, Paredes.Single(p => p.Nombre == "ParedArena").Desde);

        /// <summary>
        /// Los saltos del recorrido, en orden. Todos se dan caminando (Ctrl): ninguno obliga a correr.
        /// Bajar a la trinchera y a la hondonada no es un salto: es dejarse caer.
        /// </summary>
        public static readonly Paso[] Pasos =
        {
            // El barranco, por sus ramas; y si se cae, se sale con el doble salto por uno u otro lado.
            new Paso("SueloInicio", "RamaDelBarranco1"),
            new Paso("RamaDelBarranco1", "RamaPodridaDelBarranco"),
            new Paso("RamaPodridaDelBarranco", "RamaDelBarranco2"),
            new Paso("RamaDelBarranco2", "SueloDeLaLinde"),
            new Paso("FondoDelBarranco", "SueloInicio"),
            new Paso("FondoDelBarranco", "SueloDeLaLinde"),

            // La trinchera.
            new Paso("FondoDeLaTrinchera", "SueloDelBosque"),

            // Las copas.
            new Paso("SueloDelBosque", "RamaDeLaBifurcacion"),
            new Paso("RamaDeLaBifurcacion", "RamaDeLasCopas"),
            new Paso("RamaDeLasCopas", "RamaPodridaDeLasCopas"),
            new Paso("RamaPodridaDeLasCopas", "PuestoDelVigia"),
            new Paso("PuestoDelVigia", "RamaPodridaDeLaBajada"),
            new Paso("RamaPodridaDeLaBajada", "RamaDeLaBajada"),

            // La hondonada: se vuelve por el escalón.
            new Paso("FondoDeLaHondonada", "EscalonDeLaHondonada"),
            new Paso("EscalonDeLaHondonada", "SueloDelBosque"),

            // La ciénaga: se baja al agua dejándose caer; se sale al tocón y a la otra orilla bajo el agua.
            new Paso("FondoDeLaCienaga1", "Tocon", MEDIOS.AGUA),
            new Paso("FondoDeLaCienaga2", "SueloTrasLaCienaga", MEDIOS.AGUA),

            // Hasta la arena y sus ramas.
            new Paso("SueloTrasLaCienaga", "Claro"),
            new Paso("Claro", "Antesala"),
            new Paso("Antesala", "SueloArena"),
            new Paso("SueloArena", "RamaArenaIzquierda"),
            new Paso("SueloArena", "RamaArenaDerecha"),
        };

        /// <summary>La trampa del laberinto: el fondo de la hondonada.</summary>
        public const string LaTrampa = "FondoDeLaHondonada";

        public static bool EsDe(LevelData nivel) => nivel != null && nivel.id == IdNivel;

        /// <summary>Todo lo que se pisa: suelos, ramas sanas y podridas y las ramas de la arena.</summary>
        public static IEnumerable<Bloque> TodoLoQueSePisa => Suelos.Concat(Ramas).Concat(RamasPodridas).Concat(PlataformasArena);

        /// <summary>Lo que se pisa con ese nombre.</summary>
        public static Pisada PisadaDe(string nombre)
        {
            foreach (var bloque in TodoLoQueSePisa)
            {
                if (bloque.Nombre == nombre)
                {
                    return new Pisada(nombre, bloque.Desde, bloque.Nombre == "SueloArena" ? LimitesArena.y : bloque.Hasta, bloque.Arriba);
                }
            }

            throw new KeyNotFoundException($"No hay ninguna pisada llamada {nombre} en el Nivel 3.");
        }

        /// <summary>La altura de lo que se pisa bajo (x, desde): lo más alto que la cubre sin pasar de esa altura.</summary>
        public static float SueloBajo(float x, float desde = float.MaxValue)
        {
            var bajo = TodoLoQueSePisa.Where(b => x >= b.Desde && x < b.Hasta && b.Arriba <= desde + 1e-4f).ToList();
            return bajo.Count > 0 ? bajo.Max(b => b.Arriba) : Fondo;
        }

        private static Bloque Rama(string nombre, float desde, float hasta, float arriba) =>
            new Bloque(nombre, desde, hasta, arriba - GrosorRama, arriba);
    }
}
