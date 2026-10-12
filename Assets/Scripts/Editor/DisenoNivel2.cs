using System.Collections.Generic;
using System.Linq;
using Fragmentum.Data;
using UnityEngine;
using Bloque = Fragmentum.EditorTools.DisenoNivel1.Bloque;
using Pisada = Fragmentum.EditorTools.DisenoNivel1.Pisada;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El recorrido del Nivel 2 (Tristeza, Ciudad Sumergida): la propuesta de
    /// <c>Docs/Diseno/Nivel2_Diseno.md</c>, aprobada el 2026-10-08. Lo usan
    /// <see cref="PreparadorNivel"/> para montar la escena y las pruebas para comprobar que todo
    /// se puede cruzar con los saltos de Kael, en seco y bajo el agua.
    ///
    /// <b>La idea:</b> arriba todo resbala y nada agarra, y la superficie no lleva a ninguna parte;
    /// la única salida es hundirse. Bajo el agua todo pesa, pero Kael salta más alto: la torre
    /// solo se sube así («la Tristeza pesa, pero también sostiene»).
    ///
    /// <b>Las medidas.</b> x e y en unidades (1 u = 1 tile). Los bordes horizontales de los bloques
    /// caen en unidades enteras, como en el Nivel 1. Cada agua llega casi a la altura de lo que se
    /// pisa al salir de ella (<see cref="BajoLaSuperficie"/> por debajo): al cruzar la superficie la
    /// gravedad vuelve a ser la de fuera y el salto se queda corto, así que ninguna subida termina
    /// muy por encima del agua de la que sale.
    ///
    /// Cambios frente al esquema de la propuesta: la casa es una campana que cuelga del techo de
    /// hielo y guarda el aire debajo (sin suelo propio); los dos primeros escalones de la torre
    /// miden 4,25 u (el esquema decía 4,5: con el margen de las pruebas no llegaba) y el último
    /// 3,75, porque sale del agua; y todo lo de la sección 2 se corrió media unidad para caer en
    /// unidades enteras.
    ///
    /// Fase B (mecánicas): el tapón de hielo rompible, las placas que suben y bajan, los ecos de
    /// intentos y el eco de memoria en el primer checkpoint.
    ///
    /// Fase C (enemigos): las Esculturas dormidas y las Medusas con su ritmo. Las dos Medusas de la
    /// torre bajaron y subieron medio metro respecto de la fase A: a su altura de entonces rozaban
    /// la cabeza de Kael (1,8 u) mientras esperaba en los rellanos. La Plañidera, el mini-jefe,
    /// ocupa el sitio de la segunda Escultura (PENDIENTES N32): la plaza este, entre el puente y el
    /// tapón de hielo, la única salida de la superficie.
    ///
    /// Fase D (jefe): la Doncella flota, y el suelo de su arena se parte en tres franjas que ella
    /// inunda y congela (<see cref="Franjas"/>). El agua sube hasta <see cref="CapaHelada"/> (el
    /// diseño decía 4): a esa altura, la capa de hielo que deja al congelarse se alcanza desde el
    /// hielo seco con el doble salto, que a 4 u se quedaba en el límite exacto.
    ///
    /// Tras la prueba del autor (2026-10-12, PENDIENTES N31): la plaza este mide 16 u y no 6, para
    /// que la Plañidera tenga sitio; la campana es 3 u más ancha y su repisa mide 4, con la mesa
    /// junto a la silla vacía; la muralla es de hielo liso y no agarra (<see cref="NoAgarra"/>),
    /// porque con el salto de pared se subía y se saltaba medio nivel; y junto al tapón Kael dice
    /// cómo se rompe (<see cref="PistaDelTapon"/>). Todo lo que hay del tapón en adelante se
    /// corrió 10 u, y de la campana en adelante, 13.
    /// </summary>
    public static class DisenoNivel2
    {
        public const int IdNivel = 2;

        /// <summary>Sube al cambiar el recorrido: <see cref="PreparadorNivel"/> lo vuelve a montar entero.</summary>
        public const int Version = 12;

        /// <summary>Base de todos los bloques: por debajo de lo que enseña la cámara (−13).</summary>
        public const float Fondo = -14f;

        /// <summary>Tope de los muros de la arena: por encima de lo que enseña la cámara (20).</summary>
        public const float Tope = 22f;

        /// <summary>Lo que enseña la cámara en vertical (mínimo, máximo): del suelo de la calle al techo de la arena.</summary>
        public static readonly Vector2 AlturaCamara = new Vector2(-13f, 20f);

        /// <summary>El encuadre de la cámara del Nivel 1, para que los dos mundos se vean a la misma escala.</summary>
        public const float TamanoCamara = 4.85f;

        /// <summary>La zona mortal: muy por debajo de la calle, que es lo más hondo del nivel.</summary>
        public const float ZonaMortal = -16f;

        public enum MEDIOS
        {
            SECO,
            AGUA
        }

        /// <summary>Un rectángulo del nivel que no es un bloque: agua, hielo o una bolsa de aire.</summary>
        public readonly struct Zona
        {
            public Zona(string nombre, float desde, float hasta, float abajo, float arriba)
            {
                Nombre = nombre;
                Desde = desde;
                Hasta = hasta;
                Abajo = abajo;
                Arriba = arriba;
            }

            public string Nombre { get; }
            public float Desde { get; }
            public float Hasta { get; }
            public float Abajo { get; }
            public float Arriba { get; }

            public Rect Rect => Rect.MinMaxRect(Desde, Abajo, Hasta, Arriba);

            public bool Contiene(float x, float y) => x >= Desde && x <= Hasta && y >= Abajo - 1e-4f && y <= Arriba + 1e-4f;

            public override string ToString() => $"{Nombre} (x {Desde:0.##}-{Hasta:0.##}, y {Abajo:0.##}-{Arriba:0.##})";
        }

        /// <summary>
        /// Una placa de hielo flotante centrada en X, con la superficie a la altura Y en el centro de
        /// su oscilación: sube y baja Amplitud u en un seno de <see cref="PeriodoPlaca"/> s, con
        /// Desfase s de adelanto.
        /// </summary>
        public readonly struct Placa
        {
            public Placa(string nombre, float x, float y, float amplitud, float desfase)
            {
                Nombre = nombre;
                X = x;
                Y = y;
                Amplitud = amplitud;
                Desfase = desfase;
            }

            public string Nombre { get; }
            public float X { get; }
            public float Y { get; }
            public float Amplitud { get; }
            public float Desfase { get; }
            public float Ancho => AnchoPlaca;
            public float Desde => X - Ancho * 0.5f;
            public float Hasta => X + Ancho * 0.5f;
        }

        public enum TIPOS_ENEMIGO
        {
            LAMENTO,
            ESCULTURA,
            MEDUSA,
            PLANIDERA
        }

        /// <summary>
        /// Un enemigo en (X, Y). Los que andan (el Lamento y la Plañidera) patrullan de Desde a
        /// Hasta en x; la Escultura no anda (Desde = Hasta = X); la Medusa flota en un seno de
        /// Amplitud alrededor de (X, Y), en vertical o en horizontal, de Periodo s y con Desfase s
        /// de adelanto.
        /// </summary>
        public readonly struct Enemigo
        {
            public Enemigo(string nombre, TIPOS_ENEMIGO tipo, float x, float y, float desde, float hasta,
                bool vertical = false, float amplitud = 0f, float periodo = 0f, float desfase = 0f)
            {
                Nombre = nombre;
                Tipo = tipo;
                X = x;
                Y = y;
                Desde = desde;
                Hasta = hasta;
                Vertical = vertical;
                Amplitud = amplitud;
                Periodo = periodo;
                Desfase = desfase;
            }

            public string Nombre { get; }
            public TIPOS_ENEMIGO Tipo { get; }
            public float X { get; }
            public float Y { get; }
            public float Desde { get; }
            public float Hasta { get; }
            public bool Vertical { get; }
            public float Amplitud { get; }
            public float Periodo { get; }
            public float Desfase { get; }
        }

        /// <summary>
        /// Escala de los Lamentos: son los Ecos de este mundo y van al tamaño que el diseñador dio a
        /// los Ecos de la Ira en el Nivel 1 (<see cref="DisenoNivel1.EscalaEco"/>, PENDIENTES N18).
        /// </summary>
        public static readonly Vector2 EscalaLamento = DisenoNivel1.EscalaEco;

        /// <summary>Alto de Kael (su cápsula): lo que tiene que quedar libre sobre un rellano.</summary>
        public const float AltoKael = 1.8f;

        /// <summary>Radio de la Medusa (su dibujo) y de lo que quema (su contacto), los de su prefab.</summary>
        public const float RadioMedusa = GeneradorEnemigos.RadioMedusa;
        public const float RadioContactoMedusa = GeneradorEnemigos.RadioContactoMedusa;

        /// <summary>Un salto del recorrido, de una pisada a otra, y en qué medio se da.</summary>
        public readonly struct Paso
        {
            public Paso(string desde, string hasta, MEDIOS medio)
            {
                Desde = desde;
                Hasta = hasta;
                Medio = medio;
            }

            public string Desde { get; }
            public string Hasta { get; }
            public MEDIOS Medio { get; }

            public override string ToString() => $"{Desde} → {Hasta} ({Medio})";
        }

        /// <summary>El ancho de la placa flotante (el de su prefab).</summary>
        public const float AnchoPlaca = 2.5f;

        /// <summary>Segundos de una oscilación de las placas.</summary>
        public const float PeriodoPlaca = 3f;

        // --- Sección 1 (−6 a 46): la superficie helada. Tres Lamentos, una Escultura y la Plañidera ---
        // --- Sección 2 (46 a 74): hundirse. La calle, la campana, la torre y cuatro Medusas ---
        // --- Arena (74 a 109): la antesala y la sala de la Doncella, de 25 × 15 u ---

        /// <summary>
        /// Los tramos del <c>LevelData</c> (desde, hasta). De ellos sale lo que enseña la cámara,
        /// que llega 6 u más allá de cada extremo: de la pared de la izquierda (−6) a la del fondo
        /// de la arena (109). Los pone <see cref="GeneradorProgresion"/>.
        /// </summary>
        public static readonly Vector2[] Secciones =
        {
            new Vector2(0f, 40f),
            new Vector2(40f, 70f),
            new Vector2(70f, 103f),
        };

        /// <summary>Lo que la cámara pasa de cada extremo de las secciones (<c>LevelManager.margenCamara</c>).</summary>
        public const float MargenCamara = 6f;

        /// <summary>Lo que se pisa y su relleno (capa del suelo).</summary>
        public static readonly Bloque[] Suelos =
        {
            new Bloque("SueloInicio", -6f, 5f, Fondo, 0f),
            new Bloque("PrimerHielo", 5f, 13f, Fondo, 0f),
            new Bloque("FondoDeLasPlacas", 13f, 21f, Fondo, -5f),
            new Bloque("EscalonDeLasPlacas", 20f, 21f, -5f, -3f),
            new Bloque("Plaza", 21f, 46f, Fondo, 0f),
            new Bloque("Puente", 25f, 30f, 0f, 2f),
            new Bloque("SueloCalle", 46f, 73f, Fondo, -11f),
            new Bloque("RepisaDeLaCampana", 59f, 63f, -8f, -7.5f),
            new Bloque("Rellano1", 70f, 73f, -7.25f, -6.75f),
            new Bloque("Rellano2", 67f, 70f, -3f, -2.5f),
            new Bloque("Rellano3", 70f, 73f, 0.75f, 1.25f),
            new Bloque("Antesala", 74f, 82f, Fondo, 0f),
            new Bloque("SueloArena", 82f, 109f, Fondo, 0f),
        };

        /// <summary>
        /// Muros y techos (capa de las paredes). El techo de hielo cubre la calle: no se pisa porque
        /// la muralla corta el paso por arriba. La campana cuelga de él y guarda el aire dentro.
        /// </summary>
        public static readonly Bloque[] Paredes =
        {
            new Bloque("ParedIzquierda", -7f, -6f, Fondo, Tope),
            new Bloque("TechoDeHielo", 48f, 66f, -1f, 0f),
            new Bloque("Muralla", 48f, 50f, 0f, 9f),
            new Bloque("CampanaOeste", 56f, 57f, -8.5f, -1f),
            new Bloque("CampanaEste", 65f, 66f, -8.5f, -1f),
            new Bloque("TorreOeste", 66f, 67f, -4f, 7.5f),
            new Bloque("TorreEsteBaja", 73f, 74f, Fondo, 1.25f),
            new Bloque("TorreEsteAlta", 73f, 74f, 4.5f, 7.5f),
            new Bloque("TejadoDeLaTorre", 66f, 74f, 7.5f, 8.5f),
            new Bloque("ParedArena", 108f, 109f, 0f, Tope),
            new Bloque("TechoArena", 82f, 109f, 15f, Tope),
        };

        /// <summary>
        /// Los bloques que son de hielo macizo y no de ladrillo: el techo helado de la calle y la
        /// muralla que cierra la plaza.
        /// </summary>
        public static bool EsDeHielo(string bloque) => bloque == "TechoDeHielo" || bloque == "Muralla";

        /// <summary>
        /// Las paredes en las que Kael no se agarra (<see cref="Fragmentum.Core.SinAgarre"/>): la
        /// muralla. Es de hielo liso: sin esto se subía con el salto de pared, se andaba por el
        /// techo helado y por el tejado de la torre, y se caía en la antesala sin haber bajado al agua.
        /// </summary>
        public static bool NoAgarra(string bloque) => bloque == "Muralla";

        /// <summary>
        /// La silla vacía del primer checkpoint (el punto de sus patas): en la repisa de la campana,
        /// a la izquierda de la mesa y de la bandera.
        /// </summary>
        public static readonly Vector2 SillaVacia = new Vector2(59.6f, -7.5f);

        /// <summary>El centro de la torre del reloj: ahí cuelga su campana y ahí se ve la del fondo.</summary>
        public const float CentroDeLaTorre = 70f;

        /// <summary>La campana del campanario: cuelga del tejado de la torre, en el centro.</summary>
        public static readonly Vector2 CampanaDelCampanario = new Vector2(CentroDeLaTorre, 7.5f);

        /// <summary>El balcón de la arena: nunca se inunda. La Doncella lo atraviesa.</summary>
        public static readonly Bloque[] PlataformasArena =
        {
            new Bloque("Balcon", 93f, 98f, 6.5f, 7f),
        };

        /// <summary>
        /// Las placas de hielo flotantes. Las del agua de la sección 1 suben de ras del hielo (y 0) a
        /// y 1, a la contra (medio periodo de desfase): cuando una sube, la otra baja. En lo más bajo
        /// siguen por encima del agua, así que encima de ellas nunca se está mojado. Las de la arena,
        /// a media altura y también a la contra.
        /// </summary>
        public static readonly Placa[] Placas =
        {
            new Placa("PlacaFlotante1", 15.25f, 0.5f, amplitud: 0.5f, desfase: 0f),
            new Placa("PlacaFlotante2", 18.75f, 0.5f, amplitud: 0.5f, desfase: PeriodoPlaca * 0.5f),
            new Placa("PlacaArena1", 89f, 3.5f, amplitud: 0.5f, desfase: 0f),
            new Placa("PlacaArena2", 102f, 3.5f, amplitud: 0.5f, desfase: PeriodoPlaca * 0.5f),
        };

        /// <summary>
        /// Lo que queda la superficie del agua por debajo de lo que se pisa al salir. Si estuviera a
        /// ras, Kael de pie en el borde seguiría rozando el agua (la física cuenta como solapados
        /// dos cuerpos que solo se tocan) y andaría frenado sobre la repisa o el rellano.
        /// </summary>
        public const float BajoLaSuperficie = 0.1f;

        /// <summary>
        /// El agua. Los rectángulos no se solapan (dos aguas a la vez frenarían el doble) y dejan
        /// fuera las bolsas de aire. Cada superficie queda <see cref="BajoLaSuperficie"/> por debajo
        /// de lo que se pisa al salir; la de la calle llega al techo de hielo.
        /// </summary>
        public static readonly Zona[] Aguas =
        {
            new Zona("AguaDeLasPlacas", 13f, 21f, -5f, 0f - BajoLaSuperficie),
            new Zona("AguaDeLaCalle", 46f, 56f, -11f, -1f),
            new Zona("AguaBajoLaCampana", 56f, 66f, -11f, -7.5f - BajoLaSuperficie),
            new Zona("AguaAntesDeLaTorre", 66f, 67f, -11f, -4f),
            new Zona("AguaDeLaTorre", 67f, 73f, -11f, 1.25f - BajoLaSuperficie),
        };

        /// <summary>Donde se respira bajo el agua: dentro de la campana y en el campanario.</summary>
        public static readonly Zona[] BolsasDeAire =
        {
            new Zona("BolsaDeLaCampana", 57f, 65f, -7.5f - BajoLaSuperficie, -1f),
            new Zona("Campanario", 67f, 73f, 1.25f - BajoLaSuperficie, 7.5f),
        };

        /// <summary>
        /// Lo que resbala: una franja de media unidad sobre cada suelo de hielo. El puente y la
        /// antesala son de piedra: allí se pelea bien.
        /// </summary>
        public static readonly Zona[] Hielos =
        {
            new Zona("HieloDelPrimerTramo", 5f, 13f, 0f, 0.5f),
            new Zona("HieloDeLaPlazaOeste", 21f, 25f, 0f, 0.5f),
            new Zona("HieloDeLaPlazaEste", 30f, 46f, 0f, 0.5f),
            new Zona("HieloDeLaArena", 83f, 108f, 0f, 0.5f),
        };

        /// <summary>El hueco del suelo de la plaza que lleva a la calle. Lo tapa el hielo rompible.</summary>
        public static readonly Zona Agujero = new Zona("Agujero", 46f, 48f, -1f, 0f);

        /// <summary>
        /// El tapón de hielo rompible, con su pivote arriba en el centro: tapa el agujero desde su
        /// fondo (y −1) y asoma 1,25 u sobre la plaza, para que la espada lo alcance de frente.
        /// </summary>
        public static readonly Vector2 HieloRompible = new Vector2(47f, 1.25f);

        /// <summary>
        /// Donde Kael dice cómo se rompe el tapón (la línea «pista» de sus diálogos): las 4 u de
        /// plaza que hay delante de él. La dice también si lo agrieta con un golpe normal.
        /// </summary>
        public static readonly Zona PistaDelTapon = new Zona("PistaDelTapon", 42f, 46f, 0f, 3f);

        public static readonly Enemigo[] Enemigos =
        {
            new Enemigo("Lamento_1", TIPOS_ENEMIGO.LAMENTO, 9f, 0f, 7f, 11f),
            new Enemigo("Escultura_1", TIPOS_ENEMIGO.ESCULTURA, 23.5f, 0f, 23.5f, 23.5f),
            new Enemigo("Lamento_2", TIPOS_ENEMIGO.LAMENTO, 27.5f, 2f, 26f, 29f),
            // El mini-jefe, donde estaba la segunda Escultura: guarda el tapón, la única salida.
            // En mitad de la plaza este, que mide 16 u: flota 2 u a cada lado.
            new Enemigo("Planidera", TIPOS_ENEMIGO.PLANIDERA, 38f, 0f, 36f, 40f),
            new Enemigo("Lamento_3", TIPOS_ENEMIGO.LAMENTO, 44.5f, 0f, 43.5f, 45.5f),
            // La calle: suben y bajan a la contra, cada 4 s. Por encima de ellas quedan 3,5 u de agua libre.
            new Enemigo("Medusa_1", TIPOS_ENEMIGO.MEDUSA, 50.5f, -7.75f, 50.5f, 50.5f, vertical: true, amplitud: 2.75f, periodo: 4f, desfase: 0f),
            new Enemigo("Medusa_2", TIPOS_ENEMIGO.MEDUSA, 53.5f, -7.75f, 53.5f, 53.5f, vertical: true, amplitud: 2.75f, periodo: 4f, desfase: 2f),
            // La torre: cruzan de pared a pared cada 5 s, entre los rellanos y sin rozar a Kael en ellos.
            new Enemigo("Medusa_3", TIPOS_ENEMIGO.MEDUSA, CentroDeLaTorre, -4f, 67.5f, 72.5f, vertical: false, amplitud: 2.5f, periodo: 5f, desfase: 0f),
            new Enemigo("Medusa_4", TIPOS_ENEMIGO.MEDUSA, CentroDeLaTorre, 0.1f, 67.5f, 72.5f, vertical: false, amplitud: 2.5f, periodo: 5f, desfase: 2.5f),
        };

        public const float Inicio = 2f;

        /// <summary>
        /// Los checkpoints (también en <c>LevelData.puntosDeControl</c>): en la repisa seca de la
        /// campana, pasadas la silla vacía y la mesa, y en la antesala, antes del jefe. Curan.
        /// </summary>
        public static readonly Vector2[] Checkpoints =
        {
            new Vector2(62.4f, -7.5f),
            new Vector2(78f, 0f),
        };

        public const float SueloArena = 0f;
        public const float TechoArena = 15f;

        /// <summary>La puerta que se cierra detrás del jugador, en el borde de la arena.</summary>
        public const float PuertaDesde = 82f;
        public const float PuertaHasta = 83f;

        /// <summary>El trigger de entrada: el jugador ya ha aterrizado y la puerta queda detrás.</summary>
        public const float Entrada = 85f;

        public const float Jefe = 95.5f;
        public const float Salida = 106f;

        /// <summary>Hasta dónde llega la zona mortal, a cada lado del nivel.</summary>
        public static readonly Vector2 AnchoZonaMortal = new Vector2(-20f, 125f);

        /// <summary>Por dentro de la puerta y de la pared de la derecha.</summary>
        public static Vector2 LimitesArena => new Vector2(PuertaHasta, Paredes.Single(p => p.Nombre == "ParedArena").Desde);

        /// <summary>
        /// Lo que se pisa cuando una franja inundada se congela: la capa de hielo. Se sube desde el
        /// hielo seco con el doble salto (con uno solo, no) y desde el agua de al lado.
        /// </summary>
        public const float CapaHelada = 3.5f;

        public const float GrosorCapaHelada = 0.5f;

        /// <summary>Hasta dónde sube el agua de la inundación: casi a ras de la capa, como las demás aguas.</summary>
        public const float AguaDeLaInundacion = CapaHelada - BajoLaSuperficie;

        /// <summary>
        /// Altura de los pies de la Doncella mientras flota. Por encima de la cabeza de Kael y de su
        /// espada, y con su cuerpo entero por debajo del balcón y dentro de lo que enseña la cámara
        /// cuando Kael está en el suelo.
        /// </summary>
        public const float VueloDoncella = 3f;

        /// <summary>
        /// Las tres franjas del suelo de la arena que la Doncella inunda y congela, con el agua
        /// llena: A y C llevan dentro una placa, y B queda bajo el balcón, que nunca se inunda.
        /// </summary>
        public static readonly Zona[] Franjas =
        {
            new Zona("FranjaA", 83f, 91f, SueloArena, SueloArena + AguaDeLaInundacion),
            new Zona("FranjaB", 91f, 100f, SueloArena, SueloArena + AguaDeLaInundacion),
            new Zona("FranjaC", 100f, 108f, SueloArena, SueloArena + AguaDeLaInundacion),
        };

        /// <summary>
        /// Una pieza del tileset o de los props colocada de adorno: su pivote en (X, Y). No se pisa
        /// ni choca. Las que cuelgan o tapan una pared van por delante de los bloques.
        /// </summary>
        public readonly struct Decorado
        {
            public Decorado(string nombre, ConstructorEscenarioMundo2.Pieza pieza, float x, float y, bool volteado = false, bool delante = false)
            {
                Nombre = nombre;
                Pieza = pieza;
                X = x;
                Y = y;
                Volteado = volteado;
                Delante = delante;
            }

            public string Nombre { get; }
            public ConstructorEscenarioMundo2.Pieza Pieza { get; }
            public float X { get; }
            public float Y { get; }

            /// <summary>Dibujado en espejo, para que dos iguales no se vean iguales.</summary>
            public bool Volteado { get; }

            /// <summary>Por delante de los bloques (cuelga sobre una pared), no por detrás.</summary>
            public bool Delante { get; }

            public Vector2 Posicion => new Vector2(X, Y);
        }

        /// <summary>
        /// El atrezo (PENDIENTES N36: «decide tú»). La idea de cada cosa:
        /// - <b>la estatua</b> sale antes que la Escultura Viviente (en el inicio) y otra vez en la
        ///   plaza este: la primera enseña que las estatuas son decorado, y la Escultura lo desmiente;
        /// - <b>los faroles</b> marcan los tres sitios en los que se puede parar: el inicio, el
        ///   puente de piedra y la antesala;
        /// - <b>la plaza de la Plañidera</b> es una plaza en ruinas: una columna rota y unas rocas;
        /// - <b>la mesa</b>, junto a la silla vacía del eco de memoria («Alguien deja una silla vacía
        ///   en la mesa»);
        /// - <b>la calle hundida</b>: algas, la campana caída, otra columna y rocas en el fondo;
        /// - <b>las cadenas</b> a los lados de la campana del campanario, y <b>los estandartes</b>
        ///   sobre el frente del puente y bajo el balcón de la arena.
        /// La silla y la campana tienen su propio campo, que usan las pruebas.
        /// </summary>
        public static readonly Decorado[] Decorados =
        {
            // El inicio.
            new Decorado("Estatua_Inicio", ConstructorEscenarioMundo2.Estatua, -3f, 0f),
            new Decorado("Farol_Inicio", ConstructorEscenarioMundo2.Farol, 4.2f, 0f),

            // El puente de piedra: los estandartes caen sobre su frente.
            new Decorado("Estandartes_Puente", ConstructorEscenarioMundo2.Estandartes, 27.5f, 2f, delante: true),
            new Decorado("Farol_Puente", ConstructorEscenarioMundo2.Farol, 25.6f, 2f, volteado: true),

            // La plaza este, la de la Plañidera.
            new Decorado("Rocas_Plaza", ConstructorEscenarioMundo2.Rocas, 32.2f, 0f),
            new Decorado("Columna_Plaza", ConstructorEscenarioMundo2.ColumnaRota, 35f, 0f),
            new Decorado("Estatua_Plaza", ConstructorEscenarioMundo2.Estatua, 41.5f, 0f, volteado: true),

            // La calle hundida.
            new Decorado("Algas_Calle_1", ConstructorEscenarioMundo2.Algas, 49f, -11f),
            new Decorado("Columna_Calle", ConstructorEscenarioMundo2.ColumnaRota, 52f, -11f, volteado: true),
            new Decorado("CampanaCaida", ConstructorEscenarioMundo2.CampanaCaida, 55f, -11f),
            new Decorado("Algas_Campana", ConstructorEscenarioMundo2.Algas, 57.7f, -11f, volteado: true),
            new Decorado("Rocas_Calle", ConstructorEscenarioMundo2.Rocas, 61f, -11f, volteado: true),
            new Decorado("Algas_Calle_2", ConstructorEscenarioMundo2.Algas, 64.4f, -11f),
            new Decorado("Algas_Torre", ConstructorEscenarioMundo2.Algas, 68f, -11f, volteado: true),

            // La campana: la mesa, entre la silla vacía y la bandera.
            new Decorado("MesaVacia", ConstructorEscenarioMundo2.Mesa, 61f, -7.5f),

            // El campanario: las cadenas, a los lados de la campana.
            new Decorado("Cadena_Oeste", ConstructorEscenarioMundo2.Cadena, CentroDeLaTorre - 1.8f, 7.5f),
            new Decorado("Cadena_Este", ConstructorEscenarioMundo2.Cadena, CentroDeLaTorre + 1.8f, 7.5f, volteado: true),

            // La antesala y la arena.
            new Decorado("Farol_Antesala", ConstructorEscenarioMundo2.Farol, 76f, 0f),
            new Decorado("Estandartes_Balcon", ConstructorEscenarioMundo2.Estandartes, 95.5f, 6.5f),
        };

        /// <summary>
        /// Los saltos del recorrido, en orden, con el medio en que se dan. La caída por el agujero
        /// no es un salto: la comprueban aparte las pruebas.
        /// </summary>
        public static readonly Paso[] Pasos =
        {
            new Paso("SueloInicio", "PrimerHielo", MEDIOS.SECO),
            new Paso("PrimerHielo", "PlacaFlotante1", MEDIOS.SECO),
            new Paso("PlacaFlotante1", "PlacaFlotante2", MEDIOS.SECO),
            new Paso("PlacaFlotante2", "Plaza", MEDIOS.SECO),
            new Paso("Plaza", "Puente", MEDIOS.SECO),
            new Paso("FondoDeLasPlacas", "EscalonDeLasPlacas", MEDIOS.AGUA),
            new Paso("EscalonDeLasPlacas", "Plaza", MEDIOS.AGUA),
            new Paso("SueloCalle", "RepisaDeLaCampana", MEDIOS.AGUA),
            new Paso("SueloCalle", "Rellano1", MEDIOS.AGUA),
            new Paso("Rellano1", "Rellano2", MEDIOS.AGUA),
            new Paso("Rellano2", "Rellano3", MEDIOS.AGUA),
            new Paso("Rellano3", "Antesala", MEDIOS.SECO),
            new Paso("Antesala", "SueloArena", MEDIOS.SECO),
            new Paso("SueloArena", "PlacaArena1", MEDIOS.SECO),
            new Paso("SueloArena", "PlacaArena2", MEDIOS.SECO),
            new Paso("PlacaArena1", "Balcon", MEDIOS.SECO),
        };

        public static bool EsDe(LevelData nivel) => nivel != null && nivel.id == IdNivel;

        /// <summary>Lo que se pisa con ese nombre: un suelo, una plataforma de la arena o una placa.</summary>
        public static Pisada PisadaDe(string nombre)
        {
            foreach (var bloque in Suelos.Concat(PlataformasArena))
            {
                if (bloque.Nombre == nombre)
                {
                    return new Pisada(nombre, bloque.Desde, bloque.Nombre == "SueloArena" ? LimitesArena.y : bloque.Hasta, bloque.Arriba);
                }
            }

            foreach (var placa in Placas)
            {
                if (placa.Nombre == nombre)
                {
                    return new Pisada(nombre, placa.Desde, placa.Hasta, placa.Y);
                }
            }

            throw new KeyNotFoundException($"No hay ninguna pisada llamada {nombre} en el Nivel 2.");
        }

        /// <summary>El agua que hay en (x, y), si la hay.</summary>
        public static Zona? AguaEn(float x, float y)
        {
            foreach (var agua in Aguas)
            {
                if (agua.Contiene(x, y))
                {
                    return agua;
                }
            }

            return null;
        }

        /// <summary>La altura del suelo bajo (x, desde): el bloque más alto que la cubre sin pasar de esa altura.</summary>
        public static float SueloBajo(float x, float desde = float.MaxValue)
        {
            var bajo = Suelos.Concat(PlataformasArena).Where(b => x >= b.Desde && x < b.Hasta && b.Arriba <= desde + 1e-4f).ToList();
            return bajo.Count > 0 ? bajo.Max(b => b.Arriba) : Fondo;
        }
    }
}
