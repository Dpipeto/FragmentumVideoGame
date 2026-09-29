using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Fragmentum.Data;
using UnityEngine;

namespace Fragmentum.Narrative
{
    /// <summary>Una respuesta del registro emocional al terminar un nivel.</summary>
    [Serializable]
    public class EntradaRegistro
    {
        /// <summary>Fecha y hora local en formato ISO 8601 (2026-09-29T18:04:11).</summary>
        public string fecha;

        public string participante;
        public int nivel;
        public string mundo;
        public string emocion;

        /// <summary>De 1 a 5.</summary>
        public int valor;

        /// <summary>Comentario opcional del jugador.</summary>
        public string texto;
    }

    /// <summary>Contenido del archivo del registro: el código de participante y las respuestas.</summary>
    [Serializable]
    public class DatosRegistro
    {
        public int version = 1;
        public string participante = string.Empty;
        public List<EntradaRegistro> entradas = new List<EntradaRegistro>();
    }

    /// <summary>
    /// Registro emocional (tarea G): al terminar cada nivel el jugador puede marcar, en una escala
    /// de 1 a 5, cómo vivió la emoción del mundo y dejar un comentario. Es opcional y nunca
    /// bloquea el juego.
    ///
    /// Genera datos cualitativos desde dentro del propio videojuego, que es lo que le falta al
    /// instrumento de la tesis frente a los cuestionarios externos (TMMS-24 y SCCS).
    ///
    /// - Se guarda en local, en un archivo aparte de la partida: <c>registro_emocional.json</c>, y
    ///   se exporta a la vez a <c>registro_emocional.csv</c> (separado por «;», legible en Excel).
    /// - Admite un código de participante opcional para el pilotaje. No guarda datos personales.
    /// - Como el guardado de la partida (patrón Facade): escritura atómica y, si el archivo está
    ///   dañado, se empieza uno nuevo sin borrar el anterior.
    /// </summary>
    public sealed class RegistroEmocional
    {
        public const string NombreJson = "registro_emocional.json";
        public const string NombreCsv = "registro_emocional.csv";
        public const int ValorMinimo = 1;
        public const int ValorMaximo = 5;

        /// <param name="carpeta">Carpeta del registro; por defecto <c>Application.persistentDataPath</c>.</param>
        public RegistroEmocional(string carpeta = null)
        {
            Carpeta = string.IsNullOrEmpty(carpeta) ? Application.persistentDataPath : carpeta;
            Datos = Cargar();
        }

        public string Carpeta { get; }

        public string RutaJson => Path.Combine(Carpeta, NombreJson);

        public string RutaCsv => Path.Combine(Carpeta, NombreCsv);

        public DatosRegistro Datos { get; private set; }

        public IReadOnlyList<EntradaRegistro> Entradas => Datos.entradas;

        /// <summary>Código de participante opcional (pilotaje). Vacío si no se ha indicado.</summary>
        public string Participante
        {
            get => Datos.participante ?? string.Empty;
            set => Datos.participante = (value ?? string.Empty).Trim();
        }

        /// <summary>Añade una respuesta y guarda el JSON y el CSV. El valor se limita a 1-5.</summary>
        public EntradaRegistro Registrar(LevelData nivel, int valor, string texto, DateTime? fecha = null)
        {
            var entrada = new EntradaRegistro
            {
                fecha = (fecha ?? DateTime.Now).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                participante = Participante,
                nivel = nivel != null ? nivel.id : 0,
                mundo = nivel != null ? nivel.nombreMundo : string.Empty,
                emocion = nivel != null && nivel.emocion != null ? nivel.emocion.nombreCanonico : string.Empty,
                valor = Mathf.Clamp(valor, ValorMinimo, ValorMaximo),
                texto = (texto ?? string.Empty).Trim(),
            };

            Datos.entradas.Add(entrada);
            Guardar();
            return entrada;
        }

        /// <summary>Escribe el JSON (atómico) y regenera el CSV. Devuelve si se guardó.</summary>
        public bool Guardar()
        {
            try
            {
                Directory.CreateDirectory(Carpeta);
                var temporal = RutaJson + ".tmp";
                File.WriteAllText(temporal, JsonUtility.ToJson(Datos, true), new UTF8Encoding(false));
                if (File.Exists(RutaJson))
                {
                    File.Replace(temporal, RutaJson, RutaJson + ".bak");
                }
                else
                {
                    File.Move(temporal, RutaJson);
                }

                // Con BOM: Excel reconoce así las tildes del CSV.
                File.WriteAllText(RutaCsv, ACsv(Datos), new UTF8Encoding(true));
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"[Registro emocional] No se pudo guardar en {Carpeta}: {e.Message}");
                return false;
            }
        }

        /// <summary>El registro como CSV separado por «;», con una fila por respuesta.</summary>
        public static string ACsv(DatosRegistro datos)
        {
            var csv = new StringBuilder();
            csv.Append("fecha;participante;nivel;mundo;emocion;valor;texto\n");
            foreach (var e in datos.entradas)
            {
                csv.Append(Campo(e.fecha)).Append(';')
                    .Append(Campo(e.participante)).Append(';')
                    .Append(e.nivel.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(Campo(e.mundo)).Append(';')
                    .Append(Campo(e.emocion)).Append(';')
                    .Append(e.valor.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(Campo(e.texto)).Append('\n');
            }

            return csv.ToString();
        }

        // Un campo de texto entre comillas si hace falta; los saltos de línea se vuelven espacios.
        private static string Campo(string valor)
        {
            var texto = (valor ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            return texto.IndexOfAny(new[] { ';', '"' }) >= 0 ? $"\"{texto.Replace("\"", "\"\"")}\"" : texto;
        }

        private DatosRegistro Cargar()
        {
            if (!File.Exists(RutaJson))
            {
                return new DatosRegistro();
            }

            try
            {
                var datos = JsonUtility.FromJson<DatosRegistro>(File.ReadAllText(RutaJson));
                if (datos != null)
                {
                    datos.entradas ??= new List<EntradaRegistro>();
                    datos.participante ??= string.Empty;
                    return datos;
                }
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Registro emocional] No se pudo leer {RutaJson}: {e.Message}");
            }

            Debug.LogWarning("[Registro emocional] El archivo está dañado: se empieza uno nuevo y el anterior se conserva como .danado.");
            try
            {
                File.Copy(RutaJson, RutaJson + ".danado", true);
            }
            catch (IOException)
            {
            }

            return new DatosRegistro();
        }
    }
}
