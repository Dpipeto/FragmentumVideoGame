using System.Collections.Generic;
using Fragmentum.Core.Save;
using Fragmentum.Data;
using NUnit.Framework;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Reglas de la base de datos de niveles con datos en memoria: desbloqueo secuencial,
    /// orden, búsqueda por escena y validación de la configuración.
    /// </summary>
    public class LevelDatabaseTests
    {
        private readonly List<Object> _creados = new List<Object>();
        private LevelDatabase _bd;
        private EmocionData _emocion;

        [SetUp]
        public void Preparar()
        {
            _emocion = Crear<EmocionData>();
            _bd = Crear<LevelDatabase>();
            for (var id = 1; id <= 3; id++)
            {
                var nivel = Crear<LevelData>();
                nivel.id = id;
                nivel.name = $"L{id}";
                nivel.emocion = _emocion;
                _bd.niveles.Add(nivel);
            }
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (var objeto in _creados) Object.DestroyImmediate(objeto);
            _creados.Clear();
        }

        private T Crear<T>() where T : ScriptableObject
        {
            var objeto = ScriptableObject.CreateInstance<T>();
            _creados.Add(objeto);
            return objeto;
        }

        [Test]
        public void PartidaNueva_SoloTieneDesbloqueadoElPrimerNivel()
        {
            var datos = new DatosGuardado();

            Assert.IsTrue(_bd.EstaDesbloqueado(_bd.niveles[0], datos));
            Assert.IsFalse(_bd.EstaDesbloqueado(_bd.niveles[1], datos));
            Assert.IsFalse(_bd.EstaDesbloqueado(_bd.niveles[2], datos), "No se puede entrar al final desde el principio.");
        }

        [Test]
        public void ElDesbloqueo_EsSecuencial()
        {
            var datos = new DatosGuardado { nivelMaximoAlcanzado = 2 };

            Assert.IsTrue(_bd.EstaDesbloqueado(_bd.niveles[1], datos));
            Assert.IsFalse(_bd.EstaDesbloqueado(_bd.niveles[2], datos));
        }

        [Test]
        public void Siguiente_RespetaElOrden_YElUltimoNoTieneSiguiente()
        {
            Assert.AreSame(_bd.niveles[1], _bd.Siguiente(_bd.niveles[0]));
            Assert.IsNull(_bd.Siguiente(_bd.niveles[2]));
        }

        [Test]
        public void PorEscena_DevuelveNullSiLaEscenaNoEsUnNivel()
        {
            Assert.IsNull(_bd.PorEscena("PruebaJugador"));
            Assert.IsNull(_bd.PorEscena(string.Empty));
        }

        [Test]
        public void SinEscena_ElNivelNoEsJugable_YParaContinuarNoLoElige()
        {
            Assert.IsFalse(_bd.niveles[0].Jugable, "Un nivel sin escena está «en construcción».");
            Assert.IsNull(_bd.ParaContinuar(new DatosGuardado { nivelMaximoAlcanzado = 3 }));
        }

        [Test]
        public void Validar_DetectaIdsRepetidosYDesordenados_YNivelesSinEmocion()
        {
            _bd.niveles[2].id = 2;
            _bd.niveles[1].emocion = null;

            var problemas = string.Join("\n", _bd.Validar());

            StringAssert.Contains("repetido", problemas);
            StringAssert.Contains("ordenados", problemas);
            StringAssert.Contains("no tiene emoción", problemas);
            StringAssert.Contains("Falta la escena del menú", problemas);
        }
    }
}
