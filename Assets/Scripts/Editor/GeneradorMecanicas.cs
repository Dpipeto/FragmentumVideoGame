using System;
using Fragmentum.Core;
using Fragmentum.Levels.Mecanicas;
using UnityEditor;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Fase 9: prefabs de las mecánicas del mundo de la Ira (Anexo A) en
    /// <c>Assets/Prefabs/Mecanicas</c>: plataforma inestable, plataforma de combustión, lava y
    /// torreta de fuego. Son las piezas que se arrastran al pintar el Nivel 1, y las que coloca
    /// el preparador de escenas en el recorrido provisional.
    ///
    /// Todas tienen el pivote en su cara superior (la torreta, en su base): se colocan a la
    /// altura del suelo. Sin arte todavía, son rectángulos de color (PENDIENTES B10).
    /// </summary>
    public static class GeneradorMecanicas
    {
        public const string Carpeta = "Assets/Prefabs/Mecanicas";

        public const string PlataformaInestable = "PlataformaInestable";
        public const string PlataformaCaliente = "PlataformaCaliente";
        public const string Lava = "Lava";
        public const string TorretaFuego = "TorretaFuego";

        [MenuItem("Fragmentum/Niveles/Crear prefabs de mecánicas (Ira)", priority = 132)]
        public static void MenuCrearPrefabs() => CrearPrefabs();

        public static bool CrearPrefabs()
        {
            AsegurarConfiguracion();
            AsegurarCarpeta(Carpeta);

            var ok = Guardar(PlataformaInestable, CrearPlataformaInestable);
            ok &= Guardar(PlataformaCaliente, CrearPlataformaCaliente);
            ok &= Guardar(Lava, CrearLava);
            ok &= Guardar(TorretaFuego, CrearTorreta);
            if (ok)
            {
                Debug.Log($"[Fragmentum] Prefabs de mecánicas creados en {Carpeta}.");
            }

            return ok;
        }

        public static GameObject Cargar(string nombre) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Carpeta}/{nombre}.prefab");

        // --- Piezas ---

        private static GameObject CrearPlataformaInestable()
        {
            var raiz = Raiz(PlataformaInestable, Capa(Capas.Suelo), GameTags.Suelo);
            var cuerpo = raiz.AddComponent<BoxCollider2D>();
            cuerpo.size = new Vector2(2f, 0.5f);
            cuerpo.offset = new Vector2(0f, -0.25f);

            var visual = Visual(raiz.transform, new Vector2(0f, -0.25f), new Vector2(2f, 0.5f), Hex("8A6A55"));
            var componente = raiz.AddComponent<Levels.Mecanicas.PlataformaInestable>();
            Asignar(componente, "visual", p => p.objectReferenceValue = visual);
            return raiz;
        }

        private static GameObject CrearPlataformaCaliente()
        {
            var raiz = Raiz(PlataformaCaliente, Capa(Capas.Suelo), GameTags.HotPlatform);
            var cuerpo = raiz.AddComponent<BoxCollider2D>();
            cuerpo.size = new Vector2(2.5f, 0.3f);
            cuerpo.offset = new Vector2(0f, -0.15f);

            var visual = Visual(raiz.transform, new Vector2(0f, -0.15f), new Vector2(2.5f, 0.3f), Hex("5C4033"));
            var componente = raiz.AddComponent<Levels.Mecanicas.PlataformaCaliente>();
            Asignar(componente, "visual", p => p.objectReferenceValue = visual);
            return raiz;
        }

        private static GameObject CrearLava()
        {
            var raiz = Raiz(Lava, 0, GameTags.Lava);
            var zona = raiz.AddComponent<BoxCollider2D>();
            zona.isTrigger = true;
            zona.size = new Vector2(3f, 1f);
            zona.offset = new Vector2(0f, -0.5f);

            var visual = Visual(raiz.transform, new Vector2(0f, -0.5f), new Vector2(3f, 1f), Hex("CC4400"));
            var componente = raiz.AddComponent<Levels.Mecanicas.Lava>();
            Asignar(componente, "visual", p => p.objectReferenceValue = visual);
            return raiz;
        }

        private static GameObject CrearTorreta()
        {
            // Cuerpo macizo sobre el suelo y la boca por encima: dispara a media altura del
            // jugador, que la esquiva saltando.
            var raiz = Raiz(TorretaFuego, Capa(Capas.Suelo), GameTags.Suelo);
            var cuerpo = raiz.AddComponent<BoxCollider2D>();
            cuerpo.size = new Vector2(0.9f, 0.9f);
            cuerpo.offset = new Vector2(0f, 0.45f);

            var visual = Visual(raiz.transform, new Vector2(0f, 0.45f), new Vector2(0.9f, 0.9f), Hex("3A1A10"));
            var boca = new GameObject("Boca").transform;
            boca.SetParent(raiz.transform, false);
            boca.localPosition = new Vector3(0f, 1.25f, 0f);
            Visual(raiz.transform, new Vector2(0f, 1.1f), new Vector2(0.4f, 0.4f), Hex("FF6B35"));

            var componente = raiz.AddComponent<Levels.Mecanicas.TorretaFuego>();
            Asignar(componente, "visual", p => p.objectReferenceValue = visual);
            Asignar(componente, "boca", p => p.objectReferenceValue = boca);
            return raiz;
        }

        // --- Utilidades ---

        private static GameObject Raiz(string nombre, int capa, string tag)
        {
            var raiz = new GameObject(nombre) { layer = capa };
            if (!string.IsNullOrEmpty(tag))
            {
                raiz.tag = tag;
            }

            return raiz;
        }

        private static SpriteRenderer Visual(Transform padre, Vector2 posicion, Vector2 tamano, Color color)
        {
            var objeto = new GameObject("Visual");
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = posicion;
            objeto.transform.localScale = new Vector3(tamano.x, tamano.y, 1f);
            var renderer2d = objeto.AddComponent<SpriteRenderer>();
            renderer2d.sprite = SpriteCuadrado;
            renderer2d.color = color;
            renderer2d.sortingOrder = 1;
            return renderer2d;
        }

        private static bool Guardar(string nombre, Func<GameObject> crear)
        {
            var raiz = crear();
            try
            {
                PrefabUtility.SaveAsPrefabAsset(raiz, $"{Carpeta}/{nombre}.prefab", out var guardado);
                if (!guardado)
                {
                    Debug.LogError($"[Fragmentum] No se pudo guardar el prefab {nombre}.");
                }

                return guardado;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }
    }
}
