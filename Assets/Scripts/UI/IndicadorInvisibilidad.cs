using Fragmentum.Core.Input;
using Fragmentum.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Cuándo vuelve a estar lista la Invisibilidad Parcial (la que Kael gana en el Mundo 3): junto a la
    /// vida, un cuadro con su tecla (Q) que se rellena mientras espera, como la barra de aire. Mientras
    /// Kael está invisible, el cuadro late entero; lista, la tecla brilla. Solo se ve cuando Kael tiene
    /// la habilidad. No necesita imagen (<c>Nivel4_Diseno.md</c>, sección 8).
    /// </summary>
    public class IndicadorInvisibilidad : MonoBehaviour
    {
        private const string PlantillaTecla = "{Ability}";
        private const string TeclaPorDefecto = "Q";

        /// <summary>El color del Miedo, de donde viene la habilidad.</summary>
        public static readonly Color ColorLista = new Color(0.5f, 0.88f, 0.69f, 1f);

        private static readonly Color ColorEsperando = new Color(0.5f, 0.88f, 0.69f, 0.35f);

        private GameObject _cuadro;
        private Image _relleno;
        private Text _tecla;
        private PlayerAbilities _habilidades;

        /// <summary>¿Se ve? (Kael tiene la habilidad.)</summary>
        public bool Visible => _cuadro != null && _cuadro.activeSelf;

        /// <summary>Lo relleno que está (0 recién gastada, 1 lista o en uso).</summary>
        public float Carga { get; private set; } = 1f;

        /// <summary>¿Kael está invisible ahora mismo?</summary>
        public bool EnUso { get; private set; }

        /// <summary>¿Se puede usar ya?</summary>
        public bool Lista { get; private set; }

        public string Tecla => _tecla != null ? _tecla.text : string.Empty;

        /// <summary>
        /// Lo relleno que tiene que estar el cuadro: lleno mientras dura y cuando está lista; mientras
        /// espera, lo que lleva de la espera (<paramref name="espera"/> es lo que le queda).
        /// </summary>
        public static float Relleno(bool enUso, float espera, float recarga)
        {
            if (enUso || recarga <= 0f)
            {
                return 1f;
            }

            return 1f - Mathf.Clamp01(espera / recarga);
        }

        /// <summary>Crea el cuadro, oculto. La llama el <see cref="Hud"/>.</summary>
        public void Construir(RectTransform raiz)
        {
            var fondo = FabricaUI.Objeto("Invisibilidad", raiz).gameObject.AddComponent<Image>();
            fondo.color = new Color(0f, 0f, 0f, 0.6f);
            fondo.raycastTarget = false;
            FabricaUI.Colocar(fondo.rectTransform, new Vector2(0f, 1f), new Vector2(402f, -20f), new Vector2(44f, 44f), new Vector2(0f, 1f));
            _cuadro = fondo.gameObject;

            _relleno = FabricaUI.Objeto("Relleno", fondo.transform).gameObject.AddComponent<Image>();
            _relleno.color = ColorEsperando;
            _relleno.raycastTarget = false;
            _relleno.type = Image.Type.Filled;
            _relleno.fillMethod = Image.FillMethod.Vertical;
            _relleno.fillOrigin = (int)Image.OriginVertical.Bottom;
            _relleno.sprite = Hud.SpriteBlanco();
            _relleno.rectTransform.anchorMin = Vector2.zero;
            _relleno.rectTransform.anchorMax = Vector2.one;
            _relleno.rectTransform.offsetMin = new Vector2(3f, 3f);
            _relleno.rectTransform.offsetMax = new Vector2(-3f, -3f);

            _tecla = FabricaUI.Titulo(fondo.transform, "Tecla", TeclaPorDefecto, 24, Color.white);
            _tecla.raycastTarget = false;
            FabricaUI.Estirar(_tecla.rectTransform);
            _cuadro.SetActive(false);
        }

        /// <summary>Lo engancha a Kael (o lo suelta, con null).</summary>
        public void Vincular(PlayerController jugador)
        {
            _habilidades = jugador != null ? jugador.Habilidades : null;
            var entrada = jugador != null ? jugador.GetComponent<FuenteInputJugador>() : null;
            var tecla = entrada != null ? EtiquetasControles.Resolver(PlantillaTecla, entrada.Acciones) : string.Empty;
            if (_tecla != null)
            {
                _tecla.text = string.IsNullOrEmpty(tecla) || tecla == EtiquetasControles.SinTecla ? TeclaPorDefecto : tecla;
            }

            Actualizar();
        }

        private void Update() => Actualizar();

        private void Actualizar()
        {
            if (_cuadro == null)
            {
                return;
            }

            var tiene = _habilidades != null && _habilidades.Tiene(HABILIDADES.INVISIBILIDAD_PARCIAL);
            if (_cuadro.activeSelf != tiene)
            {
                _cuadro.SetActive(tiene);
            }

            if (!tiene)
            {
                return;
            }

            EnUso = _habilidades.EsInvisible;
            Lista = _habilidades.InvisibilidadLista;
            Carga = Relleno(EnUso, _habilidades.EsperaInvisibilidad, _habilidades.CooldownInvisibilidad);
            _relleno.fillAmount = Carga;
            if (EnUso)
            {
                var latido = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
                _relleno.color = new Color(ColorLista.r, ColorLista.g, ColorLista.b, latido);
            }
            else
            {
                _relleno.color = Lista ? ColorLista : ColorEsperando;
            }

            _tecla.color = Lista || EnUso ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }
    }
}
