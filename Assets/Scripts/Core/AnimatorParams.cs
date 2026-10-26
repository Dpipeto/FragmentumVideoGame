using UnityEngine;

namespace Fragmentum.Core
{
    /// <summary>
    /// Parámetros del Animator en un único sitio, con sus hashes ya calculados.
    ///
    /// Los métodos <c>Fijar*</c> comprueban que el parámetro exista antes de escribirlo. Así
    /// un Animator al que todavía le faltan clips (por ejemplo, el del jefe, que solo tiene
    /// "Walk") no llena la consola de avisos por frame: se avisa una sola vez y el juego sigue.
    ///
    /// Se conservan los nombres que ya usaba el prototipo: MovEn, Attack, Damage, Death,
    /// Movement y Walk. En Kael, el clip lo elige <c>PlayerAnimacion</c> según el estado de la
    /// FSM; de estos parámetros, Movement marca el ritmo de las piernas al correr y
    /// VelocidadAtaque estira el clip de ataque a la duración del golpe.
    /// </summary>
    public static class AnimatorParams
    {
        /// <summary>Nombre y hash de un parámetro del Animator.</summary>
        public readonly struct Parametro
        {
            public Parametro(string nombre)
            {
                Nombre = nombre;
                Hash = Animator.StringToHash(nombre);
            }

            public string Nombre { get; }
            public int Hash { get; }
        }

        // Parámetros heredados del prototipo (no se renombran).
        public static readonly Parametro MovEn = new Parametro("MovEn");
        public static readonly Parametro Attack = new Parametro("Attack");
        public static readonly Parametro Damage = new Parametro("Damage");
        public static readonly Parametro Death = new Parametro("Death");
        public static readonly Parametro Movement = new Parametro("Movement");
        public static readonly Parametro Walk = new Parametro("Walk");

        // Multiplicador de velocidad del estado Ataque. En Kael, 1 / duración del golpe en
        // segundos; en los Ecos, la velocidadAtaque de su EnemigoAnimacion.
        public static readonly Parametro VelocidadAtaque = new Parametro("VelocidadAtaque");

        // Multiplicador del estado Dash de Kael: 1 / duración del dash en segundos.
        public static readonly Parametro VelocidadDash = new Parametro("VelocidadDash");

        // Multiplicador del estado Run de los Ecos: velocidad real / la del ciclo dibujado.
        public static readonly Parametro VelocidadCorrer = new Parametro("VelocidadCorrer");

        // La Centinela Ardiente. Andar y morir usan Walk y Death; cada ataque tiene su disparador,
        // que lo empieza al momento desde cualquier estado.
        public static readonly Parametro Espadazo = new Parametro("Espadazo");
        public static readonly Parametro GolpeSuelo = new Parametro("GolpeSuelo");
        public static readonly Parametro Salto = new Parametro("Salto");
        public static readonly Parametro Rugido = new Parametro("Rugido");

        // El espadazo acaba con la espada clavada (C1-C4) en vez de recuperarse (E3-E4).
        public static readonly Parametro Clavar = new Parametro("Clavar");

        // Las mecánicas con arte (torreta, plataformas): el estado de su ciclo, un entero que
        // pone su script con los valores de su enum (ESTADOS_TORRETA, …).
        public static readonly Parametro EstadoMecanica = new Parametro("Estado");

        // Los dibujos de las zonas de daño que siguen sus fases (la erupción): la zona ya quema
        // y la zona ya acabó (Combate.DibujoDeZona).
        public static readonly Parametro Quema = new Parametro("Quema");
        public static readonly Parametro Apagada = new Parametro("Apagada");

        // La Escultura Viviente tiene hiperarmadura: no se aturde, pero acusa el golpe (H1). El
        // disparador lo pone su script, no la FSM.
        public static readonly Parametro Golpeada = new Parametro("Golpeada");

        // La Medusa de Hielo: el golpe que la aparta (A1) y el segundo que pasa encogida (A2).
        public static readonly Parametro Apartada = new Parametro("Apartada");
        public static readonly Parametro Encogida = new Parametro("Encogida");

        // La Doncella del Lamento: el gesto de cada ataque tiene su disparador (el rugido y la
        // muerte usan Rugido y Death). El llanto va con «Estado», que pone su script: 0 flota,
        // 1 baja a llorar y 2 llora.
        public static readonly Parametro Ola = new Parametro("Ola");
        public static readonly Parametro Inundar = new Parametro("Inundar");
        public static readonly Parametro Congelar = new Parametro("Congelar");

        /// <summary>¿El Animator tiene este parámetro? Avisa una sola vez por parámetro ausente.</summary>
        public static bool Existe(Animator animator, in Parametro parametro)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            var parametros = animator.parameters;
            for (var i = 0; i < parametros.Length; i++)
            {
                if (parametros[i].nameHash == parametro.Hash)
                {
                    return true;
                }
            }

            AvisarUnaVez(animator, parametro);
            return false;
        }

        public static void FijarBool(Animator animator, in Parametro parametro, bool valor)
        {
            if (Existe(animator, parametro))
            {
                animator.SetBool(parametro.Hash, valor);
            }
        }

        public static void FijarFloat(Animator animator, in Parametro parametro, float valor)
        {
            if (Existe(animator, parametro))
            {
                animator.SetFloat(parametro.Hash, valor);
            }
        }

        public static void FijarEntero(Animator animator, in Parametro parametro, int valor)
        {
            if (Existe(animator, parametro))
            {
                animator.SetInteger(parametro.Hash, valor);
            }
        }

        public static void Disparar(Animator animator, in Parametro parametro)
        {
            if (Existe(animator, parametro))
            {
                animator.SetTrigger(parametro.Hash);
            }
        }

        // Se guarda el nombre, no el objeto, para no retener Animators ya destruidos.
        private static readonly System.Collections.Generic.HashSet<(string animator, int parametro)> Avisados =
            new System.Collections.Generic.HashSet<(string animator, int parametro)>();

        private static void AvisarUnaVez(Animator animator, in Parametro parametro)
        {
            if (!Avisados.Add((animator.name, parametro.Hash)))
            {
                return;
            }

            Debug.LogWarning(
                $"[Animator] '{animator.name}' no tiene el parámetro '{parametro.Nombre}'. " +
                "La animación correspondiente queda pendiente; el juego continúa. " +
                "Apúntalo en PENDIENTES.md.",
                animator);
        }
    }
}
