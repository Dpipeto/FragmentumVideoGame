using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Pantalla que se superpone al juego: pausa, Game Over y fin de nivel. Traduce a uGUI la
    /// maqueta <c>Referencias/Menus/fragmentum_pause_worlds.jsx</c>: velo oscuro con el tono
    /// del mundo, viñeta, esquinas y separador con su color de acento, y opciones de texto que
    /// se iluminan al seleccionarlas.
    ///
    /// Patrón Template Method: esta clase crea el marco común, se muestra y se oculta, tiñe
    /// con el acento del nivel y selecciona la primera opción (para jugar con teclado o mando).
    /// Cada pantalla solo construye su contenido y lo rellena con los datos del nivel.
    ///
    /// Se construye por código: no hay referencias de Inspector que puedan quedar rotas, y el
    /// estilo común vive en <see cref="FabricaUI"/>.
    /// </summary>
    public abstract class PantallaSuperpuesta : MonoBehaviour
    {
        public static readonly Color AcentoNeutro = new Color(0.94f, 0.93f, 0.85f, 1f);

        private readonly List<Graphic> _acentos = new List<Graphic>();
        private readonly List<Button> _opciones = new List<Button>();
        private Image _velo;
        private bool _construida;

        /// <summary>Opciones de la pantalla, en orden (las pruebas las pulsan).</summary>
        public IReadOnlyList<Button> Opciones => _opciones;

        public bool Visible => _construida && gameObject.activeSelf;

        /// <summary>Acento con el que se mostró por última vez.</summary>
        public Color Acento { get; private set; } = AcentoNeutro;

        protected static GameStateMachine Juego => GameStateMachine.Instancia;

        /// <summary>Crea el marco y el contenido. La llama el UIManager una sola vez.</summary>
        public void Construir()
        {
            if (_construida)
            {
                return;
            }

            _construida = true;
            gameObject.SetActive(false);

            var raiz = (RectTransform)transform;
            FabricaUI.Estirar(raiz);

            _velo = FabricaUI.Panel(raiz, "Velo", new Color(0.02f, 0.02f, 0.05f, 0.72f));
            FabricaUI.Vineta(raiz, 0.55f);
            _acentos.AddRange(FabricaUI.Esquinas(raiz, new Color(1f, 1f, 1f, 0.6f)));

            var contenido = FabricaUI.Objeto("Contenido", raiz);
            FabricaUI.Colocar(contenido, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 960f));
            FabricaUI.ColumnaCentrada(contenido, 10f);

            ConstruirContenido(contenido);
        }

        /// <summary>Muestra la pantalla con los colores y textos del nivel (null fuera de los niveles).</summary>
        public void Mostrar(LevelData nivel)
        {
            Construir();

            Acento = nivel != null ? nivel.ColorAcento : AcentoNeutro;
            var fondo = nivel != null ? Color.Lerp(nivel.ColorOscuro, Color.black, 0.45f) : new Color(0.02f, 0.02f, 0.05f);
            _velo.color = new Color(fondo.r, fondo.g, fondo.b, 0.74f);

            foreach (var grafico in _acentos)
            {
                var alfa = grafico.color.a;
                grafico.color = new Color(Acento.r, Acento.g, Acento.b, alfa);
            }

            foreach (var opcion in _opciones)
            {
                FabricaUI.AplicarAcento(opcion, Acento);
            }

            Rellenar(nivel);
            gameObject.SetActive(true);
            SeleccionarPrimera();
        }

        public void Ocultar()
        {
            if (!_construida || !gameObject.activeSelf)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            gameObject.SetActive(false);
        }

        /// <summary>Crea los elementos propios de la pantalla dentro de la columna central.</summary>
        protected abstract void ConstruirContenido(RectTransform columna);

        /// <summary>Actualiza textos y opciones con los datos del nivel antes de mostrarse.</summary>
        protected abstract void Rellenar(LevelData nivel);

        // --- Piezas comunes para las pantallas ---

        protected Button AgregarOpcion(Transform columna, string etiqueta, UnityAction accion)
        {
            var opcion = FabricaUI.Opcion(columna, $"Opcion_{_opciones.Count}", etiqueta, AcentoNeutro, accion);
            _opciones.Add(opcion);
            return opcion;
        }

        /// <summary>Opción con otro tamaño (por ejemplo, los números de una escala).</summary>
        protected Button AgregarOpcionEn(Transform padre, string etiqueta, UnityAction accion, float ancho, float alto)
        {
            var opcion = FabricaUI.Opcion(padre, $"Opcion_{_opciones.Count}", etiqueta, AcentoNeutro, accion, ancho, alto);
            _opciones.Add(opcion);
            return opcion;
        }

        /// <summary>
        /// Qué queda seleccionado al abrirse (para el teclado y el mando). Por defecto, la primera
        /// opción disponible; una pantalla puede preferir otra, por ejemplo una inofensiva.
        /// </summary>
        protected virtual Selectable PrimeraSeleccion() => null;

        /// <summary>
        /// Texto que toma el color de acento del mundo. <paramref name="deTitulo"/>: con la letra
        /// de los títulos (el nombre del mundo, el de un fragmento) y no la de lectura (una cita).
        /// </summary>
        protected Text TextoAcento(Transform columna, string nombre, int tamano, float alfa = 1f, bool deTitulo = false)
        {
            var color = new Color(1f, 1f, 1f, alfa);
            var texto = deTitulo
                ? FabricaUI.Titulo(columna, nombre, string.Empty, tamano, color)
                : FabricaUI.Texto(columna, nombre, string.Empty, tamano, color);
            _acentos.Add(texto);
            return texto;
        }

        protected Image SeparadorAcento(Transform columna)
        {
            var linea = FabricaUI.Linea(columna, "Separador", AcentoNeutro, 90f, 3f);
            _acentos.Add(linea);
            return linea;
        }

        /// <summary>Ayuda de controles al pie, con las teclas del color del acento.</summary>
        protected static string Ayuda(Color acento, params (string tecla, string accion)[] pares)
        {
            var html = FabricaUI.ColorHtml(acento);
            var partes = new List<string>();
            foreach (var (tecla, accion) in pares)
            {
                partes.Add($"<color=#{html}>{tecla}</color>  {accion}");
            }

            return string.Join("   <color=#FFFFFF33>·</color>   ", partes);
        }

        private void SeleccionarPrimera()
        {
            if (EventSystem.current == null)
            {
                return;
            }

            var preferida = PrimeraSeleccion();
            if (preferida != null && preferida.gameObject.activeInHierarchy && preferida.interactable)
            {
                EventSystem.current.SetSelectedGameObject(preferida.gameObject);
                return;
            }

            foreach (var opcion in _opciones)
            {
                if (opcion != null && opcion.gameObject.activeInHierarchy && opcion.interactable)
                {
                    EventSystem.current.SetSelectedGameObject(opcion.gameObject);
                    return;
                }
            }
        }
    }
}
