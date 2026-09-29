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
                // Saltos LF siempre, para que el archivo no cambie según el sistema operativo.
                var texto = GenerarDocumento(nombre, tabla).Replace("\r\n", "\n");
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

        /// <summary>
        /// Busca todas las FSM declaradas en el proyecto con el índice de tipos del Editor
        /// (<see cref="TypeCache"/>), ordenadas por nombre para que la exportación sea estable.
        /// </summary>
        public static IEnumerable<(string nombre, ITablaLegible tabla)> BuscarTablas()
        {
            var tipos = new List<Type>(TypeCache.GetTypesWithAttribute<DeclaraFSMAttribute>());
            tipos.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));

            foreach (var tipo in tipos)
            {
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

        /// <summary>
        /// Solo el bloque Mermaid (stateDiagram-v2) de una tabla.
        ///
        /// Los grupos se dibujan como estados compuestos y se <b>anidan</b> cuando un grupo
        /// contiene todos los estados de otro (LOCOMOCION dentro de VIVO en el jugador): así el
        /// diagrama muestra la máquina jerárquica tal como está declarada. Cada estado se
        /// dibuja dentro del grupo más pequeño que lo contiene, y cada transición dentro del
        /// bloque más interno que contiene sus dos extremos.
        /// </summary>
        public static string GenerarMermaid(ITablaLegible tabla)
        {
            // 1. Grupos, en orden de declaración.
            var nombres = new List<string>();
            var miembros = new Dictionary<string, List<string>>();
            foreach (var (grupo, estados) in tabla.GruposLegibles)
            {
                if (miembros.ContainsKey(grupo))
                {
                    continue;
                }

                nombres.Add(grupo);
                miembros[grupo] = new List<string>(estados);
            }

            // 2. Grupo padre de cada grupo: el más pequeño que lo contiene estrictamente.
            var padreDeGrupo = new Dictionary<string, string>();
            foreach (var hijo in nombres)
            {
                string mejor = null;
                foreach (var candidato in nombres)
                {
                    if (candidato == hijo || miembros[candidato].Count <= miembros[hijo].Count)
                    {
                        continue;
                    }

                    if (ContieneTodos(miembros[candidato], miembros[hijo]) &&
                        (mejor == null || miembros[candidato].Count < miembros[mejor].Count))
                    {
                        mejor = candidato;
                    }
                }

                padreDeGrupo[hijo] = mejor;
            }

            // 3. Grupo dueño de cada estado: el más pequeño que lo contiene (empate: el primero).
            var duenoDeEstado = new Dictionary<string, string>();
            var estadosDeGrupo = new Dictionary<string, List<string>>();
            foreach (var grupo in nombres)
            {
                estadosDeGrupo[grupo] = new List<string>();
            }

            foreach (var grupo in nombres)
            {
                foreach (var estado in miembros[grupo])
                {
                    if (duenoDeEstado.TryGetValue(estado, out var actual) &&
                        miembros[actual].Count <= miembros[grupo].Count)
                    {
                        continue;
                    }

                    if (actual != null)
                    {
                        estadosDeGrupo[actual].Remove(estado);
                    }

                    duenoDeEstado[estado] = grupo;
                    estadosDeGrupo[grupo].Add(estado);
                }
            }

            // Cadena de contenedores de un nodo (estado o grupo), del más interno al más externo.
            List<string> Contenedores(string nodo, bool esGrupo)
            {
                var cadena = new List<string>();
                var actual = esGrupo
                    ? (padreDeGrupo.TryGetValue(nodo, out var p) ? p : null)
                    : (duenoDeEstado.TryGetValue(nodo, out var d) ? d : null);

                while (actual != null)
                {
                    cadena.Add(actual);
                    actual = padreDeGrupo.TryGetValue(actual, out var siguiente) ? siguiente : null;
                }

                return cadena;
            }

            // 4. Cada transición va al bloque más interno que contiene a sus dos extremos.
            //    Las declaradas desde un superestado se dibujan una sola vez, desde el grupo.
            const string raiz = "";
            var lineasPorBloque = new Dictionary<string, List<string>> { [raiz] = new List<string>() };
            var yaEmitidas = new HashSet<string>();

            foreach (var (desde, hacia, condicion, grupoOrigen) in tabla.TransicionesLegibles)
            {
                var etiqueta = string.IsNullOrEmpty(condicion) ? string.Empty : $" : {Limpiar(condicion)}";
                var esDeGrupo = !string.IsNullOrEmpty(grupoOrigen);
                var origen = esDeGrupo ? grupoOrigen : desde;
                var linea = $"{origen} --> {hacia}{etiqueta}";

                if (!yaEmitidas.Add(linea))
                {
                    continue;
                }

                var destinos = Contenedores(hacia, false);
                var bloque = raiz;
                foreach (var contenedor in Contenedores(origen, esDeGrupo))
                {
                    if (destinos.Contains(contenedor))
                    {
                        bloque = contenedor;
                        break;
                    }
                }

                if (!lineasPorBloque.TryGetValue(bloque, out var lista))
                {
                    lista = new List<string>();
                    lineasPorBloque[bloque] = lista;
                }

                lista.Add(linea);
            }

            // 5. Escritura recursiva: estado inicial, grupos hijos, estados propios y transiciones.
            var inicial = tabla.EstadoInicial;
            var caminoInicial = string.IsNullOrEmpty(inicial) ? new List<string>() : Contenedores(inicial, false);
            caminoInicial.Reverse(); // del grupo más externo al más interno

            string SiguienteHaciaInicial(string bloque)
            {
                var indice = bloque == raiz ? 0 : caminoInicial.IndexOf(bloque) + 1;
                if (bloque != raiz && indice == 0)
                {
                    return null; // el estado inicial no está dentro de este bloque
                }

                return indice < caminoInicial.Count ? caminoInicial[indice] : inicial;
            }

            var sb = new StringBuilder();
            sb.AppendLine("stateDiagram-v2");

            void EscribirBloque(string bloque, int nivel)
            {
                var sangria = new string(' ', 4 * (nivel + 1));

                if (!string.IsNullOrEmpty(inicial))
                {
                    var siguiente = SiguienteHaciaInicial(bloque);
                    if (siguiente != null)
                    {
                        sb.AppendLine($"{sangria}[*] --> {siguiente}");
                    }
                }

                foreach (var grupo in nombres)
                {
                    var padre = padreDeGrupo[grupo] ?? raiz;
                    if (padre != bloque)
                    {
                        continue;
                    }

                    sb.AppendLine($"{sangria}state {grupo} {{");
                    EscribirBloque(grupo, nivel + 1);
                    sb.AppendLine($"{sangria}}}");
                }

                if (bloque != raiz)
                {
                    foreach (var estado in estadosDeGrupo[bloque])
                    {
                        sb.AppendLine($"{sangria}{estado}");
                    }
                }

                if (lineasPorBloque.TryGetValue(bloque, out var lineas))
                {
                    foreach (var linea in lineas)
                    {
                        sb.AppendLine($"{sangria}{linea}");
                    }
                }
            }

            EscribirBloque(raiz, 0);
            return sb.ToString();
        }

        private static bool ContieneTodos(List<string> contenedor, List<string> contenido)
        {
            foreach (var elemento in contenido)
            {
                if (!contenedor.Contains(elemento))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Quita de las etiquetas los caracteres que rompen el diagrama.</summary>
        private static string Limpiar(string texto) =>
            texto.Replace("\n", " ").Replace(":", ",").Replace("|", "/").Trim();
    }
}
