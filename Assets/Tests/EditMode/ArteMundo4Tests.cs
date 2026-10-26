using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Core.Combate;
using Fragmentum.EditorTools;
using Fragmentum.Enemies;
using Fragmentum.Levels.Mecanicas;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El arte del Mundo 4 (Culpa) montado en el juego (fase E, primera tanda): el proyecto pasa su
    /// verificador (Fragmentum → Niveles → Verificar el arte del Nivel 4); el Penitente, el Artillero y el
    /// Verdugo usan sus prefabs con arte; el cañón del Artillero es la boca de su fusil en el dibujo; el
    /// Verdugo lanza su garfio dibujado desde su mano; el Juez tiene un estado por gesto y la gema flota
    /// sobre su montón; y la explosión, la ola, la placa y la bala, con su tamaño de juego. De la segunda
    /// tanda: el barril y la mina con sus dibujos, las coberturas del alto de su bloque, el campanario con
    /// la torre, el arco y el balcón del diseño, y el tileset y los props cortados en sus piezas. Y lo último: las
    /// memorias a la medida pedida, el fondo con sus siete capas y las cinemáticas del Juez con su texto en el plano.
    /// </summary>
    public class ArteMundo4Tests
    {
        private static string Lista(IEnumerable<string> problemas) => string.Join("\n", problemas);

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
        public void ElArteDelNivel4_PasaLaVerificacion()
        {
            Assert.IsTrue(VerificadorNivel4.HayArte, "El arte del Mundo 4 está en el proyecto.");
            var problemas = VerificadorNivel4.Problemas();
            CollectionAssert.IsEmpty(problemas, Lista(problemas));
        }

        [Test]
        public void TodosLosFotogramasDeLasTablas_Estan_YCadaPngTieneSuRegla()
        {
            var sprites = VerificadorNivel4.Sprites();
            foreach (var fotograma in VerificadorNivel4.FotogramasDeLasTablas())
            {
                CollectionAssert.Contains(sprites, fotograma);
            }

            foreach (var sprite in sprites)
            {
                Assert.IsNotNull(TablaArte.ReglaDe(sprite), $"{sprite} no tiene regla.");
            }
        }

        // --- Los enemigos ---

        [Test]
        public void LosEnemigosDelNivel4_UsanSusPrefabsConArte()
        {
            Assert.AreSame(ConstructorPenitente.Prefab, GeneradorEnemigos.CargarPrefab("Penitente"));
            Assert.AreSame(ConstructorArtillero.Prefab, GeneradorEnemigos.CargarPrefab("Artillero"));
            Assert.AreSame(ConstructorVerdugo.Prefab, GeneradorEnemigos.CargarPrefab("Verdugo"));
            foreach (var prefab in new[] { ConstructorPenitente.Prefab, ConstructorArtillero.Prefab, ConstructorVerdugo.Prefab })
            {
                Assert.AreEqual(Color.white, prefab.GetComponent<EnemyController>().Datos.tinte, $"{prefab.name}: sus colores son los del dibujo.");
            }
        }

        [Test]
        public void ElPenitente_AtacaAlRitmoDelEco()
        {
            var datos = ConstructorPenitente.Prefab.GetComponent<EnemyController>().Datos;
            Assert.AreEqual(ConstructorEcoIra.VelocidadAtaque, ConstructorPenitente.VelocidadAtaque, 1e-4f, "1,6×, desde el principio.");
            Assert.AreEqual(0.45f, datos.attackCooldown, 1e-4f, "Y descansa lo que el Eco.");
            Assert.AreEqual(ConstructorPenitente.DuracionAtaque, datos.duracionAtaque, 1e-4f);
            var animacion = ConstructorPenitente.Prefab.GetComponentInChildren<EnemigoAnimacion>();
            Assert.AreEqual(ConstructorPenitente.VelocidadAtaque, animacion.velocidadAtaque, 1e-4f);
        }

        [Test]
        public void ElArtillero_DisparaSuBala_DesdeLaBocaDeSuFusil()
        {
            var dibujo = ConstructorArtillero.CanonDelDibujo;
            Assert.AreEqual(DisenoNivel4.CanonDelante, dibujo.x, 0.01f, "El cañón del diseño es la boca de su fusil en el dibujo…");
            Assert.AreEqual(DisenoNivel4.AlturaCanon, dibujo.y, 0.01f, "…a 1,06 u del suelo.");

            var artillero = ConstructorArtillero.Prefab.GetComponent<Artillero>();
            Assert.AreEqual(DisenoNivel4.CanonDelante, artillero.canon.x, 1e-4f);
            Assert.AreEqual(DisenoNivel4.AlturaCanon, artillero.canon.y, 1e-4f);
            Assert.IsTrue(artillero.ConArte, "Su Animator sigue lo que hace.");
            Assert.AreSame(ConstructorEfectosMundo4.PrefabBala, Campo<Proyectil>(artillero, "bala"), "Dispara su bala dibujada.");
            Assert.IsTrue(artillero.GetComponentInChildren<SpriteRenderer>().flipX, "Dibujado mirando a la izquierda: va volteado.");
            Assert.Greater(DisenoNivel4.AlturaCanon, DisenoNivel4.AltoSacosArtillero, "Su fusil asoma por encima de sus sacos.");
        }

        [Test]
        public void ElArtillero_AvisaDelCulatazo_YMuereHastaQuedarSoloSuAbrigo()
        {
            var avisa = ConstructorArtillero.ClipDe(ConstructorArtillero.EstadoDe(POSES_ARTILLERO.ALZA_LA_CULATA));
            CollectionAssert.AreEqual(new[] { "G1" }, avisa.Fotogramas, "El aviso del culatazo: el fusil vuelto, con la culata en alto.");
            var muerte = ConstructorArtillero.ClipDe(ConstructorArtillero.Muerte);
            CollectionAssert.AreEqual(new[] { "M1", "M2", "M3" }, muerte.Fotogramas, "Cae, tendido y, al final, solo su abrigo.");
            var evento = ConstructorArtillero.EventosDe(muerte).Single();
            Assert.AreEqual(muerte.Inicio(2), evento.time, 1e-4f, "Se desvanece cuando ya solo queda el abrigo.");

            // Cada uno de los dos nuevos, con su lienzo y su ancla (la de los demás, llevada a él), al mismo píxel.
            foreach (var suelto in ConstructorArtillero.Sueltos)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ConstructorArtillero.RutaSprite(suelto.Archivo));
                Assert.IsNotNull(sprite, suelto.Archivo);
                Assert.AreEqual(suelto.Lienzo, new Vector2Int((int)sprite.rect.width, (int)sprite.rect.height), "No se recorta ni se reescala.");
                Assert.AreEqual(ConstructorArtillero.PixelesPorUnidad, sprite.pixelsPerUnit, 1e-3f, "El mismo píxel que los demás.");
                Assert.AreEqual(suelto.Ancla.x, sprite.pivot.x, 0.01f, suelto.Archivo);
                Assert.AreEqual(suelto.Lienzo.y - suelto.Ancla.y, sprite.pivot.y, 0.01f, suelto.Archivo);
            }
        }

        [Test]
        public void LaOndaDeLaSentencia_SeLevantaLoQueSuZona_DesdeSuFrente()
        {
            var prefab = ConstructorEfectosMundo4.PrefabOnda;
            Assert.IsNotNull(prefab, "Falta la onda dibujada.");
            var dibujo = prefab.GetComponent<SpriteRenderer>();
            Assert.IsTrue(dibujo.flipX, "Dibujada avanzando a la izquierda: va volteada, y la zona la espeja.");
            Assert.IsNotNull(prefab.GetComponent<BucleDeSprites>(), "Sus dos fotogramas, en bucle.");

            var sentencia = AssetDatabase.LoadAssetAtPath<PatronSentencia>($"{GeneradorJefes.CarpetaPatrones}/Sentencia.asset");
            var sprite = dibujo.sprite;
            var alto = (sprite.rect.height - (ConstructorEfectosMundo4.LienzoOnda.y - ConstructorEfectosMundo4.SueloOnda) - 3) / sprite.pixelsPerUnit;
            Assert.AreEqual(sentencia.altoOnda, alto, 0.05f, "Lo que se levanta sobre el suelo mide lo que su zona: se salta igual.");
            Assert.AreEqual(1f / ConstructorEfectosMundo4.LienzoOnda.x, sprite.pivot.x / sprite.rect.width, 1e-3f, "El pivote, en su frente.");
        }

        [Test]
        public void ElVerdugo_LanzaSuGarfioDibujado_ConLaCadenaDesdeSuMano()
        {
            var verdugo = ConstructorVerdugo.Prefab.GetComponent<Verdugo>();
            Assert.AreEqual(ConstructorVerdugo.ManoDelDibujo.x, verdugo.manoCadena, 1e-4f, "La cadena sale de su mano, donde la dibujó…");
            Assert.AreEqual(ConstructorVerdugo.ManoDelDibujo.y, verdugo.alturaMano, 1e-4f);
            Assert.Greater(verdugo.alturaMano, verdugo.alturaCadena, "…y baja hasta el garfio, que vuela a la altura del pecho de Kael.");
            Assert.Less(verdugo.alturaCadena, DisenoNivel4.AltoKael, "De pie, le da…");
            Assert.Greater(verdugo.alturaCadena - verdugo.radioGarfio, DisenoNivel4.AltoKaelAgachado, "…y agachado le pasa por encima.");

            var garfio = Campo<Transform>(verdugo, "garfio");
            Assert.AreSame(ConstructorEfectosMundo4.Garfio, garfio.GetComponent<SpriteRenderer>().sprite);
            Assert.IsFalse(garfio.gameObject.activeSelf, "Apagado hasta que lo lanza.");
            var cadena = Campo<SpriteRenderer>(verdugo, "cadena");
            Assert.AreSame(ConstructorEfectosMundo4.Cadena, cadena.sprite);
            Assert.AreEqual(SpriteDrawMode.Tiled, cadena.drawMode, "El tramo se repite a lo largo.");
        }

        // --- El Juez ---

        [Test]
        public void ElJuez_TieneUnEstadoYUnDisparadorPorGesto()
        {
            var controlador = ConstructorJuez.Controlador;
            Assert.IsNotNull(controlador);
            var estados = controlador.layers[0].stateMachine.states.Select(s => s.state.name).ToList();
            var parametros = controlador.parameters;
            foreach (GESTOS_JUEZ gesto in System.Enum.GetValues(typeof(GESTOS_JUEZ)))
            {
                CollectionAssert.Contains(estados, gesto.ToString());
                Assert.IsTrue(parametros.Any(p => p.name == gesto.ToString() && p.type == AnimatorControllerParameterType.Trigger),
                    $"El disparador de {gesto}.");
            }

            CollectionAssert.Contains(estados, ConstructorJuez.Andar);
            CollectionAssert.Contains(estados, ConstructorJuez.Rugido);
            Assert.AreEqual(1.5f, ConstructorJuez.ClipDe(ConstructorJuez.Rugido).Duracion, 1e-4f, "La condena dura lo que espera al cambiar de fase.");

            var juez = Prefab(VerificadorNivel4.RutaPrefabJuez).GetComponent<JuezSilente>();
            Assert.AreEqual(Color.white, juez.Datos.tinte);
            Assert.AreEqual(ConstructorJuez.DuracionDerrota, juez.Datos.duracionDerrota, 1e-4f);
            Assert.AreSame(ConstructorEfectosMundo4.Cadena, Campo<Sprite>(juez, "cadena"), "Las cadenas de la trampa, dibujadas.");
        }

        [Test]
        public void LaGemaDelJuez_FlotaSobreSuMonton_YVaAparte()
        {
            var juez = Prefab(VerificadorNivel4.RutaPrefabJuez).GetComponent<JuezSilente>();
            var gema = juez.Fragmento.Gema;
            Assert.IsNotNull(gema, "M5 es solo el montón: la gema va aparte.");
            Assert.AreEqual(ConstructorJuez.RutaFragmento, AssetDatabase.GetAssetPath(gema.sprite));
            Assert.IsFalse(juez.Fragmento.gameObject.activeSelf, "Aparece al llegar la muerte a M5.");

            var cima = (ConstructorJuez.Ancla.y - ConstructorJuez.CimaDelMonton.y) / ConstructorJuez.PixelesPorUnidad;
            var abajo = gema.transform.localPosition.y - gema.sprite.bounds.size.y * 0.5f;
            Assert.That(abajo, Is.InRange(cima + 0.1f, cima + 0.5f), "Flota sobre el montón, cerca de él.");
            var zona = juez.Fragmento.GetComponent<BoxCollider2D>();
            Assert.GreaterOrEqual(zona.offset.y + zona.size.y * 0.5f, gema.transform.localPosition.y, "Se recoge sin saltar: del suelo a la gema.");
        }

        [Test]
        public void LosAtaquesDelJuez_LlevanSuDibujo()
        {
            const string carpeta = GeneradorJefes.CarpetaPatrones;
            Assert.AreSame(ConstructorEfectosMundo4.PrefabArtilleria, AssetDatabase.LoadAssetAtPath<PatronOrden>($"{carpeta}/Orden.asset").efecto);
            Assert.AreSame(ConstructorEfectosMundo4.PrefabOla, AssetDatabase.LoadAssetAtPath<PatronPueblo>($"{carpeta}/Pueblo.asset").efecto);
            Assert.AreSame(ConstructorEfectosMundo4.PrefabPlaca, AssetDatabase.LoadAssetAtPath<PatronCompanero>($"{carpeta}/Companero.asset").placa);
            Assert.AreSame(ConstructorEfectosMundo4.PrefabOnda, AssetDatabase.LoadAssetAtPath<PatronSentencia>($"{carpeta}/Sentencia.asset").efecto,
                "La onda de la sentencia, con su dibujo (apartado K2).");
            Assert.IsFalse(AssetDatabase.LoadAssetAtPath<PatronSentencia>($"{carpeta}/Sentencia.asset").dibujarGolpe,
                "La espada clavada ya se ve en su dibujo.");
        }

        // --- Lo que lanzan y lo que dejan ---

        [Test]
        public void LaExplosion_MideLoQueAlcanzaUnBarril_YLaLlevanElBarrilYLaMina()
        {
            var bola = AssetDatabase.LoadAssetAtPath<Sprite>(ConstructorEfectosMundo4.RutaExplosion("Explosion2"));
            Assert.AreEqual(DisenoNivel4.RadioBarril * 2f, bola.bounds.size.x, 0.1f, "La bola de fuego, del ancho de su alcance.");
            Assert.AreEqual(0f, bola.bounds.min.y, 1e-3f, "Nace en el suelo.");

            var explosion = ConstructorEfectosMundo4.PrefabExplosion;
            Assert.AreEqual(DisenoNivel4.RadioBarril, explosion.radio, 1e-4f);
            foreach (var nombre in new[] { GeneradorMecanicas.BarrilExplosivo, GeneradorMecanicas.Mina })
            {
                var prefab = Prefab($"{GeneradorMecanicas.Carpeta}/{nombre}.prefab");
                Component pieza = (Component)prefab.GetComponent<BarrilExplosivo>() ?? prefab.GetComponent<Mina>();
                Assert.AreSame(explosion, Campo<ExplosionDibujada>(pieza, "dibujoExplosion"), $"{nombre} estalla con la explosión dibujada.");
            }
        }

        [Test]
        public void LaOla_MiraALaDerecha_ConElPivoteEnSuFrente_YMideLoQueSuZona()
        {
            var ola = ConstructorEfectosMundo4.PrefabOla.GetComponent<SpriteRenderer>();
            Assert.IsTrue(ola.flipX, "Dibujada avanzando a la izquierda: volteada mira a la derecha y la zona la espeja.");
            Assert.AreEqual(0f, ola.sprite.pivot.x, 1e-3f, "El pivote, en su frente.");
            var pueblo = AssetDatabase.LoadAssetAtPath<PatronPueblo>($"{GeneradorJefes.CarpetaPatrones}/Pueblo.asset");
            Assert.AreEqual(pueblo.alto, ConstructorEfectosMundo4.AltoLlamas / ola.sprite.pixelsPerUnit, 0.05f,
                "Las llamas, del alto de su zona de daño: se salta.");
        }

        [Test]
        public void LaPlacaYLaBala_SonProyectilesConSuDibujo()
        {
            var placa = ConstructorEfectosMundo4.PrefabPlaca;
            Assert.IsNotNull(placa.GetComponentInChildren<BucleDeSprites>(), "La placa gira en el aire (dos fotogramas).");
            Assert.AreEqual(ConstructorJuez.PixelesPorUnidad, placa.GetComponentInChildren<SpriteRenderer>().sprite.pixelsPerUnit, 1e-3f,
                "Con el píxel del Juez.");

            var bala = ConstructorEfectosMundo4.PrefabBala;
            var dibujo = bala.GetComponentInChildren<SpriteRenderer>();
            Assert.IsTrue(dibujo.flipX, "Dibujada volando a la izquierda: volteada vuela a la derecha y el proyectil la espeja.");
            Assert.That(dibujo.sprite.bounds.size.x, Is.InRange(0.4f, 0.7f), "Un trazador corto.");
        }

        // --- La segunda tanda: las mecánicas y el escenario ---

        [Test]
        public void ElBarril_TieneSusCuatroDibujos_YSeApoyaEnElSuelo()
        {
            var dibujos = ConstructorMecanicasMundo4.DibujosBarril;
            Assert.IsNotNull(dibujos, "Están los cuatro dibujos del barril.");
            var barril = Prefab($"{GeneradorMecanicas.Carpeta}/{GeneradorMecanicas.BarrilExplosivo}.prefab").GetComponent<BarrilExplosivo>();
            Assert.IsTrue(barril.ConArte);
            CollectionAssert.AreEqual(dibujos,
                new[] { "spriteIntacto", "spriteMecha1", "spriteMecha2", "spriteQuemado" }.Select(c => Campo<Sprite>(barril, c)).ToArray(),
                "Intacto, la mecha que parpadea (dos) y quemado.");

            var dibujo = barril.GetComponentInChildren<SpriteRenderer>();
            Assert.AreSame(dibujos[0], dibujo.sprite, "Empieza intacto.");
            Assert.AreEqual(0f, dibujo.sprite.bounds.min.y, 0.02f, "Con el pivote al pie: se apoya en el suelo.");
            Assert.AreEqual(0f, dibujo.sprite.bounds.center.x, 0.01f, "Centrado sobre su choque.");
        }

        [Test]
        public void LaMina_TieneSuBolaDibujada_YLaCadenaDelVerdugoDesdeElFondo()
        {
            var dibujos = ConstructorMecanicasMundo4.DibujosMina;
            Assert.IsNotNull(dibujos, "Están la mina quieta y avisando.");
            var mina = Prefab($"{GeneradorMecanicas.Carpeta}/{GeneradorMecanicas.Mina}.prefab").GetComponent<Mina>();
            Assert.AreSame(dibujos[0], Campo<Sprite>(mina, "spriteReposo"));
            Assert.AreSame(dibujos[1], Campo<Sprite>(mina, "spriteAviso"));
            var bola = Campo<SpriteRenderer>(mina, "dibujoBola");
            Assert.IsNotNull(bola, "La bola parpadea con Kael cerca: necesita su dibujo.");
            Assert.AreSame(dibujos[0], bola.sprite);

            var cadena = mina.transform.Find("Cadena").GetComponent<SpriteRenderer>();
            Assert.AreSame(ConstructorEfectosMundo4.Cadena, cadena.sprite, "La cadena es la del Verdugo.");
            Assert.AreEqual(SpriteDrawMode.Tiled, cadena.drawMode, "Repetida del fondo a la bola.");
            Assert.AreEqual(DisenoNivel4.MinaSobreElFondo, cadena.size.x, 1e-4f, "Girada: su largo es el ancho del mosaico.");
        }

        [Test]
        public void LasCoberturas_MidenElAltoDeSuBloque_YSobresalenPoco()
        {
            var bloques = DisenoNivel4.Coberturas.Append(DisenoNivel4.Suelos.Single(s => s.Nombre == "Tanque"));
            foreach (var bloque in bloques)
            {
                var pieza = PreparadorNivel.CoberturaDe(bloque.Nombre);
                Assert.IsNotNull(pieza, $"{bloque.Nombre} tiene su dibujo.");
                var sprite = pieza.Sprite;
                Assert.IsNotNull(sprite, $"Falta {pieza.Ruta}.");
                Assert.AreEqual(bloque.Arriba - bloque.Abajo, sprite.bounds.max.y, 0.02f, $"{bloque.Nombre}: lo alto del dibujo, lo alto del bloque.");

                // La chimenea lleva debajo su base de tejas, más ancha, y el tanque su cañón: se miden solos.
                if (pieza == ConstructorMecanicasMundo4.Chimenea || pieza == ConstructorMecanicasMundo4.Tanque)
                {
                    continue;
                }

                var mitad = (bloque.Hasta - bloque.Desde) * 0.5f;
                Assert.LessOrEqual(-sprite.bounds.min.x - mitad, 0.25f, $"{bloque.Nombre}: sobresale poco por la izquierda.");
                Assert.LessOrEqual(sprite.bounds.max.x - mitad, 0.25f, $"{bloque.Nombre}: sobresale poco por la derecha.");
            }
        }

        [Test]
        public void ElCampanario_DibujaLaTorre_ElArco_YElBalcon_DelDiseno()
        {
            Assert.IsNotNull(ConstructorMecanicasMundo4.Campanario, $"Falta {ConstructorMecanicasMundo4.RutaCampanario}.");
            var torre = DisenoNivel4.Paredes.Single(p => p.Nombre == "TorreDelCampanario");
            var balcon = DisenoNivel4.Balcon;
            var suelo = DisenoNivel4.PisadaDe("BajoElCampanario").Y;
            Assert.AreEqual(balcon.Arriba - suelo, ConstructorMecanicasMundo4.AlturaBalconDibujado, 0.05f, "El suelo del balcón, el del dibujo.");
            Assert.AreEqual(balcon.Hasta - balcon.Desde, ConstructorMecanicasMundo4.AnchoBalconDibujado, 0.05f, "Lo que sale el balcón de la torre.");
            Assert.AreEqual(torre.Abajo - suelo, ConstructorMecanicasMundo4.AltoArcoDibujado, 0.05f, "El hueco del arco.");
            Assert.AreEqual(torre.Arriba - suelo, ConstructorMecanicasMundo4.AltoTorreDibujada, 0.05f, "Lo alto de la torre.");
            Assert.AreEqual(torre.Hasta - torre.Desde, ConstructorMecanicasMundo4.AnchoTorre, 0.1f, "Lo ancho de la torre.");
        }

        [Test]
        public void ElTilesetYLosProps_EstanCortados_ConSuPixel_YLosTejadosSeAlarganPorSuCentro()
        {
            Assert.IsTrue(ConstructorEscenarioMundo4.Completo, "Las dos hojas, cortadas en todas sus piezas.");
            foreach (var pieza in ConstructorEscenarioMundo4.Piezas)
            {
                var sprite = ConstructorEscenarioMundo4.Cargar(pieza);
                Assert.AreEqual(pieza.PixelesPorUnidad, sprite.pixelsPerUnit, 1e-3f, pieza.Nombre);
                Assert.AreEqual(pieza.Tamano.x, sprite.bounds.size.x, 1e-3f, pieza.Nombre);
            }

            var tejado = ConstructorEscenarioMundo4.Cargar(ConstructorEscenarioMundo4.Tejado);
            Assert.AreEqual(ConstructorEscenarioMundo4.Tejado.Bordes, tejado.border, "Los extremos de la viga no se estiran.");
        }

        // --- Lo último de la segunda tanda: las memorias, el fondo y las cinemáticas ---

        [Test]
        public void LasMemorias_MidenLoQueSePidio_YSuBrilloCaeEncimaDelObjeto()
        {
            // Lo que se pidió, a 32 píxeles por unidad (Prompts_Mundo4.md, E9): la radio y la carta, 32 de ancho;
            // la campana, 34; el casco sobre el fusil, 48 de alto; la losa, el ancho de una marca.
            var medidas = new (ConstructorMecanicasMundo4.DibujoMemoria dibujo, float medida, bool ancho)[]
            {
                (ConstructorMecanicasMundo4.Radio, 1f, true),
                (ConstructorMecanicasMundo4.Campana, 34f / 32f, true),
                (ConstructorMecanicasMundo4.Casco, 1.5f, false),
                (ConstructorMecanicasMundo4.Carta, 1f, true),
                (ConstructorMecanicasMundo4.Losa, DisenoNivel4.TamanoMarca.x, true),
            };
            foreach (var (dibujo, medida, ancho) in medidas)
            {
                Assert.IsTrue(dibujo.Completo, $"{dibujo.Nombre}: apagada y brillando.");
                var apagada = dibujo.Apagada;
                var brillando = dibujo.Brillando;
                Assert.AreEqual(apagada.rect.size, brillando.rect.size, $"{dibujo.Nombre}: el mismo lienzo.");
                Assert.AreEqual(apagada.pivot, brillando.pivot, $"{dibujo.Nombre}: el mismo pivote: el brillo cae encima.");

                // Lo que mide el objeto (lo opaco de la apagada), no su lienzo.
                var opaco = Opaco(apagada);
                var mide = (ancho ? opaco.width : opaco.height) / apagada.pixelsPerUnit;
                Assert.AreEqual(medida, mide, 0.03f, $"{dibujo.Nombre}: {(ancho ? "de ancho" : "de alto")}.");
            }

            // Al pie: el objeto apoya en su pivote; la losa, metida en el suelo, tiene su borde de arriba en él.
            foreach (var dibujo in ConstructorMecanicasMundo4.Memorias)
            {
                var opaco = Opaco(dibujo.Apagada);
                Assert.AreEqual(dibujo.Apagada.pivot.y, opaco.yMin, 1f, $"{dibujo.Nombre}: el pivote, en su fila de abajo.");
            }

            var losa = ConstructorMecanicasMundo4.Losa.Apagada;
            Assert.AreEqual(losa.rect.height, losa.pivot.y, 1e-3f, "La losa: el pivote, arriba.");
        }

        // Lo opaco de un sprite, en píxeles de su textura (desde abajo a la izquierda, como el pivote).
        private static Rect Opaco(Sprite sprite)
        {
            var ruta = AssetDatabase.GetAssetPath(sprite);
            var textura = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(textura.LoadImage(System.IO.File.ReadAllBytes(ruta)), ruta);
                var pixeles = textura.GetPixels32();
                int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
                for (var y = 0; y < textura.height; y++)
                {
                    for (var x = 0; x < textura.width; x++)
                    {
                        if (pixeles[y * textura.width + x].a == 0)
                        {
                            continue;
                        }

                        x0 = Mathf.Min(x0, x);
                        y0 = Mathf.Min(y0, y);
                        x1 = Mathf.Max(x1, x);
                        y1 = Mathf.Max(y1, y);
                    }
                }

                return Rect.MinMaxRect(x0, y0, x1 + 1, y1 + 1);
            }
            finally
            {
                Object.DestroyImmediate(textura);
            }
        }

        [Test]
        public void ElFondo_TieneSusSieteCapas_LaAlambradaAlSuelo_YElCampanarioSobreLaPlaza()
        {
            var prefab = Prefab(ConstructorFondoMundo4.RutaPrefab);
            Assert.IsNull(prefab.GetComponent<Fragmentum.Levels.Escenario.FondoConLaLuz>(), "Aquí es de día: no se oscurece con la luz.");
            Assert.AreEqual(ConstructorFondoMundo4.CapasFondo.Length, prefab.GetComponentsInChildren<Renderer>(true).Length);

            // Las capas de la vista siguen a la cámara; la alambrada, en su sitio del mundo.
            var grupos = prefab.GetComponentsInChildren<Fragmentum.Levels.Escenario.ParallaxMovement>(true);
            Assert.AreEqual(ConstructorFondoMundo4.NombreVista, grupos.Single(g => g.SigueCamaraEnY).name);
            var alSuelo = grupos.Single(g => !g.SigueCamaraEnY);
            var alambrada = alSuelo.transform.Find(ConstructorFondoMundo4.Alambrada.Nombre);
            Assert.IsNotNull(alambrada, "Desde los tejados no se ve en el aire: va anclada al suelo.");
            Assert.AreEqual(ConstructorFondoMundo4.Alambrada.Arriba, alambrada.localPosition.y + alambrada.localScale.y * 0.5f, 1e-4f);
            Assert.Less(alambrada.localPosition.y - alambrada.localScale.y * 0.5f, DisenoNivel4.FondoRio,
                "Su borde de abajo no asoma en el fondo del río.");

            // El hito, centrado en la plaza del Juez.
            var campanario = prefab.GetComponentInChildren<Fragmentum.Levels.Escenario.ParallaxProps>(true);
            Assert.IsNotNull(campanario);
            var sprite = campanario.GetComponentInChildren<SpriteRenderer>(true);
            Assert.IsNotNull(sprite.sprite, "El campanario, con su dibujo.");
            Assert.AreEqual(ConstructorFondoMundo4.CentroDelCampanario, sprite.transform.position.x, 1e-3f);
            Assert.That(ConstructorFondoMundo4.CentroDelCampanario,
                Is.InRange(DisenoNivel4.LimitesArena.x, DisenoNivel4.LimitesArena.y), "Dentro de la plaza.");
        }

        [Test]
        public void LasCinematicasDelJuez_LlevanSusPlanos_YSuTextoSeLeeDentroDelPlano()
        {
            Assert.IsTrue(ConstructorCinematicasNivel4.HayPlanos, "Los cuatro planos del Juez.");
            var presentacion = AssetDatabase.LoadAssetAtPath<Fragmentum.Narrative.CutsceneData>(ConstructorCinematicasNivel4.RutaPresentacion);
            var derrota = AssetDatabase.LoadAssetAtPath<Fragmentum.Narrative.CutsceneData>(ConstructorCinematicasNivel4.RutaDerrota);
            var nivel = ConstructorCinematicasNivel4.Nivel();
            Assert.AreSame(presentacion, nivel.cinematicaJefe, "El Nivel 4 presenta al Juez con sus planos.");
            Assert.AreSame(derrota, nivel.cinematicaJefeDerrotado, "Y lo despide con los suyos.");
            Assert.AreEqual(9.8f, presentacion.DuracionTotal, 1e-3f, "Lo que dura la del Pavor.");
            Assert.IsFalse(presentacion.sobreElJuego, "Con planos, opaca.");
            CollectionAssert.AreEqual(new[] { ConstructorCinematicasNivel4.Derrota1, ConstructorCinematicasNivel4.Derrota2 },
                derrota.diapositivas.Where(d => d.imagen != null).Select(d => d.imagen.name).ToArray());

            // Lo que dice y el rótulo, dentro del plano; de cerca, lo que dice va arriba y el rótulo abajo.
            var carteles = presentacion.diapositivas.SelectMany(d => d.carteles).ToList();
            Assert.AreEqual(4, carteles.Count, "Las dos partes de su línea, su nombre y su mundo.");
            foreach (var cartel in carteles)
            {
                Assert.That(cartel.punto.x, Is.InRange(0, ConstructorCinematicasNivel4.Rejilla.x), cartel.texto);
                Assert.That(cartel.punto.y, Is.InRange(0, ConstructorCinematicasNivel4.Rejilla.y - 12), cartel.texto);
            }

            var guion = GuionMundos.De(Fragmentum.Data.EMOCIONES.CULPA);
            Assert.AreEqual(guion.AntesDelJefe, string.Join(" ", carteles.Where(c => !c.titulo).Take(2).Select(c => c.texto)), "Su línea, entera.");

            // De cerca, lo que dice y el rótulo van en la misma franja: uno detrás de otro, sin pisarse.
            var cerca = presentacion.diapositivas.Last(d => d.imagen != null).carteles;
            var rotulo = cerca.Single(c => c.titulo);
            Assert.LessOrEqual(cerca.First().Fin, rotulo.inicio + 1e-3f, "El rótulo entra cuando se ha ido lo que dice.");
        }

        [Test]
        public void EnDosLineas_ParteLaLineaPorLaFraseMasCercanaASuMitad()
        {
            Assert.AreEqual("Una radio entre el barro. «Adelante».\nÉl dice que sí antes de preguntar adónde.",
                ConstructorCinematicasNivel4.EnDosLineas("Una radio entre el barro. «Adelante». Él dice que sí antes de preguntar adónde."));
            Assert.AreEqual("Una carta que solo dice «Lo siento».\nLa escribió muchas veces. Nunca la envió.",
                ConstructorCinematicasNivel4.EnDosLineas("Una carta que solo dice «Lo siento». La escribió muchas veces. Nunca la envió."));
            Assert.AreEqual("sin punto ninguno\nen toda la frase", ConstructorCinematicasNivel4.EnDosLineas("sin punto ninguno en toda la frase"),
                "Sin final de frase, por el espacio más cercano a la mitad.");
            Assert.AreEqual("corta", ConstructorCinematicasNivel4.EnDosLineas("corta"));
        }
    }
}
