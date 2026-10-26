using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.UI
{
    /// <summary>
    /// La capa de brasas de las cinemáticas en pixel art: puntos de un píxel del plano que caen
    /// despacio y parpadean. Viven en la rejilla de la cámara de cinemáticas
    /// (<see cref="CamaraCinematica"/>): cada brasa ocupa un píxel entero y baja de píxel en píxel,
    /// nunca a medias. Su opacidad es la del plano, así que suben desde negro y cortan con él.
    ///
    /// No es un dibujo: son píxeles sueltos con los colores de <see cref="colores"/> (las vetas
    /// rojas y el calor de la sala), ajustables en el prefab como el resto de sus valores.
    ///
    /// Dónde está cada brasa depende solo del tiempo desde que se encendió la capa y de la
    /// <see cref="semilla"/>: la misma cinemática se ve igual cada vez, y se puede capturar o
    /// comprobar en cualquier instante (<see cref="Simular"/>).
    /// </summary>
    public class CapaBrasas : MonoBehaviour
    {
        [Min(0)] public int cantidad = 16;

        [Tooltip("Píxeles del plano por segundo que cae una brasa: la más lenta y la más rápida.")]
        public Vector2 velocidad = new Vector2(3f, 8f);

        [Tooltip("Píxeles por segundo de deriva lateral, como mucho, a un lado o a otro.")]
        [Min(0f)] public float deriva = 1.5f;

        [Tooltip("Fila del plano (desde arriba) donde se apagan: el suelo de la sala. Vuelven a caer desde arriba.")]
        [Min(1)] public int filaSuelo = 110;

        [Tooltip("Segundos entre cambios de brillo.")]
        [Min(0.02f)] public float parpadeo = 0.15f;

        [Range(0f, 1f)] public float brilloMinimo = 0.45f;

        public Color[] colores =
        {
            new Color32(232, 106, 42, 255),
            new Color32(184, 48, 32, 255),
            new Color32(242, 165, 65, 255),
        };

        public int semilla = 7;

        private struct Brasa
        {
            public float X;
            public float Y;
            public float Velocidad;
            public float Deriva;
            public int Color;
        }

        private Brasa[] _brasas = new Brasa[0];
        private readonly List<SpriteRenderer> _dibujos = new List<SpriteRenderer>();
        private float _alfa;

        public bool Activa { get; private set; }

        /// <summary>Segundos desde que se encendió.</summary>
        public float Tiempo { get; private set; }

        /// <summary>Los píxeles de las brasas (para las pruebas).</summary>
        public IReadOnlyList<SpriteRenderer> Dibujos => _dibujos;

        /// <summary>
        /// Enciende o apaga la capa, con la opacidad del plano. Al encenderse, el tiempo vuelve a 0:
        /// las brasas empiezan siempre igual.
        /// </summary>
        public void Fijar(bool activa, float alfa)
        {
            if (activa && !Activa)
            {
                Tiempo = 0f;
            }

            Activa = activa;
            _alfa = Mathf.Clamp01(alfa);
            Colocar();
        }

        /// <summary>
        /// Pone las brasas donde están <paramref name="segundos"/> después de encenderse, con la
        /// opacidad <paramref name="alfa"/> (la del plano).
        /// </summary>
        public void Simular(float segundos, float alfa = 1f)
        {
            Activa = true;
            _alfa = Mathf.Clamp01(alfa);
            Tiempo = segundos;
            Colocar();
        }

        private void Update()
        {
            if (!Activa)
            {
                return;
            }

            // La cinemática para el juego: cuenta el tiempo sin escala.
            Tiempo += Time.unscaledDeltaTime;
            Colocar();
        }

        /// <summary>El píxel del plano (columna, fila desde arriba) de la brasa <paramref name="i"/> en el instante <paramref name="t"/>.</summary>
        public Vector2Int PixelDe(int i, float t)
        {
            Preparar();
            var brasa = _brasas[i];
            var fila = Mathf.FloorToInt(Mathf.Repeat(brasa.Y + brasa.Velocidad * t, filaSuelo));
            var columna = Mathf.FloorToInt(Mathf.Repeat(brasa.X + brasa.Deriva * t, CamaraCinematica.AnchoReferencia));
            return new Vector2Int(columna, fila);
        }

        /// <summary>Brillo de la brasa <paramref name="i"/> en el instante <paramref name="t"/>: cambia a saltos, cada <see cref="parpadeo"/> s.</summary>
        public float BrilloDe(int i, float t)
        {
            var paso = Mathf.FloorToInt(t / parpadeo);
            var azar = new System.Random(semilla * 7919 + i * 104729 + paso);
            return Mathf.Lerp(brilloMinimo, 1f, (float)azar.NextDouble());
        }

        private void Colocar()
        {
            Preparar();
            for (var i = 0; i < _dibujos.Count; i++)
            {
                var dibujo = _dibujos[i];
                dibujo.enabled = Activa && _alfa > 0f;
                if (!dibujo.enabled)
                {
                    continue;
                }

                // El centro del píxel: el sprite mide 1 × 1 y su pivote está en el centro.
                var pixel = PixelDe(i, Tiempo);
                dibujo.transform.localPosition = TextoEnRejilla.EnElMundo(pixel.x, pixel.y) + new Vector3(0.5f, -0.5f, 0f);
                var color = colores.Length > 0 ? colores[_brasas[i].Color % colores.Length] : Color.white;
                color.a = BrilloDe(i, Tiempo) * _alfa;
                dibujo.color = color;
            }
        }

        // Las brasas y sus píxeles, si cambió la cantidad o la semilla.
        private void Preparar()
        {
            if (_brasas.Length != cantidad)
            {
                var azar = new System.Random(semilla);
                _brasas = new Brasa[cantidad];
                for (var i = 0; i < cantidad; i++)
                {
                    _brasas[i] = new Brasa
                    {
                        X = (float)azar.NextDouble() * CamaraCinematica.AnchoReferencia,
                        Y = (float)azar.NextDouble() * filaSuelo,
                        Velocidad = Mathf.Lerp(velocidad.x, velocidad.y, (float)azar.NextDouble()),
                        Deriva = Mathf.Lerp(-deriva, deriva, (float)azar.NextDouble()),
                        Color = azar.Next(0, 1 << 16),
                    };
                }
            }

            var camara = GetComponentInParent<CamaraCinematica>(true);
            var material = camara != null && camara.Plano != null ? camara.Plano.sharedMaterial : null;
            while (_dibujos.Count < cantidad)
            {
                var objeto = new GameObject($"Brasa{_dibujos.Count + 1}") { layer = gameObject.layer };
                objeto.transform.SetParent(transform, false);
                var dibujo = objeto.AddComponent<SpriteRenderer>();
                dibujo.sprite = CamaraCinematica.Blanco;
                dibujo.sortingOrder = CamaraCinematica.OrdenBrasas;
                if (material != null)
                {
                    dibujo.sharedMaterial = material;
                }

                dibujo.enabled = false;
                _dibujos.Add(dibujo);
            }

            for (var i = cantidad; i < _dibujos.Count; i++)
            {
                _dibujos[i].enabled = false;
            }
        }
    }
}
