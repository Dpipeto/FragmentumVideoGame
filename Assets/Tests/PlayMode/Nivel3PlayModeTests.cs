using System.Collections;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Core.Juego;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using Fragmentum.Levels.Escenario;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// El Nivel 3 montado, jugándose: correr hace ruido y caminar no, la trinchera guarda la
    /// partida, las raíces del callejón no agarran y el puesto del vigía se sostiene (fase A); y la
    /// oscuridad cambia por tramos con Kael llevando su luz, y la primera antorcha, con su aviso, se
    /// enciende y enseña el primer cepo (fase B); esa antorcha despierta al primer Espectro, que
    /// espera en el borde de su luz (fase C); y el Espectro del Pavor espera visible en su arena, se
    /// desvanece al empezar el combate y, al vencerlo, la oscuridad retrocede (fase D); y se presenta
    /// y cae con sus cinemáticas, y la puerta del árbol se abre al recoger su fragmento (fase F). En
    /// la ciénaga, la maraña sujeta a Kael y solo el Dash Acuático la cruza. El Nivel 1 sigue con luz.
    /// Las medidas las
    /// comprueban <c>DisenoNivel3Tests</c> y <c>EscenaNivel3Tests</c>; aquí, que funcionan en el
    /// juego.
    /// </summary>
    public class Nivel3PlayModeTests : PruebaFlujoBase
    {
        private LevelData Nivel3 => BaseDeDatos.niveles[2];

        private IEnumerator EntrarEnNivel3()
        {
            Gestor.Datos.nivelMaximoAlcanzado = Mathf.Max(Gestor.Datos.nivelMaximoAlcanzado, 3);
            Assert.IsTrue(Juego.JugarNivel(Nivel3), "El Nivel 3 tiene escena y está desbloqueado.");
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 8f);
            Assert.AreEqual(Nivel3.escena.Nombre, SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Jugador, "El LevelManager del Nivel 3 hace aparecer al jugador.");
        }

        private static void ApagarEnemigos()
        {
            foreach (var enemigo in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
            {
                enemigo.gameObject.SetActive(false);
            }
        }

        private static void ApagarCepos()
        {
            foreach (var cepo in Object.FindObjectsByType<Cepo>(FindObjectsInactive.Exclude))
            {
                cepo.gameObject.SetActive(false);
            }
        }

        [UnityTest]
        public IEnumerator AlEmpezar_KaelEstaEnLaLinde_YCorrerHaceRuido_PeroCaminarNo()
        {
            yield return EntrarEnNivel3();
            ApagarEnemigos();
            // Corriendo llega al primer cepo (x 11): aquí solo se mide el ruido.
            ApagarCepos();
            Assert.AreEqual(2f, Jugador.transform.position.x, 0.1f, "Kael aparece en el inicio.");

            var modo = Jugador.GetComponent<PlayerModoMovimiento>();
            Assert.IsNotNull(modo);
            Assert.IsTrue(modo.EmitirRuido, "En el mundo del Miedo, correr se oye.");

            var ruidos = 0;
            modo.OnRuido += (_, _) => ruidos++;
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);

            // Caminando con Alt (el 35 % de la velocidad), en silencio.
            entrada.EjeHorizontal = 0.35f;
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(MODOS_MOVIMIENTO.STEALTH, modo.ModoActual, "Caminando, el modo es el sigiloso.");
            Assert.AreEqual(0, ruidos, "Caminando no se hace ruido.");

            // Corriendo, ruido cada poco.
            entrada.EjeHorizontal = 1f;
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(MODOS_MOVIMIENTO.RUN, modo.ModoActual);
            Assert.Greater(ruidos, 0, "Corriendo, sí.");
            entrada.EjeHorizontal = 0f;
        }

        [UnityTest]
        public IEnumerator LaTrinchera_GuardaLaPartida()
        {
            yield return EntrarEnNivel3();
            ApagarEnemigos();
            var checkpoint = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Exclude).Single(c => c.Id == "L3_CP1");
            Assert.IsFalse(checkpoint.Activo);

            yield return LlevarJugadorA(EnElSuelo(checkpoint.transform.position));
            Assert.IsTrue(checkpoint.Activo, "Al tocarlo, la trinchera es el punto de reaparición.");
            Assert.IsTrue(checkpoint.EcoDeMemoria, "Y suena el eco de memoria.");
        }

        [UnityTest]
        public IEnumerator LasRaicesDelCallejon_NoAgarran_YElPuestoDelVigiaSeSostiene()
        {
            yield return EntrarEnNivel3();
            ApagarEnemigos();

            // Pegado a las raíces desde el fondo de la hondonada: Kael choca con ellas, pero no se agarra.
            var raices = GameObject.Find("RaicesDelCallejon").GetComponent<BoxCollider2D>().bounds;
            yield return LlevarJugadorA(new Vector2(raices.min.x - 0.45f, -4f + 0.05f));
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);
            entrada.EjeHorizontal = 1f;
            for (var i = 0; i < 10; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.IsTrue(Jugador.Sensores.EnSuelo, "De pie en el fondo de la hondonada.");
            Assert.IsFalse(Jugador.Sensores.ParedDerecha, "Las raíces no cuentan como pared para agarrarse.");
            Assert.Less(Jugador.transform.position.x, raices.min.x, "Pero no se atraviesan.");
            entrada.EjeHorizontal = 0f;

            // De pie en el puesto del vigía, a 8,5 u sobre la hondonada.
            var puesto = GameObject.Find("PuestoDelVigia").GetComponent<BoxCollider2D>().bounds;
            yield return LlevarJugadorA(new Vector2(puesto.center.x, puesto.max.y + 0.05f));
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(Jugador.Sensores.EnSuelo, "El puesto se pisa.");
            Assert.AreEqual(puesto.max.y, Jugador.transform.position.y, 0.1f, "Y no se cae.");
        }

        [UnityTest]
        public IEnumerator LaOscuridad_CambiaPorTramos_YKaelLlevaSuLuz()
        {
            yield return EntrarEnNivel3();
            ApagarEnemigos();

            var oscuridad = Object.FindAnyObjectByType<LuzDelNivel>();
            Assert.IsNotNull(oscuridad);
            Assert.IsTrue(oscuridad.Activa, "El LevelManager la activa: las reglas del Nivel 3 piden oscuridad.");
            Assert.AreEqual(0, oscuridad.TramoActual, "Al empezar, la linde.");
            Assert.AreEqual(oscuridad.Tramos[0].intensidad, oscuridad.LuzGlobal.intensity, 0.01f, "Con luz de luna, sin transición al empezar.");

            var luz = Jugador.GetComponent<LuzJugador>();
            Assert.IsTrue(luz.Encendida, "Kael lleva su luz.");
            Assert.AreEqual(Nivel3.reglas.radioLuzJugador, luz.Radio, 1e-4f, "De 3 u.");

            // En la trinchera empieza la oscuridad completa: la luz baja poco a poco.
            var trinchera = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Exclude).Single(c => c.Id == "L3_CP1");
            yield return LlevarJugadorA(EnElSuelo(trinchera.transform.position));
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1, oscuridad.TramoActual, "El bosque cerrado.");
            Assert.Greater(oscuridad.LuzGlobal.intensity, oscuridad.Tramos[1].intensidad + 0.05f, "No de golpe…");
            yield return new WaitForSeconds(oscuridad.Transicion + 0.3f);
            Assert.AreEqual(oscuridad.Tramos[1].intensidad, oscuridad.LuzGlobal.intensity, 0.01f, "…pero acaba a oscuras.");
            Assert.IsTrue(luz.Encendida, "Y Kael sigue con su luz.");
        }

        [UnityTest]
        public IEnumerator LaPrimeraAntorcha_LlevaSuAviso_SeEnciendeConE_YEnsenaElPrimerCepo()
        {
            yield return EntrarEnNivel3();
            ApagarEnemigos();
            var antorcha = Object.FindObjectsByType<Antorcha>(FindObjectsInactive.Exclude).OrderBy(a => a.transform.position.x).First();
            var cepo = Object.FindObjectsByType<Cepo>(FindObjectsInactive.Exclude).OrderBy(c => c.transform.position.x).First();
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(cepo.SeVe, "Desde el inicio, a oscuras, el primer cepo no se ve.");
            Assert.IsFalse(Interfaz.Hud.Interaccion.Visible, "Lejos de la antorcha, no hay aviso.");

            yield return LlevarJugadorA(new Vector2(antorcha.transform.position.x - 1f, antorcha.transform.position.y + 0.05f));
            yield return null;
            yield return null;
            Assert.IsTrue(Interfaz.Hud.Interaccion.Visible, "Junto a la antorcha, el aviso.");
            StringAssert.Contains("Encender", Interfaz.Hud.Interaccion.Texto);
            Assert.IsFalse(cepo.SeVe, "Todavía a oscuras.");

            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);
            entrada.PulsarInteractuar();
            yield return null;
            yield return null;
            Assert.AreEqual(ESTADOS_ANTORCHA.LIT, antorcha.Estado, "Con E, se enciende.");
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(cepo.SeVe, "Y su luz enseña el primer cepo.");
            Assert.IsFalse(Interfaz.Hud.Interaccion.Visible, "Encendida, el aviso se va.");
        }

        [UnityTest]
        public IEnumerator LaPrimeraAntorcha_DespiertaAlPrimerEspectro_QueNoSeAcercaAKaelEnLaLuz()
        {
            yield return EntrarEnNivel3();
            var espectros = Object.FindObjectsByType<Espectro>(FindObjectsInactive.Exclude).OrderBy(e => e.transform.position.x).ToList();
            Assert.AreEqual(6, espectros.Count);
            Assert.IsTrue(espectros.All(e => e.Dormido), "Al empezar, todos duermen.");

            var antorcha = Object.FindObjectsByType<Antorcha>(FindObjectsInactive.Exclude).OrderBy(a => a.transform.position.x).First();
            yield return LlevarJugadorA(new Vector2(antorcha.transform.position.x - 1f, antorcha.transform.position.y + 0.05f));
            Assert.IsTrue(antorcha.Encender());
            yield return null;
            Assert.IsFalse(espectros[0].Dormido, "La primera antorcha despierta al primer Espectro…");
            Assert.IsTrue(espectros.Skip(1).All(e => e.Dormido), "…y a ninguno más.");

            for (var fin = Time.time + 2f; Time.time < fin;)
            {
                Assert.GreaterOrEqual(Vector2.Distance(espectros[0].transform.position, Jugador.transform.position), 3f, "Con Kael en la luz, no se le acerca.");
                yield return null;
            }

            Assert.AreEqual(ESTADOS_ESPECTRO.AL_BORDE, espectros[0].Estado, "Espera en el borde de la luz.");
        }

        [UnityTest]
        public IEnumerator ElPavor_EsperaVisible_SeDesvaneceAlEmpezar_YAlVencerloLaOscuridadRetrocede()
        {
            yield return EntrarEnNivel3();
            ApagarEnemigos();
            var arena = Object.FindAnyObjectByType<ArenaJefe>();
            var pavor = arena.Jefe as EspectroDelPavor;
            Assert.IsNotNull(pavor, "El jefe de la arena es el Espectro del Pavor.");
            Assert.IsTrue(pavor.Visible, "Antes del combate espera visible en el centro de su arena: su presentación.");

            // Lo mismo que hace la arena cuando Kael cruza su entrada.
            arena.IniciarCombate(Jugador);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(pavor.EnCombate);
            Assert.IsFalse(pavor.Visible, "Al empezar el combate se desvanece.");

            var oscuridad = Object.FindAnyObjectByType<LuzDelNivel>();
            Assert.IsFalse(oscuridad.Retrocedida);
            pavor.Vida.Matar();
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(pavor.Derrotado);
            Assert.IsTrue(oscuridad.Retrocedida, "Al vencerlo, la oscuridad retrocede…");

            // Cuando termina de caer, la cinemática de su derrota, que para el tiempo: se salta.
            yield return Hasta(() => Juego.EstadoActual == ESTADOS_JUEGO.CUTSCENE, 4f, "la cinemática de su derrota");
            yield return SaltarCinematicaSiHay();
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);
            yield return new WaitForSeconds(pavor.SubidaAlMorir + 0.3f);
            Assert.AreEqual(pavor.LuzAlMorir, oscuridad.LuzGlobal.intensity, 0.02f, "…y en 3 s el bosque se ve entero.");
        }

        [UnityTest]
        public IEnumerator ElPavor_SePresentaYCaeConSusCinematicas_YLaPuertaDelArbolSeAbreAlRecogerSuFragmento()
        {
            yield return EntrarEnNivel3();
            ApagarEnemigos();
            var arena = Object.FindAnyObjectByType<ArenaJefe>();
            var pavor = (EspectroDelPavor)arena.Jefe;
            var salida = Object.FindAnyObjectByType<SalidaNivel>();
            var puerta = salida.Puerta;
            Assert.IsNotNull(puerta, "La salida es la puerta del árbol.");
            Assert.IsFalse(puerta.Abierta, "Cerrada mientras él viva.");

            // Al cruzar la entrada, su presentación: sus planos, opacos y en su rejilla.
            yield return LlevarJugadorA(EnElSuelo(arena.transform.position));
            Assert.AreEqual(ESTADOS_JUEGO.CUTSCENE, Juego.EstadoActual, "Al entrar, la presentación del Pavor.");
            Assert.IsNotNull(Nivel3.cinematicaJefe);
            Assert.AreSame(Nivel3.cinematicaJefe, Juego.CinematicaActual);
            Assert.IsFalse(Interfaz.Cinematica.SobreElJuego, "Con sus planos, es opaca.");
            Assert.AreEqual(new Vector2Int(480, 268), Interfaz.CamaraCinematica.Rejilla, "Sus planos de 480 × 268, en su rejilla.");
            yield return SaltarCinematicaSiHay();
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);
            Assert.IsTrue(pavor.EnCombate);
            Assert.IsFalse(pavor.Fragmento.GetComponent<BrilloDeFragmento>().Encendido, "Mientras pelea, su fragmento no brilla.");

            // Cae en su montón de hojas con el fragmento encima; su cinemática, y de vuelta al juego.
            pavor.Vida.Matar();
            yield return Hasta(() => pavor.Fragmento.Disponible, 5f, "que el fragmento aparezca sobre su montón de hojas");
            Assert.AreEqual(ESTADOS_JUEGO.CUTSCENE, Juego.EstadoActual, "Cuando termina de caer, la cinemática de su derrota.");
            Assert.IsNotNull(Nivel3.cinematicaJefeDerrotado);
            Assert.AreSame(Nivel3.cinematicaJefeDerrotado, Juego.CinematicaActual);
            yield return SaltarCinematicaSiHay();
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual, "Y se vuelve al juego, con el fragmento en el montón.");
            Assert.IsFalse(salida.Abierta, "Sin el fragmento, la puerta sigue cerrada.");
            var brillo = pavor.Fragmento.GetComponent<BrilloDeFragmento>();
            Assert.IsTrue(brillo.Encendido, "El fragmento brilla sobre el montón.");
            Assert.IsNotNull(brillo.Copia.sprite, "Con sus píxeles, recortados de M5.");
            Assert.IsTrue(brillo.Destello.enabled, "Y su silueta destella con el latido.");
            Assert.AreSame(brillo.Copia.sprite, brillo.Destello.sprite);

            yield return LlevarJugadorA(new Vector2(pavor.Fragmento.Centro.x, arena.transform.position.y + 0.05f));
            yield return Hasta(() => salida.Abierta, 4f, "que la arena se abra tras recoger el fragmento");
            CollectionAssert.Contains(Gestor.Datos.fragmentos, "MIEDO");
            Assert.IsTrue(puerta.Abierta, "Al recoger el fragmento, la puerta del árbol se abre.");
            Assert.IsFalse(brillo.Encendido, "Recogido, deja de brillar.");
            Assert.IsFalse(brillo.Destello.enabled, "Ni destella.");
        }

        private static IEnumerator Hasta(System.Func<bool> condicion, float maximo, string que)
        {
            var limite = Time.unscaledTime + maximo;
            while (!condicion())
            {
                if (Time.unscaledTime > limite)
                {
                    Assert.Fail($"Tiempo agotado esperando {que}.");
                }

                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator LaCienaga_LaMaranaSujetaAKael_YSoloElDashAcuaticoLaCruza()
        {
            yield return EntrarEnNivel3();
            ApagarEnemigos();
            Jugador.Habilidades.Desbloquear(HABILIDADES.DASH_ACUATICO);

            // En el fondo del primer túnel, a 1 u de la primera maraña, nadando hacia ella.
            var marana = Object.FindObjectsByType<Marana>(FindObjectsInactive.Exclude).Single(m => m.name == "Marana1");
            var caja = marana.GetComponent<BoxCollider2D>().bounds;
            yield return LlevarJugadorA(new Vector2(caja.min.x - 1f, caja.min.y + 0.05f));
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);
            entrada.EjeHorizontal = 1f;
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(Jugador.Motor.BajoElAgua, "Bajo el agua de la ciénaga.");
            Assert.IsTrue(marana.Sujetando, "Nadando, la maraña lo sujeta.");
            Assert.Less(Jugador.transform.position.x, caja.center.x, "Y casi no avanza: no llega ni a la mitad.");
            StringAssert.StartsWith("Atrapa", marana.Dibujos[0].sprite.name, "Se cierra sobre él.");

            // De golpe: un Dash Acuático la cruza entera, con el cuerpo.
            entrada.PulsarDash();
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(marana.Sujetando, "Ya no lo sujeta.");
            Assert.Greater(Jugador.transform.position.x, caja.max.x, "Está al otro lado.");
            Assert.IsTrue(Jugador.Motor.BajoElAgua, "Todavía bajo el agua: el túnel sigue.");
            entrada.EjeHorizontal = 0f;
        }

        [UnityTest]
        public IEnumerator SinElDashAcuatico_LaMaranaNoSeCruza()
        {
            yield return EntrarEnNivel3();
            ApagarEnemigos();
            Assert.IsFalse(Jugador.Habilidades.Tiene(HABILIDADES.DASH_ACUATICO), "Sin terminar el Mundo 2, Kael no lo tiene.");

            var marana = Object.FindObjectsByType<Marana>(FindObjectsInactive.Exclude).Single(m => m.name == "Marana1");
            var caja = marana.GetComponent<BoxCollider2D>().bounds;
            yield return LlevarJugadorA(new Vector2(caja.min.x - 1f, caja.min.y + 0.05f));
            var entrada = new InputSimulado();
            Jugador.FijarFuenteInput(entrada);
            entrada.EjeHorizontal = 1f;
            yield return new WaitForSeconds(1f);
            entrada.PulsarDash();
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(marana.Sujetando, "Bajo el agua no hay dash sin el Dash Acuático: sigue sujeto.");
            Assert.Less(Jugador.transform.position.x, caja.center.x);
            entrada.EjeHorizontal = 0f;
        }

        [UnityTest]
        public IEnumerator EnElNivel1_NoHayOscuridad_NiLuzDeKael()
        {
            yield return EntrarEnNivel1();
            Assert.IsFalse(Jugador.GetComponent<LuzJugador>().Encendida, "Fuera del Miedo, Kael no lleva luz.");
            var oscuridad = Object.FindAnyObjectByType<LuzDelNivel>();
            Assert.IsTrue(oscuridad == null || !oscuridad.Activa, "Ni hay oscuridad.");
        }
    }
}
