using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Levels.Mecanicas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// La Doncella del Lamento en una arena plana de 24 u con sus tres franjas (A, B y C, de 8 u):
    /// flota sin caer, solo se la hiere mientras llora, sus olas cruzan la arena a ras del hielo,
    /// inunda la franja de Kael, en la fase 2 congela lo que inunda y llora sobre su hielo o metida
    /// en su agua. Cada prueba usa el prefab real, sus patrones reales y el jugador real.
    /// </summary>
    public class DoncellaPlayModeTests : PruebaPlayModeBase
    {
        private const string RutaPrefab = "Assets/Prefabs/Jefes/DoncellaLamento.prefab";
        private const string CarpetaPatrones = "Assets/Data/Jefes/Patrones";
        private static readonly Vector2 LimitesArena = new Vector2(-12f, 12f);

        private FranjaInundable _a;
        private FranjaInundable _b;
        private FranjaInundable _c;

        [UnityTearDown]
        public IEnumerator BorrarZonasDeDanio()
        {
            foreach (var zona in Object.FindObjectsByType<ZonaDeDanio>(FindObjectsInactive.Include))
            {
                Object.Destroy(zona.gameObject);
            }

            yield return null;
        }

        // --- Flotar y la ventana ---

        [UnityTest]
        public IEnumerator AlEmpezar_SubeYFlota_YTocarlaNoHaceDanio()
        {
            var doncella = CrearDoncella();
            SinPatrones(doncella, 1f);
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            Assert.AreEqual(doncella.Suelo, doncella.Pies.y, 0.01f, "Antes del combate espera en el hielo.");

            doncella.Activar(Jugador.transform, LimitesArena);
            yield return EsperarHasta(() => Mathf.Abs(doncella.Pies.y - Altura(doncella)) < 0.05f, 2f, "que suba a su altura de vuelo");
            Assert.AreEqual(RigidbodyType2D.Kinematic, doncella.GetComponent<Rigidbody2D>().bodyType, "Flota: ni cae ni choca.");

            // Va de lado a lado sin perder altura ni salirse de la arena.
            var minimo = float.MaxValue;
            var maximo = float.MinValue;
            yield return Durante(1.5f, () =>
            {
                minimo = Mathf.Min(minimo, doncella.Pies.x);
                maximo = Mathf.Max(maximo, doncella.Pies.x);
                Assert.AreEqual(Altura(doncella), doncella.Pies.y, 0.05f, "No cae.");
            });
            Assert.Greater(maximo - minimo, 1f, "Se mueve de lado a lado.");
            Assert.GreaterOrEqual(minimo, LimitesArena.x);
            Assert.LessOrEqual(maximo, LimitesArena.y);

            // Kael debajo de ella, y después a su misma altura: su cuerpo no hiere.
            var vida = Jugador.Vida.Vida;
            Jugador.Revivir(new Vector2(doncella.Pies.x, doncella.Pies.y + 0.5f));
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(vida, Jugador.Vida.Vida, "Tocarla no hace daño.");
        }

        [UnityTest]
        public IEnumerator SoloSeLaHiereMientrasLlora_YEntoncesCadaGolpeValeElDoble()
        {
            var doncella = CrearDoncella();
            SinPatrones(doncella, 1f);
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            doncella.Activar(Jugador.transform, LimitesArena);
            var bloqueados = 0;
            doncella.OnGolpeEnElHielo += () => bloqueados++;
            var llantos = new List<bool>();
            doncella.OnLlanto += llantos.Add;

            doncella.Vida.RecibirGolpe(new Golpe(3, doncella.Centro));
            Assert.AreEqual(doncella.Vida.VidaMaxima, doncella.Vida.Vida, "Fuera del llanto, los golpes no le hacen nada.");
            Assert.AreEqual(1, bloqueados, "Y suenan a hielo.");

            doncella.Llorar(0.5f);
            yield return null;
            yield return null;
            Assert.AreEqual(ESTADOS_JEFE.VULNERABLE, doncella.EstadoActual);
            Assert.IsTrue(doncella.Llorando);
            var lugar = doncella.Pies;

            doncella.Vida.RecibirGolpe(new Golpe(1, doncella.Centro));
            Assert.AreEqual(doncella.Vida.VidaMaxima - 2, doncella.Vida.Vida, "Llorando, cada golpe vale el doble.");
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(lugar.x, doncella.Pies.x, 0.02f, "Mientras llora no se mueve.");
            Assert.AreEqual(lugar.y, doncella.Pies.y, 0.02f);

            yield return EsperarHasta(() => doncella.EstadoActual != ESTADOS_JEFE.VULNERABLE, 1f, "que deje de llorar");
            Assert.IsFalse(doncella.Llorando);
            CollectionAssert.AreEqual(new[] { true, false }, llantos);
            doncella.Vida.RecibirGolpe(new Golpe(3, doncella.Centro));
            Assert.AreEqual(doncella.Vida.VidaMaxima - 2, doncella.Vida.Vida, "Y vuelve a ser inmune.");
        }

        // --- Fase 1 ---

        [UnityTest]
        public IEnumerator LaOla_NaceEnLaParedDeSuEspalda_YCruzaLaArenaARasDelHielo()
        {
            var doncella = CrearDoncella();
            var ola = Patron<PatronOla>("Ola");
            SoloPatrones(doncella, ola);
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            var vida = Jugador.Vida.Vida;
            doncella.Activar(Jugador.transform, LimitesArena);

            ZonaDeDanio zona = null;
            yield return EsperarHasta(() => (zona = Olas().FirstOrDefault()) != null, 3f, "la ola");
            Assert.AreEqual(-1, doncella.Mirando, "Mira a Kael, que está a su izquierda.");
            Assert.AreEqual(LimitesArena.y - ola.tamanoOla.x * 0.5f, zona.transform.position.x, 0.05f, "Nace en la pared que tiene a la espalda.");
            Assert.AreEqual(doncella.Suelo + ola.tamanoOla.y * 0.5f, zona.transform.position.y, 0.05f, "A ras del hielo, no a su altura.");
            Assert.IsFalse(zona.Activa, "Primero avisa, sin hacer daño.");
            Assert.IsTrue(zona.GetComponent<SpriteRenderer>().enabled, "Sin arte todavía, se ve como un rectángulo.");

            yield return EsperarHasta(() => zona.Activa, 1.5f, "que la ola salga");
            var salida = zona.transform.position.x;
            yield return EsperarHasta(() => Jugador.Vida.Vida < vida, 4f, "que la ola llegue a Kael, a 19 u, cruzando bajo ella");
            Assert.Less(zona.transform.position.x, salida - 15f, "Ha cruzado la arena.");
        }

        [UnityTest]
        public IEnumerator LaOlaDoble_SonDosOlasSeguidas()
        {
            var doncella = CrearDoncella();
            SoloPatrones(doncella, Patron<PatronOla>("OlaDoble"));
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            doncella.Activar(Jugador.transform, LimitesArena);

            var vistas = new HashSet<ZonaDeDanio>();
            yield return EsperarHasta(() => Olas().Any(), 3f, "la primera ola");
            yield return Durante(1.2f, () => vistas.UnionWith(Olas()));
            Assert.AreEqual(2, vistas.Count, "Dos olas, una detrás de otra.");
        }

        [UnityTest]
        public IEnumerator LaInundacion_AvisaEnLaFranjaDeKael_YDespuesElAguaLoMojaSinHerirlo()
        {
            var doncella = CrearDoncella();
            SoloPatrones(doncella, Patron<PatronInundacion>("Inundacion", p => p.duracion = 1.5f), Patron<PatronLlanto>("Llanto"));
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            var vida = Jugador.Vida.Vida;
            doncella.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => _a.Estado == ESTADOS_FRANJA.WARNING, 3f, "el aviso en la franja de Kael");
            Assert.AreEqual(ESTADOS_FRANJA.DRY, _b.Estado, "Solo la franja de Kael.");
            Assert.AreEqual(ESTADOS_FRANJA.DRY, _c.Estado);
            Assert.IsFalse(Jugador.Motor.BajoElAgua, "El aviso no moja.");

            yield return EsperarHasta(() => Jugador.Motor.BajoElAgua, 2f, "que el agua suba");
            Assert.AreEqual(ESTADOS_FRANJA.FLOODED, _a.Estado);

            // Llora en seco: no baja a la franja mojada, aunque Kael esté en ella.
            yield return EsperarHasta(() => doncella.EstadoActual == ESTADOS_JEFE.VULNERABLE, 4f, "el llanto");
            Assert.AreEqual(doncella.Suelo, doncella.Pies.y, 0.1f, "En la fase 1 llora en el hielo.");
            Assert.Greater(doncella.Pies.x - doncella.MedioAncho, _a.Hasta, "Fuera del agua: a pie no se llega a lo mojado.");

            yield return EsperarHasta(() => _a.Estado == ESTADOS_FRANJA.DRY, 4f, "que el agua se vaya");
            Assert.AreEqual(vida, Jugador.Vida.Vida, "El agua no hace daño.");
        }

        [UnityTest]
        public IEnumerator ElLlanto_BajaAlHieloATiroDeEspada_YDespuesVuelveASubir()
        {
            var doncella = CrearDoncella();
            // Tras el llanto, una ola: mientras la lanza se queda en el aire, a su altura.
            SoloPatrones(doncella, Patron<PatronLlanto>("Llanto"), Patron<PatronOla>("Ola"));
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            Assert.AreEqual(1, Jugador.Mirando, "Kael mira hacia ella.");
            doncella.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => doncella.EstadoActual == ESTADOS_JEFE.VULNERABLE, 5f, "el llanto");
            Assert.IsTrue(doncella.Llorando);
            Assert.AreEqual(doncella.Suelo, doncella.Pies.y, 0.1f, "Baja al hielo.");
            Assert.AreEqual(PiesJugador.x + doncella.DistanciaAKael, doncella.Pies.x, 0.15f, "Al lado de Kael, por donde ella estaba.");

            // La espada de verdad, sin moverse del sitio.
            var espada = Jugador.transform.Find("Pivote/Espada").GetComponent<Hitbox>();
            espada.gameObject.SetActive(true);
            for (var i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            espada.gameObject.SetActive(false);
            Assert.AreEqual(doncella.Vida.VidaMaxima - 2 * espada.Danio, doncella.Vida.Vida, "La espada la alcanza, y vale el doble.");

            yield return EsperarHasta(() => doncella.EstadoActual == ESTADOS_JEFE.PHASE_1, 2.5f, "que deje de llorar");
            yield return EsperarHasta(() => Mathf.Abs(doncella.Pies.y - Altura(doncella)) < 0.05f, 2f, "que vuelva a subir");
        }

        // --- Fase 2 ---

        [UnityTest]
        public IEnumerator EnLaFase2_CongelaLoQueInunda_YLloraSobreLaCapa()
        {
            var doncella = CrearDoncella();
            DirectaALaFase2(doncella, Patron<PatronInundacion>("Inundacion"), Patron<PatronCongelar>("Congelar"), Patron<PatronLlanto>("LlantoSobreElAgua"));
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            var vida = Jugador.Vida.Vida;
            doncella.Activar(Jugador.transform, LimitesArena);
            yield return EsperarHasta(() => doncella.Fase == 2, 1f, "la fase 2");

            yield return EsperarHasta(() => _a.Estado == ESTADOS_FRANJA.FLOODED, 4f, "que inunde la franja de Kael");
            yield return EsperarHasta(() => _a.Estado == ESTADOS_FRANJA.FREEZING, 3f, "que el agua se ponga blanca");
            Assert.AreEqual(vida, Jugador.Vida.Vida, "El aviso no hace daño.");
            yield return EsperarHasta(() => _a.Congelada, 1.5f, "que se congele");
            yield return EsperarHasta(() => Jugador.Vida.Vida < vida, 0.5f, "que las púas hieran a Kael, que no salió del agua");

            yield return EsperarHasta(() => doncella.EstadoActual == ESTADOS_JEFE.VULNERABLE, 4f, "el llanto");
            Assert.IsTrue(_a.Congelada, "La capa de hielo sigue ahí cuando llora.");
            Assert.AreEqual(_a.Techo + doncella.AlturaSobreLaCapa, doncella.Pies.y, 0.1f, "Llora flotando bajo sobre lo que acaba de congelar.");
            Assert.GreaterOrEqual(doncella.Pies.x - doncella.MedioAncho, _a.Desde, "Dentro de la franja.");
            Assert.LessOrEqual(doncella.Pies.x + doncella.MedioAncho, _a.Hasta);

            // De pie en la capa, junto a ella y mirándola, la espada la alcanza.
            var espada = Jugador.transform.Find("Pivote/Espada").GetComponent<Hitbox>();
            var miraALaDerecha = espada.transform.position.x >= Jugador.transform.position.x;
            var x = Mathf.Clamp(doncella.Pies.x + (miraALaDerecha ? -2f : 2f), _a.Desde + 0.6f, _a.Hasta - 0.6f);
            var cuerpo = Jugador.Motor.Cuerpo;
            cuerpo.position = new Vector2(x, _a.Techo + 0.05f);
            cuerpo.linearVelocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
            yield return EsperarHasta(() => Jugador.Sensores.EnSuelo, 1f, "que Kael se apoye en la capa");
            Assert.AreEqual(_a.Techo, PiesJugador.y, 0.1f);
            var antes = doncella.Vida.Vida;
            espada.gameObject.SetActive(true);
            for (var i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            espada.gameObject.SetActive(false);
            Assert.Less(doncella.Vida.Vida, antes, "Subido a su hielo, se la hiere.");
        }

        [UnityTest]
        public IEnumerator EnLaFase2_SinNadaCongelado_LloraMetidaEnSuAgua()
        {
            var doncella = CrearDoncella();
            DirectaALaFase2(doncella, Patron<PatronInundacion>("Inundacion"), Patron<PatronLlanto>("LlantoSobreElAgua"));
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            doncella.Activar(Jugador.transform, LimitesArena);

            yield return EsperarHasta(() => doncella.EstadoActual == ESTADOS_JEFE.VULNERABLE, 8f, "el llanto");
            Assert.IsTrue(_a.Inundada, "El agua sigue ahí cuando llora.");
            Assert.AreEqual(_a.Superficie - doncella.HundimientoAlLlorar, doncella.Pies.y, 0.1f, "Llora con los pies metidos en el agua.");
            Assert.GreaterOrEqual(doncella.Pies.x - doncella.MedioAncho, _a.Desde, "Sobre la franja inundada.");
            Assert.LessOrEqual(doncella.Pies.x + doncella.MedioAncho, _a.Hasta);
        }

        [UnityTest]
        public IEnumerator AlLlegarALaMitadDeVida_Grita_YEmpiezaLaFase2()
        {
            var doncella = CrearDoncella();
            SinPatrones(doncella, 1f, 0.5f);
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            var fases = new List<int>();
            doncella.OnFaseCambiada += fases.Add;
            doncella.Activar(Jugador.transform, LimitesArena);
            yield return new WaitForSeconds(1.2f);

            // Solo se la hiere llorando: la mitad de la vida se va en una ventana.
            doncella.Llorar(0.3f);
            yield return null;
            yield return null;
            doncella.Vida.RecibirGolpe(new Golpe(Mathf.CeilToInt(doncella.Vida.VidaMaxima / 4f), doncella.Centro));
            yield return EsperarHasta(() => doncella.EstadoActual == ESTADOS_JEFE.PHASE_2, 1f, "la fase 2 al cerrarse la ventana");
            CollectionAssert.AreEqual(new[] { 1, 2 }, fases);

            // Grita quieta antes de seguir.
            var x = doncella.Pies.x;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(x, doncella.Pies.x, 0.02f, "Mientras grita no va de lado a lado.");
        }

        // --- Reinicio y derrota ---

        [UnityTest]
        public IEnumerator Reiniciar_SecaLaArena_YLaDevuelveAlHielo()
        {
            var doncella = CrearDoncella();
            SoloPatrones(doncella, Patron<PatronInundacion>("Inundacion"));
            var inicio = doncella.transform.position;
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            doncella.Activar(Jugador.transform, LimitesArena);
            yield return EsperarHasta(() => _a.Estado == ESTADOS_FRANJA.FLOODED, 4f, "que inunde");

            doncella.Reiniciar();
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(doncella.EnCombate);
            Assert.AreEqual(ESTADOS_FRANJA.DRY, _a.Estado, "La arena vuelve a estar seca.");
            Assert.IsFalse(Jugador.Motor.BajoElAgua);
            Assert.AreEqual(inicio.x, doncella.Pies.x, 0.01f);
            Assert.AreEqual(inicio.y, doncella.Pies.y, 0.01f, "Y ella, en el hielo.");
            yield return ComprobarQueNunca(() => Mathf.Abs(doncella.Pies.y - inicio.y) > 0.01f, 0.3f, "Fuera del combate no flota.");
        }

        [UnityTest]
        public IEnumerator AlCaer_ElAguaSeVa()
        {
            var doncella = CrearDoncella();
            SoloPatrones(doncella, Patron<PatronInundacion>("Inundacion"));
            yield return CrearJugadorEnSuelo(new Vector2(-8f, 0f));
            doncella.Activar(Jugador.transform, LimitesArena);
            yield return EsperarHasta(() => _a.Estado == ESTADOS_FRANJA.FLOODED, 4f, "que inunde");

            doncella.Vida.Matar();
            yield return null;
            yield return null;

            Assert.IsTrue(doncella.Derrotado);
            Assert.AreEqual(ESTADOS_FRANJA.DRY, _a.Estado, "Al caer ella, el agua se va.");
            Assert.IsFalse(doncella.Llorando);
        }

        // --- Utilidades ---

        private DoncellaLamento CrearDoncella()
        {
            Suelo(-20f, 20f);
            var objeto = Object.Instantiate(CargarPrefab(RutaPrefab), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            Creados.Add(objeto);
            var doncella = objeto.GetComponent<DoncellaLamento>();
            Assert.IsNotNull(doncella, "El prefab lleva su controlador.");

            _a = Franja("FranjaA", -12f, -4f);
            _b = Franja("FranjaB", -4f, 4f);
            _c = Franja("FranjaC", 4f, 12f);
            doncella.FijarFranjas(_a, _b, _c);
            return doncella;
        }

        private FranjaInundable Franja(string nombre, float desde, float hasta)
        {
            var franja = FranjaInundable.Crear(nombre, desde, hasta, 0f);
            Creados.Add(franja.gameObject);
            return franja;
        }

        private static float Altura(DoncellaLamento doncella) => doncella.Suelo + doncella.AlturaDeVuelo;

        private static IEnumerable<ZonaDeDanio> Olas() => Object.FindObjectsByType<ZonaDeDanio>().Where(z => z.name.EndsWith("Ola"));

        // Una copia del patrón real, para ajustarlo sin tocar el asset.
        private T Patron<T>(string nombre, Action<T> ajustar = null) where T : PatronAtaque
        {
            var copia = Object.Instantiate(CargarAsset<T>($"{CarpetaPatrones}/{nombre}.asset"));
            copia.name = nombre;
            Creados.Add(copia);
            ajustar?.Invoke(copia);
            return copia;
        }

        private void SinPatrones(BossController jefe, params float[] umbrales)
        {
            var datos = Object.Instantiate(jefe.Datos);
            Creados.Add(datos);
            datos.fases = umbrales.Select(u => new FaseJefe { umbralVida = u }).ToList();
            jefe.FijarDatos(datos);
        }

        private void SoloPatrones(BossController jefe, params PatronAtaque[] patrones)
        {
            var datos = Object.Instantiate(jefe.Datos);
            Creados.Add(datos);
            datos.fases = new List<FaseJefe>
            {
                new FaseJefe { umbralVida = 1f, pausaEntrePatrones = 0.3f, patrones = patrones.ToList() },
            };
            jefe.FijarDatos(datos);
        }

        // Dos fases con el mismo umbral: pasa a la segunda nada más empezar, sin tener que herirla.
        private void DirectaALaFase2(BossController jefe, params PatronAtaque[] patrones)
        {
            var datos = Object.Instantiate(jefe.Datos);
            Creados.Add(datos);
            datos.fases = new List<FaseJefe>
            {
                new FaseJefe { umbralVida = 1f },
                new FaseJefe { umbralVida = 1f, pausaEntrePatrones = 0.3f, patrones = patrones.ToList() },
            };
            jefe.FijarDatos(datos);
        }
    }
}
