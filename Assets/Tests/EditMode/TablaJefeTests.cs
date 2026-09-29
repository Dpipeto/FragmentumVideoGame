using System;
using Fragmentum.Bosses;
using Fragmentum.Core.FSM;
using NUnit.Framework;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La tabla de transiciones de los jefes corresponde al diagrama del plan (§3.5): fases que
    /// avanzan de una en una, ventana vulnerable desde cualquier fase, derrota desde todo el
    /// combate y ningún estado DAMAGED.
    /// </summary>
    public class TablaJefeTests
    {
        private TablaTransiciones<ESTADOS_JEFE> _tabla;

        [SetUp]
        public void Preparar() => _tabla = BossController.CrearTabla();

        [Test]
        public void ElCombateEmpiezaEnLaFase1()
        {
            Assert.AreEqual("PHASE_1", ((ITablaLegible)_tabla).EstadoInicial);
        }

        [Test]
        public void LasFases_AvanzanDeUnaEnUna_YNoRetroceden()
        {
            Assert.IsTrue(_tabla.EsValida(ESTADOS_JEFE.PHASE_1, ESTADOS_JEFE.PHASE_2));
            Assert.IsTrue(_tabla.EsValida(ESTADOS_JEFE.PHASE_2, ESTADOS_JEFE.PHASE_3));
            Assert.IsFalse(_tabla.EsValida(ESTADOS_JEFE.PHASE_1, ESTADOS_JEFE.PHASE_3));
            Assert.IsFalse(_tabla.EsValida(ESTADOS_JEFE.PHASE_2, ESTADOS_JEFE.PHASE_1));
            Assert.IsFalse(_tabla.EsValida(ESTADOS_JEFE.PHASE_3, ESTADOS_JEFE.PHASE_2));
        }

        [TestCase(ESTADOS_JEFE.PHASE_1)]
        [TestCase(ESTADOS_JEFE.PHASE_2)]
        [TestCase(ESTADOS_JEFE.PHASE_3)]
        public void CualquierFase_PuedeAbrirLaVentanaVulnerable(ESTADOS_JEFE fase)
        {
            Assert.IsTrue(_tabla.EsValida(fase, ESTADOS_JEFE.VULNERABLE));
            Assert.IsTrue(_tabla.EsValida(ESTADOS_JEFE.VULNERABLE, fase), "La ventana vuelve a la fase que toque según la vida.");
        }

        [Test]
        public void LaDerrota_LlegaDesdeTodoElCombate_YEsDefinitiva()
        {
            foreach (var estado in new[] { ESTADOS_JEFE.PHASE_1, ESTADOS_JEFE.PHASE_2, ESTADOS_JEFE.PHASE_3, ESTADOS_JEFE.VULNERABLE })
            {
                Assert.IsTrue(_tabla.EsValida(estado, ESTADOS_JEFE.DEFEATED), $"{estado} → DEFEATED");
            }

            foreach (ESTADOS_JEFE destino in Enum.GetValues(typeof(ESTADOS_JEFE)))
            {
                Assert.IsFalse(_tabla.EsValida(ESTADOS_JEFE.DEFEATED, destino), $"DEFEATED no sale a {destino}.");
            }
        }

        [Test]
        public void NoHayEstadoDAMAGED()
        {
            // Un golpe no interrumpe al jefe: así ninguno puede quedar congelado (bug de la v1).
            CollectionAssert.DoesNotContain(Enum.GetNames(typeof(ESTADOS_JEFE)), "DAMAGED");
        }

        [Test]
        public void ElGrupoCombate_ContieneLasFasesYLaVentana()
        {
            CollectionAssert.AreEquivalent(
                new[] { ESTADOS_JEFE.PHASE_1, ESTADOS_JEFE.PHASE_2, ESTADOS_JEFE.PHASE_3, ESTADOS_JEFE.VULNERABLE },
                _tabla.EstadosDelGrupo(BossController.GrupoCombate));
        }
    }
}
