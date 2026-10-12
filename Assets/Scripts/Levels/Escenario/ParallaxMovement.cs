using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fragmentum.Levels.Escenario
{
    /// <summary>
    /// Parallax de las capas repetibles (cielo, montañas, primer plano). Cada hija es un Quad con
    /// una textura que se repite en horizontal: lo que se mueve es la textura, no el Quad. La
    /// velocidad de cada capa sale de su Z, como en el prototipo:
    /// <c>velocidad = 1 - (z - zCámara) / distancia de la capa más lejana</c>
    /// (ver <see cref="CalculoParallax"/>).
    ///
    /// Corrige el <c>ParallaxMovement</c> del prototipo:
    /// - con URP el desplazamiento no hacía nada, porque los shaders 2D de sprites ignoran el
    ///   tiling y el offset. Las capas usan URP/Unlit, y el desplazamiento se escribe en la
    ///   textura principal del shader que sea (<c>_BaseMap</c> en URP, <c>_MainTex</c> en el
    ///   pipeline integrado);
    /// - el desplazamiento se divide por el ancho de cada textura en el mundo. Con un único factor
    ///   (<c>parallaxSpeed</c>), una textura el doble de ancha corría el doble de rápido y las
    ///   velocidades no seguían la profundidad;
    /// - sigue a la cámara también en vertical, porque el arte mide justo una pantalla de alto, y
    ///   cada capa se ajusta al tamaño de la vista: vale para cualquier cámara y proporción;
    /// - ya no fija la posición del objeto en (cámara − 1, y, 9.92), que dejaba un hueco en el
    ///   borde derecho y movía todas las capas en Z.
    ///
    /// No lo escales ni lo gires: las capas se dimensionan desde aquí. Se ejecuta después de
    /// <see cref="CameraMovement"/>; si fuera antes, las capas irían un fotograma por detrás de
    /// la cámara y asomaría el borde al subir o bajar.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ParallaxMovement : MonoBehaviour
    {
        /// <summary>Las capas son algo más anchas que la vista, por si el redondeo deja una columna sin cubrir.</summary>
        public const float MargenAncho = 1.02f;

        [Tooltip("Seguir a la cámara también en vertical: cada capa ocupa siempre la vista entera (el cielo, las montañas). " +
                 "Desactivado, las capas se quedan a la altura y con el alto con que se colocaron y solo se mueven en horizontal: " +
                 "un fondo que está en un sitio del mundo, como el de debajo del agua.")]
        [SerializeField] private bool seguirCamaraEnY = true;

        [Tooltip("Distancia a la cámara de la capa más lejana de todo el fondo, de la que salen las velocidades. " +
                 "0 = la de la más lejana de estas capas. Se fija cuando el fondo se reparte en varios grupos.")]
        [SerializeField, Min(0f)] private float lejania;

        private Camera _camara;
        private Renderer[] _capas = new Renderer[0];
        private float[] _velocidades = new float[0];
        private float[] _proporciones = new float[0];
        private float[] _periodos = new float[0];
        private int[] _propiedades = new int[0];
        private MaterialPropertyBlock _bloque;
        private bool _avisadoPerspectiva;

        /// <summary>Número de capas que mueve (las hijas con Renderer).</summary>
        public int Capas => _capas.Length;

        /// <summary>Velocidad de una capa, en el orden de las hijas (0 = viaja con la cámara).</summary>
        public float Velocidad(int capa) => _velocidades[capa];

        /// <summary>La capa (Renderer) de ese índice, en el orden de las hijas.</summary>
        public Renderer Capa(int capa) => _capas[capa];

        /// <summary>¿Las capas siguen a la cámara en vertical y ocupan la vista entera?</summary>
        public bool SigueCamaraEnY => seguirCamaraEnY;

        private void Start()
        {
            _camara = Camera.main;
            if (_camara == null)
            {
                Debug.LogError("[Parallax] No hay ninguna cámara con el tag MainCamera: el fondo no se moverá.", this);
                enabled = false;
                return;
            }

            Preparar(_camara);
        }

        private void LateUpdate()
        {
            if (_camara != null)
            {
                Aplicar(_camara);
            }
        }

        /// <summary>
        /// Lee las capas y calcula la velocidad de cada una desde su Z. Lo llama <c>Start</c>; las
        /// herramientas del editor lo llaman a mano para ver el fondo sin darle a Play.
        /// </summary>
        public void Preparar(Camera camara)
        {
            var capas = new List<Renderer>();
            for (var i = 0; i < transform.childCount; i++)
            {
                var hija = transform.GetChild(i);
                var capa = hija.GetComponent<Renderer>();
                if (capa != null)
                {
                    capas.Add(capa);
                }
                else
                {
                    Debug.LogWarning($"[Parallax] '{hija.name}' no tiene Renderer: no se mueve.", hija);
                }
            }

            _capas = capas.ToArray();
            _velocidades = new float[_capas.Length];
            _proporciones = new float[_capas.Length];
            _periodos = new float[_capas.Length];
            _propiedades = new int[_capas.Length];

            var zCamara = camara.transform.position.z;
            var masLejana = lejania;
            if (masLejana <= 0f)
            {
                foreach (var capa in _capas)
                {
                    masLejana = Mathf.Max(masLejana, capa.transform.position.z - zCamara);
                }
            }

            for (var i = 0; i < _capas.Length; i++)
            {
                var material = _capas[i].sharedMaterial;
                var textura = material != null ? material.mainTexture : null;
                _velocidades[i] = CalculoParallax.VelocidadPorProfundidad(_capas[i].transform.position.z, zCamara, masLejana);
                _proporciones[i] = textura != null && textura.height > 0 ? (float)textura.width / textura.height : 1f;
                // Una textura que se repite en espejo (sus bordes no casan) tarda dos anchos en volver a empezar.
                _periodos[i] = textura != null && textura.wrapModeU == TextureWrapMode.Mirror ? 2f : 1f;
                _propiedades[i] = Shader.PropertyToID(TexturaPrincipal(material) + "_ST");
                AvisarSiNoSeDesplaza(_capas[i], material);
            }
        }

        /// <summary>
        /// Coloca las capas sobre la cámara y desplaza sus texturas. Lo llama <c>LateUpdate</c>
        /// cada fotograma.
        /// </summary>
        public void Aplicar(Camera camara)
        {
            if (!camara.orthographic)
            {
                if (!_avisadoPerspectiva)
                {
                    _avisadoPerspectiva = true;
                    Debug.LogWarning("[Parallax] La cámara no es ortográfica: el fondo no se ajusta a la vista.", this);
                }

                return;
            }

            var posicion = camara.transform.position;
            transform.position = new Vector3(posicion.x, seguirCamaraEnY ? posicion.y : transform.position.y, transform.position.z);

            var alto = camara.orthographicSize * 2f;
            var ancho = alto * camara.aspect * MargenAncho;
            var escala = transform.lossyScale;
            _bloque ??= new MaterialPropertyBlock();

            for (var i = 0; i < _capas.Length; i++)
            {
                var capa = _capas[i];
                if (capa == null)
                {
                    continue;
                }

                // Siguiendo a la cámara, la capa ocupa la vista entera. Anclada, conserva su altura
                // y su alto en el mundo y solo se ensancha hasta cubrir la vista.
                var local = capa.transform.localPosition;
                var altoCapa = seguirCamaraEnY ? alto : capa.transform.localScale.y * escala.y;
                capa.transform.localPosition = new Vector3(0f, seguirCamaraEnY ? 0f : local.y, local.z);
                capa.transform.localScale = new Vector3(ancho / escala.x, altoCapa / escala.y, 1f);

                // La textura ocupa todo el alto de la capa: su ancho en el mundo sale de su proporción.
                var anchoTextura = altoCapa * _proporciones[i];
                var offset = CalculoParallax.OffsetTextura(posicion.x, _velocidades[i], anchoTextura, ancho, _periodos[i]);

                capa.GetPropertyBlock(_bloque);
                _bloque.SetVector(_propiedades[i], new Vector4(ancho / anchoTextura, 1f, offset, 0f));
                capa.SetPropertyBlock(_bloque);
            }
        }

        /// <summary>Nombre de la textura principal del shader ([MainTexture]), o _MainTex.</summary>
        public static string TexturaPrincipal(Material material)
        {
            var shader = material != null ? material.shader : null;
            if (shader == null)
            {
                return "_MainTex";
            }

            for (var i = 0; i < shader.GetPropertyCount(); i++)
            {
                if ((shader.GetPropertyFlags(i) & ShaderPropertyFlags.MainTexture) != 0)
                {
                    return shader.GetPropertyName(i);
                }
            }

            return material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
        }

        // Los shaders de sprites (los 2D de URP y Sprites/Default) dibujan la textura tal cual.
        private void AvisarSiNoSeDesplaza(Renderer capa, Material material)
        {
            var shader = material != null && material.shader != null ? material.shader.name : "ninguno";
            if (material == null || shader.StartsWith("Universal Render Pipeline/2D/") || shader.StartsWith("Sprites/"))
            {
                Debug.LogWarning(
                    $"[Parallax] La capa '{capa.name}' usa el shader {shader}, que ignora el desplazamiento de textura: " +
                    "no se moverá. Usa un material URP/Unlit transparente.", capa);
            }
        }
    }
}
