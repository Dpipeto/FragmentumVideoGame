using System.Collections.Generic;
using Fragmentum.Core.FSM;
using NUnit.Framework;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Pruebas de la tabla de transiciones: es la función de transición de la FSM y, además,
    /// la fuente de la que se generan los diagramas de la tesis.
    /// </summary>
    public class TablaTransicionesTests
    {
        private static TablaTransiciones<ESTADOS_PRUEBA> Tabla()
        {
            return new TablaTransiciones<ESTADOS_PRUEBA>("Prueba")
                .Inicial(ESTADOS_PRUEBA.IDLE)
                .DefinirGrupo("VIVO", ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.RUN, ESTADOS_PRUEBA.DASH)
                .Permitir(ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.RUN, "hay input")
                .PermitirDesdeGrupo("VIVO", ESTADOS_PRUEBA.DEAD, "vida 0");
        }

        [Test]
        public void Permitir_SoloValidaLaTransicionDeclarada()
        {
            var tabla = Tabla();

            Assert.IsTrue(tabla.EsValida(ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.RUN));
            Assert.IsFalse(tabla.EsValida(ESTADOS_PRUEBA.RUN, ESTADOS_PRUEBA.IDLE),
                "La tabla no es simétrica: la vuelta hay que declararla aparte.");
        }

        [Test]
        public void PermitirDesdeGrupo_ExpandeATodosLosMiembros()
        {
            var tabla = Tabla();

            Assert.IsTrue(tabla.EsValida(ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.DEAD));
            Assert.IsTrue(tabla.EsValida(ESTADOS_PRUEBA.RUN, ESTADOS_PRUEBA.DEAD));
            Assert.IsTrue(tabla.EsValida(ESTADOS_PRUEBA.DASH, ESTADOS_PRUEBA.DEAD));
            Assert.IsFalse(tabla.EsValida(ESTADOS_PRUEBA.DEAD, ESTADOS_PRUEBA.DEAD),
                "Un grupo no genera transiciones de un estado a sí mismo.");
        }

        [Test]
        public void PermitirDesdeTodos_CubreTodaLaEnumeracionSalvoLasExcepciones()
        {
            var tabla = new TablaTransiciones<ESTADOS_PRUEBA>("Prueba")
                .PermitirDesdeTodos(ESTADOS_PRUEBA.DEAD, "muerte instantánea", ESTADOS_PRUEBA.DASH);

            Assert.IsTrue(tabla.EsValida(ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.DEAD));
            Assert.IsTrue(tabla.EsValida(ESTADOS_PRUEBA.RUN, ESTADOS_PRUEBA.DEAD));
            Assert.IsFalse(tabla.EsValida(ESTADOS_PRUEBA.DASH, ESTADOS_PRUEBA.DEAD));
        }

        [Test]
        public void Transiciones_NoSeDuplican()
        {
            var tabla = new TablaTransiciones<ESTADOS_PRUEBA>("Prueba")
                .Permitir(ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.RUN, "hay input")
                .Permitir(ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.RUN, "duplicada");

            Assert.AreEqual(1, tabla.Transiciones.Count);
        }

        [Test]
        public void PermitirDesdeGrupo_SinDefinirElGrupo_Falla()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new TablaTransiciones<ESTADOS_PRUEBA>("Prueba")
                    .PermitirDesdeGrupo("NO_EXISTE", ESTADOS_PRUEBA.DEAD));
        }

        [Test]
        public void VistaLegible_DevuelveEstadoInicialGruposYTransiciones()
        {
            ITablaLegible legible = Tabla();

            Assert.AreEqual("IDLE", legible.EstadoInicial);

            var grupos = new List<(string grupo, IReadOnlyList<string> estados)>(legible.GruposLegibles);
            Assert.AreEqual(1, grupos.Count);
            Assert.AreEqual("VIVO", grupos[0].grupo);
            CollectionAssert.AreEquivalent(new[] { "IDLE", "RUN", "DASH" }, grupos[0].estados);

            var transiciones = new List<(string desde, string hacia, string condicion, string grupoOrigen)>(
                legible.TransicionesLegibles);
            Assert.AreEqual(4, transiciones.Count, "1 suelta + 3 expandidas desde el grupo.");
        }
    }
}
