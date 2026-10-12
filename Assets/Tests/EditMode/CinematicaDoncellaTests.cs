using System.Linq;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Narrative;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Las cinemáticas de la Doncella del Lamento (fase F del Mundo 2): sus cuatro planos de
    /// 480 × 268, la presentación (cuatro beats, su línea del guion repartida entre los dos planos y
    /// su rótulo) y la derrota, las dos en su propia rejilla. Y la cámara de cinemáticas, que ahora
    /// cambia de rejilla con cada cinemática sin que las de 240 × 135 se enteren.
    /// </summary>
    public class CinematicaDoncellaTests
    {
        private static readonly Vector2Int Rejilla = new Vector2Int(480, 268);

        private static CutsceneData Presentacion()
        {
            var presentacion = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicasNivel2.RutaPresentacion);
            Assert.IsNotNull(presentacion, "Falta la presentación de la Doncella: ejecuta Fragmentum → Regenerar todo.");
            return presentacion;
        }

        private static CutsceneData Derrota()
        {
            var derrota = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicasNivel2.RutaDerrota);
            Assert.IsNotNull(derrota, "Falta la cinemática de la derrota de la Doncella: ejecuta Fragmentum → Regenerar todo.");
            return derrota;
        }

        // --- Los planos ---

        [Test]
        public void LosCuatroPlanos_MidenCuatrocientosOchentaPorDoscientosSesentaYOcho_AUnPixelPorUnidad()
        {
            Assert.IsTrue(ConstructorCinematicasNivel2.HayPlanos);
            Assert.AreEqual(Rejilla, ConstructorCinematicasNivel2.Rejilla, "La decisión N34: van a 480, no a 240.");
            foreach (var plano in ConstructorCinematicasNivel2.Planos)
            {
                var ruta = ConstructorCinematicasNivel2.RutaImagen(plano);
                Assert.IsNotNull(TablaArte.ReglaDe(ruta), $"{plano} tiene su regla de importación.");
                var sprite = ConstructorCinematicasNivel2.Imagen(plano);
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
            Assert.AreEqual(0.3f, beats[0].DuracionBeat, 0.001f);
            Assert.AreEqual(ConstructorCinematicasNivel2.Presentacion1, beats[1].imagen.name, "2: la nave helada, con ella subiendo por la grieta.");
            Assert.AreEqual((0.7f, 3.8f, 0.3f), (beats[1].fundidoEntrada, beats[1].duracion, beats[1].fundidoSalida));
            Assert.IsNull(beats[2].imagen, "3: negro.");
            Assert.AreEqual(0.2f, beats[2].DuracionBeat, 0.001f);
            Assert.AreEqual(ConstructorCinematicasNivel2.Presentacion2, beats[3].imagen.name, "4: su rostro.");
            Assert.AreEqual((0.6f, 3.6f, 0.3f), (beats[3].fundidoEntrada, beats[3].duracion, beats[3].fundidoSalida));

            // Triste y quieta: ni brasas, ni temblores, ni flash. Y sin sonido, que no lo hay.
            Assert.IsTrue(beats.All(b => !b.brasas && b.temblores.Count == 0 && b.flashFinal == 0 && b.desplazamiento == Vector2Int.zero));
            Assert.IsTrue(beats.All(b => b.cues.Count == 0 && b.audio == null));
            Assert.IsNull(datos.musica);
        }

        [Test]
        public void DiceSuLineaDelGuion_RepartidaEntreLosDosPlanos_YAlFinalEntraSuRotulo()
        {
            var datos = Presentacion();
            var guion = GuionMundos.De(EMOCIONES.TRISTEZA).AntesDelJefe;
            var nave = datos.diapositivas[1].carteles.Single();
            var rostro = datos.diapositivas[3].carteles;
            Assert.AreEqual(3, rostro.Count, "La pregunta, el rótulo y el nombre del mundo.");
            var pregunta = rostro[0];
            Assert.AreEqual(guion, $"{nave.texto} {pregunta.texto}", "Las dos mitades son su línea del guion, sin tocar una letra.");
            StringAssert.StartsWith("¿", pregunta.texto, "De cerca, pregunta.");

            var nivel = ConstructorCinematicasNivel2.Nivel();
            Assert.AreEqual(nivel.jefe.nombreCanonico.ToUpperInvariant(), rostro[1].texto);
            Assert.IsTrue(rostro[1].titulo);
            Assert.AreEqual(nivel.nombreMundo, rostro[2].texto);
            Assert.IsFalse(rostro[2].titulo);

            // En el tiempo: su línea sobre la nave; la pregunta y, cuando se ha ido, el rótulo.
            var linea = new LineaTiempoCinematica(datos);
            CollectionAssert.AreEqual(new[] { nave.texto }, linea.TextosEn(3f).Select(t => t.Cartel.texto).ToList());
            CollectionAssert.IsEmpty(linea.TextosEn(0.8f), "Mientras la nave sube desde negro, nadie habla.");
            CollectionAssert.AreEqual(new[] { pregunta.texto }, linea.TextosEn(6.5f).Select(t => t.Cartel.texto).ToList());
            CollectionAssert.AreEqual(new[] { rostro[1].texto, rostro[2].texto }, linea.TextosEn(9f).Select(t => t.Cartel.texto).ToList());
            Assert.Less(pregunta.Fin, rostro[1].inicio, "El rótulo entra cuando la pregunta ya se ha ido.");

            // Da tiempo a leerla: no más de 17 letras por segundo.
            Assert.LessOrEqual(nave.texto.Length / nave.duracion, 17f, nave.texto);
            Assert.LessOrEqual(pregunta.texto.Length / pregunta.duracion, 18f, pregunta.texto);

            // Todo centrado y dentro del plano, con la letra a 10 px (40 en pantalla a ×4, como la de la Centinela).
            Assert.AreEqual(ConstructorCinematicasNivel2.TamanoFuente, datos.tamanoFuente);
            foreach (var cartel in rostro.Append(nave))
            {
                Assert.IsTrue(cartel.enPunto, cartel.texto);
                Assert.AreEqual(Rejilla.x / 2, cartel.punto.x, cartel.texto);
                Assert.AreEqual(TextAnchor.UpperCenter, cartel.alineacion, cartel.texto);
                var alto = datos.tamanoFuente * (cartel.titulo ? 2 : 1);
                Assert.LessOrEqual(cartel.punto.y + alto, Rejilla.y, $"{cartel.texto}: cabe en el plano.");
            }

            Assert.GreaterOrEqual(rostro[2].punto.y, rostro[1].punto.y + datos.tamanoFuente * 2, "El nombre del mundo, debajo del rótulo.");
        }

        [Test]
        public void SeVeUnaVezPorPartida_LlevaLaLineaDelJefe_YSeSaltaEnSeguida()
        {
            var datos = Presentacion();
            Assert.IsTrue(datos.soloPrimeraVez);
            Assert.IsTrue(datos.llevaDialogoDelJefe, "Su línea ya va dentro: el HUD no la repite al empezar el combate.");
            Assert.IsTrue(datos.saltable);
            Assert.AreEqual(0.5f, datos.bloqueoSalto, 1e-4f);
        }

        [Test]
        public void SuLinea_SePartePorLaUltimaFrase()
        {
            Assert.AreEqual(("Uno.", "¿Dos?"), ConstructorCinematicasNivel2.Partir("Uno. ¿Dos?"));
            Assert.AreEqual(("Uno. Dos…", "¡Tres!"), ConstructorCinematicasNivel2.Partir("  Uno. Dos… ¡Tres!  "));
            Assert.AreEqual((string.Empty, "Una sola."), ConstructorCinematicasNivel2.Partir("Una sola."), "Con una frase, va entera en su rostro.");
            Assert.AreEqual((string.Empty, string.Empty), ConstructorCinematicasNivel2.Partir(null));
        }

        // --- La derrota ---

        [Test]
        public void LaDerrota_SonSusDosPlanos_SinTexto_YElNivel2LlevaLasDosCinematicas()
        {
            var derrota = Derrota();
            Assert.IsTrue(derrota.escalaEntera);
            Assert.AreEqual(Rejilla, derrota.Rejilla);
            Assert.IsFalse(derrota.soloPrimeraVez, "Cada vez que cae.");
            CollectionAssert.AreEqual(new[] { ConstructorCinematicasNivel2.Derrota1, ConstructorCinematicasNivel2.Derrota2 },
                derrota.diapositivas.Select(d => d.imagen.name).ToList());
            Assert.IsTrue(derrota.diapositivas.All(d => Mathf.Approximately(d.duracion, ConstructorCinematicasNivel2.DuracionPlanoFinal)));
            Assert.IsTrue(derrota.diapositivas.All(d => string.IsNullOrEmpty(d.texto) && d.carteles.Count == 0), "Sin texto, como la de la Ira.");

            var nivel = ConstructorCinematicasNivel2.Nivel();
            Assert.IsNotNull(nivel);
            Assert.AreSame(Presentacion(), nivel.cinematicaJefe);
            Assert.AreSame(derrota, nivel.cinematicaJefeDerrotado);
        }

        // --- La rejilla ---

        [Test]
        public void LaRejillaDeCuatrocientosOchenta_SeAmpliaPorCuatroEn1080_YPorDosEn720()
        {
            Assert.AreEqual(new RectInt(0, 4, 1920, 1072), CamaraCinematica.RectEnPantalla(1920, 1080, Rejilla), "×4, con 4 px de franja arriba y abajo.");
            Assert.AreEqual(new RectInt(160, 92, 960, 536), CamaraCinematica.RectEnPantalla(1280, 720, Rejilla), "×2, con borde negro.");
            Assert.AreEqual(new RectInt(0, 0, 1920, 1080), CamaraCinematica.RectEnPantalla(1920, 1080), "La de siempre no cambia: ×8 justo.");
            Assert.AreEqual(CamaraCinematica.RectEnPantalla(1280, 720), CamaraCinematica.RectEnPantalla(1280, 720, CamaraCinematica.RejillaPorDefecto));

            // Los tercios de un texto sin punto propio son los de su rejilla.
            var abajo = new Cartel { posicion = POSICIONES_CARTEL.ABAJO };
            Assert.AreEqual(new Vector2Int(240, 268 - 268 / 6), CamaraCinematica.Anclaje(abajo, Vector2Int.zero, Rejilla).punto);
            Assert.AreEqual(new Vector2Int(240, 134), CamaraCinematica.Anclaje(new Cartel { posicion = POSICIONES_CARTEL.CENTRO }, Vector2Int.zero, Rejilla).punto);
            Assert.AreEqual(new Vector2Int(120, 135 - 135 / 6), CamaraCinematica.Anclaje(abajo).punto, "Y en la de siempre, los de siempre.");

            // Una cinemática sin rejilla propia usa la de por defecto.
            Assert.AreEqual(new Vector2Int(240, 135), CamaraCinematica.RejillaPorDefecto);
            Assert.AreEqual(CamaraCinematica.RejillaPorDefecto, CutsceneData.RejillaPorDefecto);
            var datos = ScriptableObject.CreateInstance<CutsceneData>();
            try
            {
                Assert.AreEqual(CamaraCinematica.RejillaPorDefecto, datos.Rejilla);
                datos.rejilla = Vector2Int.zero;
                Assert.AreEqual(CamaraCinematica.RejillaPorDefecto, datos.Rejilla, "Una rejilla vacía no vale: la de por defecto.");
            }
            finally
            {
                Object.DestroyImmediate(datos);
            }

            var ira = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicasNivel1.RutaPresentacion);
            Assert.AreEqual(CamaraCinematica.RejillaPorDefecto, ira.Rejilla, "La de la Centinela sigue en 240 × 135.");
        }

        [Test]
        public void LaCamara_CambiaDeRejilla_YVuelveALaDeSiempre()
        {
            var camara = CamaraCinematica.Crear(null);
            try
            {
                Assert.AreEqual(CamaraCinematica.RejillaPorDefecto, camara.Rejilla);
                Assert.AreEqual(240, camara.PixelPerfect.refResolutionX);

                camara.FijarRejilla(Rejilla);
                Assert.AreEqual(Rejilla, camara.Rejilla);
                Assert.AreEqual(480, camara.PixelPerfect.refResolutionX);
                Assert.AreEqual(268, camara.PixelPerfect.refResolutionY);
                Assert.AreEqual(134f, camara.Camara.orthographicSize, 1e-4f, "Ve los 268 píxeles del plano.");
                Assert.AreEqual(new Vector3(480f, 268f, 1f), camara.transform.Find("Flash").localScale, "El flash cubre la rejilla entera.");

                camara.FijarRejilla(Vector2Int.zero);
                Assert.AreEqual(CamaraCinematica.RejillaPorDefecto, camara.Rejilla);
                Assert.AreEqual(135, camara.PixelPerfect.refResolutionY);
                Assert.AreEqual(67.5f, camara.Camara.orthographicSize, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(camara.gameObject);
            }

            Assert.AreEqual(new Vector3(-240f, 134f, 0f), TextoEnRejilla.EnElMundo(0, 0, Rejilla), "La esquina de arriba a la izquierda del plano.");
            Assert.AreEqual(new Vector3(-120f, 67.5f, 0f), TextoEnRejilla.EnElMundo(0, 0));
        }

        [Test]
        public void SuTexto_MideEnPantallaLoMismoQueElDeLaCentinela_YCaeEnSuPixel()
        {
            var datos = Presentacion();
            var subtitulo = datos.diapositivas[3].carteles[2];
            var rotulo = datos.diapositivas[3].carteles[1];
            var objeto = new GameObject("Texto", typeof(RectTransform));
            try
            {
                var texto = objeto.AddComponent<Text>();
                var sombra = objeto.AddComponent<Shadow>();
                var rejilla = CamaraCinematica.RectEnPantalla(1920, 1080, datos.Rejilla);

                PantallaCinematica.ColocarSobreRejilla(texto, sombra, subtitulo, datos, rejilla, 1f);
                Assert.AreEqual(40, texto.fontSize, "10 px del plano a ×4: los 40 de la Centinela (5 px a ×8).");
                Assert.AreEqual(new Vector2(240 * 4, 4 + (268 - ConstructorCinematicasNivel2.PuntoSubtitulo.y) * 4), texto.rectTransform.anchoredPosition,
                    "Anclado en su píxel del plano, contando la franja de 4 px.");

                PantallaCinematica.ColocarSobreRejilla(texto, sombra, rotulo, datos, rejilla, 1f);
                Assert.AreEqual(80, texto.fontSize, "El rótulo, al doble.");

                // Y la de la Centinela, como siempre.
                var ira = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicasNivel1.RutaPresentacion);
                var deKael = ira.diapositivas[1].carteles[0];
                PantallaCinematica.ColocarSobreRejilla(texto, sombra, deKael, ira, CamaraCinematica.RectEnPantalla(1920, 1080), 1f);
                Assert.AreEqual(40, texto.fontSize);
                Assert.AreEqual(new Vector2(deKael.punto.x * 8, (135 - deKael.punto.y) * 8), texto.rectTransform.anchoredPosition);
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }
    }
}
