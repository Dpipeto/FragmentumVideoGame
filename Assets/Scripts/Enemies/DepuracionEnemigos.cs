using UnityEngine;
using UnityEngine.InputSystem;

namespace Fragmentum.Enemies
{
    /// <summary>
    /// Escribe sobre cada enemigo su estado, su vida y su tipo, para ver la FSM funcionando en
    /// la partida (y grabarla para la tesis). F2 lo muestra u oculta.
    /// </summary>
    public class DepuracionEnemigos : MonoBehaviour
    {
        [SerializeField] private bool mostrar = true;
        [SerializeField] private Vector3 desplazamiento = new Vector3(0f, 2.4f, 0f);

        private GUIStyle _estilo;
        private Camera _camara;

        private void Update()
        {
            var teclado = Keyboard.current;
            if (teclado != null && teclado.f2Key.wasPressedThisFrame)
            {
                mostrar = !mostrar;
            }
        }

        private void OnGUI()
        {
            if (!mostrar)
            {
                return;
            }

            if (_camara == null)
            {
                _camara = Camera.main;
                if (_camara == null) return;
            }

            if (_estilo == null)
            {
                _estilo = new GUIStyle(GUI.skin.box) { fontSize = 12, richText = true, alignment = TextAnchor.MiddleCenter };
            }

            foreach (var enemigo in EnemyController.Activos)
            {
                if (enemigo == null) continue;

                var pantalla = _camara.WorldToScreenPoint(enemigo.transform.position + desplazamiento);
                if (pantalla.z < 0f) continue;

                var nombre = enemigo.Datos != null ? enemigo.Datos.nombreCanonico : enemigo.name;
                var vida = enemigo.Vida != null ? $"{enemigo.Vida.Vida}/{enemigo.Vida.VidaMaxima}" : "?";
                var texto = $"<b>{enemigo.EstadoActual}</b>\n{nombre} · {vida}";
                var tamano = _estilo.CalcSize(new GUIContent(texto));
                GUI.Box(new Rect(pantalla.x - tamano.x * 0.5f, Screen.height - pantalla.y - tamano.y, tamano.x + 8f, tamano.y), texto, _estilo);
            }
        }
    }
}
