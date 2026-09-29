using System;
using System.IO;
using System.Text.RegularExpressions;
using Fragmentum.Data;
using Fragmentum.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El registro emocional guarda en local, en JSON y CSV, sin perder respuestas y sin datos
    /// personales más allá del código de participante opcional.
    /// </summary>
    public class RegistroEmocionalTests
    {
        private string _carpeta;
        private LevelData _nivel;
        private EmocionData _emocion;

        [SetUp]
        public void Preparar()
        {
            _carpeta = Path.Combine(Path.GetTempPath(), "FragmentumRegistro", Guid.NewGuid().ToString("N"));
            _emocion = ScriptableObject.CreateInstance<EmocionData>();
            _emocion.nombreCanonico = "Ira";
            _nivel = ScriptableObject.CreateInstance<LevelData>();
            _nivel.id = 1;
            _nivel.nombreMundo = "Fortaleza Volcánica";
            _nivel.emocion = _emocion;
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(_nivel);
            Object.DestroyImmediate(_emocion);
            if (Directory.Exists(_carpeta))
            {
                Directory.Delete(_carpeta, true);
            }
        }

        [Test]
        public void Registrar_GuardaElJsonYElCsv()
        {
            var registro = new RegistroEmocional(_carpeta);

            var entrada = registro.Registrar(_nivel, 4, "Me costó el jefe", new DateTime(2026, 9, 29, 18, 4, 11));

            Assert.IsTrue(File.Exists(registro.RutaJson));
            Assert.IsTrue(File.Exists(registro.RutaCsv));
            Assert.AreEqual("2026-09-29T18:04:11", entrada.fecha);
            Assert.AreEqual("Ira", entrada.emocion);

            var csv = File.ReadAllText(registro.RutaCsv);
            StringAssert.StartsWith("fecha;participante;nivel;mundo;emocion;valor;texto", csv.TrimStart('﻿'));
            StringAssert.Contains("2026-09-29T18:04:11;;1;Fortaleza Volcánica;Ira;4;Me costó el jefe", csv);
        }

        [Test]
        public void LasRespuestas_SobrevivenAlCerrarElJuego()
        {
            var registro = new RegistroEmocional(_carpeta) { Participante = "  P-07 " };
            registro.Registrar(_nivel, 2, "");
            registro.Registrar(_nivel, 5, "otra vez");

            var releido = new RegistroEmocional(_carpeta);

            Assert.AreEqual(2, releido.Entradas.Count);
            Assert.AreEqual("P-07", releido.Participante, "El código se guarda sin espacios.");
            Assert.AreEqual("P-07", releido.Entradas[1].participante);
            Assert.AreEqual(5, releido.Entradas[1].valor);
        }

        [TestCase(0, 1)]
        [TestCase(9, 5)]
        public void ElValor_SeLimitaALaEscala(int dado, int guardado)
        {
            var registro = new RegistroEmocional(_carpeta);
            Assert.AreEqual(guardado, registro.Registrar(_nivel, dado, "").valor);
        }

        [Test]
        public void ElCsv_EscapaPuntoYComaComillasYSaltos()
        {
            var datos = new DatosRegistro();
            datos.entradas.Add(new EntradaRegistro { fecha = "f", nivel = 1, valor = 3, texto = "uno; \"dos\"\ntres" });

            var csv = RegistroEmocional.ACsv(datos);

            StringAssert.Contains("\"uno; \"\"dos\"\" tres\"", csv, "Una fila por respuesta, aunque el comentario tenga saltos.");
            Assert.AreEqual(2, csv.Split('\n').Length - 1, "Cabecera y una fila.");
        }

        [Test]
        public void UnArchivoDanado_SeConserva_YSeEmpiezaUnoNuevo()
        {
            Directory.CreateDirectory(_carpeta);
            var ruta = Path.Combine(_carpeta, RegistroEmocional.NombreJson);
            File.WriteAllText(ruta, "{ esto no es json");

            LogAssert.Expect(LogType.Warning, new Regex("dañado"));
            var registro = new RegistroEmocional(_carpeta);

            Assert.AreEqual(0, registro.Entradas.Count);
            Assert.IsTrue(File.Exists(ruta + ".danado"), "El archivo dañado no se pierde.");
        }
    }
}
