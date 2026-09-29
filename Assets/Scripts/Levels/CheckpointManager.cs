using System;
using System.Collections.Generic;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// Lleva los checkpoints de un nivel: cuál está activo, dónde reaparece el jugador y el
    /// guardado de la partida al activarlos (tarea D8). Va junto al <see cref="LevelManager"/>,
    /// que le pide el punto de reaparición tras un Game Over y al empezar el nivel.
    ///
    /// Encuentra solo los checkpoints de su escena; no hace falta registrarlos a mano.
    /// </summary>
    [DisallowMultipleComponent]
    public class CheckpointManager : MonoBehaviour
    {
        private readonly List<Checkpoint> _checkpoints = new List<Checkpoint>();
        private bool _inicializado;

        /// <summary>Se activó un checkpoint (el jugador lo tocó).</summary>
        public event Action<Checkpoint> OnCheckpointActivado;

        /// <summary>El checkpoint activo, o null si el jugador aún no ha tocado ninguno.</summary>
        public Checkpoint Activo { get; private set; }

        /// <summary>Inicio del nivel: donde reaparece el jugador si no hay checkpoint activo.</summary>
        public Vector2 PuntoInicio { get; set; }

        public Vector2 PuntoReaparicion => Activo != null ? Activo.PuntoReaparicion : PuntoInicio;

        public IReadOnlyList<Checkpoint> Checkpoints
        {
            get
            {
                Inicializar();
                return _checkpoints;
            }
        }

        private void Awake() => Inicializar();

        /// <summary>Busca los checkpoints de la escena. Se puede llamar varias veces.</summary>
        public void Inicializar()
        {
            if (_inicializado)
            {
                return;
            }

            _inicializado = true;
            foreach (var checkpoint in FindObjectsByType<Checkpoint>(FindObjectsInactive.Include))
            {
                if (checkpoint.gameObject.scene == gameObject.scene)
                {
                    Registrar(checkpoint);
                }
            }
        }

        /// <summary>Añade un checkpoint (por ejemplo, uno creado durante la partida).</summary>
        public void Registrar(Checkpoint checkpoint)
        {
            if (checkpoint == null || _checkpoints.Contains(checkpoint))
            {
                return;
            }

            var repetido = Buscar(checkpoint.Id);
            if (repetido != null)
            {
                Debug.LogWarning($"[Checkpoints] Hay dos checkpoints con el id '{checkpoint.Id}' ({repetido.name} y {checkpoint.name}). Dales ids distintos: el guardado no sabría a cuál volver.", checkpoint);
            }

            _checkpoints.Add(checkpoint);
            checkpoint.Vincular(this);
            checkpoint.MarcarActivo(checkpoint == Activo);
        }

        public Checkpoint Buscar(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var checkpoint in _checkpoints)
            {
                if (checkpoint != null && checkpoint.Id == id)
                {
                    return checkpoint;
                }
            }

            return null;
        }

        /// <summary>
        /// El jugador tocó el checkpoint: pasa a ser el activo y se guarda la partida. Tocar el
        /// que ya está activo no hace nada.
        /// </summary>
        public void Activar(Checkpoint checkpoint)
        {
            if (checkpoint == null || checkpoint == Activo)
            {
                return;
            }

            Registrar(checkpoint);
            Fijar(checkpoint);

            if (GameManager.Instancia != null)
            {
                GameManager.Instancia.RegistrarCheckpoint(checkpoint.Id);
            }

            EventosJuego.NotificarCheckpoint(checkpoint.Id);
            OnCheckpointActivado?.Invoke(checkpoint);
        }

        /// <summary>
        /// Vuelve a activar el checkpoint de una partida guardada, sin guardar otra vez.
        /// Devuelve false (y avisa) si ya no existe en la escena.
        /// </summary>
        public bool Restaurar(string id)
        {
            Inicializar();
            var checkpoint = Buscar(id);
            if (checkpoint == null)
            {
                Debug.LogWarning($"[Checkpoints] El checkpoint guardado '{id}' ya no existe en la escena: se empieza desde el inicio del nivel.", this);
                return false;
            }

            Fijar(checkpoint);
            return true;
        }

        private void Fijar(Checkpoint checkpoint)
        {
            if (Activo != null)
            {
                Activo.MarcarActivo(false);
            }

            Activo = checkpoint;
            checkpoint.MarcarActivo(true);
        }
    }
}
