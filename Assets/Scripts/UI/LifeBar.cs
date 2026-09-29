using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Barra de vida que se pinta <b>solo cuando la vida cambia</b>, escuchando el evento
    /// <c>OnVidaCambiada</c> de <see cref="Health"/>.
    ///
    /// Corrige el <c>LifeBar</c> del prototipo, que hacía <c>GameObject.Find("Player")</c> (y
    /// encontraba el sprite decorativo que también se llamaba Player) y leía la vida en cada
    /// frame. Admite una imagen de relleno, corazones sueltos, o ambos.
    /// </summary>
    public class LifeBar : MonoBehaviour
    {
        [Tooltip("Vida que se muestra. Vacío = la del objeto con el tag Player.")]
        [SerializeField] private Health vida;

        [Tooltip("Imagen de relleno (Image Type = Filled).")]
        [SerializeField] private Image relleno;

        [Tooltip("Opcional: un corazón por punto de vida.")]
        [SerializeField] private Image[] corazones = new Image[0];

        [SerializeField] private Color colorCorazonVacio = new Color(1f, 1f, 1f, 0.25f);

        private Color[] _coloresCorazon = new Color[0];
        private bool _suscrito;

        private void Awake()
        {
            _coloresCorazon = new Color[corazones.Length];
            for (var i = 0; i < corazones.Length; i++)
            {
                _coloresCorazon[i] = corazones[i] != null ? corazones[i].color : Color.white;
            }
        }

        private void OnEnable()
        {
            if (vida == null)
            {
                var jugador = GameObject.FindGameObjectWithTag(GameTags.Player);
                vida = jugador != null ? jugador.GetComponent<Health>() : null;
            }

            if (vida == null)
            {
                Debug.LogWarning("[LifeBar] No hay vida asignada ni jugador con el tag Player. La barra no se actualizará.", this);
                return;
            }

            vida.OnVidaCambiada += Pintar;
            _suscrito = true;
        }

        private void OnDisable()
        {
            if (_suscrito && vida != null)
            {
                vida.OnVidaCambiada -= Pintar;
            }

            _suscrito = false;
        }

        // En Start todas las vidas ya se inicializaron (Awake): se pide el valor actual.
        private void Start()
        {
            if (vida != null)
            {
                vida.NotificarVida();
            }
        }

        private void Pintar(int actual, int maxima)
        {
            if (relleno != null)
            {
                relleno.fillAmount = maxima > 0 ? (float)actual / maxima : 0f;
            }

            for (var i = 0; i < corazones.Length; i++)
            {
                if (corazones[i] == null)
                {
                    continue;
                }

                corazones[i].gameObject.SetActive(i < maxima);
                corazones[i].color = i < actual ? _coloresCorazon[i] : colorCorazonVacio;
            }
        }
    }
}
