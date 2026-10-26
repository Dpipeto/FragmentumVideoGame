using System;
using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Las explosiones del mundo de la Culpa: la de un barril y la de una mina. Golpean una vez a
    /// todo lo que tienen a su alcance (a los enemigos más que a Kael), encienden los barriles
    /// cercanos (las cadenas explosivas del Anexo A) y lo anuncian (<see cref="AlEstallar"/>): el
    /// Verdugo se arrodilla si le pilla cerca.
    ///
    /// Los barriles y las minas no reciben el golpe: un barril lo enciende la cadena, y una mina solo
    /// estalla al tocarla o al golpearla.
    /// </summary>
    public static class Explosiones
    {
        /// <summary>Estalló algo: su centro y su radio.</summary>
        public static event Action<Vector2, float> AlEstallar;

        /// <summary>
        /// Una explosión en <paramref name="centro"/>: quita <paramref name="danioEnemigos"/> a los
        /// enemigos y <paramref name="danioJugador"/> a Kael que estén a <paramref name="radio"/>, y
        /// enciende los barriles a menos de <paramref name="cadena"/>. Devuelve a cuántos golpeó.
        /// Con <paramref name="dibujo"/> (su arte), la explosión dibujada nace en <paramref name="pie"/>
        /// (si no se da, en el centro); sin él, un destello provisional.
        /// </summary>
        public static int Estallar(Vector2 centro, float radio, int danioEnemigos, int danioJugador, GameObject causante,
            float cadena = 0f, BarrilExplosivo excepto = null, ExplosionDibujada dibujo = null, Vector2? pie = null)
        {
            var capaJugador = LayerMask.NameToLayer(Capas.Player);
            var golpeados = new HashSet<IGolpeable>();
            foreach (var zona in Physics2D.OverlapCircleAll(centro, radio, Capas.Mascara(Capas.Player, Capas.Enemy)))
            {
                var raiz = zona.attachedRigidbody != null ? zona.attachedRigidbody.gameObject : zona.gameObject;
                var objetivo = raiz.GetComponentInParent<IGolpeable>();
                if (objetivo == null || objetivo is BarrilExplosivo || objetivo is Mina || !objetivo.EstaVivo || !golpeados.Add(objetivo))
                {
                    continue;
                }

                var danio = raiz.layer == capaJugador ? danioJugador : danioEnemigos;
                if (danio > 0)
                {
                    objetivo.RecibirGolpe(new Golpe(danio, centro, causante));
                }
            }

            if (cadena > 0f)
            {
                BarrilExplosivo.EncenderCercanos(centro, cadena, excepto);
            }

            if (dibujo != null)
            {
                ExplosionDibujada.Crear(dibujo, pie ?? centro, radio);
            }
            else
            {
                DestelloDeExplosion.Crear(centro, radio);
            }
            AlEstallar?.Invoke(centro, radio);
            return golpeados.Count;
        }

        /// <summary>Olvida a quien escuchaba. Lo usan las pruebas y el arranque sin recarga de dominio.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reiniciar() => AlEstallar = null;
    }
}
