using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Clips de sprites construidos desde una tabla: cada clip es una lista de fotogramas (por
    /// nombre de archivo) con lo que dura cada uno. A mano son muchos keyframes con tiempos
    /// irregulares y es fácil equivocarse; en una tabla se revisan de un vistazo.
    ///
    /// Lo usan el Golem de Ceniza (<see cref="ConstructorGolem"/>) y las mecánicas
    /// (<see cref="ConstructorMecanicas"/>). Mismo criterio que los clips de la Centinela:
    /// - una clave por fotograma y otra de cierre con el último, para que el clip dure
    ///   exactamente lo de la tabla;
    /// - un fotograma que dura <see cref="SeQueda"/> se queda en pantalla al acabar el clip (sin
    ///   bucle, el clip se detiene en él): así son los estados que sostiene el script.
    /// </summary>
    public static class ClipsDeSprites
    {
        /// <summary>Muestras por segundo de los clips.</summary>
        public const float FotogramasPorSegundo = 60f;

        /// <summary>
        /// Duración de un fotograma que se queda en pantalla al acabar el clip: se muestra un
        /// fotograma y el clip, sin bucle, se detiene en él.
        /// </summary>
        public const float SeQueda = 1f / FotogramasPorSegundo;

        /// <summary>Un clip: el estado del Animator que lo usa, sus fotogramas y cuánto dura cada uno.</summary>
        public sealed class Clip
        {
            /// <summary>Nombre del asset (<c>Golem_Ataque</c>).</summary>
            public string Nombre;

            /// <summary>Estado del Animator que lo reproduce (<c>Ataque</c>).</summary>
            public string Estado;

            public string[] Fotogramas;
            public float[] Duraciones;
            public bool Bucle;

            public float Duracion => Duraciones.Sum();

            /// <summary>Instante en que empieza el fotograma i.</summary>
            public float Inicio(int i) => Duraciones.Take(i).Sum();

            /// <summary>¿El último fotograma se queda en pantalla?</summary>
            public bool SeQuedaAlFinal => !Bucle && Duraciones[Duraciones.Length - 1] <= SeQueda + 1e-5f;

            public string Ruta(string carpeta) => $"{carpeta}/{Nombre}.anim";
        }

        /// <summary>
        /// Crea o actualiza el clip <paramref name="definicion"/> en <paramref name="carpeta"/>. Si
        /// ya existe, se rehace sobre el mismo asset: conserva su identificador y lo que lo use no
        /// se rompe. <paramref name="eventos"/> son los eventos de animación del clip (o ninguno).
        /// </summary>
        public static AnimationClip Crear(string carpeta, Clip definicion, IReadOnlyDictionary<string, Sprite> sprites,
            IEnumerable<AnimationEvent> eventos = null)
        {
            var ruta = definicion.Ruta(carpeta);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
            var nuevo = clip == null;
            if (nuevo)
            {
                clip = new AnimationClip();
            }

            clip.frameRate = FotogramasPorSegundo;

            // Una clave por fotograma y una más al final con el último, que fija cuánto dura.
            // Unity alarga los clips de sprites un fotograma (1/60 s) tras la última clave: la de
            // cierre va ese fotograma antes para que el clip dure exactamente lo indicado. Si el
            // último fotograma solo dura ese fotograma (se queda), sobra la de cierre.
            var n = definicion.Fotogramas.Length;
            var claves = new List<ObjectReferenceKeyframe>();
            for (var i = 0; i < n; i++)
            {
                claves.Add(new ObjectReferenceKeyframe { time = definicion.Inicio(i), value = sprites[definicion.Fotogramas[i]] });
            }

            if (definicion.Duraciones[n - 1] > SeQueda + 1e-5f)
            {
                claves.Add(new ObjectReferenceKeyframe { time = definicion.Duracion - SeQueda, value = sprites[definicion.Fotogramas[n - 1]] });
            }

            AnimationUtility.SetObjectReferenceCurve(clip, BindingSprite, claves.ToArray());
            AnimationUtility.SetAnimationEvents(clip, (eventos ?? Enumerable.Empty<AnimationEvent>()).ToArray());

            var ajustes = AnimationUtility.GetAnimationClipSettings(clip);
            ajustes.loopTime = definicion.Bucle;
            AnimationUtility.SetAnimationClipSettings(clip, ajustes);

            if (nuevo)
            {
                AssetDatabase.CreateAsset(clip, ruta);
            }
            else
            {
                EditorUtility.SetDirty(clip);
            }

            return clip;
        }

        /// <summary>La curva que cambia el sprite del <see cref="SpriteRenderer"/>.</summary>
        public static EditorCurveBinding BindingSprite => EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");

        /// <summary>
        /// Carga los sprites que usan <paramref name="clips"/>, con los ajustes de
        /// <see cref="TablaArte"/> ya aplicados. Si falta alguno avisa y deja
        /// <paramref name="sprites"/> vacío: no es un fallo, el arte puede no haber llegado.
        /// Devuelve false solo si uno no se pudo importar como sprite.
        /// </summary>
        public static bool CargarSprites(IEnumerable<Clip> clips, Func<string, string> ruta, string quien, out Dictionary<string, Sprite> sprites)
        {
            sprites = null;
            var archivos = clips.SelectMany(c => c.Fotogramas).Distinct().ToList();
            var faltan = archivos.Where(a => AssetDatabase.LoadAssetAtPath<Texture2D>(ruta(a)) == null).ToList();
            if (faltan.Count > 0)
            {
                Debug.LogWarning($"[Fragmentum] Faltan fotogramas de {quien}: {string.Join(", ", faltan.Select(ruta))}. No se crean sus clips.");
                return true;
            }

            var cargados = new Dictionary<string, Sprite>();
            foreach (var archivo in archivos)
            {
                if (AssetImporter.GetAtPath(ruta(archivo)) is TextureImporter importador && TablaArte.Ajustar(importador))
                {
                    importador.SaveAndReimport();
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta(archivo));
                if (sprite == null)
                {
                    Debug.LogError($"[Fragmentum] {ruta(archivo)} no se importó como sprite.");
                    return false;
                }

                cargados[archivo] = sprite;
            }

            sprites = cargados;
            return true;
        }
    }
}
