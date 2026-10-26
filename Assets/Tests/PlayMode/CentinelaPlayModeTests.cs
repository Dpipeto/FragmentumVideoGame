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
    /// La Centinela Ardiente combatiendo con su arte, con el prefab real, el jugador real y física
    /// real (criterios del encargo del arte):
    /// - el daño sale cuando el dibujo golpea: el tajo en E2, las ondas en G3, el salto en J2;
    /// - la ventana vulnerable es C2 y dura lo que se ve (también en la furia), y E3 es una
    ///   ventana corta;
    /// - los espadazos encadenados acortan E1 y no los demás fotogramas;
    /// - J4 se sostiene hasta tocar el suelo y J5 llega con el aterrizaje;
    /// - al caer se queda en D4 y la recompensa espera a que Kael recoja el fragmento.
    /// </summary>
    public class CentinelaPlayModeTests : PruebaPlayModeBase
    {
        private const string RutaPrefab = "Assets/Prefabs/Jefes/CentinelaArdiente.prefab";
        private const string CarpetaPatrones = "Assets/Data/Jefes/Patrones";
        private static readonly Vector2 LimitesArena = new Vector2(-15f, 15f);

        private CentinelaArdiente _jefe;
        private SpriteRenderer _sprite;
        private AnimacionJefe _arte;

        private string Fotograma => _sprite.sprite != null ? _sprite.sprite.name : string.Empty;

        [UnityTearDown]
        public IEnumerator BorrarZonasDeDanio()
        {
            foreach (var zona in Object.FindObjectsByType<ZonaDeDanio>(FindObjectsInactive.Include))
            {
                Object.Destroy(zona.gameObject);
            }

            yield return null;
        }

        private IEnumerator Preparar(float xJugador, string patron = null)
        {
            Suelo(-20f, 20f);
            var objeto = Object.Instantiate(CargarPrefab(RutaPrefab), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            Creados.Add(objeto);
            _jefe = objeto.GetComponent<CentinelaArdiente>();
            _arte = objeto.GetComponentInChildren<AnimacionJefe>();
            _sprite = _arte.GetComponent<SpriteRenderer>();
            if (patron != null)
            {
                SoloPatron(patron);
            }

            yield return CrearJugadorEnSuelo(new Vector2(xJugador, 0f));
            Assert.IsTrue(_jefe.TieneArte, "El prefab de la Centinela tiene su arte.");
            _jefe.Activar(Jugador.transform, LimitesArena);
        }

        private void SoloPatron(string patron)
        {
            var datos = Object.Instantiate(_jefe.Datos);
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
            _jefe.FijarDatos(datos);
        }

        private static bool HayZona(string nombre) => Object.FindObjectsByType<ZonaDeDanio>().Any(z => z.name.EndsWith(nombre));

        /// <summary>Fotogramas en orden de aparición, con el instante en que aparecen.</summary>
        private sealed class Registro
        {
            public readonly List<(string fotograma, float inicio)> Fotogramas = new List<(string, float)>();

            public void Anotar(string fotograma)
            {
                if (Fotogramas.Count == 0 || Fotogramas[Fotogramas.Count - 1].fotograma != fotograma)
                {
                    Fotogramas.Add((fotograma, Time.time));
                }
            }

            /// <summary>Cuánto se vio cada aparición de un fotograma.</summary>
            public List<float> Duraciones(string fotograma) =>
                Enumerable.Range(0, Fotogramas.Count - 1).Where(i => Fotogramas[i].fotograma == fotograma)
                    .Select(i => Fotogramas[i + 1].inicio - Fotogramas[i].inicio).ToList();
        }

        // --- El espadazo y la espada clavada ---

        [UnityTest]
        public IEnumerator ElTajo_SoloHaceDanioMientrasSeVeE2_YLaVentanaEsC2()
        {
            yield return Preparar(-2.8f, "Espadazo");
            var conZona = new List<string>();
            var vulnerable = new List<string>();
            float inicioVentana = -1f, finVentana = -1f;

            yield return Durante(4.5f, () =>
            {
                if (HayZona("Espadazo")) conZona.Add(Fotograma);
                if (_jefe.EsVulnerable)
                {
                    vulnerable.Add(Fotograma);
                    if (inicioVentana < 0f) inicioVentana = Time.time;
                }
                else if (inicioVentana > 0f && finVentana < 0f)
                {
                    finVentana = Time.time;
                }
            });

            Assert.IsNotEmpty(conZona, "El espadazo hace daño.");
            Assert.Greater(conZona.Count(f => f == "E2"), conZona.Count(f => f != "E2"), $"La zona del tajo va con E2: {string.Join(",", conZona)}.");
            CollectionAssert.IsSubsetOf(conZona.Distinct(), new[] { "E2", "E3", "C1" }, "Como mucho un fotograma de retraso al apagarse.");

            Assert.Greater(finVentana, inicioVentana, "La espada se clava y la ventana se cierra.");
            Assert.AreEqual(1.2f, finVentana - inicioVentana, 0.1f, "La ventana dura lo que se ve C2.");
            Assert.Greater(vulnerable.Count(f => f == "C2"), vulnerable.Count * 0.9f, $"Vulnerable mientras se ve C2: {string.Join(",", vulnerable.Distinct())}.");
        }

        [UnityTest]
        public IEnumerator EnLaFuria_LosEspadazosEncadenadosAcortanE1_YLaVentanaSigueSiendoC2()
        {
            yield return Preparar(-2.8f, "EspadazoFuria");
            var registro = new Registro();
            float inicioVentana = -1f, finVentana = -1f;
            yield return Durante(4.5f, () =>
            {
                registro.Anotar(Fotograma);
                if (_jefe.EsVulnerable && inicioVentana < 0f) inicioVentana = Time.time;
                if (!_jefe.EsVulnerable && inicioVentana > 0f && finVentana < 0f) finVentana = Time.time;
            });

            // Furia: 0,45 s de aviso en el primero y 0,3 s en los encadenados; E2 sigue en 0,08 s.
            var e1 = registro.Duraciones("E1");
            Assert.GreaterOrEqual(e1.Count, 3, string.Join(",", registro.Fotogramas.Select(f => f.fotograma)));
            Assert.AreEqual(0.45f, e1[0], 0.06f, "El primero.");
            Assert.AreEqual(0.30f, e1[1], 0.06f, "El segundo, encadenado.");
            Assert.AreEqual(0.30f, e1[2], 0.06f, "El tercero, encadenado.");
            foreach (var e2 in registro.Duraciones("E2").Take(3))
            {
                Assert.AreEqual(0.08f, e2, 0.05f, "El tajo no cambia.");
            }

            Assert.AreEqual(1.2f, finVentana - inicioVentana, 0.1f, "La ventana dura lo que C2 aunque el patrón diga 0,9 s.");
        }

        [UnityTest]
        public IEnumerator LaRecuperacionE3_EsUnaVentanaCorta_DeDanioDoble()
        {
            yield return Preparar(-2.8f, "Espadazo");
            yield return EsperarHasta(() => Fotograma == "E1", 2f, "el primer espadazo");
            var antes = _jefe.Vida.Vida;
            _jefe.Vida.RecibirGolpe(new Golpe(1, _jefe.Centro));
            Assert.AreEqual(antes - 1, _jefe.Vida.Vida, "En la anticipación, daño normal.");

            yield return EsperarHasta(() => Fotograma == "E3", 1f, "la recuperación del primer espadazo (E3)");
            antes = _jefe.Vida.Vida;
            _jefe.Vida.RecibirGolpe(new Golpe(1, _jefe.Centro));
            Assert.AreEqual(antes - 2, _jefe.Vida.Vida, "En E3, daño doble.");
        }

        // --- Golpe al suelo, erupciones y salto ---

        [UnityTest]
        public IEnumerator LasOndas_SalenConG3_YEnLaOndaDobleSeVeG3EntreLosGolpes()
        {
            yield return Preparar(-8f, "OndaDoble");
            var registro = new Registro();
            string alSalir = null;
            yield return Durante(2.5f, () =>
            {
                registro.Anotar(Fotograma);
                if (alSalir == null && HayZona("OndaDeSuelo")) alSalir = Fotograma;
            });

            Assert.AreEqual("G3", alSalir, "La primera onda sale con G3, la onda dibujada en el suelo.");
            var secuencia = registro.Fotogramas.Select(f => f.fotograma).SkipWhile(f => f != "G1").ToList();
            CollectionAssert.AreEqual(new[] { "G1", "G2", "G3", "G1", "G2", "G3" }, secuencia.Take(6), "Entre los dos golpes se ve la onda.");
        }

        [UnityTest]
        public IEnumerator LasErupciones_EmpiezanConElGolpeAlSuelo()
        {
            yield return Preparar(-6f, "PlataformasEnErupcion");
            string alBrotar = null;
            var vistos = new HashSet<string>();
            yield return Durante(2f, () =>
            {
                vistos.Add(Fotograma);
                if (alBrotar == null && HayZona("Erupcion")) alBrotar = Fotograma;
            });

            Assert.IsTrue(vistos.Contains("G1") && vistos.Contains("G2"), "Golpea el suelo.");
            Assert.AreEqual("G3", alBrotar, "La lava brota con G3.");
        }

        [UnityTest]
        public IEnumerator ElSalto_DespegaConJ2_SostieneJ4HastaElSuelo_YJ5LlegaAlAterrizar()
        {
            yield return Preparar(-6f, "SaltoAplastante");
            var registro = new Registro();
            var j5EnElAire = false;
            float despegue = -1f;
            string conOndas = null;
            yield return Durante(3f, () =>
            {
                registro.Anotar(Fotograma);
                if (despegue < 0f && _jefe.SueloY > 0.2f) despegue = Time.time;
                if (Fotograma == "J5" && _jefe.SueloY > 0.3f) j5EnElAire = true;
                if (conOndas == null && HayZona("OndaDelSalto")) conOndas = Fotograma;
            });

            var secuencia = registro.Fotogramas.Select(f => f.fotograma).SkipWhile(f => f != "J1").Take(5).ToArray();
            CollectionAssert.AreEqual(new[] { "J1", "J2", "J3", "J4", "J5" }, secuencia);
            var j2 = registro.Fotogramas.First(f => f.fotograma == "J2").inicio;
            Assert.AreEqual(j2, despegue, 0.1f, "Despega con J2.");
            Assert.AreEqual(0.4f, registro.Duraciones("J4")[0], 0.1f, "J4 se ve desde lo alto del salto hasta el suelo (0,8 s de vuelo − J2 − J3).");
            Assert.IsFalse(j5EnElAire, "J5 no llega antes de tocar el suelo.");
            Assert.AreEqual("J5", conOndas, "Las ondas salen al aterrizar, con J5.");
        }

        // --- Rugido, golpes recibidos y latido ---

        [UnityTest]
        public IEnumerator AlEmpezarElCombate_Ruge_YDespuesAtaca()
        {
            // Tras su presentación (el ojo que se abre), la Centinela se levanta rugiendo.
            yield return Preparar(-2.8f, "Espadazo");
            yield return null;
            yield return null;
            Assert.AreEqual("R1", Fotograma, "Al empezar el combate ruge.");
            var registro = new Registro();
            yield return Durante(1.6f, () => registro.Anotar(Fotograma));
            var primerAtaque = registro.Fotogramas.FindIndex(f => f.fotograma == "E1");
            Assert.Greater(primerAtaque, 0, "Después del rugido, el primer espadazo.");
            var antes = registro.Fotogramas.Take(primerAtaque).Select(f => f.fotograma).ToList();
            CollectionAssert.IsSubsetOf(new[] { "R1", "R2", "R3" }, antes, "El rugido entero.");
            Assert.IsTrue(antes.All(f => f.StartsWith("R") || f.StartsWith("I") || f.StartsWith("A")),
                "Antes de atacar, solo el rugido (y el idle o el andar). " + string.Join(", ", antes));
        }

        [UnityTest]
        public IEnumerator AlCambiarDeFase_Ruge()
        {
            yield return Preparar(-10f);
            yield return null;
            _jefe.Vida.RecibirGolpe(new Golpe(_jefe.Vida.VidaMaxima / 2, _jefe.Centro));
            yield return EsperarHasta(() => _jefe.Fase == 2, 1f, "la fase 2");
            yield return null;
            yield return null;
            Assert.AreEqual("R1", Fotograma, "Al entrar en la fase 2 ruge.");
        }

        [UnityTest]
        public IEnumerator UnGolpeRecibido_PoneLaSiluetaBlanca_UnMomento()
        {
            yield return Preparar(-10f);
            _jefe.Vida.RecibirGolpe(new Golpe(1, _jefe.Centro));
            Assert.IsTrue(_arte.DestelloActivo, "Silueta blanca: el tinte blanco no se vería sobre el arte.");
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(_arte.DestelloActivo, "Dura 0,1 s.");
        }

        [UnityTest]
        public IEnumerator ConLaEspadaClavada_LateEnContrafaseConElForcejeo()
        {
            yield return Preparar(-2.8f, "Espadazo");
            yield return EsperarHasta(() => _jefe.EsVulnerable, 4f, "la espada clavada");
            var arriba = new List<float>();
            var abajo = new List<float>();
            yield return Durante(1f, () =>
            {
                if (!_jefe.EsVulnerable || Fotograma != "C2") return;
                (_arte.Elevado ? arriba : abajo).Add(_sprite.color.b);
            });

            Assert.IsNotEmpty(arriba, "El sprite sube en el forcejeo.");
            Assert.IsNotEmpty(abajo);
            Assert.Greater(arriba.Average(), abajo.Average() + 0.2f, "Abajo brilla (brasa) y arriba se apaga: van en contrafase.");
        }

        // --- La muerte y el fragmento ---

        [UnityTest]
        public IEnumerator AlCaer_SeQuedaEnD4_YLaRecompensaEsperaAQueKaelRecojaElFragmento()
        {
            yield return Preparar(-10f);
            var recogidos = 0;
            _jefe.OnFragmentoRecogido += _ => recogidos++;
            _jefe.Vida.Matar();
            yield return null;
            Assert.IsTrue(_jefe.Derrotado);
            Assert.IsFalse(_jefe.Fragmento.Disponible, "El fragmento aún no está: la muerte tarda 2,1 s en llegar a D4.");

            var registro = new Registro();
            yield return Durante(2.6f, () => registro.Anotar(Fotograma));
            CollectionAssert.AreEqual(new[] { "D1", "D2", "D3", "D4" }, registro.Fotogramas.Select(f => f.fotograma).SkipWhile(f => f != "D1"));
            Assert.IsTrue(_jefe.gameObject.activeSelf, "No desaparece.");
            Assert.AreEqual(1f, _sprite.color.a, 1e-4f, "Ni se desvanece.");
            Assert.IsTrue(_jefe.Fragmento.Disponible, "En D4 aparece el fragmento.");
            Assert.AreEqual(0, recogidos);

            yield return new WaitForSeconds(1f);
            Assert.AreEqual("D4", Fotograma, "La ceniza se queda.");

            // Kael entra en la ceniza: lo recoge sin saltar.
            Jugador.Revivir(new Vector2(_jefe.Fragmento.Centro.x, 0.05f));
            for (var i = 0; i < 6; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.AreEqual(1, recogidos, "Al tocarlo, el jefe avisa una vez: la arena otorga la recompensa.");
            Assert.IsTrue(_jefe.FragmentoRecogido);
            Assert.IsTrue(_jefe.Fragmento.GetComponentInChildren<SpriteMask>().enabled, "La máscara borra el fragmento del dibujo...");
            Assert.AreEqual(SpriteMaskInteraction.VisibleOutsideMask, _sprite.maskInteraction);
            Assert.AreEqual("D4", Fotograma, "...y la ceniza se queda.");
        }

        [UnityTest]
        public IEnumerator Reiniciar_VuelveAlIdle()
        {
            yield return Preparar(-2.8f, "Espadazo");
            yield return EsperarHasta(() => Fotograma == "E1", 2f, "el espadazo");
            _jefe.Reiniciar();
            yield return null;
            Assert.AreEqual("I1", Fotograma);
            Assert.IsFalse(_arte.SosteniendoCaida);
        }
    }
}
