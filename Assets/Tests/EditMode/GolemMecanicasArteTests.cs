using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.EditorTools;
using Fragmentum.Levels.Mecanicas;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests
{
    /// <summary>
    /// El arte del Golem de Ceniza, las mecánicas y las cinemáticas del Nivel 1.
    ///
    /// Fase A (importación): los 30 PNG están en su carpeta con su lienzo entero, y cada uno se
    /// importa como pixel art con los píxeles por unidad y el pivote de su carpeta. Los valores
    /// esperados se copian del encargo, no de <see cref="TablaArte"/>: una errata en la tabla salta
    /// aquí. Los pivotes se comprueban además contra los píxeles: caen en el suelo del Golem y de
    /// la torreta y en la superficie que se pisa de las plataformas.
    ///
    /// Fase B (clips): los del Golem tienen los tiempos del encargo, sin uniformar; los de las
    /// mecánicas son los estados que sostiene su script.
    ///
    /// Fase C (Animators): el del Golem se mueve con los parámetros de la FSM del enemigo; los de
    /// las mecánicas, con el entero que pone su script. Ninguno tiene estados sueltos ni
    /// transiciones rotas, y regenerarlos no cambia nada.
    /// </summary>
    public class GolemMecanicasArteTests
    {
        private sealed class Grupo
        {
            public string Carpeta;
            public string[] Archivos;
            public int Ancho;
            public int Alto;
            public float PixelesPorUnidad;
            public Vector2 Pivote;
            public SpriteAlignment Alineacion = SpriteAlignment.Custom;

            public IEnumerable<string> Rutas => Archivos.Select(a => $"{Carpeta}/{a}.png");
        }

        private const string Golem = "Assets/Sprites/Enemigos/GolemCeniza";
        private const string Mecanicas = "Assets/Sprites/Mecanicas";
        private const string Cinematicas = "Assets/Sprites/Cinematicas/Nivel1";

        // Sección 2 del encargo; la torreta disparando, con su pivote corrido 15 px (decisión 1).
        private static readonly Grupo[] Grupos =
        {
            new Grupo
            {
                Carpeta = Golem, Ancho = 90, Alto = 92, PixelesPorUnidad = 24f, Pivote = new Vector2(0.443f, 0.043f),
                Archivos = new[]
                {
                    "Golem_I1", "Golem_I2", "Golem_I3", "Golem_W1", "Golem_W2", "Golem_W3", "Golem_W4",
                    "Golem_A1", "Golem_A3", "Golem_A4", "Golem_A5", "Golem_D1", "Golem_D2", "Golem_D3", "Golem_D4",
                },
            },
            new Grupo
            {
                Carpeta = Mecanicas, Ancho = 82, Alto = 42, PixelesPorUnidad = 24f, Pivote = new Vector2(0.470f, 0.048f),
                Archivos = new[] { "Torreta_1_reposo", "Torreta_2_carga" },
            },
            new Grupo
            {
                Carpeta = Mecanicas, Ancho = 82, Alto = 42, PixelesPorUnidad = 24f, Pivote = new Vector2(0.287f, 0.048f),
                Archivos = new[] { "Torreta_3_disparo" },
            },
            new Grupo
            {
                Carpeta = Mecanicas, Ancho = 151, Alto = 64, PixelesPorUnidad = 48f, Pivote = new Vector2(0f, 1f),
                Archivos = new[] { "Lava", "Lava_espejo" },
            },
            new Grupo
            {
                Carpeta = Mecanicas, Ancho = 184, Alto = 142, PixelesPorUnidad = 48f, Pivote = new Vector2(0.5f, 0.852f),
                Archivos = new[] { "Plataforma_1_intacta", "Plataforma_2_agrietada", "Plataforma_3_rota", "Plataforma_4_polvo" },
            },
            new Grupo
            {
                Carpeta = Mecanicas, Ancho = 166, Alto = 89, PixelesPorUnidad = 48f, Pivote = new Vector2(0.5f, 0.708f),
                Archivos = new[] { "Combustion_1_apagada", "Combustion_2_calienta", "Combustion_3_encendida" },
            },
            new Grupo
            {
                // A 1 píxel por unidad, como la cámara de cinemáticas (Pixel Perfect, Assets PPU 1).
                Carpeta = Cinematicas, Ancho = 240, Alto = 135, PixelesPorUnidad = 1f, Pivote = new Vector2(0.5f, 0.5f),
                Alineacion = SpriteAlignment.Center,
                Archivos = new[] { "CinematicaJefe1", "CinematicaJefe2", "CinematicaFinalLvl1" },
            },
        };

        private static Sprite Cargar(string ruta)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            Assert.IsNotNull(sprite, $"Falta {ruta} o no se importó como sprite.");
            return sprite;
        }

        private static string Ruta(string carpeta, string archivo) => $"{carpeta}/{archivo}.png";

        /// <summary>
        /// Filas opacas de un PNG leído del disco, contadas desde arriba como en el LEEME: la más
        /// alta y la más baja de la franja de columnas pedida (todas si no se pide).
        /// </summary>
        private static (int arriba, int abajo) FilasOpacas(string ruta, int desdeX = 0, int hastaX = int.MaxValue)
        {
            var textura = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(textura.LoadImage(File.ReadAllBytes(ruta)), $"No se pudo leer {ruta}.");
                var pixeles = textura.GetPixels32();
                int ancho = textura.width, alto = textura.height;
                int arriba = int.MaxValue, abajo = int.MinValue;
                for (var y = 0; y < alto; y++)
                {
                    for (var x = Mathf.Max(0, desdeX); x <= Mathf.Min(ancho - 1, hastaX); x++)
                    {
                        if (pixeles[y * ancho + x].a > 128)
                        {
                            var fila = alto - 1 - y;
                            arriba = Mathf.Min(arriba, fila);
                            abajo = Mathf.Max(abajo, fila);
                        }
                    }
                }

                return (arriba, abajo);
            }
            finally
            {
                Object.DestroyImmediate(textura);
            }
        }

        /// <summary>Primera columna opaca de una fila (contada desde arriba) de un PNG leído del disco.</summary>
        private static int PrimeraColumnaOpaca(string ruta, int fila)
        {
            var textura = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(textura.LoadImage(File.ReadAllBytes(ruta)), $"No se pudo leer {ruta}.");
                var y = textura.height - 1 - fila;
                for (var x = 0; x < textura.width; x++)
                {
                    if (textura.GetPixel(x, y).a > 0.5f)
                    {
                        return x;
                    }
                }

                return -1;
            }
            finally
            {
                Object.DestroyImmediate(textura);
            }
        }

        // Fila del lienzo (desde arriba) donde cae el pivote de un sprite.
        private static float FilaDelPivote(string ruta)
        {
            var sprite = Cargar(ruta);
            return sprite.rect.height - sprite.pivot.y;
        }

        // --- Fase A: importación ---

        [Test]
        public void LosTreintaPNG_EstanEnSuCarpeta_ConElLienzoEntero()
        {
            var rutas = Grupos.SelectMany(g => g.Rutas).ToList();
            Assert.AreEqual(30, rutas.Count, "El encargo trae 15 fotogramas del Golem, 12 de mecánicas y 3 planos.");

            foreach (var grupo in Grupos)
            {
                foreach (var ruta in grupo.Rutas)
                {
                    var sprite = Cargar(ruta);
                    Assert.AreEqual(new Vector2(grupo.Ancho, grupo.Alto), sprite.rect.size, $"{ruta}: el lienzo no se recorta ni se reescala.");
                }
            }
        }

        [Test]
        public void CadaSprite_TieneLosPixelesPorUnidadYElPivoteDeSuCarpeta()
        {
            foreach (var grupo in Grupos)
            {
                foreach (var ruta in grupo.Rutas)
                {
                    var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
                    var ajustes = new TextureImporterSettings();
                    importador.ReadTextureSettings(ajustes);

                    Assert.AreEqual(grupo.PixelesPorUnidad, importador.spritePixelsPerUnit, $"{ruta}: píxeles por unidad.");
                    Assert.AreEqual(grupo.Alineacion, (SpriteAlignment)ajustes.spriteAlignment, $"{ruta}: tipo de pivote.");
                    Assert.AreEqual(grupo.Pivote.x, ajustes.spritePivot.x, 0.0006f, $"{ruta}: pivote X.");
                    Assert.AreEqual(grupo.Pivote.y, ajustes.spritePivot.y, 0.0006f, $"{ruta}: pivote Y.");

                    Assert.AreEqual(TextureImporterType.Sprite, importador.textureType, ruta);
                    Assert.AreEqual(SpriteImportMode.Single, importador.spriteImportMode, $"{ruta}: archivos sueltos, sin hojas.");
                    Assert.AreEqual(FilterMode.Point, importador.filterMode, ruta);
                    Assert.AreEqual(TextureImporterCompression.Uncompressed, importador.textureCompression, ruta);
                    Assert.AreEqual(SpriteMeshType.FullRect, ajustes.spriteMeshType, ruta);
                    Assert.IsFalse(importador.mipmapEnabled, $"{ruta}: sin mipmaps.");
                    Assert.AreEqual(TextureWrapMode.Clamp, importador.wrapMode, ruta);
                }
            }
        }

        [Test]
        public void LosPixelesPorUnidad_CambianPorCarpeta_YUnPixelDeArteMideLoMismo()
        {
            float Ppu(string carpeta, string archivo) => Cargar(Ruta(carpeta, archivo)).pixelsPerUnit;

            // El Golem y la torreta tienen los bloques del doble que la lava y las plataformas.
            Assert.AreEqual(24f, Ppu(Golem, "Golem_I1"));
            Assert.AreEqual(24f, Ppu(Mecanicas, "Torreta_1_reposo"));
            Assert.AreEqual(48f, Ppu(Mecanicas, "Lava"));
            Assert.AreEqual(48f, Ppu(Mecanicas, "Plataforma_1_intacta"));
            Assert.AreEqual(48f, Ppu(Mecanicas, "Combustion_1_apagada"));

            // Tamaños del encargo.
            var lava = Cargar(Ruta(Mecanicas, "Lava")).bounds.size;
            Assert.AreEqual(3.15f, lava.x, 0.01f, "La lava mide 3,15 u de ancho.");
            Assert.AreEqual(1.33f, lava.y, 0.01f, "La lava mide 1,33 u de alto.");
            Assert.AreEqual(3.83f, Cargar(Ruta(Mecanicas, "Plataforma_1_intacta")).bounds.size.x, 0.01f, "La plataforma mide 3,83 u de ancho.");
            Assert.AreEqual(3.83f, Cargar(Ruta(Golem, "Golem_I1")).bounds.size.y, 0.01f, "El lienzo del Golem mide 92 / 24 = 3,83 u de alto.");

            // Un píxel de arte: 1/24 u en el Golem es el doble de grande que 1/48 en la lava, y
            // los dos se dibujaron con bloques en esa misma proporción (20,48 y 10,24 px).
            Assert.AreEqual(2f, Ppu(Mecanicas, "Lava") / Ppu(Golem, "Golem_I1"));
        }

        [Test]
        public void LosPivotes_CaenEnElSuelo_YEnLaSuperficieQueSePisa()
        {
            // El Golem apoya en la línea de suelo de la fila 88 en los 15 fotogramas.
            foreach (var ruta in Grupos[0].Rutas)
            {
                Assert.AreEqual(88, FilasOpacas(ruta).abajo, $"{ruta}: la línea de suelo es la fila 88 (deriva 0 px).");
                Assert.AreEqual(88f, FilaDelPivote(ruta), 0.5f, $"{ruta}: el pivote cae en la línea de suelo.");
            }

            // La torreta, en la base del bloque (y 1-40).
            foreach (var archivo in new[] { "Torreta_1_reposo", "Torreta_2_carga", "Torreta_3_disparo" })
            {
                var ruta = Ruta(Mecanicas, archivo);
                Assert.AreEqual((1, 40), FilasOpacas(ruta), ruta);
                Assert.AreEqual(40f, FilaDelPivote(ruta), 0.5f, $"{ruta}: el pivote cae en la base del bloque.");
            }

            // Las plataformas, en la superficie que se pisa.
            foreach (var archivo in new[] { "Plataforma_1_intacta", "Plataforma_2_agrietada" })
            {
                var ruta = Ruta(Mecanicas, archivo);
                Assert.AreEqual(21, FilasOpacas(ruta).arriba, $"{ruta}: la superficie es la fila 21.");
                Assert.AreEqual(21f, FilaDelPivote(ruta), 0.5f, $"{ruta}: el pivote cae en la superficie.");
            }

            var apagada = Ruta(Mecanicas, "Combustion_1_apagada");
            Assert.AreEqual(26, FilasOpacas(apagada).arriba, "La superficie de la combustión es la fila 26.");
            Assert.AreEqual(26f, FilaDelPivote(apagada), 0.5f, "El pivote de la combustión cae en la superficie.");
            Assert.AreEqual(0, FilasOpacas(Ruta(Mecanicas, "Combustion_3_encendida")).arriba, "Encendida, la llama sube hasta la fila 0.");

            // La lava, en la costra: la esquina de arriba a la izquierda.
            Assert.AreEqual(0, FilasOpacas(Ruta(Mecanicas, "Lava")).arriba, "La costra está en la fila 0.");
            Assert.AreEqual(Vector2.zero, Cargar(Ruta(Mecanicas, "Lava")).pivot - new Vector2(0f, 64f));
        }

        [Test]
        public void LaTorreta_NoSaltaAlDisparar()
        {
            // Al disparar, el bloque está dibujado 15 px más a la izquierda: su pivote corrido
            // lo deja en el mismo sitio del mundo en los tres fotogramas.
            float BordeIzquierdo(string archivo)
            {
                var ruta = Ruta(Mecanicas, archivo);
                return (PrimeraColumnaOpaca(ruta, 30) - Cargar(ruta).pivot.x) / 24f;
            }

            var reposo = BordeIzquierdo("Torreta_1_reposo");
            Assert.AreEqual(reposo, BordeIzquierdo("Torreta_2_carga"), 0.5f / 24f, "La carga no mueve el bloque.");
            Assert.AreEqual(reposo, BordeIzquierdo("Torreta_3_disparo"), 0.5f / 24f, "El disparo no mueve el bloque.");
            Assert.AreEqual(16, PrimeraColumnaOpaca(Ruta(Mecanicas, "Torreta_1_reposo"), 30));
            Assert.AreEqual(1, PrimeraColumnaOpaca(Ruta(Mecanicas, "Torreta_3_disparo"), 30), "El PNG del disparo no se toca.");
        }

        [Test]
        public void LaTablaDeImportacion_DaLaReglaMasConcreta_YUnaSolaPorRuta()
        {
            Assert.AreEqual(0.470f, TablaArte.ReglaDe(Ruta(Mecanicas, "Torreta_1_reposo")).Pivote.Value.x, 0.0001f);
            Assert.AreEqual(0.287f, TablaArte.ReglaDe(Ruta(Mecanicas, "Torreta_3_disparo")).Pivote.Value.x, 0.0006f);
            Assert.AreEqual(48f, TablaArte.ReglaDe(Ruta(Mecanicas, "Lava_espejo")).PixelesPorUnidad);

            Assert.AreEqual(ConstructorCentinela.PixelesPorUnidadIdle, TablaArte.ReglaDe(ConstructorCentinela.RutaSprite("I1")).PixelesPorUnidad);
            Assert.AreEqual(ConstructorCentinela.PixelesPorUnidad, TablaArte.ReglaDe(ConstructorCentinela.RutaSprite("E1")).PixelesPorUnidad);
            Assert.AreEqual(ConstructorEcoIra.PixelesPorUnidad, TablaArte.ReglaDe(ConstructorEcoIra.CarpetaSprites + "/i1.png").PixelesPorUnidad);

            Assert.IsNull(TablaArte.ReglaDe("Assets/Sprites/Kael/Kael_Idle.png"), "Kael lo ajusta su constructor (hojas cortadas).");
            Assert.IsNull(TablaArte.ReglaDe("Assets/Sprites/Fondos/Mundo1Ira/capa.png"), "Los fondos van por capas, fuera de la tabla.");

            var repetidas = TablaArte.Reglas.GroupBy(r => r.Ruta).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            CollectionAssert.IsEmpty(repetidas, "Dos reglas para la misma ruta: ¿cuál vale?");
        }

        [Test]
        public void LaTablaYaEstaAplicada_ATodoElArteConRegla()
        {
            // También a la Centinela y al Eco, que antes tenían su propio importador: pasar a la
            // tabla no les cambió ningún ajuste.
            Assert.AreEqual(0, TablaArte.AplicarATodo(), "Algún sprite con regla tenía otros ajustes.");
        }

        // --- Fase B: clips ---

        private const float SeQueda = 1f / 60f;

        private sealed class ClipEsperado
        {
            public string Ruta;
            public bool Bucle;
            public (string sprite, float duracion)[] Fotogramas;

            public float Duracion => Fotogramas.Sum(f => f.duracion);
        }

        private const string AnimGolem = "Assets/Animaciones/Enemigos/GolemCeniza";
        private const string AnimMecanicas = "Assets/Animaciones/Mecanicas";

        // Sección 3 del encargo, copiada tal cual.
        private static readonly ClipEsperado[] ClipsGolem =
        {
            new ClipEsperado
            {
                Ruta = AnimGolem + "/Golem_Idle.anim", Bucle = true,
                Fotogramas = new[] { ("Golem_I1", 0.40f), ("Golem_I2", 0.40f), ("Golem_I3", 0.40f), ("Golem_I2", 0.40f) },
            },
            new ClipEsperado
            {
                Ruta = AnimGolem + "/Golem_Andar.anim", Bucle = true,
                Fotogramas = new[] { ("Golem_W1", 0.13f), ("Golem_W2", 0.13f), ("Golem_W3", 0.13f), ("Golem_W4", 0.13f) },
            },
            new ClipEsperado
            {
                Ruta = AnimGolem + "/Golem_Ataque.anim",
                Fotogramas = new[] { ("Golem_A1", 0.50f), ("Golem_A3", 0.08f), ("Golem_A4", 0.14f), ("Golem_A5", 0.26f) },
            },
            new ClipEsperado
            {
                Ruta = AnimGolem + "/Golem_Muerte.anim",
                Fotogramas = new[] { ("Golem_D1", 0.35f), ("Golem_D2", 0.45f), ("Golem_D3", 0.50f), ("Golem_D4", SeQueda) },
            },
        };

        // Sección 4: cada estado de la mecánica, un fotograma que se queda (el tiempo lo pone el
        // script). Al romperse, la plataforma se ve partida un momento y después el polvo.
        private static readonly ClipEsperado[] ClipsMecanicas =
        {
            new ClipEsperado { Ruta = AnimMecanicas + "/Torreta_Reposo.anim", Fotogramas = new[] { ("Torreta_1_reposo", SeQueda) } },
            new ClipEsperado { Ruta = AnimMecanicas + "/Torreta_Carga.anim", Fotogramas = new[] { ("Torreta_2_carga", SeQueda) } },
            new ClipEsperado { Ruta = AnimMecanicas + "/Torreta_Disparo.anim", Fotogramas = new[] { ("Torreta_3_disparo", SeQueda) } },
            new ClipEsperado { Ruta = AnimMecanicas + "/Plataforma_Intacta.anim", Fotogramas = new[] { ("Plataforma_1_intacta", SeQueda) } },
            new ClipEsperado { Ruta = AnimMecanicas + "/Plataforma_Agrietada.anim", Fotogramas = new[] { ("Plataforma_2_agrietada", SeQueda) } },
            new ClipEsperado { Ruta = AnimMecanicas + "/Plataforma_Rompe.anim", Fotogramas = new[] { ("Plataforma_3_rota", 0.25f), ("Plataforma_4_polvo", SeQueda) } },
            new ClipEsperado { Ruta = AnimMecanicas + "/Combustion_Apagada.anim", Fotogramas = new[] { ("Combustion_1_apagada", SeQueda) } },
            new ClipEsperado { Ruta = AnimMecanicas + "/Combustion_Calienta.anim", Fotogramas = new[] { ("Combustion_2_calienta", SeQueda) } },
            new ClipEsperado { Ruta = AnimMecanicas + "/Combustion_Encendida.anim", Fotogramas = new[] { ("Combustion_3_encendida", SeQueda) } },
        };

        private static AnimationClip CargarClip(string ruta)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
            Assert.IsNotNull(clip, $"Falta el clip {ruta}.");
            return clip;
        }

        /// <summary>El sprite que muestra el clip en el instante t.</summary>
        private static string SpriteEn(AnimationClip clip, float t)
        {
            var claves = AnimationUtility.GetObjectReferenceCurve(clip, ClipsDeSprites.BindingSprite);
            var visible = claves.Where(c => c.time <= t + 1e-4f).OrderBy(c => c.time).LastOrDefault();
            return visible.value != null ? visible.value.name : null;
        }

        private static void ComprobarClip(ClipEsperado esperado)
        {
            var clip = CargarClip(esperado.Ruta);
            Assert.AreEqual(esperado.Duracion, clip.length, 0.001f, $"{esperado.Ruta}: dura lo que suma la tabla.");
            Assert.AreEqual(esperado.Bucle, AnimationUtility.GetAnimationClipSettings(clip).loopTime, $"{esperado.Ruta}: bucle.");

            var inicio = 0f;
            foreach (var (sprite, duracion) in esperado.Fotogramas)
            {
                Assert.AreEqual(sprite, SpriteEn(clip, inicio + 0.001f), $"{esperado.Ruta}: a los {inicio:0.###} s empieza {sprite}.");
                Assert.AreEqual(sprite, SpriteEn(clip, inicio + duracion - 0.002f), $"{esperado.Ruta}: {sprite} dura {duracion:0.###} s.");
                inicio += duracion;
            }
        }

        [Test]
        public void LosClipsDelGolem_TienenLosTiemposDelEncargo()
        {
            foreach (var esperado in ClipsGolem)
            {
                ComprobarClip(esperado);
            }
        }

        [Test]
        public void ElAtaqueDelGolem_NoEstaUniformado_YVaDeA1AA3()
        {
            var ataque = ClipsGolem[2].Fotogramas;
            Assert.GreaterOrEqual(ataque[0].duracion, 6f * ataque[1].duracion, "A1 (anticipación) tiene que durar mucho más que A3 (impacto).");
            Assert.AreEqual(0.08f, ConstructorGolem.ClipDe(ConstructorGolem.Ataque).Duraciones.Min(), 1e-5f, "A3 es el fotograma más corto.");
            Assert.AreEqual(4, ConstructorGolem.ClipDe(ConstructorGolem.Ataque).Duraciones.Distinct().Count(), "Las duraciones del ataque no se reparten por igual.");

            var todos = ConstructorGolem.Clips.SelectMany(c => c.Fotogramas).ToList();
            CollectionAssert.DoesNotContain(todos, "Golem_A2", "No hay A2: el salto de A1 a A3 es el golpe.");
            Assert.AreEqual(15, todos.Distinct().Count(), "Los 15 fotogramas del Golem se usan.");
        }

        [Test]
        public void LaMuerteDelGolem_SeQuedaEnD4()
        {
            var muerte = CargarClip(AnimGolem + "/Golem_Muerte.anim");
            Assert.IsFalse(AnimationUtility.GetAnimationClipSettings(muerte).loopTime);
            Assert.AreEqual("Golem_D4", SpriteEn(muerte, 100f), "Sin bucle, el clip se detiene en D4.");
        }

        [Test]
        public void LosClipsDeLasMecanicas_SonLosEstadosQueSostieneSuScript()
        {
            foreach (var esperado in ClipsMecanicas)
            {
                ComprobarClip(esperado);
                Assert.AreEqual(esperado.Fotogramas.Last().sprite, SpriteEn(CargarClip(esperado.Ruta), 100f), $"{esperado.Ruta}: el último fotograma se queda.");
            }

            // Los 10 fotogramas de la torreta y las plataformas se usan; la lava no tiene clip.
            var usados = ConstructorMecanicas.TodosLosClips.SelectMany(c => c.Fotogramas).Distinct().ToList();
            Assert.AreEqual(10, usados.Count);
            // Solo los de esta carpeta: los de las mecánicas de los demás mundos van en subcarpetas.
            var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimMecanicas }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(c => System.IO.Path.GetDirectoryName(c)?.Replace('\\', '/') == AnimMecanicas).ToList();
            Assert.AreEqual(ClipsMecanicas.Length + 1, clips.Count, "Y el vuelo de la bola:\n" + string.Join("\n", clips));
            Assert.IsFalse(clips.Any(c => c.Contains("Lava")), "La lava es un solo fotograma: no tiene clip.");
        }

        [Test]
        public void LaBola_VuelaEnBucleConDosFotogramas_YDejaSusChispas()
        {
            ComprobarClip(new ClipEsperado
            {
                Ruta = AnimMecanicas + "/Bola_Vuelo.anim", Bucle = true,
                Fotogramas = new[] { ("Bola_1_vuelo", 0.1f), ("Bola_2_vuelo", 0.1f) },
            });

            var controlador = CargarControlador(AnimMecanicas + "/Bola.controller");
            var estados = controlador.layers[0].stateMachine.states;
            Assert.AreEqual(1, estados.Length, "Un solo estado: el vuelo.");
            Assert.AreEqual("Bola_Vuelo", estados[0].state.motion.name);
            Assert.AreEqual(0, controlador.parameters.Length, "No la gobierna ningún script.");

            // Las tres comparten lienzo y pivote: el centro de la bola, no del lienzo.
            foreach (var nombre in new[] { "Bola_1_vuelo", "Bola_2_vuelo", "Bola_3_disipa" })
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Mecanicas/{nombre}.png");
                Assert.IsNotNull(sprite, nombre);
                Assert.AreEqual(new Vector2(40f, 29f), sprite.rect.size, $"{nombre}: lienzo.");
                Assert.AreEqual(24f, sprite.pixelsPerUnit, $"{nombre}: la densidad de la torreta.");
                Assert.AreEqual(0.688f * 40f, sprite.pivot.x, 0.05f, $"{nombre}: pivote x.");
                Assert.AreEqual(0.414f * 29f, sprite.pivot.y, 0.05f, $"{nombre}: pivote y.");
            }
        }

        // --- Fase C: Animators ---

        private const string ControladorGolem = AnimGolem + "/GolemCeniza.controller";

        private static readonly (string ruta, System.Type estados, string[] nombres)[] ControladoresMecanicas =
        {
            (AnimMecanicas + "/Torreta.controller", typeof(ESTADOS_TORRETA), new[] { "Reposo", "Carga", "Disparo" }),
            (AnimMecanicas + "/Plataforma.controller", typeof(ESTADOS_PLATAFORMA), new[] { "Intacta", "Agrietada", "Rompe" }),
            (AnimMecanicas + "/Combustion.controller", typeof(ESTADOS_COMBUSTION), new[] { "Apagada", "Calienta", "Encendida" }),
        };

        private static AnimatorController CargarControlador(string ruta)
        {
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta);
            Assert.IsNotNull(controlador, $"Falta el Animator {ruta}.");
            return controlador;
        }

        private static Dictionary<string, AnimatorState> EstadosDe(AnimatorController controlador) =>
            controlador.layers[0].stateMachine.states.Select(s => s.state).ToDictionary(s => s.name);

        private static AnimatorStateTransition Transicion(AnimatorState desde, string nombre)
        {
            var transicion = desde.transitions.FirstOrDefault(t => t.name == nombre);
            Assert.IsNotNull(transicion, $"Falta la transición «{nombre}» desde {desde.name}.");
            return transicion;
        }

        private static bool Pide(AnimatorStateTransition transicion, string parametro, AnimatorConditionMode modo) =>
            transicion.conditions.Any(c => c.parameter == parametro && c.mode == modo);

        [Test]
        public void ElAnimatorDelGolem_TieneUnEstadoPorClip_YEmpiezaEnIdle()
        {
            var controlador = CargarControlador(ControladorGolem);
            var estados = EstadosDe(controlador);
            CollectionAssert.AreEquivalent(new[] { "Idle", "Andar", "Ataque", "Muerte" }, estados.Keys);
            Assert.AreEqual("Idle", controlador.layers[0].stateMachine.defaultState.name);

            foreach (var esperado in ClipsGolem)
            {
                var estado = System.IO.Path.GetFileNameWithoutExtension(esperado.Ruta).Replace("Golem_", string.Empty);
                Assert.AreEqual(CargarClip(esperado.Ruta), estados[estado].motion, $"{estado} reproduce {esperado.Ruta}.");
            }

            // Los parámetros que ya escribe la FSM del enemigo y EnemigoAnimacion.
            CollectionAssert.AreEquivalent(new[] { "MovEn", "Attack", "Damage", "Death", "VelocidadCorrer", "VelocidadAtaque" },
                controlador.parameters.Select(p => p.name));
            Assert.AreEqual("VelocidadCorrer", estados["Andar"].speedParameter, "El andar sigue la velocidad real.");
            Assert.IsTrue(estados["Andar"].speedParameterActive);
            Assert.AreEqual("VelocidadAtaque", estados["Ataque"].speedParameter);
        }

        [Test]
        public void ElGolem_AtacaDesdeIdleOAndando_YAlAcabarVuelveAIdle()
        {
            var estados = EstadosDe(CargarControlador(ControladorGolem));

            foreach (var (desde, nombre) in new[] { ("Idle", "Ya a tiro"), ("Andar", "Entra en rango") })
            {
                var atacar = Transicion(estados[desde], nombre);
                Assert.AreEqual(estados["Ataque"], atacar.destinationState);
                Assert.IsTrue(Pide(atacar, "Attack", AnimatorConditionMode.If));
                Assert.AreEqual(atacar, estados[desde].transitions[0], $"Desde {desde}, atacar va primero.");
            }

            var termina = Transicion(estados["Ataque"], "Termina el golpe");
            Assert.AreEqual(estados["Idle"], termina.destinationState);
            Assert.IsTrue(termina.hasExitTime);
            Assert.AreEqual(1f, termina.exitTime, "El ataque se ve entero: A1, A3, A4 y A5.");

            var interrumpido = Transicion(estados["Ataque"], "Golpe interrumpido");
            Assert.IsTrue(Pide(interrumpido, "Attack", AnimatorConditionMode.IfNot));
        }

        [Test]
        public void ElGolem_ParadoRespira_YSoloAndaSiSeMueve()
        {
            var estados = EstadosDe(CargarControlador(ControladorGolem));

            var anda = Transicion(estados["Idle"], "Echa a andar");
            Assert.AreEqual(estados["Andar"], anda.destinationState);
            Assert.IsTrue(Pide(anda, "MovEn", AnimatorConditionMode.If));
            var umbralAnda = anda.conditions.Single(c => c.parameter == "VelocidadCorrer" && c.mode == AnimatorConditionMode.Greater).threshold;

            var para = Transicion(estados["Andar"], "Se para");
            Assert.AreEqual(estados["Idle"], para.destinationState);
            var umbralPara = para.conditions.Single(c => c.parameter == "VelocidadCorrer" && c.mode == AnimatorConditionMode.Less).threshold;
            Assert.Less(umbralPara, umbralAnda, "Con un margen entre los dos, no parpadea entre Idle y Andar al arrancar o frenar.");

            Assert.IsTrue(Pide(Transicion(estados["Andar"], "Lo pierde"), "MovEn", AnimatorConditionMode.IfNot));
        }

        [Test]
        public void LaMuerteDelGolem_InterrumpeCualquierEstado_YNoTieneSalida()
        {
            var controlador = CargarControlador(ControladorGolem);
            var maquina = controlador.layers[0].stateMachine;
            var muere = maquina.anyStateTransitions.Single();
            Assert.AreEqual("Muerte", muere.destinationState.name);
            Assert.IsTrue(Pide(muere, "Death", AnimatorConditionMode.If));
            Assert.IsFalse(muere.canTransitionToSelf, "Si volviera a sí misma, la muerte se reiniciaría en cada fotograma.");
            Assert.IsEmpty(EstadosDe(controlador)["Muerte"].transitions, "De la muerte no se sale: se queda en D4.");
        }

        [Test]
        public void CadaMecanica_SaltaAlEstadoQuePideSuScript()
        {
            foreach (var (ruta, tipo, nombres) in ControladoresMecanicas)
            {
                var controlador = CargarControlador(ruta);
                var maquina = controlador.layers[0].stateMachine;
                var estados = EstadosDe(controlador);

                // Un solo parámetro, el entero que pone el script; ninguno del jugador.
                Assert.AreEqual(1, controlador.parameters.Length, ruta);
                Assert.AreEqual("Estado", controlador.parameters[0].name, ruta);
                Assert.AreEqual(AnimatorControllerParameterType.Int, controlador.parameters[0].type, ruta);

                // Un estado por valor del enum, en su orden, y empieza en el primero.
                var valores = System.Enum.GetValues(tipo);
                Assert.AreEqual(valores.Length, nombres.Length, $"{ruta}: un estado por valor de {tipo.Name}.");
                CollectionAssert.AreEquivalent(nombres, estados.Keys, ruta);
                Assert.AreEqual(nombres[0], maquina.defaultState.name, ruta);

                for (var i = 0; i < nombres.Length; i++)
                {
                    var salto = maquina.anyStateTransitions.SingleOrDefault(t => t.destinationState == estados[nombres[i]]);
                    Assert.IsNotNull(salto, $"{ruta}: nada lleva a {nombres[i]}.");
                    var condicion = salto.conditions.Single();
                    Assert.AreEqual(AnimatorConditionMode.Equals, condicion.mode, ruta);
                    Assert.AreEqual(i, (int)condicion.threshold, $"{ruta}: {nombres[i]} es Estado = {i} ({valores.GetValue(i)}).");
                    Assert.IsFalse(salto.hasExitTime, $"{ruta}: el cambio es inmediato; el tiempo lo pone el script.");
                    Assert.IsFalse(salto.canTransitionToSelf, ruta);
                    Assert.IsEmpty(estados[nombres[i]].transitions, $"{ruta}: {nombres[i]} no cambia solo.");
                }
            }
        }

        [Test]
        public void NingunAnimatorDelNivel1_TieneEstadosSueltosNiTransicionesRotas()
        {
            foreach (var ruta in ControladoresMecanicas.Select(c => c.ruta).Prepend(ControladorGolem))
            {
                var controlador = CargarControlador(ruta);
                var maquina = controlador.layers[0].stateMachine;
                var parametros = controlador.parameters.Select(p => p.name).ToList();
                var transiciones = maquina.states.SelectMany(s => s.state.transitions).Concat(maquina.anyStateTransitions).ToList();

                Assert.IsEmpty(maquina.stateMachines, $"{ruta}: sin submáquinas.");
                foreach (var transicion in transiciones)
                {
                    Assert.IsNotNull(transicion.destinationState, $"{ruta}: «{transicion.name}» no lleva a ningún estado.");
                    Assert.IsTrue(transicion.hasExitTime || transicion.conditions.Length > 0, $"{ruta}: «{transicion.name}» no tiene condición ni tiempo de salida.");
                    Assert.AreEqual(0f, transicion.duration, $"{ruta}: «{transicion.name}» mezclaría fotogramas.");
                    foreach (var condicion in transicion.conditions)
                    {
                        CollectionAssert.Contains(parametros, condicion.parameter, $"{ruta}: «{transicion.name}» usa un parámetro que no existe.");
                    }
                }

                foreach (var estado in maquina.states.Select(s => s.state))
                {
                    Assert.IsNotNull(estado.motion, $"{ruta}: {estado.name} sin clip.");
                    Assert.IsTrue(estado == maquina.defaultState || transiciones.Any(t => t.destinationState == estado), $"{ruta}: nada lleva a {estado.name}.");
                }
            }
        }

        [Test]
        public void RegenerarClipsYAnimators_NoCambiaNada()
        {
            var rutas = ClipsGolem.Concat(ClipsMecanicas).Select(c => c.Ruta)
                .Concat(ControladoresMecanicas.Select(c => c.ruta)).Append(ControladorGolem).ToList();
            var antes = rutas.ToDictionary(r => r, File.ReadAllText);

            Assert.IsTrue(ArteNivel1.Construir());

            foreach (var ruta in rutas)
            {
                Assert.AreEqual(antes[ruta], File.ReadAllText(ruta), $"{ruta} cambió al regenerar.");
            }
        }

        // --- Fase D: scripts y prefabs ---

        private const string PrefabGolem = "Assets/Prefabs/Enemigos/GolemCeniza.prefab";
        private const string PrefabsMecanicas = "Assets/Prefabs/Mecanicas";

        private static GameObject CargarPrefab(string ruta)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, $"Falta el prefab {ruta}.");
            return prefab;
        }

        private static T Campo<T>(Object objeto, string campo) where T : Object =>
            new SerializedObject(objeto).FindProperty(campo).objectReferenceValue as T;

        private static Rect Caja(BoxCollider2D caja) => new Rect(caja.offset - caja.size / 2f, caja.size);

        [Test]
        public void ElPrefabDelGolem_EsUnEnemigoConSuArte_YSusDatos()
        {
            var prefab = CargarPrefab(PrefabGolem);
            var enemigo = prefab.GetComponent<Fragmentum.Enemies.EnemyController>();
            Assert.IsNotNull(enemigo, "El Golem es un enemigo más (EnemyController), no un jefe.");

            var datos = enemigo.Datos;
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Fragmentum.Data.EnemyData>("Assets/Data/Enemigos/GolemCeniza.asset"), datos);
            Assert.IsTrue(datos.hiperarmadura, "Los golpes no le cortan el ataque.");
            Assert.IsTrue(datos.spriteMiraDerecha, "Está dibujado mirando a la derecha.");
            Assert.AreEqual(Color.white, datos.tinte, "Con arte, sin tinte.");
            Assert.IsTrue(datos.inmuneALava);

            var animacion = prefab.GetComponentInChildren<Fragmentum.Enemies.EnemigoAnimacion>(true);
            Assert.IsNotNull(animacion);
            var animator = animacion.GetComponent<Animator>();
            Assert.AreEqual(CargarControlador(ControladorGolem), animator.runtimeAnimatorController);
            Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, animator.cullingMode, "Fuera de cámara también: si no, los eventos no llegan.");
            Assert.AreEqual(Vector3.zero, animacion.transform.localPosition, "El sprite en los pies: el pivote del arte es el suelo.");
            Assert.AreEqual("Golem_I1", animacion.GetComponent<SpriteRenderer>().sprite.name);
            Assert.AreEqual(ConstructorGolem.VelocidadNaturalAndar, animacion.VelocidadNaturalCarrera);
            Assert.AreEqual("Assets/Sprites/Enemigos/Materiales/Destello.mat", AssetDatabase.GetAssetPath(Campo<Material>(animacion, "materialDestello")),
                "Destella con la silueta (el color del SpriteRenderer no aclara el gris).");
            Assert.AreEqual(Color.white, animacion.ColorDestello);
            Assert.Greater(animacion.RestosEnPantalla, 0f, "Los restos (D4) se quedan un momento.");

            var arma = prefab.transform.Find("Pivote/Arma");
            Assert.IsFalse(arma.gameObject.activeSelf, "La onda está apagada en reposo.");
            var onda = Caja(arma.GetComponent<BoxCollider2D>());
            var centro = (Vector2)arma.localPosition;
            Assert.Greater(onda.width, 2f * onda.height, "La onda es ancha y baja.");
            Assert.Less(centro.y + onda.yMax, 0.7f, "Baja: se esquiva saltando.");
            Assert.Greater(centro.x + onda.xMax, 2f, "Llega más allá del puño que toca el suelo.");
            Assert.IsTrue(prefab.transform.Find("Contacto").gameObject.activeSelf, "El contacto con el cuerpo hace daño siempre.");
        }

        [Test]
        public void LosEventosDelGolem_CaenEnSuFotograma_YLlamanAEnemigoAnimacion()
        {
            var esperados = new (string clip, string funcion, float tiempo)[]
            {
                ("Golem_Ataque", "AbrirGolpe", 0.50f),
                ("Golem_Ataque", "CerrarGolpe", 0.58f),
                ("Golem_Ataque", "FinAtaque", 0.98f),
                ("Golem_Muerte", "FinMuerte", 1.30f),
            };

            foreach (var (clip, funcion, tiempo) in esperados)
            {
                var eventos = CargarClip($"{AnimGolem}/{clip}.anim").events;
                var evento = eventos.SingleOrDefault(e => e.functionName == funcion);
                Assert.IsNotNull(evento, $"{clip}: falta el evento {funcion}.");
                Assert.AreEqual(tiempo, evento.time, 0.001f, $"{clip}: {funcion}.");

                var metodo = typeof(Fragmentum.Enemies.EnemigoAnimacion).GetMethod(funcion);
                Assert.IsNotNull(metodo, $"{funcion} no existe en EnemigoAnimacion.");
                Assert.IsTrue(metodo.IsPublic && metodo.ReturnType == typeof(void) && metodo.GetParameters().Length == 0, funcion);
            }

            Assert.IsEmpty(CargarClip($"{AnimGolem}/Golem_Idle.anim").events);
            Assert.IsEmpty(CargarClip($"{AnimGolem}/Golem_Andar.anim").events);
        }

        [Test]
        public void LosPrefabsDeLasMecanicas_LlevanSuArte_YColliders_MedidosEnElDibujo()
        {
            // Torreta: el bloque (x 16-61, y 1-40) es suelo; la llama del disparo llega a x 81.
            var torreta = CargarPrefab($"{PrefabsMecanicas}/TorretaFuego.prefab");
            var bloque = Caja(torreta.GetComponent<BoxCollider2D>());
            Assert.AreEqual(46f / 24f, bloque.width, 0.01f);
            Assert.AreEqual(40f / 24f, bloque.height, 0.01f);
            Assert.AreEqual(0f, bloque.yMin, 0.05f, "El pivote cae en la base del bloque.");
            var llamaTorreta = torreta.transform.Find("Llama").GetComponent<BoxCollider2D>();
            Assert.IsTrue(llamaTorreta.isTrigger);
            Assert.AreEqual((82f - 0.287f * 82f) / 24f, Caja(llamaTorreta).xMax, 0.02f, "La llama llega hasta x 81 del disparo (con su pivote corrido).");
            Assert.Greater(Caja(llamaTorreta).xMin, bloque.xMax - 0.1f, "La llama empieza delante del bloque.");
            Assert.AreEqual(CargarPrefab($"{PrefabsMecanicas}/BolaDeFuego.prefab"), Campo<Component>(torreta.GetComponent<TorretaFuego>(), "bola")?.gameObject);
            ComprobarDibujo(torreta, "Torreta_1_reposo", "Torreta");

            // Plataforma inestable: de la superficie (fila 21) hacia abajo, todo el ancho.
            var plataforma = CargarPrefab($"{PrefabsMecanicas}/PlataformaInestable.prefab");
            var cuerpo = Caja(plataforma.GetComponent<BoxCollider2D>());
            Assert.AreEqual(184f / 48f, cuerpo.width, 0.01f);
            Assert.AreEqual(42f / 48f, cuerpo.height, 0.01f);
            Assert.AreEqual(0f, cuerpo.yMax, 0.01f, "La superficie a la altura del Transform.");
            Assert.IsTrue(plataforma.GetComponent<BoxCollider2D>().enabled, "Intacta, sostiene.");
            ComprobarDibujo(plataforma, "Plataforma_1_intacta", "Plataforma");

            // Combustión: la superficie (fila 26) y la base (fila 87); la llama, encima.
            var combustion = CargarPrefab($"{PrefabsMecanicas}/PlataformaCombustion.prefab");
            var base_ = Caja(combustion.GetComponent<BoxCollider2D>());
            Assert.AreEqual(164f / 48f, base_.width, 0.01f);
            Assert.AreEqual(62f / 48f, base_.height, 0.01f);
            Assert.AreEqual(0f, base_.yMax, 0.01f, "La superficie a la altura del Transform.");
            Assert.AreEqual(GameTags.HotPlatform, combustion.tag);
            var llamaCombustion = Caja(combustion.transform.Find("Llama").GetComponent<BoxCollider2D>());
            Assert.AreEqual(26f / 48f, llamaCombustion.yMax, 0.01f, "La llama sube hasta la fila 0.");
            ComprobarDibujo(combustion, "Combustion_1_apagada", "Combustion");

            // Lava: el trigger desde la costra (fila 0) hacia abajo, y sus dos franjas.
            var lava = CargarPrefab($"{PrefabsMecanicas}/Lava.prefab");
            var zona = lava.GetComponent<BoxCollider2D>();
            Assert.IsTrue(zona.isTrigger);
            Assert.AreEqual(151f / 48f, zona.size.x, 0.01f);
            Assert.AreEqual(64f / 48f, zona.size.y, 0.01f);
            Assert.AreEqual(0f, Caja(zona).yMax, 0.01f, "El daño empieza en la costra, no más abajo.");
            Assert.AreEqual("Lava", Campo<Sprite>(lava.GetComponent<Lava>(), "franja").name);
            Assert.AreEqual("Lava_espejo", Campo<Sprite>(lava.GetComponent<Lava>(), "franjaEspejo").name);
            Assert.AreEqual(0, lava.transform.childCount, "Las franjas no se guardan: las monta el componente.");

            // La bola de la torreta: montada y referenciada, con su arte.
            var bola = CargarPrefab($"{PrefabsMecanicas}/BolaDeFuego.prefab");
            Assert.AreEqual(LayerMask.NameToLayer(Capas.EnemyWeapon), bola.layer);
            Assert.AreEqual(RigidbodyType2D.Kinematic, bola.GetComponent<Rigidbody2D>().bodyType);
            Assert.IsTrue(bola.GetComponent<CircleCollider2D>().isTrigger);
            Assert.IsNotNull(bola.GetComponent<Fragmentum.Core.Combate.Hitbox>());
            Assert.IsNotNull(bola.GetComponent<Fragmentum.Core.Combate.Proyectil>());
            ComprobarDibujo(bola, "Bola_1_vuelo", "Bola");
            Assert.AreEqual("Bola_3_disipa", Campo<Sprite>(bola.GetComponent<Fragmentum.Core.Combate.Proyectil>(), "restos").name,
                "Al apagarse deja sus chispas.");
            Assert.AreEqual(Color.white, bola.GetComponentInChildren<SpriteRenderer>().color, "Sin tinte: el color es el del dibujo.");
        }

        private static void ComprobarDibujo(GameObject prefab, string primerFotograma, string controlador)
        {
            var dibujo = prefab.transform.Find("Sprite");
            Assert.IsNotNull(dibujo, $"{prefab.name}: falta su dibujo.");
            Assert.AreEqual(Vector3.zero, dibujo.localPosition, $"{prefab.name}: el pivote del arte es el del prefab.");
            Assert.AreEqual(primerFotograma, dibujo.GetComponent<SpriteRenderer>().sprite.name);
            Assert.AreEqual(CargarControlador($"{AnimMecanicas}/{controlador}.controller"), dibujo.GetComponent<Animator>().runtimeAnimatorController);
        }

        [Test]
        public void EnReposo_NingunaZonaDeAtaqueEstaEncendida()
        {
            Assert.IsFalse(CargarPrefab(PrefabGolem).transform.Find("Pivote/Arma").gameObject.activeSelf, "Golem: la onda, solo en A3.");
            Assert.IsFalse(CargarPrefab($"{PrefabsMecanicas}/TorretaFuego.prefab").transform.Find("Llama").gameObject.activeSelf, "Torreta: la llama, solo al disparar.");
            Assert.IsFalse(CargarPrefab($"{PrefabsMecanicas}/PlataformaCombustion.prefab").transform.Find("Llama").gameObject.activeSelf, "Combustión: la llama, solo encendida.");
        }

        [Test]
        public void LasCinematicasDelJefe_LlevanSusPlanos_AEscalaEntera()
        {
            var presentacion = AssetDatabase.LoadAssetAtPath<Fragmentum.Narrative.CutsceneData>("Assets/Data/Narrativa/Cinematica_Jefe_Ira.asset");
            Assert.IsTrue(presentacion.escalaEntera);
            Assert.IsFalse(presentacion.sobreElJuego, "Con los planos, opaca.");
            // Por beats: negro, la sala, negro y el ojo (su guion lo comprueba CinematicaJefe1Tests).
            Assert.AreEqual("CinematicaJefe1", presentacion.diapositivas[1].imagen.name, "Kael diminuto en la nave, la Centinela esperando.");
            Assert.AreEqual("CinematicaJefe2", presentacion.diapositivas[3].imagen.name, "El ojo que se abre.");

            var derrota = AssetDatabase.LoadAssetAtPath<Fragmentum.Narrative.CutsceneData>("Assets/Data/Narrativa/Cinematica_Derrota_Ira.asset");
            Assert.IsNotNull(derrota, "Falta la cinemática de la derrota de la Centinela.");
            Assert.IsTrue(derrota.escalaEntera);
            Assert.IsFalse(derrota.soloPrimeraVez);
            Assert.AreEqual("CinematicaFinalLvl1", derrota.diapositivas[0].imagen.name, "La sala vacía, la espada clavada.");
            Assert.IsTrue(derrota.diapositivas.All(d => d.imagen != null), "Sin plano no hay diapositiva.");
            Assert.AreEqual("CinematicaFinalLvl2", derrota.diapositivas[1].imagen.name, "Kael con el fragmento en el hombro y los huecos que faltan.");

            var nivel = AssetDatabase.LoadAssetAtPath<Fragmentum.Data.LevelDatabase>("Assets/Data/Niveles/LevelDatabase.asset").niveles
                .Single(n => n != null && n.cinematicaJefe == presentacion);
            Assert.AreEqual(derrota, nivel.cinematicaJefeDerrotado, "El Nivel 1 la reproduce cuando la Centinela termina de caer.");
        }

        [Test]
        public void UnPNGNuevoEnMecanicas_SeImportaConLaReglaDeSuGrupo()
        {
            var ruta = Ruta(Mecanicas, "Plataforma_prueba");
            var textura = new Texture2D(4, 4);
            try
            {
                File.WriteAllBytes(ruta, textura.EncodeToPNG());
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);

                var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
                var ajustes = new TextureImporterSettings();
                importador.ReadTextureSettings(ajustes);
                Assert.AreEqual(48f, importador.spritePixelsPerUnit);
                Assert.AreEqual(0.5f, ajustes.spritePivot.x, 0.0001f);
                Assert.AreEqual(0.852f, ajustes.spritePivot.y, 0.0001f);
                Assert.AreEqual(FilterMode.Point, importador.filterMode);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importador.textureCompression);
            }
            finally
            {
                Object.DestroyImmediate(textura);
                AssetDatabase.DeleteAsset(ruta);
            }
        }
    }
}
