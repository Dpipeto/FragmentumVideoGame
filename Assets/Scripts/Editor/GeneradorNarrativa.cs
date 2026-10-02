using System;
using System.Collections.Generic;
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
    /// Los textos son <b>placeholders evidentes</b>: el guion y las imágenes son del diseño
    /// (PENDIENTES B1 y B2). Cada línea dice que está pendiente y nombra la emoción del mundo,
    /// para que el sistema se vea funcionar sin inventar la historia. Solo se usan textos del
    /// Anexo A donde el Anexo los da (el cierre del Jardín, la entrada al final y la pregunta de
    /// los créditos). La excepción es la cinemática inicial, que ya tiene guion e imágenes
    /// (<see cref="ConstructorCinematicaInicial"/>).
    ///
    /// Solo crea lo que falta y solo enlaza los campos vacíos: lo que se escriba a mano no se pisa.
    /// </summary>
    public static class GeneradorNarrativa
    {
        public const string Carpeta = "Assets/Data/Narrativa";
        public const string RutaConfiguracionRegistro = Carpeta + "/RegistroEmocional.asset";

        [MenuItem("Fragmentum/Narrativa/Generar cinemáticas, diálogos y registro", priority = 150)]
        public static void MenuConstruir() => Construir();

        [MenuItem("Fragmentum/Narrativa/Restablecer los placeholders de narrativa", priority = 151)]
        public static void MenuRestablecer()
        {
            if (EditorUtility.DisplayDialog("Fragmentum", "Se sobrescribirán las cinemáticas y los diálogos generados con los placeholders. Los textos escritos a mano en ellos se perderán. ¿Continuar?", "Restablecer", "Cancelar"))
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

                var emocion = nivel.emocion.nombreCanonico;
                var clave = Clave(emocion);
                CutsceneData cierre;
                CutsceneData entrada = null;

                switch (nivel.emocion.id)
                {
                    case EMOCIONES.ESPERANZA:
                        // Anexo A: cinemática de cierre de ~30 s, no interactiva.
                        cierre = Cinematica("Cinematica_Cierre_Jardin", "Cierre_Jardin", sobrescribir, false,
                            "[CIERRE DEL JARDÍN ETÉREO · pendiente de guion e imágenes, ~30 s]",
                            "Kael ve al Soldado Original a lo lejos por primera vez.");
                        break;
                    case EMOCIONES.INTEGRACION:
                        // Anexo A: entrada de ~45 s y cierre con la pregunta final, sin respuesta.
                        entrada = Cinematica("Cinematica_Entrada_Final", "Entrada_Final", sobrescribir, false,
                            "[ENTRADA AL ESPEJO DEL ORIGEN · pendiente de guion e imágenes, ~45 s]",
                            "El Soldado Original confronta a Kael y lo llama un error.",
                            "Kael responderá con sus actos en el combate, no con palabras.");
                        cierre = Cinematica("Cinematica_Final", "Final", sobrescribir, false,
                            "El Soldado Original se fragmenta en luz. Kael absorbe los fragmentos.",
                            "El mundo se reconstruye.",
                            "¿Eres un error… o una nueva identidad?");
                        break;
                    default:
                        cierre = Cinematica($"Cinematica_Cierre_{clave}", $"Cierre_{clave}", sobrescribir, false,
                            $"[CIERRE DEL MUNDO DE LA {emocion.ToUpperInvariant()} · pendiente de guion e imágenes (PENDIENTES B1)]",
                            $"Kael integra el {(nivel.fragmento != null ? nivel.fragmento.nombre : "fragmento del mundo")}.");
                        break;
                }

                var dialogos = Dialogos(nivel, clave, sobrescribir);
                // El jefe final ya se presenta en la cinemática de entrada del nivel.
                var presentacion = nivel.jefe != null && nivel.emocion.id != EMOCIONES.INTEGRACION ? CinematicaJefe(nivel, clave, sobrescribir) : null;

                if (nivel.id == bd.PrimerId)
                {
                    entrada = inicial;
                }

                Enlazar(nivel, entrada, cierre, dialogos, presentacion);
            }

            AssetDatabase.SaveAssets();
        }

        // --- Cinemáticas ---

        private static CutsceneData Cinematica(string asset, string id, bool sobrescribir, bool soloPrimeraVez, params string[] textos)
        {
            return Crear<CutsceneData>(asset, sobrescribir, c =>
            {
                c.id = id;
                c.saltable = true;
                c.soloPrimeraVez = soloPrimeraVez;
                c.diapositivas = new List<Diapositiva>();
                foreach (var texto in textos)
                {
                    c.diapositivas.Add(new Diapositiva { texto = texto, duracion = 4f });
                }
            });
        }

        /// <summary>
        /// Presentación del jefe al entrar en su arena (~7 s), sobre la escena congelada: su
        /// nombre como título y el diálogo antes del combate. Texto pendiente de guion; las
        /// imágenes, si se dibujan, van en el campo «imagen» de cada diapositiva.
        /// </summary>
        private static CutsceneData CinematicaJefe(LevelData nivel, string clave, bool sobrescribir)
        {
            var jefe = nivel.jefe.nombreCanonico;
            var emocion = nivel.emocion.nombreCanonico;
            return Crear<CutsceneData>($"Cinematica_Jefe_{clave}", sobrescribir, c =>
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
                c.diapositivas = new List<Diapositiva>
                {
                    new Diapositiva
                    {
                        duracion = 3f,
                        carteles = new List<Cartel>
                        {
                            new Cartel { texto = jefe.ToUpperInvariant(), inicio = 0.2f, duracion = 2.8f, posicion = POSICIONES_CARTEL.CENTRO, titulo = true },
                            new Cartel { texto = $"[Presentación · {emocion} · pendiente de guion]", inicio = 0.6f, duracion = 2.4f, posicion = POSICIONES_CARTEL.ABAJO },
                        },
                    },
                    new Diapositiva
                    {
                        duracion = 4.5f,
                        texto = $"{jefe}: [Antes del combate · {emocion} · pendiente de guion] {jefe} encarna {ConArticulo(nivel)}.",
                    },
                };
            });
        }

        // --- Diálogos ---

        private static DialogueData Dialogos(LevelData nivel, string clave, bool sobrescribir)
        {
            var emocion = nivel.emocion.nombreCanonico;
            var jefe = nivel.jefe != null ? nivel.jefe.nombreCanonico : null;

            return Crear<DialogueData>($"Dialogos_{clave}", sobrescribir, d =>
            {
                d.lineas = new List<LineaDialogo>
                {
                    Linea("Kael", MOMENTOS_DIALOGO.APERTURA, $"[Apertura · {emocion} · pendiente de guion] Kael nombra {ConArticulo(nivel)} al entrar en {nivel.nombreMundo}."),
                    Linea("Kael", MOMENTOS_DIALOGO.ECO_MEMORIA, $"[Eco de memoria · {emocion} · pendiente de guion]"),
                    Linea("Kael", MOMENTOS_DIALOGO.CIERRE, $"[Cierre · {emocion} · pendiente de guion] Kael reconoce {ConArticulo(nivel)} como parte de sí."),
                };

                if (jefe != null)
                {
                    d.lineas.Insert(2, Linea(jefe, MOMENTOS_DIALOGO.PRE_JEFE, $"[Antes del jefe · {emocion} · pendiente de guion] {jefe} encarna {ConArticulo(nivel)}."));
                }
            });
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

        private static T Crear<T>(string asset, bool sobrescribir, Action<T> rellenar) where T : ScriptableObject
        {
            var ruta = $"{Carpeta}/{asset}.asset";
            var existente = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (existente == null)
            {
                existente = ScriptableObject.CreateInstance<T>();
                rellenar(existente);
                AssetDatabase.CreateAsset(existente, ruta);
            }
            else if (sobrescribir)
            {
                rellenar(existente);
                EditorUtility.SetDirty(existente);
            }

            return existente;
        }

        /// <summary>«la ira», «el miedo»…: la emoción con su artículo.</summary>
        private static string ConArticulo(LevelData nivel) =>
            (nivel.emocion.id == EMOCIONES.MIEDO ? "el " : "la ") + nivel.emocion.nombreCanonico.ToLowerInvariant();

        private static string Clave(string emocion) =>
            emocion.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace(" ", string.Empty);
    }
}
