using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Enemies.Deteccion;
using NUnit.Framework;
using UnityEditor;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Comprueba que los datos generados reflejan el Anexo A: que cada variante encarna su
    /// emoción con valores distintos. Si alguien retoca un asset y rompe esa correspondencia
    /// (por ejemplo, un Lamento más rápido que un Soldado de lava), la prueba lo señala.
    /// </summary>
    public class DatosEnemigosTests
    {
        private static EnemyData Cargar(string nombre)
        {
            var datos = GeneradorEnemigos.CargarDatos(nombre);
            Assert.IsNotNull(datos, $"Falta Assets/Data/Enemigos/{nombre}.asset: ejecuta Fragmentum → Regenerar todo.");
            return datos;
        }

        [Test]
        public void Ira_EsMasAgresivaQueLaReferencia()
        {
            var ira = Cargar("SoldadoLava");
            var generico = Cargar("Generico");

            Assert.Greater(ira.velocidadPersecucion, generico.velocidadPersecucion);
            Assert.Less(ira.attackCooldown, generico.attackCooldown);
            Assert.Greater(ira.detectionRadius, generico.detectionRadius);
        }

        [Test]
        public void Tristeza_EsLentaYPesada()
        {
            var tristeza = Cargar("Lamento");
            var generico = Cargar("Generico");

            Assert.Less(tristeza.velocidadPersecucion, generico.velocidadPersecucion);
            Assert.Greater(tristeza.esperaEnWaypoint, generico.esperaEnWaypoint);
            Assert.Greater(tristeza.resistenciaRetroceso, generico.resistenciaRetroceso);
        }

        [Test]
        public void GolemDeCeniza_TieneVida10ComoPideElAnexo()
        {
            var golem = Cargar("GolemCeniza");

            Assert.AreEqual(10, golem.vidaMaxima);
            Assert.Greater(golem.resistenciaRetroceso, 0.5f, "Es pesado: apenas retrocede.");
        }

        [Test]
        public void SoloElGolemDeCeniza_EsInmuneALaLava()
        {
            Assert.IsTrue(Cargar("GolemCeniza").inmuneALava, "Decisión del diseño (PENDIENTES N9): el Golem no muere en la lava.");
            foreach (var otro in new[] { "Generico", "SoldadoLava", "EcoIra", "Lamento", "Escultura", "Planidera", "Espectro", "CentinelaCiego" })
            {
                Assert.IsFalse(Cargar(otro).inmuneALava, $"{otro} sí muere en la lava.");
            }
        }

        [Test]
        public void ElEcoDeLaIra_AtacaMasDeSeguidoQueAntes()
        {
            var eco = Cargar("EcoIra");
            Assert.Less(eco.attackCooldown, 0.7f, "Tras la primera prueba el Eco descansa menos entre golpes.");
            Assert.Less(eco.duracionAtaque, 0.73f, "Y el golpe sin animación también es más corto.");
            Assert.Less(eco.inicioVentanaGolpe, eco.finVentanaGolpe);
            Assert.LessOrEqual(eco.finVentanaGolpe, eco.duracionAtaque);
        }

        [Test]
        public void Miedo_UsaEstrategiasDeDeteccionPropias()
        {
            Assert.IsInstanceOf<DeteccionEnOscuridad>(Cargar("Espectro").deteccion);
            Assert.IsInstanceOf<DeteccionPorRuido>(Cargar("CentinelaCiego").deteccion);
            Assert.IsInstanceOf<DeteccionConVision>(Cargar("SoldadoLava").deteccion);
        }

        [Test]
        public void TodasLasVariantes_TienenPrefab()
        {
            foreach (var variante in GeneradorEnemigos.Variantes)
            {
                Assert.IsNotNull(GeneradorEnemigos.CargarPrefab(variante), $"Falta el prefab de {variante}.");
            }

            Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(GeneradorEnemigos.RutaPrefabBase));
        }
    }
}
