using System.Linq;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using MEDIOS = Fragmentum.EditorTools.DisenoNivel2.MEDIOS;
using TIPOS = Fragmentum.EditorTools.DisenoNivel2.TIPOS_ENEMIGO;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El recorrido de diseño del Nivel 2 (<see cref="DisenoNivel2"/>) se puede cruzar con los
    /// saltos de Kael, en seco y bajo el agua; cumple los recuentos del Anexo A, y el agua y las
    /// bolsas de aire encajan con lo que se pisa. Lo comprueba sobre los datos;
    /// <see cref="EscenaNivel2Tests"/> comprueba que la escena los sigue.
    /// </summary>
    public class DisenoNivel2Tests
    {
        // Los mismos márgenes que en el Nivel 1: nunca se pide el límite exacto de lo que se puede.
        private const float MargenAlcance = 0.85f;
        private const float MargenAltura = 0.9f;

        // Al cruzar la superficie, la gravedad vuelve a ser la de fuera: lo que quede de impulso
        // tiene que subir a Kael al menos esto por encima del borde.
        private const float MargenAlSalirDelAgua = 0.25f;

        private const float AnchoKael = 0.8f;
        private const float AltoKael = 1.8f;

        private static readonly ParametrosJugador Kael = new ParametrosJugador();

        private static ModificadorMovimiento Medio(MEDIOS medio) =>
            medio == MEDIOS.AGUA ? ModificadorMovimiento.BajoElAgua : ModificadorMovimiento.Neutro;

        private static float Gravedad(MEDIOS medio) => Mathf.Abs(Physics2D.gravity.y) * Kael.escalaGravedad * Medio(medio).gravedad;

        private static float Salto(MEDIOS medio) => Kael.velocidadSalto * Medio(medio).salto;

        private static float DobleSalto(MEDIOS medio) => Kael.velocidadDobleSalto * Medio(medio).salto;

        private static float AlturaSalto(MEDIOS medio) => Salto(medio) * Salto(medio) / (2f * Gravedad(medio));

        private static float AlturaDobleSalto(MEDIOS medio) => AlturaSalto(medio) + DobleSalto(medio) * DobleSalto(medio) / (2f * Gravedad(medio));

        /// <summary>Distancia que recorre Kael corriendo hasta caer en algo <paramref name="subida"/> más alto.</summary>
        private static float Alcance(float subida, MEDIOS medio)
        {
            var g = Gravedad(medio);
            var caida = g * Kael.multiplicadorGravedadCaida;
            var tiempo = subida <= AlturaSalto(medio)
                ? Salto(medio) / g + Mathf.Sqrt(2f * Mathf.Max(0f, AlturaSalto(medio) - subida) / caida)
                : Salto(medio) / g + DobleSalto(medio) / g + Mathf.Sqrt(2f * Mathf.Max(0f, AlturaDobleSalto(medio) - subida) / caida);
            return Kael.velocidadMaxima * Medio(medio).velocidad * tiempo;
        }

        [Test]
        public void BajoElAgua_KaelSaltaMasAlto_YLasMedidasSonLasDelDocumento()
        {
            Assert.AreEqual(2.45f, AlturaSalto(MEDIOS.SECO), 0.05f);
            Assert.AreEqual(4.15f, AlturaDobleSalto(MEDIOS.SECO), 0.1f);
            Assert.AreEqual(2.9f, AlturaSalto(MEDIOS.AGUA), 0.1f, "Bajo el agua, el salto sube unas 2,9 u.");
            Assert.AreEqual(5f, AlturaDobleSalto(MEDIOS.AGUA), 0.1f, "Y con el doble salto, unas 5.");
            Assert.Greater(AlturaDobleSalto(MEDIOS.AGUA), AlturaDobleSalto(MEDIOS.SECO), "El agua sostiene.");
        }

        /// <summary>Lo que sube y baja una pisada: la amplitud si es una placa flotante, 0 si no.</summary>
        private static float Amplitud(string pisada) =>
            DisenoNivel2.Placas.Where(p => p.Nombre == pisada).Select(p => p.Amplitud).DefaultIfEmpty(0f).First();

        [Test]
        public void CadaPaso_SeAlcanzaEnSuMedio_EsperandoElMomento()
        {
            // Con placas que suben y bajan, cada salto tiene que poder darse en su mejor momento:
            // saliendo de lo alto de una placa y llegando a lo bajo de la otra.
            foreach (var paso in DisenoNivel2.Pasos)
            {
                var desde = DisenoNivel2.PisadaDe(paso.Desde);
                var hasta = DisenoNivel2.PisadaDe(paso.Hasta);
                var subida = (hasta.Y - Amplitud(paso.Hasta)) - (desde.Y + Amplitud(paso.Desde));
                var hueco = Mathf.Max(hasta.Desde - desde.Hasta, desde.Desde - hasta.Hasta, 0f);
                Assert.LessOrEqual(subida, AlturaDobleSalto(paso.Medio) * MargenAltura, $"{paso}: demasiado alto.");
                Assert.LessOrEqual(hueco, Alcance(Mathf.Max(0f, subida), paso.Medio) * MargenAlcance, $"{paso}: demasiado lejos.");
            }
        }

        [Test]
        public void LasPlacasDelAgua_SeCruzanSinEsperar()
        {
            // La primera vez que sale una placa no hay que medir el momento: se cruza en el peor caso.
            foreach (var paso in DisenoNivel2.Pasos.Where(p => p.Desde.StartsWith("PlacaFlotante") || p.Hasta.StartsWith("PlacaFlotante")))
            {
                var desde = DisenoNivel2.PisadaDe(paso.Desde);
                var hasta = DisenoNivel2.PisadaDe(paso.Hasta);
                var subida = (hasta.Y + Amplitud(paso.Hasta)) - (desde.Y - Amplitud(paso.Desde));
                var hueco = Mathf.Max(hasta.Desde - desde.Hasta, desde.Desde - hasta.Hasta, 0f);
                Assert.LessOrEqual(subida, AlturaSalto(MEDIOS.SECO) * MargenAltura, $"{paso}: con un salto normal.");
                Assert.LessOrEqual(hueco, Alcance(Mathf.Max(0f, subida), MEDIOS.SECO) * MargenAlcance, $"{paso}: demasiado lejos.");
            }
        }

        [Test]
        public void LasPlacas_FlotanSobreElAgua_ALaContra_YConElAnchoDeSuPrefab()
        {
            var agua = Agua("AguaDeLasPlacas");
            var delAgua = DisenoNivel2.Placas.Where(p => p.Nombre.StartsWith("PlacaFlotante")).OrderBy(p => p.X).ToList();
            Assert.AreEqual(2, delAgua.Count);
            foreach (var placa in delAgua)
            {
                Assert.Greater(placa.Y - placa.Amplitud, agua.Arriba, $"{placa.Nombre}: en lo más bajo sigue fuera del agua; encima no se moja.");
                Assert.That(placa.X, Is.InRange(agua.Desde, agua.Hasta));
            }

            Assert.AreEqual(DisenoNivel2.PeriodoPlaca * 0.5f, Mathf.Abs(delAgua[1].Desfase - delAgua[0].Desfase), 1e-4f, "A la contra: cuando una sube, la otra baja.");
            var deLaArena = DisenoNivel2.Placas.Where(p => p.Nombre.StartsWith("PlacaArena")).ToList();
            Assert.AreEqual(DisenoNivel2.PeriodoPlaca * 0.5f, Mathf.Abs(deLaArena[1].Desfase - deLaArena[0].Desfase), 1e-4f);
            Assert.AreEqual(GeneradorMecanicas.TamanoPlaca.x, DisenoNivel2.AnchoPlaca, 1e-4f, "El diseño mide lo que el prefab.");
        }

        [Test]
        public void ElTapon_TapaElAgujero_YAsomaParaQueLaEspadaLoAlcance()
        {
            var tapon = DisenoNivel2.HieloRompible;
            var tamano = GeneradorMecanicas.TamanoTapon;
            var agujero = DisenoNivel2.Agujero;
            Assert.AreEqual(agujero.Desde, tapon.x - tamano.x * 0.5f, 1e-4f, "Tapa el agujero de lado a lado.");
            Assert.AreEqual(agujero.Hasta, tapon.x + tamano.x * 0.5f, 1e-4f);
            Assert.AreEqual(agujero.Abajo, tapon.y - tamano.y, 1e-4f, "Desde el fondo del hueco.");

            // La espada de Kael golpea entre 0,25 y 1,55 u sobre sus pies: el tapón tiene que asomar
            // por encima del suelo de la plaza para que se le pueda dar de frente.
            var asoma = tapon.y - DisenoNivel2.PisadaDe("Plaza").Y;
            Assert.Greater(asoma, 0.75f, "Asoma lo bastante para recibir el golpe.");
            Assert.LessOrEqual(asoma, AlturaSalto(MEDIOS.SECO) * MargenAltura, "Y se puede subir encima: no es otra muralla.");
        }

        [Test]
        public void LosPasosBajoElAgua_SeDanDentroDelAgua_YLosEnSecoFuera()
        {
            foreach (var paso in DisenoNivel2.Pasos)
            {
                var desde = DisenoNivel2.PisadaDe(paso.Desde);
                var hasta = DisenoNivel2.PisadaDe(paso.Hasta);
                var agua = AguaDelSalto(desde, hasta);
                if (paso.Medio == MEDIOS.SECO)
                {
                    Assert.IsNull(agua, $"{paso}: sale de un sitio seco.");
                    continue;
                }

                Assert.IsNotNull(agua, $"{paso}: sale de dentro del agua.");
                Assert.LessOrEqual(hasta.Y, agua.Value.Arriba + DisenoNivel2.BajoLaSuperficie + 1e-4f,
                    $"{paso}: termina dentro del agua o en su superficie, no por encima.");
            }
        }

        [Test]
        public void AlSalirDelAgua_QuedaImpulsoParaSubirAlBorde()
        {
            // Las subidas que acaban en la superficie: lo que sobra del salto bajo el agua, con la
            // gravedad de fuera, tiene que subir a Kael por encima del borde.
            var sobre = Gravedad(MEDIOS.AGUA) / Gravedad(MEDIOS.SECO);
            var comprobadas = 0;
            foreach (var paso in DisenoNivel2.Pasos.Where(p => p.Medio == MEDIOS.AGUA))
            {
                var desde = DisenoNivel2.PisadaDe(paso.Desde);
                var hasta = DisenoNivel2.PisadaDe(paso.Hasta);
                var agua = AguaDelSalto(desde, hasta).Value;
                if (Mathf.Abs(hasta.Y - DisenoNivel2.BajoLaSuperficie - agua.Arriba) > 1e-3f)
                {
                    continue;
                }

                // Bajo el agua sube hasta la superficie; lo que sobra, fuera, tiene que cubrir lo que
                // queda hasta el borde y el margen.
                var sobrante = (AlturaDobleSalto(MEDIOS.AGUA) - (agua.Arriba - desde.Y)) * sobre - DisenoNivel2.BajoLaSuperficie;
                Assert.GreaterOrEqual(sobrante, MargenAlSalirDelAgua, $"{paso}: al cruzar la superficie no llega al borde.");
                comprobadas++;
            }

            Assert.AreEqual(3, comprobadas, "Se sale del agua tres veces: del agua de las placas, a la campana y al campanario.");
        }

        [Test]
        public void LaTorre_SoloSeSubeBajoElAgua()
        {
            var rellanos = new[] { "SueloCalle", "Rellano1", "Rellano2" }.Select(DisenoNivel2.PisadaDe).ToList();
            for (var i = 1; i < rellanos.Count; i++)
            {
                var subida = rellanos[i].Y - rellanos[i - 1].Y;
                Assert.Greater(subida, AlturaDobleSalto(MEDIOS.SECO), $"{rellanos[i].Nombre}: en seco no se llega.");
                Assert.LessOrEqual(subida, AlturaDobleSalto(MEDIOS.AGUA) * MargenAltura, $"{rellanos[i].Nombre}: bajo el agua, sí.");
            }

            var torre = DisenoNivel2.Aguas.Single(a => a.Nombre == "AguaDeLaTorre");
            Assert.AreEqual(DisenoNivel2.PisadaDe("Rellano3").Y, torre.Arriba + DisenoNivel2.BajoLaSuperficie, 1e-4f,
                "El último rellano está a ras del agua: ahí se respira.");
        }

        [Test]
        public void CadaAgua_LlegaALaAlturaDeLoQueSePisaAlSalir_YNoSeSolapan()
        {
            // Justo por debajo: de pie en el borde, Kael ya no roza el agua.
            var bajo = DisenoNivel2.BajoLaSuperficie;
            Assert.That(bajo, Is.InRange(0.05f, 0.2f));
            Assert.AreEqual(DisenoNivel2.PisadaDe("Plaza").Y - bajo, Agua("AguaDeLasPlacas").Arriba, 1e-4f, "Las placas flotan a ras del agua.");
            Assert.AreEqual(DisenoNivel2.PisadaDe("RepisaDeLaCampana").Y - bajo, Agua("AguaBajoLaCampana").Arriba, 1e-4f, "La repisa queda seca, en la bolsa de aire.");
            Assert.AreEqual(DisenoNivel2.PisadaDe("Rellano3").Y - bajo, Agua("AguaDeLaTorre").Arriba, 1e-4f);
            foreach (var placa in DisenoNivel2.Placas.Where(p => p.Y == 0f))
            {
                Assert.Greater(placa.Y, Agua("AguaDeLasPlacas").Arriba, $"De pie en {placa.Nombre}, Kael está en seco.");
            }

            var techo = DisenoNivel2.Paredes.Single(p => p.Nombre == "TechoDeHielo");
            Assert.AreEqual(techo.Abajo, Agua("AguaDeLaCalle").Arriba, 1e-4f, "El agua de la calle llega al techo de hielo.");

            var aguas = DisenoNivel2.Aguas;
            for (var i = 0; i < aguas.Length; i++)
            {
                for (var j = i + 1; j < aguas.Length; j++)
                {
                    var a = aguas[i].Rect;
                    var b = aguas[j].Rect;
                    var solape = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > 1e-3f && Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > 1e-3f;
                    Assert.IsFalse(solape, $"{aguas[i]} y {aguas[j]} se solapan: dos aguas a la vez frenarían el doble.");
                }
            }
        }

        [Test]
        public void LasBolsasDeAire_NoTienenAgua_YCabeKael()
        {
            foreach (var bolsa in DisenoNivel2.BolsasDeAire)
            {
                Assert.IsNull(DisenoNivel2.AguaEn((bolsa.Desde + bolsa.Hasta) * 0.5f, bolsa.Abajo + 0.5f), $"{bolsa.Nombre}: dentro se respira.");
                Assert.GreaterOrEqual(bolsa.Arriba - bolsa.Abajo, AltoKael + 1f, $"{bolsa.Nombre}: Kael cabe de pie y puede saltar.");
            }

            var campana = DisenoNivel2.BolsasDeAire.Single(b => b.Nombre == "BolsaDeLaCampana");
            var cp1 = DisenoNivel2.Checkpoints[0];
            Assert.IsTrue(campana.Contiene(cp1.x, cp1.y + 0.5f), "El primer checkpoint está en la bolsa de aire, junto a la silla vacía.");
            Assert.AreEqual(DisenoNivel2.SueloBajo(cp1.x, cp1.y), cp1.y, 1e-4f, "De pie en la repisa.");
        }

        [Test]
        public void LosPasosEstrechos_DejanPasarAKael()
        {
            Assert.GreaterOrEqual(DisenoNivel2.Agujero.Hasta - DisenoNivel2.Agujero.Desde, AnchoKael * 2f, "Por el agujero se cae sin rozar.");

            var campanaOeste = DisenoNivel2.Paredes.Single(p => p.Nombre == "CampanaOeste");
            var calle = DisenoNivel2.PisadaDe("SueloCalle").Y;
            Assert.GreaterOrEqual(campanaOeste.Abajo - calle, AltoKael + 0.5f, "Bajo el borde de la campana se pasa de pie.");

            var torreOeste = DisenoNivel2.Paredes.Single(p => p.Nombre == "TorreOeste");
            Assert.GreaterOrEqual(torreOeste.Abajo - calle, AltoKael + 0.5f, "A la torre se entra por debajo de su pared.");

            var repisa = DisenoNivel2.PisadaDe("RepisaDeLaCampana");
            Assert.GreaterOrEqual(repisa.Desde - campanaOeste.Hasta, AnchoKael * 2f, "Entre la campana y la repisa hay hueco para subir.");

            var ventanaAbajo = DisenoNivel2.Paredes.Single(p => p.Nombre == "TorreEsteBaja").Arriba;
            var ventanaArriba = DisenoNivel2.Paredes.Single(p => p.Nombre == "TorreEsteAlta").Abajo;
            Assert.AreEqual(DisenoNivel2.PisadaDe("Rellano3").Y, ventanaAbajo, 1e-4f, "Se sale del campanario andando.");
            Assert.GreaterOrEqual(ventanaArriba - ventanaAbajo, AltoKael + 0.5f, "Por la ventana cabe Kael.");
        }

        [Test]
        public void LaSuperficie_NoLlevaANingunaParte_SalvoHundirse()
        {
            var muralla = DisenoNivel2.Paredes.Single(p => p.Nombre == "Muralla");
            Assert.Greater(muralla.Arriba - DisenoNivel2.PisadaDe("Plaza").Y, AlturaDobleSalto(MEDIOS.SECO) * 2f, "La muralla no se salta.");
            Assert.AreEqual(DisenoNivel2.PisadaDe("Plaza").Hasta, DisenoNivel2.Agujero.Desde, 1e-4f, "El agujero está al final de la plaza.");
            Assert.AreEqual(muralla.Desde, DisenoNivel2.Agujero.Hasta, 1e-4f, "Y la muralla, justo detrás.");
            Assert.IsNotNull(DisenoNivel2.AguaEn((DisenoNivel2.Agujero.Desde + DisenoNivel2.Agujero.Hasta) * 0.5f, -2f), "Debajo del agujero hay agua.");
        }

        [Test]
        public void LaPlanidera_GuardaElTapon_EnLaPlazaEste_EntreElPuenteYElAgujero()
        {
            var planidera = DisenoNivel2.Enemigos.Single(e => e.Tipo == TIPOS.PLANIDERA);
            var puente = DisenoNivel2.PisadaDe("Puente");
            var medioAncho = GeneradorEnemigos.TamanoPlanidera.x * 0.5f;
            Assert.GreaterOrEqual(planidera.Desde - medioAncho, puente.Hasta, "Su ruta no se mete en el puente.");
            Assert.LessOrEqual(planidera.Hasta + medioAncho, DisenoNivel2.Agujero.Desde, "Ni en el tapón de hielo.");
            Assert.Greater(planidera.Hasta, planidera.Desde, "Flota de un lado a otro: tiene ruta.");
            Assert.AreEqual(DisenoNivel2.PisadaDe("Plaza").Y, planidera.Y, 1e-4f, "En el suelo de la plaza.");
            Assert.IsFalse(DisenoNivel2.Enemigos.Any(e => e.Tipo == TIPOS.ESCULTURA && e.X > puente.Hasta),
                "Ocupa el sitio de la segunda Escultura: en la plaza este no queda ninguna.");
            Assert.Greater(DisenoNivel2.Agujero.Desde - puente.Hasta, 5f, "La plaza este da sitio para esquivar las esquirlas.");
            Assert.Greater(puente.Y, 1.5f, "Y el puente, para ponerse a cubierto: tapa lo que hay detrás.");
        }

        [Test]
        public void LosEnemigos_SonLosDelAnexo_YCadaUnoEstaEnSuSitio()
        {
            Assert.AreEqual(3, DisenoNivel2.Enemigos.Count(e => e.Tipo == TIPOS.LAMENTO && e.X < 36f), "Anexo A: 3 Lamentos en la sección 1.");
            Assert.AreEqual(1, DisenoNivel2.Enemigos.Count(e => e.Tipo == TIPOS.ESCULTURA && e.X < 36f),
                "El Anexo dice 2 Esculturas Vivientes: la segunda dejó su sitio a la Plañidera (PENDIENTES N32).");
            Assert.AreEqual(1, DisenoNivel2.Enemigos.Count(e => e.Tipo == TIPOS.PLANIDERA), "Un mini-jefe, como el Golem del Mundo 1.");
            Assert.AreEqual(4, DisenoNivel2.Enemigos.Count(e => e.Tipo == TIPOS.MEDUSA && e.X >= 36f && e.X < 61f), "4 Medusas de Hielo bajo el agua.");

            foreach (var enemigo in DisenoNivel2.Enemigos.Where(e => e.Tipo != TIPOS.MEDUSA))
            {
                Assert.AreEqual(enemigo.Y, DisenoNivel2.SueloBajo(enemigo.X, enemigo.Y), 1e-4f, $"{enemigo.Nombre} de pie en su suelo.");
                Assert.AreEqual(DisenoNivel2.SueloBajo(enemigo.Desde, enemigo.Y), DisenoNivel2.SueloBajo(enemigo.Hasta, enemigo.Y), 1e-4f, $"La ruta de {enemigo.Nombre} no cambia de suelo.");
                Assert.That(enemigo.X, Is.InRange(enemigo.Desde, enemigo.Hasta), enemigo.Nombre);
                Assert.IsNull(DisenoNivel2.AguaEn(enemigo.X, enemigo.Y + 0.5f), $"{enemigo.Nombre} está en seco.");
            }

            foreach (var escultura in DisenoNivel2.Enemigos.Where(e => e.Tipo == TIPOS.ESCULTURA))
            {
                Assert.AreEqual(escultura.Desde, escultura.Hasta, $"{escultura.Nombre} no anda (decisión del 2026-10-08).");
            }

            foreach (var medusa in DisenoNivel2.Enemigos.Where(e => e.Tipo == TIPOS.MEDUSA))
            {
                var extremos = medusa.Vertical
                    ? new[] { new Vector2(medusa.X, medusa.Y - medusa.Amplitud), new Vector2(medusa.X, medusa.Y + medusa.Amplitud) }
                    : new[] { new Vector2(medusa.X - medusa.Amplitud, medusa.Y), new Vector2(medusa.X + medusa.Amplitud, medusa.Y) };
                foreach (var punto in extremos)
                {
                    Assert.IsNotNull(DisenoNivel2.AguaEn(punto.x, punto.y), $"{medusa.Nombre} no sale del agua ({punto}).");
                }
            }
        }

        [Test]
        public void LasMedusas_TienenRitmo_YVanALaContraDosADos()
        {
            var medusas = DisenoNivel2.Enemigos.Where(e => e.Tipo == TIPOS.MEDUSA).ToList();
            foreach (var medusa in medusas)
            {
                Assert.Greater(medusa.Periodo, 2f, $"{medusa.Nombre}: un ritmo que se pueda leer.");
                Assert.Greater(medusa.Amplitud, 1f, medusa.Nombre);
            }

            // Las de la calle y las de la torre, por parejas: cuando una está en un extremo, la otra en el contrario.
            foreach (var pareja in new[] { ("Medusa_1", "Medusa_2"), ("Medusa_3", "Medusa_4") })
            {
                var a = medusas.Single(m => m.Nombre == pareja.Item1);
                var b = medusas.Single(m => m.Nombre == pareja.Item2);
                Assert.AreEqual(a.Periodo, b.Periodo, 1e-4f, pareja.ToString());
                Assert.AreEqual(a.Periodo * 0.5f, Mathf.Abs(a.Desfase - b.Desfase), 1e-4f, $"{pareja}: a la contra.");
            }
        }

        [Test]
        public void EnLosRellanosDeLaTorre_LasMedusasNoRozanAKael_NiAtraviesanLasLosas()
        {
            // Esperar en un rellano tiene que ser seguro: el hueco para pasar se elige desde ahí.
            var torre = DisenoNivel2.Enemigos.Where(e => e.Tipo == TIPOS.MEDUSA && !e.Vertical).ToList();
            Assert.AreEqual(2, torre.Count);
            foreach (var medusa in torre)
            {
                var desde = medusa.X - medusa.Amplitud - DisenoNivel2.RadioMedusa;
                var hasta = medusa.X + medusa.Amplitud + DisenoNivel2.RadioMedusa;
                foreach (var losa in DisenoNivel2.Suelos.Where(b => b.Nombre.StartsWith("Rellano") && b.Hasta > desde && b.Desde < hasta))
                {
                    if (medusa.Y > losa.Arriba)
                    {
                        var hueco = medusa.Y - DisenoNivel2.RadioContactoMedusa - (losa.Arriba + DisenoNivel2.AltoKael);
                        Assert.GreaterOrEqual(hueco, 0.2f, $"{medusa.Nombre} pasa sobre {losa.Nombre} sin rozar la cabeza de Kael.");
                    }

                    var dentro = medusa.Y + DisenoNivel2.RadioMedusa > losa.Abajo && medusa.Y - DisenoNivel2.RadioMedusa < losa.Arriba;
                    Assert.IsFalse(dentro, $"{medusa.Nombre} no atraviesa {losa.Nombre}.");
                }
            }
        }

        [Test]
        public void LosCheckpoints_LaArenaYLaSalida_SonLosDelDiseno()
        {
            Assert.AreEqual(2, DisenoNivel2.Checkpoints.Length, "Dos checkpoints: la campana y la antesala (decisión del 2026-10-08).");
            var cp2 = DisenoNivel2.Checkpoints[1];
            Assert.That(cp2.x, Is.InRange(DisenoNivel2.PisadaDe("Antesala").Desde, DisenoNivel2.PisadaDe("Antesala").Hasta), "El segundo, en la antesala.");
            Assert.Less(cp2.x, DisenoNivel2.PuertaDesde, "Antes de la arena.");
            foreach (var punto in DisenoNivel2.Checkpoints)
            {
                Assert.IsFalse(DisenoNivel2.Enemigos.Any(e => Mathf.Abs(e.X - punto.x) < 2f && Mathf.Abs(e.Y - punto.y) < 3f), $"Sin enemigos encima del checkpoint de x {punto.x}.");
            }

            var limites = DisenoNivel2.LimitesArena;
            Assert.AreEqual(25f, limites.y - limites.x, 0.01f, "Como la del Nivel 1: 25 × 15 u.");
            Assert.AreEqual(15f, DisenoNivel2.TechoArena - DisenoNivel2.SueloArena, 0.01f);
            Assert.That(DisenoNivel2.Entrada, Is.InRange(limites.x, limites.y));
            Assert.That(DisenoNivel2.Jefe, Is.InRange(limites.x, limites.y));
            Assert.That(DisenoNivel2.Salida, Is.InRange(limites.x, limites.y));
            Assert.IsTrue(DisenoNivel2.Hielos.Any(h => h.Desde <= limites.x && h.Hasta >= limites.y), "Anexo A: el suelo de la arena resbala.");
        }

        [Test]
        public void LosBloques_EmpiezanYAcabanEnUnidadesEnteras()
        {
            foreach (var bloque in DisenoNivel2.Suelos.Concat(DisenoNivel2.Paredes).Concat(DisenoNivel2.PlataformasArena))
            {
                Assert.AreEqual(Mathf.Round(bloque.Desde), bloque.Desde, 1e-4f, bloque.Nombre);
                Assert.AreEqual(Mathf.Round(bloque.Hasta), bloque.Hasta, 1e-4f, bloque.Nombre);
            }
        }

        [Test]
        public void ElHielo_EstaSobreSuelosDeHielo_YNoSobreLaPiedra()
        {
            foreach (var hielo in DisenoNivel2.Hielos)
            {
                Assert.AreEqual(hielo.Abajo, DisenoNivel2.SueloBajo((hielo.Desde + hielo.Hasta) * 0.5f, hielo.Abajo), 1e-4f, $"{hielo.Nombre} apoya en su suelo.");
            }

            foreach (var piedra in new[] { "SueloInicio", "Puente", "Antesala" })
            {
                var pisada = DisenoNivel2.PisadaDe(piedra);
                Assert.IsFalse(DisenoNivel2.Hielos.Any(h => h.Desde < pisada.Hasta - 0.01f && h.Hasta > pisada.Desde + 0.01f && Mathf.Abs(h.Abajo - pisada.Y) < 0.01f),
                    $"{piedra} es de piedra: no resbala.");
            }
        }

        // --- La arena de la Doncella (fase D) ---

        [Test]
        public void LasFranjasDeLaArena_LaPartenEnTres_SinHuecosNiSolapes()
        {
            var franjas = DisenoNivel2.Franjas;
            var limites = DisenoNivel2.LimitesArena;
            Assert.AreEqual(3, franjas.Length);
            Assert.AreEqual(limites.x, franjas[0].Desde, 1e-4f, "Empiezan en la puerta.");
            Assert.AreEqual(limites.y, franjas[franjas.Length - 1].Hasta, 1e-4f, "Y acaban en la pared.");
            for (var i = 0; i < franjas.Length; i++)
            {
                var franja = franjas[i];
                if (i > 0)
                {
                    Assert.AreEqual(franjas[i - 1].Hasta, franja.Desde, 1e-4f, $"{franja.Nombre} empieza donde acaba la anterior.");
                }

                Assert.AreEqual(Mathf.Round(franja.Desde), franja.Desde, 1e-4f, $"{franja.Nombre}: bordes en unidades enteras.");
                Assert.AreEqual(Mathf.Round(franja.Hasta), franja.Hasta, 1e-4f, franja.Nombre);
                Assert.AreEqual(DisenoNivel2.SueloArena, franja.Abajo, 1e-4f, $"{franja.Nombre}: el agua sale del suelo de la arena.");
                Assert.AreEqual(DisenoNivel2.SueloArena + DisenoNivel2.AguaDeLaInundacion, franja.Arriba, 1e-4f, franja.Nombre);
            }

            // A y C llevan una placa dentro; B queda bajo el balcón, que nunca se inunda.
            var placa1 = DisenoNivel2.PisadaDe("PlacaArena1");
            var placa2 = DisenoNivel2.PisadaDe("PlacaArena2");
            var balcon = DisenoNivel2.PlataformasArena.Single(p => p.Nombre == "Balcon");
            Assert.IsTrue(placa1.Desde >= franjas[0].Desde && placa1.Hasta <= franjas[0].Hasta, "La primera placa, en la franja A.");
            Assert.IsTrue(placa2.Desde >= franjas[2].Desde && placa2.Hasta <= franjas[2].Hasta, "La segunda, en la C.");
            Assert.IsTrue(balcon.Desde >= franjas[1].Desde && balcon.Hasta <= franjas[1].Hasta, "El balcón, sobre la B.");
            Assert.Greater(balcon.Abajo, DisenoNivel2.SueloArena + DisenoNivel2.CapaHelada, "El balcón queda por encima del agua y de su hielo.");
        }

        [Test]
        public void LaCapaHelada_SeSubeConElDobleSalto_YSaliendoDelAguaDeAlLado()
        {
            var capa = DisenoNivel2.CapaHelada;
            Assert.AreEqual(capa - DisenoNivel2.BajoLaSuperficie, DisenoNivel2.AguaDeLaInundacion, 1e-4f,
                "El agua llega casi a ras de lo que deja al congelarse, como las demás aguas.");
            Assert.Greater(capa, AlturaSalto(MEDIOS.SECO), "Con un salto solo no se sube: hay que querer subirse.");
            Assert.LessOrEqual(capa, AlturaDobleSalto(MEDIOS.SECO) * MargenAltura, "Desde el hielo seco, con el doble salto y margen.");

            // Desde otra franja inundada: lo que sobra del salto al cruzar la superficie.
            var sobre = Gravedad(MEDIOS.AGUA) / Gravedad(MEDIOS.SECO);
            var sobrante = (AlturaDobleSalto(MEDIOS.AGUA) - DisenoNivel2.AguaDeLaInundacion) * sobre - DisenoNivel2.BajoLaSuperficie;
            Assert.GreaterOrEqual(sobrante, MargenAlSalirDelAgua, "Y saliendo del agua de la franja de al lado.");

            var balcon = DisenoNivel2.PlataformasArena.Single(p => p.Nombre == "Balcon");
            Assert.GreaterOrEqual(balcon.Abajo - (DisenoNivel2.SueloArena + capa), AltoKael + 0.25f, "De pie en la capa, Kael cabe bajo el balcón.");
            Assert.Less(DisenoNivel2.GrosorCapaHelada, capa - AltoKael, "Y bajo la capa, de pie en el suelo.");
        }

        [Test]
        public void LaDoncella_FlotaPorEncimaDeKael_YPorDebajoDelBalcon()
        {
            var jefe = GeneradorJefes.CargarDatos(VerificadorDoncella.NombreDatos);
            Assert.IsNotNull(jefe);
            var pies = DisenoNivel2.SueloArena + DisenoNivel2.VueloDoncella;
            Assert.Greater(DisenoNivel2.VueloDoncella, AltoKael, "Flota por encima de la cabeza de Kael.");
            var balcon = DisenoNivel2.PlataformasArena.Single(p => p.Nombre == "Balcon");
            Assert.LessOrEqual(pies + jefe.tamano.y, balcon.Abajo, "Y entera por debajo del balcón.");
        }

        private static DisenoNivel2.Zona Agua(string nombre) => DisenoNivel2.Aguas.Single(a => a.Nombre == nombre);

        // El agua en la que despega el salto: en el punto de la pisada de salida más cercano a la de
        // llegada (la calle tiene un agua distinta bajo la campana y en la torre).
        private static DisenoNivel2.Zona? AguaDelSalto(DisenoNivel1.Pisada desde, DisenoNivel1.Pisada hasta)
        {
            var x = Mathf.Clamp((hasta.Desde + hasta.Hasta) * 0.5f, desde.Desde + 0.1f, desde.Hasta - 0.1f);
            return DisenoNivel2.AguaEn(x, desde.Y + 0.1f);
        }
    }
}
