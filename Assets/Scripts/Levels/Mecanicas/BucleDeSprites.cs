using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Pasa los fotogramas de un sprite en bucle, a ritmo fijo: la superficie del agua (que se
    /// repite a lo ancho) y las columnas de burbujas del mundo de la Tristeza. Sin Animator: son
    /// decorado, no tienen estados ni eventos.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BucleDeSprites : MonoBehaviour
    {
        [SerializeField] private Sprite[] fotogramas = new Sprite[0];

        [Tooltip("Segundos que dura cada fotograma.")]
        [SerializeField, Min(0.02f)] private float segundos = 0.2f;

        [Tooltip("Segundos de adelanto: dos bucles iguales no van a la vez.")]
        [SerializeField] private float desfase;

        private SpriteRenderer _sprite;
        private int _ultimo = -1;

        public int Fotogramas => fotogramas != null ? fotogramas.Length : 0;

        public float Segundos => segundos;

        /// <summary>El fotograma que toca en el segundo <paramref name="t"/>.</summary>
        public static int Indice(float t, int cuantos, float segundos) =>
            cuantos <= 0 ? -1 : (int)Mathf.Repeat(Mathf.Floor(t / Mathf.Max(0.02f, segundos)), cuantos);

        /// <summary>Cambia el bucle por código (lo usa el dibujo de las franjas de la Doncella).</summary>
        public void Fijar(Sprite[] nuevos, float segundosPorFotograma, float adelanto = 0f)
        {
            fotogramas = nuevos ?? new Sprite[0];
            segundos = Mathf.Max(0.02f, segundosPorFotograma);
            desfase = adelanto;
            _ultimo = -1;
        }

        private void Awake() => _sprite = GetComponent<SpriteRenderer>();

        private void Update()
        {
            var indice = Indice(Time.time + desfase, Fotogramas, segundos);
            if (indice < 0 || indice == _ultimo)
            {
                return;
            }

            _ultimo = indice;
            if (fotogramas[indice] != null)
            {
                _sprite.sprite = fotogramas[indice];
            }
        }
    }
}
