using System.Linq;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Player
{
    /// <summary>
    /// El dibujo de la <b>Defensa Reforzada</b> (<c>Prompts_Mundo4.md</c>, apartado J): un arco de placas de hierro
    /// con el borde rojo del fragmento de la Culpa, delante de Kael. «Un escudo que no niega el golpe: lo resiste.»
    ///
    /// Solo se ve cuando hace algo, porque el escudo de <see cref="PlayerHealth"/> no se activa: está o no está.
    /// - <b>Al recargarse</b> (<see cref="PlayerHealth.OnEscudoRecargado"/>) aparece un momento delante de Kael
    ///   (el primer dibujo) y se desvanece: ya puede parar el siguiente golpe.
    /// - <b>Al parar un golpe</b> (<see cref="PlayerHealth.OnEscudoAbsorbio"/>), del lado de donde viene: lo para
    ///   (el segundo, con las chispas) y se rompe (el tercero), deshaciéndose.
    ///
    /// Está dibujado mirando a la derecha, con el lado cóncavo hacia Kael: del otro lado se voltea. Sin sus tres
    /// dibujos no hace nada.
    /// </summary>
    [DisallowMultipleComponent]
    public class EscudoDibujado : MonoBehaviour
    {
        private enum FASES_ESCUDO
        {
            OCULTO,
            APARECE,
            PARA,
        }

        [SerializeField] private PlayerHealth vida;

        [Tooltip("Para saber hacia dónde mira Kael cuando el escudo se recarga.")]
        [SerializeField] private PlayerController jugador;

        [SerializeField] private SpriteRenderer dibujo;

        [Tooltip("Aparece, para el golpe y se rompe.")]
        [SerializeField] private Sprite[] dibujos = new Sprite[0];

        [Tooltip("Su centro, desde los pies de Kael y mirando a la derecha.")]
        public Vector2 delante = new Vector2(0.6f, 0.95f);

        [Tooltip("Lo que se ve al recargarse: aparece y se desvanece.")]
        [Min(0.05f)] public float duracionAparece = 0.6f;

        [Tooltip("Lo que se ve parando el golpe, antes de romperse.")]
        [Min(0.02f)] public float duracionGolpe = 0.12f;

        [Tooltip("Lo que tarda en deshacerse al romperse.")]
        [Min(0.05f)] public float duracionRotura = 0.35f;

        private FASES_ESCUDO _fase = FASES_ESCUDO.OCULTO;
        private float _inicio;

        /// <summary>¿Tiene sus tres dibujos?</summary>
        public bool Completo => dibujos != null && dibujos.Length >= 3 && dibujos.Take(3).All(d => d != null);

        /// <summary>¿Se ve ahora?</summary>
        public bool Visible => dibujo != null && dibujo.enabled;

        /// <summary>El dibujo que enseña ahora (null si no se ve).</summary>
        public Sprite DibujoActual => Visible ? dibujo.sprite : null;

        /// <summary>El lado en el que está: 1 delante a la derecha, -1 a la izquierda.</summary>
        public int Lado { get; private set; } = 1;

        public SpriteRenderer Dibujo => dibujo;

        private void Awake()
        {
            if (vida == null) vida = GetComponentInParent<PlayerHealth>();
            if (jugador == null) jugador = GetComponentInParent<PlayerController>();
            Ocultar();
        }

        private void OnEnable()
        {
            if (vida != null)
            {
                vida.OnEscudoAbsorbio += AlParar;
                vida.OnEscudoRecargado += AlRecargarse;
            }
        }

        private void OnDisable()
        {
            if (vida != null)
            {
                vida.OnEscudoAbsorbio -= AlParar;
                vida.OnEscudoRecargado -= AlRecargarse;
            }

            Ocultar();
        }

        private void AlRecargarse()
        {
            Empezar(FASES_ESCUDO.APARECE, jugador != null ? jugador.Mirando : Lado);
        }

        private void AlParar(Golpe golpe)
        {
            // Del lado de donde viene el golpe.
            Empezar(FASES_ESCUDO.PARA, golpe.Origen.x < transform.position.x ? -1 : 1);
        }

        private void Empezar(FASES_ESCUDO fase, int lado)
        {
            if (!Completo || dibujo == null)
            {
                return;
            }

            _fase = fase;
            _inicio = Time.time;
            Lado = lado < 0 ? -1 : 1;
            dibujo.transform.localPosition = new Vector3(Lado * delante.x, delante.y, 0f);
            dibujo.flipX = Lado < 0;
            dibujo.enabled = true;
            Pintar();
        }

        private void Update()
        {
            if (_fase != FASES_ESCUDO.OCULTO)
            {
                Pintar();
            }
        }

        private void Pintar()
        {
            var t = Time.time - _inicio;
            switch (_fase)
            {
                case FASES_ESCUDO.APARECE:
                    if (t >= duracionAparece)
                    {
                        Ocultar();
                        return;
                    }

                    // Sube y baja: un instante entero en el medio.
                    Fijar(dibujos[0], Mathf.Sin(Mathf.PI * t / duracionAparece));
                    break;

                case FASES_ESCUDO.PARA:
                    if (t < duracionGolpe)
                    {
                        Fijar(dibujos[1], 1f);
                    }
                    else if (t < duracionGolpe + duracionRotura)
                    {
                        Fijar(dibujos[2], 1f - (t - duracionGolpe) / duracionRotura);
                    }
                    else
                    {
                        Ocultar();
                    }

                    break;
            }
        }

        private void Fijar(Sprite sprite, float opacidad)
        {
            dibujo.sprite = sprite;
            var color = dibujo.color;
            color.a = Mathf.Clamp01(opacidad);
            dibujo.color = color;
        }

        private void Ocultar()
        {
            _fase = FASES_ESCUDO.OCULTO;
            if (dibujo != null)
            {
                dibujo.enabled = false;
            }
        }
    }
}
