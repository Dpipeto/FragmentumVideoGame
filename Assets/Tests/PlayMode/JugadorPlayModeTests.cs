using System.Collections;
using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Pruebas de aceptación del jugador con física real: instancian el prefab del juego,
    /// lo controlan con <see cref="InputSimulado"/> y comprueban los criterios del encargo
    /// (sección 7) que dependen del movimiento. Cada prueba construye su propio escenario.
    /// </summary>
    public class JugadorPlayModeTests : PruebaPlayModeBase
    {
        private Rigidbody2D Cuerpo => Jugador.Motor.Cuerpo;

        // --- Criterio: mantener pulsado espacio ya no produce salto flotante ni infinito ---

        [UnityTest]
        public IEnumerator MantenerElSalto_DaUnSoloSaltoDeAlturaNormal()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Input.PulsarSalto(mantener: true);
            var maximo = 0f;
            yield return Durante(2f, () => maximo = Mathf.Max(maximo, PiesJugador.y));

            Assert.That(maximo, Is.InRange(2.0f, 2.9f), "Altura de un salto completo (~2,5 tiles).");
            Assert.AreEqual(1, Veces(ESTADOS_JUGADOR.JUMP), "Mantener el botón no debe encadenar saltos.");
            Assert.AreEqual(0, Veces(ESTADOS_JUGADOR.DOUBLE_JUMP), "Mantener no gasta el doble salto.");
            Assert.IsTrue(Jugador.Sensores.EnSuelo, "Debe volver al suelo.");
            Assert.AreEqual(ESTADOS_JUGADOR.IDLE, Jugador.EstadoActual);
        }

        [UnityTest]
        public IEnumerator SoltarElSaltoPronto_SaltaMenos()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Input.PulsarSalto(mantener: true);
            yield return null;
            yield return null;
            Input.SoltarSalto();

            var maximo = 0f;
            yield return Durante(1.2f, () => maximo = Mathf.Max(maximo, PiesJugador.y));

            Assert.That(maximo, Is.InRange(0.2f, 1.3f), "Un toque corto debe dar un salto bajo (altura variable).");
        }

        [UnityTest]
        public IEnumerator DobleSalto_SubeMasYSoloUnaVez()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Input.PulsarSalto();
            yield return EsperarEstado(ESTADOS_JUGADOR.FALL, 1.5f);
            Input.PulsarSalto();
            yield return EsperarEstado(ESTADOS_JUGADOR.DOUBLE_JUMP, 0.5f);

            var maximo = 0f;
            yield return Durante(0.6f, () => maximo = Mathf.Max(maximo, PiesJugador.y));
            Input.PulsarSalto();
            yield return Durante(1.5f, () => maximo = Mathf.Max(maximo, PiesJugador.y));

            Assert.Greater(maximo, 3.5f, "El doble salto suma altura sobre el simple.");
            Assert.AreEqual(1, Veces(ESTADOS_JUGADOR.DOUBLE_JUMP), "Un tercer salto en el aire no debe existir.");
        }

        // --- Criterio: estar apoyado sobre dos plataformas solapadas no rompe el suelo ---

        [UnityTest]
        public IEnumerator PlataformasSolapadas_NoRompenLaDeteccionDeSuelo()
        {
            Suelo(-10f, 10f);
            Bloque("SueloSolapado", 6f, 20f, -1.5f, 0f, Capas.Suelo);
            yield return CrearJugadorEnSuelo(new Vector2(4f, 0f));

            var contactosMaximos = 0;
            Input.EjeHorizontal = 1f;
            var limite = Time.time + 4f;
            while (PiesJugador.x < 14f)
            {
                Assert.Less(Time.time, limite, "El jugador no avanzó.");
                Assert.IsTrue(Jugador.Sensores.EnSuelo, $"Perdió el suelo en x = {PiesJugador.x:0.00}.");
                contactosMaximos = Mathf.Max(contactosMaximos, Jugador.Sensores.Suelo.Contactos);
                yield return null;
            }

            Assert.GreaterOrEqual(contactosMaximos, 2, "En la zona solapada debe pisar las dos plataformas.");
            Assert.AreEqual(0, Veces(ESTADOS_JUGADOR.FALL), "Nunca debió caer.");
        }

        // --- Criterio: el jugador se mueve por física ---

        [UnityTest]
        public IEnumerator Correr_MueveElRigidbodyPorVelocidad()
        {
            Suelo(-10f, 20f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Input.EjeHorizontal = 1f;
            yield return new WaitForSeconds(1f);

            Assert.That(Cuerpo.linearVelocity.x, Is.EqualTo(Jugador.Parametros.velocidadMaxima).Within(0.2f));
            Assert.Greater(PiesJugador.x, 4f);
            Assert.AreEqual(ESTADOS_JUGADOR.RUN, Jugador.EstadoActual);

            Input.EjeHorizontal = 0f;
            yield return new WaitForSeconds(0.5f);
            Assert.That(Cuerpo.linearVelocity.x, Is.EqualTo(0f).Within(0.05f), "Al soltar debe frenar.");
            Assert.AreEqual(ESTADOS_JUGADOR.IDLE, Jugador.EstadoActual);
        }

        [UnityTest]
        public IEnumerator Dash_RecorreSuDistanciaYRespetaElCooldown()
        {
            Suelo(-10f, 30f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Input.PulsarDash();
            yield return new WaitForSeconds(0.3f);
            Assert.That(PiesJugador.x, Is.InRange(2.3f, 4.5f), "Distancia del dash.");

            Input.PulsarDash(); // aún en cooldown
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(1, Veces(ESTADOS_JUGADOR.DASH), "El cooldown debe impedir el segundo dash.");

            yield return new WaitForSeconds(0.6f);
            Input.PulsarDash();
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(2, Veces(ESTADOS_JUGADOR.DASH), "Pasado el cooldown, el dash vuelve.");
        }

        // --- Coyote time ---

        [UnityTest]
        public IEnumerator CoyoteTime_PermiteSaltarJustoTrasElBorde()
        {
            Suelo(-10f, 0f);
            yield return CrearJugadorEnSuelo(new Vector2(-3f, 0f));

            Input.EjeHorizontal = 1f;
            yield return EsperarEstado(ESTADOS_JUGADOR.FALL, 2f);
            yield return new WaitForSeconds(0.04f);
            Input.PulsarSalto();
            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(1, Veces(ESTADOS_JUGADOR.JUMP), "Dentro del coyote time es un salto normal.");
            Assert.AreEqual(0, Veces(ESTADOS_JUGADOR.DOUBLE_JUMP), "El coyote no gasta el doble salto.");
        }

        [UnityTest]
        public IEnumerator SinCoyoteTime_SaltarEnElAireUsaElDobleSalto()
        {
            Suelo(-10f, 0f);
            yield return CrearJugadorEnSuelo(new Vector2(-3f, 0f));

            Input.EjeHorizontal = 1f;
            yield return EsperarEstado(ESTADOS_JUGADOR.FALL, 2f);
            yield return new WaitForSeconds(0.25f);
            Input.PulsarSalto();
            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(0, Veces(ESTADOS_JUGADOR.JUMP));
            Assert.AreEqual(1, Veces(ESTADOS_JUGADOR.DOUBLE_JUMP));
        }

        // --- Pared ---

        [UnityTest]
        public IEnumerator WallJump_SaleHaciaFueraDeLaPared()
        {
            Suelo(-10f, 10f);
            Bloque("Pared", 3f, 4f, 0f, 12f, Capas.Wall);
            yield return CrearJugador(new Vector2(2.3f, 6f));

            Input.EjeHorizontal = 1f;
            yield return EsperarEstado(ESTADOS_JUGADOR.WALL_SLIDE, 2f);
            yield return new WaitForSeconds(0.3f);
            Assert.GreaterOrEqual(Cuerpo.linearVelocity.y, -3.6f, "En la pared la caída se frena.");

            Input.PulsarSalto();
            yield return EsperarEstado(ESTADOS_JUGADOR.WALL_JUMP, 0.5f);
            yield return new WaitForFixedUpdate();

            Assert.Less(Cuerpo.linearVelocity.x, 0f, "Con la pared a la derecha, el wall jump sale hacia la izquierda.");
            Assert.Greater(Cuerpo.linearVelocity.y, 0f);
            Assert.AreEqual(-1, Jugador.Mirando);
        }

        [UnityTest]
        public IEnumerator EnUnaParedSinAgarre_NiSeDeslizaNiSaltaDesdeElla()
        {
            // El hielo liso de la muralla del Nivel 2: una pared como la de arriba, pero marcada.
            Suelo(-10f, 10f);
            var pared = Bloque("ParedDeHielo", 3f, 4f, 0f, 12f, Capas.Wall);
            pared.AddComponent<SinAgarre>();
            yield return CrearJugador(new Vector2(2.3f, 6f));

            Input.EjeHorizontal = 1f;
            yield return new WaitForSeconds(0.25f);
            var sensores = Jugador.GetComponent<PlayerSensors>();
            Assert.IsFalse(sensores.ParedDerecha, "Para los sentidos de Kael no es una pared a la que agarrarse.");
            Assert.AreEqual(ESTADOS_JUGADOR.FALL, Jugador.EstadoActual, "Cae junto a ella sin frenarse.");
            Assert.LessOrEqual(PiesJugador.x, 3f - 0.39f, "Pero sigue siendo sólida: no la atraviesa.");

            // El salto junto a ella es el doble salto de siempre, no un salto de pared.
            Input.PulsarSalto();
            for (var i = 0; i < 4; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.AreEqual(0, Veces(ESTADOS_JUGADOR.WALL_SLIDE), "No se desliza por ella.");
            Assert.AreEqual(0, Veces(ESTADOS_JUGADOR.WALL_JUMP), "Ni salta desde ella.");

            // La misma pared, sin la marca, agarra.
            Object.DestroyImmediate(pared.GetComponent<SinAgarre>());
            yield return EsperarEstado(ESTADOS_JUGADOR.WALL_SLIDE, 2f);
        }

        // --- Daño, i-frames, muerte y reaparición ---

        [UnityTest]
        public IEnumerator Golpe_ProvocaRetrocesoEIFrames()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            var pinchos = Bloque("Pinchos", 0.3f, 1.3f, 0f, 1f, Capas.EnemyWeapon, trigger: true);
            var hitbox = pinchos.AddComponent<Hitbox>();
            hitbox.Objetivos = Capas.MascaraJugador;
            hitbox.GolpearUnaVezPorActivacion = false;

            yield return EsperarEstado(ESTADOS_JUGADOR.DAMAGED, 1f);
            yield return new WaitForFixedUpdate();

            var vidaMaxima = Jugador.Vida.VidaMaxima;
            Assert.AreEqual(vidaMaxima - 1, Jugador.Vida.Vida);
            Assert.Less(Cuerpo.linearVelocity.x, 0f, "El retroceso se aleja del golpe.");

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(vidaMaxima - 1, Jugador.Vida.Vida, "Los i-frames impiden un segundo golpe inmediato.");
        }

        [UnityTest]
        public IEnumerator Muerte_YRevivir_VuelvenPorLaTabla()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Jugador.Vida.Matar();
            yield return null;
            Assert.AreEqual(ESTADOS_JUGADOR.DEAD, Jugador.EstadoActual);

            Input.EjeHorizontal = 1f;
            yield return new WaitForSeconds(0.3f);
            Assert.That(PiesJugador.x, Is.EqualTo(0f).Within(0.1f), "Muerto no responde al input.");

            Jugador.Revivir(new Vector2(-2f, 0.05f));
            yield return null;
            Assert.AreNotEqual(ESTADOS_JUGADOR.DEAD, Jugador.EstadoActual);
            Assert.AreEqual(Jugador.Vida.VidaMaxima, Jugador.Vida.Vida);
        }

        // --- Agacharse ---

        [UnityTest]
        public IEnumerator Agachado_BajoUnTecho_NoSeLevantaHastaSalir()
        {
            Suelo(-10f, 10f);
            Bloque("Techo", 1f, 5f, 1.2f, 2.2f, Capas.Suelo);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Input.AgacharseMantenido = true;
            yield return EsperarEstado(ESTADOS_JUGADOR.CROUCH, 0.5f);

            Input.EjeHorizontal = 1f;
            yield return EsperarHasta(() => PiesJugador.x >= 3f, 4f, "avanzar agachado bajo el techo");
            Input.EjeHorizontal = 0f;
            Input.AgacharseMantenido = false;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(ESTADOS_JUGADOR.CROUCH, Jugador.EstadoActual, "Sin espacio arriba no se levanta.");

            Input.EjeHorizontal = 1f;
            yield return EsperarHasta(() => Jugador.EstadoActual != ESTADOS_JUGADOR.CROUCH, 4f, "salir de debajo del techo");
            Assert.Greater(PiesJugador.x, 5f, "Solo se levanta al salir de debajo del techo.");
        }

        // --- Ataque ---

        [UnityTest]
        public IEnumerator Ataque_GolpeaHaciaDondeMira_AAmbosLados()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);
            var derecha = Muneco(1.3f);
            var izquierda = Muneco(-1.3f);

            Input.PulsarAtaque();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(2, derecha.Vida, "Mirando a la derecha golpea al de la derecha.");
            Assert.AreEqual(3, izquierda.Vida, "…y no al de la izquierda.");

            Input.EjeHorizontal = -1f;
            yield return null;
            yield return null;
            Input.EjeHorizontal = 0f;
            yield return new WaitForSeconds(0.2f);

            Input.PulsarAtaque();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(2, izquierda.Vida, "Al girarse golpea al de la izquierda.");
            Assert.AreEqual(2, derecha.Vida);
        }

        [UnityTest]
        public IEnumerator AtaqueCargado_SaleAlSoltarConDobleDanio()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);
            Jugador.Habilidades.Desbloquear(HABILIDADES.ATAQUE_CARGADO);

            var muneco = Muneco(1.3f, vida: 10);
            var golpes = new List<Golpe>();
            muneco.OnGolpeRecibido += golpes.Add;

            Input.PulsarAtaque(mantener: true);
            yield return new WaitForSeconds(0.8f);
            Input.SoltarAtaque();
            yield return new WaitForSeconds(0.8f);

            Assert.AreEqual(2, golpes.Count, "Pulsar da un golpe normal; soltar la carga, otro cargado.");
            Assert.IsFalse(golpes[0].Cargado);
            Assert.AreEqual(1, golpes[0].Cantidad);
            Assert.IsTrue(golpes[1].Cargado);
            Assert.AreEqual(2, golpes[1].Cantidad);
        }

        // --- Modificadores y habilidades ---

        [UnityTest]
        public IEnumerator BajoElAgua_ElDashRequiereDashAcuatico()
        {
            Suelo(-10f, 10f);
            var agua = Bloque("Agua", -5f, 5f, 0f, 4f, null, trigger: true);
            agua.AddComponent<ZonaModificadorMovimiento>();
            yield return CrearJugadorEnSuelo(Vector2.zero);
            yield return EsperarHasta(() => Jugador.Motor.BajoElAgua, 1f, "entrar en el agua");

            Input.PulsarDash();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(0, Veces(ESTADOS_JUGADOR.DASH), "Sin Dash Acuático no hay dash bajo el agua.");

            Jugador.Habilidades.Desbloquear(HABILIDADES.DASH_ACUATICO);
            Input.PulsarDash();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, Veces(ESTADOS_JUGADOR.DASH));
        }

        [UnityTest]
        public IEnumerator InvisibilidadParcial_SeActivaConLaTeclaDeHabilidad()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            Input.PulsarHabilidad();
            yield return null;
            Assert.IsFalse(Jugador.Habilidades.EsInvisible, "Bloqueada hasta obtener el fragmento del Miedo.");

            Jugador.Habilidades.Desbloquear(HABILIDADES.INVISIBILIDAD_PARCIAL);
            Input.PulsarHabilidad();
            yield return null;
            yield return null;
            Assert.IsTrue(Jugador.Habilidades.EsInvisible);
        }
    }
}
