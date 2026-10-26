using System.Linq;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El recorrido de diseño del Nivel 1 (<see cref="DisenoNivel1"/>) se puede cruzar con los
    /// saltos de Kael, cumple los recuentos del Anexo A y encaja con el tileset. Lo comprueba sobre
    /// los datos; <see cref="EscenasNivelTests"/> comprueba que la escena los sigue.
    /// </summary>
    public class DisenoNivel1Tests
    {
        // Margen: lo que se pide al jugador no llega nunca al límite exacto de lo que puede.
        private const float MargenAlcance = 0.85f;
        private const float MargenAltura = 0.9f;

        private static readonly ParametrosJugador Kael = new ParametrosJugador();

        private static float Gravedad => Mathf.Abs(Physics2D.gravity.y) * Kael.escalaGravedad;
        private static float AlturaSalto => Kael.velocidadSalto * Kael.velocidadSalto / (2f * Gravedad);
        private static float AlturaDobleSalto => AlturaSalto + Kael.velocidadDobleSalto * Kael.velocidadDobleSalto / (2f * Gravedad);

        /// <summary>Distancia que recorre Kael corriendo hasta caer en algo <paramref name="subida"/> más alto.</summary>
        private static float Alcance(float subida)
        {
            var caida = Gravedad * Kael.multiplicadorGravedadCaida;
            var tiempo = subida <= AlturaSalto
                ? Kael.velocidadSalto / Gravedad + Mathf.Sqrt(2f * (AlturaSalto - subida) / caida)
                : Kael.velocidadSalto / Gravedad + Kael.velocidadDobleSalto / Gravedad + Mathf.Sqrt(2f * Mathf.Max(0f, AlturaDobleSalto - subida) / caida);
            return Kael.velocidadMaxima * tiempo;
        }

        [Test]
        public void LasMedidasDeKael_SonLasDelDocumentoDeDiseno()
        {
            Assert.AreEqual(2.45f, AlturaSalto, 0.05f, "El salto sube unas 2,45 u.");
            Assert.AreEqual(4.15f, AlturaDobleSalto, 0.1f, "Con el doble salto, unas 4,2.");
            Assert.AreEqual(4.45f, Alcance(0f), 0.1f, "Un salto corriendo, unas 4,4 u.");
        }

        [Test]
        public void CadaPisadaDelRecorrido_SeAlcanzaDesdeLaAnterior()
        {
            var recorrido = DisenoNivel1.Recorrido();
            Assert.AreEqual("SueloInicio", recorrido.First().Nombre);
            Assert.AreEqual("SueloArena", recorrido.Last().Nombre);

            for (var i = 1; i < recorrido.Count; i++)
            {
                var desde = recorrido[i - 1];
                var hasta = recorrido[i];
                var hueco = hasta.Desde - desde.Hasta;
                var subida = hasta.Y - desde.Y;
                Assert.LessOrEqual(subida, AlturaDobleSalto * MargenAltura, $"De {desde} a {hasta}: demasiado alto.");
                Assert.LessOrEqual(hueco, Alcance(Mathf.Max(0f, subida)) * MargenAlcance, $"De {desde} a {hasta}: demasiado lejos.");
            }
        }

        [Test]
        public void ElHuecoMasLargo_SeCruzaSinDash_YLaTorretaSeSaltaConUnSalto()
        {
            var recorrido = DisenoNivel1.Recorrido();
            var mayor = Enumerable.Range(1, recorrido.Count - 1).Max(i => recorrido[i].Desde - recorrido[i - 1].Hasta);
            Assert.AreEqual(3f, mayor, 0.01f, "El salto a la puerta de la arena: 3 u.");
            Assert.Less(mayor, Alcance(0f) * MargenAlcance, "Sin dash: el dash sobra.");

            var bloqueTorreta = GeneradorMecanicas.Geometria.BloqueTorreta;
            Assert.Less(bloqueTorreta.yMax, AlturaSalto * MargenAltura, "La torreta se salta (o se pisa) con un salto normal.");
        }

        [Test]
        public void LasPlataformasDeLaArena_SeAlcanzanUnaDesdeOtra()
        {
            var superficies = DisenoNivel1.PlataformasArena
                .Select(p => new DisenoNivel1.Pisada(p.Nombre, p.Desde, p.Hasta, p.Arriba))
                .Append(new DisenoNivel1.Pisada("SueloArena", DisenoNivel1.LimitesArena.x, DisenoNivel1.LimitesArena.y, DisenoNivel1.SueloArena))
                .ToList();

            foreach (var plataforma in superficies.Where(s => s.Nombre != "SueloArena"))
            {
                var desdeAlguna = superficies.Any(s =>
                {
                    var subida = plataforma.Y - s.Y;
                    var hueco = Mathf.Max(plataforma.Desde - s.Hasta, s.Desde - plataforma.Hasta, 0f);
                    return subida > 0f && subida <= AlturaDobleSalto * MargenAltura && hueco <= Alcance(subida) * MargenAlcance;
                });
                Assert.IsTrue(desdeAlguna, $"{plataforma} no se alcanza desde nada más bajo.");
            }

            Assert.AreEqual(3, DisenoNivel1.PlataformasArena.Select(p => p.Arriba).Distinct().Count(), "Anexo A: plataformas a tres alturas.");
            Assert.Less(DisenoNivel1.PlataformasArena.Min(p => p.Abajo) - DisenoNivel1.SueloArena, 3.4f,
                "La más baja queda por debajo de la cabeza de la Centinela (3,4 u): por eso las atraviesa.");
        }

        [Test]
        public void DesdeLasPlataformas_LaEspadaDeKaelNoAlcanzaALaCentinelaEnElSuelo()
        {
            // Lo que queda la espada de Kael sobre sus pies: en el prefab, los pies están en el origen.
            var jugador = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorJugador.RutaPrefab);
            Assert.IsNotNull(jugador);
            var espada = jugador.GetComponentsInChildren<BoxCollider2D>(true).Single(c => c.name == "Espada");
            var sobreLosPies = jugador.transform.InverseTransformPoint(
                espada.transform.TransformPoint(espada.offset - new Vector2(0f, espada.size.y * 0.5f))).y;

            var centinela = GeneradorJefes.CargarDatos(ConstructorCentinela.NombreDatos);
            Assert.IsNotNull(centinela);
            var cabeza = DisenoNivel1.SueloArena + centinela.tamano.y;
            foreach (var plataforma in DisenoNivel1.PlataformasArena)
            {
                // El fallo que encontró el segundo tester: desde la más baja, a 3 u, se le pegaba sin que ella alcanzara.
                Assert.Greater(plataforma.Arriba + sobreLosPies, cabeza + 0.2f,
                    $"{plataforma.Nombre}: desde ella, la espada de Kael alcanzaría la cabeza de la Centinela, y ningún ataque de ella llega allí.");
            }
        }

        [Test]
        public void LaArena_Mide25Por15_YLosCheckpointsVanAntesDelGolem_TrasEl_YAnteLaArena()
        {
            var limites = DisenoNivel1.LimitesArena;
            Assert.AreEqual(25f, limites.y - limites.x, 0.01f, "Decisión del diseño: una sala de 25 u, no de 60.");
            Assert.AreEqual(15f, DisenoNivel1.TechoArena - DisenoNivel1.SueloArena, 0.01f, "Anexo A: 15 u de alto.");
            Assert.That(DisenoNivel1.Jefe, Is.InRange(limites.x, limites.y));
            Assert.That(DisenoNivel1.Salida, Is.InRange(limites.x, limites.y));
            Assert.AreEqual((limites.x + limites.y) * 0.5f, DisenoNivel1.Jefe + 1.5f, 3f, "La Centinela espera hacia el centro de la sala.");

            var puntos = DisenoNivel1.Checkpoints;
            var golem = DisenoNivel1.Enemigos.Single(e => e.Golem).X;
            Assert.AreEqual(3, puntos.Length, "Antes del Golem, tras él y ante la arena.");
            Assert.That(puntos[0], Is.InRange(25f, 55f));
            Assert.Less(puntos[0], golem, "El primero, antes del Golem.");

            Assert.Greater(puntos[1], golem, "El segundo, pasado el Golem: morir en la forja no obliga a repetirlo.");
            Assert.Less(puntos[1], DisenoNivel1.Lavas.Single(l => l.Nombre == "LavaDeLaEscalera").Desde, "Y antes del primer peligro de la forja.");

            var antesala = DisenoNivel1.Suelos.Single(b => b.Nombre == "Antesala");
            Assert.That(puntos[2], Is.InRange(antesala.Desde, antesala.Hasta), "El tercero, en la antesala.");
            Assert.Greater(puntos[2], DisenoNivel1.Lavas.Single(l => l.Nombre == "LavaDeLaForja").Hasta, "Pasado el lago de la forja.");
            Assert.Less(puntos[2], DisenoNivel1.PuertaDesde, "Y antes de la arena: morir ante la Centinela no obliga a repetir el nivel.");

            foreach (var punto in puntos)
            {
                Assert.AreEqual(DisenoNivel1.SueloEn(punto), DisenoNivel1.SueloEn(punto + 0.5f), $"La bandera de x {punto} no queda en un borde.");
                Assert.AreEqual(DisenoNivel1.SueloEn(punto), DisenoNivel1.SueloEn(punto - 0.5f), $"La bandera de x {punto} no queda en un borde.");
                Assert.IsFalse(DisenoNivel1.Enemigos.Any(e => Mathf.Abs(e.X - punto) < 2f), $"Sin enemigos encima del checkpoint de x {punto}.");
            }
        }

        [Test]
        public void LasSecciones_SonCuatro_YLlevanLaCamaraHastaElFondoDeLaArena()
        {
            var secciones = DisenoNivel1.Secciones;
            Assert.AreEqual(4, secciones.Length, "Las dos del Anexo A, la forja (2026-10-13) y la arena.");
            Assert.AreEqual(0f, secciones[0].Desde);
            for (var i = 1; i < secciones.Length; i++)
            {
                Assert.AreEqual(secciones[i - 1].Hasta, secciones[i].Desde, 1e-4f, "Cada una empieza donde acaba la anterior.");
            }

            Assert.AreEqual(DisenoNivel1.PuertaDesde, secciones[3].Desde, 1e-4f, "La última es la arena.");
            Assert.AreEqual(DisenoNivel1.LimitesArena.y - 1f, secciones[3].Hasta, 1e-4f, "Y acaba 1 u antes de la pared, como antes de alargar el nivel.");
            Assert.GreaterOrEqual(secciones[2].Hasta - secciones[2].Desde, 40f, "La forja alarga el nivel de verdad.");
        }

        [Test]
        public void LosBloques_EmpiezanYAcabanEnUnidadesEnteras()
        {
            // El tileset pinta celdas enteras: un borde a media unidad se dibujaría fuera de su colisión.
            foreach (var bloque in DisenoNivel1.Suelos.Concat(DisenoNivel1.Paredes).Concat(DisenoNivel1.PlataformasArena))
            {
                Assert.AreEqual(Mathf.Round(bloque.Desde), bloque.Desde, 1e-4f, bloque.Nombre);
                Assert.AreEqual(Mathf.Round(bloque.Hasta), bloque.Hasta, 1e-4f, bloque.Nombre);
            }
        }

        [Test]
        public void CadaLava_CubreSuHueco_YSeApoyaEnSuCuenca()
        {
            foreach (var lago in DisenoNivel1.Lavas)
            {
                Assert.GreaterOrEqual(lago.Franjas * DisenoNivel1.AnchoFranjaLava, lago.Hasta - lago.Desde, $"{lago.Nombre}: sin huecos a los lados.");
                Assert.Less((lago.Franjas - 1) * DisenoNivel1.AnchoFranjaLava, lago.Hasta - lago.Desde, $"{lago.Nombre}: sin franjas de más.");

                var cuenca = DisenoNivel1.Suelos.Single(b => b.Desde == lago.Desde && b.Hasta == lago.Hasta);
                Assert.AreEqual(cuenca.Arriba, lago.Costra - DisenoNivel1.AltoFranjaLava, 1e-4f, $"{lago.Nombre}: la franja descansa en su cuenca.");
                Assert.Less(lago.Costra, Mathf.Min(DisenoNivel1.SueloEn(lago.Desde - 0.5f), DisenoNivel1.SueloEn(lago.Hasta + 0.5f)),
                    $"{lago.Nombre}: la costra queda por debajo de las orillas.");
            }
        }

        [Test]
        public void CadaEnemigo_PatrullaSobreUnSoloSuelo_YLejosDeLaLava()
        {
            foreach (var guardia in DisenoNivel1.Enemigos)
            {
                Assert.AreEqual(guardia.Y, DisenoNivel1.SueloEn(guardia.X), $"{guardia.Nombre} de pie en su suelo.");
                var suelo = DisenoNivel1.Suelos.Single(b => guardia.X >= b.Desde && guardia.X < b.Hasta && b.Arriba == guardia.Y);
                Assert.That(guardia.Desde, Is.InRange(suelo.Desde, suelo.Hasta), guardia.Nombre);
                Assert.That(guardia.Hasta, Is.InRange(suelo.Desde, suelo.Hasta), guardia.Nombre);
                Assert.That(guardia.X, Is.InRange(guardia.Desde, guardia.Hasta), guardia.Nombre);
            }

            // Anexo A: tres soldados en la sección 1 y cuatro con el Golem en la 2. Los Ecos ocupan su sitio.
            Assert.AreEqual(3, DisenoNivel1.Enemigos.Count(e => !e.Golem && e.X < 25f));
            Assert.AreEqual(4, DisenoNivel1.Enemigos.Count(e => !e.Golem && e.X >= 25f && e.X < 55f));
            Assert.AreEqual(1, DisenoNivel1.Enemigos.Count(e => e.Golem));
            Assert.That(DisenoNivel1.Enemigos.Single(e => e.Golem).X, Is.InRange(25f, 55f), "El mini-jefe cierra la sección 2.");

            // La forja: tres Ecos más, y ninguno pasada la puerta de la arena.
            Assert.AreEqual(3, DisenoNivel1.Enemigos.Count(e => !e.Golem && e.X >= 55f && e.X < DisenoNivel1.PuertaDesde));
            Assert.IsFalse(DisenoNivel1.Enemigos.Any(e => e.X >= DisenoNivel1.PuertaDesde), "En la arena, solo la Centinela.");
            Assert.AreEqual(DisenoNivel1.Enemigos.Length, DisenoNivel1.Enemigos.Select(e => e.Nombre).Distinct().Count(), "Cada enemigo, con su nombre: el marcador los distingue por él.");
        }

        [Test]
        public void LasMecanicas_EnsenanEnTresTiempos()
        {
            var inestables = DisenoNivel1.Mecanicas.Where(p => p.Prefab == GeneradorMecanicas.PlataformaInestable).OrderBy(p => p.X).ToList();
            Assert.AreEqual(4, inestables.Count);
            Assert.Greater(DisenoNivel1.SueloEn(inestables[0].X), -3f, "La primera, sobre un foso que no mata: se sale con un salto.");
            foreach (var inestable in inestables.Skip(1))
            {
                Assert.IsTrue(DisenoNivel1.Lavas.Any(l => inestable.X > l.Desde && inestable.X < l.Hasta), $"{inestable.Nombre}: las demás, sobre la lava.");
            }

            var combustion = DisenoNivel1.Mecanicas.Where(p => p.Prefab == GeneradorMecanicas.PlataformaCombustion).OrderBy(p => p.X).ToList();
            Assert.AreEqual(4, combustion.Count);
            Assert.IsTrue(combustion.All(c => DisenoNivel1.Lavas.Any(l => c.X > l.Desde && c.X < l.Hasta)), "Sobre los lagos de lava.");
            Assert.AreNotEqual(combustion[0].Desfase, combustion[1].Desfase, "Desfasadas: hay que esperar.");
            Assert.AreNotEqual(combustion[2].Desfase, combustion[3].Desfase, "Las de la forja, también.");

            var torretas = DisenoNivel1.Mecanicas.Where(p => p.Prefab == GeneradorMecanicas.TorretaFuego).OrderBy(p => p.X).ToList();
            Assert.AreEqual(2, torretas.Count);
            Assert.IsTrue(torretas.All(t => t.MiraIzquierda), "Disparan hacia el jugador que llega por la izquierda.");
            Assert.Less(torretas[0].X, 25f, "La primera, en la sección 1.");
            Assert.That(torretas[1].X, Is.InRange(55f, DisenoNivel1.PuertaDesde), "La segunda, en la forja.");
        }

        [Test]
        public void LaForja_EsElExamen_UnaInestableEntreDosDeCombustion_YDosEcosBajoLaTorreta()
        {
            // El lago de la forja: la inestable no deja esperar, así que la espera es en la primera de combustión.
            var lago = DisenoNivel1.Lavas.Single(l => l.Nombre == "LavaDeLaForja");
            var sobreElLago = DisenoNivel1.Mecanicas.Where(p => p.X > lago.Desde && p.X < lago.Hasta).OrderBy(p => p.X).ToList();
            CollectionAssert.AreEqual(
                new[] { GeneradorMecanicas.PlataformaCombustion, GeneradorMecanicas.PlataformaInestable, GeneradorMecanicas.PlataformaCombustion },
                sobreElLago.Select(p => p.Prefab).ToArray());
            Assert.Greater(lago.Hasta - lago.Desde, DisenoNivel1.Lavas.Single(l => l.Nombre == "LavaDelLago").Hasta - DisenoNivel1.Lavas.Single(l => l.Nombre == "LavaDelLago").Desde,
                "Más largo que el primer lago.");
            Assert.IsTrue(sobreElLago.All(p => Mathf.Approximately(p.Y, DisenoNivel1.SueloEn(lago.Desde - 0.5f))), "A ras de las orillas, como en el primer lago.");

            // La almena: dos Ecos delante de la torreta, en su línea de fuego, y sitio detrás de ella para esperar.
            var torreta = DisenoNivel1.Mecanicas.Where(p => p.Prefab == GeneradorMecanicas.TorretaFuego).OrderBy(p => p.X).Last();
            var almena = DisenoNivel1.Suelos.Single(b => torreta.X >= b.Desde && torreta.X < b.Hasta && b.Arriba == torreta.Y);
            Assert.AreEqual("Almena", almena.Nombre);
            Assert.AreEqual(2, DisenoNivel1.Enemigos.Count(e => e.Y == torreta.Y && e.X > almena.Desde && e.X < torreta.X),
                "La tercera vez de la torreta es junto a enemigos.");
            Assert.GreaterOrEqual(almena.Hasta - torreta.X, 2f, "Detrás de la torreta queda suelo para mirar el lago antes de saltar.");
            Assert.Greater(almena.Arriba, DisenoNivel1.SueloArena, "La forja sube por encima de la arena y vuelve a bajar a ella.");
        }
    }
}
