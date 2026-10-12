using System;
using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Funde en transparente las últimas filas del sprite de la Doncella del Lamento: su cola de
    /// agua se sale del lienzo por abajo y acaba en un corte recto. Decidido el 2026-10-10: los
    /// dibujos se conservan y el juego funde el corte.
    ///
    /// El sprite lleva el material de <c>Fragmentum/SpriteFundidoAbajo</c>; este componente le dice,
    /// en cada fotograma, cuánto fundir. Los fotogramas que no acaban en la cola cortada (llorando,
    /// con la cola enroscada, y el charco de la muerte) no se funden: <see cref="enteros"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ColaFundida : MonoBehaviour
    {
        private static readonly int Fundido = Shader.PropertyToID("_Fundido");

        [Tooltip("Fracción del alto del lienzo que se funde por abajo.")]
        [SerializeField, Range(0f, 0.5f)] private float fraccion = 0.08f;

        [Tooltip("Fotogramas que se ven enteros: no acaban en la cola cortada.")]
        [SerializeField] private Sprite[] enteros = new Sprite[0];

        private SpriteRenderer _sprite;
        private MaterialPropertyBlock _bloque;
        private Sprite _ultimo;
        private bool _aplicado;

        /// <summary>Lo que se funde el fotograma que se ve ahora (0 = nada).</summary>
        public float FraccionActual => _sprite != null && Array.IndexOf(enteros, _sprite.sprite) >= 0 ? 0f : fraccion;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _bloque = new MaterialPropertyBlock();
        }

        private void OnEnable() => _aplicado = false;

        // Después del Animator: el fotograma ya es el de esta imagen.
        private void LateUpdate()
        {
            if (_aplicado && _sprite.sprite == _ultimo)
            {
                return;
            }

            _aplicado = true;
            _ultimo = _sprite.sprite;
            _sprite.GetPropertyBlock(_bloque);
            _bloque.SetFloat(Fundido, FraccionActual);
            _sprite.SetPropertyBlock(_bloque);
        }
    }
}
