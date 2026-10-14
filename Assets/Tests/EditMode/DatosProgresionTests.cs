using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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
            Assert.IsTrue(bd.niveles[1].Jugable, "El Nivel 2 (Tristeza) también.");
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
            Assert.AreSame(BaseDeDatos(), gestor.BaseDeDatos);
        }

        // --- Fase 6 ---

        [Test]
        public void LosSistemasPersistentes_TienenInterfaz_ConLaAccionDePausa()
        {
            var sistemas = Resources.Load<GameObject>(Bootstrap.RutaRecurso);
            var interfaz = sistemas.GetComponent<UIManager>();
            Assert.IsNotNull(interfaz, "Sin UIManager no hay pausa, Game Over ni fin de nivel.");

            var acciones = new SerializedObject(interfaz).FindProperty("acciones").objectReferenceValue as InputActionAsset;
            Assert.IsNotNull(acciones, "El UIManager lee la pausa del asset de input.");
            var pausa = acciones.FindAction("Game/Pause");
            Assert.IsNotNull(pausa, "Falta la acción Game/Pause.");
            CollectionAssert.Contains(RutasDe(pausa), "<Keyboard>/escape");
            CollectionAssert.Contains(RutasDe(pausa), "<Gamepad>/start");
        }

        [Test]
        public void ElNivel1_PreveLosCheckpointsYLosTramosDeSuDiseno()
        {
            // El Anexo A pone un checkpoint en x 40; con las plataformas reales el lago creció y la
            // bandera va tras la subida, antes del Golem. Al alargar el nivel (2026-10-13) se
            // añadieron dos: tras el Golem y ante la arena (Docs/Diseno/Nivel1_Diseno.md).
            var nivel = BaseDeDatos().niveles[0];
            CollectionAssert.AreEqual(DisenoNivel1.Checkpoints, nivel.puntosDeControl);
            Assert.AreEqual(43f, DisenoNivel1.Checkpoints[0]);

            Assert.AreEqual(DisenoNivel1.Secciones.Length, nivel.secciones.Count);
            for (var i = 0; i < nivel.secciones.Count; i++)
            {
                Assert.AreEqual(DisenoNivel1.Secciones[i].Nombre, nivel.secciones[i].nombre);
                Assert.AreEqual(DisenoNivel1.Secciones[i].Desde, nivel.secciones[i].desdeX, 1e-4f);
                Assert.AreEqual(DisenoNivel1.Secciones[i].Hasta, nivel.secciones[i].hastaX, 1e-4f);
            }

            Assert.AreEqual(new Vector2(0f, DisenoNivel1.LimitesArena.y - 1f), nivel.Extension, "De ahí sale lo que enseña la cámara.");
        }

        [Test]
        public void CadaMundo_TieneElAcentoYLaCitaDeLaMaqueta()
        {
            var bd = BaseDeDatos();
            foreach (var nivel in bd.niveles)
            {
                Assert.Greater(nivel.acento.a, 0f, $"{nivel.name} sin acento.");
                Assert.IsFalse(string.IsNullOrEmpty(nivel.cita), $"{nivel.name} sin cita.");
            }

            Assert.AreEqual("FF6B35", ColorUtility.ToHtmlStringRGB(bd.niveles[0].acento));
            StringAssert.Contains("rabia", bd.niveles[0].cita);
            StringAssert.Contains("integrar", bd.niveles[5].cita);
        }

        [Test]
        public void LaEtiqueta_NumeraLosMundosEnRomanos()
        {
            var bd = BaseDeDatos();
            Assert.AreEqual("I · FORTALEZA VOLCÁNICA", bd.niveles[0].Etiqueta);
            Assert.AreEqual("VI · EL ESPEJO DEL ORIGEN", bd.niveles[5].Etiqueta);
        }

        [Test]
        public void ElColorOscuro_EsElMasOscuroDeLaPaleta()
        {
            var nivel = BaseDeDatos().niveles[0];
            Assert.AreEqual("1A0800", ColorUtility.ToHtmlStringRGB(nivel.ColorOscuro), "Ira: #1A0800 es el más oscuro del Anexo A.");
            Assert.AreEqual(new Vector2(0f, 125f), nivel.Extension, "Las secciones del Nivel 1 cubren de 0 a 125 u (80 antes de la forja).");
        }

        private static List<string> RutasDe(InputAction accion)
        {
            var rutas = new List<string>();
            foreach (var enlace in accion.bindings)
            {
                rutas.Add(enlace.path);
            }

            return rutas;
        }
    }
}
