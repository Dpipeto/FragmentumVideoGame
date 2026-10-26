using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.Narrative;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Las cinemáticas de la Doncella del Lamento, con sus cuatro planos (<see cref="Carpeta"/>,
    /// 480 × 268: PENDIENTES N34), en pixel art y en su propia rejilla (<see cref="Rejilla"/>, el
    /// doble de fina que la del Nivel 1):
    /// - <b>Presentación</b> (<c>Cinematica_Jefe_Tristeza</c>), al entrar en la arena: la nave
    ///   helada, con ella subiendo por la grieta, y después su rostro. Dice su línea del guion
    ///   (<c>Docs/Diseno/Guion.md</c>, «Antes del jefe») repartida entre los dos planos, y al final
    ///   entra su rótulo, con el nombre del mundo debajo. Para esta no hay un guion plano a plano
    ///   como el de la Centinela: lleva su misma estructura (negro, plano, negro, plano con rótulo)
    ///   y casi su misma duración (9,8 s: las presentaciones de jefe no pasan de 10), sin brasas,
    ///   temblores ni flash. Se escribe entera cada vez que se regenera.
    /// - <b>Derrota</b> (<c>Cinematica_Derrota_Tristeza</c>), cuando termina de derretirse: ella
    ///   deshecha sobre el hielo y la ciudad, fuera del agua. Sin texto, como la de la Ira. Al
    ///   terminar se vuelve al juego, con el fragmento en el charco para recogerlo. Solo pone lo
    ///   que falta: un plano ya asignado a mano se respeta.
    ///
    /// Sin sonido: en el proyecto todavía no hay audio. La música va en
    /// <see cref="CutsceneData.musica"/> y los efectos, en los cues de cada plano.
    /// Si falta algún plano, no se toca nada y se avisa: la presentación sigue siendo la genérica.
    /// </summary>
    public static class ConstructorCinematicasNivel2
    {
        public const string Carpeta = TablaArte.CarpetaSprites + "/Cinematicas/Nivel2";

        public const string Presentacion1 = "PresentacionDoncella1";
        public const string Presentacion2 = "PresentacionDoncella2";
        public const string Derrota1 = "DerrotaDoncella1";
        public const string Derrota2 = "DerrotaDoncella2";

        public static readonly string[] Planos = { Presentacion1, Presentacion2, Derrota1, Derrota2 };

        public const string RutaPresentacion = GeneradorNarrativa.Carpeta + "/Cinematica_Jefe_Tristeza.asset";
        public const string RutaDerrota = GeneradorNarrativa.Carpeta + "/Cinematica_Derrota_Tristeza.asset";

        /// <summary>El tamaño de los planos: la rejilla en la que se dibujan.</summary>
        public static readonly Vector2Int Rejilla = new Vector2Int(480, 268);

        /// <summary>
        /// Alto de la letra en píxeles del plano. En 1920 × 1080 la rejilla va a ×4: 40 px, lo mismo
        /// que mide la de la presentación de la Centinela (5 px a ×8).
        /// </summary>
        public const int TamanoFuente = 10;

        /// <summary>Lo que dura cada plano de la derrota: lo mismo que los de la Ira.</summary>
        public const float DuracionPlanoFinal = ConstructorCinematicasNivel1.DuracionPlanoFinal;

        /// <summary>
        /// El rótulo, centrado bajo su rostro y por debajo de la gema del pecho (que llega a la fila
        /// 215), en píxeles de su imagen. El título mide el doble que la letra: 20 px.
        /// </summary>
        public static readonly Vector2Int PuntoRotulo = new Vector2Int(240, 222);

        public static readonly Vector2Int PuntoSubtitulo = new Vector2Int(240, 246);

        /// <summary>
        /// Lo que dice, centrado sobre el hielo del suelo, por debajo de la grieta (que llega a la
        /// fila 226) y de la gema.
        /// </summary>
        public static readonly Vector2Int PuntoTexto = new Vector2Int(240, 240);

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(Carpeta + "/", UI.CamaraCinematica.PixelesPorUnidad, null,
                "Planos de la Doncella: 480×268 (N34), a 1 píxel por unidad como la cámara de cinemáticas, que los dibuja " +
                "en su propia rejilla.", Rejilla),
        };

        public static string RutaImagen(string nombre) => $"{Carpeta}/{nombre}.png";

        public static Sprite Imagen(string nombre) => AssetDatabase.LoadAssetAtPath<Sprite>(RutaImagen(nombre));

        /// <summary>¿Están los cuatro planos en el proyecto?</summary>
        public static bool HayPlanos => Planos.All(p => File.Exists(RutaImagen(p)));

        /// <summary>
        /// La línea de la Doncella, en dos tiempos: lo que dice sobre la nave y la pregunta que hace
        /// de cerca. Se parte por la última frase; si solo tiene una, va entera en el segundo plano.
        /// </summary>
        public static (string primero, string segundo) Partir(string linea)
        {
            var frases = Regex.Split((linea ?? string.Empty).Trim(), @"(?<=[.!?…])\s+").Where(f => f.Length > 0).ToArray();
            return frases.Length < 2
                ? (string.Empty, frases.FirstOrDefault() ?? string.Empty)
                : (string.Join(" ", frases.Take(frases.Length - 1)), frases[frases.Length - 1]);
        }

        /// <summary>
        /// Escribe la presentación: cuatro beats (negro 0,3 · la nave 0,7 + 3,8 + 0,3 · negro 0,2 ·
        /// su rostro 0,6 + 3,6 + 0,3), 9,8 s. La primera parte de su línea, sobre la nave, 3,6 s
        /// (son 56 letras); la pregunta, sobre su rostro, 1,8 s; y el rótulo, a los 2,5 s de ese
        /// plano, hasta que empieza a irse.
        /// </summary>
        public static void Presentacion(CutsceneData cinematica, string jefe, string mundo, string linea)
        {
            var (primero, segundo) = Partir(linea);
            var nave = new Diapositiva
            {
                imagen = Imagen(Presentacion1),
                fundidoEntrada = 0.7f,
                duracion = 3.8f,
                fundidoSalida = 0.3f,
            };
            if (primero.Length > 0)
            {
                nave.carteles.Add(Dicho(primero, 0.9f, 3.6f));
            }

            var rostro = new Diapositiva
            {
                imagen = Imagen(Presentacion2),
                fundidoEntrada = 0.6f,
                duracion = 3.6f,
                fundidoSalida = 0.3f,
                carteles =
                {
                    Dicho(segundo, 0.5f, 1.8f),
                    new Cartel
                    {
                        texto = jefe.ToUpperInvariant(),
                        inicio = 2.5f,
                        duracion = 1.7f,
                        titulo = true,
                        enPunto = true,
                        punto = PuntoRotulo,
                        alineacion = TextAnchor.UpperCenter,
                    },
                    new Cartel
                    {
                        texto = mundo,
                        inicio = 2.5f,
                        duracion = 1.7f,
                        enPunto = true,
                        punto = PuntoSubtitulo,
                        alineacion = TextAnchor.UpperCenter,
                    },
                },
            };

            cinematica.id = "Jefe_Tristeza";
            cinematica.montaje = MONTAJES_CINEMATICA.BEATS;
            cinematica.escalaEntera = true;
            cinematica.rejilla = Rejilla;
            cinematica.sobreElJuego = false;
            cinematica.saltable = true;
            cinematica.soloPrimeraVez = true;
            cinematica.llevaDialogoDelJefe = true;
            cinematica.bloqueoSalto = 0.5f;
            cinematica.avisoSaltar = string.Empty;
            cinematica.fundidoTexto = 0.4f;
            cinematica.tamanoFuente = TamanoFuente;
            cinematica.musica = MusicaNiveles.DeCinematica(cinematica.id);
            cinematica.diapositivas = new List<Diapositiva>
            {
                new Diapositiva { duracion = 0.3f },
                nave,
                new Diapositiva { duracion = 0.2f },
                rostro,
            };
        }

        private static Cartel Dicho(string texto, float inicio, float duracion) => new Cartel
        {
            texto = texto,
            inicio = inicio,
            duracion = duracion,
            enPunto = true,
            punto = PuntoTexto,
            alineacion = TextAnchor.UpperCenter,
        };

        /// <summary>Monta las dos cinemáticas y las pone en el Nivel 2. Devuelve false solo si algo falló.</summary>
        public static bool Construir()
        {
            if (!HayPlanos)
            {
                var faltan = Planos.Where(p => !File.Exists(RutaImagen(p)));
                Debug.LogWarning($"[Fragmentum] Faltan planos de la Doncella ({string.Join(", ", faltan)}) en {Carpeta}: " +
                                 "su presentación sigue siendo la genérica y no hay cinemática de derrota.");
                return true;
            }

            if (Planos.Any(p => Imagen(p) == null))
            {
                Debug.LogError($"[Fragmentum] Los planos de {Carpeta} no están importados como sprites.");
                return false;
            }

            var nivel = Nivel();
            var presentacion = AssetDatabase.LoadAssetAtPath<CutsceneData>(RutaPresentacion);
            if (nivel == null || nivel.jefe == null || presentacion == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta el Nivel 2, su jefe o {RutaPresentacion} (la genera GeneradorNarrativa): " +
                                 "las cinemáticas de la Doncella no se montan.");
                return true;
            }

            var guion = GuionMundos.De(EMOCIONES.TRISTEZA);
            Presentacion(presentacion, nivel.jefe.nombreCanonico, nivel.nombreMundo, guion != null ? guion.AntesDelJefe : string.Empty);
            EditorUtility.SetDirty(presentacion);

            var derrota = Derrota();
            var cambia = false;
            if (nivel.cinematicaJefe == null)
            {
                nivel.cinematicaJefe = presentacion;
                cambia = true;
            }

            if (nivel.cinematicaJefeDerrotado == null && derrota != null)
            {
                nivel.cinematicaJefeDerrotado = derrota;
                cambia = true;
            }

            if (cambia)
            {
                EditorUtility.SetDirty(nivel);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Fragmentum] Cinemáticas de la Doncella: {RutaPresentacion} ({presentacion.DuracionTotal:0.0} s) y {RutaDerrota}.");
            return true;
        }

        // La de la derrota: la crea si falta y le añade los planos que no tenga.
        private static CutsceneData Derrota()
        {
            var cinematica = AssetDatabase.LoadAssetAtPath<CutsceneData>(RutaDerrota);
            var nueva = cinematica == null;
            if (nueva)
            {
                cinematica = ScriptableObject.CreateInstance<CutsceneData>();
                cinematica.id = "Derrota_Tristeza";
                cinematica.saltable = true;
                cinematica.soloPrimeraVez = false;
                cinematica.sobreElJuego = false;
                cinematica.escalaEntera = true;
                cinematica.bloqueoSalto = 1f;
                cinematica.finAviso = 4f;
                cinematica.diapositivas = new List<Diapositiva>();
            }

            var cambia = cinematica.rejilla != Rejilla;
            cinematica.rejilla = Rejilla;
            foreach (var plano in new[] { Imagen(Derrota1), Imagen(Derrota2) })
            {
                if (cinematica.diapositivas.Any(d => d != null && d.imagen == plano))
                {
                    continue;
                }

                cinematica.diapositivas.Add(new Diapositiva { imagen = plano, duracion = DuracionPlanoFinal });
                cambia = true;
            }

            // Su música (Musica/Cinematicas), si la tiene.
            var musica = MusicaNiveles.DeCinematica(cinematica.Id);
            if (cinematica.musica != musica)
            {
                cinematica.musica = musica;
                cambia = true;
            }

            if (nueva)
            {
                AssetDatabase.CreateAsset(cinematica, RutaDerrota);
            }
            else if (cambia)
            {
                EditorUtility.SetDirty(cinematica);
            }

            return cinematica;
        }

        /// <summary>El Nivel 2, o null si no está en la base de datos.</summary>
        public static LevelData Nivel()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            return bd != null ? bd.niveles.FirstOrDefault(DisenoNivel2.EsDe) : null;
        }
    }
}
