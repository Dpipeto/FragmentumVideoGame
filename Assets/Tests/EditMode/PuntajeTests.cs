using System.Linq;
using Fragmentum.Core.Puntuacion;
using Fragmentum.Core.Save;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El puntaje que sustituye a las monedas: las cuentas del bono por tiempo, cómo se guarda el
    /// mejor de cada nivel y que los assets del juego llevan los valores de la tabla (un mini-jefe
    /// vale más que un enemigo común y un jefe más que cualquiera de los dos).
    /// </summary>
    public class PuntajeTests
    {
        // --- El bono por tiempo ---

        [Test]
        public void BonoDeTiempo_DaPuntosPorCadaSegundoQueSobra()
        {
            Assert.AreEqual(1500, ReglasPuntaje.BonoDeTiempo(300f, 600f, 5), "Sobran 300 s a 5 puntos.");
            Assert.AreEqual(3000, ReglasPuntaje.BonoDeTiempo(0f, 600f, 5));
            Assert.AreEqual(5, ReglasPuntaje.BonoDeTiempo(598.2f, 600f, 5), "Solo cuentan los segundos enteros que sobran.");
        }

        [Test]
        public void BonoDeTiempo_CuantoMenosSeTarda_MasDa()
        {
            var rapido = ReglasPuntaje.BonoDeTiempo(240f, 600f, 5);
            var lento = ReglasPuntaje.BonoDeTiempo(480f, 600f, 5);

            Assert.Greater(rapido, lento);
        }

        [Test]
        public void BonoDeTiempo_PasadoElTiempo_NoHayBonoPeroTampocoSeResta()
        {
            Assert.AreEqual(0, ReglasPuntaje.BonoDeTiempo(600f, 600f, 5));
            Assert.AreEqual(0, ReglasPuntaje.BonoDeTiempo(5000f, 600f, 5));
            Assert.AreEqual(0, ReglasPuntaje.BonoDeTiempo(10f, 0f, 5), "Un nivel sin tiempo con bono no da bono.");
            Assert.AreEqual(0, ReglasPuntaje.BonoDeTiempo(10f, 600f, 0));
        }

        [Test]
        public void ElRelojYLosPuntos_SeEscribenLegibles()
        {
            Assert.AreEqual("00:00", ReglasPuntaje.Tiempo(0f));
            Assert.AreEqual("04:35", ReglasPuntaje.Tiempo(275.9f));
            Assert.AreEqual("75:02", ReglasPuntaje.Tiempo(4502f), "Pasada la hora siguen contando los minutos.");
            Assert.AreEqual("00:00", ReglasPuntaje.Tiempo(-3f));

            Assert.AreEqual("0", ReglasPuntaje.Puntos(0));
            Assert.AreEqual("600", ReglasPuntaje.Puntos(600));
            Assert.AreEqual("12 345", ReglasPuntaje.Puntos(12345));
        }

        [Test]
        public void Resultado_SumaSusTresPartes_YSoloEsRecordSiHabiaAlgoQueSuperar()
        {
            var primero = new ResultadoNivel(800, 1500, 1200, 360f, 0, false);
            Assert.AreEqual(3500, primero.Total);
            Assert.IsFalse(primero.EsRecord, "La primera vez no hay con qué comparar.");

            Assert.IsTrue(new ResultadoNivel(800, 1500, 1200, 360f, 3000, true).EsRecord);
            Assert.IsFalse(new ResultadoNivel(800, 1500, 1200, 360f, 3500, true).EsRecord, "Igualarlo no es superarlo.");
        }

        // --- El guardado ---

        [Test]
        public void AnotarPuntaje_GuardaElMejorDeCadaNivel()
        {
            var datos = new DatosGuardado();

            Assert.IsNull(datos.PuntajeDe(1));
            Assert.IsTrue(datos.AnotarPuntaje(1, 3000, 400f));
            Assert.IsFalse(datos.AnotarPuntaje(1, 2500, 200f), "Un puntaje peor no borra el mejor.");
            Assert.AreEqual(3000, datos.PuntajeDe(1).puntos);
            Assert.AreEqual(400f, datos.PuntajeDe(1).segundos);

            Assert.IsTrue(datos.AnotarPuntaje(1, 3600, 380f));
            Assert.AreEqual(3600, datos.PuntajeDe(1).puntos);
            Assert.AreEqual(1, datos.puntajes.Count, "Un solo registro por nivel.");
        }

        [Test]
        public void AnotarPuntaje_AIgualdadDePuntos_SeQuedaElMasRapido()
        {
            var datos = new DatosGuardado();
            datos.AnotarPuntaje(2, 4000, 500f);

            Assert.IsFalse(datos.AnotarPuntaje(2, 4000, 520f));
            Assert.IsTrue(datos.AnotarPuntaje(2, 4000, 470f));
            Assert.AreEqual(470f, datos.PuntajeDe(2).segundos);
        }

        [Test]
        public void PuntajeTotal_SumaLosMejoresDeTodosLosNiveles()
        {
            var datos = new DatosGuardado();
            Assert.AreEqual(0, datos.PuntajeTotal);

            datos.AnotarPuntaje(1, 3000, 400f);
            datos.AnotarPuntaje(2, 4500, 600f);
            datos.AnotarPuntaje(1, 3200, 390f);

            Assert.AreEqual(7700, datos.PuntajeTotal);
        }

        // --- La tabla, en los assets ---

        [Test]
        public void LosEnemigos_LlevanLosPuntosDeLaTabla()
        {
            foreach (var (asset, puntos) in TablaPuntaje.Enemigos)
            {
                var datos = AssetDatabase.LoadAssetAtPath<EnemyData>($"{GeneradorEnemigos.CarpetaDatos}/{asset}.asset");
                Assert.IsNotNull(datos, $"Falta el enemigo {asset}.");
                Assert.AreEqual(puntos, datos.puntos, $"{asset}: ejecuta Fragmentum → Progresión → Aplicar la tabla de puntaje.");
            }
        }

        [Test]
        public void UnMiniJefe_ValeVariasVecesUnEnemigoComun()
        {
            Assert.IsNotEmpty(TablaPuntaje.MiniJefes);
            foreach (var miniJefe in TablaPuntaje.MiniJefes)
            {
                var puntos = TablaPuntaje.PuntosDeEnemigo(miniJefe);
                Assert.GreaterOrEqual(puntos, TablaPuntaje.EnemigoComun * 4, $"{miniJefe} es un mini-jefe.");
            }

            var comunes = TablaPuntaje.Enemigos.Where(e => !TablaPuntaje.MiniJefes.Contains(e.asset)).ToList();
            var menorMiniJefe = TablaPuntaje.MiniJefes.Min(TablaPuntaje.PuntosDeEnemigo);
            Assert.IsTrue(comunes.All(e => e.puntos < menorMiniJefe), "Ningún enemigo común vale lo que un mini-jefe.");
            Assert.Greater(comunes.Select(e => e.puntos).Distinct().Count(), 1, "Los enemigos no valen todos lo mismo.");
        }

        [Test]
        public void LosJefes_ValenMasQueCualquierEnemigo_YCadaMundoMasQueElAnterior()
        {
            var mayorEnemigo = TablaPuntaje.Enemigos.Max(e => e.puntos);
            var anterior = mayorEnemigo;
            foreach (var (asset, puntos) in TablaPuntaje.Jefes)
            {
                var datos = AssetDatabase.LoadAssetAtPath<JefeData>($"{GeneradorJefes.CarpetaDatos}/{asset}.asset");
                Assert.IsNotNull(datos, $"Falta el jefe {asset}.");
                Assert.AreEqual(puntos, datos.puntos, $"{asset}: ejecuta Fragmentum → Progresión → Aplicar la tabla de puntaje.");
                Assert.Greater(puntos, anterior, $"{asset} vale más que lo anterior.");
                anterior = puntos;
            }
        }

        [Test]
        public void LosNiveles_TienenTiempoConBono()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            foreach (var (id, tiempoConBono) in TablaPuntaje.Niveles)
            {
                var nivel = bd.niveles.Find(n => n != null && n.id == id);
                Assert.IsNotNull(nivel, $"Falta el nivel {id}.");
                Assert.AreEqual(tiempoConBono, nivel.tiempoConBono, 0.01f);
                Assert.AreEqual(TablaPuntaje.PuntosPorSegundo, nivel.puntosPorSegundo);
                Assert.Greater(nivel.tiempoConBono, 120f, "Menos de dos minutos no deja bono a nadie.");
            }
        }

        [Test]
        public void ElBonoDeTiempo_NoAplastaAlCombate()
        {
            // Cruzar el Nivel 1 en la mitad de su tiempo con bono vale del orden de su jefe: correr
            // y pelear compiten, ninguno de los dos decide solo el puntaje.
            var (_, tiempo) = TablaPuntaje.Niveles.First(n => n.nivel == 1);
            var bono = ReglasPuntaje.BonoDeTiempo(tiempo * 0.5f, tiempo, TablaPuntaje.PuntosPorSegundo);
            var jefe = TablaPuntaje.Jefes[0].puntos;

            Assert.That(bono, Is.InRange(jefe / 2, jefe * 2));
        }

        // --- La letra ---

        [Test]
        public void LasDosLetras_EstanEnElProyecto_ConSuLicencia()
        {
            Assert.IsNotNull(Resources.Load<Font>(FabricaUI.RutaFuenteTitulos), "Falta la letra de los títulos.");
            Assert.IsNotNull(Resources.Load<Font>(FabricaUI.RutaFuenteTexto), "Falta la letra de los diálogos.");
            Assert.IsNotNull(Resources.Load<Font>(FabricaUI.RutaFuenteTexto + "-Italic"), "Falta la cursiva de los diálogos.");
            Assert.IsNotNull(Resources.Load<TextAsset>("Fuentes/Cinzel-LICENCIA-OFL"), "La licencia OFL acompaña a la fuente.");
            Assert.IsNotNull(Resources.Load<TextAsset>("Fuentes/Alegreya-LICENCIA-OFL"), "La licencia OFL acompaña a la fuente.");
        }

        [Test]
        public void LosTitulosYLosTextos_UsanCadaUnoSuLetra()
        {
            var raiz = new GameObject("Raiz", typeof(RectTransform));
            try
            {
                var titulo = FabricaUI.Titulo(raiz.transform, "Titulo", "FRAGMENTUM", 40, Color.white);
                var texto = FabricaUI.Texto(raiz.transform, "Texto", "Una línea de Kael.", 26, Color.white);
                var boton = FabricaUI.Boton(raiz.transform, "Boton", "Continuar", Color.black, 60f);

                Assert.AreSame(FabricaUI.FuenteTitulos, titulo.font);
                Assert.AreSame(FabricaUI.Fuente, texto.font);
                Assert.AreSame(FabricaUI.FuenteTitulos, boton.GetComponentInChildren<UnityEngine.UI.Text>().font, "Los botones del menú, con la letra de los títulos.");
                Assert.AreNotSame(FabricaUI.Fuente, FabricaUI.FuenteTitulos);
                StringAssert.Contains("Cinzel", FabricaUI.FuenteTitulos.name);
                StringAssert.Contains("Alegreya", FabricaUI.Fuente.name);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }
    }
}
