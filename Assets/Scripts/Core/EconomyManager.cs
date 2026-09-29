using System;
using Fragmentum.Core.Save;
using UnityEngine;

namespace Fragmentum.Core
{
    /// <summary>
    /// Monedas del jugador. Sale del <c>GameManager</c> de la v1, que mezclaba monedas, UI de
    /// Game Over y navegación de escenas, y corrige su bug: las monedas se leían de
    /// PlayerPrefs pero no se guardaban nunca. Ahora viven en los datos de la partida y se
    /// guardan con el resto del progreso.
    /// </summary>
    [DisallowMultipleComponent]
    public class EconomyManager : MonoBehaviour
    {
        private DatosGuardado _datos;

        /// <summary>Monedas actuales, para la UI.</summary>
        public event Action<int> OnMonedasCambiadas;

        public int Monedas => _datos != null ? _datos.monedas : 0;

        /// <summary>Apunta a los datos de la partida (al cargar o al empezar una nueva).</summary>
        public void Vincular(DatosGuardado datos)
        {
            _datos = datos;
            OnMonedasCambiadas?.Invoke(Monedas);
        }

        public void Sumar(int cantidad)
        {
            if (_datos == null || cantidad <= 0)
            {
                return;
            }

            _datos.monedas += cantidad;
            OnMonedasCambiadas?.Invoke(_datos.monedas);
        }

        /// <summary>Gasta si hay suficientes. Devuelve si se pudo.</summary>
        public bool Gastar(int cantidad)
        {
            if (_datos == null || cantidad <= 0 || _datos.monedas < cantidad)
            {
                return false;
            }

            _datos.monedas -= cantidad;
            OnMonedasCambiadas?.Invoke(_datos.monedas);
            return true;
        }
    }
}
