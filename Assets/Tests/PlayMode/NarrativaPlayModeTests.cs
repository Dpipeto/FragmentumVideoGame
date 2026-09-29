using System.Collections;
using System.Collections.Generic;
using System.IO;
using Fragmentum.Core;
using Fragmentum.Core.Juego;
using Fragmentum.Core.Save;
using Fragmentum.Levels;
using Fragmentum.Narrative;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Criterios de aceptación de la Fase 8 (tarea G): la cinemática inicial funciona sin
    /// imágenes y se ve una sola vez; ningún asset ausente bloquea el juego; los diálogos
    /// internos aparecen al empezar y antes del jefe; el registro emocional se guarda en JSON y
    /// CSV y se puede omitir; y «yo reconstruido» muestra los fragmentos integrados.
    /// </summary>
    public class NarrativaPlayModeTests : PruebaFlujoBase
    {
        // Estas pruebas parten de una partida nueva: la cinemática inicial aún no se ha visto.
        protected override bool CinematicaInicialVista => false;

        // --- Cinemáticas ---

        [UnityTest]
        public IEnumerator LaCinematicaInicial_SeVeAntesDelNivel1_SinImagenes()
        {
            Assert.IsTrue(Juego.JugarNivel(Nivel1));
            yield return EsperarEstado(ESTADOS_JUEGO.CUTSCENE, 5f);

            var cinematica = Interfaz.Cinematica;
            Assert.IsTrue(cinematica.Visible);
            Assert.AreEqual("Inicial", cinematica.Actual.Id);
            StringAssert.Contains("CINEMÁTICA INICIAL", cinematica.TextoActual, "Sin imágenes se ve el texto: el sistema funciona igual.");
            Assert.AreEqual(0f, Time.timeScale, "La cinemática detiene el juego.");

            cinematica.Siguiente();
            Assert.AreEqual(1, cinematica.Indice, "Un clic o Enter pasa a la diapositiva siguiente.");

            Interfaz.AlternarPausa(); // Esc: saltar
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);
            Assert.IsFalse(cinematica.Visible);
            CollectionAssert.Contains(Gestor.Datos.cinematicasVistas, "Inicial");
            CollectionAssert.Contains(new SaveSystem(CarpetaGuardado).Cargar().cinematicasVistas, "Inicial", "Queda guardado que ya se vio.");
        }

        [UnityTest]
        public IEnumerator LaCinematicaInicial_NoSeRepite()
        {
            Gestor.Datos.cinematicasVistas.Add("Inicial");
            var estados = new List<ESTADOS_JUEGO>();
            Juego.OnStateChanged += (_, nuevo) => estados.Add(nuevo);

            yield return EntrarEnNivel1();

            CollectionAssert.DoesNotContain(estados, ESTADOS_JUEGO.CUTSCENE);
        }

        [UnityTest]
        public IEnumerator UnaCinematica_AvanzaSola_YVuelveAlJuego()
        {
            Gestor.Datos.cinematicasVistas.Add("Inicial");
            yield return EntrarEnNivel1();

            var memoria = ScriptableObject.CreateInstance<CutsceneData>();
            memoria.id = "MemoriaDePrueba";
            memoria.diapositivas.Add(new Diapositiva { texto = "uno", duracion = 0.6f });
            memoria.diapositivas.Add(new Diapositiva { texto = "dos", duracion = 0.6f });

            Assert.IsTrue(Juego.IniciarCinematica(memoria));
            Assert.AreEqual(ESTADOS_JUEGO.CUTSCENE, Juego.EstadoActual);
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 4f);
            Object.Destroy(memoria);
        }

        [UnityTest]
        public IEnumerator SinReproductor_LaCinematicaNoBloqueaElJuego()
        {
            Object.DestroyImmediate(Interfaz);

            Assert.IsTrue(Juego.JugarNivel(Nivel1));
            yield return EsperarEstado(ESTADOS_JUEGO.CUTSCENE, 5f);
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 4f);

            CollectionAssert.Contains(Gestor.Datos.cinematicasVistas, "Inicial");
        }

        // --- Registro emocional ---

        [UnityTest]
        public IEnumerator AlSuperarElNivel_CierreYRegistroEmocional()
        {
            Gestor.Datos.cinematicasVistas.Add("Inicial");
            yield return EntrarEnNivel1();

            Assert.IsTrue(Juego.CompletarNivel());
            Assert.AreEqual(ESTADOS_JUEGO.CUTSCENE, Juego.EstadoActual, "Primero el cierre del mundo.");
            Assert.AreEqual("Cierre_Ira", Juego.CinematicaActual.Id);
            Interfaz.AlternarPausa(); // saltar
            Assert.AreEqual(ESTADOS_JUEGO.LEVEL_COMPLETE, Juego.EstadoActual);

            var fin = Interfaz.PantallaFinNivel;
            Assert.IsTrue(fin.MostrandoRegistro, "El fin de nivel empieza por el registro emocional.");

            fin.ElegirValor(4);
            fin.GuardarRegistro();

            Assert.IsFalse(fin.MostrandoRegistro, "Después, la recompensa.");
            var registro = new RegistroEmocional(CarpetaGuardado);
            Assert.AreEqual(1, registro.Entradas.Count);
            Assert.AreEqual(4, registro.Entradas[0].valor);
            Assert.AreEqual("Ira", registro.Entradas[0].emocion);
            Assert.AreEqual(1, registro.Entradas[0].nivel);
            StringAssert.Contains(";Ira;4;", File.ReadAllText(registro.RutaCsv), "Se exporta también a CSV.");
            StringAssert.Contains("Cierre", fin.LineaCierre, "La línea de cierre de Kael acompaña al fragmento.");
        }

        [UnityTest]
        public IEnumerator ElRegistroEmocional_SePuedeOmitir()
        {
            Gestor.Datos.cinematicasVistas.Add("Inicial");
            yield return EntrarEnNivel1();
            Juego.CompletarNivel();
            yield return SaltarCinematicaSiHay();

            var fin = Interfaz.PantallaFinNivel;
            fin.OmitirRegistro();

            Assert.IsFalse(fin.MostrandoRegistro);
            Assert.IsTrue(fin.BotonContinuar.gameObject.activeInHierarchy);
            Assert.AreEqual(0, new RegistroEmocional(CarpetaGuardado).Entradas.Count, "Omitir no guarda nada.");
        }

        // --- Diálogos internos ---

        [UnityTest]
        public IEnumerator LasLineasDeApertura_AparecenAlEmpezarAJugar()
        {
            Assert.IsTrue(Juego.JugarNivel(Nivel1));
            yield return EsperarEstado(ESTADOS_JUEGO.CUTSCENE, 5f);
            yield return EsperarReal(0.3f);
            Assert.AreEqual(string.Empty, Interfaz.Hud.LineaActual, "Durante la cinemática, los diálogos esperan.");

            Interfaz.AlternarPausa(); // saltar la cinemática
            yield return EsperarHasta(() => Interfaz.Hud.LineaActual.Length > 0, 2f, "la línea de apertura");

            StringAssert.Contains("Apertura", Interfaz.Hud.LineaActual);
            StringAssert.Contains("Ira", Interfaz.Hud.LineaActual, "La línea nombra la emoción del mundo.");
            Assert.AreEqual("KAEL", Interfaz.Hud.HablanteActual);
        }

        [UnityTest]
        public IEnumerator AntesDelJefe_HablaElArquitecto()
        {
            Gestor.Datos.cinematicasVistas.Add("Inicial");
            yield return EntrarEnNivel1();
            DialogueManager.Instancia.Limpiar();
            var arena = Object.FindAnyObjectByType<ArenaJefe>();

            yield return LlevarJugadorA(new Vector2(arena.transform.position.x, 0.05f));
            yield return EsperarHasta(() => Interfaz.Hud.LineaActual.Length > 0, 2f, "la línea previa al jefe");

            Assert.AreEqual("CENTINELA ARDIENTE", Interfaz.Hud.HablanteActual);
        }

        // --- Yo reconstruido ---

        [UnityTest]
        public IEnumerator YoReconstruido_MuestraLosFragmentosIntegrados()
        {
            Gestor.Datos.fragmentos.Add("IRA");
            Gestor.Datos.fragmentos.Add("TRISTEZA");

            UIManager.Instancia.MostrarYoReconstruido();
            yield return null;

            var pantalla = Interfaz.YoReconstruido;
            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual(2, pantalla.Integrados);
            Assert.AreEqual(5, pantalla.Total, "Cinco fragmentos: uno por mundo hasta el Jardín.");

            Interfaz.AlternarPausa(); // Esc cierra
            Assert.IsFalse(pantalla.Visible);
        }

        // Espera con tiempo sin escala una condición (la HUD vive fuera del tiempo del juego).
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
    }
}
