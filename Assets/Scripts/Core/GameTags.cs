using UnityEngine;

namespace Fragmentum.Core
{
    /// <summary>
    /// Tags del proyecto en un único sitio. Ningún script vuelve a escribir un tag como
    /// literal suelto: así un cambio de nombre se hace una vez y el compilador encuentra
    /// todos los usos.
    ///
    /// El menú <c>Fragmentum → Configurar proyecto</c> crea en el proyecto los que falten.
    /// </summary>
    public static class GameTags
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Boss = "Boss";
        public const string Weapon = "Weapon";
        public const string Suelo = "suelo";
        public const string Lava = "Lava";
        public const string HotPlatform = "HotPlatform";
        public const string IcePlatform = "IcePlatform";
        public const string FalsePlatform = "FalsePlatform";
        public const string Checkpoint = "Checkpoint";
        public const string Memory = "Memory";

        /// <summary>Tags que el configurador del proyecto debe crear (los propios, no los de Unity).</summary>
        public static readonly string[] Propios =
        {
            Enemy, Boss, Weapon, Suelo, Lava, HotPlatform, IcePlatform, FalsePlatform, Checkpoint, Memory
        };
    }

    /// <summary>
    /// Capas de física del proyecto y máscaras derivadas. Sustituye a los <c>LayerMask</c>
    /// sueltos asignados a mano en cada Inspector.
    /// </summary>
    public static class Capas
    {
        public const string Suelo = "suelo";
        public const string Wall = "Wall";
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string PlayerWeapon = "PlayerWeapon";
        public const string EnemyWeapon = "EnemyWeapon";

        /// <summary>
        /// Las cinemáticas en pixel art: solo las ve su cámara (<c>CamaraCinematica</c>). No es de
        /// física: no lleva colliders.
        /// </summary>
        public const string Cinematica = "Cinematica";

        /// <summary>Capas propias del juego, en el orden en que se crean.</summary>
        public static readonly string[] Propias = { Suelo, Wall, Player, Enemy, PlayerWeapon, EnemyWeapon, Cinematica };

        private static int _mascaraSuelo = -1;
        private static int _mascaraPared = -1;
        private static int _mascaraEnemigos = -1;
        private static int _mascaraJugador = -1;

        /// <summary>Máscara del suelo, para la detección de apoyo del jugador.</summary>
        public static int MascaraSuelo => _mascaraSuelo >= 0 ? _mascaraSuelo : _mascaraSuelo = LayerMask.GetMask(Suelo);

        /// <summary>Máscara de paredes, para el wall slide y el wall jump.</summary>
        public static int MascaraPared => _mascaraPared >= 0 ? _mascaraPared : _mascaraPared = LayerMask.GetMask(Wall);

        /// <summary>Máscara de enemigos, para las hitbox del jugador.</summary>
        public static int MascaraEnemigos => _mascaraEnemigos >= 0 ? _mascaraEnemigos : _mascaraEnemigos = LayerMask.GetMask(Enemy);

        /// <summary>Máscara del jugador, para las hitbox de enemigos y jefes.</summary>
        public static int MascaraJugador => _mascaraJugador >= 0 ? _mascaraJugador : _mascaraJugador = LayerMask.GetMask(Player);

        /// <summary>Construye una máscara a partir de nombres de capa.</summary>
        public static int Mascara(params string[] nombres) => LayerMask.GetMask(nombres);

        /// <summary>¿La capa de este objeto está dentro de la máscara?</summary>
        public static bool EnMascara(GameObject objeto, int mascara) =>
            objeto != null && (mascara & (1 << objeto.layer)) != 0;

        /// <summary>Olvida las máscaras cacheadas. Lo usa el configurador tras crear capas.</summary>
        public static void LimpiarCache()
        {
            _mascaraSuelo = -1;
            _mascaraPared = -1;
            _mascaraEnemigos = -1;
            _mascaraJugador = -1;
        }
    }
}
