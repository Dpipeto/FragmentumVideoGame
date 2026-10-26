using System.Collections.Generic;
using System.Linq;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MEDIOS = Fragmentum.EditorTools.DisenoNivel2.MEDIOS;
using TIPOS = Fragmentum.EditorTools.DisenoNivel4.TIPOS_ENEMIGO;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El recorrido de diseño del Nivel 4 (<see cref="DisenoNivel4"/>) se cruza por los tejados y por la
    /// calle; la cobertura para los disparos solo a Kael agachado tras ella; la tierra de nadie se cruza
    /// invisible y, sin la Invisibilidad, llega al menos una ráfaga; el balcón del campanario no se
    /// alcanza; y el río se cruza bajo el agua, pero sus alambradas solo con el Dash Acuático. Lo
    /// comprueba sobre los datos; <see cref="EscenaNivel4Tests"/> comprueba que la escena los sigue.
    /// </summary>
    public class DisenoNivel4Tests
    {
        // Los mismos márgenes que en los otros niveles: nunca se pide el límite exacto.
        private const float MargenAlcance = 0.85f;
        private const float MargenAltura = 0.9f;

        /// <summary>Lo que se pide de más a la línea de tiro sobre o bajo el borde de una cobertura: la bala tiene grosor.</summary>
        private const float RadioBala = 0.1f;

        private static readonly ParametrosJugador Kael = new ParametrosJugador();

        private static float Gravedad => Mathf.Abs(Physics2D.gravity.y) * Kael.escalaGravedad;
        private static float AlturaSalto => Kael.velocidadSalto * Kael.velocidadSalto / (2f * Gravedad);
        private static float AlturaDobleSalto => AlturaSalto + Kael.velocidadDobleSalto * Kael.velocidadDobleSalto / (2f * Gravedad);

        // Bajo el agua, como en DisenoNivel2Tests y en la ciénaga del Nivel 3.
        private const float MargenAlSalirDelAgua = 0.25f;

        private static ModificadorMovimiento Agua => ModificadorMovimiento.BajoElAgua;
        private static float GravedadAgua => Gravedad * Agua.gravedad;
        private static float AlturaDobleSaltoAgua =>
            Mathf.Pow(Kael.velocidadSalto * Agua.salto, 2f) / (2f * GravedadAgua) + Mathf.Pow(Kael.velocidadDobleSalto * Agua.salto, 2f) / (2f * GravedadAgua);

        /// <summary>Lo que recorre un dash: no lo frena el agua (fija su propia velocidad).</summary>
        private static float Dash => Kael.velocidadDash * Kael.dashDuration;

        private static DisenoNivel2.Zona AguaBajo(float x) => DisenoNivel4.Aguas.Single(a => x >= a.Desde && x <= a.Hasta);

        /// <summary>Distancia que avanza Kael corriendo con el doble salto hasta caer en algo <paramref name="subida"/> más alto.</summary>
        private static float AlcanceCorriendo(float subida)
        {
            var caida = Gravedad * Kael.multiplicadorGravedadCaida;
            var tiempo = Kael.velocidadSalto / Gravedad + Kael.velocidadDobleSalto / Gravedad +
                         Mathf.Sqrt(2f * Mathf.Max(0f, AlturaDobleSalto - subida) / caida);
            return Kael.velocidadMaxima * tiempo;
        }

        /// <summary>Lo mismo con un solo salto: el que se da entre tejados.</summary>
        private static float AlcanceDeUnSalto(float subida)
        {
            var caida = Gravedad * Kael.multiplicadorGravedadCaida;
            var tiempo = Kael.velocidadSalto / Gravedad + Mathf.Sqrt(2f * Mathf.Max(0f, AlturaSalto - subida) / caida);
            return Kael.velocidadMaxima * tiempo;
        }

        private static float Aire()
        {
            var objeto = new GameObject("Aire");
            try
            {
                return new SerializedObject(objeto.AddComponent<OxigenoJugador>()).FindProperty("capacidad").floatValue;
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        // --- Kael y sus saltos ---

        [Test]
        public void LasMedidasDeKael_SonLasDelDocumento()
        {
            Assert.AreEqual(DisenoNivel4.AltoKael * Kael.alturaAgachado, DisenoNivel4.AltoKaelAgachado, 1e-4f, "Agachado, el 60 %: 1,08 u.");
            Assert.AreEqual(2f, Kael.velocidadAgachado, 1e-4f, "Agachado avanza a 2 u/s.");
            Assert.AreEqual(2.4f, AlturaSalto, 0.1f, "Un salto sube 2,4 u…");
            Assert.AreEqual(4.1f, AlturaDobleSalto, 0.1f, "…y el doble salto, 4,1.");
            Assert.AreEqual(4.4f, AlcanceDeUnSalto(0f), 0.1f, "Corriendo, un salto cruza unos 4,4 u.");
            Assert.AreEqual(DisenoNivel4.DuracionInvisibilidad, new PlayerAbilitiesMedidas().Duracion, 1e-4f, "La Invisibilidad dura 3 s.");
        }

        [Test]
        public void CadaPaso_SeDaCorriendo()
        {
            // Los del río se dan bajo el agua: los comprueba ElRio_SeSaleDelAgua_AlPilarYALaOtraOrilla.
            foreach (var paso in DisenoNivel4.Pasos.Where(p => p.Medio == MEDIOS.SECO))
            {
                var desde = DisenoNivel4.PisadaDe(paso.Desde);
                var hasta = DisenoNivel4.PisadaDe(paso.Hasta);
                var subida = hasta.Y - desde.Y;
                var hueco = Mathf.Max(hasta.Desde - desde.Hasta, desde.Desde - hasta.Hasta, 0f);
                Assert.LessOrEqual(subida, AlturaDobleSalto * MargenAltura, $"{paso}: demasiado alto.");
                Assert.LessOrEqual(hueco, AlcanceCorriendo(Mathf.Max(0f, subida)) * MargenAlcance, $"{paso}: no se llega corriendo.");
            }
        }

        [Test]
        public void LosHuecosEntreTejados_SeSaltanCorriendo_ConUnSalto()
        {
            var tejados = DisenoNivel4.Tejados.OrderBy(t => t.Desde).ToList();
            Assert.AreEqual(3, tejados.Count);
            for (var i = 1; i < tejados.Count; i++)
            {
                var hueco = tejados[i].Desde - tejados[i - 1].Hasta;
                Assert.That(hueco, Is.InRange(2f, 3f), "Huecos de 2 y 3 u.");
                Assert.LessOrEqual(hueco, AlcanceDeUnSalto(0f) * MargenAlcance, $"Del {tejados[i - 1].Nombre} al {tejados[i].Nombre}, con un salto.");
                Assert.AreEqual(tejados[i - 1].Arriba, tejados[i].Arriba, 1e-4f, "A la misma altura.");
            }

            // Desde la calle no se sube a los tejados: solo por los escombros de la bifurcación.
            Assert.Greater(tejados[0].Abajo - DisenoNivel4.PisadaDe("Calle").Y, AlturaDobleSalto + DisenoNivel4.AltoKael, "Bajo los tejados, la calle.");
            Assert.Greater(DisenoNivel4.AlturaTejados - DisenoNivel4.PisadaDe("Convergencia").Y, AlturaDobleSalto,
                "Desde la convergencia no se sube al último tejado.");
            Assert.Greater(DisenoNivel4.AlturaTejados - DisenoNivel4.PisadaDe("Bifurcacion").Y, AlturaDobleSalto,
                "Desde el suelo de la bifurcación tampoco: hay que subir a los escombros.");
        }

        [Test]
        public void PorLosTejados_YPorLaCalle_SeLlegaALaArena()
        {
            var tejados = DisenoNivel4.Tejados.Select(t => t.Nombre).ToList();
            var porArriba = Alcanzadas(new[] { "Calle", "EscalonDeLaCalle" });
            var porAbajo = Alcanzadas(tejados.Append("Escombros"));
            Assert.IsTrue(porArriba.Contains("SueloArena"), "Por los tejados se llega a la arena.");
            Assert.IsTrue(porAbajo.Contains("SueloArena"), "Por la calle, también.");
            Assert.IsTrue(porAbajo.Contains("Convergencia") && porArriba.Contains("Convergencia"), "Las dos rutas llegan a la convergencia.");
            Assert.IsFalse(Alcanzadas(new string[0]).Contains(DisenoNivel4.Balcon.Nombre), "Al balcón no se llega.");
        }

        // Lo que se alcanza desde el inicio con los pasos y dejándose caer, sin pisar lo prohibido.
        private static HashSet<string> Alcanzadas(IEnumerable<string> prohibidas)
        {
            var fuera = new HashSet<string>(prohibidas);
            var alcanzadas = new HashSet<string> { "SueloInicio" };
            var cambia = true;
            while (cambia)
            {
                cambia = false;
                foreach (var paso in DisenoNivel4.Pasos.Where(p => alcanzadas.Contains(p.Desde) && !fuera.Contains(p.Hasta)))
                {
                    cambia |= alcanzadas.Add(paso.Hasta);
                }

                foreach (var desde in alcanzadas.ToList())
                {
                    var arriba = DisenoNivel4.PisadaDe(desde);
                    foreach (var abajo in DisenoNivel4.Suelos.Concat(DisenoNivel4.Tejados).Where(b => !fuera.Contains(b.Nombre) && b.Arriba < arriba.Y))
                    {
                        if (abajo.Desde <= arriba.Hasta + 0.01f && abajo.Hasta >= arriba.Desde - 0.01f)
                        {
                            cambia |= alcanzadas.Add(abajo.Nombre);
                        }
                    }
                }
            }

            return alcanzadas;
        }

        [Test]
        public void LosBloques_EmpiezanYAcabanEnUnidadesEnteras()
        {
            // El balcón no: como las coberturas, lo dibuja su pieza (el campanario), no el tileset, y empieza
            // donde lo pinta su arte.
            foreach (var bloque in DisenoNivel4.Suelos.Concat(DisenoNivel4.Paredes).Concat(DisenoNivel4.Tejados))
            {
                Assert.AreEqual(Mathf.Round(bloque.Desde), bloque.Desde, 1e-4f, bloque.Nombre);
                Assert.AreEqual(Mathf.Round(bloque.Hasta), bloque.Hasta, 1e-4f, bloque.Nombre);
            }
        }

        // --- La cobertura y los disparos ---

        [Test]
        public void LasCoberturas_MidenLoQueDiceElDocumento_YSeSubenConUnSalto()
        {
            Assert.Greater(DisenoNivel4.AltoCobertura, DisenoNivel4.AltoKaelAgachado, "Agachado, Kael queda por debajo del borde…");
            Assert.Less(DisenoNivel4.AltoCobertura, DisenoNivel4.AltoKael, "…y de pie asoma la cabeza.");
            foreach (var cobertura in DisenoNivel4.Coberturas)
            {
                // Los sacos de los Artilleros, más bajos: su fusil dispara por encima (decidido el 2026-10-21).
                Assert.AreEqual(DisenoNivel4.AltoDe(cobertura.Nombre), cobertura.Arriba - cobertura.Abajo, 1e-4f, cobertura.Nombre);
                Assert.AreEqual(cobertura.Abajo, DisenoNivel4.SueloBajo((cobertura.Desde + cobertura.Hasta) * 0.5f, cobertura.Abajo), 1e-4f,
                    $"{cobertura.Nombre}, de pie en su suelo.");
                Assert.LessOrEqual(DisenoNivel4.AltoCobertura, AlturaSalto * MargenAltura, $"{cobertura.Nombre} se salta.");
            }
        }

        [Test]
        public void LosArtilleros_SeArrodillanTrasSusSacos()
        {
            foreach (var artillero in DisenoNivel4.Enemigos.Where(e => e.Tipo == TIPOS.ARTILLERO && !e.EnElCampanario))
            {
                var sacos = DisenoNivel4.Coberturas.SingleOrDefault(c => c.Nombre.StartsWith("SacosDelArtillero") &&
                                                                         c.Hasta < artillero.X && artillero.X - c.Hasta < 0.5f);
                Assert.IsTrue(sacos.Nombre != null, $"{artillero.Nombre} tiene sus sacos delante.");
                Assert.AreEqual(artillero.Y, sacos.Abajo, 1e-4f, $"{artillero.Nombre}: en su mismo suelo.");
                Assert.Greater(artillero.Canon.y, sacos.Arriba, $"{artillero.Nombre}: su cañón asoma por encima…");
                Assert.That(artillero.Canon.x, Is.InRange(sacos.Desde - 0.3f, sacos.Desde - 0.01f), $"{artillero.Nombre}: …y por delante de sus sacos.");
            }
        }

        [Test]
        public void TrasCadaCobertura_SoloAgachadoNoLeAlcanzan()
        {
            var comprobadas = 0;
            foreach (var artillero in DisenoNivel4.Enemigos.Where(e => e.Tipo == TIPOS.ARTILLERO && !e.EnElCampanario))
            {
                var delante = DisenoNivel4.Coberturas.Where(c => Mathf.Approximately(c.Abajo, artillero.Y) && c.Hasta < artillero.X - 0.5f &&
                                                                 artillero.X - c.Desde <= DisenoNivel4.VisionArtillero).ToList();
                foreach (var cobertura in delante)
                {
                    // Kael pegado a la cobertura, por el lado del que viene.
                    var kael = cobertura.Desde - DisenoNivel4.AnchoKael * 0.5f - 0.05f;
                    var agachado = new Vector2(kael, artillero.Y + DisenoNivel4.AltoKaelAgachado - DisenoNivel4.BajoLaCabeza);
                    var dePie = new Vector2(kael, artillero.Y + DisenoNivel4.AltoKael - DisenoNivel4.BajoLaCabeza);

                    // Lo más alto de la línea sobre la cobertura es su borde del lado del Artillero.
                    Assert.Less(AlturaDeLaLinea(artillero.Canon, agachado, cobertura.Hasta), cobertura.Arriba - RadioBala,
                        $"{cobertura.Nombre} contra {artillero.Nombre}: agachado, la bala da en la cobertura.");
                    Assert.Greater(AlturaDeLaLinea(artillero.Canon, dePie, cobertura.Hasta), cobertura.Arriba + RadioBala,
                        $"{cobertura.Nombre} contra {artillero.Nombre}: de pie, la bala pasa por encima y le da en la cabeza.");
                    comprobadas++;
                }
            }

            Assert.GreaterOrEqual(comprobadas, 5, "Los tres escombros del campo y las dos chimeneas.");

            // Al descubierto, agachado también le da: apunta a la cabeza.
            var campo = DisenoNivel4.Enemigos.Single(e => e.Nombre == "Artillero_1");
            var alDescubierto = new Vector2(41f, DisenoNivel4.AltoKaelAgachado - DisenoNivel4.BajoLaCabeza);
            Assert.IsFalse(DisenoNivel4.Coberturas.Any(c => c.Desde > alDescubierto.x && c.Hasta < campo.Canon.x &&
                                                            AlturaDeLaLinea(campo.Canon, alDescubierto, c.Hasta) < c.Arriba),
                "Agachado entre el último escombro y el Artillero, la línea de tiro no la para nada.");
        }

        // La altura de la recta de a a b en x.
        private static float AlturaDeLaLinea(Vector2 a, Vector2 b, float x) => Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x));

        [Test]
        public void LaTierraDeNadie_SeCruzaInvisible_YSinEllaLlegaUnaRafaga()
        {
            var refugio = DisenoNivel4.Coberturas.Single(c => c.Nombre == "SacosDelRefugio");
            var torre = DisenoNivel4.Paredes.Single(p => p.Nombre == "TorreDelCampanario");
            var descubierto = torre.Desde - refugio.Hasta;
            Assert.That(descubierto, Is.InRange(13f, 14f), "Unas 13 u al descubierto.");
            Assert.IsFalse(DisenoNivel4.Coberturas.Any(c => c.Desde > refugio.Hasta && c.Hasta < torre.Desde), "Sin una sola cobertura.");

            var cruzar = descubierto / Kael.velocidadMaxima;
            Assert.Less(cruzar, DisenoNivel4.DuracionInvisibilidad, "Corriendo, se cruza mientras dura la Invisibilidad.");

            // Los dos del campanario se turnan: entre una ráfaga y la siguiente pasa menos de lo que se tarda en cruzar.
            var campanario = DisenoNivel4.Enemigos.Where(e => e.EnElCampanario).OrderBy(e => e.Turno).ToList();
            Assert.AreEqual(2, campanario.Count);
            Assert.AreEqual(DisenoNivel4.TurnoDelCampanario, campanario[1].Turno - campanario[0].Turno, 1e-4f);
            var ciclo = DisenoNivel4.ApuntaArtillero + (DisenoNivel4.BalasPorRafaga - 1) * DisenoNivel4.EntreBalas + DisenoNivel4.RecargaArtillero;
            var entreRafagas = Mathf.Max(DisenoNivel4.TurnoDelCampanario, ciclo - DisenoNivel4.TurnoDelCampanario);
            Assert.Less(entreRafagas, cruzar, "Sin la Invisibilidad, al menos una ráfaga llega.");

            // Ven toda la tierra de nadie, pero no a quien espera tras los sacos del refugio.
            foreach (var artillero in campanario)
            {
                Assert.LessOrEqual(artillero.X - refugio.Hasta, DisenoNivel4.VisionArtillero, $"{artillero.Nombre} ve desde los sacos del refugio.");
                Assert.Greater(artillero.X - (refugio.Desde - DisenoNivel4.AnchoKael * 0.5f), DisenoNivel4.VisionArtillero,
                    $"{artillero.Nombre} no ve a Kael tras los sacos del refugio.");
                Assert.Greater(artillero.X - DisenoNivel4.Checkpoints[0].x, DisenoNivel4.VisionArtillero, $"{artillero.Nombre} no ve el refugio.");
            }
        }

        [Test]
        public void ElBalconDelCampanario_NoSeAlcanza_YBajoElArcoPasaKael()
        {
            var balcon = DisenoNivel4.Balcon;
            var torre = DisenoNivel4.Paredes.Single(p => p.Nombre == "TorreDelCampanario");
            Assert.AreEqual(torre.Desde, balcon.Hasta, 1e-4f, "El balcón sale de la torre.");
            Assert.IsTrue(DisenoNivel4.NoAgarra(torre.Nombre), "La torre no agarra: no se sube con el salto de pared.");
            Assert.Greater(torre.Arriba, AlturaDobleSalto + DisenoNivel4.AlturaTejados, "Ni se salta.");

            foreach (var pisada in DisenoNivel4.TodoLoQueSePisa)
            {
                // Desde la derecha, la torre se interpone.
                if (pisada.Desde >= torre.Hasta)
                {
                    continue;
                }

                var subida = balcon.Arriba - pisada.Arriba;
                var hueco = Mathf.Max(balcon.Desde - pisada.Hasta, pisada.Desde - balcon.Hasta, 0f);
                Assert.IsTrue(subida > AlturaDobleSalto || hueco > AlcanceCorriendo(Mathf.Max(0f, subida)),
                    $"Desde {pisada.Nombre} no se llega al balcón (sube {subida:0.##} u a {hueco:0.##} u).");
            }

            // Bajo el arco cabe Kael; y al salir, los escombros dejan sitio para saltar a ellos sin la torre encima.
            Assert.GreaterOrEqual(torre.Abajo - DisenoNivel4.PisadaDe("BajoElCampanario").Y, DisenoNivel4.HuecoBajoUnTecho);
            Assert.GreaterOrEqual(DisenoNivel4.PisadaDe("Escombros").Desde - torre.Hasta, DisenoNivel4.AnchoKael, "Pasado el arco hay sitio para saltar.");
            Assert.LessOrEqual(DisenoNivel4.PisadaDe("Escombros").Y, AlturaSalto * MargenAltura, "A los escombros, con un salto.");
        }

        [Test]
        public void LosArtillerosDelCampanario_EstanEnSuBalcon_YBajoElArcoNoVenAKael()
        {
            var balcon = DisenoNivel4.Balcon;
            var torre = DisenoNivel4.Paredes.Single(p => p.Nombre == "TorreDelCampanario");
            foreach (var artillero in DisenoNivel4.Enemigos.Where(e => e.EnElCampanario))
            {
                Assert.AreEqual(balcon.Arriba, artillero.Y, 1e-4f, $"{artillero.Nombre}, en el balcón.");
                Assert.That(artillero.X, Is.InRange(balcon.Desde, balcon.Hasta), artillero.Nombre);
                Assert.Less(artillero.X, torre.Desde, "Miran a la tierra de nadie: el arco queda a su espalda.");
            }
        }

        // --- El río ---

        [Test]
        public void ElRio_SeSaleDelAgua_AlPilarYALaOtraOrilla()
        {
            var pasos = DisenoNivel4.Pasos.Where(p => p.Medio == MEDIOS.AGUA).ToList();
            Assert.AreEqual(2, pasos.Count, "Del primer ojo al pilar, y del segundo a la otra orilla.");
            foreach (var paso in pasos)
            {
                var desde = DisenoNivel4.PisadaDe(paso.Desde);
                var hasta = DisenoNivel4.PisadaDe(paso.Hasta);
                var agua = AguaBajo((desde.Desde + desde.Hasta) * 0.5f);
                Assert.AreEqual(agua.Abajo, desde.Y, 1e-4f, $"{paso}: sale del fondo del agua.");
                Assert.AreEqual(agua.Arriba + DisenoNivel4.BajoLaSuperficie, hasta.Y, 1e-4f, $"{paso}: a ras del agua (por encima, se respira).");
                Assert.LessOrEqual(hasta.Y - desde.Y, AlturaDobleSaltoAgua * MargenAltura, $"{paso}: con el doble salto bajo el agua.");
                Assert.AreEqual(0f, Mathf.Max(hasta.Desde - desde.Hasta, desde.Desde - hasta.Hasta, 0f), 1e-4f, $"{paso}: junto al agua.");

                var sobrante = (AlturaDobleSaltoAgua - (agua.Arriba - desde.Y)) * Agua.gravedad - DisenoNivel4.BajoLaSuperficie;
                Assert.GreaterOrEqual(sobrante, MargenAlSalirDelAgua, $"{paso}: al cruzar la superficie no llega al borde.");
            }

            // El pilar queda fuera del agua: encima se respira.
            var pilar = DisenoNivel4.PisadaDe("Pilar");
            Assert.IsFalse(DisenoNivel4.Aguas.Any(a => a.Desde < pilar.Hasta && a.Hasta > pilar.Desde), "Sobre el pilar no hay agua.");
            Assert.AreEqual(DisenoNivel4.Aguas[0].Hasta, pilar.Desde, 1e-4f);
            Assert.AreEqual(DisenoNivel4.Aguas[1].Desde, pilar.Hasta, 1e-4f);
            Assert.AreEqual(DisenoNivel4.PisadaDe("Orilla").Hasta, DisenoNivel4.Aguas[0].Desde, 1e-4f, "Se entra desde la orilla, dejándose caer.");
            Assert.AreEqual(DisenoNivel4.PisadaDe("OtraOrilla").Desde, DisenoNivel4.Aguas[1].Hasta, 1e-4f);

            // De cada ojo se sale al aire antes del pilar o de la otra orilla.
            foreach (var restos in DisenoNivel4.RestosDelPuente)
            {
                var agua = AguaBajo(restos.Desde);
                Assert.GreaterOrEqual(agua.Hasta - restos.Hasta, 1f, $"{restos.Nombre}: entre su final y la orilla hay agua abierta para saltar.");
                Assert.GreaterOrEqual(restos.Desde - agua.Desde, 1f, $"{restos.Nombre}: y antes, para meterse.");
            }
        }

        [Test]
        public void BajoLosRestosDelPuente_CabeKael_YNoSeSaltanPorArriba()
        {
            var restos = DisenoNivel4.RestosDelPuente.ToList();
            Assert.AreEqual(2, restos.Count, "El techo de los dos ojos.");
            foreach (var techo in restos)
            {
                Assert.IsTrue(DisenoNivel4.NoAgarra(techo.Nombre), $"{techo.Nombre}: no agarran.");
                foreach (var x in new[] { techo.Desde + 0.05f, (techo.Desde + techo.Hasta) * 0.5f, techo.Hasta - 0.05f })
                {
                    var fondo = DisenoNivel4.SueloBajo(x, techo.Abajo - 0.01f);
                    Assert.AreEqual(DisenoNivel4.FondoRio, fondo, 1e-4f, $"{techo.Nombre} en x {x:0.##}: debajo, el fondo del río.");
                    Assert.GreaterOrEqual(techo.Abajo - fondo, DisenoNivel4.HuecoBajoUnTecho, $"{techo.Nombre} en x {x:0.##}: Kael no pasa por debajo.");
                }

                Assert.Less(techo.Abajo, DisenoNivel4.Aguas[0].Arriba, $"{techo.Nombre}: bajo él no se respira.");
                Assert.Greater(techo.Arriba, AlturaDobleSalto, $"{techo.Nombre}: desde la orilla o el pilar no se salta por encima.");
                Assert.Greater(techo.Arriba - DisenoNivel4.FondoRio, AlturaDobleSaltoAgua, $"{techo.Nombre}: ni desde el fondo del agua.");
                Assert.AreEqual(1, DisenoNivel4.Aguas.Count(a => techo.Desde >= a.Desde && techo.Hasta <= a.Hasta), $"{techo.Nombre}: sobre el agua.");
            }
        }

        [Test]
        public void LasAlambradas_LlenanSuOjo_YSoloSeCruzanConUnDash()
        {
            Assert.AreEqual(2.88f, Dash, 0.01f, "Un dash recorre 2,9 u, también bajo el agua.");
            var nadando = Kael.velocidadMaxima * Agua.velocidad * ModificadorMovimiento.Marana.velocidad;
            var aire = Aire();

            Assert.AreEqual(2, DisenoNivel4.Alambradas.Length);
            foreach (var alambrada in DisenoNivel4.Alambradas)
            {
                var techo = DisenoNivel4.RestosDelPuente.Single(p => alambrada.Desde > p.Desde && alambrada.Hasta < p.Hasta);
                Assert.AreEqual(DisenoNivel4.FondoRio, alambrada.Abajo, 1e-4f, $"{alambrada.Nombre}: del fondo…");
                Assert.AreEqual(techo.Abajo, alambrada.Arriba, 1e-4f, $"{alambrada.Nombre}: …al techo de su ojo: no se salta por encima.");
                Assert.GreaterOrEqual(alambrada.Desde - techo.Desde, 1f, $"{alambrada.Nombre}: se llega a ella ya dentro del ojo.");
                Assert.IsNotNull(AguaBajo(alambrada.Desde), $"{alambrada.Nombre}: bajo el agua.");

                var recorrido = alambrada.Hasta - alambrada.Desde + DisenoNivel4.AnchoKael;
                Assert.Greater(recorrido / nadando, aire, $"{alambrada.Nombre}: sin el dash se acaba el aire antes de cruzarla.");
                Assert.LessOrEqual(recorrido, Dash, $"{alambrada.Nombre}: un dash la cruza, aun desde el borde.");
            }
        }

        [Test]
        public void LaMina_DejaPasarPorDebajoAlSubir_YPorEncimaAlBajar()
        {
            Assert.AreEqual(1, DisenoNivel4.Minas.Length);
            var mina = DisenoNivel4.Minas[0];
            var techo = DisenoNivel4.RestosDelPuente.Single(p => mina.x > p.Desde && mina.x < p.Hasta);
            Assert.AreEqual(DisenoNivel4.TechoSegundoOjo, techo.Abajo, 1e-4f, "En el segundo ojo, el alto.");
            Assert.AreEqual(DisenoNivel4.FondoRio, mina.y, 1e-4f, "Sujeta al fondo.");
            Assert.Less(mina.x + DisenoNivel4.RadioBolaMina, DisenoNivel4.Alambradas[1].Desde, "Antes de la alambrada.");

            var abajo = mina.y + DisenoNivel4.MinaSobreElFondo;
            var arriba = abajo + DisenoNivel4.RecorridoMina;
            var holgura = 0.2f;
            Assert.GreaterOrEqual(arriba - DisenoNivel4.RadioBolaMina - DisenoNivel4.FondoRio, DisenoNivel4.AltoKael + holgura,
                "Arriba, por debajo cabe Kael.");
            Assert.GreaterOrEqual(techo.Abajo - (abajo + DisenoNivel4.RadioBolaMina), DisenoNivel4.AltoKael + holgura,
                "Abajo, por encima cabe Kael.");
            Assert.Less(arriba + DisenoNivel4.RadioBolaMina, techo.Abajo, "Arriba no toca el techo.");
            Assert.Greater(AlturaDobleSaltoAgua, abajo + DisenoNivel4.RadioBolaMina - DisenoNivel4.FondoRio, "Bajo el agua se salta por encima.");
        }

        [Test]
        public void CadaTramoDelRio_SeCruzaConElAireQueHay()
        {
            var aire = Aire();
            var nadando = Kael.velocidadMaxima * Agua.velocidad;

            // De la orilla al pilar, y del pilar a la otra orilla, esperando a la mina una vuelta entera.
            var primero = DisenoNivel4.Aguas[0].Hasta - DisenoNivel4.Aguas[0].Desde;
            var segundo = DisenoNivel4.Aguas[1].Hasta - DisenoNivel4.Aguas[1].Desde;
            Assert.Less(primero / nadando, aire * 0.5f, "El primer tramo cuesta menos de la mitad del aire.");
            Assert.Less(segundo / nadando + DisenoNivel4.PeriodoMina, aire * 0.8f, "El segundo, con la espera de la mina, deja margen.");
        }

        [Test]
        public void LaLineaDelRio_EstaEnLaOrilla_YElCP2AUnosPasosDelAgua()
        {
            var linea = DisenoNivel4.LineaDelRio;
            var cp2 = DisenoNivel4.Checkpoints[1];
            Assert.Greater(linea.Desde, cp2.x, "Pasado el CP2…");
            Assert.AreEqual(DisenoNivel4.Aguas[0].Desde, linea.Hasta, 1e-4f, "…y justo antes del agua.");
            Assert.LessOrEqual(DisenoNivel4.Aguas[0].Desde - cp2.x, 4f, "Quien se ahoga vuelve a unos pasos del agua.");
            Assert.IsFalse(DisenoNivel4.Enemigos.Any(e => e.Hasta > DisenoNivel4.Aguas[0].Desde && e.Desde < DisenoNivel4.Aguas[1].Hasta),
                "En el río no hay enemigos: la mina basta.");
        }

        // --- Los barriles, las memorias y las pistas ---

        [Test]
        public void LosBarriles_SeEncadenanEnElTejado_YNoEnElPatio()
        {
            foreach (var barril in DisenoNivel4.Barriles)
            {
                Assert.AreEqual(barril.y, DisenoNivel4.SueloBajo(barril.x, barril.y), 1e-4f, $"El barril de x {barril.x}, en su suelo.");
            }

            List<Vector2> Vecinos(Vector2 barril) => DisenoNivel4.Barriles.Where(b => b != barril && Vector2.Distance(b, barril) < DisenoNivel4.CadenaBarril).ToList();

            var tejado = DisenoNivel4.Barriles.Where(b => Mathf.Approximately(b.y, DisenoNivel4.AlturaTejados)).OrderBy(b => b.x).ToList();
            Assert.AreEqual(4, tejado.Count, "Cuatro en el último tejado.");
            for (var i = 1; i < tejado.Count; i++)
            {
                Assert.Less(tejado[i].x - tejado[i - 1].x, DisenoNivel4.CadenaBarril, "Uno enciende al siguiente: una cadena.");
            }

            var patio = DisenoNivel4.LimitesPatio;
            var delPatio = DisenoNivel4.Barriles.Where(b => b.x > patio.x && b.x < patio.y).ToList();
            Assert.AreEqual(4, delPatio.Count, "Cuatro en el patio del Verdugo.");
            Assert.IsTrue(delPatio.All(b => Vecinos(b).Count == 0), "Separados: ninguno enciende a otro.");
            Assert.AreEqual(0, Vecinos(DisenoNivel4.Barriles[0]).Count, "El del tutorial, solo.");

            // El del tutorial se lleva a los dos Penitentes que tiene al lado; el último tejado entero se va con uno.
            var tutorial = DisenoNivel4.Barriles[0];
            Assert.AreEqual(2, DisenoNivel4.Enemigos.Count(e => Mathf.Abs(e.X - tutorial.x) <= DisenoNivel4.RadioBarril && Mathf.Approximately(e.Y, tutorial.y)),
                "Junto al barril del tutorial, dos Penitentes a su alcance.");
            foreach (var enemigo in DisenoNivel4.Enemigos.Where(e => Mathf.Approximately(e.Y, DisenoNivel4.AlturaTejados) && e.X > tejado[0].x - 1f))
            {
                Assert.IsTrue(tejado.Any(b => Mathf.Abs(b.x - enemigo.X) <= DisenoNivel4.RadioBarril), $"{enemigo.Nombre}: lo alcanza la cadena.");
            }
        }

        [Test]
        public void LasMemorias_EstanEnSuSitio_YSuZonaTieneLosEnemigosQueCuesta()
        {
            var memorias = DisenoNivel4.Memorias;
            Assert.AreEqual(4, memorias.Length, "Las cuatro del Anexo A.");
            foreach (var memoria in memorias)
            {
                Assert.AreEqual(memoria.Y, DisenoNivel4.SueloBajo(memoria.X, memoria.Y), 1e-4f, $"{memoria.Nombre}, en su suelo.");
            }

            int EnSuZona(DisenoNivel4.Memoria m) => DisenoNivel4.Enemigos.Count(e => m.Zona.Contiene(e.X, e.Y));
            Assert.AreEqual(0, EnSuZona(memorias[0]), "La radio es la memoria sin riesgo: no hay enemigos en su zona.");
            Assert.AreEqual(2, EnSuZona(memorias[1]), "La campana: los dos Penitentes que vienen detrás.");
            Assert.AreEqual(3, EnSuZona(memorias[2]), "El casco: los tres de la segunda mitad de la calle.");
            Assert.AreEqual(1, EnSuZona(memorias[3]), "La carta: el Verdugo.");
            Assert.IsTrue(DisenoNivel4.Enemigos.Where(e => memorias[3].Zona.Contiene(e.X, e.Y)).All(e => e.Tipo == TIPOS.VERDUGO));

            // En la calle hay dos memorias; en los tejados, ninguna.
            Assert.AreEqual(2, memorias.Count(m => Mathf.Approximately(m.Y, DisenoNivel4.PisadaDe("Calle").Y)));
            Assert.IsFalse(memorias.Any(m => Mathf.Approximately(m.Y, DisenoNivel4.AlturaTejados)));
            Assert.That(memorias[3].X, Is.InRange(DisenoNivel4.Aguas[1].Hasta, DisenoNivel4.LimitesPatio.x), "La carta, a la puerta del patio.");
        }

        [Test]
        public void LasPistas_EstanAntesDeLoQueNombran()
        {
            var barril = DisenoNivel4.Barriles[0];
            Assert.Less(DisenoNivel4.PistaDelBarril.Hasta, barril.x, "La pista del barril, antes del barril.");
            Assert.Less(barril.x - DisenoNivel4.PistaDelBarril.Hasta, 3f, "Y a la vista de él.");

            var tierra = DisenoNivel4.PistaDeLaTierraDeNadie;
            var refugio = DisenoNivel4.Coberturas.Single(c => c.Nombre == "SacosDelRefugio");
            Assert.Greater(tierra.Desde, DisenoNivel4.Checkpoints[0].x, "La de la tierra de nadie, pasado el refugio…");
            Assert.LessOrEqual(tierra.Hasta, refugio.Desde, "…y antes de los sacos.");
        }

        // --- Los enemigos, los checkpoints y la arena ---

        [Test]
        public void LosEnemigos_SonLosDelDiseno_YPatrullanSobreUnaSolaPisada()
        {
            var enemigos = DisenoNivel4.Enemigos;
            var penitentes = enemigos.Where(e => e.Tipo == TIPOS.PENITENTE).ToList();
            Assert.AreEqual(11, penitentes.Count);
            Assert.AreEqual(3, penitentes.Count(e => e.X < DisenoNivel4.Secciones[0].y), "Tres en el campo.");
            Assert.AreEqual(5, penitentes.Count(e => Mathf.Approximately(e.Y, DisenoNivel4.PisadaDe("Calle").Y)), "Cinco en la calle.");
            Assert.AreEqual(3, penitentes.Count(e => Mathf.Approximately(e.Y, DisenoNivel4.AlturaTejados)), "Tres en los tejados.");
            Assert.AreEqual(5, enemigos.Count(e => e.Tipo == TIPOS.ARTILLERO), "Cinco Artilleros: uno en el campo, dos en el campanario y dos en los tejados.");
            Assert.AreEqual(2, enemigos.Count(e => e.EnElCampanario));
            var verdugo = enemigos.Single(e => e.Tipo == TIPOS.VERDUGO);
            Assert.That(verdugo.X, Is.InRange(DisenoNivel4.LimitesPatio.x, DisenoNivel4.LimitesPatio.y), "El Verdugo, en su patio.");
            Assert.IsFalse(enemigos.Any(e => e.X >= DisenoNivel4.PuertaDesde), "En la arena, solo el jefe.");
            Assert.AreEqual(enemigos.Length, enemigos.Select(e => e.Nombre).Distinct().Count(), "Cada uno con su nombre.");

            foreach (var enemigo in enemigos)
            {
                Assert.AreEqual(enemigo.Y, DisenoNivel4.SueloBajo(enemigo.X, enemigo.Y), 1e-4f, $"{enemigo.Nombre} de pie en lo que pisa.");
                var pisada = DisenoNivel4.Suelos.Concat(DisenoNivel4.Tejados).Append(DisenoNivel4.Balcon)
                    .Single(b => enemigo.X >= b.Desde && enemigo.X < b.Hasta && Mathf.Approximately(b.Arriba, enemigo.Y));
                Assert.That(enemigo.Desde, Is.InRange(pisada.Desde, pisada.Hasta), enemigo.Nombre);
                Assert.That(enemigo.Hasta, Is.InRange(pisada.Desde, pisada.Hasta), enemigo.Nombre);
                Assert.That(enemigo.X, Is.InRange(enemigo.Desde, enemigo.Hasta), enemigo.Nombre);
                if (enemigo.Tipo == TIPOS.ARTILLERO)
                {
                    Assert.AreEqual(enemigo.Desde, enemigo.Hasta, 1e-4f, $"{enemigo.Nombre}: no se mueve.");
                }
                else
                {
                    Assert.IsFalse(DisenoNivel4.Coberturas.Any(c => Mathf.Approximately(c.Abajo, enemigo.Y) && c.Desde < enemigo.Hasta && c.Hasta > enemigo.Desde),
                        $"{enemigo.Nombre}: su ruta no cruza una cobertura.");
                }
            }
        }

        [Test]
        public void LosCheckpoints_EstanEnElRefugio_LaConvergencia_YLaAntesala()
        {
            var puntos = DisenoNivel4.Checkpoints;
            Assert.AreEqual(3, puntos.Length);
            var refugio = DisenoNivel4.Coberturas.Single(c => c.Nombre == "SacosDelRefugio");
            Assert.That(puntos[0].x, Is.InRange(DisenoNivel4.Enemigos.Single(e => e.Nombre == "Artillero_1").X, refugio.Desde), "El refugio, tras el primer Artillero.");
            Assert.That(puntos[1].x, Is.InRange(DisenoNivel4.PisadaDe("Convergencia").Desde, DisenoNivel4.PisadaDe("Convergencia").Hasta), "La convergencia.");
            Assert.That(puntos[2].x, Is.InRange(DisenoNivel4.PisadaDe("Antesala").Desde, DisenoNivel4.PisadaDe("Antesala").Hasta), "La antesala.");

            foreach (var punto in puntos)
            {
                Assert.AreEqual(punto.y, DisenoNivel4.SueloBajo(punto.x, punto.y), 1e-4f, $"La bandera de x {punto.x}, en su suelo.");
                Assert.AreEqual(punto.y, DisenoNivel4.SueloBajo(punto.x + 0.5f, punto.y), 1e-4f, $"La bandera de x {punto.x} no queda en un borde.");
                Assert.AreEqual(punto.y, DisenoNivel4.SueloBajo(punto.x - 0.5f, punto.y), 1e-4f, $"La bandera de x {punto.x} no queda en un borde.");
                Assert.IsFalse(DisenoNivel4.Enemigos.Any(e => e.Tipo != TIPOS.ARTILLERO && e.Desde - 2f < punto.x && e.Hasta + 2f > punto.x && Mathf.Approximately(e.Y, punto.y)),
                    $"Sin enemigos rondando el checkpoint de x {punto.x}.");
            }
        }

        [Test]
        public void LaArena_Mide25Por15_ConLasCuatroMarcasYDosCoberturas_Simetricas()
        {
            var limites = DisenoNivel4.LimitesArena;
            Assert.AreEqual(25f, limites.y - limites.x, 0.01f, "Como las arenas de los otros mundos.");
            Assert.AreEqual(15f, DisenoNivel4.TechoArena - DisenoNivel4.SueloArena, 0.01f);
            Assert.AreEqual((limites.x + limites.y) * 0.5f, DisenoNivel4.Jefe, 0.01f, "El Juez, en el centro.");
            Assert.That(DisenoNivel4.Salida, Is.InRange(limites.x, limites.y));
            Assert.That(DisenoNivel4.Entrada, Is.InRange(limites.x, limites.y));

            var marcas = DisenoNivel4.Marcas.OrderBy(x => x).ToArray();
            Assert.AreEqual(4, marcas.Length);
            for (var i = 0; i < marcas.Length; i++)
            {
                Assert.That(marcas[i], Is.InRange(limites.x + 1f, limites.y - 1f), "Dentro de la plaza, sin pegarse a sus paredes.");
                Assert.AreEqual(DisenoNivel4.Jefe - marcas[i], marcas[marcas.Length - 1 - i] - DisenoNivel4.Jefe, 1e-4f, "Simétricas respecto al Juez.");
            }

            var escombros = DisenoNivel4.Coberturas.Where(c => c.Desde > limites.x && c.Hasta < limites.y).ToList();
            Assert.AreEqual(2, escombros.Count, "Dos escombros, para cubrirse de las placas.");
            var centros = escombros.Select(c => (c.Desde + c.Hasta) * 0.5f).OrderBy(c => c).ToList();
            Assert.AreEqual(DisenoNivel4.Jefe - centros[0], centros[1] - DisenoNivel4.Jefe, 1e-4f, "Simétricos.");
            Assert.IsFalse(escombros.Any(c => marcas.Any(m => m > c.Desde - 0.6f && m < c.Hasta + 0.6f)), "Ninguna marca bajo un escombro.");
        }

        [Test]
        public void LaParedDeLasMemorias_SeVeDesdeElSueloDeLaPlaza_DetrasDelJuez()
        {
            // Con Kael en el suelo, la cámara (su centro, 1,5 u por encima de él) enseña hasta TamanoCamara más arriba.
            const float centroDeLaCamara = 1.5f;
            var pared = DisenoNivel4.Proyeccion;
            Assert.Greater(pared.yMin, DisenoNivel4.SueloArena, "Por encima del suelo: no se mete detrás de él.");
            Assert.Less(pared.yMax, DisenoNivel4.SueloArena + centroDeLaCamara + DisenoNivel4.TamanoCamara - 0.2f,
                "Entera a la vista con Kael en el suelo: si no, en cada ataque no se sabría cuál es la memoria verdadera.");
            Assert.AreEqual(DisenoNivel4.Jefe, pared.center.x, 1e-4f, "Detrás del Juez.");
            Assert.That(pared.xMin, Is.GreaterThan(DisenoNivel4.LimitesArena.x));
            Assert.That(pared.xMax, Is.LessThan(DisenoNivel4.LimitesArena.y));
        }

        [Test]
        public void LasSecciones_SonCuatro_YLlevanLaCamaraDeLaParedIzquierdaAlFondoDeLaArena()
        {
            var secciones = DisenoNivel4.Secciones;
            Assert.AreEqual(4, secciones.Length, "Las tres del recorrido y la arena.");
            Assert.AreEqual(DisenoNivel4.TextosDeLasSecciones.Length, secciones.Length);
            for (var i = 1; i < secciones.Length; i++)
            {
                Assert.AreEqual(secciones[i - 1].y, secciones[i].x, 1e-4f, "Cada una empieza donde acaba la anterior.");
            }

            var izquierda = DisenoNivel4.Paredes.Single(p => p.Nombre == "ParedIzquierda");
            var fondo = DisenoNivel4.Paredes.Single(p => p.Nombre == "ParedArena");
            Assert.AreEqual(izquierda.Hasta, secciones[0].x - DisenoNivel4.MargenCamara, 1e-4f, "La cámara empieza en la pared de la izquierda.");
            Assert.AreEqual(fondo.Hasta, secciones[3].y + DisenoNivel4.MargenCamara, 1e-4f, "Y llega a la del fondo de la arena.");
            Assert.AreEqual(DisenoNivel4.PuertaDesde, secciones[3].x, 1e-4f, "La última es la arena.");
            Assert.LessOrEqual(DisenoNivel4.AlturaCamara.x, DisenoNivel4.FondoRio - 2f, "La cámara baja hasta el fondo del río.");
            Assert.GreaterOrEqual(DisenoNivel4.AlturaCamara.y, DisenoNivel4.TechoArena, "Y sube hasta el techo de la arena.");
            Assert.That(DisenoNivel4.Paredes.Single(p => p.Nombre == "ParedArena").Hasta, Is.InRange(190f, 210f), "Unas 200 u.");
        }

        /// <summary>La Invisibilidad que da el Miedo, leída del componente (3 s).</summary>
        private class PlayerAbilitiesMedidas
        {
            public float Duracion
            {
                get
                {
                    var objeto = new GameObject("Habilidades");
                    try
                    {
                        return new SerializedObject(objeto.AddComponent<PlayerAbilities>()).FindProperty("duracionInvisibilidad").floatValue;
                    }
                    finally
                    {
                        Object.DestroyImmediate(objeto);
                    }
                }
            }
        }
    }
}
