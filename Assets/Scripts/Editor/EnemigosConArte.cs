using System;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Data;
using Fragmentum.Enemies;
using UnityEditor;
using UnityEngine;
using static Fragmentum.EditorTools.UtilidadesEditor;
using Object = UnityEngine.Object;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// El armazón que comparten los enemigos con arte del Mundo 2 (el Lamento, la Escultura y la
    /// Plañidera), el mismo que montan a mano los constructores del Eco de la Ira y del Golem:
    /// cuerpo, vida, <see cref="EnemyController"/>, un pivote que se voltea con el sprite y el
    /// arma, y la zona de contacto. Cada constructor pone encima lo que es suyo.
    /// </summary>
    public static class EnemigosConArte
    {
        /// <summary>Las piezas del prefab mientras se monta.</summary>
        public sealed class Armazon
        {
            public GameObject Raiz;
            public Transform Pivote;
            public SpriteRenderer Sprite;
            public Animator Animator;
            public EnemigoAnimacion Animacion;
            public Hitbox Arma;
            public Hitbox Contacto;
            public EnemyController Controlador;
        }

        /// <summary>Medidas del prefab, en unidades, desde los pies y mirando a la derecha.</summary>
        public struct Medidas
        {
            public Vector2 Cuerpo;
            public Vector2 Contacto;
            public Vector2 CentroArma;
            public Vector2 TamanoArma;
        }

        /// <summary>
        /// Monta el armazón, deja que <paramref name="completar"/> añada lo propio del enemigo y lo
        /// guarda en <paramref name="ruta"/>. Devuelve el prefab, o null si no se pudo guardar.
        /// </summary>
        public static GameObject CrearPrefab(string nombre, string ruta, EnemyData datos, Sprite reposo, RuntimeAnimatorController controlador,
            in Medidas medidas, Action<Armazon> completar = null)
        {
            var destello = AssetDatabase.LoadAssetAtPath<Material>(ConstructorEcoIra.RutaMaterialDestello);
            if (destello == null)
            {
                Debug.LogWarning($"[Fragmentum] Falta {ConstructorEcoIra.RutaMaterialDestello}: {nombre} no destellará al recibir golpes.");
            }

            var raiz = new GameObject(nombre) { layer = Capa(Capas.Enemy), tag = GameTags.Enemy };
            try
            {
                var cuerpoRigido = raiz.AddComponent<Rigidbody2D>();
                cuerpoRigido.gravityScale = 3f;
                cuerpoRigido.freezeRotation = true;
                cuerpoRigido.interpolation = RigidbodyInterpolation2D.Interpolate;
                cuerpoRigido.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var cuerpo = raiz.AddComponent<CapsuleCollider2D>();
                cuerpo.size = medidas.Cuerpo;
                cuerpo.offset = new Vector2(0f, medidas.Cuerpo.y / 2f);

                raiz.AddComponent<Health>();
                var enemigo = raiz.AddComponent<EnemyController>();

                // Lo que se voltea al girar: el sprite y el arma.
                var pivote = new GameObject("Pivote").transform;
                pivote.SetParent(raiz.transform, false);

                var objetoSprite = new GameObject("Sprite") { layer = raiz.layer };
                objetoSprite.transform.SetParent(pivote, false);
                var sprite = objetoSprite.AddComponent<SpriteRenderer>();
                sprite.sprite = reposo;
                sprite.sortingOrder = 5;
                sprite.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ConstructorEcoIra.RutaMaterialSprite);

                var animator = objetoSprite.AddComponent<Animator>();
                animator.runtimeAnimatorController = controlador;
                animator.applyRootMotion = false;
                // Fuera de cámara también: si no, los eventos del golpe y de la muerte no llegan.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                var animacion = objetoSprite.AddComponent<EnemigoAnimacion>();
                Asignar(animacion, "materialDestello", p => p.objectReferenceValue = destello);

                // Arma: solo golpea a la capa Player, y solo entre los eventos de su clip.
                var objetoArma = new GameObject("Arma") { layer = Capa(Capas.EnemyWeapon) };
                objetoArma.transform.SetParent(pivote, false);
                objetoArma.transform.localPosition = medidas.CentroArma;
                var zonaArma = objetoArma.AddComponent<BoxCollider2D>();
                zonaArma.isTrigger = true;
                zonaArma.size = medidas.TamanoArma;
                var arma = objetoArma.AddComponent<Hitbox>();
                Asignar(arma, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
                objetoArma.SetActive(false);

                // Contacto: tocar el cuerpo (danioContacto; con 0 se apaga al arrancar).
                var objetoContacto = new GameObject("Contacto") { layer = Capa(Capas.EnemyWeapon) };
                objetoContacto.transform.SetParent(raiz.transform, false);
                var zonaContacto = objetoContacto.AddComponent<CapsuleCollider2D>();
                zonaContacto.isTrigger = true;
                zonaContacto.size = medidas.Contacto;
                zonaContacto.offset = new Vector2(0f, medidas.Contacto.y / 2f);
                var contacto = objetoContacto.AddComponent<Hitbox>();
                Asignar(contacto, "objetivos", p => p.intValue = Capas.Mascara(Capas.Player));
                Asignar(contacto, "golpearUnaVezPorActivacion", p => p.boolValue = false);

                Asignar(enemigo, "datos", p => p.objectReferenceValue = datos);
                Asignar(enemigo, "animator", p => p.objectReferenceValue = animator);
                Asignar(enemigo, "pivote", p => p.objectReferenceValue = pivote);
                Asignar(enemigo, "sprite", p => p.objectReferenceValue = sprite);
                Asignar(enemigo, "arma", p => p.objectReferenceValue = arma);
                Asignar(enemigo, "contacto", p => p.objectReferenceValue = contacto);
                Asignar(enemigo, "animacion", p => p.objectReferenceValue = animacion);

                completar?.Invoke(new Armazon
                {
                    Raiz = raiz,
                    Pivote = pivote,
                    Sprite = sprite,
                    Animator = animator,
                    Animacion = animacion,
                    Arma = arma,
                    Contacto = contacto,
                    Controlador = enemigo,
                });

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

        /// <summary>Un evento de animación en el instante <paramref name="tiempo"/> del clip.</summary>
        public static AnimationEvent Evento(string funcion, float tiempo) =>
            new AnimationEvent { functionName = funcion, time = tiempo };
    }
}
