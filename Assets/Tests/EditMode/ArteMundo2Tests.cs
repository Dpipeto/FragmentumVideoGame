using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Data;
using Fragmentum.EditorTools;
using Fragmentum.Enemies;
using Fragmentum.Levels.Escenario;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El arte del Mundo 2 (Tristeza) montado en el juego: el proyecto pasa su verificador
    /// (Fragmentum → Niveles → Verificar el arte del Nivel 2) y cada comprobación encuentra lo que
    /// se le rompe a propósito; las cuentas con las que se dibujan las franjas de la Doncella, la
    /// burbuja del aire y los bucles; y la escena del Nivel 2 vestida con el tileset, con su fondo.
    /// Lo roto son objetos en memoria: el arte del proyecto no se toca.
    /// </summary>
    public class ArteMundo2Tests
    {
        private static string Lista(IEnumerable<string> problemas) => string.Join("\n", problemas);

        private static void Contiene(List<string> problemas, string texto) =>
            Assert.IsTrue(problemas.Any(p => p.Contains(texto)), $"Se esperaba «{texto}» entre:\n{Lista(problemas)}");

        // --- El verificador ---

        [Test]
        public void ElArteDelNivel2_PasaLaVerificacion()
        {
            Assert.IsTrue(VerificadorNivel2.HayArte, "El arte del Mundo 2 está en el proyecto.");
            var problemas = VerificadorNivel2.Problemas();
            CollectionAssert.IsEmpty(problemas, Lista(problemas));

            LogAssert.Expect(LogType.Log, new Regex(@"\[Nivel 2\] Verificación correcta: 117 sprites, 37 clips, 8 Animators, 10 prefabs"));
            Assert.IsTrue(VerificadorNivel2.Verificar());
        }

        [Test]
        public void TodosLosFotogramasDeLasTablas_Estan_YCadaPngTieneSuRegla()
        {
            var sprites = VerificadorNivel2.Sprites();
            foreach (var fotograma in VerificadorNivel2.FotogramasDeLasTablas())
            {
                CollectionAssert.Contains(sprites, fotograma);
            }

            foreach (var ruta in sprites)
            {
                Assert.IsNotNull(TablaArte.ReglaDe(ruta), $"{ruta} no tiene regla en TablaArte.");
            }
        }

        [Test]
        public void UnSpriteReimportadoSinSusAjustes_SeNota()
        {
            var ruta = ConstructorLamento.RutaSprite(ConstructorLamento.Clips[0].Fotogramas[0]);
            var problemas = VerificadorNivel1.ProblemasDeSprite(ruta, null);
            Assert.IsNotEmpty(problemas, "Sin importador (el PNG no está) hay problema.");

            Contiene(VerificadorNivel1.ProblemasDeSprite("Assets/Sprites/Enemigos/Lamento/Otra/Nuevo.png", null), "Nuevo");
        }

        [Test]
        public void ElAtaqueDeLaEscultura_VolteaSoloLosFotogramasDibujadosAlReves()
        {
            var ataque = AssetDatabase.LoadAssetAtPath<AnimationClip>(ConstructorEscultura.RutaClip(ConstructorEscultura.Ataque));
            Assert.IsNotNull(ataque);
            CollectionAssert.IsEmpty(VerificadorNivel2.ProblemasDelVolteo(ataque));

            // Una copia sin la curva: golpearía hacia atrás.
            var copia = Object.Instantiate(ataque);
            try
            {
                copia.name = "Escultura_Ataque";
                AnimationUtility.SetEditorCurve(copia, ConstructorEscultura.CurvaVolteo, null);
                Contiene(VerificadorNivel2.ProblemasDelVolteo(copia), "no voltea el dibujo");

                // Y otra que voltea el ataque entero: los fotogramas que miran bien saldrían al revés.
                AnimationUtility.SetEditorCurve(copia, ConstructorEscultura.CurvaVolteo, AnimationCurve.Constant(0f, copia.length, 1f));
                var problemas = VerificadorNivel2.ProblemasDelVolteo(copia);
                Contiene(problemas, "A1 va volteado y no debe");
                Assert.IsFalse(problemas.Any(p => p.Contains("A3")), Lista(problemas));
            }
            finally
            {
                Object.DestroyImmediate(copia);
            }
        }

        [Test]
        public void UnArteDeFranjasIncompleto_UnaBurbujaCorta_YUnaOlaSinEfecto_SeNotan()
        {
            Contiene(VerificadorNivel2.ProblemasDeLasFranjas(null), "Falta el arte de las franjas");
            var franjas = ScriptableObject.CreateInstance<ArteDeFranja>();
            var interfaz = ScriptableObject.CreateInstance<ArteInterfaz>();
            var patron = ScriptableObject.CreateInstance<PatronOla>();
            try
            {
                Contiene(VerificadorNivel2.ProblemasDeLasFranjas(franjas), "le falta algún dibujo");

                Contiene(VerificadorNivel2.ProblemasDeLaBurbuja(null), "Falta el arte de la interfaz");
                Contiene(VerificadorNivel2.ProblemasDeLaBurbuja(interfaz), "la burbuja del aire debe tener sus 5 fotogramas");
                var real = AssetDatabase.LoadAssetAtPath<ArteInterfaz>(ConstructorMecanicasMundo2.RutaArteInterfaz);
                interfaz.oxigeno = real.oxigeno.Take(3).ToArray();
                Contiene(VerificadorNivel2.ProblemasDeLaBurbuja(interfaz), "5 fotogramas");

                patron.name = "OlaDePrueba";
                var ola = ConstructorMecanicasMundo2.PrefabOla;
                Assert.IsNotNull(ola);
                Contiene(VerificadorNivel2.ProblemasDeLasOlas(new[] { patron }, ola), "Patrón OlaDePrueba: su ola no usa OlaDeAgua.prefab");
                patron.efecto = ola;
                CollectionAssert.IsEmpty(VerificadorNivel2.ProblemasDeLasOlas(new[] { patron }, ola));
                Contiene(VerificadorNivel2.ProblemasDeLasOlas(new PatronOla[0], ola), "No hay ningún patrón de ola");
            }
            finally
            {
                Object.DestroyImmediate(franjas);
                Object.DestroyImmediate(interfaz);
                Object.DestroyImmediate(patron);
            }

            Assert.AreEqual(2, VerificadorNivel2.PatronesDeOla().Count, "La ola y la ola doble de la Doncella.");
        }

        [Test]
        public void UnaDoncellaSinSuCola_SinSuDestello_OSinSuFragmento_SeNota()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VerificadorNivel2.RutaPrefabDoncella);
            Assert.IsNotNull(prefab);
            CollectionAssert.IsEmpty(VerificadorNivel2.ProblemasDeLaDoncella(prefab));

            var copia = Object.Instantiate(prefab);
            try
            {
                copia.name = "DoncellaRota";
                Object.DestroyImmediate(copia.GetComponentInChildren<Fragmentum.Bosses.ColaFundida>(true));
                Object.DestroyImmediate(copia.GetComponentInChildren<Fragmentum.Bosses.FragmentoJefe>(true));
                var serializado = new SerializedObject(copia.GetComponent<Fragmentum.Bosses.DoncellaLamento>());
                serializado.FindProperty("destelloDeHielo").objectReferenceValue = null;
                serializado.ApplyModifiedPropertiesWithoutUndo();

                var problemas = VerificadorNivel2.ProblemasDeLaDoncella(copia);
                Contiene(problemas, "no funde la cola");
                Contiene(problemas, "destello de hielo");
                Contiene(problemas, "no deja su fragmento");
            }
            finally
            {
                Object.DestroyImmediate(copia);
            }
        }

        [Test]
        public void ElFondo_TieneUnGrupoQueSigueALaCamara_YOtroAncladoBajoElAgua()
        {
            Contiene(VerificadorNivel2.ProblemasDelFondo(null), "Falta el prefab del fondo");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorFondoMundo2.RutaPrefab);
            Assert.IsNotNull(prefab);
            CollectionAssert.IsEmpty(VerificadorNivel2.ProblemasDelFondo(prefab));

            var grupos = prefab.GetComponentsInChildren<ParallaxMovement>(true);
            Assert.AreEqual(2, grupos.Length);
            Assert.AreEqual(ConstructorFondoMundo2.NombreVista, grupos.Single(g => g.SigueCamaraEnY).name);
            Assert.AreEqual(ConstructorFondoMundo2.NombreBajoElAgua, grupos.Single(g => !g.SigueCamaraEnY).name);

            var sinAnclar = Object.Instantiate(prefab);
            try
            {
                Object.DestroyImmediate(sinAnclar.GetComponentsInChildren<ParallaxMovement>(true).Single(g => !g.SigueCamaraEnY));
                Contiene(VerificadorNivel2.ProblemasDelFondo(sinAnclar), "otro anclado bajo el agua");
            }
            finally
            {
                Object.DestroyImmediate(sinAnclar);
            }
        }

        [Test]
        public void UnaCinematicaDeLaDoncellaFueraDeSuRejilla_OSinUnPlano_SeNota()
        {
            CollectionAssert.IsEmpty(VerificadorNivel2.ProblemasDeLasCinematicas());
            var planos = new[] { ConstructorCinematicasNivel2.Presentacion1, ConstructorCinematicasNivel2.Presentacion2 };
            Contiene(VerificadorNivel2.ProblemasDeLaCinematica(null, ConstructorCinematicasNivel2.RutaPresentacion, planos), "Falta la cinemática Cinematica_Jefe_Tristeza");

            var real = AssetDatabase.LoadAssetAtPath<Fragmentum.Narrative.CutsceneData>(ConstructorCinematicasNivel2.RutaPresentacion);
            var copia = Object.Instantiate(real);
            try
            {
                CollectionAssert.IsEmpty(VerificadorNivel2.ProblemasDeLaCinematica(copia, ConstructorCinematicasNivel2.RutaPresentacion, planos));

                copia.rejilla = Fragmentum.Narrative.CutsceneData.RejillaPorDefecto;
                copia.diapositivas[3].imagen = null;
                var problemas = VerificadorNivel2.ProblemasDeLaCinematica(copia, ConstructorCinematicasNivel2.RutaPresentacion, planos);
                Contiene(problemas, "no va en la rejilla de sus planos (480 × 268)");
                Contiene(problemas, "le falta el plano PresentacionDoncella2");
            }
            finally
            {
                Object.DestroyImmediate(copia);
            }
        }

        // --- Los Lamentos, los Ecos de este mundo ---

        [Test]
        public void ElLamento_GolpeaYDescansa_ComoElEcoDeLaIraTrasSuAjuste()
        {
            Assert.AreEqual(ConstructorEcoIra.VelocidadAtaque, ConstructorLamento.VelocidadAtaque);
            var animacion = ConstructorLamento.Prefab.GetComponentInChildren<EnemigoAnimacion>(true);
            Assert.AreEqual(ConstructorEcoIra.VelocidadAtaque, animacion.velocidadAtaque, 1e-4f,
                "Su clip de ataque va a la velocidad que se le dio al del Eco tras la primera prueba del Nivel 1.");

            var lamento = GeneradorEnemigos.CargarDatos(ConstructorLamento.NombreDatos);
            var eco = GeneradorEnemigos.CargarDatos(ConstructorEcoIra.NombreDatos);
            Assert.AreEqual(eco.attackCooldown, lamento.attackCooldown, 1e-4f, "Descansa entre golpes lo que el Eco.");
            Assert.AreEqual(ConstructorLamento.DescansoEntreGolpes, lamento.attackCooldown, 1e-4f);

            // Sus datos llevan los tiempos del clip ya acelerado: los eventos del golpe van con él.
            var ataque = ConstructorLamento.ClipDe(ConstructorLamento.Attack);
            Assert.AreEqual(ataque.Duracion / ConstructorEcoIra.VelocidadAtaque, lamento.duracionAtaque, 1e-4f);
            Assert.AreEqual(ataque.Inicio(1) / ConstructorEcoIra.VelocidadAtaque, lamento.inicioVentanaGolpe, 1e-4f);
            Assert.AreEqual(ataque.Inicio(3) / ConstructorEcoIra.VelocidadAtaque, lamento.finVentanaGolpe, 1e-4f);
            Assert.Less(lamento.inicioVentanaGolpe, 0.3f, "Avisa menos de un tercio de segundo.");

            Assert.Less(lamento.velocidadPersecucion, eco.velocidadPersecucion, "En lo demás sigue siendo lento: persigue sin prisa.");
            Assert.Greater(lamento.esperaEnWaypoint, eco.esperaEnWaypoint);
        }

        // --- Las cuentas del dibujo ---

        [Test]
        public void LasPiezasDeUnaFranja_SeRepartenDeBordeABorde()
        {
            CollectionAssert.AreEqual(new[] { -3f, 0f, 3f }, DibujoDeFranja.Repartir(9f, 3));
            CollectionAssert.AreEqual(new[] { 0f }, DibujoDeFranja.Repartir(9f, 0), "Al menos una.");

            // Las losas: la primera empieza en un borde y la última acaba en el otro.
            const float ancho = 8f;
            var losa = ConstructorMecanicasMundo2.AnchoLosa;
            var centros = DibujoDeFranja.RepartirLosas(ancho, losa);
            Assert.AreEqual(Mathf.CeilToInt(ancho / losa - 0.05f), centros.Length);
            Assert.AreEqual(-ancho * 0.5f, centros.First() - losa * 0.5f, 1e-4f);
            Assert.AreEqual(ancho * 0.5f, centros.Last() + losa * 0.5f, 1e-4f);
            // Sin huecos que se vean: se montan o, como mucho, se separan menos de un píxel de su arte.
            var pixel = 1f / ConstructorMecanicasMundo2.PixelesPorUnidadCapa;
            for (var i = 1; i < centros.Length; i++)
            {
                Assert.Less(centros[i] - centros[i - 1], losa + pixel, "Entre dos losas no cabe ni un píxel.");
            }

            CollectionAssert.AreEqual(new[] { 0f }, DibujoDeFranja.RepartirLosas(2f, 2.6f), "Una franja estrecha: una losa, centrada.");
        }

        [Test]
        public void LosDibujosDeUnaFranja_SiguenASuEstado()
        {
            var arte = ConstructorMecanicasMundo2.ArteFranja;
            Assert.IsNotNull(arte);
            Assert.IsTrue(arte.Completo);

            Assert.AreSame(arte.aviso[0], DibujoDeFranja.FotogramaDeAviso(arte, 0f));
            Assert.AreSame(arte.aviso[arte.aviso.Length - 1], DibujoDeFranja.FotogramaDeAviso(arte, 1f));
            Assert.AreSame(arte.aviso[arte.aviso.Length - 1], DibujoDeFranja.FotogramaDeAviso(arte, 7f), "No se sale de la tabla.");

            const float puas = 0.6f;
            Assert.IsNull(DibujoDeFranja.FotogramaDePuas(arte, ESTADOS_FRANJA.FLOODED, 1f, puas), "Con agua, sin púas.");
            Assert.AreSame(arte.puasAviso, DibujoDeFranja.FotogramaDePuas(arte, ESTADOS_FRANJA.FREEZING, 0.2f, puas), "La escarcha avisa.");
            Assert.AreSame(arte.puasBrota, DibujoDeFranja.FotogramaDePuas(arte, ESTADOS_FRANJA.FROZEN, 0f, puas));
            CollectionAssert.Contains(arte.puasQuietas, DibujoDeFranja.FotogramaDePuas(arte, ESTADOS_FRANJA.FROZEN, puas * 0.5f, puas));
            Assert.AreSame(arte.puasRompe, DibujoDeFranja.FotogramaDePuas(arte, ESTADOS_FRANJA.FROZEN, puas + arte.segundosRompe * 0.5f, puas));
            Assert.IsNull(DibujoDeFranja.FotogramaDePuas(arte, ESTADOS_FRANJA.FROZEN, puas + arte.segundosRompe + 0.01f, puas), "Rotas, ya no se ven.");

            Assert.IsNull(DibujoDeFranja.FotogramaDeCapa(arte, ESTADOS_FRANJA.FREEZING, 0.5f, 3f), "Todavía no hay capa.");
            Assert.AreSame(arte.capaSeForma, DibujoDeFranja.FotogramaDeCapa(arte, ESTADOS_FRANJA.FROZEN, 0f, 5f));
            Assert.AreSame(arte.capaEntera, DibujoDeFranja.FotogramaDeCapa(arte, ESTADOS_FRANJA.FROZEN, arte.segundosFormarse + 0.01f, 5f));
            Assert.AreSame(arte.capaSeDerrite, DibujoDeFranja.FotogramaDeCapa(arte, ESTADOS_FRANJA.FROZEN, 4f, arte.segundosDerretirse * 0.5f),
                "Avisa de que se va: le queda poco.");
        }

        [Test]
        public void LaBurbujaDelAire_VaDeLlenaAVacia()
        {
            Assert.AreEqual(0, ArteInterfaz.FotogramaDeAire(1f, 5), "Llena.");
            Assert.AreEqual(1, ArteInterfaz.FotogramaDeAire(0.75f, 5));
            Assert.AreEqual(2, ArteInterfaz.FotogramaDeAire(0.5f, 5));
            Assert.AreEqual(3, ArteInterfaz.FotogramaDeAire(0.25f, 5));
            Assert.AreEqual(4, ArteInterfaz.FotogramaDeAire(0f, 5), "A punto de romperse.");
            Assert.AreEqual(0, ArteInterfaz.FotogramaDeAire(3f, 5), "No se sale de la tabla.");
            Assert.AreEqual(4, ArteInterfaz.FotogramaDeAire(-1f, 5));
            Assert.AreEqual(-1, ArteInterfaz.FotogramaDeAire(0.5f, 0), "Sin fotogramas, ninguno.");

            var arte = ArteInterfaz.Cargar();
            Assert.IsNotNull(arte, "Está en Resources: el HUD lo carga al construirse.");
            Assert.IsTrue(arte.TieneOxigeno);
            Assert.AreEqual(ConstructorMecanicasMundo2.FotogramasOxigeno.Length, arte.oxigeno.Length);
        }

        [Test]
        public void UnBucleDeSprites_AvanzaUnFotogramaCadaTanto_YVuelveAEmpezar()
        {
            Assert.AreEqual(0, BucleDeSprites.Indice(0f, 4, 0.2f));
            Assert.AreEqual(0, BucleDeSprites.Indice(0.19f, 4, 0.2f));
            Assert.AreEqual(1, BucleDeSprites.Indice(0.21f, 4, 0.2f));
            Assert.AreEqual(3, BucleDeSprites.Indice(0.79f, 4, 0.2f));
            Assert.AreEqual(0, BucleDeSprites.Indice(0.81f, 4, 0.2f), "Vuelve a empezar.");
            Assert.AreEqual(3, BucleDeSprites.Indice(-0.1f, 4, 0.2f), "Con un adelanto negativo tampoco se sale.");
            Assert.AreEqual(-1, BucleDeSprites.Indice(1f, 0, 0.2f), "Sin fotogramas, ninguno.");
        }

        [Test]
        public void UnaCapaEnEspejo_TardaDosAnchosEnRepetirse()
        {
            // 10 u de textura, a la velocidad del mundo: la que se repite vuelve a empezar cada
            // 10 u de cámara; la de espejo, cada 20.
            float Normal(float camara) => CalculoParallax.OffsetTextura(camara, 1f, 10f, 20f, 1f);
            float Espejo(float camara) => CalculoParallax.OffsetTextura(camara, 1f, 10f, 20f, 2f);
            Assert.AreEqual(Normal(0f), Normal(10f), 1e-4f, "Una vuelta entera.");
            Assert.AreEqual(1f, Mathf.Abs(Espejo(0f) - Espejo(10f)), 1e-4f, "A un ancho, la de espejo va por su copia vuelta.");
            Assert.AreEqual(Espejo(0f), Espejo(20f), 1e-4f, "Y a los dos anchos vuelve a empezar.");
            Assert.AreEqual(Normal(3f), Espejo(3f) % 1f, 1e-4f, "Dentro de cada copia avanzan igual.");
        }

        // --- El tileset ---

        [Test]
        public void LasPiezasDelTileset_EstanCortadas_YElLadrilloMideUnaUnidad()
        {
            Assert.IsTrue(ConstructorEscenarioMundo2.HayArte);
            Assert.IsTrue(ConstructorEscenarioMundo2.Completo);
            CollectionAssert.IsEmpty(VerificadorNivel2.ProblemasDelTileset());

            var ladrillo = ConstructorEscenarioMundo2.Cargar(ConstructorEscenarioMundo2.Ladrillo);
            Assert.AreEqual(Vector2.one, (Vector2)ladrillo.bounds.size, "1 × 1 u: casa con los bloques, que empiezan en unidades enteras.");
            Assert.AreEqual(Vector2.zero, ladrillo.pivot, "Se repite desde la esquina de abajo a la izquierda.");

            var hielo = ConstructorEscenarioMundo2.Cargar(ConstructorEscenarioMundo2.Hielo);
            Assert.AreEqual(hielo.rect.height, hielo.pivot.y, 1e-3f, "Cuelga de su borde de arriba: es lo que se pisa.");

            var viga = ConstructorEscenarioMundo2.Cargar(ConstructorEscenarioMundo2.Viga);
            Assert.AreEqual(ConstructorEscenarioMundo2.Viga.BordeIzquierdo, viga.border.x, "Las patas no se repiten.");
            Assert.AreEqual(ConstructorEscenarioMundo2.Viga.BordeDerecho, viga.border.z);
            Assert.Less(viga.border.x + viga.border.z, 2f * ConstructorEscenarioMundo2.PixelesPorUnidadTiles,
                "Las dos patas caben en la plataforma más estrecha del nivel (2 u).");

            // El rectángulo de una pieza, con la y desde abajo como la quiere Unity.
            var caja = ConstructorEscenarioMundo2.Ladrillo.CajaEnLaHoja(ConstructorEscenarioMundo2.TamanoTiles.y);
            Assert.AreEqual(new Rect(111f, 267f - 5f - 32f, 32f, 32f), caja);
            Assert.AreEqual(caja, ladrillo.rect);
            Assert.AreEqual(new Vector2(0.5f, 0f), ConstructorEscenarioMundo2.Silla.PivoteNormalizado, "La silla se apoya.");
            Assert.AreEqual(new Vector2(0.5f, 1f), ConstructorEscenarioMundo2.Campana.PivoteNormalizado, "La campana cuelga.");

            // Todas las piezas, también las del atrezo: cortadas, a su tamaño y dentro de su hoja.
            var piezas = ConstructorEscenarioMundo2.Piezas;
            Assert.AreEqual(piezas.Length, piezas.Select(p => p.Nombre).Distinct().Count(), "Cada pieza, con su nombre.");
            foreach (var pieza in piezas)
            {
                var sprite = ConstructorEscenarioMundo2.Cargar(pieza);
                Assert.IsNotNull(sprite, $"Falta la pieza {pieza.Nombre}.");
                var hoja = pieza.Hoja == ConstructorEscenarioMundo2.RutaTiles ? ConstructorEscenarioMundo2.TamanoTiles : ConstructorEscenarioMundo2.TamanoProps;
                Assert.AreEqual(pieza.CajaEnLaHoja(hoja.y), sprite.rect, pieza.Nombre);
                Assert.That(pieza.Caja.xMax, Is.LessThanOrEqualTo(hoja.x), pieza.Nombre);
                Assert.That(pieza.Caja.yMax, Is.LessThanOrEqualTo(hoja.y), pieza.Nombre);
                Assert.AreEqual(pieza.Tamano.x, sprite.bounds.size.x, 1e-4f, $"{pieza.Nombre}: sin reescalar.");
                Assert.AreEqual(pieza.Tamano.y, sprite.bounds.size.y, 1e-4f, pieza.Nombre);
            }

            // Lo que se apoya tiene el pivote abajo (unas filas dentro del suelo); lo que cuelga, arriba.
            foreach (var pieza in new[] { ConstructorEscenarioMundo2.Estatua, ConstructorEscenarioMundo2.Farol, ConstructorEscenarioMundo2.Rocas,
                         ConstructorEscenarioMundo2.Mesa, ConstructorEscenarioMundo2.CampanaCaida, ConstructorEscenarioMundo2.ColumnaRota, ConstructorEscenarioMundo2.Algas })
            {
                Assert.That(pieza.PivoteNormalizado.y, Is.InRange(0f, 0.08f), $"{pieza.Nombre} se apoya.");
                Assert.AreEqual(0.5f, pieza.PivoteNormalizado.x, 1e-4f, pieza.Nombre);
            }

            foreach (var pieza in new[] { ConstructorEscenarioMundo2.Estandartes, ConstructorEscenarioMundo2.Cadena })
            {
                Assert.That(pieza.PivoteNormalizado.y, Is.InRange(0.9f, 1f), $"{pieza.Nombre} cuelga.");
            }
        }

        [Test]
        public void ACadaBloque_LeTocaSuPieza()
        {
            Assert.AreEqual(ConstructorEscenarioMundo2.Viga.Nombre, ConstructorEscenarioMundo2.PiezaDeBloque(0.5f, false).Nombre, "Una plataforma fina.");
            Assert.AreEqual(ConstructorEscenarioMundo2.Viga.Nombre, ConstructorEscenarioMundo2.PiezaDeBloque(0.5f, true).Nombre);
            Assert.AreEqual(ConstructorEscenarioMundo2.HieloMacizo.Nombre, ConstructorEscenarioMundo2.PiezaDeBloque(1f, true).Nombre);
            Assert.AreEqual(ConstructorEscenarioMundo2.Ladrillo.Nombre, ConstructorEscenarioMundo2.PiezaDeBloque(1f, false).Nombre);
            Assert.AreEqual(ConstructorEscenarioMundo2.Ladrillo.Nombre, ConstructorEscenarioMundo2.PiezaDeBloque(14f, false).Nombre);

            Assert.IsTrue(DisenoNivel2.EsDeHielo("TechoDeHielo"), "El techo helado de la calle.");
            Assert.IsTrue(DisenoNivel2.EsDeHielo("Muralla"), "Y la muralla, que no agarra.");
            Assert.IsFalse(DisenoNivel2.Suelos.Any(s => DisenoNivel2.EsDeHielo(s.Nombre)));
        }

        [Test]
        public void LaEscenaDelNivel2_EstaVestidaConElTileset_YTieneSuFondo()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            var escena = EditorSceneManager.OpenScene(bd.niveles[1].escena.Ruta, OpenSceneMode.Additive);
            try
            {
                var recorrido = PreparadorNivel.RaizRecorrido(escena);
                var escenario = recorrido.Find(PreparadorNivel.NombreEscenario);
                Assert.IsNotNull(escenario, "El recorrido lleva los dibujos de sus bloques.");

                var cajas = recorrido.GetComponentsInChildren<BoxCollider2D>(true).Where(c => !c.isTrigger).ToList();
                foreach (var bloque in DisenoNivel2.Suelos.Concat(DisenoNivel2.Paredes).Concat(DisenoNivel2.PlataformasArena))
                {
                    var dibujo = escenario.Find(PreparadorNivel.NombreDibujo(bloque.Nombre));
                    Assert.IsNotNull(dibujo, $"El bloque {bloque.Nombre} no tiene dibujo.");
                    var sprite = dibujo.GetComponent<SpriteRenderer>();
                    var alto = bloque.Arriba - bloque.Abajo;
                    var pieza = ConstructorEscenarioMundo2.PiezaDeBloque(alto, DisenoNivel2.EsDeHielo(bloque.Nombre));
                    Assert.AreEqual(pieza.Nombre, sprite.sprite.name, bloque.Nombre);
                    Assert.AreEqual(SpriteDrawMode.Tiled, sprite.drawMode, bloque.Nombre);
                    Assert.AreEqual(bloque.Hasta - bloque.Desde, sprite.size.x, 1e-4f, $"{bloque.Nombre}: tan ancho como su colisión.");
                    Assert.AreEqual(bloque.Desde, dibujo.position.x, 1e-4f, bloque.Nombre);

                    // El ladrillo llena el bloque desde abajo; la viga y el hielo cuelgan de lo que se pisa.
                    var esLadrillo = pieza.Nombre == ConstructorEscenarioMundo2.Ladrillo.Nombre;
                    Assert.AreEqual(esLadrillo ? bloque.Abajo : bloque.Arriba, dibujo.position.y, 1e-4f, bloque.Nombre);
                    if (esLadrillo || pieza.Nombre == ConstructorEscenarioMundo2.HieloMacizo.Nombre)
                    {
                        Assert.AreEqual(alto, sprite.size.y, 1e-4f, $"{bloque.Nombre}: lleno de arriba abajo.");
                    }

                    var liso = cajas.Single(c => c.name == bloque.Nombre).GetComponent<SpriteRenderer>();
                    Assert.IsFalse(liso.enabled, $"{bloque.Nombre}: su rectángulo de color está apagado.");
                }

                // El hielo que resbala se ve: los bloques de hielo, a ras de lo que se pisa.
                var hielos = recorrido.Find("Hielo");
                foreach (var zona in DisenoNivel2.Hielos)
                {
                    var remate = hielos.Find(PreparadorNivel.NombreRemate(zona));
                    Assert.IsNotNull(remate, zona.Nombre);
                    var sprite = remate.GetComponent<SpriteRenderer>();
                    Assert.AreEqual(ConstructorEscenarioMundo2.Hielo.Nombre, sprite.sprite.name);
                    Assert.AreEqual(zona.Hasta - zona.Desde, sprite.size.x, 1e-4f, zona.Nombre);
                    Assert.AreEqual(new Vector2(zona.Desde, zona.Abajo), (Vector2)remate.position, zona.Nombre);
                    Assert.Greater(sprite.sortingOrder, ConstructorEscenarioMundo2.OrdenBloques, "Sobre el ladrillo.");
                }

                // La silla vacía, en la repisa del primer checkpoint, y la campana, colgando del tejado.
                var silla = escenario.Find("SillaVacia");
                Assert.IsNotNull(silla);
                Assert.AreEqual(DisenoNivel2.SillaVacia, (Vector2)silla.position);
                var repisa = DisenoNivel2.Suelos.Single(s => s.Nombre == "RepisaDeLaCampana");
                Assert.AreEqual(repisa.Arriba, DisenoNivel2.SillaVacia.y, 1e-4f, "Apoyada en la repisa.");
                var medioAncho = ConstructorEscenarioMundo2.Silla.Tamano.x * 0.5f;
                Assert.GreaterOrEqual(DisenoNivel2.SillaVacia.x - medioAncho, repisa.Desde, "Entera sobre la repisa.");
                Assert.Less(DisenoNivel2.SillaVacia.x + medioAncho, DisenoNivel2.Checkpoints[0].x, "A la izquierda de la bandera.");

                var campana = escenario.Find("CampanaDelCampanario");
                Assert.IsNotNull(campana);
                var tejado = DisenoNivel2.Paredes.Single(p => p.Nombre == "TejadoDeLaTorre");
                Assert.AreEqual(tejado.Abajo, campana.position.y, 1e-4f, "Cuelga del tejado.");
                var rellano = DisenoNivel2.Suelos.Single(s => s.Nombre == "Rellano3");
                Assert.Greater(campana.position.y - ConstructorEscenarioMundo2.Campana.Tamano.y, rellano.Arriba + DisenoNivel2.AltoKael,
                    "Por encima de la cabeza de Kael en el último rellano.");
                Assert.Less(silla.GetComponent<SpriteRenderer>().sortingOrder, ConstructorEscenarioMundo2.OrdenBloques, "Los props, detrás.");

                // El atrezo: cada pieza en su sitio, sin colisión, detrás de los bloques o colgada sobre ellos.
                foreach (var decorado in DisenoNivel2.Decorados)
                {
                    var objeto = escenario.Find(decorado.Nombre);
                    Assert.IsNotNull(objeto, $"Falta {decorado.Nombre}.");
                    var dibujo = objeto.GetComponent<SpriteRenderer>();
                    Assert.AreEqual(decorado.Pieza.Nombre, dibujo.sprite.name, decorado.Nombre);
                    Assert.AreEqual(decorado.Posicion, (Vector2)objeto.position, decorado.Nombre);
                    Assert.AreEqual(decorado.Volteado, dibujo.flipX, decorado.Nombre);
                    Assert.AreEqual(decorado.Delante ? ConstructorEscenarioMundo2.OrdenSobreBloques : ConstructorEscenarioMundo2.OrdenProps,
                        dibujo.sortingOrder, decorado.Nombre);
                    Assert.Less(dibujo.sortingOrder, 5, $"{decorado.Nombre}: por detrás de los enemigos y de Kael.");
                    Assert.IsNull(objeto.GetComponent<Collider2D>(), $"{decorado.Nombre} no se pisa ni choca.");
                }

                Assert.IsTrue(ConstructorFondoMundo2.EstaColocado(escena), "El fondo del mundo está en la escena.");

                // Los Lamentos, al tamaño de los Ecos del Nivel 1; los demás, al suyo.
                Assert.AreEqual(DisenoNivel1.EscalaEco, DisenoNivel2.EscalaLamento);
                var enemigos = recorrido.Find("Enemigos");
                foreach (var guardia in DisenoNivel2.Enemigos)
                {
                    var enemigo = enemigos.Find(guardia.Nombre);
                    Assert.IsNotNull(enemigo, guardia.Nombre);
                    var esLamento = guardia.Tipo == DisenoNivel2.TIPOS_ENEMIGO.LAMENTO;
                    Assert.AreEqual(esLamento ? DisenoNivel2.EscalaLamento.x : 1f, enemigo.localScale.x, 1e-4f, guardia.Nombre);
                    Assert.AreEqual(esLamento ? DisenoNivel2.EscalaLamento.y : 1f, enemigo.localScale.y, 1e-4f, guardia.Nombre);
                }

                Assert.AreEqual(5, DisenoNivel2.Enemigos.Count(e => e.Tipo == DisenoNivel2.TIPOS_ENEMIGO.LAMENTO), "Tres en la sección 1 y dos en el canal.");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}
