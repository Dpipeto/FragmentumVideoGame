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
using Fragmentum.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Fragmentum.Tests
{
    /// <summary>
    /// La escena del Nivel 3 está montada con el recorrido de <see cref="DisenoNivel3"/>: bloques,
    /// ramas, raíces que no agarran, Espectros y Centinelas Ciegos, checkpoints, salida y la arena del
    /// Espectro del Pavor (fase A); la oscuridad por tramos, las antorchas, los cepos, las ramas
    /// podridas que ceden y las cintas (fase B); y el Acechador con los árboles del claro, los
    /// Espectros dormidos y el oído de los Centinelas (fase C); y la ciénaga, con su agua, sus
    /// marañas y su pista. Y la herramienta que la monta no cambia nada al prepararla otra vez.
    /// </summary>
    public class EscenaNivel3Tests
    {
        private Scene _nivel3;
        private LevelData _datos;

        [OneTimeSetUp]
        public void AbrirNivel3()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            Assert.IsNotNull(bd, "Falta la LevelDatabase: ejecuta Fragmentum → Regenerar todo.");
            _datos = bd.niveles[2];
            Assert.IsTrue(_datos.escena.Asignada, "El Nivel 3 tiene escena.");
            _nivel3 = EditorSceneManager.OpenScene(_datos.escena.Ruta, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void CerrarNivel3()
        {
            if (_nivel3.IsValid() && _nivel3.isLoaded)
            {
                EditorSceneManager.CloseScene(_nivel3, true);
            }
        }

        [Test]
        public void ElNivel3_EsJugable_YTieneLosCheckpointsYElTiempoDelDiseno()
        {
            Assert.AreEqual(GeneradorProgresion.RutaEscenaNivel3, _datos.escena.Ruta);
            Assert.IsTrue(_datos.Jugable, "Está en Build Settings.");
            CollectionAssert.AreEqual(DisenoNivel3.Checkpoints.Select(c => c.x).ToList(), _datos.puntosDeControl);
            Assert.AreEqual(960f, _datos.tiempoConBono, 0.01f, "Decisión 9: 15 minutos con bono, porque se camina mucho; y uno más por la ciénaga.");
            Assert.AreEqual(DisenoNivel3.ColorVeloAgua, _datos.reglas.colorVeloAgua, "Bajo el agua de la ciénaga, el velo es verde.");
            Assert.IsTrue(_datos.reglas.sigilo, "Correr hace ruido.");
            Assert.IsTrue(_datos.reglas.oscuridad, "El nivel es oscuro: lo aplica el LevelManager con la LuzDelNivel de la escena.");
            Assert.AreEqual(DisenoNivel3.RadioLuzKael, _datos.reglas.radioLuzJugador, 1e-4f);
        }

        [Test]
        public void ElNivel3_TieneLevelManager_ConInicio_YLaCamaraBajaHastaLaHondonada()
        {
            var gestores = Todos<LevelManager>(_nivel3);
            Assert.AreEqual(1, gestores.Count);
            var serializado = new SerializedObject(gestores[0]);
            Assert.IsNotNull(serializado.FindProperty("prefabJugador").objectReferenceValue);
            var inicio = serializado.FindProperty("puntoInicio").objectReferenceValue as Transform;
            Assert.IsNotNull(inicio);
            Assert.AreEqual(new Vector2(DisenoNivel3.Inicio, 0.05f), (Vector2)inicio.position);
            Assert.AreEqual(DisenoNivel3.AlturaCamara, serializado.FindProperty("alturaCamara").vector2Value);

            var camaras = Todos<Camera>(_nivel3);
            Assert.AreEqual(1, camaras.Count);
            Assert.IsNotNull(camaras[0].GetComponent<CameraMovement>());
            Assert.AreEqual(DisenoNivel3.TamanoCamara, camaras[0].orthographicSize, 1e-4f, "El encuadre de los otros dos niveles.");
        }

        [Test]
        public void ElNivel3_TieneElRecorridoDeLaVersionActual_ConSusBloquesYSusRamas()
        {
            var recorrido = PreparadorNivel.RaizRecorrido(_nivel3);
            Assert.IsNotNull(recorrido);
            Assert.IsNotNull(recorrido.Find(PreparadorNivel.MarcaVersionNivel3), "Montado con la versión actual del diseño.");
            Assert.IsFalse(_nivel3.GetRootGameObjects().Any(r => r.name == PreparadorNivel.NombreProvisional));

            var cajas = recorrido.GetComponentsInChildren<BoxCollider2D>(true).Where(c => !c.isTrigger).ToList();
            var capaSuelo = LayerMask.NameToLayer(Capas.Suelo);
            foreach (var esperado in DisenoNivel3.Suelos.Concat(DisenoNivel3.Paredes).Concat(DisenoNivel3.Ramas)
                         .Concat(DisenoNivel3.RamasPodridas).Concat(DisenoNivel3.PlataformasArena))
            {
                var caja = cajas.SingleOrDefault(b => b.name == esperado.Nombre);
                Assert.IsNotNull(caja, $"Falta el bloque {esperado.Nombre}.");
                var rect = Rectangulo(caja);
                Assert.AreEqual(esperado.Desde, rect.xMin, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Hasta, rect.xMax, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Abajo, rect.yMin, 0.01f, esperado.Nombre);
                Assert.AreEqual(esperado.Arriba, rect.yMax, 0.01f, esperado.Nombre);
            }

            Assert.IsTrue(DisenoNivel3.Suelos.Concat(DisenoNivel3.Ramas).Concat(DisenoNivel3.RamasPodridas).Concat(DisenoNivel3.PlataformasArena)
                .All(s => cajas.Single(c => c.name == s.Nombre).gameObject.layer == capaSuelo), "Todo lo que se pisa está en la capa del suelo.");
            Assert.IsTrue(DisenoNivel3.Paredes.All(s => cajas.Single(c => c.name == s.Nombre).gameObject.layer == LayerMask.NameToLayer(Capas.Wall)));
        }

        [Test]
        public void LasRaicesDelCallejon_YLasDelArbolHundido_NoAgarran_YSonLasUnicas()
        {
            var sinAgarre = Todos<SinAgarre>(_nivel3);
            var esperadas = DisenoNivel3.Paredes.Where(p => DisenoNivel3.NoAgarra(p.Nombre)).Select(p => p.Nombre).ToList();
            CollectionAssert.AreEquivalent(new[] { "RaicesDelCallejon", "RaicesDelArbolHundido1", "RaicesDelArbolHundido2" }, esperadas);
            CollectionAssert.AreEquivalent(esperadas, sinAgarre.Select(s => s.name).ToList(), "Solo las raíces: las demás paredes se escalan.");
            foreach (var raices in sinAgarre)
            {
                var caja = raices.GetComponent<BoxCollider2D>();
                Assert.IsFalse(caja.isTrigger, $"{raices.name} sigue siendo una pared: se choca con ella.");
                Assert.AreEqual(LayerMask.NameToLayer(Capas.Wall), caja.gameObject.layer);
            }
        }

        [Test]
        public void LaCienaga_TieneSuAgua_SusMaranasDibujadas_YSuPista()
        {
            // El agua: una zona por cada una del diseño, con su superficie dibujada en bucle.
            var aguas = Todos<ZonaModificadorMovimiento>(_nivel3).Where(z => z.Modificador.esAgua).ToList();
            Assert.AreEqual(DisenoNivel3.Aguas.Length, aguas.Count, "Solo la ciénaga tiene agua.");
            foreach (var zona in DisenoNivel3.Aguas)
            {
                var agua = aguas.SingleOrDefault(a => a.name == zona.Nombre);
                Assert.IsNotNull(agua, $"Falta el agua {zona.Nombre}.");
                AssertRect(zona.Rect, Rectangulo(agua.GetComponent<BoxCollider2D>()), zona.Nombre);
                Assert.IsTrue(agua.GetComponent<BoxCollider2D>().isTrigger);
                var superficie = PreparadorNivel.RaizRecorrido(_nivel3).GetComponentsInChildren<BucleDeSprites>(true)
                    .SingleOrDefault(b => b.name == PreparadorNivel.NombreSuperficie(zona));
                Assert.IsNotNull(superficie, $"{zona.Nombre}: su superficie dibujada.");
                Assert.AreEqual(ConstructorMecanicasMundo3.FotogramasAgua.Length, superficie.Fotogramas, "Con sus tres fotogramas, en bucle.");
                Assert.AreEqual(zona.Arriba, superficie.transform.position.y, 1e-4f, "Su línea, a la altura del agua.");
            }

            // Las marañas: una zona que sujeta, con un dibujo por cada 2,5 u.
            var maranas = Todos<Marana>(_nivel3);
            Assert.AreEqual(DisenoNivel3.Maranas.Length, maranas.Count);
            foreach (var zona in DisenoNivel3.Maranas)
            {
                var marana = maranas.SingleOrDefault(m => m.name == zona.Nombre);
                Assert.IsNotNull(marana, $"Falta la maraña {zona.Nombre}.");
                AssertRect(zona.Rect, Rectangulo(marana.GetComponent<BoxCollider2D>()), zona.Nombre);
                Assert.IsTrue(marana.GetComponent<BoxCollider2D>().isTrigger, "Se entra en ella: no es una pared.");
                var sujeta = marana.GetComponent<ZonaModificadorMovimiento>().Modificador;
                Assert.AreEqual(ModificadorMovimiento.Marana.velocidad, sujeta.velocidad, 1e-4f, $"{zona.Nombre}: sujeta.");
                Assert.AreEqual(0f, sujeta.salto, 1e-4f, $"{zona.Nombre}: dentro no se salta.");

                var piezas = Mathf.RoundToInt((zona.Hasta - zona.Desde) / DisenoNivel3.AnchoDibujoMarana);
                Assert.AreEqual(piezas, marana.Dibujos.Length, $"{zona.Nombre}: un dibujo por cada 2,5 u.");
                Assert.IsTrue(marana.Dibujos.All(d => d != null && d.sprite != null), $"{zona.Nombre}: con su arte.");
                Assert.IsTrue(marana.Dibujos.All(d => d.sortingOrder == PreparadorNivel.OrdenMarana), "Por delante de Kael y por detrás del agua.");
                var serializada = new SerializedObject(marana);
                Assert.AreEqual(ConstructorMecanicasMundo3.FotogramasMaranaReposo.Length, serializada.FindProperty("reposo").arraySize);
                Assert.AreEqual(ConstructorMecanicasMundo3.FotogramasMaranaAtrapa.Length, serializada.FindProperty("atrapa").arraySize);
            }

            // La pista, en la orilla, escucha a las dos marañas.
            var pista = Todos<ZonaDeDialogo>(_nivel3).SingleOrDefault(z => z.name == PreparadorNivel.NombrePistaDeLaCienaga);
            Assert.IsNotNull(pista, "Ante el agua, Kael nombra el Dash Acuático.");
            Assert.AreEqual(Fragmentum.Narrative.MOMENTOS_DIALOGO.PISTA, pista.Momento);
            CollectionAssert.AreEquivalent(maranas, pista.Maranas, "Y lo repite si una maraña lo sujeta.");
            AssertRect(DisenoNivel3.PistaDeLaCienaga.Rect, Rectangulo(pista.GetComponent<BoxCollider2D>()), pista.name);
            Assert.IsTrue(_datos.dialogos.Del(Fragmentum.Narrative.MOMENTOS_DIALOGO.PISTA).Count > 0, "El Nivel 3 tiene la línea de la pista.");
        }

        private static void AssertRect(Rect esperado, Rect real, string nombre)
        {
            Assert.AreEqual(esperado.xMin, real.xMin, 0.01f, nombre);
            Assert.AreEqual(esperado.xMax, real.xMax, 0.01f, nombre);
            Assert.AreEqual(esperado.yMin, real.yMin, 0.01f, nombre);
            Assert.AreEqual(esperado.yMax, real.yMax, 0.01f, nombre);
        }

        [Test]
        public void ElNivel3_TieneLosEspectrosLosCentinelasCiegosYElAcechador_ConSuRuta()
        {
            var enemigos = Todos<EnemyController>(_nivel3);
            Assert.AreEqual(DisenoNivel3.Enemigos.Length, enemigos.Count, "Los Espectros, los Centinelas Ciegos y el Acechador.");
            foreach (var esperado in DisenoNivel3.Enemigos)
            {
                var enemigo = enemigos.SingleOrDefault(e => e.name == esperado.Nombre);
                Assert.IsNotNull(enemigo, $"Falta {esperado.Nombre}.");
                var variante = esperado.Tipo == DisenoNivel3.TIPOS_ENEMIGO.ESPECTRO ? "Espectro"
                    : esperado.Tipo == DisenoNivel3.TIPOS_ENEMIGO.CENTINELA_CIEGO ? "CentinelaCiego"
                    : "Acechador";
                Assert.AreEqual(RutaPrefab(variante), PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(enemigo.gameObject), esperado.Nombre);
                Assert.AreEqual(new Vector2(esperado.X, esperado.Y + 0.05f), (Vector2)enemigo.transform.position, esperado.Nombre);

                var ruta = new SerializedObject(enemigo).FindProperty("waypoints");
                Assert.AreEqual(2, ruta.arraySize, esperado.Nombre);
                Assert.AreEqual(esperado.Desde, (ruta.GetArrayElementAtIndex(0).objectReferenceValue as Transform).position.x, 0.01f);
                Assert.AreEqual(esperado.Hasta, (ruta.GetArrayElementAtIndex(1).objectReferenceValue as Transform).position.x, 0.01f);
            }
        }

        [Test]
        public void ElNivel3_TieneLosCheckpoints_ConElEcoDeMemoriaEnLaTrinchera_LaSalida_YLaZonaMortal()
        {
            var checkpoints = Todos<Checkpoint>(_nivel3).OrderBy(c => c.transform.position.x).ToList();
            Assert.AreEqual(DisenoNivel3.Checkpoints.Length, checkpoints.Count);
            for (var i = 0; i < checkpoints.Count; i++)
            {
                Assert.AreEqual($"L3_CP{i + 1}", checkpoints[i].Id);
                Assert.AreEqual(DisenoNivel3.Checkpoints[i] + new Vector2(0f, 0.05f), (Vector2)checkpoints[i].transform.position, "De pie en su suelo.");
                Assert.IsTrue(checkpoints[i].GetComponent<Collider2D>().isTrigger);
                Assert.IsTrue(checkpoints[i].Cura, "Todos los checkpoints curan.");
            }

            var conEco = checkpoints.Where(c => c.EcoDeMemoria).ToList();
            Assert.AreEqual(1, conEco.Count);
            Assert.AreEqual("L3_CP1", conEco[0].Id, "En la trinchera: «Él se quedó quieto para que no lo encontraran».");

            var salidas = Todos<SalidaNivel>(_nivel3);
            Assert.AreEqual(1, salidas.Count);
            Assert.AreEqual(new Vector2(DisenoNivel3.Salida, DisenoNivel3.SueloArena), (Vector2)salidas[0].transform.position);

            var zonas = Todos<DeadZone>(_nivel3);
            Assert.AreEqual(1, zonas.Count);
            Assert.Less(Rectangulo((BoxCollider2D)zonas[0].GetComponent<Collider2D>()).yMax, DisenoNivel3.PisadaDe(DisenoNivel3.LaTrampa).Y - 2f,
                "La zona mortal queda muy por debajo de la hondonada: caer en ella no mata.");
        }

        [Test]
        public void ElNivel3_TieneLaArenaConElEspectroDelPavor()
        {
            var arenas = Todos<ArenaJefe>(_nivel3);
            Assert.AreEqual(1, arenas.Count);
            var arena = arenas[0];

            Assert.IsInstanceOf<EspectroDelPavor>(arena.Jefe, "El jefe del Miedo es el Espectro del Pavor.");
            Assert.AreEqual(DisenoNivel3.LimitesArena, arena.Limites);
            Assert.AreEqual(DisenoNivel3.Jefe, arena.Jefe.transform.position.x, 0.01f);
            Assert.AreEqual(DisenoNivel3.Entrada, arena.transform.position.x, 0.01f);
            Assert.IsTrue(arena.GetComponent<Collider2D>().isTrigger);

            var puertas = new SerializedObject(arena).FindProperty("puertas");
            Assert.AreEqual(1, puertas.arraySize);
            var puerta = puertas.GetArrayElementAtIndex(0).objectReferenceValue as GameObject;
            Assert.IsNotNull(puerta);
            Assert.IsFalse(puerta.activeSelf, "Abierta hasta que empieza el combate.");

            CollectionAssert.AreEquivalent(DisenoNivel3.PlataformasArena.Select(p => p.Nombre).ToList(), arena.Plataformas.Select(p => p.name).ToList(),
                "El jefe atraviesa las dos ramas de la arena.");
        }

        [Test]
        public void LasSeccionesDelNivel_SonLasDelDiseno_YLaCamaraLlegaAlFondoDeLaArena()
        {
            Assert.AreEqual(DisenoNivel3.Secciones.Length, _datos.secciones.Count);
            for (var i = 0; i < _datos.secciones.Count; i++)
            {
                Assert.AreEqual(DisenoNivel3.Secciones[i].x, _datos.secciones[i].desdeX, 1e-4f, _datos.secciones[i].nombre);
                Assert.AreEqual(DisenoNivel3.Secciones[i].y, _datos.secciones[i].hastaX, 1e-4f, _datos.secciones[i].nombre);
                Assert.AreEqual(DisenoNivel3.TextosDeLasSecciones[i].nombre, _datos.secciones[i].nombre);
            }

            var margen = new SerializedObject(Todos<LevelManager>(_nivel3).Single()).FindProperty("margenCamara").floatValue;
            Assert.AreEqual(DisenoNivel3.MargenCamara, margen, 1e-4f);
            var fondo = DisenoNivel3.Paredes.Single(p => p.Nombre == "ParedArena");
            Assert.AreEqual(fondo.Hasta, _datos.Extension.y + margen, 1e-4f, "La cámara llega hasta la pared del fondo de la arena.");
        }

        [Test]
        public void LaOscuridad_LlevaLaLuzGlobal_ConLaLuzDeCadaTramo_YLaEscenaSeEditaConLuz()
        {
            var globales = Todos<Light2D>(_nivel3).Where(l => l.lightType == Light2D.LightType.Global).ToList();
            Assert.AreEqual(1, globales.Count, "Una sola luz global.");
            Assert.AreEqual(1f, globales[0].intensity, 1e-4f, "En el Editor, con luz: la oscuridad la pone el LevelManager al jugar.");

            var oscuridad = Todos<LuzDelNivel>(_nivel3);
            Assert.AreEqual(1, oscuridad.Count);
            Assert.AreSame(globales[0], oscuridad[0].LuzGlobal, "Lleva la luz global de la escena.");
            Assert.AreEqual(DisenoNivel3.Luces.Length, oscuridad[0].Tramos.Count);
            for (var i = 0; i < DisenoNivel3.Luces.Length; i++)
            {
                var esperada = DisenoNivel3.Luces[i];
                var tramo = oscuridad[0].Tramos[i];
                Assert.AreEqual(esperada.Nombre, tramo.nombre);
                Assert.AreEqual(esperada.Desde, tramo.desdeX, 1e-4f, esperada.Nombre);
                Assert.AreEqual(esperada.Hasta, tramo.hastaX, 1e-4f, esperada.Nombre);
                Assert.AreEqual(esperada.Intensidad, tramo.intensidad, 1e-4f, esperada.Nombre);
                Assert.AreEqual(esperada.Color, tramo.color, esperada.Nombre);
            }
        }

        [Test]
        public void LasAntorchas_YLosCepos_EstanDondeDiceElDiseno()
        {
            var antorchas = Todos<Antorcha>(_nivel3).OrderBy(a => a.transform.position.x).ToList();
            Assert.AreEqual(DisenoNivel3.Antorchas.Length, antorchas.Count);
            for (var i = 0; i < antorchas.Count; i++)
            {
                Assert.AreEqual(DisenoNivel3.Antorchas[i], (Vector2)antorchas[i].transform.position, $"Antorcha {i + 1}, de pie en su suelo.");
                Assert.AreEqual(RutaMecanica(GeneradorMecanicas.Antorcha), PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(antorchas[i].gameObject));
                Assert.AreEqual(DisenoNivel3.RadioAntorcha, antorchas[i].radio, 1e-4f);
                Assert.AreEqual(DisenoNivel3.DuracionAntorcha, antorchas[i].duracion, 1e-4f);
                Assert.IsFalse(antorchas[i].Luz.enabled, "Apagada hasta que Kael la enciende.");
            }

            var cepos = Todos<Cepo>(_nivel3).OrderBy(c => c.transform.position.x).ToList();
            Assert.AreEqual(DisenoNivel3.Cepos.Length, cepos.Count);
            for (var i = 0; i < cepos.Count; i++)
            {
                Assert.AreEqual(DisenoNivel3.Cepos[i], (Vector2)cepos[i].transform.position, $"Cepo {i + 1}, en el suelo.");
                Assert.AreEqual(RutaMecanica(GeneradorMecanicas.Cepo), PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(cepos[i].gameObject));
            }
        }

        [Test]
        public void LasRamasPodridas_Ceden_YLasCintasMarcanLaRutaBuena()
        {
            var podridas = Todos<RamaPodrida>(_nivel3);
            CollectionAssert.AreEquivalent(DisenoNivel3.RamasPodridas.Select(r => r.Nombre).ToList(), podridas.Select(r => r.name).ToList());
            foreach (var rama in podridas)
            {
                Assert.AreEqual(RutaMecanica(GeneradorMecanicas.RamaPodrida), PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(rama.gameObject));
                Assert.IsNotNull(rama.GetComponent<PlataformaInestable>(), $"{rama.name} cede.");
            }

            var cajas = Todos<BoxCollider2D>(_nivel3);
            Assert.IsTrue(DisenoNivel3.Ramas.All(r => cajas.Single(c => c.name == r.Nombre).GetComponent<PlataformaInestable>() == null),
                "Las ramas sanas no ceden.");
            var colorSana = Todos<SpriteRenderer>(_nivel3).First(s => s.name == DisenoNivel3.Ramas[0].Nombre).color;
            Assert.AreEqual(colorSana, podridas[0].ColorSana, "A oscuras, una rama podrida es del color de las sanas.");
            if (podridas[0].ConArte)
            {
                var sana = Todos<SpriteRenderer>(_nivel3).First(s => s.name == PreparadorNivel.NombreDibujo(DisenoNivel3.Ramas[0].Nombre));
                Assert.AreEqual(sana.sprite, podridas[0].DibujoDe(ESTADOS_PLATAFORMA.INTACT, false, 0f, 1f),
                    "Con arte, a oscuras una rama podrida es el dibujo de las sanas.");
            }

            var recorrido = PreparadorNivel.RaizRecorrido(_nivel3);
            var cintas = recorrido.Find("Cintas");
            Assert.IsNotNull(cintas);
            Assert.AreEqual(DisenoNivel3.Cintas.Length, cintas.childCount);
            for (var i = 0; i < DisenoNivel3.Cintas.Length; i++)
            {
                var cinta = cintas.GetChild(i);
                Assert.IsNull(cinta.GetComponent<Collider2D>(), "Las cintas no se chocan.");
                var dibujo = cinta.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(dibujo);
                Assert.AreEqual(DisenoNivel3.Cintas[i].x, cinta.position.x, 1e-3f);
                if (cinta.GetComponent<BucleDeSprites>() != null)
                {
                    // Con arte, atada por el nudo en el centro de su rama, al viento.
                    Assert.AreEqual(DisenoNivel3.Cintas[i].y - DisenoNivel3.GrosorRama * 0.5f, cinta.position.y, 1e-3f, "Atada a su rama.");
                }
                else
                {
                    Assert.AreEqual(DisenoNivel3.Cintas[i].y + 0.05f, cinta.position.y + cinta.lossyScale.y * 0.5f, 1e-3f, "Cuelga de lo alto de su rama.");
                }

                Assert.AreEqual(ConstructorEcoIra.RutaMaterialSprite, AssetDatabase.GetAssetPath(dibujo.sharedMaterial), "Solo se ve con luz.");
            }
        }

        [Test]
        public void ElClaro_TieneSusTresArboles_SinCollider_YElAcechadorEmbisteDentroDeEl()
        {
            var arboles = Todos<Arbol>(_nivel3).OrderBy(a => a.transform.position.x).ToList();
            Assert.AreEqual(DisenoNivel3.Arboles.Length, arboles.Count);
            for (var i = 0; i < arboles.Count; i++)
            {
                var arbol = arboles[i];
                Assert.AreEqual(new Vector2(DisenoNivel3.Arboles[i], DisenoNivel3.PisadaDe("Claro").Y), (Vector2)arbol.transform.position, "De pie en el claro.");
                Assert.AreEqual(DisenoNivel3.TamanoArbol, arbol.Tamano);
                Assert.IsEmpty(arbol.GetComponentsInChildren<Collider2D>(), "Kael pasa por delante: no tiene collider.");
                var tronco = arbol.GetComponentInChildren<SpriteRenderer>();
                Assert.IsNotNull(tronco, "Su tronco: con arte, el tocón del árbol muerto; sin él, un rectángulo.");
                Assert.Less(tronco.sortingOrder, 0, "Por detrás de Kael y de los enemigos.");
            }

            var acechador = Todos<Acechador>(_nivel3).Single();
            Assert.AreEqual(DisenoNivel3.LimitesAcechador, acechador.limites, "Embiste dentro del claro.");
        }

        [Test]
        public void LosEspectrosEmpiezanDormidos_YLosCentinelasCiegosOyenConSuCono()
        {
            var espectros = Todos<Espectro>(_nivel3);
            Assert.AreEqual(DisenoNivel3.Enemigos.Count(e => e.Tipo == DisenoNivel3.TIPOS_ENEMIGO.ESPECTRO), espectros.Count);
            Assert.IsTrue(espectros.All(e => e.empiezaDormido), "Duermen hasta que una antorcha, Kael o un golpe los despierta.");
            Assert.IsTrue(espectros.All(e => !e.GetComponent<EnemyController>().enabled), "Dormidos, su controlador está apagado.");

            var centinelas = Todos<CentinelaCiego>(_nivel3);
            Assert.AreEqual(DisenoNivel3.Enemigos.Count(e => e.Tipo == DisenoNivel3.TIPOS_ENEMIGO.CENTINELA_CIEGO), centinelas.Count);
        }

        [Test]
        public void PrepararOtraVez_NoCambiaNada()
        {
            Assert.IsFalse(PreparadorNivel.Preparar(_nivel3, _datos), "Con la misma versión del diseño, la herramienta no toca nada.");
            Assert.IsFalse(_nivel3.isDirty);
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
