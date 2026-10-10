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
    /// </summary>
    public static class DisenoNivel2
    {
        public const int IdNivel = 2;

        /// <summary>Sube al cambiar el recorrido: <see cref="PreparadorNivel"/> lo vuelve a montar entero.</summary>
        public const int Version = 6;

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

        // --- Sección 1 (−6 a 36): la superficie helada. Tres Lamentos, una Escultura y la Plañidera ---
        // --- Sección 2 (36 a 61): hundirse. La calle, la campana, la torre y cuatro Medusas ---
        // --- Arena (61 a 96): la antesala y la sala de la Doncella, de 25 × 15 u ---

        /// <summary>Lo que se pisa y su relleno (capa del suelo).</summary>
        public static readonly Bloque[] Suelos =
        {
            new Bloque("SueloInicio", -6f, 5f, Fondo, 0f),
            new Bloque("PrimerHielo", 5f, 13f, Fondo, 0f),
            new Bloque("FondoDeLasPlacas", 13f, 21f, Fondo, -5f),
            new Bloque("EscalonDeLasPlacas", 20f, 21f, -5f, -3f),
            new Bloque("Plaza", 21f, 36f, Fondo, 0f),
            new Bloque("Puente", 25f, 30f, 0f, 2f),
            new Bloque("SueloCalle", 36f, 60f, Fondo, -11f),
            new Bloque("RepisaDeLaCampana", 49f, 51f, -8f, -7.5f),
            new Bloque("Rellano1", 57f, 60f, -7.25f, -6.75f),
            new Bloque("Rellano2", 54f, 57f, -3f, -2.5f),
            new Bloque("Rellano3", 57f, 60f, 0.75f, 1.25f),
            new Bloque("Antesala", 61f, 69f, Fondo, 0f),
            new Bloque("SueloArena", 69f, 96f, Fondo, 0f),
        };

        /// <summary>
        /// Muros y techos (capa de las paredes). El techo de hielo cubre la calle: no se pisa porque
        /// la muralla corta el paso por arriba. La campana cuelga de él y guarda el aire dentro.
        /// </summary>
        public static readonly Bloque[] Paredes =
        {
            new Bloque("ParedIzquierda", -7f, -6f, Fondo, Tope),
            new Bloque("TechoDeHielo", 38f, 53f, -1f, 0f),
            new Bloque("Muralla", 38f, 40f, 0f, 9f),
            new Bloque("CampanaOeste", 46f, 47f, -8.5f, -1f),
            new Bloque("CampanaEste", 52f, 53f, -8.5f, -1f),
            new Bloque("TorreOeste", 53f, 54f, -4f, 7.5f),
            new Bloque("TorreEsteBaja", 60f, 61f, Fondo, 1.25f),
            new Bloque("TorreEsteAlta", 60f, 61f, 4.5f, 7.5f),
            new Bloque("TejadoDeLaTorre", 53f, 61f, 7.5f, 8.5f),
            new Bloque("ParedArena", 95f, 96f, 0f, Tope),
            new Bloque("TechoArena", 69f, 96f, 15f, Tope),
        };

        /// <summary>El balcón de la arena: nunca se inunda. La Doncella lo atraviesa.</summary>
        public static readonly Bloque[] PlataformasArena =
        {
            new Bloque("Balcon", 80f, 85f, 6.5f, 7f),
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
            new Placa("PlacaArena1", 76f, 3.5f, amplitud: 0.5f, desfase: 0f),
            new Placa("PlacaArena2", 89f, 3.5f, amplitud: 0.5f, desfase: PeriodoPlaca * 0.5f),
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
            new Zona("AguaDeLaCalle", 36f, 46f, -11f, -1f),
            new Zona("AguaBajoLaCampana", 46f, 53f, -11f, -7.5f - BajoLaSuperficie),
            new Zona("AguaAntesDeLaTorre", 53f, 54f, -11f, -4f),
            new Zona("AguaDeLaTorre", 54f, 60f, -11f, 1.25f - BajoLaSuperficie),
        };

        /// <summary>Donde se respira bajo el agua: dentro de la campana y en el campanario.</summary>
        public static readonly Zona[] BolsasDeAire =
        {
            new Zona("BolsaDeLaCampana", 47f, 52f, -7.5f - BajoLaSuperficie, -1f),
            new Zona("Campanario", 54f, 60f, 1.25f - BajoLaSuperficie, 7.5f),
        };

        /// <summary>
        /// Lo que resbala: una franja de media unidad sobre cada suelo de hielo. El puente y la
        /// antesala son de piedra: allí se pelea bien.
        /// </summary>
        public static readonly Zona[] Hielos =
        {
            new Zona("HieloDelPrimerTramo", 5f, 13f, 0f, 0.5f),
            new Zona("HieloDeLaPlazaOeste", 21f, 25f, 0f, 0.5f),
            new Zona("HieloDeLaPlazaEste", 30f, 36f, 0f, 0.5f),
            new Zona("HieloDeLaArena", 70f, 95f, 0f, 0.5f),
        };

        /// <summary>El hueco del suelo de la plaza que lleva a la calle. Lo tapa el hielo rompible.</summary>
        public static readonly Zona Agujero = new Zona("Agujero", 36f, 38f, -1f, 0f);

        /// <summary>
        /// El tapón de hielo rompible, con su pivote arriba en el centro: tapa el agujero desde su
        /// fondo (y −1) y asoma 1,25 u sobre la plaza, para que la espada lo alcance de frente.
        /// </summary>
        public static readonly Vector2 HieloRompible = new Vector2(37f, 1.25f);

        public static readonly Enemigo[] Enemigos =
        {
            new Enemigo("Lamento_1", TIPOS_ENEMIGO.LAMENTO, 9f, 0f, 7f, 11f),
            new Enemigo("Escultura_1", TIPOS_ENEMIGO.ESCULTURA, 23.5f, 0f, 23.5f, 23.5f),
            new Enemigo("Lamento_2", TIPOS_ENEMIGO.LAMENTO, 27.5f, 2f, 26f, 29f),
            // El mini-jefe, donde estaba la segunda Escultura: guarda el tapón, la única salida.
            new Enemigo("Planidera", TIPOS_ENEMIGO.PLANIDERA, 32f, 0f, 31f, 33f),
            new Enemigo("Lamento_3", TIPOS_ENEMIGO.LAMENTO, 34.5f, 0f, 33.5f, 35.5f),
            // La calle: suben y bajan a la contra, cada 4 s. Por encima de ellas quedan 3,5 u de agua libre.
            new Enemigo("Medusa_1", TIPOS_ENEMIGO.MEDUSA, 40.5f, -7.75f, 40.5f, 40.5f, vertical: true, amplitud: 2.75f, periodo: 4f, desfase: 0f),
            new Enemigo("Medusa_2", TIPOS_ENEMIGO.MEDUSA, 43.5f, -7.75f, 43.5f, 43.5f, vertical: true, amplitud: 2.75f, periodo: 4f, desfase: 2f),
            // La torre: cruzan de pared a pared cada 5 s, entre los rellanos y sin rozar a Kael en ellos.
            new Enemigo("Medusa_3", TIPOS_ENEMIGO.MEDUSA, 57f, -4f, 54.5f, 59.5f, vertical: false, amplitud: 2.5f, periodo: 5f, desfase: 0f),
            new Enemigo("Medusa_4", TIPOS_ENEMIGO.MEDUSA, 57f, 0.1f, 54.5f, 59.5f, vertical: false, amplitud: 2.5f, periodo: 5f, desfase: 2.5f),
        };

        public const float Inicio = 2f;

        /// <summary>
        /// Los checkpoints (también en <c>LevelData.puntosDeControl</c>): en la repisa seca de la
        /// campana, junto a la silla vacía, y en la antesala, antes del jefe.
        /// </summary>
        public static readonly Vector2[] Checkpoints =
        {
            new Vector2(50f, -7.5f),
            new Vector2(65f, 0f),
        };

        public const float SueloArena = 0f;
        public const float TechoArena = 15f;

        /// <summary>La puerta que se cierra detrás del jugador, en el borde de la arena.</summary>
        public const float PuertaDesde = 69f;
        public const float PuertaHasta = 70f;

        /// <summary>El trigger de entrada: el jugador ya ha aterrizado y la puerta queda detrás.</summary>
        public const float Entrada = 72f;

        public const float Jefe = 82.5f;
        public const float Salida = 93f;

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
            new Zona("FranjaA", 70f, 78f, SueloArena, SueloArena + AguaDeLaInundacion),
            new Zona("FranjaB", 78f, 87f, SueloArena, SueloArena + AguaDeLaInundacion),
            new Zona("FranjaC", 87f, 95f, SueloArena, SueloArena + AguaDeLaInundacion),
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
