using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Pruebas de las habilidades desbloqueables: el Ataque Cargado (pulsar, mantener,
    /// soltar), la Invisibilidad Parcial (3 s de duración, 8 s de espera) y el enganche de la
    /// Defensa Reforzada con la vida.
    /// </summary>
    public class PlayerAbilitiesTests
    {
        private GameObject _objeto;
        private PlayerHealth _vida;
        private PlayerAbilities _habilidades;
        private float _tiempo;

        [SetUp]
        public void Preparar()
        {
            _tiempo = 0f;
            _objeto = new GameObject("JugadorDePrueba");
            _vida = _objeto.AddComponent<PlayerHealth>();
            _vida.Reloj = () => _tiempo;
            _vida.Inicializar(3);
            _habilidades = _objeto.AddComponent<PlayerAbilities>();
            _habilidades.Reloj = () => _tiempo;
        }

        [TearDown]
        public void Limpiar() => Object.DestroyImmediate(_objeto);

        /// <summary>Simula pulsar en t=0, mantener hasta <paramref name="soltarEn"/> y soltar.</summary>
        private void PulsarMantenerYSoltar(float soltarEn)
        {
            _tiempo = 0f;
            _habilidades.ActualizarCarga(pulsado: true, mantenido: true);
            _tiempo = soltarEn;
            _habilidades.ActualizarCarga(pulsado: false, mantenido: true);
            _habilidades.ActualizarCarga(pulsado: false, mantenido: false);
        }

        [Test]
        public void SinLaHabilidad_NoHayAtaqueCargado()
        {
            PulsarMantenerYSoltar(2f);

            Assert.IsFalse(_habilidades.HayAtaqueCargadoPendiente);
        }

        [Test]
        public void CargaCompleta_AlSoltar_DejaUnAtaqueCargadoQueSeGastaUnaVez()
        {
            _habilidades.Desbloquear(HABILIDADES.ATAQUE_CARGADO);

            _habilidades.ActualizarCarga(pulsado: true, mantenido: true);
            _tiempo = 0.3f;
            Assert.IsFalse(_habilidades.CargaCompleta);
            _tiempo = 0.7f;
            Assert.IsTrue(_habilidades.CargaCompleta);

            _habilidades.ActualizarCarga(pulsado: false, mantenido: false);

            Assert.IsTrue(_habilidades.ConsumirAtaqueCargado());
            Assert.IsFalse(_habilidades.ConsumirAtaqueCargado(), "La carga se gasta al usarla.");
        }

        [Test]
        public void SoltarAntesDeTiempo_NoCarga()
        {
            _habilidades.Desbloquear(HABILIDADES.ATAQUE_CARGADO);

            PulsarMantenerYSoltar(0.3f);

            Assert.IsFalse(_habilidades.HayAtaqueCargadoPendiente);
        }

        [Test]
        public void LaCargaSoltada_CaducaSiNoSeUsa()
        {
            _habilidades.Desbloquear(HABILIDADES.ATAQUE_CARGADO);
            PulsarMantenerYSoltar(0.7f);
            Assert.IsTrue(_habilidades.HayAtaqueCargadoPendiente);

            _tiempo = 2f;
            Assert.IsFalse(_habilidades.HayAtaqueCargadoPendiente);
        }

        [Test]
        public void RecibirDanio_CancelaLaCarga()
        {
            _habilidades.Desbloquear(HABILIDADES.ATAQUE_CARGADO);
            _habilidades.ActualizarCarga(pulsado: true, mantenido: true);
            _tiempo = 0.7f;

            _habilidades.CancelarCarga();
            _habilidades.ActualizarCarga(pulsado: false, mantenido: false);

            Assert.IsFalse(_habilidades.HayAtaqueCargadoPendiente);
        }

        [Test]
        public void Invisibilidad_Dura3s_YEspera8sMas()
        {
            Assert.IsFalse(_habilidades.ActivarInvisibilidad(), "Sin desbloquear no se activa.");

            _habilidades.Desbloquear(HABILIDADES.INVISIBILIDAD_PARCIAL);
            Assert.IsTrue(_habilidades.ActivarInvisibilidad());

            _tiempo = 2.9f;
            Assert.IsTrue(_habilidades.EsInvisible);
            Assert.IsFalse(_habilidades.ActivarInvisibilidad(), "No se reactiva mientras dura.");

            _tiempo = 3.1f;
            Assert.IsFalse(_habilidades.EsInvisible);

            _tiempo = 10.9f;
            Assert.IsFalse(_habilidades.InvisibilidadLista);

            _tiempo = 11f;
            Assert.IsTrue(_habilidades.InvisibilidadLista);
        }

        [Test]
        public void DesbloquearDefensaReforzada_ActivaElEscudoDeLaVida()
        {
            Assert.IsFalse(_vida.EscudoDesbloqueado);

            _habilidades.Desbloquear(HABILIDADES.DEFENSA_REFORZADA);

            Assert.IsTrue(_vida.EscudoDesbloqueado);
        }

        [Test]
        public void FijarDesbloqueadas_SustituyeElConjuntoCompleto()
        {
            _habilidades.Desbloquear(HABILIDADES.DEFENSA_REFORZADA);

            _habilidades.FijarDesbloqueadas(new[] { HABILIDADES.DASH_ACUATICO });

            Assert.IsTrue(_habilidades.Tiene(HABILIDADES.DASH_ACUATICO));
            Assert.IsFalse(_habilidades.Tiene(HABILIDADES.DEFENSA_REFORZADA));
            Assert.IsFalse(_vida.EscudoDesbloqueado, "Al perder la habilidad, el escudo se desactiva.");
        }
    }
}
