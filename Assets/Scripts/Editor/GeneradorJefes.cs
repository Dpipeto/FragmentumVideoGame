using System;
using System.Collections.Generic;
using Fragmentum.Bosses;
using Fragmentum.Bosses.Patrones;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using UnityEditor;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Fase 7: crea los patrones de ataque, completa los datos de combate de los cinco jefes
    /// según el Anexo A y genera un prefab por jefe en <c>Assets/Prefabs/Jefes</c>.
    ///
    /// Los <c>JefeData</c> los crea la progresión (Fase 5) con su identidad; aquí solo se
    /// completan los que aún no tienen fases, para no pisar lo que se haya ajustado a mano.
    /// «Restablecer» los vuelve a poner con los valores del diseño.
    /// </summary>
    public static class GeneradorJefes
    {
        public const string CarpetaDatos = "Assets/Data/Jefes";
        public const string CarpetaPatrones = "Assets/Data/Jefes/Patrones";
        public const string CarpetaPrefabs = "Assets/Prefabs/Jefes";

        /// <summary>Cada jefe: nombre de su asset de datos y su controlador.</summary>
        public static readonly (string asset, Type controlador)[] Jefes =
        {
            ("CentinelaArdiente", typeof(CentinelaArdiente)),
            ("DoncellaLamento", typeof(DoncellaLamento)),
            ("EspectroDelPavor", typeof(EspectroDelPavor)),
            ("JuezSilente", typeof(JuezSilente)),
            ("VerdaderoSoldado", typeof(VerdaderoSoldado)),
        };

        [MenuItem("Fragmentum/Jefes/Generar datos y prefabs de jefes", priority = 140)]
        public static void MenuConstruir() => Construir();

        [MenuItem("Fragmentum/Jefes/Restablecer datos de jefes al Anexo A", priority = 141)]
        public static void MenuRestablecer()
        {
            if (EditorUtility.DisplayDialog("Fragmentum", "Se sobrescribirán las fases, la vida y los patrones de los cinco jefes con los valores del diseño. ¿Continuar?", "Restablecer", "Cancelar"))
            {
                GenerarDatos(true);
                CrearPrefabs();
            }
        }

        /// <summary>Datos y prefabs. Devuelve si todos los prefabs se guardaron.</summary>
        public static bool Construir()
        {
            AsegurarConfiguracion();
            GenerarDatos(false);
            return CrearPrefabs();
        }

        // --- Datos ---

        public static void GenerarDatos(bool sobrescribir)
        {
            AsegurarCarpeta(CarpetaPatrones);

            // Centinela Ardiente. Tras la primera prueba se sentía fácil: ahora encadena golpes,
            // salta sobre el jugador y tiene una tercera fase de ira desatada (CHANGELOG). Los
            // avisos nunca bajan de 0,3 s: más rápida, pero siempre legible.
            var espadazo = Patron<PatronEspadazo>("Espadazo", sobrescribir, p =>
            {
                p.nombre = "Espadazo";
                p.descripcion = "Se acerca, avisa y golpea delante dos veces seguidas; tras el segundo la espada queda clavada 1,2 s (ventana vulnerable).";
                p.aviso = 0.55f;
                p.danio = 1;
                p.golpes = 2;
                p.avisoEncadenado = 0.35f;
                p.color = Hex("FF6B35");
                p.dibujarZona = false; // el golpe lo enseña E2
            });

            var espadazoFuria = Patron<PatronEspadazo>("EspadazoFuria", sobrescribir, p =>
            {
                p.nombre = "Espadazo de furia";
                p.descripcion = "Combo de tres espadazos, echándose encima del jugador entre uno y otro. La ventana dura menos (0,9 s).";
                p.aviso = 0.45f;
                p.danio = 1;
                p.golpes = 3;
                p.avisoEncadenado = 0.3f;
                p.impulsoEncadenado = 1.8f;
                p.duracionVentana = 0.9f;
                p.color = Hex("FF6B35");
                p.dibujarZona = false; // el golpe lo enseña E2
            });

            var onda = Patron<PatronOndaSuelo>("OndaDeSuelo", sobrescribir, p =>
            {
                p.nombre = "Onda de suelo";
                p.descripcion = "Golpe al suelo: una onda baja recorre el piso hacia los dos lados. Se esquiva saltando.";
                p.aviso = 0.6f;
                p.danio = 1;
                p.color = Hex("CC4400");
            });

            var ondaDoble = Patron<PatronOndaSuelo>("OndaDoble", sobrescribir, p =>
            {
                p.nombre = "Onda doble";
                p.descripcion = "Dos golpes al suelo seguidos: hay que saltar una onda detrás de otra.";
                p.aviso = 0.5f;
                p.danio = 1;
                p.repeticiones = 2;
                p.intervalo = 0.55f;
                p.velocidadOnda = 10f;
                p.recuperacion = 0.5f;
                p.color = Hex("CC4400");
            });

            var erupcion = Patron<PatronPlataformasErupcion>("PlataformasEnErupcion", sobrescribir, p =>
            {
                p.nombre = "Plataformas en erupción";
                p.descripcion = "Columnas de lava bajo el jugador y a su alrededor, anunciadas antes. Obligan a moverse.";
                p.aviso = 0.75f;
                p.danio = 1;
                p.erupciones = 4;
                p.intervalo = 0.4f;
                p.color = Hex("FF3D00");
            });

            var erupcionRafaga = Patron<PatronPlataformasErupcion>("ErupcionEnRafaga", sobrescribir, p =>
            {
                p.nombre = "Erupción en ráfaga";
                p.descripcion = "Seis columnas de lava muy seguidas por toda la arena: no hay sitio en el que quedarse quieto.";
                p.aviso = 0.6f;
                p.danio = 1;
                p.erupciones = 6;
                p.intervalo = 0.28f;
                p.dispersion = 4.5f;
                p.duracion = 0.8f;
                p.recuperacion = 0.5f;
                p.color = Hex("FF3D00");
            });

            var salto = Patron<PatronSaltoAplastante>("SaltoAplastante", sobrescribir, p =>
            {
                p.nombre = "Salto aplastante";
                p.descripcion = "Salta hacia el jugador y quema al aterrizar (2 de daño), con una onda a cada lado.";
                p.aviso = 0.5f;
                p.danio = 2;
                p.tiempoVuelo = 0.8f;
                p.danioOnda = 1;
                p.color = Hex("E85D04");
                p.dibujarZona = false; // la caída la enseña J4-J5
            });

            VestirOndas(ConstructorEfectos.PrefabOnda, salto, onda, ondaDoble);

            Completar("CentinelaArdiente", sobrescribir, j =>
            {
                j.vidaMaxima = 30;
                j.velocidad = 2.4f;
                j.danioContacto = 1;
                j.duracionVulnerable = 1.2f;
                j.multiplicadorVulnerable = 2f;
                j.inmuneFueraDeVulnerable = false;
                // Con arte: sin tinte, y la derrota dura lo que la muerte hasta D4 (ConstructorCentinela).
                j.tinte = Color.white;
                j.duracionDerrota = ConstructorCentinela.DuracionDerrota;
                j.tamano = new Vector2(2.2f, 3.4f);
                j.fases = new List<FaseJefe>
                {
                    Fase("Espadazo y onda de suelo", 1f, 0.7f, 1f, espadazo, onda, salto, espadazo, onda),
                    Fase("Furia: plataformas en erupción (50 %)", 0.5f, 0.5f, 1.3f, ondaDoble, espadazoFuria, erupcion, salto, espadazoFuria, erupcion),
                    Fase("Ira desatada (25 %)", 0.25f, 0.3f, 1.6f, ondaDoble, erupcionRafaga, salto, espadazoFuria, salto, ondaDoble, espadazoFuria),
                };
            });

            // Esqueletos: vida y fases del Anexo A, sin patrones todavía (Fase 10).
            Completar("DoncellaLamento", sobrescribir, j => Esqueleto(j, 26, "4A7FA5", Fase("Olas e inundación", 1f), Fase("Congelamiento (50 %)", 0.5f)));
            Completar("EspectroDelPavor", sobrescribir, j => Esqueleto(j, 22, "6B4FA0", Fase("Ataques con destello", 1f), Fase("Tres clones (50 %)", 0.5f)));
            Completar("JuezSilente", sobrescribir, j =>
            {
                Esqueleto(j, 28, "8B3333", Fase("Memorias", 1f), Fase("Memoria real y trampa (50 %)", 0.5f));
                j.inmuneFueraDeVulnerable = true;
                j.duracionVulnerable = 3f;
            });
            Completar("VerdaderoSoldado", sobrescribir, j =>
                Esqueleto(j, 40, "5A5A5A", Fase("Combate espejo", 1f), Fase("Secciones de cada mundo (60 %)", 0.6f), Fase("Integración (30 %)", 0.3f)));

            AssetDatabase.SaveAssets();
        }

        private static T Patron<T>(string nombre, bool sobrescribir, Action<T> rellenar) where T : PatronAtaque
        {
            var ruta = $"{CarpetaPatrones}/{nombre}.asset";
            var patron = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (patron == null)
            {
                patron = ScriptableObject.CreateInstance<T>();
                rellenar(patron);
                AssetDatabase.CreateAsset(patron, ruta);
            }
            else if (sobrescribir)
            {
                rellenar(patron);
                EditorUtility.SetDirty(patron);
            }

            return patron;
        }

        /// <summary>
        /// Las ondas de la Centinela llevan su dibujo (<see cref="ConstructorEfectos"/>) en vez del
        /// rectángulo. Solo se rellena el campo vacío: un dibujo puesto a mano se respeta. Sin el
        /// prefab (sin arte), se quedan como estaban.
        /// </summary>
        private static void VestirOndas(GameObject dibujo, PatronSaltoAplastante salto, params PatronOndaSuelo[] ondas)
        {
            if (dibujo == null)
            {
                return;
            }

            foreach (var onda in ondas)
            {
                if (onda.efecto == null)
                {
                    onda.efecto = dibujo;
                    EditorUtility.SetDirty(onda);
                }
            }

            if (salto.efectoOndas == null)
            {
                salto.efectoOndas = dibujo;
                EditorUtility.SetDirty(salto);
            }
        }

        private static void Completar(string asset, bool sobrescribir, Action<JefeData> rellenar)
        {
            var jefe = CargarDatos(asset);
            if (jefe == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {CarpetaDatos}/{asset}.asset: ejecuta Fragmentum → Progresión → Generar datos de niveles.");
                return;
            }

            if (sobrescribir || jefe.fases == null || jefe.fases.Count == 0)
            {
                rellenar(jefe);
                EditorUtility.SetDirty(jefe);
            }
        }

        private static void Esqueleto(JefeData j, int vida, string tinte, params FaseJefe[] fases)
        {
            j.vidaMaxima = vida;
            j.velocidad = 2f;
            j.danioContacto = 1;
            j.duracionVulnerable = 1.2f;
            j.multiplicadorVulnerable = 2f;
            j.inmuneFueraDeVulnerable = false;
            j.tinte = Hex(tinte);
            j.tamano = new Vector2(2f, 3.2f);
            j.fases = new List<FaseJefe>(fases);
        }

        private static FaseJefe Fase(string nombre, float umbral) => Fase(nombre, umbral, 1f, 1f);

        private static FaseJefe Fase(string nombre, float umbral, float pausa, float multiplicadorVelocidad, params PatronAtaque[] patrones) =>
            new FaseJefe
            {
                nombre = nombre,
                umbralVida = umbral,
                pausaEntrePatrones = pausa,
                multiplicadorVelocidad = multiplicadorVelocidad,
                patrones = new List<PatronAtaque>(patrones),
            };

        public static JefeData CargarDatos(string asset) => AssetDatabase.LoadAssetAtPath<JefeData>($"{CarpetaDatos}/{asset}.asset");

        // --- Prefabs ---

        public static bool CrearPrefabs()
        {
            AsegurarCarpeta(CarpetaPrefabs);
            var ok = true;
            foreach (var (asset, controlador) in Jefes)
            {
                var datos = CargarDatos(asset);
                if (datos == null)
                {
                    ok = false;
                    continue;
                }

                ok &= CrearPrefab(asset, controlador, datos) != null;
            }

            if (ok)
            {
                Debug.Log($"[Fragmentum] Prefabs de jefes creados en {CarpetaPrefabs} ({Jefes.Length}).");
            }

            return ok;
        }

        /// <summary>El prefab del jefe de esos datos, o null si todavía no existe.</summary>
        public static GameObject CargarPrefab(JefeData datos) =>
            datos != null ? AssetDatabase.LoadAssetAtPath<GameObject>($"{CarpetaPrefabs}/{datos.name}.prefab") : null;

        private static GameObject CrearPrefab(string nombre, Type tipoControlador, JefeData datos)
        {
            var raiz = new GameObject(nombre) { layer = Capa(Capas.Enemy), tag = GameTags.Boss };
            try
            {
                var tamano = datos.tamano;

                var cuerpoRigido = raiz.AddComponent<Rigidbody2D>();
                cuerpoRigido.gravityScale = 3f;
                cuerpoRigido.mass = 20f;
                cuerpoRigido.freezeRotation = true;
                cuerpoRigido.interpolation = RigidbodyInterpolation2D.Interpolate;
                cuerpoRigido.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var cuerpo = raiz.AddComponent<CapsuleCollider2D>();
                cuerpo.size = tamano;
                cuerpo.offset = new Vector2(0f, tamano.y * 0.5f);

                raiz.AddComponent<SaludJefe>();
                var controlador = (BossController)raiz.AddComponent(tipoControlador);

                var pivote = new GameObject("Pivote").transform;
                pivote.SetParent(raiz.transform, false);

                var objetoSprite = new GameObject("Sprite") { layer = raiz.layer };
                objetoSprite.transform.SetParent(pivote, false);
                var sprite = objetoSprite.AddComponent<SpriteRenderer>();
                sprite.sortingOrder = 5;
                var animator = objetoSprite.AddComponent<Animator>();

                // Los jefes con arte (de momento, la Centinela) llevan sus fotogramas, su Animator
                // y su fragmento; los demás, la cápsula provisional.
                var conArte = nombre == ConstructorCentinela.NombreDatos && ConstructorCentinela.Vestir(objetoSprite, pivote, controlador);
                if (!conArte)
                {
                    objetoSprite.transform.localPosition = new Vector3(0f, tamano.y * 0.5f, 0f);
                    sprite.sprite = SpriteCapsula;
                    sprite.color = datos.tinte;
                    if (sprite.sprite != null)
                    {
                        // La cápsula base mide 1 × 2 u.
                        objetoSprite.transform.localScale = new Vector3(tamano.x, tamano.y * 0.5f, 1f);
                    }

                    var provisional = objetoSprite.AddComponent<SpriteProvisional>();
                    Asignar(provisional, "assetPendiente", p => p.stringValue = $"Sprite y animaciones de {datos.nombreCanonico} (PENDIENTES B5)");
                    Asignar(provisional, "color", p => p.colorValue = datos.tinte);
                    Asignar(provisional, "tamano", p => p.vector2Value = tamano);
                }

                // Contacto: tocar al jefe hace daño mientras combate.
                var objetoContacto = new GameObject("Contacto") { layer = Capa(Capas.EnemyWeapon) };
                objetoContacto.transform.SetParent(raiz.transform, false);
                var zonaContacto = objetoContacto.AddComponent<CapsuleCollider2D>();
                zonaContacto.isTrigger = true;
                zonaContacto.size = tamano * 0.9f;
                zonaContacto.offset = new Vector2(0f, tamano.y * 0.5f);
                var contacto = objetoContacto.AddComponent<Hitbox>();
                Asignar(contacto, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
                Asignar(contacto, "golpearUnaVezPorActivacion", p => p.boolValue = false);
                objetoContacto.SetActive(false);

                Asignar(controlador, "datos", p => p.objectReferenceValue = datos);
                Asignar(controlador, "pivote", p => p.objectReferenceValue = pivote);
                Asignar(controlador, "sprite", p => p.objectReferenceValue = sprite);
                Asignar(controlador, "animator", p => p.objectReferenceValue = animator);
                Asignar(controlador, "contacto", p => p.objectReferenceValue = contacto);

                var ruta = $"{CarpetaPrefabs}/{nombre}.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta, out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar {ruta}.");
                    return null;
                }

                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }
    }
}
