using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Las comprobaciones que comparten los verificadores del arte (<see cref="VerificadorCentinela"/>
    /// y <see cref="VerificadorNivel1"/>). Cada una recibe lo que revisa, para probarla con objetos
    /// rotos a propósito, y devuelve un mensaje por problema con el nombre de lo que falta o está
    /// mal; vacía si todo está bien.
    /// </summary>
    public static class ComprobacionesArte
    {
        /// <summary>Margen de las duraciones (el encargo: 1 ms).</summary>
        public const float MargenTiempo = 0.001f;

        /// <summary>Margen del pivote, normalizado.</summary>
        public const float MargenPivote = 0.001f;

        // --- Sprites ---

        /// <summary>
        /// Un sprite suelto de pixel art: existe, con su lienzo entero, sus píxeles por unidad, su
        /// pivote, filtro Point, sin compresión ni mipmaps, Clamp y malla Full Rect.
        /// <paramref name="dondeElPivote"/> se añade al mensaje del pivote (<c>", en los pies"</c>).
        /// </summary>
        public static List<string> ProblemasDeSprite(string nombre, string ruta, TextureImporter importador, float pixelesPorUnidad,
            SpriteAlignment alineacion, Vector2 pivote, Vector2Int? lienzo, string dondeElPivote = "")
        {
            var problemas = new List<string>();
            void Si(bool mal, string mensaje)
            {
                if (mal)
                {
                    problemas.Add($"Sprite {nombre}: {mensaje}");
                }
            }

            if (importador == null)
            {
                problemas.Add($"Falta el sprite {nombre} ({ruta}).");
                return problemas;
            }

            var ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);
            importador.GetSourceTextureWidthAndHeight(out var ancho, out var alto);
            var actual = ajustes.spritePivot;

            if (lienzo is { } esperado)
            {
                Si(ancho != esperado.x || alto != esperado.y,
                    $"lienzo de {ancho}×{alto} px (debe ser {esperado.x}×{esperado.y}; no se recorta ni se reescala).");
            }

            Si(importador.textureType != TextureImporterType.Sprite, $"se importa como {importador.textureType}, no como Sprite.");
            Si(importador.spriteImportMode != SpriteImportMode.Single, $"modo {importador.spriteImportMode} (debe ser Single).");
            Si(!Mathf.Approximately(importador.spritePixelsPerUnit, pixelesPorUnidad),
                $"{Numero(importador.spritePixelsPerUnit)} píxeles por unidad (debe ser {Numero(pixelesPorUnidad)}).");
            Si(ajustes.spriteAlignment != (int)alineacion || Vector2.Distance(actual, pivote) > MargenPivote,
                $"pivote ({Numero(actual.x)}, {Numero(actual.y)}) (debe ser {alineacion} ({Numero(pivote.x)}, {Numero(pivote.y)}){dondeElPivote}).");
            Si(ajustes.spriteMeshType != SpriteMeshType.FullRect, $"malla {ajustes.spriteMeshType} (debe ser Full Rect).");
            Si(importador.filterMode != FilterMode.Point, $"filtro {importador.filterMode} (debe ser Point, sin suavizado).");
            Si(importador.textureCompression != TextureImporterCompression.Uncompressed, $"compresión {importador.textureCompression} (debe ser sin compresión).");
            Si(importador.mipmapEnabled, "con mipmaps (deben estar desactivados).");
            Si(importador.wrapMode != TextureWrapMode.Clamp, $"wrap {importador.wrapMode} (debe ser Clamp).");
            return problemas;
        }

        // --- Clips ---

        /// <summary>
        /// Un clip de sprites: existe, dura lo que suma su tabla (margen de 1 ms), se repite o no
        /// como dice, y a cada instante de <paramref name="fotogramas"/> se ve el fotograma indicado.
        /// </summary>
        public static List<string> ProblemasDeClip(string ruta, AnimationClip clip, float duracion, bool bucle,
            IEnumerable<(float inicio, string fotograma)> fotogramas)
        {
            var nombre = Path.GetFileNameWithoutExtension(ruta);
            var problemas = new List<string>();
            if (clip == null)
            {
                problemas.Add($"Falta el clip {nombre} ({ruta}).");
                return problemas;
            }

            if (Mathf.Abs(clip.length - duracion) > MargenTiempo)
            {
                problemas.Add($"Clip {nombre}: dura {Numero(clip.length)} s (la tabla suma {Numero(duracion)} s).");
            }

            if (clip.isLooping != bucle)
            {
                problemas.Add($"Clip {nombre}: {(bucle ? "debe repetirse en bucle" : "no debe repetirse en bucle")}.");
            }

            var enlace = AnimationUtility.GetObjectReferenceCurveBindings(clip).FirstOrDefault(b => b.type == typeof(SpriteRenderer) && b.propertyName == "m_Sprite");
            var claves = enlace.type != null ? AnimationUtility.GetObjectReferenceCurve(clip, enlace) : new ObjectReferenceKeyframe[0];
            foreach (var (inicio, fotograma) in fotogramas)
            {
                var clave = claves.FirstOrDefault(k => Mathf.Abs(k.time - inicio) <= MargenTiempo);
                var visto = clave.value != null ? clave.value.name : "nada";
                if (visto != fotograma)
                {
                    problemas.Add($"Clip {nombre}: a los {Numero(inicio)} s se ve {visto} (debe ser {fotograma}).");
                }
            }

            return problemas;
        }

        // --- Animator ---

        /// <summary>
        /// Un controlador: un estado por cada uno de <paramref name="esperados"/> con su clip,
        /// ninguno que no esté en la tabla, ninguno suelto (al que no lleve nada) y ninguna
        /// transición rota: sin destino, sin condición ni tiempo de salida, o con un parámetro que
        /// no existe o de otro tipo. <paramref name="quien"/> encabeza los mensajes
        /// (<c>"Animator"</c>, <c>"Animator Torreta"</c>).
        /// </summary>
        public static List<string> ProblemasDelControlador(AnimatorController controlador, string ruta, string quien,
            IReadOnlyList<(string estado, string clip)> esperados)
        {
            var problemas = new List<string>();
            if (controlador == null || controlador.layers.Length == 0)
            {
                problemas.Add(controlador == null ? $"Falta el {quien} ({ruta})." : $"El {quien} no tiene capas.");
                return problemas;
            }

            var maquina = controlador.layers[0].stateMachine;
            var estados = maquina.states.Select(s => s.state).ToList();
            foreach (var (nombre, clip) in esperados)
            {
                var estado = estados.FirstOrDefault(s => s.name == nombre);
                if (estado == null)
                {
                    problemas.Add($"{quien}: falta el estado {nombre}.");
                }
                else if (estado.motion == null || estado.motion.name != clip)
                {
                    problemas.Add($"{quien}: el estado {nombre} reproduce {(estado.motion != null ? estado.motion.name : "nada")} (debe ser {clip}).");
                }
            }

            foreach (var sobrante in estados.Where(s => esperados.All(e => e.estado != s.name)))
            {
                problemas.Add($"{quien}: el estado {sobrante.name} no está en la tabla.");
            }

            if (maquina.defaultState == null || !estados.Contains(maquina.defaultState))
            {
                problemas.Add($"{quien}: no tiene estado inicial.");
            }

            var parametros = controlador.parameters.ToDictionary(p => p.name, p => p.type);
            var transiciones = Transiciones(maquina).ToList();
            foreach (var (desde, t) in transiciones)
            {
                if (t.destinationState == null || !estados.Contains(t.destinationState))
                {
                    problemas.Add($"{quien}: la transición «{t.name}» desde {desde} no lleva a ningún estado del controlador.");
                    continue;
                }

                var nombre = $"«{t.name}» ({desde} → {t.destinationState.name})";
                if (!t.hasExitTime && t.conditions.Length == 0)
                {
                    problemas.Add($"{quien}: la transición {nombre} no tiene condición ni tiempo de salida: se tomaría en cada fotograma.");
                }

                foreach (var condicion in t.conditions)
                {
                    if (!parametros.TryGetValue(condicion.parameter, out var tipo))
                    {
                        problemas.Add($"{quien}: la transición {nombre} usa el parámetro {condicion.parameter}, que no existe.");
                    }
                    else if (!Admite(condicion.mode, tipo))
                    {
                        problemas.Add($"{quien}: la transición {nombre} compara {condicion.parameter} con {condicion.mode}, pero es {tipo}: nunca se cumpliría.");
                    }
                }
            }

            foreach (var suelto in estados.Where(s => s != maquina.defaultState && transiciones.All(x => x.transicion.destinationState != s)))
            {
                problemas.Add($"{quien}: nada lleva al estado {suelto.name} (está suelto).");
            }

            return problemas;
        }

        /// <summary>Las transiciones de la primera capa con el nombre de su origen («Any State» para las de cualquier estado).</summary>
        public static IEnumerable<(string desde, AnimatorStateTransition transicion)> Transiciones(AnimatorStateMachine maquina) =>
            maquina.states.SelectMany(s => s.state.transitions.Select(t => (s.state.name, t)))
                .Concat(maquina.anyStateTransitions.Select(t => ("Any State", t)));

        // Qué condiciones tienen sentido con cada tipo de parámetro.
        private static bool Admite(AnimatorConditionMode modo, AnimatorControllerParameterType tipo)
        {
            switch (modo)
            {
                case AnimatorConditionMode.If:
                case AnimatorConditionMode.IfNot:
                    return tipo == AnimatorControllerParameterType.Bool || tipo == AnimatorControllerParameterType.Trigger;
                case AnimatorConditionMode.Greater:
                case AnimatorConditionMode.Less:
                    return tipo == AnimatorControllerParameterType.Float || tipo == AnimatorControllerParameterType.Int;
                default:
                    return tipo == AnimatorControllerParameterType.Int;
            }
        }

        // --- Eventos ---

        /// <summary>
        /// Cada evento de <paramref name="clip"/> llama a una función pública y sin valor de retorno
        /// de alguno de <paramref name="receptores"/> (los scripts del objeto del Animator), sin
        /// parámetros o con uno que un evento pueda pasar.
        /// </summary>
        public static List<string> ProblemasDeEventos(AnimationClip clip, IReadOnlyCollection<Type> receptores)
        {
            var problemas = new List<string>();
            var quien = receptores.Count > 0
                ? "ninguna función de " + string.Join(" ni de ", receptores.Select(r => r.Name))
                : "nada: el objeto del Animator no tiene scripts que reciban eventos";
            foreach (var evento in AnimationUtility.GetAnimationEvents(clip))
            {
                if (!receptores.Any(r => Recibe(r, evento.functionName)))
                {
                    problemas.Add($"Clip {clip.name}: el evento «{evento.functionName}» (a los {Numero(evento.time)} s) no llama a {quien}.");
                }
            }

            return problemas;
        }

        private static bool Recibe(Type receptor, string funcion)
        {
            var metodo = receptor.GetMethod(funcion);
            if (metodo == null || !metodo.IsPublic || metodo.ReturnType != typeof(void))
            {
                return false;
            }

            var parametros = metodo.GetParameters();
            if (parametros.Length == 0)
            {
                return true;
            }

            var tipo = parametros[0].ParameterType;
            return parametros.Length == 1 && (tipo == typeof(float) || tipo == typeof(int) || tipo == typeof(string) ||
                                              tipo == typeof(AnimationEvent) || typeof(Object).IsAssignableFrom(tipo));
        }

        /// <summary>Los scripts del objeto de <paramref name="animator"/>: los que pueden recibir sus eventos.</summary>
        public static Type[] ReceptoresDe(Animator animator) =>
            animator.GetComponents<MonoBehaviour>().Where(m => m != null).Select(m => m.GetType()).Distinct().ToArray();

        // --- Prefabs ---

        /// <summary>
        /// Lo que hace daño en <paramref name="prefab"/> (una <see cref="Hitbox"/> o la capa
        /// EnemyWeapon) está apagado en reposo, salvo lo que <paramref name="siempreActivo"/> diga
        /// que daña siempre.
        /// </summary>
        public static List<string> ProblemasDeDanioEnReposo(GameObject prefab, Func<Collider2D, bool> siempreActivo = null)
        {
            var problemas = new List<string>();
            foreach (var colision in CollidersDeDanio(prefab))
            {
                if (colision.enabled && Encendido(colision.transform) && (siempreActivo == null || !siempreActivo(colision)))
                {
                    problemas.Add($"Prefab {prefab.name}: el collider de daño «{colision.name}» está encendido en reposo.");
                }
            }

            return problemas;
        }

        /// <summary>Los colliders que hacen daño: los de una <see cref="Hitbox"/> o en la capa EnemyWeapon.</summary>
        public static IEnumerable<Collider2D> CollidersDeDanio(GameObject prefab)
        {
            var capaArmas = LayerMask.NameToLayer(Capas.EnemyWeapon);
            return prefab.GetComponentsInChildren<Collider2D>(true)
                .Where(c => c.GetComponent<Hitbox>() != null || c.gameObject.layer == capaArmas);
        }

        /// <summary>
        /// ¿Está activo el objeto con todos sus padres? En un prefab sin instanciar
        /// <c>activeInHierarchy</c> siempre es false: se sube a mano.
        /// </summary>
        public static bool Encendido(Transform objeto)
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

        public static string Numero(float valor) => valor.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
