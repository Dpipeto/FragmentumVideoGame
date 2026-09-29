using Fragmentum.Core.Combate;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Pruebas de la vida del jugador y de la Defensa Reforzada (escudo que absorbe un golpe
    /// cada 15 s, fragmento de la Culpa).
    /// </summary>
    public class PlayerHealthTests
    {
        private GameObject _objeto;
        private PlayerHealth _vida;
        private float _tiempo;

        [SetUp]
        public void Preparar()
        {
            _tiempo = 0f;
            _objeto = new GameObject("JugadorDePrueba");
            _vida = _objeto.AddComponent<PlayerHealth>();
            _vida.Reloj = () => _tiempo;
            _vida.Inicializar(3);
        }

        [TearDown]
        public void Limpiar() => Object.DestroyImmediate(_objeto);

        private static Golpe Golpe(int cantidad = 1) => new Golpe(cantidad, Vector2.zero);

        [Test]
        public void SinDefensaReforzada_ElGolpeQuitaVida()
        {
            Assert.IsTrue(_vida.RecibirGolpe(Golpe()));
            Assert.AreEqual(2, _vida.Vida);
        }

        [Test]
        public void ConDefensaReforzada_ElPrimerGolpeSeAbsorbe()
        {
            _vida.EscudoDesbloqueado = true;
            var absorbidos = 0;
            _vida.OnEscudoAbsorbio += _ => absorbidos++;

            Assert.IsFalse(_vida.RecibirGolpe(Golpe()), "Un golpe absorbido no surte efecto.");
            Assert.AreEqual(3, _vida.Vida);
            Assert.AreEqual(1, absorbidos);
            Assert.IsFalse(_vida.EscudoListo, "El escudo queda recargándose.");
        }

        [Test]
        public void TrasAbsorber_HayUnaVentanaDeInvulnerabilidad_YLuegoElGolpeEntra()
        {
            _vida.EscudoDesbloqueado = true;
            _vida.RecibirGolpe(Golpe());

            // Mismo instante: la hitbox que sigue tocando no debe colar el golpe.
            Assert.IsFalse(_vida.RecibirGolpe(Golpe()));
            Assert.AreEqual(3, _vida.Vida);

            // Pasada la ventana, el escudo sigue recargándose: el golpe entra.
            _tiempo = 1f;
            Assert.IsTrue(_vida.RecibirGolpe(Golpe()));
            Assert.AreEqual(2, _vida.Vida);
        }

        [Test]
        public void ElEscudo_SeRecargaA_Los15Segundos()
        {
            _vida.EscudoDesbloqueado = true;
            _vida.RecibirGolpe(Golpe());

            _tiempo = 14.9f;
            Assert.IsFalse(_vida.EscudoListo);

            _tiempo = 15f;
            Assert.IsTrue(_vida.EscudoListo);
            Assert.IsFalse(_vida.RecibirGolpe(Golpe()), "Recargado, vuelve a absorber.");
            Assert.AreEqual(3, _vida.Vida);
        }

        [Test]
        public void ElGolpeLetal_AtraviesaElEscudo()
        {
            // Lava, vacío y ahogamiento matan aunque la Defensa Reforzada esté lista.
            _vida.EscudoDesbloqueado = true;

            _vida.Matar();

            Assert.IsFalse(_vida.EstaVivo);
            Assert.IsTrue(_vida.EscudoListo, "El escudo no se gasta con un golpe que no puede absorber.");
        }
    }
}
