using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.Narrative;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Fin de nivel (plan, §3.6: «registro emocional omitible y fragmento»), en dos pasos:
    ///
    /// 1. <b>Registro emocional</b> (tarea G): la pregunta del mundo, una escala de 1 a 5, un
    ///    comentario opcional y, la primera vez, un código de participante opcional. «Omitir»
    ///    pasa sin guardar nada: nunca bloquea el juego.
    /// 2. <b>Recompensa:</b> el fragmento integrado, la habilidad que desbloquea, la línea de
    ///    cierre de Kael, cuántos fragmentos lleva y a dónde lleva «Continuar».
    ///
    /// Si el registro está desactivado en su configuración, se pasa directamente al paso 2.
    /// </summary>
    public class PantallaFinNivel : PantallaSuperpuesta
    {
        private readonly List<Button> _escala = new List<Button>();
        private Text _mundo;

        private GameObject _panelRegistro;
        private Text _pregunta;
        private Text _etiquetaMinima;
        private Text _etiquetaMaxima;
        private InputField _comentario;
        private InputField _participante;
        private Button _guardar;
        private Button _omitir;

        private GameObject _panelRecompensa;
        private Text _gracias;
        private Text _fragmento;
        private Text _descripcionFragmento;
        private Text _habilidad;
        private Text _cierre;
        private Text _integrados;
        private Text _siguiente;
        private Button _continuar;
        private Button _menu;

        private Text _ayuda;
        private LevelData _nivel;
        private int _valor;

        /// <summary>Textos del registro. Sin configuración, los valores por defecto.</summary>
        public ConfiguracionRegistro Configuracion { get; set; }

        public bool MostrandoRegistro => _panelRegistro != null && _panelRegistro.activeSelf;

        public int ValorElegido => _valor;

        public Button BotonContinuar => _continuar;

        public Button BotonMenu => _menu;

        public string LineaCierre => _cierre != null ? _cierre.text : string.Empty;

        private ConfiguracionRegistro Config => Configuracion != null ? Configuracion : Configuracion = ScriptableObject.CreateInstance<ConfiguracionRegistro>();

        // --- API (también la usan las pruebas) ---

        /// <summary>Marca un valor de la escala (1-5).</summary>
        public void ElegirValor(int valor)
        {
            _valor = Mathf.Clamp(valor, RegistroEmocional.ValorMinimo, RegistroEmocional.ValorMaximo);
            for (var i = 0; i < _escala.Count; i++)
            {
                var elegido = i + 1 == _valor;
                var colores = _escala[i].colors;
                colores.normalColor = elegido ? Acento : new Color(1f, 1f, 1f, 0.55f);
                _escala[i].colors = colores;
            }

            _guardar.interactable = true;
        }

        /// <summary>Guarda la respuesta en el registro emocional y pasa a la recompensa.</summary>
        public void GuardarRegistro()
        {
            if (_valor <= 0)
            {
                return;
            }

            var registro = GameManager.Instancia != null ? GameManager.Instancia.Registro : null;
            if (registro != null)
            {
                if (_participante.gameObject.activeSelf && !string.IsNullOrWhiteSpace(_participante.text))
                {
                    registro.Participante = _participante.text;
                }

                registro.Registrar(_nivel, _valor, _comentario.text);
            }

            MostrarRecompensa(true);
        }

        /// <summary>Pasa a la recompensa sin guardar nada.</summary>
        public void OmitirRegistro() => MostrarRecompensa(false);

        // --- Construcción ---

        protected override void ConstruirContenido(RectTransform columna)
        {
            FabricaUI.Texto(columna, "Titulo", FabricaUI.Espaciar("MUNDO SUPERADO"), 34, new Color(1f, 1f, 1f, 0.85f));
            _mundo = TextoAcento(columna, "Mundo", 24);
            FabricaUI.Hueco(columna, 6f);
            SeparadorAcento(columna);
            FabricaUI.Hueco(columna, 14f);

            ConstruirRegistro(columna);
            ConstruirRecompensa(columna);

            FabricaUI.Hueco(columna, 20f);
            _ayuda = FabricaUI.Texto(columna, "Ayuda", string.Empty, 18, new Color(1f, 1f, 1f, 0.4f));
        }

        private void ConstruirRegistro(RectTransform columna)
        {
            var panel = Seccion(columna, "Registro");
            _panelRegistro = panel.gameObject;

            _pregunta = FabricaUI.Texto(panel, "Pregunta", string.Empty, 28, new Color(1f, 1f, 1f, 0.9f));
            Ancho(_pregunta, 900f);

            var fila = FabricaUI.Objeto("Escala", panel);
            var horizontal = fila.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 18f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;
            for (var i = 1; i <= RegistroEmocional.ValorMaximo; i++)
            {
                var valor = i;
                _escala.Add(AgregarOpcionEn(fila, i.ToString(), () => ElegirValor(valor), 76f, 64f));
            }

            var etiquetas = FabricaUI.Objeto("Etiquetas", panel);
            var elemento = etiquetas.gameObject.AddComponent<LayoutElement>();
            elemento.preferredWidth = 520f;
            elemento.preferredHeight = 26f;
            _etiquetaMinima = FabricaUI.Texto(etiquetas, "Minima", string.Empty, 18, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleLeft);
            FabricaUI.Estirar(_etiquetaMinima.rectTransform);
            _etiquetaMaxima = FabricaUI.Texto(etiquetas, "Maxima", string.Empty, 18, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleRight);
            FabricaUI.Estirar(_etiquetaMaxima.rectTransform);

            FabricaUI.Hueco(panel, 8f);
            _comentario = FabricaUI.Campo(panel, "Comentario", string.Empty, 700f);
            _participante = FabricaUI.Campo(panel, "Participante", string.Empty, 420f);
            FabricaUI.Hueco(panel, 8f);

            var acciones = FabricaUI.Objeto("Acciones", panel);
            var filaAcciones = acciones.gameObject.AddComponent<HorizontalLayoutGroup>();
            filaAcciones.spacing = 40f;
            filaAcciones.childAlignment = TextAnchor.MiddleCenter;
            filaAcciones.childControlWidth = true;
            filaAcciones.childControlHeight = true;
            filaAcciones.childForceExpandWidth = false;
            _omitir = AgregarOpcionEn(acciones, "Omitir", OmitirRegistro, 260f, 60f);
            _guardar = AgregarOpcionEn(acciones, "Guardar", GuardarRegistro, 260f, 60f);
        }

        private void ConstruirRecompensa(RectTransform columna)
        {
            var panel = Seccion(columna, "Recompensa");
            _panelRecompensa = panel.gameObject;

            _gracias = FabricaUI.Texto(panel, "Gracias", string.Empty, 18, new Color(1f, 1f, 1f, 0.45f));
            FabricaUI.Texto(panel, "Integrado", "FRAGMENTO INTEGRADO", 18, new Color(1f, 1f, 1f, 0.45f));
            _fragmento = TextoAcento(panel, "Fragmento", 40);
            _descripcionFragmento = FabricaUI.Texto(panel, "DescripcionFragmento", string.Empty, 22, new Color(1f, 1f, 1f, 0.6f));
            Ancho(_descripcionFragmento, 900f);
            FabricaUI.Hueco(panel, 8f);
            _habilidad = FabricaUI.Texto(panel, "Habilidad", string.Empty, 22, new Color(1f, 1f, 1f, 0.85f));
            Ancho(_habilidad, 900f);
            FabricaUI.Hueco(panel, 8f);
            _cierre = TextoAcento(panel, "Cierre", 22, 0.7f);
            _cierre.fontStyle = FontStyle.Italic;
            Ancho(_cierre, 900f);
            _integrados = FabricaUI.Texto(panel, "Integrados", string.Empty, 20, new Color(1f, 1f, 1f, 0.55f));
            FabricaUI.Hueco(panel, 16f);

            _continuar = AgregarOpcion(panel, "Continuar", () => Juego?.ContinuarTrasNivel());
            _siguiente = FabricaUI.Texto(panel, "Siguiente", string.Empty, 18, new Color(1f, 1f, 1f, 0.45f));
            FabricaUI.Hueco(panel, 6f);
            _menu = AgregarOpcion(panel, "Menú principal", () => Juego?.IrAlMenu());
        }

        // --- Relleno ---

        protected override void Rellenar(LevelData nivel)
        {
            _nivel = nivel;
            _mundo.text = nivel != null ? nivel.Etiqueta : string.Empty;

            // Registro
            var config = Config;
            _valor = 0;
            ElegirSinMarcar();
            _pregunta.text = config.PreguntaPara(nivel);
            _etiquetaMinima.text = $"1 · {config.etiquetaMinima}";
            _etiquetaMaxima.text = $"{config.etiquetaMaxima} · 5";
            _comentario.text = string.Empty;
            _comentario.gameObject.SetActive(config.pedirComentario);
            Marcador(_comentario, config.ayudaComentario);

            var registro = GameManager.Instancia != null ? GameManager.Instancia.Registro : null;
            var pedirParticipante = config.pedirParticipante && registro != null && string.IsNullOrEmpty(registro.Participante);
            _participante.text = string.Empty;
            _participante.gameObject.SetActive(pedirParticipante);
            Marcador(_participante, config.ayudaParticipante);

            // Recompensa
            var fragmento = nivel != null ? nivel.fragmento : null;
            _fragmento.text = fragmento != null ? fragmento.nombre : "—";
            _descripcionFragmento.text = fragmento != null ? fragmento.descripcion : string.Empty;

            var habilidad = nivel == null ? null
                : nivel.habilidadOtorgada != null ? nivel.habilidadOtorgada
                : fragmento != null ? fragmento.habilidad : null;
            _habilidad.text = habilidad != null
                ? $"<color=#{FabricaUI.ColorHtml(Acento)}>Nueva habilidad: {habilidad.nombre}</color>\n<size=20>{habilidad.descripcion}</size>"
                : string.Empty;

            var cierre = nivel != null && nivel.dialogos != null ? nivel.dialogos.Del(MOMENTOS_DIALOGO.CIERRE) : null;
            _cierre.text = cierre != null && cierre.Count > 0 ? $"«{cierre[0].texto}» — {cierre[0].hablante}" : string.Empty;
            _integrados.text = ContarIntegrados();
            _siguiente.text = DescribirSiguiente(nivel);

            var conRegistro = config.activo;
            _panelRegistro.SetActive(conRegistro);
            _panelRecompensa.SetActive(!conRegistro);
            _gracias.text = string.Empty;
            ActualizarAyuda();
        }

        protected override Selectable PrimeraSeleccion() => MostrandoRegistro ? _omitir : _continuar;

        private void MostrarRecompensa(bool guardado)
        {
            _panelRegistro.SetActive(false);
            _panelRecompensa.SetActive(true);
            _gracias.text = guardado ? "Gracias: tu respuesta quedó guardada." : string.Empty;
            ActualizarAyuda();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_continuar.gameObject);
            }
        }

        private void ElegirSinMarcar()
        {
            foreach (var boton in _escala)
            {
                FabricaUI.AplicarAcento(boton, Acento);
            }

            _guardar.interactable = false;
        }

        private void ActualizarAyuda()
        {
            _ayuda.text = MostrandoRegistro
                ? Ayuda(Acento, ("←→", "ELEGIR"), ("ENTER", "CONFIRMAR"))
                : Ayuda(Acento, ("↑↓", "NAVEGAR"), ("ENTER", "SELECCIONAR"));
        }

        private static string ContarIntegrados()
        {
            var gestor = GameManager.Instancia;
            var bd = gestor != null ? gestor.BaseDeDatos : null;
            if (bd == null)
            {
                return string.Empty;
            }

            var total = 0;
            var marcas = new System.Text.StringBuilder();
            foreach (var nivel in bd.niveles)
            {
                if (nivel == null || nivel.fragmento == null)
                {
                    continue;
                }

                total++;
                var integrado = gestor.Datos.TieneFragmento(nivel.fragmento.id.ToString());
                marcas.Append(integrado ? $"<color=#{FabricaUI.ColorHtml(nivel.ColorAcento)}>◆</color> " : "<color=#FFFFFF40>◇</color> ");
            }

            return $"{marcas}\n<size=16>{gestor.Datos.fragmentos.Count} de {total} fragmentos integrados</size>";
        }

        private static string DescribirSiguiente(LevelData nivel)
        {
            var bd = GameManager.Instancia != null ? GameManager.Instancia.BaseDeDatos : null;
            var siguiente = bd != null && nivel != null ? bd.Siguiente(nivel) : null;
            if (siguiente == null)
            {
                return "Vuelves al selector de mundos.";
            }

            return siguiente.Jugable
                ? $"Siguiente: {siguiente.Etiqueta}"
                : $"{siguiente.Etiqueta} está en construcción: vuelves al selector.";
        }

        // --- Piezas ---

        private static RectTransform Seccion(RectTransform columna, string nombre)
        {
            var seccion = FabricaUI.Objeto(nombre, columna);
            FabricaUI.ColumnaCentrada(seccion, 10f);
            var ajuste = seccion.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return seccion;
        }

        private static void Ancho(Graphic grafico, float ancho)
        {
            var elemento = grafico.gameObject.AddComponent<LayoutElement>();
            elemento.preferredWidth = ancho;
        }

        private static void Marcador(InputField campo, string texto)
        {
            if (campo.placeholder is Text marcador)
            {
                marcador.text = texto;
            }
        }
    }
}
