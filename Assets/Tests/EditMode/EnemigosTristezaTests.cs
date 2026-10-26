using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.EditorTools;
using Fragmentum.Enemies;
using Fragmentum.Enemies.Deteccion;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Los enemigos del mundo de la Tristeza sin escena: el pesar del Lamento, la Escultura que
    /// duerme y no anda, la Medusa que no muere y la Plañidera, el mini-jefe. Sus datos y sus prefabs.
    /// </summary>
    public class EnemigosTristezaTests
    {
        // --- El pesar ---

        [Test]
        public void ElPesar_SoloFrena_NiPesaEnElSaltoNiMoja()
        {
            var pesar = Pesadumbre.Modificador(0.5f);
            Assert.AreEqual(0.5f, pesar.velocidad, 1e-5f, "Mitad de velocidad (Nivel2_Diseno.md, sección 5).");
            Assert.AreEqual(1f, pesar.gravedad);
            Assert.AreEqual(1f, pesar.salto, "El salto no cambia: la tristeza frena, no hunde.");
            Assert.AreEqual(1f, pesar.aceleracion);
            Assert.IsFalse(pesar.esAgua);
        }

        [Test]
        public void ElPrefabDelLamento_PesaConSuZarpazoYConSuCuerpo()
        {
            var prefab = GeneradorEnemigos.CargarPrefab("Lamento");
            Assert.IsNotNull(prefab, "Falta el prefab: ejecuta Fragmentum → Regenerar todo.");
            var pesar = prefab.GetComponent<Pesar>();
            Assert.IsNotNull(pesar, "El Lamento lleva su Pesar.");
            Assert.AreEqual(2f, pesar.duracion, 1e-5f, "2 s.");
            Assert.AreEqual(0.5f, pesar.factorVelocidad, 1e-5f);

            var controlador = new SerializedObject(prefab.GetComponent<EnemyController>());
            var arma = controlador.FindProperty("arma").objectReferenceValue;
            var contacto = controlador.FindProperty("contacto").objectReferenceValue;
            CollectionAssert.AreEquivalent(new[] { arma, contacto }, pesar.Golpes, "Pesan el zarpazo y el cuerpo («reducen velocidad al contacto»).");
        }

        [Test]
        public void ElPrefabDeKael_LlevaSuPesadumbre()
        {
            var prefab = ConstructorJugador.PrefabJugador();
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<Pesadumbre>(), "El pesar vive en Kael: se pasa aunque el Lamento muera.");
        }

        // --- La Escultura ---

        [Test]
        public void LaEscultura_NoAnda_NoSeInterrumpe_YAvisaAntesDeGolpear()
        {
            var datos = GeneradorEnemigos.CargarDatos("Escultura");
            Assert.IsNotNull(datos, "Faltan sus datos: ejecuta Fragmentum → Regenerar todo.");
            Assert.AreEqual(0f, datos.velocidadPatrulla, "No anda (decisión del 2026-10-08).");
            Assert.AreEqual(0f, datos.velocidadPersecucion);
            Assert.IsTrue(datos.hiperarmadura, "Como el Golem: los golpes no la interrumpen.");
            Assert.AreEqual(0, datos.danioContacto, "Parece una estatua: tocarla no hace daño.");
            Assert.AreEqual(1f, datos.resistenciaRetroceso, 1e-5f);
            Assert.GreaterOrEqual(datos.inicioVentanaGolpe, 0.8f, "Un golpe lento y telegrafiado.");
            var castigo = datos.duracionAtaque - datos.finVentanaGolpe + datos.attackCooldown;
            Assert.GreaterOrEqual(castigo, 1.5f, "Tras golpear tarda en recuperarse: ahí se la castiga.");
        }

        [Test]
        public void ElPrefabDeLaEscultura_DuermeConElControladorApagado_YNoQuemaAlTocarla()
        {
            var prefab = GeneradorEnemigos.CargarPrefab("Escultura");
            Assert.IsNotNull(prefab, "Falta el prefab: ejecuta Fragmentum → Regenerar todo.");
            var controlador = prefab.GetComponent<EnemyController>();
            Assert.IsFalse(controlador.enabled, "Dormida, su controlador está apagado.");
            Assert.AreEqual("Escultura", controlador.Datos.name);
            var dormida = prefab.GetComponent<EsculturaDormida>();
            Assert.IsNotNull(dormida);
            Assert.Greater(dormida.radioDespertar, 2f);
            Assert.Greater(dormida.tiempoDespertar, 0.5f, "Tarda en despertar.");

            var contacto = new SerializedObject(controlador).FindProperty("contacto").objectReferenceValue as Hitbox;
            Assert.IsNotNull(contacto);
            Assert.IsFalse(contacto.gameObject.activeSelf, "Sin daño por contacto, ni dormida ni despierta.");
            Assert.AreEqual(2.4f, prefab.GetComponent<CapsuleCollider2D>().size.y, 0.05f, "Del tamaño del diseño (~2,4 u).");
        }

        // --- La Medusa ---

        [Test]
        public void LaMedusa_RecorreSuLinea_YDosALaContraVanAlReves()
        {
            Assert.AreEqual(new Vector2(0f, 2f), MedusaHielo.Desplazamiento(1f, true, 2f, 4f, 0f), "Vertical: en un cuarto de periodo, arriba del todo.");
            Assert.AreEqual(new Vector2(2f, 0f), MedusaHielo.Desplazamiento(1f, false, 2f, 4f, 0f), "Horizontal: a la derecha del todo.");
            var a = MedusaHielo.Desplazamiento(1f, true, 2f, 4f, 0f);
            var b = MedusaHielo.Desplazamiento(1f, true, 2f, 4f, 2f);
            Assert.AreEqual(-a.y, b.y, 1e-4f, "Con medio periodo de desfase, van al revés.");
        }

        [Test]
        public void LaMedusa_NoMuere_ElGolpeLaApartaYLaEncoge()
        {
            var objeto = new GameObject("Medusa");
            try
            {
                objeto.AddComponent<Rigidbody2D>();
                var medusa = objeto.AddComponent<MedusaHielo>();
                Assert.IsTrue(medusa.EstaVivo);

                Assert.IsTrue(medusa.RecibirGolpe(new Golpe(99, new Vector2(-1f, 0f))), "La espada la nota…");
                Assert.IsTrue(medusa.EstaVivo, "…pero no muere, por fuerte que sea.");
                Assert.IsTrue(medusa.Encogida, "Se encoge.");
                Assert.AreEqual(medusa.empuje, medusa.Apartado.x, 1e-4f, "Y se aparta lejos de quien golpea.");
                Assert.AreEqual(0f, medusa.Apartado.y, 1e-4f);

                Assert.IsFalse(medusa.RecibirGolpe(new Golpe(1, new Vector2(-1f, 0f))), "Encogida, otro golpe no la mueve más.");
                Assert.AreEqual(medusa.empuje, medusa.Apartado.magnitude, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void ElPrefabDeLaMedusa_LaEspadaLaNota_KaelLaAtraviesa_YSuContactoQuema()
        {
            var prefab = GeneradorEnemigos.CargarPrefab(GeneradorEnemigos.Medusa);
            Assert.IsNotNull(prefab, "Falta el prefab: ejecuta Fragmentum → Regenerar todo.");
            Assert.AreEqual(RigidbodyType2D.Kinematic, prefab.GetComponent<Rigidbody2D>().bodyType);
            Assert.AreEqual(LayerMask.NameToLayer(Capas.Enemy), prefab.layer);
            Assert.IsNull(prefab.GetComponent<EnemyController>(), "No anda ni persigue: no es un EnemyController.");
            Assert.IsNull(prefab.GetComponent<Health>(), "No tiene vida: no muere.");

            var medusa = prefab.GetComponent<MedusaHielo>();
            Assert.IsNotNull(medusa.Cuerpo, "Lo que encoge está asignado.");
            var zonaGolpe = medusa.Cuerpo.GetComponent<CircleCollider2D>();
            Assert.IsNotNull(zonaGolpe);
            Assert.IsFalse(zonaGolpe.isTrigger);
            Assert.AreEqual(LayerMask.NameToLayer(Capas.Enemy), zonaGolpe.gameObject.layer, "La espada solo golpea la capa Enemy.");
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer(Capas.Enemy), LayerMask.NameToLayer(Capas.Player)),
                "Esa capa no choca con Kael: la atraviesa.");

            var contacto = medusa.Cuerpo.GetComponentInChildren<Hitbox>(true);
            Assert.IsNotNull(contacto, "Quema al contacto.");
            Assert.AreEqual(LayerMask.NameToLayer(Capas.EnemyWeapon), contacto.gameObject.layer);
            Assert.IsTrue(contacto.GetComponent<Collider2D>().isTrigger);
            Assert.AreEqual(Capas.Mascara(Capas.Player), (int)contacto.Objetivos);
            Assert.IsFalse(contacto.GolpearUnaVezPorActivacion, "Quema mientras se la toca; los i-frames marcan el ritmo.");
            Assert.AreEqual(1, contacto.Danio);
            Assert.AreEqual(GeneradorEnemigos.RadioContactoMedusa, contacto.GetComponent<CircleCollider2D>().radius, 1e-5f);
            Assert.IsTrue(contacto.transform.IsChildOf(medusa.Cuerpo), "Encoge con ella: encogida deja más hueco.");
        }

        // --- La Plañidera ---

        [Test]
        public void LaPlanidera_EsElMiniJefe_ComoElGolem_PeroFlotaDespacioYNoQuemaAlTocarla()
        {
            var datos = GeneradorEnemigos.CargarDatos("Planidera");
            Assert.IsNotNull(datos, "Faltan sus datos: ejecuta Fragmentum → Regenerar todo.");
            var golem = GeneradorEnemigos.CargarDatos("GolemCeniza");
            Assert.AreEqual(golem.vidaMaxima, datos.vidaMaxima, "Como el Golem de Ceniza: 10 de vida.");
            Assert.IsTrue(datos.hiperarmadura, "Los golpes no la interrumpen: por eso no lleva fotograma de daño.");
            Assert.AreEqual(1f, datos.resistenciaRetroceso, 1e-5f, "Flota: la espada no la empuja.");
            Assert.AreEqual(0, datos.danioContacto, "Lo suyo son las esquirlas y el barrido: tocarla no hace daño.");
            Assert.That(datos.velocidadPersecucion, Is.InRange(0.5f, 2f), "Flota hacia Kael, despacio.");
            Assert.GreaterOrEqual(datos.inicioVentanaGolpe, 0.5f, "El barrido avisa: echa el brazo atrás medio segundo.");
            Assert.IsInstanceOf<DeteccionConVision>(datos.deteccion, "Ve con línea de visión: el puente de la plaza tapa a Kael.");
            Assert.IsFalse(datos.inmuneALava);
        }

        [Test]
        public void ElPrefabDeLaPlanidera_LlevaSusEsquirlas_AvisaAntesDeLanzarlas_YSeQuedaDesarmada()
        {
            var prefab = GeneradorEnemigos.CargarPrefab("Planidera");
            Assert.IsNotNull(prefab, "Falta el prefab: ejecuta Fragmentum → Regenerar todo.");
            var controlador = prefab.GetComponent<EnemyController>();
            Assert.IsTrue(controlador.enabled, "Empieza acechando: manda su controlador.");
            Assert.AreEqual("Planidera", controlador.Datos.name);
            Assert.IsNull(prefab.GetComponent<EsculturaDormida>());
            Assert.IsNull(prefab.GetComponent<Pesar>());

            var planidera = prefab.GetComponent<Planidera>();
            Assert.IsNotNull(planidera, "Las esquirlas las lleva su componente.");
            Assert.AreEqual(3, planidera.esquirlas, "Las lanza una tras otra.");
            Assert.AreEqual(1, planidera.danioEsquirla);
            Assert.GreaterOrEqual(planidera.tiempoApuntar, 0.6f, "El aviso da tiempo a reaccionar.");
            Assert.AreEqual(2f, planidera.tiempoDesarmada, 1e-5f, "Desarmada llora unos 2 s: ahí se la castiga.");
            Assert.AreEqual(
                planidera.tiempoAlzar + planidera.tiempoApuntar + 2 * planidera.intervaloEsquirlas + planidera.tiempoDesarmada + planidera.tiempoRehacer,
                planidera.DuracionRafaga, 1e-5f);

            var cuerpo = prefab.GetComponent<CapsuleCollider2D>();
            Assert.AreEqual(GeneradorEnemigos.TamanoPlanidera, cuerpo.size);
            var pivote = new SerializedObject(controlador).FindProperty("pivote").objectReferenceValue as Transform;
            var serializado = new SerializedObject(planidera);
            var arco = serializado.FindProperty("arco").objectReferenceValue as Transform;
            Assert.IsNotNull(arco, "De dónde salen las esquirlas.");
            Assert.IsTrue(arco.IsChildOf(pivote), "El arco se voltea con ella.");
            Assert.Greater(arco.localPosition.y, cuerpo.size.y, "Salen de encima de su cabeza.");
            Assert.AreSame(pivote, serializado.FindProperty("pivote").objectReferenceValue, "Mientras apunta, se gira ella sola.");

            var contacto = new SerializedObject(controlador).FindProperty("contacto").objectReferenceValue as Hitbox;
            Assert.IsNotNull(contacto);
            Assert.IsFalse(contacto.gameObject.activeSelf, "Sin daño por contacto.");
            var arma = new SerializedObject(controlador).FindProperty("arma").objectReferenceValue as Hitbox;
            var zona = arma.GetComponent<BoxCollider2D>();
            Assert.LessOrEqual(arma.transform.localPosition.x - zona.size.x * 0.5f, 0f, "El barrido empieza en su cuerpo: pegarse a ella no lo esquiva.");
            Assert.GreaterOrEqual(arma.transform.localPosition.x + zona.size.x * 0.5f, controlador.Datos.attackRadius, "Y llega hasta donde ataca.");
        }
    }
}
