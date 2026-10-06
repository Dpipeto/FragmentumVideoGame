using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Fragmentum.EditorTools
{
    /// <summary>
    /// Arma controladores de Animator desde código, de forma idempotente: los estados y las
    /// transiciones se reutilizan por nombre, así que regenerar no cambia los identificadores del
    /// archivo, y lo que sobra se quita. Lo usan el Golem de Ceniza
    /// (<see cref="ConstructorGolem"/>) y las mecánicas (<see cref="ConstructorMecanicas"/>).
    ///
    /// Todas las transiciones son instantáneas (pixel art: no se mezclan fotogramas) y no se
    /// interrumpen.
    /// </summary>
    public static class ControladoresAnimator
    {
        /// <summary>Una transición deseada. <see cref="Salida"/> null: sin tiempo de salida.</summary>
        public readonly struct Transicion
        {
            public readonly string Nombre;
            public readonly AnimatorState Hasta;
            public readonly float? Salida;
            public readonly AnimatorCondition[] Condiciones;

            public Transicion(string nombre, AnimatorState hasta, float? salida, params AnimatorCondition[] condiciones)
            {
                Nombre = nombre;
                Hasta = hasta;
                Salida = salida;
                Condiciones = condiciones;
            }
        }

        public static AnimatorController CargarOCrear(string ruta) =>
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta) ?? AnimatorController.CreateAnimatorControllerAtPath(ruta);

        /// <summary>Deja exactamente estos parámetros, en este orden (no toca el archivo si ya están).</summary>
        public static void AsegurarParametros(AnimatorController controlador, params (string nombre, AnimatorControllerParameterType tipo, float inicial)[] deseados)
        {
            var actuales = controlador.parameters;
            var iguales = actuales.Length == deseados.Length && actuales.Zip(deseados, (a, d) =>
                a.name == d.nombre && a.type == d.tipo && Mathf.Approximately(Inicial(a), d.inicial)).All(x => x);
            if (iguales)
            {
                return;
            }

            controlador.parameters = deseados.Select(d => new AnimatorControllerParameter
            {
                name = d.nombre,
                type = d.tipo,
                defaultFloat = d.inicial,
                defaultInt = Mathf.RoundToInt(d.inicial),
                defaultBool = d.inicial != 0f,
            }).ToArray();
        }

        private static float Inicial(AnimatorControllerParameter parametro)
        {
            switch (parametro.type)
            {
                case AnimatorControllerParameterType.Int:
                    return parametro.defaultInt;
                case AnimatorControllerParameterType.Bool:
                    return parametro.defaultBool ? 1f : 0f;
                default:
                    return parametro.defaultFloat;
            }
        }

        /// <summary>
        /// Un estado por clip, en este orden, con su clip y sin velocidad propia salvo
        /// <paramref name="velocidades"/> (estado → parámetro que la multiplica). Quita los que
        /// sobran y deja el primero como estado inicial.
        /// </summary>
        public static Dictionary<string, AnimatorState> Estados(AnimatorStateMachine maquina, IReadOnlyList<(string nombre, Motion clip)> deseados,
            IReadOnlyDictionary<string, string> velocidades = null)
        {
            var estados = new Dictionary<string, AnimatorState>();
            for (var i = 0; i < deseados.Count; i++)
            {
                var (nombre, clip) = deseados[i];
                var estado = maquina.states.Select(s => s.state).FirstOrDefault(s => s.name == nombre) ??
                             maquina.AddState(nombre, new Vector3(300f + 240f * (i % 2), 80f * i, 0f));
                estado.motion = clip;
                estado.speed = 1f;
                estado.writeDefaultValues = true;
                string parametro = null;
                estado.speedParameterActive = velocidades != null && velocidades.TryGetValue(nombre, out parametro);
                estado.speedParameter = parametro ?? string.Empty;
                estados[nombre] = estado;
            }

            foreach (var sobrante in maquina.states.Select(s => s.state).Where(s => !estados.ContainsKey(s.name)).ToList())
            {
                maquina.RemoveState(sobrante);
            }

            maquina.defaultState = estados[deseados[0].nombre];
            return estados;
        }

        /// <summary>Las transiciones que salen de <paramref name="desde"/>, en orden de prioridad.</summary>
        public static void Transiciones(AnimatorState desde, params Transicion[] deseadas)
        {
            foreach (var sobrante in desde.transitions.Where(t => deseadas.All(d => d.Nombre != t.name)).ToList())
            {
                desde.RemoveTransition(sobrante);
            }

            foreach (var deseada in deseadas)
            {
                var transicion = desde.transitions.FirstOrDefault(t => t.name == deseada.Nombre) ?? desde.AddTransition(deseada.Hasta);
                transicion.destinationState = deseada.Hasta;
                Configurar(transicion, deseada);
            }

            // El orden importa: la primera que se cumple gana.
            desde.transitions = deseadas.Select(d => desde.transitions.First(t => t.name == d.Nombre)).ToArray();
        }

        /// <summary>
        /// Las transiciones desde cualquier estado, en orden de prioridad. Ninguna vuelve al estado
        /// en que ya está: con su condición cumplida, el clip se reiniciaría en cada fotograma.
        /// </summary>
        public static void DesdeCualquierEstado(AnimatorStateMachine maquina, params Transicion[] deseadas)
        {
            foreach (var sobrante in maquina.anyStateTransitions.Where(t => deseadas.All(d => d.Nombre != t.name)).ToList())
            {
                maquina.RemoveAnyStateTransition(sobrante);
            }

            foreach (var deseada in deseadas)
            {
                var transicion = maquina.anyStateTransitions.FirstOrDefault(t => t.name == deseada.Nombre) ?? maquina.AddAnyStateTransition(deseada.Hasta);
                transicion.destinationState = deseada.Hasta;
                Configurar(transicion, deseada);
                transicion.canTransitionToSelf = false;
            }

            maquina.anyStateTransitions = deseadas.Select(d => maquina.anyStateTransitions.First(t => t.name == d.Nombre)).ToArray();
        }

        public static AnimatorCondition Condicion(AnimatorConditionMode modo, string parametro, float umbral = 0f) =>
            new AnimatorCondition { mode = modo, parameter = parametro, threshold = umbral };

        private static void Configurar(AnimatorStateTransition transicion, in Transicion deseada)
        {
            transicion.name = deseada.Nombre;
            transicion.hasExitTime = deseada.Salida.HasValue;
            transicion.exitTime = deseada.Salida ?? 1f;
            transicion.hasFixedDuration = true;
            transicion.duration = 0f;
            transicion.offset = 0f;
            transicion.interruptionSource = TransitionInterruptionSource.None;
            transicion.conditions = deseada.Condiciones;
        }
    }
}
