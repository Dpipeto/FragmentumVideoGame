using System;
using System.IO;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.EditorTools;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Las opciones de sonido (<see cref="AjustesAudio"/>): todo empieza al 100 %, se queda entre 0 y 1, se
    /// guarda en su archivo y se lee al volver; si el archivo está roto, todo a su volumen, y en uno de antes
    /// de que hubiera efectos, los efectos al 100 %. Y el menú principal tiene su botón «Opciones».
    /// </summary>
    public class OpcionesTests
    {
        private string _carpeta;

        [SetUp]
        public void Preparar()
        {
            _carpeta = Path.Combine(Path.GetTempPath(), "FragmentumPruebas", Guid.NewGuid().ToString("N"));
            AjustesAudio.UsarCarpeta(_carpeta);
        }

        [TearDown]
        public void Limpiar()
        {
            AjustesAudio.UsarCarpeta(null);
            if (Directory.Exists(_carpeta))
            {
                Directory.Delete(_carpeta, true);
            }
        }

        [Test]
        public void SinArchivo_TodoSuenaASuVolumen()
        {
            Assert.AreEqual(1f, AjustesAudio.General);
            Assert.AreEqual(1f, AjustesAudio.Musica);
            Assert.AreEqual(1f, AjustesAudio.Efectos);
            Assert.AreEqual(1f, AudioListener.volume);
        }

        [Test]
        public void SeGuarda_YSeLeeAlVolver()
        {
            AjustesAudio.General = 0.4f;
            AjustesAudio.Musica = 0.7f;
            AjustesAudio.Efectos = 0.3f;
            Assert.AreEqual(0.4f, AudioListener.volume, 1e-4f);
            FileAssert.Exists(Path.Combine(_carpeta, AjustesAudio.NombreArchivo));

            AjustesAudio.UsarCarpeta(_carpeta); // como al arrancar otra vez
            Assert.AreEqual(0.4f, AjustesAudio.General, 1e-4f);
            Assert.AreEqual(0.7f, AjustesAudio.Musica, 1e-4f);
            Assert.AreEqual(0.3f, AjustesAudio.Efectos, 1e-4f);
            Assert.AreEqual(0.4f, AudioListener.volume, 1e-4f, "Al arrancar, el general ya se aplica.");
        }

        [Test]
        public void UnArchivoDeAntesDeLosEfectos_LosDejaAl100()
        {
            Directory.CreateDirectory(_carpeta);
            File.WriteAllText(Path.Combine(_carpeta, AjustesAudio.NombreArchivo), "{ \"general\": 0.4, \"musica\": 0.7 }");
            AjustesAudio.UsarCarpeta(_carpeta);
            Assert.AreEqual(0.4f, AjustesAudio.General, 1e-4f);
            Assert.AreEqual(0.7f, AjustesAudio.Musica, 1e-4f);
            Assert.AreEqual(1f, AjustesAudio.Efectos, "Lo guardado en la v0.65, sin efectos: a su volumen.");
        }

        [Test]
        public void SeQuedaEntreCeroYUno_YAvisaAlCambiar()
        {
            var avisos = 0;
            void Contar() => avisos++;
            AjustesAudio.Cambiaron += Contar;
            try
            {
                AjustesAudio.Musica = 3f;
                Assert.AreEqual(1f, AjustesAudio.Musica, "Ya estaba al 100 %: no cambia, no avisa.");
                AjustesAudio.Musica = -1f;
                Assert.AreEqual(0f, AjustesAudio.Musica);
                Assert.AreEqual(1, avisos);
            }
            finally
            {
                AjustesAudio.Cambiaron -= Contar;
            }
        }

        [Test]
        public void ConElArchivoRoto_TodoSuenaASuVolumen()
        {
            Directory.CreateDirectory(_carpeta);
            File.WriteAllText(Path.Combine(_carpeta, AjustesAudio.NombreArchivo), "{ esto no es json");
            AjustesAudio.UsarCarpeta(_carpeta);
            Assert.AreEqual(1f, AjustesAudio.General);
            Assert.AreEqual(1f, AjustesAudio.Musica);
        }

        [Test]
        public void ElPorcentaje_SeLeeEnEntero()
        {
            Assert.AreEqual("70 %", PantallaOpciones.Porcentaje(0.7f));
            Assert.AreEqual("0 %", PantallaOpciones.Porcentaje(0f));
        }

        [Test]
        public void ElMenuPrincipal_TieneElBotonOpciones()
        {
            var escena = EditorSceneManager.OpenScene(GeneradorProgresion.RutaEscenaMenu, OpenSceneMode.Additive);
            try
            {
                var menu = escena.GetRootGameObjects().Select(r => r.GetComponentInChildren<MainMenu>(true)).FirstOrDefault(m => m != null);
                Assert.IsNotNull(menu);
                Assert.IsNotNull(new SerializedObject(menu).FindProperty("botonOpciones").objectReferenceValue,
                    "Regenera el menú: Fragmentum → Progresión → Crear escenas de menú y selector.");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
