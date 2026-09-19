using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Fragmentum.Core.FSM;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Genera los diagramas de estados en Mermaid **a partir del código**: lee la tabla de
    /// transiciones de cada FSM marcada con <see cref="DeclaraFSMAttribute"/> y escribe un
    /// archivo por máquina en <c>Docs/FSM/</c>.
    ///
    /// Es lo que hace comprobable el criterio de aceptación «cada diagrama corresponde
    /// exactamente al código»: el diagrama no se dibuja a mano, se exporta.
    /// </summary>
    public static class ExportadorDiagramasFSM
    {
        private const string CarpetaSalida = "Docs/FSM";

        [MenuItem("Fragmentum/Exportar diagramas FSM", priority = 20)]
        public static void Exportar()
        {
            var raiz = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var carpeta = Path.Combine(raiz, CarpetaSalida);
            Directory.CreateDirectory(carpeta);

            var exportadas = 0;
            foreach (var (nombre, tabla) in BuscarTablas())
            {
                var texto = GenerarDocumento(nombre, tabla);
                File.WriteAllText(Path.Combine(carpeta, $"{nombre}.md"), texto, new UTF8Encoding(false));
                exportadas++;
            }

            if (exportadas == 0)
            {
                Debug.LogWarning(
                    "[Fragmentum] No se encontró ninguna FSM que exportar. Marca el controlador con " +
                    "[DeclaraFSM(\"Nombre\")] y dale un método estático público CrearTabla().");
                return;
            }

            Debug.Log($"[Fragmentum] {exportadas} diagrama(s) exportado(s) a {CarpetaSalida}/.");
        }

        /// <summary>Busca por reflexión todas las FSM declaradas en el proyecto.</summary>
        public static IEnumerable<(string nombre, ITablaLegible tabla)> BuscarTablas()
        {
            foreach (var ensamblado in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] tipos;
                try
                {
                    tipos = ensamblado.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    tipos = e.Types;
                }

                foreach (var tipo in tipos)
                {
                    if (tipo == null)
                    {
                        continue;
                    }

                    var atributo = tipo.GetCustomAttribute<DeclaraFSMAttribute>();
                    if (atributo == null)
                    {
                        continue;
                    }

                    var metodo = tipo.GetMethod(atributo.Metodo,
                        BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);

                    if (metodo == null)
                    {
                        Debug.LogWarning(
                            $"[Fragmentum] {tipo.Name} está marcada con [DeclaraFSM] pero no tiene el método " +
                            $"estático público {atributo.Metodo}().");
                        continue;
                    }

                    if (metodo.Invoke(null, null) is ITablaLegible tabla)
                    {
                        yield return (atributo.Nombre, tabla);
                    }
                }
            }
        }

        /// <summary>Documento Markdown completo de una FSM, con su diagrama y su tabla.</summary>
        public static string GenerarDocumento(string nombre, ITablaLegible tabla)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# FSM: {nombre}");
            sb.AppendLine();
            sb.AppendLine("> Generado automáticamente desde la tabla de transiciones del código");
            sb.AppendLine("> (menú *Fragmentum → Exportar diagramas FSM*). No editar a mano.");
            sb.AppendLine();
            sb.AppendLine("```mermaid");
            sb.Append(GenerarMermaid(tabla));
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("| Desde | Hacia | Condición |");
            sb.AppendLine("|---|---|---|");
            foreach (var (desde, hacia, condicion, _) in tabla.TransicionesLegibles)
            {
                sb.AppendLine($"| {desde} | {hacia} | {(string.IsNullOrEmpty(condicion) ? "—" : condicion)} |");
            }

            return sb.ToString();
        }

        /// <summary>Solo el bloque Mermaid (stateDiagram-v2) de una tabla.</summary>
        public static string GenerarMermaid(ITablaLegible tabla)
        {
            var grupoDeEstado = new Dictionary<string, string>();
            var estadosPorGrupo = new Dictionary<string, List<string>>();

            foreach (var (grupo, estados) in tabla.GruposLegibles)
            {
                var lista = new List<string>();
                foreach (var estado in estados)
                {
                    if (grupoDeEstado.ContainsKey(estado))
                    {
                        continue; // un estado se dibuja dentro de un solo grupo
                    }

                    grupoDeEstado[estado] = grupo;
                    lista.Add(estado);
                }

                estadosPorGrupo[grupo] = lista;
            }

            var internas = new Dictionary<string, List<string>>();
            var externas = new List<string>();
            var yaEmitidas = new HashSet<string>();

            foreach (var (desde, hacia, condicion, grupoOrigen) in tabla.TransicionesLegibles)
            {
                var etiqueta = string.IsNullOrEmpty(condicion) ? string.Empty : $" : {Limpiar(condicion)}";

                if (!string.IsNullOrEmpty(grupoOrigen))
                {
                    // Transición declarada desde un superestado: se dibuja desde el grupo.
                    var linea = $"    {grupoOrigen} --> {hacia}{etiqueta}";
                    if (yaEmitidas.Add(linea))
                    {
                        externas.Add(linea);
                    }

                    continue;
                }

                grupoDeEstado.TryGetValue(desde, out var grupoDesde);
                grupoDeEstado.TryGetValue(hacia, out var grupoHacia);

                if (!string.IsNullOrEmpty(grupoDesde) && grupoDesde == grupoHacia)
                {
                    if (!internas.TryGetValue(grupoDesde, out var lista))
                    {
                        lista = new List<string>();
                        internas[grupoDesde] = lista;
                    }

                    lista.Add($"        {desde} --> {hacia}{etiqueta}");
                }
                else
                {
                    externas.Add($"    {desde} --> {hacia}{etiqueta}");
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("stateDiagram-v2");

            var inicial = tabla.EstadoInicial;
            if (!string.IsNullOrEmpty(inicial))
            {
                grupoDeEstado.TryGetValue(inicial, out var grupoInicial);
                sb.AppendLine($"    [*] --> {(string.IsNullOrEmpty(grupoInicial) ? inicial : grupoInicial)}");
            }

            foreach (var par in estadosPorGrupo)
            {
                sb.AppendLine($"    state {par.Key} {{");

                if (!string.IsNullOrEmpty(inicial) && par.Value.Contains(inicial))
                {
                    sb.AppendLine($"        [*] --> {inicial}");
                }

                foreach (var estado in par.Value)
                {
                    sb.AppendLine($"        {estado}");
                }

                if (internas.TryGetValue(par.Key, out var lineas))
                {
                    foreach (var linea in lineas)
                    {
                        sb.AppendLine(linea);
                    }
                }

                sb.AppendLine("    }");
            }

            foreach (var linea in externas)
            {
                sb.AppendLine(linea);
            }

            return sb.ToString();
        }

        /// <summary>Quita de las etiquetas los caracteres que rompen el diagrama.</summary>
        private static string Limpiar(string texto) =>
            texto.Replace("\n", " ").Replace(":", ",").Replace("|", "/").Trim();
    }
}
