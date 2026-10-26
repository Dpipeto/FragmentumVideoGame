using UnityEngine;

namespace Fragmentum.Levels.Escenario
{
    /// <summary>
    /// Oscurece el fondo con la luz del nivel. Las capas del parallax son materiales sin luz (los de
    /// los sprites ignoran el desplazamiento de la textura, ver <see cref="ParallaxMovement"/>): en un
    /// nivel con <see cref="LuzDelNivel"/>, sin esto, el bosque del fondo se vería con luz de día en
    /// el bosque cerrado y en la arena a oscuras.
    ///
    /// Cada fotograma tiñe todas sus capas (y sus hitos) con el color de la luz global por su
    /// intensidad, hasta 1: con la luna de la linde se ve a medias; en el bosque cerrado y en la
    /// arena, casi nada; cuando la oscuridad retrocede, entero. En un nivel sin oscuridad no hace nada.
    /// </summary>
    [DisallowMultipleComponent]
    public class FondoConLaLuz : MonoBehaviour
    {
        private static readonly int ColorBase = Shader.PropertyToID("_BaseColor");

        private LuzDelNivel _luz;
        private MeshRenderer[] _capas;
        private SpriteRenderer[] _hitos;
        private MaterialPropertyBlock _bloque;
        private bool _buscada;

        /// <summary>El tinte de ahora: blanco si el nivel no tiene oscuridad.</summary>
        public Color Tinte { get; private set; } = Color.white;

        /// <summary>El tinte del fondo con esa luz global: su color por su intensidad, hasta 1, y opaco.</summary>
        public static Color TinteCon(Color color, float intensidad)
        {
            var f = Mathf.Clamp01(intensidad);
            return new Color(color.r * f, color.g * f, color.b * f, 1f);
        }

        private void Awake()
        {
            _capas = GetComponentsInChildren<MeshRenderer>(true);
            _hitos = GetComponentsInChildren<SpriteRenderer>(true);
            _bloque = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (!_buscada || _luz == null)
            {
                _luz = FindAnyObjectByType<LuzDelNivel>();
                _buscada = true;
            }

            var global = _luz != null && _luz.isActiveAndEnabled ? _luz.LuzGlobal : null;
            Tinte = global != null && global.isActiveAndEnabled ? TinteCon(global.color, global.intensity) : Color.white;

            foreach (var capa in _capas)
            {
                if (capa == null)
                {
                    continue;
                }

                // Sin pisar el desplazamiento que pone el parallax en el mismo bloque.
                capa.GetPropertyBlock(_bloque);
                _bloque.SetColor(ColorBase, Tinte);
                capa.SetPropertyBlock(_bloque);
            }

            foreach (var hito in _hitos)
            {
                if (hito != null)
                {
                    hito.color = Tinte;
                }
            }
        }
    }
}
