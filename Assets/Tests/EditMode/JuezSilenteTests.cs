using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El Juez Silente (fase D), sin jugar: sus datos (inmune salvo en su ventana de 3 s, dos fases),
    /// cada fase con sus cuatro memorias, cada ataque esquivable como dice el documento, y su verificador
    /// en verde. Lo que pasa jugando, en <c>JuezPlayModeTests</c>; la plaza, en <c>EscenaNivel4Tests</c>.
    /// </summary>
    public class JuezSilenteTests
    {
        private static readonly ParametrosJugador Kael = new ParametrosJugador();

        private static float AlturaSalto
        {
            get
            {
                var gravedad = Mathf.Abs(Physics2D.gravity.y) * Kael.escalaGravedad;
                return Kael.velocidadSalto * Kael.velocidadSalto / (2f * gravedad);
            }
        }

        [Test]
        public void SusDatos_InmuneSalvoEnSuVentana_YDosFasesConLasCuatroMemorias()
        {
            var datos = GeneradorJefes.CargarDatos(VerificadorJuez.NombreDatos);
            Assert.IsNotNull(datos);
            Assert.AreEqual(28, datos.vidaMaxima, "28 de vida (36 si no se miró ninguna memoria).");
            Assert.IsTrue(datos.inmuneFueraDeVulnerable, "Ningún golpe le hace nada (Anexo A: inmune a ataques directos)…");
            Assert.AreEqual(3f, datos.duracionVulnerable, 1e-4f, "…salvo arrodillado, 3 s.");
            Assert.AreEqual(0, datos.danioContacto, "Tocarlo no hace daño: lo hacen sus ataques.");
            Assert.IsFalse(GeneradorJefes.EsEsqueleto(datos), "Ya no es un esqueleto: tiene sus patrones.");
            Assert.AreEqual(2, datos.fases.Count);
            Assert.AreEqual(0.5f, datos.fases[1].umbralVida, 1e-4f, "La segunda, al 50 %.");

            foreach (var fase in datos.fases)
            {
                var memorias = fase.patrones.Cast<PatronMemoria>().Select(p => p.memoria).ToList();
                CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, memorias, $"{fase.nombre}: cada ataque es una memoria, las cuatro.");
            }

            var primera = datos.fases[0].patrones.Cast<PatronMemoria>().Select(p => p.memoria).ToList();
            var segunda = datos.fases[1].patrones.Cast<PatronMemoria>().Select(p => p.memoria).ToList();
            CollectionAssert.AreNotEqual(primera, segunda, "En la segunda fase, en otro orden.");
            CollectionAssert.IsEmpty(VerificadorJuez.Problemas(), string.Join("\n", VerificadorJuez.Problemas()));
        }

        [Test]
        public void CadaAtaque_AvisaUnSegundo_YSeEsquivaComoDiceElDocumento()
        {
            var datos = GeneradorJefes.CargarDatos(VerificadorJuez.NombreDatos);
            var patrones = datos.fases[0].patrones;
            Assert.IsTrue(patrones.All(p => Mathf.Approximately(p.aviso, 1f)), "Cada ataque avisa 1 s antes de hacer daño.");

            var orden = patrones.OfType<PatronOrden>().Single();
            Assert.AreEqual(6, orden.explosiones, "La orden: seis explosiones…");
            Assert.AreEqual(0.8f, orden.marca, 1e-4f, "…cada una marcada 0,8 s antes.");
            Assert.AreEqual(1, orden.danio);

            var pueblo = patrones.OfType<PatronPueblo>().Single();
            Assert.AreEqual(2, pueblo.olas, "El pueblo: una ola desde cada lado…");
            Assert.Less(pueblo.alto, AlturaSalto * 0.9f, "…que se salta.");

            var companero = patrones.OfType<PatronCompanero>().Single();
            Assert.AreEqual(3, companero.rafagas, "El compañero: tres ráfagas de placas.");
            Assert.Greater(companero.alturaSalida, Fragmentum.EditorTools.DisenoNivel4.AltoKael,
                "Salen de más arriba de la cabeza de Kael: agachado al descubierto, también le dan.");

            var sentencia = patrones.OfType<PatronSentencia>().Single();
            Assert.AreEqual(2, sentencia.golpes, "La carta: dos golpes…");
            Assert.AreEqual(2, sentencia.danio, "…que quitan 2: el ataque que más quita.");
            Assert.Less(sentencia.altoOnda, AlturaSalto * 0.9f, "Su onda se salta.");

            var prefab = GeneradorJefes.CargarPrefab(datos);
            var juez = prefab.GetComponent<JuezSilente>();
            Assert.AreEqual(1f, juez.sujecionTrampa, 1e-4f, "La trampa sujeta 1 s…");
            Assert.AreEqual(1, juez.danioTrampa, "…y quita 1.");
            Assert.AreEqual(1.3f, juez.vidaSinMemorias, 1e-4f, "Sin memorias, un 30 % más de vida.");
            Assert.AreEqual(36, Mathf.RoundToInt(datos.vidaMaxima * juez.vidaSinMemorias));
        }
    }
}
