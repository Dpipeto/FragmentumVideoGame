using Fragmentum.Data;
using UnityEngine;

namespace Fragmentum.Bosses
{
    /// <summary>
    /// Centinela Ardiente, jefe del mundo de la Ira (Anexo A). Es la implementación de referencia.
    ///
    /// - <b>Fase 1:</b> espadazo directo en combo de dos, que deja la espada clavada 1,2 s
    ///   (ventana vulnerable), onda de suelo y salto aplastante.
    /// - <b>Fase 2</b> (50 % de vida): furia. Más rápida, combos de tres con ventana más corta,
    ///   ondas dobles y plataformas en erupción que obligan a moverse.
    /// - <b>Fase 3</b> (25 %): ira desatada. Casi sin pausas entre ataques, erupciones en ráfaga
    ///   y el cuerpo al rojo. No está en el Anexo A: se añadió tras la primera prueba, porque el
    ///   combate se sentía fácil y no transmitía la ira.
    /// - Al entrar en las fases 2 y 3 <b>ruge</b>: un aviso largo y quieta, antes de que empiece la
    ///   fase. Es la señal de que el combate cambia.
    /// - <b>Muerte:</b> explosión (provisional: destello y desvanecido) y «el suelo se
    ///   estabiliza»: al cortarse los ataques desaparecen todas las erupciones.
    /// - <b>Recompensa:</b> Fragmento de la Ira → Ataque Cargado. La otorga la arena al recibir
    ///   <c>OnJefeDerrotado</c>.
    ///
    /// Casi todo es dato: los patrones y los umbrales están en <c>Assets/Data/Jefes</c>. La clase
    /// solo redefine los pasos propios de este jefe (patrón Template Method).
    /// </summary>
    public class CentinelaArdiente : BossController
    {
        private const float Rugido = 0.9f;

        // Color al rojo de la ira desatada, mezclado con el tinte de sus datos (con arte, blanco).
        private static readonly Color AlRojo = new Color(1f, 0.32f, 0.12f, 1f);

        [Tooltip("Cuánto se enrojece el jefe en la fase 3 (0 = nada, 1 = rojo puro).")]
        [SerializeField, Range(0f, 1f)] private float enrojecimientoFase3 = 0.45f;

        protected override void AlCambiarDeFase(int fase)
        {
            if (fase >= 2)
            {
                Avisar(Rugido);
            }

            if (fase >= 3)
            {
                ColorBase = Color.Lerp(datos.tinte, AlRojo, enrojecimientoFase3);
            }
        }

        // Ruge quieta antes de la primera embestida de la fase.
        protected override float EsperaAlEmpezarFase(int fase, FaseJefe datosFase) =>
            fase >= 2 ? Rugido : base.EsperaAlEmpezarFase(fase, datosFase);
    }
}
