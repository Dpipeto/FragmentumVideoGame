using System;
using Fragmentum.Core.Combate;
using Fragmentum.Levels.Mecanicas;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Enemies
{
    /// <summary>
    /// Lo que comparten las piezas del Miedo que le quitan el mando al <see cref="EnemyController"/>
    /// para hacer algo suyo (el Espectro dormido o al borde de la luz, el Centinela Ciego cuando
    /// oye, el Acechador cuando embiste), como ya hacían la Escultura dormida y la Plañidera:
    /// - apagarlo y encenderlo (<see cref="Tomar"/>, <see cref="Devolver"/>), parando al enemigo y
    ///   cerrando su arma;
    /// - moverlo y girarlo con el controlador apagado, cuando su LateUpdate no voltea el dibujo;
    /// - encontrar a Kael: su centro, su ruido y su luz;
    /// - que muera por el camino de siempre si el golpe lo mata con el controlador apagado
    ///   (<see cref="AntesDeMorir"/>).
    ///
    /// No toca el <see cref="EnemyController"/>: usa lo que ya ofrece a sus estados.
    /// </summary>
    internal sealed class RelevoDelControlador
    {
        private readonly EnemyController _controlador;
        private readonly Health _vida;
        private readonly Transform _pivote;
        private readonly SeguimientoJugador _seguimiento = new SeguimientoJugador(null);
        private PlayerController _kael;
        private Collider2D _cuerpoKael;
        private PlayerModoMovimiento _ruidoKael;
        private LuzJugador _luzKael;
        private Action<Vector2, float> _oyente;

        public RelevoDelControlador(EnemyController controlador, Health vida, Transform pivote)
        {
            _controlador = controlador;
            _vida = vida;
            _pivote = pivote;
        }

        /// <summary>¿Lleva el mando la pieza (el controlador, apagado)?</summary>
        public bool Tomado => !_controlador.enabled;

        public int Mirando => _controlador.Mirando;

        public Vector2 Centro => _controlador.Centro;

        // --- El mando ---

        /// <summary>Apaga el controlador: el enemigo se para y su arma se cierra.</summary>
        public void Tomar()
        {
            _controlador.FijarArma(false);
            _controlador.enabled = false;
            _controlador.Detener();
        }

        /// <summary>Le devuelve el mando al controlador, que sigue donde lo dejó.</summary>
        public void Devolver() => _controlador.enabled = true;

        /// <summary>
        /// Si el golpe lo mata con el controlador apagado, lo enciende ya: <see cref="Health"/> avisa
        /// de la muerte justo después del golpe, y así el controlador la oye y muere por su camino.
        /// Solo vale con el controlador ya arrancado (no para un Espectro que no ha despertado nunca).
        /// </summary>
        public void AntesDeMorir()
        {
            if (!_vida.EstaVivo && Tomado)
            {
                _controlador.enabled = true;
            }
        }

        public void Mirar(int direccion)
        {
            _controlador.Mirar(direccion);
            Voltear();
        }

        /// <summary>Avanza en horizontal; false si un borde o una pared se lo impiden.</summary>
        public bool Mover(int direccion, float velocidad)
        {
            var pudo = _controlador.Mover(direccion, velocidad);
            Voltear();
            return pudo;
        }

        public void Detener() => _controlador.Detener();

        public void FijarArma(bool activa) => _controlador.FijarArma(activa);

        public bool Bloqueado(int direccion) => _controlador.Bloqueado(direccion);

        /// <summary>Voltea el dibujo hacia donde mira, como hace el controlador en su LateUpdate.</summary>
        public void Voltear()
        {
            if (_pivote == null)
            {
                return;
            }

            var derecha = _controlador.Datos == null || _controlador.Datos.spriteMiraDerecha;
            var escala = _pivote.localScale;
            escala.x = Mathf.Abs(escala.x) * _controlador.Mirando * (derecha ? 1 : -1);
            _pivote.localScale = escala;
        }

        // --- Kael ---

        /// <summary>Kael, o null si todavía no está en la escena.</summary>
        public PlayerController Kael
        {
            get
            {
                var jugador = _seguimiento.Jugador;
                if (jugador != _kael)
                {
                    SoltarRuido();
                    _kael = jugador;
                    _cuerpoKael = jugador != null ? jugador.GetComponent<Collider2D>() : null;
                    _ruidoKael = jugador != null ? jugador.GetComponent<PlayerModoMovimiento>() : null;
                    _luzKael = jugador != null ? jugador.GetComponentInChildren<LuzJugador>(true) : null;
                    if (_ruidoKael != null && _oyente != null)
                    {
                        _ruidoKael.OnRuido += _oyente;
                    }
                }

                return _kael;
            }
        }

        public bool KaelVivo => Kael != null && _kael.EstaVivo;

        public Vector2 CentroDeKael =>
            Kael == null ? Centro
            : _cuerpoKael != null ? (Vector2)_cuerpoKael.bounds.center
            : (Vector2)_kael.transform.position + Vector2.up * 0.9f;

        /// <summary>La luz propia de Kael, encendida solo en los niveles a oscuras.</summary>
        public LuzJugador LuzDeKael => Kael != null ? _luzKael : null;

        /// <summary>¿Está el nivel a oscuras? Lo dice la luz de Kael, que el LevelManager solo enciende entonces.</summary>
        public bool HayOscuridad => LuzDeKael != null && _luzKael.Encendida;

        /// <summary>
        /// Escucha el ruido de Kael (correr, el chasquido de un cepo). Se vuelve a suscribir solo si
        /// Kael cambia; llámalo en cada Update.
        /// </summary>
        public void Escuchar(Action<Vector2, float> oyente)
        {
            if (_oyente != oyente)
            {
                SoltarRuido();
                _oyente = oyente;
                if (_ruidoKael != null)
                {
                    _ruidoKael.OnRuido += _oyente;
                }
            }

            _ = Kael;
        }

        /// <summary>Deja de escuchar a Kael (al desactivarse la pieza).</summary>
        public void Soltar()
        {
            SoltarRuido();
            _oyente = null;
            _seguimiento.Soltar();
            _kael = null;
        }

        private void SoltarRuido()
        {
            if (_ruidoKael != null && _oyente != null)
            {
                _ruidoKael.OnRuido -= _oyente;
            }
        }
    }
}
