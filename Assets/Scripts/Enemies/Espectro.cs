using System;
using System.Collections;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Levels.Mecanicas;
using UnityEngine;

namespace Fragmentum.Enemies
{
    public enum ESTADOS_ESPECTRO
    {
        /// <summary>Quieto, con el controlador apagado: ni ve, ni anda, ni ataca.</summary>
        DORMIDO,

        /// <summary>Manda el <see cref="EnemyController"/>: solo se acerca a Kael si está a oscuras.</summary>
        DESPIERTO,

        /// <summary>Kael está en la luz de una antorcha: sale de ella y espera en su borde.</summary>
        AL_BORDE,

        /// <summary>La luz se apagó: va hacia Kael hasta tenerlo a tiro, y entonces lo caza su controlador.</summary>
        ACUDE
    }

    /// <summary>
    /// El Espectro, el Eco del mundo del Miedo (<c>Nivel3_Diseno.md</c>, sección 5). Su
    /// <see cref="EnemyController"/>, con la detección en oscuridad, ya solo va a por Kael cuando
    /// está a oscuras. Esta pieza le añade lo demás:
    ///
    /// - <b>Duerme</b> hasta que una antorcha se enciende con él dentro de su luz
    ///   (<see cref="Antorcha.AlEncenderse"/>), hasta que Kael pasa a <see cref="radioDespertar"/> o
    ///   hasta que le pegan. Dormido, su controlador está apagado, como el de la Escultura.
    /// - <b>Espera en el borde de la luz.</b> Despierto, si Kael está dentro de la luz de una
    ///   antorcha y a menos de <see cref="alcanceEspera"/>, el Espectro sale de esa luz por el lado
    ///   contrario y espera fuera, de cara a él. Cuando la luz se apaga, <b>acude</b>: va hacia Kael
    ///   hasta tenerlo a tiro (el borde de una antorcha queda más lejos de lo que ve) y su
    ///   controlador lo caza.
    /// - <b>Es casi invisible.</b> En un nivel a oscuras no se ve; dentro de la luz de Kael se adivina
    ///   (<see cref="opacidadEnLaLuzDeKael"/>); bajo una antorcha se ve entero, y también mientras
    ///   ataca, que es cuando se le encienden los ojos. En los niveles con luz se ve siempre.
    ///
    /// No toca <see cref="EnemyController"/>: solo lo apaga y lo enciende.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController), typeof(Health))]
    public class Espectro : MonoBehaviour
    {
        [Header("Sueño")]
        [Tooltip("Empieza dormido, con el controlador apagado.")]
        public bool empiezaDormido = true;

        [Tooltip("Despierta si Kael pasa a esta distancia (de centro a centro).")]
        [Min(0f)] public float radioDespertar = 1.5f;

        [Header("Casi invisible")]
        [Tooltip("Dentro de la luz de Kael se adivina: esta fracción de opaco.")]
        [Range(0f, 1f)] public float opacidadEnLaLuzDeKael = 0.25f;

        [Tooltip("Lo rápido que aparece y se desvanece (opacidad por segundo).")]
        [Min(0.1f)] public float velocidadFundido = 5f;

        [Header("Al borde de la luz")]
        [Tooltip("Espera en el borde si Kael, en la luz de una antorcha, está a menos de esto. Más que el radio de " +
                 "una antorcha (8 u): los que la antorcha despierta esperan todos.")]
        [Min(0f)] public float alcanceEspera = 12f;

        [Tooltip("Sale de la luz de una antorcha hasta esta distancia más allá de su borde.")]
        [Min(0f)] public float margenBorde = 0.5f;

        [Header("Referencias")]
        [Tooltip("Lo que se voltea al girar (el del EnemyController).")]
        [SerializeField] private Transform pivote;

        [SerializeField] private SpriteRenderer sprite;

        [Header("Sin arte")]
        [Tooltip("Dormido, más apagado.")]
        [SerializeField] private Color colorDormido = new Color(0.16f, 0.16f, 0.2f, 1f);

        [Tooltip("Al atacar se le encienden los ojos: el bloque se aclara a este color.")]
        [SerializeField] private Color colorAtaque = new Color(0.62f, 1f, 0.66f, 1f);

        private EnemyController _controlador;
        private Health _vida;
        private RelevoDelControlador _relevo;
        private IFuenteLuz _luzDeLaQueSale;
        private float _opacidad = 1f;

        // El fotograma en que se encendió el controlador: su Start (que deja la vida al máximo) llega
        // en el siguiente, y hasta entonces no se le vuelve a apagar.
        private int _encendidoEn;

        public event Action<ESTADOS_ESPECTRO> OnCambio;

        public ESTADOS_ESPECTRO Estado { get; private set; } = ESTADOS_ESPECTRO.DORMIDO;

        public bool Dormido => Estado == ESTADOS_ESPECTRO.DORMIDO;

        /// <summary>Lo que se le ve ahora, de 0 (nada) a 1 (entero).</summary>
        public float Opacidad => _opacidad;

        /// <summary>
        /// Lo que se le ve: entero si el nivel tiene luz, si ataca o si está bajo una antorcha; a
        /// oscuras, solo se adivina dentro de la luz de Kael, y fuera de ella no se ve.
        /// </summary>
        public static float OpacidadEn(bool hayOscuridad, bool atacando, bool bajoUnaAntorcha, bool enLaLuzDeKael, float opacidadEnLaLuzDeKael)
        {
            if (!hayOscuridad || atacando || bajoUnaAntorcha)
            {
                return 1f;
            }

            return enLaLuzDeKael ? opacidadEnLaLuzDeKael : 0f;
        }

        private EnemyData Datos => _controlador.Datos;

        private Color Tinte => Datos != null ? Datos.tinte : Color.white;

        private bool Atacando => !Dormido && _controlador.enabled && _controlador.EstadoActual == ESTADOS_ENEMIGO.ATTACK;

        private float Velocidad => Datos != null ? Datos.velocidadPersecucion : 3.8f;

        private float DistanciaAKael => Vector2.Distance(_relevo.CentroDeKael, _relevo.Centro);

        private bool KaelEnUnaAntorcha => _relevo.KaelVivo && Iluminacion.EstaIluminado(_relevo.CentroDeKael);

        private void Awake()
        {
            _controlador = GetComponent<EnemyController>();
            _vida = GetComponent<Health>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (pivote == null && sprite != null) pivote = sprite.transform.parent != transform ? sprite.transform.parent : sprite.transform;
            _relevo = new RelevoDelControlador(_controlador, _vida, pivote);

            if (empiezaDormido)
            {
                // Dormido no ve ni ataca. Su vida ya es la de sus datos, por si le pegan dormido.
                _controlador.enabled = false;
                if (Datos != null)
                {
                    _vida.Inicializar(Datos.vidaMaxima, Datos.invulnerabilidad);
                }
            }
            else
            {
                // El prefab lo trae apagado, porque empieza dormido.
                _controlador.enabled = true;
                Estado = ESTADOS_ESPECTRO.DESPIERTO;
                _encendidoEn = Time.frameCount;
            }
        }

        private void OnEnable()
        {
            _vida.OnGolpeRecibido += AlRecibirGolpe;
            Antorcha.AlEncenderse += AlEncenderseUnaAntorcha;
        }

        private void OnDisable()
        {
            _vida.OnGolpeRecibido -= AlRecibirGolpe;
            Antorcha.AlEncenderse -= AlEncenderseUnaAntorcha;
            _relevo.Soltar();
        }

        private void Update()
        {
            if (!_vida.EstaVivo)
            {
                return;
            }

            switch (Estado)
            {
                case ESTADOS_ESPECTRO.DORMIDO:
                    if (_relevo.KaelVivo && DistanciaAKael <= radioDespertar)
                    {
                        Despertar();
                    }

                    break;

                case ESTADOS_ESPECTRO.DESPIERTO:
                    if (Time.frameCount > _encendidoEn + 2 && KaelEnUnaAntorcha && DistanciaAKael <= alcanceEspera)
                    {
                        var estado = _controlador.EstadoActual;
                        if (estado == ESTADOS_ENEMIGO.IDLE || estado == ESTADOS_ENEMIGO.PATROL || estado == ESTADOS_ENEMIGO.CHASE)
                        {
                            _relevo.Tomar();
                            _luzDeLaQueSale = null;
                            Cambiar(ESTADOS_ESPECTRO.AL_BORDE);
                        }
                    }

                    break;

                case ESTADOS_ESPECTRO.AL_BORDE:
                    if (!_relevo.KaelVivo || DistanciaAKael > alcanceEspera)
                    {
                        Devolver();
                    }
                    else if (!KaelEnUnaAntorcha)
                    {
                        Cambiar(ESTADOS_ESPECTRO.ACUDE);
                    }

                    break;

                case ESTADOS_ESPECTRO.ACUDE:
                    if (KaelEnUnaAntorcha)
                    {
                        _luzDeLaQueSale = null;
                        Cambiar(ESTADOS_ESPECTRO.AL_BORDE);
                    }
                    else if (!_relevo.KaelVivo || DistanciaAKael > alcanceEspera * 1.5f ||
                             DistanciaAKael <= (Datos != null ? Datos.detectionRadius : 6f))
                    {
                        // Ya lo tiene a tiro (o lo perdió): que lo cace su controlador.
                        Devolver();
                    }

                    break;
            }
        }

        private void FixedUpdate()
        {
            if (!_vida.EstaVivo)
            {
                return;
            }

            if (Estado == ESTADOS_ESPECTRO.AL_BORDE)
            {
                SalirDeLaLuz();
            }
            else if (Estado == ESTADOS_ESPECTRO.ACUDE)
            {
                var haciaKael = _relevo.CentroDeKael.x >= _relevo.Centro.x ? 1 : -1;
                if (!_relevo.Mover(haciaKael, Velocidad))
                {
                    Devolver();
                }
            }
        }

        private void LateUpdate()
        {
            if (sprite == null)
            {
                return;
            }

            var objetivo = OpacidadEn(_relevo.HayOscuridad, Atacando, Iluminacion.EstaIluminado(_relevo.Centro), EnLaLuzDeKael(), opacidadEnLaLuzDeKael);
            _opacidad = Mathf.MoveTowards(_opacidad, objetivo, velocidadFundido * Time.deltaTime);

            var color = Dormido ? colorDormido : Atacando ? colorAtaque : Tinte;
            color.a = Tinte.a * _opacidad;
            sprite.color = color;
        }

        /// <summary>Despierta ya (una antorcha, Kael cerca, un golpe; o para las pruebas).</summary>
        public void Despertar()
        {
            if (!Dormido)
            {
                return;
            }

            Cambiar(ESTADOS_ESPECTRO.DESPIERTO);
            if (!_vida.EstaVivo)
            {
                // Muerto antes de despertar: no se enciende (el controlador le devolvería la vida).
                foreach (var c in GetComponentsInChildren<Collider2D>())
                {
                    c.enabled = false;
                }

                Destroy(gameObject, Datos != null ? Datos.deathDelay : 1f);
                return;
            }

            var perdida = _vida.VidaMaxima - _vida.Vida;
            _controlador.enabled = true;
            _encendidoEn = Time.frameCount;
            if (perdida > 0)
            {
                StartCoroutine(Reaplicar(perdida));
            }
        }

        // Sale de la luz por el lado contrario a ella; fuera (con su margen), espera de cara a Kael.
        private void SalirDeLaLuz()
        {
            var centro = _relevo.Centro;
            var luz = Iluminacion.FuenteEn(centro);
            if (luz != null)
            {
                _luzDeLaQueSale = luz;
            }

            if (_luzDeLaQueSale != null && _luzDeLaQueSale.Encendida &&
                Vector2.Distance(centro, _luzDeLaQueSale.Posicion) < _luzDeLaQueSale.Radio + margenBorde)
            {
                var fuera = centro.x >= _luzDeLaQueSale.Posicion.x ? 1 : -1;
                if (_relevo.Mover(fuera, Velocidad))
                {
                    return;
                }
            }

            _luzDeLaQueSale = null;
            _relevo.Detener();
            _relevo.Mirar(_relevo.CentroDeKael.x >= centro.x ? 1 : -1);
        }

        private void Devolver()
        {
            Cambiar(ESTADOS_ESPECTRO.DESPIERTO);
            _relevo.Devolver();
        }

        // El círculo de la luz de Kael sale de su luz 2D, a la altura del pecho.
        private bool EnLaLuzDeKael()
        {
            var luz = _relevo.LuzDeKael;
            if (luz == null || !luz.Encendida)
            {
                return false;
            }

            var centro = luz.Luz != null ? luz.Luz.transform.position : luz.transform.position;
            return Vector2.Distance(centro, _relevo.Centro) <= luz.Radio;
        }

        private void AlEncenderseUnaAntorcha(Antorcha antorcha)
        {
            if (Dormido && antorcha != null && Vector2.Distance(antorcha.Posicion, _relevo.Centro) <= antorcha.Radio)
            {
                Despertar();
            }
        }

        private void AlRecibirGolpe(Golpe golpe)
        {
            if (Dormido)
            {
                Despertar();
                return;
            }

            _relevo.AntesDeMorir();
        }

        // El Start del controlador (en el fotograma siguiente) deja la vida al máximo.
        private IEnumerator Reaplicar(int perdida)
        {
            yield return null;
            yield return null;
            _vida.PerderVida(perdida);
        }

        private void Cambiar(ESTADOS_ESPECTRO nuevo)
        {
            Estado = nuevo;
            OnCambio?.Invoke(nuevo);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 1f, 0.66f, 0.6f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, radioDespertar);
        }
    }
}
