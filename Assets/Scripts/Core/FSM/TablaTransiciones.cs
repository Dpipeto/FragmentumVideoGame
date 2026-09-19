using System;
using System.Collections.Generic;

namespace Fragmentum.Core.FSM
{
    /// <summary>
    /// Función de transición de la FSM, declarada en un único sitio.
    ///
    /// Cumple dos papeles:
    /// 1. <see cref="MaquinaEstados{TEstado}.TransitionTo"/> rechaza cualquier transición
    ///    que no esté aquí, así que el código no puede alejarse del diseño sin avisar.
    /// 2. El exportador de diagramas la lee para generar el Mermaid, de modo que el
    ///    diagrama de la tesis siempre corresponde al código.
    ///
    /// Se declara encadenando llamadas:
    /// <code>
    /// new TablaTransiciones&lt;STATES&gt;("Enemigo")
    ///     .Inicial(STATES.IDLE)
    ///     .DefinirGrupo("VIVO", STATES.IDLE, STATES.PATROL, STATES.CHASE)
    ///     .Permitir(STATES.IDLE, STATES.CHASE, "detecta al jugador")
    ///     .PermitirDesdeGrupo("VIVO", STATES.DEAD, "vida 0");
    /// </code>
    /// </summary>
    public sealed class TablaTransiciones<TEstado> : ITablaLegible where TEstado : struct, Enum
    {
        /// <summary>Una transición declarada: de qué estado, a cuál y con qué condición.</summary>
        public readonly struct Transicion
        {
            public Transicion(TEstado desde, TEstado hacia, string condicion, string grupoOrigen)
            {
                Desde = desde;
                Hacia = hacia;
                Condicion = condicion ?? string.Empty;
                GrupoOrigen = grupoOrigen ?? string.Empty;
            }

            public TEstado Desde { get; }
            public TEstado Hacia { get; }
            public string Condicion { get; }

            /// <summary>Superestado del que nació la transición, si se declaró por grupo.</summary>
            public string GrupoOrigen { get; }
        }

        private readonly List<Transicion> _transiciones = new List<Transicion>();
        private readonly Dictionary<string, List<TEstado>> _grupos = new Dictionary<string, List<TEstado>>();
        private readonly HashSet<(TEstado desde, TEstado hacia)> _validas =
            new HashSet<(TEstado desde, TEstado hacia)>();

        private bool _tieneInicial;
        private TEstado _inicial;

        public TablaTransiciones(string nombre)
        {
            Nombre = string.IsNullOrEmpty(nombre) ? typeof(TEstado).Name : nombre;
        }

        public string Nombre { get; }

        /// <summary>Transiciones declaradas, en el orden en que se declararon.</summary>
        public IReadOnlyList<Transicion> Transiciones => _transiciones;

        /// <summary>Declara el estado en el que arranca la máquina.</summary>
        public TablaTransiciones<TEstado> Inicial(TEstado estado)
        {
            _inicial = estado;
            _tieneInicial = true;
            return this;
        }

        /// <summary>
        /// Define un superestado: un nombre que agrupa varios estados. Sirve para declarar
        /// de una vez transiciones comunes (por ejemplo, "cualquier estado VIVO puede morir")
        /// y para dibujar el diagrama con estados compuestos.
        /// </summary>
        public TablaTransiciones<TEstado> DefinirGrupo(string nombre, params TEstado[] estados)
        {
            if (string.IsNullOrEmpty(nombre) || estados == null || estados.Length == 0)
            {
                return this;
            }

            if (!_grupos.TryGetValue(nombre, out var lista))
            {
                lista = new List<TEstado>();
                _grupos.Add(nombre, lista);
            }

            foreach (var estado in estados)
            {
                if (!lista.Contains(estado))
                {
                    lista.Add(estado);
                }
            }

            return this;
        }

        /// <summary>Declara una transición concreta.</summary>
        public TablaTransiciones<TEstado> Permitir(TEstado desde, TEstado hacia, string condicion = "")
        {
            return Registrar(desde, hacia, condicion, string.Empty);
        }

        /// <summary>Declara la misma transición desde todos los estados de un grupo.</summary>
        public TablaTransiciones<TEstado> PermitirDesdeGrupo(string grupo, TEstado hacia, string condicion = "")
        {
            if (!_grupos.TryGetValue(grupo, out var estados))
            {
                throw new ArgumentException($"El grupo '{grupo}' no está definido. Usa DefinirGrupo antes.", nameof(grupo));
            }

            foreach (var desde in estados)
            {
                if (!EqualityComparer<TEstado>.Default.Equals(desde, hacia))
                {
                    Registrar(desde, hacia, condicion, grupo);
                }
            }

            return this;
        }

        /// <summary>Declara la misma transición desde todos los estados de la enumeración.</summary>
        public TablaTransiciones<TEstado> PermitirDesdeTodos(TEstado hacia, string condicion = "", params TEstado[] excepto)
        {
            foreach (TEstado desde in (TEstado[])Enum.GetValues(typeof(TEstado)))
            {
                if (EqualityComparer<TEstado>.Default.Equals(desde, hacia))
                {
                    continue;
                }

                if (excepto != null && Array.IndexOf(excepto, desde) >= 0)
                {
                    continue;
                }

                Registrar(desde, hacia, condicion, string.Empty);
            }

            return this;
        }

        /// <summary>¿Está declarada esta transición?</summary>
        public bool EsValida(TEstado desde, TEstado hacia) => _validas.Contains((desde, hacia));

        /// <summary>Estados que contiene un grupo. Lista vacía si el grupo no existe.</summary>
        public IReadOnlyList<TEstado> EstadosDelGrupo(string grupo)
        {
            return _grupos.TryGetValue(grupo, out var estados) ? estados : Array.Empty<TEstado>();
        }

        private TablaTransiciones<TEstado> Registrar(TEstado desde, TEstado hacia, string condicion, string grupoOrigen)
        {
            if (_validas.Add((desde, hacia)))
            {
                _transiciones.Add(new Transicion(desde, hacia, condicion, grupoOrigen));
            }

            return this;
        }

        // --- ITablaLegible: vista sin genéricos para el exportador de diagramas ---

        string ITablaLegible.EstadoInicial => _tieneInicial ? _inicial.ToString() : string.Empty;

        IEnumerable<(string desde, string hacia, string condicion, string grupoOrigen)> ITablaLegible.TransicionesLegibles
        {
            get
            {
                foreach (var t in _transiciones)
                {
                    yield return (t.Desde.ToString(), t.Hacia.ToString(), t.Condicion, t.GrupoOrigen);
                }
            }
        }

        IEnumerable<(string grupo, IReadOnlyList<string> estados)> ITablaLegible.GruposLegibles
        {
            get
            {
                foreach (var par in _grupos)
                {
                    var nombres = new List<string>(par.Value.Count);
                    foreach (var estado in par.Value)
                    {
                        nombres.Add(estado.ToString());
                    }

                    yield return (par.Key, nombres);
                }
            }
        }
    }
}
