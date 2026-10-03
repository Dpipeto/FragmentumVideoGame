using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Criterios de aceptación de la tarea C con física real: el jefe cambia de fase por su
    /// vida, un golpe no lo interrumpe, sus patrones hacen daño y abren la ventana vulnerable, y
    /// al derrotarlo se emite el evento que otorga el fragmento. Cada prueba monta una arena
    /// plana con el prefab real del jefe y el del jugador.
    /// </summary>
    public class JefePlayModeTests : PruebaPlayModeBase
    {
        private const string CarpetaJefes = "Assets/Prefabs/Jefes";
        private const string CarpetaPatrones = "Assets/Data/Jefes/Patrones";
        private static readonly Vector2 LimitesArena = new Vector2(-15f, 15f);

        [UnityTearDown]
        public IEnumerator BorrarZonasDeDanio()
        {
            // Las zonas que quedan vivas no deben alcanzar al jugador de la prueba siguiente.
            foreach (var zona in Object.FindObjectsByType<ZonaDeDanio>(FindObjectsInactive.Include))
            {
                Object.Destroy(zona.gameObject);
            }

            yield return null;
        }

        // --- Fases ---

        [UnityTest]
        public IEnumerator Activar_EmpiezaEnLaFase1_ConDanioPorContacto()
        {
            var jefe = CrearJefe("CentinelaArdiente");
            yield return CrearJugadorEnSuelo(new Vector2(-10f, 0f));
            Assert.IsFalse(jefe.EnCombate, "Espera hasta que la arena lo active.");
            Assert.IsFalse(Contacto(jefe).gameObject.activeSelf, "Fuera del combate no hace daño.");

            jefe.Activar(Jugador.transform, LimitesArena);

            Assert.IsTrue(jefe.EnCombate);
            Assert.AreEqual(ESTADOS_JEFE.PHASE_1, jefe.EstadoActual);
            Assert.AreEqual(1, jefe.Fase);
            Assert.IsTrue(Contacto(jefe).gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator AlBajarDeLaMitadDeVida_PasaALaFase2_UnaSolaVez()
        {
            var jefe = CrearJefe("CentinelaArdiente");
            SinPatrones(jefe, 1f, 0.5f);
            yield return CrearJugadorEnSuelo(new Vector2(-10f, 0f));
            var fases = new List<int>();
            jefe.OnFaseCambiada += fases.Add;
            jefe.Activar(Jugador.transform, LimitesArena);

            jefe.Vida.RecibirGolpe(new Golpe(jefe.Vida.VidaMaxima / 2, jefe.Centro));
            yield return null;
            yield return null;

            Assert.AreEqual(ESTADOS_JEFE.PHASE_2, jefe.EstadoActual);
            CollectionAssert.AreEqual(new[] { 1, 2 }, fases);
        }

        [UnityTest]
        public IEnumerator LaVentanaVulnerable_DuplicaElDanio_YVuelveALaFaseQueToca()
        {
            var jefe = CrearJefe("CentinelaArdiente");
            SinPatrones(jefe, 1f, 0.5f);
            yield return CrearJugadorEnSuelo(new Vector2(-10f, 0f));
            jefe.Activar(Jugador.transform, LimitesArena);

            jefe.AbrirVentanaVulnerable(0.5f);
            yield return null;
            Assert.AreEqual(ESTADOS_JEFE.VULNERABLE, jefe.EstadoActual);

            var antes = jefe.Vida.Vida;
            jefe.Vida.RecibirGolpe(new Golpe(1, jefe.Centro));
            Assert.AreEqual(antes - 2, jefe.Vida.Vida, "En la ventana, cada golpe vale el doble.");

            // Durante la ventana baja de la mitad: al cerrarse, sale directamente a la fase 2.
            yield return new WaitForSeconds(0.2f);
            jefe.Vida.RecibirGolpe(new Golpe(jefe.Vida.VidaMaxima / 4, jefe.Centro));
            yield return EsperarHasta(() => jefe.EstadoActual != ESTADOS_JEFE.VULNERABLE, 1f, "que se cierre la ventana");
            Assert.AreEqual(ESTADOS_JEFE.PHASE_2, jefe.EstadoActual);
        }

        [UnityTest]
        public IEnumerator LaIraDesatada_LlegaAlCuartoDeVida_YRugeAntesDeAtacar()
        {
            var jefe = CrearJefe("CentinelaArdiente");
            yield return CrearJugadorEnSuelo(new Vector2(-10f, 0f));
            var fases = new List<int>();
            jefe.OnFaseCambiada += fases.Add;
            jefe.Activar(Jugador.transform, LimitesArena);
            var colorFase1 = jefe.GetComponentInChildren<SpriteRenderer>().color;

            jefe.Vida.RecibirGolpe(new Golpe(Mathf.CeilToInt(jefe.Vida.VidaMaxima * 0.8f), jefe.Centro));
            yield return EsperarHasta(() => jefe.EstadoActual == ESTADOS_JEFE.PHASE_3, 1f, "la fase 3");
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, fases, "Las fases llegan de una en una.");

            // Ruge: quieta y sin atacar mientras dura el aviso.
            yield return ComprobarQueNunca(() => jefe.PatronActual != null, 0.6f, "Durante el rugido no ataca.");
            yield return EsperarHasta(() => jefe.PatronActual != null, 2f, "que empiece a atacar tras el rugido");

            Assert.Greater(jefe.VelocidadActual, jefe.Datos.velocidad, "En la ira desatada persigue más rápido.");
            Assert.AreNotEqual(colorFase1, jefe.GetComponentInChildren<SpriteRenderer>().color, "Y se pone al rojo.");
        }

        // --- Patrones ---

        [UnityTest]
        public IEnumerator Espadazo_EsUnComboDeDos_YSoloElUltimoDejaLaEspadaClavada()
        {
            var jefe = CrearJefe("CentinelaArdiente");
            SoloPatron(jefe, "Espadazo");
            yield return CrearJugadorEnSuelo(new Vector2(-2.8f, 0f));
            var golpes = new HashSet<ZonaDeDanio>();
            jefe.Activar(Jugador.transform, LimitesArena);

            var limite = Time.time + 5f;
            while (jefe.EstadoActual != ESTADOS_JEFE.VULNERABLE && Time.time < limite)
            {
                foreach (var zona in Object.FindObjectsByType<ZonaDeDanio>())
                {
                    if (zona.name.EndsWith("Espadazo"))
                    {
                        golpes.Add(zona);
                    }
                }

                yield return null;
            }

            Assert.AreEqual(ESTADOS_JEFE.VULNERABLE, jefe.EstadoActual, "Tras el combo, la espada queda clavada.");
            Assert.AreEqual(2, golpes.Count, "Dos espadazos antes de la ventana.");
        }

        [UnityTest]
        public IEnumerator SaltoAplastante_MarcaLaCaida_YQuemaAlAterrizarConOndas()
        {
            var jefe = CrearJefe("CentinelaArdiente");
            SoloPatron(jefe, "SaltoAplastante");
            yield return CrearJugadorEnSuelo(new Vector2(-6f, 0f));
            var vidaJugador = Jugador.Vida.Vida;
            jefe.Activar(Jugador.transform, LimitesArena);

            ZonaDeDanio marca = null;
            yield return EsperarHasta(() => (marca = Object.FindObjectsByType<ZonaDeDanio>().FirstOrDefault(z => z.name.EndsWith("Aterrizaje"))) != null, 3f, "la marca de la caída");
            Assert.AreEqual(Jugador.Motor.Cuerpo.position.x, marca.transform.position.x, 0.5f, "Salta hacia donde está el jugador.");
            Assert.IsFalse(marca.Activa, "Mientras vuela, la marca avisa sin quemar.");

            yield return new WaitForSeconds(0.3f);
            Assert.Greater(jefe.SueloY, 0.5f, "Está en el aire.");
            Assert.AreEqual(vidaJugador, Jugador.Vida.Vida, "En el aire no hace daño.");

            yield return EsperarHasta(() => Object.FindObjectsByType<ZonaDeDanio>().Any(z => z.name.EndsWith("OndaDelSalto")), 2f, "las ondas al aterrizar");
            Assert.AreEqual(-6f, jefe.Centro.x, 0.8f, "Cae donde marcó.");
            yield return EsperarHasta(() => Jugador.Vida.Vida < vidaJugador, 1f, "que la caída queme al jugador que no se apartó");
        }

        [UnityTest]
        public IEnumerator Espadazo_GolpeaAlJugadorDelante_YDejaAlJefeVulnerable()
        {
            var jefe = CrearJefe("CentinelaArdiente");
            SoloPatron(jefe, "Espadazo");
            yield return CrearJugadorEnSuelo(new Vector2(-2.8f, 0f));
            var vidaJugador = Jugador.Vida.Vida;
            jefe.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => jefe.EstadoActual == ESTADOS_JEFE.VULNERABLE, 5f, "la espada clavada");

            Assert.Less(Jugador.Vida.Vida, vidaJugador, "El espadazo alcanza al jugador que está delante.");
            Assert.AreEqual(-1, jefe.Mirando, "Se gira hacia el jugador, que está a su izquierda.");
        }

        [UnityTest]
        public IEnumerator UnGolpe_NoInterrumpeElAtaqueDelJefe()
        {
            var jefe = CrearJefe("CentinelaArdiente");
            SoloPatron(jefe, "Espadazo");
            yield return CrearJugadorEnSuelo(new Vector2(-2.8f, 0f));
            jefe.Activar(Jugador.transform, LimitesArena);
            yield return EsperarHasta(() => jefe.PatronActual != null, 2f, "que empiece el espadazo");
            var patron = jefe.PatronActual;

            jefe.Vida.RecibirGolpe(new Golpe(1, jefe.Centro));
            yield return null;

            Assert.AreEqual(ESTADOS_JEFE.PHASE_1, jefe.EstadoActual, "No hay estado DAMAGED: el jefe sigue en su fase.");
            Assert.AreSame(patron, jefe.PatronActual, "El ataque en curso continúa.");
            yield return EsperarHasta(() => jefe.EstadoActual == ESTADOS_JEFE.VULNERABLE, 3f, "que el espadazo termine igualmente");
        }

        [UnityTest]
        public IEnumerator OndaDeSuelo_RecorreElSuelo_YAlcanzaAlJugador()
        {
            var jefe = CrearJefe("CentinelaArdiente");
            SoloPatron(jefe, "OndaDeSuelo");
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            var vidaJugador = Jugador.Vida.Vida;
            jefe.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => Object.FindObjectsByType<ZonaDeDanio>().Any(z => z.name.EndsWith("OndaDeSuelo")), 3f, "la onda");
            yield return EsperarHasta(() => Jugador.Vida.Vida < vidaJugador, 3f, "que la onda llegue al jugador, a 8 u");
        }

        [UnityTest]
        public IEnumerator PlataformasEnErupcion_BrotanBajoElJugador_ConAvisoPrevio()
        {
            var jefe = CrearJefe("CentinelaArdiente");
            SoloPatron(jefe, "PlataformasEnErupcion");
            yield return CrearJugadorEnSuelo(new Vector2(-6f, 0f));
            var vidaJugador = Jugador.Vida.Vida;
            jefe.Activar(Jugador.transform, LimitesArena);

            ZonaDeDanio primera = null;
            yield return EsperarHasta(() => (primera = Object.FindObjectsByType<ZonaDeDanio>().FirstOrDefault(z => z.name.EndsWith("Erupcion"))) != null, 3f, "la primera erupción");
            Assert.AreEqual(Jugador.Motor.Cuerpo.position.x, primera.transform.position.x, 0.5f, "La primera brota bajo el jugador.");
            Assert.IsFalse(primera.Activa, "Primero avisa, sin hacer daño.");
            Assert.AreEqual(vidaJugador, Jugador.Vida.Vida);

            yield return EsperarHasta(() => Jugador.Vida.Vida < vidaJugador, 2f, "que la erupción queme al jugador quieto");
        }

        // --- Derrota y reinicio ---

        [UnityTest]
        public IEnumerator Derrota_EmiteElEvento_ApagaElDanio_YDesvanece()
        {
            var jefe = CrearJefe("CentinelaArdiente");
            yield return CrearJugadorEnSuelo(new Vector2(-10f, 0f));
            var derrotas = 0;
            jefe.OnJefeDerrotado += _ => derrotas++;
            jefe.Activar(Jugador.transform, LimitesArena);

            jefe.Vida.Matar();
            yield return null;

            Assert.AreEqual(ESTADOS_JEFE.DEFEATED, jefe.EstadoActual);
            Assert.IsTrue(jefe.Derrotado);
            Assert.AreEqual(1, derrotas, "El evento que otorga el fragmento se emite una vez.");
            Assert.IsFalse(Contacto(jefe).gameObject.activeSelf, "Derrotado, ya no hace daño.");
            Assert.IsFalse(jefe.GetComponent<Collider2D>().enabled);
            Assert.IsEmpty(Object.FindObjectsByType<ZonaDeDanio>(), "Sus ataques desaparecen: el suelo se estabiliza.");

            yield return EsperarHasta(() => !jefe.gameObject.activeSelf, jefe.Datos.duracionDerrota + 1f, "que se desvanezca");
            Assert.AreEqual(1, derrotas);
        }

        [UnityTest]
        public IEnumerator Reiniciar_DevuelveAlJefeAlPrincipioDelCombate()
        {
            var jefe = CrearJefe("CentinelaArdiente");
            var inicio = jefe.transform.position;
            yield return CrearJugadorEnSuelo(new Vector2(-10f, 0f));
            jefe.Activar(Jugador.transform, LimitesArena);
            jefe.Vida.RecibirGolpe(new Golpe(14, jefe.Centro));
            yield return new WaitForSeconds(0.3f);

            jefe.Reiniciar();

            Assert.IsFalse(jefe.EnCombate);
            Assert.AreEqual(jefe.Vida.VidaMaxima, jefe.Vida.Vida);
            Assert.AreEqual(inicio.x, jefe.transform.position.x, 0.01f);
            Assert.IsFalse(Contacto(jefe).gameObject.activeSelf);

            var fases = new List<int>();
            jefe.OnFaseCambiada += fases.Add;
            jefe.Activar(Jugador.transform, LimitesArena);
            CollectionAssert.AreEqual(new[] { 1 }, fases, "El combate vuelve a empezar en la fase 1.");
        }

        // --- Esqueletos con mecánica propia ---

        [UnityTest]
        public IEnumerator ElJuezSilente_EsInmuneHastaLaMemoriaCorrecta()
        {
            var juez = (JuezSilente)CrearJefe("JuezSilente");
            yield return CrearJugadorEnSuelo(new Vector2(-10f, 0f));
            juez.Activar(Jugador.transform, LimitesArena);

            juez.Vida.RecibirGolpe(new Golpe(3, juez.Centro));
            Assert.AreEqual(juez.Vida.VidaMaxima, juez.Vida.Vida, "Los ataques directos no le hacen nada.");

            juez.ActivarMemoriaCorrecta();
            yield return null;
            Assert.AreEqual(ESTADOS_JEFE.VULNERABLE, juez.EstadoActual);

            juez.Vida.RecibirGolpe(new Golpe(3, juez.Centro));
            Assert.AreEqual(juez.Vida.VidaMaxima - 6, juez.Vida.Vida);
        }

        [UnityTest]
        public IEnumerator ElVerdaderoSoldado_NoCaeSinLaIntegracion()
        {
            var soldado = (VerdaderoSoldado)CrearJefe("VerdaderoSoldado");
            yield return CrearJugadorEnSuelo(new Vector2(-10f, 0f));
            soldado.Activar(Jugador.transform, LimitesArena);

            soldado.Vida.RecibirGolpe(new Golpe(soldado.Vida.VidaMaxima, soldado.Centro));
            yield return EsperarHasta(() => soldado.EstadoActual == ESTADOS_JEFE.PHASE_3, 1f, "la fase 3");
            Assert.IsTrue(soldado.Vida.EstaVivo, "Quitarle la vida no basta: se queda con 1.");
            Assert.IsFalse(soldado.Derrotado);

            soldado.CompletarIntegracion();
            yield return null;
            Assert.IsTrue(soldado.Derrotado, "El combate termina con la integración, sin violencia.");
        }

        // --- Utilidades ---

        private BossController CrearJefe(string nombre)
        {
            Suelo(-20f, 20f);
            var objeto = Object.Instantiate(CargarPrefab($"{CarpetaJefes}/{nombre}.prefab"), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            Creados.Add(objeto);
            return objeto.GetComponent<BossController>();
        }

        private static Hitbox Contacto(BossController jefe) => jefe.GetComponentInChildren<Hitbox>(true);

        // Copia de los datos del jefe con otras fases, para aislar un comportamiento.
        private void SinPatrones(BossController jefe, params float[] umbrales)
        {
            var datos = Object.Instantiate(jefe.Datos);
            Creados.Add(datos);
            datos.fases = umbrales.Select(u => new FaseJefe { umbralVida = u }).ToList();
            jefe.FijarDatos(datos);
        }

        private void SoloPatron(BossController jefe, string patron)
        {
            var datos = Object.Instantiate(jefe.Datos);
            Creados.Add(datos);
            datos.fases = new List<FaseJefe>
            {
                new FaseJefe
                {
                    umbralVida = 1f,
                    pausaEntrePatrones = 0.3f,
                    patrones = new List<PatronAtaque> { CargarAsset<PatronAtaque>($"{CarpetaPatrones}/{patron}.asset") },
                },
            };
            jefe.FijarDatos(datos);
        }
    }
}
