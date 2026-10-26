using Fragmentum.Core;
using Fragmentum.Core.Puntuacion;
using Fragmentum.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Fragmentum.UI
{
    /// <summary>
    /// El puntaje y el reloj del nivel en el HUD. Escucha a <see cref="EventosJuego"/>: no lee
    /// el marcador en cada fotograma. Al activarse se pone al día con el marcador de la escena,
    /// porque el HUD está apagado durante las cinemáticas y pudo perderse algún aviso.
    ///
    /// Sustituye al contador de monedas.
    /// </summary>
    public class ContadorPuntaje : MonoBehaviour
    {
        private const float DuracionGanancia = 1.4f;

        [SerializeField] private Text puntos;
        [SerializeField] private Text tiempo;

        [Tooltip("Opcional: enseña un momento lo que se acaba de ganar («+100»).")]
        [SerializeField] private Text ganancia;

        private float _ocultarGananciaEn;

        public string TextoPuntos => puntos != null ? puntos.text : string.Empty;

        public string TextoTiempo => tiempo != null ? tiempo.text : string.Empty;

        /// <summary>Lo que enseña ahora mismo como recién ganado, o vacío.</summary>
        public string TextoGanancia => ganancia != null && ganancia.gameObject.activeSelf ? ganancia.text : string.Empty;

        /// <summary>Le da sus textos cuando se crea por código.</summary>
        public void Fijar(Text textoPuntos, Text textoTiempo, Text textoGanancia)
        {
            puntos = textoPuntos;
            tiempo = textoTiempo;
            ganancia = textoGanancia;
            PonerAlDia();
        }

        private void OnEnable()
        {
            EventosJuego.PuntajeCambiado += AlCambiarPuntaje;
            EventosJuego.TiempoCambiado += AlCambiarTiempo;
            PonerAlDia();
        }

        private void OnDisable()
        {
            EventosJuego.PuntajeCambiado -= AlCambiarPuntaje;
            EventosJuego.TiempoCambiado -= AlCambiarTiempo;
        }

        private void Update()
        {
            if (ganancia != null && ganancia.gameObject.activeSelf && Time.unscaledTime >= _ocultarGananciaEn)
            {
                ganancia.gameObject.SetActive(false);
            }
        }

        private void PonerAlDia()
        {
            var marcador = MarcadorNivel.Actual;
            Pintar(marcador != null ? marcador.Puntos : 0);
            AlCambiarTiempo(marcador != null ? Mathf.FloorToInt(marcador.Segundos) : 0);
            if (ganancia != null)
            {
                ganancia.gameObject.SetActive(false);
            }
        }

        private void AlCambiarPuntaje(int total, int ganados)
        {
            Pintar(total);
            if (ganancia != null && ganados > 0)
            {
                ganancia.text = $"+{ReglasPuntaje.Puntos(ganados)}";
                ganancia.gameObject.SetActive(true);
                _ocultarGananciaEn = Time.unscaledTime + DuracionGanancia;
            }
        }

        private void AlCambiarTiempo(int segundos)
        {
            if (tiempo != null)
            {
                tiempo.text = $"TIEMPO  {ReglasPuntaje.Tiempo(segundos)}";
            }
        }

        private void Pintar(int total)
        {
            if (puntos != null)
            {
                puntos.text = $"PUNTAJE  {ReglasPuntaje.Puntos(total)}";
            }
        }
    }
}
