using Fragmentum.Core.Combate;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Enemies
{
    /// <summary>
    /// El golpe del Lamento pesa: cada vez que su zarpazo o su cuerpo alcanzan a Kael, este va a la
    /// mitad de velocidad durante 2 s (<see cref="Pesadumbre"/>). Escucha el
    /// <see cref="Hitbox.OnImpacto"/> de sus hitbox, así que solo pesa el golpe que entra: con
    /// i-frames o escudo, nada. No toca <see cref="EnemyController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class Pesar : MonoBehaviour
    {
        [Tooltip("Las hitbox que pesan: el arma y el contacto.")]
        [SerializeField] private Hitbox[] golpes = new Hitbox[0];

        [Tooltip("Segundos que dura el pesar.")]
        [Min(0.1f)] public float duracion = 2f;

        [Tooltip("A qué fracción de su velocidad va Kael.")]
        [Range(0.1f, 1f)] public float factorVelocidad = 0.5f;

        public Hitbox[] Golpes => golpes;

        private void OnEnable()
        {
            foreach (var golpe in golpes)
            {
                if (golpe != null)
                {
                    golpe.OnImpacto += AlImpactar;
                }
            }
        }

        private void OnDisable()
        {
            foreach (var golpe in golpes)
            {
                if (golpe != null)
                {
                    golpe.OnImpacto -= AlImpactar;
                }
            }
        }

        private void AlImpactar(IGolpeable objetivo, Golpe golpe)
        {
            if (objetivo is Component componente)
            {
                var pesadumbre = componente.GetComponentInParent<Pesadumbre>();
                if (pesadumbre != null)
                {
                    pesadumbre.Aplicar(duracion, factorVelocidad);
                }
            }
        }
    }
}
