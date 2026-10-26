using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Levels.Escenario;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Narrative;
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
    /// Comprueba que el arte del Nivel 3 (el Espectro, el Centinela Ciego, el Acechador, el Espectro
    /// del Pavor y su lluvia de hojas, las mecánicas, el fondo y el tileset) sigue montado como lo
    /// dejaron <see cref="ArteMundo3"/>, <see cref="ConstructorFondoMundo3"/> y
    /// <see cref="ConstructorEscenarioMundo3"/>. Con las comprobaciones de <see cref="VerificadorNivel1"/>:
    /// 1. cada PNG de las carpetas del mundo tiene su regla en <see cref="TablaArte"/> y la cumple
    ///    (lienzo, píxeles por unidad, pivote y ajustes de pixel art), y no falta ningún fotograma;
    /// 2. cada clip existe, con sus fotogramas, su duración y exactamente los eventos de su tabla;
    /// 3. cada Animator tiene un estado por clip, ninguno suelto y ninguna transición rota;
    /// 4. en cada prefab, el dibujo usa su controlador, lo que hace daño está apagado en reposo
    ///    (salvo lo que daña siempre) y cada evento llama a una función que existe;
    /// 5. lo demás: el Pavor (su luciérnaga dibujada, su fragmento y sus hojas), las mecánicas
    ///    vestidas (el cepo con sus dos dibujos y la rama podrida con sus cuatro), el fondo, el tileset
    ///    y las cinemáticas del Pavor (en la rejilla de sus planos, con todos ellos, y en el Nivel 3).
    ///
    /// Falla ruidosamente y se ejecuta al final de «Regenerar todo». Sin el arte del mundo en el
    /// proyecto no hay nada que revisar: avisa y da por buena la verificación.
    /// </summary>
    public static class VerificadorNivel3
    {
        private const string Prefijo = "[Nivel 3]";

        [MenuItem("Fragmentum/Niveles/Verificar el arte del Nivel 3", priority = 139)]
        public static void MenuVerificar() => Verificar();

        /// <summary>Las carpetas con los PNG del mundo (sin el fondo ni el tileset, que no siguen <see cref="TablaArte"/>).</summary>
        public static readonly string[] Carpetas =
        {
            ConstructorEspectro.CarpetaSprites, ConstructorCentinelaCiego.CarpetaSprites, ConstructorAcechador.CarpetaSprites,
            ConstructorPavor.CarpetaSprites, ConstructorPavor.CarpetaHojas, ConstructorPavor.CarpetaLuciernaga,
            ConstructorMecanicasMundo3.CarpetaMecanicas, ConstructorCinematicasNivel3.Carpeta,
        };

        /// <summary>¿Está en el proyecto el arte del mundo?</summary>
        public static bool HayArte => Carpetas.Any(AssetDatabase.IsValidFolder);

        /// <summary>Revisa todo. Devuelve true si está bien; si no, deja un error por problema en la consola.</summary>
        public static bool Verificar()
        {
            if (!HayArte)
            {
                Debug.LogWarning($"{Prefijo} El arte del Mundo 3 no está en el proyecto: no hay nada que verificar.");
                return true;
            }

            var problemas = Problemas();
            if (problemas.Count == 0)
            {
                Debug.Log($"{Prefijo} Verificación correcta: {Sprites().Count} sprites, {Clips.Count} clips, {Animators.Count} Animators, " +
                          $"{Prefabs.Length} prefabs y sus eventos; el Pavor, las mecánicas, el fondo, el tileset y las cinemáticas, completos.");
                return true;
            }

            foreach (var problema in problemas)
            {
                Debug.LogError($"{Prefijo} {problema}");
            }

            Debug.LogError($"{Prefijo} La verificación falló: {problemas.Count} problema(s). " +
                           "Fragmentum → Niveles → Generar el arte del Mundo 3 (Miedo) lo rehace desde las tablas.");
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

            problemas.AddRange(ProblemasDelPavor(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPavor)));
            problemas.AddRange(ProblemasDeLasHojas(AssetDatabase.LoadAssetAtPath<PatronHojas>($"{GeneradorJefes.CarpetaPatrones}/Hojas.asset")));
            problemas.AddRange(ProblemasDeLasMecanicas());
            problemas.AddRange(ProblemasDelFondo(AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorFondoMundo3.RutaPrefab)));
            problemas.AddRange(ProblemasDelTileset());
            problemas.AddRange(ProblemasDeLasCinematicas());
            return problemas;
        }

        // --- 1. Sprites ---

        /// <summary>Los fotogramas que nombran las tablas de los constructores: todos tienen que estar.</summary>
        public static IEnumerable<string> FotogramasDeLasTablas() =>
            ConstructorEspectro.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorEspectro.RutaSprite)
                .Concat(ConstructorCentinelaCiego.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorCentinelaCiego.RutaSprite))
                .Concat(ConstructorAcechador.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorAcechador.RutaSprite))
                .Concat(ConstructorPavor.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorPavor.RutaSprite))
                .Concat(ConstructorPavor.ClipsHojas.SelectMany(c => c.Fotogramas).Concat(ConstructorPavor.FotogramasHoja).Select(ConstructorPavor.RutaHoja))
                .Concat(ConstructorPavor.FotogramasLuciernaga.Select(ConstructorPavor.RutaLuciernaga))
                .Concat(ConstructorMecanicasMundo3.MecanicaAntorcha.Clips.SelectMany(c => c.Fotogramas).Select(ConstructorMecanicasMundo3.Antorcha))
                .Concat(new[] { "Abierto", "Cerrado" }.Select(ConstructorMecanicasMundo3.Cepo))
                .Concat(new[] { "Rama", "RamaPodrida", "RamaRota", "Astillas" }.Select(ConstructorMecanicasMundo3.Rama))
                .Concat(ConstructorMecanicasMundo3.FotogramasCinta.Select(ConstructorMecanicasMundo3.Cinta))
                .Concat(ConstructorMecanicasMundo3.FotogramasAgua.Select(ConstructorMecanicasMundo3.Agua))
                .Concat(ConstructorMecanicasMundo3.FotogramasMaranaReposo.Concat(ConstructorMecanicasMundo3.FotogramasMaranaAtrapa)
                    .Select(ConstructorMecanicasMundo3.Marana))
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
            De(ConstructorEspectro.Clips, ConstructorEspectro.CarpetaAnimaciones, ConstructorLamento.EventosDe)
                .Concat(De(ConstructorCentinelaCiego.Clips, ConstructorCentinelaCiego.CarpetaAnimaciones, ConstructorCentinelaCiego.EventosDe))
                .Concat(De(ConstructorAcechador.Clips, ConstructorAcechador.CarpetaAnimaciones, ConstructorAcechador.EventosDe))
                .Concat(De(ConstructorPavor.Clips, ConstructorPavor.CarpetaAnimaciones, ConstructorPavor.EventosDe))
                .Concat(De(ConstructorPavor.ClipsHojas, ConstructorEfectos.CarpetaAnimaciones))
                .Concat(De(ConstructorMecanicasMundo3.MecanicaAntorcha.Clips, ConstructorMecanicasMundo3.CarpetaAnimaciones))
                .ToList();

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
            De(ConstructorEspectro.NombreDatos, ConstructorEspectro.RutaControlador, ConstructorEspectro.Clips),
            De(ConstructorCentinelaCiego.NombreDatos, ConstructorCentinelaCiego.RutaControlador, ConstructorCentinelaCiego.Clips),
            De(ConstructorAcechador.NombreDatos, ConstructorAcechador.RutaControlador, ConstructorAcechador.Clips),
            De(ConstructorPavor.NombreDatos, ConstructorPavor.RutaControlador, ConstructorPavor.Clips),
            De("HojasDelPavor", ConstructorPavor.RutaControladorHojas, ConstructorPavor.ClipsHojas),
            De(ConstructorMecanicasMundo3.MecanicaAntorcha.Nombre, ConstructorMecanicasMundo3.RutaControladorAntorcha,
                ConstructorMecanicasMundo3.MecanicaAntorcha.Clips, ConstructorMecanicasMundo3.MecanicaAntorcha),
        };

        // --- 4. Prefabs ---

        public static string RutaPrefabPavor => $"{GeneradorJefes.CarpetaPrefabs}/{ConstructorPavor.NombreDatos}.prefab";

        private static string RutaMecanica(string nombre) => $"{GeneradorMecanicas.Carpeta}/{nombre}.prefab";

        public static readonly PrefabEsperado[] Prefabs =
        {
            // El cuerpo del Espectro y del Centinela Ciego hace daño siempre; su golpe, solo en su fotograma.
            new PrefabEsperado
            {
                Ruta = ConstructorEspectro.RutaPrefab, Controlador = ConstructorEspectro.RutaControlador,
                ZonasDeDanio = new[] { "Arma", "Contacto" }, DananSiempre = new[] { "Contacto" },
            },
            new PrefabEsperado
            {
                Ruta = ConstructorCentinelaCiego.RutaPrefab, Controlador = ConstructorCentinelaCiego.RutaControlador,
                ZonasDeDanio = new[] { "Arma", "Contacto" }, DananSiempre = new[] { "Contacto" },
            },
            // Tocar al Acechador no hace daño: su mordisco y su embestida, solo cuando los lanza.
            new PrefabEsperado
            {
                Ruta = ConstructorAcechador.RutaPrefab, Controlador = ConstructorAcechador.RutaControlador,
                ZonasDeDanio = new[] { "Arma", "Contacto", "Embestida" },
            },
            new PrefabEsperado
            {
                Ruta = RutaPrefabPavor, Controlador = ConstructorPavor.RutaControlador,
                ZonasDeDanio = new[] { "Contacto" },
            },
            new PrefabEsperado { Ruta = ConstructorPavor.RutaPrefabHojas, Controlador = ConstructorPavor.RutaControladorHojas },
            new PrefabEsperado { Ruta = RutaMecanica(GeneradorMecanicas.Antorcha), Controlador = ConstructorMecanicasMundo3.RutaControladorAntorcha },
        };

        // --- 5. Lo demás ---

        /// <summary>El Pavor: su luciérnaga es la dibujada (de dos fotogramas), deja su fragmento y su dibujo no depende de la luz.</summary>
        public static List<string> ProblemasDelPavor(GameObject prefab)
        {
            var problemas = new List<string>();
            var pavor = prefab != null ? prefab.GetComponent<EspectroDelPavor>() : null;
            if (pavor == null)
            {
                return problemas; // que falte ya lo dice la revisión de los prefabs
            }

            var luciernaga = pavor.FotogramasLuciernaga;
            if (luciernaga == null || luciernaga.Count != ConstructorPavor.FotogramasLuciernaga.Length || luciernaga.Any(s => s == null))
            {
                problemas.Add($"Prefab {prefab.name}: le falta su luciérnaga dibujada; la figura verdadera llevaría un punto provisional.");
            }

            if (pavor.Fragmento == null)
            {
                problemas.Add($"Prefab {prefab.name}: no deja fragmento (se mide en M5, sobre el montón de hojas).");
            }
            else
            {
                var brillo = pavor.Fragmento.GetComponent<BrilloDeFragmento>();
                if (brillo == null || brillo.Copia == null || brillo.Luz == null || brillo.Destello == null)
                {
                    problemas.Add($"Prefab {prefab.name}: su fragmento no brilla; azul oscuro sobre el montón, apenas se ve.");
                }
                else if (!ConstructorPavor.AlumbraLaCapa(brillo.Luz, brillo.Copia.sortingLayerID))
                {
                    problemas.Add($"Prefab {prefab.name}: la luz de su fragmento no alumbra la capa de la copia; no se vería.");
                }
            }

            if (prefab.GetComponentInChildren<AnimacionJefe>(true) == null)
            {
                problemas.Add($"Prefab {prefab.name}: le falta AnimacionJefe; ni gritaría ni caería con su dibujo.");
            }

            return problemas;
        }

        /// <summary>El patrón de las hojas lleva su dibujo; sin él, cada franja sería un rectángulo.</summary>
        public static List<string> ProblemasDeLasHojas(PatronHojas hojas)
        {
            var problemas = new List<string>();
            if (hojas != null && hojas.efecto == null)
            {
                problemas.Add($"Patrón {hojas.name}: no lleva el dibujo de sus franjas ({ConstructorPavor.RutaPrefabHojas}).");
            }

            var dibujo = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorPavor.RutaPrefabHojas);
            if (dibujo != null && dibujo.GetComponent<LluviaDeHojas>() == null)
            {
                problemas.Add($"Prefab {dibujo.name}: le falta su LluviaDeHojas; el suelo brillaría y no caería nada.");
            }

            return problemas;
        }

        /// <summary>El cepo lleva sus dos dibujos y la rama podrida, sus cuatro (sana, podrida, rota y astillas).</summary>
        public static List<string> ProblemasDeLasMecanicas()
        {
            var problemas = new List<string>();
            var cepo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaMecanica(GeneradorMecanicas.Cepo));
            if (cepo != null)
            {
                var serializado = new SerializedObject(cepo.GetComponent<Cepo>());
                if (serializado.FindProperty("spriteAbierto").objectReferenceValue == null || serializado.FindProperty("spriteCerrado").objectReferenceValue == null)
                {
                    problemas.Add($"Prefab {cepo.name}: le faltan sus dibujos (abierto y cerrado); sería un rectángulo.");
                }
            }

            var rama = AssetDatabase.LoadAssetAtPath<GameObject>(RutaMecanica(GeneradorMecanicas.RamaPodrida));
            if (rama != null && !rama.GetComponent<RamaPodrida>().ConArte)
            {
                problemas.Add($"Prefab {rama.name}: le faltan sus dibujos (sana, podrida, rota y astillas); sería un rectángulo.");
            }

            return problemas;
        }

        /// <summary>El fondo: sus capas con su dibujo, el hito y lo que lo oscurece con la luz del nivel.</summary>
        public static List<string> ProblemasDelFondo(GameObject prefab)
        {
            var problemas = new List<string>();
            if (prefab == null)
            {
                problemas.Add($"Falta el prefab del fondo ({ConstructorFondoMundo3.RutaPrefab}).");
                return problemas;
            }

            if (prefab.GetComponent<FondoConLaLuz>() == null)
            {
                problemas.Add($"Prefab {prefab.name}: le falta FondoConLaLuz; en el bosque cerrado y en la arena se vería con luz.");
            }

            var capas = prefab.GetComponentsInChildren<Renderer>(true).ToList();
            if (capas.Count != ConstructorFondoMundo3.CapasFondo.Length)
            {
                problemas.Add($"Prefab {prefab.name}: tiene {capas.Count} capas y deben ser {ConstructorFondoMundo3.CapasFondo.Length}.");
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
        /// Las cinemáticas del Pavor: con sus cuatro planos en el proyecto, la presentación y la
        /// derrota van en la rejilla de esos planos, cada una con los suyos, y el Nivel 3 las lleva.
        /// </summary>
        public static List<string> ProblemasDeLasCinematicas()
        {
            var problemas = new List<string>();
            if (!ConstructorCinematicasNivel3.HayPlanos)
            {
                return problemas;
            }

            var presentacion = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicasNivel3.RutaPresentacion);
            var derrota = AssetDatabase.LoadAssetAtPath<CutsceneData>(ConstructorCinematicasNivel3.RutaDerrota);
            problemas.AddRange(VerificadorNivel2.ProblemasDeLaCinematica(presentacion, ConstructorCinematicasNivel3.RutaPresentacion,
                new[] { ConstructorCinematicasNivel3.Presentacion1, ConstructorCinematicasNivel3.Presentacion2 }));
            problemas.AddRange(VerificadorNivel2.ProblemasDeLaCinematica(derrota, ConstructorCinematicasNivel3.RutaDerrota,
                new[] { ConstructorCinematicasNivel3.Derrota1, ConstructorCinematicasNivel3.Derrota2 }));

            var nivel = ConstructorCinematicasNivel3.Nivel();
            if (nivel != null && presentacion != null && nivel.cinematicaJefe != presentacion)
            {
                problemas.Add($"El Nivel 3 no presenta a su jefe con {Path.GetFileName(ConstructorCinematicasNivel3.RutaPresentacion)}.");
            }

            if (nivel != null && derrota != null && nivel.cinematicaJefeDerrotado != derrota)
            {
                problemas.Add($"El Nivel 3 no lleva {Path.GetFileName(ConstructorCinematicasNivel3.RutaDerrota)}: al caer el Pavor no se vería nada.");
            }

            return problemas;
        }

        /// <summary>El tileset: cada pieza cortada, a su escala y de su tamaño.</summary>
        public static List<string> ProblemasDelTileset()
        {
            var problemas = new List<string>();
            if (!ConstructorEscenarioMundo3.HayArte)
            {
                return problemas;
            }

            foreach (var pieza in ConstructorEscenarioMundo3.Piezas)
            {
                var sprite = ConstructorEscenarioMundo3.Cargar(pieza);
                if (sprite == null)
                {
                    problemas.Add($"Tileset: falta la pieza {pieza.Nombre} de {Path.GetFileName(pieza.Hoja)} (¿se reimportó la hoja sin cortar?).");
                }
                else if (!Mathf.Approximately(sprite.pixelsPerUnit, pieza.PixelesPorUnidad) ||
                         Vector2.Distance(sprite.rect.size, new Vector2(pieza.Caja.width, pieza.Caja.height)) > 0.01f)
                {
                    problemas.Add($"Tileset: la pieza {pieza.Nombre} no mide lo que se midió ({pieza.Caja.width}×{pieza.Caja.height} a {pieza.PixelesPorUnidad} px/u).");
                }
            }

            return problemas;
        }
    }
}
