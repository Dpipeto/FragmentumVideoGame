using System.Collections.Generic;
using Fragmentum.Data;
using Fragmentum.Narrative;
using NUnit.Framework;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Los datos de la narrativa se comportan como espera el resto del juego: una cinemática sin
    /// arte dice qué falta, los diálogos se filtran por momento y la pregunta del registro nombra
    /// la emoción del mundo.
    /// </summary>
    public class NarrativaTests
    {
        private readonly List<Object> _creados = new List<Object>();

        [TearDown]
        public void Limpiar()
        {
            foreach (var objeto in _creados)
            {
                Object.DestroyImmediate(objeto);
            }

            _creados.Clear();
        }

        [Test]
        public void UnaCinematicaSinTexto_DiceQueFalta()
        {
            var cinematica = Crear<CutsceneData>();
            cinematica.id = "Inicial";
            cinematica.diapositivas.Add(new Diapositiva { texto = "", duracion = 2f });
            cinematica.diapositivas.Add(new Diapositiva { texto = "Hola", duracion = 3f });

            StringAssert.Contains("PENDIENTE", cinematica.TextoDe(0));
            StringAssert.Contains("Inicial", cinematica.TextoDe(0));
            Assert.AreEqual("Hola", cinematica.TextoDe(1));
            Assert.AreEqual(5f, cinematica.DuracionTotal, 0.001f);
            Assert.IsTrue(cinematica.FaltaArte, "Sin imágenes: se avisa y se muestra en negro.");
        }

        [Test]
        public void SinId_LaCinematicaUsaSuNombre()
        {
            var cinematica = Crear<CutsceneData>();
            cinematica.name = "Cinematica_Prueba";
            Assert.AreEqual("Cinematica_Prueba", cinematica.Id);
        }

        [Test]
        public void LosDialogos_SeFiltranPorMomento_YSeSaltanLasLineasVacias()
        {
            var dialogos = Crear<DialogueData>();
            dialogos.lineas.Add(new LineaDialogo { momento = MOMENTOS_DIALOGO.APERTURA, texto = "a" });
            dialogos.lineas.Add(new LineaDialogo { momento = MOMENTOS_DIALOGO.CIERRE, texto = "c" });
            dialogos.lineas.Add(new LineaDialogo { momento = MOMENTOS_DIALOGO.APERTURA, texto = "b" });
            dialogos.lineas.Add(new LineaDialogo { momento = MOMENTOS_DIALOGO.APERTURA, texto = "  " });

            var apertura = dialogos.Del(MOMENTOS_DIALOGO.APERTURA);

            Assert.AreEqual(2, apertura.Count);
            Assert.AreEqual("a", apertura[0].texto);
            Assert.AreEqual("b", apertura[1].texto, "En el orden del guion.");
            Assert.IsEmpty(dialogos.Del(MOMENTOS_DIALOGO.PRE_JEFE));
        }

        [Test]
        public void LaPreguntaDelRegistro_NombraLaEmocionDelMundo()
        {
            var config = Crear<ConfiguracionRegistro>();
            var emocion = Crear<EmocionData>();
            emocion.nombreCanonico = "Miedo";
            var nivel = Crear<LevelData>();
            nivel.emocion = emocion;

            Assert.AreEqual("¿Con qué intensidad sentiste miedo en este mundo?", config.PreguntaPara(nivel));
        }

        private T Crear<T>() where T : ScriptableObject
        {
            var objeto = ScriptableObject.CreateInstance<T>();
            _creados.Add(objeto);
            return objeto;
        }
    }
}
