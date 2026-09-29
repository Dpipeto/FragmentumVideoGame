using System.Text;
using Fragmentum.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fragmentum.Player
{
    /// <summary>
    /// Panel de depuración en pantalla: estado de las dos FSM del jugador, velocidad, sensores,
    /// vida y habilidades, más la lista de controles. Sirve para probar cada fase y para
    /// grabar demostraciones de la FSM para la tesis. F1 lo muestra u oculta.
    /// </summary>
    public class DepuracionJugador : MonoBehaviour
    {
        [SerializeField] private PlayerController jugador;
        [SerializeField] private PlayerModoMovimiento modo;
        [SerializeField] private bool mostrar = true;

        [Tooltip("Texto extra (por ejemplo, qué probar en cada zona de la escena).")]
        [SerializeField, TextArea(2, 8)] private string notas;

        private readonly StringBuilder _texto = new StringBuilder(512);
        private GUIStyle _estilo;
        private ESTADOS_JUGADOR _anterior;

        private void Awake()
        {
            if (jugador == null)
            {
                var objeto = GameObject.FindGameObjectWithTag(GameTags.Player);
                jugador = objeto != null ? objeto.GetComponent<PlayerController>() : null;
            }

            if (modo == null && jugador != null) modo = jugador.GetComponent<PlayerModoMovimiento>();
        }

        private void OnEnable()
        {
            if (jugador != null) jugador.OnStateChanged += Registrar;
        }

        private void OnDisable()
        {
            if (jugador != null) jugador.OnStateChanged -= Registrar;
        }

        private void Registrar(ESTADOS_JUGADOR anterior, ESTADOS_JUGADOR nuevo) => _anterior = anterior;

        private void Update()
        {
            var teclado = Keyboard.current;
            if (teclado != null && teclado.f1Key.wasPressedThisFrame)
            {
                mostrar = !mostrar;
            }
        }

        private void OnGUI()
        {
            if (!mostrar || jugador == null)
            {
                return;
            }

            if (_estilo == null)
            {
                _estilo = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 13,
                    richText = true,
                    padding = new RectOffset(8, 8, 6, 6)
                };
            }

            var v = jugador.Motor != null ? jugador.Motor.Velocidad : Vector2.zero;
            var s = jugador.Sensores;
            var h = jugador.Habilidades;
            var vida = jugador.Vida;

            _texto.Clear();
            _texto.Append("<b>Estado:</b> ").Append(jugador.EstadoActual)
                .Append("   <color=#aaaaaa>(antes ").Append(_anterior).Append(")</color>\n");
            if (modo != null)
            {
                _texto.Append("<b>Modo:</b> ").Append(modo.ModoActual).Append('\n');
            }

            _texto.Append("<b>Velocidad:</b> ").Append(v.x.ToString("0.0")).Append(", ").Append(v.y.ToString("0.0")).Append('\n');
            if (s != null)
            {
                _texto.Append("<b>Suelo:</b> ").Append(s.EnSuelo ? "sí" : "no");
                if (s.Suelo != null)
                {
                    _texto.Append(" (").Append(s.Suelo.Contactos).Append(" contacto/s)");
                }

                _texto.Append("   <b>Pared:</b> ").Append(s.ParedIzquierda ? "I" : "-").Append(s.ParedDerecha ? "D" : "-").Append('\n');
            }

            if (vida != null)
            {
                _texto.Append("<b>Vida:</b> ").Append(vida.Vida).Append('/').Append(vida.VidaMaxima);
                if (vida.EscudoDesbloqueado)
                {
                    _texto.Append("   <b>Escudo:</b> ").Append(vida.EscudoListo ? "listo" : "recargando");
                }

                _texto.Append('\n');
            }

            if (h != null)
            {
                _texto.Append("<b>Habilidades:</b> ").Append(h.Desbloqueadas.Count == 0 ? "ninguna" : string.Join(", ", h.Desbloqueadas)).Append('\n');
                if (h.Tiene(HABILIDADES.INVISIBILIDAD_PARCIAL))
                {
                    _texto.Append("<b>Invisible:</b> ").Append(h.EsInvisible ? "sí" : h.InvisibilidadLista ? "no (lista)" : $"no ({h.EsperaInvisibilidad:0.0} s)").Append('\n');
                }

                if (h.CargaCompleta)
                {
                    _texto.Append("<color=#ff9977><b>¡Carga completa! Suelta para el ataque cargado</b></color>\n");
                }
            }

            _texto.Append("\n<color=#aaaaaa>A/D mover · Espacio saltar · Shift dash · Clic atacar (mantener = cargar)\n")
                .Append("C/Ctrl agacharse · Q invisibilidad · F1 ocultar este panel</color>");

            if (!string.IsNullOrEmpty(notas))
            {
                _texto.Append("\n\n").Append(notas);
            }

            var contenido = new GUIContent(_texto.ToString());
            var alto = _estilo.CalcHeight(contenido, 440f);
            GUI.Box(new Rect(10f, 64f, 440f, alto), contenido, _estilo);
        }
    }
}
