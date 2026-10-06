using Fragmentum.Enemies.Deteccion;
using UnityEngine;

namespace Fragmentum.Data
{
    /// <summary>
    /// Parámetros de un tipo de enemigo (patrón Type Object). El mismo <c>EnemyController</c>
    /// sirve para todos: un Soldado de lava agresivo y rápido, un Lamento lento y pesado o un
    /// Espectro que solo se acerca en la oscuridad son assets distintos, no scripts distintos.
    /// Así la correspondencia emoción ↔ mecánica queda en los datos del mundo.
    /// </summary>
    [CreateAssetMenu(menuName = "Fragmentum/Enemigo", fileName = "Enemigo")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identidad")]
        public string nombreCanonico = "Enemigo";

        [TextArea(2, 4)]
        [Tooltip("Qué emoción encarna y cómo se nota al jugarlo.")]
        public string descripcion;

        [Header("Vida y daño")]
        [Min(1)] public int vidaMaxima = 3;

        [Tooltip("Daño del ataque.")]
        [Min(0)] public int danio = 1;

        [Tooltip("Daño por tocar su cuerpo. 0 = tocarlo no hace daño.")]
        [Min(0)] public int danioContacto = 1;

        [Tooltip("Invulnerabilidad tras recibir un golpe (i-frames).")]
        [Min(0f)] public float invulnerabilidad = 0.3f;

        [Header("Movimiento")]
        public float velocidadPatrulla = 1.5f;
        public float velocidadPersecucion = 3f;

        [Tooltip("Distancia a la que se considera alcanzado un waypoint.")]
        public float toleranciaWaypoint = 0.2f;

        [Tooltip("Pausa en cada waypoint. 0 = patrulla sin detenerse.")]
        public float esperaEnWaypoint = 1f;

        [Tooltip("No avanza si delante hay un borde o una pared (no se tira a la lava).")]
        public bool evitarBordes = true;

        [Tooltip("La lava no le hace daño y la cruza andando (Golem de Ceniza). Caer al vacío sí lo mata.")]
        public bool inmuneALava;

        [Header("Detección")]
        [Tooltip("Estrategia de detección. Vacío = por distancia.")]
        public DeteccionData deteccion;

        public float detectionRadius = 5f;

        [Tooltip("Histéresis: deja de perseguir solo cuando el jugador se aleja más de detectionRadius × este factor.")]
        [Min(1f)] public float factorHisteresis = 1.5f;

        [Header("Ataque")]
        [Tooltip("Distancia horizontal a la que ataca.")]
        public float attackRadius = 1.3f;

        [Tooltip("Diferencia de altura máxima para atacar.")]
        public float alturaAtaque = 1.5f;

        [Tooltip("Segundos entre el final de un ataque y el comienzo del siguiente.")]
        public float attackCooldown = 1.2f;

        [Tooltip("Duración total del ataque.")]
        public float duracionAtaque = 0.5f;

        [Tooltip("Instante del ataque en que se abre la hitbox del arma (telegrafía el golpe).")]
        public float inicioVentanaGolpe = 0.25f;

        [Tooltip("Instante del ataque en que se cierra la hitbox del arma.")]
        public float finVentanaGolpe = 0.4f;

        [Header("Daño recibido")]
        [Tooltip("Hiperarmadura: los golpes le quitan vida y lo hacen destellar, pero no lo aturden ni cortan su ataque " +
                 "(Golem de Ceniza: sin animación de daño y con 10 de vida, si cada golpe lo aturdiera apenas llegaría a atacar).")]
        public bool hiperarmadura;

        [Tooltip("Segundos de aturdimiento tras un golpe. Interrumpe el ataque. Sin efecto con hiperarmadura.")]
        public float damagedDuration = 0.35f;

        [Tooltip("Velocidad del retroceso al recibir un golpe.")]
        public Vector2 retroceso = new Vector2(4f, 3f);

        [Tooltip("0 = sale despedido; 1 = no se mueve (enemigos pesados).")]
        [Range(0f, 1f)] public float resistenciaRetroceso;

        [Tooltip("Segundos entre la muerte y la desaparición del objeto.")]
        public float deathDelay = 1f;

        [Header("Presentación")]
        [Tooltip("Márcalo si el arte original mira a la derecha. El prototipo lo tenía al revés.")]
        public bool spriteMiraDerecha = true;

        [Tooltip("Tinte del sprite. Mientras no hay arte distingue las variantes; con arte final, blanco.")]
        public Color tinte = Color.white;
    }
}
