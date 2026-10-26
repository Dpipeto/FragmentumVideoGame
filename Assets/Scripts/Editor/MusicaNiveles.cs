using Fragmentum.Data;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// La música del juego (PENDIENTES B8): cómo se importa y dónde va. Sale de los proyectos de LMMS de
    /// <c>Musica/</c>: los <c>Herramientas/componer_*.py</c> los escriben y <c>Herramientas/exportar_musica.py</c>
    /// los pasa a OGG.
    /// - <b>Los niveles y sus jefes</b> (<see cref="Carpeta"/>): bucles que se repiten sin costura. Se leen del
    ///   disco mientras suenan.
    /// - <b>Las cinemáticas</b> (<see cref="CarpetaCinematicas"/>): una pieza por cinemática, que dura lo que
    ///   ella; se llama como su id (<see cref="DeCinematica"/>). Las memorias de la Culpa no llevan: el Anexo A
    ///   pide silencio.
    /// - <b>Los golpes</b> (<see cref="CarpetaGolpes"/>): lo que suena al hacer algo, como encender una antorcha.
    /// Las piezas cortas se cargan en memoria: tienen que sonar en el acto, a la vez que su imagen.
    /// </summary>
    public static class MusicaNiveles
    {
        public const string Carpeta = "Assets/Audio/Musica";
        public const string CarpetaCinematicas = Carpeta + "/Cinematicas";
        public const string CarpetaGolpes = Carpeta + "/Golpes";

        /// <summary>El Nivel 1: percusión industrial y un sinte roto. Mi frigio, 160 BPM, 64 compases (96 s).</summary>
        public const string RutaIra = Carpeta + "/Nivel1_Ira.ogg";

        /// <summary>La Centinela Ardiente. Mi frigio, 168 BPM, 48 compases (68,6 s).</summary>
        public const string RutaCentinela = Carpeta + "/Nivel1_Centinela.ogg";

        /// <summary>El Nivel 2: la caja de música bajo el agua y el piano. Si menor, 70 BPM, 24 compases (82,3 s).</summary>
        public const string RutaTristeza = Carpeta + "/Nivel2_Tristeza.ogg";

        /// <summary>La Doncella del Lamento: un vals. Si menor, 3/4, 140 BPM, 48 compases (61,7 s).</summary>
        public const string RutaDoncella = Carpeta + "/Nivel2_Doncella.ogg";

        /// <summary>El Nivel 3: casi silencio y ruidos del bosque. 192 s.</summary>
        public const string RutaMiedo = Carpeta + "/Nivel3_Miedo.ogg";

        /// <summary>El Espectro del Pavor: percusión irregular y graves. Do# menor, 105 BPM, 32 compases (73,1 s).</summary>
        public const string RutaPavor = Carpeta + "/Nivel3_Pavor.ogg";

        /// <summary>El Nivel 4: la marcha. Re menor, 64 BPM, 32 compases (120 s).</summary>
        public const string RutaMarchaNivel4 = Carpeta + "/Nivel4_Marcha.ogg";

        /// <summary>El Juez Silente: órgano y bajo. Re menor, 96 BPM, 32 compases (80 s).</summary>
        public const string RutaJuez = Carpeta + "/Nivel4_Juez.ogg";

        /// <summary>El menú inicial y el selector de niveles: el tema de Kael, tranquilo. Re menor, 60 BPM, 24 compases (96 s).</summary>
        public const string RutaMenu = Carpeta + "/Menu.ogg";

        /// <summary>Las cuerdas al encender una antorcha (Anexo A: «stinger de cuerdas»), 3 s.</summary>
        public const string RutaAntorcha = CarpetaGolpes + "/Antorcha.ogg";

        /// <summary>La música de cada nivel y la de su jefe, por orden (Ira, Tristeza, Miedo y Culpa).</summary>
        public static readonly (string musica, string jefe)[] DeLosNiveles =
        {
            (RutaIra, RutaCentinela), (RutaTristeza, RutaDoncella), (RutaMiedo, RutaPavor), (RutaMarchaNivel4, RutaJuez),
        };

        /// <summary>La música de una cinemática, por su id («Jefe_Culpa», «Cierre_Ira»…), o nada si no tiene.</summary>
        public static AudioClip DeCinematica(string id) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{CarpetaCinematicas}/{id}.ogg");

        /// <summary>
        /// A cada cinemática de <see cref="GeneradorNarrativa.Carpeta"/> le pone la música que lleva su id, si
        /// hay. Las que no tienen pieza (las memorias, las del Jardín y el final) se quedan como están.
        /// </summary>
        public static void PonerEnLasCinematicas()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CutsceneData", new[] { GeneradorNarrativa.Carpeta }))
            {
                var cinematica = AssetDatabase.LoadAssetAtPath<Narrative.CutsceneData>(AssetDatabase.GUIDToAssetPath(guid));
                var musica = cinematica != null ? DeCinematica(cinematica.Id) : null;
                if (musica != null && cinematica.musica != musica)
                {
                    cinematica.musica = musica;
                    EditorUtility.SetDirty(cinematica);
                }
            }
        }

        /// <summary>¿Es una pieza corta, que tiene que sonar en el acto (una cinemática o un golpe)?</summary>
        public static bool EsCorta(string ruta) =>
            ruta.StartsWith(CarpetaCinematicas + "/") || ruta.StartsWith(CarpetaGolpes + "/");

        /// <summary>
        /// En el juego, en Vorbis, estéreo y a su frecuencia, como sale de LMMS. Los bucles se leen del disco
        /// mientras suenan (no ocupan memoria entera); las piezas cortas se cargan antes, para sonar en el acto.
        /// </summary>
        public static void AjustarImportador(AudioImporter importador, string ruta)
        {
            var ajustes = importador.defaultSampleSettings;
            ajustes.loadType = EsCorta(ruta) ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.Streaming;
            ajustes.compressionFormat = AudioCompressionFormat.Vorbis;
            ajustes.quality = 0.7f;
            ajustes.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importador.defaultSampleSettings = ajustes;
            importador.forceToMono = false;
            importador.loadInBackground = !EsCorta(ruta);
        }

        /// <summary>Pone a un nivel su música y la de su jefe, si no las tiene (lo puesto a mano no se pisa).</summary>
        public static void Asignar(LevelData nivel, string rutaMusica, string rutaJefe)
        {
            AsegurarImportacion(rutaMusica);
            AsegurarImportacion(rutaJefe);
            var cambio = false;
            if (nivel.musica == null)
            {
                nivel.musica = AssetDatabase.LoadAssetAtPath<AudioClip>(rutaMusica);
                cambio |= nivel.musica != null;
            }

            if (nivel.musicaJefe == null)
            {
                nivel.musicaJefe = AssetDatabase.LoadAssetAtPath<AudioClip>(rutaJefe);
                cambio |= nivel.musicaJefe != null;
            }

            if (cambio)
            {
                EditorUtility.SetDirty(nivel);
            }
        }

        /// <summary>Pone la música del menú y del selector en la base de datos, si no la tiene.</summary>
        public static void AsignarAlMenu(LevelDatabase bd)
        {
            AsegurarImportacion(RutaMenu);
            if (bd.musicaMenu == null)
            {
                bd.musicaMenu = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaMenu);
                if (bd.musicaMenu != null)
                {
                    EditorUtility.SetDirty(bd);
                }
            }
        }

        // Si se importó antes de que existiera esta regla, se vuelve a importar con ella.
        private static void AsegurarImportacion(string ruta)
        {
            if (!(AssetImporter.GetAtPath(ruta) is AudioImporter importador))
            {
                return;
            }

            var esperado = EsCorta(ruta) ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.Streaming;
            if (importador.defaultSampleSettings.loadType == esperado)
            {
                return;
            }

            AjustarImportador(importador, ruta);
            importador.SaveAndReimport();
        }
    }
}
