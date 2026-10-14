using System;
using System.Collections;
using Fragmentum.Core;
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
    /// Las mecánicas del mundo del Miedo (fase B) con sus prefabs, el jugador real y física real:
    ///
    /// - La antorcha se enciende con Interactuar solo al alcance, alumbra su tiempo, parpadea al
    ///   final, no se vuelve a encender y vuelve a estar lista cuando Kael reaparece.
    /// - El cepo solo se ve de cerca o con luz; al pisarlo quita 1, sujeta 1 s (ni andar, ni saltar,
    ///   ni el dash) y suena: lo oye un Centinela Ciego, que no oye caminar. Al reaparecer, se rearma.
    /// - La rama podrida cruje 0,3 s, se parte, vuelve a los 4 s y se delata con luz.
    /// - Los anillos de ruido salen al correr, no al caminar, y llegan hasta donde se oye.
    /// - La luz de Kael se enciende con su radio y no cuenta como luz para los Espectros.
    /// </summary>
    public class MecanicasMiedoPlayModeTests : PruebaPlayModeBase
    {
        private const string CarpetaMecanicas = "Assets/Prefabs/Mecanicas";
        private const string RutaCentinela = "Assets/Prefabs/Enemigos/Enemigo_CentinelaCiego.prefab";

        private const float Margen = 0.1f;

        private T Instanciar<T>(string prefab, Vector2 posicion) where T : Component
        {
            var objeto = Object.Instantiate(CargarPrefab($"{CarpetaMecanicas}/{prefab}.prefab"), new Vector3(posicion.x, posicion.y, 0f), Quaternion.identity);
            Creados.Add(objeto);
            var componente = objeto.GetComponent<T>();
            Assert.IsNotNull(componente, $"El prefab {prefab} no tiene {typeof(T).Name}.");
            return componente;
        }

        private ZonaIluminada Luz(Vector2 posicion)
        {
            var objeto = new GameObject("Luz");
            objeto.transform.position = posicion;
            Creados.Add(objeto);
            return objeto.AddComponent<ZonaIluminada>();
        }

        private static IEnumerator DosFotogramas()
        {
            yield return null;
            yield return null;
        }

        // Como al reintentar tras un Game Over: Kael muere y el LevelManager lo revive en el checkpoint.
        private IEnumerator MorirYReaparecer(Vector2 pies)
        {
            Jugador.Vida.Matar();
            yield return DosFotogramas();
            Jugador.Revivir(pies);
            yield return DosFotogramas();
        }

        // --- Antorcha ---

        [UnityTest]
        public IEnumerator Antorcha_SeEnciendeConE_SoloAlAlcance_AlumbraSuTiempo_YVuelveAlReaparecer()
        {
            Suelo(-12f, 12f);
            var antorcha = Instanciar<Antorcha>("Antorcha", Vector2.zero);
            antorcha.duracion = 2f;
            antorcha.parpadeo = 1f;
            yield return CrearJugadorEnSuelo(new Vector2(-4f, 0f));
            var interaccion = Jugador.GetComponent<InteraccionJugador>();

            var encendidas = 0;
            Action<Antorcha> contar = a => { if (a == antorcha) encendidas++; };
            Antorcha.AlEncenderse += contar;
            try
            {
                Input.PulsarInteractuar();
                yield return DosFotogramas();
                Assert.AreEqual(ESTADOS_ANTORCHA.UNLIT, antorcha.Estado, "Lejos, Interactuar no la enciende.");
                Assert.IsNull(interaccion.Candidato);

                Jugador.Motor.Teletransportar(new Vector2(-1f, 0.05f));
                yield return new WaitForFixedUpdate();
                yield return DosFotogramas();
                Assert.AreSame(antorcha, interaccion.Candidato, "A 1 u, está al alcance.");
                Assert.IsFalse(Iluminacion.EstaIluminado(antorcha.Posicion), "Apagada no alumbra.");

                Input.PulsarInteractuar();
                yield return DosFotogramas();
                var encendidaEn = Time.time;
                Assert.AreEqual(ESTADOS_ANTORCHA.LIT, antorcha.Estado, "Al alcance, la enciende.");
                Assert.AreEqual(1, encendidas, "Y lo anuncia: así despertarán los Espectros.");
                Assert.IsTrue(antorcha.Luz.enabled, "Se ve su luz.");
                Assert.IsTrue(Iluminacion.EstaIluminado(antorcha.Posicion + new Vector2(7.5f, 0f)), "Alumbra 8 u.");
                Assert.IsFalse(Iluminacion.EstaIluminado(antorcha.Posicion + new Vector2(8.5f, 0f)), "Y no más.");
                Assert.IsNull(interaccion.Candidato, "Encendida, ya no hay nada que hacer con ella.");

                yield return EsperarHasta(() => antorcha.Parpadea, 2f, "que parpadee al final");
                Assert.AreEqual(antorcha.duracion - antorcha.parpadeo, Time.time - encendidaEn, Margen, "Parpadea sus últimos segundos.");

                yield return EsperarHasta(() => antorcha.Estado == ESTADOS_ANTORCHA.SPENT, 2f, "que se consuma");
                Assert.AreEqual(antorcha.duracion, Time.time - encendidaEn, Margen);
                Assert.IsFalse(antorcha.Luz.enabled);
                Assert.IsFalse(Iluminacion.EstaIluminado(antorcha.Posicion), "Consumida, ya no alumbra.");

                Input.PulsarInteractuar();
                yield return DosFotogramas();
                Assert.AreEqual(ESTADOS_ANTORCHA.SPENT, antorcha.Estado, "Una vez por vida: no se vuelve a encender.");
                Assert.AreEqual(1, encendidas);

                yield return MorirYReaparecer(new Vector2(-1f, 0.05f));
                Assert.AreEqual(ESTADOS_ANTORCHA.UNLIT, antorcha.Estado, "Al reaparecer tras morir, vuelve a estar lista.");
                Assert.AreSame(antorcha, interaccion.Candidato);
            }
            finally
            {
                Antorcha.AlEncenderse -= contar;
            }
        }

        // --- Cepo ---

        [UnityTest]
        public IEnumerator Cepo_SoloSeVeAUnaUnidadDeKael_OConLuz()
        {
            Suelo(-12f, 12f);
            var cepo = Instanciar<Cepo>("Cepo", Vector2.zero);
            yield return CrearJugadorEnSuelo(new Vector2(-5f, 0f));
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(cepo.SeVe, "A oscuras y lejos, escondido.");
            Assert.AreEqual(0f, cepo.Opacidad, 1e-3f);

            // Kael mide 0,8 u de ancho y el cepo 0,8: con Kael en x −1,7 quedan 0,9 u entre los dos.
            Jugador.Motor.Teletransportar(new Vector2(-1.7f, 0.05f));
            yield return EsperarHasta(() => cepo.SeVe, 0.5f, "que se vea a menos de 1 u de Kael");
            yield return EsperarHasta(() => cepo.Opacidad >= 1f, 0.5f, "que aparezca del todo");
            Assert.AreEqual(ESTADOS_CEPO.ARMED, cepo.Estado, "Verlo no lo cierra.");

            Jugador.Motor.Teletransportar(new Vector2(-2.6f, 0.05f));
            yield return EsperarHasta(() => !cepo.SeVe, 0.5f, "que a 1,8 u vuelva a esconderse");

            Luz(new Vector2(2f, 1f));
            yield return EsperarHasta(() => cepo.SeVe, 0.5f, "que la luz de una antorcha lo enseñe desde lejos");
        }

        [UnityTest]
        public IEnumerator Cepo_AlPisarlo_QuitaUno_LoSujetaUnSegundo_SuenaAOchoUnidades_YSeRearmaAlReaparecer()
        {
            Suelo(-12f, 12f);
            var cepo = Instanciar<Cepo>("Cepo", Vector2.zero);
            yield return CrearJugadorEnSuelo(new Vector2(-3f, 0f));
            var oido = 0f;
            Jugador.GetComponent<PlayerModoMovimiento>().OnRuido += (_, radio) => oido = Mathf.Max(oido, radio);
            var vida = Jugador.Vida.Vida;

            Input.EjeHorizontal = 0.35f;
            yield return EsperarHasta(() => cepo.Estado == ESTADOS_CEPO.CLOSED, 3f, "que Kael lo pise caminando");
            var cerradoEn = Time.time;
            Assert.AreEqual(vida - 1, Jugador.Vida.Vida, "Quita 1.");
            Assert.AreEqual(8f, oido, 1e-4f, "El chasquido se oye a 8 u.");
            Assert.IsTrue(cepo.Sujetando);
            Assert.IsTrue(cepo.SeVe, "Cerrado, se queda a la vista.");

            var x = Jugador.transform.position.x;
            Input.EjeHorizontal = 1f;
            Input.PulsarSalto();
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(x, Jugador.transform.position.x, 0.05f, "Sujeto, no anda.");
            Assert.Less(Jugador.transform.position.y, 0.2f, "Ni salta.");
            Input.SoltarSalto();
            Input.PulsarDash();
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(x, Jugador.transform.position.x, 0.05f, "Ni sale con el dash.");

            yield return EsperarHasta(() => !cepo.Sujetando, 1f, "que lo suelte");
            Assert.AreEqual(1f, Time.time - cerradoEn, Margen, "Lo sujeta 1 s.");
            yield return new WaitForSeconds(0.4f);
            Assert.Greater(Jugador.transform.position.x, x + 0.5f, "Suelto, vuelve a andar.");
            Assert.AreEqual(vida - 1, Jugador.Vida.Vida, "Cerrado ya no hace daño.");

            Input.EjeHorizontal = 0f;
            yield return MorirYReaparecer(new Vector2(-5f, 0.05f));
            Assert.AreEqual(ESTADOS_CEPO.ARMED, cepo.Estado, "Al reaparecer tras morir, vuelve a estar armado…");
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(cepo.SeVe, "…y escondido.");
        }

        [UnityTest]
        public IEnumerator Cepo_SuChasquido_LoOyeUnCentinelaCiego_QueNoOyeCaminar()
        {
            Suelo(-12f, 20f);
            var cepo = Instanciar<Cepo>("Cepo", Vector2.zero);
            yield return CrearJugadorEnSuelo(new Vector2(-3f, 0f));
            Jugador.GetComponent<PlayerModoMovimiento>().EmitirRuido = true;

            var objeto = Object.Instantiate(CargarPrefab(RutaCentinela), new Vector3(6f, 0.05f, 0f), Quaternion.identity);
            Creados.Add(objeto);
            var centinela = objeto.GetComponent<EnemyController>();
            yield return new WaitForSeconds(0.3f);

            Input.EjeHorizontal = 0.35f;
            yield return ComprobarQueNunca(() => centinela.EstadoActual == ESTADOS_ENEMIGO.CHASE, 0.7f, "Caminando, el Centinela no oye a Kael.");
            yield return EsperarHasta(() => cepo.Estado == ESTADOS_CEPO.CLOSED, 3f, "que Kael pise el cepo");
            yield return EsperarHasta(() => centinela.EstadoActual == ESTADOS_ENEMIGO.CHASE, 0.5f, "que el Centinela oiga el chasquido, a 6 u");
        }

        // --- Rama podrida ---

        [UnityTest]
        public IEnumerator RamaPodrida_Cruje_SeParte_VuelveALosCuatroSegundos_YLaLuzLaDelata()
        {
            Suelo(-12f, -1f);
            Suelo(-12f, 12f, yArriba: -4f);
            var rama = Instanciar<RamaPodrida>("RamaPodrida", Vector2.zero);
            var plataforma = rama.Plataforma;
            yield return CrearJugadorEnSuelo(new Vector2(-6f, 0f));
            Assert.IsFalse(rama.Delatada, "A oscuras, como las demás.");

            Jugador.Motor.Teletransportar(new Vector2(0f, 0.05f));
            yield return EsperarHasta(() => plataforma.Estado == ESTADOS_PLATAFORMA.CRACKED, 0.3f, "que cruja al pisarla");
            var pisadaEn = Time.time;
            yield return EsperarHasta(() => plataforma.Caida, 1f, "que se parta");
            Assert.AreEqual(0.3f, Time.time - pisadaEn, Margen, "Cruje 0,3 s.");
            var rotaEn = Time.time;
            yield return new WaitForSeconds(0.5f);
            Assert.Less(Jugador.transform.position.y, -1f, "Kael cae.");

            yield return EsperarHasta(() => plataforma.Estado == ESTADOS_PLATAFORMA.INTACT, 5f, "que vuelva");
            Assert.AreEqual(4f, Time.time - rotaEn, 0.15f, "Vuelve a los 4 s.");

            Luz(new Vector2(2.5f, 0f));
            yield return DosFotogramas();
            Assert.IsTrue(rama.Delatada, "Con la luz de una antorcha, se delata.");
            Assert.AreEqual(rama.ColorPodrida, rama.GetComponentInChildren<SpriteRenderer>().color);
        }

        // --- Anillos de ruido y luz de Kael ---

        [UnityTest]
        public IEnumerator AnillosDeRuido_SalenAlCorrer_NoAlCaminar_YLleganHastaDondeSeOye()
        {
            Suelo(-30f, 30f);
            yield return CrearJugadorEnSuelo(new Vector2(-20f, 0f));
            var modo = Jugador.GetComponent<PlayerModoMovimiento>();
            modo.EmitirRuido = true;
            var anillos = Jugador.GetComponent<AnillosDeRuido>();

            Input.EjeHorizontal = 0.35f;
            yield return ComprobarQueNunca(() => anillos.Activos > 0, 0.8f, "Caminando no hay anillos.");

            Input.EjeHorizontal = 1f;
            yield return EsperarHasta(() => anillos.Activos > 0, 1.5f, "que corriendo salgan anillos");
            Assert.AreEqual(6f, anillos.RadioMayor, 1e-4f, "Llegan hasta donde se oye la carrera.");

            Input.EjeHorizontal = 0f;
            yield return new WaitForSeconds(anillos.Duracion + 0.3f);
            Assert.AreEqual(0, anillos.Activos, "Y se desvanecen.");

            modo.EmitirRuido = false;
            Input.EjeHorizontal = -1f;
            yield return ComprobarQueNunca(() => anillos.Activos > 0, 0.8f, "Sin sigilo en el nivel, correr no hace ruido ni anillos.");
            Input.EjeHorizontal = 0f;
        }

        [UnityTest]
        public IEnumerator LuzDeKael_SeEnciendeConSuRadio_YNoCuentaComoLuzParaLosEspectros()
        {
            Suelo(-12f, 12f);
            yield return CrearJugadorEnSuelo(new Vector2(0f, 0f));
            var luz = Jugador.GetComponent<LuzJugador>();
            Assert.IsFalse(luz.Encendida, "Fuera de un nivel a oscuras, apagada.");

            luz.Encender(3f);
            Assert.IsTrue(luz.Encendida);
            Assert.AreEqual(3f, luz.Radio, 1e-4f);
            Assert.AreEqual(1.5f, luz.Luz.intensity, 1e-4f);
            yield return null;
            Assert.IsFalse(Iluminacion.EstaIluminado(Jugador.transform.position), "Con su luz, Kael ve dónde pisa, pero no está a salvo.");
        }
    }
}
