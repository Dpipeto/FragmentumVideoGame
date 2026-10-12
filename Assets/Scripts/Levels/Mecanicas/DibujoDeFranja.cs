using System.Collections.Generic;
using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// El dibujo de una <see cref="FranjaInundable"/> con su arte (<see cref="ArteDeFranja"/>). La
    /// franja sigue mandando: este componente solo mira en qué estado está y cuánto lleva en él, y
    /// enseña lo que toca:
    /// - <b>aviso:</b> las grietas por las que sale el agua, repartidas por el suelo;
    /// - <b>agua:</b> su superficie, en bucle y repetida a lo ancho, sobre un cuerpo liso y
    ///   translúcido del color con el que acaba; al ponerse blanca, los dos se aclaran;
    /// - <b>púas:</b> la escarcha mientras el agua se pone blanca; después brotan, se quedan
    ///   mientras hacen daño y se rompen;
    /// - <b>capa de hielo:</b> las losas que se pisan: se forman, se quedan y, justo antes de
    ///   deshacerse, se derriten (es el aviso de que hay que bajarse).
    ///
    /// Lo crea la propia franja al construirse, si lleva arte completo.
    /// </summary>
    [DisallowMultipleComponent]
    public class DibujoDeFranja : MonoBehaviour
    {
        public const int OrdenAviso = 8;
        public const int OrdenPuas = 8;
        public const int OrdenCapa = 2;

        private FranjaInundable _franja;
        private ArteDeFranja _arte;
        private SpriteRenderer[] _avisos;
        private SpriteRenderer[] _puas;
        private SpriteRenderer[] _losas;
        private SpriteRenderer _cuerpo;
        private SpriteRenderer _superficie;

        public IReadOnlyList<SpriteRenderer> Avisos => _avisos;

        public IReadOnlyList<SpriteRenderer> Puas => _puas;

        public IReadOnlyList<SpriteRenderer> Losas => _losas;

        public SpriteRenderer Superficie => _superficie;

        public SpriteRenderer Cuerpo => _cuerpo;

        // --- Qué toca en cada momento (sin estado: se puede probar) ---

        /// <summary>
        /// Los centros de <paramref name="cuantos"/> piezas repartidas por igual a lo ancho de la
        /// franja, desde su centro.
        /// </summary>
        public static float[] Repartir(float ancho, int cuantos)
        {
            cuantos = Mathf.Max(1, cuantos);
            var centros = new float[cuantos];
            for (var i = 0; i < cuantos; i++)
            {
                centros[i] = -ancho * 0.5f + (i + 0.5f) * ancho / cuantos;
            }

            return centros;
        }

        /// <summary>
        /// Los centros de las losas que cubren la franja de punta a punta: las de los extremos
        /// acaban justo en sus bordes y, si sobra hielo, las de en medio se montan unas sobre otras.
        /// </summary>
        public static float[] RepartirLosas(float ancho, float anchoLosa)
        {
            var cuantas = Mathf.Max(1, Mathf.CeilToInt(ancho / Mathf.Max(0.1f, anchoLosa) - 0.05f));
            var centros = new float[cuantas];
            if (cuantas == 1)
            {
                return centros;
            }

            var primero = -ancho * 0.5f + anchoLosa * 0.5f;
            var paso = (ancho - anchoLosa) / (cuantas - 1);
            for (var i = 0; i < cuantas; i++)
            {
                centros[i] = primero + i * paso;
            }

            return centros;
        }

        /// <summary>La grieta del aviso según lo avanzado (0 a 1) que vaya.</summary>
        public static Sprite FotogramaDeAviso(ArteDeFranja arte, float avance) =>
            arte.aviso[Mathf.Clamp((int)(Mathf.Clamp01(avance) * arte.aviso.Length), 0, arte.aviso.Length - 1)];

        /// <summary>
        /// Las púas: la escarcha mientras el agua se pone blanca; congelada, brotan, se quedan
        /// mientras hacen daño (<paramref name="duracionPuas"/>) y se rompen. Null: no se ven.
        /// </summary>
        public static Sprite FotogramaDePuas(ArteDeFranja arte, ESTADOS_FRANJA estado, float tiempoEnEstado, float duracionPuas)
        {
            if (estado == ESTADOS_FRANJA.FREEZING)
            {
                return arte.puasAviso;
            }

            if (estado != ESTADOS_FRANJA.FROZEN)
            {
                return null;
            }

            if (tiempoEnEstado < Mathf.Min(arte.segundosBrota, duracionPuas))
            {
                return arte.puasBrota;
            }

            if (tiempoEnEstado < duracionPuas)
            {
                return arte.puasQuietas[BucleDeSprites.Indice(tiempoEnEstado - arte.segundosBrota, arte.puasQuietas.Length, arte.segundosQuieta)];
            }

            return tiempoEnEstado < duracionPuas + arte.segundosRompe ? arte.puasRompe : null;
        }

        /// <summary>
        /// La capa de hielo: se forma, se queda y, cuando le quedan <c>segundosDerretirse</c>, se
        /// derrite. Null: no hay capa.
        /// </summary>
        public static Sprite FotogramaDeCapa(ArteDeFranja arte, ESTADOS_FRANJA estado, float tiempoEnEstado, float tiempoRestante)
        {
            if (estado != ESTADOS_FRANJA.FROZEN)
            {
                return null;
            }

            if (tiempoRestante < arte.segundosDerretirse)
            {
                return arte.capaSeDerrite;
            }

            return tiempoEnEstado < arte.segundosFormarse ? arte.capaSeForma : arte.capaEntera;
        }

        // --- Montaje ---

        internal void Montar(FranjaInundable franja, ArteDeFranja arte)
        {
            _franja = franja;
            _arte = arte;

            var grietas = Repartir(franja.ancho, Mathf.RoundToInt(franja.ancho / arte.separacionAviso));
            _avisos = new SpriteRenderer[grietas.Length];
            for (var i = 0; i < grietas.Length; i++)
            {
                _avisos[i] = Pieza($"Grieta_{i + 1}", new Vector2(grietas[i], 0f), arte.aviso[0], OrdenAviso);
            }

            var grupos = Repartir(franja.ancho, Mathf.RoundToInt(franja.ancho / arte.anchoPuas));
            _puas = new SpriteRenderer[grupos.Length];
            for (var i = 0; i < grupos.Length; i++)
            {
                _puas[i] = Pieza($"Puas_{i + 1}", new Vector2(grupos[i], 0f), arte.puasAviso, OrdenPuas);
            }

            var losas = RepartirLosas(franja.ancho, arte.anchoLosa);
            _losas = new SpriteRenderer[losas.Length];
            for (var i = 0; i < losas.Length; i++)
            {
                _losas[i] = Pieza($"Losa_{i + 1}", new Vector2(losas[i], franja.alturaCapa), arte.capaEntera, OrdenCapa);
            }

            // El cuerpo del agua: un rectángulo liso, del color con el que acaba la superficie.
            _cuerpo = Pieza("CuerpoDelAgua", Vector2.zero, SpriteProvisional.Cuadrado(), FranjaInundable.OrdenAgua);
            _cuerpo.drawMode = SpriteDrawMode.Sliced;

            _superficie = Pieza("SuperficieDelAgua", Vector2.zero, arte.superficie[0], FranjaInundable.OrdenAgua);
            _superficie.drawMode = SpriteDrawMode.Tiled;
            _superficie.tileMode = SpriteTileMode.Continuous;
            _superficie.size = new Vector2(franja.ancho, arte.superficie[0].bounds.size.y);

            Pintar();
        }

        private SpriteRenderer Pieza(string nombre, Vector2 posicion, Sprite sprite, int orden)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetParent(transform, false);
            objeto.transform.localPosition = posicion;
            var dibujo = objeto.AddComponent<SpriteRenderer>();
            dibujo.sprite = sprite;
            dibujo.sortingOrder = orden;
            dibujo.enabled = false;
            return dibujo;
        }

        // Después de la franja, que cambia de estado en su Update.
        private void LateUpdate() => Pintar();

        internal void Pintar()
        {
            if (_franja == null || _arte == null)
            {
                return;
            }

            var estado = _franja.Estado;
            var tiempo = _franja.TiempoEnEstado;

            var avisa = estado == ESTADOS_FRANJA.WARNING;
            var grieta = avisa ? FotogramaDeAviso(_arte, _franja.AvanceDelEstado) : null;
            foreach (var aviso in _avisos)
            {
                Poner(aviso, grieta);
            }

            var puas = FotogramaDePuas(_arte, estado, tiempo, _franja.DuracionPuas);
            foreach (var grupo in _puas)
            {
                Poner(grupo, puas);
            }

            var capa = FotogramaDeCapa(_arte, estado, tiempo, _franja.TiempoRestante);
            foreach (var losa in _losas)
            {
                Poner(losa, capa);
            }

            PintarAgua(estado);
        }

        // La superficie va a la altura del agua; por debajo de ella, el cuerpo liso hasta el suelo.
        private void PintarAgua(ESTADOS_FRANJA estado)
        {
            var nivel = _franja.Nivel;
            var hay = _franja.TieneAgua;
            var conSuperficie = hay && nivel >= _arte.fondoSuperficie;
            var alto = conSuperficie ? nivel - _arte.fondoSuperficie : nivel;

            _cuerpo.enabled = hay && alto > 0.001f;
            if (_cuerpo.enabled)
            {
                _cuerpo.size = new Vector2(_franja.ancho, alto);
                _cuerpo.transform.localPosition = new Vector3(0f, alto * 0.5f, 0f);
            }

            _superficie.enabled = conSuperficie;
            if (conSuperficie)
            {
                _superficie.transform.localPosition = new Vector3(0f, nivel, 0f);
                _superficie.sprite = _arte.superficie[BucleDeSprites.Indice(Time.time, _arte.superficie.Length, _arte.segundosSuperficie)];
            }

            // Poniéndose blanca: el aviso de que se congela.
            var agua = _arte.colorAgua;
            var cinta = new Color(1f, 1f, 1f, agua.a);
            if (estado == ESTADOS_FRANJA.FREEZING)
            {
                var blanco = 0.55f + 0.45f * Mathf.PingPong(Time.time * 5f, 1f);
                var hielo = _franja.colorHielo;
                agua = Color.Lerp(agua, new Color(hielo.r, hielo.g, hielo.b, 0.85f), blanco);
                cinta = Color.Lerp(cinta, Color.white, blanco);
            }

            _cuerpo.color = agua;
            _superficie.color = cinta;
        }

        private static void Poner(SpriteRenderer dibujo, Sprite sprite)
        {
            dibujo.enabled = sprite != null;
            if (sprite != null && dibujo.sprite != sprite)
            {
                dibujo.sprite = sprite;
            }
        }
    }
}
