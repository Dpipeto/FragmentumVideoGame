using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Levels;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Narrative;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Las piezas del mundo de la Culpa (fase B), sin jugar: el barril solo se enciende con el Ataque
    /// Cargado, la mina sube y baja como dice el diseño, las consecuencias agravan sin curar, la memoria
    /// se recuerda una vez, el indicador de la Invisibilidad se rellena mientras espera, y el guion y
    /// los prefabs tienen lo que el diseño pide. Lo que pasa jugando, en <c>MecanicasCulpaPlayModeTests</c>.
    /// </summary>
    public class MecanicasCulpaTests
    {
        [Test]
        public void ElBarril_SoloLoEnciendeElAtaqueCargado_YGolpeadoSinCargarSoloSuena()
        {
            var objeto = new GameObject("Barril");
            try
            {
                var barril = objeto.AddComponent<BarrilExplosivo>();
                var suena = 0;
                barril.OnSuena += () => suena++;
                Assert.AreEqual(ESTADOS_BARRIL.INTACT, barril.Estado);

                Assert.IsFalse(barril.RecibirGolpe(new Golpe(1, Vector2.zero)), "Un golpe normal no lo enciende…");
                Assert.AreEqual(1, suena, "…solo suena.");
                Assert.AreEqual(ESTADOS_BARRIL.INTACT, barril.Estado);

                Assert.IsTrue(barril.RecibirGolpe(new Golpe(2, Vector2.zero, null, cargado: true)), "El Ataque Cargado, sí.");
                Assert.AreEqual(ESTADOS_BARRIL.FUSE, barril.Estado, "Se le enciende la mecha.");
                Assert.IsFalse(barril.EstaVivo, "Ya no se le puede volver a encender.");
                Assert.IsFalse(barril.Encender());

                barril.Reponer();
                Assert.AreEqual(ESTADOS_BARRIL.INTACT, barril.Estado, "Al reaparecer Kael, vuelve a estar intacto.");
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void LaMina_EstaAbajoUnTercio_SubeArribaOtroTercio_YBaja()
        {
            const float periodo = DisenoNivel4.PeriodoMina;
            const float recorrido = DisenoNivel4.RecorridoMina;
            Assert.AreEqual(0f, Mina.Altura(0f, periodo, recorrido), 1e-4f, "Empieza abajo.");
            Assert.AreEqual(0f, Mina.Altura(periodo * 0.3f, periodo, recorrido), 1e-4f, "Abajo un tercio del ciclo.");
            Assert.That(Mina.Altura(periodo * 0.42f, periodo, recorrido), Is.InRange(0.01f, recorrido - 0.01f), "Sube.");
            Assert.AreEqual(recorrido, Mina.Altura(periodo * 0.5f, periodo, recorrido), 1e-4f, "Arriba.");
            Assert.AreEqual(recorrido, Mina.Altura(periodo * 0.8f, periodo, recorrido), 1e-4f, "Arriba otro tercio.");
            Assert.That(Mina.Altura(periodo * 0.9f, periodo, recorrido), Is.InRange(0.01f, recorrido - 0.01f), "Baja.");
            Assert.AreEqual(0f, Mina.Altura(periodo, periodo, recorrido), 1e-4f, "Y vuelve a empezar.");

            var anterior = -1f;
            for (var t = periodo / 3f; t <= periodo * 0.5f; t += 0.01f)
            {
                var altura = Mina.Altura(t, periodo, recorrido);
                Assert.GreaterOrEqual(altura, anterior - 1e-5f, "Mientras sube, no baja.");
                anterior = altura;
            }

            // Lo que dura arriba da para pasar por debajo nadando (de un lado de la bola al otro, con el cuerpo).
            var nadando = new Fragmentum.Player.ParametrosJugador().velocidadMaxima * Fragmentum.Player.ModificadorMovimiento.BajoElAgua.velocidad;
            var pasar = (DisenoNivel4.RadioBolaMina * 2f + DisenoNivel4.AnchoKael) / nadando;
            Assert.Less(pasar, periodo / 3f, "Arriba, da tiempo a pasar por debajo nadando.");
        }

        [Test]
        public void LasConsecuencias_AgravanUnVeintePorCiento_SinTocarLosDatosOriginales()
        {
            var datos = ScriptableObject.CreateInstance<EnemyData>();
            try
            {
                datos.velocidadPatrulla = 1f;
                datos.velocidadPersecucion = 2.2f;
                datos.esperaEnWaypoint = 1.5f;
                datos.attackCooldown = 0.45f;
                datos.duracionAtaque = 0.5f;
                datos.inicioVentanaGolpe = 0.3f;
                datos.finVentanaGolpe = 0.4f;
                datos.vidaMaxima = 3;

                var agravados = SistemaConsecuencias.Agravados(datos, DisenoNivel4.AgresividadPorMemoria);
                try
                {
                    Assert.AreNotSame(datos, agravados, "Una copia: los datos del asset no cambian.");
                    Assert.AreEqual(1.2f, agravados.velocidadPatrulla, 1e-4f);
                    Assert.AreEqual(2.64f, agravados.velocidadPersecucion, 1e-4f, "Persigue un 20 % más deprisa.");
                    Assert.AreEqual(0.45f / 1.2f, agravados.attackCooldown, 1e-4f, "Y ataca un 20 % más deprisa.");
                    Assert.AreEqual(0.5f / 1.2f, agravados.duracionAtaque, 1e-4f);
                    Assert.AreEqual(0.3f / 1.2f, agravados.inicioVentanaGolpe, 1e-4f);
                    Assert.AreEqual(0.4f / 1.2f, agravados.finVentanaGolpe, 1e-4f);
                    Assert.AreEqual(3, agravados.vidaMaxima, "La vida no cambia.");
                    Assert.AreEqual(2.2f, datos.velocidadPersecucion, 1e-4f, "El original sigue igual.");
                }
                finally
                {
                    Object.DestroyImmediate(agravados);
                }
            }
            finally
            {
                Object.DestroyImmediate(datos);
            }
        }

        [Test]
        public void LaMemoria_SeRecuerdaUnaVez_YLoAnuncia()
        {
            var objeto = new GameObject("Memoria");
            try
            {
                var memoria = objeto.AddComponent<MemoriaInteractuable>();
                MemoriaInteractuable recordada = null;
                void AlRecordar(MemoriaInteractuable m) => recordada = m;
                MemoriaInteractuable.AlRecordar += AlRecordar;
                try
                {
                    Assert.AreEqual("Recordar", memoria.Accion);
                    Assert.IsTrue(memoria.Usable, "Sin Kael invisible, se puede recordar.");
                    Assert.IsTrue(memoria.Recordar());
                    Assert.AreSame(memoria, recordada, "Lo anuncia: el sistema de consecuencias lo escucha.");
                    Assert.IsTrue(memoria.Recordada);
                    Assert.IsFalse(memoria.Recordar(), "Una vez por partida del nivel.");
                    Assert.IsFalse(memoria.Disponible, "Recordada, ya no se ofrece.");
                }
                finally
                {
                    MemoriaInteractuable.AlRecordar -= AlRecordar;
                }
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }

            Assert.AreEqual("Para recordar, déjate ver", MemoriaInteractuable.AvisoInvisible, "Decisión 12: invisible no se recuerda.");
        }

        [Test]
        public void ElIndicadorDeLaInvisibilidad_SeVaciaAlUsarla_YSeRellenaMientrasEspera()
        {
            const float recarga = 8f;
            Assert.AreEqual(1f, IndicadorInvisibilidad.Relleno(true, 11f, recarga), 1e-4f, "Mientras dura, lleno.");
            Assert.AreEqual(0f, IndicadorInvisibilidad.Relleno(false, recarga, recarga), 1e-4f, "Al acabarse, vacío.");
            Assert.AreEqual(0.5f, IndicadorInvisibilidad.Relleno(false, 4f, recarga), 1e-4f, "A media espera, medio.");
            Assert.AreEqual(1f, IndicadorInvisibilidad.Relleno(false, 0f, recarga), 1e-4f, "Lista, lleno.");
        }

        [Test]
        public void ElGuion_TieneLasDosPistas_LaLineaDelRio_YLasCuatroMemorias()
        {
            var culpa = GuionMundos.De(EMOCIONES.CULPA);
            foreach (var linea in new[] { culpa.Pista, culpa.PistaInvisibilidad, culpa.Orilla })
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(linea));
                Assert.LessOrEqual(linea.Length, 125, $"Las líneas del HUD duran 4,5 s: «{linea}».");
            }

            StringAssert.Contains("Ataque Cargado", culpa.Pista, "La del barril nombra el Ataque Cargado.");
            StringAssert.Contains("Invisibilidad", culpa.PistaInvisibilidad, "La de la tierra de nadie, la Invisibilidad.");
            CollectionAssert.AreEqual(DisenoNivel4.Memorias.Select(m => m.Nombre).ToList(), culpa.Memorias.Select(m => m.id).ToList(),
                "Una línea por memoria, en el orden del diseño.");

            // En los assets: los diálogos de la Culpa y una cinemática por memoria.
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            var dialogos = bd.niveles[3].dialogos;
            Assert.IsNotNull(dialogos);
            Assert.AreEqual(culpa.Pista, dialogos.Del(MOMENTOS_DIALOGO.PISTA).Single().texto);
            Assert.AreEqual(culpa.PistaInvisibilidad, dialogos.Del(MOMENTOS_DIALOGO.PISTA_INVISIBILIDAD).Single().texto);
            Assert.AreEqual(culpa.Orilla, dialogos.Del(MOMENTOS_DIALOGO.ORILLA).Single().texto);
            foreach (var (id, texto) in culpa.Memorias)
            {
                var cinematica = GeneradorNarrativa.CargarCinematicaMemoria(id);
                Assert.IsNotNull(cinematica, $"Falta la cinemática de la memoria {id}.");
                Assert.IsTrue(cinematica.saltable);
                Assert.IsFalse(cinematica.soloPrimeraVez, "Se ve cada vez que se recuerda.");
                Assert.AreEqual(GeneradorNarrativa.DuracionMemoria, cinematica.DuracionTotal, 1e-4f, "Unos 5 s.");
                var imagen = ConstructorCinematicasNivel4.Imagen(id);
                if (imagen == null)
                {
                    // Sin su imagen: la línea de Kael sobre el juego congelado.
                    Assert.IsTrue(cinematica.sobreElJuego, "Sobre el juego congelado.");
                    Assert.AreEqual(texto, cinematica.diapositivas.Single().texto);
                    continue;
                }

                // Con su imagen (fase F): la imagen, con la línea de Kael encima, en dos líneas.
                var plano = cinematica.diapositivas.SingleOrDefault(d => d.imagen != null);
                Assert.IsNotNull(plano, $"{id}: con su imagen.");
                Assert.AreSame(imagen, plano.imagen, $"{id}: la suya.");
                Assert.IsTrue(cinematica.escalaEntera);
                Assert.AreEqual(ConstructorCinematicasNivel4.Rejilla, cinematica.Rejilla, "En la rejilla de su imagen.");
                Assert.AreEqual(ConstructorCinematicasNivel4.EnDosLineas(texto), plano.carteles.Single().texto, $"{id}: la línea de Kael.");
                Assert.AreEqual(texto, plano.carteles.Single().texto.Replace("\n", " "), "Sin perder ni una letra.");
            }
        }

        [Test]
        public void ElBarrilYLaMina_TienenLasMedidasDelDiseno_YLaEspadaLosGolpea()
        {
            var capaEnemigos = LayerMask.NameToLayer(Capas.Enemy);
            var prefabBarril = GeneradorMecanicas.Cargar(GeneradorMecanicas.BarrilExplosivo);
            Assert.IsNotNull(prefabBarril, "Falta el prefab del barril: ejecuta Fragmentum → Regenerar todo.");
            var barril = prefabBarril.GetComponent<BarrilExplosivo>();
            Assert.AreEqual(DisenoNivel4.MechaBarril, barril.mecha, 1e-4f);
            Assert.AreEqual(DisenoNivel4.RadioBarril, barril.radio, 1e-4f);
            Assert.AreEqual(DisenoNivel4.DanioBarrilEnemigos, barril.danioEnemigos);
            Assert.AreEqual(DisenoNivel4.DanioBarrilKael, barril.danioJugador);
            Assert.AreEqual(DisenoNivel4.CadenaBarril, barril.cadena, 1e-4f);
            Assert.AreEqual(capaEnemigos, prefabBarril.layer, "En la capa de los enemigos: la espada lo golpea.");
            Assert.IsFalse(prefabBarril.GetComponent<BoxCollider2D>().isTrigger, "Un bloque que Kael y los enemigos atraviesan (la matriz no los junta).");
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(capaEnemigos, LayerMask.NameToLayer(Capas.Player)), "Kael no choca con él.");
            Assert.IsNotNull(barril.Visual);

            var prefabMina = GeneradorMecanicas.Cargar(GeneradorMecanicas.Mina);
            Assert.IsNotNull(prefabMina, "Falta el prefab de la mina.");
            var mina = prefabMina.GetComponent<Mina>();
            Assert.AreEqual(DisenoNivel4.RecorridoMina, mina.recorrido, 1e-4f);
            Assert.AreEqual(DisenoNivel4.PeriodoMina, mina.periodo, 1e-4f);
            Assert.AreEqual(DisenoNivel4.RadioMina, mina.radio, 1e-4f);
            Assert.AreEqual(DisenoNivel4.DanioMinaKael, mina.danioJugador);
            Assert.IsNotNull(mina.Bola);
            Assert.AreEqual(capaEnemigos, mina.Bola.gameObject.layer, "La bola, en la capa de los enemigos: la espada la golpea.");
            Assert.AreEqual(DisenoNivel4.RadioBolaMina, mina.Bola.GetComponent<CircleCollider2D>().radius, 1e-4f);
            Assert.AreEqual(RigidbodyType2D.Kinematic, prefabMina.GetComponent<Rigidbody2D>().bodyType, "Se mueve sola, sin caer.");
        }
    }
}
