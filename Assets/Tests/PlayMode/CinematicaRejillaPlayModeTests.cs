using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fragmentum.Core;
using Fragmentum.Core.Juego;
using Fragmentum.Narrative;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Las cinemáticas en pixel art con los sistemas de verdad (fase B): el reproductor, la cámara
    /// de cinemáticas y la FSM global. Un montaje por beats corto, con las mismas piezas que la
    /// entrada del jefe: negro, un plano que sube desde negro y corta en seco, temblores de un
    /// píxel, un beat en negro, un plano encuadrado y un flash final de 2 fotogramas.
    /// </summary>
    public class CinematicaRejillaPlayModeTests : PruebaFlujoBase
    {
        private PantallaCinematica Reproductor => Interfaz.Cinematica;

        private CamaraCinematica Camara => Interfaz.CamaraCinematica;

        private static Sprite Plano(string nombre)
        {
            Sprite plano = null;
#if UNITY_EDITOR
            plano = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Cinematicas/Nivel1/{nombre}.png");
#endif
            Assert.IsNotNull(plano, $"Falta el plano {nombre}.");
            return plano;
        }

        // Negro 0,3 · plano A (sube 0,4, se mantiene 0,4, corte seco) · negro 0,2 · plano B encuadrado (sube 0,3, 0,3) + flash 2.
        private static CutsceneData Corta(string id)
        {
            var datos = ScriptableObject.CreateInstance<CutsceneData>();
            datos.id = id;
            datos.montaje = MONTAJES_CINEMATICA.BEATS;
            datos.escalaEntera = true;
            datos.bloqueoSalto = 0.2f;
            datos.avisoSaltar = string.Empty;
            datos.diapositivas = new List<Diapositiva>
            {
                new Diapositiva { duracion = 0.3f },
                new Diapositiva { imagen = Plano("CinematicaJefe1"), fundidoEntrada = 0.4f, duracion = 0.4f, temblores = { 0.5f } },
                new Diapositiva { duracion = 0.2f },
                new Diapositiva { imagen = Plano("CinematicaJefe2"), fundidoEntrada = 0.3f, duracion = 0.3f, desplazamiento = new Vector2Int(17, 19), flashFinal = 2 },
            };
            return datos;
        }

        private static IEnumerator EsperarHasta(System.Func<bool> condicion, float maximo, string descripcion)
        {
            var limite = Time.unscaledTime + maximo;
            while (!condicion())
            {
                if (Time.unscaledTime > limite)
                {
                    Assert.Fail($"Tiempo agotado ({maximo:0.0} s) esperando {descripcion}.");
                }

                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EnPixelArt_LaDibujaLaCamaraDeCinematicas_YLaInterfazNoTapaNada()
        {
            yield return EntrarEnNivel1();
            var datos = ScriptableObject.CreateInstance<CutsceneData>();
            datos.id = "Rejilla";
            datos.escalaEntera = true;
            datos.diapositivas.Add(new Diapositiva { imagen = Plano("CinematicaJefe1"), duracion = 2f });

            Assert.IsTrue(Juego.IniciarCinematica(datos));
            yield return null;
            yield return null;

            Assert.IsTrue(Reproductor.EnRejilla);
            Assert.IsTrue(Camara.Visible, "La cámara de cinemáticas se enciende.");
            Assert.AreEqual(datos.diapositivas[0].imagen, Camara.Plano.sprite);
            Assert.IsFalse(Reproductor.SobreElJuego, "Opaca: la cámara limpia a negro.");
            var k = Mathf.Max(1, Mathf.Min(Screen.width / 240, Screen.height / 135));
            Assert.AreEqual(k, Camara.Escala, $"La Pixel Perfect Camera amplía por el mayor entero que cabe en {Screen.width} × {Screen.height}.");
            Assert.AreEqual(k, Reproductor.EscalaEntera);

            Assert.IsTrue(Juego.TerminarCinematica());
            yield return null;
            Assert.IsFalse(Camara.Visible, "Al terminar se apaga.");
        }

        [UnityTest]
        public IEnumerator PorBeats_SubeDesdeNegro_TiemblaUnPixel_Encuadra_YAcabaConDosFotogramasDeFlash()
        {
            yield return EntrarEnNivel1();
            Assert.IsFalse(EventosJuego.EntradaBloqueada);
            Assert.IsTrue(Juego.IniciarCinematica(Corta("Beats")));
            Assert.IsTrue(EventosJuego.EntradaBloqueada, "Mientras dura, Kael no recibe input.");

            var alfasSubiendo = new List<float>();
            var temblores = new List<Vector2Int>();
            var encuadres = new HashSet<Vector3>();
            var fotogramasConFlash = 0;
            var vioNegroEntreLosPlanos = false;
            var limite = Time.unscaledTime + 10f;
            while (Juego.EstadoActual == ESTADOS_JUEGO.CUTSCENE)
            {
                var t = Reproductor.Tiempo;
                if (Camara.FlashVisible)
                {
                    fotogramasConFlash++;
                }

                if (t > 0.3f && t < 0.7f && Camara.Plano.enabled)
                {
                    alfasSubiendo.Add(Camara.Plano.color.a);
                }

                if (t > 1.15f && t < 1.3f && !Camara.Plano.enabled)
                {
                    vioNegroEntreLosPlanos = true;
                }

                if (Camara.Plano.enabled && Camara.Plano.sprite != null && Camara.Plano.sprite.name == "CinematicaJefe2")
                {
                    encuadres.Add(Camara.Plano.transform.localPosition);
                }

                temblores.Add(Camara.Temblor);
                Assert.IsTrue(EventosJuego.EntradaBloqueada);
                if (Time.unscaledTime > limite)
                {
                    Assert.Fail("La cinemática no termina.");
                }

                yield return null;
            }

            Assert.IsTrue(alfasSubiendo.Any(a => a > 0.05f && a < 0.95f), "El plano A sube desde negro: " + string.Join(", ", alfasSubiendo));
            Assert.IsTrue(vioNegroEntreLosPlanos, "Corte seco a negro entre los dos planos.");
            CollectionAssert.AreEquivalent(new[] { new Vector3(17f, 19f, 0f) }, encuadres, "El plano B, 17 px a la derecha y 19 arriba.");
            Assert.IsTrue(temblores.Any(v => v != Vector2Int.zero), "Tiembla.");
            Assert.IsTrue(temblores.All(v => Mathf.Abs(v.x) + Mathf.Abs(v.y) <= 1), "Un píxel entero, ni más ni menos: " + string.Join(", ", temblores.Distinct()));
            Assert.AreEqual(2, fotogramasConFlash, "El flash son 2 fotogramas contados, no 0,033 s.");

            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);
            Assert.IsFalse(Camara.Visible);
            Assert.IsFalse(EventosJuego.EntradaBloqueada, "Al acabar, Kael vuelve a recibir input.");
        }

        [UnityTest]
        public IEnumerator AlSaltarla_VaDirectoAlFlashFinal()
        {
            yield return EntrarEnNivel1();
            Assert.IsTrue(Juego.IniciarCinematica(Corta("Saltada")));
            yield return EsperarHasta(() => Reproductor.Tiempo >= 0.5f, 3f, "pasar el bloqueo");

            Assert.IsTrue(Reproductor.PedirSalto());
            var fotogramasConFlash = 0;
            var antesDelFinal = Reproductor.Tiempo;
            while (Juego.EstadoActual == ESTADOS_JUEGO.CUTSCENE)
            {
                if (Camara.FlashVisible)
                {
                    fotogramasConFlash++;
                    Assert.IsTrue(Reproductor.EnFlashFinal, "El flash es el del final, no el de su hora.");
                }

                yield return null;
            }

            Assert.Less(antesDelFinal, 1.9f - 0.5f, "Se saltó con la cinemática a medias.");
            Assert.AreEqual(2, fotogramasConFlash, "Saltar lleva al flash final, entero.");
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);
        }

        [UnityTest]
        public IEnumerator UnCueSinClip_AvisaConSuNombre_YLaCinematicaSigue()
        {
            yield return EntrarEnNivel1();
            var datos = Corta("SinAudio");
            datos.diapositivas[0].cues.Add(new CueAudio { nombre = "drone_sala", bucle = true });
            datos.diapositivas[1].cues.Add(new CueAudio { nombre = "golpe_escala", instante = 0.3f });

            LogAssert.Expect(LogType.Warning, new Regex("'SinAudio': falta el audio «drone_sala»"));
            LogAssert.Expect(LogType.Warning, new Regex("'SinAudio': falta el audio «golpe_escala» \\(diapositiva 2, a los 0.6 s\\)"));
            Assert.IsTrue(Juego.IniciarCinematica(datos));
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);
        }

        [UnityTest]
        public IEnumerator ElTexto_SeEscribeEnLaRejilla_DondeDiceElCartel()
        {
            yield return EntrarEnNivel1();
            var datos = Corta("ConTexto");
            datos.diapositivas[1].carteles.Add(new Cartel
            {
                texto = "No recuerdo esta sala.\nEl calor, sí.",
                inicio = 0.1f,
                duracion = 0.6f,
                enPunto = true,
                punto = new Vector2Int(24, 110),
                alineacion = TextAnchor.UpperLeft,
            });

            LogAssert.Expect(LogType.Warning, new Regex("'ConTexto' no tiene fuente bitmap"));
            Assert.IsTrue(Juego.IniciarCinematica(datos));
            yield return EsperarHasta(() => Reproductor.TextoActual.Length > 0, 2f, "el texto de Kael");

            Assert.AreEqual("No recuerdo esta sala.\nEl calor, sí.", Reproductor.TextoActual);
            var texto = Camara.Textos.First(t => t.Visible);
            Assert.AreEqual(new Vector2Int(24, 110), texto.Bloque.position, "Anclado en su píxel del plano.");
            Assert.IsTrue(texto.Vertices.All(v => Mathf.Approximately(v.x, Mathf.Round(v.x))), "En columnas enteras.");

            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 5f);
        }
    }
}
