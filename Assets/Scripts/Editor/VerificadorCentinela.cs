using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Comprueba que el arte de la Centinela Ardiente sigue montado como lo dejó
    /// <see cref="ConstructorCentinela"/>. Es la red que avisa si alguien reimporta los sprites y
    /// se pierden sus ajustes, o si un clip, el Animator o el prefab cambian a mano:
    /// 1. los 29 sprites existen, con su lienzo, sus píxeles por unidad (49; 58 el idle), el
    ///    pivote de los pies, filtro Point y sin compresión;
    /// 2. los 8 clips existen, con sus fotogramas y la duración de la tabla (margen de 1 ms);
    /// 3. el Animator tiene los 8 estados, ninguno suelto y ninguna transición rota;
    /// 4. en el prefab, todo lo que hace daño está apagado en reposo;
    /// 5. cada evento de animación llama a una función que existe en <see cref="AnimacionJefe"/>.
    ///
    /// Falla ruidosamente: un error en la consola por cada problema, con el nombre de lo que falta
    /// o está mal. Se ejecuta desde el menú y al final de «Regenerar todo», que falla si algo no
    /// cuadra. Cada comprobación recibe lo que revisa, para probarla con objetos rotos a propósito.
    /// </summary>
    public static class VerificadorCentinela
    {
        // Margen de las duraciones (el encargo: 1 ms) y del pivote.
        private const float MargenTiempo = 0.001f;
        private const float MargenPivote = 0.001f;

        public static string RutaPrefab => $"{GeneradorJefes.CarpetaPrefabs}/{ConstructorCentinela.NombreDatos}.prefab";

        [MenuItem("Fragmentum/Jefes/Verificar Centinela", priority = 143)]
        public static void MenuVerificar() => Verificar();

        /// <summary>Revisa todo. Devuelve true si está bien; si no, deja un error por problema en la consola.</summary>
        public static bool Verificar()
        {
            var problemas = Problemas();
            if (problemas.Count == 0)
            {
                Debug.Log($"[Centinela] Verificación correcta: {ConstructorCentinela.Archivos.Length} sprites, {ConstructorCentinela.Clips.Length} clips, el Animator, el prefab y los eventos.");
                return true;
            }

            foreach (var problema in problemas)
            {
                Debug.LogError($"[Centinela] {problema}");
            }

            Debug.LogError($"[Centinela] La verificación falló: {problemas.Count} problema(s). " +
                           "Fragmentum → Jefes → Generar los clips y el Animator de la Centinela lo rehace desde la tabla.");
            return false;
        }

        /// <summary>Todos los problemas encontrados; vacío si todo está bien.</summary>
        public static List<string> Problemas()
        {
            var problemas = new List<string>();
            foreach (var archivo in ConstructorCentinela.Archivos)
            {
                problemas.AddRange(ProblemasDeSprite(archivo, AssetImporter.GetAtPath(ConstructorCentinela.RutaSprite(archivo)) as TextureImporter));
            }

            foreach (var definicion in ConstructorCentinela.Clips)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(definicion.Ruta);
                problemas.AddRange(ProblemasDeClip(definicion, clip));
                if (clip != null)
                {
                    problemas.AddRange(ProblemasDeEventos(clip));
                }
            }

            problemas.AddRange(ProblemasDelControlador(ConstructorCentinela.Controlador));
            problemas.AddRange(ProblemasDelPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab)));
            return problemas;
        }

        // --- 1. Sprites ---

        /// <summary>Un fotograma: existe y conserva los ajustes de importación del LEEME.</summary>
        public static List<string> ProblemasDeSprite(string archivo, TextureImporter importador)
        {
            var problemas = new List<string>();
            void Si(bool mal, string mensaje)
            {
                if (mal)
                {
                    problemas.Add($"Sprite {archivo}: {mensaje}");
                }
            }

            if (importador == null)
            {
                problemas.Add($"Falta el sprite {archivo} ({ConstructorCentinela.RutaSprite(archivo)}).");
                return problemas;
            }

            var ppu = ConstructorCentinela.PixelesPorUnidadDe(archivo);
            var ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);
            importador.GetSourceTextureWidthAndHeight(out var ancho, out var alto);
            var pivote = ajustes.spritePivot;
            var esperado = ConstructorCentinela.Pivote;

            Si(ancho != ConstructorCentinela.AnchoLienzo || alto != ConstructorCentinela.AltoLienzo,
                $"lienzo de {ancho}×{alto} px (debe ser {ConstructorCentinela.AnchoLienzo}×{ConstructorCentinela.AltoLienzo}; no se recorta ni se reescala).");
            Si(importador.textureType != TextureImporterType.Sprite, $"se importa como {importador.textureType}, no como Sprite.");
            Si(importador.spriteImportMode != SpriteImportMode.Single, $"modo {importador.spriteImportMode} (debe ser Single).");
            Si(!Mathf.Approximately(importador.spritePixelsPerUnit, ppu), $"{Numero(importador.spritePixelsPerUnit)} píxeles por unidad (debe ser {Numero(ppu)}).");
            Si(ajustes.spriteAlignment != (int)SpriteAlignment.Custom || Vector2.Distance(pivote, esperado) > MargenPivote,
                $"pivote ({Numero(pivote.x)}, {Numero(pivote.y)}) (debe ser Custom ({Numero(esperado.x)}, {Numero(esperado.y)}), en los pies).");
            Si(ajustes.spriteMeshType != SpriteMeshType.FullRect, $"malla {ajustes.spriteMeshType} (debe ser Full Rect).");
            Si(importador.filterMode != FilterMode.Point, $"filtro {importador.filterMode} (debe ser Point, sin suavizado).");
            Si(importador.textureCompression != TextureImporterCompression.Uncompressed, $"compresión {importador.textureCompression} (debe ser sin compresión).");
            Si(importador.mipmapEnabled, "con mipmaps (deben estar desactivados).");
            Si(importador.wrapMode != TextureWrapMode.Clamp, $"wrap {importador.wrapMode} (debe ser Clamp).");
            return problemas;
        }

        // --- 2. Clips ---

        /// <summary>Un clip: existe, con los fotogramas de la tabla y lo que suman sus duraciones.</summary>
        public static List<string> ProblemasDeClip(ConstructorCentinela.Clip definicion, AnimationClip clip)
        {
            var nombre = Path.GetFileNameWithoutExtension(definicion.Ruta);
            var problemas = new List<string>();
            if (clip == null)
            {
                problemas.Add($"Falta el clip {nombre} ({definicion.Ruta}).");
                return problemas;
            }

            if (Mathf.Abs(clip.length - definicion.Duracion) > MargenTiempo)
            {
                problemas.Add($"Clip {nombre}: dura {Numero(clip.length)} s (la tabla suma {Numero(definicion.Duracion)} s).");
            }

            if (clip.isLooping != definicion.Bucle)
            {
                problemas.Add($"Clip {nombre}: {(definicion.Bucle ? "debe repetirse en bucle" : "no debe repetirse en bucle")}.");
            }

            var enlace = AnimationUtility.GetObjectReferenceCurveBindings(clip).FirstOrDefault(b => b.type == typeof(SpriteRenderer) && b.propertyName == "m_Sprite");
            var claves = enlace.type != null ? AnimationUtility.GetObjectReferenceCurve(clip, enlace) : new ObjectReferenceKeyframe[0];
            for (var i = 0; i < definicion.Fotogramas.Length; i++)
            {
                var inicio = definicion.Inicio(i);
                var clave = claves.FirstOrDefault(k => Mathf.Abs(k.time - inicio) <= MargenTiempo);
                var visto = clave.value != null ? clave.value.name : "nada";
                if (visto != definicion.Fotogramas[i])
                {
                    problemas.Add($"Clip {nombre}: a los {Numero(inicio)} s se ve {visto} (debe ser {definicion.Fotogramas[i]}).");
                }
            }

            return problemas;
        }

        // --- 3. Animator ---

        /// <summary>El controlador: los 8 estados con su clip, ninguno suelto y ninguna transición rota.</summary>
        public static List<string> ProblemasDelControlador(AnimatorController controlador)
        {
            var problemas = new List<string>();
            if (controlador == null || controlador.layers.Length == 0)
            {
                problemas.Add(controlador == null ? $"Falta el Animator ({ConstructorCentinela.RutaControlador})." : "El Animator no tiene capas.");
                return problemas;
            }

            var maquina = controlador.layers[0].stateMachine;
            var estados = maquina.states.Select(s => s.state).ToList();
            foreach (var definicion in ConstructorCentinela.Clips)
            {
                var clip = Path.GetFileNameWithoutExtension(definicion.Ruta);
                var estado = estados.FirstOrDefault(s => s.name == definicion.Estado);
                if (estado == null)
                {
                    problemas.Add($"Animator: falta el estado {definicion.Estado}.");
                }
                else if (estado.motion == null || estado.motion.name != clip)
                {
                    problemas.Add($"Animator: el estado {definicion.Estado} reproduce {(estado.motion != null ? estado.motion.name : "nada")} (debe ser {clip}).");
                }
            }

            foreach (var sobrante in estados.Where(s => ConstructorCentinela.Clips.All(c => c.Estado != s.name)))
            {
                problemas.Add($"Animator: el estado {sobrante.name} no está en la tabla.");
            }

            if (maquina.defaultState == null || !estados.Contains(maquina.defaultState))
            {
                problemas.Add("Animator: no tiene estado inicial.");
            }

            var parametros = new HashSet<string>(controlador.parameters.Select(p => p.name));
            var transiciones = estados.SelectMany(s => s.transitions.Select(t => (desde: s.name, t)))
                .Concat(maquina.anyStateTransitions.Select(t => (desde: "Any State", t))).ToList();
            foreach (var (desde, t) in transiciones)
            {
                if (t.destinationState == null || !estados.Contains(t.destinationState))
                {
                    problemas.Add($"Animator: la transición «{t.name}» desde {desde} no lleva a ningún estado del controlador.");
                    continue;
                }

                var nombre = $"«{t.name}» ({desde} → {t.destinationState.name})";
                if (!t.hasExitTime && t.conditions.Length == 0)
                {
                    problemas.Add($"Animator: la transición {nombre} no tiene condición ni tiempo de salida: se tomaría en cada fotograma.");
                }

                foreach (var condicion in t.conditions.Where(c => !parametros.Contains(c.parameter)))
                {
                    problemas.Add($"Animator: la transición {nombre} usa el parámetro {condicion.parameter}, que no existe.");
                }
            }

            foreach (var suelto in estados.Where(s => s != maquina.defaultState && transiciones.All(x => x.t.destinationState != s)))
            {
                problemas.Add($"Animator: nada lleva al estado {suelto.name} (está suelto).");
            }

            return problemas;
        }

        // --- 4. Prefab ---

        /// <summary>
        /// El prefab: con el arte montado y, en reposo, todo lo que hace daño apagado (el contacto
        /// se enciende al empezar el combate; las zonas de los ataques se crean al golpear) y el
        /// fragmento escondido hasta D4.
        /// </summary>
        public static List<string> ProblemasDelPrefab(GameObject prefab)
        {
            var problemas = new List<string>();
            if (prefab == null)
            {
                problemas.Add($"Falta el prefab del jefe ({RutaPrefab}).");
                return problemas;
            }

            var arte = prefab.GetComponentInChildren<AnimacionJefe>(true);
            if (arte == null)
            {
                problemas.Add($"Prefab {prefab.name}: le falta AnimacionJefe, que recibe los eventos de los clips.");
            }
            else
            {
                var animator = arte.GetComponent<Animator>();
                if (animator == null || animator.runtimeAnimatorController != ConstructorCentinela.Controlador)
                {
                    problemas.Add($"Prefab {prefab.name}: el Animator del sprite no usa {Path.GetFileName(ConstructorCentinela.RutaControlador)}.");
                }

                if (arte.transform.localPosition != Vector3.zero)
                {
                    problemas.Add($"Prefab {prefab.name}: el sprite no está en los pies (posición {arte.transform.localPosition}); el forcejeo fija su altura.");
                }
            }

            var capaArmas = LayerMask.NameToLayer(Capas.EnemyWeapon);
            foreach (var colision in prefab.GetComponentsInChildren<Collider2D>(true))
            {
                var ataque = colision.GetComponent<Hitbox>() != null || colision.gameObject.layer == capaArmas;
                if (ataque && colision.enabled && Encendido(colision.transform))
                {
                    problemas.Add($"Prefab {prefab.name}: el collider de ataque «{colision.name}» está encendido en reposo.");
                }
            }

            var fragmento = prefab.GetComponentInChildren<FragmentoJefe>(true);
            if (fragmento == null)
            {
                problemas.Add($"Prefab {prefab.name}: le falta el fragmento de la ceniza (FragmentoJefe).");
            }
            else if (Encendido(fragmento.transform))
            {
                problemas.Add($"Prefab {prefab.name}: el fragmento está visible en reposo (aparece en D4).");
            }

            return problemas;
        }

        // En un prefab sin instanciar activeInHierarchy siempre es false: se sube a mano.
        private static bool Encendido(Transform objeto)
        {
            for (var t = objeto; t != null; t = t.parent)
            {
                if (!t.gameObject.activeSelf)
                {
                    return false;
                }
            }

            return true;
        }

        // --- 5. Eventos ---

        /// <summary>Cada evento del clip llama a una función pública de <see cref="AnimacionJefe"/> que acepta su parámetro.</summary>
        public static List<string> ProblemasDeEventos(AnimationClip clip)
        {
            var problemas = new List<string>();
            foreach (var evento in AnimationUtility.GetAnimationEvents(clip))
            {
                var metodo = typeof(AnimacionJefe).GetMethod(evento.functionName);
                var parametros = metodo != null ? metodo.GetParameters() : null;
                var valido = metodo != null && metodo.IsPublic && metodo.ReturnType == typeof(void) &&
                             (parametros.Length == 0 || (parametros.Length == 1 && parametros[0].ParameterType == typeof(float)));
                if (!valido)
                {
                    problemas.Add($"Clip {clip.name}: el evento «{evento.functionName}» (a los {Numero(evento.time)} s) no llama a ninguna función de AnimacionJefe.");
                }
            }

            return problemas;
        }

        private static string Numero(float valor) => valor.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
