using System;
using System.IO;
using Fragmentum.Core.Save;
using NUnit.Framework;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Pruebas del guardado: ida y vuelta completa, escritura atómica con copia de seguridad,
    /// recuperación de archivos dañados y partidas de antes de que el puntaje sustituyera a las
    /// monedas. Cubren el criterio «el progreso sobrevive al cierre y reapertura del juego».
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

        private SaveSystem Sistema() => new SaveSystem(_carpeta);

        [Test]
        public void SinArchivo_EmpiezaUnaPartidaNueva()
        {
            var datos = Sistema().Cargar();

            Assert.AreEqual(1, datos.nivelMaximoAlcanzado);
            Assert.AreEqual(0, datos.PuntajeTotal);
            Assert.IsFalse(datos.TieneProgreso);
        }

        [Test]
        public void GuardarYCargar_ConservaTodoElProgreso()
        {
            var datos = new DatosGuardado
            {
                nivelMaximoAlcanzado = 3,
                checkpoint = new DatosCheckpoint { nivelId = 3, checkpointId = "L3_CP1" },
                intento = new DatosIntento { nivelId = 3, puntos = 350, segundos = 81.5f }
            };
            datos.intento.derrotados.Add("Espectro_1@12,0");
            datos.AnotarPuntaje(1, 4200, 301.5f);
            datos.AnotarPuntaje(2, 5300, 512f);
            datos.nivelesCompletados.AddRange(new[] { 1, 2 });
            datos.fragmentos.AddRange(new[] { "IRA", "TRISTEZA" });
            datos.habilidades.AddRange(new[] { "ATAQUE_CARGADO", "DASH_ACUATICO" });
            datos.cinematicasVistas.Add("INICIAL");

            Assert.IsTrue(Sistema().Guardar(datos));
            var cargados = Sistema().Cargar();

            Assert.AreEqual(3, cargados.nivelMaximoAlcanzado);
            Assert.AreEqual(4200, cargados.PuntajeDe(1).puntos);
            Assert.AreEqual(301.5f, cargados.PuntajeDe(1).segundos, 0.001f);
            Assert.AreEqual(9500, cargados.PuntajeTotal);
            Assert.AreEqual(3, cargados.intento.nivelId);
            Assert.AreEqual(350, cargados.intento.puntos);
            Assert.AreEqual(81.5f, cargados.intento.segundos, 0.001f);
            CollectionAssert.AreEqual(new[] { "Espectro_1@12,0" }, cargados.intento.derrotados);
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
            sistema.Guardar(new DatosGuardado { nivelMaximoAlcanzado = 2 });
            sistema.Guardar(new DatosGuardado { nivelMaximoAlcanzado = 3 });

            Assert.IsTrue(File.Exists(sistema.RutaCopia), "El guardado anterior debe quedar como .bak.");
            StringAssert.Contains("\"nivelMaximoAlcanzado\": 2", File.ReadAllText(sistema.RutaCopia));
            Assert.IsFalse(File.Exists(sistema.Ruta + ".tmp"), "No debe quedar el archivo temporal.");
            Assert.AreEqual(3, sistema.Cargar().nivelMaximoAlcanzado);
        }

        [Test]
        public void ArchivoDanado_SeRecuperaLaCopia()
        {
            var sistema = Sistema();
            sistema.Guardar(new DatosGuardado { nivelMaximoAlcanzado = 4 });
            sistema.Guardar(new DatosGuardado { nivelMaximoAlcanzado = 5 });
            File.WriteAllText(sistema.Ruta, "{ esto no es json");

            Assert.AreEqual(4, sistema.Cargar().nivelMaximoAlcanzado, "Debe recuperar la copia de seguridad.");
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
        public void UnaPartidaDeCuandoHabiaMonedas_SeCargaIgual_YSinPuntaje()
        {
            var sistema = Sistema();
            File.WriteAllText(sistema.Ruta,
                "{\"version\":1,\"nivelMaximoAlcanzado\":2,\"nivelesCompletados\":[1],\"monedas\":12,\"migradoDePlayerPrefs\":true}");

            var datos = sistema.Cargar();

            Assert.AreEqual(2, datos.nivelMaximoAlcanzado);
            CollectionAssert.AreEqual(new[] { 1 }, datos.nivelesCompletados);
            Assert.IsTrue(datos.TieneProgreso);
            Assert.AreEqual(0, datos.PuntajeTotal, "Las monedas no se convierten en puntos.");
            Assert.IsNull(datos.PuntajeDe(1), "El nivel superado antes de que hubiera puntaje no tiene récord todavía.");
            Assert.AreEqual(0, datos.intento.nivelId);
        }

        [Test]
        public void JsonIncompleto_SeNormalizaSinListasNulas()
        {
            var sistema = Sistema();
            File.WriteAllText(sistema.Ruta, "{\"version\":1,\"puntajes\":null,\"intento\":null}");

            var datos = sistema.Cargar();

            Assert.IsNotNull(datos.puntajes);
            Assert.IsNotNull(datos.intento);
            Assert.IsNotNull(datos.intento.derrotados);
            Assert.IsNotNull(datos.fragmentos);
            Assert.IsNotNull(datos.checkpoint);
            Assert.AreEqual(1, datos.nivelMaximoAlcanzado);
        }
    }
}
