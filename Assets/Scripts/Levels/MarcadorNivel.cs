using System;
using System.Collections.Generic;
using Fragmentum.Bosses;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Core.Juego;
using Fragmentum.Core.Puntuacion;
using Fragmentum.Core.Save;
using Fragmentum.Data;
using Fragmentum.Enemies;
using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// El marcador de un nivel: los puntos de los enemigos y del jefe, y el reloj. Uno por
    /// escena, junto al <see cref="LevelManager"/>, que lo crea si la escena no lo trae.
    ///
    /// - <b>Enemigos:</b> escucha la muerte de cada enemigo de la escena (su <c>Health</c>; el
    ///   <c>EnemyController</c> no sabe que existe el puntaje) y suma lo que vale su
    ///   <c>EnemyData</c>. Cada enemigo cuenta una sola vez por intento.
    /// - <b>Jefe:</b> suma lo que vale su <c>JefeData</c> cuando el LevelManager lo da por caído.
    /// - <b>Reloj:</b> corre solo mientras se juega: ni en pausa, ni en las cinemáticas, ni en el
    ///   Game Over. Morir no lo detiene ni lo reinicia: el tiempo perdido es la penalización.
    /// - <b>Checkpoints:</b> al activar uno se guarda una foto del intento (puntos, reloj y a quién
    ///   se derrotó). Si se sale y se continúa desde ese checkpoint, el intento sigue donde iba:
    ///   salir no pone el reloj a cero ni deja cobrar dos veces al mismo enemigo.
    /// - <b>Cierre:</b> al cruzar la salida añade el bono por tiempo, lo compara con el mejor
    ///   puntaje guardado del nivel y lo deja en la partida.
    ///
    /// Avisa por <see cref="EventosJuego"/>; el HUD no lo conoce.
    /// </summary>
    [DisallowMultipleComponent]
    public class MarcadorNivel : MonoBehaviour
    {
        private readonly HashSet<string> _derrotados = new HashSet<string>();
        private readonly List<(Health vida, Action alMorir)> _enganches = new List<(Health, Action)>();
        private LevelData _nivel;
        private LevelManager _gestor;
        private CheckpointManager _checkpoints;
        private int _segundoAvisado = -1;
        private bool _jefeContado;

        /// <summary>El marcador de la escena actual, o null fuera de los niveles.</summary>
        public static MarcadorNivel Actual { get; private set; }

        public int PuntosEnemigos { get; private set; }

        public int PuntosJefe { get; private set; }

        /// <summary>Lo ganado hasta ahora en el intento, sin el bono por tiempo.</summary>
        public int Puntos => PuntosEnemigos + PuntosJefe;

        /// <summary>Segundos jugados en este intento.</summary>
        public float Segundos { get; private set; }

        /// <summary>Enemigos derrotados en el intento (también los que no dan puntos).</summary>
        public int Derrotados => _derrotados.Count;

        /// <summary>¿Se cruzó ya la salida? Desde entonces ni suma ni corre.</summary>
        public bool Cerrado { get; private set; }

        /// <summary>El resultado, válido desde <see cref="Cerrar"/>.</summary>
        public ResultadoNivel Resultado { get; private set; }

        // El reloj solo corre jugando. Sin FSM global (escenas de prueba) corre siempre.
        private static bool SeJuega =>
            GameStateMachine.Instancia == null || GameStateMachine.Instancia.EstadoActual == ESTADOS_JUEGO.PLAYING;

        private static DatosGuardado Datos => GameManager.Instancia != null ? GameManager.Instancia.Datos : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Actual = null;

        private void OnEnable() => Actual = this;

        private void OnDisable()
        {
            if (Actual == this)
            {
                Actual = null;
            }
        }

        // En Start todos los enemigos de la escena ya pasaron por su Awake.
        private void Start()
        {
            foreach (var enemigo in FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
            {
                if (enemigo.gameObject.scene == gameObject.scene)
                {
                    Seguir(enemigo);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var (vida, alMorir) in _enganches)
            {
                if (vida != null)
                {
                    vida.OnMuerte -= alMorir;
                }
            }

            _enganches.Clear();
            Soltar();
        }

        private void Update()
        {
            if (Cerrado || !SeJuega)
            {
                return;
            }

            Segundos += Time.deltaTime;
            AvisarDelReloj();
        }

        // --- API ---

        /// <summary>
        /// Pone el marcador a cero o, si se continúa desde un checkpoint guardado de este nivel,
        /// lo deja como estaba al activarlo. Lo llama el LevelManager al cargar la escena.
        /// </summary>
        public void Empezar(LevelData nivel, LevelManager gestor, bool desdeCheckpoint)
        {
            Soltar();
            _nivel = nivel;
            _gestor = gestor;
            _checkpoints = gestor != null ? gestor.Checkpoints : null;
            if (_gestor != null)
            {
                _gestor.OnJefeDerrotado += AlDerrotarJefe;
            }

            if (_checkpoints != null)
            {
                _checkpoints.OnCheckpointActivado += AlActivarCheckpoint;
            }

            _derrotados.Clear();
            PuntosEnemigos = 0;
            PuntosJefe = 0;
            Segundos = 0f;
            _jefeContado = false;
            Cerrado = false;

            var datos = Datos;
            var foto = datos != null ? datos.intento : null;
            if (desdeCheckpoint && nivel != null && foto != null && foto.nivelId == nivel.id)
            {
                PuntosEnemigos = Mathf.Max(0, foto.puntos);
                Segundos = Mathf.Max(0f, foto.segundos);
                _derrotados.UnionWith(foto.derrotados);
            }
            else if (datos != null && nivel != null)
            {
                datos.intento = new DatosIntento();
            }

            _segundoAvisado = -1;
            EventosJuego.NotificarPuntaje(Puntos, 0);
            AvisarDelReloj();
        }

        /// <summary>Empieza a escuchar a un enemigo. Los de la escena se siguen solos al empezar.</summary>
        public void Seguir(EnemyController enemigo)
        {
            // Su Health, y no enemigo.Vida: un enemigo inactivo aún no ha pasado por su Awake.
            var vida = enemigo != null ? enemigo.GetComponent<Health>() : null;
            if (vida == null)
            {
                return;
            }

            var id = Identificar(enemigo);
            Action alMorir = () => AlMorirEnemigo(id, enemigo);
            vida.OnMuerte += alMorir;
            _enganches.Add((vida, alMorir));
        }

        /// <summary>
        /// Cruzada la salida: suma el bono por tiempo y anota el puntaje en la partida si mejora
        /// al guardado. Solo cuenta la primera vez; después devuelve el mismo resultado.
        /// </summary>
        public ResultadoNivel Cerrar()
        {
            if (Cerrado)
            {
                return Resultado;
            }

            Cerrado = true;
            var bono = _nivel != null ? ReglasPuntaje.BonoDeTiempo(Segundos, _nivel.tiempoConBono, _nivel.puntosPorSegundo) : 0;
            var datos = Datos;
            var mejor = datos != null && _nivel != null ? datos.PuntajeDe(_nivel.id) : null;
            Resultado = new ResultadoNivel(PuntosEnemigos, PuntosJefe, bono, Segundos, mejor != null ? mejor.puntos : 0, mejor != null);

            if (datos != null && _nivel != null)
            {
                datos.AnotarPuntaje(_nivel.id, Resultado.Total, Segundos);
                datos.intento = new DatosIntento();
            }

            return Resultado;
        }

        // --- Sumar ---

        private void AlMorirEnemigo(string id, EnemyController enemigo)
        {
            if (Cerrado || !_derrotados.Add(id))
            {
                return;
            }

            var puntos = enemigo != null && enemigo.Datos != null ? enemigo.Datos.puntos : 0;
            if (puntos <= 0)
            {
                return;
            }

            PuntosEnemigos += puntos;
            EventosJuego.NotificarPuntaje(Puntos, puntos);
        }

        private void AlDerrotarJefe(BossController jefe)
        {
            if (Cerrado || _jefeContado)
            {
                return;
            }

            _jefeContado = true;
            var puntos = jefe != null && jefe.Datos != null ? jefe.Datos.puntos : 0;
            if (puntos <= 0)
            {
                return;
            }

            PuntosJefe += puntos;
            EventosJuego.NotificarPuntaje(Puntos, puntos);
        }

        // La foto del intento en el checkpoint. El GameManager acaba de guardar el checkpoint
        // (CheckpointManager.Activar); se guarda otra vez para que la foto vaya con él.
        private void AlActivarCheckpoint(Checkpoint checkpoint)
        {
            var gestor = GameManager.Instancia;
            if (gestor == null || _nivel == null || Cerrado)
            {
                return;
            }

            gestor.Datos.intento = new DatosIntento
            {
                nivelId = _nivel.id,
                puntos = PuntosEnemigos,
                segundos = Segundos,
                derrotados = new List<string>(_derrotados)
            };
            gestor.Guardar();
        }

        private void AvisarDelReloj()
        {
            var entero = Mathf.FloorToInt(Segundos);
            if (entero != _segundoAvisado)
            {
                _segundoAvisado = entero;
                EventosJuego.NotificarTiempo(entero);
            }
        }

        private void Soltar()
        {
            if (_gestor != null)
            {
                _gestor.OnJefeDerrotado -= AlDerrotarJefe;
            }

            if (_checkpoints != null)
            {
                _checkpoints.OnCheckpointActivado -= AlActivarCheckpoint;
            }

            _gestor = null;
            _checkpoints = null;
        }

        // El nombre y el sitio donde empieza: lo mismo cada vez que se carga la escena, así que
        // la foto del checkpoint reconoce al enemigo aunque el objeto sea otro.
        private static string Identificar(EnemyController enemigo)
        {
            var posicion = enemigo.transform.position;
            return $"{enemigo.name}@{Mathf.RoundToInt(posicion.x)},{Mathf.RoundToInt(posicion.y)}";
        }
    }
}
