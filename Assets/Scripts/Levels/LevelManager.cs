using System;
using System.Collections;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.Levels.Escenario;
using Fragmentum.Narrative;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// Uno por escena de nivel (tarea D5). Aplica al nivel su configuración de
    /// <see cref="LevelData"/>, hace aparecer al jugador, lo hace reaparecer en el checkpoint y
    /// declara la victoria.
    ///
    /// - <b>Aparición:</b> usa el jugador que ya haya en la escena o instancia el prefab. Si la
    ///   partida guardada tiene un checkpoint de este nivel, empieza ahí.
    /// - <b>Configuración:</b> fondo de cámara con la paleta, límites de cámara desde las
    ///   secciones del Anexo A y música del nivel. Lo que falte (música, prefab) se avisa por
    ///   consola y el nivel sigue funcionando.
    /// - <b>Reaparición:</b> tras un Game Over, la FSM global pide reintentar
    ///   (<see cref="EventosJuego.ReintentoSolicitado"/>) y el jugador vuelve al checkpoint
    ///   activo. Fuera de los niveles del juego (escenas de prueba) no hay Game Over: el jugador
    ///   reaparece solo tras una espera. Sustituye a la <c>ReaparicionSimple</c> provisional.
    /// - <b>Victoria:</b> <see cref="Completar"/>, que llama la <see cref="SalidaNivel"/>. La FSM
    ///   global muestra el fin de nivel. En los niveles con jefe, la salida solo se abre al
    ///   derrotarlo, y el fragmento y la habilidad llegan en ese momento (<see cref="JefeDerrotado"/>).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CheckpointManager))]
    public class LevelManager : MonoBehaviour
    {
        [Tooltip("Vacío = el nivel de la LevelDatabase que usa esta escena.")]
        [SerializeField] private LevelData nivel;

        [Header("Jugador")]
        [Tooltip("Se instancia si la escena no tiene ya un jugador (tag Player).")]
        [SerializeField] private GameObject prefabJugador;

        [Tooltip("Inicio del nivel. Vacío = donde esté el jugador de la escena, o este objeto.")]
        [SerializeField] private Transform puntoInicio;

        [Tooltip("Fuera de los niveles del juego (escenas de prueba) no hay Game Over: el jugador reaparece solo tras esta espera.")]
        [SerializeField] private float esperaReaparicion = 1.2f;

        [Header("Cámara")]
        [Tooltip("Ajusta los límites horizontales de la cámara al tramo de las secciones del nivel.")]
        [SerializeField] private bool limitesDesdeSecciones = true;

        [SerializeField] private float margenCamara = 6f;

        [Tooltip("Límites verticales (mínimo, máximo) cuando se ajusta desde las secciones. Anexo A: altura máxima 20 u.")]
        [SerializeField] private Vector2 alturaCamara = new Vector2(-6f, 24f);

        [Tooltip("Pinta el fondo de la cámara con el color más oscuro de la paleta del nivel.")]
        [SerializeField] private bool fondoDesdePaleta = true;

        [Header("Música")]
        [Tooltip("Vacío = se crea al haber música que tocar.")]
        [SerializeField] private AudioSource fuenteMusica;

        [Range(0f, 1f)]
        [SerializeField] private float volumenMusica = 0.6f;

        private CheckpointManager _checkpoints;
        private PlayerController _jugador;
        private CameraMovement _camara;
        private bool _completado;

        /// <summary>El jugador reapareció (tras un Game Over o, fuera de los niveles, tras morir).</summary>
        public event Action<PlayerController> OnJugadorReaparecio;

        /// <summary>Se declaró la victoria del nivel.</summary>
        public event Action OnNivelCompletado;

        /// <summary>El jefe del nivel fue derrotado (ya se otorgó su fragmento y su habilidad).</summary>
        public event Action<BossController> OnJefeDerrotado;

        public LevelData Nivel => nivel;

        public PlayerController Jugador => _jugador;

        public CheckpointManager Checkpoints => _checkpoints;

        public bool Completado => _completado;

        // En un nivel del juego la muerte la lleva la FSM global (Game Over → reintentar).
        private static bool LaMuerteEsDelJuego =>
            GameStateMachine.Instancia != null && GameManager.Instancia != null && GameManager.Instancia.NivelActual != null;

        private void Awake()
        {
            _checkpoints = GetComponent<CheckpointManager>();
            _checkpoints.Inicializar();

            if (nivel == null && GameManager.Instancia != null && GameManager.Instancia.BaseDeDatos != null)
            {
                nivel = GameManager.Instancia.BaseDeDatos.PorEscena(gameObject.scene.name);
            }

            PrepararJugador();
            PrepararCamara();
            PrepararMusica();
        }

        private void OnEnable() => EventosJuego.ReintentoSolicitado += Reaparecer;

        private void OnDisable() => EventosJuego.ReintentoSolicitado -= Reaparecer;

        // En Start todas las cámaras ya pasaron por su Awake: se encuadra sin suavizado.
        private void Start()
        {
            if (_camara != null)
            {
                _camara.Encuadrar();
            }

            // Las líneas de apertura esperan a que se juegue (tras la cinemática de entrada, si la hay).
            if (nivel != null && DialogueManager.Instancia != null)
            {
                DialogueManager.Instancia.Reproducir(nivel.dialogos, MOMENTOS_DIALOGO.APERTURA);
            }
        }

        private void OnDestroy()
        {
            if (_jugador != null && _jugador.Vida != null)
            {
                _jugador.Vida.OnMuerte -= AlMorir;
            }
        }

        // --- API ---

        /// <summary>Devuelve al jugador, vivo, al checkpoint activo (o al inicio del nivel).</summary>
        public void Reaparecer()
        {
            if (_jugador == null)
            {
                return;
            }

            _jugador.Revivir(_checkpoints.PuntoReaparicion);
            if (_camara != null)
            {
                _camara.Encuadrar();
            }

            OnJugadorReaparecio?.Invoke(_jugador);
        }

        /// <summary>
        /// Victoria: pide a la FSM global que complete el nivel. Solo cuenta una vez. Devuelve
        /// false fuera de los niveles del juego (escenas de prueba), donde no hay fin de nivel.
        /// </summary>
        public bool Completar()
        {
            if (_completado)
            {
                return false;
            }

            var juego = GameStateMachine.Instancia;
            if (juego == null || GameManager.Instancia == null || GameManager.Instancia.NivelActual == null)
            {
                Debug.Log("[Nivel] Salida alcanzada. Esta escena no es un nivel de la LevelDatabase, así que no hay fin de nivel.", this);
                return false;
            }

            if (!juego.CompletarNivel())
            {
                return false;
            }

            _completado = true;
            OnNivelCompletado?.Invoke();
            return true;
        }

        /// <summary>
        /// El jefe cayó: otorga ya el fragmento y la habilidad del nivel (tarea C). La victoria
        /// llega después, al cruzar la salida que la arena abre.
        /// </summary>
        public void JefeDerrotado(BossController jefe)
        {
            var nivelActual = nivel != null ? nivel : GameManager.Instancia != null ? GameManager.Instancia.NivelActual : null;
            if (GameManager.Instancia != null)
            {
                GameManager.Instancia.OtorgarRecompensa(nivelActual);
            }

            OnJefeDerrotado?.Invoke(jefe);
        }

        /// <summary>Cambia entre la música del nivel y la del jefe (<c>LevelData.musicaJefe</c>), si existen.</summary>
        public void MusicaDeJefe(bool enCombate)
        {
            if (nivel == null)
            {
                return;
            }

            var clip = enCombate && nivel.musicaJefe != null ? nivel.musicaJefe : nivel.musica;
            if (clip == null)
            {
                return;
            }

            if (fuenteMusica == null)
            {
                fuenteMusica = gameObject.AddComponent<AudioSource>();
                fuenteMusica.loop = true;
                fuenteMusica.playOnAwake = false;
                fuenteMusica.volume = volumenMusica;
            }

            if (fuenteMusica.clip != clip)
            {
                fuenteMusica.clip = clip;
                fuenteMusica.Play();
            }
        }

        // --- Preparación ---

        private void PrepararJugador()
        {
            _jugador = PlayerController.BuscarEnEscena(gameObject.scene);

            _checkpoints.PuntoInicio = puntoInicio != null ? (Vector2)puntoInicio.position
                : _jugador != null ? (Vector2)_jugador.transform.position
                : (Vector2)transform.position;

            // Partida guardada con un checkpoint de este nivel: se continúa desde él.
            var guardado = GameManager.Instancia != null ? GameManager.Instancia.CheckpointDe(nivel) : null;
            if (guardado != null)
            {
                _checkpoints.Restaurar(guardado.checkpointId);
            }

            var aparicion = _checkpoints.PuntoReaparicion;
            if (_jugador == null)
            {
                if (prefabJugador == null)
                {
                    Debug.LogWarning("[Nivel] La escena no tiene jugador y el LevelManager no tiene prefab del jugador que instanciar. Asigna Prefabs/Player.prefab.", this);
                    return;
                }

                _jugador = Instantiate(prefabJugador, aparicion, Quaternion.identity).GetComponent<PlayerController>();
                if (_jugador == null)
                {
                    Debug.LogError("[Nivel] El prefab del jugador no tiene PlayerController.", this);
                    return;
                }
            }
            else if (_checkpoints.Activo != null)
            {
                _jugador.transform.position = aparicion;
            }

            if (_jugador.Vida != null)
            {
                _jugador.Vida.OnMuerte += AlMorir;
            }
        }

        private void PrepararCamara()
        {
            var camara = Camera.main;
            if (camara == null)
            {
                Debug.LogWarning("[Nivel] No hay cámara con el tag MainCamera: la cámara no seguirá al jugador.", this);
                return;
            }

            _camara = camara.GetComponent<CameraMovement>();
            if (_camara == null)
            {
                _camara = camara.gameObject.AddComponent<CameraMovement>();
            }

            if (_jugador != null)
            {
                _camara.FijarObjetivo(_jugador.transform);
            }

            if (nivel == null)
            {
                return;
            }

            if (fondoDesdePaleta && nivel.paleta != null && nivel.paleta.Length > 0)
            {
                camara.backgroundColor = nivel.ColorOscuro;
            }

            var tramo = nivel.Extension;
            if (limitesDesdeSecciones && tramo.y > tramo.x)
            {
                _camara.FijarLimites(new Vector2(tramo.x - margenCamara, alturaCamara.x), new Vector2(tramo.y + margenCamara, alturaCamara.y));
            }
        }

        private void PrepararMusica()
        {
            if (nivel == null)
            {
                return;
            }

            if (nivel.musica == null)
            {
                Debug.LogWarning($"[Nivel] '{nivel.Titulo}' no tiene música asignada en su LevelData (PENDIENTES B8): el nivel suena en silencio.", this);
                return;
            }

            if (fuenteMusica == null)
            {
                fuenteMusica = gameObject.AddComponent<AudioSource>();
            }

            fuenteMusica.clip = nivel.musica;
            fuenteMusica.loop = true;
            fuenteMusica.playOnAwake = false;
            fuenteMusica.volume = volumenMusica;
            fuenteMusica.Play();
        }

        // --- Muerte fuera de los niveles del juego ---

        private void AlMorir()
        {
            if (!LaMuerteEsDelJuego && isActiveAndEnabled)
            {
                StartCoroutine(ReaparecerTrasEspera());
            }
        }

        private IEnumerator ReaparecerTrasEspera()
        {
            yield return new WaitForSeconds(esperaReaparicion);
            if (_jugador != null && !_jugador.EstaVivo)
            {
                Reaparecer();
            }
        }

        private void OnDrawGizmosSelected()
        {
            var inicio = puntoInicio != null ? puntoInicio.position : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(inicio, 0.5f);

            if (nivel == null || nivel.secciones == null)
            {
                return;
            }

            // Las secciones del Anexo A, como guía para pintar el layout.
            foreach (var seccion in nivel.secciones)
            {
                Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
                var centro = new Vector3((seccion.desdeX + seccion.hastaX) * 0.5f, 10f, 0f);
                Gizmos.DrawWireCube(centro, new Vector3(seccion.hastaX - seccion.desdeX, 20f, 0f));
            }
        }
    }
}
