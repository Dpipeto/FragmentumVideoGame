using UnityEngine;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Recibe los eventos de los clips de un enemigo con arte (el Golem) cuando una prueba
    /// reproduce su Animator sin el resto del enemigo: sin receptor, Unity da un error por cada
    /// evento. No hace nada.
    /// </summary>
    public class ReceptorEventosEnemigo : MonoBehaviour
    {
        public void AbrirGolpe()
        {
        }

        public void CerrarGolpe()
        {
        }

        public void FinAtaque()
        {
        }

        public void FinMuerte()
        {
        }
    }
}
