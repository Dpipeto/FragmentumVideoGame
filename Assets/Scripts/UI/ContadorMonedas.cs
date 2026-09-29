using Fragmentum.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Muestra las monedas escuchando a <see cref="EconomyManager"/>. En la v1 el GameManager
    /// escribía directamente el texto de las monedas.
    /// </summary>
    public class ContadorMonedas : MonoBehaviour
    {
        [SerializeField] private Text texto;
        [SerializeField] private string formato = "Monedas: {0}";

        private EconomyManager _economia;

        private void OnEnable()
        {
            if (texto == null) texto = GetComponent<Text>();
            _economia = GameManager.Instancia != null ? GameManager.Instancia.Economia : null;
            if (_economia != null)
            {
                _economia.OnMonedasCambiadas += Pintar;
                Pintar(_economia.Monedas);
            }
            else if (texto != null)
            {
                texto.text = string.Empty;
            }
        }

        private void OnDisable()
        {
            if (_economia != null)
            {
                _economia.OnMonedasCambiadas -= Pintar;
            }
        }

        private void Pintar(int monedas)
        {
            if (texto != null)
            {
                texto.text = string.Format(formato, monedas);
            }
        }
    }
}
