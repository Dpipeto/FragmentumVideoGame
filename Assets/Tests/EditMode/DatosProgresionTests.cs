using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Comprueba que la progresión generada refleja el Anexo A y está bien configurada:
    /// orden de los mundos, nombres canónicos, fragmento → habilidad, escenas en Build Settings
    /// y sistemas persistentes listos para arrancar.
    /// </summary>
    public class DatosProgresionTests
    {
        private static LevelDatabase BaseDeDatos()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            Assert.IsNotNull(bd, "Falta la LevelDatabase: ejecuta Fragmentum → Regenerar todo.");
            return bd;
        }

        [Test]
        public void LosSeisMundos_EstanEnElOrdenDelGDD()
        {
            var bd = BaseDeDatos();
            var esperadas = new[] { EMOCIONES.IRA, EMOCIONES.TRISTEZA, EMOCIONES.MIEDO, EMOCIONES.CULPA, EMOCIONES.ESPERANZA, EMOCIONES.INTEGRACION };

            Assert.AreEqual(esperadas.Length, bd.niveles.Count);
            for (var i = 0; i < esperadas.Length; i++)
            {
                Assert.AreEqual(i + 1, bd.niveles[i].id);
                Assert.AreEqual(esperadas[i], bd.niveles[i].emocion.id, $"Nivel {i + 1}");
            }
        }

        [Test]
        public void LosNombresCanonicos_SonLosDelGDD()
        {
            var bd = BaseDeDatos();

            Assert.AreEqual("Centinela Ardiente", bd.niveles[0].jefe.nombreCanonico, "No «Pyrokratos».");
            Assert.AreEqual("Juez Silente", bd.niveles[3].jefe.nombreCanonico, "No «Juez Silencioso».");
            Assert.AreEqual("Esperanza", bd.niveles[4].emocion.nombreCanonico, "No «Aceptación».");
            Assert.IsNull(bd.niveles[4].jefe, "El Jardín Etéreo no tiene jefe.");
        }

        [Test]
        public void CadaFragmento_DesbloqueaLaHabilidadDeSuMundo()
        {
            var bd = BaseDeDatos();

            Assert.AreEqual(HABILIDADES.ATAQUE_CARGADO, bd.niveles[0].fragmento.habilidad.id);
            Assert.AreEqual(HABILIDADES.DASH_ACUATICO, bd.niveles[1].fragmento.habilidad.id);
            Assert.AreEqual(HABILIDADES.INVISIBILIDAD_PARCIAL, bd.niveles[2].fragmento.habilidad.id);
            Assert.AreEqual(HABILIDADES.DEFENSA_REFORZADA, bd.niveles[3].fragmento.habilidad.id);
            Assert.IsNull(bd.niveles[4].fragmento.habilidad, "La Esperanza integra; no da habilidad nueva.");
        }

        [Test]
        public void LasReglasDeNivel_SiguenElAnexo()
        {
            var bd = BaseDeDatos();

            Assert.IsTrue(bd.niveles[2].reglas.sigilo && bd.niveles[2].reglas.oscuridad, "El Miedo tiene sigilo y oscuridad.");
            Assert.AreEqual(3f, bd.niveles[2].reglas.radioLuzJugador);
            Assert.IsTrue(bd.niveles[3].reglas.agacharse, "La Culpa tiene cobertura.");
            Assert.IsFalse(bd.niveles[0].reglas.agacharse);
        }

        [Test]
        public void LaBaseDeDatos_EsValida_YSusEscenasEstanEnBuildSettings()
        {
            var bd = BaseDeDatos();

            CollectionAssert.IsEmpty(bd.Validar(), string.Join("\n", bd.Validar()));
            Assert.IsTrue(bd.escenaMenu.EnBuild);
            Assert.IsTrue(bd.escenaSelector.EnBuild);
            Assert.IsTrue(bd.niveles[0].Jugable, "El Nivel 1 (Ira) tiene escena.");
            Assert.AreEqual(bd.escenaMenu.Ruta, EditorBuildSettings.scenes[0].path, "El menú es la primera escena de la build.");
        }

        [Test]
        public void LosSistemasPersistentes_ApuntanALaBaseDeDatos()
        {
            var sistemas = Resources.Load<GameObject>(Bootstrap.RutaRecurso);
            Assert.IsNotNull(sistemas, "Falta Resources/Sistemas.prefab.");

            var gestor = sistemas.GetComponent<GameManager>();
            Assert.IsNotNull(gestor);
            Assert.IsNotNull(sistemas.GetComponent<GameStateMachine>());
            Assert.IsNotNull(sistemas.GetComponent<SceneLoader>());
            Assert.IsNotNull(sistemas.GetComponent<EconomyManager>());
            Assert.AreSame(BaseDeDatos(), gestor.BaseDeDatos);
        }
    }
}
