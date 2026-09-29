using UnityEngine;

namespace Fragmentum.Enemies.Deteccion
{
    /// <summary>
    /// Lo que un enemigo sabe del jugador en un instante. Lo prepara <c>EnemyController</c> y
    /// lo consulta la estrategia de detección, que así no necesita conocer ni al enemigo ni al
    /// jugador (y se puede probar sin escena).
    /// </summary>
    public readonly struct ContextoDeteccion
    {
        public ContextoDeteccion(Vector2 origen, Vector2 objetivo, float radio, bool objetivoVivo,
            bool objetivoOculto, float ahora, float tiempoUltimoRuido, int mascaraObstaculos)
        {
            Origen = origen;
            Objetivo = objetivo;
            Radio = radio;
            ObjetivoVivo = objetivoVivo;
            ObjetivoOculto = objetivoOculto;
            Ahora = ahora;
            TiempoUltimoRuido = tiempoUltimoRuido;
            MascaraObstaculos = mascaraObstaculos;
        }

        /// <summary>Ojos del enemigo (centro de su cuerpo).</summary>
        public Vector2 Origen { get; }

        /// <summary>Centro del cuerpo del jugador.</summary>
        public Vector2 Objetivo { get; }

        /// <summary>Radio de detección. Mientras persigue ya viene ampliado por la histéresis.</summary>
        public float Radio { get; }

        public bool ObjetivoVivo { get; }

        /// <summary>El jugador usa la Invisibilidad Parcial.</summary>
        public bool ObjetivoOculto { get; }

        public float Ahora { get; }

        /// <summary>Último instante en que este enemigo oyó ruido del jugador.</summary>
        public float TiempoUltimoRuido { get; }

        /// <summary>Capas que tapan la vista (suelo y paredes).</summary>
        public int MascaraObstaculos { get; }

        public float Distancia => Vector2.Distance(Origen, Objetivo);

        /// <summary>Vivo y dentro del radio: la condición base de casi todas las estrategias.</summary>
        public bool EnRadio => ObjetivoVivo && Distancia <= Radio;
    }

    /// <summary>
    /// Estrategia de detección del jugador (patrón Strategy como ScriptableObject). Cada
    /// <c>EnemyData</c> apunta a una, y así un enemigo de la Ira, uno de la Tristeza y uno del
    /// Miedo se comportan distinto sin duplicar el controlador ni añadir condicionales por tipo.
    ///
    /// El asset se comparte entre enemigos, así que no guarda estado: todo lo que necesita
    /// llega en el <see cref="ContextoDeteccion"/>.
    /// </summary>
    public abstract class DeteccionData : ScriptableObject
    {
        [TextArea(2, 4)]
        [Tooltip("Para qué tipo de enemigo sirve; solo documentación.")]
        [SerializeField] private string descripcion;

        public string Descripcion => descripcion;

        /// <summary>¿Detecta al jugador con este contexto?</summary>
        public abstract bool Detecta(in ContextoDeteccion contexto);

        /// <summary>Comportamiento cuando un enemigo no tiene estrategia asignada: por distancia.</summary>
        public static bool PorDefecto(in ContextoDeteccion contexto) =>
            contexto.EnRadio && !contexto.ObjetivoOculto;
    }
}
