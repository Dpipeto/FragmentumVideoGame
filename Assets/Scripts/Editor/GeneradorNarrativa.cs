using System;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Data;
using Fragmentum.Narrative;
using UnityEditor;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Fase 8: crea las cinemáticas, los diálogos de cada mundo y la configuración del registro
    /// emocional, y los enlaza a los <c>LevelData</c>.
    ///
    /// Los textos son los del guion (<see cref="GuionMundos"/>, <c>Docs/Diseno/Guion.md</c>). Antes
    /// del guion el generador dejaba placeholders que decían «pendiente de guion»: donde todavía
    /// queden, los sustituye por el guion. Lo escrito a mano no se pisa: un texto sin esa marca, o
    /// una cinemática con imágenes, se queda como está. La cinemática inicial tiene su propio guion
    /// e imágenes (<see cref="ConstructorCinematicaInicial"/>).
    ///
    /// Solo crea lo que falta y solo enlaza los campos vacíos.
    /// </summary>
    public static class GeneradorNarrativa
    {
        public const string Carpeta = "Assets/Data/Narrativa";
        public const string RutaConfiguracionRegistro = Carpeta + "/RegistroEmocional.asset";

        /// <summary>La marca de los textos provisionales de antes del guion.</summary>
        public const string MarcaProvisional = "pendiente de guion";

        // Los textos de la cinemática final de antes del guion (del Anexo A, sin la marca).
        private static readonly string[] FinalAnterior =
        {
            "El Soldado Original se fragmenta en luz. Kael absorbe los fragmentos.",
            "El mundo se reconstruye.",
            GuionMundos.PreguntaFinal,
        };

        [MenuItem("Fragmentum/Narrativa/Generar cinemáticas, diálogos y registro", priority = 150)]
        public static void MenuConstruir() => Construir();

        [MenuItem("Fragmentum/Narrativa/Restablecer el guion en cinemáticas y diálogos", priority = 151)]
        public static void MenuRestablecer()
        {
            if (EditorUtility.DisplayDialog("Fragmentum", "Se sobrescribirán las cinemáticas y los diálogos generados con el guion. Los textos escritos a mano en ellos se perderán. ¿Continuar?", "Restablecer", "Cancelar"))
            {
                GenerarDatos(true);
            }
        }

        public static bool Construir()
        {
            GenerarDatos(false);
            return true;
        }

        public static void GenerarDatos(bool sobrescribir)
        {
            AsegurarCarpeta(Carpeta);
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            if (bd == null)
            {
                Debug.LogWarning("[Fragmentum] No hay LevelDatabase: no se generan los datos de narrativa.");
                return;
            }

            ConfiguracionDelRegistro();

            // La inicial ya tiene guion e imágenes: se crea (o restablece) con ellos.
            var inicial = Crear<CutsceneData>("Cinematica_Inicial", sobrescribir, ConstructorCinematicaInicial.Rellenar);

            foreach (var nivel in bd.niveles)
            {
                if (nivel == null || nivel.emocion == null)
                {
                    continue;
                }

                var guion = GuionMundos.De(nivel.emocion.id);
                var clave = Clave(nivel.emocion.nombreCanonico);
                CutsceneData cierre;
                CutsceneData entrada = null;

                switch (nivel.emocion.id)
                {
                    case EMOCIONES.ESPERANZA:
                        // Anexo A: cinemática de cierre de ~30 s, no interactiva.
                        cierre = Cinematica("Cinematica_Cierre_Jardin", "Cierre_Jardin", sobrescribir, guion.DuracionCierre, guion.CinematicaCierre);
                        break;
                    case EMOCIONES.INTEGRACION:
                        // Anexo A: entrada de ~45 s y cierre con la pregunta final, sin respuesta.
                        entrada = Cinematica("Cinematica_Entrada_Final", "Entrada_Final", sobrescribir, GuionMundos.DuracionEntradaFinal, GuionMundos.EntradaFinal);
                        cierre = Cinematica("Cinematica_Final", "Final", sobrescribir, GuionMundos.DuracionFinal, GuionMundos.Final,
                            c => c.diapositivas.Count == FinalAnterior.Length && c.diapositivas.Select(d => d.texto).SequenceEqual(FinalAnterior),
                            c => c.diapositivas.Last().duracion = GuionMundos.DuracionPregunta);
                        break;
                    default:
                        cierre = Cinematica($"Cinematica_Cierre_{clave}", $"Cierre_{clave}", sobrescribir, guion.DuracionCierre, guion.CinematicaCierre);
                        break;
                }

                var dialogos = Dialogos(nivel, guion, clave, sobrescribir);
                // El jefe final ya se presenta en la cinemática de entrada del nivel.
                var presentacion = nivel.jefe != null && nivel.emocion.id != EMOCIONES.INTEGRACION ? CinematicaJefe(nivel, guion, clave, sobrescribir) : null;

                if (nivel.id == bd.PrimerId)
                {
                    entrada = inicial;
                }

                Enlazar(nivel, entrada, cierre, dialogos, presentacion);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>¿Es un texto provisional de antes del guion?</summary>
        public static bool EsProvisional(string texto) => texto != null && texto.Contains(MarcaProvisional);

        // --- Cinemáticas ---

        /// <summary>
        /// Una cinemática de textos. Se rellena al crearla, al restablecer, o si todavía es la
        /// provisional: sin imágenes y con algún texto marcado (o lo que diga <paramref name="anterior"/>).
        /// <paramref name="ajustar"/> retoca lo rellenado (la pregunta final dura más).
        /// </summary>
        private static CutsceneData Cinematica(string asset, string id, bool sobrescribir, float duracion, string[] textos,
            Func<CutsceneData, bool> anterior = null, Action<CutsceneData> ajustar = null)
        {
            return Crear<CutsceneData>(asset, sobrescribir, c =>
            {
                c.id = id;
                c.saltable = true;
                c.soloPrimeraVez = false;
                c.diapositivas = new List<Diapositiva>();
                foreach (var texto in textos)
                {
                    c.diapositivas.Add(new Diapositiva { texto = texto, duracion = duracion });
                }

                ajustar?.Invoke(c);
            }, c => c.diapositivas != null && c.diapositivas.All(d => d.imagen == null) &&
                    (c.diapositivas.Any(d => EsProvisional(d.texto)) || (anterior != null && anterior(c))));
        }

        /// <summary>
        /// Presentación del jefe al entrar en su arena (~7 s), sobre la escena congelada: su
        /// nombre como título, su mundo debajo y su línea antes del combate. Las imágenes, si se
        /// dibujan, van en el campo «imagen» de cada diapositiva. (La de la Centinela la monta
        /// <see cref="ConstructorCinematicasNivel1"/> con sus planos.)
        /// </summary>
        private static CutsceneData CinematicaJefe(LevelData nivel, GuionMundos.Mundo guion, string clave, bool sobrescribir)
        {
            var jefe = nivel.jefe.nombreCanonico;
            var presentacion = Crear<CutsceneData>($"Cinematica_Jefe_{clave}", sobrescribir, c =>
            {
                c.id = $"Jefe_{clave}";
                c.saltable = true;
                c.soloPrimeraVez = false;
                c.sobreElJuego = true;
                c.opacidadVelo = 0.55f;
                // Corta y dentro del juego: el bloqueo solo evita saltarla con la tecla con la que
                // se iba corriendo (mantener una tecla no la salta; hay que volver a pulsar).
                c.bloqueoSalto = 1f;
                c.finAviso = 4f;
                c.fundidoImagen = 0.4f;
                c.llevaDialogoDelJefe = true;
                c.diapositivas = new List<Diapositiva>
                {
                    new Diapositiva
                    {
                        duracion = 3f,
                        carteles = new List<Cartel>
                        {
                            new Cartel { texto = jefe.ToUpperInvariant(), inicio = 0.2f, duracion = 2.8f, posicion = POSICIONES_CARTEL.CENTRO, titulo = true },
                            new Cartel { texto = nivel.nombreMundo, inicio = 0.6f, duracion = 2.4f, posicion = POSICIONES_CARTEL.ABAJO },
                        },
                    },
                    new Diapositiva { duracion = 4.5f, texto = $"{jefe}: {guion.AntesDelJefe}" },
                };
            });

            // Antes del guion: solo se cambian los textos marcados, lo demás se respeta.
            var cambio = false;
            foreach (var diapositiva in presentacion.diapositivas)
            {
                if (EsProvisional(diapositiva.texto))
                {
                    diapositiva.texto = $"{jefe}: {guion.AntesDelJefe}";
                    cambio = true;
                }

                foreach (var cartel in (diapositiva.carteles ?? new List<Cartel>()).Where(c => EsProvisional(c.texto)))
                {
                    cartel.texto = nivel.nombreMundo;
                    cambio = true;
                }
            }

            // Dice la línea del jefe: el HUD no la repite al empezar el combate.
            if (!presentacion.llevaDialogoDelJefe && presentacion.diapositivas.Any(d => d.texto == $"{jefe}: {guion.AntesDelJefe}"))
            {
                presentacion.llevaDialogoDelJefe = true;
                cambio = true;
            }

            if (cambio)
            {
                EditorUtility.SetDirty(presentacion);
            }

            return presentacion;
        }

        // --- Diálogos ---

        private static DialogueData Dialogos(LevelData nivel, GuionMundos.Mundo guion, string clave, bool sobrescribir)
        {
            var dialogos = Crear<DialogueData>($"Dialogos_{clave}", sobrescribir, d => d.lineas = LineasDelGuion(nivel, guion));

            // Antes del guion: cada momento que solo tiene líneas marcadas pasa a la del guion; un
            // momento con alguna línea escrita a mano se queda como está.
            var lineas = dialogos.lineas ?? new List<LineaDialogo>();

            // Un momento que el guion ganó después de crearse el asset (la pista): se añade su línea.
            var faltan = LineasDelGuion(nivel, guion).Where(d => !lineas.Any(l => l != null && l.momento == d.momento)).ToList();
            if (faltan.Count > 0)
            {
                lineas.AddRange(faltan);
                dialogos.lineas = lineas;
                EditorUtility.SetDirty(dialogos);
            }

            if (!lineas.Any(l => l != null && EsProvisional(l.texto)))
            {
                return dialogos;
            }

            var nuevas = new List<LineaDialogo>();
            foreach (var deseada in LineasDelGuion(nivel, guion))
            {
                var propias = lineas.Where(l => l != null && l.momento == deseada.momento).ToList();
                nuevas.AddRange(propias.Count > 0 && propias.Any(l => !EsProvisional(l.texto)) ? propias : new List<LineaDialogo> { deseada });
            }

            // Y las escritas a mano de un momento que el guion no tiene.
            nuevas.AddRange(lineas.Where(l => l != null && !EsProvisional(l.texto) && !nuevas.Contains(l)));
            dialogos.lineas = nuevas;
            EditorUtility.SetDirty(dialogos);
            return dialogos;
        }

        /// <summary>Las líneas del guion de un mundo, en orden: apertura, eco de memoria, la pista, antes del jefe y cierre.</summary>
        private static List<LineaDialogo> LineasDelGuion(LevelData nivel, GuionMundos.Mundo guion)
        {
            var lineas = new List<LineaDialogo> { Linea("Kael", MOMENTOS_DIALOGO.APERTURA, guion.Apertura) };
            if (!string.IsNullOrEmpty(guion.EcoMemoria))
            {
                lineas.Add(Linea("Kael", MOMENTOS_DIALOGO.ECO_MEMORIA, guion.EcoMemoria));
            }

            if (!string.IsNullOrEmpty(guion.Pista))
            {
                lineas.Add(Linea("Kael", MOMENTOS_DIALOGO.PISTA, guion.Pista));
            }

            if (nivel.jefe != null && !string.IsNullOrEmpty(guion.AntesDelJefe))
            {
                lineas.Add(Linea(nivel.jefe.nombreCanonico, MOMENTOS_DIALOGO.PRE_JEFE, guion.AntesDelJefe));
            }

            lineas.Add(Linea("Kael", MOMENTOS_DIALOGO.CIERRE, guion.Cierre));
            return lineas;
        }

        private static LineaDialogo Linea(string hablante, MOMENTOS_DIALOGO momento, string texto) =>
            new LineaDialogo { hablante = hablante, momento = momento, texto = texto, duracion = 4.5f };

        // --- Registro emocional ---

        private static void ConfiguracionDelRegistro()
        {
            if (AssetDatabase.LoadAssetAtPath<ConfiguracionRegistro>(RutaConfiguracionRegistro) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ConfiguracionRegistro>(), RutaConfiguracionRegistro);
            }
        }

        public static ConfiguracionRegistro CargarConfiguracionRegistro() =>
            AssetDatabase.LoadAssetAtPath<ConfiguracionRegistro>(RutaConfiguracionRegistro);

        // --- Utilidades ---

        // Enlaza en el LevelData solo los campos vacíos: lo que se haya asignado a mano se respeta.
        private static void Enlazar(LevelData nivel, CutsceneData entrada, CutsceneData cierre, DialogueData dialogos, CutsceneData presentacion)
        {
            var cambio = false;
            if (nivel.cinematicaEntrada == null && entrada != null)
            {
                nivel.cinematicaEntrada = entrada;
                cambio = true;
            }

            if (nivel.cinematicaCierre == null && cierre != null)
            {
                nivel.cinematicaCierre = cierre;
                cambio = true;
            }

            if (nivel.dialogos == null && dialogos != null)
            {
                nivel.dialogos = dialogos;
                cambio = true;
            }

            if (nivel.cinematicaJefe == null && presentacion != null)
            {
                nivel.cinematicaJefe = presentacion;
                cambio = true;
            }

            if (cambio)
            {
                EditorUtility.SetDirty(nivel);
            }
        }

        /// <summary>
        /// Crea el asset si falta. Si existe, lo rellena otra vez al restablecer o, con
        /// <paramref name="esProvisional"/>, si todavía es el provisional de antes del guion.
        /// </summary>
        private static T Crear<T>(string asset, bool sobrescribir, Action<T> rellenar, Func<T, bool> esProvisional = null) where T : ScriptableObject
        {
            var ruta = $"{Carpeta}/{asset}.asset";
            var existente = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (existente == null)
            {
                existente = ScriptableObject.CreateInstance<T>();
                rellenar(existente);
                AssetDatabase.CreateAsset(existente, ruta);
            }
            else if (sobrescribir || (esProvisional != null && esProvisional(existente)))
            {
                rellenar(existente);
                EditorUtility.SetDirty(existente);
            }

            return existente;
        }

        private static string Clave(string emocion) =>
            emocion.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace(" ", string.Empty);
    }
}
