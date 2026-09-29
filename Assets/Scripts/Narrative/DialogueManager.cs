using System;
using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Core.Juego;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fragmentum.Narrative
{
    /// <summary>
    /// Diálogos internos de Kael (tarea G). Pone en cola las líneas de un momento del nivel
    /// (apertura, eco de memoria, antes del jefe) y las muestra de una en una durante el juego,
    /// sin detenerlo: son pensamientos, no conversaciones.
    ///
    /// Solo avanza mientras se está jugando: una línea pedida durante una cinemática o una pausa
    /// espera a que se vuelva a jugar. Al cambiar de escena, la cola se vacía.
    ///
    /// No dibuja nada: avisa con eventos y el HUD pinta la línea (la UI solo escucha).
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueManager : MonoBehaviour
    {
        private readonly Queue<LineaDialogo> _cola = new Queue<LineaDialogo>();
        private float _restante;

        public static DialogueManager Instancia { get; private set; }

        /// <summary>Empieza a mostrarse una línea.</summary>
        public event Action<LineaDialogo> OnLineaMostrada;

        /// <summary>La línea actual termina y no hay otra detrás.</summary>
        public event Action OnLineasTerminadas;

        public LineaDialogo LineaActual { get; private set; }

        public int Pendientes => _cola.Count;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(this);
                return;
            }

            Instancia = this;
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
                SceneManager.sceneLoaded -= AlCargarEscena;
            }
        }

        /// <summary>Añade a la cola las líneas de ese momento (ninguna si no hay datos).</summary>
        public void Reproducir(DialogueData dialogos, MOMENTOS_DIALOGO momento)
        {
            if (dialogos == null)
            {
                return;
            }

            foreach (var linea in dialogos.Del(momento))
            {
                _cola.Enqueue(linea);
            }
        }

        /// <summary>Vacía la cola y oculta la línea actual.</summary>
        public void Limpiar()
        {
            _cola.Clear();
            if (LineaActual != null)
            {
                LineaActual = null;
                OnLineasTerminadas?.Invoke();
            }
        }

        private void Update()
        {
            var juego = GameStateMachine.Instancia;
            if (juego != null && juego.EstadoActual != ESTADOS_JUEGO.PLAYING)
            {
                return;
            }

            if (LineaActual != null)
            {
                _restante -= Time.deltaTime;
                if (_restante > 0f)
                {
                    return;
                }

                LineaActual = null;
                if (_cola.Count == 0)
                {
                    OnLineasTerminadas?.Invoke();
                }
            }

            if (_cola.Count > 0)
            {
                LineaActual = _cola.Dequeue();
                _restante = LineaActual.duracion;
                OnLineaMostrada?.Invoke(LineaActual);
            }
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo) => Limpiar();
    }
}
