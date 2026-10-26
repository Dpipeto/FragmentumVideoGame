using System.Collections;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Bosses.Patrones
{
    /// <summary>
    /// El compañero (la placa): el Juez abre la capa y las placas de identificación salen volando en
    /// <see cref="rafagas"/> ráfagas, rectas a la cabeza de Kael, como los disparos de los Artilleros.
    /// Se responde agachándose tras uno de los dos escombros de la plaza: la cobertura, en el combate.
    /// Invisible, vuelan a la cabeza de donde lo vio por última vez.
    ///
    /// Con <see cref="placa"/>, cada una se ve dibujada, girando en el aire (<c>Prompts_Mundo4.md</c>, C1).
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Jefes/Patrón: el compañero", fileName = "PatronCompanero")]
    public class PatronCompanero : PatronMemoria
    {
        [Min(1)] public int rafagas = 3;

        [Min(1)] public int placasPorRafaga = 3;

        [Min(0.01f)] public float entrePlacas = 0.12f;

        [Min(0f)] public float entreRafagas = 0.9f;

        [Min(1f)] public float velocidad = 12f;

        [Min(0.02f)] public float radio = 0.15f;

        [Tooltip("De dónde salen, sobre sus pies: de la capa, a la altura del pecho.")]
        [Min(0f)] public float alturaSalida = 2.4f;

        [Min(1f)] public float alcance = 30f;

        [Tooltip("La placa dibujada: su sprite y su radio son los del prefab. Vacía = un cuadrado de su color.")]
        public Proyectil placa;

        protected override IEnumerator Atacar(BossController jefe, JuezSilente juez)
        {
            jefe.Detener();
            jefe.Mirar(Kael(jefe, juez).x >= jefe.Centro.x ? 1 : -1);
            jefe.Avisar(aviso);
            Gesto(juez, GESTOS_JUEZ.AVISO_COMPANERO);
            yield return new WaitForSeconds(aviso);
            Gesto(juez, GESTOS_JUEZ.COMPANERO);

            for (var r = 0; r < rafagas; r++)
            {
                for (var p = 0; p < placasPorRafaga; p++)
                {
                    var origen = new Vector2(jefe.Centro.x + jefe.Mirando * jefe.MedioAncho, jefe.SueloY + alturaSalida);
                    var mira = juez != null ? juez.CabezaVista : jefe.CentroJugador + Vector2.up * 0.65f;
                    var direccion = mira - origen;
                    if (direccion.sqrMagnitude < 0.0001f)
                    {
                        direccion = new Vector2(jefe.Mirando, 0f);
                    }

                    if (placa != null)
                    {
                        Proyectil.Disparar(placa, origen, direccion.normalized * velocidad, danio, Capas.MascaraJugador, alcance);
                    }
                    else
                    {
                        Proyectil.Disparar("Placa", origen, direccion.normalized * velocidad, radio, danio, Capas.MascaraJugador, alcance, color);
                    }

                    yield return new WaitForSeconds(entrePlacas);
                }

                yield return new WaitForSeconds(entreRafagas);
            }

            Gesto(juez, GESTOS_JUEZ.CIERRA_CAPA);
        }
    }
}
