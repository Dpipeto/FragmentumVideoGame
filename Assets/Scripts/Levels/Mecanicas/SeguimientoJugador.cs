using System;
using Fragmentum.Core;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Lo que una pieza del escenario necesita de Kael: encontrarlo (por su tag, sin buscarlo en
    /// cada fotograma) y enterarse de cuándo reaparece en un checkpoint tras morir (su
    /// <c>Health.OnRevivir</c>). Lo usan las antorchas y los cepos del mundo del Miedo, que vuelven a
    /// estar listos «una vez por vida».
    /// </summary>
    internal sealed class SeguimientoJugador
    {
        private const float EsperaEntreBusquedas = 0.5f;

        private readonly Action _alReaparecer;
        private PlayerController _jugador;
        private float _proximaBusqueda;

        public SeguimientoJugador(Action alReaparecer) => _alReaparecer = alReaparecer;

        /// <summary>Kael, o null si todavía no está en la escena.</summary>
        public PlayerController Jugador
        {
            get
            {
                if (_jugador == null && Time.time >= _proximaBusqueda)
                {
                    Buscar();
                }

                return _jugador;
            }
        }

        /// <summary>Deja de escuchar a Kael (al desactivarse la pieza).</summary>
        public void Soltar()
        {
            if (!ReferenceEquals(_jugador, null) && _jugador.Vida != null)
            {
                _jugador.Vida.OnRevivir -= _alReaparecer;
            }

            _jugador = null;
            _proximaBusqueda = 0f;
        }

        private void Buscar()
        {
            Soltar();
            _proximaBusqueda = Time.time + EsperaEntreBusquedas;
            var objeto = GameObject.FindGameObjectWithTag(GameTags.Player);
            _jugador = objeto != null ? objeto.GetComponent<PlayerController>() : null;
            if (_jugador != null && _jugador.Vida != null)
            {
                _jugador.Vida.OnRevivir += _alReaparecer;
            }
        }
    }
}
