using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// La explosión dibujada del mundo de la Culpa (<c>Prompts_Mundo4.md</c>, C2): el estallido, la bola
    /// de fuego, el humo y el cráter, una vez, y se va. La del barril y la de la mina: solo se ve, el
    /// golpe ya lo dio <see cref="Explosiones"/>. Su dibujo nace en el suelo (el pivote, abajo en el
    /// centro) y mide lo que <see cref="radio"/>; para otro alcance se escala.
    ///
    /// Mientras no llega su arte, <see cref="Explosiones"/> pinta un <see cref="DestelloDeExplosion"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExplosionDibujada : MonoBehaviour
    {
        [Tooltip("El alcance que cubre el dibujo a escala 1: la mitad de lo que mide de ancho.")]
        [Min(0.1f)] public float radio = 2.5f;

        [Tooltip("Lo que dura: lo que su clip tarda en llegar al cráter.")]
        [Min(0.05f)] public float duracion = 0.7f;

        /// <summary>
        /// Una copia de <paramref name="prefab"/> con la base en <paramref name="pie"/> y escalada para
        /// cubrir <paramref name="alcance"/>. Se destruye sola.
        /// </summary>
        public static ExplosionDibujada Crear(ExplosionDibujada prefab, Vector2 pie, float alcance)
        {
            var explosion = Instantiate(prefab, pie, Quaternion.identity);
            explosion.name = prefab.name;
            explosion.transform.localScale = Vector3.one * (alcance / Mathf.Max(0.1f, prefab.radio));
            return explosion;
        }

        private void Start() => Destroy(gameObject, duracion);
    }
}
