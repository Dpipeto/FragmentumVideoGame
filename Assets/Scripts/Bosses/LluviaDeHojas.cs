using System.Collections.Generic;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Las hojas que caen en una franja del ataque de las hojas del Espectro del Pavor
    /// (<see cref="Patrones.PatronHojas"/>). Va en el dibujo de la franja, el prefab que viste su zona
    /// de daño (como la erupción de la Centinela): mientras la zona avisa, no hace nada; cuando
    /// empieza a hacer daño, suelta sus hojas desde lo alto de la franja, que caen hasta el suelo en
    /// lo que dura el daño y se rompen al tocarlo.
    ///
    /// Solo es dibujo: el daño lo hace la zona. Las hojas llevan el material y el orden del dibujo
    /// de la franja (que no dependen de la luz: en la arena del Pavor no hay otra que la de Kael).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class LluviaDeHojas : MonoBehaviour
    {
        // Lo que se balancean al caer, en unidades.
        private const float Balanceo = 0.12f;

        [Tooltip("La hoja que cae, con la punta hacia abajo.")]
        [SerializeField] private Sprite hoja;

        [Tooltip("La hoja rota contra el suelo.")]
        [SerializeField] private Sprite rota;

        [Tooltip("Las hojas de cada franja.")]
        [Min(1)] public int cuantas = 5;

        [Tooltip("Lo que tarda cada hoja en caer: lo que dura el daño de la zona.")]
        [Min(0.05f)] public float caida = 0.6f;

        [Tooltip("Lo que se ve rota antes de desaparecer.")]
        [Min(0f)] public float rotaDura = 0.15f;

        [Tooltip("Ancho y alto de la franja si no está en una zona (para verla suelta).")]
        public Vector2 franja = new Vector2(2f, 5f);

        private readonly List<Hoja> _hojas = new List<Hoja>();
        private ZonaDeDanio _zona;
        private SpriteRenderer _suelo;
        private bool _soltadas;

        private sealed class Hoja
        {
            public SpriteRenderer Dibujo;
            public float X;
            public float Desde;
            public float Inicio;
            public float Desfase;
        }

        /// <summary>¿Ya soltó sus hojas? (Lo hace cuando la zona empieza a hacer daño.)</summary>
        public bool Soltadas => _soltadas;

        /// <summary>Las hojas que siguen cayendo o rotas en el suelo.</summary>
        public int EnElAire => _hojas.Count;

        private Vector2 Franja => _zona != null ? _zona.Tamano : franja;

        private void Awake()
        {
            _suelo = GetComponent<SpriteRenderer>();
            _zona = GetComponentInParent<ZonaDeDanio>();
        }

        private void Update()
        {
            if (!_soltadas && (_zona == null || _zona.Activa))
            {
                Soltar();
            }

            for (var i = _hojas.Count - 1; i >= 0; i--)
            {
                if (!Caer(_hojas[i]))
                {
                    Destroy(_hojas[i].Dibujo.gameObject);
                    _hojas.RemoveAt(i);
                }
            }
        }

        // Desde lo alto de la franja, repartidas a lo ancho y con un poco de desorden: unas salen
        // antes y otras desde más abajo, para que no caigan en fila.
        private void Soltar()
        {
            _soltadas = true;
            if (hoja == null)
            {
                return;
            }

            var tamano = Franja;
            for (var i = 0; i < cuantas; i++)
            {
                var objeto = new GameObject($"Hoja_{i + 1}") { layer = gameObject.layer };
                objeto.transform.SetParent(transform, false);
                var dibujo = objeto.AddComponent<SpriteRenderer>();
                dibujo.sprite = hoja;
                dibujo.sharedMaterial = _suelo.sharedMaterial;
                dibujo.sortingOrder = _suelo.sortingOrder + 1;
                dibujo.flipX = Random.value < 0.5f;

                var fraccion = (i + 0.5f) / cuantas;
                _hojas.Add(new Hoja
                {
                    Dibujo = dibujo,
                    X = (fraccion - 0.5f) * tamano.x * 0.8f,
                    Desde = tamano.y * Random.Range(0.75f, 1f),
                    Inicio = Time.time + Random.Range(0f, caida * 0.3f),
                    Desfase = Random.Range(0f, Mathf.PI * 2f),
                });
                Caer(_hojas[_hojas.Count - 1]);
            }
        }

        // Coloca la hoja donde va; al tocar el suelo se rompe y, pasado su rato, se va (false).
        private bool Caer(Hoja h)
        {
            var t = (Time.time - h.Inicio) / caida;
            if (t < 0f)
            {
                h.Dibujo.enabled = false;
                return true;
            }

            h.Dibujo.enabled = true;
            if (t < 1f)
            {
                var x = h.X + Mathf.Sin(h.Desfase + t * Mathf.PI * 2f) * Balanceo;
                h.Dibujo.transform.localPosition = new Vector3(x, Mathf.Lerp(h.Desde, 0f, t * t), 0f);
                return true;
            }

            if (rota != null)
            {
                h.Dibujo.sprite = rota;
            }

            h.Dibujo.transform.localPosition = new Vector3(h.X, 0f, 0f);
            return t < 1f + rotaDura / caida;
        }
    }
}
