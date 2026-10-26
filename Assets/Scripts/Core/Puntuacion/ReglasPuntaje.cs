using System.Globalization;
using UnityEngine;

namespace Fragmentum.Core.Puntuacion
{
    /// <summary>
    /// Las cuentas del puntaje, sin estado: cuánto vale el tiempo y cómo se escriben los puntos y
    /// el reloj. Sustituye a las monedas de la v1, que se recogían pero no medían nada del juego.
    ///
    /// El puntaje de un nivel tiene tres partes: lo que vale cada enemigo derrotado
    /// (<c>EnemyData.puntos</c>: un mini-jefe vale varias veces lo que un Eco), lo que vale el jefe
    /// (<c>JefeData.puntos</c>) y un bono por tiempo, mayor cuanto antes se cruza la salida.
    /// </summary>
    public static class ReglasPuntaje
    {
        // Miles separados por un espacio: «12 345». Ni punto ni coma, que cambian con el idioma.
        private static readonly NumberFormatInfo Miles = new NumberFormatInfo { NumberGroupSeparator = " " };

        /// <summary>
        /// El bono por tiempo: <paramref name="puntosPorSegundo"/> por cada segundo entero que
        /// sobre de <paramref name="tiempoConBono"/>. Pasado ese tiempo no hay bono, pero tampoco
        /// se resta: tardar no quita lo ganado.
        /// </summary>
        public static int BonoDeTiempo(float segundos, float tiempoConBono, int puntosPorSegundo)
        {
            if (tiempoConBono <= 0f || puntosPorSegundo <= 0)
            {
                return 0;
            }

            var sobran = Mathf.FloorToInt(tiempoConBono - Mathf.Max(0f, segundos));
            return Mathf.Max(0, sobran) * puntosPorSegundo;
        }

        /// <summary>El reloj: «04:35». Pasada la hora siguen contando los minutos («75:02»).</summary>
        public static string Tiempo(float segundos)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(segundos));
            return $"{total / 60:00}:{total % 60:00}";
        }

        /// <summary>Los puntos con los miles separados: «12 345».</summary>
        public static string Puntos(int puntos) => puntos.ToString("N0", Miles);
    }

    /// <summary>
    /// El puntaje de un nivel recién superado, desglosado, y cómo queda frente al mejor anterior.
    /// Lo calcula <c>MarcadorNivel</c> al cruzar la salida y lo enseña el fin de nivel.
    /// </summary>
    public readonly struct ResultadoNivel
    {
        public ResultadoNivel(int enemigos, int jefe, int bonoTiempo, float segundos, int mejorAnterior, bool habiaAnterior)
        {
            Enemigos = enemigos;
            Jefe = jefe;
            BonoTiempo = bonoTiempo;
            Segundos = segundos;
            MejorAnterior = mejorAnterior;
            HabiaAnterior = habiaAnterior;
        }

        /// <summary>Puntos por los enemigos derrotados, mini-jefes incluidos.</summary>
        public int Enemigos { get; }

        /// <summary>Puntos por el jefe del mundo.</summary>
        public int Jefe { get; }

        public int BonoTiempo { get; }

        /// <summary>Lo que duró el nivel, sin pausas ni cinemáticas.</summary>
        public float Segundos { get; }

        /// <summary>El mejor puntaje guardado de este nivel antes de este intento.</summary>
        public int MejorAnterior { get; }

        /// <summary>¿Se había superado ya el nivel con puntaje? La primera vez no hay con qué comparar.</summary>
        public bool HabiaAnterior { get; }

        public int Total => Enemigos + Jefe + BonoTiempo;

        public bool EsRecord => HabiaAnterior && Total > MejorAnterior;
    }
}
