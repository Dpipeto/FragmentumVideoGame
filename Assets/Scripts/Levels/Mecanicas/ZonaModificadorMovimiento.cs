using System.Collections.Generic;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Zona que cambia la física del jugador mientras está dentro: agua, hielo, barro…
    /// Añade un <see cref="ModificadorMovimiento"/> al motor al entrar y lo quita al salir.
    ///
    /// Es la pieza reutilizable sobre la que se construyen <c>UnderwaterZone</c> (con oxígeno)
    /// e <c>IceSurface</c> en la Fase 10. Ya sirve para probar el Dash Acuático.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ZonaModificadorMovimiento : MonoBehaviour
    {
        public enum TIPOS { AGUA, HIELO, PERSONALIZADO }

        [SerializeField] private TIPOS tipo = TIPOS.AGUA;

        [Tooltip("Solo se usa con el tipo PERSONALIZADO.")]
        [SerializeField] private ModificadorMovimiento personalizado = ModificadorMovimiento.Neutro;

        [SerializeField] private Color colorGizmo = new Color(0.2f, 0.5f, 1f, 0.25f);

        private readonly HashSet<PlayerMotor> _dentro = new HashSet<PlayerMotor>();

        public ModificadorMovimiento Modificador => tipo switch
        {
            TIPOS.AGUA => ModificadorMovimiento.BajoElAgua,
            TIPOS.HIELO => ModificadorMovimiento.Hielo,
            _ => personalizado
        };

        /// <summary>
        /// Cambia el tipo por código, para las zonas que nacen en mitad del juego (el agua y el
        /// hielo de la inundación de la Doncella). A quien ya esté dentro le cambia el modificador.
        /// </summary>
        public void FijarTipo(TIPOS nuevo)
        {
            tipo = nuevo;
            foreach (var motor in _dentro)
            {
                if (motor != null)
                {
                    motor.AgregarModificador(this, Modificador);
                }
            }
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var motor = MotorDe(other);
            if (motor != null && _dentro.Add(motor))
            {
                motor.AgregarModificador(this, Modificador);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var motor = MotorDe(other);
            if (motor != null && _dentro.Remove(motor))
            {
                motor.QuitarModificador(this);
            }
        }

        // Si la zona se apaga con el jugador dentro, no debe dejarle el modificador puesto.
        private void OnDisable()
        {
            foreach (var motor in _dentro)
            {
                if (motor != null)
                {
                    motor.QuitarModificador(this);
                }
            }

            _dentro.Clear();
        }

        // Solo cuenta el cuerpo del jugador: la espada (trigger hijo) no debe mojarlo.
        private static PlayerMotor MotorDe(Collider2D other)
        {
            if (other.isTrigger || other.attachedRigidbody == null)
            {
                return null;
            }

            return other.attachedRigidbody.GetComponent<PlayerMotor>();
        }

        private void OnDrawGizmos()
        {
            var zona = GetComponent<BoxCollider2D>();
            if (zona == null)
            {
                return;
            }

            Gizmos.color = colorGizmo;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(zona.offset, zona.size);
        }
    }
}
