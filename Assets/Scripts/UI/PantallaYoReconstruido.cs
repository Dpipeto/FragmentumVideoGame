using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Pantalla «yo reconstruido» (tarea G): visualización acumulativa de los fragmentos de
    /// identidad. Kael está en el centro y los cinco fragmentos a su alrededor; los integrados
    /// brillan con el color de su emoción y los que faltan son un contorno apagado. Cada fragmento
    /// dice qué habilidad dio.
    ///
    /// Se abre desde el menú principal. Sin iconos todavía (PENDIENTES B9): los fragmentos son
    /// rombos de color y Kael, una silueta.
    /// </summary>
    public class PantallaYoReconstruido : PantallaSuperpuesta
    {
        private const float RadioConstelacion = 250f;

        private readonly List<Image> _rombos = new List<Image>();
        private readonly List<Text> _nombres = new List<Text>();
        private Text _cuenta;
        private RectTransform _constelacion;

        /// <summary>Fragmentos integrados que muestra ahora mismo.</summary>
        public int Integrados { get; private set; }

        public int Total { get; private set; }

        protected override void ConstruirContenido(RectTransform columna)
        {
            FabricaUI.Titulo(columna, "Titulo", FabricaUI.Espaciar("YO RECONSTRUIDO"), 34, new Color(1f, 1f, 1f, 0.85f));
            _cuenta = TextoAcento(columna, "Cuenta", 22);
            FabricaUI.Hueco(columna, 6f);
            SeparadorAcento(columna);

            _constelacion = FabricaUI.Objeto("Constelacion", columna);
            var elemento = _constelacion.gameObject.AddComponent<LayoutElement>();
            elemento.preferredWidth = 900f;
            elemento.preferredHeight = 600f;

            // Kael: una silueta clara en el centro.
            var kael = FabricaUI.Objeto("Kael", _constelacion).gameObject.AddComponent<Image>();
            kael.color = new Color(0.94f, 0.93f, 0.85f, 0.35f);
            kael.raycastTarget = false;
            FabricaUI.Colocar(kael.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(70f, 170f));

            FabricaUI.Hueco(columna, 10f);
            AgregarOpcion(columna, "Volver", () => Ocultar());
        }

        protected override void Rellenar(LevelData nivel)
        {
            var gestor = GameManager.Instancia;
            var bd = gestor != null ? gestor.BaseDeDatos : null;
            var fragmentos = new List<FragmentoData>();
            if (bd != null)
            {
                foreach (var n in bd.niveles)
                {
                    if (n != null && n.fragmento != null)
                    {
                        fragmentos.Add(n.fragmento);
                    }
                }
            }

            AsegurarHuecos(fragmentos.Count);
            Total = fragmentos.Count;
            Integrados = 0;

            for (var i = 0; i < fragmentos.Count; i++)
            {
                var fragmento = fragmentos[i];
                var integrado = gestor != null && gestor.Datos.TieneFragmento(fragmento.id.ToString());
                var color = ColorDe(fragmento, bd);
                if (integrado)
                {
                    Integrados++;
                }

                _rombos[i].color = integrado ? color : new Color(color.r, color.g, color.b, 0.18f);
                var habilidad = fragmento.habilidad != null ? $"\n<size=16>{fragmento.habilidad.nombre}</size>" : string.Empty;
                _nombres[i].text = integrado ? $"{fragmento.nombre}{habilidad}" : $"<color=#FFFFFF55>{fragmento.nombre}</color>";
            }

            _cuenta.text = $"{Integrados} de {Total} fragmentos integrados";
        }

        // Un rombo y un nombre por fragmento, repartidos en arco alrededor de Kael.
        private void AsegurarHuecos(int cantidad)
        {
            while (_rombos.Count < cantidad)
            {
                var i = _rombos.Count;
                var rombo = FabricaUI.Objeto($"Fragmento{i}", _constelacion).gameObject.AddComponent<Image>();
                rombo.raycastTarget = false;
                rombo.rectTransform.sizeDelta = new Vector2(46f, 46f);
                rombo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                _rombos.Add(rombo);

                var nombre = FabricaUI.Titulo(_constelacion, $"Nombre{i}", string.Empty, 19, new Color(1f, 1f, 1f, 0.85f));
                nombre.rectTransform.sizeDelta = new Vector2(260f, 60f);
                _nombres.Add(nombre);
            }

            for (var i = 0; i < _rombos.Count; i++)
            {
                var visible = i < cantidad;
                _rombos[i].gameObject.SetActive(visible);
                _nombres[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                // De izquierda a derecha por arriba, en el orden de los mundos.
                var angulo = Mathf.Lerp(200f, -20f, cantidad > 1 ? i / (float)(cantidad - 1) : 0.5f) * Mathf.Deg2Rad;
                var posicion = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * RadioConstelacion;
                _rombos[i].rectTransform.anchoredPosition = posicion + new Vector2(0f, 20f);
                _nombres[i].rectTransform.anchoredPosition = posicion + new Vector2(0f, -38f);
            }
        }

        private static Color ColorDe(FragmentoData fragmento, LevelDatabase bd)
        {
            if (bd != null)
            {
                foreach (var nivel in bd.niveles)
                {
                    if (nivel != null && nivel.fragmento == fragmento)
                    {
                        return nivel.ColorAcento;
                    }
                }
            }

            return AcentoNeutro;
        }
    }
}
