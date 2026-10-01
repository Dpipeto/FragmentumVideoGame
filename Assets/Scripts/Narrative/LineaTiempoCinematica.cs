using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Narrative
{
    /// <summary>
    /// Qué se ve en cada instante de una cinemática: qué imagen y con qué opacidad, qué textos y
    /// si se puede saltar. Es lógica pura (no toca la escena): <c>PantallaCinematica</c> solo
    /// aplica lo que dice, y las pruebas recorren la cinemática entera sin esperarla.
    ///
    /// Reglas:
    /// - Los fundidos ocurren <b>dentro</b> de la duración de cada diapositiva, así que la
    ///   cinemática dura exactamente la suma de sus diapositivas.
    /// - La primera imagen sale de negro y la última vuelve a negro.
    /// - Entre dos imágenes hay fundido cruzado: la que entra aparece encima de la que sale, que
    ///   sigue opaca debajo. Así nunca hay un corte duro ni un oscurecimiento a mitad del cruce.
    /// - Cada texto entra y sale con su propio fundido.
    /// </summary>
    public sealed class LineaTiempoCinematica
    {
        /// <summary>Una imagen en pantalla: índice de su diapositiva (-1 = ninguna) y opacidad.</summary>
        public readonly struct Capa
        {
            public Capa(int indice, float alfa)
            {
                Indice = indice;
                Alfa = alfa;
            }

            public int Indice { get; }
            public float Alfa { get; }
            public bool Visible => Indice >= 0 && Alfa > 0f;

            public static readonly Capa Ninguna = new Capa(-1, 0f);
        }

        /// <summary>Un texto en pantalla con su opacidad.</summary>
        public readonly struct TextoVisible
        {
            public TextoVisible(Cartel cartel, float alfa)
            {
                Cartel = cartel;
                Alfa = alfa;
            }

            public Cartel Cartel { get; }
            public float Alfa { get; }
        }

        private readonly CutsceneData _datos;
        private readonly float[] _inicios;
        private readonly List<Cartel>[] _carteles;

        public LineaTiempoCinematica(CutsceneData datos)
        {
            _datos = datos;
            var n = datos != null ? datos.diapositivas.Count : 0;
            _inicios = new float[n + 1];
            _carteles = new List<Cartel>[n];
            for (var i = 0; i < n; i++)
            {
                var diapositiva = datos.diapositivas[i];
                _inicios[i + 1] = _inicios[i] + (diapositiva != null ? diapositiva.duracion : 0f);
                _carteles[i] = datos.CartelesDe(i);
            }
        }

        public int Diapositivas => _carteles.Length;

        public float Duracion => _inicios[_inicios.Length - 1];

        public bool Terminada(float t) => t >= Duracion;

        /// <summary>Cuándo empieza la diapositiva <paramref name="indice"/>.</summary>
        public float InicioDe(int indice) => _inicios[Mathf.Clamp(indice, 0, _inicios.Length - 1)];

        /// <summary>La diapositiva en curso en el instante <paramref name="t"/> (-1 si no hay ninguna).</summary>
        public int IndiceEn(float t)
        {
            if (Diapositivas == 0)
            {
                return -1;
            }

            for (var i = Diapositivas - 1; i >= 0; i--)
            {
                if (t >= _inicios[i])
                {
                    return i;
                }
            }

            return 0;
        }

        // --- Imágenes ---

        /// <summary>
        /// Las dos capas de imagen en el instante <paramref name="t"/>: la de abajo y la que entra
        /// encima durante un fundido cruzado.
        /// </summary>
        public (Capa abajo, Capa encima) ImagenesEn(float t)
        {
            var i = IndiceEn(t);
            if (i < 0 || Terminada(t))
            {
                return (Capa.Ninguna, Capa.Ninguna);
            }

            var local = t - _inicios[i];
            var duracion = _inicios[i + 1] - _inicios[i];

            // Primera diapositiva: sale de negro.
            if (i == 0)
            {
                var entrada = Fundido(i);
                if (entrada > 0f && local < entrada)
                {
                    return (new Capa(i, local / entrada), Capa.Ninguna);
                }
            }

            var salida = i + 1 < Diapositivas ? FundidoEntre(i, i + 1) : Fundido(i);
            var restante = duracion - local;
            if (salida > 0f && restante < salida)
            {
                var progreso = 1f - restante / salida;
                return i + 1 < Diapositivas
                    ? (new Capa(i, 1f), new Capa(i + 1, progreso)) // fundido cruzado
                    : (new Capa(i, 1f - progreso), Capa.Ninguna);   // la última vuelve a negro
            }

            return (new Capa(i, 1f), Capa.Ninguna);
        }

        // El fundido no puede ocupar más de media diapositiva: en las muy cortas se acorta.
        private float Fundido(int i)
        {
            var duracion = _inicios[i + 1] - _inicios[i];
            return Mathf.Min(_datos.fundidoImagen, duracion * 0.5f);
        }

        private float FundidoEntre(int a, int b) => Mathf.Min(Fundido(a), Fundido(b));

        // --- Textos ---

        /// <summary>Los textos de la diapositiva en curso que se ven en el instante <paramref name="t"/>.</summary>
        public List<TextoVisible> TextosEn(float t)
        {
            var visibles = new List<TextoVisible>();
            var i = IndiceEn(t);
            if (i < 0 || Terminada(t))
            {
                return visibles;
            }

            var local = t - _inicios[i];
            foreach (var cartel in _carteles[i])
            {
                var alfa = AlfaTexto(cartel, local);
                if (alfa > 0f)
                {
                    visibles.Add(new TextoVisible(cartel, alfa));
                }
            }

            return visibles;
        }

        /// <summary>Opacidad de un cartel, <paramref name="local"/> segundos después de empezar su diapositiva.</summary>
        public float AlfaTexto(Cartel cartel, float local)
        {
            var fundido = Mathf.Min(_datos.fundidoTexto, cartel.duracion * 0.5f);
            if (local < cartel.inicio || local > cartel.Fin)
            {
                return 0f;
            }

            if (fundido <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01((local - cartel.inicio) / fundido) * Mathf.Clamp01((cartel.Fin - local) / fundido);
        }

        /// <summary>Los carteles de una diapositiva, ya con el texto principal incluido.</summary>
        public IReadOnlyList<Cartel> CartelesDe(int indice) => _carteles[indice];

        // --- Saltar ---

        public bool PuedeSaltar(float t) => _datos != null && _datos.saltable && t >= _datos.bloqueoSalto;

        /// <summary>
        /// Opacidad del aviso «pulsa cualquier tecla»: aparece al acabar el bloqueo y se ha ido del
        /// todo en <see cref="CutsceneData.finAviso"/>, con el fundido de los textos.
        /// </summary>
        public float AlfaAviso(float t)
        {
            if (_datos == null || !_datos.saltable || string.IsNullOrEmpty(_datos.avisoSaltar))
            {
                return 0f;
            }

            var desde = _datos.bloqueoSalto;
            var hasta = _datos.finAviso;
            if (t < desde || t > hasta || hasta <= desde)
            {
                return 0f;
            }

            var fundido = Mathf.Min(_datos.fundidoTexto, (hasta - desde) * 0.5f);
            var alfa = fundido > 0f ? Mathf.Clamp01((t - desde) / fundido) * Mathf.Clamp01((hasta - t) / fundido) : 1f;
            return alfa * _datos.opacidadAviso;
        }
    }
}
