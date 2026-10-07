using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.UI
{
    /// <summary>
    /// Un texto en la rejilla de la cámara de cinemáticas (<see cref="CamaraCinematica"/>). Monta su
    /// propia malla con los glifos de la fuente, cada uno en píxeles enteros del plano y con los
    /// píxeles de la fuente del mismo tamaño que los del plano: el texto se amplía con la imagen, por
    /// el mismo entero, y nunca queda a otra escala (si el texto fuera a 1,5× y la imagen a 4×, se
    /// notaría).
    ///
    /// Con una fuente bitmap, cada píxel de una letra es un píxel de la rejilla. Con otra fuente se
    /// ve, pero sus bordes suavizados no son pixel art.
    ///
    /// Las coordenadas son píxeles del plano contados desde arriba a la izquierda, como en el guion.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TextoEnRejilla : MonoBehaviour
    {
        private Mesh _malla;
        private MeshRenderer _dibujo;
        private Font _fuente;
        private Color _color = Color.white;
        private float _alfa = 1f;
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int> _triangulos = new List<int>();
        private readonly List<Color> _colores = new List<Color>();

        // Lo último pedido, para rehacer la malla si la fuente rehace su textura.
        private string _texto = string.Empty;
        private int _tamano;
        private int _interlineado;
        private Vector2Int _punto;
        private TextAnchor _alineacion;

        public string Texto => _texto;

        /// <summary>El bloque de texto en píxeles del plano (x desde la izquierda, y desde arriba).</summary>
        public RectInt Bloque { get; private set; }

        /// <summary>Los vértices de la malla, en unidades del mundo relativas al centro del plano (para las pruebas).</summary>
        public IReadOnlyList<Vector3> Vertices => _vertices;

        public bool Visible => gameObject.activeSelf && _alfa > 0f && !string.IsNullOrEmpty(_texto);

        private void Awake() => Preparar();

        private void OnEnable() => Font.textureRebuilt += AlRehacerTextura;

        private void OnDisable() => Font.textureRebuilt -= AlRehacerTextura;

        private void OnDestroy()
        {
            if (_malla == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_malla);
            }
            else
            {
                DestroyImmediate(_malla);
            }
        }

        /// <summary>
        /// Escribe <paramref name="texto"/> con <paramref name="fuente"/> a <paramref name="tamano"/>
        /// píxeles, anclado en <paramref name="punto"/> según <paramref name="alineacion"/>. Las
        /// líneas se separan con <paramref name="interlineado"/> píxeles (0 = el de la fuente).
        /// </summary>
        public void Fijar(string texto, Font fuente, int tamano, int interlineado, Vector2Int punto, TextAnchor alineacion, Color color)
        {
            Preparar();
            var igual = texto == _texto && fuente == _fuente && tamano == _tamano && interlineado == _interlineado &&
                        punto == _punto && alineacion == _alineacion && color == _color;
            _texto = texto ?? string.Empty;
            _fuente = fuente;
            _tamano = Mathf.Max(1, tamano);
            _interlineado = interlineado;
            _punto = punto;
            _alineacion = alineacion;
            _color = color;
            if (!igual)
            {
                Construir();
            }
        }

        /// <summary>Opacidad del texto (los fundidos de entrada y salida).</summary>
        public void FijarAlfa(float alfa)
        {
            if (Mathf.Approximately(alfa, _alfa) && _colores.Count == _vertices.Count)
            {
                return;
            }

            _alfa = alfa;
            Colorear();
        }

        // --- Malla ---

        private void Preparar()
        {
            if (_malla != null)
            {
                return;
            }

            _malla = new Mesh { name = "TextoEnRejilla" };
            _malla.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = _malla;
            _dibujo = GetComponent<MeshRenderer>();
        }

        private void AlRehacerTextura(Font fuente)
        {
            if (fuente == _fuente)
            {
                Construir();
            }
        }

        private void Construir()
        {
            _vertices.Clear();
            _uvs.Clear();
            _triangulos.Clear();
            Bloque = new RectInt(_punto.x, _punto.y, 0, 0);
            if (_fuente == null || string.IsNullOrEmpty(_texto))
            {
                Volcar();
                return;
            }

            // Una fuente dinámica (TTF) se rasteriza al tamaño pedido; una bitmap tiene el suyo.
            var tamano = _fuente.dynamic ? _tamano : 0;
            if (_fuente.dynamic)
            {
                _fuente.RequestCharactersInTexture(_texto, tamano, FontStyle.Normal);
            }

            var escala = _fuente.dynamic && _fuente.fontSize > 0 ? (float)_tamano / _fuente.fontSize : 1f;
            var alto = _interlineado > 0 ? _interlineado : Mathf.Max(1, Mathf.RoundToInt(_fuente.lineHeight * escala));
            var ascenso = Mathf.RoundToInt((_fuente.ascent > 0 ? _fuente.ascent : _fuente.lineHeight) * escala);

            var lineas = _texto.Replace("\r", string.Empty).Split('\n');
            var anchos = new int[lineas.Length];
            for (var i = 0; i < lineas.Length; i++)
            {
                foreach (var c in lineas[i])
                {
                    if (Glifo(c, tamano, out var info))
                    {
                        anchos[i] += info.advance;
                    }
                }
            }

            var ancho = Mathf.Max(0, Max(anchos));
            var altoBloque = alto * lineas.Length;
            var (fx, fy) = Factores(_alineacion);
            var izquierda = _punto.x - Mathf.FloorToInt(ancho * fx);
            var arriba = _punto.y - Mathf.FloorToInt(altoBloque * fy);
            Bloque = new RectInt(izquierda, arriba, ancho, altoBloque);

            var material = _fuente.material;
            if (_dibujo.sharedMaterial != material)
            {
                _dibujo.sharedMaterial = material;
            }

            for (var i = 0; i < lineas.Length; i++)
            {
                var x = izquierda + Mathf.FloorToInt((ancho - anchos[i]) * fx);
                var linea = arriba + alto * i + ascenso; // la línea base, en filas desde arriba
                foreach (var c in lineas[i])
                {
                    if (!Glifo(c, tamano, out var info))
                    {
                        continue;
                    }

                    if (info.maxX > info.minX && info.maxY > info.minY)
                    {
                        Cuadro(x + info.minX, linea - info.maxY, x + info.maxX, linea - info.minY, info);
                    }

                    x += info.advance;
                }
            }

            Volcar();
        }

        private bool Glifo(char c, int tamano, out CharacterInfo info) =>
            _fuente.dynamic ? _fuente.GetCharacterInfo(c, out info, tamano, FontStyle.Normal) : _fuente.GetCharacterInfo(c, out info);

        // Un glifo: de la columna x0 a la x1 y de la fila y0 a la y1 del plano (desde arriba).
        private void Cuadro(int x0, int y0, int x1, int y1, CharacterInfo info)
        {
            var primero = _vertices.Count;
            _vertices.Add(EnElMundo(x0, y1));
            _vertices.Add(EnElMundo(x0, y0));
            _vertices.Add(EnElMundo(x1, y0));
            _vertices.Add(EnElMundo(x1, y1));
            _uvs.Add(info.uvBottomLeft);
            _uvs.Add(info.uvTopLeft);
            _uvs.Add(info.uvTopRight);
            _uvs.Add(info.uvBottomRight);
            _triangulos.Add(primero);
            _triangulos.Add(primero + 1);
            _triangulos.Add(primero + 2);
            _triangulos.Add(primero);
            _triangulos.Add(primero + 2);
            _triangulos.Add(primero + 3);
        }

        /// <summary>
        /// Un borde de píxel del plano en unidades relativas a su centro: x entera, y en medios
        /// (el plano mide 135 de alto y su centro cae a mitad de un píxel). Coinciden con los bordes
        /// de los píxeles de la rejilla.
        /// </summary>
        public static Vector3 EnElMundo(int columna, int fila) =>
            new Vector3(columna - CamaraCinematica.AnchoReferencia * 0.5f, CamaraCinematica.AltoReferencia * 0.5f - fila, 0f);

        private void Volcar()
        {
            _malla.Clear();
            _malla.SetVertices(_vertices);
            _malla.SetUVs(0, _uvs);
            _malla.SetTriangles(_triangulos, 0);
            Colorear();
            _malla.RecalculateBounds();
        }

        private void Colorear()
        {
            _colores.Clear();
            var color = new Color(_color.r, _color.g, _color.b, _color.a * _alfa);
            for (var i = 0; i < _vertices.Count; i++)
            {
                _colores.Add(color);
            }

            if (_malla != null)
            {
                _malla.SetColors(_colores);
            }
        }

        // Qué fracción del bloque queda a la izquierda y por encima del punto de anclaje.
        private static (float x, float y) Factores(TextAnchor alineacion)
        {
            var x = (int)alineacion % 3 * 0.5f;
            var y = (int)alineacion / 3 * 0.5f;
            return (x, y);
        }

        private static int Max(int[] valores)
        {
            var max = 0;
            foreach (var v in valores)
            {
                max = Mathf.Max(max, v);
            }

            return max;
        }
    }
}
