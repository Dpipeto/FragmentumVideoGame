using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Enemies;
using Fragmentum.Enemies.Deteccion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Los enemigos del mundo de la Culpa (fase C), sin jugar: sus datos son los del documento (el
    /// Penitente, con el ritmo de los Ecos), sus prefabs llevan sus piezas, el Artillero apunta a la
    /// cabeza y solo por delante, y el Verdugo elige su golpe según dónde está Kael; su cadena pasa por
    /// encima de Kael agachado. Lo que pasa jugando, en <c>EnemigosCulpaPlayModeTests</c>.
    /// </summary>
    public class EnemigosCulpaTests
    {
        [Test]
        public void LosDatos_SonLosDelDocumento()
        {
            var penitente = GeneradorEnemigos.CargarDatos("Penitente");
            Assert.IsNotNull(penitente, "Falta el Penitente: ejecuta Fragmentum → Regenerar todo.");
            Assert.AreEqual(3, penitente.vidaMaxima);
            Assert.AreEqual(1f, penitente.velocidadPatrulla, 1e-4f, "Lento: patrulla a 1 u/s…");
            Assert.AreEqual(2.2f, penitente.velocidadPersecucion, 1e-4f, "…y persigue a 2,2.");
            Assert.AreEqual(0.45f, penitente.attackCooldown, 1e-4f, "Con el ritmo de ataque de los Ecos desde el principio (tu regla).");
            Assert.IsInstanceOf<DeteccionConVision>(penitente.deteccion, "Ve: con la Invisibilidad, lo pierde.");
            Assert.AreEqual(DisenoNivel1.EscalaEco, DisenoNivel4.EscalaPenitente, "A la escala de los Ecos.");

            var artillero = GeneradorEnemigos.CargarDatos("Artillero");
            Assert.IsNotNull(artillero);
            Assert.AreEqual(3, artillero.vidaMaxima);
            Assert.AreEqual(0f, artillero.velocidadPatrulla, 1e-4f, "No se mueve.");
            Assert.AreEqual(0f, artillero.attackRadius, 1e-4f, "Sus ataques los lleva su pieza.");

            var verdugo = GeneradorEnemigos.CargarDatos("Verdugo");
            Assert.IsNotNull(verdugo);
            Assert.AreEqual(16, verdugo.vidaMaxima, "16 de vida…");
            Assert.IsTrue(verdugo.hiperarmadura, "…e hiperarmadura, como el Golem.");
            Assert.AreEqual(0f, verdugo.attackRadius, 1e-4f, "Sus golpes los lleva su pieza.");
            Assert.AreEqual(0, verdugo.danioContacto, "Tocarlo no hace daño.");

            CollectionAssert.Contains(TablaPuntaje.MiniJefes, "Verdugo");
            Assert.AreEqual(100, TablaPuntaje.Enemigos.Single(e => e.asset == "Penitente").puntos, "El común.");
            Assert.AreEqual(150, TablaPuntaje.Enemigos.Single(e => e.asset == "Artillero").puntos);
            Assert.AreEqual(800, TablaPuntaje.Enemigos.Single(e => e.asset == "Verdugo").puntos, "Entre el Acechador (700) y el jefe.");
        }

        [Test]
        public void LosPrefabs_LlevanSusPiezas()
        {
            var penitente = GeneradorEnemigos.CargarPrefab("Penitente");
            Assert.IsNotNull(penitente);
            Assert.AreSame(GeneradorEnemigos.CargarDatos("Penitente"), penitente.GetComponent<EnemyController>().Datos);

            var artillero = GeneradorEnemigos.CargarPrefab("Artillero").GetComponent<Artillero>();
            Assert.IsNotNull(artillero, "El Artillero lleva su pieza.");
            Assert.AreEqual(DisenoNivel4.VisionArtillero, artillero.vision, 1e-4f);
            Assert.AreEqual(DisenoNivel4.ApuntaArtillero, artillero.tiempoApuntar, 1e-4f);
            Assert.AreEqual(DisenoNivel4.BalasPorRafaga, artillero.balas);
            Assert.AreEqual(DisenoNivel4.RecargaArtillero, artillero.recarga, 1e-4f);
            Assert.AreEqual(DisenoNivel4.BajoLaCabeza, artillero.bajoLaCabeza, 1e-4f);
            Assert.IsTrue(artillero.miraIzquierda, "Mira hacia el que llega.");
            Assert.IsNotNull(artillero.Linea, "La línea roja del aviso.");
            Assert.IsNotNull(artillero.Linea.sharedMaterial, "Con su material: si no, no se dibuja.");
            Assert.IsFalse(artillero.Linea.enabled, "Apagada hasta que apunta.");

            // Su cañón, el del diseño: por delante de sus sacos y por encima.
            var diseno = DisenoNivel4.Enemigos.First(e => e.Tipo == DisenoNivel4.TIPOS_ENEMIGO.ARTILLERO);
            Assert.AreEqual(diseno.Canon.x - diseno.X, -artillero.canon.x, 1e-4f);
            Assert.AreEqual(diseno.Canon.y - diseno.Y, artillero.canon.y, 1e-4f);

            var verdugo = GeneradorEnemigos.CargarPrefab("Verdugo");
            Assert.IsNotNull(verdugo.GetComponent<Verdugo>(), "El Verdugo lleva su pieza.");
            var serializado = new SerializedObject(verdugo.GetComponent<Verdugo>());
            foreach (var zona in new[] { "hachazo", "barrido" })
            {
                var hitbox = serializado.FindProperty(zona).objectReferenceValue as Hitbox;
                Assert.IsNotNull(hitbox, $"Su {zona}.");
                Assert.IsFalse(hitbox.gameObject.activeSelf, $"El {zona}, apagado hasta que golpea.");
                Assert.AreEqual(LayerMask.NameToLayer(Capas.EnemyWeapon), hitbox.gameObject.layer);
            }

            Assert.IsNotNull(serializado.FindProperty("garfio").objectReferenceValue, "Y el garfio de su cadena.");
            Assert.AreEqual(GeneradorEnemigos.TamanoVerdugo.y, verdugo.GetComponent<CapsuleCollider2D>().size.y, 1e-4f, "Unas 3 u.");
        }

        [Test]
        public void ElArtillero_ApuntaALaCabeza_YSoloVePorDelante()
        {
            var cuerpo = new Bounds(new Vector3(10f, 0.9f, 0f), new Vector3(0.8f, 1.8f, 0f));
            var mira = Artillero.PuntoDeMira(cuerpo, DisenoNivel4.BajoLaCabeza);
            Assert.AreEqual(10f, mira.x, 1e-4f);
            Assert.AreEqual(1.8f - DisenoNivel4.BajoLaCabeza, mira.y, 1e-4f, "A 0,1 u de lo alto: a lo alto de la cabeza.");

            var agachado = new Bounds(new Vector3(10f, 0.54f, 0f), new Vector3(0.8f, 1.08f, 0f));
            Assert.AreEqual(1.08f - DisenoNivel4.BajoLaCabeza, Artillero.PuntoDeMira(agachado, DisenoNivel4.BajoLaCabeza).y, 1e-4f,
                "Agachado, también a la cabeza: agacharse solo no basta.");

            var canon = new Vector2(20f, 1.5f);
            Assert.IsTrue(Artillero.PorDelante(canon, new Vector2(10f, 1.5f), -1, DisenoNivel4.VisionArtillero), "Mirando a la izquierda, lo ve a 10 u.");
            Assert.IsFalse(Artillero.PorDelante(canon, new Vector2(5f, 1.5f), -1, DisenoNivel4.VisionArtillero), "A 15 u, no.");
            Assert.IsFalse(Artillero.PorDelante(canon, new Vector2(22f, 1.5f), -1, DisenoNivel4.VisionArtillero), "A su espalda, tampoco.");
        }

        [Test]
        public void ElVerdugo_EligeSuGolpe_SegunDondeEstaKael()
        {
            var objeto = new GameObject("Verdugo");
            try
            {
                var verdugo = objeto.AddComponent<Verdugo>();
                Assert.AreEqual(ESTADOS_VERDUGO.BARRIDO, verdugo.Elegir(0.5f, 0f, 1), "Pegado a él: barrido.");
                Assert.AreEqual(ESTADOS_VERDUGO.BARRIDO, verdugo.Elegir(-1.5f, 0f, 1), "A su espalda: barrido.");
                Assert.AreEqual(ESTADOS_VERDUGO.HACHAZO, verdugo.Elegir(2f, 0f, 1), "Delante y a su alcance: hachazo.");
                Assert.AreEqual(ESTADOS_VERDUGO.MANDA, verdugo.Elegir(4f, 0f, 1), "A media distancia, camina hacia él.");
                Assert.AreEqual(ESTADOS_VERDUGO.CADENA, verdugo.Elegir(7f, 0f, 1), "Lejos: la cadena.");
                Assert.AreEqual(ESTADOS_VERDUGO.CADENA, verdugo.Elegir(-7f, 0f, 1), "Lejos a su espalda, también (se gira).");
                Assert.AreEqual(ESTADOS_VERDUGO.MANDA, verdugo.Elegir(7f, 2f, 1), "Si no está a su altura, no la lanza.");
                Assert.AreEqual(ESTADOS_VERDUGO.MANDA, verdugo.Elegir(-3f, 0f, 1), "A su espalda, pero lejos del barrido: se gira andando.");
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void LaCadenaDelVerdugo_PasaPorEncimaDeKaelAgachado_YLeDaDePie()
        {
            var objeto = new GameObject("Verdugo");
            try
            {
                var verdugo = objeto.AddComponent<Verdugo>();
                Assert.Greater(verdugo.alturaCadena - verdugo.radioGarfio, DisenoNivel4.AltoKaelAgachado + 0.05f,
                    "Agachado, el garfio le pasa por encima.");
                Assert.Less(verdugo.alturaCadena + verdugo.radioGarfio, DisenoNivel4.AltoKael, "De pie, le da en el pecho.");
                Assert.AreEqual(DisenoNivel4.RadioBarril, verdugo.radioRodillas, 1e-4f, "Una explosión a menos de 2,5 u lo arrodilla.");
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }
    }
}
