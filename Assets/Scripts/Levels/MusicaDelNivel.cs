using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// La música de un nivel, que lleva el <see cref="LevelManager"/>: una fuente en bucle que funde.
    /// - <b>Calla en las cinemáticas</b> y, al volver, sigue donde iba. En la Culpa el Anexo A pide
    ///   silencio en las memorias, que son cinemáticas; y las cinemáticas traen su propio sonido.
    /// - <b>Cambia sin cortes</b> a la del jefe y de vuelta: la que suena se apaga y la nueva empieza
    ///   desde el principio. Si se pide justo al acabar una cinemática (la presentación del jefe), entra de
    ///   golpe: la cinemática ya la ha preparado.
    /// El tiempo lo da quien la lleva (<see cref="Avanzar"/>), sin escalar: las cinemáticas paran el juego.
    /// </summary>
    public sealed class MusicaDelNivel
    {
        private readonly AudioSource _fuente;

        // Se está callando (o ya calla) por una cinemática, y aún no ha vuelto a su volumen.
        private bool _calladaPorCinematica;

        public MusicaDelNivel(AudioSource fuente)
        {
            _fuente = fuente;
            _fuente.loop = true;
            _fuente.playOnAwake = false;
        }

        public AudioSource Fuente => _fuente;

        /// <summary>La que suena o va a sonar en cuanto se apague la de antes.</summary>
        public AudioClip Pedida { get; private set; }

        /// <summary>De 0 (callada) a 1 (a su volumen).</summary>
        public float Fundido { get; private set; } = 1f;

        /// <summary>Callada y parada por una cinemática: al volver, sigue por donde iba.</summary>
        public bool EnPausa { get; private set; }

        /// <summary>Empieza a sonar ya, a su volumen.</summary>
        public void Empezar(AudioClip clip, float volumen)
        {
            Pedida = clip;
            _fuente.clip = clip;
            _fuente.volume = volumen;
            _fuente.Play();
            Fundido = 1f;
            EnPausa = false;
        }

        /// <summary>Pide otra: la que suena se apaga y luego entra esta. Si no sonaba nada, entra fundiendo.</summary>
        public void Pedir(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (_fuente.clip == null)
            {
                Fundido = 0f;
            }

            Pedida = clip;
        }

        /// <summary>Un paso del fundido: callar = hay una cinemática; duracion, lo que tarda en apagarse o volver.</summary>
        public void Avanzar(float segundos, bool callar, float volumen, float duracion)
        {
            if (Pedida == null)
            {
                return;
            }

            if (callar)
            {
                _calladaPorCinematica = true;
            }

            // Lo que se pide al acabar una cinemática entra ya, aunque la de antes no se hubiera apagado del
            // todo (la presentación del jefe se puede saltar enseguida).
            var cambiar = _fuente.clip != Pedida;
            var deGolpe = cambiar && _calladaPorCinematica && !callar;
            var paso = duracion > 0f ? segundos / duracion : 1f;
            Fundido = deGolpe ? 0f : Mathf.MoveTowards(Fundido, callar || cambiar ? 0f : 1f, paso);

            if (Fundido <= 0f)
            {
                if (cambiar)
                {
                    _fuente.clip = Pedida;
                    _fuente.Play();
                    EnPausa = false;
                    if (deGolpe)
                    {
                        Fundido = 1f;
                        _calladaPorCinematica = false;
                    }
                }

                if (callar && !EnPausa)
                {
                    _fuente.Pause();
                    EnPausa = true;
                }
            }

            if (!callar && EnPausa)
            {
                _fuente.UnPause();
                EnPausa = false;
            }

            if (!callar && Fundido >= 1f)
            {
                _calladaPorCinematica = false;
            }

            _fuente.volume = volumen * Fundido;
        }
    }
}
