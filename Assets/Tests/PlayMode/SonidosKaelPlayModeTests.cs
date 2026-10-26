using System;
using System.Collections;
using System.IO;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Los efectos de sonido de Kael en juego (<see cref="SonidosKael"/>): el salto y el doble salto, el dash, el
    /// tajo y el cargado, y el daño (pero no el golpe que para el escudo). Suenan al volumen de «Efectos», cada vez
    /// un poco más agudos o más graves.
    /// </summary>
    public class SonidosKaelPlayModeTests : PruebaPlayModeBase
    {
        private string _carpeta;

        private SonidosKael Sonidos => Jugador.GetComponent<SonidosKael>();

        // Los ajustes, en una carpeta propia: la prueba no toca los del jugador.
        [SetUp]
        public void Preparar()
        {
            _carpeta = Path.Combine(Path.GetTempPath(), "FragmentumPruebas", Guid.NewGuid().ToString("N"));
            AjustesAudio.UsarCarpeta(_carpeta);
        }

        [TearDown]
        public void Olvidar()
        {
            AjustesAudio.UsarCarpeta(null);
            if (Directory.Exists(_carpeta))
            {
                Directory.Delete(_carpeta, true);
            }
        }

        private void AssertSuena(AudioClip esperado, string que)
        {
            Assert.IsNotNull(esperado, $"Kael no tiene el sonido de {que}: regenera el jugador.");
            Assert.AreSame(esperado, Sonidos.Ultimo, $"Suena {que}.");
            Assert.AreSame(esperado, Sonidos.UltimaFuente.clip);
            Assert.IsTrue(Sonidos.UltimaFuente.isPlaying, $"{que}: sonando.");
        }

        [UnityTest]
        public IEnumerator AlSaltar_SuenaElSalto_YEnElAireElDobleSalto()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);
            Assert.IsNull(Sonidos.Ultimo, "Quieto en el suelo, Kael no suena.");

            Input.PulsarSalto();
            yield return EsperarEstado(ESTADOS_JUGADOR.JUMP, 0.5f);
            AssertSuena(Sonidos.Salto, "el salto");

            yield return EsperarEstado(ESTADOS_JUGADOR.FALL, 1.5f);
            Input.PulsarSalto();
            yield return EsperarEstado(ESTADOS_JUGADOR.DOUBLE_JUMP, 0.5f);
            AssertSuena(Sonidos.DobleSalto, "el doble salto");
        }

        [UnityTest]
        public IEnumerator AlHacerElDash_SuenaElDash()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Input.PulsarDash();
            yield return EsperarEstado(ESTADOS_JUGADOR.DASH, 0.5f);
            AssertSuena(Sonidos.Dash, "el dash");
        }

        [UnityTest]
        public IEnumerator AlAtacar_SuenaElTajo_YElCargadoSuenaAlSuyo()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Input.PulsarAtaque();
            yield return EsperarEstado(ESTADOS_JUGADOR.ATTACK, 0.5f);
            AssertSuena(Sonidos.Ataque, "el tajo");
            yield return EsperarEstado(ESTADOS_JUGADOR.IDLE, 1f);

            Jugador.Habilidades.Desbloquear(HABILIDADES.ATAQUE_CARGADO);
            Input.PulsarAtaque(mantener: true);
            yield return new WaitForSeconds(0.8f);
            Input.SoltarAtaque();
            yield return EsperarHasta(() => Jugador.AtaqueEnCursoCargado, 0.5f, "el tajo cargado");
            AssertSuena(Sonidos.AtaqueCargado, "el tajo cargado");
        }

        [UnityTest]
        public IEnumerator AlRecibirUnGolpe_SuenaElDanio_PeroNoSiLoParaElEscudo()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Jugador.Vida.EscudoDesbloqueado = true;
            Jugador.Vida.RecibirGolpe(new Golpe(1, PiesJugador + Vector2.left * 2f));
            yield return null;
            Assert.IsNull(Sonidos.Ultimo, "El escudo lo para: no hay daño.");

            yield return new WaitForSeconds(0.6f); // la invulnerabilidad tras el escudo
            var vida = Jugador.Vida.Vida;
            Jugador.Vida.RecibirGolpe(new Golpe(1, PiesJugador + Vector2.left * 2f));
            yield return null;
            Assert.AreEqual(vida - 1, Jugador.Vida.Vida, "El escudo se está recargando: este golpe llega.");
            AssertSuena(Sonidos.Danio, "el daño");
        }

        [UnityTest]
        public IEnumerator SuenaAlVolumenDeEfectos_YCadaVezUnPocoDistinto()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);
            AjustesAudio.Efectos = 0.5f;

            Input.PulsarSalto();
            yield return EsperarEstado(ESTADOS_JUGADOR.JUMP, 0.5f);
            Assert.AreEqual(Sonidos.Volumen * 0.5f, Sonidos.UltimaFuente.volume, 1e-4f, "Sobre el volumen de «Efectos».");
            Assert.That(Sonidos.UltimaFuente.pitch, Is.InRange(0.95f, 1.05f), "Un poco más agudo o más grave, no mucho.");
            Assert.AreEqual(0f, Sonidos.UltimaFuente.spatialBlend, "Se oye igual esté donde esté (la cámara lo sigue).");
        }
    }
}
