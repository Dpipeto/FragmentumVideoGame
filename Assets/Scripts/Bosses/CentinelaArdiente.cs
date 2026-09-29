namespace Fragmentum.Bosses
{
    /// <summary>
    /// Centinela Ardiente, jefe del mundo de la Ira (Anexo A). Es la implementación de referencia.
    ///
    /// - <b>Fase 1:</b> martillazo directo, que deja el martillo clavado 1,2 s (ventana
    ///   vulnerable), y onda de suelo.
    /// - <b>Fase 2</b> (50 % de vida): además, plataformas en erupción que obligan a moverse.
    /// - <b>Muerte:</b> explosión (provisional: destello y desvanecido) y «el suelo se
    ///   estabiliza»: al cortarse los ataques desaparecen todas las erupciones.
    /// - <b>Recompensa:</b> Fragmento de la Ira → Ataque Cargado. La otorga la arena al recibir
    ///   <c>OnJefeDerrotado</c>.
    ///
    /// Casi todo es dato: los patrones y los umbrales están en <c>Assets/Data/Jefes</c>. La clase
    /// solo redefine los pasos propios de este jefe (patrón Template Method).
    /// </summary>
    public class CentinelaArdiente : BossController
    {
        /// <summary>Al entrar en la fase 2 ruge: un aviso largo antes de que empiecen las erupciones.</summary>
        protected override void AlCambiarDeFase(int fase)
        {
            if (fase >= 2)
            {
                Avisar(0.8f);
            }
        }
    }
}
