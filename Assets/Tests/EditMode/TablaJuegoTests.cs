using System;
using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Core.FSM;
using Fragmentum.Core.Juego;
using Fragmentum.EditorTools;
using NUnit.Framework;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Pruebas de la FSM global (tarea E): su tabla de transiciones y la tabla de efectos del
    /// plan (§3.6), que la convierten en la única dueña de Time.timeScale y del input.
    /// </summary>
    public class TablaJuegoTests
    {
        private static readonly ESTADOS_JUEGO[] Todos = (ESTADOS_JUEGO[])Enum.GetValues(typeof(ESTADOS_JUEGO));

        private TablaTransiciones<ESTADOS_JUEGO> _tabla;

        [SetUp]
        public void Preparar() => _tabla = GameStateMachine.CrearTabla();

        [Test]
        public void TodosLosEstados_SonAlcanzablesDesdeBOOT_YNingunoEsUnCallejon()
        {
            var visitados = new HashSet<ESTADOS_JUEGO> { ESTADOS_JUEGO.BOOT };
            var pendientes = new Queue<ESTADOS_JUEGO>(visitados);
            while (pendientes.Count > 0)
            {
                var actual = pendientes.Dequeue();
                foreach (var t in _tabla.Transiciones)
                {
                    if (t.Desde == actual && visitados.Add(t.Hacia)) pendientes.Enqueue(t.Hacia);
                }
            }

            CollectionAssert.AreEquivalent(Todos, visitados);

            foreach (var estado in Todos)
            {
                var tieneSalida = false;
                foreach (var t in _tabla.Transiciones) tieneSalida |= t.Desde == estado;
                Assert.IsTrue(tieneSalida, $"{estado} no tiene salida: el juego se quedaría atascado.");
            }
        }

        [Test]
        public void SoloSeCambiaDeEscenaPasandoPorLOADING()
        {
            // Menú y selector son escenas distintas: no hay salto directo entre ellos.
            Assert.IsFalse(_tabla.EsValida(ESTADOS_JUEGO.MAIN_MENU, ESTADOS_JUEGO.LEVEL_SELECT));
            Assert.IsFalse(_tabla.EsValida(ESTADOS_JUEGO.LEVEL_SELECT, ESTADOS_JUEGO.PLAYING));
            Assert.IsTrue(_tabla.EsValida(ESTADOS_JUEGO.LEVEL_SELECT, ESTADOS_JUEGO.LOADING));
            Assert.IsTrue(_tabla.EsValida(ESTADOS_JUEGO.LOADING, ESTADOS_JUEGO.PLAYING));
        }

        [Test]
        public void LaPausa_SoloExisteDentroDeUnNivel()
        {
            foreach (var estado in Todos)
            {
                Assert.AreEqual(estado == ESTADOS_JUEGO.PLAYING, _tabla.EsValida(estado, ESTADOS_JUEGO.PAUSED), $"{estado} -> PAUSED");
            }
        }

        [Test]
        public void GameOver_OfreceReintentarOSalir()
        {
            Assert.IsTrue(_tabla.EsValida(ESTADOS_JUEGO.GAME_OVER, ESTADOS_JUEGO.PLAYING));
            Assert.IsTrue(_tabla.EsValida(ESTADOS_JUEGO.GAME_OVER, ESTADOS_JUEGO.LOADING));
            Assert.IsFalse(_tabla.EsValida(ESTADOS_JUEGO.GAME_OVER, ESTADOS_JUEGO.LEVEL_COMPLETE));
        }

        [TestCase(ESTADOS_JUEGO.BOOT, 1f, true)]
        [TestCase(ESTADOS_JUEGO.MAIN_MENU, 1f, true)]
        [TestCase(ESTADOS_JUEGO.LEVEL_SELECT, 1f, true)]
        [TestCase(ESTADOS_JUEGO.LOADING, 1f, true)]
        [TestCase(ESTADOS_JUEGO.PLAYING, 1f, false)]
        [TestCase(ESTADOS_JUEGO.PAUSED, 0f, true)]
        [TestCase(ESTADOS_JUEGO.CUTSCENE, 0f, true)]
        [TestCase(ESTADOS_JUEGO.GAME_OVER, 0f, true)]
        [TestCase(ESTADOS_JUEGO.LEVEL_COMPLETE, 0f, true)]
        public void Efectos_CoincidenConLaTablaDelPlan(ESTADOS_JUEGO estado, float escalaTiempo, bool bloqueaInput)
        {
            var efectos = GameStateMachine.Efectos(estado);

            Assert.AreEqual(escalaTiempo, efectos.escalaTiempo);
            Assert.AreEqual(bloqueaInput, efectos.bloqueaInput);
        }

        [Test]
        public void Diagrama_AgrupaLosEstadosDentroDelNivel()
        {
            var mermaid = ExportadorDiagramasFSM.GenerarMermaid(_tabla);

            StringAssert.Contains("state EN_NIVEL {", mermaid);
            StringAssert.Contains("[*] --> BOOT", mermaid);
            StringAssert.Contains("PLAYING --> PAUSED : pausa", mermaid);
        }
    }
}
