using System;
using UnityEngine;

namespace Fragmentum.Core.Combate
{
    /// <summary>
    /// Vida de una entidad, con invulnerabilidad temporal tras recibir un golpe.
    ///
    /// Es la base del jugador (<c>PlayerHealth</c>), los enemigos y los jefes. La barra de
    /// vida, el audio, los VFX y la FSM se enteran por eventos, sin preguntar cada frame ni
    /// buscar objetos por nombre.
    /// </summary>
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IGolpeable
    {
        [Header("Vida")]
        [Tooltip("Vida máxima en puntos enteros.")]
        [SerializeField] private int vidaMaxima = 3;

        [Tooltip("Segundos de invulnerabilidad tras recibir un golpe (i-frames).")]
        [SerializeField] private float invulnerabilidadTrasGolpe = 0.5f;

        private float _finInvulnerabilidad = float.NegativeInfinity;
        private Func<float> _reloj;

        /// <summary>Vida actual (actual, máxima).</summary>
        public event Action<int, int> OnVidaCambiada;

        /// <summary>Golpe aceptado, con quien lo lanzó y desde dónde.</summary>
        public event Action<Golpe> OnGolpeRecibido;

        /// <summary>Puntos curados.</summary>
        public event Action<int> OnCurado;

        /// <summary>La vida llegó a cero.</summary>
        public event Action OnMuerte;

        /// <summary>Volvió a la vida (respawn en checkpoint).</summary>
        public event Action OnRevivir;

        public int Vida { get; private set; }

        public int VidaMaxima => vidaMaxima;

        public bool EstaVivo => Vida > 0;

        public bool EsInvulnerable => Reloj() < _finInvulnerabilidad;

        /// <summary>
        /// Fuente de tiempo. Por defecto <see cref="Time.time"/>; las pruebas la sustituyen
        /// para controlar los i-frames sin esperar en tiempo real.
        /// </summary>
        public Func<float> Reloj
        {
            get => _reloj ?? (_reloj = () => Time.time);
            set => _reloj = value ?? (() => Time.time);
        }

        protected virtual void Awake()
        {
            Inicializar();
        }

        /// <summary>
        /// Deja la vida al máximo y quita la invulnerabilidad. Se llama sola en Awake, y a mano
        /// cuando la vida viene de un dato (por ejemplo, <c>EnemyData.vidaMaxima</c>). Con
        /// <paramref name="invulnerabilidad"/> ≥ 0 también fija los i-frames tras cada golpe.
        /// </summary>
        public void Inicializar(int vidaMaximaNueva = -1, float invulnerabilidad = -1f)
        {
            if (vidaMaximaNueva > 0)
            {
                vidaMaxima = vidaMaximaNueva;
            }

            if (invulnerabilidad >= 0f)
            {
                invulnerabilidadTrasGolpe = invulnerabilidad;
            }

            vidaMaxima = Mathf.Max(1, vidaMaxima);
            Vida = vidaMaxima;
            _finInvulnerabilidad = float.NegativeInfinity;
        }

        /// <summary>
        /// Reemite la vida actual. Los componentes de UI la llaman en su Start, después de
        /// suscribirse, para pintar el estado inicial sin depender del orden de ejecución.
        /// </summary>
        public void NotificarVida() => OnVidaCambiada?.Invoke(Vida, vidaMaxima);

        public virtual bool RecibirGolpe(in Golpe golpe)
        {
            if (!EstaVivo || golpe.Cantidad <= 0)
            {
                return false;
            }

            if (!golpe.IgnoraInvulnerabilidad && (EsInvulnerable || !PuedeRecibir(golpe)))
            {
                return false;
            }

            Vida = Mathf.Max(0, Vida - golpe.Cantidad);
            _finInvulnerabilidad = Reloj() + invulnerabilidadTrasGolpe;

            OnGolpeRecibido?.Invoke(golpe);
            OnVidaCambiada?.Invoke(Vida, vidaMaxima);

            if (Vida == 0)
            {
                OnMuerte?.Invoke();
            }

            return true;
        }

        /// <summary>Cura sin pasar de la vida máxima. Devuelve los puntos realmente curados.</summary>
        public int Curar(int cantidad)
        {
            if (!EstaVivo || cantidad <= 0)
            {
                return 0;
            }

            var antes = Vida;
            Vida = Mathf.Min(vidaMaxima, Vida + cantidad);
            var curado = Vida - antes;

            if (curado > 0)
            {
                OnCurado?.Invoke(curado);
                OnVidaCambiada?.Invoke(Vida, vidaMaxima);
            }

            return curado;
        }

        /// <summary>
        /// Quita vida sin que sea un golpe: no hay retroceso (nadie golpea), ni i-frames, ni escudo
        /// que lo pare. Es el ahogo del mundo de la Tristeza: Kael se va apagando sin que nada lo
        /// empuje. Devuelve los puntos realmente perdidos.
        /// </summary>
        public int PerderVida(int cantidad)
        {
            if (!EstaVivo || cantidad <= 0)
            {
                return 0;
            }

            var antes = Vida;
            Vida = Mathf.Max(0, Vida - cantidad);
            OnVidaCambiada?.Invoke(Vida, vidaMaxima);
            if (Vida == 0)
            {
                OnMuerte?.Invoke();
            }

            return antes - Vida;
        }

        /// <summary>Muerte instantánea: ignora i-frames y escudos (lava, vacío, ahogamiento).</summary>
        public void Matar(GameObject causante = null)
        {
            RecibirGolpe(Golpe.Letal(transform.position, causante));
        }

        /// <summary>Devuelve a la vida, por ejemplo al reaparecer en un checkpoint.</summary>
        public void Revivir(int vida = -1)
        {
            Vida = vida > 0 ? Mathf.Min(vida, vidaMaxima) : vidaMaxima;
            _finInvulnerabilidad = float.NegativeInfinity;
            OnRevivir?.Invoke();
            OnVidaCambiada?.Invoke(Vida, vidaMaxima);
        }

        /// <summary>
        /// Gancho para las subclases: el jugador lo redefine para la Defensa Reforzada, que
        /// absorbe un golpe cada 15 s.
        /// </summary>
        protected virtual bool PuedeRecibir(in Golpe golpe) => true;

        /// <summary>
        /// Abre una ventana de invulnerabilidad sin quitar vida. La usa el escudo del jugador
        /// para que un golpe absorbido no se repita en el frame siguiente. Nunca acorta una
        /// ventana que ya estuviera abierta.
        /// </summary>
        protected void ConcederInvulnerabilidad(float segundos)
        {
            _finInvulnerabilidad = Mathf.Max(_finInvulnerabilidad, Reloj() + Mathf.Max(0f, segundos));
        }
    }
}
