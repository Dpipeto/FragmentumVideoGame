using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Los datos de los jefes siguen el Anexo A y la elección de fase por vida es correcta.
    /// También comprueba que cada jefe tiene su prefab con su controlador.
    /// </summary>
    public class DatosJefesTests
    {
        private readonly List<Object> _creados = new List<Object>();

        [TearDown]
        public void Limpiar()
        {
            foreach (var objeto in _creados)
            {
                if (objeto != null)
                {
                    Object.DestroyImmediate(objeto);
                }
            }

            _creados.Clear();
        }

        // --- Fase según la vida ---

        [TestCase(1f, 0)]
        [TestCase(0.51f, 0)]
        [TestCase(0.5f, 1)]
        [TestCase(0.1f, 1)]
        [TestCase(0f, 1)]
        public void DosFases_LaSegundaEmpiezaAMitadDeVida(float fraccion, int esperada)
        {
            Assert.AreEqual(esperada, Jefe(1f, 0.5f).FaseParaVida(fraccion));
        }

        [TestCase(0.7f, 0)]
        [TestCase(0.6f, 1)]
        [TestCase(0.31f, 1)]
        [TestCase(0.3f, 2)]
        [TestCase(0f, 2)]
        public void TresFases_ElJefeFinal(float fraccion, int esperada)
        {
            Assert.AreEqual(esperada, Jefe(1f, 0.6f, 0.3f).FaseParaVida(fraccion));
        }

        [Test]
        public void SinFases_SiempreLaPrimera()
        {
            var jefe = Jefe();
            Assert.AreEqual(0, jefe.FaseParaVida(0.1f));
            Assert.AreEqual(1, jefe.NumeroFases, "Siempre hay al menos una fase.");
        }

        // --- Datos del Anexo A ---

        [Test]
        public void LaCentinelaArdiente_SigueElAnexo_YAnadeUnaTerceraFaseDeIra()
        {
            var centinela = Datos("CentinelaArdiente");

            Assert.AreEqual("Centinela Ardiente", centinela.nombreCanonico);
            Assert.AreEqual(3, centinela.fases.Count, "Las dos fases del Anexo y la ira desatada, añadida tras la primera prueba.");
            Assert.AreEqual(0.5f, centinela.fases[1].umbralVida, "La fase 2 empieza al 50 % de vida.");
            Assert.AreEqual(0.25f, centinela.fases[2].umbralVida, 0.001f, "La ira desatada, al 25 %.");
            Assert.AreEqual(1.2f, centinela.duracionVulnerable, 0.001f, "La espada queda clavada 1,2 s.");

            var fase1 = Tipos(centinela.fases[0]);
            CollectionAssert.Contains(fase1, typeof(PatronEspadazo));
            CollectionAssert.Contains(fase1, typeof(PatronOndaSuelo));
            CollectionAssert.DoesNotContain(fase1, typeof(PatronPlataformasErupcion));
            CollectionAssert.Contains(Tipos(centinela.fases[1]), typeof(PatronPlataformasErupcion), "Las erupciones llegan en la fase 2.");
            CollectionAssert.Contains(Tipos(centinela.fases[2]), typeof(PatronPlataformasErupcion));
        }

        [Test]
        public void LaCentinelaArdiente_SeEnfureceFaseAFase_SinDejarDeSerLegible()
        {
            var fases = Datos("CentinelaArdiente").fases;
            for (var i = 1; i < fases.Count; i++)
            {
                Assert.Greater(fases[i].multiplicadorVelocidad, fases[i - 1].multiplicadorVelocidad, $"La fase {i + 1} persigue más rápido.");
                Assert.Less(fases[i].pausaEntrePatrones, fases[i - 1].pausaEntrePatrones, $"La fase {i + 1} descansa menos entre ataques.");
            }

            foreach (var fase in fases)
            {
                // Siempre hay por dónde ganarle: algún espadazo deja la espada clavada.
                Assert.IsTrue(fase.patrones.OfType<PatronEspadazo>().Any(m => m.abreVentana), $"{fase.nombre}: abre la ventana vulnerable.");
                CollectionAssert.Contains(Tipos(fase), typeof(PatronSaltoAplastante), $"{fase.nombre}: salta sobre el jugador.");
                foreach (var patron in fase.patrones)
                {
                    Assert.GreaterOrEqual(patron.aviso, 0.3f, $"{patron.nombre}: el aviso se tiene que poder ver.");
                    if (patron is PatronEspadazo espadazo)
                    {
                        Assert.GreaterOrEqual(espadazo.avisoEncadenado, 0.3f, $"{patron.nombre}: también entre golpes.");
                    }
                }
            }

            var furia = fases[1].patrones.OfType<PatronEspadazo>().First();
            Assert.GreaterOrEqual(furia.golpes, 3, "En la furia, combos de tres.");
            Assert.Less(furia.duracionVentana, 1.2f, "Y la ventana dura menos.");
        }

        [Test]
        public void ElJuezSilente_SoloRecibeDanioEnLaVentana()
        {
            Assert.IsTrue(Datos("JuezSilente").inmuneFueraDeVulnerable);
        }

        [Test]
        public void ElVerdaderoSoldado_TieneTresFases()
        {
            var soldado = Datos("VerdaderoSoldado");
            Assert.AreEqual(3, soldado.fases.Count);
            Assert.AreEqual(0.6f, soldado.fases[1].umbralVida, 0.001f);
            Assert.AreEqual(0.3f, soldado.fases[2].umbralVida, 0.001f);
        }

        [Test]
        public void CadaJefe_TienePrefabConSuControladorYSusDatos()
        {
            foreach (var (asset, controlador) in GeneradorJefes.Jefes)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GeneradorJefes.CarpetaPrefabs}/{asset}.prefab");
                Assert.IsNotNull(prefab, $"Falta el prefab de {asset}.");

                var jefe = prefab.GetComponent(controlador) as BossController;
                Assert.IsNotNull(jefe, $"{asset} no tiene {controlador.Name}.");
                Assert.AreSame(Datos(asset), jefe.Datos);
                Assert.IsNotNull(prefab.GetComponent<SaludJefe>());
                Assert.IsTrue(prefab.CompareTag(GameTags.Boss));
                Assert.AreEqual(LayerMask.NameToLayer(Capas.Enemy), prefab.layer, "La espada del jugador golpea a la capa Enemy.");

                var contacto = prefab.GetComponentInChildren<Hitbox>(true);
                Assert.IsNotNull(contacto, $"{asset} no hace daño por contacto.");
                Assert.AreEqual(LayerMask.NameToLayer(Capas.EnemyWeapon), contacto.gameObject.layer);
            }
        }

        [Test]
        public void CadaNivelConJefe_TieneElPrefabDeSuJefe()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            foreach (var nivel in bd.niveles.Where(n => n.jefe != null))
            {
                Assert.IsNotNull(GeneradorJefes.CargarPrefab(nivel.jefe), $"{nivel.name}: falta el prefab de {nivel.jefe.nombreCanonico}.");
            }
        }

        private JefeData Jefe(params float[] umbrales)
        {
            var jefe = ScriptableObject.CreateInstance<JefeData>();
            _creados.Add(jefe);
            foreach (var umbral in umbrales)
            {
                jefe.fases.Add(new FaseJefe { umbralVida = umbral });
            }

            return jefe;
        }

        private static JefeData Datos(string asset)
        {
            var datos = GeneradorJefes.CargarDatos(asset);
            Assert.IsNotNull(datos, $"Falta el JefeData {asset}: ejecuta Fragmentum → Regenerar todo.");
            return datos;
        }

        private static List<System.Type> Tipos(FaseJefe fase) => fase.patrones.Where(p => p != null).Select(p => p.GetType()).ToList();
    }
}
