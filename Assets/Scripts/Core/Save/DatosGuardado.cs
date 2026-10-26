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

    /// <summary>El mejor puntaje de un nivel ya superado, y lo que se tardó en hacerlo.</summary>
    [Serializable]
    public class DatosPuntaje
    {
        public int nivelId;
        public int puntos;
        public float segundos;
    }

    /// <summary>
    /// Foto del intento en curso, tomada al activar un checkpoint: los puntos por enemigos, el
    /// reloj y a quién se derrotó. Si se sale y se continúa desde el checkpoint, el intento sigue
    /// de aquí; así salir no pone el reloj a cero ni deja cobrar dos veces al mismo enemigo.
    /// </summary>
    [Serializable]
    public class DatosIntento
    {
        public int nivelId;
        public int puntos;
        public float segundos;
        public List<string> derrotados = new List<string>();
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
        public DatosCheckpoint checkpoint = new DatosCheckpoint();
        public List<string> cinematicasVistas = new List<string>();
        public List<string> memoriasActivadas = new List<string>();

        /// <summary>El mejor puntaje de cada nivel superado. Sustituye a las monedas de la v1.</summary>
        public List<DatosPuntaje> puntajes = new List<DatosPuntaje>();

        /// <summary>El intento en curso tal como estaba en el último checkpoint activado.</summary>
        public DatosIntento intento = new DatosIntento();

        /// <summary>¿Hay algo que continuar (más allá de una partida recién empezada)?</summary>
        public bool TieneProgreso =>
            nivelesCompletados.Count > 0 || nivelMaximoAlcanzado > 1 || !checkpoint.Vacio;

        /// <summary>La suma de los mejores puntajes de todos los niveles superados.</summary>
        public int PuntajeTotal
        {
            get
            {
                var total = 0;
                foreach (var puntaje in puntajes)
                {
                    total += puntaje.puntos;
                }

                return total;
            }
        }

        /// <summary>El mejor puntaje guardado de un nivel, o null si aún no se ha superado.</summary>
        public DatosPuntaje PuntajeDe(int nivelId) => puntajes.Find(p => p.nivelId == nivelId);

        /// <summary>
        /// Anota el puntaje de un nivel recién superado si mejora al guardado (a igualdad de
        /// puntos, si se tardó menos). Devuelve si quedó anotado.
        /// </summary>
        public bool AnotarPuntaje(int nivelId, int puntos, float segundos)
        {
            var guardado = PuntajeDe(nivelId);
            if (guardado == null)
            {
                puntajes.Add(new DatosPuntaje { nivelId = nivelId, puntos = puntos, segundos = segundos });
                return true;
            }

            if (puntos < guardado.puntos || (puntos == guardado.puntos && segundos >= guardado.segundos))
            {
                return false;
            }

            guardado.puntos = puntos;
            guardado.segundos = segundos;
            return true;
        }

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
            puntajes ??= new List<DatosPuntaje>();
            puntajes.RemoveAll(p => p == null);
            intento ??= new DatosIntento();
            intento.derrotados ??= new List<string>();
            if (nivelMaximoAlcanzado < 1) nivelMaximoAlcanzado = 1;
        }
    }
}
