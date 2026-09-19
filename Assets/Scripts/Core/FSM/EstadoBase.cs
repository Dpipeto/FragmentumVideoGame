using System;

namespace Fragmentum.Core.FSM
{
    /// <summary>
    /// Base cómoda para los estados: guarda el dueño (el controlador que aporta datos y
    /// componentes) y la máquina, y deja todos los métodos vacíos para que cada estado
    /// redefina solo lo que necesita.
    /// </summary>
    /// <typeparam name="TEstado">Enumeración de estados.</typeparam>
    /// <typeparam name="TDueno">Controlador dueño de la máquina (por ejemplo, PlayerController).</typeparam>
    public abstract class EstadoBase<TEstado, TDueno> : IEstado<TEstado> where TEstado : struct, Enum
    {
        /// <summary>Controlador que aporta componentes, datos y servicios al estado.</summary>
        protected readonly TDueno Dueno;

        /// <summary>Máquina a la que pertenece el estado. Se usa para consultar, no para transitar.</summary>
        protected readonly MaquinaEstados<TEstado> Maquina;

        protected EstadoBase(TEstado id, TDueno dueno, MaquinaEstados<TEstado> maquina)
        {
            Id = id;
            Dueno = dueno;
            Maquina = maquina;
        }

        public TEstado Id { get; }

        /// <summary>Segundos transcurridos desde que se entró en el estado actual.</summary>
        protected float TiempoEnEstado => Maquina.TiempoEnEstado;

        public virtual void OnEnter() { }

        public virtual void OnExit() { }

        public virtual void Actualizar() { }

        public virtual void ActualizarFisica() { }

        public virtual TEstado? EvaluarTransiciones() => null;
    }
}
