using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Narrative;
using Fragmentum.Levels.Escenario;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using AnimatorEsperado = Fragmentum.EditorTools.VerificadorNivel1.AnimatorEsperado;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;
using ClipEsperado = Fragmentum.EditorTools.VerificadorNivel1.ClipEsperado;
using PrefabEsperado = Fragmentum.EditorTools.VerificadorNivel1.PrefabEsperado;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Comprueba que el arte del Nivel 2 (el Lamento, la Escultura, la Medusa, la Plañidera, la
    /// Doncella, las mecánicas, los efectos, la burbuja del HUD, el fondo y el tileset) sigue
    /// montado como lo dejaron <see cref="ArteMundo2"/>, <see cref="ConstructorFondoMundo2"/> y
    /// <see cref="ConstructorEscenarioMundo2"/>. Usa las mismas comprobaciones que
    /// <see cref="VerificadorNivel1"/>:
    /// 1. cada PNG de las carpetas del mundo tiene su regla en <see cref="TablaArte"/> y la cumple
    ///    (lienzo, píxeles por unidad, pivote y ajustes de pixel art), y no falta ningún fotograma
    ///    de las tablas;
    /// 2. cada clip existe, con sus fotogramas, su duración y exactamente los eventos de su tabla, y
    ///    el ataque de la Escultura voltea sus tres fotogramas dibujados al revés;
    /// 3. cada Animator tiene un estado por clip, ninguno suelto y ninguna transición rota;
    /// 4. en cada prefab, el dibujo usa su controlador, lo que hace daño está apagado en reposo
    ///    (salvo lo que daña siempre) y cada evento llama a una función que existe;
    /// 5. lo que no es un prefab con Animator: el arte de las franjas, la burbuja del HUD, la cola
    ///    fundida y el fragmento de la Doncella, la ola de sus patrones, el fondo y el tileset;
    /// 6. las cinemáticas de la Doncella: en la rejilla de sus planos, con los cuatro, y puestas en
    ///    el Nivel 2.
    ///
    /// Falla ruidosamente, como el del Nivel 1, y se ejecuta al final de «Regenerar todo». Sin el
    /// arte del mundo en el proyecto no hay nada que revisar: avisa y da por buena la verificación.
    /// </summary>
    public static class VerificadorNivel2
    {
        private const string Prefijo = "[Nivel 2]";

        [MenuItem("Fragmentum/Niveles/Verificar el arte del Nivel 2", priority = 136)]
        public static void MenuVerificar() => Verificar();

        /// <summary>Las carpetas con los PNG del mundo (sin el fondo ni el tileset, que no siguen <see cref="TablaArte"/>).</summary>
        public static readonly string[] Carpetas =
        {
            ConstructorLamento.CarpetaSprites, ConstructorEscultura.CarpetaSprites, ConstructorMedusa.CarpetaSprites,
            ConstructorPlanidera.CarpetaSprites, ConstructorDoncella.CarpetaSprites, ConstructorMecanicasMundo2.CarpetaMecanicas,
            ConstructorMecanicasMundo2.CarpetaEfectos, ConstructorMecanicasMundo2.CarpetaInterfaz, ConstructorCinematicasNivel2.Carpeta,
        };

        /// <summary>¿Está en el proyecto el arte del mundo?</summary>
        public static bool HayArte => Carpetas.Any(AssetDatabase.IsValidFolder);

        /// <summary>Revisa todo. Devuelve true si está bien; si no, deja un error por problema en la consola.</summary>
        public static bool Verificar()
        {
            if (!HayArte)
            {
                Debug.LogWarning($"{Prefijo} El arte del Mundo 2 no está en el proyecto: no hay nada que verificar.");
                return true;
            }

            var problemas = Problemas();
            if (problemas.Count == 0)
            {
                Debug.Log($"{Prefijo} Verificación correcta: {Sprites().Count} sprites, {Clips.Count} clips, {Animators.Count} Animators, " +
                          $"{Prefabs.Length} prefabs y sus eventos; las franjas, la burbuja, la Doncella, el fondo, el tileset y las cinemáticas, completos.");
                return true;
            }

            foreach (var problema in problemas)
            {
                Debug.LogError($"{Prefijo} {problema}");
            }

            Debug.LogError($"{Prefijo} La verificación falló: {problemas.Count} problema(s). " +
                           "Fragmentum → Niveles → Generar el arte del Mundo 2 (Tristeza) lo rehace desde las tablas.");
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

            problemas.AddRange(ProblemasDelVolteo(AssetDatabase.LoadAssetAtPath<AnimationClip>(ConstructorEscultura.RutaClip(ConstructorEscultura.Ataque))));

            foreach (var animator in Animators)
            {
                problemas.AddRange(VerificadorNivel1.ProblemasDelAnimator(animator, AssetDatabase.LoadAssetAtPath<AnimatorController>(animator.Ruta)));
            }

            foreach (var pieza in Prefabs)
            {
                problemas.AddRange(VerificadorNivel1.ProblemasDelPrefab(pieza, AssetDatabase.LoadAssetAtPath<GameObject>(pieza.Ruta)));
            }

            problemas.AddRange(ProblemasDeLasFranjas(ConstructorMecanicasMundo2.ArteFranja));
            problemas.AddRange(ProblemasDeLaBurbuja(AssetDatabase.LoadAssetAtPath<ArteInterfaz>(ConstructorMecanicasMundo2.RutaArteInterfaz)));
            problemas.AddRange(ProblemasDeLaDoncella(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabDoncella)));
            problemas.AddRange(ProblemasDeLasOlas(PatronesDeOla(), ConstructorMecanicasMundo2.PrefabOla));
            problemas.AddRange(ProblemasDelFondo(AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorFondoMundo2.RutaPrefab)));
            problemas.AddRange(ProblemasDelTileset());
            problemas.AddRange(ProblemasDeLasCinematicas());
            return problemas;
        }

        // --- 1. Sprites ---

        /// <summary>Los fotogramas que nombran las tablas de los constructores: todos tienen que estar.</summary>
        public static IEnumerable<string> FotogramasDeLasTablas() =>
            ConstructorLamento.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorLamento.RutaSprite)
                .Concat(ConstructorEscultura.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorEscultura.RutaSprite))
                .Concat(ConstructorMedusa.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorMedusa.RutaSprite))
                .Concat(ConstructorPlanidera.Clips.SelectMany(c => c.Fotogramas).Concat(ConstructorPlanidera.EsquirlaVuelo.Fotogramas)
                    .Append(ConstructorPlanidera.FotogramaEsquirlaRota).Select(ConstructorPlanidera.RutaSprite))
                .Concat(ConstructorDoncella.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorDoncella.RutaSprite))
                .Concat(ConstructorMecanicasMundo2.Tapon.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorMecanicasMundo2.Hielo))
                .Concat(ConstructorMecanicasMundo2.ClipsOla.SelectMany(c => c.Fotogramas).Select(ConstructorMecanicasMundo2.Ola))
                .Concat(ConstructorMecanicasMundo2.FotogramasBurbujas.Select(ConstructorMecanicasMundo2.Burbujas))
                .Concat(ConstructorMecanicasMundo2.FotogramasAgua.Select(ConstructorMecanicasMundo2.Agua))
                .Concat(ConstructorMecanicasMundo2.FotogramasAviso.Select(ConstructorMecanicasMundo2.Inundacion))
                .Concat(ConstructorMecanicasMundo2.FotogramasPuasQuietas.Select(ConstructorMecanicasMundo2.Puas))
                .Concat(ConstructorMecanicasMundo2.FotogramasOxigeno.Select(ConstructorMecanicasMundo2.Oxigeno))
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
            De(ConstructorLamento.Clips, ConstructorLamento.CarpetaAnimaciones, ConstructorLamento.EventosDe)
                .Concat(De(ConstructorEscultura.Clips, ConstructorEscultura.CarpetaAnimaciones, ConstructorEscultura.EventosDe))
                .Concat(De(ConstructorMedusa.Clips, ConstructorMedusa.CarpetaAnimaciones))
                .Concat(De(ConstructorPlanidera.Clips, ConstructorPlanidera.CarpetaAnimaciones, ConstructorPlanidera.EventosDe))
                .Concat(De(new[] { ConstructorPlanidera.EsquirlaVuelo }, ConstructorPlanidera.CarpetaAnimaciones))
                .Concat(De(ConstructorDoncella.Clips, ConstructorDoncella.CarpetaAnimaciones, ConstructorDoncella.EventosDe))
                .Concat(De(ConstructorMecanicasMundo2.Tapon.Clips, ConstructorMecanicasMundo2.CarpetaAnimaciones))
                .Concat(De(ConstructorMecanicasMundo2.ClipsOla, ConstructorMecanicasMundo2.CarpetaAnimaciones))
                .ToList();

        /// <summary>
        /// El ataque de la Escultura voltea el dibujo en <see cref="ConstructorEscultura.FotogramasVolteados"/>
        /// (vienen dibujados mirando al otro lado) y en ninguno más.
        /// </summary>
        public static List<string> ProblemasDelVolteo(AnimationClip ataque)
        {
            var problemas = new List<string>();
            if (ataque == null)
            {
                return problemas; // que falte ya lo dice la revisión de los clips
            }

            var curva = AnimationUtility.GetEditorCurve(ataque, ConstructorEscultura.CurvaVolteo);
            if (curva == null)
            {
                problemas.Add($"Clip {ataque.name}: no voltea el dibujo; {string.Join(", ", ConstructorEscultura.FotogramasVolteados)} saldrían golpeando hacia atrás.");
                return problemas;
            }

            var definicion = ConstructorEscultura.ClipDe(ConstructorEscultura.Ataque);
            for (var i = 0; i < definicion.Fotogramas.Length; i++)
            {
                var volteado = curva.Evaluate(definicion.Inicio(i) + definicion.Duraciones[i] * 0.5f) > 0.5f;
                var debe = ConstructorEscultura.FotogramasVolteados.Contains(definicion.Fotogramas[i]);
                if (volteado != debe)
                {
                    problemas.Add($"Clip {ataque.name}: el fotograma {definicion.Fotogramas[i]} {(debe ? "debe ir volteado y no lo va" : "va volteado y no debe")}.");
                }
            }

            return problemas;
        }

        // --- 3. Animators ---

        private static AnimatorEsperado De(string nombre, string ruta, IEnumerable<Clip> clips, ConstructorMecanicas.Mecanica mecanica = null) =>
            new AnimatorEsperado
            {
                Nombre = nombre,
                Ruta = ruta,
                Estados = clips.Select(c => (c.Estado, c.Nombre)).ToArray(),
                Mecanica = mecanica,
            };

        public static IReadOnlyList<AnimatorEsperado> Animators => new[]
        {
            De(ConstructorLamento.NombreDatos, ConstructorLamento.RutaControlador, ConstructorLamento.Clips),
            De(ConstructorEscultura.NombreDatos, ConstructorEscultura.RutaControlador, ConstructorEscultura.Clips),
            De(GeneradorEnemigos.Medusa, ConstructorMedusa.RutaControlador, ConstructorMedusa.Clips),
            De(ConstructorPlanidera.NombreDatos, ConstructorPlanidera.RutaControlador, ConstructorPlanidera.Clips),
            De("Esquirla", ConstructorPlanidera.RutaControladorEsquirla, new[] { ConstructorPlanidera.EsquirlaVuelo }),
            De(ConstructorDoncella.NombreDatos, ConstructorDoncella.RutaControlador, ConstructorDoncella.Clips),
            De(ConstructorMecanicasMundo2.Tapon.Nombre, ConstructorMecanicasMundo2.Tapon.RutaControlador, ConstructorMecanicasMundo2.Tapon.Clips,
                ConstructorMecanicasMundo2.Tapon),
            De("Ola", ConstructorMecanicasMundo2.RutaControladorOla, ConstructorMecanicasMundo2.ClipsOla),
        };

        // --- 4. Prefabs ---

        public static string RutaPrefabDoncella => $"{GeneradorJefes.CarpetaPrefabs}/{ConstructorDoncella.NombreDatos}.prefab";

        private static string RutaMecanica(string nombre) => $"{GeneradorMecanicas.Carpeta}/{nombre}.prefab";

        public static readonly PrefabEsperado[] Prefabs =
        {
            // El cuerpo del Lamento quema siempre; su golpe, solo en su fotograma.
            new PrefabEsperado
            {
                Ruta = ConstructorLamento.RutaPrefab, Controlador = ConstructorLamento.RutaControlador,
                ZonasDeDanio = new[] { "Arma", "Contacto" }, DananSiempre = new[] { "Contacto" },
            },
            // La Escultura nace dormida y la Plañidera no quema al tocarla: todo apagado.
            new PrefabEsperado
            {
                Ruta = ConstructorEscultura.RutaPrefab, Controlador = ConstructorEscultura.RutaControlador,
                ZonasDeDanio = new[] { "Arma", "Contacto" },
            },
            new PrefabEsperado
            {
                Ruta = ConstructorPlanidera.RutaPrefab, Controlador = ConstructorPlanidera.RutaControlador,
                ZonasDeDanio = new[] { "Arma", "Contacto" },
            },
            // La esquirla solo existe lanzada, y la Medusa quema siempre.
            new PrefabEsperado
            {
                Ruta = ConstructorPlanidera.RutaPrefabEsquirla, Controlador = ConstructorPlanidera.RutaControladorEsquirla,
                ZonasDeDanio = new[] { "Planidera_Esquirla" }, DananSiempre = new[] { "Planidera_Esquirla" },
            },
            new PrefabEsperado
            {
                Ruta = GeneradorEnemigos.RutaPrefabMedusa, Controlador = ConstructorMedusa.RutaControlador,
                ZonasDeDanio = new[] { "Contacto" }, DananSiempre = new[] { "Contacto" },
            },
            new PrefabEsperado
            {
                Ruta = RutaPrefabDoncella, Controlador = ConstructorDoncella.RutaControlador,
                ZonasDeDanio = new[] { "Contacto" },
            },
            new PrefabEsperado { Ruta = ConstructorMecanicasMundo2.RutaPrefabOla, Controlador = ConstructorMecanicasMundo2.RutaControladorOla },
            new PrefabEsperado { Ruta = RutaMecanica(GeneradorMecanicas.HieloRompible), Controlador = ConstructorMecanicasMundo2.Tapon.RutaControlador },
            new PrefabEsperado { Ruta = RutaMecanica(GeneradorMecanicas.PlataformaFlotante) },
            new PrefabEsperado { Ruta = ConstructorMecanicasMundo2.RutaPrefabBurbujas },
        };

        // --- 5. Lo demás ---

        /// <summary>El arte de las franjas de la Doncella: existe y no le falta ningún dibujo.</summary>
        public static List<string> ProblemasDeLasFranjas(ArteDeFranja arte)
        {
            var problemas = new List<string>();
            if (arte == null)
            {
                problemas.Add($"Falta el arte de las franjas ({ConstructorMecanicasMundo2.RutaArteFranja}).");
            }
            else if (!arte.Completo)
            {
                problemas.Add("Arte de las franjas: le falta algún dibujo (el aviso, la superficie, las púas o la capa); las franjas saldrían con sus rectángulos.");
            }

            return problemas;
        }

        /// <summary>La burbuja del HUD: existe, con todos sus fotogramas, del lleno al vacío.</summary>
        public static List<string> ProblemasDeLaBurbuja(ArteInterfaz arte)
        {
            var problemas = new List<string>();
            var cuantos = ConstructorMecanicasMundo2.FotogramasOxigeno.Length;
            if (arte == null)
            {
                problemas.Add($"Falta el arte de la interfaz ({ConstructorMecanicasMundo2.RutaArteInterfaz}).");
            }
            else if (!arte.TieneOxigeno || arte.oxigeno.Length != cuantos || arte.oxigeno.Any(s => s == null))
            {
                problemas.Add($"Arte de la interfaz: la burbuja del aire debe tener sus {cuantos} fotogramas; el HUD enseñaría la barra.");
            }

            return problemas;
        }

        /// <summary>
        /// La Doncella: su dibujo se funde por abajo (<see cref="ColaFundida"/>, con su material),
        /// lleva el material del destello de hielo y deja su fragmento.
        /// </summary>
        public static List<string> ProblemasDeLaDoncella(GameObject prefab)
        {
            var problemas = new List<string>();
            if (prefab == null)
            {
                return problemas; // que falte ya lo dice la revisión de los prefabs
            }

            var cola = prefab.GetComponentInChildren<ColaFundida>(true);
            var dibujo = cola != null ? cola.GetComponent<SpriteRenderer>() : null;
            var material = AssetDatabase.LoadAssetAtPath<Material>(ConstructorDoncella.RutaMaterial);
            if (cola == null || dibujo == null || material == null || dibujo.sharedMaterial != material)
            {
                problemas.Add($"Prefab {prefab.name}: su dibujo no funde la cola ({Path.GetFileName(ConstructorDoncella.RutaMaterial)}); se vería cortada.");
            }

            var doncella = prefab.GetComponent<DoncellaLamento>();
            var destello = doncella != null ? new SerializedObject(doncella).FindProperty("destelloDeHielo") : null;
            if (destello == null || destello.objectReferenceValue == null)
            {
                problemas.Add($"Prefab {prefab.name}: no tiene el material del destello de hielo; un golpe bloqueado no se vería.");
            }

            if (prefab.GetComponentInChildren<FragmentoJefe>(true) == null)
            {
                problemas.Add($"Prefab {prefab.name}: no deja su fragmento.");
            }

            return problemas;
        }

        /// <summary>Los patrones de ola del proyecto: la ola y la ola doble de la Doncella.</summary>
        public static List<PatronOla> PatronesDeOla() =>
            AssetDatabase.FindAssets($"t:{nameof(PatronOla)}", new[] { GeneradorJefes.CarpetaPatrones })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<PatronOla>)
                .Where(p => p != null).ToList();

        /// <summary>Las olas de la Doncella se ven con su efecto: sin él, saldrían como rectángulos.</summary>
        public static List<string> ProblemasDeLasOlas(IReadOnlyCollection<PatronOla> patrones, GameObject ola)
        {
            var problemas = new List<string>();
            if (patrones.Count == 0)
            {
                problemas.Add("No hay ningún patrón de ola de la Doncella.");
            }

            foreach (var patron in patrones.Where(p => p.efecto == null || p.efecto != ola))
            {
                problemas.Add($"Patrón {patron.name}: su ola no usa {Path.GetFileName(ConstructorMecanicasMundo2.RutaPrefabOla)}.");
            }

            return problemas;
        }

        /// <summary>
        /// El fondo: su prefab, con sus capas repartidas en el grupo que sigue a la cámara y el que
        /// se queda bajo el agua.
        /// </summary>
        public static List<string> ProblemasDelFondo(GameObject prefab)
        {
            var problemas = new List<string>();
            if (prefab == null)
            {
                problemas.Add($"Falta el prefab del fondo ({ConstructorFondoMundo2.RutaPrefab}).");
                return problemas;
            }

            var grupos = prefab.GetComponentsInChildren<ParallaxMovement>(true);
            if (grupos.Count(g => g.SigueCamaraEnY) != 1 || grupos.Count(g => !g.SigueCamaraEnY) != 1)
            {
                problemas.Add($"Prefab {prefab.name}: debe tener un grupo de capas que siga a la cámara y otro anclado bajo el agua.");
            }

            // El relleno es un color liso: es la única capa sin dibujo.
            foreach (var capa in prefab.GetComponentsInChildren<Renderer>(true).Where(c => c.name != ConstructorFondoMundo2.NombreRelleno))
            {
                var material = capa.sharedMaterial;
                if (material == null || (capa is MeshRenderer && material.mainTexture == null))
                {
                    problemas.Add($"Prefab {prefab.name}: la capa «{capa.name}» no tiene su dibujo.");
                }
            }

            return problemas;
        }

        /// <summary>
        /// Las cinemáticas de la Doncella: con sus cuatro planos en el proyecto, la presentación y la
        /// derrota van en la rejilla de esos planos, cada una con los suyos, y el Nivel 2 las lleva.
        /// </summary>
        public static List<string> ProblemasDeLasCinematicas()
        {
            var problemas = new List<string>();
            if (!ConstructorCinematicasNivel2.HayPlanos)
            {
                return problemas;
            }

            var presentacion = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicasNivel2.RutaPresentacion);
            var derrota = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicasNivel2.RutaDerrota);
            problemas.AddRange(ProblemasDeLaCinematica(presentacion, ConstructorCinematicasNivel2.RutaPresentacion,
                new[] { ConstructorCinematicasNivel2.Presentacion1, ConstructorCinematicasNivel2.Presentacion2 }));
            problemas.AddRange(ProblemasDeLaCinematica(derrota, ConstructorCinematicasNivel2.RutaDerrota,
                new[] { ConstructorCinematicasNivel2.Derrota1, ConstructorCinematicasNivel2.Derrota2 }));

            var nivel = ConstructorCinematicasNivel2.Nivel();
            if (nivel != null && presentacion != null && nivel.cinematicaJefe != presentacion)
            {
                problemas.Add($"El Nivel 2 no presenta a su jefe con {Path.GetFileName(ConstructorCinematicasNivel2.RutaPresentacion)}.");
            }

            if (nivel != null && derrota != null && nivel.cinematicaJefeDerrotado != derrota)
            {
                problemas.Add($"El Nivel 2 no lleva {Path.GetFileName(ConstructorCinematicasNivel2.RutaDerrota)}: al caer la Doncella no se vería nada.");
            }

            return problemas;
        }

        /// <summary>Una cinemática de la Doncella: existe, va en la rejilla de sus planos y los lleva todos.</summary>
        public static List<string> ProblemasDeLaCinematica(CutsceneData cinematica, string ruta, IReadOnlyList<string> planos)
        {
            var nombre = Path.GetFileNameWithoutExtension(ruta);
            var problemas = new List<string>();
            if (cinematica == null)
            {
                problemas.Add($"Falta la cinemática {nombre} ({ruta}).");
                return problemas;
            }

            var rejilla = ConstructorCinematicasNivel2.Rejilla;
            if (!cinematica.escalaEntera || cinematica.Rejilla != rejilla)
            {
                problemas.Add($"Cinemática {nombre}: no va en la rejilla de sus planos ({rejilla.x} × {rejilla.y}); se vería solo una esquina, o estirada.");
            }

            var imagenes = cinematica.diapositivas.Where(d => d != null && d.imagen != null).Select(d => d.imagen.name).ToList();
            foreach (var plano in planos.Where(p => !imagenes.Contains(p)))
            {
                problemas.Add($"Cinemática {nombre}: le falta el plano {plano}.");
            }

            return problemas;
        }

        /// <summary>El tileset: si están las hojas, están cortadas con todas sus piezas.</summary>
        public static List<string> ProblemasDelTileset()
        {
            var problemas = new List<string>();
            if (!ConstructorEscenarioMundo2.HayArte)
            {
                return problemas;
            }

            foreach (var pieza in ConstructorEscenarioMundo2.Piezas)
            {
                var sprite = ConstructorEscenarioMundo2.Cargar(pieza);
                if (sprite == null)
                {
                    problemas.Add($"Tileset: falta la pieza {pieza.Nombre} de {Path.GetFileName(pieza.Hoja)} (¿se reimportó la hoja sin cortar?).");
                }
                else if (!Mathf.Approximately(sprite.pixelsPerUnit, pieza.PixelesPorUnidad) ||
                         Vector2.Distance(sprite.rect.size, new Vector2(pieza.Caja.width, pieza.Caja.height)) > 0.01f)
                {
                    problemas.Add($"Tileset: la pieza {pieza.Nombre} mide {sprite.rect.width} × {sprite.rect.height} px a {sprite.pixelsPerUnit} px por unidad; " +
                                  $"debe medir {pieza.Caja.width} × {pieza.Caja.height} a {pieza.PixelesPorUnidad}.");
                }
            }

            return problemas;
        }
    }
}
