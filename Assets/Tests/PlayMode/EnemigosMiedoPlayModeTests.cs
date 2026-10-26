using System.Collections;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Enemies;
using Fragmentum.Levels.Escenario;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Los enemigos del mundo del Miedo (fase C) con sus prefabs, el jugador real y física real:
    ///
    /// - El Espectro duerme hasta que Kael pasa cerca o una antorcha se enciende con él dentro (y solo
    ///   a esos); con Kael en la luz sale de ella y espera en su borde, y cuando se apaga va a por él;
    ///   a oscuras no se ve, en la luz de Kael se adivina y al atacar se ve entero.
    /// - El Centinela Ciego oye delante hasta donde llega el ruido y detrás solo de cerca; al oír se
    ///   para y ladea la cabeza, va hacia el ruido y apuñala allí; si no hay nadie, vuelve a su ronda.
    /// - El Acechador oye el doble de lejos, se agazapa y embiste: contra un árbol se queda aturdido
    ///   y los golpes valen el doble (y muere una sola vez); contra Kael, le quita 2; si no choca, frena
    ///   en el borde de su territorio. De cerca, muerde.
    /// </summary>
    public class EnemigosMiedoPlayModeTests : PruebaPlayModeBase
    {
        private T Enemigo<T>(string variante, Vector2 pies) where T : Component
        {
            var objeto = Object.Instantiate(CargarPrefab($"Assets/Prefabs/Enemigos/Enemigo_{variante}.prefab"),
                new Vector3(pies.x, pies.y + 0.05f, 0f), Quaternion.identity);
            Creados.Add(objeto);
            var componente = objeto.GetComponent<T>();
            Assert.IsNotNull(componente, $"El prefab de {variante} no tiene {typeof(T).Name}.");
            return componente;
        }

        private Antorcha AntorchaEn(Vector2 pie)
        {
            var objeto = Object.Instantiate(CargarPrefab("Assets/Prefabs/Mecanicas/Antorcha.prefab"), new Vector3(pie.x, pie.y, 0f), Quaternion.identity);
            Creados.Add(objeto);
            return objeto.GetComponent<Antorcha>();
        }

        private Arbol ArbolEn(float x)
        {
            var objeto = new GameObject("Arbol");
            objeto.transform.position = new Vector3(x, 0f, 0f);
            Creados.Add(objeto);
            var arbol = objeto.AddComponent<Arbol>();
            arbol.Fijar(new Vector2(1.2f, 6f));
            return arbol;
        }

        private static Vector2 Centro(Component enemigo) => enemigo.GetComponent<Collider2D>().bounds.center;

        private PlayerModoMovimiento Ruido => Jugador.GetComponent<PlayerModoMovimiento>();

        // --- El Espectro ---

        [UnityTest]
        public IEnumerator Espectro_DuermeHastaQueKaelPasaCerca_YDespiertoLoPersigueAOscuras()
        {
            Suelo(-12f, 20f);
            yield return CrearJugadorEnSuelo(new Vector2(-6f, 0f));
            var espectro = Enemigo<Espectro>("Espectro", Vector2.zero);
            var controlador = espectro.GetComponent<EnemyController>();

            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(espectro.Dormido);
            Assert.IsFalse(controlador.enabled, "Dormido, ni ve, ni anda, ni ataca.");

            Jugador.Motor.Teletransportar(new Vector2(-1.2f, 0.05f));
            yield return EsperarHasta(() => !espectro.Dormido, 0.5f, "que despierte al pasar Kael a 1,5 u");
            Assert.IsTrue(controlador.enabled);
            yield return EsperarHasta(() => controlador.EstadoActual == ESTADOS_ENEMIGO.CHASE || controlador.EstadoActual == ESTADOS_ENEMIGO.ATTACK,
                1.5f, "que vaya a por Kael, que está a oscuras");
        }

        [UnityTest]
        public IEnumerator Espectro_UnaAntorchaDespiertaALosDeSuLuz_YNoALosDeMasLejos()
        {
            Suelo(-12f, 30f);
            yield return CrearJugadorEnSuelo(new Vector2(-6f, 0f));
            var antorcha = AntorchaEn(new Vector2(-4f, 0f));
            var cerca = Enemigo<Espectro>("Espectro", new Vector2(3f, 0f));
            var lejos = Enemigo<Espectro>("Espectro", new Vector2(12f, 0f));
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(cerca.Dormido && lejos.Dormido);

            Assert.Less(Vector2.Distance(antorcha.Posicion, Centro(cerca)), antorcha.Radio);
            Assert.Greater(Vector2.Distance(antorcha.Posicion, Centro(lejos)), antorcha.Radio);
            Assert.IsTrue(antorcha.Encender());
            yield return null;
            Assert.IsFalse(cerca.Dormido, "Lo que enseña la luz lo despierta ella misma.");
            Assert.IsTrue(lejos.Dormido, "Pero solo a los de su luz.");
        }

        [UnityTest]
        public IEnumerator Espectro_ConKaelEnLaLuz_SaleDeEllaYEsperaEnSuBorde_YCuandoSeApagaVaAPorEl()
        {
            Suelo(-12f, 30f);
            yield return CrearJugadorEnSuelo(new Vector2(-5f, 0f));
            var antorcha = AntorchaEn(new Vector2(-4f, 0f));
            antorcha.duracion = 3f;
            antorcha.parpadeo = 0.5f;
            var espectro = Enemigo<Espectro>("Espectro", new Vector2(1f, 0f));
            var controlador = espectro.GetComponent<EnemyController>();
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(antorcha.Encender());
            yield return EsperarHasta(() => espectro.Estado == ESTADOS_ESPECTRO.AL_BORDE, 0.5f, "que no se acerque a Kael, que está en la luz");
            yield return EsperarHasta(() => !Iluminacion.EstaIluminado(Centro(espectro)), 3f, "que salga de la luz");
            Assert.Greater(Centro(espectro).x, antorcha.Posicion.x, "Por el lado contrario a la antorcha.");

            var borde = espectro.transform.position.x;
            yield return ComprobarQueNunca(() => Iluminacion.EstaIluminado(Centro(espectro)), 0.8f, "Espera fuera de la luz.");
            Assert.AreEqual(borde, espectro.transform.position.x, 0.6f, "En su borde: ni se va ni entra.");
            Assert.AreEqual(ESTADOS_ESPECTRO.AL_BORDE, espectro.Estado);

            yield return EsperarHasta(() => !antorcha.Encendida, 3f, "que se apague la antorcha");
            yield return EsperarHasta(() => controlador.EstadoActual == ESTADOS_ENEMIGO.CHASE || controlador.EstadoActual == ESTADOS_ENEMIGO.ATTACK,
                4f, "que, a oscuras, vaya a por Kael");
        }

        [UnityTest]
        public IEnumerator Espectro_AOscurasNoSeVe_EnLaLuzDeKaelSeAdivina_YAlAtacarSeVeEntero()
        {
            Suelo(-12f, 20f);
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            Jugador.GetComponent<LuzJugador>().Encender(3f);
            var espectro = Enemigo<Espectro>("Espectro", Vector2.zero);
            var controlador = espectro.GetComponent<EnemyController>();

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0f, espectro.Opacidad, 0.01f, "A oscuras, lejos de la luz de Kael, no se ve.");

            Jugador.Motor.Teletransportar(new Vector2(-2.5f, 0.05f));
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(espectro.Dormido, "A 2,5 u sigue dormido…");
            Assert.AreEqual(0.25f, espectro.Opacidad, 0.01f, "…pero dentro de la luz de Kael se adivina.");

            espectro.Despertar();
            yield return EsperarHasta(() => controlador.EstadoActual == ESTADOS_ENEMIGO.ATTACK, 3f, "que ataque a Kael, que está a oscuras");
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1f, espectro.Opacidad, 0.01f, "Al atacar se le encienden los ojos y se ve entero, antes del golpe.");
        }

        [UnityTest]
        public IEnumerator Espectro_GolpeadoDormido_Despierta_YElGolpeCuenta()
        {
            Suelo(-12f, 20f);
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            var espectro = Enemigo<Espectro>("Espectro", Vector2.zero);
            var vida = espectro.GetComponent<Health>();
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(vida.RecibirGolpe(new Golpe(1, new Vector2(-1f, 0.8f))));
            Assert.IsFalse(espectro.Dormido, "Un golpe lo despierta.");
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(vida.VidaMaxima - 1, vida.Vida, "Y el golpe cuenta, aunque al despertar su controlador le devuelva la vida.");
        }

        // --- El Centinela Ciego ---

        [UnityTest]
        public IEnumerator CentinelaCiego_DetrasSoloOyeDeCerca()
        {
            Suelo(-15f, 15f);
            yield return CrearJugadorEnSuelo(new Vector2(-10f, 0f));
            var oido = Enemigo<CentinelaCiego>("CentinelaCiego", Vector2.zero);
            var controlador = oido.GetComponent<EnemyController>();
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1, controlador.Mirando, "Mira a la derecha: Kael está a su espalda.");

            Ruido.HacerRuido(new Vector2(-4f, 0f), 6f);
            yield return null;
            Assert.IsFalse(oido.Alerta, "Por la espalda, a 4 u, no oye correr.");

            Ruido.HacerRuido(new Vector2(-2.5f, 0f), 6f);
            yield return null;
            Assert.AreEqual(ESTADOS_CENTINELA_CIEGO.ESCUCHA, oido.Estado, "Detrás y de cerca, sí.");
            Assert.AreEqual(-1, controlador.Mirando, "Y se gira hacia el ruido.");
        }

        [UnityTest]
        public IEnumerator CentinelaCiego_AlOirSeParaYLadeaLaCabeza_VaHaciaElRuido_YApunalaAlli()
        {
            Suelo(-15f, 15f);
            yield return CrearJugadorEnSuelo(new Vector2(4f, 0f));
            var oido = Enemigo<CentinelaCiego>("CentinelaCiego", Vector2.zero);
            var controlador = oido.GetComponent<EnemyController>();
            yield return new WaitForSeconds(0.3f);
            var vidaKael = Jugador.Vida.Vida;

            Ruido.HacerRuido(PiesJugador, 6f);
            yield return null;
            Assert.AreEqual(ESTADOS_CENTINELA_CIEGO.ESCUCHA, oido.Estado, "Delante, a 4 u: lo oye.");
            Assert.IsFalse(controlador.enabled, "Mientras oye, su oído lleva el mando.");
            var antes = oido.transform.position.x;
            yield return new WaitForSeconds(0.45f);
            Assert.AreEqual(ESTADOS_CENTINELA_CIEGO.ESCUCHA, oido.Estado, "Ladea la cabeza 0,6 s: el aviso.");
            Assert.AreEqual(antes, oido.transform.position.x, 0.05f, "Sin moverse.");

            yield return EsperarHasta(() => oido.Estado == ESTADOS_CENTINELA_CIEGO.ESTOCADA, 2f, "que vaya hacia el ruido y apuñale");
            yield return EsperarHasta(() => Jugador.Vida.Vida <= vidaKael - 2, 1f, "que la estocada alcance a Kael, que seguía allí");
            yield return EsperarHasta(() => oido.Estado == ESTADOS_CENTINELA_CIEGO.RONDA, 3.5f, "que vuelva a su ronda");
            Assert.IsTrue(controlador.enabled);
        }

        [UnityTest]
        public IEnumerator CentinelaCiego_SiNoHayNadieDondeOyo_ApunalaElAire_YALosDosSegundosVuelveASuRonda()
        {
            Suelo(-15f, 15f);
            yield return CrearJugadorEnSuelo(new Vector2(-10f, 0f));
            var oido = Enemigo<CentinelaCiego>("CentinelaCiego", Vector2.zero);
            yield return new WaitForSeconds(0.3f);
            var vidaKael = Jugador.Vida.Vida;

            Ruido.HacerRuido(new Vector2(5f, 0f), 6f);
            yield return EsperarHasta(() => oido.Estado == ESTADOS_CENTINELA_CIEGO.ESTOCADA, 2.5f, "que vaya al ruido y apuñale");
            Assert.AreEqual(5f - oido.distanciaEstocada, Centro(oido).x, 0.3f, "Donde oyó el ruido, no donde está Kael.");

            yield return EsperarHasta(() => oido.Estado == ESTADOS_CENTINELA_CIEGO.BUSCA, 1f, "que acabe la estocada");
            var buscando = Time.time;
            yield return EsperarHasta(() => oido.Estado == ESTADOS_CENTINELA_CIEGO.RONDA, 3f, "que vuelva a su ronda");
            Assert.AreEqual(oido.tiempoBusqueda, Time.time - buscando, 0.15f, "Escucha 2 s más.");
            Assert.AreEqual(vidaKael, Jugador.Vida.Vida);
        }

        // --- El Acechador ---

        [UnityTest]
        public IEnumerator Acechador_SeEstrellaContraUnArbol_YAturdidoLosGolpesValenElDoble()
        {
            Suelo(-15f, 30f);
            var arbol = ArbolEn(8f);
            yield return CrearJugadorEnSuelo(new Vector2(8.2f, 0f));
            var acechador = Enemigo<Acechador>("Acechador", Vector2.zero);
            var controlador = acechador.GetComponent<EnemyController>();
            var vida = acechador.GetComponent<Health>();
            Arbol choque = null;
            acechador.OnChoque += a => choque = a;
            yield return new WaitForSeconds(0.3f);
            var vidaKael = Jugador.Vida.Vida;

            Ruido.HacerRuido(PiesJugador, 6f);
            yield return null;
            Assert.AreEqual(ESTADOS_ACECHADOR.AGAZAPADO, acechador.Estado, "A 8 u: más lejos que los anillos, pero oye el doble.");
            yield return new WaitForSeconds(0.55f);
            Assert.AreEqual(ESTADOS_ACECHADOR.AGAZAPADO, acechador.Estado, "Se agazapa 0,7 s: el aviso.");

            yield return EsperarHasta(() => acechador.Aturdido, 2f, "que embista y se estrelle contra el árbol");
            Assert.AreSame(arbol, choque);
            Assert.LessOrEqual(acechador.GetComponent<Collider2D>().bounds.max.x, arbol.Tronco.xMin + 0.3f, "Se para en el tronco.");
            Assert.AreEqual(vidaKael, Jugador.Vida.Vida, "Kael, delante del árbol, se libra.");

            Assert.IsTrue(vida.RecibirGolpe(new Golpe(1, PiesJugador)));
            yield return null;
            yield return null;
            Assert.AreEqual(vida.VidaMaxima - 2, vida.Vida, "Aturdido, un golpe vale el doble.");

            yield return EsperarHasta(() => acechador.Estado == ESTADOS_ACECHADOR.MERODEA, 3f, "que se recupere");
            Assert.IsTrue(controlador.enabled);
        }

        [UnityTest]
        public IEnumerator Acechador_AturdidoMuere_PorElCaminoDeSiempre_YUnaSolaVez()
        {
            Suelo(-15f, 30f);
            ArbolEn(8f);
            yield return CrearJugadorEnSuelo(new Vector2(8.2f, 0f));
            var acechador = Enemigo<Acechador>("Acechador", Vector2.zero);
            var controlador = acechador.GetComponent<EnemyController>();
            var vida = acechador.GetComponent<Health>();
            yield return new WaitForSeconds(0.3f);
            vida.PerderVida(vida.Vida - 2);
            var muertes = 0;
            vida.OnMuerte += () => muertes++;

            Ruido.HacerRuido(PiesJugador, 6f);
            yield return EsperarHasta(() => acechador.Aturdido, 2.5f, "que se estrelle");
            vida.RecibirGolpe(new Golpe(1, PiesJugador));
            yield return null;
            yield return null;

            Assert.IsFalse(vida.EstaVivo, "Con 2 de vida, un golpe aturdido lo mata.");
            Assert.AreEqual(1, muertes, "Avisa de su muerte una sola vez (el marcador suma sus puntos una vez).");
            Assert.AreEqual(ESTADOS_ENEMIGO.DEAD, controlador.EstadoActual, "Muere por el camino de siempre.");
        }

        [UnityTest]
        public IEnumerator Acechador_SinUnArbolDelante_ArrollaAKael_LeQuitaDos_YFrena()
        {
            Suelo(-15f, 30f);
            yield return CrearJugadorEnSuelo(new Vector2(6f, 0f));
            var acechador = Enemigo<Acechador>("Acechador", Vector2.zero);
            yield return new WaitForSeconds(0.3f);
            var vidaKael = Jugador.Vida.Vida;

            Ruido.HacerRuido(PiesJugador, 6f);
            yield return EsperarHasta(() => Jugador.Vida.Vida <= vidaKael - 2, 2.5f, "que lo arrolle: quita 2");
            Assert.AreEqual(vidaKael - 2, Jugador.Vida.Vida);
            yield return null;
            Assert.AreNotEqual(ESTADOS_ACECHADOR.EMBISTE, acechador.Estado, "Tras arrollarlo, frena.");
            yield return EsperarHasta(() => acechador.Estado == ESTADOS_ACECHADOR.MERODEA, 2f, "que vuelva a merodear");
        }

        [UnityTest]
        public IEnumerator Acechador_SiNoChocaConNada_FrenaEnElBordeDeSuTerritorio()
        {
            Suelo(-15f, 40f);
            yield return CrearJugadorEnSuelo(new Vector2(-12f, 0f));
            var acechador = Enemigo<Acechador>("Acechador", Vector2.zero);
            acechador.limites = new Vector2(-10f, 6f);
            yield return new WaitForSeconds(0.3f);

            Ruido.HacerRuido(new Vector2(10f, 0f), 6f);
            yield return EsperarHasta(() => acechador.Estado == ESTADOS_ACECHADOR.FRENA, 2.5f, "que embista y frene");
            yield return EsperarHasta(() => acechador.Estado == ESTADOS_ACECHADOR.MERODEA, 1f, "que vuelva a merodear");
            Assert.LessOrEqual(Centro(acechador).x, 6.1f, "Sin salir de su territorio (el claro).");
            Assert.Greater(Centro(acechador).x, 4f, "Pero hasta su borde.");
        }

        [UnityTest]
        public IEnumerator Acechador_SiKaelSeLePega_EchaLaCabezaAtrasYMuerde()
        {
            Suelo(-15f, 15f);
            yield return CrearJugadorEnSuelo(new Vector2(1.9f, 0f));
            var vidaKael = Jugador.Vida.Vida;
            var acechador = Enemigo<Acechador>("Acechador", Vector2.zero);
            var controlador = acechador.GetComponent<EnemyController>();

            yield return EsperarHasta(() => controlador.EstadoActual == ESTADOS_ENEMIGO.ATTACK, 2f, "que eche la cabeza atrás");
            yield return EsperarHasta(() => Jugador.Vida.Vida < vidaKael, 1.5f, "que muerda");
            Assert.AreEqual(vidaKael - 1, Jugador.Vida.Vida, "El mordisco quita 1.");
            Assert.AreEqual(ESTADOS_ACECHADOR.MERODEA, acechador.Estado, "Sin ruido no embiste: muerde.");
        }
    }
}
