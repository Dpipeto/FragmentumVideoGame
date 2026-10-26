using System.IO;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Los efectos de sonido (PENDIENTES B8), hechos con los sonidos de LMMS (<c>Sonidos/LEEME.md</c>):
    /// - cada uno es un WAV mono a 44,1 kHz de menos de 2 s, que se carga descomprimido para sonar en el acto;
    /// - Kael lleva los suyos, cada uno en su estado;
    /// - y sus proyectos de LMMS están junto al juego.
    /// </summary>
    public class SonidosTests
    {
        private const string Exportar = "python Herramientas/exportar_sonidos.py";

        private static readonly string[] DeKael =
        {
            SonidosDelJuego.RutaSalto, SonidosDelJuego.RutaDobleSalto, SonidosDelJuego.RutaDash, SonidosDelJuego.RutaAtaque,
            SonidosDelJuego.RutaAtaqueCargado, SonidosDelJuego.RutaDanio,
        };

        private static readonly string[] Proyectos =
        {
            "Sonidos/Kael/Salto.mmp", "Sonidos/Kael/DobleSalto.mmp", "Sonidos/Kael/Dash.mmp", "Sonidos/Kael/Ataque.mmp",
            "Sonidos/Kael/AtaqueCargado.mmp", "Sonidos/Kael/Danio.mmp",
        };

        private static AudioClip Clip(string ruta)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
            Assert.IsNotNull(clip, $"Falta {ruta}: {Exportar}");
            return clip;
        }

        private static SonidosKael DeKaelEnElPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorJugador.RutaPrefab);
            Assert.IsNotNull(prefab);
            var sonidos = prefab.GetComponent<SonidosKael>();
            Assert.IsNotNull(sonidos, "Regenera el jugador: Fragmentum → Regenerar todo.");
            return sonidos;
        }

        [TestCaseSource(nameof(DeKael))]
        public void CadaEfecto_EsCortoYMono_YSuenaEnElActo(string ruta)
        {
            var clip = Clip(ruta);
            Assert.AreEqual(44100, clip.frequency);
            Assert.AreEqual(1, clip.channels);
            Assert.Less(clip.length, 2f, "PENDIENTES: «SFX: WAV mono, 44,1 kHz, menos de 2 s».");
            var importador = (AudioImporter)AssetImporter.GetAtPath(ruta);
            Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, importador.defaultSampleSettings.loadType,
                "Descomprimido al cargar: tiene que sonar en el mismo frame.");
            Assert.AreEqual(AudioCompressionFormat.PCM, importador.defaultSampleSettings.compressionFormat);
        }

        [Test]
        public void Kael_LlevaSusSonidos()
        {
            var sonidos = DeKaelEnElPrefab();
            Assert.AreSame(Clip(SonidosDelJuego.RutaSalto), sonidos.Salto);
            Assert.AreSame(Clip(SonidosDelJuego.RutaDobleSalto), sonidos.DobleSalto);
            Assert.AreSame(Clip(SonidosDelJuego.RutaDash), sonidos.Dash);
            Assert.AreSame(Clip(SonidosDelJuego.RutaAtaque), sonidos.Ataque);
            Assert.AreSame(Clip(SonidosDelJuego.RutaAtaqueCargado), sonidos.AtaqueCargado);
            Assert.AreSame(Clip(SonidosDelJuego.RutaDanio), sonidos.Danio);
        }

        [Test]
        public void CadaEstado_SuenaConElSuyo()
        {
            var sonidos = DeKaelEnElPrefab();
            Assert.AreSame(sonidos.Salto, sonidos.DeEstado(ESTADOS_JUGADOR.JUMP));
            Assert.AreSame(sonidos.Salto, sonidos.DeEstado(ESTADOS_JUGADOR.WALL_JUMP), "Desde la pared, también el salto.");
            Assert.AreSame(sonidos.DobleSalto, sonidos.DeEstado(ESTADOS_JUGADOR.DOUBLE_JUMP));
            Assert.AreSame(sonidos.Dash, sonidos.DeEstado(ESTADOS_JUGADOR.DASH));
            Assert.AreSame(sonidos.Ataque, sonidos.DeEstado(ESTADOS_JUGADOR.ATTACK));
            Assert.AreSame(sonidos.AtaqueCargado, sonidos.DeEstado(ESTADOS_JUGADOR.ATTACK, cargado: true));
            foreach (var callado in new[]
                     {
                         ESTADOS_JUGADOR.IDLE, ESTADOS_JUGADOR.RUN, ESTADOS_JUGADOR.CROUCH, ESTADOS_JUGADOR.FALL,
                         ESTADOS_JUGADOR.WALL_SLIDE, ESTADOS_JUGADOR.DAMAGED, ESTADOS_JUGADOR.DEAD,
                     })
            {
                Assert.IsNull(sonidos.DeEstado(callado), $"{callado} no suena (el daño suena con el golpe, no con el estado).");
            }
        }

        [TestCaseSource(nameof(Proyectos))]
        public void SusProyectosDeLMMS_EstanJuntoAlJuego(string proyecto)
        {
            FileAssert.Exists(Path.Combine(Directory.GetCurrentDirectory(), proyecto),
                "Para cambiar un efecto: se abre en LMMS, se guarda y " + Exportar);
        }
    }
}
