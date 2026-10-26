using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Enemies.Deteccion;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Pruebas de aceptación de los enemigos (tarea A del encargo) con el prefab real, el
    /// jugador real y física real. Cada prueba fija un criterio de la sección 7 o un
    /// requisito de la FSM del enemigo.
    /// </summary>
    public class EnemigoPlayModeTests : PruebaPlayModeBase
    {
        private const string RutaPrefabEnemigo = "Assets/Prefabs/Enemigos/Enemigo.prefab";

        private readonly List<(ESTADOS_ENEMIGO estado, float tiempo)> _historialEnemigo = new List<(ESTADOS_ENEMIGO, float)>();

        // --- Criterio: el enemigo ataca al jugador venga por izquierda o por derecha ---

        [UnityTest]
        public IEnumerator AtacaAlJugador_PorLaIzquierda() => AtacaAlJugadorPorElLado(-1);

        [UnityTest]
        public IEnumerator AtacaAlJugador_PorLaDerecha() => AtacaAlJugadorPorElLado(1);

        private IEnumerator AtacaAlJugadorPorElLado(int lado)
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(new Vector2(lado * 1.1f, 0f));
            var enemigo = CrearEnemigo(Vector2.zero, DatosPrueba());

            var vidaInicial = Jugador.Vida.Vida;
            yield return EsperarHasta(() => Jugador.Vida.Vida < vidaInicial, 3f, "que el enemigo golpee al jugador");

            Assert.AreEqual(lado, enemigo.Mirando, "Debe atacar mirando hacia el jugador.");
            Assert.IsTrue(Paso(ESTADOS_ENEMIGO.ATTACK), "El golpe debe venir del estado ATTACK.");
        }

        // --- Criterio: patrulla cuando no detecta al jugador ---

        [UnityTest]
        public IEnumerator SinJugadorCerca_PatrullaEntreSusWaypoints()
        {
            Suelo(-10f, 10f);
            var enemigo = CrearEnemigo(Vector2.zero, DatosPrueba(d =>
            {
                d.velocidadPatrulla = 2.5f;
                d.esperaEnWaypoint = 0.2f;
            }), Punto("A", -3f), Punto("B", 3f));

            float minimo = 0f, maximo = 0f;
            yield return Durante(7f, () =>
            {
                minimo = Mathf.Min(minimo, enemigo.transform.position.x);
                maximo = Mathf.Max(maximo, enemigo.transform.position.x);
            });

            Assert.LessOrEqual(minimo, -2.6f, "Debe llegar al waypoint A.");
            Assert.GreaterOrEqual(maximo, 2.6f, "Debe llegar al waypoint B.");
            Assert.IsTrue(Paso(ESTADOS_ENEMIGO.PATROL));
            Assert.IsTrue(Paso(ESTADOS_ENEMIGO.IDLE), "Con espera, se detiene en cada punto.");
        }

        [UnityTest]
        public IEnumerator SinWaypoints_SeQuedaEnIdleSinErrores()
        {
            Suelo(-10f, 10f);
            var enemigo = CrearEnemigo(Vector2.zero, DatosPrueba());

            yield return new WaitForSeconds(1f);

            Assert.AreEqual(ESTADOS_ENEMIGO.IDLE, enemigo.EstadoActual);
            Assert.That(enemigo.transform.position.x, Is.EqualTo(0f).Within(0.05f));
        }

        // --- Histéresis ---

        [UnityTest]
        public IEnumerator Histeresis_NoSueltaAlJugadorHastaUnRadioYMedio()
        {
            Suelo(-10f, 12f);
            yield return CrearJugadorEnSuelo(new Vector2(3f, 0f));
            var enemigo = CrearEnemigo(Vector2.zero, DatosPrueba(d =>
            {
                d.detectionRadius = 4f;
                d.factorHisteresis = 1.5f;
                d.velocidadPersecucion = 0f; // quieto, para controlar la distancia
            }));

            yield return EsperarHasta(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.CHASE, 1f, "que lo detecte a 3 u");

            Jugador.Motor.Teletransportar(new Vector2(5f, 0.05f)); // fuera del radio, dentro de 1,5 × radio
            yield return ComprobarQueNunca(() => enemigo.EstadoActual != ESTADOS_ENEMIGO.CHASE, 0.6f,
                "Entre el radio y 1,5 × radio debe seguir persiguiendo.");

            Jugador.Motor.Teletransportar(new Vector2(7f, 0.05f)); // más allá de 1,5 × radio
            yield return EsperarHasta(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.IDLE, 1f, "que lo suelte");

            Jugador.Motor.Teletransportar(new Vector2(5f, 0.05f)); // otra vez en la franja intermedia
            yield return ComprobarQueNunca(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.CHASE, 0.6f,
                "Para volver a perseguir tiene que entrar en el radio normal.");

            Assert.AreEqual(1, Veces(ESTADOS_ENEMIGO.CHASE), "Sin parpadeo: una sola entrada en CHASE.");
        }

        // --- Cooldown ---

        [UnityTest]
        public IEnumerator Ataques_RespetanElCooldown()
        {
            Suelo(-10f, 10f);
            Bloque("Pared", 1.6f, 2.6f, 0f, 4f, Capas.Wall); // el retroceso no lo saca de alcance
            yield return CrearJugadorEnSuelo(new Vector2(1.1f, 0f));
            var datos = DatosPrueba(d =>
            {
                d.attackCooldown = 1f;
                d.duracionAtaque = 0.4f;
                d.inicioVentanaGolpe = 0.1f;
                d.finVentanaGolpe = 0.3f;
            });
            CrearEnemigo(Vector2.zero, datos);

            yield return new WaitForSeconds(3.5f);

            var ataques = _historialEnemigo.Where(e => e.estado == ESTADOS_ENEMIGO.ATTACK).Select(e => e.tiempo).ToList();
            Assert.GreaterOrEqual(ataques.Count, 2, "Debe atacar más de una vez.");
            for (var i = 1; i < ataques.Count; i++)
            {
                Assert.GreaterOrEqual(ataques[i] - ataques[i - 1], datos.duracionAtaque + datos.attackCooldown - 0.05f,
                    "Entre dos ataques debe pasar la duración del ataque más el cooldown.");
            }
        }

        // --- DAMAGED interrumpe ATTACK ---

        [UnityTest]
        public IEnumerator Golpe_InterrumpeElAtaqueYCierraElArma()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(new Vector2(1.1f, 0f));
            var enemigo = CrearEnemigo(Vector2.zero, DatosPrueba(d =>
            {
                d.duracionAtaque = 1.2f;
                d.inicioVentanaGolpe = 0.9f; // golpe tardío: se interrumpe antes
                d.finVentanaGolpe = 1.1f;
            }));

            yield return EsperarHasta(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.ATTACK, 2f, "que empiece a atacar");
            yield return new WaitForSeconds(0.2f);

            var vidaJugador = Jugador.Vida.Vida;
            enemigo.Vida.RecibirGolpe(new Golpe(1, PiesJugador + Vector2.up));
            Assert.AreEqual(ESTADOS_ENEMIGO.DAMAGED, enemigo.EstadoActual, "El golpe interrumpe el ataque al instante.");

            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(vidaJugador, Jugador.Vida.Vida, "El ataque interrumpido no debe llegar a golpear.");
            yield return EsperarHasta(() => enemigo.EstadoActual != ESTADOS_ENEMIGO.DAMAGED, 1f, "que termine el aturdimiento");
            Assert.IsTrue(Paso(ESTADOS_ENEMIGO.CHASE));
        }

        // --- DEAD real ---

        [UnityTest]
        public IEnumerator Muerte_ApagaColisionesYDestruyeElObjeto()
        {
            Suelo(-10f, 10f);
            var enemigo = CrearEnemigo(Vector2.zero, DatosPrueba(d => d.deathDelay = 0.5f));
            yield return new WaitForSeconds(0.2f);

            enemigo.Vida.Matar();
            Assert.AreEqual(ESTADOS_ENEMIGO.DEAD, enemigo.EstadoActual);
            Assert.IsTrue(enemigo.GetComponentsInChildren<Collider2D>().All(c => !c.enabled), "Un cadáver no colisiona ni hace daño.");
            Assert.AreEqual(RigidbodyType2D.Kinematic, enemigo.GetComponent<Rigidbody2D>().bodyType);

            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(enemigo == null, "Debe destruirse tras deathDelay.");
        }

        // --- Criterio: variantes con distinto EnemyData se comportan distinto ---

        [UnityTest]
        public IEnumerator VariantesEmocionales_SeComportanDeFormaDistinta()
        {
            Suelo(-10f, 30f);
            yield return CrearJugadorEnSuelo(new Vector2(4.5f, 0f));

            var ira = CargarAsset<EnemyData>("Assets/Data/Enemigos/SoldadoLava.asset");
            var tristeza = CargarAsset<EnemyData>("Assets/Data/Enemigos/Lamento.asset");

            var avanceIra = 0f;
            yield return MedirAvance(ira, v => avanceIra = v);
            var avanceTristeza = 0f;
            yield return MedirAvance(tristeza, v => avanceTristeza = v);

            Assert.Greater(avanceIra, avanceTristeza * 2f,
                $"El enemigo de la Ira ({avanceIra:0.00} u) debe perseguir mucho más rápido que el de la Tristeza ({avanceTristeza:0.00} u).");
            // El Lamento descansa entre golpes lo que el Eco de la Ira (el ajuste del Nivel 1): lo que
            // le distingue es que avisa más antes de golpear.
            Assert.Less(ira.inicioVentanaGolpe, tristeza.inicioVentanaGolpe, "La Ira golpea con menos aviso.");
        }

        private IEnumerator MedirAvance(EnemyData datos, Action<float> resultado)
        {
            var enemigo = CrearEnemigo(Vector2.zero, datos);
            yield return EsperarHasta(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.CHASE, 1f, $"que {datos.nombreCanonico} persiga");
            var inicio = enemigo.transform.position.x;
            yield return new WaitForSeconds(0.5f);
            resultado(enemigo.transform.position.x - inicio);
            Object.Destroy(enemigo.gameObject);
            yield return null;
        }

        // --- Estrategias de detección ---

        [UnityTest]
        public IEnumerator ConVision_NoVeATravesDeUnMuro()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(new Vector2(3f, 0f));
            var muro = Bloque("Muro", 1.2f, 1.8f, 0f, 3f, Capas.Wall);
            var enemigo = CrearEnemigo(Vector2.zero, DatosPrueba(d => d.deteccion = Estrategia<DeteccionConVision>()));

            yield return ComprobarQueNunca(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.CHASE, 1f, "Con el muro en medio no debe verlo.");

            Object.Destroy(muro);
            yield return EsperarHasta(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.CHASE, 1f, "que lo vea sin el muro");
        }

        [UnityTest]
        public IEnumerator InvisibilidadParcial_EngañaALaVista()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(new Vector2(3f, 0f));
            Jugador.Habilidades.Desbloquear(HABILIDADES.INVISIBILIDAD_PARCIAL);
            Jugador.Habilidades.ActivarInvisibilidad();
            var enemigo = CrearEnemigo(Vector2.zero, DatosPrueba(d => d.velocidadPersecucion = 0f));

            yield return ComprobarQueNunca(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.CHASE, 2f, "Invisible no debe ser detectado.");
            yield return EsperarHasta(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.CHASE, 2f, "que lo detecte al acabar la invisibilidad");
        }

        [UnityTest]
        public IEnumerator EnOscuridad_NoSeAcercaSiElJugadorEstaEnLaLuz()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(new Vector2(3f, 0f));
            var luz = new GameObject("Luz");
            luz.transform.position = new Vector3(3f, 1f, 0f);
            luz.AddComponent<ZonaIluminada>();
            Creados.Add(luz);
            var enemigo = CrearEnemigo(Vector2.zero, DatosPrueba(d => d.deteccion = Estrategia<DeteccionEnOscuridad>()));

            yield return ComprobarQueNunca(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.CHASE, 1f, "Con el jugador iluminado no debe ir a por él.");

            luz.SetActive(false);
            yield return EsperarHasta(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.CHASE, 1f, "que vaya a por él en la oscuridad");
        }

        [UnityTest]
        public IEnumerator PorRuido_OyeAlJugadorCuandoCorre()
        {
            Suelo(-10f, 20f);
            yield return CrearJugadorEnSuelo(new Vector2(5f, 0f));
            Jugador.GetComponent<PlayerModoMovimiento>().EmitirRuido = true;
            var enemigo = CrearEnemigo(Vector2.zero, DatosPrueba(d =>
            {
                d.deteccion = Estrategia<DeteccionPorRuido>();
                d.velocidadPersecucion = 0f;
            }));

            yield return ComprobarQueNunca(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.CHASE, 1f, "Quieto, el jugador no hace ruido.");

            Input.EjeHorizontal = 1f; // correr hace ruido
            yield return EsperarHasta(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.CHASE, 1f, "que lo oiga correr");
        }

        // --- Bordes y fuego amigo ---

        [UnityTest]
        public IEnumerator Persiguiendo_NoSeTiraPorElBorde()
        {
            Bloque("Plataforma", -2f, 2f, -1f, 0f, Capas.Suelo);
            Suelo(-10f, 20f, yArriba: -4f);
            yield return CrearJugadorEnSuelo(new Vector2(5f, -4f));
            var enemigo = CrearEnemigo(Vector2.zero, DatosPrueba(d => d.detectionRadius = 8f));

            yield return EsperarHasta(() => enemigo.EstadoActual == ESTADOS_ENEMIGO.CHASE, 1f, "que detecte al jugador de abajo");
            yield return new WaitForSeconds(2f);

            Assert.That(enemigo.transform.position.x, Is.InRange(0.8f, 2f), "Debe avanzar hasta el borde y pararse.");
            Assert.Greater(enemigo.transform.position.y, -0.5f, "No debe caerse de la plataforma.");
        }

        [UnityTest]
        public IEnumerator ArmaEnemiga_NoHiereAOtrosEnemigos()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(new Vector2(1.1f, 0f));
            var atacante = CrearEnemigo(Vector2.zero, DatosPrueba());
            var vecino = CrearEnemigo(new Vector2(0.9f, 0f), DatosPrueba(d => d.danioContacto = 0));

            var vidaJugador = Jugador.Vida.Vida;
            yield return new WaitForSeconds(2f);

            Assert.Less(Jugador.Vida.Vida, vidaJugador, "El jugador sí recibe los golpes.");
            Assert.AreEqual(vecino.Vida.VidaMaxima, vecino.Vida.Vida, "El arma de un enemigo no daña a otro.");
            Assert.AreEqual(atacante.Vida.VidaMaxima, atacante.Vida.Vida);
        }

        // --- Utilidades ---

        private EnemyController CrearEnemigo(Vector2 pies, EnemyData datos, params Transform[] ruta)
        {
            var objeto = Object.Instantiate(CargarPrefab(RutaPrefabEnemigo), new Vector3(pies.x, pies.y + 0.05f, 0f), Quaternion.identity);
            Creados.Add(objeto);

            var enemigo = objeto.GetComponent<EnemyController>();
            enemigo.FijarDatos(datos);
            enemigo.FijarWaypoints(ruta);
            enemigo.OnStateChanged += (_, nuevo) => _historialEnemigo.Add((nuevo, Time.time));
            return enemigo;
        }

        /// <summary>Datos neutros para las pruebas: sin daño por contacto y detección por distancia.</summary>
        private EnemyData DatosPrueba(Action<EnemyData> ajustar = null)
        {
            var datos = ScriptableObject.CreateInstance<EnemyData>();
            datos.danioContacto = 0;
            datos.esperaEnWaypoint = 0.5f;
            ajustar?.Invoke(datos);
            Creados.Add(datos);
            return datos;
        }

        private T Estrategia<T>() where T : DeteccionData
        {
            var estrategia = ScriptableObject.CreateInstance<T>();
            Creados.Add(estrategia);
            return estrategia;
        }

        private bool Paso(ESTADOS_ENEMIGO estado) => _historialEnemigo.Any(e => e.estado == estado);

        private int Veces(ESTADOS_ENEMIGO estado) => _historialEnemigo.Count(e => e.estado == estado);

        [TearDown]
        public void LimpiarHistorial() => _historialEnemigo.Clear();
    }
}
