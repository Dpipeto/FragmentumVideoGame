using System.Collections;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Mecánicas del mundo de la Ira (Fase 9, Anexo A) con sus prefabs reales, el jugador real y
    /// física real: plataforma inestable, plataforma de combustión, lava, torreta y proyectil.
    /// </summary>
    public class MecanicasPlayModeTests : PruebaPlayModeBase
    {
        private const string CarpetaMecanicas = "Assets/Prefabs/Mecanicas";
        private const string RutaPrefabEnemigo = "Assets/Prefabs/Enemigos/Enemigo.prefab";

        // --- Plataforma inestable ---

        [UnityTest]
        public IEnumerator PlataformaInestable_CaeTrasAguantarAlJugador_YVuelveAlRato()
        {
            Suelo(-10f, -1.5f);
            Suelo(-10f, 10f, yArriba: -4f);
            var plataforma = Instanciar<PlataformaInestable>(nameof(PlataformaInestable), Vector2.zero);
            var cayoEn = -1f;
            plataforma.OnCaida += cayo =>
            {
                if (cayo)
                {
                    cayoEn = Time.time;
                }
            };
            yield return CrearJugadorEnSuelo(new Vector2(-5f, 0f));

            yield return ComprobarQueNunca(() => plataforma.Caida, 0.8f, "Sin nadie encima no se cae.");

            Jugador.Motor.Teletransportar(new Vector2(0f, 0.05f));
            var pisadaEn = Time.time;
            yield return EsperarHasta(() => plataforma.Caida, 1.5f, "que la plataforma caiga");
            Assert.GreaterOrEqual(Time.time - pisadaEn, 0.5f, "Aguanta un momento (0,6 s) antes de caer.");
            Assert.IsFalse(plataforma.GetComponent<Collider2D>().enabled, "Caída, ya no sostiene.");

            yield return EsperarHasta(() => PiesJugador.y < -3f, 2f, "que el jugador caiga con ella");

            yield return EsperarHasta(() => !plataforma.Caida, 4f, "que la plataforma vuelva");
            Assert.GreaterOrEqual(Time.time - cayoEn, 2.9f, "Vuelve a los 3 s.");
            Assert.IsTrue(plataforma.GetComponent<Collider2D>().enabled);
        }

        // --- Plataforma de combustión ---

        [UnityTest]
        public IEnumerator PlataformaCaliente_SoloQuemaTrasDosSegundosEncima_YSeEnfria()
        {
            Suelo(-10f, 10f);
            var plataforma = Instanciar<PlataformaCaliente>(nameof(PlataformaCaliente), new Vector2(0f, 0.3f));
            yield return CrearJugadorEnSuelo(new Vector2(-5f, 0f));
            var vida = Jugador.Vida.Vida;

            Jugador.Motor.Teletransportar(new Vector2(0f, 0.35f));
            yield return ComprobarQueNunca(() => Jugador.Vida.Vida < vida, 1.5f, "Antes de 2 s encima no quema.");
            Assert.Greater(plataforma.Calor, 0.5f, "Se va calentando mientras el jugador sigue encima.");

            yield return EsperarHasta(() => Jugador.Vida.Vida < vida, 1.2f, "que queme pasados 2 s");

            Jugador.Motor.Teletransportar(new Vector2(-5f, 0.05f));
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(0f, plataforma.Calor, 0.01f, "Al bajarse, se enfría.");
        }

        // --- Lava ---

        [UnityTest]
        public IEnumerator Lava_MataAlJugador_AunqueSeaInvulnerable()
        {
            Suelo(-10f, 10f, yArriba: -1f);
            Instanciar<Lava>(nameof(Lava), Vector2.zero);
            yield return CrearJugadorEnSuelo(new Vector2(-5f, -1f));

            Jugador.Vida.RecibirGolpe(new Golpe(1, PiesJugador + Vector2.left));
            Assert.IsTrue(Jugador.Vida.EsInvulnerable, "Tras un golpe tiene i-frames.");

            Jugador.Motor.Teletransportar(new Vector2(0f, -0.95f));
            yield return EsperarHasta(() => !Jugador.Vida.EstaVivo, 0.5f, "que la lava lo mate pese a los i-frames");
            yield return EsperarEstado(ESTADOS_JUGADOR.DEAD, 0.5f);
        }

        [UnityTest]
        public IEnumerator Lava_TambienMataALosEnemigos()
        {
            Suelo(-10f, 10f, yArriba: -1f);
            Instanciar<Lava>(nameof(Lava), Vector2.zero);

            var objeto = Object.Instantiate(CargarPrefab(RutaPrefabEnemigo), new Vector3(0f, 0.5f, 0f), Quaternion.identity);
            Creados.Add(objeto);
            var enemigo = objeto.GetComponent<EnemyController>();
            enemigo.FijarDatos(CargarAsset<EnemyData>("Assets/Data/Enemigos/SoldadoLava.asset"));

            yield return EsperarHasta(() => enemigo == null || !enemigo.Vida.EstaVivo, 2f, "que el enemigo muera en la lava");
        }

        [UnityTest]
        public IEnumerator Lava_NoQuemaAlGolemDeCeniza()
        {
            Suelo(-10f, 10f, yArriba: -1f);
            var lava = Instanciar<Lava>(nameof(Lava), Vector2.zero).GetComponent<Collider2D>();

            var objeto = Object.Instantiate(CargarPrefab("Assets/Prefabs/Enemigos/Enemigo_GolemCeniza.prefab"), new Vector3(0f, 0.5f, 0f), Quaternion.identity);
            Creados.Add(objeto);
            var golem = objeto.GetComponent<EnemyController>();
            Assert.IsTrue(golem.InmuneALava, "Es de ceniza y magma: sus datos lo hacen inmune.");

            yield return ComprobarQueNunca(() => golem == null || !golem.Vida.EstaVivo, 1.5f, "La lava no mata al Golem de Ceniza.");
            Assert.IsTrue(golem.GetComponent<Collider2D>().IsTouching(lava), "Y la estaba tocando todo el tiempo.");
            Assert.AreEqual(golem.Vida.VidaMaxima, golem.Vida.Vida, "Ni le quita vida.");
        }

        // --- Torreta de fuego ---

        [UnityTest]
        public IEnumerator Torreta_SoloDisparaConElJugadorAlAlcance_YLaBolaLoHiere()
        {
            Suelo(-30f, 30f);
            var torreta = Instanciar<TorretaFuego>(nameof(TorretaFuego), Vector2.zero);
            Proyectil disparado = null;
            torreta.OnDisparo += p =>
            {
                disparado = p;
                Creados.Add(p.gameObject);
            };
            yield return CrearJugadorEnSuelo(new Vector2(15f, 0f));

            yield return ComprobarQueNunca(() => torreta.Disparos > 0, 1.5f, "Fuera de su alcance no dispara.");

            var vida = Jugador.Vida.Vida;
            Jugador.Motor.Teletransportar(new Vector2(6f, 0.05f));
            var entroEn = Time.time;
            yield return EsperarHasta(() => torreta.Disparos > 0, 2f, "que dispare al tener al jugador a su alcance");
            Assert.GreaterOrEqual(Time.time - entroEn, 0.4f, "Avisa con un destello antes del primer disparo.");
            Assert.Greater(disparado.Velocidad.x, 0f, "Dispara hacia el lado del jugador.");
            Assert.AreEqual(0f, disparado.Velocidad.y, "En horizontal: se esquiva saltando.");

            yield return EsperarHasta(() => Jugador.Vida.Vida < vida, 2f, "que la bola de fuego alcance al jugador");
            yield return EsperarHasta(() => disparado == null, 0.2f, "que la bola desaparezca al golpear");
        }

        // --- Proyectil ---

        [UnityTest]
        public IEnumerator Proyectil_LoFrenaUnaPared()
        {
            Bloque("Pared", 3f, 4f, 0f, 4f, Capas.Wall);
            var bola = Disparar(alcance: 20f);

            var maximo = 0f;
            yield return EsperarHasta(() =>
            {
                if (bola != null)
                {
                    maximo = Mathf.Max(maximo, bola.transform.position.x);
                }

                return bola == null;
            }, 1f, "que la pared frene la bola");
            Assert.Less(maximo, 3.2f, "No atraviesa la pared.");
        }

        [UnityTest]
        public IEnumerator Proyectil_DesapareceAlAcabarSuAlcance()
        {
            var bola = Disparar(alcance: 2f);

            var maximo = 0f;
            yield return EsperarHasta(() =>
            {
                if (bola != null)
                {
                    maximo = Mathf.Max(maximo, bola.transform.position.x);
                }

                return bola == null;
            }, 1f, "que la bola desaparezca");
            Assert.That(maximo, Is.InRange(1.5f, 2.5f), "Recorre su alcance y desaparece.");
        }

        // --- Utilidades ---

        private T Instanciar<T>(string nombre, Vector2 posicion) where T : Component
        {
            var objeto = Object.Instantiate(CargarPrefab($"{CarpetaMecanicas}/{nombre}.prefab"), new Vector3(posicion.x, posicion.y, 0f), Quaternion.identity);
            Creados.Add(objeto);
            var componente = objeto.GetComponent<T>();
            Assert.IsNotNull(componente, $"El prefab {nombre} no tiene {typeof(T).Name}.");
            return componente;
        }

        private Proyectil Disparar(float alcance)
        {
            var bola = Proyectil.Disparar("Bola", new Vector2(0f, 1f), new Vector2(8f, 0f), 0.3f, 1, Capas.MascaraJugador, alcance, Color.red);
            Creados.Add(bola.gameObject);
            return bola;
        }
    }
}
