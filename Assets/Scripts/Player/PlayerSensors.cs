using System.Collections.Generic;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Sentidos del jugador: suelo (a través de <see cref="CheckGround"/>), paredes a cada lado
    /// y techo. Todo por consulta física con capas, evaluado una vez por frame antes de que
    /// la FSM decida.
    ///
    /// La pared se detecta a ambos lados por separado. El wall jump usa el lado de la pared
    /// detectada, no la escala del sprite; en el prototipo, depender de <c>localScale.x</c>
    /// hacía saltar hacia la pared cuando el sprite estaba volteado.
    ///
    /// Una pared marcada con <see cref="SinAgarre"/> (el hielo liso) no cuenta como pared: Kael
    /// choca con ella, pero ni se desliza ni salta desde ella.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerSensors : MonoBehaviour
    {
        [SerializeField] private CheckGround suelo;

        [Tooltip("Collider del cuerpo. Si se deja vacío se usa el del mismo objeto.")]
        [SerializeField] private Collider2D cuerpo;

        [Tooltip("Capas que cuentan como pared y como techo. Si se deja vacío: 'suelo' y 'Wall'.")]
        [SerializeField] private LayerMask capasPared;

        [Tooltip("Distancia a la que se detecta una pared, en unidades.")]
        [SerializeField] private float alcancePared = 0.1f;

        [Tooltip("Alto de la zona de detección de pared respecto del cuerpo. Menor que 1 para no confundir el suelo con una pared.")]
        [SerializeField, Range(0.2f, 1f)] private float altoRelativoPared = 0.5f;

        private readonly List<Collider2D> _resultados = new List<Collider2D>(4);
        private ContactFilter2D _filtro;
        private bool _preparado;

        public CheckGround Suelo => suelo;

        public bool EnSuelo => suelo != null && suelo.EnSuelo;

        public bool ParedDerecha { get; private set; }

        public bool ParedIzquierda { get; private set; }

        /// <summary>¿Hay pared en la dirección indicada (-1 izquierda, 1 derecha)?</summary>
        public bool HayParedHacia(int lado) => lado > 0 ? ParedDerecha : lado < 0 && ParedIzquierda;

        private void Awake() => Preparar();

        private void Preparar()
        {
            if (suelo == null)
            {
                suelo = GetComponent<CheckGround>();
            }

            if (cuerpo == null)
            {
                cuerpo = GetComponent<Collider2D>();
            }

            if (suelo == null)
            {
                Debug.LogWarning("[PlayerSensors] Falta el componente CheckGround: el jugador no detectará el suelo.", this);
            }

            var mascara = capasPared.value != 0 ? capasPared.value : Capas.Mascara(Capas.Suelo, Capas.Wall);
            _filtro = new ContactFilter2D();
            _filtro.SetLayerMask(mascara);
            _filtro.useTriggers = false;
            _preparado = true;
        }

        /// <summary>Evalúa suelo y paredes. La llama el jugador al principio de cada frame.</summary>
        public void Actualizar()
        {
            if (!_preparado)
            {
                Preparar();
            }

            if (suelo != null)
            {
                suelo.Evaluar();
            }

            if (cuerpo == null || !cuerpo.enabled)
            {
                ParedDerecha = ParedIzquierda = false;
                return;
            }

            var b = cuerpo.bounds;
            var tamano = new Vector2(alcancePared, b.size.y * altoRelativoPared);
            ParedDerecha = HayAlgo(new Vector2(b.max.x + alcancePared * 0.5f, b.center.y), tamano, soloConAgarre: true);
            ParedIzquierda = HayAlgo(new Vector2(b.min.x - alcancePared * 0.5f, b.center.y), tamano, soloConAgarre: true);
        }

        /// <summary>
        /// ¿Cabe el cuerpo de pie? Mira el hueco que hay entre la parte alta del cuerpo actual
        /// (agachado) y la altura completa. Lo usa CROUCH para no levantarse bajo un techo.
        /// </summary>
        public bool HayEspacioArriba(float alturaDePie)
        {
            if (!_preparado)
            {
                Preparar();
            }

            if (cuerpo == null)
            {
                return true;
            }

            var b = cuerpo.bounds;
            var hueco = alturaDePie - b.size.y;
            if (hueco <= 0.01f)
            {
                return true;
            }

            // Se deja un margen para no detectar el techo que ya se está rozando.
            const float margen = 0.02f;
            var centro = new Vector2(b.center.x, b.max.y + margen + hueco * 0.5f);
            var tamano = new Vector2(b.size.x * 0.9f, Mathf.Max(0.01f, hueco - margen));
            return !HayAlgo(centro, tamano);
        }

        // Con soloConAgarre no cuenta lo marcado como SinAgarre: es pared para chocar, no para agarrarse.
        private bool HayAlgo(Vector2 centro, Vector2 tamano, bool soloConAgarre = false)
        {
            _resultados.Clear();
            Physics2D.OverlapBox(centro, tamano, 0f, _filtro, _resultados);

            var propio = cuerpo != null ? cuerpo.attachedRigidbody : null;
            for (var i = 0; i < _resultados.Count; i++)
            {
                var otro = _resultados[i];
                if (otro == null || (propio != null && otro.attachedRigidbody == propio))
                {
                    continue;
                }

                if (soloConAgarre && otro.TryGetComponent<SinAgarre>(out _))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            var c = cuerpo != null ? cuerpo : GetComponent<Collider2D>();
            if (c == null)
            {
                return;
            }

            var b = c.bounds;
            var tamano = new Vector3(alcancePared, b.size.y * altoRelativoPared, 0f);
            Gizmos.color = ParedDerecha ? Color.cyan : Color.gray;
            Gizmos.DrawWireCube(new Vector3(b.max.x + alcancePared * 0.5f, b.center.y), tamano);
            Gizmos.color = ParedIzquierda ? Color.cyan : Color.gray;
            Gizmos.DrawWireCube(new Vector3(b.min.x - alcancePared * 0.5f, b.center.y), tamano);
        }
    }
}
