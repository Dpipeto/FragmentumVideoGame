using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Core
{
    /// <summary>Algo que ilumina un círculo: una antorcha, una zona con luz, un cristal.</summary>
    public interface IFuenteLuz
    {
        Vector2 Posicion { get; }

        float Radio { get; }

        bool Encendida { get; }
    }

    /// <summary>
    /// Registro de las fuentes de luz activas. Responde a una sola pregunta: ¿este punto está
    /// iluminado? Se registran en él las antorchas del Nivel 3, y lo preguntan los enemigos del
    /// mundo del Miedo, que solo se acercan en la oscuridad, y los cepos y las ramas podridas, que
    /// se delatan con luz.
    ///
    /// La luz propia del jugador no se registra aquí: si contara, el jugador estaría siempre
    /// iluminado y esos enemigos no se acercarían nunca.
    /// </summary>
    public static class Iluminacion
    {
        private static readonly List<IFuenteLuz> Fuentes = new List<IFuenteLuz>();

        public static int Cantidad => Fuentes.Count;

        public static void Registrar(IFuenteLuz fuente)
        {
            if (fuente != null && !Fuentes.Contains(fuente))
            {
                Fuentes.Add(fuente);
            }
        }

        public static void Quitar(IFuenteLuz fuente) => Fuentes.Remove(fuente);

        /// <summary>¿Cae el punto dentro de alguna fuente de luz encendida?</summary>
        public static bool EstaIluminado(Vector2 punto) => FuenteEn(punto) != null;

        /// <summary>
        /// La fuente encendida que ilumina el punto (la de centro más cercano, si son varias), o
        /// null. La usan los Espectros, que salen de la luz por el lado contrario a ella.
        /// </summary>
        public static IFuenteLuz FuenteEn(Vector2 punto)
        {
            IFuenteLuz mejor = null;
            var menor = float.MaxValue;
            for (var i = 0; i < Fuentes.Count; i++)
            {
                var fuente = Fuentes[i];
                if (fuente == null || !fuente.Encendida)
                {
                    continue;
                }

                var distancia = (punto - fuente.Posicion).sqrMagnitude;
                if (distancia <= fuente.Radio * fuente.Radio && distancia < menor)
                {
                    mejor = fuente;
                    menor = distancia;
                }
            }

            return mejor;
        }

        /// <summary>Vacía el registro. Lo usan las pruebas y el arranque sin recarga de dominio.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reiniciar() => Fuentes.Clear();
    }
}
