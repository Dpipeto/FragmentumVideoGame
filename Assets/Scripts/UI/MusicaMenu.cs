using Fragmentum.Core;
using Fragmentum.Core.Juego;
using UnityEngine;

namespace Fragmentum.UI
{
    /// <summary>
    /// La música del menú inicial y del selector de niveles: la misma en los dos, y sin cortarse al pasar
    /// de uno a otro, porque vive con los sistemas (la crea el <see cref="UIManager"/>). Suena en
    /// <c>MAIN_MENU</c> y <c>LEVEL_SELECT</c> (y en la carga entre ellos); al entrar en un nivel o en una
    /// cinemática se apaga, y al volver al menú empieza otra vez. El clip es el de la
    /// <c>LevelDatabase</c> (<c>musicaMenu</c>); su volumen, el de la música de las opciones.
    /// </summary>
    [DisallowMultipleComponent]
    public class MusicaMenu : MonoBehaviour
    {
        /// <summary>A qué volumen suena (antes del de las opciones): como la música de los niveles.</summary>
        public const float Volumen = 0.6f;

        /// <summary>Lo que tarda en apagarse o en volver.</summary>
        public const float Fundido = 0.8f;

        private AudioSource _fuente;
        private bool _quiere;

        public AudioSource Fuente => _fuente;

        /// <summary>¿Está sonando (o empezando a sonar)?</summary>
        public bool Sonando { get; private set; }

        /// <summary>De 0 (callada) a 1.</summary>
        public float Nivel { get; private set; }

        private void Awake()
        {
            _fuente = gameObject.AddComponent<AudioSource>();
            _fuente.loop = true;
            _fuente.playOnAwake = false;
            _fuente.ignoreListenerPause = true;
            _fuente.volume = 0f;
        }

        private void Update()
        {
            var juego = GameStateMachine.Instancia;
            var estado = juego != null ? juego.EstadoActual : ESTADOS_JUEGO.MAIN_MENU;
            if (estado == ESTADOS_JUEGO.MAIN_MENU || estado == ESTADOS_JUEGO.LEVEL_SELECT)
            {
                _quiere = true;
            }
            else if (estado != ESTADOS_JUEGO.LOADING && estado != ESTADOS_JUEGO.BOOT)
            {
                _quiere = false;
            }

            var clip = Clip;
            if (_quiere && !Sonando && clip != null)
            {
                _fuente.clip = clip;
                _fuente.Play();
                Sonando = true;
            }

            Nivel = Mathf.MoveTowards(Nivel, _quiere ? 1f : 0f, Time.unscaledDeltaTime / Fundido);
            if (!_quiere && Sonando && Nivel <= 0f)
            {
                _fuente.Stop();
                Sonando = false;
            }

            _fuente.volume = Volumen * AjustesAudio.Musica * Nivel;
        }

        private static AudioClip Clip
        {
            get
            {
                var gestor = GameManager.Instancia;
                return gestor != null && gestor.BaseDeDatos != null ? gestor.BaseDeDatos.musicaMenu : null;
            }
        }
    }
}
