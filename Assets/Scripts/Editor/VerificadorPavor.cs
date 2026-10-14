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
    /// Comprueba que al Espectro del Pavor se le puede vencer. Es invisible e intangible salvo
    /// cuando ataca, así que un cambio en sus datos puede dejarlo invencible sin que nada falle a
    /// la vista:
    /// 1. sus datos: cada fase tiene patrones y entre ellos uno que abre la ventana (el jadeo del
    ///    tajo o el de las sombras), cada ataque avisa al menos <see cref="AvisoMinimo"/> s (el
    ///    destello es lo único que lo delata), las sombras tienen su tajo y tocarlo no hace daño
    ///    (estando invisible, sería un golpe que no se ve venir);
    /// 2. su prefab: lleva su controlador con esos datos, tiene un cuerpo que la espada nota, su
    ///    dibujo no depende de la luz (en su arena a oscuras, su destello tiene que verse) y, en
    ///    reposo, nada de lo que hace daño está encendido.
    ///
    /// Falla ruidosamente, como el de la Doncella: un error en la consola por cada problema. Se
    /// ejecuta desde el menú y al final de «Regenerar todo».
    /// </summary>
    public static class VerificadorPavor
    {
        public const string NombreDatos = GeneradorJefes.NombrePavor;

        /// <summary>Lo menos que avisa un ataque: el destello es lo único que lo delata.</summary>
        public const float AvisoMinimo = 0.5f;

        public static string RutaPrefab => $"{GeneradorJefes.CarpetaPrefabs}/{NombreDatos}.prefab";

        [MenuItem("Fragmentum/Jefes/Verificar Espectro del Pavor", priority = 145)]
        public static void MenuVerificar() => Verificar();

        /// <summary>Revisa todo. Devuelve true si está bien; si no, deja un error por problema en la consola.</summary>
        public static bool Verificar()
        {
            var problemas = Problemas();
            if (problemas.Count == 0)
            {
                Debug.Log("[Pavor] Verificación correcta: sus datos, sus fases con su ventana, su destello y el prefab.");
                return true;
            }

            foreach (var problema in problemas)
            {
                Debug.LogError($"[Pavor] {problema}");
            }

            Debug.LogError($"[Pavor] La verificación falló: {problemas.Count} problema(s). " +
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

        /// <summary>Sus datos: sin daño por contacto y cada fase con su ventana y avisos que se leen.</summary>
        public static List<string> ProblemasDeLosDatos(JefeData datos)
        {
            var problemas = new List<string>();
            if (datos == null)
            {
                problemas.Add($"Faltan los datos del jefe ({GeneradorJefes.CarpetaDatos}/{NombreDatos}.asset).");
                return problemas;
            }

            if (datos.danioContacto > 0)
            {
                problemas.Add($"Datos: tocarlo quita {datos.danioContacto}. Es invisible: sería un golpe que no se ve venir.");
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

        /// <summary>Una fase: con patrones, con uno que abre la ventana y con avisos que se leen.</summary>
        public static List<string> ProblemasDeLaFase(FaseJefe fase)
        {
            var problemas = new List<string>();
            var nombre = fase != null ? fase.nombre : "(vacía)";
            if (fase == null || fase.patrones == null || fase.patrones.Count == 0)
            {
                problemas.Add($"Fase «{nombre}»: no tiene patrones. Espera invisible y no se le puede golpear.");
                return problemas;
            }

            if (fase.patrones.Any(p => p == null))
            {
                problemas.Add($"Fase «{nombre}»: tiene un patrón vacío.");
            }

            var conVentana = fase.patrones.OfType<PatronTajo>().Any(t => t.jadeo > 0f) ||
                             fase.patrones.OfType<PatronSombras>().Any(s => s.jadeo > 0f && s.tajo != null);
            if (!conVentana)
            {
                problemas.Add($"Fase «{nombre}»: ningún ataque lo deja jadeando (un tajo o las sombras con jadeo). Sin ventana " +
                              "solo se le golpea mientras avisa, y es casi imposible vencerlo.");
            }

            foreach (var sombras in fase.patrones.OfType<PatronSombras>())
            {
                if (sombras.tajo == null)
                {
                    problemas.Add($"Fase «{nombre}»: las sombras «{sombras.name}» no tienen tajo: no atacan ni jadean.");
                }
            }

            foreach (var patron in fase.patrones)
            {
                if (patron != null && patron.aviso < AvisoMinimo)
                {
                    problemas.Add($"Fase «{nombre}»: «{patron.name}» avisa {Numero(patron.aviso)} s, menos de {Numero(AvisoMinimo)}. " +
                                  "Su destello es lo único que lo delata.");
                }
            }

            return problemas;
        }

        // --- 2. Prefab ---

        /// <summary>
        /// El prefab: con su controlador y sus datos, con un cuerpo en la capa Enemy (la espada de
        /// Kael solo golpea esa capa), con un dibujo que se ve a oscuras y, en reposo, sin nada que
        /// haga daño encendido.
        /// </summary>
        public static List<string> ProblemasDelPrefab(GameObject prefab, JefeData datos)
        {
            var problemas = new List<string>();
            if (prefab == null)
            {
                problemas.Add($"Falta el prefab del jefe ({RutaPrefab}).");
                return problemas;
            }

            var pavor = prefab.GetComponent<EspectroDelPavor>();
            if (pavor == null)
            {
                problemas.Add($"Prefab {prefab.name}: le falta su controlador, EspectroDelPavor.");
            }
            else if (datos != null && pavor.Datos != datos)
            {
                problemas.Add($"Prefab {prefab.name}: no usa los datos de {NombreDatos}.asset.");
            }

            var cuerpo = prefab.GetComponent<Collider2D>();
            if (cuerpo == null || !cuerpo.enabled || prefab.layer != LayerMask.NameToLayer(Capas.Enemy))
            {
                problemas.Add($"Prefab {prefab.name}: no tiene un cuerpo en la capa {Capas.Enemy}. La espada no le daría ni jadeando.");
            }

            var dibujo = prefab.GetComponentInChildren<SpriteRenderer>(true);
            if (dibujo == null || AssetDatabase.GetAssetPath(dibujo.sharedMaterial) != GeneradorJefes.RutaMaterialSinLuz)
            {
                problemas.Add($"Prefab {prefab.name}: su dibujo depende de la luz. En su arena, a oscuras, no se vería su destello.");
            }

            problemas.AddRange(ProblemasDeDanioEnReposo(prefab));
            return problemas;
        }
    }
}
