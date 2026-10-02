using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fragmentum.Levels.Escenario
{
    /// <summary>
    /// Dónde va cada hito del fondo (la fortaleza, las cataratas) y cada tramo de muro, como
    /// fracción de la longitud del nivel. Así el mismo prefab sirve para niveles de cualquier
    /// longitud: <see cref="Repartir"/> convierte las fracciones en posiciones y deja las hijas
    /// de cada <see cref="ParallaxProps"/> en su sitio.
    ///
    /// Solo trabaja en el editor (menú <i>Fragmentum → Niveles → Repartir los hitos del fondo</i>, y
    /// al preparar una escena de nivel). Durante el juego no hace nada: cada capa ya sabe dónde
    /// están sus hitos.
    ///
    /// Un hito se ve durante mucho recorrido: la fortaleza, más ancha que la pantalla, tarda unas
    /// 87 u de cámara en cruzarla. Si dos hitos de la misma capa se ven a la vez pierden el efecto:
    /// <see cref="Solapes"/> lo comprueba.
    /// </summary>
    public class RepartoHitos : MonoBehaviour
    {
        /// <summary>Proporción de pantalla con la que se calculan tramos y solapes.</summary>
        public const float AspectoReferencia = 16f / 9f;

        /// <summary>Hitos sueltos de una capa: uno por posición.</summary>
        [Serializable]
        public sealed class GrupoHitos
        {
            public ParallaxProps capa;

            [Tooltip("Dónde se ve centrado cada hito, como fracción del nivel (0 = inicio de la primera sección, 1 = final de la última). " +
                     "Hay tantos hitos como posiciones: si faltan, se copian del primero; si sobran, se desactivan.")]
            public float[] posiciones = new float[0];
        }

        /// <summary>Tramos de una capa (sprites en modo Tiled): uno por tramo.</summary>
        [Serializable]
        public sealed class GrupoTramos
        {
            public ParallaxProps capa;

            [Tooltip("Tramos del nivel (fracciones desde, hasta) en los que la capa tapa la pantalla entera. " +
                     "Fuera de ellos entra y sale poco a poco.")]
            public Vector2[] tramos = new Vector2[0];
        }

        [SerializeField] private GrupoHitos[] hitos = new GrupoHitos[0];
        [SerializeField] private GrupoTramos[] tramos = new GrupoTramos[0];

        public IReadOnlyList<GrupoHitos> Hitos => hitos;

        public IReadOnlyList<GrupoTramos> Tramos => tramos;

        /// <summary>
        /// Coloca los hitos y los tramos para un nivel que va de <c>extension.x</c> a
        /// <c>extension.y</c>, visto con una cámara de <paramref name="altoVista"/> unidades de alto
        /// (2 × tamaño ortográfico).
        /// </summary>
        public void Repartir(Vector2 extension, float altoVista)
        {
            var largo = extension.y - extension.x;
            foreach (var grupo in hitos)
            {
                if (grupo?.capa == null)
                {
                    continue;
                }

                var hijas = Asegurar(grupo.capa.transform, grupo.posiciones.Length);
                for (var i = 0; i < hijas.Count; i++)
                {
                    MoverX(hijas[i], extension.x + grupo.posiciones[i] * largo);
                }

                grupo.capa.Anclar();
            }

            var anchoVista = altoVista * AspectoReferencia;
            foreach (var grupo in tramos)
            {
                if (grupo?.capa == null)
                {
                    continue;
                }

                var escala = Escala(grupo.capa, altoVista);
                var hijas = Asegurar(grupo.capa.transform, grupo.tramos.Length);
                for (var i = 0; i < hijas.Count; i++)
                {
                    var desde = extension.x + grupo.tramos[i].x * largo;
                    var hasta = extension.x + grupo.tramos[i].y * largo;
                    MoverX(hijas[i], (desde + hasta) * 0.5f);

                    var sprite = hijas[i].GetComponent<SpriteRenderer>();
                    if (sprite != null && sprite.drawMode != SpriteDrawMode.Simple)
                    {
                        var ancho = CalculoParallax.AnchoParaCubrir(desde, hasta, grupo.capa.velocidad, anchoVista);
                        sprite.size = new Vector2(ancho / (escala * hijas[i].lossyScale.x), sprite.size.y);
                    }
                }

                grupo.capa.Anclar();
            }
        }

        /// <summary>
        /// Parejas de hitos de una misma capa que se ven a la vez en algún punto del nivel. Vacía si
        /// el reparto está bien.
        /// </summary>
        public List<string> Solapes(Vector2 extension, float altoVista)
        {
            var solapes = new List<string>();
            var anchoVista = altoVista * AspectoReferencia;
            foreach (var grupo in hitos)
            {
                if (grupo?.capa == null)
                {
                    continue;
                }

                var escala = Escala(grupo.capa, altoVista);
                var visibles = new List<(string nombre, Vector2 recorrido)>();
                foreach (Transform hija in grupo.capa.transform)
                {
                    if (hija.gameObject.activeSelf)
                    {
                        var ancho = AnchoLocal(hija) * escala * grupo.capa.transform.lossyScale.x;
                        visibles.Add((hija.name, CalculoParallax.RecorridoVisible(hija.position.x, ancho, grupo.capa.velocidad, anchoVista)));
                    }
                }

                for (var i = 0; i < visibles.Count; i++)
                {
                    for (var j = i + 1; j < visibles.Count; j++)
                    {
                        var desde = Mathf.Max(visibles[i].recorrido.x, visibles[j].recorrido.x, extension.x);
                        var hasta = Mathf.Min(visibles[i].recorrido.y, visibles[j].recorrido.y, extension.y);
                        if (desde < hasta)
                        {
                            solapes.Add($"{visibles[i].nombre} y {visibles[j].nombre} se ven a la vez con la cámara entre x = {desde:0.#} y x = {hasta:0.#}.");
                        }
                    }
                }
            }

            return solapes;
        }

        // La capa se escala para que su alto de referencia ocupe la vista (ver ParallaxProps).
        private static float Escala(ParallaxProps capa, float altoVista) =>
            capa.altoReferencia > 0f ? altoVista / capa.altoReferencia : 1f;

        private static float AnchoLocal(Transform hija)
        {
            var sprite = hija.GetComponent<SpriteRenderer>();
            if (sprite == null || sprite.sprite == null)
            {
                return 0f;
            }

            var ancho = sprite.drawMode == SpriteDrawMode.Simple ? sprite.sprite.bounds.size.x : sprite.size.x;
            return ancho * hija.localScale.x;
        }

        private static void MoverX(Transform hija, float x)
        {
            var posicion = hija.position;
            hija.position = new Vector3(x, posicion.y, posicion.z);
        }

        /// <summary>
        /// Deja <paramref name="cuantas"/> hijas activas: copia la primera si faltan y desactiva las
        /// que sobran (las de un prefab no se pueden borrar desde una escena).
        /// </summary>
        private static List<Transform> Asegurar(Transform grupo, int cuantas)
        {
            var hijas = new List<Transform>();
            foreach (Transform hija in grupo)
            {
                hijas.Add(hija);
            }

            if (hijas.Count == 0)
            {
                if (cuantas > 0)
                {
                    Debug.LogWarning($"[Parallax] '{grupo.name}' no tiene ninguna hija que copiar: no se reparten sus hitos.", grupo);
                }

                return hijas;
            }

            var nombre = NombreBase(hijas[0].name);
            while (hijas.Count < cuantas)
            {
                var copia = Object.Instantiate(hijas[0].gameObject, grupo);
                copia.name = $"{nombre}_{hijas.Count + 1}";
                hijas.Add(copia.transform);
            }

            for (var i = 0; i < hijas.Count; i++)
            {
                hijas[i].gameObject.SetActive(i < cuantas);
            }

            return hijas.GetRange(0, cuantas);
        }

        private static string NombreBase(string nombre)
        {
            var guion = nombre.LastIndexOf('_');
            return guion > 0 && int.TryParse(nombre.Substring(guion + 1), out _) ? nombre.Substring(0, guion) : nombre;
        }
    }
}
