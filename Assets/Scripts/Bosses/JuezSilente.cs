namespace Fragmentum.Bosses
{
    /// <summary>
    /// Juez Silente, jefe del mundo de la Culpa. <b>Esqueleto documentado</b>.
    ///
    /// Ya implementado: es inmune a los ataques directos (<c>inmuneFueraDeVulnerable</c> en sus
    /// datos) y solo se vuelve vulnerable cuando el jugador activa la memoria correcta:
    /// <see cref="ActivarMemoriaCorrecta"/> abre la ventana. Es la mecánica de la tesis: enfrentar
    /// la culpa es lo que permite avanzar.
    ///
    /// Pendiente (Fase 10):
    /// - Proyectar las memorias al fondo de la arena. Lo harán los <c>MemoryObject</c>, que
    ///   llamarán a <see cref="ActivarMemoriaCorrecta"/> o a <see cref="ActivarMemoriaFalsa"/>.
    /// - Fase 2 (50 %): dos memorias a la vez, una real y una trampa.
    /// - Sistema de consecuencias del nivel: sin memorias activadas, el juez tiene un 30 % más
    ///   de vida.
    /// - Muerte: todas las memorias se iluminan.
    /// </summary>
    public class JuezSilente : BossController
    {
        /// <summary>El jugador activó la memoria que corresponde al ataque en curso: el juez se expone.</summary>
        public void ActivarMemoriaCorrecta() => AbrirVentanaVulnerable();

        /// <summary>El jugador activó la memoria trampa. Castigo pendiente de diseño (Fase 10).</summary>
        public void ActivarMemoriaFalsa() => Avisar(1f);
    }
}
