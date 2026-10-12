using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.EditorTools;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Las mecánicas del mundo de la Tristeza sin escena: el aire y la asfixia, la grabación de
    /// los ecos de intentos, el seno de las placas, el tapón de hielo, perder vida sin golpe y
    /// sus prefabs.
    /// </summary>
    public class MecanicasTristezaTests
    {
        // --- El aire ---

        [Test]
        public void BajoElAgua_ElAireDura15s_YLuegoSePierdeUnCorazonCada2s()
        {
            var aire = new ReservaOxigeno(15f, 15f, 2f);
            var perdidos = 0;
            for (var i = 0; i < 149; i++)
            {
                perdidos += aire.Avanzar(0.1f, true); // hasta 14,9 s
            }

            Assert.AreEqual(0, perdidos, "Con aire no se pierde nada.");
            Assert.IsFalse(aire.Asfixiandose);

            perdidos += aire.Avanzar(0.2f, true); // 15,1 s
            Assert.AreEqual(1, perdidos, "Al acabarse el aire, el primer corazón.");
            Assert.IsTrue(aire.Asfixiandose);

            for (var i = 0; i < 17; i++)
            {
                perdidos += aire.Avanzar(0.1f, true); // hasta 16,8 s
            }

            Assert.AreEqual(1, perdidos, "El segundo no llega antes de 2 s.");
            perdidos += aire.Avanzar(0.3f, true); // 17,1 s
            Assert.AreEqual(2, perdidos, "A los 2 s, el segundo.");

            Assert.AreEqual(3, aire.Avanzar(6f, true), "Un paso largo cuenta todos los corazones que caben (a los 19, 21 y 23 s).");
        }

        [Test]
        public void FueraDelAgua_ElAireSeRecargaDeprisa_YLaAsfixiaSeOlvida()
        {
            var aire = new ReservaOxigeno(15f, 15f, 2f);
            aire.Avanzar(16f, true);
            Assert.IsTrue(aire.Asfixiandose);

            Assert.AreEqual(0, aire.Avanzar(0.5f, false));
            Assert.IsFalse(aire.Asfixiandose);
            Assert.AreEqual(0.5f, aire.Fraccion, 1e-3f, "15 s de aire por segundo: a medio segundo, la mitad.");
            aire.Avanzar(1f, false);
            Assert.IsTrue(aire.Lleno);
        }

        [Test]
        public void PerderVida_NoEsUnGolpe_NiRespetaLaInvulnerabilidad()
        {
            var objeto = new GameObject("Vida");
            try
            {
                var vida = objeto.AddComponent<Health>();
                vida.Inicializar(5, 1f);
                var golpes = 0;
                var muertes = 0;
                vida.OnGolpeRecibido += _ => golpes++;
                vida.OnMuerte += () => muertes++;

                Assert.IsTrue(vida.RecibirGolpe(new Golpe(1, Vector2.zero)));
                Assert.IsTrue(vida.EsInvulnerable, "Tras un golpe hay i-frames…");
                Assert.AreEqual(1, vida.PerderVida(1), "…pero el ahogo no es un golpe y no los respeta.");
                Assert.AreEqual(3, vida.Vida);
                Assert.AreEqual(1, golpes, "No avisa de golpe: así no hay retroceso.");

                Assert.AreEqual(3, vida.PerderVida(10), "No baja de cero.");
                Assert.AreEqual(1, muertes);
                Assert.AreEqual(0, vida.PerderVida(1), "Muerto, ya no pierde nada.");
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        // --- Los ecos de intentos ---

        [Test]
        public void LaGrabacion_GuardaSoloLosUltimosInstantes_EnOrden()
        {
            var grabacion = new GrabacionIntento(3);
            for (var i = 0; i < 5; i++)
            {
                grabacion.Grabar(new MuestraEco(new Vector2(i, 0f), null, false));
            }

            Assert.AreEqual(3, grabacion.Cantidad);
            var copia = grabacion.Copiar();
            Assert.AreEqual(2f, copia[0].Posicion.x, "La más vieja que queda.");
            Assert.AreEqual(4f, copia[2].Posicion.x, "La última.");

            grabacion.Limpiar();
            Assert.AreEqual(0, grabacion.Cantidad);
            grabacion.Grabar(new MuestraEco(new Vector2(9f, 0f), null, true));
            Assert.AreEqual(9f, grabacion.Muestra(0).Posicion.x);
            Assert.IsTrue(grabacion.Muestra(0).Volteado);
        }

        // --- Las placas ---

        [Test]
        public void LaPlaca_SubeYBajaSuAmplitud_YDosPlacasALaContraVanAlReves()
        {
            Assert.AreEqual(0f, PlataformaFlotante.Desplazamiento(0f, 0.5f, 3f, 0f), 1e-5f);
            Assert.AreEqual(0.5f, PlataformaFlotante.Desplazamiento(0.75f, 0.5f, 3f, 0f), 1e-4f, "Un cuarto de ciclo: arriba del todo.");
            Assert.AreEqual(-0.5f, PlataformaFlotante.Desplazamiento(2.25f, 0.5f, 3f, 0f), 1e-4f);
            for (var t = 0f; t < 3f; t += 0.37f)
            {
                Assert.AreEqual(-PlataformaFlotante.Desplazamiento(t, 0.5f, 3f, 0f), PlataformaFlotante.Desplazamiento(t, 0.5f, 3f, 1.5f), 1e-4f,
                    "Con medio periodo de desfase, una baja cuando la otra sube.");
            }

            Assert.Less(PlataformaFlotante.VelocidadMaxima(0.5f, 3f), 1.5f, "Baja despacio: Kael la sigue sin despegarse.");
        }

        // --- El tapón de hielo ---

        [Test]
        public void ElTapon_UnGolpeNormalLoAgrieta_YSoloElCargadoLoRompe()
        {
            var objeto = new GameObject("Tapon");
            try
            {
                objeto.AddComponent<BoxCollider2D>();
                var hielo = objeto.AddComponent<HieloRompible>();
                Assert.AreEqual(ESTADOS_HIELO.INTACT, hielo.Estado);

                Assert.IsTrue(hielo.RecibirGolpe(new Golpe(5, Vector2.zero)), "El golpe se nota…");
                Assert.AreEqual(ESTADOS_HIELO.CRACKED, hielo.Estado, "…y lo agrieta…");
                Assert.IsTrue(hielo.Tapa, "…pero sigue tapando el paso, por fuerte que sea.");

                Assert.IsTrue(hielo.RecibirGolpe(new Golpe(1, Vector2.zero, cargado: true)));
                Assert.AreEqual(ESTADOS_HIELO.BROKEN, hielo.Estado, "El Ataque Cargado lo rompe.");
                Assert.IsFalse(hielo.Tapa, "Y deja el hueco abierto.");
                Assert.IsFalse(hielo.EstaVivo);
                Assert.IsFalse(hielo.RecibirGolpe(new Golpe(1, Vector2.zero, cargado: true)), "Roto, ya no recibe golpes.");
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        // --- Los prefabs ---

        [Test]
        public void ElPrefabDeLaPlaca_EsCinematico_YLlevaSuHielo()
        {
            var prefab = GeneradorMecanicas.Cargar(GeneradorMecanicas.PlataformaFlotante);
            Assert.IsNotNull(prefab, "Falta el prefab: ejecuta Fragmentum → Regenerar todo.");
            Assert.AreEqual(LayerMask.NameToLayer(Capas.Suelo), prefab.layer, "Se pisa.");
            Assert.AreEqual(RigidbodyType2D.Kinematic, prefab.GetComponent<Rigidbody2D>().bodyType);
            Assert.IsNotNull(prefab.GetComponent<PlataformaFlotante>());

            var cuerpo = prefab.GetComponent<BoxCollider2D>();
            Assert.IsFalse(cuerpo.isTrigger);
            Assert.AreEqual(DisenoNivel2.AnchoPlaca, cuerpo.size.x, 1e-4f, "El ancho del diseño.");
            Assert.AreEqual(0f, cuerpo.offset.y + cuerpo.size.y * 0.5f, 1e-4f, "El pivote en la superficie.");

            var hielo = prefab.GetComponentInChildren<ZonaModificadorMovimiento>(true);
            Assert.IsNotNull(hielo, "La placa resbala.");
            Assert.AreNotSame(prefab, hielo.gameObject, "En un hijo, que se mueve con ella.");
            Assert.IsTrue(hielo.GetComponent<Collider2D>().isTrigger);
            Assert.Less(hielo.Modificador.aceleracion, 0.5f);
            Assert.IsFalse(hielo.Modificador.esAgua);
        }

        [Test]
        public void ElPrefabDelTapon_SePisa_YLaEspadaLoNota()
        {
            var prefab = GeneradorMecanicas.Cargar(GeneradorMecanicas.HieloRompible);
            Assert.IsNotNull(prefab, "Falta el prefab: ejecuta Fragmentum → Regenerar todo.");
            Assert.AreEqual(LayerMask.NameToLayer(Capas.Suelo), prefab.layer);
            var cuerpo = prefab.GetComponent<BoxCollider2D>();
            Assert.AreEqual(GeneradorMecanicas.TamanoTapon, cuerpo.size);

            var hielo = prefab.GetComponent<HieloRompible>();
            Assert.IsNotNull(hielo);
            var zona = new SerializedObject(hielo).FindProperty("zonaDeGolpe").objectReferenceValue as BoxCollider2D;
            Assert.IsNotNull(zona, "La zona de golpe está asignada.");
            Assert.AreEqual(LayerMask.NameToLayer(Capas.Enemy), zona.gameObject.layer, "La espada solo golpea la capa Enemy.");
            Assert.AreEqual(cuerpo.size, zona.size);
            Assert.AreEqual(cuerpo.offset, zona.offset);
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer(Capas.Enemy), LayerMask.NameToLayer(Capas.Player)),
                "Esa capa no choca con Kael: la zona de golpe no le estorba.");
            Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer(Capas.Enemy), LayerMask.NameToLayer(Capas.PlayerWeapon)),
                "Y la espada sí la toca.");
        }

        [Test]
        public void ElPrefabDeKael_LlevaSuAire()
        {
            var prefab = ConstructorJugador.PrefabJugador();
            Assert.IsNotNull(prefab);
            var oxigeno = prefab.GetComponent<OxigenoJugador>();
            Assert.IsNotNull(oxigeno, "Kael respira bajo el agua con OxigenoJugador.");
            var datos = new SerializedObject(oxigeno);
            Assert.AreEqual(10f, datos.FindProperty("capacidad").floatValue, "10 s de aire: eran 15 y al probar el nivel sobraba (2026-10-12).");
            Assert.AreEqual(10f, datos.FindProperty("recargaPorSegundo").floatValue, "Y se llena en 1 s al respirar.");
            Assert.AreEqual(2f, datos.FindProperty("intervaloAsfixia").floatValue, "Y después, un corazón cada 2 s.");
        }
    }
}
