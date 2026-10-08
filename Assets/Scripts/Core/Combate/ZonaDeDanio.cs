using UnityEngine;

namespace Fragmentum.Core.Combate
{
    /// <summary>
    /// Zona de daño temporal creada por código: el espadazo, la onda de suelo o la erupción de
    /// un jefe y, más adelante, trampas y proyectiles sencillos (Fases 9 y 10).
    ///
    /// - Puede <b>avisar</b> antes: se ve translúcida y no hace daño.
    /// - Puede <b>moverse</b> a velocidad constante (la onda de suelo).
    /// - <b>Desaparece sola</b> al acabar su duración.
    ///
    /// El daño lo reparte un <see cref="Hitbox"/>, así que respeta las capas, los i-frames y los
    /// escudos como cualquier otro golpe. Se dibuja como un rectángulo de color, salvo que se
    /// cree sin dibujo porque el golpe ya lo enseña una animación (el espadazo) o porque lleva
    /// su propio dibujo dentro (<see cref="Vestir"/>, la onda).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(Hitbox))]
    public class ZonaDeDanio : MonoBehaviour
    {
        private BoxCollider2D _zona;
        private Rigidbody2D _cuerpo;
        private Hitbox _hitbox;
        private SpriteRenderer _visual;
        private Color _color;
        private Vector2 _velocidad;
        private float _inicio;
        private float _aviso;
        private float _duracion;

        /// <summary>¿Ya hace daño? (Durante el aviso, no.)</summary>
        public bool Activa { get; private set; }

        public Hitbox Hitbox => _hitbox;

        public Vector2 Tamano => _zona != null ? _zona.size : Vector2.zero;

        /// <summary>
        /// Crea la zona en la capa EnemyWeapon. <paramref name="objetivos"/> son las capas a las que
        /// golpea; <paramref name="velocidad"/> en unidades por segundo (cero = quieta). Sin
        /// <paramref name="dibujar"/>, el rectángulo no se ve y solo queda el daño.
        /// </summary>
        public static ZonaDeDanio Crear(string nombre, Vector2 centro, Vector2 tamano, int danio, LayerMask objetivos,
            float aviso, float duracion, Vector2 velocidad, Color color, bool dibujar = true)
        {
            var objeto = new GameObject(nombre);
            objeto.SetActive(false);
            var capa = LayerMask.NameToLayer(Capas.EnemyWeapon);
            if (capa >= 0)
            {
                objeto.layer = capa;
            }

            objeto.transform.position = centro;

            var visual = objeto.AddComponent<SpriteRenderer>();
            visual.sprite = SpriteProvisional.Cuadrado();
            visual.drawMode = SpriteDrawMode.Sliced;
            visual.size = tamano;
            visual.sortingOrder = 8;
            visual.enabled = dibujar;

            var zona = objeto.AddComponent<BoxCollider2D>();
            zona.isTrigger = true;
            zona.size = tamano;

            var cuerpo = objeto.AddComponent<Rigidbody2D>();
            cuerpo.bodyType = RigidbodyType2D.Kinematic;

            var hitbox = objeto.AddComponent<Hitbox>();
            hitbox.Danio = danio;
            hitbox.Objetivos = objetivos;
            hitbox.GolpearUnaVezPorActivacion = true;

            var efecto = objeto.AddComponent<ZonaDeDanio>();
            efecto.Configurar(aviso, duracion, velocidad, color);
            objeto.SetActive(true);
            return efecto;
        }

        /// <summary>
        /// Pone dentro de la zona el dibujo de su ataque (un prefab con su animación, como la
        /// onda de la Centinela) en lugar del rectángulo. El pivote del dibujo es su frente en el
        /// suelo: cae en el borde de la zona que va por delante y en su base. Mirando a la
        /// izquierda (<paramref name="direccion"/> &lt; 0) se espeja. Sin prefab no hace nada.
        /// </summary>
        public GameObject Vestir(GameObject dibujo, int direccion)
        {
            if (dibujo == null)
            {
                return null;
            }

            var lado = direccion < 0 ? -1f : 1f;
            var tamano = Tamano;
            var objeto = Instantiate(dibujo, transform);
            objeto.name = dibujo.name;
            objeto.transform.localPosition = new Vector3(lado * tamano.x * 0.5f, -tamano.y * 0.5f, 0f);
            var escala = objeto.transform.localScale;
            objeto.transform.localScale = new Vector3(Mathf.Abs(escala.x) * lado, escala.y, escala.z);
            return objeto;
        }

        private void Configurar(float aviso, float duracion, Vector2 velocidad, Color color)
        {
            _zona = GetComponent<BoxCollider2D>();
            _cuerpo = GetComponent<Rigidbody2D>();
            _hitbox = GetComponent<Hitbox>();
            _visual = GetComponent<SpriteRenderer>();

            _aviso = Mathf.Max(0f, aviso);
            _duracion = Mathf.Max(0f, duracion);
            _velocidad = velocidad;
            _color = color;
            _inicio = Time.time;

            Activa = _aviso <= 0f;
            _zona.enabled = Activa;
            Pintar();
        }

        private void FixedUpdate()
        {
            var transcurrido = Time.time - _inicio;
            if (!Activa && transcurrido >= _aviso)
            {
                Activa = true;
                _hitbox.ReiniciarObjetivos();
                _zona.enabled = true;
            }

            if (Activa && _velocidad != Vector2.zero)
            {
                _cuerpo.MovePosition(_cuerpo.position + _velocidad * Time.fixedDeltaTime);
            }

            if (transcurrido >= _aviso + _duracion)
            {
                Destroy(gameObject);
            }
        }

        private void Update() => Pintar();

        // Durante el aviso, translúcida y latiendo; activa, casi opaca.
        private void Pintar()
        {
            if (_visual == null)
            {
                return;
            }

            var alfa = Activa ? 0.85f : 0.2f + 0.2f * Mathf.PingPong(Time.time * 4f, 1f);
            _visual.color = new Color(_color.r, _color.g, _color.b, alfa);
        }
    }
}
