using Fragmentum.Levels.Mecanicas;
using Fragmentum.Narrative;
using Fragmentum.Player;
using UnityEngine;

namespace Fragmentum.Levels
{
    /// <summary>
    /// Un sitio del nivel en el que Kael piensa en voz alta: cuando entra su cuerpo, dice las
    /// líneas de un momento de los diálogos del nivel (<see cref="DialogueData"/>). Es un
    /// pensamiento, como los demás: no detiene el juego.
    ///
    /// La usa la pista del tapón de hielo (mundo de la Tristeza): quien llega ante él oye a Kael
    /// decir que hace falta el Ataque Cargado. Con un <see cref="hielo"/> asignado, la dice también
    /// cuando un golpe normal lo agrieta sin romperlo (que es cuando el jugador acaba de ver que
    /// no basta), y calla si el hielo ya está roto.
    ///
    /// Cada motivo vale una vez, y la línea nunca suena dos veces seguidas: si acaba de sonar por
    /// uno, el otro se da por dicho.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ZonaDeDialogo : MonoBehaviour
    {
        [SerializeField] private MOMENTOS_DIALOGO momento = MOMENTOS_DIALOGO.PISTA;

        [Tooltip("Opcional. La línea suena también cuando este hielo se agrieta sin romperse, y calla si ya está roto.")]
        [SerializeField] private HieloRompible hielo;

        [Tooltip("Segundos que tienen que pasar para que vuelva a sonar.")]
        [Min(0f)] [SerializeField] private float esperaEntreVeces = 8f;

        private LevelManager _nivel;
        private bool _dichaAlEntrar;
        private bool _dichaAlAgrietar;
        private float _ultimaVez = float.NegativeInfinity;

        public MOMENTOS_DIALOGO Momento => momento;

        public HieloRompible Hielo => hielo;

        /// <summary>Cuántas veces ha sonado.</summary>
        public int Veces { get; private set; }

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnEnable()
        {
            if (hielo != null)
            {
                hielo.OnCambioEstado += AlCambiarElHielo;
            }
        }

        private void OnDisable()
        {
            if (hielo != null)
            {
                hielo.OnCambioEstado -= AlCambiarElHielo;
            }
        }

        private void OnTriggerEnter2D(Collider2D otro)
        {
            // Solo el cuerpo del jugador (no su espada, que también es un trigger) y vivo.
            if (_dichaAlEntrar || otro.isTrigger || otro.attachedRigidbody == null)
            {
                return;
            }

            var jugador = otro.attachedRigidbody.GetComponent<PlayerController>();
            if (jugador == null || !jugador.EstaVivo)
            {
                return;
            }

            _dichaAlEntrar = true;
            Decir();
        }

        private void AlCambiarElHielo(ESTADOS_HIELO estado)
        {
            if (estado != ESTADOS_HIELO.CRACKED || _dichaAlAgrietar)
            {
                return;
            }

            _dichaAlAgrietar = true;
            Decir();
        }

        /// <summary>
        /// Pone la línea en la cola de los diálogos. No lo hace si el hielo ya está roto, si acaba
        /// de sonar o si la escena no tiene diálogos. Devuelve si la dijo.
        /// </summary>
        public bool Decir()
        {
            if ((hielo != null && hielo.Roto) || Time.time - _ultimaVez < esperaEntreVeces)
            {
                return false;
            }

            if (_nivel == null)
            {
                _nivel = FindAnyObjectByType<LevelManager>();
            }

            var dialogos = _nivel != null && _nivel.Nivel != null ? _nivel.Nivel.dialogos : null;
            if (dialogos == null || DialogueManager.Instancia == null || dialogos.Del(momento).Count == 0)
            {
                return false;
            }

            DialogueManager.Instancia.Reproducir(dialogos, momento);
            _ultimaVez = Time.time;
            Veces++;
            return true;
        }
    }
}
