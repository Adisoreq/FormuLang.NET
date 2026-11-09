using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Standards;
using FormuLang.NET.Core.Structures;
using FormuLang.NET.Extensions.Stock.Indicators;

namespace FormuLang.NET.Extensions.Stock
{
    /// <summary>
    /// The Stock standard (docs/standards/stock/README.md). Every instance has its own indicator registry; register it
    /// with <see cref="StockEnvironmentExtensions.RegisterStock"/>.
    /// </summary>
    public sealed class StockStandard : IStandard
    {
        public const string Identifier = "stock-0.1";

        /// <summary>
        /// Required Core standard, in the <c>REQUIRE</c> format.
        /// </summary>
        public const string RequiredCore = "core-0.2+";

        public string ID => "stock";

        public IStandardVersion Version { get; } = new StandardVersion { Major = 0, Minor = 1 };

        public string Name => "Stock";

        public IEnumerable<string> ExtendedTypes => [Candle.TypeName];

        public IAbstractTree<string> ExtendedTypeTree { get; }

        public IEnumerable<IFunction> NamedFunctions { get; }

        /// <summary>
        /// Indicator definitions provided by the host application.
        /// </summary>
        public IndicatorRegistry Registry { get; }

        public StockStandard(IndicatorRegistry? registry = null)
        {
            Registry = registry ?? new IndicatorRegistry();

            var tree = new AbstractTree<string>(Types.ObjectName, Types.ObjectName);
            tree.AddNode(Candle.TypeName, Candle.TypeName, Types.ObjectName);
            ExtendedTypeTree = tree;

            NamedFunctions = [StockFunctions.Candle, StockFunctions.Indicator(Registry)];
        }

        /// <summary>
        /// Checks that the environment supports the required Core standard (<c>core-0.2+</c>).
        /// </summary>
        public static bool IsCoreSupported(IEnvironment environment)
            => environment.Standards.Any(s => CoreProcedures.Satisfies(s, "core", ["0", "2+"]));
    }

    public static class StockEnvironmentExtensions
    {
        /// <summary>
        /// Adds the Stock standard: <c>stock-0.1</c> in <c>STANDARDS</c>, the <c>Stock.Candle</c> type, the functions
        /// <c>CANDLE</c> and <c>INDICATOR</c>, and the indicator registry.
        /// </summary>
        /// <exception cref="InvalidOperationException">The environment does not support <c>core-0.2+</c>.</exception>
        public static StockStandard RegisterStock(this Core.Structures.Environment environment, IndicatorRegistry? registry = null)
        {
            if (!StockStandard.IsCoreSupported(environment))
                throw new InvalidOperationException($"The Stock standard requires {StockStandard.RequiredCore}.");

            var standard = new StockStandard(registry);
            environment.RegisterStandard(standard);
            return standard;
        }

        /// <summary>
        /// Indicator registry of the environment, or <c>null</c> if it does not support the Stock standard.
        /// </summary>
        public static IndicatorRegistry? GetIndicatorRegistry(this IEnvironment environment)
            => (environment.GetFunction(StockFunctions.IndicatorName) as IndicatorFunction)?.Registry;
    }
}
