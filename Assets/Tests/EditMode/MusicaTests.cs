using System.IO;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Narrative;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La música del juego (PENDIENTES B8), hecha con los sonidos de LMMS (<c>Musica/LEEME.md</c>):
    /// - cada bucle de nivel y de jefe dura sus compases justos (si no, salta al repetirse), se lee del disco
    ///   mientras suena y está puesto en su nivel; y el del menú, que es también el del selector;
    /// - cada cinemática con música dura lo que ella y suena en el acto; las memorias, en silencio (Anexo A);
    /// - la antorcha suena al encenderse;
    /// - y sus proyectos de LMMS están junto al juego.
    /// </summary>
    public class MusicaTests
    {
        private const string Exportar = "python Herramientas/exportar_musica.py";

        // (ruta, compases, BPM, tiempos por compás)
        private static readonly object[] Bucles =
        {
            new object[] { MusicaNiveles.RutaIra, 64, 160f, 4 },
            new object[] { MusicaNiveles.RutaCentinela, 48, 168f, 4 },
            new object[] { MusicaNiveles.RutaTristeza, 24, 70f, 4 },
            new object[] { MusicaNiveles.RutaDoncella, 48, 140f, 3 },
            new object[] { MusicaNiveles.RutaMiedo, 48, 60f, 4 },
            new object[] { MusicaNiveles.RutaPavor, 32, 105f, 4 },
            new object[] { MusicaNiveles.RutaMarchaNivel4, 32, 64f, 4 },
            new object[] { MusicaNiveles.RutaJuez, 32, 96f, 4 },
            new object[] { MusicaNiveles.RutaMenu, 24, 60f, 4 },
        };

        private static readonly string[] Cinematicas =
        {
            "Inicial",
            "Jefe_Ira", "Jefe_Tristeza", "Jefe_Miedo", "Jefe_Culpa",
            "Derrota_Ira", "Derrota_Tristeza", "Derrota_Miedo", "Derrota_Culpa",
            "Cierre_Ira", "Cierre_Tristeza", "Cierre_Miedo", "Cierre_Culpa",
        };

        private static readonly string[] Memorias = { "ElCompanero", "ElPueblo", "LaCarta", "LaOrden" };

        private static readonly string[] Proyectos =
        {
            "Musica/Nivel1/Nivel1_Ira.mmp", "Musica/Nivel1/Nivel1_Centinela.mmp",
            "Musica/Nivel2/Nivel2_Tristeza.mmp", "Musica/Nivel2/Nivel2_Doncella.mmp",
            "Musica/Nivel3/Nivel3_Miedo.mmp", "Musica/Nivel3/Nivel3_Pavor.mmp",
            "Musica/Nivel4/Nivel4_Marcha.mmp", "Musica/Nivel4/Nivel4_Juez.mmp",
            "Musica/Golpes/Antorcha.mmp", "Musica/Cinematicas/Inicial.mmp", "Musica/Cinematicas/Jefe_Culpa.mmp",
            "Musica/Menu/Menu.mmp",
        };

        private static AudioClip Clip(string ruta)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
            Assert.IsNotNull(clip, $"Falta {ruta}: {Exportar}");
            return clip;
        }

        private static CutsceneData Cinematica(string id)
        {
            var cinematica = AssetDatabase.LoadAssetAtPath<CutsceneData>($"{GeneradorNarrativa.Carpeta}/Cinematica_{id}.asset");
            Assert.IsNotNull(cinematica, $"Falta la cinemática {id}.");
            return cinematica;
        }

        [TestCaseSource(nameof(Bucles))]
        public void CadaBucle_DuraSusCompasesJustos_ParaRepetirseSinCostura(string ruta, int compases, float bpm, int tiempos)
        {
            var clip = Clip(ruta);
            Assert.AreEqual(44100, clip.frequency);
            Assert.AreEqual(2, clip.channels, "En estéreo, como sale de LMMS.");
            Assert.AreEqual((int)System.Math.Round(compases * tiempos * 60.0 / bpm * 44100.0), clip.samples,
                "Ni una muestra de más ni de menos: el último compás enlaza con el primero.");
        }

        [TestCaseSource(nameof(Bucles))]
        public void CadaBucle_SeLeeDelDiscoMientrasSuena(string ruta, int compases, float bpm, int tiempos)
        {
            Clip(ruta);
            var importador = (AudioImporter)AssetImporter.GetAtPath(ruta);
            Assert.AreEqual(AudioClipLoadType.Streaming, importador.defaultSampleSettings.loadType);
            Assert.AreEqual(AudioCompressionFormat.Vorbis, importador.defaultSampleSettings.compressionFormat);
            Assert.IsFalse(importador.forceToMono);
        }

        [Test]
        public void CadaNivel_SuenaConSuMusica_YSuJefeConLaSuya()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            for (var i = 0; i < MusicaNiveles.DeLosNiveles.Length; i++)
            {
                var (musica, jefe) = MusicaNiveles.DeLosNiveles[i];
                Assert.AreSame(Clip(musica), bd.niveles[i].musica, $"El nivel {i + 1}, en su recorrido.");
                Assert.AreSame(Clip(jefe), bd.niveles[i].musicaJefe, $"El nivel {i + 1}, en la arena de su jefe.");
            }
        }

        [Test]
        public void ElMenuYElSelector_SuenanConLaMusicaDelMenu()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            Assert.AreSame(Clip(MusicaNiveles.RutaMenu), bd.musicaMenu, "La misma en los dos: la toca MusicaMenu, con los sistemas.");
        }

        [TestCaseSource(nameof(Cinematicas))]
        public void CadaCinematica_LlevaSuMusica_QueDuraLoQueElla_YSuenaEnElActo(string id)
        {
            var cinematica = Cinematica(id);
            var clip = Clip($"{MusicaNiveles.CarpetaCinematicas}/{id}.ogg");
            Assert.AreSame(clip, cinematica.musica);
            Assert.AreEqual(Mathf.RoundToInt(cinematica.DuracionTotal * 44100f), clip.samples, 2,
                $"Dura lo que la cinemática ({cinematica.DuracionTotal} s): si cambia, hay que volver a componerla.");
            var importador = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            Assert.AreEqual(AudioClipLoadType.CompressedInMemory, importador.defaultSampleSettings.loadType,
                "Cargada antes: tiene que empezar a la vez que su primer plano.");
        }

        [TestCaseSource(nameof(Memorias))]
        public void LasMemorias_SeVenEnSilencio(string memoria)
        {
            Assert.IsNull(Cinematica($"Memoria_{memoria}").musica, "Anexo A: silencio en las memorias.");
        }

        [Test]
        public void LaAntorcha_SuenaAlEncenderse()
        {
            var prefab = GeneradorMecanicas.Cargar(GeneradorMecanicas.Antorcha);
            Assert.IsNotNull(prefab);
            var clip = Clip(MusicaNiveles.RutaAntorcha);
            Assert.AreSame(clip, prefab.GetComponent<Antorcha>().SonidoAlEncender, "Anexo A: «stinger de cuerdas».");
            Assert.AreEqual(3f * 44100f, clip.samples, 2f);
        }

        [TestCaseSource(nameof(Proyectos))]
        public void SusProyectosDeLMMS_EstanJuntoAlJuego(string proyecto)
        {
            FileAssert.Exists(Path.Combine(Directory.GetCurrentDirectory(), proyecto),
                "Para cambiar un tema: se abre en LMMS, se guarda y " + Exportar);
        }
    }
}
