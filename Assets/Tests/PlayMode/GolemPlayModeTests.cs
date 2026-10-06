using System.Collections;
using System.Collections.Generic;
using Fragmentum.Core.Combate;
using Fragmentum.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// El Golem de Ceniza jugando, con su prefab, el jugador real y física real:
    /// - la onda del puñetazo solo hace daño durante A3 (0,08 s), tras medio segundo de A1;
    /// - los golpes no le cortan el ataque (hiperarmadura), pero destella;
    /// - persigue andando y, parado a distancia de ataque, respira;
    /// - mira hacia Kael (está dibujado mirando a la derecha);
    /// - el contacto con su cuerpo hace daño siempre;
    /// - al morir se queda en D4 un momento y después se desvanece.
    /// </summary>
    public class GolemPlayModeTests : PruebaPlayModeBase
    {
        private const string RutaPrefab = "Assets/Prefabs/Enemigos/GolemCeniza.prefab";

        private EnemyController _golem;
        private EnemigoAnimacion _animacion;
        private SpriteRenderer _sprite;
        private GameObject _arma;
        private GameObject _contacto;

        private void CrearGolem(float x)
        {
            var objeto = Object.Instantiate(CargarPrefab(RutaPrefab), new Vector3(x, 0.05f, 0f), Quaternion.identity);
            Creados.Add(objeto);
            _golem = objeto.GetComponent<EnemyController>();
            _animacion = objeto.GetComponentInChildren<EnemigoAnimacion>();
            _sprite = _animacion.GetComponent<SpriteRenderer>();
            _arma = objeto.transform.Find("Pivote/Arma").gameObject;
            _contacto = objeto.transform.Find("Contacto").gameObject;
        }

        private string Fotograma => _sprite != null && _sprite.sprite != null ? _sprite.sprite.name : string.Empty;

        /// <summary>Instantes del primer ataque: cuándo empieza, cuándo se abre y se cierra la onda.</summary>
        private sealed class Ataque
        {
            public float Inicio = -1f, Apertura = -1f, Cierre = -1f;
            public string FotogramaDelDanio;
            public int Danio;
            public readonly List<string> OndaFueraDeA3 = new List<string>();
        }

        private IEnumerator ObservarAtaque(Ataque ataque, float segundos)
        {
            var vida = Jugador.Vida.Vida;
            yield return Durante(segundos, () =>
            {
                if (ataque.Inicio < 0f && _golem.EstadoActual == ESTADOS_ENEMIGO.ATTACK) ataque.Inicio = Time.time;
                if (_arma.activeSelf)
                {
                    if (ataque.Apertura < 0f) ataque.Apertura = Time.time;
                    if (Fotograma != "Golem_A3") ataque.OndaFueraDeA3.Add($"{Fotograma} (t = {Time.time:0.000})");
                }
                else if (ataque.Apertura >= 0f && ataque.Cierre < 0f)
                {
                    ataque.Cierre = Time.time;
                }

                if (ataque.FotogramaDelDanio == null && Jugador.Vida.Vida < vida)
                {
                    ataque.FotogramaDelDanio = Fotograma;
                    ataque.Danio = vida - Jugador.Vida.Vida;
                }
            });
        }

        [UnityTest]
        public IEnumerator LaOnda_SoloHaceDanioEnA3_TrasMedioSegundoDeA1()
        {
            Suelo(-12f, 12f);
            yield return CrearJugadorEnSuelo(new Vector2(1.6f, 0f));
            CrearGolem(0f);

            var ataque = new Ataque();
            yield return ObservarAtaque(ataque, 2f);

            Assert.Greater(ataque.Inicio, 0f, "Debe atacar al jugador que tiene a tiro.");
            Assert.IsEmpty(ataque.OndaFueraDeA3, "La onda solo puede hacer daño mientras se ve A3.");
            Assert.AreEqual(0.50f, ataque.Apertura - ataque.Inicio, 0.05f, "A1: medio segundo con los puños en alto.");
            Assert.AreEqual(0.08f, ataque.Cierre - ataque.Apertura, 0.04f, "A3: el impacto.");
            Assert.AreEqual("Golem_A3", ataque.FotogramaDelDanio, "El daño llega con el impacto.");
            Assert.AreEqual(_golem.Datos.danio, ataque.Danio);
        }

        [UnityTest]
        public IEnumerator LosGolpes_NoLeCortanElAtaque_PeroDestella()
        {
            Suelo(-12f, 12f);
            yield return CrearJugadorEnSuelo(new Vector2(1.6f, 0f));
            CrearGolem(0f);
            Assert.IsTrue(_golem.Datos.hiperarmadura);

            yield return EsperarHasta(() => Fotograma == "Golem_A1", 2f, "que levante los puños (A1)");
            var vida = _golem.Vida.Vida;
            _golem.Vida.RecibirGolpe(new Golpe(1, new Vector2(1.6f, 1f)));
            yield return null;

            Assert.AreEqual(vida - 1, _golem.Vida.Vida, "El golpe le quita vida.");
            Assert.IsTrue(_animacion.DestelloActivo, "Y destella: la silueta en su color.");
            Assert.AreEqual(ESTADOS_ENEMIGO.ATTACK, _golem.EstadoActual, "Pero no se aturde ni deja el ataque.");
            yield return EsperarHasta(() => Fotograma == "Golem_A3", 0.6f, "que el golpe llegue igual (A3)");
            Assert.IsFalse(_animacion.DestelloActivo, "El destello dura 0,1 s.");
        }

        [UnityTest]
        public IEnumerator Persigue_Andando_YParadoADistanciaDeAtaque_Respira()
        {
            Suelo(-12f, 12f);
            yield return CrearJugadorEnSuelo(new Vector2(4.5f, 0f));
            CrearGolem(0f);

            yield return EsperarHasta(() => Fotograma.StartsWith("Golem_W"), 2f, "que eche a andar hacia Kael");
            yield return EsperarHasta(() => _golem.EstadoActual == ESTADOS_ENEMIGO.ATTACK, 4f, "que llegue a distancia de ataque y golpee");
            yield return EsperarHasta(() => _golem.EstadoActual == ESTADOS_ENEMIGO.CHASE, 1.5f, "que acabe el golpe");

            // Tras el golpe se acerca un paso, hasta el 80 % de su distancia de ataque, y ahí espera
            // a poder volver a golpear sin moverse: respira, no anda en el sitio.
            var cuerpo = _golem.GetComponent<Rigidbody2D>();
            var espera = _golem.Datos.attackRadius * 0.8f + 0.05f;
            var quieto = 0;
            yield return EsperarHasta(() =>
            {
                var cerca = Mathf.Abs(Jugador.transform.position.x - _golem.transform.position.x) <= espera;
                quieto = cerca && Mathf.Abs(cuerpo.linearVelocity.x) < 0.01f ? quieto + 1 : 0;
                return quieto >= 3;
            }, 1.5f, "que se pare a distancia de ataque");
            Assert.AreEqual(ESTADOS_ENEMIGO.CHASE, _golem.EstadoActual, "Espera el tiempo entre ataques.");
            yield return EsperarHasta(() => Fotograma.StartsWith("Golem_I"), 0.3f, "que respire mientras espera");
            yield return ComprobarQueNunca(() => Fotograma.StartsWith("Golem_W"), 0.6f, "Parado no anda en el sitio.");
        }

        [UnityTest]
        public IEnumerator MiraHaciaKael_YGolpeaPorEseLado()
        {
            Suelo(-12f, 12f);
            yield return CrearJugadorEnSuelo(new Vector2(-1.6f, 0f));
            CrearGolem(0f);

            yield return EsperarHasta(() => _arma.activeSelf, 2f, "el golpe hacia la izquierda");
            Assert.Less(_arma.transform.position.x, _golem.transform.position.x, "La onda sale por el lado de Kael.");
            Assert.Less(_sprite.transform.lossyScale.x, 0f, "Dibujado mirando a la derecha: a la izquierda se voltea.");
        }

        [UnityTest]
        public IEnumerator ElContacto_HaceDanioSiempre_YLaOndaEstaApagadaEnReposo()
        {
            Suelo(-12f, 12f);
            CrearGolem(0f);
            yield return null;

            Assert.IsTrue(_contacto.activeSelf, "Tocar su cuerpo hace daño siempre.");
            Assert.IsFalse(_arma.activeSelf, "La onda, solo en A3.");
        }

        [UnityTest]
        public IEnumerator AlMorir_SeQuedaEnD4_YDespuesSeDesvanece()
        {
            Suelo(-12f, 12f);
            CrearGolem(0f);
            yield return null;

            _golem.Vida.Matar();
            yield return EsperarHasta(() => Fotograma == "Golem_D4", 1.6f, "que caiga hasta D4");
            var enD4 = Time.time;
            Assert.IsFalse(_contacto.activeSelf, "Muerto, ya no hace daño.");

            yield return EsperarHasta(() => _sprite == null || _sprite.color.a < 0.99f, 2f, "que empiece a desvanecerse");
            Assert.AreEqual(_animacion.RestosEnPantalla, Time.time - enD4, 0.1f, "Los restos se quedan un momento.");
            yield return EsperarHasta(() => _golem == null, 1f, "que desaparezca");
        }
    }
}
