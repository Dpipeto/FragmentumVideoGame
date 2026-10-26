using Fragmentum.Core;
using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Lo que se ve de una explosión mientras no llega su arte (<c>Prompts_Mundo4.md</c>, apartado C):
    /// un cuadrado anaranjado del tamaño de su alcance que se apaga en un momento. Solo se ve: el
    /// golpe ya lo dio <see cref="Explosiones"/>.
    /// </summary>
    public class DestelloDeExplosion : MonoBehaviour
    {
        /// <summary>Lo que tarda en apagarse.</summary>
        public const float Duracion = 0.35f;

        public static readonly Color ColorDestello = new Color(1f, 0.55f, 0.2f, 0.6f);

        private SpriteRenderer _dibujo;
        private float _desde;

        /// <summary>Crea el destello en <paramref name="centro"/>, de <paramref name="radio"/> de alcance.</summary>
        public static DestelloDeExplosion Crear(Vector2 centro, float radio)
        {
            var objeto = new GameObject("Explosion");
            objeto.transform.position = centro;
            objeto.transform.localScale = new Vector3(radio * 2f, radio * 2f, 1f);
            var dibujo = objeto.AddComponent<SpriteRenderer>();
            dibujo.sprite = SpriteProvisional.Cuadrado();
            dibujo.color = ColorDestello;
            dibujo.sortingOrder = 20;
            var destello = objeto.AddComponent<DestelloDeExplosion>();
            destello._dibujo = dibujo;
            destello._desde = Time.time;
            return destello;
        }

        private void Update()
        {
            var t = (Time.time - _desde) / Duracion;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            _dibujo.color = new Color(ColorDestello.r, ColorDestello.g, ColorDestello.b, ColorDestello.a * (1f - t));
        }
    }
}
