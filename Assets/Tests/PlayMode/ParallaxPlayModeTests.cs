using System.Collections;
using System.Linq;
using Fragmentum.Levels.Escenario;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// El fondo del Mundo 1 con una cámara de verdad que se mueve: cada capa se desplaza en
    /// pantalla según su profundidad, siguen a la cámara en vertical, tapan la vista y los hitos
    /// salen en su sitio aunque la partida empiece en un checkpoint.
    /// </summary>
    public class ParallaxPlayModeTests : PruebaPlayModeBase
    {
        private const string RutaFondo = "Assets/Prefabs/Fondos/FondoMundo1.prefab";

        // Velocidad de cada capa según la tabla del montaje (0 = viaja con la cámara, 1 = fija en el mundo).
        private static readonly (string ruta, float velocidad)[] Capas =
        {
            ("CapasRepetibles/Cielo", 0f),
            ("CapasRepetibles/FondoLejano", 0.2f),
            ("Fortaleza/Fortaleza_1", 0.4f),
            ("Muro/Muro_1", 0.6f),
            ("Cataratas/Catarata_2", 0.6f),
            ("CapasRepetibles/PrimerPlano", 0.8f),
        };

        private Camera _camara;
        private GameObject _fondo;

        private IEnumerator Montar(float x, float y, float tamanoOrtografico = 4.85f)
        {
            var objeto = new GameObject("Camara") { tag = "MainCamera" };
            Creados.Add(objeto);
            _camara = objeto.AddComponent<Camera>();
            _camara.orthographic = true;
            _camara.orthographicSize = tamanoOrtografico;
            MoverCamara(x, y);

            _fondo = Object.Instantiate(CargarPrefab(RutaFondo));
            Creados.Add(_fondo);
            yield return null; // Start y el primer LateUpdate
            yield return null;
        }

        private void MoverCamara(float x, float y) => _camara.transform.position = new Vector3(x, y, -10f);

        private Transform Capa(string ruta)
        {
            var capa = _fondo.transform.Find(ruta);
            Assert.IsNotNull(capa, $"Falta {ruta} en el prefab del fondo.");
            return capa;
        }

        /// <summary>
        /// Dónde está la capa en pantalla, en unidades: para un prop, su X respecto de la cámara;
        /// para una repetible, lo que se ha corrido la textura, que se repite cada
        /// <c>periodo</c> unidades.
        /// </summary>
        private (float posicion, float periodo) PosicionEnPantalla(Transform capa)
        {
            if (capa.GetComponent<SpriteRenderer>() != null)
            {
                return (capa.position.x - _camara.transform.position.x, float.PositiveInfinity);
            }

            var renderer = capa.GetComponent<MeshRenderer>();
            var bloque = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(bloque);
            var textura = renderer.sharedMaterial.mainTexture;
            var anchoTextura = _camara.orthographicSize * 2f * textura.width / textura.height;
            return (-bloque.GetVector("_BaseMap_ST").z * anchoTextura, anchoTextura);
        }

        [UnityTest]
        public IEnumerator AlMoverLaCamara_CadaCapaSeDesplazaEnPantallaSegunSuProfundidad()
        {
            yield return Montar(30f, 1.5f);
            var antes = Capas.Select(c => PosicionEnPantalla(Capa(c.ruta))).ToArray();

            MoverCamara(31f, 1.5f);
            yield return null;

            for (var i = 0; i < Capas.Length; i++)
            {
                var (despues, periodo) = PosicionEnPantalla(Capa(Capas[i].ruta));
                var desplazamiento = despues - antes[i].posicion;
                if (!float.IsInfinity(periodo))
                {
                    // Si la textura dio la vuelta (el desplazamiento va de 0 a 1), se cuenta el tramo corto.
                    desplazamiento = Mathf.Repeat(desplazamiento + periodo / 2f, periodo) - periodo / 2f;
                }

                Assert.AreEqual(-Capas[i].velocidad, desplazamiento, 2e-3f,
                    $"{Capas[i].ruta}: por cada unidad de cámara se corre {Capas[i].velocidad} u en pantalla.");
            }
        }

        [UnityTest]
        public IEnumerator LasCapasSiguenALaCamaraEnVertical_YTapanLaVista()
        {
            yield return Montar(20f, 1.5f);
            MoverCamara(23f, 6f);
            yield return null;

            var medioAlto = _camara.orthographicSize;
            var medioAncho = medioAlto * _camara.aspect;
            foreach (var quad in _fondo.GetComponentInChildren<ParallaxMovement>().GetComponentsInChildren<MeshRenderer>())
            {
                var caja = quad.bounds;
                Assert.LessOrEqual(caja.min.x, 23f - medioAncho + 1e-3f, $"{quad.name} no llega al borde izquierdo.");
                Assert.GreaterOrEqual(caja.max.x, 23f + medioAncho - 1e-3f, $"{quad.name} no llega al borde derecho.");
                Assert.AreEqual(6f - medioAlto, caja.min.y, 1e-3f, $"{quad.name}: ocupa justo el alto de la vista.");
                Assert.AreEqual(6f + medioAlto, caja.max.y, 1e-3f, quad.name);
            }

            // La catarata sigue apoyada en el borde inferior de la pantalla.
            Assert.AreEqual(6f - medioAlto, Capa("Cataratas/Catarata_1").position.y, 1e-3f);
            Assert.AreEqual(6f, Capa("Fortaleza/Fortaleza_1").position.y, 1e-3f);
        }

        [UnityTest]
        public IEnumerator ElMuroYSuCatarata_VanJuntos()
        {
            yield return Montar(50f, 1.5f);
            var muro = Capa("Muro/Muro_1");
            var catarata = Capa("Cataratas/Catarata_2");
            var separacion = catarata.position.x - muro.position.x;

            for (var x = 52f; x <= 76f; x += 4f)
            {
                MoverCamara(x, 1.5f);
                yield return null;
                Assert.AreEqual(separacion, catarata.position.x - muro.position.x, 1e-3f, $"Con la cámara en {x} la catarata resbala sobre el muro.");
            }
        }

        [UnityTest]
        public IEnumerator LosHitosSalenEnSuSitio_AunqueLaPartidaEmpieceEnUnCheckpoint()
        {
            // La cámara arranca en x = 40 (el checkpoint del Nivel 1), no al principio del nivel.
            yield return Montar(40f, 1.5f);
            var fortaleza = Capa("Fortaleza/Fortaleza_1");
            var ancla = _fondo.transform.Find("Fortaleza").GetComponent<ParallaxProps>().Ancla(0);

            MoverCamara(ancla, 1.5f);
            yield return null;
            Assert.AreEqual(0f, fortaleza.position.x - ancla, 1e-3f, "Con la cámara en su ancla, la fortaleza queda centrada.");
        }

        [UnityTest]
        public IEnumerator ConOtroTamanoDeCamara_ElFondoSeAjustaALaVista()
        {
            // La plantilla de escenas usa tamaño ortográfico 5, no 4,85 como el Nivel 1.
            yield return Montar(10f, 0f, 5f);

            var cielo = Capa("CapasRepetibles/Cielo").GetComponent<MeshRenderer>();
            Assert.AreEqual(10f, cielo.bounds.size.y, 1e-3f);
            Assert.AreEqual(-5f, Capa("Cataratas/Catarata_1").position.y, 1e-3f, "Los props se escalan con las repetibles.");
            var fortaleza = Capa("Fortaleza/Fortaleza_1").GetComponent<SpriteRenderer>();
            Assert.AreEqual(10f, fortaleza.bounds.size.y, 1e-3f);
        }
    }
}
