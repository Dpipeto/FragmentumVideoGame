using System.Collections.Generic;
using System.Text.RegularExpressions;
using Fragmentum.Core.FSM;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Pruebas del núcleo de las FSM. Verifican las garantías en las que se apoya toda la
    /// arquitectura: un único punto de transición, orden OnExit → OnEnter y rechazo de las
    /// transiciones que no estén declaradas.
    /// </summary>
    public class MaquinaEstadosTests
    {
        private List<string> _registro;
        private Dictionary<ESTADOS_PRUEBA, EstadoEspia> _estados;
        private MaquinaEstados<ESTADOS_PRUEBA> _maquina;
        private float _tiempo;

        [SetUp]
        public void Preparar()
        {
            _registro = new List<string>();
            _tiempo = 0f;

            var tabla = new TablaTransiciones<ESTADOS_PRUEBA>("Prueba")
                .Inicial(ESTADOS_PRUEBA.IDLE)
                .DefinirGrupo("VIVO", ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.RUN, ESTADOS_PRUEBA.DASH)
                .Permitir(ESTADOS_PRUEBA.IDLE, ESTADOS_PRUEBA.RUN, "hay input")
                .Permitir(ESTADOS_PRUEBA.RUN, ESTADOS_PRUEBA.IDLE, "sin input")
                .Permitir(ESTADOS_PRUEBA.RUN, ESTADOS_PRUEBA.DASH, "dash")
                .Permitir(ESTADOS_PRUEBA.DASH, ESTADOS_PRUEBA.IDLE, "termina el dash")
                .PermitirDesdeGrupo("VIVO", ESTADOS_PRUEBA.DEAD, "vida 0");

            _maquina = new MaquinaEstados<ESTADOS_PRUEBA>(tabla, () => _tiempo);

            var dueno = new DuenoDePrueba();
            _estados = new Dictionary<ESTADOS_PRUEBA, EstadoEspia>();
            foreach (ESTADOS_PRUEBA id in System.Enum.GetValues(typeof(ESTADOS_PRUEBA)))
            {
                var estado = new EstadoEspia(id, dueno, _maquina, _registro);
                _estados[id] = estado;
                _maquina.Registrar(estado);
            }

            _maquina.Iniciar(ESTADOS_PRUEBA.IDLE);
            _registro.Clear();
        }

        [Test]
        public void Iniciar_EntraEnElEstadoInicial()
        {
            Assert.AreEqual(ESTADOS_PRUEBA.IDLE, _maquina.EstadoActual);
            Assert.IsTrue(_maquina.Iniciada);
        }

        [Test]
        public void TransitionTo_EjecutaSalidaYEntradaEnOrden()
        {
            var cambios = new List<string>();
            _maquina.OnStateChanged += (anterior, nuevo) => cambios.Add($"{anterior}->{nuevo}");

            Assert.IsTrue(_maquina.TransitionTo(ESTADOS_PRUEBA.RUN));

            CollectionAssert.AreEqual(new[] { "IDLE.OnExit", "RUN.OnEnter" }, _registro);
            CollectionAssert.AreEqual(new[] { "IDLE->RUN" }, cambios);
            Assert.AreEqual(ESTADOS_PRUEBA.RUN, _maquina.EstadoActual);
            Assert.AreEqual(ESTADOS_PRUEBA.IDLE, _maquina.EstadoAnterior);
        }

        [Test]
        public void TransitionTo_RechazaLaTransicionNoDeclarada()
        {
            // IDLE -> DASH no está en la tabla: debe rechazarse y avisar por consola.
            LogAssert.Expect(LogType.Error, new Regex("Transición no declarada"));

            Assert.IsFalse(_maquina.TransitionTo(ESTADOS_PRUEBA.DASH));
            Assert.AreEqual(ESTADOS_PRUEBA.IDLE, _maquina.EstadoActual);
            CollectionAssert.IsEmpty(_registro, "Una transición rechazada no debe ejecutar OnExit ni OnEnter.");
        }

        [Test]
        public void Actualizar_AplicaLaTransicionQuePideElEstado()
        {
            _estados[ESTADOS_PRUEBA.IDLE].Pedir = ESTADOS_PRUEBA.RUN;

            _maquina.Actualizar();

            Assert.AreEqual(ESTADOS_PRUEBA.RUN, _maquina.EstadoActual);
            CollectionAssert.AreEqual(
                new[] { "IDLE.Actualizar", "IDLE.OnExit", "RUN.OnEnter" }, _registro);
        }

        [Test]
        public void Actualizar_EncadenaTransicionesEnElMismoFrame()
        {
            // RUN -> DASH -> IDLE en un solo frame, sin volver a ejecutar Actualizar.
            _maquina.TransitionTo(ESTADOS_PRUEBA.RUN);
            _registro.Clear();

            _estados[ESTADOS_PRUEBA.RUN].Pedir = ESTADOS_PRUEBA.DASH;
            _estados[ESTADOS_PRUEBA.DASH].Pedir = ESTADOS_PRUEBA.IDLE;

            _maquina.Actualizar();

            Assert.AreEqual(ESTADOS_PRUEBA.IDLE, _maquina.EstadoActual);
            Assert.AreEqual(1, _estados[ESTADOS_PRUEBA.RUN].VecesActualizado,
                "Solo debe ejecutarse el Actualizar del estado con el que empieza el frame.");
            Assert.AreEqual(0, _estados[ESTADOS_PRUEBA.DASH].VecesActualizado);
        }

        [Test]
        public void ActualizarFisica_SoloEjecutaElEstadoActual()
        {
            _maquina.ActualizarFisica();

            CollectionAssert.AreEqual(new[] { "IDLE.ActualizarFisica" }, _registro);
        }

        [Test]
        public void TiempoEnEstado_SeReiniciaAlCambiar()
        {
            _tiempo = 5f;
            Assert.AreEqual(5f, _maquina.TiempoEnEstado, 0.001f);

            _maquina.TransitionTo(ESTADOS_PRUEBA.RUN);
            Assert.AreEqual(0f, _maquina.TiempoEnEstado, 0.001f);

            _tiempo = 7.5f;
            Assert.AreEqual(2.5f, _maquina.TiempoEnEstado, 0.001f);
        }

        [Test]
        public void TransitionTo_DesdeGrupo_FuncionaDesdeCualquierEstadoDelGrupo()
        {
            _maquina.TransitionTo(ESTADOS_PRUEBA.RUN);
            Assert.IsTrue(_maquina.TransitionTo(ESTADOS_PRUEBA.DEAD),
                "PermitirDesdeGrupo debe habilitar la transición desde todos los estados del grupo.");
        }

    }
}
