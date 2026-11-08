using FormuLang.NET.Abstractions.Functions;

namespace FormuLang.NET.Abstractions.Structures
{
    public interface IStandard
    {
        public string ID { get; }
        public IStandardVersion Version { get; }
        public string Name { get; }

        public IEnumerable<string> ExtendedTypes { get; }
        public IAbstractTree<string> ExtendedTypeTree { get; }
        public IEnumerable<IFunction> NamedFunctions { get; }
    }
}
