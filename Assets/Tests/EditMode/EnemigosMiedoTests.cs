using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Enemies;
using Fragmentum.Enemies.Deteccion;
using Fragmentum.Levels.Escenario;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Los enemigos del mundo del Miedo (fase C), sin escena: sus prefabs y sus datos con las medidas
    /// del diseño (<c>Nivel3_Diseno.md</c>, sección 5) y las reglas puras: lo que oye el Centinela
    /// Ciego con su cono y el Acechador con su oído, lo que se ve de un Espectro, la detección por
    /// tacto, la luz que ilumina un punto y el tronco de un árbol.
    /// </summary>
    public class EnemigosMiedoTests
    {
        private static GameObject Prefab(string variante)
        {
            var prefab = GeneradorEnemigos.CargarPrefab(variante);
            Assert.IsNotNull(prefab, $"Falta el prefab de {variante}: ejecuta Fragmentum → Regenerar todo.");
            return prefab;
        }

        private static EnemyData Datos(string nombre)
        {
            var datos = GeneradorEnemigos.CargarDatos(nombre);
            Assert.IsNotNull(datos, $"Falta Assets/Data/Enemigos/{nombre}.asset: ejecuta Fragmentum → Regenerar todo.");
            return datos;
        }

        // --- Prefabs y datos ---

        [Test]
        public void ElEspectro_EmpiezaDormido_YAtacaAlRitmoDelEco_SinQueDejeDeVerseVenir()
        {
            var prefab = Prefab("Espectro");
            var espectro = prefab.GetComponent<Espectro>();
            Assert.IsNotNull(espectro, "Lleva su sueño y su casi invisibilidad.");
            Assert.IsTrue(espectro.empiezaDormido);
            Assert.IsFalse(prefab.GetComponent<EnemyController>().enabled, "Dormido, el controlador está apagado.");
            Assert.AreEqual(1.5f, espectro.radioDespertar, 1e-4f, "Despierta si Kael pasa a 1,5 u.");
            Assert.AreEqual(0.25f, espectro.opacidadEnLaLuzDeKael, 1e-4f, "En la luz de Kael se adivina.");
            Assert.Greater(espectro.alcanceEspera, DisenoNivel3.RadioAntorcha, "Los que despierta una antorcha esperan todos en su borde.");
            var serializado = new SerializedObject(espectro);
            Assert.IsNotNull(serializado.FindProperty("pivote").objectReferenceValue);
            Assert.IsNotNull(serializado.FindProperty("sprite").objectReferenceValue);

            var datos = Datos("Espectro");
            Assert.IsInstanceOf<DeteccionEnOscuridad>(datos.deteccion, "Despierto, solo se acerca a oscuras.");
            Assert.AreEqual(ConstructorEspectro.InicioDelGolpe, datos.inicioVentanaGolpe, 1e-4f, "Avisa con a1 al ritmo del Eco (1,6×): 0,31 s.");
            Assert.AreEqual(0.5f / ConstructorEcoIra.VelocidadAtaque, datos.inicioVentanaGolpe, 1e-4f);
            Assert.Less(1f / espectro.velocidadFundido + 0.1f, datos.inicioVentanaGolpe,
                "Solo se le ve entero al atacar: aparece del todo al menos una décima antes del golpe.");
            Assert.Greater(datos.finVentanaGolpe, datos.inicioVentanaGolpe);
            Assert.LessOrEqual(datos.finVentanaGolpe, datos.duracionAtaque);
            Assert.AreEqual(2, datos.vidaMaxima, "Frágil.");
            Assert.AreEqual(3.8f, datos.velocidadPersecucion, 1e-4f, "Rápido.");
        }

        [Test]
        public void ElCentinelaCiego_OyeConSuCono_YSuBayonetaLlegaAUnaCuatro()
        {
            var prefab = Prefab("CentinelaCiego");
            var oido = prefab.GetComponent<CentinelaCiego>();
            Assert.IsNotNull(oido, "Lleva su oído.");
            Assert.AreEqual(1f, oido.oidoDelante, 1e-4f, "Delante, hasta donde llega el ruido.");
            Assert.AreEqual(0.5f, oido.oidoDetras, 1e-4f, "Detrás, la mitad.");
            Assert.AreEqual(0.6f, oido.tiempoEscucha, 1e-4f, "El aviso: ladea la cabeza 0,6 s.");
            Assert.AreEqual(4f, oido.velocidadIr, 1e-4f);
            Assert.AreEqual(2f, oido.tiempoBusqueda, 1e-4f, "Si en 2 s no encuentra a nadie, vuelve a su ronda.");

            var datos = Datos("CentinelaCiego");
            Assert.IsInstanceOf<DeteccionPorTacto>(datos.deteccion, "Su controlador solo nota el roce.");
            Assert.AreEqual(1.2f, datos.detectionRadius, 1e-4f);
            Assert.AreEqual(2, datos.danio, "La estocada quita 2.");
            Assert.AreEqual(5, datos.vidaMaxima);

            var arma = (Hitbox)new SerializedObject(prefab.GetComponent<EnemyController>()).FindProperty("arma").objectReferenceValue;
            var caja = arma.GetComponent<BoxCollider2D>();
            var alcance = arma.transform.localPosition.x + caja.size.x * 0.5f;
            Assert.AreEqual(datos.attackRadius, alcance, 0.1f, "La bayoneta llega a 1,4 u de su centro.");
        }

        [Test]
        public void ElAcechador_EsUnMiniJefeLargoYBajo_ConSuEmbestidaApagada()
        {
            var prefab = Prefab("Acechador");
            var acechador = prefab.GetComponent<Acechador>();
            Assert.IsNotNull(acechador);
            Assert.AreEqual(2f, acechador.oido, 1e-4f, "Oye el doble de lejos: 12 u corriendo.");
            Assert.AreEqual(0.7f, acechador.tiempoAgazapado, 1e-4f, "El aviso.");
            Assert.AreEqual(10f, acechador.velocidadEmbestida, 1e-4f);
            Assert.AreEqual(2, acechador.danioEmbestida);
            Assert.AreEqual(2.5f, acechador.tiempoAturdido, 1e-4f);
            Assert.AreEqual(2f, acechador.multiplicadorAturdido, 1e-4f, "Aturdido, los golpes valen el doble.");

            var cuerpo = prefab.GetComponent<CapsuleCollider2D>();
            Assert.AreEqual(CapsuleDirection2D.Horizontal, cuerpo.direction, "Un lobo enorme: más largo que alto.");
            Assert.AreEqual(GeneradorEnemigos.TamanoAcechador, cuerpo.size);

            var embestida = (Hitbox)new SerializedObject(acechador).FindProperty("embestida").objectReferenceValue;
            Assert.IsNotNull(embestida, "La zona de daño de la embestida.");
            Assert.IsFalse(embestida.gameObject.activeSelf, "Solo encendida mientras embiste.");
            Assert.IsTrue(embestida.GetComponent<Collider2D>().isTrigger);
            Assert.AreEqual(LayerMask.NameToLayer(Capas.EnemyWeapon), embestida.gameObject.layer);
            Assert.AreEqual(Capas.Mascara(Capas.Player), embestida.Objetivos.value);

            var contacto = (Hitbox)new SerializedObject(prefab.GetComponent<EnemyController>()).FindProperty("contacto").objectReferenceValue;
            Assert.IsFalse(contacto.gameObject.activeSelf, "Tocarlo no hace daño: solo la embestida y el mordisco.");

            var datos = Datos("Acechador");
            Assert.AreEqual(10, datos.vidaMaxima, "Como el Golem y la Plañidera.");
            Assert.IsTrue(datos.hiperarmadura, "Los golpes no lo interrumpen.");
            Assert.IsInstanceOf<DeteccionPorTacto>(datos.deteccion, "Muerde si Kael se le pega.");
            Assert.AreEqual(0.5f, datos.inicioVentanaGolpe, 1e-4f, "Echa la cabeza atrás 0,5 s.");
            Assert.AreEqual(1, datos.danio, "El mordisco quita 1.");
            Assert.AreEqual(0, datos.danioContacto);
            CollectionAssert.Contains(TablaPuntaje.MiniJefes, "Acechador");
            Assert.AreEqual(700, TablaPuntaje.PuntosDeEnemigo("Acechador"), "Entre la Plañidera (600) y el jefe.");
        }

        // --- Las reglas ---

        [Test]
        public void ElCentinelaCiego_OyeDelanteHastaDondeLlegaElRuido_YDetrasLaMitad()
        {
            var oreja = new Vector2(0f, 0.8f);
            const int mirandoALaDerecha = 1;

            // Corriendo (6 u): delante hasta 6; detrás, hasta 3.
            Assert.IsTrue(CentinelaCiego.Oye(oreja, mirandoALaDerecha, new Vector2(5.9f, 0.8f), 6f, 1f, 0.5f));
            Assert.IsFalse(CentinelaCiego.Oye(oreja, mirandoALaDerecha, new Vector2(6.2f, 0.8f), 6f, 1f, 0.5f), "Más allá de los anillos no oye.");
            Assert.IsTrue(CentinelaCiego.Oye(oreja, mirandoALaDerecha, new Vector2(-2.9f, 0.8f), 6f, 1f, 0.5f));
            Assert.IsFalse(CentinelaCiego.Oye(oreja, mirandoALaDerecha, new Vector2(-3.2f, 0.8f), 6f, 1f, 0.5f), "Por la espalda se le puede sorprender.");

            // El chasquido de un cepo (8 u): delante hasta 8; detrás, hasta 4.
            Assert.IsTrue(CentinelaCiego.Oye(oreja, mirandoALaDerecha, new Vector2(7.9f, 0.8f), 8f, 1f, 0.5f));
            Assert.IsFalse(CentinelaCiego.Oye(oreja, mirandoALaDerecha, new Vector2(-4.2f, 0.8f), 8f, 1f, 0.5f));

            // Mirando a la izquierda, delante y detrás se cambian.
            Assert.IsTrue(CentinelaCiego.Oye(oreja, -1, new Vector2(-5.9f, 0.8f), 6f, 1f, 0.5f));
            Assert.IsFalse(CentinelaCiego.Oye(oreja, -1, new Vector2(3.2f, 0.8f), 6f, 1f, 0.5f));
        }

        [Test]
        public void ElAcechador_OyeElDobleDeLejos_EnTodasDirecciones()
        {
            Assert.IsTrue(Acechador.Oye(11.9f, 6f, 2f), "Corriendo, a 12 u.");
            Assert.IsFalse(Acechador.Oye(12.1f, 6f, 2f));
            Assert.IsTrue(Acechador.Oye(15.9f, 8f, 2f), "Un cepo, a 16 u.");
        }

        [Test]
        public void ElEspectro_AOscurasSoloSeAdivinaEnLaLuzDeKael_SalvoAlAtacarOBajoUnaAntorcha()
        {
            const float adivina = 0.25f;
            Assert.AreEqual(1f, Espectro.OpacidadEn(false, false, false, false, adivina), "En un nivel con luz se ve siempre.");
            Assert.AreEqual(0f, Espectro.OpacidadEn(true, false, false, false, adivina), "A oscuras, no se ve.");
            Assert.AreEqual(adivina, Espectro.OpacidadEn(true, false, false, true, adivina), "En la luz de Kael se adivina.");
            Assert.AreEqual(1f, Espectro.OpacidadEn(true, true, false, false, adivina), "Al atacar se ve entero.");
            Assert.AreEqual(1f, Espectro.OpacidadEn(true, false, true, false, adivina), "Bajo una antorcha, también.");
        }

        [Test]
        public void LaDeteccionPorTacto_SoloNotaLoQueTieneEncima_AunqueSeaInvisible()
        {
            var tacto = ScriptableObject.CreateInstance<DeteccionPorTacto>();
            try
            {
                ContextoDeteccion Contexto(float distancia, bool vivo = true, bool oculto = false) =>
                    new ContextoDeteccion(Vector2.zero, new Vector2(distancia, 0f), 1.2f, vivo, oculto, 0f, float.NegativeInfinity, 0);

                Assert.IsTrue(tacto.Detecta(Contexto(1f)));
                Assert.IsFalse(tacto.Detecta(Contexto(1.5f)), "No ve ni oye: más allá del roce, nada.");
                Assert.IsTrue(tacto.Detecta(Contexto(1f, oculto: true)), "No ve: la invisibilidad no sirve contra él.");
                Assert.IsFalse(tacto.Detecta(Contexto(1f, vivo: false)));
            }
            finally
            {
                Object.DestroyImmediate(tacto);
            }
        }

        private sealed class LuzFalsa : IFuenteLuz
        {
            public LuzFalsa(Vector2 posicion, float radio)
            {
                Posicion = posicion;
                Radio = radio;
            }

            public Vector2 Posicion { get; }
            public float Radio { get; }
            public bool Encendida { get; set; } = true;
        }

        [Test]
        public void LaLuzQueIluminaUnPunto_EsLaDeCentroMasCercano_YSoloSiEstaEncendida()
        {
            Iluminacion.Reiniciar();
            var lejana = new LuzFalsa(new Vector2(-6f, 0f), 8f);
            var cercana = new LuzFalsa(new Vector2(3f, 0f), 8f);
            try
            {
                Iluminacion.Registrar(lejana);
                Iluminacion.Registrar(cercana);
                Assert.AreSame(cercana, Iluminacion.FuenteEn(Vector2.zero), "Las dos lo iluminan: la más cercana.");
                cercana.Encendida = false;
                Assert.AreSame(lejana, Iluminacion.FuenteEn(Vector2.zero));
                Assert.IsNull(Iluminacion.FuenteEn(new Vector2(20f, 0f)));
                Assert.IsFalse(Iluminacion.EstaIluminado(new Vector2(20f, 0f)));
                Assert.IsTrue(Iluminacion.EstaIluminado(Vector2.zero));
            }
            finally
            {
                Iluminacion.Reiniciar();
            }
        }

        [Test]
        public void ElTroncoDeUnArbol_EstaDePieSobreSuPie()
        {
            var objeto = new GameObject("Arbol");
            try
            {
                objeto.transform.position = new Vector3(98f, 0f, 0f);
                var arbol = objeto.AddComponent<Arbol>();
                arbol.Fijar(DisenoNivel3.TamanoArbol);
                Assert.AreEqual(new Rect(97.4f, 0f, 1.2f, 6f).ToString(), arbol.Tronco.ToString());
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }
    }
}
