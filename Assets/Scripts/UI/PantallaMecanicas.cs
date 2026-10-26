using System;
using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Core.Save;
using Fragmentum.Data;
using Fragmentum.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// Pantalla «Mecánicas»: qué sabe hacer Kael y con qué teclas. A la izquierda, sus movimientos
    /// (<see cref="GuiaMecanicas"/>); a la derecha, las habilidades de los fragmentos en el orden de
    /// los mundos. Las integradas se explican con el color de su emoción; las que faltan se ven
    /// bloqueadas, con el mundo donde se consiguen. Se recalcula cada vez que se abre, así que crece
    /// con la partida.
    ///
    /// Se abre desde el menú principal y desde la pausa. Las teclas salen del asset de input
    /// (<see cref="EtiquetasControles"/>).
    /// </summary>
    public class PantallaMecanicas : PantallaSuperpuesta
    {
        private const float AnchoColumna = 600f;
        private const float AnchoControles = 200f;
        private const float SeparacionColumnas = 60f;
        private const int TamanoNombre = 22;
        private const int TamanoTexto = 17;

        /// <summary>Una fila de la guía, ya resuelta.</summary>
        public struct Fila
        {
            public string Nombre;
            public string Controles;
            public string Descripcion;

            /// <summary>Habilidad de un fragmento (columna derecha); si no, un movimiento.</summary>
            public bool EsHabilidad;

            /// <summary>Habilidad todavía sin integrar: no se dice cuál es, solo dónde se consigue.</summary>
            public bool Bloqueada;

            /// <summary>Fragmento que la da (solo habilidades).</summary>
            public string Origen;

            /// <summary>Color de su emoción (solo habilidades).</summary>
            public Color Color;
        }

        private readonly List<Fila> _filas = new List<Fila>();
        private readonly List<(Text controles, Text cuerpo)> _filasMovimientos = new List<(Text, Text)>();
        private readonly List<(Text controles, Text cuerpo)> _filasHabilidades = new List<(Text, Text)>();
        private RectTransform _columnaMovimientos;
        private RectTransform _columnaHabilidades;
        private Text _cuenta;
        private Text _ayuda;
        private GuiaMecanicas _guiaPorDefecto;

        /// <summary>Textos de los movimientos. Vacío = los del diseño.</summary>
        public GuiaMecanicas Guia { get; set; }

        /// <summary>Asset de input del que salen las teclas.</summary>
        public InputActionAsset Acciones { get; set; }

        /// <summary>Base de datos y progreso que se muestran. Vacíos = los del GameManager.</summary>
        public LevelDatabase BaseDeDatos { get; set; }

        public DatosGuardado Datos { get; set; }

        /// <summary>Lo que muestra ahora mismo, fila a fila.</summary>
        public IReadOnlyList<Fila> Filas => _filas;

        public int HabilidadesIntegradas { get; private set; }

        public int HabilidadesTotales { get; private set; }

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
            FabricaUI.Titulo(columna, "Titulo", FabricaUI.Espaciar("MECÁNICAS"), 34, new Color(1f, 1f, 1f, 0.85f));
            _cuenta = TextoAcento(columna, "Cuenta", 22);
            FabricaUI.Hueco(columna, 6f);
            SeparadorAcento(columna);
            FabricaUI.Hueco(columna, 18f);

            var cuerpo = FabricaUI.Objeto("Cuerpo", columna);
            var horizontal = cuerpo.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = SeparacionColumnas;
            horizontal.childAlignment = TextAnchor.UpperCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            // Algo más ancho que el marco común (1100): sin el mínimo, la columna central lo encogería.
            cuerpo.gameObject.AddComponent<LayoutElement>().minWidth = 2f * AnchoColumna + SeparacionColumnas;

            _columnaMovimientos = Columna(cuerpo, "Movimientos", "MOVIMIENTOS");
            _columnaHabilidades = Columna(cuerpo, "Habilidades", "HABILIDADES");

            FabricaUI.Hueco(columna, 18f);
            AgregarOpcion(columna, "Volver", Cerrar);
            FabricaUI.Hueco(columna, 6f);
            _ayuda = FabricaUI.Texto(columna, "Ayuda", string.Empty, 18, new Color(1f, 1f, 1f, 0.4f));
        }

        protected override void Rellenar(LevelData nivel)
        {
            var gestor = GameManager.Instancia;
            var bd = BaseDeDatos != null ? BaseDeDatos : gestor != null ? gestor.BaseDeDatos : null;
            var datos = Datos ?? (gestor != null ? gestor.Datos : null);
            var jugador = gestor != null ? gestor.Jugador : null;
            var vivas = jugador != null ? jugador.Habilidades : null;

            _filas.Clear();
            _filas.AddRange(Calcular(GuiaUsada(), bd, datos, nivel, vivas != null ? vivas.Tiene : (Func<HABILIDADES, bool>)null, Acciones));

            HabilidadesTotales = 0;
            HabilidadesIntegradas = 0;
            var movimientos = 0;
            var habilidades = 0;
            foreach (var fila in _filas)
            {
                if (fila.EsHabilidad)
                {
                    HabilidadesTotales++;
                    if (!fila.Bloqueada) HabilidadesIntegradas++;
                    Pintar(FilaEn(_columnaHabilidades, _filasHabilidades, habilidades++), fila);
                }
                else
                {
                    Pintar(FilaEn(_columnaMovimientos, _filasMovimientos, movimientos++), fila);
                }
            }

            Recortar(_filasMovimientos, movimientos);
            Recortar(_filasHabilidades, habilidades);

            _cuenta.text = HabilidadesTotales == 0 ? string.Empty
                : $"{HabilidadesIntegradas} de {HabilidadesTotales} habilidades integradas";
            _ayuda.text = Ayuda(Acento, ("ESC", "VOLVER"));
        }

        /// <summary>
        /// Las filas de la guía para un progreso dado: los movimientos que ya tocan y todas las
        /// habilidades de los mundos, integradas o no. Una habilidad cuenta como integrada si está
        /// en el progreso guardado o si el jugador vivo ya la tiene (las escenas de prueba las dan
        /// desde el Inspector).
        /// </summary>
        public static List<Fila> Calcular(GuiaMecanicas guia, LevelDatabase bd, DatosGuardado datos, LevelData nivelActual,
            Func<HABILIDADES, bool> tieneEnVivo, InputActionAsset acciones)
        {
            var filas = new List<Fila>();

            if (guia != null)
            {
                foreach (var movimiento in guia.movimientos)
                {
                    if (movimiento == null || !SeMuestra(movimiento.requisito, bd, datos, nivelActual))
                    {
                        continue;
                    }

                    filas.Add(new Fila
                    {
                        Nombre = movimiento.nombre,
                        Controles = EtiquetasControles.Resolver(movimiento.controles, acciones),
                        Descripcion = movimiento.descripcion,
                    });
                }
            }

            if (bd == null)
            {
                return filas;
            }

            var vistas = new HashSet<HabilidadData>();
            foreach (var nivel in bd.niveles)
            {
                var habilidad = nivel == null ? null
                    : nivel.habilidadOtorgada != null ? nivel.habilidadOtorgada
                    : nivel.fragmento != null ? nivel.fragmento.habilidad : null;
                if (habilidad == null || !vistas.Add(habilidad))
                {
                    continue;
                }

                var integrada = (datos != null && datos.TieneHabilidad(habilidad.id.ToString())) ||
                                (tieneEnVivo != null && tieneEnVivo(habilidad.id));
                filas.Add(integrada
                    ? new Fila
                    {
                        EsHabilidad = true,
                        Nombre = habilidad.nombre,
                        Controles = string.IsNullOrEmpty(habilidad.controles) ? "—" : EtiquetasControles.Resolver(habilidad.controles, acciones),
                        Descripcion = habilidad.descripcion,
                        Origen = nivel.fragmento != null ? nivel.fragmento.nombre : nivel.Titulo,
                        Color = nivel.ColorAcento,
                    }
                    : new Fila
                    {
                        EsHabilidad = true,
                        Bloqueada = true,
                        Nombre = "? ? ?",
                        Controles = "· · ·",
                        Descripcion = $"Se integra al superar {nivel.Etiqueta}.",
                        Color = nivel.ColorAcento,
                    });
            }

            return filas;
        }

        private static bool SeMuestra(REQUISITOS_MECANICA requisito, LevelDatabase bd, DatosGuardado datos, LevelData nivelActual)
        {
            switch (requisito)
            {
                case REQUISITOS_MECANICA.MUNDO_CON_AGACHARSE:
                    if (nivelActual != null && nivelActual.reglas.agacharse)
                    {
                        return true;
                    }

                    if (bd != null && datos != null)
                    {
                        foreach (var nivel in bd.niveles)
                        {
                            if (nivel != null && nivel.reglas.agacharse && bd.EstaDesbloqueado(nivel, datos))
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                default:
                    return true;
            }
        }

        private GuiaMecanicas GuiaUsada()
        {
            if (Guia != null)
            {
                return Guia;
            }

            return _guiaPorDefecto != null ? _guiaPorDefecto : _guiaPorDefecto = GuiaMecanicas.PorDefecto();
        }

        private void OnDestroy()
        {
            if (_guiaPorDefecto == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_guiaPorDefecto);
            }
            else
            {
                DestroyImmediate(_guiaPorDefecto);
            }
        }

        // --- Construcción ---

        private static RectTransform Columna(Transform padre, string nombre, string titulo)
        {
            var columna = FabricaUI.Objeto(nombre, padre);
            var vertical = FabricaUI.Columna(columna, 10f);
            vertical.childAlignment = TextAnchor.UpperLeft;
            var elemento = columna.gameObject.AddComponent<LayoutElement>();
            elemento.preferredWidth = AnchoColumna;
            elemento.minWidth = AnchoColumna;

            var cabecera = FabricaUI.Titulo(columna, "Cabecera", FabricaUI.Espaciar(titulo), 18, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleLeft);
            cabecera.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
            return columna;
        }

        private static (Text controles, Text cuerpo) FilaEn(RectTransform columna, List<(Text, Text)> filas, int indice)
        {
            while (filas.Count <= indice)
            {
                var fila = FabricaUI.Objeto($"Fila{filas.Count}", columna);
                var horizontal = fila.gameObject.AddComponent<HorizontalLayoutGroup>();
                horizontal.spacing = 20f;
                horizontal.childAlignment = TextAnchor.UpperLeft;
                horizontal.childControlWidth = true;
                horizontal.childControlHeight = true;
                horizontal.childForceExpandWidth = false;
                horizontal.childForceExpandHeight = false;

                var controles = FabricaUI.Texto(fila, "Controles", string.Empty, 18, Color.white, TextAnchor.UpperRight);
                controles.fontStyle = FontStyle.Bold;
                var elemento = controles.gameObject.AddComponent<LayoutElement>();
                elemento.preferredWidth = AnchoControles;
                elemento.minWidth = AnchoControles;

                var cuerpo = FabricaUI.Texto(fila, "Cuerpo", string.Empty, TamanoTexto, new Color(1f, 1f, 1f, 0.92f), TextAnchor.UpperLeft);
                cuerpo.lineSpacing = 1.05f;
                var ancho = cuerpo.gameObject.AddComponent<LayoutElement>();
                ancho.preferredWidth = AnchoColumna - AnchoControles - horizontal.spacing;
                ancho.flexibleWidth = 1f;

                filas.Add((controles, cuerpo));
            }

            var par = filas[indice];
            par.Item1.transform.parent.gameObject.SetActive(true);
            return par;
        }

        private static void Recortar(List<(Text controles, Text cuerpo)> filas, int visibles)
        {
            for (var i = visibles; i < filas.Count; i++)
            {
                filas[i].controles.transform.parent.gameObject.SetActive(false);
            }
        }

        private void Pintar((Text controles, Text cuerpo) destino, Fila fila)
        {
            if (fila.Bloqueada)
            {
                destino.controles.text = fila.Controles;
                destino.controles.color = new Color(1f, 1f, 1f, 0.3f);
                destino.cuerpo.text = $"<size={TamanoNombre}><color=#FFFFFF4D>{fila.Nombre}</color></size>\n<color=#FFFFFF59>{fila.Descripcion}</color>";
                return;
            }

            destino.controles.text = fila.Controles;
            destino.controles.color = Acento;

            var nombre = fila.EsHabilidad ? $"<color=#{FabricaUI.ColorHtml(fila.Color)}>{fila.Nombre}</color>" : fila.Nombre;
            var origen = string.IsNullOrEmpty(fila.Origen) ? string.Empty : $"\n<size=14><color=#FFFFFF59>{fila.Origen}</color></size>";
            destino.cuerpo.text = $"<size={TamanoNombre}>{nombre}</size>\n<color=#FFFFFF8C>{fila.Descripcion}</color>{origen}";
        }
    }
}
