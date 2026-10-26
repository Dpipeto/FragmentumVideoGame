using System.Collections.Generic;
using System.Linq;
using Fragmentum.Data;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El recorrido del Nivel 1 (Ira, Fortaleza Volcánica) tal como se monta: la propuesta de
    /// <c>Docs/Diseno/Nivel1_Diseno.md</c>, aprobada, con las medidas reales de las piezas. Lo
    /// usan <see cref="PreparadorNivel"/> para montar la escena y las pruebas para comprobar que
    /// todo se puede cruzar con los saltos de Kael.
    ///
    /// <b>La idea:</b> la Ira empuja a correr y el nivel enseña a contenerla. Cada peligro sale
    /// tres veces: sin riesgo, sobre lava y junto a enemigos.
    ///
    /// <b>Las medidas.</b> x e y en unidades (1 u = 1 tile). Todos los bordes de los bloques caen
    /// en unidades enteras: el tileset pinta celdas enteras, y un bloque de 10,5 u se dibujaría con
    /// medio tile de más fuera de su colisión. La lava se dibuja detrás del suelo, así que puede
    /// pasarse un poco por debajo de las orillas y el hueco no tiene que medir lo que sus franjas.
    ///
    /// Cambios frente al esquema de la propuesta, por las piezas reales: la plataforma de
    /// combustión mide 3,42 u y la inestable 3,83 (el esquema dibujaba 1,8 y 1,6), así que el lago
    /// pasa de 6 a 9 u, la subida lleva una inestable y no dos, y el checkpoint queda en x 43.
    ///
    /// <b>La sección 3, «la forja» (2026-10-13).</b> Los dos niveles bien jugados se pasaban en diez
    /// minutos y el autor pidió alargarlos para que el puntaje tuviera recorrido. Entre el Golem y
    /// la arena hay ahora 45 u más, que son el examen de lo aprendido: una inestable sobre lava
    /// para subir a la almena, dos Ecos bajo el fuego de una segunda torreta, y un lago de 12 u
    /// con una inestable entre dos de combustión (hay que mirar la segunda antes de pisar la que
    /// se rompe). La arena, entera, se corre esas 45 u. Con dos checkpoints más: tras el Golem y
    /// ante la arena.
    /// </summary>
    public static class DisenoNivel1
    {
        public const int IdNivel = 1;

        /// <summary>
        /// Versión del recorrido. Al subirla, <see cref="PreparadorNivel"/> vuelve a montar entero
        /// el de la escena (lo que se haya movido a mano en él se pierde).
        /// </summary>
        public const int Version = 3;

        /// <summary>Base de todos los bloques: muy por debajo de lo que enseña la cámara (−6).</summary>
        public const float Fondo = -8f;

        /// <summary>Tope de los muros: por encima de lo que enseña la cámara (24).</summary>
        public const float Tope = 26f;

        /// <summary>Lo que mide una franja de lava (151 px a 48 px/u).</summary>
        public const float AnchoFranjaLava = 151f / 48f;

        /// <summary>Lo que mide de alto una franja de lava (64 px a 48 px/u).</summary>
        public const float AltoFranjaLava = 64f / 48f;

        /// <summary>Escala de los Ecos: la que dejó el diseñador en el Nivel 1 (PENDIENTES N18).</summary>
        public static readonly Vector2 EscalaEco = new Vector2(1.8f, 1.5f);

        /// <summary>Un bloque macizo del escenario, de (Desde, Abajo) a (Hasta, Arriba).</summary>
        public readonly struct Bloque
        {
            public Bloque(string nombre, float desde, float hasta, float abajo, float arriba)
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
        }

        /// <summary>Un hueco con lava: de Desde a Hasta, con la costra a la altura Costra.</summary>
        public readonly struct Lago
        {
            public Lago(string nombre, float desde, float hasta, float costra)
            {
                Nombre = nombre;
                Desde = desde;
                Hasta = hasta;
                Costra = costra;
            }

            public string Nombre { get; }
            public float Desde { get; }
            public float Hasta { get; }
            public float Costra { get; }
            public float Centro => (Desde + Hasta) * 0.5f;

            /// <summary>Franjas que hacen falta para cubrir el hueco (centradas, se meten bajo las orillas).</summary>
            public int Franjas => Mathf.CeilToInt((Hasta - Desde) / AnchoFranjaLava - 0.001f);
        }

        /// <summary>Una mecánica (prefab de <see cref="GeneradorMecanicas"/>) con su pivote en (X, Y).</summary>
        public readonly struct Pieza
        {
            public Pieza(string nombre, string prefab, float x, float y, bool miraIzquierda = false, float desfase = 0f)
            {
                Nombre = nombre;
                Prefab = prefab;
                X = x;
                Y = y;
                MiraIzquierda = miraIzquierda;
                Desfase = desfase;
            }

            public string Nombre { get; }
            public string Prefab { get; }
            public float X { get; }
            public float Y { get; }
            public bool MiraIzquierda { get; }

            /// <summary>Segundos de desfase del ciclo (plataformas de combustión).</summary>
            public float Desfase { get; }
        }

        /// <summary>Un enemigo de pie en (X, Y), con su ruta de patrulla entre Desde y Hasta.</summary>
        public readonly struct Guardia
        {
            public Guardia(string nombre, bool golem, float x, float y, float desde, float hasta)
            {
                Nombre = nombre;
                Golem = golem;
                X = x;
                Y = y;
                Desde = desde;
                Hasta = hasta;
            }

            public string Nombre { get; }
            public bool Golem { get; }
            public float X { get; }
            public float Y { get; }
            public float Desde { get; }
            public float Hasta { get; }
        }

        /// <summary>Una superficie que se pisa: (Desde, Hasta) en x, a la altura Y.</summary>
        public readonly struct Pisada
        {
            public Pisada(string nombre, float desde, float hasta, float y)
            {
                Nombre = nombre;
                Desde = desde;
                Hasta = hasta;
                Y = y;
            }

            public string Nombre { get; }
            public float Desde { get; }
            public float Hasta { get; }
            public float Y { get; }

            public override string ToString() => $"{Nombre} (x {Desde:0.##}-{Hasta:0.##}, y {Y:0.##})";
        }

        // --- Sección 1 (0-25): el despertar. Tres Ecos y la torreta ---
        // --- Sección 2 (25-55): la fortaleza arde. Cuatro Ecos, el Golem y el primer checkpoint ---
        // --- Sección 3 (55-100): la forja. Tres Ecos, otra torreta, dos lagos y dos checkpoints ---
        // --- Arena (100-126): la Centinela, en una sala de 25 × 15 u ---

        /// <summary>Un tramo del <c>LevelData</c>: de él sale lo que enseña la cámara.</summary>
        public readonly struct Tramo
        {
            public Tramo(string nombre, float desde, float hasta, string contenido)
            {
                Nombre = nombre;
                Desde = desde;
                Hasta = hasta;
                Contenido = contenido;
            }

            public string Nombre { get; }
            public float Desde { get; }
            public float Hasta { get; }
            public string Contenido { get; }
        }

        /// <summary>
        /// Los tramos del <c>LevelData</c>. Los pone <see cref="GeneradorProgresion"/>. El Anexo A
        /// preveía dos secciones y la arena: la tercera se añadió al alargar el nivel.
        /// </summary>
        public static readonly Tramo[] Secciones =
        {
            new Tramo("Sección 1", 0f, 25f, "Terreno abierto. Plataformas inestables. 3 Ecos de la Ira y 1 torreta de fuego fija."),
            new Tramo("Sección 2", 25f, 55f, "Plataformeo sobre lava y plataformas de combustión. 4 Ecos de la Ira y el Golem de Ceniza. Checkpoint en x = 43."),
            new Tramo("Sección 3", 55f, 100f, "La forja: una almena con 2 Ecos bajo el fuego de una torreta y un lago de 12 u con una inestable entre dos de combustión. 3 Ecos. Checkpoints en x = 57,5 y x = 94,5."),
            new Tramo("Arena del jefe", 100f, 125f, "Sala cerrada de 25 × 15 u con plataformas a 3 alturas. La lava brota en las erupciones; el suelo es firme."),
        };

        /// <summary>Lo que se pisa y su relleno (capa del suelo). Las cuencas son el fondo de la lava.</summary>
        public static readonly Bloque[] Suelos =
        {
            new Bloque("SueloInicio", -6f, 12f, Fondo, 0f),
            new Bloque("Escalon", 4f, 6f, 0f, 1f),
            new Bloque("FondoDelFoso", 12f, 17f, Fondo, -2f),
            new Bloque("Terraza", 17f, 27f, Fondo, 2f),
            new Bloque("CuencaDelLago", 27f, 36f, Fondo, 0f),
            new Bloque("Isla", 36f, 39f, Fondo, 2f),
            new Bloque("CuencaDeLaSubida", 39f, 42f, Fondo, 0f),
            new Bloque("Patio", 42f, 52f, Fondo, 5f),
            new Bloque("CuencaDeLaPuerta", 52f, 55f, Fondo, 3f),
            // La forja.
            new Bloque("Rellano", 55f, 60f, Fondo, 5f),
            new Bloque("CuencaDeLaEscalera", 60f, 64f, Fondo, 3f),
            new Bloque("Almena", 64f, 75f, Fondo, 7f),
            new Bloque("CuencaDeLaForja", 75f, 87f, Fondo, 5f),
            new Bloque("Cornisa", 87f, 92f, Fondo, 7f),
            new Bloque("Antesala", 92f, 97f, Fondo, 5f),
            new Bloque("CuencaDeLaArena", 97f, 100f, Fondo, 3f),
            new Bloque("SueloArena", 100f, 135f, Fondo, 5f),
        };

        /// <summary>Muros y techo (capa de las paredes).</summary>
        public static readonly Bloque[] Paredes =
        {
            new Bloque("ParedIzquierda", -7f, -6f, Fondo, 20f),
            new Bloque("ParedArena", 126f, 135f, 5f, Tope),
            new Bloque("TechoArena", 100f, 126f, 20f, Tope),
        };

        /// <summary>
        /// Las plataformas de la arena, a tres alturas (Anexo A). Son finas (vigas) y el jefe las
        /// atraviesa (<c>ArenaJefe.plataformas</c>): sirven para esquivar el salto y las ondas. Kael
        /// las atraviesa saltando desde abajo y las pisa al caer (<c>PlataformaDeUnSentido</c>).
        ///
        /// Las más bajas, a 3,5 u del suelo (v3): a 3 u, la cabeza de la Centinela (3,4 u) asomaba
        /// por encima y la espada de Kael, que baja hasta 0,25 u bajo sus pies, la alcanzaba desde
        /// arriba sin que ningún ataque de ella llegara allí.
        /// </summary>
        public static readonly Bloque[] PlataformasArena =
        {
            new Bloque("Plataforma8Izquierda", 104f, 108f, 8f, 8.5f),
            new Bloque("Plataforma8Derecha", 118f, 122f, 8f, 8.5f),
            new Bloque("Plataforma11", 110f, 115f, 10.5f, 11f),
            new Bloque("Plataforma14Izquierda", 106f, 109f, 13.5f, 14f),
            new Bloque("Plataforma14Derecha", 117f, 120f, 13.5f, 14f),
        };

        /// <summary>La lava, con la costra 2/3 de u por debajo de las orillas y su franja apoyada en la cuenca.</summary>
        public static readonly Lago[] Lavas =
        {
            new Lago("LavaDelLago", 27f, 36f, AltoFranjaLava),
            new Lago("LavaDeLaSubida", 39f, 42f, AltoFranjaLava),
            new Lago("LavaDeLaPuerta", 52f, 55f, 3f + AltoFranjaLava),
            new Lago("LavaDeLaEscalera", 60f, 64f, 3f + AltoFranjaLava),
            new Lago("LavaDeLaForja", 75f, 87f, 5f + AltoFranjaLava),
            new Lago("LavaDeLaArena", 97f, 100f, 3f + AltoFranjaLava),
        };

        /// <summary>
        /// Las mecánicas. Las dos de combustión van desfasadas medio ciclo (1,9 s de 3,8): cuando la
        /// primera empieza a calentarse, la segunda se apaga. Cruzar es esperar a ver las brasas.
        ///
        /// En la forja, lo mismo con una inestable en medio: sobre ella no se puede esperar (se
        /// rompe al segundo de pisarla), así que la espera se hace en la primera de combustión,
        /// mirando la segunda. Con medio ciclo de desfase, la segunda se apaga 0,9 s antes de que
        /// la primera empiece a quemar: ese es el momento de cruzar. La segunda torreta dispara a lo largo de la
        /// almena, hacia los dos Ecos: la tercera vez de cada peligro es junto a enemigos.
        /// </summary>
        public static readonly Pieza[] Mecanicas =
        {
            new Pieza("PlataformaInestableDelFoso", GeneradorMecanicas.PlataformaInestable, 14.5f, 0f),
            new Pieza("TorretaFuego", GeneradorMecanicas.TorretaFuego, 23.4f, 2f, miraIzquierda: true),
            new Pieza("PlataformaCombustion1", GeneradorMecanicas.PlataformaCombustion, 29.43f, 2f, desfase: 0f),
            new Pieza("PlataformaCombustion2", GeneradorMecanicas.PlataformaCombustion, 33.57f, 2f, desfase: 1.9f),
            new Pieza("PlataformaInestableDeLaSubida", GeneradorMecanicas.PlataformaInestable, 40.5f, 3.5f),
            // La forja.
            new Pieza("PlataformaInestableDeLaEscalera", GeneradorMecanicas.PlataformaInestable, 62f, 6f),
            new Pieza("TorretaFuegoDeLaAlmena", GeneradorMecanicas.TorretaFuego, 72.4f, 7f, miraIzquierda: true),
            new Pieza("PlataformaCombustion3", GeneradorMecanicas.PlataformaCombustion, 77.2f, 7f, desfase: 0f),
            new Pieza("PlataformaInestableDeLaForja", GeneradorMecanicas.PlataformaInestable, 81f, 7f),
            new Pieza("PlataformaCombustion4", GeneradorMecanicas.PlataformaCombustion, 84.8f, 7f, desfase: 1.9f),
        };

        public static readonly Guardia[] Enemigos =
        {
            new Guardia("EcoIra_1", false, 10f, 0f, 7.5f, 11.2f),
            new Guardia("EcoIra_2", false, 18.8f, 2f, 18f, 19.8f),
            new Guardia("EcoIra_3", false, 21f, 2f, 20.4f, 21.6f),
            new Guardia("EcoIra_4", false, 26f, 2f, 25.4f, 26.4f),
            new Guardia("EcoIra_5", false, 37.5f, 2f, 37f, 38f),
            new Guardia("EcoIra_6", false, 45.5f, 5f, 44.7f, 46.3f),
            new Guardia("GolemCeniza", true, 48.5f, 5f, 47.6f, 49.6f),
            new Guardia("EcoIra_7", false, 51f, 5f, 50.6f, 51.4f),
            // La forja: dos en la almena, a tiro de la torreta, y uno en la cornisa, tras el lago.
            new Guardia("EcoIra_8", false, 66.5f, 7f, 65.5f, 67.5f),
            new Guardia("EcoIra_9", false, 69.5f, 7f, 68.6f, 70.4f),
            new Guardia("EcoIra_10", false, 89.5f, 7f, 88.5f, 90.5f),
        };

        public const float Inicio = 2f;

        /// <summary>
        /// Los checkpoints (también en <c>LevelData.puntosDeControl</c>): tras la subida y antes del
        /// Golem; en el rellano, pasado el Golem; y en la antesala, ante la arena. Curan.
        /// </summary>
        public static readonly float[] Checkpoints = { 43f, 57.5f, 94.5f };

        public const float SueloArena = 5f;
        public const float TechoArena = 20f;

        /// <summary>La puerta que se cierra detrás del jugador, en el borde de la arena.</summary>
        public const float PuertaDesde = 100f;
        public const float PuertaHasta = 101f;

        /// <summary>El trigger de entrada: el jugador ya ha aterrizado y la puerta queda detrás.</summary>
        public const float Entrada = 103f;

        public const float Jefe = 115f;

        /// <summary>La puerta de salida, al fondo: mide 3,75 u y no se mete en la pared.</summary>
        public const float Salida = 124f;

        /// <summary>Por dentro de la puerta y de la pared de la derecha.</summary>
        public static Vector2 LimitesArena => new Vector2(PuertaHasta, Paredes[1].Desde);

        public static bool EsDe(LevelData nivel) => nivel != null && nivel.id == IdNivel;

        /// <summary>La altura del suelo en x (el bloque más alto que la cubre), sin las plataformas.</summary>
        public static float SueloEn(float x)
        {
            var bajo = Suelos.Where(b => x >= b.Desde && x < b.Hasta).ToList();
            return bajo.Count > 0 ? bajo.Max(b => b.Arriba) : Fondo;
        }

        /// <summary>
        /// Lo que pisa el jugador de izquierda a derecha, hasta el suelo de la arena: los suelos que
        /// no son cuencas y las plataformas de las mecánicas, con su ancho real.
        /// </summary>
        public static List<Pisada> Recorrido()
        {
            var pisadas = new List<Pisada>();
            foreach (var bloque in Suelos.Where(b => !b.Nombre.StartsWith("Cuenca") && b.Nombre != "FondoDelFoso" && b.Nombre != "Escalon"))
            {
                pisadas.Add(new Pisada(bloque.Nombre, bloque.Desde, bloque.Nombre == "SueloArena" ? LimitesArena.y : bloque.Hasta, bloque.Arriba));
            }

            foreach (var pieza in Mecanicas.Where(p => p.Prefab != GeneradorMecanicas.TorretaFuego))
            {
                var cuerpo = pieza.Prefab == GeneradorMecanicas.PlataformaCombustion
                    ? GeneradorMecanicas.Geometria.CuerpoCombustion
                    : GeneradorMecanicas.Geometria.CuerpoPlataforma;
                pisadas.Add(new Pisada(pieza.Nombre, pieza.X + cuerpo.xMin, pieza.X + cuerpo.xMax, pieza.Y));
            }

            return pisadas.OrderBy(p => p.Desde).ToList();
        }
    }
}
