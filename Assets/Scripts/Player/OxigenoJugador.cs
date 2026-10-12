using System;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// La cuenta del aire, sin Unity: cuánto queda y cuándo toca perder un corazón. La usa
    /// <see cref="OxigenoJugador"/> y la prueban las pruebas sin escena.
    ///
    /// Bajo el agua el aire baja un segundo por segundo. Al agotarse, cada
    /// <see cref="IntervaloAsfixia"/> s se pierde un corazón, y el primero llega en cuanto el aire
    /// llega a cero. Fuera del agua el aire se recarga deprisa y la asfixia se olvida.
    /// </summary>
    public sealed class ReservaOxigeno
    {
        public ReservaOxigeno(float capacidad, float recargaPorSegundo, float intervaloAsfixia)
        {
            Capacidad = Mathf.Max(0.1f, capacidad);
            RecargaPorSegundo = Mathf.Max(0f, recargaPorSegundo);
            IntervaloAsfixia = Mathf.Max(0.1f, intervaloAsfixia);
            Restante = Capacidad;
        }

        public float Capacidad { get; }
        public float RecargaPorSegundo { get; }
        public float IntervaloAsfixia { get; }

        /// <summary>Segundos de aire que quedan.</summary>
        public float Restante { get; private set; }

        /// <summary>De 0 (sin aire) a 1 (lleno).</summary>
        public float Fraccion => Restante / Capacidad;

        public bool Lleno => Restante >= Capacidad;

        /// <summary>Sin aire y bajo el agua: Kael se está ahogando.</summary>
        public bool Asfixiandose { get; private set; }

        private float _proximoCorazon;

        /// <summary>Avanza <paramref name="dt"/> segundos. Devuelve cuántos corazones se pierden en ese paso.</summary>
        public int Avanzar(float dt, bool bajoElAgua)
        {
            if (dt <= 0f)
            {
                return 0;
            }

            if (!bajoElAgua)
            {
                Restante = Mathf.Min(Capacidad, Restante + RecargaPorSegundo * dt);
                Asfixiandose = false;
                return 0;
            }

            var antes = Restante;
            Restante = Mathf.Max(0f, Restante - dt);
            if (Restante > 0f)
            {
                Asfixiandose = false;
                return 0;
            }

            var perdidos = 0;
            if (!Asfixiandose)
            {
                // El primer corazón se pierde al agotarse el aire; el siguiente cuenta desde ese momento.
                Asfixiandose = true;
                perdidos = 1;
                _proximoCorazon = IntervaloAsfixia - (dt - antes);
            }
            else
            {
                _proximoCorazon -= dt;
            }

            while (_proximoCorazon <= 0f)
            {
                perdidos++;
                _proximoCorazon += IntervaloAsfixia;
            }

            return perdidos;
        }

        /// <summary>Aire al máximo y sin asfixia (al reaparecer).</summary>
        public void Llenar()
        {
            Restante = Capacidad;
            Asfixiandose = false;
        }

        /// <summary>Deja <paramref name="segundos"/> de aire (para depurar y para las pruebas).</summary>
        public void Fijar(float segundos)
        {
            Restante = Mathf.Clamp(segundos, 0f, Capacidad);
            Asfixiandose = false;
        }
    }

    /// <summary>
    /// El oxígeno de Kael bajo el agua (Anexo A, mundo de la Tristeza): dura
    /// <see cref="capacidad"/> s y se recarga al salir del agua o en una bolsa de aire. Cuando se
    /// acaba, Kael no muere de golpe: pierde un corazón cada <see cref="intervaloAsfixia"/> s, sin
    /// retroceso (<see cref="Health.PerderVida"/>), hasta que respira o muere. Decisión del
    /// 2026-10-08, en lugar del fundido a blanco del Anexo. Dura 10 s desde el 2026-10-12 (eran
    /// 15, y al probar el nivel sobraba): cada tramo sin aire se hace en unos 8.
    ///
    /// Sabe si está bajo el agua por el motor (<see cref="PlayerMotor.BajoElAgua"/>), que lo marcan
    /// las zonas de agua. La barra del HUD escucha <see cref="OnCambio"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor))]
    public class OxigenoJugador : MonoBehaviour
    {
        [Tooltip("Segundos de aire bajo el agua.")]
        [Min(1f)] [SerializeField] private float capacidad = 10f;

        [Tooltip("Segundos de aire que se recuperan por segundo fuera del agua (los de la capacidad = lleno en 1 s).")]
        [Min(0.5f)] [SerializeField] private float recargaPorSegundo = 10f;

        [Tooltip("Sin aire, se pierde un corazón cada estos segundos.")]
        [Min(0.25f)] [SerializeField] private float intervaloAsfixia = 2f;

        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Health vida;

        private ReservaOxigeno _reserva;
        private float _ultimaFraccion = -1f;
        private bool _ultimoBajoElAgua;
        private bool _ultimaAsfixia;

        /// <summary>Cambió el aire o el medio: (fracción de aire, bajo el agua, asfixiándose).</summary>
        public event Action<float, bool, bool> OnCambio;

        public float Fraccion => Reserva.Fraccion;

        public float Restante => Reserva.Restante;

        public float Capacidad => capacidad;

        public bool Asfixiandose => Reserva.Asfixiandose;

        public bool BajoElAgua => motor != null && motor.BajoElAgua;

        private ReservaOxigeno Reserva => _reserva ?? (_reserva = new ReservaOxigeno(capacidad, recargaPorSegundo, intervaloAsfixia));

        private void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (vida == null) vida = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (vida != null)
            {
                vida.OnRevivir += Llenar;
            }
        }

        private void OnDisable()
        {
            if (vida != null)
            {
                vida.OnRevivir -= Llenar;
            }
        }

        private void Update()
        {
            var bajoElAgua = BajoElAgua && (vida == null || vida.EstaVivo);
            var perdidos = Reserva.Avanzar(Time.deltaTime, bajoElAgua);
            if (perdidos > 0 && vida != null)
            {
                vida.PerderVida(perdidos);
            }

            Avisar(bajoElAgua);
        }

        /// <summary>Aire al máximo (al reaparecer).</summary>
        public void Llenar()
        {
            Reserva.Llenar();
            Avisar(BajoElAgua);
        }

        /// <summary>Deja <paramref name="segundos"/> de aire (para depurar y para las pruebas).</summary>
        public void FijarAire(float segundos)
        {
            Reserva.Fijar(segundos);
            Avisar(BajoElAgua);
        }

        /// <summary>Vuelve a emitir el estado (la barra lo pide al engancharse).</summary>
        public void Notificar() => OnCambio?.Invoke(Reserva.Fraccion, BajoElAgua, Reserva.Asfixiandose);

        private void Avisar(bool bajoElAgua)
        {
            // La asfixia cuenta aparte: el último paso hasta 0 puede mover la fracción menos de 0,001.
            var fraccion = Reserva.Fraccion;
            var asfixia = Reserva.Asfixiandose;
            if (Mathf.Abs(fraccion - _ultimaFraccion) < 0.001f && bajoElAgua == _ultimoBajoElAgua && asfixia == _ultimaAsfixia)
            {
                return;
            }

            _ultimaFraccion = fraccion;
            _ultimoBajoElAgua = bajoElAgua;
            _ultimaAsfixia = asfixia;
            OnCambio?.Invoke(fraccion, bajoElAgua, asfixia);
        }
    }
}
