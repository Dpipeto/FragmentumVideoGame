using Fragmentum.Core;
using Fragmentum.Enemies.Deteccion;
using NUnit.Framework;
using UnityEngine;

namespace Fragmentum.Tests
{
    /// <summary>
    /// Pruebas de las estrategias de detección que no necesitan física (la de visión se
    /// prueba en PlayMode, con muros reales).
    /// </summary>
    public class DeteccionTests
    {
        private sealed class LuzDePrueba : IFuenteLuz
        {
            public Vector2 Posicion { get; set; }
            public float Radio { get; set; } = 3f;
            public bool Encendida { get; set; } = true;
        }

        private DeteccionPorDistancia _distancia;
        private DeteccionEnOscuridad _oscuridad;
        private DeteccionPorRuido _ruido;

        [SetUp]
        public void Preparar()
        {
            Iluminacion.Reiniciar();
            _distancia = ScriptableObject.CreateInstance<DeteccionPorDistancia>();
            _oscuridad = ScriptableObject.CreateInstance<DeteccionEnOscuridad>();
            _ruido = ScriptableObject.CreateInstance<DeteccionPorRuido>();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(_distancia);
            Object.DestroyImmediate(_oscuridad);
            Object.DestroyImmediate(_ruido);
            Iluminacion.Reiniciar();
        }

        private static ContextoDeteccion Contexto(float distancia, bool vivo = true, bool oculto = false,
            float ahora = 10f, float ultimoRuido = float.NegativeInfinity) =>
            new ContextoDeteccion(Vector2.zero, new Vector2(distancia, 0f), 5f, vivo, oculto, ahora, ultimoRuido, 0);

        [Test]
        public void PorDistancia_DetectaDentroDelRadio()
        {
            Assert.IsTrue(_distancia.Detecta(Contexto(4f)));
            Assert.IsFalse(_distancia.Detecta(Contexto(6f)));
        }

        [Test]
        public void PorDistancia_NoDetectaAlJugadorMuertoNiInvisible()
        {
            Assert.IsFalse(_distancia.Detecta(Contexto(2f, vivo: false)));
            Assert.IsFalse(_distancia.Detecta(Contexto(2f, oculto: true)), "La Invisibilidad Parcial engaña a la vista.");
        }

        [Test]
        public void EnOscuridad_IgnoraAlJugadorIluminado()
        {
            var luz = new LuzDePrueba { Posicion = new Vector2(3f, 0f) };
            Iluminacion.Registrar(luz);

            Assert.IsFalse(_oscuridad.Detecta(Contexto(3f)), "En la luz, el Espectro no se acerca.");

            luz.Encendida = false;
            Assert.IsTrue(_oscuridad.Detecta(Contexto(3f)), "Al apagarse la luz, sí.");
        }

        [Test]
        public void PorRuido_OyeDuranteSuMemoria_YNotaElContacto()
        {
            Assert.IsFalse(_ruido.Detecta(Contexto(4f)), "Sin ruido no detecta a distancia.");
            Assert.IsTrue(_ruido.Detecta(Contexto(4f, ultimoRuido: 9f)), "Oyó ruido hace 1 s.");
            Assert.IsFalse(_ruido.Detecta(Contexto(4f, ultimoRuido: 5f)), "El ruido de hace 5 s ya se olvidó.");
            Assert.IsTrue(_ruido.Detecta(Contexto(1f)), "Pegado a él lo nota aunque no haga ruido.");
        }

        [Test]
        public void PorRuido_NoLoEngañaLaInvisibilidad()
        {
            Assert.IsTrue(_ruido.Detecta(Contexto(4f, oculto: true, ultimoRuido: 9.5f)),
                "El Centinela Ciego no ve: la invisibilidad no le afecta.");
        }

        [Test]
        public void SinEstrategia_SeUsaLaDetecionPorDistancia()
        {
            Assert.IsTrue(DeteccionData.PorDefecto(Contexto(4f)));
            Assert.IsFalse(DeteccionData.PorDefecto(Contexto(6f)));
        }
    }
}
