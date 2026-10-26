using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Enemies;
using Fragmentum.Levels;
using Fragmentum.Levels.Escenario;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Narrative;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La escena del Nivel 4 está montada con el recorrido de <see cref="DisenoNivel4"/>: bloques,
    /// tejados, el balcón del campanario, la torre y los restos del puente que no agarran, las
    /// coberturas, el agua del río con sus alambradas, los enemigos con su ruta, los checkpoints, la
    /// salida y la plaza del Juez Silente (fase A); los barriles, la mina, las memorias con su
    /// cinemática y su zona, el sistema de consecuencias, las pistas y la línea del río (fase B); los
    /// Artilleros y el Verdugo (fase C); la escena vestida con su arte: el tileset, las coberturas, el
    /// campanario, el río, las alambradas y el atrezo (fase E, segunda tanda); en la plaza, las cuatro marcas de las memorias y la pared en la
    /// que el Juez las proyecta (fase D); y las memorias con su dibujo, cada una sobre su losa en la plaza, la pared con sus
    /// imágenes y el fondo del mundo (lo último de la fase E). Y la herramienta que la monta no cambia nada al prepararla otra vez.
    /// </summary>
    public class EscenaNivel4Tests
    {
        private Scene _nivel4;
        private LevelData _datos;

        [OneTimeSetUp]
        public void AbrirNivel4()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            Assert.IsNotNull(bd, "Falta la LevelDatabase: ejecuta Fragmentum → Regenerar todo.");
            _datos = bd.niveles[3];
            Assert.IsTrue(_datos.escena.Asignada, "El Nivel 4 tiene escena.");
            _nivel4 = EditorSceneManager.OpenScene(_datos.escena.Ruta, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void CerrarNivel4()
        {
            if (_nivel4.IsValid() && _nivel4.isLoaded)
            {
                EditorSceneManager.CloseScene(_nivel4, true);
            }
        }

        [Test]
        public void ElNivel4_EsJugable_YTieneLosCheckpointsYElTiempoDelDiseno()
        {
            Assert.AreEqual(GeneradorProgresion.RutaEscenaNivel4, _datos.escena.Ruta);
            Assert.IsTrue(_datos.Jugable, "Está en Build Settings.");
            CollectionAssert.AreEqual(DisenoNivel4.Checkpoints.Select(c => c.x).ToList(), _datos.puntosDeControl);
            Assert.AreEqual(960f, _datos.tiempoConBono, 0.01f, "Decisión 1: 16 minutos con bono.");
            Assert.AreEqual(DisenoNivel4.ColorVeloAgua, _datos.reglas.colorVeloAgua, "Bajo el agua del río, el velo es pardo.");
            Assert.IsTrue(_datos.reglas.agacharse, "En la Culpa se agacha uno tras la cobertura.");
            Assert.IsFalse(_datos.reglas.sigilo, "Correr no hace ruido: aquí no se esconde nada a oscuras.");
            Assert.IsFalse(_datos.reglas.oscuridad, "Con luz de día.");
        }

        [Test]
        public void ElNivel4_TieneLevelManager_ConInicio_YLaCamaraBajaHastaElRio()
        {
            var gestores = Todos<LevelManager>(_nivel4);
            Assert.AreEqual(1, gestores.Count);
            var serializado = new SerializedObject(gestores[0]);
            Assert.IsNotNull(serializado.FindProperty("prefabJugador").objectReferenceValue);
            var inicio = serializado.FindProperty("puntoInicio").objectReferenceValue as Transform;
            Assert.IsNotNull(inicio);
            Assert.AreEqual(new Vector2(DisenoNivel4.Inicio, 0.05f), (Vector2)inicio.position);
            Assert.AreEqual(DisenoNivel4.AlturaCamara, serializado.FindProperty("alturaCamara").vector2Value);

            var camaras = Todos<Camera>(_nivel4);
            Assert.AreEqual(1, camaras.Count);
            Assert.IsNotNull(camaras[0].GetComponent<CameraMovement>());
            Assert.AreEqual(DisenoNivel4.TamanoCamara, camaras[0].orthographicSize, 1e-4f, "El encuadre de los otros niveles.");
        }

        [Test]
        public void ElNivel4_TieneElRecorridoDeLaVersionActual_ConSusBloquesTejadosYCoberturas()
        {
            var recorrido = PreparadorNivel.RaizRecorrido(_nivel4);
            Assert.IsNotNull(recorrido);
            Assert.IsNotNull(recorrido.Find(PreparadorNivel.MarcaVersionNivel4), "Montado con la versión actual del diseño.");
            Assert.IsFalse(_nivel4.GetRootGameObjects().Any(r => r.name == PreparadorNivel.NombreProvisional));

            var cajas = recorrido.GetComponentsInChildren<BoxCollider2D>(true).Where(c => !c.isTrigger).ToList();
            var capaSuelo = LayerMask.NameToLayer(Capas.Suelo);
            var pisables = DisenoNivel4.Suelos.Concat(DisenoNivel4.Tejados).Append(DisenoNivel4.Balcon).Concat(DisenoNivel4.Coberturas).ToList();
            foreach (var esperado in pisables.Concat(DisenoNivel4.Paredes))
            {
                var caja = cajas.SingleOrDefault(b => b.name == esperado.Nombre);
                Assert.IsNotNull(caja, $"Falta el bloque {esperado.Nombre}.");
                var rect = Rectangulo(caja);
                Assert.AreEqual(esperado.Desde, rect.xMin, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Hasta, rect.xMax, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Abajo, rect.yMin, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Arriba, rect.yMax, 0.01f, esperado.Nombre);
            }

            Assert.IsTrue(pisables.All(s => cajas.Single(c => c.name == s.Nombre).gameObject.layer == capaSuelo),
                "Todo lo que se pisa está en la capa del suelo: también las coberturas, contra las que se paran las balas.");
            Assert.IsTrue(DisenoNivel4.Paredes.All(s => cajas.Single(c => c.name == s.Nombre).gameObject.layer == LayerMask.NameToLayer(Capas.Wall)));
        }

        [Test]
        public void LaTorreDelCampanario_YLosRestosDelPuente_NoAgarran_YSonLosUnicos()
        {
            var sinAgarre = Todos<SinAgarre>(_nivel4);
            var esperadas = DisenoNivel4.Paredes.Where(p => DisenoNivel4.NoAgarra(p.Nombre)).Select(p => p.Nombre).ToList();
            CollectionAssert.AreEquivalent(new[] { "TorreDelCampanario", "RestosDelPuente1", "RestosDelPuente2" }, esperadas);
            CollectionAssert.AreEquivalent(esperadas, sinAgarre.Select(s => s.name).ToList(), "Solo ellos: las demás paredes se escalan.");
            foreach (var pared in sinAgarre)
            {
                var caja = pared.GetComponent<BoxCollider2D>();
                Assert.IsFalse(caja.isTrigger, $"{pared.name} sigue siendo una pared: se choca con ella.");
                Assert.AreEqual(LayerMask.NameToLayer(Capas.Wall), caja.gameObject.layer);
            }
        }

        [Test]
        public void ElRio_TieneSuAgua_YSusAlambradas_QueSujetanComoLasMaranas()
        {
            var aguas = Todos<ZonaModificadorMovimiento>(_nivel4).Where(z => z.Modificador.esAgua).ToList();
            Assert.AreEqual(DisenoNivel4.Aguas.Length, aguas.Count, "Solo el río tiene agua.");
            foreach (var zona in DisenoNivel4.Aguas)
            {
                var agua = aguas.SingleOrDefault(a => a.name == zona.Nombre);
                Assert.IsNotNull(agua, $"Falta el agua {zona.Nombre}.");
                AssertRect(zona.Rect, Rectangulo(agua.GetComponent<BoxCollider2D>()), zona.Nombre);
                Assert.IsTrue(agua.GetComponent<BoxCollider2D>().isTrigger);
            }

            var alambradas = Todos<Marana>(_nivel4);
            Assert.AreEqual(DisenoNivel4.Alambradas.Length, alambradas.Count);
            foreach (var zona in DisenoNivel4.Alambradas)
            {
                var alambrada = alambradas.SingleOrDefault(a => a.name == zona.Nombre);
                Assert.IsNotNull(alambrada, $"Falta la alambrada {zona.Nombre}.");
                Assert.AreEqual(Vector3.one, alambrada.transform.localScale, "Sin escala: su dibujo es hijo.");
                var caja = alambrada.GetComponent<BoxCollider2D>();
                Assert.IsTrue(caja.isTrigger);
                AssertRect(zona.Rect, Rectangulo(caja), zona.Nombre);
                var tipo = alambrada.GetComponent<ZonaModificadorMovimiento>();
                Assert.AreEqual((int)ZonaModificadorMovimiento.TIPOS.MARANA, new SerializedObject(tipo).FindProperty("tipo").enumValueIndex,
                    "Sujeta como la maraña de la ciénaga.");
                Assert.AreEqual(ModificadorMovimiento.Marana.velocidad, tipo.Modificador.velocidad, 1e-4f);
            }
        }

        [Test]
        public void ElNivel4_EstaVestido_ConSuTileset_SusCoberturas_ElCampanario_YSuAtrezo()
        {
            Assert.IsTrue(ConstructorEscenarioMundo4.Completo, "El tileset y los props del Mundo 4, cortados.");
            var recorrido = PreparadorNivel.RaizRecorrido(_nivel4);
            var escenario = recorrido.Find(PreparadorNivel.NombreEscenario);
            Assert.IsNotNull(escenario, "La escena lleva su escenario dibujado.");

            // Cada bloque apaga su rectángulo de color: lo dibuja su arte.
            var cajas = recorrido.GetComponentsInChildren<BoxCollider2D>(true).Where(c => !c.isTrigger).ToList();
            var bloques = DisenoNivel4.Suelos.Concat(DisenoNivel4.Paredes).Concat(DisenoNivel4.Tejados).Append(DisenoNivel4.Balcon)
                .Concat(DisenoNivel4.Coberturas);
            foreach (var bloque in bloques)
            {
                var liso = cajas.Single(c => c.name == bloque.Nombre).GetComponent<SpriteRenderer>();
                Assert.IsFalse(liso != null && liso.enabled, $"{bloque.Nombre}: con su dibujo, su rectángulo se apaga.");
                if (bloque.Nombre != DisenoNivel4.Balcon.Nombre)
                {
                    Assert.IsNotNull(escenario.Find(PreparadorNivel.NombreDibujo(bloque.Nombre)), $"{bloque.Nombre}: tiene su dibujo.");
                }
            }

            // Las coberturas y el tanque, con su pie en el de su bloque y por delante de Kael.
            foreach (var bloque in DisenoNivel4.Coberturas.Append(DisenoNivel4.Suelos.Single(s => s.Nombre == "Tanque")))
            {
                var dibujo = escenario.Find(PreparadorNivel.NombreDibujo(bloque.Nombre)).GetComponent<SpriteRenderer>();
                Assert.AreSame(PreparadorNivel.CoberturaDe(bloque.Nombre).Sprite, dibujo.sprite, bloque.Nombre);
                Assert.AreEqual(new Vector2((bloque.Desde + bloque.Hasta) * 0.5f, bloque.Abajo), (Vector2)dibujo.transform.position, bloque.Nombre);
                Assert.AreEqual(ConstructorEscenarioMundo4.OrdenCoberturas, dibujo.sortingOrder, bloque.Nombre);
            }

            // El campanario, con el pie de su torre en el de la del diseño.
            var torre = DisenoNivel4.Paredes.Single(p => p.Nombre == "TorreDelCampanario");
            var campanario = escenario.Find(PreparadorNivel.NombreDibujo(torre.Nombre)).GetComponent<SpriteRenderer>();
            Assert.AreSame(ConstructorMecanicasMundo4.Campanario, campanario.sprite);
            Assert.AreEqual(new Vector2(torre.Desde, DisenoNivel4.PisadaDe("BajoElCampanario").Y), (Vector2)campanario.transform.position);

            // El atrezo, en su sitio y como lo pide el diseño.
            foreach (var decorado in DisenoNivel4.Decorados)
            {
                var dibujo = escenario.Find(decorado.Nombre)?.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(dibujo, $"Falta el atrezo {decorado.Nombre}.");
                Assert.AreSame(ConstructorEscenarioMundo4.Cargar(decorado.Pieza), dibujo.sprite, decorado.Nombre);
                Assert.AreEqual(decorado.Posicion, (Vector2)dibujo.transform.position, decorado.Nombre);
                Assert.AreEqual(decorado.Volteado, dibujo.flipX, decorado.Nombre);
            }
        }

        [Test]
        public void ElRio_TieneSuSuperficieDibujada_YLasAlambradas_SuDibujoDeReposoYDeAtrapar()
        {
            var superficie = ConstructorMecanicasMundo4.SuperficieDelAgua;
            Assert.IsNotNull(superficie, "Está el agua del río.");
            var rio = PreparadorNivel.RaizRecorrido(_nivel4).Find("Rio");
            foreach (var zona in DisenoNivel4.Aguas)
            {
                var dibujo = rio.Find(PreparadorNivel.NombreSuperficie(zona));
                Assert.IsNotNull(dibujo, $"{zona.Nombre}: con su superficie.");
                Assert.AreSame(superficie[0], dibujo.GetComponent<SpriteRenderer>().sprite);
                Assert.IsNotNull(dibujo.GetComponent<BucleDeSprites>(), $"{zona.Nombre}: el agua se mueve.");
            }

            var reposo = ConstructorMecanicasMundo4.AlambradaEnReposo;
            var atrapa = ConstructorMecanicasMundo4.AlambradaQueAtrapa;
            Assert.IsNotNull(reposo);
            Assert.IsNotNull(atrapa);
            foreach (var alambrada in Todos<Marana>(_nivel4))
            {
                var zona = DisenoNivel4.Alambradas.Single(z => z.Nombre == alambrada.name);
                var dibujo = alambrada.GetComponentInChildren<SpriteRenderer>(true);
                Assert.AreSame(reposo[0], dibujo.sprite, alambrada.name);
                Assert.AreEqual(SpriteDrawMode.Tiled, dibujo.drawMode, alambrada.name);
                Assert.AreEqual(new Vector2(zona.Hasta - zona.Desde, zona.Arriba - zona.Abajo), dibujo.size, $"{alambrada.name}: llena su ojo.");
                Assert.AreEqual(zona.Arriba, dibujo.transform.position.y, 1e-4f, $"{alambrada.name}: cuelga del techo.");
                var serializado = new SerializedObject(alambrada);
                Assert.AreEqual(atrapa.Length, serializado.FindProperty("atrapa").arraySize, $"{alambrada.name}: se cierra al atrapar.");
            }
        }

        private static void AssertRect(Rect esperado, Rect real, string nombre)
        {
            Assert.AreEqual(esperado.xMin, real.xMin, 0.01f, nombre);
            Assert.AreEqual(esperado.xMax, real.xMax, 0.01f, nombre);
            Assert.AreEqual(esperado.yMin, real.yMin, 0.01f, nombre);
            Assert.AreEqual(esperado.yMax, real.yMax, 0.01f, nombre);
        }

        [Test]
        public void LosEnemigos_EstanConSuRuta_LosPenitentesALaEscalaDeLosEcos_YLosArtillerosSeTurnan()
        {
            var enemigos = Todos<EnemyController>(_nivel4);
            Assert.AreEqual(DisenoNivel4.Enemigos.Length, enemigos.Count, "Los Penitentes, los Artilleros y el Verdugo.");
            foreach (var esperado in DisenoNivel4.Enemigos)
            {
                var enemigo = enemigos.SingleOrDefault(e => e.name == esperado.Nombre);
                Assert.IsNotNull(enemigo, $"Falta {esperado.Nombre}.");
                var variante = esperado.Tipo == DisenoNivel4.TIPOS_ENEMIGO.PENITENTE ? "Penitente"
                    : esperado.Tipo == DisenoNivel4.TIPOS_ENEMIGO.ARTILLERO ? "Artillero"
                    : "Verdugo";
                Assert.AreEqual(RutaPrefab(variante), PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(enemigo.gameObject), esperado.Nombre);
                Assert.AreEqual(new Vector2(esperado.X, esperado.Y + 0.05f), (Vector2)enemigo.transform.position, esperado.Nombre);
                if (esperado.Tipo == DisenoNivel4.TIPOS_ENEMIGO.PENITENTE)
                {
                    Assert.AreEqual(new Vector3(DisenoNivel4.EscalaPenitente.x, DisenoNivel4.EscalaPenitente.y, 1f), enemigo.transform.localScale,
                        $"{esperado.Nombre}: el Eco de la Culpa, a la escala de los Ecos.");
                }

                if (esperado.Tipo == DisenoNivel4.TIPOS_ENEMIGO.ARTILLERO)
                {
                    var artillero = enemigo.GetComponent<Artillero>();
                    Assert.AreEqual(esperado.Turno, artillero.turno, 1e-4f, $"{esperado.Nombre}: su turno.");
                    Assert.AreEqual(esperado.Canon.x, artillero.Canon.x, 1e-3f, $"{esperado.Nombre}: su cañón, por delante de sus sacos.");
                    Assert.AreEqual(esperado.Canon.y + 0.05f, artillero.Canon.y, 1e-3f, $"{esperado.Nombre}: y a su altura (de pie, 0,05 u sobre el suelo).");
                }

                if (esperado.Tipo == DisenoNivel4.TIPOS_ENEMIGO.VERDUGO)
                {
                    Assert.AreEqual(DisenoNivel4.LimitesPatio, enemigo.GetComponent<Verdugo>().limites, "Busca a Kael dentro del patio.");
                }

                var ruta = new SerializedObject(enemigo).FindProperty("waypoints");
                Assert.AreEqual(2, ruta.arraySize, esperado.Nombre);
                Assert.AreEqual(esperado.Desde, (ruta.GetArrayElementAtIndex(0).objectReferenceValue as Transform).position.x, 0.01f);
                Assert.AreEqual(esperado.Hasta, (ruta.GetArrayElementAtIndex(1).objectReferenceValue as Transform).position.x, 0.01f);
            }
        }

        [Test]
        public void ElNivel4_TieneLosCheckpoints_ConElEcoDeMemoriaEnElRefugio_LaSalida_YLaZonaMortal()
        {
            var checkpoints = Todos<Checkpoint>(_nivel4).OrderBy(c => c.transform.position.x).ToList();
            Assert.AreEqual(DisenoNivel4.Checkpoints.Length, checkpoints.Count);
            for (var i = 0; i < checkpoints.Count; i++)
            {
                Assert.AreEqual($"L4_CP{i + 1}", checkpoints[i].Id);
                Assert.AreEqual(DisenoNivel4.Checkpoints[i] + new Vector2(0f, 0.05f), (Vector2)checkpoints[i].transform.position, "De pie en su suelo.");
                Assert.IsTrue(checkpoints[i].GetComponent<Collider2D>().isTrigger);
                Assert.IsTrue(checkpoints[i].Cura, "Todos los checkpoints curan.");
            }

            var conEco = checkpoints.Where(c => c.EcoDeMemoria).ToList();
            Assert.AreEqual(1, conEco.Count);
            Assert.AreEqual("L4_CP1", conEco[0].Id, "En el refugio.");

            var salidas = Todos<SalidaNivel>(_nivel4);
            Assert.AreEqual(1, salidas.Count);
            Assert.AreEqual(new Vector2(DisenoNivel4.Salida, DisenoNivel4.SueloArena), (Vector2)salidas[0].transform.position);

            var zonas = Todos<DeadZone>(_nivel4);
            Assert.AreEqual(1, zonas.Count);
            Assert.Less(Rectangulo((BoxCollider2D)zonas[0].GetComponent<Collider2D>()).yMax, DisenoNivel4.FondoRio - 2f,
                "La zona mortal queda muy por debajo del río.");
        }

        [Test]
        public void ElNivel4_TieneLaPlazaConElJuezSilente()
        {
            var arenas = Todos<ArenaJefe>(_nivel4);
            Assert.AreEqual(1, arenas.Count);
            var arena = arenas[0];

            Assert.IsInstanceOf<JuezSilente>(arena.Jefe, "El jefe de la Culpa es el Juez Silente.");
            Assert.AreEqual(DisenoNivel4.LimitesArena, arena.Limites);
            Assert.AreEqual(DisenoNivel4.Jefe, arena.Jefe.transform.position.x, 0.01f);
            Assert.AreEqual(DisenoNivel4.Entrada, arena.transform.position.x, 0.01f);
            Assert.IsTrue(arena.GetComponent<Collider2D>().isTrigger);

            var puertas = new SerializedObject(arena).FindProperty("puertas");
            Assert.AreEqual(1, puertas.arraySize);
            var puerta = puertas.GetArrayElementAtIndex(0).objectReferenceValue as GameObject;
            Assert.IsNotNull(puerta);
            Assert.IsFalse(puerta.activeSelf, "Abierta hasta que empieza el combate.");
            Assert.AreEqual(0, arena.Plataformas.Count, "La plaza es llana.");
        }

        [Test]
        public void LasSeccionesDelNivel_SonLasDelDiseno_YLaCamaraLlegaAlFondoDeLaArena()
        {
            Assert.AreEqual(DisenoNivel4.Secciones.Length, _datos.secciones.Count);
            for (var i = 0; i < _datos.secciones.Count; i++)
            {
                Assert.AreEqual(DisenoNivel4.Secciones[i].x, _datos.secciones[i].desdeX, 1e-4f, _datos.secciones[i].nombre);
                Assert.AreEqual(DisenoNivel4.Secciones[i].y, _datos.secciones[i].hastaX, 1e-4f, _datos.secciones[i].nombre);
                Assert.AreEqual(DisenoNivel4.TextosDeLasSecciones[i].nombre, _datos.secciones[i].nombre);
            }

            var margen = new SerializedObject(Todos<LevelManager>(_nivel4).Single()).FindProperty("margenCamara").floatValue;
            Assert.AreEqual(DisenoNivel4.MargenCamara, margen, 1e-4f);
            var fondo = DisenoNivel4.Paredes.Single(p => p.Nombre == "ParedArena");
            Assert.AreEqual(fondo.Hasta, _datos.Extension.y + margen, 1e-4f, "La cámara llega hasta la pared del fondo de la arena.");
        }

        [Test]
        public void LosBarriles_YLaMina_EstanDondeDiceElDiseno()
        {
            var barriles = Todos<BarrilExplosivo>(_nivel4).OrderBy(b => b.transform.position.x).ToList();
            Assert.AreEqual(DisenoNivel4.Barriles.Length, barriles.Count);
            var esperados = DisenoNivel4.Barriles.OrderBy(b => b.x).ToList();
            for (var i = 0; i < barriles.Count; i++)
            {
                Assert.AreEqual(esperados[i], (Vector2)barriles[i].transform.position, "De pie en su sitio.");
                Assert.AreEqual(RutaMecanica(GeneradorMecanicas.BarrilExplosivo), PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(barriles[i].gameObject));
            }

            var minas = Todos<Mina>(_nivel4);
            Assert.AreEqual(DisenoNivel4.Minas.Length, minas.Count);
            Assert.AreEqual(DisenoNivel4.Minas[0], (Vector2)minas[0].transform.position, "Sujeta al fondo del río, en el segundo ojo.");
        }

        [Test]
        public void LasMemorias_TienenSuCinematicaYSuZona_YElSistemaDeConsecuenciasLasEscucha()
        {
            var memorias = Todos<MemoriaInteractuable>(_nivel4);
            Assert.AreEqual(DisenoNivel4.Memorias.Length, memorias.Count, "Las cuatro del Anexo A.");
            foreach (var esperada in DisenoNivel4.Memorias)
            {
                var memoria = memorias.SingleOrDefault(m => m.Id == esperada.Nombre);
                Assert.IsNotNull(memoria, $"Falta la memoria {esperada.Nombre}.");
                Assert.AreEqual(esperada.Posicion, (Vector2)memoria.transform.position, esperada.Nombre);
                Assert.AreEqual(esperada.Zona.Rect, memoria.Zona, $"{esperada.Nombre}: su zona de consecuencias.");
                Assert.AreSame(GeneradorNarrativa.CargarCinematicaMemoria(esperada.Nombre), memoria.Cinematica, $"{esperada.Nombre}: su cinemática.");
                Assert.AreEqual(DisenoNivel4.DistanciaRecordar, memoria.distancia, 1e-4f);
                Assert.IsNotNull(memoria.Brillo, $"{esperada.Nombre}: su brillo ámbar.");
            }

            var sistemas = Todos<SistemaConsecuencias>(_nivel4);
            Assert.AreEqual(1, sistemas.Count, "Un sistema de consecuencias para todo el nivel.");
            Assert.AreEqual(DisenoNivel4.AgresividadPorMemoria, sistemas[0].agresividad, 1e-4f, "Anexo A: un 20 % más agresivos.");
            Assert.AreEqual(DisenoNivel4.VidaDelJuezSinMemorias, sistemas[0].vidaDelJuezSinMemorias, 1e-4f, "Y el Juez, un 30 % más de vida.");
        }

        [Test]
        public void LasMemorias_TienenSuDibujo_YEncimaElMismoBrillando_QueSeApagaAlRecordarlas()
        {
            Assume.That(ConstructorMecanicasMundo4.Memorias.All(m => m.Completo), "Sin el arte de las memorias no hay nada que mirar.");
            var memorias = Todos<MemoriaInteractuable>(_nivel4);
            for (var i = 0; i < DisenoNivel4.Memorias.Length; i++)
            {
                var esperada = DisenoNivel4.Memorias[i];
                var dibujo = ConstructorMecanicasMundo4.Memorias[i];
                var memoria = memorias.Single(m => m.Id == esperada.Nombre);
                var apagada = memoria.transform.Find("Dibujo").GetComponent<SpriteRenderer>();
                Assert.AreSame(dibujo.Apagada, apagada.sprite, $"{esperada.Nombre}: el objeto, apagado.");
                Assert.AreSame(dibujo.Brillando, memoria.Brillo.sprite, $"{esperada.Nombre}: encima, el mismo brillando.");
                Assert.AreEqual(esperada.Posicion, (Vector2)apagada.transform.position, "Los dos, al pie de la memoria.");
                Assert.AreEqual(esperada.Posicion, (Vector2)memoria.Brillo.transform.position);
                Assert.Greater(memoria.Brillo.sortingOrder, apagada.sortingOrder, "El brillo, por encima del objeto.");
                Assert.Less(memoria.Brillo.sortingOrder, 10, "Y los dos, por detrás de Kael (10).");

                var ajustes = new SerializedObject(memoria);
                Assert.AreEqual(Color.white, ajustes.FindProperty("colorBrillo").colorValue, "El brillo, como se dibujó (late con su opacidad).");
                Assert.AreEqual(0f, ajustes.FindProperty("colorApagada").colorValue.a, "Recordada, el brillo se va y queda el objeto.");
            }
        }

        [Test]
        public void LasDosPistas_YLaLineaDelRio_SuenanEnSuSitio()
        {
            var zonas = Todos<ZonaDeDialogo>(_nivel4);
            foreach (var (zona, momento) in new[]
                     {
                         (DisenoNivel4.PistaDelBarril, MOMENTOS_DIALOGO.PISTA),
                         (DisenoNivel4.PistaDeLaTierraDeNadie, MOMENTOS_DIALOGO.PISTA_INVISIBILIDAD),
                         (DisenoNivel4.LineaDelRio, MOMENTOS_DIALOGO.ORILLA),
                     })
            {
                var dialogo = zonas.SingleOrDefault(z => z.name == zona.Nombre);
                Assert.IsNotNull(dialogo, $"Falta {zona.Nombre}.");
                Assert.AreEqual(momento, dialogo.Momento, zona.Nombre);
                Assert.IsTrue(dialogo.GetComponent<BoxCollider2D>().isTrigger);
                AssertRect(zona.Rect, Rectangulo(dialogo.GetComponent<BoxCollider2D>()), zona.Nombre);
                Assert.AreEqual(1, _datos.dialogos.Del(momento).Count, $"{zona.Nombre}: su línea está en los diálogos de la Culpa.");
            }
        }

        [Test]
        public void LaPlaza_TieneLasCuatroMarcas_YLaParedDeLasMemorias_DelJuez()
        {
            var juez = Todos<JuezSilente>(_nivel4).Single();
            var marcas = juez.Marcas;
            Assert.AreEqual(DisenoNivel4.Memorias.Length, marcas.Count, "Una marca por memoria.");
            for (var i = 0; i < marcas.Count; i++)
            {
                var marca = marcas[i];
                Assert.IsNotNull(marca, $"La marca {i}.");
                Assert.AreEqual(i, marca.memoria, "En el orden de las memorias.");
                Assert.AreEqual(new Vector2(DisenoNivel4.Memorias[i].Marca, DisenoNivel4.SueloArena), (Vector2)marca.transform.position,
                    $"La de {DisenoNivel4.Memorias[i].Objeto}, en su sitio de la plaza.");
                Assert.AreSame(juez, marca.Juez, "Se la enseña al Juez.");
                Assert.AreEqual(PreparadorNivel.ColorDeMarca(i), marca.Color, "Con el color de su memoria (sin arte) o el de su dibujo.");
                Assert.AreEqual(DisenoNivel4.DistanciaMarca, marca.RadioInteraccion, 1e-4f);
                Assert.IsNotNull(marca.Dibujo);
                Assert.IsFalse(marca.Brilla, "Apagada hasta que empieza su ataque.");
            }

            Assert.IsNotNull(juez.Pantalla, "La pared del fondo, donde proyecta.");
            Assert.IsFalse(juez.Pantalla.enabled, "Vacía hasta el primer ataque.");
            Assert.AreEqual(DisenoNivel4.Proyeccion.center, (Vector2)juez.Pantalla.transform.position);
            Assert.Less(juez.Pantalla.sortingOrder, 0, "Detrás del Juez y de Kael.");
        }

        [Test]
        public void EnLaPlaza_CadaMemoriaEsperaSobreSuLosa_YLaParedProyectaSusImagenes()
        {
            Assume.That(PreparadorNivel.HayArteDeLasMarcas, "Sin el arte de las losas y de las memorias, las marcas son rectángulos.");
            var juez = Todos<JuezSilente>(_nivel4).Single();
            for (var i = 0; i < juez.Marcas.Count; i++)
            {
                var marca = juez.Marcas[i];
                var losa = marca.transform.Find("Losa").GetComponent<SpriteRenderer>();
                var objeto = marca.transform.Find("Memoria").GetComponent<SpriteRenderer>();
                Assert.AreSame(ConstructorMecanicasMundo4.Losa.Apagada, losa.sprite, "La losa apagada, siempre.");
                Assert.AreSame(ConstructorMecanicasMundo4.Losa.Brillando, marca.Dibujo.sprite, "Encima, la encendida: lo que brilla.");
                Assert.AreSame(ConstructorMecanicasMundo4.Memorias[i].Apagada, objeto.sprite, "Y sobre ella, su memoria: se sabe de quién es.");
                Assert.AreEqual(Color.white, marca.Color);
                Assert.AreEqual(0f, new SerializedObject(marca).FindProperty("opacidadApagada").floatValue, "Apagada, la encendida no se ve.");
                Assert.AreEqual(DisenoNivel4.SueloArena, losa.bounds.max.y, 1e-3f, "La losa, a ras del suelo de la plaza.");
                Assert.AreEqual(DisenoNivel4.TamanoMarca.x, losa.bounds.size.x, 0.02f, "Y del ancho de una marca.");
                Assert.AreEqual(DisenoNivel4.Memorias[i].Marca, losa.bounds.center.x, 1e-3f);
                Assert.Less(losa.sortingOrder, marca.Dibujo.sortingOrder);
                Assert.Greater(objeto.sortingOrder, marca.Dibujo.sortingOrder, "El objeto, de pie delante de su losa.");
            }

            Assume.That(ConstructorCinematicasNivel4.ImagenesDeLasMemorias, Is.Not.Null, "Sin las imágenes, la pared es del color de la marca.");
            CollectionAssert.AreEqual(ConstructorCinematicasNivel4.ImagenesDeLasMemorias, juez.Imagenes.ToArray(), "Una imagen por memoria, en su orden.");
            Assert.IsTrue(juez.TieneImagenes);
            Assert.AreEqual(DisenoNivel4.Proyeccion.width, juez.Pantalla.bounds.size.x, 0.01f, "A lo ancho de la pared.");
            Assert.LessOrEqual(juez.Pantalla.bounds.size.y, DisenoNivel4.Proyeccion.height + 0.01f, "Sin pasarse de su alto.");
            var imagen = juez.Imagenes[0].rect.size;
            Assert.AreEqual(imagen.x / imagen.y, juez.Pantalla.bounds.size.x / juez.Pantalla.bounds.size.y, 0.01f, "Sin deformarla.");
        }

        [Test]
        public void ElNivel4_TieneElFondoDelMundo4()
        {
            Assume.That(AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorFondoMundo4.RutaPrefab), Is.Not.Null, "Sin su arte, el nivel no tiene fondo.");
            var fondos = _nivel4.GetRootGameObjects().Where(r => r.name == "FondoMundo4").ToList();
            Assert.AreEqual(1, fondos.Count, "Un fondo, el de la Culpa.");
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(fondos[0]), "Del prefab: se regenera con él.");
            Assert.IsNull(fondos[0].GetComponent<FondoConLaLuz>(), "Aquí es de día: no se oscurece.");
        }

        [Test]
        public void PrepararOtraVez_NoCambiaNada()
        {
            Assert.IsFalse(PreparadorNivel.Preparar(_nivel4, _datos), "Con la misma versión del diseño, la herramienta no toca nada.");
            Assert.IsFalse(_nivel4.isDirty);
        }

        private static string RutaPrefab(string variante)
        {
            var prefab = GeneradorEnemigos.CargarPrefab(variante);
            Assert.IsNotNull(prefab, $"Falta el prefab de {variante}: ejecuta Fragmentum → Regenerar todo.");
            return AssetDatabase.GetAssetPath(prefab);
        }

        private static string RutaMecanica(string nombre)
        {
            var prefab = GeneradorMecanicas.Cargar(nombre);
            Assert.IsNotNull(prefab, $"Falta el prefab {nombre}: ejecuta Fragmentum → Regenerar todo.");
            return AssetDatabase.GetAssetPath(prefab);
        }

        private static List<T> Todos<T>(Scene escena) where T : Component =>
            escena.GetRootGameObjects().SelectMany(raiz => raiz.GetComponentsInChildren<T>(true)).ToList();

        private static Rect Rectangulo(BoxCollider2D caja)
        {
            var escala = caja.transform.lossyScale;
            var tamano = new Vector2(Mathf.Abs(escala.x) * caja.size.x, Mathf.Abs(escala.y) * caja.size.y);
            var centro = (Vector2)caja.transform.position + Vector2.Scale(caja.offset, escala);
            return new Rect(centro - tamano * 0.5f, tamano);
        }
    }
}
