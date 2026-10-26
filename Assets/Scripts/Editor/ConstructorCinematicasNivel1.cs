using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Data;
using Fragmentum.Narrative;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Los planos del jefe del Nivel 1 (<see cref="Carpeta"/>, 240 × 135, 16:9 exacto), puestos en
    /// sus cinemáticas, que se ven a escala entera (<see cref="CutsceneData.escalaEntera"/>):
    /// - <b>Presentación</b> (<c>Cinematica_Jefe_Ira</c>, al entrar en la arena):
    ///   <c>CinematicaJefe1</c> (Kael diminuto en la nave, la Centinela esperando) y
    ///   <c>CinematicaJefe2</c> (el ojo que se abre), opacas. Al terminar, la Centinela ruge y
    ///   empieza el combate. Sus textos no se tocan: son del guion (PENDIENTES N17).
    /// - <b>Derrota</b> (<c>Cinematica_Derrota_Ira</c>, cuando la Centinela termina de caer):
    ///   <c>CinematicaFinalLvl1</c> (la sala vacía, la espada clavada, el fragmento frío) y, cuando
    ///   llegue, <c>CinematicaFinalLvl2</c> (los huecos de los fragmentos). Al terminar se vuelve
    ///   al juego, con el fragmento en la ceniza para recogerlo.
    ///
    /// Solo pone lo que falta: una imagen ya asignada a mano se respeta. Si falta un PNG, su
    /// diapositiva no se crea y se avisa; al dejarlo en la carpeta y regenerar, aparece.
    /// </summary>
    public static class ConstructorCinematicasNivel1
    {
        public const string Carpeta = TablaArte.CarpetaCinematicasNivel1;

        public const string Jefe1 = "CinematicaJefe1";
        public const string Jefe2 = "CinematicaJefe2";
        public const string Final1 = "CinematicaFinalLvl1";
        public const string Final2 = "CinematicaFinalLvl2";

        public const string RutaPresentacion = GeneradorNarrativa.Carpeta + "/Cinematica_Jefe_Ira.asset";
        public const string RutaDerrota = GeneradorNarrativa.Carpeta + "/Cinematica_Derrota_Ira.asset";

        /// <summary>Lo que dura cada plano del final.</summary>
        public const float DuracionPlanoFinal = 4.5f;

        public static string RutaImagen(string nombre) => $"{Carpeta}/{nombre}.png";

        public static Sprite Imagen(string nombre) => AssetDatabase.LoadAssetAtPath<Sprite>(RutaImagen(nombre));

        /// <summary>Pone los planos en las dos cinemáticas. Devuelve false solo si algo falló.</summary>
        public static bool Construir()
        {
            var faltan = new[] { Jefe1, Jefe2, Final1, Final2 }.Where(n => !File.Exists(RutaImagen(n))).ToList();
            if (faltan.Count > 0)
            {
                Debug.LogWarning($"[Fragmentum] Faltan planos del jefe del Nivel 1: {string.Join(", ", faltan)}. " +
                                 "Sus diapositivas se crearán al dejarlos en " + Carpeta + " y regenerar.");
            }

            var presentacion = AssetDatabase.LoadAssetAtPath<CutsceneData>(RutaPresentacion);
            if (presentacion == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {RutaPresentacion} (la genera GeneradorNarrativa): la presentación queda sin planos.");
            }
            else
            {
                Presentacion(presentacion);
            }

            var derrota = Derrota();
            var nivel = NivelDe(presentacion);
            if (nivel != null && nivel.cinematicaJefeDerrotado == null && derrota != null)
            {
                nivel.cinematicaJefeDerrotado = derrota;
                EditorUtility.SetDirty(nivel);
            }

            AssetDatabase.SaveAssets();
            return true;
        }

        // La presentación ya existe (con el guion): solo sus planos, opaca y a escala entera.
        private static void Presentacion(CutsceneData cinematica)
        {
            var cambia = !cinematica.escalaEntera || cinematica.sobreElJuego;
            cinematica.escalaEntera = true;
            cinematica.sobreElJuego = false;

            var planos = new[] { Imagen(Jefe1), Imagen(Jefe2) };
            for (var i = 0; i < planos.Length; i++)
            {
                if (planos[i] == null)
                {
                    continue;
                }

                if (i >= cinematica.diapositivas.Count)
                {
                    cinematica.diapositivas.Add(new Diapositiva { duracion = 4f });
                }

                if (cinematica.diapositivas[i].imagen == null)
                {
                    cinematica.diapositivas[i].imagen = planos[i];
                    cambia = true;
                }
            }

            if (cambia)
            {
                EditorUtility.SetDirty(cinematica);
            }
        }

        // La de la derrota: la crea si falta y le añade los planos que tenga.
        private static CutsceneData Derrota()
        {
            var cinematica = AssetDatabase.LoadAssetAtPath<CutsceneData>(RutaDerrota);
            var nueva = cinematica == null;
            if (nueva)
            {
                cinematica = ScriptableObject.CreateInstance<CutsceneData>();
                cinematica.id = "Derrota_Ira";
                cinematica.saltable = true;
                cinematica.soloPrimeraVez = false;
                cinematica.sobreElJuego = false;
                cinematica.escalaEntera = true;
                cinematica.bloqueoSalto = 1f;
                cinematica.finAviso = 4f;
                cinematica.diapositivas = new List<Diapositiva>();
            }

            var cambia = false;
            var planos = new[] { Imagen(Final1), Imagen(Final2) }.Where(p => p != null).ToList();
            foreach (var plano in planos)
            {
                if (cinematica.diapositivas.Any(d => d != null && d.imagen == plano))
                {
                    continue;
                }

                var hueco = cinematica.diapositivas.FirstOrDefault(d => d != null && d.imagen == null);
                if (hueco != null)
                {
                    hueco.imagen = plano;
                }
                else
                {
                    cinematica.diapositivas.Add(new Diapositiva { imagen = plano, duracion = DuracionPlanoFinal });
                }

                cambia = true;
            }

            if (nueva)
            {
                if (cinematica.diapositivas.Count == 0)
                {
                    return null;
                }

                AssetDatabase.CreateAsset(cinematica, RutaDerrota);
            }
            else if (cambia)
            {
                EditorUtility.SetDirty(cinematica);
            }

            return cinematica;
        }

        // El nivel cuya presentación de jefe es esta (el Nivel 1).
        private static LevelData NivelDe(CutsceneData presentacion)
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            if (bd == null || presentacion == null)
            {
                return null;
            }

            return bd.niveles.FirstOrDefault(n => n != null && n.cinematicaJefe == presentacion);
        }
    }
}
