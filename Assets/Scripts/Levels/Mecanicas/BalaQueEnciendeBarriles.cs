using UnityEngine;

namespace Fragmentum.Levels.Mecanicas
{
    /// <summary>
    /// Lo que añade el Artillero a cada bala: si toca un barril intacto, lo enciende y se apaga en él
    /// (<c>Nivel4_Diseno.md</c>: un barril no es cobertura; un disparo lo enciende).
    /// </summary>
    public class BalaQueEnciendeBarriles : MonoBehaviour
    {
        [Min(0.01f)] public float radio = 0.1f;

        private void FixedUpdate()
        {
            if (BarrilExplosivo.EncenderEn(transform.position, radio))
            {
                Destroy(gameObject);
            }
        }
    }
}
