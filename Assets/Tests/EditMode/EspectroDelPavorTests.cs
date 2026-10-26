using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El Espectro del Pavor (fase D del Mundo 3), sin escena: sus datos y sus patrones con las
    /// medidas del diseño (<c>Nivel3_Diseno.md</c>, sección 6), su prefab (un dibujo que se ve a
    /// oscuras), el verificador (que pasa con lo generado y se queja de lo que lo haría invencible)
    /// y dónde caen las franjas de sus hojas.
    /// </summary>
    public class EspectroDelPavorTests
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

        private T Crear<T>() where T : ScriptableObject
        {
            var objeto = ScriptableObject.CreateInstance<T>();
            _creados.Add(objeto);
            return objeto;
        }

        private static JefeData Datos()
        {
            var datos = GeneradorJefes.CargarDatos(GeneradorJefes.NombrePavor);
            Assert.IsNotNull(datos, "Faltan los datos del Espectro del Pavor: ejecuta Fragmentum → Regenerar todo.");
            return datos;
        }

        private static List<System.Type> Tipos(FaseJefe fase) => fase.patrones.Select(p => p.GetType()).ToList();

        // --- Datos ---

        [Test]
        public void SusDatos_SonLosDelDiseno_ConSusCuatroAtaquesEnDosFases()
        {
            var datos = Datos();
            Assert.IsFalse(GeneradorJefes.EsEsqueleto(datos), "Ya no es un esqueleto: tiene sus patrones.");
            Assert.AreEqual(22, datos.vidaMaxima, "Anexo A.");
            Assert.AreEqual(0, datos.danioContacto, "Tocarlo no hace daño: es invisible.");
            Assert.AreEqual(2, datos.fases.Count);
            Assert.AreEqual(0.5f, datos.fases[1].umbralVida, 1e-4f, "La segunda fase, al 50 %.");

            CollectionAssert.AreEqual(new[] { typeof(PatronTajo), typeof(PatronEmbestidaSombra), typeof(PatronTajo), typeof(PatronHojas) },
                Tipos(datos.fases[0]), "Fase 1: tajo, embestida, tajo y hojas.");
            CollectionAssert.AreEqual(new[] { typeof(PatronSombras), typeof(PatronHojas), typeof(PatronSombras), typeof(PatronEmbestidaSombra) },
                Tipos(datos.fases[1]), "Fase 2: sombras, hojas, sombras y embestida.");

            // v0.67: el tajo, las sombras y la embestida, más rápidos (0,6 s); las hojas, 1 s.
            foreach (var patron in datos.fases.SelectMany(f => f.patrones))
            {
                var aviso = patron is PatronHojas ? 1f : 0.6f;
                Assert.AreEqual(aviso, patron.aviso, 1e-4f, $"«{patron.name}»: avisa {aviso} s con su destello.");
                Assert.GreaterOrEqual(patron.aviso, VerificadorPavor.AvisoMinimo, "Rápido, pero se lee.");
            }
        }

        [Test]
        public void SusAtaques_TienenLasMedidasDelDiseno()
        {
            var fases = Datos().fases;
            var tajo = fases[0].patrones.OfType<PatronTajo>().First();
            Assert.AreEqual(2.5f, tajo.distancia, 1e-4f, "Aparece a 2,5 u de Kael.");
            Assert.AreEqual(3f, tajo.alcance, 1e-4f, "Un tajo de 3 u.");
            Assert.AreEqual(2, tajo.danio);
            Assert.AreEqual(1.5f, tajo.jadeo, 1e-4f, "Jadea 1,5 s: la ventana.");

            var embestida = fases[0].patrones.OfType<PatronEmbestidaSombra>().First();
            Assert.AreEqual(16f, embestida.velocidad, 1e-4f, "v0.67: de 11 a 16 u/s.");
            Assert.AreEqual(1.6f, embestida.alto, 1e-4f, "Baja, desde donde pisa Kael…");
            Assert.Less(embestida.alto, 2.4f, "…más baja que el salto de Kael (2,4 u): se salta.");
            Assert.AreEqual(1, embestida.danio);
            Assert.AreEqual(1f, embestida.visibleAlFinal, 1e-4f);

            var hojas = fases[0].patrones.OfType<PatronHojas>().First();
            Assert.AreEqual(4, hojas.franjasMinimo);
            Assert.AreEqual(5, hojas.franjasMaximo, "Cuatro o cinco franjas.");
            Assert.AreEqual(1, hojas.danio);

            var sombras = fases[1].patrones.OfType<PatronSombras>().First();
            Assert.AreSame(tajo, sombras.tajo, "Las sombras lanzan el mismo tajo que él.");
            Assert.AreEqual(1.5f, sombras.jadeo, 1e-4f, "Y jadean las tres 1,5 s.");
        }

        // --- Prefab y verificador ---

        [Test]
        public void SuPrefab_SeVeAOscuras_YElVerificadorLoDaPorBueno()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VerificadorPavor.RutaPrefab);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<EspectroDelPavor>());
            var dibujo = prefab.GetComponentInChildren<SpriteRenderer>(true);
            Assert.AreEqual(GeneradorJefes.RutaMaterialSinLuz, AssetDatabase.GetAssetPath(dibujo.sharedMaterial),
                "Su dibujo no depende de la luz: su destello se ve desde cualquier sitio de la arena.");
            CollectionAssert.IsEmpty(VerificadorPavor.Problemas());
        }

        [Test]
        public void ElVerificador_SeQuejaDeLoQueLoHariaInvencible()
        {
            var tajo = Crear<PatronTajo>();
            tajo.name = "TajoSinJadeo";
            tajo.aviso = 1f;
            tajo.jadeo = 0f;
            var rapido = Crear<PatronHojas>();
            rapido.name = "HojasRapidas";
            rapido.aviso = 0.2f;
            var sinTajo = Crear<PatronSombras>();
            sinTajo.name = "SombrasSinTajo";
            sinTajo.aviso = 1f;

            var datos = Crear<JefeData>();
            datos.danioContacto = 1;
            datos.fases = new List<FaseJefe>
            {
                new FaseJefe { nombre = "Sin ventana", patrones = new List<PatronAtaque> { tajo, rapido } },
                new FaseJefe { nombre = "Sombras sin tajo", umbralVida = 0.5f, patrones = new List<PatronAtaque> { sinTajo } },
                new FaseJefe { nombre = "Vacía", umbralVida = 0.25f },
            };

            var problemas = VerificadorPavor.ProblemasDeLosDatos(datos);
            Assert.IsTrue(problemas.Any(p => p.Contains("tocarlo quita")), "Daño por contacto estando invisible.");
            Assert.IsTrue(problemas.Any(p => p.Contains("«Sin ventana»") && p.Contains("jadeando")), "Una fase sin ventana.");
            Assert.IsTrue(problemas.Any(p => p.Contains("HojasRapidas") && p.Contains("avisa")), "Un aviso que no se lee.");
            Assert.IsTrue(problemas.Any(p => p.Contains("SombrasSinTajo") && p.Contains("no tienen tajo")));
            Assert.IsTrue(problemas.Any(p => p.Contains("«Vacía»") && p.Contains("no tiene patrones")));
        }

        [Test]
        public void ElVerificador_SeQuejaDeUnDibujoQueDependeDeLaLuz()
        {
            var copia = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(VerificadorPavor.RutaPrefab));
            _creados.Add(copia);
            copia.GetComponentInChildren<SpriteRenderer>(true).sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(ConstructorEcoIra.RutaMaterialSprite);

            var problemas = VerificadorPavor.ProblemasDelPrefab(copia, Datos());
            Assert.IsTrue(problemas.Any(p => p.Contains("depende de la luz")), string.Join("\n", problemas));
        }

        // --- Las hojas ---

        [Test]
        public void LasFranjasDeLasHojas_LaPrimeraBajoKael_SinPisarse_YDentroDeLaArena()
        {
            var limites = new Vector2(113f, 138f);
            for (var intento = 0; intento < 20; intento++)
            {
                var kael = Random.Range(115f, 136f);
                var franjas = PatronHojas.Franjas(limites, kael, 5, 2f, 1f);
                // Se reparten al azar: a veces cuatro quedan de forma que ya no cabe la quinta. El diseño dice cuatro o cinco.
                Assert.That(franjas.Count, Is.InRange(4, 5), "En 25 u caben cuatro o cinco franjas de 2 u con su hueco.");
                Assert.AreEqual(kael, franjas[0], 1e-4f, "La primera, bajo Kael.");
                foreach (var x in franjas)
                {
                    Assert.That(x, Is.InRange(limites.x + 1f, limites.y - 1f), "Dentro de la arena.");
                }

                for (var i = 0; i < franjas.Count; i++)
                {
                    for (var j = i + 1; j < franjas.Count; j++)
                    {
                        Assert.GreaterOrEqual(Mathf.Abs(franjas[i] - franjas[j]), 3f - 1e-4f, "Sin pisarse: siempre queda dónde ponerse.");
                    }
                }
            }

            Assert.LessOrEqual(PatronHojas.Franjas(new Vector2(0f, 6f), 3f, 5, 2f, 1f).Count, 2, "Si no caben, las que quepan.");
        }
    }
}
