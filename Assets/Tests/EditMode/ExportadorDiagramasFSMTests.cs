using Fragmentum.Core.FSM;
using Fragmentum.EditorTools;
using NUnit.Framework;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Pruebas del exportador de diagramas. Garantizan que el Mermaid sale de la tabla de
    /// transiciones, que es lo que permite afirmar en la tesis que los diagramas
    /// corresponden al código.
    /// </summary>
    public class ExportadorDiagramasFSMTests
    {
        private static ITablaLegible TablaDePrueba()
        {
            return new TablaTransiciones<ESTADOS_PRUEBA>("Prueba")
                .Inicial(ESTADOS_PRUEBA.IDLE)
                .DefinirGrupo("VIVO", ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.RUN)
                .Permitir(ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.RUN, "hay input")
                .Permitir(ESTADOS_PRUEBA.RUN, ESTADOS_PRUEBA.IDLE, "sin input")
                .PermitirDesdeGrupo("VIVO", ESTADOS_PRUEBA.DEAD, "vida 0");
        }

        [Test]
        public void GenerarMermaid_EmpiezaPorLaCabeceraYElEstadoInicial()
        {
            var mermaid = ExportadorDiagramasFSM.GenerarMermaid(TablaDePrueba());

            StringAssert.StartsWith("stateDiagram-v2", mermaid);
            StringAssert.Contains("[*] --> VIVO", mermaid);
        }

        [Test]
        public void GenerarMermaid_DibujaElGrupoComoEstadoCompuesto()
        {
            var mermaid = ExportadorDiagramasFSM.GenerarMermaid(TablaDePrueba());

            StringAssert.Contains("state VIVO {", mermaid);
            StringAssert.Contains("IDLE --> RUN : hay input", mermaid);
            StringAssert.Contains("RUN --> IDLE : sin input", mermaid);
        }

        [Test]
        public void GenerarMermaid_DibujaUnaSolaFlechaDesdeElSuperestado()
        {
            var mermaid = ExportadorDiagramasFSM.GenerarMermaid(TablaDePrueba());

            StringAssert.Contains("VIVO --> DEAD : vida 0", mermaid);
            Assert.IsFalse(mermaid.Contains("IDLE --> DEAD"),
                "Las transiciones declaradas por grupo se dibujan una vez, desde el grupo.");
        }

        [Test]
        public void GenerarMermaid_AnidaUnGrupoContenidoEnOtro()
        {
            var tabla = new TablaTransiciones<ESTADOS_PRUEBA>("Anidada")
                .Inicial(ESTADOS_PRUEBA.IDLE)
                .DefinirGrupo("VIVO", ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.RUN, ESTADOS_PRUEBA.DASH)
                .DefinirGrupo("SUELO", ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.RUN)
                .Permitir(ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.RUN, "hay input")
                .PermitirDesdeGrupo("SUELO", ESTADOS_PRUEBA.DASH, "dash")
                .Permitir(ESTADOS_PRUEBA.DASH, ESTADOS_PRUEBA.IDLE, "fin del dash")
                .PermitirDesdeGrupo("VIVO", ESTADOS_PRUEBA.DEAD, "vida 0");

            var mermaid = ExportadorDiagramasFSM.GenerarMermaid(tabla);

            var vivo = mermaid.IndexOf("state VIVO {", System.StringComparison.Ordinal);
            var suelo = mermaid.IndexOf("state SUELO {", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(vivo, 0);
            Assert.Greater(suelo, vivo, "SUELO debe escribirse dentro del bloque de VIVO.");

            // El estado inicial se alcanza bajando por los grupos: VIVO -> SUELO -> IDLE.
            StringAssert.Contains("[*] --> VIVO", mermaid);
            StringAssert.Contains("[*] --> SUELO", mermaid);
            StringAssert.Contains("[*] --> IDLE", mermaid);

            StringAssert.Contains("SUELO --> DASH : dash", mermaid);
            StringAssert.Contains("VIVO --> DEAD : vida 0", mermaid);
            Assert.IsFalse(mermaid.Contains("IDLE --> DASH"));
        }

        [Test]
        public void BuscarTablas_EncuentraLasFSMDelJugador()
        {
            var nombres = new System.Collections.Generic.List<string>();
            foreach (var (nombre, _) in ExportadorDiagramasFSM.BuscarTablas())
            {
                nombres.Add(nombre);
            }

            CollectionAssert.Contains(nombres, "Jugador");
            CollectionAssert.Contains(nombres, "JugadorModoMovimiento");
        }

        [Test]
        public void GenerarDocumento_IncluyeElBloqueMermaidYLaTabla()
        {
            var documento = ExportadorDiagramasFSM.GenerarDocumento("Prueba", TablaDePrueba());

            StringAssert.Contains("# FSM: Prueba", documento);
            StringAssert.Contains("```mermaid", documento);
            StringAssert.Contains("| Desde | Hacia | Condición |", documento);
            StringAssert.Contains("| IDLE | RUN | hay input |", documento);
        }
    }
}
