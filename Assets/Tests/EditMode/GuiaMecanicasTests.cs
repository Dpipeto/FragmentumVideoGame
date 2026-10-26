using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Save;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La pantalla «Mecánicas»: explica lo que sabe hacer Kael con las teclas del asset de input,
    /// y las habilidades de los fragmentos aparecen según se integran.
    /// </summary>
    public class GuiaMecanicasTests
    {
        private static InputActionAsset Acciones()
        {
            var acciones = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ConstructorJugador.RutaAccionesInput);
            Assert.IsNotNull(acciones, $"Falta {ConstructorJugador.RutaAccionesInput}.");
            return acciones;
        }

        private static LevelDatabase BaseDeDatos()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            Assert.IsNotNull(bd, "Falta la LevelDatabase: ejecuta Fragmentum → Regenerar todo.");
            return bd;
        }

        private static GuiaMecanicas Guia()
        {
            var guia = AssetDatabase.LoadAssetAtPath<GuiaMecanicas>(GeneradorProgresion.RutaGuiaMecanicas);
            Assert.IsNotNull(guia, $"Falta {GeneradorProgresion.RutaGuiaMecanicas}: ejecuta Fragmentum → Regenerar todo.");
            return guia;
        }

        private static PantallaMecanicas.Fila[] Filas(DatosGuardado datos, LevelData nivelActual = null, System.Func<HABILIDADES, bool> enVivo = null) =>
            PantallaMecanicas.Calcular(Guia(), BaseDeDatos(), datos, nivelActual, enVivo, Acciones()).ToArray();

        [TestCase("{Jump}", "ESPACIO")]
        [TestCase("{Sprint}", "SHIFT")]
        [TestCase("{Attack}", "CLIC / ENTER")]
        [TestCase("{Crouch}", "C / CTRL")]
        [TestCase("{Ability}", "Q")]
        [TestCase("{Walk}", "ALT")]
        [TestCase("{Move/left,right}", "A D / ← →")]
        [TestCase("Mantén {Attack}", "Mantén CLIC / ENTER")]
        [TestCase("Pasiva", "Pasiva")]
        [TestCase("{NoExiste}", EtiquetasControles.SinTecla)]
        public void LasTeclas_SalenDelAssetDeInput(string plantilla, string esperado)
        {
            Assert.AreEqual(esperado, EtiquetasControles.Resolver(plantilla, Acciones()));
        }

        [Test]
        public void CadaPlantilla_NombraAccionesQueExisten()
        {
            var mapa = Acciones().FindActionMap(EtiquetasControles.MapaJugador, true);
            var plantillas = Guia().movimientos.Select(m => (m.nombre, m.controles))
                .Concat(BaseDeDatos().niveles.Where(n => n != null && n.fragmento != null && n.fragmento.habilidad != null)
                    .Select(n => (n.fragmento.habilidad.nombre, n.fragmento.habilidad.controles)));

            foreach (var (nombre, controles) in plantillas)
            {
                Assert.IsNotEmpty(controles, $"«{nombre}» no dice con qué teclas se hace.");
                foreach (var accion in EtiquetasControles.AccionesDe(controles))
                {
                    var encontrada = mapa.FindAction(accion, false);
                    Assert.IsNotNull(encontrada, $"«{nombre}» nombra la acción {{{accion}}}, que no existe en el mapa Player.");
                    Assert.IsNotEmpty(EtiquetasControles.DeAccion(encontrada), $"{{{accion}}} no tiene teclas de teclado ni de ratón.");
                }
            }
        }

        [Test]
        public void SinProgreso_SeVenLosMovimientos_YLasCuatroHabilidadesBloqueadas()
        {
            var filas = Filas(new DatosGuardado());

            var movimientos = filas.Where(f => !f.EsHabilidad).Select(f => f.Nombre).ToArray();
            CollectionAssert.AreEqual(new[] { "Moverse", "Caminar", "Saltar", "Doble salto", "Pared", "Dash", "Atacar" }, movimientos,
                "Agacharse no aparece hasta llegar a un mundo donde sirve.");

            var habilidades = filas.Where(f => f.EsHabilidad).ToArray();
            Assert.AreEqual(4, habilidades.Length, "Una por fragmento, salvo la Esperanza.");
            Assert.IsTrue(habilidades.All(h => h.Bloqueada));
            Assert.IsTrue(habilidades.All(h => h.Nombre == "? ? ?"), "No se dice cuál es hasta integrarla.");
            StringAssert.Contains("II · CIUDAD SUMERGIDA", habilidades[1].Descripcion, "Dice dónde se consigue.");
        }

        [Test]
        public void AlIntegrarLaIra_SeExplicaElAtaqueCargado_ConSusTeclas()
        {
            var datos = new DatosGuardado();
            datos.habilidades.Add(HABILIDADES.ATAQUE_CARGADO.ToString());

            var habilidades = Filas(datos).Where(f => f.EsHabilidad).ToArray();

            var cargado = habilidades[0];
            Assert.IsFalse(cargado.Bloqueada);
            Assert.AreEqual("Ataque Cargado", cargado.Nombre);
            Assert.AreEqual("Mantén CLIC / ENTER", cargado.Controles);
            Assert.AreEqual("Fragmento de la Ira", cargado.Origen);
            Assert.AreEqual(BaseDeDatos().niveles[0].ColorAcento, cargado.Color, "Con el color de su mundo.");
            Assert.IsTrue(habilidades.Skip(1).All(h => h.Bloqueada));
        }

        [Test]
        public void ElJugadorVivo_TambienCuenta()
        {
            var filas = Filas(new DatosGuardado(), enVivo: h => h == HABILIDADES.INVISIBILIDAD_PARCIAL);

            var invisibilidad = filas.Where(f => f.EsHabilidad).ElementAt(2);
            Assert.IsFalse(invisibilidad.Bloqueada, "Las escenas de prueba dan habilidades desde el Inspector.");
            Assert.AreEqual("Q", invisibilidad.Controles);
        }

        [Test]
        public void Agacharse_AparecePorPrimeraVez_EnElMundoDeLaCulpa()
        {
            var datos = new DatosGuardado { nivelMaximoAlcanzado = 3 };
            Assert.IsFalse(Filas(datos).Any(f => f.Nombre == "Agacharse"), "Hasta el Miedo no sirve.");

            datos.nivelMaximoAlcanzado = 4;
            var agacharse = Filas(datos).Single(f => f.Nombre == "Agacharse");
            Assert.AreEqual("C / CTRL", agacharse.Controles);

            var culpa = BaseDeDatos().niveles.First(n => n.reglas.agacharse);
            Assert.IsTrue(Filas(new DatosGuardado(), culpa).Any(f => f.Nombre == "Agacharse"), "Y dentro de ese mundo, siempre.");
        }

        [Test]
        public void ElGenerador_DejaLaGuia_LasTeclasDeCadaHabilidad_YLaGuiaEnLosSistemas()
        {
            Assert.GreaterOrEqual(Guia().movimientos.Count, 7);

            foreach (var habilidad in BaseDeDatos().niveles.Where(n => n != null && n.fragmento != null && n.fragmento.habilidad != null)
                         .Select(n => n.fragmento.habilidad))
            {
                Assert.IsNotEmpty(habilidad.controles, $"{habilidad.name} sin teclas para la guía.");
            }

            var interfaz = Resources.Load<GameObject>(Bootstrap.RutaRecurso).GetComponent<UIManager>();
            Assert.AreEqual(Guia(), new SerializedObject(interfaz).FindProperty("guiaMecanicas").objectReferenceValue);
        }

        [Test]
        public void SinAsset_LaGuiaUsaLosMovimientosDelDiseno()
        {
            var guia = GuiaMecanicas.PorDefecto();
            try
            {
                var filas = PantallaMecanicas.Calcular(guia, null, null, null, null, Acciones());
                Assert.AreEqual(7, filas.Count, "Sin base de datos: los movimientos y ninguna habilidad.");
                Assert.AreEqual("ESPACIO", filas[2].Controles);
            }
            finally
            {
                Object.DestroyImmediate(guia);
            }
        }

        [Test]
        public void CaminarConAlt_DejaLaDireccionPorDebajoDelSigilo()
        {
            var jugador = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorJugador.RutaPrefab);
            Assert.IsNotNull(jugador, $"Falta {ConstructorJugador.RutaPrefab}.");
            var entrada = jugador.GetComponentInChildren<Fragmentum.Core.Input.FuenteInputJugador>(true);
            var modo = jugador.GetComponentInChildren<PlayerModoMovimiento>(true);
            var umbral = new SerializedObject(modo).FindProperty("umbralSigilo").floatValue;

            Assert.Less(entrada.FactorCaminar, umbral, "Con Alt, Kael va por debajo del umbral del sigilo: no hace ruido.");
            Assert.AreEqual(entrada.FactorCaminar, Fragmentum.Core.Input.FuenteInputJugador.ConCaminar(1f, true, entrada.FactorCaminar), 1e-5f);
            Assert.AreEqual(-1f, Fragmentum.Core.Input.FuenteInputJugador.ConCaminar(-1f, false, entrada.FactorCaminar), 1e-5f, "Sin Alt, a fondo.");
            Assert.AreEqual(PlayerModoMovimiento.Clasificar(entrada.FactorCaminar, umbral, 0.6f), MODOS_MOVIMIENTO.STEALTH);
        }

        [Test]
        public void ElMenuPrincipal_TieneElBotonMecanicas()
        {
            var escena = EditorSceneManager.OpenScene(GeneradorProgresion.RutaEscenaMenu, OpenSceneMode.Additive);
            try
            {
                var menu = escena.GetRootGameObjects().Select(r => r.GetComponentInChildren<MainMenu>(true)).FirstOrDefault(m => m != null);
                Assert.IsNotNull(menu);
                Assert.IsNotNull(new SerializedObject(menu).FindProperty("botonMecanicas").objectReferenceValue);
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
