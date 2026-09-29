using System.Linq;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Narrative;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La narrativa generada está enlazada a los niveles: cinemática inicial, cierres de cada
    /// mundo, diálogos que nombran la emoción y el registro emocional conectado a la interfaz.
    /// </summary>
    public class DatosNarrativaTests
    {
        private static LevelDatabase BaseDeDatos()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            Assert.IsNotNull(bd, "Falta la LevelDatabase: ejecuta Fragmentum → Regenerar todo.");
            return bd;
        }

        [Test]
        public void LaCinematicaInicial_EsLaEntradaDelNivel1_YSoloSeVeUnaVez()
        {
            var inicial = BaseDeDatos().niveles[0].cinematicaEntrada;

            Assert.IsNotNull(inicial, "Plan §3.6: la cinemática inicial es la entrada del Nivel 1.");
            Assert.AreEqual("Inicial", inicial.Id);
            Assert.IsTrue(inicial.soloPrimeraVez);
            Assert.IsTrue(inicial.saltable);
            Assert.IsNotEmpty(inicial.diapositivas);
        }

        [Test]
        public void CadaMundo_TieneCinematicaDeCierre_YDialogosQueNombranSuEmocion()
        {
            foreach (var nivel in BaseDeDatos().niveles)
            {
                Assert.IsNotNull(nivel.cinematicaCierre, $"{nivel.name} sin cinemática de cierre.");
                Assert.IsNotNull(nivel.dialogos, $"{nivel.name} sin diálogos.");

                var emocion = nivel.emocion.nombreCanonico;
                foreach (var momento in new[] { MOMENTOS_DIALOGO.APERTURA, MOMENTOS_DIALOGO.CIERRE })
                {
                    var lineas = nivel.dialogos.Del(momento);
                    Assert.IsNotEmpty(lineas, $"{nivel.name}: falta la línea de {momento}.");
                    Assert.IsTrue(lineas.Any(l => l.texto.Contains(emocion)), $"{nivel.name}: la línea de {momento} debe nombrar «{emocion}» (tarea G).");
                }

                Assert.AreEqual(nivel.jefe != null, nivel.dialogos.Del(MOMENTOS_DIALOGO.PRE_JEFE).Count > 0,
                    $"{nivel.name}: hay línea antes del jefe si y solo si el mundo tiene jefe.");
            }
        }

        [Test]
        public void ElFinal_TerminaConLaPreguntaDelAnexo()
        {
            var final = BaseDeDatos().niveles[5];

            Assert.IsNotNull(final.cinematicaEntrada, "Anexo A: el final empieza con una cinemática de confrontación.");
            Assert.IsFalse(final.cinematicaEntrada.soloPrimeraVez);
            Assert.AreEqual("¿Eres un error… o una nueva identidad?", final.cinematicaCierre.diapositivas.Last().texto);
        }

        [Test]
        public void LosSistemas_TienenDialogosYElRegistroConfigurado()
        {
            var sistemas = Resources.Load<GameObject>(Bootstrap.RutaRecurso);

            Assert.IsNotNull(sistemas.GetComponent<DialogueManager>());
            var interfaz = sistemas.GetComponent<UIManager>();
            var configuracion = new SerializedObject(interfaz).FindProperty("registro").objectReferenceValue as ConfiguracionRegistro;
            Assert.IsNotNull(configuracion, "El fin de nivel usa la configuración del registro emocional.");
            Assert.IsTrue(configuracion.activo);
            StringAssert.Contains("{emocion}", configuracion.pregunta);
        }

        [Test]
        public void ElMenuPrincipal_TieneElBotonYoReconstruido()
        {
            var escena = EditorSceneManager.OpenScene(GeneradorProgresion.RutaEscenaMenu, OpenSceneMode.Additive);
            try
            {
                var menu = escena.GetRootGameObjects().Select(r => r.GetComponentInChildren<MainMenu>(true)).FirstOrDefault(m => m != null);
                Assert.IsNotNull(menu);
                Assert.IsNotNull(new SerializedObject(menu).FindProperty("botonYoReconstruido").objectReferenceValue);
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
