using System;
using UnityEngine;

namespace Fragmentum.Core.Save
{
    /// <summary>
    /// Importa el progreso que la v1 guardaba en <c>PlayerPrefs</c> para no perder los datos
    /// de las pruebas ya hechas. La v1 solo leía la clave «Monedas» (nunca la escribía, que es
    /// el bug 3.4), así que es lo único que se puede recuperar.
    ///
    /// La clave antigua no se borra: si algo sale mal, sigue ahí.
    /// </summary>
    public class MigracionPlayerPrefs
    {
        public const string ClaveMonedas = "Monedas";

        private readonly Func<string, bool> _tieneClave;
        private readonly Func<string, int> _leerEntero;

        /// <summary>Por defecto lee los PlayerPrefs reales; las pruebas inyectan otra fuente.</summary>
        public MigracionPlayerPrefs(Func<string, bool> tieneClave = null, Func<string, int> leerEntero = null)
        {
            _tieneClave = tieneClave ?? PlayerPrefs.HasKey;
            _leerEntero = leerEntero ?? (clave => PlayerPrefs.GetInt(clave, 0));
        }

        /// <summary>Aplica la migración sobre datos nuevos. Devuelve si importó algo.</summary>
        public bool Aplicar(DatosGuardado datos)
        {
            if (datos == null || datos.migradoDePlayerPrefs || !_tieneClave(ClaveMonedas))
            {
                return false;
            }

            datos.monedas = Mathf.Max(0, _leerEntero(ClaveMonedas));
            datos.migradoDePlayerPrefs = true;
            Debug.Log($"[Guardado] Se importaron {datos.monedas} monedas de los PlayerPrefs de la versión anterior.");
            return true;
        }
    }
}
