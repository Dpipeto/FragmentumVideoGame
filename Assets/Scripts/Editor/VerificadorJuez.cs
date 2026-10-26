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
    /// Comprueba que al Juez Silente se le puede vencer. Es inmune salvo cuando Kael mira la memoria de su
    /// ataque, así que un cambio en sus datos puede dejarlo invencible sin que nada falle a la vista:
    /// 1. sus datos: inmune fuera de su ventana, con ventana, sin daño por contacto, y cada fase con sus
    ///    ataques, todos memorias (<see cref="PatronMemoria"/>), las cuatro, y avisando al menos
    ///    <see cref="AvisoMinimo"/> s (hay que llegar a la marca esquivando);
    /// 2. su prefab: lleva su controlador con esos datos, un cuerpo que la espada nota y, en reposo,
    ///    nada de lo que hace daño encendido.
    ///
    /// Las marcas de la plaza las comprueba <c>EscenaNivel4Tests</c>. Falla ruidosamente, como los del
    /// Pavor y la Doncella: un error en la consola por cada problema. Se ejecuta desde el menú y al final
    /// de «Regenerar todo».
    /// </summary>
    public static class VerificadorJuez
    {
        public const string NombreDatos = "JuezSilente";

        /// <summary>Lo menos que avisa un ataque: hay que llegar a su marca.</summary>
        public const float AvisoMinimo = 0.8f;

        public static string RutaPrefab => $"{GeneradorJefes.CarpetaPrefabs}/{NombreDatos}.prefab";

        [MenuItem("Fragmentum/Jefes/Verificar Juez Silente", priority = 146)]
        public static void MenuVerificar() => Verificar();

        /// <summary>Revisa todo. Devuelve true si está bien; si no, deja un error por problema en la consola.</summary>
        public static bool Verificar()
        {
            var problemas = Problemas();
            if (problemas.Count == 0)
            {
                Debug.Log("[Juez] Verificación correcta: sus datos, sus fases con sus cuatro memorias y el prefab.");
                return true;
            }

            foreach (var problema in problemas)
            {
                Debug.LogError($"[Juez] {problema}");
            }

            Debug.LogError($"[Juez] La verificación falló: {problemas.Count} problema(s). " +
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
                problemas.Add("Datos: no es inmune fuera de su ventana. «Inmune a ataques directos» (Anexo A): se le vencería sin mirar.");
            }

            if (datos.duracionVulnerable <= 0f)
            {
                problemas.Add("Datos: su ventana no dura nada. Mirar una memoria no serviría de nada.");
            }

            if (datos.danioContacto > 0)
            {
                problemas.Add($"Datos: tocarlo quita {datos.danioContacto}. Hay que pasar a su lado para llegar a las marcas.");
            }

            if (datos.fases == null || datos.fases.Count == 0)
            {
                problemas.Add("Datos: no tiene fases.");
                return problemas;
            }

            foreach (var fase in datos.fases)
            {
                problemas.AddRange(ProblemasDeLaFase(fase));
            }

            return problemas;
        }

        /// <summary>Una fase: con sus ataques, todos memorias, las cuatro, y con avisos que dan tiempo.</summary>
        public static List<string> ProblemasDeLaFase(FaseJefe fase)
        {
            var problemas = new List<string>();
            var nombre = fase != null ? fase.nombre : "(vacía)";
            if (fase == null || fase.patrones == null || fase.patrones.Count == 0)
            {
                problemas.Add($"Fase «{nombre}»: no tiene ataques. No proyecta ninguna memoria y no se le puede vencer.");
                return problemas;
            }

            if (fase.patrones.Any(p => p == null))
            {
                problemas.Add($"Fase «{nombre}»: tiene un ataque vacío.");
            }

            var noMemorias = fase.patrones.Where(p => p != null && !(p is PatronMemoria)).ToList();
            foreach (var patron in noMemorias)
            {
                problemas.Add($"Fase «{nombre}»: «{patron.name}» no es una memoria: mientras dura, no hay marca que mirar.");
            }

            var memorias = fase.patrones.OfType<PatronMemoria>().Select(p => p.memoria).Distinct().OrderBy(m => m).ToList();
            if (!memorias.SequenceEqual(new[] { 0, 1, 2, 3 }))
            {
                problemas.Add($"Fase «{nombre}»: sus ataques son las memorias {string.Join(", ", memorias)}, no las cuatro.");
            }

            foreach (var patron in fase.patrones.Where(p => p != null && p.aviso < AvisoMinimo))
            {
                problemas.Add($"Fase «{nombre}»: «{patron.name}» avisa {Numero(patron.aviso)} s, menos de {Numero(AvisoMinimo)}. " +
                              "No da tiempo a llegar a su marca.");
            }

            return problemas;
        }

        // --- 2. Prefab ---

        public static List<string> ProblemasDelPrefab(GameObject prefab, JefeData datos)
        {
            var problemas = new List<string>();
            if (prefab == null)
            {
                problemas.Add($"Falta el prefab del jefe ({RutaPrefab}).");
                return problemas;
            }

            var juez = prefab.GetComponent<JuezSilente>();
            if (juez == null)
            {
                problemas.Add($"Prefab {prefab.name}: le falta su controlador, JuezSilente.");
            }
            else if (datos != null && juez.Datos != datos)
            {
                problemas.Add($"Prefab {prefab.name}: no usa los datos de {NombreDatos}.asset.");
            }

            var cuerpo = prefab.GetComponent<Collider2D>();
            if (cuerpo == null || !cuerpo.enabled || prefab.layer != LayerMask.NameToLayer(Capas.Enemy))
            {
                problemas.Add($"Prefab {prefab.name}: no tiene un cuerpo en la capa {Capas.Enemy}. La espada no le daría ni arrodillado.");
            }

            problemas.AddRange(ProblemasDeDanioEnReposo(prefab));
            return problemas;
        }
    }
}
