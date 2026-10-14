using Fragmentum.Core;
using Fragmentum.Core.Input;
using Fragmentum.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// El aviso de Interactuar: cuando Kael tiene algo al alcance (una antorcha), sobre ello aparece
    /// la tecla y lo que hace («E · Encender»). Es el tutorial de la primera antorcha del mundo del
    /// Miedo, y sirve para todas.
    ///
    /// Vive en el HUD (encima de todo, sin luz que lo apague) y se coloca cada fotograma sobre lo
    /// que Kael usaría, pasando su posición del mundo a la pantalla.
    /// </summary>
    public class AvisoInteraccion : MonoBehaviour
    {
        private const string PlantillaTecla = "{Interact}";
        private const string TeclaPorDefecto = "E";

        // Por encima del pie de la antorcha, en unidades: sobre su llama.
        private static readonly Vector2 Encima = new Vector2(0f, 2.3f);

        private RectTransform _raiz;
        private Text _texto;
        private InteraccionJugador _interaccion;
        private string _tecla = TeclaPorDefecto;

        /// <summary>¿Se ve el aviso?</summary>
        public bool Visible => _texto != null && _texto.gameObject.activeSelf;

        /// <summary>Lo que dice ahora mismo («E · Encender»), o vacío.</summary>
        public string Texto => Visible ? _texto.text : string.Empty;

        /// <summary>Crea el texto, oculto. La llama el <see cref="Hud"/>.</summary>
        public void Construir(RectTransform raiz)
        {
            _raiz = raiz;
            _texto = FabricaUI.Titulo(raiz, "AvisoInteraccion", string.Empty, 26, new Color(1f, 0.9f, 0.7f, 0.95f));
            FabricaUI.Colocar(_texto.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 40f));
            _texto.raycastTarget = false;
            var sombra = _texto.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0f, 0f, 0f, 0.8f);
            sombra.effectDistance = new Vector2(2f, -2f);
            _texto.gameObject.SetActive(false);
        }

        /// <summary>Lo engancha a Kael (o lo suelta, con null).</summary>
        public void Vincular(PlayerController jugador)
        {
            _interaccion = jugador != null ? jugador.GetComponent<InteraccionJugador>() : null;
            var entrada = jugador != null ? jugador.GetComponent<FuenteInputJugador>() : null;
            var tecla = entrada != null ? EtiquetasControles.Resolver(PlantillaTecla, entrada.Acciones) : string.Empty;
            _tecla = string.IsNullOrEmpty(tecla) || tecla == EtiquetasControles.SinTecla ? TeclaPorDefecto : tecla;
        }

        private void LateUpdate()
        {
            if (_texto == null)
            {
                return;
            }

            var candidato = _interaccion != null && _interaccion.isActiveAndEnabled ? _interaccion.Candidato : null;
            var camara = Camera.main;
            if (candidato == null || camara == null)
            {
                Ocultar();
                return;
            }

            var pantalla = camara.WorldToScreenPoint(candidato.PuntoInteraccion + Encima);
            if (pantalla.z < 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_raiz, pantalla, null, out var local))
            {
                Ocultar();
                return;
            }

            _texto.text = $"{_tecla} · {candidato.Accion}";
            _texto.rectTransform.anchoredPosition = local;
            if (!_texto.gameObject.activeSelf)
            {
                _texto.gameObject.SetActive(true);
            }
        }

        private void Ocultar()
        {
            if (_texto.gameObject.activeSelf)
            {
                _texto.gameObject.SetActive(false);
            }
        }
    }
}
