using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fragmentum.Core
{
    /// <summary>
    /// Crea los sistemas persistentes (GameManager, FSM global, cargador de escenas, economía)
    /// antes de que se cargue la primera escena, sea cual sea: el menú en una build, o
    /// cualquier nivel al darle a Play en el Editor. Así ninguna escena necesita llevar su
    /// propio GameManager (en la v1 cada escena tenía uno, que se destruía al salir).
    ///
    /// Los sistemas salen de <c>Resources/Sistemas.prefab</c>. Si falta, se crean vacíos y se
    /// avisa: el juego arranca igual, sin progresión.
    /// </summary>
    public static class Bootstrap
    {
        public const string RutaRecurso = "Sistemas";

        /// <summary>Las pruebas lo activan para montar sus propios sistemas.</summary>
        public static bool Desactivado { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Arrancar()
        {
            // El ejecutor de pruebas de Unity arranca en una escena «InitTestScene…»: las
            // pruebas crean sus propios sistemas y no deben tocar la partida real.
            if (Desactivado || GameManager.Instancia != null ||
                SceneManager.GetActiveScene().name.StartsWith("InitTestScene"))
            {
                return;
            }

            CrearSistemas();
        }

        /// <summary>Instancia los sistemas persistentes y los marca para sobrevivir a los cambios de escena.</summary>
        public static GameObject CrearSistemas()
        {
            var prefab = Resources.Load<GameObject>(RutaRecurso);
            GameObject sistemas;
            if (prefab != null)
            {
                sistemas = Object.Instantiate(prefab);
            }
            else
            {
                Debug.LogWarning(
                    $"[Bootstrap] Falta Resources/{RutaRecurso}.prefab: se crean los sistemas sin LevelDatabase " +
                    "(sin menú ni progresión). Ejecuta Fragmentum → Regenerar todo.");
                sistemas = new GameObject();
                sistemas.AddComponent<EconomyManager>();
                sistemas.AddComponent<GameManager>();
                sistemas.AddComponent<SceneLoader>();
                sistemas.AddComponent<GameStateMachine>();
            }

            sistemas.name = RutaRecurso;
            Object.DontDestroyOnLoad(sistemas);
            return sistemas;
        }
    }
}
