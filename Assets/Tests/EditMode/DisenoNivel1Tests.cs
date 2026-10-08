using System.Linq;
using Fragmentum.EditorTools;
using Fragmentum.Player;
using NUnit.Framework;
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
        public void LaArena_Mide25Por15_YElCheckpointVaEnLaSeccion2AntesDelGolem()
        {
            var limites = DisenoNivel1.LimitesArena;
            Assert.AreEqual(25f, limites.y - limites.x, 0.01f, "Decisión del diseño: el tramo de 55 a 80 da 25 u, no 60.");
            Assert.AreEqual(15f, DisenoNivel1.TechoArena - DisenoNivel1.SueloArena, 0.01f, "Anexo A: 15 u de alto.");
            Assert.That(DisenoNivel1.Jefe, Is.InRange(limites.x, limites.y));
            Assert.That(DisenoNivel1.Salida, Is.InRange(limites.x, limites.y));

            Assert.That(DisenoNivel1.Checkpoint, Is.InRange(25f, 55f));
            Assert.Less(DisenoNivel1.Checkpoint, DisenoNivel1.Enemigos.Single(e => e.Golem).X);
            Assert.AreEqual(DisenoNivel1.SueloEn(DisenoNivel1.Checkpoint), DisenoNivel1.SueloEn(DisenoNivel1.Checkpoint + 0.5f), "La bandera no queda en un borde.");
            Assert.IsFalse(DisenoNivel1.Enemigos.Any(e => Mathf.Abs(e.X - DisenoNivel1.Checkpoint) < 2f), "Sin enemigos encima del checkpoint.");
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
        }

        [Test]
        public void LasMecanicas_EnsenanEnTresTiempos()
        {
            var inestables = DisenoNivel1.Mecanicas.Where(p => p.Prefab == GeneradorMecanicas.PlataformaInestable).OrderBy(p => p.X).ToList();
            Assert.AreEqual(2, inestables.Count);
            Assert.Greater(DisenoNivel1.SueloEn(inestables[0].X), -3f, "La primera, sobre un foso que no mata: se sale con un salto.");
            Assert.IsTrue(DisenoNivel1.Lavas.Any(l => inestables[1].X > l.Desde && inestables[1].X < l.Hasta), "La segunda, sobre la lava.");

            var combustion = DisenoNivel1.Mecanicas.Where(p => p.Prefab == GeneradorMecanicas.PlataformaCombustion).ToList();
            Assert.AreEqual(2, combustion.Count);
            Assert.IsTrue(combustion.All(c => DisenoNivel1.Lavas.Any(l => c.X > l.Desde && c.X < l.Hasta)), "Sobre el lago de lava.");
            Assert.AreNotEqual(combustion[0].Desfase, combustion[1].Desfase, "Desfasadas: hay que esperar.");

            var torreta = DisenoNivel1.Mecanicas.Single(p => p.Prefab == GeneradorMecanicas.TorretaFuego);
            Assert.IsTrue(torreta.MiraIzquierda, "Dispara hacia el jugador que llega por la izquierda.");
            Assert.Less(torreta.X, 25f, "Sección 1.");
        }
    }
}
