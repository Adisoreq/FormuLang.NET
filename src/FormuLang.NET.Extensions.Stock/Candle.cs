using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Utils;

namespace FormuLang.NET.Extensions.Stock
{
    /// <summary>
    /// One candle of a chart, as provided by the host application (docs/standards/stock/types/Candle.md).
    /// In a formula it is a <c>Stock.Candle</c> object with the keys <c>open</c>, <c>high</c>, <c>low</c>, <c>close</c>,
    /// <c>volume</c> and <c>time</c>.
    /// </summary>
    /// <param name="Volume">Traded volume of the period; <c>null</c> if unknown.</param>
    /// <param name="Time">Start of the period: Unix time in milliseconds, UTC; <c>null</c> if unknown.</param>
    public readonly record struct Candle(double Open, double High, double Low, double Close, double? Volume = null, double? Time = null)
    {
        public const string TypeName = "Stock.Candle";

        /// <summary>
        /// Attributes of <c>Stock.Candle</c>, in the order of the type.
        /// </summary>
        public static IReadOnlyList<string> Attributes { get; } = ["open", "high", "low", "close", "volume", "time"];

        /// <summary>
        /// Candle with the start of the period given as a date.
        /// </summary>
        public Candle(double open, double high, double low, double close, double? volume, DateTimeOffset time)
            : this(open, high, low, close, volume, time.ToUnixTimeMilliseconds()) { }

        /// <summary>
        /// Checks the rules of <c>Stock.Candle</c>: finite prices, <c>high</c> is the highest and <c>low</c> the lowest
        /// price, and <c>volume</c> is not negative.
        /// </summary>
        public bool IsValid()
        {
            double[] prices = [Open, High, Low, Close];

            return prices.All(double.IsFinite)
                && High >= Open && High >= Close && High >= Low
                && Low <= Open && Low <= Close
                && (Volume is not double v || (double.IsFinite(v) && v >= 0))
                && (Time is not double t || double.IsFinite(t));
        }

        /// <summary>
        /// The candle as a FormuLang <c>Stock.Candle</c> value.
        /// </summary>
        public IValue ToValue()
        {
            var obj = new Core.Primitives.Object();

            obj.InitVariable("open", Number(Open));
            obj.InitVariable("high", Number(High));
            obj.InitVariable("low", Number(Low));
            obj.InitVariable("close", Number(Close));
            obj.InitVariable("volume", Volume is double v ? Number(v) : Value.Null("null"));
            obj.InitVariable("time", Time is double t ? Number(t) : Value.Null("null"));

            return Value.Object(obj, TypeName, "");
        }

        /// <summary>
        /// Reads a candle from an object, following the algorithm of <c>CANDLE</c> (docs/standards/stock/functions/CANDLE.md):
        /// only the attributes of <c>Stock.Candle</c>, the required prices are Numbers, <c>volume</c> and <c>time</c> are
        /// Numbers or <c>null</c>, and the values follow the rules of the type.
        /// </summary>
        public static bool TryFromObject(IObject obj, out Candle candle)
        {
            candle = default;

            if (obj.Keys.Any(key => !Attributes.Contains(key)))
                return false;

            double? Read(string key, bool required, out bool ok)
            {
                ok = true;

                if (!obj.HasKey(key))
                {
                    ok = !required;
                    return null;
                }

                IValue value = obj.GetProperty(key).GetValue();

                if (value.Type.Base == Types.NumberName && value.Raw is double d)
                    return d;

                ok = !required && value.Type.Base == Types.NullName;
                return null;
            }

            double? open = Read("open", true, out bool o), high = Read("high", true, out bool h),
                low = Read("low", true, out bool l), close = Read("close", true, out bool c),
                volume = Read("volume", false, out bool v), time = Read("time", false, out bool t);

            if (!(o && h && l && c && v && t))
                return false;

            candle = new Candle(open!.Value, high!.Value, low!.Value, close!.Value, volume, time);
            return candle.IsValid();
        }

        private static IValue Number(double value) => Value.Number(value, StringConverter.NumberToString(value));
    }
}
