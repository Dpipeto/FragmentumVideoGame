using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Pruebas de aceptación del jugador con física real: instancian el prefab del juego,
    /// lo controlan con <see cref="InputSimulado"/> y comprueban los criterios del encargo
    /// (sección 7) que dependen del movimiento. Cada prueba construye su propio escenario.
    /// </summary>
    public class JugadorPlayModeTests
    {
        private const string RutaPrefab = "Assets/Prefabs/Player.prefab";

        private readonly List<GameObject> _creados = new List<GameObject>();
        private readonly List<ESTADOS_JUGADOR> _historial = new List<ESTADOS_JUGADOR>();
        private PlayerController _jugador;
        private InputSimulado _input;

        private Rigidbody2D Cuerpo => _jugador.Motor.Cuerpo;

        private Vector2 Pies => Cuerpo.position;

        [UnityTearDown]
        public IEnumerator Limpiar()
        {
            foreach (var objeto in _creados)
            {
                if (objeto != null)
                {
                    Object.Destroy(objeto);
                }
            }

            _creados.Clear();
            _historial.Clear();
            yield return null;
        }

        // --- Criterio: mantener pulsado espacio ya no produce salto flotante ni infinito ---

        [UnityTest]
        public IEnumerator MantenerElSalto_DaUnSoloSaltoDeAlturaNormal()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            _input.PulsarSalto(mantener: true);
            var maximo = 0f;
            yield return Durante(2f, () => maximo = Mathf.Max(maximo, Pies.y));

            Assert.That(maximo, Is.InRange(2.0f, 2.9f), "Altura de un salto completo (~2,5 tiles).");
            Assert.AreEqual(1, Veces(ESTADOS_JUGADOR.JUMP), "Mantener el botón no debe encadenar saltos.");
            Assert.AreEqual(0, Veces(ESTADOS_JUGADOR.DOUBLE_JUMP), "Mantener no gasta el doble salto.");
            Assert.IsTrue(_jugador.Sensores.EnSuelo, "Debe volver al suelo.");
            Assert.AreEqual(ESTADOS_JUGADOR.IDLE, _jugador.EstadoActual);
        }

        [UnityTest]
        public IEnumerator SoltarElSaltoPronto_SaltaMenos()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            _input.PulsarSalto(mantener: true);
            yield return null;
            yield return null;
            _input.SoltarSalto();

            var maximo = 0f;
            yield return Durante(1.2f, () => maximo = Mathf.Max(maximo, Pies.y));

            Assert.That(maximo, Is.InRange(0.2f, 1.3f), "Un toque corto debe dar un salto bajo (altura variable).");
        }

        [UnityTest]
        public IEnumerator DobleSalto_SubeMasYSoloUnaVez()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            _input.PulsarSalto();
            yield return EsperarEstado(ESTADOS_JUGADOR.FALL, 1.5f);
            _input.PulsarSalto();
            yield return EsperarEstado(ESTADOS_JUGADOR.DOUBLE_JUMP, 0.5f);

            var maximo = 0f;
            yield return Durante(0.6f, () => maximo = Mathf.Max(maximo, Pies.y));
            _input.PulsarSalto();
            yield return Durante(1.5f, () => maximo = Mathf.Max(maximo, Pies.y));

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
            _input.EjeHorizontal = 1f;
            var limite = Time.time + 4f;
            while (Pies.x < 14f)
            {
                Assert.Less(Time.time, limite, "El jugador no avanzó.");
                Assert.IsTrue(_jugador.Sensores.EnSuelo, $"Perdió el suelo en x = {Pies.x:0.00}.");
                contactosMaximos = Mathf.Max(contactosMaximos, _jugador.Sensores.Suelo.Contactos);
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

            _input.EjeHorizontal = 1f;
            yield return new WaitForSeconds(1f);

            Assert.That(Cuerpo.linearVelocity.x, Is.EqualTo(_jugador.Parametros.velocidadMaxima).Within(0.2f));
            Assert.Greater(Pies.x, 4f);
            Assert.AreEqual(ESTADOS_JUGADOR.RUN, _jugador.EstadoActual);

            _input.EjeHorizontal = 0f;
            yield return new WaitForSeconds(0.5f);
            Assert.That(Cuerpo.linearVelocity.x, Is.EqualTo(0f).Within(0.05f), "Al soltar debe frenar.");
            Assert.AreEqual(ESTADOS_JUGADOR.IDLE, _jugador.EstadoActual);
        }

        [UnityTest]
        public IEnumerator Dash_RecorreSuDistanciaYRespetaElCooldown()
        {
            Suelo(-10f, 30f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            _input.PulsarDash();
            yield return new WaitForSeconds(0.3f);
            Assert.That(Pies.x, Is.InRange(2.3f, 4.5f), "Distancia del dash.");

            _input.PulsarDash(); // aún en cooldown
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(1, Veces(ESTADOS_JUGADOR.DASH), "El cooldown debe impedir el segundo dash.");

            yield return new WaitForSeconds(0.6f);
            _input.PulsarDash();
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(2, Veces(ESTADOS_JUGADOR.DASH), "Pasado el cooldown, el dash vuelve.");
        }

        // --- Coyote time ---

        [UnityTest]
        public IEnumerator CoyoteTime_PermiteSaltarJustoTrasElBorde()
        {
            Suelo(-10f, 0f);
            yield return CrearJugadorEnSuelo(new Vector2(-3f, 0f));

            _input.EjeHorizontal = 1f;
            yield return EsperarEstado(ESTADOS_JUGADOR.FALL, 2f);
            yield return new WaitForSeconds(0.04f);
            _input.PulsarSalto();
            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(1, Veces(ESTADOS_JUGADOR.JUMP), "Dentro del coyote time es un salto normal.");
            Assert.AreEqual(0, Veces(ESTADOS_JUGADOR.DOUBLE_JUMP), "El coyote no gasta el doble salto.");
        }

        [UnityTest]
        public IEnumerator SinCoyoteTime_SaltarEnElAireUsaElDobleSalto()
        {
            Suelo(-10f, 0f);
            yield return CrearJugadorEnSuelo(new Vector2(-3f, 0f));

            _input.EjeHorizontal = 1f;
            yield return EsperarEstado(ESTADOS_JUGADOR.FALL, 2f);
            yield return new WaitForSeconds(0.25f);
            _input.PulsarSalto();
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

            _input.EjeHorizontal = 1f;
            yield return EsperarEstado(ESTADOS_JUGADOR.WALL_SLIDE, 2f);
            yield return new WaitForSeconds(0.3f);
            Assert.GreaterOrEqual(Cuerpo.linearVelocity.y, -3.6f, "En la pared la caída se frena.");

            _input.PulsarSalto();
            yield return EsperarEstado(ESTADOS_JUGADOR.WALL_JUMP, 0.5f);
            yield return new WaitForFixedUpdate();

            Assert.Less(Cuerpo.linearVelocity.x, 0f, "Con la pared a la derecha, el wall jump sale hacia la izquierda.");
            Assert.Greater(Cuerpo.linearVelocity.y, 0f);
            Assert.AreEqual(-1, _jugador.Mirando);
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

            var vidaMaxima = _jugador.Vida.VidaMaxima;
            Assert.AreEqual(vidaMaxima - 1, _jugador.Vida.Vida);
            Assert.Less(Cuerpo.linearVelocity.x, 0f, "El retroceso se aleja del golpe.");

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(vidaMaxima - 1, _jugador.Vida.Vida, "Los i-frames impiden un segundo golpe inmediato.");
        }

        [UnityTest]
        public IEnumerator Muerte_YRevivir_VuelvenPorLaTabla()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            _jugador.Vida.Matar();
            yield return null;
            Assert.AreEqual(ESTADOS_JUGADOR.DEAD, _jugador.EstadoActual);

            _input.EjeHorizontal = 1f;
            yield return new WaitForSeconds(0.3f);
            Assert.That(Pies.x, Is.EqualTo(0f).Within(0.1f), "Muerto no responde al input.");

            _jugador.Revivir(new Vector2(-2f, 0.05f));
            yield return null;
            Assert.AreNotEqual(ESTADOS_JUGADOR.DEAD, _jugador.EstadoActual);
            Assert.AreEqual(_jugador.Vida.VidaMaxima, _jugador.Vida.Vida);
        }

        // --- Agacharse ---

        [UnityTest]
        public IEnumerator Agachado_BajoUnTecho_NoSeLevantaHastaSalir()
        {
            Suelo(-10f, 10f);
            Bloque("Techo", 1f, 5f, 1.2f, 2.2f, Capas.Suelo);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            _input.AgacharseMantenido = true;
            yield return EsperarEstado(ESTADOS_JUGADOR.CROUCH, 0.5f);

            _input.EjeHorizontal = 1f;
            yield return EsperarHasta(() => Pies.x >= 3f, 4f, "avanzar agachado bajo el techo");
            _input.EjeHorizontal = 0f;
            _input.AgacharseMantenido = false;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(ESTADOS_JUGADOR.CROUCH, _jugador.EstadoActual, "Sin espacio arriba no se levanta.");

            _input.EjeHorizontal = 1f;
            yield return EsperarHasta(() => _jugador.EstadoActual != ESTADOS_JUGADOR.CROUCH, 4f, "salir de debajo del techo");
            Assert.Greater(Pies.x, 5f, "Solo se levanta al salir de debajo del techo.");
        }

        // --- Ataque ---

        [UnityTest]
        public IEnumerator Ataque_GolpeaHaciaDondeMira_AAmbosLados()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);
            var derecha = Muneco(1.3f);
            var izquierda = Muneco(-1.3f);

            _input.PulsarAtaque();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(2, derecha.Vida, "Mirando a la derecha golpea al de la derecha.");
            Assert.AreEqual(3, izquierda.Vida, "…y no al de la izquierda.");

            _input.EjeHorizontal = -1f;
            yield return null;
            yield return null;
            _input.EjeHorizontal = 0f;
            yield return new WaitForSeconds(0.2f);

            _input.PulsarAtaque();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(2, izquierda.Vida, "Al girarse golpea al de la izquierda.");
            Assert.AreEqual(2, derecha.Vida);
        }

        [UnityTest]
        public IEnumerator AtaqueCargado_SaleAlSoltarConDobleDanio()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);
            _jugador.Habilidades.Desbloquear(HABILIDADES.ATAQUE_CARGADO);

            var muneco = Muneco(1.3f, vida: 10);
            var golpes = new List<Golpe>();
            muneco.OnGolpeRecibido += golpes.Add;

            _input.PulsarAtaque(mantener: true);
            yield return new WaitForSeconds(0.8f);
            _input.SoltarAtaque();
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
            yield return EsperarHasta(() => _jugador.Motor.BajoElAgua, 1f, "entrar en el agua");

            _input.PulsarDash();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(0, Veces(ESTADOS_JUGADOR.DASH), "Sin Dash Acuático no hay dash bajo el agua.");

            _jugador.Habilidades.Desbloquear(HABILIDADES.DASH_ACUATICO);
            _input.PulsarDash();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, Veces(ESTADOS_JUGADOR.DASH));
        }

        [UnityTest]
        public IEnumerator InvisibilidadParcial_SeActivaConLaTeclaDeHabilidad()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(Vector2.zero);

            _input.PulsarHabilidad();
            yield return null;
            Assert.IsFalse(_jugador.Habilidades.EsInvisible, "Bloqueada hasta obtener el fragmento del Miedo.");

            _jugador.Habilidades.Desbloquear(HABILIDADES.INVISIBILIDAD_PARCIAL);
            _input.PulsarHabilidad();
            yield return null;
            yield return null;
            Assert.IsTrue(_jugador.Habilidades.EsInvisible);
        }

        // --- Utilidades ---

        private IEnumerator CrearJugador(Vector2 pies)
        {
            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
#endif
            Assert.IsNotNull(prefab, $"Falta {RutaPrefab}: ejecuta Fragmentum → Jugador → Crear prefab del jugador.");

            var objeto = Object.Instantiate(prefab, new Vector3(pies.x, pies.y, 0f), Quaternion.identity);
            _creados.Add(objeto);

            _jugador = objeto.GetComponent<PlayerController>();
            _input = new InputSimulado();
            _jugador.FijarFuenteInput(_input);
            _jugador.OnStateChanged += (_, nuevo) => _historial.Add(nuevo);

            yield return null; // Start: la FSM arranca
        }

        private IEnumerator CrearJugadorEnSuelo(Vector2 pies)
        {
            yield return CrearJugador(pies + new Vector2(0f, 0.05f));
            yield return EsperarHasta(
                () => _jugador.Sensores.EnSuelo && _jugador.EstadoActual == ESTADOS_JUGADOR.IDLE, 1f,
                "que el jugador se apoye en el suelo");
            yield return new WaitForSeconds(0.1f);
            _historial.Clear();
        }

        private void Suelo(float xMin, float xMax) => Bloque("Suelo", xMin, xMax, -1f, 0f, Capas.Suelo);

        private GameObject Bloque(string nombre, float xMin, float xMax, float yMin, float yMax, string capa, bool trigger = false)
        {
            var objeto = new GameObject(nombre);
            if (!string.IsNullOrEmpty(capa))
            {
                objeto.layer = LayerMask.NameToLayer(capa);
            }

            objeto.transform.position = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
            var caja = objeto.AddComponent<BoxCollider2D>();
            caja.size = new Vector2(xMax - xMin, yMax - yMin);
            caja.isTrigger = trigger;
            _creados.Add(objeto);
            return objeto;
        }

        private Health Muneco(float x, int vida = 3)
        {
            var objeto = new GameObject("Muneco") { layer = LayerMask.NameToLayer(Capas.Enemy) };
            objeto.transform.position = new Vector3(x, 0f, 0f);
            var caja = objeto.AddComponent<BoxCollider2D>();
            caja.isTrigger = true;
            caja.size = new Vector2(1f, 2f);
            caja.offset = new Vector2(0f, 1f);
            var salud = objeto.AddComponent<Health>();
            salud.Inicializar(vida);
            _creados.Add(objeto);
            return salud;
        }

        private int Veces(ESTADOS_JUGADOR estado) => _historial.Count(e => e == estado);

        private IEnumerator EsperarEstado(ESTADOS_JUGADOR estado, float maximo) =>
            EsperarHasta(() => _jugador.EstadoActual == estado || _historial.Contains(estado), maximo, $"el estado {estado}");

        private static IEnumerator EsperarHasta(Func<bool> condicion, float maximo, string descripcion)
        {
            var limite = Time.time + maximo;
            while (!condicion())
            {
                if (Time.time > limite)
                {
                    Assert.Fail($"Tiempo agotado ({maximo:0.0} s) esperando {descripcion}.");
                }

                yield return null;
            }
        }

        private static IEnumerator Durante(float segundos, Action cadaFrame)
        {
            var fin = Time.time + segundos;
            while (Time.time < fin)
            {
                cadaFrame();
                yield return null;
            }
        }
    }
}
