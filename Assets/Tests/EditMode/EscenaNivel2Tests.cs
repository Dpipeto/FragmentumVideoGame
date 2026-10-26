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
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La escena del Nivel 2 está montada con el recorrido de <see cref="DisenoNivel2"/>: bloques,
    /// agua, hielo, Lamentos, checkpoints, salida y la arena de la Doncella. Y la herramienta que la
    /// monta no cambia nada al prepararla otra vez.
    /// </summary>
    public class EscenaNivel2Tests
    {
        private Scene _nivel2;
        private LevelData _datos;

        [OneTimeSetUp]
        public void AbrirNivel2()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            Assert.IsNotNull(bd, "Falta la LevelDatabase: ejecuta Fragmentum → Regenerar todo.");
            _datos = bd.niveles[1];
            Assert.IsTrue(_datos.escena.Asignada, "El Nivel 2 tiene escena.");
            _nivel2 = EditorSceneManager.OpenScene(_datos.escena.Ruta, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void CerrarNivel2()
        {
            if (_nivel2.IsValid() && _nivel2.isLoaded)
            {
                EditorSceneManager.CloseScene(_nivel2, true);
            }
        }

        [Test]
        public void ElNivel2_EsJugable_YTieneLosCheckpointsDelDisenoEnSusDatos()
        {
            Assert.AreEqual(GeneradorProgresion.RutaEscenaNivel2, _datos.escena.Ruta);
            Assert.IsTrue(_datos.Jugable, "Está en Build Settings.");
            CollectionAssert.AreEqual(DisenoNivel2.Checkpoints.Select(c => c.x).ToList(), _datos.puntosDeControl);
        }

        [Test]
        public void ElNivel2_TieneLevelManager_ConInicio_YLaCamaraBajaHastaLaCalle()
        {
            var gestores = Todos<LevelManager>(_nivel2);
            Assert.AreEqual(1, gestores.Count);
            var serializado = new SerializedObject(gestores[0]);
            Assert.IsNotNull(serializado.FindProperty("prefabJugador").objectReferenceValue);
            var inicio = serializado.FindProperty("puntoInicio").objectReferenceValue as Transform;
            Assert.IsNotNull(inicio);
            Assert.AreEqual(new Vector2(DisenoNivel2.Inicio, 0.05f), (Vector2)inicio.position);
            Assert.AreEqual(DisenoNivel2.AlturaCamara, serializado.FindProperty("alturaCamara").vector2Value, "La calle está a y −11.");

            var camaras = Todos<Camera>(_nivel2);
            Assert.AreEqual(1, camaras.Count);
            Assert.IsNotNull(camaras[0].GetComponent<CameraMovement>());
            Assert.AreEqual(DisenoNivel2.TamanoCamara, camaras[0].orthographicSize, 1e-4f, "El encuadre del Nivel 1.");
        }

        [Test]
        public void ElNivel2_TieneElRecorridoDeLaVersionActual()
        {
            var recorrido = PreparadorNivel.RaizRecorrido(_nivel2);
            Assert.IsNotNull(recorrido);
            Assert.IsNotNull(recorrido.Find(PreparadorNivel.MarcaVersionNivel2), "Montado con la versión actual del diseño.");
            Assert.IsFalse(_nivel2.GetRootGameObjects().Any(r => r.name == PreparadorNivel.NombreProvisional));

            var cajas = recorrido.GetComponentsInChildren<BoxCollider2D>(true).Where(c => !c.isTrigger).ToList();
            foreach (var esperado in DisenoNivel2.Suelos.Concat(DisenoNivel2.Paredes).Concat(DisenoNivel2.PlataformasArena))
            {
                var caja = cajas.SingleOrDefault(b => b.name == esperado.Nombre);
                Assert.IsNotNull(caja, $"Falta el bloque {esperado.Nombre}.");
                var rect = Rectangulo(caja);
                Assert.AreEqual(esperado.Desde, rect.xMin, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Hasta, rect.xMax, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Abajo, rect.yMin, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Arriba, rect.yMax, 0.01f, esperado.Nombre);
            }

            foreach (var placa in DisenoNivel2.Placas)
            {
                var caja = cajas.SingleOrDefault(b => b.name == placa.Nombre);
                Assert.IsNotNull(caja, $"Falta la placa {placa.Nombre}.");
                Assert.AreEqual(placa.Y, Rectangulo(caja).yMax, 0.01f, placa.Nombre);
                Assert.AreEqual(LayerMask.NameToLayer(Capas.Suelo), caja.gameObject.layer, $"{placa.Nombre} se pisa.");

                var flotante = caja.GetComponent<PlataformaFlotante>();
                Assert.IsNotNull(flotante, $"{placa.Nombre} flota.");
                Assert.AreEqual(GeneradorMecanicas.Carpeta + $"/{GeneradorMecanicas.PlataformaFlotante}.prefab", PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(caja.gameObject));
                Assert.AreEqual(placa.Amplitud, flotante.amplitud, 1e-4f, placa.Nombre);
                Assert.AreEqual(DisenoNivel2.PeriodoPlaca, flotante.periodo, 1e-4f, placa.Nombre);
                Assert.AreEqual(placa.Desfase, flotante.desfase, 1e-4f, placa.Nombre);
            }

            var capaSuelo = LayerMask.NameToLayer(Capas.Suelo);
            Assert.IsTrue(DisenoNivel2.Suelos.All(s => cajas.Single(c => c.name == s.Nombre).gameObject.layer == capaSuelo));
            Assert.IsTrue(DisenoNivel2.Paredes.All(s => cajas.Single(c => c.name == s.Nombre).gameObject.layer == LayerMask.NameToLayer(Capas.Wall)));
        }

        [Test]
        public void ElAguaYElHielo_EstanDondeDiceElDiseno_SinContarElDeLasPlacas()
        {
            // Las franjas de hielo de las placas van con ellas: aquí cuentan solo las del suelo.
            var zonas = Todos<ZonaModificadorMovimiento>(_nivel2).Where(z => z.GetComponentInParent<PlataformaFlotante>() == null).ToList();
            var aguas = zonas.Where(z => z.Modificador.esAgua).ToList();
            var hielos = zonas.Where(z => !z.Modificador.esAgua).ToList();
            Assert.AreEqual(DisenoNivel2.Aguas.Length, aguas.Count);
            Assert.AreEqual(DisenoNivel2.Hielos.Length, hielos.Count);

            foreach (var esperado in DisenoNivel2.Aguas.Concat(DisenoNivel2.Hielos))
            {
                var zona = zonas.SingleOrDefault(z => z.name == esperado.Nombre);
                Assert.IsNotNull(zona, $"Falta {esperado.Nombre}.");
                var caja = zona.GetComponent<BoxCollider2D>();
                Assert.IsTrue(caja.isTrigger, $"{esperado.Nombre} no choca: se atraviesa.");
                var rect = Rectangulo(caja);
                Assert.AreEqual(esperado.Rect.xMin, rect.xMin, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Rect.xMax, rect.xMax, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Rect.yMin, rect.yMin, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Rect.yMax, rect.yMax, 0.01f, esperado.Nombre);
            }

            Assert.IsTrue(hielos.All(h => h.Modificador.aceleracion < 0.5f), "El hielo resbala.");
            // Con su arte, la zona no se dibuja ella: la dibujan su superficie y, debajo, su cuerpo liso.
            var conArte = ConstructorMecanicasMundo2.SuperficieDelAgua != null;
            foreach (var esperado in DisenoNivel2.Aguas)
            {
                var agua = aguas.Single(a => a.name == esperado.Nombre);
                var dibujos = new List<SpriteRenderer>();
                if (conArte)
                {
                    Assert.IsNull(agua.GetComponent<SpriteRenderer>(), $"{agua.name}: con arte, el velo liso sobra.");
                    var superficie = agua.transform.parent.Find(PreparadorNivel.NombreSuperficie(esperado));
                    var cuerpo = agua.transform.parent.Find(PreparadorNivel.NombreCuerpoDelAgua(esperado));
                    Assert.IsNotNull(superficie, $"{agua.name} tiene su superficie.");
                    Assert.IsNotNull(cuerpo, $"{agua.name} tiene su cuerpo.");
                    Assert.AreEqual(esperado.Arriba, superficie.position.y, 1e-4f, $"{agua.name}: la superficie, a la altura del agua.");
                    Assert.AreEqual(esperado.Hasta - esperado.Desde, superficie.GetComponent<SpriteRenderer>().size.x, 1e-4f, agua.name);
                    Assert.IsNotNull(superficie.GetComponent<BucleDeSprites>(), $"{agua.name}: la superficie se mueve.");
                    dibujos.Add(superficie.GetComponent<SpriteRenderer>());
                    dibujos.Add(cuerpo.GetComponent<SpriteRenderer>());
                }
                else
                {
                    dibujos.Add(agua.GetComponent<SpriteRenderer>());
                }

                foreach (var dibujo in dibujos)
                {
                    Assert.IsNotNull(dibujo, $"{agua.name} se ve.");
                    Assert.Less(dibujo.color.a, 1f, "Translúcida.");
                    Assert.AreEqual(PreparadorNivel.OrdenAgua, dibujo.sortingOrder, "Por delante de Kael: se le ve sumergido.");
                }
            }
        }

        [Test]
        public void ElNivel2_TieneLosLamentosConSuRuta_YLasEsculturasDormidasEnSuSitio()
        {
            var enemigos = Todos<EnemyController>(_nivel2);
            var lamentos = DisenoNivel2.Enemigos.Where(e => e.Tipo == DisenoNivel2.TIPOS_ENEMIGO.LAMENTO).ToList();
            var esculturas = DisenoNivel2.Enemigos.Where(e => e.Tipo == DisenoNivel2.TIPOS_ENEMIGO.ESCULTURA).ToList();
            Assert.AreEqual(lamentos.Count + esculturas.Count + 1, enemigos.Count,
                "Los Lamentos, las Esculturas y la Plañidera (las Medusas no son EnemyController).");

            foreach (var esperado in lamentos)
            {
                var enemigo = enemigos.SingleOrDefault(e => e.name == esperado.Nombre);
                Assert.IsNotNull(enemigo, $"Falta {esperado.Nombre}.");
                Assert.AreEqual(RutaPrefab("Lamento"), PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(enemigo.gameObject));
                Assert.AreEqual(new Vector2(esperado.X, esperado.Y + 0.05f), (Vector2)enemigo.transform.position, esperado.Nombre);
                Assert.IsNotNull(enemigo.GetComponent<Pesar>(), $"{esperado.Nombre} pesa.");

                var ruta = new SerializedObject(enemigo).FindProperty("waypoints");
                Assert.AreEqual(2, ruta.arraySize, esperado.Nombre);
                Assert.AreEqual(esperado.Desde, (ruta.GetArrayElementAtIndex(0).objectReferenceValue as Transform).position.x, 0.01f);
                Assert.AreEqual(esperado.Hasta, (ruta.GetArrayElementAtIndex(1).objectReferenceValue as Transform).position.x, 0.01f);
            }

            foreach (var esperada in esculturas)
            {
                var escultura = enemigos.SingleOrDefault(e => e.name == esperada.Nombre);
                Assert.IsNotNull(escultura, $"Falta {esperada.Nombre}.");
                Assert.AreEqual(RutaPrefab("Escultura"), PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(escultura.gameObject));
                Assert.AreEqual(new Vector2(esperada.X, esperada.Y + 0.05f), (Vector2)escultura.transform.position, esperada.Nombre);
                Assert.IsFalse(escultura.enabled, $"{esperada.Nombre} empieza dormida.");
                Assert.IsNotNull(escultura.GetComponent<EsculturaDormida>());
                Assert.AreEqual(0, new SerializedObject(escultura).FindProperty("waypoints").arraySize, $"{esperada.Nombre} no anda: sin ruta.");
            }
        }

        [Test]
        public void ElNivel2_TieneALaPlanidera_ConSuRuta_EnElSitioDeLaSegundaEscultura()
        {
            var esperada = DisenoNivel2.Enemigos.Single(e => e.Tipo == DisenoNivel2.TIPOS_ENEMIGO.PLANIDERA);
            var planideras = Todos<Planidera>(_nivel2);
            Assert.AreEqual(1, planideras.Count, "Un mini-jefe.");
            var planidera = planideras[0];
            Assert.AreEqual(esperada.Nombre, planidera.name);
            Assert.AreEqual(RutaPrefab("Planidera"), PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(planidera.gameObject));
            Assert.AreEqual(new Vector2(esperada.X, esperada.Y + 0.05f), (Vector2)planidera.transform.position);

            var controlador = planidera.GetComponent<EnemyController>();
            Assert.IsTrue(controlador.enabled, "Empieza acechando, no dormida.");
            var ruta = new SerializedObject(controlador).FindProperty("waypoints");
            Assert.AreEqual(2, ruta.arraySize);
            Assert.AreEqual(esperada.Desde, (ruta.GetArrayElementAtIndex(0).objectReferenceValue as Transform).position.x, 0.01f);
            Assert.AreEqual(esperada.Hasta, (ruta.GetArrayElementAtIndex(1).objectReferenceValue as Transform).position.x, 0.01f);
            Assert.IsFalse(Todos<EsculturaDormida>(_nivel2).Any(e => e.transform.position.x > esperada.Desde - 8f && e.transform.position.x < DisenoNivel2.Agujero.Desde),
                "En su plaza no queda ninguna Escultura: las dos del canal están mucho más allá.");
        }

        [Test]
        public void ElNivel2_TieneLasCuatroMedusas_ConElRecorridoDelDiseno()
        {
            var medusas = Todos<MedusaHielo>(_nivel2);
            var esperadas = DisenoNivel2.Enemigos.Where(e => e.Tipo == DisenoNivel2.TIPOS_ENEMIGO.MEDUSA).ToList();
            Assert.AreEqual(esperadas.Count, medusas.Count);
            foreach (var esperada in esperadas)
            {
                var medusa = medusas.SingleOrDefault(m => m.name == esperada.Nombre);
                Assert.IsNotNull(medusa, $"Falta {esperada.Nombre}.");
                Assert.AreEqual(GeneradorEnemigos.RutaPrefabMedusa, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(medusa.gameObject));
                Assert.AreEqual(new Vector2(esperada.X, esperada.Y), (Vector2)medusa.transform.position, "En el centro de su recorrido.");
                Assert.AreEqual(esperada.Vertical, medusa.vertical, esperada.Nombre);
                Assert.AreEqual(esperada.Amplitud, medusa.amplitud, 1e-4f, esperada.Nombre);
                Assert.AreEqual(esperada.Periodo, medusa.periodo, 1e-4f, esperada.Nombre);
                Assert.AreEqual(esperada.Desfase, medusa.desfase, 1e-4f, esperada.Nombre);
            }
        }

        [Test]
        public void ElNivel2_TieneLosCheckpoints_LaSalida_YLaZonaMortalBajoLaCalle()
        {
            var checkpoints = Todos<Checkpoint>(_nivel2).OrderBy(c => c.transform.position.x).ToList();
            Assert.AreEqual(DisenoNivel2.Checkpoints.Length, checkpoints.Count);
            for (var i = 0; i < checkpoints.Count; i++)
            {
                Assert.AreEqual($"L2_CP{i + 1}", checkpoints[i].Id);
                Assert.AreEqual(DisenoNivel2.Checkpoints[i] + new Vector2(0f, 0.05f), (Vector2)checkpoints[i].transform.position, "De pie en su suelo.");
                Assert.IsTrue(checkpoints[i].GetComponent<Collider2D>().isTrigger);
            }

            var salidas = Todos<SalidaNivel>(_nivel2);
            Assert.AreEqual(1, salidas.Count);
            Assert.AreEqual(new Vector2(DisenoNivel2.Salida, DisenoNivel2.SueloArena), (Vector2)salidas[0].transform.position);

            var zonas = Todos<DeadZone>(_nivel2);
            Assert.AreEqual(1, zonas.Count);
            Assert.Less(Rectangulo((BoxCollider2D)zonas[0].GetComponent<Collider2D>()).yMax, DisenoNivel2.PisadaDe("SueloCalle").Y - 2f,
                "La zona mortal queda muy por debajo de la calle: bajo el agua no se muere por caer.");
        }

        [Test]
        public void ElNivel2_TieneLaArenaConLaDoncellaDelLamento()
        {
            var arenas = Todos<ArenaJefe>(_nivel2);
            Assert.AreEqual(1, arenas.Count);
            var arena = arenas[0];

            Assert.IsInstanceOf<DoncellaLamento>(arena.Jefe, "El jefe de la Tristeza es la Doncella del Lamento.");
            Assert.AreEqual(DisenoNivel2.LimitesArena, arena.Limites);
            Assert.AreEqual(DisenoNivel2.Jefe, arena.Jefe.transform.position.x, 0.01f);
            Assert.AreEqual(DisenoNivel2.Entrada, arena.transform.position.x, 0.01f);
            Assert.IsTrue(arena.GetComponent<Collider2D>().isTrigger);

            var puertas = new SerializedObject(arena).FindProperty("puertas");
            Assert.AreEqual(1, puertas.arraySize);
            var puerta = puertas.GetArrayElementAtIndex(0).objectReferenceValue as GameObject;
            Assert.IsNotNull(puerta);
            Assert.IsFalse(puerta.activeSelf, "Abierta hasta que empieza el combate.");

            var plataformas = DisenoNivel2.Placas.Where(p => p.X > DisenoNivel2.LimitesArena.x).Select(p => p.Nombre)
                .Concat(DisenoNivel2.PlataformasArena.Select(p => p.Nombre)).ToList();
            CollectionAssert.AreEquivalent(plataformas, arena.Plataformas.Select(p => p.name).ToList(), "La Doncella atraviesa las placas y el balcón.");
        }

        [Test]
        public void LasSeccionesDelNivel_SonLasDelDiseno_YLaCamaraLlegaAlFondoDeLaArena()
        {
            Assert.AreEqual(DisenoNivel2.Secciones.Length, _datos.secciones.Count);
            for (var i = 0; i < _datos.secciones.Count; i++)
            {
                Assert.AreEqual(DisenoNivel2.Secciones[i].x, _datos.secciones[i].desdeX, 1e-4f, _datos.secciones[i].nombre);
                Assert.AreEqual(DisenoNivel2.Secciones[i].y, _datos.secciones[i].hastaX, 1e-4f, _datos.secciones[i].nombre);
            }

            var margen = new SerializedObject(Todos<LevelManager>(_nivel2).Single()).FindProperty("margenCamara").floatValue;
            Assert.AreEqual(DisenoNivel2.MargenCamara, margen, 1e-4f);
            var fondo = DisenoNivel2.Paredes.Single(p => p.Nombre == "ParedArena");
            Assert.AreEqual(fondo.Hasta, _datos.Extension.y + margen, 1e-4f, "La cámara llega hasta la pared del fondo de la arena.");
        }

        [Test]
        public void LaMuralla_NoAgarra_YEsLaUnica()
        {
            var sinAgarre = Todos<SinAgarre>(_nivel2);
            Assert.AreEqual(1, sinAgarre.Count, "Solo la muralla: las demás paredes se escalan.");
            Assert.AreEqual("Muralla", sinAgarre[0].name);
            Assert.IsTrue(DisenoNivel2.NoAgarra(sinAgarre[0].name));
            var caja = sinAgarre[0].GetComponent<BoxCollider2D>();
            Assert.IsFalse(caja.isTrigger, "Sigue siendo una pared: se choca con ella.");
            Assert.AreEqual(LayerMask.NameToLayer(Capas.Wall), caja.gameObject.layer);
        }

        [Test]
        public void DelanteDelTapon_KaelTieneSuPista()
        {
            var zonas = Todos<ZonaDeDialogo>(_nivel2);
            Assert.AreEqual(1, zonas.Count);
            var zona = zonas[0];
            Assert.AreEqual(PreparadorNivel.NombrePistaDelTapon, zona.name);
            Assert.AreEqual(Fragmentum.Narrative.MOMENTOS_DIALOGO.PISTA, zona.Momento);
            Assert.AreSame(Todos<HieloRompible>(_nivel2).Single(), zona.Hielo, "La dice también si agrieta el tapón, y calla si ya está roto.");

            var caja = zona.GetComponent<BoxCollider2D>();
            Assert.IsTrue(caja.isTrigger, "No choca: Kael la atraviesa.");
            var rect = Rectangulo(caja);
            var esperado = DisenoNivel2.PistaDelTapon.Rect;
            Assert.AreEqual(esperado.xMin, rect.xMin, 0.01f);
            Assert.AreEqual(esperado.xMax, rect.xMax, 0.01f);
            Assert.AreEqual(esperado.yMin, rect.yMin, 0.01f);
            Assert.AreEqual(esperado.yMax, rect.yMax, 0.01f);

            var lineas = _datos.dialogos.Del(Fragmentum.Narrative.MOMENTOS_DIALOGO.PISTA);
            Assert.AreEqual(1, lineas.Count, "El nivel tiene la línea que dice.");
            StringAssert.Contains("Ataque Cargado", lineas[0].texto);
        }

        [Test]
        public void LosCheckpoints_Curan()
        {
            Assert.IsTrue(Todos<Checkpoint>(_nivel2).All(c => c.Cura), "Decisión del 2026-10-12: todos los checkpoints curan a Kael.");
        }

        [Test]
        public void ElAgujero_LoTapaElHieloRompible_YHayEcosDeIntentos()
        {
            var tapones = Todos<HieloRompible>(_nivel2);
            Assert.AreEqual(1, tapones.Count, "Un tapón: la única salida de la superficie.");
            Assert.AreEqual(DisenoNivel2.HieloRompible, (Vector2)tapones[0].transform.position);
            Assert.AreEqual(GeneradorMecanicas.Carpeta + $"/{GeneradorMecanicas.HieloRompible}.prefab", PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(tapones[0].gameObject));

            Assert.AreEqual(1, Todos<EcoDeIntentos>(_nivel2).Count, "Los ecos de intentos del mundo de la Tristeza.");
        }

        [Test]
        public void ElEcoDeMemoria_SuenaEnLaCampana_YSoloAhi()
        {
            var checkpoints = Todos<Checkpoint>(_nivel2);
            var conEco = checkpoints.Where(c => c.EcoDeMemoria).ToList();
            Assert.AreEqual(1, conEco.Count);
            Assert.AreEqual("L2_CP1", conEco[0].Id, "Junto a la silla vacía.");
            Assert.AreEqual(PreparadorNivel.CheckpointDelEcoDeMemoria(_datos), conEco[0].Id);
        }

        [Test]
        public void PrepararOtraVez_NoCambiaNada()
        {
            Assert.IsFalse(PreparadorNivel.Preparar(_nivel2, _datos), "Con la misma versión del diseño, la herramienta no toca nada.");
            Assert.IsFalse(_nivel2.isDirty);
        }

        [Test]
        public void LaArena_TieneLasTresFranjas_YLaDoncellaLasLleva()
        {
            var franjas = Todos<FranjaInundable>(_nivel2).OrderBy(f => f.transform.position.x).ToList();
            Assert.AreEqual(DisenoNivel2.Franjas.Length, franjas.Count);
            for (var i = 0; i < franjas.Count; i++)
            {
                var zona = DisenoNivel2.Franjas[i];
                var franja = franjas[i];
                Assert.AreEqual(zona.Nombre, franja.name);
                Assert.AreEqual(zona.Desde, franja.Desde, 1e-3f, zona.Nombre);
                Assert.AreEqual(zona.Hasta, franja.Hasta, 1e-3f, zona.Nombre);
                Assert.AreEqual(zona.Abajo, franja.Suelo, 1e-3f, $"{zona.Nombre}: sale del suelo de la arena.");
                Assert.AreEqual(zona.Arriba, franja.Superficie, 1e-3f, $"{zona.Nombre}: hasta dónde sube el agua.");
                Assert.AreEqual(DisenoNivel2.SueloArena + DisenoNivel2.CapaHelada, franja.Techo, 1e-3f, $"{zona.Nombre}: la capa de hielo.");
                Assert.AreEqual(DisenoNivel2.GrosorCapaHelada, franja.grosorCapa, 1e-3f, zona.Nombre);
            }

            var arena = Todos<ArenaJefe>(_nivel2).Single();
            var doncella = (DoncellaLamento)arena.Jefe;
            CollectionAssert.AreEqual(franjas, doncella.Franjas.ToList(), "La Doncella lleva las tres, de izquierda a derecha.");
            Assert.AreEqual(DisenoNivel2.VueloDoncella, doncella.AlturaDeVuelo, 1e-4f);
            Assert.IsTrue(doncella.Datos.inmuneFueraDeVulnerable, "Solo se la hiere mientras llora.");
            Assert.IsFalse(GeneradorJefes.EsEsqueleto(doncella.Datos), "Ya tiene sus patrones.");

            // Con Kael en el suelo de la arena, se la ve entera mientras flota.
            var camara = Todos<Camera>(_nivel2).Single();
            var desplazamiento = new SerializedObject(camara.GetComponent<CameraMovement>()).FindProperty("desplazamiento").vector3Value;
            var loMasAltoQueSeVe = DisenoNivel2.SueloArena + desplazamiento.y + camara.orthographicSize;
            Assert.LessOrEqual(DisenoNivel2.SueloArena + DisenoNivel2.VueloDoncella + doncella.Datos.tamano.y, loMasAltoQueSeVe,
                "Flota dentro de lo que enseña la cámara cuando Kael está en el suelo.");
        }

        // El prefab de una variante: el de su arte (Lamento.prefab) si está y, si no, el de bloques
        // de color (Enemigo_Lamento.prefab). Es el que coloca el preparador del nivel.
        private static string RutaPrefab(string variante)
        {
            var prefab = GeneradorEnemigos.CargarPrefab(variante);
            Assert.IsNotNull(prefab, $"Falta el prefab de {variante}: ejecuta Fragmentum → Regenerar todo.");
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
