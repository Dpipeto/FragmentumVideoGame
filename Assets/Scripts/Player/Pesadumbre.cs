using System;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// El pesar que deja el Lamento (mundo de la Tristeza): Kael va a la mitad de velocidad
    /// durante 2 s. Es el «reducen velocidad al contacto» del Anexo A: la tristeza se pega si te
    /// dejas alcanzar.
    ///
    /// Vive en Kael y no en el enemigo, para que se pase aunque el Lamento muera o desaparezca.
    /// Es un <see cref="ModificadorMovimiento"/> con su propia fuente, como el agua o el hielo,
    /// así que se multiplica con ellos. Otro zarpazo renueva el tiempo, no lo suma. Al reaparecer
    /// se quita.
    /// </summary>
    [DisallowMultipleComponent]
    public class Pesadumbre : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Health vida;

        private float _fin = float.NegativeInfinity;

        /// <summary>Empieza (true) o se pasa (false). Para el audio y los VFX.</summary>
        public event Action<bool> OnCambio;

        public bool Activa { get; private set; }

        /// <summary>Segundos que le quedan.</summary>
        public float Restante => Activa ? Mathf.Max(0f, _fin - Time.time) : 0f;

        /// <summary>Lo que hace el pesar: solo frena; ni la gravedad ni el salto cambian.</summary>
        public static ModificadorMovimiento Modificador(float factorVelocidad) =>
            new ModificadorMovimiento(Mathf.Clamp(factorVelocidad, 0.05f, 1f), 1f, 1f, 1f, false);

        private void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (vida == null) vida = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (vida != null)
            {
                vida.OnRevivir += Quitar;
            }
        }

        private void OnDisable()
        {
            if (vida != null)
            {
                vida.OnRevivir -= Quitar;
            }

            Quitar();
        }

        private void Update()
        {
            if (Activa && Time.time >= _fin)
            {
                Quitar();
            }
        }

        /// <summary>Kael va a <paramref name="factorVelocidad"/> de su velocidad durante <paramref name="segundos"/>.</summary>
        public void Aplicar(float segundos, float factorVelocidad)
        {
            if (motor == null || segundos <= 0f)
            {
                return;
            }

            motor.AgregarModificador(this, Modificador(factorVelocidad));
            _fin = Mathf.Max(_fin, Time.time + segundos);
            if (!Activa)
            {
                Activa = true;
                OnCambio?.Invoke(true);
            }
        }

        public void Quitar()
        {
            if (!Activa)
            {
                return;
            }

            if (motor != null)
            {
                motor.QuitarModificador(this);
            }

            Activa = false;
            _fin = float.NegativeInfinity;
            OnCambio?.Invoke(false);
        }
    }
}
