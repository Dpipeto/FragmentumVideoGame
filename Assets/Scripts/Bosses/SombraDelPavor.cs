using Fragmentum.Core;
using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Una sombra del Espectro del Pavor (fase 2, <c>PatronSombras</c>): una figura sin vida, igual
    /// que él, que también destella y lanza su tajo. Solo se distingue por su luciérnaga, verde; la
    /// del Espectro verdadero es dorada.
    ///
    /// Golpearla la deshace en hojas y castiga a Kael: su luz se encoge un rato
    /// (<see cref="EspectroDelPavor.SombraGolpeada"/>). Golpear al verdadero deshace a todas.
    ///
    /// Está en la capa Enemy, como el jefe: la espada de Kael la nota y Kael la atraviesa.
    /// </summary>
    [DisallowMultipleComponent]
    public class SombraDelPavor : MonoBehaviour, IGolpeable
    {
        private const float Deshaciendose = 0.35f;

        private EspectroDelPavor _duenio;
        private SpriteRenderer _dibujo;
        private Collider2D _cuerpo;
        private Transform _luciernaga;
        private Color _tinte;
        private float _desfase;
        private float _finDestello = float.NegativeInfinity;
        private float _inicioDeshacer = -1f;
        private int _mirando = 1;

        /// <summary>¿Sigue ahí? (Una sombra deshecha ya no recibe golpes.)</summary>
        public bool EstaVivo => _inicioDeshacer < 0f;

        public bool Deshecha => !EstaVivo;

        /// <summary>El centro de su cuerpo.</summary>
        public Vector2 Centro => _cuerpo != null ? (Vector2)_cuerpo.bounds.center : (Vector2)transform.position + Vector2.up * 1.6f;

        public float MedioAncho => _cuerpo != null ? _cuerpo.bounds.extents.x : 1f;

        /// <summary>Hacia dónde mira: 1 derecha, -1 izquierda.</summary>
        public int Mirando => _mirando;

        public bool Destellando => Time.time < _finDestello;

        /// <summary>
        /// Una sombra de <paramref name="duenio"/> de pie en <paramref name="pies"/>: el mismo dibujo,
        /// el mismo cuerpo y una luciérnaga verde.
        /// </summary>
        public static SombraDelPavor Crear(EspectroDelPavor duenio, Vector2 pies, SpriteRenderer como)
        {
            var objeto = new GameObject($"{duenio.name}_Sombra") { layer = duenio.gameObject.layer };
            SceneManagerMover(objeto, duenio.gameObject);
            objeto.transform.position = new Vector3(pies.x, pies.y, 0f);

            var cuerpoRigido = objeto.AddComponent<Rigidbody2D>();
            cuerpoRigido.bodyType = RigidbodyType2D.Kinematic;
            cuerpoRigido.gravityScale = 0f;

            var tamano = duenio.Datos != null ? duenio.Datos.tamano : new Vector2(2f, 3.2f);
            var cuerpo = objeto.AddComponent<CapsuleCollider2D>();
            cuerpo.size = tamano;
            cuerpo.offset = new Vector2(0f, tamano.y * 0.5f);

            var dibujo = new GameObject("Sprite") { layer = objeto.layer };
            dibujo.transform.SetParent(objeto.transform, false);
            var sprite = dibujo.AddComponent<SpriteRenderer>();
            if (como != null)
            {
                sprite.sprite = como.sprite;
                sprite.drawMode = como.drawMode;
                sprite.size = como.size;
                sprite.sharedMaterial = como.sharedMaterial;
                sprite.sortingOrder = como.sortingOrder;
                dibujo.transform.localPosition = como.transform.localPosition;
                dibujo.transform.localScale = new Vector3(Mathf.Abs(como.transform.lossyScale.x), como.transform.lossyScale.y, 1f);
            }

            var sombra = objeto.AddComponent<SombraDelPavor>();
            sombra._duenio = duenio;
            sombra._dibujo = sprite;
            sombra._cuerpo = cuerpo;
            sombra._tinte = duenio.Datos != null ? duenio.Datos.tinte : Color.white;
            sombra._desfase = Random.Range(0f, Mathf.PI * 2f);
            sombra._luciernaga = EspectroDelPavor.CrearLuciernaga(objeto.transform, duenio.ColorLuciernagaSombra, sprite);
            sombra._luciernaga.gameObject.SetActive(true);
            sombra.Mirar(duenio.CentroJugador.x >= pies.x ? 1 : -1);
            return sombra;
        }

        /// <summary>El aviso de su tajo, como el del Espectro: destella <paramref name="segundos"/>.</summary>
        public void Destellar(float segundos) => _finDestello = Time.time + Mathf.Max(0f, segundos);

        public void Mirar(int direccion)
        {
            if (direccion != 0)
            {
                _mirando = direccion > 0 ? 1 : -1;
            }
        }

        /// <summary>Un punto delante de ella, como <see cref="BossController.PuntoDelante"/>.</summary>
        public Vector2 PuntoDelante(float distancia, float altura) =>
            new Vector2(Centro.x + _mirando * distancia, transform.position.y + altura);

        public bool RecibirGolpe(in Golpe golpe)
        {
            if (!EstaVivo)
            {
                return false;
            }

            Deshacer();
            if (_duenio != null)
            {
                _duenio.SombraGolpeada(this);
            }

            return true;
        }

        /// <summary>Se deshace en hojas: deja de recibir golpes, se encoge y desaparece.</summary>
        public void Deshacer()
        {
            if (!EstaVivo)
            {
                return;
            }

            _inicioDeshacer = Time.time;
            if (_cuerpo != null)
            {
                _cuerpo.enabled = false;
            }

            if (_luciernaga != null)
            {
                _luciernaga.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_dibujo == null)
            {
                return;
            }

            if (Deshecha)
            {
                var avance = Mathf.Clamp01((Time.time - _inicioDeshacer) / Deshaciendose);
                var color = Color.Lerp(_tinte, new Color(0.05f, 0.08f, 0.05f, 0f), avance);
                _dibujo.color = color;
                transform.localScale = Vector3.one * (1f - 0.5f * avance);
                if (avance >= 1f)
                {
                    Destroy(gameObject);
                }

                return;
            }

            var tinte = _tinte;
            if (Destellando && Mathf.Repeat(Time.time * 8f, 1f) < 0.5f)
            {
                tinte = new Color(0.72f, 1f, 0.72f, 1f);
            }

            tinte.a = 1f;
            _dibujo.color = tinte;
            _dibujo.flipX = false;
            var escala = _dibujo.transform.localScale;
            _dibujo.transform.localScale = new Vector3(Mathf.Abs(escala.x) * _mirando, escala.y, escala.z);
            EspectroDelPavor.Orbitar(_luciernaga, Centro + Vector2.up * 1.2f, _desfase);
        }

        // La sombra vive en la escena de su jefe (que puede no ser la activa en las pruebas).
        private static void SceneManagerMover(GameObject objeto, GameObject como)
        {
            if (como.scene.IsValid() && objeto.scene != como.scene)
            {
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(objeto, como.scene);
            }
        }
    }
}
