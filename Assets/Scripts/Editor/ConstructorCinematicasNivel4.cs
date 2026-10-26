using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Data;
using Fragmentum.Narrative;
using UnityEditor;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Las cinemáticas del Mundo 4 (Culpa, fase F) con sus planos (<see cref="Carpeta"/>, 480 × 268, en la
    /// rejilla de los de la Doncella y el Pavor; <c>Prompts_Mundo4.md</c>, apartados G y H):
    /// - <b>Presentación del Juez Silente</b> (<c>Cinematica_Jefe_Culpa</c>), al cruzar la verja: la plaza del
    ///   pueblo con sus cuatro losas, el Juez quieto al fondo y Kael entrando; y el Juez de cerca, con la
    ///   balanza delante de la cara. Su línea del guion, repartida entre los dos planos, y su rótulo con el
    ///   nombre del mundo: la estructura y la duración de la del Pavor (9,8 s). De cerca, el cielo de humo es
    ///   claro: lo que dice y, después, el rótulo van abajo, sobre la capa oscura, uno detrás de otro.
    /// - <b>Derrota</b> (<c>Cinematica_Derrota_Culpa</c>): el Juez de rodillas con las cuatro losas encendidas, y
    ///   la plaza vacía con Kael levantando el fragmento. Sin texto, como las otras. Solo pone lo que falta.
    /// - <b>Las cuatro memorias</b> (<c>Cinematica_Memoria_…</c>): la imagen de cada una (la misma que el Juez
    ///   proyecta en la plaza) con la línea de Kael encima, en dos líneas, abajo. 5 s: negro 0,2 · la imagen
    ///   0,5 + 3,8 + 0,5. Se reescriben cada vez que se regenera.
    ///
    /// Sin sonido. Si faltan los planos de una de ellas, esa no se toca y se avisa.
    /// </summary>
    public static class ConstructorCinematicasNivel4
    {
        public const string Carpeta = TablaArte.CarpetaSprites + "/Cinematicas/Nivel4";

        public const string Presentacion1 = "PresentacionJuez1";
        public const string Presentacion2 = "PresentacionJuez2";
        public const string Derrota1 = "DerrotaJuez1";
        public const string Derrota2 = "DerrotaJuez2";

        public static readonly string[] Planos = { Presentacion1, Presentacion2, Derrota1, Derrota2 };

        public const string RutaPresentacion = GeneradorNarrativa.Carpeta + "/Cinematica_Jefe_Culpa.asset";
        public const string RutaDerrota = GeneradorNarrativa.Carpeta + "/Cinematica_Derrota_Culpa.asset";

        /// <summary>La rejilla de los planos: la de los de la Doncella.</summary>
        public static readonly Vector2Int Rejilla = ConstructorCinematicasNivel2.Rejilla;

        /// <summary>La letra: la de la Doncella (10 px del plano, 40 en 1920 × 1080).</summary>
        public const int TamanoFuente = ConstructorCinematicasNivel2.TamanoFuente;

        /// <summary>Lo que dura cada plano de la derrota: lo mismo que los de los otros mundos.</summary>
        public const float DuracionPlanoFinal = ConstructorCinematicasNivel2.DuracionPlanoFinal;

        /// <summary>En la plaza: abajo, a la derecha de Kael, sobre la tierra oscura (filas 250 a 268).</summary>
        public static readonly Vector2Int PuntoPlaza = new Vector2Int(300, 250);

        /// <summary>De cerca: abajo, sobre la capa oscura (filas 220 a 268); arriba está el cielo de humo, claro.</summary>
        public static readonly Vector2Int PuntoCerca = new Vector2Int(240, 244);

        /// <summary>El rótulo (el doble de alto) y el nombre del mundo, cuando se ha ido lo que dice: en la misma franja.</summary>
        public static readonly Vector2Int PuntoRotulo = new Vector2Int(240, 230);

        public static readonly Vector2Int PuntoSubtitulo = new Vector2Int(240, 252);

        /// <summary>
        /// La línea de Kael en las memorias: abajo en el centro, en dos líneas, con el borde de abajo del texto en esta
        /// fila (se ancla por abajo: dos líneas miden unas 26 filas y, ancladas por arriba, la segunda se salía del
        /// plano). En las cuatro imágenes esa franja (filas 238 a 264) es oscura: la tierra de la trinchera, la
        /// ceniza de la plaza, el barro, la caja.
        /// </summary>
        public static readonly Vector2Int PuntoMemoria = new Vector2Int(240, 264);

        // La memoria: negro, y la imagen con su fundido de entrada y de salida.
        public const float NegroMemoria = 0.2f;
        public const float EntradaMemoria = 0.5f;
        public const float ImagenMemoria = 3.8f;
        public const float SalidaMemoria = 0.5f;

        /// <summary>La línea de Kael en su memoria: entra al terminar el fundido y se va antes que la imagen.</summary>
        public const float InicioLineaMemoria = 0.5f;
        public const float DuracionLineaMemoria = 3.6f;

        public static IEnumerable<TablaArte.Regla> Reglas => new[]
        {
            new TablaArte.Regla(Carpeta + "/", UI.CamaraCinematica.PixelesPorUnidad, null,
                "Planos del Mundo 4 (el Juez Silente, sus cuatro memorias y la salida): 480×268, como los de la Doncella, a 1 píxel " +
                "por unidad como la cámara de cinemáticas, que los dibuja en su propia rejilla.", Rejilla),
        };

        public static string RutaImagen(string nombre) => $"{Carpeta}/{nombre}.png";

        public static Sprite Imagen(string nombre) => AssetDatabase.LoadAssetAtPath<Sprite>(RutaImagen(nombre));

        /// <summary>¿Están los cuatro planos del Juez en el proyecto?</summary>
        public static bool HayPlanos => Planos.All(p => File.Exists(RutaImagen(p)));

        /// <summary>Las imágenes de las cuatro memorias, en el orden de <see cref="DisenoNivel4.Memorias"/>, o null si falta alguna.</summary>
        public static Sprite[] ImagenesDeLasMemorias
        {
            get
            {
                var imagenes = DisenoNivel4.Memorias.Select(m => Imagen(m.Nombre)).ToArray();
                return imagenes.All(i => i != null) ? imagenes : null;
            }
        }

        /// <summary>
        /// Escribe la presentación: cuatro beats (negro 0,3 · la plaza 0,7 + 3,5 + 0,3 · negro 0,2 · de cerca
        /// 0,6 + 3,9 + 0,3), 9,8 s. La primera parte de su línea, sobre la plaza, 3,3 s (es corta: «No dicto
        /// sentencia.»); la segunda, de cerca, 16 letras por segundo; y en cuanto se va, en el mismo sitio, el
        /// rótulo, 1,5 s, hasta que empieza a irse el plano.
        /// </summary>
        public static void Presentacion(CutsceneData cinematica, string jefe, string mundo, string linea)
        {
            var (primero, segundo) = ConstructorCinematicasNivel2.Partir(linea);
            var plaza = new Diapositiva
            {
                imagen = Imagen(Presentacion1),
                fundidoEntrada = 0.7f,
                duracion = 3.5f,
                fundidoSalida = 0.3f,
            };
            if (primero.Length > 0)
            {
                plaza.carteles.Add(Dicho(primero, 0.9f, 3.3f, PuntoPlaza));
            }

            var dice = Mathf.Ceil(segundo.Length / 16f * 10f) / 10f;
            var rotulo = Mathf.Round((0.5f + dice) * 10f) / 10f;

            var cerca = new Diapositiva
            {
                imagen = Imagen(Presentacion2),
                fundidoEntrada = 0.6f,
                duracion = 3.9f,
                fundidoSalida = 0.3f,
                carteles =
                {
                    Dicho(segundo, 0.5f, dice, PuntoCerca),
                    new Cartel
                    {
                        texto = jefe.ToUpperInvariant(),
                        inicio = rotulo,
                        duracion = 1.5f,
                        titulo = true,
                        enPunto = true,
                        punto = PuntoRotulo,
                        alineacion = TextAnchor.UpperCenter,
                    },
                    Dicho(mundo, rotulo, 1.5f, PuntoSubtitulo),
                },
            };

            cinematica.id = "Jefe_Culpa";
            cinematica.montaje = MONTAJES_CINEMATICA.BEATS;
            cinematica.escalaEntera = true;
            cinematica.rejilla = Rejilla;
            cinematica.sobreElJuego = false;
            cinematica.saltable = true;
            cinematica.soloPrimeraVez = true;
            cinematica.llevaDialogoDelJefe = true;
            cinematica.bloqueoSalto = 0.5f;
            cinematica.avisoSaltar = string.Empty;
            cinematica.fundidoTexto = 0.4f;
            cinematica.tamanoFuente = TamanoFuente;
            cinematica.musica = MusicaNiveles.DeCinematica(cinematica.id);
            cinematica.diapositivas = new List<Diapositiva>
            {
                new Diapositiva { duracion = 0.3f },
                plaza,
                new Diapositiva { duracion = 0.2f },
                cerca,
            };
        }

        /// <summary>
        /// Escribe la cinemática de una memoria: su imagen, con la línea de Kael encima en dos líneas
        /// (<see cref="EnDosLineas"/>). Se ve cada vez que se recuerda y se puede saltar.
        /// </summary>
        public static void Memoria(CutsceneData cinematica, string id, Sprite imagen, string texto)
        {
            cinematica.id = $"Memoria_{id}";
            cinematica.montaje = MONTAJES_CINEMATICA.BEATS;
            cinematica.escalaEntera = true;
            cinematica.rejilla = Rejilla;
            cinematica.sobreElJuego = false;
            cinematica.saltable = true;
            cinematica.soloPrimeraVez = false;
            cinematica.llevaDialogoDelJefe = false;
            cinematica.bloqueoSalto = 0.5f;
            cinematica.avisoSaltar = string.Empty;
            cinematica.fundidoTexto = 0.4f;
            cinematica.tamanoFuente = TamanoFuente;
            cinematica.musica = null; // las memorias, en silencio (Anexo A)
            cinematica.diapositivas = new List<Diapositiva>
            {
                new Diapositiva { duracion = NegroMemoria },
                new Diapositiva
                {
                    imagen = imagen,
                    fundidoEntrada = EntradaMemoria,
                    duracion = ImagenMemoria,
                    fundidoSalida = SalidaMemoria,
                    carteles = { Dicho(EnDosLineas(texto), InicioLineaMemoria, DuracionLineaMemoria, PuntoMemoria, TextAnchor.LowerCenter) },
                },
            };
        }

        /// <summary>
        /// Parte una línea en dos por el final de frase más cercano a su mitad (o, si no hay, por el espacio más
        /// cercano): en el plano no cabe entera, y el texto no se ajusta solo.
        /// </summary>
        public static string EnDosLineas(string texto)
        {
            texto = (texto ?? string.Empty).Trim();
            var mitad = texto.Length / 2f;
            var cortes = Enumerable.Range(1, Mathf.Max(0, texto.Length - 2))
                .Where(i => texto[i] == ' ' && ".!?…»".IndexOf(texto[i - 1]) >= 0 && !(texto[i - 1] == '»' && texto[i - 2] != '.'))
                .ToList();
            if (cortes.Count == 0)
            {
                cortes = Enumerable.Range(1, Mathf.Max(0, texto.Length - 2)).Where(i => texto[i] == ' ').ToList();
            }

            if (cortes.Count == 0)
            {
                return texto;
            }

            var corte = cortes.OrderBy(i => Mathf.Abs(i - mitad)).First();
            return texto.Substring(0, corte) + "\n" + texto.Substring(corte + 1);
        }

        private static Cartel Dicho(string texto, float inicio, float duracion, Vector2Int punto,
            TextAnchor alineacion = TextAnchor.UpperCenter) => new Cartel
        {
            texto = texto,
            inicio = inicio,
            duracion = duracion,
            enPunto = true,
            punto = punto,
            alineacion = alineacion,
        };

        [MenuItem("Fragmentum/Cinemáticas/Montar las cinemáticas del Mundo 4 (el Juez y las memorias)", priority = 166)]
        public static void MenuConstruir() => Construir();

        /// <summary>
        /// Monta la presentación y la derrota del Juez y las cuatro memorias, cada parte si tiene sus imágenes, y
        /// las pone en el Nivel 4. Devuelve false solo si algo falló.
        /// </summary>
        public static bool Construir()
        {
            var ok = true;
            var todas = Planos.Concat(DisenoNivel4.Memorias.Select(m => m.Nombre)).Where(p => File.Exists(RutaImagen(p)));
            if (todas.Any(p => Imagen(p) == null))
            {
                Debug.LogError($"[Fragmentum] Los planos de {Carpeta} no están importados como sprites.");
                return false;
            }

            var nivel = Nivel();
            if (HayPlanos)
            {
                ok &= Jefe(nivel);
            }
            else
            {
                var faltan = Planos.Where(p => !File.Exists(RutaImagen(p)));
                Debug.LogWarning($"[Fragmentum] Faltan planos del Juez Silente ({string.Join(", ", faltan)}) en {Carpeta}: " +
                                 "su presentación sigue siendo la genérica y no hay cinemática de derrota.");
            }

            Memorias();
            AssetDatabase.SaveAssets();
            return ok;
        }

        // La presentación y la derrota del Juez, enlazadas al Nivel 4.
        private static bool Jefe(LevelData nivel)
        {
            var presentacion = AssetDatabase.LoadAssetAtPath<CutsceneData>(RutaPresentacion);
            if (nivel == null || nivel.jefe == null || presentacion == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta el Nivel 4, su jefe o {RutaPresentacion} (la genera GeneradorNarrativa): " +
                                 "las cinemáticas del Juez Silente no se montan.");
                return true;
            }

            var guion = GuionMundos.De(EMOCIONES.CULPA);
            Presentacion(presentacion, nivel.jefe.nombreCanonico, nivel.nombreMundo, guion != null ? guion.AntesDelJefe : string.Empty);
            EditorUtility.SetDirty(presentacion);

            var derrota = Derrota();
            var cambia = false;
            if (nivel.cinematicaJefe == null)
            {
                nivel.cinematicaJefe = presentacion;
                cambia = true;
            }

            if (nivel.cinematicaJefeDerrotado == null && derrota != null)
            {
                nivel.cinematicaJefeDerrotado = derrota;
                cambia = true;
            }

            if (cambia)
            {
                EditorUtility.SetDirty(nivel);
            }

            Debug.Log($"[Fragmentum] Cinemáticas del Juez Silente: {RutaPresentacion} ({presentacion.DuracionTotal:0.0} s) y {RutaDerrota}.");
            return true;
        }

        // La de la derrota: la crea si falta y le añade los planos que no tenga.
        private static CutsceneData Derrota()
        {
            var cinematica = AssetDatabase.LoadAssetAtPath<CutsceneData>(RutaDerrota);
            var nueva = cinematica == null;
            if (nueva)
            {
                cinematica = ScriptableObject.CreateInstance<CutsceneData>();
                cinematica.id = "Derrota_Culpa";
                cinematica.saltable = true;
                cinematica.soloPrimeraVez = false;
                cinematica.sobreElJuego = false;
                cinematica.escalaEntera = true;
                cinematica.bloqueoSalto = 1f;
                cinematica.finAviso = 4f;
                cinematica.diapositivas = new List<Diapositiva>();
            }

            var cambia = cinematica.rejilla != Rejilla;
            cinematica.rejilla = Rejilla;
            foreach (var plano in new[] { Imagen(Derrota1), Imagen(Derrota2) })
            {
                if (cinematica.diapositivas.Any(d => d != null && d.imagen == plano))
                {
                    continue;
                }

                cinematica.diapositivas.Add(new Diapositiva { imagen = plano, duracion = DuracionPlanoFinal });
                cambia = true;
            }

            // Su música (Musica/Cinematicas), si la tiene.
            var musica = MusicaNiveles.DeCinematica(cinematica.Id);
            if (cinematica.musica != musica)
            {
                cinematica.musica = musica;
                cambia = true;
            }

            if (nueva)
            {
                AssetDatabase.CreateAsset(cinematica, RutaDerrota);
            }
            else if (cambia)
            {
                EditorUtility.SetDirty(cinematica);
            }

            return cinematica;
        }

        // Las cuatro memorias: cada una con su imagen, si está, y la línea de Kael del guion.
        private static void Memorias()
        {
            var guion = GuionMundos.De(EMOCIONES.CULPA);
            if (guion == null)
            {
                return;
            }

            var hechas = 0;
            foreach (var (id, texto) in guion.Memorias)
            {
                var imagen = Imagen(id);
                var cinematica = GeneradorNarrativa.CargarCinematicaMemoria(id);
                if (imagen == null || cinematica == null)
                {
                    Debug.LogWarning($"[Fragmentum] Falta la imagen de la memoria {id} ({RutaImagen(id)}) o su cinemática (la genera " +
                                     "GeneradorNarrativa): esa memoria sigue siendo su línea sobre el juego.");
                    continue;
                }

                Memoria(cinematica, id, imagen, texto);
                EditorUtility.SetDirty(cinematica);
                hechas++;
            }

            Debug.Log($"[Fragmentum] Memorias de la Culpa: {hechas} de {guion.Memorias.Length} con su imagen.");
        }

        /// <summary>El Nivel 4, o null si no está en la base de datos.</summary>
        public static LevelData Nivel()
        {
            var bd = AssetDatabase.LoadAssetAtPath<LevelDatabase>(GeneradorProgresion.RutaBaseDeDatos);
            return bd != null ? bd.niveles.FirstOrDefault(DisenoNivel4.EsDe) : null;
        }
    }
}
