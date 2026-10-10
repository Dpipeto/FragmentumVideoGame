using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Core.Juego;
using Fragmentum.Data;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// El Nivel 2 montado, jugándose: el agua y el hielo de la escena cambian de verdad la física
    /// de Kael, y la repisa de la campana es un checkpoint seco. Las medidas las comprueban
    /// <c>DisenoNivel2Tests</c> y <c>EscenaNivel2Tests</c>; aquí, que funcionan en el juego.
    /// </summary>
    public class Nivel2PlayModeTests : PruebaFlujoBase
    {
        private LevelData Nivel2 => BaseDeDatos.niveles[1];

        private IEnumerator EntrarEnNivel2()
        {
            Gestor.Datos.nivelMaximoAlcanzado = Mathf.Max(Gestor.Datos.nivelMaximoAlcanzado, 2);
            Assert.IsTrue(Juego.JugarNivel(Nivel2), "El Nivel 2 tiene escena y está desbloqueado.");
            yield return EsperarEstado(ESTADOS_JUEGO.PLAYING, 8f);
            Assert.AreEqual(Nivel2.escena.Nombre, SceneManager.GetActiveScene().name);
            Assert.IsNotNull(Jugador, "El LevelManager del Nivel 2 hace aparecer al jugador.");
        }

        private static BoxCollider2D Zona(string nombre)
        {
            var zona = Object.FindObjectsByType<ZonaModificadorMovimiento>(FindObjectsInactive.Include)
                .SingleOrDefault(z => z.name == nombre);
            Assert.IsNotNull(zona, $"Falta la zona {nombre} en la escena.");
            return zona.GetComponent<BoxCollider2D>();
        }

        [UnityTest]
        public IEnumerator AlEmpezar_KaelEstaEnLaPiedra_SinAguaNiHielo()
        {
            yield return EntrarEnNivel2();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(Jugador.Motor.BajoElAgua);
            Assert.AreEqual(1f, Jugador.Motor.MultAceleracion, 1e-4f, "El inicio es de piedra: agarra.");
        }

        [UnityTest]
        public IEnumerator EnElHielo_Resbala_YEnElAgua_TodoPesa()
        {
            yield return EntrarEnNivel2();

            var hielo = Zona("HieloDelPrimerTramo").bounds;
            yield return LlevarJugadorA(new Vector2(hielo.max.x - 1f, hielo.min.y + 0.05f));
            Assert.Less(Jugador.Motor.MultAceleracion, 0.5f, "Sobre el hielo cuesta arrancar y frenar.");
            Assert.IsFalse(Jugador.Motor.BajoElAgua);

            var calle = Zona("AguaDeLaCalle").bounds;
            yield return LlevarJugadorA(new Vector2(calle.min.x + 2f, calle.min.y + 0.05f));
            Assert.IsTrue(Jugador.Motor.BajoElAgua, "En la calle hundida Kael está bajo el agua.");
            Assert.AreEqual(0.6f, Jugador.Motor.MultVelocidad, 1e-3f, "Anexo A: velocidad × 0,6.");
            Assert.AreEqual(0.6f, Jugador.Motor.MultAceleracion, 1e-3f, "La aceleración del agua, sin el hielo de arriba.");
        }

        // Mueve a Kael sin revivirlo: Revivir le devolvería la vida y el aire.
        private static IEnumerator Mover(Vector2 pies)
        {
            var cuerpo = Jugador.GetComponent<Rigidbody2D>();
            cuerpo.position = pies;
            cuerpo.linearVelocity = Vector2.zero;
            for (var i = 0; i < 6; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private static void ApagarEnemigos()
        {
            ApagarLamentos();
            foreach (var escultura in Object.FindObjectsByType<EsculturaDormida>(FindObjectsInactive.Exclude))
            {
                escultura.gameObject.SetActive(false);
            }

            foreach (var medusa in Object.FindObjectsByType<MedusaHielo>(FindObjectsInactive.Exclude))
            {
                medusa.gameObject.SetActive(false);
            }
        }

        // Los que andan: los Lamentos y la Plañidera.
        private static void ApagarLamentos()
        {
            foreach (var enemigo in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
            {
                if (enemigo.GetComponent<EsculturaDormida>() == null)
                {
                    enemigo.gameObject.SetActive(false);
                }
            }
        }

        [UnityTest]
        public IEnumerator ElTapon_UnGolpeLoAgrieta_YElCargadoLoRompe_YKaelCaeAlAgua()
        {
            yield return EntrarEnNivel2();
            ApagarEnemigos();
            var tapon = Object.FindAnyObjectByType<HieloRompible>();
            Assert.IsNotNull(tapon);
            var centro = (Vector2)tapon.transform.position;

            // Kael pegado al tapón por la izquierda, mirándolo: la espada lo alcanza de frente.
            yield return LlevarJugadorA(new Vector2(centro.x - 1.7f, 0.05f));
            Assert.AreEqual(1, Jugador.Mirando, "Kael mira hacia el tapón.");
            var espada = Jugador.transform.Find("Pivote/Espada").GetComponent<Fragmentum.Core.Combate.Hitbox>();

            yield return Espadazo(espada, false);
            Assert.AreEqual(ESTADOS_HIELO.CRACKED, tapon.Estado, "La espada lo nota (zona de golpe en la capa Enemy) y lo agrieta.");
            Assert.IsTrue(tapon.Tapa, "Un golpe normal no abre el paso.");

            yield return Espadazo(espada, true);
            Assert.IsTrue(tapon.Roto, "El golpe cargado lo rompe.");
            Assert.IsFalse(tapon.Tapa);

            // Sin el tapón, el hueco lleva a la calle hundida.
            yield return LlevarJugadorA(new Vector2(centro.x, 0.5f));
            var limite = Time.time + 3f;
            while (!Jugador.Motor.BajoElAgua && Time.time < limite)
            {
                yield return null;
            }

            Assert.IsTrue(Jugador.Motor.BajoElAgua, "Kael se hunde por el agujero.");
        }

        private static IEnumerator Espadazo(Fragmentum.Core.Combate.Hitbox espada, bool cargado)
        {
            espada.Cargado = cargado;
            espada.gameObject.SetActive(true);
            for (var i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            espada.gameObject.SetActive(false);
            espada.Cargado = false;
        }

        [UnityTest]
        public IEnumerator LaPlacaFlotante_SubeYBaja_LlevandoAKael()
        {
            yield return EntrarEnNivel2();
            ApagarEnemigos();
            var placa = Object.FindObjectsByType<PlataformaFlotante>(FindObjectsInactive.Exclude).Single(p => p.name == "PlacaFlotante1");
            yield return LlevarJugadorA(new Vector2(placa.transform.position.x, placa.transform.position.y + 0.05f));

            var minimo = float.MaxValue;
            var maximo = float.MinValue;
            var fin = Time.time + 1.8f;
            yield return new WaitForSeconds(0.2f);
            while (Time.time < fin)
            {
                yield return new WaitForFixedUpdate();
                var y = placa.GetComponent<Rigidbody2D>().position.y;
                minimo = Mathf.Min(minimo, y);
                maximo = Mathf.Max(maximo, y);
                Assert.AreEqual(y, Jugador.transform.position.y, 0.2f, "Kael va encima de la placa, sin despegarse ni hundirse.");
            }

            Assert.GreaterOrEqual(maximo - minimo, placa.amplitud * 0.9f, "En más de medio ciclo, la placa recorre al menos su amplitud.");
            Assert.IsFalse(Jugador.Motor.BajoElAgua, "Encima de la placa no se moja.");
            Assert.Less(Jugador.Motor.MultAceleracion, 0.5f, "Y resbala: es hielo.");
        }

        [UnityTest]
        public IEnumerator SinAire_KaelPierdeUnCorazonCada2s_SinRetroceso_YRespirarLoSalva()
        {
            yield return EntrarEnNivel2();
            var oxigeno = Jugador.GetComponent<OxigenoJugador>();
            Assert.IsNotNull(oxigeno);

            var calle = Zona("AguaDeLaCalle").bounds;
            yield return LlevarJugadorA(new Vector2(calle.min.x + 2f, calle.min.y + 0.05f));
            Assert.IsTrue(Jugador.Motor.BajoElAgua);
            Assert.IsTrue(Interfaz.Hud.Agua.BarraVisible, "Bajo el agua aparece la barra de aire.");
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(Interfaz.Hud.Agua.Velo, 0.3f, "Y los bordes de la pantalla se oscurecen.");

            var vida = Jugador.Vida.Vida;
            oxigeno.FijarAire(0.05f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(vida - 1, Jugador.Vida.Vida, "Sin aire, el primer corazón.");
            Assert.AreNotEqual(ESTADOS_JUGADOR.DAMAGED, Jugador.EstadoActual, "Sin retroceso: el ahogo no es un golpe.");
            Assert.IsTrue(oxigeno.Asfixiandose);
            Assert.IsTrue(Interfaz.Hud.Agua.Asfixia, "La barra parpadea en rojo.");

            yield return new WaitForSeconds(2.1f);
            Assert.AreEqual(vida - 2, Jugador.Vida.Vida, "Dos segundos después, otro.");

            // A la bolsa de aire de la campana: se respira.
            var cp1 = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include).Single(c => c.Id == "L2_CP1");
            yield return Mover(cp1.transform.position);
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(Jugador.Motor.BajoElAgua);
            Assert.IsFalse(oxigeno.Asfixiandose);
            Assert.AreEqual(1f, oxigeno.Fraccion, 1e-3f, "El aire vuelve entero.");
            Assert.AreEqual(vida - 2, Jugador.Vida.Vida, "Los corazones perdidos no vuelven.");
            Assert.IsFalse(Interfaz.Hud.Agua.BarraVisible, "Con el aire lleno y fuera del agua, la barra se va.");
            Assert.Less(Interfaz.Hud.Agua.Velo, 0.05f, "Y el velo también.");
        }

        [UnityTest]
        public IEnumerator TrasMorir_UnaSombraRepiteElIntento_YSeVa()
        {
            yield return EntrarEnNivel2();
            ApagarEnemigos();
            var eco = Object.FindAnyObjectByType<EcoDeIntentos>();
            Assert.IsNotNull(eco);
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(eco.Reproduciendo, "Antes de morir no hay sombra.");

            Jugador.Vida.Matar();
            yield return EsperarEstado(ESTADOS_JUEGO.GAME_OVER, 3f);
            Interfaz.PantallaGameOver.Opciones[0].onClick.Invoke(); // Reintentar
            yield return null;
            yield return null;

            Assert.Greater(eco.MuestrasDelUltimoIntento, 15, "Grabó el segundo que vivió Kael.");
            Assert.IsTrue(eco.Reproduciendo, "Al reaparecer, la sombra repite el intento.");
            Assert.IsNotNull(eco.Sombra.sprite, "Con los fotogramas de Kael.");
            Assert.That(eco.Sombra.color.a, Is.InRange(0.05f, 0.9f), "Translúcida.");
            Assert.IsNull(eco.Sombra.GetComponent<Collider2D>(), "No daña ni ayuda: no choca con nada.");

            yield return new WaitForSeconds(eco.MuestrasDelUltimoIntento / 30f + 1f);
            Assert.IsFalse(eco.Reproduciendo, "Al acabar el intento, se desvanece.");
        }

        [UnityTest]
        public IEnumerator AlTocarLaCampana_SuenaElEcoDeMemoria()
        {
            yield return EntrarEnNivel2();
            Fragmentum.Narrative.DialogueManager.Instancia.Limpiar(); // la apertura, fuera
            var cp1 = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include).Single(c => c.Id == "L2_CP1");
            yield return LlevarJugadorA(cp1.transform.position);
            yield return null;
            yield return null;
            StringAssert.Contains("silla vacía", Interfaz.Hud.LineaActual, "La línea de memoria del guion, junto a la silla vacía.");
        }

        [UnityTest]
        public IEnumerator LaRepisaDeLaCampana_EsUnCheckpointSeco()
        {
            yield return EntrarEnNivel2();
            var checkpoint = Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include).Single(c => c.Id == "L2_CP1");

            yield return LlevarJugadorA(checkpoint.transform.position);
            var nivel = Object.FindAnyObjectByType<LevelManager>();
            Assert.AreSame(checkpoint, nivel.Checkpoints.Activo, "Tocar la bandera la activa.");
            Assert.IsFalse(Jugador.Motor.BajoElAgua, "En la bolsa de aire de la campana se respira.");
        }

        // --- Fase C: los enemigos ---

        [UnityTest]
        public IEnumerator ElLamento_TeVuelvePesado_2s()
        {
            yield return EntrarEnNivel2();
            ApagarEnemigos();
            var lamento = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include).Single(e => e.name == "Lamento_1");
            lamento.gameObject.SetActive(true);
            lamento.enabled = false; // quieto: el golpe lo da la prueba

            // Que mire a la izquierda: el golpe empuja a Kael hacia la piedra del inicio, no hacia el agua.
            var pivote = lamento.transform.Find("Pivote");
            pivote.localScale = new Vector3(-Mathf.Abs(pivote.localScale.x), pivote.localScale.y, 1f);
            var arma = lamento.GetComponent<Pesar>().Golpes[0];
            yield return Mover(new Vector2(arma.transform.position.x - 0.3f, 0.05f)); // el golpe le llega desde la derecha

            var pesadumbre = Jugador.GetComponent<Pesadumbre>();
            Assert.IsNotNull(pesadumbre);
            Assert.AreEqual(1f, Jugador.Motor.MultVelocidad, 1e-4f, "Antes del golpe, a su velocidad.");

            var vida = Jugador.Vida.Vida;
            arma.gameObject.SetActive(true);
            for (var i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            arma.gameObject.SetActive(false);
            Assert.AreEqual(vida - 1, Jugador.Vida.Vida, "El zarpazo quita un corazón…");
            Assert.IsTrue(pesadumbre.Activa, "…y pesa.");
            Assert.AreEqual(0.5f, Jugador.Motor.MultVelocidad, 1e-4f, "Kael va a la mitad de velocidad.");

            yield return new WaitForSeconds(2.2f);
            Assert.IsFalse(pesadumbre.Activa, "A los 2 s se pasa.");
            Assert.AreEqual(1f, Jugador.Motor.MultVelocidad, 1e-4f);
        }

        [UnityTest]
        public IEnumerator LaEscultura_DuermeHastaQueKaelSeAcerca_YDespiertaSinMoverse()
        {
            yield return EntrarEnNivel2();
            ApagarLamentos();
            var esculturas = Object.FindObjectsByType<EsculturaDormida>(FindObjectsInactive.Exclude);
            Assert.AreEqual(1, esculturas.Length, "La segunda dejó su sitio a la Plañidera.");
            foreach (var dormida in esculturas)
            {
                Assert.AreEqual(ESTADOS_ESCULTURA.DORMIDA, dormida.Estado, $"{dormida.name}: de lejos, una estatua más.");
                Assert.IsFalse(dormida.GetComponent<EnemyController>().enabled);
            }

            var escultura = esculturas.Single(e => e.name == "Escultura_1");
            var x = escultura.transform.position.x;
            yield return Mover(new Vector2(x - 2f, 0.05f));
            yield return null;
            Assert.AreEqual(ESTADOS_ESCULTURA.DESPERTANDO, escultura.Estado, "Kael pasa cerca: despierta.");
            Assert.IsFalse(escultura.GetComponent<EnemyController>().enabled, "Mientras se despereza, aún no ataca.");

            yield return new WaitForSeconds(escultura.tiempoDespertar + 0.2f);
            Assert.AreEqual(ESTADOS_ESCULTURA.DESPIERTA, escultura.Estado);
            var controlador = escultura.GetComponent<EnemyController>();
            Assert.IsTrue(controlador.enabled);

            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(ESTADOS_ENEMIGO.CHASE, controlador.EstadoActual, "Ve a Kael…");
            Assert.AreEqual(x, escultura.transform.position.x, 0.05f, "…pero no anda (decisión del 2026-10-08).");
            Assert.AreEqual(-1, controlador.Mirando, "Se gira hacia él.");
        }

        [UnityTest]
        public IEnumerator LaEscultura_DescargaSuGolpe_SiKaelSeQuedaDelante()
        {
            yield return EntrarEnNivel2();
            ApagarLamentos();
            var escultura = Object.FindObjectsByType<EsculturaDormida>(FindObjectsInactive.Exclude).Single(e => e.name == "Escultura_1");
            escultura.DespertarYa();
            var x = escultura.transform.position.x;
            yield return Mover(new Vector2(x - 1.5f, 0.05f));

            var vida = Jugador.Vida.Vida;
            var limite = Time.time + 4f;
            while (Jugador.Vida.Vida == vida && Time.time < limite)
            {
                yield return null;
            }

            Assert.AreEqual(vida - 2, Jugador.Vida.Vida, "Su golpe quita 2.");
            Assert.AreEqual(x, escultura.transform.position.x, 0.05f, "Y sigue en su sitio.");
        }

        [UnityTest]
        public IEnumerator LaEscultura_AGolpesDespiertaEnElActo_YElGolpeCuenta()
        {
            yield return EntrarEnNivel2();
            ApagarLamentos();
            var escultura = Object.FindObjectsByType<EsculturaDormida>(FindObjectsInactive.Exclude).Single(e => e.name == "Escultura_1");
            var vida = escultura.GetComponent<Fragmentum.Core.Combate.Health>();
            var x = escultura.transform.position.x;

            // Kael a su izquierda, mirándola: la espada la alcanza (cubre de 0,3 a 1,5 u por delante).
            yield return Mover(new Vector2(x - 1.2f, 0.05f));
            Assert.AreEqual(1, Jugador.Mirando);
            var espada = Jugador.transform.Find("Pivote/Espada").GetComponent<Fragmentum.Core.Combate.Hitbox>();
            yield return Espadazo(espada, false);

            Assert.AreEqual(ESTADOS_ESCULTURA.DESPIERTA, escultura.Estado, "A golpes, despierta del todo, sin desperezarse.");
            Assert.IsTrue(escultura.GetComponent<EnemyController>().enabled);
            for (var i = 0; i < 4; i++)
            {
                yield return null;
            }

            Assert.AreEqual(vida.VidaMaxima - espada.Danio, vida.Vida, "Al encenderse no recupera la vida: el golpe a la estatua cuenta.");
        }

        [UnityTest]
        public IEnumerator LaMedusa_NoMuere_LaEspadaLaApartaYLaEncoge()
        {
            yield return EntrarEnNivel2();
            ApagarLamentos();
            var medusa = Object.FindObjectsByType<MedusaHielo>(FindObjectsInactive.Exclude).Single(m => m.name == "Medusa_1");
            medusa.amplitud = 0f; // quieta en el centro de su recorrido, para apuntarle
            yield return new WaitForFixedUpdate();
            var centro = medusa.Base;
            var escala = medusa.Cuerpo.localScale;

            // Bajo el agua, a su izquierda: la espada la alcanza y su contacto no toca a Kael.
            yield return Mover(new Vector2(centro.x - 1.3f, centro.y - 0.9f));
            Assert.IsTrue(Jugador.Motor.BajoElAgua);
            Assert.AreEqual(1, Jugador.Mirando);
            var vida = Jugador.Vida.Vida;
            var espada = Jugador.transform.Find("Pivote/Espada").GetComponent<Fragmentum.Core.Combate.Hitbox>();

            yield return Espadazo(espada, false);
            yield return new WaitForSeconds(0.15f);
            Assert.IsTrue(medusa.Encogida, "La espada la encoge…");
            Assert.Greater(medusa.Apartado.x, 1f, "…y la aparta, lejos de Kael.");
            Assert.Less(medusa.Cuerpo.localScale.x, escala.x * 0.9f);
            Assert.AreEqual(vida, Jugador.Vida.Vida, "Golpearla de lejos no quema.");

            yield return new WaitForSeconds(2f);
            Assert.IsFalse(medusa.Encogida, "Al rato vuelve a su tamaño…");
            Assert.Less(medusa.Apartado.magnitude, 0.05f, "…y a su línea.");

            // Ni el Ataque Cargado la mata. (Kael se ha hundido: vuelve a su sitio.)
            yield return Mover(new Vector2(centro.x - 1.3f, centro.y - 0.9f));
            yield return Espadazo(espada, true);
            Assert.IsTrue(medusa.isActiveAndEnabled && medusa.EstaVivo, "No muere: tampoco con el golpe cargado.");
            Assert.IsTrue(medusa.Encogida);
        }

        [UnityTest]
        public IEnumerator LaMedusa_QuemaAlContacto()
        {
            yield return EntrarEnNivel2();
            ApagarLamentos();
            var medusa = Object.FindObjectsByType<MedusaHielo>(FindObjectsInactive.Exclude).Single(m => m.name == "Medusa_1");
            medusa.amplitud = 0f;
            yield return new WaitForFixedUpdate();

            var vida = Jugador.Vida.Vida;
            yield return Mover(new Vector2(medusa.Base.x, medusa.Base.y - 0.9f));
            Assert.AreEqual(vida - 1, Jugador.Vida.Vida, "Tocarla quema.");
        }

        // --- La Plañidera, el mini-jefe ---

        // Ella sola en la plaza: sin el Lamento de al lado, la Escultura ni las Medusas.
        private static Planidera SoloLaPlanidera()
        {
            var planidera = Object.FindAnyObjectByType<Planidera>();
            Assert.IsNotNull(planidera, "La Plañidera está en la plaza este.");
            foreach (var enemigo in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
            {
                if (enemigo.gameObject != planidera.gameObject)
                {
                    enemigo.gameObject.SetActive(false);
                }
            }

            foreach (var medusa in Object.FindObjectsByType<MedusaHielo>(FindObjectsInactive.Exclude))
            {
                medusa.gameObject.SetActive(false);
            }

            return planidera;
        }

        [UnityTest]
        public IEnumerator LaPlanidera_AlzaApuntaYLanzaSusEsquirlas_YSeQuedaDesarmadaSinAtacar()
        {
            yield return EntrarEnNivel2();
            var planidera = SoloLaPlanidera();
            var controlador = planidera.GetComponent<EnemyController>();
            var lanzadas = new List<Proyectil>();
            planidera.OnEsquirla += lanzadas.Add;

            // Kael sigue lejos, en el inicio: así no la pilla a mitad de un barrido.
            planidera.EmpezarRafaga();
            Assert.AreEqual(ESTADOS_PLANIDERA.ALZA, planidera.Estado);
            Assert.IsFalse(controlador.enabled, "Con las esquirlas en alto, ni anda ni barre.");
            var x = planidera.transform.position.x;

            // Kael, quieto a 3 u, en la plaza este.
            yield return Mover(new Vector2(x + 3f, 0.05f));
            var vida = Jugador.Vida.Vida;
            yield return new WaitForSeconds(planidera.tiempoAlzar + 0.1f);
            Assert.AreEqual(ESTADOS_PLANIDERA.APUNTA, planidera.Estado, "Antes de lanzar, apunta: es el aviso.");
            Assert.AreEqual(0, lanzadas.Count);
            Assert.AreEqual(vida, Jugador.Vida.Vida, "El aviso no hace daño.");

            yield return new WaitForSeconds(planidera.tiempoApuntar + (planidera.esquirlas - 1) * planidera.intervaloEsquirlas + 0.3f);
            Assert.AreEqual(planidera.esquirlas, lanzadas.Count, "Las lanza una tras otra.");
            Assert.AreEqual(ESTADOS_PLANIDERA.DESARMADA, planidera.Estado, "Sin esquirlas, se queda desarmada.");
            Assert.Less(Jugador.Vida.Vida, vida, "Si Kael no se mueve, las esquirlas le dan.");

            // Desarmada: Kael se le pega por la izquierda y la golpea. Ella ni se mueve ni responde.
            yield return Mover(new Vector2(x - 1.2f, 0.05f));
            Assert.AreEqual(1, Jugador.Mirando, "Kael la mira.");
            var vidaDeElla = planidera.GetComponent<Health>();
            var antes = vidaDeElla.Vida;
            var vidaDeKael = Jugador.Vida.Vida;
            var espada = Jugador.transform.Find("Pivote/Espada").GetComponent<Hitbox>();
            yield return Espadazo(espada, false);
            Assert.AreEqual(antes - espada.Danio, vidaDeElla.Vida, "Es el momento de castigarla.");

            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(ESTADOS_PLANIDERA.DESARMADA, planidera.Estado);
            Assert.IsFalse(controlador.enabled);
            Assert.AreEqual(vidaDeKael, Jugador.Vida.Vida, "Desarmada no ataca, y tocarla no hace daño.");
            Assert.AreEqual(x, planidera.transform.position.x, 0.05f, "Ni se mueve.");

            yield return new WaitForSeconds(planidera.tiempoDesarmada + planidera.tiempoRehacer);
            Assert.AreEqual(ESTADOS_PLANIDERA.ACECHA, planidera.Estado, "Las esquirlas vuelven…");
            Assert.IsTrue(controlador.enabled, "…y sigue acechando.");
        }

        [UnityTest]
        public IEnumerator LaPlanidera_DeCerca_AvisaYBarreConElBrazo()
        {
            yield return EntrarEnNivel2();
            var planidera = SoloLaPlanidera();
            planidera.esperaEntreRafagas = 999f; // solo el barrido
            var controlador = planidera.GetComponent<EnemyController>();
            var x = planidera.transform.position.x;
            yield return Mover(new Vector2(x - 1.2f, 0.05f));

            var vida = Jugador.Vida.Vida;
            var limite = Time.time + 5f;
            var inicioDelAtaque = -1f;
            while (Jugador.Vida.Vida == vida && Time.time < limite)
            {
                if (inicioDelAtaque < 0f && controlador.EstadoActual == ESTADOS_ENEMIGO.ATTACK)
                {
                    inicioDelAtaque = Time.time;
                }

                yield return null;
            }

            Assert.AreEqual(vida - 2, Jugador.Vida.Vida, "Su barrido quita 2, como el golpe del Golem.");
            Assert.GreaterOrEqual(inicioDelAtaque, 0f, "El daño viene de su ataque, no de tocarla.");
            Assert.GreaterOrEqual(Time.time - inicioDelAtaque, 0.45f, "Echa el brazo atrás antes de barrer: da tiempo a apartarse.");
            Assert.AreEqual(ESTADOS_PLANIDERA.ACECHA, planidera.Estado);
        }

        [UnityTest]
        public IEnumerator LaPlanidera_TrasAcecharAKael_AlzaLasEsquirlasEllaSola()
        {
            yield return EntrarEnNivel2();
            var planidera = SoloLaPlanidera();
            var controlador = planidera.GetComponent<EnemyController>();
            Assert.IsFalse(planidera.ConLasEsquirlas, "Mientras no ve a Kael, no las alza.");

            yield return Mover(new Vector2(planidera.transform.position.x + 3f, 0.05f));
            var vioAKael = false;
            var limite = Time.time + 10f;
            while (!planidera.ConLasEsquirlas && Time.time < limite)
            {
                vioAKael |= controlador.EstadoActual == ESTADOS_ENEMIGO.CHASE;
                yield return null;
            }

            Assert.IsTrue(vioAKael, "Lo ve y flota hacia él.");
            Assert.IsTrue(planidera.ConLasEsquirlas, "Tras acecharlo un rato, alza las esquirlas.");
            Assert.IsFalse(controlador.enabled);
        }

        [UnityTest]
        public IEnumerator LaPlanidera_SiMuereConLasEsquirlasEnAlto_MuereDeVerdad()
        {
            yield return EntrarEnNivel2();
            var planidera = SoloLaPlanidera();
            var controlador = planidera.GetComponent<EnemyController>();
            yield return new WaitForSeconds(0.2f);
            planidera.EmpezarRafaga();
            yield return null;
            Assert.IsFalse(controlador.enabled);

            planidera.GetComponent<Health>().Matar();
            Assert.IsTrue(controlador.enabled, "Su controlador se enciende a tiempo de oír la muerte.");
            Assert.AreEqual(ESTADOS_ENEMIGO.DEAD, controlador.EstadoActual, "Y muere por el camino de siempre.");
            Assert.AreEqual(ESTADOS_PLANIDERA.ACECHA, planidera.Estado, "No se queda apuntando.");

            yield return new WaitForSeconds(controlador.Datos.deathDelay + 0.3f);
            Assert.IsTrue(planidera == null, "Desaparece.");
        }
    }
}
