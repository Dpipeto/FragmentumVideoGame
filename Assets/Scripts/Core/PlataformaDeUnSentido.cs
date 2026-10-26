using UnityEngine;

namespace Fragmentum.Core
{
    /// <summary>
    /// Las plataformas que se atraviesan saltando desde abajo y se pisan al caer encima: un
    /// <c>PlatformEffector2D</c> de un sentido. Son las del mundo de la Ira (las de la arena, las
    /// inestables y las de combustión).
    ///
    /// Para la física ya funcionan así. Lo que hay aquí es para quien pregunta «¿está encima?» con
    /// una consulta (el suelo de Kael, la plataforma inestable): mientras Kael la atraviesa
    /// subiendo, sus pies están dentro de ella, y no la pisa.
    /// </summary>
    public static class PlataformaDeUnSentido
    {
        /// <summary>Lo que pueden quedar los pies por debajo de su cara de arriba estando encima.</summary>
        public const float Tolerancia = 0.05f;

        /// <summary>Por encima de esta velocidad vertical se está subiendo, no apoyado.</summary>
        public const float VelocidadDeSubida = 0.05f;

        /// <summary>¿Se atraviesa desde abajo?</summary>
        public static bool Es(Collider2D plataforma) =>
            plataforma != null && plataforma.usedByEffector &&
            plataforma.TryGetComponent<PlatformEffector2D>(out var efector) && efector.enabled && efector.useOneWay;

        /// <summary>
        /// ¿Está <paramref name="cuerpo"/> de pie sobre <paramref name="plataforma"/>? Con los pies en
        /// su cara de arriba y sin subir.
        /// </summary>
        public static bool Encima(Collider2D plataforma, Collider2D cuerpo)
        {
            var subiendo = cuerpo.attachedRigidbody != null && cuerpo.attachedRigidbody.linearVelocity.y > VelocidadDeSubida;
            return !subiendo && cuerpo.bounds.min.y >= plataforma.bounds.max.y - Tolerancia;
        }

        /// <summary>
        /// ¿Cuenta <paramref name="plataforma"/> como lo que pisa <paramref name="cuerpo"/>? Las sólidas,
        /// siempre que se toquen; las de un sentido, solo con él encima.
        /// </summary>
        public static bool LaPisa(Collider2D plataforma, Collider2D cuerpo) => !Es(plataforma) || Encima(plataforma, cuerpo);

        /// <summary>La hace de un sentido. Al montar el nivel o el prefab, en el editor.</summary>
        public static PlatformEffector2D Hacer(Collider2D plataforma)
        {
            if (!plataforma.TryGetComponent<PlatformEffector2D>(out var efector))
            {
                efector = plataforma.gameObject.AddComponent<PlatformEffector2D>();
            }

            efector.useOneWay = true;
            efector.useSideFriction = false;
            efector.useSideBounce = false;
            efector.surfaceArc = 180f;
            plataforma.usedByEffector = true;
            return efector;
        }
    }
}
