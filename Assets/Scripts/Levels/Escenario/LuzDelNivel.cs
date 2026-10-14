using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Fragmentum.Levels.Escenario
{
    /// <summary>Cuánta luz hay en un tramo del nivel: la de la luna, la oscuridad completa…</summary>
    [Serializable]
    public struct TramoDeLuz
    {
        public string nombre;
        public float desdeX;
        public float hastaX;
        public Color color;

        [Range(0f, 2f)] public float intensidad;

        public TramoDeLuz(string nombre, float desdeX, float hastaX, Color color, float intensidad)
        {
            this.nombre = nombre;
            this.desdeX = desdeX;
            this.hastaX = hastaX;
            this.color = color;
            this.intensidad = intensidad;
        }
    }

    /// <summary>
    /// La oscuridad de un nivel (mundo del Miedo, Anexo A: «luces 2D de URP»). Lleva la luz global
    /// de la escena y la cambia según el tramo en que esté Kael: la linde con luz de luna, el bosque
    /// cerrado a oscuras, el claro con luna otra vez y la arena con la oscuridad total. Entre un
    /// tramo y otro, la luz cambia poco a poco.
    ///
    /// No hace nada hasta que se activa: lo hace el <c>LevelManager</c> si las reglas del nivel piden
    /// oscuridad (<c>LevelData.reglas.oscuridad</c>). Así la escena se edita con luz.
    ///
    /// El Espectro del Pavor la usa en su combate: con su grito la oscuridad late (<see cref="Latir"/>)
    /// y, al vencerlo, retrocede (<see cref="Retroceder"/>): la luz sube y ya no vuelve a bajar.
    /// </summary>
    [DisallowMultipleComponent]
    public class LuzDelNivel : MonoBehaviour
    {
        [Tooltip("La luz global de la escena (Global Light 2D).")]
        [SerializeField] private Light2D luzGlobal;

        [Tooltip("La luz de cada tramo, de izquierda a derecha.")]
        [SerializeField] private TramoDeLuz[] tramos = Array.Empty<TramoDeLuz>();

        [Tooltip("Segundos que tarda la luz en pasar de la de un tramo a la del siguiente.")]
        [Min(0.01f)] [SerializeField] private float transicion = 1.5f;

        private Transform _objetivo;
        private float _intensidadInicial;
        private Color _colorInicial;
        private float _finLatido = float.NegativeInfinity;
        private bool _retrocedida;
        private float _luzTrasRetroceder;
        private Color _colorTrasRetroceder;
        private float _velocidadRetroceso;

        public bool Activa { get; private set; }

        /// <summary>¿Retrocedió ya la oscuridad? (Venció al Espectro del Pavor.)</summary>
        public bool Retrocedida => _retrocedida;

        /// <summary>¿Está latiendo? (El grito del Espectro del Pavor.)</summary>
        public bool Latiendo => Time.time < _finLatido;

        /// <summary>El tramo en el que está lo que sigue (−1 sin tramos).</summary>
        public int TramoActual { get; private set; } = -1;

        public IReadOnlyList<TramoDeLuz> Tramos => tramos;

        public Light2D LuzGlobal => luzGlobal;

        public float Transicion => transicion;

        /// <summary>
        /// El tramo que contiene <paramref name="x"/> (desde incluido, hasta excluido) o, fuera de
        /// todos, el más cercano. −1 si no hay tramos.
        /// </summary>
        public static int TramoEn(IReadOnlyList<TramoDeLuz> tramos, float x)
        {
            var mejor = -1;
            var mejorDistancia = float.MaxValue;
            for (var i = 0; i < tramos.Count; i++)
            {
                var tramo = tramos[i];
                if (x >= tramo.desdeX && x < tramo.hastaX)
                {
                    return i;
                }

                var distancia = x < tramo.desdeX ? tramo.desdeX - x : x - tramo.hastaX;
                if (distancia < mejorDistancia)
                {
                    mejor = i;
                    mejorDistancia = distancia;
                }
            }

            return mejor;
        }

        /// <summary>
        /// Oscurece el nivel y sigue a <paramref name="objetivo"/> (Kael). La luz de su tramo se pone
        /// de golpe: al empezar no hay transición.
        /// </summary>
        public void Activar(Transform objetivo)
        {
            if (luzGlobal == null)
            {
                Debug.LogWarning("[Nivel] La oscuridad del nivel no tiene luz global que llevar: el nivel se queda con luz.", this);
                return;
            }

            if (!Activa)
            {
                _intensidadInicial = luzGlobal.intensity;
                _colorInicial = luzGlobal.color;
            }

            Activa = true;
            _objetivo = objetivo;
            TramoActual = TramoEn(tramos, X);
            if (TramoActual >= 0)
            {
                luzGlobal.intensity = tramos[TramoActual].intensidad;
                luzGlobal.color = tramos[TramoActual].color;
            }
        }

        /// <summary>La oscuridad late durante <paramref name="segundos"/>: la luz sube y baja a golpes.</summary>
        public void Latir(float segundos) => _finLatido = Time.time + Mathf.Max(0f, segundos);

        /// <summary>
        /// La oscuridad retrocede: la luz sube hasta <paramref name="intensidad"/> en
        /// <paramref name="segundos"/>, con el color de <paramref name="color"/>, y ya no cambia por
        /// tramos. Es lo que pasa al vencer al Espectro del Pavor: el bosque se ve entero.
        /// </summary>
        public void Retroceder(float intensidad, Color color, float segundos)
        {
            if (luzGlobal == null)
            {
                return;
            }

            _retrocedida = true;
            _finLatido = float.NegativeInfinity;
            _luzTrasRetroceder = intensidad;
            _colorTrasRetroceder = color;
            _velocidadRetroceso = Mathf.Abs(intensidad - luzGlobal.intensity) / Mathf.Max(0.01f, segundos);
        }

        /// <summary>Devuelve la luz de la escena (la que tenía al activarse).</summary>
        public void Desactivar()
        {
            if (!Activa)
            {
                return;
            }

            Activa = false;
            TramoActual = -1;
            if (luzGlobal != null)
            {
                luzGlobal.intensity = _intensidadInicial;
                luzGlobal.color = _colorInicial;
            }
        }

        private float X => _objetivo != null ? _objetivo.position.x : transform.position.x;

        private void Update()
        {
            if (_retrocedida && luzGlobal != null)
            {
                luzGlobal.intensity = Mathf.MoveTowards(luzGlobal.intensity, _luzTrasRetroceder, _velocidadRetroceso * Time.deltaTime);
                luzGlobal.color = Color.Lerp(luzGlobal.color, _colorTrasRetroceder, Mathf.Clamp01(Time.deltaTime * 2f));
                return;
            }

            if (!Activa || luzGlobal == null || tramos.Length == 0)
            {
                return;
            }

            TramoActual = TramoEn(tramos, X);
            var tramo = tramos[TramoActual];

            // Lo que haya que recorrer entre dos tramos (el más claro y el más oscuro) se hace en
            // «transicion» segundos.
            var paso = Time.deltaTime / transicion;
            var rango = Mathf.Max(0.05f, RangoDeIntensidad());
            if (Latiendo)
            {
                // Dos latidos por segundo: la luz sube de golpe y vuelve a caer.
                luzGlobal.intensity = tramo.intensidad + 0.25f * Mathf.Max(0f, Mathf.Sin(Time.time * Mathf.PI * 4f));
            }
            else
            {
                luzGlobal.intensity = Mathf.MoveTowards(luzGlobal.intensity, tramo.intensidad, rango * paso);
            }

            luzGlobal.color = Color.Lerp(luzGlobal.color, tramo.color, Mathf.Clamp01(paso * 4f));
        }

        private float RangoDeIntensidad()
        {
            float minimo = float.MaxValue, maximo = float.MinValue;
            foreach (var tramo in tramos)
            {
                minimo = Mathf.Min(minimo, tramo.intensidad);
                maximo = Mathf.Max(maximo, tramo.intensidad);
            }

            return maximo - minimo;
        }

        private void OnDrawGizmosSelected()
        {
            foreach (var tramo in tramos)
            {
                Gizmos.color = new Color(tramo.color.r, tramo.color.g, tramo.color.b, 0.5f);
                var centro = new Vector3((tramo.desdeX + tramo.hastaX) * 0.5f, 8f, 0f);
                Gizmos.DrawWireCube(centro, new Vector3(tramo.hastaX - tramo.desdeX, 0.5f, 0f));
            }
        }
    }
}
