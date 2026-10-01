using System.Linq;
using Fragmentum.EditorTools;
using Fragmentum.Narrative;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La cinemática inicial con su guion, y las reglas de tiempo de cualquier cinemática
    /// (<see cref="LineaTiempoCinematica"/>): fundidos, textos, bloqueo del salto y aviso. Recorre
    /// los 80 s sin esperarlos.
    /// </summary>
    public class CinematicaInicialTests
    {
        private static CutsceneData Inicial()
        {
            var cinematica = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicaInicial.RutaAsset);
            Assert.IsNotNull(cinematica, $"Falta {ConstructorCinematicaInicial.RutaAsset}: ejecuta Fragmentum → Narrativa → Importar la cinemática inicial.");
            return cinematica;
        }

        private static LineaTiempoCinematica Linea() => new LineaTiempoCinematica(Inicial());

        // Cuánto pesa cada diapositiva en lo que se ve: la de encima tapa a la de abajo.
        private static float[] Pesos(LineaTiempoCinematica linea, float t)
        {
            var pesos = new float[linea.Diapositivas];
            var (abajo, encima) = linea.ImagenesEn(t);
            if (encima.Indice >= 0)
            {
                pesos[encima.Indice] += encima.Alfa;
            }

            if (abajo.Indice >= 0)
            {
                pesos[abajo.Indice] += abajo.Alfa * (1f - (encima.Indice >= 0 ? encima.Alfa : 0f));
            }

            return pesos;
        }

        private static float CartelEn(LineaTiempoCinematica linea, float t, string texto) =>
            linea.TextosEn(t).Where(v => v.Cartel.texto == texto).Select(v => v.Alfa).DefaultIfEmpty(0f).Max();

        // --- El guion ---

        [Test]
        public void TieneLos12PlanosDelGuion_YDura80Segundos()
        {
            var cinematica = Inicial();

            Assert.AreEqual(12, cinematica.diapositivas.Count);
            Assert.AreEqual(80f, cinematica.DuracionTotal, 0.001f);
            Assert.AreEqual(80f, Linea().Duracion, 0.001f, "Los fundidos van dentro de cada plano: no alargan la cinemática.");
            Assert.IsTrue(cinematica.soloPrimeraVez && cinematica.saltable);

            for (var i = 0; i < 12; i++)
            {
                var imagen = cinematica.diapositivas[i].imagen;
                Assert.IsNotNull(imagen, $"El plano {i + 1} no tiene imagen.");
                Assert.AreEqual($"Plano_{i + 1:00}", imagen.name);
            }

            Assert.AreEqual("Hubo una guerra.", cinematica.diapositivas[1].texto);
            Assert.AreEqual("¿Qué es lo que no estás mirando?", cinematica.diapositivas[11].texto);
            Assert.IsEmpty(cinematica.TextoDe(0), "Los planos sin texto no muestran el aviso de «pendiente».");

            var titulo = cinematica.diapositivas[10].carteles.Single();
            Assert.AreEqual("FRAGMENTUM: Ecos de la Identidad", titulo.texto);
            Assert.AreEqual(POSICIONES_CARTEL.ARRIBA, titulo.posicion, "La mitad de arriba de la imagen está vacía a propósito.");
            Assert.IsTrue(titulo.titulo);
        }

        [Test]
        public void LasImagenes_SeImportanComoPixelArt()
        {
            for (var n = 1; n <= ConstructorCinematicaInicial.Planos; n++)
            {
                var ruta = ConstructorCinematicaInicial.RutaImagen(n);
                var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
                Assert.IsNotNull(importador, $"Falta {ruta}.");
                Assert.AreEqual(TextureImporterType.Sprite, importador.textureType, ruta);
                Assert.AreEqual(FilterMode.Point, importador.filterMode, ruta);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importador.textureCompression, ruta);
                Assert.IsFalse(importador.mipmapEnabled, ruta);
            }
        }

        // --- Imágenes ---

        [Test]
        public void NuncaHayUnCorteDuro_EnTodaLaCinematica()
        {
            var linea = Linea();
            const float paso = 1f / 120f;
            var maximoPorPaso = paso / 0.6f + 0.002f; // el fundido más rápido es de 0,6 s

            var anterior = Pesos(linea, 0f);
            for (var t = paso; t < linea.Duracion; t += paso)
            {
                var pesos = Pesos(linea, t);
                for (var k = 0; k < pesos.Length; k++)
                {
                    Assert.LessOrEqual(Mathf.Abs(pesos[k] - anterior[k]), maximoPorPaso,
                        $"Corte duro en el plano {k + 1} a los {t:0.000} s.");
                }

                anterior = pesos;
            }
        }

        [Test]
        public void EmpiezaDesdeNegro_CruzaLosPlanos_YTerminaEnNegro()
        {
            var linea = Linea();

            Assert.AreEqual(0f, Pesos(linea, 0f).Sum(), 0.001f, "Empieza en negro.");
            Assert.AreEqual(0.5f, Pesos(linea, 0.3f)[0], 0.01f, "El primer plano sale de negro en 0,6 s.");
            Assert.AreEqual(1f, Pesos(linea, 3f)[0], 0.001f);

            // Fundido cruzado entre el plano 1 y el 2, en los últimos 0,6 s del 1 (de 6,4 a 7 s).
            var (abajo, encima) = linea.ImagenesEn(6.7f);
            Assert.AreEqual(0, abajo.Indice);
            Assert.AreEqual(1f, abajo.Alfa, 0.001f, "La que sale sigue opaca debajo: el cruce no oscurece.");
            Assert.AreEqual(1, encima.Indice);
            Assert.AreEqual(0.5f, encima.Alfa, 0.01f);
            Assert.AreEqual(1f, Pesos(linea, 7f)[1], 0.001f, "A los 7 s ya solo se ve el plano 2.");

            Assert.AreEqual(0.5f, Pesos(linea, 79.7f)[11], 0.01f, "El último vuelve a negro.");
            Assert.IsTrue(linea.Terminada(80f));
            Assert.IsFalse(linea.Terminada(79.99f));
        }

        // --- Textos ---

        [Test]
        public void UnTextoSinTiempos_EntraALos08_YSeVa08AntesDelFinal()
        {
            var linea = Linea();
            const string texto = "Hubo una guerra.";
            var inicio = linea.InicioDe(1); // el plano 2 dura 8 s

            Assert.AreEqual(0f, CartelEn(linea, inicio + 0.7f, texto));
            Assert.AreEqual(0.5f, CartelEn(linea, inicio + 1.0f, texto), 0.01f, "Entra con un fundido de 0,4 s desde 0,8 s.");
            Assert.AreEqual(1f, CartelEn(linea, inicio + 4f, texto), 0.001f);
            Assert.AreEqual(0.5f, CartelEn(linea, inicio + 7.0f, texto), 0.01f, "Sale con un fundido que acaba 0,8 s antes del final.");
            Assert.AreEqual(0f, CartelEn(linea, inicio + 7.3f, texto));

            var cartel = linea.CartelesDe(1).Single();
            Assert.AreEqual(POSICIONES_CARTEL.ABAJO, cartel.posicion, "Tercio inferior.");
        }

        [Test]
        public void ElPlano4_EncadenaSusDosTextosConSusTiempos()
        {
            var linea = Linea();
            var inicio = linea.InicioDe(3);

            Assert.AreEqual(0f, CartelEn(linea, inicio + 0.4f, "Le dolía."));
            Assert.AreEqual(1f, CartelEn(linea, inicio + 1.5f, "Le dolía."), 0.001f);
            Assert.AreEqual(0f, CartelEn(linea, inicio + 3.2f, "Le dolía."), "Se ha ido a los 3 s.");
            Assert.AreEqual(0f, CartelEn(linea, inicio + 3.4f, "Así que decidió no sentirlo."));
            Assert.AreEqual(1f, CartelEn(linea, inicio + 5f, "Así que decidió no sentirlo."), 0.001f);
            Assert.AreEqual(0f, CartelEn(linea, inicio + 6.6f, "Así que decidió no sentirlo."), "Se ha ido a los 6,5 s.");
        }

        [Test]
        public void ElTitulo_SeVeArriba_EnElPlano11()
        {
            var linea = Linea();
            var visibles = linea.TextosEn(linea.InicioDe(10) + 2.5f);

            Assert.AreEqual(1, visibles.Count);
            Assert.AreEqual(POSICIONES_CARTEL.ARRIBA, visibles[0].Cartel.posicion);
            Assert.IsTrue(visibles[0].Cartel.titulo);
            Assert.AreEqual(1f, visibles[0].Alfa, 0.001f);
        }

        // --- Saltar ---

        [Test]
        public void NoSePuedeSaltar_AntesDeLos2Segundos()
        {
            var linea = Linea();

            Assert.IsFalse(linea.PuedeSaltar(0f));
            Assert.IsFalse(linea.PuedeSaltar(1.99f));
            Assert.IsTrue(linea.PuedeSaltar(2f));
            Assert.IsTrue(linea.PuedeSaltar(79f));
        }

        [Test]
        public void ElAvisoParaSaltar_ApareceALos2_YSeHaIdoALos5()
        {
            var linea = Linea();
            var opacidad = Inicial().opacidadAviso;

            Assert.AreEqual(0f, linea.AlfaAviso(1.9f));
            Assert.AreEqual(opacidad, linea.AlfaAviso(3.5f), 0.001f, "Discreto: opacidad baja.");
            Assert.Less(opacidad, 0.6f);
            Assert.AreEqual(0f, linea.AlfaAviso(5f), 0.001f);
            Assert.AreEqual(0f, linea.AlfaAviso(6f));
        }

        [Test]
        public void UnaCinematicaNoSaltable_NoMuestraElAviso()
        {
            var datos = ScriptableObject.CreateInstance<CutsceneData>();
            try
            {
                datos.saltable = false;
                datos.diapositivas.Add(new Diapositiva { texto = "uno", duracion = 10f });
                var linea = new LineaTiempoCinematica(datos);

                Assert.IsFalse(linea.PuedeSaltar(5f));
                Assert.AreEqual(0f, linea.AlfaAviso(3f));
            }
            finally
            {
                Object.DestroyImmediate(datos);
            }
        }
    }
}
