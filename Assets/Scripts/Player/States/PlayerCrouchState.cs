using Fragmentum.Core.FSM;

namespace Fragmentum.Player.States
{
    /// <summary>
    /// CROUCH: agachado (cobertura del mundo de la Culpa). Encoge el collider y solo deja
    /// levantarse, atacar o hacer dash si hay espacio arriba, para no quedar incrustado en
    /// un techo bajo.
    ///
    /// Es un estado de la FSM y no una bandera: así lo pedía el encargo para las mecánicas
    /// de nivel.
    /// </summary>
    public sealed class PlayerCrouchState : PlayerLocomotionState
    {
        public PlayerCrouchState(PlayerController dueno, MaquinaEstados<ESTADOS_JUGADOR> maquina)
            : base(ESTADOS_JUGADOR.CROUCH, dueno, maquina)
        {
        }

        public override void OnEnter()
        {
            Dueno.RecargarSaltos();
            Dueno.Motor.FijarAgachado(true);
        }

        public override void OnExit() => Dueno.Motor.FijarAgachado(false);

        public override void Actualizar() => Dueno.Mirar(Dueno.DireccionEntrada);

        public override void ActualizarFisica() =>
            Dueno.Motor.MoverHorizontal(Dueno.EjeHorizontal, Dueno.P.velocidadAgachado, true);

        protected override bool PuedeAccionar() => HayEspacioArriba();

        protected override ESTADOS_JUGADOR? EvaluarLocomocion()
        {
            if (!Dueno.Sensores.EnSuelo)
            {
                Dueno.IniciarCoyote();
                return ESTADOS_JUGADOR.FALL;
            }

            if (!Dueno.QuiereAgacharse() && HayEspacioArriba())
            {
                return ESTADOS_JUGADOR.IDLE;
            }

            return null;
        }

        private bool HayEspacioArriba() => Dueno.Sensores.HayEspacioArriba(Dueno.Motor.AlturaDePie);
    }
}
