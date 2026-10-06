using System.Collections.Generic;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Lava (Anexo A, mundo de la Ira): muerte instantánea por trigger, para el jugador y para los
    /// enemigos. Como la zona de caída, envía un <see cref="Golpe.Letal"/>, que atraviesa los
    /// i-frames y el escudo de la Defensa Reforzada. No quema a quien sea
    /// <see cref="IInmuneALava"/>: el Golem de Ceniza la cruza andando.
    ///
    /// <b>El dibujo.</b> Una franja de 151 × 64 px (3,15 u) que se repite en horizontal, alternando
    /// la normal y la espejada: la franja no se dibujó para encajar consigo misma, y espejando una
    /// de cada dos el borde derecho de una es el borde derecho de la otra y la costura desaparece.
    /// <see cref="tramos"/> dice cuántas franjas, centradas en este objeto. Las monta el propio
    /// componente, también en el editor (sin guardarlas en la escena), y ajusta el trigger a ellas.
    ///
    /// El pivote está arriba en el centro: la costra (la fila 0 del dibujo) queda a la altura del
    /// Transform, y el trigger empieza ahí, no más abajo.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider2D))]
    public class Lava : MonoBehaviour
    {
        private const string PrefijoTramo = "Tramo_";

        [Header("Dibujo")]
        [Tooltip("La franja de lava (Lava.png), con la costra en su fila de arriba.")]
        [SerializeField] private Sprite franja;

        [Tooltip("La misma franja espejada (Lava_espejo.png). Se alterna con la otra para que no se vea la juntura.")]
        [SerializeField] private Sprite franjaEspejo;

        [Tooltip("Cuántas franjas, de izquierda a derecha y centradas en este objeto (3,15 u cada una).")]
        [SerializeField, Min(1)] private int tramos = 1;

        [Tooltip("Orden de dibujo: por delante del suelo, para que la costra se vea.")]
        [SerializeField] private int ordenDibujo = 1;

        private readonly List<SpriteRenderer> _dibujos = new List<SpriteRenderer>();
        private bool _pendiente = true;

        public int Tramos => tramos;

        /// <summary>Lo que mide una franja (3,15 u con su arte).</summary>
        public float AnchoFranja => franja != null ? franja.bounds.size.x : 3f;

        public float Ancho => tramos * AnchoFranja;

        public float Alto => franja != null ? franja.bounds.size.y : 1f;

        /// <summary>Las franjas montadas, de izquierda a derecha.</summary>
        public IReadOnlyList<SpriteRenderer> Dibujos => _dibujos;

        /// <summary>Cambia cuántas franjas hay y las vuelve a montar (herramientas de nivel y pruebas).</summary>
        public void FijarTramos(int cuantos)
        {
            tramos = Mathf.Max(1, cuantos);
            Montar();
        }

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnValidate() => _pendiente = true;

        private void OnEnable()
        {
#if UNITY_EDITOR
            // En el editor se monta fuera de la activación: no se pueden crear ni destruir hijos
            // mientras el objeto se activa.
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += MontarSiPendiente;
            }
#endif
        }

        private void Start() => Montar();

        private void Update()
        {
            if (!Application.isPlaying)
            {
                MontarSiPendiente();
            }
        }

        private void MontarSiPendiente()
        {
            if (this != null && isActiveAndEnabled && (_pendiente || _dibujos.Count != tramos))
            {
                Montar();
            }
        }

        private void Montar()
        {
            _pendiente = false;
            Desmontar();

            var zona = GetComponent<BoxCollider2D>();
            zona.isTrigger = true;
            var tamano = new Vector2(Ancho, Alto);
            if (zona.size != tamano)
            {
                zona.size = tamano;
                zona.offset = new Vector2(0f, -Alto / 2f);
            }

            if (franja == null)
            {
                return;
            }

            var izquierda = -Ancho / 2f;
            for (var i = 0; i < tramos; i++)
            {
                var objeto = new GameObject(PrefijoTramo + i) { layer = gameObject.layer };
                // En el editor no se guardan: se montan de nuevo al abrir la escena.
                if (!Application.isPlaying)
                {
                    objeto.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                }

                objeto.transform.SetParent(transform, false);
                objeto.transform.localPosition = new Vector3(izquierda + i * AnchoFranja, 0f, 0f);
                var dibujo = objeto.AddComponent<SpriteRenderer>();
                dibujo.sprite = i % 2 == 1 && franjaEspejo != null ? franjaEspejo : franja;
                dibujo.sortingOrder = ordenDibujo;
                _dibujos.Add(dibujo);
            }
        }

        // Quita las franjas montadas antes (también las que quedaran de una recarga de scripts).
        private void Desmontar()
        {
            _dibujos.Clear();
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var hijo = transform.GetChild(i).gameObject;
                if (hijo.name.StartsWith(PrefijoTramo))
                {
                    if (Application.isPlaying)
                    {
                        hijo.SetActive(false);
                        Destroy(hijo);
                    }
                    else
                    {
                        DestroyImmediate(hijo);
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D otro) => Quemar(otro);

        // También en Stay: si algo reaparece o se activa ya dentro, Enter no se repetiría.
        private void OnTriggerStay2D(Collider2D otro) => Quemar(otro);

        private void Quemar(Collider2D otro)
        {
            if (otro.isTrigger || !Application.isPlaying)
            {
                return;
            }

            var raiz = otro.attachedRigidbody != null ? otro.attachedRigidbody.gameObject : otro.gameObject;
            if (raiz.GetComponentInParent<IInmuneALava>() is { InmuneALava: true })
            {
                return;
            }

            var objetivo = raiz.GetComponentInParent<IGolpeable>();
            if (objetivo != null && objetivo.EstaVivo)
            {
                objetivo.RecibirGolpe(Golpe.Letal(transform.position, gameObject));
            }
        }
    }
}
