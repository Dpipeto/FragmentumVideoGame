using Fragmentum.Bosses;
using Fragmentum.Core.Combate;
using NUnit.Framework;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Reglas de la vida de los jefes: la ventana vulnerable multiplica el daño, el Juez Silente
    /// es inmune fuera de ella y los golpes letales lo atraviesan todo.
    /// </summary>
    public class SaludJefeTests
    {
        private GameObject _objeto;
        private SaludJefe _vida;
        private bool _vulnerable;

        [SetUp]
        public void Preparar()
        {
            _objeto = new GameObject("Jefe");
            _vida = _objeto.AddComponent<SaludJefe>();
            _vida.Inicializar(20, 0f);
            _vida.MultiplicadorVulnerable = 2f;
            _vida.EsVulnerable = () => _vulnerable;
            _vulnerable = false;
        }

        [TearDown]
        public void Limpiar() => Object.DestroyImmediate(_objeto);

        [Test]
        public void FueraDeLaVentana_DanioNormal()
        {
            Assert.IsTrue(_vida.RecibirGolpe(new Golpe(3, Vector2.zero)));
            Assert.AreEqual(17, _vida.Vida);
        }

        [Test]
        public void EnLaVentana_ElDanioSeMultiplica()
        {
            _vulnerable = true;

            Assert.IsTrue(_vida.RecibirGolpe(new Golpe(3, Vector2.zero)));
            Assert.AreEqual(14, _vida.Vida, "3 × 2 = 6 de daño.");
        }

        [Test]
        public void Inmune_FueraDeLaVentana_NoRecibeDanio()
        {
            _vida.InmuneFueraDeVulnerable = true;
            var bloqueados = 0;
            _vida.OnGolpeBloqueado += _ => bloqueados++;

            Assert.IsFalse(_vida.RecibirGolpe(new Golpe(3, Vector2.zero)));
            Assert.AreEqual(20, _vida.Vida);
            Assert.AreEqual(1, bloqueados);

            _vulnerable = true;
            Assert.IsTrue(_vida.RecibirGolpe(new Golpe(3, Vector2.zero)), "En la ventana, sí.");
            Assert.AreEqual(14, _vida.Vida);
        }

        [Test]
        public void UnGolpeLetal_AtraviesaLaInmunidad()
        {
            _vida.InmuneFueraDeVulnerable = true;

            _vida.Matar();

            Assert.IsFalse(_vida.EstaVivo, "La lava o el vacío matan también a un jefe.");
        }
    }
}
