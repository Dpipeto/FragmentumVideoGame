using UnityEngine;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Recibe los eventos de los clips de la Centinela sin hacer nada, para probar su Animator
    /// solo, sin jefe. Sin un receptor, Unity da un error por cada evento.
    /// Tiene las mismas funciones que <c>AnimacionJefe</c>.
    /// </summary>
    public class ReceptorEventosJefe : MonoBehaviour
    {
        public int Golpes { get; private set; }

        public void Golpe() => Golpes++;

        public void FinGolpe()
        {
        }

        public void VentanaCorta(float segundos)
        {
        }

        public void Ventana(float segundos)
        {
        }

        public void EnElAire()
        {
        }

        public void Aterrizaje()
        {
        }

        public void FinMuerte()
        {
        }
    }
}
