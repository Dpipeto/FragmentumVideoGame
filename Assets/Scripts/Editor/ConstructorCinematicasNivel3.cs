using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.Narrative;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Las cinemáticas del Espectro del Pavor (fase F del Mundo 3), con sus cuatro planos
    /// (<see cref="Carpeta"/>, 480 × 268, en la rejilla de los de la Doncella):
    /// - <b>Presentación</b> (<c>Cinematica_Jefe_Miedo</c>), al cruzar la verja: el claro a oscuras,
    ///   con Kael en el círculo de su luz y las grietas de una máscara mirándolo desde lejos; y
    ///   después el jefe de cerca, medio deshecho en la oscuridad. Dice su línea del guion
    ///   (<c>Docs/Diseno/Guion.md</c>, «Antes del jefe») repartida entre los dos planos, y al final
    ///   entra su rótulo con el nombre del mundo debajo: la estructura y la duración de la de la
    ///   Doncella (9,8 s). De cerca, el jefe ocupa el centro y su espada la derecha: lo que dice y su
    ///   rótulo van arriba a la izquierda, sobre el negro, uno detrás de otro. Se escribe entera cada
    ///   vez que se regenera.
    /// - <b>Derrota</b> (<c>Cinematica_Derrota_Miedo</c>), cuando termina de caer: se deshace en una
    ///   tormenta de hojas y el bosque se ve con luz por primera vez. Sin texto, como las otras dos.
    ///   Al terminar se vuelve al juego, con el fragmento sobre su montón de hojas. Solo pone lo que
    ///   falta: un plano ya asignado a mano se respeta.
    ///
    /// Sin sonido: en el proyecto todavía no hay audio. Si falta algún plano, no se toca nada y se
    /// avisa: la presentación sigue siendo la genérica.
    /// </summary>
    public static class ConstructorCinematicasNivel3
    {
        public const string Carpeta = TablaArte.CarpetaSprites + "/Cinematicas/Nivel3";

        public const string Presentacion1 = "PresentacionEspectro1";
        public const string Presentacion2 = "PresentacionEspectro2";
        public const string Derrota1 = "DerrotaEspectro1";
        public const string Derrota2 = "DerrotaEspectro2";

        public static readonly string[] Planos = { Presentacion1, Presentacion2, Derrota1, Derrota2 };

        public const string RutaPresentacion = GeneradorNarrativa.Carpeta + "/Cinematica_Jefe_Miedo.asset";
        public const string RutaDerrota = GeneradorNarrativa.Carpeta + "/Cinematica_Derrota_Miedo.asset";

        /// <summary>La rejilla de los planos: la de los de la Doncella.</summary>
        public static readonly Vector2Int Rejilla = ConstructorCinematicasNivel2.Rejilla;

        /// <summary>La letra: la de la Doncella (10 px del plano, 40 en 1920 × 1080).</summary>
        public const int TamanoFuente = ConstructorCinematicasNivel2.TamanoFuente;

        /// <summary>Lo que dura cada plano de la derrota: lo mismo que los de la Ira y la Tristeza.</summary>
        public const float DuracionPlanoFinal = ConstructorCinematicasNivel2.DuracionPlanoFinal;

        /// <summary>
        /// Lo que dice sobre el claro, centrado, en el negro que hay entre la máscara (llega a la
        /// fila 140) y la luz de Kael (empieza en la 184), en píxeles de su imagen.
        /// </summary>
        public static readonly Vector2Int PuntoClaro = new Vector2Int(240, 160);

        /// <summary>
        /// De cerca: arriba a la izquierda, donde todo es negro hasta la columna 240 y la fila 26. Ahí
        /// va lo que dice y, cuando se ha ido, su rótulo (el doble de alto) y el nombre del mundo.
        /// </summary>
        public static readonly Vector2Int PuntoCerca = new Vector2Int(120, 10);

        public static readonly Vector2Int PuntoRotulo = new Vector2Int(120, 6);

        public static readonly Vector2Int PuntoSubtitulo = new Vector2Int(120, 30);

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(Carpeta + "/", UI.CamaraCinematica.PixelesPorUnidad, null,
                "Planos del Mundo 3 (el Espectro del Pavor y la salida): 480×268, como los de la Doncella, a 1 píxel por " +
                "unidad como la cámara de cinemáticas, que los dibuja en su propia rejilla.", Rejilla),
        };

        public static string RutaImagen(string nombre) => $"{Carpeta}/{nombre}.png";

        public static Sprite Imagen(string nombre) => AssetDatabase.LoadAssetAtPath<Sprite>(RutaImagen(nombre));

        /// <summary>¿Están los cuatro planos en el proyecto?</summary>
        public static bool HayPlanos => Planos.All(p => File.Exists(RutaImagen(p)));

        /// <summary>
        /// Escribe la presentación: cuatro beats (negro 0,3 · el claro 0,7 + 3,8 + 0,3 · negro 0,2 ·
        /// de cerca 0,6 + 3,6 + 0,3), 9,8 s. La primera parte de su línea, sobre el claro, 3,6 s; la
        /// segunda, de cerca, 2,1 s (son 34 letras: 16 por segundo); y el rótulo, a los 2,7 s de ese
        /// plano, hasta que empieza a irse.
        /// </summary>
        public static void Presentacion(CutsceneData cinematica, string jefe, string mundo, string linea)
        {
            var (primero, segundo) = ConstructorCinematicasNivel2.Partir(linea);
            var claro = new Diapositiva
            {
                imagen = Imagen(Presentacion1),
                fundidoEntrada = 0.7f,
                duracion = 3.8f,
                fundidoSalida = 0.3f,
            };
            if (primero.Length > 0)
            {
                claro.carteles.Add(Dicho(primero, 0.9f, 3.6f, PuntoClaro));
            }

            var cerca = new Diapositiva
            {
                imagen = Imagen(Presentacion2),
                fundidoEntrada = 0.6f,
                duracion = 3.6f,
                fundidoSalida = 0.3f,
                carteles =
                {
                    Dicho(segundo, 0.5f, 2.1f, PuntoCerca),
                    new Cartel
                    {
                        texto = jefe.ToUpperInvariant(),
                        inicio = 2.7f,
                        duracion = 1.5f,
                        titulo = true,
                        enPunto = true,
                        punto = PuntoRotulo,
                        alineacion = TextAnchor.UpperCenter,
                    },
                    Dicho(mundo, 2.7f, 1.5f, PuntoSubtitulo),
                },
            };

            cinematica.id = "Jefe_Miedo";
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
                claro,
                new Diapositiva { duracion = 0.2f },
                cerca,
            };
        }

        private static Cartel Dicho(string texto, float inicio, float duracion, Vector2Int punto) => new Cartel
        {
            texto = texto,
            inicio = inicio,
            duracion = duracion,
            enPunto = true,
            punto = punto,
            alineacion = TextAnchor.UpperCenter,
        };

        /// <summary>Monta las dos cinemáticas y las pone en el Nivel 3. Devuelve false solo si algo falló.</summary>
        public static bool Construir()
        {
            if (!HayPlanos)
            {
                var faltan = Planos.Where(p => !File.Exists(RutaImagen(p)));
                Debug.LogWarning($"[Fragmentum] Faltan planos del Espectro del Pavor ({string.Join(", ", faltan)}) en {Carpeta}: " +
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
                Debug.LogWarning($"[Fragmentum] Falta el Nivel 3, su jefe o {RutaPresentacion} (la genera GeneradorNarrativa): " +
                                 "las cinemáticas del Espectro del Pavor no se montan.");
                return true;
            }

            var guion = GuionMundos.De(EMOCIONES.MIEDO);
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
            Debug.Log($"[Fragmentum] Cinemáticas del Espectro del Pavor: {RutaPresentacion} ({presentacion.DuracionTotal:0.0} s) y {RutaDerrota}.");
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
                cinematica.id = "Derrota_Miedo";
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

        /// <summary>El Nivel 3, o null si no está en la base de datos.</summary>
        public static LevelData Nivel()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            return bd != null ? bd.niveles.FirstOrDefault(DisenoNivel3.EsDe) : null;
        }
    }
}
