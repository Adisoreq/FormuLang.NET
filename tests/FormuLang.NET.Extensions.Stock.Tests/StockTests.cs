using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Processing;
using FormuLang.NET.Extensions.Stock.Contexts;
using FormuLang.NET.Extensions.Stock.Indicators;
using Env = FormuLang.NET.Core.Structures.Environment;

namespace FormuLang.NET.Extensions.Stock.Tests
{
    /// <summary>
    /// Examples of docs/standards/stock: CANDLE, INDICATOR, indicator examples, pattern and strategy examples.
    /// </summary>
    public class StockTests
    {
        // docs/standards/stock/examples/indicator_examples.md
        private const string Sma = """
            $period = PARAMETERS.period

            # The last `period` values of the source, fewer at the beginning of the chart
            $window = RANGE(SOURCE, MAX(I - $period + 1, @array_indexing), I)
            $complete = AND(LEN($window) == $period, LEN(FILTER($window, (e) => ISNULL(e))) == 0)

            IF($complete, AVG($window), null)
            """;

        private const string Ema = """
            $period = PARAMETERS.period
            $alpha = 2 / ($period + 1)
            $x = SOURCE[I]

            $window = RANGE(SOURCE, MAX(I - $period + 1, @array_indexing), I)
            $complete = AND(LEN($window) == $period, LEN(FILTER($window, (e) => ISNULL(e))) == 0)

            IF(ISNULL($x),
               null,
               IF(NOT(ISNULL(PREV)),
                  $alpha * $x + (1 - $alpha) * PREV,
                  IF($complete, AVG($window), null)))
            """;

        private const string Macd = """
            $short = INDICATOR("EMA", { period: PARAMETERS.short_period }, SOURCE)
            $long = INDICATOR("EMA", { period: PARAMETERS.long_period }, SOURCE)

            # MACD line for every candle: the two series joined element by element
            $line = MAP($short, (s, i) => IF(OR(ISNULL(s), ISNULL($long[i])), null, s - $long[i]))

            $macd = $line[I]
            $signal = INDICATOR("EMA", { period: PARAMETERS.signal_period }, $line)[I]

            IF(OR(ISNULL($macd), ISNULL($signal)),
               null,
               { macd: $macd, signal: $signal, histogram: $macd - $signal })
            """;

        private static Env CreateEnvironment(out IndicatorRegistry registry)
        {
            var env = Env.Create();
            registry = env.RegisterStock().Registry;

            registry.Add("SMA", Sma, new IndicatorParameter("period", 14));
            registry.Add("EMA", Ema, new IndicatorParameter("period", 14));
            registry.Add("MACD", Macd,
                new IndicatorParameter("short_period", 12),
                new IndicatorParameter("long_period", 26),
                new IndicatorParameter("signal_period", 9));

            return env;
        }

        private static Env CreateEnvironment() => CreateEnvironment(out _);

        private static Candle Flat(double price) => new(price, price, price, price);

        /// <summary>
        /// <c>CLOSE</c> = <c>[1, 2, 3, 4, 5]</c>.
        /// </summary>
        private static readonly Candle[] OneToFive = [Flat(1), Flat(2), Flat(3), Flat(4), Flat(5)];

        /// <summary>
        /// Executes the formula in a Stock Context and returns the serialized result of the last iteration.
        /// </summary>
        private static string RunLast(string formula, Candle[]? candles = null, Env? env = null)
        {
            var context = StockContext.Create(env ?? CreateEnvironment(), candles ?? OneToFive);
            IterationExecutionResult result = Executor.Create().ExecuteIterations(formula, context);
            return result.Results[^1].ToString();
        }

        private static void AssertResult(string formula, string expected, Candle[]? candles = null)
        {
            string actual = RunLast(formula, candles);

            if (expected.EndsWith("_ERROR"))
                Assert.StartsWith($"[{expected} (", actual);
            else
                Assert.Equal(expected, actual);
        }

        // Standard

        [Fact]
        public void RegisterStock_AddsTheStandard()
        {
            var env = CreateEnvironment();

            Assert.True(env.Supports("stock-0.1"));
            Assert.Equal("[\"core-0.3\", \"stock-0.1\"]", env.GetProperty("STANDARDS").GetValue().ToString());
            Assert.Contains("Stock.Candle", env.ExtendedTypes);
            Assert.NotNull(env.GetFunction("CANDLE"));
            Assert.NotNull(env.GetIndicatorRegistry());
            Assert.True(StockStandard.IsCoreSupported(env));
        }

        [Theory]
        [InlineData("REQUIRE \"stock-0.1\"\nREQUIRE \"core-0.2+\"\n1", "1")]
        [InlineData("REQUIRE \"stock-0.x\"\nCLOSE[I] - OPEN[I]", "0")]
        public void Require(string formula, string expected) => AssertResult(formula, expected);

        // Stock Context

        [Fact]
        public void StockContext_Constants()
        {
            Candle[] candles = [new(10, 12, 9, 11, 5_000, 1_700_000_000_000), new(11, 13, 10, 12)];

            Assert.Equal("[2, [10, 11], [12, 13], [9, 10], [11, 12], [5000, null], [1700000000000, null]]",
                RunLast("[N, OPEN, HIGH, LOW, CLOSE, VOLUME, TIME]", candles));
            Assert.Equal("[12, 12, 13]", RunLast("[CLOSE[I], CANDLES[I].close, CANDLES[I].high]", candles));
            Assert.Equal("true", RunLast("ISOBJ(CANDLES[0])", candles));
        }

        [Fact]
        public void StockContext_RejectsInvalidCandles()
        {
            Assert.Throws<ArgumentException>(() => StockContext.Create(CreateEnvironment(), [new Candle(10, 9, 8, 11)]));
            Assert.Throws<ArgumentException>(() => StockContext.Create(CreateEnvironment(), [new Candle(10, 12, 8, 11, -1)]));
        }

        [Fact]
        public void StockContext_CandleSerialization()
        {
            Assert.Equal("[Stock.Candle] { open: 10, high: 12, low: 9, close: 11, volume: null, time: null }",
                RunLast("@include_object_class = true\nTEXT(CANDLES[0])", [new Candle(10, 12, 9, 11)]));
        }

        // CANDLE

        [Theory]
        [InlineData("CANDLE({ open: 1_000, high: 1_200, low: 800, close: 1_000 })",
            "[Stock.Candle] { open: 1000, high: 1200, low: 800, close: 1000, volume: null, time: null }")]
        [InlineData("CANDLE({ open: 10, high: 12, low: 9, close: 11, volume: 5_000 })",
            "[Stock.Candle] { open: 10, high: 12, low: 9, close: 11, volume: 5000, time: null }")]
        [InlineData("CANDLE({ open: 1_000, low: 300 })",
            "[OPERATION_ERROR (3)] Object does not meet the requirements to be <Stock.Candle>.")]
        [InlineData("CANDLE({ open: 10, high: 9, low: 8, close: 11 })",
            "[OPERATION_ERROR (3)] Object does not meet the requirements to be <Stock.Candle>.")]
        [InlineData("CANDLE({ open: 10, high: 12, low: 9, close: 11, color: \"red\" })",
            "[OPERATION_ERROR (3)] Object does not meet the requirements to be <Stock.Candle>.")]
        [InlineData("CANDLE({ open: 10, high: 12, low: 9, close: 11, volume: \"5\" })",
            "[OPERATION_ERROR (3)] Object does not meet the requirements to be <Stock.Candle>.")]
        [InlineData("CANDLE({ open: 10, high: 12, low: 9, close: 11, volume: -5 })",
            "[OPERATION_ERROR (3)] Object does not meet the requirements to be <Stock.Candle>.")]
        public void Candle_Examples(string formula, string expected)
        {
            var result = Executor.Create().Execute("@include_object_class = true\n" + formula, CreateEnvironment());
            Assert.Equal(expected, result.ToString());
        }

        [Theory]
        [InlineData("CANDLE(1)", "ARGUMENT_ERROR")]
        [InlineData("KEYS(CANDLE({ open: 1, high: 1, low: 1, close: 1 }))", "[\"open\", \"high\", \"low\", \"close\", \"volume\", \"time\"]")]
        [InlineData("CANDLE({ open: 1, high: 2, low: 1, close: 2 }).high", "2")]
        [InlineData("EQU(CANDLE({ open: 1, high: 1, low: 1, close: 1 }), CANDLES[0])", "true")]
        public void Candle_Values(string formula, string expected) => AssertResult(formula, expected, [Flat(1)]);

        // INDICATOR

        [Theory]
        [InlineData("INDICATOR(\"SMA\", { period: 3 })", "[null, null, 2, 3, 4]")]
        [InlineData("INDICATOR(\"SMA\", { period: 2 }, [10, 20, 30, 40, 50])", "[null, 15, 25, 35, 45]")]
        [InlineData("INDICATOR(\"SMA\", { period: 3 })[I]", "4")]
        [InlineData("INDICATOR(\"XYZ\")", "INVALID_KEY_ERROR")]
        [InlineData("INDICATOR(\"SMA\", { length: 3 })", "INVALID_KEY_ERROR")]
        [InlineData("INDICATOR(\"SMA\", { period: 3 }, [1, 2])", "OPERATION_ERROR")]
        [InlineData("INDICATOR(\"SMA\", { period: 3 }, [1, 2, \"3\", 4, 5])", "OPERATION_ERROR")]
        [InlineData("INDICATOR(\"SMA\", { period: 2 }, [1, null, 3, 4, 5])", "[null, null, null, 3.5, 4.5]")]
        [InlineData("INDICATOR(\"SMA\")", "[null, null, null, null, null]")]
        [InlineData("INDICATOR(\"EMA\", { period: 3 })", "[null, null, 2, 3, 4]")]
        [InlineData("INDICATOR(1)", "ARGUMENT_ERROR")]
        public void Indicator_Examples(string formula, string expected) => AssertResult(formula, expected);

        [Fact]
        public void Indicator_EnvironmentVariablesOfTheCallerAreNotPassed()
        {
            Assert.Equal("[null, null, 2, 3, 4]", RunLast("@array_indexing = 1\nINDICATOR(\"SMA\", { period: 3 })"));
            Assert.Equal("4", RunLast("@array_indexing = 1\nINDICATOR(\"SMA\", { period: 3 })[I]"));
        }

        [Fact]
        public void Indicator_OutsideOfAStockContext()
        {
            var result = Executor.Create().Execute("INDICATOR(\"SMA\")", CreateEnvironment());
            Assert.Equal("OPERATION_ERROR", result.Error.Name);
        }

        [Fact]
        public void Indicator_ThatCallsItself()
        {
            var env = CreateEnvironment(out var registry);
            registry.Add("LOOP", "INDICATOR(\"LOOP\")[I]");
            registry.Add("A", "INDICATOR(\"B\")[I]");
            registry.Add("B", "INDICATOR(\"A\")[I]");

            Assert.StartsWith("[OPERATION_ERROR (3)] Indicator \"LOOP\" calls itself.", RunLast("INDICATOR(\"LOOP\")", env: env));
            Assert.StartsWith("[OPERATION_ERROR (3)] Indicator \"A\" calls itself.", RunLast("INDICATOR(\"A\")", env: env));
        }

        [Theory]
        [InlineData("\"text\"")]
        [InlineData("IF(I == 0, 1, { a: 1 })")]
        [InlineData("IF(I == 0, { a: 1 }, { b: 1 })")]
        [InlineData("{ a: \"x\" }")]
        [InlineData("$x = 1")]
        public void Indicator_ResultThatIsNotAllowed(string formula)
        {
            var env = CreateEnvironment(out var registry);
            registry.Add("BAD", formula);

            Assert.StartsWith("[OPERATION_ERROR (3)] Iteration", RunLast("INDICATOR(\"BAD\")", env: env));
        }

        [Fact]
        public void Indicator_ErrorOfAnIterationIsReturned()
        {
            var env = CreateEnvironment(out var registry);
            registry.Add("FAIL", "IF(I == 2, ERROR(\"broken\"), I)");

            Assert.Equal("[CUSTOM_ERROR (1)] broken", RunLast("INDICATOR(\"FAIL\")", env: env));
        }

        [Fact]
        public void Indicator_Macd()
        {
            var context = StockContext.Create(CreateEnvironment(), OneToFive);
            IValue series = context.ComputeIndicator("MACD", Params(("short_period", 2), ("long_period", 3), ("signal_period", 2)));

            IValue[] values = (IValue[])series.Raw!;
            Assert.Equal(5, values.Length);
            Assert.All(values[..3], v => Assert.Equal(Types.NullName, v.Type.Base));

            foreach (IValue value in values[3..])
            {
                var obj = (IObject)value.Raw!;
                Assert.Equal(["macd", "signal", "histogram"], obj.Keys);
                Assert.Equal(0.5, (double)obj.GetProperty("macd").GetValue().Raw!, 1e-12);
                Assert.Equal(0.5, (double)obj.GetProperty("signal").GetValue().Raw!, 1e-12);
                Assert.Equal(0, (double)obj.GetProperty("histogram").GetValue().Raw!, 1e-12);
            }
        }

        [Fact]
        public void Indicator_MacdUsage()
        {
            const string formula = "$macd = INDICATOR(\"MACD\", { short_period: 2, long_period: 3, signal_period: 2 })[I]\nIF(ISNULL($macd), null, ROUND($macd.histogram, 6))";
            var context = StockContext.Create(CreateEnvironment(), OneToFive);

            var result = Executor.Create().ExecuteIterations(formula, context);
            Assert.Equal(["null", "null", "null", "0", "0"], result.Values.Select(v => v.ToString()!));
        }

        [Fact]
        public void IndicatorContext_CreatedByTheHost()
        {
            var context = IndicatorContext.Create(CreateEnvironment(), OneToFive, Params(("period", 3)));
            Assert.Equal("[null, null, 2, 3, 4]", Executor.Create().ExecuteIndicator(Sma, context).ToString());
        }

        // Pattern and strategy examples (docs/standards/stock/examples/strategy_examples.md)

        private const string BullishEngulfing = """
            # In the first iteration, the previous candle is the current one
            $prev = CANDLES[MAX(I - 1, @array_indexing)]
            $curr = CANDLES[I]

            $prev_bearish = $prev.close < $prev.open
            $curr_bullish = $curr.close > $curr.open
            $covers = AND($curr.open <= $prev.close, $curr.close >= $prev.open)

            AND(I > @array_indexing, AND($prev_bearish, AND($curr_bullish, $covers)))
            """;

        private const string Crossover = """
            $fast = INDICATOR("SMA", { period: 10 })
            $slow = INDICATOR("SMA", { period: 40 })

            $p = MAX(I - 1, @array_indexing)
            $fast_prev = $fast[$p]
            $slow_prev = $slow[$p]
            $fast_curr = $fast[I]
            $slow_curr = $slow[I]

            # Both SMAs need a value for the previous and the current candle
            $values = [$fast_prev, $slow_prev, $fast_curr, $slow_curr]
            $ready = AND(I > @array_indexing, LEN(FILTER($values, (e) => ISNULL(e))) == 0)

            IF(NOT($ready),
               NEUTRAL,
               IF(AND($fast_prev <= $slow_prev, $fast_curr > $slow_curr),
                  BUY,
                  IF(AND($fast_prev >= $slow_prev, $fast_curr < $slow_curr), SELL, NEUTRAL)))
            """;

        private const string MacdStrength = """
            $macd = INDICATOR("MACD")[I]

            IF(ISNULL($macd),
               NEUTRAL,
               CLAMP($macd.histogram / CLOSE[I] * 100, SELL, BUY))
            """;

        [Fact]
        public void Pattern_BullishEngulfing()
        {
            Candle[] candles =
            [
                new(10, 10.5, 7.5, 8),   // bearish
                new(7.5, 11, 7, 10.5),   // bullish, covers the previous body
                new(10.5, 11, 9, 9.5),   // bearish
                new(9.6, 10, 9, 9.8),    // bullish, does not cover
            ];

            var context = PatternContext.Create(CreateEnvironment(), candles);
            PatternResult result = Executor.Create().ExecutePattern(BullishEngulfing, context);

            Assert.True(result.Successful);
            Assert.Equal([1], result.Indices);
        }

        [Fact]
        public void Pattern_ResultThatIsNotABoolean()
        {
            var context = PatternContext.Create(CreateEnvironment(), OneToFive);
            PatternResult result = Executor.Create().ExecutePattern("I", context);

            Assert.Equal("OPERATION_ERROR", result.Error.Name);
        }

        [Fact]
        public void Strategy_MovingAverageCrossover()
        {
            // Falling prices, then rising prices: the fast SMA crosses above the slow SMA once
            double[] close = [.. Enumerable.Range(0, 50).Select(i => 100.0 - i), .. Enumerable.Range(1, 50).Select(i => 50.0 + 2 * i)];
            Candle[] candles = close.Select(Flat).ToArray();

            var context = StrategyContext.Create(CreateEnvironment(), candles);
            StrategyResult result = Executor.Create().ExecuteStrategy(Crossover, context);

            Assert.True(result.Successful, result.Error.ToString());
            Assert.Equal(candles.Length, result.Signals.Count);

            double?[] expected = ExpectedCrossover(close, 10, 40);
            Assert.Equal(expected, result.Signals);
            Assert.Single(result.Signals, s => s == StrategyContext.Buy);
        }

        [Fact]
        public void Strategy_MacdSignalStrength()
        {
            double[] close = Enumerable.Range(0, 60).Select(i => 100 + 10 * Math.Sin(i / 5.0)).ToArray();
            var context = StrategyContext.Create(CreateEnvironment(), close.Select(Flat));

            StrategyResult result = Executor.Create().ExecuteStrategy(MacdStrength, context);

            Assert.True(result.Successful, result.Error.ToString());
            Assert.All(result.Signals, s => Assert.InRange(s!.Value, -1, 1));
            Assert.All(result.Signals.Take(33), s => Assert.Equal(0, s));
            Assert.Contains(result.Signals, s => s != 0);
        }

        [Fact]
        public void Strategy_SignalsAreClamped()
        {
            var context = StrategyContext.Create(CreateEnvironment(), OneToFive);
            StrategyResult result = Executor.Create().ExecuteStrategy("I - 2 + BUY * 0 + SELL * 0 + NEUTRAL", context);

            Assert.Equal([-1, -1, 0, 1, 1], result.Signals);
        }

        // Helpers

        private static IObject Params(params (string Key, double Value)[] entries)
        {
            var obj = new Core.Primitives.Object();
            foreach (var (key, value) in entries)
                obj.InitVariable(key, Value.Number(value, ""));
            return obj;
        }

        private static double?[] ExpectedCrossover(double[] close, int fastPeriod, int slowPeriod)
        {
            double? Sma(int i, int period) => i + 1 < period ? null : close[(i - period + 1)..(i + 1)].Average();

            var signals = new double?[close.Length];

            for (int i = 0; i < close.Length; i++)
            {
                int p = Math.Max(i - 1, 0);
                double? fp = Sma(p, fastPeriod), sp = Sma(p, slowPeriod), fc = Sma(i, fastPeriod), sc = Sma(i, slowPeriod);

                if (i == 0 || fp == null || sp == null || fc == null || sc == null)
                    signals[i] = 0;
                else if (fp <= sp && fc > sc)
                    signals[i] = 1;
                else if (fp >= sp && fc < sc)
                    signals[i] = -1;
                else
                    signals[i] = 0;
            }

            return signals;
        }
    }
}
