using Fragmentum.Core.Combate;
using UnityEngine;

namespace Fragmentum.Levels.Pruebas
{
    /// <summary>
    /// Muñeco de entrenamiento para las escenas de prueba: recibe golpes, destella, escribe
    /// el daño en la consola (distingue el ataque cargado) y revive solo al morir.
    /// Sirve para comprobar la hitbox de la espada antes de que existan enemigos (Fase 4).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class ObjetivoDePrueba : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Color colorGolpe = Color.white;
        [SerializeField] private Color colorGolpeCargado = new Color(1f, 0.5f, 0.2f);
        [SerializeField] private float duracionDestello = 0.12f;
        [SerializeField] private float esperaReinicio = 1f;

        private Health _vida;
        private Color _colorBase;
        private Color _colorDestello;
        private float _finDestello;

        private void Awake()
        {
            _vida = GetComponent<Health>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) _colorBase = sprite.color;
        }

        private void OnEnable()
        {
            _vida.OnGolpeRecibido += AlRecibirGolpe;
            _vida.OnMuerte += AlMorir;
        }

        private void OnDisable()
        {
            _vida.OnGolpeRecibido -= AlRecibirGolpe;
            _vida.OnMuerte -= AlMorir;
        }

        private void Update()
        {
            if (sprite != null)
            {
                sprite.color = Time.time < _finDestello ? _colorDestello : _colorBase;
            }
        }

        private void AlRecibirGolpe(Golpe golpe)
        {
            _finDestello = Time.time + duracionDestello;
            _colorDestello = golpe.Cargado ? colorGolpeCargado : colorGolpe;
            Debug.Log(
                $"[Prueba] {name} recibe {golpe.Cantidad} de daño{(golpe.Cargado ? " (ATAQUE CARGADO)" : string.Empty)}. " +
                $"Vida: {_vida.Vida}/{_vida.VidaMaxima}", this);
        }

        private void AlMorir()
        {
            Debug.Log($"[Prueba] {name} derribado; revive en {esperaReinicio:0.0} s.", this);
            Invoke(nameof(Reiniciar), esperaReinicio);
        }

        private void Reiniciar() => _vida.Revivir();
    }
}
