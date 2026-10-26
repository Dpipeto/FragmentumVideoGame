using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Levels.Escenario;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Fragmentum.EditorTools.ConstructorFondoMundo1;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El fondo con parallax del Mundo 1: las cuentas, la importación de las seis imágenes, el
    /// prefab que deja <see cref="ConstructorFondoMundo1"/> y su reparto en el Nivel 1.
    /// </summary>
    public class FondoParallaxTests
    {
        private const float ZCamara = -10f;
        private const float AnchoVista = AltoReferencia * RepartoHitos.AspectoReferencia;

        // --- Cuentas ---

        [Test]
        public void VelocidadPorProfundidad_DaLaTablaDelMontaje()
        {
            // Cámara en z = -10 y la capa más lejana (el cielo) en z = 40: 50 u de distancia.
            foreach (var capa in CapasFondo)
            {
                Assert.AreEqual(capa.Velocidad, CalculoParallax.VelocidadPorProfundidad(capa.Z, ZCamara, 50f), 1e-5f, capa.Nombre);
            }
        }

        [Test]
        public void UnHitoAnclado_SeVeCentradoAlPasarLaCamara_YSeDesplazaASuVelocidad()
        {
            Assert.AreEqual(24f, CalculoParallax.PosicionX(24f, 24f, 0.4f), 1e-5f, "Con la cámara en su ancla, en el centro.");

            // Diez unidades más allá, en pantalla se ha movido 0,4 × 10 hacia la izquierda.
            Assert.AreEqual(-4f, CalculoParallax.PosicionX(24f, 34f, 0.4f) - 34f, 1e-5f);
            Assert.AreEqual(0f, CalculoParallax.PosicionX(24f, 34f, 0f) - 34f, 1e-5f, "Velocidad 0: viaja con la cámara.");
            Assert.AreEqual(24f, CalculoParallax.PosicionX(24f, 34f, 1f), 1e-5f, "Velocidad 1: fija en el mundo.");
        }

        [Test]
        public void DosTexturasDeDistintoAncho_ConLaMismaVelocidad_SeMuevenIgualEnPantalla()
        {
            // Lo que fallaba con un único parallaxSpeed: el fondo lejano (2752 px) corría el doble que el cielo (1376 px).
            const float velocidad = 0.2f;
            const float ancho = 17.6f;
            foreach (var anchoTextura in new[] { 17.38f, 34.76f, 14.15f })
            {
                var antes = CalculoParallax.OffsetTextura(10f, velocidad, anchoTextura, ancho);
                var despues = CalculoParallax.OffsetTextura(11f, velocidad, anchoTextura, ancho);
                Assert.AreEqual(velocidad, Mathf.Repeat(despues - antes, 1f) * anchoTextura, 1e-4f,
                    $"Una textura de {anchoTextura} u se desplaza {velocidad} u por cada unidad de cámara.");
            }
        }

        [Test]
        public void ConVelocidadUno_LaTexturaQuedaFijaEnElMundo()
        {
            const float anchoTextura = 34.76f;
            var antes = CalculoParallax.OffsetTextura(3f, 1f, anchoTextura, 17.6f);
            var despues = CalculoParallax.OffsetTextura(8f, 1f, anchoTextura, 17.6f);
            Assert.AreEqual(5f, Mathf.Repeat(despues - antes, 1f) * anchoTextura, 1e-4f, "Avanza lo mismo que la cámara.");
        }

        [Test]
        public void UnTramoDelAnchoCalculado_TapaLaPantallaMientrasLaCamaraRecorreElTramo()
        {
            const float velocidad = 0.6f;
            var ancho = CalculoParallax.AnchoParaCubrir(60f, 80f, velocidad, AnchoVista);
            for (var camara = 60f; camara <= 80f; camara += 0.5f)
            {
                var centroEnPantalla = CalculoParallax.PosicionX(70f, camara, velocidad) - camara;
                Assert.LessOrEqual(centroEnPantalla - ancho / 2f, -AnchoVista / 2f + 1e-4f, $"Borde izquierdo con la cámara en {camara}.");
                Assert.GreaterOrEqual(centroEnPantalla + ancho / 2f, AnchoVista / 2f - 1e-4f, $"Borde derecho con la cámara en {camara}.");
            }
        }

        [Test]
        public void LaFortaleza_SeVeDuranteMasDe80uDeRecorrido()
        {
            // Por eso en el Nivel 1 (75 u de recorrido de cámara) solo cabe una.
            var anchoFortaleza = 1376f / PixelesPorUnidad;
            var recorrido = CalculoParallax.RecorridoVisible(0f, anchoFortaleza, 0.4f, AnchoVista);
            Assert.Greater(recorrido.y - recorrido.x, 80f);
        }

        // --- Importación ---

        [Test]
        public void LasImagenes_SeImportanComoPixelArt_SinReescalar()
        {
            foreach (var capa in CapasFondo)
            {
                var importador = AssetImporter.GetAtPath(capa.Ruta) as TextureImporter;
                Assert.IsNotNull(importador, $"Falta {capa.Ruta}: cópiala y ejecuta Fragmentum → Regenerar todo.");
                Assert.AreEqual(FilterMode.Point, importador.filterMode, $"{capa.Archivo}: filtro Point, sin suavizado.");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importador.textureCompression, capa.Archivo);
                Assert.IsFalse(importador.mipmapEnabled, capa.Archivo);
                Assert.IsTrue(importador.alphaIsTransparency, capa.Archivo);
                Assert.AreEqual(TextureImporterNPOTScale.None, importador.npotScale, $"{capa.Archivo}: sin reescalar a potencia de dos.");

                importador.GetSourceTextureWidthAndHeight(out var ancho, out var alto);
                Assert.GreaterOrEqual(importador.maxTextureSize, Mathf.Max(ancho, alto), $"{capa.Archivo}: el tamaño máximo no la reduce.");
                var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(capa.Ruta);
                Assert.AreEqual(new Vector2Int(ancho, alto), new Vector2Int(textura.width, textura.height), $"{capa.Archivo}: se importa a su tamaño.");
            }
        }

        [Test]
        public void LasRepetibles_SeRepitenEnHorizontal_YLasDemasSonSprites()
        {
            foreach (var capa in CapasFondo)
            {
                var importador = (TextureImporter)AssetImporter.GetAtPath(capa.Ruta);
                if (capa.Tecnica == TECNICAS_FONDO.REPETIBLE)
                {
                    Assert.AreEqual(TextureImporterType.Default, importador.textureType, capa.Archivo);
                    Assert.AreEqual(TextureWrapMode.Repeat, importador.wrapModeU, $"{capa.Archivo}: con Clamp el desplazamiento estira el píxel del borde.");
                    Assert.AreEqual(TextureWrapMode.Clamp, importador.wrapModeV, capa.Archivo);
                    continue;
                }

                var ajustes = new TextureImporterSettings();
                importador.ReadTextureSettings(ajustes);
                Assert.AreEqual(TextureImporterType.Sprite, importador.textureType, capa.Archivo);
                Assert.AreEqual(SpriteImportMode.Single, importador.spriteImportMode, capa.Archivo);
                Assert.AreEqual(SpriteMeshType.FullRect, ajustes.spriteMeshType, capa.Archivo);
                Assert.AreEqual(PixelesPorUnidad, importador.spritePixelsPerUnit, 1e-3f, $"{capa.Archivo}: misma escala que las repetibles.");
                Assert.AreEqual((int)capa.Pivote, ajustes.spriteAlignment, capa.Archivo);
            }
        }

        [Test]
        public void LasTexturasQueSeRepiten_EncajanBordeConBorde()
        {
            foreach (var capa in CapasFondo.Where(c => c.Tecnica != TECNICAS_FONDO.PROP))
            {
                var textura = new Texture2D(2, 2);
                try
                {
                    var ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", capa.Ruta));
                    Assert.IsTrue(textura.LoadImage(File.ReadAllBytes(ruta)), capa.Ruta);
                    var pixeles = textura.GetPixels32();
                    var w = textura.width;

                    // Diferencia entre la última columna y la primera (la costura) frente a la de columnas vecinas.
                    float Diferencia(int a, int b)
                    {
                        var suma = 0f;
                        for (var y = 0; y < textura.height; y++)
                        {
                            Color32 p = pixeles[y * w + a], q = pixeles[y * w + b];
                            suma += Mathf.Abs(p.r - q.r) + Mathf.Abs(p.g - q.g) + Mathf.Abs(p.b - q.b) + Mathf.Abs(p.a - q.a);
                        }

                        return suma / (4f * textura.height);
                    }

                    var vecinas = Enumerable.Range(0, 64).Select(i => Diferencia(i * (w - 2) / 64, i * (w - 2) / 64 + 1)).Average();
                    Assert.LessOrEqual(Diferencia(w - 1, 0), vecinas * 1.5f + 0.01f, $"{capa.Archivo} deja una costura al repetirse.");
                }
                finally
                {
                    Object.DestroyImmediate(textura);
                }
            }
        }

        // --- Prefab ---

        private static GameObject Prefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Assert.IsNotNull(prefab, $"Falta {RutaPrefab}: ejecuta Fragmentum → Regenerar todo.");
            return prefab;
        }

        [Test]
        public void ElPrefab_TieneLasSeisCapasConSuTecnica()
        {
            var prefab = Prefab();
            var repetibles = prefab.GetComponentInChildren<ParallaxMovement>();
            Assert.IsNotNull(repetibles);
            CollectionAssert.AreEquivalent(new[] { "Cielo", "FondoLejano", "PrimerPlano" },
                repetibles.transform.Cast<Transform>().Select(t => t.name).ToArray(), "Solo las capas repetibles: el muro va en tramos.");

            foreach (var capa in CapasFondo.Where(c => c.Tecnica == TECNICAS_FONDO.REPETIBLE))
            {
                var quad = repetibles.transform.Find(capa.Nombre).GetComponent<MeshRenderer>();
                Assert.IsNotNull(quad, capa.Nombre);
                var material = quad.sharedMaterial;
                Assert.AreEqual(ShaderCapas, material.shader.name, $"{capa.Nombre}: los shaders 2D de sprites ignoran el desplazamiento.");
                Assert.AreEqual("_BaseMap", ParallaxMovement.TexturaPrincipal(material));
                Assert.AreEqual(1f, material.GetFloat("_Surface"), $"{capa.Nombre}: transparente, deja ver las capas de atrás.");
                Assert.AreEqual((int)UnityEngine.Rendering.RenderQueue.Transparent, material.renderQueue, capa.Nombre);
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(capa.Ruta), material.mainTexture, capa.Nombre);
                Assert.AreEqual(capa.Z, quad.transform.localPosition.z, 1e-4f, capa.Nombre);
            }

            foreach (var (grupo, capa, hitos) in new[] { ("Fortaleza", Fortaleza, 1), ("Muro", Muro, 1), ("Cataratas", Catarata, 1) })
            {
                var props = prefab.transform.Find(grupo).GetComponent<ParallaxProps>();
                Assert.IsNotNull(props, grupo);
                Assert.AreEqual(hitos, props.transform.childCount, $"{grupo}: hitos del reparto del Nivel 1.");
                foreach (Transform hija in props.transform)
                {
                    var sprite = hija.GetComponent<SpriteRenderer>();
                    Assert.AreEqual(capa.Ruta, AssetDatabase.GetAssetPath(sprite.sprite), hija.name);
                    Assert.AreEqual(capa.Tecnica == TECNICAS_FONDO.TRAMO ? SpriteDrawMode.Tiled : SpriteDrawMode.Simple, sprite.drawMode, hija.name);
                    Assert.AreEqual("Sprite-Unlit-Default", sprite.sharedMaterial.name, $"{hija.name}: como las repetibles, no le afectan las luces.");
                }
            }
        }

        [Test]
        public void LasVelocidades_SiguenLaProfundidad_EnLasDosTecnicas()
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var camara = new GameObject("Camara").AddComponent<Camera>();
                camara.orthographic = true;
                camara.transform.position = new Vector3(0f, 0f, ZCamara);
                var fondo = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(), escena);

                var repetibles = fondo.GetComponentInChildren<ParallaxMovement>();
                repetibles.Preparar(camara);
                Assert.AreEqual(3, repetibles.Capas);
                for (var i = 0; i < repetibles.Capas; i++)
                {
                    var capa = CapasFondo.Single(c => c.Nombre == repetibles.Capa(i).name);
                    Assert.AreEqual(capa.Velocidad, repetibles.Velocidad(i), 1e-4f, capa.Nombre);
                }

                // Las de props llevan la velocidad escrita: tiene que ser la que daría su Z.
                foreach (var props in fondo.GetComponentsInChildren<ParallaxProps>())
                {
                    var esperada = CalculoParallax.VelocidadPorProfundidad(props.transform.position.z, ZCamara, 50f);
                    Assert.AreEqual(esperada, props.velocidad, 1e-4f, $"{props.name} a z = {props.transform.position.z}.");
                }
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void ElOrdenDeDibujo_VaDeLejosACerca_YQuedaDetrasDelJuego()
        {
            var ordenes = CapasFondo.Select(c => c.Orden).ToArray();
            CollectionAssert.IsOrdered(ordenes, "De la capa más lejana a la más cercana.");
            CollectionAssert.AllItemsAreUnique(ordenes, "Sin empates: el muro y la catarata comparten Z.");
            Assert.Less(ordenes.Max(), -2, "El escenario pintado usa de -2 en adelante y el jugador 10.");

            foreach (var renderer in Prefab().GetComponentsInChildren<Renderer>(true))
            {
                var capa = CapasFondo.Single(c => renderer.name == c.Nombre || renderer.name.StartsWith(c.Nombre + "_"));
                Assert.AreEqual(capa.Orden, renderer.sortingOrder, renderer.name);
            }
        }

        // --- Reparto ---

        [Test]
        public void ElNivel1_TieneElFondo_ConUnaFortaleza_YUnaCatarataSobreElMuro()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            var datos = bd.niveles[0];
            var nivel1 = EditorSceneManager.OpenScene(datos.escena.Ruta, OpenSceneMode.Additive);
            try
            {
                var repartos = EscenasNivelTests.TodosEn<RepartoHitos>(nivel1);
                Assert.AreEqual(1, repartos.Count, "Un fondo en el Nivel 1.");
                var reparto = repartos[0];
                Assert.AreEqual(1, EscenasNivelTests.TodosEn<ParallaxMovement>(nivel1).Count);

                Assert.IsEmpty(reparto.Solapes(datos.Extension, AltoReferencia));
                Assert.AreEqual(1, Activas(reparto.transform.Find("Fortaleza")).Count, "Una sola fortaleza.");

                var muro = reparto.transform.Find("Muro").GetComponent<ParallaxProps>();
                var tramo = Activas(muro.transform).Single().GetComponent<SpriteRenderer>();
                var ancho = tramo.size.x;

                // La catarata necesita una pared de la que caer: la única va sobre el muro.
                var cataratas = reparto.transform.Find("Cataratas").GetComponent<ParallaxProps>();
                var catarata = Activas(cataratas.transform).Single().GetComponent<SpriteRenderer>();
                Assert.AreEqual(muro.velocidad, cataratas.velocidad, 1e-5f, "Van juntos.");
                var separacion = cataratas.velocidad * (catarata.transform.position.x - tramo.transform.position.x);
                Assert.LessOrEqual(Mathf.Abs(separacion) + catarata.sprite.bounds.size.x / 2f, ancho / 2f, "La catarata cae dentro del tramo de muro.");

                // El muro tapa la pantalla mientras la cámara recorre la arena del jefe: desde donde
                // empieza su tramo (tres cuartos del nivel) hasta el final.
                var limiteCamara = datos.Extension.y + 6f - AnchoVista / 2f;
                var inicioDelMuro = Mathf.Lerp(datos.Extension.x, datos.Extension.y, ConstructorFondoMundo1.TramosMuro[0].x);
                Assert.Less(inicioDelMuro, DisenoNivel1.PuertaDesde, "El muro empieza antes que la arena.");
                for (var camara = inicioDelMuro; camara <= limiteCamara; camara += 0.5f)
                {
                    var centro = CalculoParallax.PosicionX(tramo.transform.position.x, camara, muro.velocidad) - camara;
                    Assert.LessOrEqual(centro - ancho / 2f, -AnchoVista / 2f + 1e-3f, $"Cámara en {camara}.");
                    Assert.GreaterOrEqual(centro + ancho / 2f, AnchoVista / 2f - 1e-3f, $"Cámara en {camara}.");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(nivel1, true);
            }
        }

        [Test]
        public void Repartir_EnUnNivelLargo_CopiaHitos_AnchaLosTramos_YDesactivaLosQueSobran()
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var fondo = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(), escena);
                var reparto = fondo.GetComponent<RepartoHitos>();
                var fortalezas = reparto.Hitos.Single(h => h.capa.name == "Fortaleza");
                fortalezas.posiciones = new[] { 0.2f, 0.5f, 0.8f };

                reparto.Repartir(new Vector2(0f, 300f), AltoReferencia);
                var activas = Activas(fortalezas.capa.transform);
                CollectionAssert.AreEqual(new[] { 60f, 150f, 240f }, activas.Select(t => t.position.x).ToArray());
                Assert.IsEmpty(reparto.Solapes(new Vector2(0f, 300f), AltoReferencia), "Con 90 u entre ellas no se ven a la vez.");

                var tramo = Activas(fondo.transform.Find("Muro")).Single();
                Assert.AreEqual(262.5f, tramo.position.x, 1e-3f, "En el centro del tramo (75 % a 100 %).");
                Assert.AreEqual(CalculoParallax.AnchoParaCubrir(225f, 300f, 0.6f, AnchoVista), tramo.GetComponent<SpriteRenderer>().size.x, 1e-3f);

                fortalezas.posiciones = new[] { 0.5f };
                reparto.Repartir(new Vector2(0f, 300f), AltoReferencia);
                Assert.AreEqual(1, Activas(fortalezas.capa.transform).Count, "Las que sobran se desactivan.");

                fortalezas.posiciones = new[] { 0.3f, 0.4f };
                reparto.Repartir(new Vector2(0f, 300f), AltoReferencia);
                Assert.IsNotEmpty(reparto.Solapes(new Vector2(0f, 300f), AltoReferencia), "A 30 u, dos fortalezas se ven a la vez.");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static List<Transform> Activas(Transform grupo) =>
            grupo.Cast<Transform>().Where(t => t.gameObject.activeSelf).ToList();
    }
}
