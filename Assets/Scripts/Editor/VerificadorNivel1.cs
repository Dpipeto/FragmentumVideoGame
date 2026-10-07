using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Levels.Mecanicas;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.ComprobacionesArte;
using Clip = Fragmentum.EditorTools.ClipsDeSprites.Clip;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Comprueba que el arte del Nivel 1 (el Golem de Ceniza, las mecánicas y los planos del jefe)
    /// sigue montado como lo dejaron <see cref="ArteNivel1"/> y <see cref="GeneradorMecanicas"/>. Es
    /// la red que avisa si alguien reimporta un sprite y pierde sus ajustes, o si un clip, un
    /// Animator o un prefab cambian a mano:
    /// 1. los 34 sprites existen (15 del Golem, 15 de las mecánicas con la bola y 4 planos), con el lienzo, los
    ///    píxeles por unidad y el pivote de su carpeta (<see cref="TablaArte"/>) y como pixel art.
    ///    Un PNG nuevo en esas carpetas también se revisa;
    /// 2. los 14 clips existen, con sus fotogramas, sus eventos y la duración de su tabla (margen
    ///    de 1 ms), y ninguno anima un collider;
    /// 3. los 5 Animators (Golem, torreta, plataforma inestable, combustión y bola) tienen un estado por
    ///    clip, ninguno suelto y ninguna transición rota, y en las mecánicas cada valor de
    ///    <c>Estado</c> lleva al estado de su clip;
    /// 4. en los 6 prefabs, lo que hace daño está apagado en reposo, salvo lo que daña siempre: el
    ///    cuerpo del Golem y la bola de fuego (la lava mata con su propio trigger, no con una
    ///    hitbox);
    /// 5. cada evento de animación llama a una función que existe en un script del objeto de su
    ///    Animator;
    /// 6. la plataforma inestable deja de sostener cuando se ve rota (Plataforma_3_rota), y la de
    ///    combustión sostiene en todos sus fotogramas.
    ///
    /// Falla ruidosamente: un error en la consola por cada problema, con el nombre de lo que falta
    /// o está mal. Se ejecuta desde el menú y al final de «Regenerar todo», que falla si algo no
    /// cuadra. El arte que todavía no ha llegado (<see cref="ArtePendiente"/>) no es un fallo: se
    /// avisa. Cada comprobación recibe lo que revisa, para probarla con objetos rotos a propósito.
    /// </summary>
    public static class VerificadorNivel1
    {
        private const string Prefijo = "[Nivel 1]";

        /// <summary>El fotograma de la plataforma partida en tres trozos: con él ya no sostiene.</summary>
        public const string FotogramaRota = "Plataforma_3_rota";

        [MenuItem("Fragmentum/Niveles/Verificar el arte del Nivel 1", priority = 135)]
        public static void MenuVerificar() => Verificar();

        /// <summary>Revisa todo. Devuelve true si está bien; si no, deja un error por problema en la consola.</summary>
        public static bool Verificar()
        {
            foreach (var falta in ArtePendiente())
            {
                Debug.LogWarning($"{Prefijo} Arte pendiente: {falta}");
            }

            var problemas = Problemas();
            if (problemas.Count == 0)
            {
                Debug.Log($"{Prefijo} Verificación correcta: {Sprites.Count} sprites, {Clips.Count} clips, {Animators.Count} Animators, " +
                          $"{Prefabs.Length} prefabs y sus eventos; la plataforma inestable no sostiene rota y la de combustión siempre.");
                return true;
            }

            foreach (var problema in problemas)
            {
                Debug.LogError($"{Prefijo} {problema}");
            }

            Debug.LogError($"{Prefijo} La verificación falló: {problemas.Count} problema(s). " +
                           "Fragmentum → Niveles → Generar el arte del Golem, las mecánicas y las cinemáticas del Nivel 1 lo rehace desde las tablas.");
            return false;
        }

        /// <summary>Todos los problemas encontrados; vacío si todo está bien.</summary>
        public static List<string> Problemas()
        {
            var problemas = new List<string>();
            foreach (var ruta in Sprites.Concat(SpritesNuevos()))
            {
                problemas.AddRange(ProblemasDeSprite(ruta, AssetImporter.GetAtPath(ruta) as TextureImporter));
            }

            foreach (var esperado in Clips)
            {
                problemas.AddRange(ProblemasDeClip(esperado, AssetDatabase.LoadAssetAtPath<AnimationClip>(esperado.Ruta)));
            }

            foreach (var animator in Animators)
            {
                problemas.AddRange(ProblemasDelAnimator(animator, AssetDatabase.LoadAssetAtPath<AnimatorController>(animator.Ruta)));
            }

            foreach (var pieza in Prefabs)
            {
                problemas.AddRange(ProblemasDelPrefab(pieza, AssetDatabase.LoadAssetAtPath<GameObject>(pieza.Ruta)));
            }

            problemas.AddRange(ProblemasDeLaPlataformaInestable(ConstructorMecanicas.Plataforma, GeneradorMecanicas.Cargar(GeneradorMecanicas.PlataformaInestable)));
            problemas.AddRange(ProblemasDeLaCombustion(GeneradorMecanicas.Cargar(GeneradorMecanicas.PlataformaCombustion)));
            return problemas;
        }

        /// <summary>El arte que el encargo anuncia pero aún no ha llegado. No es un fallo.</summary>
        public static IEnumerable<string> ArtePendiente()
        {
            var final2 = ConstructorCinematicasNivel1.RutaImagen(ConstructorCinematicasNivel1.Final2);
            if (!File.Exists(final2))
            {
                yield return $"falta {ConstructorCinematicasNivel1.Final2} ({final2}); la cinemática de la derrota se ve con un solo plano.";
            }
        }

        // --- 1. Sprites ---

        private static readonly string[] Planos =
        {
            ConstructorCinematicasNivel1.Jefe1, ConstructorCinematicasNivel1.Jefe2, ConstructorCinematicasNivel1.Final1,
            ConstructorCinematicasNivel1.Final2,
        };

        /// <summary>
        /// Los 34 sprites del encargo, sacados de las tablas de los constructores: los fotogramas de
        /// los clips del Golem y de las mecánicas, la lava, la bola de fuego y los cuatro planos.
        /// </summary>
        public static IReadOnlyList<string> Sprites =>
            ConstructorGolem.Clips.SelectMany(c => c.Fotogramas).Distinct().Select(ConstructorGolem.RutaSprite)
                .Concat(ConstructorMecanicas.TodosLosClips.SelectMany(c => c.Fotogramas).Concat(ConstructorMecanicas.FotogramasLava)
                    .Concat(ConstructorMecanicas.BolaVuelo.Fotogramas).Append(ConstructorMecanicas.FotogramaBolaDisipa)
                    .Distinct().Select(ConstructorMecanicas.RutaSprite))
                .Concat(Planos.Select(ConstructorCinematicasNivel1.RutaImagen))
                .ToList();

        /// <summary>
        /// Los PNG de las carpetas del Nivel 1 que no están entre <see cref="Sprites"/> (lo que llegue
        /// después: el segundo plano del final, la bola de fuego). También deben seguir su regla.
        /// </summary>
        public static IEnumerable<string> SpritesNuevos()
        {
            var conocidos = new HashSet<string>(Sprites);
            var carpetas = new[] { TablaArte.CarpetaGolem, TablaArte.CarpetaMecanicas, TablaArte.CarpetaCinematicasNivel1 }
                .Where(AssetDatabase.IsValidFolder).ToArray();
            return carpetas.Length == 0
                ? Enumerable.Empty<string>()
                : AssetDatabase.FindAssets("t:Texture2D", carpetas).Select(AssetDatabase.GUIDToAssetPath)
                    .Where(r => !conocidos.Contains(r)).Distinct().OrderBy(r => r, StringComparer.Ordinal);
        }

        /// <summary>
        /// Un sprite: existe y tiene el lienzo, los píxeles por unidad y el pivote de la regla de su
        /// carpeta en <see cref="TablaArte"/>, con los ajustes de pixel art. La regla se busca por
        /// <paramref name="ruta"/>, así que se puede revisar cualquier importador contra ella.
        /// </summary>
        public static List<string> ProblemasDeSprite(string ruta, TextureImporter importador)
        {
            var nombre = Path.GetFileNameWithoutExtension(ruta);
            var regla = TablaArte.ReglaDe(ruta);
            if (regla == null)
            {
                return new List<string> { $"Sprite {nombre}: no tiene regla en TablaArte ({ruta})." };
            }

            return ComprobacionesArte.ProblemasDeSprite(nombre, ruta, importador, regla.PixelesPorUnidad ?? 100f,
                regla.Alineacion, regla.Pivote ?? new Vector2(0.5f, 0.5f), regla.Lienzo);
        }

        // --- 2. Clips ---

        /// <summary>Un clip de las tablas: dónde está, su definición y los eventos que debe llevar.</summary>
        public sealed class ClipEsperado
        {
            public string Ruta;
            public Clip Definicion;
            public (float instante, string funcion)[] Eventos;

            public string Nombre => Path.GetFileNameWithoutExtension(Ruta);
        }

        /// <summary>Los 13 clips: 4 del Golem, con sus eventos, y 9 de las mecánicas, sin eventos.</summary>
        public static IReadOnlyList<ClipEsperado> Clips =>
            ConstructorGolem.Clips.Select(c => new ClipEsperado
                {
                    Ruta = c.Ruta(ConstructorGolem.CarpetaAnimaciones),
                    Definicion = c,
                    Eventos = ConstructorGolem.Eventos.Where(e => e.Estado == c.Estado)
                        .Select(e => (ConstructorGolem.Instante(e), e.Funcion)).ToArray(),
                })
                .Concat(ConstructorMecanicas.TodosLosClips.Append(ConstructorMecanicas.BolaVuelo).Select(c => new ClipEsperado
                {
                    Ruta = c.Ruta(ConstructorMecanicas.CarpetaAnimaciones),
                    Definicion = c,
                    Eventos = new (float, string)[0],
                }))
                .ToList();

        /// <summary>
        /// Un clip: existe, con los fotogramas, el bucle y la duración de su tabla, exactamente los
        /// eventos de la tabla, y sin animar ningún collider (los gobiernan los scripts).
        /// </summary>
        public static List<string> ProblemasDeClip(ClipEsperado esperado, AnimationClip clip)
        {
            var definicion = esperado.Definicion;
            var problemas = ComprobacionesArte.ProblemasDeClip(esperado.Ruta, clip, definicion.Duracion, definicion.Bucle,
                definicion.Fotogramas.Select((fotograma, i) => (definicion.Inicio(i), fotograma)));
            if (clip == null)
            {
                return problemas;
            }

            var eventos = AnimationUtility.GetAnimationEvents(clip);
            bool Coincide(AnimationEvent evento, (float instante, string funcion) deseado) =>
                evento.functionName == deseado.funcion && Mathf.Abs(evento.time - deseado.instante) <= MargenTiempo;

            foreach (var deseado in esperado.Eventos.Where(d => !eventos.Any(e => Coincide(e, d))))
            {
                problemas.Add($"Clip {esperado.Nombre}: falta el evento «{deseado.funcion}» a los {Numero(deseado.instante)} s.");
            }

            foreach (var sobrante in eventos.Where(e => !esperado.Eventos.Any(d => Coincide(e, d))))
            {
                problemas.Add($"Clip {esperado.Nombre}: el evento «{sobrante.functionName}» a los {Numero(sobrante.time)} s no está en la tabla.");
            }

            var enlaces = AnimationUtility.GetCurveBindings(clip).Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip));
            foreach (var enlace in enlaces.Where(AnimaUnCollider))
            {
                var donde = string.IsNullOrEmpty(enlace.path) ? string.Empty : $" de «{enlace.path}»";
                problemas.Add($"Clip {esperado.Nombre}: anima {enlace.type.Name}.{enlace.propertyName}{donde}; los colliders los gobierna el script, no el dibujo.");
            }

            return problemas;
        }

        // Una curva que enciende o apaga un collider (o el objeto que lo lleva).
        private static bool AnimaUnCollider(EditorCurveBinding enlace) =>
            typeof(Collider2D).IsAssignableFrom(enlace.type) || (enlace.type == typeof(GameObject) && enlace.propertyName == "m_IsActive");

        // --- 3. Animators ---

        /// <summary>Un controlador de la tabla: su nombre, dónde está, sus estados y, si es de una mecánica, cuál.</summary>
        public sealed class AnimatorEsperado
        {
            public string Nombre;
            public string Ruta;
            public (string estado, string clip)[] Estados;
            public ConstructorMecanicas.Mecanica Mecanica;
        }

        public static IReadOnlyList<AnimatorEsperado> Animators =>
            new[]
                {
                    new AnimatorEsperado
                    {
                        Nombre = ConstructorGolem.NombreDatos,
                        Ruta = ConstructorGolem.RutaControlador,
                        Estados = ConstructorGolem.Clips.Select(c => (c.Estado, c.Nombre)).ToArray(),
                    },
                }
                .Concat(ConstructorMecanicas.Mecanicas.Select(m => new AnimatorEsperado
                {
                    Nombre = m.Nombre,
                    Ruta = m.RutaControlador,
                    Estados = m.Clips.Select(c => (c.Estado, c.Nombre)).ToArray(),
                    Mecanica = m,
                }))
                .Append(new AnimatorEsperado
                {
                    Nombre = "Bola",
                    Ruta = ConstructorMecanicas.RutaControladorBola,
                    Estados = new[] { (ConstructorMecanicas.BolaVuelo.Estado, ConstructorMecanicas.BolaVuelo.Nombre) },
                })
                .ToList();

        /// <summary>
        /// Un controlador: un estado por clip, ninguno suelto ni ninguna transición rota y, en las
        /// mecánicas, cada valor de su enum lleva por <c>Estado</c> al estado de su clip.
        /// </summary>
        public static List<string> ProblemasDelAnimator(AnimatorEsperado esperado, AnimatorController controlador)
        {
            var quien = $"Animator {esperado.Nombre}";
            var problemas = ProblemasDelControlador(controlador, esperado.Ruta, quien, esperado.Estados);
            var mecanica = esperado.Mecanica;
            if (controlador == null || controlador.layers.Length == 0 || mecanica == null)
            {
                return problemas;
            }

            var valores = Enum.GetNames(mecanica.Estados);
            if (valores.Length != mecanica.Clips.Length)
            {
                problemas.Add($"{quien}: {mecanica.Estados.Name} tiene {valores.Length} valores y la tabla {mecanica.Clips.Length} clips; tiene que haber uno por valor.");
                return problemas;
            }

            var parametro = AnimatorParams.EstadoMecanica.Nombre;
            var transiciones = controlador.layers[0].stateMachine.anyStateTransitions;
            for (var i = 0; i < valores.Length; i++)
            {
                var estado = mecanica.Clips[i].Estado;
                var valor = i;
                var lleva = transiciones.Any(t => t.destinationState != null && t.destinationState.name == estado &&
                                                  t.conditions.Any(c => c.parameter == parametro && c.mode == AnimatorConditionMode.Equals &&
                                                                        Mathf.Approximately(c.threshold, valor)));
                if (!lleva)
                {
                    problemas.Add($"{quien}: {parametro} = {i} ({valores[i]}) no lleva a {estado}; el dibujo no seguiría al script.");
                }
            }

            return problemas;
        }

        // --- 4 y 5. Prefabs y eventos ---

        /// <summary>
        /// Un prefab con arte: su controlador (vacío si no tiene Animator), las zonas de daño que
        /// debe tener y las que dañan siempre (las demás, apagadas en reposo).
        /// </summary>
        public sealed class PrefabEsperado
        {
            public string Ruta;
            public string Controlador;
            public string[] ZonasDeDanio = new string[0];
            public string[] DananSiempre = new string[0];

            public string Nombre => Path.GetFileNameWithoutExtension(Ruta);
        }

        private static string RutaMecanica(string nombre) => $"{GeneradorMecanicas.Carpeta}/{nombre}.prefab";

        public static readonly PrefabEsperado[] Prefabs =
        {
            // El cuerpo del Golem hace daño siempre (danioContacto); la onda, solo en A3.
            new PrefabEsperado
            {
                Ruta = ConstructorGolem.RutaPrefab, Controlador = ConstructorGolem.RutaControlador,
                ZonasDeDanio = new[] { "Arma", "Contacto" }, DananSiempre = new[] { "Contacto" },
            },
            new PrefabEsperado
            {
                Ruta = RutaMecanica(GeneradorMecanicas.TorretaFuego), Controlador = ConstructorMecanicas.Torreta.RutaControlador,
                ZonasDeDanio = new[] { "Llama" },
            },
            new PrefabEsperado
            {
                Ruta = RutaMecanica(GeneradorMecanicas.PlataformaInestable), Controlador = ConstructorMecanicas.Plataforma.RutaControlador,
            },
            new PrefabEsperado
            {
                Ruta = RutaMecanica(GeneradorMecanicas.PlataformaCombustion), Controlador = ConstructorMecanicas.Combustion.RutaControlador,
                ZonasDeDanio = new[] { "Llama" },
            },
            // La lava mata con su propio trigger (no es una hitbox) y no tiene clip.
            new PrefabEsperado { Ruta = RutaMecanica(GeneradorMecanicas.Lava) },
            // La bola solo existe disparada: hacer daño es todo lo que hace.
            new PrefabEsperado
            {
                Ruta = RutaMecanica(GeneradorMecanicas.BolaDeFuego), Controlador = ConstructorMecanicas.RutaControladorBola,
                ZonasDeDanio = new[] { GeneradorMecanicas.BolaDeFuego }, DananSiempre = new[] { GeneradorMecanicas.BolaDeFuego },
            },
        };

        /// <summary>
        /// Un prefab: existe, su dibujo usa su controlador (si no, se generó sin arte), tiene sus
        /// zonas de daño y, en reposo, las apagadas lo están; y cada evento de sus clips llama a una
        /// función de un script del objeto de su Animator.
        /// </summary>
        public static List<string> ProblemasDelPrefab(PrefabEsperado esperado, GameObject prefab)
        {
            var problemas = new List<string>();
            if (prefab == null)
            {
                problemas.Add($"Falta el prefab {esperado.Nombre} ({esperado.Ruta}).");
                return problemas;
            }

            var zonas = CollidersDeDanio(prefab).Select(c => c.name).ToList();
            foreach (var zona in esperado.ZonasDeDanio.Where(z => !zonas.Contains(z)))
            {
                problemas.Add($"Prefab {prefab.name}: le falta su zona de daño «{zona}».");
            }

            problemas.AddRange(ProblemasDeDanioEnReposo(prefab, c => esperado.DananSiempre.Contains(c.name)));

            if (string.IsNullOrEmpty(esperado.Controlador))
            {
                return problemas;
            }

            var animator = prefab.GetComponentInChildren<Animator>(true);
            var controlador = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(esperado.Controlador);
            if (animator == null || animator.runtimeAnimatorController == null || animator.runtimeAnimatorController != controlador)
            {
                problemas.Add($"Prefab {prefab.name}: su dibujo no usa {Path.GetFileName(esperado.Controlador)} (¿se generó sin arte?).");
            }

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                var receptores = ReceptoresDe(animator);
                foreach (var clip in animator.runtimeAnimatorController.animationClips.Where(c => c != null).Distinct())
                {
                    problemas.AddRange(ProblemasDeEventos(clip, receptores));
                }
            }

            return problemas;
        }

        // --- 6. Lo que sostiene ---

        /// <summary>
        /// La plataforma inestable deja de sostener en cuanto se ve rota: cada estado que muestra
        /// <see cref="FotogramaRota"/> es un valor de <c>Estado</c> en el que su script apaga el
        /// collider (<see cref="PlataformaInestable.SostieneEn"/>). Que el Animator lleve cada valor a
        /// su estado y que el clip muestre esos fotogramas lo miran las comprobaciones 2 y 3. En el
        /// prefab, intacta, sostiene.
        /// </summary>
        public static List<string> ProblemasDeLaPlataformaInestable(ConstructorMecanicas.Mecanica mecanica, GameObject prefab)
        {
            var problemas = new List<string>();
            var rotas = mecanica.Clips.Where(c => c.Fotogramas.Contains(FotogramaRota)).ToList();
            if (rotas.Count == 0)
            {
                problemas.Add($"Plataforma inestable: ningún estado muestra {FotogramaRota}.");
            }

            foreach (var clip in rotas)
            {
                var estado = (ESTADOS_PLATAFORMA)mecanica.ValorDe(clip.Estado);
                if (PlataformaInestable.SostieneEn(estado))
                {
                    problemas.Add($"Plataforma inestable: en {clip.Estado} (Estado = {estado}) se ve {FotogramaRota} y su collider sigue activo.");
                }
            }

            if (prefab != null && !Sostiene(prefab))
            {
                problemas.Add($"Prefab {prefab.name}: intacta no sostiene (su collider sólido falta o está apagado).");
            }

            return problemas;
        }

        /// <summary>
        /// La plataforma de combustión sostiene en todos sus fotogramas: su suelo es el collider
        /// sólido de la raíz, encendido, que su script no toca; la llama, que se enciende y se apaga,
        /// está en otro objeto (apagarla no apaga el suelo). Que ningún clip anime un collider lo
        /// mira la comprobación 2.
        /// </summary>
        public static List<string> ProblemasDeLaCombustion(GameObject prefab)
        {
            var problemas = new List<string>();
            if (prefab == null)
            {
                return problemas; // ya lo nombra la comprobación de los prefabs
            }

            if (!Sostiene(prefab))
            {
                problemas.Add($"Prefab {prefab.name}: su suelo (el collider sólido de la raíz) falta o está apagado.");
            }

            var combustion = prefab.GetComponent<PlataformaCombustion>();
            var llama = combustion != null ? combustion.Llama : null;
            if (llama == null)
            {
                problemas.Add($"Prefab {prefab.name}: le falta la llama.");
            }
            else if (llama.gameObject == prefab || prefab.GetComponents<Collider2D>().Any(c => !c.isTrigger && c.transform.IsChildOf(llama.transform)))
            {
                problemas.Add($"Prefab {prefab.name}: la llama comparte objeto con el suelo; al apagarla dejaría de sostener.");
            }

            return problemas;
        }

        // El collider sólido de la raíz: existe y está encendido.
        private static bool Sostiene(GameObject prefab)
        {
            var solidos = prefab.GetComponents<Collider2D>().Where(c => !c.isTrigger).ToList();
            return solidos.Count > 0 && solidos.All(c => c.enabled);
        }
    }
}
