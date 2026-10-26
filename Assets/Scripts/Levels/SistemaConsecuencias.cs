using System;
using System.Collections;
using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Core.Juego;
using Fragmentum.Data;
using Fragmentum.Enemies;
using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// El sistema de consecuencias del mundo de la Culpa (Anexo A): mirar cuesta, pero no mirar cuesta
    /// más. Es la tesis hecha regla de juego.
    ///
    /// - <b>Al recordar una memoria</b> (<see cref="MemoriaInteractuable.AlRecordar"/>), los enemigos
    ///   que están en su zona se vuelven un <see cref="agresividad"/> más agresivos: se mueven y atacan
    ///   un 20 % más deprisa. Cada uno toma una copia de sus datos con los valores subidos
    ///   (<see cref="Agravados"/>), así que <c>EnemyController</c> no cambia; lo que lleva su propia
    ///   pieza (la recarga del Artillero) lo sube ella (<see cref="IAgravable"/>). Al terminar la
    ///   cinemática, destellan en rojo apagado: así se ve qué lo causó.
    /// - <b>Si al llegar al Juez no se ha recordado ninguna</b> (<see cref="NingunaRecordada"/>), el
    ///   Juez tiene un <see cref="vidaDelJuezSinMemorias"/> más de vida: lo aplica el Juez.
    ///
    /// Las memorias cuentan en cada partida del nivel: al empezarlo otra vez, la escena se carga de
    /// nuevo y se olvidan.
    /// </summary>
    [DisallowMultipleComponent]
    public class SistemaConsecuencias : MonoBehaviour
    {
        [Tooltip("Cuántas veces más deprisa van los enemigos de la zona de una memoria (Anexo A: un 20 % más).")]
        [Min(1f)] public float agresividad = 1.2f;

        [Tooltip("La vida del Juez si no se recordó ninguna memoria (Anexo A: un 30 % más).")]
        [Min(1f)] public float vidaDelJuezSinMemorias = 1.3f;

        [Tooltip("El destello de los enemigos que se vuelven más agresivos: rojo apagado.")]
        [SerializeField] private Color colorDestello = new Color(0.55f, 0.2f, 0.2f, 1f);

        [SerializeField, Min(0.1f)] private float duracionDestello = 1f;

        private readonly List<string> _recordadas = new List<string>();

        /// <summary>El de la escena, si lo hay.</summary>
        public static SistemaConsecuencias Instancia { get; private set; }

        /// <summary>Las memorias recordadas en esta partida del nivel, en orden.</summary>
        public IReadOnlyList<string> Recordadas => _recordadas;

        public bool NingunaRecordada => _recordadas.Count == 0;

        /// <summary>Una memoria volvió más agresivos a estos enemigos.</summary>
        public event Action<MemoriaInteractuable, IReadOnlyList<EnemyController>> OnConsecuencia;

        /// <summary>
        /// Una copia de <paramref name="datos"/> que va <paramref name="factor"/> veces más deprisa: anda
        /// y persigue más rápido, espera menos y su golpe (el aviso, la ventana y el descanso) llega antes.
        /// </summary>
        public static EnemyData Agravados(EnemyData datos, float factor)
        {
            var copia = Instantiate(datos);
            copia.name = $"{datos.name} (agravado ×{factor:0.##})";
            copia.velocidadPatrulla *= factor;
            copia.velocidadPersecucion *= factor;
            copia.esperaEnWaypoint /= factor;
            copia.attackCooldown /= factor;
            copia.duracionAtaque /= factor;
            copia.inicioVentanaGolpe /= factor;
            copia.finVentanaGolpe /= factor;
            return copia;
        }

        /// <summary>
        /// Vuelve a <paramref name="enemigo"/> <paramref name="factor"/> veces más agresivo sin curarlo:
        /// sus datos agravados y, si la tiene, su propia pieza.
        /// </summary>
        public static void Agravar(EnemyController enemigo, float factor)
        {
            if (enemigo == null || enemigo.Datos == null)
            {
                return;
            }

            var vida = enemigo.Vida != null ? enemigo.Vida.Vida : 0;
            enemigo.FijarDatos(Agravados(enemigo.Datos, factor));
            if (enemigo.Vida != null && vida > 0 && enemigo.Vida.Vida > vida)
            {
                enemigo.Vida.PerderVida(enemigo.Vida.Vida - vida);
            }

            foreach (var agravable in enemigo.GetComponents<IAgravable>())
            {
                agravable.Agravar(factor);
            }
        }

        private void OnEnable()
        {
            Instancia = this;
            MemoriaInteractuable.AlRecordar += AlRecordar;
        }

        private void OnDisable()
        {
            MemoriaInteractuable.AlRecordar -= AlRecordar;
            if (Instancia == this)
            {
                Instancia = null;
            }
        }

        private void AlRecordar(MemoriaInteractuable memoria)
        {
            if (memoria == null || _recordadas.Contains(memoria.Id))
            {
                return;
            }

            _recordadas.Add(memoria.Id);
            var afectados = new List<EnemyController>();
            foreach (var enemigo in EnemyController.Activos)
            {
                if (enemigo != null && enemigo.Vida != null && enemigo.Vida.EstaVivo && memoria.Zona.Contains((Vector2)enemigo.transform.position))
                {
                    afectados.Add(enemigo);
                }
            }

            foreach (var enemigo in afectados)
            {
                Agravar(enemigo, agresividad);
            }

            OnConsecuencia?.Invoke(memoria, afectados);
            if (afectados.Count > 0)
            {
                StartCoroutine(DestellarTrasLaCinematica(afectados));
            }
        }

        // Al volver al juego, los que se volvieron más agresivos destellan en rojo apagado.
        private IEnumerator DestellarTrasLaCinematica(List<EnemyController> enemigos)
        {
            yield return null;
            yield return new WaitUntil(() => GameStateMachine.Instancia == null || GameStateMachine.Instancia.EstadoActual != ESTADOS_JUEGO.CUTSCENE);

            var dibujos = new List<(SpriteRenderer dibujo, Color color)>();
            foreach (var enemigo in enemigos)
            {
                var dibujo = enemigo != null ? enemigo.GetComponentInChildren<SpriteRenderer>() : null;
                if (dibujo != null)
                {
                    dibujos.Add((dibujo, dibujo.color));
                }
            }

            for (var t = 0f; t < duracionDestello; t += Time.deltaTime)
            {
                var mezcla = Mathf.PingPong(t * 4f / duracionDestello, 1f);
                foreach (var (dibujo, color) in dibujos)
                {
                    if (dibujo != null)
                    {
                        dibujo.color = Color.Lerp(color, colorDestello, mezcla);
                    }
                }

                yield return null;
            }

            foreach (var (dibujo, color) in dibujos)
            {
                if (dibujo != null)
                {
                    dibujo.color = color;
                }
            }
        }
    }
}
