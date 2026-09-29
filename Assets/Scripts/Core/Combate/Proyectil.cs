using UnityEngine;

namespace Fragmentum.Core.Combate
{
    /// <summary>
    /// Proyectil que vuela en línea recta: la bola de la torreta de fuego (Nivel 1) y, más
    /// adelante, las olas de la Doncella o los disparos de los Artilleros (Fase 10).
    ///
    /// Hace daño con un <see cref="Hitbox"/> (respeta capas, i-frames y escudos) y desaparece al
    /// golpear a su objetivo, al tocar el suelo o una pared, o al acabar su alcance.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Hitbox))]
    public class Proyectil : MonoBehaviour
    {
        private Rigidbody2D _cuerpo;
        private CircleCollider2D _zona;
        private Hitbox _hitbox;
        private Vector2 _velocidad;
        private float _muereEn;
        private int _capasQueLoFrenan;

        /// <summary>
        /// Crea un proyectil en la capa EnemyWeapon que golpea a <paramref name="objetivos"/>.
        /// </summary>
        public static Proyectil Disparar(string nombre, Vector2 origen, Vector2 velocidad, float radio, int danio,
            LayerMask objetivos, float alcance, Color color)
        {
            var objeto = new GameObject(nombre);
            objeto.SetActive(false);
            var capa = LayerMask.NameToLayer(Capas.EnemyWeapon);
            if (capa >= 0)
            {
                objeto.layer = capa;
            }

            objeto.transform.position = origen;

            var visual = objeto.AddComponent<SpriteRenderer>();
            visual.sprite = SpriteProvisional.Cuadrado();
            visual.drawMode = SpriteDrawMode.Sliced;
            visual.size = Vector2.one * radio * 2f;
            visual.color = color;
            visual.sortingOrder = 8;

            var cuerpo = objeto.AddComponent<Rigidbody2D>();
            cuerpo.bodyType = RigidbodyType2D.Kinematic;

            var zona = objeto.AddComponent<CircleCollider2D>();
            zona.isTrigger = true;
            zona.radius = radio;

            var hitbox = objeto.AddComponent<Hitbox>();
            hitbox.Danio = danio;
            hitbox.Objetivos = objetivos;

            var proyectil = objeto.AddComponent<Proyectil>();
            proyectil._velocidad = velocidad;
            proyectil._muereEn = Time.time + (velocidad.sqrMagnitude > 0f ? alcance / velocidad.magnitude : 0f);
            objeto.SetActive(true);
            return proyectil;
        }

        public Vector2 Velocidad => _velocidad;

        private void Awake()
        {
            _cuerpo = GetComponent<Rigidbody2D>();
            _zona = GetComponent<CircleCollider2D>();
            _hitbox = GetComponent<Hitbox>();
            _capasQueLoFrenan = Capas.Mascara(Capas.Suelo, Capas.Wall);
        }

        private void OnEnable() => _hitbox.OnImpacto += AlImpactar;

        private void OnDisable() => _hitbox.OnImpacto -= AlImpactar;

        private void FixedUpdate()
        {
            _cuerpo.MovePosition(_cuerpo.position + _velocidad * Time.fixedDeltaTime);

            // Una consulta y no un trigger: la matriz de colisión separa las armas enemigas del
            // escenario (para que las zonas de daño no choquen con él), y la consulta no depende de ella.
            if (Time.time >= _muereEn || Physics2D.OverlapCircle(_cuerpo.position, _zona.radius, _capasQueLoFrenan) != null)
            {
                Destroy(gameObject);
            }
        }

        private void AlImpactar(IGolpeable objetivo, Golpe golpe) => Destroy(gameObject);
    }
}
