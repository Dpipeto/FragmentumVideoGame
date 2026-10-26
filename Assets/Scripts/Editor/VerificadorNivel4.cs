using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Enemies;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Narrative;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using AnimatorEsperado = Fragmentum.EditorTools.VerificadorNivel1.AnimatorEsperado;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;
using ClipEsperado = Fragmentum.EditorTools.VerificadorNivel1.ClipEsperado;
using Object = UnityEngine.Object;
using PrefabEsperado = Fragmentum.EditorTools.VerificadorNivel1.PrefabEsperado;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Comprueba que el arte del Nivel 4 (el Penitente, el Artillero, el Verdugo, el Juez Silente y lo que
    /// lanzan y dejan) sigue montado como lo dejó <see cref="ArteMundo4"/>. Con las comprobaciones de
    /// <see cref="VerificadorNivel1"/>:
    /// 1. cada PNG de las carpetas del mundo tiene su regla en <see cref="TablaArte"/> y la cumple
    ///    (lienzo, píxeles por unidad, pivote y ajustes de pixel art), y no falta ningún fotograma;
    /// 2. cada clip existe, con sus fotogramas, su duración y exactamente los eventos de su tabla;
    /// 3. cada Animator tiene un estado por clip, ninguno suelto y ninguna transición rota;
    /// 4. en cada prefab, el dibujo usa su controlador, lo que hace daño está apagado en reposo (salvo
    ///    lo que daña siempre: el cuerpo del Penitente y los proyectiles) y cada evento llama a una
    ///    función que existe;
    /// 5. lo demás: el Juez (su fragmento con la gema, la cadena de su trampa y el dibujo de sus ataques),
    ///    el Artillero (su bala y su cañón, el del dibujo), el Verdugo (su garfio y su cadena) y la
    ///    explosión del barril y de la mina;
    /// 6. el escenario: el tileset cortado, las memorias con sus dos dibujos y la losa, el fondo con sus siete
    ///    capas y las cinemáticas del Juez y de las memorias, cada una con sus planos.
    ///
    /// Falla ruidosamente y se ejecuta al final de «Regenerar todo». Sin el arte del mundo en el proyecto
    /// no hay nada que revisar: avisa y da por buena la verificación.
    /// </summary>
    public static class VerificadorNivel4
    {
        private const string Prefijo = "[Nivel 4]";

        [MenuItem("Fragmentum/Niveles/Verificar el arte del Nivel 4", priority = 140)]
        public static void MenuVerificar() => Verificar();

        /// <summary>Las carpetas con los PNG del mundo.</summary>
        public static readonly string[] Carpetas =
        {
            ConstructorPenitente.CarpetaSprites, ConstructorArtillero.CarpetaSprites, ConstructorVerdugo.CarpetaSprites,
            ConstructorJuez.CarpetaSprites, ConstructorEfectosMundo4.Carpeta, ConstructorMecanicasMundo4.Carpeta,
            ConstructorCinematicasNivel4.Carpeta,
        };

        /// <summary>¿Está en el proyecto el arte del mundo?</summary>
        public static bool HayArte => Carpetas.Any(AssetDatabase.IsValidFolder);

        /// <summary>Revisa todo. Devuelve true si está bien; si no, deja un error por problema en la consola.</summary>
        public static bool Verificar()
        {
            if (!HayArte)
            {
                Debug.LogWarning($"{Prefijo} El arte del Mundo 4 no está en el proyecto: no hay nada que verificar.");
                return true;
            }

            var problemas = Problemas();
            if (problemas.Count == 0)
            {
                Debug.Log($"{Prefijo} Verificación correcta: {Sprites().Count} sprites, {Clips.Count} clips, {Animators.Count} Animators, " +
                          $"{Prefabs.Length} prefabs y sus eventos; el Juez, el Artillero, el Verdugo y las explosiones, completos.");
                return true;
            }

            foreach (var problema in problemas)
            {
                Debug.LogError($"{Prefijo} {problema}");
            }

            Debug.LogError($"{Prefijo} La verificación falló: {problemas.Count} problema(s). " +
                           "Fragmentum → Niveles → Generar el arte del Mundo 4 (Culpa) lo rehace desde las tablas.");
            return false;
        }

        /// <summary>Todos los problemas encontrados; vacío si todo está bien.</summary>
        public static List<string> Problemas()
        {
            var problemas = new List<string>();
            foreach (var falta in FotogramasDeLasTablas().Where(r => !File.Exists(r)))
            {
                problemas.Add($"Falta el fotograma {Path.GetFileNameWithoutExtension(falta)} ({falta}).");
            }

            foreach (var ruta in Sprites())
            {
                problemas.AddRange(VerificadorNivel1.ProblemasDeSprite(ruta, AssetImporter.GetAtPath(ruta) as TextureImporter));
            }

            foreach (var esperado in Clips)
            {
                problemas.AddRange(VerificadorNivel1.ProblemasDeClip(esperado, AssetDatabase.LoadAssetAtPath<AnimationClip>(esperado.Ruta)));
            }

            foreach (var animator in Animators)
            {
                problemas.AddRange(VerificadorNivel1.ProblemasDelAnimator(animator, AssetDatabase.LoadAssetAtPath<AnimatorController>(animator.Ruta)));
            }

            foreach (var pieza in Prefabs)
            {
                problemas.AddRange(VerificadorNivel1.ProblemasDelPrefab(pieza, AssetDatabase.LoadAssetAtPath<GameObject>(pieza.Ruta)));
            }

            problemas.AddRange(ProblemasDelJuez(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJuez)));
            problemas.AddRange(ProblemasDelArtillero(ConstructorArtillero.Prefab));
            problemas.AddRange(ProblemasDelVerdugo(ConstructorVerdugo.Prefab));
            problemas.AddRange(ProblemasDeLasExplosiones());
            problemas.AddRange(ProblemasDelBarrilYLaMina());
            problemas.AddRange(ProblemasDelEscenario());
            problemas.AddRange(ProblemasDeLasMemorias());
            problemas.AddRange(ProblemasDelFondo(AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorFondoMundo4.RutaPrefab)));
            problemas.AddRange(ProblemasDeLasCinematicas());
            return problemas;
        }

        // --- 1. Sprites ---

        /// <summary>Los fotogramas que nombran las tablas de los constructores: todos tienen que estar.</summary>
        public static IEnumerable<string> FotogramasDeLasTablas() =>
            ConstructorPenitente.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorPenitente.RutaSprite)
                .Concat(ConstructorArtillero.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorArtillero.RutaSprite))
                .Concat(ConstructorVerdugo.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorVerdugo.RutaSprite))
                .Concat(ConstructorJuez.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorJuez.RutaSprite))
                .Append(ConstructorJuez.RutaFragmento)
                .Concat(ConstructorEfectosMundo4.ClipsArtilleria.Append(ConstructorEfectosMundo4.ExplosionEntera)
                    .SelectMany(c => c.Fotogramas).Select(ConstructorEfectosMundo4.RutaExplosion))
                .Concat(ConstructorEfectosMundo4.FotogramasOla.Select(ConstructorEfectosMundo4.RutaFuego))
                .Concat(ConstructorEfectosMundo4.FotogramasPlaca.Select(ConstructorEfectosMundo4.RutaPlaca))
                .Concat(ConstructorEfectosMundo4.FotogramasOnda.Select(ConstructorEfectosMundo4.RutaOnda))
                .Append(ConstructorEfectosMundo4.RutaBala("Bala1"))
                .Concat(new[] { "Garfio", "Cadena" }.Select(ConstructorEfectosMundo4.RutaCadena))
                .Concat(ConstructorMecanicasMundo4.Rutas)
                .Distinct();

        /// <summary>Todos los PNG de las carpetas del mundo: cada uno debe seguir su regla.</summary>
        public static List<string> Sprites()
        {
            var carpetas = Carpetas.Where(AssetDatabase.IsValidFolder).ToArray();
            return carpetas.Length == 0
                ? new List<string>()
                : AssetDatabase.FindAssets("t:Texture2D", carpetas).Select(AssetDatabase.GUIDToAssetPath)
                    .Where(r => r.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .Distinct().OrderBy(r => r, StringComparer.Ordinal).ToList();
        }

        // --- 2. Clips ---

        private static IEnumerable<ClipEsperado> De(IEnumerable<Clip> clips, string carpeta, Func<Clip, IEnumerable<AnimationEvent>> eventos = null) =>
            clips.Select(c => new ClipEsperado
            {
                Ruta = c.Ruta(carpeta),
                Definicion = c,
                Eventos = eventos == null
                    ? new (float, string)[0]
                    : eventos(c).Select(e => (e.time, e.functionName)).ToArray(),
            });

        public static IReadOnlyList<ClipEsperado> Clips =>
            De(ConstructorPenitente.Clips, ConstructorPenitente.CarpetaAnimaciones, ConstructorLamento.EventosDe)
                .Concat(De(ConstructorArtillero.Clips, ConstructorArtillero.CarpetaAnimaciones, ConstructorArtillero.EventosDe))
                .Concat(De(ConstructorVerdugo.Clips, ConstructorVerdugo.CarpetaAnimaciones, ConstructorVerdugo.EventosDe))
                .Concat(De(ConstructorJuez.Clips, ConstructorJuez.CarpetaAnimaciones, ConstructorJuez.EventosDe))
                .Concat(De(new[] { ConstructorEfectosMundo4.ExplosionEntera }, ConstructorEfectosMundo4.CarpetaAnimaciones))
                .Concat(De(ConstructorEfectosMundo4.ClipsArtilleria, ConstructorEfectosMundo4.CarpetaAnimaciones))
                .ToList();

        // --- 3. Animators ---

        private static AnimatorEsperado De(string nombre, string ruta, IEnumerable<Clip> clips) =>
            new AnimatorEsperado
            {
                Nombre = nombre,
                Ruta = ruta,
                Estados = clips.Select(c => (c.Estado, c.Nombre)).ToArray(),
            };

        public static IReadOnlyList<AnimatorEsperado> Animators => new[]
        {
            De(ConstructorPenitente.NombreDatos, ConstructorPenitente.RutaControlador, ConstructorPenitente.Clips),
            De(ConstructorArtillero.NombreDatos, ConstructorArtillero.RutaControlador, ConstructorArtillero.Clips),
            De(ConstructorVerdugo.NombreDatos, ConstructorVerdugo.RutaControlador, ConstructorVerdugo.Clips),
            De(ConstructorJuez.NombreDatos, ConstructorJuez.RutaControlador, ConstructorJuez.Clips),
            De(ConstructorEfectosMundo4.NombreExplosion, $"{ConstructorEfectosMundo4.CarpetaAnimaciones}/Explosion.controller",
                new[] { ConstructorEfectosMundo4.ExplosionEntera }),
            De(ConstructorEfectosMundo4.NombreArtilleria, $"{ConstructorEfectosMundo4.CarpetaAnimaciones}/Artilleria.controller",
                ConstructorEfectosMundo4.ClipsArtilleria),
        };

        // --- 4. Prefabs ---

        public static string RutaPrefabJuez => $"{GeneradorJefes.CarpetaPrefabs}/{ConstructorJuez.NombreDatos}.prefab";

        public static readonly PrefabEsperado[] Prefabs =
        {
            // El cuerpo del Penitente, como el del Eco: su contacto se apaga al arrancar (no hace daño).
            new PrefabEsperado
            {
                Ruta = ConstructorPenitente.RutaPrefab, Controlador = ConstructorPenitente.RutaControlador,
                ZonasDeDanio = new[] { "Arma", "Contacto" }, DananSiempre = new[] { "Contacto" },
            },
            // Tocar al Artillero o al Verdugo no hace daño: solo sus golpes, cuando los lanzan.
            new PrefabEsperado
            {
                Ruta = ConstructorArtillero.RutaPrefab, Controlador = ConstructorArtillero.RutaControlador,
                ZonasDeDanio = new[] { "Arma", "Contacto" },
            },
            new PrefabEsperado
            {
                Ruta = ConstructorVerdugo.RutaPrefab, Controlador = ConstructorVerdugo.RutaControlador,
                ZonasDeDanio = new[] { "Contacto", "Hachazo", "Barrido" },
            },
            new PrefabEsperado
            {
                Ruta = RutaPrefabJuez, Controlador = ConstructorJuez.RutaControlador,
                ZonasDeDanio = new[] { "Contacto" },
            },
            new PrefabEsperado
            {
                Ruta = ConstructorEfectosMundo4.RutaPrefab(ConstructorEfectosMundo4.NombreExplosion),
                Controlador = $"{ConstructorEfectosMundo4.CarpetaAnimaciones}/Explosion.controller",
            },
            new PrefabEsperado
            {
                Ruta = ConstructorEfectosMundo4.RutaPrefab(ConstructorEfectosMundo4.NombreArtilleria),
                Controlador = $"{ConstructorEfectosMundo4.CarpetaAnimaciones}/Artilleria.controller",
            },
            new PrefabEsperado { Ruta = ConstructorEfectosMundo4.RutaPrefab(ConstructorEfectosMundo4.NombreOla) },
            // Los proyectiles hacen daño siempre: es lo que son.
            new PrefabEsperado
            {
                Ruta = ConstructorEfectosMundo4.RutaPrefab(ConstructorEfectosMundo4.NombrePlaca),
                ZonasDeDanio = new[] { ConstructorEfectosMundo4.NombrePlaca }, DananSiempre = new[] { ConstructorEfectosMundo4.NombrePlaca },
            },
            new PrefabEsperado
            {
                Ruta = ConstructorEfectosMundo4.RutaPrefab(ConstructorEfectosMundo4.NombreBala),
                ZonasDeDanio = new[] { ConstructorEfectosMundo4.NombreBala }, DananSiempre = new[] { ConstructorEfectosMundo4.NombreBala },
            },
        };

        // --- 5. Lo demás ---

        private static Object Referencia(Object componente, string campo) =>
            componente != null ? new SerializedObject(componente).FindProperty(campo)?.objectReferenceValue : null;

        /// <summary>
        /// El Juez: deja su fragmento (con la gema dibujada aparte, sobre el montón de M5), sus cadenas de la
        /// trampa son las dibujadas y cada ataque con dibujo lo lleva.
        /// </summary>
        public static List<string> ProblemasDelJuez(GameObject prefab)
        {
            var problemas = new List<string>();
            var juez = prefab != null ? prefab.GetComponent<JuezSilente>() : null;
            if (juez == null)
            {
                return problemas; // que falte ya lo dice la revisión de los prefabs
            }

            if (juez.Fragmento == null || juez.Fragmento.Gema == null || juez.Fragmento.Gema.sprite == null)
            {
                problemas.Add($"Prefab {prefab.name}: no deja fragmento, o su gema no está dibujada (M5 es solo el montón).");
            }

            if (Referencia(juez, "cadena") != ConstructorEfectosMundo4.Cadena)
            {
                problemas.Add($"Prefab {prefab.name}: la trampa no lleva la cadena dibujada.");
            }

            var patrones = $"{GeneradorJefes.CarpetaPatrones}";
            var orden = AssetDatabase.LoadAssetAtPath<PatronOrden>($"{patrones}/Orden.asset");
            var pueblo = AssetDatabase.LoadAssetAtPath<PatronPueblo>($"{patrones}/Pueblo.asset");
            var companero = AssetDatabase.LoadAssetAtPath<PatronCompanero>($"{patrones}/Companero.asset");
            var sentencia = AssetDatabase.LoadAssetAtPath<PatronSentencia>($"{patrones}/Sentencia.asset");
            if (orden != null && ConstructorEfectosMundo4.PrefabArtilleria != null && orden.efecto == null)
            {
                problemas.Add("La orden: su artillería no lleva su dibujo (la marca y la explosión).");
            }

            if (pueblo != null && ConstructorEfectosMundo4.PrefabOla != null && pueblo.efecto == null)
            {
                problemas.Add("El pueblo: su ola no lleva su dibujo.");
            }

            if (companero != null && ConstructorEfectosMundo4.PrefabPlaca != null && companero.placa == null)
            {
                problemas.Add("El compañero: sus placas no llevan su dibujo.");
            }

            if (sentencia != null && ConstructorEfectosMundo4.PrefabOnda != null && sentencia.efecto == null)
            {
                problemas.Add("La sentencia: su onda no lleva su dibujo.");
            }

            return problemas;
        }

        /// <summary>El Artillero: dispara su bala dibujada, y su cañón es la boca de su fusil en el dibujo.</summary>
        public static List<string> ProblemasDelArtillero(GameObject prefab)
        {
            var problemas = new List<string>();
            var artillero = prefab != null ? prefab.GetComponent<Artillero>() : null;
            if (artillero == null)
            {
                return problemas;
            }

            if (ConstructorEfectosMundo4.PrefabBala != null && Referencia(artillero, "bala") == null)
            {
                problemas.Add($"Prefab {prefab.name}: dispara un cuadrado, no su bala dibujada.");
            }

            var dibujo = ConstructorArtillero.CanonDelDibujo;
            if (Mathf.Abs(dibujo.x - DisenoNivel4.CanonDelante) > 0.01f || Mathf.Abs(dibujo.y - DisenoNivel4.AlturaCanon) > 0.01f)
            {
                problemas.Add($"Artillero: el cañón del diseño ({DisenoNivel4.CanonDelante}, {DisenoNivel4.AlturaCanon}) no es la boca de su " +
                              $"fusil en el dibujo ({ComprobacionesArte.Numero(dibujo.x)}, {ComprobacionesArte.Numero(dibujo.y)}).");
            }

            if (Mathf.Abs(artillero.canon.x - DisenoNivel4.CanonDelante) > 1e-3f || Mathf.Abs(artillero.canon.y - DisenoNivel4.AlturaCanon) > 1e-3f)
            {
                problemas.Add($"Prefab {prefab.name}: su cañón no es el del diseño.");
            }

            return problemas;
        }

        /// <summary>El Verdugo: lanza el garfio dibujado, con su cadena en mosaico desde su mano.</summary>
        public static List<string> ProblemasDelVerdugo(GameObject prefab)
        {
            var problemas = new List<string>();
            var verdugo = prefab != null ? prefab.GetComponent<Verdugo>() : null;
            if (verdugo == null || ConstructorEfectosMundo4.Garfio == null)
            {
                return problemas;
            }

            var garfio = Referencia(verdugo, "garfio") as Transform;
            var dibujo = garfio != null ? garfio.GetComponent<SpriteRenderer>() : null;
            if (dibujo == null || dibujo.sprite != ConstructorEfectosMundo4.Garfio)
            {
                problemas.Add($"Prefab {prefab.name}: su garfio no es el dibujado.");
            }

            var cadena = Referencia(verdugo, "cadena") as SpriteRenderer;
            if (cadena == null || cadena.sprite != ConstructorEfectosMundo4.Cadena || cadena.drawMode != SpriteDrawMode.Tiled)
            {
                problemas.Add($"Prefab {prefab.name}: le falta su cadena (el tramo dibujado, en mosaico).");
            }

            return problemas;
        }

        /// <summary>El barril y la mina estallan con la explosión dibujada.</summary>
        public static List<string> ProblemasDeLasExplosiones()
        {
            var problemas = new List<string>();
            var explosion = ConstructorEfectosMundo4.PrefabExplosion;
            if (explosion == null)
            {
                return problemas;
            }

            foreach (var nombre in new[] { GeneradorMecanicas.BarrilExplosivo, GeneradorMecanicas.Mina })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GeneradorMecanicas.Carpeta}/{nombre}.prefab");
                Component pieza = prefab == null ? null
                    : (Component)prefab.GetComponent<BarrilExplosivo>() ?? prefab.GetComponent<Mina>();
                if (pieza != null && Referencia(pieza, "dibujoExplosion") != explosion)
                {
                    problemas.Add($"Prefab {nombre}: estalla con el destello provisional, no con la explosión dibujada.");
                }
            }

            return problemas;
        }

        /// <summary>El barril y la mina, con sus dibujos: el barril con sus cuatro, la mina con el quieto y el aviso.</summary>
        public static List<string> ProblemasDelBarrilYLaMina()
        {
            var problemas = new List<string>();
            var barril = ConstructorMecanicasMundo4.DibujosBarril;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GeneradorMecanicas.Carpeta}/{GeneradorMecanicas.BarrilExplosivo}.prefab");
            var pieza = prefab != null ? prefab.GetComponent<BarrilExplosivo>() : null;
            if (barril != null && pieza != null)
            {
                var campos = new[] { "spriteIntacto", "spriteMecha1", "spriteMecha2", "spriteQuemado" };
                for (var i = 0; i < campos.Length; i++)
                {
                    if (Referencia(pieza, campos[i]) != barril[i])
                    {
                        problemas.Add($"Prefab {prefab.name}: {campos[i]} no es {barril[i].name}.");
                    }
                }
            }

            var mina = ConstructorMecanicasMundo4.DibujosMina;
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{GeneradorMecanicas.Carpeta}/{GeneradorMecanicas.Mina}.prefab");
            var bola = prefab != null ? prefab.GetComponent<Mina>() : null;
            if (mina != null && bola != null)
            {
                if (Referencia(bola, "spriteReposo") != mina[0] || Referencia(bola, "spriteAviso") != mina[1])
                {
                    problemas.Add($"Prefab {prefab.name}: le faltan sus dibujos (quieta y avisando).");
                }

                if (!(Referencia(bola, "dibujoBola") is SpriteRenderer dibujo) || dibujo.sprite != mina[0])
                {
                    problemas.Add($"Prefab {prefab.name}: su bola no lleva el dibujo de la mina.");
                }
            }

            return problemas;
        }

        /// <summary>El tileset y los props, cortados en todas sus piezas (con las hojas en el proyecto).</summary>
        public static List<string> ProblemasDelEscenario()
        {
            var problemas = new List<string>();
            if (!ConstructorEscenarioMundo4.HayArte)
            {
                return problemas;
            }

            foreach (var pieza in ConstructorEscenarioMundo4.Piezas.Where(p => ConstructorEscenarioMundo4.Cargar(p) == null))
            {
                problemas.Add($"Falta la pieza {pieza.Nombre} de {pieza.Hoja}: Fragmentum → Niveles → Cortar el tileset y los props del Mundo 4.");
            }

            return problemas;
        }

        /// <summary>
        /// Las memorias y su losa: si llegó alguna, están todas, cada una con su dibujo apagado y el brillando en el
        /// mismo lienzo y con el mismo pivote (el brillo va encima del apagado y tiene que caer en su sitio).
        /// </summary>
        public static List<string> ProblemasDeLasMemorias()
        {
            var problemas = new List<string>();
            var todas = ConstructorMecanicasMundo4.Memorias.Append(ConstructorMecanicasMundo4.Losa).ToArray();
            if (todas.All(m => m.Apagada == null && m.Brillando == null))
            {
                return problemas;
            }

            foreach (var memoria in todas)
            {
                if (!memoria.Completo)
                {
                    problemas.Add($"Memoria {memoria.Nombre}: le falta su dibujo apagado o el brillando ({memoria.RutaApagada}, {memoria.RutaBrillando}).");
                    continue;
                }

                if (memoria.Apagada.rect.size != memoria.Brillando.rect.size || memoria.Apagada.pivot != memoria.Brillando.pivot)
                {
                    problemas.Add($"Memoria {memoria.Nombre}: el dibujo brillando no tiene el lienzo o el pivote del apagado; su brillo no caería encima.");
                }
            }

            return problemas;
        }

        /// <summary>El fondo: si están sus imágenes, el prefab con sus siete capas, cada una con su dibujo.</summary>
        public static List<string> ProblemasDelFondo(GameObject prefab)
        {
            var problemas = new List<string>();
            if (ConstructorFondoMundo4.CapasFondo.Any(c => AssetDatabase.LoadAssetAtPath<Texture2D>(ConstructorFondoMundo4.RutaDe(c)) == null))
            {
                return problemas;
            }

            if (prefab == null)
            {
                problemas.Add($"Falta el prefab del fondo ({ConstructorFondoMundo4.RutaPrefab}).");
                return problemas;
            }

            var capas = prefab.GetComponentsInChildren<Renderer>(true).ToList();
            if (capas.Count != ConstructorFondoMundo4.CapasFondo.Length)
            {
                problemas.Add($"Prefab {prefab.name}: tiene {capas.Count} capas y deben ser {ConstructorFondoMundo4.CapasFondo.Length}.");
            }

            foreach (var capa in capas)
            {
                var material = capa.sharedMaterial;
                var sinDibujo = capa is MeshRenderer ? material == null || material.mainTexture == null : ((SpriteRenderer)capa).sprite == null;
                if (sinDibujo)
                {
                    problemas.Add($"Prefab {prefab.name}: la capa «{capa.name}» no tiene su dibujo.");
                }
            }

            return problemas;
        }

        /// <summary>
        /// Las cinemáticas del Juez (con sus cuatro planos: la presentación y la derrota, cada una con los suyos, y
        /// el Nivel 4 las lleva) y las de las memorias (con sus imágenes: cada una con la suya).
        /// </summary>
        public static List<string> ProblemasDeLasCinematicas()
        {
            var problemas = new List<string>();
            if (ConstructorCinematicasNivel4.HayPlanos)
            {
                var presentacion = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicasNivel4.RutaPresentacion);
                var derrota = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicasNivel4.RutaDerrota);
                problemas.AddRange(VerificadorNivel2.ProblemasDeLaCinematica(presentacion, ConstructorCinematicasNivel4.RutaPresentacion,
                    new[] { ConstructorCinematicasNivel4.Presentacion1, ConstructorCinematicasNivel4.Presentacion2 }));
                problemas.AddRange(VerificadorNivel2.ProblemasDeLaCinematica(derrota, ConstructorCinematicasNivel4.RutaDerrota,
                    new[] { ConstructorCinematicasNivel4.Derrota1, ConstructorCinematicasNivel4.Derrota2 }));

                var nivel = ConstructorCinematicasNivel4.Nivel();
                if (nivel != null && presentacion != null && nivel.cinematicaJefe != presentacion)
                {
                    problemas.Add($"El Nivel 4 no presenta a su jefe con {Path.GetFileName(ConstructorCinematicasNivel4.RutaPresentacion)}.");
                }

                if (nivel != null && derrota != null && nivel.cinematicaJefeDerrotado != derrota)
                {
                    problemas.Add($"El Nivel 4 no lleva {Path.GetFileName(ConstructorCinematicasNivel4.RutaDerrota)}: al caer el Juez no se vería nada.");
                }
            }

            if (ConstructorCinematicasNivel4.ImagenesDeLasMemorias != null)
            {
                foreach (var memoria in DisenoNivel4.Memorias)
                {
                    var ruta = GeneradorNarrativa.RutaCinematicaMemoria(memoria.Nombre);
                    problemas.AddRange(VerificadorNivel2.ProblemasDeLaCinematica(AssetDatabase.LoadAssetAtPath<CutsceneData>(ruta), ruta,
                        new[] { memoria.Nombre }));
                }
            }

            return problemas;
        }
    }
}
