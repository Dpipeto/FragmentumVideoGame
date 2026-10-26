using System.Linq;
using Fragmentum.Core;
using Fragmentum.EditorTools;
using Fragmentum.Levels.Escenario;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Las mecánicas del mundo del Miedo (fase B), sin escena: los prefabs de la antorcha, el cepo y
    /// la rama podrida con las medidas del diseño; la luz, la interacción y los anillos de ruido de
    /// Kael; y las reglas puras (el parpadeo, cuándo se ve un cepo, qué luz toca en cada tramo, qué
    /// tiene Kael al alcance).
    /// </summary>
    public class MecanicasMiedoTests
    {
        private static GameObject Prefab(string nombre)
        {
            var prefab = GeneradorMecanicas.Cargar(nombre);
            Assert.IsNotNull(prefab, $"Falta el prefab {nombre}: ejecuta Fragmentum → Regenerar todo.");
            return prefab;
        }

        // --- Prefabs ---

        [Test]
        public void LaAntorcha_TieneLasMedidasDelAnexoA_YSuLuzApagada()
        {
            var prefab = Prefab(GeneradorMecanicas.Antorcha);
            var antorcha = prefab.GetComponent<Antorcha>();
            Assert.IsNotNull(antorcha);
            Assert.AreEqual(DisenoNivel3.RadioAntorcha, antorcha.radio, 1e-4f, "Anexo A: 8 u.");
            Assert.AreEqual(DisenoNivel3.DuracionAntorcha, antorcha.duracion, 1e-4f, "Anexo A: 15 s.");
            Assert.AreEqual(DisenoNivel3.ParpadeoAntorcha, antorcha.parpadeo, 1e-4f);
            Assert.AreEqual(DisenoNivel3.DistanciaEncender, antorcha.distanciaEncender, 1e-4f);
            Assert.AreEqual(ESTADOS_ANTORCHA.UNLIT, antorcha.Estado);
            Assert.IsNull(prefab.GetComponentInChildren<Collider2D>(true), "No se choca con ella.");

            var luz = antorcha.Luz;
            Assert.IsNotNull(luz, "Lleva su luz 2D.");
            Assert.IsFalse(luz.enabled, "Apagada hasta que Kael la enciende.");
            Assert.AreEqual(Light2D.LightType.Point, luz.lightType);
            Assert.AreEqual(DisenoNivel3.RadioAntorcha, luz.pointLightOuterRadius, 1e-4f, "La luz que se ve llega a donde llega la que cuenta.");
            Assert.AreEqual(GeneradorMecanicas.AlturaLlama, luz.transform.localPosition.y, 1e-4f, "En la llama.");
            Assert.AreEqual(GeneradorMecanicas.AlturaLlama, antorcha.Posicion.y - prefab.transform.position.y, 1e-4f, "El círculo que cuenta sale de la llama.");
        }

        [Test]
        public void ElCepo_SujetaUnSegundo_QuitaUno_SuenaAOchoUnidades_YSeVeAUna()
        {
            var prefab = Prefab(GeneradorMecanicas.Cepo);
            var cepo = prefab.GetComponent<Cepo>();
            Assert.IsNotNull(cepo);
            Assert.AreEqual(DisenoNivel3.DanioCepo, cepo.danio);
            Assert.AreEqual(DisenoNivel3.SujecionCepo, cepo.sujecion, 1e-4f);
            Assert.AreEqual(DisenoNivel3.RadioRuidoCepo, cepo.radioRuido, 1e-4f);
            Assert.AreEqual(DisenoNivel3.DistanciaVerCepo, cepo.distanciaVer, 1e-4f, "Anexo A: trampas invisibles hasta 1 u.");
            Assert.IsNull(prefab.GetComponentInChildren<Collider2D>(true), "Nota a Kael por su caja, sin collider: ni lo frena ni lo sostiene.");
            Assert.AreEqual(GeneradorMecanicas.TamanoCepo, cepo.Zona.size);
            Assert.AreEqual(0f, cepo.Zona.yMin - prefab.transform.position.y, 1e-4f, "El pivote está en el suelo.");
            Assert.IsNotNull(prefab.GetComponentInChildren<SpriteRenderer>(true), "Su dibujo (sin arte, un rectángulo).");
            if (ConstructorMecanicasMundo3.CepoAbierto != null)
            {
                var serializado = new SerializedObject(cepo);
                Assert.AreEqual(ConstructorMecanicasMundo3.CepoAbierto, serializado.FindProperty("spriteAbierto").objectReferenceValue, "Con arte, abierto…");
                Assert.AreEqual(ConstructorMecanicasMundo3.CepoCerrado, serializado.FindProperty("spriteCerrado").objectReferenceValue, "…y cerrado.");
            }
        }

        [Test]
        public void LaRamaPodrida_CedeConLosTiemposDelMiedo_YAOscurasEsComoLasSanas()
        {
            var prefab = Prefab(GeneradorMecanicas.RamaPodrida);
            Assert.AreEqual(LayerMask.NameToLayer(Capas.Suelo), prefab.layer, "Se pisa como cualquier rama.");
            var caja = prefab.GetComponent<BoxCollider2D>();
            Assert.IsFalse(caja.isTrigger);
            Assert.AreEqual(GeneradorMecanicas.TamanoRama, caja.size);
            Assert.AreEqual(0f, caja.offset.y + caja.size.y * 0.5f, 1e-4f, "El pivote está en la superficie, como en la plataforma inestable.");

            var plataforma = prefab.GetComponent<PlataformaInestable>();
            Assert.AreEqual(DisenoNivel3.CrujidoRama, plataforma.retardoRotura, 1e-4f, "Cruje 0,3 s.");
            Assert.AreEqual(DisenoNivel3.VueltaRama, plataforma.duracionCaida + plataforma.tiempoReaparicion, 1e-4f, "Y vuelve a los 4 s de partirse.");

            var rama = prefab.GetComponent<RamaPodrida>();
            Assert.IsNotNull(rama);
            Assert.AreEqual(GeneradorMecanicas.ColorRamaSinArte, rama.ColorSana, "A oscuras, del color de las sanas.");
            Assert.AreNotEqual(rama.ColorSana, rama.ColorPodrida, "Bajo una antorcha, se delata.");
            if (ConstructorMecanicasMundo3.HayRamas)
            {
                Assert.IsTrue(rama.ConArte, "Con arte, lleva sus cuatro dibujos.");
                Assert.AreEqual(ConstructorMecanicasMundo3.RamaSana, rama.DibujoDe(ESTADOS_PLATAFORMA.INTACT, false, 0f, 1f), "A oscuras, es la sana.");
                Assert.AreEqual(ConstructorMecanicasMundo3.RamaPodrida, rama.DibujoDe(ESTADOS_PLATAFORMA.INTACT, true, 0f, 1f), "Con luz, la podrida.");
                Assert.AreEqual(ConstructorMecanicasMundo3.RamaPodrida, rama.DibujoDe(ESTADOS_PLATAFORMA.CRACKED, false, 0f, 1f), "Crujiendo se delata, aun a oscuras.");
                Assert.AreEqual(ConstructorMecanicasMundo3.RamaRota, rama.DibujoDe(ESTADOS_PLATAFORMA.BROKEN, false, 0.1f, 1f), "Al partirse, se dobla…");
                Assert.AreEqual(ConstructorMecanicasMundo3.Astillas, rama.DibujoDe(ESTADOS_PLATAFORMA.BROKEN, false, 0.6f, 1f), "…y se deshace en astillas.");
            }
        }

        [Test]
        public void Kael_LlevaSuLuzApagada_LaInteraccion_YLosAnillosDeRuido()
        {
            var jugador = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorJugador.RutaPrefab);
            Assert.IsNotNull(jugador, "Falta el prefab del jugador.");
            Assert.IsNotNull(jugador.GetComponent<InteraccionJugador>(), "Interactuar (E).");

            var luz = jugador.GetComponent<LuzJugador>();
            Assert.IsNotNull(luz);
            Assert.IsNotNull(luz.Luz, "Su luz 2D.");
            Assert.IsFalse(luz.Luz.enabled, "Apagada: solo la encienden los niveles a oscuras.");
            Assert.AreEqual(Light2D.LightType.Point, luz.Luz.lightType);
            Assert.AreEqual(DisenoNivel3.RadioLuzKael, luz.Luz.pointLightOuterRadius, 1e-4f, "Anexo A: 3 u.");
            Assert.AreEqual(1.5f, luz.Intensidad, 1e-4f, "Anexo A: intensidad 1,5.");
            Assert.IsTrue(luz.Luz.transform.IsChildOf(jugador.transform));

            var anillos = jugador.GetComponent<AnillosDeRuido>();
            Assert.IsNotNull(anillos);
            var material = new SerializedObject(anillos).FindProperty("material").objectReferenceValue as Material;
            Assert.IsNotNull(material, "Los anillos tienen con qué dibujarse.");
            StringAssert.Contains("Unlit", material.shader.name, "Y se ven a oscuras: no les afecta la luz.");
        }

        // --- Reglas puras ---

        [Test]
        public void LaAntorcha_SoloParpadeaEnSusUltimosSegundos()
        {
            for (var t = 0f; t < 3f; t += 0.05f)
            {
                Assert.AreEqual(1f, Antorcha.FactorParpadeo(10f, 3f, t), 1e-5f, "Lejos del final, la llama no parpadea.");
            }

            var minimo = 1f;
            for (var t = 0f; t < 3f; t += 0.01f)
            {
                var factor = Antorcha.FactorParpadeo(1.5f, 3f, t);
                Assert.That(factor, Is.InRange(Antorcha.MinimoParpadeo - 1e-5f, 1f + 1e-5f));
                minimo = Mathf.Min(minimo, factor);
            }

            Assert.Less(minimo, 0.5f, "En los últimos segundos, sí: baja de verdad.");
            Assert.AreEqual(1f, Antorcha.FactorParpadeo(1f, 0f, 0.3f), 1e-5f, "Sin parpadeo, nunca.");
        }

        [Test]
        public void ElCepo_SeVeCerrado_ConLuz_OConKaelAUnaUnidad()
        {
            Assert.IsFalse(Cepo.SeVeCon(ESTADOS_CEPO.ARMED, false, 1.5f, 1f), "A oscuras y lejos, no se ve.");
            Assert.IsTrue(Cepo.SeVeCon(ESTADOS_CEPO.ARMED, false, 1f, 1f), "A 1 u, sí.");
            Assert.IsTrue(Cepo.SeVeCon(ESTADOS_CEPO.ARMED, true, 20f, 1f), "Con la luz de una antorcha, desde lejos.");
            Assert.IsTrue(Cepo.SeVeCon(ESTADOS_CEPO.CLOSED, false, 20f, 1f), "Cerrado ya no se esconde.");

            var cepo = new Rect(-0.4f, 0f, 0.8f, 0.3f);
            Assert.AreEqual(0f, Cepo.Separacion(cepo, new Rect(0f, 0f, 0.8f, 1.8f)), 1e-5f, "Encima: se tocan.");
            Assert.AreEqual(1f, Cepo.Separacion(cepo, new Rect(-2.2f, 0f, 0.8f, 1.8f)), 1e-5f, "Al lado: lo que hay entre los bordes.");
            Assert.AreEqual(0.5f, Cepo.Separacion(cepo, new Rect(-0.4f, 0.8f, 0.8f, 1.8f)), 1e-5f, "Encima, en el aire.");
        }

        [Test]
        public void LaLuzDelNivel_EligeElTramoDeKael_OElMasCercano()
        {
            var tramos = DisenoNivel3.Luces.Select(l => new TramoDeLuz(l.Nombre, l.Desde, l.Hasta, l.Color, l.Intensidad)).ToArray();
            Assert.AreEqual(0, LuzDelNivel.TramoEn(tramos, 2f), "El inicio, en la linde.");
            Assert.AreEqual(1, LuzDelNivel.TramoEn(tramos, 36f), "Cada tramo empieza donde acaba el anterior.");
            Assert.AreEqual(1, LuzDelNivel.TramoEn(tramos, 41f), "La trinchera, a oscuras.");
            Assert.AreEqual(1, LuzDelNivel.TramoEn(tramos, DisenoNivel3.Maranas[0].Desde), "La ciénaga, a oscuras: es parte del bosque cerrado.");
            Assert.AreEqual(2, LuzDelNivel.TramoEn(tramos, DisenoNivel3.Arboles[1]), "El claro.");
            Assert.AreEqual(3, LuzDelNivel.TramoEn(tramos, DisenoNivel3.Jefe), "La arena.");
            Assert.AreEqual(0, LuzDelNivel.TramoEn(tramos, -50f), "Fuera del nivel, el más cercano.");
            Assert.AreEqual(3, LuzDelNivel.TramoEn(tramos, 500f));
            Assert.AreEqual(-1, LuzDelNivel.TramoEn(new TramoDeLuz[0], 3f));
        }

        [Test]
        public void LosAnillosDeRuido_CrecenHastaDondeSeOye()
        {
            Assert.AreEqual(0.3f, AnillosDeRuido.RadioEn(0f, 6f), 1e-5f, "Salen de los pies.");
            Assert.AreEqual(6f, AnillosDeRuido.RadioEn(1f, 6f), 1e-5f, "Y llegan a donde se oye.");
            var anterior = 0f;
            for (var t = 0f; t <= 1f; t += 0.05f)
            {
                var radio = AnillosDeRuido.RadioEn(t, 8f);
                Assert.GreaterOrEqual(radio, anterior, "Solo crecen.");
                anterior = radio;
            }
        }

        private sealed class Usable : IInteractuable
        {
            public Usable(Vector2 punto, float radio, bool disponible = true)
            {
                PuntoInteraccion = punto;
                RadioInteraccion = radio;
                Disponible = disponible;
            }

            public Vector2 PuntoInteraccion { get; }
            public float RadioInteraccion { get; }
            public bool Disponible { get; }
            public string Accion => "Usar";
            public int Usos { get; private set; }
            public void Interactuar(GameObject quien) => Usos++;
        }

        [Test]
        public void AlAlcance_EsLoMasCercanoQueSePuedeUsar()
        {
            Interacciones.Reiniciar();
            try
            {
                var lejos = new Usable(new Vector2(3f, 0f), 1.5f);
                var cerca = new Usable(new Vector2(1f, 0f), 1.5f);
                var gastada = new Usable(new Vector2(0.2f, 0f), 1.5f, disponible: false);
                Interacciones.Registrar(lejos);
                Interacciones.Registrar(cerca);
                Interacciones.Registrar(gastada);
                Interacciones.Registrar(cerca);
                Assert.AreEqual(3, Interacciones.Cantidad, "Cada cosa se registra una vez.");

                Assert.AreSame(cerca, Interacciones.AlAlcance(Vector2.zero), "La más cercana que se puede usar; la gastada no cuenta.");
                Assert.AreSame(lejos, Interacciones.AlAlcance(new Vector2(3.5f, 0f)));
                Assert.IsNull(Interacciones.AlAlcance(new Vector2(-2f, 0f)), "Fuera del alcance de todas.");

                Interacciones.Quitar(cerca);
                Assert.IsNull(Interacciones.AlAlcance(Vector2.zero), "La de x 3 queda a más de 1,5 u.");
            }
            finally
            {
                Interacciones.Reiniciar();
            }
        }
    }
}
