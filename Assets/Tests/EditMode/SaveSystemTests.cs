using System;
using System.IO;
using Fragmentum.Core.Save;
using NUnit.Framework;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Pruebas del guardado: ida y vuelta completa, escritura atómica con copia de seguridad,
    /// recuperación de archivos dañados y migración desde los PlayerPrefs de la v1. Cubren el
    /// criterio «el progreso sobrevive al cierre y reapertura del juego».
    /// </summary>
    public class SaveSystemTests
    {
        private string _carpeta;

        [SetUp]
        public void Preparar()
        {
            _carpeta = Path.Combine(Path.GetTempPath(), "FragmentumPruebas", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_carpeta);
        }

        [TearDown]
        public void Limpiar()
        {
            if (Directory.Exists(_carpeta))
            {
                Directory.Delete(_carpeta, true);
            }
        }

        private SaveSystem Sistema(int monedasV1 = -1) => new SaveSystem(_carpeta, new MigracionPlayerPrefs(
            clave => monedasV1 >= 0 && clave == MigracionPlayerPrefs.ClaveMonedas,
            _ => monedasV1));

        [Test]
        public void SinArchivo_EmpiezaUnaPartidaNueva()
        {
            var datos = Sistema().Cargar();

            Assert.AreEqual(1, datos.nivelMaximoAlcanzado);
            Assert.AreEqual(0, datos.monedas);
            Assert.IsFalse(datos.TieneProgreso);
        }

        [Test]
        public void GuardarYCargar_ConservaTodoElProgreso()
        {
            var datos = new DatosGuardado
            {
                nivelMaximoAlcanzado = 3,
                monedas = 12,
                checkpoint = new DatosCheckpoint { nivelId = 3, checkpointId = "L3_CP1" }
            };
            datos.nivelesCompletados.AddRange(new[] { 1, 2 });
            datos.fragmentos.AddRange(new[] { "IRA", "TRISTEZA" });
            datos.habilidades.AddRange(new[] { "ATAQUE_CARGADO", "DASH_ACUATICO" });
            datos.cinematicasVistas.Add("INICIAL");

            Assert.IsTrue(Sistema().Guardar(datos));
            var cargados = Sistema().Cargar();

            Assert.AreEqual(3, cargados.nivelMaximoAlcanzado);
            Assert.AreEqual(12, cargados.monedas);
            CollectionAssert.AreEqual(new[] { 1, 2 }, cargados.nivelesCompletados);
            CollectionAssert.AreEqual(new[] { "IRA", "TRISTEZA" }, cargados.fragmentos);
            CollectionAssert.AreEqual(new[] { "ATAQUE_CARGADO", "DASH_ACUATICO" }, cargados.habilidades);
            Assert.AreEqual("L3_CP1", cargados.checkpoint.checkpointId);
            CollectionAssert.Contains(cargados.cinematicasVistas, "INICIAL");
        }

        [Test]
        public void Guardar_DejaLaVersionAnteriorComoCopia_YNingunTemporal()
        {
            var sistema = Sistema();
            sistema.Guardar(new DatosGuardado { monedas = 1 });
            sistema.Guardar(new DatosGuardado { monedas = 2 });

            Assert.IsTrue(File.Exists(sistema.RutaCopia), "El guardado anterior debe quedar como .bak.");
            StringAssert.Contains("\"monedas\": 1", File.ReadAllText(sistema.RutaCopia));
            Assert.IsFalse(File.Exists(sistema.Ruta + ".tmp"), "No debe quedar el archivo temporal.");
            Assert.AreEqual(2, sistema.Cargar().monedas);
        }

        [Test]
        public void ArchivoDanado_SeRecuperaLaCopia()
        {
            var sistema = Sistema();
            sistema.Guardar(new DatosGuardado { monedas = 7 });
            sistema.Guardar(new DatosGuardado { monedas = 9 });
            File.WriteAllText(sistema.Ruta, "{ esto no es json");

            Assert.AreEqual(7, sistema.Cargar().monedas, "Debe recuperar la copia de seguridad.");
        }

        [Test]
        public void ArchivoYCopiaDanados_EmpiezaDeCeroSinExcepciones()
        {
            var sistema = Sistema();
            File.WriteAllText(sistema.Ruta, "{ roto");
            File.WriteAllText(sistema.RutaCopia, "tampoco");

            DatosGuardado datos = null;
            Assert.DoesNotThrow(() => datos = sistema.Cargar());
            Assert.IsNotNull(datos);
            Assert.AreEqual(1, datos.nivelMaximoAlcanzado);
            Assert.IsTrue(File.Exists(sistema.Ruta), "El archivo dañado no se borra: se conserva para revisarlo.");
        }

        [Test]
        public void SinPartida_ImportaLasMonedasDeLaV1()
        {
            var datos = Sistema(monedasV1: 25).Cargar();

            Assert.AreEqual(25, datos.monedas);
            Assert.IsTrue(datos.migradoDePlayerPrefs);
        }

        [Test]
        public void ConPartidaGuardada_NoVuelveAImportarLaV1()
        {
            Sistema().Guardar(new DatosGuardado { monedas = 3 });

            Assert.AreEqual(3, Sistema(monedasV1: 25).Cargar().monedas);
        }

        [Test]
        public void JsonIncompleto_SeNormalizaSinListasNulas()
        {
            var sistema = Sistema();
            File.WriteAllText(sistema.Ruta, "{\"version\":1,\"monedas\":4}");

            var datos = sistema.Cargar();

            Assert.AreEqual(4, datos.monedas);
            Assert.IsNotNull(datos.fragmentos);
            Assert.IsNotNull(datos.checkpoint);
            Assert.AreEqual(1, datos.nivelMaximoAlcanzado);
        }
    }
}
