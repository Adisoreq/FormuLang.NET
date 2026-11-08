using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;

namespace FormuLang.NET.Abstractions.Functions
{
    public interface IFunctionOverload : IInvokable
    {
        public IEnumerable<IArgumentTemplate> Template { get; }
        public bool Matches(IEnumerable<IValue> parameters, IContext context);
    }
}
