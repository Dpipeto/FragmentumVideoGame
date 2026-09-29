using System;
using System.Collections.Generic;
using Fragmentum.Core.FSM;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using NUnit.Framework;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Pruebas de la tabla de transiciones del jugador. La tabla es la especificación de la
    /// FSM y la fuente de su diagrama, así que estas pruebas fijan por escrito las reglas de
    /// diseño que la tesis describe (ninguna transición puede saltárselas sin romper una prueba).
    /// </summary>
    public class TablaJugadorTests
    {
        private static readonly ESTADOS_JUGADOR[] Locomocion =
        {
            ESTADOS_JUGADOR.IDLE, ESTADOS_JUGADOR.RUN, ESTADOS_JUGADOR.CROUCH, ESTADOS_JUGADOR.JUMP,
            ESTADOS_JUGADOR.DOUBLE_JUMP, ESTADOS_JUGADOR.FALL, ESTADOS_JUGADOR.WALL_SLIDE, ESTADOS_JUGADOR.WALL_JUMP
        };

        private static readonly ESTADOS_JUGADOR[] Todos = (ESTADOS_JUGADOR[])Enum.GetValues(typeof(ESTADOS_JUGADOR));

        private TablaTransiciones<ESTADOS_JUGADOR> _tabla;

        [SetUp]
        public void Preparar() => _tabla = PlayerController.CrearTabla();

        [Test]
        public void EstadoInicial_EsIDLE()
        {
            Assert.AreEqual("IDLE", ((ITablaLegible)_tabla).EstadoInicial);
        }

        [Test]
        public void TodosLosEstados_SonAlcanzablesDesdeIDLE()
        {
            var visitados = new HashSet<ESTADOS_JUGADOR> { ESTADOS_JUGADOR.IDLE };
            var pendientes = new Queue<ESTADOS_JUGADOR>(visitados);

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

            CollectionAssert.AreEquivalent(Todos, visitados, "Hay estados declarados a los que nunca se llega.");
        }

        [Test]
        public void TodoEstado_TieneSalida()
        {
            foreach (var estado in Todos)
            {
                var tieneSalida = false;
                foreach (var t in _tabla.Transiciones)
                {
                    tieneSalida |= t.Desde == estado;
                }

                Assert.IsTrue(tieneSalida, $"{estado} es un estado sin salida: el jugador quedaría atascado.");
            }
        }

        [Test]
        public void DesdeCualquierEstadoVivo_SePuedeMorir()
        {
            foreach (var estado in Todos)
            {
                if (estado != ESTADOS_JUGADOR.DEAD)
                {
                    Assert.IsTrue(_tabla.EsValida(estado, ESTADOS_JUGADOR.DEAD), $"{estado} no puede pasar a DEAD.");
                }
            }
        }

        [Test]
        public void DEAD_SoloSaleAIDLE_AlRevivir()
        {
            foreach (var estado in Todos)
            {
                Assert.AreEqual(estado == ESTADOS_JUGADOR.IDLE, _tabla.EsValida(ESTADOS_JUGADOR.DEAD, estado),
                    $"DEAD -> {estado}");
            }
        }

        [Test]
        public void LosOchoEstadosDeLocomocion_PuedenHacerDashYAtacar()
        {
            foreach (var estado in Locomocion)
            {
                Assert.IsTrue(_tabla.EsValida(estado, ESTADOS_JUGADOR.DASH), $"{estado} -> DASH");
                Assert.IsTrue(_tabla.EsValida(estado, ESTADOS_JUGADOR.ATTACK), $"{estado} -> ATTACK");
            }
        }

        [Test]
        public void DashYAtaque_NoSeEncadenanEntreSi()
        {
            Assert.IsFalse(_tabla.EsValida(ESTADOS_JUGADOR.DASH, ESTADOS_JUGADOR.ATTACK));
            Assert.IsFalse(_tabla.EsValida(ESTADOS_JUGADOR.ATTACK, ESTADOS_JUGADOR.DASH));
            Assert.IsFalse(_tabla.EsValida(ESTADOS_JUGADOR.DAMAGED, ESTADOS_JUGADOR.DASH),
                "Durante el retroceso no hay control.");
        }

        [Test]
        public void DAMAGED_DesdeCualquierEstadoVivoSalvoElMismo()
        {
            foreach (var estado in Todos)
            {
                var esperado = estado != ESTADOS_JUGADOR.DAMAGED && estado != ESTADOS_JUGADOR.DEAD;
                Assert.AreEqual(esperado, _tabla.EsValida(estado, ESTADOS_JUGADOR.DAMAGED), $"{estado} -> DAMAGED");
            }
        }

        [Test]
        public void WallJump_SoloDesdeLaPared_OSuCoyote()
        {
            var origenes = new List<ESTADOS_JUGADOR>();
            foreach (var t in _tabla.Transiciones)
            {
                if (t.Hacia == ESTADOS_JUGADOR.WALL_JUMP)
                {
                    origenes.Add(t.Desde);
                }
            }

            CollectionAssert.AreEquivalent(new[] { ESTADOS_JUGADOR.WALL_SLIDE, ESTADOS_JUGADOR.FALL }, origenes);
        }

        [Test]
        public void ElDobleSalto_NoPuedeRepetirseNiVolverASaltar()
        {
            // Esto es lo que impedía el salto infinito del prototipo: desde el doble salto
            // solo se puede caer (o hacer dash, atacar, recibir daño o morir).
            Assert.IsFalse(_tabla.EsValida(ESTADOS_JUGADOR.DOUBLE_JUMP, ESTADOS_JUGADOR.JUMP));
            Assert.IsFalse(_tabla.EsValida(ESTADOS_JUGADOR.DOUBLE_JUMP, ESTADOS_JUGADOR.DOUBLE_JUMP));
            Assert.IsFalse(_tabla.EsValida(ESTADOS_JUGADOR.JUMP, ESTADOS_JUGADOR.JUMP));
        }

        [Test]
        public void Agacharse_SoloDesdeElSuelo()
        {
            foreach (var estado in Todos)
            {
                var esperado = estado == ESTADOS_JUGADOR.IDLE || estado == ESTADOS_JUGADOR.RUN;
                Assert.AreEqual(esperado, _tabla.EsValida(estado, ESTADOS_JUGADOR.CROUCH), $"{estado} -> CROUCH");
            }
        }

        [Test]
        public void Diagrama_AnidaLocomocionDentroDeVivo()
        {
            var mermaid = ExportadorDiagramasFSM.GenerarMermaid(_tabla);

            var vivo = mermaid.IndexOf("state VIVO {", StringComparison.Ordinal);
            var locomocion = mermaid.IndexOf("state LOCOMOCION {", StringComparison.Ordinal);
            Assert.GreaterOrEqual(vivo, 0);
            Assert.Greater(locomocion, vivo, "LOCOMOCION debe dibujarse dentro de VIVO.");

            StringAssert.Contains("[*] --> VIVO", mermaid);
            StringAssert.Contains("LOCOMOCION --> DASH", mermaid);
            StringAssert.Contains("VIVO --> DEAD", mermaid);
            StringAssert.Contains("DEAD --> IDLE", mermaid);
            Assert.IsFalse(mermaid.Contains("IDLE --> DASH"),
                "Las salidas del superestado se dibujan una vez, desde LOCOMOCION.");
        }

        [Test]
        public void ModoMovimiento_PermiteLasSeisTransiciones()
        {
            var tabla = PlayerModoMovimiento.CrearTabla();
            var modos = (MODOS_MOVIMIENTO[])Enum.GetValues(typeof(MODOS_MOVIMIENTO));

            foreach (var desde in modos)
            {
                foreach (var hacia in modos)
                {
                    if (desde != hacia)
                    {
                        Assert.IsTrue(tabla.EsValida(desde, hacia), $"{desde} -> {hacia}");
                    }
                }
            }
        }

        [TestCase(0f, MODOS_MOVIMIENTO.STEALTH)]
        [TestCase(0.39f, MODOS_MOVIMIENTO.STEALTH)]
        [TestCase(0.4f, MODOS_MOVIMIENTO.NORMAL)]
        [TestCase(0.6f, MODOS_MOVIMIENTO.NORMAL)]
        [TestCase(0.61f, MODOS_MOVIMIENTO.RUN)]
        [TestCase(1f, MODOS_MOVIMIENTO.RUN)]
        public void ModoMovimiento_ClasificaPorFraccionDeVelocidad(float fraccion, MODOS_MOVIMIENTO esperado)
        {
            Assert.AreEqual(esperado, PlayerModoMovimiento.Clasificar(fraccion, 0.4f, 0.6f));
        }
    }
}
