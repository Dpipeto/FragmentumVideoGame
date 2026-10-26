using System.Collections;
using System.IO;
using Fragmentum.Core;
using Fragmentum.Core.Juego;
using Fragmentum.Levels;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Las opciones y la música del menú, con los sistemas reales: el volumen se cambia desde el menú
    /// principal y desde la pausa, se oye al momento y se guarda aparte de la partida; la música del menú
    /// sigue sonando en el selector sin cortarse y se apaga al entrar en un nivel.
    /// </summary>
    public class OpcionesPlayModeTests : PruebaFlujoBase
    {
        private PantallaOpciones Opciones => Interfaz.Opciones;

        private MusicaMenu MusicaMenu => Interfaz.MusicaMenu;

        private IEnumerator QueSueneLaDelMenu()
        {
            var limite = Time.unscaledTime + 3f;
            while (MusicaMenu.Nivel < 1f)
            {
                Assert.Less(Time.unscaledTime, limite, "La música del menú no llegó a sonar.");
                yield return null;
            }

            Assert.IsTrue(MusicaMenu.Sonando);
            Assert.IsNotNull(BaseDeDatos.musicaMenu, "La LevelDatabase tiene la música del menú.");
            Assert.AreSame(BaseDeDatos.musicaMenu, MusicaMenu.Fuente.clip);
        }

        [UnityTest]
        public IEnumerator DesdeElMenu_ElVolumenSeOyeAlMomento_YSeGuardaAparte()
        {
            yield return QueSueneLaDelMenu();
            Assert.AreEqual(MusicaMenu.Volumen, MusicaMenu.Fuente.volume, 1e-3f, "Sin tocar nada, a su volumen.");

            UIManager.Instancia.MostrarOpciones();
            yield return null;
            Assert.IsTrue(Opciones.Visible);
            Assert.AreEqual(PantallaOpciones.Pasos, Opciones.General.value, "Todo empieza al 100 %.");
            Assert.AreEqual(PantallaOpciones.Pasos, Opciones.Musica.value);
            Assert.AreEqual(PantallaOpciones.Pasos, Opciones.Efectos.value);

            Opciones.Efectos.value = 3;
            Assert.AreEqual(0.3f, AjustesAudio.Efectos, 1e-4f, "Los efectos, aparte de la música.");
            Assert.AreEqual(1f, AjustesAudio.Musica);

            Opciones.Musica.value = 5;
            Assert.AreEqual(0.5f, AjustesAudio.Musica, 1e-4f);
            yield return null;
            Assert.AreEqual(MusicaMenu.Volumen * 0.5f, MusicaMenu.Fuente.volume, 1e-3f, "La música del menú baja al momento.");

            Opciones.General.value = 7;
            Assert.AreEqual(0.7f, AjustesAudio.General, 1e-4f);
            Assert.AreEqual(0.7f, AudioListener.volume, 1e-4f, "El general es el de todo lo que suena.");
            FileAssert.Exists(Path.Combine(CarpetaGuardado, AjustesAudio.NombreArchivo), "Se guarda solo, aparte de la partida.");

            Opciones.Opciones[0].onClick.Invoke(); // Volver
            Assert.IsFalse(Opciones.Visible);
            Assert.AreEqual(ESTADOS_JUEGO.MAIN_MENU, Juego.EstadoActual);

            UIManager.Instancia.MostrarOpciones();
            Assert.AreEqual(5, Opciones.Musica.value, "Al volver a abrirlas, lo que se dejó.");
            Assert.AreEqual(7, Opciones.General.value);
            Assert.AreEqual(3, Opciones.Efectos.value);
            Interfaz.AlternarPausa(); // Esc cierra
            Assert.IsFalse(Opciones.Visible);

            Gestor.NuevaPartida();
            Assert.AreEqual(0.5f, AjustesAudio.Musica, 1e-4f, "Una partida nueva no toca el volumen.");
        }

        [UnityTest]
        public IEnumerator LaMusicaDelMenu_SigueEnElSelectorSinCortarse_YSeApagaEnLosNiveles()
        {
            yield return QueSueneLaDelMenu();

            Assert.IsTrue(Juego.IrAlSelector());
            var limite = Time.unscaledTime + 5f;
            while (Juego.EstadoActual != ESTADOS_JUEGO.LEVEL_SELECT)
            {
                Assert.Less(Time.unscaledTime, limite, "No se llegó al selector.");
                Assert.IsTrue(MusicaMenu.Sonando, "No se corta al pasar al selector.");
                Assert.AreSame(BaseDeDatos.musicaMenu, MusicaMenu.Fuente.clip, "Es la misma.");
                yield return null;
            }

            yield return EsperarReal(0.5f);
            Assert.IsTrue(MusicaMenu.Sonando);
            Assert.AreEqual(1f, MusicaMenu.Nivel, 1e-3f, "En el selector, a su volumen.");

            yield return EntrarEnNivel1();
            yield return EsperarReal(MusicaMenu.Fundido + 0.3f);
            Assert.IsFalse(MusicaMenu.Sonando, "En el nivel se apaga.");
            Assert.AreEqual(0f, MusicaMenu.Fuente.volume, 1e-4f);
            var nivel = Object.FindAnyObjectByType<LevelManager>();
            Assert.AreSame(Nivel1.musica, nivel.Musica.Fuente.clip, "Y suena la del nivel.");
        }

        [UnityTest]
        public IEnumerator DesdeLaPausa_BajaLaMusicaDelNivel_YAlCerrarseVuelveALaPausa()
        {
            yield return EntrarEnNivel1();
            var nivel = Object.FindAnyObjectByType<LevelManager>();
            Interfaz.AlternarPausa();
            Assert.AreEqual(ESTADOS_JUEGO.PAUSED, Juego.EstadoActual);

            Interfaz.MenuPausa.Opciones[3].onClick.Invoke(); // Opciones
            yield return null;
            Assert.IsTrue(Opciones.Visible);
            Assert.IsFalse(Interfaz.MenuPausa.Visible, "Las opciones tapan la pausa, no se suman a ella.");
            Assert.AreEqual(ESTADOS_JUEGO.PAUSED, Juego.EstadoActual, "Sigue en pausa.");
            Assert.AreEqual(Nivel1.ColorAcento, Opciones.Acento, "Con el color del mundo, como la pausa.");

            Opciones.Musica.value = 2;
            yield return null;
            Assert.AreEqual(0.6f * 0.2f, nivel.Musica.Fuente.volume, 1e-3f, "La música del nivel baja al momento, en la pausa.");

            Opciones.Opciones[0].onClick.Invoke(); // Volver
            Assert.IsFalse(Opciones.Visible);
            Assert.IsTrue(Interfaz.MenuPausa.Visible, "Volver regresa a la pausa.");

            Interfaz.MenuPausa.Opciones[3].onClick.Invoke();
            Interfaz.AlternarPausa(); // Esc: primero cierra las opciones…
            Assert.IsFalse(Opciones.Visible);
            Assert.IsTrue(Interfaz.MenuPausa.Visible);
            Interfaz.AlternarPausa(); // …y después reanuda.
            Assert.AreEqual(ESTADOS_JUEGO.PLAYING, Juego.EstadoActual);
        }
    }
}
