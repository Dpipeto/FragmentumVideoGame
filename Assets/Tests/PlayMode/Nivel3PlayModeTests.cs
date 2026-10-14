using System.Collections;
using System.Linq;
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
    /// enciende y enseña el primer cepo (fase B); y esa antorcha despierta al primer Espectro, que
    /// espera en el borde de su luz (fase C). El Nivel 1 sigue con luz. Las medidas las
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
        public IEnumerator EnElNivel1_NoHayOscuridad_NiLuzDeKael()
        {
            yield return EntrarEnNivel1();
            Assert.IsFalse(Jugador.GetComponent<LuzJugador>().Encendida, "Fuera del Miedo, Kael no lleva luz.");
            var oscuridad = Object.FindAnyObjectByType<LuzDelNivel>();
            Assert.IsTrue(oscuridad == null || !oscuridad.Activa, "Ni hay oscuridad.");
        }
    }
}
