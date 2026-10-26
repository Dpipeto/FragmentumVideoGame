using System.Collections.Generic;
using System.Linq;
using Fragmentum.Narrative;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El montaje por beats (fase B), con los tiempos del guion de la entrada del jefe del Nivel 1
    /// (negro 0,5 · plano A 1,0 + 4,9 · negro 0,5 · plano B 1,1 + 1,6, flash de 2 fotogramas), y el
    /// texto en la rejilla de la cámara de cinemáticas. Lógica pura: no espera a nada.
    /// </summary>
    public class CinematicaBeatsTests
    {
        private CutsceneData _datos;

        [SetUp]
        public void Crear()
        {
            _datos = ScriptableObject.CreateInstance<CutsceneData>();
            _datos.id = "PruebaBeats";
            _datos.montaje = MONTAJES_CINEMATICA.BEATS;
            _datos.escalaEntera = true;
            _datos.diapositivas = new List<Diapositiva>
            {
                new Diapositiva { duracion = 0.5f, cues = { new CueAudio { nombre = "drone_sala", bucle = true, hasta = 9.6f } } },
                new Diapositiva
                {
                    imagen = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f),
                    fundidoEntrada = 1f, duracion = 4.9f, fundidoSalida = 0f, brasas = true,
                    temblores = { 1.9f, 3.1f },
                    carteles = { new Cartel { texto = "No recuerdo esta sala.\nEl calor, sí.", inicio = 3.5f, duracion = 2.4f, enPunto = true, punto = new Vector2Int(24, 110), alineacion = TextAnchor.UpperLeft } },
                    cues =
                    {
                        new CueAudio { nombre = "amb_forja", bucle = true, hasta = 5.9f },
                        new CueAudio { nombre = "golpe_escala", instante = 1.1f },
                    },
                },
                new Diapositiva { duracion = 0.5f },
                new Diapositiva
                {
                    imagen = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f),
                    fundidoEntrada = 1.1f, duracion = 1.6f, desplazamiento = new Vector2Int(17, 19), flashFinal = 2,
                    carteles = { new Cartel { texto = "CENTINELA ARDIENTE", inicio = 1.1f, duracion = 1.6f, titulo = true } },
                },
            };
        }

        [TearDown]
        public void Borrar() => Object.DestroyImmediate(_datos);

        [Test]
        public void PorBeats_DuraLaSubidaLoQueSeMantieneYLaBajada_DeCadaBeat()
        {
            Assert.AreEqual(9.6f, _datos.DuracionTotal, 0.001f, "0,5 + (1,0 + 4,9) + 0,5 + (1,1 + 1,6).");
            var linea = new LineaTiempoCinematica(_datos);
            Assert.AreEqual(9.6f, linea.Duracion, 0.001f, "El flash son fotogramas: no cuenta en la duración.");
            Assert.AreEqual(0.5f, linea.InicioDe(1), 0.001f);
            Assert.AreEqual(6.4f, linea.InicioDe(2), 0.001f, "El corte seco a negro, a los 6,4 s.");
            Assert.AreEqual(6.9f, linea.InicioDe(3), 0.001f);
            Assert.AreEqual(0, linea.IndiceEn(0.4f));
            Assert.AreEqual(1, linea.IndiceEn(6.39f));
            Assert.AreEqual(2, linea.IndiceEn(6.41f));
            Assert.AreEqual(3, linea.IndiceEn(9.5f));
        }

        [Test]
        public void UnBeat_SubeDesdeNegro_SeMantiene_YConFundidoCeroCortaEnSeco()
        {
            var linea = new LineaTiempoCinematica(_datos);
            float Alfa(float t) => linea.ImagenesEn(t).abajo.Alfa;

            Assert.AreEqual(0.5f, Alfa(1.0f), 0.01f, "A mitad de la subida de 1 s.");
            Assert.AreEqual(1f, Alfa(1.6f), 0.001f);
            Assert.AreEqual(1f, Alfa(6.39f), 0.001f, "Fundido de salida 0: se ve entero hasta el corte.");
            Assert.AreEqual(2, linea.ImagenesEn(6.41f).abajo.Indice, "Después del corte, el beat en negro.");
            Assert.IsNull(_datos.diapositivas[2].imagen);
            Assert.AreEqual(0.5f, Alfa(6.9f + 0.55f), 0.01f, "El ojo sube desde negro en 1,1 s.");
            Assert.IsFalse(linea.ImagenesEn(9.6f).abajo.Visible, "Terminada.");
        }

        [Test]
        public void UnBeatSinImagen_EsNegroAPropósito_YNoDiceQueFalteNada()
        {
            Assert.IsFalse(_datos.FaltaArte);
            Assert.AreEqual(string.Empty, _datos.TextoDe(0));
            Assert.IsEmpty(_datos.CartelesDe(2));

            // Con fundidos cruzados, la misma diapositiva vacía sí avisa (la Fase 8 lo pide).
            _datos.montaje = MONTAJES_CINEMATICA.FUNDIDOS_CRUZADOS;
            Assert.IsTrue(_datos.FaltaArte);
            StringAssert.Contains("PENDIENTE", _datos.TextoDe(0));
        }

        [Test]
        public void LosTextos_LosTemblores_YLosCues_CuentanDesdeQueEmpiezaElBeat()
        {
            var linea = new LineaTiempoCinematica(_datos);

            Assert.IsEmpty(linea.TextosEn(3.9f), "El texto de Kael entra a los 4,0 s (3,5 desde el inicio del beat).");
            Assert.AreEqual("No recuerdo esta sala.\nEl calor, sí.", linea.TextosEn(4.5f).Single().Cartel.texto);
            Assert.AreEqual(1, linea.TextosEn(8.05f).Count, "El rótulo, a los 8,0 s.");

            Assert.AreEqual(1, linea.TembloresEntre(2.3f, 2.5f), "Temblor a los 2,4 s.");
            Assert.AreEqual(1, linea.TembloresEntre(3.5f, 3.7f), "Y a los 3,6 s.");
            Assert.AreEqual(2, linea.TembloresEntre(-1f, 9.6f));

            var cues = linea.CuesEntre(-1f, 9.6f).ToDictionary(c => c.Cue.nombre);
            Assert.AreEqual(0f, cues["drone_sala"].Inicio, 0.001f);
            Assert.AreEqual(9.6f, cues["drone_sala"].Fin, 0.001f, "El zumbido, hasta el final.");
            Assert.AreEqual(0.5f, cues["amb_forja"].Inicio, 0.001f);
            Assert.AreEqual(6.4f, cues["amb_forja"].Fin, 0.001f, "El ambiente se corta en seco con la imagen.");
            Assert.AreEqual(1.6f, cues["golpe_escala"].Inicio, 0.001f);
            Assert.IsTrue(float.IsPositiveInfinity(cues["golpe_escala"].Fin), "Un golpe suelto suena entero.");
            Assert.IsEmpty(linea.CuesEntre(1.6f, 3f), "Cada cue empieza una sola vez.");
        }

        [Test]
        public void ElFlash_SonFotogramasAlTerminarElUltimoBeat()
        {
            var linea = new LineaTiempoCinematica(_datos);
            Assert.AreEqual(0, linea.FlashEntre(-1f, 9.5f));
            Assert.AreEqual(2, linea.FlashEntre(9.5f, 9.7f), "2 fotogramas al acabar el ojo.");
            Assert.AreEqual(2, linea.FlashFinal, "Al saltarla, se va directo a él.");
        }

        // --- Texto en la rejilla ---

        [Test]
        public void ElTextoEnRejilla_CaeEnPixelesEnterosDelPlano()
        {
            var objeto = new GameObject("Texto", typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                var texto = objeto.AddComponent<TextoEnRejilla>();
                texto.Fijar("No recuerdo esta sala.\nEl calor, sí.", FabricaUI.Fuente, 8, 0, new Vector2Int(24, 110), TextAnchor.UpperLeft, Color.white);

                Assert.IsNotEmpty(texto.Vertices);
                foreach (var v in texto.Vertices)
                {
                    // x entera; y en medios, como los bordes de los píxeles del plano (mide 135 de alto).
                    Assert.AreEqual(Mathf.Round(v.x), v.x, 1e-4f, $"x = {v.x}");
                    Assert.AreEqual(Mathf.Round(v.y + 0.5f), v.y + 0.5f, 1e-4f, $"y = {v.y}");
                }

                Assert.AreEqual(new Vector2Int(24, 110), texto.Bloque.position, "Arriba a la izquierda, en su píxel.");
                Assert.Greater(texto.Bloque.width, 0);

                // Centrado bajo un punto: el bloque se reparte a los dos lados.
                texto.Fijar("CENTINELA ARDIENTE", FabricaUI.Fuente, 8, 0, new Vector2Int(120, 100), TextAnchor.UpperCenter, Color.white);
                Assert.AreEqual(120 - texto.Bloque.width / 2, texto.Bloque.x);
                Assert.AreEqual(100, texto.Bloque.y);
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void EnElMundo_LosBordesDelPlano_SonLosDeLaRejilla()
        {
            Assert.AreEqual(new Vector3(-120f, 67.5f, 0f), TextoEnRejilla.EnElMundo(0, 0), "Arriba a la izquierda del plano.");
            Assert.AreEqual(new Vector3(120f, -67.5f, 0f), TextoEnRejilla.EnElMundo(240, 135), "Abajo a la derecha.");
        }
    }
}
