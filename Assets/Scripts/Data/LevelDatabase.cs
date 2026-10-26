using System.Collections.Generic;
using Fragmentum.Core;
using Fragmentum.Core.Save;
using UnityEngine;

namespace Fragmentum.Data
{
    /// <summary>
    /// Lista ordenada de los niveles y escenas de navegación: la <b>única fuente de verdad</b>
    /// para el menú, el selector y las transiciones. Ningún script vuelve a escribir un
    /// nombre de escena (en la v1 el selector cargaba «Level1»…«Level10», que no existían).
    ///
    /// La regla de desbloqueo es secuencial: se puede jugar hasta el nivel máximo alcanzado.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Base de datos de niveles", fileName = "LevelDatabase")]
    public class LevelDatabase : ScriptableObject
    {
        [Header("Navegación")]
        public ReferenciaEscena escenaMenu = new ReferenciaEscena();
        public ReferenciaEscena escenaSelector = new ReferenciaEscena();

        [Tooltip("La música del menú inicial y del selector (la misma en los dos). Vacío = en silencio.")]
        public AudioClip musicaMenu;

        [Header("Niveles, en orden")]
        public List<LevelData> niveles = new List<LevelData>();

        public int PrimerId => niveles.Count > 0 && niveles[0] != null ? niveles[0].id : 1;

        public LevelData PorId(int id)
        {
            foreach (var nivel in niveles)
            {
                if (nivel != null && nivel.id == id) return nivel;
            }

            return null;
        }

        /// <summary>El nivel que usa esa escena, o null si la escena no es un nivel (menú, pruebas…).</summary>
        public LevelData PorEscena(string nombreEscena)
        {
            if (string.IsNullOrEmpty(nombreEscena)) return null;
            foreach (var nivel in niveles)
            {
                if (nivel != null && nivel.escena != null && nivel.escena.Nombre == nombreEscena) return nivel;
            }

            return null;
        }

        public LevelData Siguiente(LevelData nivel)
        {
            var indice = niveles.IndexOf(nivel);
            return indice >= 0 && indice + 1 < niveles.Count ? niveles[indice + 1] : null;
        }

        public bool EstaDesbloqueado(LevelData nivel, DatosGuardado datos) =>
            nivel != null && datos != null && nivel.id <= Mathf.Max(PrimerId, datos.nivelMaximoAlcanzado);

        /// <summary>
        /// El nivel donde «Continuar» debe dejar al jugador: el desbloqueado más avanzado que
        /// tenga escena. Null si no hay ninguno jugable.
        /// </summary>
        public LevelData ParaContinuar(DatosGuardado datos)
        {
            LevelData elegido = null;
            foreach (var nivel in niveles)
            {
                if (nivel != null && nivel.Jugable && EstaDesbloqueado(nivel, datos))
                {
                    elegido = nivel;
                }
            }

            return elegido;
        }

        /// <summary>
        /// Problemas de configuración, en texto. Vacío si todo está bien. Lo usan el
        /// inspector de la base de datos, el menú de validación y las pruebas.
        /// </summary>
        public List<string> Validar()
        {
            var problemas = new List<string>();
            if (!escenaMenu.Asignada) problemas.Add("Falta la escena del menú.");
            else if (!escenaMenu.EnBuild) problemas.Add($"La escena del menú '{escenaMenu.Nombre}' no está en Build Settings.");
            if (!escenaSelector.Asignada) problemas.Add("Falta la escena del selector.");
            else if (!escenaSelector.EnBuild) problemas.Add($"La escena del selector '{escenaSelector.Nombre}' no está en Build Settings.");

            var ids = new HashSet<int>();
            var anterior = int.MinValue;
            for (var i = 0; i < niveles.Count; i++)
            {
                var nivel = niveles[i];
                if (nivel == null)
                {
                    problemas.Add($"El hueco {i} de la lista de niveles está vacío.");
                    continue;
                }

                if (!ids.Add(nivel.id)) problemas.Add($"Id de nivel repetido: {nivel.id} ({nivel.name}).");
                if (nivel.id <= anterior) problemas.Add($"Los niveles no están ordenados por id ({nivel.name}).");
                anterior = nivel.id;

                if (nivel.emocion == null) problemas.Add($"{nivel.name} no tiene emoción.");
                if (nivel.escena.Asignada && !nivel.escena.EnBuild)
                {
                    problemas.Add($"La escena '{nivel.escena.Nombre}' de {nivel.name} no está en Build Settings.");
                }
            }

            return problemas;
        }
    }
}
