using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core;
using FormuLang.NET.Core.Functions;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Utils;
using FormuLang.NET.Extensions.Stock.Contexts;
using FormuLang.NET.Extensions.Stock.Indicators;

namespace FormuLang.NET.Extensions.Stock
{
    /// <summary>
    /// Built-in functions of the Stock standard (docs/standards/stock/functions).
    /// </summary>
    public static class StockFunctions
    {
        public const string CandleName = "CANDLE";
        public const string IndicatorName = "INDICATOR";

        /// <summary>
        /// <c>CANDLE(object)</c> (docs/standards/stock/functions/CANDLE.md).
        /// </summary>
        public static Function Candle { get; } = new(CandleName,
        [
            new FunctionOverload([ArgumentTemplate.Of("object", Types.Object)], (a, _) =>
                Stock.Candle.TryFromObject((IObject)a[0].Raw!, out Candle candle)
                    ? candle.ToValue()
                    : OperationError($"Object does not meet the requirements to be <{Stock.Candle.TypeName}>.")),
        ]);

        /// <summary>
        /// <c>INDICATOR(id, parameters, source)</c> with the indicators of <paramref name="registry"/>
        /// (docs/standards/stock/functions/INDICATOR.md).
        /// </summary>
        public static IndicatorFunction Indicator(IndicatorRegistry registry) => new(registry);

        internal static IValue OperationError(string message) => Value.Error(Errors.OperationError(message), "");

        internal static IValue InvalidKeyError(string message) => Value.Error(Errors.InvalidKeyError(message), "");
    }

    /// <summary>
    /// The <c>INDICATOR</c> function; holds the indicator registry of the environment.
    /// </summary>
    public sealed class IndicatorFunction : Function
    {
        public IndicatorRegistry Registry { get; }

        internal IndicatorFunction(IndicatorRegistry registry)
            : base(StockFunctions.IndicatorName, CreateOverloads(registry))
        {
            Registry = registry;
        }

        private static IFunctionOverload[] CreateOverloads(IndicatorRegistry registry)
        {
            ArgumentTemplate id = ArgumentTemplate.Of("id", Types.Text);
            ArgumentTemplate parameters = ArgumentTemplate.Of("parameters", Types.Object);
            ArgumentTemplate source = ArgumentTemplate.Of("source", Types.Array);

            return
            [
                new FunctionOverload([id], (a, c) => Compute(registry, (string)a[0].Raw!, null, null, c)),
                new FunctionOverload([id, parameters], (a, c) => Compute(registry, (string)a[0].Raw!, (IObject)a[1].Raw!, null, c)),
                new FunctionOverload([id, parameters, source], (a, c) => Compute(registry, (string)a[0].Raw!, (IObject)a[1].Raw!, a[2], c)),
            ];
        }

        private static IValue Compute(IndicatorRegistry registry, string id, IObject? parameters, IValue? source, IContext context)
        {
            if (context is not StockContext caller)
                return StockFunctions.OperationError($"\"{StockFunctions.IndicatorName}\" can be called only in a Stock Context.");

            StockSeries series = caller.Series;

            // 1. Definition
            IndicatorDefinition? definition = registry.Get(id);
            if (definition == null)
                return StockFunctions.InvalidKeyError($"Indicator \"{id}\" is not defined.");

            // 2. Parameters, completed with the default values
            var completed = new Core.Primitives.Object();

            if (parameters != null)
            {
                foreach (string name in parameters.Keys)
                {
                    if (definition.GetParameter(name) == null)
                        return StockFunctions.InvalidKeyError($"Indicator \"{id}\" has no parameter \"{name}\".");
                }
            }

            foreach (IndicatorParameter parameter in definition.Parameters)
            {
                IValue value = parameters != null && parameters.HasKey(parameter.Name)
                    ? parameters.GetProperty(parameter.Name).GetValue()
                    : parameter.DefaultValue;

                completed.InitConstant(parameter.Name, value);
            }

            IValue parametersValue = Value.Object(completed, "PARAMETERS");

            // 3. Source
            source ??= series.Close;
            IValue[] elements = (IValue[])source.Raw!;

            if (elements.Length != series.Count)
                return StockFunctions.OperationError($"The source has {elements.Length} elements, but there are {series.Count} candles.");

            if (elements.Any(e => e.Type.Base != Types.NumberName && e.Type.Base != Types.NullName))
                return StockFunctions.OperationError("Every element of the source must be a Number or null.");

            string key = $"{id}|{StringConverter.ValueToString(parametersValue)}|{series.SourceKey(elements)}";

            // 4. An indicator that calls itself, directly or indirectly
            for (IContext? c = caller; c is IndicatorContext indicator; c = indicator.Caller)
            {
                if (indicator.IndicatorKey == key)
                    return StockFunctions.OperationError($"Indicator \"{id}\" calls itself.");
            }

            if (series.TryGetIndicator(key, out IValue? stored))
                return stored!;

            // 5-7. Child Indicator Context with its own iterations
            var child = new IndicatorContext(caller.Environment, series, parametersValue, source, key, caller);
            child.PreExecute();

            IValue result = series.Executor.ExecuteIndicator(definition.Formula, child);
            series.StoreIndicator(key, result);
            return result;
        }
    }
}
