using System.Collections.Generic;
using System.Linq;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Levels;
using Fragmentum.Narrative;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El arte de lo que abre y cierra cada nivel (tanda del 2026-10-13): la puerta de salida, la
    /// verja de la arena, el checkpoint y la cinemática de salida, en los tres mundos montados.
    /// </summary>
    public class PuertasTests
    {
        private static IEnumerable<ConstructorPuertas.Juego> Juegos => ConstructorPuertas.Juegos;

        private static LevelData Nivel(int id) =>
            AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos).niveles.Single(n => n.id == id);

        // --- El arte importado ---

        [Test]
        public void LasPuertas_EstanImportadas_MidenUnas4u_YApoyanEnElSuelo()
        {
            foreach (var juego in Juegos)
            {
                var dibujos = ConstructorPuertas.Puerta(juego);
                Assert.AreEqual(4, dibujos.Length);
                foreach (var dibujo in dibujos)
                {
                    Assert.IsNotNull(dibujo, $"Falta un dibujo de la puerta de {juego.Clave}: python Herramientas/traer_arte_puertas.py.");
                    Assert.AreEqual(juego.PixelesPorUnidadPuerta, dibujo.pixelsPerUnit, 1e-3f, dibujo.name);
                    Assert.AreEqual(juego.LienzoPuerta, new Vector2Int((int)dibujo.rect.width, (int)dibujo.rect.height), "No se recorta ni se reescala.");
                    // Lo que mide por encima del suelo: la de la Culpa, al abrirse, deja caer sus cadenas a su pie.
                    Assert.That(juego.AltoPuerta, Is.InRange(3.9f, 4.1f), $"{dibujo.name}: algo más del doble que Kael.");
                    Assert.LessOrEqual(dibujo.bounds.size.y - juego.AltoPuerta, 0.3f, $"{dibujo.name}: bajo el suelo, como mucho sus cadenas.");
                    Assert.AreEqual(dibujo.rect.width * 0.5f, dibujo.pivot.x, 0.6f, $"{dibujo.name}: el pivote en el centro…");
                    Assert.AreEqual(dibujo.rect.height - juego.SueloPuerta, dibujo.pivot.y, 0.01f, "…y en la fila en que apoya.");
                    Assert.AreEqual(FilterMode.Point, dibujo.texture.filterMode, "Pixel art.");
                }
            }
        }

        [Test]
        public void LasVerjas_YLosCheckpoints_EstanImportados_ConElPivoteEnSuPie()
        {
            foreach (var juego in Juegos)
            {
                var verja = ConstructorPuertas.Verja(juego);
                Assert.IsNotNull(verja, $"Falta la verja de {juego.Clave}.");
                Assert.AreEqual(juego.PixelesPorUnidadVerja, verja.pixelsPerUnit, 1e-3f);
                Assert.That(verja.bounds.size.y, Is.InRange(3.9f, 4.1f), "4 u de alto.");

                var estados = ConstructorPuertas.Checkpoint(juego);
                for (var i = 0; i < estados.Length; i++)
                {
                    Assert.IsNotNull(estados[i], $"Falta el checkpoint {i + 1} de {juego.Clave}.");
                    Assert.AreEqual(juego.PieCheckpoint[i].x, estados[i].rect.width, 1e-3f);
                    Assert.AreEqual(juego.PieCheckpoint[i].y, estados[i].pivot.x, 0.01f, $"{estados[i].name}: el pivote en el pie, no en el centro del lienzo.");
                    Assert.AreEqual(0f, estados[i].pivot.y, 0.01f);
                    Assert.That(estados[i].bounds.size.y, Is.InRange(3f, 3.6f), "Más alto que Kael, menos que la puerta.");
                }
            }
        }

        // --- Los niveles ---

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void LaSalida_EsLaPuertaDelMundo_CerradaYDentroDeLaArena(int id)
        {
            var nivel = Nivel(id);
            var juego = ConstructorPuertas.De(nivel);
            var escena = EditorSceneManager.OpenScene(nivel.escena.Ruta, OpenSceneMode.Additive);
            try
            {
                var salida = EscenasNivelTests.TodosEn<SalidaNivel>(escena).Single();
                Assert.IsNull(salida.transform.Find("Portal"), "Ya no es un rectángulo translúcido.");
                var puerta = salida.Puerta;
                Assert.IsNotNull(puerta, "La salida lleva su puerta dibujada.");
                var dibujos = ConstructorPuertas.Puerta(juego);
                Assert.AreSame(dibujos[0], puerta.DibujoCerrada);
                Assert.AreSame(dibujos[1], puerta.DibujoEntreabierta);
                Assert.AreEqual(2, puerta.DibujosAbierta);
                Assert.AreSame(dibujos[0], puerta.GetComponent<SpriteRenderer>().sprite, "En la escena, cerrada.");
                Assert.Less(puerta.GetComponent<SpriteRenderer>().sortingOrder, 5, "Por detrás de Kael.");

                var arena = EscenasNivelTests.TodosEn<ArenaJefe>(escena).Single();
                var limites = puerta.GetComponent<SpriteRenderer>().bounds;
                Assert.GreaterOrEqual(limites.min.x, arena.Limites.x, "La puerta cabe en la arena…");
                Assert.LessOrEqual(limites.max.x, arena.Limites.y, "…sin meterse en su pared.");
                var bajoElSuelo = (juego.LienzoPuerta.y - juego.SueloPuerta) / juego.PixelesPorUnidadPuerta;
                Assert.AreEqual(salida.transform.position.y, limites.min.y + bajoElSuelo, 0.05f, "De pie en el suelo (debajo, solo las cadenas que caen al abrirse).");

                var zona = salida.GetComponent<BoxCollider2D>();
                Assert.That(zona.bounds.center.x, Is.InRange(limites.min.x, limites.max.x), "Se sale cruzando la puerta.");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void LosCheckpoints_LlevanElDibujoDelMundo_EnLugarDeLaBandera(int id)
        {
            var nivel = Nivel(id);
            var juego = ConstructorPuertas.De(nivel);
            var escena = EditorSceneManager.OpenScene(nivel.escena.Ruta, OpenSceneMode.Additive);
            try
            {
                var checkpoints = EscenasNivelTests.TodosEn<Checkpoint>(escena);
                Assert.IsNotEmpty(checkpoints);
                var estados = ConstructorPuertas.Checkpoint(juego);
                foreach (var checkpoint in checkpoints)
                {
                    Assert.IsTrue(checkpoint.TieneDibujos, checkpoint.Id);
                    Assert.IsNull(checkpoint.transform.Find("Mastil"), $"{checkpoint.Id}: sin la bandera provisional.");
                    Assert.IsNull(checkpoint.transform.Find("Pano"), checkpoint.Id);
                    Assert.AreSame(estados[0], checkpoint.DibujoActual, $"{checkpoint.Id}: empieza apagado.");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void LaPuertaDeLaArena_EsLaVerjaDelMundo_YLlegaAlTecho(int id)
        {
            var nivel = Nivel(id);
            var juego = ConstructorPuertas.De(nivel);
            var escena = EditorSceneManager.OpenScene(nivel.escena.Ruta, OpenSceneMode.Additive);
            try
            {
                var arena = EscenasNivelTests.TodosEn<ArenaJefe>(escena).Single();
                var puerta = arena.transform.Find("Puerta");
                Assert.IsNotNull(puerta);
                Assert.IsFalse(puerta.gameObject.activeSelf, "Abierta hasta que Kael entra.");
                Assert.IsFalse(puerta.GetComponent<SpriteRenderer>().enabled, "Su rectángulo de color, apagado.");
                Assert.IsTrue(puerta.GetComponent<BoxCollider2D>() != null, "La colisión sigue siendo la del bloque.");

                // El bloque apagado no tiene caja de colisión calculada: su sitio sale de su transform.
                var centro = puerta.position.x;
                var suelo = puerta.position.y - puerta.lossyScale.y * 0.5f;
                var techo = puerta.position.y + puerta.lossyScale.y * 0.5f;
                var verja = puerta.Find("Verja");
                Assert.AreSame(ConstructorPuertas.Verja(juego), verja.GetComponent<SpriteRenderer>().sprite);
                Assert.AreEqual(centro, verja.position.x, 0.01f, "Centrada en la puerta.");
                Assert.AreEqual(suelo, verja.position.y, 0.01f, "Desde el suelo.");
                Assert.AreEqual(Vector3.one, verja.lossyScale, "Sin la escala del bloque: se ve a su tamaño.");

                // Hasta el techo: repetida, o con un muro de hielo encima.
                if (juego.VerjaSeRepite)
                {
                    var dibujo = verja.GetComponent<SpriteRenderer>();
                    Assert.AreEqual(SpriteDrawMode.Tiled, dibujo.drawMode);
                    Assert.AreEqual(techo - suelo, dibujo.size.y, 0.01f, "La verja se repite hasta el techo: no se ve un hueco por encima.");
                }
                else
                {
                    var muro = puerta.Find("MuroSobreLaVerja");
                    Assert.IsNotNull(muro, "Encima de la verja, un muro de hielo hasta el techo.");
                    Assert.AreEqual(techo, muro.position.y, 0.01f, "Cuelga del techo…");
                    Assert.AreEqual(techo - suelo - juego.AltoVerja, muro.GetComponent<SpriteRenderer>().size.y, 0.01f, "…y baja hasta la verja.");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        // --- Las cinemáticas de salida ---

        [Test]
        public void LasCinematicasDeSalida_LlevanSusDosPlanos_YLasFrasesDelCierre()
        {
            foreach (var juego in Juegos)
            {
                var cinematica = AssetDatabase.LoadAssetAtPath<CutsceneData>(juego.RutaCinematica);
                Assert.IsNotNull(cinematica, juego.RutaCinematica);
                Assert.AreEqual(MONTAJES_CINEMATICA.BEATS, cinematica.montaje);
                Assert.AreEqual(ConstructorPuertas.Rejilla, cinematica.rejilla, "En la rejilla de sus planos: 480 × 268.");
                Assert.IsTrue(cinematica.escalaEntera);
                Assert.IsTrue(cinematica.saltable, "Se puede saltar.");

                var planos = cinematica.diapositivas.Where(d => d.imagen != null).ToList();
                Assert.AreEqual(2, planos.Count);
                Assert.AreSame(ConstructorPuertas.Plano(juego, 0), planos[0].imagen, "Primero, el plano general.");
                Assert.AreSame(ConstructorPuertas.Plano(juego, 1), planos[1].imagen, "Después, Kael ante la puerta.");

                var frases = GuionMundos.De(juego.Emocion).CinematicaCierre;
                var dichas = planos.SelectMany(p => p.carteles).Select(c => c.texto).ToArray();
                CollectionAssert.AreEqual(frases.Take(3).ToArray(), dichas, "Las frases del cierre del guion, en orden.");
                foreach (var plano in planos)
                {
                    foreach (var cartel in plano.carteles)
                    {
                        Assert.LessOrEqual(cartel.Fin, plano.duracion + 1e-3f, $"«{cartel.texto}» acaba antes que su plano.");
                        Assert.GreaterOrEqual(cartel.duracion, cartel.texto.Length / 30f, $"«{cartel.texto}» se puede leer.");
                    }
                }

                var nivel = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos).niveles
                    .Single(n => n.emocion != null && n.emocion.id == juego.Emocion);
                Assert.AreSame(cinematica, nivel.cinematicaCierre, "Es la que se ve al cruzar la puerta del nivel.");
            }
        }
    }
}
