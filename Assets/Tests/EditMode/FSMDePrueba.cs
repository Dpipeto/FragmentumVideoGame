using System.Collections.Generic;
using Fragmentum.Core.FSM;

namespace Fragmentum.Tests
{
    /// <summary>Estados de juguete para probar el núcleo de las FSM.</summary>
    public enum ESTADOS_PRUEBA
    {
        IDLE,
        RUN,
        DASH,
        DEAD
    }

    /// <summary>Dueño falso: solo sirve para comprobar que el estado lo recibe.</summary>
    public class DuenoDePrueba
    {
        public int Contador;
    }

    /// <summary>
    /// Estado espía: apunta el orden en que se le llama y puede pedir una transición
    /// concreta en la siguiente evaluación.
    /// </summary>
    public class EstadoEspia : EstadoBase<ESTADOS_PRUEBA, DuenoDePrueba>
    {
        private readonly List<string> _registro;

        public EstadoEspia(ESTADOS_PRUEBA id, DuenoDePrueba dueno,
            MaquinaEstados<ESTADOS_PRUEBA> maquina, List<string> registro)
            : base(id, dueno, maquina)
        {
            _registro = registro;
        }

        /// <summary>Estado que pedirá la próxima vez que se evalúe; null para quedarse.</summary>
        public ESTADOS_PRUEBA? Pedir { get; set; }

        public int VecesActualizado { get; private set; }

        public override void OnEnter() => _registro.Add($"{Id}.OnEnter");

        public override void OnExit() => _registro.Add($"{Id}.OnExit");

        public override void Actualizar()
        {
            VecesActualizado++;
            _registro.Add($"{Id}.Actualizar");
        }

        public override void ActualizarFisica() => _registro.Add($"{Id}.ActualizarFisica");

        public override ESTADOS_PRUEBA? EvaluarTransiciones()
        {
            var pedido = Pedir;
            Pedir = null;
            return pedido;
        }
    }
}
