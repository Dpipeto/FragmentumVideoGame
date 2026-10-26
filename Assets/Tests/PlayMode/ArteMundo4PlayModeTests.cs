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
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// El arte del Mundo 4 en juego, con los prefabs que lo llevan (fase E, primera tanda):
    ///
    /// - El Juez Silente hace el gesto que le piden sus ataques y se arrodilla con su dibujo al mirar su
    ///   memoria; la trampa dibuja sus cadenas sobre Kael; al caer, la gema flota sobre su montón y se
    ///   apaga al recogerla.
    /// - La artillería de «La orden» avisa con su marca y estalla con su dibujo al hacer daño.
    /// - El barril y la mina estallan con la explosión dibujada, a la escala de su alcance.
    /// - El Artillero dispara su bala dibujada desde la boca de su fusil; el Verdugo lanza su garfio con
    ///   la cadena desde su mano.
    /// </summary>
    public class ArteMundo4PlayModeTests : PruebaPlayModeBase
    {
        private const string RutaJuez = "Assets/Prefabs/Jefes/JuezSilente.prefab";
        private const string RutaArtilleria = "Assets/Prefabs/Efectos/ArtilleriaDeLaOrden.prefab";
        private const string CarpetaEnemigos = "Assets/Prefabs/Enemigos";
        private const string CarpetaMecanicas = "Assets/Prefabs/Mecanicas";
        private static readonly Vector2 LimitesArena = new Vector2(-12.5f, 12.5f);

        [UnityTearDown]
        public IEnumerator BorrarLoQueQueda()
        {
            foreach (var zona in Object.FindObjectsByType<ZonaDeDanio>(FindObjectsInactive.Include))
            {
                Object.Destroy(zona.gameObject);
            }

            foreach (var explosion in Object.FindObjectsByType<ExplosionDibujada>(FindObjectsInactive.Include))
            {
                Object.Destroy(explosion.gameObject);
            }

            foreach (var proyectil in Object.FindObjectsByType<Proyectil>(FindObjectsInactive.Include))
            {
                Object.Destroy(proyectil.gameObject);
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

        // El Juez en combate, sin ataques: solo hace lo que le pide la prueba.
        private IEnumerator JuezQuieto(System.Action<JuezSilente> listo)
        {
            Suelo(-20f, 20f);
            var juez = Instanciar<JuezSilente>(RutaJuez, new Vector2(0f, 0.05f));
            var datos = Object.Instantiate(juez.Datos);
            Creados.Add(datos);
            datos.fases = new List<FaseJefe>
            {
                new FaseJefe { nombre = "Prueba", umbralVida = 1f, pausaEntrePatrones = 0.5f, patrones = new List<PatronAtaque>() },
            };
            juez.FijarDatos(datos);
            yield return CrearJugadorEnSuelo(new Vector2(-4f, 0f));
            juez.Activar(Jugador.transform, LimitesArena);
            yield return null;
            Assert.IsTrue(juez.TieneArte, "El Juez lleva su arte.");
            listo(juez);
        }

        // --- El Juez Silente ---

        [UnityTest]
        public IEnumerator Juez_HaceElGestoQueLePiden_YSeArrodillaAlMirarSuMemoria()
        {
            JuezSilente juez = null;
            yield return JuezQuieto(j => juez = j);
            var animador = juez.GetComponentInChildren<Animator>();
            yield return EsperarHasta(() => En(animador, GESTOS_JUEZ.REPOSO.ToString()), 0.5f, "que espere respirando");

            juez.Gesto(GESTOS_JUEZ.AVISO_ORDEN);
            yield return EsperarHasta(() => En(animador, GESTOS_JUEZ.AVISO_ORDEN.ToString()), 0.2f, "el aviso de la orden");
            juez.Gesto(GESTOS_JUEZ.ORDEN);
            yield return EsperarHasta(() => En(animador, GESTOS_JUEZ.ORDEN.ToString()), 0.2f, "la orden");

            juez.ActivarMemoriaCorrecta();
            yield return EsperarHasta(() => juez.EsVulnerable, 0.5f, "que se arrodille");
            yield return EsperarHasta(() => En(animador, GESTOS_JUEZ.ARRODILLADO.ToString()), 0.2f, "de rodillas, con su dibujo");
            yield return EsperarHasta(() => !juez.EsVulnerable, juez.Datos.duracionVulnerable + 0.5f, "que acabe su ventana");
            yield return EsperarHasta(() => En(animador, GESTOS_JUEZ.REPOSO.ToString()), 0.3f, "que se levante");
        }

        [UnityTest]
        public IEnumerator LaTrampa_DibujaSusCadenasSobreKael_MientrasLoSujeta()
        {
            JuezSilente juez = null;
            yield return JuezQuieto(j => juez = j);

            juez.ActivarMemoriaFalsa();
            Assert.IsTrue(juez.Sujetando, "La trampa sujeta a Kael.");
            Assert.IsNotNull(juez.CadenasDibujadas, "Y se le ven las cadenas.");
            var cadenas = juez.CadenasDibujadas.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(2, cadenas.Length, "Dos, cruzadas.");
            Assert.IsTrue(cadenas.All(c => c.drawMode == SpriteDrawMode.Tiled && c.sprite != null));

            yield return EsperarHasta(() => !juez.Sujetando, juez.sujecionTrampa + 0.5f, "que lo suelten");
            yield return null;
            Assert.IsTrue(juez.CadenasDibujadas == null, "Al soltarlo, las cadenas desaparecen.");
        }

        [UnityTest]
        public IEnumerator AlCaer_LaGemaFlotaSobreSuMonton_YSeApagaAlRecogerla()
        {
            JuezSilente juez = null;
            yield return JuezQuieto(j => juez = j);
            Jugador.transform.position = new Vector3(-10f, 0.05f, 0f);

            juez.ActivarMemoriaCorrecta();
            yield return EsperarHasta(() => juez.EsVulnerable, 0.5f, "que se arrodille");
            juez.Vida.RecibirGolpe(new Golpe(juez.Vida.Vida, juez.Centro));
            yield return EsperarHasta(() => juez.Fragmento != null && juez.Fragmento.Disponible, juez.Datos.duracionDerrota + 1f,
                "que la gema aparezca sobre el montón");
            var gema = juez.Fragmento.Gema;
            Assert.IsTrue(gema.enabled && gema.gameObject.activeInHierarchy, "La gema se ve.");
            Assert.Greater(gema.bounds.min.y, juez.transform.position.y + 0.9f, "Flota sobre el montón.");

            juez.RecogerFragmento();
            yield return null;
            Assert.IsFalse(gema.enabled, "Recogida, ya no está.");
        }

        // --- La artillería de «La orden» ---

        [UnityTest]
        public IEnumerator LaOrden_AvisaConSuMarca_YEstallaConSuDibujo()
        {
            const float aviso = 0.4f;
            const float duracion = 0.3f;
            var zona = ZonaDeDanio.Crear("Orden", new Vector2(0f, 1.5f), new Vector2(1.8f, 3f), 1, Capas.MascaraJugador,
                aviso, duracion, Vector2.zero, Color.red, false);
            var dibujo = zona.Vestir(CargarPrefab(RutaArtilleria), 0);
            var animador = dibujo.GetComponent<Animator>();
            Assert.AreEqual(0f, dibujo.transform.position.y, 1e-3f, "La marca, en el suelo.");
            yield return null;
            Assert.IsTrue(En(animador, "Aviso"), "Mientras avisa, la marca.");

            yield return EsperarHasta(() => zona.Activa, aviso + 0.1f, "que la zona haga daño");
            yield return EsperarHasta(() => En(animador, "Brota") || En(animador, "Arde"), 0.2f, "que estalle");
            yield return EsperarHasta(() => zona == null, duracion + 0.3f, "que la zona acabe");
            Assert.IsTrue(dibujo != null, "El humo y el cráter siguen un momento, ya sin daño.");
            yield return EsperarHasta(() => dibujo == null, 1f, "que desaparezca");
        }

        // --- Las explosiones ---

        [UnityTest]
        public IEnumerator ElBarril_EstallaConLaExplosionDibujada_EnSuPie()
        {
            Suelo(-10f, 10f);
            var barril = Instanciar<BarrilExplosivo>($"{CarpetaMecanicas}/BarrilExplosivo.prefab", new Vector2(2f, 0f));
            yield return null;
            barril.Estallar();
            var explosion = Object.FindAnyObjectByType<ExplosionDibujada>();
            Assert.IsNotNull(explosion, "Estalla con su dibujo.");
            Assert.AreEqual(2f, explosion.transform.position.x, 1e-3f);
            Assert.AreEqual(0f, explosion.transform.position.y, 1e-3f, "Nace en el pie del barril.");
            Assert.AreEqual(1f, explosion.transform.localScale.x, 1e-3f, "A la escala de su alcance: el dibujo mide el de un barril.");
            yield return new WaitForSeconds(explosion.duracion + 0.2f);
            Assert.IsTrue(explosion == null, "Y se va.");
        }

        [UnityTest]
        public IEnumerator LaMina_EstallaConLaExplosionDibujada_ALaEscalaDeSuAlcance()
        {
            var mina = Instanciar<Mina>($"{CarpetaMecanicas}/Mina.prefab", new Vector2(0f, -3f));
            yield return null;
            var centro = mina.Centro;
            var radioBola = mina.RadioBola;
            mina.Estallar();
            var explosion = Object.FindAnyObjectByType<ExplosionDibujada>();
            Assert.IsNotNull(explosion);
            Assert.AreEqual(mina.radio / explosion.radio, explosion.transform.localScale.x, 1e-3f, "Más pequeña que la del barril.");
            Assert.AreEqual(centro.y - radioBola, explosion.transform.position.y, 1e-3f, "Nace bajo la bola.");
        }

        // --- Los enemigos ---

        [UnityTest]
        public IEnumerator ElArtillero_DisparaSuBalaDibujada_DesdeLaBocaDeSuFusil()
        {
            Suelo(-20f, 20f);
            yield return CrearJugadorEnSuelo(new Vector2(-7f, 0f));
            var artillero = Instanciar<Artillero>($"{CarpetaEnemigos}/Artillero.prefab", new Vector2(0f, 0.05f));
            var animador = artillero.GetComponentInChildren<Animator>();
            Proyectil bala = null;
            var salida = Vector2.zero;
            artillero.OnDisparo += p =>
            {
                if (bala == null)
                {
                    bala = p;
                    salida = p.transform.position;
                }
            };

            yield return EsperarHasta(() => En(animador, POSES_ARTILLERO.APUNTA.ToString()), 1.5f, "que apunte con el fusil al hombro");
            yield return EsperarHasta(() => bala != null, 2f, "que dispare");
            Assert.AreEqual("BalaDelArtillero", bala.name, "Su bala dibujada.");
            Assert.AreEqual(artillero.Canon.x, salida.x, 1e-3f, "Sale de la boca de su fusil…");
            Assert.AreEqual(artillero.Canon.y, salida.y, 1e-3f);
            Assert.Less(bala.transform.localScale.x, 0f, "…y vuela a la izquierda, con la estela detrás.");
        }

        [UnityTest]
        public IEnumerator ElVerdugo_LanzaSuGarfio_ConLaCadenaDesdeSuMano()
        {
            Suelo(-30f, 30f);
            yield return CrearJugadorEnSuelo(new Vector2(-8.5f, 0f));
            var verdugo = Instanciar<Verdugo>($"{CarpetaEnemigos}/Verdugo.prefab", new Vector2(0f, 0.05f));
            verdugo.limites = new Vector2(-30f, 30f);

            yield return EsperarHasta(() => verdugo.GarfioEnVuelo, 6f, "que lance la cadena");
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(verdugo.GarfioEnVuelo, "Sigue volando.");
            var garfio = verdugo.transform.Find("Garfio");
            var cadena = verdugo.transform.Find("Cadena").GetComponent<SpriteRenderer>();
            Assert.IsTrue(garfio.gameObject.activeInHierarchy && cadena.gameObject.activeInHierarchy, "Se ven el garfio y la cadena.");
            Assert.AreEqual(-90f, Mathf.DeltaAngle(0f, garfio.eulerAngles.z), 1f, "El garfio, por delante: los eslabones miran a su mano.");
            Assert.Greater(cadena.size.x, 0.5f, "La cadena se alarga de su mano al garfio.");
            Assert.Less(cadena.bounds.max.x, verdugo.transform.position.x, "Por delante de él.");
        }
    }
}
