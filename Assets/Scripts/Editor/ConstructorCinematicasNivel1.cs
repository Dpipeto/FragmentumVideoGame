using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Data;
using Fragmentum.Narrative;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Las cinemáticas del jefe del Nivel 1, con sus planos (<see cref="Carpeta"/>, 240 × 135,
    /// 16:9 exacto), en pixel art (<see cref="CutsceneData.escalaEntera"/>):
    /// - <b>Presentación</b> (<c>Cinematica_Jefe_Ira</c>, la «Cine01_EntradaJefe» del encargo): «El
    ///   que ya estaba aquí», 9,6 s, al entrar en la arena. Se escribe entera desde
    ///   <see cref="Presentacion"/>, que sigue el guion (<c>GUION-cinematica-jefe1.md</c>), como la
    ///   cinemática inicial. Al regenerar se conservan los clips de sonido y la fuente que se hayan
    ///   puesto a mano.
    /// - <b>Derrota</b> (<c>Cinematica_Derrota_Ira</c>, cuando la Centinela termina de caer):
    ///   <c>CinematicaFinalLvl1</c> (la sala vacía, la espada clavada, el fragmento frío) y, cuando
    ///   llegue, <c>CinematicaFinalLvl2</c> (los huecos de los fragmentos). Al terminar se vuelve
    ///   al juego, con el fragmento en la ceniza para recogerlo. Solo pone lo que falta: una imagen
    ///   ya asignada a mano se respeta. Si falta un PNG, su diapositiva no se crea y se avisa.
    /// </summary>
    public static class ConstructorCinematicasNivel1
    {
        public const string Carpeta = TablaArte.CarpetaCinematicasNivel1;

        public const string Jefe1 = "CinematicaJefe1";
        public const string Jefe2 = "CinematicaJefe2";
        public const string Final1 = "CinematicaFinalLvl1";
        public const string Final2 = "CinematicaFinalLvl2";

        public const string RutaPresentacion = GeneradorNarrativa.Carpeta + "/Cinematica_Jefe_Ira.asset";
        public const string RutaDerrota = GeneradorNarrativa.Carpeta + "/Cinematica_Derrota_Ira.asset";

        /// <summary>Lo que dura cada plano del final.</summary>
        public const float DuracionPlanoFinal = 4.5f;

        // --- La presentación: «El que ya estaba aquí» ---

        /// <summary>Los cues del guion que suenan dentro de la cinemática. El rugido va con el del jefe, ya en juego.</summary>
        public static readonly string[] CuesPresentacion =
        {
            "drone_sala", "amb_forja", "golpe_escala", "respiracion_fuelle", "voz_kael_01", "brasa_aviva", "rotulo_metal",
        };

        /// <summary>El texto de Kael: abajo a la izquierda, alineado bajo su silueta (columnas 22-25, pies en la fila 109).</summary>
        public static readonly Vector2Int PuntoTextoKael = new Vector2Int(22, 113);

        /// <summary>
        /// El rótulo, centrado bajo el ojo con aire de sobra, en píxeles de su imagen: el ojo ocupa
        /// las columnas 73-155 y llega a la fila 67. Se mueve con el encuadre del ojo, así que con
        /// (0, 0) también queda debajo.
        /// </summary>
        public static readonly Vector2Int PuntoRotulo = new Vector2Int(114, 77);

        public static readonly Vector2Int PuntoSubtitulo = new Vector2Int(114, 90);

        /// <summary>
        /// El encuadre del ojo: 17 px a la derecha y 19 arriba, para que caiga donde estaba la
        /// cabeza de la Centinela en el plano anterior (raccord del guion). Se puede comparar con
        /// (0, 0) en el asset.
        /// </summary>
        public static readonly Vector2Int EncuadreOjo = new Vector2Int(17, 19);

        /// <summary>
        /// Escribe el guion en la presentación: cuatro beats (negro 0,5 · la sala 1,0 + 4,9 · negro
        /// 0,5 · el ojo 1,1 + 1,6, con 2 fotogramas de flash), sus textos, sus temblores, las brasas
        /// y sus cues. Los clips de los cues y la fuente que ya tuviera se conservan.
        /// </summary>
        public static void Presentacion(CutsceneData cinematica)
        {
            var clips = cinematica.diapositivas
                .Where(d => d != null && d.cues != null)
                .SelectMany(d => d.cues)
                .Where(c => c != null && c.clip != null && !string.IsNullOrEmpty(c.nombre))
                .GroupBy(c => c.nombre)
                .ToDictionary(g => g.Key, g => g.First().clip);
            CueAudio Cue(string nombre, float instante, bool bucle = false, float hasta = 0f) => new CueAudio
            {
                nombre = nombre,
                clip = clips.TryGetValue(nombre, out var clip) ? clip : null,
                instante = instante,
                bucle = bucle,
                hasta = hasta,
            };

            cinematica.id = "Jefe_Ira";
            cinematica.montaje = MONTAJES_CINEMATICA.BEATS;
            cinematica.escalaEntera = true;
            cinematica.sobreElJuego = false;
            cinematica.saltable = true;
            cinematica.soloPrimeraVez = true;
            cinematica.bloqueoSalto = 0.5f;
            cinematica.avisoSaltar = string.Empty;
            cinematica.fundidoTexto = 0.4f;
            cinematica.tamanoFuente = 5;
            cinematica.temblorPixeles = 1;
            cinematica.temblorFotogramas = 4;
            cinematica.musica = MusicaNiveles.DeCinematica(cinematica.id);
            cinematica.diapositivas = new List<Diapositiva>
            {
                // 0,0 – 0,5: negro. Queda solo el zumbido grave, que sube hasta el final.
                new Diapositiva
                {
                    duracion = 0.5f,
                    cues = { Cue("drone_sala", 0f, bucle: true, hasta: 9.6f) },
                },

                // 0,5 – 6,4: la sala. Sube en 1 s, brasas, dos temblores de un píxel, el texto de
                // Kael de 4,0 a 6,4 (se va en 0,4 s) y corte seco a negro.
                new Diapositiva
                {
                    imagen = Imagen(Jefe1),
                    fundidoEntrada = 1f,
                    duracion = 4.9f,
                    fundidoSalida = 0f,
                    brasas = true,
                    temblores = { 1.9f, 3.1f },
                    carteles =
                    {
                        new Cartel
                        {
                            texto = "No recuerdo esta sala.\nEl calor, sí.",
                            inicio = 3.5f,
                            duracion = 2.4f,
                            enPunto = true,
                            punto = PuntoTextoKael,
                            alineacion = TextAnchor.UpperLeft,
                        },
                    },
                    cues =
                    {
                        Cue("amb_forja", 0f, bucle: true, hasta: 5.9f),
                        Cue("golpe_escala", 1.1f),
                        Cue("respiracion_fuelle", 1.5f, bucle: true, hasta: 5.5f),
                        Cue("voz_kael_01", 3.5f),
                    },
                },

                // 6,4 – 6,9: negro, medio segundo de nada.
                new Diapositiva { duracion = 0.5f },

                // 6,9 – 9,6: el ojo sube desde negro en 1,1 s, encuadrado donde estaba la cabeza; el
                // rótulo entra en seco a los 8,0 con el golpe metálico; flash de 2 fotogramas.
                new Diapositiva
                {
                    imagen = Imagen(Jefe2),
                    fundidoEntrada = 1.1f,
                    duracion = 1.6f,
                    fundidoSalida = 0f,
                    desplazamiento = EncuadreOjo,
                    flashFinal = 2,
                    carteles =
                    {
                        new Cartel
                        {
                            texto = "CENTINELA ARDIENTE",
                            inicio = 1.1f,
                            duracion = 1.6f,
                            titulo = true,
                            sinFundido = true,
                            enPunto = true,
                            punto = PuntoRotulo,
                            alineacion = TextAnchor.UpperCenter,
                        },
                        new Cartel
                        {
                            texto = "Arquitecto de la Ira",
                            inicio = 1.1f,
                            duracion = 1.6f,
                            sinFundido = true,
                            enPunto = true,
                            punto = PuntoSubtitulo,
                            alineacion = TextAnchor.UpperCenter,
                        },
                    },
                    cues =
                    {
                        Cue("brasa_aviva", 0f, hasta: 1.1f),
                        Cue("rotulo_metal", 1.1f),
                    },
                },
            };
        }

        public static string RutaImagen(string nombre) => $"{Carpeta}/{nombre}.png";

        public static Sprite Imagen(string nombre) => AssetDatabase.LoadAssetAtPath<Sprite>(RutaImagen(nombre));

        /// <summary>Pone los planos en las dos cinemáticas. Devuelve false solo si algo falló.</summary>
        public static bool Construir()
        {
            var faltan = new[] { Jefe1, Jefe2, Final1, Final2 }.Where(n => !File.Exists(RutaImagen(n))).ToList();
            if (faltan.Count > 0)
            {
                Debug.LogWarning($"[Fragmentum] Faltan planos del jefe del Nivel 1: {string.Join(", ", faltan)}. " +
                                 "Sus diapositivas se crearán al dejarlos en " + Carpeta + " y regenerar.");
            }

            var presentacion = AssetDatabase.LoadAssetAtPath<CutsceneData>(RutaPresentacion);
            if (presentacion == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {RutaPresentacion} (la genera GeneradorNarrativa): la presentación queda sin su guion.");
            }
            else
            {
                Presentacion(presentacion);
                EditorUtility.SetDirty(presentacion);
            }

            var derrota = Derrota();
            var nivel = NivelDe(presentacion);
            if (nivel != null && nivel.cinematicaJefeDerrotado == null && derrota != null)
            {
                nivel.cinematicaJefeDerrotado = derrota;
                EditorUtility.SetDirty(nivel);
            }

            AssetDatabase.SaveAssets();
            return true;
        }

        // La de la derrota: la crea si falta y le añade los planos que tenga.
        private static CutsceneData Derrota()
        {
            var cinematica = AssetDatabase.LoadAssetAtPath<CutsceneData>(RutaDerrota);
            var nueva = cinematica == null;
            if (nueva)
            {
                cinematica = ScriptableObject.CreateInstance<CutsceneData>();
                cinematica.id = "Derrota_Ira";
                cinematica.saltable = true;
                cinematica.soloPrimeraVez = false;
                cinematica.sobreElJuego = false;
                cinematica.escalaEntera = true;
                cinematica.bloqueoSalto = 1f;
                cinematica.finAviso = 4f;
                cinematica.diapositivas = new List<Diapositiva>();
            }

            var cambia = false;

            // Su música (Musica/Cinematicas), si la tiene.
            var musica = MusicaNiveles.DeCinematica(cinematica.Id);
            if (cinematica.musica != musica)
            {
                cinematica.musica = musica;
                cambia = true;
            }
            var planos = new[] { Imagen(Final1), Imagen(Final2) }.Where(p => p != null).ToList();
            foreach (var plano in planos)
            {
                if (cinematica.diapositivas.Any(d => d != null && d.imagen == plano))
                {
                    continue;
                }

                var hueco = cinematica.diapositivas.FirstOrDefault(d => d != null && d.imagen == null);
                if (hueco != null)
                {
                    hueco.imagen = plano;
                }
                else
                {
                    cinematica.diapositivas.Add(new Diapositiva { imagen = plano, duracion = DuracionPlanoFinal });
                }

                cambia = true;
            }

            if (nueva)
            {
                if (cinematica.diapositivas.Count == 0)
                {
                    return null;
                }

                AssetDatabase.CreateAsset(cinematica, RutaDerrota);
            }
            else if (cambia)
            {
                EditorUtility.SetDirty(cinematica);
            }

            return cinematica;
        }

        // El nivel cuya presentación de jefe es esta (el Nivel 1).
        private static LevelData NivelDe(CutsceneData presentacion)
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            if (bd == null || presentacion == null)
            {
                return null;
            }

            return bd.niveles.FirstOrDefault(n => n != null && n.cinematicaJefe == presentacion);
        }
    }
}
