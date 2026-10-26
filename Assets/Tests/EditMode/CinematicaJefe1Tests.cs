using System.Collections.Generic;
using System.Linq;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Narrative;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La presentación del jefe del Nivel 1 (fase C), «El que ya estaba aquí», contra su guion
    /// (<c>GUION-cinematica-jefe1.md</c>): los cuatro beats con sus tiempos, los textos, los
    /// temblores, las brasas, el encuadre del ojo, el flash y los siete cues. Los valores
    /// esperados se copian del guion en segundos desde el inicio de la cinemática, no de la tabla
    /// del constructor. Y la capa de brasas: píxeles enteros que caen de uno en uno.
    /// </summary>
    public class CinematicaJefe1Tests
    {
        private static CutsceneData Presentacion()
        {
            var presentacion = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicasNivel1.RutaPresentacion);
            Assert.IsNotNull(presentacion, "Falta la presentación del jefe del Nivel 1: ejecuta Fragmentum → Regenerar todo.");
            return presentacion;
        }

        [Test]
        public void SonCuatroBeats_QueSumanNueveSeisSegundos()
        {
            var datos = Presentacion();
            Assert.AreEqual(MONTAJES_CINEMATICA.BEATS, datos.montaje);
            Assert.IsTrue(datos.escalaEntera, "Pixel art: la cámara de cinemáticas.");
            Assert.IsFalse(datos.sobreElJuego);
            Assert.AreEqual(9.6f, datos.DuracionTotal, 0.001f, "9,6 s ± 1 ms.");

            var beats = datos.diapositivas;
            Assert.AreEqual(4, beats.Count);
            Assert.IsNull(beats[0].imagen, "1: negro.");
            Assert.AreEqual(0.5f, beats[0].DuracionBeat, 0.001f);
            Assert.AreEqual("CinematicaJefe1", beats[1].imagen.name, "2: la sala.");
            Assert.AreEqual((1f, 4.9f, 0f), (beats[1].fundidoEntrada, beats[1].duracion, beats[1].fundidoSalida), "Sube en 1 s y corta en seco.");
            Assert.IsNull(beats[2].imagen, "3: negro, silencio.");
            Assert.AreEqual(0.5f, beats[2].DuracionBeat, 0.001f);
            Assert.IsEmpty(beats[2].cues, "En silencio.");
            Assert.AreEqual("CinematicaJefe2", beats[3].imagen.name, "4: el ojo.");
            Assert.AreEqual((1.1f, 1.6f, 0f), (beats[3].fundidoEntrada, beats[3].duracion, beats[3].fundidoSalida));
        }

        [Test]
        public void LaSala_TieneBrasas_YTiemblaUnPixelA2_4YA3_6()
        {
            var datos = Presentacion();
            var linea = new LineaTiempoCinematica(datos);
            Assert.IsTrue(datos.diapositivas[1].brasas);
            Assert.IsFalse(datos.diapositivas[3].brasas, "El ojo, sin brasas.");
            Assert.AreEqual(1, linea.TembloresEntre(2.35f, 2.45f));
            Assert.AreEqual(1, linea.TembloresEntre(3.55f, 3.65f));
            Assert.AreEqual(2, linea.TembloresEntre(-1f, 10f), "Solo esos dos.");
            Assert.AreEqual(1, datos.temblorPixeles, "De un píxel.");
        }

        [Test]
        public void ElTextoDeKael_DeCuatroASeisCuatro_BajoSuSilueta()
        {
            var datos = Presentacion();
            var linea = new LineaTiempoCinematica(datos);
            Assert.IsEmpty(linea.TextosEn(3.95f));
            var kael = linea.TextosEn(5f).Single().Cartel;
            Assert.AreEqual("No recuerdo esta sala.\nEl calor, sí.", kael.texto, "Las dos líneas juntas.");
            Assert.AreEqual(1f, linea.AlfaTexto(kael, 4.4f - 0.5f), 0.01f, "Entero desde las 4,4.");
            Assert.Greater(linea.AlfaTexto(kael, 6.2f - 0.5f), 0f, "Se va de 6,0 a 6,4…");
            Assert.Less(linea.AlfaTexto(kael, 6.2f - 0.5f), 1f);
            Assert.IsEmpty(linea.TextosEn(6.45f), "…y a las 6,4 ya no está.");

            // Abajo a la izquierda, alineado bajo Kael (columnas 22-25, pies en la fila 109).
            Assert.IsTrue(kael.enPunto);
            Assert.AreEqual(TextAnchor.UpperLeft, kael.alineacion);
            Assert.AreEqual(22, kael.punto.x);
            Assert.Greater(kael.punto.y, 109, "Debajo de sus pies.");
        }

        [Test]
        public void ElOjo_CaeDondeEstabaLaCabeza_YElRotuloEntraEnSecoALasOcho()
        {
            var datos = Presentacion();
            var linea = new LineaTiempoCinematica(datos);
            Assert.AreEqual(new Vector2Int(17, 19), datos.diapositivas[3].desplazamiento, "17 a la derecha, 19 arriba.");

            Assert.IsEmpty(linea.TextosEn(7.95f));
            var rotulo = linea.TextosEn(8.05f);
            CollectionAssert.AreEqual(new[] { "CENTINELA ARDIENTE", "Arquitecto de la Ira" }, rotulo.Select(t => t.Cartel.texto));
            Assert.IsTrue(rotulo.All(t => t.Alfa == 1f), "En seco, con el golpe metálico.");
            Assert.IsTrue(rotulo[0].Cartel.titulo);
            Assert.AreEqual(2, linea.TextosEn(9.59f).Count, "Hasta el final.");

            // Bajo el ojo, en píxeles de la imagen: el ojo llega a la fila 67.
            Assert.IsTrue(rotulo.All(t => t.Cartel.enPunto && t.Cartel.alineacion == TextAnchor.UpperCenter));
            Assert.Greater(rotulo[0].Cartel.punto.y, 67, "Sin tocar el ojo.");
            Assert.Greater(rotulo[1].Cartel.punto.y, rotulo[0].Cartel.punto.y);

            Assert.AreEqual(2, linea.FlashFinal, "Flash de 2 fotogramas.");
        }

        [Test]
        public void LosSieteCues_SuenanDondeDiceElGuion()
        {
            var cues = new LineaTiempoCinematica(Presentacion()).CuesEntre(-1f, 10f).ToDictionary(c => c.Cue.nombre);
            CollectionAssert.AreEquivalent(ConstructorCinematicasNivel1.CuesPresentacion, cues.Keys);

            void Comprobar(string nombre, float desde, float hasta, bool bucle)
            {
                Assert.AreEqual(desde, cues[nombre].Inicio, 0.001f, $"{nombre}: empieza.");
                if (float.IsPositiveInfinity(hasta))
                {
                    Assert.IsTrue(float.IsPositiveInfinity(cues[nombre].Fin), $"{nombre}: suena entero.");
                }
                else
                {
                    Assert.AreEqual(hasta, cues[nombre].Fin, 0.001f, $"{nombre}: se corta.");
                }

                Assert.AreEqual(bucle, cues[nombre].Cue.bucle, nombre);
            }

            Comprobar("drone_sala", 0f, 9.6f, true);
            Comprobar("amb_forja", 0.5f, 6.4f, true);
            Comprobar("golpe_escala", 1.6f, float.PositiveInfinity, false);
            Comprobar("respiracion_fuelle", 2.0f, 6.0f, true);
            Comprobar("voz_kael_01", 4.0f, float.PositiveInfinity, false);
            Comprobar("brasa_aviva", 6.9f, 8.0f, false);
            Comprobar("rotulo_metal", 8.0f, float.PositiveInfinity, false);
        }

        [Test]
        public void SeVeUnaVezPorPartida_YSoloElNegroInicialNoSeSalta()
        {
            var datos = Presentacion();
            Assert.IsTrue(datos.soloPrimeraVez, "La marca de «vista» va en GameManager y se guarda en la partida.");
            Assert.IsTrue(datos.saltable);
            Assert.AreEqual(0.5f, datos.bloqueoSalto, 0.001f, "Una tecla pulsada al entrar no se lleva el rótulo.");
            Assert.IsEmpty(datos.avisoSaltar, "Sin aviso para saltar encima del plano.");

            var nivel = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Data/Niveles/LevelDatabase.asset").niveles[0];
            Assert.AreSame(datos, nivel.cinematicaJefe, "El Nivel 1 la reproduce al entrar en la arena.");
        }

        [Test]
        public void Regenerar_ConservaLosClipsQueSePusieronAMano()
        {
            var copia = Object.Instantiate(Presentacion());
            var clip = AudioClip.Create("drone_de_prueba", 100, 1, 44100, false);
            try
            {
                copia.diapositivas[0].cues.Single(c => c.nombre == "drone_sala").clip = clip;
                ConstructorCinematicasNivel1.Presentacion(copia);
                Assert.AreSame(clip, copia.diapositivas[0].cues.Single(c => c.nombre == "drone_sala").clip);
                Assert.AreEqual(9.6f, copia.DuracionTotal, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(clip);
                Object.DestroyImmediate(copia);
            }
        }

        // --- Brasas ---

        [Test]
        public void LasBrasas_OcupanPixelesEnteros_YCaenDeUnoEnUno()
        {
            var objeto = new GameObject("Brasas");
            try
            {
                var brasas = objeto.AddComponent<CapaBrasas>();
                for (var i = 0; i < brasas.cantidad; i++)
                {
                    var antes = brasas.PixelDe(i, 2f);
                    Assert.That(antes.x, Is.InRange(0, 239));
                    Assert.That(antes.y, Is.InRange(0, brasas.filaSuelo - 1), "Por encima del suelo de la sala.");

                    // En un fotograma (1/60 s) avanzan como mucho un píxel: nunca saltan.
                    var despues = brasas.PixelDe(i, 2f + 1f / 60f);
                    Assert.LessOrEqual(Mathf.Abs(despues.y - antes.y) % (brasas.filaSuelo - 1), 1, $"Brasa {i}");
                }

                // Lo mismo cada vez: dependen solo del tiempo y de la semilla.
                Assert.AreEqual(brasas.PixelDe(3, 1.234f), brasas.PixelDe(3, 1.234f));

                brasas.Simular(1f, 0.5f);
                var dibujo = brasas.Dibujos[0];
                Assert.IsTrue(dibujo.enabled);
                var centro = dibujo.transform.localPosition;
                Assert.AreEqual(Mathf.Round(centro.x - 0.5f), centro.x - 0.5f, 1e-4f, "En el centro de un píxel de la rejilla.");
                Assert.AreEqual(Mathf.Round(centro.y), centro.y, 1e-4f);
                Assert.LessOrEqual(dibujo.color.a, 0.5f, "Con la opacidad del plano.");

                brasas.Fijar(false, 1f);
                Assert.IsTrue(brasas.Dibujos.All(d => !d.enabled), "Apagadas, no se ven.");
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void ElTexto_SeMueveConElEncuadreDeSuPlano()
        {
            var cartel = new Cartel { enPunto = true, punto = new Vector2Int(114, 77), alineacion = TextAnchor.UpperCenter };
            Assert.AreEqual(new Vector2Int(131, 58), CamaraCinematica.Anclaje(cartel, new Vector2Int(17, 19)).punto,
                "17 a la derecha y 19 arriba, como el ojo.");
            Assert.AreEqual(new Vector2Int(114, 77), CamaraCinematica.Anclaje(cartel).punto, "Sin encuadre, donde está en la imagen.");
        }
    }
}
