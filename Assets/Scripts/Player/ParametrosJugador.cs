using System;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// Valores de ajuste del jugador, agrupados para verlos juntos en el Inspector.
    ///
    /// Están pensados para la escala del proyecto v2: 1 tile = 1 unidad. Con los valores por
    /// defecto el salto sube unos 2,5 tiles, el doble salto 1,7 más y el dash recorre unos 3.
    /// </summary>
    [Serializable]
    public class ParametrosJugador
    {
        [Header("Carrera")]
        [Tooltip("Velocidad horizontal máxima, en unidades por segundo.")]
        public float velocidadMaxima = 6f;

        [Tooltip("Aceleración en el suelo (u/s²). Alta = respuesta inmediata.")]
        public float aceleracionSuelo = 70f;

        [Tooltip("Frenado en el suelo al soltar la dirección (u/s²).")]
        public float desaceleracionSuelo = 80f;

        [Tooltip("Aceleración en el aire (u/s²).")]
        public float aceleracionAire = 45f;

        [Tooltip("Frenado en el aire al soltar la dirección (u/s²).")]
        public float desaceleracionAire = 30f;

        [Tooltip("Por debajo de este valor el eje horizontal cuenta como 'sin input'.")]
        [Range(0f, 0.5f)] public float zonaMuerta = 0.2f;

        [Header("Salto")]
        [Tooltip("Velocidad vertical al saltar desde el suelo.")]
        public float velocidadSalto = 12f;

        [Tooltip("Velocidad vertical del doble salto.")]
        public float velocidadDobleSalto = 10f;

        [Tooltip("Al soltar el salto mientras se sube, la velocidad vertical se multiplica por esto (salto de altura variable).")]
        [Range(0f, 1f)] public float cortePorSoltarSalto = 0.5f;

        [Tooltip("Coyote time: segundos tras salir de un borde en los que todavía se puede saltar.")]
        public float coyoteTime = 0.1f;

        [Tooltip("Jump buffer: una pulsación de salto hecha hasta estos segundos antes de aterrizar también salta.")]
        public float jumpBuffer = 0.1f;

        [Tooltip("Escala de gravedad del Rigidbody2D.")]
        public float escalaGravedad = 3f;

        [Tooltip("Multiplicador de gravedad al caer: caer más rápido que subir da un salto menos flotante.")]
        public float multiplicadorGravedadCaida = 1.5f;

        [Tooltip("Velocidad máxima de caída.")]
        public float velocidadCaidaMaxima = 18f;

        [Header("Pared")]
        [Tooltip("Velocidad máxima de caída mientras se desliza por una pared.")]
        public float velocidadWallSlide = 2.5f;

        [Tooltip("Velocidad del wall jump: X hacia fuera de la pared, Y hacia arriba.")]
        public Vector2 impulsoWallJump = new Vector2(7f, 12f);

        [Tooltip("Segundos en los que el input horizontal queda bloqueado tras un wall jump.")]
        public float wallJumpDuration = 0.2f;

        [Header("Dash (Shift izquierdo)")]
        public float velocidadDash = 16f;
        public float dashDuration = 0.18f;

        [Tooltip("Segundos de espera entre dashes. En el prototipo este valor se sobrescribía con 1 al usarlo.")]
        public float dashCooldown = 0.6f;

        [Header("Ataque")]
        [Tooltip("Duración total del ataque. Es un límite por código: un evento de animación perdido no bloquea al jugador.")]
        public float duracionAtaque = 0.35f;

        [Tooltip("Momento del ataque en que se activa la hitbox de la espada.")]
        public float inicioVentanaGolpe = 0.08f;

        [Tooltip("Momento del ataque en que se desactiva la hitbox de la espada.")]
        public float finVentanaGolpe = 0.22f;

        [Tooltip("Duración del ataque cargado (Ira). Es más lento que el normal.")]
        public float duracionAtaqueCargado = 0.5f;

        [Header("Daño")]
        [Tooltip("Segundos sin control tras recibir un golpe.")]
        public float damagedDuration = 0.3f;

        [Tooltip("Velocidad del retroceso al recibir un golpe: X alejándose del atacante, Y hacia arriba.")]
        public Vector2 impulsoRetroceso = new Vector2(5f, 6f);

        [Header("Agacharse (cobertura del mundo de la Culpa)")]
        [Tooltip("Altura del cuerpo agachado respecto de la altura normal.")]
        [Range(0.3f, 1f)] public float alturaAgachado = 0.5f;

        [Tooltip("Velocidad al avanzar agachado. 0 = cobertura estática.")]
        public float velocidadAgachado = 2f;
    }
}
