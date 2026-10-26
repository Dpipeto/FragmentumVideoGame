using System.Collections;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// Un ataque del Juez Silente: cada uno es una memoria (<c>Nivel4_Diseno.md</c>, sección 6).
    /// Mientras dura, el Juez la proyecta en la pared del fondo y su marca brilla en el suelo
    /// (<see cref="JuezSilente.Proyectar"/>); si Kael la mira, la ventana corta el ataque. Al acabar
    /// solo, las marcas se apagan. Con otro jefe, el ataque sale igual, sin memorias.
    ///
    /// Apunta a donde el Juez vio a Kael por última vez (<see cref="JuezSilente.KaelVisto"/>): invisible,
    /// Kael se esconde del golpe, pero no puede mirar.
    ///
    /// Con arte, cada ataque pide sus gestos (<see cref="Gesto"/>) y, al acabar, el Juez vuelve al reposo.
    /// </summary>
    public abstract class PatronMemoria : PatronAtaque
    {
        [Tooltip("La memoria que es este ataque: 0 la orden (la radio), 1 el pueblo (la campana), 2 el compañero (la placa) y 3 la carta.")]
        [Range(0, 3)] public int memoria;

        [Tooltip("Lo que se queda quieto al acabar, antes del siguiente.")]
        [Min(0f)] public float recuperacion = 0.8f;

        public override IEnumerator Ejecutar(BossController jefe)
        {
            var juez = jefe as JuezSilente;
            if (juez != null)
            {
                juez.Proyectar(memoria);
            }

            yield return Atacar(jefe, juez);
            if (juez != null)
            {
                juez.Apagar();
                juez.Gesto(GESTOS_JUEZ.REPOSO);
            }

            yield return new WaitForSeconds(recuperacion);
        }

        /// <summary>El ataque en sí. Avisa <see cref="PatronAtaque.aviso"/> s antes de hacer daño.</summary>
        protected abstract IEnumerator Atacar(BossController jefe, JuezSilente juez);

        /// <summary>Pide un gesto al arte del Juez (con otro jefe, nada).</summary>
        protected static void Gesto(JuezSilente juez, GESTOS_JUEZ gesto)
        {
            if (juez != null)
            {
                juez.Gesto(gesto);
            }
        }

        /// <summary>Dónde está Kael para el Juez: donde lo ve o, invisible, donde lo vio por última vez.</summary>
        protected static Vector2 Kael(BossController jefe, JuezSilente juez) => juez != null ? juez.KaelVisto : jefe.CentroJugador;
    }
}
