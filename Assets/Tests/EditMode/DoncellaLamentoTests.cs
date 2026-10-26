using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Los datos de la Doncella del Lamento siguen el diseño (<c>Docs/Diseno/Nivel2_Diseno.md</c>,
    /// sección 6): solo se la hiere mientras llora, cada fase cierra su ronda con un llanto y en la
    /// segunda congela lo que inunda. Y <see cref="VerificadorDoncella"/> avisa si algo de eso se
    /// rompe, porque entonces no se la podría vencer.
    /// </summary>
    public class DoncellaLamentoTests
    {
        // La espada de Kael golpea de frente, hasta 1,55 u sobre sus pies (ConstructorJugador).
        private const float EspadaArriba = 1.55f;
        private const float MargenAltura = 0.9f;
        private const float HolguraDeEspada = 0.5f;

        private readonly List<Object> _creados = new List<Object>();

        private static JefeData Datos => GeneradorJefes.CargarDatos(VerificadorDoncella.NombreDatos);

        [TearDown]
        public void Limpiar()
        {
            foreach (var objeto in _creados)
            {
                if (objeto != null)
                {
                    Object.DestroyImmediate(objeto);
                }
            }

            _creados.Clear();
        }

        // --- Datos ---

        [Test]
        public void LaDoncella_SoloSeHiereLlorando_YTocarlaNoHaceDanio()
        {
            var doncella = Datos;
            Assert.AreEqual("Doncella del Lamento", doncella.nombreCanonico);
            Assert.AreEqual(26, doncella.vidaMaxima);
            Assert.IsTrue(doncella.inmuneFueraDeVulnerable, "Fuera del llanto, los golpes no le hacen nada.");
            Assert.AreEqual(2f, doncella.multiplicadorVulnerable, 1e-4f, "Llorando, el daño vale el doble.");
            Assert.AreEqual(0, doncella.danioContacto, "Su cuerpo no hiere: las placas que atraviesa son refugio.");
            Assert.AreEqual(2, doncella.fases.Count);
            Assert.AreEqual(0.5f, doncella.fases[1].umbralVida, 1e-4f, "La fase 2 empieza al 50 % de vida.");
            Assert.IsFalse(GeneradorJefes.EsEsqueleto(doncella));
        }

        [Test]
        public void CadaFase_CierraSuRondaConUnLlanto_YEnLaSegundaLloraMenos()
        {
            var fases = Datos.fases;
            foreach (var fase in fases)
            {
                Assert.IsInstanceOf<PatronLlanto>(fase.patrones.Last(), $"{fase.nombre}: la ronda acaba llorando.");
                Assert.AreEqual(1, fase.patrones.OfType<PatronLlanto>().Count(), $"{fase.nombre}: una sola ventana por ronda.");
            }

            var llanto1 = (PatronLlanto)fases[0].patrones.Last();
            var llanto2 = (PatronLlanto)fases[1].patrones.Last();
            Assert.AreEqual(1.5f, llanto1.duracion, 1e-4f, "En el hielo llora 1,5 s.");
            Assert.AreEqual(1.2f, llanto2.duracion, 1e-4f, "Sobre el agua, 1,2 s.");
        }

        [Test]
        public void LaFase1_TieneOlasEInundacion_YLaFase2_CongelaLoQueInunda()
        {
            var fases = Datos.fases;
            var fase1 = Tipos(fases[0]);
            CollectionAssert.Contains(fase1, typeof(PatronOla));
            CollectionAssert.Contains(fase1, typeof(PatronInundacion));
            CollectionAssert.DoesNotContain(fase1, typeof(PatronCongelar), "En la fase 1 el agua no se congela.");

            var fase2 = Tipos(fases[1]);
            Assert.AreEqual(2, fases[1].patrones.OfType<PatronOla>().Single().olas, "En la fase 2, olas dobles.");
            var inunda = fase2.IndexOf(typeof(PatronInundacion));
            var congela = fase2.IndexOf(typeof(PatronCongelar));
            Assert.GreaterOrEqual(inunda, 0);
            Assert.AreEqual(inunda + 1, congela, "Congela justo después de inundar: lo que estaba inundado.");
            Assert.AreEqual(congela + 1, fase2.Count - 1, "Y llora justo después de congelar: sobre su hielo.");

            foreach (var patron in fases.SelectMany(f => f.patrones).Where(p => !(p is PatronLlanto)))
            {
                Assert.GreaterOrEqual(patron.aviso, 0.5f, $"{patron.nombre}: el aviso se tiene que poder ver.");
            }
        }

        [Test]
        public void ElAguaSigueAhiCuandoSeCongela_YLaCapaCuandoLlora()
        {
            var fase = Datos.fases[1];
            var inundacion = fase.patrones.OfType<PatronInundacion>().Single();
            var congelar = fase.patrones.OfType<PatronCongelar>().Single();
            var llanto = fase.patrones.OfType<PatronLlanto>().Single();

            // Desde que el agua empieza a subir hasta que acaba el aviso de congelar (1 s para subir, de sobra).
            var hastaCongelar = 1f + inundacion.recuperacion + fase.pausaEntrePatrones + congelar.aviso;
            Assert.Less(hastaCongelar, inundacion.duracion, "El agua no se ha ido cuando toca congelarla.");

            // Desde que se congela hasta que deja de llorar, con lo que tarda como mucho en bajar.
            var hastaElFinDelLlanto = congelar.duracionPuas + congelar.recuperacion + fase.pausaEntrePatrones +
                                      llanto.tiempoMaximoBajando + 0.5f + llanto.duracion;
            Assert.Less(hastaElFinDelLlanto, congelar.duracionCapa, "La capa de hielo aguanta hasta que termina de llorar.");
        }

        [Test]
        public void Llora_DondeLaEspadaLlega()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VerificadorDoncella.RutaPrefab);
            Assert.IsNotNull(prefab);
            var doncella = prefab.GetComponent<DoncellaLamento>();
            Assert.AreEqual(DisenoNivel2.VueloDoncella, doncella.AlturaDeVuelo, 1e-4f, "El prefab vuela a la altura del diseño.");
            Assert.Greater(doncella.AlturaDeVuelo, EspadaArriba, "Flotando, desde el suelo no se le da sin saltar.");

            // En el hielo (fase 1), a tiro de espada: 1,5 u por delante de Kael, y ella mide 1 u a cada lado.
            Assert.LessOrEqual(doncella.DistanciaAKael, 1.5f + Datos.tamano.x * 0.5f - HolguraDeEspada, "Baja a tiro de espada.");
            Assert.Greater(doncella.DistanciaAKael, Datos.tamano.x * 0.5f + 0.4f, "Pero no encima de Kael.");

            // Sobre la capa de hielo: de pie en ella, la espada llega.
            Assert.Greater(doncella.AlturaSobreLaCapa, 0f, "Flota sobre la capa, no dentro.");
            Assert.LessOrEqual(doncella.AlturaSobreLaCapa + HolguraDeEspada, EspadaArriba, "De pie en la capa, la espada la alcanza con holgura.");

            // Metida en el agua: de pie en el fondo no se llega, y con un salto bajo el agua, sí.
            var kael = new ParametrosJugador();
            var agua = ModificadorMovimiento.BajoElAgua;
            var salto = kael.velocidadSalto * agua.salto;
            var alturaSalto = salto * salto / (2f * Mathf.Abs(Physics2D.gravity.y) * kael.escalaGravedad * agua.gravedad);
            var pies = DisenoNivel2.AguaDeLaInundacion - doncella.HundimientoAlLlorar;
            Assert.Greater(pies, EspadaArriba, "De pie en el fondo no se llega: hay que saltar.");
            Assert.LessOrEqual(pies + HolguraDeEspada, alturaSalto * MargenAltura + EspadaArriba, "Con un salto bajo el agua, sí.");
        }

        // --- El verificador ---

        [Test]
        public void ElVerificador_DaPorBuenaALaDoncella()
        {
            Assert.IsEmpty(VerificadorDoncella.Problemas());
            Assert.IsTrue(VerificadorDoncella.Verificar());
        }

        [Test]
        public void ElVerificador_AvisaSiUnaFaseSeQuedaSinLlanto()
        {
            var datos = Copia();
            datos.fases[1].patrones.RemoveAll(p => p is PatronLlanto);

            var problemas = VerificadorDoncella.ProblemasDeLosDatos(datos);

            Assert.AreEqual(1, problemas.Count, string.Join("\n", problemas));
            StringAssert.Contains("no tiene llanto", problemas[0]);
            StringAssert.Contains(datos.fases[1].nombre, problemas[0]);
        }

        [Test]
        public void ElVerificador_AvisaSiDejaDeSerInmune_OSiUnaFaseSeQuedaVacia()
        {
            var datos = Copia();
            datos.inmuneFueraDeVulnerable = false;
            datos.fases[0].patrones.Clear();

            var problemas = VerificadorDoncella.ProblemasDeLosDatos(datos);

            Assert.AreEqual(2, problemas.Count, string.Join("\n", problemas));
            Assert.IsTrue(problemas.Any(p => p.Contains("no es inmune")));
            Assert.IsTrue(problemas.Any(p => p.Contains("no tiene patrones")));
            StringAssert.Contains("Faltan los datos", VerificadorDoncella.ProblemasDeLosDatos(null).Single());
        }

        [Test]
        public void ElVerificador_AvisaSiCongelaSinInundar_OSiUnAtaqueNoAvisa()
        {
            var datos = Copia();
            var fase = datos.fases[1];
            fase.patrones.RemoveAll(p => p is PatronInundacion);
            var indice = fase.patrones.FindIndex(p => p is PatronOla);
            var olaRapida = Object.Instantiate(fase.patrones[indice]);
            olaRapida.name = "OlaRapida";
            olaRapida.aviso = 0.1f;
            _creados.Add(olaRapida);
            fase.patrones[indice] = olaRapida;

            var problemas = VerificadorDoncella.ProblemasDeLosDatos(datos);

            Assert.AreEqual(2, problemas.Count, string.Join("\n", problemas));
            Assert.IsTrue(problemas.Any(p => p.Contains("congela, pero no inunda")));
            Assert.IsTrue(problemas.Any(p => p.Contains("OlaRapida") && p.Contains("avisa 0.1 s")));
        }

        [Test]
        public void ElVerificador_AvisaSiElPrefabPierdeSuControlador_OTieneDanioEncendido()
        {
            var raiz = PrefabUtility.LoadPrefabContents(VerificadorDoncella.RutaPrefab);
            try
            {
                Assert.IsEmpty(VerificadorDoncella.ProblemasDelPrefab(raiz, null), "El prefab, tal cual, está bien.");

                raiz.transform.Find("Contacto").gameObject.SetActive(true);
                Object.DestroyImmediate(raiz.GetComponent<DoncellaLamento>());

                var problemas = VerificadorDoncella.ProblemasDelPrefab(raiz, null);
                Assert.AreEqual(2, problemas.Count, string.Join("\n", problemas));
                Assert.IsTrue(problemas.Any(p => p.Contains("le falta su controlador")));
                Assert.IsTrue(problemas.Any(p => p.Contains("Contacto") && p.Contains("encendido en reposo")));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            StringAssert.Contains("Falta el prefab", VerificadorDoncella.ProblemasDelPrefab(null, null).Single());
        }

        // --- Utilidades ---

        // Una copia de sus datos, con sus propias listas de patrones, para romperla sin tocar el asset.
        private JefeData Copia()
        {
            var copia = Object.Instantiate(Datos);
            _creados.Add(copia);
            return copia;
        }

        private static List<System.Type> Tipos(FaseJefe fase) => fase.patrones.Where(p => p != null).Select(p => p.GetType()).ToList();
    }
}
