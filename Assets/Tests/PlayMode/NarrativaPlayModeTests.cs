using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public IEnumerator LaCinematicaInicial_SeVeAntesDelNivel1_YSoloSeSaltaPasados2Segundos()
        {
            Assert.IsTrue(Juego.JugarNivel(Nivel1));
            yield return EsperarEstado(ESTADOS_JUEGO.CUTSCENE, 5f);

            var cinematica = Interfaz.Cinematica;
            Assert.IsTrue(cinematica.Visible);
            Assert.AreEqual("Inicial", cinematica.Actual.Id);
            Assert.AreEqual(12, cinematica.Linea.Diapositivas);
            Assert.AreEqual(0f, Time.timeScale, "La cinemática detiene el juego.");

            // Los primeros 2 s, ninguna tecla la salta: ni Esc ni cualquier otra.
            Assert.IsFalse(cinematica.PedirSalto(), "Cualquier tecla, antes de 2 s: no salta.");
            Interfaz.AlternarPausa();
            Assert.AreEqual(ESTADOS_JUEGO.CUTSCENE, Juego.EstadoActual, "Esc, antes de 2 s: no salta.");
            Assert.AreEqual(0f, cinematica.AlfaAviso, "El aviso aún no se ve.");

            yield return EsperarHasta(() => cinematica.Tiempo >= 2.6f, 5f, "pasar el bloqueo de 2 s");
            Assert.Greater(cinematica.AlfaAviso, 0f, "A los 2 s aparece el aviso para saltar.");

            Assert.IsTrue(cinematica.PedirSalto(), "Pasados 2 s, cualquier tecla la salta.");
            Assert.AreEqual(ESTADOS_JUEGO.CUTSCENE, Juego.EstadoActual, "Se aplica en el frame siguiente: la tecla no llega al juego.");
            yield return null;
            yield return null;

            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);
            Assert.IsFalse(cinematica.Visible);
            CollectionAssert.Contains(Gestor.Datos.cinematicasVistas, "Inicial");
            CollectionAssert.Contains(new SaveSystem(CarpetaGuardado).Cargar().cinematicasVistas, "Inicial", "Queda guardado que ya se vio.");
        }

        [UnityTest]
        public IEnumerator UnTexto_EntraConFundido_YLaCinematicaTerminaSola()
        {
            Gestor.Datos.cinematicasVistas.Add("Inicial");
            yield return EntrarEnNivel1();

            // Una cinemática corta con las mismas reglas, para no esperar los 80 s de la inicial.
            var corta = ScriptableObject.CreateInstance<CutsceneData>();
            corta.id = "Corta";
            corta.diapositivas.Add(new Diapositiva { texto = "Hubo una guerra.", duracion = 3f });

            Assert.IsTrue(Juego.IniciarCinematica(corta));
            var cinematica = Interfaz.Cinematica;
            Assert.AreEqual(string.Empty, cinematica.TextoActual, "Al empezar, el texto todavía no ha entrado.");

            yield return EsperarHasta(() => cinematica.Tiempo >= 1.5f, 3f, "el centro de la diapositiva");
            Assert.AreEqual("Hubo una guerra.", cinematica.TextoActual);

            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 4f);
            Assert.IsFalse(cinematica.Visible);
            Object.Destroy(corta);
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
        public IEnumerator ConEscalaEntera_ElPlanoMideUnMultiploExactoDeSuTamano_EnPixelesEnteros()
        {
            Gestor.Datos.cinematicasVistas.Add("Inicial");
            yield return EntrarEnNivel1();

            Sprite plano = null;
#if UNITY_EDITOR
            plano = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cinematicas/Nivel1/CinematicaJefe1.png");
#endif
            Assert.IsNotNull(plano, "Falta el plano CinematicaJefe1.");
            Assert.AreEqual(new Vector2(240f, 135f), plano.rect.size);

            var pixelArt = ScriptableObject.CreateInstance<CutsceneData>();
            pixelArt.id = "PixelArt";
            pixelArt.escalaEntera = true;
            pixelArt.diapositivas.Add(new Diapositiva { imagen = plano, duracion = 2f });
            Assert.IsTrue(Juego.IniciarCinematica(pixelArt));
            yield return null;
            yield return null;

            var cinematica = Interfaz.Cinematica;
            var pantalla = new Vector2(Screen.width, Screen.height);
            var k = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(pantalla.x / 240f, pantalla.y / 135f)));
            Assert.AreEqual(k, cinematica.EscalaEntera, $"El mayor entero que cabe en {pantalla.x} × {pantalla.y}.");

            var rect = cinematica.RectImagenEnPantalla;
            Assert.AreEqual(k * 240f, rect.width, 0.5f, "Ancho: k veces 240 píxeles de pantalla.");
            Assert.AreEqual(k * 135f, rect.height, 0.5f, "Alto: k veces 135.");
            Assert.AreEqual(Mathf.Round(rect.xMin), rect.xMin, 0.05f, "El borde cae en un píxel entero: no se interpola.");
            Assert.AreEqual(Mathf.Round(rect.yMin), rect.yMin, 0.05f);
            Assert.AreEqual(pantalla.x / 2f, rect.center.x, 0.51f, "Centrada.");

            Assert.IsTrue(Juego.TerminarCinematica());
            yield return null;

            // Sin escala entera, la imagen vuelve a llenar la pantalla con su proporción.
            var libre = ScriptableObject.CreateInstance<CutsceneData>();
            libre.id = "Libre";
            libre.diapositivas.Add(new Diapositiva { imagen = plano, duracion = 2f });
            Assert.IsTrue(Juego.IniciarCinematica(libre));
            yield return null;
            Assert.AreEqual(0, cinematica.EscalaEntera);
            Assert.AreEqual(Mathf.Min(pantalla.x, pantalla.y * 16f / 9f), cinematica.RectImagenEnPantalla.width, 1f, "Escala libre: llena el ancho o el alto.");
            Assert.IsTrue(Juego.TerminarCinematica());

            Object.Destroy(pixelArt);
            Object.Destroy(libre);
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
            yield return EsperarReal(2.1f); // pasado el bloqueo inicial
            Interfaz.AlternarPausa(); // Esc: saltar
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
            StringAssert.Contains(Nivel1.dialogos.Del(MOMENTOS_DIALOGO.CIERRE)[0].texto, fin.LineaCierre, "La línea de cierre de Kael acompaña al fragmento.");
            StringAssert.Contains("Ira", fin.LineaCierre, "Y nombra la emoción del mundo.");
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
            yield return EsperarReal(2.1f); // pasado el bloqueo inicial
            Assert.AreEqual(string.Empty, Interfaz.Hud.LineaActual, "Durante la cinemática, los diálogos esperan.");

            Interfaz.AlternarPausa(); // Esc: saltar la cinemática
            yield return EsperarHasta(() => Interfaz.Hud.LineaActual.Length > 0, 2f, "la línea de apertura");

            StringAssert.Contains("Este calor no es mío", Interfaz.Hud.LineaActual, "La apertura del guion.");
            StringAssert.Contains("Ira", Interfaz.Hud.LineaActual, "La línea nombra la emoción del mundo.");
            Assert.AreEqual("KAEL", Interfaz.Hud.HablanteActual);
        }

        [UnityTest]
        public IEnumerator AntesDelJefe_LaPresentacionLlevaElTextoDelGuion()
        {
            Gestor.Datos.cinematicasVistas.Add("Inicial");
            yield return EntrarEnNivel1();
            DialogueManager.Instancia.Limpiar();
            var arena = Object.FindAnyObjectByType<ArenaJefe>();

            yield return LlevarJugadorA(EnElSuelo(arena.transform.position));
            Assert.AreEqual(ESTADOS_JUEGO.CUTSCENE, Juego.EstadoActual, "Al entrar en la arena, la presentación del jefe.");

            // El guion: Kael habla a los 4 s; el nombre del jefe es el rótulo del final (lo comprueba CinematicaJefe1Tests).
            yield return EsperarHasta(() => Interfaz.Cinematica.TextoActual.Contains("No recuerdo esta sala."), 6f, "el texto de Kael");
            StringAssert.Contains("El calor, sí.", Interfaz.Cinematica.TextoActual, "Las dos líneas juntas.");

            Assert.IsTrue(Juego.TerminarCinematica());
            yield return null;
            yield return null;
            Assert.IsEmpty(Interfaz.Hud.LineaActual, "No se repite en el HUD: ya se dijo en la cinemática.");
        }

        [UnityTest]
        public IEnumerator SinPresentacion_LaLineaPreviaAlJefeVaEnElHud()
        {
            Gestor.Datos.cinematicasVistas.Add("Inicial");
            yield return EntrarEnNivel1();
            DialogueManager.Instancia.Limpiar();
            var arena = Object.FindAnyObjectByType<ArenaJefe>();

            // Lo que pasa en un nivel cuyo LevelData no tiene cinemática de jefe.
            arena.IniciarCombate(Jugador);
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
