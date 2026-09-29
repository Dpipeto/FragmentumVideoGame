using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Narrative;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Reproductor de cinemáticas (<c>CutscenePlayer</c> del plan): muestra las diapositivas de
    /// un <see cref="CutsceneData"/> a pantalla completa mientras la FSM global está en CUTSCENE.
    ///
    /// - Cada diapositiva dura lo que diga su dato; un clic, Enter o Espacio pasan a la
    ///   siguiente, y Esc o Start saltan la cinemática entera si es saltable.
    /// - <b>Funciona sin arte:</b> una diapositiva sin imagen se ve en negro con su texto, y sin
    ///   texto dice qué falta. La cinemática inicial ya se puede ver, cronometrar y saltar
    ///   (criterio de aceptación de la Fase 8).
    /// - Cuenta el tiempo sin escala, porque la cinemática para el juego.
    /// - Al terminar se lo dice a la FSM global, que decide a dónde se va.
    /// </summary>
    public class PantallaCinematica : MonoBehaviour
    {
        private const float DuracionSinDiapositivas = 3f;

        private static readonly HashSet<string> AvisadasSinArte = new HashSet<string>();

        private Image _imagen;
        private Text _texto;
        private Text _progreso;
        private Text _ayuda;
        private Button _siguiente;
        private AudioSource _audio;
        private bool _construida;
        private float _finDiapositiva;

        public CutsceneData Actual { get; private set; }

        public int Indice { get; private set; }

        public bool Reproduciendo { get; private set; }

        public string TextoActual => _texto != null ? _texto.text : string.Empty;

        public bool Visible => _construida && gameObject.activeSelf;

        private int Total => Actual != null && Actual.diapositivas.Count > 0 ? Actual.diapositivas.Count : 1;

        public void Construir()
        {
            if (_construida)
            {
                return;
            }

            _construida = true;
            gameObject.SetActive(false);
            var raiz = (RectTransform)transform;
            FabricaUI.Estirar(raiz);

            FabricaUI.Panel(raiz, "Fondo", Color.black);

            _imagen = FabricaUI.Objeto("Imagen", raiz).gameObject.AddComponent<Image>();
            _imagen.preserveAspect = true;
            _imagen.raycastTarget = false;
            FabricaUI.Estirar(_imagen.rectTransform);

            // Toda la pantalla es el botón «siguiente»: clic, Enter o Espacio.
            _siguiente = FabricaUI.Objeto("Siguiente", raiz).gameObject.AddComponent<Button>();
            var zona = _siguiente.gameObject.AddComponent<Image>();
            zona.color = new Color(0f, 0f, 0f, 0f);
            FabricaUI.Estirar((RectTransform)_siguiente.transform);
            _siguiente.transition = Selectable.Transition.None;
            _siguiente.onClick.AddListener(Siguiente);

            var banda = FabricaUI.Panel(raiz, "Banda", new Color(0f, 0f, 0f, 0.55f), false);
            FabricaUI.Colocar(banda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1920f, 220f));

            _texto = FabricaUI.Texto(raiz, "Texto", string.Empty, 34, new Color(0.94f, 0.93f, 0.85f, 1f));
            FabricaUI.Colocar(_texto.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(1400f, 180f));
            _texto.raycastTarget = false;

            _progreso = FabricaUI.Texto(raiz, "Progreso", string.Empty, 20, new Color(1f, 1f, 1f, 0.45f), TextAnchor.UpperRight);
            FabricaUI.Colocar(_progreso.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -32f), new Vector2(300f, 30f), new Vector2(1f, 1f));
            _progreso.raycastTarget = false;

            _ayuda = FabricaUI.Texto(raiz, "Ayuda", string.Empty, 18, new Color(1f, 1f, 1f, 0.4f));
            FabricaUI.Colocar(_ayuda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(1200f, 30f));
            _ayuda.raycastTarget = false;

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.ignoreListenerPause = true;
        }

        /// <summary>Empieza a reproducir. Sin datos, termina en el acto.</summary>
        public void Reproducir(CutsceneData cinematica)
        {
            Construir();
            Actual = cinematica;
            if (cinematica == null)
            {
                Debug.LogWarning("[Cinemática] La FSM pidió una cinemática sin datos: se continúa sin ella.");
                Terminar();
                return;
            }

            if (cinematica.FaltaArte && AvisadasSinArte.Add(cinematica.Id))
            {
                Debug.LogWarning($"[Cinemática] '{cinematica.Id}' no tiene imágenes (PENDIENTES B1): se muestra en negro con su texto.");
            }

            Reproduciendo = true;
            gameObject.SetActive(true);
            _ayuda.text = cinematica.saltable ? "CLIC O ENTER  siguiente   ·   ESC  saltar" : "CLIC O ENTER  siguiente";
            Mostrar(0);
        }

        /// <summary>Pasa a la diapositiva siguiente, o termina si era la última.</summary>
        public void Siguiente()
        {
            if (!Reproduciendo)
            {
                return;
            }

            if (Indice + 1 >= Total)
            {
                Terminar();
                return;
            }

            Mostrar(Indice + 1);
        }

        /// <summary>Esc o Start: salta la cinemática entera si es saltable.</summary>
        public void Saltar()
        {
            if (Reproduciendo && Actual != null && Actual.saltable)
            {
                Terminar();
            }
        }

        /// <summary>La oculta sin avisar a la FSM (cuando otro sistema cambió el estado).</summary>
        public void Ocultar()
        {
            Reproduciendo = false;
            if (_audio != null)
            {
                _audio.Stop();
            }

            if (_construida)
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (Reproduciendo && Time.unscaledTime >= _finDiapositiva)
            {
                Siguiente();
            }
        }

        private void Mostrar(int indice)
        {
            Indice = indice;
            var diapositiva = Actual.diapositivas.Count > indice ? Actual.diapositivas[indice] : null;

            _imagen.sprite = diapositiva != null ? diapositiva.imagen : null;
            _imagen.enabled = _imagen.sprite != null;
            _texto.text = Actual.TextoDe(indice);
            _progreso.text = $"{indice + 1} / {Total}";
            _finDiapositiva = Time.unscaledTime + (diapositiva != null ? diapositiva.duracion : DuracionSinDiapositivas);

            _audio.Stop();
            if (diapositiva != null && diapositiva.audio != null)
            {
                _audio.clip = diapositiva.audio;
                _audio.Play();
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_siguiente.gameObject);
            }
        }

        private void Terminar()
        {
            Ocultar();
            var juego = GameStateMachine.Instancia;
            if (juego != null)
            {
                juego.TerminarCinematica();
            }
        }
    }
}
