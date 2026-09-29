using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragmentum.Core;
using Fragmentum.Core.Combate;
using Fragmentum.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Fragmentum.Tests.PlayMode
{
    /// <summary>
    /// Base de las pruebas PlayMode: cada prueba monta su propio escenario (bloques con
    /// capas reales), instancia prefabs reales del juego y todo se destruye al terminar.
    /// </summary>
    public abstract class PruebaPlayModeBase
    {
        protected const string RutaPrefabJugador = "Assets/Prefabs/Player.prefab";

        protected readonly List<Object> Creados = new List<Object>();
        protected readonly List<ESTADOS_JUGADOR> HistorialJugador = new List<ESTADOS_JUGADOR>();

        protected PlayerController Jugador;
        protected InputSimulado Input;

        protected Vector2 PiesJugador => Jugador.Motor.Cuerpo.position;

        [UnityTearDown]
        public IEnumerator Limpiar()
        {
            foreach (var objeto in Creados)
            {
                if (objeto != null)
                {
                    Object.Destroy(objeto);
                }
            }

            Creados.Clear();
            HistorialJugador.Clear();
            Jugador = null;
            yield return null;
        }

        // --- Prefabs ---

        protected static GameObject CargarPrefab(string ruta)
        {
            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
#endif
            Assert.IsNotNull(prefab, $"Falta {ruta}: ejecuta Fragmentum → Regenerar todo.");
            return prefab;
        }

        protected static T CargarAsset<T>(string ruta) where T : Object
        {
            T asset = null;
#if UNITY_EDITOR
            asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(ruta);
#endif
            Assert.IsNotNull(asset, $"Falta {ruta}: ejecuta Fragmentum → Regenerar todo.");
            return asset;
        }

        protected IEnumerator CrearJugador(Vector2 pies)
        {
            var objeto = Object.Instantiate(CargarPrefab(RutaPrefabJugador), new Vector3(pies.x, pies.y, 0f), Quaternion.identity);
            Creados.Add(objeto);

            Jugador = objeto.GetComponent<PlayerController>();
            Input = new InputSimulado();
            Jugador.FijarFuenteInput(Input);
            Jugador.OnStateChanged += (_, nuevo) => HistorialJugador.Add(nuevo);

            yield return null; // Start: la FSM arranca
        }

        protected IEnumerator CrearJugadorEnSuelo(Vector2 pies)
        {
            yield return CrearJugador(pies + new Vector2(0f, 0.05f));
            yield return EsperarHasta(
                () => Jugador.Sensores.EnSuelo && Jugador.EstadoActual == ESTADOS_JUGADOR.IDLE, 1f,
                "que el jugador se apoye en el suelo");
            yield return new WaitForSeconds(0.1f);
            HistorialJugador.Clear();
        }

        // --- Escenario ---

        protected void Suelo(float xMin, float xMax, float yArriba = 0f) =>
            Bloque("Suelo", xMin, xMax, yArriba - 1f, yArriba, Capas.Suelo);

        protected GameObject Bloque(string nombre, float xMin, float xMax, float yMin, float yMax, string capa, bool trigger = false)
        {
            var objeto = new GameObject(nombre);
            if (!string.IsNullOrEmpty(capa))
            {
                objeto.layer = LayerMask.NameToLayer(capa);
            }

            objeto.transform.position = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
            var caja = objeto.AddComponent<BoxCollider2D>();
            caja.size = new Vector2(xMax - xMin, yMax - yMin);
            caja.isTrigger = trigger;
            Creados.Add(objeto);
            return objeto;
        }

        protected Transform Punto(string nombre, float x, float y = 0f)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.position = new Vector3(x, y, 0f);
            Creados.Add(objeto);
            return objeto.transform;
        }

        protected Health Muneco(float x, int vida = 3)
        {
            var objeto = new GameObject("Muneco") { layer = LayerMask.NameToLayer(Capas.Enemy) };
            objeto.transform.position = new Vector3(x, 0f, 0f);
            var caja = objeto.AddComponent<BoxCollider2D>();
            caja.isTrigger = true;
            caja.size = new Vector2(1f, 2f);
            caja.offset = new Vector2(0f, 1f);
            var salud = objeto.AddComponent<Health>();
            salud.Inicializar(vida);
            Creados.Add(objeto);
            return salud;
        }

        // --- Esperas ---

        protected int Veces(ESTADOS_JUGADOR estado) => HistorialJugador.Count(e => e == estado);

        protected IEnumerator EsperarEstado(ESTADOS_JUGADOR estado, float maximo) =>
            EsperarHasta(() => Jugador.EstadoActual == estado || HistorialJugador.Contains(estado), maximo, $"el estado {estado}");

        protected static IEnumerator EsperarHasta(Func<bool> condicion, float maximo, string descripcion)
        {
            var limite = Time.time + maximo;
            while (!condicion())
            {
                if (Time.time > limite)
                {
                    Assert.Fail($"Tiempo agotado ({maximo:0.0} s) esperando {descripcion}.");
                }

                yield return null;
            }
        }

        /// <summary>Espera y falla si la condición llega a cumplirse.</summary>
        protected static IEnumerator ComprobarQueNunca(Func<bool> condicion, float segundos, string descripcion)
        {
            var fin = Time.time + segundos;
            while (Time.time < fin)
            {
                Assert.IsFalse(condicion(), descripcion);
                yield return null;
            }
        }

        protected static IEnumerator Durante(float segundos, Action cadaFrame)
        {
            var fin = Time.time + segundos;
            while (Time.time < fin)
            {
                cadaFrame();
                yield return null;
            }
        }
    }
}
