using Fragmentum.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Menú principal. No carga escenas por su cuenta: pide cada acción a la FSM global.
    ///
    /// Respecto de la v1: ya no hay nombres de escena escritos en el código («Niveles»,
    /// «Level1»); existe «Continuar», que lleva al nivel más avanzado desbloqueado; y
    /// «Salir» también funciona en el Editor.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button botonContinuar;
        [SerializeField] private Button botonNuevaPartida;
        [SerializeField] private Button botonSelector;
        [SerializeField] private Button botonSalir;

        [Tooltip("Opcional: resumen del progreso guardado.")]
        [SerializeField] private Text textoProgreso;

        private void Start()
        {
            Enganchar(botonContinuar, Continuar);
            Enganchar(botonNuevaPartida, NuevaPartida);
            Enganchar(botonSelector, SeleccionarNivel);
            Enganchar(botonSalir, Salir);
            Refrescar();
        }

        /// <summary>Activa «Continuar» solo si hay progreso y algo jugable, y pinta el resumen.</summary>
        public void Refrescar()
        {
            var gestor = GameManager.Instancia;
            var bd = gestor != null ? gestor.BaseDeDatos : null;
            var hayProgreso = gestor != null && gestor.Datos.TieneProgreso;

            if (botonContinuar != null)
            {
                botonContinuar.interactable = hayProgreso && bd != null && bd.ParaContinuar(gestor.Datos) != null;
            }

            if (gestor == null)
            {
                Debug.LogWarning("[Menú] No hay sistemas del juego (GameManager). Arranca desde el menú o revisa Resources/Sistemas.", this);
            }

            if (textoProgreso != null)
            {
                textoProgreso.text = gestor == null || bd == null ? string.Empty
                    : $"Fragmentos integrados: {gestor.Datos.fragmentos.Count} de {TotalFragmentos(bd)} · Monedas: {gestor.Datos.monedas}";
            }
        }

        public void Continuar() => Juego()?.ContinuarPartida();

        public void NuevaPartida()
        {
            var gestor = GameManager.Instancia;
            if (gestor == null)
            {
                return;
            }

            gestor.NuevaPartida();
            var bd = gestor.BaseDeDatos;
            var primero = bd != null && bd.niveles.Count > 0 ? bd.niveles[0] : null;
            if (primero != null && primero.Jugable)
            {
                Juego()?.JugarNivel(primero);
            }
            else
            {
                Juego()?.IrAlSelector();
            }
        }

        public void SeleccionarNivel() => Juego()?.IrAlSelector();

        public void Salir()
        {
            if (GameManager.Instancia != null)
            {
                GameManager.Instancia.Guardar();
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static GameStateMachine Juego() => GameStateMachine.Instancia;

        private static int TotalFragmentos(Data.LevelDatabase bd)
        {
            var total = 0;
            foreach (var nivel in bd.niveles)
            {
                if (nivel != null && nivel.fragmento != null) total++;
            }

            return total;
        }

        private static void Enganchar(Button boton, UnityEngine.Events.UnityAction accion)
        {
            if (boton != null)
            {
                boton.onClick.AddListener(accion);
            }
        }
    }
}
