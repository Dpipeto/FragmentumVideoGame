using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Bosses;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Fragmentum.EditorTools.ComprobacionesArte;

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
    /// cuadra. Cada comprobación recibe lo que revisa, para probarla con objetos rotos a propósito;
    /// las que comparte con el Nivel 1 están en <see cref="ComprobacionesArte"/>.
    /// </summary>
    public static class VerificadorCentinela
    {
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
        public static List<string> ProblemasDeSprite(string archivo, TextureImporter importador) =>
            ComprobacionesArte.ProblemasDeSprite(archivo, ConstructorCentinela.RutaSprite(archivo), importador,
                ConstructorCentinela.PixelesPorUnidadDe(archivo), SpriteAlignment.Custom, ConstructorCentinela.Pivote,
                new Vector2Int(ConstructorCentinela.AnchoLienzo, ConstructorCentinela.AltoLienzo), ", en los pies");

        // --- 2. Clips ---

        /// <summary>Un clip: existe, con los fotogramas de la tabla y lo que suman sus duraciones.</summary>
        public static List<string> ProblemasDeClip(ConstructorCentinela.Clip definicion, AnimationClip clip) =>
            ComprobacionesArte.ProblemasDeClip(definicion.Ruta, clip, definicion.Duracion, definicion.Bucle,
                definicion.Fotogramas.Select((fotograma, i) => (definicion.Inicio(i), fotograma)));

        // --- 3. Animator ---

        /// <summary>El controlador: los 8 estados con su clip, ninguno suelto y ninguna transición rota.</summary>
        public static List<string> ProblemasDelControlador(AnimatorController controlador) =>
            ComprobacionesArte.ProblemasDelControlador(controlador, ConstructorCentinela.RutaControlador, "Animator",
                ConstructorCentinela.Clips.Select(c => (c.Estado, Path.GetFileNameWithoutExtension(c.Ruta))).ToList());

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

            problemas.AddRange(ProblemasDeDanioEnReposo(prefab));

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

        // --- 5. Eventos ---

        /// <summary>Cada evento del clip llama a una función pública de <see cref="AnimacionJefe"/> que acepta su parámetro.</summary>
        public static List<string> ProblemasDeEventos(AnimationClip clip) =>
            ComprobacionesArte.ProblemasDeEventos(clip, new[] { typeof(AnimacionJefe) });
    }
}
