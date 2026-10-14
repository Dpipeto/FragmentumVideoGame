using System.Collections.Generic;
using Fragmentum.Data;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Lo que vale cada cosa en el puntaje, en un solo sitio: los enemigos, los jefes y el bono
    /// por tiempo de cada nivel. <see cref="Aplicar"/> lo escribe en sus assets cada vez que se
    /// regenera el proyecto (los generadores no sobrescriben los assets que ya existen, así que un
    /// valor de diseño nuevo no llegaría a ellos de otra forma).
    ///
    /// <b>El criterio.</b> Un enemigo común vale 100. El que aguanta más o pega más fuerte, algo
    /// más. Un mini-jefe (el Golem de Ceniza, la Plañidera) vale varias veces un enemigo común:
    /// es la pelea que el jugador puede saltarse corriendo, y el puntaje paga por no saltársela.
    /// Un jefe vale más que todo lo demás de su mundo junto a su mini-jefe, y cada mundo, más que
    /// el anterior. El bono por tiempo da 5 puntos por segundo que sobre: cruzar un nivel en la
    /// mitad de su tiempo con bono vale más o menos lo que su jefe, así que correr y pelear
    /// compiten de verdad.
    /// </summary>
    public static class TablaPuntaje
    {
        public const int EnemigoComun = 100;

        /// <summary>Puntos por segundo que sobre del tiempo con bono.</summary>
        public const int PuntosPorSegundo = 5;

        /// <summary>Los enemigos, por el nombre de su asset en <c>Assets/Data/Enemigos</c>.</summary>
        public static readonly (string asset, int puntos)[] Enemigos =
        {
            ("Generico", EnemigoComun),
            ("SoldadoLava", EnemigoComun),
            ("EcoIra", EnemigoComun),
            ("Lamento", EnemigoComun),
            ("Espectro", 150),
            ("CentinelaCiego", 150),
            ("Escultura", 250),
            ("GolemCeniza", 500),
            ("Planidera", 600),
        };

        /// <summary>Los que son mini-jefes: valen varias veces un enemigo común.</summary>
        public static readonly string[] MiniJefes = { "GolemCeniza", "Planidera" };

        /// <summary>Los jefes, por el nombre de su asset en <c>Assets/Data/Jefes</c>.</summary>
        public static readonly (string asset, int puntos)[] Jefes =
        {
            ("CentinelaArdiente", 1500),
            ("DoncellaLamento", 2000),
            ("EspectroDelPavor", 2500),
            ("JuezSilente", 3000),
            ("VerdaderoSoldado", 5000),
        };

        /// <summary>
        /// Segundos de juego con bono de cada nivel, por su id. Los de los niveles montados salen de lo
        /// que miden (<see cref="DisenoNivel1"/>, <see cref="DisenoNivel2"/> y <see cref="DisenoNivel3"/>):
        /// el doble, más o menos, de lo que tarda quien los conoce. El Nivel 3, que se cruza caminando
        /// para no hacer ruido, lleva 15 minutos (Nivel3_Diseno.md, decisión 9). Los demás llevan 12
        /// hasta que existan.
        /// </summary>
        public static readonly (int nivel, float tiempoConBono)[] Niveles =
        {
            (1, 600f),
            (2, 780f),
            (3, 900f),
            (4, 720f),
            (5, 720f),
            (6, 720f),
        };

        [MenuItem("Fragmentum/Progresión/Aplicar la tabla de puntaje", priority = 125)]
        public static void MenuAplicar()
        {
            if (Aplicar())
            {
                Debug.Log("[Fragmentum] Tabla de puntaje aplicada a los enemigos, los jefes y los niveles.");
            }
        }

        /// <summary>Escribe la tabla en los assets que no la tengan ya. Devuelve false si falta alguno.</summary>
        public static bool Aplicar()
        {
            var ok = true;
            foreach (var (asset, puntos) in Enemigos)
            {
                var datos = AssetDatabase.LoadAssetAtPath<EnemyData>($"{GeneradorEnemigos.CarpetaDatos}/{asset}.asset");
                if (datos == null)
                {
                    Debug.LogError($"[Fragmentum] Tabla de puntaje: no existe el enemigo {asset}.");
                    ok = false;
                }
                else if (datos.puntos != puntos)
                {
                    datos.puntos = puntos;
                    EditorUtility.SetDirty(datos);
                }
            }

            foreach (var (asset, puntos) in Jefes)
            {
                var datos = AssetDatabase.LoadAssetAtPath<JefeData>($"{GeneradorJefes.CarpetaDatos}/{asset}.asset");
                if (datos == null)
                {
                    Debug.LogError($"[Fragmentum] Tabla de puntaje: no existe el jefe {asset}.");
                    ok = false;
                }
                else if (datos.puntos != puntos)
                {
                    datos.puntos = puntos;
                    EditorUtility.SetDirty(datos);
                }
            }

            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            var niveles = bd != null ? bd.niveles : new List<LevelData>();
            foreach (var (id, tiempoConBono) in Niveles)
            {
                var nivel = niveles.Find(n => n != null && n.id == id);
                if (nivel == null)
                {
                    Debug.LogError($"[Fragmentum] Tabla de puntaje: no existe el nivel {id} en la LevelDatabase.");
                    ok = false;
                }
                else if (!Mathf.Approximately(nivel.tiempoConBono, tiempoConBono) || nivel.puntosPorSegundo != PuntosPorSegundo)
                {
                    nivel.tiempoConBono = tiempoConBono;
                    nivel.puntosPorSegundo = PuntosPorSegundo;
                    EditorUtility.SetDirty(nivel);
                }
            }

            AssetDatabase.SaveAssets();
            return ok;
        }

        /// <summary>Lo que vale un enemigo según la tabla, o −1 si no está.</summary>
        public static int PuntosDeEnemigo(string asset)
        {
            foreach (var (nombre, puntos) in Enemigos)
            {
                if (nombre == asset)
                {
                    return puntos;
                }
            }

            return -1;
        }
    }
}
