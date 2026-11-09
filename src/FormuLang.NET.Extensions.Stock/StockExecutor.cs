using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Processing;
using FormuLang.NET.Extensions.Stock.Contexts;

namespace FormuLang.NET.Extensions.Stock
{
    /// <summary>
    /// Executes formulas in the Stock context types and checks their expected results
    /// (docs/standards/stock/README.md, "Context types").
    /// </summary>
    public static class StockExecutor
    {
        /// <summary>
        /// Executes an indicator formula for every candle (Indicator Context).
        /// </summary>
        /// <returns>
        /// An Array with the value of the indicator for every candle; the Error of an iteration; or
        /// <c>OPERATION_ERROR</c> if an iteration returns a value that is not allowed, or a kind of value other than the
        /// previous non-<c>null</c> results.
        /// </returns>
        public static IValue ExecuteIndicator(this Executor executor, string formula, IndicatorContext context)
        {
            IterationExecutionResult run = executor.ExecuteIterations(formula, context, stopOnError: true);

            if (!run.Successful)
                return Value.Error(run.Error, "");

            string? kind = null;
            HashSet<string>? keys = null;
            var values = new List<IValue>(run.Results.Count);

            foreach (IValue value in run.Values)
            {
                string? current = Kind(value);

                if (current == null)
                    return NotAllowed(values.Count, "a value that is not allowed in the Indicator Context");

                if (current == Types.ObjectName)
                {
                    var objectKeys = ((IObject)value.Raw!).Keys.ToHashSet(StringComparer.Ordinal);

                    if (keys != null && !keys.SetEquals(objectKeys))
                        return NotAllowed(values.Count, "an Object with other keys than the previous results");

                    keys = objectKeys;
                }

                if (current != Types.NullName)
                {
                    if (kind != null && kind != current)
                        return NotAllowed(values.Count, "another kind of value than the previous results");

                    kind = current;
                }

                values.Add(value);
            }

            return Value.Array(values, "");
        }

        /// <summary>
        /// Kind of an allowed Indicator Context result: Null, Number, or Object with Number values.
        /// </summary>
        private static string? Kind(IValue value)
        {
            switch (value.Type.Base)
            {
                case Types.NullName:
                case Types.NumberName:
                    return value.Type.Base;

                case Types.ObjectName:
                    var obj = (IObject)value.Raw!;
                    return obj.Keys.All(k => obj.GetProperty(k).GetValue().Type.Base == Types.NumberName) ? Types.ObjectName : null;

                default:
                    return null;
            }
        }

        private static IValue NotAllowed(int iteration, string problem)
            => StockFunctions.OperationError($"Iteration {iteration} of the indicator returned {problem}.");

        /// <summary>
        /// Executes a pattern formula for every candle (Pattern Context) and collects the candles for which it returned
        /// <c>true</c>.
        /// </summary>
        /// <param name="stopOnError">Stops at the first Error, or at the first result that is not a Boolean.</param>
        public static PatternResult ExecutePattern(this Executor executor, string formula, PatternContext context, bool stopOnError = true)
        {
            IterationExecutionResult run = executor.ExecuteIterations(formula, context, stopOnError);
            var indices = new List<int>();
            IError error = run.Error;

            for (int i = 0; i < run.Results.Count; i++)
            {
                IValue value = run.Results[i].Value;

                if (value.Type.Base == Types.BooleanName)
                {
                    if (value.Raw is true)
                        indices.Add(i);
                }
                else if (run.Results[i].Successful && error.IsSuccess())
                {
                    error = Errors.OperationError($"Iteration {i} of the pattern returned a value that is not a Boolean.");
                    if (stopOnError)
                        break;
                }
            }

            return new PatternResult(indices, error, run);
        }

        /// <summary>
        /// Executes a strategy formula for every candle (Strategy Context). Signals outside of the range from
        /// <c>SELL</c> to <c>BUY</c> are clamped.
        /// </summary>
        /// <param name="stopOnError">Stops at the first Error, or at the first result that is not a Number.</param>
        public static StrategyResult ExecuteStrategy(this Executor executor, string formula, StrategyContext context, bool stopOnError = true)
        {
            IterationExecutionResult run = executor.ExecuteIterations(formula, context, stopOnError);
            var signals = new List<double?>(run.Results.Count);
            IError error = run.Error;

            for (int i = 0; i < run.Results.Count; i++)
            {
                IValue value = run.Results[i].Value;

                if (value.Raw is double signal && value.Type.Base == Types.NumberName)
                {
                    signals.Add(Math.Clamp(signal, StrategyContext.Sell, StrategyContext.Buy));
                    continue;
                }

                signals.Add(null);

                if (run.Results[i].Successful && error.IsSuccess())
                {
                    error = Errors.OperationError($"Iteration {i} of the strategy returned a value that is not a Number.");
                    if (stopOnError)
                        break;
                }
            }

            return new StrategyResult(signals, error, run);
        }
    }

    /// <param name="Indices">0-based indices of the candles at which the pattern starts.</param>
    /// <param name="Error">The first Error, or <c>SUCCESS</c>.</param>
    public sealed record PatternResult(IReadOnlyList<int> Indices, IError Error, IterationExecutionResult Iterations)
    {
        public bool Successful => Error.IsSuccess();
    }

    /// <param name="Signals">Signal of every executed iteration, from -1 to 1; <c>null</c> for a failed iteration.</param>
    /// <param name="Error">The first Error, or <c>SUCCESS</c>.</param>
    public sealed record StrategyResult(IReadOnlyList<double?> Signals, IError Error, IterationExecutionResult Iterations)
    {
        public bool Successful => Error.IsSuccess();
    }
}
