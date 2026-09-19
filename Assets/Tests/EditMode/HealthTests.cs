using Fragmentum.Core.Combate;
using NUnit.Framework;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Pruebas de la vida y los i-frames. Fijan el comportamiento que en el prototipo estaba
    /// roto: la zona de caída no mataba si el jugador estaba parpadeando, y un jefe golpeado
    /// se quedaba invulnerable para siempre porque nada apagaba su bandera de daño.
    /// </summary>
    public class HealthTests
    {
        private GameObject _objeto;
        private Health _vida;
        private float _tiempo;

        [SetUp]
        public void Preparar()
        {
            _tiempo = 0f;
            _objeto = new GameObject("SujetoDePrueba");
            _vida = _objeto.AddComponent<Health>();
            _vida.Reloj = () => _tiempo;
            _vida.Inicializar(3);
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(_objeto);
        }

        private Golpe CrearGolpe(int cantidad = 1, bool ignoraInvulnerabilidad = false) =>
            new Golpe(cantidad, Vector2.zero, null, false, ignoraInvulnerabilidad);

        [Test]
        public void Inicializar_DejaLaVidaAlMaximo()
        {
            Assert.AreEqual(3, _vida.Vida);
            Assert.AreEqual(3, _vida.VidaMaxima);
            Assert.IsTrue(_vida.EstaVivo);
        }

        [Test]
        public void RecibirGolpe_RestaVidaYAvisa()
        {
            var actual = -1;
            var maxima = -1;
            _vida.OnVidaCambiada += (a, m) => { actual = a; maxima = m; };

            Assert.IsTrue(_vida.RecibirGolpe(CrearGolpe()));

            Assert.AreEqual(2, _vida.Vida);
            Assert.AreEqual(2, actual);
            Assert.AreEqual(3, maxima);
        }

        [Test]
        public void RecibirGolpe_DuranteLosIFrames_NoHaceNada()
        {
            _vida.RecibirGolpe(CrearGolpe());

            Assert.IsTrue(_vida.EsInvulnerable);
            Assert.IsFalse(_vida.RecibirGolpe(CrearGolpe()));
            Assert.AreEqual(2, _vida.Vida);
        }

        [Test]
        public void RecibirGolpe_CuandoCaducanLosIFrames_VuelveAHacerDano()
        {
            _vida.RecibirGolpe(CrearGolpe());
            _tiempo = 10f;

            Assert.IsFalse(_vida.EsInvulnerable);
            Assert.IsTrue(_vida.RecibirGolpe(CrearGolpe()));
            Assert.AreEqual(1, _vida.Vida);
        }

        [Test]
        public void GolpeQueIgnoraInvulnerabilidad_MataAunqueEstesParpadeando()
        {
            // Este es el caso de la zona de caída: en el prototipo el golpe se perdía y el
            // jugador caía para siempre.
            _vida.RecibirGolpe(CrearGolpe());
            Assert.IsTrue(_vida.EsInvulnerable);

            var muerto = false;
            _vida.OnMuerte += () => muerto = true;

            Assert.IsTrue(_vida.RecibirGolpe(CrearGolpe(99, ignoraInvulnerabilidad: true)));
            Assert.AreEqual(0, _vida.Vida);
            Assert.IsFalse(_vida.EstaVivo);
            Assert.IsTrue(muerto);
        }

        [Test]
        public void Matar_DisparaLaMuerteUnaSolaVez()
        {
            var muertes = 0;
            _vida.OnMuerte += () => muertes++;

            _vida.Matar();
            _vida.Matar();

            Assert.AreEqual(1, muertes, "Un cadáver no puede volver a morir.");
        }

        [Test]
        public void Curar_NoPasaDeLaVidaMaxima()
        {
            _vida.RecibirGolpe(CrearGolpe());

            Assert.AreEqual(1, _vida.Curar(5));
            Assert.AreEqual(3, _vida.Vida);
            Assert.AreEqual(0, _vida.Curar(1), "Curar con la vida llena no cura nada.");
        }

        [Test]
        public void Curar_ConLaVidaAgotada_NoResucita()
        {
            _vida.Matar();

            Assert.AreEqual(0, _vida.Curar(2));
            Assert.IsFalse(_vida.EstaVivo);
        }

        [Test]
        public void Revivir_DevuelveLaVidaYQuitaLaInvulnerabilidad()
        {
            _vida.Matar();

            var revivido = false;
            _vida.OnRevivir += () => revivido = true;

            _vida.Revivir();

            Assert.IsTrue(_vida.EstaVivo);
            Assert.AreEqual(3, _vida.Vida);
            Assert.IsFalse(_vida.EsInvulnerable);
            Assert.IsTrue(revivido);
        }
    }
}
