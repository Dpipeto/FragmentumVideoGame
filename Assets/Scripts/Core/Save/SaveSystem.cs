using System;
using System.IO;
using UnityEngine;

namespace Fragmentum.Core.Save
{
    /// <summary>
    /// Guardado del progreso en JSON (patrón Facade): oculta la ruta, la serialización, la
    /// escritura atómica, la copia de seguridad y la migración desde la v1.
    ///
    /// - Archivo: <c>persistentDataPath/fragmentum_guardado.json</c>.
    /// - Escritura atómica: se escribe un temporal y se reemplaza el archivo, dejando el
    ///   anterior como <c>.bak</c>. Un cierre a mitad de guardado nunca deja la partida rota.
    /// - Si el archivo está dañado, se recupera la copia; si tampoco sirve, se empieza de cero
    ///   avisando por consola (nunca se lanza una excepción al jugador).
    ///
    /// Sustituye a los PlayerPrefs sueltos de la v1, donde las monedas se leían pero no se
    /// guardaban nunca.
    /// </summary>
    public sealed class SaveSystem
    {
        public const int VersionActual = 1;
        public const string NombreArchivo = "fragmentum_guardado.json";

        private readonly MigracionPlayerPrefs _migracion;

        /// <param name="carpeta">Carpeta del guardado; por defecto <c>Application.persistentDataPath</c>.</param>
        /// <param name="migracion">Fuente de la migración; las pruebas pasan una falsa.</param>
        public SaveSystem(string carpeta = null, MigracionPlayerPrefs migracion = null)
        {
            Carpeta = string.IsNullOrEmpty(carpeta) ? Application.persistentDataPath : carpeta;
            _migracion = migracion ?? new MigracionPlayerPrefs();
        }

        public string Carpeta { get; }

        public string Ruta => Path.Combine(Carpeta, NombreArchivo);

        public string RutaCopia => Ruta + ".bak";

        private string RutaTemporal => Ruta + ".tmp";

        public bool Existe => File.Exists(Ruta);

        /// <summary>Carga la partida. Nunca devuelve null ni lanza excepciones.</summary>
        public DatosGuardado Cargar()
        {
            if (!Existe)
            {
                var nuevos = new DatosGuardado();
                _migracion.Aplicar(nuevos);
                return nuevos;
            }

            if (Leer(Ruta, out var datos))
            {
                return datos;
            }

            Debug.LogWarning($"[Guardado] El archivo {Ruta} está dañado; se intenta recuperar la copia de seguridad.");
            if (File.Exists(RutaCopia) && Leer(RutaCopia, out datos))
            {
                return datos;
            }

            Debug.LogWarning("[Guardado] No se pudo recuperar la partida; se empieza una nueva. El archivo dañado no se borra.");
            return new DatosGuardado();
        }

        /// <summary>Guarda con escritura atómica. Devuelve si se guardó.</summary>
        public bool Guardar(DatosGuardado datos)
        {
            if (datos == null)
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(Carpeta);
                datos.version = VersionActual;
                File.WriteAllText(RutaTemporal, JsonUtility.ToJson(datos, true));

                if (File.Exists(Ruta))
                {
                    File.Replace(RutaTemporal, Ruta, RutaCopia);
                }
                else
                {
                    File.Move(RutaTemporal, Ruta);
                }

                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"[Guardado] No se pudo guardar en {Ruta}: {e.Message}");
                return false;
            }
        }

        private static bool Leer(string ruta, out DatosGuardado datos)
        {
            datos = null;
            try
            {
                var texto = File.ReadAllText(ruta);
                if (string.IsNullOrWhiteSpace(texto))
                {
                    return false;
                }

                datos = JsonUtility.FromJson<DatosGuardado>(texto);
                if (datos == null)
                {
                    return false;
                }

                datos.Normalizar();
                return true;
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
