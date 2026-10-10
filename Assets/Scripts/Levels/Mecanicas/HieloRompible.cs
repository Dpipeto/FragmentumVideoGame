using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Un tapón de hielo que solo se rompe con el <b>Ataque Cargado</b> (mundo de la Tristeza). Tapa
    /// el agujero de la plaza helada: la superficie no lleva a ninguna parte y la única salida es
    /// hundirse. La grieta brilla en el naranja de la Ira: el golpe contenido del mundo anterior
    /// es lo que abre el camino. No está en el Anexo A; lo aprobó el autor el 2026-10-08.
    ///
    /// - Un golpe normal lo <b>agrieta</b> (el aviso: sirve, pero no basta) y sigue tapando.
    /// - Un golpe cargado lo <b>rompe</b>: el collider se apaga y el hueco queda abierto. No vuelve.
    ///
    /// La espada del jugador solo golpea la capa Enemy, así que el tapón lleva una zona de golpe
    /// en esa capa (un collider hijo). Con la matriz de colisión del proyecto, esa capa no choca
    /// con Kael ni con los enemigos: solo la nota la espada. El cuerpo que se pisa sigue en la capa
    /// del suelo.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class HieloRompible : MonoBehaviour, IGolpeable
    {
        [Tooltip("El dibujo: se apaga al romperse.")]
        [SerializeField] private SpriteRenderer visual;

        [Tooltip("La zona que golpea la espada (collider en la capa Enemy). Se apaga al romperse.")]
        [SerializeField] private Collider2D zonaDeGolpe;

        [SerializeField] private Animator animator;

        [Tooltip("Sin arte: colores del rectángulo provisional según el estado.")]
        [SerializeField] private Color colorIntacto = new Color(0.75f, 0.85f, 0.95f, 1f);

        [SerializeField] private Color colorAgrietado = new Color(1f, 0.55f, 0.3f, 1f);

        private BoxCollider2D _cuerpo;

        /// <summary>Cambió de estado.</summary>
        public event Action<ESTADOS_HIELO> OnCambioEstado;

        public ESTADOS_HIELO Estado { get; private set; }

        public bool Roto => Estado == ESTADOS_HIELO.BROKEN;

        /// <summary>¿Tapa el paso? (Su collider está activo.)</summary>
        public bool Tapa
        {
            get
            {
                var cuerpo = _cuerpo != null ? _cuerpo : GetComponent<BoxCollider2D>();
                return cuerpo != null && cuerpo.enabled;
            }
        }

        public bool EstaVivo => !Roto;

        private void Awake()
        {
            _cuerpo = GetComponent<BoxCollider2D>();
            if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            Pintar();
        }

        private void Update()
        {
            if (animator != null && animator.isInitialized)
            {
                AnimatorParams.FijarEntero(animator, AnimatorParams.EstadoMecanica, (int)Estado);
            }
        }

        public bool RecibirGolpe(in Golpe golpe)
        {
            if (Roto)
            {
                return false;
            }

            if (golpe.Cargado)
            {
                Romper();
                return true;
            }

            if (Estado == ESTADOS_HIELO.INTACT)
            {
                Cambiar(ESTADOS_HIELO.CRACKED);
            }

            return true;
        }

        /// <summary>Lo parte y deja el hueco abierto (también para las pruebas y las herramientas).</summary>
        public void Romper()
        {
            if (Roto)
            {
                return;
            }

            if (_cuerpo == null)
            {
                _cuerpo = GetComponent<BoxCollider2D>();
            }

            _cuerpo.enabled = false;
            if (zonaDeGolpe != null)
            {
                zonaDeGolpe.enabled = false;
            }

            Cambiar(ESTADOS_HIELO.BROKEN);
        }

        private void Cambiar(ESTADOS_HIELO estado)
        {
            Estado = estado;
            Pintar();
            OnCambioEstado?.Invoke(estado);
        }

        // Sin Animator (sin arte), el rectángulo provisional cambia de color y se apaga al romperse.
        private void Pintar()
        {
            if (visual == null || animator != null)
            {
                return;
            }

            visual.enabled = !Roto;
            visual.color = Estado == ESTADOS_HIELO.CRACKED ? colorAgrietado : colorIntacto;
        }
    }
}
