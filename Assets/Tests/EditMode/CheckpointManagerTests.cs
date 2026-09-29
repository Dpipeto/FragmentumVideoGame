using System.Collections.Generic;
using System.Text.RegularExpressions;
using Fragmentum.Core;
using Fragmentum.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Reglas de los checkpoints (tarea D8): dónde reaparece el jugador, qué se activa, qué se
    /// avisa y cómo se recupera el checkpoint de una partida guardada.
    /// </summary>
    public class CheckpointManagerTests
    {
        private readonly List<GameObject> _objetos = new List<GameObject>();
        private int _avisos;

        [SetUp]
        public void Preparar()
        {
            EventosJuego.Reiniciar();
            _avisos = 0;
            EventosJuego.CheckpointActivado += _ => _avisos++;
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (var objeto in _objetos)
            {
                if (objeto != null)
                {
                    Object.DestroyImmediate(objeto);
                }
            }

            _objetos.Clear();
            EventosJuego.Reiniciar();
        }

        [Test]
        public void SinCheckpointActivo_ReapareceEnElInicio()
        {
            var gestor = CrearGestor();
            gestor.PuntoInicio = new Vector2(3f, 0.05f);

            Assert.IsNull(gestor.Activo);
            Assert.AreEqual(new Vector2(3f, 0.05f), gestor.PuntoReaparicion);
        }

        [Test]
        public void Inicializar_EncuentraLosCheckpointsDeLaEscena()
        {
            CrearCheckpoint("L1_CP1", 40f);
            CrearCheckpoint("L1_CP2", 60f);

            var gestor = CrearGestor();

            Assert.AreEqual(2, gestor.Checkpoints.Count, "No hace falta registrarlos a mano.");
        }

        [Test]
        public void Activar_LoHaceActivo_YSoloAvisaUnaVez()
        {
            var primero = CrearCheckpoint("L1_CP1", 40f);
            var segundo = CrearCheckpoint("L1_CP2", 60f);
            var gestor = CrearGestor();

            gestor.Activar(primero);
            Assert.AreSame(primero, gestor.Activo);
            Assert.AreEqual(primero.PuntoReaparicion, gestor.PuntoReaparicion);
            Assert.IsTrue(primero.Activo);

            gestor.Activar(primero);
            Assert.AreEqual(1, _avisos, "Volver a tocar el checkpoint activo no hace nada.");

            gestor.Activar(segundo);
            Assert.AreSame(segundo, gestor.Activo);
            Assert.IsFalse(primero.Activo, "Solo hay un checkpoint activo.");
            Assert.AreEqual(2, _avisos);
        }

        [Test]
        public void Restaurar_ActivaElCheckpointGuardado_SinAvisar()
        {
            CrearCheckpoint("L1_CP1", 40f);
            var segundo = CrearCheckpoint("L1_CP2", 60f);
            var gestor = CrearGestor();

            Assert.IsTrue(gestor.Restaurar("L1_CP2"));

            Assert.AreSame(segundo, gestor.Activo);
            Assert.AreEqual(0, _avisos, "Restaurar no es activar: no se vuelve a guardar ni a avisar.");
        }

        [Test]
        public void Restaurar_UnCheckpointQueYaNoExiste_AvisaYEmpiezaDesdeElInicio()
        {
            CrearCheckpoint("L1_CP1", 40f);
            var gestor = CrearGestor();
            gestor.PuntoInicio = new Vector2(2f, 0.05f);

            LogAssert.Expect(LogType.Warning, new Regex("ya no existe"));
            Assert.IsFalse(gestor.Restaurar("L1_CP9"));

            Assert.IsNull(gestor.Activo);
            Assert.AreEqual(new Vector2(2f, 0.05f), gestor.PuntoReaparicion);
        }

        [Test]
        public void DosCheckpointsConElMismoId_SeAvisa()
        {
            CrearCheckpoint("L1_CP1", 40f);
            CrearCheckpoint("L1_CP1", 60f);

            LogAssert.Expect(LogType.Warning, new Regex("dos checkpoints con el id 'L1_CP1'"));
            CrearGestor();
        }

        [Test]
        public void SinId_SeDerivaDeLaPosicion()
        {
            var checkpoint = CrearCheckpoint(string.Empty, 12.4f, 3f);

            Assert.AreEqual("CP_12_3", checkpoint.Id);
        }

        private CheckpointManager CrearGestor()
        {
            var objeto = new GameObject("Nivel");
            _objetos.Add(objeto);
            var gestor = objeto.AddComponent<CheckpointManager>();
            gestor.Inicializar();
            return gestor;
        }

        private Checkpoint CrearCheckpoint(string id, float x, float y = 0.05f)
        {
            var objeto = new GameObject(string.IsNullOrEmpty(id) ? "Checkpoint" : id);
            _objetos.Add(objeto);
            objeto.transform.position = new Vector3(x, y, 0f);
            objeto.AddComponent<BoxCollider2D>().isTrigger = true;
            var checkpoint = objeto.AddComponent<Checkpoint>();

            var serializado = new SerializedObject(checkpoint);
            serializado.FindProperty("id").stringValue = id;
            serializado.ApplyModifiedPropertiesWithoutUndo();
            return checkpoint;
        }
    }
}
