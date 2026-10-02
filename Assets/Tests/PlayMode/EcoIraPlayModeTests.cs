using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// El Eco de la Ira jugando, con su prefab, el jugador real y física real: el arma solo hace
    /// daño en a2 y a3, la anticipación dura más que el golpe, no salta al cambiar de estado, sus
    /// piernas siguen al cuerpo y la muerte espera al clip antes de soltar las partículas.
    /// </summary>
    public class EcoIraPlayModeTests : PruebaPlayModeBase
    {
        private const string RutaPrefab = "Assets/Prefabs/Enemigos/EcoIra.prefab";
        private static readonly int VelocidadCorrer = Animator.StringToHash("VelocidadCorrer");

        private EnemyController _eco;
        private EnemigoAnimacion _animacion;
        private Animator _animator;
        private SpriteRenderer _sprite;
        private GameObject _arma;

        private void CrearEco(float x, params Transform[] ruta)
        {
            var objeto = Object.Instantiate(CargarPrefab(RutaPrefab), new Vector3(x, 0.05f, 0f), Quaternion.identity);
            Creados.Add(objeto);
            _eco = objeto.GetComponent<EnemyController>();
            _eco.FijarWaypoints(ruta);
            _animacion = objeto.GetComponentInChildren<EnemigoAnimacion>();
            _animator = _animacion.GetComponent<Animator>();
            _sprite = _animacion.GetComponent<SpriteRenderer>();
            _arma = objeto.transform.Find("Pivote/Arma").gameObject;
        }

        /// <summary>Una copia de los datos del Eco con algún ajuste (antes de su Start).</summary>
        private void AjustarDatos(Action<EnemyData> ajustar)
        {
            var datos = Object.Instantiate(_eco.Datos);
            ajustar(datos);
            Creados.Add(datos);
            _eco.FijarDatos(datos);
        }

        private string Fotograma => _sprite.sprite.name;

        private bool EnClip(string estado) => _animator.GetCurrentAnimatorStateInfo(0).IsName(estado);

        /// <summary>Instantes del primer ataque: cuándo empieza, cuándo se abre y se cierra el arma.</summary>
        private sealed class Ataque
        {
            public float Inicio = -1f, Apertura = -1f, Cierre = -1f;
            public string FotogramaDelDanio;
            public readonly List<string> ArmaFueraDeA2A3 = new List<string>();
        }

        private IEnumerator ObservarAtaque(Ataque ataque, float segundos)
        {
            var vida = Jugador.Vida.Vida;
            yield return Durante(segundos, () =>
            {
                if (ataque.Inicio < 0f && _eco.EstadoActual == ESTADOS_ENEMIGO.ATTACK) ataque.Inicio = Time.time;
                if (_arma.activeSelf)
                {
                    if (ataque.Apertura < 0f) ataque.Apertura = Time.time;
                    if (Fotograma != "a2" && Fotograma != "a3") ataque.ArmaFueraDeA2A3.Add($"{Fotograma} (t = {Time.time:0.000})");
                }
                else if (ataque.Apertura >= 0f && ataque.Cierre < 0f)
                {
                    ataque.Cierre = Time.time;
                }

                if (ataque.FotogramaDelDanio == null && Jugador.Vida.Vida < vida) ataque.FotogramaDelDanio = Fotograma;
            });
        }

        // --- Criterio: el collider de ataque solo hace daño durante a2 y a3 ---

        [UnityTest]
        public IEnumerator ElArma_SoloSeAbreEnA2YA3_YLaAnticipacionDuraMasQueElGolpe()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(new Vector2(1f, 0f));
            CrearEco(0f);

            var ataque = new Ataque();
            yield return ObservarAtaque(ataque, 2f);

            Assert.Greater(ataque.Inicio, 0f, "Debe atacar al jugador que tiene a tiro.");
            Assert.IsEmpty(ataque.ArmaFueraDeA2A3, "El arma solo puede estar abierta mientras se ven a2 y a3.");
            Assert.AreEqual(0.35f, ataque.Apertura - ataque.Inicio, 0.05f, "La anticipación (a1) dura 0,35 s.");
            Assert.AreEqual(0.13f, ataque.Cierre - ataque.Apertura, 0.05f, "El golpe (a2 + a3) dura 0,13 s.");
            Assert.That(ataque.FotogramaDelDanio, Is.EqualTo("a2").Or.EqualTo("a3"), "El daño llega durante el golpe.");
        }

        [UnityTest]
        public IEnumerator VelocidadAnimacion_EstiraTodoElClip_YLaVentanaSigueEnA2YA3()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(new Vector2(1f, 0f));
            CrearEco(0f);
            _animacion.velocidadAnimacion = 0.5f;

            var ataque = new Ataque();
            yield return ObservarAtaque(ataque, 2.5f);

            Assert.AreEqual(0.5f, _animator.speed, 1e-4f);
            Assert.IsEmpty(ataque.ArmaFueraDeA2A3);
            Assert.AreEqual(0.70f, ataque.Apertura - ataque.Inicio, 0.06f, "A ritmo 0,5 la anticipación dura el doble.");
            Assert.AreEqual(0.26f, ataque.Cierre - ataque.Apertura, 0.06f, "Y el golpe también.");
        }

        [UnityTest]
        public IEnumerator UnGolpeDuranteLaAnticipacion_CancelaElAtaqueSinAbrirElArma()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(new Vector2(1f, 0f));
            CrearEco(0f);

            yield return EsperarHasta(() => _eco.EstadoActual == ESTADOS_ENEMIGO.ATTACK, 2f, "que el Eco ataque");
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual("a1", Fotograma, "A los 0,1 s sigue en la anticipación.");

            _eco.Vida.RecibirGolpe(new Golpe(1, new Vector2(1f, 0.5f)));
            Assert.AreEqual(ESTADOS_ENEMIGO.DAMAGED, _eco.EstadoActual);
            yield return null;
            yield return null;
            Assert.IsFalse(EnClip("Attack"), "El Animator deja el ataque en cuanto se interrumpe: no hay golpe fantasma.");
            yield return ComprobarQueNunca(() => _arma.activeSelf, 0.5f, "Interrumpido en a1, el golpe no llega a salir.");
        }

        // --- Criterio: no salta verticalmente al cambiar de estado ---

        [UnityTest]
        public IEnumerator AlCambiarEntreIdleRunAttackYDeath_NoSaltaEnVertical()
        {
            Suelo(-10f, 10f);
            yield return CrearJugadorEnSuelo(new Vector2(9f, 0f)); // fuera del radio de detección
            CrearEco(0f);
            yield return new WaitForSeconds(0.3f);

            var clips = new HashSet<string>();
            float minimo = float.PositiveInfinity, maximo = float.NegativeInfinity;
            void Muestra()
            {
                foreach (var clip in new[] { "Idle", "Run", "Attack", "Death" })
                {
                    if (EnClip(clip)) clips.Add(clip);
                }

                minimo = Mathf.Min(minimo, _sprite.bounds.min.y);
                maximo = Mathf.Max(maximo, _sprite.bounds.min.y);
            }

            yield return Durante(0.3f, Muestra);
            Jugador.Motor.Teletransportar(new Vector2(3f, 0.05f));
            yield return Durante(2f, Muestra);
            _eco.Vida.RecibirGolpe(new Golpe(100, new Vector2(-1f, 0.5f)));
            yield return Durante(0.3f, Muestra);

            CollectionAssert.IsSupersetOf(clips, new[] { "Idle", "Run", "Attack", "Death" });
            Assert.Less(maximo - minimo, 0.005f, "Los doce fotogramas comparten el pivote de los pies.");
        }

        // --- Ritmo de las piernas ---

        [UnityTest]
        public IEnumerator Corriendo_LasPiernasSiguenAlCuerpo_YParadoNoCorreEnElSitio()
        {
            Bloque("Plataforma", -6f, 2f, -1f, 0f, Capas.Suelo);
            Suelo(-10f, 20f, yArriba: -4f);
            yield return CrearJugadorEnSuelo(new Vector2(5f, -4f));
            CrearEco(-2f);
            AjustarDatos(d =>
            {
                d.deteccion = null; // por distancia: desde arriba, el borde le taparía la vista
                d.detectionRadius = 12f;
            });

            yield return EsperarHasta(() => _eco.EstadoActual == ESTADOS_ENEMIGO.CHASE, 1f, "que detecte al jugador de abajo");
            yield return new WaitForSeconds(0.2f);

            // La velocidad real (la que deja el roce con el suelo), medida por lo que avanza.
            var desde = _eco.transform.position.x;
            var ritmos = new List<float>();
            yield return Durante(0.3f, () => ritmos.Add(_animator.GetFloat(VelocidadCorrer)));
            var real = (_eco.transform.position.x - desde) / 0.3f;
            Assert.IsTrue(EnClip("Run"));
            Assert.Greater(real, 1.5f, "Persigue.");
            Assert.AreEqual(real / _animacion.VelocidadNaturalCarrera, ritmos.Average(), 0.15f,
                "El ciclo de correr va al ritmo de la velocidad real del cuerpo.");

            var cuerpo = _eco.GetComponent<Rigidbody2D>();
            yield return EsperarHasta(() => Mathf.Abs(cuerpo.linearVelocity.x) < 0.01f, 4f, "que se pare en el borde");
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(0f, _animator.GetFloat(VelocidadCorrer), 1e-3f);
            var tiempo = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(ESTADOS_ENEMIGO.CHASE, _eco.EstadoActual);
            Assert.IsTrue(EnClip("Run"), "Sigue persiguiendo, con los ojos encendidos...");
            Assert.AreEqual(tiempo, _animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1e-4f, "...pero no corre en el sitio.");
        }

        [UnityTest]
        public IEnumerator Patrullando_CorreEntreSusPuntos_YSeDetieneEnIdle()
        {
            Suelo(-10f, 10f);
            CrearEco(0f, Punto("A", -2f), Punto("B", 2f));

            var corrio = false;
            var paroTrasCorrer = false;
            var ritmo = 0f;
            yield return Durante(5f, () =>
            {
                if (EnClip("Run") && _eco.EstadoActual == ESTADOS_ENEMIGO.PATROL)
                {
                    corrio = true;
                    ritmo = Mathf.Max(ritmo, _animator.GetFloat(VelocidadCorrer));
                }

                paroTrasCorrer |= corrio && EnClip("Idle") && _eco.EstadoActual == ESTADOS_ENEMIGO.IDLE;
            });

            Assert.IsTrue(corrio, "Patrulla con el clip de correr.");
            Assert.IsTrue(paroTrasCorrer, "En cada punto hace una pausa en Idle.");
            Assert.AreEqual(1f, ritmo, 0.1f, "A la velocidad de patrulla, el ciclo va a su ritmo dibujado (0,12 s).");
        }

        // --- Daño y muerte ---

        [UnityTest]
        public IEnumerator AlRecibirUnGolpe_SeVuelveUnaSiluetaBlanca_0_1s()
        {
            Suelo(-10f, 10f);
            CrearEco(0f);
            yield return new WaitForSeconds(0.2f);
            var normal = _sprite.sharedMaterial;

            _eco.Vida.RecibirGolpe(new Golpe(1, new Vector2(-2f, 0.5f)));
            Assert.IsTrue(_animacion.DestelloActivo, "El golpe se ve al instante.");
            yield return new WaitForSeconds(0.05f);
            Assert.IsTrue(_animacion.DestelloActivo, "Dura 0,1 s.");
            yield return new WaitForSeconds(0.1f);
            Assert.IsFalse(_animacion.DestelloActivo, "Y vuelve a su color.");
            Assert.AreSame(normal, _sprite.sharedMaterial);
        }

        [UnityTest]
        public IEnumerator AlMorir_EsperaAlClip_SueltaLasParticulasDesdeElPecho_SeFundeYDesaparece()
        {
            Suelo(-10f, 10f);
            CrearEco(0f);
            yield return new WaitForSeconds(0.3f);

            var raiz = _eco.gameObject;
            var cuerpo = raiz.GetComponent<CapsuleCollider2D>();
            var estallido = raiz.transform.Find("Pivote/Pecho").GetComponent<ParticleSystem>();
            var pecho = (Vector2)raiz.transform.position + new Vector2(0.044f, 0.276f); // mira a la derecha
            var muerte = Time.time;

            _eco.Vida.RecibirGolpe(new Golpe(100, new Vector2(-2f, 0.5f)));
            Assert.AreEqual(ESTADOS_ENEMIGO.DEAD, _eco.EstadoActual);
            Assert.IsFalse(_arma.activeSelf, "Un enemigo moribundo no hace daño.");

            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(EnClip("Death"));
            Assert.IsTrue(cuerpo.enabled, "El collider del cuerpo sigue hasta que acaba el clip.");
            Assert.IsFalse(estallido.isPlaying);

            yield return EsperarHasta(() => estallido.transform.parent == null, 1f, "que se suelten las partículas");
            Assert.AreEqual(0.30f, Time.time - muerte, 0.06f, "Al acabar el clip de muerte (d2 + d3).");
            Assert.IsFalse(cuerpo.enabled);
            Assert.IsTrue(estallido.isPlaying);
            Assert.AreEqual(pecho.x, estallido.transform.position.x, 0.02f, "Desde la cavidad del pecho...");
            Assert.AreEqual(pecho.y, estallido.transform.position.y, 0.02f, "...no desde el centro del sprite.");
            yield return null;
            Assert.Greater(estallido.particleCount, 0);

            var alfa = _sprite.color.a;
            yield return new WaitForSeconds(0.2f);
            Assert.Less(_sprite.color.a, alfa, "Se funde.");

            yield return EsperarHasta(() => raiz == null, 1f, "que el Eco desaparezca");
            Assert.AreEqual(0.7f, Time.time - muerte, 0.12f, "Clip (0,3 s) y fundido (0,4 s).");
            yield return EsperarHasta(() => estallido == null, 2f, "que las partículas acaben y se borren solas");
        }
    }
}
