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
