using System.Collections;
using Fragmentum.Core.Juego;
using Fragmentum.Player;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// La guía de mecánicas con los sistemas reales: se abre desde el menú principal y desde la
    /// pausa, vuelve a la pausa al cerrarse y crece con las habilidades integradas.
    /// </summary>
    public class MenuMecanicasPlayModeTests : PruebaFlujoBase
    {
        private PantallaMecanicas Mecanicas => Interfaz.Mecanicas;

        [UnityTest]
        public IEnumerator DesdeElMenu_CreceConLasHabilidadesIntegradas()
        {
            UIManager.Instancia.MostrarMecanicas();
            yield return null;

            Assert.IsTrue(Mecanicas.Visible);
            Assert.AreEqual(0, Mecanicas.HabilidadesIntegradas);
            Assert.AreEqual(4, Mecanicas.HabilidadesTotales);

            Mecanicas.Opciones[0].onClick.Invoke(); // Volver
            Assert.IsFalse(Mecanicas.Visible);
            Assert.AreEqual(ESTADOS_JUEGO.MAIN_MENU, Juego.EstadoActual);

            Gestor.Datos.habilidades.Add(HABILIDADES.ATAQUE_CARGADO.ToString());
            Gestor.Datos.habilidades.Add(HABILIDADES.DASH_ACUATICO.ToString());
            UIManager.Instancia.MostrarMecanicas();
            yield return null;

            Assert.AreEqual(2, Mecanicas.HabilidadesIntegradas, "Se recalcula cada vez que se abre.");

            Interfaz.AlternarPausa(); // Esc cierra
            Assert.IsFalse(Mecanicas.Visible);
        }

        [UnityTest]
        public IEnumerator DesdeLaPausa_TapaElMenu_YAlCerrarseVuelveALaPausa()
        {
            yield return EntrarEnNivel1();

            Interfaz.AlternarPausa();
            Assert.AreEqual(ESTADOS_JUEGO.PAUSED, Juego.EstadoActual);

            Interfaz.MenuPausa.Opciones[2].onClick.Invoke(); // Mecánicas
            yield return null;
            Assert.IsTrue(Mecanicas.Visible);
            Assert.IsFalse(Interfaz.MenuPausa.Visible, "La guía tapa la pausa, no se suma a ella.");
            Assert.AreEqual(ESTADOS_JUEGO.PAUSED, Juego.EstadoActual, "Sigue en pausa.");
            Assert.AreEqual(Nivel1.ColorAcento, Mecanicas.Acento, "Con el color del mundo, como la pausa.");

            Mecanicas.Opciones[0].onClick.Invoke(); // Volver
            Assert.IsFalse(Mecanicas.Visible);
            Assert.IsTrue(Interfaz.MenuPausa.Visible, "Volver regresa a la pausa.");

            Interfaz.MenuPausa.Opciones[2].onClick.Invoke();
            Interfaz.AlternarPausa(); // Esc: primero cierra la guía…
            Assert.IsFalse(Mecanicas.Visible);
            Assert.IsTrue(Interfaz.MenuPausa.Visible);
            Assert.AreEqual(ESTADOS_JUEGO.PAUSED, Juego.EstadoActual);

            Interfaz.AlternarPausa(); // …y después reanuda.
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator AlDerrotarAlJefe_LaGuiaYaExplicaLaHabilidadNueva()
        {
            yield return EntrarEnNivel1();

            Gestor.OtorgarRecompensa(Nivel1);
            Interfaz.AlternarPausa();
            UIManager.Instancia.MostrarMecanicas();
            yield return null;

            Assert.AreEqual(1, Mecanicas.HabilidadesIntegradas, "La recompensa del jefe llega en el acto.");
            Assert.IsTrue(Jugador.Habilidades.Tiene(HABILIDADES.ATAQUE_CARGADO));
        }
    }
}
