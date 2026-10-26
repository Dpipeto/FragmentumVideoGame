using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
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
    /// El arte del Mundo 3 en juego, con los prefabs que lo llevan:
    ///
    /// - El Centinela Ciego escucha, va hacia el ruido y apuñala con su dibujo: su Animator sigue el
    ///   estado de su oído. El Acechador se agazapa y embiste con el suyo.
    /// - El Espectro del Pavor hace el gesto que le piden sus ataques (el aviso, el tajo, el jadeo),
    ///   y repetirlo vuelve a empezar el clip.
    /// - Cada franja de hojas: durante el aviso de su zona no cae nada; cuando hace daño, caen sus
    ///   hojas y se rompen contra el suelo.
    /// - La antorcha enciende su llama y se consume con su dibujo; el cepo se cierra con el suyo.
    /// </summary>
    public class ArteMundo3PlayModeTests : PruebaPlayModeBase
    {
        private const string CarpetaEnemigos = "Assets/Prefabs/Enemigos";
        private const string RutaPavor = "Assets/Prefabs/Jefes/EspectroDelPavor.prefab";
        private const string RutaHojas = "Assets/Prefabs/Efectos/HojasDelPavor.prefab";
        private const string CarpetaMecanicas = "Assets/Prefabs/Mecanicas";
        private static readonly Vector2 LimitesArena = new Vector2(-12.5f, 12.5f);

        [UnityTearDown]
        public IEnumerator BorrarZonas()
        {
            foreach (var zona in Object.FindObjectsByType<ZonaDeDanio>(FindObjectsInactive.Include))
            {
                Object.Destroy(zona.gameObject);
            }

            foreach (var lluvia in Object.FindObjectsByType<LluviaDeHojas>(FindObjectsInactive.Include))
            {
                Object.Destroy(lluvia.gameObject);
            }

            yield return null;
        }

        private T Instanciar<T>(string ruta, Vector2 pies) where T : Component
        {
            var objeto = Object.Instantiate(CargarPrefab(ruta), new Vector3(pies.x, pies.y, 0f), Quaternion.identity);
            Creados.Add(objeto);
            var componente = objeto.GetComponent<T>();
            Assert.IsNotNull(componente, $"{ruta} no tiene {typeof(T).Name}.");
            return componente;
        }

        private static bool En(Animator animador, string estado) => animador.GetCurrentAnimatorStateInfo(0).IsName(estado);

        private PlayerModoMovimiento Ruido => Jugador.GetComponent<PlayerModoMovimiento>();

        // --- Los enemigos ---

        [UnityTest]
        public IEnumerator CentinelaCiego_EscuchaVaYApunalaConSuDibujo()
        {
            Suelo(-15f, 15f);
            yield return CrearJugadorEnSuelo(new Vector2(4f, 0f));
            var oido = Instanciar<CentinelaCiego>($"{CarpetaEnemigos}/CentinelaCiego.prefab", new Vector2(0f, 0.05f));
            var animador = oido.GetComponentInChildren<Animator>();
            Assert.IsTrue(oido.ConArte);
            yield return new WaitForSeconds(0.3f);

            Ruido.HacerRuido(PiesJugador, 6f);
            yield return EsperarHasta(() => En(animador, "Escucha"), 0.3f, "que escuche con la mano en la máscara");
            Assert.AreEqual(Quaternion.identity, oido.GetComponentInChildren<SpriteRenderer>().transform.localRotation, "Con arte, el bloque no se ladea.");
            yield return EsperarHasta(() => oido.Estado == ESTADOS_CENTINELA_CIEGO.VA || oido.Estado == ESTADOS_CENTINELA_CIEGO.ESTOCADA, 1f, "que vaya");
            yield return EsperarHasta(() => En(animador, "Va") || En(animador, "Estocada"), 0.3f, "que ande hacia el ruido");
            yield return EsperarHasta(() => En(animador, "Estocada"), 2f, "que apuñale");
            yield return EsperarHasta(() => En(animador, "Busca"), 1.5f, "que busque");
            yield return EsperarHasta(() => oido.Estado == ESTADOS_CENTINELA_CIEGO.RONDA, 3.5f, "que vuelva a su ronda");
            yield return EsperarHasta(() => En(animador, "Quieto") || En(animador, "Andar"), 0.3f, "que vuelva a su ronda con su dibujo");
        }

        [UnityTest]
        public IEnumerator Acechador_SeAgazapaYEmbisteConSuDibujo()
        {
            Suelo(-15f, 30f);
            yield return CrearJugadorEnSuelo(new Vector2(8.2f, 0f));
            var acechador = Instanciar<Acechador>($"{CarpetaEnemigos}/Acechador.prefab", new Vector2(0f, 0.05f));
            var animador = acechador.GetComponentInChildren<Animator>();
            var dibujo = acechador.GetComponentInChildren<SpriteRenderer>();
            var escala = dibujo.transform.localScale;
            Assert.IsTrue(acechador.ConArte);
            yield return new WaitForSeconds(0.3f);

            Ruido.HacerRuido(PiesJugador, 6f);
            yield return EsperarHasta(() => En(animador, "Agazapado"), 0.3f, "que se agazape");
            Assert.AreEqual(escala, dibujo.transform.localScale, "Con arte, el dibujo no se encoge: está agazapado.");
            Assert.AreEqual(Color.white, dibujo.color, "Ni se tiñe.");
            yield return EsperarHasta(() => En(animador, "Embiste"), 1.2f, "que embista");
        }

        // --- El Espectro del Pavor ---

        [UnityTest]
        public IEnumerator Pavor_HaceElGestoQueLePiden_YRepetirloLoVuelveAEmpezar()
        {
            Suelo(-20f, 20f);
            var pavor = Instanciar<EspectroDelPavor>(RutaPavor, new Vector2(0f, 0.05f));
            var animador = pavor.GetComponentInChildren<Animator>();
            yield return null;
            Assert.IsTrue(pavor.TieneArte);
            Assert.IsTrue(En(animador, GESTOS_PAVOR.REPOSO.ToString()), "Espera respirando.");

            pavor.Gesto(GESTOS_PAVOR.AVISO_TAJO);
            yield return EsperarHasta(() => En(animador, GESTOS_PAVOR.AVISO_TAJO.ToString()), 0.2f, "el aviso del tajo");
            pavor.Gesto(GESTOS_PAVOR.TAJO);
            yield return EsperarHasta(() => En(animador, GESTOS_PAVOR.TAJO.ToString()), 0.2f, "el tajo");
            yield return new WaitForSeconds(0.3f);
            var avanzado = animador.GetCurrentAnimatorStateInfo(0).normalizedTime;

            pavor.Gesto(GESTOS_PAVOR.TAJO);
            yield return null;
            Assert.IsTrue(En(animador, GESTOS_PAVOR.TAJO.ToString()));
            Assert.Less(animador.GetCurrentAnimatorStateInfo(0).normalizedTime, avanzado, "Repetir el gesto vuelve a empezar su clip.");

            pavor.Gesto(GESTOS_PAVOR.REPOSO);
            yield return EsperarHasta(() => En(animador, GESTOS_PAVOR.REPOSO.ToString()), 0.2f, "que vuelva a reposar");
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(En(animador, GESTOS_PAVOR.REPOSO.ToString()), "Y ningún disparador viejo lo saca de ahí.");
        }

        [UnityTest]
        public IEnumerator Pavor_ConSuTajo_AvisaGolpeaYJadea_ConSuDibujo()
        {
            Suelo(-20f, 20f);
            var pavor = Instanciar<EspectroDelPavor>(RutaPavor, new Vector2(0f, 0.05f));
            var datos = Object.Instantiate(pavor.Datos);
            Creados.Add(datos);
            datos.fases = new List<FaseJefe>
            {
                new FaseJefe
                {
                    nombre = "Prueba", umbralVida = 1f, pausaEntrePatrones = 0.5f,
                    patrones = new List<PatronAtaque> { CargarAsset<PatronAtaque>("Assets/Data/Jefes/Patrones/Tajo.asset") },
                },
            };
            pavor.FijarDatos(datos);
            yield return CrearJugadorEnSuelo(new Vector2(-4f, 0f));
            var animador = pavor.GetComponentInChildren<Animator>();

            pavor.Activar(Jugador.transform, LimitesArena);
            yield return EsperarHasta(() => pavor.Destellando, 3f, "que avise");
            yield return EsperarHasta(() => En(animador, GESTOS_PAVOR.AVISO_TAJO.ToString()), 0.2f, "con la espada en alto");
            yield return EsperarHasta(() => En(animador, GESTOS_PAVOR.TAJO.ToString()), 1.3f, "que tajee");
            yield return EsperarHasta(() => pavor.EsVulnerable, 0.6f, "que se quede jadeando");
            yield return EsperarHasta(() => En(animador, GESTOS_PAVOR.JADEA.ToString()), 0.2f, "jadeando, con la máscara apagada");
        }

        // --- La lluvia de hojas ---

        [UnityTest]
        public IEnumerator LasHojas_NoCaenDuranteElAviso_YCaenCuandoLaZonaHaceDanio()
        {
            const float aviso = 0.4f;
            const float duracion = 0.6f;
            var zona = ZonaDeDanio.Crear("Hojas", new Vector2(0f, 2.5f), new Vector2(2f, 5f), 1, Capas.MascaraJugador,
                aviso, duracion, Vector2.zero, Color.green, false);
            var dibujo = zona.Vestir(CargarPrefab(RutaHojas), 0);
            var lluvia = dibujo.GetComponent<LluviaDeHojas>();
            Assert.IsNotNull(lluvia);
            Assert.AreEqual(0f, dibujo.transform.position.y, 1e-3f, "El suelo que brilla, en la base de la franja.");

            yield return new WaitForSeconds(aviso * 0.5f);
            Assert.IsFalse(lluvia.Soltadas, "Durante el aviso, solo brilla el suelo.");

            yield return EsperarHasta(() => zona.Activa, aviso, "que la zona haga daño");
            yield return EsperarHasta(() => lluvia.Soltadas, 0.2f, "que caigan las hojas");
            Assert.AreEqual(lluvia.cuantas, lluvia.EnElAire, "Caen todas las de la franja.");

            yield return EsperarHasta(() => lluvia == null || lluvia.EnElAire == 0, duracion * 2f + 0.6f, "que se rompan contra el suelo y desaparezcan");
        }

        // --- Las mecánicas ---

        [UnityTest]
        public IEnumerator LaAntorcha_EnciendeSuLlama_YSeConsumeConSuDibujo()
        {
            Suelo(-12f, 12f);
            var antorcha = Instanciar<Antorcha>($"{CarpetaMecanicas}/Antorcha.prefab", Vector2.zero);
            antorcha.duracion = 0.6f;
            antorcha.parpadeo = 0.2f;
            var animador = antorcha.GetComponentInChildren<Animator>();
            yield return null;
            yield return EsperarHasta(() => En(animador, "Apagada"), 0.2f, "que esté apagada");

            // El Animator tarda dos fotogramas en cambiar, y al encenderse su luz por primera vez Unity
            // puede compilar su shader: 0,2 s se quedaban cortos (fallaba justo tras regenerar).
            Assert.IsTrue(antorcha.Encender());
            Assert.IsNotNull(antorcha.SonidoAlEncender, "Su golpe de cuerdas (Musica/Golpes/Antorcha).");
            Assert.AreSame(antorcha.SonidoAlEncender, antorcha.Fuente.clip, "Al encenderse, suenan sus cuerdas.");
            yield return EsperarHasta(() => En(animador, "Encendida"), 0.5f, "su llama");
            yield return EsperarHasta(() => antorcha.Estado == ESTADOS_ANTORCHA.SPENT, 1f, "que se consuma");
            yield return EsperarHasta(() => En(animador, "Consumida"), 0.5f, "el tocón quemado");
        }

        [UnityTest]
        public IEnumerator ElCepo_SeCierraConSuDibujo()
        {
            Suelo(-12f, 12f);
            var cepo = Instanciar<Cepo>($"{CarpetaMecanicas}/Cepo.prefab", Vector2.zero);
            var dibujo = cepo.GetComponentInChildren<SpriteRenderer>();
            yield return CrearJugadorEnSuelo(new Vector2(-5f, 0f));
            var abierto = dibujo.sprite;

            Jugador.Motor.Teletransportar(new Vector2(0f, 0.05f));
            yield return EsperarHasta(() => cepo.Estado == ESTADOS_CEPO.CLOSED, 0.5f, "que se cierre al pisarlo");
            yield return null;
            Assert.AreNotEqual(abierto, dibujo.sprite, "Cerrado es otro dibujo.");
            Assert.AreEqual(1f, dibujo.color.r, 1e-3f, "Sin teñirlo.");
        }
    }
}
