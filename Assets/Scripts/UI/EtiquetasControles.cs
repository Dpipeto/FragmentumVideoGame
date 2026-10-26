using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;

namespace Fragmentum.UI
{
    /// <summary>
    /// Escribe las teclas de una acción tal como están en el asset de input, para que la guía de
    /// mecánicas no se quede atrás si cambian. Solo teclado y ratón, como la ayuda de la pausa.
    ///
    /// Las plantillas llevan las acciones entre llaves: «Mantén {Attack}» → «Mantén CLIC / ENTER».
    /// Con una barra se eligen las partes de un compuesto, y sus teclas se emparejan por orden:
    /// {Move/left,right} → «A D / ← →».
    /// </summary>
    public static class EtiquetasControles
    {
        public const string MapaJugador = "Player";

        /// <summary>Lo que se escribe si la acción no existe o no tiene teclas.</summary>
        public const string SinTecla = "?";

        private static readonly Regex Marcador = new Regex(@"\{(\w+)(?:/([\w,]+))?\}");

        private static readonly Dictionary<string, string> Nombres = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "space", "ESPACIO" },
            { "leftShift", "SHIFT" }, { "rightShift", "SHIFT" }, { "shift", "SHIFT" },
            { "leftCtrl", "CTRL" }, { "rightCtrl", "CTRL" }, { "ctrl", "CTRL" },
            { "leftAlt", "ALT" }, { "rightAlt", "ALT" }, { "alt", "ALT" },
            { "enter", "ENTER" }, { "numpadEnter", "ENTER" },
            { "escape", "ESC" }, { "tab", "TAB" }, { "backspace", "RETROCESO" },
            { "upArrow", "↑" }, { "downArrow", "↓" }, { "leftArrow", "←" }, { "rightArrow", "→" },
            { "leftButton", "CLIC" }, { "rightButton", "CLIC DERECHO" }, { "middleButton", "RUEDA" },
        };

        /// <summary>Sustituye cada {Accion} de la plantilla por sus teclas.</summary>
        public static string Resolver(string plantilla, InputActionAsset acciones, string mapa = MapaJugador)
        {
            if (string.IsNullOrEmpty(plantilla))
            {
                return string.Empty;
            }

            var mapaAcciones = acciones != null ? acciones.FindActionMap(mapa, false) : null;
            return Marcador.Replace(plantilla, m =>
            {
                var accion = mapaAcciones != null ? mapaAcciones.FindAction(m.Groups[1].Value, false) : null;
                var teclas = accion != null ? DeAccion(accion, Partes(m)) : string.Empty;
                return teclas.Length > 0 ? teclas : SinTecla;
            });
        }

        /// <summary>Acciones que nombra la plantilla, en orden (para comprobar que existen).</summary>
        public static List<string> AccionesDe(string plantilla)
        {
            var lista = new List<string>();
            if (!string.IsNullOrEmpty(plantilla))
            {
                foreach (Match m in Marcador.Matches(plantilla))
                {
                    lista.Add(m.Groups[1].Value);
                }
            }

            return lista;
        }

        /// <summary>
        /// Teclas de una acción separadas por « / ». Un compuesto escribe juntas las teclas de sus
        /// partes con el mismo orden: las primeras de cada parte, luego las segundas…
        /// </summary>
        public static string DeAccion(InputAction accion, ICollection<string> partes = null)
        {
            var salida = new List<string>();
            List<List<string>> compuesto = null;
            Dictionary<string, List<string>> porParte = null;

            foreach (var binding in accion.bindings)
            {
                if (binding.isPartOfComposite)
                {
                    if (porParte == null || (partes != null && !Contiene(partes, binding.name)) || !EsTecladoORaton(binding.effectivePath))
                    {
                        continue;
                    }

                    if (!porParte.TryGetValue(binding.name, out var teclas))
                    {
                        teclas = new List<string>();
                        porParte.Add(binding.name, teclas);
                        compuesto.Add(teclas);
                    }

                    teclas.Add(Etiqueta(binding.effectivePath));
                    continue;
                }

                Volcar(compuesto, salida);
                compuesto = null;
                porParte = null;

                if (binding.isComposite)
                {
                    compuesto = new List<List<string>>();
                    porParte = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                }
                else if (EsTecladoORaton(binding.effectivePath))
                {
                    Anadir(salida, Etiqueta(binding.effectivePath));
                }
            }

            Volcar(compuesto, salida);
            return string.Join(" / ", salida);
        }

        /// <summary>«&lt;Keyboard&gt;/space» → «ESPACIO».</summary>
        public static string Etiqueta(string ruta)
        {
            if (string.IsNullOrEmpty(ruta))
            {
                return string.Empty;
            }

            var control = ruta.Substring(ruta.LastIndexOf('/') + 1);
            if (Nombres.TryGetValue(control, out var nombre))
            {
                return nombre;
            }

            return control.Length <= 2
                ? control.ToUpperInvariant()
                : InputControlPath.ToHumanReadableString(ruta, InputControlPath.HumanReadableStringOptions.OmitDevice).ToUpperInvariant();
        }

        private static bool EsTecladoORaton(string ruta) =>
            !string.IsNullOrEmpty(ruta) &&
            (ruta.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase) || ruta.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase));

        private static string[] Partes(Match m) =>
            m.Groups[2].Success ? m.Groups[2].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) : null;

        private static bool Contiene(ICollection<string> partes, string parte)
        {
            foreach (var p in partes)
            {
                if (string.Equals(p, parte, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // Empareja las teclas de las partes: [A, ←] y [D, →] → «A D», «← →».
        private static void Volcar(List<List<string>> compuesto, List<string> salida)
        {
            if (compuesto == null || compuesto.Count == 0)
            {
                return;
            }

            var filas = 0;
            foreach (var teclas in compuesto)
            {
                filas = Math.Max(filas, teclas.Count);
            }

            for (var i = 0; i < filas; i++)
            {
                var juntas = new List<string>();
                foreach (var teclas in compuesto)
                {
                    if (i < teclas.Count)
                    {
                        juntas.Add(teclas[i]);
                    }
                }

                Anadir(salida, string.Join(" ", juntas));
            }
        }

        private static void Anadir(List<string> salida, string etiqueta)
        {
            if (!string.IsNullOrEmpty(etiqueta) && !salida.Contains(etiqueta))
            {
                salida.Add(etiqueta);
            }
        }
    }
}
