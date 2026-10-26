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
    /// - Al empezar el combate y al entrar en las fases 2 y 3 <b>ruge</b>: un aviso largo y quieta,
    ///   antes de su primer ataque. Al empezar, es la Centinela que se levanta tras su presentación
    ///   (el ojo que se abre); después, la señal de que el combate cambia.
    /// - <b>Muerte:</b> cae (D1-D3) y se queda en D4, la ceniza con el fragmento blanco flotando.
    ///   «El suelo se estabiliza»: al cortarse los ataques desaparecen todas las erupciones.
    /// - <b>Recompensa:</b> Fragmento de la Ira → Ataque Cargado. La otorga la arena cuando Kael
    ///   recoge el fragmento de la ceniza (<c>OnFragmentoRecogido</c>).
    /// - <b>Espada clavada:</b> mientras forcejea en C2, late en contrafase: brilla con el sprite
    ///   abajo y se apaga al subir.
    ///
    /// Casi todo es dato: los patrones y los umbrales están en <c>Assets/Data/Jefes</c>. La clase
    /// solo redefine los pasos propios de este jefe (patrón Template Method).
    /// </summary>
    public class CentinelaArdiente : BossController
    {
        private const float Rugido = 0.9f;

        // Color al rojo de la ira desatada, mezclado con el tinte de sus datos (con arte, blanco).
        private static readonly Color AlRojo = new Color(1f, 0.32f, 0.12f, 1f);

        // Brasa del latido de la espada clavada.
        private static readonly Color Brasa = new Color(1f, 0.78f, 0.45f, 1f);

        [Tooltip("Cuánto se enrojece el jefe en la fase 3 (0 = nada, 1 = rojo puro).")]
        [SerializeField, Range(0f, 1f)] private float enrojecimientoFase3 = 0.45f;

        protected override void AlCambiarDeFase(int fase)
        {
            Avisar(Rugido);
            Rugir();

            if (fase >= 3)
            {
                ColorBase = Color.Lerp(datos.tinte, AlRojo, enrojecimientoFase3);
            }
        }

        // Ruge quieta antes de la primera embestida de cada fase. Dura lo que el clip (R1-R3, 0,9 s).
        protected override float EsperaAlEmpezarFase(int fase, FaseJefe datosFase) => Rugido;

        // Con arte, el latido va en contrafase con el forcejeo de C2: el sprite abajo, brasa;
        // arriba, casi apagado.
        protected override Color ColorVentana(Color colorBase) =>
            TieneArte ? Color.Lerp(colorBase, Brasa, Arte.Elevado ? 0.15f : 0.7f) : base.ColorVentana(colorBase);
    }
}
