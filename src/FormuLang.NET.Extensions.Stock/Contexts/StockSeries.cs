using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Processing;
using FormuLang.NET.Core.Utils;
using System.Runtime.CompilerServices;

namespace FormuLang.NET.Extensions.Stock.Contexts
{
    /// <summary>
    /// Candles of one chart and the values derived from them, shared by a Stock Context and every child Indicator
    /// Context created by <c>INDICATOR</c>. Also stores the computed indicator series, which are deterministic
    /// (docs/standards/stock/functions/INDICATOR.md, "Performance").
    /// </summary>
    internal sealed class StockSeries
    {
        private readonly Dictionary<string, IValue> _indicators = new(StringComparer.Ordinal);
        private readonly ConditionalWeakTable<IValue[], string> _sourceKeys = new();

        public IReadOnlyList<Candle> Candles { get; }
        public int Count => Candles.Count;

        /// <summary>
        /// Executor of the indicator formulas.
        /// </summary>
        public Executor Executor { get; }

        public IValue CandlesValue { get; }
        public IValue Open { get; }
        public IValue High { get; }
        public IValue Low { get; }
        public IValue Close { get; }
        public IValue Volume { get; }
        public IValue Time { get; }

        /// <exception cref="ArgumentException">A candle breaks the rules of <c>Stock.Candle</c>.</exception>
        public StockSeries(IEnumerable<Candle> candles, Executor? executor)
        {
            Candles = candles.ToArray();
            Executor = executor ?? Executor.Create();

            for (int i = 0; i < Candles.Count; i++)
            {
                if (!Candles[i].IsValid())
                    throw new ArgumentException($"Candle {i} does not meet the requirements of {Candle.TypeName}.", nameof(candles));
            }

            CandlesValue = Value.Array(Candles.Select(c => c.ToValue()), "CANDLES");
            Open = Numbers(c => c.Open, "OPEN");
            High = Numbers(c => c.High, "HIGH");
            Low = Numbers(c => c.Low, "LOW");
            Close = Numbers(c => c.Close, "CLOSE");
            Volume = Numbers(c => c.Volume, "VOLUME");
            Time = Numbers(c => c.Time, "TIME");
        }

        private Value Numbers(Func<Candle, double?> select, string name)
            => Value.Array(Candles.Select(c => select(c) is double d
                ? (IValue)Value.Number(d, StringConverter.NumberToString(d))
                : Value.Null("null")), name);

        public bool TryGetIndicator(string key, out IValue? series) => _indicators.TryGetValue(key, out series);

        public void StoreIndicator(string key, IValue series) => _indicators[key] = series;

        /// <summary>
        /// Key of an indicator source: <c>CLOSE</c> itself, or the serialized elements.
        /// </summary>
        public string SourceKey(IValue[] source)
        {
            if (ReferenceEquals(source, Close.Raw))
                return "CLOSE";

            return _sourceKeys.GetValue(source, s => StringConverter.ArrayToString(s));
        }
    }
}
