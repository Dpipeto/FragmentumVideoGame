using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Círculo de luz fijo que se registra en <see cref="Iluminacion"/>: siempre encendido, sin
    /// luz que se vea. La antorcha del Nivel 3 es <see cref="Antorcha"/>, con su encendido, su
    /// duración y su luz 2D; este queda para las pruebas y para la luz fija de otros mundos.
    /// </summary>
    public class ZonaIluminada : MonoBehaviour, IFuenteLuz
    {
        [SerializeField] private float radio = 3f;
        [SerializeField] private bool encendida = true;

        public Vector2 Posicion => transform.position;

        public float Radio => radio;

        public bool Encendida
        {
            get => encendida && isActiveAndEnabled;
            set => encendida = value;
        }

        private void OnEnable() => Iluminacion.Registrar(this);

        private void OnDisable() => Iluminacion.Quitar(this);

        private void OnDrawGizmos()
        {
            Gizmos.color = encendida ? new Color(1f, 0.85f, 0.3f, 0.6f) : new Color(0.4f, 0.4f, 0.4f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radio);
        }
    }
}
