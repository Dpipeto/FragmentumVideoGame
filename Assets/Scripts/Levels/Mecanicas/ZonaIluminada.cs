using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Círculo de luz fijo que se registra en <see cref="Iluminacion"/>. Es la versión
    /// mínima de la antorcha del Nivel 3: la <c>Torch</c> de la Fase 10 añadirá el encendido
    /// con la tecla E, la duración de 15 s y el despertar de los Espectros.
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
