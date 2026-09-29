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
    /// iluminado? La usan los enemigos del mundo del Miedo, que solo se acercan en la
    /// oscuridad, y la usarán las antorchas del Nivel 3 (Fase 10).
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
        public static bool EstaIluminado(Vector2 punto)
        {
            for (var i = 0; i < Fuentes.Count; i++)
            {
                var fuente = Fuentes[i];
                if (fuente == null || !fuente.Encendida)
                {
                    continue;
                }

                if ((punto - fuente.Posicion).sqrMagnitude <= fuente.Radio * fuente.Radio)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Vacía el registro. Lo usan las pruebas y el arranque sin recarga de dominio.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reiniciar() => Fuentes.Clear();
    }
}
