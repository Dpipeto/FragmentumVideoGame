using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Lava (Anexo A, mundo de la Ira): muerte instantánea por trigger, para el jugador y para los
    /// enemigos. Como la zona de caída, envía un <see cref="Golpe.Letal"/>, que atraviesa los
    /// i-frames y el escudo de la Defensa Reforzada.
    ///
    /// Se ve: el color late despacio (provisional hasta que haya el tile animado, PENDIENTES B10).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Lava : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color colorA = new Color(1f, 0.3f, 0.02f);
        [SerializeField] private Color colorB = new Color(1f, 0.62f, 0.1f);

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void Awake()
        {
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (visual != null)
            {
                visual.color = Color.Lerp(colorA, colorB, 0.5f + 0.5f * Mathf.Sin(Time.time * 2.2f));
            }
        }

        private void OnTriggerEnter2D(Collider2D otro) => Quemar(otro);

        // También en Stay: si algo reaparece o se activa ya dentro, Enter no se repetiría.
        private void OnTriggerStay2D(Collider2D otro) => Quemar(otro);

        private void Quemar(Collider2D otro)
        {
            if (otro.isTrigger)
            {
                return;
            }

            var raiz = otro.attachedRigidbody != null ? otro.attachedRigidbody.gameObject : otro.gameObject;
            var objetivo = raiz.GetComponentInParent<IGolpeable>();
            if (objetivo != null && objetivo.EstaVivo)
            {
                objetivo.RecibirGolpe(Golpe.Letal(transform.position, gameObject));
            }
        }
    }
}
