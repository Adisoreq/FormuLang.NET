using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Structures;

namespace FormuLang.NET.Core.Standards
{
    /// <summary>
    /// The Core standard (docs/standards/core/README.md).
    /// </summary>
    public sealed class CoreStandard : IStandard
    {
        public const string Identifier = "core-0.2";

        public static CoreStandard Instance { get; } = new();

        public string ID => "core";

        public IStandardVersion Version { get; } = new StandardVersion { Major = 0, Minor = 2 };

        public string Name => "Core";

        public IEnumerable<string> ExtendedTypes => [];

        public IAbstractTree<string> ExtendedTypeTree { get; } = new AbstractTree<string>(Types.ObjectName, Types.ObjectName);

        public IEnumerable<IFunction> NamedFunctions { get; } =
            CoreFunctions.All.Cast<IFunction>().Concat(CoreProcedures.All).ToArray();

        private CoreStandard() { }
    }
}
