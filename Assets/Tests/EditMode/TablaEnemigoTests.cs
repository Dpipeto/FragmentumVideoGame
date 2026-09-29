using System;
using System.Collections.Generic;
using Fragmentum.Core.FSM;
using Fragmentum.EditorTools;
using Fragmentum.Enemies;
using NUnit.Framework;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Pruebas de la tabla de transiciones del enemigo: fijan por escrito las reglas de la
    /// tarea A del encargo (un único punto de transición y un diagrama que corresponde al código).
    /// </summary>
    public class TablaEnemigoTests
    {
        private static readonly ESTADOS_ENEMIGO[] Todos = (ESTADOS_ENEMIGO[])Enum.GetValues(typeof(ESTADOS_ENEMIGO));

        private TablaTransiciones<ESTADOS_ENEMIGO> _tabla;

        [SetUp]
        public void Preparar() => _tabla = EnemyController.CrearTabla();

        [Test]
        public void LosSeisEstadosPedidos_Existen()
        {
            CollectionAssert.AreEquivalent(
                new[] { "IDLE", "PATROL", "CHASE", "ATTACK", "DAMAGED", "DEAD" },
                Array.ConvertAll(Todos, e => e.ToString()));
        }

        [Test]
        public void TodosLosEstados_SonAlcanzablesDesdeIDLE()
        {
            var visitados = new HashSet<ESTADOS_ENEMIGO> { ESTADOS_ENEMIGO.IDLE };
            var pendientes = new Queue<ESTADOS_ENEMIGO>(visitados);
            while (pendientes.Count > 0)
            {
                var actual = pendientes.Dequeue();
                foreach (var t in _tabla.Transiciones)
                {
                    if (t.Desde == actual && visitados.Add(t.Hacia))
                    {
                        pendientes.Enqueue(t.Hacia);
                    }
                }
            }

            CollectionAssert.AreEquivalent(Todos, visitados);
        }

        [Test]
        public void DEAD_EsFinal_YSeLlegaDesdeCualquierEstadoVivo()
        {
            foreach (var estado in Todos)
            {
                Assert.IsFalse(_tabla.EsValida(ESTADOS_ENEMIGO.DEAD, estado), $"DEAD -> {estado} no debe existir.");
                if (estado != ESTADOS_ENEMIGO.DEAD)
                {
                    Assert.IsTrue(_tabla.EsValida(estado, ESTADOS_ENEMIGO.DEAD), $"{estado} -> DEAD");
                }
            }
        }

        [Test]
        public void SoloSeAtacaDesdeLaPersecucion()
        {
            foreach (var estado in Todos)
            {
                Assert.AreEqual(estado == ESTADOS_ENEMIGO.CHASE, _tabla.EsValida(estado, ESTADOS_ENEMIGO.ATTACK), $"{estado} -> ATTACK");
            }
        }

        [Test]
        public void DAMAGED_InterrumpeCualquierEstadoVivo_YVuelveAPerseguir()
        {
            Assert.IsTrue(_tabla.EsValida(ESTADOS_ENEMIGO.ATTACK, ESTADOS_ENEMIGO.DAMAGED), "El daño debe interrumpir el ataque.");
            Assert.IsTrue(_tabla.EsValida(ESTADOS_ENEMIGO.IDLE, ESTADOS_ENEMIGO.DAMAGED));
            Assert.IsTrue(_tabla.EsValida(ESTADOS_ENEMIGO.PATROL, ESTADOS_ENEMIGO.DAMAGED));
            Assert.IsTrue(_tabla.EsValida(ESTADOS_ENEMIGO.CHASE, ESTADOS_ENEMIGO.DAMAGED));
            Assert.IsTrue(_tabla.EsValida(ESTADOS_ENEMIGO.DAMAGED, ESTADOS_ENEMIGO.CHASE));
            Assert.IsFalse(_tabla.EsValida(ESTADOS_ENEMIGO.DAMAGED, ESTADOS_ENEMIGO.ATTACK), "Aturdido no puede atacar.");
        }

        [Test]
        public void AlPerderAlJugador_VuelveAPatrullarOAEsperar()
        {
            Assert.IsTrue(_tabla.EsValida(ESTADOS_ENEMIGO.CHASE, ESTADOS_ENEMIGO.PATROL));
            Assert.IsTrue(_tabla.EsValida(ESTADOS_ENEMIGO.CHASE, ESTADOS_ENEMIGO.IDLE));
        }

        [Test]
        public void Diagrama_DibujaElGrupoVivoYLaMuerteDesdeElGrupo()
        {
            var mermaid = ExportadorDiagramasFSM.GenerarMermaid(_tabla);

            StringAssert.Contains("state VIVO {", mermaid);
            StringAssert.Contains("VIVO --> DEAD", mermaid);
            StringAssert.Contains("CHASE --> ATTACK", mermaid);
            Assert.IsFalse(mermaid.Contains("IDLE --> DEAD"));
        }
    }
}
