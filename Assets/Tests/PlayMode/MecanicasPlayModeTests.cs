using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// Mecánicas del mundo de la Ira con su arte, sus prefabs reales, el jugador real y física
    /// real: plataforma inestable, plataforma de combustión, lava, torreta y proyectil.
    ///
    /// - La plataforma inestable se agrieta al pisarla y se rompe aunque el jugador se baje; rota,
    ///   ya no sostiene; desaparece y vuelve.
    /// - La de combustión cicla sola y solo quema encendida; nunca deja de sostener.
    /// - La lava mata (también con i-frames) salvo al Golem, y monta sus franjas alternadas.
    /// - La torreta cicla sola: avisa, y solo hace daño (llama y bola) al disparar, hacia donde mira.
    /// </summary>
    public class MecanicasPlayModeTests : PruebaPlayModeBase
    {
        private const string CarpetaMecanicas = "Assets/Prefabs/Mecanicas";
        private const string RutaPrefabEnemigo = "Assets/Prefabs/Enemigos/Enemigo.prefab";
        private const string RutaGolem = "Assets/Prefabs/Enemigos/GolemCeniza.prefab";

        // Margen de los tiempos de los ciclos.
        private const float Margen = 0.1f;

        // --- Plataforma inestable ---

        [UnityTest]
        public IEnumerator PlataformaInestable_SeAgrietaAlPisarla_SeRompeAunqueTeBajes_YVuelve()
        {
            Suelo(-12f, -2.5f);
            Suelo(-12f, 12f, yArriba: -4f);
            var plataforma = Instanciar<PlataformaInestable>(nameof(PlataformaInestable), Vector2.zero);
            yield return CrearJugadorEnSuelo(new Vector2(-6f, 0f));

            yield return ComprobarQueNunca(() => plataforma.Estado != ESTADOS_PLATAFORMA.INTACT, 0.8f, "Sin nadie encima, sigue intacta.");

            Jugador.Motor.Teletransportar(new Vector2(0f, 0.05f));
            yield return EsperarHasta(() => plataforma.Estado == ESTADOS_PLATAFORMA.CRACKED, 0.3f, "que se agriete al pisarla");
            var pisadaEn = Time.time;
            Assert.IsTrue(plataforma.Sostiene, "Agrietada, aún se puede pisar: es el aviso.");

            // El jugador se baja enseguida: las grietas no se cierran.
            Jugador.Motor.Teletransportar(new Vector2(-6f, 0.05f));
            yield return EsperarHasta(() => plataforma.Caida, 1f, "que se rompa aunque ya no haya nadie encima");
            Assert.AreEqual(plataforma.retardoRotura, Time.time - pisadaEn, Margen, "Se rompe al acabar el aviso.");
            Assert.IsFalse(plataforma.Sostiene, "Rota, el collider se apaga en el acto.");
            Assert.IsTrue(plataforma.Visible, "Aún se ven los trozos y el polvo.");

            var rotaEn = Time.time;
            yield return EsperarHasta(() => !plataforma.Visible, 1f, "que desaparezca");
            Assert.AreEqual(plataforma.duracionCaida, Time.time - rotaEn, Margen);

            var desaparecioEn = Time.time;
            yield return EsperarHasta(() => plataforma.Estado == ESTADOS_PLATAFORMA.INTACT, plataforma.tiempoReaparicion + 0.5f, "que vuelva");
            Assert.AreEqual(plataforma.tiempoReaparicion, Time.time - desaparecioEn, Margen);
            Assert.IsTrue(plataforma.Sostiene);
            Assert.IsTrue(plataforma.Visible);
        }

        [UnityTest]
        public IEnumerator PlataformaInestable_ConElJugadorEncima_LoDejaCaer()
        {
            Suelo(-12f, -2.5f);
            Suelo(-12f, 12f, yArriba: -4f);
            var plataforma = Instanciar<PlataformaInestable>(nameof(PlataformaInestable), Vector2.zero);
            yield return CrearJugadorEnSuelo(new Vector2(-6f, 0f));

            Jugador.Motor.Teletransportar(new Vector2(0f, 0.05f));
            yield return EsperarHasta(() => plataforma.Caida, 1.5f, "que la plataforma se rompa");
            yield return EsperarHasta(() => PiesJugador.y < -3f, 2f, "que el jugador caiga con ella");
        }

        [UnityTest]
        public IEnumerator PlataformaInestable_SeAtraviesaSaltandoDesdeAbajo_YSoloSeAgrietaAlCaerEncima()
        {
            Suelo(-12f, 12f);
            var plataforma = Instanciar<PlataformaInestable>(nameof(PlataformaInestable), new Vector2(0f, 2f));
            yield return CrearJugadorEnSuelo(new Vector2(0f, 0f));
            Assert.IsTrue(PlataformaDeUnSentido.Es(plataforma.GetComponent<Collider2D>()), "Como todas las del mundo de la Ira.");

            Input.PulsarSalto();
            var dentro = 0;
            var laPisoAlSubir = false;
            var fin = Time.time + 1.5f;
            while (Time.time < fin && !(Jugador.Sensores.EnSuelo && PiesJugador.y > 1.9f))
            {
                if (PiesJugador.y > 0.3f && PiesJugador.y < 1.95f)
                {
                    dentro++;
                    laPisoAlSubir |= Jugador.Sensores.EnSuelo || plataforma.Estado != ESTADOS_PLATAFORMA.INTACT;
                }

                yield return null;
            }

            Input.SoltarSalto();
            Assert.Greater(dentro, 0, "La atraviesa: no se da con la cabeza.");
            Assert.IsFalse(laPisoAlSubir, "Mientras la atraviesa subiendo, ni la pisa ni la agrieta.");
            Assert.IsTrue(Jugador.Sensores.EnSuelo, "Cae encima y la pisa, como siempre…");
            Assert.AreEqual(2f, PiesJugador.y, 0.1f);
            yield return EsperarHasta(() => plataforma.Estado == ESTADOS_PLATAFORMA.CRACKED, 0.3f, "…y entonces se agrieta");
        }

        // --- Plataforma de combustión ---

        [UnityTest]
        public IEnumerator PlataformaCombustion_CiclaSola_YSoloLaLlamaEncendidaHaceDanio()
        {
            Suelo(-12f, 12f);
            var plataforma = Instanciar<PlataformaCombustion>(nameof(PlataformaCombustion), new Vector2(0f, 0.3f));
            var estados = new List<(ESTADOS_COMBUSTION estado, float t)>();
            plataforma.OnEstadoCambiado += e => estados.Add((e, Time.time));
            var inicio = Time.time;

            // Sin jugador: el ciclo no depende de nadie.
            yield return Durante(plataforma.tiempoApagada + plataforma.tiempoCalienta + plataforma.tiempoEncendida + 0.3f, () =>
            {
                Assert.IsTrue(plataforma.GetComponent<Collider2D>().enabled, "Nunca deja de sostener.");
                Assert.AreEqual(plataforma.Estado == ESTADOS_COMBUSTION.BURNING, plataforma.LlamaActiva, "La llama solo hace daño encendida.");
            });

            CollectionAssert.AreEqual(new[] { ESTADOS_COMBUSTION.HEATING, ESTADOS_COMBUSTION.BURNING, ESTADOS_COMBUSTION.OFF }, estados.Select(e => e.estado));
            Assert.AreEqual(plataforma.tiempoApagada, estados[0].t - inicio, Margen, "Apagada.");
            Assert.AreEqual(plataforma.tiempoCalienta, estados[1].t - estados[0].t, Margen, "Calienta: el aviso.");
            Assert.AreEqual(plataforma.tiempoEncendida, estados[2].t - estados[1].t, Margen, "Encendida.");
        }

        [UnityTest]
        public IEnumerator PlataformaCombustion_QuemaAlQueEstaEncima_SoloCuandoSeEnciende()
        {
            Suelo(-12f, 12f);
            var plataforma = Instanciar<PlataformaCombustion>(nameof(PlataformaCombustion), new Vector2(0f, 0.3f));
            yield return CrearJugadorEnSuelo(new Vector2(-6f, 0f));
            var vida = Jugador.Vida.Vida;

            Jugador.Motor.Teletransportar(new Vector2(0f, 0.35f));
            yield return EsperarHasta(() =>
            {
                Assert.AreEqual(vida, Jugador.Vida.Vida, "Apagada o calentando, se puede estar encima.");
                return plataforma.Estado == ESTADOS_COMBUSTION.BURNING;
            }, plataforma.tiempoApagada + plataforma.tiempoCalienta + 0.3f, "que se encienda");
            yield return EsperarHasta(() => Jugador.Vida.Vida < vida, 0.3f, "que la llama queme al encenderse");
            Assert.That(PiesJugador.y, Is.GreaterThan(0.2f), "Y el suelo sigue ahí: lo que mata es el momento.");
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

            var objeto = Object.Instantiate(CargarPrefab(RutaGolem), new Vector3(0f, 0.5f, 0f), Quaternion.identity);
            Creados.Add(objeto);
            var golem = objeto.GetComponent<EnemyController>();
            Assert.IsTrue(golem.InmuneALava, "Es de ceniza y magma: sus datos lo hacen inmune.");

            yield return ComprobarQueNunca(() => golem == null || !golem.Vida.EstaVivo, 1.5f, "La lava no mata al Golem de Ceniza.");
            Assert.IsTrue(golem.GetComponent<Collider2D>().IsTouching(lava), "Y la estaba tocando todo el tiempo.");
            Assert.AreEqual(golem.Vida.VidaMaxima, golem.Vida.Vida, "Ni le quita vida.");
        }

        [UnityTest]
        public IEnumerator Lava_MontaSusFranjasAlternadas_YElTriggerLasCubreDesdeLaCostra()
        {
            var lava = Instanciar<Lava>(nameof(Lava), new Vector2(2f, -1f));
            yield return null;
            Assert.AreEqual(1, lava.Dibujos.Count, "El prefab trae una franja.");

            lava.FijarTramos(4);
            CollectionAssert.AreEqual(new[] { "Lava", "Lava_espejo", "Lava", "Lava_espejo" }, lava.Dibujos.Select(d => d.sprite.name),
                "Alterna la franja y su espejo: el borde derecho de una es el de la otra.");

            var ancho = lava.AnchoFranja;
            Assert.AreEqual(151f / 48f, ancho, 0.001f);
            for (var i = 1; i < lava.Dibujos.Count; i++)
            {
                Assert.AreEqual(lava.Dibujos[i - 1].bounds.max.x, lava.Dibujos[i].bounds.min.x, 0.001f, "Sin huecos ni solapes entre franjas.");
            }

            Assert.AreEqual(2f, (lava.Dibujos[0].bounds.min.x + lava.Dibujos[3].bounds.max.x) / 2f, 0.001f, "Centradas en el objeto.");

            yield return new WaitForFixedUpdate();
            var zona = lava.GetComponent<BoxCollider2D>().bounds;
            Assert.AreEqual(4f * ancho, zona.size.x, 0.01f, "El trigger cubre las cuatro.");
            Assert.AreEqual(-1f, zona.max.y, 0.01f, "Y empieza en la costra, a la altura del objeto.");
            Assert.AreEqual(-1f, lava.Dibujos[0].bounds.max.y, 0.01f, "La costra (fila 0) está arriba del todo.");
        }

        // --- Torreta de fuego ---

        [UnityTest]
        public IEnumerator Torreta_CiclaSola_AvisaYSoloQuemaAlDisparar()
        {
            Suelo(-30f, 30f);
            var torreta = Instanciar<TorretaFuego>(nameof(TorretaFuego), Vector2.zero);
            var disparos = new List<Proyectil>();
            torreta.OnDisparo += p =>
            {
                disparos.Add(p);
                Creados.Add(p.gameObject);
            };
            var estados = new List<(ESTADOS_TORRETA estado, float t)>();
            torreta.OnEstadoCambiado += e => estados.Add((e, Time.time));
            var inicio = Time.time;

            // Sin jugador cerca: el ciclo no depende de él.
            yield return Durante(torreta.tiempoReposo + torreta.tiempoCarga + torreta.tiempoDisparo + 0.2f, () =>
                Assert.AreEqual(torreta.Estado == ESTADOS_TORRETA.FIRE, torreta.LlamaActiva, "La llama solo está encendida al disparar."));

            CollectionAssert.AreEqual(new[] { ESTADOS_TORRETA.CHARGE, ESTADOS_TORRETA.FIRE, ESTADOS_TORRETA.IDLE }, estados.Select(e => e.estado));
            Assert.AreEqual(torreta.tiempoReposo, estados[0].t - inicio, Margen, "Reposo.");
            Assert.AreEqual(torreta.tiempoCarga, estados[1].t - estados[0].t, Margen, "Carga: el aviso.");
            Assert.AreEqual(torreta.tiempoDisparo, estados[2].t - estados[1].t, Margen, "Disparo.");

            Assert.AreEqual(1, disparos.Count, "Una bola por disparo.");
            Assert.AreEqual(GeneradorMecanicasNombres.BolaDeFuego, disparos[0].name, "La bola es la del prefab.");
            Assert.Greater(disparos[0].Velocidad.x, 0f, "Dispara hacia donde mira el dibujo: a la derecha.");
            Assert.AreEqual(0f, disparos[0].Velocidad.y, "En horizontal: se esquiva saltando.");
        }

        [UnityTest]
        public IEnumerator Torreta_LaBolaHiereAlJugador_YDesapareceAlGolpear()
        {
            Suelo(-30f, 30f);
            var torreta = Instanciar<TorretaFuego>(nameof(TorretaFuego), Vector2.zero);
            Proyectil disparado = null;
            torreta.OnDisparo += p =>
            {
                disparado = p;
                Creados.Add(p.gameObject);
            };
            yield return CrearJugadorEnSuelo(new Vector2(6f, 0f));
            var vida = Jugador.Vida.Vida;

            yield return EsperarHasta(() => disparado != null, torreta.tiempoReposo + torreta.tiempoCarga + 0.3f, "el primer disparo");
            yield return EsperarHasta(() => Jugador.Vida.Vida < vida, 2f, "que la bola de fuego alcance al jugador");
            yield return EsperarHasta(() => disparado == null, 0.2f, "que la bola desaparezca al golpear");

            // Deja sus chispas un momento, sin hacer daño, y se van solas.
            var restos = GameObject.Find("BolaDeFuego_Restos");
            Assert.IsNotNull(restos, "Al apagarse deja sus chispas.");
            Assert.AreEqual("Bola_3_disipa", restos.GetComponent<SpriteRenderer>().sprite.name);
            Assert.IsNull(restos.GetComponent<Collider2D>(), "Las chispas no hacen daño.");
            yield return EsperarHasta(() => restos == null, 0.5f, "que las chispas se apaguen");
        }

        [UnityTest]
        public IEnumerator Torreta_LaLlamaQuemaDelanteDelBloque()
        {
            Suelo(-30f, 30f);
            var torreta = Instanciar<TorretaFuego>(nameof(TorretaFuego), Vector2.zero);
            torreta.OnDisparo += p => Creados.Add(p.gameObject);
            yield return CrearJugadorEnSuelo(new Vector2(1.7f, 0f));
            var vida = Jugador.Vida.Vida;

            // El ciclo empezó al crear la torreta, no al aparecer Kael: se espera a su disparo.
            yield return EsperarHasta(() => torreta.Estado == ESTADOS_TORRETA.FIRE, torreta.tiempoReposo + torreta.tiempoCarga + 0.3f, "el disparo");
            Assert.AreEqual(vida, Jugador.Vida.Vida, "En reposo y en carga no quema.");
            yield return EsperarHasta(() => Jugador.Vida.Vida < vida, 0.4f, "que la llama queme al salir");
        }

        [UnityTest]
        public IEnumerator Torreta_Volteada_DisparaALaIzquierda()
        {
            Suelo(-30f, 30f);
            var torreta = Instanciar<TorretaFuego>(nameof(TorretaFuego), Vector2.zero);
            torreta.transform.localScale = new Vector3(-1f, 1f, 1f);
            Proyectil disparado = null;
            torreta.OnDisparo += p =>
            {
                disparado = p;
                Creados.Add(p.gameObject);
            };

            yield return EsperarHasta(() => disparado != null, torreta.tiempoReposo + torreta.tiempoCarga + 0.3f, "el primer disparo");
            Assert.AreEqual(-1, torreta.Mirando);
            Assert.Less(disparado.Velocidad.x, 0f, "Con la escala X negativa mira y dispara a la izquierda.");
            Assert.Less(disparado.transform.position.x, -1f, "Sale por delante del bloque, a la izquierda.");
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

        // Los nombres de los prefabs, sin depender del ensamblado del editor.
        private static class GeneradorMecanicasNombres
        {
            public const string BolaDeFuego = "BolaDeFuego";
        }

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
