using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Enemies;
using Fragmentum.Levels.Escenario;
using Fragmentum.Levels.Mecanicas;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El arte del Mundo 3 (Miedo) montado en el juego: el proyecto pasa su verificador
    /// (Fragmentum → Niveles → Verificar el arte del Nivel 3) y cada comprobación encuentra lo que se
    /// le rompe a propósito; los enemigos, el Espectro del Pavor y las mecánicas llevan sus piezas
    /// (los Animators que siguen su estado, los dibujos volteados, la luciérnaga, el fragmento, la
    /// lluvia de hojas); el fondo se oscurece con la luz; y la escena del Nivel 3, vestida con el
    /// bosque. Lo roto son objetos en memoria: el arte del proyecto no se toca.
    /// </summary>
    public class ArteMundo3Tests
    {
        private static string Lista(IEnumerable<string> problemas) => string.Join("\n", problemas);

        private static void Contiene(List<string> problemas, string texto) =>
            Assert.IsTrue(problemas.Any(p => p.Contains(texto)), $"Se esperaba «{texto}» entre:\n{Lista(problemas)}");

        private static GameObject Prefab(string ruta)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, $"Falta {ruta}.");
            return prefab;
        }

        private static T Campo<T>(Object objeto, string campo) where T : Object =>
            new SerializedObject(objeto).FindProperty(campo).objectReferenceValue as T;

        // --- El verificador ---

        [Test]
        public void ElArteDelNivel3_PasaLaVerificacion()
        {
            Assert.IsTrue(VerificadorNivel3.HayArte, "El arte del Mundo 3 está en el proyecto.");
            var problemas = VerificadorNivel3.Problemas();
            CollectionAssert.IsEmpty(problemas, Lista(problemas));
        }

        [Test]
        public void TodosLosFotogramasDeLasTablas_Estan_YCadaPngTieneSuRegla()
        {
            var sprites = VerificadorNivel3.Sprites();
            foreach (var fotograma in VerificadorNivel3.FotogramasDeLasTablas())
            {
                CollectionAssert.Contains(sprites, fotograma);
            }

            foreach (var ruta in sprites)
            {
                Assert.IsNotNull(TablaArte.ReglaDe(ruta), $"{ruta} no tiene regla en TablaArte.");
            }

            // D3 del Espectro y Hoja2 no se usan (decidido el 2026-10-15): ni siquiera están.
            CollectionAssert.DoesNotContain(sprites, ConstructorEspectro.RutaSprite("d3"));
            CollectionAssert.DoesNotContain(sprites, ConstructorPavor.RutaHoja("Hoja2"));
        }

        [Test]
        public void LoQueSeRompe_SeNota()
        {
            Contiene(VerificadorNivel3.ProblemasDelFondo(null), "Falta el prefab del fondo");
            Contiene(VerificadorNivel3.ProblemasDelFondo(new GameObject("FondoSinNada")), "FondoConLaLuz");

            var hojas = ScriptableObject.CreateInstance<PatronHojas>();
            try
            {
                Contiene(VerificadorNivel3.ProblemasDeLasHojas(hojas), "no lleva el dibujo de sus franjas");
            }
            finally
            {
                Object.DestroyImmediate(hojas);
                foreach (var suelto in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include).Where(g => g.name == "FondoSinNada"))
                {
                    Object.DestroyImmediate(suelto);
                }
            }

            var copia = Object.Instantiate(Prefab(VerificadorNivel3.RutaPrefabPavor));
            try
            {
                Object.DestroyImmediate(copia.GetComponentInChildren<AnimacionJefe>(true));
                Contiene(VerificadorNivel3.ProblemasDelPavor(copia), "AnimacionJefe");
            }
            finally
            {
                Object.DestroyImmediate(copia);
            }
        }

        // --- Los enemigos ---

        [Test]
        public void ElEspectro_DuermeConSuArte_YNoSeTine()
        {
            var prefab = Prefab(ConstructorEspectro.RutaPrefab);
            Assert.IsFalse(prefab.GetComponent<EnemyController>().enabled, "Duerme: el controlador, apagado.");
            var espectro = prefab.GetComponent<Espectro>();
            Assert.IsNotNull(espectro);
            var serializado = new SerializedObject(espectro);
            Assert.AreEqual(Color.white, serializado.FindProperty("colorDormido").colorValue, "Con arte no se tiñe dormido…");
            Assert.AreEqual(Color.white, serializado.FindProperty("colorAtaque").colorValue, "…ni atacando: solo se funde.");
            Assert.AreEqual(ConstructorEspectro.Controlador, prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController);

            var datos = GeneradorEnemigos.CargarDatos(ConstructorEspectro.NombreDatos);
            Assert.AreEqual(Color.white, datos.tinte);
            Assert.AreEqual(ConstructorEcoIra.VelocidadAtaque, ConstructorEspectro.VelocidadAtaque, "Ataca al ritmo del Eco de la Ira, como el Lamento.");
            Assert.AreEqual(ConstructorLamento.DescansoEntreGolpes, datos.attackCooldown, 1e-4f, "Y descansa lo que ellos.");
            Assert.AreEqual(0.5f / ConstructorEcoIra.VelocidadAtaque, datos.inicioVentanaGolpe, 1e-4f, "Su aviso, a1, a 1,6×.");
            Assert.AreEqual(ConstructorEspectro.VelocidadAtaque,
                new SerializedObject(prefab.GetComponentInChildren<EnemigoAnimacion>(true)).FindProperty(nameof(EnemigoAnimacion.velocidadAtaque)).floatValue, 1e-4f);
            Assert.AreEqual(ConstructorEspectro.InicioDelGolpe, datos.inicioVentanaGolpe, 1e-4f);
            Assert.AreEqual(ConstructorEspectro.FinDelGolpe, datos.finVentanaGolpe, 1e-4f);
        }

        [Test]
        public void ElCentinelaCiego_YElAcechador_VanVolteados_YSuAnimatorSigueSuEstado()
        {
            var centinela = Prefab(ConstructorCentinelaCiego.RutaPrefab);
            var oido = centinela.GetComponent<CentinelaCiego>();
            Assert.IsNotNull(oido);
            Assert.AreEqual(ConstructorCentinelaCiego.Controlador, Campo<Animator>(oido, "animator").runtimeAnimatorController,
                "Su oído manda en su Animator.");
            Assert.IsTrue(centinela.GetComponentInChildren<SpriteRenderer>(true).flipX, "Dibujado mirando a la izquierda: volteado.");

            var acechador = Prefab(ConstructorAcechador.RutaPrefab);
            var caza = acechador.GetComponent<Acechador>();
            Assert.IsNotNull(caza);
            Assert.AreEqual(ConstructorAcechador.Controlador, Campo<Animator>(caza, "animator").runtimeAnimatorController);
            Assert.IsTrue(acechador.GetComponentInChildren<SpriteRenderer>(true).flipX);
            Assert.AreEqual(CapsuleDirection2D.Horizontal, acechador.GetComponent<CapsuleCollider2D>().direction, "Un cuerpo largo y bajo.");
            Assert.IsNotNull(Campo<Hitbox>(caza, "embestida"), "Su embestida.");

            // Cada estado de su oído (o de su caza) tiene su estado en el Animator.
            var estados = ConstructorCentinelaCiego.Controlador.layers[0].stateMachine.states.Select(s => s.state.name).ToList();
            foreach (var (_, animacion) in ConstructorCentinelaCiego.Oido)
            {
                CollectionAssert.Contains(estados, animacion);
            }

            estados = ConstructorAcechador.Controlador.layers[0].stateMachine.states.Select(s => s.state.name).ToList();
            foreach (var (_, animacion) in ConstructorAcechador.Caza)
            {
                CollectionAssert.Contains(estados, animacion);
            }
        }

        // --- El Espectro del Pavor ---

        [Test]
        public void ElPavor_TieneUnEstadoPorGesto_YSeVisteConSuArte()
        {
            var controlador = ConstructorPavor.Controlador;
            Assert.IsNotNull(controlador);
            var estados = controlador.layers[0].stateMachine.states.Select(s => s.state.name).ToList();
            foreach (GESTOS_PAVOR gesto in System.Enum.GetValues(typeof(GESTOS_PAVOR)))
            {
                CollectionAssert.Contains(estados, gesto.ToString(), $"{gesto}: lo pide EspectroDelPavor por su nombre.");
            }

            var prefab = Prefab(VerificadorNivel3.RutaPrefabPavor);
            var pavor = prefab.GetComponent<EspectroDelPavor>();
            var dibujo = prefab.GetComponentInChildren<SpriteRenderer>(true);
            Assert.IsTrue(dibujo.flipX, "Dibujado mirando a la izquierda: volteado.");
            Assert.AreEqual(GeneradorJefes.RutaMaterialSinLuz, AssetDatabase.GetAssetPath(dibujo.sharedMaterial), "A oscuras, su destello se ve.");
            Assert.IsNotNull(prefab.GetComponentInChildren<AnimacionJefe>(true), "Grita y cae con su dibujo.");
            Assert.AreEqual(2, pavor.FotogramasLuciernaga.Count, "La luciérnaga dibujada.");
            Assert.IsNotNull(pavor.Fragmento, "Deja el fragmento del Miedo, sobre el montón de hojas.");

            var datos = GeneradorJefes.CargarDatos(ConstructorPavor.NombreDatos);
            Assert.AreEqual(Color.white, datos.tinte, "Sin tinte: los colores son del dibujo.");
            Assert.AreEqual(ConstructorPavor.DuracionDerrota, datos.duracionDerrota, 1e-4f, "La derrota dura su muerte hasta M5.");

            var hojas = AssetDatabase.LoadAssetAtPath<PatronHojas>($"{GeneradorJefes.CarpetaPatrones}/Hojas.asset");
            Assert.AreEqual(ConstructorPavor.PrefabHojas, hojas.efecto, "Cada franja de hojas va dibujada.");
            Assert.IsNotNull(ConstructorPavor.PrefabHojas.GetComponent<LluviaDeHojas>());
            Assert.IsNotNull(ConstructorPavor.PrefabHojas.GetComponent<DibujoDeZona>(), "Sigue las fases de su zona.");
        }

        [Test]
        public void ElFragmentoDelPavor_FlotaSobreElMontonDeHojas()
        {
            var medida = ConstructorPavor.MedirFragmento();
            Assert.IsNotNull(medida, "En M5 hay un fragmento suelto, encima del montón.");
            var (centro, tamano) = medida.Value;
            Assert.Greater(centro.y, 1f, "Flota por encima del montón.");
            Assert.Less(tamano.x, 1f, "Es una gema, no el montón.");

            // Y brilla: una copia de sus píxeles que recibe luz, encima del dibujado, y su luz.
            var pavor = Prefab(VerificadorNivel3.RutaPrefabPavor).GetComponent<EspectroDelPavor>();
            var brillo = pavor.Fragmento.GetComponent<BrilloDeFragmento>();
            Assert.IsNotNull(brillo, "Azul oscuro sobre el montón, apenas se veía.");
            Assert.AreEqual(centro.y, brillo.Copia.transform.localPosition.y, 1e-4f, "La copia, sobre el fragmento dibujado.");
            var dibujo = pavor.GetComponentInChildren<SpriteRenderer>(true);
            Assert.Greater(brillo.Copia.sortingOrder, dibujo.sortingOrder, "Por delante del dibujo del jefe.");
            Assert.AreNotEqual(GeneradorJefes.RutaMaterialSinLuz, AssetDatabase.GetAssetPath(brillo.Copia.sharedMaterial), "La copia recibe la luz.");
            Assert.AreEqual(ConstructorPavor.ColorBrilloFragmento, brillo.Luz.color);
            Assert.IsTrue(ConstructorPavor.AlumbraLaCapa(brillo.Luz, brillo.Copia.sortingLayerID),
                "Su luz alumbra la capa de la copia: sin capas, no alumbraba nada.");
            Assert.IsTrue(ConstructorPavor.AlumbraLaCapa(brillo.Luz, SortingLayer.NameToID("Default")), "Y la del suelo y el fondo.");
            Assert.IsNotNull(brillo.Destello, "Y destella: la luz 2D no aclara la gema por encima de sus colores.");
            Assert.AreEqual(ConstructorEcoIra.RutaMaterialDestello, AssetDatabase.GetAssetPath(brillo.Destello.sharedMaterial), "Su silueta, como el destello de los golpes.");
            Assert.Greater(brillo.Destello.sortingOrder, brillo.Copia.sortingOrder, "Encima de la copia.");
            Assert.Greater(brillo.destelloMaximo, 0.3f, "Que se note.");
            Assert.Greater(brillo.intensidadMaxima, brillo.intensidadMinima, "Late.");
            Assert.AreEqual(ConstructorCentinela.CajaDelFragmento(ConstructorPavor.RutaSprite(ConstructorPavor.ClipDe(ConstructorPavor.Muerte).Fotogramas.Last())).Value.caja,
                new SerializedObject(brillo).FindProperty("caja").rectIntValue, "Recorta su fragmento de M5.");
        }

        // --- Las mecánicas ---

        [Test]
        public void LaAntorcha_LlevaSuDibujo_YSuAnimatorSigueSuEstado()
        {
            var prefab = Prefab($"{GeneradorMecanicas.Carpeta}/{GeneradorMecanicas.Antorcha}.prefab");
            var antorcha = prefab.GetComponent<Antorcha>();
            Assert.AreEqual(ConstructorMecanicasMundo3.ControladorAntorcha, Campo<Animator>(antorcha, "animator").runtimeAnimatorController);
            Assert.IsNull(Campo<SpriteRenderer>(antorcha, "cabeza"), "Con arte, nada se tiñe: la llama es su dibujo.");

            var estados = ConstructorMecanicasMundo3.ControladorAntorcha.layers[0].stateMachine.states.Select(s => s.state.name).ToList();
            CollectionAssert.AreEquivalent(ConstructorMecanicasMundo3.MecanicaAntorcha.Clips.Select(c => c.Estado), estados);

            // La cabeza de la antorcha, donde su luz.
            var apagada = ConstructorMecanicasMundo3.AntorchaApagada;
            Assert.AreEqual(GeneradorMecanicas.AlturaLlama, (85f - 27f) / apagada.pixelsPerUnit, 0.1f, "La cabeza, a la altura de su luz.");
        }

        [Test]
        public void LaRama_SeAlargaPorSuCentro_YMideLoQueLaPodrida()
        {
            var rama = ConstructorMecanicasMundo3.RamaSana;
            Assert.IsNotNull(rama);
            Assert.AreEqual(new Vector4(ConstructorMecanicasMundo3.BordeIzquierdoRama, 0f, ConstructorMecanicasMundo3.BordeDerechoRama, 0f), rama.border,
                "Sus puntas no se repiten.");
            var cuerpo = (rama.rect.width - 2f * ConstructorMecanicasMundo3.MargenRama) / rama.pixelsPerUnit;
            Assert.AreEqual(GeneradorMecanicas.TamanoRama.x, cuerpo, 0.02f, "La rama dibujada mide lo que la podrida: 2 u.");
            Assert.AreEqual(2f + 2f * ConstructorMecanicasMundo3.MargenRama / ConstructorMecanicasMundo3.PixelesPorUnidadRama,
                ConstructorMecanicasMundo3.AnchoDibujoRama(2f), 1e-4f);
        }

        // --- El fondo ---

        [Test]
        public void ElFondo_SeOscureceConLaLuzDelNivel()
        {
            Assert.AreEqual(new Color(0.08f, 0.08f, 0.08f, 1f), FondoConLaLuz.TinteCon(Color.white, 0.08f), "En el bosque cerrado, casi no se ve.");
            Assert.AreEqual(new Color(0.35f, 0.375f, 0.5f, 1f), FondoConLaLuz.TinteCon(new Color(0.7f, 0.75f, 1f), 0.5f), "Con la luna, frío y a medias.");
            Assert.AreEqual(Color.white, FondoConLaLuz.TinteCon(Color.white, 1.4f), "Nunca más que entero.");

            var prefab = Prefab(ConstructorFondoMundo3.RutaPrefab);
            Assert.IsNotNull(prefab.GetComponent<FondoConLaLuz>());
            Assert.AreEqual(ConstructorFondoMundo3.CapasFondo.Length, prefab.GetComponentsInChildren<Renderer>(true).Length);

            // Las capas de la vista siguen a la cámara; los helechos, en su sitio del mundo.
            var grupos = prefab.GetComponentsInChildren<ParallaxMovement>(true);
            Assert.AreEqual(ConstructorFondoMundo3.NombreVista, grupos.Single(g => g.SigueCamaraEnY).name);
            var alSuelo = grupos.Single(g => !g.SigueCamaraEnY);
            Assert.AreEqual(ConstructorFondoMundo3.NombreAlSuelo, alSuelo.name);
            var helechos = alSuelo.transform.Find(ConstructorFondoMundo3.Helechos.Nombre);
            Assert.IsNotNull(helechos, "Bajo las copas no se ven en el aire: van anclados al suelo del bosque.");
            Assert.AreEqual(ConstructorFondoMundo3.Helechos.Arriba, helechos.localPosition.y + helechos.localScale.y * 0.5f, 1e-4f);
            Assert.Less(helechos.localPosition.y - helechos.localScale.y * 0.5f, -4f, "Su borde de abajo no asoma en la hondonada (y = −4).");
        }

        // --- La escena ---

        [Test]
        public void ElNivel3_SeVisteConElBosque()
        {
            var escena = EditorSceneManager.OpenScene(GeneradorProgresion.RutaEscenaNivel3, OpenSceneMode.Additive);
            try
            {
                var recorrido = PreparadorNivel.RaizRecorrido(escena);
                var escenario = recorrido.Find(PreparadorNivel.NombreEscenario);
                Assert.IsNotNull(escenario, "Con el tileset cortado, el recorrido lleva su escenario.");

                var bloques = recorrido.GetComponentsInChildren<BoxCollider2D>(true).Where(c => !c.isTrigger).ToList();
                foreach (var b in DisenoNivel3.Suelos.Concat(DisenoNivel3.Paredes).Concat(DisenoNivel3.Ramas).Concat(DisenoNivel3.PlataformasArena))
                {
                    Assert.IsNotNull(escenario.Find(PreparadorNivel.NombreDibujo(b.Nombre)), $"{b.Nombre}: lleva su dibujo.");
                    var liso = bloques.Single(c => c.name == b.Nombre).GetComponent<SpriteRenderer>();
                    Assert.IsFalse(liso.enabled, $"{b.Nombre}: su rectángulo de color, apagado.");
                }

                // Los Espectros, al tamaño de los Ecos del Nivel 1; los demás, al suyo.
                Assert.AreEqual(DisenoNivel1.EscalaEco, DisenoNivel3.EscalaEspectro);
                var enemigos = recorrido.Find("Enemigos");
                foreach (var guardia in DisenoNivel3.Enemigos)
                {
                    var enemigo = enemigos.Find(guardia.Nombre);
                    Assert.IsNotNull(enemigo, guardia.Nombre);
                    var esEspectro = guardia.Tipo == DisenoNivel3.TIPOS_ENEMIGO.ESPECTRO;
                    Assert.AreEqual(esEspectro ? DisenoNivel3.EscalaEspectro.x : 1f, enemigo.localScale.x, 1e-4f, guardia.Nombre);
                    Assert.AreEqual(esEspectro ? DisenoNivel3.EscalaEspectro.y : 1f, enemigo.localScale.y, 1e-4f, guardia.Nombre);
                }

                foreach (var decorado in DisenoNivel3.Decorados)
                {
                    var dibujo = escenario.Find(decorado.Nombre);
                    Assert.IsNotNull(dibujo, $"Falta el atrezo {decorado.Nombre}.");
                    Assert.IsNull(dibujo.GetComponent<Collider2D>(), $"{decorado.Nombre}: no se pisa ni choca.");
                }

                Assert.IsTrue(escena.GetRootGameObjects().Any(r => r.name == "FondoMundo3"), "Lleva el fondo del bosque.");
                foreach (var arbol in recorrido.GetComponentsInChildren<Arbol>(true))
                {
                    Assert.AreEqual(ConstructorEscenarioMundo3.Cargar(ConstructorEscenarioMundo3.Tocon), arbol.GetComponentInChildren<SpriteRenderer>().sprite,
                        "Los árboles del claro: el tocón del árbol muerto.");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
