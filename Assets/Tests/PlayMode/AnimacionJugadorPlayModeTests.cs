using System.Collections;
using Fragmentum.Core;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Kael anima lo que hace: cada estado de la FSM reproduce su clip (PlayerAnimacion) con
    /// el prefab real y física real. Tras cada cambio de estado se espera un frame más, porque
    /// el Animator aplica el clip nuevo después del Update en el que cambió el estado.
    /// </summary>
    public class AnimacionJugadorPlayModeTests : PruebaPlayModeBase
    {
        private Animator Animador => Jugador.GetComponentInChildren<Animator>();

        private bool Suena(string estado) => Animador.GetCurrentAnimatorStateInfo(0).IsName(estado);

        private IEnumerator EsperarClip(ESTADOS_JUGADOR estado, float maximo)
        {
            yield return EsperarEstado(estado, maximo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Correr_Saltar_Caer_YAterrizar_CambianDeClip()
        {
            Suelo(-20f, 20f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Assert.IsTrue(Suena(PlayerAnimacion.Idle), "Quieto: Idle.");
            var sprite = Animador.GetComponent<SpriteRenderer>();
            Assert.AreEqual(PiesJugador.y, sprite.bounds.min.y, 0.02f, "Los pies del sprite pisan donde pisa el cuerpo.");

            Input.EjeHorizontal = 1f;
            yield return EsperarClip(ESTADOS_JUGADOR.RUN, 0.5f);
            Assert.IsTrue(Suena(PlayerAnimacion.Correr), "Corriendo: Correr.");

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(Jugador.Parametros.velocidadMaxima, Animador.GetFloat(AnimatorParams.Movement.Hash), 0.2f,
                "Movement lleva la velocidad real y marca el ritmo de las piernas.");
            Input.EjeHorizontal = 0f;

            Input.PulsarSalto();
            yield return EsperarClip(ESTADOS_JUGADOR.JUMP, 0.5f);
            Assert.IsTrue(Suena(PlayerAnimacion.Salto), "Subiendo: Salto.");

            yield return EsperarClip(ESTADOS_JUGADOR.FALL, 1.5f);
            Assert.IsTrue(Suena(PlayerAnimacion.Caida) || Suena(PlayerAnimacion.CaidaBucle), "Bajando: Caida.");

            yield return EsperarHasta(() => Jugador.EstadoActual == ESTADOS_JUGADOR.IDLE, 2f, "aterrizar");
            yield return null;
            Assert.IsTrue(Suena(PlayerAnimacion.Idle), "Al aterrizar vuelve a Idle.");
        }

        [UnityTest]
        public IEnumerator DobleSalto_VuelveAlPrimerFotogramaDelSalto()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Input.PulsarSalto();
            yield return EsperarClip(ESTADOS_JUGADOR.JUMP, 0.5f);
            yield return new WaitForSeconds(0.2f);
            var antes = Animador.GetCurrentAnimatorStateInfo(0).normalizedTime;

            Input.PulsarSalto();
            yield return EsperarClip(ESTADOS_JUGADOR.DOUBLE_JUMP, 0.5f);
            var info = Animador.GetCurrentAnimatorStateInfo(0);

            Assert.IsTrue(info.IsName(PlayerAnimacion.Salto));
            Assert.Less(info.normalizedTime, antes, "El doble salto reinicia el clip aunque ya sonara Salto.");
        }

        [UnityTest]
        public IEnumerator Ataque_SeEstiraALaDuracionDelGolpe_NormalYCargado()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);
            Jugador.Habilidades.Desbloquear(HABILIDADES.ATAQUE_CARGADO);

            Input.PulsarAtaque(mantener: true);
            yield return EsperarClip(ESTADOS_JUGADOR.ATTACK, 0.5f);
            Assert.IsTrue(Suena(PlayerAnimacion.Ataque));
            Assert.AreEqual(1f / Jugador.Parametros.duracionAtaque, Animador.GetFloat(AnimatorParams.VelocidadAtaque.Hash), 0.01f,
                "El clip de 1 s ocupa justo el ataque normal.");

            yield return EsperarHasta(() => Jugador.EstadoActual == ESTADOS_JUGADOR.IDLE, 1f, "terminar el ataque");
            yield return null;
            Assert.IsTrue(Suena(PlayerAnimacion.Idle), "Tras el golpe vuelve a Idle.");

            // Mantener carga el golpe; al soltar sale el cargado, más lento.
            yield return new WaitForSeconds(0.6f);
            Input.SoltarAtaque();
            yield return EsperarHasta(() => Jugador.EstadoActual == ESTADOS_JUGADOR.ATTACK, 0.5f, "el ataque cargado");
            yield return null;
            Assert.IsTrue(Suena(PlayerAnimacion.Ataque));
            Assert.AreEqual(1f / Jugador.Parametros.duracionAtaqueCargado, Animador.GetFloat(AnimatorParams.VelocidadAtaque.Hash), 0.01f,
                "El cargado dura más: el mismo clip se ve más lento.");
        }

        [UnityTest]
        public IEnumerator Dash_SinClipPropio_UsaElDeCorrerSinReiniciarlo()
        {
            Suelo(-10f, 20f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Input.EjeHorizontal = 1f;
            yield return EsperarClip(ESTADOS_JUGADOR.RUN, 0.5f);
            yield return new WaitForSeconds(0.2f);
            var antes = Animador.GetCurrentAnimatorStateInfo(0).normalizedTime;

            Input.PulsarDash();
            yield return EsperarClip(ESTADOS_JUGADOR.DASH, 0.5f);
            var info = Animador.GetCurrentAnimatorStateInfo(0);

            Assert.IsTrue(info.IsName(PlayerAnimacion.Correr), "El dash usa el clip de correr hasta que tenga el suyo.");
            Assert.GreaterOrEqual(info.normalizedTime, antes, "Correr ya sonaba: sigue sin saltar al primer fotograma.");
            Assert.AreEqual(PlayerAnimacion.Correr, Jugador.GetComponent<PlayerAnimacion>().Ultimo);
        }
    }
}
