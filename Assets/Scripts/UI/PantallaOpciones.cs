using System;
using Fragmentum.Core;
using Fragmentum.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Pantalla «Opciones»: el volumen general, el de la música y el de los efectos (<see cref="AjustesAudio"/>),
    /// de 10 en 10 %. Se oye al momento: en el menú, con su música; en la pausa, con la del nivel. Se guarda sola,
    /// aparte de la partida.
    ///
    /// Se abre desde el menú principal y desde la pausa; al cerrarla desde la pausa, la pausa vuelve (como
    /// las mecánicas).
    /// </summary>
    public class PantallaOpciones : PantallaSuperpuesta
    {
        /// <summary>Los pasos de cada deslizador: de 10 en 10 %.</summary>
        public const int Pasos = 10;

        private const float AnchoEtiqueta = 330f;
        private const float AnchoValor = 120f;

        private Slider _general;
        private Slider _musica;
        private Slider _efectos;
        private Text _valorGeneral;
        private Text _valorMusica;
        private Text _valorEfectos;
        private Text _ayuda;

        /// <summary>El volumen general (0 a <see cref="Pasos"/>).</summary>
        public Slider General => _general;

        /// <summary>El volumen de la música (0 a <see cref="Pasos"/>).</summary>
        public Slider Musica => _musica;

        /// <summary>El volumen de los efectos de sonido (0 a <see cref="Pasos"/>).</summary>
        public Slider Efectos => _efectos;

        /// <summary>Se cerró con «Volver» o con Esc (la pausa vuelve a mostrarse).</summary>
        public event Action Cerrada;

        public void Cerrar()
        {
            if (!Visible)
            {
                return;
            }

            Ocultar();
            Cerrada?.Invoke();
        }

        protected override void ConstruirContenido(RectTransform columna)
        {
            FabricaUI.Titulo(columna, "Titulo", FabricaUI.Espaciar("OPCIONES"), 34, new Color(1f, 1f, 1f, 0.85f));
            TextoAcento(columna, "Sonido", 22).text = "Sonido";
            FabricaUI.Hueco(columna, 6f);
            SeparadorAcento(columna);
            FabricaUI.Hueco(columna, 34f);

            (_general, _valorGeneral) = Fila(columna, "General", "Volumen general", v => AjustesAudio.General = v);
            FabricaUI.Hueco(columna, 16f);
            (_musica, _valorMusica) = Fila(columna, "Musica", "Música", v => AjustesAudio.Musica = v);
            FabricaUI.Hueco(columna, 16f);
            (_efectos, _valorEfectos) = Fila(columna, "Efectos", "Efectos", v => AjustesAudio.Efectos = v);

            FabricaUI.Hueco(columna, 34f);
            AgregarOpcion(columna, "Volver", Cerrar);
            FabricaUI.Hueco(columna, 6f);
            _ayuda = FabricaUI.Texto(columna, "Ayuda", string.Empty, 18, new Color(1f, 1f, 1f, 0.4f));
        }

        protected override void Rellenar(LevelData nivel)
        {
            Poner(_general, _valorGeneral, AjustesAudio.General);
            Poner(_musica, _valorMusica, AjustesAudio.Musica);
            Poner(_efectos, _valorEfectos, AjustesAudio.Efectos);
            foreach (var deslizador in new[] { _general, _musica, _efectos })
            {
                FabricaUI.AplicarAcento(deslizador, Acento);
                deslizador.fillRect.GetComponent<Image>().color = Acento;
            }

            _ayuda.text = Ayuda(Acento, ("↑↓", "ELEGIR"), ("←→", "SUBIR O BAJAR"), ("ESC", "VOLVER"));
        }

        // Con el teclado o el mando se empieza en el primer volumen.
        protected override Selectable PrimeraSeleccion() => _general;

        /// <summary>Lo que dice el valor de un volumen («70 %»).</summary>
        public static string Porcentaje(float volumen) => $"{Mathf.RoundToInt(volumen * 100f)} %";

        private static void Poner(Slider deslizador, Text valor, float volumen)
        {
            deslizador.SetValueWithoutNotify(Mathf.Round(volumen * Pasos));
            valor.text = Porcentaje(volumen);
        }

        // Una fila: el nombre, el deslizador y el valor.
        private static (Slider, Text) Fila(Transform columna, string nombre, string etiqueta, UnityAction<float> alCambiar)
        {
            var fila = FabricaUI.Objeto(nombre, columna);
            var horizontal = fila.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 24f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            var texto = FabricaUI.Titulo(fila, "Etiqueta", etiqueta, 30, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleLeft);
            Ancho(texto.gameObject, AnchoEtiqueta);

            var deslizador = FabricaUI.Deslizador(fila, "Deslizador", AcentoNeutro);
            deslizador.minValue = 0f;
            deslizador.maxValue = Pasos;
            deslizador.wholeNumbers = true;

            var valor = FabricaUI.Titulo(fila, "Valor", string.Empty, 28, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleRight);
            Ancho(valor.gameObject, AnchoValor);

            deslizador.onValueChanged.AddListener(v =>
            {
                alCambiar(v / Pasos);
                valor.text = Porcentaje(v / Pasos);
            });
            return (deslizador, valor);
        }

        private static void Ancho(GameObject objeto, float ancho)
        {
            var elemento = objeto.AddComponent<LayoutElement>();
            elemento.preferredWidth = ancho;
            elemento.preferredHeight = 44f;
        }
    }
}
