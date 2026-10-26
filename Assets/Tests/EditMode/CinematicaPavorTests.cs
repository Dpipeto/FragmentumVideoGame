using System.IO;
using System.Linq;
using Fragmentum.Core;
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
    /// Las cinemáticas del Espectro del Pavor (fase F del Mundo 3): sus cuatro planos de 480 × 268,
    /// la presentación (cuatro beats, su línea del guion repartida entre los dos planos y su rótulo
    /// arriba a la izquierda, sobre el negro) y la derrota, sin texto. Y que el texto de la
    /// presentación y el de la salida del Miedo caen sobre zonas oscuras de sus planos: la letra es
    /// clara.
    /// </summary>
    public class CinematicaPavorTests
    {
        private static readonly Vector2Int Rejilla = new Vector2Int(480, 268);

        private static CutsceneData Cargar(string ruta)
        {
            var cinematica = AssetDatabase.LoadAssetAtPath<CutsceneData>(ruta);
            Assert.IsNotNull(cinematica, $"Falta {ruta}: ejecuta Fragmentum → Regenerar todo.");
            return cinematica;
        }

        private static CutsceneData Presentacion() => Cargar(ConstructorCinematicasNivel3.RutaPresentacion);

        // --- Los planos ---

        [Test]
        public void LosPlanosDelPavorYDeLaSalida_MidenCuatrocientosOchentaPorDoscientosSesentaYOcho_AUnPixelPorUnidad()
        {
            Assert.IsTrue(ConstructorCinematicasNivel3.HayPlanos);
            Assert.AreEqual(Rejilla, ConstructorCinematicasNivel3.Rejilla, "La rejilla de los de la Doncella.");
            foreach (var plano in ConstructorCinematicasNivel3.Planos.Concat(ConstructorPuertas.Miedo.Planos))
            {
                var ruta = ConstructorCinematicasNivel3.RutaImagen(plano);
                Assert.IsNotNull(TablaArte.ReglaDe(ruta), $"{plano} tiene su regla de importación.");
                var sprite = ConstructorCinematicasNivel3.Imagen(plano);
                Assert.IsNotNull(sprite, $"{plano} está importado como sprite.");
                Assert.AreEqual((Vector2)Rejilla, sprite.rect.size, plano);
                Assert.AreEqual(CamaraCinematica.PixelesPorUnidad, sprite.pixelsPerUnit, $"{plano}: un píxel del plano, uno de la rejilla.");
                Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, plano);
                CollectionAssert.IsEmpty(VerificadorNivel1.ProblemasDeSprite(ruta, AssetImporter.GetAtPath(ruta) as TextureImporter));
            }
        }

        // --- La presentación ---

        [Test]
        public void LaPresentacion_SonCuatroBeats_EnSuRejilla_YNoPasaDeDiezSegundos()
        {
            var datos = Presentacion();
            Assert.AreEqual(MONTAJES_CINEMATICA.BEATS, datos.montaje);
            Assert.IsTrue(datos.escalaEntera, "Pixel art: la cámara de cinemáticas.");
            Assert.IsFalse(datos.sobreElJuego);
            Assert.AreEqual(Rejilla, datos.Rejilla, "En la rejilla de sus planos.");
            Assert.AreEqual(9.8f, datos.DuracionTotal, 0.001f, "Las presentaciones de jefe no pasan de 10 s.");

            var beats = datos.diapositivas;
            Assert.AreEqual(4, beats.Count);
            Assert.IsNull(beats[0].imagen, "1: negro.");
            Assert.AreEqual(ConstructorCinematicasNivel3.Presentacion1, beats[1].imagen.name, "2: el claro a oscuras y la máscara mirando a Kael.");
            Assert.AreEqual((0.7f, 3.8f, 0.3f), (beats[1].fundidoEntrada, beats[1].duracion, beats[1].fundidoSalida));
            Assert.IsNull(beats[2].imagen, "3: negro.");
            Assert.AreEqual(ConstructorCinematicasNivel3.Presentacion2, beats[3].imagen.name, "4: él, de cerca.");
            Assert.AreEqual((0.6f, 3.6f, 0.3f), (beats[3].fundidoEntrada, beats[3].duracion, beats[3].fundidoSalida));
            Assert.IsTrue(beats.All(b => b.cues.Count == 0 && b.audio == null), "Sin efectos de sonido.");
            Assert.IsNotNull(datos.musica, "Su música: Musica/Cinematicas/Jefe_Miedo.");
            Assert.AreSame(MusicaNiveles.DeCinematica("Jefe_Miedo"), datos.musica);

            Assert.IsTrue(datos.soloPrimeraVez);
            Assert.IsTrue(datos.llevaDialogoDelJefe, "Su línea ya va dentro: el HUD no la repite al empezar el combate.");
            Assert.IsTrue(datos.saltable);
        }

        [Test]
        public void DiceSuLineaDelGuion_RepartidaEntreLosDosPlanos_YDespuesEntraSuRotulo()
        {
            var datos = Presentacion();
            var claro = datos.diapositivas[1].carteles.Single();
            var cerca = datos.diapositivas[3].carteles;
            Assert.AreEqual(3, cerca.Count, "Su frase, el rótulo y el nombre del mundo.");
            Assert.AreEqual(GuionMundos.De(EMOCIONES.MIEDO).AntesDelJefe, $"{claro.texto} {cerca[0].texto}",
                "Las dos mitades son su línea del guion, sin tocar una letra.");

            var nivel = ConstructorCinematicasNivel3.Nivel();
            Assert.AreEqual(nivel.jefe.nombreCanonico.ToUpperInvariant(), cerca[1].texto);
            Assert.IsTrue(cerca[1].titulo);
            Assert.AreEqual(nivel.nombreMundo, cerca[2].texto);
            Assert.IsFalse(cerca[2].titulo);
            Assert.GreaterOrEqual(cerca[2].punto.y, cerca[1].punto.y + datos.tamanoFuente * 2, "El nombre del mundo, debajo del rótulo.");

            // Su frase y su rótulo van en el mismo sitio: uno detrás de otro.
            Assert.Less(cerca[0].Fin, cerca[1].inicio, "El rótulo entra cuando su frase ya se ha ido.");
            Assert.LessOrEqual(cerca[1].Fin, datos.diapositivas[3].fundidoEntrada + datos.diapositivas[3].duracion + 1e-3f, "Y se va cuando el plano empieza a irse.");
            var linea = new LineaTiempoCinematica(datos);
            CollectionAssert.IsEmpty(linea.TextosEn(0.8f), "Mientras el claro sube desde negro, nadie habla.");
            CollectionAssert.AreEqual(new[] { claro.texto }, linea.TextosEn(3f).Select(t => t.Cartel.texto).ToList());
            CollectionAssert.AreEqual(new[] { cerca[1].texto, cerca[2].texto }, linea.TextosEn(9f).Select(t => t.Cartel.texto).ToList());

            // Da tiempo a leerla.
            Assert.LessOrEqual(claro.texto.Length / claro.duracion, 17f, claro.texto);
            Assert.LessOrEqual(cerca[0].texto.Length / cerca[0].duracion, 18f, cerca[0].texto);
            Assert.AreEqual(ConstructorCinematicasNivel3.TamanoFuente, datos.tamanoFuente);
        }

        [Test]
        public void SuTexto_YElDeLaSalida_CaenSobreZonasOscurasDeSusPlanos()
        {
            var presentacion = Presentacion();
            foreach (var beat in presentacion.diapositivas.Where(d => d.imagen != null))
            {
                foreach (var cartel in beat.carteles)
                {
                    ComprobarQueSeLee(beat.imagen.name, cartel, presentacion.tamanoFuente);
                }
            }

            var salida = Cargar(ConstructorPuertas.Miedo.RutaCinematica);
            foreach (var beat in salida.diapositivas.Where(d => d.imagen != null))
            {
                foreach (var cartel in beat.carteles)
                {
                    ComprobarQueSeLee(beat.imagen.name, cartel, salida.tamanoFuente);
                }
            }
        }

        // La caja que ocupa más o menos el texto (media letra de ancho por carácter) tiene que ser
        // oscura de media: la letra es clara.
        private static void ComprobarQueSeLee(string plano, Cartel cartel, int tamanoFuente)
        {
            Assert.IsTrue(cartel.enPunto, cartel.texto);
            Assert.AreEqual(TextAnchor.UpperCenter, cartel.alineacion, cartel.texto);
            var alto = tamanoFuente * (cartel.titulo ? 2 : 1);
            var ancho = Mathf.Min(Rejilla.x, cartel.texto.Length * alto * 0.55f);
            var x0 = Mathf.Max(0, Mathf.RoundToInt(cartel.punto.x - ancho / 2f));
            var x1 = Mathf.Min(Rejilla.x, Mathf.RoundToInt(cartel.punto.x + ancho / 2f));
            Assert.LessOrEqual(cartel.punto.y + alto, Rejilla.y, $"«{cartel.texto}» cabe en el plano.");
            Assert.That(cartel.punto.x - ancho / 2f, Is.GreaterThanOrEqualTo(-1f), $"«{cartel.texto}» no se sale por la izquierda.");
            Assert.That(cartel.punto.x + ancho / 2f, Is.LessThanOrEqualTo(Rejilla.x + 1f), $"«{cartel.texto}» no se sale por la derecha.");

            var textura = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(textura.LoadImage(File.ReadAllBytes(ConstructorCinematicasNivel3.RutaImagen(plano))), plano);
                var pixeles = textura.GetPixels32();
                var suma = 0f;
                var cuantos = 0;
                for (var fila = cartel.punto.y; fila < cartel.punto.y + alto; fila++)
                {
                    var y = textura.height - 1 - fila; // las filas del plano se cuentan desde arriba
                    for (var x = x0; x < x1; x++)
                    {
                        suma += ((Color)pixeles[y * textura.width + x]).grayscale;
                        cuantos++;
                    }
                }

                Assert.Less(suma / cuantos, 0.25f, $"{plano}: «{cartel.texto}» va sobre una zona oscura.");
            }
            finally
            {
                Object.DestroyImmediate(textura);
            }
        }

        // --- La derrota ---

        [Test]
        public void LaDerrota_SonSusDosPlanos_SinTexto_YElNivel3LlevaLasDosCinematicas()
        {
            var derrota = Cargar(ConstructorCinematicasNivel3.RutaDerrota);
            Assert.IsTrue(derrota.escalaEntera);
            Assert.AreEqual(Rejilla, derrota.Rejilla);
            Assert.IsFalse(derrota.soloPrimeraVez, "Cada vez que cae.");
            CollectionAssert.AreEqual(new[] { ConstructorCinematicasNivel3.Derrota1, ConstructorCinematicasNivel3.Derrota2 },
                derrota.diapositivas.Select(d => d.imagen.name).ToList());
            Assert.IsTrue(derrota.diapositivas.All(d => Mathf.Approximately(d.duracion, ConstructorCinematicasNivel3.DuracionPlanoFinal)));
            Assert.IsTrue(derrota.diapositivas.All(d => string.IsNullOrEmpty(d.texto) && d.carteles.Count == 0), "Sin texto, como las otras dos.");

            var nivel = ConstructorCinematicasNivel3.Nivel();
            Assert.IsNotNull(nivel);
            Assert.AreSame(Presentacion(), nivel.cinematicaJefe);
            Assert.AreSame(derrota, nivel.cinematicaJefeDerrotado);
            CollectionAssert.IsEmpty(VerificadorNivel3.ProblemasDeLasCinematicas());
        }

        // --- La salida ---

        [Test]
        public void LaSalidaDelMiedo_DaTiempoALeerSuFraseLarga()
        {
            var frases = GuionMundos.De(EMOCIONES.MIEDO).CinematicaCierre;
            Assert.AreEqual(2.6f, ConstructorPuertas.ParaLeer(frases[0]), 1e-4f, "Una frase corta, lo de siempre.");
            Assert.AreEqual(2.7f, ConstructorPuertas.ParaLeer(frases[1]), 1e-4f, "«El bosque no era un laberinto…» tiene 80 letras: 2,7 s.");

            var general = Cargar(ConstructorPuertas.Miedo.RutaCinematica).diapositivas.First(d => d.imagen != null);
            Assert.AreEqual(3.7f, general.carteles[1].inicio, 1e-4f, "La segunda entra donde siempre…");
            Assert.AreEqual(6.4f, general.duracion, 1e-4f, "…y el plano dura lo de siempre: cabe justa.");
        }
    }
}
