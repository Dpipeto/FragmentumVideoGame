using System.Collections;
using Fragmentum.Core;
using Fragmentum.Levels.Escenario;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// Reaparición mínima: al morir, el jugador vuelve al punto indicado tras unos segundos.
    ///
    /// <b>Provisional.</b> Mantiene jugable la escena de prueba de la Fase 3. En la Fase 6 lo
    /// sustituyen el CheckpointManager (último checkpoint activado) y la pantalla de Game Over.
    /// </summary>
    public class ReaparicionSimple : MonoBehaviour
    {
        [Tooltip("Vacío = el objeto con el tag Player.")]
        [SerializeField] private PlayerController jugador;

        [Tooltip("Dónde reaparecer. Vacío = donde empezó el jugador.")]
        [SerializeField] private Transform punto;

        [SerializeField] private float espera = 1.2f;

        [Tooltip("Opcional: se recoloca de golpe al reaparecer.")]
        [SerializeField] private CameraMovement camara;

        private Vector2 _inicio;

        private void Start()
        {
            if (jugador == null)
            {
                var objeto = GameObject.FindGameObjectWithTag(GameTags.Player);
                jugador = objeto != null ? objeto.GetComponent<PlayerController>() : null;
            }

            if (jugador == null || jugador.Vida == null)
            {
                Debug.LogWarning("[Reaparición] No se encontró al jugador; no habrá reaparición.", this);
                enabled = false;
                return;
            }

            _inicio = jugador.transform.position;
            jugador.Vida.OnMuerte += AlMorir;
        }

        private void OnDestroy()
        {
            if (jugador != null && jugador.Vida != null)
            {
                jugador.Vida.OnMuerte -= AlMorir;
            }
        }

        private void AlMorir() => StartCoroutine(Reaparecer());

        private IEnumerator Reaparecer()
        {
            yield return new WaitForSeconds(espera);

            jugador.Revivir(punto != null ? (Vector2)punto.position : _inicio);
            if (camara != null)
            {
                camara.Encuadrar();
            }
        }
    }
}
