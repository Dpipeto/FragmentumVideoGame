using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fragmentum.Core
{
    /// <summary>
    /// Carga de escenas asíncrona con fundido a negro. Es el único sitio que llama a
    /// <c>SceneManager</c>: el menú, el selector y el fin de nivel piden la carga a la FSM
    /// global, y esta se la pide al cargador.
    ///
    /// Antes de cargar comprueba que la escena esté en Build Settings: una escena inexistente
    /// se avisa por consola y no se carga, en lugar de romper el juego (en la v1, el selector
    /// cargaba «Level2»…«Level10», que no existían).
    ///
    /// El fundido usa tiempo sin escala, así que funciona con el juego en pausa.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private float duracionFundido = 0.35f;
        [SerializeField] private Color colorFundido = Color.black;

        private CanvasGroup _fundido;

        /// <summary>Terminó de cargarse una escena: (nombre).</summary>
        public event Action<string> OnCargaTerminada;

        public bool Cargando { get; private set; }

        private void Awake() => CrearFundido();

        /// <summary>¿Existe la escena en Build Settings?</summary>
        public static bool PuedeCargar(string nombre) =>
            !string.IsNullOrEmpty(nombre) && Application.CanStreamedLevelBeLoaded(nombre);

        /// <summary>
        /// Empieza a cargar la escena. Devuelve false (y avisa) si no existe o si ya hay una
        /// carga en curso; en ese caso no llama a <paramref name="alTerminar"/>.
        /// </summary>
        public bool Cargar(string nombre, Action alTerminar = null)
        {
            if (Cargando)
            {
                Debug.LogWarning($"[Escenas] Ya hay una carga en curso; se ignora la de '{nombre}'.", this);
                return false;
            }

            if (!PuedeCargar(nombre))
            {
                Debug.LogError(
                    $"[Escenas] La escena '{nombre}' no existe o no está en Build Settings. " +
                    "Revísala en la LevelDatabase (Fragmentum → Regenerar todo configura Build Settings).", this);
                return false;
            }

            StartCoroutine(CargarRutina(nombre, alTerminar));
            return true;
        }

        private IEnumerator CargarRutina(string nombre, Action alTerminar)
        {
            Cargando = true;

            // Sin selección, un segundo Enter del teclado o del mando no repite la acción.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            yield return Fundir(1f);

            var operacion = SceneManager.LoadSceneAsync(nombre, LoadSceneMode.Single);
            while (operacion != null && !operacion.isDone)
            {
                yield return null;
            }

            // Un frame más para que los objetos de la escena ejecuten su Start.
            yield return null;

            Cargando = false;
            alTerminar?.Invoke();
            OnCargaTerminada?.Invoke(nombre);
            yield return Fundir(0f);
        }

        private IEnumerator Fundir(float objetivo)
        {
            if (_fundido == null)
            {
                yield break;
            }

            _fundido.blocksRaycasts = objetivo > 0f;
            var inicio = _fundido.alpha;
            var t = 0f;
            while (t < duracionFundido)
            {
                t += Time.unscaledDeltaTime;
                _fundido.alpha = Mathf.Lerp(inicio, objetivo, duracionFundido > 0f ? t / duracionFundido : 1f);
                yield return null;
            }

            _fundido.alpha = objetivo;
        }

        // El fundido es un lienzo propio, por encima de todo, que vive con los sistemas. Tiene
        // su GraphicRaycaster para que, mientras dura la carga, se trague los clics: sin él,
        // blocksRaycasts no hacía nada y un doble clic en el selector pedía dos cargas.
        private void CrearFundido()
        {
            var objeto = new GameObject("Fundido", typeof(RectTransform));
            objeto.transform.SetParent(transform, false);

            var lienzo = objeto.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            lienzo.sortingOrder = 1000;
            objeto.AddComponent<GraphicRaycaster>();

            var imagen = new GameObject("Negro", typeof(RectTransform)).AddComponent<Image>();
            imagen.transform.SetParent(objeto.transform, false);
            imagen.color = colorFundido;
            var rect = imagen.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _fundido = objeto.AddComponent<CanvasGroup>();
            _fundido.alpha = 0f;
            _fundido.blocksRaycasts = false;
            _fundido.interactable = false;
        }
    }
}
