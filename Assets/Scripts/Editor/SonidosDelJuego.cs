using Fragmentum.Player;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Los efectos de sonido del juego (PENDIENTES B8): cómo se importan y dónde van. Salen de los proyectos de
    /// LMMS de <c>Sonidos/</c>: <c>Herramientas/componer_sonidos_*.py</c> los escriben y
    /// <c>Herramientas/exportar_sonidos.py</c> los pasa a WAV mono, en la misma carpeta de <see cref="Carpeta"/>.
    /// Son cortos y tienen que sonar en el acto: se cargan descomprimidos.
    /// </summary>
    public static class SonidosDelJuego
    {
        public const string Carpeta = "Assets/Audio/Sonidos";
        public const string CarpetaKael = Carpeta + "/Kael";

        public const string RutaSalto = CarpetaKael + "/Salto.wav";
        public const string RutaDobleSalto = CarpetaKael + "/DobleSalto.wav";
        public const string RutaDash = CarpetaKael + "/Dash.wav";
        public const string RutaAtaque = CarpetaKael + "/Ataque.wav";
        public const string RutaAtaqueCargado = CarpetaKael + "/AtaqueCargado.wav";
        public const string RutaDanio = CarpetaKael + "/Danio.wav";

        /// <summary>Los de Kael: (campo de <see cref="SonidosKael"/>, archivo).</summary>
        public static readonly (string campo, string ruta)[] DeKael =
        {
            ("salto", RutaSalto), ("dobleSalto", RutaDobleSalto), ("dash", RutaDash), ("ataque", RutaAtaque),
            ("ataqueCargado", RutaAtaqueCargado), ("danio", RutaDanio),
        };

        /// <summary>En mono, sin comprimir y cargados antes: un efecto tiene que sonar en el mismo frame.</summary>
        public static void AjustarImportador(AudioImporter importador)
        {
            var ajustes = importador.defaultSampleSettings;
            ajustes.loadType = AudioClipLoadType.DecompressOnLoad;
            ajustes.compressionFormat = AudioCompressionFormat.PCM;
            ajustes.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            ajustes.preloadAudioData = true;
            importador.defaultSampleSettings = ajustes;
            importador.forceToMono = true;
            importador.loadInBackground = false;
        }

        /// <summary>Pone a Kael sus sonidos (el prefab del jugador los lleva en <see cref="SonidosKael"/>).</summary>
        public static void PonerEnKael(SonidosKael sonidos)
        {
            var serializado = new SerializedObject(sonidos);
            foreach (var (campo, ruta) in DeKael)
            {
                AsegurarImportacion(ruta);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                if (clip == null)
                {
                    Debug.LogWarning($"[Fragmentum] Falta {ruta}: python Herramientas/exportar_sonidos.py");
                }

                serializado.FindProperty(campo).objectReferenceValue = clip;
            }

            serializado.ApplyModifiedPropertiesWithoutUndo();
        }

        // Si se importó antes de que existiera esta regla, se vuelve a importar con ella.
        private static void AsegurarImportacion(string ruta)
        {
            if (AssetImporter.GetAtPath(ruta) is AudioImporter importador &&
                importador.defaultSampleSettings.loadType != AudioClipLoadType.DecompressOnLoad)
            {
                AjustarImportador(importador);
                importador.SaveAndReimport();
            }
        }
    }
}
