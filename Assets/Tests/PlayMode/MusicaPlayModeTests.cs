using Fragmentum.Levels;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// La música de un nivel (<see cref="MusicaDelNivel"/>), paso a paso: en una cinemática se apaga, se
    /// para y al volver sigue donde iba; al pedir la del jefe, la que suena se apaga y la otra entra
    /// fundiendo; si se pide en una cinemática, entra callada y suena al volver; y si se pide justo al
    /// acabar una (la presentación del jefe), entra de golpe.
    /// </summary>
    public class MusicaPlayModeTests
    {
        private const float Volumen = 0.6f;
        private const float Fundido = 0.6f;

        private GameObject _objeto;
        private AudioClip _marcha;
        private AudioClip _juez;

        [SetUp]
        public void Preparar()
        {
            _objeto = new GameObject("Musica");
            _marcha = AudioClip.Create("Marcha", 44100, 2, 44100, false);
            _juez = AudioClip.Create("Juez", 44100, 2, 44100, false);
        }

        [TearDown]
        public void Limpiar()
        {
            Object.Destroy(_objeto);
            Object.Destroy(_marcha);
            Object.Destroy(_juez);
        }

        private MusicaDelNivel Musica()
        {
            var musica = new MusicaDelNivel(_objeto.AddComponent<AudioSource>());
            Assert.IsTrue(musica.Fuente.loop, "En bucle.");
            return musica;
        }

        [Test]
        public void EnUnaCinematica_SeApaga_SePara_YAlVolverSigue()
        {
            var musica = Musica();
            musica.Empezar(_marcha, Volumen);
            Assert.AreEqual(Volumen, musica.Fuente.volume, 1e-4f, "Empieza a su volumen.");

            musica.Avanzar(0.3f, true, Volumen, Fundido);
            Assert.AreEqual(0.3f, musica.Fuente.volume, 1e-4f, "A medio fundido.");
            Assert.IsFalse(musica.EnPausa);
            musica.Avanzar(0.3f, true, Volumen, Fundido);
            Assert.AreEqual(0f, musica.Fuente.volume, 1e-4f);
            Assert.IsTrue(musica.EnPausa, "Callada, se para.");
            musica.Avanzar(0.3f, true, Volumen, Fundido);
            Assert.IsTrue(musica.EnPausa, "Y sigue parada mientras dura la cinemática.");

            musica.Avanzar(0.3f, false, Volumen, Fundido);
            Assert.IsFalse(musica.EnPausa, "Al volver, sigue.");
            Assert.AreSame(_marcha, musica.Fuente.clip, "La misma.");
            Assert.AreEqual(0.3f, musica.Fuente.volume, 1e-4f, "Fundiendo.");
            musica.Avanzar(0.3f, false, Volumen, Fundido);
            Assert.AreEqual(Volumen, musica.Fuente.volume, 1e-4f);
        }

        [Test]
        public void AlPedirLaDelJefe_LaQueSuenaSeApaga_YLaOtraEntraFundiendo()
        {
            var musica = Musica();
            musica.Empezar(_marcha, Volumen);
            musica.Pedir(_juez);
            Assert.AreSame(_juez, musica.Pedida);

            musica.Avanzar(0.3f, false, Volumen, Fundido);
            Assert.AreSame(_marcha, musica.Fuente.clip, "Hasta que se apaga, suena la de antes.");
            Assert.AreEqual(0.3f, musica.Fuente.volume, 1e-4f);
            musica.Avanzar(0.3f, false, Volumen, Fundido);
            Assert.AreSame(_juez, musica.Fuente.clip, "Apagada, entra la del jefe.");
            Assert.AreEqual(0f, musica.Fuente.volume, 1e-4f);
            musica.Avanzar(0.3f, false, Volumen, Fundido);
            Assert.AreEqual(0.3f, musica.Fuente.volume, 1e-4f, "Fundiendo.");
            musica.Avanzar(0.3f, false, Volumen, Fundido);
            Assert.AreEqual(Volumen, musica.Fuente.volume, 1e-4f);

            musica.Pedir(_marcha);
            musica.Avanzar(0.6f, false, Volumen, Fundido);
            Assert.AreSame(_marcha, musica.Fuente.clip, "Y de vuelta a la del nivel.");
        }

        [Test]
        public void SiSePideEnUnaCinematica_EntraCallada_YSuenaAlVolver()
        {
            var musica = Musica();
            musica.Empezar(_marcha, Volumen);
            musica.Avanzar(0.6f, true, Volumen, Fundido);
            Assert.IsTrue(musica.EnPausa);

            musica.Pedir(_juez);
            musica.Avanzar(0.3f, true, Volumen, Fundido);
            Assert.AreSame(_juez, musica.Fuente.clip);
            Assert.IsTrue(musica.EnPausa, "Callada mientras dura la cinemática.");
            Assert.AreEqual(0f, musica.Fuente.volume, 1e-4f);

            musica.Avanzar(0.3f, false, Volumen, Fundido);
            Assert.IsFalse(musica.EnPausa);
            Assert.AreEqual(0.3f, musica.Fuente.volume, 1e-4f, "Al volver, entra fundiendo.");
        }

        [Test]
        public void AlAcabarUnaCinematica_LaQueSePideEntraDeGolpe()
        {
            // La presentación del jefe calla la del nivel; al acabar se pide la del jefe, que ya viene preparada.
            var musica = Musica();
            musica.Empezar(_marcha, Volumen);
            musica.Avanzar(0.6f, true, Volumen, Fundido);
            Assert.IsTrue(musica.EnPausa);

            musica.Pedir(_juez);
            musica.Avanzar(0.02f, false, Volumen, Fundido);
            Assert.AreSame(_juez, musica.Fuente.clip);
            Assert.IsFalse(musica.EnPausa);
            Assert.AreEqual(Volumen, musica.Fuente.volume, 1e-4f, "A su volumen, sin fundido.");
        }

        [Test]
        public void SiLaCinematicaSeSaltaEnseguida_LaQueSePideTambienEntraDeGolpe()
        {
            // Se salta antes de que la del nivel se haya apagado del todo.
            var musica = Musica();
            musica.Empezar(_marcha, Volumen);
            musica.Avanzar(0.3f, true, Volumen, Fundido);
            Assert.IsFalse(musica.EnPausa, "Aún no se ha apagado.");

            musica.Pedir(_juez);
            musica.Avanzar(0.02f, false, Volumen, Fundido);
            Assert.AreSame(_juez, musica.Fuente.clip);
            Assert.AreEqual(Volumen, musica.Fuente.volume, 1e-4f);

            // Y lo siguiente que se pida, ya sin cinemática, vuelve a fundir.
            musica.Pedir(_marcha);
            musica.Avanzar(0.3f, false, Volumen, Fundido);
            Assert.AreSame(_juez, musica.Fuente.clip, "Hasta que se apaga, suena la de antes.");
            Assert.AreEqual(0.3f, musica.Fuente.volume, 1e-4f);
        }

        [Test]
        public void SinNadaQueSonara_LaQueSePideEntraFundiendo()
        {
            var musica = Musica();
            musica.Pedir(_juez);
            musica.Avanzar(0.3f, false, Volumen, Fundido);
            Assert.AreSame(_juez, musica.Fuente.clip);
            Assert.AreEqual(0f, musica.Fuente.volume, 1e-4f);
            musica.Avanzar(0.6f, false, Volumen, Fundido);
            Assert.AreEqual(Volumen, musica.Fuente.volume, 1e-4f);
        }
    }
}
