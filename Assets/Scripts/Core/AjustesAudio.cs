using System;
using System.IO;
using UnityEngine;

namespace Fragmentum.Core
{
    /// <summary>
    /// El volumen que elige el jugador en «Opciones», de 0 a 1:
    /// - <see cref="General"/>: todo lo que suena (el del <c>AudioListener</c>).
    /// - <see cref="Musica"/>: la música (la del nivel, la de su jefe, la de las cinemáticas y la del menú),
    ///   sobre el volumen de cada una.
    /// - <see cref="Efectos"/>: los efectos de sonido (los de Kael, el de la antorcha), sobre el de cada uno.
    ///
    /// Se guarda aparte de la partida, en <c>fragmentum_ajustes.json</c> junto al guardado: empezar una
    /// partida nueva no lo toca. Si el archivo falta o está roto, todo a su volumen.
    /// </summary>
    public static class AjustesAudio
    {
        public const string NombreArchivo = "fragmentum_ajustes.json";

        [Serializable]
        private sealed class Datos
        {
            public float general = 1f;
            public float musica = 1f;
            public float efectos = 1f;
        }

        private static Datos _datos;
        private static string _carpeta;

        /// <summary>Cambió alguno.</summary>
        public static event Action Cambiaron;

        public static float General
        {
            get => Cargados.general;
            set => Cambiar(ref Cargados.general, value);
        }

        public static float Musica
        {
            get => Cargados.musica;
            set => Cambiar(ref Cargados.musica, value);
        }

        public static float Efectos
        {
            get => Cargados.efectos;
            set => Cambiar(ref Cargados.efectos, value);
        }

        public static string Ruta => Path.Combine(string.IsNullOrEmpty(_carpeta) ? Application.persistentDataPath : _carpeta, NombreArchivo);

        /// <summary>Lee y guarda los ajustes en otra carpeta (las pruebas, para no tocar los del jugador).</summary>
        public static void UsarCarpeta(string carpeta)
        {
            _carpeta = carpeta;
            _datos = null;
            Aplicar();
        }

        /// <summary>Pone el volumen general en el <c>AudioListener</c>.</summary>
        public static void Aplicar() => AudioListener.volume = General;

        private static Datos Cargados => _datos ??= Leer();

        private static void Cambiar(ref float campo, float valor)
        {
            valor = Mathf.Clamp01(valor);
            if (Mathf.Approximately(campo, valor))
            {
                return;
            }

            campo = valor;
            Aplicar();
            Guardar();
            Cambiaron?.Invoke();
        }

        private static Datos Leer()
        {
            try
            {
                if (File.Exists(Ruta))
                {
                    var datos = JsonUtility.FromJson<Datos>(File.ReadAllText(Ruta));
                    if (datos != null)
                    {
                        datos.general = Mathf.Clamp01(datos.general);
                        datos.musica = Mathf.Clamp01(datos.musica);
                        datos.efectos = Mathf.Clamp01(datos.efectos);
                        return datos;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Opciones] No se pudo leer {Ruta} ({e.Message}): todo a su volumen.");
            }

            return new Datos();
        }

        private static void Guardar()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Ruta));
                File.WriteAllText(Ruta, JsonUtility.ToJson(_datos, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Opciones] No se pudo guardar {Ruta}: {e.Message}");
            }
        }

        // Al entrar en juego: los del jugador, desde su archivo (y olvida la carpeta de las pruebas).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _carpeta = null;
            _datos = null;
            Cambiaron = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AlArrancar() => Aplicar();
    }
}
