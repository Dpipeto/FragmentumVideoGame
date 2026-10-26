using UnityEngine;

namespace Fragmentum.Core.Combate
{
    /// <summary>
    /// El dibujo animado de una <see cref="ZonaDeDanio"/> que sigue sus fases (la erupción de la
    /// Centinela). La zona lo avisa y su Animator hace el resto:
    /// - mientras la zona avisa, el aviso (la grieta que brilla);
    /// - al quemar (<c>Quema</c>), brota y arde;
    /// - al acabar la zona (<c>Apagada</c>) se suelta de ella y sigue un momento, ya sin daño,
    ///   para apagarse; luego se destruye solo.
    ///
    /// Un dibujo sin este componente (la onda) vive y muere con su zona.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class DibujoDeZona : MonoBehaviour
    {
        [Tooltip("Segundos que sigue viéndose al acabar la zona: lo que dura su clip de apagarse. 0 = desaparece con ella.")]
        [Min(0f)] public float duracionApagado;

        private Animator _animator;

        private Animator Animador => _animator != null ? _animator : _animator = GetComponent<Animator>();

        /// <summary>La zona ya hace daño.</summary>
        public void Quemar() => AnimatorParams.FijarBool(Animador, AnimatorParams.Quema, true);

        /// <summary>La zona acabó: el dibujo se queda donde estaba, se apaga y desaparece.</summary>
        public void Apagar()
        {
            if (duracionApagado <= 0f)
            {
                return; // se va con la zona
            }

            transform.SetParent(null, true);
            AnimatorParams.FijarBool(Animador, AnimatorParams.Apagada, true);
            Destroy(gameObject, duracionApagado);
        }
    }
}
