using System;
using System.Collections.Generic;

namespace Fragmentum.Core.Save
{
    /// <summary>Punto de control activo: en qué nivel y qué checkpoint.</summary>
    [Serializable]
    public class DatosCheckpoint
    {
        public int nivelId;
        public string checkpointId = string.Empty;

        public bool Vacio => string.IsNullOrEmpty(checkpointId);
    }

    /// <summary>
    /// Todo lo que sobrevive al cierre del juego, en el formato del plan (§6.2). Los
    /// fragmentos y habilidades se guardan por nombre (IRA, ATAQUE_CARGADO…) para que un
    /// cambio en el orden de los enum no corrompa las partidas.
    ///
    /// Solo campos públicos: es lo que serializa <c>JsonUtility</c>.
    /// </summary>
    [Serializable]
    public class DatosGuardado
    {
        /// <summary>Versión del formato; permite migrar partidas antiguas si cambia.</summary>
        public int version = 1;

        /// <summary>Id del nivel más avanzado desbloqueado. Empieza en el primero.</summary>
        public int nivelMaximoAlcanzado = 1;

        public List<int> nivelesCompletados = new List<int>();
        public List<string> fragmentos = new List<string>();
        public List<string> habilidades = new List<string>();
        public int monedas;
        public DatosCheckpoint checkpoint = new DatosCheckpoint();
        public List<string> cinematicasVistas = new List<string>();
        public List<string> memoriasActivadas = new List<string>();

        /// <summary>Las monedas se importaron desde los PlayerPrefs de la v1.</summary>
        public bool migradoDePlayerPrefs;

        /// <summary>¿Hay algo que continuar (más allá de una partida recién empezada)?</summary>
        public bool TieneProgreso =>
            nivelesCompletados.Count > 0 || nivelMaximoAlcanzado > 1 || monedas > 0 || !checkpoint.Vacio;

        public bool NivelCompletado(int id) => nivelesCompletados.Contains(id);

        public bool TieneFragmento(string id) => fragmentos.Contains(id);

        public bool TieneHabilidad(string id) => habilidades.Contains(id);

        /// <summary>Añade sin duplicar. Devuelve si era nuevo.</summary>
        public static bool Anadir<T>(List<T> lista, T valor)
        {
            if (lista.Contains(valor))
            {
                return false;
            }

            lista.Add(valor);
            return true;
        }

        /// <summary>Repara listas nulas (un JSON antiguo o editado a mano puede no traerlas).</summary>
        public void Normalizar()
        {
            nivelesCompletados ??= new List<int>();
            fragmentos ??= new List<string>();
            habilidades ??= new List<string>();
            checkpoint ??= new DatosCheckpoint();
            cinematicasVistas ??= new List<string>();
            memoriasActivadas ??= new List<string>();
            if (nivelMaximoAlcanzado < 1) nivelMaximoAlcanzado = 1;
            if (monedas < 0) monedas = 0;
        }
    }
}
