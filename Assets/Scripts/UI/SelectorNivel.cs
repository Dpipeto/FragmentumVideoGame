using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Selector de niveles generado desde la <see cref="LevelDatabase"/>.
    ///
    /// Corrige el selector de la v1 (auditoría, sección 3.5):
    /// - Generaba 10 botones fijos que cargaban «Level1»…«Level10», escenas que no existían.
    ///   Ahora hay un botón por nivel de la base de datos.
    /// - Decía «Level 3». Ahora muestra el número, la emoción y el mundo («3. Miedo — Bosque
    ///   Tenebroso»), con el color de la emoción.
    /// - Se podía entrar al nivel final desde el principio. Ahora los niveles no desbloqueados
    ///   aparecen bloqueados, y los que aún no tienen escena, «en construcción».
    /// - Muestra qué fragmentos se han integrado.
    /// - Limpia los botones anteriores antes de regenerarlos.
    /// </summary>
    public class SelectorNivel : MonoBehaviour
    {
        [Tooltip("Contenedor donde se crean los botones (con un VerticalLayoutGroup).")]
        [SerializeField] private RectTransform contenedor;

        [SerializeField] private Button botonVolver;

        [SerializeField] private float altoBoton = 84f;

        private readonly List<Button> _botones = new List<Button>();
        private readonly Dictionary<LevelData, Button> _porNivel = new Dictionary<LevelData, Button>();

        /// <summary>Botones generados, en el orden de la base de datos.</summary>
        public IReadOnlyList<Button> Botones => _botones;

        public Button BotonDe(LevelData nivel) => nivel != null && _porNivel.TryGetValue(nivel, out var b) ? b : null;

        private void Start()
        {
            if (botonVolver != null)
            {
                botonVolver.onClick.AddListener(() => GameStateMachine.Instancia?.IrAlMenu());
            }

            Generar();
        }

        /// <summary>Regenera los botones desde la base de datos y el progreso guardado.</summary>
        public void Generar()
        {
            Limpiar();

            var gestor = GameManager.Instancia;
            var bd = gestor != null ? gestor.BaseDeDatos : null;
            if (bd == null || contenedor == null)
            {
                Debug.LogWarning("[Selector] Falta la LevelDatabase (sistemas del juego) o el contenedor de botones; no se generan niveles.", this);
                return;
            }

            foreach (var nivel in bd.niveles)
            {
                if (nivel != null)
                {
                    Crear(nivel, gestor);
                }
            }
        }

        private void Crear(LevelData nivel, GameManager gestor)
        {
            var desbloqueado = gestor.EstaDesbloqueado(nivel);
            var jugable = nivel.Jugable;
            var completado = gestor.Datos.NivelCompletado(nivel.id);

            string detalle;
            if (!desbloqueado) detalle = "Bloqueado · supera el mundo anterior";
            else if (!jugable) detalle = "En construcción";
            else if (completado) detalle = nivel.fragmento != null ? $"Fragmento integrado · {nivel.fragmento.nombre}" : "Completado";
            else detalle = $"Dificultad {new string('*', nivel.dificultad)}";

            var titulo = $"{nivel.id}. {nivel.Titulo}";
            var etiqueta = $"<b>{titulo}</b>\n<size=20>{detalle}</size>";

            var boton = FabricaUI.Boton(contenedor, $"Nivel_{nivel.id}", etiqueta, FabricaUI.Fondo(nivel.ColorPrincipal), altoBoton,
                () => GameStateMachine.Instancia?.JugarNivel(nivel), 28);
            boton.interactable = desbloqueado && jugable;

            _botones.Add(boton);
            _porNivel[nivel] = boton;
        }

        private void Limpiar()
        {
            foreach (var boton in _botones)
            {
                if (boton != null)
                {
                    Destroy(boton.gameObject);
                }
            }

            _botones.Clear();
            _porNivel.Clear();
        }
    }
}
