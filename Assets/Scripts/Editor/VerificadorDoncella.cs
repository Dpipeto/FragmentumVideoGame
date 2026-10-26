using System.Collections.Generic;
using System.Linq;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Core;
using Fragmentum.Data;
using UnityEditor;
using UnityEngine;
using static Fragmentum.EditorTools.ComprobacionesArte;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Comprueba que a la Doncella del Lamento se la puede vencer. Solo se la hiere mientras llora,
    /// así que un cambio en sus datos puede dejarla invencible sin que nada falle a la vista:
    /// 1. sus datos: es inmune fuera de la ventana, cada fase tiene patrones y entre ellos un
    ///    llanto (sin él no hay ventana), los ataques avisan al menos <see cref="AvisoMinimo"/>
    ///    segundos y, donde se congela, antes hay una inundación (sin agua no hay nada que congelar);
    /// 2. su prefab: lleva su controlador con esos datos, se la puede golpear y, en reposo, todo lo
    ///    que hace daño está apagado.
    ///
    /// Falla ruidosamente: un error en la consola por cada problema. Se ejecuta desde el menú y al
    /// final de «Regenerar todo», que falla si algo no cuadra. Cada comprobación recibe lo que
    /// revisa, para probarla con objetos rotos a propósito.
    /// </summary>
    public static class VerificadorDoncella
    {
        public const string NombreDatos = "DoncellaLamento";

        /// <summary>Lo menos que avisa un ataque: más rápido deja de poder leerse.</summary>
        public const float AvisoMinimo = 0.3f;

        public static string RutaPrefab => $"{GeneradorJefes.CarpetaPrefabs}/{NombreDatos}.prefab";

        [MenuItem("Fragmentum/Jefes/Verificar Doncella", priority = 144)]
        public static void MenuVerificar() => Verificar();

        /// <summary>Revisa todo. Devuelve true si está bien; si no, deja un error por problema en la consola.</summary>
        public static bool Verificar()
        {
            var problemas = Problemas();
            if (problemas.Count == 0)
            {
                Debug.Log("[Doncella] Verificación correcta: sus datos, sus fases con llanto y el prefab.");
                return true;
            }

            foreach (var problema in problemas)
            {
                Debug.LogError($"[Doncella] {problema}");
            }

            Debug.LogError($"[Doncella] La verificación falló: {problemas.Count} problema(s). " +
                           "Fragmentum → Jefes → Restablecer datos de jefes al Anexo A los vuelve a poner como en el diseño.");
            return false;
        }

        /// <summary>Todos los problemas encontrados; vacío si todo está bien.</summary>
        public static List<string> Problemas()
        {
            var datos = GeneradorJefes.CargarDatos(NombreDatos);
            var problemas = ProblemasDeLosDatos(datos);
            problemas.AddRange(ProblemasDelPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab), datos));
            return problemas;
        }

        // --- 1. Datos ---

        /// <summary>Sus datos: inmune fuera del llanto, y cada fase con ataques legibles y un llanto.</summary>
        public static List<string> ProblemasDeLosDatos(JefeData datos)
        {
            var problemas = new List<string>();
            if (datos == null)
            {
                problemas.Add($"Faltan los datos del jefe ({GeneradorJefes.CarpetaDatos}/{NombreDatos}.asset).");
                return problemas;
            }

            if (!datos.inmuneFueraDeVulnerable)
            {
                problemas.Add("Datos: no es inmune fuera de la ventana vulnerable. Solo se la hiere mientras llora.");
            }

            if (datos.fases == null || datos.fases.Count == 0)
            {
                problemas.Add("Datos: no tiene fases.");
                return problemas;
            }

            foreach (var fase in datos.fases)
            {
                problemas.AddRange(ProblemasDeLaFase(fase, datos));
            }

            return problemas;
        }

        /// <summary>Una fase: con patrones, con un llanto, con avisos que se leen y sin congelar en seco.</summary>
        public static List<string> ProblemasDeLaFase(FaseJefe fase, JefeData datos)
        {
            var problemas = new List<string>();
            var nombre = fase != null ? fase.nombre : "(vacía)";
            if (fase == null || fase.patrones == null || fase.patrones.Count == 0)
            {
                problemas.Add($"Fase «{nombre}»: no tiene patrones. Espera sin atacar y, como es inmune, no se la puede vencer.");
                return problemas;
            }

            if (fase.patrones.Any(p => p == null))
            {
                problemas.Add($"Fase «{nombre}»: tiene un patrón vacío.");
            }

            var llantos = fase.patrones.OfType<PatronLlanto>().ToList();
            if (llantos.Count == 0)
            {
                problemas.Add($"Fase «{nombre}»: no tiene llanto. Sin él no se abre la ventana vulnerable y no se la puede vencer.");
            }

            foreach (var llanto in llantos)
            {
                if (llanto.duracion <= 0f && (datos == null || datos.duracionVulnerable <= 0f))
                {
                    problemas.Add($"Fase «{nombre}»: el llanto «{llanto.name}» dura 0 s.");
                }
            }

            foreach (var patron in fase.patrones)
            {
                if (patron != null && !(patron is PatronLlanto) && patron.aviso < AvisoMinimo)
                {
                    problemas.Add($"Fase «{nombre}»: «{patron.name}» avisa {Numero(patron.aviso)} s, menos de {Numero(AvisoMinimo)}.");
                }
            }

            if (fase.patrones.OfType<PatronCongelar>().Any() && !fase.patrones.OfType<PatronInundacion>().Any())
            {
                problemas.Add($"Fase «{nombre}»: congela, pero no inunda. Sin agua no hay nada que congelar.");
            }

            return problemas;
        }

        // --- 2. Prefab ---

        /// <summary>
        /// El prefab: con su controlador y sus datos, con un cuerpo en la capa Enemy (la espada de
        /// Kael solo golpea esa capa) y, en reposo, sin nada que haga daño encendido.
        /// </summary>
        public static List<string> ProblemasDelPrefab(GameObject prefab, JefeData datos)
        {
            var problemas = new List<string>();
            if (prefab == null)
            {
                problemas.Add($"Falta el prefab del jefe ({RutaPrefab}).");
                return problemas;
            }

            var doncella = prefab.GetComponent<DoncellaLamento>();
            if (doncella == null)
            {
                problemas.Add($"Prefab {prefab.name}: le falta su controlador, DoncellaLamento.");
            }
            else if (datos != null && doncella.Datos != datos)
            {
                problemas.Add($"Prefab {prefab.name}: no usa los datos de {NombreDatos}.asset.");
            }

            var cuerpo = prefab.GetComponent<Collider2D>();
            if (cuerpo == null || !cuerpo.enabled || prefab.layer != LayerMask.NameToLayer(Capas.Enemy))
            {
                problemas.Add($"Prefab {prefab.name}: no tiene un cuerpo en la capa {Capas.Enemy}. La espada no le daría ni llorando.");
            }

            problemas.AddRange(ProblemasDeDanioEnReposo(prefab));
            return problemas;
        }
    }
}
