using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Fragmentum.Core
{
    /// <summary>
    /// Referencia a una escena que se elige arrastrando el asset en el Inspector, en lugar de
    /// escribir su nombre. Guarda también la ruta, que es lo único que existe en una build.
    ///
    /// Sustituye a los nombres de escena por concatenación de la v1 (<c>"Level" + i</c>,
    /// <c>"Menu_Inicial"</c>): un renombrado ya no rompe nada en silencio, y
    /// <see cref="EnBuild"/> dice si la escena se puede cargar de verdad.
    /// </summary>
    [Serializable]
    public class ReferenciaEscena
    {
        [Tooltip("La escena (asset). Solo se usa en el Editor; en la build cuenta la ruta.")]
        [SerializeField] private Object escena;

        [SerializeField] private string ruta;

        /// <summary>Ruta del asset, por ejemplo <c>Assets/Scenes/Nivel1.unity</c>.</summary>
        public string Ruta => ruta ?? string.Empty;

        /// <summary>Nombre de la escena, sin carpeta ni extensión.</summary>
        public string Nombre => string.IsNullOrEmpty(ruta) ? string.Empty : Path.GetFileNameWithoutExtension(ruta);

        public bool Asignada => !string.IsNullOrEmpty(ruta);

        /// <summary>¿Está en Build Settings, y por tanto se puede cargar?</summary>
        public bool EnBuild => Asignada && SceneUtility.GetBuildIndexByScenePath(ruta) >= 0;

        public override string ToString() => Asignada ? Nombre : "(sin escena)";

#if UNITY_EDITOR
        public UnityEditor.SceneAsset Asset => escena as UnityEditor.SceneAsset;

        /// <summary>Crea la referencia a partir del asset (herramientas de Editor).</summary>
        public static ReferenciaEscena Desde(UnityEditor.SceneAsset asset)
        {
            var referencia = new ReferenciaEscena { escena = asset };
            referencia.Refrescar();
            return referencia;
        }

        /// <summary>
        /// Actualiza la ruta a partir del asset (por si la escena se movió o se renombró).
        /// Devuelve si cambió algo.
        /// </summary>
        public bool Refrescar()
        {
            var nueva = escena != null ? UnityEditor.AssetDatabase.GetAssetPath(escena) : string.Empty;
            if (nueva == ruta)
            {
                return false;
            }

            ruta = nueva;
            return true;
        }
#endif
    }
}
