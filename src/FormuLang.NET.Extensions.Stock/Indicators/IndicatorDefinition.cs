using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Utils;
using FormuLang.NET.Core.Validation;

namespace FormuLang.NET.Extensions.Stock.Indicators
{
    /// <summary>
    /// Parameter of an indicator with its default value, e.g. <c>period</c> = <c>14</c>.
    /// </summary>
    public sealed record IndicatorParameter(string Name, IValue DefaultValue)
    {
        public IndicatorParameter(string name, double defaultValue)
            : this(name, Value.Number(defaultValue, StringConverter.NumberToString(defaultValue))) { }
    }

    /// <summary>
    /// Indicator definition of the indicator registry (docs/standards/stock/README.md, "Indicator registry").
    /// </summary>
    public sealed class IndicatorDefinition
    {
        /// <summary>
        /// Unique identifier used to call the indicator, e.g. <c>"SMA"</c>.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Formula executed in an Indicator Context, once for every candle.
        /// </summary>
        public string Formula { get; }

        /// <summary>
        /// Parameters with their default values, in the order of the definition.
        /// </summary>
        public IReadOnlyList<IndicatorParameter> Parameters { get; }

        /// <exception cref="ArgumentException">
        /// The identifier or a parameter name is not a valid identifier, or a parameter is defined twice.
        /// </exception>
        public IndicatorDefinition(string id, string formula, IEnumerable<IndicatorParameter> parameters)
        {
            if (!IdentifierValidator.Validate(id))
                throw new ArgumentException($"\"{id}\" is not a valid identifier.", nameof(id));

            Parameters = parameters.ToArray();

            foreach (IndicatorParameter parameter in Parameters)
            {
                if (!IdentifierValidator.Validate(parameter.Name))
                    throw new ArgumentException($"\"{parameter.Name}\" is not a valid parameter name.", nameof(parameters));

                if (parameter.DefaultValue.Type.Base is Types.ErrorName or Types.UndefinedName)
                    throw new ArgumentException($"Parameter \"{parameter.Name}\" has no valid default value.", nameof(parameters));
            }

            if (Parameters.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != Parameters.Count)
                throw new ArgumentException("A parameter is defined more than once.", nameof(parameters));

            Id = id;
            Formula = formula;
        }

        public IndicatorDefinition(string id, string formula, params IndicatorParameter[] parameters)
            : this(id, formula, (IEnumerable<IndicatorParameter>)parameters) { }

        public IndicatorParameter? GetParameter(string name) => Parameters.FirstOrDefault(p => p.Name == name);
    }

    /// <summary>
    /// Indicator definitions provided by the host application. Formulas use it only through <c>INDICATOR</c>.
    /// </summary>
    public sealed class IndicatorRegistry
    {
        private readonly Dictionary<string, IndicatorDefinition> _definitions = new(StringComparer.Ordinal);

        public IEnumerable<IndicatorDefinition> Definitions => _definitions.Values;

        /// <exception cref="InvalidOperationException">An indicator with the same identifier is already registered.</exception>
        public IndicatorRegistry Add(IndicatorDefinition definition)
        {
            if (!_definitions.TryAdd(definition.Id, definition))
                throw new InvalidOperationException($"Indicator \"{definition.Id}\" is already registered.");

            return this;
        }

        /// <summary>
        /// Registers an indicator, e.g. <c>Add("SMA", formula, new IndicatorParameter("period", 14))</c>.
        /// </summary>
        public IndicatorRegistry Add(string id, string formula, params IndicatorParameter[] parameters)
            => Add(new IndicatorDefinition(id, formula, parameters));

        public bool Remove(string id) => _definitions.Remove(id);

        public bool Contains(string id) => _definitions.ContainsKey(id);

        public IndicatorDefinition? Get(string id) => _definitions.GetValueOrDefault(id);
    }
}
