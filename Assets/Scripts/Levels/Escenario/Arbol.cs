using System.Collections.Generic;
using UnityEngine;

namespace Fragmentum.Levels.Escenario
{
    /// <summary>
    /// Un árbol grueso del claro del Nivel 3 (<c>Nivel3_Diseno.md</c>, sección 3). Kael pasa por
    /// delante de él; el Acechador, que embiste a ciegas hacia el ruido, se estrella contra su
    /// tronco y se queda aturdido. Vencerlo es eso: hacerse oír delante de un árbol.
    ///
    /// No tiene collider: con uno, Kael chocaría con él, y el proyecto no tiene una capa que choque
    /// solo con los enemigos. Su tronco (<see cref="Tronco"/>) se registra aquí, y el Acechador lo
    /// mira mientras embiste (<see cref="Choca"/>). Ningún otro enemigo embiste.
    /// </summary>
    [DisallowMultipleComponent]
    public class Arbol : MonoBehaviour
    {
        private static readonly List<Arbol> Registrados = new List<Arbol>();

        [Tooltip("Lo que mide el tronco: de ancho y de alto desde el pie (el Transform).")]
        [SerializeField] private Vector2 tamano = new Vector2(1.2f, 6f);

        /// <summary>Los árboles activos de la escena.</summary>
        public static IReadOnlyList<Arbol> Todos => Registrados;

        public Vector2 Tamano => tamano;

        /// <summary>El tronco, en el mundo: de pie sobre el Transform.</summary>
        public Rect Tronco
        {
            get
            {
                var pie = (Vector2)transform.position;
                return new Rect(pie.x - tamano.x * 0.5f, pie.y, tamano.x, tamano.y);
            }
        }

        /// <summary>Lo ajusta la herramienta que monta la escena.</summary>
        public void Fijar(Vector2 nuevoTamano) => tamano = nuevoTamano;

        /// <summary>
        /// El primer árbol cuyo tronco toca la caja, sin contar los de <paramref name="salvo"/> (los
        /// que ya tocaba al empezar a embestir), o null.
        /// </summary>
        public static Arbol Choca(Rect caja, ICollection<Arbol> salvo = null)
        {
            for (var i = 0; i < Registrados.Count; i++)
            {
                var arbol = Registrados[i];
                if (arbol != null && (salvo == null || !salvo.Contains(arbol)) && arbol.Tronco.Overlaps(caja))
                {
                    return arbol;
                }
            }

            return null;
        }

        /// <summary>Los árboles cuyo tronco toca la caja.</summary>
        public static List<Arbol> Tocando(Rect caja)
        {
            var tocando = new List<Arbol>();
            for (var i = 0; i < Registrados.Count; i++)
            {
                if (Registrados[i] != null && Registrados[i].Tronco.Overlaps(caja))
                {
                    tocando.Add(Registrados[i]);
                }
            }

            return tocando;
        }

        private void OnEnable()
        {
            if (!Registrados.Contains(this))
            {
                Registrados.Add(this);
            }
        }

        private void OnDisable() => Registrados.Remove(this);

        /// <summary>Vacía el registro. Lo usan las pruebas y el arranque sin recarga de dominio.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reiniciar() => Registrados.Clear();

        private void OnDrawGizmos()
        {
            var tronco = Tronco;
            Gizmos.color = new Color(0.45f, 0.3f, 0.15f, 0.8f);
            Gizmos.DrawWireCube(tronco.center, tronco.size);
        }
    }
}
